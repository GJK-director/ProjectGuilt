// Phase 3.4：Finalized Clash 的延迟提交计划。只保存规则数据，不包含任何表现层信息。
using System.Collections.Generic;

public enum BattleResolutionPlanState
{
    Pending,
    Activated,
    Completed
}

public enum BattleImpactState
{
    Pending,
    Committed,
    Skipped
}

public sealed class BattleImpact
{
    public int impactIndex;
    public CharacterData attacker;
    public CharacterData target;
    public BattleCardState sourceCardState;
    public int basePower;
    public int clashPoint;
    public string clashResult;
    public bool allowsDamage;
    public bool shouldTriggerHit;
    public BattleImpactState state;
    public int committedDamage;
    public bool didKill;

    public BattleImpact(
        int impactIndex,
        CharacterData attacker,
        CharacterData target,
        BattleCardState sourceCardState,
        int basePower,
        int clashPoint,
        string clashResult,
        bool allowsDamage,
        bool shouldTriggerHit
    )
    {
        this.impactIndex = impactIndex;
        this.attacker = attacker;
        this.target = target;
        this.sourceCardState = sourceCardState;
        this.basePower = basePower;
        this.clashPoint = clashPoint;
        this.clashResult = clashResult;
        this.allowsDamage = allowsDamage;
        this.shouldTriggerHit = shouldTriggerHit;
        state = BattleImpactState.Pending;
    }
}

public sealed class BattleResolutionPlan
{
    public BattleExecutionItem executionItem;
    public BattleActionSlot actionSlot;
    public BattleEnemyIntent enemyIntent;
    public BattleClashSession clashSession;

    public string resultType;
    public bool playerCardUsed;
    public bool enemyCardUsed;
    public bool playerCardParticipated;
    public BattleCardUseDisposition playerCardUseDisposition;
    public bool triggeredEventChain;

    public CharacterData attacker;
    public CharacterData target;
    public BattleCardState sourceCardState;

    // Defense 的一次性 Guard 只消费本次计算实际看到的层数。
    public int guardUpStackToConsume;
    public int guardDownStackToConsume;

    public readonly List<BattleImpact> impacts = new List<BattleImpact>();

    public BattleResolutionPlanState State { get; internal set; }
    public BattleResolveResult CompletedResult { get; internal set; }
    public bool IsActionCompleted { get; private set; }

    public BattleResolutionPlan(
        BattleExecutionItem executionItem,
        BattleActionSlot actionSlot,
        BattleEnemyIntent enemyIntent,
        BattleClashSession clashSession
    )
    {
        this.executionItem = executionItem;
        this.actionSlot = actionSlot;
        this.enemyIntent = enemyIntent;
        this.clashSession = clashSession;
        State = BattleResolutionPlanState.Pending;
    }

    public BattleImpact GetNextPendingImpact()
    {
        foreach (BattleImpact impact in impacts)
        {
            if (impact != null && impact.state == BattleImpactState.Pending)
            {
                return impact;
            }
        }

        return null;
    }

    public bool HasPendingImpact()
    {
        return GetNextPendingImpact() != null;
    }

    public void MarkActionCompleted()
    {
        IsActionCompleted = true;
    }
}
