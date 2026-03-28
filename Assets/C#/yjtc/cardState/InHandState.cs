using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InHandState : stateBase, ICardState
{
    public int handID = 0;
    
    Image Image;
    CardMouseHandler CardMouseHandler;
    public bool CanTransitionTo(CardState newState)
    {
        return true;
    }

    public void EnterState()
    {
        CardSum.Instance.HandCardList.Add(this.gameObject);
        handID = CardSum.Instance.HandCardList.Count;
        
        //rectTransform.SetAsLastSibling();
        RectTransform.SetAsFirstSibling();
    }

    public void ExitState()
    {
        outHandList();
    }

    public void UpdateState()
    {
        Vector3 myPos;
        myPos= CardSum.Instance.firstCardPos.transform.position;
        
        myPos.x += (handID - 1) *Screen.width*CardSum.Instance.cardInterval;

        if(CardMouseHandler.IsMouseInsideUsingCorners())
        {
            myPos.y+= Screen.height * CardSum.Instance.cardInterval;
            //print("222");
        }
        
        transform.position = Vector3.Lerp(transform.position, myPos, Time.deltaTime * 10f);

    }


    void outHandList()
    {
        // 获取当前手牌列表
        List<GameObject> handList = CardSum.Instance.HandCardList;

        // 找到当前卡牌在列表中的索引（handID是从1开始的）
        int currentIndex = handID - 1;

        // 验证索引有效性
        if (currentIndex < 0 || currentIndex >= handList.Count)
        {
            Debug.LogError($"无效的handID: {handID}, 列表长度: {handList.Count}");
            return;
        }

        // 删除当前卡牌
        handList.RemoveAt(currentIndex);

        // 更新后面所有卡牌的handID
        for (int i = currentIndex; i < handList.Count; i++)
        {
            InHandState state = handList[i].GetComponent<InHandState>();
            if (state != null)
            {
                state.handID -= 1;
            }
        }

        
    }
    protected override void Awake()
    {
        base.Awake();
        CardMouseHandler=this.GetComponent<CardMouseHandler>();
        if (image != null)
        {
            image.alphaHitTestMinimumThreshold = 0.2f; // 只在不透明区域检测

        }
    }

}
