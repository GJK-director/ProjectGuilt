// Phase 3.3：可暂停的执行推进器。只控制等待和Roll时机，不负责表现或战斗结算规则。
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
    ItemCompleted,
    Completed,
    Failed
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

    public BattleExecutionItem CurrentItem { get; private set; }
    public BattleClashSession CurrentClashSession { get; private set; }
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
        BattleRollGateSettings settings
    )
    {
        this.lifecycleController = lifecycleController;
        this.settings = settings != null
            ? settings.CloneNormalized()
            : new BattleRollGateSettings();
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
        if (Phase == BattleExecutionRunnerPhase.ItemCompleted)
        {
            CurrentItem = null;
            CurrentClashSession = null;
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

        if (Phase != BattleExecutionRunnerPhase.WaitingForRoll ||
            CurrentClashSession == null)
        {
            // 提前请求直接拒绝且不缓存，ReadyPause结束后必须重新请求。
            failureMessage = "Manual Roll请求失败：当前尚未进入WaitingForRoll";
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
        CurrentClashSession = null;
        CurrentItemCompleted = false;

        if (CurrentItem == null)
        {
            if (plan != null && plan.isCompleted)
            {
                return CompleteRunner(out failureMessage);
            }

            return Fail("Pausable执行失败：没有可推进的ExecutionItem", out failureMessage);
        }

        if (runtimeState.IsBattleEnded ||
            CurrentItem.executionType != BattleExecutionItemType.RespondedEnemyIntent)
        {
            bool executed = BattleExecutionPlanExecutor
                .ExecuteNextItemFromLifecycle(lifecycleController);
            if (!executed || !CurrentItem.isCompleted)
            {
                return Fail("Pausable非Clash Item未能按同步路径完成", out failureMessage);
            }

            return FinishCurrentItem(out failureMessage);
        }

        bool itemCompleted;
        if (!BattleExecutionPlanExecutor.TryBeginPausableRespondedEnemyIntent(
                CurrentItem,
                runtimeState,
                out BattleClashSession session,
                out itemCompleted,
                out failureMessage
            ))
        {
            return Fail(failureMessage, out failureMessage);
        }

        if (itemCompleted)
        {
            return FinishCurrentItem(out failureMessage);
        }

        CurrentClashSession = session;
        EnterReadyPause();
        return true;
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
            bool finalized = BattleExecutionPlanExecutor
                .FinalizePausableRespondedEnemyIntent(
                    CurrentItem,
                    lifecycleController.RuntimeState,
                    CurrentClashSession
                );
            if (!finalized || CurrentItem == null || !CurrentItem.isCompleted)
            {
                return Fail("Clash Finalize失败：当前Item未完成", out failureMessage);
            }

            return FinishCurrentItem(out failureMessage);
        }

        if (!CurrentClashSession.RequiresAnotherRoll)
        {
            return Fail("Clash Attempt结束后既未Finalized也未请求下一次Roll", out failureMessage);
        }

        // AttackTie只回到新一轮ReadyPause，本次请求不会继续产生第二个Attempt。
        EnterReadyPause();
        return true;
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
            return CompleteRunner(out failureMessage);
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
}
