using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardObject : MonoBehaviour
{
    public Card card;

    public void Effect()
    {
        card.OnTrigger();
    }
    public int GetID()=>card.id;
    public int GetNature1()=>card.nature1;
    public int GetNature2()=>card.nature2;
    public int GetNature3()=>card.nature3;
    public string GetName()=>card.name;
    public int GetSale()=>card.sale;
    public string GetDescription()=>card.description;
}
