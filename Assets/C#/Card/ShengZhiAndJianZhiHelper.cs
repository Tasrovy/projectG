using UnityEngine;
using UnityEngine.Events;

public class ShengZhiAndJianZhiHelper : Singleton<ShengZhiAndJianZhiHelper>
{
    public int num;
    public UnityEvent BeginAction = new UnityEvent();
    private bool isWaitingForSelection = false;
    private UnityAction shengZhiListener = null;
    private UnityAction jianZhiListener = null;

    protected override void Awake()
    {
        base.Awake();
        // 监听selectCardEnd事件，当用户选择卡牌后触发BeginAction
        EventManage.AddEvent(EventManageEnum.selectCardEnd, OnSelectCardEnd);
    }

    protected override void OnDestroy()
    {
        EventManage.RemoveEvent(EventManageEnum.selectCardEnd, OnSelectCardEnd);
        base.OnDestroy();
    }

    private void OnSelectCardEnd(object obj)
    {
        Debug.Log($"[ShengZhiAndJianZhiHelper] OnSelectCardEnd: 收到selectCardEnd事件，isWaitingForSelection={isWaitingForSelection}，BeginAction={(BeginAction != null ? "非空" : "null")}");

        if (isWaitingForSelection && BeginAction != null)
        {
            Debug.Log("[ShengZhiAndJianZhiHelper] OnSelectCardEnd: 用户选择卡牌完成，触发BeginAction");
            BeginAction.Invoke();
            isWaitingForSelection = false;

            // 清除监听器引用
            shengZhiListener = null;
            jianZhiListener = null;
        }
        else
        {
            Debug.Log($"[ShengZhiAndJianZhiHelper] OnSelectCardEnd: 条件不满足，不触发BeginAction。isWaitingForSelection={isWaitingForSelection}，BeginAction={(BeginAction != null ? "非空" : "null")}");
        }
    }

    public void SetNum(int num)
    {
        this.num = num;
    }

    public void CallIt()
    {
        Debug.Log("[ShengZhiAndJianZhiHelper] CallIt: 发送selectCardBegin事件，进入卡牌选择状态");
        EventManage.SendEvent(EventManageEnum.selectCardBegin, null);
    }

    public void ShengZhi()
    {
        Debug.Log("[ShengZhiAndJianZhiHelper] ShengZhi: 开始增殖流程");
        isWaitingForSelection = true;

        // 移除旧的增殖监听器（如果存在）
        if (shengZhiListener != null)
        {
            BeginAction.RemoveListener(shengZhiListener);
            shengZhiListener = null;
        }

        // 创建并添加新的增殖监听器
        shengZhiListener = () =>
        {
            Card s = CardSum.Instance.selectedObj.GetComponent<Card>();
            ShengZhi(s);
        };
        BeginAction.AddListener(shengZhiListener);

        Debug.Log("[ShengZhiAndJianZhiHelper] ShengZhi: 增殖监听器已设置，isWaitingForSelection=true，准备发送selectCardBegin事件");
        CallIt();
        Debug.Log("[ShengZhiAndJianZhiHelper] ShengZhi: selectCardBegin事件已发送，等待用户选择卡牌");
    }

    public void JianZhi()
    {
        Debug.Log("[ShengZhiAndJianZhiHelper] JianZhi: 开始消耗流程");
        isWaitingForSelection = true;

        // 移除旧的消耗监听器（如果存在）
        if (jianZhiListener != null)
        {
            BeginAction.RemoveListener(jianZhiListener);
            jianZhiListener = null;
        }

        // 创建并添加新的消耗监听器
        jianZhiListener = () =>
        {
            Card s = CardSum.Instance.selectedObj.GetComponent<Card>();
            JianZhi(s);
            num--;
            if(num>0) JianZhi();
        };
        BeginAction.AddListener(jianZhiListener);

        Debug.Log("[ShengZhiAndJianZhiHelper] JianZhi: 消耗监听器已设置，isWaitingForSelection=true，准备发送selectCardBegin事件");
        CallIt();
        Debug.Log("[ShengZhiAndJianZhiHelper] JianZhi: selectCardBegin事件已发送，等待用户选择卡牌");
    }

    public void ShengZhi(Card c)
    {
        for (int i = 0; i < num; i++)
        {
            Card t = new Card();
            t.InitCard(c);
            t.OnAdded();
            CardManager.Instance.AddCardInHand(t);
        }
    }

    public void JianZhi(Card c)
    {
        c.OnBroken();
        CardManager.Instance.BreakCard(c);
    }
}