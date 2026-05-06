using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DeselectOnMouseExit : MonoBehaviour, IPointerExitHandler
{
    private void OnEnable()
    {
        // 选项每次出现时，延迟1帧清空初始的默认焦点（防止出现时默认高亮）
        StartCoroutine(ClearInitialFocus());
    }

    private IEnumerator ClearInitialFocus()
    {
        // 等待1帧，确保UI系统完成默认焦点的分配
        yield return null;
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == gameObject)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 如果当前选中的就是我自己，鼠标移出时，强行清空焦点
        if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == gameObject)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
