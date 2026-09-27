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

## Damage Impact Presentation

默认近战 Attack-v-Attack 的 `ImpactIndex == 0` 由 `BattleSceneExecutionPresenter.TryStartDefaultAttackImpact` 启动完整 `BattleAttackVsAttackPresentationPlayer` Slash / Hit 表现；视觉接触由 `ReachVisualImpact` 标记，不能直接写 HP。Presenter completion 返回 Runner 后才允许 `BattleResolver.CommitImpact`。后续 Impact 由 Runner 按 delay gate 逐段提交，不重复完整默认攻击动画。提交后的 `BattleImpact` 由 `OnImpactCommitted` 通知 Damage Number observer。未来特殊卡 Damage Marker 仍为 `RESERVED / NOT IMPLEMENTED IN v0.1`。详细责任边界见 [Damage](../Battle/Damage.md)。

## Combat Dice Split Prototype v0.1

仅 `ResolutionPlan.requiresClashWinPresentation` 的测试卡胜利分支进入动态 `BattleCombatDicePrototypePresenter`。`ClashWin` 独立 cue 复用现有基础攻击姿势，调用 `PlaySlashPresentation(..., playAttackEffect: false)` 关闭刀光；其接触节点只触发表现。复用现有 PerfectGuard 黄色火花，在双方之间偏敌人侧的位置捕获一次世界坐标，淡出期间不跟随敌人。双方短 Hit Stop（默认 0.05s）和轻震屏后，敌人以已有受力姿势弹开 0.2 世界单位，不染红、不播放血 / 正常命中特效。尾段结束再停顿 0.15s，才放行第一颗伤害骰；不产生 HP、Hit、DamageModifier 或伤害数字。

Scene Presenter 从已有 Guard Player 的 `PresentationProfile` 获取素材 / 受力 Profile，不修改共享资产。`BattlePerfectGuardFxPlayer.TrySpawnAtWorldPosition` 增加定点播放与显式取消；原 `TrySpawn` 保留随防御者定位。`BattleCameraDirector.TryPlayImpactShake(profile, amplitudeScale)` 提供单次轻震屏，旧单参数调用保持倍率 1。碰撞参数位于运行时原型组件；第一刀仍原地挥击，碰撞弹开与之后两次伤害击退分别存在，但只有后两次为命中。

每颗骰子先显示规则层保存的 Roll，再调用既有 `BattleAttackVsAttackPresentationPlayer.TryPlayDamageDie`。第 0 颗使用近战，第 1 颗原地使用抵近射击、枪口火焰及现有 ShootHitFx；视觉命中 callback 只放行对应 Impact。段间 `DamageDieComplete` 等待 Player 与 Camera 反馈全部结束，保留角色击退位置及首击镜头终点。第二击仅缩短击退距离到 20%，沿首击镜头终点运行既有抵近射击 GenericHit 推进 / 冲击及震屏；最后才释放整次行动的 Camera ownership。

`BattleCombatDiceHUD` 在固定屏幕 Canvas 显示 Damage 序号、原始 Roll、Hit 次数；只读数据。关闭 / 取消会隐藏 HUD、清理播放器并经 Presenter 原路径释放 Camera；取消 token 不放行后续伤害。旧卡缺省分支维持前述默认行为。详见 [原型手工验收](../Battle/CombatDicePrototype.md)。

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
