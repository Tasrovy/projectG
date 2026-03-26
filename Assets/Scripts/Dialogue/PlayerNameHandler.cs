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

    public void ConfirmName()
    {
        string playerName = nameInputField.text;
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = defaultName;
        }

        variableStorage.SetValue("$my_name", playerName);

        // 以下添加其余逻辑
        gameObject.SetActive(false);
    }
}
