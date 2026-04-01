using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventSystemPersistent : MonoBehaviour
{
    public GameObject mainEventSystem;
    public GameObject dialogueEventSystem;

    public void SwitchToDialogue()
    {
        if (mainEventSystem != null)
        {
            mainEventSystem.SetActive(false);
        }
        if (dialogueEventSystem != null)
        {
            dialogueEventSystem.SetActive(true);
        }
    }

    public void SwitchToMain()
    {
        if (dialogueEventSystem != null)
        {
            dialogueEventSystem.SetActive(false);
        }
        if (mainEventSystem != null)
        {
            mainEventSystem.SetActive(true);
        }
    }
}
