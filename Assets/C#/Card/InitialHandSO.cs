using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InitialHandEntry
{
    public int id;
    public int num = 1;
}

public class InitialHandSO : ScriptableObject
{
    public List<InitialHandEntry> entries = new List<InitialHandEntry>();
}
