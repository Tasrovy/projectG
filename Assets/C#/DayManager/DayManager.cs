using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DayManager : Singleton<DayManager>
{
    public Dictionary<int,UnityEvent> dayEvents = new Dictionary<int, UnityEvent>();
    public int dayNumber = 0;
    public DayDataSO daySO;
    protected override bool IsPersistent => true;
    protected override void Awake()
    {
        base.Awake();
        daySO = ExcelLoader.Instance.ReadDayExcel("day.xlsx");
        if (daySO != null)
        {
            Debug.Log($"成功加载了 {daySO.dayDatas.Count} 天的数据");
        }
        DUEL.Instance.OnEndDUEL.AddListener(NextDay);
    }

    private void Start()
    {
        NextDay();
        Debug.Log($"[DayManager] 初始化完成，当前是第 {dayNumber} 天");
    }
    
    public void NextDay()
    {
        OnDayEnd();
        dayNumber++;
        Debug.Log($"========== [DayManager 检测器] 天数改变，当前是第 {dayNumber} 天 ==========");
        CardManager.Instance.SetProbRarity1(daySO.dayDatas[dayNumber].probRarity1);
        CardManager.Instance.SetProbRarity2(daySO.dayDatas[dayNumber].probRarity2);
        CardManager.Instance.SetProbRarity3(daySO.dayDatas[dayNumber].probRarity3);
        CardManager.Instance.DrawCard(daySO.dayDatas[dayNumber].drawNum);
        Debug.Log($"[DayManager] {daySO.dayDatas[dayNumber].drawNum}");
        if(dayEvents.ContainsKey(dayNumber)) dayEvents[dayNumber]?.Invoke();
    }

    public UnityEvent GetNextDayEvent()
    {
        if(!dayEvents.ContainsKey(dayNumber+1)) dayEvents[dayNumber+1] = new UnityEvent();
        return dayEvents[dayNumber+1];
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
