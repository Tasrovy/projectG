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
    public bool IsCurrentDayFinalDay { get; private set; }
    public DayDataSO daySO;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text weekText;


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
        SkipWeekendDays();

        if (daySO == null || daySO.dayDatas == null || daySO.dayDatas.Count == 0)
        {
            Debug.LogError("[DayManager] daySO 或 dayDatas 为空，无法过天。");
            return;
        }

        if (dayNumber <= 0 || dayNumber > daySO.dayDatas.Count)
        {
            Debug.LogWarning($"[DayManager] NextDay 超出 day 配置范围：dayNumber={dayNumber}, count={daySO.dayDatas.Count}。将停留在最后一天以避免越界。");
            dayNumber = Mathf.Clamp(dayNumber, 1, daySO.dayDatas.Count);
            UpdateFinalDayFlag();
            OnDayAdvanced?.Invoke();
            return;
        }

        UpdateFinalDayFlag();
        int dayIndex = dayNumber - 1;
        DayData currentDayData = daySO.dayDatas[dayIndex];

        Debug.Log($"<color=#FFD700>========== [DayManager 检测器] 天数改变，当前是第 {dayNumber} 天 ==========</color>");
        if (CardManager.Instance != null)
        {
            CardManager.Instance.SetProbRarity1(currentDayData.probRarity1);
            CardManager.Instance.SetProbRarity2(currentDayData.probRarity2);
            CardManager.Instance.SetProbRarity3(currentDayData.probRarity3);
        }
        Debug.Log($"[DayManager] drawNum={currentDayData.drawNum}, IsCurrentDayFinalDay={IsCurrentDayFinalDay}");
        if(dayEvents.ContainsKey(dayNumber)) dayEvents[dayNumber]?.Invoke();
        OnDayAdvanced?.Invoke();
    }

    /// <summary>
    /// 若当前 dayNumber 对应周六或周日，持续递增直到落在工作日（下周一）。
    /// 也处理意外进入周末的情况。最多跳过 7 天防止死循环。
    /// </summary>
    private void SkipWeekendDays()
    {
        if (daySO == null || daySO.dayDatas == null || daySO.dayDatas.Count == 0)
            return;

        for (int i = 0; i < 7; i++)
        {
            if (dayNumber <= 0 || dayNumber > daySO.dayDatas.Count)
                break;

            DayOfWeek dow = GetCurrentDate().DayOfWeek;
            if (dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday)
                break;
            dayNumber++;
            Debug.Log($"[DayManager] 跳过周末，dayNumber 推进至 {dayNumber}（{GetCurrentDate().DayOfWeek}）");
        }
    }

    /// <summary>
    /// 预览下一次 NextDay() 实际会落到的工作日编号（已包含周末跳过逻辑）。
    /// </summary>
    public int GetPreviewNextDayNumber()
    {
        int candidate = dayNumber + 1;
        for (int i = 0; i < 7; i++)
        {
            if (daySO == null || daySO.dayDatas == null || candidate - 1 >= daySO.dayDatas.Count)
                break;

            DayOfWeek dow = GetDayOfWeekByDayNumber(candidate);
            if (dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday)
                break;
            candidate++;
        }
        return candidate;
    }

    private void UpdateFinalDayFlag()
    {
        if (daySO == null || daySO.dayDatas == null || daySO.dayDatas.Count == 0 || dayNumber <= 0)
        {
            IsCurrentDayFinalDay = false;
            return;
        }

        int previewNext = GetPreviewNextDayNumber();
        IsCurrentDayFinalDay = previewNext > daySO.dayDatas.Count;
    }

    public DayOfWeek GetDayOfWeekByDayNumber(int targetDayNumber)
    {
        DateTime date = GetDateByDayNumber(targetDayNumber);
        return date.DayOfWeek;
    }

    public DateTime GetDateByDayNumber(int targetDayNumber)
    {
        if (targetDayNumber > 0 && daySO != null && targetDayNumber - 1 < daySO.dayDatas.Count)
        {
            string dateStr = daySO.dayDatas[targetDayNumber - 1].date;
            if (!string.IsNullOrEmpty(dateStr))
            {
                string[] parts = dateStr.Split('_');
                if (parts.Length == 2 && int.TryParse(parts[0], out int month) && int.TryParse(parts[1], out int day))
                {
                    return new DateTime(2026, month, day);
                }
            }
        }
        return DateTime.Now;
    }

    public void UpdateDayText()
    {
        if (dayNumber <= 0 || daySO == null || dayNumber - 1 >= daySO.dayDatas.Count) return;

        string dateStr = daySO.dayDatas[dayNumber - 1].date;
        if (string.IsNullOrEmpty(dateStr)) return;

        string[] parts = dateStr.Split('_');
        if (parts.Length == 2 && int.TryParse(parts[0], out int month) && int.TryParse(parts[1], out int day))
        {
            if (dayText != null)
                dayText.text = $"{month}/{day}";

            if (weekText != null)
                weekText.text = GetWeekdayAbbreviation(new DateTime(2026, month, day).DayOfWeek);
        }
    }

    private string GetWeekdayAbbreviation(DayOfWeek dayOfWeek)
    {
        switch (dayOfWeek)
        {
            case DayOfWeek.Monday:
                return "MON";
            case DayOfWeek.Tuesday:
                return "TUE";
            case DayOfWeek.Wednesday:
                return "WED";
            case DayOfWeek.Thursday:
                return "THU";
            case DayOfWeek.Friday:
                return "FRI";
            case DayOfWeek.Saturday:
                return "SAT";
            default:
                return "SUN";
        }
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
        IsCurrentDayFinalDay = false;
        dayEvents.Clear();
        Debug.Log("[DayManager] ResetToStart 完成，dayNumber 重置为 0。");
    }

    public void OnDayEnd()
    {
        DataManager.Instance.SetNature1Effect(0);
        DataManager.Instance.SetNature2Effect(0);
        DataManager.Instance.SetNature3Effect(0);
    }

    /// <summary>
    /// 从 day 表的 date 字段（格式如 05_16）解析当前游戏日期。
    /// 年份取系统当前年，月日来自表格。
    /// </summary>
    public DateTime GetCurrentDate()
    {
        return GetDateByDayNumber(dayNumber);
    }
}
