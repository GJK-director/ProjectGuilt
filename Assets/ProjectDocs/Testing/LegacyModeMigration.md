# Legacy Mode Migration Index

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`3e16a1d9c9eefcdac9c357a3d5cba12095767bec`)

下表是当前 `BattleTestMode` 的事实索引。所有 Mode 定义位于 `ROOT/Assets/Tests/Legacy/Runner/CardLoadTest.cs`，由 `CardLoadTest.Start()` 的连续 `if` 分发。除 103、107、113、132、133 外，本轮统一标记为 `NOT_ANALYZED_FOR_MIGRATION`；这些重要 Mode 标记为 `MAPPED_FOR_FUTURE_MIGRATION`。Mode 数量与 ID 不因本批移动而变化。

## Inventory Reconciliation

- Active BattleTestMode enum members: **113**
- Migration inventory records: **113**
- ACTIVE_ENUM: **113**
- HISTORICAL_ONLY: **0**
- SUBCASE: **0**
- DUPLICATE_RECORD: **0**
- DOC_ONLY_UNKNOWN: **0**
- INVALID_RECORD: **0**
- Actual enum members covered by this table: **113/113**
- Actual enum members not covered by this table: **0**

本次按当前源码逐项以 Mode ID 和成员名进行 exact matching；所有 113 条记录均对应一个当前 enum member。`CardLoadTest.Start()` 同样存在 113 个一对一 dispatch branch，没有发现缺失、额外或重复 dispatch。enum 数值没有 duplicate value，也没有 alias。

本次审计确认 `113` 的来源为当前 `BattleTestMode` enum 成员数；它同时与 dispatch branch 数和 Migration Inventory Record 数一致，不是未经证明的历史或子测试计数。

| Mode | Current Name | Current Area | Current Entry | Migration Status | Inventory Class | Notes |
|---:|---|---|---|---|---|---|
| 2 | BattleRuntimeStateEndCurrentTurnBasic | RuntimeState | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 3 | BattleRuntimeStatePrepareNextTurnBasic | RuntimeState | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 7 | BattleResolverResolveRespondedAttackVsAttackBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 8 | BattleResolverRespondedPlayerWinBothCardsResolvedBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 9 | BattleResolverRespondedEnemyWinBothCardsResolvedBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 10 | BattleResolverRespondedClashSinLoseResolvedBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 11 | BattleResolverResolveRespondedDefenseFullBlockBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 12 | BattleResolverResolveRespondedDefenseReducedDamageBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 13 | BattleResolverDefenseKnownEnemyPointBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 19 | ActionSlotExecutionPlanExecuteFreeAbilityBasic | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 20 | ActionSlotExecutionPlanExecuteHighSpeedFreeAttackMixedBasic | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 22 | ActionSlotExecutionPlanExecuteUnrespondedBasic | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 25 | ActionSlotPassiveGuardFullBlockBasic | Guard | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 26 | ActionSlotPassiveGuardReducedDamageBasic | Guard | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 32 | ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardReducedDamageBasic | Guard | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 33 | ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardFullBlockBasic | Guard | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 36 | ActionSlotExecutionPlanExecuteRespondedEnemyWinNoPassiveGuardBasic | Guard | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 37 | ActionSlotExecutionPlanExecuteRespondedPlayerWinPassiveGuardNotTriggeredBasic | Guard | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 39 | ActionSlotExecutionPlanExecuteRespondedTieLimit | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 40 | ActionSlotExecutionPlanExecuteMixedBasic | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 41 | BattleResolverRespondedDodgeVsAttackBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 42 | ActionSlotPassiveDodgeUnrespondedBasic | Dodge | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 43 | ActionSlotPassiveDodgeAfterAttackLoseBasic | Dodge | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 44 | BattleEndedVictoryDefeatBasic | Lifecycle | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 45 | ExecutionPlanInvalidActionCompletionBasic | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 46 | SingleAllyDeathExecutionFilteringBasic | Lifecycle | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 47 | BuffTriggerConsumeOrderBasic | Buffs | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 48 | BuffDefinitionDataLayerBasic | Buffs | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 49 | BuffLifecycleBattleIntegrationBasic | Buffs | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 50 | BuffBeforeUseActionUnavailableBasic | Buffs | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 51 | ExecutionItemStatusBasic | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 52 | CardResolvedHitContractBasic | Events | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 53 | CardResourceSnapshotAndConsumeBasic | Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 54 | CardAssignmentEligibilityBasic | Cards/UI | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 55 | RealCardResourceMigrationBasic | Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 56 | BattleDefinitionDataBootstrapBasic | Bootstrap | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 57 | BattlePreparedActionAssignmentModelBasic | Actions | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 58 | BattleExecutionOrderingAndGuardPriorityBasic | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 59 | BattleContinuousDodgeLifecycleBasic | Dodge | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 60 | BattleCardDragAssignmentRoutingBasic | UI/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 61 | BattleAutomaticTurnCycleAndCooldownDragBasic | Lifecycle/UI | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 62 | BattleCardPrimaryPreviewContractBasic | UI/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 63 | BattleCardCooldownFutureTurnSemanticsBasic | Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 64 | BattleCardPrimaryVisualPresetBasic | UI/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 65 | BattleCardHoverAndDragMotionBasic | UI/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 66 | BattleCardClickAssignBasic | UI/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 67 | BattleCardClickInteractionIntegration | UI/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 68 | BattleCardExponentialMotionAndSpreadBasic | UI/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 69 | BattleActionSlotVisualInteractionBasic | UI/Actions | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 70 | BattleBuffGridLayoutBasic | UI/Buffs | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 71 | BattleBuffInspectorPreviewBasic | UI/Buffs | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 72 | BattlePermanentBulletBuffBasic | Buffs | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 73 | BattleActionRelationLineBasic | UI/Relations | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 74 | BattleCharacterStatusWorldFollowBasic | UI/Status | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 75 | BattleActionRelationInteractionFix | UI/Relations | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 76 | BattleLifecyclePhaseContractBasic | Lifecycle | `BattleLifecyclePhaseContractTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 77 | BattleLifecycleControllerBasic | Lifecycle | `BattleLifecycleControllerTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 78 | BattleInteractionStateAndEndLockBasic | Lifecycle | `BattleInteractionStateAndEndLockTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 79 | BattleExecutionPlanSingleItemAdvanceBasic | Execution | `BattleExecutionPlanSingleItemAdvanceTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 80 | BattleClashSessionBasic | Resolution | `BattleClashSessionTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 81 | BattleRollGateBasic | Execution | `BattleRollGateTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 82 | BattleResolutionPlanBasic | Resolution | `BattleResolutionPlanTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 83 | BattlePresentationProtocolBasic | Presentation | `BattlePresentationProtocolTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 84 | BattleClashEngagementBasic | Presentation | `BattleClashEngagementTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 85 | BattleLongRangeShootResourceContractBasic | Cards/Resolution | `BattleLongRangeShootResourceContractTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 86 | BattleFirstStrikeExecutionPlanBasic | Execution/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 87 | BattleInteractionClassifierBasic | Interactions | `BattleInteractionClassifierTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 88 | BattleExecutionPlanInteractionBasic | Interactions/Execution | `BattleExecutionPlanInteractionTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 89 | BattleExecutionPlanFirstStrikePolicyBasic | Execution | `BattleExecutionPlanFirstStrikePolicyTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 90 | BattleExecutionInteractionContextBasic | Interactions | `BattleExecutionInteractionContextTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 91 | BattleExecutionEffectiveInteractionBasic | Interactions | `BattleExecutionEffectiveInteractionTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 92 | BattleGenericAttackVsDefenseBasic | Resolution | `BattleGenericAttackVsDefenseTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 93 | BattleGenericAttackVsDodgeBasic | Resolution | `BattleGenericAttackVsDodgeTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 94 | BattleGenericUnilateralAttackBasic | Resolution | `BattleGenericUnilateralAttackTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 95 | BattlePresentationInteractionContextBasic | Presentation | `BattlePresentationInteractionContextTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 96 | BattleGenericPausableRoutingBasic | Execution/Presentation | `BattleGenericPausableRoutingTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 97 | BattleNeutralPresentationRouterBasic | Presentation | `BattleNeutralPresentationRouterTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 98 | BattleReadyMovementContinuationBasic | Presentation | `BattleReadyMovementContinuationTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 99 | BattleMultiSlotIntentRelationQueryBasic | UI/Relations | `BattleMultiSlotIntentRelationQueryTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 100 | BattleMultiSlotRelationRenderingBasic | UI/Relations | `BattleMultiSlotRelationRenderingTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 101 | CharacterDefaultCardDataContractBasic | Data/Units | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 102 | CharacterPresentationBindingContractBasic | Presentation | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 103 | FullBattleIntegrationRegressionBasic | Bootstrap/Resolution/Intent | `FullBattleIntegrationRegressionTests.Run()` | MAPPED_FOR_FUTURE_MIGRATION | ACTIVE_ENUM | 28 项当前数据侧 Integration |
| 104 | BattleActionRollPanelLifecycleBasic | UI/Presentation | `BattleActionRollPanelLifecycleTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 105 | BattleCardCommonRulesPhaseOneBasic | Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 106 | BattleResourceFoundationAndBasicKnife | Cards/Knife | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 107 | BattleAngerAndKnifeCardsBasic | Cards/Knife/Resolution | `BattleAngerAndKnifeCardsBasicTests.Run(cards)` coroutine | MAPPED_FOR_FUTURE_MIGRATION | ACTIVE_ENUM | Anger/Knife/Multi-impact 等组合 |
| 108 | BattleBasicShootingLoop | Cards/Shooting | `BattleBasicShootingLoopTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 109 | BattleDeckManifestBasic | Cards/Decks | `BattleDeckManifestTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 110 | BattleAbilityPhaseBasic | Cards/Ability | `BattleAbilityPhaseBasicTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 111 | BattleAngerAndModificationAbility | Cards/Ability | `BattleAngerAndModificationAbilityTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 112 | BattleAllInBasic | Cards/Shooting | `BattleAllInBasicTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 113 | BattleConservationAbility | Cards/Shooting/Buffs | `BattleConservationAbilityTests.Run(cards)` | MAPPED_FOR_FUTURE_MIGRATION | ACTIVE_ENUM | Conservation/0 Bullet/Cooldown |
| 114 | BattleDeckBootstrapPreset | Bootstrap/Decks | `BattleDeckBootstrapPresetTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 115 | BattleDeckHandGroupingBasic | UI/Cards | `BattleDeckHandGroupingTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 116 | BattleLifecycleTimingBasic | Lifecycle/Events | `BattleLifecycleTimingTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 117 | BattleUsePolicyDataBasic | Cards | `BattleUsePolicyDataTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 118 | BattleAttackUsePolicyResolutionBasic | Cards/Resolution | `BattleAttackUsePolicyResolutionTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 119 | BattleCardUsedCommitBasic | Events/Cards | `BattleCardUsedCommitTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 120 | BattleGuardCardUsedCommitBasic | Events/Guard/Dodge | `BattleGuardCardUsedCommitTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 121 | BattleCardUsedConsequencesBasic | Cards/Events | `BattleCardUsedConsequencesTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 122 | BattleCardUsedResourceConsequencesBasic | Cards/Resources | `BattleCardUsedResourceConsequencesTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 123 | BattleCardResolvedBasic | Events | `BattleCardResolvedTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 124 | BattleActionFinishedBasic | Events/Execution | `BattleActionFinishedTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 125 | BattleImpactFactsBasic | Events/Resolution | `BattleImpactFactsTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 126 | BattleScopedDamageModifierBasic | Resolution/Events | `BattleScopedDamageModifierTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 127 | BattleHiddenPendingStateBasic | Cards/Resources | `BattleHiddenPendingStateTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 128 | BattleResourceSpecialStateNormalizationBasic | Cards/Resources | `BattleResourceSpecialStateNormalizationTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 129 | BattleRuntimeInteractionLifecycleBasic | Interactions/Lifecycle | `BattleRuntimeInteractionLifecycleTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 130 | BattleRuleEvaluationAndEffectHookupBasic | Events/Effects | `BattleRuleEvaluationAndEffectHookupTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 131 | BattleDeckFrozenSemanticsMigration | Cards/Decks | `BattleDeckFrozenSemanticsMigrationTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | ACTIVE_ENUM |  |
| 132 | BattleCardKeywordPresentationBasic | UI/Cards/Keywords | `BattleCardKeywordPresentationTests.Run(cards)` | MAPPED_FOR_FUTURE_MIGRATION | ACTIVE_ENUM | Card Keyword Presentation/Tooltip |
| 133 | BattleGameSettingsIntegrationBasic | Settings/Bootstrap | `BattleGameSettingsIntegrationTests.Run()` | MAPPED_FOR_FUTURE_MIGRATION | ACTIVE_ENUM | Settings/Deck/Display |

本索引不决定 KEEP、DELETE、MERGE 或 REPLACED。
