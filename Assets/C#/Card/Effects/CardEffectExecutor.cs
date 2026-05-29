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

    // ===================== Snapshot / Rollback =====================

    private struct StateSnapshot
    {
        public List<Card> handCards;
        public List<Card> deckCards;
        public int dataNature1, dataNature2, dataNature3;
        public int dataMoney, dataExtraCharm;
        public float dataEffect1, dataEffect2, dataEffect3;
    }

    private bool _hasSnapshot;
    private StateSnapshot _snapshot;

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
            if (!_hasSnapshot)
            {
                TakeSnapshot();
            }
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

    // ===================== Snapshot =====================

    public void TakeSnapshot(bool force = false)
    {
        if (_hasSnapshot)
        {
            if (!force)
            {
                Debug.Log("[Snapshot] 已有快照，跳过重复拍摄");
                return;
            }
            Debug.Log("[Snapshot] 强制覆盖快照");
        }

        _hasSnapshot = true;

        // 手牌深拷贝
        _snapshot.handCards = new List<Card>();
        foreach (var card in CardManager.Instance.cardInHand)
        {
            var copy = new Card();
            copy.InitCard(card);
            _snapshot.handCards.Add(copy);
        }

        // 牌堆深拷贝
        _snapshot.deckCards = new List<Card>();
        foreach (var card in CardManager.Instance.cardSet)
        {
            var copy = new Card();
            copy.InitCard(card);
            _snapshot.deckCards.Add(copy);
        }

        // DataManager 值拷贝
        var dm = DataManager.Instance;
        _snapshot.dataNature1 = dm.nature1;
        _snapshot.dataNature2 = dm.nature2;
        _snapshot.dataNature3 = dm.nature3;
        _snapshot.dataMoney = dm.MoneyNum;
        _snapshot.dataExtraCharm = dm.extraCharm;
        _snapshot.dataEffect1 = dm.currNature1Effect;
        _snapshot.dataEffect2 = dm.currNature2Effect;
        _snapshot.dataEffect3 = dm.currNature3Effect;

        Debug.Log($"[Snapshot] 已记录快照: 手牌{_snapshot.handCards.Count}张, 牌堆{_snapshot.deckCards.Count}张");
    }

    private void RestoreSnapshot()
    {
        if (!_hasSnapshot) return;

        Debug.Log("[Snapshot] 回滚快照...");

        // 恢复手牌
        CardManager.Instance.cardInHand.Clear();
        foreach (var card in _snapshot.handCards)
        {
            var restored = new Card();
            restored.InitCard(card);
            CardManager.Instance.cardInHand.Add(restored);
        }

        // 恢复牌堆
        CardManager.Instance.cardSet.Clear();
        foreach (var card in _snapshot.deckCards)
        {
            var restored = new Card();
            restored.InitCard(card);
            CardManager.Instance.cardSet.Add(restored);
        }

        // 恢复 DataManager
        var dm = DataManager.Instance;
        dm.nature1 = _snapshot.dataNature1;
        dm.nature2 = _snapshot.dataNature2;
        dm.nature3 = _snapshot.dataNature3;
        dm.MoneyNum = _snapshot.dataMoney;
        dm.extraCharm = _snapshot.dataExtraCharm;
        dm.currNature1Effect = _snapshot.dataEffect1;
        dm.currNature2Effect = _snapshot.dataEffect2;
        dm.currNature3Effect = _snapshot.dataEffect3;

        _hasSnapshot = false;

        CardManager.Instance.NotifyDeckOrHandChanged();
        if (PropertiesShow.Instance != null)
            PropertiesShow.Instance.UpdatePropertiesShow();
    }

    /// <summary>
    /// 立即回滚：检测到卡牌不足时直接恢复快照并清空效果链
    /// </summary>
    private void RollbackImmediate()
    {
        RestoreSnapshot();
        _effectChainQueue.Clear();
        _isExecutingChain = false;
        _waitingForAsync = false;
        _currentChainCard = null;
        _currentChainEffects = null;
        _currentEffectIndex = 0;
    }

    // ===================== Chain Execution =====================

    private void StartExecutingNextChain()
    {
        if (_effectChainQueue.Count == 0)
        {
            _isExecutingChain = false;
            _hasSnapshot = false;

            if (PropertiesShow.Instance != null)
            {
                PropertiesShow.Instance.UpdatePropertiesShow();
            }
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

        // === 条件失败 → 回滚快照 ===
        if (_currentChainCard != null && _owner.IsConditionFailed(_currentChainCard))
        {
            Debug.Log("[CardEffect] 检测到条件失败，执行回滚...");

            // 快照在 BreakCard 之前拍摄，回滚自然恢复所有手牌和牌堆
            RestoreSnapshot();

            // 清空尚未执行的子效果链（如被拆卸牌的 OnBroken 效果）
            _effectChainQueue.Clear();
            _isExecutingChain = false;
            _waitingForAsync = false;
            _currentChainCard = null;
            _currentChainEffects = null;
            _currentEffectIndex = 0;

            if (PropertiesShow.Instance != null)
            {
                PropertiesShow.Instance.UpdatePropertiesShow();
            }
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
        if (effect.methodName == "addWithSame")
        {
            if (CardManager.Instance.cardInHand.Count <= 0)
            {
                Debug.Log($"{_currentChainCard.name} 技能发动失败：手牌中没有可供选择的牌。");
                RollbackImmediate();
                return;
            }

            _waitingForAsync = true;
        }
        else if (effect.methodName == "beMade" || effect.methodName == "_beMadeDirect" ||
            effect.methodName == "beAdded" || effect.methodName == "_beAddedDirect" ||
            effect.methodName == "beAddedTo" || effect.methodName == "addNatureAtSumIf")
        {
            int giftCardNum = CardManager.Instance.GetGiftCardNum();
            if (giftCardNum <= 0)
            {
                Debug.Log($"{_currentChainCard.name} 技能发动失败：手牌中没有合法的目标牌可供选择！");
                RollbackImmediate();
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
                RollbackImmediate();
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
