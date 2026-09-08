# Testing

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## CURRENT

```text
SampleScene
→ CardLoadTest
→ BattleTestMode
→ sequential if dispatch
```

当前：

- 113 个 Mode。
- ID 范围 2–133，非连续。
- 无重复 enum value。
- 很多测试编译进默认 `Assembly-CSharp`。
- Unity Test Framework 已安装。
- 尚无正式 `Assets/Tests` 测试程序集。
- 现有测试尚未迁移。

## TARGET

- Shared test infrastructure
- Test Suites
- 少量 Harness
- Legacy Mode 逐步迁移

本轮尚未迁移任何 Mode，也不创建测试 `.cs`。

## Navigation

见 `TestArchitecture.md`、`RegressionTestMap.md`、`TestWritingGuide.md`、`ManualHarnesses.md`、`LegacyModeMigration.md`。
