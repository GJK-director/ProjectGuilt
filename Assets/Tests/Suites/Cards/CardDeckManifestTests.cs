using System.Collections.Generic;

public static class CardDeckManifestTests
{
    public static bool KnifeCardDefinitionsMatchExpectedValues(
        IReadOnlyList<CardTestData> cards
    )
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
            Matches(doubleSlash, "连斩", 3, 6, 1, "PointAsDamage") &&
            HasDamageImpactPercents(doubleSlash, 80, 80) &&
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

    public static bool DeckManifestsUseExpectedMembershipAndDoNotShareCards()
    {
        BattleDeckManifest knife = BattleDeckManifests.Get(BattleDeckPreset.Knife);
        BattleDeckManifest shooting = BattleDeckManifests.Get(BattleDeckPreset.Shooting);
        return !object.ReferenceEquals(knife, shooting) &&
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
    }

    public static bool ManifestResolutionFindsRequiredAvailableCards(
        IReadOnlyList<CardTestData> cards
    )
    {
        BattleDeckManifest knife = BattleDeckManifests.Get(BattleDeckPreset.Knife);
        BattleDeckManifest shooting = BattleDeckManifests.Get(BattleDeckPreset.Shooting);
        List<string> shootingMissing = new List<string>();
        List<string> knifeMissing = new List<string>();
        List<string> availableShooting = shooting.ResolveAvailableCardIDs(
            cards,
            shootingMissing
        );
        List<string> availableKnife = knife.ResolveAvailableCardIDs(
            cards,
            knifeMissing
        );
        return !Contains(shootingMissing, "shoot_all_in_001") &&
            !Contains(shootingMissing, "sin_conservation_001") &&
            Contains(availableShooting, "ability_modification_001") &&
            Contains(availableKnife, "sin_anger_001") &&
            Contains(availableKnife, "sin_iai_001") &&
            Contains(availableShooting, "shoot_all_in_001") &&
            Contains(availableShooting, "sin_conservation_001");
    }

    public static bool ShootingFirstStrikeCardsHaveExpectedTraits(
        IReadOnlyList<CardTestData> cards
    )
    {
        return HasTrait(cards, "atk_bullet_001", BattleCardTrait.FirstStrike) &&
            HasTrait(cards, "shoot_aim_001", BattleCardTrait.FirstStrike);
    }

    private static CardTestData Find(
        IReadOnlyList<CardTestData> cards,
        string cardID
    )
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

    private static bool Matches(
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

    private static bool HasDamageImpactPercents(
        CardTestData card,
        params int[] expected
    )
    {
        if (card == null || card.damageImpactPercents == null ||
            expected == null || card.damageImpactPercents.Length != expected.Length)
        {
            return false;
        }

        for (int index = 0; index < expected.Length; index++)
        {
            if (card.damageImpactPercents[index] != expected[index])
            {
                return false;
            }
        }

        return true;
    }

    private static bool SharesCardID(
        BattleDeckManifest first,
        BattleDeckManifest second
    )
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

    private static bool SharesCardID(
        IReadOnlyList<string> first,
        IReadOnlyList<string> second
    )
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

    private static bool HasTrait(
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

    private static bool HasExactly(
        IReadOnlyList<string> actual,
        params string[] expected
    )
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

    private static bool Contains(
        IReadOnlyList<string> values,
        string value
    )
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
