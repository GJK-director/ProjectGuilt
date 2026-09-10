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

public static class BattleDeckHandGroupingRules
{
    public static List<BattleCardState> GetCardsForGroup(
        CharacterData owner,
        bool showSpecialCards
    )
    {
        if (owner == null || owner.battleCards == null)
        {
            return new List<BattleCardState>();
        }

        BattleDeckManifest manifest = FindMatchingManifest(owner);
        if (manifest == null)
        {
            List<BattleCardState> legacyCards = new List<BattleCardState>();
            foreach (BattleCardState cardState in owner.battleCards)
            {
                if (cardState != null && cardState.cardData != null &&
                    cardState.cardData.isSinCard == showSpecialCards)
                {
                    legacyCards.Add(cardState);
                }
            }
            return legacyCards;
        }

        IReadOnlyList<string> ids = showSpecialCards
            ? manifest.specialCardIDs
            : manifest.normalCardIDs;
        List<BattleCardState> groupedCards = new List<BattleCardState>();
        foreach (string cardID in ids)
        {
            for (int index = 0; index < owner.battleCards.Count; index++)
            {
                BattleCardState cardState = owner.battleCards[index];
                if (cardState != null && cardState.cardData != null &&
                    cardState.cardData.cardID == cardID)
                {
                    groupedCards.Add(cardState);
                    break;
                }
            }
        }
        return groupedCards;
    }

    static BattleDeckManifest FindMatchingManifest(CharacterData owner)
    {
        BattleDeckManifest knife = BattleDeckManifests.Get(BattleDeckPreset.Knife);
        if (MatchesManifest(owner, knife)) return knife;
        BattleDeckManifest shooting = BattleDeckManifests.Get(BattleDeckPreset.Shooting);
        if (MatchesManifest(owner, shooting)) return shooting;
        return null;
    }

    static bool MatchesManifest(CharacterData owner, BattleDeckManifest manifest)
    {
        if (owner == null || owner.battleCards == null || manifest == null ||
            owner.battleCards.Count != 8)
        {
            return false;
        }
        for (int index = 0; index < manifest.normalCardIDs.Count; index++)
        {
            if (!MatchesCardID(owner.battleCards[index], manifest.normalCardIDs[index]))
            {
                return false;
            }
        }
        for (int index = 0; index < manifest.specialCardIDs.Count; index++)
        {
            int cardIndex = manifest.normalCardIDs.Count + index;
            if (!MatchesCardID(owner.battleCards[cardIndex], manifest.specialCardIDs[index]))
            {
                return false;
            }
        }
        return true;
    }

    static bool MatchesCardID(BattleCardState cardState, string cardID)
    {
        return cardState != null && cardState.cardData != null &&
            cardState.cardData.cardID == cardID;
    }
}
