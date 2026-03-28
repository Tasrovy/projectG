using UnityEngine;
using System;
using System.Reflection;

public class CardEffect : Singleton<CardEffect>
{
    // --- 这里定义所有的效果函数 ---

    public void p(string str)
    {
        Debug.Log(str);
    }
    
    public void DealDamage(int amount) 
    { 
        Debug.Log($"[效果] 造成了 {amount} 点伤害"); 
    }

    public void DrawCard(int count) 
    { 
        Debug.Log($"[效果] 抽了 {count} 张牌"); 
    }

    // 支持多个参数的例子
    public void Heal(int amount, bool playParticle)
    {
        Debug.Log($"[效果] 回复了 {amount} 点血量，播放特效: {playParticle}");
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
}