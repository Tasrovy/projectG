using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DayData
{
    public float probRarity1;
    public float probRarity2;
    public float probRarity3;
    public int day;
    public int profit;
}

public class DayDataSO : ScriptableObject
{
    public List<DayData> dayDatas = new List<DayData>();
}
