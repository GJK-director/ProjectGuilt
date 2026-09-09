using System.Collections.Generic;

public static class DeckPresetBootstrapTests
{
    public static bool ExplicitPlayerFactoryUsesRequestedCardIDsWithoutMutatingDefinition(
        List<CardTestData> cards
    )
    {
        BattleDeckManifest knife = BattleDeckManifests.Get(BattleDeckPreset.Knife);
        return VerifyExplicitFactory(cards, knife);
    }

    public static bool KnifePresetBuildsExpectedRuntime(List<CardTestData> cards)
    {
        BattleDeckManifest knife = BattleDeckManifests.Get(BattleDeckPreset.Knife);
        return VerifyPresetEncounter(cards, BattleDeckPreset.Knife, knife);
    }

    public static bool ShootingPresetBuildsExpectedRuntime(
        List<CardTestData> cards
    )
    {
        BattleDeckManifest shooting =
            BattleDeckManifests.Get(BattleDeckPreset.Shooting);
        return VerifyPresetEncounter(
            cards,
            BattleDeckPreset.Shooting,
            shooting
        );
    }

    public static bool KnifeAndShootingPresetsDoNotCross()
    {
        BattleDeckManifest knife = BattleDeckManifests.Get(BattleDeckPreset.Knife);
        BattleDeckManifest shooting =
            BattleDeckManifests.Get(BattleDeckPreset.Shooting);
        return VerifyNoCross(knife, shooting);
    }

    public static bool KnifePresetInitialResourcesMatchContract()
    {
        return VerifyPresetResources(BattleDeckPreset.Knife);
    }

    public static bool ShootingPresetInitialResourcesMatchContract()
    {
        return VerifyPresetResources(BattleDeckPreset.Shooting);
    }

    public static bool ShootingPresetDoesNotDependOnJsonBullet(
        List<CardTestData> cards
    )
    {
        BattleDeckManifest shooting =
            BattleDeckManifests.Get(BattleDeckPreset.Shooting);
        return VerifyShootingWithoutJsonBullet(cards, shooting);
    }

    public static bool PresetApplicationDoesNotMutateCharacterDefinition(
        List<CardTestData> cards
    )
    {
        BattleDeckManifest shooting =
            BattleDeckManifests.Get(BattleDeckPreset.Shooting);
        return VerifyDefinitionUnchanged(cards, shooting);
    }

    public static bool PresetCardInstanceIDsAreUnique()
    {
        return VerifyPresetCardInstanceIDs();
    }

    public static bool PresetCardOrderMatchesManifest()
    {
        BattleDeckManifest knife = BattleDeckManifests.Get(BattleDeckPreset.Knife);
        BattleDeckManifest shooting =
            BattleDeckManifests.Get(BattleDeckPreset.Shooting);
        return VerifyActualCardOrder(knife, shooting);
    }

    public static bool LegacyBootstrapWithoutExplicitPresetStillSucceeds()
    {
        return VerifyLegacyBootstrap();
    }

    private static bool VerifyExplicitFactory(
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

    private static bool VerifyPresetEncounter(
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
            HasDefinitionNeutralBuffs(
                result.allyBDefinition,
                result.runtimeState.allyB
            );
        return resources && HasUniqueInstanceIDs(player) &&
            HasDefinitionNeutralBuffs(result.allyADefinition, player) &&
            secondPlayer;
    }

    private static bool VerifyNoCross(
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

    private static bool VerifyPresetResources(BattleDeckPreset preset)
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

    private static bool VerifyShootingWithoutJsonBullet(
        List<CardTestData> cards,
        BattleDeckManifest manifest
    )
    {
        List<CharacterDefinitionData> definitions = CharacterDefinitionLoader.LoadDefinitions();
        CharacterDefinitionData source =
            CharacterDefinitionLoader.FindByID(definitions, "ally_001");
        if (source == null)
        {
            return false;
        }

        CharacterDefinitionData clone = CloneDefinition(source, true);
        List<string> explicitIDs = new List<string>();
        explicitIDs.AddRange(manifest.normalCardIDs);
        explicitIDs.AddRange(manifest.specialCardIDs);
        BattleUnitFactoryResult result = BattleUnitFactory.CreatePlayer(
            clone,
            cards,
            explicitIDs
        );
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

    private static bool VerifyDefinitionUnchanged(
        List<CardTestData> cards,
        BattleDeckManifest manifest
    )
    {
        List<CharacterDefinitionData> definitions = CharacterDefinitionLoader.LoadDefinitions();
        CharacterDefinitionData source =
            CharacterDefinitionLoader.FindByID(definitions, "ally_001");
        if (source == null)
        {
            return false;
        }

        CharacterDefinitionData snapshot = CloneDefinition(source, false);
        List<string> explicitIDs = new List<string>();
        explicitIDs.AddRange(manifest.normalCardIDs);
        explicitIDs.AddRange(manifest.specialCardIDs);
        BattleUnitFactoryResult result = BattleUnitFactory.CreatePlayer(
            source,
            cards,
            explicitIDs
        );
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

    private static bool VerifyPresetCardInstanceIDs()
    {
        return VerifyPresetCardInstanceIDs(BattleDeckPreset.Knife) &&
            VerifyPresetCardInstanceIDs(BattleDeckPreset.Shooting);
    }

    private static bool VerifyPresetCardInstanceIDs(BattleDeckPreset preset)
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

    private static bool VerifyActualCardOrder(
        BattleDeckManifest knife,
        BattleDeckManifest shooting
    )
    {
        return VerifyActualCardOrder(BattleDeckPreset.Knife, knife) &&
            VerifyActualCardOrder(BattleDeckPreset.Shooting, shooting);
    }

    private static bool VerifyActualCardOrder(
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

    private static bool VerifyLegacyBootstrap()
    {
        BattleDefinitionBootstrapResult result =
            BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        return result != null && result.isSuccess && result.runtimeState != null;
    }

    private static bool ContainsOnlyManifestIDs(
        CharacterData player,
        BattleDeckManifest manifest
    )
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

    private static bool HasUniqueInstanceIDs(CharacterData player)
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
                if (player.battleCards[left].instanceID ==
                    player.battleCards[right].instanceID)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static CharacterDefinitionData CloneDefinition(
        CharacterDefinitionData source,
        bool removeBullet
    )
    {
        List<InitialBuffDefinitionData> buffs =
            new List<InitialBuffDefinitionData>();
        if (source.initialBuffs != null)
        {
            foreach (InitialBuffDefinitionData buff in source.initialBuffs)
            {
                if (buff != null &&
                    (!removeBullet || buff.buffID != BattleResourceID.Bullet))
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

    private static bool SameDefinitionContent(
        CharacterDefinitionData left,
        CharacterDefinitionData right
    )
    {
        if (left == null || right == null || left.characterID != right.characterID ||
            left.characterName != right.characterName || left.maxHP != right.maxHP ||
            left.minSpeed != right.minSpeed || left.maxSpeed != right.maxSpeed ||
            left.actionSlotCount != right.actionSlotCount ||
            left.prefabKey != right.prefabKey || left.portraitKey != right.portraitKey)
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
            if (a.buffID != b.buffID || a.stack != b.stack ||
                a.duration != b.duration)
            {
                return false;
            }
        }
        return true;
    }

    private static bool SameStringArray(string[] left, string[] right)
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

    private static bool HasDefinitionNeutralBuffs(
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

    private static bool IsDeckResource(string buffID)
    {
        return buffID == BattleResourceID.Bullet ||
            buffID == BattleResourceID.Anger ||
            buffID == BattleResourceID.Modification ||
            buffID == BattleResourceID.Conservation;
    }
}
