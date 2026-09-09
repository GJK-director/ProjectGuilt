# Legacy Runner

Status: TRANSITIONAL
Last Verified: 2026-09-09
Repository Basis: 当前本地 HEAD (`3d75eef67b8dc9d1c90ac09d613e634d9d240441`)

当前包含 `CardLoadTest.cs`，对应 `SampleScene` 的 Legacy `BattleTestMode` runner。当前有 111 个 active enum member 和 111 个一对一 sequential `if` dispatch branch，ID 仍覆盖 2–133 的非连续范围，其中 89 与 109 是已退休 standalone Mode 的保留空洞。Mode89 与 Mode109 的 enum member 和 standalone dispatch 已分别在 Phase6A/Phase6B 移除；两者的 compatibility wrapper 仍被 Mode109/Mode114 链路使用。Runner 本身仍是 Legacy，不是未来最终测试架构。
