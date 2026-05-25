using UnityEngine;

public static class BattleExecutionPlanExecutor
{
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

        Debug.Log(
            item.order +
            ". UnrespondedEnemyIntent：未来将处理无人响应敌人意图\n" +
            "   敌人意图：敌人意图" + item.enemyIntent.intentOrder + "\n" +
            "   敌人卡牌：" + item.enemyIntent.GetCardName() + "\n" +
            "   将命中角色：" + targetCharacterName + "\n" +
            "   将命中槽位：" + targetSlotText + "\n" +
            "   当前仅预览命中目标，不造成伤害"
        );
    }
}
