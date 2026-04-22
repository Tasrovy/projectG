using System.Collections.Generic;

public sealed class CardEffectPlan
{
    public List<EffectCommand> madeEffects = new List<EffectCommand>();
    public List<EffectCommand> brokenEffects = new List<EffectCommand>();
    public List<EffectCommand> addedEffects = new List<EffectCommand>();
    public List<EffectCommand> triggerMadeEffects = new List<EffectCommand>();
    public List<EffectCommand> triggerBrokenEffects = new List<EffectCommand>();
    public List<EffectCommand> triggerAddedEffects = new List<EffectCommand>();
    public List<EffectCommand> buffEffects = new List<EffectCommand>();
    public List<EffectCommand> triggerEffects = new List<EffectCommand>();
    public List<EffectCommand> nextTurnEffects = new List<EffectCommand>();
}
