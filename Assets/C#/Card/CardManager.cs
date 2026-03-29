using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Data;
using ExcelDataReader;
using System.Linq; // 引入 Linq 方便查询
using Random = System.Random;

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

    public void SetProbRarity1(float rarity)
    {
        probRarity1 = rarity;
    }

    public void SetProbRarity2(float rarity)
    {
        probRarity2 = rarity;
    }

    public void SetProbRarity3(float rarity)
    {
        probRarity3 = rarity;
    }
    
    protected override void Awake()
    {
        base.Awake();
        LoadAllCards();
        ImplementCardSet();
        Debug.Log($"准备抽取{5}张牌");
        DrawCard(5);
    }

    private void Start()
    {
        DUEL.Instance.Begin();
    }

    public void ImplementCardSet()
    {
        Debug.Log("准备实现牌堆");
        foreach (var data in cardDatas)
        {
            GenCard(data);
        }

        NotifyDeckOrHandChanged();
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
            if (targetIndex < 0 || targetIndex >= cardSet.Count)
            {
                Debug.LogWarning($"[DrawCard] 无效的卡牌索引: {targetIndex}, 牌堆数量: {cardSet.Count}");
                break;
            }

            // 4. 执行抽取
            Card drawnCard = cardSet[targetIndex];
            cardSet.RemoveAt(targetIndex);

            // 5. 更新状态
            UpdatePityCounters(GetCardType(drawnCard.id));
            cardInHand.Add(drawnCard);

            Debug.Log($"[抽牌] 抽到:{drawnCard.name} | ID:{drawnCard.id} | 类型:{GetCardType(drawnCard.id)} | 稀有度:{GetCardRarity(drawnCard.id)}");
        }

        NotifyDeckOrHandChanged();
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
        // 安全检查：牌堆为空时直接返回-1
        if (cardSet.Count == 0)
        {
            Debug.LogWarning("[SelectBestCardIndex] 牌堆为空，无法选择卡牌索引");
            return -1;
        }

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

    public void BreakCard(Card card)
    {
        //card.
        if(!cardInHand.Contains(card)) return;
        cardInHand.Remove(card);
        NotifyDeckOrHandChanged();
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

        NotifyDeckOrHandChanged();
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

    public void AddCard(int cardID, int num)
    {
        if (num <= 0) return;

        // 1. 先获取该 ID 对应的基础数据，以防需要生成新卡
        CardData targetData = GetCardDataById(cardID);
        if (targetData == null)
        {
            Debug.LogError($"[AddCard] 添加失败！未在数据库中找到 ID 为 {cardID} 的卡牌数据。");
            return;
        }

        // 2. 循环执行 num 次添加操作
        for (int i = 0; i < num; i++)
        {
            // 在牌堆中查找是否有该 ID 的卡牌 (获取第一个匹配的索引)
            int indexInSet = cardSet.FindIndex(c => c.id == cardID);

            if (indexInSet >= 0)
            {
                // 情况 A：牌堆中存在该卡牌
                Card existCard = cardSet[indexInSet];
                cardSet.RemoveAt(indexInSet); // 从牌堆中移除
                cardInHand.Add(existCard); // 加入手牌

                Debug.Log($"[AddCard] 从牌堆中检出并添加: {existCard.name} (ID:{cardID}) 到手牌。");
            }
            else
            {
                // 情况 B：牌堆中不存在或数量已不足，直接印制（生成）新卡
                Card newCard = new Card();
                newCard.InitCard(targetData); // 使用基础数据初始化
                cardInHand.Add(newCard); // 直接加入手牌 (注意：这里不进牌堆)

                Debug.Log($"[AddCard] 牌堆数量不足，已印制新卡: {targetData.name} (ID:{cardID}) 并加入手牌。");
            }
        }

        NotifyDeckOrHandChanged();
    }
    
    /// <summary>
    /// 随机获得指定类型和稀有度的卡牌加入手牌
    /// 优先从牌堆检索，不足时从数据库随机生成
    /// </summary>
    /// <param name="type">卡牌类型：0为任意类型，1/2/3为指定类型</param>
    /// <param name="num">需要获取的数量</param>
    /// <param name="level">卡牌稀有度 (对应1,2,3)</param>
    public void AddRandomCard(int type, int num, int level)
    {
        if (num <= 0) return;

        // 预处理：先从全局图鉴 (cardDatas) 中筛选出符合条件的"备用蓝图池"
        // 这样如果牌堆不够了，我们知道可以印哪些牌
        List<CardData> validDataPool = cardDatas.Where(d => 
            (type == 0 || GetCardType(d.id) == type) && 
            GetCardRarity(d.id) == level
        ).ToList();

        // 安全校验：如果整个游戏数据库里都没有这种组合的牌，直接报错返回，防止死循环或报错
        if (validDataPool.Count == 0)
        {
            Debug.LogError($"[AddRandomCard] 错误！数据库中不存在 Type={type}, Level={level} 的卡牌。");
            return;
        }

        // 循环执行 num 次抽取/生成
        for (int i = 0; i < num; i++)
        {
            // 1. 每次抽取前，动态扫描当前牌堆中符合条件的卡牌
            List<Card> validCardsInSet = cardSet.Where(c => 
                (type == 0 || GetCardType(c.id) == type) && 
                GetCardRarity(c.id) == level
            ).ToList();

            if (validCardsInSet.Count > 0)
            {
                // 情况 A：牌堆里有符合条件的牌，随机抽走一张
                int randomIndex = rng.Next(validCardsInSet.Count);
                Card selectedCard = validCardsInSet[randomIndex];
                
                // 从牌堆移除，加入手牌
                cardSet.Remove(selectedCard);
                cardInHand.Add(selectedCard);
                
                Debug.Log($"[AddRandomCard] 从牌堆随机抽取了: {selectedCard.name} (ID:{selectedCard.id}) 加入手牌。");
            }
            else
            {
                // 情况 B：牌堆里没有符合条件的牌了，使用"备用蓝图池"随机印一张新卡
                int randomIndex = rng.Next(validDataPool.Count);
                CardData selectedData = validDataPool[randomIndex];

                Card newCard = new Card();
                newCard.InitCard(selectedData);
                cardInHand.Add(newCard); // 生成的新卡直接进手牌，不进牌堆

                Debug.Log($"[AddRandomCard] 牌堆条件卡不足，随机生成新卡: {selectedData.name} (ID:{selectedData.id}) 加入手牌。");
            }
        }

        NotifyDeckOrHandChanged();
    }
    
    /// <summary>
    /// 条件触发：如果手牌中没有指定类型的卡牌，则随机获得该类型/稀有度的卡牌
    /// </summary>
    /// <param name="type">要检查并获取的卡牌类型（0表示任意类型）</param>
    /// <param name="num">获取数量</param>
    /// <param name="level">卡牌稀有度</param>
    public void AddRandomCardIfNot(int type, int num, int level)
    {
        // 1. 检查手牌中是否已经拥有该类型的卡牌
        // 如果 type == 0，表示只要手牌里有任意牌就算"拥有"；否则严格匹配类型
        bool hasCardOfType = cardInHand.Any(c => type == 0 ? true : GetCardType(c.id) == type);

        // 2. 如果手牌里没有该类型的牌，执行添加逻辑
        if (!hasCardOfType)
        {
            Debug.Log($"[addRandomCardIfNot] 触发！手牌中缺乏类型为 {type} 的卡牌，准备添加 {num} 张。");
            
            // 直接复用刚才写好的随机抽卡/生成方法
            AddRandomCard(type, num, level);
        }
        else
        {
            Debug.Log($"[addRandomCardIfNot] 跳过。手牌中已经存在类型为 {type} 的卡牌。");
        }
    }

    public void ChangeHandGift()
    {
        // --- 第一部分：将手牌中所有 ID 为 1 的牌放回牌堆 ---
        
        // 1. 找出所有 ID 为 1 的手牌
        List<Card> cardsToReturn = cardInHand.FindAll(c => c.id == 1);

        if (cardsToReturn.Count > 0)
        {
            foreach (Card card in cardsToReturn)
            {
                cardSet.Add(card);       // 放回牌堆
                cardInHand.Remove(card);  // 从手牌移除
            }
            Debug.Log($"[ChangeHandGift] 已将 {cardsToReturn.Count} 张 ID 为 1 的手牌放回牌堆。");
            
            // 可选：放回牌堆后是否需要洗牌？如果需要可以调用你之前的 ShuffleDeck();
            // ShuffleDeck(); 
        }

        // --- 第二部分：从牌堆重新抽一张 ID 为 1 的牌到手牌 ---

        // 2. 在牌堆中寻找 ID 为 1 的牌
        int indexInSet = cardSet.FindIndex(c => c.id == 1);

        if (indexInSet != -1)
        {
            // 如果牌堆里有（包括刚刚放回去的那些），抽出一张
            Card drawnCard = cardSet[indexInSet];
            cardSet.RemoveAt(indexInSet);
            cardInHand.Add(drawnCard);
            Debug.Log($"[ChangeHandGift] 已从牌堆重新抽回一张 ID 为 1 的牌: {drawnCard.name}");
        }
        else
        {
            // 万一牌堆里彻底没有 ID 为 1 的牌了，直接生成一张
            CardData data = GetCardDataById(1);
            if (data != null)
            {
                Card newCard = new Card();
                newCard.InitCard(data);
                cardInHand.Add(newCard);
                Debug.Log("[ChangeHandGift] 牌堆中无 ID 为 1 的牌，已直接生成一张加入手牌。");
            }
        }

        NotifyDeckOrHandChanged();
    }
    
    public void AddCardInHand(Card card)
    {
        cardInHand.Add(card);
        NotifyDeckOrHandChanged();
    }

    public int GetGiftCardNum()
    {
        int num = 0;
        foreach (Card card in cardInHand)
        {
            if (GetCardType(card.id) == 1)
                num++;
        }
        return num;
    }

    /// <summary>
    /// 获取牌堆中指定卡牌ID的数量
    /// </summary>
    /// <param name="cardId">卡牌ID</param>
    /// <returns>牌堆中该卡牌的数量</returns>
    public int GetCardCountInDeck(int cardId)
    {
        int count = 0;
        foreach (Card card in cardSet)
        {
            if (card.id == cardId)
                count++;
        }
        return count;
    }

    /// <summary>
    /// 获取牌堆中指定卡牌ID的所有卡牌列表
    /// </summary>
    /// <param name="cardId">卡牌ID</param>
    /// <returns>牌堆中该卡牌的所有实例列表</returns>
    public List<Card> GetCardsInDeckById(int cardId)
    {
        List<Card> result = new List<Card>();
        foreach (Card card in cardSet)
        {
            if (card.id == cardId)
                result.Add(card);
        }
        return result;
    }

    /// <summary>
    /// 通知牌堆或手牌发生变化
    /// </summary>
    public void NotifyDeckOrHandChanged()
    {
        // 直接调用DUEL刷新UI，使用yjtc模块原有功能
        if (DUEL.Instance != null)
        {
            DUEL.Instance.InitCardObject();
        }
    }
}