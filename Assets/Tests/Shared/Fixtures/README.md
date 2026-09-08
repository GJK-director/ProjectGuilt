# Shared Fixtures

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`c35bd41a13587b11b43fd062f40a32d541f35d17`)

Fixtures/ 只承载可复用的测试状态容器。

当前：

- `BattleTestContext.cs`：承载 Loader/Bootstrap 产生的 Cards、Definitions、Runtime 和 Bootstrap 结果。

该类型不负责 Load、Create、Execute、Resolve、Assert 或推进战斗。

Batch 4A 状态：`TEST_ONLY`、`TRANSITIONAL`、默认 `Assembly-CSharp`；没有迁移 Legacy Mode。
