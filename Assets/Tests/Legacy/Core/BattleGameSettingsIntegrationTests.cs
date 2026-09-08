using System.Collections.Generic;
using UnityEngine;

public static class BattleGameSettingsIntegrationTests
{
    struct PreferenceSnapshot
    {
        public bool exists;
        public int value;
    }

    public static bool Run()
    {
        PreferenceSnapshot deck = Capture(GameSettingsState.SelectedDeckPreferenceKey);
        PreferenceSnapshot fullscreen = Capture(GameSettingsState.FullscreenPreferenceKey);
        PreferenceSnapshot resolution = Capture(GameSettingsState.ResolutionPreferenceKey);

        try
        {
            PlayerPrefs.DeleteKey(GameSettingsState.SelectedDeckPreferenceKey);
            PlayerPrefs.DeleteKey(GameSettingsState.FullscreenPreferenceKey);
            PlayerPrefs.DeleteKey(GameSettingsState.ResolutionPreferenceKey);
            PlayerPrefs.Save();

            bool defaultKnife = !GameSettingsState.HasSelectedDeckPreference &&
                GameSettingsState.SelectedDeck == BattleDeckPreset.Knife &&
                BattleSceneBootstrap.ResolvePlayerDeckPreset(
                    BattleDeckPreset.Knife
                ) == BattleDeckPreset.Knife;

            GameSettingsState.SetSelectedDeck(BattleDeckPreset.Shooting);
            BattleDeckPreset shootingPreset =
                BattleSceneBootstrap.ResolvePlayerDeckPreset(
                    BattleDeckPreset.Knife
                );
            bool shootingPersists = GameSettingsState.HasSelectedDeckPreference &&
                GameSettingsState.SelectedDeck == BattleDeckPreset.Shooting &&
                shootingPreset == BattleDeckPreset.Shooting;
            bool shootingBootstrap = VerifyBootstrapDeck(shootingPreset);

            GameSettingsState.SetSelectedDeck(BattleDeckPreset.Knife);
            bool knifePersists = GameSettingsState.SelectedDeck ==
                BattleDeckPreset.Knife &&
                BattleSceneBootstrap.ResolvePlayerDeckPreset(
                    BattleDeckPreset.Shooting
                ) == BattleDeckPreset.Knife;

            bool displayMapping = VerifyDisplayMapping();
            bool passed = defaultKnife && shootingPersists && shootingBootstrap &&
                knifePersists && displayMapping;

            Debug.Log("===== Mode133 BattleGameSettingsIntegration =====");
            Debug.Log("默认Knife且无偏好：" + defaultKnife);
            Debug.Log("Shooting偏好持久化：" + shootingPersists);
            Debug.Log("Shooting实际Bootstrap：" + shootingBootstrap);
            Debug.Log("切回Knife：" + knifePersists);
            Debug.Log("显示设置映射：" + displayMapping);
            Debug.Log("Passed: " + passed);
            return passed;
        }
        finally
        {
            Restore(GameSettingsState.SelectedDeckPreferenceKey, deck);
            Restore(GameSettingsState.FullscreenPreferenceKey, fullscreen);
            Restore(GameSettingsState.ResolutionPreferenceKey, resolution);
            PlayerPrefs.Save();
        }
    }

    static bool VerifyBootstrapDeck(BattleDeckPreset preset)
    {
        BattleDefinitionBootstrapResult result =
            BattleDefinitionBootstrap.CreateRuntimeState(
                "encounter_test_001",
                preset
            );
        BattleDeckManifest manifest = BattleDeckManifests.Get(preset);
        CharacterData player = result != null && result.runtimeState != null
            ? result.runtimeState.allyA
            : null;
        if (player == null || player.battleCards == null ||
            player.battleCards.Count != 8 || manifest == null)
        {
            return false;
        }

        List<string> expectedIDs = new List<string>();
        expectedIDs.AddRange(manifest.normalCardIDs);
        expectedIDs.AddRange(manifest.specialCardIDs);
        for (int index = 0; index < expectedIDs.Count; index++)
        {
            BattleCardState state = player.battleCards[index];
            if (state == null || state.cardData == null ||
                state.cardData.cardID != expectedIDs[index])
            {
                return false;
            }
        }

        return true;
    }

    static bool VerifyDisplayMapping()
    {
        GameSettingsState.GetResolution(
            GameResolutionPreset.OneK,
            out int oneKWidth,
            out int oneKHeight
        );
        GameSettingsState.GetResolution(
            GameResolutionPreset.TwoK,
            out int twoKWidth,
            out int twoKHeight
        );
        GameSettingsState.SetFullscreen(false);
        bool windowed = GameSettingsState.GetFullscreenMode() ==
            FullScreenMode.Windowed;
        GameSettingsState.SetFullscreen(true);
        bool fullscreen = GameSettingsState.GetFullscreenMode() ==
            FullScreenMode.FullScreenWindow;
        return oneKWidth == 1920 && oneKHeight == 1080 &&
            twoKWidth == 2560 && twoKHeight == 1440 &&
            windowed && fullscreen;
    }

    static PreferenceSnapshot Capture(string key)
    {
        return new PreferenceSnapshot
        {
            exists = PlayerPrefs.HasKey(key),
            value = PlayerPrefs.GetInt(key)
        };
    }

    static void Restore(string key, PreferenceSnapshot snapshot)
    {
        if (snapshot.exists)
        {
            PlayerPrefs.SetInt(key, snapshot.value);
        }
        else
        {
            PlayerPrefs.DeleteKey(key);
        }
    }
}

