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
        EventManage.SendEvent(EventManageEnum.cardUsed, "ռ�ӣ��������ſ�������");
        this.gameObject.GetComponent<CardObject>().Effect();
        if (true)
        {

        }
        
        print($"����{this.name}��ʹ���ˣ�����ʹ��״̬");
        cardStateMachine.ChangeState(CardState.InDeck);
    }

    public void ExitState()
    {
        
    }

    public void UpdateState()
    {
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
