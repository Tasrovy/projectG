using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CardEffectExecutor
{
    private readonly CardEffect _owner;
    private readonly Queue<(Card card, List<EffectCommand> effects)> _effectChainQueue = new Queue<(Card, List<EffectCommand>)>();

    private bool _isExecutingChain = false;
    private Card _currentChainCard;
    private List<EffectCommand> _currentChainEffects;
    private int _currentEffectIndex;
    private bool _waitingForAsync = false;

    public CardEffectExecutor(CardEffect owner)
    {
        _owner = owner;
    }

    public bool IsWaitingForAsync => _waitingForAsync;

    public void ExecuteEffectList(Card card, List<EffectCommand> effects)
    {
        if (effects == null || effects.Count == 0) return;

        _effectChainQueue.Enqueue((card, effects));

        if (!_isExecutingChain)
        {
            StartExecutingNextChain();
        }
    }

    public void OnSelectCardEnd(object obj)
    {
        if (_waitingForAsync)
        {
            Debug.Log("[CardEffect] 玩家已完成UI操作，恢复效果链执行...");
            _waitingForAsync = false;
            ExecuteNextEffect();
        }
    }

    private void StartExecutingNextChain()
    {
        if (_effectChainQueue.Count == 0)
        {
            _isExecutingChain = false;
            return;
        }

        var (card, effects) = _effectChainQueue.Dequeue();
        _currentChainCard = card;
        _currentChainEffects = effects;
        _currentEffectIndex = 0;
        _isExecutingChain = true;
        _waitingForAsync = false;

        ExecuteNextEffect();
    }

    private void ExecuteNextEffect()
    {
        if (_waitingForAsync) return;

        if (_currentChainCard != null && _owner.IsConditionFailed(_currentChainCard))
        {
            StartExecutingNextChain();
            return;
        }

        if (_currentChainEffects == null || _currentEffectIndex >= _currentChainEffects.Count)
        {
            StartExecutingNextChain();
            return;
        }

        var effect = _currentChainEffects[_currentEffectIndex];
        _currentEffectIndex++;

        _owner.SetCallerCard(_currentChainCard);
        ExecuteSingleEffect(effect);
    }

    private void ExecuteSingleEffect(EffectCommand effect)
    {
        if (effect.methodName == "beMade" || effect.methodName == "_beMadeDirect" ||
            effect.methodName == "beAdded" || effect.methodName == "_beAddedDirect" ||
            effect.methodName == "beAddedTo" || effect.methodName == "addNatureAtSumIf")
        {
            int giftCardNum = CardManager.Instance.GetGiftCardNum();
            if (giftCardNum <= 0)
            {
                Debug.Log($"{_currentChainCard.name} 技能发动失败：手牌中没有合法的目标牌可供选择！");
                CardSubmitHelper.Instance.RestoreCallerCardOnInvalidTarget();
                StartExecutingNextChain();
                return;
            }

            _waitingForAsync = true;
        }
        else if (effect.methodName == "beBroken" || effect.methodName == "_beBrokenDirect")
        {
            int threshold = 0;
            if (effect.parameters != null && effect.parameters.Length > 0)
            {
                threshold = Convert.ToInt32(effect.parameters[0]);
            }

            int giftCardNum = CardManager.Instance.GetGiftCardNum();
            if (giftCardNum < threshold)
            {
                Debug.Log($"{_currentChainCard.name} 消耗失败：礼品卡不足，停止执行当前效果链");
                CardSubmitHelper.Instance.RestoreCallerCardOnInvalidTarget();
                StartExecutingNextChain();
                return;
            }

            _waitingForAsync = true;
        }

        try
        {
            _owner.Execute(effect.methodName, effect.parameters);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CardEffect] 执行效果 {effect.methodName} 时出错: {ex.InnerException?.Message ?? ex.Message}");
            _waitingForAsync = false;
        }

        if (!_waitingForAsync)
        {
            ExecuteNextEffect();
        }
    }
}
