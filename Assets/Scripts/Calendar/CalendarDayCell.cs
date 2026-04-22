using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CalendarDayCell : MonoBehaviour
{
    private Button button;
    private TMP_Text label;
    private Image image;

    private void Awake()
    {
        button = GetComponent<Button>();
        label = GetComponentInChildren<TMP_Text>();
        image = GetComponent<Image>();
    }

    public void Bind(DateTime date, bool active, Action<DateTime> clickAction)
    {
        if (!active)
        {
            // 无效格子：隐藏所有视觉，禁用交互，但保留 GameObject 激活以维持 GridLayoutGroup 占位
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
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => clickAction?.Invoke(date));
    }
}
