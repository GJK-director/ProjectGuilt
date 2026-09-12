# Enemy Intent

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-11

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

定义生成、pattern/cycle、目标槽位与意图模型。

## NOT Responsible

玩家输入、伤害公式、未来 AI 决策。

## Main Entry

BattleDefinitionBootstrap.CreateIntentQueueForTurn。

## Data

EncounterDefinitions、EnemyDefinitions/cardIDs。

## Runtime Flow

Bootstrap provider → IntentQueue → ActionSlots → ExecutionPlan。

## Invariants

按实际回合选择 cycle；保留 pattern fallback；重复卡条目创建独立 RuntimeState。

## Dependencies

Bootstrap、Definitions、Actions。

## Regression Tests

EnemyIntent Suite；Mode103 provider 集成。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

逐回合检查 BattleScene 目标；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

固定兼容意图存在于自动回合支持；生成 owner 不在 EnemyIntent 目录。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
