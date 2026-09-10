# Test Writing Guide

Status: TRANSITIONAL
Last Verified: 2026-09-10
Repository Basis: `5db805ea452288e86502df0b3075becb7f8f4024`

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

## Formal Suite Cases

- 一个 Case 验证一个条件。
- 当前 Transitional Assembly-CSharp 阶段，Case 默认使用 `public static bool`。
- Case 不直接 `Debug.Log`；Runner / compatibility caller 负责聚合日志。
- Production Data Case 使用 `BattleScenarioBuilder` / `BattleTestContext`。
- Synthetic Case 使用 Shared Factories。
- 不因为一个新 Case 创建新 Mode；Legacy Mode 可以临时调用 Formal Case。
- 不复制 Formal Case 回 Legacy。

这是 Transitional contract。未来如果统一 Test Runner / Assertion API 建立，再集中迁移。

## Just-In-Time Migration

当前 Legacy test 治理采用 `JUST_IN_TIME_TEST_MIGRATION`。Formal Suite 是首选 regression source；Legacy Mode 可以暂时保留。修改 Production system 前先查询 `RegressionTestMap.md` 与 `LegacyContractTriage.md`，只迁移仍对当前修改有价值的 Contract，不为历史 Mode 一对一创建 Suite。UI / Camera / Animation / Presentation 进入实际修改阶段时，再按需建立 shared harness。

## Frozen Result Logging

任何新增或实际改写、且会输出人工验收结果的 test runner、compatibility runner 或 manual harness，结果区开始前必须打印：

```csharp
Debug.Log("========== 以下是测试结果 ==========");
```

全绿时，结果区只需保留有效的 PASS / aggregate 结果；不要求复制初始化、loader 或成功调用栈日志。失败时保留结果区、False 项、Exception/Error 与必要 stack trace。本规则只约束未来新增或实际改写的 runner，本批不修改现有 runner。
