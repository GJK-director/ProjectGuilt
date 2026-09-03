// 脚本中文说明：验证 AttackVsDefense 通用 Resolver 在两个阵营方向使用同一套规则。
using System.Collections.Generic;
using UnityEngine;

public static class BattleGenericAttackVsDefenseTests
{
    public static bool Run()
    {
        bool[] results =
        {
            VerifyFullBlock(false),
            VerifyFullBlock(true),
            VerifyReducedDamage(false),
            VerifyReducedDamage(true),
            VerifyDirectionSymmetry(),
            VerifyFullBlockAttackLifecycle(),
            VerifyEnemyDefenseLifecycle(),
            VerifyInteractionDirectionAndNormalization(),
            VerifyInvalidPairRejected(),
            VerifyMeleeAndCloseRangeIdentity(),
            VerifyLongRangeIdentityAndResourceContract(),
            VerifyLongRangeRespondedFullBlock(),
            VerifyLongRangeRespondedReducedDamage(),
            VerifyOldAdapterParity()
        };
        string[] names =
        {
            "Golden EnemyAttack + PlayerDefense FullBlock",
            "Reverse PlayerAttack + EnemyDefense FullBlock",
            "Golden EnemyAttack + PlayerDefense ReducedDamage",
            "Reverse PlayerAttack + EnemyDefense ReducedDamage",
            "两个方向固定输入数学对称",
            "FullBlock时Attack仍Resolved并消费资源",
            "Enemy Defense正式Resolved并进入CD",
            "两个方向均归一化到同一Generic Core",
            "Defense + Defense被安全拒绝",
            "Melee与CloseRange均为AttackVsDefense",
            "LongRange仍为AttackVsDefense并保留资源契约",
            "LongRange Responded Adapter FullBlock保持Defense Session",
            "LongRange Responded Adapter ReducedDamage保持Defense Session",
            "旧Responded Adapter与Generic Core结果一致"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log("模式92 测试" + (index + 1) + " " + names[index] + "：" + results[index]);
            allPassed &= results[index];
        }
        Debug.Log("模式92 Generic AttackVsDefense聚合结果：" + allPassed);
        return allPassed;
    }

    static bool VerifyFullBlock(bool playerIsAttacker)
    {
        Fixture fixture = CreateFixture(
            "mode92_full_" + playerIsAttacker,
            playerIsAttacker,
            4,
            8
        );
        Resolution outcome = ResolveThroughRespondedAdapter(fixture);
        return IsSuccessful(outcome, "DefenseFullBlock") &&
            outcome.result.damage == 0 &&
            fixture.defenseActor.currentHP == fixture.defenseActor.maxHP &&
            BothCardsResolved(fixture);
    }

    static bool VerifyReducedDamage(bool playerIsAttacker)
    {
        Fixture fixture = CreateFixture(
            "mode92_reduced_" + playerIsAttacker,
            playerIsAttacker,
            8,
            5
        );
        Resolution outcome = ResolveThroughRespondedAdapter(fixture);
        return IsSuccessful(outcome, "DefenseReducedDamage") &&
            outcome.result.damage > 0 &&
            fixture.defenseActor.currentHP < fixture.defenseActor.maxHP &&
            BothCardsResolved(fixture);
    }

    static bool VerifyDirectionSymmetry()
    {
        Fixture golden = CreateFixture("mode92_symmetry_golden", false, 8, 5);
        Fixture reverse = CreateFixture("mode92_symmetry_reverse", true, 8, 5);
        Resolution goldenOutcome = ResolveThroughRespondedAdapter(golden);
        Resolution reverseOutcome = ResolveThroughRespondedAdapter(reverse);

        return IsSuccessful(goldenOutcome, "DefenseReducedDamage") &&
            IsSuccessful(reverseOutcome, "DefenseReducedDamage") &&
            goldenOutcome.session.SideBPoint == reverseOutcome.session.SideBPoint &&
            goldenOutcome.session.SideAPoint == reverseOutcome.session.SideAPoint &&
            goldenOutcome.session.RemainingAttackPoint ==
                reverseOutcome.session.RemainingAttackPoint &&
            goldenOutcome.result.damage == reverseOutcome.result.damage;
    }

    static bool VerifyFullBlockAttackLifecycle()
    {
        Fixture fixture = CreateFixture("mode92_attack_lifecycle", true, 4, 8);
        const string resourceID = "Mode92AttackResource";
        fixture.attackActor.AddBuff(
            resourceID,
            "Mode92 Attack Resource",
            BuffCategory.AbilityBuff,
            2,
            -1,
            BattleTiming.TurnEnd,
            BuffExpireRule.Permanent
        );
        fixture.attackCard.cardData.resourceRule = new CardResourceRuleData
        {
            resourceType = "BuffStack",
            resourceID = resourceID,
            requiredStackForNormalVersion = 1,
            consumeAmountOnSuccess = 1
        };

        Resolution outcome = ResolveThroughRespondedAdapter(fixture);
        return IsSuccessful(outcome, "DefenseFullBlock") &&
            outcome.result.playerCardUsed && outcome.result.enemyCardUsed &&
            fixture.attackCard.currentCooldown == ExpectedCooldown(fixture.attackCard) &&
            fixture.attackActor.GetBuffStack(resourceID) == 1;
    }

    static bool VerifyEnemyDefenseLifecycle()
    {
        Fixture fixture = CreateFixture("mode92_enemy_defense_lifecycle", true, 4, 8);
        Resolution outcome = ResolveThroughRespondedAdapter(fixture);
        return IsSuccessful(outcome, "DefenseFullBlock") &&
            outcome.result.enemyCardUsed &&
            fixture.defenseCard.currentCooldown == ExpectedCooldown(fixture.defenseCard);
    }

    static bool VerifyInteractionDirectionAndNormalization()
    {
        Fixture golden = CreateFixture("mode92_direction_golden", false, 4, 8);
        Fixture reverse = CreateFixture("mode92_direction_reverse", true, 4, 8);
        bool goldenNormalized = TryNormalize(golden, out BattleClashSession goldenSession);
        bool reverseNormalized = TryNormalize(reverse, out BattleClashSession reverseSession);

        return golden.context.effectiveInteractionType == BattleInteractionType.AttackVsDefense &&
            reverse.context.effectiveInteractionType == BattleInteractionType.AttackVsDefense &&
            goldenNormalized && reverseNormalized &&
            IsNormalizedSession(golden, goldenSession) &&
            IsNormalizedSession(reverse, reverseSession);
    }

    static bool VerifyInvalidPairRejected()
    {
        CharacterData sideA = CreateCharacter("mode92_invalid_a");
        CharacterData sideB = CreateCharacter("mode92_invalid_b");
        BattleCardState cardA = CreateCard(sideA, "mode92_invalid_a", CardType.Defense, 5);
        BattleCardState cardB = CreateCard(sideB, "mode92_invalid_b", CardType.Defense, 5);
        BattleExecutionInteractionContext context = new BattleExecutionInteractionContext(
            null,
            new BattleExecutionAction(sideA, cardA, null, null, sideB),
            new BattleExecutionAction(sideB, cardB, null, null, sideA)
        );
        int hpA = sideA.currentHP;
        int hpB = sideB.currentHP;
        bool normalized = BattleResolver.TryGetAttackAndDefenseActions(
            context,
            out _,
            out _
        );
        BattleResolveResult result = BattleResolver.ResolveAttackVsDefense(
            context.sideA,
            context.sideB
        );

        return !normalized && result != null && !result.isSuccess &&
            result.resultType == "Invalid" &&
            sideA.currentHP == hpA && sideB.currentHP == hpB &&
            cardA.currentCooldown == 0 && cardB.currentCooldown == 0;
    }

    static bool VerifyMeleeAndCloseRangeIdentity()
    {
        Fixture melee = CreateFixture(
            "mode92_melee",
            true,
            4,
            8,
            AttackDeliveryMode.Melee
        );
        Fixture closeRange = CreateFixture(
            "mode92_close_range",
            true,
            4,
            8,
            AttackDeliveryMode.CloseRangeShoot
        );
        Resolution meleeOutcome = ResolveDirectly(melee);
        Resolution closeRangeOutcome = ResolveDirectly(closeRange);
        return melee.context.effectiveInteractionType == BattleInteractionType.AttackVsDefense &&
            closeRange.context.effectiveInteractionType == BattleInteractionType.AttackVsDefense &&
            IsSuccessful(meleeOutcome, "DefenseFullBlock") &&
            IsSuccessful(closeRangeOutcome, "DefenseFullBlock");
    }

    static bool VerifyLongRangeIdentityAndResourceContract()
    {
        Fixture fixture = CreateFixture(
            "mode92_long_range",
            true,
            4,
            8,
            AttackDeliveryMode.LongRangeShoot
        );
        const string resourceID = "Mode92LongRangeBullet";
        fixture.attackActor.AddBuff(
            resourceID,
            "Mode92 LongRange Bullet",
            BuffCategory.AbilityBuff,
            1,
            -1,
            BattleTiming.TurnEnd,
            BuffExpireRule.Permanent
        );
        fixture.attackCard.cardData.resourceRule = new CardResourceRuleData
        {
            resourceType = "BuffStack",
            resourceID = resourceID,
            requiredStackForNormalVersion = 1,
            consumeAmountOnSuccess = 1
        };

        Resolution outcome = ResolveDirectly(fixture);
        return fixture.context.effectiveInteractionType == BattleInteractionType.AttackVsDefense &&
            IsSuccessful(outcome, "DefenseFullBlock") &&
            fixture.attackActor.GetBuffStack(resourceID) == 0;
    }

    static bool VerifyLongRangeRespondedFullBlock()
    {
        Fixture fixture = CreateFixture(
            "mode92_long_range_adapter_full",
            true,
            4,
            8,
            AttackDeliveryMode.LongRangeShoot
        );
        const string resourceID = "Mode92LongRangeAdapterFullBullet";
        ConfigureSingleUseResource(fixture.attackActor, fixture.attackCard, resourceID);

        Resolution outcome = ResolveThroughRespondedAdapter(fixture);
        return outcome.session != null &&
            outcome.session.ClashType == BattleClashType.DefenseVsAttack &&
            outcome.session.FinalResult == BattleClashFinalResult.DefenseFullBlock &&
            IsSuccessful(outcome, "DefenseFullBlock") &&
            outcome.result.damage == 0 &&
            fixture.attackActor.GetBuffStack(resourceID) == 1;
    }

    static bool VerifyLongRangeRespondedReducedDamage()
    {
        Fixture fixture = CreateFixture(
            "mode92_long_range_adapter_reduced",
            true,
            8,
            5,
            AttackDeliveryMode.LongRangeShoot
        );
        const string resourceID = "Mode92LongRangeAdapterReducedBullet";
        ConfigureSingleUseResource(fixture.attackActor, fixture.attackCard, resourceID);

        Resolution outcome = ResolveThroughRespondedAdapter(fixture);
        return outcome.session != null &&
            outcome.session.ClashType == BattleClashType.DefenseVsAttack &&
            outcome.session.FinalResult == BattleClashFinalResult.DefenseReducedDamage &&
            outcome.session.RemainingAttackPoint > 0 &&
            IsSuccessful(outcome, "DefenseReducedDamage") &&
            outcome.result.damage > 0 &&
            fixture.attackActor.GetBuffStack(resourceID) == 1;
    }

    static bool VerifyOldAdapterParity()
    {
        Fixture adapterFixture = CreateFixture("mode92_adapter_parity", false, 8, 5);
        Fixture directFixture = CreateFixture("mode92_direct_parity", false, 8, 5);
        Resolution adapter = ResolveThroughRespondedAdapter(adapterFixture);
        Resolution direct = ResolveDirectly(directFixture);

        return IsSuccessful(adapter, "DefenseReducedDamage") &&
            IsSuccessful(direct, "DefenseReducedDamage") &&
            adapter.result.resultType == direct.result.resultType &&
            adapter.result.damage == direct.result.damage &&
            adapterFixture.attackCard.currentCooldown == directFixture.attackCard.currentCooldown &&
            adapterFixture.defenseCard.currentCooldown == directFixture.defenseCard.currentCooldown;
    }

    static bool TryNormalize(Fixture fixture, out BattleClashSession session)
    {
        session = null;
        if (!BattleResolver.TryGetAttackAndDefenseActions(
                fixture.context,
                out BattleExecutionAction attackAction,
                out BattleExecutionAction defenseAction
            ))
        {
            return false;
        }

        return BattleResolver.TryBeginAttackVsDefense(
            attackAction,
            defenseAction,
            out session
        ) == null;
    }

    static bool IsNormalizedSession(Fixture fixture, BattleClashSession session)
    {
        return session != null && session.ClashType == BattleClashType.DefenseVsAttack &&
            object.ReferenceEquals(session.SideA.cardState, fixture.defenseCard) &&
            object.ReferenceEquals(session.SideB.cardState, fixture.attackCard);
    }

    static Resolution ResolveThroughRespondedAdapter(Fixture fixture)
    {
        BattleResolveResult beginFailure = BattleResolver.TryBeginRespondedClash(
            fixture.actionSlot,
            fixture.enemyIntent,
            out BattleClashSession session
        );
        if (beginFailure != null || session == null ||
            !session.RollNextAttempt() || !session.IsFinalized)
        {
            return new Resolution(beginFailure, session);
        }
        return new Resolution(
            BattleResolver.FinalizeRespondedClash(
                fixture.actionSlot,
                fixture.enemyIntent,
                session
            ),
            session
        );
    }

    static Resolution ResolveDirectly(Fixture fixture)
    {
        if (!BattleResolver.TryGetAttackAndDefenseActions(
                fixture.context,
                out BattleExecutionAction attackAction,
                out BattleExecutionAction defenseAction
            ))
        {
            return new Resolution(null, null);
        }
        BattleResolveResult beginFailure = BattleResolver.TryBeginAttackVsDefense(
            attackAction,
            defenseAction,
            out BattleClashSession session
        );
        if (beginFailure != null || session == null ||
            !session.RollNextAttempt() || !session.IsFinalized)
        {
            return new Resolution(beginFailure, session);
        }
        return new Resolution(
            BattleResolver.FinalizeAttackVsDefense(
                attackAction,
                defenseAction,
                session
            ),
            session
        );
    }

    static Fixture CreateFixture(
        string id,
        bool playerIsAttacker,
        int attackPoint,
        int defensePoint,
        string deliveryMode = AttackDeliveryMode.Melee
    )
    {
        CharacterData player = CreateCharacter(id + "_player");
        CharacterData enemy = CreateCharacter(id + "_enemy");
        CharacterData attackActor = playerIsAttacker ? player : enemy;
        CharacterData defenseActor = playerIsAttacker ? enemy : player;
        BattleCardState attackCard = CreateCard(
            attackActor,
            id + "_attack",
            CardType.Attack,
            attackPoint,
            deliveryMode
        );
        BattleCardState defenseCard = CreateCard(
            defenseActor,
            id + "_defense",
            CardType.Defense,
            defensePoint
        );
        BattleCardState playerCard = playerIsAttacker ? attackCard : defenseCard;
        BattleCardState enemyCard = playerIsAttacker ? defenseCard : attackCard;
        BattleEnemyIntent intent = new BattleEnemyIntent(
            id + "_intent",
            enemy,
            enemyCard,
            player,
            1,
            1
        );
        BattleActionSlot slot = new BattleActionSlot(player, 1);
        slot.AssignResponse(player, playerCard, intent, false);
        intent.MarkResponded();
        BattleExecutionAction playerAction = new BattleExecutionAction(
            player,
            playerCard,
            slot,
            intent,
            enemy
        );
        BattleExecutionAction enemyAction = new BattleExecutionAction(
            enemy,
            enemyCard,
            null,
            intent,
            player
        );

        return new Fixture
        {
            player = player,
            enemy = enemy,
            attackActor = attackActor,
            defenseActor = defenseActor,
            attackCard = attackCard,
            defenseCard = defenseCard,
            actionSlot = slot,
            enemyIntent = intent,
            context = new BattleExecutionInteractionContext(
                null,
                playerAction,
                enemyAction
            )
        };
    }

    static CharacterData CreateCharacter(string id)
    {
        return new CharacterData(id, 30, 5, 5);
    }

    static BattleCardState CreateCard(
        CharacterData owner,
        string id,
        string cardType,
        int point,
        string deliveryMode = AttackDeliveryMode.Melee
    )
    {
        CardTestData data = new CardTestData
        {
            cardID = id + "_data",
            cardName = id,
            cardType = cardType,
            attackDeliveryMode = cardType == CardType.Attack
                ? deliveryMode
                : string.Empty,
            isClashable = cardType == CardType.Attack,
            minPoint = point,
            maxPoint = point,
            cooldown = 2,
            damageFormula = cardType == CardType.Attack ? "PointAsDamage" : string.Empty,
            defenseFormula = cardType == CardType.Defense ? "PointAsDefense" : string.Empty,
            effects = new List<CardEffectData>()
        };
        return BattleCardManager.CreateBattleCard(owner, data, id + "_instance");
    }

    static bool IsSuccessful(Resolution outcome, string resultType)
    {
        return outcome != null && outcome.result != null &&
            outcome.result.isSuccess && outcome.result.resultType == resultType;
    }

    static bool BothCardsResolved(Fixture fixture)
    {
        return fixture.attackCard.currentCooldown == ExpectedCooldown(fixture.attackCard) &&
            fixture.defenseCard.currentCooldown == ExpectedCooldown(fixture.defenseCard);
    }

    static int ExpectedCooldown(BattleCardState card)
    {
        int baseCooldown = BattleCardManager.GetBaseCooldown(card.cardData);
        return baseCooldown > 0 ? baseCooldown + 1 : 0;
    }

    static void ConfigureSingleUseResource(
        CharacterData actor,
        BattleCardState card,
        string resourceID
    )
    {
        actor.AddBuff(
            resourceID,
            resourceID,
            BuffCategory.AbilityBuff,
            2,
            -1,
            BattleTiming.TurnEnd,
            BuffExpireRule.Permanent
        );
        card.cardData.resourceRule = new CardResourceRuleData
        {
            resourceType = "BuffStack",
            resourceID = resourceID,
            requiredStackForNormalVersion = 1,
            consumeAmountOnSuccess = 1
        };
    }

    sealed class Fixture
    {
        public CharacterData player;
        public CharacterData enemy;
        public CharacterData attackActor;
        public CharacterData defenseActor;
        public BattleCardState attackCard;
        public BattleCardState defenseCard;
        public BattleActionSlot actionSlot;
        public BattleEnemyIntent enemyIntent;
        public BattleExecutionInteractionContext context;
    }

    sealed class Resolution
    {
        public BattleResolveResult result;
        public BattleClashSession session;

        public Resolution(BattleResolveResult result, BattleClashSession session)
        {
            this.result = result;
            this.session = session;
        }
    }
}
