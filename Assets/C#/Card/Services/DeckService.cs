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
}
