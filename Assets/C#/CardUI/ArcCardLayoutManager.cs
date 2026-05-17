using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 弧线卡牌布局管理器 - 将子物体以 CardZone 为参考排列成圆弧形
/// 圆心 = CardZone + arcCenterOffset，卡牌沿圆弧等距放置
/// 支持鼠标滚轮旋转圆弧，参考杀戮尖塔的手牌扇形排列
/// </summary>
public class ArcCardLayoutManager : MonoBehaviour, IScrollHandler
{
    [Header("圆弧参数")]
    public float arcRadius = 500f;               // 圆弧半径
    public float maxArcSpan = 80f;               // 最大展开角度（度）
    public float cardSpacing = 18f;              // 卡牌间角度间隔（度）
    public Vector2 arcCenterOffset = Vector2.zero; // 圆心偏移: 圆心 = CardZone 位置 + 此偏移
    public float scrollSpeed = 100f;             // 滚轮滚动速度

    [Header("调试")]
    public bool showArcGizmo = true;             // 在 Scene 视图中显示圆弧

    private float _scrollOffset = 0f;

    private void Start()
    {
        LayoutCards();
    }

    private void Update()
    {
        LayoutCards();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EditorApplication.delayCall += DelayedLayout;
    }

    private void DelayedLayout()
    {
        EditorApplication.delayCall -= DelayedLayout;
        if (this == null) return;
        LayoutCards();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showArcGizmo) return;

        int count = transform.childCount;
        float halfSpan = count > 1
            ? Mathf.Min(cardSpacing * (count - 1), maxArcSpan) / 2f
            : 40f; // 没有子物体时显示默认半弧

        Transform t = transform;
        Vector3 center = t.position + (Vector3)(Vector2)t.TransformVector(arcCenterOffset);
        int segments = 32;

        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Vector3 prev = center + new Vector3(
            Mathf.Sin(-halfSpan * Mathf.Deg2Rad) * arcRadius,
            Mathf.Cos(-halfSpan * Mathf.Deg2Rad) * arcRadius, 0);
        for (int i = 1; i <= segments; i++)
        {
            float tParam = (float)i / segments;
            float rad = Mathf.Lerp(-halfSpan, halfSpan, tParam) * Mathf.Deg2Rad;
            Vector3 curr = center + new Vector3(
                Mathf.Sin(rad) * arcRadius,
                Mathf.Cos(rad) * arcRadius, 0);
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }

        // 圆心
        Gizmos.color = new Color(1, 0, 0, 0.4f);
        Gizmos.DrawSphere(center, 5f);
        // 圆心到弧顶的连线
        Gizmos.color = new Color(1, 0, 0, 0.15f);
        Gizmos.DrawLine(center, center + new Vector3(0, arcRadius, 0));
    }
#endif

    public void OnScroll(PointerEventData eventData)
    {
        int count = transform.childCount;
        if (count <= 1) return;

        float totalSpan = Mathf.Min(cardSpacing * (count - 1), maxArcSpan);

        _scrollOffset += eventData.scrollDelta.y * scrollSpeed * Time.deltaTime;

        float halfSpan = totalSpan / 2f;
        _scrollOffset = Mathf.Clamp(_scrollOffset, -halfSpan, halfSpan);

        LayoutCards();
    }

    /// <summary>
    /// 外部调用：强制刷新布局
    /// </summary>
    public void Refresh()
    {
        LayoutCards();
    }

    private void LayoutCards()
    {
        int count = transform.childCount;
        if (count == 0) return;

        // 圆心 = CardZone 的局部坐标 + arcCenterOffset
        Vector2 center = arcCenterOffset;

        // 计算实际圆弧跨度
        float totalSpan = Mathf.Min(cardSpacing * (count - 1), maxArcSpan);
        float halfSpan = totalSpan / 2f;

        for (int i = 0; i < count; i++)
        {
            Transform child = transform.GetChild(i);
            RectTransform rt = child.GetComponent<RectTransform>();
            if (rt == null) continue;

            // 卡牌在圆弧上的角度，scrollOffset 旋转整个圆弧
            float t = count > 1 ? (float)i / (count - 1) : 0.5f;
            float angle = Mathf.Lerp(-halfSpan + _scrollOffset, halfSpan + _scrollOffset, t);
            float rad = angle * Mathf.Deg2Rad;

            // 圆弧上的位置: 圆心 + 半径向量 (弧朝偏移方向弯曲)
            rt.anchoredPosition = new Vector2(
                center.x + Mathf.Sin(rad) * arcRadius,
                center.y + Mathf.Cos(rad) * arcRadius
            );

            // 获取该卡片的 CardUIObject 组件，判断是否正在拖拽
            CardUIObject cardUI = child.GetComponent<CardUIObject>();
            bool isDragging = cardUI != null && cardUI.IsDragging;

            // 旋转: 卡牌顶部向外散开，底部汇聚（拖拽中的卡牌保持竖直）
            rt.localRotation = isDragging ? Quaternion.identity : Quaternion.Euler(0, 0, -angle);
        }
    }
}
