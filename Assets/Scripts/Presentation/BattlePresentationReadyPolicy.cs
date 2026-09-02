// 脚本中文说明：为中立Presentation Route建立逐Actor Ready Pose，不负责位移。
public enum BattlePresentationReadyPoseKind
{
    None,
    Idle,
    Sprint,
    Aim,
    Guard,
    Dodge
}

public sealed class BattlePresentationReadyDirective
{
    public BattleExecutionAction Action { get; private set; }
    public CharacterData Actor { get; private set; }
    public BattlePresentationReadyPoseKind PoseKind { get; private set; }
    public bool PreserveCurrentPose { get; private set; }

    public bool ShouldApplyReady
    {
        get
        {
            return Actor != null &&
                PoseKind != BattlePresentationReadyPoseKind.None &&
                !PreserveCurrentPose;
        }
    }

    internal BattlePresentationReadyDirective(
        BattleExecutionAction action,
        BattlePresentationReadyPoseKind poseKind,
        bool preserveCurrentPose = false
    )
        : this(action, action != null ? action.actor : null, poseKind,
            preserveCurrentPose)
    {
    }

    private BattlePresentationReadyDirective(
        BattleExecutionAction action,
        CharacterData actor,
        BattlePresentationReadyPoseKind poseKind,
        bool preserveCurrentPose
    )
    {
        Action = action;
        Actor = actor;
        PoseKind = poseKind;
        PreserveCurrentPose = preserveCurrentPose;
    }

    internal static BattlePresentationReadyDirective ForActor(
        CharacterData actor,
        BattlePresentationReadyPoseKind poseKind,
        bool preserveCurrentPose = false
    )
    {
        return new BattlePresentationReadyDirective(
            null,
            actor,
            poseKind,
            preserveCurrentPose
        );
    }
}

public sealed class BattlePresentationReadyContract
{
    public BattlePresentationReadyDirective Primary { get; private set; }
    public BattlePresentationReadyDirective Secondary { get; private set; }

    public int ReadyDirectiveCount
    {
        get
        {
            int count = Primary != null && Primary.Actor != null ? 1 : 0;
            return count +
                (Secondary != null && Secondary.Actor != null ? 1 : 0);
        }
    }

    internal BattlePresentationReadyContract(
        BattlePresentationReadyDirective primary,
        BattlePresentationReadyDirective secondary
    )
    {
        Primary = primary;
        Secondary = secondary;
    }
}

public static class BattlePresentationReadyPolicy
{
    public static BattlePresentationReadyContract Create(
        BattlePresentationRoute route
    )
    {
        if (route == null || route.InteractionContext == null)
        {
            return new BattlePresentationReadyContract(null, null);
        }

        BattlePresentationInteractionContext context =
            route.InteractionContext;
        if (route.HandlerKind ==
            BattlePresentationHandlerKind.AttackVsAttack)
        {
            return new BattlePresentationReadyContract(
                CreateAttackDirective(
                    context.AttackActionA,
                    route.AttackDeliveryA
                ),
                CreateAttackDirective(
                    context.AttackActionB,
                    route.AttackDeliveryB
                )
            );
        }

        if (route.HandlerKind ==
            BattlePresentationHandlerKind.AttackVsDefense)
        {
            return new BattlePresentationReadyContract(
                CreateAttackDirective(
                    context.AttackAction,
                    route.AttackDelivery
                ),
                new BattlePresentationReadyDirective(
                    context.DefenseAction,
                    BattlePresentationReadyPoseKind.Idle
                )
            );
        }

        if (route.HandlerKind ==
            BattlePresentationHandlerKind.AttackVsDodge)
        {
            bool preserveDodge = route.ContinuationPolicy ==
                BattlePresentationContinuationPolicy.PreserveDodgePose;
            return new BattlePresentationReadyContract(
                CreateAttackDirective(
                    context.AttackAction,
                    route.AttackDelivery
                ),
                new BattlePresentationReadyDirective(
                    context.DodgeAction,
                    preserveDodge
                        ? BattlePresentationReadyPoseKind.Dodge
                        : BattlePresentationReadyPoseKind.Idle,
                    preserveDodge
                )
            );
        }

        if (route.HandlerKind ==
            BattlePresentationHandlerKind.UnilateralAttack)
        {
            return new BattlePresentationReadyContract(
                CreateAttackDirective(
                    context.AttackAction,
                    route.AttackDelivery
                ),
                BattlePresentationReadyDirective.ForActor(
                    context.Target,
                    BattlePresentationReadyPoseKind.Idle
                )
            );
        }

        return new BattlePresentationReadyContract(null, null);
    }

    private static BattlePresentationReadyDirective CreateAttackDirective(
        BattleExecutionAction action,
        BattlePresentationAttackDeliveryKind delivery
    )
    {
        BattlePresentationReadyPoseKind poseKind = delivery ==
                BattlePresentationAttackDeliveryKind.LongRangeShoot
            ? BattlePresentationReadyPoseKind.Aim
            : delivery == BattlePresentationAttackDeliveryKind.Melee ||
                delivery ==
                    BattlePresentationAttackDeliveryKind.CloseRangeShoot
                ? BattlePresentationReadyPoseKind.Sprint
                : BattlePresentationReadyPoseKind.None;
        return new BattlePresentationReadyDirective(action, poseKind);
    }
}
