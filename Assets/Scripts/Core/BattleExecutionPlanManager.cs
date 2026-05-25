using System.Collections.Generic;
using UnityEngine;

public static class BattleExecutionPlanManager
{
    public static BattleExecutionPlan CreateBasicExecutionPlan(
        List<BattleActionSlot> actionSlots,
        List<BattleEnemyIntent> intentQueue
    )
    {
        BattleExecutionPlan executionPlan = new BattleExecutionPlan();

        if (intentQueue == null || intentQueue.Count == 0)
        {
            return executionPlan;
        }

        int order = 1;

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent != null && intent.isResponded)
            {
                BattleActionSlot actionSlot = FindSlotByEnemyIntent(actionSlots, intent);

                executionPlan.AddItem(new BattleExecutionItem(
                    order,
                    BattleExecutionItemType.RespondedEnemyIntent,
                    intent,
                    actionSlot
                ));

                order++;
            }
        }

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent != null && !intent.isResponded)
            {
                executionPlan.AddItem(new BattleExecutionItem(
                    order,
                    BattleExecutionItemType.UnrespondedEnemyIntent,
                    intent,
                    null
                ));

                order++;
            }
        }

        return executionPlan;
    }

    public static void PrintExecutionPlan(BattleExecutionPlan executionPlan)
    {
        Debug.Log("===== 当前 BattleExecutionPlan =====");
        Debug.Log("提示：当前只生成并打印执行计划，不执行任何 item");

        if (executionPlan == null || executionPlan.executionItems == null || executionPlan.executionItems.Count == 0)
        {
            Debug.Log("当前 BattleExecutionPlan 没有执行项");
            Debug.Log("ExecutionPlan 项数量：0");
            return;
        }

        foreach (BattleExecutionItem item in executionPlan.executionItems)
        {
            if (item == null)
            {
                continue;
            }

            if (item.executionType == BattleExecutionItemType.RespondedEnemyIntent)
            {
                PrintRespondedEnemyIntentItem(item);
                continue;
            }

            if (item.executionType == BattleExecutionItemType.UnrespondedEnemyIntent)
            {
                PrintUnrespondedEnemyIntentItem(item);
                continue;
            }

            if (item.executionType == BattleExecutionItemType.FreeAction)
            {
                Debug.Log(item.order + ". FreeAction：当前暂未实现打印细节");
            }
        }

        Debug.Log("ExecutionPlan 项数量：" + executionPlan.executionItems.Count);
    }

    static void PrintRespondedEnemyIntentItem(BattleExecutionItem item)
    {
        if (item.enemyIntent == null)
        {
            Debug.Log(item.order + ". RespondedEnemyIntent：敌人意图为空");
            return;
        }

        if (item.actionSlot == null)
        {
            Debug.Log(
                item.order +
                ". RespondedEnemyIntent：敌人意图" +
                item.enemyIntent.intentOrder +
                " 已响应，但未找到绑定槽位，当前实际目标：" +
                item.enemyIntent.GetActualTargetSlotText()
            );
            return;
        }

        Debug.Log(
            item.order +
            ". RespondedEnemyIntent：" +
            item.actionSlot.GetActorName() +
            " 槽位" +
            item.actionSlot.slotIndex +
            " 处理 敌人意图" +
            item.enemyIntent.intentOrder +
            "，当前实际目标：" +
            item.enemyIntent.GetActualTargetSlotText()
        );
    }

    static void PrintUnrespondedEnemyIntentItem(BattleExecutionItem item)
    {
        if (item.enemyIntent == null)
        {
            Debug.Log(item.order + ". UnrespondedEnemyIntent：敌人意图为空");
            return;
        }

        Debug.Log(
            item.order +
            ". UnrespondedEnemyIntent：敌人意图" +
            item.enemyIntent.intentOrder +
            " 未响应，未来按 actualTarget 执行，目标：" +
            item.enemyIntent.GetActualTargetSlotText()
        );
    }

    static BattleActionSlot FindSlotByEnemyIntent(
        List<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent
    )
    {
        if (actionSlots == null || enemyIntent == null)
        {
            return null;
        }

        foreach (BattleActionSlot slot in actionSlots)
        {
            if (slot != null && object.ReferenceEquals(slot.enemyIntent, enemyIntent))
            {
                return slot;
            }
        }

        return null;
    }
}
