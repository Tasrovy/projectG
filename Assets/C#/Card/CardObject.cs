using UnityEngine;
using UnityEngine.EventSystems;

public class CardObject : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Card card;

    private Vector3 _originalScale;
    private CardChoosing _choosingManager;
    private ShopController _shoppingManager;
    private CardUIObject _uiObject;
    private bool _isPinned;

    private void Awake()
    {
        _originalScale = transform.localScale;

        // 缓存组件
        _choosingManager = GetComponentInParent<CardChoosing>();
        _shoppingManager = GetComponentInParent<ShopController>();
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

    // ================= 商店 hover/pin 交互 =================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_shoppingManager == null) return;

        var detailUI = DUELUIObjectManager.Instance.GetCardDetailUI();
        if (detailUI != null)
            detailUI.Show(card);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_shoppingManager == null) return;
        if (_isPinned) return;

        var detailUI = DUELUIObjectManager.Instance.GetCardDetailUI();
        if (detailUI != null && (card == null || detailUI.CurrentCard == card))
            detailUI.Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 1. 如果在 CardChoosing 界面中（例如开局三选一）
        if (_choosingManager != null)
        {
            _choosingManager.SelectCard(this);
            return;
        }

        // 2. 如果在商店中
        if (_shoppingManager != null)
        {
            _shoppingManager.SelectCard(this);
            PinDetail();
            return;
        }

        // 3. 如果在 CardSelector 激活模式下，交给 CardUIObject 处理
        // CardUIObject 的 OnPointerClick 会自动触发
    }

    private void PinDetail()
    {
        _isPinned = true;

        var detailUI = DUELUIObjectManager.Instance.GetCardDetailUI();
        if (detailUI != null)
            detailUI.Show(card);
    }

    public void UnpinDetail()
    {
        _isPinned = false;

        var detailUI = DUELUIObjectManager.Instance.GetCardDetailUI();
        if (detailUI != null)
            detailUI.Hide();
    }

    // --- 快捷获取卡牌数据（保持不变，增加 null 检查） ---

    public void Effect()
    {
        Debug.Log($"[CardObject][Effect] frame={Time.frameCount}, time={Time.time:F3}, cardRef={(card != null ? card.GetHashCode() : 0)}, id={card?.id}, name={card?.name}");
        card?.OnTrigger();
    }

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
