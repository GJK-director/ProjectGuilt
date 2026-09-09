# Legacy Runner

Status: TRANSITIONAL
Last Verified: 2026-09-09
Repository Basis: 当前本地 HEAD (`fea7fabf2bdf75f6c6eceb8ababf88c6565a3eb0`)

当前包含 `CardLoadTest.cs`，对应 `SampleScene` 的 Legacy `BattleTestMode` runner。当前有 112 个 active enum member 和 112 个一对一 sequential `if` dispatch branch，ID 仍覆盖 2–133 的非连续范围，其中 89 是已退休 standalone Mode 的保留空洞。Mode89 的 enum member 与 standalone dispatch 已在 Phase6A 移除；其 compatibility wrapper 仍由 Mode109 使用。Runner 本身仍是 Legacy，不是未来最终测试架构。
