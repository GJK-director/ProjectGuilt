# Battle Units

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-11

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

角色状态、Definition→实例、与视图身份衔接。

## NOT Responsible

卡牌定义编辑、动画编排。

## Main Entry

BattleUnitFactory / CharacterData；视图由 BattleUnitViewSpawner。

## Data

Character/EnemyDefinitions、Cards；Scene Prefab 字段。

## Runtime Flow

Loader → Factory → CharacterData/CardState → Spawner。

## Invariants

Runtime 实例与 Definition 分离；显式 cardIDs 不修改原定义；prefabKey 不是 Spawner 自动选择器。

## Dependencies

Cards、Buffs、Data、UI。

## Regression Tests

DefaultCard/Binding Legacy；Bootstrap Suite。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

角色、状态跟随与实例绑定；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

固定角色兼容引用保留；数据与视觉配置分离。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
