using UnityEngine;

public sealed class ShopTransactionService
{
    private readonly float _sellRatio;

    public ShopTransactionService(float sellRatio)
    {
        _sellRatio = Mathf.Max(0f, sellRatio);
    }

    public int GetBuyPrice(Card card)
    {
        return card != null ? Mathf.Max(0, card.sale) : 0;
    }

    public int GetSellPrice(Card card)
    {
        if (card == null)
        {
            return 0;
        }

        return Mathf.Max(0, Mathf.RoundToInt(card.sale * _sellRatio));
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
