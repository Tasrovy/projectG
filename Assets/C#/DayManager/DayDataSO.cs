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
    public string date; // 格式 MM_DD，如 05_18
    public int profit;
    public int drawNum;
    public int getCardNum;
    public string dailyDialog;
    public string specialDialog;
    public string failedDialog;
    public int target1;
    public int target2;
    public int target3;
    public int target4;
    public int targetCharm;
}

public class DayDataSO : ScriptableObject
{
    public List<DayData> dayDatas = new List<DayData>();
}
