using System.Collections.Generic;
using UnityEngine;

// 统一协调同步战斗生命周期；具体卡牌结算仍由现有Executor与Resolver负责。
public sealed class BattleLifecycleController
{
    private readonly BattleRuntimeState runtimeState;
    private BattleExecutionRunner executionRunner;

    public BattleRuntimeState RuntimeState
    {
        get { return runtimeState; }
    }

    public BattleExecutionRunner ExecutionRunner
    {
        get { return executionRunner; }
    }

    public BattleLifecycleController(BattleRuntimeState runtimeState)
    {
        this.runtimeState = runtimeState;
    }

    public bool TryBeginPausableExecution(
        BattleRollGateSettings settings,
        out string failureMessage
    )
    {
        if (!ValidateActiveBattle(out failureMessage))
        {
            return false;
        }
        if (runtimeState.currentExecutionPlan == null ||
            runtimeState.currentExecutionPlan.isCompleted)
        {
            failureMessage = "Pausable执行启动失败：当前计划为空或已完成";
            return false;
        }
        if (executionRunner != null &&
            !executionRunner.IsCompleted &&
            !executionRunner.HasFailed)
        {
            failureMessage = "Pausable执行启动失败：当前Runner仍在执行";
            return false;
        }
        if (runtimeState.LifecyclePhase != BattleLifecyclePhase.Prepare &&
            runtimeState.LifecyclePhase != BattleLifecyclePhase.PlanReady)
        {
            failureMessage = "Pausable执行启动失败：当前阶段必须为Prepare或PlanReady";
            return false;
        }

        if (!runtimeState.TryTransitionTo(
                BattleLifecyclePhase.Executing,
                out failureMessage
            ))
        {
            return false;
        }

        executionRunner = new BattleExecutionRunner(this, settings);
        return executionRunner.Begin(out failureMessage);
    }

    public bool AdvancePausableExecution(
        float deltaTime,
        out string failureMessage
    )
    {
        if (executionRunner == null)
        {
            failureMessage = "Pausable执行推进失败：当前没有Runner";
            return false;
        }

        return executionRunner.Advance(deltaTime, out failureMessage);
    }

    public bool TryRequestManualRoll(out string failureMessage)
    {
        if (executionRunner == null)
        {
            failureMessage = "Manual Roll请求失败：当前没有Runner";
            return false;
        }

        return executionRunner.TryRequestManualRoll(out failureMessage);
    }

    public bool TryCommitNextResolutionStep(out string failureMessage)
    {
        if (executionRunner == null)
        {
            failureMessage = "Resolution提交失败：当前没有Runner";
            return false;
        }

        return executionRunner.TryCommitNextResolutionStep(out failureMessage);
    }

    internal bool HandlePausableItemCompleted(out string failureMessage)
    {
        if (!ValidateRuntimeState(out failureMessage) ||
            runtimeState.currentExecutionPlan == null)
        {
            return false;
        }

        EvaluateBattleEnd();
        BattleExecutionPlanExecutor.RefreshPlanCompletionFromRunner(
            runtimeState.currentExecutionPlan
        );
        failureMessage = string.Empty;
        return true;
    }

    internal bool TryCompletePausableExecution(out string failureMessage)
    {
        if (!ValidateRuntimeState(out failureMessage))
        {
            return false;
        }
        if (runtimeState.IsBattleEnded)
        {
            failureMessage = string.Empty;
            return true;
        }
        if (runtimeState.currentExecutionPlan == null ||
            !runtimeState.currentExecutionPlan.isCompleted)
        {
            failureMessage = "Pausable执行完成失败：ExecutionPlan尚未完成";
            return false;
        }
        if (runtimeState.LifecyclePhase == BattleLifecyclePhase.TurnResolved)
        {
            failureMessage = string.Empty;
            return true;
        }
        if (runtimeState.LifecyclePhase != BattleLifecyclePhase.Executing)
        {
            failureMessage = "Pausable执行完成失败：当前阶段必须为Executing";
            return false;
        }

        return runtimeState.TryTransitionTo(
            BattleLifecyclePhase.TurnResolved,
            out failureMessage
        );
    }

    public bool TryInitializeToPrepare(out string failureMessage)
    {
        if (!ValidateRuntimeState(out failureMessage))
        {
            return false;
        }

        if (runtimeState.LifecyclePhase != BattleLifecyclePhase.Init)
        {
            failureMessage =
                "战斗初始化失败：当前阶段必须为Init，当前为" +
                runtimeState.LifecyclePhase;
            return false;
        }

        return runtimeState.TryTransitionTo(
            BattleLifecyclePhase.Prepare,
            out failureMessage
        );
    }

    public bool TryCreateExecutionPlan(
        bool enterPlanReady,
        out BattleExecutionPlan executionPlan,
        out string failureMessage
    )
    {
        executionPlan = null;
        if (!ValidateActiveBattle(out failureMessage))
        {
            return false;
        }
        if (runtimeState.LifecyclePhase != BattleLifecyclePhase.Prepare)
        {
            failureMessage = "创建执行计划失败：当前阶段必须为Prepare";
            return false;
        }
        if (runtimeState.currentExecutionPlan != null)
        {
            failureMessage = "创建执行计划失败：当前已经存在执行计划";
            return false;
        }

        executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            runtimeState.actionSlots,
            runtimeState.intentQueue
        );
        if (executionPlan == null || executionPlan.executionItems == null ||
            executionPlan.executionItems.Count == 0)
        {
            runtimeState.ClearExecutionPlan();
            executionPlan = null;
            failureMessage = "创建执行计划失败：计划为空或没有执行项";
            return false;
        }

        runtimeState.SetExecutionPlan(executionPlan);
        if (!enterPlanReady)
        {
            failureMessage = string.Empty;
            return true;
        }

        if (!runtimeState.TryTransitionTo(
                BattleLifecyclePhase.PlanReady,
                out failureMessage
            ))
        {
            runtimeState.ClearExecutionPlan();
            executionPlan = null;
            return false;
        }

        return true;
    }

    public bool TryExecuteCurrentPlan(out string failureMessage)
    {
        if (!ValidateActiveBattle(out failureMessage))
        {
            return false;
        }
        if (HasActivePausableRunner())
        {
            failureMessage = "执行计划失败：Pausable Runner仍在执行";
            return false;
        }
        if (runtimeState.currentExecutionPlan == null)
        {
            failureMessage = "执行计划失败：当前计划为空";
            return false;
        }
        if (runtimeState.currentExecutionPlan.isCompleted)
        {
            failureMessage = "执行计划失败：当前计划已经完成";
            return false;
        }
        if (runtimeState.LifecyclePhase != BattleLifecyclePhase.Prepare &&
            runtimeState.LifecyclePhase != BattleLifecyclePhase.PlanReady)
        {
            failureMessage =
                "执行计划失败：当前阶段必须为Prepare或PlanReady";
            return false;
        }

        if (!runtimeState.TryTransitionTo(
                BattleLifecyclePhase.Executing,
                out failureMessage
            ))
        {
            return false;
        }

        BattleExecutionPlanExecutor.ExecuteCurrentPlanFromLifecycle(this);
        if (runtimeState.IsBattleEnded)
        {
            failureMessage = string.Empty;
            return true;
        }
        if (!runtimeState.currentExecutionPlan.isCompleted)
        {
            failureMessage = "执行计划失败：仍有未完成执行项";
            return false;
        }

        return runtimeState.TryTransitionTo(
            BattleLifecyclePhase.TurnResolved,
            out failureMessage
        );
    }

    public bool TryExecuteNextItem(
        out bool planCompleted,
        out string failureMessage
    )
    {
        planCompleted = false;
        if (!ValidateRuntimeState(out failureMessage))
        {
            return false;
        }
        if (HasActivePausableRunner())
        {
            failureMessage = "单项执行失败：Pausable Runner仍在执行";
            return false;
        }
        if (runtimeState.currentExecutionPlan == null)
        {
            failureMessage = "单项执行失败：当前计划为空";
            return false;
        }
        if (runtimeState.currentExecutionPlan.isCompleted)
        {
            planCompleted = true;
            failureMessage = "单项执行失败：当前计划已经完成";
            return false;
        }

        // BattleEnded后只允许继续把剩余Item逐项标记为Skipped，不再执行战斗逻辑。
        if (runtimeState.IsBattleEnded)
        {
            bool skipped = BattleExecutionPlanExecutor
                .ExecuteNextItemFromLifecycle(this);
            planCompleted = runtimeState.currentExecutionPlan.isCompleted;
            failureMessage = skipped
                ? string.Empty
                : "单项执行失败：BattleEnded剩余项无法完成跳过";
            return skipped;
        }

        if (runtimeState.LifecyclePhase == BattleLifecyclePhase.Prepare ||
            runtimeState.LifecyclePhase == BattleLifecyclePhase.PlanReady)
        {
            if (!runtimeState.TryTransitionTo(
                    BattleLifecyclePhase.Executing,
                    out failureMessage
                ))
            {
                return false;
            }
        }
        else if (runtimeState.LifecyclePhase !=
            BattleLifecyclePhase.Executing)
        {
            failureMessage =
                "单项执行失败：当前阶段必须为Prepare、PlanReady或Executing";
            return false;
        }

        if (!BattleExecutionPlanExecutor.ExecuteNextItemFromLifecycle(this))
        {
            failureMessage = "单项执行失败：当前Item未能正常完成";
            return false;
        }

        planCompleted = runtimeState.currentExecutionPlan.isCompleted;
        if (runtimeState.IsBattleEnded || !planCompleted)
        {
            failureMessage = string.Empty;
            return true;
        }

        return runtimeState.TryTransitionTo(
            BattleLifecyclePhase.TurnResolved,
            out failureMessage
        );
    }

    public bool TryEndCurrentTurn(out string failureMessage)
    {
        if (!ValidateActiveBattle(out failureMessage))
        {
            return false;
        }
        if (runtimeState.LifecyclePhase != BattleLifecyclePhase.TurnResolved)
        {
            failureMessage = "结束回合失败：当前阶段必须为TurnResolved";
            return false;
        }
        if (runtimeState.currentExecutionPlan == null ||
            !runtimeState.currentExecutionPlan.isCompleted)
        {
            failureMessage = "结束回合失败：ExecutionPlan必须存在且已完成";
            return false;
        }

        if (!runtimeState.TryTransitionTo(
                BattleLifecyclePhase.TurnEnding,
                out failureMessage
            ))
        {
            return false;
        }

        BattleContinuousDodgeManager.FinalizeActiveDodges(
            runtimeState,
            "TurnEnd"
        );
        BattleTurnProcessor.EndTurn(GetLivingTurnParticipants());
        runtimeState.ClearCurrentTurnRuntimeObjects();

        return runtimeState.TryTransitionTo(
            BattleLifecyclePhase.TurnEnded,
            out failureMessage
        );
    }

    public bool TryPrepareNextTurn(
        List<BattleActionSlot> newActionSlots,
        List<BattleEnemyIntent> newIntentQueue,
        out string failureMessage
    )
    {
        if (!ValidateActiveBattle(out failureMessage))
        {
            return false;
        }
        if (runtimeState.LifecyclePhase != BattleLifecyclePhase.TurnEnded)
        {
            failureMessage = "准备下一回合失败：当前阶段必须为TurnEnded";
            return false;
        }
        if (runtimeState.currentExecutionPlan != null &&
            !runtimeState.currentExecutionPlan.isCompleted)
        {
            failureMessage = "准备下一回合失败：ExecutionPlan尚未完成";
            return false;
        }
        List<BattleActionSlot> filteredSlots = FilterLivingActionSlots(
            newActionSlots
        );
        if (filteredSlots.Count == 0)
        {
            EvaluateBattleEnd();
            failureMessage = runtimeState.IsBattleEnded
                ? "战斗已结束，不能准备空的下一回合"
                : "没有存活角色行动槽位，不能准备下一回合";
            return false;
        }
        if (newIntentQueue == null)
        {
            failureMessage = "准备下一回合失败：新敌人意图队列为空";
            return false;
        }
        for (int index = 0; index < newIntentQueue.Count; index++)
        {
            if (newIntentQueue[index] == null)
            {
                failureMessage =
                    "准备下一回合失败：新敌人意图包含空项，索引=" + index;
                return false;
            }
        }

        if (!runtimeState.TryTransitionTo(
                BattleLifecyclePhase.PreparingNextTurn,
                out failureMessage
            ))
        {
            return false;
        }

        runtimeState.AdvanceTurn();
        BattleTurnProcessor.StartTurn(GetLivingTurnParticipants());
        runtimeState.SetActionSlots(filteredSlots);
        runtimeState.SetIntentQueue(newIntentQueue);
        runtimeState.ClearExecutionPlan();

        return runtimeState.TryTransitionTo(
            BattleLifecyclePhase.Prepare,
            out failureMessage
        );
    }

    public BattleResult EvaluateBattleEnd()
    {
        if (runtimeState == null || runtimeState.IsBattleEnded)
        {
            return runtimeState != null
                ? runtimeState.battleResult
                : BattleResult.None;
        }

        bool allyADead = runtimeState.allyA != null &&
            runtimeState.allyA.IsDead();
        bool allyBDead = runtimeState.allyB != null &&
            runtimeState.allyB.IsDead();
        BattleResult result = BattleResult.None;
        if (allyADead && allyBDead)
        {
            result = BattleResult.Defeat;
        }
        else if (AreAllRegisteredUnitsDead(runtimeState.enemyUnits))
        {
            result = BattleResult.Victory;
        }

        if (result == BattleResult.None)
        {
            return runtimeState.battleResult;
        }

        string failureMessage;
        if (!runtimeState.TryTransitionTo(
                BattleLifecyclePhase.BattleEnded,
                out failureMessage
            ))
        {
            Debug.LogError(failureMessage);
            return runtimeState.battleResult;
        }

        runtimeState.battleResult = result;
        BattleContinuousDodgeManager.FinalizeActiveDodges(
            runtimeState,
            "BattleEnded"
        );
        Debug.Log("检测到战斗结束：" + runtimeState.battleResult);
        // BattleEnded是终局权威状态；只有首次成功转换到该阶段才输出结束表现。
        Debug.Log("游戏结束");
        return runtimeState.battleResult;
    }

    private bool ValidateRuntimeState(out string failureMessage)
    {
        if (runtimeState == null)
        {
            failureMessage = "生命周期操作失败：BattleRuntimeState为空";
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

    private bool HasActivePausableRunner()
    {
        return executionRunner != null &&
            !executionRunner.IsCompleted &&
            !executionRunner.HasFailed;
    }

    private bool ValidateActiveBattle(out string failureMessage)
    {
        if (!ValidateRuntimeState(out failureMessage))
        {
            return false;
        }
        if (runtimeState.IsBattleEnded)
        {
            failureMessage = "生命周期操作失败：战斗已经结束";
            return false;
        }

        return true;
    }

    private List<BattleActionSlot> FilterLivingActionSlots(
        List<BattleActionSlot> slots
    )
    {
        List<BattleActionSlot> filteredSlots = new List<BattleActionSlot>();
        if (slots == null)
        {
            return filteredSlots;
        }

        int filteredCount = 0;
        foreach (BattleActionSlot slot in slots)
        {
            if (slot == null || slot.owner == null || slot.owner.IsDead() ||
                (slot.actor != null && slot.actor.IsDead()))
            {
                filteredCount++;
                continue;
            }
            filteredSlots.Add(slot);
        }

        if (filteredCount > 0)
        {
            Debug.Log(
                "准备下一回合：过滤死亡或无效角色槽位数量：" +
                filteredCount
            );
        }
        return filteredSlots;
    }

    private List<CharacterData> GetLivingTurnParticipants()
    {
        List<CharacterData> participants = new List<CharacterData>();
        AddLivingParticipants(participants, runtimeState.allyUnits);
        AddLivingParticipants(participants, runtimeState.enemyUnits);
        return participants;
    }

    private static void AddLivingParticipants(
        List<CharacterData> participants,
        List<CharacterData> characters
    )
    {
        if (participants == null || characters == null)
        {
            return;
        }

        foreach (CharacterData character in characters)
        {
            if (character == null || character.IsDead() ||
                ContainsReference(participants, character))
            {
                continue;
            }
            participants.Add(character);
        }
    }

    private static bool ContainsReference(
        List<CharacterData> characters,
        CharacterData target
    )
    {
        foreach (CharacterData character in characters)
        {
            if (object.ReferenceEquals(character, target))
            {
                return true;
            }
        }
        return false;
    }

    private static bool AreAllRegisteredUnitsDead(List<CharacterData> units)
    {
        if (units == null || units.Count == 0)
        {
            return false;
        }

        bool foundUnit = false;
        foreach (CharacterData unit in units)
        {
            if (unit == null)
            {
                continue;
            }
            foundUnit = true;
            if (!unit.IsDead())
            {
                return false;
            }
        }
        return foundUnit;
    }
}
