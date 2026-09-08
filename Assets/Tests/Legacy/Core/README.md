# Legacy Core Tests

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`b10297c9be5bc244e6ed08592f32f5ab63f992b4`)

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

Batch 3A 从 Production Runtime 文件提取的 Embedded Legacy Test classes：

- `BattleDeckManifestTests.cs`
- `BattleAllInBasicTests.cs`
- `BattleConservationAbilityTests.cs`
- `BattleDeckBootstrapPresetTests.cs`
- `BattleDeckHandGroupingTests.cs`
- `BattleGameSettingsIntegrationTests.cs`

这些文件只是从 Production Core 目录隔离，尚未转换为正式 Suite；不改变 class 名、namespace、方法、assertion 或 Mode。
