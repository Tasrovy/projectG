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

    // 记录条件失败的卡牌ID（当beMade/beBroken条件不满足时）
    private static HashSet<int> conditionFailedCardIds = new HashSet<int>();

    // 等待BeginAction触发的标志
    private bool waitingForBeginAction = false;
    // BeginAction监听器引用
    private UnityEngine.Events.UnityAction beginActionListener = null;


    protected override void Awake()
    {
        base.Awake();
        Debug.Log("[CardEffect] Awake: CardEffect实例已创建，注册selectCardEnd事件监听器");
        // 监听选择卡牌结束事件，表示异步操作完成
        EventManage.AddEvent(EventManageEnum.selectCardEnd, OnSelectCardEnd);
    }

    protected override void OnDestroy()
    {
        Debug.Log("[CardEffect] OnDestroy: CardEffect实例即将销毁，移除selectCardEnd事件监听器");
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

        Debug.Log($"[CardEffect] ExecuteEffectList: 卡牌 {card?.name ?? "null"} (ID:{card?.id ?? -1}) 提交效果链执行，效果数量:{effects.Count}");

        // 显示效果链的详细信息
        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            string paramStr = effect.parameters != null ? string.Join(", ", effect.parameters) : "无参数";
            Debug.Log($"[CardEffect] ExecuteEffectList:  效果{i+1}: {effect.methodName}({paramStr})");
        }

        effectChainQueue.Enqueue((card, effects));

        if (!isExecutingChain)
        {
            Debug.Log($"[CardEffect] ExecuteEffectList: 当前无执行中的效果链，开始执行");
            StartExecutingNextChain();
        }
        else
        {
            Debug.Log($"[CardEffect] ExecuteEffectList: 效果链已加入队列，等待执行，队列长度:{effectChainQueue.Count}");
        }
    }

    /// <summary>
    /// 开始执行下一个效果链
    /// </summary>
    private void StartExecutingNextChain()
    {
        if (effectChainQueue.Count == 0)
        {
            Debug.Log($"[CardEffect] StartExecutingNextChain: 效果链队列为空，停止执行");
            isExecutingChain = false;
            // 清理任何可能存在的BeginAction订阅
            if (waitingForBeginAction)
            {
                waitingForBeginAction = false;
                if (beginActionListener != null && ShengZhiAndJianZhiHelper.Instance != null)
                {
                    ShengZhiAndJianZhiHelper.Instance.BeginAction.RemoveListener(beginActionListener);
                    beginActionListener = null;
                }
                Debug.Log($"[CardEffect] StartExecutingNextChain: 清理残留的BeginAction订阅");
            }
            return;
        }

        var (card, effects) = effectChainQueue.Dequeue();
        currentChainCard = card;
        currentChainEffects = effects;
        currentEffectIndex = 0;
        isExecutingChain = true;
        waitingForAsync = false;
        // 重置BeginAction等待标志
        if (waitingForBeginAction)
        {
            waitingForBeginAction = false;
            if (beginActionListener != null && ShengZhiAndJianZhiHelper.Instance != null)
            {
                ShengZhiAndJianZhiHelper.Instance.BeginAction.RemoveListener(beginActionListener);
                beginActionListener = null;
            }
            Debug.Log($"[CardEffect] StartExecutingNextChain: 重置BeginAction等待标志并清理订阅");
        }

        Debug.Log($"[CardEffect] StartExecutingNextChain: 开始执行卡牌 {card?.name ?? "null"} (ID:{card?.id ?? -1}) 的效果链，效果数量:{effects.Count}，队列剩余:{effectChainQueue.Count}");
        // 开始执行第一个效果
        ExecuteNextEffect();
    }

    /// <summary>
    /// 执行下一个效果
    /// </summary>
    private void ExecuteNextEffect()
    {
        // 如果正在等待异步操作，则暂停执行
        if (waitingForAsync)
        {
            Debug.Log($"[CardEffect] ExecuteNextEffect: 正在等待异步操作，暂停执行");
            return;
        }

        // 检查是否所有效果都已执行完毕
        if (currentEffectIndex >= currentChainEffects.Count)
        {
            Debug.Log($"[CardEffect] ExecuteNextEffect: 当前效果链执行完毕，共执行{currentEffectIndex}个效果");
            // 当前链执行完毕，开始下一个链
            StartExecutingNextChain();
            return;
        }

        var effect = currentChainEffects[currentEffectIndex];
        Debug.Log($"[CardEffect] ExecuteNextEffect: 执行第{currentEffectIndex+1}/{currentChainEffects.Count}个效果，方法:{effect.methodName}，参数:{string.Join(",", effect.parameters ?? new object[0])}");
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
        Debug.Log($"[CardEffect] ExecuteSingleEffect: 开始执行单个效果，方法:{effect.methodName}");
        // 检查礼品卡条件（beMade/beBroken特有）
        if (effect.methodName == "beMade" || effect.methodName == "_beMadeDirect")
        {
            int giftCardNum = CardManager.Instance.GetGiftCardNum();
            Debug.Log($"[CardEffect] ExecuteSingleEffect: {effect.methodName}检查礼品卡数量，当前数量: {giftCardNum}");
            // beMade 强制要求 GiftCardNum > 0
            if (giftCardNum <= 0)
            {
                Debug.Log($"{currentChainCard.name} 制作失败：礼品卡不足 (当前: {giftCardNum})，停止执行当前效果链");
                // 记录条件失败的卡牌ID
                if (currentChainCard != null)
                {
                    MarkConditionFailed(currentChainCard.id);
                }
                // 条件不满足，停止执行当前效果链，开始执行下一个效果链
                StartExecutingNextChain();
                return;
            }

            // 设置异步等待标志
            waitingForAsync = true;
            // 订阅BeginAction事件，阻塞执行直到用户选择完成
            SubscribeToBeginAction();
        }
        else if (effect.methodName == "beBroken" || effect.methodName == "_beBrokenDirect")
        {
            int threshold = 0;
            if (effect.parameters != null && effect.parameters.Length > 0)
                threshold = Convert.ToInt32(effect.parameters[0]);

            int giftCardNum = CardManager.Instance.GetGiftCardNum();
            Debug.Log($"[CardEffect] ExecuteSingleEffect: {effect.methodName}检查礼品卡数量，当前数量: {giftCardNum}，阈值: {threshold}，条件: {giftCardNum} >= {threshold}");

            // beBroken 要求其 大于等于 参数（当数量相等时也可以正常触发）
            if (giftCardNum < threshold)
            {
                Debug.Log($"{currentChainCard.name} 消耗失败：当前礼品卡数量 {giftCardNum} 小于设定值 {threshold}，停止执行当前效果链");
                // 记录条件失败的卡牌ID
                if (currentChainCard != null)
                {
                    MarkConditionFailed(currentChainCard.id);
                }
                // 条件不满足，停止执行当前效果链，开始执行下一个效果链
                StartExecutingNextChain();
                return;
            }

            // 设置异步等待标志
            waitingForAsync = true;
            // 订阅BeginAction事件，阻塞执行直到用户选择完成
            SubscribeToBeginAction();
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
            // 如果正在等待BeginAction，清理订阅
            if (waitingForBeginAction)
            {
                waitingForBeginAction = false;
                if (beginActionListener != null && ShengZhiAndJianZhiHelper.Instance != null)
                {
                    ShengZhiAndJianZhiHelper.Instance.BeginAction.RemoveListener(beginActionListener);
                    beginActionListener = null;
                }
                Debug.Log($"[CardEffect] ExecuteSingleEffect: 发生异常，已清理BeginAction订阅");
            }
        }

        // 如果不是异步效果，立即执行下一个
        if (!waitingForAsync)
        {
            ExecuteNextEffect();
        }
        else
        {
            Debug.Log($"[CardEffect] ExecuteSingleEffect: 异步效果 {effect.methodName} 正在等待用户操作");
        }
    }

    /// <summary>
    /// 选择卡牌结束事件回调
    /// </summary>
    private void OnSelectCardEnd(object obj)
    {
        Debug.Log($"[CardEffect] OnSelectCardEnd: 收到selectCardEnd事件，waitingForBeginAction={waitingForBeginAction}，waitingForAsync={waitingForAsync}");

        // 如果正在等待BeginAction，则忽略selectCardEnd事件
        if (waitingForBeginAction)
        {
            Debug.Log($"[CardEffect] OnSelectCardEnd: 正在等待BeginAction，忽略selectCardEnd事件");
            return;
        }

        if (waitingForAsync)
        {
            Debug.Log($"[CardEffect] OnSelectCardEnd: 用户选择卡牌完成，异步操作结束");
            // 异步操作完成，继续执行下一个效果
            waitingForAsync = false;
            ExecuteNextEffect();
        }
        else
        {
            Debug.Log($"[CardEffect] OnSelectCardEnd: 收到selectCardEnd事件，但当前没有等待异步操作");
        }
    }

    /// <summary>
    /// 订阅BeginAction事件
    /// </summary>
    private void SubscribeToBeginAction()
    {
        Debug.Log($"[CardEffect] SubscribeToBeginAction: 开始订阅，waitingForBeginAction={waitingForBeginAction}，beginActionListener={beginActionListener != null}");

        if (ShengZhiAndJianZhiHelper.Instance == null)
        {
            Debug.LogError("[CardEffect] SubscribeToBeginAction: ShengZhiAndJianZhiHelper.Instance为null");
            return;
        }

        // 如果已有监听器，先移除
        if (beginActionListener != null)
        {
            Debug.Log($"[CardEffect] SubscribeToBeginAction: 移除旧的监听器");
            ShengZhiAndJianZhiHelper.Instance.BeginAction.RemoveListener(beginActionListener);
        }

        beginActionListener = OnBeginActionTriggered;
        ShengZhiAndJianZhiHelper.Instance.BeginAction.AddListener(beginActionListener);
        waitingForBeginAction = true;
        waitingForAsync = true; // 为了向后兼容
        Debug.Log($"[CardEffect] SubscribeToBeginAction: 已订阅BeginAction事件，ShengZhiAndJianZhiHelper.Instance.BeginAction监听器数量: 需要手动检查");
    }

    /// <summary>
    /// BeginAction触发时的回调
    /// </summary>
    private void OnBeginActionTriggered()
    {
        Debug.Log($"[CardEffect] OnBeginActionTriggered: BeginAction触发，waitingForBeginAction={waitingForBeginAction}");

        if (waitingForBeginAction)
        {
            Debug.Log("[CardEffect] OnBeginActionTriggered: BeginAction已触发，恢复效果链执行");
            waitingForBeginAction = false;
            waitingForAsync = false; // 清除异步等待标志
            // 移除监听器
            if (beginActionListener != null && ShengZhiAndJianZhiHelper.Instance != null)
            {
                ShengZhiAndJianZhiHelper.Instance.BeginAction.RemoveListener(beginActionListener);
                beginActionListener = null;
            }
            // 继续执行下一个效果
            ExecuteNextEffect();
        }
        else
        {
            Debug.Log($"[CardEffect] OnBeginActionTriggered: 收到BeginAction但waitingForBeginAction=false");
        }
    }

    public void addNature(int num, int id)
    {
        Debug.Log($"[CardEffect] addNature: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 增加属性 {id} 值 {num}");
        CallerCard.Add(id, num);
        Debug.Log($"[CardEffect] addNature: 执行完成，当前属性{id}值为 {CallerCard?.GetNatureById(id) ?? -1}");
    }

    public void addNatureTo(int num, int id)
    {
        Debug.Log($"[CardEffect] addNatureTo: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 设置属性 {id} 目标值 {num}，当前值 {CallerCard?.GetNatureById(id) ?? -1}");
        CallerCard.AddTo(id, num);
        Debug.Log($"[CardEffect] addNatureTo: 执行完成，属性{id}当前值 {CallerCard?.GetNatureById(id) ?? -1}");
    }

    public void addNatureAtSum( int num,int id)
    {
        Debug.Log($"[CardEffect] addNatureAtSum: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 为DataManager增加属性 {id} 值 {num}");
        DataManager.Instance.Add(id, num);
        Debug.Log($"[CardEffect] addNatureAtSum: 执行完成，DataManager属性{id}增加 {num}");
    }

    public void addNatureFromTo(int id1, int id2)
    {
        Debug.Log($"[CardEffect] addNatureFromTo: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 复制DataManager属性 {id2} 到 {id1}");
        DataManager.Instance.AddNatureFromTo(id1, id2);
        Debug.Log($"[CardEffect] addNatureFromTo: 执行完成，属性{id1}复制了属性{id2}的值");
    }

    public void addCard(int cardID, int num)
    {
        Debug.Log($"[CardEffect] addCard: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 添加卡牌 ID:{cardID} 数量:{num}");
        CardManager.Instance.AddCard(cardID, num);
        Debug.Log($"[CardEffect] addCard: 执行完成，已添加卡牌 ID:{cardID} 数量:{num}");
    }

    public void addRandomCard(int type, int num, int level)
    {
        Debug.Log($"[CardEffect] addRandomCard: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 随机添加类型:{type} 数量:{num} 稀有度:{level}");
        CardManager.Instance.AddRandomCard(type, num, level);
        Debug.Log($"[CardEffect] addRandomCard: 执行完成，已随机添加 {num} 张卡牌");
    }

    public void addRandomCardIfNot(int type, int num, int level)
    {
        Debug.Log($"[CardEffect] addRandomCardIfNot: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 检查类型:{type}，如果不存在则添加 {num} 张稀有度:{level}");
        CardManager.Instance.AddRandomCardIfNot(type, num, level);
        Debug.Log($"[CardEffect] addRandomCardIfNot: 执行完成");
    }

    public void addMoney(int money)
    {
        Debug.Log($"[CardEffect] addMoney: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 增加金钱 {money}");
        DataManager.Instance.Add(4, money);
        Debug.Log($"[CardEffect] addMoney: 执行完成，增加金钱 {money}");
    }

    public void changeProperty(float ratio)
    {
        Debug.Log($"[CardEffect] changeProperty: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 注册属性比例变化监听器 ratio={ratio}");
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
        Debug.Log($"[CardEffect] beAdded: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 开始生长，每次值:{num}，次数:{times}，当前属性值: n1={CallerCard?.nature1 ?? -1}, n2={CallerCard?.nature2 ?? -1}, n3={CallerCard?.nature3 ?? -1}");
        for (int i = 0; i < times; i++)
        {
            CallerCard.Add(1, num);
            CallerCard.Add(2, num);
            CallerCard.Add(3, num);
            Debug.Log($"[CardEffect] beAdded: 第{i+1}次生长完成，当前属性值: n1={CallerCard?.nature1 ?? -1}, n2={CallerCard?.nature2 ?? -1}, n3={CallerCard?.nature3 ?? -1}");
        }
        Debug.Log($"[CardEffect] beAdded: 生长完成，最终属性值: n1={CallerCard?.nature1 ?? -1}, n2={CallerCard?.nature2 ?? -1}, n3={CallerCard?.nature3 ?? -1}");
    }

    public void beAddedTo(int num)
    {
        Debug.Log($"[CardEffect] beAddedTo: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 设置目标值:{num}，当前属性值: n1={CallerCard?.nature1 ?? -1}, n2={CallerCard?.nature2 ?? -1}, n3={CallerCard?.nature3 ?? -1}");
        CallerCard.AddTo(1, num);
        CallerCard.AddTo(2, num);
        CallerCard.AddTo(3, num);
        Debug.Log($"[CardEffect] beAddedTo: 执行完成，最终属性值: n1={CallerCard?.nature1 ?? -1}, n2={CallerCard?.nature2 ?? -1}, n3={CallerCard?.nature3 ?? -1}");
    }

    public void beMade(int num)
    {
        Debug.Log($"[CardEffect] beMade: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 开始制作，数量:{num}");

        // 如果num为0，直接返回
        if (num == 0)
        {
            Debug.Log($"[CardEffect] beMade: num为0，直接返回");
            return;
        }

        ShengZhiAndJianZhiHelper.Instance.SetNum(num);
        ShengZhiAndJianZhiHelper.Instance.ShengZhi();
        Debug.Log($"[CardEffect] beMade: 制作指令已发送，等待用户选择卡牌");
    }

    public void beBroken(int num)
    {
        Debug.Log($"[CardEffect] beBroken: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 开始消耗，阈值:{num}");

        // 如果num为0，直接返回
        if (num == 0)
        {
            Debug.Log($"[CardEffect] beBroken: num为0，直接返回");
            return;
        }

        ShengZhiAndJianZhiHelper.Instance.SetNum(num);
        ShengZhiAndJianZhiHelper.Instance.JianZhi();
        Debug.Log($"[CardEffect] beBroken: 消耗指令已发送，等待用户选择卡牌");
    }

    public void addAddNum(int num)
    {
        Debug.Log($"[CardEffect] addAddNum: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 尝试修改added字段，修改值:{num}，当前added值:'{CallerCard?.added ?? "null"}'");
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
        Debug.Log($"[CardEffect] changeHandGift: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 触发更换手牌礼品卡");
        CardManager.Instance.ChangeHandGift();
        Debug.Log($"[CardEffect] changeHandGift: 执行完成，已更换手牌中的礼品卡");
    }

    public void addNatureAtSumIf(int type1, int type2)
    {
        Debug.Log($"[CardEffect] addNatureAtSumIf: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 检查属性{type1}是否>0");
        int num = CallerCard.GetNatureById(type1);
        Debug.Log($"[CardEffect] addNatureAtSumIf: 属性{type1}值为{num}，条件判断:{num>0}");
        if(num>0)
        {
            DataManager.Instance.Add(type2,10);
            Debug.Log($"[CardEffect] addNatureAtSumIf: 条件满足，为DataManager属性{type2}增加10");
        }
        else
        {
            Debug.Log($"[CardEffect] addNatureAtSumIf: 条件不满足，跳过执行");
        }
    }

    public void addNatureByOther(int type1, int type2)
    {
        Debug.Log($"[CardEffect] addNatureByOther: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 根据DataManager属性{type1}增加属性{type2}");
        int sourceValue = DataManager.Instance.GetNatureById(type1);
        int addValue = sourceValue / 2;
        Debug.Log($"[CardEffect] addNatureByOther: 源属性{type1}值:{sourceValue}，计算增加量:{addValue}");
        DataManager.Instance.Add(type2, addValue);
        Debug.Log($"[CardEffect] addNatureByOther: 执行完成，为属性{type2}增加{addValue}");
    }

    public void addWithSame(int sameNum, int trueNum, int falseNum)
    {
        Debug.Log($"[CardEffect] addWithSame: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 检查牌堆中相同卡牌数量，阈值:{sameNum}，满足生长{trueNum}，不满足生长{falseNum}");
        // 获取当前卡牌在牌堆中的数量
        int sameCount = CardManager.Instance.GetCardCountInDeck(CallerCard.id);
        Debug.Log($"[CardEffect] addWithSame: 牌堆中相同卡牌数量:{sameCount}，条件判断:{sameCount >= sameNum}");

        if (sameCount >= sameNum)
        {
            // 条件满足：生长trueNum
            Debug.Log($"[CardEffect] addWithSame: 条件满足，执行生长{trueNum}");
            beAdded(trueNum, 1);
        }
        else
        {
            // 条件不满足：生长falseNum
            Debug.Log($"[CardEffect] addWithSame: 条件不满足，执行生长{falseNum}");
            beAdded(falseNum, 1);
        }
        Debug.Log($"[CardEffect] addWithSame: 执行完成");
    }

    public void addWithSameTogether(int sameNum, int addNum)
    {
        Debug.Log($"[CardEffect] addWithSameTogether: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 检查牌堆中相同卡牌集体生长，阈值:{sameNum}，生长值:{addNum}");
        // 获取牌堆中所有相同ID的卡牌
        List<Card> sameCards = CardManager.Instance.GetCardsInDeckById(CallerCard.id);
        Debug.Log($"[CardEffect] addWithSameTogether: 牌堆中相同卡牌数量:{sameCards.Count}，条件判断:{sameCards.Count >= sameNum}");

        // 检查数量是否满足条件
        if (sameCards.Count >= sameNum)
        {
            Debug.Log($"[CardEffect] addWithSameTogether: 条件满足，开始对{sameCards.Count}张相同卡牌执行集体生长");
            // 对这些相同的礼物都执行生长
            foreach (Card card in sameCards)
            {
                // 生长操作：对每张卡增加三种属性值
                card.Add(1, addNum);
                card.Add(2, addNum);
                card.Add(3, addNum);
                Debug.Log($"[CardEffect] addWithSameTogether: 卡牌 {card.name} (ID:{card.id}) 生长完成，当前属性: n1={card.nature1}, n2={card.nature2}, n3={card.nature3}");
            }

            // 通知UI更新，因为牌堆中的卡牌属性已改变
            CardManager.Instance.NotifyDeckOrHandChanged();
            Debug.Log($"[CardEffect] addWithSameTogether: 集体生长完成，已通知UI更新");
        }
        else
        {
            Debug.Log($"[CardEffect] addWithSameTogether: 条件不满足，跳过执行");
        }
    }

    public void noConsumed()
    {
        Debug.Log($"[CardEffect] noConsumed: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 触发不消耗效果，复制当前卡牌到手牌");
        // 生成一张和这张牌数据一致的牌加入手牌
        Card copyCard = new Card();
        copyCard.InitCard(CallerCard);  // 复制当前卡牌数据
        Debug.Log($"[CardEffect] noConsumed: 已复制卡牌 {copyCard.name} (ID:{copyCard.id})，属性值: n1={copyCard.nature1}, n2={copyCard.nature2}, n3={copyCard.nature3}");
        copyCard.OnAdded();             // 触发added效果
        CardManager.Instance.AddCardInHand(copyCard);  // 加入手牌
        Debug.Log($"[CardEffect] noConsumed: 复制卡牌已加入手牌");
    }

    public void addCardNatureToDataManager()
    {
        Debug.Log($"[CardEffect] addCardNatureToDataManager: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 将卡牌属性添加到DataManager");
        if (CallerCard == null) return;

        // 检查卡牌是否条件失败
        if (CallerCard != null && IsConditionFailed(CallerCard.id))
        {
            Debug.Log($"[CardEffect] addCardNatureToDataManager: 卡牌 {CallerCard.name} (ID:{CallerCard.id}) 条件失败，不执行添加属性操作");
            // 不清除标记，让OnTrigger()处理
            return;
        }

        Debug.Log($"[CardEffect] addCardNatureToDataManager: 属性值: n1={CallerCard.nature1}, n2={CallerCard.nature2}, n3={CallerCard.nature3}");
        DataManager.Instance.Add(1, CallerCard.nature1);
        DataManager.Instance.Add(2, CallerCard.nature2);
        DataManager.Instance.Add(3, CallerCard.nature3);
        Debug.Log($"[CardEffect] addCardNatureToDataManager: 执行完成，已将卡牌属性添加到DataManager");
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
            Debug.Log($"[CardEffect] Execute: 方法 {methodName} 执行成功");
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
        Debug.Log($"[CardEffect] ConvertParameters: 开始转换方法 {methodName} 的参数，原始参数: [{string.Join(", ", stringArgs ?? new string[0])}]");

        MethodInfo method = this.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (method == null)
        {
            Debug.LogError($"[CardEffect] ConvertParameters: 找不到方法 {methodName}");
            return null;
        }

        ParameterInfo[] paramInfos = method.GetParameters();
        object[] result = new object[paramInfos.Length];

        Debug.Log($"[CardEffect] ConvertParameters: 方法 {methodName} 需要 {paramInfos.Length} 个参数，提供 {stringArgs?.Length ?? 0} 个");

        for (int i = 0; i < paramInfos.Length; i++)
        {
            if (i < stringArgs.Length)
            {
                // 核心：自动转为 int, float, bool, string 等
                try
                {
                    result[i] = Convert.ChangeType(stringArgs[i].Trim(), paramInfos[i].ParameterType);
                    Debug.Log($"[CardEffect] ConvertParameters: 参数 {i} 转换成功: '{stringArgs[i].Trim()}' -> {result[i]} (类型: {paramInfos[i].ParameterType.Name})");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CardEffect] ConvertParameters: 参数 {i} 转换失败: '{stringArgs[i].Trim()}' -> {paramInfos[i].ParameterType.Name}, 错误: {ex.Message}");
                    result[i] = paramInfos[i].HasDefaultValue ? paramInfos[i].DefaultValue : null;
                }
            }
            else
            {
                result[i] = paramInfos[i].HasDefaultValue ? paramInfos[i].DefaultValue : null;
                Debug.Log($"[CardEffect] ConvertParameters: 参数 {i} 使用默认值: {result[i] ?? "null"}");
            }
        }

        Debug.Log($"[CardEffect] ConvertParameters: 方法 {methodName} 参数转换完成，结果: [{string.Join(", ", result)}]");
        return result;
    }

    public void SetCallerCard(Card card)
    {
        Debug.Log($"[CardEffect] SetCallerCard: 设置当前调用者卡牌为 {card?.name ?? "null"} (ID:{card?.id ?? -1})，之前调用者卡牌为 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1})");
        CallerCard = card;
    }

    /// <summary>
    /// 检查卡牌是否条件失败（如beMade/beBroken条件不满足）
    /// </summary>
    public bool IsConditionFailed(int cardId)
    {
        bool failed = conditionFailedCardIds.Contains(cardId);
        Debug.Log($"[CardEffect] IsConditionFailed: 检查卡牌ID {cardId} 是否条件失败: {failed}");
        return failed;
    }

    /// <summary>
    /// 标记卡牌条件失败
    /// </summary>
    public void MarkConditionFailed(int cardId)
    {
        if (conditionFailedCardIds.Add(cardId))
        {
            Debug.Log($"[CardEffect] MarkConditionFailed: 标记卡牌ID {cardId} 条件失败");
        }
    }

    /// <summary>
    /// 清除卡牌的条件失败标记
    /// </summary>
    public void ClearConditionFailed(int cardId)
    {
        if (conditionFailedCardIds.Remove(cardId))
        {
            Debug.Log($"[CardEffect] ClearConditionFailed: 清除卡牌ID {cardId} 的条件失败标记");
        }
    }

    // --- int类型字段的直接执行方法（避免循环调用） ---

    /// <summary>
    /// int类型made字段的直接执行方法（卡牌打出时触发）
    /// </summary>
    public void _beMadeDirect(int num)
    {
        Debug.Log($"[CardEffect] _beMadeDirect: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 直接执行增殖，数量:{num}");

        // 检查CallerCard是否有效
        if (CallerCard == null)
        {
            Debug.LogError($"[CardEffect] _beMadeDirect: CallerCard为null，无法执行增殖");
            return;
        }

        // 如果num为0，直接返回
        if (num == 0)
        {
            Debug.Log($"[CardEffect] _beMadeDirect: num为0，直接返回");
            return;
        }

        // 检查ShengZhiAndJianZhiHelper实例是否存在
        if (ShengZhiAndJianZhiHelper.Instance == null)
        {
            Debug.LogError($"[CardEffect] _beMadeDirect: ShengZhiAndJianZhiHelper.Instance 为null，无法执行增殖");
            return;
        }

        Debug.Log($"[CardEffect] _beMadeDirect: 设置增殖数量为 {num}");
        ShengZhiAndJianZhiHelper.Instance.SetNum(num);

        Debug.Log($"[CardEffect] _beMadeDirect: 调用ShengZhi()方法，这将触发CallIt()发送selectCardBegin事件");
        ShengZhiAndJianZhiHelper.Instance.ShengZhi();
        Debug.Log($"[CardEffect] _beMadeDirect: 直接增殖指令已发送，等待用户选择卡牌");
    }

    /// <summary>
    /// int类型broken字段的直接执行方法（卡牌打出时触发）
    /// </summary>
    public void _beBrokenDirect(int num)
    {
        Debug.Log($"[CardEffect] _beBrokenDirect: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 直接执行消耗，数量:{num}");

        // 检查CallerCard是否有效
        if (CallerCard == null)
        {
            Debug.LogError($"[CardEffect] _beBrokenDirect: CallerCard为null，无法执行消耗");
            return;
        }

        // 如果num为0，直接返回
        if (num == 0)
        {
            Debug.Log($"[CardEffect] _beBrokenDirect: num为0，直接返回");
            return;
        }

        // 检查ShengZhiAndJianZhiHelper实例是否存在
        if (ShengZhiAndJianZhiHelper.Instance == null)
        {
            Debug.LogError($"[CardEffect] _beBrokenDirect: ShengZhiAndJianZhiHelper.Instance 为null，无法执行消耗");
            return;
        }

        Debug.Log($"[CardEffect] _beBrokenDirect: 设置消耗数量为 {num}");
        ShengZhiAndJianZhiHelper.Instance.SetNum(num);

        Debug.Log($"[CardEffect] _beBrokenDirect: 调用JianZhi()方法，这将触发CallIt()发送selectCardBegin事件");
        ShengZhiAndJianZhiHelper.Instance.JianZhi();
        Debug.Log($"[CardEffect] _beBrokenDirect: 直接消耗指令已发送，等待用户选择卡牌");
    }

    /// <summary>
    /// int类型added字段的直接执行方法（卡牌打出时触发）
    /// </summary>
    public void _beAddedDirect(int num, int times = 1)
    {
        Debug.Log($"[CardEffect] _beAddedDirect: 卡牌 {CallerCard?.name ?? "null"} (ID:{CallerCard?.id ?? -1}) 直接执行生长，数量:{num}, 次数:{times}");

        // 检查CallerCard是否有效
        if (CallerCard == null)
        {
            Debug.LogError($"[CardEffect] _beAddedDirect: CallerCard为null，无法执行生长");
            return;
        }

        // 如果num为0，直接返回（虽然解析阶段应该已经跳过，但这里作为安全措施）
        if (num == 0)
        {
            Debug.Log($"[CardEffect] _beAddedDirect: num为0，直接返回");
            return;
        }

        // 直接执行生长逻辑，不通过beAdded->OnAdded循环
        for (int i = 0; i < times; i++)
        {
            CallerCard.Add(3, num); // 默认添加到属性3
            Debug.Log($"[CardEffect] _beAddedDirect: 第{i+1}次生长，增加属性3值 {num}，当前值: {CallerCard?.GetNatureById(3) ?? -1}");
        }

        Debug.Log($"[CardEffect] _beAddedDirect: 直接生长执行完成");
    }
}