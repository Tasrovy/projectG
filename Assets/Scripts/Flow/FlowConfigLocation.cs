public static class FlowConfigLocation
{
    public static string Root
    {
        get
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorPrefs.GetBool("ProjectG.FlowTestProfile", false)) return "FlowConfigTest";
#endif
            return "FlowConfigV2";
        }
    }
}
