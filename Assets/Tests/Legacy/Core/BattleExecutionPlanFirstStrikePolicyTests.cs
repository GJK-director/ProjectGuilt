// 脚本中文说明：验证完整 ExecutionItem 的 FirstStrike Priority Policy，不执行 Combat。
using System.Collections.Generic;
using UnityEngine;

public static class BattleExecutionPlanFirstStrikePolicyTests
{
    public static bool Run()
    {
        bool[] results = new bool[14];

        results[0] = FirstStrikeExecutionTests
            .FreeActionPlayerFirstStrikeUsesFirstStrikeTier();
        results[1] = FirstStrikeExecutionTests
            .FreeActionPlayerNormalAttackUsesNormalTier();
        results[2] = FirstStrikeExecutionTests
            .UnrespondedEnemyFirstStrikeUsesFirstStrikeTier();
        results[3] = FirstStrikeExecutionTests
            .UnrespondedEnemyNormalAttackUsesNormalTier();
        results[4] = FirstStrikeExecutionTests
            .RespondedPlayerFirstStrikeEnemyNormalUsesFirstStrikeTier();
        results[5] = FirstStrikeExecutionTests
            .RespondedPlayerNormalEnemyFirstStrikeUsesFirstStrikeTier();
        results[6] = FirstStrikeExecutionTests
            .RespondedFirstStrikeDefenseEnemyNormalUsesFirstStrikeTier();
        results[7] = FirstStrikeExecutionTests
            .RespondedNormalDefenseEnemyFirstStrikeUsesFirstStrikeTier();
        results[8] = FirstStrikeExecutionTests
            .RespondedBothNormalUsesNormalTier();
        results[9] = FirstStrikeExecutionTests
            .RespondedBothFirstStrikeUsesFirstStrikeTier();
        results[10] = FirstStrikeExecutionTests
            .FirstStrikeDoesNotChangeAttackVsDefenseInteraction();
        results[11] = FirstStrikeExecutionTests
            .FirstStrikeSortsBeforeLaterNormalFreeAction();
        results[12] = FirstStrikeExecutionTests
            .EnemyFirstStrikeRespondedItemStaysPairedAndSortsFirst();
        results[13] = ActionOrderExecutionTests.Run();

        string[] names =
        {
            "FreeAction Player FirstStrike Attack",
            "FreeAction Player Normal Attack",
            "Unresponded Enemy FirstStrike Attack",
            "Unresponded Enemy Normal Attack",
            "Responded Player FirstStrike + Enemy Normal",
            "Responded Player Normal + Enemy FirstStrike",
            "Responded FirstStrike Defense + Enemy Normal Attack",
            "Responded Normal Defense + Enemy FirstStrike Attack",
            "Responded 双方 Normal",
            "Responded 双方 FirstStrike",
            "FirstStrike 不改变 AttackVsDefense Interaction",
            "FirstStrike 排在后建 Normal FreeAction 前",
            "Enemy FirstStrike Responded Item 不拆 Pairing 且优先",
            "Action Order Formal Suite"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式89 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }

        Debug.Log("模式89 FirstStrike Priority Policy聚合结果：" + allPassed);
        return allPassed;
    }

}

// ActionOrderExecutionTests = 验证普通行动、响应行动和 FirstStrike 的统一排序契约。
// 只检查 ExecutionPlan metadata，不执行 Resolver 或 Presentation。
public static class ActionOrderExecutionTests
{
    public static bool Run()
    {
        bool[] results =
        {
            P6Slot1UnilateralPrecedesP6Slot2ResponseToE5(),
            SameSpeedUnilateralActionPrecedesResponse(),
            EqualEffectiveSpeedUnrespondedEnemyActionPrecedesResponse(),
            SameSpeedSlot1UsesBattlePosition(),
            SlotPriorityPrecedesBattlePosition(),
            SameSpeedActionsGroupBySlotThenBattlePosition(),
            SameSpeedResponsesUseSlotOrderInsteadOfAssignmentSequence(),
            SameSpeedFreeActionsUseSlotOrder(),
            FirstStrikePrecedesFasterNormalAction(),
            LaterFirstStrikeAssignmentPrecedesEarlierAssignment(),
            AbilityUsesNormalSpeedOrdering(),
            FirstStrikeResponseKeepsPairing(),
            UnrespondedDefenseAndDodgeStayOutOfPlan()
        };
        string[] names =
        {
            "P6 Slot1 unilateral before P6 Slot2 response to E5",
            "Same-speed unilateral action before response",
            "Equal effective speed unresponded enemy action before responded action",
            "Same-speed Slot1 uses battle position",
            "Slot priority before battle position",
            "Same-speed actions group by slot then battle position",
            "Same-speed responses use slot order instead of assignment sequence",
            "Same-speed FreeActions use slot order",
            "FirstStrike before faster Normal action",
            "Later FirstStrike assignment before earlier assignment",
            "Ability uses Normal speed ordering",
            "FirstStrike response keeps pairing",
            "Unresponded Defense and Dodge stay out of plan"
        };

        bool passed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log("Action Order Case " + (index + 1) + " " + names[index] + ": " + results[index]);
            passed &= results[index];
        }

        Debug.Log("Action Order Formal Suite: " + passed);
        return passed;
    }

    static bool P6Slot1UnilateralPrecedesP6Slot2ResponseToE5()
    {
        CharacterData player = Unit("order_a_player", 6);
        CharacterData enemy = Unit("order_a_enemy", 5);
        BattleEnemyIntent intent = AttackIntent(enemy, player, "order_a_enemy_attack", 1, 1);
        BattleActionSlot slot1 = FreeSlot(player, 1, CardType.Attack, "order_a_unilateral", false, enemy);
        BattleActionSlot slot2 = ResponseSlot(player, 2, CardType.Attack, "order_a_response", false, intent, 2);
        BattleExecutionPlan plan = Plan(
            player, null, enemy, null,
            new List<BattleActionSlot> { slot1, slot2 },
            new List<BattleEnemyIntent> { intent }
        );

        return HasTwoItems(plan) &&
            object.ReferenceEquals(plan.executionItems[0].actionSlot, slot1) &&
            object.ReferenceEquals(plan.executionItems[1].actionSlot, slot2) &&
            plan.executionItems[1].effectiveSpeed == 6 &&
            plan.executionItems[1].responsePriority == 1 &&
            object.ReferenceEquals(plan.executionItems[1].orderingActor, player);
    }

    static bool SameSpeedUnilateralActionPrecedesResponse()
    {
        CharacterData player = Unit("order_b_player", 5);
        CharacterData enemy = Unit("order_b_enemy", 5);
        BattleEnemyIntent intent = AttackIntent(enemy, player, "order_b_enemy_attack", 1, 1);
        BattleActionSlot response = ResponseSlot(player, 1, CardType.Attack, "order_b_response", false, intent, 2);
        BattleActionSlot unilateral = FreeSlot(player, 2, CardType.Attack, "order_b_unilateral", false, enemy);
        BattleExecutionPlan plan = Plan(
            player, null, enemy, null,
            new List<BattleActionSlot> { response, unilateral },
            new List<BattleEnemyIntent> { intent }
        );

        return HasTwoItems(plan) &&
            plan.executionItems[0].executionType == BattleExecutionItemType.FreeAction &&
            object.ReferenceEquals(plan.executionItems[0].actionSlot, unilateral) &&
            plan.executionItems[1].executionType == BattleExecutionItemType.RespondedEnemyIntent &&
            object.ReferenceEquals(plan.executionItems[1].actionSlot, response) &&
            plan.executionItems[1].responsePriority == 0;
    }

    static bool EqualEffectiveSpeedUnrespondedEnemyActionPrecedesResponse()
    {
        CharacterData player = Unit("order_c_player", 3);
        CharacterData enemy = Unit("order_c_enemy", 5);
        BattleEnemyIntent responseIntent = AttackIntent(enemy, player, "order_c_enemy_attack_1", 1, 1);
        BattleEnemyIntent secondIntent = AttackIntent(enemy, player, "order_c_enemy_attack_2", 2, 2);
        BattleActionSlot response = ResponseSlot(player, 1, CardType.Defense, "order_c_response", false, responseIntent, 1);
        BattleExecutionPlan plan = Plan(
            player, null, enemy, null,
            new List<BattleActionSlot> { response },
            new List<BattleEnemyIntent> { responseIntent, secondIntent }
        );

        return HasTwoItems(plan) &&
            plan.executionItems[0].executionType == BattleExecutionItemType.UnrespondedEnemyIntent &&
            object.ReferenceEquals(plan.executionItems[0].enemyIntent, secondIntent) &&
            plan.executionItems[0].actionSlotOrder == 2 &&
            plan.executionItems[1].executionType == BattleExecutionItemType.RespondedEnemyIntent &&
            object.ReferenceEquals(plan.executionItems[1].enemyIntent, responseIntent) &&
            object.ReferenceEquals(plan.executionItems[1].actionSlot, response) &&
            object.ReferenceEquals(plan.executionItems[1].orderingActor, enemy) &&
            plan.executionItems[1].actionSlotOrder == 1;
    }

    static bool SameSpeedSlot1UsesBattlePosition()
    {
        CharacterData player = Unit("order_d_player", 5);
        CharacterData enemy = Unit("order_d_enemy", 5);
        BattleEnemyIntent enemyIntent = AttackIntent(
            enemy,
            player,
            "order_d_enemy_attack",
            1,
            1
        );
        BattleActionSlot playerSlot = FreeSlot(player, 1, CardType.Attack, "order_d_player_attack", false, enemy);
        BattleExecutionPlan plan = Plan(
            player, null, enemy, null,
            new List<BattleActionSlot> { playerSlot },
            new List<BattleEnemyIntent> { enemyIntent }
        );

        return HasTwoItems(plan) &&
            plan.executionItems[0].executionType == BattleExecutionItemType.FreeAction &&
            object.ReferenceEquals(plan.executionItems[0].actionSlot, playerSlot) &&
            plan.executionItems[0].enemyIntent == null &&
            object.ReferenceEquals(plan.executionItems[0].orderingActor, player) &&
            plan.executionItems[0].actionSlotOrder == 1 &&
            plan.executionItems[0].actorPositionOrder == 1 &&
            plan.executionItems[1].executionType == BattleExecutionItemType.UnrespondedEnemyIntent &&
            plan.executionItems[1].actionSlot == null &&
            object.ReferenceEquals(plan.executionItems[1].enemyIntent, enemyIntent) &&
            object.ReferenceEquals(plan.executionItems[1].orderingActor, enemy) &&
            plan.executionItems[1].actionSlotOrder == 1 &&
            plan.executionItems[1].actorPositionOrder == 2;
    }

    static bool SlotPriorityPrecedesBattlePosition()
    {
        CharacterData player = Unit("order_e_player", 5);
        CharacterData enemy = Unit("order_e_enemy", 5);
        BattleEnemyIntent enemyIntent = AttackIntent(
            enemy,
            player,
            "order_e_enemy_attack",
            1,
            1
        );
        BattleActionSlot playerSlot2 = FreeSlot(player, 2, CardType.Attack, "order_e_player_attack", false, enemy);
        BattleExecutionPlan plan = Plan(
            player, null, enemy, null,
            new List<BattleActionSlot> { playerSlot2 },
            new List<BattleEnemyIntent> { enemyIntent }
        );

        return HasTwoItems(plan) &&
            plan.executionItems[0].executionType == BattleExecutionItemType.UnrespondedEnemyIntent &&
            plan.executionItems[0].actionSlot == null &&
            object.ReferenceEquals(plan.executionItems[0].enemyIntent, enemyIntent) &&
            object.ReferenceEquals(plan.executionItems[0].orderingActor, enemy) &&
            plan.executionItems[0].actionSlotOrder == 1 &&
            plan.executionItems[0].actorPositionOrder == 2 &&
            plan.executionItems[1].executionType == BattleExecutionItemType.FreeAction &&
            object.ReferenceEquals(plan.executionItems[1].actionSlot, playerSlot2) &&
            plan.executionItems[1].enemyIntent == null &&
            object.ReferenceEquals(plan.executionItems[1].orderingActor, player) &&
            plan.executionItems[1].actionSlotOrder == 2 &&
            plan.executionItems[1].actorPositionOrder == 1;
    }

    static bool SameSpeedActionsGroupBySlotThenBattlePosition()
    {
        CharacterData ally1 = Unit("order_f_ally_1", 5);
        CharacterData ally2 = Unit("order_f_ally_2", 5);
        CharacterData enemy1 = Unit("order_f_enemy_1", 5);
        CharacterData enemy2 = Unit("order_f_enemy_2", 5);
        BattleEnemyIntent enemy1Slot1 = AttackIntent(
            enemy1,
            ally1,
            "order_f_enemy_1_slot_1",
            1,
            1
        );
        BattleEnemyIntent enemy1Slot2 = AttackIntent(
            enemy1,
            ally1,
            "order_f_enemy_1_slot_2",
            2,
            2
        );
        BattleEnemyIntent enemy2Slot1 = AttackIntent(
            enemy2,
            ally1,
            "order_f_enemy_2_slot_1",
            1,
            3
        );
        BattleEnemyIntent enemy2Slot2 = AttackIntent(
            enemy2,
            ally1,
            "order_f_enemy_2_slot_2",
            2,
            4
        );
        List<BattleActionSlot> slots = new List<BattleActionSlot>
        {
            FreeSlot(ally2, 2, CardType.Attack, "order_f_a2_s2", false, enemy1),
            FreeSlot(ally1, 1, CardType.Attack, "order_f_a1_s1", false, enemy1),
            FreeSlot(ally2, 1, CardType.Attack, "order_f_a2_s1", false, enemy1),
            FreeSlot(ally1, 2, CardType.Attack, "order_f_a1_s2", false, enemy1)
        };
        BattleExecutionPlan plan = Plan(
            ally1, ally2, enemy1, enemy2, slots,
            new List<BattleEnemyIntent>
            {
                enemy2Slot2,
                enemy1Slot1,
                enemy2Slot1,
                enemy1Slot2
            }
        );
        CharacterData[] expectedActors = { ally1, ally2, enemy1, enemy2, ally1, ally2, enemy1, enemy2 };
        int[] expectedSlots = { 1, 1, 1, 1, 2, 2, 2, 2 };
        BattleExecutionItemType[] expectedTypes =
        {
            BattleExecutionItemType.FreeAction,
            BattleExecutionItemType.FreeAction,
            BattleExecutionItemType.UnrespondedEnemyIntent,
            BattleExecutionItemType.UnrespondedEnemyIntent,
            BattleExecutionItemType.FreeAction,
            BattleExecutionItemType.FreeAction,
            BattleExecutionItemType.UnrespondedEnemyIntent,
            BattleExecutionItemType.UnrespondedEnemyIntent
        };
        BattleActionSlot[] expectedSlotsByItem =
        {
            slots[1],
            slots[2],
            null,
            null,
            slots[3],
            slots[0],
            null,
            null
        };
        BattleEnemyIntent[] expectedIntents =
        {
            null,
            null,
            enemy1Slot1,
            enemy2Slot1,
            null,
            null,
            enemy1Slot2,
            enemy2Slot2
        };

        if (plan == null || plan.executionItems == null || plan.executionItems.Count != expectedActors.Length)
        {
            return false;
        }

        for (int index = 0; index < expectedActors.Length; index++)
        {
            BattleExecutionItem item = plan.executionItems[index];
            if (item == null ||
                item.executionType != expectedTypes[index] ||
                !object.ReferenceEquals(item.actionSlot, expectedSlotsByItem[index]) ||
                !object.ReferenceEquals(item.enemyIntent, expectedIntents[index]) ||
                !object.ReferenceEquals(item.orderingActor, expectedActors[index]) ||
                item.actionSlotOrder != expectedSlots[index])
            {
                return false;
            }
        }

        return true;
    }

    static bool SameSpeedResponsesUseSlotOrderInsteadOfAssignmentSequence()
    {
        CharacterData actor = Unit("order_g_actor", 5);
        CharacterData enemy1 = Unit("order_g_enemy_1", 5);
        CharacterData enemy2 = Unit("order_g_enemy_2", 5);
        BattleEnemyIntent intentA = AttackIntent(enemy1, actor, "order_g_intent_a", 1, 1);
        BattleEnemyIntent intentB = AttackIntent(enemy2, actor, "order_g_intent_b", 2, 2);
        BattleActionSlot responseA = ResponseSlot(actor, 1, CardType.Attack, "order_g_response_a", false, intentA, 10);
        BattleActionSlot responseB = ResponseSlot(actor, 2, CardType.Attack, "order_g_response_b", false, intentB, 20);
        BattleExecutionPlan plan = Plan(
            actor, null, enemy1, enemy2,
            new List<BattleActionSlot> { responseB, responseA },
            new List<BattleEnemyIntent> { intentA, intentB }
        );

        return HasTwoItems(plan) &&
            object.ReferenceEquals(plan.executionItems[0].actionSlot, responseA) &&
            object.ReferenceEquals(plan.executionItems[0].enemyIntent, intentA) &&
            plan.executionItems[0].actionSlotOrder == 1 &&
            plan.executionItems[0].actionAssignmentSequence == 10 &&
            object.ReferenceEquals(plan.executionItems[1].actionSlot, responseB) &&
            object.ReferenceEquals(plan.executionItems[1].enemyIntent, intentB) &&
            plan.executionItems[1].actionSlotOrder == 2 &&
            plan.executionItems[1].actionAssignmentSequence == 20;
    }

    static bool SameSpeedFreeActionsUseSlotOrder()
    {
        CharacterData actor = Unit("order_g_free_actor", 5);
        CharacterData enemy = Unit("order_g_free_enemy", 5);
        BattleActionSlot slot1 = FreeSlot(actor, 1, CardType.Attack, "order_g_free_slot1", false, enemy);
        BattleActionSlot slot2 = FreeSlot(actor, 2, CardType.Attack, "order_g_free_slot2", false, enemy);
        BattleExecutionPlan plan = Plan(
            actor, null, enemy, null,
            new List<BattleActionSlot> { slot2, slot1 },
            new List<BattleEnemyIntent>()
        );

        return HasTwoItems(plan) &&
            plan.executionItems[0].executionType == BattleExecutionItemType.FreeAction &&
            object.ReferenceEquals(plan.executionItems[0].actionSlot, slot1) &&
            plan.executionItems[0].actionSlotOrder == 1 &&
            plan.executionItems[1].executionType == BattleExecutionItemType.FreeAction &&
            object.ReferenceEquals(plan.executionItems[1].actionSlot, slot2) &&
            plan.executionItems[1].actionSlotOrder == 2;
    }

    static bool FirstStrikePrecedesFasterNormalAction()
    {
        CharacterData firstStrikeActor = Unit("order_h_first", 4);
        CharacterData normalActor = Unit("order_h_normal", 8);
        BattleActionSlot firstStrike = FreeSlot(firstStrikeActor, 1, CardType.Attack, "order_h_firststrike", true, normalActor);
        BattleActionSlot normal = FreeSlot(normalActor, 1, CardType.Attack, "order_h_normal_attack", false, firstStrikeActor);
        BattleExecutionPlan plan = Plan(
            firstStrikeActor, null, normalActor, null,
            new List<BattleActionSlot> { normal, firstStrike },
            new List<BattleEnemyIntent>()
        );

        return HasTwoItems(plan) &&
            object.ReferenceEquals(plan.executionItems[0].actionSlot, firstStrike) &&
            plan.executionItems[0].priorityTier == BattleExecutionPriorityTier.FirstStrike &&
            object.ReferenceEquals(plan.executionItems[1].actionSlot, normal);
    }

    static bool LaterFirstStrikeAssignmentPrecedesEarlierAssignment()
    {
        CharacterData actor = Unit("order_i_actor", 5);
        CharacterData enemy = Unit("order_i_enemy", 5);
        BattleActionSlot first = FreeSlot(actor, 1, CardType.Attack, "order_i_first", true, enemy);
        BattleActionSlot second = FreeSlot(actor, 2, CardType.Attack, "order_i_second", true, enemy);
        first.assignmentSequence = 10;
        second.assignmentSequence = 20;
        BattleExecutionPlan plan = Plan(
            actor, null, enemy, null,
            new List<BattleActionSlot> { first, second },
            new List<BattleEnemyIntent>()
        );

        return HasTwoItems(plan) &&
            object.ReferenceEquals(plan.executionItems[0].actionSlot, second) &&
            plan.executionItems[0].firstStrikeSourceSequence == 20 &&
            object.ReferenceEquals(plan.executionItems[1].actionSlot, first) &&
            plan.executionItems[1].firstStrikeSourceSequence == 10;
    }

    static bool AbilityUsesNormalSpeedOrdering()
    {
        CharacterData abilityActor = Unit("order_j_ability", 4);
        CharacterData normalActor = Unit("order_j_normal", 5);
        BattleActionSlot ability = FreeSlot(abilityActor, 1, CardType.Ability, "order_j_ability_card", false, abilityActor);
        BattleActionSlot normal = FreeSlot(normalActor, 1, CardType.Attack, "order_j_normal_card", false, abilityActor);
        BattleExecutionPlan plan = Plan(
            abilityActor, null, normalActor, null,
            new List<BattleActionSlot> { ability, normal },
            new List<BattleEnemyIntent>()
        );

        return HasTwoItems(plan) &&
            object.ReferenceEquals(plan.executionItems[0].actionSlot, normal) &&
            plan.executionItems[0].priorityTier == BattleExecutionPriorityTier.Normal &&
            object.ReferenceEquals(plan.executionItems[1].actionSlot, ability) &&
            plan.executionItems[1].priorityTier == BattleExecutionPriorityTier.Normal;
    }

    static bool FirstStrikeResponseKeepsPairing()
    {
        CharacterData player = Unit("order_k_player", 5);
        CharacterData enemy = Unit("order_k_enemy", 5);
        BattleEnemyIntent intent = AttackIntent(enemy, player, "order_k_enemy_attack", 1, 1);
        BattleActionSlot response = ResponseSlot(player, 1, CardType.Attack, "order_k_firststrike_response", true, intent, 1);
        BattleExecutionPlan plan = Plan(
            player, null, enemy, null,
            new List<BattleActionSlot> { response },
            new List<BattleEnemyIntent> { intent }
        );

        return plan != null && plan.executionItems != null &&
            plan.executionItems.Count == 1 &&
            object.ReferenceEquals(plan.executionItems[0].actionSlot, response) &&
            object.ReferenceEquals(plan.executionItems[0].enemyIntent, intent) &&
            plan.executionItems[0].priorityTier == BattleExecutionPriorityTier.FirstStrike;
    }

    static bool UnrespondedDefenseAndDodgeStayOutOfPlan()
    {
        CharacterData player = Unit("order_l_player", 5);
        CharacterData enemy = Unit("order_l_enemy", 5);
        BattleEnemyIntent defense = GuardIntent(enemy, player, CardType.Defense, "order_l_defense", 1, 1);
        BattleEnemyIntent dodge = GuardIntent(enemy, player, CardType.Dodge, "order_l_dodge", 2, 2);
        BattleExecutionPlan plan = Plan(
            player, null, enemy, null,
            new List<BattleActionSlot>(),
            new List<BattleEnemyIntent> { defense, dodge }
        );

        return plan != null && plan.executionItems != null &&
            plan.executionItems.Count == 0 && !defense.isResponded && !dodge.isResponded;
    }

    static BattleExecutionPlan Plan(
        CharacterData allyA,
        CharacterData allyB,
        CharacterData enemy,
        CharacterData enemy2,
        List<BattleActionSlot> slots,
        List<BattleEnemyIntent> intents
    )
    {
        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(allyA, allyB, enemy, enemy2);
        runtimeState.SetActionSlots(slots);
        runtimeState.SetIntentQueue(intents);
        return BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(slots, intents, runtimeState);
    }

    static BattleActionSlot FreeSlot(
        CharacterData actor,
        int slotIndex,
        string cardType,
        string cardID,
        bool firstStrike,
        CharacterData target
    )
    {
        BattleActionSlot slot = new BattleActionSlot(actor, slotIndex);
        slot.AssignFreeAction(
            actor,
            TestCardFactory.CreateState(actor, cardID, cardType, 1, AttackDeliveryMode.Melee, 0, firstStrike),
            target
        );
        return slot;
    }

    static BattleActionSlot ResponseSlot(
        CharacterData actor,
        int slotIndex,
        string cardType,
        string cardID,
        bool firstStrike,
        BattleEnemyIntent intent,
        long assignmentSequence
    )
    {
        BattleActionSlot slot = new BattleActionSlot(actor, slotIndex);
        slot.AssignResponse(
            actor,
            TestCardFactory.CreateState(actor, cardID, cardType, 1, AttackDeliveryMode.Melee, 0, firstStrike),
            intent,
            false
        );
        slot.assignmentSequence = assignmentSequence;
        intent.MarkResponded();
        return slot;
    }

    static BattleEnemyIntent AttackIntent(
        CharacterData enemy,
        CharacterData target,
        string cardID,
        int enemySlotIndex,
        int intentOrder
    )
    {
        return TestIntentFactory.Create(
            cardID + "_intent",
            enemy,
            TestCardFactory.CreateState(enemy, cardID, CardType.Attack, 1, AttackDeliveryMode.Melee, 0, false),
            target,
            enemySlotIndex,
            intentOrder,
            enemySlotIndex
        );
    }

    static BattleEnemyIntent GuardIntent(
        CharacterData enemy,
        CharacterData target,
        string cardType,
        string cardID,
        int enemySlotIndex,
        int intentOrder
    )
    {
        return TestIntentFactory.Create(
            cardID + "_intent",
            enemy,
            TestCardFactory.CreateState(enemy, cardID, cardType, 1, AttackDeliveryMode.Melee, 0, false),
            target,
            enemySlotIndex,
            intentOrder,
            enemySlotIndex
        );
    }

    static CharacterData Unit(string id, int speed)
    {
        return TestCharacterFactory.Create(id, 30, speed, speed);
    }

    static bool HasTwoItems(BattleExecutionPlan plan)
    {
        return plan != null && plan.executionItems != null && plan.executionItems.Count == 2;
    }
}
