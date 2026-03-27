using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InDeckState : stateBase, ICardState
{
    public bool CanTransitionTo(CardState newState)
    {
        return true;
    }

    public void EnterState()
    {
        CanvasGroup.alpha = 0;
        transform.position =new Vector3(-999, -999, 0);
    }

    public void ExitState()
    {

        CanvasGroup.alpha = 1;
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
