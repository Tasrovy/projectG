using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CardEffectPlanParser
{
    public void ParseAll(Card card, CardEffectPlan plan)
    {
        if (card == null || plan == null) return;

        ParseFieldWithIntSupport(card.made, plan.triggerMadeEffects, plan.madeEffects, "made");
        ParseFieldWithIntSupport(card.broken, plan.triggerBrokenEffects, plan.brokenEffects, "broken");
        ParseFieldWithIntSupport(card.added, plan.triggerAddedEffects, plan.addedEffects, "added");

        ParseStringToCommands(card.buff, plan.buffEffects);
        ParseStringToCommands(card.trigger, plan.triggerEffects);
        ParseStringToCommands(card.nextTurn, plan.nextTurnEffects);
    }

    public void ReparseAdded(Card card, CardEffectPlan plan)
    {
        if (card == null || plan == null) return;
        ParseFieldWithIntSupport(card.added, plan.triggerAddedEffects, plan.addedEffects, "added");
    }

    private void ParseFieldWithIntSupport(string fieldValue, List<EffectCommand> triggerList, List<EffectCommand> lifecycleList, string fieldName)
    {
        triggerList.Clear();
        lifecycleList.Clear();

        if (string.IsNullOrEmpty(fieldValue)) return;

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
            return;
        }

        ParseStringToCommands(fieldValue, lifecycleList);
        Debug.Log($"[Card] 字段 '{fieldName}' 解析为被动效果 (字符串:'{fieldValue}')，将在 On{char.ToUpper(fieldName[0]) + fieldName.Substring(1)} 触发");
    }

    private void ParseStringToCommands(string effectSource, List<EffectCommand> targetList)
    {
        targetList.Clear();
        if (string.IsNullOrEmpty(effectSource)) return;

        string[] commands = effectSource.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string cmd in commands)
        {
            string trimmedCmd = cmd.Trim();
            int leftBracket = trimmedCmd.IndexOf('(');
            int rightBracket = trimmedCmd.LastIndexOf(')');

            if (leftBracket <= 0 || rightBracket <= leftBracket)
            {
                Debug.LogWarning($"[Card] 格式解析错误: {trimmedCmd}");
                continue;
            }

            string methodName = trimmedCmd.Substring(0, leftBracket).Trim();
            string argsContent = trimmedCmd.Substring(leftBracket + 1, rightBracket - leftBracket - 1);
            string[] rawArgs = string.IsNullOrWhiteSpace(argsContent)
                ? Array.Empty<string>()
                : argsContent.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

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

            if (methodName == "beMade" || methodName == "_beMadeDirect" || methodName == "beBroken" || methodName == "_beBrokenDirect")
            {
                targetList.Insert(0, command);
            }
            else
            {
                targetList.Add(command);
            }
        }
    }
}
