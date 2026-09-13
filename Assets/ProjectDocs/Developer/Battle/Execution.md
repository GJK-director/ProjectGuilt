# Battle Execution

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-11

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

计划排序、执行项、暂停/恢复与完成。

## NOT Responsible

最终数值公式、Scene 视觉细节。

## Main Entry

BattleExecutionPlanManager / Executor / Runner。

## Data

ExecutionPlan/Item、ActionSlot、Intent、Context。

## Runtime Flow

Lifecycle → Plan → Runner/Executor → Resolver + Presentation completion。

## Auto Clash Contract

正式 BattleScene 的 Auto Clash 配置 owner 是 `BattleAutoClashController`，挂在 `BattleSceneBootstrap` GameObject 上；它只负责 Inspector 配置，不负责 Update、Coroutine、计时或模拟输入。

`BattleRollMode.Manual` 保留 `WaitingForRoll → Space → TryRequestManualRoll → RollOneAttempt`。`BattleRollMode.Auto` 使用现有 `AutoRollDelay`，由 `BattleExecutionRunner.Advance(deltaTime)` 推进到 `RollOneAttempt`。

Auto Clash Delay 从“正式 Runner 已进入原本允许玩家按 Space Roll 的 Roll Gate”开始。每个新 Gate、Tie 后的下一次 Roll、以及下一 ExecutionItem 都重新等待配置的 Delay；`0` 表示下一次 Runner Advance 时自动 Roll。

Planning Space 不受影响。Auto ON 时，Execution Manual Roll Space 不再进入 `TryRequestManualRoll`。Runner 是 Auto Roll Gate 的唯一 Runtime owner，不存在第二套 Auto Timer。Auto 模式沿用正式 Runner 原本覆盖的 Clash、unilateral roll、Defense 和 Dodge 范围。

Auto Clash v0.1：`USER UNITY MANUAL VERIFIED`。

## Invariants

FirstStrike 改优先级而非拆散 pairing；无效动作完成与实际效果分开；暂停不得重复提交。

## Dependencies

Resolution、Lifecycle、Presentation protocol。

## Regression Tests

FirstStrike Suite；RollGate/Pausable/Interaction Legacy。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

正式 Harness、SampleScene；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

CURRENT + DEFERRED_DEBT：Executor；同步/暂停/fallback 保留。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
