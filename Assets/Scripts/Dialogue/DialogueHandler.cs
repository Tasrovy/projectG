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

    public void SendProperties(string properties)
    {
        if (characterHighlightManager == null)
        {
            characterHighlightManager = GetComponent<CharacterHighlightManager>();
        }

        if (characterHighlightManager == null)
        {
            Debug.LogError("CharacterHighlightManager not found on the same GameObject.");
            return;
        }

        if (string.IsNullOrWhiteSpace(properties))
        {
            Debug.LogError("SendProperties failed: input is null or empty.");
            return;
        }

        if (!Regex.IsMatch(properties, @"^-?\d+ -?\d+ -?\d+ -?\d+$"))
        {
            Debug.LogError("SendProperties format error: expected exactly four integers separated by single spaces.");
            return;
        }

        string[] parts = properties.Split(' ');
        int[] values = new int[4];
        for (int i = 0; i < 4; i++)
        {
            if (!int.TryParse(parts[i], out values[i]))
            {
                Debug.LogError("SendProperties parse error: one of the values is not a valid int.");
                return;
            }
        }

        characterHighlightManager.SetDialogueCompleteProperties(values);
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
