using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Data;
using ExcelDataReader;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ExcelLoader : Singleton<ExcelLoader>
{
    /// <summary>
    /// 读取数据：编辑器下同步Excel与SO，发布后直接读SO
    /// </summary>
    /// <param name="excelPath">相对于项目根目录的路径，如 "Cards.xlsx" 或 "Assets/Excel/Buffs.xlsx"</param>
    public CardDatabaseSO ReadExcel(string excelPath)
    {
        // 提取文件名（不含扩展名）作为 Resource 里的名称
        string fileNameNoExt = Path.GetFileNameWithoutExtension(excelPath);

#if UNITY_EDITOR
        // --- 编辑器逻辑：同步 Excel 到 SO ---
        Debug.Log($"<color=green>[ExcelLoader]</color> 正在同步: {excelPath} -> Resources/{fileNameNoExt}.asset");
        return SyncExcelToSO(excelPath, fileNameNoExt);
#else
        // --- 运行逻辑：直接加载 SO ---
        return Resources.Load<CardDatabaseSO>(fileNameNoExt);
#endif
    }

#if UNITY_EDITOR
    private CardDatabaseSO SyncExcelToSO(string excelPath, string soName)
    {
        // 确保文件存在（支持相对路径和绝对路径）
        string fullPath = Path.GetFullPath(excelPath);
        if (!File.Exists(fullPath))
        {
            Debug.LogError("找不到Excel文件: " + fullPath);
            return null;
        }

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        CardDatabaseSO db = ScriptableObject.CreateInstance<CardDatabaseSO>();
        db.name = soName;
        using (var stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                });

                DataTable table = result.Tables[0];
                foreach (DataRow row in table.Rows)
                {
                    if (row["id"] == DBNull.Value || string.IsNullOrEmpty(row["id"].ToString())) continue;

                    CardData data = new CardData();
                    // 自动匹配字段名
                    foreach (var field in typeof(CardData).GetFields())
                    {
                        if (table.Columns.Contains(field.Name))
                        {
                            object value = row[field.Name];
                            if (value != DBNull.Value && !string.IsNullOrWhiteSpace(value.ToString()))
                            {
                                try 
                                {
                                    // 获取 Excel 原始字符串
                                    string rawValue = value.ToString().Trim();

                                    // 核心修复：处理数值转换
                                    if (field.FieldType == typeof(int))
                                    {
                                        // 先转成 double 再转 int，防止 Excel 里的 "1.0" 导致 int.Parse 报错
                                        float f = float.Parse(rawValue);
                                        field.SetValue(data, (int)f);
                                    }
                                    else if (field.FieldType == typeof(float))
                                    {
                                        field.SetValue(data, float.Parse(rawValue));
                                    }
                                    else
                                    {
                                        // 其他类型（string 等）使用通用转换
                                        field.SetValue(data, Convert.ChangeType(value, field.FieldType));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // 这里的报错能帮你精准定位是哪一行哪一列错了
                                    Debug.LogError($"[ExcelLoader] 转换失败！列名: {field.Name}, 单元格内容: '{value}', 错误原因: {ex.Message}");
                                }
                            }
                        }
                    }

                    db.allCards.Add(data);
                }
            }
        }

        // 存储路径：Assets/Resources/soName.asset
        string resDir = Application.dataPath + "/Resources";
        if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);

        string assetPath = $"Assets/Resources/{soName}.asset";
        CardDatabaseSO existingAsset = AssetDatabase.LoadAssetAtPath<CardDatabaseSO>(assetPath);

        if (existingAsset == null)
        {
            AssetDatabase.CreateAsset(db, assetPath);
        }
        else
        {
            EditorUtility.CopySerialized(db, existingAsset);
            db = existingAsset;
        }

        AssetDatabase.SaveAssets();
        return db;
    }
#endif

    /// <summary>
    /// 读取天数数据：编辑器下同步，发布后读SO
    /// </summary>
    public DayDataSO ReadDayExcel(string excelPath)
    {
        string fileNameNoExt = Path.GetFileNameWithoutExtension(excelPath);

#if UNITY_EDITOR
        return SyncDayExcelToSO(excelPath, fileNameNoExt);
#else
        return Resources.Load<DayDataSO>(fileNameNoExt);
#endif
    }

#if UNITY_EDITOR
    private DayDataSO SyncDayExcelToSO(string excelPath, string soName)
    {
        string fullPath = Path.GetFullPath(excelPath);
        if (!File.Exists(fullPath))
        {
            Debug.LogError("找不到DayData Excel文件: " + fullPath);
            return null;
        }

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // 创建临时实例
        DayDataSO db = ScriptableObject.CreateInstance<DayDataSO>();

        using (var stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                });

                DataTable table = result.Tables[0];
                foreach (DataRow row in table.Rows)
                {
                    // 以 dayNumber 作为主键判断，如果为空则跳过
                    if (row["day"] == DBNull.Value || string.IsNullOrEmpty(row["day"].ToString())) continue;

                    DayData data = new DayData();
                    // 自动匹配 DayData 类中的字段名与 Excel 列名
                    foreach (var field in typeof(DayData).GetFields())
                    {
                        if (table.Columns.Contains(field.Name))
                        {
                            object value = row[field.Name];
                            if (value != DBNull.Value)
                            {
                                try
                                {
                                    field.SetValue(data, Convert.ChangeType(value, field.FieldType));
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"字段 {field.Name} 转换失败: {e.Message}");
                                }
                            }
                        }
                    }

                    db.dayDatas.Add(data);
                }
            }
        }

        // 存储到 Resources 目录
        string resDir = Application.dataPath + "/Resources";
        if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);

        string assetPath = $"Assets/Resources/{soName}.asset";
        DayDataSO existingAsset = AssetDatabase.LoadAssetAtPath<DayDataSO>(assetPath);

        if (existingAsset == null)
        {
            AssetDatabase.CreateAsset(db, assetPath);
        }
        else
        {
            EditorUtility.CopySerialized(db, existingAsset);
            db = existingAsset;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"<color=cyan>[ExcelLoader]</color> DayData同步成功: {assetPath}");
        return db;
    }
#endif

    // 依然支持你习惯的 ReadText 逻辑
    public static string ReadText(CardDatabaseSO db, string fieldName, int index)
    {
        if (db == null || index >= db.allCards.Count) return "";
        FieldInfo field = typeof(CardData).GetField(fieldName);
        return field?.GetValue(db.allCards[index])?.ToString() ?? "";
    }
}