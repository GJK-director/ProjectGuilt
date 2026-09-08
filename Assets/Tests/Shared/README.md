# Shared Test Infrastructure

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`c35bd41a13587b11b43fd062f40a32d541f35d17`)

目标是承载可复用的测试 Fixtures、Builders 和 Assertions。

Batch 4A 当前已有：

- `Fixtures/BattleTestContext.cs`
- `Builders/BattleScenarioBuilder.cs`
- `Builders/TestCharacterFactory.cs`
- `Builders/TestCardFactory.cs`
- `Builders/TestIntentFactory.cs`

这些代码属于 `TEST_ONLY`、`TRANSITIONAL` 基础设施，目前编译进默认 `Assembly-CSharp`；本批没有创建 Assertions、Test asmdef 或迁移 Legacy Mode。
