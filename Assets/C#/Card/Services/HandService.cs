using System.Collections.Generic;
using UnityEngine;

public class HandService
{
    private readonly List<Card> _cardInHand;
    private readonly List<Card> _cardSet;

    public HandService(List<Card> cardInHand, List<Card> cardSet)
    {
        _cardInHand = cardInHand;
        _cardSet = cardSet;
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
        return num;
    }

    public void ChangeHandGift(System.Func<int, CardData> getCardDataById)
    {
        List<Card> cardsToReturn = _cardInHand.FindAll(c => c.id == 1);
        if (cardsToReturn.Count > 0)
        {
            foreach (Card card in cardsToReturn)
            {
                _cardSet.Add(card);
                _cardInHand.Remove(card);
            }
            Debug.Log($"[HandService] 已将 {cardsToReturn.Count} 张 ID 为 1 的手牌放回牌堆。");
        }

        int indexInSet = _cardSet.FindIndex(c => c.id == 1);
        if (indexInSet != -1)
        {
            Card drawnCard = _cardSet[indexInSet];
            _cardSet.RemoveAt(indexInSet);
            _cardInHand.Add(drawnCard);
            Debug.Log($"[HandService] 已从牌堆重新抽回一张 ID 为 1 的牌: {drawnCard.name}");
        }
        else
        {
            CardData data = getCardDataById(1);
            if (data != null)
            {
                Card newCard = new Card();
                newCard.InitCard(data);
                _cardInHand.Add(newCard);
                Debug.Log("[HandService] 牌堆中无 ID 为 1 的牌，已直接生成一张加入手牌。");
            }
        }
    }
}
