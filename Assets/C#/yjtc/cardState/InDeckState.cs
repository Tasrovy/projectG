using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InDeckState : stateBase, ICardState
{
    public int deckID;
    public bool CanTransitionTo(CardState newState)
    {
        return true;
    }

    public void EnterState()
    {
        CanvasGroup.alpha = 0;
        transform.position =new Vector3(-999, -999, 0);
        CardSum.Instance.DeckCardList.Add(this.gameObject);
        deckID= CardSum.Instance.DeckCardList.Count;
    }

    public void ExitState()
    {

        CanvasGroup.alpha = 1;
        CardSum.Instance.DeckCardList.Remove(this.gameObject);
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
