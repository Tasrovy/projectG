using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(RectTransform))]
public class PointTester : MonoBehaviour
{
    [Header("触发的事件 (可在此绑定Confirm/Skip)")]
    public UnityEvent onClickAction;

    [Header("点击成功后是否立即隐藏自身")]
    public bool hideOnClick = false;

    private RectTransform rectTransform;
    private Camera cachedCamera;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cachedCamera = parentCanvas.worldCamera;
        }
    }

    private void Update()
    {
        // 高性能过滤：只有按下鼠标左键的那一帧才会进入坐标判断
        if (!Input.GetMouseButtonDown(0)) return;

        // 这里原脚本有冗余的 GetWorldCorners(数组)，其实 RectangleContainsScreenPoint 内部就算过了，直接干掉避免多余算力
        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, cachedCamera))
        {
            Debug.Log($"[PointTester] 成功点击到【{gameObject.name}】的四个角框定区域内！");
            onClickAction?.Invoke();

            if (hideOnClick)
            {
                gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 提供一个公开方法，供外部或其他 UnityEvent 手动调用来隐藏自己
    /// </summary>
    public void HideSelf()
    {
        gameObject.SetActive(false);
    }
}

