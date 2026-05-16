using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EffectCommand
{
    public string methodName;
    public object[] parameters;
}

[System.Serializable]
public class CardData
{
    public int id;
    public int dialog;
    public int nature1;
    public int nature2;
    public int nature3;
    public string name;
    public int sale;
    public int sell;
    public string made;
    public string broken;
    public string added;
    public string description;
    public string buff;
    public string trigger;
    public string nextTurn;
    public string text;
    public string prompt;

}

[System.Serializable]
public class Card
{
    public int id;
    public int dialog;
    public int nature1;
    public int nature2;
    public int nature3;
    public string name;
    public int sale;
    public int sell;
    public string made;
    public string broken;
    public string added;
    public string buff;
    public string trigger;
    public string description;
    public string nextTurn;
    public string text;
    public string prompt;

    [NonSerialized] private CardEffectPlan _effectPlan;
    [NonSerialized] private CardEffectPlanParser _effectPlanParser;
    [NonSerialized] private CardNatureState _natureState;
    [NonSerialized] private CardRuntime _runtime;
    [NonSerialized] private CardDescriptionFormatter _descriptionFormatter;

    private void EnsureComponents()
    {
        if (_effectPlan == null) _effectPlan = new CardEffectPlan();
        if (_effectPlanParser == null) _effectPlanParser = new CardEffectPlanParser();
        if (_natureState == null) _natureState = new CardNatureState(this);
        if (_runtime == null) _runtime = new CardRuntime(this, _effectPlan);
        if (_descriptionFormatter == null) _descriptionFormatter = new CardDescriptionFormatter();
    }

    public void InitCard(CardData cardData)
    {
        EnsureComponents();
        id = cardData.id;
        dialog = cardData.dialog;
        nature1 = cardData.nature1;
        nature2 = cardData.nature2;
        nature3 = cardData.nature3;
        name = cardData.name;
        description = cardData.description;
        sale = cardData.sale;
        sell = cardData.sell;
        made = cardData.made;
        broken = cardData.broken;
        added = cardData.added;
        buff = cardData.buff;
        trigger = cardData.trigger;
        nextTurn = cardData.nextTurn;
        text = cardData.text;
        prompt = cardData.prompt;
        _effectPlanParser.ParseAll(this, _effectPlan);
    }

    public void InitCard(Card cardData)
    {
        EnsureComponents();
        id = cardData.id;
        dialog = cardData.dialog;
        nature1 = cardData.nature1;
        nature2 = cardData.nature2;
        nature3 = cardData.nature3;
        name = cardData.name;
        description = cardData.description;
        sale = cardData.sale;
        sell = cardData.sell;
        made = cardData.made;
        broken = cardData.broken;
        added = cardData.added;
        buff = cardData.buff;
        trigger = cardData.trigger;
        nextTurn = cardData.nextTurn;
        text = cardData.text;
        prompt = cardData.prompt;
        _effectPlanParser.ParseAll(this, _effectPlan);
    }
    
    private int GetCardType(int id)
    {
        string s = Math.Abs(id).ToString();
        return s.Length >= 1 ? int.Parse(s[0].ToString()) : 0;
    }
    
    public void Add(int id, int num)
    {
        EnsureComponents();
        _natureState.Add(id, num);
    }

    public void AddTo(int id, int num)
    {
        EnsureComponents();
        _natureState.AddTo(id, num);
    }

    public int GetNatureById(int id)
    {
        EnsureComponents();
        return _natureState.GetNatureById(id);
    }
    
    // ==========================================
    // 生命周期回调被动触发：只有当字段为字符串时，才会触发这里的效果
    // ==========================================
    public void OnMade()
    {
        EnsureComponents();
        Debug.Log("[Card]卡牌被生枝");
        _runtime.OnMade();
    }

    public void OnBroken()
    {
        EnsureComponents();
        Debug.Log("[Card]卡牌被剪枝");
        _runtime.OnBroken();
    }

    public void OnAdded()
    {
        EnsureComponents();
        Debug.Log("[Card]卡牌被生长");
        _runtime.OnAdded();
    }

    public void OnBuffUpdate()
    {
        EnsureComponents();
        _runtime.OnBuffUpdate();
    }

    // ==========================================
    // 主动打出触发：专门执行前置条件（纯数字配置）和Trigger常规配置
    // ==========================================
    public void OnTrigger()
    {
        EnsureComponents();
        _runtime.OnTrigger();
    }

    public void OnNextTurn()
    {
        EnsureComponents();
        _runtime.OnNextTurn();
    }

    public string GetParsedDescription()
    {
        EnsureComponents();
        return _descriptionFormatter.Format(this);
    }

    public bool TryModifyAddedValue(int delta)
    {
        EnsureComponents();
        if (string.IsNullOrEmpty(added)) return false;

        if (!added.Contains("(") && !added.Contains(")") && int.TryParse(added.Trim(), out int currentValue))
        {
            int newValue = currentValue + delta;
            added = newValue.ToString();

            _effectPlanParser.ReparseAdded(this, _effectPlan);

            Debug.Log($"[Card] 成功修改 added 字段数值: {currentValue} → {newValue}");
            return true;
        }
        
        Debug.LogWarning($"[Card] added 字段为字符串类型，无法作为纯数字修改。当前值: '{added}'");
        return false;
    }
}
