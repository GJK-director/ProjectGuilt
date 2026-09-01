// 脚本中文说明：BattleInteractionClassifier 的纯规则矩阵测试。
using UnityEngine;

public static class BattleInteractionClassifierTests
{
    public static bool Run()
    {
        bool[] results = new bool[8];
        CardTestData meleeAttack = CreateCard(
            CardType.Attack,
            AttackDeliveryMode.Melee
        );
        CardTestData longRangeAttack = CreateCard(
            CardType.Attack,
            AttackDeliveryMode.LongRangeShoot
        );
        CardTestData closeRangeAttack = CreateCard(
            CardType.Attack,
            AttackDeliveryMode.CloseRangeShoot
        );
        CardTestData defense = CreateCard(CardType.Defense);
        CardTestData dodge = CreateCard(CardType.Dodge);
        CardTestData unknown = CreateCard("Unknown");

        results[0] =
            Is(meleeAttack, meleeAttack, BattleInteractionType.AttackVsAttack) &&
            Is(meleeAttack, defense, BattleInteractionType.AttackVsDefense) &&
            Is(defense, meleeAttack, BattleInteractionType.AttackVsDefense) &&
            Is(meleeAttack, dodge, BattleInteractionType.AttackVsDodge) &&
            Is(dodge, meleeAttack, BattleInteractionType.AttackVsDodge);

        results[1] =
            Is(meleeAttack, null, BattleInteractionType.UnilateralAttack) &&
            Is(null, meleeAttack, BattleInteractionType.UnilateralAttack);

        results[2] =
            Is(defense, defense, BattleInteractionType.NoInteraction) &&
            Is(defense, dodge, BattleInteractionType.NoInteraction) &&
            Is(dodge, defense, BattleInteractionType.NoInteraction) &&
            Is(dodge, dodge, BattleInteractionType.NoInteraction);

        results[3] =
            Is(defense, null, BattleInteractionType.NoInteraction) &&
            Is(dodge, null, BattleInteractionType.NoInteraction) &&
            Is(null, defense, BattleInteractionType.NoInteraction) &&
            Is(null, dodge, BattleInteractionType.NoInteraction) &&
            Is(null, null, BattleInteractionType.NoInteraction) &&
            Is(meleeAttack, unknown, BattleInteractionType.NoInteraction) &&
            Is(unknown, meleeAttack, BattleInteractionType.NoInteraction);

        results[4] =
            Is(meleeAttack, defense, BattleInteractionType.AttackVsDefense) &&
            Is(longRangeAttack, defense, BattleInteractionType.AttackVsDefense) &&
            Is(closeRangeAttack, defense, BattleInteractionType.AttackVsDefense);

        results[5] =
            Is(longRangeAttack, null, BattleInteractionType.UnilateralAttack) &&
            Is(closeRangeAttack, null, BattleInteractionType.UnilateralAttack);

        results[6] =
            BattleInteractionClassifier.Classify(meleeAttack, defense) ==
                BattleInteractionClassifier.Classify(defense, meleeAttack) &&
            BattleInteractionClassifier.Classify(meleeAttack, dodge) ==
                BattleInteractionClassifier.Classify(dodge, meleeAttack);

        results[7] = BattleInteractionClassifier.Classify(
                new BattleCardState(null, meleeAttack, "interaction87_attack"),
                new BattleCardState(null, defense, "interaction87_defense")
            ) == BattleInteractionType.AttackVsDefense;

        string[] names =
        {
            "基础Attack / Defense / Dodge矩阵",
            "Attack与null的单边攻击",
            "非Attack组合均无Interaction",
            "null与未知类型安全归类",
            "AttackDeliveryMode不改变AttackVsDefense",
            "远近程攻击与null均为UnilateralAttack",
            "AttackVsDefense与AttackVsDodge交换对称",
            "BattleCardState重载转交CardType分类"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式87 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }

        Debug.Log("模式87 BattleInteractionClassifier聚合结果：" + allPassed);
        return allPassed;
    }

    private static CardTestData CreateCard(
        string cardType,
        string attackDeliveryMode = null
    )
    {
        return new CardTestData
        {
            cardType = cardType,
            attackDeliveryMode = attackDeliveryMode
        };
    }

    private static bool Is(
        CardTestData sideA,
        CardTestData sideB,
        BattleInteractionType expected
    )
    {
        return BattleInteractionClassifier.Classify(sideA, sideB) == expected;
    }
}
