﻿using UnityEngine;
using System.Collections.Generic;

public class CardEffect : Singleton<CardEffect>
{
    public Card CallerCard;

    // 防止监听器重复添加的跟踪集合
    private static readonly HashSet<string> registeredChangePropertyListeners = new HashSet<string>();

    // 按卡牌实例记录条件失败状态，避免同 ID 卡互相污染
    private static readonly HashSet<Card> conditionFailedCards = new HashSet<Card>();

    private CardEffectExecutor _executor;
    private CardEffectLibrary _library;
    private CardEffectInvoker _invoker;

    public HashSet<string> RegisteredChangePropertyListeners => registeredChangePropertyListeners;

    protected override bool IsPersistent => true;
    
    protected override void Awake()
    {
        base.Awake();
        EnsureComponents();
        Debug.Log("[CardEffect] Awake: CardEffect实例已创建");
    }

    private void EnsureComponents()
    {
        if (_library == null) _library = new CardEffectLibrary(this);
        if (_invoker == null) _invoker = new CardEffectInvoker(_library);
        if (_executor == null) _executor = new CardEffectExecutor(this);
    }

    public void ExecuteEffectList(Card card, List<EffectCommand> effects)
    {
        EnsureComponents();
        _executor.ExecuteEffectList(card, effects);
    }

    public void OnSelectCardEnd(object obj)
    {
        EnsureComponents();
        _executor.OnSelectCardEnd(obj);
    }

    public void Execute(string methodName, object[] parameters)
    {
        EnsureComponents();
        _invoker.Execute(methodName, parameters);
    }

    public object[] ConvertParameters(string methodName, string[] stringArgs)
    {
        EnsureComponents();
        return _invoker.ConvertParameters(methodName, stringArgs);
    }

    public void addNature(int num, int id)
    {
        EnsureComponents();
        _library.addNature(num, id);
    }

    public void addNatureTo(int num, int id)
    {
        EnsureComponents();
        _library.addNatureTo(num, id);
    }

    public void addNatureAtSum(int num, int id)
    {
        EnsureComponents();
        _library.addNatureAtSum(num, id);
    }

    public void addNatureFromTo(int id1, int id2)
    {
        EnsureComponents();
        _library.addNatureFromTo(id1, id2);
    }

    public void addCard(int cardID, int num)
    {
        EnsureComponents();
        _library.addCard(cardID, num);
    }

    public void addRandomCard(int type, int num, int level)
    {
        EnsureComponents();
        _library.addRandomCard(type, num, level);
    }

    public void addRandomCardIfNot(int type, int num, int level)
    {
        EnsureComponents();
        _library.addRandomCardIfNot(type, num, level);
    }

    public void addMoney(int money)
    {
        EnsureComponents();
        _library.addMoney(money);
    }

    public void changeProperty(float ratio)
    {
        EnsureComponents();
        _library.changeProperty(ratio);
    }

    public void beAdded(int num, int times)
    {
        EnsureComponents();
        _library.beAdded(num, times);
    }

    public void beAddedTo(int num)
    {
        EnsureComponents();
        _library.beAddedTo(num);
    }

    public void beMade(int num)
    {
        EnsureComponents();
        _library.beMade(num);
    }

    public void beBroken(int num)
    {
        EnsureComponents();
        _library.beBroken(num);
    }

    public void addAddNum(int num)
    {
        EnsureComponents();
        _library.addAddNum(num);
    }

    public void changeHandGift()
    {
        EnsureComponents();
        _library.changeHandGift();
    }

    public void addNatureAtSumIf(int type1, int type2, int num)
    {
        EnsureComponents();
        _library.addNatureAtSumIf(type1, type2, num);
    }

    public void addNatureByOther(int type1, int type2)
    {
        EnsureComponents();
        _library.addNatureByOther(type1, type2);
    }

    public void shake(int type, float extent)
    {
        EnsureComponents();
        _library.shake(type, extent);
    }

    public void magnify(float multiple)
    {
        EnsureComponents();
        _library.magnify(multiple);
    }

    public void addWithSame(int sameNum, int trueNum, int falseNum)
    {
        EnsureComponents();
        _library.addWithSame(sameNum, trueNum, falseNum);
    }

    public void addWithSameTogether(int sameNum, int addNum)
    {
        EnsureComponents();
        _library.addWithSameTogether(sameNum, addNum);
    }

    public void drawCard(int num)
    {
        EnsureComponents();
        _library.drawCard(num);
    }

    public void noConsumed()
    {
        EnsureComponents();
        _library.noConsumed();
    }

    public void addCardNatureToDataManager()
    {
        EnsureComponents();
        _library.addCardNatureToDataManager();
    }

    public void _beMadeDirect(int num)
    {
        EnsureComponents();
        _library._beMadeDirect(num);
    }

    public void _beBrokenDirect(int num)
    {
        EnsureComponents();
        _library._beBrokenDirect(num);
    }

    public void _beAddedDirect(int num, int times = 1)
    {
        EnsureComponents();
        _library._beAddedDirect(num, times);
    }

    public void _onTriggerFinalize()
    {
        EnsureComponents();
        _library._onTriggerFinalize();
    }

    public void SetCallerCard(Card card) => CallerCard = card;
    public bool IsWaitingForAsync => _executor != null && _executor.IsWaitingForAsync;
    public bool IsConditionFailed(Card card) => card != null && conditionFailedCards.Contains(card);
    public void MarkConditionFailed(Card card)
    {
        if (card != null) conditionFailedCards.Add(card);
    }

    public void ClearConditionFailed(Card card)
    {
        if (card != null) conditionFailedCards.Remove(card);
    }
}
