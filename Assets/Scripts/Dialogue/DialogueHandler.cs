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
    private bool willSwitchScene;
    private SceneType nextSceneType;

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
        
        // 状态转为结束：当YarnSpinner内部真正结束时执行离场
        if (wasDialogueRunning && !isDialogueRunning)
        {
            HideSkipButton();
            wasDialogueRunning = false;
            StartCoroutine(EndDialogueRoutine());
        }
        // 状态转为运行中：YarnSpinner内部真正启动时（可能要花几帧启动），才拉起UI
        else if (!wasDialogueRunning && isDialogueRunning)
        {
            wasDialogueRunning = true;
            ShowSkipButton();
        }
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
        // 保证顺序：先执行转场黑屏
        yield return TransitionManager.Instance.PlayTransition();
        
        // 向YarnSpinner发送开始指令。不再人为提前去抢状态或显示按钮
        // 接下来由Update自动完美捕捉起跑的瞬间！
        dialogueRunner.StartDialogue(yarnScript);
    }

    /// <summary>
    /// 提供给UnityEvent调用的函数，传入SceneType（由枚举名转换）
    /// 并保存起来用于对话结束后的场景切换
    /// </summary>
    public void SetNextSceneType(string sceneTypeName)
    {
        if (System.Enum.TryParse(sceneTypeName, true, out SceneType parsedScene))
        {
            nextSceneType = parsedScene;
            willSwitchScene = true;
        }
        else
        {
            Debug.LogWarning($"[DialogueHandler] Cannot parse SceneType from string: {sceneTypeName}");
        }
    }

    /// <summary>
    /// 提供给代码或支持Enum的UnityEvent使用
    /// </summary>
    public void SetNextSceneByEnum(SceneType sceneType)
    {
        nextSceneType = sceneType;
        willSwitchScene = true;
    }

    private IEnumerator EndDialogueRoutine()
    {
        // 先播放离场转场动画
        yield return TransitionManager.Instance.PlayTransition();

        // 黑屏/转场彻底结束后，检查是否有存储的待切场景
        if (willSwitchScene)
        {
            willSwitchScene = false;   // 状态复位
            
            // 调用存在的 UISceneManager 切场景接口
            if (UISceneManager.Instance != null)
            {
                UISceneManager.Instance.SwitchToScene(nextSceneType);
            }
            else
            {
                Debug.LogError("UISceneManager.Instance is null, cannot switch scene.");
            }
        }
    }

    public void SetDialogueProperties(int p1, int p2, int p3)
    {
        if (characterHighlightManager == null)
        {
            characterHighlightManager = GetComponent<CharacterHighlightManager>();
        }

        if (characterHighlightManager != null)
        {
            characterHighlightManager.SetDialogueCompleteProperties(p1, p2, p3);
        }
        else
        {
            Debug.LogError("CharacterHighlightManager not found on the same GameObject.");
        }
    }

    public void SetDialogueMoney(int minMoney, int maxMoney)
    {
        if (characterHighlightManager == null)
        {
            characterHighlightManager = GetComponent<CharacterHighlightManager>();
        }

        if (characterHighlightManager != null)
        {
            // Unity中整型Random.Range是左闭右开，因此加1以确保能取到maxMoney
            int randomMoney = Random.Range(minMoney, maxMoney + 1);
            characterHighlightManager.SetDialogueCompleteMoney(randomMoney);
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
        StartCoroutine(EndDialogueRoutine());
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
