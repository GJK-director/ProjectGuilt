# Test Architecture

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`b10297c9be5bc244e6ed08592f32f5ab63f992b4`)

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

本轮只记录设计，不创建这些 `.cs`。

## Test Principles

- 一个系统 = 一个 Suite。
- 一个条件 = 一个 Case。
- 新增 Case 默认优先进入 existing Suite。
- 禁止默认采用“一个条件 = 一个新 Mode / 新场景”。
- 只有存在新的系统级环境需求时，才需要新的 Harness。

## Current Boundary

当前测试仍由 `Assets/Tests/Legacy/Runner/CardLoadTest.cs`、Legacy 静态 Test 类、Presentation Sandbox 和正式 BattleScene Harness 共同承担；本批只完成 Legacy Core 测试物理隔离。
