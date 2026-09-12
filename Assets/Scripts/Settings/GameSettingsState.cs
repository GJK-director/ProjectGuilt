using System.Collections.Generic;
using UnityEngine;

public enum GameResolutionPreset
{
    OneK = 0,
    TwoK = 1
}

// Persistent menu settings. UI selection visuals are projections of this state,
// never the source of truth for gameplay or display configuration.
public static class GameSettingsState
{
    internal const string SelectedDeckPreferenceKey =
        "ProjectGuilt.SelectedDeck";
    internal const string FullscreenPreferenceKey =
        "ProjectGuilt.Fullscreen";
    internal const string ResolutionPreferenceKey =
        "ProjectGuilt.ResolutionPreset";

    static bool hasPendingBattleDeck;
    static BattleDeckPreset pendingBattleDeck;

    public static bool HasSelectedDeckPreference
    {
        get { return PlayerPrefs.HasKey(SelectedDeckPreferenceKey); }
    }

    public static BattleDeckPreset SelectedDeck
    {
        get
        {
            return PlayerPrefs.GetInt(
                SelectedDeckPreferenceKey,
                (int)BattleDeckPreset.Knife
            ) == (int)BattleDeckPreset.Shooting
                ? BattleDeckPreset.Shooting
                : BattleDeckPreset.Knife;
        }
    }

    public static bool IsFullscreen
    {
        get { return PlayerPrefs.GetInt(FullscreenPreferenceKey, 0) != 0; }
    }

    public static GameResolutionPreset ResolutionPreset
    {
        get
        {
            return PlayerPrefs.GetInt(
                ResolutionPreferenceKey,
                (int)GameResolutionPreset.OneK
            ) == (int)GameResolutionPreset.TwoK
                ? GameResolutionPreset.TwoK
                : GameResolutionPreset.OneK;
        }
    }

    public static void SetSelectedDeck(BattleDeckPreset preset)
    {
        PlayerPrefs.SetInt(SelectedDeckPreferenceKey, (int)preset);
        PlayerPrefs.Save();
    }

    public static bool HasPendingBattleDeck
    {
        get { return hasPendingBattleDeck; }
    }

    public static void SetPendingBattleDeck(BattleDeckPreset preset)
    {
        pendingBattleDeck = preset;
        hasPendingBattleDeck = true;
    }

    public static bool TryConsumePendingBattleDeck(out BattleDeckPreset preset)
    {
        if (!hasPendingBattleDeck)
        {
            preset = BattleDeckPreset.Knife;
            return false;
        }

        preset = pendingBattleDeck;
        ClearPendingBattleDeck();
        return true;
    }

    internal static void ClearPendingBattleDeck()
    {
        hasPendingBattleDeck = false;
        pendingBattleDeck = BattleDeckPreset.Knife;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSessionState()
    {
        ClearPendingBattleDeck();
    }

    public static void SetFullscreen(bool isFullscreen)
    {
        PlayerPrefs.SetInt(FullscreenPreferenceKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SetResolutionPreset(GameResolutionPreset preset)
    {
        PlayerPrefs.SetInt(ResolutionPreferenceKey, (int)preset);
        PlayerPrefs.Save();
    }

    public static GameResolutionPreset ResolutionPresetFromDropdownIndex(int index)
    {
        return index == (int)GameResolutionPreset.TwoK
            ? GameResolutionPreset.TwoK
            : GameResolutionPreset.OneK;
    }

    public static int GetResolutionDropdownIndex()
    {
        return (int)ResolutionPreset;
    }

    public static void GetResolution(
        GameResolutionPreset preset,
        out int width,
        out int height
    )
    {
        if (preset == GameResolutionPreset.TwoK)
        {
            width = 2560;
            height = 1440;
            return;
        }

        width = 1920;
        height = 1080;
    }

    public static FullScreenMode GetFullscreenMode()
    {
        return IsFullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
    }

    public static void ApplyDisplaySettings()
    {
        GetResolution(ResolutionPreset, out int width, out int height);
        Screen.SetResolution(width, height, GetFullscreenMode());
    }
}
