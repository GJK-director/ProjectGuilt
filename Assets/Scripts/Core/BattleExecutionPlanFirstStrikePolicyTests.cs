// 脚本中文说明：验证完整 ExecutionItem 的 FirstStrike Priority Policy，不执行 Combat。
using System.Collections.Generic;
using UnityEngine;

public static class BattleExecutionPlanFirstStrikePolicyTests
{
    public static bool Run()
    {
        bool[] results = new bool[13];

        results[0] = VerifyFreeActionPriority(true, BattleExecutionPriorityTier.FirstStrike);
        results[1] = VerifyFreeActionPriority(false, BattleExecutionPriorityTier.Normal);
        results[2] = VerifyUnrespondedPriority(true, BattleExecutionPriorityTier.FirstStrike);
        results[3] = VerifyUnrespondedPriority(false, BattleExecutionPriorityTier.Normal);
        results[4] = VerifyRespondedPriority(
            CardType.Attack,
            true,
            CardType.Attack,
            false,
            BattleInteractionType.AttackVsAttack,
            BattleExecutionPriorityTier.FirstStrike
        );
        results[5] = VerifyRespondedPriority(
            CardType.Attack,
            false,
            CardType.Attack,
            true,
            BattleInteractionType.AttackVsAttack,
            BattleExecutionPriorityTier.FirstStrike
        );
        results[6] = VerifyRespondedPriority(
            CardType.Defense,
            true,
            CardType.Attack,
            false,
            BattleInteractionType.AttackVsDefense,
            BattleExecutionPriorityTier.FirstStrike
        );
        results[7] = VerifyRespondedPriority(
            CardType.Defense,
            false,
            CardType.Attack,
            true,
            BattleInteractionType.AttackVsDefense,
            BattleExecutionPriorityTier.FirstStrike
        );
        results[8] = VerifyRespondedPriority(
            CardType.Attack,
            false,
            CardType.Attack,
            false,
            BattleInteractionType.AttackVsAttack,
            BattleExecutionPriorityTier.Normal
        );
        results[9] = VerifyRespondedPriority(
            CardType.Attack,
            true,
            CardType.Attack,
            true,
            BattleInteractionType.AttackVsAttack,
            BattleExecutionPriorityTier.FirstStrike
        );
        results[10] = VerifyRespondedPriority(
            CardType.Attack,
            true,
            CardType.Defense,
            false,
            BattleInteractionType.AttackVsDefense,
            BattleExecutionPriorityTier.FirstStrike
        );
        results[11] = VerifyFirstStrikeSortsBeforeLaterNormalFreeAction();
        results[12] = VerifyEnemyFirstStrikeRespondedItemStaysPairedAndSortsFirst();

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
            "Enemy FirstStrike Responded Item 不拆 Pairing 且优先"
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

    private static bool VerifyFreeActionPriority(
        bool firstStrike,
        BattleExecutionPriorityTier expectedTier
    )
    {
        TestContext context = CreateContext(5, 5, 5);
        BattleActionSlot slot = CreateFreeSlot(
            context.allyA,
            1,
            CreateCard(context.allyA, CardType.Attack, firstStrike, "mode89_free"),
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
        TestContext context = CreateContext(5, 5, 5);
        BattleEnemyIntent intent = CreateIntent(
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
        TestContext context = CreateContext(5, 5, 5);
        BattleEnemyIntent intent = CreateIntent(
            context,
            enemyCardType,
            enemyFirstStrike,
            "mode89_responded"
        );
        BattleActionSlot responseSlot = new BattleActionSlot(context.allyA, 1);
        responseSlot.AssignResponse(
            context.allyA,
            CreateCard(
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

    private static bool VerifyFirstStrikeSortsBeforeLaterNormalFreeAction()
    {
        TestContext context = CreateContext(5, 5, 5);
        BattleActionSlot normalSlot = CreateFreeSlot(
            context.allyA,
            1,
            CreateCard(context.allyA, CardType.Attack, false, "mode89_normal"),
            context.enemy
        );
        BattleActionSlot firstStrikeSlot = CreateFreeSlot(
            context.allyA,
            2,
            CreateCard(context.allyA, CardType.Attack, true, "mode89_first"),
            context.enemy
        );

        BattleExecutionPlan plan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            new List<BattleActionSlot> { normalSlot, firstStrikeSlot },
            new List<BattleEnemyIntent>()
        );
        return plan.executionItems.Count == 2 &&
            object.ReferenceEquals(plan.executionItems[0].actionSlot, firstStrikeSlot) &&
            plan.executionItems[0].priorityTier == BattleExecutionPriorityTier.FirstStrike &&
            object.ReferenceEquals(plan.executionItems[1].actionSlot, normalSlot) &&
            plan.executionItems[1].priorityTier == BattleExecutionPriorityTier.Normal;
    }

    private static bool VerifyEnemyFirstStrikeRespondedItemStaysPairedAndSortsFirst()
    {
        TestContext context = CreateContext(1, 10, 1);
        BattleEnemyIntent intent = CreateIntent(
            context,
            CardType.Attack,
            true,
            "mode89_enemy_first"
        );
        BattleActionSlot responseSlot = new BattleActionSlot(context.allyA, 1);
        responseSlot.AssignResponse(
            context.allyA,
            CreateCard(context.allyA, CardType.Defense, false, "mode89_response"),
            intent,
            false
        );
        intent.MarkResponded();
        BattleActionSlot normalFreeSlot = CreateFreeSlot(
            context.allyB,
            1,
            CreateCard(context.allyB, CardType.Attack, false, "mode89_normal_free"),
            context.enemy
        );

        BattleExecutionPlan plan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            new List<BattleActionSlot> { normalFreeSlot, responseSlot },
            new List<BattleEnemyIntent> { intent }
        );
        return plan.executionItems.Count == 2 &&
            object.ReferenceEquals(plan.executionItems[0].actionSlot, responseSlot) &&
            object.ReferenceEquals(plan.executionItems[0].enemyIntent, intent) &&
            plan.executionItems[0].interactionType == BattleInteractionType.AttackVsDefense &&
            plan.executionItems[0].priorityTier == BattleExecutionPriorityTier.FirstStrike &&
            plan.executionItems[0].effectiveSpeed < plan.executionItems[1].effectiveSpeed &&
            object.ReferenceEquals(plan.executionItems[1].actionSlot, normalFreeSlot) &&
            plan.executionItems[1].priorityTier == BattleExecutionPriorityTier.Normal;
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

    private static BattleEnemyIntent CreateIntent(
        TestContext context,
        string enemyCardType,
        bool enemyFirstStrike,
        string instanceID
    )
    {
        return new BattleEnemyIntent(
            instanceID,
            context.enemy,
            CreateCard(context.enemy, enemyCardType, enemyFirstStrike, instanceID + "_card"),
            context.allyA,
            1,
            1
        );
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

    private static TestContext CreateContext(
        int allyASpeed,
        int allyBSpeed,
        int enemySpeed
    )
    {
        return new TestContext
        {
            allyA = new CharacterData("mode89_ally_a", 30, allyASpeed, allyASpeed),
            allyB = new CharacterData("mode89_ally_b", 30, allyBSpeed, allyBSpeed),
            enemy = new CharacterData("mode89_enemy", 30, enemySpeed, enemySpeed)
        };
    }

    private sealed class TestContext
    {
        public CharacterData allyA;
        public CharacterData allyB;
        public CharacterData enemy;
    }
}
