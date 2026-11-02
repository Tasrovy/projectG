using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Markup;
using Yarn.Unity;

/// <summary>正文点击先完成逐字显示，显示完成后的下一次点击才推进。</summary>
public sealed class NovelContinueHandler : ActionMarkupHandler
{
    [SerializeField] private Button continueButton;
    [SerializeField] private DialogueRunner dialogueRunner;
    private bool fullyVisible;
    private bool presenting;
    private int lastClickFrame = -1;

    private void Awake()
    {
        if (continueButton != null) continueButton.interactable = false;
    }

    public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text)
    {
        fullyVisible = false;
        presenting = false;
        if (continueButton == null) return;
        continueButton.enabled = true;
        continueButton.interactable = true;
        continueButton.onClick.RemoveListener(Advance);
        continueButton.onClick.AddListener(Advance);
    }

    public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
    {
        presenting = true;
    }

    public override YarnTask OnCharacterWillAppear(int index, MarkupParseResult line, CancellationToken token)
        => YarnTask.CompletedTask;

    public override void OnLineDisplayComplete() { fullyVisible = true; }

    public void Advance()
    {
        if (!presenting || dialogueRunner == null || !dialogueRunner.IsDialogueRunning
            || lastClickFrame == Time.frameCount) return;
        var reading = DialogueHandler.Instance != null
            ? DialogueHandler.Instance.GetComponent<NovelReadingController>() : null;
        if (reading != null && reading.Mode == NovelReadingController.DisplayMode.Artwork) return;
        if (reading != null && reading.GetComponent<NovelSkipController>()?.BlocksReading == true) return;
        lastClickFrame = Time.frameCount;
        if (fullyVisible) dialogueRunner.RequestNextLine();
        else dialogueRunner.RequestHurryUpLine();
    }

    public override void OnLineWillDismiss()
    {
        presenting = false;
        if (continueButton == null) return;
        continueButton.onClick.RemoveListener(Advance);
        continueButton.interactable = false;
    }
}
