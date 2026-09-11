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
