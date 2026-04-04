using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardUIObject : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("UI 组件引用")] 
    public Image baseImage;
    public Text nameText;
    public Text descriptionText;

    [Header("重要：动画容器")] 
    public RectTransform visualContainer;

    [Header("卡图资源")] 
    public Sprite commonSprite;
    public Sprite ShengZhi;
    public Sprite JianZhi;
    public Sprite ShengZhang;

    [Header("视觉反馈颜色")] 
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color disabledColor = Color.gray;

    [Header("动画设置 (点击与悬浮)")] 
    public float selectOffset = 50f; 
    public float hoverOffset = 20f; 
    public float hoverScale = 1.1f; 
    public float moveSpeed = 15f;
    public float sortLerpSpeed = 15f; // 别人让位时的平滑速度

    // 内部状态
    private CardObject _cardObject;
    private Vector2 _containerTargetPos;
    private Vector2 _containerOriginalPos;
    private Vector3 _targetScale = Vector3.one;
    
    // 【新增】：记录上一帧根节点的位置，用于计算 LayoutGroup 造成的瞬移
    private Vector3 _lastRootPosition; 

    [SerializeField]private bool _isSelected = false;
    [SerializeField]private bool _isActiveMode = false;
    [SerializeField]private bool _isHovering = false;
    [SerializeField]private bool _isDragging = false;
    public bool _isSelectMode => CardActionResolver.Instance.currentMode==CardPlayMode.EffectSelect;

    private Transform _dragCanvasParent; 

    public bool IsSelected => _isSelected;
    public Card Card => _cardObject != null ? _cardObject.card : null;
    public bool IsValidToSelect => Card != null&&Card.id.ToString().StartsWith("1");

    private void Awake()
    {
        _cardObject = GetComponent<CardObject>();

        if (visualContainer == null && transform.childCount > 0)
            visualContainer = transform.GetChild(0).GetComponent<RectTransform>();

        if (visualContainer != null)
        {
            _containerOriginalPos = visualContainer.anchoredPosition;
            _containerTargetPos = _containerOriginalPos;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        _dragCanvasParent = canvas != null ? canvas.transform : transform.parent;
    }

    private void Start()
    {
        if (CardSelector.Instance != null)
            CardSelector.Instance.AddAllSelectObjects(this);

        if (Card != null) SetCard(Card);
        
        // 初始化根节点位置
        _lastRootPosition = transform.position; 
        
        UpdateVisual();
    }

    private void Update()
    {
        if (visualContainer == null) return;

        // 【核心魔法】：反向位移补偿，用于实现平滑让位
        // 如果我的根节点(被LayoutGroup控制)突然发生了移动，而我没有在被拖拽...
        if (transform.position != _lastRootPosition)
        {
            if (!_isDragging) 
            {
                // 计算根节点瞬间移动的向量
                Vector3 delta = transform.position - _lastRootPosition;
                // 把视觉容器向反方向推，保持它在屏幕上的绝对位置不变
                visualContainer.position -= delta;
            }
            _lastRootPosition = transform.position; // 更新记录
        }

        // 1. 位置插值 (如果是拖拽中，位置由鼠标接管，不进行自动插值)
        if (!_isDragging && Vector2.Distance(visualContainer.anchoredPosition, _containerTargetPos) > 0.1f)
        {
            // 使用 Lerp 平滑滑动到目标位置（此时目标位置通常是局部的 0,0）
            visualContainer.anchoredPosition = Vector2.Lerp(visualContainer.anchoredPosition, _containerTargetPos, Time.deltaTime * sortLerpSpeed);
        }

        // 2. 缩放插值
        if (Vector3.Distance(visualContainer.localScale, _targetScale) > 0.01f)
        {
            visualContainer.localScale = Vector3.Lerp(visualContainer.localScale, _targetScale, Time.deltaTime * moveSpeed);
        }
    }

    // ================= 鼠标悬浮交互 =================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isActiveMode || (!IsValidToSelect&&_isSelectMode) || _isDragging) return;
        _isHovering = true;
        UpdateVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        UpdateVisual();
    }

    // ================= 鼠标点击交互 =================

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isActiveMode || (!IsValidToSelect&&_isSelectMode) || _isDragging) return;

        _isSelected = !_isSelected;

        if (CardSelector.Instance != null)
            CardSelector.Instance.SetSelectObject(_isSelected ? this : null);

        UpdateVisual();
    }

    // ================= 鼠标拖拽换位交互 =================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_isActiveMode || (!IsValidToSelect&&_isSelectMode)) return;
    
        _isDragging = true;
        _isHovering = false; 

        if (baseImage != null) baseImage.raycastTarget = false;

        if (visualContainer != null && _dragCanvasParent != null)
        {
            Vector3 worldPos = visualContainer.position;
            visualContainer.SetParent(_dragCanvasParent, false); 
            visualContainer.position = worldPos;
            visualContainer.localScale = _targetScale; 
            visualContainer.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || visualContainer == null) return;

        // 1. 图片跟随鼠标
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            visualContainer, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint);
        visualContainer.position = worldPoint;

        // 2. 实时排序逻辑
        Transform parentZone = transform.parent; // 这里的 parent 是根节点所在的 LayoutGroup
        if (parentZone != null)
        {
            // 获取鼠标在 LayoutGroup 下的局部坐标 X
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)parentZone, eventData.position, eventData.pressEventCamera, out Vector2 localPointerPos);

            int newIndex = 0;
            for (int i = 0; i < parentZone.childCount; i++)
            {
                Transform sibling = parentZone.GetChild(i);
                if (sibling == transform) continue; 

                // 如果鼠标的 X 坐标大于某张牌的 X 坐标，说明我们应该排在它右边
                if (localPointerPos.x > sibling.localPosition.x)
                {
                    newIndex++; // 增加索引值
                }
            }

            // 如果计算出的新位置和当前位置不同，执行换位
            int currentIndex = transform.GetSiblingIndex();
            if (currentIndex != newIndex)
            {
                transform.SetSiblingIndex(newIndex);
                // 注意：一旦这里改变了 SiblingIndex，LayoutGroup 会瞬间移动其他兄弟节点。
                // 但由于我们在 Update 里写了“反向位移补偿”，其他卡牌会非常丝滑地闪避让位！
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;
    
        if (baseImage != null) baseImage.raycastTarget = true;

        if (visualContainer != null)
        {
            // 放手时，视觉容器回到根节点（此时根节点已经在正确的顺序位置上了）
            visualContainer.SetParent(transform, false);
            
            // 为了实现“吸附”效果，计算它当前相对根节点的位置
            visualContainer.anchoredPosition = transform.InverseTransformPoint(visualContainer.position);
            visualContainer.localScale = _targetScale;
        }

        UpdateVisual();

        // ==========================================
        // 【重要】：在这里同步后端数据！
        // 视觉上排好序了，你需要告诉后端管理器重新排列 List<Card>
        // 例如：
        // CardManager.Instance.ReorderHandCard(this.Card, transform.GetSiblingIndex());
        // ==========================================
    }

    // ================= 状态管理 =================

    public void SetActiveMode(bool isActive)
    {
        _isActiveMode = isActive;
        if (!isActive)
        {
            _isSelected = false;
            _isHovering = false;
        }
        UpdateVisual();
    }

    public void ForceDeselect()
    {
        if (_isSelected)
        {
            _isSelected = false;
            UpdateVisual();
        }
    }

    public void UpdateVisual()
    {
        if (visualContainer == null) return;

        float currentYOffset = 0f;
        float currentScale = 1f;

        if (_isSelected) currentYOffset += selectOffset;

        if (_isHovering && !_isDragging)
        {
            currentYOffset += hoverOffset;
            currentScale = hoverScale;
        }

        if (_isDragging)
        {
            currentScale = hoverScale; // 拖动时保持放大
        }

        _containerTargetPos = _containerOriginalPos + new Vector2(0, currentYOffset);
        _targetScale = new Vector3(currentScale, currentScale, currentScale);
        
        if (baseImage == null) return;

        if (!_isActiveMode) { baseImage.color = normalColor; return; }
        if (!IsValidToSelect&&_isSelectMode) baseImage.color = disabledColor;
        else if (_isSelected) baseImage.color = selectedColor;
        else baseImage.color = normalColor;
    }

    public void SetCard(Card card)
    {
        if (card == null) return;
        nameText.text = card.name;
        descriptionText.text = card.GetParsedDescription();

        int brokenValue = 0, addedValue = 0;
        int.TryParse(card.broken, out brokenValue);
        int.TryParse(card.added, out addedValue);

        if (card.id.ToString()[0] == '1') baseImage.sprite = commonSprite;
        else if (brokenValue > 0) baseImage.sprite = JianZhi;
        else if (addedValue > 0) baseImage.sprite = ShengZhang;
        else baseImage.sprite = ShengZhi;
    }

    public void ResetState()
    {
        _isSelected = false;
        _isActiveMode = false;
        _isHovering = false;
        _isDragging = false;
        UpdateVisual();
        if (visualContainer != null) visualContainer.anchoredPosition = _containerOriginalPos;
    }
}