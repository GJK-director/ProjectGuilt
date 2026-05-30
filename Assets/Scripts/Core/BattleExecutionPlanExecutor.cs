// 脚本中文说明：战斗执行计划执行器。负责按执行计划逐项处理已响应敌人意图和无人响应敌人意图。
using UnityEngine;

// BattleExecutionPlanExecutor = 战斗执行计划执行器
// Executor = 执行器，负责按计划逐项执行。
// 注意：当前这个类还有临时直接扣血逻辑，后续会再和 BattleResolver 统一。
public static class BattleExecutionPlanExecutor
{
    // MaxRespondedClashAttempts = 已响应敌人意图的最大连续拼点次数
    // 如果双方一直平局，最多尝试 10 次，避免死循环。
    const int MaxRespondedClashAttempts = 10;

    // ExecuteExecutionPlan = BattleActionSlot.cs
    // Execute = 执行，ExecutionPlan = 执行计划。
    public static void ExecuteExecutionPlan(BattleExecutionPlan plan)
    {
        Debug.Log("===== BattleExecutionPlan 正式执行开始 =====");
        Debug.Log("提示：第一版只正式执行 RespondedEnemyIntent 攻击卡 vs 攻击卡，以及 UnrespondedEnemyIntent，不调用 BattleResolver，不触发事件链");

        if (plan == null || plan.executionItems == null || plan.executionItems.Count == 0)
        {
            Debug.Log("当前 BattleExecutionPlan 没有可执行项");
            return;
        }

        bool allItemsCompleted = true;

        foreach (BattleExecutionItem item in plan.executionItems)
        {
            if (item == null)
            {
                Debug.LogWarning("执行计划项为空，跳过");
                allItemsCompleted = false;
                continue;
            }

            if (item.isCompleted)
            {
                Debug.Log(item.order + ". 执行项已完成，跳过重复执行");
                continue;
            }

            // 无人响应敌人意图：敌人攻击按 actualTarget 直接处理。
            if (item.executionType == BattleExecutionItemType.UnrespondedEnemyIntent)
            {
                bool isCompleted = ExecuteUnrespondedEnemyIntent(item);

                if (!isCompleted)
                {
                    allItemsCompleted = false;
                }

                continue;
            }

            // 已响应敌人意图：玩家槽位和敌人卡牌进行第一版简化拼点。
            if (item.executionType == BattleExecutionItemType.RespondedEnemyIntent)
            {
                bool isCompleted = ExecuteRespondedEnemyIntent(item);

                if (!isCompleted)
                {
                    allItemsCompleted = false;
                }

                continue;
            }

            // 自由行动：当前还没有在执行计划里正式结算。
            if (item.executionType == BattleExecutionItemType.FreeAction)
            {
                Debug.Log(item.order + ". FreeAction：暂未实现正式普通行动结算，本项保持未完成");
                allItemsCompleted = false;
            }
        }

        if (allItemsCompleted)
        {
            plan.isCompleted = true;
            Debug.Log("BattleExecutionPlan 已全部完成");
            return;
        }

        Debug.Log("当前仍有未完成执行项");
    }

    // PrintExecutionPlanStepPreview = 打印执行步骤预览
    // Preview = 预览，只看顺序，不应该改变战斗状态。
    public static void PrintExecutionPlanStepPreview(BattleExecutionPlan executionPlan)
    {
        Debug.Log("===== BattleExecutionPlan 执行步骤预览 =====");
        Debug.Log("提示：当前只预览执行步骤，不执行任何 item，不修改任何状态");

        if (executionPlan == null || executionPlan.executionItems == null || executionPlan.executionItems.Count == 0)
        {
            Debug.Log("当前 BattleExecutionPlan 没有可预览的执行步骤");
            return;
        }

        int previewCount = 0;

        foreach (BattleExecutionItem item in executionPlan.executionItems)
        {
            if (item == null)
            {
                continue;
            }

            previewCount++;

            if (item.executionType == BattleExecutionItemType.RespondedEnemyIntent)
            {
                PrintRespondedEnemyIntentStepPreview(item);
                continue;
            }

            if (item.executionType == BattleExecutionItemType.UnrespondedEnemyIntent)
            {
                PrintUnrespondedEnemyIntentStepPreview(item);
                continue;
            }

            if (item.executionType == BattleExecutionItemType.FreeAction)
            {
                Debug.Log(item.order + ". FreeAction：未来将处理普通行动 / 偷刀，但当前暂未实现正式处理");
            }
        }

        if (previewCount == 0)
        {
            Debug.Log("当前 BattleExecutionPlan 没有可预览的执行步骤");
        }
    }

    // ExecuteUnrespondedEnemyIntent = 执行无人响应的敌人意图
    // 第一版逻辑：敌人 Roll 攻击点数，然后直接对 actualTargetCharacter 造成同等伤害。
    // 注意：这是临时直接结算，没有接入 BattleResolver 的完整伤害 / 事件链。
    static bool ExecuteUnrespondedEnemyIntent(BattleExecutionItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("执行 UnrespondedEnemyIntent 失败：item 为空");
            return false;
        }

        BattleEnemyIntent enemyIntent = item.enemyIntent;

        // 下面这些判断都是数据完整性检查。
        // 缺少敌人意图、卡牌、实际目标时，就不能安全执行。
        if (enemyIntent == null)
        {
            Debug.LogWarning(item.order + ". UnrespondedEnemyIntent 执行失败：敌人意图为空");
            return false;
        }

        if (enemyIntent.enemyCardState == null)
        {
            Debug.LogWarning(item.order + ". UnrespondedEnemyIntent 执行失败：敌人卡牌状态为空");
            return false;
        }

        if (enemyIntent.enemyCardState.cardData == null)
        {
            Debug.LogWarning(item.order + ". UnrespondedEnemyIntent 执行失败：敌人卡牌数据为空");
            return false;
        }

        if (enemyIntent.actualTargetCharacter == null)
        {
            Debug.LogWarning(item.order + ". UnrespondedEnemyIntent 执行失败：实际目标角色为空");
            return false;
        }

        if (enemyIntent.actualTargetSlotIndex <= 0)
        {
            Debug.LogWarning(
                item.order +
                ". UnrespondedEnemyIntent 执行失败：实际目标槽位无效：" +
                enemyIntent.actualTargetSlotIndex
            );
            return false;
        }

        // 从敌人卡牌数据里读取点数范围。
        int minPoint = enemyIntent.enemyCardState.cardData.minPoint;
        int maxPoint = enemyIntent.enemyCardState.cardData.maxPoint;

        if (minPoint < 0 || maxPoint < 0 || maxPoint < minPoint)
        {
            Debug.LogWarning(
                item.order +
                ". UnrespondedEnemyIntent 执行失败：敌人攻击点数范围异常：" +
                minPoint +
                "-" +
                maxPoint
            );
            return false;
        }

        // enemyAttackPoint = 敌人攻击点数
        // 当前无人响应时仍然会 Roll 敌人卡牌点数。
        int enemyAttackPoint = BattleCalculator.Rollpoint(minPoint, maxPoint);

        // damage = 造成伤害
        // 第一版暂定：伤害直接等于敌人攻击点数。
        int damage = enemyAttackPoint;

        // TakeDamage = 受到伤害。
        // 注意：这里是临时直接扣血，没有走完整 BattleResolver。
        enemyIntent.actualTargetCharacter.TakeDamage(damage);

        // 当前执行项已经处理完，标记完成。
        item.isCompleted = true;

        Debug.Log(
            item.order +
            ". UnrespondedEnemyIntent 已正式执行\n" +
            "   敌人意图：敌人意图" + enemyIntent.intentOrder + "\n" +
            "   敌人卡牌：" + enemyIntent.GetCardName() + "\n" +
            "   命中角色：" + enemyIntent.GetActualTargetName() + "\n" +
            "   命中槽位：槽位" + enemyIntent.actualTargetSlotIndex + "\n" +
            "   敌人攻击点数：" + enemyAttackPoint + "\n" +
            "   造成伤害：" + damage + "\n" +
            "   第一版不触发事件链，不处理敌人卡牌 CD / UseCount"
        );

        return true;
    }

    // ExecuteRespondedEnemyIntent = 执行已响应的敌人意图
    // 第一版逻辑：只支持玩家攻击卡 vs 敌人攻击卡。
    // 双方 Roll 点比较，点数高的一方造成等于点数的伤害。
    static bool ExecuteRespondedEnemyIntent(BattleExecutionItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("执行 RespondedEnemyIntent 失败：item 为空");
            return false;
        }

        BattleActionSlot actionSlot = item.actionSlot;

        // actionSlot = 玩家行动槽位。
        // 已响应敌人意图必须有一个玩家槽位来处理。
        if (actionSlot == null)
        {
            Debug.LogWarning(item.order + ". RespondedEnemyIntent 执行失败：行动槽位为空");
            return false;
        }

        BattleEnemyIntent enemyIntent = item.enemyIntent;

        // enemyIntent = 敌人意图。
        // 它记录敌人卡牌、敌人、原目标、实际目标等信息。
        if (enemyIntent == null)
        {
            Debug.LogWarning(item.order + ". RespondedEnemyIntent 执行失败：敌人意图为空");
            return false;
        }

        if (actionSlot.actor == null)
        {
            Debug.LogWarning(item.order + ". RespondedEnemyIntent 执行失败：玩家行动者为空");
            return false;
        }

        if (actionSlot.cardState == null)
        {
            Debug.LogWarning(item.order + ". RespondedEnemyIntent 执行失败：玩家卡牌状态为空");
            return false;
        }

        if (actionSlot.cardState.cardData == null)
        {
            Debug.LogWarning(item.order + ". RespondedEnemyIntent 执行失败：玩家卡牌数据为空");
            return false;
        }

        if (enemyIntent.enemy == null)
        {
            Debug.LogWarning(item.order + ". RespondedEnemyIntent 执行失败：敌人为空");
            return false;
        }

        if (enemyIntent.enemyCardState == null)
        {
            Debug.LogWarning(item.order + ". RespondedEnemyIntent 执行失败：敌人卡牌状态为空");
            return false;
        }

        if (enemyIntent.enemyCardState.cardData == null)
        {
            Debug.LogWarning(item.order + ". RespondedEnemyIntent 执行失败：敌人卡牌数据为空");
            return false;
        }

        if (enemyIntent.actualTargetCharacter == null)
        {
            Debug.LogWarning(item.order + ". RespondedEnemyIntent 执行失败：实际目标角色为空");
            return false;
        }

        // 第一版只处理攻击卡对攻击卡。
        // 防御、闪避、能力卡还没有接入这里的正式结算。
        if (actionSlot.cardState.cardData.cardType != "Attack")
        {
            Debug.LogWarning(item.order + ". RespondedEnemyIntent 执行失败：第一版只支持玩家攻击卡，当前卡牌类型：" + actionSlot.cardState.cardData.cardType);
            return false;
        }

        if (enemyIntent.enemyCardState.cardData.cardType != "Attack")
        {
            Debug.LogWarning(item.order + ". RespondedEnemyIntent 执行失败：第一版只支持敌人攻击卡，当前卡牌类型：" + enemyIntent.enemyCardState.cardData.cardType);
            return false;
        }

        int playerMinPoint = actionSlot.cardState.cardData.minPoint;
        int playerMaxPoint = actionSlot.cardState.cardData.maxPoint;
        int enemyMinPoint = enemyIntent.enemyCardState.cardData.minPoint;
        int enemyMaxPoint = enemyIntent.enemyCardState.cardData.maxPoint;

        // 检查双方卡牌点数范围是否有效。
        // 例如 minPoint 不能小于 0，maxPoint 不能小于 minPoint。
        if (IsInvalidPointRange(playerMinPoint, playerMaxPoint))
        {
            Debug.LogWarning(
                item.order +
                ". RespondedEnemyIntent 执行失败：玩家卡牌点数范围异常：" +
                playerMinPoint +
                "-" +
                playerMaxPoint
            );
            return false;
        }

        if (IsInvalidPointRange(enemyMinPoint, enemyMaxPoint))
        {
            Debug.LogWarning(
                item.order +
                ". RespondedEnemyIntent 执行失败：敌人卡牌点数范围异常：" +
                enemyMinPoint +
                "-" +
                enemyMaxPoint
            );
            return false;
        }

        // 最多拼点 MaxRespondedClashAttempts 次。
        // 平局继续，超过上限自动结束。
        for (int attempt = 1; attempt <= MaxRespondedClashAttempts; attempt++)
        {
            // 当前第一版直接 Roll 原始点数范围。
            // 这里还没有接入 GetFinalClashPoint 的 Buff 修正。
            int playerAttackPoint = BattleCalculator.Rollpoint(playerMinPoint, playerMaxPoint);
            int enemyAttackPoint = BattleCalculator.Rollpoint(enemyMinPoint, enemyMaxPoint);

            if (playerAttackPoint == enemyAttackPoint)
            {
                Debug.Log(
                    "第" +
                    attempt +
                    "次拼点：玩家点数 " +
                    playerAttackPoint +
                    "，敌人点数 " +
                    enemyAttackPoint +
                    "，平局，继续拼点"
                );
                continue;
            }

            // 点数高的一方胜利。
            bool isPlayerWin = playerAttackPoint > enemyAttackPoint;

            // 第一版暂定：伤害等于胜利方点数。
            int damage = isPlayerWin ? playerAttackPoint : enemyAttackPoint;

            // 玩家胜利时敌人受伤；敌人胜利时实际目标角色受伤。
            CharacterData damagedCharacter = isPlayerWin ? enemyIntent.enemy : enemyIntent.actualTargetCharacter;
            string resultText = isPlayerWin ? "玩家胜利" : "敌人胜利";

            damagedCharacter.TakeDamage(damage);
            item.isCompleted = true;

            Debug.Log(
                item.order +
                ". RespondedEnemyIntent 已正式执行\n" +
                "   执行项编号：" + item.order + "\n" +
                "   玩家角色：" + actionSlot.actor.characterName + "\n" +
                "   玩家卡牌：" + actionSlot.cardState.GetCardName() + "\n" +
                "   敌人：" + enemyIntent.GetEnemyName() + "\n" +
                "   敌人卡牌：" + enemyIntent.GetCardName() + "\n" +
                "   响应敌人意图：敌人意图" + enemyIntent.intentOrder + "\n" +
                "   玩家点数：" + playerAttackPoint + "\n" +
                "   敌人点数：" + enemyAttackPoint + "\n" +
                "   胜负结果：" + resultText + "\n" +
                "   受伤角色：" + damagedCharacter.characterName + "\n" +
                "   造成伤害：" + damage + "\n" +
                "   第一版不触发事件链，不处理 CD / UseCount / Buff"
            );

            return true;
        }

        item.isCompleted = true;

        Debug.Log(
            item.order +
            ". RespondedEnemyIntent 自动结束\n" +
            "   已连续拼点 " + MaxRespondedClashAttempts + " 次仍未分出胜负\n" +
            "   本次 RespondedEnemyIntent 自动结束\n" +
            "   双方都不造成伤害\n" +
            "   第一版视作双方都没有成功使用这张卡\n" +
            "   当前执行项标记完成，避免 ExecutionPlan 卡住"
        );

        return true;
    }

    // IsInvalidPointRange = 判断点数范围是否无效
    // minPoint = 最小点数，maxPoint = 最大点数。
    static bool IsInvalidPointRange(int minPoint, int maxPoint)
    {
        return minPoint < 0 || maxPoint < 0 || maxPoint < minPoint;
    }

    // PrintRespondedEnemyIntentStepPreview = 打印已响应敌人意图的步骤预览
    // 只打印未来会处理什么，不执行，不 Roll，不扣血。
    static void PrintRespondedEnemyIntentStepPreview(BattleExecutionItem item)
    {
        if (item.enemyIntent == null)
        {
            Debug.Log(item.order + ". RespondedEnemyIntent：敌人意图为空，无法预览响应处理");
            return;
        }

        if (item.actionSlot == null)
        {
            Debug.Log(
                item.order +
                ". RespondedEnemyIntent：未来应处理已响应敌人意图，但当前缺少绑定槽位，敌人意图：敌人意图" +
                item.enemyIntent.intentOrder
            );
            return;
        }

        Debug.Log(
            item.order +
            ". RespondedEnemyIntent：未来将处理玩家槽位对敌人意图的响应，槽位：" +
            item.actionSlot.GetActorName() +
            " 槽位" +
            item.actionSlot.slotIndex +
            "，敌人意图：敌人意图" +
            item.enemyIntent.intentOrder
        );
    }

    // PrintUnrespondedEnemyIntentStepPreview = 打印无人响应敌人意图的步骤预览
    // 只预览敌人卡牌、点数范围、命中角色和命中槽位。
    static void PrintUnrespondedEnemyIntentStepPreview(BattleExecutionItem item)
    {
        if (item == null)
        {
            Debug.Log("UnrespondedEnemyIntent：执行步骤预览失败，item 为空");
            return;
        }

        if (item.enemyIntent == null)
        {
            Debug.Log(item.order + ". UnrespondedEnemyIntent：敌人意图为空，无法预览无人响应处理");
            return;
        }

        string targetCharacterName = item.enemyIntent.GetActualTargetName();
        string targetSlotText = item.enemyIntent.actualTargetSlotIndex > 0
            ? "槽位" + item.enemyIntent.actualTargetSlotIndex
            : "槽位无效(" + item.enemyIntent.actualTargetSlotIndex + ")";
        string enemyAttackPointRangeText = GetEnemyAttackPointRangeText(item.enemyIntent);

        Debug.Log(
            item.order +
            ". UnrespondedEnemyIntent：未来将处理无人响应敌人意图\n" +
            "   敌人意图：敌人意图" + item.enemyIntent.intentOrder + "\n" +
            "   敌人卡牌：" + item.enemyIntent.GetCardName() + "\n" +
            "   " + enemyAttackPointRangeText + "\n" +
            "   将命中角色：" + targetCharacterName + "\n" +
            "   将命中槽位：" + targetSlotText + "\n" +
            "   当前仅预览点数范围和命中目标，不 roll 点数，不造成伤害"
        );
    }

    // GetEnemyAttackPointRangeText = 获取敌人攻击点数范围文本
    // enemyIntent = 敌人意图，里面保存敌人卡牌状态。
    static string GetEnemyAttackPointRangeText(BattleEnemyIntent enemyIntent)
    {
        if (enemyIntent == null || enemyIntent.enemyCardState == null)
        {
            return "敌人攻击点数范围：未知（敌人卡牌状态为空）";
        }

        if (enemyIntent.enemyCardState.cardData == null)
        {
            return "敌人攻击点数范围：未知（敌人卡牌数据为空）";
        }

        int minPoint = enemyIntent.enemyCardState.cardData.minPoint;
        int maxPoint = enemyIntent.enemyCardState.cardData.maxPoint;

        if (minPoint < 0 || maxPoint < 0 || maxPoint < minPoint)
        {
            return "敌人攻击点数范围：点数范围异常：" + minPoint + "-" + maxPoint;
        }

        return "敌人攻击点数范围：" + minPoint + "-" + maxPoint;
    }
}
