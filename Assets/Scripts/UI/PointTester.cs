using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class PointTester : MonoBehaviour, IPointerClickHandler
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, eventData.position, cachedCamera))
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

