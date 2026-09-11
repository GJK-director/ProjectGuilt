# Battle Cards

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-11

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

卡牌定义、实例、使用 eligibility、资源与 manifest。

## NOT Responsible

最终伤害、UI 布局、Scene 编排。

## Main Entry

BattleCardManager / BattleCardState / BattleDeckManifest。

## Data

CardsTest；preset manifest。

## Runtime Flow

Loader → Factory/Bootstrap → CardState → CardUsed/Effects。

## Invariants

实例不可按同名混同；正式使用后果经 CardUsed 提交；Ability 不进入 clash；显式 preset 与默认牌来源区分。

## Dependencies

Events、Buff/资源、Resolver。

## Regression Tests

Cards/Bootstrap/FirstStrike Suite；资源/Ability Legacy。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

SampleScene/BattleScene；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

CardTestData 兼容字段及 direct-call seam；不按 Test 命名退役。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
