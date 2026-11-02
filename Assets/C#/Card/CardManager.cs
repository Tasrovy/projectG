using System;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : Singleton<CardManager>
{
    public List<CardData> cardDatas = new List<CardData>();
    public List<CardData> giftCards = new List<CardData>();
    public List<CardData> funcCards = new List<CardData>();
    public List<CardData> eventCards = new List<CardData>();
    public List<Card> cardSet = new List<Card>();
    public List<Card> cardInHand = new List<Card>();
    [HideInInspector] public int consecutiveNonGiftCount = 0; // 礼物牌保底计数：连续未抽到礼物牌的次数
    [HideInInspector] public int consecutiveNonFuncCount  = 0; // 功能牌保底计数：连续未抽到功能牌的次数
    [HideInInspector] public int consecutiveNonEventCount = 0; // 事件牌保底计数：连续未抽到事件牌的次数

    protected override bool IsPersistent => true;

    [Header("动态稀有度概率 (和需为 1.0)")]
    [Range(0, 1)] public float probRarity1 = 0.7f;
    [Range(0, 1)] public float probRarity2 = 0.2f;
    [Range(0, 1)] public float probRarity3 = 0.1f;

    [Header("Excel配置")]
    public List<string> cardExcelPaths = new List<string> { "giftCard.xlsx", "funcCard.xlsx", "eventCard.xlsx" };

    [Header("初始手牌配置")]
    public bool useInitialHandFromExcel = false;
    public string initialHandExcelPath = "initialHand.xlsx";

    private readonly System.Random _rng = new System.Random();

    private CardRepositoryService _repositoryService;
    private DeckService _deckService;
    private HandService _handService;
    private DrawService _drawService;
    private InitialHandService _initialHandService;

    // 公开抽卡服务，供外部按职责调用
    public DrawService DrawService => _drawService;

    protected override void Awake()
    {
        base.Awake();
        InitializeServices();

        LoadAllCards();
        FilterGiftCards();
        FilterFuncCards();
        FilterEventCards();
        ImplementCardSet();
        ApplyInitialHandFromExcel();
    }

    private void Start()
    {
        //DUEL.Instance.Begin();
    }

    private void InitializeServices()
    {
        _repositoryService = new CardRepositoryService(cardDatas, cardExcelPaths);
        _deckService = new DeckService(cardSet, cardDatas);
        _handService = new HandService(cardInHand);
        _drawService = new DrawService(cardSet, cardInHand, _rng, probRarity1, probRarity2, probRarity3);
        _initialHandService = new InitialHandService(_deckService);
    }

    // ===================== 概率与抽卡（转发 DrawService） =====================
    public void SetProbRarity1(float rarity)
    {
        probRarity1 = rarity;
        _drawService.SetProbRarity1(rarity);
    }

    public void SetProbRarity2(float rarity)
    {
        probRarity2 = rarity;
        _drawService.SetProbRarity2(rarity);
    }

    public void SetProbRarity3(float rarity)
    {
        probRarity3 = rarity;
        _drawService.SetProbRarity3(rarity);
    }

    public void DrawCard(int num)
    {
        Debug.Log($"[DrawCard] 请求抽 {num} 张，当前 cardSet 数量: {cardSet.Count}");
        _drawService.DrawCard(num);
        NotifyDeckOrHandChanged();
    }

    public void ResetPity()
    {
        _drawService.ResetPity();
    }

    public void AddRandomCard(int type, int num, int level)
    {
        _drawService.AddRandomCard(type, num, level, cardDatas);
        NotifyDeckOrHandChanged();
    }

    public void AddRandomCardIfNot(int type, int num, int level)
    {
        _drawService.AddRandomCardIfNot(type, num, level, cardDatas);
        NotifyDeckOrHandChanged();
    }

    // ===================== 数据与牌堆（Repository + Deck） =====================
    /// <summary>
    /// 从 cardDatas 中筛选 ID 最高位（万位）为 1 的卡牌到 giftCards 列表
    /// </summary>
    private void FilterGiftCards()
    {
        giftCards.Clear();
        foreach (var data in cardDatas)
        {
            if (data.id / 10000 == 1)
            {
                giftCards.Add(data);
            }
        }
        Debug.Log($"[CardManager] FilterGiftCards 完成，共筛选到 {giftCards.Count} 张礼物卡。");
    }

    /// <summary>
    /// 从 cardDatas 中筛选 ID 最高位（万位）为 2 的卡牌到 funcCards 列表
    /// </summary>
    private void FilterFuncCards()
    {
        funcCards.Clear();
        foreach (var data in cardDatas)
        {
            if (data.id / 10000 == 2)
            {
                funcCards.Add(data);
            }
        }
        Debug.Log($"[CardManager] FilterFuncCards 完成，共筛选到 {funcCards.Count} 张功能卡。");
    }

    /// <summary>
    /// 从 cardDatas 中筛选 ID 最高位（万位）为 3 的卡牌到 eventCards 列表
    /// </summary>
    private void FilterEventCards()
    {
        eventCards.Clear();
        foreach (var data in cardDatas)
        {
            if (data.id / 10000 == 3)
            {
                eventCards.Add(data);
            }
        }
        Debug.Log($"[CardManager] FilterEventCards 完成，共筛选到 {eventCards.Count} 张事件卡。");
    }

    public void LoadAllCards()
    {
        _repositoryService.LoadAllCards();
    }

    public void ImplementCardSet()
    {
        Debug.Log("准备实现牌堆");
        _deckService.ImplementCardSet();
        NotifyDeckOrHandChanged();
    }

    public void GenCard(CardData data)
    {
        _deckService.GenCard(data);
    }

    public void ShuffleDeck()
    {
        _deckService.ShuffleDeck(_rng);
        NotifyDeckOrHandChanged();
    }

    public CardData GetCardDataById(int id)
    {
        return _repositoryService.GetCardDataById(id);
    }

    public CardData GetRandomCardDataByType(int type)
    {
        return _repositoryService.GetRandomCardDataByType(type);
    }

    public int GetCardCountInDeck(int cardId)
    {
        return _deckService.GetCardCountInDeck(cardId);
    }

    public List<Card> GetCardsInDeckById(int cardId)
    {
        return _deckService.GetCardsInDeckById(cardId);
    }

    public List<List<Card>> GetGiftCardGroupsWithCountGreaterThan(int minCount)
    {
        return _handService.GetGiftCardGroupsWithCountGreaterThan(minCount);
    }

    // ===================== 手牌（Hand） =====================
    public void BreakCard(Card card)
    {
        _handService.BreakCard(card);
        NotifyDeckOrHandChanged();
    }

    public void AddCardInHand(Card card)
    {
        _handService.AddCardInHand(card);
        NotifyDeckOrHandChanged();
    }

    public int GetGiftCardNum()
    {
        return _handService.GetGiftCardNum();
    }

    public void ChangeHandGift(Card ignoredCard = null)
    {
        Debug.Log("[CardManager] Begin Change Hand Gift");
        _handService.ChangeHandGift(GetRandomCardDataByType, ignoredCard);
        NotifyDeckOrHandChanged();
    }

    // ===================== 其他兼容接口 =====================
    public void AddCard(int cardID, int num)
    {
        if (num <= 0) return;

        CardData targetData = GetCardDataById(cardID);
        if (targetData == null)
        {
            Debug.LogError($"[AddCard] 添加失败！未在数据库中找到 ID 为 {cardID} 的卡牌数据。");
            return;
        }

        for (int i = 0; i < num; i++)
        {
            Card newCard = new Card();
            newCard.InitCard(targetData);
            cardInHand.Add(newCard);
            Debug.Log($"[AddCard] 直接生成新卡: {targetData.name} (ID:{cardID}) 并加入手牌。");
        }

        NotifyDeckOrHandChanged();
    }

    private void ApplyInitialHandFromExcel()
    {
        _initialHandService.ApplyInitialHandFromExcel(
            useInitialHandFromExcel,
            initialHandExcelPath,
            cardInHand,
            GetCardDataById
        );
        NotifyDeckOrHandChanged();
    }

    public void NotifyDeckOrHandChanged()
    {
        if (DUEL.Instance != null)
        {
            DUEL.Instance.InitCardObject();
        }
    }

    /// <summary>
    /// 清空手牌与牌堆。游戏失败时调用。
    /// </summary>
    public void ClearAllCards()
    {
        cardInHand.Clear();
        cardSet.Clear();
        NotifyDeckOrHandChanged();
    }

    public void AddCardToSet(Card card)
    {
        _deckService.AddCardToSet(card);
    }

    // ===================== 查询函数（转发 DeckService） =====================
    /// <summary>
    /// 获取牌堆中相同ID卡牌的最大数量
    /// </summary>
    /// <returns>牌堆中出现次数最多的卡牌ID的数量，如果牌堆为空则返回0</returns>
    public int GetMaxSameIdCardCount()
    {
        return _deckService.GetMaxSameIdCardCount();
    }

    /// <summary>
    /// 获取牌堆中出现次数最多的卡牌ID及其数量
    /// </summary>
    /// <param name="cardId">输出出现次数最多的卡牌ID</param>
    /// <param name="count">输出该ID的出现次数</param>
    /// <returns>如果牌堆为空则返回false，否则返回true</returns>
    public bool TryGetMaxSameIdCard(out int cardId, out int count)
    {
        return _deckService.TryGetMaxSameIdCard(out cardId, out count);
    }
}
