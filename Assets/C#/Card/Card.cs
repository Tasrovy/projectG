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

public class Card : MonoBehaviour
{
    public int id;
    public int cardProperty1;
    public int cardProperty2;
    public int cardProperty3;
    public string cardName;
    public string cardDescription;
    public string originEffect;
    public List<EffectCommand> actualEffects = new List<EffectCommand>();

    public void InitCard(CardData cardData)
    {
        id = cardData.id;
        cardProperty1 = cardData.cardProperty1;
        cardProperty2 = cardData.cardProperty2;
        cardProperty3 = cardData.cardProperty3;
        cardName = cardData.cardName;
        cardDescription = cardData.cardDescription;
        originEffect = cardData.effect;
    }
    
    private void ParseEffectString()
    {
        actualEffects.Clear();
        if (string.IsNullOrEmpty(originEffect)) return;

        // 1. 支持多条指令，用分号 ';' 分隔。例如 "Add(1,2); Heal(10)"
        string[] commands = originEffect.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

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