// 脚本中文说明：单次拼点会话。只保存规则层上下文、初始化快照与逐次Roll结果，不负责动画、输入或伤害提交。
using UnityEngine;

public enum BattleClashType
{
    AttackVsAttack,
    DodgeVsAttack,
    DefenseVsAttack
}

public enum BattleClashAttemptResult
{
    None,
    SideAWin,
    SideBWin,
    AttackTie,
    DodgeSuccess,
    DodgeFailed,
    DefenseFullBlock,
    DefenseReducedDamage,
    TieLimit
}

public enum BattleClashFinalResult
{
    None,
    SideAWin,
    SideBWin,
    DodgeSuccess,
    DodgeFailed,
    DefenseFullBlock,
    DefenseReducedDamage,
    TieLimit
}

public sealed class BattleClashPointSnapshot
{
    public int nextCardPointStack;
    public int nextCardPointModifier;
    public int nextClashPointStack;
    public int nextClashPointModifier;
}

public sealed class BattleClashResourceSnapshot
{
    public BattleCardState cardState;
    public string resourceID;
    public int capturedStack;
    public bool hasRule;
    public bool normalVersionEnabled;
    public int selectedMinPoint;
    public int selectedMaxPoint;
    public int pointModifierFromResource;
    public int plannedConsumeAmount;
    public bool shouldConsumeOnSuccess;
}

public sealed class BattleClashSideState
{
    public CharacterData actor;
    public BattleCardState cardState;
    public BattleClashPointSnapshot pointSnapshot;
    public BattleClashResourceSnapshot resourceSnapshot;

    public BattleClashSideState(
        CharacterData actor,
        BattleCardState cardState,
        BattleClashPointSnapshot pointSnapshot,
        BattleClashResourceSnapshot resourceSnapshot
    )
    {
        this.actor = actor;
        this.cardState = cardState;
        this.pointSnapshot = pointSnapshot ?? new BattleClashPointSnapshot();
        this.resourceSnapshot = resourceSnapshot ??
            CreateDefaultResourceSnapshot(cardState);
    }

    static BattleClashResourceSnapshot CreateDefaultResourceSnapshot(
        BattleCardState cardState
    )
    {
        CardTestData cardData = cardState != null ? cardState.cardData : null;
        return new BattleClashResourceSnapshot
        {
            cardState = cardState,
            selectedMinPoint = cardData != null ? cardData.minPoint : 0,
            selectedMaxPoint = cardData != null ? cardData.maxPoint : 0
        };
    }
}

public sealed class BattleClashSession
{
    public const int MaxAttackTieCount = 10;

    public BattleClashType ClashType { get; private set; }
    public BattleClashSideState SideA { get; private set; }
    public BattleClashSideState SideB { get; private set; }
    public CharacterData ActualTarget { get; private set; }
    public bool IsContinuousDodgeContinuation { get; private set; }
    public bool UsesKnownSideBPoint { get; private set; }
    public int KnownSideBPoint { get; private set; }

    public int AttemptIndex { get; private set; }
    public int AttackTieCount { get; private set; }
    public int SideAPoint { get; private set; }
    public int SideBPoint { get; private set; }
    public int SideADefensePointScaled { get; private set; }
    public int RemainingAttackPoint { get; private set; }
    public bool IsFullBlock { get; private set; }
    public BattleClashAttemptResult AttemptResult { get; private set; }
    public BattleClashFinalResult FinalResult { get; private set; }
    public bool RequiresAnotherRoll { get; private set; }
    public bool IsFinalized { get; private set; }

    BattleClashSession(
        BattleClashType clashType,
        BattleClashSideState sideA,
        BattleClashSideState sideB,
        CharacterData actualTarget,
        bool usesKnownSideBPoint,
        int knownSideBPoint,
        bool isContinuousDodgeContinuation
    )
    {
        ClashType = clashType;
        SideA = sideA;
        SideB = sideB;
        ActualTarget = actualTarget;
        UsesKnownSideBPoint = usesKnownSideBPoint;
        KnownSideBPoint = Mathf.Max(0, knownSideBPoint);
        IsContinuousDodgeContinuation = isContinuousDodgeContinuation;
        AttemptResult = BattleClashAttemptResult.None;
        FinalResult = BattleClashFinalResult.None;
    }

    public static BattleClashSession CreateAttackVsAttack(
        BattleClashSideState sideA,
        BattleClashSideState sideB,
        CharacterData actualTarget
    )
    {
        return new BattleClashSession(
            BattleClashType.AttackVsAttack,
            sideA,
            sideB,
            actualTarget,
            false,
            0,
            false
        );
    }

    public static BattleClashSession CreateDodgeVsAttack(
        BattleClashSideState dodgeSide,
        BattleClashSideState attackSide,
        CharacterData actualTarget,
        bool usesKnownAttackPoint = false,
        int knownAttackPoint = 0,
        bool isContinuousDodgeContinuation = false
    )
    {
        return new BattleClashSession(
            BattleClashType.DodgeVsAttack,
            dodgeSide,
            attackSide,
            actualTarget,
            usesKnownAttackPoint,
            knownAttackPoint,
            isContinuousDodgeContinuation
        );
    }

    public static BattleClashSession CreateDefenseVsAttack(
        BattleClashSideState defenseSide,
        BattleClashSideState attackSide,
        CharacterData actualTarget,
        bool usesKnownAttackPoint = false,
        int knownAttackPoint = 0
    )
    {
        return new BattleClashSession(
            BattleClashType.DefenseVsAttack,
            defenseSide,
            attackSide,
            actualTarget,
            usesKnownAttackPoint,
            knownAttackPoint,
            false
        );
    }

    // 每次调用只产生一个新的RollAttempt；AttackTie不会在本方法内部自动再Roll。
    public bool RollNextAttempt()
    {
        if (IsFinalized || SideA == null || SideB == null ||
            SideA.cardState == null || SideB.cardState == null ||
            SideA.cardState.cardData == null ||
            SideB.cardState.cardData == null)
        {
            return false;
        }

        AttemptIndex++;
        RequiresAnotherRoll = false;

        if (ClashType == BattleClashType.AttackVsAttack)
        {
            RollAttackAttempt();
        }
        else if (ClashType == BattleClashType.DodgeVsAttack)
        {
            RollDodgeAttempt();
        }
        else
        {
            RollDefenseAttempt();
        }

        return true;
    }

    void RollAttackAttempt()
    {
        SideAPoint = RollClashPoint(SideA);
        SideBPoint = RollClashPoint(SideB);

        if (SideAPoint > SideBPoint)
        {
            FinalizeResult(
                BattleClashAttemptResult.SideAWin,
                BattleClashFinalResult.SideAWin
            );
            return;
        }
        if (SideAPoint < SideBPoint)
        {
            FinalizeResult(
                BattleClashAttemptResult.SideBWin,
                BattleClashFinalResult.SideBWin
            );
            return;
        }

        AttackTieCount++;
        if (AttackTieCount >= MaxAttackTieCount)
        {
            FinalizeResult(
                BattleClashAttemptResult.TieLimit,
                BattleClashFinalResult.TieLimit
            );
            return;
        }

        AttemptResult = BattleClashAttemptResult.AttackTie;
        RequiresAnotherRoll = true;
    }

    void RollDodgeAttempt()
    {
        SideAPoint = RollClashPoint(SideA);
        SideBPoint = UsesKnownSideBPoint
            ? KnownSideBPoint
            : RollClashPoint(SideB);

        // 闪避点数相等也算闪避成功，不进入AttackTie或TieLimit。
        if (SideAPoint >= SideBPoint)
        {
            FinalizeResult(
                BattleClashAttemptResult.DodgeSuccess,
                BattleClashFinalResult.DodgeSuccess
            );
            return;
        }

        FinalizeResult(
            BattleClashAttemptResult.DodgeFailed,
            BattleClashFinalResult.DodgeFailed
        );
    }

    void RollDefenseAttempt()
    {
        SideADefensePointScaled = BattleCalculator.GetFinalDefensePointScaled(
            SideA.actor,
            SideA.cardState.cardData,
            SideA.pointSnapshot.nextCardPointModifier,
            SideA.resourceSnapshot.selectedMinPoint,
            SideA.resourceSnapshot.selectedMaxPoint,
            SideA.resourceSnapshot.pointModifierFromResource
        );
        SideAPoint = BattleCalculator.ConvertScaledDamageToHPDamage(
            SideADefensePointScaled
        );
        SideBPoint = UsesKnownSideBPoint
            ? KnownSideBPoint
            : BattleCalculator.GetFinalAttackPointWithoutClash(
                SideB.actor,
                SideB.cardState.cardData,
                SideB.pointSnapshot.nextCardPointModifier,
                SideB.resourceSnapshot.selectedMinPoint,
                SideB.resourceSnapshot.selectedMaxPoint,
                SideB.resourceSnapshot.pointModifierFromResource
            );
        RemainingAttackPoint = BattleCalculator
            .CalculateRemainingAttackPointAfterDefense(
                SideBPoint,
                SideADefensePointScaled
            );
        IsFullBlock = RemainingAttackPoint == 0;
        FinalizeResult(
            IsFullBlock
                ? BattleClashAttemptResult.DefenseFullBlock
                : BattleClashAttemptResult.DefenseReducedDamage,
            IsFullBlock
                ? BattleClashFinalResult.DefenseFullBlock
                : BattleClashFinalResult.DefenseReducedDamage
        );
    }

    static int RollClashPoint(BattleClashSideState side)
    {
        return BattleCalculator.GetFinalClashPoint(
            side.actor,
            side.cardState.cardData,
            side.pointSnapshot.nextClashPointModifier,
            side.pointSnapshot.nextCardPointModifier,
            side.resourceSnapshot.selectedMinPoint,
            side.resourceSnapshot.selectedMaxPoint,
            side.resourceSnapshot.pointModifierFromResource
        );
    }

    void FinalizeResult(
        BattleClashAttemptResult attemptResult,
        BattleClashFinalResult finalResult
    )
    {
        AttemptResult = attemptResult;
        FinalResult = finalResult;
        RequiresAnotherRoll = false;
        IsFinalized = true;
    }
}
