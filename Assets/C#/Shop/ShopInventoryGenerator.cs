using System.Collections.Generic;
using UnityEngine;

public sealed class ShopInventoryGenerator
{
    public List<Card> GenerateDailyShopCards(int count)
    {
        List<Card> results = new List<Card>();

        if (CardManager.Instance == null || CardManager.Instance.cardDatas == null || CardManager.Instance.cardDatas.Count < count)
        {
            Debug.LogError($"[ShopInventoryGenerator] 牌库数据异常或不足 {count} 张。");
            return results;
        }

        if (DayManager.Instance == null || DayManager.Instance.daySO == null || DayManager.Instance.daySO.dayDatas == null)
        {
            Debug.LogError("[ShopInventoryGenerator] DayManager 或 daySO 未初始化。");
            return results;
        }

        int dayNum = DayManager.Instance.dayNumber;
        if (dayNum < 0 || dayNum >= DayManager.Instance.daySO.dayDatas.Count)
        {
            Debug.LogError($"[ShopInventoryGenerator] dayNumber 越界: {dayNum}");
            return results;
        }

        var dayData = DayManager.Instance.daySO.dayDatas[dayNum];
        float prob1 = dayData.probRarity1;
        float prob2 = dayData.probRarity2;
        float prob3 = dayData.probRarity3;

        List<CardData> pool1 = new List<CardData>();
        List<CardData> pool2 = new List<CardData>();
        List<CardData> pool3 = new List<CardData>();

        foreach (CardData data in CardManager.Instance.cardDatas)
        {
            int rarity = (data.id / 1000) % 10;
            if (rarity == 1) pool1.Add(data);
            else if (rarity == 2) pool2.Add(data);
            else if (rarity == 3) pool3.Add(data);
        }

        bool pityActive = CardManager.Instance.consecutiveNonGiftCount >= 4;
        CardData forcedGift = null;
        if (pityActive && CardManager.Instance.giftCards != null && CardManager.Instance.giftCards.Count > 0)
        {
            int giftIndex = Random.Range(0, CardManager.Instance.giftCards.Count);
            forcedGift = CardManager.Instance.giftCards[giftIndex];
            pool1.RemoveAll(d => d.id == forcedGift.id);
            pool2.RemoveAll(d => d.id == forcedGift.id);
            pool3.RemoveAll(d => d.id == forcedGift.id);
            CardManager.Instance.consecutiveNonGiftCount = 0;
        }

        for (int i = 0; i < count; i++)
        {
            CardData selectedData = (i == 0 && forcedGift != null)
                ? forcedGift
                : PopWeightedRandom(pool1, pool2, pool3, prob1, prob2, prob3);

            if (selectedData == null)
            {
                continue;
            }

            Card card = new Card();
            card.InitCard(selectedData);
            results.Add(card);
        }

        return results;
    }

    private static CardData PopWeightedRandom(List<CardData> p1, List<CardData> p2, List<CardData> p3, float w1, float w2, float w3)
    {
        float curW1 = p1.Count > 0 ? w1 : 0f;
        float curW2 = p2.Count > 0 ? w2 : 0f;
        float curW3 = p3.Count > 0 ? w3 : 0f;
        float totalWeight = curW1 + curW2 + curW3;

        if (totalWeight <= 0f)
        {
            Debug.LogError("[ShopInventoryGenerator] 符合条件的卡池为空。");
            return null;
        }

        float randomValue = Random.Range(0f, totalWeight);
        List<CardData> selectedPool;

        if (randomValue < curW1) selectedPool = p1;
        else if (randomValue < curW1 + curW2) selectedPool = p2;
        else selectedPool = p3;

        int index = Random.Range(0, selectedPool.Count);
        CardData selectedData = selectedPool[index];
        selectedPool.RemoveAt(index);

        if (selectedData.id / 10000 == 1) CardManager.Instance.consecutiveNonGiftCount = 0;
        else CardManager.Instance.consecutiveNonGiftCount++;

        return selectedData;
    }
}
