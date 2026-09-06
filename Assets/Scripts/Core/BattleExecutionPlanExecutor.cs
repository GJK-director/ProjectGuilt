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

    // ExecuteNextItem = 无RuntimeState场景下的单项推进兼容入口。
    // 每次调用最多处理一个尚未完成的ExecutionItem，不会自行执行后续项。
    public static bool ExecuteNextItem(BattleExecutionPlan plan)
    {
        return ExecuteNextItemInternal(plan, null);
    }

    public static void ExecuteExecutionPlan(BattleExecutionPlan plan, BattleRuntimeState runtimeState)
    {
        if (runtimeState == null)
        {
            ExecuteExecutionPlanInternal(plan, null);
            return;
        }

        if (!object.ReferenceEquals(runtimeState.currentExecutionPlan, plan))
        {
            Debug.LogWarning("执行计划失败：传入计划不是RuntimeState当前计划");
            return;
        }

        string failureMessage;
        if (!new BattleLifecycleController(runtimeState)
                .TryExecuteCurrentPlan(out failureMessage))
        {
            Debug.LogWarning(failureMessage);
        }
    }

    internal static void ExecuteCurrentPlanFromLifecycle(
        BattleLifecycleController lifecycleController
    )
    {
        BattleRuntimeState runtimeState = lifecycleController != null
            ? lifecycleController.RuntimeState
            : null;
        BattleExecutionPlan plan = runtimeState != null
            ? runtimeState.currentExecutionPlan
            : null;
        ExecuteExecutionPlanInternal(plan, lifecycleController);
    }

    internal static bool ExecuteNextItemFromLifecycle(
        BattleLifecycleController lifecycleController
    )
    {
        BattleRuntimeState runtimeState = lifecycleController != null
            ? lifecycleController.RuntimeState
            : null;
        BattleExecutionPlan plan = runtimeState != null
            ? runtimeState.currentExecutionPlan
            : null;
        return ExecuteNextItemInternal(plan, lifecycleController);
    }

    static void ExecuteExecutionPlanInternal(
        BattleExecutionPlan plan,
        BattleLifecycleController lifecycleController
    )
    {
        Debug.Log("===== BattleExecutionPlan 正式执行开始 =====");
        Debug.Log("提示：RespondedEnemyIntent / UnrespondedEnemyIntent / FreeAction 已交给 BattleResolver 正式入口处理");

        if (plan == null || plan.executionItems == null || plan.executionItems.Count == 0)
        {
            Debug.Log("当前 BattleExecutionPlan 没有可执行项");
            return;
        }

        while (!plan.isCompleted)
        {
            if (!ExecuteNextItemInternal(plan, lifecycleController))
            {
                break;
            }
        }

        if (plan.isCompleted)
        {
            Debug.Log("BattleExecutionPlan 已全部完成");
            return;
        }

        Debug.Log("当前仍有未完成执行项");
    }

    static bool ExecuteNextItemInternal(
        BattleExecutionPlan plan,
        BattleLifecycleController lifecycleController
    )
    {
        if (plan == null || plan.executionItems == null ||
            plan.executionItems.Count == 0)
        {
            Debug.Log("当前 BattleExecutionPlan 没有可执行项");
            return false;
        }

        if (plan.isCompleted)
        {
            Debug.Log("BattleExecutionPlan 已完成，不重复推进");
            return true;
        }

        int itemIndex = FindNextUnfinishedItemIndex(plan);
        if (itemIndex < 0)
        {
            plan.isCompleted = true;
            return true;
        }

        BattleExecutionItem item = plan.executionItems[itemIndex];
        if (item == null)
        {
            Debug.LogWarning("执行计划项为空，ExecutionPlan 失败并停止");
            return false;
        }

        if (item.status == BattleExecutionItemStatus.Failed)
        {
            Debug.LogWarning(
                item.order + ". 执行项已是 Failed，ExecutionPlan 停止继续执行"
            );
            return false;
        }

        BattleRuntimeState runtimeState = lifecycleController != null
            ? lifecycleController.RuntimeState
            : null;
        if (runtimeState != null && runtimeState.IsBattleEnded)
        {
            item.MarkSkipped(BattleExecutionItemOutcomeReason.BattleEnded);
            Debug.Log(item.order + ". 因 BattleEnded 跳过");
            RefreshPlanCompletion(plan);
            return true;
        }

        bool isCompleted;
        if (item.executionType == BattleExecutionItemType.UnrespondedEnemyIntent)
        {
            isCompleted = ExecuteUnrespondedEnemyIntent(item, runtimeState);
        }
        else if (item.executionType == BattleExecutionItemType.RespondedEnemyIntent)
        {
            isCompleted = ExecuteRespondedEnemyIntent(item, runtimeState);
        }
        else if (item.executionType == BattleExecutionItemType.FreeAction)
        {
            isCompleted = ExecuteFreeAction(item);
        }
        else
        {
            item.MarkFailed(
                BattleExecutionItemOutcomeReason.UnsupportedExecutionType
            );
            Debug.LogWarning(
                item.order + ". 不支持的 ExecutionItem 类型：" +
                item.executionType
            );
            isCompleted = false;
        }

        if (item.status == BattleExecutionItemStatus.Pending)
        {
            item.MarkFailed(BattleExecutionItemOutcomeReason.ResolverFailure);
            Debug.LogWarning(
                item.order +
                ". 执行后仍保持 Pending，按 ResolverFailure 处理并停止计划"
            );
            isCompleted = false;
        }

        if (item.status == BattleExecutionItemStatus.Failed)
        {
            Debug.LogWarning(
                item.order +
                ". 执行项 Failed，ExecutionPlan 停止继续执行，后续 item 保持 Pending"
            );
            return false;
        }

        if (!isCompleted || !item.isCompleted)
        {
            return false;
        }

        if (lifecycleController != null && runtimeState != null)
        {
            lifecycleController.EvaluateBattleEnd();
        }

        RefreshPlanCompletion(plan);
        return true;
    }

    static int FindNextUnfinishedItemIndex(BattleExecutionPlan plan)
    {
        for (int index = 0; index < plan.executionItems.Count; index++)
        {
            BattleExecutionItem item = plan.executionItems[index];
            if (item == null ||
                item.status == BattleExecutionItemStatus.Failed)
            {
                return index;
            }

            if (item.status == BattleExecutionItemStatus.Executed ||
                item.status == BattleExecutionItemStatus.Skipped ||
                item.isCompleted)
            {
                continue;
            }

            return index;
        }

        return -1;
    }

    static void RefreshPlanCompletion(BattleExecutionPlan plan)
    {
        for (int index = 0; index < plan.executionItems.Count; index++)
        {
            BattleExecutionItem item = plan.executionItems[index];
            if (item == null ||
                item.status == BattleExecutionItemStatus.Failed ||
                (item.status != BattleExecutionItemStatus.Executed &&
                 item.status != BattleExecutionItemStatus.Skipped &&
                 !item.isCompleted))
            {
                plan.isCompleted = false;
                return;
            }
        }

        plan.isCompleted = true;
    }

    internal static void RefreshPlanCompletionFromRunner(
        BattleExecutionPlan plan
    )
    {
        if (plan != null && plan.executionItems != null)
        {
            RefreshPlanCompletion(plan);
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

        BattleExecutionInteractionContext plannedContext =
            BattleExecutionInteractionContextFactory.BuildEffective(item, null);
        if (TryCompleteBasicCombatNoInteraction(
                item,
                plannedContext,
                item.order + ". UnrespondedEnemyIntent"
            ))
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
        return ExecuteUnrespondedEnemyIntentWithSelection(item, guardSelection);
    }

    static bool ExecuteUnrespondedEnemyIntentWithSelection(
        BattleExecutionItem item,
        BattleGuardSelectionResult guardSelection
    )
    {
        if (item == null)
        {
            Debug.LogWarning("执行 UnrespondedEnemyIntent 失败：item 为空");
            return false;
        }

        BattleActionSlot passiveGuardSlot = guardSelection.slot;
        BattleExecutionInteractionContext interactionContext =
            BattleExecutionInteractionContextFactory.BuildEffective(
                item,
                passiveGuardSlot
            );

        if (passiveGuardSlot != null &&
            !ValidateRuntimeResponseInteraction(
                item,
                passiveGuardSlot,
                interactionContext,
                guardSelection.selectionType
            ))
        {
            return false;
        }

        if (TryCompleteBasicCombatNoInteraction(
                item,
                interactionContext,
                item.order + ". UnrespondedEnemyIntent"
            ))
        {
            return true;
        }

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

            return CompleteUnrespondedGuardResult(
                item,
                passiveGuardSlot,
                result,
                guardSelection.selectionType
            );
        }

        result = BattleResolver.ResolveUnrespondedEnemyIntent(item.enemyIntent);

        LogResolveResult(item.order, "UnrespondedEnemyIntent Resolver 结算结果", result);

        if (TryCompleteTieLimit(item, result))
        {
            return true;
        }

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

    static bool CompleteUnrespondedGuardResult(
        BattleExecutionItem item,
        BattleActionSlot guardSlot,
        BattleResolveResult result,
        BattleGuardSelectionType selectionType
    )
    {
        LogResolveResult(item.order, "Guard Resolver 结算结果", result);

        if (TryCompleteTieLimit(item, result))
        {
            return true;
        }

        if (TryMarkResolveFailure(item, result, false))
        {
            Debug.LogWarning(
                item.order +
                ". UnrespondedEnemyIntent 守备失败，ExecutionPlan 停止"
            );
            return false;
        }

        HandlePlayerCardDisposition(
            guardSlot,
            result,
            GetContinuousDodgeSource(selectionType),
            item.enemyIntent,
            item.order + ". UnrespondedEnemyIntent"
        );

        // 一张敌人卡只处理选中的这一张卡；成功或失败都不继续寻找第二张守备。
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

    static bool TryCompleteTieLimit(
        BattleExecutionItem item,
        BattleResolveResult result
    )
    {
        if (item == null || result == null ||
            !result.isTieLimitReached ||
            result.resultType != "TieLimit" ||
            !result.isSuccess ||
            !result.shouldCompleteItem)
        {
            return false;
        }

        // TieLimit是合法的无胜负终态：当前项完成，但双方卡牌都不提交使用。
        item.MarkExecuted(BattleExecutionItemOutcomeReason.TieLimitReached);
        Debug.Log(item.order + ". TieLimit正常结束，ExecutionPlan继续下一项");
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
        bool handledBeforeResolver;
        bool preResolveResult;
        if (!TryPrepareRespondedEnemyIntent(
                item,
                runtimeState,
                out handledBeforeResolver,
                out preResolveResult
            ))
        {
            return handledBeforeResolver && preResolveResult;
        }

        if (PrepareShootResponseAttempt(item))
        {
            return ExecuteUnavailableResourceResponseSynchronously(item);
        }

        BattleExecutionInteractionContext interactionContext =
            BattleExecutionInteractionContextFactory.BuildPlanned(item);
        if (TryCompleteBasicCombatNoInteraction(
                item,
                interactionContext,
                item.order + ". RespondedEnemyIntent"
            ))
        {
            return true;
        }

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            item.actionSlot,
            item.enemyIntent
        );

        return CompleteRespondedEnemyIntentResult(item, runtimeState, result);
    }

    // Pausable Runner在Begin时复用与同步入口完全相同的前置检查。
    internal static bool TryBeginPausableRespondedEnemyIntent(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        out BattleClashSession session,
        out bool itemCompleted,
        out string failureMessage
    )
    {
        session = null;
        itemCompleted = false;
        failureMessage = string.Empty;

        bool handledBeforeResolver;
        bool preResolveResult;
        if (!TryPrepareRespondedEnemyIntent(
                item,
                runtimeState,
                out handledBeforeResolver,
                out preResolveResult
            ))
        {
            itemCompleted = handledBeforeResolver && item != null && item.isCompleted;
            if (!preResolveResult)
            {
                failureMessage = "Pausable RespondedEnemyIntent前置检查未能完成当前Item";
            }
            return preResolveResult;
        }

        if (PrepareShootResponseAttempt(item))
        {
            if (!RestoreUnavailableResponseOriginalTarget(item))
            {
                failureMessage =
                    "Pausable NoBullet响应失败：无法恢复敌人原始目标";
                return false;
            }

            if (TryCompleteEnemyItemBecauseActualTargetDead(item))
            {
                itemCompleted = true;
            }
            return true;
        }

        BattleExecutionInteractionContext interactionContext =
            BattleExecutionInteractionContextFactory.BuildPlanned(item);
        if (TryCompleteBasicCombatNoInteraction(
                item,
                interactionContext,
                item.order + ". Pausable RespondedEnemyIntent"
            ))
        {
            itemCompleted = true;
            return true;
        }

        BattleResolveResult beginFailure = BattleResolver.TryBeginRespondedClash(
            item.actionSlot,
            item.enemyIntent,
            out session
        );
        if (beginFailure == null && session != null)
        {
            return true;
        }

        bool completed = CompleteRespondedEnemyIntentResult(
            item,
            runtimeState,
            beginFailure
        );
        itemCompleted = item != null && item.isCompleted;
        if (!completed)
        {
            failureMessage = "Pausable RespondedEnemyIntent初始化失败";
        }
        return completed;
    }

    // Guard Selection 必须先于 Pausable Eligibility，确保 Runner 消费本次冻结的 Effective Interaction。
    internal static bool TryBuildPausableRoutingContext(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        out BattleActionSlot actionSlot,
        out BattleGuardSelectionType guardSelectionType,
        out BattleExecutionInteractionContext executionContext,
        out BattlePresentationInteractionContext presentationContext
    )
    {
        actionSlot = item != null ? item.actionSlot : null;
        guardSelectionType = BattleGuardSelectionType.None;
        executionContext = null;
        presentationContext = null;
        if (item == null)
        {
            return false;
        }

        if (item.executionType == BattleExecutionItemType.UnrespondedEnemyIntent)
        {
            System.Collections.Generic.IReadOnlyList<BattleActionSlot> guardSlots =
                runtimeState != null
                    ? runtimeState.actionSlots
                    : item.passiveGuardCandidates;
            BattleGuardSelectionResult selection =
                BattleGuardSelectionManager.SelectHandlingCardForEnemyIntent(
                    guardSlots,
                    item.enemyIntent
                );
            actionSlot = selection != null ? selection.slot : null;
            guardSelectionType = selection != null
                ? selection.selectionType
                : BattleGuardSelectionType.None;
        }

        if (item.executionType == BattleExecutionItemType.FreeAction)
        {
            item.reactiveEnemyGuardIntent =
                BattleGuardSelectionManager.SelectEnemyDefensiveIntentForFreeAttack(
                    runtimeState != null ? runtimeState.intentQueue : null,
                    item.actionSlot
                );
            executionContext = BattleExecutionInteractionContextFactory
                .BuildEffectiveFreeAction(item, item.reactiveEnemyGuardIntent);
        }
        else
        {
            executionContext = BattleExecutionInteractionContextFactory.BuildEffective(
                item,
                actionSlot
            );
        }

        BattlePresentationInteractionContextFactory.TryCreate(
            executionContext,
            guardSelectionType == BattleGuardSelectionType.ContinuousDodge,
            out presentationContext
        );
        return true;
    }

    internal static bool IsPausableMeleeFreeAttack(
        BattleExecutionItem item
    )
    {
        return item != null &&
            item.executionType == BattleExecutionItemType.FreeAction &&
            item.actionSlot != null &&
            item.actionSlot.slotType == BattleActionSlotType.FreeAction &&
            item.actionSlot.cardState != null &&
            item.actionSlot.cardState.IsMeleeAttack();
    }

    internal static bool TryBeginPausableFreeMeleeAttack(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        out BattleResolutionPlan plan,
        out bool itemCompleted,
        out string failureMessage
    )
    {
        plan = null;
        itemCompleted = false;
        failureMessage = string.Empty;
        if (item == null || runtimeState == null ||
            !IsPausableMeleeFreeAttack(item))
        {
            failureMessage =
                "Pausable FreeAction Melee启动失败：Item语义不匹配";
            return false;
        }

        BattleExecutionInteractionContext interactionContext =
            BattleExecutionInteractionContextFactory.BuildPlanned(item);
        if (interactionContext.effectiveInteractionType !=
            BattleInteractionType.UnilateralAttack)
        {
            item.MarkFailed(BattleExecutionItemOutcomeReason.InvalidData);
            failureMessage =
                "Pausable FreeAction Melee 的 Effective Interaction 不是 UnilateralAttack";
            return false;
        }

        if (item.actionSlot.actor == null || item.actionSlot.actor.IsDead())
        {
            item.MarkSkipped(BattleExecutionItemOutcomeReason.ActorDead);
            itemCompleted = true;
            return true;
        }

        BattleResolveResult failureResult;
        plan = BattleResolver.BuildFreeAttackResolutionPlan(
            item,
            item.actionSlot,
            out failureResult
        );
        if (plan != null)
        {
            Debug.Log(
                "[FreeAction Melee] ActionBegin / Item=" + item.order
            );
            return true;
        }

        if (TryMarkResolveFailure(item, failureResult, true))
        {
            failureMessage = failureResult != null
                ? failureResult.message
                : "FreeAttack ResolutionPlan建立失败";
            return false;
        }

        if (failureResult != null && !failureResult.isSuccess &&
            failureResult.shouldCompleteItem)
        {
            item.MarkSkipped(BattleExecutionItemOutcomeReason.ActionUnavailable);
            itemCompleted = true;
            return true;
        }

        failureMessage = "FreeAttack ResolutionPlan建立失败";
        return false;
    }

    internal static bool TryBeginPausableUnilateralAttack(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        BattlePresentationInteractionContext presentationContext,
        BattleActionSlot compatibilityActionSlot,
        out BattleResolutionPlan plan,
        out bool itemCompleted,
        out string failureMessage
    )
    {
        plan = null;
        itemCompleted = false;
        failureMessage = string.Empty;
        if (item == null || runtimeState == null || presentationContext == null ||
            presentationContext.InteractionType !=
                BattleInteractionType.UnilateralAttack ||
            presentationContext.AttackAction == null)
        {
            failureMessage =
                "Pausable UnilateralAttack启动失败：Interaction语义不匹配";
            return false;
        }

        if (item.executionType == BattleExecutionItemType.UnrespondedEnemyIntent &&
            TryCompleteEnemyItemBecauseActualTargetDead(item))
        {
            itemCompleted = true;
            return true;
        }

        if (presentationContext.AttackAction.actor == null ||
            presentationContext.AttackAction.actor.IsDead())
        {
            item.MarkSkipped(BattleExecutionItemOutcomeReason.ActorDead);
            itemCompleted = true;
            return true;
        }

        plan = BattleResolver.BuildUnilateralAttackResolutionPlan(
            presentationContext.AttackAction,
            item,
            compatibilityActionSlot,
            out BattleResolveResult failureResult
        );
        if (plan == null)
        {
            if (TryMarkResolveFailure(item, failureResult, true))
            {
                failureMessage = failureResult != null
                    ? failureResult.message
                    : "UnilateralAttack ResolutionPlan建立失败";
                return false;
            }

            if (failureResult != null && !failureResult.isSuccess &&
                failureResult.shouldCompleteItem)
            {
                item.MarkSkipped(BattleExecutionItemOutcomeReason.ActionUnavailable);
                itemCompleted = true;
                return true;
            }

            failureMessage = "UnilateralAttack ResolutionPlan建立失败";
            return false;
        }

        return true;
    }

    internal static bool TryBeginPausableFreeActionVsEnemyGuard(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        out BattleClashSession session,
        out bool itemCompleted,
        out string failureMessage
    )
    {
        session = null;
        itemCompleted = false;
        failureMessage = string.Empty;
        if (item == null || runtimeState == null || item.actionSlot == null ||
            item.reactiveEnemyGuardIntent == null)
        {
            failureMessage = "Pausable FreeAction Enemy Guard启动失败：运行时守备意图为空";
            return false;
        }

        BattleResolveResult beginFailure = BattleResolver.TryBeginRespondedClash(
            item.actionSlot,
            item.reactiveEnemyGuardIntent,
            out session
        );
        if (beginFailure == null && session != null)
        {
            return true;
        }

        if (beginFailure != null && !beginFailure.isSuccess &&
            beginFailure.shouldCompleteItem)
        {
            item.MarkSkipped(BattleExecutionItemOutcomeReason.ActionUnavailable);
            itemCompleted = true;
            return true;
        }

        failureMessage = beginFailure != null
            ? beginFailure.message
            : "Pausable FreeAction Enemy Guard无法建立Clash";
        return false;
    }

    // 保留旧测试入口；正式 Runner 会在路由前先选定 Guard 并调用下方重载。
    internal static bool TryBeginPausableUnrespondedEnemyIntent(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        out BattleActionSlot actionSlot,
        out BattleClashSession session,
        out bool itemCompleted,
        out string failureMessage
    )
    {
        System.Collections.Generic.IReadOnlyList<BattleActionSlot> guardSlots =
            runtimeState != null
                ? runtimeState.actionSlots
                : item != null ? item.passiveGuardCandidates : null;
        BattleGuardSelectionResult selection =
            BattleGuardSelectionManager.SelectHandlingCardForEnemyIntent(
                guardSlots,
                item != null ? item.enemyIntent : null
            );
        BattleActionSlot selectedSlot = selection != null
            ? selection.slot
            : null;
        BattleGuardSelectionType selectedType = selection != null
            ? selection.selectionType
            : BattleGuardSelectionType.None;
        return TryBeginPausableUnrespondedEnemyIntent(
            item,
            runtimeState,
            selectedSlot,
            selectedType,
            out actionSlot,
            out session,
            out itemCompleted,
            out failureMessage
        );
    }

    // Unresponded Clash 只消费 Runner 在 Eligibility 前已经冻结的 Guard Selection。
    internal static bool TryBeginPausableUnrespondedEnemyIntent(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        BattleActionSlot selectedActionSlot,
        BattleGuardSelectionType selectedGuardType,
        out BattleActionSlot actionSlot,
        out BattleClashSession session,
        out bool itemCompleted,
        out string failureMessage
    )
    {
        actionSlot = null;
        session = null;
        itemCompleted = false;
        failureMessage = string.Empty;

        if (item == null)
        {
            failureMessage = "Pausable UnrespondedEnemyIntent启动失败：item为空";
            return false;
        }

        if (TryCompleteEnemyItemBecauseActualTargetDead(item))
        {
            itemCompleted = true;
            return true;
        }

        if (selectedActionSlot == null ||
            selectedGuardType == BattleGuardSelectionType.None)
        {
            failureMessage =
                "Pausable Unresponded Clash启动失败：缺少Runtime守备Action";
            return false;
        }

        actionSlot = selectedActionSlot;
        BattleExecutionInteractionContext interactionContext =
            BattleExecutionInteractionContextFactory.BuildEffective(
                item,
                actionSlot
            );
        if (!ValidateRuntimeResponseInteraction(
                item,
                actionSlot,
                interactionContext,
                selectedGuardType
            ))
        {
            failureMessage =
                "Pausable Runtime Guard 的 Effective Interaction 不匹配";
            return false;
        }

        BattleResolveResult beginFailure = selectedGuardType ==
                BattleGuardSelectionType.ContinuousDodge
            ? BattleResolver.TryBeginContinuousDodgeClash(
                actionSlot,
                item.enemyIntent,
                out session
            )
            : BattleResolver.TryBeginRespondedClash(
                actionSlot,
                item.enemyIntent,
                out session
            );
        if (beginFailure == null && session != null)
        {
            return true;
        }

        bool failureCompleted = CompleteUnrespondedGuardResult(
            item,
            actionSlot,
            beginFailure,
            selectedGuardType
        );
        itemCompleted = item.isCompleted;
        if (!failureCompleted)
        {
            failureMessage =
                "Pausable Runtime Guard Clash初始化失败";
        }
        return failureCompleted;
    }

    internal static BattleResolutionPlan BuildPausableEnemyIntentResolutionPlan(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        BattleActionSlot actionSlot,
        BattleClashSession session
    )
    {
        if (item == null || runtimeState == null || actionSlot == null)
        {
            return null;
        }

        if (item.responseAttemptState ==
            BattleResponseAttemptState.UnavailableResource)
        {
            return BattleResolver.BuildUnrespondedEnemyIntentResolutionPlan(
                item,
                actionSlot,
                item.enemyIntent
            );
        }

        if (session == null || !session.IsFinalized)
        {
            return null;
        }

        BattleEnemyIntent enemyIntent = item.executionType ==
                BattleExecutionItemType.FreeAction
            ? item.reactiveEnemyGuardIntent
            : item.enemyIntent;
        return BattleResolver.BuildRespondedClashResolutionPlan(
            actionSlot,
            enemyIntent,
            session,
            item
        );
    }

    internal static bool TryCommitPausableResolutionStep(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        BattleResolutionPlan plan,
        out bool resolutionCompleted,
        out BattleResolveResult result
    )
    {
        resolutionCompleted = plan != null &&
            plan.State == BattleResolutionPlanState.Completed;
        result = plan != null ? plan.CompletedResult : null;
        if (item == null || runtimeState == null || plan == null)
        {
            return false;
        }

        if (!BattleResolver.TryCommitNextResolutionStep(
                plan,
                out result
            ))
        {
            return false;
        }

        resolutionCompleted = plan.State == BattleResolutionPlanState.Completed;
        return true;
    }

    internal static bool CompletePausableFreeAction(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        BattleResolutionPlan plan
    )
    {
        return CompletePausableUnilateralAttack(item, runtimeState, plan);
    }

    internal static bool CompletePausableUnilateralAttack(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        BattleResolutionPlan plan
    )
    {
        if (item == null || runtimeState == null || plan == null ||
            (plan.planKind != BattleResolutionPlanKind.FreeActionAttack &&
                plan.planKind !=
                    BattleResolutionPlanKind.UnrespondedEnemyAttack) ||
            plan.State != BattleResolutionPlanState.Completed ||
            plan.CompletedResult == null)
        {
            return false;
        }

        if (!plan.IsActionCompleted)
        {
            if (plan.CompletedResult.playerCardUsed && plan.actionSlot != null)
            {
                plan.actionSlot.MarkUsed();
            }

            item.MarkExecuted();
            plan.MarkActionCompleted();
            Debug.Log(
                "[FreeAction Melee] ActionComplete / Item=" + item.order
            );
        }

        return item.isCompleted;
    }

    // ActionComplete表现结束后才提交槽位与ExecutionItem状态。
    internal static bool CompletePausableEnemyIntentAction(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        BattleResolutionPlan plan,
        BattleGuardSelectionType guardSelectionType
    )
    {
        if (item == null || runtimeState == null || plan == null ||
            plan.State != BattleResolutionPlanState.Completed ||
            plan.CompletedResult == null)
        {
            return false;
        }

        if (!plan.IsActionCompleted)
        {
            bool completed;
            if (item.responseAttemptState ==
                BattleResponseAttemptState.UnavailableResource)
            {
                item.actionSlot.MarkUsed();
                item.MarkExecuted(
                    BattleExecutionItemOutcomeReason
                        .ResponseUnavailableFallbackToUnresponded
                );
                Debug.Log(
                    item.order +
                    ". NoBullet响应尝试已结束：行动槽位已提交，" +
                    "LongRange卡牌未进入成功使用路径"
                );
                completed = true;
            }
            else if (item.executionType ==
                BattleExecutionItemType.RespondedEnemyIntent)
            {
                completed = CompleteRespondedEnemyIntentResult(
                    item,
                    runtimeState,
                    plan.CompletedResult
                );
            }
            else if (item.executionType ==
                    BattleExecutionItemType.UnrespondedEnemyIntent &&
                plan.clashSession != null &&
                guardSelectionType != BattleGuardSelectionType.None)
            {
                completed = CompleteUnrespondedGuardResult(
                    item,
                    plan.actionSlot,
                    plan.CompletedResult,
                    guardSelectionType
                );
            }
            else if (item.executionType == BattleExecutionItemType.FreeAction &&
                item.reactiveEnemyGuardIntent != null &&
                plan.clashSession != null &&
                plan.CompletedResult.isSuccess &&
                plan.CompletedResult.shouldCompleteItem)
            {
                HandlePlayerCardDisposition(
                    item.actionSlot,
                    plan.CompletedResult,
                    ContinuousDodgeSource.None,
                    item.reactiveEnemyGuardIntent,
                    item.order + ". FreeAction Enemy Guard"
                );
                item.reactiveEnemyGuardIntent.MarkConsumedAsReactiveGuard();
                item.MarkExecuted();
                completed = true;
            }
            else
            {
                return false;
            }

            if (!completed)
            {
                return false;
            }
            plan.MarkActionCompleted();
        }

        return item.isCompleted;
    }

    static bool PrepareShootResponseAttempt(BattleExecutionItem item)
    {
        if (item == null ||
            !BattleResolver.TryCaptureShootResponseResourceSnapshot(
                item.actionSlot,
                item.enemyIntent,
                out BattleClashResourceSnapshot resourceSnapshot
            ))
        {
            return false;
        }

        bool unavailable = BattleResolver.IsResourceUnavailableForExecution(
            resourceSnapshot
        );
        item.SetResponseAttempt(
            unavailable
                ? BattleResponseAttemptState.UnavailableResource
                : BattleResponseAttemptState.Valid,
            resourceSnapshot
        );
        return unavailable;
    }

    static bool ExecuteUnavailableResourceResponseSynchronously(
        BattleExecutionItem item
    )
    {
        if (!RestoreUnavailableResponseOriginalTarget(item))
        {
            return false;
        }
        if (TryCompleteEnemyItemBecauseActualTargetDead(item))
        {
            return true;
        }

        BattleResolveResult result = BattleResolver.ResolveUnrespondedEnemyIntent(
            item.enemyIntent
        );
        LogResolveResult(
            item.order,
            "NoBullet响应回落 UnrespondedEnemyIntent 结算结果",
            result
        );
        if (TryMarkResolveFailure(item, result, false))
        {
            return false;
        }

        item.actionSlot.MarkUsed();
        item.MarkExecuted(
            BattleExecutionItemOutcomeReason
                .ResponseUnavailableFallbackToUnresponded
        );
        return true;
    }

    static bool RestoreUnavailableResponseOriginalTarget(
        BattleExecutionItem item
    )
    {
        if (item == null || item.enemyIntent == null ||
            item.enemyIntent.originalTargetCharacter == null)
        {
            return false;
        }

        item.enemyIntent.SetActualTarget(
            item.enemyIntent.originalTargetCharacter,
            item.enemyIntent.originalTargetSlotIndex
        );
        return true;
    }

    static bool TryPrepareRespondedEnemyIntent(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        out bool handledBeforeResolver,
        out bool handledResult
    )
    {
        handledBeforeResolver = false;
        handledResult = false;

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
            handledBeforeResolver = true;
            handledResult = ExecuteRespondedFallbackToUnresponded(
                item,
                runtimeState,
                item.order + ". 精确响应槽位或卡牌在进入Resolver前已失效，恢复原目标并转为Unresponded处理",
                BattleExecutionItemOutcomeReason.None
            );
            return false;
        }

        if (item.actionSlot.actor.IsDead())
        {
            handledBeforeResolver = true;
            handledResult = ExecuteRespondedFallbackToUnresponded(
                item,
                runtimeState,
                item.order + ". 响应角色已死亡，原响应失效，恢复原目标并转为Unresponded处理",
                BattleExecutionItemOutcomeReason.None
            );
            return false;
        }

        if (TryCompleteEnemyItemBecauseActualTargetDead(item))
        {
            handledBeforeResolver = true;
            handledResult = true;
            return false;
        }

        return true;
    }

    static bool CompleteRespondedEnemyIntentResult(
        BattleExecutionItem item,
        BattleRuntimeState runtimeState,
        BattleResolveResult result
    )
    {
        LogResolveResult(
            item.order,
            "RespondedEnemyIntent Resolver 结算结果",
            result
        );

        if (TryCompleteTieLimit(item, result))
        {
            return true;
        }

        if (TryMarkResolveFailure(item, result, true))
        {
            Debug.LogWarning(item.order + ". RespondedEnemyIntent 失败，ExecutionPlan 停止");
            return false;
        }

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

        // Enemy Defense / Dodge 是后续Attack的reactive guard候选，不存在独立的
        // Unresponded攻击结算。空枪只取消响应方，不能把该守备意图误消费。
        if (enemyIntent.enemyCardState != null &&
            enemyIntent.enemyCardState.cardData != null &&
            enemyIntent.enemyCardState.cardData.cardType != CardType.Attack)
        {
            item.actionSlot.MarkUsed();
            item.MarkExecuted(executedReason);
            return true;
        }

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

        BattleExecutionInteractionContext interactionContext =
            BattleExecutionInteractionContextFactory.BuildPlanned(item);
        if (TryCompleteBasicCombatNoInteraction(
                item,
                interactionContext,
                item.order + ". FreeAction"
            ))
        {
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

        if (TryCompleteTieLimit(item, result))
        {
            return true;
        }

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

    static bool TryCompleteBasicCombatNoInteraction(
        BattleExecutionItem item,
        BattleExecutionInteractionContext context,
        string logPrefix
    )
    {
        if (item == null || context == null ||
            context.effectiveInteractionType !=
                BattleInteractionType.NoInteraction ||
            !ContainsOnlyBasicCombatActions(context))
        {
            return false;
        }

        MarkInteractionActionSlotsHandled(context);
        item.MarkSkipped(BattleExecutionItemOutcomeReason.NoInteraction);
        Debug.Log(
            logPrefix +
            "：Effective Interaction 为 NoInteraction，" +
            "不进入 Resolver；已安排槽位仅标记为本回合已处理"
        );
        return true;
    }

    static bool ContainsOnlyBasicCombatActions(
        BattleExecutionInteractionContext context
    )
    {
        bool hasAction = false;
        if (!IsBasicCombatActionOrNull(context.sideA, ref hasAction) ||
            !IsBasicCombatActionOrNull(context.sideB, ref hasAction))
        {
            return false;
        }

        return hasAction;
    }

    static bool IsBasicCombatActionOrNull(
        BattleExecutionAction action,
        ref bool hasAction
    )
    {
        if (action == null)
        {
            return true;
        }

        if (action.cardState == null || action.cardState.cardData == null)
        {
            return false;
        }

        hasAction = true;
        string cardType = action.cardState.cardData.cardType;
        return cardType == CardType.Attack ||
            cardType == CardType.Defense ||
            cardType == CardType.Dodge;
    }

    static void MarkInteractionActionSlotsHandled(
        BattleExecutionInteractionContext context
    )
    {
        BattleActionSlot sideASlot = context.sideA != null
            ? context.sideA.actionSlot
            : null;
        BattleActionSlot sideBSlot = context.sideB != null
            ? context.sideB.actionSlot
            : null;

        if (sideASlot != null)
        {
            sideASlot.MarkUsed();
        }

        if (sideBSlot != null && !object.ReferenceEquals(sideBSlot, sideASlot))
        {
            sideBSlot.MarkUsed();
        }
    }

    static bool ValidateRuntimeResponseInteraction(
        BattleExecutionItem item,
        BattleActionSlot responseSlot,
        BattleExecutionInteractionContext context,
        BattleGuardSelectionType selectionType
    )
    {
        BattleInteractionType expectedInteraction;
        string cardType = responseSlot != null &&
            responseSlot.cardState != null &&
            responseSlot.cardState.cardData != null
                ? responseSlot.cardState.cardData.cardType
                : null;

        if (cardType == CardType.Defense)
        {
            expectedInteraction = BattleInteractionType.AttackVsDefense;
        }
        else if (cardType == CardType.Dodge)
        {
            expectedInteraction = BattleInteractionType.AttackVsDodge;
        }
        else
        {
            expectedInteraction = BattleInteractionType.NoInteraction;
        }

        bool isValid = context != null &&
            expectedInteraction != BattleInteractionType.NoInteraction &&
            context.effectiveInteractionType == expectedInteraction &&
            (selectionType != BattleGuardSelectionType.ContinuousDodge ||
             expectedInteraction == BattleInteractionType.AttackVsDodge);
        if (isValid)
        {
            return true;
        }

        if (item != null)
        {
            item.MarkFailed(BattleExecutionItemOutcomeReason.InvalidData);
        }

        Debug.LogWarning(
            "Runtime Guard Selection 与 Effective Interaction 不匹配：" +
            "Selection=" + selectionType +
            "，CardType=" + (cardType ?? "null") +
            "，Effective=" +
                (context != null
                    ? context.effectiveInteractionType.ToString()
                    : "null")
        );
        return false;
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
