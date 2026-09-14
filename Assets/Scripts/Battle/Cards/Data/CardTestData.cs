// 脚本中文说明：卡牌测试数据。负责承接 CardsTest.json 里读取出来的一张卡牌模板数据。
using System.Collections.Generic;

// BattleCardTrait = 卡牌固有词条，不属于角色运行时 Buff。
public enum BattleCardTrait
{
    FirstStrike,
    DoubleClashAgainstDefense,
    HeavyAnger,
    IaiAnger,
    GrantNextClashPointUpOnSuccessfulDodge,
    GrantBulletOnSuccessfulDodge,
    ReloadBulletOnDodgeResolution,
    AllInBulletDump
}

public static class CardResourceInsufficientBehavior
{
    // 保持旧资源卡的缺省行为：资源不足时改用fallback点数范围。
    public const string SoftFallback = "SoftFallback";
    // 只在真实执行时判定不可用；Planning仍然可以安排该卡。
    public const string ActionUnavailable = "ActionUnavailable";
}

public static class CardResourceConsumeTiming
{
    // 保持旧卡语义：只有成功使用时才支付资源。
    public const string OnSuccessfulUse = "OnSuccessfulUse";
    // 终局射击参与即支付，胜负不影响本次支付。
    public const string OnResolvedParticipation = "OnResolvedParticipation";
}

public static class BattleDamageDistributionMode
{
    public const string Independent = "Independent";
    public const string Cumulative = "Cumulative";

    public static string ResolveOrDefault(string value)
    {
        return value == Cumulative ? Cumulative : Independent;
    }
}

// 通用多段伤害分布计算。它只把卡牌数据中的总量百分比转换为真实Impact段。
public static class BattleDamageDistribution
{
    public static int CombineMultiplierPercent(int current, int additional)
    {
        return UnityEngine.Mathf.Max(0, current) *
            UnityEngine.Mathf.Max(0, additional) / 100;
    }

    public static int GetSegmentCount(int[] percents)
    {
        return percents != null && percents.Length > 0 ? percents.Length : 1;
    }

    public static int GetCumulativeSegmentDamage(
        int baseResolvedDamage,
        int[] cumulativePercents,
        int segmentIndex
    )
    {
        int currentTotal = GetCumulativeTotal(
            baseResolvedDamage,
            cumulativePercents,
            segmentIndex
        );
        int previousTotal = segmentIndex > 0
            ? GetCumulativeTotal(
                baseResolvedDamage,
                cumulativePercents,
                segmentIndex - 1
            )
            : 0;
        return System.Math.Max(0, currentTotal - previousTotal);
    }

    static int GetCumulativeTotal(
        int baseResolvedDamage,
        int[] cumulativePercents,
        int segmentIndex
    )
    {
        int percent = cumulativePercents != null &&
            segmentIndex >= 0 && segmentIndex < cumulativePercents.Length
            ? System.Math.Max(0, cumulativePercents[segmentIndex])
            : 100;
        long total = (long)System.Math.Max(0, baseResolvedDamage) * percent;
        return (int)System.Math.Min(int.MaxValue, total / 100);
    }
}

public static class CardUsePolicy
{
    public const string Normal = "Normal";
    public const string ImmediateCommit = "ImmediateCommit";

    public static string ResolveOrDefault(string value)
    {
        return string.IsNullOrEmpty(value) ? Normal : value;
    }

    public static bool IsKnownSerializedValue(string value)
    {
        return string.IsNullOrEmpty(value) ||
            value == Normal ||
            value == ImmediateCommit;
    }
}

// CardResourceRuleData = 卡牌资源规则。
// 缺省值保持旧的软资源fallback与成功使用支付语义。
public class CardResourceRuleData
{
    public string resourceType;
    public string resourceID;
    public int requiredStackForNormalVersion;
    public int fallbackMinPoint;
    public int fallbackMaxPoint;
    public int pointPerStack;
    public int exactStackForBonus;
    public int exactStackPointBonus;
    public int consumeAmountOnSuccess;
    public bool consumeAllCapturedOnSuccess;
    public string insufficientBehavior;
    public string consumeTiming;
}

// ALL IN 的卡牌固有规则：识别卡牌并保留旧兼容查询；实际伤害分段来自CardTestData。
public static class BattleAllInRules
{
    public static bool IsAllIn(CardTestData cardData)
    {
        return cardData != null && cardData.HasTrait(BattleCardTrait.AllInBulletDump);
    }

    public static bool IsAllIn(BattleCardState cardState)
    {
        return cardState != null && IsAllIn(cardState.cardData);
    }

}

// CardTestData = 卡牌测试数据
// 用来接收 JSON 里的单张卡牌数据
public class CardTestData
{
    public string cardID;       // 卡牌ID
    public string cardName;     // 卡牌名称
    public string description;  // 一级卡面描述，由策划手写
    public string rarity;       // 稀有度
    public string cardType;     // 卡牌类型
    // Attack 的空间 / 演出兑现方式。旧数据未填写时默认视为 Melee。
    public string attackDeliveryMode;
    // 缺省时使用通用表现；特殊值只改变Presentation，不改变Combat语义。
    public string presentationVariant;
    // usePolicy = 卡牌独立的使用策略；缺省时为 Normal。
    public string usePolicy;
    public bool isSinCard;      // 是否罪卡
    public bool consumeOnUse;   // 是否使用后消耗
    public CardUseConditionData[] useConditions;
    public CardResourceRuleData resourceRule;
    public CardResourceRuleData[] resourceRules;
    // sinCardCategory = 罪卡分类
    // Clash：拼点型罪卡
    // Ability：能力型罪卡
    public string sinCardCategory;
    // maxUseCount = 本场战斗最大可生效次数
    // 主要给消耗型罪卡使用
    // 0 或小于 0 表示不限制次数
    public int maxUseCount;
    public bool isClashable;    // 是否可拼点
                                // sinCardUseRule = 罪卡使用规则
                                // UseCount：按次数消耗
                                // Permanent：本场战斗内不因使用次数消失
    public string sinCardUseRule;
    public string damageFormula;  // 伤害公式
    public string defenseFormula; // 防御公式

    public int minPoint;        // 最小点数
    public int maxPoint;        // 最大点数

    public int speedModifier;   // 速度修正
    public int cooldown;        // 冷却
    public int guiltCost;       // 负罪感消耗
                                // guiltGain = 使用罪卡后增加的负罪感
                                // 注意：这不是消耗，而是累计增加
    public int guiltGain;
    // Gameplay 多段伤害百分比；缺省时保持单段 100% 伤害。
    public int[] damageImpactPercents;
    // Independent表示每段独立百分比；Cumulative表示累计总量百分比。
    public string damageDistributionMode;
    // 每个正式Impact在前一段提交后等待的时间；第0段不等待。
    public float[] damageImpactDelaySeconds;
    // 仅用于单个Impact的旧HP表现分段，不能决定Combat伤害段数。
    public int hpDisplayStageCount;

    public List<CardEffectData> effects; // 卡牌效果列表
    public CardKeywordData[] keywords;   // 本卡描述中涉及的词条说明，由策划手写
    // traits = 卡牌固有词条；字段缺省或为空时表示没有特殊词条。
    public BattleCardTrait[] traits;

    public bool HasTrait(BattleCardTrait trait)
    {
        if (traits == null)
        {
            return false;
        }

        for (int index = 0; index < traits.Length; index++)
        {
            if (traits[index] == trait)
            {
                return true;
            }
        }

        return false;
    }

    public string GetAttackDeliveryMode()
    {
        if (cardType != CardType.Attack)
        {
            return AttackDeliveryMode.Melee;
        }

        return AttackDeliveryMode.ResolveOrDefault(attackDeliveryMode);
    }

    public bool IsMeleeAttack()
    {
        return cardType == CardType.Attack &&
            GetAttackDeliveryMode() == AttackDeliveryMode.Melee;
    }

    public bool IsLongRangeShoot()
    {
        return cardType == CardType.Attack &&
            GetAttackDeliveryMode() == AttackDeliveryMode.LongRangeShoot;
    }

    public bool IsCloseRangeShoot()
    {
        return cardType == CardType.Attack &&
            GetAttackDeliveryMode() == AttackDeliveryMode.CloseRangeShoot;
    }

    public string GetPresentationVariant()
    {
        return BattleCardPresentationVariant.ResolveOrDefault(
            presentationVariant
        );
    }

    public bool IsSpecialLongRangeDuelPresentation()
    {
        return GetPresentationVariant() ==
            BattleCardPresentationVariant.SpecialLongRangeDuel;
    }

    public string GetUsePolicy()
    {
        return CardUsePolicy.ResolveOrDefault(usePolicy);
    }

    public bool IsImmediateCommit()
    {
        return GetUsePolicy() == CardUsePolicy.ImmediateCommit;
    }
}
