using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class DialogueHandler : MonoBehaviour
{
    public static DialogueHandler Instance { get; private set; }
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private Button skipDialogueButton;

    private CharacterHighlightManager characterHighlightManager;
    private bool wasDialogueRunning;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        

        characterHighlightManager = GetComponent<CharacterHighlightManager>();

        if (skipDialogueButton != null)
        {
            skipDialogueButton.onClick.RemoveListener(HandleSkipDialogueClicked);
            skipDialogueButton.onClick.AddListener(HandleSkipDialogueClicked);
            skipDialogueButton.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (dialogueRunner == null)
        {
            return;
        }

        bool isDialogueRunning = dialogueRunner.IsDialogueRunning;
        if (wasDialogueRunning && !isDialogueRunning)
        {
            HideSkipButton();
            // 在对话正常跑完结束后触发离场转场
            StartCoroutine(TransitionManager.Instance.PlayTransition());
        }

        wasDialogueRunning = isDialogueRunning;
    }

    public void StartDialogue(string yarnScript)
    {
        if (dialogueRunner != null)
        {
            StartCoroutine(StartDialogueRoutine(yarnScript));
        }
    }

    private IEnumerator StartDialogueRoutine(string yarnScript)
    {
        // 保证顺序：1. 先执行完整转场（黑落 -> 停留 -> 变亮）
        yield return TransitionManager.Instance.PlayTransition();
        
        // 当屏幕完全亮起转场结束后，2. 才会启动对话引擎跳出文本！
        dialogueRunner.StartDialogue(yarnScript);
        ShowSkipButton();
        wasDialogueRunning = true;
    }

    public void SetDialogueProperties(int p1, int p2, int p3, int money)
    {
        if (characterHighlightManager == null)
        {
            characterHighlightManager = GetComponent<CharacterHighlightManager>();
        }

        if (characterHighlightManager != null)
        {
            characterHighlightManager.SetDialogueCompleteProperties(p1, p2, p3, money);
        }
        else
        {
            Debug.LogError("CharacterHighlightManager not found on the same GameObject.");
        }
    }

    private async void HandleSkipDialogueClicked()
    {
        if (dialogueRunner == null || !dialogueRunner.IsDialogueRunning)
        {
            HideSkipButton();
            return;
        }

        if (skipDialogueButton != null)
        {
            skipDialogueButton.interactable = false;
        }

        await dialogueRunner.Stop();

        HideSkipButton();
        if (skipDialogueButton != null)
        {
            skipDialogueButton.interactable = true;
        }
        wasDialogueRunning = false;

        // 如果玩家因为跳过而人工强停对话结束，那么同样触发离场的转场
        StartCoroutine(TransitionManager.Instance.PlayTransition());
    }

    private void ShowSkipButton()
    {
        if (skipDialogueButton != null)
        {
            skipDialogueButton.gameObject.SetActive(true);
        }
    }

    private void HideSkipButton()
    {
        if (skipDialogueButton != null)
        {
            skipDialogueButton.gameObject.SetActive(false);
        }
    }
}
