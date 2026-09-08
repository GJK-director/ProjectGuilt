# Code Map

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`b10297c9be5bc244e6ed08592f32f5ab63f992b4`)

本表记录当前路径到冻结目标功能域的映射，不表示已经移动文件。

| Current Path | Main Type | Current Responsibility | Target Domain | Migration Status | Notes |
|---|---|---|---|---|---|
| `Core/BattleActionSlot.cs` | slot model | 行动槽位、Placement、Continuous Dodge | Battle/Actions | NOT_MOVED | 当前仍在 Core |
| `Core/BattleActionSlotManager.cs` | manager | 创建和分配行动槽位 | Battle/Actions | NOT_MOVED | |
| `Core/BattleBulletRules.cs` | rules | Bullet、Pending、Modification、Conservation | Battle/Cards/Runtime | NOT_MOVED | |
| `Core/BattleCalculator.cs` | rules | 点数、伤害、Anger、Knife | Battle/Resolution | NOT_MOVED | |
| `Core/BattleCardManager.cs` | manager | 卡牌可用性、CD、Used、资源 | Battle/Cards/Runtime | NOT_MOVED | |
| `Core/BattleCardState.cs` | state | 运行时卡牌实例 | Battle/Cards/Runtime | NOT_MOVED | |
| `Core/BattleDeckManifest.cs` | manifest | Deck manifest、preset、分组 | Battle/Cards/Decks | NOT_MOVED | Embedded Legacy Tests extracted in Batch 3A |
| `Core/CardEffectData.cs` | DTO | Effect、Condition、Filter、Formula | Battle/Cards/Effects | NOT_MOVED | |
| `Core/CardEffectExecutor.cs` | executor | Effect 执行与旧兼容适配 | Battle/Cards/Effects | NOT_MOVED | |
| `Core/CardEffectType.cs` | constants | Effect 类型 | Battle/Cards/Effects | NOT_MOVED | |
| `Core/CardKeywordData.cs` | DTO/resolver | Keyword、Tooltip | Battle/Cards/Keywords | NOT_MOVED | |
| `Core/CardTestData.cs` | DTO | Card JSON 数据结构 | Battle/Cards/Data | NOT_MOVED | 文件名仍带 Test |
| `Core/CardUseConditionData.cs` | DTO | 使用条件 | Battle/Cards/Data | NOT_MOVED | |
| `Core/CardUseConditionType.cs` | constants | 条件类型 | Battle/Cards/Data | NOT_MOVED | |
| `Core/BattleEnemyIntent.cs` | model | Enemy Intent 和目标槽位 | Battle/EnemyIntent | NOT_MOVED | |
| `Core/BattleEnemyIntentManager.cs` | manager | Intent 辅助处理 | Battle/EnemyIntent | NOT_MOVED | |
| `Core/BattleExecutionAction.cs` | model | Execution Action | Battle/Execution | NOT_MOVED | |
| `Tests/Legacy/Core/BattleExecutionEffectiveInteractionTests.cs` | tests | Effective Interaction 测试文件 | Battle/Execution | MOVED_TO_LEGACY | Batch 2A physical isolation |
| `Core/BattleExecutionInteractionContext.cs` | context | Execution Interaction Identity | Battle/Execution | NOT_MOVED | |
| `Core/BattleExecutionItem.cs` | model | Execution Item 状态 | Battle/Execution | NOT_MOVED | |
| `Core/BattleExecutionPausablePolicy.cs` | policy | Pausable 执行策略 | Battle/Execution | NOT_MOVED | |
| `Core/BattleExecutionPlan.cs` | plan | ExecutionPlan | Battle/Execution | NOT_MOVED | |
| `Core/BattleExecutionPlanExecutor.cs` | executor | Item 执行、Fallback、事件提交 | Battle/Execution | NOT_MOVED | |
| `Core/BattleExecutionPlanManager.cs` | manager | Plan 创建、排序、Guard 选择 | Battle/Execution | NOT_MOVED | |
| `Core/BattleExecutionRunner.cs` | runner | Roll、Pause、Resume、Manual Gate | Battle/Execution | NOT_MOVED | |
| `Core/BattleLifecycleController.cs` | controller | Lifecycle transition、ExecutionStart | Battle/Lifecycle | NOT_MOVED | |
| `Core/BattleLifecyclePhase.cs` | enum | Lifecycle phase | Battle/Lifecycle | NOT_MOVED | |
| `Core/BattleTurnProcessor.cs` | processor | TurnStart、TurnEnd、Buff/CD | Battle/Turn | NOT_MOVED | |
| `Core/BattleResolutionPlan.cs` | plan | BattleImpact、Damage Modifier | Battle/Resolution | NOT_MOVED | |
| `Core/BattleResolver.cs` | resolver | Clash、Resolution、Impact | Battle/Resolution | NOT_MOVED | |
| `Core/BattleClashSession.cs` | session | Roll、Tie、资源快照 | Battle/Resolution | NOT_MOVED | |
| `Core/ClashResult.cs` | constants | Clash result | Battle/Resolution | NOT_MOVED | |
| `Core/BattleEventContext.cs` | context | 事件 payload | Battle/Events | NOT_MOVED | |
| `Core/BattleEventProcessor.cs` | processor | 统一事件广播 | Battle/Events | NOT_MOVED | |
| `Core/BattleTiming.cs` | constants | Timing vocabulary | Battle/Events | NOT_MOVED | 保留 Legacy timing |
| `Core/BattleRuntimeState.cs` | state | Battle Runtime 容器 | Battle/State | NOT_MOVED | |
| `Core/BattleTargeting.cs` | rules | Target、Intercept | Battle/Targeting | NOT_MOVED | |
| `Core/BattleInteractionClassifier.cs` | classifier | Interaction 分类 | Battle/Interactions | NOT_MOVED | |
| `Core/BattleUnitFactory.cs` | factory | Definition → Unit/CardState | Battle/Units | NOT_MOVED | |
| `Characters/CharacterData.cs` | state | HP、速度、Buff、卡牌、Guilt | Battle/Units | NOT_MOVED | |
| `Battle/Buffs/Runtime/BuffData.cs` | model | Buff 定义与运行时数据 | Battle/Buffs | MOVED | Batch 2B feature-first physical migration |
| `Battle/Buffs/Runtime/PendingBuffData.cs` | model | 延迟 Buff | Battle/Buffs | MOVED | Batch 2B feature-first physical migration |
| `Battle/Buffs/Runtime/BuffApplyTiming.cs` | constants | Buff 触发时机 | Battle/Buffs | MOVED | Batch 2B feature-first physical migration |
| `Battle/Buffs/Runtime/BuffCategory.cs` | constants | Buff 分类 | Battle/Buffs | MOVED | Batch 2B feature-first physical migration |
| `Battle/Buffs/Runtime/BuffExpireRule.cs` | constants | Buff 过期规则 | Battle/Buffs | MOVED | Batch 2B feature-first physical migration |
| `Core/BattleDefinitionBootstrap.cs` | bootstrap | Definition → Runtime/Intent | Battle/Bootstrap | NOT_MOVED | |
| `Core/BattleSceneBootstrap.cs` | bootstrap | BattleScene 入口、Context 持有 | Battle/Bootstrap | NOT_MOVED | |
| `Core/GuiltManager.cs` | manager | Guilt | Battle/Guilt | NOT_MOVED | |
| `Core/GameSettingsState.cs` | settings | PlayerPrefs 设置及运行时显示配置 | Settings | NOT_MOVED | BattleGameSettingsIntegrationTests extracted to Legacy/Core |
| `Camera/BattleCameraDirector.cs` | director | Camera Focus、Approach、Shake、Recovery | Presentation/Camera | NOT_MOVED | |
| `Camera/GrayboxBattleCameraController.cs` | controller | 实际 Camera Transform/Projection | Presentation/Camera | NOT_MOVED | |
| `Presentation/BattleSceneExecutionPresenter.cs` | presenter | Scene Presentation 编排 | Presentation/Scene | NOT_MOVED | |
| `Presentation/Battle*PresentationProfile.cs` | ScriptableObjects | 表现配置 | Presentation/Profiles | NOT_MOVED | 实际文件分散在 Presentation |
| `Presentation/*PresentationPlayer.cs` | players | Combat Presentation | Presentation/Combat | NOT_MOVED | |
| `Presentation/Battle*FxPlayer.cs` | FX players | Hit/Guard FX | Presentation/Effects | NOT_MOVED | |
| `Presentation/BattleTurnTransitionPresentationCoordinator.cs` | coordinator | Turn Transition | Presentation/Turn | NOT_MOVED | |
| `Battle/Buffs/UI/BattleBuffGroupUIView.cs` | view | Buff group UI | UI/Buffs | MOVED | Batch 2B feature-first physical migration |
| `Battle/Buffs/UI/BattleBuffIconUIView.cs` | view | Buff icon UI | UI/Buffs | MOVED | Batch 2B feature-first physical migration |
| `Core/BattleSimpleUIController.cs` | MonoBehaviour | UI、Planning、Turn Cycle、兼容路径 | UI/Battle | KEEP_IN_PLACE_TEMP | 本阶段不拆分多职责 Controller |
| `Core/CardDataLoader.cs` | loader | CardsTest JSON | Data/Loader | NOT_MOVED | |
| `Scripts/Data/Definitions/*` | DTOs | Character/Enemy/Encounter Definition | Data/Definitions | NOT_MOVED | |
| `Scripts/Data/Loaders/*` | loaders | Definition JSON Loader | Data/Loader | NOT_MOVED | |

除特别标记外，本表初始状态均为 `NOT_MOVED`。不确定文件统一使用 `NEEDS_TARGETED_AUDIT`，本表不表示迁移已经发生。
