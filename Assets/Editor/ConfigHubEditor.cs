using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ConfigHub))]
public class ConfigHubEditor : Editor
{
    private ConfigHub _hub;

    // 添加区域状态
    private GameObject  _addGameObject;
    private int         _addCompIdx;
    private string[]    _addCompOptions = Array.Empty<string>();
    private Component[] _addCompList    = Array.Empty<Component>();
    private Component   _addTarget;
    private int         _addTypeIdx;
    private int         _addFieldIdx;
    private string[]    _addFieldOptions = Array.Empty<string>();

    private static readonly string[] TypeLabels = { "int", "float", "bool", "string" };
    private static readonly Type[]   Types      = { typeof(int), typeof(float), typeof(bool), typeof(string) };

    private static readonly BindingFlags Flags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private void OnEnable() => _hub = (ConfigHub)target;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("配置集成器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("在下方添加条目后，修改值会实时同步到目标组件（EditMode 下通过 OnValidate，运行时通过 Awake）。", MessageType.Info);
        EditorGUILayout.Space(4);

        DrawSection("int 变量",    serializedObject.FindProperty("intEntries"));
        DrawSection("float 变量",  serializedObject.FindProperty("floatEntries"));
        DrawSection("bool 变量",   serializedObject.FindProperty("boolEntries"));
        DrawSection("string 变量", serializedObject.FindProperty("stringEntries"));

        EditorGUILayout.Space(8);
        DrawAddArea();

        EditorGUILayout.Space(4);
        if (GUILayout.Button("立即推送到所有组件"))
            _hub.PushAll();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSection(string label, SerializedProperty list)
    {
        if (list.arraySize == 0) return;

        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        int removeIdx = -1;
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty elem      = list.GetArrayElementAtIndex(i);
            SerializedProperty targetProp = elem.FindPropertyRelative("target");
            SerializedProperty fieldProp  = elem.FindPropertyRelative("fieldName");
            SerializedProperty valueProp  = elem.FindPropertyRelative("value");

            string compName = targetProp.objectReferenceValue != null
                ? targetProp.objectReferenceValue.GetType().Name
                : "（未设置）";
            string entryLabel = $"{compName}  .  {fieldProp.stringValue}";

            // 读取原始字段上的 Header / Tooltip attribute
            string attrTooltip = null;
            string attrHeader  = null;
            if (targetProp.objectReferenceValue is Component comp)
            {
                FieldInfo fi = comp.GetType().GetField(fieldProp.stringValue, Flags);
                if (fi != null)
                {
                    var ta = fi.GetCustomAttributes<TooltipAttribute>().FirstOrDefault();
                    if (ta != null) attrTooltip = ta.tooltip;
                    var ha = fi.GetCustomAttributes<HeaderAttribute>().FirstOrDefault();
                    if (ha != null) attrHeader = ha.header;
                }
            }

            if (attrHeader != null)
                EditorGUILayout.LabelField(attrHeader, EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(entryLabel, attrTooltip), GUILayout.MinWidth(180));
            EditorGUILayout.PropertyField(valueProp, GUIContent.none);
            if (GUILayout.Button("×", GUILayout.Width(22)))
                removeIdx = i;
            EditorGUILayout.EndHorizontal();
        }

        if (removeIdx >= 0)
            list.DeleteArrayElementAtIndex(removeIdx);

        EditorGUILayout.Space(4);
    }

    private void DrawAddArea()
    {
        EditorGUILayout.LabelField("添加条目", EditorStyles.boldLabel);

        // 第一行：拖入 GameObject
        GameObject newGO = (GameObject)EditorGUILayout.ObjectField(
            "目标物体", _addGameObject, typeof(GameObject), true);

        if (newGO != _addGameObject)
        {
            _addGameObject = newGO;
            _addCompIdx    = 0;
            _addFieldIdx   = 0;
            RefreshCompOptions();
            RefreshFieldOptions();
        }

        if (_addGameObject == null)
        {
            EditorGUILayout.HelpBox("请先拖入目标物体。", MessageType.None);
            return;
        }

        // 第二行：选择组件
        int newCompIdx = EditorGUILayout.Popup("目标组件", _addCompIdx, _addCompOptions);
        if (newCompIdx != _addCompIdx)
        {
            _addCompIdx  = newCompIdx;
            _addFieldIdx = 0;
            _addTarget   = _addCompList.Length > _addCompIdx ? _addCompList[_addCompIdx] : null;
            RefreshFieldOptions();
        }

        // 第三行：选择类型
        int newTypeIdx = EditorGUILayout.Popup("变量类型", _addTypeIdx, TypeLabels);
        if (newTypeIdx != _addTypeIdx)
        {
            _addTypeIdx  = newTypeIdx;
            _addFieldIdx = 0;
            RefreshFieldOptions();
        }

        if (_addTarget == null)
        {
            EditorGUILayout.HelpBox("该物体上没有可选组件。", MessageType.Warning);
            return;
        }

        if (_addFieldOptions.Length == 0)
        {
            EditorGUILayout.HelpBox($"该组件上没有可序列化的 {TypeLabels[_addTypeIdx]} 类型字段。", MessageType.Warning);
            return;
        }

        // 第四行：选择字段
        _addFieldIdx = EditorGUILayout.Popup("选择字段", _addFieldIdx, _addFieldOptions);

        if (GUILayout.Button("添加"))
        {
            AddEntry(_addTypeIdx, _addTarget, _addFieldOptions[_addFieldIdx]);
            serializedObject.Update();
        }
    }

    private void RefreshCompOptions()
    {
        if (_addGameObject == null)
        {
            _addCompOptions = Array.Empty<string>();
            _addCompList    = Array.Empty<Component>();
            _addTarget      = null;
            return;
        }
        _addCompList = _addGameObject.GetComponents<Component>();
        var names = new string[_addCompList.Length];
        for (int i = 0; i < _addCompList.Length; i++)
            names[i] = _addCompList[i] != null ? _addCompList[i].GetType().Name : "(null)";
        _addCompOptions = names;
        _addTarget      = _addCompList.Length > 0 ? _addCompList[0] : null;
    }

    private void RefreshFieldOptions()
    {
        if (_addTarget == null) { _addFieldOptions = Array.Empty<string>(); return; }
        _addFieldOptions = GetSerializableFields(_addTarget, Types[_addTypeIdx]);
    }

    private static string[] GetSerializableFields(Component comp, Type targetType)
    {
        var result = new List<string>();
        foreach (FieldInfo fi in comp.GetType().GetFields(Flags))
        {
            if (fi.FieldType != targetType) continue;
            bool isPublic   = fi.IsPublic;
            bool hasSF      = fi.GetCustomAttribute<SerializeField>() != null;
            bool hasHide    = fi.GetCustomAttribute<HideInInspector>() != null;
            bool hasNonSer  = fi.GetCustomAttribute<NonSerializedAttribute>() != null;
            if ((!isPublic && !hasSF) || hasHide || hasNonSer) continue;
            result.Add(fi.Name);
        }
        return result.ToArray();
    }

    private void AddEntry(int typeIdx, Component target, string fieldName)
    {
        Undo.RecordObject(_hub, "ConfigHub AddEntry");

        FieldInfo fi = target.GetType().GetField(fieldName, Flags);
        object current = fi?.GetValue(target);

        switch (typeIdx)
        {
            case 0: _hub.intEntries.Add(new ConfigHub.IntEntry
                        { target = target, fieldName = fieldName, value = current is int i ? i : 0 }); break;
            case 1: _hub.floatEntries.Add(new ConfigHub.FloatEntry
                        { target = target, fieldName = fieldName, value = current is float f ? f : 0f }); break;
            case 2: _hub.boolEntries.Add(new ConfigHub.BoolEntry
                        { target = target, fieldName = fieldName, value = current is bool b && b }); break;
            case 3: _hub.stringEntries.Add(new ConfigHub.StringEntry
                        { target = target, fieldName = fieldName, value = current as string ?? "" }); break;
        }
        EditorUtility.SetDirty(_hub);
    }
}
