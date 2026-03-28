using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardMouseHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    cardStateMachine cardStateMachine;
    RectTransform rectTransform;
    private Vector3[] worldCorners = new Vector3[4];
    bool following=false;
    bool inputing=true;
    private void Awake()
    {
        cardStateMachine =transform.GetComponent<cardStateMachine>();
        rectTransform = GetComponent<RectTransform>();

        EventManage.AddEvent(EventManageEnum.drawPileOpen, (a) =>
        {
            inputing = false;
        });

        EventManage.AddEvent(EventManageEnum.drawPileClose, (a) =>
        {
            inputing = true;
        });
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        

    }




    public void OnPointerUp(PointerEventData eventData)
    {
        //cardStateMachine.ChangeState(cardStateMachine.preStayState);
        //cardStateMachine.ChangeState(CardState.InHand);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!inputing)
        {
            return;
        }

        if(cardStateMachine.CurrentState==CardState.InHand|| cardStateMachine.CurrentState == CardState.Dragging)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (IsMouseInsideUsingCorners() && !CardSum.Instance.dragingCard)
                {
                    cardStateMachine.ChangeState(CardState.Dragging);
                    following = true;
                    CardSum.Instance.dragingCard = true;
                }

            }
            if (Input.GetMouseButtonUp(0) && following)
            {
                if(cardJudge.Instance.IsMouseInsideUsingCorners())
                {
                    cardStateMachine.ChangeState(CardState.InUsing);
                    CardSum.Instance.dragingCard = false;
                    return;
                }
                

                cardStateMachine.ChangeState(cardStateMachine.preStayState);
                following = false;
                CardSum.Instance.dragingCard = false;
            }
        }

        
    }
    
    public bool IsMouseInsideUsingCorners()
    {
        // 获取四个角的世界坐标（顺序：左下、左上、右上、右下）
        rectTransform.GetWorldCorners(worldCorners);

        Vector2 mousePos = Input.mousePosition;

        // 方法1：使用矩形边界判定
        float minX = worldCorners[0].x;
        float maxX = worldCorners[2].x;
        float minY = worldCorners[0].y;
        float maxY = worldCorners[1].y;

        return mousePos.x >= minX && mousePos.x <= maxX &&
               mousePos.y >= minY && mousePos.y <= maxY;
    }
}
