using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Lives independently of the Talk root, which is disabled during player actions.
public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }
    public static bool Enabled => true;
    public string State { get; private set; } = "Idle";
    public string CurrentStageId => stage?.stageId;
    public int StageDay { get; private set; } = 1;
    public string SelectedStoryId { get; private set; }
    public int DaysUntilCheckpoint => stage == null || config == null ? 0 : FlowConfig.IDs(stage.checkpointIds).Select(id=>config.checkpoints.First(c=>c.checkpointId==id).stageDay).Where(d=>d>=StageDay).Select(d=>d-StageDay).DefaultIfEmpty(0).Min();
    public bool IsTutorial => State == "TutorialPrompt" || State == "TutorialAction";
    FlowConfig config; FlowStage stage; int tutorialIndex, tutorialCount;
    Action dialogueDone; bool playingOwned, advancingTutorial;
    readonly HashSet<string> seenRun = new HashSet<string>();
    readonly HashSet<string> flags = new HashSet<string>();
    readonly HashSet<string> resolved = new HashSet<string>();
    List<FlowTutorial> steps;
    readonly List<KeyValuePair<string,string>> transactionEvents = new List<KeyValuePair<string,string>>();
    public static void BufferOperation(string evt, int cardId) { Instance?.transactionEvents.Add(new KeyValuePair<string,string>(evt,cardId.ToString())); }
    public static void EndTransaction(bool success)
    {
        if (Instance == null) return;
        var events = Instance.transactionEvents.ToArray(); Instance.transactionEvents.Clear();
        if (success) foreach (var e in events) Report(e.Key,e.Value);
    }
    public static bool AllowsCard(Card card)
    {
        if (card == null) return false;
        if (Allows("PlayCard")) return true;
        // Craft operations are initiated by playing a function card; target selection remains available.
        string plan = (card.made ?? "") + ";" + (card.broken ?? "") + ";" + (card.added ?? "") + ";" + (card.trigger ?? "");
        int number;
        return (Allows("Combine") && ((int.TryParse(card.made,out number) && number > 0) || plan.Contains("beMade")))
            || (Allows("Dismantle") && ((int.TryParse(card.broken,out number) && number > 0) || plan.Contains("beBroken")))
            || (Allows("Attach") && ((int.TryParse(card.added,out number) && number > 0) || plan.Contains("beAdded")));
    }
    [Serializable] class Snapshot { public int n1,n2,n3,money,charm; public List<int> hand = new List<int>(); public List<int> deck = new List<int>(); }
    Snapshot initial;
    DayDataSO originalCalendar;

    public static GameFlowController Ensure()
    {
        if (Instance == null) { var go = new GameObject("GameFlowController"); Instance = go.AddComponent<GameFlowController>(); DontDestroyOnLoad(go); }
        return Instance;
    }
    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }
    public void BeginRun()
    {
        if (State != "Idle" && State != "Completed" && State != "Failed" && State != "ConfigError") return;
        try
        {
#if UNITY_EDITOR
            // Importer is in the editor assembly, so invoke without a player dependency.
            var importer = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("FlowConfigImporter")).FirstOrDefault(t => t != null);
            importer?.GetMethod("Sync").Invoke(null, null);
#endif
            config = Resources.Load<FlowConfig>(FlowConfigLocation.Root + "/FlowConfig");
            if (config == null) throw new Exception("请先执行Tools/ProjectG/同步新流程配置");
            config.Validate();
            if (originalCalendar == null) originalCalendar = DayManager.Instance.daySO;
            if (config.days.Count > 0)
            {
                var calendar = ScriptableObject.CreateInstance<DayDataSO>(); calendar.dayDatas = config.days;
                DayManager.Instance.daySO = calendar;
            }
            var handler = DialogueHandler.Instance;
            foreach (var node in config.stories.Where(s => s.enabled).Select(s => s.entryNode)
                .Concat(config.tutorial.Select(t => t.entryNode)).Concat(config.checkpoints.Select(c => c.failureNode))
                .Concat(config.stages.Select(s => s.successNode)))
                if (!string.IsNullOrWhiteSpace(node) && !handler.HasFlowNode(node)) throw new Exception("未配置或缺失Yarn入口: " + node);
            if (DayManager.Instance.daySO == null || DayManager.Instance.daySO.dayDatas.Count == 0) throw new Exception("day表为空");
            stage = config.stages[0]; StageDay = 1; seenRun.Clear(); flags.Clear(); resolved.Clear(); SelectedStoryId = null;
            initial = Capture(); steps = config.tutorial.OrderBy(t => t.order).ToList(); tutorialIndex = 0;
            DayManager.Instance.ResetToStart();
            NextTutorial();
        }
        catch (Exception e) { FailConfig(e.GetBaseException().Message); }
    }
    void NextTutorial()
    {
        if (tutorialIndex >= steps.Count)
        {
            RestoreInitial(); DayManager.Instance.ResetToStart(); DayManager.Instance.NextDay(); StartMorning(); return;
        }
        var t = steps[tutorialIndex]; tutorialCount = 0; State = "TutorialPrompt";
        try { PrepareTutorial(t); } catch(Exception e) { FailConfig(e.Message); return; }
        Play(t.entryNode, () =>
        {
            if (t.completionEvent == "DialogueCompleted") { CountTutorial("DialogueCompleted", ""); return; }
            State = "TutorialAction";
            UISceneManager.Instance.SwitchToScene(t.uiMode == "Shop" ? SceneType.Shop : t.uiMode == "Craft" ? SceneType.CardFight : SceneType.Talk);
            if (t.uiMode == "Craft") DUEL.Instance.Begin();
        });
    }
    void PrepareTutorial(FlowTutorial t)
    {
        var cm=CardManager.Instance;
        if(t.allowedAction=="Buy")
        {
            int maximum=cm.cardDatas.Select(c=>Math.Max(0,c.sale)).DefaultIfEmpty(0).Max();
            DataManager.Instance.MoneyNum=Math.Max(DataManager.Instance.MoneyNum,maximum*t.requiredCount);
            return;
        }
        if(t.uiMode!="Craft" && t.allowedAction!="Sell")return;
        // Tutorial resources are isolated by the initial snapshot and restored afterwards.
        Func<CardData,bool> target=d=>string.IsNullOrEmpty(t.targetId)||d.id.ToString()==t.targetId;
        var gift=cm.cardDatas.FirstOrDefault(d=>CardIdUtility.GetCardType(d.id)==1 && target(d));
        if(gift==null)throw new Exception("教程缺少可用目标礼物卡:"+t.stepId);
        var operation=cm.cardDatas.Where(d=>CardIdUtility.GetCardType(d.id)==2)
            .OrderBy(d=>string.IsNullOrWhiteSpace(d.trigger)?0:1)
            .ThenBy(d=> { int n; return (int.TryParse(d.made,out n)?Math.Abs(n):0)+(int.TryParse(d.broken,out n)?Math.Abs(n):0)+(int.TryParse(d.added,out n)?Math.Abs(n):0); })
            .FirstOrDefault(d=>
        {
            int count;
            if(t.allowedAction=="Combine")return int.TryParse(d.made,out count)&&count>0;
            if(t.allowedAction=="Dismantle")return int.TryParse(d.broken,out count)&&count>0;
            if(t.allowedAction=="Attach")return int.TryParse(d.added,out count)&&count>0;
            return false;
        });
        if(new[]{"Combine","Dismantle","Attach"}.Contains(t.allowedAction)&&operation==null)throw new Exception("教程缺少操作功能卡:"+t.stepId);
        int needed=2;
        if(operation!=null){int.TryParse(t.allowedAction=="Dismantle"?operation.broken:operation.made,out needed);needed=Math.Max(2,needed+1);}
        for(int i=0;i<needed*t.requiredCount;i++){var c=new Card();c.InitCard(gift);cm.AddCardInHand(c);}
        if(operation!=null)for(int i=0;i<t.requiredCount;i++){var c=new Card();c.InitCard(operation);cm.AddCardInHand(c);}
    }
    [Yarn.Unity.YarnCommand("flow_flag")]
    public static void SetStoryFlag(string key, bool value) { Ensure().SetFlag(key,value); }
    public static bool Allows(string action)
    {
        var f = Instance;
        if (f == null) return true;
        if (f.State == "ConfigError" || f.State == "Completed" || f.State == "Failed") return false;
        if (!f.IsTutorial) return f.State == "PlayerAction";
        if (f.State != "TutorialAction") return false;
        string allowed = f.steps[f.tutorialIndex].allowedAction;
        return allowed == "Free" || allowed == action;
    }
    public static bool AllowsScene(SceneType scene)
    {
        var f=Instance;
        if(f==null || f.State=="Idle" || f.State=="PlayerAction" || f.State=="Completed" || f.State=="Failed")return true;
        if(f.State=="ConfigError")return scene==SceneType.Begin;
        if(f.State=="TutorialAction")
        {
            var mode=f.steps[f.tutorialIndex].uiMode;
            return scene==(mode=="Craft"?SceneType.CardFight:mode=="Shop"?SceneType.Shop:SceneType.Talk);
        }
        return scene==SceneType.Talk;
    }
    public static void Report(string eventName, string targetId)
    { Instance?.CountTutorial(eventName, targetId); }
    void CountTutorial(string evt, string id)
    {
        if (!IsTutorial || advancingTutorial) return;
        var t = steps[tutorialIndex];
        if (evt != t.completionEvent || (!string.IsNullOrEmpty(t.targetId) && t.targetId != id)) return;
        if (++tutorialCount < t.requiredCount) return;
        advancingTutorial = true; StartCoroutine(AdvanceTutorial());
    }
    IEnumerator AdvanceTutorial()
    {
        // Let the transaction finish updating its UI before changing roots.
        yield return null; tutorialIndex++; advancingTutorial = false; NextTutorial();
    }
    public void FinishDay()
    {
        if (State != "PlayerAction" || (CardEffect.Instance != null && CardEffect.Instance.IsWaitingForAsync)) return;
        State = "Checking";
        try
        {
            var c = FlowConfig.IDs(stage.checkpointIds).Select(id => config.checkpoints.First(x => x.checkpointId == id)).FirstOrDefault(x => x.stageDay == StageDay);
            if (c == null) { AdvanceDay(); return; }
            if (resolved.Contains(c.checkpointId)) throw new Exception("检查点重复结算:" + c.checkpointId);
            var dm = DataManager.Instance;
            // Snapshot all numeric inputs once before evaluating the entire candidate set.
            double n1=dm.nature1,n2=dm.nature2,n3=dm.nature3,m=dm.MoneyNum,ch=dm.GetCharm();
            var eligible = FlowConfig.IDs(c.storyIds).Select(id => config.stories.First(s => s.storyId == id)).Where(s => s.enabled)
                .Where(s => LuaStoryCondition.Evaluate(s.conditionLua,n1,n2,n3,m,ch,stage.stageId,StageDay,Seen,seenRun.Contains,flags.Contains)).ToList();
            if (eligible.Count == 0 && c.checkpointType == "Major")
            { Play(c.failureNode, () => EndRun(false)); return; }
            var selected = StorySelection.Choose(eligible, Seen, count => UnityEngine.Random.Range(0,count))
                ?? config.stories.First(s => s.storyId == c.fallbackStoryId);
            SelectedStoryId = selected.storyId; State = "Checkpoint";
            Play(selected.entryNode, () =>
            {
                MarkSeen(selected.storyId); resolved.Add(c.checkpointId);
                if (c.checkpointType == "Minor") AdvanceDay();
                else if (!string.IsNullOrWhiteSpace(stage.nextStageId)) { stage = config.stages.First(s => s.stageId == stage.nextStageId); StageDay = 0; AdvanceDay(); }
                else Play(stage.successNode, () => EndRun(true));
            });
        }
        catch (Exception e) { FailConfig(e.Message); }
    }
    void AdvanceDay()
    {
        if (DayManager.Instance.IsCurrentDayFinalDay) { FailConfig("day.xlsx日期不足，尚未完成阶段，不会按失败结算"); return; }
        StageDay++; DayManager.Instance.NextDay(); StartMorning();
    }
    void StartMorning()
    {
        State = "Morning";
        var day = DayManager.Instance.CurrentDayData;
        if (day == null) { FailConfig("没有当天day配置"); return; }
        Play(day.dailyDialog, () => { State = "PlayerAction"; UISceneManager.Instance.SwitchToScene(SceneType.Select); });
    }
    void Play(string node, Action done)
    {
        if (string.IsNullOrWhiteSpace(node)) { done(); return; }
        if (!DialogueHandler.Instance.HasFlowNode(node)) { FailConfig("缺少Yarn节点:" + node); return; }
        dialogueDone = done; playingOwned = true;
        DialogueHandler.Instance.StartFlowDialogue(node);
    }
    public void DialogueCompleted(bool requestedEnd, SceneType returnScene)
    {
        if (playingOwned)
        {
            playingOwned = false; var done = dialogueDone; dialogueDone = null;
            try { done?.Invoke(); } catch (Exception e) { FailConfig(e.Message); }
        }
        else if (State == "PlayerAction")
        { if (requestedEnd) FinishDay(); else UISceneManager.Instance.SwitchToScene(returnScene); }
    }
    public bool Seen(string id) => PlayerPrefs.GetInt("Flow.Seen." + id,0) == 1;
    void MarkSeen(string id) { seenRun.Add(id); PlayerPrefs.SetInt("Flow.Seen."+id,1); PlayerPrefs.Save(); }
    public void SetFlag(string key, bool value) { if (value) flags.Add(key); else flags.Remove(key); }
    void EndRun(bool success)
    {
        State = success ? "Completed" : "Failed";
        PlayerPrefs.SetString("Flow.LastResult",State); PlayerPrefs.Save();
        RestoreInitial(); DayManager.Instance.ResetToStart();
        if (originalCalendar != null) DayManager.Instance.daySO = originalCalendar;
        UISceneManager.Instance.SwitchToScene(SceneType.Begin);
        Debug.Log("[Flow] 本轮结束:" + State);
    }
    void FailConfig(string message) { State = "ConfigError"; playingOwned = false; dialogueDone = null; Debug.LogError("[Flow] 配置错误，流程暂停（不判负）: " + message); }
    Snapshot Capture()
    {
        var d=DataManager.Instance; var cards=CardManager.Instance;
        return new Snapshot { n1=d.nature1,n2=d.nature2,n3=d.nature3,money=d.MoneyNum,charm=d.extraCharm,hand=cards.cardInHand.Select(c=>c.id).ToList(),deck=cards.cardSet.Select(c=>c.id).ToList() };
    }
    void RestoreInitial()
    {
        if (initial == null) return;
        var d=DataManager.Instance; d.nature1=initial.n1;d.nature2=initial.n2;d.nature3=initial.n3;d.MoneyNum=initial.money;d.extraCharm=initial.charm;
        d.SetNature1Effect(0);d.SetNature2Effect(0);d.SetNature3Effect(0);
        var cm=CardManager.Instance;cm.ClearAllCards();cm.ResetPity();
        foreach(var id in initial.hand){var c=new Card();c.InitCard(cm.GetCardDataById(id));cm.AddCardInHand(c);}
        foreach(var id in initial.deck){var c=new Card();c.InitCard(cm.GetCardDataById(id));cm.AddCardToSet(c);}
        cm.NotifyDeckOrHandChanged();
    }
}
