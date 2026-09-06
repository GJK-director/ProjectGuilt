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

// ALL IN 的卡牌固有规则：把本次资源快照捕获的子弹作为一次攻击的倍率与显示分段。
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

    public static int GetDamageMultiplierPercent(int capturedBullet)
    {
        switch (UnityEngine.Mathf.Clamp(capturedBullet, 0, 6))
        {
            case 1: return 100;
            case 2: return 180;
            case 3: return 230;
            case 4: return 270;
            case 5: return 300;
            case 6: return 320;
            default: return 0;
        }
    }

    public static int GetHpDisplayStageCount(int capturedBullet)
    {
        return UnityEngine.Mathf.Max(1, capturedBullet);
    }

    public static int CombineDamageMultiplierPercent(int current, int additional)
    {
        return UnityEngine.Mathf.Max(0, current) *
            UnityEngine.Mathf.Max(0, additional) / 100;
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
    // 逻辑伤害仍只提交一次；大于1时只把HP显示拆成多段。
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
}
