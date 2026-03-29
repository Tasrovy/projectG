using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum CardState
{
    InDeck,         // 牌库中
    InShop,         // 商店中
    InHand,         // 手牌状态
    Dragging,       // 拖动中
    InUsing,         // 使用中
}

// 状态接口
public interface ICardState
{
    void EnterState();
    void UpdateState();
    void ExitState();
    bool CanTransitionTo(CardState newState);
}

public class cardStateMachine : MonoBehaviour
{
    private Dictionary<CardState, ICardState> states;
    private ICardState currentState;
    public CardState currentStateType;
    public Transform mainCan;
    CardBase cardInfor;
    public CardState previousState = CardState.InDeck;
    public CardState preStayState = CardState.InDeck;

    public CardState CurrentState => currentStateType;
    bool isInited = false;
    // Start is called before the first frame update
    void Start()
    {
        if (!isInited)
            InitializeFunc();
        isInited = true;
    }

    // Update is called once per frame
    void Update()
    {
        currentState?.UpdateState();
    }

    
    private void InitializeFunc()
    {
        if(mainCan == null)
            mainCan = GameObject.Find("cardFa").transform;
        transform.SetParent(mainCan);

        DraggingState draggingState = this.GetComponent<DraggingState>();
        InHandState inHandState = this.GetComponent<InHandState>();
        InShopState inShopState = this.GetComponent<InShopState>();
        InUsingState inUsingState = this.GetComponent<InUsingState>();
        InDeckState inDeckState = this.GetComponent<InDeckState>();

        cardInfor = GetComponent<CardBase>();
        states = new Dictionary<CardState, ICardState>
        {
            { CardState.InHand, inHandState },
            { CardState.Dragging, draggingState },
                { CardState.InShop, inShopState },
            { CardState.InUsing, inUsingState },
            { CardState.InDeck, inDeckState },


            /*
            { CardState.InDeck, new InDeckState(this) },
            { CardState.InShop, new InShopState(this) },
            
            { CardState.Dragging, new DraggingState(this) },
            { CardState.InUsing, new InUsingState(this) }
            */
        };
        
        isInited = true;
        currentState= states[CardState.InHand];
        currentState.EnterState();
    }
    public bool ChangeState(CardState newState)
    {

        if (currentState != null && !currentState.CanTransitionTo(newState))
        {
            Debug.LogWarning($"无法从 {currentStateType} 切换到 {newState}");
            return false;
        }
        if (states == null)
        {
            InitializeFunc();
            isInited = true;
            //Debug.LogWarning($"{this.name}状态机初始化异常！！");
        }

        if (states[newState] == null)
        {
            Debug.LogWarning($"{this.name}没有 {newState} 状态");
            return false;
        }
        previousState = currentStateType;
        
        if(currentStateType ==CardState.InHand||currentStateType==CardState.InDeck)
            preStayState = currentStateType;

        currentState?.ExitState();
        currentState = states[newState];
        currentStateType = newState;
        currentState.EnterState();

        return true;
    }
}
