// 延迟提交计划：保存Clash或无响应攻击的规则结果，不包含任何表现层信息。
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

public enum BattleResolutionPlanKind
{
    RespondedClash,
    UnrespondedEnemyAttack,
    FreeActionAttack
}

// A modifier belongs to one incoming Impact, never to the target's persistent buffs.
public sealed class BattleScopedDamageModifier
{
    public readonly BattleImpact impact;
    public readonly BattleCardState sourceCard;
    public readonly BattleClashSession sourceInteraction;
    public readonly int multiplierPercent;
    public bool Applied { get; private set; }

    public BattleScopedDamageModifier(
        BattleImpact impact, BattleCardState sourceCard,
        BattleClashSession sourceInteraction, int multiplierPercent)
    {
        this.impact = impact;
        this.sourceCard = sourceCard;
        this.sourceInteraction = sourceInteraction;
        this.multiplierPercent = System.Math.Max(0, multiplierPercent);
    }

    public bool TryApply(BattleEventContext context)
    {
        if (Applied || context == null || context.timing != BattleTiming.DamageModifier ||
            !object.ReferenceEquals(context.impact, impact) || impact == null ||
            impact.state != BattleImpactState.Pending || !impact.allowsDamage)
        {
            return false;
        }
        Applied = true;
        context.damage = (int)System.Math.Min(int.MaxValue,
            (long)System.Math.Max(0, context.damage) * multiplierPercent / 100);
        return true;
    }
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
    public bool didHit;
    public int actualDamage;
    public int committedDamage;
    public bool didKill;
    public bool usesPrecalculatedDamage;
    public int precalculatedDamage;
    public int damageMultiplierPercent = 100;
    public int hpDisplayStageCount = 1;
    public BattleScopedDamageModifier scopedDamageModifier;
    public BattleRuntimeInteraction runtimeInteraction;

    public BattleImpact(
        int impactIndex,
        CharacterData attacker,
        CharacterData target,
        BattleCardState sourceCardState,
        int basePower,
        int clashPoint,
        string clashResult,
        bool allowsDamage,
        bool shouldTriggerHit,
        BattleRuntimeInteraction runtimeInteraction = null
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
        this.runtimeInteraction = runtimeInteraction;
        didHit = false;
        actualDamage = 0;
        committedDamage = 0;
        didKill = false;
        hpDisplayStageCount = sourceCardState != null &&
            sourceCardState.cardData != null
            ? System.Math.Max(1, sourceCardState.cardData.hpDisplayStageCount)
            : 1;
        state = BattleImpactState.Pending;
    }

    public void SetPrecalculatedDamage(int damage)
    {
        usesPrecalculatedDamage = true;
        precalculatedDamage = damage;
    }
}

public sealed class BattleResolutionPlan
{
    public BattleResolutionPlanKind planKind;
    public BattleExecutionItem executionItem;
    public BattleActionSlot actionSlot;
    public BattleEnemyIntent enemyIntent;
    public BattleClashSession clashSession;
    public BattleRuntimeInteraction runtimeInteraction;

    public string resultType;
    public bool playerCardUsed;
    public bool enemyCardUsed;
    public bool playerCardParticipated;
    public BattleCardUseDisposition playerCardUseDisposition;
    public bool triggeredEventChain;

    public CharacterData attacker;
    public CharacterData target;
    public BattleCardState sourceCardState;
    public int unrespondedEnemyPoint;
    public BattleClashPointSnapshot unrespondedPointSnapshot;
    public BattleClashResourceSnapshot unrespondedResourceSnapshot;
    public int freeActionPoint;
    public bool freeActionHasRolled;
    public BattleClashPointSnapshot freeActionPointSnapshot;
    public BattleClashResourceSnapshot freeActionResourceSnapshot;

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
        runtimeInteraction = clashSession != null
            ? clashSession.runtimeInteraction
            : null;
        planKind = clashSession != null
            ? BattleResolutionPlanKind.RespondedClash
            : BattleResolutionPlanKind.UnrespondedEnemyAttack;
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
