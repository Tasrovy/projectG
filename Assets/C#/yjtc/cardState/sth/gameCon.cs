using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gameCon : MonoBehaviour
{
    public void gameBegin()
    {
        UISceneManager.Instance.SwitchToScene(SceneType.Talk);
        DialogueHandler.Instance.StartDialogue("main_start");
    }
}
