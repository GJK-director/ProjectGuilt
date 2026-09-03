// 脚本中文说明：把冻结的中立 Interaction Context 转换为 Presenter 演出路由。
using System;

public enum BattlePresentationHandlerKind
{
    None,
    AttackVsAttack,
    AttackVsDefense,
    AttackVsDodge,
    UnilateralAttack
}

public enum BattlePresentationGrammarKind
{
    None,
    MeleeClash,
    LongRangeVsMeleeClash,
    CloseRangeClash,
    AttackVsDefense,
    AttackVsDodge,
    UnilateralAttack
}

public enum BattlePresentationAttackDeliveryKind
{
    None,
    Melee,
    CloseRangeShoot,
    LongRangeShoot
}

public enum BattlePresentationResultKind
{
    None,
    SideAWin,
    SideBWin,
    AttackTie,
    DefenseFullBlock,
    DefenseReducedDamage,
    DodgeSuccess,
    DodgeFailed,
    UnilateralAttack
}

public sealed class BattlePresentationRoute
{
    public BattlePresentationHandlerKind HandlerKind { get; private set; }
    public BattlePresentationGrammarKind GrammarKind { get; private set; }
    public BattlePresentationCue Cue { get; private set; }
    public BattleInteractionType InteractionType { get; private set; }
    public BattlePresentationAttackDeliveryKind AttackDelivery { get; private set; }
    public BattlePresentationAttackDeliveryKind AttackDeliveryA { get; private set; }
    public BattlePresentationAttackDeliveryKind AttackDeliveryB { get; private set; }
    public BattlePresentationResultKind ResultKind { get; private set; }
    public BattlePresentationContinuationPolicy ContinuationPolicy
    {
        get;
        private set;
    }
    public BattlePresentationInteractionContext InteractionContext
    {
        get;
        private set;
    }

    public bool UsesLongRangeGrammar
    {
        get
        {
            return GrammarKind ==
                BattlePresentationGrammarKind.LongRangeVsMeleeClash;
        }
    }

    public bool UsesNearRangeAttackGrammar
    {
        get
        {
            return GrammarKind == BattlePresentationGrammarKind.MeleeClash ||
                GrammarKind ==
                    BattlePresentationGrammarKind.CloseRangeClash ||
                AttackDelivery == BattlePresentationAttackDeliveryKind.Melee ||
                AttackDelivery ==
                    BattlePresentationAttackDeliveryKind.CloseRangeShoot;
        }
    }

    internal BattlePresentationRoute(
        BattlePresentationRequest request,
        BattlePresentationInteractionContext context,
        BattlePresentationHandlerKind handlerKind,
        BattlePresentationGrammarKind grammarKind,
        BattlePresentationAttackDeliveryKind attackDelivery,
        BattlePresentationAttackDeliveryKind attackDeliveryA,
        BattlePresentationAttackDeliveryKind attackDeliveryB,
        BattlePresentationResultKind resultKind
    )
    {
        HandlerKind = handlerKind;
        GrammarKind = grammarKind;
        Cue = request.Cue;
        InteractionType = context.InteractionType;
        AttackDelivery = attackDelivery;
        AttackDeliveryA = attackDeliveryA;
        AttackDeliveryB = attackDeliveryB;
        ResultKind = resultKind;
        ContinuationPolicy = context.ContinuationPolicy;
        InteractionContext = context;
    }
}

public static class BattlePresentationRouter
{
    public static bool TryCreateRoute(
        BattlePresentationRequest request,
        out BattlePresentationRoute route
    )
    {
        route = null;
        BattlePresentationInteractionContext context = request != null
            ? request.InteractionContext
            : null;
        if (request == null || context == null || IsActionUnavailable(request))
        {
            return false;
        }

        BattlePresentationResultKind resultKind = ResolveResult(request, context);
        if (context.InteractionType == BattleInteractionType.AttackVsAttack)
        {
            BattlePresentationAttackDeliveryKind deliveryA = ParseDelivery(
                context.AttackDeliveryModeA
            );
            BattlePresentationAttackDeliveryKind deliveryB = ParseDelivery(
                context.AttackDeliveryModeB
            );
            if (deliveryA == BattlePresentationAttackDeliveryKind.None ||
                deliveryB == BattlePresentationAttackDeliveryKind.None)
            {
                return false;
            }

            BattlePresentationGrammarKind grammar = ResolveAttackClashGrammar(
                deliveryA,
                deliveryB
            );
            if (grammar == BattlePresentationGrammarKind.None)
            {
                return false;
            }

            route = new BattlePresentationRoute(
                request,
                context,
                BattlePresentationHandlerKind.AttackVsAttack,
                grammar,
                BattlePresentationAttackDeliveryKind.None,
                deliveryA,
                deliveryB,
                resultKind
            );
            return true;
        }

        BattlePresentationAttackDeliveryKind delivery = ParseDelivery(
            context.AttackDeliveryMode
        );
        if (delivery == BattlePresentationAttackDeliveryKind.None)
        {
            return false;
        }

        BattlePresentationHandlerKind handlerKind;
        BattlePresentationGrammarKind grammarKind;
        if (context.InteractionType == BattleInteractionType.AttackVsDefense)
        {
            handlerKind = BattlePresentationHandlerKind.AttackVsDefense;
            grammarKind = BattlePresentationGrammarKind.AttackVsDefense;
        }
        else if (context.InteractionType == BattleInteractionType.AttackVsDodge)
        {
            handlerKind = BattlePresentationHandlerKind.AttackVsDodge;
            grammarKind = BattlePresentationGrammarKind.AttackVsDodge;
        }
        else if (context.InteractionType == BattleInteractionType.UnilateralAttack)
        {
            handlerKind = BattlePresentationHandlerKind.UnilateralAttack;
            grammarKind = BattlePresentationGrammarKind.UnilateralAttack;
            resultKind = BattlePresentationResultKind.UnilateralAttack;
        }
        else
        {
            return false;
        }

        route = new BattlePresentationRoute(
            request,
            context,
            handlerKind,
            grammarKind,
            delivery,
            BattlePresentationAttackDeliveryKind.None,
            BattlePresentationAttackDeliveryKind.None,
            resultKind
        );
        return true;
    }

    public static bool IsActionUnavailable(BattlePresentationRequest request)
    {
        if (request == null)
        {
            return false;
        }

        return request.ExecutionItem != null &&
                request.ExecutionItem.responseAttemptState ==
                    BattleResponseAttemptState.UnavailableResource ||
            string.Equals(
                request.Outcome,
                "ActionUnavailable",
                StringComparison.Ordinal
            ) ||
            string.Equals(
                request.Outcome,
                "ResponseUnavailableResource",
                StringComparison.Ordinal
            );
    }

    private static BattlePresentationGrammarKind ResolveAttackClashGrammar(
        BattlePresentationAttackDeliveryKind deliveryA,
        BattlePresentationAttackDeliveryKind deliveryB
    )
    {
        bool aLongRange = deliveryA ==
            BattlePresentationAttackDeliveryKind.LongRangeShoot;
        bool bLongRange = deliveryB ==
            BattlePresentationAttackDeliveryKind.LongRangeShoot;
        if (aLongRange || bLongRange)
        {
            BattlePresentationAttackDeliveryKind opponent = aLongRange
                ? deliveryB
                : deliveryA;
            return aLongRange != bLongRange &&
                opponent == BattlePresentationAttackDeliveryKind.Melee
                    ? BattlePresentationGrammarKind.LongRangeVsMeleeClash
                    : BattlePresentationGrammarKind.None;
        }

        if (deliveryA == BattlePresentationAttackDeliveryKind.CloseRangeShoot ||
            deliveryB == BattlePresentationAttackDeliveryKind.CloseRangeShoot)
        {
            return BattlePresentationGrammarKind.CloseRangeClash;
        }

        return BattlePresentationGrammarKind.MeleeClash;
    }

    private static BattlePresentationAttackDeliveryKind ParseDelivery(
        string deliveryMode
    )
    {
        if (deliveryMode == AttackDeliveryMode.Melee)
        {
            return BattlePresentationAttackDeliveryKind.Melee;
        }
        if (deliveryMode == AttackDeliveryMode.CloseRangeShoot)
        {
            return BattlePresentationAttackDeliveryKind.CloseRangeShoot;
        }
        if (deliveryMode == AttackDeliveryMode.LongRangeShoot)
        {
            return BattlePresentationAttackDeliveryKind.LongRangeShoot;
        }
        return BattlePresentationAttackDeliveryKind.None;
    }

    private static BattlePresentationResultKind ResolveResult(
        BattlePresentationRequest request,
        BattlePresentationInteractionContext context
    )
    {
        if (context.InteractionType == BattleInteractionType.UnilateralAttack)
        {
            return BattlePresentationResultKind.UnilateralAttack;
        }

        BattleClashSession session = request.ClashSession;
        if (session != null)
        {
            if (!session.IsFinalized &&
                session.AttemptResult == BattleClashAttemptResult.AttackTie)
            {
                return BattlePresentationResultKind.AttackTie;
            }

            BattlePresentationResultKind sessionResult = ParseResult(
                session.FinalResult.ToString()
            );
            if (sessionResult != BattlePresentationResultKind.None)
            {
                return sessionResult;
            }
        }

        return ParseResult(request.Outcome);
    }

    private static BattlePresentationResultKind ParseResult(string result)
    {
        if (string.Equals(result, "SideAWin", StringComparison.Ordinal))
        {
            return BattlePresentationResultKind.SideAWin;
        }
        if (string.Equals(result, "SideBWin", StringComparison.Ordinal))
        {
            return BattlePresentationResultKind.SideBWin;
        }
        if (string.Equals(result, "AttackTie", StringComparison.Ordinal))
        {
            return BattlePresentationResultKind.AttackTie;
        }
        if (string.Equals(result, "DefenseFullBlock", StringComparison.Ordinal))
        {
            return BattlePresentationResultKind.DefenseFullBlock;
        }
        if (string.Equals(
                result,
                "DefenseReducedDamage",
                StringComparison.Ordinal
            ))
        {
            return BattlePresentationResultKind.DefenseReducedDamage;
        }
        if (string.Equals(result, "DodgeSuccess", StringComparison.Ordinal))
        {
            return BattlePresentationResultKind.DodgeSuccess;
        }
        if (string.Equals(result, "DodgeFailed", StringComparison.Ordinal))
        {
            return BattlePresentationResultKind.DodgeFailed;
        }
        return BattlePresentationResultKind.None;
    }
}
