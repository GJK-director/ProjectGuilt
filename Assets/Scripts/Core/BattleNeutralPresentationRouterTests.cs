// 脚本中文说明：验证 Neutral Presenter Router 只按 Interaction、Delivery、Cue 与 Result 分发。
using UnityEngine;

public static class BattleNeutralPresentationRouterTests
{
    public static bool Run()
    {
        bool[] results =
        {
            VerifyAttackClash(
                AttackDeliveryMode.Melee,
                AttackDeliveryMode.Melee,
                BattlePresentationGrammarKind.MeleeClash
            ),
            VerifyAttackClash(
                AttackDeliveryMode.LongRangeShoot,
                AttackDeliveryMode.Melee,
                BattlePresentationGrammarKind.LongRangeVsMeleeClash
            ),
            VerifyAttackClash(
                AttackDeliveryMode.CloseRangeShoot,
                AttackDeliveryMode.Melee,
                BattlePresentationGrammarKind.CloseRangeClash
            ),
            VerifyDirectionalRoute(
                true,
                CardType.Defense,
                AttackDeliveryMode.Melee,
                BattlePresentationHandlerKind.AttackVsDefense
            ),
            VerifyDirectionalRoute(
                false,
                CardType.Defense,
                AttackDeliveryMode.Melee,
                BattlePresentationHandlerKind.AttackVsDefense
            ),
            VerifyDirectionalRoute(
                true,
                CardType.Defense,
                AttackDeliveryMode.LongRangeShoot,
                BattlePresentationHandlerKind.AttackVsDefense
            ),
            VerifyDirectionalRoute(
                false,
                CardType.Defense,
                AttackDeliveryMode.LongRangeShoot,
                BattlePresentationHandlerKind.AttackVsDefense
            ),
            VerifyDirectionalRoute(
                true,
                CardType.Defense,
                AttackDeliveryMode.CloseRangeShoot,
                BattlePresentationHandlerKind.AttackVsDefense
            ),
            VerifyDirectionalRoute(
                true,
                CardType.Dodge,
                AttackDeliveryMode.Melee,
                BattlePresentationHandlerKind.AttackVsDodge
            ),
            VerifyDirectionalRoute(
                false,
                CardType.Dodge,
                AttackDeliveryMode.Melee,
                BattlePresentationHandlerKind.AttackVsDodge
            ),
            VerifyDirectionalRoute(
                true,
                CardType.Dodge,
                AttackDeliveryMode.LongRangeShoot,
                BattlePresentationHandlerKind.AttackVsDodge
            ),
            VerifyDirectionalRoute(
                false,
                CardType.Dodge,
                AttackDeliveryMode.CloseRangeShoot,
                BattlePresentationHandlerKind.AttackVsDodge
            ),
            VerifyUnilateral(true, AttackDeliveryMode.Melee),
            VerifyUnilateral(false, AttackDeliveryMode.Melee),
            VerifyUnilateral(true, AttackDeliveryMode.CloseRangeShoot),
            VerifyUnilateral(false, AttackDeliveryMode.CloseRangeShoot),
            VerifyUnilateral(true, AttackDeliveryMode.LongRangeShoot),
            VerifyUnilateral(false, AttackDeliveryMode.LongRangeShoot),
            VerifyDodgeResults(),
            VerifyDefenseResults(),
            VerifyContinuousDodgeMetadata(),
            VerifyGuardAttackDeliveryVisualPolicy(),
            VerifyDodgeAttackDeliveryVisualPolicy(),
            VerifyRejectedRoutes()
        };
        string[] names =
        {
            "AttackVsAttack Melee/Melee",
            "AttackVsAttack LongRange/Melee",
            "AttackVsAttack CloseRange/Melee",
            "Player Attack + Enemy Defense",
            "Enemy Attack + Player Defense",
            "Player LongRange + Enemy Defense",
            "Enemy LongRange + Player Defense",
            "Player CloseRange + Enemy Defense",
            "Player Attack + Enemy Dodge",
            "Enemy Attack + Player Dodge",
            "LongRange + Dodge",
            "CloseRange + Dodge",
            "Player Melee Unilateral",
            "Enemy Melee Unilateral",
            "Player CloseRange Unilateral",
            "Enemy CloseRange Unilateral",
            "Player LongRange Unilateral",
            "Enemy LongRange Unilateral",
            "Dodge Result只消费Combat Result",
            "Defense Result只消费Combat Result",
            "Continuous Dodge保留metadata",
            "Guard Player按Melee/CloseRange/LongRange选择攻击视觉",
            "Dodge Player的LongRange不映射为Slash",
            "NoInteraction/Ability/ActionUnavailable拒绝"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式97 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }
        Debug.Log("模式97 Neutral Presentation Router聚合结果：" + allPassed);
        return allPassed;
    }

    static bool VerifyAttackClash(
        string deliveryA,
        string deliveryB,
        BattlePresentationGrammarKind expectedGrammar
    )
    {
        BattlePresentationRequest request = CreateRequest(
            CardType.Attack,
            deliveryA,
            CardType.Attack,
            deliveryB,
            false,
            BattlePresentationCue.ActionBegin,
            string.Empty
        );
        return TryRoute(request, out BattlePresentationRoute route) &&
            route.HandlerKind == BattlePresentationHandlerKind.AttackVsAttack &&
            route.GrammarKind == expectedGrammar &&
            route.InteractionType == BattleInteractionType.AttackVsAttack &&
            route.UsesLongRangeGrammar ==
                (expectedGrammar ==
                    BattlePresentationGrammarKind.LongRangeVsMeleeClash);
    }

    static bool VerifyDirectionalRoute(
        bool attackOnSideA,
        string responseCardType,
        string deliveryMode,
        BattlePresentationHandlerKind expectedHandler
    )
    {
        BattlePresentationRequest request = attackOnSideA
            ? CreateRequest(
                CardType.Attack,
                deliveryMode,
                responseCardType,
                null,
                false,
                BattlePresentationCue.ActionBegin,
                string.Empty
            )
            : CreateRequest(
                responseCardType,
                null,
                CardType.Attack,
                deliveryMode,
                false,
                BattlePresentationCue.ActionBegin,
                string.Empty
            );
        BattlePresentationAttackDeliveryKind expectedDelivery =
            ParseExpectedDelivery(deliveryMode);
        BattlePresentationGrammarKind expectedGrammar = expectedHandler ==
                BattlePresentationHandlerKind.AttackVsDefense
            ? BattlePresentationGrammarKind.AttackVsDefense
            : BattlePresentationGrammarKind.AttackVsDodge;
        return TryRoute(request, out BattlePresentationRoute route) &&
            route.HandlerKind == expectedHandler &&
            route.GrammarKind == expectedGrammar &&
            route.AttackDelivery == expectedDelivery &&
            !route.UsesLongRangeGrammar &&
            route.InteractionContext.AttackAction != null &&
            (expectedHandler == BattlePresentationHandlerKind.AttackVsDefense
                ? route.InteractionContext.DefenseAction != null
                : route.InteractionContext.DodgeAction != null);
    }

    static bool VerifyUnilateral(bool attackOnSideA, string deliveryMode)
    {
        BattlePresentationRequest request = attackOnSideA
            ? CreateRequest(
                CardType.Attack,
                deliveryMode,
                null,
                null,
                false,
                BattlePresentationCue.Impact,
                "FreeAttack"
            )
            : CreateRequest(
                null,
                null,
                CardType.Attack,
                deliveryMode,
                false,
                BattlePresentationCue.Impact,
                "UnrespondedEnemyAttack"
            );
        return TryRoute(request, out BattlePresentationRoute route) &&
            route.HandlerKind ==
                BattlePresentationHandlerKind.UnilateralAttack &&
            route.GrammarKind ==
                BattlePresentationGrammarKind.UnilateralAttack &&
            route.AttackDelivery == ParseExpectedDelivery(deliveryMode) &&
            route.ResultKind ==
                BattlePresentationResultKind.UnilateralAttack;
    }

    static bool VerifyDodgeResults()
    {
        BattlePresentationRoute success = RouteForResult(
            CardType.Dodge,
            "DodgeSuccess"
        );
        BattlePresentationRoute failed = RouteForResult(
            CardType.Dodge,
            "DodgeFailed"
        );
        return success != null && failed != null &&
            success.HandlerKind == failed.HandlerKind &&
            success.HandlerKind == BattlePresentationHandlerKind.AttackVsDodge &&
            success.ResultKind == BattlePresentationResultKind.DodgeSuccess &&
            failed.ResultKind == BattlePresentationResultKind.DodgeFailed;
    }

    static bool VerifyDefenseResults()
    {
        BattlePresentationRoute fullBlock = RouteForResult(
            CardType.Defense,
            "DefenseFullBlock"
        );
        BattlePresentationRoute reduced = RouteForResult(
            CardType.Defense,
            "DefenseReducedDamage"
        );
        return fullBlock != null && reduced != null &&
            fullBlock.HandlerKind == reduced.HandlerKind &&
            fullBlock.HandlerKind ==
                BattlePresentationHandlerKind.AttackVsDefense &&
            fullBlock.ResultKind ==
                BattlePresentationResultKind.DefenseFullBlock &&
            reduced.ResultKind ==
                BattlePresentationResultKind.DefenseReducedDamage;
    }

    static bool VerifyContinuousDodgeMetadata()
    {
        BattlePresentationRequest request = CreateRequest(
            CardType.Attack,
            AttackDeliveryMode.Melee,
            CardType.Dodge,
            null,
            true,
            BattlePresentationCue.ActionBegin,
            string.Empty
        );
        return TryRoute(request, out BattlePresentationRoute route) &&
            route.InteractionType == BattleInteractionType.AttackVsDodge &&
            route.HandlerKind == BattlePresentationHandlerKind.AttackVsDodge &&
            route.ContinuationPolicy ==
                BattlePresentationContinuationPolicy.PreserveDodgePose;
    }

    static bool VerifyGuardAttackDeliveryVisualPolicy()
    {
        return BattleAttackVsGuardPresentationPlayer
                .UsesMeleeVisual(
                    BattlePresentationAttackDeliveryKind.Melee
                ) &&
            BattleAttackVsGuardPresentationPlayer
                .UsesCloseRangeShootVisual(
                    BattlePresentationAttackDeliveryKind.CloseRangeShoot
                ) &&
            BattleAttackVsGuardPresentationPlayer
                .UsesLongRangeShootVisual(
                    BattlePresentationAttackDeliveryKind.LongRangeShoot
                ) &&
            !BattleAttackVsGuardPresentationPlayer
                .UsesMeleeVisual(
                    BattlePresentationAttackDeliveryKind.LongRangeShoot
                );
    }

    static bool VerifyDodgeAttackDeliveryVisualPolicy()
    {
        return BattleAttackVsDodgePresentationPlayer
                .UsesMeleeVisual(
                    BattlePresentationAttackDeliveryKind.Melee
                ) &&
            BattleAttackVsDodgePresentationPlayer
                .UsesCloseRangeShootVisual(
                    BattlePresentationAttackDeliveryKind.CloseRangeShoot
                ) &&
            BattleAttackVsDodgePresentationPlayer
                .UsesLongRangeShootVisual(
                    BattlePresentationAttackDeliveryKind.LongRangeShoot
                ) &&
            !BattleAttackVsDodgePresentationPlayer
                .UsesMeleeVisual(
                    BattlePresentationAttackDeliveryKind.LongRangeShoot
                );
    }

    static bool VerifyRejectedRoutes()
    {
        BattlePresentationRequest noInteraction = CreateRequest(
            CardType.Defense,
            null,
            CardType.Dodge,
            null,
            false,
            BattlePresentationCue.ActionBegin,
            string.Empty
        );
        BattlePresentationRequest ability = CreateRequest(
            "Ability",
            null,
            null,
            null,
            false,
            BattlePresentationCue.ActionBegin,
            string.Empty
        );
        BattlePresentationRequest unavailable = CreateRequest(
            CardType.Attack,
            AttackDeliveryMode.LongRangeShoot,
            null,
            null,
            false,
            BattlePresentationCue.ActionBegin,
            "ActionUnavailable"
        );
        return !TryRoute(noInteraction, out _) &&
            !TryRoute(ability, out _) &&
            !TryRoute(unavailable, out _);
    }

    static BattlePresentationRoute RouteForResult(
        string responseCardType,
        string result
    )
    {
        BattlePresentationRequest request = CreateRequest(
            CardType.Attack,
            AttackDeliveryMode.Melee,
            responseCardType,
            null,
            false,
            BattlePresentationCue.RollResult,
            result
        );
        BattlePresentationRouter.TryCreateRoute(request, out var route);
        return route;
    }

    static bool TryRoute(
        BattlePresentationRequest request,
        out BattlePresentationRoute route
    )
    {
        return BattlePresentationRouter.TryCreateRoute(request, out route);
    }

    static BattlePresentationRequest CreateRequest(
        string sideACardType,
        string sideADeliveryMode,
        string sideBCardType,
        string sideBDeliveryMode,
        bool preserveDodgePose,
        BattlePresentationCue cue,
        string outcome
    )
    {
        CharacterData sideAActor = new CharacterData(
            "mode97_side_a",
            30,
            5,
            5
        );
        CharacterData sideBActor = new CharacterData(
            "mode97_side_b",
            30,
            5,
            5
        );
        BattleExecutionAction sideA = sideACardType != null
            ? CreateAction(
                sideAActor,
                sideBActor,
                sideACardType,
                sideADeliveryMode,
                "mode97_side_a_card"
            )
            : null;
        BattleExecutionAction sideB = sideBCardType != null
            ? CreateAction(
                sideBActor,
                sideAActor,
                sideBCardType,
                sideBDeliveryMode,
                "mode97_side_b_card"
            )
            : null;
        BattleExecutionInteractionContext executionContext =
            new BattleExecutionInteractionContext(null, sideA, sideB);
        BattlePresentationInteractionContextFactory.TryCreate(
            executionContext,
            preserveDodgePose,
            out BattlePresentationInteractionContext context
        );
        return new BattlePresentationRequest(
            97L,
            cue,
            null,
            null,
            null,
            null,
            outcome,
            false,
            context
        );
    }

    static BattleExecutionAction CreateAction(
        CharacterData actor,
        CharacterData target,
        string cardType,
        string deliveryMode,
        string id
    )
    {
        BattleCardState card = new BattleCardState(
            actor,
            new CardTestData
            {
                cardID = id,
                cardName = id,
                cardType = cardType,
                attackDeliveryMode = cardType == CardType.Attack
                    ? deliveryMode
                    : string.Empty
            },
            id + "_instance"
        );
        return new BattleExecutionAction(actor, card, null, null, target);
    }

    static BattlePresentationAttackDeliveryKind ParseExpectedDelivery(
        string deliveryMode
    )
    {
        if (deliveryMode == AttackDeliveryMode.LongRangeShoot)
        {
            return BattlePresentationAttackDeliveryKind.LongRangeShoot;
        }
        if (deliveryMode == AttackDeliveryMode.CloseRangeShoot)
        {
            return BattlePresentationAttackDeliveryKind.CloseRangeShoot;
        }
        return BattlePresentationAttackDeliveryKind.Melee;
    }
}
