// 脚本中文说明：根据中立 Presentation Context 描述 Runner 所需阶段，不读取历史 Item 来源。
public sealed class BattleExecutionPhaseRequirements
{
    public BattleInteractionType InteractionType { get; private set; }
    public bool HasPresentationPhases { get; private set; }
    public bool RequiresActionBegin { get; private set; }
    public bool RequiresRollResult { get; private set; }
    public bool RequiresManualRoll { get; private set; }
    public bool RequiresImpact { get; private set; }
    public bool RequiresActionComplete { get; private set; }
    public bool RequiresClashSession { get; private set; }

    internal BattleExecutionPhaseRequirements(
        BattleInteractionType interactionType,
        bool hasPresentationPhases,
        bool requiresRollResult,
        bool requiresManualRoll,
        bool requiresImpact,
        bool requiresClashSession
    )
    {
        InteractionType = interactionType;
        HasPresentationPhases = hasPresentationPhases;
        RequiresActionBegin = hasPresentationPhases;
        RequiresRollResult = hasPresentationPhases && requiresRollResult;
        RequiresManualRoll = hasPresentationPhases && requiresManualRoll;
        RequiresImpact = hasPresentationPhases && requiresImpact;
        RequiresActionComplete = hasPresentationPhases;
        RequiresClashSession = hasPresentationPhases && requiresClashSession;
    }
}

public static class BattleExecutionPausablePolicy
{
    public static BattleExecutionPhaseRequirements Evaluate(
        BattlePresentationInteractionContext context
    )
    {
        if (context == null ||
            context.InteractionType == BattleInteractionType.NoInteraction)
        {
            return new BattleExecutionPhaseRequirements(
                BattleInteractionType.NoInteraction,
                false,
                false,
                false,
                false,
                false
            );
        }

        if (context.InteractionType == BattleInteractionType.UnilateralAttack)
        {
            // 单方攻击内部生成攻击点，但不伪造 Clash 或等待手动 Roll。
            return new BattleExecutionPhaseRequirements(
                context.InteractionType,
                true,
                false,
                false,
                true,
                false
            );
        }

        bool requiresClash =
            context.InteractionType == BattleInteractionType.AttackVsAttack ||
            context.InteractionType == BattleInteractionType.AttackVsDefense ||
            context.InteractionType == BattleInteractionType.AttackVsDodge;
        return new BattleExecutionPhaseRequirements(
            context.InteractionType,
            requiresClash,
            requiresClash,
            requiresClash,
            requiresClash,
            requiresClash
        );
    }
}
