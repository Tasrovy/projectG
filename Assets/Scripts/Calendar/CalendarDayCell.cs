using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CalendarDayCell : MonoBehaviour
{
    private static readonly Color ColorNormalBg   = Color.white;
    private static readonly Color ColorNormalText = Color.black;
    private static readonly Color ColorTodayText  = Color.red;
    private static readonly Color ColorSelectedBg = Color.black;
    private static readonly Color ColorSelectedText = Color.white;

    // 全局记录当前被选中的单元格
    private static CalendarDayCell currentSelected;

    private Button button;
    private TMP_Text label;
    private Image image;

    private bool isToday;
    private bool isSelected;

    private void Awake()
    {
        button = GetComponent<Button>();
        label = GetComponentInChildren<TMP_Text>();
        image = GetComponent<Image>();
    }

    public void Bind(DateTime date, bool active, Action<DateTime> clickAction)
    {
        // 如果该格之前是选中状态，重用前先清除全局引用
        if (currentSelected == this) currentSelected = null;
        isSelected = false;

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
        button.interactable = true;

        // 判断是否为今日
        isToday = false;
        if (DayManager.Instance != null)
        {
            DateTime today = DayManager.Instance.GetCurrentDate();
            isToday = (date.Year == today.Year && date.Month == today.Month && date.Day == today.Day);
        }

        ApplyVisual();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
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

            clickAction?.Invoke(date);
        });
    }

    private void ApplyVisual()
    {
        if (isSelected)
        {
            if (image != null) image.color = ColorSelectedBg;
            // 今日被选中：背景黑色，但文字保持红色
            label.color = isToday ? ColorTodayText : ColorSelectedText;
        }
        else if (isToday)
        {
            if (image != null) image.color = ColorNormalBg;
            label.color = ColorTodayText;
        }
        else
        {
            if (image != null) image.color = ColorNormalBg;
            label.color = ColorNormalText;
        }
    }
}
