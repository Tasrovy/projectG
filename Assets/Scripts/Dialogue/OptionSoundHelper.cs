using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

// 继承 IPointerClickHandler 来监听鼠标/手指点击
public class OptionSoundHelper : MonoBehaviour, IPointerClickHandler
{
    [Header("选项点击时触发的事件")]
    public UnityEvent onClickEvent;

    public void OnPointerClick(PointerEventData eventData)
    {
        // 触发通过面板配置的所有UnityEvent
        onClickEvent?.Invoke();
    }

    public void SelectAudioPlay()
    {
        Debug.Log("播放选项点击音效");
        if (DialogueUIAudio.Instance != null)
        {
            DialogueUIAudio.Instance.PlayDialogueOptionClick();
            
        }
    }
}
