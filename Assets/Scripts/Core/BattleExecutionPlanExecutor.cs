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
                MarkRemainingItemsSkippedBecauseBattleEnded(plan, i);
                allItemsCompleted = true;
                break;
            }

            BattleExecutionItem item = plan.executionItems[i];

            if (item == null)
            {
                Debug.LogWarning("执行计划项为空，ExecutionPlan 失败并停止");
                allItemsCompleted = false;
                break;
            }

            if (item.status == BattleExecutionItemStatus.Failed)
            {
                Debug.LogWarning(item.order + ". 执行项已是 Failed，ExecutionPlan 停止继续执行");
                allItemsCompleted = false;
                break;
            }

            if (item.status == BattleExecutionItemStatus.Executed ||
                item.status == BattleExecutionItemStatus.Skipped ||
                item.isCompleted)
            {
                Debug.Log(item.order + ". 执行项已完成，跳过重复执行");
                continue;
            }

            bool isCompleted = false;

            // 无人响应敌人意图：敌人攻击按 actualTarget 直接处理。
            if (item.executionType == BattleExecutionItemType.UnrespondedEnemyIntent)
            {
                isCompleted = ExecuteUnrespondedEnemyIntent(item, runtimeState);
            }
            else if (item.executionType == BattleExecutionItemType.RespondedEnemyIntent)
            {
                // 已响应敌人意图：交给 BattleResolver 正式入口处理。
                isCompleted = ExecuteRespondedEnemyIntent(item, runtimeState);
            }
            else if (item.executionType == BattleExecutionItemType.FreeAction)
            {
                // 自由行动：交给 BattleResolver 正式入口处理。
                isCompleted = ExecuteFreeAction(item);
            }
            else
            {
                item.MarkFailed(BattleExecutionItemOutcomeReason.UnsupportedExecutionType);
                Debug.LogWarning(item.order + ". 不支持的 ExecutionItem 类型：" + item.executionType);
                isCompleted = false;
            }

            if (item.status == BattleExecutionItemStatus.Pending)
            {
                item.MarkFailed(BattleExecutionItemOutcomeReason.ResolverFailure);
                Debug.LogWarning(item.order + ". 执行后仍保持 Pending，按 ResolverFailure 处理并停止计划");
                isCompleted = false;
            }

            if (item.status == BattleExecutionItemStatus.Failed)
            {
                allItemsCompleted = false;
                Debug.LogWarning(item.order + ". 执行项 Failed，ExecutionPlan 停止继续执行，后续 item 保持 Pending");
                break;
            }

            if (!isCompleted || !item.isCompleted)
            {
                allItemsCompleted = false;
                break;
            }

            if (runtimeState != null)
            {
                runtimeState.EvaluateBattleEnd();

                if (runtimeState.IsBattleEnded)
                {
                    MarkRemainingItemsSkippedBecauseBattleEnded(plan, i + 1);
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

    static void MarkRemainingItemsSkippedBecauseBattleEnded(BattleExecutionPlan plan, int startIndex)
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

            item.MarkSkipped(BattleExecutionItemOutcomeReason.BattleEnded);
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
    static bool ExecuteUnrespondedEnemyIntent(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState
    )
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

        System.Collections.Generic.IReadOnlyList<BattleActionSlot> guardSlots =
            runtimeState != null
                ? runtimeState.actionSlots
                : item.passiveGuardCandidates;
        BattleGuardSelectionResult guardSelection =
            BattleGuardSelectionManager.SelectHandlingCardForEnemyIntent(
            guardSlots,
            item.enemyIntent
        );
        BattleActionSlot passiveGuardSlot = guardSelection.slot;
        BattleResolveResult result = null;

        if (passiveGuardSlot != null)
        {
            Debug.Log(
                item.order +
                ". UnrespondedEnemyIntent：守备接管，使用 " +
                passiveGuardSlot.GetDisplaySlotName() +
                " / " +
                passiveGuardSlot.GetCardName()
            );

            result = guardSelection.selectionType == BattleGuardSelectionType.ContinuousDodge
                ? BattleResolver.ResolveContinuousDodgeVsAttack(passiveGuardSlot, item.enemyIntent)
                : BattleResolver.ResolveRespondedEnemyIntent(passiveGuardSlot, item.enemyIntent);

            LogResolveResult(item.order, "Guard Resolver 结算结果", result);

            if (TryMarkResolveFailure(item, result, false))
            {
                Debug.LogWarning(
                    item.order +
                    ". UnrespondedEnemyIntent 守备失败，ExecutionPlan 停止"
                );

                return false;
            }

            HandlePlayerCardDisposition(
                passiveGuardSlot,
                result,
                GetContinuousDodgeSource(guardSelection.selectionType),
                item.enemyIntent,
                item.order + ". UnrespondedEnemyIntent"
            );

            // 一张敌人卡只处理选中的这一张卡；成功或失败都不继续寻找第二张守备。
            item.MarkExecuted();
            return true;
        }

        result = BattleResolver.ResolveUnrespondedEnemyIntent(item.enemyIntent);

        LogResolveResult(item.order, "UnrespondedEnemyIntent Resolver 结算结果", result);

        if (TryMarkResolveFailure(item, result, false))
        {
            Debug.LogWarning(
                item.order +
                ". UnrespondedEnemyIntent 失败，ExecutionPlan 停止"
            );

            return false;
        }

        item.MarkExecuted();
        return true;
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
            "   playerCardParticipated：" + (result != null && result.playerCardParticipated) + "\n" +
            "   playerCardUseDisposition：" +
                (result != null ? result.playerCardUseDisposition.ToString() : "None") + "\n" +
            "   enemyCardUsed：" + (result != null && result.enemyCardUsed) + "\n" +
            "   hasDamage：" + (result != null && result.hasDamage) + "\n" +
            "   damage：" + (result != null ? result.damage : 0) + "\n" +
            "   triggeredEventChain：" + (result != null && result.triggeredEventChain) + "\n" +
            "   message：" + (result != null ? result.message : "BattleResolveResult 为空")
        );
    }

    static bool TryMarkResolveFailure(
        BattleExecutionItem item,
        BattleResolveResult result,
        bool allowActionUnavailable
    )
    {
        BattleExecutionItemOutcomeReason failedReason = GetFailedOutcomeReason(result, allowActionUnavailable);

        if (failedReason == BattleExecutionItemOutcomeReason.None)
        {
            return false;
        }

        if (item != null)
        {
            item.MarkFailed(failedReason);
        }

        return true;
    }

    static BattleExecutionItemOutcomeReason GetFailedOutcomeReason(
        BattleResolveResult result,
        bool allowActionUnavailable
    )
    {
        if (result == null)
        {
            return BattleExecutionItemOutcomeReason.ResolverFailure;
        }

        if (result.isTieLimitReached)
        {
            return BattleExecutionItemOutcomeReason.TieLimitReached;
        }

        if (result.resultType == "Invalid")
        {
            return BattleExecutionItemOutcomeReason.InvalidData;
        }

        if (result.resultType == "Unsupported")
        {
            return BattleExecutionItemOutcomeReason.UnsupportedResolveType;
        }

        if (allowActionUnavailable &&
            result.resultType == "ActionUnavailable" &&
            result.shouldCompleteItem)
        {
            return BattleExecutionItemOutcomeReason.None;
        }

        if (!result.isSuccess || !result.shouldCompleteItem)
        {
            return BattleExecutionItemOutcomeReason.ResolverFailure;
        }

        return BattleExecutionItemOutcomeReason.None;
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
            GetSortMetadataPreviewText(item) +
            "   当前只预览执行方式，不执行 item，不修改状态"
        );
    }

    // ExecuteRespondedEnemyIntent = 执行已响应的敌人意图
    // Executor 只负责分派和完成状态，正式结算交给 BattleResolver。
    static bool ExecuteRespondedEnemyIntent(BattleExecutionItem item, BattleRuntimeState runtimeState)
    {
        if (item == null)
        {
            Debug.LogWarning("执行 RespondedEnemyIntent 失败：item 为空");
            return false;
        }

        if (item.actionSlot == null ||
            item.actionSlot.actor == null ||
            item.actionSlot.cardState == null ||
            item.actionSlot.cardState.cardData == null)
        {
            return ExecuteRespondedFallbackToUnresponded(
                item,
                runtimeState,
                item.order + ". 精确响应槽位或卡牌在进入Resolver前已失效，恢复原目标并转为Unresponded处理",
                BattleExecutionItemOutcomeReason.None
            );
        }

        if (item.actionSlot != null &&
            item.actionSlot.actor != null &&
            item.actionSlot.actor.IsDead())
        {
            return ExecuteRespondedFallbackToUnresponded(
                item,
                runtimeState,
                item.order + ". 响应角色已死亡，原响应失效，恢复原目标并转为Unresponded处理",
                BattleExecutionItemOutcomeReason.None
            );
        }

        if (TryCompleteEnemyItemBecauseActualTargetDead(item))
        {
            return true;
        }

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            item.actionSlot,
            item.enemyIntent
        );

        if (TryMarkResolveFailure(item, result, true))
        {
            Debug.LogWarning(item.order + ". RespondedEnemyIntent 失败，ExecutionPlan 停止");
            return false;
        }

        Debug.Log(
            item.order +
            ". RespondedEnemyIntent Resolver 结算结果\n" +
            "   resultType：" + result.resultType + "\n" +
            "   isSuccess：" + result.isSuccess + "\n" +
            "   shouldCompleteItem：" + result.shouldCompleteItem + "\n" +
            "   playerCardUsed：" + result.playerCardUsed + "\n" +
            "   playerCardParticipated：" + result.playerCardParticipated + "\n" +
            "   playerCardUseDisposition：" + result.playerCardUseDisposition + "\n" +
            "   enemyCardUsed：" + result.enemyCardUsed + "\n" +
            "   hasDamage：" + result.hasDamage + "\n" +
            "   damage：" + result.damage + "\n" +
            "   triggeredEventChain：" + result.triggeredEventChain + "\n" +
            "   message：" + result.message
        );

        if (!result.isSuccess || !result.shouldCompleteItem)
        {
            if (result.resultType == "ActionUnavailable" && result.shouldCompleteItem)
            {
                return ExecuteRespondedActionUnavailableFallback(item, runtimeState, result);
            }

            item.MarkFailed(BattleExecutionItemOutcomeReason.ResolverFailure);
            Debug.LogWarning(
                item.order +
                ". RespondedEnemyIntent 未完成：Resolver 未返回可完成结果，Executor 不补做结算"
            );

            return false;
        }

        HandlePlayerCardDisposition(
            item.actionSlot,
            result,
            ContinuousDodgeSource.ExactEnemyIntent,
            item.enemyIntent,
            item.order + ". RespondedEnemyIntent"
        );

        item.MarkExecuted();
        return true;
    }

    static void HandlePlayerCardDisposition(
        BattleActionSlot slot,
        BattleResolveResult result,
        ContinuousDodgeSource dodgeSource,
        BattleEnemyIntent enemyIntent,
        string logPrefix
    )
    {
        if (slot == null || result == null)
        {
            return;
        }

        if (result.playerCardUseDisposition == BattleCardUseDisposition.DeferForContinuousDodge)
        {
            BattleContinuousDodgeManager.RegisterSuccess(slot, result, dodgeSource, enemyIntent);
            return;
        }

        if (result.playerCardUseDisposition == BattleCardUseDisposition.FinalizeImmediately)
        {
            BattleContinuousDodgeManager.RecordImmediateFinalization(slot, result);
            return;
        }

        slot.MarkUsed();
        Debug.Log(logPrefix + "：行动槽位已正式提交，标记为已使用");
    }

    static ContinuousDodgeSource GetContinuousDodgeSource(
        BattleGuardSelectionType selectionType
    )
    {
        if (selectionType == BattleGuardSelectionType.EnemySpecificGuard)
        {
            return ContinuousDodgeSource.EnemySpecificGuard;
        }

        if (selectionType == BattleGuardSelectionType.PassiveGuard)
        {
            return ContinuousDodgeSource.PassiveGuard;
        }

        if (selectionType == BattleGuardSelectionType.ContinuousDodge)
        {
            return ContinuousDodgeSource.ContinuousDodge;
        }

        return ContinuousDodgeSource.None;
    }

    static bool ExecuteRespondedActionUnavailableFallback(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        BattleResolveResult result
    )
    {
        if (item == null || item.enemyIntent == null)
        {
            Debug.LogWarning("Responded ActionUnavailable 回落失败：item或敌人意图为空");
            return false;
        }

        return ExecuteRespondedFallbackToUnresponded(
            item,
            runtimeState,
            item.order +
                ". RespondedEnemyIntent 响应卡不可用，撤销目标改写并转 Unresponded：" +
                result.message,
            BattleExecutionItemOutcomeReason.ResponseUnavailableFallbackToUnresponded
        );
    }

    static bool ExecuteRespondedFallbackToUnresponded(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        string logMessage,
        BattleExecutionItemOutcomeReason executedReason
    )
    {
        if (item == null || item.enemyIntent == null)
        {
            Debug.LogWarning("Responded 回落 Unresponded 失败：item或敌人意图为空");
            return false;
        }

        BattleEnemyIntent enemyIntent = item.enemyIntent;

        if (enemyIntent.originalTargetCharacter == null)
        {
            Debug.LogWarning("Responded 回落 Unresponded 失败：originalTargetCharacter为空");
            return false;
        }

        Debug.Log(logMessage);

        enemyIntent.SetActualTarget(
            enemyIntent.originalTargetCharacter,
            enemyIntent.originalTargetSlotIndex
        );

        if (TryCompleteEnemyItemBecauseActualTargetDead(item))
        {
            return true;
        }

        bool fallbackCompleted = ExecuteUnrespondedEnemyIntent(item, runtimeState);

        if (fallbackCompleted &&
            item.status == BattleExecutionItemStatus.Executed &&
            executedReason != BattleExecutionItemOutcomeReason.None)
        {
            item.MarkExecuted(executedReason);
        }

        return fallbackCompleted;
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
            item.MarkSkipped(BattleExecutionItemOutcomeReason.ActorDead);
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

        if (TryMarkResolveFailure(item, result, true))
        {
            Debug.LogWarning(
                item.order +
                ". FreeAction 失败，ExecutionPlan 停止"
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

            item.MarkSkipped(BattleExecutionItemOutcomeReason.ActionUnavailable);
            return true;
        }

        if (result.playerCardUsed && item.actionSlot != null)
        {
            item.actionSlot.MarkUsed();
            Debug.Log(item.order + ". FreeAction：玩家行动槽位已标记为已使用");
        }

        item.MarkExecuted();
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
        item.MarkSkipped(BattleExecutionItemOutcomeReason.ActualTargetDead);
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
            item.enemyIntent.intentOrder +
            "\n" +
            GetSortMetadataPreviewText(item)
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
            GetSortMetadataPreviewText(item) +
            "   当前仅预览点数范围和命中目标，不 roll 点数，不造成伤害"
        );
    }

    static string GetSortMetadataPreviewText(BattleExecutionItem item)
    {
        if (item == null)
        {
            return "";
        }

        return
            "   排序键：速度=" + item.effectiveSpeed +
            "，响应优先=" + item.responsePriority +
            "，槽位=" + item.actionSlotOrder +
            "，站位=" + item.actorPositionOrder +
            "，稳定序=" + item.stableOrder +
            "\n";
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
