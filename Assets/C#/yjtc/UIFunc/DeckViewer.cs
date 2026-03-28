using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 抽牌堆查看管理器，负责打开查看界面、动态排列卡牌、支持拖动滚动
/// </summary>
public class DeckViewer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject deckViewPanel;         // 整个抽牌堆查看面板
    [SerializeField] private RectTransform cardContainer;      // 卡牌放置的容器（如Grid Layout Group所在对象）
    [SerializeField] private ScrollRect scrollRect;            // 滚动视图组件，用于拖动查看

    [Header("Layout Settings")]
    [SerializeField] private int maxColumns = 4;               // 每行最多卡牌数量
    [SerializeField] private float cardSpacing = 10f;          // 卡牌间距
    [SerializeField] private Vector2 cardSize = new Vector2(150f, 200f); // 卡牌显示大小

    [Header("Button")]
    [SerializeField] private Button openDeckButton;             // 打开抽牌堆的按钮

    // 抽牌堆中所有卡牌的列表（外部赋值，例如游戏开始时填充）
    private List<GameObject> allCardsInDeck = new List<GameObject>();

    // 当前在查看面板中显示的卡牌实例（用于清理）
    private List<GameObject> displayedCards = new List<GameObject>();

    private List<GameObject> cardImageList = new List<GameObject>();

    private void Start()
    {
        // 初始化：确保面板一开始是关闭的
        if (deckViewPanel != null)
            deckViewPanel.SetActive(false);

        // 绑定按钮事件
        if (openDeckButton != null)
            openDeckButton.onClick.AddListener(OpenDeckView);

        // 示例：模拟添加一些卡牌到抽牌堆（实际使用时，您应该从游戏逻辑中获取真实的卡牌列表）
        // 注意：这里仅为演示，实际开发中应通过外部方法初始化抽牌堆内容。
        // 您可以使用 SetDeckCards 方法来设置抽牌堆卡牌列表。
        allCardsInDeck = CardSum.Instance.DeckCardList;
    }

    private void Awake()
    {

    }

    /// <summary>
    /// 外部调用，设置抽牌堆中的卡牌列表
    /// </summary>
    /// <param name="cards">抽牌堆中所有卡牌的GameObject列表</param>
    public void SetDeckCards(List<GameObject> cards)
    {
        allCardsInDeck = new List<GameObject>(cards);
    }

    /// <summary>
    /// 打开抽牌堆查看面板，并动态排列所有卡牌
    /// </summary>
    public void OpenDeckView()
    {
        if (deckViewPanel.activeSelf)
        {
            deckViewPanel.SetActive(false);
            EventManage.SendEvent(EventManageEnum.drawPileClose, null);
            return;
        }

        if (allCardsInDeck == null || allCardsInDeck.Count == 0)
        {
            Debug.LogWarning("抽牌堆中没有卡牌，无法打开查看。");
            return;
        }
        print(111);
        // 清除之前显示的卡牌
        ClearDisplayedCards();

        // 重新生成并排列卡牌
        cardImageMake();

        // 显示面板
        if (deckViewPanel != null)
        {
            deckViewPanel.SetActive(true);
            EventManage.SendEvent(EventManageEnum.drawPileOpen, null);
        }
            

        // 可选：重置滚动位置到顶部
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    /// <summary>
    /// 关闭查看面板（可由面板上的关闭按钮调用）
    /// </summary>
    public void CloseDeckView()
    {
        if (deckViewPanel != null)
            deckViewPanel.SetActive(false);
    }

    public void cardImageMake()
    {
        // 遍历卡牌列表，为每张卡牌生成Image对象
        for (int i = 0; i < CardSum.Instance.DeckCardList.Count; i++)
        {
            GameObject originalCard = CardSum.Instance.DeckCardList[i];

            // 创建一个新的Image对象作为卡牌的显示
            GameObject cardImageObj = new GameObject($"Card_{i + 1}");

            // 设置为Content的子物体
            cardImageObj.transform.SetParent(cardContainer.transform, false);

            // 添加Image组件
            Image cardImage = cardImageObj.AddComponent<Image>();

            // 获取原始卡牌上的IndexState组件，获取卡牌ID
            InDeckState inDeckState = originalCard.GetComponent<InDeckState>();
            int id= inDeckState.deckID;
            cardImageList.Add(cardImageObj);
        }

            
}
    /// <summary>
    /// 清除当前显示的所有卡牌实例
    /// </summary>
    private void ClearDisplayedCards()
    {
        foreach (GameObject card in cardImageList)
        {
            if (card != null)
                Destroy(card);
        }
        cardImageList.Clear();
    }

    /// <summary>
    /// 根据抽牌堆列表生成UI显示，按从左到右、从上到下排列，支持滚动
    /// </summary>
    private void GenerateDeckDisplay()
    {
        if (cardContainer == null)
        {
            Debug.LogError("CardContainer未指定，无法排列卡牌。");
            return;
        }

        // 为方便布局，使用GridLayoutGroup组件自动排列
        GridLayoutGroup grid = cardContainer.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            // 如果没有GridLayoutGroup，则添加并设置参数
            grid = cardContainer.gameObject.AddComponent<GridLayoutGroup>();
        }

        // 设置网格布局参数
        grid.cellSize = cardSize;
        grid.spacing = new Vector2(cardSpacing, cardSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = maxColumns;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;

        // 确保容器的大小适应内容（ScrollRect需要Content Size Fitter配合）
        ContentSizeFitter fitter = cardContainer.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = cardContainer.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // 水平不自动适应，由GridLayoutGroup控制宽度
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;   // 垂直适应内容高度

        // 遍历抽牌堆中的所有卡牌，创建UI显示对象
        foreach (GameObject originalCard in allCardsInDeck)
        {
            if (originalCard == null) continue;

            // 创建卡牌UI副本（实例化原始卡牌，或者根据需求生成显示用Prefab）
            // 这里直接实例化原始卡牌，但注意：原始卡牌可能带有复杂逻辑或3D模型，建议使用专门用于UI显示的Prefab。
            // 为了演示，我们实例化原始卡牌并设置其父级为cardContainer，同时调整其Transform为UI布局。
            GameObject displayCard = Instantiate(originalCard, cardContainer);

            // 确保RectTransform正确，以便GridLayoutGroup控制位置
            RectTransform rect = displayCard.GetComponent<RectTransform>();
            if (rect == null)
                rect = displayCard.AddComponent<RectTransform>();

            // 重置局部位置和缩放（GridLayoutGroup会管理位置）
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = cardSize;
            rect.localScale = Vector3.one;

            // 确保卡牌上的Canvas组件不会干扰（如果有，可调整层级）
            Canvas cardCanvas = displayCard.GetComponent<Canvas>();
            if (cardCanvas != null)
            {
                cardCanvas.overrideSorting = true;
                cardCanvas.sortingOrder = 1; // 确保在面板上方显示
            }

            // 可选：禁用卡牌上的交互组件（如Button），避免在查看时误操作
            Button btn = displayCard.GetComponent<Button>();
            if (btn != null)
                btn.interactable = false;

            // 记录已显示的卡牌，以便清理
            displayedCards.Add(displayCard);
        }
    }

    /// <summary>
    /// 可选：通过按钮关闭面板时调用
    /// </summary>
    public void OnCloseButtonClicked()
    {
        CloseDeckView();
    }

    // 示例：在Inspector中提供调试方法，模拟添加卡牌（实际使用时请勿依赖）
    [ContextMenu("Debug/Add Sample Cards")]
    private void AddSampleCards()
    {
        // 模拟创建一些测试卡牌对象（实际项目中，您应该通过游戏逻辑获取卡牌）
        List<GameObject> sampleCards = new List<GameObject>();
        for (int i = 1; i <= 20; i++)
        {
            GameObject card = new GameObject($"SampleCard_{i}");
            InDeckState state = card.AddComponent<InDeckState>();
            state.deckID = i;
            // 添加一个Image组件用于视觉展示（可选，仅演示）
            Image img = card.AddComponent<Image>();
            img.color = Random.ColorHSV();
            sampleCards.Add(card);
        }
        SetDeckCards(sampleCards);
        Debug.Log($"已添加 {sampleCards.Count} 张测试卡牌到抽牌堆。");
    }
}

/// <summary>
/// 可选：用于从任何地方调用打开抽牌堆的辅助类（例如在其他脚本中触发）
/// </summary>
public class DeckViewerHelper : MonoBehaviour
{
    public DeckViewer deckViewer;
    public void OpenDeck()
    {
        if (deckViewer != null)
            deckViewer.OpenDeckView();
    }
}
