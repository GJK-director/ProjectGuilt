// 脚本中文说明：验证 Player/Enemy 单方面攻击共用同一套 UnilateralAttack Combat Core。
using System.Collections.Generic;
using UnityEngine;

public static class BattleGenericUnilateralAttackTests
{
    public static bool Run()
    {
        bool[] results =
        {
            VerifyGoldenPlayerMeleeFreeAttack(),
            VerifyGoldenEnemyUnrespondedAttack(),
            VerifyDirectionSymmetry(),
            VerifyPlayerAttackLifecycle(),
            VerifyEnemyAttackLifecycle(),
            VerifyContextIdentityAndNormalization(),
            VerifyInvalidNonAttackRejected(),
            VerifyNullTargetRejected(),
            VerifyMeleeAndCloseRangeIdentity(),
            VerifyLongRangeWithResource(),
            VerifyLongRangeNoResourceContract(),
            VerifyPlayerOldAdapterParity(),
            VerifyEnemyOldAdapterParity(),
            VerifyAbilityRegression(),
            VerifyNoInteractionRegression()
        };
        string[] names =
        {
            "Golden Player Melee FreeAttack",
            "Golden Enemy Unresponded Attack",
            "两个方向固定输入数学对称",
            "Player单边Attack完整生命周期",
            "Enemy单边Attack完整生命周期",
            "两个方向均归一化到UnilateralAttack Core",
            "Defense + null被Generic Core安全拒绝",
            "Attack目标为空时安全拒绝",
            "Melee与CloseRange均为UnilateralAttack",
            "LongRange资源充足时正常结算",
            "LongRange无资源不提交Damage/Resolved/CD",
            "旧Player FreeAction Adapter与Generic Core一致",
            "旧Enemy Unresponded Adapter与Generic Core一致",
            "Ability FreeAction保持独立Resolver",
            "Defense/Dodge FreeAction保持NoInteraction"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log("模式94 测试" + (index + 1) + " " + names[index] + "：" + results[index]);
            allPassed &= results[index];
        }
        Debug.Log("模式94 Generic UnilateralAttack聚合结果：" + allPassed);
        return allPassed;
    }

    static bool VerifyGoldenPlayerMeleeFreeAttack()
    {
        Fixture fixture = CreatePlayerFixture("mode94_player_golden", 7);
        BattleResolveResult result = BattleResolver.ResolveFreeAction(fixture.actionSlot);
        return IsSuccessful(result, "FreeAttack") && result.damage == 7 &&
            fixture.target.currentHP == fixture.target.maxHP - 7 &&
            fixture.attackCard.currentCooldown == ExpectedCooldown(fixture.attackCard) &&
            result.playerCardUsed && !result.enemyCardUsed;
    }

    static bool VerifyGoldenEnemyUnrespondedAttack()
    {
        Fixture fixture = CreateEnemyFixture("mode94_enemy_golden", 7);
        BattleResolveResult result = BattleResolver.ResolveUnrespondedEnemyIntent(
            fixture.enemyIntent
        );
        return IsSuccessful(result, "UnrespondedEnemyAttack") && result.damage == 7 &&
            fixture.target.currentHP == fixture.target.maxHP - 7 &&
            fixture.attackCard.currentCooldown == ExpectedCooldown(fixture.attackCard) &&
            !result.playerCardUsed && result.enemyCardUsed;
    }

    static bool VerifyDirectionSymmetry()
    {
        Fixture player = CreatePlayerFixture("mode94_symmetry_player", 8);
        Fixture enemy = CreateEnemyFixture("mode94_symmetry_enemy", 8);
        BattleResolveResult playerResult = BattleResolver.ResolveUnilateralAttack(
            player.attackAction
        );
        BattleResolveResult enemyResult = BattleResolver.ResolveUnilateralAttack(
            enemy.attackAction
        );

        return IsSuccessful(playerResult, "FreeAttack") &&
            IsSuccessful(enemyResult, "UnrespondedEnemyAttack") &&
            playerResult.playerPoint == enemyResult.enemyPoint &&
            playerResult.damage == enemyResult.damage &&
            object.ReferenceEquals(playerResult.damagedCharacter, player.target) &&
            object.ReferenceEquals(enemyResult.damagedCharacter, enemy.target);
    }

    static bool VerifyPlayerAttackLifecycle()
    {
        Fixture fixture = CreatePlayerFixture("mode94_player_lifecycle", 6);
        ConfigureLifecycleProbe(fixture, "Mode94Player");
        BattleResolveResult result = BattleResolver.ResolveUnilateralAttack(
            fixture.attackAction
        );
        return VerifyLifecycleOutcome(fixture, result, "Mode94Player");
    }

    static bool VerifyEnemyAttackLifecycle()
    {
        Fixture fixture = CreateEnemyFixture("mode94_enemy_lifecycle", 6);
        ConfigureLifecycleProbe(fixture, "Mode94Enemy");
        BattleResolveResult result = BattleResolver.ResolveUnilateralAttack(
            fixture.attackAction
        );
        return VerifyLifecycleOutcome(fixture, result, "Mode94Enemy");
    }

    static bool VerifyContextIdentityAndNormalization()
    {
        Fixture player = CreatePlayerFixture("mode94_context_player", 6);
        Fixture enemy = CreateEnemyFixture("mode94_context_enemy", 6);
        BattleExecutionInteractionContext playerContext =
            BattleExecutionInteractionContextFactory.BuildPlanned(player.executionItem);
        BattleExecutionInteractionContext enemyContext =
            BattleExecutionInteractionContextFactory.BuildPlanned(enemy.executionItem);

        bool playerNormalized = BattleResolver.TryGetUnilateralAttackAction(
            playerContext,
            out BattleExecutionAction playerAction
        );
        bool enemyNormalized = BattleResolver.TryGetUnilateralAttackAction(
            enemyContext,
            out BattleExecutionAction enemyAction
        );
        return playerContext.effectiveInteractionType ==
                BattleInteractionType.UnilateralAttack &&
            enemyContext.effectiveInteractionType ==
                BattleInteractionType.UnilateralAttack &&
            playerNormalized && enemyNormalized &&
            object.ReferenceEquals(playerAction.cardState, player.attackCard) &&
            object.ReferenceEquals(enemyAction.cardState, enemy.attackCard);
    }

    static bool VerifyInvalidNonAttackRejected()
    {
        CharacterData actor = CreateCharacter("mode94_invalid_actor");
        CharacterData target = CreateCharacter("mode94_invalid_target");
        BattleCardState defense = CreateCard(
            actor,
            "mode94_invalid_defense",
            CardType.Defense,
            6
        );
        BattleExecutionAction action = new BattleExecutionAction(
            actor,
            defense,
            null,
            null,
            target
        );
        int hpBefore = target.currentHP;
        BattleResolveResult result = BattleResolver.ResolveUnilateralAttack(action);
        return result != null && !result.isSuccess && result.resultType == "Invalid" &&
            target.currentHP == hpBefore && defense.currentCooldown == 0;
    }

    static bool VerifyNullTargetRejected()
    {
        CharacterData actor = CreateCharacter("mode94_null_target_actor");
        BattleCardState attack = CreateCard(
            actor,
            "mode94_null_target_attack",
            CardType.Attack,
            6
        );
        BattleExecutionAction action = new BattleExecutionAction(
            actor,
            attack,
            null,
            null,
            null
        );
        BattleResolveResult result = BattleResolver.ResolveUnilateralAttack(action);
        return result != null && !result.isSuccess && result.resultType == "Invalid" &&
            attack.currentCooldown == 0;
    }

    static bool VerifyMeleeAndCloseRangeIdentity()
    {
        Fixture melee = CreatePlayerFixture(
            "mode94_melee",
            6,
            AttackDeliveryMode.Melee
        );
        Fixture closeRange = CreatePlayerFixture(
            "mode94_close_range",
            6,
            AttackDeliveryMode.CloseRangeShoot
        );
        BattleResolveResult meleeResult = BattleResolver.ResolveUnilateralAttack(
            melee.attackAction
        );
        BattleResolveResult closeRangeResult = BattleResolver.ResolveUnilateralAttack(
            closeRange.attackAction
        );
        return IsSuccessful(meleeResult, "FreeAttack") &&
            IsSuccessful(closeRangeResult, "FreeAttack") &&
            meleeResult.playerPoint == closeRangeResult.playerPoint &&
            meleeResult.damage == closeRangeResult.damage;
    }

    static bool VerifyLongRangeWithResource()
    {
        Fixture fixture = CreatePlayerFixture(
            "mode94_long_range_resource",
            6,
            AttackDeliveryMode.LongRangeShoot
        );
        ConfigureResourceRule(fixture, "Mode94LongRangeBullet", 1);
        BattleResolveResult result = BattleResolver.ResolveUnilateralAttack(
            fixture.attackAction
        );
        return IsSuccessful(result, "FreeAttack") && result.damage == 6 &&
            fixture.attackActor.GetBuffStack("Mode94LongRangeBullet") == 0 &&
            fixture.attackCard.currentCooldown == ExpectedCooldown(fixture.attackCard);
    }

    static bool VerifyLongRangeNoResourceContract()
    {
        Fixture fixture = CreatePlayerFixture(
            "mode94_long_range_empty",
            6,
            AttackDeliveryMode.LongRangeShoot
        );
        ConfigureResourceRule(fixture, "Mode94EmptyBullet", 0);
        int hpBefore = fixture.target.currentHP;
        BattleResolveResult result = BattleResolver.ResolveUnilateralAttack(
            fixture.attackAction
        );
        return result != null && !result.isSuccess &&
            result.resultType == "ActionUnavailable" &&
            fixture.target.currentHP == hpBefore &&
            fixture.attackCard.currentCooldown == 0 &&
            fixture.attackActor.GetBuffStack("Mode94EmptyBullet") == 0;
    }

    static bool VerifyPlayerOldAdapterParity()
    {
        Fixture adapter = CreatePlayerFixture("mode94_player_adapter", 7);
        Fixture direct = CreatePlayerFixture("mode94_player_direct", 7);
        BattleResolveResult adapterResult = BattleResolver.ResolveFreeAction(
            adapter.actionSlot
        );
        BattleResolveResult directResult = BattleResolver.ResolveUnilateralAttack(
            direct.attackAction
        );
        return ResultsMatch(adapterResult, directResult) &&
            adapter.attackCard.currentCooldown == direct.attackCard.currentCooldown;
    }

    static bool VerifyEnemyOldAdapterParity()
    {
        Fixture adapter = CreateEnemyFixture("mode94_enemy_adapter", 7);
        Fixture direct = CreateEnemyFixture("mode94_enemy_direct", 7);
        BattleResolveResult adapterResult = BattleResolver.ResolveUnrespondedEnemyIntent(
            adapter.enemyIntent
        );
        BattleResolveResult directResult = BattleResolver.ResolveUnilateralAttack(
            direct.attackAction
        );
        return ResultsMatch(adapterResult, directResult) &&
            adapter.attackCard.currentCooldown == direct.attackCard.currentCooldown;
    }

    static bool VerifyAbilityRegression()
    {
        CharacterData actor = CreateCharacter("mode94_ability_actor");
        BattleCardState ability = CreateCard(
            actor,
            "mode94_ability",
            "Ability",
            0
        );
        ability.cardData.effects.Add(CreateBuffEffect(
            BattleTiming.OnPlay,
            "Mode94AbilityOnPlay"
        ));
        BattleActionSlot slot = new BattleActionSlot(actor, 1);
        slot.AssignFreeAction(actor, ability, actor);
        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);
        return IsSuccessful(result, "FreeAbility") &&
            actor.GetBuffStack("Mode94AbilityOnPlay") == 1 &&
            ability.currentCooldown == ExpectedCooldown(ability) &&
            result.damage == 0;
    }

    static bool VerifyNoInteractionRegression()
    {
        return VerifyFreeNoInteraction(CardType.Defense) &&
            VerifyFreeNoInteraction(CardType.Dodge);
    }

    static bool VerifyFreeNoInteraction(string cardType)
    {
        CharacterData actor = CreateCharacter("mode94_no_interaction_" + cardType);
        CharacterData target = CreateCharacter("mode94_no_interaction_target_" + cardType);
        BattleCardState card = CreateCard(
            actor,
            "mode94_no_interaction_card_" + cardType,
            cardType,
            5
        );
        BattleActionSlot slot = new BattleActionSlot(actor, 1);
        slot.AssignFreeAction(actor, card, target);
        BattleExecutionItem item = new BattleExecutionItem(
            0,
            BattleExecutionItemType.FreeAction,
            null,
            slot
        );
        item.interactionType = BattleInteractionType.NoInteraction;
        BattleExecutionPlan plan = new BattleExecutionPlan();
        plan.AddItem(item);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(plan);
        return item.isCompleted &&
            item.status == BattleExecutionItemStatus.Skipped &&
            item.outcomeReason == BattleExecutionItemOutcomeReason.NoInteraction &&
            slot.isUsed && card.currentCooldown == 0 &&
            target.currentHP == target.maxHP;
    }

    static void ConfigureLifecycleProbe(Fixture fixture, string prefix)
    {
        ConfigureResourceRule(fixture, prefix + "Resource", 2);
        fixture.attackCard.cardData.effects.Add(CreateBuffEffect(
            BattleTiming.Hit,
            prefix + "Hit"
        ));
        fixture.attackCard.cardData.effects.Add(CreateBuffEffect(
            BattleTiming.AfterDamage,
            prefix + "AfterDamage"
        ));
    }

    static bool VerifyLifecycleOutcome(
        Fixture fixture,
        BattleResolveResult result,
        string prefix
    )
    {
        return result != null && result.isSuccess && result.triggeredEventChain &&
            result.damage == 6 &&
            fixture.attackCard.currentCooldown == ExpectedCooldown(fixture.attackCard) &&
            fixture.attackActor.GetBuffStack(prefix + "Resource") == 1 &&
            fixture.attackActor.GetBuffStack(prefix + "Hit") == 1 &&
            fixture.attackActor.GetBuffStack(prefix + "AfterDamage") == 1;
    }

    static void ConfigureResourceRule(
        Fixture fixture,
        string resourceID,
        int initialStack
    )
    {
        if (initialStack > 0)
        {
            fixture.attackActor.AddBuff(
                resourceID,
                resourceID,
                BuffCategory.AbilityBuff,
                initialStack,
                -1,
                BattleTiming.TurnEnd,
                BuffExpireRule.Permanent
            );
        }
        fixture.attackCard.cardData.resourceRule = new CardResourceRuleData
        {
            resourceType = "BuffStack",
            resourceID = resourceID,
            requiredStackForNormalVersion = 1,
            consumeAmountOnSuccess = 1,
            fallbackMinPoint = 0,
            fallbackMaxPoint = 0
        };
    }

    static CardEffectData CreateBuffEffect(string timing, string buffID)
    {
        return new CardEffectData
        {
            trigger = timing,
            effectType = CardEffectType.ApplyBuff,
            target = CardTargetType.Self,
            buffType = buffID,
            buffName = buffID,
            buffCategory = BuffCategory.AbilityBuff,
            stack = 1,
            duration = -1,
            checkTiming = BattleTiming.TurnEnd,
            expireRule = BuffExpireRule.Permanent,
            applyTiming = "Immediate"
        };
    }

    static Fixture CreatePlayerFixture(
        string id,
        int point,
        string deliveryMode = AttackDeliveryMode.Melee
    )
    {
        CharacterData attacker = CreateCharacter(id + "_player");
        CharacterData target = CreateCharacter(id + "_enemy");
        BattleCardState attack = CreateCard(
            attacker,
            id + "_attack",
            CardType.Attack,
            point,
            deliveryMode
        );
        BattleActionSlot slot = new BattleActionSlot(attacker, 1);
        slot.AssignFreeAction(attacker, attack, target);
        BattleExecutionItem item = new BattleExecutionItem(
            0,
            BattleExecutionItemType.FreeAction,
            null,
            slot
        );
        item.interactionType = BattleInteractionType.UnilateralAttack;
        BattleExecutionAction action = new BattleExecutionAction(
            attacker,
            attack,
            slot,
            null,
            target
        );
        return new Fixture
        {
            attackActor = attacker,
            target = target,
            attackCard = attack,
            actionSlot = slot,
            attackAction = action,
            executionItem = item
        };
    }

    static Fixture CreateEnemyFixture(
        string id,
        int point,
        string deliveryMode = AttackDeliveryMode.Melee
    )
    {
        CharacterData target = CreateCharacter(id + "_player");
        CharacterData attacker = CreateCharacter(id + "_enemy");
        BattleCardState attack = CreateCard(
            attacker,
            id + "_attack",
            CardType.Attack,
            point,
            deliveryMode
        );
        BattleEnemyIntent intent = new BattleEnemyIntent(
            id + "_intent",
            attacker,
            attack,
            target,
            1,
            1
        );
        BattleExecutionItem item = new BattleExecutionItem(
            0,
            BattleExecutionItemType.UnrespondedEnemyIntent,
            intent,
            null
        );
        item.interactionType = BattleInteractionType.UnilateralAttack;
        BattleExecutionAction action = new BattleExecutionAction(
            attacker,
            attack,
            null,
            intent,
            target
        );
        return new Fixture
        {
            attackActor = attacker,
            target = target,
            attackCard = attack,
            enemyIntent = intent,
            attackAction = action,
            executionItem = item
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

    static bool ResultsMatch(BattleResolveResult first, BattleResolveResult second)
    {
        return first != null && second != null && first.isSuccess && second.isSuccess &&
            first.resultType == second.resultType && first.damage == second.damage &&
            first.playerPoint == second.playerPoint &&
            first.enemyPoint == second.enemyPoint;
    }

    static bool IsSuccessful(BattleResolveResult result, string resultType)
    {
        return result != null && result.isSuccess && result.resultType == resultType;
    }

    static int ExpectedCooldown(BattleCardState card)
    {
        int baseCooldown = BattleCardManager.GetBaseCooldown(card.cardData);
        return baseCooldown > 0 ? baseCooldown + 1 : 0;
    }

    sealed class Fixture
    {
        public CharacterData attackActor;
        public CharacterData target;
        public BattleCardState attackCard;
        public BattleActionSlot actionSlot;
        public BattleEnemyIntent enemyIntent;
        public BattleExecutionAction attackAction;
        public BattleExecutionItem executionItem;
    }
}
