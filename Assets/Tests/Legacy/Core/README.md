# Legacy Core Tests

Status: TRANSITIONAL
Last Verified: 2026-09-09
Repository Basis: 当前本地 HEAD (`610fba0ca460945658a3fa17cd1472d2f5fceb75`)

当前从 `Assets/Scripts/Core` 物理隔离到此目录的 28 个 Legacy Core Regression 文件如下：

- `BattleClashSessionTests.cs`
- `BattleExecutionEffectiveInteractionTests.cs`
- `BattleExecutionInteractionContextTests.cs`
- `BattleExecutionPlanFirstStrikePolicyTests.cs`
- `BattleExecutionPlanInteractionTests.cs`
- `BattleExecutionPlanSingleItemAdvanceTests.cs`
- `BattleGenericAttackVsDefenseTests.cs`
- `BattleGenericAttackVsDodgeTests.cs`
- `BattleGenericPausableRoutingTests.cs`
- `BattleGenericUnilateralAttackTests.cs`
- `BattleInteractionClassifierTests.cs`
- `BattleInteractionStateAndEndLockTests.cs`
- `BattleLifecycleControllerTests.cs`
- `BattleLifecyclePhaseContractTests.cs`
- `BattleNeutralPresentationRouterTests.cs`
- `BattlePresentationInteractionContextTests.cs`
- `BattlePresentationProtocolTests.cs`
- `BattleReadyMovementContinuationTests.cs`
- `BattleResolutionPlanTests.cs`
- `BattleRollGateTests.cs`
- `CharacterDefaultCardDataContractTests.cs`
- `FullBattleIntegrationRegressionTests.cs`

Batch 4B：

`FullBattleIntegrationRegressionTests` 已开始消费 Shared Infrastructure，使用
`BattleTestContext` 与 `BattleScenarioBuilder` 构造 production fixture，但仍属于
Legacy；Mode103 仍为单一聚合入口，原有 28 项检查保持不变。

Batch 4C：

Mode103 的默认 synthetic character、标准 card state 和 synthetic enemy intent 分别开始
消费 `TestCharacterFactory`、`TestCardFactory` 和 `TestIntentFactory`。`CreateCardData`、
2.5x 特殊 CardData 与 custom-speed CharacterData 仍保留在 Legacy 测试中，以保持原有
fixture 语义。

Batch 5A：

`FullBattleIntegrationRegressionTests` 开始消费第一套 Formal Suite。Test4 的 Intent
Pattern / Cycle / Target 部分迁到 `EnemyIntentTests`，TurnCycle integration 仍为 Legacy；
Test5 的测试逻辑由 `EnemyIntentTests` 提供，Mode103 仅保留 compatibility result slot。

Batch 5B：

`BattleDeckManifestTests` 继续保留 Mode109 的聚合入口，并消费 `Assets/Tests/Suites/Cards/CardDeckManifestTests.cs` 的四个 Cards Formal Case。Mode89 standalone 已退休；Mode109 standalone 也已退休，该 wrapper 现在为 Mode114 保留，FirstStrike policy regression 通过保留的 Mode89 compatibility wrapper 继续服务。

Batch 5C：

`BattleExecutionPlanFirstStrikePolicyTests` 是保留的历史 Mode89 compatibility aggregation wrapper，13 个 FirstStrike test implementation 由 `Assets/Tests/Suites/Execution/FirstStrikeExecutionTests.cs` 提供。Mode109 仍调用该 wrapper；Mode89 不再有可选择的 standalone enum/dispatch。

Batch 5D：

`BattleDeckBootstrapPresetTests` 现在是 Mode114 compatibility aggregation wrapper。Cards domain 由 `CardDeckManifestTests` 提供，Bootstrap domain 由 `Assets/Tests/Suites/Bootstrap/DeckPresetBootstrapTests.cs` 提供；DeckManifest regression 仍通过 Mode109 wrapper。

Batch 3A 从 Production Runtime 文件提取的 Embedded Legacy Test classes：

- `BattleDeckManifestTests.cs`
- `BattleAllInBasicTests.cs`
- `BattleConservationAbilityTests.cs`
- `BattleDeckBootstrapPresetTests.cs`
- `BattleDeckHandGroupingTests.cs`
- `BattleGameSettingsIntegrationTests.cs`

这些文件只是从 Production Core 目录隔离，尚未转换为正式 Suite；不改变 class 名、namespace、方法、assertion 或 Mode。

Batch 6A：

Mode89 standalone enum 与 runner dispatch 已退休。该文件不再对应可选择的 standalone Mode，仅保留历史兼容 aggregation / logging；Mode109 是当前 consumer，13 个实际 FirstStrike Case 的正式所有者是 `FirstStrikeExecutionTests`。

Batch 6B：

`BattleDeckManifestTests.cs` 不再对应可直接选择的 Mode109；Mode109 standalone enum 与 runner dispatch 已退休。该文件只保留 historical compatibility aggregation / logging，consumer 为 `BattleDeckBootstrapPresetTests` / Mode114。实际 Cards tests 为 `CardDeckManifestTests`；Execution 继续使用 retained Mode89 wrapper → `FirstStrikeExecutionTests`。

Batch 6C：

`BattleDeckBootstrapPresetTests.cs` 不再对应可直接选择的 Mode114；Mode114 standalone enum 与 runner dispatch 已退休。该文件只保留 historical compatibility aggregation / logging，consumer 为 `BattleDeckHandGroupingTests` / Mode115。实际 Cards tests 为 `CardDeckManifestTests`，Bootstrap tests 为 `DeckPresetBootstrapTests`；Execution 继续使用 retained Mode109 wrapper → retained Mode89 wrapper → `FirstStrikeExecutionTests`。
