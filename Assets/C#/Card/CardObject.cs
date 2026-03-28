using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Card card;

    private Vector3 originalScale;

    private void Awake()
    {
        // 记录初始大小
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 鼠标移入时稍微放大
        transform.localScale = originalScale * 1.05f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 鼠标移出时恢复原尺寸
        transform.localScale = originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 通知父级的选择管理器，自己被点击选中了
        CardChoosing choosingManager = GetComponentInParent<CardChoosing>();
        if (choosingManager != null)
        {
            choosingManager.SelectCard(this);
        }
    }

    public void Effect()
    {
        card.Effect();
    }
    public int GetID()=>card.id;
    public int GetNature1()=>card.nature1;
    public int GetNature2()=>card.nature2;
    public int GetNature3()=>card.nature3;
    public string GetName()=>card.name;
    public int GetSale()=>card.sale;
    public string GetDescription()=>card.description;
}
