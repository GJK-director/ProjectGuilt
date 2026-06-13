// 脚本中文说明：罪卡条件检查器。负责判断罪卡当前是否满足 HP、Buff、负罪感等使用条件。
using UnityEngine;

// SinCardConditionChecker = 罪卡使用条件检查器
// 负责判断一张罪卡当前是否满足使用前提
public static class SinCardConditionChecker
{
    // EvaluateUseConditions = 通用 useConditions 解释入口。
    // useConditions 是通用卡牌使用条件，不只属于罪卡。
    // isSinCard 只控制罪卡 UseCount、guilt 和消耗等专属规则。
    public static CardEligibilityResult EvaluateUseConditions(
        CharacterData user,
        CharacterData target,
        CardTestData cardData
    )
    {
        if (cardData == null)
        {
            return CardEligibilityResult.Failure(
                CardEligibilityFailureReason.InvalidCardData,
                "卡牌数据为空"
            );
        }

        if (cardData.useConditions == null || cardData.useConditions.Length == 0)
        {
            return CardEligibilityResult.Success();
        }

        for (int i = 0; i < cardData.useConditions.Length; i++)
        {
            CardEligibilityResult result = EvaluateCondition(user, target, cardData.useConditions[i]);

            if (!result.isEligible)
            {
                return result;
            }
        }

        return CardEligibilityResult.Success();
    }

    // CanUseSinCard = 判断罪卡是否可以使用
    // user = 使用者
    // target = 当前目标
    // failReason = 不能使用的原因
    public static bool CanUseSinCard(
        CharacterData user,
        CharacterData target,
        CardTestData cardData,
        out string failReason
    )
    {
        failReason = "";

        CardEligibilityResult result = EvaluateUseConditions(user, target, cardData);
        failReason = result.failureMessage;

        return result.isEligible;
    }

    // EvaluateCondition = 判断单个条件是否满足
    static CardEligibilityResult EvaluateCondition(
        CharacterData user,
        CharacterData target,
        CardUseConditionData condition
    )
    {
        if (condition == null)
        {
            return CardEligibilityResult.Success();
        }

        string conditionType = condition.conditionType;

        if (string.IsNullOrEmpty(conditionType))
        {
            conditionType = CardUseConditionType.None;
        }

        if (conditionType == CardUseConditionType.None)
        {
            return CardEligibilityResult.Success();
        }

        // 先获取要检查的角色
        CharacterData checkTarget = GetConditionTarget(user, target, condition.target);

        if (checkTarget == null)
        {
            return CardEligibilityResult.Failure(
                CardEligibilityFailureReason.UnsupportedCondition,
                "使用条件检查失败：目标为空",
                conditionType
            );
        }

        // 下面这些条件先预留入口
        // 下一步我们会根据 CharacterData 里现有字段，一个个正式实现

        if (conditionType == CardUseConditionType.HpBelowPercent)
        {
            int requiredPercent = condition.value;

            float currentHpPercent = GetHpPercent(checkTarget);

            if (currentHpPercent < requiredPercent)
            {
                return CardEligibilityResult.Success();
            }

            return CardEligibilityResult.Failure(
                CardEligibilityFailureReason.UnsupportedCondition,
                checkTarget.characterName +
                " 当前 HP 百分比不满足条件，需要低于：" +
                requiredPercent +
                "%，当前：" +
                currentHpPercent.ToString("F1") +
                "%",
                conditionType,
                "",
                requiredPercent,
                (int)currentHpPercent
            );
        }

        if (conditionType == CardUseConditionType.HpAbovePercent)
        {
            int requiredPercent = condition.value;

            float currentHpPercent = GetHpPercent(checkTarget);

            if (currentHpPercent > requiredPercent)
            {
                return CardEligibilityResult.Success();
            }

            return CardEligibilityResult.Failure(
                CardEligibilityFailureReason.UnsupportedCondition,
                checkTarget.characterName +
                " 当前 HP 百分比不满足条件，需要高于：" +
                requiredPercent +
                "%，当前：" +
                currentHpPercent.ToString("F1") +
                "%",
                conditionType,
                "",
                requiredPercent,
                (int)currentHpPercent
            );
        }

        if (conditionType == CardUseConditionType.HasBuff)
        {
            if (string.IsNullOrEmpty(condition.buffType))
            {
                return CardEligibilityResult.Failure(
                    CardEligibilityFailureReason.BuffStackRequirementNotMet,
                    "使用条件缺少 buffType",
                    conditionType
                );
            }

            if (!HasBuff(checkTarget, condition.buffType))
            {
                return CardEligibilityResult.Failure(
                    CardEligibilityFailureReason.BuffStackRequirementNotMet,
                    checkTarget.characterName + " 没有状态：" + condition.buffType,
                    conditionType,
                    condition.buffType,
                    1,
                    0
                );
            }

            return CardEligibilityResult.Success();
        }

        if (conditionType == CardUseConditionType.BuffStackAtLeast)
        {
            if (string.IsNullOrEmpty(condition.buffType))
            {
                return CardEligibilityResult.Failure(
                    CardEligibilityFailureReason.BuffStackRequirementNotMet,
                    "使用条件缺少 buffType",
                    conditionType
                );
            }

            int requiredStack = condition.value;

            if (requiredStack <= 0)
            {
                requiredStack = 1;
            }

            int currentStack = GetBuffStack(checkTarget, condition.buffType);

            if (currentStack < requiredStack)
            {
                return CardEligibilityResult.Failure(
                    CardEligibilityFailureReason.BuffStackRequirementNotMet,
                    checkTarget.characterName +
                    " 的状态 " +
                    condition.buffType +
                    " 层数不足，需要：" +
                    requiredStack +
                    "，当前：" +
                    currentStack,
                    conditionType,
                    condition.buffType,
                    requiredStack,
                    currentStack
                );
            }

            return CardEligibilityResult.Success();
        }

        if (conditionType == CardUseConditionType.GuiltAtLeast)
        {
            int requiredGuilt = condition.value;
            int currentGuilt = checkTarget.currentGuilt;

            if (currentGuilt >= requiredGuilt)
            {
                return CardEligibilityResult.Success();
            }

            return CardEligibilityResult.Failure(
                CardEligibilityFailureReason.GuiltRequirementNotMet,
                checkTarget.characterName +
                " 当前负罪感不足，需要至少：" +
                requiredGuilt +
                "，当前：" +
                currentGuilt,
                conditionType,
                "",
                requiredGuilt,
                currentGuilt
            );
        }

        if (conditionType == CardUseConditionType.GuiltBelow)
        {
            int requiredGuilt = condition.value;
            int currentGuilt = checkTarget.currentGuilt;

            if (currentGuilt < requiredGuilt)
            {
                return CardEligibilityResult.Success();
            }

            return CardEligibilityResult.Failure(
                CardEligibilityFailureReason.GuiltRequirementNotMet,
                checkTarget.characterName +
                " 当前负罪感过高，需要低于：" +
                requiredGuilt +
                "，当前：" +
                currentGuilt,
                conditionType,
                "",
                requiredGuilt,
                currentGuilt
            );
        }

        return CardEligibilityResult.Failure(
            CardEligibilityFailureReason.UnsupportedCondition,
            "未知使用条件类型：" + conditionType,
            conditionType
        );
    }
    // HasBuff = 是否拥有指定 Buff
    // 注意：这里检查的是 buffID，不是中文 buffName
    static bool HasBuff(CharacterData character, string buffID)
    {
        return GetBuffStack(character, buffID) > 0;
    }
    // GetHpPercent = 获取角色当前 HP 百分比
    // 返回值范围大概是 0 到 100
    static float GetHpPercent(CharacterData character)
    {
        if (character == null)
        {
            return 0f;
        }

        if (character.maxHP <= 0)
        {
            return 0f;
        }

        return (float)character.currentHP / character.maxHP * 100f;
    }

    // GetBuffStack = 获取指定 Buff 的总层数
    // 如果同一个 buffID 有多个 BuffData，就把层数加起来
    static int GetBuffStack(CharacterData character, string buffID)
    {
        if (character == null)
        {
            return 0;
        }

        if (character.buffs == null || character.buffs.Count == 0)
        {
            return 0;
        }

        if (string.IsNullOrEmpty(buffID))
        {
            return 0;
        }

        int totalStack = 0;

        foreach (BuffData buff in character.buffs)
        {
            if (buff == null)
            {
                continue;
            }

            if (buff.buffID == buffID)
            {
                if (buff.stack > 0)
                {
                    totalStack += buff.stack;
                }
            }
        }

        return totalStack;
    }
    // GetConditionTarget = 获取条件检查目标
    static CharacterData GetConditionTarget(
        CharacterData user,
        CharacterData target,
        string targetType
    )
    {
        if (string.IsNullOrEmpty(targetType))
        {
            targetType = CardTargetType.Self;
        }

        if (targetType == CardTargetType.Self)
        {
            return user;
        }

        if (targetType == CardTargetType.Target)
        {
            return target;
        }

        Debug.LogWarning("暂未处理的条件目标类型：" + targetType);
        return null;
    }
}
