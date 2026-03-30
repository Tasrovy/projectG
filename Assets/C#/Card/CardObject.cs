using UnityEngine;
using UnityEngine.EventSystems;

public class CardObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("视觉设置")]
    [SerializeField] private float hoverScale = 1.05f; // 悬浮放大倍数

    // 自动属性：外部可以直接读取 card，但只能通过内部的 SetCard 修改
    public Card card;

    private Vector3 _originalScale;
    private CardSelectObject _selectObject;
    private CardChoosing _choosingManager;

    private void Awake()
    {
        _originalScale = transform.localScale;
        
        // 缓存组件，避免每次点击或设置时产生性能消耗
        _selectObject = GetComponent<CardSelectObject>();
        _choosingManager = GetComponentInParent<CardChoosing>(); 
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
    }

    // --- 鼠标交互事件 ---

    public void OnPointerEnter(PointerEventData eventData) => transform.localScale = _originalScale * hoverScale;

    public void OnPointerExit(PointerEventData eventData) => transform.localScale = _originalScale;

    public void OnPointerClick(PointerEventData eventData)
    {
        // 直接使用缓存的管理器，无需每次 GetComponentInParent
        if (_choosingManager != null)
        {
            _choosingManager.SelectCard(this);
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