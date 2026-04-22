using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BtnAwake : MonoBehaviour
{
    [SerializeField] private UnityEvent onEnableEvent;

    void OnEnable()
    {
        onEnableEvent?.Invoke();
    }
}
