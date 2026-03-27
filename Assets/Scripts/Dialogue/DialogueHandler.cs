using System.Collections;
using System.Collections.Generic;
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
        }

        wasDialogueRunning = isDialogueRunning;
    }

    public void StartDialogue(string yarnScript)
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.StartDialogue(yarnScript);
            ShowSkipButton();
            wasDialogueRunning = true;
        }
    }

    private void HandleSkipDialogueClicked()
    {
        if (dialogueRunner == null || !dialogueRunner.IsDialogueRunning)
        {
            HideSkipButton();
            return;
        }

        if (characterHighlightManager == null)
        {
            characterHighlightManager = GetComponent<CharacterHighlightManager>();
        }

        if (characterHighlightManager != null)
        {
            _ = characterHighlightManager.OnDialogueCompleteAsync();
        }

        dialogueRunner.Stop();
        HideSkipButton();
        wasDialogueRunning = false;
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
