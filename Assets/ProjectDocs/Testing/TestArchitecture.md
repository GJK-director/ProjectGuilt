# Test Architecture

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

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

当前测试仍由 `CardLoadTest`、静态 Test 类、Presentation Sandbox 和正式 BattleScene Harness 共同承担；物理迁移尚未执行。
