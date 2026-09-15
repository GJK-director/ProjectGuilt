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
