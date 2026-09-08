# Manual Harnesses

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## SampleScene

`SampleScene` 是 Legacy Mode Runner，挂载 `CardLoadTest`，通过 Inspector 选择 `BattleTestMode`。

## BattlePresentationSandbox

`BattlePresentationSandbox` 是 Presentation / Camera Sandbox，使用 `BattlePresentationSandboxController` 和 Editor Inspector。

## BattleScene Formal Presentation Harness

`BattleFormalPresentationTestHarness` 当前仍被正式 `BattleScene` 引用，负责准备部分正式场景中的 Presentation 测试数据；当前代码注释表明它不替代正式 Runner/Presenter。

它暂不能仅凭名称判定为不可达。

## Boundary

本轮不修改任何 Scene，不调整 Harness 入口，不迁移 Mode。
