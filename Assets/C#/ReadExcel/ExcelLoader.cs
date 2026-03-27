using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Data;
using ExcelDataReader;

public class ExcelLoader : Singleton<ExcelLoader>
{
    public string resourcePath = "Resources/";

    public DataSet ReadExcel(string fileName)
    {
        fileName = resourcePath + fileName;
        if (!File.Exists(fileName))
        {
            Debug.LogError($"找不到Excel文件: {fileName}");
            return null;
        }

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        try
        {
            using (FileStream stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // 创建 Excel 读取器
                using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                {
                    // 将所有表数据转为 DataSet
                    return reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true // 设为 true 后，才能用 row["id"]
                        }
                    });
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ExcelLoader] 读取失败: {e.Message}");
            return null;
        }
    }

    public static string ReadText(DataSet dataSet,string id, int index)
    {
        DataRow row = dataSet.Tables[0].Rows[index];
        return row[id].ToString();
    }
}
