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
        SkipWeekendDays();
        Debug.Log($"<color=#FFD700>========== [DayManager 检测器] 天数改变，当前是第 {dayNumber} 天 ==========</color>");
        CardManager.Instance.SetProbRarity1(daySO.dayDatas[dayNumber].probRarity1);
        CardManager.Instance.SetProbRarity2(daySO.dayDatas[dayNumber].probRarity2);
        CardManager.Instance.SetProbRarity3(daySO.dayDatas[dayNumber].probRarity3);
        CardManager.Instance.DrawCard(daySO.dayDatas[dayNumber].drawNum);
        Debug.Log($"[DayManager] {daySO.dayDatas[dayNumber].drawNum}");
        if(dayEvents.ContainsKey(dayNumber)) dayEvents[dayNumber]?.Invoke();
        OnDayAdvanced?.Invoke();
    }

    /// <summary>
    /// 若当前 dayNumber 对应周六或周日，持续递增直到落在工作日（下周一）。
    /// 也处理意外进入周末的情况。最多跳过 7 天防止死循环。
    /// </summary>
    private void SkipWeekendDays()
    {
        for (int i = 0; i < 7; i++)
        {
            DayOfWeek dow = GetCurrentDate().DayOfWeek;
            if (dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday)
                break;
            dayNumber++;
            Debug.Log($"[DayManager] 跳过周末，dayNumber 推进至 {dayNumber}（{GetCurrentDate().DayOfWeek}）");
        }
    }

    public void UpdateDayText()
    {
        if (dayText == null) return;
        if (dayNumber <= 0 || daySO == null || dayNumber - 1 >= daySO.dayDatas.Count) return;

        string dateStr = daySO.dayDatas[dayNumber - 1].date;
        if (string.IsNullOrEmpty(dateStr)) return;

        string[] parts = dateStr.Split('_');
        if (parts.Length == 2 && int.TryParse(parts[0], out int month) && int.TryParse(parts[1], out int day))
        {
            dayText.text = $"{month}.{day}";
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
        if (dayNumber > 0 && daySO != null && dayNumber - 1 < daySO.dayDatas.Count)
        {
            string dateStr = daySO.dayDatas[dayNumber - 1].date;
            if (!string.IsNullOrEmpty(dateStr))
            {
                string[] parts = dateStr.Split('_');
                if (parts.Length == 2 && int.TryParse(parts[0], out int month) && int.TryParse(parts[1], out int day))
                {
                    return new DateTime(DateTime.Now.Year, month, day);
                }
            }
        }
        return DateTime.Now;
    }


}
