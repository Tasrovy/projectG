using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NovelSkipController))]
public sealed class NovelSkipControllerEditor : Editor
{
    private int preview;
    [InitializeOnLoadMethod]
    private static void RegisterCleanup()
    {
        EditorApplication.playModeStateChanged -= ClearBeforePlay;
        EditorApplication.playModeStateChanged += ClearBeforePlay;
    }
    private static void ClearBeforePlay(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode) return;
        foreach (var controller in Object.FindObjectsByType<NovelSkipController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            controller.ClearPreview();
    }
    public override void OnInspectorGUI()
    {
        var controller = (NovelSkipController)target;
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
        {
            if (preview == 1) controller.PreviewConfirmation();
            if (preview == 2) controller.PreviewLoading();
        }
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("预览确认弹窗")) { preview = 1; controller.PreviewConfirmation(); }
            if (GUILayout.Button("预览加载画面")) { preview = 2; controller.PreviewLoading(); }
            if (GUILayout.Button("关闭预览")) { preview = 0; controller.ClearPreview(); }
        }
        EditorGUILayout.HelpBox("无需运行：在 Game 视图查看预览。Panel Position 控制弹窗整体位置；修改参数会刷新当前预览。预览物体不保存到场景。", MessageType.Info);
    }
    private void OnDisable()
    {
        if (target != null && !Application.isPlaying) ((NovelSkipController)target).ClearPreview();
    }
}
