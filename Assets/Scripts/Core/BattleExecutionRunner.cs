// 可暂停执行推进器：协调Roll、表现等待和提交边界，不拥有具体表现或战斗规则。
using UnityEngine;

public enum BattleRollMode
{
    Manual,
    Auto
}

public enum BattleExecutionRunnerPhase
{
    Idle,
    ClashReadyPause,
    WaitingForRoll,
    AutoRollDelay,
    Rolling,
    Finalizing,
    ResolutionPending,
    WaitingForPresentation,
    ItemCompleted,
    Completed,
    Failed
}

enum BattlePresentationContinuation
{
    None,
    AfterActionBegin,
    AfterUnilateralActionBegin,
    AfterUnavailableResponseActionBegin,
    AfterRollResult,
    AfterImpact,
    AfterActionComplete,
    AfterExecutionComplete
}

[System.Serializable]
public sealed class BattleRollGateSettings
{
    public BattleRollMode rollMode = BattleRollMode.Manual;
    public float clashReadyPause = 0.5f;
    public float autoRollDelay = 0.25f;

    public BattleRollGateSettings()
    {
    }

    public BattleRollGateSettings(
        BattleRollMode rollMode,
        float clashReadyPause,
        float autoRollDelay
    )
    {
        this.rollMode = rollMode;
        this.clashReadyPause = Mathf.Max(0f, clashReadyPause);
        this.autoRollDelay = Mathf.Max(0f, autoRollDelay);
    }

    public BattleRollGateSettings CloneNormalized()
    {
        return new BattleRollGateSettings(
            rollMode,
            clashReadyPause,
            autoRollDelay
        );
    }
}

public sealed class BattleExecutionRunner
{
    const float TimerEpsilon = 0.0001f;

    readonly BattleLifecycleController lifecycleController;
    readonly BattleRollGateSettings settings;
    readonly IBattleExecutionPresenter presenter;

    static long nextPresentationRequestId;
    BattlePresentationContinuation presentationContinuation;

    public BattleExecutionItem CurrentItem { get; private set; }
    public BattleActionSlot CurrentActionSlot { get; private set; }
    public BattleClashSession CurrentClashSession { get; private set; }
    public BattleResolutionPlan CurrentResolutionPlan { get; private set; }
    public BattleExecutionInteractionContext CurrentExecutionInteractionContext
    {
        get;
        private set;
    }
    public BattlePresentationInteractionContext
        CurrentPresentationInteractionContext { get; private set; }
    public BattleExecutionPhaseRequirements CurrentPhaseRequirements
    {
        get;
        private set;
    }
    public BattleGuardSelectionType CurrentGuardSelectionType
    {
        get;
        private set;
    }
    public BattlePresentationRequest CurrentPresentationRequest { get; private set; }
    public BattlePresentationCompletion CurrentPresentationCompletion { get; private set; }
    public BattleExecutionRunnerPhase Phase { get; private set; }
    public float ClashReadyPauseRemaining { get; private set; }
    public float AutoRollDelayRemaining { get; private set; }
    public bool IsWaitingForInput
    {
        get
        {
            return settings.rollMode == BattleRollMode.Manual &&
                Phase == BattleExecutionRunnerPhase.WaitingForRoll;
        }
    }
    public bool IsCompleted { get; private set; }
    public bool HasFailed { get; private set; }
    public bool CurrentItemCompleted { get; private set; }
    public BattleRollMode RollMode
    {
        get { return settings.rollMode; }
    }

    internal BattleExecutionRunner(
        BattleLifecycleController lifecycleController,
        BattleRollGateSettings settings,
        IBattleExecutionPresenter presenter
    )
    {
        this.lifecycleController = lifecycleController;
        this.settings = settings != null
            ? settings.CloneNormalized()
            : new BattleRollGateSettings();
        this.presenter = presenter ?? BattleImmediatePresenter.Instance;
        Phase = BattleExecutionRunnerPhase.Idle;
    }

    internal bool Begin(out string failureMessage)
    {
        if (Phase != BattleExecutionRunnerPhase.Idle)
        {
            failureMessage = "Pausable执行启动失败：Runner已经开始";
            return false;
        }

        return BeginNextItem(out failureMessage);
    }

    public bool Advance(float deltaTime, out string failureMessage)
    {
        failureMessage = string.Empty;
        if (HasFailed)
        {
            failureMessage = "Pausable执行推进失败：Runner已失败";
            return false;
        }

        if (IsCompleted)
        {
            return true;
        }

        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        if (Phase == BattleExecutionRunnerPhase.WaitingForPresentation)
        {
            return AdvanceWaitingPresentation(out failureMessage);
        }

        if (Phase == BattleExecutionRunnerPhase.ResolutionPending)
        {
            return AdvanceResolutionPending(out failureMessage);
        }

        if (Phase == BattleExecutionRunnerPhase.ItemCompleted)
        {
            CurrentItem = null;
            CurrentActionSlot = null;
            CurrentClashSession = null;
            CurrentResolutionPlan = null;
            CurrentExecutionInteractionContext = null;
            CurrentPresentationInteractionContext = null;
            CurrentPhaseRequirements = null;
            CurrentGuardSelectionType = BattleGuardSelectionType.None;
            CurrentItemCompleted = false;
            return BeginNextItem(out failureMessage);
        }

        if (Phase == BattleExecutionRunnerPhase.ClashReadyPause)
        {
            ClashReadyPauseRemaining = Mathf.Max(
                0f,
                ClashReadyPauseRemaining - safeDeltaTime
            );
            if (ClashReadyPauseRemaining <= TimerEpsilon)
            {
                CompleteReadyPause();
            }
            return true;
        }

        if (Phase == BattleExecutionRunnerPhase.AutoRollDelay)
        {
            AutoRollDelayRemaining = Mathf.Max(
                0f,
                AutoRollDelayRemaining - safeDeltaTime
            );
            if (AutoRollDelayRemaining <= TimerEpsilon)
            {
                return RollOneAttempt(out failureMessage);
            }
            return true;
        }

        // Manual WaitingForRoll可以无限等待；Advance不会隐式生成Roll请求。
        return true;
    }

    public bool TryRequestManualRoll(out string failureMessage)
    {
        failureMessage = string.Empty;
        if (settings.rollMode != BattleRollMode.Manual)
        {
            failureMessage = "Manual Roll请求失败：当前Runner不是Manual模式";
            return false;
        }

        if (Phase != BattleExecutionRunnerPhase.WaitingForRoll)
        {
            // 提前请求直接拒绝且不缓存，ReadyPause结束后必须重新请求。
            failureMessage = "Manual Roll请求失败：当前尚未进入WaitingForRoll";
            return false;
        }

        if (!HasPendingRollMechanic())
        {
            failureMessage = "Manual Roll请求失败：当前没有可执行的Roll Mechanic";
            return false;
        }

        return RollOneAttempt(out failureMessage);
    }

    bool BeginNextItem(out string failureMessage)
    {
        failureMessage = string.Empty;
        BattleRuntimeState runtimeState = lifecycleController != null
            ? lifecycleController.RuntimeState
            : null;
        BattleExecutionPlan plan = runtimeState != null
            ? runtimeState.currentExecutionPlan
            : null;
        CurrentItem = FindNextPendingItem(plan);
        CurrentActionSlot = null;
        CurrentClashSession = null;
        CurrentResolutionPlan = null;
        CurrentExecutionInteractionContext = null;
        CurrentPresentationInteractionContext = null;
        CurrentPhaseRequirements = null;
        CurrentGuardSelectionType = BattleGuardSelectionType.None;
        ClearPresentationReferences();
        CurrentItemCompleted = false;

        if (CurrentItem == null)
        {
            if (plan != null && plan.isCompleted)
            {
                return BeginPresentation(
                    BattlePresentationCue.ExecutionComplete,
                    BattlePresentationContinuation.AfterExecutionComplete,
                    null,
                    "ExecutionComplete"
                );
            }

            return Fail("Pausable执行失败：没有可推进的ExecutionItem", out failureMessage);
        }

        if (runtimeState.IsBattleEnded)
        {
            bool executed = BattleExecutionPlanExecutor
                .ExecuteNextItemFromLifecycle(lifecycleController);
            if (!executed || !CurrentItem.isCompleted)
            {
                return Fail("Pausable非Clash Item未能按同步路径完成", out failureMessage);
            }

            return FinishCurrentItem(out failureMessage);
        }

        if (!BattleExecutionPlanExecutor.TryBuildPausableRoutingContext(
                CurrentItem,
                runtimeState,
                out BattleActionSlot routedActionSlot,
                out BattleGuardSelectionType guardSelectionType,
                out BattleExecutionInteractionContext executionContext,
                out BattlePresentationInteractionContext presentationContext
            ))
        {
            return Fail(
                "Pausable执行失败：无法建立Effective Interaction Context",
                out failureMessage
            );
        }

        CurrentActionSlot = routedActionSlot;
        CurrentGuardSelectionType = guardSelectionType;
        CurrentExecutionInteractionContext = executionContext;
        CurrentPresentationInteractionContext = presentationContext;
        CurrentPhaseRequirements = BattleExecutionPausablePolicy.Evaluate(
            presentationContext
        );

        if (!CurrentPhaseRequirements.HasPresentationPhases)
        {
            bool executed = BattleExecutionPlanExecutor
                .ExecuteNextItemFromLifecycle(lifecycleController);
            if (!executed || !CurrentItem.isCompleted)
            {
                return Fail(
                    "非Combat Presentation Item未能按同步路径完成",
                    out failureMessage
                );
            }

            return FinishCurrentItem(out failureMessage);
        }

        if (CurrentPhaseRequirements.InteractionType ==
            BattleInteractionType.UnilateralAttack)
        {
            bool unilateralBegan = BattleExecutionPlanExecutor
                .TryBeginPausableUnilateralAttack(
                    CurrentItem,
                    runtimeState,
                    CurrentPresentationInteractionContext,
                    CurrentActionSlot,
                    out BattleResolutionPlan unilateralPlan,
                    out bool unilateralItemCompleted,
                    out failureMessage
                );
            if (!unilateralBegan)
            {
                return Fail(failureMessage, out failureMessage);
            }

            if (unilateralItemCompleted)
            {
                return FinishCurrentItem(out failureMessage);
            }

            CurrentResolutionPlan = unilateralPlan;
            return BeginPresentation(
                BattlePresentationCue.ActionBegin,
                BattlePresentationContinuation.AfterUnilateralActionBegin,
                null,
                BattleInteractionType.UnilateralAttack.ToString()
            );
        }

        bool itemCompleted;
        BattleActionSlot actionSlot;
        BattleClashSession session;
        bool began;
        if (CurrentItem.executionType ==
            BattleExecutionItemType.RespondedEnemyIntent)
        {
            actionSlot = CurrentItem.actionSlot;
            began = BattleExecutionPlanExecutor
                .TryBeginPausableRespondedEnemyIntent(
                    CurrentItem,
                    runtimeState,
                    out session,
                    out itemCompleted,
                    out failureMessage
                );
        }
        else if (CurrentItem.executionType ==
            BattleExecutionItemType.UnrespondedEnemyIntent)
        {
            began = BattleExecutionPlanExecutor
                .TryBeginPausableUnrespondedEnemyIntent(
                    CurrentItem,
                    runtimeState,
                    CurrentActionSlot,
                    CurrentGuardSelectionType,
                    out actionSlot,
                    out session,
                    out itemCompleted,
                    out failureMessage
                );
        }
        else if (CurrentItem.executionType == BattleExecutionItemType.FreeAction &&
            CurrentItem.reactiveEnemyGuardIntent != null)
        {
            actionSlot = CurrentItem.actionSlot;
            began = BattleExecutionPlanExecutor
                .TryBeginPausableFreeActionVsEnemyGuard(
                    CurrentItem,
                    runtimeState,
                    out session,
                    out itemCompleted,
                    out failureMessage
                );
        }
        else
        {
            return Fail(
                "Pausable Clash缺少可识别的Execution来源Adapter",
                out failureMessage
            );
        }

        if (!began)
        {
            return Fail(failureMessage, out failureMessage);
        }

        if (itemCompleted)
        {
            return FinishCurrentItem(out failureMessage);
        }

        if (actionSlot == null)
        {
            return Fail("Pausable执行启动失败：行动槽位为空", out failureMessage);
        }

        CurrentActionSlot = actionSlot;
        CurrentClashSession = session;
        if (CurrentItem.responseAttemptState ==
            BattleResponseAttemptState.UnavailableResource)
        {
            return BeginPresentation(
                BattlePresentationCue.ActionBegin,
                BattlePresentationContinuation
                    .AfterUnavailableResponseActionBegin,
                null,
                "ResponseUnavailableResource"
            );
        }

        if (session == null)
        {
            return Fail("Pausable Clash启动失败：Session为空", out failureMessage);
        }

        return BeginPresentation(
            BattlePresentationCue.ActionBegin,
            BattlePresentationContinuation.AfterActionBegin,
            null,
            session.ClashType.ToString()
        );
    }

    void EnterReadyPause()
    {
        Phase = BattleExecutionRunnerPhase.ClashReadyPause;
        ClashReadyPauseRemaining = settings.clashReadyPause;
        AutoRollDelayRemaining = 0f;
        if (ClashReadyPauseRemaining <= TimerEpsilon)
        {
            CompleteReadyPause();
        }
    }

    void CompleteReadyPause()
    {
        ClashReadyPauseRemaining = 0f;
        if (settings.rollMode == BattleRollMode.Manual)
        {
            Phase = BattleExecutionRunnerPhase.WaitingForRoll;
            return;
        }

        Phase = BattleExecutionRunnerPhase.AutoRollDelay;
        AutoRollDelayRemaining = settings.autoRollDelay;
    }

    bool RollOneAttempt(out string failureMessage)
    {
        failureMessage = string.Empty;
        if (CurrentClashSession != null)
        {
            return RollClashAttempt(out failureMessage);
        }

        if (IsPendingUnilateralRoll())
        {
            return RollUnilateralAttack(out failureMessage);
        }

        return Fail("Roll失败：当前没有可执行的Roll Mechanic", out failureMessage);
    }

    bool RollClashAttempt(out string failureMessage)
    {
        failureMessage = string.Empty;
        if (CurrentClashSession == null || CurrentClashSession.IsFinalized)
        {
            return Fail("Roll失败：当前ClashSession无效或已完成", out failureMessage);
        }

        Phase = BattleExecutionRunnerPhase.Rolling;
        if (!CurrentClashSession.RollNextAttempt())
        {
            return Fail("Roll失败：BattleClashSession拒绝本次Attempt", out failureMessage);
        }

        if (CurrentClashSession.IsFinalized)
        {
            Phase = BattleExecutionRunnerPhase.Finalizing;
            CurrentResolutionPlan = BattleExecutionPlanExecutor
                .BuildPausableEnemyIntentResolutionPlan(
                    CurrentItem,
                    lifecycleController.RuntimeState,
                    CurrentActionSlot,
                    CurrentClashSession
                );
            if (CurrentResolutionPlan == null)
            {
                return Fail("Clash Finalize失败：无法建立ResolutionPlan", out failureMessage);
            }

            return BeginPresentation(
                BattlePresentationCue.RollResult,
                BattlePresentationContinuation.AfterRollResult,
                null,
                CurrentClashSession.FinalResult.ToString()
            );
        }

        if (!CurrentClashSession.RequiresAnotherRoll)
        {
            return Fail("Clash Attempt结束后既未Finalized也未请求下一次Roll", out failureMessage);
        }

        // AttackTie的结果也必须先完成表现，之后才能回到新一轮ReadyPause。
        return BeginPresentation(
            BattlePresentationCue.RollResult,
            BattlePresentationContinuation.AfterRollResult,
            null,
            CurrentClashSession.AttemptResult.ToString()
        );
    }

    bool RollUnilateralAttack(out string failureMessage)
    {
        failureMessage = string.Empty;
        Phase = BattleExecutionRunnerPhase.Rolling;
        if (!BattleResolver.TryRollUnilateralAttackResolutionPlan(
                CurrentResolutionPlan,
                out _
            ) || !CurrentResolutionPlan.freeActionHasRolled ||
            CurrentResolutionPlan.impacts.Count == 0)
        {
            return Fail(
                "Roll失败：UnilateralAttack无法生成有效攻击点与Impact",
                out failureMessage
            );
        }

        return BeginPresentation(
            BattlePresentationCue.RollResult,
            BattlePresentationContinuation.AfterRollResult,
            null,
            CurrentResolutionPlan.resultType
        );
    }

    bool HasPendingRollMechanic()
    {
        return CurrentClashSession != null && !CurrentClashSession.IsFinalized ||
            IsPendingUnilateralRoll();
    }

    bool IsPendingUnilateralRoll()
    {
        return CurrentClashSession == null && CurrentPhaseRequirements != null &&
            CurrentPhaseRequirements.InteractionType ==
                BattleInteractionType.UnilateralAttack &&
            IsUnilateralResolutionPlan(CurrentResolutionPlan) &&
            CurrentResolutionPlan.State == BattleResolutionPlanState.Pending &&
            !CurrentResolutionPlan.freeActionHasRolled &&
            CurrentResolutionPlan.impacts.Count == 0;
    }

    static bool IsUnilateralResolutionPlan(BattleResolutionPlan plan)
    {
        return plan != null &&
            (plan.planKind == BattleResolutionPlanKind.FreeActionAttack ||
             plan.planKind ==
                BattleResolutionPlanKind.UnrespondedEnemyAttack);
    }

    public bool TryCommitNextResolutionStep(out string failureMessage)
    {
        failureMessage = string.Empty;
        if (HasFailed)
        {
            failureMessage = "Resolution提交失败：Runner已失败";
            return false;
        }
        if (IsCompleted)
        {
            return true;
        }

        if (Phase == BattleExecutionRunnerPhase.ResolutionPending)
        {
            return AdvanceResolutionPending(out failureMessage);
        }

        if (Phase == BattleExecutionRunnerPhase.WaitingForPresentation &&
            presentationContinuation == BattlePresentationContinuation.AfterImpact)
        {
            return AdvanceWaitingPresentation(out failureMessage);
        }

        failureMessage = "Resolution提交失败：当前没有可推进的Resolution表现步骤";
        return false;
    }

    public bool CancelPendingPresentation(string reason = "Runner Cancel")
    {
        if (CurrentPresentationRequest == null ||
            CurrentPresentationCompletion == null)
        {
            return false;
        }

        presenter.Cancel(
            CurrentPresentationRequest,
            CurrentPresentationCompletion
        );
        CurrentPresentationCompletion.TryCancel(
            CurrentPresentationRequest.RequestId
        );
        ClearPresentationReferences();
        HasFailed = true;
        Phase = BattleExecutionRunnerPhase.Failed;
        Debug.Log("Pausable表现等待已取消：" + reason);
        return true;
    }

    bool AdvanceWaitingPresentation(out string failureMessage)
    {
        failureMessage = string.Empty;
        if (CurrentPresentationRequest == null ||
            CurrentPresentationCompletion == null)
        {
            return Fail("表现推进失败：当前Presentation Request为空", out failureMessage);
        }

        if (!CurrentPresentationCompletion.IsCompleted)
        {
            return true;
        }

        long requestId = CurrentPresentationRequest.RequestId;
        if (!CurrentPresentationCompletion.TryConsume(requestId))
        {
            return Fail("表现推进失败：Completion已失效或重复消费", out failureMessage);
        }

        BattlePresentationContinuation continuation = presentationContinuation;
        ClearPresentationReferences();

        if (continuation == BattlePresentationContinuation.AfterActionBegin)
        {
            EnterReadyPause();
            return true;
        }

        if (continuation ==
            BattlePresentationContinuation.AfterUnilateralActionBegin)
        {
            if (CurrentResolutionPlan == null ||
                CurrentPhaseRequirements == null ||
                CurrentPhaseRequirements.InteractionType !=
                    BattleInteractionType.UnilateralAttack ||
                CurrentClashSession != null ||
                !IsUnilateralResolutionPlan(CurrentResolutionPlan) ||
                CurrentResolutionPlan.State !=
                    BattleResolutionPlanState.Pending ||
                CurrentResolutionPlan.freeActionHasRolled ||
                CurrentResolutionPlan.impacts.Count != 0)
            {
                return Fail(
                    "Unilateral ActionBegin完成后Phase Contract无效",
                    out failureMessage
                );
            }

            EnterReadyPause();
            return true;
        }

        if (continuation ==
            BattlePresentationContinuation.AfterUnavailableResponseActionBegin)
        {
            CurrentResolutionPlan = BattleExecutionPlanExecutor
                .BuildPausableEnemyIntentResolutionPlan(
                    CurrentItem,
                    lifecycleController.RuntimeState,
                    CurrentActionSlot,
                    null
                );
            if (CurrentResolutionPlan == null)
            {
                return Fail(
                    "NoBullet ActionBegin完成后无法建立敌方ResolutionPlan",
                    out failureMessage
                );
            }

            Phase = BattleExecutionRunnerPhase.ResolutionPending;
            return true;
        }

        if (continuation == BattlePresentationContinuation.AfterRollResult)
        {
            if (IsUnilateralResolutionPlan(CurrentResolutionPlan))
            {
                if (CurrentClashSession != null ||
                    !CurrentResolutionPlan.freeActionHasRolled ||
                    CurrentResolutionPlan.impacts.Count == 0)
                {
                    return Fail(
                        "UnilateralAttack RollResult表现完成后Plan无效",
                        out failureMessage
                    );
                }

                Phase = BattleExecutionRunnerPhase.ResolutionPending;
                return true;
            }

            if (CurrentClashSession == null)
            {
                return Fail("RollResult表现完成失败：ClashSession为空", out failureMessage);
            }

            if (CurrentClashSession.IsFinalized)
            {
                if (CurrentResolutionPlan == null)
                {
                    return Fail("RollResult表现完成失败：ResolutionPlan为空", out failureMessage);
                }

                Phase = BattleExecutionRunnerPhase.ResolutionPending;
                return true;
            }

            if (!CurrentClashSession.RequiresAnotherRoll)
            {
                return Fail("RollResult表现完成后没有合法后续Roll", out failureMessage);
            }

            EnterReadyPause();
            return true;
        }

        if (continuation == BattlePresentationContinuation.AfterImpact)
        {
            return CommitOneResolutionStep(out failureMessage);
        }

        if (continuation == BattlePresentationContinuation.AfterActionComplete)
        {
            bool unilateral = CurrentPhaseRequirements != null &&
                CurrentPhaseRequirements.InteractionType ==
                    BattleInteractionType.UnilateralAttack &&
                (CurrentItem == null ||
                    CurrentItem.reactiveEnemyGuardIntent == null);
            bool completed = unilateral
                    ? BattleExecutionPlanExecutor
                        .CompletePausableUnilateralAttack(
                        CurrentItem,
                        lifecycleController.RuntimeState,
                        CurrentResolutionPlan
                    )
                    : BattleExecutionPlanExecutor
                        .CompletePausableEnemyIntentAction(
                            CurrentItem,
                            lifecycleController.RuntimeState,
                            CurrentResolutionPlan,
                            CurrentGuardSelectionType
                        );
            if (!completed)
            {
                return Fail("ActionComplete后未能完成ExecutionItem", out failureMessage);
            }

            return FinishCurrentItem(out failureMessage);
        }

        if (continuation ==
            BattlePresentationContinuation.AfterExecutionComplete)
        {
            return CompleteRunner(out failureMessage);
        }

        return Fail("表现推进失败：Continuation无效", out failureMessage);
    }

    bool AdvanceResolutionPending(out string failureMessage)
    {
        failureMessage = string.Empty;
        if (CurrentResolutionPlan == null)
        {
            return Fail("Resolution推进失败：ResolutionPlan为空", out failureMessage);
        }

        BattleImpact impact = CurrentResolutionPlan.GetNextPendingImpact();
        if (impact != null)
        {
            if (CurrentResolutionPlan.planKind ==
                BattleResolutionPlanKind.FreeActionAttack)
            {
                Debug.Log(
                    "[FreeAction Melee] Impact Presentation / Item=" +
                    (CurrentItem != null ? CurrentItem.order : 0)
                );
            }
            return BeginPresentation(
                BattlePresentationCue.Impact,
                BattlePresentationContinuation.AfterImpact,
                impact,
                CurrentResolutionPlan.resultType
            );
        }

        // 0-impact结果不伪造Impact；独立推进一次Activation与CompleteResolution。
        return CommitOneResolutionStep(out failureMessage);
    }

    bool CommitOneResolutionStep(out string failureMessage)
    {
        failureMessage = string.Empty;
        BattleImpact pendingImpact = CurrentResolutionPlan != null
            ? CurrentResolutionPlan.GetNextPendingImpact()
            : null;
        int hpBefore = pendingImpact != null && pendingImpact.target != null
            ? pendingImpact.target.currentHP
            : 0;
        if (!BattleExecutionPlanExecutor
                .TryCommitPausableResolutionStep(
                    CurrentItem,
                    lifecycleController.RuntimeState,
                    CurrentResolutionPlan,
                    out bool resolutionCompleted,
                    out BattleResolveResult result
                ))
        {
            return Fail("Resolution提交失败：当前步骤未能完成", out failureMessage);
        }

        if (pendingImpact != null && pendingImpact.target != null &&
            presenter is IBattleImpactCommitObserver impactObserver)
        {
            impactObserver.OnImpactCommitted(
                pendingImpact,
                hpBefore,
                pendingImpact.target.currentHP
            );
        }

        if (pendingImpact != null && CurrentResolutionPlan.planKind ==
            BattleResolutionPlanKind.FreeActionAttack)
        {
            int hpAfter = pendingImpact.target != null
                ? pendingImpact.target.currentHP
                : hpBefore;
            Debug.Log(
                "[FreeAction Melee] Impact Commit / Target HP: Before=" +
                hpBefore + " / After=" + hpAfter
            );
        }

        if (!resolutionCompleted)
        {
            Phase = BattleExecutionRunnerPhase.ResolutionPending;
            return true;
        }

        if (result == null)
        {
            return Fail("Resolution提交失败：完成结果为空", out failureMessage);
        }

        return BeginPresentation(
            BattlePresentationCue.ActionComplete,
            BattlePresentationContinuation.AfterActionComplete,
            null,
            result.resultType
        );
    }

    bool BeginPresentation(
        BattlePresentationCue cue,
        BattlePresentationContinuation continuation,
        BattleImpact impact,
        string outcome
    )
    {
        long requestId = System.Threading.Interlocked.Increment(
            ref nextPresentationRequestId
        );
        CurrentPresentationRequest = new BattlePresentationRequest(
            requestId,
            cue,
            CurrentItem,
            CurrentClashSession,
            CurrentResolutionPlan,
            impact,
            outcome,
            cue == BattlePresentationCue.ActionComplete &&
                ShouldCarryBattleActionCameraToNextItem(),
            CurrentPresentationInteractionContext
        );
        CurrentPresentationCompletion = new BattlePresentationCompletion(
            requestId
        );
        presentationContinuation = continuation;
        Phase = BattleExecutionRunnerPhase.WaitingForPresentation;

        presenter.Present(
            CurrentPresentationRequest,
            CurrentPresentationCompletion
        );

        // 即使Presenter同步完成，本次调用也必须停在新建的表现边界。
        return true;
    }

    bool ShouldCarryBattleActionCameraToNextItem()
    {
        if (CurrentItem == null || CurrentActionSlot == null ||
            CurrentClashSession == null || CurrentResolutionPlan == null ||
            CurrentClashSession.ClashType != BattleClashType.DodgeVsAttack ||
            CurrentClashSession.FinalResult !=
                BattleClashFinalResult.DodgeSuccess ||
            CurrentResolutionPlan.playerCardUseDisposition !=
                BattleCardUseDisposition.DeferForContinuousDodge)
        {
            return false;
        }

        BattleRuntimeState runtimeState = lifecycleController != null
            ? lifecycleController.RuntimeState
            : null;
        BattleExecutionPlan plan = runtimeState != null
            ? runtimeState.currentExecutionPlan
            : null;
        BattleExecutionItem nextItem = FindNextPendingItemAfter(
            plan,
            CurrentItem
        );
        if (nextItem == null ||
            nextItem.executionType !=
                BattleExecutionItemType.UnrespondedEnemyIntent ||
            nextItem.enemyIntent == null)
        {
            return false;
        }

        return BattleGuardSelectionManager
            .WouldSelectContinuousDodgeForEnemyIntent(
                runtimeState.actionSlots,
                nextItem.enemyIntent,
                CurrentActionSlot
            );
    }

    void ClearPresentationReferences()
    {
        CurrentPresentationRequest = null;
        CurrentPresentationCompletion = null;
        presentationContinuation = BattlePresentationContinuation.None;
    }

    bool FinishCurrentItem(out string failureMessage)
    {
        CurrentItemCompleted = CurrentItem != null && CurrentItem.isCompleted;
        if (!CurrentItemCompleted)
        {
            return Fail("Pausable执行失败：当前Item尚未完成", out failureMessage);
        }

        if (!lifecycleController.HandlePausableItemCompleted(out failureMessage))
        {
            return Fail(failureMessage, out failureMessage);
        }

        BattleRuntimeState runtimeState = lifecycleController.RuntimeState;
        if (runtimeState.currentExecutionPlan != null &&
            runtimeState.currentExecutionPlan.isCompleted)
        {
            return BeginPresentation(
                BattlePresentationCue.ExecutionComplete,
                BattlePresentationContinuation.AfterExecutionComplete,
                null,
                "ExecutionComplete"
            );
        }

        Phase = BattleExecutionRunnerPhase.ItemCompleted;
        failureMessage = string.Empty;
        return true;
    }

    bool CompleteRunner(out string failureMessage)
    {
        if (!lifecycleController.TryCompletePausableExecution(out failureMessage))
        {
            return Fail(failureMessage, out failureMessage);
        }

        IsCompleted = true;
        Phase = BattleExecutionRunnerPhase.Completed;
        return true;
    }

    bool Fail(string message, out string failureMessage)
    {
        HasFailed = true;
        Phase = BattleExecutionRunnerPhase.Failed;
        failureMessage = string.IsNullOrEmpty(message)
            ? "Pausable执行失败"
            : message;
        return false;
    }

    static BattleExecutionItem FindNextPendingItem(BattleExecutionPlan plan)
    {
        if (plan == null || plan.executionItems == null)
        {
            return null;
        }

        foreach (BattleExecutionItem item in plan.executionItems)
        {
            if (item == null || item.status == BattleExecutionItemStatus.Failed)
            {
                return item;
            }

            if (!item.isCompleted && item.status == BattleExecutionItemStatus.Pending)
            {
                return item;
            }
        }

        return null;
    }

    static BattleExecutionItem FindNextPendingItemAfter(
        BattleExecutionPlan plan,
        BattleExecutionItem currentItem
    )
    {
        if (plan == null || plan.executionItems == null ||
            currentItem == null)
        {
            return null;
        }

        bool foundCurrent = false;
        foreach (BattleExecutionItem item in plan.executionItems)
        {
            if (!foundCurrent)
            {
                foundCurrent = object.ReferenceEquals(item, currentItem);
                continue;
            }

            if (item != null && !item.isCompleted &&
                item.status == BattleExecutionItemStatus.Pending)
            {
                return item;
            }
        }

        return null;
    }
}
