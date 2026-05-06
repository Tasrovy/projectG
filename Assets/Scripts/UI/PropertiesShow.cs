using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PropertiesShow : MonoBehaviour
{
#region propIcons
    private Slider slider1;
    private Slider slider2;
    private Slider slider3;
    private Slider slider4;

    private Image fillImage1;
    private Image fillImage2;
    private Image fillImage3;
    private Image fillImage4;

    private Color defaultFillColor1;
    private Color defaultFillColor2;
    private Color defaultFillColor3;
    private Color defaultFillColor4;

    private TMP_Text numText1_TMP;
    private Text numText1_Legacy;

    private TMP_Text numText2_TMP;
    private Text numText2_Legacy;

    private TMP_Text numText3_TMP;
    private Text numText3_Legacy;

    private TMP_Text numText4_TMP;
    private Text numText4_Legacy;
#endregion

    private TMP_Text targetText;
    private int selectedTargetType = 1;

    private void Awake()
    {
        InitializeReferences();
        InitializeTargetTextReference();
    }

    private void InitializeTargetTextReference()
    {
        Transform targetTransform = FindChildRecursive(transform, "target");
        if (targetTransform == null) return;

        targetText = targetTransform.GetComponent<TMP_Text>();
    }

    /// <summary>
    /// 开局由 UI 的 UnityEvent 主动调用，随机一次并写入 DayManager。
    /// </summary>
    public void InitializeRandomTargetByEvent()
    {
        if (targetText == null)
        {
            InitializeTargetTextReference();
        }

        int targetType = Random.Range(1, 5);
        selectedTargetType = targetType;

        // 存入 DayManager 供其他系统读取
        if (DayManager.Instance != null)
            DayManager.Instance.SetTargetType(targetType);

        RefreshTargetText();
    }

    private void InitializeReferences()
    {
        // 1. 获取 propIcon_1 下的元素
        Transform prop1 = transform.Find("propIcon_1");
        if (prop1 != null)
        {
            slider1 = prop1.GetComponentInChildren<Slider>(true);
            fillImage1 = GetSliderFillImage(slider1);
            if (fillImage1 != null) defaultFillColor1 = fillImage1.color;
            Transform numTransform = FindChildRecursive(prop1, "num");
            if (numTransform != null)
            {
                numText1_TMP = numTransform.GetComponent<TMP_Text>();
                numText1_Legacy = numTransform.GetComponent<Text>();
            }
        }

        // 2. 获取 propIcon_2 下的元素
        Transform prop2 = transform.Find("propIcon_2");
        if (prop2 != null)
        {
            slider2 = prop2.GetComponentInChildren<Slider>(true);
            fillImage2 = GetSliderFillImage(slider2);
            if (fillImage2 != null) defaultFillColor2 = fillImage2.color;
            Transform numTransform = FindChildRecursive(prop2, "num");
            if (numTransform != null)
            {
                numText2_TMP = numTransform.GetComponent<TMP_Text>();
                numText2_Legacy = numTransform.GetComponent<Text>();
            }
        }

        // 3. 获取 propIcon_3 下的元素
        Transform prop3 = transform.Find("propIcon_3");
        if (prop3 != null)
        {
            slider3 = prop3.GetComponentInChildren<Slider>(true);
            fillImage3 = GetSliderFillImage(slider3);
            if (fillImage3 != null) defaultFillColor3 = fillImage3.color;
            Transform numTransform = FindChildRecursive(prop3, "num");
            if (numTransform != null)
            {
                numText3_TMP = numTransform.GetComponent<TMP_Text>();
                numText3_Legacy = numTransform.GetComponent<Text>();
            }
        }

        // 4. 获取 propIcon_4 下的元素
        Transform prop4 = transform.Find("propIcon_4");
        if (prop4 != null)
        {
            slider4 = prop4.GetComponentInChildren<Slider>(true);
            fillImage4 = GetSliderFillImage(slider4);
            if (fillImage4 != null) defaultFillColor4 = fillImage4.color;
            Transform numTransform = FindChildRecursive(prop4, "num");
            if (numTransform != null)
            {
                numText4_TMP = numTransform.GetComponent<TMP_Text>();
                numText4_Legacy = numTransform.GetComponent<Text>();
            }
        }
    }

    private void OnEnable()
    {
        if (DayManager.Instance != null)
            DayManager.OnDayAdvanced += RefreshTargetText;
        UpdatePropertiesShow();
    }

    private void OnDisable()
    {
        DayManager.OnDayAdvanced -= RefreshTargetText;
    }

    public void UpdatePropertiesShow()
    {
        if (DataManager.Instance == null) return;

        int n1 = DataManager.Instance.nature1;
        int n2 = DataManager.Instance.nature2;
        int n3 = DataManager.Instance.nature3;
        int money = DataManager.Instance.MoneyNum;
        float n4 = money;

        UpdateSliderVisual(slider1, fillImage1, defaultFillColor1, n1);
        UpdateSliderVisual(slider2, fillImage2, defaultFillColor2, n2);
        UpdateSliderVisual(slider3, fillImage3, defaultFillColor3, n3);
        UpdateSliderVisual(slider4, fillImage4, defaultFillColor4, n4);

        string n1Str = n1.ToString();
        if (numText1_TMP != null) numText1_TMP.text = n1Str;
        if (numText1_Legacy != null) numText1_Legacy.text = n1Str;

        string n2Str = n2.ToString();
        if (numText2_TMP != null) numText2_TMP.text = n2Str;
        if (numText2_Legacy != null) numText2_Legacy.text = n2Str;

        string n3Str = n3.ToString();
        if (numText3_TMP != null) numText3_TMP.text = n3Str;
        if (numText3_Legacy != null) numText3_Legacy.text = n3Str;

        string n4Str = FormatValue(n4);
        if (numText4_TMP != null) numText4_TMP.text = n4Str;
        if (numText4_Legacy != null) numText4_Legacy.text = n4Str;
    }

    private void RefreshTargetText()
    {
        if (targetText == null) return;

        int targetType = selectedTargetType;
        if (DayManager.Instance != null && DayManager.Instance.TargetType >= 1 && DayManager.Instance.TargetType <= 4)
        {
            targetType = DayManager.Instance.TargetType;
            selectedTargetType = targetType;
        }

        string targetName = targetType switch
        {
            1 => "友情羁绊",
            2 => "情绪依赖",
            3 => "安全感",
            4 => "金钱",
            _ => "友情羁绊"
        };

        string detailLine = BuildNextCheckDetailLine(targetType);
        targetText.text = $"<size=18>攻略目标：{targetName}</size>\n<size=12>{detailLine}</size>";
    }

    private string BuildNextCheckDetailLine(int targetType)
    {
        if (DayManager.Instance == null || DayManager.Instance.daySO == null || DayManager.Instance.daySO.dayDatas == null)
        {
            return "距离下次检定：--天，目标值：--";
        }

        List<DayData> dayDatas = DayManager.Instance.daySO.dayDatas;
        int currentDay = DayManager.Instance.GetDayNumber();

        // 用 dayData.day 字段做差值，避免数组下标与天数不一致时出现偏移
        DayData nextCheck = null;
        foreach (DayData d in dayDatas)
        {
            if (d.day >= currentDay && !string.IsNullOrEmpty(d.failedDialog))
            {
                if (nextCheck == null || d.day < nextCheck.day)
                    nextCheck = d;
            }
        }

        if (nextCheck == null)
            return "距离下次检定：--天，目标值：--";

        int daysUntilCheck = nextCheck.day - currentDay;
        int targetValue = GetTargetValueByType(nextCheck, targetType);
        Debug.Log($"[PropertiesShow] 距离下次检定：{daysUntilCheck}天，目标值：{targetValue}");
        return $"距离下次检定：{daysUntilCheck}天，目标值：{targetValue}";
    }

    private int GetTargetValueByType(DayData dayData, int targetType)
    {
        return targetType switch
        {
            1 => dayData.target1,
            2 => dayData.target2,
            3 => dayData.target3,
            4 => dayData.target4,
            _ => dayData.target1
        };
    }

    private void UpdateSliderVisual(Slider slider, Image fillImage, Color defaultColor, float value)
    {
        if (slider == null) return;

        float maxValue = value > 100f ? 1000f : 100f;
        float normalized = Mathf.Clamp(value / maxValue, 0f, 1f);
        slider.normalizedValue = normalized;

        if (fillImage != null)
        {
            fillImage.color = value > 100f ? Color.red : defaultColor;
        }
    }

    private Image GetSliderFillImage(Slider slider)
    {
        if (slider == null) return null;
        if (slider.fillRect == null) return null;
        return slider.fillRect.GetComponent<Image>();
    }

    private string FormatValue(float value)
    {
        float rounded = Mathf.Round(value);
        if (Mathf.Approximately(value, rounded))
        {
            return ((int)rounded).ToString();
        }

        return value.ToString("0.##");
    }

    /// <summary>
    /// 递归查找子物体
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string childName)
    {
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
}
