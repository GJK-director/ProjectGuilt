# Test Writing Guide

Status: CURRENT
Role: TEST AUTHORING AND JIT POLICY
Last Verified: 2026-09-11

## Default Choice

existing Suite → existing Fixture → new Case。
一个系统拥有一套 Suite，一个条件对应一个 Case；不因一个条件创建新 Mode。只有新的系统级环境需求才建立 Harness。

Production Data 使用 BattleScenarioBuilder/BattleTestContext；synthetic scenario 优先使用 TestCharacterFactory、TestCardFactory、TestIntentFactory。若构造 API 本身就是被测对象，可保留专用构造。
现行 Case 为 public static bool；Case 不直接 Debug.Log，caller 聚合结果。不复制 Formal Case 回 Legacy，不假设 Unity Test Runner 会自动发现。

## JUST_IN_TIME_TEST_MIGRATION

identify Domain → check existing Formal Coverage → check [RegressionTestMap](RegressionTestMap.md) → check [LegacyModeMigration](LegacyModeMigration.md) if needed → migrate only valuable related contract → modify Production。
必要时才读 [LegacyContractTriage](LegacyContractTriage.md) 的 Historical provenance；其中 Recommended Carrier 不证明当前 Suite 存在。

不得为了清零 Legacy 迁测试、为一个 Case 创建新 Mode、无关扩张 Suite。先证明行为仍 Current、有回归价值且未被现有 Case 充分保护。retired standalone wrapper 需检查实际 consumer，不能按标签删除。
UI / Camera / Animation / Presentation 默认 manual/shared harness + human Unity acceptance。

## Result Logging

新增或实际改写、会输出人工验收结果的 runner、compatibility runner、manual harness，在结果区开始前输出：

```csharp
Debug.Log("========== 以下是测试结果 ==========");
```

全绿保留有效 PASS/aggregate；失败保留 False 项、Exception/Error 与必要 stack trace。不要把初始化/loader 日志当验收结果。
未执行只报告 NOT RUN；静态检查不代替编译或视觉验收。DO NOT USE UNITY BATCHMODE。

## Documentation Impact

修改 Case owner、caller、unique coverage、运行/人工验收方式时更新 Testing owner；Production 修改后执行 AGENTS 的 DOC IMPACT GATE。只更新受影响事实，不制造无意义 Markdown 改动。
