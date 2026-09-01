// 脚本中文说明：验证Ready、Movement与逐ActorContinuation三份契约互不替代。
using UnityEngine;

public static class BattleReadyMovementContinuationTests
{
    public static bool Run()
    {
        bool[] results =
        {
            VerifyMeleeAttackVsAttack(true),
            VerifyMeleeAttackVsAttack(false),
            VerifyAttackVsAttackReady(
                AttackDeliveryMode.LongRangeShoot,
                AttackDeliveryMode.Melee,
                BattlePresentationReadyPoseKind.Aim,
                BattlePresentationReadyPoseKind.Sprint
            ),
            VerifyAttackVsAttackReady(
                AttackDeliveryMode.CloseRangeShoot,
                AttackDeliveryMode.Melee,
                BattlePresentationReadyPoseKind.Sprint,
                BattlePresentationReadyPoseKind.Sprint
            ),
            VerifyDirectionalReady(
                false,
                CardType.Defense,
                false,
                BattlePresentationReadyPoseKind.Sprint,
                BattlePresentationReadyPoseKind.Guard
            ),
            VerifyDirectionalReady(
                true,
                CardType.Defense,
                false,
                BattlePresentationReadyPoseKind.Sprint,
                BattlePresentationReadyPoseKind.Guard
            ),
            VerifyDirectionalReady(
                false,
                CardType.Dodge,
                false,
                BattlePresentationReadyPoseKind.Sprint,
                BattlePresentationReadyPoseKind.Dodge
            ),
            VerifyDirectionalReady(
                true,
                CardType.Dodge,
                false,
                BattlePresentationReadyPoseKind.Sprint,
                BattlePresentationReadyPoseKind.Dodge
            ),
            VerifyNewDodgeEngagementReappliesBothReady(),
            VerifyPreserveDodgeActor(),
            VerifyContinuousDodgeAttackReady(
                AttackDeliveryMode.Melee,
                BattlePresentationReadyPoseKind.Sprint
            ),
            VerifyContinuousDodgeAttackReady(
                AttackDeliveryMode.LongRangeShoot,
                BattlePresentationReadyPoseKind.Aim
            ),
            VerifyContinuousDodgeAttackReady(
                AttackDeliveryMode.CloseRangeShoot,
                BattlePresentationReadyPoseKind.Sprint
            ),
            VerifyUnilateralReady(true, AttackDeliveryMode.Melee,
                BattlePresentationReadyPoseKind.Sprint, true),
            VerifyUnilateralReady(false, AttackDeliveryMode.Melee,
                BattlePresentationReadyPoseKind.Sprint, true),
            VerifyUnilateralReady(true, AttackDeliveryMode.CloseRangeShoot,
                BattlePresentationReadyPoseKind.Sprint, false),
            VerifyUnilateralReady(false, AttackDeliveryMode.LongRangeShoot,
                BattlePresentationReadyPoseKind.Aim, false),
            VerifyNoApproachStillHasReadyHandler(),
            VerifyPreserveDoesNotAffectAttackActor(),
            VerifyNextEngagementOverridesPreviousPose()
        };
        string[] names =
        {
            "Melee AttackVsAttack Ready + Approach",
            "Melee AttackVsAttack 无Approach仍Ready",
            "LongRange Aim + Melee Sprint",
            "CloseRange + Melee 双Sprint",
            "Enemy Attack + Player Defense",
            "Player Attack + Enemy Defense",
            "Enemy Attack + Player Dodge",
            "Player Attack + Enemy Dodge",
            "普通Dodge后新Engagement重建双方Ready",
            "Continuous Dodge只保留Dodger",
            "Continuous Dodge新Melee Attacker Sprint",
            "Continuous Dodge新LongRange Attacker Aim",
            "Continuous Dodge新CloseRange Attacker Sprint",
            "Player Melee Unilateral",
            "Enemy Melee Unilateral",
            "CloseRange Unilateral Sprint",
            "LongRange Unilateral Aim",
            "RequiresApproach=false仍有Ready Handler",
            "PreserveDodge不影响Attack Actor",
            "ActionComplete后新ActionBegin覆盖旧Pose"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式98 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }
        Debug.Log("模式98 Ready/Movement/Continuation聚合结果：" + allPassed);
        return allPassed;
    }

    static bool VerifyMeleeAttackVsAttack(bool requiresApproach)
    {
        BattlePresentationRoute route = CreateRoute(
            CardType.Attack,
            AttackDeliveryMode.Melee,
            CardType.Attack,
            AttackDeliveryMode.Melee,
            false
        );
        BattlePresentationPhaseContract phase = route?.InteractionContext
            .CreateActionBeginPhaseContract(requiresApproach);
        BattlePresentationReadyContract ready =
            BattlePresentationReadyPolicy.Create(route);
        return phase != null &&
            phase.RequiresApproach == requiresApproach &&
            phase.RequiresReadyPose &&
            IsReady(ready.Primary, route.InteractionContext.AttackActionA,
                BattlePresentationReadyPoseKind.Sprint) &&
            IsReady(ready.Secondary, route.InteractionContext.AttackActionB,
                BattlePresentationReadyPoseKind.Sprint);
    }

    static bool VerifyAttackVsAttackReady(
        string deliveryA,
        string deliveryB,
        BattlePresentationReadyPoseKind expectedA,
        BattlePresentationReadyPoseKind expectedB
    )
    {
        BattlePresentationRoute route = CreateRoute(
            CardType.Attack,
            deliveryA,
            CardType.Attack,
            deliveryB,
            false
        );
        BattlePresentationReadyContract ready =
            BattlePresentationReadyPolicy.Create(route);
        return route != null &&
            IsReady(ready.Primary, route.InteractionContext.AttackActionA,
                expectedA) &&
            IsReady(ready.Secondary, route.InteractionContext.AttackActionB,
                expectedB);
    }

    static bool VerifyDirectionalReady(
        bool attackOnSideA,
        string responseType,
        bool preserveDodge,
        BattlePresentationReadyPoseKind expectedAttack,
        BattlePresentationReadyPoseKind expectedResponse
    )
    {
        BattlePresentationRoute route = CreateDirectionalRoute(
            attackOnSideA,
            responseType,
            AttackDeliveryMode.Melee,
            preserveDodge
        );
        BattlePresentationReadyContract ready =
            BattlePresentationReadyPolicy.Create(route);
        BattleExecutionAction responseAction = responseType == CardType.Defense
            ? route?.InteractionContext.DefenseAction
            : route?.InteractionContext.DodgeAction;
        return route != null &&
            IsReady(ready.Primary, route.InteractionContext.AttackAction,
                expectedAttack) &&
            IsReady(ready.Secondary, responseAction, expectedResponse);
    }

    static bool VerifyNewDodgeEngagementReappliesBothReady()
    {
        return VerifyDirectionalReady(
            true,
            CardType.Dodge,
            false,
            BattlePresentationReadyPoseKind.Sprint,
            BattlePresentationReadyPoseKind.Dodge
        );
    }

    static bool VerifyPreserveDodgeActor()
    {
        BattlePresentationRoute route = CreateDirectionalRoute(
            false,
            CardType.Dodge,
            AttackDeliveryMode.Melee,
            true
        );
        BattlePresentationReadyContract ready =
            BattlePresentationReadyPolicy.Create(route);
        return ready.Primary.ShouldApplyReady &&
            !ready.Primary.PreserveCurrentPose &&
            ready.Secondary.PreserveCurrentPose &&
            !ready.Secondary.ShouldApplyReady &&
            ReferenceEquals(
                ready.Secondary.Action,
                route.InteractionContext.DodgeAction
            );
    }

    static bool VerifyContinuousDodgeAttackReady(
        string delivery,
        BattlePresentationReadyPoseKind expectedAttack
    )
    {
        BattlePresentationRoute route = CreateDirectionalRoute(
            false,
            CardType.Dodge,
            delivery,
            true
        );
        BattlePresentationReadyContract ready =
            BattlePresentationReadyPolicy.Create(route);
        return IsReady(
                ready.Primary,
                route.InteractionContext.AttackAction,
                expectedAttack
            ) &&
            ready.Secondary.PreserveCurrentPose &&
            !ready.Secondary.ShouldApplyReady;
    }

    static bool VerifyUnilateralReady(
        bool attackOnSideA,
        string delivery,
        BattlePresentationReadyPoseKind expected,
        bool requiresApproach
    )
    {
        BattlePresentationRoute route = attackOnSideA
            ? CreateRoute(CardType.Attack, delivery, null, null, false)
            : CreateRoute(null, null, CardType.Attack, delivery, false);
        BattlePresentationPhaseContract phase = route?.InteractionContext
            .CreateActionBeginPhaseContract(requiresApproach);
        BattlePresentationReadyContract ready =
            BattlePresentationReadyPolicy.Create(route);
        return route != null && phase != null &&
            phase.RequiresApproach == requiresApproach &&
            phase.RequiresReadyPose && ready.Secondary == null &&
            IsReady(
                ready.Primary,
                route.InteractionContext.AttackAction,
                expected
            );
    }

    static bool VerifyNoApproachStillHasReadyHandler()
    {
        BattlePresentationRoute route = CreateDirectionalRoute(
            true,
            CardType.Defense,
            AttackDeliveryMode.Melee,
            false
        );
        BattlePresentationPhaseContract phase = route.InteractionContext
            .CreateActionBeginPhaseContract(false);
        BattlePresentationReadyContract ready =
            BattlePresentationReadyPolicy.Create(route);
        return !phase.RequiresApproach && phase.RequiresReadyPose &&
            ready.ReadyDirectiveCount == 2 &&
            ready.Primary.ShouldApplyReady &&
            ready.Secondary.ShouldApplyReady;
    }

    static bool VerifyPreserveDoesNotAffectAttackActor()
    {
        BattlePresentationRoute route = CreateDirectionalRoute(
            true,
            CardType.Dodge,
            AttackDeliveryMode.Melee,
            true
        );
        BattlePresentationReadyContract ready =
            BattlePresentationReadyPolicy.Create(route);
        return ready.Primary.Action == route.InteractionContext.AttackAction &&
            ready.Primary.ShouldApplyReady &&
            !ready.Primary.PreserveCurrentPose &&
            ready.Secondary.Action == route.InteractionContext.DodgeAction &&
            ready.Secondary.PreserveCurrentPose;
    }

    static bool VerifyNextEngagementOverridesPreviousPose()
    {
        BattlePresentationRoute continuation = CreateDirectionalRoute(
            false,
            CardType.Dodge,
            AttackDeliveryMode.Melee,
            true
        );
        BattlePresentationRoute nextEngagement = CreateDirectionalRoute(
            false,
            CardType.Dodge,
            AttackDeliveryMode.Melee,
            false
        );
        BattlePresentationReadyContract previous =
            BattlePresentationReadyPolicy.Create(continuation);
        BattlePresentationReadyContract next =
            BattlePresentationReadyPolicy.Create(nextEngagement);
        return previous.Secondary.PreserveCurrentPose &&
            next.Primary.ShouldApplyReady &&
            next.Secondary.ShouldApplyReady &&
            !next.Secondary.PreserveCurrentPose;
    }

    static bool IsReady(
        BattlePresentationReadyDirective directive,
        BattleExecutionAction expectedAction,
        BattlePresentationReadyPoseKind expectedPose
    )
    {
        return directive != null && directive.ShouldApplyReady &&
            ReferenceEquals(directive.Action, expectedAction) &&
            directive.PoseKind == expectedPose;
    }

    static BattlePresentationRoute CreateDirectionalRoute(
        bool attackOnSideA,
        string responseType,
        string delivery,
        bool preserveDodge
    )
    {
        return attackOnSideA
            ? CreateRoute(
                CardType.Attack,
                delivery,
                responseType,
                null,
                preserveDodge
            )
            : CreateRoute(
                responseType,
                null,
                CardType.Attack,
                delivery,
                preserveDodge
            );
    }

    static BattlePresentationRoute CreateRoute(
        string sideACardType,
        string sideADelivery,
        string sideBCardType,
        string sideBDelivery,
        bool preserveDodge
    )
    {
        CharacterData sideAActor = new CharacterData(
            "mode98_side_a",
            30,
            5,
            5
        );
        CharacterData sideBActor = new CharacterData(
            "mode98_side_b",
            30,
            5,
            5
        );
        BattleExecutionAction sideA = sideACardType != null
            ? CreateAction(
                sideAActor,
                sideBActor,
                sideACardType,
                sideADelivery,
                "mode98_side_a_card"
            )
            : null;
        BattleExecutionAction sideB = sideBCardType != null
            ? CreateAction(
                sideBActor,
                sideAActor,
                sideBCardType,
                sideBDelivery,
                "mode98_side_b_card"
            )
            : null;
        BattleExecutionInteractionContext executionContext =
            new BattleExecutionInteractionContext(null, sideA, sideB);
        BattlePresentationInteractionContextFactory.TryCreate(
            executionContext,
            preserveDodge,
            out BattlePresentationInteractionContext interactionContext
        );
        BattlePresentationRequest request = new BattlePresentationRequest(
            98L,
            BattlePresentationCue.ActionBegin,
            null,
            null,
            null,
            null,
            string.Empty,
            false,
            interactionContext
        );
        BattlePresentationRouter.TryCreateRoute(request, out var route);
        return route;
    }

    static BattleExecutionAction CreateAction(
        CharacterData actor,
        CharacterData target,
        string cardType,
        string delivery,
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
                    ? delivery
                    : string.Empty
            },
            id + "_instance"
        );
        return new BattleExecutionAction(actor, card, null, null, target);
    }
}
