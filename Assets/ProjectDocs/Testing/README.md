# Testing

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`b10297c9be5bc244e6ed08592f32f5ab63f992b4`)

## CURRENT

```text
SampleScene
→ Assets/Tests/Legacy/Runner/CardLoadTest.cs
→ BattleTestMode
→ sequential if dispatch
```

当前：

- 113 个 active `BattleTestMode` enum members。
- `LegacyModeMigration.md` 同步记录 113 条 Migration Inventory Records；两者不是两个不同的 Mode 总数。
- ID 范围 2–133，非连续。
- 无重复 enum value。
- 很多测试编译进默认 `Assembly-CSharp`。
- Unity Test Framework 已安装。
- 尚无正式 `Assets/Tests` 测试程序集。
- 22 个 standalone Core Tests 已从 Production Core 目录物理隔离到 `Assets/Tests/Legacy/Core/`；Batch 3A 另提取 6 个嵌入式 Legacy Test classes。
- 当前 Legacy Core 中共有 28 个已物理隔离的测试文件；它们仍未迁移成正式 Suite。
- 这些文件尚未迁移成正式 Suite，尚未去重、删除 Mode 或引入 shared fixtures。

## TARGET

- Shared test infrastructure
- Test Suites
- 少量 Harness
- Legacy Mode 逐步迁移

Batch 3A 没有改变任何 Mode；Legacy Runner 和 standalone Core Tests 仍保持原有代码与入口。

## Navigation

见 `TestArchitecture.md`、`RegressionTestMap.md`、`TestWritingGuide.md`、`ManualHarnesses.md`、`LegacyModeMigration.md`。
