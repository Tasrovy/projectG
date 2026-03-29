using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

[System.Serializable]
public class EffectCommand
{
    public string methodName;
    public object[] parameters;
}

[System.Serializable]
public class CardData
{
    public int id;
    public int nature1;
    public int nature2;
    public int nature3;
    public string name;
    public int sale;
    public string made;
    public string broken;
    public string added;
    public string description;
    public string buff;
    public string trigger;
    public string nextTurn;
}

[System.Serializable]
public class Card
{
    public int id;
    public int nature1;
    public int nature2;
    public int nature3;
    public string name;
    public int sale;
    public string made;
    public string broken;
    public string added;
    public string buff;
    public string trigger;
    public string description;
    public string nextTurn;

    // --- 被动生命周期效果列表 (仅当字段为字符串函数时，在 OnMade/OnBroken/OnAdded 中触发) ---
    private List<EffectCommand> madeEffects = new List<EffectCommand>();
    private List<EffectCommand> brokenEffects = new List<EffectCommand>();
    private List<EffectCommand> addedEffects = new List<EffectCommand>();
    
    // --- 主动打出条件效果列表 (仅当字段为纯数字时，在 OnTrigger 中作为前置条件触发) ---
    private List<EffectCommand> triggerMadeEffects = new List<EffectCommand>();
    private List<EffectCommand> triggerBrokenEffects = new List<EffectCommand>();
    private List<EffectCommand> triggerAddedEffects = new List<EffectCommand>();

    // --- 其他常规效果列表 ---
    private List<EffectCommand> buffEffects = new List<EffectCommand>();
    private List<EffectCommand> triggerEffects = new List<EffectCommand>();
    private List<EffectCommand> nextTurnEffects = new List<EffectCommand>();

    public void InitCard(CardData cardData)
    {
        id = cardData.id;
        nature1 = cardData.nature1;
        nature2 = cardData.nature2;
        nature3 = cardData.nature3;
        name = cardData.name;
        description = cardData.description;
        sale = cardData.sale;
        made = cardData.made;
        broken = cardData.broken;
        added = cardData.added;
        buff = cardData.buff;
        trigger = cardData.trigger;
        nextTurn = cardData.nextTurn;
        ParseAllEffects();
    }
    
    public void InitCard(Card cardData)
    {
        id = cardData.id;
        nature1 = cardData.nature1;
        nature2 = cardData.nature2;
        nature3 = cardData.nature3;
        name = cardData.name;
        description = cardData.description;
        sale = cardData.sale;        
        made = cardData.made;
        broken = cardData.broken;
        added = cardData.added;
        buff = cardData.buff;
        trigger = cardData.trigger;
        nextTurn = cardData.nextTurn;
        ParseAllEffects();
    }
    
    // 解析所有效果字段
    private void ParseAllEffects()
    {
        // 将数字逻辑和字符串逻辑分流到不同的列表中
        ParseFieldWithIntSupport(made, triggerMadeEffects, madeEffects, "made");
        ParseFieldWithIntSupport(broken, triggerBrokenEffects, brokenEffects, "broken");
        ParseFieldWithIntSupport(added, triggerAddedEffects, addedEffects, "added");
        
        ParseStringToCommands(buff, buffEffects);
        ParseStringToCommands(trigger, triggerEffects);
        ParseStringToCommands(nextTurn, nextTurnEffects);
    }

    /// <summary>
    /// 解析核心分流逻辑：
    /// 如果是纯数字 -> 解析成内部指令，放入 triggerList，由 OnTrigger 执行
    /// 如果是字符串 -> 解析成常规效果，放入 lifecycleList，由 OnBroken/OnMade 等被动执行
    /// </summary>
    private void ParseFieldWithIntSupport(string fieldValue, List<EffectCommand> triggerList, List<EffectCommand> lifecycleList, string fieldName)
    {
        triggerList.Clear();
        lifecycleList.Clear();
        
        if (string.IsNullOrEmpty(fieldValue)) return;

        // 检查是否为纯数字（且不包含括号，确保不是函数调用）
        if (!fieldValue.Contains("(") && !fieldValue.Contains(")") && int.TryParse(fieldValue.Trim(), out int intValue))
        {
            if (intValue == 0) return;

            EffectCommand command = new EffectCommand();
            switch (fieldName.ToLower())
            {
                case "made":
                    command.methodName = "_beMadeDirect";
                    command.parameters = new object[] { intValue };
                    break;
                case "broken":
                    command.methodName = "_beBrokenDirect";
                    command.parameters = new object[] { intValue };
                    break;
                case "added":
                    command.methodName = "_beAddedDirect";
                    command.parameters = new object[] { intValue, 1 };
                    break;
                default:
                    Debug.LogWarning($"[Card] 未知的int类型字段: {fieldName}");
                    return;
            }
            
            triggerList.Add(command);
            Debug.Log($"[Card] 字段 '{fieldName}' 解析为前置条件 (数字:{intValue})，将在 OnTrigger 触发");
        }
        else
        {
            // 字符串类型：使用原有的解析逻辑，作为被动效果
            ParseStringToCommands(fieldValue, lifecycleList);
            Debug.Log($"[Card] 字段 '{fieldName}' 解析为被动效果 (字符串:'{fieldValue}')，将在 On{char.ToUpper(fieldName[0]) + fieldName.Substring(1)} 触发");
        }
    }

    // 通用解析核心逻辑
    private void ParseStringToCommands(string effectSource, List<EffectCommand> targetList)
    {
        targetList.Clear();
        if (string.IsNullOrEmpty(effectSource)) return;

        string[] commands = effectSource.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string cmd in commands)
        {
            string trimmedCmd = cmd.Trim();
            int leftBracket = trimmedCmd.IndexOf('(');
            int rightBracket = trimmedCmd.LastIndexOf(')');

            if (leftBracket > 0 && rightBracket > leftBracket)
            {
                string methodName = trimmedCmd.Substring(0, leftBracket).Trim();
                string argsContent = trimmedCmd.Substring(leftBracket + 1, rightBracket - leftBracket - 1);
                
                string[] rawArgs = string.IsNullOrWhiteSpace(argsContent) 
                    ? new string[0] 
                    : argsContent.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                EffectCommand command = new EffectCommand();
                command.methodName = methodName;

                try
                {
                    command.parameters = CardEffect.Instance.ConvertParameters(methodName, rawArgs);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Card] 参数转换失败: 方法 '{methodName}', 参数 '{argsContent}'. 错误: {ex.Message}");
                    continue;
                }

                // 确保打断机制相关的核心方法始终排在前面
                if (methodName == "beMade" || methodName == "_beMadeDirect" || methodName == "beBroken" || methodName == "_beBrokenDirect")
                {
                    targetList.Insert(0, command);
                }
                else
                {
                    targetList.Add(command);
                }
            }
            else
            {
                Debug.LogWarning($"[Card] 格式解析错误: {trimmedCmd}");
            }
        }
    }

    private void ExecuteEffectList(List<EffectCommand> effectList)
    {
        if (effectList == null || effectList.Count == 0) return;
        CardEffect.Instance.ExecuteEffectList(this, effectList);
    }
    
    private int GetCardType(int id)
    {
        string s = Math.Abs(id).ToString();
        return s.Length >= 1 ? int.Parse(s[0].ToString()) : 0;
    }
    
    public void Add(int id, int num)
    {
        switch (id)
        {
            case 1 :AddNature1(num); break;
            case 2 :AddNature2(num); break;
            case 3 :AddNature3(num); break;
            default: Debug.LogError($"unknown id :{id}"); break;
        }
    }

    public void AddTo(int id, int num)
    {
        switch (id)
        {
            case 1 :AddNature1To(num); break;
            case 2 :AddNature2To(num); break;
            case 3 :AddNature3To(num); break;
            default: Debug.LogError($"unknown id :{id}"); break;
        }
    }

    public int GetNatureById(int id)
    {
        switch (id)
        {
            case 1 :return nature1;
            case 2 :return nature2;
            case 3 :return nature3;
        }
        return 0;
    }

    // --- 加法逻辑 ---
    private void AddNature1(int num)
    {
        if (nature1 == 0) return; 
        int target = nature1 + num;
        if (nature1 < 0) nature1 = target > 0 ? 0 : target;
        else if (nature1 > 0) nature1 = target < 0 ? 0 : target;
    }

    private void AddNature2(int num)
    {
        if (nature2 == 0) return;
        int target = nature2 + num;
        if (nature2 < 0) nature2 = target > 0 ? 0 : target;
        else if (nature2 > 0) nature2 = target < 0 ? 0 : target;
    }

    private void AddNature3(int num)
    {
        if (nature3 == 0) return;
        int target = nature3 + num;
        if (nature3 < 0) nature3 = target > 0 ? 0 : target;
        else if (nature3 > 0) nature3 = target < 0 ? 0 : target;
    }

    // --- 目标赋值逻辑 ---
    private void AddNature1To(int num)
    {
        if (nature1 == 0) return; 
        int safeNum = num;
        if (nature1 < 0 && num > 0) safeNum = 0;
        else if (nature1 > 0 && num < 0) safeNum = 0;
        if (nature1 < safeNum) nature1 = safeNum;
    }

    private void AddNature2To(int num)
    {
        if (nature2 == 0) return;
        int safeNum = num;
        if (nature2 < 0 && num > 0) safeNum = 0;
        else if (nature2 > 0 && num < 0) safeNum = 0;
        if (nature2 < safeNum) nature2 = safeNum; 
    }

    private void AddNature3To(int num)
    {
        if (nature3 == 0) return;
        int safeNum = num;
        if (nature3 < 0 && num > 0) safeNum = 0;
        else if (nature3 > 0 && num < 0) safeNum = 0;
        if (nature3 < safeNum) nature3 = safeNum;
    }
    
    // ==========================================
    // 生命周期回调被动触发：只有当字段为字符串时，才会触发这里的效果
    // ==========================================
    public void OnMade() => ExecuteEffectList(madeEffects);
    public void OnBroken() => ExecuteEffectList(brokenEffects);
    public void OnAdded() => ExecuteEffectList(addedEffects);
    public void OnBuffUpdate() => ExecuteEffectList(buffEffects);

    // ==========================================
    // 主动打出触发：专门执行前置条件（纯数字配置）和Trigger常规配置
    // ==========================================
    public void OnTrigger()
    {
        Debug.Log($"[Card] OnTrigger: 卡牌 {name} (ID:{id}) 开始执行效果");

        // 辅助函数：执行效果链并检查条件是否失败
        bool ExecuteEffectsAndCheckCondition(List<EffectCommand> effects, string effectType)
        {
            if (effects == null || effects.Count == 0) return false; 

            Debug.Log($"[Card] OnTrigger: 正在核验前置条件 [{effectType}]");
            ExecuteEffectList(effects);

            if (CardEffect.Instance != null && CardEffect.Instance.IsConditionFailed(this.id))
            {
                Debug.Log($"[Card] OnTrigger: {effectType} 前置条件失败(或玩家取消)，停止打出该卡牌");
                return true; 
            }
            return false; 
        }

        // 1. 验证 made 前置条件 (仅数字配置会进入此处)
        if (ExecuteEffectsAndCheckCondition(triggerMadeEffects, "Made(数字配置)")) return;

        // 2. 验证 broken 前置条件 (仅数字配置会进入此处)
        if (ExecuteEffectsAndCheckCondition(triggerBrokenEffects, "Broken(数字配置)")) return;

        // 3. 验证 added 前置条件 (仅数字配置会进入此处)
        if (ExecuteEffectsAndCheckCondition(triggerAddedEffects, "Added(数字配置)")) return;

        // 4. 前置条件通过，开始执行真正的 Trigger 效果
        if (triggerEffects != null && triggerEffects.Count > 0)
        {
            ExecuteEffectList(triggerEffects);
        }

        // 5. 将卡牌的三种属性添加到DataManager
        if (CardEffect.Instance != null)
        {
            CardEffect.Instance.SetCallerCard(this);
            CardEffect.Instance.Execute("addCardNatureToDataManager", new object[0]);
        }

        // 6. 添加OnNextTurn监听器
        if (CardEffect.Instance != null && !CardEffect.Instance.IsConditionFailed(this.id))
        {
            DayManager.Instance.GetNextDayEvent().AddListener(OnNextTurn);
        }
    }

    public void OnNextTurn() => ExecuteEffectList(nextTurnEffects);

    public string GetParsedDescription()
    {
        if (string.IsNullOrEmpty(description)) return string.Empty;

        return Regex.Replace(description, @"\{([^}]+)\}", match =>
        {
            if (match.Success && match.Groups.Count > 1)
            {
                string fieldName = match.Groups[1].Value;
                return GetFieldValue(fieldName);
            }
            return match.Value;
        });
    }

    private string GetFieldValue(string fieldName)
    {
        switch (fieldName.ToLower())
        {
            case "id": return id.ToString();
            case "nature1": return nature1.ToString();
            case "nature2": return nature2.ToString();
            case "nature3": return nature3.ToString();
            case "name": return name ?? string.Empty;
            case "sale": return sale.ToString();
            case "made": return made ?? string.Empty;
            case "broken": return broken ?? string.Empty;
            case "added": return added ?? string.Empty;
            case "buff": return buff ?? string.Empty;
            case "trigger": return trigger ?? string.Empty;
            case "nextturn": return nextTurn ?? string.Empty;
            case "description": return description ?? string.Empty;
            default: return string.Empty;
        }
    }

    public bool TryModifyAddedValue(int delta)
    {
        if (string.IsNullOrEmpty(added)) return false;

        // 检查是否为纯数字类型
        if (!added.Contains("(") && !added.Contains(")") && int.TryParse(added.Trim(), out int currentValue))
        {
            int newValue = currentValue + delta;
            added = newValue.ToString();

            // 重新解析 added 字段，更新 triggerAddedEffects 列表
            ParseFieldWithIntSupport(added, triggerAddedEffects, addedEffects, "added");

            Debug.Log($"[Card] 成功修改 added 字段数值: {currentValue} → {newValue}");
            return true;
        }
        
        Debug.LogWarning($"[Card] added 字段为字符串类型，无法作为纯数字修改。当前值: '{added}'");
        return false;
    }
}