# Legacy Mode Migration Index

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

下表是当前 `BattleTestMode` 的事实索引。所有 Mode 定义位于 `ROOT/Assets/Scripts/Core/CardLoadTest.cs`，由 `CardLoadTest.Start()` 的连续 `if` 分发。除 103、107、113、132、133 外，本轮统一标记为 `NOT_ANALYZED_FOR_MIGRATION`；这些重要 Mode 标记为 `MAPPED_FOR_FUTURE_MIGRATION`。

| Mode | Current Name | Current Area | Current Entry | Migration Status | Notes |
|---:|---|---|---|---|---|
| 2 | BattleRuntimeStateEndCurrentTurnBasic | RuntimeState | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 3 | BattleRuntimeStatePrepareNextTurnBasic | RuntimeState | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 7 | BattleResolverResolveRespondedAttackVsAttackBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 8 | BattleResolverRespondedPlayerWinBothCardsResolvedBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 9 | BattleResolverRespondedEnemyWinBothCardsResolvedBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 10 | BattleResolverRespondedClashSinLoseResolvedBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 11 | BattleResolverResolveRespondedDefenseFullBlockBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 12 | BattleResolverResolveRespondedDefenseReducedDamageBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 13 | BattleResolverDefenseKnownEnemyPointBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 19 | ActionSlotExecutionPlanExecuteFreeAbilityBasic | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 20 | ActionSlotExecutionPlanExecuteHighSpeedFreeAttackMixedBasic | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 22 | ActionSlotExecutionPlanExecuteUnrespondedBasic | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 25 | ActionSlotPassiveGuardFullBlockBasic | Guard | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 26 | ActionSlotPassiveGuardReducedDamageBasic | Guard | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 32 | ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardReducedDamageBasic | Guard | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 33 | ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardFullBlockBasic | Guard | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 36 | ActionSlotExecutionPlanExecuteRespondedEnemyWinNoPassiveGuardBasic | Guard | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 37 | ActionSlotExecutionPlanExecuteRespondedPlayerWinPassiveGuardNotTriggeredBasic | Guard | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 39 | ActionSlotExecutionPlanExecuteRespondedTieLimit | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 40 | ActionSlotExecutionPlanExecuteMixedBasic | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 41 | BattleResolverRespondedDodgeVsAttackBasic | Resolution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 42 | ActionSlotPassiveDodgeUnrespondedBasic | Dodge | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 43 | ActionSlotPassiveDodgeAfterAttackLoseBasic | Dodge | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 44 | BattleEndedVictoryDefeatBasic | Lifecycle | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 45 | ExecutionPlanInvalidActionCompletionBasic | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 46 | SingleAllyDeathExecutionFilteringBasic | Lifecycle | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 47 | BuffTriggerConsumeOrderBasic | Buffs | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 48 | BuffDefinitionDataLayerBasic | Buffs | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 49 | BuffLifecycleBattleIntegrationBasic | Buffs | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 50 | BuffBeforeUseActionUnavailableBasic | Buffs | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 51 | ExecutionItemStatusBasic | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 52 | CardResolvedHitContractBasic | Events | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 53 | CardResourceSnapshotAndConsumeBasic | Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 54 | CardAssignmentEligibilityBasic | Cards/UI | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 55 | RealCardResourceMigrationBasic | Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 56 | BattleDefinitionDataBootstrapBasic | Bootstrap | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 57 | BattlePreparedActionAssignmentModelBasic | Actions | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 58 | BattleExecutionOrderingAndGuardPriorityBasic | Execution | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 59 | BattleContinuousDodgeLifecycleBasic | Dodge | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 60 | BattleCardDragAssignmentRoutingBasic | UI/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 61 | BattleAutomaticTurnCycleAndCooldownDragBasic | Lifecycle/UI | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 62 | BattleCardPrimaryPreviewContractBasic | UI/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 63 | BattleCardCooldownFutureTurnSemanticsBasic | Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 64 | BattleCardPrimaryVisualPresetBasic | UI/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 65 | BattleCardHoverAndDragMotionBasic | UI/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 66 | BattleCardClickAssignBasic | UI/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 67 | BattleCardClickInteractionIntegration | UI/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 68 | BattleCardExponentialMotionAndSpreadBasic | UI/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 69 | BattleActionSlotVisualInteractionBasic | UI/Actions | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 70 | BattleBuffGridLayoutBasic | UI/Buffs | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 71 | BattleBuffInspectorPreviewBasic | UI/Buffs | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 72 | BattlePermanentBulletBuffBasic | Buffs | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 73 | BattleActionRelationLineBasic | UI/Relations | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 74 | BattleCharacterStatusWorldFollowBasic | UI/Status | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 75 | BattleActionRelationInteractionFix | UI/Relations | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 76 | BattleLifecyclePhaseContractBasic | Lifecycle | `BattleLifecyclePhaseContractTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 77 | BattleLifecycleControllerBasic | Lifecycle | `BattleLifecycleControllerTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 78 | BattleInteractionStateAndEndLockBasic | Lifecycle | `BattleInteractionStateAndEndLockTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 79 | BattleExecutionPlanSingleItemAdvanceBasic | Execution | `BattleExecutionPlanSingleItemAdvanceTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 80 | BattleClashSessionBasic | Resolution | `BattleClashSessionTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 81 | BattleRollGateBasic | Execution | `BattleRollGateTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 82 | BattleResolutionPlanBasic | Resolution | `BattleResolutionPlanTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 83 | BattlePresentationProtocolBasic | Presentation | `BattlePresentationProtocolTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 84 | BattleClashEngagementBasic | Presentation | `BattleClashEngagementTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 85 | BattleLongRangeShootResourceContractBasic | Cards/Resolution | `BattleLongRangeShootResourceContractTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 86 | BattleFirstStrikeExecutionPlanBasic | Execution/Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 87 | BattleInteractionClassifierBasic | Interactions | `BattleInteractionClassifierTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 88 | BattleExecutionPlanInteractionBasic | Interactions/Execution | `BattleExecutionPlanInteractionTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 89 | BattleExecutionPlanFirstStrikePolicyBasic | Execution | `BattleExecutionPlanFirstStrikePolicyTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 90 | BattleExecutionInteractionContextBasic | Interactions | `BattleExecutionInteractionContextTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 91 | BattleExecutionEffectiveInteractionBasic | Interactions | `BattleExecutionEffectiveInteractionTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 92 | BattleGenericAttackVsDefenseBasic | Resolution | `BattleGenericAttackVsDefenseTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 93 | BattleGenericAttackVsDodgeBasic | Resolution | `BattleGenericAttackVsDodgeTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 94 | BattleGenericUnilateralAttackBasic | Resolution | `BattleGenericUnilateralAttackTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 95 | BattlePresentationInteractionContextBasic | Presentation | `BattlePresentationInteractionContextTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 96 | BattleGenericPausableRoutingBasic | Execution/Presentation | `BattleGenericPausableRoutingTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 97 | BattleNeutralPresentationRouterBasic | Presentation | `BattleNeutralPresentationRouterTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 98 | BattleReadyMovementContinuationBasic | Presentation | `BattleReadyMovementContinuationTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 99 | BattleMultiSlotIntentRelationQueryBasic | UI/Relations | `BattleMultiSlotIntentRelationQueryTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 100 | BattleMultiSlotRelationRenderingBasic | UI/Relations | `BattleMultiSlotRelationRenderingTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 101 | CharacterDefaultCardDataContractBasic | Data/Units | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 102 | CharacterPresentationBindingContractBasic | Presentation | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 103 | FullBattleIntegrationRegressionBasic | Bootstrap/Resolution/Intent | `FullBattleIntegrationRegressionTests.Run()` | MAPPED_FOR_FUTURE_MIGRATION | 28 项当前数据侧 Integration |
| 104 | BattleActionRollPanelLifecycleBasic | UI/Presentation | `BattleActionRollPanelLifecycleTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 105 | BattleCardCommonRulesPhaseOneBasic | Cards | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 106 | BattleResourceFoundationAndBasicKnife | Cards/Knife | CardLoadTest sequence | NOT_ANALYZED_FOR_MIGRATION | |
| 107 | BattleAngerAndKnifeCardsBasic | Cards/Knife/Resolution | `BattleAngerAndKnifeCardsBasicTests.Run(cards)` coroutine | MAPPED_FOR_FUTURE_MIGRATION | Anger/Knife/Multi-impact 等组合 |
| 108 | BattleBasicShootingLoop | Cards/Shooting | `BattleBasicShootingLoopTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | |
| 109 | BattleDeckManifestBasic | Cards/Decks | `BattleDeckManifestTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | |
| 110 | BattleAbilityPhaseBasic | Cards/Ability | `BattleAbilityPhaseBasicTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 111 | BattleAngerAndModificationAbility | Cards/Ability | `BattleAngerAndModificationAbilityTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | |
| 112 | BattleAllInBasic | Cards/Shooting | `BattleAllInBasicTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | |
| 113 | BattleConservationAbility | Cards/Shooting/Buffs | `BattleConservationAbilityTests.Run(cards)` | MAPPED_FOR_FUTURE_MIGRATION | Conservation/0 Bullet/Cooldown |
| 114 | BattleDeckBootstrapPreset | Bootstrap/Decks | `BattleDeckBootstrapPresetTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | |
| 115 | BattleDeckHandGroupingBasic | UI/Cards | `BattleDeckHandGroupingTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | |
| 116 | BattleLifecycleTimingBasic | Lifecycle/Events | `BattleLifecycleTimingTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 117 | BattleUsePolicyDataBasic | Cards | `BattleUsePolicyDataTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | |
| 118 | BattleAttackUsePolicyResolutionBasic | Cards/Resolution | `BattleAttackUsePolicyResolutionTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 119 | BattleCardUsedCommitBasic | Events/Cards | `BattleCardUsedCommitTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 120 | BattleGuardCardUsedCommitBasic | Events/Guard/Dodge | `BattleGuardCardUsedCommitTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 121 | BattleCardUsedConsequencesBasic | Cards/Events | `BattleCardUsedConsequencesTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 122 | BattleCardUsedResourceConsequencesBasic | Cards/Resources | `BattleCardUsedResourceConsequencesTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 123 | BattleCardResolvedBasic | Events | `BattleCardResolvedTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 124 | BattleActionFinishedBasic | Events/Execution | `BattleActionFinishedTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 125 | BattleImpactFactsBasic | Events/Resolution | `BattleImpactFactsTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 126 | BattleScopedDamageModifierBasic | Resolution/Events | `BattleScopedDamageModifierTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 127 | BattleHiddenPendingStateBasic | Cards/Resources | `BattleHiddenPendingStateTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 128 | BattleResourceSpecialStateNormalizationBasic | Cards/Resources | `BattleResourceSpecialStateNormalizationTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 129 | BattleRuntimeInteractionLifecycleBasic | Interactions/Lifecycle | `BattleRuntimeInteractionLifecycleTests.Run()` | NOT_ANALYZED_FOR_MIGRATION | |
| 130 | BattleRuleEvaluationAndEffectHookupBasic | Events/Effects | `BattleRuleEvaluationAndEffectHookupTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | |
| 131 | BattleDeckFrozenSemanticsMigration | Cards/Decks | `BattleDeckFrozenSemanticsMigrationTests.Run(cards)` | NOT_ANALYZED_FOR_MIGRATION | |
| 132 | BattleCardKeywordPresentationBasic | UI/Cards/Keywords | `BattleCardKeywordPresentationTests.Run(cards)` | MAPPED_FOR_FUTURE_MIGRATION | Card Keyword Presentation/Tooltip |
| 133 | BattleGameSettingsIntegrationBasic | Settings/Bootstrap | `BattleGameSettingsIntegrationTests.Run()` | MAPPED_FOR_FUTURE_MIGRATION | Settings/Deck/Display |

本索引不决定 KEEP、DELETE、MERGE 或 REPLACED。
