# Tests

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`3e16a1d9c9eefcdac9c357a3d5cba12095767bec`)

## CURRENT

Legacy Runner 已位于 `Assets/Tests/Legacy/Runner/CardLoadTest.cs`，22 个 standalone Core Tests 已位于 `Assets/Tests/Legacy/Core/`。主要入口仍为 `SampleScene` → `CardLoadTest` → `BattleTestMode`。

## TARGET

- `Legacy`：已物理隔离但尚未转换为正式 Suite 的旧测试。
- `Shared`：可复用 Fixtures、Builders、Assertions。
- `Suites`：按系统组织的自动 Regression。
- `Harness`：需要真实 Unity 场景、UI、Camera 的测试环境。

禁止把生产 Runtime 代码放入 `Assets/Tests`。

本批只做物理隔离，不创建新的测试 `.cs`、Suite、Shared fixture 或 Test asmdef。
