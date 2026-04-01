using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InDeckState : stateBase, ICardState
{
    public int deckID;
    public bool CanTransitionTo(CardState newState)
    {
        return true;
    }

    public void EnterState()
    {
        CanvasGroup.alpha = 0;
        transform.position =new Vector3(-999, -999, 0);
        CardSum.Instance.DeckCardList.Add(this.gameObject);
        deckID= CardSum.Instance.DeckCardList.Count;

        // 检查卡牌是否条件失败
        CardObject cardObject = this.gameObject.GetComponent<CardObject>();
        if (cardObject != null && cardObject.card != null && CardEffect.Instance != null)
        {
            Card card = cardObject.card;
            if (CardEffect.Instance.IsConditionFailed(card.id))
            {
                Debug.Log($"[InDeckState] 卡牌 {card.name} (ID:{card.id}) 条件失败，从牌堆拿回手牌");

                // 1. 从牌堆逻辑数据中移除
                if (CardManager.Instance != null && CardManager.Instance.cardSet.Contains(card))
                {
                    CardManager.Instance.cardSet.Remove(card);
                    Debug.Log($"[InDeckState] 已从CardManager牌堆中移除卡牌 {card.name}");
                }

                // 2. 添加到手牌逻辑数据
                if (CardManager.Instance != null && !CardManager.Instance.cardInHand.Contains(card))
                {
                    CardManager.Instance.cardInHand.Add(card);
                    Debug.Log($"[InDeckState] 已将卡牌 {card.name} 添加到CardManager手牌");
                }

                // 3. 通知UI刷新
                if (CardManager.Instance != null)
                {
                    CardManager.Instance.NotifyDeckOrHandChanged();
                    Debug.Log($"[InDeckState] 已通知UI刷新");
                }

                // 4. 清除条件失败标记
                CardEffect.Instance.ClearConditionFailed(card.id);

                // 5. 立即返回手牌状态
                cardStateMachine.ChangeState(CardState.InHand);
                return;
            }
        }

        Debug.Log($"[InDeckState] 卡牌ID 21003条件检查完成，开始执行正常流程");

        // 正常情况：卡牌成功进入牌堆，更新CardManager数据并刷新UI
        CardObject normalCardObject = this.gameObject.GetComponent<CardObject>();
        Debug.Log($"[InDeckState] normalCardObject: {normalCardObject != null}, card: {normalCardObject?.card?.name ?? "null"}, CardManager.Instance: {CardManager.Instance != null}");

        if (normalCardObject != null && normalCardObject.card != null && CardManager.Instance != null)
        {
            Card card = normalCardObject.card;
            Debug.Log($"[InDeckState] 处理卡牌: {card.name} (ID:{card.id})");

            // 1. 从手牌逻辑数据中移除（如果存在）
            bool wasInHand = CardManager.Instance.cardInHand.Contains(card);
            if (wasInHand)
            {
                CardManager.Instance.cardInHand.Remove(card);
                Debug.Log($"[InDeckState] 正常流程：从CardManager手牌中移除卡牌 {card.name} (ID:{card.id})，手牌数量: {CardManager.Instance.cardInHand.Count}");
            }
            else
            {
                Debug.Log($"[InDeckState] 警告：卡牌 {card.name} 不在CardManager手牌中");
            }

            // 2. 添加到牌堆逻辑数据（如果不存在）
            bool wasInDeck = CardManager.Instance.cardSet.Contains(card);
            if (!wasInDeck)
            {
                CardManager.Instance.cardSet.Add(card);
                Debug.Log($"[InDeckState] 正常流程：将卡牌 {card.name} (ID:{card.id}) 添加到CardManager牌堆，牌堆数量: {CardManager.Instance.cardSet.Count}");
            }
            else
            {
                Debug.Log($"[InDeckState] 卡牌 {card.name} 已在CardManager牌堆中");
            }

            // 3. 检查CardSum列表
            Debug.Log($"[InDeckState] CardSum.DeckCardList数量: {CardSum.Instance?.DeckCardList?.Count ?? -1}，包含当前对象: {CardSum.Instance?.DeckCardList?.Contains(this.gameObject) ?? false}");
            Debug.Log($"[InDeckState] CardSum.HandCardList数量: {CardSum.Instance?.HandCardList?.Count ?? -1}，包含当前对象: {CardSum.Instance?.HandCardList?.Contains(this.gameObject) ?? false}");

            // 4. 通知UI刷新
            CardManager.Instance.NotifyDeckOrHandChanged();
            Debug.Log($"[InDeckState] 正常流程：已通知UI刷新");
        }
        else
        {
            Debug.LogError($"[InDeckState] 错误：无法处理卡牌，normalCardObject: {normalCardObject != null}, card: {normalCardObject?.card != null}, CardManager.Instance: {CardManager.Instance != null}");
        }
    }

    public void ExitState()
    {

        CanvasGroup.alpha = 1;
        CardSum.Instance.DeckCardList.Remove(this.gameObject);
    }

    public void UpdateState()
    {
        

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
