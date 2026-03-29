using UnityEngine;
using UnityEngine.Events;

public class ShengZhiAndJianZhiHelper : Singleton<ShengZhiAndJianZhiHelper>
{
    public int num;
    public UnityEvent BeginAction;

    public void SetNum(int num)
    {
        this.num = num;
    }

    public void CallIt()
    {
        EventManage.SendEvent(EventManageEnum.selectCardBegin, null);
    }

    public void ShengZhi()
    {
        CallIt();
        BeginAction.RemoveAllListeners();
        BeginAction.AddListener(() =>
        {
            Card s = CardSum.Instance.selectedObj.GetComponent<Card>();
            ShengZhi(s);
        });
    }

    public void JianZhi()
    {
        CallIt();
        BeginAction.RemoveAllListeners();
        BeginAction.AddListener(() =>
        {
            Card s = CardSum.Instance.selectedObj.GetComponent<Card>();
            JianZhi(s);
            num--;
            if(num>0) JianZhi();
        });
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