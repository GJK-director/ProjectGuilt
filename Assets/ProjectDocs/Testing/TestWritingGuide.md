# Test Writing Guide

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`c35bd41a13587b11b43fd062f40a32d541f35d17`)

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

## Shared Construction

新增非 Presentation 测试时，优先复用：

- `BattleScenarioBuilder`
- `TestCharacterFactory`
- `TestCardFactory`
- `TestIntentFactory`

不要重复手写 `new CharacterData(...)`、`new CardTestData(...)`、`BattleCardManager.CreateBattleCard(...)` 或 `new BattleEnemyIntent(...)`，除非测试目的本身就是验证这些构造 API。

Production Data test → `BattleScenarioBuilder`。

Synthetic unit scenario → `TestCharacterFactory` + `TestCardFactory` + `TestIntentFactory`。

Batch 4A 只建立 Shared construction infrastructure；没有迁移 Legacy Mode、创建 Suite 或改变 Runtime 行为。
