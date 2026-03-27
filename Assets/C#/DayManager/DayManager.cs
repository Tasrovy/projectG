using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DayManager : Singleton<DayManager>
{
    public Dictionary<int,UnityEvent>  dayEvents = new Dictionary<int, UnityEvent>();
    public int dayNumber = 1;

    public void NextDay()
    {
        OnDayEnd();
        dayNumber++;
        if(dayEvents.ContainsKey(dayNumber)) dayEvents[dayNumber]?.Invoke();
    }

    public void AddDayEvent(int day, UnityAction func)
    {
        if(!dayEvents.ContainsKey(day)) dayEvents[day] = new UnityEvent();
        dayEvents[day].AddListener(func);
    }
    
    public int GetDayNumber()=>dayNumber;

    public void OnDayEnd()
    {
        DataManager.Instance.SetCardProperty1Effect(0);
        DataManager.Instance.SetCardProperty2Effect(0);
        DataManager.Instance.SetCardProperty3Effect(0);
    }
}
