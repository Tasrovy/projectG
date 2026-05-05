using System.Collections.Generic;
using UnityEngine;

public class HandService
{
    private readonly List<Card> _cardInHand;

    public HandService(List<Card> cardInHand)
    {
        _cardInHand = cardInHand;
    }

    public void BreakCard(Card card)
    {
        if (!_cardInHand.Contains(card)) return;
        _cardInHand.Remove(card);
    }

    public void AddCardInHand(Card card)
    {
        _cardInHand.Add(card);
    }

    public int GetGiftCardNum()
    {
        int num = 0;
        foreach (Card card in _cardInHand)
        {
            if (CardIdUtility.GetCardType(card.id) == 1) num++;
        }
        Debug.Log($"[HandService] {num}");
        return num;
    }

    public List<List<Card>> GetGiftCardGroupsWithCountGreaterThan(int minCount)
    {
        Dictionary<int, List<Card>> giftCardGroups = new Dictionary<int, List<Card>>();

        foreach (Card card in _cardInHand)
        {
            if (card.id / 10000 != 1) continue;

            if (!giftCardGroups.TryGetValue(card.id, out List<Card> cards))
            {
                cards = new List<Card>();
                giftCardGroups[card.id] = cards;
            }

            cards.Add(card);
        }

        List<List<Card>> result = new List<List<Card>>();
        foreach (List<Card> cards in giftCardGroups.Values)
        {
            if (cards.Count >= minCount)
            {
                result.Add(cards);
            }
        }

        return result;
    }

    public void ChangeHandGift(System.Func<int, CardData> getRandomCardDataByType, Card ignoredCard = null)
    {
        List<Card> giftCardsInHand = _cardInHand.FindAll(c =>
            CardIdUtility.GetCardType(c.id) == 1 && c != ignoredCard);
        int giftCount = giftCardsInHand.Count;
        if (giftCount <= 0) return;

        foreach (Card card in giftCardsInHand)
        {
            _cardInHand.Remove(card);
        }

        Debug.Log($"[HandService] Destroyed {giftCount} gift cards in hand.");

        CardData data = getRandomCardDataByType(1);
        if (data == null)
        {
            Debug.LogWarning("[HandService] Failed to get random type 1 card data.");
            return;
        }

        for (int i = 0; i < giftCount; i++)
        {
            Card newCard = new Card();
            newCard.InitCard(data);
            _cardInHand.Add(newCard);
        }

        Debug.Log($"[HandService] Generated {giftCount} gift cards from random data: {data.name}.");
    }
}
