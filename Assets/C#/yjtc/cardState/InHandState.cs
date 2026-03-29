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
        RepairList2();
        List<GameObject> handList = CardSum.Instance.HandCardList;
        if (handList.Contains(this.gameObject))
        {
            Debug.LogWarning($"卡片 {this.name} 已存在于手牌中，跳过添加");
            return;
        }


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

    // 修复列表：移除所有 null 条目 + 去重（保留第一个）
    public void RepairList()
    {
        // 1. 移除所有 null
        int removedNull = CardSum.Instance.HandCardList.RemoveAll(item => item == null);
        List<GameObject> handList= CardSum.Instance.HandCardList;

        // 2. 去重（保留第一次出现的位置）
        var uniqueList = new List<GameObject>();
        foreach (var card in handList)
        {
            if (!uniqueList.Contains(card))
                uniqueList.Add(card);
        }

        int removedDuplicate = handList.Count - uniqueList.Count;
        handList = uniqueList;

        if (removedNull > 0 || removedDuplicate > 0)
        {
            Debug.Log($"修复列表：移除 null ({removedNull})，移除重复 ({removedDuplicate})，当前数量：{handList.Count}");
        }
    }
    public void RepairList2()
    {
        List<GameObject> handList = CardSum.Instance.HandCardList;

        // 1. 移除 null
        int removedNull = handList.RemoveAll(item => item == null);

        // 2. 原地去重（从后往前删除重复项）
        int removedDuplicate = 0;
        for (int i = 0; i < handList.Count; i++)
        {
            GameObject current = handList[i];
            // 从后往前查找并删除重复
            for (int j = handList.Count - 1; j > i; j--)
            {
                if (handList[j] == current)
                {
                    handList.RemoveAt(j);
                    removedDuplicate++;
                }
            }
        }

        if (removedNull > 0 || removedDuplicate > 0)
        {
            Debug.Log($"修复列表：移除 null ({removedNull})，移除重复 ({removedDuplicate})，当前数量：{handList.Count}");
        }
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
