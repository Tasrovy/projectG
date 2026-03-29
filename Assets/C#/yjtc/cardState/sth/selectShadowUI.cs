using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class selectShadowUI : MonoBehaviour
{
    private void Awake()
    {
        EventManage.AddEvent(EventManageEnum.selectCardBegin, showMe);
        EventManage.AddEvent(EventManageEnum.selectCardEnd, hideMe);
        this.gameObject.SetActive(false);
    }

    void showMe(object none)
    {
        this.gameObject.SetActive(true);
        CardSum.Instance.selectCarding = true;
    }

    void hideMe(object none)
    {
        gameObject.SetActive(false);
            CardSum.Instance.selectCarding = false;
    }


}
