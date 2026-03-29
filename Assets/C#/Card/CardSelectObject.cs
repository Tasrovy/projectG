using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 卡牌选择UI对象
/// 只有在“激活模式”下，ID第一位为1的卡牌才能被点击和变色。
/// 未激活时，所有卡牌都显示为正常颜色且不可点击。
/// </summary>
[RequireComponent(typeof(Image))]
public class CardSelectObject : MonoBehaviour, IPointerClickHandler
{
    [Header("视觉设置")]
    [SerializeField] private Color normalColor = Color.white;    // 正常状态 (未激活、或激活但未选中)
    [SerializeField] private Color selectedColor = Color.green;  // 选中状态
    [SerializeField] private Color disabledColor = Color.gray;   // 不可选中状态(ID非1开头)

    private Image _image;
    private CardObject _cardObject;
    
    private bool _isSelected = false;
    private bool _isActiveMode = false; // 新增：是否处于“可选择激活模式”

    // 对外暴露的属性
    public bool IsSelected => _isSelected;
    public bool IsActiveMode => _isActiveMode;
    public Card Card => _cardObject != null ? _cardObject.card : null;
    
    // 核心条件：卡牌不为空，且 ID 的第一位是 "1"
    public bool IsValidToSelect => Card != null && Card.id.ToString().StartsWith("1");

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.raycastTarget = true;
        _cardObject = GetComponentInParent<CardObject>();
    }

    private void Start()
    {
        if (CardSelector.Instance != null)
            CardSelector.Instance.AddAllSelectObjects(this);

        UpdateVisual();
    }

    private void OnDestroy()
    {
        if (CardSelector.Instance != null)
            CardSelector.Instance.RemoveAllSelectObjects(this);
    }

    /// <summary>
    /// 由外部管理器调用，开启或关闭“选择模式”
    /// </summary>
    public void SetActiveMode(bool isActive)
    {
        _isActiveMode = isActive;
        Debug.Log($"[CardSelectObject] ⚙️ {gameObject.name} (卡牌:{Card?.name}) 激活模式设为: {isActive}");
        
        // 如果关闭了激活模式，自动清空选中状态
        if (!isActive) 
        {
            _isSelected = false;
        }
        
        UpdateVisual();
    }

    /// <summary>
    /// 新增：供 CardSelector 调用的强制取消选中方法（单选逻辑用）
    /// </summary>
    public void ForceDeselect()
    {
        if (_isSelected)
        {
            Debug.Log($"[CardSelectObject] 🔄 {gameObject.name} (卡牌:{Card?.name}) 被管理器强制取消选中！");
            _isSelected = false;
            UpdateVisual();
        }
    }

    /// <summary>
    /// 鼠标点击事件
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 第一步测试：Unity EventSystem 是否检测到了点击
        Debug.Log($"[CardSelectObject] 🖱️ 鼠标点击了UI物体: {gameObject.name} | 内部卡牌数据名: {Card?.name ?? "无数据"}");

        // 1. 如果没有被激活
        if (!_isActiveMode)
        {
            Debug.LogWarning($"[CardSelectObject] ❌ 点击被拦截: 当前卡牌未处于激活选择模式 (_isActiveMode = false)");
            return;
        }

        // 2. 如果不满足条件（ID不以1开头）
        if (!IsValidToSelect)
        {
            Debug.LogWarning($"[CardSelectObject] ❌ 点击被拦截: 该卡牌不满足选择条件 (当前卡牌ID: {Card?.id})");
            return;
        }

        // 3. 状态取反
        _isSelected = !_isSelected;
        Debug.Log($"[CardSelectObject] ✅ 点击生效！状态切换为: {(_isSelected ? "选中 (Selected)" : "取消选中 (Deselected)")}");

        // 4. 通知管理器
        if (CardSelector.Instance != null)
        {
            CardSelector.Instance.SetSelectObject(_isSelected ? this : null);
        }
        else
        {
            Debug.LogError($"[CardSelectObject] ⚠️ 找不到 CardSelector.Instance！无法通知管理器！");
        }

        // 5. 更新表现
        UpdateVisual();
    }

    /// <summary>
    /// 更新卡牌颜色
    /// </summary>
    public void UpdateVisual()
    {
        if (_image == null) return;

        if (!_isActiveMode)
        {
            _image.color = normalColor;
            return; 
        }

        // --- 以下是激活模式下的颜色逻辑 ---
        if (!IsValidToSelect)
        {
            _image.color = disabledColor; // 激活了，但不满足ID为1开头，置灰
        }
        else if (_isSelected)
        {
            _image.color = selectedColor; // 激活了，满足条件且被选中，绿色
        }
        else
        {
            _image.color = normalColor;   // 激活了，满足条件但未选中，白色
        }
    }

    /// <summary>
    /// 外部重置状态使用
    /// </summary>
    public void ResetState()
    {
        _isSelected = false;
        _isActiveMode = false;
        UpdateVisual();
    }
    
    public void RefreshCardData()
    {
        UpdateVisual();
    }
}