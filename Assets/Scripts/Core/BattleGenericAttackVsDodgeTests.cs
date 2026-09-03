// 脚本中文说明：验证 AttackVsDodge 通用 Resolver 与连续闪避外层策略的兼容性。
using System.Collections.Generic;
using UnityEngine;

public static class BattleGenericAttackVsDodgeTests
{
    public static bool Run()
    {
        bool[] results =
        {
            VerifyDodgeSuccess(false),
            VerifyDodgeSuccess(true),
            VerifyDodgeFailed(false),
            VerifyDodgeFailed(true),
            VerifyDirectionSymmetry(),
            VerifyDodgeSuccessAttackLifecycle(),
            VerifyEnemyDodgeLifecycle(),
            VerifyInteractionDirectionAndNormalization(),
            VerifyInvalidPairRejected(),
            VerifyMeleeAndCloseRangeIdentity(),
            VerifyLongRangeIdentityAndResourceContract(),
            VerifyLongRangeRespondedDodgeSuccess(),
            VerifyLongRangeRespondedDodgeFailed(),
            VerifyOldAdapterParity(),
            VerifyContinuousDodgeBeginRegression(),
            VerifyContinuousDodgeSuccessPolicyProtection(),
            VerifyContinuousDodgeFailedPolicyProtection()
        };
        string[] names =
        {
            "Golden EnemyAttack + PlayerDodge Success",
            "Reverse PlayerAttack + EnemyDodge Success",
            "Golden EnemyAttack + PlayerDodge Failed",
            "Reverse PlayerAttack + EnemyDodge Failed",
            "两个方向固定输入数学对称",
            "DodgeSuccess时Attack仍Resolved并消费资源",
            "Enemy Dodge正式Resolved并进入CD",
            "两个方向均归一化到同一Generic Core",
            "Dodge + Dodge被安全拒绝",
            "Melee与CloseRange均为AttackVsDodge",
            "LongRange仍为AttackVsDodge并保留资源契约",
            "LongRange Responded Adapter DodgeSuccess保持Dodge Session",
            "LongRange Responded Adapter DodgeFailed保持Dodge Session",
            "旧Responded Adapter与Generic Core结果一致",
            "TryBeginContinuousDodgeClash仍汇入Generic Session",
            "Continuous Dodge成功仍由外层保留Slot",
            "Continuous Dodge失败仍由外层立即收尾"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log("模式93 测试" + (index + 1) + " " + names[index] + "：" + results[index]);
            allPassed &= results[index];
        }
        Debug.Log("模式93 Generic AttackVsDodge聚合结果：" + allPassed);
        return allPassed;
    }

    static bool VerifyDodgeSuccess(bool playerIsAttacker)
    {
        Fixture fixture = CreateFixture(
            "mode93_success_" + playerIsAttacker,
            playerIsAttacker,
            4,
            8
        );
        Resolution outcome = ResolveThroughRespondedAdapter(fixture);
        if (!IsSuccessful(outcome, "DodgeSuccess") ||
            outcome.result.damage != 0 ||
            fixture.dodgeActor.currentHP != fixture.dodgeActor.maxHP ||
            fixture.attackCard.currentCooldown != ExpectedCooldown(fixture.attackCard))
        {
            return false;
        }

        return playerIsAttacker
            ? outcome.result.playerCardUsed && outcome.result.enemyCardUsed &&
                !outcome.result.playerCardParticipated &&
                fixture.dodgeCard.currentCooldown == ExpectedCooldown(fixture.dodgeCard)
            : !outcome.result.playerCardUsed && outcome.result.enemyCardUsed &&
                outcome.result.playerCardParticipated &&
                outcome.result.playerCardUseDisposition ==
                    BattleCardUseDisposition.DeferForContinuousDodge &&
                fixture.dodgeCard.currentCooldown == 0;
    }

    static bool VerifyDodgeFailed(bool playerIsAttacker)
    {
        Fixture fixture = CreateFixture(
            "mode93_failed_" + playerIsAttacker,
            playerIsAttacker,
            8,
            5
        );
        Resolution outcome = ResolveThroughRespondedAdapter(fixture);
        return IsSuccessful(outcome, "DodgeFailed") &&
            outcome.result.damage > 0 &&
            fixture.dodgeActor.currentHP < fixture.dodgeActor.maxHP &&
            fixture.attackCard.currentCooldown == ExpectedCooldown(fixture.attackCard) &&
            fixture.dodgeCard.currentCooldown == ExpectedCooldown(fixture.dodgeCard) &&
            outcome.result.playerCardUsed && outcome.result.enemyCardUsed &&
            (playerIsAttacker ||
                outcome.result.playerCardUseDisposition ==
                    BattleCardUseDisposition.FinalizeImmediately);
    }

    static bool VerifyDirectionSymmetry()
    {
        Fixture golden = CreateFixture("mode93_symmetry_golden", false, 8, 5);
        Fixture reverse = CreateFixture("mode93_symmetry_reverse", true, 8, 5);
        Resolution goldenOutcome = ResolveThroughRespondedAdapter(golden);
        Resolution reverseOutcome = ResolveThroughRespondedAdapter(reverse);

        return IsSuccessful(goldenOutcome, "DodgeFailed") &&
            IsSuccessful(reverseOutcome, "DodgeFailed") &&
            goldenOutcome.session.SideBPoint == reverseOutcome.session.SideBPoint &&
            goldenOutcome.session.SideAPoint == reverseOutcome.session.SideAPoint &&
            goldenOutcome.result.damage == reverseOutcome.result.damage;
    }

    static bool VerifyDodgeSuccessAttackLifecycle()
    {
        Fixture fixture = CreateFixture("mode93_attack_lifecycle", true, 4, 8);
        const string resourceID = "Mode93AttackResource";
        fixture.attackActor.AddBuff(
            resourceID,
            "Mode93 Attack Resource",
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
        return IsSuccessful(outcome, "DodgeSuccess") &&
            outcome.result.damage == 0 && outcome.result.triggeredEventChain &&
            outcome.result.playerCardUsed &&
            fixture.attackCard.currentCooldown == ExpectedCooldown(fixture.attackCard) &&
            fixture.attackActor.GetBuffStack(resourceID) == 1;
    }

    static bool VerifyEnemyDodgeLifecycle()
    {
        Fixture fixture = CreateFixture("mode93_enemy_dodge_lifecycle", true, 4, 8);
        Resolution outcome = ResolveThroughRespondedAdapter(fixture);
        return IsSuccessful(outcome, "DodgeSuccess") &&
            outcome.result.enemyCardUsed &&
            fixture.dodgeCard.currentCooldown == ExpectedCooldown(fixture.dodgeCard);
    }

    static bool VerifyInteractionDirectionAndNormalization()
    {
        Fixture golden = CreateFixture("mode93_direction_golden", false, 4, 8);
        Fixture reverse = CreateFixture("mode93_direction_reverse", true, 4, 8);
        bool goldenNormalized = TryNormalize(golden, out BattleClashSession goldenSession);
        bool reverseNormalized = TryNormalize(reverse, out BattleClashSession reverseSession);

        return golden.context.effectiveInteractionType == BattleInteractionType.AttackVsDodge &&
            reverse.context.effectiveInteractionType == BattleInteractionType.AttackVsDodge &&
            goldenNormalized && reverseNormalized &&
            IsNormalizedSession(golden, goldenSession) &&
            IsNormalizedSession(reverse, reverseSession);
    }

    static bool VerifyInvalidPairRejected()
    {
        CharacterData sideA = CreateCharacter("mode93_invalid_a");
        CharacterData sideB = CreateCharacter("mode93_invalid_b");
        BattleCardState cardA = CreateCard(sideA, "mode93_invalid_a", CardType.Dodge, 5);
        BattleCardState cardB = CreateCard(sideB, "mode93_invalid_b", CardType.Dodge, 5);
        BattleExecutionInteractionContext context = new BattleExecutionInteractionContext(
            null,
            new BattleExecutionAction(sideA, cardA, null, null, sideB),
            new BattleExecutionAction(sideB, cardB, null, null, sideA)
        );
        int hpA = sideA.currentHP;
        int hpB = sideB.currentHP;
        bool normalized = BattleResolver.TryGetAttackAndDodgeActions(
            context,
            out _,
            out _
        );
        BattleResolveResult result = BattleResolver.ResolveAttackVsDodge(
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
            "mode93_melee",
            true,
            4,
            8,
            AttackDeliveryMode.Melee
        );
        Fixture closeRange = CreateFixture(
            "mode93_close_range",
            true,
            4,
            8,
            AttackDeliveryMode.CloseRangeShoot
        );
        Resolution meleeOutcome = ResolveDirectly(melee);
        Resolution closeRangeOutcome = ResolveDirectly(closeRange);
        return melee.context.effectiveInteractionType == BattleInteractionType.AttackVsDodge &&
            closeRange.context.effectiveInteractionType == BattleInteractionType.AttackVsDodge &&
            IsSuccessful(meleeOutcome, "DodgeSuccess") &&
            IsSuccessful(closeRangeOutcome, "DodgeSuccess");
    }

    static bool VerifyLongRangeIdentityAndResourceContract()
    {
        Fixture fixture = CreateFixture(
            "mode93_long_range",
            true,
            4,
            8,
            AttackDeliveryMode.LongRangeShoot
        );
        const string resourceID = "Mode93LongRangeBullet";
        fixture.attackActor.AddBuff(
            resourceID,
            "Mode93 LongRange Bullet",
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
        return fixture.context.effectiveInteractionType == BattleInteractionType.AttackVsDodge &&
            IsSuccessful(outcome, "DodgeSuccess") &&
            fixture.attackActor.GetBuffStack(resourceID) == 0;
    }

    static bool VerifyLongRangeRespondedDodgeSuccess()
    {
        Fixture fixture = CreateFixture(
            "mode93_long_range_adapter_success",
            true,
            4,
            8,
            AttackDeliveryMode.LongRangeShoot
        );
        const string resourceID = "Mode93LongRangeAdapterSuccessBullet";
        ConfigureSingleUseResource(fixture.attackActor, fixture.attackCard, resourceID);

        Resolution outcome = ResolveThroughRespondedAdapter(fixture);
        return outcome.session != null &&
            outcome.session.ClashType == BattleClashType.DodgeVsAttack &&
            outcome.session.FinalResult == BattleClashFinalResult.DodgeSuccess &&
            IsSuccessful(outcome, "DodgeSuccess") &&
            outcome.result.damage == 0 &&
            fixture.attackActor.GetBuffStack(resourceID) == 1;
    }

    static bool VerifyLongRangeRespondedDodgeFailed()
    {
        Fixture fixture = CreateFixture(
            "mode93_long_range_adapter_failed",
            true,
            8,
            5,
            AttackDeliveryMode.LongRangeShoot
        );
        const string resourceID = "Mode93LongRangeAdapterFailedBullet";
        ConfigureSingleUseResource(fixture.attackActor, fixture.attackCard, resourceID);

        Resolution outcome = ResolveThroughRespondedAdapter(fixture);
        return outcome.session != null &&
            outcome.session.ClashType == BattleClashType.DodgeVsAttack &&
            outcome.session.FinalResult == BattleClashFinalResult.DodgeFailed &&
            IsSuccessful(outcome, "DodgeFailed") &&
            outcome.result.damage > 0 &&
            fixture.attackActor.GetBuffStack(resourceID) == 1;
    }

    static bool VerifyOldAdapterParity()
    {
        Fixture adapterFixture = CreateFixture("mode93_adapter_parity", false, 8, 5);
        Fixture directFixture = CreateFixture("mode93_direct_parity", false, 8, 5);
        Resolution adapter = ResolveThroughRespondedAdapter(adapterFixture);
        Resolution direct = ResolveDirectly(directFixture);

        return IsSuccessful(adapter, "DodgeFailed") &&
            IsSuccessful(direct, "DodgeFailed") &&
            adapter.result.resultType == direct.result.resultType &&
            adapter.result.damage == direct.result.damage &&
            adapter.result.playerCardUseDisposition ==
                direct.result.playerCardUseDisposition &&
            adapterFixture.attackCard.currentCooldown == directFixture.attackCard.currentCooldown &&
            adapterFixture.dodgeCard.currentCooldown == directFixture.dodgeCard.currentCooldown;
    }

    static bool VerifyContinuousDodgeBeginRegression()
    {
        Fixture fixture = CreateFixture("mode93_continuous_begin", false, 4, 8);
        fixture.actionSlot.ActivateContinuousDodge(
            ContinuousDodgeSource.ExactEnemyIntent,
            8,
            fixture.enemy
        );
        BattleResolveResult beginFailure = BattleResolver.TryBeginContinuousDodgeClash(
            fixture.actionSlot,
            fixture.enemyIntent,
            out BattleClashSession session
        );
        return beginFailure == null && IsNormalizedSession(fixture, session) &&
            session.IsContinuousDodgeContinuation;
    }

    static bool VerifyContinuousDodgeSuccessPolicyProtection()
    {
        Fixture fixture = CreateFixture("mode93_continuous_success", false, 4, 8);
        fixture.actionSlot.ActivateContinuousDodge(
            ContinuousDodgeSource.ExactEnemyIntent,
            8,
            fixture.enemy
        );
        Resolution outcome = ResolveContinuous(fixture);
        if (!IsSuccessful(outcome, "DodgeSuccess") ||
            outcome.result.playerCardUseDisposition !=
                BattleCardUseDisposition.DeferForContinuousDodge)
        {
            return false;
        }

        BattleContinuousDodgeManager.RegisterSuccess(
            fixture.actionSlot,
            outcome.result,
            ContinuousDodgeSource.ContinuousDodge,
            fixture.enemyIntent
        );
        return fixture.actionSlot.isContinuousDodgeActive &&
            !fixture.actionSlot.isCardUseFinalized &&
            fixture.actionSlot.successfulDodgeCount == 2 &&
            fixture.dodgeCard.currentCooldown == 0 &&
            fixture.attackCard.currentCooldown == ExpectedCooldown(fixture.attackCard);
    }

    static bool VerifyContinuousDodgeFailedPolicyProtection()
    {
        Fixture fixture = CreateFixture("mode93_continuous_failed", false, 8, 5);
        fixture.actionSlot.ActivateContinuousDodge(
            ContinuousDodgeSource.ExactEnemyIntent,
            5,
            fixture.enemy
        );
        Resolution outcome = ResolveContinuous(fixture);
        if (!IsSuccessful(outcome, "DodgeFailed") || outcome.result.damage <= 0 ||
            outcome.result.playerCardUseDisposition !=
                BattleCardUseDisposition.FinalizeImmediately)
        {
            return false;
        }

        BattleContinuousDodgeManager.RecordImmediateFinalization(
            fixture.actionSlot,
            outcome.result
        );
        return !fixture.actionSlot.isContinuousDodgeActive &&
            fixture.actionSlot.isCardUseFinalized && fixture.actionSlot.isUsed &&
            fixture.dodgeCard.currentCooldown == ExpectedCooldown(fixture.dodgeCard) &&
            fixture.attackCard.currentCooldown == ExpectedCooldown(fixture.attackCard);
    }

    static bool TryNormalize(Fixture fixture, out BattleClashSession session)
    {
        session = null;
        if (!BattleResolver.TryGetAttackAndDodgeActions(
                fixture.context,
                out BattleExecutionAction attackAction,
                out BattleExecutionAction dodgeAction
            ))
        {
            return false;
        }

        return BattleResolver.TryBeginAttackVsDodge(
            attackAction,
            dodgeAction,
            out session
        ) == null;
    }

    static bool IsNormalizedSession(Fixture fixture, BattleClashSession session)
    {
        return session != null && session.ClashType == BattleClashType.DodgeVsAttack &&
            object.ReferenceEquals(session.SideA.cardState, fixture.dodgeCard) &&
            object.ReferenceEquals(session.SideB.cardState, fixture.attackCard);
    }

    static Resolution ResolveThroughRespondedAdapter(Fixture fixture)
    {
        BattleResolveResult beginFailure = BattleResolver.TryBeginRespondedClash(
            fixture.actionSlot,
            fixture.enemyIntent,
            out BattleClashSession session
        );
        return CompleteRespondedResolution(fixture, beginFailure, session);
    }

    static Resolution ResolveContinuous(Fixture fixture)
    {
        BattleResolveResult beginFailure = BattleResolver.TryBeginContinuousDodgeClash(
            fixture.actionSlot,
            fixture.enemyIntent,
            out BattleClashSession session
        );
        return CompleteRespondedResolution(fixture, beginFailure, session);
    }

    static Resolution CompleteRespondedResolution(
        Fixture fixture,
        BattleResolveResult beginFailure,
        BattleClashSession session
    )
    {
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
        if (!BattleResolver.TryGetAttackAndDodgeActions(
                fixture.context,
                out BattleExecutionAction attackAction,
                out BattleExecutionAction dodgeAction
            ))
        {
            return new Resolution(null, null);
        }
        BattleResolveResult beginFailure = BattleResolver.TryBeginAttackVsDodge(
            attackAction,
            dodgeAction,
            out BattleClashSession session
        );
        if (beginFailure != null || session == null ||
            !session.RollNextAttempt() || !session.IsFinalized)
        {
            return new Resolution(beginFailure, session);
        }
        return new Resolution(
            BattleResolver.FinalizeAttackVsDodge(
                attackAction,
                dodgeAction,
                session
            ),
            session
        );
    }

    static Fixture CreateFixture(
        string id,
        bool playerIsAttacker,
        int attackPoint,
        int dodgePoint,
        string deliveryMode = AttackDeliveryMode.Melee
    )
    {
        CharacterData player = CreateCharacter(id + "_player");
        CharacterData enemy = CreateCharacter(id + "_enemy");
        CharacterData attackActor = playerIsAttacker ? player : enemy;
        CharacterData dodgeActor = playerIsAttacker ? enemy : player;
        BattleCardState attackCard = CreateCard(
            attackActor,
            id + "_attack",
            CardType.Attack,
            attackPoint,
            deliveryMode
        );
        BattleCardState dodgeCard = CreateCard(
            dodgeActor,
            id + "_dodge",
            CardType.Dodge,
            dodgePoint
        );
        BattleCardState playerCard = playerIsAttacker ? attackCard : dodgeCard;
        BattleCardState enemyCard = playerIsAttacker ? dodgeCard : attackCard;
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
            dodgeActor = dodgeActor,
            attackCard = attackCard,
            dodgeCard = dodgeCard,
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
            effects = new List<CardEffectData>()
        };
        return BattleCardManager.CreateBattleCard(owner, data, id + "_instance");
    }

    static bool IsSuccessful(Resolution outcome, string resultType)
    {
        return outcome != null && outcome.result != null &&
            outcome.result.isSuccess && outcome.result.resultType == resultType;
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
        public CharacterData dodgeActor;
        public BattleCardState attackCard;
        public BattleCardState dodgeCard;
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
