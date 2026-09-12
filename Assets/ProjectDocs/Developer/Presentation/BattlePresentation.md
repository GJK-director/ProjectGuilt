# Battle Presentation

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-11

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

request/completion、路由、Player、Profile、回合表现。

## NOT Responsible

规则随机、伤害计算、资源提交。

## Main Entry

BattleSceneExecutionPresenter / Router / Players。

## Data

Protocol/InteractionContext、Profile assets。

## Runtime Flow

Runner → request → Scene Presenter → Player/Camera/Character → completion。

## Invariants

视觉不重排规则提交；完成/取消只走对应协议；动态 Player 也是真实 consumer。

## Dependencies

Execution protocol、Camera、Character、UI。

## Regression Tests

Protocol/Engagement/Binding/Pausable Legacy。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

正式 Harness、Sandbox；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

CURRENT + DEFERRED_DEBT：Scene Presenter；Sandbox Scene-bound。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
