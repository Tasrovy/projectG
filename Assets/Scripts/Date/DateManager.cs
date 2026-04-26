using System;
using UnityEngine;
using TMPro;

public class DateManager : Singleton<DateManager>
{
    protected override bool IsPersistent => false;

    [SerializeField] private string excelFileName = "date.xlsx";

    public DateDataSO dateSO;

    // 留空则运行时自动从 Canvas 路径查找，也可在 Inspector 手动拖入覆盖
    private TMP_Text morningTodo;
    private TMP_Text afternoonTodo;
    private TMP_Text afterclassTodo;
    private TMP_Text dayDesc;

    // Canvas 下到 todoList 的路径
    private const string TodoListPath = "properties/calendar/window/todoList";

    protected override void Awake()
    {
        base.Awake();
        dateSO = ExcelLoader.Instance.ReadDateExcel(excelFileName);
        if (dateSO != null)
            Debug.Log($"[DateManager] 成功加载了 {dateSO.dateDatas.Count} 条日期数据");
        ResolveReferences();
    }

    /// <summary>
    /// 通过 Canvas → 路径 查找 TMP 引用，父物体 inactive 也能找到。
    /// Inspector 已手动拖入的字段不会被覆盖。
    /// </summary>
    private void ResolveReferences()
    {
        // 找没有父物体的根级 Canvas（排除嵌套在其他物体下的 Canvas）
        Canvas canvas = null;
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.transform.parent == null)
            {
                canvas = c;
                break;
            }
        }

        if (canvas == null)
        {
            Debug.LogWarning("[DateManager] 未找到根级 Canvas，无法自动绑定 todoList 文本。");
            return;
        }

        Transform todoList = canvas.transform.Find(TodoListPath);
        if (todoList == null)
        {
            Debug.LogWarning($"[DateManager] 未在 Canvas 下找到路径: {TodoListPath}");
            return;
        }

        if (morningTodo == null)   morningTodo   = FindTMP(todoList, "morning_todo");
        if (afternoonTodo == null) afternoonTodo = FindTMP(todoList, "afternoon_todo");
        if (afterclassTodo == null) afterclassTodo = FindTMP(todoList, "afterclass_todo");
        if (dayDesc == null)       dayDesc       = FindTMP(todoList, "dayDesc");
    }

    private static TMP_Text FindTMP(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        if (t == null)
        {
            Debug.LogWarning($"[DateManager] todoList 下未找到子物体: {childName}");
            return null;
        }
        return t.GetComponent<TMP_Text>();
    }

    private void Start()
    {
        // 初始显示当天数据
        ShowCurrentDayData();
    }

    /// <summary>
    /// 日历格子点击入口，由 CalendarPopup 调用。
    /// 按格式 MM_dd（如 05_16）在 dateSO 中查找对应日期数据，
    /// 找到则填充 todoList 四个文本，否则清空。
    /// </summary>
    public void OnDateClicked(DateTime date)
    {
        if (dateSO == null) return;

        string dateKey = $"{date.Month:D2}_{date.Day:D2}";
        DateData found = FindByDateKey(dateKey);

        if (found != null)
            SetTodoTexts(found.morning, found.afternoon, found.afterclass, found.text);
        else
            SetTodoTexts("", "", "", "");
    }

    /// <summary>
    /// 显示 DayManager 当天天数对应的日期数据。
    /// </summary>
    private void ShowCurrentDayData()
    {
        if (dateSO == null || DayManager.Instance == null) return;

        int currentDay = DayManager.Instance.GetDayNumber();
        DateData found = null;

        foreach (var data in dateSO.dateDatas)
        {
            if (data.day == currentDay)
            {
                found = data;
                break;
            }
        }

        if (found != null)
            SetTodoTexts(found.morning, found.afternoon, found.afterclass, found.text);
        else
            SetTodoTexts("", "", "", "");
    }

    private DateData FindByDateKey(string dateKey)
    {
        if (dateSO == null) return null;

        foreach (var data in dateSO.dateDatas)
        {
            if (data.date == dateKey)
                return data;
        }

        return null;
    }

    private void SetTodoTexts(string morning, string afternoon, string afterclass, string text)
    {
        SetTMP(morningTodo,    morning);
        SetTMP(afternoonTodo,  afternoon);
        SetTMP(afterclassTodo, afterclass);
        SetTMP(dayDesc,        text);
    }

    private static void SetTMP(TMP_Text tmp, string value)
    {
        if (tmp == null) return;
        tmp.richText = true;
        tmp.text = ProcessRichText(value);
    }

    /// <summary>
    /// 将 Excel 单元格里的字面 \n 转换为真正的换行符，
    /// 其余 TMP 富文本标签（&lt;b&gt;、&lt;size&gt; 等）直接透传。
    /// </summary>
    private static string ProcessRichText(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        return raw.Replace("\\n", "\n");
    }
}
