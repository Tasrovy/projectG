using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

public class CardEffect : Singleton<CardEffect>
{
    public Card CallerCard;

    // 效果链执行队列
    private Queue<(Card card, List<EffectCommand> effects)> effectChainQueue = new Queue<(Card, List<EffectCommand>)>();
    private bool isExecutingChain = false;
    private Card currentChainCard;
    private List<EffectCommand> currentChainEffects;
    private int currentEffectIndex;
    private bool waitingForAsync = false;

    // 防止监听器重复添加的跟踪集合
    private static HashSet<string> registeredChangePropertyListeners = new HashSet<string>();

    // 记录条件失败的卡牌ID
    private static HashSet<int> conditionFailedCardIds = new HashSet<int>();

    protected override bool IsPersistent => true;
    
    protected override void Awake()
    {
        base.Awake();
        Debug.Log("[CardEffect] Awake: CardEffect实例已创建");
    }

    /// <summary>
    /// 执行效果链（外部调用入口）
    /// </summary>
    public void ExecuteEffectList(Card card, List<EffectCommand> effects)
    {
        if (effects == null || effects.Count == 0) return;

        effectChainQueue.Enqueue((card, effects));

        if (!isExecutingChain)
        {
            StartExecutingNextChain();
        }
    }

    /// <summary>
    /// 开始执行下一个效果链
    /// </summary>
    private void StartExecutingNextChain()
    {
        if (effectChainQueue.Count == 0)
        {
            isExecutingChain = false;
            return;
        }

        var (card, effects) = effectChainQueue.Dequeue();
        currentChainCard = card;
        currentChainEffects = effects;
        currentEffectIndex = 0;
        isExecutingChain = true;
        waitingForAsync = false;

        ExecuteNextEffect();
    }

    /// <summary>
    /// 执行下一个效果
    /// </summary>
    private void ExecuteNextEffect()
    {
        // 如果正在等待玩家选牌，则暂停执行
        if (waitingForAsync) return;

        // 检查是否所有效果都已执行完毕
        if (currentEffectIndex >= currentChainEffects.Count)
        {
            // 当前链执行完毕，开始下一个链
            StartExecutingNextChain();
            return;
        }

        var effect = currentChainEffects[currentEffectIndex];
        currentEffectIndex++;

        // 设置当前调用者卡牌
        SetCallerCard(currentChainCard);

        // 执行单个效果
        ExecuteSingleEffect(effect);
    }

    /// <summary>
    /// 执行单个效果，处理UI等待逻辑
    /// </summary>
    private void ExecuteSingleEffect(EffectCommand effect)
    {
        // 检查需要等待玩家选择的特殊卡牌效果
        if (effect.methodName == "beMade" || effect.methodName == "_beMadeDirect" ||
            effect.methodName == "beAdded" || effect.methodName == "_beAddedDirect")
        {
            int giftCardNum = CardManager.Instance.GetGiftCardNum();
            if (giftCardNum <= 0)
            {
                Debug.Log($"{currentChainCard.name} 技能发动失败：手牌中没有合法的目标牌可供选择！");
                if (currentChainCard != null) MarkConditionFailed(currentChainCard.id);
                StartExecutingNextChain();
                return;
            }
            // 标记为等待异步UI操作（阻塞后续效果）
            waitingForAsync = true;
        }
        else if (effect.methodName == "beBroken" || effect.methodName == "_beBrokenDirect")
        {
            int threshold = 0;
            if (effect.parameters != null && effect.parameters.Length > 0)
                threshold = Convert.ToInt32(effect.parameters[0]);

            int giftCardNum = CardManager.Instance.GetGiftCardNum();
            if (giftCardNum < threshold)
            {
                Debug.Log($"{currentChainCard.name} 消耗失败：礼品卡不足，停止执行当前效果链");
                if (currentChainCard != null) MarkConditionFailed(currentChainCard.id);
                StartExecutingNextChain();
                return;
            }
            // 标记为等待异步UI操作（阻塞后续效果）
            waitingForAsync = true;
        }

        // 尝试执行效果函数（如果上面设置了 waitingForAsync = true，在函数内部调出UI后，会停住）
        try
        {
            Execute(effect.methodName, effect.parameters);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CardEffect] 执行效果 {effect.methodName} 时出错: {ex.InnerException?.Message ?? ex.Message}");
            waitingForAsync = false; // 出错时解除死锁
        }

        // 如果不是异步效果，立即执行下一个
        if (!waitingForAsync)
        {
            ExecuteNextEffect();
        }
    }

    /// <summary>
    /// 【核心唤醒机制】由 CardSelector 在玩家点击“确认”或“取消”后调用
    /// </summary>
    public void OnSelectCardEnd(object obj)
    {
        if (waitingForAsync)
        {
            Debug.Log($"[CardEffect] 玩家已完成UI操作，恢复效果链执行...");
            waitingForAsync = false;
            ExecuteNextEffect(); // 继续往下走
        }
    }

    // =======================================================
    // 下方为具体的卡牌效果方法 (没有任何改动，仅保留逻辑)
    // =======================================================

    public void addNature(int num, int id) => CallerCard.Add(id, num);
    public void addNatureTo(int num, int id) => CallerCard.AddTo(id, num);
    public void addNatureAtSum(int num, int id) => DataManager.Instance.Add(id, num);
    public void addNatureFromTo(int id1, int id2) => DataManager.Instance.AddNatureFromTo(id1, id2);
    public void addCard(int cardID, int num) => CardManager.Instance.AddCard(cardID, num);
    public void addRandomCard(int type, int num, int level) => CardManager.Instance.AddRandomCard(type, num, level);
    public void addRandomCardIfNot(int type, int num, int level) => CardManager.Instance.AddRandomCardIfNot(type, num, level);
    public void addMoney(int money) => DataManager.Instance.Add(4, money);

    public void changeProperty(float ratio)
    {
        if (CallerCard == null) return;
        string listenerKey = $"{CallerCard.id}_{ratio}";
        if (registeredChangePropertyListeners.Contains(listenerKey)) return;

        registeredChangePropertyListeners.Add(listenerKey);
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
        // 唤起 UI，开启连续选牌模式
        ShengZhiAndJianZhiHelper.Instance.ShengZhang(num, times);
    }

    public void beAddedTo(int num)
    {
        CallerCard.AddTo(1, num);
        CallerCard.AddTo(2, num);
        CallerCard.AddTo(3, num);
    }

    public void beMade(int num)
    {
        if (num == 0) return;
        ShengZhiAndJianZhiHelper.Instance.SetNum(num);
        ShengZhiAndJianZhiHelper.Instance.ShengZhi(); // 呼叫UI
    }

    public void beBroken(int num)
    {
        if (num == 0) return;
        ShengZhiAndJianZhiHelper.Instance.SetNum(num);
        ShengZhiAndJianZhiHelper.Instance.JianZhi(); // 呼叫UI
    }

    public void addAddNum(int num) => CallerCard?.TryModifyAddedValue(num);
    public void changeHandGift() => CardManager.Instance.ChangeHandGift();

    public void addNatureAtSumIf(int type1, int type2)
    {
        if (CallerCard.GetNatureById(type1) > 0) DataManager.Instance.Add(type2, 10);
    }

    public void addNatureByOther(int type1, int type2)
    {
        int addValue = DataManager.Instance.GetNatureById(type1) / 2;
        DataManager.Instance.Add(type2, addValue);
    }

    public void addWithSame(int sameNum, int trueNum, int falseNum)
    {
        int sameCount = CardManager.Instance.GetCardCountInDeck(CallerCard.id);
        beAdded(sameCount >= sameNum ? trueNum : falseNum, 1);
    }

    public void addWithSameTogether(int sameNum, int addNum)
    {
        List<Card> sameCards = CardManager.Instance.GetCardsInDeckById(CallerCard.id);
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

    public void noConsumed()
    {
        Card copyCard = new Card();
        copyCard.InitCard(CallerCard);
        copyCard.OnAdded();
        CardManager.Instance.AddCardInHand(copyCard);
    }

    public void addCardNatureToDataManager()
    {
        if (CallerCard == null || IsConditionFailed(CallerCard.id)) return;
        DataManager.Instance.Add(1, CallerCard.nature1);
        DataManager.Instance.Add(2, CallerCard.nature2);
        DataManager.Instance.Add(3, CallerCard.nature3);
    }

    // --- 底层执行逻辑 ---

    public void Execute(string methodName, object[] parameters)
    {
        MethodInfo method = this.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
        if (method == null) return;
        try { method.Invoke(this, parameters); } catch (Exception e) { Debug.LogError(e); }
    }

    public object[] ConvertParameters(string methodName, string[] stringArgs)
    {
        MethodInfo method = this.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (method == null) return null;

        ParameterInfo[] paramInfos = method.GetParameters();
        object[] result = new object[paramInfos.Length];

        for (int i = 0; i < paramInfos.Length; i++)
        {
            if (i < stringArgs.Length)
            {
                try { result[i] = Convert.ChangeType(stringArgs[i].Trim(), paramInfos[i].ParameterType); }
                catch { result[i] = paramInfos[i].HasDefaultValue ? paramInfos[i].DefaultValue : null; }
            }
            else
            {
                result[i] = paramInfos[i].HasDefaultValue ? paramInfos[i].DefaultValue : null;
            }
        }
        return result;
    }

    public void SetCallerCard(Card card) => CallerCard = card;
    public bool IsConditionFailed(int cardId) => conditionFailedCardIds.Contains(cardId);
    public void MarkConditionFailed(int cardId) => conditionFailedCardIds.Add(cardId);
    public void ClearConditionFailed(int cardId) => conditionFailedCardIds.Remove(cardId);

    // --- int类型字段直接执行 ---

    public void _beMadeDirect(int num)
    {
        if (CallerCard == null || num == 0) return;
        ShengZhiAndJianZhiHelper.Instance.SetNum(num);
        ShengZhiAndJianZhiHelper.Instance.ShengZhi();
    }

    public void _beBrokenDirect(int num)
    {
        if (CallerCard == null || num == 0) return;
        ShengZhiAndJianZhiHelper.Instance.SetNum(num);
        ShengZhiAndJianZhiHelper.Instance.JianZhi();
    }

    public void _beAddedDirect(int num, int times = 1)
    {
        if (CallerCard == null || num == 0 || times == 0) return;
        // 唤起 UI，开启连续选牌模式
        ShengZhiAndJianZhiHelper.Instance.ShengZhang(num, times);
    }
}