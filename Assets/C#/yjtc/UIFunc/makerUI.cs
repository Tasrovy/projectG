using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class makerUI : UIBase
{
    public bool isShow = false;

    protected override void Awake()
    {
        base.Awake();
            hideMe();
        isShow = false;
    }

    public void showAndHide()
    {
        isShow = !isShow;
        if(isShow) 
            {
            showMe();
        }
        else
        {
            hideMe();
        }
    }
}
