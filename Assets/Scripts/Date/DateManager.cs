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

    // Canvas 下到 todoList 的候选路径（兼容旧层级与当前 calendar 结构）
    private static readonly string[] TodoListCandidatePaths =
    {
        "properties/calendar/window/todoList",
        "calendar/window/todoList",
        "window/todoList",
        "todoList"
    };

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
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (canvases == null || canvases.Length == 0)
        {
            Debug.LogWarning("[DateManager] 未找到任何 Canvas，无法自动绑定 todoList 文本。");
            return;
        }

        Transform todoList = null;
        foreach (Canvas canvas in canvases)
        {
            if (TryFindTodoListUnderCanvas(canvas.transform, out todoList))
            {
                break;
            }
        }

        if (todoList == null)
        {
            Debug.LogWarning("[DateManager] 未在任意 Canvas 下找到可用的 todoList（已尝试 properties/calendar/window/todoList 与 calendar/window/todoList 等路径）。");
            return;
        }

        if (morningTodo == null)   morningTodo   = FindTMP(todoList, "morning_todo");
        if (afternoonTodo == null) afternoonTodo = FindTMP(todoList, "afternoon_todo");
        if (afterclassTodo == null) afterclassTodo = FindTMP(todoList, "afterclass_todo");
        if (dayDesc == null) dayDesc = FindTMP(todoList, "dayDesc");
    }

    private static bool TryFindTodoListUnderCanvas(Transform canvasRoot, out Transform todoList)
    {
        todoList = null;
        if (canvasRoot == null)
        {
            return false;
        }

        for (int i = 0; i < TodoListCandidatePaths.Length; i++)
        {
            Transform candidate = canvasRoot.Find(TodoListCandidatePaths[i]);
            if (IsValidTodoList(candidate))
            {
                todoList = candidate;
                return true;
            }
        }

        // 兜底1：先找 calendar，再从其下取 window/todoList
        Transform calendar = FindChildRecursive(canvasRoot, "calendar");
        if (calendar != null)
        {
            Transform candidate = calendar.Find("window/todoList");
            if (IsValidTodoList(candidate))
            {
                todoList = candidate;
                return true;
            }
        }

        // 兜底2：全树找名为 todoList 的节点，并校验其子节点结构
        Transform fallback = FindChildRecursive(canvasRoot, "todoList");
        if (IsValidTodoList(fallback))
        {
            todoList = fallback;
            return true;
        }

        return false;
    }

    private static bool IsValidTodoList(Transform todoList)
    {
        if (todoList == null)
        {
            return false;
        }

        return todoList.Find("morning_todo") != null
            && todoList.Find("afternoon_todo") != null
            && todoList.Find("afterclass_todo") != null
            && todoList.Find("dayDesc") != null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
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
        RefreshCurrentDayData();
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
    /// 对外刷新当前天的 todo 文本（包含引用兜底）。
    /// </summary>
    public void RefreshCurrentDayData()
    {
        ResolveReferences();
        ShowCurrentDayData();
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
