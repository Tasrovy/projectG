using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UpdateMoney : MonoBehaviour
{
    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        UpdateText();
    }

    public void UpdateText()
    {
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();
            
        if (textComponent != null && DataManager.Instance != null)
        {
            textComponent.text = DataManager.Instance.MoneyNum.ToString();
        }
    }
}
