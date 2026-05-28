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

    [Header("文本更新 (可选)")] public Text titleText; // 用来显示 "牌堆 (15张)" 等信息

    protected override void Awake()
    {
        base.Awake();

        // 获取根对象（确保 DUELUIObjectManager 已经初始化了 DUELUI）
        GameObject root = DUELUIObjectManager.Instance.DUELUI;

        // 1. 初始化 viewerPanel: CardSet -> Window
        // 注意：结构中 Window 是 CardSet 的子物体
        Transform windowTrans = root.transform.Find("CardSet/Window");
        if (windowTrans != null)
        {
            viewerPanel = windowTrans.gameObject;

            // 2. 初始化 titleText: Window -> Title
            Transform titleTrans = windowTrans.Find("Title");
            if (titleTrans != null) titleText = titleTrans.GetComponent<Text>();

            // 3. 初始化 contentParent: Window -> Scroll View -> Viewport -> Content
            contentParent = windowTrans.Find("Scroll View/Viewport/Content");

            // 4. 初始化关闭按钮事件: Window -> Close
            Button closeBtn = windowTrans.Find("Close")?.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.AddListener(CloseViewer);
            }
        }

        // 5. 初始化 displayCardPrefab (如果是从资源加载)
        displayCardPrefab = Resources.Load<GameObject>("Prefabs/CardInSet"); // 路径按实际修改


        // 初始状态隐藏面板
        CloseViewer();

        // 绑定卡组图标点击事件: DUELUI -> CardSet
        Button btn = DUELUIObjectManager.Instance.GetCardSetGameObject().GetComponent<Button>();
        PointTester pt = btn.GetComponent<PointTester>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnClickDeckPile);
        }
        if (pt != null)
        {
            pt.onClickAction.AddListener(OnClickDeckPile);
        }
    }

    public void OnClickDeckPile()
    {
        SfxTrigger.PlaySound(DeckOpenSfxPath);
        OpenViewer(CardManager.Instance.cardSet, "抽牌堆");
    }

    /// <summary>
    /// 打开牌堆查看器并加载卡牌
    /// </summary>
    /// <param name="deckCards">传入你想要展示的卡牌列表</param>
    /// <param name="title">面板的标题（如"抽牌堆"、"弃牌堆"）</param>
    public void OpenViewer(List<Card> deckCards, string title = "牌堆")
    {
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
        viewerPanel.SetActive(false);

        // 关闭查看器时同时隐藏卡牌详情面板
        var detailUI = DUELUIObjectManager.Instance.GetCardDetailUI();
        if (detailUI != null)
            detailUI.Hide();
    }
}
