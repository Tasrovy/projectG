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
        if (nameInputField != null)
        {
            nameInputField.text = defaultName;
        }
    }

    public void ConfirmName()
    {
        string playerName = nameInputField.text;
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = defaultName;
        }

        if (variableStorage != null)
        {
            variableStorage.SetValue("$MY_NAME", playerName); 
        }

        gameObject.SetActive(false);
    }
}
