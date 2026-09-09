# Test Architecture

Status: TRANSITIONAL
Last Verified: 2026-09-09
Repository Basis: 当前本地 HEAD (`4abc9db9fb782b96255288f7504b5f1d848f0852`)

## Frozen Future Structure

```text
Assets/Tests/
├─ Legacy
├─ Shared
├─ Suites
└─ Harness
```

## Test Layers

- A. Pure Logic
- B. Runtime Integration
- C. Presentation / UI
- D. Camera / Visual Harness

## Migration Progress

Batch 1:
Docs/Test skeleton created.

Batch 2A:
Legacy Runner and 22 standalone Core Test files physically isolated.

`PHYSICAL ISOLATION` != `SUITE MIGRATION`.

Batch 3A:
Embedded Legacy Test classes extracted from `BattleDeckManifest.cs` and `GameSettingsState.cs` into `Assets/Tests/Legacy/Core/`.

本批仍是 `EXTRACTION`，不是 Test Refactor；没有改变 Mode、测试断言、测试数据或默认程序集边界。

Batch 4A:
Shared test construction infrastructure introduced:

- `BattleTestContext`
- `BattleScenarioBuilder`
- `TestCharacterFactory`
- `TestCardFactory`
- `TestIntentFactory`

这些类型仍编译在 `Assembly-CSharp`，没有新增 Test asmdef；本批没有迁移 Legacy Mode，也没有改变 Runtime/GamePlay 行为。

这些文件暂时仍属于 `Legacy`，而不是 `Suites`；本批没有去重、删除 Mode、建立 shared fixtures 或创建 Test asmdef。

## Future Shared Facilities

```text
BattleTestContext
BattleScenarioBuilder
TestCharacterFactory
TestCardFactory
TestIntentFactory
TestAssertion
```

Batch 4A 已创建前五个 Shared 类型；`TestAssertion` 仍仅作为未来规划，尚未创建。

Batch 4B:
Mode103 的 production fixture 已迁移为消费 `BattleTestContext` 与
`BattleScenarioBuilder.CreateProductionEncounter`。`FullBattleIntegrationRegressionTests`
仍属于 Legacy，仍是一个聚合 Mode；原有 28 项检查、测试逻辑和 Mode 入口均保留，
没有删除测试或 Mode。

Batch 4C:
Mode103 的 synthetic construction 开始消费 Shared Factories：默认角色构造使用
`TestCharacterFactory`，标准卡牌状态构造通过保留 Mode103 ID compatibility wrapper
委托 `TestCardFactory`，synthetic enemy intent 使用 `TestIntentFactory`。
`CreateCardData` 仍保留为 Legacy compatibility helper，因为当前 Shared FixedData
contract 无法完全保持旧 fixture 的 cardName / traits 语义；2.5x CardData 与 custom-speed
CharacterData 仍保留为 case-specific 数据。本批仍未迁移 Mode 为正式 Suite，28 项检查保持不变。

Stage5A / Batch5A:
建立第一套 Formal Suite：`EnemyIntentTests`。Formal Suite 按系统拥有 Cases，Legacy Mode
可以在迁移期调用 Formal Case 作为 compatibility runner；本批没有删除 Mode 或退役 Legacy
入口。Mode103 Test4 部分委托 EnemyIntent Suite，TurnCycle / provider integration 仍留在
Legacy；Test5 完全委托该 Suite。

`PHYSICAL EXTRACTION` != `SHARED INFRASTRUCTURE` != `FORMAL SUITE MIGRATION` !=
`LEGACY MODE RETIREMENT`。本批状态为 `FORMAL SUITE MIGRATION STARTED`。

## Test Principles

- 一个系统 = 一个 Suite。
- 一个条件 = 一个 Case。
- 新增 Case 默认优先进入 existing Suite。
- 禁止默认采用“一个条件 = 一个新 Mode / 新场景”。
- 只有存在新的系统级环境需求时，才需要新的 Harness。

## Current Boundary

当前测试仍由 `Assets/Tests/Legacy/Runner/CardLoadTest.cs`、Legacy 静态 Test 类、Shared construction infrastructure、Formal Suites、Presentation Sandbox 和正式 BattleScene Harness 共同承担；Batch 5A 仅开始 EnemyIntent Formal Migration，未退役 Mode103。
