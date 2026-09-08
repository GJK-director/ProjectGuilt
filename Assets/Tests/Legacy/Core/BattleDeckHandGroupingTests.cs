using System.Collections.Generic;
using UnityEngine;

public static class BattleDeckHandGroupingTests
{
    public static bool Run(List<CardTestData> cards)
    {
        BattleDeckManifest knife = BattleDeckManifests.Get(BattleDeckPreset.Knife);
        BattleDeckManifest shooting = BattleDeckManifests.Get(BattleDeckPreset.Shooting);
        BattleDefinitionBootstrapResult knifeResult = CreateResult(BattleDeckPreset.Knife);
        BattleDefinitionBootstrapResult shootingResult = CreateResult(BattleDeckPreset.Shooting);
        CharacterData knifePlayer = knifeResult == null || knifeResult.runtimeState == null
            ? null
            : knifeResult.runtimeState.allyA;
        CharacterData shootingPlayer = shootingResult == null || shootingResult.runtimeState == null
            ? null
            : shootingResult.runtimeState.allyA;

        List<BattleCardState> knifeNormal =
            BattleDeckHandGroupingRules.GetCardsForGroup(knifePlayer, false);
        List<BattleCardState> knifeSpecial =
            BattleDeckHandGroupingRules.GetCardsForGroup(knifePlayer, true);
        List<BattleCardState> shootingNormal =
            BattleDeckHandGroupingRules.GetCardsForGroup(shootingPlayer, false);
        List<BattleCardState> shootingSpecial =
            BattleDeckHandGroupingRules.GetCardsForGroup(shootingPlayer, true);

        bool knifeNormalPassed = MatchesGroup(knifeNormal, knife.normalCardIDs);
        bool knifeSpecialPassed = MatchesGroup(knifeSpecial, knife.specialCardIDs);
        bool shootingNormalPassed = MatchesGroup(shootingNormal, shooting.normalCardIDs) &&
            !ContainsCardID(shootingNormal, "ability_modification_001") &&
            !ContainsCardID(shootingNormal, "sin_conservation_001");
        CardTestData modification = FindCard(cards, "ability_modification_001");
        bool shootingSpecialPassed = MatchesGroup(shootingSpecial, shooting.specialCardIDs);
        bool modificationPassed = modification != null && !modification.isSinCard &&
            ContainsCardID(shootingSpecial, "ability_modification_001") &&
            !ContainsCardID(shootingNormal, "ability_modification_001");
        bool identityPassed = HasGroupIdentity(knifePlayer, knifeNormal, knifeSpecial) &&
            HasGroupIdentity(shootingPlayer, shootingNormal, shootingSpecial);
        bool orderPassed = MatchesGroup(knifeNormal, knife.normalCardIDs) &&
            MatchesGroup(knifeSpecial, knife.specialCardIDs) &&
            MatchesGroup(shootingNormal, shooting.normalCardIDs) &&
            MatchesGroup(shootingSpecial, shooting.specialCardIDs);
        bool legacyPassed = VerifyLegacyFallback(cards);
        bool mutationPassed = VerifyRuntimeDeckUnchanged(knifePlayer, knife, knifeNormal, knifeSpecial) &&
            VerifyRuntimeDeckUnchanged(shootingPlayer, shooting, shootingNormal, shootingSpecial);
        bool stablePassed = SameReferences(
            BattleDeckHandGroupingRules.GetCardsForGroup(shootingPlayer, false),
            BattleDeckHandGroupingRules.GetCardsForGroup(shootingPlayer, true),
            BattleDeckHandGroupingRules.GetCardsForGroup(shootingPlayer, false)
        );
        bool mode114Passed = BattleDeckBootstrapPresetTests.Run(cards);

        bool passed = knifeNormalPassed && knifeSpecialPassed && shootingNormalPassed &&
            shootingSpecialPassed && modificationPassed && identityPassed && orderPassed &&
            legacyPassed && mutationPassed && stablePassed && mode114Passed;
        Debug.Log("===== Mode115 BattleDeckHandGroupingBasic =====");
        Debug.Log("Knife Normal6：" + knifeNormalPassed);
        Debug.Log("Knife Special2：" + knifeSpecialPassed);
        Debug.Log("Shooting Normal6：" + shootingNormalPassed);
        Debug.Log("Shooting Special2：" + shootingSpecialPassed);
        Debug.Log("Modification归Special：" + modificationPassed);
        Debug.Log("Reference Identity：" + identityPassed);
        Debug.Log("Manifest顺序保持：" + orderPassed);
        Debug.Log("Legacy fallback：" + legacyPassed);
        Debug.Log("Runtime Deck未Mutation：" + mutationPassed);
        Debug.Log("Normal→Special→Normal稳定：" + stablePassed);
        Debug.Log("Mode114回归：" + mode114Passed);
        Debug.Log("Passed: " + passed);
        return passed;
    }

    static BattleDefinitionBootstrapResult CreateResult(BattleDeckPreset preset)
    {
        return BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001", preset);
    }

    static bool MatchesGroup(
        List<BattleCardState> actual,
        IReadOnlyList<string> expected
    )
    {
        if (actual == null || expected == null || actual.Count != expected.Count)
        {
            return false;
        }
        for (int index = 0; index < expected.Count; index++)
        {
            if (actual[index] == null || actual[index].cardData == null ||
                actual[index].cardData.cardID != expected[index])
            {
                return false;
            }
        }
        return true;
    }

    static bool HasGroupIdentity(
        CharacterData owner,
        List<BattleCardState> normal,
        List<BattleCardState> special
    )
    {
        if (owner == null || owner.battleCards == null || normal == null || special == null ||
            owner.battleCards.Count != 8 || normal.Count != 6 || special.Count != 2)
        {
            return false;
        }
        for (int index = 0; index < 6; index++)
        {
            if (!object.ReferenceEquals(normal[index], owner.battleCards[index]) ||
                normal[index].owner != owner)
            {
                return false;
            }
        }
        return object.ReferenceEquals(special[0], owner.battleCards[6]) &&
            object.ReferenceEquals(special[1], owner.battleCards[7]) &&
            special[0].owner == owner && special[1].owner == owner &&
            !string.IsNullOrEmpty(special[0].instanceID) &&
            !string.IsNullOrEmpty(special[1].instanceID);
    }

    static bool VerifyLegacyFallback(List<CardTestData> cards)
    {
        CharacterData owner = new CharacterData("mode115_legacy", 30, 5, 5, "mode115_legacy");
        CardTestData normalTemplate = FindCard(cards, "atk_001");
        CardTestData sinTemplate = FindCard(cards, "sin_anger_001");
        CardTestData modificationTemplate = FindCard(cards, "ability_modification_001");
        if (normalTemplate == null || sinTemplate == null || modificationTemplate == null)
        {
            return false;
        }
        BattleCardManager.CreateBattleCard(owner, normalTemplate, "mode115_legacy_normal");
        BattleCardManager.CreateBattleCard(owner, sinTemplate, "mode115_legacy_sin");
        BattleCardManager.CreateBattleCard(owner, modificationTemplate, "mode115_legacy_modification");
        List<BattleCardState> normal = BattleDeckHandGroupingRules.GetCardsForGroup(owner, false);
        List<BattleCardState> special = BattleDeckHandGroupingRules.GetCardsForGroup(owner, true);
        return normal.Count == 2 && special.Count == 1 &&
            ContainsCardID(normal, "atk_001") &&
            ContainsCardID(normal, "ability_modification_001") &&
            ContainsCardID(special, "sin_anger_001");
    }

    static bool VerifyRuntimeDeckUnchanged(
        CharacterData owner,
        BattleDeckManifest manifest,
        List<BattleCardState> normal,
        List<BattleCardState> special
    )
    {
        if (owner == null || owner.battleCards == null || owner.battleCards.Count != 8)
        {
            return false;
        }
        BattleCardState[] snapshot = owner.battleCards.ToArray();
        List<BattleCardState> normalAgain =
            BattleDeckHandGroupingRules.GetCardsForGroup(owner, false);
        List<BattleCardState> specialAgain =
            BattleDeckHandGroupingRules.GetCardsForGroup(owner, true);
        if (!MatchesGroup(normalAgain, manifest.normalCardIDs) ||
            !MatchesGroup(specialAgain, manifest.specialCardIDs))
        {
            return false;
        }
        for (int index = 0; index < snapshot.Length; index++)
        {
            if (!object.ReferenceEquals(snapshot[index], owner.battleCards[index]))
            {
                return false;
            }
        }
        return true;
    }

    static bool SameReferences(
        List<BattleCardState> firstNormal,
        List<BattleCardState> special,
        List<BattleCardState> secondNormal
    )
    {
        if (firstNormal == null || special == null || secondNormal == null ||
            firstNormal.Count != secondNormal.Count)
        {
            return false;
        }
        for (int index = 0; index < firstNormal.Count; index++)
        {
            if (!object.ReferenceEquals(firstNormal[index], secondNormal[index])) return false;
        }
        return special.Count == 2;
    }

    static bool ContainsCardID(List<BattleCardState> cards, string cardID)
    {
        if (cards == null) return false;
        foreach (BattleCardState card in cards)
        {
            if (card != null && card.cardData != null && card.cardData.cardID == cardID)
            {
                return true;
            }
        }
        return false;
    }

    static CardTestData FindCard(List<CardTestData> cards, string cardID)
    {
        if (cards == null) return null;
        foreach (CardTestData card in cards)
        {
            if (card != null && card.cardID == cardID) return card;
        }
        return null;
    }
}

