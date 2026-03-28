using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class stateBase : MonoBehaviour
{
    protected RectTransform RectTransform;
    protected cardStateMachine cardStateMachine;
    protected Image image;
    protected CanvasGroup CanvasGroup;

    protected virtual void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        cardStateMachine = GetComponent<cardStateMachine>();
        image = GetComponent<Image>();
        CanvasGroup= GetComponent<CanvasGroup>();
        if(CanvasGroup==null)
            CanvasGroup =this.AddComponent<CanvasGroup>();
    }


}
