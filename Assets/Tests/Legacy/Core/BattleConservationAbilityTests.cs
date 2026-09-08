using System.Collections.Generic;
using UnityEngine;

public static class BattleConservationAbilityTests
{
    public static bool Run(List<CardTestData> cards)
    {
        CardTestData conservation = Find(cards, "sin_conservation_001");
        CardTestData closeShoot = Find(cards, "shoot_close_001");
        CardTestData allIn = Find(cards, "shoot_all_in_001");

        bool data = conservation != null &&
            conservation.cardType == CardType.Ability &&
            conservation.isSinCard &&
            conservation.sinCardCategory == SinCardCategory.Ability &&
            conservation.sinCardUseRule == SinCardUseRule.Permanent &&
            conservation.cooldown == 3 &&
            !HasBulletCondition(conservation);
        bool zeroBulletEligible = VerifyZeroBullet(conservation);
        bool cooldownBlocks = VerifyZeroBulletCooldown(conservation);
        bool armed = VerifyAbilityArms(conservation);
        bool table = VerifyPointTable();
        bool transfer = VerifyCurrentBulletTransfer(closeShoot);
        bool ownership = VerifyCardStateOwnership(closeShoot);
        bool formal = VerifyFormalResolver(closeShoot);
        bool modification = VerifyModificationStack(closeShoot);
        bool allInLink = VerifyAllInLink(allIn);
        bool tie = VerifyTieDoesNotReread(closeShoot);
        bool failed = VerifyFailedShotConsumesGrant(closeShoot);
        bool nonShooting = VerifyNonShootingDoesNotConsume();
        bool killReload = VerifyKillReload(closeShoot);
        bool noReload = VerifyUnarmedKillDoesNotReload(closeShoot);
        bool turnEnd = VerifyTurnEndPenaltyTable();
        bool cleanup = VerifyTurnEndCleanup();
        bool reloadOrder = VerifyReloadThenTurnEnd(closeShoot, allIn);
        bool cooldown = VerifyCooldown(conservation);
        bool turnEndDeath = VerifyTurnEndDeath();
        bool manifest = BattleDeckManifestTests.Run(cards);
        bool abilityRegression = BattleAbilityPhaseBasicTests.Run();
        bool angerRegression = BattleAngerAndModificationAbilityTests.Run(cards);
        bool allInRegression = BattleAllInBasicTests.Run(cards);
        BattleBasicShootingLoopTests.Run(cards);

        bool passed = data && zeroBulletEligible && cooldownBlocks && armed && table && transfer && ownership &&
            formal && modification && allInLink && tie && failed && nonShooting &&
            killReload && noReload && turnEnd && cleanup && reloadOrder && cooldown &&
            turnEndDeath && manifest && abilityRegression && angerRegression && allInRegression;

        Debug.Log("===== Mode113 BattleConservationAbility =====");
        Debug.Log("节约数据：" + data);
        Debug.Log("0 Bullet可用：" + zeroBulletEligible);
        Debug.Log("0 Bullet时CD仍阻止：" + cooldownBlocks);
        Debug.Log("Ability只Arm：" + armed);
        Debug.Log("Clash前按当前Bullet赋值：" + transfer);
        Debug.Log("Bonus属于CardState：" + ownership);
        Debug.Log("点数表：" + table);
        Debug.Log("Formal Resolver点数：" + formal);
        Debug.Log("Modification叠加：" + modification);
        Debug.Log("ALL IN联动：" + allInLink);
        Debug.Log("Tie不重读：" + tie);
        Debug.Log("失败后不返还：" + failed);
        Debug.Log("非射击不接强化：" + nonShooting);
        Debug.Log("强化卡击杀Reload：" + killReload);
        Debug.Log("未强化卡击杀不Reload：" + noReload);
        Debug.Log("TurnEnd惩罚表：" + turnEnd);
        Debug.Log("TurnEnd清理：" + cleanup);
        Debug.Log("Kill Reload→TurnEnd顺序：" + reloadOrder);
        Debug.Log("CD3：" + cooldown);
        Debug.Log("TurnEnd致死结束战斗：" + turnEndDeath);
        Debug.Log("DeckManifest完整：" + manifest);
        Debug.Log("Mode108射击回归：已执行（Run返回void，无法聚合返回值）");
        Debug.Log("Mode110回归：" + abilityRegression);
        Debug.Log("Mode111回归：" + angerRegression);
        Debug.Log("Mode112回归：" + allInRegression);
        Debug.Log("Passed: " + passed);
        return passed;
    }

    static bool HasBulletCondition(CardTestData card)
    {
        if (card == null || card.useConditions == null)
        {
            return false;
        }

        foreach (CardUseConditionData condition in card.useConditions)
        {
            if (condition != null && condition.conditionType == "BuffStackAtLeast" &&
                condition.buffType == BattleResourceID.Bullet && condition.value >= 1)
            {
                return true;
            }
        }
        return false;
    }

    static bool VerifyZeroBullet(CardTestData source)
    {
        CharacterData owner = Unit("mode113_zero");
        BattleCardState state = State(owner, source, "mode113_zero_conservation");
        BattleActionSlot slot = new BattleActionSlot(owner, 1);
        slot.AssignFreeAction(owner, state, owner);
        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);
        return BattleBulletRules.GetBullet(owner) == 0 && result != null &&
            result.isSuccess && BattleConservationRules.IsActive(owner);
    }

    static bool VerifyZeroBulletCooldown(CardTestData source)
    {
        CharacterData owner = Unit("mode113_zero_cooldown");
        BattleCardState state = State(
            owner,
            source,
            "mode113_zero_cooldown_conservation"
        );
        state.currentCooldown = 3;
        CardEligibilityResult result = BattleCardManager.EvaluateCardEligibility(
            owner,
            owner,
            state
        );
        return BattleBulletRules.GetBullet(owner) == 0 && result != null &&
            !result.isEligible && result.failureReason ==
            CardEligibilityFailureReason.CardOnCooldown;
    }

    static bool VerifyAbilityArms(CardTestData source)
    {
        CharacterData owner = Unit("mode113_arm");
        BattleBulletRules.AddBulletCapped(owner, 4);
        BattleCardState state = State(owner, source, "mode113_arm_conservation");
        BattleActionSlot slot = new BattleActionSlot(owner, 1);
        slot.AssignFreeAction(owner, state, owner);
        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);
        return result != null && result.isSuccess &&
            BattleConservationRules.IsActive(owner) &&
            BattleConservationRules.HasPendingPointGrant(owner) &&
            !state.hasConservationPointBonus;
    }

    static bool VerifyPointTable()
    {
        return BattleConservationRules.GetPointBonusForBullet(6) == 1 &&
            BattleConservationRules.GetPointBonusForBullet(5) == 1 &&
            BattleConservationRules.GetPointBonusForBullet(4) == 2 &&
            BattleConservationRules.GetPointBonusForBullet(3) == 3 &&
            BattleConservationRules.GetPointBonusForBullet(2) == 4 &&
            BattleConservationRules.GetPointBonusForBullet(1) == 6 &&
            BattleConservationRules.GetPointBonusForBullet(0) == 0;
    }

    static bool VerifyCurrentBulletTransfer(CardTestData source)
    {
        CharacterData owner = Unit("mode113_transfer");
        BattleBulletRules.AddBulletCapped(owner, 4);
        BattleConservationRules.Activate(owner);
        if (!ConsumeBulletForTest(owner, 1) || BattleBulletRules.GetBullet(owner) != 3)
        {
            return false;
        }
        BattleCardState state = State(owner, Clone(source, 5, 5), "mode113_transfer_shot");
        bool assigned = BattleConservationRules.TryAssignPendingBonus(owner, state);
        return assigned && state.hasConservationPointBonus &&
            state.conservationPointBonus == 3 &&
            !BattleConservationRules.HasPendingPointGrant(owner);
    }

    static bool VerifyCardStateOwnership(CardTestData source)
    {
        CharacterData owner = Unit("mode113_ownership");
        BattleBulletRules.AddBulletCapped(owner, 2);
        BattleConservationRules.Activate(owner);
        BattleCardState first = State(owner, Clone(source, 5, 5), "mode113_first");
        BattleCardState second = State(owner, Clone(source, 5, 5), "mode113_second");
        BattleConservationRules.TryAssignPendingBonus(owner, first);
        return first.hasConservationPointBonus && first.conservationPointBonus == 4 &&
            !second.hasConservationPointBonus && source.minPoint == 5;
    }

    static bool VerifyFormalResolver(CardTestData source)
    {
        CharacterData player = Unit("mode113_formal_player");
        CharacterData enemy = Unit("mode113_formal_enemy");
        BattleBulletRules.AddBulletCapped(player, 2);
        BattleConservationRules.Activate(player);
        BattleCardState shot = State(player, Clone(source, 5, 5), "mode113_formal_shot");
        BattleCardState enemyAttack = State(enemy, FixedAttack("mode113_formal_enemy_attack", 1), "mode113_formal_enemy_attack");
        BattleEnemyIntent intent = new BattleEnemyIntent("mode113_formal_intent", enemy, enemyAttack, player, 1);
        BattleActionSlot slot = new BattleActionSlot(player, 1);
        slot.AssignResponse(player, shot, intent, false);
        BattleClashSession session = BattleResolver.CreateRespondedAttackClashSession(slot, intent);
        return session != null && shot.conservationPointBonus == 4 &&
            session.RollNextAttempt() && session.SideAPoint == 9;
    }

    static bool VerifyModificationStack(CardTestData source)
    {
        CharacterData player = Unit("mode113_modification");
        CharacterData enemy = Unit("mode113_modification_enemy");
        BattleBulletRules.AddBulletCapped(player, 2);
        BattleModificationRules.Activate(player);
        BattleConservationRules.Activate(player);
        BattleCardState shot = State(player, Clone(source, 5, 5), "mode113_modification_shot");
        BattleCardState enemyAttack = State(enemy, FixedAttack("mode113_modification_attack", 1), "mode113_modification_attack");
        BattleEnemyIntent intent = new BattleEnemyIntent("mode113_modification_intent", enemy, enemyAttack, player, 1);
        BattleActionSlot slot = new BattleActionSlot(player, 1);
        slot.AssignResponse(player, shot, intent, false);
        BattleClashSession session = BattleResolver.CreateRespondedAttackClashSession(slot, intent);
        return session != null && session.RollNextAttempt() && session.SideAPoint == 11;
    }

    static bool VerifyAllInLink(CardTestData source)
    {
        if (source == null) return false;
        CharacterData player = Unit("mode113_allin");
        BattleBulletRules.AddBulletCapped(player, 2);
        BattleConservationRules.Activate(player);
        BattleCardState allIn = State(player, Clone(source, 2, 2), "mode113_allin_card");
        bool assigned = BattleConservationRules.TryAssignPendingBonus(player, allIn);
        return assigned && allIn.conservationPointBonus == 4 &&
            BattleConservationRules.IsShootingAttack(allIn);
    }

    static bool VerifyTieDoesNotReread(CardTestData source)
    {
        CharacterData player = Unit("mode113_tie_player");
        CharacterData enemy = Unit("mode113_tie_enemy");
        BattleBulletRules.AddBulletCapped(player, 2);
        BattleConservationRules.Activate(player);
        BattleCardState shot = State(player, Clone(source, 5, 5), "mode113_tie_shot");
        BattleCardState enemyAttack = State(enemy, FixedAttack("mode113_tie_attack", 9), "mode113_tie_attack");
        BattleEnemyIntent intent = new BattleEnemyIntent("mode113_tie_intent", enemy, enemyAttack, player, 1);
        BattleActionSlot slot = new BattleActionSlot(player, 1);
        slot.AssignResponse(player, shot, intent, false);
        BattleClashSession session = BattleResolver.CreateRespondedAttackClashSession(slot, intent);
        if (session == null || !session.RollNextAttempt() || !session.RequiresAnotherRoll || session.IsFinalized)
        {
            return false;
        }
        if (!ConsumeBulletForTest(player, 1) || BattleBulletRules.GetBullet(player) != 1)
        {
            return false;
        }
        return shot.conservationPointBonus == 4 &&
            !BattleConservationRules.HasPendingPointGrant(player) &&
            session.RollNextAttempt() && session.SideAPoint == 9;
    }

    static bool VerifyFailedShotConsumesGrant(CardTestData source)
    {
        CharacterData player = Unit("mode113_failed_player");
        CharacterData enemy = Unit("mode113_failed_enemy");
        BattleBulletRules.AddBulletCapped(player, 2);
        BattleConservationRules.Activate(player);
        BattleCardState shot = State(player, Clone(source, 5, 5), "mode113_failed_shot");
        BattleCardState enemyAttack = State(enemy, FixedAttack("mode113_failed_attack", 20), "mode113_failed_attack");
        BattleEnemyIntent intent = new BattleEnemyIntent("mode113_failed_intent", enemy, enemyAttack, player, 1);
        BattleActionSlot slot = new BattleActionSlot(player, 1);
        slot.AssignResponse(player, shot, intent, false);
        BattleClashSession session = BattleResolver.CreateRespondedAttackClashSession(slot, intent);
        return session != null && session.RollNextAttempt() && session.IsFinalized &&
            !BattleConservationRules.HasPendingPointGrant(player);
    }

    static bool VerifyNonShootingDoesNotConsume()
    {
        CharacterData owner = Unit("mode113_non_shooting");
        BattleBulletRules.AddBulletCapped(owner, 2);
        BattleConservationRules.Activate(owner);
        BattleCardState melee = State(owner, FixedAttack("mode113_melee", 5), "mode113_melee");
        bool rejected = !BattleConservationRules.TryAssignPendingBonus(owner, melee);
        bool stillPending = BattleConservationRules.HasPendingPointGrant(owner);
        return rejected && stillPending;
    }

    static bool VerifyKillReload(CardTestData source)
    {
        CharacterData owner = Unit("mode113_reload");
        CharacterData target = Unit("mode113_reload_target");
        target.currentHP = 1;
        BattleBulletRules.AddBulletCapped(owner, 2);
        BattleConservationRules.Activate(owner);
        BattleCardState shot = BattleCardManager.CreateBattleCard(
            owner,
            source,
            "mode113_reload_shot"
        );
        BattleActionSlot slot = new BattleActionSlot(owner, 1);
        slot.AssignFreeAction(owner, shot, target);
        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);
        return result != null && result.isSuccess && target.IsDead() &&
            BattleBulletRules.GetBullet(owner) == 6 &&
            !shot.conservationKillReloadArmed;
    }

    static bool VerifyUnarmedKillDoesNotReload(CardTestData source)
    {
        CharacterData owner = Unit("mode113_no_reload");
        CharacterData target = Unit("mode113_no_reload_target");
        target.currentHP = 1;
        BattleBulletRules.AddBulletCapped(owner, 2);
        BattleCardState shot = BattleCardManager.CreateBattleCard(
            owner,
            source,
            "mode113_no_reload_shot"
        );
        BattleActionSlot slot = new BattleActionSlot(owner, 1);
        slot.AssignFreeAction(owner, shot, target);
        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);
        return result != null && result.isSuccess && target.IsDead() &&
            BattleBulletRules.GetBullet(owner) == 1;
    }

    static bool VerifyTurnEndPenaltyTable()
    {
        int[] expected = { 30, 18, 12, 8, 5, 0, 0 };
        for (int bullet = 0; bullet <= 6; bullet++)
        {
            CharacterData owner = Unit("mode113_penalty_" + bullet);
            BattleBulletRules.AddBulletCapped(owner, bullet);
            BattleConservationRules.Activate(owner);
            int damage = BattleConservationRules.ResolveTurnEnd(owner);
            int expectedDamage = Mathf.CeilToInt(owner.maxHP * expected[bullet] / 100f);
            if (damage != expectedDamage || owner.currentHP != owner.maxHP - expectedDamage)
            {
                return false;
            }
        }
        return true;
    }

    static bool VerifyTurnEndCleanup()
    {
        CharacterData owner = Unit("mode113_cleanup");
        BattleConservationRules.Activate(owner);
        BattleConservationRules.ResolveTurnEnd(owner);
        return !BattleConservationRules.IsActive(owner) &&
            !BattleConservationRules.HasPendingPointGrant(owner);
    }

    static bool VerifyReloadThenTurnEnd(CardTestData closeSource, CardTestData allInSource)
    {
        if (closeSource == null || allInSource == null) return false;
        CharacterData owner = Unit("mode113_reload_order");
        BattleModificationRules.Activate(owner);
        BattleBulletRules.AddBulletCapped(owner, 4);
        BattleConservationRules.Activate(owner);
        CharacterData target = Unit("mode113_reload_order_target");
        target.currentHP = 1;
        BattleCardState card = BattleCardManager.CreateBattleCard(
            owner,
            allInSource,
            "mode113_reload_order_card"
        );
        BattleActionSlot slot = new BattleActionSlot(owner, 1);
        slot.AssignFreeAction(owner, card, target);
        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);
        if (result == null || !result.isSuccess || !target.IsDead() ||
            BattleBulletRules.GetBullet(owner) != 4)
        {
            return false;
        }
        int damage = BattleConservationRules.ResolveTurnEnd(owner);
        return damage == 5 && owner.currentHP == 95;
    }

    static bool VerifyCooldown(CardTestData source)
    {
        CharacterData owner = Unit("mode113_cooldown");
        BattleBulletRules.AddBulletCapped(owner, 1);
        BattleCardState state = BattleCardManager.CreateBattleCard(
            owner,
            source,
            "mode113_cooldown_card"
        );
        BattleActionSlot slot = new BattleActionSlot(owner, 1);
        slot.AssignFreeAction(owner, state, owner);
        BattleConservationRules.Activate(owner);
        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);
        bool applied = result != null && result.isSuccess && state.currentCooldown == 3 &&
            state.skipNextTurnEndCooldownTick;
        BattleCardManager.ReduceCooldownsAtTurnEnd(owner);
        bool skipped = state.currentCooldown == 3 && !state.skipNextTurnEndCooldownTick;
        BattleCardManager.ReduceCooldownsAtTurnEnd(owner);
        BattleCardManager.ReduceCooldownsAtTurnEnd(owner);
        BattleCardManager.ReduceCooldownsAtTurnEnd(owner);
        return applied && skipped && state.currentCooldown == 0;
    }

    static bool VerifyTurnEndDeath()
    {
        CharacterData ally = Unit("mode113_turn_end_death_ally");
        CharacterData enemy = Unit("mode113_turn_end_death_enemy");
        ally.currentHP = 30;
        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(ally, null, enemy);
        runtimeState.SetExecutionPlan(new BattleExecutionPlan
        {
            isCompleted = true
        });
        BattleConservationRules.Activate(ally);

        BattleLifecycleController lifecycle = new BattleLifecycleController(runtimeState);
        string failureMessage;
        if (!lifecycle.TryInitializeToPrepare(out failureMessage) ||
            !runtimeState.TryTransitionTo(BattleLifecyclePhase.PlanReady, out failureMessage) ||
            !runtimeState.TryTransitionTo(BattleLifecyclePhase.Executing, out failureMessage) ||
            !runtimeState.TryTransitionTo(BattleLifecyclePhase.TurnResolved, out failureMessage))
        {
            return false;
        }

        bool ended = lifecycle.TryEndCurrentTurn(out failureMessage);
        return ended && runtimeState.IsBattleEnded &&
            runtimeState.LifecyclePhase == BattleLifecyclePhase.BattleEnded &&
            runtimeState.battleResult == BattleResult.Defeat;
    }

    static bool ConsumeBulletForTest(CharacterData owner, int amount)
    {
        if (owner == null || amount <= 0)
        {
            return false;
        }

        bool success = owner.TryConsumeBuffStackAsResource(
            BattleResourceID.Bullet,
            amount,
            out int consumed
        );
        return success && consumed == amount;
    }

    static CharacterData Unit(string id)
    {
        return new CharacterData(id, 100, 5, 5, id);
    }

    static BattleCardState State(CharacterData owner, CardTestData card, string id)
    {
        return new BattleCardState(owner, card, id);
    }

    static BattleCardState State(CharacterData owner, CardTestData card, string id, bool unused)
    {
        return State(owner, card, id);
    }

    static CardTestData FixedAttack(string id, int point)
    {
        return new CardTestData
        {
            cardID = id,
            cardName = id,
            cardType = CardType.Attack,
            attackDeliveryMode = AttackDeliveryMode.Melee,
            isClashable = true,
            minPoint = point,
            maxPoint = point,
            damageFormula = "PointAsDamage"
        };
    }

    static CardTestData Clone(CardTestData source, int minPoint, int maxPoint)
    {
        return new CardTestData
        {
            cardID = source.cardID,
            cardName = source.cardName,
            cardType = source.cardType,
            attackDeliveryMode = source.attackDeliveryMode,
            isClashable = source.isClashable,
            isSinCard = source.isSinCard,
            minPoint = minPoint,
            maxPoint = maxPoint,
            cooldown = source.cooldown,
            damageFormula = source.damageFormula,
            traits = source.traits,
            resourceRule = source.resourceRule
        };
    }

    static CardTestData Find(IReadOnlyList<CardTestData> cards, string id)
    {
        if (cards == null) return null;
        foreach (CardTestData card in cards)
        {
            if (card != null && card.cardID == id) return card;
        }
        return null;
    }
}

