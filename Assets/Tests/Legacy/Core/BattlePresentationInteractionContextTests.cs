// 脚本中文说明：验证 Effective Interaction 到中立表现角色与 Phase Contract 的映射。
using UnityEngine;

public static class BattlePresentationInteractionContextTests
{
    public static bool Run()
    {
        bool[] results = new bool[17];

        Fixture attackVsAttack = CreateFixture(
            CardType.Attack,
            AttackDeliveryMode.Melee,
            CardType.Attack,
            AttackDeliveryMode.CloseRangeShoot
        );
        results[0] = TryCreate(attackVsAttack, false, out var attackContext) &&
            attackContext.InteractionType == BattleInteractionType.AttackVsAttack &&
            object.ReferenceEquals(attackContext.AttackActionA, attackVsAttack.sideA) &&
            object.ReferenceEquals(attackContext.AttackActionB, attackVsAttack.sideB) &&
            attackContext.AttackAction == null;

        Fixture playerAttackEnemyDefense = CreateFixture(
            CardType.Attack,
            AttackDeliveryMode.Melee,
            CardType.Defense,
            null
        );
        results[1] = VerifyAttackVsDefense(
            playerAttackEnemyDefense,
            playerAttackEnemyDefense.sideA,
            playerAttackEnemyDefense.sideB
        );

        Fixture enemyAttackPlayerDefense = CreateFixture(
            CardType.Defense,
            null,
            CardType.Attack,
            AttackDeliveryMode.Melee
        );
        results[2] = VerifyAttackVsDefense(
            enemyAttackPlayerDefense,
            enemyAttackPlayerDefense.sideB,
            enemyAttackPlayerDefense.sideA
        );

        Fixture playerAttackEnemyDodge = CreateFixture(
            CardType.Attack,
            AttackDeliveryMode.Melee,
            CardType.Dodge,
            null
        );
        results[3] = VerifyAttackVsDodge(
            playerAttackEnemyDodge,
            playerAttackEnemyDodge.sideA,
            playerAttackEnemyDodge.sideB,
            false
        );

        Fixture enemyAttackPlayerDodge = CreateFixture(
            CardType.Dodge,
            null,
            CardType.Attack,
            AttackDeliveryMode.Melee
        );
        results[4] = VerifyAttackVsDodge(
            enemyAttackPlayerDodge,
            enemyAttackPlayerDodge.sideB,
            enemyAttackPlayerDodge.sideA,
            false
        );

        Fixture playerUnilateral = CreateFixture(
            CardType.Attack,
            AttackDeliveryMode.Melee,
            null,
            null
        );
        results[5] = VerifyUnilateral(playerUnilateral, playerUnilateral.sideA);

        Fixture enemyUnilateral = CreateFixture(
            null,
            null,
            CardType.Attack,
            AttackDeliveryMode.Melee
        );
        results[6] = VerifyUnilateral(enemyUnilateral, enemyUnilateral.sideB);

        BattleExecutionInteractionContext passiveDefense =
            CreateRuntimeOverrideContext(CardType.Defense);
        results[7] = BattlePresentationInteractionContextFactory.TryCreate(
                passiveDefense,
                false,
                out var passiveContext
            ) &&
            passiveDefense.executionItem.interactionType ==
                BattleInteractionType.UnilateralAttack &&
            passiveContext.InteractionType ==
                BattleInteractionType.AttackVsDefense &&
            passiveContext.AttackAction != null &&
            passiveContext.DefenseAction != null;

        BattleExecutionInteractionContext continuousDodge =
            CreateRuntimeOverrideContext(CardType.Dodge);
        results[8] = BattlePresentationInteractionContextFactory.TryCreate(
                continuousDodge,
                true,
                out var continuousContext
            ) &&
            continuousDodge.executionItem.interactionType ==
                BattleInteractionType.UnilateralAttack &&
            continuousContext.InteractionType ==
                BattleInteractionType.AttackVsDodge &&
            continuousContext.DodgeAction != null;

        Fixture noInteraction = CreateFixture(
            CardType.Defense,
            null,
            null,
            null
        );
        results[9] = !TryCreate(noInteraction, false, out _);

        Fixture ability = CreateFixture("Ability", null, null, null);
        results[10] = !TryCreate(ability, false, out _);

        Fixture meleeDefense = CreateFixture(
            CardType.Attack,
            AttackDeliveryMode.Melee,
            CardType.Defense,
            null
        );
        Fixture longRangeDefense = CreateFixture(
            CardType.Attack,
            AttackDeliveryMode.LongRangeShoot,
            CardType.Defense,
            null
        );
        results[11] = TryCreate(meleeDefense, false, out var meleeContext) &&
            TryCreate(longRangeDefense, false, out var longRangeContext) &&
            meleeContext.InteractionType == longRangeContext.InteractionType &&
            meleeContext.AttackDeliveryMode == AttackDeliveryMode.Melee &&
            longRangeContext.AttackDeliveryMode ==
                AttackDeliveryMode.LongRangeShoot;

        Fixture closeRangeDodge = CreateFixture(
            CardType.Attack,
            AttackDeliveryMode.CloseRangeShoot,
            CardType.Dodge,
            null
        );
        results[12] = TryCreate(closeRangeDodge, false, out var closeContext) &&
            closeContext.InteractionType == BattleInteractionType.AttackVsDodge &&
            closeContext.AttackDeliveryMode == AttackDeliveryMode.CloseRangeShoot;

        results[13] = VerifyRoleNormalizationIndependentOfSourceDirection();

        Fixture invalid = CreateFixture(
            CardType.Defense,
            null,
            CardType.Dodge,
            null
        );
        invalid.executionContext.effectiveInteractionType =
            BattleInteractionType.AttackVsDefense;
        results[14] = !TryCreate(invalid, false, out _);

        results[15] = TryCreate(playerUnilateral, false, out var readyContext) &&
            VerifyReadyAndMovementAreIndependent(readyContext);

        results[16] = continuousContext != null &&
            continuousContext.InteractionType ==
                BattleInteractionType.AttackVsDodge &&
            continuousContext.ContinuationPolicy ==
                BattlePresentationContinuationPolicy.PreserveDodgePose &&
            VerifyContinuationPolicy(continuousContext);

        string[] names =
        {
            "AttackVsAttack保留两个Attack Action",
            "Player Attack + Enemy Defense角色归一化",
            "Enemy Attack + Player Defense角色归一化",
            "Player Attack + Enemy Dodge角色归一化",
            "Enemy Attack + Player Dodge角色归一化",
            "Player UnilateralAttack角色与目标",
            "Enemy UnilateralAttack角色与目标",
            "Runtime Passive Defense消费Effective Interaction",
            "Runtime Continuous Dodge消费Effective Interaction",
            "NoInteraction不创建Combat Presentation Context",
            "Ability FreeAction不创建Combat Presentation Context",
            "DeliveryMode不改变AttackVsDefense",
            "CloseRangeShoot不改变AttackVsDodge",
            "Role Normalization不依赖来源阵营",
            "无效Effective Context安全拒绝",
            "Ready Pose与RequiresApproach独立",
            "Continuous Dodge continuation metadata"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式95 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }

        Debug.Log("模式95 Neutral Presentation Context聚合结果：" + allPassed);
        return allPassed;
    }

    private static bool VerifyAttackVsDefense(
        Fixture fixture,
        BattleExecutionAction expectedAttack,
        BattleExecutionAction expectedDefense
    )
    {
        return TryCreate(fixture, false, out var context) &&
            context.InteractionType == BattleInteractionType.AttackVsDefense &&
            object.ReferenceEquals(context.AttackAction, expectedAttack) &&
            object.ReferenceEquals(context.DefenseAction, expectedDefense) &&
            context.DodgeAction == null;
    }

    private static bool VerifyAttackVsDodge(
        Fixture fixture,
        BattleExecutionAction expectedAttack,
        BattleExecutionAction expectedDodge,
        bool preserveDodgePose
    )
    {
        return TryCreate(fixture, preserveDodgePose, out var context) &&
            context.InteractionType == BattleInteractionType.AttackVsDodge &&
            object.ReferenceEquals(context.AttackAction, expectedAttack) &&
            object.ReferenceEquals(context.DodgeAction, expectedDodge) &&
            context.DefenseAction == null;
    }

    private static bool VerifyUnilateral(
        Fixture fixture,
        BattleExecutionAction expectedAttack
    )
    {
        return TryCreate(fixture, false, out var context) &&
            context.InteractionType == BattleInteractionType.UnilateralAttack &&
            object.ReferenceEquals(context.AttackAction, expectedAttack) &&
            object.ReferenceEquals(context.Target, expectedAttack.target) &&
            context.DefenseAction == null && context.DodgeAction == null;
    }

    private static bool VerifyRoleNormalizationIndependentOfSourceDirection()
    {
        Fixture first = CreateFixture(
            CardType.Attack,
            AttackDeliveryMode.Melee,
            CardType.Defense,
            null
        );
        Fixture reversed = CreateFixture(
            CardType.Defense,
            null,
            CardType.Attack,
            AttackDeliveryMode.Melee
        );

        return TryCreate(first, false, out var firstContext) &&
            TryCreate(reversed, false, out var reversedContext) &&
            firstContext.AttackAction.cardState.cardData.cardType == CardType.Attack &&
            reversedContext.AttackAction.cardState.cardData.cardType == CardType.Attack &&
            firstContext.DefenseAction.cardState.cardData.cardType == CardType.Defense &&
            reversedContext.DefenseAction.cardState.cardData.cardType == CardType.Defense;
    }

    private static bool VerifyReadyAndMovementAreIndependent(
        BattlePresentationInteractionContext context
    )
    {
        BattlePresentationPhaseContract phase =
            context.CreateActionBeginPhaseContract(false);
        return !phase.RequiresApproach && phase.RequiresReadyPose &&
            !phase.PreservePreviousPose;
    }

    private static bool VerifyContinuationPolicy(
        BattlePresentationInteractionContext context
    )
    {
        BattlePresentationPhaseContract phase =
            context.CreateActionBeginPhaseContract(false);
        return !phase.RequiresApproach && phase.RequiresReadyPose &&
            phase.PreservePreviousPose;
    }

    private static bool TryCreate(
        Fixture fixture,
        bool preserveDodgePose,
        out BattlePresentationInteractionContext context
    )
    {
        return BattlePresentationInteractionContextFactory.TryCreate(
            fixture != null ? fixture.executionContext : null,
            preserveDodgePose,
            out context
        );
    }

    private static Fixture CreateFixture(
        string sideACardType,
        string sideADeliveryMode,
        string sideBCardType,
        string sideBDeliveryMode
    )
    {
        CharacterData sideAActor = new CharacterData("mode95_side_a", 30, 5, 5);
        CharacterData sideBActor = new CharacterData("mode95_side_b", 30, 5, 5);
        BattleExecutionAction sideA = sideACardType != null
            ? CreateAction(
                sideAActor,
                sideBActor,
                sideACardType,
                sideADeliveryMode,
                "mode95_side_a_card"
            )
            : null;
        BattleExecutionAction sideB = sideBCardType != null
            ? CreateAction(
                sideBActor,
                sideAActor,
                sideBCardType,
                sideBDeliveryMode,
                "mode95_side_b_card"
            )
            : null;
        BattleExecutionItem item = new BattleExecutionItem(
            1,
            BattleExecutionItemType.FreeAction,
            null,
            null
        );

        return new Fixture
        {
            sideA = sideA,
            sideB = sideB,
            executionContext = new BattleExecutionInteractionContext(
                item,
                sideA,
                sideB
            )
        };
    }

    private static BattleExecutionInteractionContext CreateRuntimeOverrideContext(
        string responseCardType
    )
    {
        CharacterData target = new CharacterData("mode95_target", 30, 5, 5);
        CharacterData enemy = new CharacterData("mode95_enemy", 30, 5, 5);
        BattleCardState enemyAttack = CreateCard(
            enemy,
            CardType.Attack,
            AttackDeliveryMode.Melee,
            "mode95_enemy_attack"
        );
        BattleEnemyIntent intent = new BattleEnemyIntent(
            "mode95_runtime_intent",
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

        BattleActionSlot responseSlot = new BattleActionSlot(target, 1);
        responseSlot.AssignPassiveGuard(
            target,
            CreateCard(
                target,
                responseCardType,
                null,
                "mode95_runtime_response"
            )
        );
        return BattleExecutionInteractionContextFactory.BuildEffective(
            item,
            responseSlot
        );
    }

    private static BattleExecutionAction CreateAction(
        CharacterData actor,
        CharacterData target,
        string cardType,
        string deliveryMode,
        string instanceID
    )
    {
        return new BattleExecutionAction(
            actor,
            CreateCard(actor, cardType, deliveryMode, instanceID),
            null,
            null,
            target
        );
    }

    private static BattleCardState CreateCard(
        CharacterData owner,
        string cardType,
        string deliveryMode,
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
                attackDeliveryMode = deliveryMode
            },
            instanceID
        );
    }

    private sealed class Fixture
    {
        public BattleExecutionAction sideA;
        public BattleExecutionAction sideB;
        public BattleExecutionInteractionContext executionContext;
    }
}
