using UnityEngine;
using System;

public enum CardPlayMode
{
    NormalPlay,   // 自由打牌阶段
    EffectSelect  // 因卡牌效果需要强制选牌阶段
}

/// <summary>
/// 卡牌操作解析器 - 监听UI事件，执行打牌逻辑或效果回调
/// </summary>
public class CardActionResolver : Singleton<CardActionResolver>
{
    public CardPlayMode currentMode = CardPlayMode.NormalPlay;

    // 存储效果选牌的回调
    private Action<Card> _onEffectConfirm;
    private Action _onEffectCancel;

    private void Start()
    {
        // 订阅 UI 选择器的纯净事件
        if (CardSelector.Instance != null)
        {
            CardSelector.Instance.OnSubmitEvent += HandleSubmit;
            CardSelector.Instance.OnCancelEvent += HandleCancel;
        }

        ResetToNormalMode();
    }

    private void OnDestroy()
    {
        if (CardSelector.Instance != null)
        {
            CardSelector.Instance.OnSubmitEvent -= HandleSubmit;
            CardSelector.Instance.OnCancelEvent -= HandleCancel;
        }
    }

    /// <summary>
    /// 进入正常自由打牌模式
    /// </summary>
    public void ResetToNormalMode()
    {
        currentMode = CardPlayMode.NormalPlay;
        _onEffectConfirm = null;
        _onEffectCancel = null;

        if (CardSelector.Instance != null)
        {
            CardSelector.Instance.SetSubmitButtonText("打出");
            CardSelector.Instance.SetAllCardsInteractable(true);
        }
    }

    /// <summary>
    /// 供各种 Helper 调用的特殊选牌接口
    /// </summary>
    public void StartEffectSelection(Action<Card> onConfirm, Action onCancel)
    {
        currentMode = CardPlayMode.EffectSelect;
        _onEffectConfirm = onConfirm;
        _onEffectCancel = onCancel;

        if (CardSelector.Instance != null)
        {
            CardSelector.Instance.SetSubmitButtonText("提交");
            CardSelector.Instance.DeselectCurrent(); // 强制取消玩家当前正捏着的牌
            // 确保场上卡牌处于可点击状态
            CardSelector.Instance.SetAllCardsInteractable(true);
        }
    }

    /// <summary>
    /// 接收玩家点击“打出/提交”按钮
    /// </summary>
    private void HandleSubmit(Card selectedCard)
    {
        if (currentMode == CardPlayMode.NormalPlay)
        {
            Debug.Log($"[Resolver] 💥 玩家正常打出了卡牌: {selectedCard?.name}");
            
            // --- 正常出牌逻辑 ---
            selectedCard.OnTrigger();
            CardManager.Instance.BreakCard(selectedCard);
            // 1. 从手牌数据中移除
            ///CardManager.Instance.RemoveCardFromHand(selectedCard);
            
            // 2. 触发这张牌自身的效果
            // selectedCard.OnTrigger(); 或者 CardEffect.Instance.Execute(selectedCard);
        }
        else if (currentMode == CardPlayMode.EffectSelect)
        {
            // --- 特殊效果选牌逻辑 ---
            Debug.Log($"[Resolver] ✅ 玩家在效果中提交了卡牌: {selectedCard?.name}");
            
            // 【重要细节】：必须先暂存回调，然后立刻重置模式，最后再 Invoke。
            // 为什么？因为像 ShengZhang 这样的技能会在 Invoke 内部再次调用 StartEffectSelection。
            // 如果先 Invoke 再 Reset，会把 ShengZhang 第二次开启的选牌状态给抹除掉！
            var confirmAction = _onEffectConfirm;
            ResetToNormalMode(); 
            
            confirmAction?.Invoke(selectedCard);
        }
    }

    /// <summary>
    /// 接收玩家点击“取消”按钮
    /// </summary>
    private void HandleCancel()
    {
        if (currentMode == CardPlayMode.NormalPlay)
        {
            Debug.Log("[Resolver] 玩家取消了打出准备，卡牌退回原位。");
            // UI已经被 CardSelector 还原了，这里无事可做
        }
        else if (currentMode == CardPlayMode.EffectSelect)
        {
            Debug.Log("[Resolver] ❌ 玩家取消了特殊选牌。");
            
            var cancelAction = _onEffectCancel;
            ResetToNormalMode();
            
            cancelAction?.Invoke();
        }
    }
}