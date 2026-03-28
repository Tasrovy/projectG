using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Data;
using ExcelDataReader;
using System.Linq; // 引入 Linq 方便查询
using Random = System.Random;

[System.Serializable]
public class CardData
{
    public int id;
    public int nature1;
    public int nature2;
    public int nature3;
    public string name;
    public int sale;
    public string description;
    public string buff;
    public string trigger;
}

// --- 卡牌管理器 ---
public class CardManager : Singleton<CardManager>
{
    public List<CardData> cardDatas = new List<CardData>();
    public List<Card> cardSet = new List<Card>();
    public List<Card> cardInHand = new List<Card>();

    [Header("动态稀有度概率 (和需为 1.0)")]
    [Range(0, 1)] public float probRarity1 = 0.7f; // 稀有度 1 的概率
    [Range(0, 1)] public float probRarity2 = 0.2f; // 稀有度 2 的概率
    [Range(0, 1)] public float probRarity3 = 0.1f; // 稀有度 3 的概率

    [Header("Excel配置")]
    public List<string> cardExcelPaths = new List<string> { "Cards.xlsx" };

    private Random rng = new Random();

    // 保底计数器：Key为类型(ID第一位1,2,3)
    private Dictionary<int, int> pityCounters = new Dictionary<int, int>() { { 1, 0 }, { 2, 0 }, { 3, 0 } };

    protected override void Awake()
    {
        base.Awake();
        LoadAllCards();
        ImplementCardSet();
        Debug.Log($"准备抽取{5}张牌");
        DrawCard(5);
        DUEL.Instance.Begin();
    }

    public void ImplementCardSet()
    {
        Debug.Log("准备实现牌堆");
        foreach (var data in cardDatas)
        {
            GenCard(data);
        }
    }
    
    // 获取 ID 第一位：类型
    private int GetCardType(int id)
    {
        string s = Math.Abs(id).ToString();
        return s.Length >= 1 ? int.Parse(s[0].ToString()) : 0;
    }

    // 获取 ID 第二位：稀有度
    private int GetCardRarity(int id)
    {
        string s = Math.Abs(id).ToString();
        return s.Length >= 2 ? int.Parse(s[1].ToString()) : 0;
    }

    /// <summary>
    /// 核心抽牌逻辑：综合保底(类型)与概率(稀有度)
    /// </summary>
    public void DrawCard(int num)
    {
        for (int i = 0; i < num; i++)
        {
            if (cardSet.Count <= 0) break;

            // 1. 判定保底类型 (ID第一位)
            int forcedType = -1;
            foreach (var kvp in pityCounters)
            {
                if (kvp.Value >= 3) { forcedType = kvp.Key; break; }
            }

            // 2. 判定本次抽取的稀有度 (基于权重掷点)
            int rolledRarity = RollRarity();

            // 3. 从牌堆中筛选候选牌
            int targetIndex = SelectBestCardIndex(forcedType, rolledRarity);

            // 4. 执行抽取
            Card drawnCard = cardSet[targetIndex];
            cardSet.RemoveAt(targetIndex);

            // 5. 更新状态
            UpdatePityCounters(GetCardType(drawnCard.id));
            cardInHand.Add(drawnCard);

            Debug.Log($"[抽牌] 抽到:{drawnCard.name} | ID:{drawnCard.id} | 类型:{GetCardType(drawnCard.id)} | 稀有度:{GetCardRarity(drawnCard.id)}");
        }
    }

    /// <summary>
    /// 根据设定的概率随机决定稀有度
    /// </summary>
    private int RollRarity()
    {
        float total = probRarity1 + probRarity2 + probRarity3;
        double diceRoll = rng.NextDouble() * total;

        if (diceRoll < probRarity1) return 1;
        if (diceRoll < probRarity1 + probRarity2) return 2;
        return 3;
    }

    /// <summary>
    /// 在牌堆中寻找最符合条件的牌索引
    /// </summary>
    private int SelectBestCardIndex(int forcedType, int targetRarity)
    {
        // 首先筛选出符合保底类型的牌（如果没触发保底，则看全量牌堆）
        List<Card> candidates = (forcedType != -1) 
            ? cardSet.Where(c => GetCardType(c.id) == forcedType).ToList() 
            : cardSet;

        if (candidates.Count == 0)
        {
            Debug.LogWarning("保底类型已在牌堆中枯竭，从全堆抽取。");
            candidates = cardSet;
        }

        // 在候选牌中寻找匹配稀有度的牌
        var matchRarity = candidates.Where(c => GetCardRarity(c.id) == targetRarity).ToList();

        Card finalChoice;
        if (matchRarity.Count > 0)
        {
            // 匹配到了目标稀有度，随机取一张
            finalChoice = matchRarity[rng.Next(matchRarity.Count)];
        }
        else
        {
            // 没匹配到（比如该稀有度抽光了），则从候选牌中随机取一张
            Debug.Log("目标稀有度无匹配，随机抽取候选牌。");
            finalChoice = candidates[rng.Next(candidates.Count)];
        }

        return cardSet.IndexOf(finalChoice);
    }

    private void UpdatePityCounters(int drawnType)
    {
        int[] monitored = { 1, 2, 3 };
        foreach (int t in monitored)
        {
            if (t == drawnType) pityCounters[t] = 0;
            else pityCounters[t]++;
        }
    }

    public void ResetPity()
    {
        pityCounters[1] = 0; pityCounters[2] = 0; pityCounters[3] = 0;
    }

    // --- 以下为原有基础功能 ---

    public void ShuffleDeck()
    {
        int n = cardSet.Count;
        while (n > 1) {
            n--;
            int k = rng.Next(n + 1);
            Card value = cardSet[k];
            cardSet[k] = cardSet[n];
            cardSet[n] = value;
        }
    }

    public void LoadAllCards()
    {
        // 2. 【修改点】在循环外清空数据，防止重复加载
        cardDatas.Clear();

        // 3. 【修改点】遍历所有的 Excel 文件路径
        foreach (string path in cardExcelPaths)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;

            CardDatabaseSO databaseSO = ExcelLoader.Instance.ReadExcel(path);

            // 判空保护：注意这里使用 continue 而不是 return，如果某张表读取失败，它会继续读取下一张表
            if (databaseSO == null || databaseSO.allCards.Count == 0) 
            {
                Debug.LogWarning($"[CardManager] 无法加载数据或数据为空，已跳过文件: {path}");
                continue; 
            }

            // 将当前表格中的所有卡牌追加到全局列表中
            cardDatas.AddRange(databaseSO.allCards);
        }

        Debug.Log($"所有表格加载完毕，共加载了 {cardDatas.Count} 张卡牌数据。");
    }

    public void GenCard(CardData data)
    {
        Card card = new Card();
        card.InitCard(data);
        cardSet.Add(card);
    }
    
    public CardData GetCardDataById(int id) => cardDatas.Find(c => c.id == id);
}