using System;
using System.Collections.Generic;
using System.Linq;

public static class StorySelection
{
    // Random source injected so selection can be verified without changing Unity's global RNG.
    public static FlowStory Choose(IList<FlowStory> eligible, Func<string,bool> seen, Func<int,int> random)
    {
        var fresh = eligible.Where(s => !seen(s.storyId)).ToList();
        IList<FlowStory> candidates = fresh.Count > 0 ? fresh : eligible;
        return candidates.Count == 0 ? null : candidates[random(candidates.Count)];
    }
}
