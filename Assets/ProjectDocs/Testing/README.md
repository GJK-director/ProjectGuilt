# Testing

Status: CURRENT
Role: TESTING ROOT NAVIGATION
Last Verified: 2026-09-11

## Current Facts

- Formal Suites: 4 C# owners。
- Formal Cases: 35 public static bool Cases。
- Shared C#: 6。
- Legacy/Core C#: 34；Legacy Runner 还包含内嵌测试。
- Active BattleTestMode: 110；当前有对应的 110 个 dispatch 分支。
- Historical standalone IDs: 89、109、114；retained wrappers 仍有 consumer。

Formal Suite != Unity Test Runner [Test]。目前是 public static bool Case ownership + compatibility caller / runner；现有测试仍在默认 Assembly-CSharp，无 Test asmdef。安装 Unity Test Framework 不等于已接入其自动发现机制。

## Where to Start

[RegressionTestMap](RegressionTestMap.md) → [LegacyModeMigration](LegacyModeMigration.md) → 必要时 [LegacyContractTriage](LegacyContractTriage.md)（HISTORICAL provenance）。

[TestArchitecture](TestArchitecture.md) 解释层次，[TestWritingGuide](TestWritingGuide.md) 解释新增/迁移规则，[ManualHarnesses](ManualHarnesses.md) 解释实际运行与人工验收。[CodeMap](../Developer/CodeMap.md#test-and-support-locations) 拥有路径事实。

## Actual Execution

Mode103 → FullBattleIntegrationRegressionTests → EnemyIntent Formal Cases（其余集成检查仍在 Legacy）。
Mode115 → BattleDeckHandGroupingTests → retained Mode114 wrapper → Cards/Bootstrap Cases → retained Mode109 wrapper → retained Mode89 wrapper → Execution Cases。

SampleScene 是 Legacy runner host；所选 mode 可能随 local working tree 不同，不把本地 Inspector 选择写成正式基线。
Suites 的 Buffs/Lifecycle/Presentation/Resolution/Settings/UI、Shared/Assertions 及部分 Harness 目录是 placeholder，不表示已有 C# 实现。

## Verification Boundary

Formal ownership 不等于 standalone retirement，也不等于 compatibility wrapper deletion。先查 consumer 再判断可删性。
JIT 只迁移相关且有价值的契约；不为清零 Legacy 扩张测试。视觉相关修改结合现有 harness 与用户 Unity 验收。
分别报告静态检查、实际运行结果、编译和人工验收；未运行不得标 PASS。禁止 Unity batchmode。
