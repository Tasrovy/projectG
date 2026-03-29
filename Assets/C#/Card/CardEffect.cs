using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

public class CardEffect : Singleton<CardEffect>
{
    // --- 这里定义所有的效果函数 ---

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


    protected override void Awake()
    {
        base.Awake();
        // 监听选择卡牌结束事件，表示异步操作完成
        EventManage.AddEvent(EventManageEnum.selectCardEnd, OnSelectCardEnd);
    }

    protected override void OnDestroy()
    {
        // 移除事件监听
        EventManage.RemoveEvent(EventManageEnum.selectCardEnd, OnSelectCardEnd);
        base.OnDestroy();
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

        // 开始执行第一个效果
        ExecuteNextEffect();
    }

    /// <summary>
    /// 执行下一个效果
    /// </summary>
    private void ExecuteNextEffect()
    {
        // 如果正在等待异步操作，则暂停执行
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
    /// 执行单个效果，处理阻塞逻辑
    /// </summary>
    private void ExecuteSingleEffect(EffectCommand effect)
    {
        // 检查礼品卡条件（beMade/beBroken特有）
        if (effect.methodName == "beMade")
        {
            // beMade 强制要求 GiftCardNum > 0
            if (CardManager.Instance.GetGiftCardNum() <= 0)
            {
                Debug.Log($"{currentChainCard.name} 制作失败：礼品卡不足");
                // 跳过当前效果，继续执行下一个
                ExecuteNextEffect();
                return;
            }

            // 设置异步等待标志
            waitingForAsync = true;
        }
        else if (effect.methodName == "beBroken")
        {
            int threshold = 0;
            if (effect.parameters != null && effect.parameters.Length > 0)
                threshold = Convert.ToInt32(effect.parameters[0]);

            // beBroken 要求其 大于 参数
            if (CardManager.Instance.GetGiftCardNum() <= threshold)
            {
                Debug.Log($"{currentChainCard.name} 消耗失败：当前礼品卡数量 {CardManager.Instance.GetGiftCardNum()} 不足设定值 {threshold}");
                // 跳过当前效果，继续执行下一个
                ExecuteNextEffect();
                return;
            }

            // 设置异步等待标志
            waitingForAsync = true;
        }

        // 执行效果
        try
        {
            Execute(effect.methodName, effect.parameters);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CardEffect] 执行效果 {effect.methodName} 时出错: {ex.InnerException?.Message ?? ex.Message}");
            // 重置异步等待标志，防止死锁
            waitingForAsync = false;
        }

        // 如果不是异步效果，立即执行下一个
        if (!waitingForAsync)
        {
            ExecuteNextEffect();
        }
    }

    /// <summary>
    /// 选择卡牌结束事件回调
    /// </summary>
    private void OnSelectCardEnd(object obj)
    {
        if (waitingForAsync)
        {
            // 异步操作完成，继续执行下一个效果
            waitingForAsync = false;
            ExecuteNextEffect();
        }
    }

    public void addNature(int id, int num)
    {
        CallerCard.Add(id, num);
    }

    public void addNatureTo(int id, int num)
    {
        CallerCard.AddTo(id, num);
    }

    public void addNatureAtSum(int id, int num)
    {
        DataManager.Instance.Add(id, num);
    }

    public void addNatureFromTo(int id1, int id2)
    {
        DataManager.Instance.AddNatureFromTo(id1, id2);
    }

    public void addCard(int cardID, int num)
    {
        CardManager.Instance.AddCard(cardID, num);
    }

    public void addRandomCard(int type, int num, int level)
    {
        CardManager.Instance.AddRandomCard(type, num, level);
    }

    public void addRandomCardIfNot(int type, int num, int level)
    {
        CardManager.Instance.AddRandomCardIfNot(type, num, level);
    }

    public void addMoney(int money)
    {
        DataManager.Instance.Add(4, money);
    }

    public void changeProperty(float ratio)
    {
        if (CallerCard == null)
        {
            Debug.LogError("[CardEffect] changeProperty: CallerCard is null");
            return;
        }

        // 生成唯一标识符，防止同一个卡牌重复添加相同ratio的监听器
        string listenerKey = $"{CallerCard.id}_{ratio}";

        if (registeredChangePropertyListeners.Contains(listenerKey))
        {
            Debug.LogWarning($"[CardEffect] changeProperty: 监听器已注册，跳过。卡牌: {CallerCard.name} (ID:{CallerCard.id}), ratio: {ratio}");
            return;
        }

        // 注册监听器
        registeredChangePropertyListeners.Add(listenerKey);
        DayManager.Instance.GetNextDayEvent().AddListener(() =>
        {
            DataManager.Instance.SetNature1Effect(ratio - 1);
            DataManager.Instance.SetNature2Effect(ratio - 1);
            DataManager.Instance.SetNature3Effect(ratio - 1);
        });

        Debug.Log($"[CardEffect] changeProperty: 已为卡牌 {CallerCard.name} (ID:{CallerCard.id}) 注册ratio={ratio}的监听器");
    }

    public void beAdded(int num, int times)
    {
        for (int i = 0; i < times; i++)
        {
            CallerCard.Add(1, num);
            CallerCard.Add(2, num);
            CallerCard.Add(3, num);
        }
    }

    public void beAddedTo(int num)
    {
        CallerCard.AddTo(1, num);
        CallerCard.AddTo(2, num);
        CallerCard.AddTo(3, num);
    }

    public void beMade(int num)
    {
        ShengZhiAndJianZhiHelper.Instance.SetNum(num);
        ShengZhiAndJianZhiHelper.Instance.ShengZhi();
    }

    public void beBroken(int num)
    {
        ShengZhiAndJianZhiHelper.Instance.SetNum(num);
        ShengZhiAndJianZhiHelper.Instance.JianZhi();
    }

    public void addAddNum(int num)
    {
        if (CallerCard == null)
        {
            Debug.LogError($"[CardEffect] addAddNum({num}): CallerCard为null");
            return;
        }

        // 尝试修改added字段的数值
        bool success = CallerCard.TryModifyAddedValue(num);

        if (!success)
        {
            Debug.LogWarning($"[CardEffect] addAddNum({num}): 无法修改added字段。CallerCard: {CallerCard.name}, added值: '{CallerCard.added}'");
        }
        else
        {
            Debug.Log($"[CardEffect] addAddNum({num}): 成功修改added字段");
        }
    }

    public void changeHandGift()
    {
        CardManager.Instance.ChangeHandGift();
    }

    public void addNatureAtSumIf(int type1, int type2)
    {
        int num = CallerCard.GetNatureById(type1);
        if(num>0) DataManager.Instance.Add(type2,10);
    }

    public void addNatureByOther(int type1, int type2)
    {
        DataManager.Instance.Add(type2, DataManager.Instance.GetNatureById(type1)/2);
    }

    public void addWithSame(int sameNum, int trueNum, int falseNum)
    {
        // 获取当前卡牌在牌堆中的数量
        int sameCount = CardManager.Instance.GetCardCountInDeck(CallerCard.id);

        if (sameCount >= sameNum)
        {
            // 条件满足：生长trueNum
            beAdded(trueNum, 1);
        }
        else
        {
            // 条件不满足：生长falseNum
            beAdded(falseNum, 1);
        }
    }

    public void addWithSameTogether(int sameNum, int addNum)
    {
        // 获取牌堆中所有相同ID的卡牌
        List<Card> sameCards = CardManager.Instance.GetCardsInDeckById(CallerCard.id);

        // 检查数量是否满足条件
        if (sameCards.Count >= sameNum)
        {
            // 对这些相同的礼物都执行生长
            foreach (Card card in sameCards)
            {
                // 生长操作：对每张卡增加三种属性值
                card.Add(1, addNum);
                card.Add(2, addNum);
                card.Add(3, addNum);
            }

            // 通知UI更新，因为牌堆中的卡牌属性已改变
            CardManager.Instance.NotifyDeckOrHandChanged();
        }
    }

    public void noConsumed()
    {
        // 生成一张和这张牌数据一致的牌加入手牌
        Card copyCard = new Card();
        copyCard.InitCard(CallerCard);  // 复制当前卡牌数据
        copyCard.OnAdded();             // 触发added效果
        CardManager.Instance.AddCardInHand(copyCard);  // 加入手牌
    }

    public void addCardNatureToDataManager()
    {
        if (CallerCard == null) return;
        DataManager.Instance.Add(1, CallerCard.nature1);
        DataManager.Instance.Add(2, CallerCard.nature2);
        DataManager.Instance.Add(3, CallerCard.nature3);
    }
    // --- 核心执行逻辑 ---

    /// <summary>
    /// 执行指定的方法
    /// </summary>
    /// <param name="methodName">方法名</param>
    /// <param name="parameters">已经解析好的参数数组</param>
    public void Execute(string methodName, object[] parameters)
    {
        // 1. 获取方法信息 (BindingFlags 确保能找到公有或私有方法)
        MethodInfo method = this.GetType().GetMethod(methodName, 
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

        if (method == null)
        {
            Debug.LogError($"[CardEffect] 找不到方法: {methodName}");
            return;
        }

        // 2. 检查参数数量是否匹配
        ParameterInfo[] targetParams = method.GetParameters();
        if (targetParams.Length != (parameters?.Length ?? 0))
        {
            Debug.LogError($"[CardEffect] 方法 {methodName} 需要 {targetParams.Length} 个参数，但提供了 {parameters?.Length ?? 0} 个");
            return;
        }

        // 3. 执行
        try
        {
            method.Invoke(this, parameters);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CardEffect] 执行 {methodName} 出错: {e.InnerException?.Message ?? e.Message}");
        }
    }

    /// <summary>
    /// 辅助方法：将字符串参数数组转换为目标方法的真实类型数组
    /// </summary>
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
                // 核心：自动转为 int, float, bool, string 等
                result[i] = Convert.ChangeType(stringArgs[i].Trim(), paramInfos[i].ParameterType);
            }
            else
            {
                result[i] = paramInfos[i].HasDefaultValue ? paramInfos[i].DefaultValue : null;
            }
        }
        return result;
    }

    public void SetCallerCard(Card card)
    {
        CallerCard = card;
    }
}