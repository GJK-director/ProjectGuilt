// 脚本中文说明：把 Execution 层已经确定的有效 Interaction 归一化为中立的表现角色。
public enum BattlePresentationContinuationPolicy
{
    NewEngagement,
    PreserveDodgePose
}

public sealed class BattlePresentationPhaseContract
{
    public bool RequiresApproach { get; private set; }
    public bool RequiresReadyPose { get; private set; }
    public bool PreservePreviousPose { get; private set; }

    private BattlePresentationPhaseContract(
        bool requiresApproach,
        BattlePresentationContinuationPolicy continuationPolicy
    )
    {
        RequiresApproach = requiresApproach;
        PreservePreviousPose = continuationPolicy ==
            BattlePresentationContinuationPolicy.PreserveDodgePose;
        // Ready是逐Actor契约；即使Dodge Actor保留Pose，新Attack Actor仍要进入Ready。
        RequiresReadyPose = true;
    }

    public static BattlePresentationPhaseContract CreateActionBegin(
        bool requiresApproach,
        BattlePresentationContinuationPolicy continuationPolicy
    )
    {
        return new BattlePresentationPhaseContract(
            requiresApproach,
            continuationPolicy
        );
    }
}

public sealed class BattlePresentationInteractionContext
{
    public BattleExecutionItem ExecutionItem { get; private set; }
    public BattleInteractionType InteractionType { get; private set; }
    public BattleExecutionAction SideA { get; private set; }
    public BattleExecutionAction SideB { get; private set; }

    // AttackVsAttack 保持两侧对称，不伪造 attacker / defender。
    public BattleExecutionAction AttackActionA { get; private set; }
    public BattleExecutionAction AttackActionB { get; private set; }

    public BattleExecutionAction AttackAction { get; private set; }
    public BattleExecutionAction DefenseAction { get; private set; }
    public BattleExecutionAction DodgeAction { get; private set; }
    public CharacterData Target { get; private set; }
    public BattlePresentationContinuationPolicy ContinuationPolicy
    {
        get;
        private set;
    }

    public string AttackDeliveryMode
    {
        get { return GetDeliveryMode(AttackAction); }
    }

    public string AttackDeliveryModeA
    {
        get { return GetDeliveryMode(AttackActionA); }
    }

    public string AttackDeliveryModeB
    {
        get { return GetDeliveryMode(AttackActionB); }
    }

    internal BattlePresentationInteractionContext(
        BattleExecutionInteractionContext executionContext,
        BattlePresentationContinuationPolicy continuationPolicy
    )
    {
        ExecutionItem = executionContext.executionItem;
        InteractionType = executionContext.effectiveInteractionType;
        SideA = executionContext.sideA;
        SideB = executionContext.sideB;
        ContinuationPolicy = continuationPolicy;
    }

    public BattlePresentationPhaseContract CreateActionBeginPhaseContract(
        bool requiresApproach
    )
    {
        return BattlePresentationPhaseContract.CreateActionBegin(
            requiresApproach,
            ContinuationPolicy
        );
    }

    internal void SetSymmetricAttackActions(
        BattleExecutionAction attackActionA,
        BattleExecutionAction attackActionB
    )
    {
        AttackActionA = attackActionA;
        AttackActionB = attackActionB;
    }

    internal void SetDirectionalActions(
        BattleExecutionAction attackAction,
        BattleExecutionAction defenseAction,
        BattleExecutionAction dodgeAction
    )
    {
        AttackAction = attackAction;
        DefenseAction = defenseAction;
        DodgeAction = dodgeAction;
        Target = attackAction != null ? attackAction.target : null;
    }

    private static string GetDeliveryMode(BattleExecutionAction action)
    {
        return action != null && action.cardState != null
            ? action.cardState.GetAttackDeliveryMode()
            : string.Empty;
    }
}

public static class BattlePresentationInteractionContextFactory
{
    // 本类不维护第二份 Interaction Matrix，只验证 Execution 已给出的 Effective Interaction 并提取角色。
    public static bool TryCreate(
        BattleExecutionInteractionContext executionContext,
        bool preserveDodgePose,
        out BattlePresentationInteractionContext presentationContext
    )
    {
        presentationContext = null;
        if (executionContext == null ||
            executionContext.effectiveInteractionType ==
                BattleInteractionType.NoInteraction)
        {
            return false;
        }

        BattlePresentationContinuationPolicy continuationPolicy =
            preserveDodgePose &&
            executionContext.effectiveInteractionType ==
                BattleInteractionType.AttackVsDodge
                ? BattlePresentationContinuationPolicy.PreserveDodgePose
                : BattlePresentationContinuationPolicy.NewEngagement;
        BattlePresentationInteractionContext context =
            new BattlePresentationInteractionContext(
                executionContext,
                continuationPolicy
            );

        if (executionContext.effectiveInteractionType ==
            BattleInteractionType.AttackVsAttack)
        {
            if (!IsRole(executionContext.sideA, CardType.Attack) ||
                !IsRole(executionContext.sideB, CardType.Attack))
            {
                return false;
            }

            context.SetSymmetricAttackActions(
                executionContext.sideA,
                executionContext.sideB
            );
            presentationContext = context;
            return true;
        }

        if (executionContext.effectiveInteractionType ==
            BattleInteractionType.AttackVsDefense)
        {
            return TrySetDirectionalContext(
                context,
                executionContext.sideA,
                executionContext.sideB,
                CardType.Defense,
                out presentationContext
            );
        }

        if (executionContext.effectiveInteractionType ==
            BattleInteractionType.AttackVsDodge)
        {
            return TrySetDirectionalContext(
                context,
                executionContext.sideA,
                executionContext.sideB,
                CardType.Dodge,
                out presentationContext
            );
        }

        if (executionContext.effectiveInteractionType ==
            BattleInteractionType.UnilateralAttack)
        {
            bool sideAIsAttack = IsRole(executionContext.sideA, CardType.Attack);
            bool sideBIsAttack = IsRole(executionContext.sideB, CardType.Attack);
            if (sideAIsAttack == sideBIsAttack)
            {
                return false;
            }

            context.SetDirectionalActions(
                sideAIsAttack ? executionContext.sideA : executionContext.sideB,
                null,
                null
            );
            presentationContext = context;
            return true;
        }

        return false;
    }

    private static bool TrySetDirectionalContext(
        BattlePresentationInteractionContext context,
        BattleExecutionAction sideA,
        BattleExecutionAction sideB,
        string responseCardType,
        out BattlePresentationInteractionContext presentationContext
    )
    {
        presentationContext = null;
        BattleExecutionAction attackAction;
        BattleExecutionAction responseAction;
        if (IsRole(sideA, CardType.Attack) &&
            IsRole(sideB, responseCardType))
        {
            attackAction = sideA;
            responseAction = sideB;
        }
        else if (IsRole(sideB, CardType.Attack) &&
            IsRole(sideA, responseCardType))
        {
            attackAction = sideB;
            responseAction = sideA;
        }
        else
        {
            return false;
        }

        context.SetDirectionalActions(
            attackAction,
            responseCardType == CardType.Defense ? responseAction : null,
            responseCardType == CardType.Dodge ? responseAction : null
        );
        presentationContext = context;
        return true;
    }

    private static bool IsRole(
        BattleExecutionAction action,
        string cardType
    )
    {
        return action != null && action.cardState != null &&
            action.cardState.cardData != null &&
            action.cardState.cardData.cardType == cardType;
    }
}
