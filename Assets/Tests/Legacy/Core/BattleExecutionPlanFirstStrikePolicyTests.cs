// 脚本中文说明：验证完整 ExecutionItem 的 FirstStrike Priority Policy，不执行 Combat。
using UnityEngine;

public static class BattleExecutionPlanFirstStrikePolicyTests
{
    public static bool Run()
    {
        bool[] results = new bool[13];

        results[0] = FirstStrikeExecutionTests
            .FreeActionPlayerFirstStrikeUsesFirstStrikeTier();
        results[1] = FirstStrikeExecutionTests
            .FreeActionPlayerNormalAttackUsesNormalTier();
        results[2] = FirstStrikeExecutionTests
            .UnrespondedEnemyFirstStrikeUsesFirstStrikeTier();
        results[3] = FirstStrikeExecutionTests
            .UnrespondedEnemyNormalAttackUsesNormalTier();
        results[4] = FirstStrikeExecutionTests
            .RespondedPlayerFirstStrikeEnemyNormalUsesFirstStrikeTier();
        results[5] = FirstStrikeExecutionTests
            .RespondedPlayerNormalEnemyFirstStrikeUsesFirstStrikeTier();
        results[6] = FirstStrikeExecutionTests
            .RespondedFirstStrikeDefenseEnemyNormalUsesFirstStrikeTier();
        results[7] = FirstStrikeExecutionTests
            .RespondedNormalDefenseEnemyFirstStrikeUsesFirstStrikeTier();
        results[8] = FirstStrikeExecutionTests
            .RespondedBothNormalUsesNormalTier();
        results[9] = FirstStrikeExecutionTests
            .RespondedBothFirstStrikeUsesFirstStrikeTier();
        results[10] = FirstStrikeExecutionTests
            .FirstStrikeDoesNotChangeAttackVsDefenseInteraction();
        results[11] = FirstStrikeExecutionTests
            .FirstStrikeSortsBeforeLaterNormalFreeAction();
        results[12] = FirstStrikeExecutionTests
            .EnemyFirstStrikeRespondedItemStaysPairedAndSortsFirst();

        string[] names =
        {
            "FreeAction Player FirstStrike Attack",
            "FreeAction Player Normal Attack",
            "Unresponded Enemy FirstStrike Attack",
            "Unresponded Enemy Normal Attack",
            "Responded Player FirstStrike + Enemy Normal",
            "Responded Player Normal + Enemy FirstStrike",
            "Responded FirstStrike Defense + Enemy Normal Attack",
            "Responded Normal Defense + Enemy FirstStrike Attack",
            "Responded 双方 Normal",
            "Responded 双方 FirstStrike",
            "FirstStrike 不改变 AttackVsDefense Interaction",
            "FirstStrike 排在后建 Normal FreeAction 前",
            "Enemy FirstStrike Responded Item 不拆 Pairing 且优先"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式89 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }

        Debug.Log("模式89 FirstStrike Priority Policy聚合结果：" + allPassed);
        return allPassed;
    }

}
