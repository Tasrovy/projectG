using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DayManager : Singleton<DayManager>
{
    public Dictionary<int,UnityEvent> dayEvents = new Dictionary<int, UnityEvent>();
    public int dayNumber = 1;

    protected override void Awake()
    {
        base.Awake();
        DayDataSO daySO = ExcelLoader.Instance.ReadDayExcel("day.xlsx");
        if (daySO != null)
        {
            Debug.Log($"成功加载了 {daySO.dayDatas.Count} 天的数据");
        }
    }
    
    public void NextDay()
    {
        OnDayEnd();
        dayNumber++;
        if(dayEvents.ContainsKey(dayNumber)) dayEvents[dayNumber]?.Invoke();
    }

    public UnityEvent GetNextDayEvent()
    {
        return dayEvents[dayNumber++];
    }
    
    public void AddDayEvent(int day, UnityAction func)
    {
        if(!dayEvents.ContainsKey(day)) dayEvents[day] = new UnityEvent();
        dayEvents[day].AddListener(func);
    }
    
    public int GetDayNumber()=>dayNumber;

    public void OnDayEnd()
    {
        DataManager.Instance.SetNature1Effect(0);
        DataManager.Instance.SetNature2Effect(0);
        DataManager.Instance.SetNature3Effect(0);
    }
}
