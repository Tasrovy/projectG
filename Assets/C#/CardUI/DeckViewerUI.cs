using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckViewerUI : Singleton<DeckViewerUI>
{
    private const string DeckOpenSfxPath = "Switch sounds/button7";
    private const string DeckCloseSfxPath = "Switch sounds/button8";

    [Header("UI 引用")] public GameObject viewerPanel; // 整个查看器面板
    public Transform contentParent; // Scroll View 里面的 Content 节点
    public GameObject displayCardPrefab; // 刚刚做的 DisplayCardPrefab
    [SerializeField] private Canvas targetCanvas;

    [Header("文本更新 (可选)")] public Text titleText; // 用来显示 "牌堆 (15张)" 等信息

    protected override bool IsPersistent => true;

    protected override void Awake()
    {
        base.Awake();
        EnsureViewer();
    }

    private void EnsureViewer()
    {
        if (viewerPanel != null && contentParent != null) return;

        GameObject prefab = Resources.Load<GameObject>("Prefabs/CardViewerWindow");
        if (prefab == null)
        {
            Debug.LogError("[DeckViewerUI] 未找到 Resources/Prefabs/CardViewerWindow 预制体。");
            return;
        }

        viewerPanel = Instantiate(prefab, GetOrCreateCanvas().transform, false);
        viewerPanel.name = "CardViewerWindow";
        titleText = viewerPanel.transform.Find("Title")?.GetComponent<Text>();
        contentParent = viewerPanel.transform.Find("Scroll View/Viewport/Content");
        displayCardPrefab = Resources.Load<GameObject>("Prefabs/CardInSet");

        Button closeBtn = viewerPanel.transform.Find("Close")?.GetComponent<Button>();
        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveListener(CloseViewer);
            closeBtn.onClick.AddListener(CloseViewer);
        }

        viewerPanel.SetActive(false);
    }

    private Canvas GetOrCreateCanvas()
    {
        if (targetCanvas != null) return targetCanvas;

        GameObject canvasObject = new GameObject("CardViewerCanvas");
        canvasObject.transform.SetParent(transform, false);

        targetCanvas = canvasObject.AddComponent<Canvas>();
        targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        targetCanvas.overrideSorting = true;
        targetCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        return targetCanvas;
    }

    public void BindDeckButton(GameObject buttonObject)
    {
        BindButton(buttonObject, OnClickDeckPile);
    }

    public void BindHandButton(GameObject buttonObject)
    {
        BindButton(buttonObject, OnClickHandPile);
    }

    private void BindButton(GameObject buttonObject, UnityEngine.Events.UnityAction onClick)
    {
        if (buttonObject == null) return;

        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(onClick);
            button.onClick.AddListener(onClick);
        }

        PointTester pointTester = buttonObject.GetComponent<PointTester>();
        if (pointTester != null)
        {
            pointTester.onClickAction.RemoveListener(onClick);
            pointTester.onClickAction.AddListener(onClick);
        }
    }

    public void OnClickDeckPile()
    {
        EnsureViewer();
        SfxTrigger.PlaySound(DeckOpenSfxPath);
        OpenViewer(CardManager.Instance.cardSet, "抽牌堆");
    }

    /// <summary>供任意场景或 UI 直接调用。</summary>
    public void ShowDeck() => OnClickDeckPile();

    public void OnClickHandPile()
    {
        EnsureViewer();
        SfxTrigger.PlaySound(DeckOpenSfxPath);
        OpenViewer(CardManager.Instance.cardInHand, "手牌");
    }

    /// <summary>供任意场景或 UI 直接调用。</summary>
    public void ShowHand() => OnClickHandPile();

    /// <summary>
    /// 打开牌堆查看器并加载卡牌
    /// </summary>
    /// <param name="deckCards">传入你想要展示的卡牌列表</param>
    /// <param name="title">面板的标题（如"抽牌堆"、"弃牌堆"）</param>
    public void OpenViewer(List<Card> deckCards, string title = "牌堆")
    {
        EnsureViewer();
        if (viewerPanel == null || contentParent == null || deckCards == null) return;

        // 1. 显示面板
        viewerPanel.SetActive(true);

        // 2. 更新标题文本
        if (titleText != null)
        {
            titleText.text = $"{title} ({deckCards.Count}张)";
        }

        // 3. 清理 Content 里旧的卡牌 (防止每次打开卡牌越来越多)
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 4. 遍历数据，生成卡牌
        foreach (Card c in deckCards)
        {
            // 实例化预制体，并将其父节点设为 Content
            GameObject newCardObj = Instantiate(displayCardPrefab, contentParent);
            // 获取我们写的展示脚本，注入数据
            CardDisplayUI displayUI = newCardObj.GetComponent<CardDisplayUI>();
            if (displayUI != null)
            {
                displayUI.Setup(c);
            }
        }
    }

    /// <summary>
    /// 关闭查看器
    /// </summary>
    public void CloseViewer()
    {
        if (viewerPanel != null && viewerPanel.activeSelf)
            SfxTrigger.PlaySound(DeckCloseSfxPath);
        if (viewerPanel != null) viewerPanel.SetActive(false);

        // 关闭查看器时同时隐藏卡牌详情面板
        var detailUI = Object.FindObjectOfType<CardDetailUI>(true);
        if (detailUI != null)
            detailUI.Hide();
    }
}
