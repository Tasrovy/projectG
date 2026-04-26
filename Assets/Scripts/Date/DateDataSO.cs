using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DateData
{
    public int day;
    public string date;
    public string morning;
    public string afternoon;
    public string afterclass;
    public string text;
}

public class DateDataSO : ScriptableObject
{
    public List<DateData> dateDatas = new List<DateData>();
}
