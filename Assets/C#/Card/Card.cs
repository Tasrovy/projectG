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
        foreach (var effect in actualEffects)
        {
            // 3. 交给单例去执行
            CardEffect.Instance.Execute(effect.methodName, effect.parameters);
        }
    }
}