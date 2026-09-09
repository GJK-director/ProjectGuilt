# Legacy Runner

Status: TRANSITIONAL
Last Verified: 2026-09-09
Repository Basis: 当前本地 HEAD (`610fba0ca460945658a3fa17cd1472d2f5fceb75`)

当前包含 `CardLoadTest.cs`，对应 `SampleScene` 的 Legacy `BattleTestMode` runner。当前有 110 个 active enum member 和 110 个一对一 sequential `if` dispatch branch，ID 仍覆盖 2–133 的非连续范围，其中 89、109 与 114 是已退休 standalone Mode 的保留空洞。Mode89、Mode109 与 Mode114 的 enum member 和 standalone dispatch 已分别在 Phase6A/Phase6B/Phase6C 移除；对应 compatibility wrapper 仍被 Mode109/Mode114/Mode115 链路使用。Runner 本身仍是 Legacy，不是未来最终测试架构。
