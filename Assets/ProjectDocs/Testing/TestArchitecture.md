# Test Architecture

Status: TRANSITIONAL
Last Verified: 2026-09-10
Repository Basis: `5db805ea452288e86502df0b3075becb7f8f4024`

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

Batch 5B：
建立第二套 Formal Suite：`Assets/Tests/Suites/Cards/CardDeckManifestTests.cs`，覆盖 Cards 域的四个独立 Case。Mode109 wrapper 继续聚合并消费该 Suite；Mode89 的 Execution FirstStrike policy 仍由 Legacy wrapper 保留。TestRunner、TestResult 和 TestAssertion 仍待后续统一基础设施建立。

Batch 5C：
建立第三套 Formal Suite：`Assets/Tests/Suites/Execution/FirstStrikeExecutionTests.cs`，覆盖 13 个 FirstStrike Execution Priority Case。当前已有 EnemyIntent、Cards、Execution 三个系统样板；Mode89 作为 compatibility aggregation wrapper，Mode109 通过 Mode89 消费 Execution Formal coverage。Formal Suite Migration != Legacy Mode Retirement；暂不创建统一 Runner / Result API。

Batch 5D：
建立第四套 Formal Suite：`Assets/Tests/Suites/Bootstrap/DeckPresetBootstrapTests.cs`，覆盖 11 个 Bootstrap Case。Mode114 的 Manifest 职责回到现有 Cards Suite 并新增两个 Case，Bootstrap 职责进入 Bootstrap Suite。Formal Suite 不是“一批迁移一个新类”：已有 Formal Suite 应增加 Case，不创建重复 Suite；Formal Suite Migration != Legacy Mode Retirement。

Batch 6A：
完成第一个 standalone Legacy Mode 的 retirement：Mode89 的 `BattleTestMode` enum member 与 `CardLoadTest.Start()` dispatch 已移除，保留 1 条历史 inventory record；13 个实际 FirstStrike Case 继续由 `FirstStrikeExecutionTests` 提供，`BattleExecutionPlanFirstStrikePolicyTests` 仅作为 Mode109 的 compatibility wrapper。当前 active enum 为 112 个。

`FORMAL SUITE MIGRATION` != `STANDALONE MODE RETIREMENT` != `COMPATIBILITY WRAPPER DELETION`。本批只完成 standalone Mode retirement；wrapper deletion 等待 Mode109 不再依赖该兼容入口。

Batch 6B：
完成第二个 standalone Legacy Mode 的 retirement：Mode109 的 `BattleTestMode` enum member 与 `CardLoadTest.Start()` dispatch 已移除，保留 Mode109 历史 inventory record；Cards 实际所有权为 `CardDeckManifestTests`，`BattleDeckManifestTests` 仅作为 Mode114 的 compatibility wrapper，Execution 继续经 retained Mode89 wrapper → `FirstStrikeExecutionTests`。当前 active enum 为 111，historical-only 为 Mode89 与 Mode109。

当前生命周期明确区分：Formal Test Ownership != Standalone Mode Entry != Compatibility Wrapper。Mode109 现为 formal ownership complete、standalone entry retired、wrapper retained（因 Mode114 仍消费）。

Batch 6C：
完成第三个 standalone Legacy Mode 的 retirement：Mode114 的 `BattleTestMode` enum member 与 `CardLoadTest.Start()` dispatch 已移除，保留 Mode114 历史 inventory record；Cards 所有权为 `CardDeckManifestTests`，Bootstrap 所有权为 `DeckPresetBootstrapTests`。`BattleDeckBootstrapPresetTests` 仅作为 Mode115 的 compatibility wrapper，因 Mode115 仍消费它而不能删除。当前 active enum 为 110，historical-only 为 Mode89、Mode109 与 Mode114。

当前链为：Mode115 ACTIVE → retained Mode114 wrapper → retained Mode109 wrapper → retained Mode89 wrapper → Formal Suites。Standalone Retirement 不自动意味着 wrapper dead；必须先检查 consumer graph。

Batch 6D / 6E：
完成 110 个 active Legacy Mode 的 value triage，并将其概念性归并为 30 个 Contract Cluster（24 个 automated-oriented、6 个 manual/design-oriented）。Mode86 的 FirstStrike execution priority/order/pairing 已有 `FirstStrikeExecutionTests` 覆盖，但 JSON traits missing/null/empty compatibility 与 LongRangeShoot non-implication 仍是 unique Legacy coverage，因此保持 active，不进行 standalone retirement。

后续采用 `JUST_IN_TIME_TEST_MIGRATION`：Formal Suite 是首选 regression source；修改 Production system 前先查询 `RegressionTestMap.md` 与 `LegacyContractTriage.md`，只迁移相关且仍有价值的 Contract；不为历史 Mode 一对一创建 Suite，不以 active Legacy Mode = 0 作为当前 Demo 的阻塞条件。UI / Camera / Animation / Presentation 仅在相关系统实际修改时按需建立 shared harness。

**Phase6 is CLOSED FOR CURRENT DEMO GOVERNANCE.** 这表示测试架构、inventory、triage 与已确认重复 standalone Mode 的治理边界已收口；不表示所有 Legacy Mode 已删除、所有 Legacy test 已 Formal 化或所有 Manual Harness 已建立。
