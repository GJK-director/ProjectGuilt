# Battle Resolution

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-11

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

Clash/Roll、ResolutionPlan、Impact、伤害修正。

## NOT Responsible

UI、Camera、计划安排。

## Main Entry

BattleResolver / BattleCalculator / BattleClashSession。

## Data

卡牌公式、roll snapshot、impact/modifier。

## Runtime Flow

Execution → Clash/Plan → Roll → Impact → Events。

## Continuous Dodge Resolution Contract

首次正式 Dodge 响应正常提交一次 `CardUsed`，随后成功结果使用 `DeferForContinuousDodge`，保持同一次卡牌使用处于 deferred 状态。后续 Continuous Dodge continuation 不重复提交 `CardUsed`，成功后继续保持 active/deferred。只有失败的正式路径或 `FinalizeActionCardUse` 触发的正式收尾，才提交最终的玩家侧 `CardResolved` 并完成行动槽使用。

## Invariants

使用有效快照；表现读取结果而不重新随机；事件与资源后果不能重复提交。

## Dependencies

Cards、Units、Events、InteractionContext。

## Regression Tests

ClashSession/ResolutionPlan/Generic/FullBattle Legacy。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

SampleScene、正式 Harness；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

CURRENT + DEFERRED_DEBT：BattleResolver；兼容分支按 JIT 处理。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
