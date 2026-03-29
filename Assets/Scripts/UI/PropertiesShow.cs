using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PropertiesShow : MonoBehaviour
{
    private Scrollbar scrollbar1;
    private Scrollbar scrollbar2;
    private Scrollbar scrollbar3;

    private TMP_Text numText1_TMP;
    private Text numText1_Legacy;

    private TMP_Text numText2_TMP;
    private Text numText2_Legacy;

    private TMP_Text numText3_TMP;
    private Text numText3_Legacy;

    private void Awake()
    {
        InitializeReferences();
    }

    private void InitializeReferences()
    {
        // 1. 获取 propIcon_1 下的元素
        Transform prop1 = transform.Find("propIcon_1");
        if (prop1 != null)
        {
            scrollbar1 = prop1.GetComponentInChildren<Scrollbar>(true);
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
            scrollbar2 = prop2.GetComponentInChildren<Scrollbar>(true);
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
            scrollbar3 = prop3.GetComponentInChildren<Scrollbar>(true);
            Transform numTransform = FindChildRecursive(prop3, "num");
            if (numTransform != null)
            {
                numText3_TMP = numTransform.GetComponent<TMP_Text>();
                numText3_Legacy = numTransform.GetComponent<Text>();
            }
        }
    }

    private void OnEnable()
    {
        UpdatePropertiesShow();
    }

    private void Update()
    {
        // 如果需要在每帧实时刷新，可以将此方法放在 Update 中
        UpdatePropertiesShow();
    }

    public void UpdatePropertiesShow()
    {
        if (DataManager.Instance == null) return;

        int n1 = DataManager.Instance.nature1;
        int n2 = DataManager.Instance.nature2;
        int n3 = DataManager.Instance.nature3;

        // 根据最新要求，最小值为0，最大值为100
        float fill1 = Mathf.Clamp(n1 / 100f, 0f, 1f);
        float fill2 = Mathf.Clamp(n2 / 100f, 0f, 1f);
        float fill3 = Mathf.Clamp(n3 / 100f, 0f, 1f);

        if (scrollbar1 != null) scrollbar1.size = fill1;
        if (scrollbar2 != null) scrollbar2.size = fill2;
        if (scrollbar3 != null) scrollbar3.size = fill3;

        string n1Str = n1.ToString();
        if (numText1_TMP != null) numText1_TMP.text = n1Str;
        if (numText1_Legacy != null) numText1_Legacy.text = n1Str;

        string n2Str = n2.ToString();
        if (numText2_TMP != null) numText2_TMP.text = n2Str;
        if (numText2_Legacy != null) numText2_Legacy.text = n2Str;

        string n3Str = n3.ToString();
        if (numText3_TMP != null) numText3_TMP.text = n3Str;
        if (numText3_Legacy != null) numText3_Legacy.text = n3Str;
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
