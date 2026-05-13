using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class DayManager : Singleton<DayManager>
{
    /// <summary>每次过天（NextDay）结束后触发，用于驱动 UI 刷新。</summary>
    public static event System.Action OnDayAdvanced;

    public Dictionary<int,UnityEvent> dayEvents = new Dictionary<int, UnityEvent>();
    public int dayNumber = 0;
    public int TargetType { get; private set; }
    public DayDataSO daySO;
    [SerializeField] private TMP_Text dayText;

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
        OnDayAdvanced?.Invoke();
    }

    /// <summary>
    /// 从 daySO 第一行读取起始月日，按游戏天数推算当前日历日期（年份固定2026）。
    /// 若当天是周五则跳过周末，直接跳到下周一（+3天）。
    /// </summary>
    private DateTime ComputeCurrentDate()
    {
        if (daySO == null || daySO.dayDatas.Count == 0) return new DateTime(2026, 1, 1);
        var startData = daySO.dayDatas[0];
        var parts = startData.date?.Split('_');
        int m = (parts != null && parts.Length == 2 && int.TryParse(parts[0], out int pm)) ? pm : 1;
        int d = (parts != null && parts.Length == 2 && int.TryParse(parts[1], out int pd)) ? pd : 1;
        DateTime date = new DateTime(2026, m, d);
        int n = dayNumber - 1;
        for (int i = 0; i < n; i++)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek == DayOfWeek.Saturday)      date = date.AddDays(2);
            else if (date.DayOfWeek == DayOfWeek.Sunday)   date = date.AddDays(1);
        }
        return date;
    }

    private void UpdateDayText()
    {
        if (dayText == null) return;
        DateTime currentDate = ComputeCurrentDate();
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
    /// 目标类型（1=友情羁绊 2=情绪依赖 3=安全感 4=金钱），由 PropertiesShow 在游戏开始时写入
    /// </summary>
    public void SetTargetType(int type) => TargetType = type;

    /// <summary>
    /// 游戏失败后重置天数与目标类型，供 OnGameFailed 调用。
    /// 重置后下一次调用 NextDay() 将从第1天重新开始。
    /// </summary>
    public void ResetToStart()
    {
        dayNumber = 0;
        TargetType = 0;
        dayEvents.Clear();
        Debug.Log("[DayManager] ResetToStart 完成，dayNumber 重置为 0。");
    }

    public void OnDayEnd()
    {
        DataManager.Instance.SetNature1Effect(0);
        DataManager.Instance.SetNature2Effect(0);
        DataManager.Instance.SetNature3Effect(0);
    }

    public DateTime GetStartDate()
    {
        if (daySO == null || daySO.dayDatas.Count == 0) return new DateTime(2026, 1, 1);
        var startData = daySO.dayDatas[0];
        var parts = startData.date?.Split('_');
        int m = (parts != null && parts.Length == 2 && int.TryParse(parts[0], out int pm)) ? pm : 1;
        int d = (parts != null && parts.Length == 2 && int.TryParse(parts[1], out int pd)) ? pd : 1;
        return new DateTime(2026, m, d);
    }
    public DateTime GetCurrentDate() => ComputeCurrentDate();
}
