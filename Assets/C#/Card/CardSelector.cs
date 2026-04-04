using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// 卡牌UI选择器 - 仅负责卡牌的点击变现、按钮显示，以及广播玩家的操作
/// </summary>
public class CardSelector : Singleton<CardSelector>
{
    [Header("UI引用")]
    [SerializeField] private Button submitButton;
    [SerializeField] private Text submitButtonText;
    [SerializeField] private Button cancelButton;

    [Header("区域设置")]
    [SerializeField] private Transform handZone; 

    // --- 对外广播的纯净事件 ---
    public event Action<Card> OnSubmitEvent; // 玩家点击了提交按钮并传出卡牌
    public event Action OnCancelEvent;       // 玩家点击了取消按钮

    private List<CardUIObject> _allSelectObjects = new List<CardUIObject>();
    private CardUIObject _selectedCardObject = null;

    protected override void Awake()
    {
        base.Awake();
        
        if (submitButton != null) submitButton.onClick.AddListener(OnSubmitClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
        
        if (submitButton != null && submitButtonText == null)
            submitButtonText = submitButton.GetComponentInChildren<Text>();

        DeselectCurrent();
    }

    /// <summary>
    /// 供外部(解析器)动态修改按钮文字 (如："打出", "提交")
    /// </summary>
    public void SetSubmitButtonText(string text)
    {
        if (submitButtonText != null) submitButtonText.text = text;
    }

    /// <summary>
    /// 统一设置所有卡牌是否可以被点击 (供解析器控制阶段)
    /// </summary>
    public void SetAllCardsInteractable(bool interactable)
    {
        foreach (var obj in _allSelectObjects)
        {
            if (obj != null) obj.SetActiveMode(interactable);
        }
    }

    public void AddAllSelectObjects(CardUIObject uiObject)
    {
        if (!_allSelectObjects.Contains(uiObject))
        {
            _allSelectObjects.Add(uiObject);
            if (handZone != null && uiObject.transform.parent != handZone)
                uiObject.transform.SetParent(handZone, false); 
            
            // 默认加入手牌即激活UI点击能力
            uiObject.SetActiveMode(true); 
        }
    }

    public void RemoveAllSelectObjects(CardUIObject uiObject)
    {
        if (_allSelectObjects.Contains(uiObject))
            _allSelectObjects.Remove(uiObject);
    }

    /// <summary>
    /// 卡牌被点击时触发
    /// </summary>
    public void SetSelectObject(CardUIObject uiObject)
    {
        if (uiObject == null)
        {
            DeselectCurrent();
            return;
        }

        if (_selectedCardObject != null && _selectedCardObject != uiObject)
            _selectedCardObject.ForceDeselect();

        _selectedCardObject = uiObject;
        
        // 显示按钮
        if (submitButton != null) { submitButton.gameObject.SetActive(true); submitButton.interactable = true; }
        if (cancelButton != null) cancelButton.gameObject.SetActive(true);
    }

    private void OnSubmitClicked()
    {
        if (_selectedCardObject == null) return;
        Card selectedCard = _selectedCardObject.Card;

        DeselectCurrent(); 
        
        // 直接把卡牌扔出去，自己不处理任何业务逻辑
        OnSubmitEvent?.Invoke(selectedCard);
    }

    private void OnCancelClicked()
    {
        DeselectCurrent();
        OnCancelEvent?.Invoke();
    }

    public void DeselectCurrent()
    {
        if (_selectedCardObject != null)
        {
            _selectedCardObject.ForceDeselect();
            _selectedCardObject = null;
        }

        if (submitButton != null) submitButton.gameObject.SetActive(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(false);
    }
}