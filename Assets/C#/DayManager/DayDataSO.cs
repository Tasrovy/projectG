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
    public int drawNum;
    public int getCardNum;
    public string dailyDialog;
    public string specialDialog;
    public string target1;
    public string target2;
    public string target3;
    public string target4;
}

public class DayDataSO : ScriptableObject
{
    public List<DayData> dayDatas = new List<DayData>();
}
