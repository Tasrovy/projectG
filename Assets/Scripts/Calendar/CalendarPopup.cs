using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CalendarPopup : MonoBehaviour
{
    [SerializeField] private GameObject todoList;

    private GameObject popupRoot;
    private Transform gridParent;
    private TMP_Text titleText;
    private GameObject dayCellPrefab;

    private int viewYear;
    private int viewMonth;

    private void OnEnable()
    {
        if (popupRoot == null) popupRoot = transform.parent.gameObject;
        if (gridParent == null) gridParent = transform.Find("btnGrid");
        if (titleText == null) titleText = transform.Find("texts/curMonth").GetComponent<TMP_Text>();
        if (dayCellPrefab == null) dayCellPrefab = Resources.Load<GameObject>("Prefabs/Calender/dayCell");

        if (gridParent == null) Debug.LogError("[CalendarPopup] 未找到子物体 btnGrid！");
        if (titleText == null) Debug.LogError("[CalendarPopup] 未找到 texts/curMonth 上的 TMP_Text！");
        if (dayCellPrefab == null) Debug.LogError("[CalendarPopup] 未能加载 Resources/Prefabs/Calender/dayCell！");

        DateTime current = DayManager.Instance.GetCurrentDate();
        viewYear = current.Year;
        viewMonth = current.Month;
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
}
