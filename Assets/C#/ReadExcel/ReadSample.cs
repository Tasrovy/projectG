using UnityEngine;
using UnityEditor;
using System.IO;
using System.Data;
using ExcelDataReader;

public class ReadSample
{
    // 在Unity顶部菜单栏创建一个按钮
    [MenuItem("Tools/读取测试 Excel")]
    public static void ReadExcel()
    {
        // 假设你的Excel文件放在项目的根目录（Assets同级）或者任意位置
        // 这里以 Assets 同级目录下的 "Test.xlsx" 为例
        string filePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Test.xlsx");

        if (!File.Exists(filePath))
        {
            Debug.LogError($"找不到Excel文件: {filePath}");
            return;
        }

        // 【重要】在比较新的.NET中读取Excel需要注册编码，否则可能会报错
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // 打开文件流
        using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            // 创建 Excel 读取器 (支持 .xls 和 .xlsx)
            using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
            {
                // 将数据转化为 DataSet（需要 ExcelDataReader.DataSet.dll）
                // 这一步会自动把 Excel 里的所有页签（Sheet）转换成数据表
                DataSet result = reader.AsDataSet();

                // 获取第一个页签 (Sheet1)
                DataTable table = result.Tables[0];

                // 打印表名、行数和列数
                Debug.Log($"读取页签: {table.TableName}, 总行数: {table.Rows.Count}, 总列数: {table.Columns.Count}");

                // 遍历所有的行和列
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    // 假设第一行是表头（字段名），我们从第二行开始读也可以，这里全读出来
                    string rowData = $"第 {i} 行数据: ";
                    for (int j = 0; j < table.Columns.Count; j++)
                    {
                        // 读取单元格数据并转为字符串
                        string cellValue = table.Rows[i][j].ToString();
                        rowData += $"[{cellValue}] ";
                    }
                    Debug.Log(rowData);
                }
            }
        }
    }
}