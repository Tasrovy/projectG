#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CreateUIDiagnosticObject
{
    [MenuItem("Tools/Create UI Diagnostic Object")]
    public static void Create()
    {
        GameObject go = new GameObject("UIInputDiagnostic");
        go.AddComponent<UIInputDiagnostic>();
        // 不自动设为 DontDestroyOnLoad，方便编辑器场景管理；用户可自行设置
        Selection.activeGameObject = go;
        Debug.Log("Created UIInputDiagnostic in scene. Attach it to a persistent object if needed.");
    }
}
#endif
