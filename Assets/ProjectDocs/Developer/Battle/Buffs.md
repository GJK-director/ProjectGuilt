# Battle Buffs

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-11

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

定义、运行态、Pending、过期与 Buff UI。

## NOT Responsible

完整卡牌效果、表现视觉状态。

## Main Entry

CharacterData / BuffData / PendingBuffData；BuffDefinitionLoader 独立文件。

## Data

BuffDefinitions、stack/duration/timing。

## Runtime Flow

Loader → Factory/Effects → CharacterData → Turn/Events → UI。

## Invariants

延迟生效与当前状态分开；回合开始先处理 Pending，再投速度；不得重复提交资源效果。

## Dependencies

Turn、Events、Effects。

## Regression Tests

Buff/Conservation/资源 Legacy；Buffs Suite 为 placeholder。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

Buff Preview、BattleScene；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

旧 timing 与事件链并存，Preview 绑定 Prefab。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
