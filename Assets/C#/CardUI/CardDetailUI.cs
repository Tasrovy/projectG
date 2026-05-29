using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class CardDetailUI : MonoBehaviour
{
    [Header("Panel")] [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private bool hideOnAwake = true;

    [Header("Position")] [SerializeField] private float cardOffsetX = 20f;

    private RectTransform _rectTransform;
    private Vector2 _initialAnchoredPosition;
    private float _canvasScale = 1f;

    [Header("卡图资源")] public Sprite giftSprite;
    public Sprite eventSprite;
    public Sprite funcSprite;
    public Image Icon;

    [Header("Main Display")] 
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text textText;

    [Header("Prompt (ScrollView)")] 
    [SerializeField] private PromptItemUI promptItemPrefab;
    [SerializeField] private Transform promptContent;

    public Card CurrentCard { get; private set; }

    public bool IsVisible
    {
        get
        {
            if (panelRoot == null || !panelRoot.activeSelf) return false;
            CanvasGroup group = panelRoot.GetComponent<CanvasGroup>();
            return group == null || group.alpha > 0;
        }
    }

    private PromptItemSO _promptItemSO;
    private bool _promptLoaded;

    private void Awake()
    {
        EnsurePromptLoaded();
        if (panelRoot == null)
            panelRoot = gameObject;

        _rectTransform = panelRoot.GetComponent<RectTransform>();
        if (_rectTransform != null)
            _initialAnchoredPosition = _rectTransform.anchoredPosition;

        Canvas c = GetComponentInParent<Canvas>();
        if (c != null) _canvasScale = c.scaleFactor;

        // 确保 CanvasGroup 存在
        CanvasGroup group = panelRoot.GetComponent<CanvasGroup>();
        if (group == null) group = panelRoot.AddComponent<CanvasGroup>();

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        // 面板背景不拦截射线
        Image panelImage = panelRoot.GetComponent<Image>();
        if (panelImage != null)
            panelImage.raycastTarget = false;

        if (hideOnAwake)
            Hide();
    }

    public void Show(Card card)
    {
        if (card == null)
        {
            Hide();
            return;
        }

        CurrentCard = card;
        Refresh();
        SetVisible(true);
    }

    /// <summary>
    /// 固定位置显示（战斗手牌用），恢复到 prefab 初始位置，开启交互
    /// </summary>
    public void ShowFixed(Card card)
    {
        ResetPosition();
        Show(card);

        // 🌟 静态显示模式下，必须允许射线拦截，否则 closeButton 无法点击
        CanvasGroup group = panelRoot.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.blocksRaycasts = true;
            group.interactable = true;
        }
    }

    /// <summary>
    /// 在目标卡牌右侧显示。悬停模式下关闭物理射线，防止闪烁。
    /// </summary>
    public void ShowAtCard(Card card, RectTransform imageRect)
    {
        if (imageRect == null || _rectTransform == null)
        {
            ShowFixed(card);
            return;
        }

        // 1. 先填充数据并显示
        Show(card);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);

        // 🌟 [核心修改 1]：悬停对齐模式下，强制关闭此面板的一切射线拦截！
        // 这样鼠标指针会直接“穿透”该面板，永远聚焦在卡牌上，彻底根除因遮挡导致的闪烁。
        CanvasGroup group = panelRoot.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.blocksRaycasts = false; 
            group.interactable = false; // 悬停时不需要与其交互
        }

        // =========================================================
        // 第一步：获取两者所归属的 Canvas 和 渲染相机
        // =========================================================
        Canvas cardCanvas = imageRect.GetComponentInParent<Canvas>();
        Camera cardCam = (cardCanvas != null && cardCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? cardCanvas.worldCamera
            : null;

        Canvas detailCanvas = _rectTransform.GetComponentInParent<Canvas>();
        Camera detailCam = (detailCanvas != null && detailCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? detailCanvas.worldCamera
            : null;

        // =========================================================
        // 第二步：将卡牌的世界边界 -> 统一投影到屏幕像素 (Screen Pixels)
        // =========================================================
        Vector3[] cardWorldCorners = new Vector3[4];
        imageRect.GetWorldCorners(cardWorldCorners);

        Vector2 cardScreenBL = RectTransformUtility.WorldToScreenPoint(cardCam, cardWorldCorners[0]);
        Vector2 cardScreenTR = RectTransformUtility.WorldToScreenPoint(cardCam, cardWorldCorners[2]);

        float cardLeft = cardScreenBL.x;
        float cardRight = cardScreenTR.x;
        float cardCenterY = (cardScreenBL.y + cardScreenTR.y) * 0.5f;

        // =========================================================
        // 第三步：将面板的世界边界 -> 投影到屏幕像素 (获取面板在屏幕上真实占据的像素宽高)
        // =========================================================
        Vector3[] detailWorldCorners = new Vector3[4];
        _rectTransform.GetWorldCorners(detailWorldCorners);

        Vector2 detailScreenBL = RectTransformUtility.WorldToScreenPoint(detailCam, detailWorldCorners[0]);
        Vector2 detailScreenTR = RectTransformUtility.WorldToScreenPoint(detailCam, detailWorldCorners[2]);

        float panelW = detailScreenTR.x - detailScreenBL.x;
        float panelH = detailScreenTR.y - detailScreenBL.y;

        // =========================================================
        // 第四步：在统一的【屏幕坐标系】下进行排版计算
        // =========================================================
        float scaleFactor = detailCanvas != null ? detailCanvas.scaleFactor : 1f;
        float gap = cardOffsetX * scaleFactor;

        float px = _rectTransform.pivot.x;
        float py = _rectTransform.pivot.y;

        float pivotX_IfRight = cardRight + gap + px * panelW;

        float sx;
        // 判断右边界是否超屏
        if (pivotX_IfRight + (1f - px) * panelW <= Screen.width)
        {
            sx = pivotX_IfRight;
        }
        else
        {
            // 🌟 [核心修改 2]：往左边翻转时，额外增加一小段安全边距（20像素），防止面板贴得太近碰到鼠标
            float safetyMargin = 20f * scaleFactor;
            sx = cardLeft - gap - safetyMargin - (1f - px) * panelW;
        }

        float sy = cardCenterY + (py - 0.5f) * panelH;

        // 上下边界 Clamp
        float top = sy + (1f - py) * panelH;
        float bot = sy - py * panelH;
        if (top > Screen.height) sy -= (top - Screen.height);
        if (bot < 0) sy += (0 - bot);

        // 计算出面板 Pivot 理想的屏幕像素坐标
        Vector2 targetScreenPos = new Vector2(sx, sy);

        // =========================================================
        // 第五步：将算出的屏幕坐标，逆推回面板所属的真实的 3D 世界坐标
        // =========================================================
        RectTransform parentRect = _rectTransform.parent as RectTransform;
        if (parentRect == null) parentRect = _rectTransform;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, targetScreenPos, detailCam,
                out Vector3 targetWorldPos))
        {
            _rectTransform.position = targetWorldPos;
        }
        else
        {
            _rectTransform.position = new Vector3(targetScreenPos.x, targetScreenPos.y, 0);
        }
    }

    // ---- CardObject / CardUIObject / CardDisplayUI 兼容重载 ----
    public void Show(CardObject cardObject)
    {
        Show(cardObject != null ? cardObject.card : null);
    }

    public void Show(CardUIObject cardUIObject)
    {
        Show(cardUIObject != null ? cardUIObject.Card : null);
    }

    public void Toggle(Card card)
    {
        if (IsVisible && ReferenceEquals(CurrentCard, card))
        {
            Hide();
            return;
        }

        Show(card);
    }

    public void Hide()
    {
        CurrentCard = null;
        ClearPromptItems();
        SetVisible(false);
    }

    public void Refresh()
    {
        if (CurrentCard == null) return;

        SetText(descriptionText, CurrentCard.GetParsedDescription());
        SetText(textText, CurrentCard.text);

        string idStr = Math.Abs(CurrentCard.id).ToString();
        if (idStr.Length > 0)
        {
            char firstChar = idStr[0];
            if (firstChar == '1') Icon.sprite = giftSprite;
            else if (firstChar == '2') Icon.sprite = funcSprite;
            else if (firstChar == '3') Icon.sprite = eventSprite;
        }

        BuildPromptItems(CurrentCard);
    }

    private void SetVisible(bool visible)
    {
        CanvasGroup group = panelRoot.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = visible ? 1 : 0;
            // 🌟 默认可见时开启交互，但是在 ShowAtCard 中会根据悬停状态动态覆盖该值
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
        else if (panelRoot != null)
        {
            panelRoot.SetActive(visible);
        }
    }

    /// <summary>
    /// 恢复到初始记录位置
    /// </summary>
    public void ResetPosition()
    {
        if (_rectTransform != null)
            _rectTransform.anchoredPosition = _initialAnchoredPosition;
    }

    private static void SetText(Text target, string value)
    {
        if (target != null)
            target.text = string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static string FormatOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static string BuildNatureText(Card card)
    {
        StringBuilder builder = new StringBuilder();
        AppendNature(builder, "Nature 1", card.nature1);
        AppendNature(builder, "Nature 2", card.nature2);
        AppendNature(builder, "Nature 3", card.nature3);
        return builder.Length > 0 ? builder.ToString() : "-";
    }

    private static void AppendNature(StringBuilder builder, string label, int value)
    {
        if (value == 0) return;

        if (builder.Length > 0)
            builder.AppendLine();

        builder.Append(label);
        builder.Append(": ");
        builder.Append(value);
    }

    private void BuildPromptItems(Card card)
    {
        ClearPromptItems();

        if (string.IsNullOrWhiteSpace(card.prompt)) return;
        if (promptItemPrefab == null || promptContent == null) return;

        EnsurePromptLoaded();
        if (_promptItemSO == null || _promptItemSO.allItems == null) return;

        string[] ids = card.prompt.Split(',');

        foreach (string idStr in ids)
        {
            if (!int.TryParse(idStr.Trim(), out int promptId)) continue;

            PromptItem item = _promptItemSO.allItems.Find(p => p.id == promptId);
            if (item == null) continue;

            PromptItemUI ui = Instantiate(promptItemPrefab, promptContent);
            ui.SetPromptItem(item);
        }
    }

    private void ClearPromptItems()
    {
        if (promptContent == null) return;

        for (int i = promptContent.childCount - 1; i >= 0; i--)
        {
            Destroy(promptContent.GetChild(i).gameObject);
        }
    }

    private void EnsurePromptLoaded()
    {
        if (_promptLoaded) return;
        _promptLoaded = true;

        _promptItemSO = ExcelLoader.Instance.ReadPromptExcel("prompt.xlsx");
    }

    private static int GetCardType(int id)
    {
        string value = Math.Abs(id).ToString();
        return value.Length > 0 && int.TryParse(value[0].ToString(), out int type) ? type : 0;
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value, out int result) ? result : 0;
    }
}