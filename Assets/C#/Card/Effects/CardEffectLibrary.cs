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
    public void changeHandGift() => CardManager.Instance.ChangeHandGift();

    public void addNatureAtSumIf(int type1, int type2)
    {
        if (_owner.CallerCard.GetNatureById(type1) > 0) DataManager.Instance.Add(type2, 10);
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
        int sameCount = CardManager.Instance.GetMaxSameIdCardCount();
        beAdded(sameCount >= sameNum ? trueNum : falseNum, 1);
    }

    public void addWithSameTogether(int sameNum, int addNum)
    {
        List<Card> sameCards = CardManager.Instance.GetCardsInDeckById(_owner.CallerCard.id);
        if (sameCards.Count >= sameNum)
        {
            foreach (Card card in sameCards)
            {
                card.Add(1, addNum);
                card.Add(2, addNum);
                card.Add(3, addNum);
            }
            CardManager.Instance.NotifyDeckOrHandChanged();
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
        copyCard.OnAdded();
        CardManager.Instance.AddCardInHand(copyCard);
    }

    public void addCardNatureToDataManager()
    {
        if (_owner.CallerCard == null || _owner.IsConditionFailed(_owner.CallerCard)) return;
        DataManager.Instance.Add(1, _owner.CallerCard.nature1);
        DataManager.Instance.Add(2, _owner.CallerCard.nature2);
        DataManager.Instance.Add(3, _owner.CallerCard.nature3);
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
