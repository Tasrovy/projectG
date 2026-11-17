using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using ExcelDataReader;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class FlowConfigImporter : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (FlowConfigLocation.Root != "FlowConfigV2") throw new BuildFailedException("请切回正式流程配置后构建");
        Sync();
    }
    [MenuItem("Tools/ProjectG/流程配置/使用三日测试")]
    public static void UseTest() { EditorPrefs.SetBool("ProjectG.FlowTestProfile", true); Sync(); }
    [MenuItem("Tools/ProjectG/流程配置/使用正式配置")]
    public static void UseProduction() { EditorPrefs.SetBool("ProjectG.FlowTestProfile", false); Sync(); }
    [MenuItem("Tools/ProjectG/同步新流程配置")]
    public static void Sync()
    {
        var draft = ScriptableObject.CreateInstance<FlowConfig>();
        try
        {
            draft.stages = Read<FlowStage>("stage"); draft.checkpoints = Read<FlowCheckpoint>("checkpoint");
            draft.stories = Read<FlowStory>("story"); draft.tutorial = Read<FlowTutorial>("tutorial"); draft.Validate();
            if (FlowConfigLocation.Root == "FlowConfigTest") draft.days = Read<DayData>("day");
            foreach (var s in draft.stories.Where(s => s.enabled)) LuaStoryCondition.CheckSyntax(s.conditionLua, s.storyId);
            string path = "Assets/Resources/" + FlowConfigLocation.Root + "/FlowConfig.asset";
            var asset = AssetDatabase.LoadAssetAtPath<FlowConfig>(path);
            if (asset == null) { AssetDatabase.CreateAsset(draft, path); draft = null; }
            else { EditorUtility.CopySerialized(draft, asset); EditorUtility.SetDirty(asset); }
            AssetDatabase.SaveAssets(); Debug.Log("[Flow] 新流程配置同步成功");
        }
        finally { if (draft != null) UnityEngine.Object.DestroyImmediate(draft); }
    }
    static List<T> Read<T>(string file) where T : new()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using (var stream = File.Open(Path.Combine(Application.dataPath, "Resources/" + FlowConfigLocation.Root + "/" + file + ".xlsx"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = ExcelReaderFactory.CreateReader(stream))
        {
            var table = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true } }).Tables[0];
            var result = new List<T>();
            foreach (DataRow row in table.Rows)
            {
                if (string.IsNullOrWhiteSpace(row[0].ToString())) continue;
                var item = new T();
                foreach (var f in typeof(T).GetFields())
                {
                    if (!table.Columns.Contains(f.Name)) throw new Exception(file + "缺少表头:" + f.Name);
                    var value = row[f.Name];
                    if (value == DBNull.Value) continue;
                    try { f.SetValue(item, f.FieldType == typeof(string) ? value.ToString().Trim() : Convert.ChangeType(value, f.FieldType, System.Globalization.CultureInfo.InvariantCulture)); }
                    catch (Exception e) { throw new Exception(file + " 行" + (result.Count + 2) + " 列" + f.Name, e); }
                }
                result.Add(item);
            }
            return result;
        }
    }
}
