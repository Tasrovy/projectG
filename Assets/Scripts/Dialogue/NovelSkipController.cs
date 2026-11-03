using System.Linq;
using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public sealed class NovelSkipController : DialoguePresenterBase
{
    [Header("确认弹窗：1920 × 1080")]
    public Vector2 panelPosition = Vector2.zero;
    public Vector2 panelSize = new Vector2(650, 280);
    public Vector2 buttonSize = new Vector2(220, 65);
    public Vector2 messagePosition = new Vector2(0, 65);
    public Vector2 buttonOffset = new Vector2(145, -65);
    public Color panelColor = new Color(.15f,.13f,.18f);
    public Color buttonColor = new Color(.35f,.25f,.4f);
    public Color textColor = Color.white;
    public bool showModalShade = false;
    public Color dimColor = new Color(0,0,0,.65f);
    [TextArea] public string message = "是否跳过当前这段剧情？";
    public string cancelText = "取消", confirmText = "确定";
    public float messageFontSize = 32, buttonFontSize = 28;
    public TMP_FontAsset uiFont;
    [Header("加载背景与状态栏")]
    public Sprite loadingBackground;
    public enum BackgroundFit { Cover, Contain, Stretch }
    [Tooltip("Cover 等比铺满裁切；Contain 等比完整显示；Stretch 拉伸铺满")]
    public BackgroundFit backgroundFit = BackgroundFit.Cover;
    [Min(.01f)] public float backgroundScale = 1f;
    public Vector2 backgroundOffset = Vector2.zero;
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    [Range(0, 1)] public float resolutionMatch = .5f;
    public Color coverColor = new Color(.06f,.04f,.09f,1);
    public Vector2 loadingPosition = new Vector2(240,80);
    public Vector2 loadingPanelSize = new Vector2(440,75);
    public Color loadingPanelColor = new Color(0,0,0,.65f);
    public string loadingText = "正在跳过剧情";
    [Min(.05f)] public float fadeDuration = .25f;
    [Min(0)] public float minimumCoverTime = .5f;
    [Header("跳过加速")]
    public bool accelerateVisuals = true;
    private static NovelSkipController fastOwner;
    public static bool IsFastSkipping => fastOwner != null && fastOwner.skipping && fastOwner.accelerateVisuals;
    private readonly System.Collections.Generic.Dictionary<LinePresenter, bool> savedFades = new System.Collections.Generic.Dictionary<LinePresenter, bool>();
    private void BeginFastMode()
    {
        fastOwner = this;
        foreach (var presenter in runner.DialoguePresenters.OfType<LinePresenter>())
        {
            savedFades[presenter] = presenter.useFadeEffect;
            if (accelerateVisuals) presenter.useFadeEffect = false;
        }
    }
    private void EndFastMode()
    {
        foreach (var entry in savedFades)
            if (entry.Key != null) entry.Key.useFadeEffect = entry.Value;
        savedFades.Clear();
        if (fastOwner == this) fastOwner = null;
    }
    private DialogueRunner runner;
    private GameObject modal;
    private CanvasGroup cover;
    private TMP_Text loadingLabel;
    private Coroutine transition;
    private bool skipping, transitioning, boundaryReached, hasLine;
    private CancellationToken lineToken;
    private int lineFrame;
    public bool BlocksReading => transitioning || skipping || (modal != null && modal.activeSelf);
    public void Initialize(DialogueRunner value)
    {
        ClearPreview();
        runner = value;
        if (runner != null && !runner.DialoguePresenters.Contains(this))
            runner.DialoguePresenters = runner.DialoguePresenters.Append(this);
    }
    public void AskToSkip()
    {
        if (runner == null || !runner.IsDialogueRunning || transitioning || skipping) return;
        if (modal == null) BuildModal();
        modal.SetActive(true);
    }
    private IEnumerator SkipTransition()
    {
        transitioning = true;
        boundaryReached = false;
        if (cover == null) BuildCover();
        cover.gameObject.SetActive(true);
        cover.alpha = 1;
        // 背景完全不透明且已完成一帧渲染后才允许剧情变化。
        yield return new WaitForEndOfFrame();
        yield return null;
        skipping = runner != null && runner.IsDialogueRunning;
        if (skipping) BeginFastMode();
        float started = Time.unscaledTime;
        while (runner != null && runner.IsDialogueRunning && (!boundaryReached || !hasLine))
        {
            loadingLabel.text = loadingText + new string('.', (int)(Time.unscaledTime * 3) % 4);
            yield return null;
        }
        EndFastMode();
        skipping = false;
        while (Time.unscaledTime - started < minimumCoverTime) yield return null;
        yield return new WaitForEndOfFrame();
        yield return FadeCover(0);
        cover.gameObject.SetActive(false);
        transitioning = false;
        transition = null;
    }
    private void BuildCover()
    {
        var root = new GameObject("Skip Transition", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        // 独立根 Canvas 避免继承父 UI 的小矩形。
        root.transform.SetParent(null, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(Mathf.Max(1, referenceResolution.x), Mathf.Max(1, referenceResolution.y));
        scaler.matchWidthOrHeight = resolutionMatch;
        cover = root.GetComponent<CanvasGroup>();
        var bg = Box("Opaque Base", root.transform, Vector2.zero, Vector2.zero, new Color(coverColor.r,coverColor.g,coverColor.b,1));
        Stretch((RectTransform)bg.transform);
        Sprite sprite = loadingBackground;
        if (Application.isPlaying && runner != null && DayManager.Instance != null)
        {
            var days = DayManager.Instance.daySO;
            int index = DayManager.Instance.dayNumber - 1;
            if (days != null && index >= 0 && index < days.dayDatas.Count && !string.IsNullOrWhiteSpace(days.dayDatas[index].skipBackground))
                sprite = Resources.Load<Sprite>(days.dayDatas[index].skipBackground) ?? sprite;
        }
        if (sprite != null)
        {
            var viewport = new GameObject("Background Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            Stretch((RectTransform)viewport.transform);
            var frame = new GameObject("Background Frame", typeof(RectTransform));
            frame.transform.SetParent(viewport.transform, false);
            var frameRect = (RectTransform)frame.transform;
            Stretch(frameRect);
            frameRect.anchoredPosition = backgroundOffset;
            frameRect.localScale = Vector3.one * Mathf.Max(.01f, backgroundScale);
            var art = Box("Loading Background", frame.transform, Vector2.zero, Vector2.zero, Color.white);
            var image = art.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.useSpriteMesh = false;
            Stretch((RectTransform)art.transform);
            if (backgroundFit != BackgroundFit.Stretch)
            {
                var fitter = art.AddComponent<AspectRatioFitter>();
                fitter.aspectRatio = sprite.rect.width / Mathf.Max(1, sprite.rect.height);
                fitter.aspectMode = backgroundFit == BackgroundFit.Cover
                    ? AspectRatioFitter.AspectMode.EnvelopeParent
                    : AspectRatioFitter.AspectMode.FitInParent;
            }
        }
        var status = Box("Loading Status", root.transform, loadingPanelSize, Vector2.zero, loadingPanelColor);
        var rect = (RectTransform)status.transform;
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = loadingPosition;
        Label(loadingText, status.transform, Vector2.zero, loadingPanelSize, messageFontSize);
        loadingLabel = status.GetComponentInChildren<TMP_Text>();
        status.transform.SetAsLastSibling();
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private IEnumerator FadeCover(float target)
    {
        float initial = cover.alpha;
        float elapsed = 0;
        while (elapsed < Mathf.Max(.05f, fadeDuration))
        {
            elapsed += Time.unscaledDeltaTime;
            cover.alpha = Mathf.Lerp(initial, target, elapsed / Mathf.Max(.05f, fadeDuration));
            yield return null;
        }
        cover.alpha = target;
    }

    private void Update()
    {
        if (runner == null || !runner.IsDialogueRunning)
        {
            EndFastMode();
        skipping = false;
            if (modal != null) modal.SetActive(false);
            return;
        }
        if (skipping && hasLine && !lineToken.IsCancellationRequested && Time.frameCount > lineFrame)
        {
            runner.RequestHurryUpLine();
            runner.RequestNextLine();
        }
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        hasLine = true;
        lineToken = token.NextLineToken;
        lineFrame = Time.frameCount;
        return YarnTask.CompletedTask;
    }
    public override YarnTask<DialogueOption> RunOptionsAsync(DialogueOption[] options, LineCancellationToken token)
    {
        EndFastMode();
        skipping = false;
        hasLine = true;
        boundaryReached = true;
        return DialogueRunner.NoOptionSelected;
    }
    public override void OnNodeExit(string nodeName) { EndFastMode(); skipping = false; hasLine = false; boundaryReached = true; }
    public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;
    public override YarnTask OnDialogueCompleteAsync()
    {
        EndFastMode();
        skipping = false;
        hasLine = false;
        if (modal != null) modal.SetActive(false);
        return YarnTask.CompletedTask;
    }

    private void BuildModal()
    {
        modal = new GameObject("Skip Confirmation", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        modal.transform.SetParent(null, false);
        var canvas = modal.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = modal.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var shade = Box("Shade", modal.transform, Vector2.zero, Vector2.zero, showModalShade ? dimColor : Color.clear);
        var shadeRect = (RectTransform)shade.transform;
        shadeRect.anchorMin = Vector2.zero;
        shadeRect.anchorMax = Vector2.one;
        shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
        var panel = Box("Panel", modal.transform, panelSize, panelPosition, panelColor);
        Label(message, panel.transform, messagePosition, new Vector2(panelSize.x - 50, 80), messageFontSize);
        MakeButton(cancelText, panel.transform, new Vector2(-buttonOffset.x, buttonOffset.y), () => modal.SetActive(false));
        MakeButton(confirmText, panel.transform, buttonOffset, () =>
        {
            modal.SetActive(false);
            if (transition == null) transition = StartCoroutine(SkipTransition());
        });
    }

    private static GameObject Box(string name, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = (RectTransform)obj.transform;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        obj.GetComponent<Image>().color = color;
        return obj;
    }
    private void Label(string text, Transform parent, Vector2 position, Vector2 size, float fontSize)
    {
        var obj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        var rect = (RectTransform)obj.transform;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var label = obj.GetComponent<TextMeshProUGUI>();
        var source = runner != null ? runner.GetComponentInChildren<LinePresenter>(true) : FindFirstObjectByType<LinePresenter>(FindObjectsInactive.Include);
        if (source != null && source.lineText != null) label.font = source.lineText.font;
        if (uiFont != null) label.font = uiFont;
        label.color = textColor;
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }
    private void MakeButton(string text, Transform parent, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var obj = Box(text, parent, buttonSize, position, buttonColor);
        obj.AddComponent<Button>().onClick.AddListener(action);
        Label(text, obj.transform, Vector2.zero, buttonSize, buttonFontSize);
    }
    public void ClearPreview()
    {
        if (Application.isPlaying) return;
        if (modal != null) DestroyImmediate(modal);
        if (cover != null) DestroyImmediate(cover.gameObject);
        modal = null;
        cover = null;
    }
    public void PreviewConfirmation()
    {
        if (Application.isPlaying) return;
        ClearPreview();
        BuildModal();
        modal.hideFlags = HideFlags.DontSave;
    }
    public void PreviewLoading()
    {
        if (Application.isPlaying) return;
        ClearPreview();
        BuildCover();
        cover.gameObject.hideFlags = HideFlags.DontSave;
        cover.alpha = 1;
    }

    private void OnDestroy()
    {
        EndFastMode();
        if (modal != null)
        {
            if (Application.isPlaying) Destroy(modal);
            else DestroyImmediate(modal);
        }
        if (cover != null)
        {
            if (Application.isPlaying) Destroy(cover.gameObject);
            else DestroyImmediate(cover.gameObject);
        }
    }
    private void OnDisable()
    {
        EndFastMode();
        skipping = false;
        hasLine = false;
        if (modal != null) modal.SetActive(false);
    }
}
