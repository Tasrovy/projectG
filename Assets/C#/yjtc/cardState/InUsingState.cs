using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InUsingState : stateBase,ICardState
{
    public bool CanTransitionTo(CardState newState)
    {
        return true;
    }

    public void EnterState()
    {
        EventManage.SendEvent(EventManageEnum.cardUsed, "占坑，传入这张卡的数据");
        print($"卡牌{this.name}被使用了，进入使用状态");
        cardStateMachine.ChangeState(CardState.InDeck);
    }

    public void ExitState()
    {
        
    }

    public void UpdateState()
    {
        throw new System.NotImplementedException();
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
