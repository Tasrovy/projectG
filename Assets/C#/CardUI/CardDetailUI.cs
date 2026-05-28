using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class CardDetailUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private bool hideOnAwake = true;

    [Header("Position")]
    [SerializeField] private float cardOffsetX = 20f;

    private RectTransform _rectTransform;
    private Vector2 _initialAnchoredPosition;    
    [Header("卡图资源")]
    public Sprite giftSprite;
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

        // 使用 CanvasGroup 控制显隐，避免 SetActive 导致 EventSystem
        // 重新评估鼠标下方对象，引发 CardObject 的 OnPointerEnter/Exit 闪烁
        CanvasGroup group = panelRoot.GetComponent<CanvasGroup>();
        if (group == null) group = panelRoot.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        // 面板背景不拦截射线，避免挡住卡牌导致闪烁
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
    /// 固定位置显示（战斗手牌用），恢复到 prefab 初始位置
    /// </summary>
    public void ShowFixed(Card card)
    {
        ResetPosition();
        Show(card);
    }

    /// <summary>
    /// 在目标卡牌Image右侧显示（抽牌堆/三选一/商店用）
    /// </summary>
    public void ShowAtCard(Card card, RectTransform imageRect)
    {
        if (imageRect == null)
        {
            ShowFixed(card);
            return;
        }

        Vector2 targetPos = CalculateRightSidePosition(imageRect);
        SetPosition(targetPos);
        Show(card);
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
        if (CurrentCard.id.ToString()[0] == '1') Icon.sprite = giftSprite;
        if (CurrentCard.id.ToString()[0] == '2') Icon.sprite = funcSprite;
        if (CurrentCard.id.ToString()[0] == '3') Icon.sprite = eventSprite;
        BuildPromptItems(CurrentCard);

    }

    private void SetVisible(bool visible)
    {
        CanvasGroup group = panelRoot.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = visible ? 1 : 0;
        }
        else if (panelRoot != null)
        {
            panelRoot.SetActive(visible);
        }
    }

    /// <summary>
    /// 直接设置面板世界位置（ScreenSpaceOverlay 下 world = screen）
    /// </summary>
    public void SetPosition(Vector2 worldPosition)
    {
        if (_rectTransform != null)
            _rectTransform.position = new Vector3(worldPosition.x, worldPosition.y, 0);
    }

    /// <summary>
    /// 恢复到初始记录位置
    /// </summary>
    public void ResetPosition()
    {
        if (_rectTransform != null)
            _rectTransform.anchoredPosition = _initialAnchoredPosition;
    }

    /// <summary>
    /// 计算在目标卡牌右侧、不重叠的世界坐标（ScreenSpaceOverlay 即屏幕坐标）
    /// </summary>
    private Vector2 CalculateRightSidePosition(RectTransform imageRect)
    {
        if (_rectTransform == null) return _initialAnchoredPosition;

        // 卡牌所在的 Canvas 和 Camera (用于拿屏幕坐标)
        Canvas targetCanvas = imageRect.GetComponentInParent<Canvas>();
        Camera targetCam = targetCanvas != null && targetCanvas.renderMode == RenderMode.ScreenSpaceCamera
            ? targetCanvas.worldCamera : null;

        // DetailUI 自己的 Canvas 和 Camera (用于最后转回世界坐标和自身尺寸)
        Canvas myCanvas = _rectTransform.GetComponentInParent<Canvas>();
        if (myCanvas == null) return _initialAnchoredPosition;
        Camera myCam = myCanvas.renderMode == RenderMode.ScreenSpaceCamera ? myCanvas.worldCamera : null;

        // 两个 Canvas 各自的 scaleFactor (参考分辨率可能不同)
        float targetScaleFactor = targetCanvas != null ? targetCanvas.scaleFactor : 1f;
        float myScaleFactor = myCanvas.scaleFactor;

        // 卡牌 Image 世界坐标 → 屏幕坐标
        Vector3[] imgCorners = new Vector3[4];
        imageRect.GetWorldCorners(imgCorners);
        float imageRightX;
        float imageCenterY;
        float imageLeftX;
        if (targetCam != null)
        {
            Vector3 bl = targetCam.WorldToScreenPoint(imgCorners[0]);
            Vector3 tr = targetCam.WorldToScreenPoint(imgCorners[2]);
            imageLeftX = bl.x;
            imageRightX = tr.x;
            imageCenterY = (bl.y + tr.y) * 0.5f;
        }
        else
        {
            imageLeftX = imgCorners[0].x;
            imageRightX = imgCorners[2].x;
            imageCenterY = (imgCorners[0].y + imgCorners[2].y) * 0.5f;
        }

        // DetailUI 屏幕尺寸: 用自己的 scaleFactor
        float panelW = _rectTransform.rect.width * myScaleFactor;
        float panelH = _rectTransform.rect.height * myScaleFactor;

        // cardOffsetX 用卡牌所在 Canvas 的 scaleFactor, 保证间距与卡牌比例一致
        float scaledOffsetX = cardOffsetX * targetScaleFactor;

        // 屏幕空间计算
        float screenX;
        float screenY = imageCenterY + (_rectTransform.pivot.y - 0.5f) * panelH;

        // 判断右侧是否放得下, 放不下就翻转到卡牌左侧
        bool fitsRight = (imageRightX + scaledOffsetX + panelW <= Screen.width);
        if (fitsRight)
        {
            screenX = imageRightX + scaledOffsetX + _rectTransform.pivot.x * panelW;
        }
        else
        {
            screenX = imageLeftX - scaledOffsetX - (1f - _rectTransform.pivot.x) * panelW;
        }

        // 上下边界 clamp
        float panelTop = screenY + (1f - _rectTransform.pivot.y) * panelH;
        float panelBottom = screenY - _rectTransform.pivot.y * panelH;
        if (panelTop > Screen.height) screenY -= panelTop - Screen.height;
        if (panelBottom < 0) screenY -= panelBottom;

        // 用 DetailUI 自己的相机和 planeDistance 转回世界坐标
        if (myCam != null)
            return myCam.ScreenToWorldPoint(new Vector3(screenX, screenY, myCanvas.planeDistance));
        else
            return new Vector2(screenX, screenY);
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
