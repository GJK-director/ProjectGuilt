using System.Collections.Generic;
using UnityEngine;

public static class BattleDeckBootstrapPresetTests
{
    public static bool Run(List<CardTestData> cards)
    {
        bool manifest = CardDeckManifestTests
            .DeckManifestsResolveAllCardsWithoutMissingEntries(cards);
        bool order = CardDeckManifestTests
            .DeckManifestOrdersNormalCardsBeforeSpecialCards();
        bool explicitFactory = DeckPresetBootstrapTests
            .ExplicitPlayerFactoryUsesRequestedCardIDsWithoutMutatingDefinition(cards);
        bool knifeBootstrap = DeckPresetBootstrapTests
            .KnifePresetBuildsExpectedRuntime(cards);
        bool shootingBootstrap = DeckPresetBootstrapTests
            .ShootingPresetBuildsExpectedRuntime(cards);
        bool noCross = DeckPresetBootstrapTests
            .KnifeAndShootingPresetsDoNotCross();
        bool knifeResources = DeckPresetBootstrapTests
            .KnifePresetInitialResourcesMatchContract();
        bool shootingResources = DeckPresetBootstrapTests
            .ShootingPresetInitialResourcesMatchContract();
        bool shootingWithoutJsonBullet = DeckPresetBootstrapTests
            .ShootingPresetDoesNotDependOnJsonBullet(cards);
        bool definitionUnchanged = DeckPresetBootstrapTests
            .PresetApplicationDoesNotMutateCharacterDefinition(cards);
        bool cardInstanceIDsUnique = DeckPresetBootstrapTests
            .PresetCardInstanceIDsAreUnique();
        bool actualCardOrder = DeckPresetBootstrapTests
            .PresetCardOrderMatchesManifest();
        bool oldBootstrap = DeckPresetBootstrapTests
            .LegacyBootstrapWithoutExplicitPresetStillSucceeds();
        bool deckManifestRegression = BattleDeckManifestTests.Run(cards);

        bool passed = manifest && order && explicitFactory && knifeBootstrap &&
            shootingBootstrap && noCross && knifeResources && shootingResources &&
            shootingWithoutJsonBullet && definitionUnchanged && oldBootstrap &&
            cardInstanceIDsUnique && actualCardOrder && deckManifestRegression;
        Debug.Log("===== Mode114 BattleDeckBootstrapPreset =====");
        Debug.Log("Manifest完整：" + manifest);
        Debug.Log("Manifest顺序：" + order);
        Debug.Log("UnitFactory explicit IDs：" + explicitFactory);
        Debug.Log("Knife完整Bootstrap：" + knifeBootstrap);
        Debug.Log("Shooting完整Bootstrap：" + shootingBootstrap);
        Debug.Log("两套无交叉：" + noCross);
        Debug.Log("Knife初始资源：" + knifeResources);
        Debug.Log("Shooting初始资源：" + shootingResources);
        Debug.Log("Shooting不依赖JSON Bullet：" + shootingWithoutJsonBullet);
        Debug.Log("Definition未Mutation：" + definitionUnchanged);
        Debug.Log("旧Bootstrap兼容：" + oldBootstrap);
        Debug.Log("CardInstanceID唯一：" + cardInstanceIDsUnique);
        Debug.Log("Normal6 + Special2顺序契约：" + actualCardOrder);
        Debug.Log("DeckManifest回归：" + deckManifestRegression);
        Debug.Log("Passed: " + passed);
        return passed;
    }
}
