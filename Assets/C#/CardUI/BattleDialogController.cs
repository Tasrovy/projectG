using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleDialogController : Singleton<BattleDialogController>
{
    [Header("Dialog Excel")]
    [SerializeField] private string dialogExcelPath = "dialog.xlsx";

    [Header("Display")]
    [SerializeField] private float defaultShowTime = 2f;
    [SerializeField] private float fadeOutDuration = 0.25f;
    [SerializeField] private bool useFadeOut = true;
    [SerializeField] private string portraitResourcePath = "Portraits/BattlePortrait";

    private readonly Dictionary<int, DialogEntry> _dialogById = new Dictionary<int, DialogEntry>();
    private bool _dialogLoaded;

    private RectTransform _root;
    private Image _portraitImage;
    private GameObject _bubbleRoot;
    private Text _bubbleText;
    private CanvasGroup _bubbleCanvasGroup;
    private Coroutine _hideCoroutine;
    private Coroutine _shakeCoroutine;
    private Coroutine _magnifyCoroutine;
    private Vector2 _portraitBaseAnchoredPosition;
    private Vector3 _portraitBaseScale = Vector3.one;

    protected override bool IsPersistent => false;

    protected override void Awake()
    {
        base.Awake();
        Debug.Log("[BattleDialog] Awake begin");
        EnsureUIRefs();
        ApplyPortraitSpriteIfConfigured();
        HideBubbleImmediate();
        Debug.Log("[BattleDialog] Awake end");
    }

    public void TryShowByCard(Card card)
    {
        if (card == null)
        {
            Debug.Log("[BattleDialog] TryShowByCard skipped: card is null");
            return;
        }
        if (card.dialog <= 0)
        {
            Debug.Log($"[BattleDialog] TryShowByCard skipped: card={card.name}, dialog={card.dialog}");
            return;
        }

        Debug.Log($"[BattleDialog] TryShowByCard trigger: card={card.name}, dialogId={card.dialog}");
        EnsureDialogLoaded();
        if (!_dialogById.TryGetValue(card.dialog, out DialogEntry entry))
        {
            Debug.LogWarning($"[BattleDialog] 未找到对话ID: {card.dialog}");
            return;
        }

        string content = entry.comment;
        if (string.IsNullOrWhiteSpace(content))
        {
            Debug.LogWarning($"[BattleDialog] 对话内容为空，dialogId={card.dialog}");
            return;
        }

        float showTime = entry.showTime > 0f ? entry.showTime : defaultShowTime;
        Debug.Log($"[BattleDialog] Show dialog: id={entry.id}, showTime={showTime}, content={content}");
        ShowDialog(content, showTime);
    }

    private void EnsureDialogLoaded()
    {
        if (_dialogLoaded) return;
        _dialogLoaded = true;
        _dialogById.Clear();

        DialogSO dialogSO = ExcelLoader.Instance.ReadDialogExcel(dialogExcelPath);
        if (dialogSO == null || dialogSO.entries == null)
        {
            Debug.LogWarning($"[BattleDialog] DialogSO load failed: {dialogExcelPath}");
            return;
        }

        foreach (DialogEntry entry in dialogSO.entries)
        {
            if (entry == null || entry.id <= 0) continue;
            _dialogById[entry.id] = entry;
        }
        Debug.Log($"[BattleDialog] Dialog table loaded: path={dialogExcelPath}, count={_dialogById.Count}");
    }

    private void EnsureUI()
    {
        EnsureUIRefs();
        if (_bubbleRoot == null || _bubbleText == null || _bubbleCanvasGroup == null)
        {
            Debug.LogWarning("[BattleDialog] DUELUI 缺少 BattleDialog/Bubble/Text 节点或组件。");
        }
    }

    private void EnsureUIRefs()
    {
        if (_root != null && _portraitImage != null && _bubbleRoot != null && _bubbleText != null && _bubbleCanvasGroup != null) return;

        _root = DUELUIObjectManager.Instance.GetBattleDialogRoot();
        _portraitImage = DUELUIObjectManager.Instance.GetBattlePortraitImage();
        _bubbleRoot = DUELUIObjectManager.Instance.GetBattleDialogBubble();
        _bubbleText = DUELUIObjectManager.Instance.GetBattleDialogText();
        _bubbleCanvasGroup = DUELUIObjectManager.Instance.GetBattleDialogCanvasGroup();

        Debug.Log(
            $"[BattleDialog] UI refs: root={(_root != null)}, portrait={(_portraitImage != null)}, bubble={(_bubbleRoot != null)}, text={(_bubbleText != null)}, canvasGroup={(_bubbleCanvasGroup != null)}"
        );

        if (_portraitImage != null)
        {
            RectTransform portraitRect = _portraitImage.rectTransform;
            _portraitBaseAnchoredPosition = portraitRect.anchoredPosition;
            _portraitBaseScale = portraitRect.localScale;
        }
    }

    private void ApplyPortraitSpriteIfConfigured()
    {
        if (_portraitImage == null)
        {
            Debug.LogWarning("[BattleDialog] Portrait Image is null. 请检查 DUELUI/BattleDialog/Portrait 是否存在并挂了 Image。");
            return;
        }
        if (string.IsNullOrWhiteSpace(portraitResourcePath))
        {
            Debug.LogWarning("[BattleDialog] portraitResourcePath 为空，跳过立绘加载。");
            return;
        }

        Debug.Log($"[BattleDialog] Try load portrait from Resources path: {portraitResourcePath}");
        Sprite portrait = Resources.Load<Sprite>(portraitResourcePath);
        if (portrait == null)
        {
            Debug.LogWarning($"[BattleDialog] Portrait sprite load failed: Resources/{portraitResourcePath}");
            return;
        }

        _portraitImage.sprite = portrait;
        _portraitImage.preserveAspect = true;
        _portraitImage.color = Color.white;
        Debug.Log($"[BattleDialog] Portrait loaded: {portrait.name}");
    }

    private void ShowDialog(string content, float showTime)
    {
        EnsureUI();
        if (_bubbleRoot == null || _bubbleText == null || _bubbleCanvasGroup == null) return;

        _bubbleText.text = content;
        _bubbleCanvasGroup.alpha = 1f;
        _bubbleRoot.SetActive(true);

        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(HideDialogRoutine(showTime));
    }

    public void Shake(int type, float extent)
    {
        EnsureUIRefs();
        if (_portraitImage == null) return;

        float clampedExtent = Mathf.Clamp01(extent);
        if (clampedExtent <= 0f) return;

        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeRoutine(type, clampedExtent));
    }

    public void Magnify(float multiple)
    {
        EnsureUIRefs();
        if (_portraitImage == null) return;

        if (_magnifyCoroutine != null) StopCoroutine(_magnifyCoroutine);
        _magnifyCoroutine = StartCoroutine(MagnifyRoutine(multiple));
    }

    private IEnumerator ShakeRoutine(int type, float extent)
    {
        RectTransform portraitRect = _portraitImage.rectTransform;
        Vector2 origin = _portraitBaseAnchoredPosition;
        float duration = 0.3f;
        float amplitude = 40f * extent;
        float frequency = 28f;
        float elapsed = 0f;
        Vector2 axis = type == 0 ? Vector2.up : Vector2.right;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(elapsed / duration);
            float offset = Mathf.Sin(elapsed * frequency * Mathf.PI) * amplitude * fade;
            portraitRect.anchoredPosition = origin + axis * offset;
            yield return null;
        }

        portraitRect.anchoredPosition = origin;
        _shakeCoroutine = null;
    }

    private IEnumerator MagnifyRoutine(float multiple)
    {
        RectTransform portraitRect = _portraitImage.rectTransform;
        Vector3 origin = _portraitBaseScale;
        float safeMultiple = Mathf.Max(0f, multiple);
        Vector3 target = origin * (1f + safeMultiple);
        float growDuration = 0.2f;
        float holdDuration = 0.12f;
        float shrinkDuration = 0.16f;
        float elapsed = 0f;

        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / growDuration);
            portraitRect.localScale = Vector3.Lerp(origin, target, t);
            yield return null;
        }

        portraitRect.localScale = target;
        yield return new WaitForSeconds(holdDuration);

        elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkDuration);
            portraitRect.localScale = Vector3.Lerp(target, origin, t);
            yield return null;
        }

        portraitRect.localScale = origin;
        _magnifyCoroutine = null;
    }

    private IEnumerator HideDialogRoutine(float showTime)
    {
        yield return new WaitForSeconds(showTime);

        if (!useFadeOut || fadeOutDuration <= 0f)
        {
            HideBubbleImmediate();
            yield break;
        }

        float t = 0f;
        float startAlpha = _bubbleCanvasGroup.alpha;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeOutDuration);
            _bubbleCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, p);
            yield return null;
        }

        HideBubbleImmediate();
    }

    private void HideBubbleImmediate()
    {
        if (_bubbleCanvasGroup != null) _bubbleCanvasGroup.alpha = 0f;
        if (_bubbleRoot != null) _bubbleRoot.SetActive(false);
        Debug.Log("[BattleDialog] Bubble hidden");
    }
}
