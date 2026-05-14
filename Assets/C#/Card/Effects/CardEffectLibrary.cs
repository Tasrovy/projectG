using System.Collections.Generic;
using UnityEngine;

public sealed class CardEffectLibrary
{
    private readonly CardEffect _owner;

    public CardEffectLibrary(CardEffect owner)
    {
        _owner = owner;
    }

    public void addNature(int num, int id) => _owner.CallerCard.Add(id, num);
    public void addNatureTo(int num, int id) => _owner.CallerCard.AddTo(id, num);
    public void addNatureAtSum(int num, int id) => DataManager.Instance.Add(id, num);
    public void addNatureFromTo(int id1, int id2) => DataManager.Instance.AddNatureFromTo(id1, id2);
    public void addCard(int cardID, int num) => CardManager.Instance.AddCard(cardID, num);
    public void addRandomCard(int type, int num, int level) => CardManager.Instance.AddRandomCard(type, num, level);
    public void addRandomCardIfNot(int type, int num, int level) => CardManager.Instance.AddRandomCardIfNot(type, num, level);
    public void addMoney(int money) => DataManager.Instance.Add(4, money);

    public void triggerEventDialogue(string str)
    {
        DUEL.Instance.End();
        DialogueHandler.Instance.TriggerEventDialogue(str);
    }
    
    public void changeProperty(float ratio)
    {
        if (_owner.CallerCard == null) return;
        string listenerKey = $"{_owner.CallerCard.id}_{ratio}";
        if (_owner.RegisteredChangePropertyListeners.Contains(listenerKey)) return;

        _owner.RegisteredChangePropertyListeners.Add(listenerKey);
        DayManager.Instance.GetNextDayEvent().AddListener(() =>
        {
            DataManager.Instance.SetNature1Effect(ratio - 1);
            DataManager.Instance.SetNature2Effect(ratio - 1);
            DataManager.Instance.SetNature3Effect(ratio - 1);
        });
    }

    public void beAdded(int num, int times)
    {
        if (num == 0 || times == 0) return;
        CardSubmitHelper.Instance.ShengZhang(num, times);
    }

    public void beAddedTo(int num)
    {
        if (num == 0) return;
        CardSubmitHelper.Instance.SetNum(num);
        CardSubmitHelper.Instance.ShengZhangTo(num);
    }

    public void beMade(int num)
    {
        if (num == 0) return;
        CardSubmitHelper.Instance.SetNum(num);
        CardSubmitHelper.Instance.ShengZhi();
    }

    public void beBroken(int num)
    {
        if (num == 0) return;
        CardSubmitHelper.Instance.SetNum(num);
        CardSubmitHelper.Instance.JianZhi();
    }

    public void addAddNum(int num) => _owner.CallerCard?.TryModifyAddedValue(num);
    public void changeHandGift() => CardManager.Instance.ChangeHandGift(_owner.CallerCard);

    public void addNatureAtSumIf(int type1, int type2, int num)
    {
        if (_owner.CallerCard == null || num == 0) return;
        if (CardIdUtility.GetCardType(_owner.CallerCard.id) != 2) return;
        CardSubmitHelper.Instance.AddNatureAtSumIf(type1, type2, num);
    }

    public void addCardSale(int num)
    {
        if (_owner.CallerCard == null || num == 0) return;
        CardSubmitHelper.Instance.AddCardSale(num);
    }

    public void addNatureByOther(int type1, int type2)
    {
        int addValue = DataManager.Instance.GetNatureById(type1) / 2;
        DataManager.Instance.Add(type2, addValue);
    }

    public void shake(int type, float extent)
    {
        BattleDialogController.Instance.Shake(type, extent);
    }

    public void magnify(float multiple)
    {
        BattleDialogController.Instance.Magnify(multiple);
    }

    public void addWithSame(int sameNum, int trueNum, int falseNum)
    {
        CardActionResolver.Instance.StartEffectSelection(
            onConfirm: (selectedCard) =>
            {
                int sameCount = 0;
                foreach (Card card in CardManager.Instance.cardInHand)
                {
                    if (card.id == selectedCard.id)
                    {
                        sameCount++;
                    }
                }

                int addNum = sameCount >= sameNum ? trueNum : falseNum;
                CardSubmitHelper.Instance.ShengZhang(selectedCard, addNum, 1);

                CardActionResolver.Instance.CompletePendingPlayedCard(true);
                if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(true);
            },
            onCancel: () =>
            {
                CardSubmitHelper.Instance.RestoreCallerCardOnInvalidTarget();
                if (CardEffect.Instance != null) CardEffect.Instance.OnSelectCardEnd(false);
            }
        );
    }

    public void addWithSameTogether(int sameNum, int addNum)
    {
        List<List<Card>> giftCardGroups = CardManager.Instance.GetGiftCardGroupsWithCountGreaterThan(sameNum);
        foreach (List<Card> giftCards in giftCardGroups)
        {
            foreach (Card card in giftCards)
            {
                CardSubmitHelper.Instance.ShengZhang(card, addNum, 1);
            }
        }
    }

    public void drawCard(int num)
    {
        CardManager.Instance.DrawCard(num);
    }

    public void noConsumed()
    {
        Card copyCard = new Card();
        copyCard.InitCard(_owner.CallerCard);
        CardManager.Instance.AddCardInHand(copyCard);
    }

    public void addCardNatureToDataManager()
    {
        if (_owner.CallerCard == null || _owner.IsConditionFailed(_owner.CallerCard)) return;
        DataManager.Instance.Add(1, _owner.CallerCard.nature1);
        DataManager.Instance.Add(2, _owner.CallerCard.nature2);
        DataManager.Instance.Add(3, _owner.CallerCard.nature3);
    }

    public void addCharm(int num)
    {
        DataManager.Instance.Add(5,num);
    }

    public void _beMadeDirect(int num)
    {
        if (_owner.CallerCard == null || num == 0) return;
        CardSubmitHelper.Instance.SetNum(num);
        CardSubmitHelper.Instance.ShengZhi();
    }

    public void _beBrokenDirect(int num)
    {
        if (_owner.CallerCard == null || num == 0) return;
        CardSubmitHelper.Instance.SetNum(num);
        CardSubmitHelper.Instance.JianZhi();
    }

    public void _beAddedDirect(int num, int times = 1)
    {
        if (_owner.CallerCard == null || num == 0 || times == 0) return;
        CardSubmitHelper.Instance.ShengZhang(num, times);
    }

    public void _onTriggerFinalize()
    {
        if (_owner.CallerCard == null || _owner.IsConditionFailed(_owner.CallerCard)) return;

        addCardNatureToDataManager();
        DayManager.Instance.GetNextDayEvent().AddListener(_owner.CallerCard.OnNextTurn);
    }
}
