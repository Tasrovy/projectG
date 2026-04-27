using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class DayManager : Singleton<DayManager>
{
    public Dictionary<int,UnityEvent> dayEvents = new Dictionary<int, UnityEvent>();
    public int dayNumber = 0;
    public int TargetType { get; private set; }
    public DayDataSO daySO;
    [SerializeField] private TMP_Text dayText;
    [Header("起始日期")]
    [SerializeField] private int startYear = 2026;
    [SerializeField] private int startMonth = 5;
    [SerializeField] private int startDay = 17;

    protected override bool IsPersistent => true;
    protected override void Awake()
    {
        base.Awake();
        daySO = ExcelLoader.Instance.ReadDayExcel("day.xlsx");
        if (daySO != null)
        {
            Debug.Log($"成功加载了 {daySO.dayDatas.Count} 天的数据");
        }
        // DUEL.Instance.OnEndDUEL.AddListener(NextDay);
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
        Debug.Log($"<color=#FFD700>========== [DayManager 检测器] 天数改变，当前是第 {dayNumber} 天 ==========</color>");
        CardManager.Instance.SetProbRarity1(daySO.dayDatas[dayNumber].probRarity1);
        CardManager.Instance.SetProbRarity2(daySO.dayDatas[dayNumber].probRarity2);
        CardManager.Instance.SetProbRarity3(daySO.dayDatas[dayNumber].probRarity3);
        CardManager.Instance.DrawCard(daySO.dayDatas[dayNumber].drawNum);
        Debug.Log($"[DayManager] {daySO.dayDatas[dayNumber].drawNum}");
        if(dayEvents.ContainsKey(dayNumber)) dayEvents[dayNumber]?.Invoke();
        UpdateDayText();
    }

    private void UpdateDayText()
    {
        if (dayText == null) return;

        DateTime startDate = new DateTime(startYear, startMonth, startDay);
        DateTime currentDate = startDate.AddDays(dayNumber - 1);
        dayText.text = $"{currentDate.Month}.{currentDate.Day}";
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

    /// <summary>
    /// 目标类型（1=友情籁绑 2=情绪依赖 3=安全感 4=魅力値），由 PropertiesShow 在游戏开始时写入
    /// </summary>
    public void SetTargetType(int type) => TargetType = type;

    public void OnDayEnd()
    {
        DataManager.Instance.SetNature1Effect(0);
        DataManager.Instance.SetNature2Effect(0);
        DataManager.Instance.SetNature3Effect(0);
    }

    public DateTime GetStartDate() => new DateTime(startYear, startMonth, startDay);
    public DateTime GetCurrentDate() => GetStartDate().AddDays(dayNumber - 1);
}
