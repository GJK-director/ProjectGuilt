# Battle Damage

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-14

路径与绑定 owner：[CodeMap](../CodeMap.md)。卡牌入口见 [Cards](Cards.md)。执行边界见 [Execution](Execution.md)，终局边界见 [Lifecycle](Lifecycle.md)，表现入口见 [BattlePresentation](../Presentation/BattlePresentation.md)，伤害数字 UI 见 [BattleUI](../UI/BattleUI.md)。

这是当前 Damage / Impact 的正式说明。本文记录已存在的 Runtime 行为、数据来源和所有权；未实现的扩展明确标为 `RESERVED / NOT IMPLEMENTED`。

## CURRENT CONTRACT

当前正式路径是：

```
Roll / Clash
→ BattleResolutionPlan
→ Build BattleImpact(s)
→ Presentation Impact Gate
→ BattleExecutionRunner
→ BattleResolver.CommitImpact
→ Damage Calculation
→ DamageModifier
→ BattleImpact.resolvedDamage
→ CharacterData.TakeDamage
→ BattleImpact.actualDamage
→ Impact Commit Observer
→ HP UI / Damage Number
→ Next Impact
→ Action Presentation Complete
→ Defeat Checkpoint
→ Lifecycle Battle-End Evaluation
```

`BattleResolver` 拥有正式 Combat Commit。表现层可以决定何时到达 Impact Commit Gate，但不重新计算伤害、不重新随机、不直接写 HP。

`BattleImpact` 是一段独立的正式伤害事实。当前关键字段为：

| Field | 当前语义 |
|---|---|
| `attacker` | 造成本段 Impact 的角色 |
| `target` | 本段受击角色 |
| `sourceCardState` | 产生本段 Impact 的精确运行时卡牌实例 |
| `impactIndex` | 本计划中的 Impact 索引，从 `0` 开始 |
| `damageMultiplierPercent` | 本段在计算时使用的伤害倍率 |
| `damageImpactDelaySeconds` | 本段在上一段提交后等待的时间；第 0 段不等待 |
| `resolvedDamage` | `DamageModifier` 完成后、HP Clamp 前的正式本段伤害 |
| `actualDamage` | `CharacterData.TakeDamage` 后实际减少的 HP |
| `committedDamage` | 当前提交记录的实际 HP 伤害，当前与 `actualDamage` 同步 |
| `didHit` | 本段是否触发命中事实事件 |
| `didKill` | 本段是否在 Defeat Checkpoint 被确认击杀 |
| `state` | `Pending`、`Committed` 或 `Skipped` |
| `runtimeInteraction` | 本段所属的正式 `BattleRuntimeInteraction` identity |

## DATA MODEL

`CardTestData` 的多段字段是：

- `damageDistributionMode`：`Independent` 或 `Cumulative`；缺省通过 `BattleDamageDistributionMode.ResolveOrDefault` 视为 `Independent`。
- `damageImpactPercents`：Gameplay 伤害段百分比。缺省或空数组由 `BattleDamageDistribution.GetSegmentCount` 视为 1 段。
- `damageImpactDelaySeconds`：每个正式 Impact 的提交间隔；第 0 段不读取等待值。
- `hpDisplayStageCount`：兼容性的单个 Impact HP 表现分段字段，不能决定 Combat Impact 段数。多段计划会将每个 Impact 的 `hpDisplayStageCount` 设为 1。

`BattleResolver.AddDamageImpacts` 读取这些字段创建 `BattleImpact`。缺少百分比时段百分比回退为 `100`；负百分比用 `Mathf.Max(0, ...)` 归零。缺少或不足的 delay 项不增加等待，已提供的负 delay 也会被夹到 `0`。累计模式在段数大于 1 且 delay 数组不足时输出 Warning，但仍建立可用的 Impact 计划。ALL IN 还会把段数限制为 captured Bullet 数量。

`BattleImpact.usesPrecalculatedDamage` 只在正式建计划时已有预计算分段伤害时使用；否则提交时由 `BattleCalculator.GetFinalDamageScaled` 和对应分布模式计算。

### resolvedDamage 与 actualDamage

`resolvedDamage` 和 `actualDamage` 不是同一个概念：

```
目标 HP = 5
正式 resolvedDamage = 20
CharacterData.TakeDamage(20)
→ currentHP = 0
→ actualDamage = 5
→ Damage Number 显示 20
```

过量伤害不会被当作实际扣血数字。HP Apply 与 Damage Number 都来自同一次已提交 Impact；Damage Number 不自行读取或计算 `actualDamage`。

当前内部伤害换算由 `BattleCalculator.ConvertScaledDamageToHPDamage` 统一执行非负整数向下取整：`550` scaled damage → `5` HP。这个规则属于全局 scaled damage 转 HP 的当前行为，不能在单张卡的文档或 UI 层另行取整。

## RUNTIME FLOW

### 建立计划

`BattleResolver.BuildAttackResolutionPlan`、`BuildDefenseResolutionPlan`、`BuildDodgeResolutionPlan` 和 free-action 对应路径建立 `BattleResolutionPlan`。胜负、Full Block、DodgeSuccess 等规则结果先落在 plan；真正的伤害段由 `AddDamageImpacts` 写入 `plan.impacts`。

Attack-v-Attack 的获胜攻击通常建立一个或多个 `BattleImpact`。Attack-v-Defense 的 Reduced Damage 使用 Session 的 `RemainingAttackPoint` 建立 Impact；Full Block 仍可有一个不允许伤害、可用于接触表现的 Impact，因此不会产生 HP 伤害事件。DodgeSuccess 不建立伪造 Impact。

### 提交一段 Impact

`BattleExecutionRunner.AdvanceResolutionPending` 取得 `CurrentResolutionPlan.GetNextPendingImpact()`，先处理该 Impact 的 `damageImpactDelaySeconds`，再进入 `BattlePresentationCue.Impact`。表现完成后，`BattleExecutionRunner.CommitOneResolutionStep` 调用 `BattleExecutionPlanExecutor.TryCommitPausableResolutionStep`，最终进入 `BattleResolver.TryCommitNextResolutionStep` 和 `CommitImpact`。

`CommitImpact` 的正式顺序是：

1. 激活尚未激活的 plan，并拒绝重复提交已完成 Impact。
2. 计算候选本段伤害；若有 `usesPrecalculatedDamage` 则使用预计算值。
3. 以 `BattleTiming.DamageModifier` 广播可修改的本段候选伤害，并将结果夹为非负值。
4. 写入 `impact.resolvedDamage`。
5. 如果 `shouldTriggerHit`，写入 `didHit` 并广播 `BattleTiming.Hit`。
6. 通过 `CharacterData.TakeDamage` 写入 HP，计算 `actualDamage` 和 `committedDamage`。
7. 有实际 HP 损失时执行当前 Anger incoming-damage 规则。
8. 广播 `BattleTiming.AfterDamage`，其 `damage` 是实际 HP 损失。
9. 本段把目标打到 HP 0 时登记 `pendingDefeatImpact`，但此时仍未把目标标为 `IsDefeated`。

一个 plan 可被逐段提交。`BattleResolver.TryCommitNextResolutionStep` 在仍有 pending Impact 时返回未完成；没有 pending Impact 后，按调用路径进入 Defeat Checkpoint，再执行 `CompleteResolution`。同步入口 `CommitResolutionSynchronously` 自动 drain 同一个 `BattleResolutionPlan`；Pausable 入口由 Runner 在每个 Presentation / Commit 边界推进同一个 plan。

### Defeat Checkpoint

`BattleResolver.CommitDefeatCheckpoint` 是当前 formal Defeat 归属点。它读取 plan 的 `pendingDefeatImpact`，确认目标仍未 `IsDefeated()` 且 `currentHP <= 0`，然后调用 `CharacterData.MarkDefeated()`、把该 Impact 的 `didKill` 设为 `true`，并广播 `BattleTiming.AfterKill`。因此 HP 归零和 Defeat 不是同一时刻。

Pausable 路径在 `BattleExecutionRunner.FinishCurrentItem` 中先完成 Action Presentation、再调用 `CommitDefeatCheckpoint(CurrentResolutionPlan)`，然后才提交 `ActionFinished` 与生命周期完成。同步路径由 Resolver 在 Resolution plan 完成前后执行相同的 checkpoint 责任，不要把这个职责写成普通 `BattleExecutionPlan` 的直接事件。

## PRESENTATION FLOW

`BattleExecutionRunner` 的 Impact request 交给 `BattleSceneExecutionPresenter`。Presenter 根据 `BattlePresentationRoute` 找到真实角色和对应 Player；表现完成只提供进入 Combat Commit 的许可。

### Default Attack

默认近战 Attack-v-Attack / Unilateral 的第 0 段满足 `TryStartDefaultAttackImpact` 的 `ImpactIndex == 0` 条件时，才启动完整攻击表现：

```
Runner → Cue.Impact(index 0)
→ BattleSceneExecutionPresenter.TryStartDefaultAttackImpact
→ BattleAttackVsAttackPresentationPlayer.TryPlayResolvedWinnerAttack
→ winner.SetSlash / PlaySlashPresentation
→ ReachVisualImpact
→ loser.SetHit / Normal Hit presentation / camera impact
→ visual-impact callback
→ Presenter completion
→ Runner CommitImpact
→ HP / Damage Number observer
```

当前表现不是对 Animator clip 名称作假设，而是使用 `SetSlash`、`PlaySlashPresentation`、`SetHit`、Sprite/Presentation state 以及现有 coroutine 时序。`ReachVisualImpact` 是视觉接触点；它会通知 Camera / Hit presentation，但不会自己写 Combat HP。Runner 收到 Impact presentation completion 后，才允许 `BattleResolver.CommitImpact`。

第 0 段提交之后，如果 plan 还有下一段，Runner 等待该下一段的 `damageImpactDelaySeconds`。第 1 段及后续段不再重复启动完整 Default Attack animation；对应 Presenter 路由会完成该 Impact gate，Runner 直接提交下一段。每一段仍然独立产生 `resolvedDamage`、HP 写入、Damage Number 和可能的 Defeat Checkpoint。

`BattleAttackVsAttackPresentationPlayer.RunResolvedWinnerAttack` 在正常完成前会等待 loser 的 sustained Hit coroutine；这只等待当前 Hit timeline，不额外等待无关的 FX 生命周期。

### Double Slash

当前 `knife_double_slash_001 / 连斩` 使用 `Independent` 与 `[80, 80]`。它的正式时序是：

```text
Impact 0 → 完整 Default Attack presentation → CommitImpact → HP / Damage Number 0
→ 等待 Impact 1 的 damageImpactDelaySeconds（当前数据未配置，回退为 0）
→ Impact 1 不重复完整攻击动画 → 直接 CommitImpact → HP / Damage Number 1
```

两个 Impact 仍属于同一个 `BattleResolutionPlan`，各自拥有自己的 `resolvedDamage`、HP 写入和 Damage Number；这不是两个独立的卡牌行动。

## CONFIGURATION GUIDE

多段卡牌示例：

```json
{
  "damageDistributionMode": "Independent",
  "damageImpactPercents": [80, 150],
  "damageImpactDelaySeconds": [0, 0.15]
}
```

Independent 模式按每段独立百分比计算。以 Base Resolved Damage `10` 为例，两个 Impact 分别为 `8` 和 `15`，每段独立进入 `DamageModifier`、HP Apply 和 Damage Number。

累计模式示例：

```json
{
  "damageDistributionMode": "Cumulative",
  "damageImpactPercents": [100, 180, 230],
  "damageImpactDelaySeconds": [0, 0.1, 0.1]
}
```

`BattleDamageDistribution.GetCumulativeSegmentDamage` 先计算累计总量再做相邻差值：`C[i] = floor(BaseResolvedDamage * cumulativePercent[i])`。Base `7` 与 `[100, 180, 230]` 得到累计总量 `7、12、16`，因此真实段为 `7、5、4`，总计 `16`。这是通用 `BattleDamageDistribution`，不是 ALL IN 专属规则。

修改多段卡时，先确认 `damageDistributionMode`，再配套 `damageImpactPercents` 和 delay 数组。不要用 `hpDisplayStageCount` 代替 Gameplay 多段配置。

`damageImpactDelaySeconds` 的 owner 是 `CardTestData` 数据、`BattleImpact` 运行时字段和 `BattleExecutionRunner` 的 pending-impact gate。它会延迟下一段 HP Apply 与 Damage Number 一起发生；它不只是 Damage Number 的显示延迟，也不改变攻击动画速度、CD 或全局动画速度。

## DEFAULT ATTACK

普通单段攻击的实际责任边界是：

1. `BattleResolver` 根据已完成的 Clash/Free Action 结果建立 `BattleResolutionPlan` 和 Impact。
2. `BattleExecutionRunner` 先进入 `BattlePresentationCue.Impact`，不得在 Presentation 未完成时提交该 Impact。
3. `BattleSceneExecutionPresenter.TryStartDefaultAttackImpact` 只让 `ImpactIndex == 0` 走完整的 Attack-v-Attack 近战表现。
4. `BattleAttackVsAttackPresentationPlayer` 在 `PlaySlashPresentation` 的视觉接触回调处触发受击者 Hit、FX 与 Camera 入口；这些不是伤害提交本身。
5. Presenter completion callback 返回 Runner 后，Runner 才调用 `BattleResolver.CommitImpact`。
6. Resolver 执行 `DamageModifier`、写入 `resolvedDamage`、`TakeDamage`、写入 `actualDamage`，再由 `IBattleImpactCommitObserver` 通知 HP UI 和 Damage Number。
7. 当前 Action Presentation 完成后，Runner 才走 `FinishCurrentItem` 和 Defeat Checkpoint；生命周期随后判断正式 `IsDefeated`。

因此“视觉 Impact”与“Combat Commit”是相邻但不同的边界：视觉接触可以先发生，HP 和事件仍由 Resolver 的正式 Commit 唯一拥有。

## SPECIAL CARD EXTENSION CONTRACT

未来特殊卡可以把视觉节奏定义为独立的 Damage Marker：

```
Visual Step 0: 0.3s，无伤害
Visual Step 1: 0.4s，Marker 0.2s → Impact 0
Visual Step 2: 0.2s，Marker 0s → Impact 1
```

未来可以为每一步指定 Sprite、step duration、marker、camera 等表现参数；Marker 只负责授予对应 `BattleResolver.CommitImpact` 的时机，不创建第二套 Resolver。每个真实 Impact 仍必须拥有自己的 `resolvedDamage`、HP Apply、Damage Number 和 Defeat Checkpoint 语义。

`RESERVED / NOT IMPLEMENTED IN v0.1`：当前 Runtime 没有以该特殊 Marker timeline 替代默认攻击表现，不能把本节写成现有卡牌的正式入口。

## DEFENSE / DODGE INTERACTION

### Attack vs Defense

Attack-v-Defense 由 `BattleResolver.BuildDefenseResolutionPlan` 建立：

- `DefenseFullBlock`：建立不允许伤害的接触 Impact；不会产生 `DamageModifier`、`AfterDamage` 或 Damage Number 的实际伤害路径。
- `DefenseReducedDamage`：用 Session 的 `RemainingAttackPoint` 建立可伤害 Impact；该段经过正常 `DamageModifier`、`resolvedDamage`、HP Apply、`actualDamage` 和 Damage Number 路径。

防御本身的 Used / resource / Guard 消费由相应 Resolver / Executor 契约负责，不由 Damage Number 或 Presenter 决定。

### Attack vs Dodge

`BattleResolver.BuildDodgeResolutionPlan` 对 DodgeSuccess 保持 0-impact 语义：不伪造 `Impact`，因此没有 `DamageModifier`、`Hit`、`AfterDamage` 或 `AfterKill`。Runner 会用 0-impact plan 正常完成 Resolution。

DodgeFailed 才会由敌方攻击建立真实 Impact；该 Impact 的伤害、DamageModifier、HP Apply、Damage Number 和 Defeat Checkpoint 遵循普通提交路径。Dodge Success / Failed 的行为结果由 `resultType` 和 plan impacts 表达，不能用 UI 是否显示数字反推。

## HP / DEFEAT

`CharacterData.currentHP` 是当前可见血量；`CharacterData.IsDead()` 表示 `currentHP <= 0`。`CharacterData.IsDefeated()` 是正式终局状态，只有 `MarkDefeated()` 在 Defeat Checkpoint 执行后才变为 true。

多段致死例子：

```
目标 HP = 5
Impact 0: resolvedDamage = 20, actualDamage = 5, currentHP = 0, IsDefeated = false
Impact 1: resolvedDamage = 10, actualDamage = 0, currentHP = 0, Damage Number = 10
全部 Impact / Action Presentation 完成
→ CommitDefeatCheckpoint
→ MarkDefeated
→ AfterKill
→ Lifecycle Battle-End Evaluation
```

后续 Impact 是否继续存在由 plan 和 Impact state 决定；不能因为 `IsDead()` 就把正式多段 plan 当作已经完成。只有被标记 `IsDefeated` 后，后续真正的伤害提交才会按当前 Resolver 规则跳过。

HP UI 必须把 `HP Depleted`（currentHP 到 0）和 `Defeated`（checkpoint 后的正式状态）分开。Damage Number 显示 `resolvedDamage`，不把 Clamp 后的 `actualDamage` 当作卡牌伤害文本。

## ALL IN EXAMPLE

当前 `shoot_all_in_001` 使用 `BattleDamageDistributionMode.Cumulative`，正式累计表为：

```
1: 100%
2: 180%
3: 230%
4: 270%
5: 300%
6: 320%
```

`BattleResolver.AddDamageImpacts` 读取 `BattleClashResourceSnapshot.capturedStack`，取 captured Bullet 数量与上述数组长度的较小值，建立前 N 个真实 Impact。captured `3` 就是 `3` 个 Impact、`3` 次正式 HP Apply、`3` 个 Damage Number 机会；每段按累计差值计算，分别经过 Commit。

不要把旧的“单个 Impact + `hpDisplayStageCount`”描述当作当前 ALL IN 多段 Combat 实现。`hpDisplayStageCount` 如果仍存在，只是单个 Impact 的兼容性 HP Presentation 字段。

## DAMAGE NUMBER

owner 是 `BattleDamageNumberPresenter`（`Assets/Scripts/Presentation/BattleDamageNumberPresenter.cs`），它不参与战斗计算。`BattleSceneExecutionPresenter.OnImpactCommitted` 是当前 caller / observer seam。

触发条件是 Impact 已经 Commit、`impact.allowsDamage` 且 `impact.didHit`。Presenter 从 `BattleUnitViewSpawner.GetHandle(impact.target)` 取得目标的 `BattleUnitViewHandle`，优先使用 `CenterAnchor`，缺少时 fallback 到 `WorldRoot`；再从 `WorldFollower.ResolvedTargetCanvas` 取得 Canvas，并使用 `BattleUnitViewSpawner.WorldCamera` 投影。

坐标链为：

```
target
→ BattleUnitViewSpawner.GetHandle
→ BattleUnitViewHandle.CenterAnchor（fallback WorldRoot）
→ WorldFollower.ResolvedTargetCanvas
→ WorldCamera.WorldToScreenPoint
→ RectTransformUtility.ScreenPointToLocalPointInRectangle
→ Damage Number RectTransform.anchoredPosition
```

运行时创建 `BattleDamageNumber` GameObject、`RectTransform` 和 `TextMeshProUGUI`，文本是 `impact.resolvedDamage.ToString()`，`raycastTarget = false`。正式 `BattleScene` 的 `BattleSceneExecutionPresenter` GameObject 上绑定 `damageNumberPresenter` 到同对象上的 `BattleDamageNumberPresenter`（Scene `fileID: 833021975`）；当前序列化值是 `fontSize = 32`、`textColor = white`、`lifetime = 0.6`。组件字段为 `Font Size`、`Text Color`、`Lifetime`。

如果 `impact`、目标 anchor、Canvas、World Camera 或 Canvas RectTransform 缺失，presenter 输出一次 warning 并不写显示对象。`Lifetime <= 0` 时创建的数字立即 `Destroy`；大于 0 时用 Unity delayed Destroy。

当前尚未实现：Fade、Move/Float、多数字避让、Crit、Heal、Buff secondary number、Shield、VFX/SFX binding、pooling。

## DEBUG / FAILURE MODES

| 现象 | 当前检查顺序 |
|---|---|
| 有伤害但没有数字 | 检查 `BattleSceneExecutionPresenter.damageNumberPresenter`、`BattleUnitViewHandle`、`CenterAnchor` / `WorldRoot`、`ResolvedTargetCanvas`、`WorldCamera`，以及 `impact.didHit` / `impact.allowsDamage` |
| 数字不对 | 先看 `BattleImpact.resolvedDamage`；不要用 `actualDamage` 解释卡牌伤害数字 |
| 第一个 Impact 把 HP 打到 0，第二个没有继续 | 区分 `IsDead()` 与 formal `IsDefeated()`，再检查后续 Impact 的 `state` 与 plan checkpoint |
| 每段都重复完整攻击动画 | 检查 `BattleSceneExecutionPresenter.TryStartDefaultAttackImpact` 的 `ImpactIndex == 0` gate |
| ALL IN 总伤害不对 | 检查 captured Bullet 数、`Cumulative` 数组和 `BattleDamageDistribution`，不要回退到 `hpDisplayStageCount` |

## TESTING CONTRACT

当前实际存在、可用于 Damage 契约回归的入口包括：

- `Assets/Tests/Legacy/Core/BattleResolutionPlanTests.cs`：`VerifyCalculateDoesNotDamage`、`VerifyFirstCommitAppliesDamage`、`VerifyCommittedImpactIsIdempotent`、`VerifyFatalDamageEndsAfterCompletion`、`VerifyDeadTargetDoesNotRepeatKill`、`VerifyFreeAttackPlanDelaysDamage`、`VerifyDodgeSuccessCompletesWithoutImpact`。
- `Assets/Tests/Legacy/Core/BattlePresentationProtocolTests.cs`：`VerifyImpactBlocksAllCommitEffects`、`VerifyImpactCommitsOnce`、`VerifyTwoImpactsCommitOnePerCompletion`、`VerifyDodgeSuccessHasNoFakeImpact`。
- `Assets/Tests/Legacy/Core/BattleAllInBasicTests.cs`：`VerifyFormalResolution` 覆盖 captured Bullet 数与 ALL IN 真实 Impact 数。
- `Assets/Tests/Legacy/Runner/CardLoadTest.cs`：Mode125 `VerifyOverkillUsesActualDamage`、`VerifyLethalImpactOwnsAfterKill`、`VerifyLaterDeathIsNotAttributedToEarlierImpact`、`VerifyDamageEventsKeepExactImpact`、`VerifyFormalFreeAttackEventOrder`；Mode112 `BattleAllInBasicTests` caller 也保留 ALL IN 回归。
- `Assets/Tests/Legacy/Core/FullBattleIntegrationRegressionTests.cs`：`VerifyPointAsDamage250Percent` 与 Battle Scene 集成伤害入口。

这些是当前 Legacy / compatibility caller，不把它们误报为 Unity Test Runner 自动发现的 `[Test]`。新增或修改 Damage Runtime 前，按 [RegressionTestMap](../../Testing/RegressionTestMap.md) 检查既有覆盖和 caller。

## KNOWN LIMITS

- 当前 Damage Number 是每个已提交 Impact 即时创建的轻量 GameObject，没有 pooling、动画、避让或多种数字类型。
- `actualDamage` 受目标剩余 HP Clamp 影响；`resolvedDamage` 保留正式 DamageModifier 后的候选值。
- `AfterKill` 不在 HP 刚归零的瞬间触发，而由 `CommitDefeatCheckpoint` 在本段/Action 的正式 checkpoint 触发。
- Full Block 和 DodgeSuccess 没有可供 Damage Number 使用的真实伤害 Impact；不要为它们制造伪数字。
- 特殊卡 Damage Marker timeline 仍为 `RESERVED / NOT IMPLEMENTED IN v0.1`。

## VERIFICATION

`2026-09-14: USER UNITY MANUAL VERIFIED: 普通单段攻击, 双斩, ALL IN, Damage Number runtime display`

以上不表示用户已单独手动验收 overkill 或致死多段边界；这些边界由当前源码和测试契约覆盖，未记录为独立人工验收。
