// 脚本中文说明：验证 ExecutionPlan 只记录计划阶段 Interaction，不执行 Resolver。
using System.Collections.Generic;
using UnityEngine;

public static class BattleExecutionPlanInteractionTests
{
    public static bool Run()
    {
        bool[] results = new bool[27];

        results[0] = VerifyResponded(
            CardType.Attack,
            CardType.Attack,
            BattleInteractionType.AttackVsAttack
        );
        results[1] = VerifyResponded(
            CardType.Defense,
            CardType.Attack,
            BattleInteractionType.AttackVsDefense
        );
        results[2] = VerifyResponded(
            CardType.Dodge,
            CardType.Attack,
            BattleInteractionType.AttackVsDodge
        );
        results[3] = VerifyResponded(
            CardType.Attack,
            CardType.Defense,
            BattleInteractionType.AttackVsDefense
        );
        results[4] = VerifyResponded(
            CardType.Attack,
            CardType.Dodge,
            BattleInteractionType.AttackVsDodge
        );
        results[5] = VerifyResponded(
            CardType.Defense,
            CardType.Defense,
            BattleInteractionType.NoInteraction
        );
        results[6] = VerifyFreeAction(
            AttackDeliveryMode.Melee,
            BattleInteractionType.UnilateralAttack
        );
        results[7] = VerifyFreeAction(
            AttackDeliveryMode.LongRangeShoot,
            BattleInteractionType.UnilateralAttack
        );
        results[8] = VerifyFreeAction(
            AttackDeliveryMode.CloseRangeShoot,
            BattleInteractionType.UnilateralAttack
        );
        results[9] = VerifyFreeAction(CardType.Defense) &&
            VerifyFreeAction(CardType.Dodge) &&
            VerifyFreeAction("Ability");
        results[10] = VerifyUnresponded(
            CardType.Attack,
            BattleInteractionType.UnilateralAttack
        );
        results[11] = VerifyUnresponded(
                CardType.Defense,
                BattleInteractionType.NoInteraction
            ) &&
            VerifyUnresponded(
                CardType.Dodge,
                BattleInteractionType.NoInteraction
            );
        results[12] = VerifyExactResponseInteractionEligibility();
        results[13] = VerifyFreeAttackSkipsStandaloneEnemyGuard();
        results[14] = VerifyFreeAttackUsesEnemyDefenseGuard();
        results[15] = VerifyFreeAttackUsesEnemyDodgeGuard();
        results[16] = VerifyFreeAttackWithoutEnemyGuardRemainsUnilateral();
        results[17] = VerifyUnavailableEnemyGuardIsNotSelected();
        results[18] = VerifyDeadEnemyGuardIsNotSelected();
        results[19] = VerifyEnemyGuardSelectionUsesStableSlotOrder();
        results[20] = VerifyEnemyGuardSelectionUsesIntentOrderTieBreak();
        results[21] = VerifyReactiveEnemyGuardIsNotConsumedBySelection();
        results[22] = VerifyConsumedReactiveEnemyGuardIsNotSelectedAgain();
        results[23] = VerifyFreeAttackSkipsStandaloneEnemyDodge();
        results[24] = VerifyOtherEnemyGuardDoesNotIntercept();
        results[25] = VerifyUnavailableFreeAttackDoesNotConsumeEnemyGuard();
        results[26] = VerifyRespondedEnemyGuardIsNotReactiveCandidate();

        string[] names =
        {
            "Responded Attack + Attack",
            "Responded Defense + Attack",
            "Responded Dodge + Attack",
            "Responded Attack + Defense",
            "Responded Attack + Dodge",
            "Responded Defense + Defense",
            "FreeAction Melee Attack",
            "FreeAction LongRangeShoot",
            "FreeAction CloseRangeShoot",
            "FreeAction Defense / Dodge / Ability",
            "Unresponded Enemy Attack",
            "Unresponded Enemy Defense / Dodge",
            "Exact Response同时验证Interaction并保留Guard降级",
            "FreeAction不生成Enemy Defense/Dodge Standalone Item",
            "FreeAction + Enemy Defense进入AttackVsDefense",
            "FreeAction + Enemy Dodge进入AttackVsDodge",
            "没有Enemy Guard时保持UnilateralAttack",
            "不可用Enemy Guard不被选择",
            "死亡Enemy Guard不被选择",
            "Enemy Guard按enemySlotIndex稳定选择",
            "Enemy Guard按intentOrder稳定打破并列",
            "选择Reactive Enemy Guard不提前消费",
            "Reactive Enemy Guard完成后不再重复选择",
            "FreeAction不生成Enemy Dodge Standalone Item",
            "其他Enemy的Guard不会跨目标接管",
            "失效FreeAction不消费Enemy Guard",
            "已Responded Enemy Guard不进入Reactive候选"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式88 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }

        Debug.Log("模式88 ExecutionPlan Interaction聚合结果：" + allPassed);
        return allPassed;
    }

    private static bool VerifyResponded(
        string playerCardType,
        string enemyCardType,
        BattleInteractionType expected
    )
    {
        TestContext context = CreateContext("responded");
        BattleEnemyIntent intent = new BattleEnemyIntent(
            "interaction88_responded",
            context.enemy,
            CreateCard(context.enemy, enemyCardType, "interaction88_enemy"),
            context.ally,
            1,
            1
        );
        BattleActionSlot responseSlot = new BattleActionSlot(context.ally, 1);
        responseSlot.AssignResponse(
            context.ally,
            CreateCard(context.ally, playerCardType, "interaction88_ally"),
            intent,
            false
        );
        intent.MarkResponded();

        return GetOnlyItem(
            new List<BattleActionSlot> { responseSlot },
            new List<BattleEnemyIntent> { intent }
        )?.interactionType == expected;
    }

    private static bool VerifyFreeAction(
        string attackDeliveryMode,
        BattleInteractionType expected
    )
    {
        return VerifyFreeAction(
            CardType.Attack,
            expected,
            attackDeliveryMode
        );
    }

    private static bool VerifyFreeAction(string cardType)
    {
        return VerifyFreeAction(cardType, BattleInteractionType.NoInteraction, null);
    }

    private static bool VerifyFreeAction(
        string cardType,
        BattleInteractionType expected,
        string attackDeliveryMode
    )
    {
        TestContext context = CreateContext("free");
        BattleActionSlot slot = new BattleActionSlot(context.ally, 1);
        slot.AssignFreeAction(
            context.ally,
            CreateCard(context.ally, cardType, "interaction88_free", attackDeliveryMode),
            context.enemy
        );

        return GetOnlyItem(
            new List<BattleActionSlot> { slot },
            new List<BattleEnemyIntent>()
        )?.interactionType == expected;
    }

    private static bool VerifyUnresponded(
        string enemyCardType,
        BattleInteractionType expected
    )
    {
        TestContext context = CreateContext("unresponded");
        BattleEnemyIntent intent = new BattleEnemyIntent(
            "interaction88_unresponded",
            context.enemy,
            CreateCard(context.enemy, enemyCardType, "interaction88_enemy"),
            context.ally,
            1,
            1
        );

        return GetOnlyItem(
            new List<BattleActionSlot>(),
            new List<BattleEnemyIntent> { intent }
        )?.interactionType == expected;
    }

    private static bool VerifyExactResponseInteractionEligibility()
    {
        return VerifyPreparedAssignment(
                CardType.Dodge,
                CardType.Defense,
                false
            ) &&
            VerifyPreparedAssignment(
                CardType.Defense,
                CardType.Dodge,
                false
            ) &&
            VerifyPreparedAssignment(
                CardType.Attack,
                CardType.Defense,
                true
            ) &&
            VerifyPreparedAssignment(
                CardType.Attack,
                CardType.Dodge,
                true
            ) &&
            VerifyPreparedAssignment(
                CardType.Defense,
                CardType.Attack,
                true
            ) &&
            VerifyPreparedAssignment(
                CardType.Dodge,
                CardType.Attack,
                true
            ) &&
            VerifyDowngradedGuardWaitsForAttack();
    }

    private static bool VerifyFreeAttackSkipsStandaloneEnemyGuard()
    {
        ReactiveEnemyGuardFixture fixture = CreateReactiveEnemyGuardFixture(
            "skip_standalone",
            CardType.Defense
        );
        BattleExecutionPlan plan = BattleExecutionPlanManager
            .CreateSpeedBasedExecutionPlan(
                fixture.runtimeState.actionSlots,
                fixture.runtimeState.intentQueue,
                fixture.runtimeState
            );

        return plan != null && plan.executionItems != null &&
            plan.executionItems.Count == 1 &&
            plan.executionItems[0].executionType ==
                BattleExecutionItemType.FreeAction &&
            object.ReferenceEquals(
                plan.executionItems[0].actionSlot,
                fixture.freeActionSlot
            );
    }

    private static bool VerifyFreeAttackSkipsStandaloneEnemyDodge()
    {
        ReactiveEnemyGuardFixture fixture = CreateReactiveEnemyGuardFixture(
            "skip_standalone_dodge",
            CardType.Dodge
        );
        BattleExecutionPlan plan = BattleExecutionPlanManager
            .CreateSpeedBasedExecutionPlan(
                fixture.runtimeState.actionSlots,
                fixture.runtimeState.intentQueue,
                fixture.runtimeState
            );

        return plan != null && plan.executionItems != null &&
            plan.executionItems.Count == 1 &&
            plan.executionItems[0].executionType ==
                BattleExecutionItemType.FreeAction;
    }

    private static bool VerifyFreeAttackUsesEnemyDefenseGuard()
    {
        return VerifyFreeAttackUsesEnemyGuard(
            CardType.Defense,
            BattleInteractionType.AttackVsDefense,
            BattleClashType.DefenseVsAttack
        );
    }

    private static bool VerifyFreeAttackUsesEnemyDodgeGuard()
    {
        return VerifyFreeAttackUsesEnemyGuard(
            CardType.Dodge,
            BattleInteractionType.AttackVsDodge,
            BattleClashType.DodgeVsAttack
        );
    }

    private static bool VerifyFreeAttackUsesEnemyGuard(
        string guardCardType,
        BattleInteractionType expectedInteraction,
        BattleClashType expectedClashType
    )
    {
        ReactiveEnemyGuardFixture fixture = CreateReactiveEnemyGuardFixture(
            "uses_" + guardCardType,
            guardCardType
        );
        BattleExecutionItem item = CreateOnlyFreeActionItem(fixture);
        bool built = BattleExecutionPlanExecutor.TryBuildPausableRoutingContext(
            item,
            fixture.runtimeState,
            out BattleActionSlot actionSlot,
            out BattleGuardSelectionType selectionType,
            out BattleExecutionInteractionContext executionContext,
            out BattlePresentationInteractionContext presentationContext
        );
        if (!built)
        {
            return false;
        }

        bool began = BattleExecutionPlanExecutor
            .TryBeginPausableFreeActionVsEnemyGuard(
                item,
                fixture.runtimeState,
                out BattleClashSession session,
                out bool itemCompleted,
                out string failureMessage
            );

        return began && !itemCompleted &&
            string.IsNullOrEmpty(failureMessage) &&
            object.ReferenceEquals(actionSlot, fixture.freeActionSlot) &&
            selectionType == BattleGuardSelectionType.None &&
            object.ReferenceEquals(item.reactiveEnemyGuardIntent, fixture.guardIntent) &&
            executionContext.effectiveInteractionType == expectedInteraction &&
            presentationContext.InteractionType == expectedInteraction &&
            session != null && session.ClashType == expectedClashType;
    }

    private static bool VerifyFreeAttackWithoutEnemyGuardRemainsUnilateral()
    {
        ReactiveEnemyGuardFixture fixture = CreateReactiveEnemyGuardFixture(
            "no_guard",
            CardType.Attack
        );
        BattleExecutionItem item = CreateOnlyFreeActionItem(fixture);
        bool built = BattleExecutionPlanExecutor.TryBuildPausableRoutingContext(
            item,
            fixture.runtimeState,
            out BattleActionSlot actionSlot,
            out BattleGuardSelectionType selectionType,
            out BattleExecutionInteractionContext executionContext,
            out BattlePresentationInteractionContext presentationContext
        );

        return built && object.ReferenceEquals(actionSlot, fixture.freeActionSlot) &&
            selectionType == BattleGuardSelectionType.None &&
            item.reactiveEnemyGuardIntent == null &&
            executionContext.effectiveInteractionType ==
                BattleInteractionType.UnilateralAttack &&
            presentationContext.InteractionType ==
                BattleInteractionType.UnilateralAttack;
    }

    private static bool VerifyUnavailableEnemyGuardIsNotSelected()
    {
        ReactiveEnemyGuardFixture fixture = CreateReactiveEnemyGuardFixture(
            "unavailable",
            CardType.Defense
        );
        fixture.guardIntent.enemyCardState.currentCooldown = 1;
        return BattleGuardSelectionManager.SelectEnemyDefensiveIntentForFreeAttack(
            fixture.runtimeState.intentQueue,
            fixture.freeActionSlot
        ) == null;
    }

    private static bool VerifyDeadEnemyGuardIsNotSelected()
    {
        ReactiveEnemyGuardFixture fixture = CreateReactiveEnemyGuardFixture(
            "dead",
            CardType.Dodge
        );
        fixture.enemy.currentHP = 0;
        return BattleGuardSelectionManager.SelectEnemyDefensiveIntentForFreeAttack(
            fixture.runtimeState.intentQueue,
            fixture.freeActionSlot
        ) == null;
    }

    private static bool VerifyEnemyGuardSelectionUsesStableSlotOrder()
    {
        ReactiveEnemyGuardFixture fixture = CreateReactiveEnemyGuardFixture(
            "slot_order",
            CardType.Defense,
            2,
            2
        );
        BattleEnemyIntent earlierSlot = CreateReactiveEnemyIntent(
            fixture,
            "slot_order_earlier",
            CardType.Dodge,
            1,
            9
        );
        fixture.runtimeState.SetIntentQueue(new List<BattleEnemyIntent>
        {
            fixture.guardIntent,
            earlierSlot
        });
        return object.ReferenceEquals(
            BattleGuardSelectionManager.SelectEnemyDefensiveIntentForFreeAttack(
                fixture.runtimeState.intentQueue,
                fixture.freeActionSlot
            ),
            earlierSlot
        );
    }

    private static bool VerifyEnemyGuardSelectionUsesIntentOrderTieBreak()
    {
        ReactiveEnemyGuardFixture fixture = CreateReactiveEnemyGuardFixture(
            "intent_order",
            CardType.Defense,
            1,
            4
        );
        BattleEnemyIntent earlierIntent = CreateReactiveEnemyIntent(
            fixture,
            "intent_order_earlier",
            CardType.Dodge,
            1,
            3
        );
        fixture.runtimeState.SetIntentQueue(new List<BattleEnemyIntent>
        {
            fixture.guardIntent,
            earlierIntent
        });
        return object.ReferenceEquals(
            BattleGuardSelectionManager.SelectEnemyDefensiveIntentForFreeAttack(
                fixture.runtimeState.intentQueue,
                fixture.freeActionSlot
            ),
            earlierIntent
        );
    }

    private static bool VerifyReactiveEnemyGuardIsNotConsumedBySelection()
    {
        ReactiveEnemyGuardFixture fixture = CreateReactiveEnemyGuardFixture(
            "not_consumed_by_selection",
            CardType.Defense
        );
        BattleEnemyIntent selected = BattleGuardSelectionManager
            .SelectEnemyDefensiveIntentForFreeAttack(
                fixture.runtimeState.intentQueue,
                fixture.freeActionSlot
            );
        return object.ReferenceEquals(selected, fixture.guardIntent) &&
            !fixture.guardIntent.isConsumedAsReactiveGuard;
    }

    private static bool VerifyConsumedReactiveEnemyGuardIsNotSelectedAgain()
    {
        ReactiveEnemyGuardFixture fixture = CreateReactiveEnemyGuardFixture(
            "consumed_once",
            CardType.Dodge
        );
        fixture.guardIntent.MarkConsumedAsReactiveGuard();
        return BattleGuardSelectionManager.SelectEnemyDefensiveIntentForFreeAttack(
            fixture.runtimeState.intentQueue,
            fixture.freeActionSlot
        ) == null;
    }

    private static bool VerifyOtherEnemyGuardDoesNotIntercept()
    {
        ReactiveEnemyGuardFixture fixture = CreateReactiveEnemyGuardFixture(
            "other_enemy",
            CardType.Defense
        );
        CharacterData otherEnemy = new CharacterData(
            "interaction88_reactive_other_enemy",
            30,
            5,
            5
        );
        BattleEnemyIntent otherEnemyGuard = new BattleEnemyIntent(
            "interaction88_reactive_other_enemy_guard",
            otherEnemy,
            CreateCombatCard(
                otherEnemy,
                "interaction88_reactive_other_enemy_defense",
                CardType.Defense,
                2
            ),
            fixture.ally,
            1,
            1
        );
        fixture.runtimeState.SetIntentQueue(new List<BattleEnemyIntent>
        {
            otherEnemyGuard
        });
        return BattleGuardSelectionManager.SelectEnemyDefensiveIntentForFreeAttack(
            fixture.runtimeState.intentQueue,
            fixture.freeActionSlot
        ) == null;
    }

    private static bool VerifyUnavailableFreeAttackDoesNotConsumeEnemyGuard()
    {
        ReactiveEnemyGuardFixture fixture = CreateReactiveEnemyGuardFixture(
            "unavailable_free_action",
            CardType.Defense
        );
        BattleExecutionItem unavailableItem = CreateOnlyFreeActionItem(fixture);
        fixture.freeActionSlot.cardState.currentCooldown = 1;
        bool built = BattleExecutionPlanExecutor.TryBuildPausableRoutingContext(
            unavailableItem,
            fixture.runtimeState,
            out BattleActionSlot actionSlot,
            out BattleGuardSelectionType selectionType,
            out BattleExecutionInteractionContext executionContext,
            out BattlePresentationInteractionContext presentationContext
        );
        if (!built)
        {
            return false;
        }

        bool began = BattleExecutionPlanExecutor
            .TryBeginPausableFreeActionVsEnemyGuard(
                unavailableItem,
                fixture.runtimeState,
                out BattleClashSession session,
                out bool itemCompleted,
                out string failureMessage
            );
        if (!began || !itemCompleted ||
            unavailableItem.status != BattleExecutionItemStatus.Skipped ||
            fixture.guardIntent.isConsumedAsReactiveGuard)
        {
            return false;
        }

        fixture.freeActionSlot.cardState.currentCooldown = 0;
        BattleExecutionItem nextItem = new BattleExecutionItem(
            2,
            BattleExecutionItemType.FreeAction,
            null,
            fixture.freeActionSlot
        );
        bool nextBuilt = BattleExecutionPlanExecutor.TryBuildPausableRoutingContext(
            nextItem,
            fixture.runtimeState,
            out BattleActionSlot nextActionSlot,
            out BattleGuardSelectionType nextSelectionType,
            out BattleExecutionInteractionContext nextExecutionContext,
            out BattlePresentationInteractionContext nextPresentationContext
        );
        return nextBuilt && object.ReferenceEquals(
            nextItem.reactiveEnemyGuardIntent,
            fixture.guardIntent
        );
    }

    private static bool VerifyRespondedEnemyGuardIsNotReactiveCandidate()
    {
        ReactiveEnemyGuardFixture fixture = CreateReactiveEnemyGuardFixture(
            "responded_guard",
            CardType.Dodge
        );
        fixture.guardIntent.MarkResponded();
        return BattleGuardSelectionManager.SelectEnemyDefensiveIntentForFreeAttack(
            fixture.runtimeState.intentQueue,
            fixture.freeActionSlot
        ) == null;
    }

    private static bool VerifyPreparedAssignment(
        string playerCardType,
        string enemyCardType,
        bool expectExactResponse
    )
    {
        PreparedAssignmentFixture fixture = CreatePreparedAssignmentFixture(
            "prepared_" + playerCardType + "_" + enemyCardType,
            playerCardType,
            enemyCardType
        );
        if (fixture == null)
        {
            return false;
        }

        bool assigned = BattleActionSlotManager.TryAssignToEnemyIntent(
            fixture.runtimeState,
            fixture.ally,
            1,
            fixture.playerCard,
            fixture.intent,
            out BattleActionAssignmentResult result
        );

        if (expectExactResponse)
        {
            return assigned && result != null && result.isSuccess &&
                !result.wasAutoDowngraded &&
                result.placementType == BattleActionPlacementType.ExactEnemyIntent &&
                result.effectiveSlotType == BattleActionSlotType.RespondToEnemyIntent &&
                fixture.slot.placementType == BattleActionPlacementType.ExactEnemyIntent &&
                fixture.slot.slotType == BattleActionSlotType.RespondToEnemyIntent &&
                object.ReferenceEquals(fixture.slot.enemyIntent, fixture.intent) &&
                fixture.intent.isResponded;
        }

        return assigned && result != null && result.isSuccess &&
            result.wasAutoDowngraded &&
            result.placementType == BattleActionPlacementType.SpecificEnemy &&
            result.effectiveSlotType == BattleActionSlotType.EnemySpecificGuard &&
            fixture.slot.placementType == BattleActionPlacementType.SpecificEnemy &&
            fixture.slot.slotType == BattleActionSlotType.EnemySpecificGuard &&
            fixture.slot.enemyIntent == null &&
            object.ReferenceEquals(fixture.slot.requestedEnemy, fixture.enemy) &&
            !fixture.intent.isResponded;
    }

    private static bool VerifyDowngradedGuardWaitsForAttack()
    {
        PreparedAssignmentFixture fixture = CreatePreparedAssignmentFixture(
            "prepared_guard_followup",
            CardType.Dodge,
            CardType.Defense
        );
        if (fixture == null)
        {
            return false;
        }

        bool assigned = BattleActionSlotManager.TryAssignToEnemyIntent(
            fixture.runtimeState,
            fixture.ally,
            1,
            fixture.playerCard,
            fixture.intent,
            out BattleActionAssignmentResult result
        );
        if (!assigned || result == null || !result.wasAutoDowngraded)
        {
            return false;
        }

        BattleExecutionPlan defensePlan =
            BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
                fixture.runtimeState.actionSlots,
                fixture.runtimeState.intentQueue,
                fixture.runtimeState
            );
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(defensePlan);
        if (fixture.slot.isUsed)
        {
            return false;
        }

        BattleEnemyIntent attackIntent = new BattleEnemyIntent(
            "interaction88_followup_attack",
            fixture.enemy,
            CreateCard(
                fixture.enemy,
                CardType.Attack,
                "interaction88_followup_enemy_attack"
            ),
            fixture.ally,
            1,
            2,
            1
        );
        BattleGuardSelectionResult selection = BattleGuardSelectionManager
            .SelectHandlingCardForEnemyIntent(
                fixture.runtimeState.actionSlots,
                attackIntent
            );
        return selection != null &&
            selection.selectionType == BattleGuardSelectionType.EnemySpecificGuard &&
            object.ReferenceEquals(selection.slot, fixture.slot);
    }

    private static PreparedAssignmentFixture CreatePreparedAssignmentFixture(
        string suffix,
        string playerCardType,
        string enemyCardType
    )
    {
        CharacterData ally = new CharacterData(
            "interaction88_prepared_ally_" + suffix,
            30,
            10,
            10
        );
        CharacterData enemy = new CharacterData(
            "interaction88_prepared_enemy_" + suffix,
            30,
            5,
            5
        );
        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(ally, null, enemy);
        List<BattleActionSlot> slots = BattleActionSlotManager
            .CreatePartyActionSlots(ally, null, 1);
        BattleEnemyIntent intent = new BattleEnemyIntent(
            "interaction88_prepared_intent_" + suffix,
            enemy,
            CreateCard(
                enemy,
                enemyCardType,
                "interaction88_prepared_enemy_card_" + suffix
            ),
            ally,
            1,
            1,
            1
        );
        runtimeState.SetActionSlots(slots);
        runtimeState.SetIntentQueue(new List<BattleEnemyIntent> { intent });

        BattleLifecycleController lifecycleController =
            new BattleLifecycleController(runtimeState);
        bool initialized = lifecycleController.TryInitializeToPrepare(
            out string failureMessage
        );
        if (!initialized ||
            runtimeState.LifecyclePhase != BattleLifecyclePhase.Prepare)
        {
            Debug.LogError(
                "Mode88 Prepared Fixture初始化失败：" + failureMessage
            );
            return null;
        }

        return new PreparedAssignmentFixture
        {
            runtimeState = runtimeState,
            ally = ally,
            enemy = enemy,
            slot = slots[0],
            playerCard = CreateCard(
                ally,
                playerCardType,
                "interaction88_prepared_player_card_" + suffix
            ),
            intent = intent
        };
    }

    private static ReactiveEnemyGuardFixture CreateReactiveEnemyGuardFixture(
        string suffix,
        string guardCardType,
        int enemySlotIndex = 1,
        int intentOrder = 1
    )
    {
        CharacterData ally = new CharacterData(
            "interaction88_reactive_ally_" + suffix,
            30,
            10,
            10
        );
        CharacterData enemy = new CharacterData(
            "interaction88_reactive_enemy_" + suffix,
            30,
            5,
            5
        );
        BattleActionSlot freeActionSlot = new BattleActionSlot(ally, 1);
        freeActionSlot.AssignFreeAction(
            ally,
            CreateCombatCard(
                ally,
                "interaction88_reactive_attack_" + suffix,
                CardType.Attack,
                8,
                AttackDeliveryMode.Melee
            ),
            enemy
        );
        BattleEnemyIntent guardIntent = new BattleEnemyIntent(
            "interaction88_reactive_intent_" + suffix,
            enemy,
            CreateCombatCard(
                enemy,
                "interaction88_reactive_guard_" + suffix,
                guardCardType,
                2
            ),
            ally,
            enemySlotIndex,
            intentOrder
        );
        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(ally, null, enemy);
        runtimeState.SetActionSlots(new List<BattleActionSlot> { freeActionSlot });
        runtimeState.SetIntentQueue(new List<BattleEnemyIntent> { guardIntent });

        return new ReactiveEnemyGuardFixture
        {
            runtimeState = runtimeState,
            ally = ally,
            enemy = enemy,
            freeActionSlot = freeActionSlot,
            guardIntent = guardIntent
        };
    }

    private static BattleEnemyIntent CreateReactiveEnemyIntent(
        ReactiveEnemyGuardFixture fixture,
        string suffix,
        string guardCardType,
        int enemySlotIndex,
        int intentOrder
    )
    {
        return new BattleEnemyIntent(
            "interaction88_reactive_intent_" + suffix,
            fixture.enemy,
            CreateCombatCard(
                fixture.enemy,
                "interaction88_reactive_guard_" + suffix,
                guardCardType,
                2
            ),
            fixture.ally,
            enemySlotIndex,
            intentOrder
        );
    }

    private static BattleExecutionItem CreateOnlyFreeActionItem(
        ReactiveEnemyGuardFixture fixture
    )
    {
        BattleExecutionPlan plan = BattleExecutionPlanManager
            .CreateSpeedBasedExecutionPlan(
                fixture.runtimeState.actionSlots,
                fixture.runtimeState.intentQueue,
                fixture.runtimeState
            );
        return plan != null && plan.executionItems != null &&
            plan.executionItems.Count == 1
            ? plan.executionItems[0]
            : null;
    }

    private static BattleExecutionItem GetOnlyItem(
        List<BattleActionSlot> actionSlots,
        List<BattleEnemyIntent> intentQueue
    )
    {
        BattleExecutionPlan plan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            actionSlots,
            intentQueue
        );
        return plan.executionItems != null && plan.executionItems.Count == 1
            ? plan.executionItems[0]
            : null;
    }

    private static BattleCardState CreateCard(
        CharacterData owner,
        string cardType,
        string instanceID,
        string attackDeliveryMode = null
    )
    {
        return new BattleCardState(
            owner,
            new CardTestData
            {
                cardID = instanceID,
                cardName = instanceID,
                cardType = cardType,
                attackDeliveryMode = attackDeliveryMode
            },
            instanceID
        );
    }

    private static BattleCardState CreateCombatCard(
        CharacterData owner,
        string instanceID,
        string cardType,
        int point,
        string attackDeliveryMode = null
    )
    {
        return BattleCardManager.CreateBattleCard(
            owner,
            new CardTestData
            {
                cardID = instanceID + "_data",
                cardName = instanceID,
                cardType = cardType,
                attackDeliveryMode = cardType == CardType.Attack
                    ? attackDeliveryMode
                    : string.Empty,
                isClashable = cardType == CardType.Attack,
                minPoint = point,
                maxPoint = point,
                cooldown = 1,
                damageFormula = cardType == CardType.Attack
                    ? "PointAsDamage"
                    : string.Empty,
                defenseFormula = cardType == CardType.Defense
                    ? "PointAsDefense"
                    : string.Empty,
                effects = new List<CardEffectData>()
            },
            instanceID
        );
    }

    private static TestContext CreateContext(string suffix)
    {
        return new TestContext
        {
            ally = new CharacterData("interaction88_ally_" + suffix, 30, 5, 5),
            enemy = new CharacterData("interaction88_enemy_" + suffix, 30, 5, 5)
        };
    }

    private sealed class TestContext
    {
        public CharacterData ally;
        public CharacterData enemy;
    }

    private sealed class PreparedAssignmentFixture
    {
        public BattleRuntimeState runtimeState;
        public CharacterData ally;
        public CharacterData enemy;
        public BattleActionSlot slot;
        public BattleCardState playerCard;
        public BattleEnemyIntent intent;
    }

    private sealed class ReactiveEnemyGuardFixture
    {
        public BattleRuntimeState runtimeState;
        public CharacterData ally;
        public CharacterData enemy;
        public BattleActionSlot freeActionSlot;
        public BattleEnemyIntent guardIntent;
    }
}
