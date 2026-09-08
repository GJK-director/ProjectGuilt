// 脚本中文说明：验证Ready、Movement、Pose Handoff与逐Actor Continuation契约互不替代。
using System.Collections.Generic;
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
                BattlePresentationReadyPoseKind.Idle
            ),
            VerifyDirectionalReady(
                true,
                CardType.Defense,
                false,
                BattlePresentationReadyPoseKind.Sprint,
                BattlePresentationReadyPoseKind.Idle
            ),
            VerifyDirectionalReady(
                false,
                CardType.Dodge,
                false,
                BattlePresentationReadyPoseKind.Sprint,
                BattlePresentationReadyPoseKind.Idle
            ),
            VerifyDirectionalReady(
                true,
                CardType.Dodge,
                false,
                BattlePresentationReadyPoseKind.Sprint,
                BattlePresentationReadyPoseKind.Idle
            ),
            VerifyNewDodgeEngagementDoesNotPreapplyDodge(),
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
            VerifyUnilateralTargetParticipantTracking(),
            VerifyNoApproachStillHasReadyHandler(),
            VerifyPreserveDoesNotAffectAttackActor(),
            VerifyNextEngagementDoesNotPreapplyDodgePose(),
            VerifyPreviousOnlyActionParticipants(),
            VerifySharedCurrentActorIsNotPreviousOnly(),
            VerifyCurrentDefenseUsesIdleDirective(),
            VerifyContinuousDodgeRemainsCurrentParticipant(),
            VerifyExecutionClosureClearsParticipantTracking()
        };
        string[] names =
        {
            "Melee AttackVsAttack Ready + Approach",
            "Melee AttackVsAttack 无Approach仍Ready",
            "LongRange Aim + Melee Sprint",
            "CloseRange + Melee 双Sprint",
            "Enemy Attack Sprint + Player Defense Idle",
            "Player Attack Sprint + Enemy Defense Idle",
            "Enemy Attack Sprint + Player Dodge Idle",
            "Player Attack Sprint + Enemy Dodge Idle",
            "普通Dodge新Engagement应用Idle",
            "Continuous Dodge只保留Dodger",
            "Continuous Dodge新Melee Attacker Sprint",
            "Continuous Dodge新LongRange Attacker Aim",
            "Continuous Dodge新CloseRange Attacker Sprint",
            "Player Melee Unilateral",
            "Enemy Melee Unilateral",
            "CloseRange Unilateral Sprint",
            "LongRange Unilateral Aim",
            "Unilateral Target Idle且保留Participant Tracking",
            "RequiresApproach=false仍遵守Defense Idle Ready",
            "PreserveDodge不影响Attack Actor",
            "新普通Engagement使用Dodge Idle",
            "前一Action A/B相对当前C/D为PreviousOnly",
            "当前共享Actor不被PreviousOnly Idle覆盖",
            "前一Slash Actor作为当前Defense接收Idle",
            "Continuous Dodge当前Dodger不被PreviousOnly覆盖",
            "ExecutionComplete清空Participant Tracking"
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
            (preserveDodge
                ? IsPreservedDodge(ready.Secondary, responseAction)
                : IsReady(
                    ready.Secondary,
                    responseAction,
                    expectedResponse
                ));
    }

    static bool VerifyNewDodgeEngagementDoesNotPreapplyDodge()
    {
        return VerifyDirectionalReady(
            true,
            CardType.Dodge,
            false,
            BattlePresentationReadyPoseKind.Sprint,
            BattlePresentationReadyPoseKind.Idle
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
            ) &&
            ReferenceEquals(
                ready.Secondary.Actor,
                route.InteractionContext.DodgeAction.actor
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
            !ready.Secondary.ShouldApplyReady &&
            ReferenceEquals(
                ready.Secondary.Actor,
                route.InteractionContext.DodgeAction.actor
            );
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
            phase.RequiresReadyPose &&
            IsReady(
                ready.Primary,
                route.InteractionContext.AttackAction,
                expected
            ) &&
            IsActorReady(
                ready.Secondary,
                route.InteractionContext.Target,
                BattlePresentationReadyPoseKind.Idle
            ) &&
            ready.Secondary.Action == null;
    }

    static bool VerifyUnilateralTargetParticipantTracking()
    {
        BattlePresentationRoute route = CreateRoute(
            CardType.Attack,
            AttackDeliveryMode.Melee,
            null,
            null,
            false
        );
        BattlePresentationReadyContract ready =
            BattlePresentationReadyPolicy.Create(route);
        var participants = new List<CharacterData>();
        BattleSceneExecutionPresenter.CollectActionParticipants(
            route.InteractionContext,
            participants
        );
        return IsActorReady(
                ready.Secondary,
                route.InteractionContext.Target,
                BattlePresentationReadyPoseKind.Idle
            ) &&
            ready.Secondary.Action == null &&
            participants.Count == 2 &&
            participants.Contains(route.InteractionContext.AttackAction.actor) &&
            participants.Contains(route.InteractionContext.Target);
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
            IsReady(
                ready.Secondary,
                route.InteractionContext.DefenseAction,
                BattlePresentationReadyPoseKind.Idle
            );
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
            ready.Secondary.PreserveCurrentPose &&
            ReferenceEquals(
                ready.Secondary.Actor,
                route.InteractionContext.DodgeAction.actor
            );
    }

    static bool VerifyNextEngagementDoesNotPreapplyDodgePose()
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
            IsReady(
                next.Secondary,
                nextEngagement.InteractionContext.DodgeAction,
                BattlePresentationReadyPoseKind.Idle
            );
    }

    static bool VerifyPreviousOnlyActionParticipants()
    {
        BattlePresentationRoute previous = CreateRoute(
            CardType.Attack,
            AttackDeliveryMode.Melee,
            CardType.Attack,
            AttackDeliveryMode.Melee,
            false
        );
        BattlePresentationRoute current = CreateRoute(
            CardType.Attack,
            AttackDeliveryMode.Melee,
            CardType.Attack,
            AttackDeliveryMode.Melee,
            false
        );
        var previousParticipants = new List<CharacterData>();
        var currentParticipants = new List<CharacterData>();
        var previousOnlyParticipants = new List<CharacterData>();
        BattleSceneExecutionPresenter.CollectActionParticipants(
            previous.InteractionContext,
            previousParticipants
        );
        BattleSceneExecutionPresenter.CollectActionParticipants(
            current.InteractionContext,
            currentParticipants
        );
        BattleSceneExecutionPresenter.CollectPreviousOnlyParticipants(
            previousParticipants,
            currentParticipants,
            previousOnlyParticipants
        );
        return previousParticipants.Count == 2 &&
            currentParticipants.Count == 2 &&
            previousOnlyParticipants.Count == 2 &&
            ReferenceEquals(previousOnlyParticipants[0], previousParticipants[0]) &&
            ReferenceEquals(previousOnlyParticipants[1], previousParticipants[1]);
    }

    static bool VerifySharedCurrentActorIsNotPreviousOnly()
    {
        CharacterData attacker = CreateActor("mode98_h_attacker");
        CharacterData priorTarget = CreateActor("mode98_h_prior_target");
        CharacterData currentTarget = CreateActor("mode98_h_current_target");
        BattlePresentationInteractionContext previous = CreateInteraction(
            CreateAction(attacker, priorTarget, CardType.Attack,
                AttackDeliveryMode.Melee, "mode98_h_previous_attack"),
            CreateAction(priorTarget, attacker, CardType.Defense,
                null, "mode98_h_previous_defense"),
            false
        );
        BattlePresentationInteractionContext current = CreateInteraction(
            CreateAction(attacker, currentTarget, CardType.Attack,
                AttackDeliveryMode.Melee, "mode98_h_current_attack"),
            CreateAction(currentTarget, attacker, CardType.Dodge,
                null, "mode98_h_current_dodge"),
            false
        );
        var previousParticipants = new List<CharacterData>();
        var currentParticipants = new List<CharacterData>();
        var previousOnlyParticipants = new List<CharacterData>();
        BattleSceneExecutionPresenter.CollectActionParticipants(
            previous, previousParticipants);
        BattleSceneExecutionPresenter.CollectActionParticipants(
            current, currentParticipants);
        BattleSceneExecutionPresenter.CollectPreviousOnlyParticipants(
            previousParticipants, currentParticipants, previousOnlyParticipants);
        return previousOnlyParticipants.Count == 1 &&
            ReferenceEquals(previousOnlyParticipants[0], priorTarget) &&
            !previousOnlyParticipants.Contains(attacker);
    }

    static bool VerifyCurrentDefenseUsesIdleDirective()
    {
        BattlePresentationRoute route = CreateDirectionalRoute(
            true,
            CardType.Defense,
            AttackDeliveryMode.Melee,
            false
        );
        BattlePresentationReadyContract ready =
            BattlePresentationReadyPolicy.Create(route);
        return IsReady(
            ready.Secondary,
            route.InteractionContext.DefenseAction,
            BattlePresentationReadyPoseKind.Idle
        );
    }

    static bool VerifyContinuousDodgeRemainsCurrentParticipant()
    {
        CharacterData attacker = CreateActor("mode98_j_attacker");
        CharacterData dodger = CreateActor("mode98_j_dodger");
        BattlePresentationInteractionContext previous = CreateInteraction(
            CreateAction(attacker, dodger, CardType.Attack,
                AttackDeliveryMode.Melee, "mode98_j_previous_attack"),
            CreateAction(dodger, attacker, CardType.Dodge,
                null, "mode98_j_previous_dodge"),
            true
        );
        BattlePresentationInteractionContext current = CreateInteraction(
            CreateAction(attacker, dodger, CardType.Attack,
                AttackDeliveryMode.Melee, "mode98_j_current_attack"),
            CreateAction(dodger, attacker, CardType.Dodge,
                null, "mode98_j_current_dodge"),
            true
        );
        BattlePresentationReadyContract ready = BattlePresentationReadyPolicy.Create(
            CreateRouteFromInteraction(current)
        );
        var previousParticipants = new List<CharacterData>();
        var currentParticipants = new List<CharacterData>();
        var previousOnlyParticipants = new List<CharacterData>();
        BattleSceneExecutionPresenter.CollectActionParticipants(
            previous, previousParticipants);
        BattleSceneExecutionPresenter.CollectActionParticipants(
            current, currentParticipants);
        BattleSceneExecutionPresenter.CollectPreviousOnlyParticipants(
            previousParticipants, currentParticipants, previousOnlyParticipants);
        return previousOnlyParticipants.Count == 0 &&
            ready.Secondary.PreserveCurrentPose &&
            ReferenceEquals(ready.Secondary.Action, current.DodgeAction) &&
            ReferenceEquals(ready.Secondary.Actor, current.DodgeAction.actor);
    }

    static bool VerifyExecutionClosureClearsParticipantTracking()
    {
        var previousParticipants = new List<CharacterData>
        {
            CreateActor("mode98_l_previous")
        };
        var currentParticipants = new List<CharacterData>
        {
            CreateActor("mode98_l_current")
        };
        var previousOnlyParticipants = new List<CharacterData>
        {
            CreateActor("mode98_l_previous_only")
        };
        BattleSceneExecutionPresenter.ClearActionParticipantTracking(
            previousParticipants,
            currentParticipants,
            previousOnlyParticipants
        );
        return previousParticipants.Count == 0 &&
            currentParticipants.Count == 0 &&
            previousOnlyParticipants.Count == 0;
    }

    static bool IsReady(
        BattlePresentationReadyDirective directive,
        BattleExecutionAction expectedAction,
        BattlePresentationReadyPoseKind expectedPose
    )
    {
        return directive != null && directive.ShouldApplyReady &&
            ReferenceEquals(directive.Action, expectedAction) &&
            ReferenceEquals(directive.Actor, expectedAction.actor) &&
            directive.PoseKind == expectedPose;
    }

    static bool IsActorReady(
        BattlePresentationReadyDirective directive,
        CharacterData expectedActor,
        BattlePresentationReadyPoseKind expectedPose
    )
    {
        return directive != null && directive.ShouldApplyReady &&
            ReferenceEquals(directive.Actor, expectedActor) &&
            directive.PoseKind == expectedPose;
    }

    static bool IsPreservedDodge(
        BattlePresentationReadyDirective directive,
        BattleExecutionAction expectedAction
    )
    {
        return directive != null &&
            ReferenceEquals(directive.Action, expectedAction) &&
            ReferenceEquals(directive.Actor, expectedAction.actor) &&
            directive.PoseKind == BattlePresentationReadyPoseKind.Dodge &&
            directive.PreserveCurrentPose &&
            !directive.ShouldApplyReady;
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

    static CharacterData CreateActor(string characterID)
    {
        return new CharacterData(characterID, 30, 5, 5);
    }

    static BattlePresentationInteractionContext CreateInteraction(
        BattleExecutionAction sideA,
        BattleExecutionAction sideB,
        bool preserveDodge
    )
    {
        BattleExecutionInteractionContext executionContext =
            new BattleExecutionInteractionContext(null, sideA, sideB);
        BattlePresentationInteractionContextFactory.TryCreate(
            executionContext,
            preserveDodge,
            out BattlePresentationInteractionContext interactionContext
        );
        return interactionContext;
    }

    static BattlePresentationRoute CreateRouteFromInteraction(
        BattlePresentationInteractionContext interactionContext
    )
    {
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
