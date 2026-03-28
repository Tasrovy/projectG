using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DraggingState : stateBase, ICardState
{
    
    public bool CanTransitionTo(CardState newState)
    {
        return true;
    }
    public void EnterState()
    {
        RectTransform.SetAsLastSibling();
        EventManage.SendEvent(EventManageEnum.ACardDragging, this);

    }

    public void ExitState()
    {
        EventManage.SendEvent(EventManageEnum.ACardOutDragging, this);
    }

    public void UpdateState()
    {
       //¿¨ÅÆ¸úËæÊó±ê
       transform.position=Input.mousePosition;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
