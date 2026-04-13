using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 手牌容器管理器 - 挂在卡牌父物体上（只有HorizontalLayoutGroup + ContentSizeFitter的空物体）
/// 功能：根据卡牌数量动态调整间距 + 支持滑动浏览
/// </summary>
public class HandCardContainerManager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("间距配置")]
    [Tooltip("牌很多时的紧凑间距（负数会让牌重叠）")]
    public float minSpacing = -30f;

    [Tooltip("牌很少时的宽松间距")]
    public float maxSpacing = 20f;

    [Tooltip("超过这个数量开始压缩间距")]
    public int compressThreshold = 5;

    [Tooltip("达到这个数量时使用最小间距")]
    public int fullCompressThreshold = 10;

    [Header("滑动配置")]
    [Tooltip("启用滑动浏览功能")]
    public bool enableDragging = true;

    [Tooltip("滑动灵敏度")]
    public float dragSpeed = 1f;

    [Tooltip("是否限制滑动范围")]
    public bool clampDrag = true;

    private HorizontalLayoutGroup layout;
    private RectTransform selfRect;
    private Canvas canvas;
    
    private Vector2 lastMousePos;
    private bool isDragging;
    private float minX, maxX;

    private void Awake()
    {
        layout = GetComponent<HorizontalLayoutGroup>();
        selfRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        Invoke(nameof(RefreshLayout), 0.1f);
    }

    /// <summary>
    /// 刷新布局 - 在卡牌数量变化后调用
    /// </summary>
    public void RefreshLayout()
    {
        if (layout == null || selfRect == null) return;

        int cardCount = transform.childCount;
        float spacing = CalculateSpacing(cardCount);
        
        layout.spacing = spacing;

        // 计算滑动范围
        CalculateDragBounds();

        Debug.Log($"[HandContainer] 卡牌:{cardCount}张, 间距:{spacing:F1}");
    }

    /// <summary>
    /// 根据卡牌数量计算间距（线性插值）
    /// </summary>
    private float CalculateSpacing(int count)
    {
        if (count <= compressThreshold) return maxSpacing;
        if (count >= fullCompressThreshold) return minSpacing;

        float t = (float)(count - compressThreshold) / (fullCompressThreshold - compressThreshold);
        return Mathf.Lerp(maxSpacing, minSpacing, t);
    }

    /// <summary>
    /// 计算滑动范围限制
    /// </summary>
    private void CalculateDragBounds()
    {
        if (!clampDrag || canvas == null)
        {
            minX = float.MinValue;
            maxX = float.MaxValue;
            return;
        }

        RectTransform viewport = canvas.GetComponent<RectTransform>();
        if (viewport == null) return;

        float containerWidth = selfRect.rect.width;
        float viewportWidth = viewport.rect.width;

        // 如果容器比视口小，不需要滑动
        if (containerWidth <= viewportWidth)
        {
            minX = 0;
            maxX = 0;
            return;
        }

        // 计算左右边界
        float halfDiff = (containerWidth - viewportWidth) * 0.5f;
        minX = -halfDiff;
        maxX = halfDiff;
    }

    // ================= 拖拽滑动 =================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!enableDragging) return;
        
        isDragging = true;
        lastMousePos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!enableDragging || !isDragging) return;

        Vector2 currentMousePos = eventData.position;
        Vector2 delta = currentMousePos - lastMousePos;
        
        // 水平移动
        Vector2 move = new Vector2(delta.x * dragSpeed, 0);
        selfRect.anchoredPosition += move;

        // 限制范围
        if (clampDrag)
        {
            float clampedX = Mathf.Clamp(selfRect.anchoredPosition.x, minX, maxX);
            Vector2 pos = selfRect.anchoredPosition;
            pos.x = clampedX;
            selfRect.anchoredPosition = pos;
        }

        lastMousePos = currentMousePos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    /// <summary>
    /// 外部调用：通知卡牌数量变化
    /// </summary>
    public void Update()
    {
        RefreshLayout();
    }
}
