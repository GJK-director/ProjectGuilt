// 脚本中文说明：验证 Runner 以 Effective Interaction 决定通用可暂停阶段。
using System.Collections.Generic;
using UnityEngine;

public static class BattleGenericPausableRoutingTests
{
    public static bool Run()
    {
        BattlePresentationInteractionContext attackVsAttack = CreateContext(
            CardType.Attack,
            AttackDeliveryMode.Melee,
            CardType.Attack,
            AttackDeliveryMode.Melee
        );
        BattlePresentationInteractionContext playerAttackEnemyDefense =
            CreateContext(
                CardType.Attack,
                AttackDeliveryMode.Melee,
                CardType.Defense,
                null
            );
        BattlePresentationInteractionContext enemyAttackPlayerDefense =
            CreateContext(
                CardType.Defense,
                null,
                CardType.Attack,
                AttackDeliveryMode.Melee
            );
        BattlePresentationInteractionContext playerAttackEnemyDodge =
            CreateContext(
                CardType.Attack,
                AttackDeliveryMode.Melee,
                CardType.Dodge,
                null
            );
        BattlePresentationInteractionContext enemyAttackPlayerDodge =
            CreateContext(
                CardType.Dodge,
                null,
                CardType.Attack,
                AttackDeliveryMode.Melee
            );
        BattlePresentationInteractionContext unilateral = CreateContext(
            CardType.Attack,
            AttackDeliveryMode.Melee,
            null,
            null
        );

        bool[] results =
        {
            VerifyClashRequirements(attackVsAttack),
            VerifyDirectionalClash(
                enemyAttackPlayerDefense,
                BattleInteractionType.AttackVsDefense
            ),
            VerifyDirectionalClash(
                playerAttackEnemyDefense,
                BattleInteractionType.AttackVsDefense
            ),
            VerifyDirectionalClash(
                enemyAttackPlayerDodge,
                BattleInteractionType.AttackVsDodge
            ),
            VerifyDirectionalClash(
                playerAttackEnemyDodge,
                BattleInteractionType.AttackVsDodge
            ),
            VerifyRuntimeGuardOverride(CardType.Defense, false),
            VerifyRuntimeGuardOverride(CardType.Dodge, true),
            VerifyUnilateralRunnerBegin(true, AttackDeliveryMode.Melee, true),
            VerifyUnilateralRunnerBegin(
                true,
                AttackDeliveryMode.CloseRangeShoot,
                true
            ),
            VerifyUnilateralRunnerBegin(
                true,
                AttackDeliveryMode.LongRangeShoot,
                true
            ),
            VerifyUnilateralRunnerBegin(false, AttackDeliveryMode.Melee, true),
            VerifyUnilateralRunnerBegin(
                false,
                AttackDeliveryMode.CloseRangeShoot,
                true
            ),
            VerifyUnilateralRunnerBegin(
                false,
                AttackDeliveryMode.LongRangeShoot,
                true
            ),
            VerifyLongRangeNoResourceDoesNotPresent(),
            VerifyNoInteractionHasNoPhases(),
            VerifyAbilityDoesNotEnterCombatPresentation(),
            VerifyUnilateralRequirements(unilateral),
            VerifyReadyWithoutApproach(unilateral),
            VerifyContinuousDodgeMetadata(),
            VerifyRunnerFreezesContextAcrossRequests()
        };
        string[] names =
        {
            "AttackVsAttack保留Manual Roll Contract",
            "Enemy Attack + Player Defense进入Pausable",
            "Player Attack + Enemy Defense进入Pausable",
            "Enemy Attack + Player Dodge进入Pausable",
            "Player Attack + Enemy Dodge进入Pausable",
            "Passive Defense先选择再判定Effective",
            "Continuous Dodge通过Effective进入Pausable",
            "Player Melee Unilateral进入Pausable",
            "Player CloseRange Unilateral进入Pausable",
            "Player LongRange Unilateral资源充足",
            "Enemy Melee Unilateral进入Pausable",
            "Enemy CloseRange Unilateral进入Pausable",
            "Enemy LongRange Unilateral资源充足",
            "LongRange NoResource不开始成功Presentation",
            "NoInteraction无Presentation Phases",
            "Ability不进入Generic Combat Presentation",
            "Unilateral有Phase但无Clash/Manual Roll",
            "RequiresApproach=false仍有ActionBegin",
            "Continuous Dodge保留PreserveDodgePose",
            "同一Engagement的Request共用冻结Context"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式96 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }

        Debug.Log("模式96 Generic Pausable Routing聚合结果：" + allPassed);
        return allPassed;
    }

    static bool VerifyClashRequirements(
        BattlePresentationInteractionContext context
    )
    {
        BattleExecutionPhaseRequirements requirements =
            BattleExecutionPausablePolicy.Evaluate(context);
        return requirements.InteractionType ==
                BattleInteractionType.AttackVsAttack &&
            requirements.HasPresentationPhases &&
            requirements.RequiresActionBegin &&
            requirements.RequiresRollResult &&
            requirements.RequiresManualRoll &&
            requirements.RequiresImpact &&
            requirements.RequiresActionComplete &&
            requirements.RequiresClashSession;
    }

    static bool VerifyDirectionalClash(
        BattlePresentationInteractionContext context,
        BattleInteractionType expectedType
    )
    {
        BattleExecutionPhaseRequirements requirements =
            BattleExecutionPausablePolicy.Evaluate(context);
        return context != null && context.InteractionType == expectedType &&
            context.AttackAction != null && requirements.HasPresentationPhases &&
            requirements.RequiresManualRoll && requirements.RequiresClashSession;
    }

    static bool VerifyRuntimeGuardOverride(
        string responseCardType,
        bool continuousDodge
    )
    {
        CharacterData target = CreateCharacter("mode96_guard_target");
        CharacterData enemy = CreateCharacter("mode96_guard_enemy");
        BattleCardState enemyAttack = CreateCard(
            enemy,
            "mode96_guard_enemy_attack",
            CardType.Attack,
            AttackDeliveryMode.Melee,
            4
        );
        BattleEnemyIntent intent = new BattleEnemyIntent(
            "mode96_guard_intent",
            enemy,
            enemyAttack,
            target,
            1,
            1
        );
        BattleExecutionItem item = new BattleExecutionItem(
            1,
            BattleExecutionItemType.UnrespondedEnemyIntent,
            intent,
            null
        );
        item.interactionType = BattleInteractionType.UnilateralAttack;

        BattleActionSlot guardSlot = new BattleActionSlot(target, 1);
        guardSlot.AssignPassiveGuard(
            target,
            CreateCard(
                target,
                "mode96_guard_response",
                responseCardType,
                null,
                7
            )
        );
        if (continuousDodge)
        {
            guardSlot.ActivateContinuousDodge(
                ContinuousDodgeSource.PassiveGuard,
                7,
                enemy
            );
        }

        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetActionSlots(new List<BattleActionSlot> { guardSlot });
        bool built = BattleExecutionPlanExecutor.TryBuildPausableRoutingContext(
            item,
            runtimeState,
            out BattleActionSlot selectedSlot,
            out BattleGuardSelectionType selectionType,
            out BattleExecutionInteractionContext executionContext,
            out BattlePresentationInteractionContext presentationContext
        );
        BattleInteractionType expectedType = responseCardType == CardType.Defense
            ? BattleInteractionType.AttackVsDefense
            : BattleInteractionType.AttackVsDodge;
        BattleExecutionPhaseRequirements requirements =
            BattleExecutionPausablePolicy.Evaluate(presentationContext);

        return built && object.ReferenceEquals(selectedSlot, guardSlot) &&
            selectionType == (continuousDodge
                ? BattleGuardSelectionType.ContinuousDodge
                : BattleGuardSelectionType.PassiveGuard) &&
            item.interactionType == BattleInteractionType.UnilateralAttack &&
            executionContext.effectiveInteractionType == expectedType &&
            presentationContext.InteractionType == expectedType &&
            requirements.HasPresentationPhases &&
            presentationContext.ContinuationPolicy == (continuousDodge
                ? BattlePresentationContinuationPolicy.PreserveDodgePose
                : BattlePresentationContinuationPolicy.NewEngagement);
    }

    static bool VerifyUnilateralRunnerBegin(
        bool playerSource,
        string deliveryMode,
        bool resourceAvailable
    )
    {
        RunnerFixture fixture = CreateUnilateralRunnerFixture(
            "mode96_unilateral_" + playerSource + "_" + deliveryMode,
            playerSource,
            deliveryMode,
            resourceAvailable
        );
        bool began = BeginRunner(fixture);
        BattleExecutionRunner runner = fixture.controller.ExecutionRunner;
        BattlePresentationRequest request = fixture.presenter.GetLastRequest();

        return began && runner != null && !runner.HasFailed &&
            runner.Phase == BattleExecutionRunnerPhase.WaitingForPresentation &&
            request != null && request.Cue == BattlePresentationCue.ActionBegin &&
            request.InteractionContext != null &&
            request.InteractionContext.InteractionType ==
                BattleInteractionType.UnilateralAttack &&
            runner.CurrentClashSession == null && !runner.IsWaitingForInput &&
            runner.CurrentPhaseRequirements.HasPresentationPhases &&
            !runner.CurrentPhaseRequirements.RequiresManualRoll &&
            !runner.CurrentPhaseRequirements.RequiresClashSession;
    }

    static bool VerifyLongRangeNoResourceDoesNotPresent()
    {
        RunnerFixture fixture = CreateUnilateralRunnerFixture(
            "mode96_no_resource",
            true,
            AttackDeliveryMode.LongRangeShoot,
            false
        );
        int hpBefore = fixture.target.currentHP;
        bool began = BeginRunner(fixture);

        return began && fixture.presenter.Requests.Count == 0 &&
            fixture.item.isCompleted &&
            fixture.item.status == BattleExecutionItemStatus.Skipped &&
            fixture.item.outcomeReason ==
                BattleExecutionItemOutcomeReason.ActionUnavailable &&
            fixture.target.currentHP == hpBefore &&
            fixture.attackCard.currentCooldown == 0;
    }

    static bool VerifyNoInteractionHasNoPhases()
    {
        BattlePresentationInteractionContext context = CreateContext(
            CardType.Defense,
            null,
            CardType.Dodge,
            null
        );
        BattleExecutionPhaseRequirements requirements =
            BattleExecutionPausablePolicy.Evaluate(context);
        return context == null && !requirements.HasPresentationPhases &&
            !requirements.RequiresActionBegin &&
            !requirements.RequiresRollResult &&
            !requirements.RequiresImpact &&
            !requirements.RequiresActionComplete;
    }

    static bool VerifyAbilityDoesNotEnterCombatPresentation()
    {
        BattlePresentationInteractionContext context = CreateContext(
            "Ability",
            null,
            null,
            null
        );
        return context == null &&
            !BattleExecutionPausablePolicy.Evaluate(context)
                .HasPresentationPhases;
    }

    static bool VerifyUnilateralRequirements(
        BattlePresentationInteractionContext context
    )
    {
        BattleExecutionPhaseRequirements requirements =
            BattleExecutionPausablePolicy.Evaluate(context);
        return requirements.HasPresentationPhases &&
            requirements.RequiresActionBegin &&
            !requirements.RequiresRollResult &&
            !requirements.RequiresManualRoll &&
            requirements.RequiresImpact &&
            requirements.RequiresActionComplete &&
            !requirements.RequiresClashSession;
    }

    static bool VerifyReadyWithoutApproach(
        BattlePresentationInteractionContext context
    )
    {
        BattleExecutionPhaseRequirements requirements =
            BattleExecutionPausablePolicy.Evaluate(context);
        BattlePresentationPhaseContract phase =
            context.CreateActionBeginPhaseContract(false);
        return requirements.RequiresActionBegin && !phase.RequiresApproach &&
            phase.RequiresReadyPose && !phase.PreservePreviousPose;
    }

    static bool VerifyContinuousDodgeMetadata()
    {
        CharacterData attacker = CreateCharacter("mode96_continuous_attacker");
        CharacterData dodger = CreateCharacter("mode96_continuous_dodger");
        BattleExecutionInteractionContext executionContext =
            new BattleExecutionInteractionContext(
                null,
                CreateAction(
                    attacker,
                    dodger,
                    CardType.Attack,
                    AttackDeliveryMode.Melee,
                    "mode96_continuous_attack"
                ),
                CreateAction(
                    dodger,
                    attacker,
                    CardType.Dodge,
                    null,
                    "mode96_continuous_dodge"
                )
            );
        bool created = BattlePresentationInteractionContextFactory.TryCreate(
            executionContext,
            true,
            out BattlePresentationInteractionContext context
        );
        BattlePresentationPhaseContract phase = created
            ? context.CreateActionBeginPhaseContract(false)
            : null;

        return created && context.ContinuationPolicy ==
                BattlePresentationContinuationPolicy.PreserveDodgePose &&
            BattleExecutionPausablePolicy.Evaluate(context)
                .RequiresActionBegin &&
            phase != null && phase.PreservePreviousPose &&
            phase.RequiresReadyPose;
    }

    static bool VerifyRunnerFreezesContextAcrossRequests()
    {
        RunnerFixture fixture = CreateRespondedAttackRunnerFixture(
            "mode96_context_freeze"
        );
        if (!BeginRunner(fixture))
        {
            return false;
        }

        BattlePresentationRequest actionBegin = fixture.presenter.GetLastRequest();
        if (!CompleteAndAdvance(fixture, actionBegin) ||
            !fixture.controller.TryRequestManualRoll(out string rollFailure) ||
            !string.IsNullOrEmpty(rollFailure))
        {
            return false;
        }

        BattlePresentationRequest rollResult = fixture.presenter.GetLastRequest();
        if (!CompleteAndAdvance(fixture, rollResult) ||
            !Advance(fixture))
        {
            return false;
        }

        BattlePresentationRequest impact = fixture.presenter.GetLastRequest();
        if (!CompleteAndAdvance(fixture, impact))
        {
            return false;
        }

        BattlePresentationRequest actionComplete =
            fixture.presenter.GetLastRequest();
        BattlePresentationInteractionContext frozen =
            actionBegin != null ? actionBegin.InteractionContext : null;

        return frozen != null && actionBegin.Cue == BattlePresentationCue.ActionBegin &&
            rollResult != null && rollResult.Cue == BattlePresentationCue.RollResult &&
            impact != null && impact.Cue == BattlePresentationCue.Impact &&
            actionComplete != null &&
            actionComplete.Cue == BattlePresentationCue.ActionComplete &&
            object.ReferenceEquals(frozen, rollResult.InteractionContext) &&
            object.ReferenceEquals(frozen, impact.InteractionContext) &&
            object.ReferenceEquals(frozen, actionComplete.InteractionContext) &&
            object.ReferenceEquals(
                frozen,
                fixture.controller.ExecutionRunner
                    .CurrentPresentationInteractionContext
            ) &&
            frozen.InteractionType == BattleInteractionType.AttackVsAttack &&
            object.ReferenceEquals(
                frozen.AttackActionA,
                actionBegin.InteractionContext.AttackActionA
            ) &&
            object.ReferenceEquals(
                frozen.AttackActionB,
                actionBegin.InteractionContext.AttackActionB
            );
    }

    static BattlePresentationInteractionContext CreateContext(
        string sideACardType,
        string sideADeliveryMode,
        string sideBCardType,
        string sideBDeliveryMode
    )
    {
        CharacterData sideAActor = CreateCharacter("mode96_context_side_a");
        CharacterData sideBActor = CreateCharacter("mode96_context_side_b");
        BattleExecutionAction sideA = sideACardType != null
            ? CreateAction(
                sideAActor,
                sideBActor,
                sideACardType,
                sideADeliveryMode,
                "mode96_context_side_a_card"
            )
            : null;
        BattleExecutionAction sideB = sideBCardType != null
            ? CreateAction(
                sideBActor,
                sideAActor,
                sideBCardType,
                sideBDeliveryMode,
                "mode96_context_side_b_card"
            )
            : null;
        BattleExecutionInteractionContext executionContext =
            new BattleExecutionInteractionContext(null, sideA, sideB);
        BattlePresentationInteractionContextFactory.TryCreate(
            executionContext,
            false,
            out BattlePresentationInteractionContext presentationContext
        );
        return presentationContext;
    }

    static RunnerFixture CreateUnilateralRunnerFixture(
        string id,
        bool playerSource,
        string deliveryMode,
        bool resourceAvailable
    )
    {
        CharacterData ally = CreateCharacter(id + "_ally");
        CharacterData allyB = CreateCharacter(id + "_ally_b");
        CharacterData enemy = CreateCharacter(id + "_enemy");
        CharacterData attacker = playerSource ? ally : enemy;
        CharacterData target = playerSource ? enemy : ally;
        BattleCardState attackCard = CreateCard(
            attacker,
            id + "_attack",
            CardType.Attack,
            deliveryMode,
            6
        );
        if (deliveryMode == AttackDeliveryMode.LongRangeShoot)
        {
            ConfigureResource(
                attacker,
                attackCard,
                id + "_bullet",
                resourceAvailable ? 1 : 0
            );
        }

        BattleActionSlot actionSlot = null;
        BattleEnemyIntent intent = null;
        BattleExecutionItem item;
        if (playerSource)
        {
            actionSlot = new BattleActionSlot(attacker, 1);
            actionSlot.AssignFreeAction(attacker, attackCard, target);
            item = new BattleExecutionItem(
                1,
                BattleExecutionItemType.FreeAction,
                null,
                actionSlot
            );
        }
        else
        {
            intent = new BattleEnemyIntent(
                id + "_intent",
                attacker,
                attackCard,
                target,
                1,
                1
            );
            item = new BattleExecutionItem(
                1,
                BattleExecutionItemType.UnrespondedEnemyIntent,
                intent,
                null
            );
        }
        item.interactionType = BattleInteractionType.UnilateralAttack;

        return CreateRunnerFixture(
            ally,
            allyB,
            enemy,
            item,
            actionSlot != null
                ? new List<BattleActionSlot> { actionSlot }
                : new List<BattleActionSlot>(),
            intent != null
                ? new List<BattleEnemyIntent> { intent }
                : new List<BattleEnemyIntent>(),
            attackCard,
            target
        );
    }

    static RunnerFixture CreateRespondedAttackRunnerFixture(string id)
    {
        CharacterData ally = CreateCharacter(id + "_ally");
        CharacterData allyB = CreateCharacter(id + "_ally_b");
        CharacterData enemy = CreateCharacter(id + "_enemy");
        BattleCardState allyAttack = CreateCard(
            ally,
            id + "_ally_attack",
            CardType.Attack,
            AttackDeliveryMode.Melee,
            8
        );
        BattleCardState enemyAttack = CreateCard(
            enemy,
            id + "_enemy_attack",
            CardType.Attack,
            AttackDeliveryMode.Melee,
            3
        );
        BattleEnemyIntent intent = new BattleEnemyIntent(
            id + "_intent",
            enemy,
            enemyAttack,
            ally,
            1,
            1
        );
        BattleActionSlot slot = new BattleActionSlot(ally, 1);
        slot.AssignResponse(ally, allyAttack, intent, false);
        BattleExecutionItem item = new BattleExecutionItem(
            1,
            BattleExecutionItemType.RespondedEnemyIntent,
            intent,
            slot
        );
        item.interactionType = BattleInteractionType.AttackVsAttack;

        return CreateRunnerFixture(
            ally,
            allyB,
            enemy,
            item,
            new List<BattleActionSlot> { slot },
            new List<BattleEnemyIntent> { intent },
            allyAttack,
            enemy
        );
    }

    static RunnerFixture CreateRunnerFixture(
        CharacterData ally,
        CharacterData allyB,
        CharacterData enemy,
        BattleExecutionItem item,
        List<BattleActionSlot> actionSlots,
        List<BattleEnemyIntent> intents,
        BattleCardState attackCard,
        CharacterData target
    )
    {
        BattleExecutionPlan plan = new BattleExecutionPlan();
        plan.AddItem(item);
        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(ally, allyB, enemy);
        runtimeState.SetActionSlots(actionSlots);
        runtimeState.SetIntentQueue(intents);
        runtimeState.SetExecutionPlan(plan);
        BattleConsolePresenter presenter = new BattleConsolePresenter(false);
        BattleLifecycleController controller = new BattleLifecycleController(
            runtimeState,
            presenter
        );
        bool initialized = controller.TryInitializeToPrepare(
            out string initializeFailure
        );
        if (!initialized)
        {
            Debug.LogWarning("Mode96 Runner初始化失败：" + initializeFailure);
        }

        return new RunnerFixture
        {
            controller = controller,
            presenter = presenter,
            item = item,
            attackCard = attackCard,
            target = target,
            initialized = initialized
        };
    }

    static bool BeginRunner(RunnerFixture fixture)
    {
        return fixture != null && fixture.initialized &&
            fixture.controller.TryBeginPausableExecution(
                new BattleRollGateSettings(BattleRollMode.Manual, 0f, 0f),
                out string failureMessage
            ) &&
            string.IsNullOrEmpty(failureMessage);
    }

    static bool CompleteAndAdvance(
        RunnerFixture fixture,
        BattlePresentationRequest request
    )
    {
        return fixture != null && request != null &&
            fixture.presenter.TryCompleteRequest(request.RequestId) &&
            Advance(fixture);
    }

    static bool Advance(RunnerFixture fixture)
    {
        return fixture.controller.AdvancePausableExecution(
                0f,
                out string failureMessage
            ) &&
            string.IsNullOrEmpty(failureMessage);
    }

    static BattleExecutionAction CreateAction(
        CharacterData actor,
        CharacterData target,
        string cardType,
        string deliveryMode,
        string id
    )
    {
        return new BattleExecutionAction(
            actor,
            CreateCard(actor, id, cardType, deliveryMode, 6),
            null,
            null,
            target
        );
    }

    static BattleCardState CreateCard(
        CharacterData owner,
        string id,
        string cardType,
        string deliveryMode,
        int point
    )
    {
        CardTestData data = new CardTestData
        {
            cardID = id + "_data",
            cardName = id,
            cardType = cardType,
            attackDeliveryMode = cardType == CardType.Attack
                ? deliveryMode
                : string.Empty,
            isClashable = cardType == CardType.Attack ||
                cardType == CardType.Defense ||
                cardType == CardType.Dodge,
            minPoint = point,
            maxPoint = point,
            cooldown = 2,
            damageFormula = cardType == CardType.Attack
                ? "PointAsDamage"
                : string.Empty,
            defenseFormula = cardType == CardType.Defense
                ? "PointAsDefense"
                : string.Empty,
            effects = new List<CardEffectData>()
        };
        return BattleCardManager.CreateBattleCard(owner, data, id + "_instance");
    }

    static void ConfigureResource(
        CharacterData actor,
        BattleCardState card,
        string resourceID,
        int initialStack
    )
    {
        if (initialStack > 0)
        {
            actor.AddBuff(
                resourceID,
                resourceID,
                BuffCategory.AbilityBuff,
                initialStack,
                -1,
                BattleTiming.TurnEnd,
                BuffExpireRule.Permanent
            );
        }

        card.cardData.resourceRule = new CardResourceRuleData
        {
            resourceType = "BuffStack",
            resourceID = resourceID,
            requiredStackForNormalVersion = 1,
            consumeAmountOnSuccess = 1,
            fallbackMinPoint = 0,
            fallbackMaxPoint = 0
        };
    }

    static CharacterData CreateCharacter(string id)
    {
        return new CharacterData(id, 30, 5, 5);
    }

    sealed class RunnerFixture
    {
        public BattleLifecycleController controller;
        public BattleConsolePresenter presenter;
        public BattleExecutionItem item;
        public BattleCardState attackCard;
        public CharacterData target;
        public bool initialized;
    }
}
