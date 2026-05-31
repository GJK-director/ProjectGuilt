// 脚本中文说明：战斗执行计划执行器。负责按执行计划逐项处理已响应敌人意图和无人响应敌人意图。
using UnityEngine;

// BattleExecutionPlanExecutor = 战斗执行计划执行器
// Executor = 执行器，负责按计划逐项执行。
// 注意：Executor 只负责按计划分派和标记完成，具体结算逐步交给 BattleResolver。
public static class BattleExecutionPlanExecutor
{
    // ExecuteExecutionPlan = BattleActionSlot.cs
    // Execute = 执行，ExecutionPlan = 执行计划。
    public static void ExecuteExecutionPlan(BattleExecutionPlan plan)
    {
        Debug.Log("===== BattleExecutionPlan 正式执行开始 =====");
        Debug.Log("提示：RespondedEnemyIntent / UnrespondedEnemyIntent 已交给 BattleResolver 正式入口处理");

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

            // 已响应敌人意图：交给 BattleResolver 正式入口处理。
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
    // Executor 只负责分派和完成状态，正式结算交给 BattleResolver。
    static bool ExecuteUnrespondedEnemyIntent(BattleExecutionItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("执行 UnrespondedEnemyIntent 失败：item 为空");
            return false;
        }

        BattleResolveResult result = BattleResolver.ResolveUnrespondedEnemyIntent(item.enemyIntent);

        Debug.Log(
            item.order +
            ". UnrespondedEnemyIntent Resolver 结算结果\n" +
            "   resultType：" + (result != null ? result.resultType : "无") + "\n" +
            "   isSuccess：" + (result != null && result.isSuccess) + "\n" +
            "   shouldCompleteItem：" + (result != null && result.shouldCompleteItem) + "\n" +
            "   playerCardUsed：" + (result != null && result.playerCardUsed) + "\n" +
            "   enemyCardUsed：" + (result != null && result.enemyCardUsed) + "\n" +
            "   hasDamage：" + (result != null && result.hasDamage) + "\n" +
            "   damage：" + (result != null ? result.damage : 0) + "\n" +
            "   triggeredEventChain：" + (result != null && result.triggeredEventChain) + "\n" +
            "   message：" + (result != null ? result.message : "BattleResolveResult 为空")
        );

        if (result == null || !result.isSuccess || !result.shouldCompleteItem)
        {
            Debug.LogWarning(
                item.order +
                ". UnrespondedEnemyIntent 未完成：Resolver 未返回可完成结果，Executor 不补做结算"
            );

            return false;
        }

        item.isCompleted = true;
        return true;
    }

    // ExecuteRespondedEnemyIntent = 执行已响应的敌人意图
    // Executor 只负责分派和完成状态，正式结算交给 BattleResolver。
    static bool ExecuteRespondedEnemyIntent(BattleExecutionItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("执行 RespondedEnemyIntent 失败：item 为空");
            return false;
        }

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            item.actionSlot,
            item.enemyIntent
        );

        if (result == null)
        {
            Debug.LogWarning(item.order + ". RespondedEnemyIntent 执行失败：BattleResolveResult 为空");
            return false;
        }

        Debug.Log(
            item.order +
            ". RespondedEnemyIntent Resolver 结算结果\n" +
            "   resultType：" + result.resultType + "\n" +
            "   isSuccess：" + result.isSuccess + "\n" +
            "   shouldCompleteItem：" + result.shouldCompleteItem + "\n" +
            "   playerCardUsed：" + result.playerCardUsed + "\n" +
            "   enemyCardUsed：" + result.enemyCardUsed + "\n" +
            "   hasDamage：" + result.hasDamage + "\n" +
            "   damage：" + result.damage + "\n" +
            "   triggeredEventChain：" + result.triggeredEventChain + "\n" +
            "   message：" + result.message
        );

        if (!result.isSuccess || !result.shouldCompleteItem)
        {
            Debug.LogWarning(
                item.order +
                ". RespondedEnemyIntent 未完成：Resolver 未返回可完成结果，Executor 不补做结算"
            );

            return false;
        }

        if (result.playerCardUsed && item.actionSlot != null)
        {
            item.actionSlot.MarkUsed();
            Debug.Log(item.order + ". RespondedEnemyIntent：玩家行动槽位已标记为已使用");
        }

        item.isCompleted = true;
        return true;
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
