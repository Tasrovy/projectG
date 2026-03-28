using UnityEngine;

public class DataManager : Singleton<DataManager>
{
    public int cardProperty1;
    public int cardProperty2;
    public int cardProperty3;
    public int MoneyNum;

    public float currProperty1Effect;
    public float currProperty2Effect;
    public float currProperty3Effect;

    protected override void Awake()
    {
        base.Awake();
        cardProperty1 = 0;
        cardProperty2 = 0;
        cardProperty3 = 0;
        MoneyNum = 0;
        currProperty1Effect = 0;
        currProperty2Effect = 0;
        currProperty3Effect = 0;
    }
    
    public void Add(int id, int num)
    {
        switch (id)
        {
            case 1 :AddCardProperty1(num);
                break;
            case 2 :AddCardProperty2(num);
                break;
            case 3 :AddCardProperty3(num);
                break;
            case 4 :AddMoneyNum(num);
                break;
            default:
                Debug.LogError($"unknown id :{id}");
                break;
        }
    }

    private void AddCardProperty1(int num)
    {
        if(cardProperty1+num<0) return;
        cardProperty1 += num*(1+cardProperty1);
    }

    private void AddCardProperty2(int num)
    {
        if (cardProperty2 + num < 0) return;
        cardProperty2 += num*(1+cardProperty2);
    }

    private void AddCardProperty3(int num)
    {
        if (cardProperty3 + num < 0) return;
        cardProperty3 += num*(1+cardProperty3);
    }

    private void AddMoneyNum(int num)
    {
        if (MoneyNum + num < 0) return;
        MoneyNum += num;
    }

    public int GetCardProperty1()=>cardProperty1;
    public int GetCardProperty2()=>cardProperty2;
    public int GetCardProperty3()=>cardProperty3;
    public int GetMoneyNum()=>MoneyNum;

    public void SetCardProperty1Effect(float num)
    {
        currProperty1Effect = num;
    }

    public void SetCardProperty2Effect(float num)
    {
        currProperty2Effect = num;
    }

    public void SetCardProperty3Effect(float num)
    {
        currProperty3Effect = num;
    }
}