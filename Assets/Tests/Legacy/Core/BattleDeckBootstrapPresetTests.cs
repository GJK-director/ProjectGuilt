using System.Collections.Generic;
using UnityEngine;

public static class BattleDeckBootstrapPresetTests
{
    public static bool Run(List<CardTestData> cards)
    {
        BattleDeckManifest knife = BattleDeckManifests.Get(BattleDeckPreset.Knife);
        BattleDeckManifest shooting = BattleDeckManifests.Get(BattleDeckPreset.Shooting);
        bool manifest = VerifyManifest(cards, knife, shooting);
        bool order = VerifyManifestOrder(knife) && VerifyManifestOrder(shooting);
        bool explicitFactory = VerifyExplicitFactory(cards, knife);
        bool knifeBootstrap = VerifyPresetEncounter(cards, BattleDeckPreset.Knife, knife);
        bool shootingBootstrap = VerifyPresetEncounter(cards, BattleDeckPreset.Shooting, shooting);
        bool noCross = VerifyNoCross(cards, knife, shooting);
        bool knifeResources = VerifyPresetResources(BattleDeckPreset.Knife);
        bool shootingResources = VerifyPresetResources(BattleDeckPreset.Shooting);
        bool shootingWithoutJsonBullet = VerifyShootingWithoutJsonBullet(cards, shooting);
        bool definitionUnchanged = VerifyDefinitionUnchanged(cards, shooting);
        bool cardInstanceIDsUnique = VerifyPresetCardInstanceIDs();
        bool actualCardOrder = VerifyActualCardOrder(knife, shooting);
        bool oldBootstrap = VerifyLegacyBootstrap();
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

    static bool VerifyManifest(
        IReadOnlyList<CardTestData> cards,
        BattleDeckManifest knife,
        BattleDeckManifest shooting
    )
    {
        List<string> missingKnife = new List<string>();
        List<string> missingShooting = new List<string>();
        List<string> knifeIDs = knife.ResolveAvailableCardIDs(cards, missingKnife);
        List<string> shootingIDs = shooting.ResolveAvailableCardIDs(cards, missingShooting);
        return knife.normalCardIDs.Count == 6 && knife.specialCardIDs.Count == 2 &&
            knifeIDs.Count == 8 && missingKnife.Count == 0 &&
            shooting.normalCardIDs.Count == 6 && shooting.specialCardIDs.Count == 2 &&
            shootingIDs.Count == 8 && missingShooting.Count == 0;
    }

    static bool VerifyManifestOrder(BattleDeckManifest manifest)
    {
        List<string> all = new List<string>();
        all.AddRange(manifest.normalCardIDs);
        all.AddRange(manifest.specialCardIDs);
        for (int index = 0; index < all.Count; index++)
        {
            if (index < manifest.normalCardIDs.Count &&
                all[index] != manifest.normalCardIDs[index])
            {
                return false;
            }
            if (index >= manifest.normalCardIDs.Count &&
                all[index] != manifest.specialCardIDs[index - manifest.normalCardIDs.Count])
            {
                return false;
            }
        }
        return all.Count == 8;
    }

    static bool VerifyExplicitFactory(
        List<CardTestData> cards,
        BattleDeckManifest manifest
    )
    {
        string[] legacyIDs = { "atk_bullet_001", "shoot_close_001" };
        CharacterDefinitionData definition = new CharacterDefinitionData
        {
            characterID = "mode114_explicit_definition",
            characterName = "Mode114 Explicit",
            maxHP = 30,
            minSpeed = 4,
            maxSpeed = 8,
            actionSlotCount = 2,
            startingCardIDs = (string[])legacyIDs.Clone(),
            initialBuffs = new InitialBuffDefinitionData[0]
        };
        List<string> explicitIDs = new List<string>();
        explicitIDs.AddRange(manifest.normalCardIDs);
        explicitIDs.AddRange(manifest.specialCardIDs);
        BattleUnitFactoryResult result = BattleUnitFactory.CreatePlayer(
            definition,
            cards,
            explicitIDs
        );
        if (result == null || !result.isSuccess || result.unit == null ||
            result.unit.battleCards.Count != explicitIDs.Count)
        {
            return false;
        }
        for (int index = 0; index < explicitIDs.Count; index++)
        {
            if (result.unit.battleCards[index].cardData.cardID != explicitIDs[index])
            {
                return false;
            }
        }
        return definition.startingCardIDs.Length == legacyIDs.Length &&
            definition.startingCardIDs[0] == legacyIDs[0] &&
            definition.startingCardIDs[1] == legacyIDs[1];
    }

    static bool VerifyPresetEncounter(
        List<CardTestData> cards,
        BattleDeckPreset preset,
        BattleDeckManifest manifest
    )
    {
        BattleDefinitionBootstrapResult result =
            BattleDefinitionBootstrap.CreateRuntimeState(
                "encounter_test_001",
                preset
            );
        if (result == null || !result.isSuccess || result.runtimeState == null ||
            result.runtimeState.allyA == null)
        {
            return false;
        }

        CharacterData player = result.runtimeState.allyA;
        List<string> expectedIDs = new List<string>();
        expectedIDs.AddRange(manifest.normalCardIDs);
        expectedIDs.AddRange(manifest.specialCardIDs);
        if (player.battleCards == null || player.battleCards.Count != 8)
        {
            return false;
        }
        for (int index = 0; index < expectedIDs.Count; index++)
        {
            BattleCardState state = player.battleCards[index];
            if (state == null || state.owner != player ||
                state.cardData == null || state.cardData.cardID != expectedIDs[index])
            {
                return false;
            }
        }

        bool resources = player.GetBuffStack(BattleResourceID.Anger) == 0 &&
            player.GetBuffStack(BattleResourceID.Modification) == 0 &&
            player.GetBuffStack(BattleResourceID.Conservation) == 0 &&
            !player.IsAngerMechanicEnabled;
        if (preset == BattleDeckPreset.Knife)
        {
            resources = resources && BattleBulletRules.GetBullet(player) == 0;
        }
        else
        {
            resources = resources && BattleBulletRules.GetBullet(player) == 6 &&
                BattleBulletRules.GetMagazineCapacity(player) == 6;
        }

        bool secondPlayer = result.runtimeState.allyB == null ||
            ContainsOnlyManifestIDs(result.runtimeState.allyB, manifest) &&
            HasUniqueInstanceIDs(result.runtimeState.allyB) &&
            HasDefinitionNeutralBuffs(result.allyBDefinition, result.runtimeState.allyB);
        return resources && HasUniqueInstanceIDs(player) &&
            HasDefinitionNeutralBuffs(result.allyADefinition, player) &&
            secondPlayer;
    }

    static bool VerifyNoCross(
        List<CardTestData> cards,
        BattleDeckManifest knife,
        BattleDeckManifest shooting
    )
    {
        BattleDefinitionBootstrapResult knifeResult =
            BattleDefinitionBootstrap.CreateRuntimeState(
                "encounter_test_001",
                BattleDeckPreset.Knife
            );
        BattleDefinitionBootstrapResult shootingResult =
            BattleDefinitionBootstrap.CreateRuntimeState(
                "encounter_test_001",
                BattleDeckPreset.Shooting
            );
        return knifeResult != null && knifeResult.isSuccess &&
            shootingResult != null && shootingResult.isSuccess &&
            ContainsOnlyManifestIDs(knifeResult.runtimeState.allyA, knife) &&
            ContainsOnlyManifestIDs(shootingResult.runtimeState.allyA, shooting);
    }

    static bool VerifyPresetResources(BattleDeckPreset preset)
    {
        BattleDefinitionBootstrapResult result =
            BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001", preset);
        if (result == null || !result.isSuccess || result.runtimeState == null ||
            result.runtimeState.allyA == null)
        {
            return false;
        }

        CharacterData player = result.runtimeState.allyA;
        bool resources = player.GetBuffStack(BattleResourceID.Anger) == 0 &&
            player.GetBuffStack(BattleResourceID.Modification) == 0 &&
            player.GetBuffStack(BattleResourceID.Conservation) == 0 &&
            !player.IsAngerMechanicEnabled;
        if (preset == BattleDeckPreset.Knife)
        {
            resources = resources && BattleBulletRules.GetBullet(player) == 0;
        }
        else
        {
            resources = resources && BattleBulletRules.GetBullet(player) == 6 &&
                BattleBulletRules.GetMagazineCapacity(player) == 6;
        }

        return resources && HasDefinitionNeutralBuffs(result.allyADefinition, player);
    }

    static bool VerifyShootingWithoutJsonBullet(
        List<CardTestData> cards,
        BattleDeckManifest manifest
    )
    {
        List<CharacterDefinitionData> definitions = CharacterDefinitionLoader.LoadDefinitions();
        CharacterDefinitionData source = CharacterDefinitionLoader.FindByID(definitions, "ally_001");
        if (source == null)
        {
            return false;
        }

        CharacterDefinitionData clone = CloneDefinition(source, true);
        List<string> explicitIDs = new List<string>();
        explicitIDs.AddRange(manifest.normalCardIDs);
        explicitIDs.AddRange(manifest.specialCardIDs);
        BattleUnitFactoryResult result = BattleUnitFactory.CreatePlayer(clone, cards, explicitIDs);
        if (result == null || !result.isSuccess || result.unit == null ||
            result.unit.GetBuffStack(BattleResourceID.Bullet) != 0)
        {
            return false;
        }

        BattleDefinitionBootstrap.ApplyPlayerDeckPresetInitialState(
            result.unit,
            BattleDeckPreset.Shooting
        );
        return result.unit.GetBuffStack(BattleResourceID.Bullet) == 6 &&
            BattleBulletRules.GetBullet(result.unit) == 6 &&
            BattleBulletRules.GetMagazineCapacity(result.unit) == 6;
    }

    static bool VerifyDefinitionUnchanged(
        List<CardTestData> cards,
        BattleDeckManifest manifest
    )
    {
        List<CharacterDefinitionData> definitions = CharacterDefinitionLoader.LoadDefinitions();
        CharacterDefinitionData source = CharacterDefinitionLoader.FindByID(definitions, "ally_001");
        if (source == null)
        {
            return false;
        }

        CharacterDefinitionData snapshot = CloneDefinition(source, false);
        List<string> explicitIDs = new List<string>();
        explicitIDs.AddRange(manifest.normalCardIDs);
        explicitIDs.AddRange(manifest.specialCardIDs);
        BattleUnitFactoryResult result = BattleUnitFactory.CreatePlayer(source, cards, explicitIDs);
        if (result == null || !result.isSuccess || result.unit == null)
        {
            return false;
        }

        BattleDefinitionBootstrap.ApplyPlayerDeckPresetInitialState(
            result.unit,
            BattleDeckPreset.Shooting
        );
        return SameDefinitionContent(source, snapshot);
    }

    static bool VerifyPresetCardInstanceIDs()
    {
        return VerifyPresetCardInstanceIDs(BattleDeckPreset.Knife) &&
            VerifyPresetCardInstanceIDs(BattleDeckPreset.Shooting);
    }

    static bool VerifyPresetCardInstanceIDs(BattleDeckPreset preset)
    {
        BattleDefinitionBootstrapResult result =
            BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001", preset);
        if (result == null || !result.isSuccess || result.runtimeState == null ||
            result.runtimeState.allyA == null)
        {
            return false;
        }
        return HasUniqueInstanceIDs(result.runtimeState.allyA);
    }

    static bool VerifyActualCardOrder(
        BattleDeckManifest knife,
        BattleDeckManifest shooting
    )
    {
        return VerifyActualCardOrder(BattleDeckPreset.Knife, knife) &&
            VerifyActualCardOrder(BattleDeckPreset.Shooting, shooting);
    }

    static bool VerifyActualCardOrder(
        BattleDeckPreset preset,
        BattleDeckManifest manifest
    )
    {
        BattleDefinitionBootstrapResult result =
            BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001", preset);
        CharacterData player = result == null || result.runtimeState == null
            ? null
            : result.runtimeState.allyA;
        if (player == null || player.battleCards == null || player.battleCards.Count != 8)
        {
            return false;
        }
        for (int index = 0; index < manifest.normalCardIDs.Count; index++)
        {
            if (player.battleCards[index] == null ||
                player.battleCards[index].cardData == null ||
                player.battleCards[index].cardData.cardID != manifest.normalCardIDs[index])
            {
                return false;
            }
        }
        for (int index = 0; index < manifest.specialCardIDs.Count; index++)
        {
            int cardIndex = manifest.normalCardIDs.Count + index;
            if (player.battleCards[cardIndex] == null ||
                player.battleCards[cardIndex].cardData == null ||
                player.battleCards[cardIndex].cardData.cardID != manifest.specialCardIDs[index])
            {
                return false;
            }
        }
        return true;
    }

    static bool VerifyLegacyBootstrap()
    {
        BattleDefinitionBootstrapResult result =
            BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        return result != null && result.isSuccess && result.runtimeState != null;
    }

    static bool ContainsOnlyManifestIDs(CharacterData player, BattleDeckManifest manifest)
    {
        if (player == null || player.battleCards == null)
        {
            return false;
        }
        List<string> expected = new List<string>();
        expected.AddRange(manifest.normalCardIDs);
        expected.AddRange(manifest.specialCardIDs);
        if (player.battleCards.Count != expected.Count)
        {
            return false;
        }
        for (int index = 0; index < expected.Count; index++)
        {
            if (player.battleCards[index].cardData.cardID != expected[index])
            {
                return false;
            }
        }
        return true;
    }

    static bool HasUniqueInstanceIDs(CharacterData player)
    {
        if (player == null || player.battleCards == null)
        {
            return false;
        }
        for (int left = 0; left < player.battleCards.Count; left++)
        {
            if (player.battleCards[left] == null ||
                string.IsNullOrEmpty(player.battleCards[left].instanceID) ||
                player.battleCards[left].owner != player)
            {
                return false;
            }
            for (int right = left + 1; right < player.battleCards.Count; right++)
            {
                if (player.battleCards[right] == null ||
                    string.IsNullOrEmpty(player.battleCards[right].instanceID))
                {
                    return false;
                }
                if (player.battleCards[left].instanceID == player.battleCards[right].instanceID)
                {
                    return false;
                }
            }
        }
        return true;
    }

    static CharacterDefinitionData CloneDefinition(
        CharacterDefinitionData source,
        bool removeBullet
    )
    {
        List<InitialBuffDefinitionData> buffs = new List<InitialBuffDefinitionData>();
        if (source.initialBuffs != null)
        {
            foreach (InitialBuffDefinitionData buff in source.initialBuffs)
            {
                if (buff != null && (!removeBullet || buff.buffID != BattleResourceID.Bullet))
                {
                    buffs.Add(new InitialBuffDefinitionData
                    {
                        buffID = buff.buffID,
                        stack = buff.stack,
                        duration = buff.duration
                    });
                }
            }
        }
        return new CharacterDefinitionData
        {
            characterID = source.characterID,
            characterName = source.characterName,
            maxHP = source.maxHP,
            minSpeed = source.minSpeed,
            maxSpeed = source.maxSpeed,
            actionSlotCount = source.actionSlotCount,
            startingCardIDs = source.startingCardIDs == null
                ? null
                : (string[])source.startingCardIDs.Clone(),
            initialBuffs = buffs.ToArray(),
            prefabKey = source.prefabKey,
            portraitKey = source.portraitKey
        };
    }

    static bool SameDefinitionContent(
        CharacterDefinitionData left,
        CharacterDefinitionData right
    )
    {
        if (left == null || right == null || left.characterID != right.characterID ||
            left.characterName != right.characterName || left.maxHP != right.maxHP ||
            left.minSpeed != right.minSpeed || left.maxSpeed != right.maxSpeed ||
            left.actionSlotCount != right.actionSlotCount || left.prefabKey != right.prefabKey ||
            left.portraitKey != right.portraitKey)
        {
            return false;
        }
        if (!SameStringArray(left.startingCardIDs, right.startingCardIDs) ||
            left.initialBuffs == null != (right.initialBuffs == null))
        {
            return false;
        }
        if (left.initialBuffs == null)
        {
            return true;
        }
        if (left.initialBuffs.Length != right.initialBuffs.Length)
        {
            return false;
        }
        for (int index = 0; index < left.initialBuffs.Length; index++)
        {
            InitialBuffDefinitionData a = left.initialBuffs[index];
            InitialBuffDefinitionData b = right.initialBuffs[index];
            if (a == null || b == null)
            {
                if (a != b) return false;
                continue;
            }
            if (a.buffID != b.buffID || a.stack != b.stack || a.duration != b.duration)
            {
                return false;
            }
        }
        return true;
    }

    static bool SameStringArray(string[] left, string[] right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }
        if (left.Length != right.Length)
        {
            return false;
        }
        for (int index = 0; index < left.Length; index++)
        {
            if (left[index] != right[index]) return false;
        }
        return true;
    }

    static bool HasDefinitionNeutralBuffs(
        CharacterDefinitionData definition,
        CharacterData player
    )
    {
        if (definition == null || definition.initialBuffs == null)
        {
            return true;
        }
        foreach (InitialBuffDefinitionData initialBuff in definition.initialBuffs)
        {
            if (initialBuff == null || IsDeckResource(initialBuff.buffID))
            {
                continue;
            }
            if (player.GetBuffStack(initialBuff.buffID) < initialBuff.stack)
            {
                return false;
            }
        }
        return true;
    }

    static bool IsDeckResource(string buffID)
    {
        return buffID == BattleResourceID.Bullet ||
            buffID == BattleResourceID.Anger ||
            buffID == BattleResourceID.Modification ||
            buffID == BattleResourceID.Conservation;
    }
}

