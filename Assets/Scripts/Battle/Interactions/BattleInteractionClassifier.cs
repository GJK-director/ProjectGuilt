// 脚本中文说明：统一根据两侧卡牌类型识别战斗 Interaction，不包含阵营或表现层语义。
public enum BattleInteractionType
{
    NoInteraction,
    AttackVsAttack,
    AttackVsDefense,
    AttackVsDodge,
    UnilateralAttack
}

public static class BattleInteractionClassifier
{
    // NoInteraction 仅描述规则分类：不 Roll、不伤害、不播放表现、不 Resolved、不进入 CD、也不消费资源。
    // 已安排的 Planning 槽位仍保留；Execution 层未来负责安全跳过，本类不参与执行。
    public static BattleInteractionType Classify(
        BattleCardState sideA,
        BattleCardState sideB
    )
    {
        return Classify(
            sideA != null ? sideA.cardData : null,
            sideB != null ? sideB.cardData : null
        );
    }

    public static BattleInteractionType Classify(
        CardTestData sideA,
        CardTestData sideB
    )
    {
        if (sideA == null && sideB == null)
        {
            return BattleInteractionType.NoInteraction;
        }

        if (sideA == null)
        {
            return sideB.cardType == CardType.Attack
                ? BattleInteractionType.UnilateralAttack
                : BattleInteractionType.NoInteraction;
        }

        if (sideB == null)
        {
            return sideA.cardType == CardType.Attack
                ? BattleInteractionType.UnilateralAttack
                : BattleInteractionType.NoInteraction;
        }

        bool sideAIsAttack = sideA.cardType == CardType.Attack;
        bool sideBIsAttack = sideB.cardType == CardType.Attack;

        if (sideAIsAttack && sideBIsAttack)
        {
            return BattleInteractionType.AttackVsAttack;
        }

        if ((sideAIsAttack && sideB.cardType == CardType.Defense) ||
            (sideBIsAttack && sideA.cardType == CardType.Defense))
        {
            return BattleInteractionType.AttackVsDefense;
        }

        if ((sideAIsAttack && sideB.cardType == CardType.Dodge) ||
            (sideBIsAttack && sideA.cardType == CardType.Dodge))
        {
            return BattleInteractionType.AttackVsDodge;
        }

        return BattleInteractionType.NoInteraction;
    }
}
