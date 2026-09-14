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

## Action Order Contract

`BattleExecutionPlanManager` 先将带 `FirstStrike` 的完整 ExecutionItem 排入 FirstStrike 层；未带该 Trait 的 Ability 与其他普通行动共同属于 Normal 层。Normal 层按有效速度降序排序，响应项使用双方速度较大值；同速响应仅在响应者与敌人速度相等时获得响应优先级。随后使用排序方位的行动槽位、BattleRuntimeState 中的战斗位置和稳定顺序完成确定性排序。已建立的响应 pairing 不会被拆开，未响应的敌方 Defense/Dodge 不会生成独立执行项。

## Impact / Completion Boundary

`BattleExecutionRunner.AdvanceResolutionPending` 按 `BattleImpact.damageImpactDelaySeconds` 推进下一段，并在 `BattlePresentationCue.Impact` 完成后由 `BattleExecutionPlanExecutor` 进入 `BattleResolver.TryCommitNextResolutionStep`。每个 `BattleImpact` 独立提交 HP 和 Damage Number；`BattleExecutionRunner.FinishCurrentItem` 在 Action Presentation 完成后执行 `CommitDefeatCheckpoint`，再提交 `ActionFinished` 和生命周期完成。同步路径由 Resolver drain 同一个 `BattleResolutionPlan`，不另建一套伤害状态机。详细契约见 [Damage](Damage.md)。

## Auto Clash Contract

正式 BattleScene 的配置链是 `BattleScene` → `BattleSceneBootstrap` → `BattleAutoClashController` → `BattleSimpleUIController` → `BattleRollGateSettings` → `BattleExecutionRunner`。`BattleAutoClashController` 是 Inspector 配置 owner，挂在 `BattleSceneBootstrap` GameObject 上；它只负责提供配置，不负责 Update、Coroutine、计时或模拟输入。

新建 `BattleAutoClashController` 组件时的代码默认值是 `Enable Auto Clash = false`、`Auto Clash Delay = 0.1`；当前正式 `BattleScene` 的序列化配置是 `Enable Auto Clash = true`、`Auto Clash Delay = 0`。Scene 的序列化值优先于代码默认值。

`BattleRollMode.Manual` 保留 `WaitingForRoll → Space → TryRequestManualRoll → RollOneAttempt`。`BattleRollMode.Auto` 使用现有 `AutoRollDelay`，由 `BattleExecutionRunner.Advance(deltaTime)` 推进到 `RollOneAttempt`。

Auto Clash Delay 从“正式 Runner 已进入原本允许玩家按 Space Roll 的 Roll Gate”开始。每个新 Gate、Tie 后的下一次 Roll、以及下一 ExecutionItem 都重新等待配置的 Delay；`0` 表示下一次 Runner Advance 时自动 Roll。

Planning Space 不受影响。Auto ON 时，Execution Manual Roll Space 不再进入 `TryRequestManualRoll`。Runner 是 Auto Roll Gate 的唯一 Runtime owner，不存在第二套 Auto Timer。Auto 模式沿用正式 Runner 原本覆盖的 Clash、unilateral roll、Defense 和 Dodge 范围。

Auto Clash v0.1：`USER UNITY MANUAL VERIFIED`。

## Invariants

FirstStrike 改优先级而非拆散 pairing；无效动作完成与实际效果分开；暂停不得重复提交。

## Dependencies

Resolution、Lifecycle、Presentation protocol。

## Regression Tests

FirstStrike Suite 与 Action Order Formal Suite；RollGate/Pausable/Interaction Legacy。两套 Execution 排序测试由 retained 的 `BattleExecutionPlanFirstStrikePolicyTests` caller 统一执行。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

正式 Harness、SampleScene；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

CURRENT + DEFERRED_DEBT：Executor；同步/暂停/fallback 保留。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
