# Tests

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## CURRENT

现有测试尚未迁移。Legacy 测试仍在 `Assets/Scripts/Core`、`Assets/Scripts/Presentation`、`Assets/Scripts/UI` 等旧位置，主要入口为 `SampleScene` → `CardLoadTest` → `BattleTestMode`。

## TARGET

- `Legacy`：尚未迁移的旧测试。
- `Shared`：可复用 Fixtures、Builders、Assertions。
- `Suites`：按系统组织的自动 Regression。
- `Harness`：需要真实 Unity 场景、UI、Camera 的测试环境。

禁止把生产 Runtime 代码放入 `Assets/Tests`。

本轮只建立目录和 README，不创建测试 `.cs`。
