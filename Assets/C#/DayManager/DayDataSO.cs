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
    public string date;
    public int profit;
    public int drawNum;
    public int getCardNum;
    public string skipBackground; // Resources 中的跳过加载背景 Sprite 路径（不含扩展名）
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
