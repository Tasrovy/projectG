using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIBase : MonoBehaviour
{
    protected virtual void Awake()
    {
        
    }

    public virtual void showMe()
    {
        if (gameObject != null)
        {
            gameObject.SetActive(true);
        }
    }

    public virtual void hideMe()
    {
        if (gameObject != null)
        {
            gameObject.SetActive(false);
        }
    }

}
