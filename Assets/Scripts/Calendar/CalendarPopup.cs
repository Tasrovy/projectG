using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CalendarPopup : MonoBehaviour
{
    private GameObject todoList;
    private Transform gridParent;
    private TMP_Text titleText;
    private TMP_Text nowDayText;
    private TMP_Text nowWeekText;
    private GameObject dayCellPrefab;

    private int viewYear;
    private int viewMonth;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (dayCellPrefab == null) dayCellPrefab = Resources.Load<GameObject>("Prefabs/Calender/dayCell");

        if (gridParent == null) Debug.LogError("[CalendarPopup] 未找到子物体 btnGrid！");
        if (titleText == null) Debug.LogError("[CalendarPopup] 未找到 curMonth 上的 TMP_Text！");
        if (todoList == null) Debug.LogError("[CalendarPopup] 未找到子物体 todoList！");
        if (nowDayText == null) Debug.LogError("[CalendarPopup] 未找到 nowDay/day 上的 TMP_Text！");
        if (nowWeekText == null) Debug.LogError("[CalendarPopup] 未找到 nowDay/week 上的 TMP_Text！");
        if (dayCellPrefab == null) Debug.LogError("[CalendarPopup] 未能加载 Resources/Prefabs/Calender/dayCell！");

        DateTime current = GetCurrentDateSafe();
        viewYear = current.Year;
        viewMonth = current.Month;
        UpdateNowDayDisplay(current);
        RebuildCalendar();
    }

    public void ShowPreviousMonth()
    {
        DateTime d = new DateTime(viewYear, viewMonth, 1).AddMonths(-1);
        viewYear = d.Year;
        viewMonth = d.Month;
        RebuildCalendar();
    }

    public void ShowNextMonth()
    {
        DateTime d = new DateTime(viewYear, viewMonth, 1).AddMonths(1);
        viewYear = d.Year;
        viewMonth = d.Month;
        RebuildCalendar();
    }

    private void RebuildCalendar()
    {
        // 清空旧格子
        foreach (Transform child in gridParent)
            Destroy(child.gameObject);

        DateTime firstDay = new DateTime(viewYear, viewMonth, 1);
        int daysInMonth = DateTime.DaysInMonth(viewYear, viewMonth);
        int startColumn = (int)firstDay.DayOfWeek; // 周日=0

        for (int i = 0; i < 42; i++)
        {
            int dayNum = i - startColumn + 1;
            bool isValid = dayNum >= 1 && dayNum <= daysInMonth;

            GameObject cell = Instantiate(dayCellPrefab, gridParent, false);
            CalendarDayCell cellScript = cell.GetComponent<CalendarDayCell>();

            if (isValid)
                cellScript.Bind(new DateTime(viewYear, viewMonth, dayNum), true, OnDateClicked);
            else
                cellScript.Bind(default, false, null);
        }

        titleText.text = $"{viewYear}.{viewMonth}";
    }

    private void OnDateClicked(DateTime date)
    {
        Debug.Log($"[Calendar] 点击了 {date:yyyy-MM-dd}");
        if (todoList != null)
            todoList.SetActive(true);
        DateManager.Instance?.OnDateClicked(date);
    }

    private void ResolveReferences()
    {
        // 脚本挂载在 window 上，所有目标 UI 都是 window 的直接子物体
        // 优先使用 [SerializeField] 已在 Inspector 中赋值的引用，未赋值时用 transform.Find() 兜底

        if (gridParent == null)
        {
            gridParent = transform.Find("btnGrid");
        }

        if (titleText == null)
        {
            Transform curMonth = transform.Find("curMonth");
            if (curMonth != null)
                titleText = curMonth.GetComponent<TMP_Text>();
        }

        if (todoList == null)
        {
            Transform todo = transform.Find("todoList");
            if (todo != null)
                todoList = todo.gameObject;
        }

        if (nowDayText == null || nowWeekText == null)
        {
            Transform nowDay = transform.Find("nowDay");
            if (nowDay != null)
            {
                if (nowDayText == null)
                {
                    Transform day = nowDay.Find("day");
                    if (day != null) nowDayText = day.GetComponent<TMP_Text>();
                }
                if (nowWeekText == null)
                {
                    Transform week = nowDay.Find("week");
                    if (week != null) nowWeekText = week.GetComponent<TMP_Text>();
                }
            }
        }
    }

    private static DateTime GetCurrentDateSafe()
    {
        if (DayManager.Instance != null)
        {
            return DayManager.Instance.GetCurrentDate();
        }

        return DateTime.Today;
    }

    private void UpdateNowDayDisplay(DateTime date)
    {
        if (nowDayText != null)
        {
            nowDayText.text = $"{date.Month}.{date.Day}";
        }

        if (nowWeekText != null)
        {
            nowWeekText.text = date.ToString("ddd", CultureInfo.InvariantCulture).ToUpperInvariant();
        }
    }
}
