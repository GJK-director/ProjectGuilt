// 脚本中文说明：组合正式数据与Phase1-6契约，验证BattleScene运行链的数据侧接线。
using System.Collections.Generic;
using UnityEngine;

public static class FullBattleIntegrationRegressionTests
{
    private const string ProductionEncounterID = "encounter_test_001";

    public static bool Run()
    {
        ProductionFixture production = CreateProductionFixture();
        bool[] results =
        {
            VerifyProductionCharacterBootstrap(production),
            VerifyProductionEnemyBootstrap(production),
            VerifyProductionCardOwnership(production),
            VerifyProductionMultiSlotIntents(production),
            VerifyAttackVsAttack(production),
            VerifyDirectionalInteraction(CardType.Defense),
            VerifyDirectionalInteraction(CardType.Dodge),
            VerifyUnilateralBothDirections(),
            VerifyNoInteractionExecution(),
            VerifyNoInteraction(CardType.Defense, CardType.Dodge),
            VerifyNoInteraction(CardType.Dodge, CardType.Dodge),
            VerifyFirstStrikeTierOrdering(),
            VerifyProductionLongRangeIsNormalTier(production),
            VerifyProductionLongRangeWithBullet(production),
            VerifyProductionLongRangeNoBullet(production),
            VerifyAttackVsDefenseFullBlockLifecycle(),
            VerifyAttackVsDodgeSuccessLifecycle(),
            VerifyContinuousDodgePerActorReady(),
            VerifyUnilateralHasNoClashSession(),
            VerifyEnemySlot2Descriptor(production),
            VerifyResponseBindsEnemySlot2(),
            VerifyProductionPresentationRequirements(production),
            VerifyCampOnlyChangesFacing(production),
            VerifyProductionDataToBindingContract(production),
            VerifyTurnEndClosingPushesIntoRecoveryPose(),
            VerifyNewTurnOpeningPullsBackToDefaultPose()
        };

        string[] names =
        {
            "Production Character Definition可建立正式Runtime Ally",
            "Production Enemy Definition可建立正式Runtime Enemy",
            "Production Card ownership与Definition引用一致",
            "Enemy Turn1/Turn2均保持Slot1 Attack/Slot2 Defense",
            "Production Melee/Melee归类AttackVsAttack",
            "AttackVsDefense两个方向归一为同一Interaction",
            "AttackVsDodge两个方向归一为同一Interaction",
            "Player/Enemy Unilateral共用UnilateralAttack",
            "Defense/Defense执行为NoInteraction且无生命周期提交",
            "Defense/Dodge归类NoInteraction",
            "Dodge/Dodge归类NoInteraction",
            "FirstStrike完整Item优先且不拆Pairing",
            "Production LongRange无FirstStrike时保持Normal",
            "Production LongRange有Bullet时完成单方攻击生命周期",
            "Production LongRange无Bullet时ActionUnavailable",
            "AttackVsDefense FullBlock仍提交Attack生命周期",
            "AttackVsDodge Success仍提交Attack生命周期",
            "Continuous Dodge只保留Dodger且新Attack进入Ready",
            "Unilateral不创建ClashSession或Manual Roll Gate",
            "Enemy Slot2 Descriptor保持Defense与Slot Identity",
            "Player Response精确绑定Enemy Slot2",
            "Production Card Capability推导Presentation Requirements",
            "Camp只改变Facing不改变Presentation Capability",
            "Production Definition到Binding Contract数据链完整",
            "TurnEnd Closing从Combat Terminal前缩到Recovery",
            "NewTurn Opening从Recovery后缩到Default"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式103 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }

        Debug.Log("模式103 Full Runtime Regression聚合结果：" + allPassed);
        Debug.Log(
            "模式103只证明数据侧Integration Contract；" +
            "BattleScene、Prefab Inspector与实际UI/Presentation仍需手动Runtime验收。"
        );
        return allPassed;
    }

    private static bool VerifyTurnEndClosingPushesIntoRecoveryPose()
    {
        const float combatTerminalRadius = 9.5f;
        const float recoveryRadius = 8.5f;
        float endRadius = Mathf.Lerp(combatTerminalRadius, recoveryRadius, 1f);

        return Mathf.Approximately(endRadius, recoveryRadius) &&
            endRadius < combatTerminalRadius;
    }

    private static bool VerifyNewTurnOpeningPullsBackToDefaultPose()
    {
        const float recoveryRadius = 8.5f;
        const float defaultRadius = 11.5f;
        float startRadius = Mathf.Lerp(recoveryRadius, defaultRadius, 0f);
        float endRadius = Mathf.Lerp(recoveryRadius, defaultRadius, 1f);

        return Mathf.Approximately(startRadius, recoveryRadius) &&
            Mathf.Approximately(endRadius, defaultRadius) &&
            endRadius > startRadius;
    }

    private static bool VerifyProductionCharacterBootstrap(
        ProductionFixture fixture
    )
    {
        return fixture.IsValid && fixture.runtime.allyUnits.Count == 1 &&
            fixture.runtime.allyA != null && fixture.allyDefinition != null &&
            fixture.runtime.allyA.runtimeUnitID ==
                fixture.allyDefinition.characterID &&
            fixture.runtime.LifecyclePhase == BattleLifecyclePhase.Prepare;
    }

    private static bool VerifyProductionEnemyBootstrap(
        ProductionFixture fixture
    )
    {
        return fixture.IsValid && fixture.runtime.enemyUnits.Count == 1 &&
            fixture.runtime.enemy != null && fixture.enemyDefinition != null &&
            fixture.runtime.enemy.runtimeUnitID == fixture.enemyDefinition.enemyID &&
            fixture.enemyDefinition.cardIDs != null &&
            fixture.enemyDefinition.cardIDs.Length == 2;
    }

    private static bool VerifyProductionCardOwnership(
        ProductionFixture fixture
    )
    {
        return fixture.IsValid && MatchesCardReferences(
                fixture.runtime.allyA.battleCards,
                fixture.allyDefinition.startingCardIDs
            ) && MatchesCardReferences(
                fixture.runtime.enemy.battleCards,
                fixture.enemyDefinition.cardIDs
            );
    }

    private static bool VerifyProductionMultiSlotIntents(
        ProductionFixture fixture
    )
    {
        if (!fixture.IsValid || fixture.runtime.intentQueue.Count != 2)
        {
            return false;
        }

        BattleEnemyIntent first = fixture.runtime.intentQueue[0];
        BattleEnemyIntent second = fixture.runtime.intentQueue[1];
        bool turnOneCorrect =
            IsIntent(first, 1, CardType.Attack, "enemy_atk_001") &&
            IsIntent(second, 2, CardType.Defense, "def_001") &&
            fixture.bootstrap.encounterDefinition.repeatIntentPattern;
        if (!turnOneCorrect)
        {
            return false;
        }

        ProductionFixture nextTurnFixture = CreateProductionFixture();
        if (!nextTurnFixture.IsValid)
        {
            return false;
        }

        BattleRuntimeState runtime = nextTurnFixture.runtime;
        BattleExecutionPlan completedPlan = new BattleExecutionPlan
        {
            isCompleted = true
        };
        runtime.SetExecutionPlan(completedPlan);
        string transitionFailure;
        if (!runtime.TryTransitionTo(
                BattleLifecyclePhase.Executing,
                out transitionFailure) ||
            !runtime.TryTransitionTo(
                BattleLifecyclePhase.TurnResolved,
                out transitionFailure))
        {
            return false;
        }

        int providerCallCount = 0;
        int requestedTurn = 0;
        bool oldSlotsWereCleared = false;
        bool receivedNewSlots = false;
        BattleNextTurnIntentQueueProvider provider = delegate(
            int nextTurnNumber,
            List<BattleActionSlot> targetActionSlots,
            out List<BattleEnemyIntent> intentQueue,
            out string failureMessage)
        {
            providerCallCount++;
            requestedTurn = nextTurnNumber;
            oldSlotsWereCleared = runtime.actionSlots != null &&
                runtime.actionSlots.Count == 0;
            receivedNewSlots = targetActionSlots != null &&
                targetActionSlots.Count > 0;

            BattleDefinitionIntentQueueResult intentResult =
                BattleDefinitionBootstrap.CreateIntentQueueForTurn(
                    runtime,
                    nextTurnFixture.bootstrap.encounterDefinition,
                    nextTurnFixture.enemyDefinition,
                    nextTurnFixture.bootstrap.allyByID,
                    nextTurnNumber,
                    targetActionSlots
                );
            if (intentResult == null || !intentResult.isSuccess ||
                intentResult.intentQueue == null)
            {
                intentQueue = null;
                failureMessage = intentResult != null
                    ? intentResult.errorMessage
                    : "Mode103 NextTurn Definition Builder未返回结果";
                return false;
            }

            intentQueue = intentResult.intentQueue;
            failureMessage = string.Empty;
            return true;
        };

        BattleLifecycleController lifecycle =
            new BattleLifecycleController(runtime);
        BattleAutomaticTurnCycleResult cycleResult =
            BattleAutomaticTurnCycle.CompleteTurnCycleAfterExecution(
                new BattleAutomaticTurnCycleResult
                {
                    startingTurn = runtime.currentTurn,
                    executedPlan = completedPlan
                },
                lifecycle,
                runtime,
                completedPlan,
                runtime.allyA,
                runtime.allyB,
                runtime.enemy,
                FindCard(runtime.enemy, "enemy_atk_001"),
                runtime.enemy2,
                null,
                provider
            );

        BattleNextTurnIntentQueueProvider rejectedProvider = delegate(
            int nextTurnNumber,
            List<BattleActionSlot> targetActionSlots,
            out List<BattleEnemyIntent> intentQueue,
            out string failureMessage)
        {
            intentQueue = null;
            failureMessage = "Mode103 Provider Failure";
            return false;
        };
        bool providerFailureDidNotFallback =
            !BattleAutomaticTurnCycle.TryCreateNextTurnIntentQueue(
                rejectedProvider,
                runtime,
                runtime.enemy,
                FindCard(runtime.enemy, "enemy_atk_001"),
                runtime.enemy2,
                null,
                runtime.allyA,
                runtime.allyB,
                runtime.actionSlots,
                out List<BattleEnemyIntent> rejectedQueue,
                out string rejectedFailure
            ) && rejectedQueue == null &&
            rejectedFailure == "Mode103 Provider Failure";

        return cycleResult != null && cycleResult.isSuccess &&
            cycleResult.advancedToNextTurn && providerCallCount == 1 &&
            requestedTurn == 2 && oldSlotsWereCleared && receivedNewSlots &&
            providerFailureDidNotFallback &&
            runtime.currentTurn == 2 && runtime.intentQueue.Count == 2 &&
            IsIntent(
                runtime.intentQueue[0],
                1,
                CardType.Attack,
                "enemy_atk_001") &&
            IsIntent(
                runtime.intentQueue[1],
                2,
                CardType.Defense,
                "def_001");
    }

    private static bool VerifyAttackVsAttack(ProductionFixture fixture)
    {
        BattleCardState allyAttack = FindCard(
            fixture.runtime?.allyA,
            "atk_001"
        );
        BattleCardState enemyAttack = FindCard(
            fixture.runtime?.enemy,
            "enemy_atk_001"
        );
        return BattleInteractionClassifier.Classify(
            allyAttack,
            enemyAttack
        ) == BattleInteractionType.AttackVsAttack;
    }

    private static bool VerifyDirectionalInteraction(string responseType)
    {
        CharacterData attacker = CreateCharacter("mode103_attacker");
        CharacterData responder = CreateCharacter("mode103_responder");
        BattleExecutionAction attack = CreateAction(
            attacker,
            responder,
            CreateCard(attacker, CardType.Attack, 5)
        );
        BattleExecutionAction response = CreateAction(
            responder,
            attacker,
            CreateCard(responder, responseType, 5)
        );
        BattleInteractionType expected = responseType == CardType.Defense
            ? BattleInteractionType.AttackVsDefense
            : BattleInteractionType.AttackVsDodge;

        return IsDirectionalContext(attack, response, expected) &&
            IsDirectionalContext(response, attack, expected);
    }

    private static bool VerifyUnilateralBothDirections()
    {
        CharacterData actor = CreateCharacter("mode103_unilateral_actor");
        CharacterData target = CreateCharacter("mode103_unilateral_target");
        BattleExecutionAction attack = CreateAction(
            actor,
            target,
            CreateCard(actor, CardType.Attack, 5)
        );
        return new BattleExecutionInteractionContext(
                null,
                attack,
                null
            ).effectiveInteractionType ==
                BattleInteractionType.UnilateralAttack &&
            new BattleExecutionInteractionContext(
                null,
                null,
                attack
            ).effectiveInteractionType ==
                BattleInteractionType.UnilateralAttack;
    }

    private static bool VerifyNoInteractionExecution()
    {
        CharacterData actor = CreateCharacter("mode103_no_interaction_actor");
        CharacterData target = CreateCharacter("mode103_no_interaction_target");
        BattleCardState defense = CreateCard(actor, CardType.Defense, 8);
        BattleActionSlot slot = new BattleActionSlot(actor, 1);
        slot.AssignFreeAction(actor, defense, target);
        BattleExecutionItem item = new BattleExecutionItem(
            1,
            BattleExecutionItemType.FreeAction,
            null,
            slot
        );
        item.interactionType = BattleInteractionType.NoInteraction;
        BattleExecutionPlan plan = new BattleExecutionPlan();
        plan.AddItem(item);
        int hpBefore = target.currentHP;

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(plan);
        return item.isCompleted && item.status == BattleExecutionItemStatus.Skipped &&
            item.outcomeReason == BattleExecutionItemOutcomeReason.NoInteraction &&
            slot.isUsed && defense.currentCooldown == 0 &&
            target.currentHP == hpBefore;
    }

    private static bool VerifyNoInteraction(string firstType, string secondType)
    {
        return BattleInteractionClassifier.Classify(
            CreateCardData(firstType, 1),
            CreateCardData(secondType, 1)
        ) == BattleInteractionType.NoInteraction;
    }

    private static bool VerifyFirstStrikeTierOrdering()
    {
        CharacterData ally = new CharacterData("mode103_slow_first", 30, 1, 1);
        CharacterData enemy = new CharacterData("mode103_fast_enemy", 30, 20, 20);
        BattleCardState response = CreateCard(
            ally,
            CardType.Attack,
            5,
            AttackDeliveryMode.Melee,
            true
        );
        BattleCardState enemyAttack = CreateCard(enemy, CardType.Attack, 5);
        BattleEnemyIntent intent = new BattleEnemyIntent(
            "mode103_firststrike_intent",
            enemy,
            enemyAttack,
            ally,
            1,
            1
        );
        BattleActionSlot responseSlot = new BattleActionSlot(ally, 1);
        responseSlot.AssignResponse(ally, response, intent, false);
        intent.MarkResponded();

        CharacterData normalActor = new CharacterData(
            "mode103_fast_normal",
            30,
            30,
            30
        );
        BattleActionSlot normalSlot = new BattleActionSlot(normalActor, 1);
        normalSlot.AssignFreeAction(
            normalActor,
            CreateCard(normalActor, CardType.Attack, 5),
            enemy
        );

        BattleExecutionPlan plan =
            BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
                new List<BattleActionSlot> { normalSlot, responseSlot },
                new List<BattleEnemyIntent> { intent }
            );
        return plan.executionItems.Count == 2 &&
            object.ReferenceEquals(plan.executionItems[0].actionSlot, responseSlot) &&
            object.ReferenceEquals(plan.executionItems[0].enemyIntent, intent) &&
            plan.executionItems[0].priorityTier ==
                BattleExecutionPriorityTier.FirstStrike &&
            plan.executionItems[0].interactionType ==
                BattleInteractionType.AttackVsAttack &&
            plan.executionItems[1].priorityTier ==
                BattleExecutionPriorityTier.Normal;
    }

    private static bool VerifyProductionLongRangeIsNormalTier(
        ProductionFixture fixture
    )
    {
        BattleCardState longRange = FindCard(
            fixture.runtime?.allyA,
            "atk_bullet_001"
        );
        if (longRange == null || longRange.HasTrait(BattleCardTrait.FirstStrike))
        {
            return false;
        }

        BattleActionSlot slot = new BattleActionSlot(fixture.runtime.allyA, 1);
        slot.AssignFreeAction(fixture.runtime.allyA, longRange, fixture.runtime.enemy);
        BattleExecutionPlan plan =
            BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
                new List<BattleActionSlot> { slot },
                new List<BattleEnemyIntent>()
            );
        return plan.executionItems.Count == 1 &&
            plan.executionItems[0].priorityTier ==
                BattleExecutionPriorityTier.Normal;
    }

    private static bool VerifyProductionLongRangeWithBullet(
        ProductionFixture fixture
    )
    {
        if (!fixture.IsValid)
        {
            return false;
        }

        CharacterData shooter = fixture.runtime.allyA;
        BattleCardState longRange = FindCard(shooter, "atk_bullet_001");
        CharacterData target = CreateCharacter("mode103_bullet_target");
        int bulletBefore = shooter.GetBuffStack("Bullet");
        int hpBefore = target.currentHP;
        BattleResolveResult result = BattleResolver.ResolveUnilateralAttack(
            CreateAction(shooter, target, longRange)
        );
        return result != null && result.isSuccess &&
            result.resultType == "FreeAttack" && result.playerCardUsed &&
            target.currentHP < hpBefore && bulletBefore > 0 &&
            shooter.GetBuffStack("Bullet") == bulletBefore - 1;
    }

    private static bool VerifyProductionLongRangeNoBullet(
        ProductionFixture fixture
    )
    {
        CardTestData productionLongRange = FindCardData(
            fixture.cards,
            "atk_bullet_001"
        );
        CharacterData shooter = CreateCharacter("mode103_empty_shooter");
        CharacterData target = CreateCharacter("mode103_empty_target");
        BattleCardState card = BattleCardManager.CreateBattleCard(
            shooter,
            productionLongRange,
            "mode103_empty_long_range"
        );
        int hpBefore = target.currentHP;
        BattleResolveResult result = BattleResolver.ResolveUnilateralAttack(
            CreateAction(shooter, target, card)
        );
        return result != null && !result.isSuccess &&
            result.resultType == "ActionUnavailable" &&
            target.currentHP == hpBefore && card.currentCooldown == 0 &&
            shooter.GetBuffStack("Bullet") == 0;
    }

    private static bool VerifyAttackVsDefenseFullBlockLifecycle()
    {
        ClashFixture fixture = CreateRespondedFixture(
            "mode103_full_block",
            CardType.Defense,
            2,
            9
        );
        const string resource = "Mode103FullBlockResource";
        fixture.attack.actor.AddBuff(resource, 2, -1);
        fixture.attack.cardState.cardData.resourceRule = CreateResourceRule(resource);
        BattleResolveResult result = BattleResolver.ResolveAttackVsDefense(
            fixture.attack,
            fixture.response
        );
        return result != null && result.isSuccess &&
            result.resultType == "DefenseFullBlock" && result.damage == 0 &&
            fixture.attack.cardState.currentCooldown == 3 &&
            fixture.response.cardState.currentCooldown == 3 &&
            fixture.attack.actor.GetBuffStack(resource) == 1;
    }

    private static bool VerifyAttackVsDodgeSuccessLifecycle()
    {
        ClashFixture fixture = CreateRespondedFixture(
            "mode103_dodge_success",
            CardType.Dodge,
            2,
            9
        );
        const string resource = "Mode103DodgeResource";
        fixture.attack.actor.AddBuff(resource, 2, -1);
        fixture.attack.cardState.cardData.resourceRule = CreateResourceRule(resource);
        BattleResolveResult result = BattleResolver.ResolveAttackVsDodge(
            fixture.attack,
            fixture.response
        );
        return result != null && result.isSuccess &&
            result.resultType == "DodgeSuccess" && result.damage == 0 &&
            fixture.attack.cardState.currentCooldown == 3 &&
            fixture.response.cardState.currentCooldown == 3 &&
            fixture.attack.actor.GetBuffStack(resource) == 1;
    }

    private static bool VerifyContinuousDodgePerActorReady()
    {
        BattlePresentationRoute route = CreatePresentationRoute(
            CardType.Dodge,
            string.Empty,
            CardType.Attack,
            AttackDeliveryMode.LongRangeShoot,
            true
        );
        BattlePresentationReadyContract ready =
            BattlePresentationReadyPolicy.Create(route);
        return route != null && ready.Primary != null &&
            ready.Primary.PoseKind == BattlePresentationReadyPoseKind.Aim &&
            ready.Primary.ShouldApplyReady &&
            !ready.Primary.PreserveCurrentPose && ready.Secondary != null &&
            ready.Secondary.PoseKind == BattlePresentationReadyPoseKind.Dodge &&
            ready.Secondary.PreserveCurrentPose &&
            !ready.Secondary.ShouldApplyReady;
    }

    private static bool VerifyUnilateralHasNoClashSession()
    {
        CharacterData actor = CreateCharacter("mode103_policy_actor");
        CharacterData target = CreateCharacter("mode103_policy_target");
        BattleExecutionAction action = CreateAction(
            actor,
            target,
            CreateCard(actor, CardType.Attack, 5)
        );
        BattleExecutionInteractionContext executionContext =
            new BattleExecutionInteractionContext(null, action, null);
        BattlePresentationInteractionContextFactory.TryCreate(
            executionContext,
            false,
            out BattlePresentationInteractionContext presentationContext
        );
        BattleExecutionPhaseRequirements requirements =
            BattleExecutionPausablePolicy.Evaluate(presentationContext);
        BattleResolutionPlan plan = BattleResolver.BuildUnilateralAttackResolutionPlan(
            action,
            null,
            null,
            out BattleResolveResult failure
        );
        return failure == null && plan != null && plan.clashSession == null &&
            requirements.HasPresentationPhases && requirements.RequiresActionBegin &&
            requirements.RequiresImpact && requirements.RequiresActionComplete &&
            !requirements.RequiresClashSession && !requirements.RequiresManualRoll &&
            !requirements.RequiresRollResult;
    }

    private static bool VerifyEnemySlot2Descriptor(ProductionFixture fixture)
    {
        if (!fixture.IsValid || fixture.runtime.intentQueue.Count < 2)
        {
            return false;
        }

        BattleEnemyIntent intent = fixture.runtime.intentQueue[1];
        BattleActionRelationQueryService query =
            new BattleActionRelationQueryService(fixture.runtime);
        IReadOnlyList<BattleActionRelationDescriptor> relations =
            query.GetRelationsForIntent(intent);
        return relations.Count == 1 &&
            relations[0].SourceIntent == intent &&
            relations[0].IntentSourceSlotIndex == 2 &&
            relations[0].EnemyActionType == CardType.Defense &&
            relations[0].EnemySlotID == "Enemy:2" &&
            BattleActionRelationVisibilityPolicy.IsVisible(
                relations[0],
                "Enemy:2",
                string.Empty,
                false
            ) && !BattleActionRelationVisibilityPolicy.IsVisible(
                relations[0],
                "Enemy:1",
                string.Empty,
                false
            ) && BattleActionRelationVisibilityPolicy.IsVisible(
                relations[0],
                string.Empty,
                string.Empty,
                true
            );
    }

    private static bool VerifyResponseBindsEnemySlot2()
    {
        ProductionFixture fixture = CreateProductionFixture();
        if (!fixture.IsValid || fixture.runtime.intentQueue.Count < 2)
        {
            return false;
        }

        BattleEnemyIntent intent = fixture.runtime.intentQueue[1];
        BattleCardState attack = FindCard(fixture.runtime.allyA, "atk_001");
        bool assigned = BattleActionSlotManager.AssignResponseToEnemyIntent(
            fixture.runtime.actionSlots,
            fixture.runtime.allyA,
            1,
            fixture.runtime.allyA,
            attack,
            intent
        );
        BattleActionRelationQueryService query =
            new BattleActionRelationQueryService(fixture.runtime);
        IReadOnlyList<BattleActionRelationDescriptor> relations =
            query.GetRelationsForIntent(intent);
        return assigned && intent.isResponded && relations.Count == 1 &&
            relations[0].IntentSourceSlotIndex == 2 &&
            relations[0].ResponseSlot != null &&
            object.ReferenceEquals(relations[0].ResponseSlot.enemyIntent, intent) &&
            relations[0].Kind == BattleActionRelationKind.DefenseResponse;
    }

    private static bool VerifyProductionPresentationRequirements(
        ProductionFixture fixture
    )
    {
        if (!fixture.IsValid)
        {
            return false;
        }

        BattleCharacterPresentationRequirements ally =
            BattleCharacterPresentationRequirements.FromCards(
                fixture.runtime.allyA.battleCards
            );
        BattleCharacterPresentationRequirements enemy =
            BattleCharacterPresentationRequirements.FromCards(
                fixture.runtime.enemy.battleCards
            );
        return HasCapabilities(
                ally,
                BattleCharacterPresentationCapability.Base |
                BattleCharacterPresentationCapability.MeleeAttack |
                BattleCharacterPresentationCapability.LongRangeShoot |
                BattleCharacterPresentationCapability.Defense |
                BattleCharacterPresentationCapability.Dodge
            ) && !ally.Requires(
                BattleCharacterPresentationCapability.CloseRangeShoot
            ) && HasCapabilities(
                enemy,
                BattleCharacterPresentationCapability.Base |
                BattleCharacterPresentationCapability.MeleeAttack |
                BattleCharacterPresentationCapability.Defense
            );
    }

    private static bool VerifyCampOnlyChangesFacing(ProductionFixture fixture)
    {
        BattleCharacterPresentationRequirements requirements =
            BattleCharacterPresentationRequirements.FromCards(
                fixture.runtime?.enemy?.battleCards
            );
        BattleCharacterPresentationCapability before = requirements.Capabilities;
        bool allyFlip = BattleCharacterPresentationFacing.ShouldFlipX(
            false,
            BattleUnitCamp.Ally
        );
        bool enemyFlip = BattleCharacterPresentationFacing.ShouldFlipX(
            false,
            BattleUnitCamp.Enemy
        );
        return before == requirements.Capabilities && allyFlip && !enemyFlip;
    }

    private static bool VerifyProductionDataToBindingContract(
        ProductionFixture fixture
    )
    {
        if (!fixture.IsValid)
        {
            return false;
        }

        BattleCharacterPresentationRequirements ally =
            BattleCharacterPresentationRequirements.FromCards(
                fixture.runtime.allyA.battleCards
            );
        BattleCharacterPresentationRequirements enemy =
            BattleCharacterPresentationRequirements.FromCards(
                fixture.runtime.enemy.battleCards
            );
        string allyError;
        string enemyError;
        return BattleCharacterPresentationBindingValidator.TryValidate(
                fixture.runtime.allyA.characterName,
                ally,
                CreateCompleteBindings(ally),
                out allyError
            ) && BattleCharacterPresentationBindingValidator.TryValidate(
                fixture.runtime.enemy.characterName,
                enemy,
                CreateCompleteBindings(enemy),
                out enemyError
            );
    }

    private static ProductionFixture CreateProductionFixture()
    {
        ProductionFixture fixture = new ProductionFixture
        {
            cards = CardDataLoader.LoadCardData(),
            characters = CharacterDefinitionLoader.LoadDefinitions(),
            enemies = EnemyDefinitionLoader.LoadDefinitions(),
            encounters = EncounterDefinitionLoader.LoadDefinitions()
        };
        fixture.bootstrap = BattleDefinitionBootstrap.CreateRuntimeStateFromDefinitions(
            ProductionEncounterID,
            fixture.cards,
            fixture.characters,
            fixture.enemies,
            fixture.encounters,
            true
        );
        fixture.runtime = fixture.bootstrap != null
            ? fixture.bootstrap.runtimeState
            : null;
        fixture.allyDefinition = fixture.bootstrap != null
            ? fixture.bootstrap.allyADefinition
            : null;
        fixture.enemyDefinition = fixture.bootstrap != null
            ? fixture.bootstrap.enemyDefinition
            : null;
        return fixture;
    }

    private static bool MatchesCardReferences(
        List<BattleCardState> states,
        string[] expectedIDs
    )
    {
        if (states == null || expectedIDs == null ||
            states.Count != expectedIDs.Length)
        {
            return false;
        }

        for (int index = 0; index < states.Count; index++)
        {
            if (states[index]?.cardData?.cardID != expectedIDs[index])
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsIntent(
        BattleEnemyIntent intent,
        int slot,
        string cardType,
        string cardID
    )
    {
        return intent != null && intent.enemySlotIndex == slot &&
            intent.enemyCardState?.cardData?.cardType == cardType &&
            intent.enemyCardState.cardData.cardID == cardID;
    }

    private static bool IsDirectionalContext(
        BattleExecutionAction sideA,
        BattleExecutionAction sideB,
        BattleInteractionType expected
    )
    {
        BattleExecutionInteractionContext executionContext =
            new BattleExecutionInteractionContext(null, sideA, sideB);
        return executionContext.effectiveInteractionType == expected &&
            BattlePresentationInteractionContextFactory.TryCreate(
                executionContext,
                false,
                out BattlePresentationInteractionContext presentation
            ) && presentation.InteractionType == expected &&
            presentation.AttackAction != null;
    }

    private static ClashFixture CreateRespondedFixture(
        string id,
        string responseType,
        int attackPoint,
        int responsePoint
    )
    {
        CharacterData player = CreateCharacter(id + "_player");
        CharacterData enemy = CreateCharacter(id + "_enemy");
        BattleCardState attackCard = CreateCard(
            player,
            CardType.Attack,
            attackPoint
        );
        BattleCardState responseCard = CreateCard(
            enemy,
            responseType,
            responsePoint
        );
        BattleEnemyIntent intent = new BattleEnemyIntent(
            id + "_intent",
            enemy,
            responseCard,
            player,
            1,
            1
        );
        BattleActionSlot slot = new BattleActionSlot(player, 1);
        slot.AssignResponse(player, attackCard, intent, false);
        intent.MarkResponded();
        return new ClashFixture
        {
            attack = new BattleExecutionAction(
                player,
                attackCard,
                slot,
                intent,
                enemy
            ),
            response = new BattleExecutionAction(
                enemy,
                responseCard,
                null,
                intent,
                player
            )
        };
    }

    private static BattlePresentationRoute CreatePresentationRoute(
        string sideAType,
        string sideADelivery,
        string sideBType,
        string sideBDelivery,
        bool preserveDodge
    )
    {
        CharacterData sideA = CreateCharacter("mode103_route_a");
        CharacterData sideB = CreateCharacter("mode103_route_b");
        BattleExecutionInteractionContext executionContext =
            new BattleExecutionInteractionContext(
                null,
                CreateAction(
                    sideA,
                    sideB,
                    CreateCard(sideA, sideAType, 5, sideADelivery)
                ),
                CreateAction(
                    sideB,
                    sideA,
                    CreateCard(sideB, sideBType, 5, sideBDelivery)
                )
            );
        if (!BattlePresentationInteractionContextFactory.TryCreate(
                executionContext,
                preserveDodge,
                out BattlePresentationInteractionContext context
            ))
        {
            return null;
        }

        BattlePresentationRequest request = new BattlePresentationRequest(
            103L,
            BattlePresentationCue.ActionBegin,
            null,
            null,
            null,
            null,
            string.Empty,
            false,
            context
        );
        BattlePresentationRouter.TryCreateRoute(request, out var route);
        return route;
    }

    private static BattlePresentationRoute CreateUnilateralPresentationRoute(
        string deliveryMode
    )
    {
        CharacterData actor = CreateCharacter("mode103_unilateral_actor");
        CharacterData target = CreateCharacter("mode103_unilateral_target");
        BattleExecutionInteractionContext executionContext =
            new BattleExecutionInteractionContext(
                null,
                CreateAction(
                    actor,
                    target,
                    CreateCard(actor, CardType.Attack, 5, deliveryMode)
                ),
                null
            );
        if (!BattlePresentationInteractionContextFactory.TryCreate(
                executionContext,
                false,
                out BattlePresentationInteractionContext context
            ))
        {
            return null;
        }

        BattlePresentationRequest request = new BattlePresentationRequest(
            103L,
            BattlePresentationCue.ActionBegin,
            null,
            null,
            null,
            null,
            string.Empty,
            false,
            context
        );
        BattlePresentationRouter.TryCreateRoute(request, out var route);
        return route;
    }

    private static CharacterData CreateCharacter(string id)
    {
        return new CharacterData(id, 30, 5, 5, id);
    }

    private static BattleCardState CreateCard(
        CharacterData owner,
        string cardType,
        int point,
        string delivery = AttackDeliveryMode.Melee,
        bool firstStrike = false
    )
    {
        CardTestData data = CreateCardData(cardType, point, delivery);
        data.cardID = owner.runtimeUnitID + "_" + cardType + "_" +
            owner.battleCards.Count;
        data.cardName = data.cardID;
        data.traits = firstStrike
            ? new[] { BattleCardTrait.FirstStrike }
            : new BattleCardTrait[0];
        return BattleCardManager.CreateBattleCard(
            owner,
            data,
            data.cardID + "_instance"
        );
    }

    private static CardTestData CreateCardData(
        string cardType,
        int point,
        string delivery = AttackDeliveryMode.Melee
    )
    {
        return new CardTestData
        {
            cardID = "mode103_" + cardType,
            cardName = "Mode103 " + cardType,
            cardType = cardType,
            attackDeliveryMode = cardType == CardType.Attack
                ? delivery
                : string.Empty,
            isClashable = cardType == CardType.Attack,
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
    }

    private static BattleExecutionAction CreateAction(
        CharacterData actor,
        CharacterData target,
        BattleCardState card
    )
    {
        return card != null
            ? new BattleExecutionAction(actor, card, null, null, target)
            : null;
    }

    private static CardResourceRuleData CreateResourceRule(string resourceID)
    {
        return new CardResourceRuleData
        {
            resourceType = "BuffStack",
            resourceID = resourceID,
            requiredStackForNormalVersion = 1,
            consumeAmountOnSuccess = 1
        };
    }

    private static BattleCardState FindCard(CharacterData owner, string cardID)
    {
        if (owner?.battleCards == null)
        {
            return null;
        }
        for (int index = 0; index < owner.battleCards.Count; index++)
        {
            BattleCardState state = owner.battleCards[index];
            if (state?.cardData?.cardID == cardID)
            {
                return state;
            }
        }
        return null;
    }

    private static CardTestData FindCardData(
        List<CardTestData> cards,
        string cardID
    )
    {
        return CardDataLoader.FindCardByID(cards, cardID);
    }

    private static bool HasCapabilities(
        BattleCharacterPresentationRequirements requirements,
        BattleCharacterPresentationCapability expected
    )
    {
        return requirements != null && requirements.Capabilities == expected;
    }

    private static BattleCharacterPresentationBindingSnapshot
        CreateCompleteBindings(BattleCharacterPresentationRequirements requirements)
    {
        BattleCharacterPresentationBindingSnapshot bindings =
            new BattleCharacterPresentationBindingSnapshot
            {
                HasCharacterSpriteRenderer = true,
                HasBodyVisualRoot = true,
                HasIdleSprite = true,
                HasHitSprite = true
            };
        if (requirements.Requires(
                BattleCharacterPresentationCapability.MeleeAttack))
        {
            bindings.HasSprintSprite = true;
            bindings.HasSlashSprite = true;
        }
        if (requirements.Requires(
                BattleCharacterPresentationCapability.LongRangeShoot))
        {
            bindings.HasAimSprite = true;
            bindings.HasShootSprite = true;
            bindings.HasLongRangeMuzzleFlashAnchor = true;
            bindings.HasLongRangeMuzzleFlashEffect = true;
        }
        if (requirements.Requires(
                BattleCharacterPresentationCapability.CloseRangeShoot))
        {
            bindings.HasSprintSprite = true;
            bindings.HasCloseRangeShootSprite = true;
            bindings.HasCloseRangeMuzzleFlashAnchor = true;
            bindings.HasCloseRangeMuzzleFlashEffect = true;
        }
        if (requirements.Requires(
                BattleCharacterPresentationCapability.Defense))
        {
            bindings.HasGuardSprite = true;
        }
        if (requirements.Requires(
                BattleCharacterPresentationCapability.Dodge))
        {
            bindings.HasDodgeSprite = true;
        }
        return bindings;
    }

    private sealed class ProductionFixture
    {
        public List<CardTestData> cards;
        public List<CharacterDefinitionData> characters;
        public List<EnemyDefinitionData> enemies;
        public List<EncounterDefinitionData> encounters;
        public BattleDefinitionBootstrapResult bootstrap;
        public BattleRuntimeState runtime;
        public CharacterDefinitionData allyDefinition;
        public EnemyDefinitionData enemyDefinition;

        public bool IsValid => bootstrap != null && bootstrap.isSuccess &&
            runtime != null;
    }

    private sealed class ClashFixture
    {
        public BattleExecutionAction attack;
        public BattleExecutionAction response;
    }
}
