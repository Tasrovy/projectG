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
                // 清除条件失败标记
                CardEffect.Instance.ClearConditionFailed(card.id);
                // 立即返回手牌状态
                cardStateMachine.ChangeState(CardState.InHand);
                return;
            }
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
