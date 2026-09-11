# Battle UI

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-11

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

局部卡牌/槽位/关系/状态/详情/Roll UI 与输入绑定。

## NOT Responsible

结算、Camera、Bootstrap Context 所有权。

## Main Entry

对应 View/Host；跨 Battle flow 才用 BattleSimpleUIController。

## Data

RuntimeState/CardState/Slot/Intent；Prefab/Scene。

## Runtime Flow

Scene 绑定 → Runtime View → 输入 Router → Planning；执行结果刷新 UI。

## Invariants

View 不自行提交伤害；选择状态与正式安排分开；Data 与 Prefab 配置分别核验。

## Dependencies

Planning、Lifecycle、Spawner、Presentation。

## Regression Tests

UI/关键词/关系线/世界跟随 Legacy。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

BattleScene、Buff Preview、对应操作验收；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

CURRENT + DEFERRED_DEBT：总 Controller；ForTesting seams/Prefab preview 保留。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
