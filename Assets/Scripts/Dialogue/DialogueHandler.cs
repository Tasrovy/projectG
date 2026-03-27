using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public class DialogueHandler : MonoBehaviour
{
    public static DialogueHandler Instance { get; private set; }
    [SerializeField] private DialogueRunner dialogueRunner;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartDialogue(string yarnProgram)
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.StartDialogue(yarnProgram);
        }
    }
}
