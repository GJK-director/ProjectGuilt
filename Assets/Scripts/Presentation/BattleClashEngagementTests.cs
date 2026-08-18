using System.Text;
using UnityEngine;

public static class BattleClashEngagementTests
{
    private const float Tolerance = 0.001f;

    public static bool Run()
    {
        BattleClashEngagementProfile profile =
            ScriptableObject.CreateInstance<BattleClashEngagementProfile>();

        bool equalLow = VerifyShares(profile, 1f, 1f, 0.5f, 0.5f);
        bool equalHigh = VerifyShares(profile, 10f, 10f, 0.5f, 0.5f);
        bool twoToOne = VerifyShares(
            profile,
            2f,
            1f,
            0.6166667f,
            0.3833333f
        );
        bool tenToOne = VerifyShares(
            profile,
            10f,
            1f,
            0.7863636f,
            0.2136364f
        );
        bool oneToTen = VerifyShares(
            profile,
            1f,
            10f,
            0.2136364f,
            0.7863636f
        );
        bool zeroSpeeds = VerifyShares(profile, 0f, 0f, 0.5f, 0.5f);
        bool contract = VerifyRangeAndSum(profile);

        Object.DestroyImmediate(profile);

        Debug.Log("模式84 测试A 1 vs 1份额为0.5/0.5：" + equalLow);
        Debug.Log("模式84 测试B 10 vs 10份额为0.5/0.5：" + equalHigh);
        Debug.Log("模式84 测试C 2 vs 1份额约为0.617/0.383：" + twoToOne);
        Debug.Log("模式84 测试D 10 vs 1份额约为0.786/0.214：" + tenToOne);
        Debug.Log("模式84 测试E 1 vs 10份额约为0.214/0.786：" + oneToTen);
        Debug.Log("模式84 测试F 0 vs 0份额为0.5/0.5：" + zeroSpeeds);
        Debug.Log("模式84 测试G 份额和及范围契约：" + contract);

        bool passed = equalLow && equalHigh && twoToOne && tenToOne &&
            oneToTen && zeroSpeeds && contract;
        Debug.Log("模式84 Clash Engagement聚合结果：" + passed);
        return passed;
    }

    private static bool VerifyShares(
        BattleClashEngagementProfile profile,
        float speedA,
        float speedB,
        float expectedA,
        float expectedB
    )
    {
        BattleClashEngagementResult result =
            BattleClashEngagementResolver.Resolve(
                profile,
                "test_a",
                "test_b",
                speedA,
                speedB
            );
        return Mathf.Abs(result.SideAMovementShare - expectedA) <=
                Tolerance &&
            Mathf.Abs(result.SideBMovementShare - expectedB) <= Tolerance;
    }

    private static bool VerifyRangeAndSum(
        BattleClashEngagementProfile profile
    )
    {
        float[] speeds = { 0f, 1f, 2f, 10f, 100f };
        bool allCasesValid = true;
        StringBuilder caseDiagnostics = new StringBuilder();
        for (int sideAIndex = 0; sideAIndex < speeds.Length; sideAIndex++)
        {
            for (int sideBIndex = 0; sideBIndex < speeds.Length; sideBIndex++)
            {
                float speedA = speeds[sideAIndex];
                float speedB = speeds[sideBIndex];
                BattleClashEngagementResult result =
                    BattleClashEngagementResolver.Resolve(
                        profile,
                        "test_a",
                        "test_b",
                        speedA,
                        speedB
                    );
                float shareA = result.SideAMovementShare;
                float shareB = result.SideBMovementShare;
                float sum = shareA + shareB;
                bool shareAInRange = IsWithinShareRange(shareA, profile);
                bool shareBInRange = IsWithinShareRange(shareB, profile);
                bool sumValid = Mathf.Approximately(sum, 1f);
                bool caseValid = shareAInRange && shareBInRange && sumValid;

                allCasesValid &= caseValid;
                caseDiagnostics.AppendLine(
                    "Case Speed=" + speedA + "/" + speedB +
                    " Share=" + shareA + "/" + shareB +
                    " Sum=" + sum +
                    " AInRange=" + shareAInRange +
                    " BInRange=" + shareBInRange +
                    " SumValid=" + sumValid
                );
            }
        }

        if (!allCasesValid)
        {
            // 仅在模式84失败时打印完整输入，避免正常运行持续产生诊断日志。
            Debug.LogError(
                "Mode84 G FAIL" +
                " Min=" + profile.MinMovementShare +
                " Max=" + profile.MaxMovementShare +
                " Influence=" + profile.RelativeSpeedInfluence +
                "\n" + caseDiagnostics
            );
        }

        return allCasesValid;
    }

    private static bool IsWithinShareRange(
        float share,
        BattleClashEngagementProfile profile
    )
    {
        // Clamp 边界经过 1f - share 后会有单精度舍入误差。
        return share >= profile.MinMovementShare - Tolerance &&
            share <= profile.MaxMovementShare + Tolerance;
    }
}
