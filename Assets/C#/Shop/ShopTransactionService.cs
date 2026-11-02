using UnityEngine;

public sealed class ShopTransactionService
{
    public ShopTransactionService()
    {
    }

    public int GetBuyPrice(Card card)
    {
        return card != null ? Mathf.Max(0, card.sale) : 0;
    }

    public int GetSellPrice(Card card)
    {
        return card != null ? Mathf.Max(0, card.sell) : 0;
    }

    public bool TryBuy(Card sourceCard)
    {
        if (sourceCard == null || CardManager.Instance == null || DataManager.Instance == null)
        {
            return false;
        }

        int price = GetBuyPrice(sourceCard);
        if (DataManager.Instance.MoneyNum < price)
        {
            return false;
        }

        Card purchasedCard = new Card();
        purchasedCard.InitCard(sourceCard);

        DataManager.Instance.Add(4, -price);
        CardManager.Instance.AddCardInHand(purchasedCard);
        return true;
    }

    public bool TrySell(Card card)
    {
        if (card == null || CardManager.Instance == null || DataManager.Instance == null)
        {
            return false;
        }

        if (!CardManager.Instance.cardInHand.Contains(card))
        {
            return false;
        }

        CardManager.Instance.cardInHand.Remove(card);
        CardManager.Instance.NotifyDeckOrHandChanged();
        DataManager.Instance.Add(4, GetSellPrice(card));
        return true;
    }
}
