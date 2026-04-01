using UnityEngine;

public class DataManager : Singleton<DataManager>
{
    public int nature1;
    public int nature2;
    public int nature3;
    public int MoneyNum;
    protected override bool IsPersistent => true;
    public float currNature1Effect;
    public float currNature2Effect;
    public float currNature3Effect;

    protected override void Awake()
    {
        base.Awake();
        nature1 = 0;
        nature2 = 0;
        nature3 = 0;
        MoneyNum = 0;
        currNature1Effect = 0;
        currNature2Effect = 0;
        currNature3Effect = 0;
    }

    public int GetNatureById(int id)
    {
        switch (id)
        {
            case 1 :return nature1;
            case 2 :return nature2;
            case 3 :return nature3;
        }
        return 0;
    }
    
    public void Add(int id, int num)
    {
        switch (id)
        {
            case 1 :AddNature1(num);
                break;
            case 2 :AddNature2(num);
                break;
            case 3 :AddNature3(num);
                break;
            case 4 :AddMoneyNum(num);
                break;
            default:
                Debug.LogError($"unknown id :{id}");
                break;
        }
    }

    public void AddTo(int id, int num)
    {
        switch (id)
        {
            case 1 :AddNature1To(num);
                break;
            case 2 :AddNature2To(num);
                break;
            case 3 :AddNature3To(num);
                break;
            default:
                Debug.LogError($"unknown id :{id}");
                break;
        }
    }

    public void AddNatureFromTo(int id1, int id2)
    {
        int sourceValue = 0;
        switch (id2)
        {
            case 1: sourceValue = nature1; break;
            case 2: sourceValue = nature2; break;
            case 3: sourceValue = nature3; break;
            default:
                Debug.LogError($"AddNatureFromTo failed: unknown source id2 :{id2}");
                return;
        }

        switch (id1)
        {
            case 1: nature1 = sourceValue; break;
            case 2: nature2 = sourceValue; break;
            case 3: nature3 = sourceValue; break;
            default:
                Debug.LogError($"AddNatureFromTo failed: unknown target id1 :{id1}");
                break;
        }
    }

    private void AddNature1(int num)
    {
        if(nature1+num<0) return;
        nature1 += (int)(num*(1+currNature1Effect));
    }

    private void AddNature2(int num)
    {
        if (nature2 + num < 0) return;
        nature2 += (int)(num*(1+currNature2Effect));
    }

    private void AddNature3(int num)
    {
        if (nature3 + num < 0) return;
        nature3 += (int)(num * (1 + currNature3Effect));
    }
    
    private void AddNature1To(int num)
    {
        if (nature1 < num) nature1 = num;
    }

    private void AddNature2To(int num)
    {
        if (nature2 < num) nature2 = num; 
    }

    private void AddNature3To(int num)
    {
        if (nature3<num) nature3 = num;
    }

    private void AddMoneyNum(int num)
    {
        if (MoneyNum + num < 0) return;
        MoneyNum += num;
    }

    public int GetNature1()=>nature1;
    public int GetNature2()=>nature2;
    public int GetNature3()=>nature3;
    public int GetMoneyNum()=>MoneyNum;

    public void SetNature1Effect(float num)
    {
        currNature1Effect = num;
    }

    public void SetNature2Effect(float num)
    {
        currNature2Effect = num;
    }

    public void SetNature3Effect(float num)
    {
        currNature3Effect = num;
    }
}