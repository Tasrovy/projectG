using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// 卡牌选择管理器 - 负责UI交互、确认/取消的拦截、并执行回调
/// </summary>
public class CardSelector : Singleton<CardSelector>
{
    [Header("UI引用")]
    [SerializeField] private Button submitButton;      // 提交按钮
    [SerializeField] private Button cancelButton;      // 取消按钮

    private List<CardSelectObject> _allSelectObjects = new List<CardSelectObject>();
    
    // 当前状态
    private bool _isSelecting = false;
    private CardSelectObject _selectedCardObject = null; // 当前选中的物体

    // 存储确认和取消时要执行的逻辑
    private Action<Card> _onConfirmAction;
    private Action _onCancelAction;

    protected override void Awake()
    {
        base.Awake();
        
        // 绑定按钮事件
        submitButton.onClick.AddListener(OnSubmitClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
        
        // 初始隐藏UI
        EndSelectionUI();
    }

    public void AddAllSelectObjects(CardSelectObject selectObject) => _allSelectObjects.Add(selectObject);
    public void RemoveAllSelectObjects(CardSelectObject selectObject) => _allSelectObjects.Remove(selectObject);

    /// <summary>
    /// 当玩家点击了某张牌时，CardSelectObject 会调用此方法
    /// </summary>
    public void SetSelectObject(CardSelectObject selectObject)
    {
        if (!_isSelecting) return;

        // 如果传进来的是 null（玩家再次点击了已选中的牌，想取消选中）
        if (selectObject == null)
        {
            _selectedCardObject = null;
            submitButton.interactable = false; // 没选牌时不能点确认
            return;
        }

        // --- 单选逻辑：如果之前选了别的牌，强制取消之前那张的选中状态 ---
        if (_selectedCardObject != null && _selectedCardObject != selectObject)
        {
            _selectedCardObject.ForceDeselect();
        }

        _selectedCardObject = selectObject;
        submitButton.interactable = true; // 选了牌，可以点确认了
    }

    /// <summary>
    /// 开始进入选择模式（由外部Helper调用）
    /// </summary>
    public void StartSelection(Action<Card> onConfirm, Action onCancel)
    {
        _isSelecting = true;
        _onConfirmAction = onConfirm;
        _onCancelAction = onCancel;
        _selectedCardObject = null;

        // 1. 显示按钮
        submitButton.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(true);
        submitButton.interactable = false; // 必须选了一张牌才能按

        // 2. 激活场上所有卡牌进入“选择模式”
        foreach (var obj in _allSelectObjects)
        {
            if (obj != null) obj.SetActiveMode(true);
        }
        
        Debug.Log("[CardSelector] 开始选牌，等待玩家确认...");
    }

    /// <summary>
    /// 点击确认按钮
    /// </summary>
    private void OnSubmitClicked()
    {
        if (_selectedCardObject == null) return;
        
        Card selectedCard = _selectedCardObject.Card;
        Debug.Log($"[CardSelector] 玩家确认选择了卡牌: {selectedCard.name}");

        EndSelectionUI();

        // 将控制权完全交还给 Helper
        _onConfirmAction?.Invoke(selectedCard);
    }

    /// <summary>
    /// 点击取消按钮
    /// </summary>
    private void OnCancelClicked()
    {
        Debug.Log("[CardSelector] 玩家取消了选牌，卡牌保持原状。");
        
        EndSelectionUI();
        
        // 将控制权完全交还给 Helper
        _onCancelAction?.Invoke();
    }

    /// <summary>
    /// 关闭UI，清理状态
    /// </summary>
    private void EndSelectionUI()
    {
        _isSelecting = false;
        _selectedCardObject = null;

        submitButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);

        // 关闭场上所有卡牌的选择模式
        foreach (var obj in _allSelectObjects)
        {
            if (obj != null) obj.SetActiveMode(false);
        }
    }

    private void NotifyEffectManagerToContinue()
    {
        if (CardEffect.Instance != null)
        {
            CardEffect.Instance.OnSelectCardEnd(null);
        }
    }
}