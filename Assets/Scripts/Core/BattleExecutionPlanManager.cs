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
                    actionSlot,
                    CollectRespondedPassiveGuardCandidates(actionSlots, intent, actionSlot)
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
                    null,
                    CollectPassiveGuardCandidates(actionSlots, intent)
                ));

                order++;
            }
        }

        return executionPlan;
    }

    // CreateSpeedBasedExecutionPlan = 创建速度规则版执行计划
    // 第一版规则：
    // 1. 高速玩家行动先处理；
    // 2. 敌人意图按 intentOrder 处理；
    // 3. 低速自由行动最后处理。
    public static BattleExecutionPlan CreateSpeedBasedExecutionPlan(
        List<BattleActionSlot> actionSlots,
        List<BattleEnemyIntent> intentQueue
    )
    {
        BattleExecutionPlan executionPlan = new BattleExecutionPlan();
        HashSet<BattleEnemyIntent> handledHighSpeedIntents = new HashSet<BattleEnemyIntent>();

        int order = 1;

        // 第一阶段：高速玩家行动阶段。
        // 按当前 actionSlots 顺序扫描，先加入能抢先的响应行动和自由行动。
        if (actionSlots != null)
        {
            foreach (BattleActionSlot slot in actionSlots)
            {
                if (!IsActionSlotReady(slot))
                {
                    continue;
                }

                if (IsHighSpeedResponseSlot(slot) && IsIntentInQueue(intentQueue, slot.enemyIntent))
                {
                    executionPlan.AddItem(new BattleExecutionItem(
                        order,
                        BattleExecutionItemType.RespondedEnemyIntent,
                        slot.enemyIntent,
                        slot,
                        CollectRespondedPassiveGuardCandidates(actionSlots, slot.enemyIntent, slot)
                    ));

                    handledHighSpeedIntents.Add(slot.enemyIntent);
                    order++;
                    continue;
                }

                if (IsHighSpeedFreeActionSlot(slot))
                {
                    executionPlan.AddItem(new BattleExecutionItem(
                        order,
                        BattleExecutionItemType.FreeAction,
                        null,
                        slot
                    ));

                    order++;
                }
            }
        }

        // 第二阶段：敌人意图节奏阶段。
        // 按 intentOrder 从小到大处理；已经被高速响应提前处理的意图跳过。
        List<BattleEnemyIntent> orderedIntents = GetIntentQueueByIntentOrder(intentQueue);

        foreach (BattleEnemyIntent intent in orderedIntents)
        {
            if (intent == null || handledHighSpeedIntents.Contains(intent))
            {
                continue;
            }

            if (intent.isResponded)
            {
                BattleActionSlot actionSlot = FindSlotByEnemyIntent(actionSlots, intent);

                executionPlan.AddItem(new BattleExecutionItem(
                    order,
                    BattleExecutionItemType.RespondedEnemyIntent,
                    intent,
                    actionSlot,
                    CollectRespondedPassiveGuardCandidates(actionSlots, intent, actionSlot)
                ));

                order++;
                continue;
            }

            executionPlan.AddItem(new BattleExecutionItem(
                order,
                BattleExecutionItemType.UnrespondedEnemyIntent,
                intent,
                null,
                CollectPassiveGuardCandidates(actionSlots, intent)
            ));

            order++;
        }

        // 第三阶段：低速自由行动阶段。
        // 低速 FreeAction 不能抢在敌人攻击前，所以最后加入。
        if (actionSlots != null)
        {
            foreach (BattleActionSlot slot in actionSlots)
            {
                if (!IsActionSlotReady(slot))
                {
                    continue;
                }

                if (slot.slotType == BattleActionSlotType.FreeAction && !IsHighSpeedFreeActionSlot(slot))
                {
                    executionPlan.AddItem(new BattleExecutionItem(
                        order,
                        BattleExecutionItemType.FreeAction,
                        null,
                        slot
                    ));

                    order++;
                }
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

            // 自由行动：打印玩家自由行动的基础信息。
            if (item.executionType == BattleExecutionItemType.FreeAction)
            {
                PrintFreeActionItem(item);
            }
        }

        Debug.Log("ExecutionPlan 项数量：" + executionPlan.executionItems.Count);
    }

    // PrintFreeActionItem = 打印自由行动执行项
    static void PrintFreeActionItem(BattleExecutionItem item)
    {
        if (item.actionSlot == null)
        {
            Debug.Log(
                item.order +
                ". FreeAction：玩家自由行动，执行时将交给 BattleResolver.ResolveFreeAction(...) 处理，但当前缺少行动槽位"
            );
            return;
        }

        Debug.Log(
            item.order +
            ". FreeAction：玩家自由行动，执行时将交给 BattleResolver.ResolveFreeAction(...) 处理，槽位：" +
            item.actionSlot.GetActorName() +
            " 槽位" +
            item.actionSlot.slotIndex +
            "，卡牌：" +
            item.actionSlot.GetCardName() +
            "，目标：" +
            item.actionSlot.GetTargetName()
        );
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

        int passiveGuardCandidateCount = item.passiveGuardCandidates != null
            ? item.passiveGuardCandidates.Count
            : 0;

        Debug.Log(
            item.order +
            ". RespondedEnemyIntent：" +
            item.actionSlot.GetActorName() +
            " 槽位" +
            item.actionSlot.slotIndex +
            " 处理 敌人意图" +
            item.enemyIntent.intentOrder +
            "，当前实际目标：" +
            item.enemyIntent.GetActualTargetSlotText() +
            "，被动守备候选数：" +
            passiveGuardCandidateCount
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

        int passiveGuardCandidateCount = item.passiveGuardCandidates != null
            ? item.passiveGuardCandidates.Count
            : 0;

        Debug.Log(
            item.order +
            ". UnrespondedEnemyIntent：敌人意图" +
            item.enemyIntent.intentOrder +
            " 未响应，未来按 actualTarget 执行，目标：" +
            item.enemyIntent.GetActualTargetSlotText() +
            "，被动守备候选数：" +
            passiveGuardCandidateCount
        );
    }

    // CollectRespondedPassiveGuardCandidates = 为 Attack vs Attack 已响应意图收集被动守备候选
    static List<BattleActionSlot> CollectRespondedPassiveGuardCandidates(
        List<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent,
        BattleActionSlot responseSlot
    )
    {
        if (!ShouldCollectPassiveGuardForRespondedItem(enemyIntent, responseSlot))
        {
            return new List<BattleActionSlot>();
        }

        return CollectPassiveGuardCandidates(actionSlots, enemyIntent);
    }

    // ShouldCollectPassiveGuardForRespondedItem = 只有 Attack vs Attack 响应才携带被动守备候选
    static bool ShouldCollectPassiveGuardForRespondedItem(
        BattleEnemyIntent enemyIntent,
        BattleActionSlot responseSlot
    )
    {
        if (enemyIntent == null || responseSlot == null)
        {
            return false;
        }

        if (responseSlot.slotType != BattleActionSlotType.RespondToEnemyIntent)
        {
            return false;
        }

        if (responseSlot.cardState == null || responseSlot.cardState.cardData == null)
        {
            return false;
        }

        if (enemyIntent.enemyCardState == null || enemyIntent.enemyCardState.cardData == null)
        {
            return false;
        }

        return responseSlot.cardState.cardData.cardType == CardType.Attack &&
            enemyIntent.enemyCardState.cardData.cardType == CardType.Attack;
    }

    // CollectPassiveGuardCandidates = 为敌人意图收集被动守备候选
    // 这里只保存候选引用，不在计划生成阶段最终决定使用哪一个。
    static List<BattleActionSlot> CollectPassiveGuardCandidates(
        List<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent
    )
    {
        List<BattleActionSlot> candidates = new List<BattleActionSlot>();

        if (actionSlots == null || enemyIntent == null || enemyIntent.actualTargetCharacter == null)
        {
            return candidates;
        }

        CharacterData target = enemyIntent.actualTargetCharacter;

        foreach (BattleActionSlot slot in actionSlots)
        {
            if (!IsPassiveGuardCandidateForTarget(slot, target))
            {
                continue;
            }

            candidates.Add(slot);
        }

        candidates.Sort(CompareActionSlotIndex);
        return candidates;
    }

    // IsPassiveGuardCandidateForTarget = 判断槽位是否是目标角色的被动守备候选
    static bool IsPassiveGuardCandidateForTarget(BattleActionSlot slot, CharacterData target)
    {
        if (slot == null || target == null)
        {
            return false;
        }

        if (slot.slotType != BattleActionSlotType.PassiveGuard)
        {
            return false;
        }

        if (slot.isUsed)
        {
            return false;
        }

        if (!object.ReferenceEquals(slot.owner, target) ||
            !object.ReferenceEquals(slot.actor, target) ||
            !object.ReferenceEquals(slot.target, target))
        {
            return false;
        }

        if (slot.cardState == null || slot.cardState.cardData == null)
        {
            return false;
        }

        return slot.cardState.cardData.cardType == CardType.Defense;
    }

    // CompareActionSlotIndex = 按角色内槽位编号升序排序
    static int CompareActionSlotIndex(BattleActionSlot left, BattleActionSlot right)
    {
        if (left == null && right == null)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        return left.slotIndex.CompareTo(right.slotIndex);
    }

    // IsActionSlotReady = 判断槽位是否有可加入计划的行动
    static bool IsActionSlotReady(BattleActionSlot slot)
    {
        return slot != null && !slot.IsEmpty() && slot.actor != null;
    }

    // IsHighSpeedResponseSlot = 判断响应槽位是否能高速抢先
    static bool IsHighSpeedResponseSlot(BattleActionSlot slot)
    {
        if (!IsActionSlotReady(slot))
        {
            return false;
        }

        if (slot.slotType != BattleActionSlotType.RespondToEnemyIntent)
        {
            return false;
        }

        if (slot.enemyIntent == null || slot.enemyIntent.enemy == null)
        {
            return false;
        }

        return IsActorFasterThan(slot.actor, slot.enemyIntent.enemy);
    }

    // IsHighSpeedFreeActionSlot = 判断自由行动是否能抢在目标前
    static bool IsHighSpeedFreeActionSlot(BattleActionSlot slot)
    {
        if (!IsActionSlotReady(slot))
        {
            return false;
        }

        if (slot.slotType != BattleActionSlotType.FreeAction)
        {
            return false;
        }

        if (slot.target == null)
        {
            return false;
        }

        return IsActorFasterThan(slot.actor, slot.target);
    }

    // IsActorFasterThan = 判断 actor 当前速度是否严格大于 target
    static bool IsActorFasterThan(CharacterData actor, CharacterData target)
    {
        if (actor == null || target == null)
        {
            return false;
        }

        return actor.GetCurrentSpeed() > target.GetCurrentSpeed();
    }

    // IsIntentInQueue = 判断敌人意图是否属于当前意图队列
    static bool IsIntentInQueue(List<BattleEnemyIntent> intentQueue, BattleEnemyIntent targetIntent)
    {
        if (intentQueue == null || targetIntent == null)
        {
            return false;
        }

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (object.ReferenceEquals(intent, targetIntent))
            {
                return true;
            }
        }

        return false;
    }

    // GetIntentQueueByIntentOrder = 按 intentOrder 获取敌人意图顺序
    static List<BattleEnemyIntent> GetIntentQueueByIntentOrder(List<BattleEnemyIntent> intentQueue)
    {
        List<BattleEnemyIntent> orderedIntents = new List<BattleEnemyIntent>();

        if (intentQueue == null)
        {
            return orderedIntents;
        }

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent != null)
            {
                orderedIntents.Add(intent);
            }
        }

        orderedIntents.Sort(CompareIntentOrder);
        return orderedIntents;
    }

    // CompareIntentOrder = 比较敌人意图顺序
    static int CompareIntentOrder(BattleEnemyIntent left, BattleEnemyIntent right)
    {
        if (left == null && right == null)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        return left.intentOrder.CompareTo(right.intentOrder);
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
