using System.Text;
using UnityEngine;

public static class BattleCardUIPreviewBuilder
{
    const string AbilityCardType = "Ability";
    const string ResourceTypeBuffStack = "BuffStack";

    public static BattleCardUIPreviewData Build(
        CharacterData owner,
        CharacterData defaultTarget,
        BattleCardState cardState
    )
    {
        BattleCardUIPreviewData data = new BattleCardUIPreviewData();

        if (cardState == null)
        {
            data.cardName = "空";
            data.pointText = "—";
            data.typeText = "";
            data.descriptionText = "";
            data.cooldownText = "—";
            data.isUsable = false;
            data.unavailableReason = "卡牌状态为空";
            return data;
        }

        if (owner == null)
        {
            owner = cardState.owner;
        }

        CardTestData cardData = cardState.cardData;

        data.cardName = cardState.GetCardName();

        if (cardData == null)
        {
            data.pointText = "—";
            data.typeText = "";
            data.descriptionText = "卡牌数据为空";
            data.cooldownText = "—";
            data.isUsable = false;
            data.unavailableReason = "卡牌数据为空";
            return data;
        }

        data.typeText = BuildTypeText(cardData);
        data.cooldownText = BuildCooldownText(cardData);
        data.pointText = BuildPointText(owner, cardData);

        CardEligibilityResult eligibility = BattleCardManager.EvaluateCardEligibility(
            owner,
            defaultTarget,
            cardState
        );

        data.isUsable = eligibility != null && eligibility.isEligible;
        data.unavailableReason = eligibility != null ? eligibility.failureMessage : "卡牌可用性检查失败";
        data.descriptionText = cardData.description ?? "";

        return data;
    }

    static string BuildTypeText(CardTestData cardData)
    {
        string typeText = GetCardTypeDisplayName(cardData.cardType);

        if (cardData.isSinCard)
        {
            typeText = "罪卡 / " + typeText;
        }

        return typeText;
    }

    static string GetCardTypeDisplayName(string cardType)
    {
        if (cardType == CardType.Attack)
        {
            return "攻击";
        }

        if (cardType == CardType.Defense)
        {
            return "防御";
        }

        if (cardType == CardType.Dodge)
        {
            return "闪避";
        }

        if (cardType == AbilityCardType)
        {
            return "能力";
        }

        return string.IsNullOrEmpty(cardType) ? "未知" : cardType;
    }

    static string BuildCooldownText(CardTestData cardData)
    {
        return cardData.cooldown.ToString();
    }

    static string BuildPointText(CharacterData owner, CardTestData cardData)
    {
        if (cardData.cardType == AbilityCardType &&
            cardData.minPoint == 0 &&
            cardData.maxPoint == 0)
        {
            return "—";
        }

        int minPoint = cardData.minPoint;
        int maxPoint = cardData.maxPoint;

        int resourcePointModifier = GetResourcePointModifier(owner, cardData, ref minPoint, ref maxPoint);
        int buffModifier = GetCurrentBuffPointModifier(owner, cardData);
        int selfEffectModifier = GetSelfEffectPointModifier(cardData);
        int totalModifier = resourcePointModifier + buffModifier + selfEffectModifier;

        minPoint += totalModifier;
        maxPoint += totalModifier;

        if (minPoint < 0)
        {
            minPoint = 0;
        }

        if (maxPoint < 0)
        {
            maxPoint = 0;
        }

        return minPoint + "-" + maxPoint;
    }

    static string BuildDescriptionText(
        CharacterData owner,
        CardTestData cardData,
        bool isUsable,
        string unavailableReason
    )
    {
        StringBuilder builder = new StringBuilder();

        AppendUseConditions(builder, cardData);
        AppendResourceRule(builder, owner, cardData);
        AppendFormula(builder, cardData);
        AppendEffects(builder, cardData);

        if (!isUsable)
        {
            AppendLine(builder, "当前不可用：" + unavailableReason);
        }

        if (builder.Length == 0)
        {
            return "暂无额外效果。";
        }

        return builder.ToString().TrimEnd();
    }

    static void AppendUseConditions(StringBuilder builder, CardTestData cardData)
    {
        if (cardData.useConditions == null || cardData.useConditions.Length == 0)
        {
            return;
        }

        foreach (CardUseConditionData condition in cardData.useConditions)
        {
            if (condition == null)
            {
                continue;
            }

            if (condition.conditionType == CardUseConditionType.BuffStackAtLeast)
            {
                AppendLine(builder, "使用条件：" + GetTargetDisplayName(condition.target) + " " + condition.buffType + " 至少 " + condition.value + " 层。");
                continue;
            }

            if (condition.conditionType == CardUseConditionType.HasBuff)
            {
                AppendLine(builder, "使用条件：" + GetTargetDisplayName(condition.target) + " 拥有 " + condition.buffType + "。");
                continue;
            }

            if (condition.conditionType == CardUseConditionType.GuiltAtLeast)
            {
                AppendLine(builder, "使用条件：负罪感至少 " + condition.value + "。");
                continue;
            }

            AppendLine(builder, "使用条件：" + condition.conditionType + "。");
        }
    }

    static void AppendResourceRule(StringBuilder builder, CharacterData owner, CardTestData cardData)
    {
        CardResourceRuleData rule = cardData.resourceRule;

        if (rule == null || rule.resourceType != ResourceTypeBuffStack)
        {
            return;
        }

        int currentStack = owner != null ? owner.GetBuffStack(rule.resourceID) : 0;
        int pointModifier = currentStack * rule.pointPerStack;

        if (rule.exactStackForBonus > 0 && currentStack == rule.exactStackForBonus)
        {
            pointModifier += rule.exactStackPointBonus;
        }

        AppendLine(builder, "当前 " + rule.resourceID + " 为 " + currentStack + "。");

        if (currentStack >= rule.requiredStackForNormalVersion)
        {
            AppendLine(builder, "资源满足，使用基础点数。");
        }
        else
        {
            AppendLine(builder, "资源不足，使用降级点数 " + rule.fallbackMinPoint + "-" + rule.fallbackMaxPoint + "。");
        }

        if (pointModifier != 0)
        {
            AppendLine(builder, "点数 +" + pointModifier + "。");
        }

        if (rule.consumeAmountOnSuccess > 0)
        {
            AppendLine(builder, "使用成功消耗 " + rule.consumeAmountOnSuccess + " 点 " + rule.resourceID + "。");
        }
    }

    static void AppendFormula(StringBuilder builder, CardTestData cardData)
    {
        if (cardData.damageFormula == "PointAsDamage")
        {
            AppendLine(builder, "伤害=点数。");
            return;
        }

        if (cardData.damageFormula == "DoublePointDamage")
        {
            AppendLine(builder, "伤害=点数x2。");
            return;
        }

        if (!string.IsNullOrEmpty(cardData.damageFormula))
        {
            AppendLine(builder, "伤害公式：" + cardData.damageFormula + "。");
        }

        if (cardData.defenseFormula == "PointAsDefense")
        {
            AppendLine(builder, "防御值=点数。");
            return;
        }

        if (!string.IsNullOrEmpty(cardData.defenseFormula))
        {
            AppendLine(builder, "防御公式：" + cardData.defenseFormula + "。");
        }
    }

    static void AppendEffects(StringBuilder builder, CardTestData cardData)
    {
        if (cardData.effects == null || cardData.effects.Count == 0)
        {
            return;
        }

        foreach (CardEffectData effect in cardData.effects)
        {
            if (effect == null)
            {
                continue;
            }

            if (effect.effectType == CardEffectType.ApplyBuff)
            {
                AppendLine(builder, GetTimingDisplayName(effect.trigger) + "：" + GetTargetDisplayName(effect.target) + "获得 " + effect.buffType + " x" + effect.stack + "。");
                continue;
            }

            if (effect.effectType == CardEffectType.ReduceCooldown)
            {
                AppendLine(builder, GetTimingDisplayName(effect.trigger) + "：减少冷却 " + effect.cooldownAmount + "。");
            }
        }
    }

    static int GetResourcePointModifier(
        CharacterData owner,
        CardTestData cardData,
        ref int minPoint,
        ref int maxPoint
    )
    {
        CardResourceRuleData rule = cardData.resourceRule;

        if (rule == null || rule.resourceType != ResourceTypeBuffStack)
        {
            return 0;
        }

        int currentStack = owner != null ? owner.GetBuffStack(rule.resourceID) : 0;

        if (currentStack >= rule.requiredStackForNormalVersion)
        {
            minPoint = cardData.minPoint;
            maxPoint = cardData.maxPoint;
        }
        else
        {
            minPoint = rule.fallbackMinPoint;
            maxPoint = rule.fallbackMaxPoint;
        }

        int modifier = currentStack * rule.pointPerStack;

        if (rule.exactStackForBonus > 0 && currentStack == rule.exactStackForBonus)
        {
            modifier += rule.exactStackPointBonus;
        }

        return modifier;
    }

    static int GetCurrentBuffPointModifier(CharacterData owner, CardTestData cardData)
    {
        if (owner == null)
        {
            return 0;
        }

        float modifier = 0f;

        if (cardData.cardType == CardType.Attack)
        {
            modifier += owner.GetBuffFlatModifier("AttackPoint");
            modifier += owner.GetBuffFlatModifier("CardPoint");

            if (cardData.isClashable)
            {
                modifier += owner.GetBuffFlatModifier("ClashPoint");
            }
        }
        else if (cardData.cardType == CardType.Defense)
        {
            modifier += owner.GetBuffFlatModifier("DefensePoint");
            modifier += owner.GetBuffFlatModifier("CardPoint");
        }
        else if (cardData.cardType == CardType.Dodge)
        {
            modifier += owner.GetBuffFlatModifier("ClashPoint");
            modifier += owner.GetBuffFlatModifier("CardPoint");
        }

        return Mathf.RoundToInt(modifier);
    }

    static int GetSelfEffectPointModifier(CardTestData cardData)
    {
        if (cardData.effects == null || cardData.effects.Count == 0)
        {
            return 0;
        }

        float modifier = 0f;

        foreach (CardEffectData effect in cardData.effects)
        {
            if (!CanPreviewSelfPointEffect(effect))
            {
                continue;
            }

            BuffDefinitionData definition;

            if (!BuffDefinitionLoader.TryGetDefinition(effect.buffType, out definition) || definition == null)
            {
                continue;
            }

            if (definition.effectType != "FlatModifier")
            {
                continue;
            }

            if (!DoesStatAffectCardPoint(cardData, definition.targetStat))
            {
                continue;
            }

            modifier += effect.stack * definition.valuePerStack;
        }

        return Mathf.RoundToInt(modifier);
    }

    static bool CanPreviewSelfPointEffect(CardEffectData effect)
    {
        if (effect == null)
        {
            return false;
        }

        bool timingMatches =
            effect.trigger == BattleTiming.BeforeUse ||
            effect.trigger == BattleTiming.OnPlay;

        return timingMatches &&
            effect.effectType == CardEffectType.ApplyBuff &&
            effect.target == CardTargetType.Self &&
            !string.IsNullOrEmpty(effect.buffType);
    }

    static bool DoesStatAffectCardPoint(CardTestData cardData, string targetStat)
    {
        if (targetStat == "CardPoint")
        {
            return true;
        }

        if (cardData.cardType == CardType.Attack)
        {
            return targetStat == "AttackPoint" ||
                (targetStat == "ClashPoint" && cardData.isClashable);
        }

        if (cardData.cardType == CardType.Defense)
        {
            return targetStat == "DefensePoint";
        }

        if (cardData.cardType == CardType.Dodge)
        {
            return targetStat == "ClashPoint";
        }

        return false;
    }

    static string GetTargetDisplayName(string target)
    {
        if (target == CardTargetType.Self)
        {
            return "自身";
        }

        if (target == CardTargetType.Target)
        {
            return "目标";
        }

        if (target == CardTargetType.AllAlly)
        {
            return "全体友方";
        }

        if (target == CardTargetType.AllEnemy)
        {
            return "全体敌方";
        }

        return string.IsNullOrEmpty(target) ? "目标" : target;
    }

    static string GetTimingDisplayName(string timing)
    {
        if (timing == BattleTiming.BeforeUse)
        {
            return "使用前";
        }

        if (timing == BattleTiming.OnPlay)
        {
            return "使用时";
        }

        if (timing == BattleTiming.AfterDamage)
        {
            return "造成伤害后";
        }

        if (timing == BattleTiming.Resolved)
        {
            return "生效时";
        }

        return string.IsNullOrEmpty(timing) ? "效果" : timing;
    }

    static void AppendLine(StringBuilder builder, string line)
    {
        if (builder == null || string.IsNullOrEmpty(line))
        {
            return;
        }

        builder.AppendLine(line);
    }
}
