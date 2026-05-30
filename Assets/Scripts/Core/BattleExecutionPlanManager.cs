// 脚本中文说明：战斗执行计划管理器。负责根据敌人意图和行动槽位生成执行计划，并打印计划内容。
using System.Collections.Generic;
using UnityEngine;

// BattleExecutionPlanManager = 战斗执行计划管理器
// Manager = 管理器，这里负责创建和打印执行计划，不负责执行计划。
public static class BattleExecutionPlanManager
{
    // CreateBasicExecutionPlan = 创建基础执行计划
    // Create = 创建，Basic = 基础，ExecutionPlan = 执行计划。
    public static BattleExecutionPlan CreateBasicExecutionPlan(
        // actionSlots = 玩家行动槽位列表
        // BattleActionSlot = 战斗行动槽位，记录玩家安排了哪张卡、是否响应敌人意图。
        List<BattleActionSlot> actionSlots,

        // intentQueue = 敌人意图队列
        // BattleEnemyIntent = 战斗敌人意图，记录敌人要攻击的角色和槽位。
        List<BattleEnemyIntent> intentQueue
    )
    {
        // 先创建一个空执行计划，后面再逐条加入执行项。
        BattleExecutionPlan executionPlan = new BattleExecutionPlan();

        // 没有敌人意图时，执行计划保持为空。
        if (intentQueue == null || intentQueue.Count == 0)
        {
            return executionPlan;
        }

        // order = 执行顺序编号
        // 当前从 1 开始，数字越小越先执行。
        int order = 1;

        // 第一轮：先把“已经被玩家响应”的敌人意图加入执行计划。
        // 当前设计倾向是：响应敌人意图优先进入执行队列。
        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent != null && intent.isResponded)
            {
                // 根据敌人意图，找到绑定这个意图的玩家行动槽位。
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

        // 第二轮：再把“无人响应”的敌人意图加入执行计划。
        // 这样无人响应攻击会排在已响应意图之后。
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

    // PrintExecutionPlan = 打印执行计划
    // Print = 打印，方便在 Console 里确认顺序。
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

            // 已响应敌人意图：打印玩家槽位如何处理这个敌人意图。
            if (item.executionType == BattleExecutionItemType.RespondedEnemyIntent)
            {
                PrintRespondedEnemyIntentItem(item);
                continue;
            }

            // 无人响应敌人意图：打印敌人意图未来会按实际目标执行。
            if (item.executionType == BattleExecutionItemType.UnrespondedEnemyIntent)
            {
                PrintUnrespondedEnemyIntentItem(item);
                continue;
            }

            // 自由行动：当前还没有接入完整执行计划打印。
            if (item.executionType == BattleExecutionItemType.FreeAction)
            {
                Debug.Log(item.order + ". FreeAction：当前暂未实现打印细节");
            }
        }

        Debug.Log("ExecutionPlan 项数量：" + executionPlan.executionItems.Count);
    }

    // PrintRespondedEnemyIntentItem = 打印已响应敌人意图执行项
    // item = 执行计划中的一条 BattleExecutionItem。
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

    // PrintUnrespondedEnemyIntentItem = 打印无人响应敌人意图执行项
    // item = 执行计划中的一条 BattleExecutionItem。
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

    // FindSlotByEnemyIntent = 根据敌人意图查找绑定的行动槽位
    // actionSlots = 所有玩家行动槽位。
    // enemyIntent = 要查找的敌人意图。
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
            // ReferenceEquals = 判断是不是同一个对象实例。
            // 这里不是比较内容相同，而是确认槽位绑定的 enemyIntent 就是传进来的那一个。
            if (slot != null && object.ReferenceEquals(slot.enemyIntent, enemyIntent))
            {
                return slot;
            }
        }

        return null;
    }
}
