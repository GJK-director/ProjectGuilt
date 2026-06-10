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
        ExecuteExecutionPlanInternal(plan, null);
    }

    public static void ExecuteExecutionPlan(BattleExecutionPlan plan, BattleRuntimeState runtimeState)
    {
        ExecuteExecutionPlanInternal(plan, runtimeState);
    }

    static void ExecuteExecutionPlanInternal(BattleExecutionPlan plan, BattleRuntimeState runtimeState)
    {
        Debug.Log("===== BattleExecutionPlan 正式执行开始 =====");
        Debug.Log("提示：RespondedEnemyIntent / UnrespondedEnemyIntent / FreeAction 已交给 BattleResolver 正式入口处理");

        if (plan == null || plan.executionItems == null || plan.executionItems.Count == 0)
        {
            Debug.Log("当前 BattleExecutionPlan 没有可执行项");
            return;
        }

        bool allItemsCompleted = true;

        for (int i = 0; i < plan.executionItems.Count; i++)
        {
            if (runtimeState != null && runtimeState.IsBattleEnded)
            {
                Debug.Log("战斗已经结束，Executor 拒绝继续执行 BattleExecutionPlan");
                MarkRemainingItemsCompletedBecauseBattleEnded(plan, i);
                allItemsCompleted = true;
                break;
            }

            BattleExecutionItem item = plan.executionItems[i];

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

            bool isCompleted = false;

            // 无人响应敌人意图：敌人攻击按 actualTarget 直接处理。
            if (item.executionType == BattleExecutionItemType.UnrespondedEnemyIntent)
            {
                isCompleted = ExecuteUnrespondedEnemyIntent(item);
            }
            else if (item.executionType == BattleExecutionItemType.RespondedEnemyIntent)
            {
                // 已响应敌人意图：交给 BattleResolver 正式入口处理。
                isCompleted = ExecuteRespondedEnemyIntent(item);
            }
            else if (item.executionType == BattleExecutionItemType.FreeAction)
            {
                // 自由行动：交给 BattleResolver 正式入口处理。
                isCompleted = ExecuteFreeAction(item);
            }

            if (!isCompleted)
            {
                allItemsCompleted = false;
                continue;
            }

            if (runtimeState != null)
            {
                runtimeState.EvaluateBattleEnd();

                if (runtimeState.IsBattleEnded)
                {
                    MarkRemainingItemsCompletedBecauseBattleEnded(plan, i + 1);
                    allItemsCompleted = true;
                    break;
                }
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

    static void MarkRemainingItemsCompletedBecauseBattleEnded(BattleExecutionPlan plan, int startIndex)
    {
        if (plan == null || plan.executionItems == null)
        {
            return;
        }

        for (int i = startIndex; i < plan.executionItems.Count; i++)
        {
            BattleExecutionItem item = plan.executionItems[i];

            if (item == null || item.isCompleted)
            {
                continue;
            }

            item.isCompleted = true;
            Debug.Log(item.order + ". 因 BattleEnded 跳过");
        }
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
                PrintFreeActionStepPreview(item);
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

        if (TryCompleteEnemyItemBecauseActualTargetDead(item))
        {
            return true;
        }

        BattleActionSlot passiveGuardSlot = FindFirstValidPassiveGuardSlot(item);
        BattleResolveResult result = null;

        if (passiveGuardSlot != null)
        {
            Debug.Log(
                item.order +
                ". UnrespondedEnemyIntent：被动守备接管，使用 " +
                passiveGuardSlot.GetDisplaySlotName() +
                " / " +
                passiveGuardSlot.GetCardName()
            );

            result = BattleResolver.ResolveRespondedEnemyIntent(passiveGuardSlot, item.enemyIntent);

            LogResolveResult(item.order, "PassiveGuard Resolver 结算结果", result);

            if (result == null || !result.isSuccess || !result.shouldCompleteItem)
            {
                Debug.LogWarning(
                    item.order +
                    ". UnrespondedEnemyIntent 被动守备未完成：Resolver 未返回可完成结果，Executor 不补做结算"
                );

                return false;
            }

            if (result.playerCardUsed)
            {
                passiveGuardSlot.MarkUsed();
                Debug.Log(item.order + ". UnrespondedEnemyIntent：被动守备槽位已标记为已使用");
            }

            item.isCompleted = true;
            return true;
        }

        result = BattleResolver.ResolveUnrespondedEnemyIntent(item.enemyIntent);

        LogResolveResult(item.order, "UnrespondedEnemyIntent Resolver 结算结果", result);

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

    // FindFirstValidPassiveGuardSlot = 执行时选择第一张仍然有效的被动守备
    static BattleActionSlot FindFirstValidPassiveGuardSlot(BattleExecutionItem item)
    {
        if (item == null || item.passiveGuardCandidates == null || item.passiveGuardCandidates.Count == 0)
        {
            return null;
        }

        foreach (BattleActionSlot slot in item.passiveGuardCandidates)
        {
            if (!IsPassiveGuardSlotStillValid(slot, item.enemyIntent))
            {
                continue;
            }

            return slot;
        }

        return null;
    }

    // IsPassiveGuardSlotStillValid = 判断候选守备在执行时是否仍然可用
    static bool IsPassiveGuardSlotStillValid(BattleActionSlot slot, BattleEnemyIntent enemyIntent)
    {
        if (slot == null || enemyIntent == null || enemyIntent.actualTargetCharacter == null)
        {
            return false;
        }

        if (enemyIntent.actualTargetCharacter.IsDead())
        {
            return false;
        }

        if (slot.IsEmpty())
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

        if (slot.owner == null || slot.actor == null || slot.target == null)
        {
            return false;
        }

        if (slot.owner.IsDead() || slot.actor.IsDead() || slot.target.IsDead())
        {
            return false;
        }

        if (slot.cardState == null || slot.cardState.cardData == null)
        {
            return false;
        }

        if (slot.cardState.cardData.cardType != CardType.Defense &&
            slot.cardState.cardData.cardType != CardType.Dodge)
        {
            return false;
        }

        if (!object.ReferenceEquals(slot.owner, enemyIntent.actualTargetCharacter) ||
            !object.ReferenceEquals(slot.actor, enemyIntent.actualTargetCharacter) ||
            !object.ReferenceEquals(slot.target, enemyIntent.actualTargetCharacter))
        {
            return false;
        }

        return BattleCardManager.CanUseCard(slot.cardState);
    }

    // LogResolveResult = 打印 Resolver 返回结果
    static void LogResolveResult(int order, string title, BattleResolveResult result)
    {
        Debug.Log(
            order +
            ". " +
            title +
            "\n" +
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
    }

    // PrintFreeActionStepPreview = 打印自由行动执行步骤预览
    static void PrintFreeActionStepPreview(BattleExecutionItem item)
    {
        if (item == null)
        {
            Debug.Log("FreeAction：执行步骤预览失败，item 为空");
            return;
        }

        if (item.actionSlot == null)
        {
            Debug.Log(
                item.order +
                ". FreeAction：执行时将调用 BattleResolver.ResolveFreeAction(...)，但当前缺少行动槽位"
            );
            return;
        }

        Debug.Log(
            item.order +
            ". FreeAction：执行时将调用 BattleResolver.ResolveFreeAction(...)，当前支持 Ability FreeAction 与 Attack FreeAction\n" +
            "   行动者：" + item.actionSlot.GetActorName() + "\n" +
            "   槽位：槽位" + item.actionSlot.slotIndex + "\n" +
            "   卡牌：" + item.actionSlot.GetCardName() + "\n" +
            "   目标：" + item.actionSlot.GetTargetName() + "\n" +
            "   当前只预览执行方式，不执行 item，不修改状态"
        );
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

        if (TryCompleteEnemyItemBecauseActualTargetDead(item))
        {
            return true;
        }

        if (item.actionSlot != null &&
            item.actionSlot.actor != null &&
            item.actionSlot.actor.IsDead())
        {
            Debug.Log(
                item.order +
                ". 响应角色已死亡，原响应失效，敌人意图转为Unresponded处理"
            );

            return ExecuteUnrespondedEnemyIntent(item);
        }

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            item.actionSlot,
            item.enemyIntent,
            item.passiveGuardCandidates
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

        if (result.triggeredPassiveGuardSlot != null &&
            !object.ReferenceEquals(result.triggeredPassiveGuardSlot, item.actionSlot) &&
            !result.triggeredPassiveGuardSlot.isUsed)
        {
            result.triggeredPassiveGuardSlot.MarkUsed();
            Debug.Log(
                item.order +
                ". RespondedEnemyIntent：触发的 PassiveGuard 槽位已标记为已使用：" +
                result.triggeredPassiveGuardSlot.GetDisplaySlotName()
            );
        }

        item.isCompleted = true;
        return true;
    }

    // ExecuteFreeAction = 执行自由行动
    // Executor 只负责分派和完成状态，正式结算交给 BattleResolver。
    static bool ExecuteFreeAction(BattleExecutionItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("执行 FreeAction 失败：item 为空");
            return false;
        }

        if (item.actionSlot != null &&
            item.actionSlot.actor != null &&
            item.actionSlot.actor.IsDead())
        {
            Debug.Log(item.order + ". FreeAction角色已死亡，本次行动跳过");
            item.isCompleted = true;
            return true;
        }

        BattleResolveResult result = BattleResolver.ResolveFreeAction(item.actionSlot);

        Debug.Log(
            item.order +
            ". FreeAction Resolver 结算结果\n" +
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

        if (result == null || !result.shouldCompleteItem)
        {
            Debug.LogWarning(
                item.order +
                ". FreeAction 未完成：Resolver 未返回可完成结果，Executor 不补做结算"
            );

            return false;
        }

        if (!result.isSuccess)
        {
            Debug.LogWarning(
                item.order +
                ". FreeAction执行时不可用，本次行动按跳过完成：" +
                result.message
            );
        }

        if (result.playerCardUsed && item.actionSlot != null)
        {
            item.actionSlot.MarkUsed();
            Debug.Log(item.order + ". FreeAction：玩家行动槽位已标记为已使用");
        }

        item.isCompleted = true;
        return true;
    }

    static bool TryCompleteEnemyItemBecauseActualTargetDead(BattleExecutionItem item)
    {
        if (item == null || item.enemyIntent == null || item.enemyIntent.actualTargetCharacter == null)
        {
            return false;
        }

        if (!item.enemyIntent.actualTargetCharacter.IsDead())
        {
            return false;
        }

        Debug.Log(item.order + ". 敌人意图实际目标已死亡，本次敌人行动跳过");
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
        int passiveGuardCandidateCount = item.passiveGuardCandidates != null
            ? item.passiveGuardCandidates.Count
            : 0;

        Debug.Log(
            item.order +
            ". UnrespondedEnemyIntent：未来将处理无人响应敌人意图\n" +
            "   敌人意图：敌人意图" + item.enemyIntent.intentOrder + "\n" +
            "   敌人卡牌：" + item.enemyIntent.GetCardName() + "\n" +
            "   " + enemyAttackPointRangeText + "\n" +
            "   将命中角色：" + targetCharacterName + "\n" +
            "   将命中槽位：" + targetSlotText + "\n" +
            "   被动守备候选数：" + passiveGuardCandidateCount + "\n" +
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
