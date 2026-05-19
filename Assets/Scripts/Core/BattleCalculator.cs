using UnityEngine;

// BattleCalculator = 战斗计算器
// 只负责计算，不负责测试流程
public static class BattleCalculator
{
    // VALUE_SCALE = 数值放大倍率
    // 100 表示内部保留两位小数精度
    // 例如：5 点伤害 = 500 内部值
    const int VALUE_SCALE = 100;

    // Rollpoint = 随机投点
    public static int Rollpoint(int minPoint, int maxPoint)
    {
        return Random.Range(minPoint, maxPoint + 1);
    }

    // GetFinalClashPoint = 计算最终拼点值
    public static int GetFinalClashPoint(CharacterData unit, CardTestData card)
    {
        int basePoint = Rollpoint(card.minPoint, card.maxPoint);

        int finalClashPoint = basePoint;

        // 攻击卡专用拼点 Buff
        if (card.cardType == "Attack")
        {
            int strengthStack = unit.GetBuffStack("Strength");
            int weaknessStack = unit.GetBuffStack("Weakness");

            finalClashPoint += strengthStack;
            finalClashPoint -= weaknessStack;

            if (strengthStack > 0 && BattleDebugSettings.ShowDetailBattleLog)
            {
                Debug.Log(unit.characterName + " 受到强壮影响，攻击拼点 +" + strengthStack);
            }

            if (weaknessStack > 0 && BattleDebugSettings.ShowDetailBattleLog)
            {
                Debug.Log(unit.characterName + " 受到虚弱影响，攻击拼点 -" + weaknessStack);
            }
        }

        // 通用拼点 Buff
        // NextClashPointUp = 下一次拼点点数增加
        if (card.isClashable)
        {
            int nextClashPointUpStack = unit.GetBuffStack("NextClashPointUp");

            finalClashPoint += nextClashPointUpStack;

            if (nextClashPointUpStack > 0 && BattleDebugSettings.ShowDetailBattleLog)
            {
                Debug.Log(unit.characterName + " 触发下一次拼点点数增加，拼点 +" + nextClashPointUpStack);
            }
        }

        if (finalClashPoint < 0)
        {
            finalClashPoint = 0;
        }

        if (BattleDebugSettings.ShowDetailBattleLog)
        {
            Debug.Log(
                unit.characterName + " 使用 " + card.cardName +
                "，基础拼点：" + basePoint +
                "，最终拼点：" + finalClashPoint
            );
        }

        return finalClashPoint;
    }

    // ToScaledValue = 转换成内部数值
    // 例如：5 → 500
    static int ToScaledValue(int value)
    {
        return value * VALUE_SCALE;
    }

    // ConvertScaledDamageToHPDamage = 把内部伤害转换成实际扣血
    // 例如：550 → 6
    public static int ConvertScaledDamageToHPDamage(int scaledDamage)
    {
        if (scaledDamage <= 0)
        {
            return 0;
        }

        // 向上取整
        return (scaledDamage + VALUE_SCALE - 1) / VALUE_SCALE;
    }

    // GetBaseDamageScaledByFormula = 根据伤害公式计算基础伤害内部值
    static int GetBaseDamageScaledByFormula(
        CharacterData attacker,
        CharacterData defender,
        CardTestData attackCard,
        int clashPoint
    )
    {
        if (string.IsNullOrEmpty(attackCard.damageFormula))
        {
            Debug.LogWarning(attackCard.cardName + " 没有填写 damageFormula，默认使用 PointAsDamage");
            return ToScaledValue(clashPoint);
        }

        if (attackCard.damageFormula == "PointAsDamage")
        {
            return ToScaledValue(clashPoint);
        }

        if (attackCard.damageFormula == "DoublePointDamage")
        {
            return ToScaledValue(clashPoint * 2);
        }

        Debug.LogWarning("未识别的伤害公式：" + attackCard.damageFormula + "，默认使用 PointAsDamage");

        return ToScaledValue(clashPoint);
    }

    // GetFinalDamageScaled = 计算最终伤害内部值
    public static int GetFinalDamageScaled(
        CharacterData attacker,
        CharacterData defender,
        CardTestData attackCard,
        int clashPoint
    )
    {
        int baseDamageScaled = GetBaseDamageScaledByFormula(attacker, defender, attackCard, clashPoint);

        // 100 = 1.00倍
        int damageMultiplier = 100;

        int damageUpStack = attacker.GetBuffStack("DamageUp");
        int damageDownStack = attacker.GetBuffStack("DamageDown");
        int vulnerableStack = defender.GetBuffStack("Vulnerable");
        int damageReductionStack = defender.GetBuffStack("DamageReduction");

        // 暂定：每层 = 10%
        damageMultiplier += damageUpStack * 10;
        damageMultiplier -= damageDownStack * 10;
        damageMultiplier += vulnerableStack * 10;
        damageMultiplier -= damageReductionStack * 10;

        if (damageMultiplier < 0)
        {
            damageMultiplier = 0;
        }

        int finalDamageScaled = baseDamageScaled * damageMultiplier / 100;

        if (BattleDebugSettings.ShowDamageLog)
        {
            Debug.Log(
                attacker.characterName + " 使用 " + attackCard.cardName +
                "，基础伤害内部值：" + baseDamageScaled +
                "，伤害倍率：" + damageMultiplier +
                "%，最终伤害内部值：" + finalDamageScaled
            );
        }

        return finalDamageScaled;
    }

    // GetBaseDefenseScaledByFormula = 根据防御公式计算基础防御内部值
    static int GetBaseDefenseScaledByFormula(
        CharacterData defender,
        CardTestData defenseCard,
        int cardPoint
    )
    {
        if (string.IsNullOrEmpty(defenseCard.defenseFormula))
        {
            Debug.LogWarning(defenseCard.cardName + " 没有填写 defenseFormula，默认使用 PointAsDefense");
            return ToScaledValue(cardPoint);
        }

        if (defenseCard.defenseFormula == "PointAsDefense")
        {
            return ToScaledValue(cardPoint);
        }

        Debug.LogWarning("未识别的防御公式：" + defenseCard.defenseFormula + "，默认使用 PointAsDefense");

        return ToScaledValue(cardPoint);
    }

    // GetFinalDefensePointScaled = 计算最终防御内部值
    public static int GetFinalDefensePointScaled(CharacterData defender, CardTestData defenseCard)
    {
        int basePoint = Rollpoint(defenseCard.minPoint, defenseCard.maxPoint);

        int baseDefenseScaled = GetBaseDefenseScaledByFormula(defender, defenseCard, basePoint);

        int finalDefenseScaled = baseDefenseScaled;

        int guardUpStack = defender.GetBuffStack("GuardUp");
        int guardDownStack = defender.GetBuffStack("GuardDown");

        finalDefenseScaled += ToScaledValue(guardUpStack);
        finalDefenseScaled -= ToScaledValue(guardDownStack);

        if (finalDefenseScaled < 0)
        {
            finalDefenseScaled = 0;
        }

        if (BattleDebugSettings.ShowDamageLog)
        {
            Debug.Log(
                defender.characterName + " 使用 " + defenseCard.cardName +
                "，基础点数：" + basePoint +
                "，基础防御内部值：" + baseDefenseScaled +
                "，最终防御内部值：" + finalDefenseScaled
            );
        }

        return finalDefenseScaled;
    }
}