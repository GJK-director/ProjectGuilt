# Shared Builders

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`c35bd41a13587b11b43fd062f40a32d541f35d17`)

Builders/ 负责标准测试对象和场景构造。

当前：

- `BattleScenarioBuilder.cs`：严格复用正式 Loader 与 `BattleDefinitionBootstrap.CreateRuntimeStateFromDefinitions(...)`。
- `TestCharacterFactory.cs`：创建单个 `CharacterData`。
- `TestCardFactory.cs`：创建固定点数 `CardTestData` 或通过 `BattleCardManager` 创建 `BattleCardState`。
- `TestIntentFactory.cs`：通过 Production 七参数构造创建 `BattleEnemyIntent`，不自动响应、消费或改写目标。

这些 Builder 属于 `TEST_ONLY`、`TRANSITIONAL` 基础设施，目前编译进默认 `Assembly-CSharp`；本批不创建 Assertion、Test asmdef 或新的 Test Mode。

Batch 4C：

Mode103 已开始消费这些 Shared Factories：默认 synthetic character 使用
`TestCharacterFactory`，标准 card state 通过 Mode103 ID compatibility wrapper 使用
`TestCardFactory`，synthetic enemy intent 使用 `TestIntentFactory`。Legacy 的
`CreateCardData`、2.5x CardData 与 custom-speed CharacterData 仍由 Mode103 自己保留。
