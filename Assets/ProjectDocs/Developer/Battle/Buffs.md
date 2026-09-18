# Battle Buffs

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-16

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

定义、运行态、Pending、消费生命周期与 Buff UI。

## NOT Responsible

完整卡牌效果、表现视觉状态。

## Main Entry

CharacterData / BuffData / PendingBuffData；BuffDefinitionLoader 独立文件。

## Data

BuffDefinitions、canonical `stack/intensity`、Pending mutation 与 consume rule。

## Runtime Flow

Loader → Factory/Effects → CharacterData → Turn/Events → UI。

## Invariants

同一角色的同一 `buffID` 只有一份 canonical `BuffData`；延迟生效与当前状态分开；回合开始先处理 Pending，再投速度；不得重复提交资源效果。Gameplay 查询可以按 `buffID` 聚合，但不能用 Pending 命令或临时快照替代 canonical state。

## Canonical Runtime State

`CharacterData.AddBuff(buffID, stackDelta, intensityDelta)` 把同一角色与 `buffID` 的状态写入唯一 `BuffData`。第一次加入使用定义的 `defaultIntensity`；普通重复加入只增加 `stack`；只有显式 `intensityDelta` 才改变 `intensity`。`GetBuffStack(...)` 与 `GetBuffIntensity(...)` 分别读取这两个语义，代码不会把 `stack` 与 `intensity` 隐式相乘。`maxStacks` 与 `maxIntensity`（大于 0 时）由 `CharacterData` 统一限制。

`CharacterData.EnsureBuffState(buffID)` 是运行时确保 canonical state 存在的入口：已有 state 时不改写 `stack` 或 `intensity`；缺少 state 时按定义的 `defaultIntensity` 创建 0 层 state，并应用定义的 intensity clamp。它不改变 `AddBuff(..., stackDelta = 0)` 的 no-op 语义，也不是 `SetInitialBuffState(...)` 的替代名称。

`PendingBuffData` 是未来要执行的 mutation command，不是未来的 Active instance。每次 `AddPendingBuff(...)` 都保留一条独立排期；`ApplyPendingBuffsAtTurnStart()` 到期后把它应用到 canonical state，并按 `applyTimes` / `intervalTurns` 继续或移除。Pending 可以有多条，但 Active 仍保持每个 `buffID` 一份。

## Zero-Stack Lifecycle

`BuffDefinitionData.retainWhenZero` 是通用的归零策略：默认 `false`，消费后 `stack` 到 0 时删除 canonical state；`true` 则保留 0 层 state。Bullet / Anger 通过定义接入，不使用 `buffID` 生命周期特判。`CharacterData.ClearBuff(...)` 不受 Retain 影响，会真正删除对应 state。

Anger 是 retained-zero resource：愤怒卡启用机制时确保 Anger state 存在但不凭空增加层数；后续可以经历 `0 → N → 0`，普通消费归零后仍保留 state。Iai 的“清空怒”只把当前 Anger stacks 消耗到 0，也保留 state；Battle reset 或明确的 `ClearBuff(Anger)` 仍然可以删除旧 state。

Runtime 保留与 UI 显示是两件事。`BuffDefinitionData.showWhenZero` 是独立显示策略；`BattleBuffGroupUIView.BuildDisplayEntries(...)` 只在 `stack <= 0` 且没有 `showWhenZero` 时过滤，因而可以显示明确允许展示的 0 层状态。

## Consume-On-Trigger Lifecycle

正式 Buff 生命周期由定义的 `retainWhenZero` 与 `consumeRule` 表达；旧的 per-instance duration / expire-rule 模型不再是当前 Runtime contract。`consumeRule = None` 的状态不会被通用触发消费；其他规则由正式事件边界显式调用 `ConsumeBuffStackByRule(...)`、`ConsumeBuffsByRule(...)` 或资源路径 `TryConsumeBuffStackAsResource(...)`。每次消费只减少 canonical state 的 `stack`，然后统一应用 zero-stack policy。

`NextClashPointUp` 与 `NextCardPointUp` 是“每个匹配正式事件最多消费 1 层”的通用触发规则；`intensity` 是每次事件的点数加成，`stack` 是剩余触发次数。`Conservation` 使用同一原则的 `consumeRule = NextEligibleShootingCardUsed`，是这个可复用规则的正式实例，不是 Conservation 专属生命周期契约。`BattleConservationRules.HandleEvent(...)` 只在已 Commit 的 eligible shooting card 的 `CardUsed` 事件消费 1 层；`TryAssignPendingBonus(...)` 只保存该张卡的 card-local 解析快照。普通卡、预览、拖拽、取消和未 Commit 行动不消费；TurnEnd 的 penalty 与消费/清除生命周期分开，TurnEnd 不删除或 StackDown Conservation。

## Dependencies

Turn、Events、Effects。

## Regression Tests

当前回归入口包括 `CardLoadTest` 的 Mode49 Buff lifecycle 子测试、`BattleConservationAbilityTests` 的 Mode113 retained wrapper，以及 `BattleResourceSpecialStateNormalizationTests` 的 Mode128 资源状态回归。覆盖 canonical Active state、独立 Pending mutation command、zero-stack Remove/Retain、show-when-zero、Conservation 的 TurnEnd 保留与正式射击卡逐层消费；实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

Buff Preview、BattleScene；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

旧 timing 与事件链并存，Preview 绑定 Prefab。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
