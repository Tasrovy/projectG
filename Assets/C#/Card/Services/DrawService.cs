using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DrawService
{
    private readonly List<Card> _cardSet;
    private readonly List<Card> _cardInHand;
    private readonly System.Random _rng;
    private readonly Dictionary<int, int> _pityCounters = new Dictionary<int, int>()
    {
        { 1, 0 }, { 2, 0 }, { 3, 0 }
    };

    public float ProbRarity1 { get; private set; }
    public float ProbRarity2 { get; private set; }
    public float ProbRarity3 { get; private set; }

    public DrawService(List<Card> cardSet, List<Card> cardInHand, System.Random rng, float probRarity1, float probRarity2, float probRarity3)
    {
        _cardSet = cardSet;
        _cardInHand = cardInHand;
        _rng = rng;
        ProbRarity1 = probRarity1;
        ProbRarity2 = probRarity2;
        ProbRarity3 = probRarity3;
    }

    // 对外公开：设置稀有度概率
    public void SetProbRarity1(float rarity) => ProbRarity1 = rarity;
    public void SetProbRarity2(float rarity) => ProbRarity2 = rarity;
    public void SetProbRarity3(float rarity) => ProbRarity3 = rarity;

    // 对外公开：重置保底
    public void ResetPity()
    {
        _pityCounters[1] = 0;
        _pityCounters[2] = 0;
        _pityCounters[3] = 0;
    }

    // 对外公开：常规抽牌
    public void DrawCard(int num)
    {
        for (int i = 0; i < num; i++)
        {
            if (_cardSet.Count <= 0) break;

            int forcedType = -1;
            foreach (var kvp in _pityCounters)
            {
                if (kvp.Value >= 3)
                {
                    forcedType = kvp.Key;
                    break;
                }
            }

            int rolledRarity = RollRarity();
            int targetIndex = SelectBestCardIndex(forcedType, rolledRarity);
            if (targetIndex < 0 || targetIndex >= _cardSet.Count)
            {
                Debug.LogWarning($"[DrawService] 无效的卡牌索引: {targetIndex}, 牌堆数量: {_cardSet.Count}");
                break;
            }

            Card drawnCard = _cardSet[targetIndex];
            _cardSet.RemoveAt(targetIndex);
            UpdatePityCounters(CardIdUtility.GetCardType(drawnCard.id));
            _cardInHand.Add(drawnCard);

            Debug.Log($"[抽牌] 抽到:{drawnCard.name} | ID:{drawnCard.id} | 类型:{CardIdUtility.GetCardType(drawnCard.id)} | 稀有度:{CardIdUtility.GetCardRarity(drawnCard.id)}");
        }
    }

    // 对外公开：随机获得指定类型/稀有度卡牌
    public void AddRandomCard(int type, int num, int level, List<CardData> cardDatas)
    {
        if (num <= 0) return;

        List<CardData> validDataPool = cardDatas.Where(d =>
            (type == 0 || CardIdUtility.GetCardType(d.id) == type) &&
            CardIdUtility.GetCardRarity(d.id) == level
        ).ToList();

        if (validDataPool.Count == 0)
        {
            Debug.LogError($"[DrawService] 错误！数据库中不存在 Type={type}, Level={level} 的卡牌。");
            return;
        }

        for (int i = 0; i < num; i++)
        {
            List<Card> validCardsInSet = _cardSet.Where(c =>
                (type == 0 || CardIdUtility.GetCardType(c.id) == type) &&
                CardIdUtility.GetCardRarity(c.id) == level
            ).ToList();

            if (validCardsInSet.Count > 0)
            {
                int randomIndex = _rng.Next(validCardsInSet.Count);
                Card selectedCard = validCardsInSet[randomIndex];
                _cardSet.Remove(selectedCard);
                _cardInHand.Add(selectedCard);
                Debug.Log($"[DrawService] 从牌堆随机抽取了: {selectedCard.name} (ID:{selectedCard.id}) 加入手牌。");
            }
            else
            {
                int randomIndex = _rng.Next(validDataPool.Count);
                CardData selectedData = validDataPool[randomIndex];

                Card newCard = new Card();
                newCard.InitCard(selectedData);
                _cardInHand.Add(newCard);
                Debug.Log($"[DrawService] 牌堆条件卡不足，随机生成新卡: {selectedData.name} (ID:{selectedData.id}) 加入手牌。");
            }
        }
    }

    // 对外公开：若手牌无某类型，则补随机卡
    public void AddRandomCardIfNot(int type, int num, int level, List<CardData> cardDatas)
    {
        bool hasCardOfType = _cardInHand.Any(c => type == 0 || CardIdUtility.GetCardType(c.id) == type);
        if (!hasCardOfType)
        {
            Debug.Log($"[DrawService] addRandomCardIfNot 触发！手牌中缺乏类型为 {type} 的卡牌，准备添加 {num} 张。");
            AddRandomCard(type, num, level, cardDatas);
        }
        else
        {
            Debug.Log($"[DrawService] addRandomCardIfNot 跳过。手牌中已经存在类型为 {type} 的卡牌。");
        }
    }

    private int RollRarity()
    {
        float total = ProbRarity1 + ProbRarity2 + ProbRarity3;
        double diceRoll = _rng.NextDouble() * total;

        if (diceRoll < ProbRarity1) return 1;
        if (diceRoll < ProbRarity1 + ProbRarity2) return 2;
        return 3;
    }

    private int SelectBestCardIndex(int forcedType, int targetRarity)
    {
        if (_cardSet.Count == 0)
        {
            Debug.LogWarning("[DrawService] 牌堆为空，无法选择卡牌索引");
            return -1;
        }

        List<Card> candidates = (forcedType != -1)
            ? _cardSet.Where(c => CardIdUtility.GetCardType(c.id) == forcedType).ToList()
            : _cardSet;

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[DrawService] 保底类型已在牌堆中枯竭，从全堆抽取。");
            candidates = _cardSet;
        }

        var matchRarity = candidates.Where(c => CardIdUtility.GetCardRarity(c.id) == targetRarity).ToList();

        Card finalChoice = matchRarity.Count > 0
            ? matchRarity[_rng.Next(matchRarity.Count)]
            : candidates[_rng.Next(candidates.Count)];

        return _cardSet.IndexOf(finalChoice);
    }

    private void UpdatePityCounters(int drawnType)
    {
        int[] monitored = { 1, 2, 3 };
        foreach (int t in monitored)
        {
            if (t == drawnType) _pityCounters[t] = 0;
            else _pityCounters[t]++;
        }
    }
}
