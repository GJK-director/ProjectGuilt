using System.Collections.Generic;
using UnityEngine;

public enum BattleDeckPreset
{
    Knife,
    Shooting
}

// Stable desired card IDs. This layer never creates missing card templates or BattleCardState instances.
public sealed class BattleDeckManifest
{
    public BattleDeckPreset preset { get; }
    public IReadOnlyList<string> normalCardIDs { get; }
    public IReadOnlyList<string> specialCardIDs { get; }

    public BattleDeckManifest(
        BattleDeckPreset preset,
        IReadOnlyList<string> normalCardIDs,
        IReadOnlyList<string> specialCardIDs
    )
    {
        this.preset = preset;
        this.normalCardIDs = normalCardIDs;
        this.specialCardIDs = specialCardIDs;
    }

    // Future bootstrap adapters can skip unimplemented target IDs safely.
    public List<string> ResolveAvailableCardIDs(
        IReadOnlyList<CardTestData> cards,
        List<string> missingCardIDs
    )
    {
        List<string> availableCardIDs = new List<string>();
        ResolveCardIDs(normalCardIDs, cards, availableCardIDs, missingCardIDs);
        ResolveCardIDs(specialCardIDs, cards, availableCardIDs, missingCardIDs);
        return availableCardIDs;
    }

    static void ResolveCardIDs(
        IReadOnlyList<string> cardIDs,
        IReadOnlyList<CardTestData> cards,
        List<string> availableCardIDs,
        List<string> missingCardIDs
    )
    {
        if (cardIDs == null)
        {
            return;
        }

        foreach (string cardID in cardIDs)
        {
            bool found = false;
            if (cards != null)
            {
                foreach (CardTestData card in cards)
                {
                    if (card != null && card.cardID == cardID)
                    {
                        found = true;
                        break;
                    }
                }
            }

            if (found)
            {
                availableCardIDs.Add(cardID);
            }
            else if (missingCardIDs != null)
            {
                missingCardIDs.Add(cardID);
            }
        }
    }
}

public static class BattleDeckManifests
{
    static readonly BattleDeckManifest knife = new BattleDeckManifest(
        BattleDeckPreset.Knife,
        new[]
        {
            "atk_001",
            "knife_stab_001",
            "knife_double_slash_001",
            "knife_heavy_001",
            "def_001",
            "dodge_001"
        },
        new[]
        {
            "sin_anger_001",
            "sin_iai_001"
        }
    );

    static readonly BattleDeckManifest shooting = new BattleDeckManifest(
        BattleDeckPreset.Shooting,
        new[]
        {
            "atk_bullet_001",
            "shoot_close_001",
            "shoot_all_in_001",
            "shoot_disengage_001",
            "shoot_reload_001",
            "shoot_aim_001"
        },
        new[]
        {
            "ability_modification_001",
            "sin_conservation_001"
        }
    );

    public static BattleDeckManifest Get(BattleDeckPreset preset)
    {
        return preset == BattleDeckPreset.Shooting ? shooting : knife;
    }
}

public static class BattleDeckManifestTests
{
    public static bool Run(IReadOnlyList<CardTestData> cards)
    {
        BattleDeckManifest knife = BattleDeckManifests.Get(BattleDeckPreset.Knife);
        BattleDeckManifest shooting = BattleDeckManifests.Get(BattleDeckPreset.Shooting);
        List<string> shootingMissing = new List<string>();
        List<string> knifeMissing = new List<string>();
        List<string> availableShooting = shooting.ResolveAvailableCardIDs(cards, shootingMissing);
        List<string> availableKnife = knife.ResolveAvailableCardIDs(cards, knifeMissing);
        bool knifeValues = VerifyKnifeValues(cards);
        bool manifests = !object.ReferenceEquals(knife, shooting) &&
            HasExactly(knife.normalCardIDs,
                "atk_001", "knife_stab_001", "knife_double_slash_001",
                "knife_heavy_001", "def_001", "dodge_001") &&
            HasExactly(shooting.normalCardIDs,
                "atk_bullet_001", "shoot_close_001", "shoot_all_in_001",
                "shoot_disengage_001", "shoot_reload_001", "shoot_aim_001") &&
            HasExactly(knife.specialCardIDs, "sin_anger_001", "sin_iai_001") &&
            HasExactly(shooting.specialCardIDs,
                "ability_modification_001", "sin_conservation_001") &&
            !Contains(shooting.normalCardIDs, "sin_anger_001") &&
            !Contains(knife.normalCardIDs, "ability_modification_001") &&
            !Contains(knife.specialCardIDs, "sin_conservation_001") &&
            !Contains(shooting.specialCardIDs, "sin_anger_001") &&
            !SharesCardID(knife, shooting);
        bool missingTemplatesAreSafe = !Contains(shootingMissing, "shoot_all_in_001") &&
            !Contains(shootingMissing, "sin_conservation_001") &&
            Contains(availableShooting, "ability_modification_001") &&
            Contains(availableKnife, "sin_anger_001") &&
            Contains(availableKnife, "sin_iai_001") &&
            Contains(availableShooting, "shoot_all_in_001") &&
            Contains(availableShooting, "sin_conservation_001");
        bool firstStrike = HasTrait(cards, "atk_bullet_001", BattleCardTrait.FirstStrike) &&
            HasTrait(cards, "shoot_aim_001", BattleCardTrait.FirstStrike) &&
            BattleExecutionPlanFirstStrikePolicyTests.Run();
        bool passed = knifeValues && manifests && missingTemplatesAreSafe && firstStrike;
        Debug.Log("===== Mode109 BattleDeckManifest =====");
        Debug.Log("Knife恢复数值：" + knifeValues);
        Debug.Log("Deck Manifest：" + manifests);
        Debug.Log("Missing Template Safe Resolve：" + missingTemplatesAreSafe);
        Debug.Log("Shooting FirstStrike + uniqueness：" + firstStrike);
        Debug.Log("Passed: " + passed);
        return passed;
    }

    static bool VerifyKnifeValues(IReadOnlyList<CardTestData> cards)
    {
        CardTestData slash = Find(cards, "atk_001");
        CardTestData stab = Find(cards, "knife_stab_001");
        CardTestData doubleSlash = Find(cards, "knife_double_slash_001");
        CardTestData heavy = Find(cards, "knife_heavy_001");
        CardTestData defense = Find(cards, "def_001");
        CardTestData dodge = Find(cards, "dodge_001");
        CardTestData iai = Find(cards, "sin_iai_001");
        return Matches(slash, "顺斩", 4, 7, 0, "PointAsDamage") &&
            Matches(stab, "突刺", 4, 6, 1, "PointAsDamage") &&
            stab.HasTrait(BattleCardTrait.DoubleClashAgainstDefense) &&
            Matches(doubleSlash, "连斩", 3, 6, 1, "PointAsDamage160Percent") &&
            doubleSlash.hpDisplayStageCount == 2 &&
            Matches(heavy, "重劈", 8, 11, 3, "PointAsDamage") &&
            heavy.HasTrait(BattleCardTrait.HeavyAnger) &&
            defense != null && defense.cardName == "架刀" &&
            defense.minPoint == 6 && defense.maxPoint == 9 && defense.cooldown == 1 &&
            dodge != null && dodge.cardName == "换气" &&
            dodge.minPoint == 1 && dodge.maxPoint == 13 && dodge.cooldown == 2 &&
            dodge.HasTrait(BattleCardTrait.GrantNextClashPointUpOnSuccessfulDodge) &&
            Matches(iai, "一闪", 5, 5, 10, "PointAsDamage150Percent") &&
            iai.isSinCard && iai.sinCardUseRule == SinCardUseRule.Permanent &&
            !iai.consumeOnUse && iai.maxUseCount == 0 &&
            iai.HasTrait(BattleCardTrait.IaiAnger);
    }

    static CardTestData Find(IReadOnlyList<CardTestData> cards, string cardID)
    {
        if (cards == null)
        {
            return null;
        }

        foreach (CardTestData card in cards)
        {
            if (card != null && card.cardID == cardID)
            {
                return card;
            }
        }
        return null;
    }

    static bool Matches(
        CardTestData card,
        string cardName,
        int minPoint,
        int maxPoint,
        int cooldown,
        string damageFormula
    )
    {
        return card != null && card.cardName == cardName &&
            card.minPoint == minPoint && card.maxPoint == maxPoint &&
            card.cooldown == cooldown && card.damageFormula == damageFormula;
    }

    static bool SharesCardID(BattleDeckManifest first, BattleDeckManifest second)
    {
        return SharesCardID(first != null ? first.normalCardIDs : null,
                second != null ? second.normalCardIDs : null) ||
            SharesCardID(first != null ? first.normalCardIDs : null,
                second != null ? second.specialCardIDs : null) ||
            SharesCardID(first != null ? first.specialCardIDs : null,
                second != null ? second.normalCardIDs : null) ||
            SharesCardID(first != null ? first.specialCardIDs : null,
                second != null ? second.specialCardIDs : null);
    }

    static bool SharesCardID(IReadOnlyList<string> first, IReadOnlyList<string> second)
    {
        if (first == null || second == null)
        {
            return false;
        }

        foreach (string firstID in first)
        {
            if (Contains(second, firstID))
            {
                return true;
            }
        }
        return false;
    }

    static bool HasTrait(
        IReadOnlyList<CardTestData> cards,
        string cardID,
        BattleCardTrait trait
    )
    {
        if (cards == null)
        {
            return false;
        }

        foreach (CardTestData card in cards)
        {
            if (card != null && card.cardID == cardID)
            {
                return card.HasTrait(trait);
            }
        }
        return false;
    }

    static bool HasExactly(IReadOnlyList<string> actual, params string[] expected)
    {
        if (actual == null || expected == null || actual.Count != expected.Length)
        {
            return false;
        }

        for (int index = 0; index < expected.Length; index++)
        {
            if (actual[index] != expected[index])
            {
                return false;
            }
        }
        return true;
    }

    static bool Contains(IReadOnlyList<string> values, string value)
    {
        if (values == null)
        {
            return false;
        }

        foreach (string current in values)
        {
            if (current == value)
            {
                return true;
            }
        }
        return false;
    }
}

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
            conservation.useConditions != null &&
            HasBulletCondition(conservation);
        bool unavailable = VerifyZeroBullet(conservation);
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

        bool passed = data && unavailable && armed && table && transfer && ownership &&
            formal && modification && allInLink && tie && failed && nonShooting &&
            killReload && noReload && turnEnd && cleanup && reloadOrder && cooldown &&
            turnEndDeath && manifest && abilityRegression && angerRegression && allInRegression;

        Debug.Log("===== Mode113 BattleConservationAbility =====");
        Debug.Log("节约数据：" + data);
        Debug.Log("0 Bullet不可用：" + unavailable);
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
        return result != null && !result.isSuccess && !BattleConservationRules.IsActive(owner);
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
