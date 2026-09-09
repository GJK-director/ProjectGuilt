using System.Collections.Generic;
using UnityEngine;

public static class BattleDeckManifestTests
{
    public static bool Run(IReadOnlyList<CardTestData> cards)
    {
        bool knifeValues = CardDeckManifestTests
            .KnifeCardDefinitionsMatchExpectedValues(cards);
        bool manifests = CardDeckManifestTests
            .DeckManifestsUseExpectedMembershipAndDoNotShareCards();
        bool missingTemplatesAreSafe = CardDeckManifestTests
            .ManifestResolutionFindsRequiredAvailableCards(cards);
        bool firstStrike = CardDeckManifestTests
            .ShootingFirstStrikeCardsHaveExpectedTraits(cards) &&
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

}
