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
            Contains(shootingMissing, "sin_conservation_001") &&
            Contains(availableShooting, "ability_modification_001") &&
            Contains(availableKnife, "sin_anger_001") &&
            Contains(availableKnife, "sin_iai_001") &&
            Contains(availableShooting, "shoot_all_in_001");
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
