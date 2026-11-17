using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable] public class FlowStage { public string stageId, checkpointIds, nextStageId, successNode; }
[Serializable] public class FlowCheckpoint { public string checkpointId, checkpointType, storyIds, fallbackStoryId, failureNode; public int stageDay; }
[Serializable] public class FlowStory { public string storyId, displayName, entryNode, conditionLua, notes; public bool enabled; }
[Serializable] public class FlowTutorial { public string stepId, entryNode, uiMode, allowedAction, completionEvent, targetId, notes; public int order, requiredCount; }
public class FlowConfig : ScriptableObject
{
    public List<FlowStage> stages = new List<FlowStage>();
    public List<FlowCheckpoint> checkpoints = new List<FlowCheckpoint>();
    public List<FlowStory> stories = new List<FlowStory>();
    public List<FlowTutorial> tutorial = new List<FlowTutorial>();
    public List<DayData> days = new List<DayData>(); // Optional isolated test calendar; production keeps day.xlsx.
    public static string[] IDs(string value) => (value ?? "").Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
    public void Validate()
    {
        var ss = stages.ToDictionary(s => s.stageId); var cc = checkpoints.ToDictionary(c => c.checkpointId); var tt = stories.ToDictionary(s => s.storyId);
        if (stages.Count == 0) throw new Exception("stage表为空");
        var used = new HashSet<string>();
        foreach (var s in stages)
        {
            var ids = IDs(s.checkpointIds); if (ids.Length == 0) throw new Exception(s.stageId + "无检查点");
            int day = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                if (!used.Add(ids[i])) throw new Exception("重复归属检查点:" + ids[i]);
                var c = cc[ids[i]];
                if (c.stageDay <= day || c.checkpointType != (i == ids.Length - 1 ? "Major" : "Minor")) throw new Exception("检查点日期或类型错误:" + ids[i]);
                day = c.stageDay;
            }
            if (!string.IsNullOrEmpty(s.nextStageId) && !ss.ContainsKey(s.nextStageId)) throw new Exception("未知下一阶段:" + s.nextStageId);
            var path = new HashSet<string>(); var cursor = s;
            while (cursor != null) { if (!path.Add(cursor.stageId)) throw new Exception("阶段形成环"); cursor = string.IsNullOrEmpty(cursor.nextStageId) ? null : ss[cursor.nextStageId]; }
        }
        if (used.Count != cc.Count) throw new Exception("存在未归属检查点");
        foreach (var c in checkpoints)
        {
            var ids = IDs(c.storyIds); if (ids.Length == 0 || ids.Distinct().Count() != ids.Length) throw new Exception("候选列表为空/重复:" + c.checkpointId);
            foreach (var id in ids) { var item = tt[id]; }
            if (c.checkpointType == "Minor") { var f = tt[c.fallbackStoryId]; if (!f.enabled || (f.conditionLua ?? "").Trim() != "return true") throw new Exception("兜底必须启用且return true"); }
            else if (string.IsNullOrWhiteSpace(c.failureNode)) throw new Exception("缺少失败入口:" + c.checkpointId);
        }
        tutorial.ToDictionary(t => t.stepId); tutorial.ToDictionary(t => t.order);
        foreach (var t in tutorial)
            if (t.requiredCount < 1 || !new[]{"Dialogue","Craft","Shop"}.Contains(t.uiMode) || !new[]{"None","PlayCard","Combine","Dismantle","Attach","Buy","Sell","Free"}.Contains(t.allowedAction) || !new[]{"DialogueCompleted","CardPlayed","CardCombined","CardDismantled","CardAttached","ItemBought","CardSold"}.Contains(t.completionEvent)) throw new Exception("教程字段错误:" + t.stepId);
    }
}
