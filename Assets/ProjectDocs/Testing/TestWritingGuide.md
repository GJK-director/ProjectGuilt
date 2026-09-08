# Test Writing Guide

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

新增测试前先确认：

1. 是否已有对应 Suite？
2. 是否可以复用现有 Fixture？
3. 是否真的需要 Scene？
4. 是否只是一个新 Case？
5. 是否会重复创建 Character、Slot、Intent？

默认优先级：

```text
existing Suite
→ existing Fixture
→ new Case
```

只有存在新的系统级环境需求时才允许创建新 Harness。

后续不应无理由继续增长 `Mode134`、`Mode135`、`Mode136` 等独立入口。

本轮不迁移旧测试，不创建新测试代码。
