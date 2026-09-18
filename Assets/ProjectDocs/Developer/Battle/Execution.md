# Battle Execution

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-15

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

计划排序、执行项、暂停/恢复与完成。

## NOT Responsible

最终数值公式、Scene 视觉细节。

## Main Entry

BattleActionOrderResolver / BattlePlanningOrderSnapshot / BattleExecutionPlanManager / Executor / Runner。

## Data

ExecutionPlan/Item、ActionSlot、Intent、Context。

## Runtime Flow

Lifecycle → Plan → Runner/Executor → Resolver + Presentation completion。

## Action Order Contract

`BattleActionOrderResolver` 是 Action Order 的 shared authoritative sorter。它只读取当前 Planning 状态，生成并排序 candidates，不回写 `ActionSlot`、`BattleEnemyIntent` 或 `BattleRuntimeState`。`BattleExecutionPlanManager` 将 candidates 转换为 `ExecutionItem`；`BattlePlanningOrderSnapshot` 复用同一 Resolver 结果作为只读 Planning projection，不在各自实现第二套排序规则。

正式 priority tier 只有两层：`FirstStrike` 与 `Normal`。只有带 `FirstStrike` Trait 的完整候选进入 FirstStrike 层；未带该 Trait 的 Ability 与其他普通行动共同属于 Normal 层。Ability 不会因为卡牌类型自动获得 FirstStrike。

Normal 层按 effective speed 降序排序。Responded Item 的 `effectiveSpeed = max(response actor speed, enemy speed)`。当 response actor speed > enemy speed 时，`orderingActor` 为 response actor，`orderingSlot` 为 response player slot；当 response actor speed <= enemy speed 时，`orderingActor` 为 enemy，`orderingSlot` 为 enemy `enemySlotIndex`。只有 response actor speed == enemy speed 的 true equal-speed response 才有 `responsePriority = 0`，其他为 `1`。

正式 comparator 依次使用：1. `priorityTier`；2. 双方均为 FirstStrike 时 `firstStrikeSourceSequence` descending；3. `effectiveSpeed` descending；4. 当双方均为 Normal 时，按执行类型优先级 ascending：`FreeAction` 与 `UnrespondedEnemyIntent` 为 unilateral/unopposed，优先级 `0`；`RespondedEnemyIntent` 为 responded，优先级 `1`；5. `actionSlotOrder` ascending；6. `actorPositionOrder` ascending；7. `stableOrder`。`actorPositionOrder` 在 `runtimeState` 可用时来自 `runtimeState.GetBattlePositionIndex(...)`。Normal comparator 不使用 `responsePriority` 或 `actionAssignmentSequence` 决定响应与 unilateral 的先后；FirstStrike 仍保留既有后续 metadata tie-break。

正式 tier 只有 FirstStrike / Normal。普通 Ability 属于 Normal，Ability + FirstStrike 属于 FirstStrike；Responded Pair 任一参与卡带 FirstStrike，整个 Pair 进入 FirstStrike tier，pairing 不拆。对拥有 player `assignmentSequence` 的 FirstStrike，后安排者优先；enemy prefilled FirstStrike 没有 player sequence 时，继续使用后续确定性排序键。

多个 FirstStrike 可以合法存在于同一角色的不同槽位。未响应的敌方 Defense/Dodge 不生成独立执行项。

## Planning Snapshot

`BattlePlanningOrderSnapshot` 只读地复用 `BattleActionOrderResolver`，为 Planning UI 提供 display order，不改变 `BattleActionSlot`、`BattleEnemyIntent` 或 `BattleRuntimeState`。Ally empty 返回 `null`；Ally filled but Planning inactive 返回 `0`；active scheduled candidate 返回 positive。Enemy active Attack 返回 positive；unresponded Defense / Dodge 返回 `0`。Formal Responded Pair 中，Ally responder 与 Enemy Intent 必须共享同一个 positive order，不论 Responded 内卡型是 Attack / Defense / Dodge。

`0` 表示不作为独立 active Planning queue item，不代表 Runtime 永远不会参与 interaction，也不消耗 positive numbering。正整数始终连续，例如 `0, 1, 2`，不是 `0, 2, 3`。

## Continuous Dodge Continuation Selection

Active Continuous Dodge 是已经开始但尚未 finalize 的同一次 Card Use。后续 Enemy Intent 选择 active Dodge 时，属于同一次 deferred use 的 continuation，不重新执行普通 Card Play Eligibility；首次 `CardUsed` 产生的 cooldown 不阻止这次 continuation，也不再次提交 `CardUsed` 或重新设置 cooldown。普通未激活的 Dodge 和其他卡牌仍严格遵守普通 cooldown 与 `EvaluateCardEligibility` 规则。

## Impact / Completion Boundary

`BattleExecutionRunner.AdvanceResolutionPending` 按 `BattleImpact.damageImpactDelaySeconds` 推进下一段，并在 `BattlePresentationCue.Impact` 完成后由 `BattleExecutionPlanExecutor` 进入 `BattleResolver.TryCommitNextResolutionStep`。每个 `BattleImpact` 独立提交 HP 和 Damage Number；`BattleExecutionRunner.FinishCurrentItem` 在 Action Presentation 完成后执行 `CommitDefeatCheckpoint`，再提交 `ActionFinished` 和生命周期完成。同步路径由 Resolver drain 同一个 `BattleResolutionPlan`，不另建一套伤害状态机。详细契约见 [Damage](Damage.md)。

## Auto Clash Contract

正式 BattleScene 的配置链是 `BattleScene` → `BattleSceneBootstrap` → `BattleAutoClashController` → `BattleSimpleUIController` → `BattleRollGateSettings` → `BattleExecutionRunner`。`BattleAutoClashController` 是 Inspector 配置 owner，挂在 `BattleSceneBootstrap` GameObject 上；它只负责提供配置，不负责 Update、Coroutine、计时或模拟输入。

新建 `BattleAutoClashController` 组件时的代码默认值是 `Enable Auto Clash = false`、`Auto Clash Delay = 0.1`；当前正式 `BattleScene` 的序列化配置是 `Enable Auto Clash = true`、`Auto Clash Delay = 0`。Scene 的序列化值优先于代码默认值。

`BattleRollMode.Manual` 保留 `WaitingForRoll → Space → TryRequestManualRoll → RollOneAttempt`。`BattleRollMode.Auto` 使用现有 `AutoRollDelay`，由 `BattleExecutionRunner.Advance(deltaTime)` 推进到 `RollOneAttempt`。

Auto Clash Delay 从“正式 Runner 已进入原本允许玩家按 Space Roll 的 Roll Gate”开始。每个新 Gate、Tie 后的下一次 Roll、以及下一 ExecutionItem 都重新等待配置的 Delay；`0` 表示下一次 Runner Advance 时自动 Roll。

Planning Space 不受影响。Auto ON 时，Execution Manual Roll Space 不再进入 `TryRequestManualRoll`。Runner 是 Auto Roll Gate 的唯一 Runtime owner，不存在第二套 Auto Timer。Auto 模式沿用正式 Runner 原本覆盖的 Clash、unilateral roll、Defense 和 Dodge 范围。

Auto Clash v0.1：`USER UNITY MANUAL VERIFIED`。

## Invariants

FirstStrike 改优先级而非拆散 pairing；无效动作完成与实际效果分开；暂停不得重复提交。

## Dependencies

Resolution、Lifecycle、Presentation protocol。

## Regression Tests

FirstStrike Suite、Planning Order Snapshot Suite、Action Order Formal Suite 与 Action Order View Suite；RollGate/Pausable/Interaction Legacy。Execution 排序测试由 retained 的 `BattleExecutionPlanFirstStrikePolicyTests` caller 和 Mode105 retained caller 统一执行。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

正式 Harness、SampleScene；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

CURRENT + DEFERRED_DEBT：Executor；同步/暂停/fallback 保留。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
