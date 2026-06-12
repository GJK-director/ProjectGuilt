// 脚本中文说明：战斗计算器。负责投点、拼点修正、伤害公式和防御数值计算，不负责流程控制。
using UnityEngine;

// BattleCalculator = 战斗计算器
// 只负责计算，不负责测试流程
public static class BattleCalculator
{
    // VALUE_SCALE = 数值放大倍率
    // 100 表示内部保留两位小数精度
    // 例如：5 点伤害 = 500 内部值
    const int VALUE_SCALE = 100;

    const string StatAttackPoint = "AttackPoint";
    const string StatClashPoint = "ClashPoint";
    const string StatCardPoint = "CardPoint";
    const string StatDefensePoint = "DefensePoint";
    const string StatDamageDealt = "DamageDealt";
    const string StatDamageTaken = "DamageTaken";

    // Rollpoint = 随机投点
    public static int Rollpoint(int minPoint, int maxPoint)
    {
        return Random.Range(minPoint, maxPoint + 1);
    }

    // GetFinalClashPoint = 计算最终拼点值
    public static int GetFinalClashPoint(CharacterData unit, CardTestData card)
    {
        return GetFinalCardPoint(
            unit,
            card,
            true,
            card != null && card.isClashable,
            true,
            0,
            0,
            false,
            false,
            "拼点"
        );
    }

    public static int GetFinalClashPoint(
        CharacterData unit,
        CardTestData card,
        int clashPointModifier,
        int cardPointModifier
    )
    {
        return GetFinalCardPoint(
            unit,
            card,
            true,
            card != null && card.isClashable,
            true,
            clashPointModifier,
            cardPointModifier,
            true,
            true,
            "拼点"
        );
    }

    // GetFinalAttackPointWithoutClash = 计算不进入正式拼点时的攻击点数
    public static int GetFinalAttackPointWithoutClash(CharacterData unit, CardTestData card)
    {
        return GetFinalCardPoint(
            unit,
            card,
            true,
            false,
            true,
            0,
            0,
            false,
            false,
            "攻击点数"
        );
    }

    public static int GetFinalAttackPointWithoutClash(
        CharacterData unit,
        CardTestData card,
        int cardPointModifier
    )
    {
        return GetFinalCardPoint(
            unit,
            card,
            true,
            false,
            true,
            0,
            cardPointModifier,
            false,
            true,
            "攻击点数"
        );
    }

    static int GetFinalCardPoint(
        CharacterData unit,
        CardTestData card,
        bool includeAttackPoint,
        bool includeClashPoint,
        bool includeCardPoint,
        int explicitClashPointModifier,
        int explicitCardPointModifier,
        bool useExplicitClashPointModifier,
        bool useExplicitCardPointModifier,
        string pointLabel
    )
    {
        int basePoint = Rollpoint(card.minPoint, card.maxPoint);

        int finalPoint = basePoint;

        if (includeAttackPoint && card.cardType == CardType.Attack)
        {
            int attackPointModifier = Mathf.RoundToInt(unit.GetBuffFlatModifier(StatAttackPoint));
            finalPoint += attackPointModifier;

            if (attackPointModifier != 0 && BattleDebugSettings.ShowDetailBattleLog)
            {
                Debug.Log(unit.characterName + " 攻击点数修正：" + attackPointModifier);
            }
        }

        if (includeClashPoint)
        {
            int clashPointModifier = useExplicitClashPointModifier
                ? explicitClashPointModifier
                : Mathf.RoundToInt(unit.GetBuffFlatModifier(StatClashPoint));
            finalPoint += clashPointModifier;

            if (clashPointModifier != 0 && BattleDebugSettings.ShowDetailBattleLog)
            {
                Debug.Log(unit.characterName + " 拼点点数修正：" + clashPointModifier);
            }
        }

        if (includeCardPoint)
        {
            int cardPointModifier = useExplicitCardPointModifier
                ? explicitCardPointModifier
                : Mathf.RoundToInt(unit.GetBuffFlatModifier(StatCardPoint));
            finalPoint += cardPointModifier;

            if (cardPointModifier != 0 && BattleDebugSettings.ShowDetailBattleLog)
            {
                Debug.Log(unit.characterName + " 卡牌点数修正：" + cardPointModifier);
            }
        }

        if (finalPoint < 0)
        {
            finalPoint = 0;
        }

        if (BattleDebugSettings.ShowDetailBattleLog)
        {
            Debug.Log(
                unit.characterName + " 使用 " + card.cardName +
                "，基础" + pointLabel + "：" + basePoint +
                "，最终" + pointLabel + "：" + finalPoint
            );
        }

        return finalPoint;
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

    // CalculateRemainingAttackPointAfterDefense = 计算防御后的剩余攻击点数
    // enemyFinalAttackPoint 使用普通点数；playerFinalDefensePointScaled 使用现有内部放大值。
    // 返回值保持为普通点数，可直接传给 GetFinalDamageScaled(...) 作为攻击点数输入。
    public static int CalculateRemainingAttackPointAfterDefense(
        int enemyFinalAttackPoint,
        int playerFinalDefensePointScaled
    )
    {
        if (enemyFinalAttackPoint < 0)
        {
            enemyFinalAttackPoint = 0;
        }

        if (playerFinalDefensePointScaled < 0)
        {
            playerFinalDefensePointScaled = 0;
        }

        int enemyAttackPointScaled = ToScaledValue(enemyFinalAttackPoint);
        int remainingAttackPointScaled = enemyAttackPointScaled - playerFinalDefensePointScaled;

        if (remainingAttackPointScaled <= 0)
        {
            return 0;
        }

        return ConvertScaledDamageToHPDamage(remainingAttackPointScaled);
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
        //看到这里
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

        int damageMultiplier = Mathf.RoundToInt(
            100f +
            attacker.GetBuffPercentModifier(StatDamageDealt) +
            defender.GetBuffPercentModifier(StatDamageTaken)
        );

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
        return GetFinalDefensePointScaled(defender, defenseCard, 0, false);
    }

    public static int GetFinalDefensePointScaled(
        CharacterData defender,
        CardTestData defenseCard,
        int cardPointModifier
    )
    {
        return GetFinalDefensePointScaled(defender, defenseCard, cardPointModifier, true);
    }

    static int GetFinalDefensePointScaled(
        CharacterData defender,
        CardTestData defenseCard,
        int explicitCardPointModifier,
        bool useExplicitCardPointModifier
    )
    {
        int basePoint = Rollpoint(defenseCard.minPoint, defenseCard.maxPoint);

        int baseDefenseScaled = GetBaseDefenseScaledByFormula(defender, defenseCard, basePoint);

        int defensePointModifier = Mathf.RoundToInt(
            defender.GetBuffFlatModifier(StatDefensePoint) +
            (useExplicitCardPointModifier
                ? explicitCardPointModifier
                : defender.GetBuffFlatModifier(StatCardPoint))
        );

        int finalDefenseScaled = baseDefenseScaled + ToScaledValue(defensePointModifier);

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
