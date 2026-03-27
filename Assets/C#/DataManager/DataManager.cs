using UnityEngine;

public class DataManager : Singleton<DataManager>
{
    public int cardProperty1;
    public int cardProperty2;
    public int cardProperty3;

    void Awake()
    {
        cardProperty1 = 0;
        cardProperty2 = 0;
        cardProperty3 = 0;
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
            default:
                Debug.LogError($"unknown id :{id}");
                break;
        }
    }

    private void AddCardProperty1(int num)
    {
        if(cardProperty1+num<0) return;
        cardProperty1 += num;
    }

    private void AddCardProperty2(int num)
    {
        if (cardProperty2 + num < 0) return;
        cardProperty2 += num;
    }

    private void AddCardProperty3(int num)
    {
        if (cardProperty3 + num < 0) return;
        cardProperty3 += num;
    }
}