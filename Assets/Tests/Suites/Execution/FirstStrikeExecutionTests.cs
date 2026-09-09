using System.Collections.Generic;

public static class FirstStrikeExecutionTests
{
    public static bool FreeActionPlayerFirstStrikeUsesFirstStrikeTier()
    {
        return VerifyFreeActionPriority(
            true,
            BattleExecutionPriorityTier.FirstStrike
        );
    }

    public static bool FreeActionPlayerNormalAttackUsesNormalTier()
    {
        return VerifyFreeActionPriority(
            false,
            BattleExecutionPriorityTier.Normal
        );
    }

    public static bool UnrespondedEnemyFirstStrikeUsesFirstStrikeTier()
    {
        return VerifyUnrespondedPriority(
            true,
            BattleExecutionPriorityTier.FirstStrike
        );
    }

    public static bool UnrespondedEnemyNormalAttackUsesNormalTier()
    {
        return VerifyUnrespondedPriority(
            false,
            BattleExecutionPriorityTier.Normal
        );
    }

    public static bool RespondedPlayerFirstStrikeEnemyNormalUsesFirstStrikeTier()
    {
        return VerifyRespondedPriority(
            CardType.Attack,
            true,
            CardType.Attack,
            false,
            BattleInteractionType.AttackVsAttack,
            BattleExecutionPriorityTier.FirstStrike
        );
    }

    public static bool RespondedPlayerNormalEnemyFirstStrikeUsesFirstStrikeTier()
    {
        return VerifyRespondedPriority(
            CardType.Attack,
            false,
            CardType.Attack,
            true,
            BattleInteractionType.AttackVsAttack,
            BattleExecutionPriorityTier.FirstStrike
        );
    }

    public static bool RespondedFirstStrikeDefenseEnemyNormalUsesFirstStrikeTier()
    {
        return VerifyRespondedPriority(
            CardType.Defense,
            true,
            CardType.Attack,
            false,
            BattleInteractionType.AttackVsDefense,
            BattleExecutionPriorityTier.FirstStrike
        );
    }

    public static bool RespondedNormalDefenseEnemyFirstStrikeUsesFirstStrikeTier()
    {
        return VerifyRespondedPriority(
            CardType.Defense,
            false,
            CardType.Attack,
            true,
            BattleInteractionType.AttackVsDefense,
            BattleExecutionPriorityTier.FirstStrike
        );
    }

    public static bool RespondedBothNormalUsesNormalTier()
    {
        return VerifyRespondedPriority(
            CardType.Attack,
            false,
            CardType.Attack,
            false,
            BattleInteractionType.AttackVsAttack,
            BattleExecutionPriorityTier.Normal
        );
    }

    public static bool RespondedBothFirstStrikeUsesFirstStrikeTier()
    {
        return VerifyRespondedPriority(
            CardType.Attack,
            true,
            CardType.Attack,
            true,
            BattleInteractionType.AttackVsAttack,
            BattleExecutionPriorityTier.FirstStrike
        );
    }

    public static bool FirstStrikeDoesNotChangeAttackVsDefenseInteraction()
    {
        return VerifyRespondedPriority(
            CardType.Attack,
            true,
            CardType.Defense,
            false,
            BattleInteractionType.AttackVsDefense,
            BattleExecutionPriorityTier.FirstStrike
        );
    }

    public static bool FirstStrikeSortsBeforeLaterNormalFreeAction()
    {
        ExecutionTestContext context = CreateContext(5, 5, 5);
        BattleActionSlot normalSlot = CreateFreeSlot(
            context.allyA,
            1,
            CreatePriorityCard(
                context.allyA,
                CardType.Attack,
                false,
                "mode89_normal"
            ),
            context.enemy
        );
        BattleActionSlot firstStrikeSlot = CreateFreeSlot(
            context.allyA,
            2,
            CreatePriorityCard(
                context.allyA,
                CardType.Attack,
                true,
                "mode89_first"
            ),
            context.enemy
        );

        BattleExecutionPlan plan =
            BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
                new List<BattleActionSlot> { normalSlot, firstStrikeSlot },
                new List<BattleEnemyIntent>()
            );
        return plan.executionItems.Count == 2 &&
            object.ReferenceEquals(
                plan.executionItems[0].actionSlot,
                firstStrikeSlot
            ) &&
            plan.executionItems[0].priorityTier ==
                BattleExecutionPriorityTier.FirstStrike &&
            object.ReferenceEquals(
                plan.executionItems[1].actionSlot,
                normalSlot
            ) &&
            plan.executionItems[1].priorityTier ==
                BattleExecutionPriorityTier.Normal;
    }

    public static bool EnemyFirstStrikeRespondedItemStaysPairedAndSortsFirst()
    {
        ExecutionTestContext context = CreateContext(1, 10, 1);
        BattleEnemyIntent intent = CreatePriorityIntent(
            context,
            CardType.Attack,
            true,
            "mode89_enemy_first"
        );
        BattleActionSlot responseSlot = new BattleActionSlot(
            context.allyA,
            1
        );
        responseSlot.AssignResponse(
            context.allyA,
            CreatePriorityCard(
                context.allyA,
                CardType.Defense,
                false,
                "mode89_response"
            ),
            intent,
            false
        );
        intent.MarkResponded();
        BattleActionSlot normalFreeSlot = CreateFreeSlot(
            context.allyB,
            1,
            CreatePriorityCard(
                context.allyB,
                CardType.Attack,
                false,
                "mode89_normal_free"
            ),
            context.enemy
        );

        BattleExecutionPlan plan =
            BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
                new List<BattleActionSlot> { normalFreeSlot, responseSlot },
                new List<BattleEnemyIntent> { intent }
            );
        return plan.executionItems.Count == 2 &&
            object.ReferenceEquals(
                plan.executionItems[0].actionSlot,
                responseSlot
            ) &&
            object.ReferenceEquals(
                plan.executionItems[0].enemyIntent,
                intent
            ) &&
            plan.executionItems[0].interactionType ==
                BattleInteractionType.AttackVsDefense &&
            plan.executionItems[0].priorityTier ==
                BattleExecutionPriorityTier.FirstStrike &&
            plan.executionItems[0].effectiveSpeed <
                plan.executionItems[1].effectiveSpeed &&
            object.ReferenceEquals(
                plan.executionItems[1].actionSlot,
                normalFreeSlot
            ) &&
            plan.executionItems[1].priorityTier ==
                BattleExecutionPriorityTier.Normal;
    }

    private static bool VerifyFreeActionPriority(
        bool firstStrike,
        BattleExecutionPriorityTier expectedTier
    )
    {
        ExecutionTestContext context = CreateContext(5, 5, 5);
        BattleActionSlot slot = CreateFreeSlot(
            context.allyA,
            1,
            CreatePriorityCard(
                context.allyA,
                CardType.Attack,
                firstStrike,
                "mode89_free"
            ),
            context.enemy
        );

        BattleExecutionItem item = GetOnlyItem(
            new List<BattleActionSlot> { slot },
            new List<BattleEnemyIntent>()
        );
        return item != null && item.priorityTier == expectedTier;
    }

    private static bool VerifyUnrespondedPriority(
        bool firstStrike,
        BattleExecutionPriorityTier expectedTier
    )
    {
        ExecutionTestContext context = CreateContext(5, 5, 5);
        BattleEnemyIntent intent = CreatePriorityIntent(
            context,
            CardType.Attack,
            firstStrike,
            "mode89_unresponded"
        );

        BattleExecutionItem item = GetOnlyItem(
            new List<BattleActionSlot>(),
            new List<BattleEnemyIntent> { intent }
        );
        return item != null && item.priorityTier == expectedTier;
    }

    private static bool VerifyRespondedPriority(
        string playerCardType,
        bool playerFirstStrike,
        string enemyCardType,
        bool enemyFirstStrike,
        BattleInteractionType expectedInteraction,
        BattleExecutionPriorityTier expectedTier
    )
    {
        ExecutionTestContext context = CreateContext(5, 5, 5);
        BattleEnemyIntent intent = CreatePriorityIntent(
            context,
            enemyCardType,
            enemyFirstStrike,
            "mode89_responded"
        );
        BattleActionSlot responseSlot = new BattleActionSlot(
            context.allyA,
            1
        );
        responseSlot.AssignResponse(
            context.allyA,
            CreatePriorityCard(
                context.allyA,
                playerCardType,
                playerFirstStrike,
                "mode89_response"
            ),
            intent,
            false
        );
        intent.MarkResponded();

        BattleExecutionItem item = GetOnlyItem(
            new List<BattleActionSlot> { responseSlot },
            new List<BattleEnemyIntent> { intent }
        );
        return item != null &&
            item.interactionType == expectedInteraction &&
            item.priorityTier == expectedTier &&
            object.ReferenceEquals(item.actionSlot, responseSlot) &&
            object.ReferenceEquals(item.enemyIntent, intent);
    }

    private static BattleActionSlot CreateFreeSlot(
        CharacterData actor,
        int slotIndex,
        BattleCardState card,
        CharacterData target
    )
    {
        BattleActionSlot slot = new BattleActionSlot(actor, slotIndex);
        slot.AssignFreeAction(actor, card, target);
        return slot;
    }

    private static BattleEnemyIntent CreatePriorityIntent(
        ExecutionTestContext context,
        string enemyCardType,
        bool enemyFirstStrike,
        string instanceID
    )
    {
        return TestIntentFactory.Create(
            instanceID,
            context.enemy,
            CreatePriorityCard(
                context.enemy,
                enemyCardType,
                enemyFirstStrike,
                instanceID + "_card"
            ),
            context.allyA,
            originalTargetSlotIndex: 1,
            intentOrder: 1,
            enemySlotIndex: 1
        );
    }

    private static BattleExecutionItem GetOnlyItem(
        List<BattleActionSlot> actionSlots,
        List<BattleEnemyIntent> intentQueue
    )
    {
        BattleExecutionPlan plan =
            BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
                actionSlots,
                intentQueue
            );
        return plan.executionItems != null && plan.executionItems.Count == 1
            ? plan.executionItems[0]
            : null;
    }

    private static BattleCardState CreatePriorityCard(
        CharacterData owner,
        string cardType,
        bool firstStrike,
        string instanceID
    )
    {
        return new BattleCardState(
            owner,
            new CardTestData
            {
                cardID = instanceID,
                cardName = instanceID,
                cardType = cardType,
                traits = firstStrike
                    ? new[] { BattleCardTrait.FirstStrike }
                    : null
            },
            instanceID
        );
    }

    private static ExecutionTestContext CreateContext(
        int allyASpeed,
        int allyBSpeed,
        int enemySpeed
    )
    {
        return new ExecutionTestContext
        {
            allyA = new CharacterData(
                "mode89_ally_a",
                30,
                allyASpeed,
                allyASpeed
            ),
            allyB = new CharacterData(
                "mode89_ally_b",
                30,
                allyBSpeed,
                allyBSpeed
            ),
            enemy = new CharacterData(
                "mode89_enemy",
                30,
                enemySpeed,
                enemySpeed
            )
        };
    }

    private sealed class ExecutionTestContext
    {
        public CharacterData allyA;
        public CharacterData allyB;
        public CharacterData enemy;
    }
}
