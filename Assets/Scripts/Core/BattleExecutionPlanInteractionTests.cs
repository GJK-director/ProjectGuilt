// 脚本中文说明：验证 ExecutionPlan 只记录计划阶段 Interaction，不执行 Resolver。
using System.Collections.Generic;
using UnityEngine;

public static class BattleExecutionPlanInteractionTests
{
    public static bool Run()
    {
        bool[] results = new bool[12];

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
            "Unresponded Enemy Defense / Dodge"
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
}
