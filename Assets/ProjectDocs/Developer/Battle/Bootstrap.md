# Battle Bootstrap

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-11

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

Definition 组装、RuntimeState、下一回合 provider。

## NOT Responsible

Resolver 公式、UI 布局、JSON 设计。

## Main Entry

BattleSceneBootstrap.InitializeBattleScene / BattleDefinitionBootstrap。

## Data

Character/Enemy/Encounter/Cards；deck preference。

## Runtime Flow

CreateRuntimeState → 可选 Harness 准备 → UI.InitializeFromRuntimeState；保留 activeBootstrapResult。

## Invariants

防止重复初始化；正式 provider 由 Bootstrap 持有；显式 Debug 初始化不是默认；preset 不修改源 Definition。

## Dependencies

Loaders、UnitFactory、Settings、UI。

## Regression Tests

Bootstrap Suite、Mode103、Mode133。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

BattleScene/正式 Harness；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

CURRENT + DEFERRED_DEBT；正式/Debug/注入并存。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
