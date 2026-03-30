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
    private Canvas parentCanvas;
    
    // 用于保存四个角的数组
    private Vector3[] worldCorners = new Vector3[4];

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        // 监听鼠标左键点击
        if (Input.GetMouseButtonDown(0))
        {
            // 实时获取当前物体在世界空间中的四个角坐标：
            // [0] 左下, [1] 左上, [2] 右上, [3] 右下
            rectTransform.GetWorldCorners(worldCorners);

            // 获取相应的相机 (如果是 Overlay 模式则为 null)
            Camera currentCamera = null;
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                currentCamera = parentCanvas.worldCamera;
            }

            // RectTransformUtility.RectangleContainsScreenPoint 的底层逻辑就是：
            // 检测传递进来的屏幕点(Input.mousePosition)是否在这四个角(worldCorners)构成的矩形区域内。
            // 这个方法完美兼容了被旋转、缩放或者不同Canvas下的情况
            if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, currentCamera))
            {
                Debug.Log($"[PointTester] 成功点击到【{gameObject.name}】的四个角框定区域内！");
                onClickAction?.Invoke(); // 触发UnityEvent

                if (hideOnClick)
                {
                    gameObject.SetActive(false); // 隐藏自己
                }
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
