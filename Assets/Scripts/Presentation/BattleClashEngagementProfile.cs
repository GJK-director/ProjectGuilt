using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BattleClashEngagementProfile",
    menuName = "Project Guilt/Battle/Clash Engagement Profile"
)]
public sealed class BattleClashEngagementProfile : ScriptableObject
{
    [Serializable]
    public sealed class CharacterSpacingOverride
    {
        [SerializeField] private string presentationKey;
        [SerializeField] private float spacingOffset;

        public string PresentationKey => presentationKey;
        public float SpacingOffset => spacingOffset;
    }

    [Serializable]
    public sealed class PairGapOverride
    {
        [SerializeField] private string sideAPresentationKey;
        [SerializeField] private string sideBPresentationKey;
        [SerializeField] private float finalGap = 3.5f;

        public string SideAPresentationKey => sideAPresentationKey;
        public string SideBPresentationKey => sideBPresentationKey;
        public float FinalGap => Mathf.Max(0f, finalGap);
    }

    [SerializeField] private float defaultClashReadyGap = 3.5f;
    [SerializeField] private float relativeSpeedInfluence = 0.35f;
    [SerializeField, Range(0f, 0.5f)]
    private float minMovementShare = 0.15f;
    [SerializeField, Range(0.5f, 1f)]
    private float maxMovementShare = 0.85f;
    [SerializeField]
    private List<CharacterSpacingOverride> characterSpacingOverrides =
        new List<CharacterSpacingOverride>();
    [SerializeField]
    private List<PairGapOverride> pairGapOverrides =
        new List<PairGapOverride>();

    public float DefaultClashReadyGap =>
        Mathf.Max(0f, defaultClashReadyGap);
    public float RelativeSpeedInfluence =>
        Mathf.Max(0f, relativeSpeedInfluence);
    public float MinMovementShare =>
        Mathf.Clamp(minMovementShare, 0f, 0.5f);
    public float MaxMovementShare =>
        Mathf.Clamp(maxMovementShare, 0.5f, 1f);

    public float GetCharacterSpacing(string presentationKey)
    {
        if (string.IsNullOrEmpty(presentationKey) ||
            characterSpacingOverrides == null)
        {
            return 0f;
        }

        for (int index = 0; index < characterSpacingOverrides.Count; index++)
        {
            CharacterSpacingOverride entry =
                characterSpacingOverrides[index];
            if (entry != null && string.Equals(
                    entry.PresentationKey,
                    presentationKey,
                    StringComparison.Ordinal
                ))
            {
                return entry.SpacingOffset;
            }
        }

        return 0f;
    }

    public bool TryGetPairGap(
        string sideAPresentationKey,
        string sideBPresentationKey,
        out float finalGap
    )
    {
        finalGap = 0f;
        if (string.IsNullOrEmpty(sideAPresentationKey) ||
            string.IsNullOrEmpty(sideBPresentationKey) ||
            pairGapOverrides == null)
        {
            return false;
        }

        for (int index = 0; index < pairGapOverrides.Count; index++)
        {
            PairGapOverride entry = pairGapOverrides[index];
            if (entry == null)
            {
                continue;
            }

            bool directMatch = KeysMatch(
                sideAPresentationKey,
                sideBPresentationKey,
                entry.SideAPresentationKey,
                entry.SideBPresentationKey
            );
            bool reverseMatch = KeysMatch(
                sideAPresentationKey,
                sideBPresentationKey,
                entry.SideBPresentationKey,
                entry.SideAPresentationKey
            );
            if (!directMatch && !reverseMatch)
            {
                continue;
            }

            finalGap = entry.FinalGap;
            return true;
        }

        return false;
    }

    private static bool KeysMatch(
        string actualA,
        string actualB,
        string expectedA,
        string expectedB
    )
    {
        return string.Equals(actualA, expectedA, StringComparison.Ordinal) &&
            string.Equals(actualB, expectedB, StringComparison.Ordinal);
    }
}
