using UnityEngine;
using System;
using System.Reflection;

public class CardEffect : Singleton<CardEffect>
{
    // --- 这里定义所有的效果函数 ---

    public Card CallerCard;

    public bool needShengZhi;
    public int shengZhiNum;
    
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
        DayManager.Instance.GetNextDayEvent().AddListener( ()=>
        {
            DataManager.Instance.SetNature1Effect(ratio - 1);
            DataManager.Instance.SetNature2Effect(ratio - 1);
            DataManager.Instance.SetNature3Effect(ratio - 1);
        });
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
        shengZhiNum = num;
        needShengZhi = true;
    }

    public void addAddNum(int num)
    {
        CallerCard.added += num;
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