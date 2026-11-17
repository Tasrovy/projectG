using System;
using MoonSharp.Interpreter;

public static class LuaStoryCondition
{
    public static void CheckSyntax(string code, string id)
    { new Script(CoreModules.None).LoadString("return function()\n" + Body(code) + "\nend", null, id); }
    static string Body(string code) => string.IsNullOrWhiteSpace(code) ? "return true" : code;
    public static bool Evaluate(string code, double n1, double n2, double n3, double money, double charm,
        string stageId, int day, Func<string, bool> seen, Func<string, bool> seenRun, Func<string, bool> flag)
    {
        // Fresh environment per evaluation: scripts cannot mutate another condition's snapshot.
        var lua = new Script(CoreModules.None);
        var stats = new Table(lua); stats["nature1"] = n1; stats["nature2"] = n2; stats["nature3"] = n3; stats["money"] = money; stats["charm"] = charm;
        lua.Globals["stats"] = stats;
        var progress = new Table(lua); progress["stageId"] = stageId; progress["stageDay"] = day; lua.Globals["progress"] = progress;
        lua.Globals["seen"] = seen; lua.Globals["seenThisRun"] = seenRun; lua.Globals["flag"] = flag;
        var fn = lua.LoadString(Body(code), null, "story-condition");
        var coroutine = lua.CreateCoroutine(fn).Coroutine;
        coroutine.AutoYieldCounter = 10000;
        var result = coroutine.Resume();
        if (coroutine.State != CoroutineState.Dead) throw new Exception("Lua条件超过10000指令预算");
        if (result.Type != DataType.Boolean) throw new Exception("Lua条件必须返回Boolean");
        return result.Boolean;
    }
}
