using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EffectCommand
{
    public string methodName;
    public object[] parameters;
}

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
    public string description;
    public string buff;
    public string trigger;
    public List<EffectCommand> actualEffects = new List<EffectCommand>();

    public void InitCard(CardData cardData)
    {
        id = cardData.id;
        nature1 = cardData.nature1;
        nature2 = cardData.nature2;
        nature3 = cardData.nature3;
        name = cardData.name;
        description = cardData.description;
        buff = cardData.buff;
        sale = cardData.sale;
        trigger = cardData.trigger;
        actualEffects.Clear();
        ParseEffectString();
    }
    
    public void InitCard(Card cardData)
    {
        id = cardData.id;
        nature1 = cardData.nature1;
        nature2 = cardData.nature2;
        nature3 = cardData.nature3;
        name = cardData.name;
        description = cardData.description;
        buff = cardData.buff;
        sale = cardData.sale;
        trigger = cardData.trigger;
        actualEffects.Clear();
        ParseEffectString();
    }
    
    private void ParseEffectString()
    {
        actualEffects.Clear();
        if (string.IsNullOrEmpty(trigger)) return;

        // 1. 支持多条指令，用分号 ';' 分隔。例如 "Add(1,2); Heal(10)"
        string[] commands = trigger.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string cmd in commands)
        {
            string trimmedCmd = cmd.Trim();
            
            // 2. 找到左括号和右括号的位置
            int leftBracket = trimmedCmd.IndexOf('(');
            int rightBracket = trimmedCmd.LastIndexOf(')');

            if (leftBracket > 0 && rightBracket > leftBracket)
            {
                // 提取方法名: "Add"
                string methodName = trimmedCmd.Substring(0, leftBracket).Trim();
                
                // 提取参数部分: "1, 2"
                string argsContent = trimmedCmd.Substring(leftBracket + 1, rightBracket - leftBracket - 1);
                
                // 分割参数
                string[] rawArgs = string.IsNullOrWhiteSpace(argsContent) 
                    ? new string[0] 
                    : argsContent.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // 3. 构建并存储指令
                EffectCommand command = new EffectCommand();
                command.methodName = methodName;
                command.parameters = CardEffect.Instance.ConvertParameters(methodName, rawArgs);

                actualEffects.Add(command);
            }
            else
            {
                Debug.LogWarning($"[Card] 效果格式错误: {trimmedCmd}。应为 Method(arg1, arg2)");
            }
        }
    }

    public void Effect()
    {
        CardEffect.Instance.SetCallerCard(this);
        foreach (var effect in actualEffects)
        {
            // 3. 交给单例去执行
            CardEffect.Instance.Execute(effect.methodName, effect.parameters);
        }
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
}