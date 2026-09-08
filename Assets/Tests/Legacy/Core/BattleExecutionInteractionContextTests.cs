// 脚本中文说明：验证 Planned / Effective Execution Interaction Context 的纯数据语义。
using System.Collections.Generic;
using UnityEngine;

public static class BattleExecutionInteractionContextTests
{
    public static bool Run()
    {
        bool[] results = new bool[14];

        RespondedFixture attackVsAttack = CreateRespondedFixture(
            CardType.Attack,
            CardType.Attack
        );
        BattleExecutionInteractionContext attackContext =
            BattleExecutionInteractionContextFactory.BuildPlanned(
                attackVsAttack.item
            );
        results[0] =
            object.ReferenceEquals(attackContext.sideA.actor, attackVsAttack.player) &&
            object.ReferenceEquals(attackContext.sideA.cardState, attackVsAttack.playerCard) &&
            object.ReferenceEquals(attackContext.sideA.actionSlot, attackVsAttack.responseSlot) &&
            object.ReferenceEquals(attackContext.sideB.actor, attackVsAttack.enemy) &&
            object.ReferenceEquals(attackContext.sideB.cardState, attackVsAttack.enemyCard) &&
            object.ReferenceEquals(attackContext.sideB.enemyIntent, attackVsAttack.intent) &&
            attackContext.effectiveInteractionType == BattleInteractionType.AttackVsAttack;

        results[1] = VerifyRespondedInteraction(
            CardType.Defense,
            null,
            CardType.Attack,
            null,
            BattleInteractionType.AttackVsDefense
        );
        results[2] = VerifyRespondedInteraction(
            CardType.Attack,
            null,
            CardType.Defense,
            null,
            BattleInteractionType.AttackVsDefense
        );
        results[3] = VerifyRespondedInteraction(
            CardType.Dodge,
            null,
            CardType.Attack,
            null,
            BattleInteractionType.AttackVsDodge
        );

        UnrespondedFixture unresponded = CreateUnrespondedFixture(CardType.Attack);
        BattleExecutionInteractionContext unrespondedContext =
            BattleExecutionInteractionContextFactory.BuildPlanned(unresponded.item);
        results[4] =
            object.ReferenceEquals(unrespondedContext.sideA.actor, unresponded.enemy) &&
            object.ReferenceEquals(unrespondedContext.sideA.cardState, unresponded.enemyCard) &&
            unrespondedContext.sideB == null &&
            unrespondedContext.effectiveInteractionType ==
                BattleInteractionType.UnilateralAttack;

        BattleActionSlot defenseSlot = CreateRuntimeResponseSlot(
            unresponded.player,
            CardType.Defense,
            "mode90_runtime_defense"
        );
        BattleExecutionInteractionContext defenseContext =
            BattleExecutionInteractionContextFactory.BuildEffective(
                unresponded.item,
                defenseSlot
            );
        results[5] =
            unresponded.item.interactionType == BattleInteractionType.UnilateralAttack &&
            object.ReferenceEquals(defenseContext.sideA.actionSlot, defenseSlot) &&
            defenseContext.effectiveInteractionType ==
                BattleInteractionType.AttackVsDefense;

        BattleActionSlot dodgeSlot = CreateRuntimeResponseSlot(
            unresponded.player,
            CardType.Dodge,
            "mode90_runtime_dodge"
        );
        BattleExecutionInteractionContext dodgeContext =
            BattleExecutionInteractionContextFactory.BuildEffective(
                unresponded.item,
                dodgeSlot
            );
        results[6] =
            unresponded.item.interactionType == BattleInteractionType.UnilateralAttack &&
            object.ReferenceEquals(dodgeContext.sideA.actionSlot, dodgeSlot) &&
            dodgeContext.effectiveInteractionType ==
                BattleInteractionType.AttackVsDodge;

        results[7] = VerifyFreeAction(
            CardType.Attack,
            AttackDeliveryMode.Melee,
            BattleInteractionType.UnilateralAttack,
            true
        );
        results[8] = VerifyFreeAction(
            CardType.Attack,
            AttackDeliveryMode.LongRangeShoot,
            BattleInteractionType.UnilateralAttack,
            true
        );
        results[9] = VerifyFreeAction(
            CardType.Attack,
            AttackDeliveryMode.CloseRangeShoot,
            BattleInteractionType.UnilateralAttack,
            true
        );
        results[10] = VerifyFreeAction(
            CardType.Defense,
            null,
            BattleInteractionType.NoInteraction,
            true
        );
        results[11] = VerifyFreeAction(
            "Ability",
            null,
            BattleInteractionType.NoInteraction,
            true
        );
        results[12] = VerifyRespondedInteraction(
            CardType.Attack,
            AttackDeliveryMode.LongRangeShoot,
            CardType.Defense,
            null,
            BattleInteractionType.AttackVsDefense
        );
        results[13] = VerifyRespondedInteraction(
                CardType.Attack,
                null,
                CardType.Defense,
                null,
                BattleInteractionType.AttackVsDefense
            ) &&
            VerifyRespondedInteraction(
                CardType.Defense,
                null,
                CardType.Attack,
                null,
                BattleInteractionType.AttackVsDefense
            );

        string[] names =
        {
            "Responded Attack + Attack 双方Action来源",
            "Responded Player Defense + Enemy Attack",
            "Responded Player Attack + Enemy Defense",
            "Responded Player Dodge + Enemy Attack",
            "Planned Unresponded Enemy Attack",
            "Runtime Passive Defense Override",
            "Runtime Continuous Dodge Override",
            "FreeAction Melee Attack",
            "FreeAction LongRangeShoot",
            "FreeAction CloseRangeShoot",
            "FreeAction Defense保留Action",
            "Ability FreeAction保留Action",
            "LongRangeShoot + Defense",
            "AttackVsDefense来源方向交换"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式90 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }

        Debug.Log("模式90 Execution Interaction Context聚合结果：" + allPassed);
        return allPassed;
    }

    private static bool VerifyRespondedInteraction(
        string playerCardType,
        string playerDeliveryMode,
        string enemyCardType,
        string enemyDeliveryMode,
        BattleInteractionType expected
    )
    {
        RespondedFixture fixture = CreateRespondedFixture(
            playerCardType,
            enemyCardType,
            playerDeliveryMode,
            enemyDeliveryMode
        );
        BattleExecutionInteractionContext context =
            BattleExecutionInteractionContextFactory.BuildPlanned(fixture.item);

        return context != null &&
            context.sideA != null &&
            context.sideB != null &&
            context.effectiveInteractionType == expected &&
            context.effectiveInteractionType ==
                BattleInteractionClassifier.Classify(
                    context.sideA.cardState,
                    context.sideB.cardState
                );
    }

    private static bool VerifyFreeAction(
        string cardType,
        string deliveryMode,
        BattleInteractionType expected,
        bool requireAction
    )
    {
        CharacterData actor = new CharacterData("mode90_free_actor", 30, 5, 5);
        CharacterData target = new CharacterData("mode90_free_target", 30, 5, 5);
        BattleCardState card = CreateCard(
            actor,
            cardType,
            "mode90_free_card",
            deliveryMode
        );
        BattleActionSlot slot = new BattleActionSlot(actor, 1);
        slot.AssignFreeAction(actor, card, target);
        BattleExecutionItem item = GetOnlyItem(
            new List<BattleActionSlot> { slot },
            new List<BattleEnemyIntent>()
        );
        BattleExecutionInteractionContext context =
            BattleExecutionInteractionContextFactory.BuildPlanned(item);

        return context != null &&
            (!requireAction ||
             (context.sideA != null &&
              object.ReferenceEquals(context.sideA.actor, actor) &&
              object.ReferenceEquals(context.sideA.cardState, card))) &&
            context.sideB == null &&
            context.effectiveInteractionType == expected;
    }

    private static RespondedFixture CreateRespondedFixture(
        string playerCardType,
        string enemyCardType,
        string playerDeliveryMode = null,
        string enemyDeliveryMode = null
    )
    {
        CharacterData player = new CharacterData("mode90_player", 30, 5, 5);
        CharacterData enemy = new CharacterData("mode90_enemy", 30, 5, 5);
        BattleCardState playerCard = CreateCard(
            player,
            playerCardType,
            "mode90_player_card",
            playerDeliveryMode
        );
        BattleCardState enemyCard = CreateCard(
            enemy,
            enemyCardType,
            "mode90_enemy_card",
            enemyDeliveryMode
        );
        BattleEnemyIntent intent = new BattleEnemyIntent(
            "mode90_intent",
            enemy,
            enemyCard,
            player,
            1,
            1
        );
        BattleActionSlot responseSlot = new BattleActionSlot(player, 1);
        responseSlot.AssignResponse(player, playerCard, intent, false);
        intent.MarkResponded();

        return new RespondedFixture
        {
            player = player,
            enemy = enemy,
            playerCard = playerCard,
            enemyCard = enemyCard,
            intent = intent,
            responseSlot = responseSlot,
            item = GetOnlyItem(
                new List<BattleActionSlot> { responseSlot },
                new List<BattleEnemyIntent> { intent }
            )
        };
    }

    private static UnrespondedFixture CreateUnrespondedFixture(string enemyCardType)
    {
        CharacterData player = new CharacterData("mode90_target", 30, 5, 5);
        CharacterData enemy = new CharacterData("mode90_enemy", 30, 5, 5);
        BattleCardState enemyCard = CreateCard(
            enemy,
            enemyCardType,
            "mode90_unresponded_card",
            null
        );
        BattleEnemyIntent intent = new BattleEnemyIntent(
            "mode90_unresponded_intent",
            enemy,
            enemyCard,
            player,
            1,
            1
        );

        return new UnrespondedFixture
        {
            player = player,
            enemy = enemy,
            enemyCard = enemyCard,
            item = GetOnlyItem(
                new List<BattleActionSlot>(),
                new List<BattleEnemyIntent> { intent }
            )
        };
    }

    private static BattleActionSlot CreateRuntimeResponseSlot(
        CharacterData actor,
        string cardType,
        string instanceID
    )
    {
        BattleActionSlot slot = new BattleActionSlot(actor, 1);
        slot.AssignPassiveGuard(
            actor,
            CreateCard(actor, cardType, instanceID, null)
        );
        return slot;
    }

    private static BattleCardState CreateCard(
        CharacterData owner,
        string cardType,
        string instanceID,
        string deliveryMode
    )
    {
        return new BattleCardState(
            owner,
            new CardTestData
            {
                cardID = instanceID,
                cardName = instanceID,
                cardType = cardType,
                attackDeliveryMode = deliveryMode
            },
            instanceID
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

    private sealed class RespondedFixture
    {
        public CharacterData player;
        public CharacterData enemy;
        public BattleCardState playerCard;
        public BattleCardState enemyCard;
        public BattleEnemyIntent intent;
        public BattleActionSlot responseSlot;
        public BattleExecutionItem item;
    }

    private sealed class UnrespondedFixture
    {
        public CharacterData player;
        public CharacterData enemy;
        public BattleCardState enemyCard;
        public BattleExecutionItem item;
    }
}
