using UnityEngine;

public class ShengZhiAndJianZhiHelper : Singleton<ShengZhiAndJianZhiHelper>
{
    public int num;

    public void SetNum(int num)
    {
        this.num = num;
    }
    
    public void CallIt()
    {
        
    }

    public void ShengZhi(Card c)
    {
        for (int i = 0; i < num; i++)
        {
            Card t =new Card();
            t.InitCard(c);
            CardManager.Instance.AddCardInHand(t);
        }
    }

    public void JianZhi(Card[] c)
    {
        foreach (var t in c)
        {
            CardManager.Instance.AddToSetFromHand(t);
        }
    }
}