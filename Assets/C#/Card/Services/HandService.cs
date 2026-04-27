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
        Debug.Log($"[HandService] {num}");
        return num;
    }

    public void ChangeHandGift(System.Func<int, CardData> getCardDataById, Card ignoredCard = null)
    {
        List<Card> giftCardsInHand = _cardInHand.FindAll(c =>
            CardIdUtility.GetCardType(c.id) == 1 && c != ignoredCard);
        int giftCount = giftCardsInHand.Count;
        if (giftCount <= 0) return;

        foreach (Card card in giftCardsInHand)
        {
            _cardInHand.Remove(card);
        }

        Debug.Log($"[HandService] 已销毁 {giftCount} 张礼物手牌。");

        Card templateCard = _cardSet.Find(c => CardIdUtility.GetCardType(c.id) == 1);
        if (templateCard != null)
        {
            for (int i = 0; i < giftCount; i++)
            {
                Card newCard = new Card();
                newCard.InitCard(templateCard);
                _cardInHand.Add(newCard);
            }

            Debug.Log($"[HandService] 已根据牌堆中的礼物样本复制 {giftCount} 张加入手牌: {templateCard.name}");
            return;
        }

        CardData data = getCardDataById(1);
        if (data != null)
        {
            for (int i = 0; i < giftCount; i++)
            {
                Card newCard = new Card();
                newCard.InitCard(data);
                _cardInHand.Add(newCard);
            }

            Debug.Log($"[HandService] 牌堆中无礼物牌，已直接生成 {giftCount} 张礼物加入手牌。");
        }
    }
}
