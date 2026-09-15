using System.Collections.Generic;
using UnityEngine;

public static class BattlePlanningOrderSnapshotTests
{
    public static bool Run()
    {
        bool[] results =
        {
            VerifyMixedSpeedResponseOrdering(),
            VerifyEqualSpeedResponsePrivilege(),
            VerifySlowerResponseUsesEnemySlotOrder(),
            VerifySameSpeedMultiActorOrdering(),
            VerifyFirstStrikePrecedesFasterNormal(),
            VerifyLaterFirstStrikePrecedesEarlierFirstStrike(),
            VerifyAbilityRemainsNormal(),
            VerifyRespondedFirstStrikeStaysPaired(),
            VerifyPassiveAndUnilateralDefenseDodgeAreZero(),
            VerifyUnrespondedEnemyDefenseDodgeAreZero(),
            VerifySnapshotIsReadOnly(),
            VerifyExecutionAndSnapshotParity()
        };
        string[] names =
        {
            "A mixed-speed response ordering",
            "B true equal-speed response privilege",
            "C slower response uses enemy slot order",
            "D same-speed multi-actor slot ordering",
            "E FirstStrike precedes faster Normal",
            "F later FirstStrike source first",
            "G Ability remains Normal",
            "H responded FirstStrike stays paired",
            "I passive and unilateral Defense/Dodge are zero",
            "J unresponded enemy Defense/Dodge are zero",
            "K snapshot does not mutate planning state",
            "L Execution and Snapshot parity"
        };

        bool passed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "Mode105 Planning Order Snapshot " + names[index] + ": " +
                results[index]
            );
            passed &= results[index];
        }

        return passed;
    }

    public static bool VerifyMixedSpeedResponseOrdering()
    {
        TestContext context = CreateFourUnitContext(6, 5, 5, 5);
        BattleEnemyIntent responseIntent = CreateIntent(
            "snapshot_a_response",
            context.enemy1,
            CardType.Attack,
            context.ally1,
            1,
            1,
            1
        );
        BattleEnemyIntent unrespondedIntent = CreateIntent(
            "snapshot_a_unresponded",
            context.enemy1,
            CardType.Attack,
            context.ally1,
            2,
            2,
            2
        );
        BattleActionSlot unilateral = FreeSlot(
            context.ally1,
            1,
            Card(context.ally1, CardType.Attack, "snapshot_a_free"),
            context.enemy1,
            1
        );
        BattleActionSlot response = ResponseSlot(
            context.ally1,
            2,
            Card(context.ally1, CardType.Attack, "snapshot_a_response_card"),
            responseIntent,
            2
        );

        BattlePlanningOrderSnapshot snapshot = Snapshot(
            context,
            new List<BattleActionSlot> { response, unilateral },
            new List<BattleEnemyIntent> { unrespondedIntent, responseIntent }
        );
        return Order(snapshot.GetActionSlotDisplayOrder(unilateral)) == 1 &&
            Order(snapshot.GetActionSlotDisplayOrder(response)) == 2 &&
            Order(snapshot.GetEnemyIntentDisplayOrder(responseIntent)) == 2 &&
            Order(snapshot.GetEnemyIntentDisplayOrder(unrespondedIntent)) == 3;
    }

    public static bool VerifyEqualSpeedResponsePrivilege()
    {
        TestContext context = CreateFourUnitContext(5, 5, 5, 5);
        BattleEnemyIntent responseIntent = CreateIntent(
            "snapshot_b_response",
            context.enemy1,
            CardType.Attack,
            context.ally1,
            1,
            1,
            1
        );
        BattleActionSlot unilateral = FreeSlot(
            context.ally1,
            1,
            Card(context.ally1, CardType.Attack, "snapshot_b_free"),
            context.enemy1,
            1
        );
        BattleActionSlot response = ResponseSlot(
            context.ally1,
            2,
            Card(context.ally1, CardType.Attack, "snapshot_b_response_card"),
            responseIntent,
            2
        );
        BattlePlanningOrderSnapshot snapshot = Snapshot(
            context,
            new List<BattleActionSlot> { unilateral, response },
            new List<BattleEnemyIntent> { responseIntent }
        );
        return Order(snapshot.GetActionSlotDisplayOrder(response)) == 1 &&
            Order(snapshot.GetEnemyIntentDisplayOrder(responseIntent)) == 1 &&
            Order(snapshot.GetActionSlotDisplayOrder(unilateral)) == 2;
    }

    public static bool VerifySlowerResponseUsesEnemySlotOrder()
    {
        TestContext context = CreateFourUnitContext(3, 3, 5, 5);
        BattleEnemyIntent responseIntent = CreateIntent(
            "snapshot_c_response",
            context.enemy1,
            CardType.Attack,
            context.ally1,
            1,
            1,
            1
        );
        BattleEnemyIntent unrespondedIntent = CreateIntent(
            "snapshot_c_unresponded",
            context.enemy1,
            CardType.Attack,
            context.ally1,
            2,
            2,
            2
        );
        BattleActionSlot response = ResponseSlot(
            context.ally1,
            1,
            Card(context.ally1, CardType.Attack, "snapshot_c_response_card"),
            responseIntent,
            1
        );
        BattlePlanningOrderSnapshot snapshot = Snapshot(
            context,
            new List<BattleActionSlot> { response },
            new List<BattleEnemyIntent> { unrespondedIntent, responseIntent }
        );
        return Order(snapshot.GetActionSlotDisplayOrder(response)) == 1 &&
            Order(snapshot.GetEnemyIntentDisplayOrder(responseIntent)) == 1 &&
            Order(snapshot.GetEnemyIntentDisplayOrder(unrespondedIntent)) == 2;
    }

    public static bool VerifySameSpeedMultiActorOrdering()
    {
        TestContext context = CreateFourUnitContext(5, 5, 5, 5);
        List<BattleActionSlot> slots = new List<BattleActionSlot>
        {
            FreeSlot(
                context.ally2,
                2,
                Card(context.ally2, CardType.Attack, "snapshot_d_ally2_s2"),
                context.enemy1,
                6
            ),
            FreeSlot(
                context.ally1,
                1,
                Card(context.ally1, CardType.Attack, "snapshot_d_ally1_s1"),
                context.enemy1,
                1
            ),
            FreeSlot(
                context.ally2,
                1,
                Card(context.ally2, CardType.Attack, "snapshot_d_ally2_s1"),
                context.enemy1,
                3
            ),
            FreeSlot(
                context.ally1,
                2,
                Card(context.ally1, CardType.Attack, "snapshot_d_ally1_s2"),
                context.enemy1,
                5
            )
        };
        List<BattleEnemyIntent> intents = new List<BattleEnemyIntent>
        {
            CreateIntent(
                "snapshot_d_enemy2_s2",
                context.enemy2,
                CardType.Attack,
                context.ally1,
                2,
                2,
                2
            ),
            CreateIntent(
                "snapshot_d_enemy1_s1",
                context.enemy1,
                CardType.Attack,
                context.ally1,
                1,
                1,
                1
            ),
            CreateIntent(
                "snapshot_d_enemy2_s1",
                context.enemy2,
                CardType.Attack,
                context.ally1,
                2,
                1,
                1
            ),
            CreateIntent(
                "snapshot_d_enemy1_s2",
                context.enemy1,
                CardType.Attack,
                context.ally1,
                1,
                2,
                2
            )
        };
        BattlePlanningOrderSnapshot snapshot = Snapshot(context, slots, intents);
        BattleActionSlot ally1Slot1 = FindSlot(slots, context.ally1, 1);
        BattleActionSlot ally1Slot2 = FindSlot(slots, context.ally1, 2);
        BattleActionSlot ally2Slot1 = FindSlot(slots, context.ally2, 1);
        BattleActionSlot ally2Slot2 = FindSlot(slots, context.ally2, 2);
        BattleEnemyIntent enemy1Slot1 = FindIntent(intents, context.enemy1, 1);
        BattleEnemyIntent enemy1Slot2 = FindIntent(intents, context.enemy1, 2);
        BattleEnemyIntent enemy2Slot1 = FindIntent(intents, context.enemy2, 1);
        BattleEnemyIntent enemy2Slot2 = FindIntent(intents, context.enemy2, 2);
        return Order(snapshot.GetActionSlotDisplayOrder(ally1Slot1)) == 1 &&
            Order(snapshot.GetActionSlotDisplayOrder(ally2Slot1)) == 2 &&
            Order(snapshot.GetEnemyIntentDisplayOrder(enemy1Slot1)) == 3 &&
            Order(snapshot.GetEnemyIntentDisplayOrder(enemy2Slot1)) == 4 &&
            Order(snapshot.GetActionSlotDisplayOrder(ally1Slot2)) == 5 &&
            Order(snapshot.GetActionSlotDisplayOrder(ally2Slot2)) == 6 &&
            Order(snapshot.GetEnemyIntentDisplayOrder(enemy1Slot2)) == 7 &&
            Order(snapshot.GetEnemyIntentDisplayOrder(enemy2Slot2)) == 8;
    }

    public static bool VerifyFirstStrikePrecedesFasterNormal()
    {
        TestContext context = CreateFourUnitContext(1, 10, 5, 5);
        BattleActionSlot firstStrike = FreeSlot(
            context.ally1,
            1,
            Card(context.ally1, CardType.Attack, "snapshot_e_first", true),
            context.enemy1,
            1
        );
        BattleActionSlot normal = FreeSlot(
            context.ally2,
            1,
            Card(context.ally2, CardType.Attack, "snapshot_e_normal"),
            context.enemy1,
            2
        );
        BattlePlanningOrderSnapshot snapshot = Snapshot(
            context,
            new List<BattleActionSlot> { normal, firstStrike },
            new List<BattleEnemyIntent>()
        );
        return Order(snapshot.GetActionSlotDisplayOrder(firstStrike)) == 1 &&
            Order(snapshot.GetActionSlotDisplayOrder(normal)) == 2;
    }

    public static bool VerifyLaterFirstStrikePrecedesEarlierFirstStrike()
    {
        TestContext context = CreateFourUnitContext(5, 5, 5, 5);
        BattleActionSlot first = FreeSlot(
            context.ally1,
            1,
            Card(context.ally1, CardType.Attack, "snapshot_f_first" , true),
            context.enemy1,
            1
        );
        BattleActionSlot second = FreeSlot(
            context.ally1,
            2,
            Card(context.ally1, CardType.Attack, "snapshot_f_second", true),
            context.enemy1,
            2
        );
        BattlePlanningOrderSnapshot snapshot = Snapshot(
            context,
            new List<BattleActionSlot> { first, second },
            new List<BattleEnemyIntent>()
        );
        return Order(snapshot.GetActionSlotDisplayOrder(second)) == 1 &&
            Order(snapshot.GetActionSlotDisplayOrder(first)) == 2;
    }

    public static bool VerifyAbilityRemainsNormal()
    {
        TestContext context = CreateFourUnitContext(4, 5, 5, 5);
        BattleActionSlot ability = FreeSlot(
            context.ally1,
            1,
            Card(context.ally1, CardType.Ability, "snapshot_g_ability"),
            context.ally1,
            1
        );
        BattleActionSlot attack = FreeSlot(
            context.ally2,
            1,
            Card(context.ally2, CardType.Attack, "snapshot_g_attack"),
            context.enemy1,
            2
        );
        BattlePlanningOrderSnapshot snapshot = Snapshot(
            context,
            new List<BattleActionSlot> { ability, attack },
            new List<BattleEnemyIntent>()
        );
        List<BattleActionOrderCandidate> candidates =
            BattleActionOrderResolver.Resolve(
                new List<BattleActionSlot> { ability, attack },
                new List<BattleEnemyIntent>(),
                context.runtimeState
            );
        return Order(snapshot.GetActionSlotDisplayOrder(attack)) == 1 &&
            Order(snapshot.GetActionSlotDisplayOrder(ability)) == 2 &&
            candidates.Count == 2 &&
            candidates[1].priorityTier == BattleExecutionPriorityTier.Normal;
    }

    public static bool VerifyRespondedFirstStrikeStaysPaired()
    {
        TestContext context = CreateFourUnitContext(2, 10, 5, 5);
        BattleEnemyIntent enemyFirstStrike = CreateIntent(
            "snapshot_h_enemy_first",
            context.enemy1,
            CardType.Attack,
            context.ally1,
            1,
            1,
            1,
            true
        );
        BattleActionSlot responseToEnemyFirstStrike = ResponseSlot(
            context.ally1,
            1,
            Card(context.ally1, CardType.Defense, "snapshot_h_response"),
            enemyFirstStrike,
            1
        );
        BattleActionSlot normal = FreeSlot(
            context.ally2,
            1,
            Card(context.ally2, CardType.Attack, "snapshot_h_normal"),
            context.enemy1,
            2
        );
        BattlePlanningOrderSnapshot enemySourceSnapshot = Snapshot(
            context,
            new List<BattleActionSlot> { normal, responseToEnemyFirstStrike },
            new List<BattleEnemyIntent> { enemyFirstStrike }
        );
        bool enemySourcePaired =
            Order(enemySourceSnapshot.GetActionSlotDisplayOrder(
                responseToEnemyFirstStrike
            )) == 1 &&
            Order(enemySourceSnapshot.GetEnemyIntentDisplayOrder(
                enemyFirstStrike
            )) == 1 &&
            Order(enemySourceSnapshot.GetActionSlotDisplayOrder(normal)) == 2;

        TestContext playerSourceContext = CreateFourUnitContext(2, 10, 5, 5);
        BattleEnemyIntent normalIntent = CreateIntent(
            "snapshot_h_player_first",
            playerSourceContext.enemy1,
            CardType.Attack,
            playerSourceContext.ally1,
            1,
            1,
            1
        );
        BattleActionSlot playerFirstStrike = ResponseSlot(
            playerSourceContext.ally1,
            1,
            Card(
                playerSourceContext.ally1,
                CardType.Attack,
                "snapshot_h_player_source",
                true
            ),
            normalIntent,
            1
        );
        BattlePlanningOrderSnapshot playerSourceSnapshot = Snapshot(
            playerSourceContext,
            new List<BattleActionSlot> { playerFirstStrike },
            new List<BattleEnemyIntent> { normalIntent }
        );
        return enemySourcePaired &&
            Order(playerSourceSnapshot.GetActionSlotDisplayOrder(
                playerFirstStrike
            )) == 1 &&
            Order(playerSourceSnapshot.GetEnemyIntentDisplayOrder(
                normalIntent
            )) == 1;
    }

    public static bool VerifyPassiveAndUnilateralDefenseDodgeAreZero()
    {
        TestContext context = CreateFourUnitContext(5, 5, 5, 5);
        BattleActionSlot passiveDefense = new BattleActionSlot(
            context.ally1,
            1
        );
        passiveDefense.AssignPassiveGuard(
            context.ally1,
            Card(context.ally1, CardType.Defense, "snapshot_i_passive")
        );
        BattleActionSlot unilateralDodge = FreeSlot(
            context.ally1,
            2,
            Card(context.ally1, CardType.Dodge, "snapshot_i_dodge"),
            context.ally1,
            2
        );
        BattleActionSlot empty = new BattleActionSlot(context.ally1, 3);
        BattleActionSlot attack = FreeSlot(
            context.ally1,
            4,
            Card(context.ally1, CardType.Attack, "snapshot_i_attack"),
            context.enemy1,
            4
        );
        BattlePlanningOrderSnapshot snapshot = Snapshot(
            context,
            new List<BattleActionSlot>
            {
                attack,
                empty,
                unilateralDodge,
                passiveDefense
            },
            new List<BattleEnemyIntent>()
        );
        return Order(snapshot.GetActionSlotDisplayOrder(passiveDefense)) == 0 &&
            Order(snapshot.GetActionSlotDisplayOrder(unilateralDodge)) == 0 &&
            !snapshot.GetActionSlotDisplayOrder(empty).HasValue &&
            Order(snapshot.GetActionSlotDisplayOrder(attack)) == 1;
    }

    public static bool VerifyUnrespondedEnemyDefenseDodgeAreZero()
    {
        TestContext context = CreateFourUnitContext(5, 5, 5, 5);
        BattleEnemyIntent defense = CreateIntent(
            "snapshot_j_defense",
            context.enemy1,
            CardType.Defense,
            context.ally1,
            1,
            1,
            1
        );
        BattleEnemyIntent dodge = CreateIntent(
            "snapshot_j_dodge",
            context.enemy2,
            CardType.Dodge,
            context.ally1,
            2,
            2,
            1
        );
        BattleEnemyIntent attack = CreateIntent(
            "snapshot_j_attack",
            context.enemy1,
            CardType.Attack,
            context.ally1,
            3,
            3,
            2
        );
        BattlePlanningOrderSnapshot snapshot = Snapshot(
            context,
            new List<BattleActionSlot>(),
            new List<BattleEnemyIntent> { attack, dodge, defense }
        );
        List<BattleActionOrderCandidate> candidates =
            BattleActionOrderResolver.Resolve(
                new List<BattleActionSlot>(),
                new List<BattleEnemyIntent> { attack, dodge, defense },
                context.runtimeState
            );
        return Order(snapshot.GetEnemyIntentDisplayOrder(defense)) == 0 &&
            Order(snapshot.GetEnemyIntentDisplayOrder(dodge)) == 0 &&
            Order(snapshot.GetEnemyIntentDisplayOrder(attack)) == 1 &&
            candidates.Count == 1 &&
            object.ReferenceEquals(candidates[0].enemyIntent, attack);
    }

    public static bool VerifySnapshotIsReadOnly()
    {
        TestContext context = CreateFourUnitContext(5, 5, 5, 5);
        BattleEnemyIntent intent = CreateIntent(
            "snapshot_k_anomaly",
            context.enemy1,
            CardType.Attack,
            context.ally1,
            1,
            1,
            1
        );
        intent.SetActualTarget(context.ally2, 2);
        intent.MarkConsumedAsReactiveGuard();
        BattleActionSlot malformedResponse = ResponseSlot(
            context.ally1,
            1,
            Card(context.ally1, CardType.Defense, "snapshot_k_card"),
            intent,
            7
        );
        malformedResponse.slotType = BattleActionSlotType.FreeAction;
        bool beforeResponded = intent.isResponded;
        bool beforeConsumed = intent.isConsumedAsReactiveGuard;
        CharacterData beforeTarget = intent.actualTargetCharacter;
        int beforeTargetSlot = intent.actualTargetSlotIndex;
        BattleActionSlotType beforeSlotType = malformedResponse.slotType;
        BattleEnemyIntent beforeSlotIntent = malformedResponse.enemyIntent;
        long beforeSequence = malformedResponse.assignmentSequence;
        bool beforeUsed = malformedResponse.isUsed;

        BattlePlanningOrderSnapshot snapshot = Snapshot(
            context,
            new List<BattleActionSlot> { malformedResponse },
            new List<BattleEnemyIntent> { intent }
        );
        int? malformedIntentOrder =
            snapshot.GetEnemyIntentDisplayOrder(intent);
        int? malformedSlotOrder =
            snapshot.GetActionSlotDisplayOrder(malformedResponse);
        return snapshot != null &&
            beforeResponded == true &&
            beforeResponded == intent.isResponded &&
            beforeConsumed == intent.isConsumedAsReactiveGuard &&
            object.ReferenceEquals(beforeTarget, intent.actualTargetCharacter) &&
            beforeTargetSlot == intent.actualTargetSlotIndex &&
            beforeSlotType == malformedResponse.slotType &&
            object.ReferenceEquals(beforeSlotIntent, malformedResponse.enemyIntent) &&
            beforeSequence == malformedResponse.assignmentSequence &&
            beforeUsed == malformedResponse.isUsed &&
            malformedIntentOrder.HasValue &&
            malformedIntentOrder.Value == 1 &&
            malformedSlotOrder.HasValue &&
            malformedSlotOrder.Value == 0;
    }

    public static bool VerifyExecutionAndSnapshotParity()
    {
        TestContext context = CreateFourUnitContext(6, 5, 5, 5);
        BattleEnemyIntent responseIntent = CreateIntent(
            "snapshot_l_response",
            context.enemy1,
            CardType.Attack,
            context.ally1,
            1,
            1,
            1
        );
        BattleEnemyIntent unrespondedIntent = CreateIntent(
            "snapshot_l_unresponded",
            context.enemy1,
            CardType.Attack,
            context.ally1,
            2,
            2,
            2
        );
        BattleActionSlot freeAttack = FreeSlot(
            context.ally1,
            1,
            Card(context.ally1, CardType.Attack, "snapshot_l_free"),
            context.enemy1,
            1
        );
        BattleActionSlot freeDefense = FreeSlot(
            context.ally2,
            2,
            Card(context.ally2, CardType.Defense, "snapshot_l_defense"),
            context.ally2,
            3
        );
        BattleActionSlot response = ResponseSlot(
            context.ally1,
            2,
            Card(context.ally1, CardType.Attack, "snapshot_l_response_card"),
            responseIntent,
            2
        );
        List<BattleActionSlot> slots =
            new List<BattleActionSlot> { freeDefense, response, freeAttack };
        List<BattleEnemyIntent> intents =
            new List<BattleEnemyIntent> { unrespondedIntent, responseIntent };
        BattleExecutionPlan plan =
            BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
                slots,
                intents,
                context.runtimeState
            );
        BattlePlanningOrderSnapshot snapshot =
            BattleExecutionPlanManager.CreatePlanningOrderSnapshot(
                slots,
                intents,
                context.runtimeState
            );
        if (plan == null || snapshot == null || plan.executionItems == null)
        {
            return false;
        }

        int filteredRank = 0;
        foreach (BattleExecutionItem item in plan.executionItems)
        {
            if (item == null)
            {
                return false;
            }

            if (item.executionType == BattleExecutionItemType.RespondedEnemyIntent)
            {
                int? actionSlotOrder =
                    snapshot.GetActionSlotDisplayOrder(item.actionSlot);
                int? enemyIntentOrder =
                    snapshot.GetEnemyIntentDisplayOrder(item.enemyIntent);
                if (!actionSlotOrder.HasValue ||
                    !enemyIntentOrder.HasValue ||
                    actionSlotOrder.Value <= 0 ||
                    enemyIntentOrder.Value <= 0 ||
                    actionSlotOrder.Value != enemyIntentOrder.Value)
                {
                    return false;
                }
                filteredRank++;
                if (actionSlotOrder.Value != filteredRank)
                {
                    return false;
                }
                continue;
            }

            if (item.executionType == BattleExecutionItemType.FreeAction)
            {
                int? actionSlotOrder =
                    snapshot.GetActionSlotDisplayOrder(item.actionSlot);
                if (!actionSlotOrder.HasValue)
                {
                    return false;
                }
                if (actionSlotOrder.Value == 0)
                {
                    continue;
                }
                if (actionSlotOrder.Value < 0)
                {
                    return false;
                }
                filteredRank++;
                if (actionSlotOrder.Value != filteredRank)
                {
                    return false;
                }
                continue;
            }

            if (item.executionType ==
                BattleExecutionItemType.UnrespondedEnemyIntent)
            {
                int? enemyIntentOrder =
                    snapshot.GetEnemyIntentDisplayOrder(item.enemyIntent);
                if (!enemyIntentOrder.HasValue)
                {
                    return false;
                }
                if (enemyIntentOrder.Value == 0)
                {
                    continue;
                }
                if (enemyIntentOrder.Value < 0)
                {
                    return false;
                }
                filteredRank++;
                if (enemyIntentOrder.Value != filteredRank)
                {
                    return false;
                }
                continue;
            }

            return false;
        }

        return true;
    }

    static bool IsActiveFreeAction(BattleActionSlot slot)
    {
        string cardType = slot != null && slot.cardState != null &&
            slot.cardState.cardData != null
                ? slot.cardState.cardData.cardType
                : string.Empty;
        return cardType != CardType.Defense && cardType != CardType.Dodge &&
            !string.IsNullOrEmpty(cardType);
    }

    static bool IsActiveEnemyIntent(BattleEnemyIntent intent)
    {
        return intent != null && intent.enemyCardState != null &&
            intent.enemyCardState.cardData != null &&
            intent.enemyCardState.cardData.cardType == CardType.Attack;
    }

    static BattlePlanningOrderSnapshot Snapshot(
        TestContext context,
        List<BattleActionSlot> slots,
        List<BattleEnemyIntent> intents
    )
    {
        return BattleExecutionPlanManager.CreatePlanningOrderSnapshot(
            slots,
            intents,
            context.runtimeState
        );
    }

    static BattleEnemyIntent CreateIntent(
        string id,
        CharacterData enemy,
        string cardType,
        CharacterData target,
        int originalTargetSlot,
        int intentOrder,
        int enemySlot,
        bool firstStrike = false
    )
    {
        return new BattleEnemyIntent(
            id,
            enemy,
            Card(enemy, cardType, id + "_card", firstStrike),
            target,
            originalTargetSlot,
            intentOrder,
            enemySlot
        );
    }

    static BattleActionSlot ResponseSlot(
        CharacterData actor,
        int slotIndex,
        BattleCardState card,
        BattleEnemyIntent intent,
        long assignmentSequence
    )
    {
        BattleActionSlot slot = new BattleActionSlot(actor, slotIndex);
        slot.AssignResponse(actor, card, intent, false);
        slot.assignmentSequence = assignmentSequence;
        intent.MarkResponded();
        return slot;
    }

    static BattleActionSlot FreeSlot(
        CharacterData actor,
        int slotIndex,
        BattleCardState card,
        CharacterData target,
        long assignmentSequence
    )
    {
        BattleActionSlot slot = new BattleActionSlot(actor, slotIndex);
        slot.AssignFreeAction(actor, card, target);
        slot.assignmentSequence = assignmentSequence;
        return slot;
    }

    static BattleCardState Card(
        CharacterData owner,
        string cardType,
        string id,
        bool firstStrike = false
    )
    {
        CardTestData data = new CardTestData
        {
            cardID = id,
            cardName = id,
            cardType = cardType,
            traits = firstStrike
                ? new[] { BattleCardTrait.FirstStrike }
                : null
        };
        return new BattleCardState(owner, data, id);
    }

    static TestContext CreateFourUnitContext(
        int ally1Speed,
        int ally2Speed,
        int enemy1Speed,
        int enemy2Speed
    )
    {
        TestContext context = new TestContext
        {
            ally1 = new CharacterData("snapshot_ally1", 30, ally1Speed, ally1Speed),
            ally2 = new CharacterData("snapshot_ally2", 30, ally2Speed, ally2Speed),
            enemy1 = new CharacterData("snapshot_enemy1", 30, enemy1Speed, enemy1Speed),
            enemy2 = new CharacterData("snapshot_enemy2", 30, enemy2Speed, enemy2Speed),
            runtimeState = new BattleRuntimeState()
        };
        context.runtimeState.SetCharacters(
            context.ally1,
            context.ally2,
            context.enemy1,
            context.enemy2
        );
        return context;
    }

    static BattleActionSlot FindSlot(
        List<BattleActionSlot> slots,
        CharacterData actor,
        int slotIndex
    )
    {
        foreach (BattleActionSlot slot in slots)
        {
            if (slot != null && object.ReferenceEquals(slot.actor, actor) &&
                slot.slotIndex == slotIndex)
            {
                return slot;
            }
        }
        return null;
    }

    static BattleEnemyIntent FindIntent(
        List<BattleEnemyIntent> intents,
        CharacterData enemy,
        int enemySlotIndex
    )
    {
        foreach (BattleEnemyIntent intent in intents)
        {
            if (intent != null && object.ReferenceEquals(intent.enemy, enemy) &&
                intent.enemySlotIndex == enemySlotIndex)
            {
                return intent;
            }
        }
        return null;
    }

    static int Order(int? value)
    {
        return value.HasValue ? value.Value : -1;
    }

    sealed class TestContext
    {
        public CharacterData ally1;
        public CharacterData ally2;
        public CharacterData enemy1;
        public CharacterData enemy2;
        public BattleRuntimeState runtimeState;
    }
}
