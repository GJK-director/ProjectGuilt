# Legacy Core Tests

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`3e16a1d9c9eefcdac9c357a3d5cba12095767bec`)

本批从 `Assets/Scripts/Core` 物理隔离到此目录的 22 个 standalone Core Regression 文件如下：

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

这些文件只是从 Production Core 目录隔离，尚未转换为正式 Suite；不改变 class 名、namespace、方法、assertion 或 Mode。
