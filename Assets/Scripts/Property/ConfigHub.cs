using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 配置集成器：将分散在多个组件上的 Inspector 变量集中到此处统一管理。
/// Awake 时自动将存储值推送给对应目标组件。
/// </summary>
public class ConfigHub : MonoBehaviour
{
    [Serializable]
    public class IntEntry
    {
        public Component target;
        public string fieldName;
        public int value;
    }

    [Serializable]
    public class FloatEntry
    {
        public Component target;
        public string fieldName;
        public float value;
    }

    [Serializable]
    public class BoolEntry
    {
        public Component target;
        public string fieldName;
        public bool value;
    }

    [Serializable]
    public class StringEntry
    {
        public Component target;
        public string fieldName;
        public string value;
    }

    public List<IntEntry>    intEntries    = new();
    public List<FloatEntry>  floatEntries  = new();
    public List<BoolEntry>   boolEntries   = new();
    public List<StringEntry> stringEntries = new();

    private void Awake()  => PushAll();

    // 编辑器下改值时立即同步到目标组件
    private void OnValidate() => PushAll();

    /// <summary>将所有条目的值推送到对应目标组件字段。</summary>
    public void PushAll()
    {
        foreach (var e in intEntries)    Push(e.target, e.fieldName, e.value);
        foreach (var e in floatEntries)  Push(e.target, e.fieldName, e.value);
        foreach (var e in boolEntries)   Push(e.target, e.fieldName, e.value);
        foreach (var e in stringEntries) Push(e.target, e.fieldName, e.value);
    }

    private static readonly BindingFlags Flags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private static void Push(Component target, string fieldName, object value)
    {
        if (target == null || string.IsNullOrEmpty(fieldName)) return;
        FieldInfo fi = target.GetType().GetField(fieldName, Flags);
        if (fi == null)
        {
            Debug.LogWarning($"[ConfigHub] {target.GetType().Name} 上未找到字段: {fieldName}");
            return;
        }
        try { fi.SetValue(target, Convert.ChangeType(value, fi.FieldType)); }
        catch (Exception e) { Debug.LogError($"[ConfigHub] 推送失败 {target.GetType().Name}.{fieldName}: {e.Message}"); }
    }
}
