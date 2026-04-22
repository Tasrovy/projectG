using System.Collections.Generic;
using UnityEngine;

public sealed class CardRuntime
{
    private static int s_triggerInvokeSerial = 0;

    private readonly Card _card;
    private readonly CardEffectPlan _plan;

    public CardRuntime(Card card, CardEffectPlan plan)
    {
        _card = card;
        _plan = plan;
    }

    public void OnMade() => ExecuteEffectList(_plan.madeEffects);
    public void OnBroken() => ExecuteEffectList(_plan.brokenEffects);
    public void OnAdded() => ExecuteEffectList(_plan.addedEffects);
    public void OnBuffUpdate() => ExecuteEffectList(_plan.buffEffects);
    public void OnNextTurn() => ExecuteEffectList(_plan.nextTurnEffects);

    public void OnTrigger()
    {
        int serial = ++s_triggerInvokeSerial;
        int cardRef = _card.GetHashCode();
        Debug.Log($"[Card][OnTrigger][Enter] serial={serial}, frame={Time.frameCount}, time={Time.time:F3}, cardRef={cardRef}, id={_card.id}, name={_card.name}");
        Debug.Log($"[Card][OnTrigger][Effects] serial={serial}, made={_plan.triggerMadeEffects?.Count ?? 0}, broken={_plan.triggerBrokenEffects?.Count ?? 0}, added={_plan.triggerAddedEffects?.Count ?? 0}, trigger={_plan.triggerEffects?.Count ?? 0}");
        Debug.Log($"[Card][OnTrigger][Stack] serial={serial}\n{StackTraceUtility.ExtractStackTrace()}");

        if (CardEffect.Instance != null)
        {
            CardEffect.Instance.ClearConditionFailed(_card);
        }
        Debug.Log($"[Card] OnTrigger: 卡牌 {_card.name} (ID:{_card.id}) 开始执行效果");

        List<EffectCommand> triggerExecutionPlan = new List<EffectCommand>();

        if (_plan.triggerMadeEffects != null && _plan.triggerMadeEffects.Count > 0)
        {
            Debug.Log("[Card] OnTrigger: 加入前置条件 [Made(数字配置)]");
            triggerExecutionPlan.AddRange(_plan.triggerMadeEffects);
        }

        if (_plan.triggerBrokenEffects != null && _plan.triggerBrokenEffects.Count > 0)
        {
            Debug.Log("[Card] OnTrigger: 加入前置条件 [Broken(数字配置)]");
            triggerExecutionPlan.AddRange(_plan.triggerBrokenEffects);
        }

        if (_plan.triggerAddedEffects != null && _plan.triggerAddedEffects.Count > 0)
        {
            Debug.Log("[Card] OnTrigger: 加入前置条件 [Added(数字配置)]");
            triggerExecutionPlan.AddRange(_plan.triggerAddedEffects);
        }

        if (_plan.triggerEffects != null && _plan.triggerEffects.Count > 0)
        {
            triggerExecutionPlan.AddRange(_plan.triggerEffects);
        }

        triggerExecutionPlan.Add(new EffectCommand
        {
            methodName = "_onTriggerFinalize",
            parameters = new object[0]
        });

        Debug.Log($"[Card][OnTrigger][Plan] serial={serial}, total={triggerExecutionPlan.Count}");
        for (int i = 0; i < triggerExecutionPlan.Count; i++)
        {
            EffectCommand cmd = triggerExecutionPlan[i];
            string paramText = (cmd.parameters == null || cmd.parameters.Length == 0)
                ? "[]"
                : $"[{string.Join(", ", cmd.parameters)}]";
            Debug.Log($"[Card][OnTrigger][PlanItem] serial={serial}, index={i}, method={cmd.methodName}, params={paramText}");
        }

        ExecuteEffectList(triggerExecutionPlan);
    }

    private void ExecuteEffectList(List<EffectCommand> effectList)
    {
        if (effectList == null || effectList.Count == 0) return;
        CardEffect.Instance.ExecuteEffectList(_card, effectList);
    }
}
