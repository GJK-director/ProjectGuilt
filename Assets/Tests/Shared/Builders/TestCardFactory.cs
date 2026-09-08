using System.Collections.Generic;

public static class TestCardFactory
{
    public static CardTestData CreateFixedData(
        string id,
        string cardType,
        int point,
        string deliveryMode = AttackDeliveryMode.Melee,
        int cooldown = 2,
        bool firstStrike = false
    )
    {
        return new CardTestData
        {
            cardID = id,
            cardName = id,
            cardType = cardType,
            attackDeliveryMode = cardType == CardType.Attack
                ? deliveryMode
                : string.Empty,
            isClashable = cardType == CardType.Attack,
            minPoint = point,
            maxPoint = point,
            cooldown = cooldown,
            damageFormula = cardType == CardType.Attack
                ? "PointAsDamage"
                : string.Empty,
            defenseFormula = cardType == CardType.Defense
                ? "PointAsDefense"
                : string.Empty,
            traits = firstStrike
                ? new[] { BattleCardTrait.FirstStrike }
                : new BattleCardTrait[0],
            effects = new List<CardEffectData>()
        };
    }

    public static BattleCardState CreateState(
        CharacterData owner,
        string id,
        string cardType,
        int point,
        string deliveryMode = AttackDeliveryMode.Melee,
        int cooldown = 2,
        bool firstStrike = false
    )
    {
        CardTestData data = CreateFixedData(
            id,
            cardType,
            point,
            deliveryMode,
            cooldown,
            firstStrike
        );

        return BattleCardManager.CreateBattleCard(
            owner,
            data,
            id + "_instance"
        );
    }
}
