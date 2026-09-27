using System;
using System.Collections.Generic;

[Serializable]
public sealed class ClashDieData
{
    public int min;
    public int max;
}

[Serializable]
public sealed class DamageDieData
{
    public int min;
    public int max;
    // Presentation only: never changes card type, resource rules or clash routing.
    public string presentation;
}

public static class CardCombatDiceRules
{
    public const string PrototypeCardID = "prototype_sword_gun_001";
    public const string Melee = "Melee";
    public const string CloseRangeShoot = "CloseRangeShoot";

    public static bool HasDice(CardTestData card)
    {
        return card != null && card.clashDie != null &&
            card.damageDice != null && card.damageDice.Length > 0;
    }

    public static bool Validate(CardTestData card, out string error)
    {
        error = string.Empty;
        if (card == null || (card.clashDie == null && card.damageDice == null))
            return true;
        if (!HasDice(card) || card.cardType != CardType.Attack ||
            !card.isClashable || !card.IsMeleeAttack() ||
            card.GetPresentationVariant() != BattleCardPresentationVariant.Default ||
            card.GetUsePolicy() != CardUsePolicy.Normal ||
            card.damageFormula != "PointAsDamage" ||
            card.resourceRule != null || (card.resourceRules != null && card.resourceRules.Length > 0) ||
            (card.traits != null && card.traits.Length > 0) ||
            (card.damageImpactPercents != null && card.damageImpactPercents.Length > 0) ||
            (card.damageImpactDelaySeconds != null && card.damageImpactDelaySeconds.Length > 0) ||
            !string.IsNullOrEmpty(card.damageDistributionMode))
        {
            error = "骰子原型需要完整双骰字段、普通近战 Attack / PointAsDamage，不能混用资源、Trait 或旧多段配置。";
            return false;
        }
        if (!ValidRange(card.clashDie.min, card.clashDie.max) ||
            card.damageDice.Length != 2 ||
            card.damageDice[0] == null || card.damageDice[1] == null ||
            card.damageDice[0].presentation != Melee ||
            card.damageDice[1].presentation != CloseRangeShoot)
        {
            error = "v0.1 需要一个有效拼点骰及近战→抵近射击两颗伤害骰。";
            return false;
        }
        foreach (DamageDieData die in card.damageDice)
        {
            if (!ValidRange(die.min, die.max))
            {
                error = "伤害骰范围必须为 1 <= min <= max <= 100。";
                return false;
            }
        }
        return true;
    }

    static bool ValidRange(int min, int max) => min >= 1 && max >= min && max <= 100;

    public static string Describe(CardTestData card)
    {
        if (!HasDice(card)) return string.Empty;
        var ranges = new List<string>();
        foreach (DamageDieData die in card.damageDice)
            ranges.Add(die.min + "~" + die.max);
        return "<color=#69CFFF>拼点骰</color>\n" + card.clashDie.min + "~" + card.clashDie.max +
            "\n<color=#FFBA69>伤害骰</color>\n" + string.Join(" → ", ranges) + "\n近战 → 射击";
    }
}
