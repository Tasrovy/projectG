using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InUsingState : stateBase,ICardState
{
    public bool CanTransitionTo(CardState newState)
    {
        return true;
    }

    private int GetCardTypeFromId(int id)
    {
        string s = Math.Abs(id).ToString();
        return s.Length >= 1 ? int.Parse(s[0].ToString()) : 0;
    }

    public void EnterState()
    {
        EventManage.SendEvent(EventManageEnum.cardUsed, "ռ�ӣ��������ſ�������");

        // 获取卡牌对象和卡牌数据
        CardObject cardObject = this.gameObject.GetComponent<CardObject>();
        Card card = cardObject?.card;

        // 检查是否为类型为1的卡牌（ID第一位为1）
        if (card != null && GetCardTypeFromId(card.id) == 1)
        {
            Debug.Log($"[InUsingState] 类型为1的卡牌 {card.name} (ID:{card.id}) 打出，直接删除不进入牌堆");

            // 1. 执行卡牌效果
            cardObject.Effect();

            // 2. 从CardManager手牌中移除卡牌数据
            if (CardManager.Instance != null)
            {
                if (CardManager.Instance.cardInHand.Contains(card))
                {
                    CardManager.Instance.cardInHand.Remove(card);
                    Debug.Log($"[InUsingState] 从CardManager手牌中移除类型为1的卡牌: {card.name}");
                }
            }

            // 3. 从CardSum列表中移除GameObject
            if (CardSum.Instance != null)
            {
                // 从手牌列表中移除
                if (CardSum.Instance.HandCardList.Contains(this.gameObject))
                {
                    CardSum.Instance.HandCardList.Remove(this.gameObject);
                    Debug.Log($"[InUsingState] 从CardSum手牌列表中移除类型为1的卡牌GameObject");

                    // 更新其他卡牌的handID（调用InHandState的outHandList逻辑）
                    // 获取InHandState组件来获取handID
                    InHandState inHandState = this.gameObject.GetComponent<InHandState>();
                    if (inHandState != null)
                    {
                        int currentIndex = inHandState.handID - 1;
                        for (int i = currentIndex; i < CardSum.Instance.HandCardList.Count; i++)
                        {
                            InHandState state = CardSum.Instance.HandCardList[i].GetComponent<InHandState>();
                            if (state != null)
                            {
                                state.handID -= 1;
                            }
                        }
                    }
                }

                // 从牌堆列表中移除（如果存在）
                if (CardSum.Instance.DeckCardList.Contains(this.gameObject))
                {
                    CardSum.Instance.DeckCardList.Remove(this.gameObject);
                    Debug.Log($"[InUsingState] 从CardSum牌堆列表中移除类型为1的卡牌GameObject");
                }
            }

            // 4. 从DUEL活动物体列表中移除
            if (DUEL.Instance != null && DUEL.Instance.activeObjects.Contains(this.gameObject))
            {
                DUEL.Instance.activeObjects.Remove(this.gameObject);
                Debug.Log($"[InUsingState] 从DUEL活动物体列表中移除类型为1的卡牌GameObject");
            }

            // 5. 通知UI刷新
            if (CardManager.Instance != null)
            {
                CardManager.Instance.NotifyDeckOrHandChanged();
            }

            // 6. 销毁GameObject
            Debug.Log($"[InUsingState] 销毁类型为1的卡牌GameObject: {this.gameObject.name}");
            Destroy(this.gameObject);

            return;
        }

        // 正常流程：执行效果并进入牌堆
        this.gameObject.GetComponent<CardObject>().Effect();
        if (true)
        {

        }

        print($"����{this.name}��ʹ���ˣ�����ʹ��״̬");
        cardStateMachine.ChangeState(CardState.InDeck);
    }

    public void ExitState()
    {
        
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
