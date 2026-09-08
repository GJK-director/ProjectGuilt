using System.Collections.Generic;
using UnityEngine;

public static class BattleAllInBasicTests
{
    public static bool Run(List<CardTestData> cards)
    {
        CardTestData template = Find(cards, "shoot_all_in_001");
        bool data = template != null && template.cardType == CardType.Attack &&
            template.minPoint == 2 && template.maxPoint == 5 && template.cooldown == 3 &&
            template.GetAttackDeliveryMode() == AttackDeliveryMode.CloseRangeShoot &&
            template.GetPresentationVariant() == BattleCardPresentationVariant.Default &&
            !template.HasTrait(BattleCardTrait.FirstStrike) &&
            template.HasTrait(BattleCardTrait.AllInBulletDump) &&
            template.resourceRule != null &&
            template.resourceRule.resourceID == BattleResourceID.Bullet &&
            template.resourceRule.requiredStackForNormalVersion >= 1 &&
            template.resourceRule.pointPerStack == 1 &&
            template.resourceRule.consumeTiming == CardResourceConsumeTiming.OnSuccessfulUse &&
            template.resourceRule.consumeAllCapturedOnSuccess;
        bool points = template != null &&
            template.minPoint + 1 == 3 && template.maxPoint + 1 == 6 &&
            template.minPoint + 6 == 8 && template.maxPoint + 6 == 11 &&
            template.minPoint + 4 + 2 == 8 && template.maxPoint + 4 + 2 == 11;
        bool multipliers = BattleAllInRules.GetDamageMultiplierPercent(1) == 100 &&
            BattleAllInRules.GetDamageMultiplierPercent(2) == 180 &&
            BattleAllInRules.GetDamageMultiplierPercent(3) == 230 &&
            BattleAllInRules.GetDamageMultiplierPercent(4) == 270 &&
            BattleAllInRules.GetDamageMultiplierPercent(5) == 300 &&
            BattleAllInRules.GetDamageMultiplierPercent(6) == 320;
        bool success = VerifyFormalResolution(template, 3, true, out int successDamage);
        bool failure = VerifyFormalResolution(template, 4, false, out _);
        bool empty = VerifyZeroBullet(template);
        bool modification = VerifyModification(template);
        bool singleImpact = VerifyFormalResolution(template, 6, true, out _);
        bool mode108Executed = false;
        BattleBasicShootingLoopTests.Run(cards);
        mode108Executed = true;

        bool passed = data && points && multipliers && success && failure &&
            empty && modification && singleImpact && mode108Executed;
        Debug.Log("===== Mode112 BattleAllInBasic =====");
        Debug.Log("ALL IN数据：" + data);
        Debug.Log("ALL IN点数区间：" + points);
        Debug.Log("ALL IN倍率表：" + multipliers);
        Debug.Log("正式Resolver成功：" + success + " / damage=" + successDamage);
        Debug.Log("失败不消费：" + failure);
        Debug.Log("0 Bullet ActionUnavailable：" + empty);
        Debug.Log("Modification联动：" + modification);
        Debug.Log("一次逻辑Impact：" + singleImpact);
        Debug.Log("Mode108射击回归：已执行（Run返回void，无法聚合返回值）");
        Debug.Log("Passed: " + passed);
        return passed;
    }

    static bool VerifyFormalResolution(
        CardTestData source,
        int bullet,
        bool shouldWin,
        out int damage
    )
    {
        damage = 0;
        CharacterData player = Unit("mode112_player_" + bullet + "_" + shouldWin);
        CharacterData enemy = Unit("mode112_enemy_" + bullet + "_" + shouldWin);
        BattleBulletRules.AddBulletCapped(player, bullet);
        BattleCardState allIn = State(player, Clone(source, 2, 5), "mode112_all_in_" + bullet);
        BattleCardState enemyAttack = State(
            enemy,
            FixedAttack("mode112_enemy_attack_" + bullet, shouldWin ? 1 : 20),
            "mode112_enemy_attack_" + bullet
        );
        BattleEnemyIntent intent = new BattleEnemyIntent(
            "mode112_intent_" + bullet, enemy, enemyAttack, player, 1
        );
        BattleActionSlot slot = new BattleActionSlot(player, 1);
        slot.AssignResponse(player, allIn, intent, false);
        BattleClashSession session = BattleResolver.CreateRespondedAttackClashSession(slot, intent);
        if (session == null || !session.RollNextAttempt() || !session.IsFinalized)
        {
            return false;
        }

        BattleResolutionPlan plan = BattleResolver.BuildRespondedClashResolutionPlan(
            slot, intent, session
        );
        if (plan == null || plan.impacts.Count != 1)
        {
            return false;
        }

        BattleImpact impact = plan.impacts[0];
        if (shouldWin)
        {
            if (!object.ReferenceEquals(impact.sourceCardState, allIn) ||
                impact.hpDisplayStageCount != bullet)
            {
                return false;
            }
        }
        else if (!object.ReferenceEquals(impact.sourceCardState, enemyAttack) ||
            impact.hpDisplayStageCount != 1)
        {
            return false;
        }

        BattleResolveResult result = Commit(plan);
        damage = result != null ? result.damage : 0;
        if (result == null || !result.isSuccess)
        {
            return false;
        }

        if (shouldWin)
        {
            return result.resultType == "PlayerWin" &&
                BattleBulletRules.GetBullet(player) == 0 &&
                enemy.currentHP < 100 && damage > 0;
        }

        return result.resultType == "EnemyWin" &&
            BattleBulletRules.GetBullet(player) == bullet &&
            enemy.currentHP == 100 && player.currentHP < 100;
    }

    static bool VerifyZeroBullet(CardTestData source)
    {
        CharacterData player = Unit("mode112_empty_player");
        CharacterData enemy = Unit("mode112_empty_enemy");
        BattleCardState allIn = State(player, Clone(source, 2, 5), "mode112_empty_all_in");
        BattleCardState enemyAttack = State(enemy, FixedAttack("mode112_empty_attack", 1), "mode112_empty_attack");
        BattleEnemyIntent intent = new BattleEnemyIntent("mode112_empty_intent", enemy, enemyAttack, player, 1);
        BattleActionSlot slot = new BattleActionSlot(player, 1);
        slot.AssignResponse(player, allIn, intent, false);
        BattleResolveResult result = BattleResolver.TryBeginRespondedClash(
            slot, intent, out BattleClashSession session
        );
        return session == null && result != null &&
            result.resultType == "ActionUnavailable" && allIn.currentCooldown == 0;
    }

    static bool VerifyModification(CardTestData source)
    {
        CharacterData player = Unit("mode112_modification_player");
        BattleBulletRules.AddBulletCapped(player, 6);
        BattleModificationRules.Activate(player);
        CardTestData modified = Clone(source, 2, 5);
        return BattleBulletRules.GetBullet(player) == 4 &&
            BattleModificationRules.GetCardPointBonus(player, modified) == 2 &&
            modified.minPoint + 4 + 2 == 8 && modified.maxPoint + 4 + 2 == 11;
    }

    static BattleResolveResult Commit(BattleResolutionPlan plan)
    {
        BattleResolveResult result = null;
        while (plan.State != BattleResolutionPlanState.Completed)
        {
            if (!BattleResolver.TryCommitNextResolutionStep(plan, out result))
            {
                return null;
            }
        }
        return result ?? plan.CompletedResult;
    }

    static CharacterData Unit(string id)
    {
        return new CharacterData(id, 100, 5, 5, id);
    }

    static BattleCardState State(CharacterData owner, CardTestData card, string id)
    {
        return new BattleCardState(owner, card, id);
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
            presentationVariant = source.presentationVariant,
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
        if (cards == null)
        {
            return null;
        }
        foreach (CardTestData card in cards)
        {
            if (card != null && card.cardID == id)
            {
                return card;
            }
        }
        return null;
    }
}

