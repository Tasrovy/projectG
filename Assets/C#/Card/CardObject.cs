using UnityEngine;
using UnityEngine.EventSystems;

public class CardObject : MonoBehaviour, IPointerClickHandler
{
    public Card card;

    private Vector3 _originalScale;
    private CardChoosing _choosingManager;
    private CardShopping _shoppingManager;
    private CardUIObject _uiObject;

    private void Awake()
    {
        _originalScale = transform.localScale;
        
        // 缓存组件
        _choosingManager = GetComponentInParent<CardChoosing>(); 
        _shoppingManager = GetComponentInParent<CardShopping>();
        _uiObject = GetComponent<CardUIObject>();
    }

    /// <summary>
    /// 统一设置卡牌数据的方法
    /// </summary>
    public void SetCard(Card newCard)
    {
        card = newCard;
        
        // 刷新 UI 显示
        if (_uiObject != null)
        {
            // 这里的 SetCard 会处理卡图切换、文字更新
            _uiObject.SetCard(card);
            // 刷新颜色和位置状态
            _uiObject.UpdateVisual();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 1. 如果当前正处于 CardSelector 的“激活模式”（即战斗中选牌触发效果）
        // 则逻辑交给 CardUIObject 处理，这里可以直接跳过或配合执行
        if (_uiObject != null && _uiObject.IsValidToSelect && CardSelector.Instance != null)
        {
            // 注意：CardUIObject 已经实现了 OnPointerClick，
            // 如果两个脚本挂在同个物体，Unity 会同时调用两个 OnPointerClick。
            // 这里我们主要处理非战斗模式（商店/选牌）的逻辑。
        }

        // 2. 如果在 CardChoosing 界面中（例如开局三选一）
        if (_choosingManager != null)
        {
            _choosingManager.SelectCard(this);
            return; // 拦截，不进入后续逻辑
        }

        // 3. 如果在 CardShopping 界面中（商店买牌）
        if (_shoppingManager != null)
        {
            _shoppingManager.SelectCard(this);
            return; // 拦截
        }
        
        // 4. 其他通用点击逻辑可以在这里扩展
    }

    // --- 快捷获取卡牌数据（保持不变，增加 null 检查） ---
    
    public void Effect() => card?.OnTrigger();

    public int GetID() => card?.id ?? 0;
    public string GetName() => card?.name ?? "未知";
    public string GetDescription() => card?.description ?? "";
    public int GetNature1() => card?.nature1 ?? 0;
    public int GetNature2() => card?.nature2 ?? 0;
    public int GetNature3() => card?.nature3 ?? 0;
    public int GetSale() => card?.sale ?? 0;

    // 方便外部重置位置
    public void ResetUIPosition()
    {
        if (_uiObject != null) _uiObject.ResetState();
    }
}