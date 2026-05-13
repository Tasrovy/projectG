using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器工具：检查 day 表中是否存在处于周六/周日、且 Dialog（dailyDialog/specialDialog/failedDialog）非空的行。
/// 使用方法：Unity 菜单 → Tools → 检查 Day 表周末 Dialog
/// </summary>
public static class DayExcelChecker
{
    [MenuItem("Tools/检查 Day 表周末 Dialog")]
    public static void CheckWeekendDialogs()
    {
        DayDataSO daySO = AssetDatabase.LoadAssetAtPath<DayDataSO>("Assets/Resources/day.asset");
        if (daySO == null)
        {
            Debug.LogError("[DayExcelChecker] 未找到 Assets/Resources/day.asset，请先在编辑器运行一次以同步 Excel。");
            return;
        }

        int issueCount = 0;
        var reportSB = new StringBuilder();

        foreach (DayData data in daySO.dayDatas)
        {
            if (string.IsNullOrEmpty(data.date)) continue;

            string[] parts = data.date.Split('_');
            if (parts.Length != 2
                || !int.TryParse(parts[0], out int month)
                || !int.TryParse(parts[1], out int day))
            {
                Debug.LogWarning($"[DayExcelChecker] day={data.day} 的 date 格式非法：{data.date}");
                continue;
            }

            DateTime date = new DateTime(2026, month, day);
            DayOfWeek dow = date.DayOfWeek;
            if (dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday) continue;

            string dowName = dow == DayOfWeek.Saturday ? "周六" : "周日";
            bool hasDailyDialog   = !string.IsNullOrEmpty(data.dailyDialog);
            bool hasSpecialDialog = !string.IsNullOrEmpty(data.specialDialog);
            bool hasFailedDialog  = !string.IsNullOrEmpty(data.failedDialog);

            if (!hasDailyDialog && !hasSpecialDialog && !hasFailedDialog) continue;

            issueCount++;
            var sb = new StringBuilder();
            sb.Append($"day={data.day} ({data.date} {dowName}) :");
            if (hasDailyDialog)   sb.Append($"  dailyDialog=\"{data.dailyDialog}\"");
            if (hasSpecialDialog) sb.Append($"  specialDialog=\"{data.specialDialog}\"");
            if (hasFailedDialog)  sb.Append($"  failedDialog=\"{data.failedDialog}\"");
            reportSB.AppendLine(sb.ToString());
        }

        string header = issueCount == 0
            ? "[DayExcelChecker] ✓ day 表检查通过，无周六/周日存在非空 Dialog 的行。"
            : $"[DayExcelChecker] 共发现 {issueCount} 行在周末配置了 Dialog，请查看下方清单：";

        // 输出到 Unity Console（便于复制）
        Debug.Log(header);
        if (reportSB.Length > 0)
            Debug.LogWarning(reportSB.ToString());
    }
}
