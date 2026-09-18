using System;
using System.Collections.Generic;

public static class BuffConsumeRule
{
    public const string None = "None";
    public const string NextEligibleShootingCardUsed =
        "NextEligibleShootingCardUsed";
    public const string FormalClashResolved = "FormalClashResolved";
    public const string SuccessfulPointCardUsed = "SuccessfulPointCardUsed";
}

// The canonical runtime state for one Character + one buffID.
public class BuffData
{
    public string buffID;
    public int stack;
    public int intensity;

    public BuffData(string id, int stacks, int currentIntensity)
    {
        buffID = id;
        stack = Math.Max(0, stacks);
        intensity = currentIntensity;
    }
}

[Serializable]
public class BuffDefinitionData
{
    public string buffID;
    public string displayName;
    public string buffCategory;
    public string effectType;
    public string targetStat;
    public int defaultIntensity;
    public int maxIntensity;
    public int maxStacks;
    public bool retainWhenZero;
    public bool showWhenZero;
    public string consumeRule;
    public string description;
}

[Serializable]
public class BuffDefinitionList
{
    public List<BuffDefinitionData> buffs;
}
