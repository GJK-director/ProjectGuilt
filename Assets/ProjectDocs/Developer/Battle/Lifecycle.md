# Battle Lifecycle

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-11

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

阶段转换、执行启动、回合边界和终局。

## NOT Responsible

单卡全部资源规则、动画实现。

## Main Entry

BattleLifecycleController / BattleTurnProcessor。

## Data

RuntimeState、living participants、plan。

## Runtime Flow

Init → Prepare/PlanReady → Executing → TurnResolved → TurnEnding → TurnEnded → PreparingNextTurn → Prepare；终局走 BattleEnded。

## Continuous Dodge Finalization Boundary

仍处于 active 的 Continuous Dodge 不在首次成功后收尾。`TurnEnd` 与 `BattleEnd` 通过 `BattleContinuousDodgeManager.FinalizeActiveDodges` 进入 `FinalizeActionCardUse`，提交 deferred Dodge 的最终 `CardResolved` 并把行动槽标记为 finalized/used。

## Default Planning Source Selection

进入 `Prepare` 后，`BattleTurnProcessor.StartTurn` 先完成 TurnStart、Pending Buff 应用与速度投掷；表现层完成正式角色 View 与行动槽绑定并刷新 UI 后，`BattleSimpleUIController` 对当前 `currentTurn` 执行一次默认来源选择。候选只来自存活的 `runtimeState.allyUnits`，按当前速度降序、`GetBattlePositionIndex(...)` 升序决定优先级，成功时只选择正式 Slot1。

这是 Planning 表现与输入状态初始化，不是 `BattleTurnProcessor` 的 Gameplay 副作用。玩家改选或清空后，普通 UI 刷新不会重复选择；只有下一次进入新的 `Prepare` 才重新处理。

## HP / Defeat Boundary

`CharacterData.IsDead()` 只表示 `currentHP <= 0`。正式终局使用 `CharacterData.IsDefeated()`；Damage plan 的 `pendingDefeatImpact` 由 `BattleResolver.CommitDefeatCheckpoint` 在 Action / Resolution 完成边界确认，调用 `MarkDefeated` 并触发 `AfterKill`，之后 `EvaluateBattleEnd` 才进入 `BattleEnded`。详见 [Damage](Damage.md)。

## Invariants

进入状态须满足 controller guard；TurnStart/TurnEnd 与卡牌事件各有职责；表现结束与终局判定不能混写。

## Dependencies

Execution、Events、Units。

## Regression Tests

LifecycleController/PhaseContract/Timing/EndLock Legacy。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

完整回合及终局返回；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

同步和 scene-presented 回合入口并存。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
