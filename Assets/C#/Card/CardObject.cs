using UnityEngine;
using UnityEngine.EventSystems;

public class CardObject : MonoBehaviour, IPointerClickHandler
{
    public Card card;

    private Vector3 _originalScale;
    private CardSelectObject _selectObject;
    private CardChoosing _choosingManager;
    private CardShopping _shoppingManager;
    private CardUIObject _uiObject;
    private void Awake()
    {
        _originalScale = transform.localScale;
        
        // 缓存组件，避免每次点击或设置时产生性能消耗
        _selectObject = GetComponent<CardSelectObject>();
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
        
        // 如果身上挂了 CardSelectObject，通知它刷新一下颜色和状态
        if (_selectObject != null)
        {
            _selectObject.RefreshCardData();
        }

        if (_uiObject != null)
        {
            _uiObject.SetCard(card);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 如果卡牌数据为空，或者身上有未激活的状态，可以根据需要拦截（目前直接响应）

        // 如果在 CardChoosing 界面中
        if (_choosingManager != null)
        {
            _choosingManager.SelectCard(this);
        }

        // 如果在 CardShopping 界面中
        if (_shoppingManager != null)
        {
            _shoppingManager.SelectCard(this);
        }
    }

    // --- 快捷获取卡牌数据 ---
    
    public void Effect() => card?.OnTrigger();

    // 增加了 null 安全判定 (? 和 ??)，防止 card 未赋值时报错
    public int GetID() => card?.id ?? 0;
    public string GetName() => card?.name ?? "未知";
    public string GetDescription() => card?.description ?? "";
    public int GetNature1() => card?.nature1 ?? 0;
    public int GetNature2() => card?.nature2 ?? 0;
    public int GetNature3() => card?.nature3 ?? 0;
    public int GetSale() => card?.sale ?? 0;
}