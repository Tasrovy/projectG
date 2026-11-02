using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogEntry
{
    public int id;
    public string comment;
    public float showTime = 2f;
}

public class DialogSO : ScriptableObject
{
    public List<DialogEntry> entries = new List<DialogEntry>();
}
