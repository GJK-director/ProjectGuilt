# Manual Harnesses

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`3e16a1d9c9eefcdac9c357a3d5cba12095767bec`)

## SampleScene

`SampleScene` 是 Legacy Mode Runner，挂载 `Assets/Tests/Legacy/Runner/CardLoadTest.cs`，通过 Inspector 选择 `BattleTestMode`。

## BattlePresentationSandbox

`BattlePresentationSandbox` 是 Presentation / Camera Sandbox，使用 `BattlePresentationSandboxController` 和 Editor Inspector。

## BattleScene Formal Presentation Harness

`BattleFormalPresentationTestHarness` 当前仍被正式 `BattleScene` 引用，负责准备部分正式场景中的 Presentation 测试数据；当前代码注释表明它不替代正式 Runner/Presenter。

它暂不能仅凭名称判定为不可达。

## Boundary

本轮不修改任何 Scene，不调整 Harness 入口，不迁移 Mode；只移动 Legacy Runner 与 Core Test 文件。
