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
    private List<EffectCommand> madeEffects = new List<EffectCommand>();
    private List<EffectCommand> brokenEffects = new List<EffectCommand>();
    private List<EffectCommand> addedEffects = new List<EffectCommand>();
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
        ParseFieldWithIntSupport(made, madeEffects, "made");
        ParseFieldWithIntSupport(broken, brokenEffects, "broken");
        ParseFieldWithIntSupport(added, addedEffects, "added");
        ParseStringToCommands(buff, buffEffects);
        ParseStringToCommands(trigger, triggerEffects);
        ParseStringToCommands(nextTurn, nextTurnEffects);
    }

    // 解析支持int类型的字段（made、broken、added）
    private void ParseFieldWithIntSupport(string fieldValue, List<EffectCommand> targetList, string fieldName)
    {
        targetList.Clear();
        if (string.IsNullOrEmpty(fieldValue)) return;

        // 检查是否为纯数字（int类型）且不包含括号（确保不是函数调用）
        if (!fieldValue.Contains("(") && !fieldValue.Contains(")") && int.TryParse(fieldValue.Trim(), out int intValue))
        {
            // int类型：根据字段名生成对应的效果命令
            EffectCommand command = new EffectCommand();

            switch (fieldName.ToLower())
            {
                case "made":
                    command.methodName = "beMade";
                    command.parameters = new object[] { intValue };
                    // beMade需要放在第一位
                    targetList.Insert(0, command);
                    break;

                case "broken":
                    command.methodName = "beBroken";
                    command.parameters = new object[] { intValue };
                    // beBroken需要放在第一位
                    targetList.Insert(0, command);
                    break;

                case "added":
                    command.methodName = "beAdded";
                    command.parameters = new object[] { intValue, 1 };
                    targetList.Add(command);
                    break;

                default:
                    Debug.LogWarning($"[Card] 未知的int类型字段: {fieldName}");
                    break;
            }

            Debug.Log($"[Card] 字段 '{fieldName}' 解析为int类型: {intValue}, 生成效果: {command.methodName}");
        }
        else
        {
            // string类型：使用原有的解析逻辑
            ParseStringToCommands(fieldValue, targetList);
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
                    // 跳过这个无效的命令
                    continue;
                }

                // --- 核心修改：确保 beMade 和 beBroken 始终在第一位 ---
                if (methodName == "beMade" || methodName == "beBroken")
                {
                    // 插入到列表的第 0 个位置
                    targetList.Insert(0, command);
                }
                else
                {
                    // 普通指令，按顺序添加到末尾
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

        // 将效果链交给CardEffect统一执行，支持异步阻塞
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
            case 1 :AddNature1(num);
                break;
            case 2 :AddNature2(num);
                break;
            case 3 :AddNature3(num);
                break;
            default:
                Debug.LogError($"unknown id :{id}");
                break;
        }
    }

    public void AddTo(int id, int num)
    {
        switch (id)
        {
            case 1 :AddNature1To(num);
                break;
            case 2 :AddNature2To(num);
                break;
            case 3 :AddNature3To(num);
                break;
            default:
                Debug.LogError($"unknown id :{id}");
                break;
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
// --- 核心加法逻辑 (AddNature) ---

    private void AddNature1(int num)
    {
        if (nature1 == 0) return; // 原数值为0不改变
        int target = nature1 + num;
        
        // 如果原值小于0，结果限制在 [负无穷, 0]
        if (nature1 < 0) nature1 = target > 0 ? 0 : target;
        // 如果原值大于0，结果限制在 [0, 正无穷]
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

    // --- 目标赋值逻辑 (AddNatureTo) ---
    // 规则：仅当目标值比当前值大时赋值，但受 0 边界限制

    private void AddNature1To(int num)
    {
        if (nature1 == 0) return; // 原数值为0不改变
        
        // 先处理 0 边界限制：不能跨越正负
        int safeNum = num;
        if (nature1 < 0 && num > 0) safeNum = 0;
        else if (nature1 > 0 && num < 0) safeNum = 0;

        // 执行原有的 "仅当目标值更大时更新" 逻辑
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
    
    public void OnMade() => ExecuteEffectList(madeEffects);
    public void OnBroken() => ExecuteEffectList(brokenEffects);
    public void OnAdded() => ExecuteEffectList(addedEffects);
    public void OnBuffUpdate() => ExecuteEffectList(buffEffects);

    public void OnTrigger()
    {
        ExecuteEffectList(triggerEffects);

        // 基本效果：将卡牌的三种属性添加到DataManager（在triggerEffects之后执行，以便使用可能被修改的属性值）
        if (CardEffect.Instance != null)
        {
            CardEffect.Instance.SetCallerCard(this);
            CardEffect.Instance.Execute("addCardNatureToDataManager", new object[0]);
        }

        // 检查卡牌是否条件失败（如beMade/beBroken条件不满足）
        // 注意：条件失败的检查在addCardNatureToDataManager中已经处理，但添加监听器需要单独检查
        if (CardEffect.Instance != null && CardEffect.Instance.IsConditionFailed(this.id))
        {
            Debug.Log($"[Card] OnTrigger: 卡牌 {name} (ID:{id}) 条件失败，不添加OnNextTurn监听器");
            // 清除条件失败标记，避免影响后续使用
            CardEffect.Instance.ClearConditionFailed(this.id);
            return;
        }

        DayManager.Instance.GetNextDayEvent().AddListener(OnNextTurn);
    }

    public void OnNextTurn() => ExecuteEffectList(nextTurnEffects);

    /// <summary>
    /// 获取解析后的描述文本，将{变量名}替换为实际值
    /// </summary>
    public string GetParsedDescription()
    {
        if (string.IsNullOrEmpty(description))
            return string.Empty;

        // 使用正则表达式替换所有{变量名}模式
        return Regex.Replace(description, @"\{([^}]+)\}", match =>
        {
            if (match.Success && match.Groups.Count > 1)
            {
                string fieldName = match.Groups[1].Value;
                return GetFieldValue(fieldName);
            }
            return match.Value; // 如果匹配失败，返回原始文本
        });
    }

    /// <summary>
    /// 根据字段名获取对应的值
    /// </summary>
    private string GetFieldValue(string fieldName)
    {
        switch (fieldName.ToLower())
        {
            case "id":
                return id.ToString();
            case "nature1":
                return nature1.ToString();
            case "nature2":
                return nature2.ToString();
            case "nature3":
                return nature3.ToString();
            case "name":
                return name ?? string.Empty;
            case "sale":
                return sale.ToString();
            case "made":
                return made ?? string.Empty;
            case "broken":
                return broken ?? string.Empty;
            case "added":
                return added ?? string.Empty;
            case "buff":
                return buff ?? string.Empty;
            case "trigger":
                return trigger ?? string.Empty;
            case "nextturn":
                return nextTurn ?? string.Empty;
            case "description":
                return description ?? string.Empty;
            default:
                // 如果字段名未知，返回原始占位符（或者空字符串）
                Debug.LogWarning($"[Card] 未知的描述变量: {{{fieldName}}}");
                return string.Empty;
        }
    }

    /// <summary>
    /// 尝试修改added字段的数值（仅当added为int类型时有效）
    /// </summary>
    /// <param name="delta">要增加/减少的数值</param>
    /// <returns>是否成功修改</returns>
    public bool TryModifyAddedValue(int delta)
    {
        if (string.IsNullOrEmpty(added))
        {
            Debug.LogWarning($"[Card] TryModifyAddedValue: added字段为空，无法修改");
            return false;
        }

        // 检查是否为int类型（纯数字且不包含括号）
        if (!added.Contains("(") && !added.Contains(")") && int.TryParse(added.Trim(), out int currentValue))
        {
            // 计算新值
            int newValue = currentValue + delta;

            // 更新added字段
            added = newValue.ToString();

            // 重新解析added效果
            ParseFieldWithIntSupport(added, addedEffects, "added");

            Debug.Log($"[Card] TryModifyAddedValue: 成功修改added字段 {currentValue} → {newValue} (delta: {delta})");
            return true;
        }
        else
        {
            // added字段为string类型，无法直接修改数值
            Debug.LogWarning($"[Card] TryModifyAddedValue: added字段为string类型，无法直接修改数值。当前值: '{added}'");
            return false;
        }
    }
}