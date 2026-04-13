using System.Collections.Generic;

public class DeckService
{
    private readonly List<Card> _cardSet;
    private readonly List<CardData> _cardDatas;

    public DeckService(List<Card> cardSet, List<CardData> cardDatas)
    {
        _cardSet = cardSet;
        _cardDatas = cardDatas;
    }

    public void ImplementCardSet()
    {
        _cardSet.Clear();
        foreach (var data in _cardDatas)
        {
            GenCard(data);
        }
    }

    public void GenCard(CardData data)
    {
        Card card = new Card();
        card.InitCard(data);
        _cardSet.Add(card);
    }

    public void ShuffleDeck(System.Random rng)
    {
        int n = _cardSet.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Card value = _cardSet[k];
            _cardSet[k] = _cardSet[n];
            _cardSet[n] = value;
        }
    }

    public bool TryTakeCardById(int cardId, out Card card)
    {
        int index = _cardSet.FindIndex(c => c.id == cardId);
        if (index < 0)
        {
            card = null;
            return false;
        }

        card = _cardSet[index];
        _cardSet.RemoveAt(index);
        return true;
    }

    public int GetCardCountInDeck(int cardId)
    {
        int count = 0;
        foreach (Card card in _cardSet)
        {
            if (card.id == cardId) count++;
        }
        return count;
    }

    public List<Card> GetCardsInDeckById(int cardId)
    {
        List<Card> result = new List<Card>();
        foreach (Card card in _cardSet)
        {
            if (card.id == cardId) result.Add(card);
        }
        return result;
    }

    public void AddCardToSet(Card card)
    {
        if(!_cardSet.Contains(card)) _cardSet.Add(card);
    }

    /// <summary>
    /// 获取牌堆中相同ID卡牌的最大数量
    /// </summary>
    /// <returns>牌堆中出现次数最多的卡牌ID的数量，如果牌堆为空则返回0</returns>
    public int GetMaxSameIdCardCount()
    {
        if (_cardSet == null || _cardSet.Count == 0)
        {
            return 0;
        }

        int maxCount = 0;
        var idCounts = new Dictionary<int, int>();

        foreach (Card card in _cardSet)
        {
            if (idCounts.ContainsKey(card.id))
            {
                idCounts[card.id]++;
            }
            else
            {
                idCounts[card.id] = 1;
            }

            if (idCounts[card.id] > maxCount)
            {
                maxCount = idCounts[card.id];
            }
        }

        return maxCount;
    }

    /// <summary>
    /// 获取牌堆中出现次数最多的卡牌ID及其数量
    /// </summary>
    /// <param name="cardId">输出出现次数最多的卡牌ID</param>
    /// <param name="count">输出该ID的出现次数</param>
    /// <returns>如果牌堆为空则返回false，否则返回true</returns>
    public bool TryGetMaxSameIdCard(out int cardId, out int count)
    {
        cardId = -1;
        count = 0;

        if (_cardSet == null || _cardSet.Count == 0)
        {
            return false;
        }

        int maxCount = 0;
        int maxId = -1;
        var idCounts = new Dictionary<int, int>();

        foreach (Card card in _cardSet)
        {
            if (idCounts.ContainsKey(card.id))
            {
                idCounts[card.id]++;
            }
            else
            {
                idCounts[card.id] = 1;
            }

            if (idCounts[card.id] > maxCount)
            {
                maxCount = idCounts[card.id];
                maxId = card.id;
            }
        }

        cardId = maxId;
        count = maxCount;
        return true;
    }
}
