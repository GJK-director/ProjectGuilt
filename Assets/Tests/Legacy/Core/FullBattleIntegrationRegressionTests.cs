// 脚本中文说明：组合正式数据与Phase1-6契约，验证BattleScene运行链的数据侧接线。
using System.Collections.Generic;
using UnityEngine;

public static class FullBattleIntegrationRegressionTests
{
    private const string ProductionEncounterID = "encounter_test_001";

    public static bool Run()
    {
        BattleTestContext production = BattleScenarioBuilder.CreateProductionEncounter(ProductionEncounterID, true);
        bool[] results =
        {
            VerifyProductionCharacterBootstrap(production),
            VerifyProductionEnemyBootstrap(production),
            VerifyProductionCardOwnership(production),
            VerifyProductionMultiSlotIntents(production),
            VerifyLegacyIntentPatternFallback(production),
            VerifyPointAsDamage250Percent(),
            VerifyAttackVsAttack(production),
            VerifyDirectionalInteraction(CardType.Defense),
            VerifyDirectionalInteraction(CardType.Dodge),
            VerifyUnilateralBothDirections(),
            VerifyNoInteractionExecution(),
            VerifyNoInteraction(CardType.Defense, CardType.Dodge),
            VerifyNoInteraction(CardType.Dodge, CardType.Dodge),
            VerifyFirstStrikeTierOrdering(),
            VerifyLongRangeWithoutFirstStrikeIsNormalTier(),
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
            "Production Enemy Definition可建立四种正式Runtime Enemy卡",
            "Production Card ownership与Definition引用一致",
            "Enemy 10回合双槽Intent Cycle与回合循环正确",
            "Legacy intentPattern在无Cycle时继续可用",
            "PointAsDamage250Percent按2.5倍计算",
            "Production Melee/Melee归类AttackVsAttack",
            "AttackVsDefense两个方向归一为同一Interaction",
            "AttackVsDodge两个方向归一为同一Interaction",
            "Player/Enemy Unilateral共用UnilateralAttack",
            "Defense/Defense执行为NoInteraction且无生命周期提交",
            "Defense/Dodge归类NoInteraction",
            "Dodge/Dodge归类NoInteraction",
            "FirstStrike完整Item优先且不拆Pairing",
            "LongRange无FirstStrike Trait时保持Normal",
            "Production LongRange有Bullet时完成单方攻击生命周期",
            "Production LongRange无Bullet时ActionUnavailable",
            "AttackVsDefense FullBlock仍提交Attack生命周期",
            "AttackVsDodge Success仍提交Attack生命周期",
            "Continuous Dodge只保留Dodger且新Attack进入Ready",
            "Unilateral共用Manual Roll Gate但不创建ClashSession",
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
        BattleTestContext fixture
    )
    {
        return fixture.IsValid && fixture.Runtime.allyUnits.Count == 1 &&
            fixture.Runtime.allyA != null && fixture.AllyDefinition != null &&
            fixture.Runtime.allyA.runtimeUnitID ==
                fixture.AllyDefinition.characterID &&
            fixture.Runtime.LifecyclePhase == BattleLifecyclePhase.Prepare;
    }

    private static bool VerifyProductionEnemyBootstrap(
        BattleTestContext fixture
    )
    {
        return fixture.IsValid && fixture.Runtime.enemyUnits.Count == 1 &&
            fixture.Runtime.enemy != null && fixture.EnemyDefinition != null &&
            fixture.Runtime.enemy.runtimeUnitID == fixture.EnemyDefinition.enemyID &&
            fixture.EnemyDefinition.cardIDs != null &&
            fixture.EnemyDefinition.cardIDs.Length == 8 &&
            CountUniqueCardIDs(fixture.EnemyDefinition.cardIDs) == 4 &&
            HasCardDefinition(fixture.Cards, "enemy_probe_001", CardType.Attack, 3, 5, "PointAsDamage") &&
            HasCardDefinition(fixture.Cards, "enemy_smash_001", CardType.Attack, 9, 12, "PointAsDamage") &&
            HasCardDefinition(fixture.Cards, "enemy_fierce_001", CardType.Attack, 2, 4, "PointAsDamage250Percent") &&
            HasCardDefinition(fixture.Cards, "enemy_guard_001", CardType.Defense, 5, 7, "PointAsDefense");
    }

    private static bool VerifyProductionCardOwnership(
        BattleTestContext fixture
    )
    {
        return fixture.IsValid && MatchesCardReferences(
                fixture.Runtime.allyA.battleCards,
                fixture.AllyDefinition.startingCardIDs
            ) && MatchesCardReferences(
                fixture.Runtime.enemy.battleCards,
                fixture.EnemyDefinition.cardIDs
            );
    }

    private static bool VerifyProductionMultiSlotIntents(
        BattleTestContext fixture
    )
    {
        if (!fixture.IsValid || fixture.Runtime.intentQueue.Count != 2 ||
            fixture.Bootstrap.encounterDefinition.intentCycle == null ||
            fixture.Bootstrap.encounterDefinition.intentCycle.Length != 10)
        {
            return false;
        }

        if (!VerifyIntentCycleTurns(fixture) ||
            !fixture.Bootstrap.encounterDefinition.repeatIntentPattern)
        {
            return false;
        }

        BattleTestContext nextTurnFixture = BattleScenarioBuilder.CreateProductionEncounter(ProductionEncounterID, true);
        if (!nextTurnFixture.IsValid)
        {
            return false;
        }

        BattleRuntimeState runtime = nextTurnFixture.Runtime;
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
                    nextTurnFixture.Bootstrap.encounterDefinition,
                    nextTurnFixture.EnemyDefinition,
                    nextTurnFixture.Bootstrap.allyByID,
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
                FindCard(runtime.enemy, "enemy_probe_001"),
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
                FindCard(runtime.enemy, "enemy_probe_001"),
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
                "enemy_probe_001") &&
            IsIntent(
                runtime.intentQueue[1],
                2,
                CardType.Defense,
                "enemy_guard_001");
    }

    private static bool VerifyPointAsDamage250Percent()
    {
        CharacterData attacker = TestCharacterFactory.Create("mode103_250_attacker");
        CharacterData defender = TestCharacterFactory.Create("mode103_250_defender");
        CardTestData fierce = new CardTestData
        {
            cardID = "mode103_250",
            cardName = "Mode103 2.5x",
            cardType = CardType.Attack,
            minPoint = 2,
            maxPoint = 4,
            damageFormula = "PointAsDamage250Percent"
        };
        return BattleCalculator.ConvertScaledDamageToHPDamage(
                BattleCalculator.GetFinalDamageScaled(attacker, defender, fierce, 2)) == 5 &&
            BattleCalculator.ConvertScaledDamageToHPDamage(
                BattleCalculator.GetFinalDamageScaled(attacker, defender, fierce, 3)) == 7 &&
            BattleCalculator.ConvertScaledDamageToHPDamage(
                BattleCalculator.GetFinalDamageScaled(attacker, defender, fierce, 4)) == 10;
    }

    private static bool VerifyLegacyIntentPatternFallback(
        BattleTestContext fixture
    )
    {
        if (!fixture.IsValid)
        {
            return false;
        }

        EncounterDefinitionData legacyDefinition =
            new EncounterDefinitionData
            {
                encounterID = "mode103_legacy_pattern",
                encounterName = "Mode103 Legacy Pattern",
                allyCharacterIDs = fixture.Bootstrap.encounterDefinition.allyCharacterIDs,
                enemyID = fixture.EnemyDefinition.enemyID,
                intentPattern = fixture.Bootstrap.encounterDefinition.intentPattern,
                repeatIntentPattern = true,
                battleBackgroundKey = "mode103_background",
                battleMusicKey = "mode103_music"
            };
        string validationError;
        if (!EncounterDefinitionLoader.ValidateDefinition(
                legacyDefinition,
                out validationError))
        {
            return false;
        }

        BattleDefinitionIntentQueueResult result =
            BattleDefinitionBootstrap.CreateIntentQueueForTurn(
                fixture.Runtime,
                legacyDefinition,
                fixture.EnemyDefinition,
                fixture.Bootstrap.allyByID,
                2,
                fixture.Runtime.actionSlots
            );
        return result != null && result.isSuccess &&
            result.intentQueue != null && result.intentQueue.Count == 2 &&
            IsCycleIntent(result.intentQueue[0], fixture.Runtime.enemy, 1,
                "enemy_probe_001", fixture.Runtime.allyA, 1) &&
            IsCycleIntent(result.intentQueue[1], fixture.Runtime.enemy, 2,
                "enemy_probe_001", fixture.Runtime.allyA, 2);
    }

    private static bool VerifyIntentCycleTurns(BattleTestContext fixture)
    {
        string[][] expectedCardIDs =
        {
            new[] { "enemy_probe_001", "enemy_probe_001" },
            new[] { "enemy_probe_001", "enemy_guard_001" },
            new[] { "enemy_smash_001", "enemy_probe_001" },
            new[] { "enemy_fierce_001", "enemy_probe_001" },
            new[] { "enemy_guard_001", "enemy_guard_001" },
            new[] { "enemy_smash_001", "enemy_fierce_001" },
            new[] { "enemy_probe_001", "enemy_probe_001" },
            new[] { "enemy_smash_001", "enemy_smash_001" },
            new[] { "enemy_guard_001", "enemy_fierce_001" },
            new[] { "enemy_smash_001", "enemy_fierce_001" }
        };

        for (int turn = 1; turn <= 21; turn++)
        {
            int cycleIndex = (turn - 1) % expectedCardIDs.Length;
            EnemyIntentRoundDefinitionData expectedRound =
                fixture.Bootstrap.encounterDefinition.intentCycle[cycleIndex];
            if (expectedRound == null || expectedRound.intents == null ||
                expectedRound.intents.Length != 2 ||
                expectedRound.intents[0] == null ||
                expectedRound.intents[1] == null ||
                expectedRound.intents[0].targetRule !=
                    EncounterDefinitionLoader.TargetRuleFixedCharacterSlot ||
                expectedRound.intents[0].targetCharacterID != "ally_001" ||
                expectedRound.intents[0].targetSlotIndex != 1 ||
                expectedRound.intents[1].targetRule !=
                    EncounterDefinitionLoader.TargetRuleFixedCharacterSlot ||
                expectedRound.intents[1].targetCharacterID != "ally_001" ||
                expectedRound.intents[1].targetSlotIndex != 2)
            {
                return false;
            }

            BattleDefinitionIntentQueueResult result =
                BattleDefinitionBootstrap.CreateIntentQueueForTurn(
                    fixture.Runtime,
                    fixture.Bootstrap.encounterDefinition,
                    fixture.EnemyDefinition,
                    fixture.Bootstrap.allyByID,
                    turn,
                    fixture.Runtime.actionSlots
                );
            if (result == null || !result.isSuccess || result.intentQueue == null ||
                result.intentQueue.Count != 2 ||
                !IsCycleIntent(result.intentQueue[0], fixture.Runtime.enemy, 1,
                    expectedCardIDs[cycleIndex][0], fixture.Runtime.allyA, 1) ||
                !IsCycleIntent(result.intentQueue[1], fixture.Runtime.enemy, 2,
                    expectedCardIDs[cycleIndex][1], fixture.Runtime.allyA, 2))
            {
                return false;
            }

            if (expectedCardIDs[cycleIndex][0] == expectedCardIDs[cycleIndex][1] &&
                object.ReferenceEquals(
                    result.intentQueue[0].enemyCardState,
                    result.intentQueue[1].enemyCardState))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCycleIntent(
        BattleEnemyIntent intent,
        CharacterData enemy,
        int enemySlotIndex,
        string cardID,
        CharacterData target,
        int targetSlotIndex
    )
    {
        return intent != null &&
            object.ReferenceEquals(intent.enemy, enemy) &&
            intent.enemySlotIndex == enemySlotIndex &&
            intent.enemyCardState != null &&
            intent.enemyCardState.cardData != null &&
            intent.enemyCardState.cardData.cardID == cardID &&
            object.ReferenceEquals(intent.originalTargetCharacter, target) &&
            intent.originalTargetSlotIndex == targetSlotIndex &&
            object.ReferenceEquals(intent.actualTargetCharacter, target) &&
            intent.actualTargetSlotIndex == targetSlotIndex &&
            !intent.isResponded && !intent.isConsumedAsReactiveGuard;
    }

    private static bool VerifyAttackVsAttack(BattleTestContext fixture)
    {
        BattleCardState allyAttack = FindCard(
            fixture.Runtime?.allyA,
            "atk_001"
        );
        BattleCardState enemyAttack = FindCard(
            fixture.Runtime?.enemy,
            "enemy_probe_001"
        );
        return BattleInteractionClassifier.Classify(
            allyAttack,
            enemyAttack
        ) == BattleInteractionType.AttackVsAttack;
    }

    private static bool VerifyDirectionalInteraction(string responseType)
    {
        CharacterData attacker = TestCharacterFactory.Create("mode103_attacker");
        CharacterData responder = TestCharacterFactory.Create("mode103_responder");
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
        CharacterData actor = TestCharacterFactory.Create("mode103_unilateral_actor");
        CharacterData target = TestCharacterFactory.Create("mode103_unilateral_target");
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
        CharacterData actor = TestCharacterFactory.Create("mode103_no_interaction_actor");
        CharacterData target = TestCharacterFactory.Create("mode103_no_interaction_target");
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
        BattleEnemyIntent intent = TestIntentFactory.Create(
            "mode103_firststrike_intent",
            enemy,
            enemyAttack,
            ally,
            originalTargetSlotIndex: 1,
            intentOrder: 1,
            enemySlotIndex: 1
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

    private static bool VerifyLongRangeWithoutFirstStrikeIsNormalTier()
    {
        CharacterData actor = TestCharacterFactory.Create("mode103_long_range_normal");
        CharacterData target = TestCharacterFactory.Create("mode103_long_range_target");
        BattleCardState longRange = CreateCard(
            actor,
            CardType.Attack,
            5,
            AttackDeliveryMode.LongRangeShoot
        );
        BattleActionSlot slot = new BattleActionSlot(actor, 1);
        slot.AssignFreeAction(actor, longRange, target);
        BattleExecutionPlan plan =
            BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
                new List<BattleActionSlot> { slot },
                new List<BattleEnemyIntent>()
            );
        return !longRange.HasTrait(BattleCardTrait.FirstStrike) &&
            plan.executionItems.Count == 1 &&
            plan.executionItems[0].priorityTier ==
                BattleExecutionPriorityTier.Normal;
    }

    private static bool VerifyProductionLongRangeWithBullet(
        BattleTestContext fixture
    )
    {
        if (!fixture.IsValid)
        {
            return false;
        }

        CharacterData shooter = fixture.Runtime.allyA;
        BattleCardState longRange = FindCard(shooter, "atk_bullet_001");
        CharacterData target = TestCharacterFactory.Create("mode103_bullet_target");
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
        BattleTestContext fixture
    )
    {
        CardTestData productionLongRange = FindCardData(
            fixture.Cards,
            "atk_bullet_001"
        );
        CharacterData shooter = TestCharacterFactory.Create("mode103_empty_shooter");
        CharacterData target = TestCharacterFactory.Create("mode103_empty_target");
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
            fixture.attack.cardState.currentCooldown == 2 &&
            fixture.response.cardState.currentCooldown == 2 &&
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
            fixture.attack.cardState.currentCooldown == 2 &&
            fixture.response.cardState.currentCooldown == 2 &&
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
        CharacterData actor = TestCharacterFactory.Create("mode103_policy_actor");
        CharacterData target = TestCharacterFactory.Create("mode103_policy_target");
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
            !requirements.RequiresClashSession && requirements.RequiresManualRoll &&
            requirements.RequiresRollResult;
    }

    private static bool VerifyEnemySlot2Descriptor(BattleTestContext fixture)
    {
        if (!TrySetProductionIntentsForTurn(fixture, 2))
        {
            return false;
        }

        BattleEnemyIntent intent = fixture.Runtime.intentQueue[1];
        BattleActionRelationQueryService query =
            new BattleActionRelationQueryService(fixture.Runtime);
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
        BattleTestContext fixture = BattleScenarioBuilder.CreateProductionEncounter(ProductionEncounterID, true);
        if (!TrySetProductionIntentsForTurn(fixture, 2))
        {
            return false;
        }

        fixture.Runtime.allyA.turnSpeed = 8;
        fixture.Runtime.enemy.turnSpeed = 2;

        BattleEnemyIntent intent = fixture.Runtime.intentQueue[1];
        BattleCardState attack = FindCard(fixture.Runtime.allyA, "atk_001");
        bool assigned = BattleActionSlotManager.AssignResponseToEnemyIntent(
            fixture.Runtime.actionSlots,
            fixture.Runtime.allyA,
            1,
            fixture.Runtime.allyA,
            attack,
            intent
        );
        BattleActionRelationQueryService query =
            new BattleActionRelationQueryService(fixture.Runtime);
        IReadOnlyList<BattleActionRelationDescriptor> relations =
            query.GetRelationsForIntent(intent);
        return assigned && intent.isResponded && relations.Count == 1 &&
            relations[0].IntentSourceSlotIndex == 2 &&
            relations[0].ResponseSlot != null &&
            object.ReferenceEquals(relations[0].ResponseSlot.enemyIntent, intent) &&
            relations[0].Kind == BattleActionRelationKind.DefenseResponse;
    }

    private static bool TrySetProductionIntentsForTurn(
        BattleTestContext fixture,
        int turn
    )
    {
        if (!fixture.IsValid)
        {
            return false;
        }

        BattleDefinitionIntentQueueResult result =
            BattleDefinitionBootstrap.CreateIntentQueueForTurn(
                fixture.Runtime,
                fixture.Bootstrap.encounterDefinition,
                fixture.EnemyDefinition,
                fixture.Bootstrap.allyByID,
                turn,
                fixture.Runtime.actionSlots
            );
        if (result == null || !result.isSuccess || result.intentQueue == null ||
            result.intentQueue.Count != 2)
        {
            return false;
        }

        fixture.Runtime.SetIntentQueue(result.intentQueue);
        return true;
    }

    private static bool VerifyProductionPresentationRequirements(
        BattleTestContext fixture
    )
    {
        if (!fixture.IsValid)
        {
            return false;
        }

        BattleCharacterPresentationRequirements ally =
            BattleCharacterPresentationRequirements.FromCards(
                fixture.Runtime.allyA.battleCards
            );
        BattleCharacterPresentationRequirements enemy =
            BattleCharacterPresentationRequirements.FromCards(
                fixture.Runtime.enemy.battleCards
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

    private static bool VerifyCampOnlyChangesFacing(BattleTestContext fixture)
    {
        BattleCharacterPresentationRequirements requirements =
            BattleCharacterPresentationRequirements.FromCards(
                fixture.Runtime?.enemy?.battleCards
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
        BattleTestContext fixture
    )
    {
        if (!fixture.IsValid)
        {
            return false;
        }

        BattleCharacterPresentationRequirements ally =
            BattleCharacterPresentationRequirements.FromCards(
                fixture.Runtime.allyA.battleCards
            );
        BattleCharacterPresentationRequirements enemy =
            BattleCharacterPresentationRequirements.FromCards(
                fixture.Runtime.enemy.battleCards
            );
        string allyError;
        string enemyError;
        return BattleCharacterPresentationBindingValidator.TryValidate(
                fixture.Runtime.allyA.characterName,
                ally,
                CreateCompleteBindings(ally),
                out allyError
            ) && BattleCharacterPresentationBindingValidator.TryValidate(
                fixture.Runtime.enemy.characterName,
                enemy,
                CreateCompleteBindings(enemy),
                out enemyError
            );
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
        CharacterData player = TestCharacterFactory.Create(id + "_player");
        CharacterData enemy = TestCharacterFactory.Create(id + "_enemy");
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
        BattleEnemyIntent intent = TestIntentFactory.Create(
            id + "_intent",
            enemy,
            responseCard,
            player,
            originalTargetSlotIndex: 1,
            intentOrder: 1,
            enemySlotIndex: 1
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
        CharacterData sideA = TestCharacterFactory.Create("mode103_route_a");
        CharacterData sideB = TestCharacterFactory.Create("mode103_route_b");
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
        CharacterData actor = TestCharacterFactory.Create("mode103_unilateral_actor");
        CharacterData target = TestCharacterFactory.Create("mode103_unilateral_target");
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



    private static BattleCardState CreateCard(
        CharacterData owner,
        string cardType,
        int point,
        string delivery = AttackDeliveryMode.Melee,
        bool firstStrike = false
    )
    {
        string id =
            owner.runtimeUnitID + "_" +
            cardType + "_" +
            owner.battleCards.Count;

        return TestCardFactory.CreateState(
            owner,
            id,
            cardType,
            point,
            delivery,
            2,
            firstStrike
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

    private static int CountUniqueCardIDs(string[] cardIDs)
    {
        if (cardIDs == null)
        {
            return 0;
        }

        HashSet<string> uniqueIDs = new HashSet<string>();
        foreach (string cardID in cardIDs)
        {
            if (!string.IsNullOrEmpty(cardID))
            {
                uniqueIDs.Add(cardID);
            }
        }

        return uniqueIDs.Count;
    }

    private static bool HasCardDefinition(
        List<CardTestData> cards,
        string cardID,
        string cardType,
        int minPoint,
        int maxPoint,
        string formula
    )
    {
        CardTestData card = FindCardData(cards, cardID);
        if (card == null || card.cardType != cardType ||
            card.minPoint != minPoint || card.maxPoint != maxPoint)
        {
            return false;
        }

        return cardType == CardType.Defense
            ? card.defenseFormula == formula
            : card.damageFormula == formula;
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

    private sealed class ClashFixture
    {
        public BattleExecutionAction attack;
        public BattleExecutionAction response;
    }
}
