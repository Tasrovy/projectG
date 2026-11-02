using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Yarn.Unity;

public class PlayerNameHandler : MonoBehaviour
{
    [Header("U引用")]
    public TMP_InputField nameInputField;
    public string defaultName = "Player";

    [Header("Yarn Spinner 引用")]
    public VariableStorageBehaviour variableStorage;

    private void Start()
    {
        if (variableStorage == null)
        {
            DialogueRunner runner = FindAnyObjectByType<DialogueRunner>();
            if (runner != null)
            {
                variableStorage = runner.VariableStorage;
            }
        }

        string initialName = defaultName;
        if (variableStorage != null && variableStorage.TryGetValue("$MY_NAME", out string storedName) && !string.IsNullOrWhiteSpace(storedName))
        {
            initialName = storedName;
        }

        if (nameInputField != null)
        {
            nameInputField.text = initialName;
        }
    }

    public void ConfirmName()
    {
        string playerName = nameInputField != null ? nameInputField.text : string.Empty;
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = defaultName;
        }

        playerName = playerName.Trim();

        // 仅使用 Yarn 变量层同步玩家名，不做本地偏好持久化。
        if (variableStorage == null)
        {
            DialogueRunner runner = FindAnyObjectByType<DialogueRunner>();
            if (runner != null)
            {
                variableStorage = runner.VariableStorage;
            }
        }

        if (variableStorage != null)
        {
            variableStorage.SetValue("$MY_NAME", playerName);
            RefreshCalendarAndUiTexts();
            Debug.Log($"Player name set to: {playerName}");
            Debug.Log(variableStorage.TryGetValue("$MY_NAME", out string retrievedName) 
                ? $"Retrieved name from storage: {retrievedName}" 
                : "Failed to retrieve name from storage.");
        }
        else
        {
            Debug.LogWarning("[PlayerNameHandler] 未找到 VariableStorage，无法写入 $MY_NAME。");
        }
    }

    private static void RefreshCalendarAndUiTexts()
    {
        if (DayManager.Instance != null)
        {
            DayManager.Instance.UpdateDayText();
        }

        if (PropertiesShow.Instance != null)
        {
            PropertiesShow.Instance.RefreshAllUiTexts();
        }

        if (DateManager.Instance != null)
        {
            DateManager.Instance.RefreshCurrentDayData();
        }

        CalendarPopup[] popups = FindObjectsByType<CalendarPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CalendarPopup popup in popups)
        {
            popup.RefreshToCurrentDate();
        }
    }
}
