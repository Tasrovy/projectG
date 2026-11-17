using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class FlowDiagnostics
{
    [MenuItem("Tools/ProjectG/验证流程规则")]
    public static string Run()
    {
        int checks=0;
        Action<bool> assert = value => { if(!value) throw new Exception("Flow test failed at " + checks); checks++; };
        Func<string,bool> no = _=>false;
        Func<string,bool> yes = _=>true;
        Func<string,bool> eval = code => LuaStoryCondition.Evaluate(code,30,20,10,50,100,"stage_01",3,no,no,no);
        assert(eval("return stats.nature1 >= 30")); assert(!eval("return stats.nature1 > 30"));
        assert(eval("return stats.nature2 == 20 and stats.charm == 100 and progress.stageDay == 3"));
        assert(!eval("return seen('x') or seenThisRun('x') or flag('x')"));
        assert(LuaStoryCondition.Evaluate("return seen('x') and not seenThisRun('x')",0,0,0,0,0,"s",1,yes,no,no));
        foreach(var code in new[]{"return 0","while true do end","return stats.unknown >= 1"})
        { bool failed=false;try{eval(code);}catch{failed=true;}assert(failed); }
        var a=new FlowStory{storyId="a"}; var b=new FlowStory{storyId="b"};
        var list=new List<FlowStory>{a,b};
        assert(StorySelection.Choose(list,id=>id=="a",n=>0)==b);
        assert(StorySelection.Choose(list,yes,n=>n-1)==b);
        assert(StorySelection.Choose(new List<FlowStory>{a},yes,n=>0)==a);
        assert(StorySelection.Choose(new List<FlowStory>(),no,n=>0)==null);
        var config=Resources.Load<FlowConfig>("FlowConfigV2/FlowConfig");config.Validate();
        foreach(var s in config.stages)
        {
            var cps=FlowConfig.IDs(s.checkpointIds).Select(id=>config.checkpoints.First(c=>c.checkpointId==id)).ToArray();
            assert(cps.Last().checkpointType=="Major");
            if(cps.Length>2)assert(!FlowConfig.IDs(cps[0].storyIds).Intersect(FlowConfig.IDs(cps[1].storyIds)).Any());
        }
        string message="Flow diagnostics passed: " + checks;Debug.Log(message);return message;
    }
}
