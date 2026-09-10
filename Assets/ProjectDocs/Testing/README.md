# Testing

Status: TRANSITIONAL
Last Verified: 2026-09-10
Repository Basis: `5db805ea452288e86502df0b3075becb7f8f4024`

## CURRENT

```text
SampleScene
→ Assets/Tests/Legacy/Runner/CardLoadTest.cs
→ BattleTestMode
→ sequential if dispatch
```

当前：

- 110 个 active `BattleTestMode` enum members。
- `LegacyModeMigration.md` 记录 113 条 Migration Inventory Records：110 条 `ACTIVE_ENUM` 加 3 条 `HISTORICAL_ONLY` 记录（Mode89、Mode109、Mode114）。
- ID 范围 2–133，89、109 与 114 为已退休 standalone Mode 的保留空洞，其他 ID 非连续。
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

Batch 6A 已开始 Legacy Mode retirement：Mode89 standalone enum 与 CardLoadTest dispatch 已移除，13 个 FirstStrike Case 及其兼容 wrapper 仍保留；整体 Legacy Mode 迁移仍处于进行中。

Batch 6B：Mode109 standalone enum 与 CardLoadTest dispatch 已移除；`BattleDeckManifestTests` wrapper 为 Mode114 保留。Mode89/Mode109 均为 `HISTORICAL_ONLY`，整体 Legacy Mode 迁移仍未完成。

Batch 6C：Mode114 standalone enum 与 CardLoadTest dispatch 已移除；`BattleDeckBootstrapPresetTests` wrapper 为 Mode115 保留。Mode89、Mode109、Mode114 均为 `HISTORICAL_ONLY`，整体 Legacy Mode 迁移仍未完成。

Phase6D / 6E Revised：110 个 active Mode 已完成 value triage，并概念性归并为 30 个 Contract Cluster（24 个 automated-oriented、6 个 manual/design-oriented）。Mode86 保持 active，因为它仍有 JSON trait compatibility 与 LongRangeShoot non-implication 两项 unique coverage；不为了减少 Mode 数量主动补建 Suite。

当前治理策略为 `JUST_IN_TIME_TEST_MIGRATION`：未来修改 Production system 前先查 Regression Map / Contract Triage，只迁移相关 Contract；Formal Suites 是首选 regression source。UI / Camera / Animation / Presentation 使用按需 shared harness，不在当前批次创建。

**Phase6 is CLOSED FOR CURRENT DEMO GOVERNANCE.** 这不表示所有 Legacy Mode 已退休、所有 Legacy test 已 Formal 化、所有 Manual Harness 已建立，或 active Legacy Mode 必须为 0。

## Navigation

见 `TestArchitecture.md`、`RegressionTestMap.md`、`TestWritingGuide.md`、`ManualHarnesses.md`、`LegacyModeMigration.md`。
