using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CalendarDayCell : MonoBehaviour
{
    private static readonly Color ColorTransparentBg = new Color(1f, 1f, 1f, 0f);
    private static readonly Color ColorNormalText = Color.black;
    private static readonly Color ColorTodayText  = Color.red;
    private static readonly Color ColorSelectedBgTint = Color.white;
    private static readonly Color ColorSelectedText = Color.black;

    // 全局记录当前被选中的单元格
    private static CalendarDayCell currentSelected;

    [Header("选中背景")]
    [SerializeField] private Sprite selectedBackgroundSprite;

    private Button button;
    private TMP_Text label;
    private Image image;
    private Sprite originalBackgroundSprite;

    private bool isToday;
    private bool isSelected;

    private RectTransform rectTransform;
    private Transform calendarRoot;
    private Action<DateTime> boundClickAction;
    private DateTime boundDate;
    private bool isBoundClickable;

    private static readonly System.Collections.Generic.List<CalendarDayCell> RegisteredCells = new System.Collections.Generic.List<CalendarDayCell>();
    private static CalendarDayCellInputRouter inputRouter;

    private sealed class CalendarDayCellInputRouter : MonoBehaviour
    {
        private void Update()
        {
            ProcessManualClickGlobal();
        }
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        label = GetComponentInChildren<TMP_Text>(true);
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        ResolveCalendarRoot();
        EnsureInputRouter();
        if (image != null) originalBackgroundSprite = image.sprite;
    }

    private void OnEnable()
    {
        RegisterCell(this);
    }

    private void OnDisable()
    {
        UnregisterCell(this);
    }

    private void OnDestroy()
    {
        UnregisterCell(this);
        if (currentSelected == this)
        {
            currentSelected = null;
        }
    }

    public void Bind(DateTime date, bool active, Action<DateTime> clickAction)
    {
        // 在隐藏层级下实例化时，确保引用已就绪。
        if (button == null || label == null || image == null)
        {
            button = GetComponent<Button>();
            label = GetComponentInChildren<TMP_Text>(true);
            image = GetComponent<Image>();
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            ResolveCalendarRoot();
            EnsureInputRouter();
            if (image != null && originalBackgroundSprite == null)
            {
                originalBackgroundSprite = image.sprite;
            }
        }

        if (button == null || label == null)
        {
            Debug.LogError("[CalendarDayCell] 缺少 Button 或 TMP_Text，无法绑定日期单元格。");
            return;
        }

        // 如果该格之前是选中状态，重用前先清除全局引用
        if (currentSelected == this) currentSelected = null;
        isSelected = false;
        boundClickAction = null;
        boundDate = default;
        isBoundClickable = false;

        if (!active)
        {
            label.text = "";
            button.interactable = false;
            button.onClick.RemoveAllListeners();
            if (image != null) image.enabled = false;
            label.enabled = false;
            return;
        }

        label.enabled = true;
        if (image != null) image.enabled = true;
        label.text = date.Day.ToString();
        button.interactable = false; // 点击逻辑改由脚本手动判定
        boundDate = date;
        boundClickAction = clickAction;
        isBoundClickable = (clickAction != null);

        // 判断是否为今日
        isToday = false;
        if (DayManager.Instance != null)
        {
            DateTime today = DayManager.Instance.GetCurrentDate();
            isToday = (date.Year == today.Year && date.Month == today.Month && date.Day == today.Day);
        }

        ApplyVisual();
    }

    private static void ProcessManualClickGlobal()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        for (int i = RegisteredCells.Count - 1; i >= 0; i--)
        {
            CalendarDayCell cell = RegisteredCells[i];
            if (cell == null)
            {
                RegisteredCells.RemoveAt(i);
                continue;
            }

            if (!cell.CanHandleManualClick())
            {
                continue;
            }

            if (cell.ContainsPointer(Input.mousePosition))
            {
                cell.HandleCellClicked();
                break;
            }
        }
    }

    private bool CanHandleManualClick()
    {
        if (!isBoundClickable)
        {
            return false;
        }

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return false;
        }

        ResolveCalendarRoot();
        if (calendarRoot == null || !calendarRoot.gameObject.activeInHierarchy)
        {
            return false;
        }

        return true;
    }

    private bool ContainsPointer(Vector3 screenPoint)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return false;
            }
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = canvas.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, eventCamera);
    }

    private void HandleCellClicked()
    {
        // 恢复上一个选中格子的原始颜色
        if (currentSelected != null && currentSelected != this)
        {
            currentSelected.isSelected = false;
            currentSelected.ApplyVisual();
        }

        isSelected = true;
        currentSelected = this;
        ApplyVisual();

        boundClickAction?.Invoke(boundDate);
    }

    private void ResolveCalendarRoot()
    {
        if (calendarRoot != null)
        {
            return;
        }

        Transform current = transform;
        while (current != null)
        {
            if (string.Equals(current.name, "calendar", StringComparison.OrdinalIgnoreCase))
            {
                calendarRoot = current;
                return;
            }
            current = current.parent;
        }
    }

    private static void EnsureInputRouter()
    {
        if (inputRouter != null)
        {
            return;
        }

        GameObject routerGo = new GameObject("CalendarDayCellInputRouter");
        DontDestroyOnLoad(routerGo);
        inputRouter = routerGo.AddComponent<CalendarDayCellInputRouter>();
    }

    private static void RegisterCell(CalendarDayCell cell)
    {
        if (cell == null)
        {
            return;
        }

        if (!RegisteredCells.Contains(cell))
        {
            RegisteredCells.Add(cell);
        }
    }

    private static void UnregisterCell(CalendarDayCell cell)
    {
        if (cell == null)
        {
            return;
        }

        RegisteredCells.Remove(cell);
    }

    private void ApplyVisual()
    {
        if (isSelected)
        {
            if (image != null)
            {
                image.sprite = selectedBackgroundSprite != null ? selectedBackgroundSprite : originalBackgroundSprite;
                image.color = ColorSelectedBgTint;
            }
            // 今日被选中：背景黑色，但文字保持红色
            label.color = isToday ? ColorTodayText : ColorSelectedText;
        }
        else
        {
            if (image != null)
            {
                image.sprite = originalBackgroundSprite;
                image.color = ColorTransparentBg;
            }

            label.color = isToday ? ColorTodayText : ColorNormalText;
        }
    }
}
