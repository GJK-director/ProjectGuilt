# CodeMap

Status: CURRENT
Role: CANONICAL AI REPO MAP
Last Verified: 2026-09-11

路径事实唯一 owner。按 Domain 定位，不维护全部 helper 数据库；契约看领域文档，数据语义看 [DataPipeline](DataPipeline.md)，具体运行看 [RegressionTestMap](../Testing/RegressionTestMap.md) 和 [ManualHarnesses](../Testing/ManualHarnesses.md)。

## Domain Map

| Domain | Current Path | Primary Runtime Entry | Important Data | Main Consumers | Main Dependencies | Scene / Prefab / Profile | Regression Tests | Manual Harness | Status / Risk |
|---|---|---|---|---|---|---|---|---|---|
| Battle State | [Battle/State](../../Scripts/Battle/State) | BattleRuntimeState | 单位集合、槽位、计划、共享 guilt | Bootstrap/UI/Lifecycle | Runtime models | BattleScene（间接） | Lifecycle/FullBattle Legacy | BattleScene | CURRENT / 高 |
| Cards | [Battle/Cards](../../Scripts/Battle/Cards) | BattleCardManager、BattleCardState、BattleDeckManifest | CardsTest.json、preset manifest | Factory/Planning/Resolver/UI | Events、Buff/资源 | BattleCardUI | Cards Suite；资源/Ability Legacy | SampleScene/BattleScene | CURRENT / 高；兼容字段 |
| Buffs | [Battle/Buffs](../../Scripts/Battle/Buffs) | CharacterData 状态应用、BuffData/PendingBuffData | BuffDefinitions.json | Turn/Effects/Units/UI | Loader、Events | Ally/EnemyStatusUI | Buff/Conservation Legacy | Buff Preview | CURRENT / 高；旧 timing |
| Card Effects | [Battle/Cards/Effects](../../Scripts/Battle/Cards/Effects) | CardEffectExecutor | effects/condition/filter/formula | EventProcessor | Character、资源、结算修正 | BattleScene（间接） | RuleEvaluation/ScopedDamage Legacy | SampleScene | CURRENT / 高 |
| Turn | [Battle/Turn](../../Scripts/Battle/Turn) | BattleTurnProcessor.StartTurn/EndTurn | pending Buff、速度、CD | Lifecycle | Events、Character | BattleScene（间接） | LifecycleTiming Legacy | BattleScene | CURRENT / 高 |
| Lifecycle | [Battle/Lifecycle](../../Scripts/Battle/Lifecycle) | BattleLifecycleController | BattleLifecyclePhase、RuntimeState | UI/自动回合 | Runner、PlanManager、Turn | BattleScene | LifecycleController/PhaseContract Legacy | BattleScene | CURRENT / 高 |
| Actions / Planning | [Battle/Actions](../../Scripts/Battle/Actions) | BattleActionSlotManager | ActionSlot、placement、response | UI Router/Controller | Card eligibility、Targeting | ActionSlot/status UI | assignment/interaction Legacy | BattleScene | CURRENT / 高；DEFERRED_DEBT |
| Resolution / Clash | [Battle/Resolution](../../Scripts/Battle/Resolution) | BattleResolver、Calculator、ClashSession | roll snapshot、ResolutionPlan/impact | Executor/Runner | Cards、Character、Events | Presenter（间接） | Clash/ResolutionPlan/Generic Legacy | 正式 Harness | CURRENT / 高；DEFERRED_DEBT |
| Execution | [Battle/Execution](../../Scripts/Battle/Execution) | PlanManager、PlanExecutor、Runner | ExecutionPlan/Item/Action/Context | Lifecycle | Resolver、Presentation protocol | BattleScene | FirstStrike Suite；RollGate/Pausable Legacy | 正式 Harness | CURRENT / 高；DEFERRED_DEBT |
| EnemyIntent | [Battle/EnemyIntent](../../Scripts/Battle/EnemyIntent) | BattleDefinitionBootstrap.CreateIntentQueueForTurn | Encounter pattern/cycle、Enemy cardIDs | Scene Bootstrap provider | Definitions、ActionSlot | BattleScene | EnemyIntent Suite；Mode103 | SampleScene/BattleScene | CURRENT / 中高；生成在 Bootstrap |
| Targeting / Interaction | [Battle/Targeting](../../Scripts/Battle/Targeting) | BattleTargeting；BattleInteractionClassifier（Battle/Interactions） | speed、target、effective context | Planning/Executor/Presenter | Slot、Intent、Unit | 关系 UI（间接） | Classifier/EffectiveInteraction Legacy | BattleScene | CURRENT / 高 |
| Units | [Battle/Units](../../Scripts/Battle/Units) | BattleUnitFactory、CharacterData | Character/EnemyDefinitions | Bootstrap、规则、UI | CardManager、Definitions | World/Status Prefab 经 Spawner | DefaultCard/Binding Legacy；Bootstrap Suite | BattleScene | CURRENT / 高 |
| Bootstrap | [Battle/Bootstrap](../../Scripts/Battle/Bootstrap) | BattleSceneBootstrap.Start → InitializeBattleScene | encounterID、preset、single-unit | BattleScene | Loaders/Factory/UI/Settings/Harness | BattleScene | Bootstrap Suite；Mode103 | 正式 Harness | CURRENT / 高；DEFERRED_DEBT |
| Guilt | [Battle/Guilt](../../Scripts/Battle/Guilt) | GuiltManager.AddGuilt/GetCurrentGuilt | guiltGain、共享 RuntimeState | 卡牌使用/角色/UI | RuntimeState | Guilt UI | 罪卡/资源 Legacy | BattleScene | CURRENT / 中高；角色兼容值 |
| Events | [Battle/Events](../../Scripts/Battle/Events) | BattleEventProcessor.ProcessEvent | BattleTiming、EventContext | Turn/Resolver | CardManager、Pending/Conservation、Effects | 无直接资产 | CardUsed/Resolved/Impact/Timing Legacy | SampleScene | CURRENT / 高；legacy vocabulary |
| Data | [Data](../../Scripts/Data) | Card/Character/Enemy/Encounter/Buff Loader | Resources/Data JSON | Bootstrap/Factory/Effects/Tests | Resources、JSON、Definitions/Validation | Resources 数据 | Cards/Bootstrap/EnemyIntent Suite；数据 Legacy | SampleScene/BattleScene | CURRENT / 中高 |
| UI | [UI](../../Scripts/UI) | 局部 View/Host；Core/BattleSimpleUIController 跨流程协调 | RuntimeState、CardState、Prefab 字段 | Scene/UI input | Planning/Lifecycle/Spawner/Presentation | Hand/Card/Slot/详情/Roll Prefab | UI/关键词/关系线 Legacy | BattleScene/Preview | CURRENT / 高；Controller DEFERRED_DEBT |
| Presentation | [Presentation](../../Scripts/Presentation) | BattleSceneExecutionPresenter、Router、Players | request/completion、Profiles | Execution Runner | Camera/Character/UI | BattleScene、Sandbox、Settings profiles | Protocol/Engagement/Binding/Pausable Legacy | 正式 Harness/Sandbox | CURRENT / 高；DEFERRED_DEBT |
| Camera | [Camera](../../Scripts/Camera) | BattleCameraDirector、GrayboxBattleCameraController | framing/motion 参数、角色位置 | Presenter/TurnCoordinator | Camera、Spawner | BattleScene camera | 相关 Presentation Legacy | 热键/Sandbox | CURRENT / 高；DEFERRED_DEBT |
| Story | [Story](../../Scripts/Story) | StorySceneFacade → FlowController/NodeExecutor | Resources/Story/prologue_501.json | IntroStoryHost/StoryTestHost | ContentProvider、StoryView | NewGameText、StoryPanel | 无 Formal Story Suite；Editor validate | StoryTestHost/NewGameText | CURRENT / 中高；独立 asmdef |
| StoryDemo | [StoryDemo](../../Scripts/StoryDemo) | IntroStoryHost、SceneLoadingOverlay | storyId、battleSceneName、宿主 SFX | NewGameText | Story API、SceneManager | NewGameText | 剧情集成人工验证 | NewGameText | CURRENT / 中；生成音效占位 |
| Settings | [Settings](../../Scripts/Settings) | GameSettingsState；UI/MainMenu/MainMenuController | PlayerPrefs preference、one-shot pending/session deck、Inspector fallback | Menu/Bootstrap | Screen、DeckManifest | Menu | GameSettingsIntegration Legacy / Mode133 | Menu 主链 | CURRENT / 中 |

## Domain Contracts

[Battle](Battle/README.md)：[Cards](Battle/Cards.md)、[Buffs](Battle/Buffs.md)、[Execution](Battle/Execution.md)、[Resolution](Battle/Resolution.md)、[Lifecycle](Battle/Lifecycle.md)、[EnemyIntent](Battle/EnemyIntent.md)、[Units](Battle/Units.md)、[Bootstrap](Battle/Bootstrap.md)。

[Presentation](Presentation/README.md)：[BattlePresentation](Presentation/BattlePresentation.md)、[Camera](Presentation/Camera.md)。另见 [UI](UI/README.md)、[BattleUI](UI/BattleUI.md)、[Story](Story/StorySystem.md)、[Settings](Settings/MenuAndSettings.md)。

## Data and Asset Locations

- [CardsTest](../../Resources/Data/CardsTest.json)、[CharacterDefinitions](../../Resources/Data/Characters/CharacterDefinitions.json)、[EnemyDefinitions](../../Resources/Data/Enemies/EnemyDefinitions.json)、[EncounterDefinitions](../../Resources/Data/Encounters/EncounterDefinitions.json)、[BuffDefinitions](../../Resources/Data/Buffs/BuffDefinitions.json)。
- [Loaders](../../Scripts/Data/Loaders)：BuffDefinitionLoader 是独立文件；[Definitions](../../Scripts/Data/Definitions)、[Validation](../../Scripts/Data/Validation)。
- [Story JSON](../../Resources/Story/prologue_501.json)；[StoryPanel](../../Scripts/Story/Prefabs/StoryPanel.prefab)。
- [Scenes](../../Scenes)：Menu、NewGameText、BattleScene 正式主链；SampleScene 为 Legacy host；Sandbox 为人工入口。BattleTest、StorySample、BattleSimpleUITest_02 不默认视为正式入口；[_Recovery](../../_Recovery) 是恢复资产。
- [BattleCardUI](../../Art/battle/kapai/BattleCardUI.prefab)、[World Prefabs](../../Prefabs/Battle/Units/World)、[Status Prefabs](../../Prefabs/Battle/Units/UI)。
- [ActionSlotCardInfoPanel](../../Prefabs/BattleActionSlotCardInfoPanel.prefab)、[SecondaryInfoPanel](../../Prefabs/BattleSecondaryInfoPanel.prefab)、[ActionRollPanel](../../Resources/UI/BattleActionRollPanel.prefab)。
- [Presentation Profiles](../../Settings)：Attack/Guard/Dodge、ClashEngagement、Hit、SpecialLongRangeDuel 等资产。
- [Editor](../../Editor)：CardLoadTest/Sandbox/Camera Inspectors、IntroStorySceneSetup、UI Prefab generators。生成器会写资源，校验与重建必须区分。

## Test and Support Locations

- [Suites](../../Tests/Suites)：EnemyIntent/EnemyIntentTests、Cards/CardDeckManifestTests、Execution/FirstStrikeExecutionTests、Bootstrap/DeckPresetBootstrapTests。
- [Shared](../../Tests/Shared)：BattleConsolePresenter；Builders 下 BattleScenarioBuilder、TestCharacterFactory、TestCardFactory、TestIntentFactory；Fixtures 下 BattleTestContext。
- [Legacy/Core](../../Tests/Legacy/Core)：standalone regression 与 retained wrappers；[Legacy/Runner/CardLoadTest](../../Tests/Legacy/Runner/CardLoadTest.cs) 还包含内嵌测试。不可仅扫描 Core 判断全部覆盖。
- [StoryTestHost](../../Tests/Harness/Story/StoryTestHost.cs)：需显式宿主绑定，无 tracked Scene/Prefab 绑定。
- [Debug](../../Scripts/Debug)：BattleFormalPresentationTestHarness、BattleSceneDevelopmentHotkeys、BattleDebugSettings。
- [BattleCharacterTargetHitbox](../../Scripts/UI/BattleCharacterTargetHitbox.cs) 与 [BattleCharacterTargetOutline](../../Scripts/Presentation/BattleCharacterTargetOutline.cs)：可选的角色目标命中区与目标轮廓；Status UI / Spawner 通过 Handle 接线，缺少引用时安全跳过。
- [SandboxController](../../Scripts/Presentation/BattlePresentationSandboxController.cs) 与 [BuffGroupDebugPreview](../../Scripts/UI/BattleBuffGroupDebugPreview.cs) 分别保留 Scene/Prefab 绑定。
- Suite/Harness 空目录不等于已有实现；当前数量与 caller 事实由 [Testing README](../Testing/README.md) / RegressionTestMap 维护。

## Dynamic Runtime Entries

- [BattleCameraDirector](../../Scripts/Camera/BattleCameraDirector.cs)：BeforeSceneLoad 注册 sceneLoaded；有 BattleSimpleUIController 的 Scene 中按需创建 Director。
- [BattleEndPanelController](../../Scripts/UI/Battle/BattleEndPanelController.cs)：Bootstrap 调用 Bind，按需创建终局 UI。
- [Scene Presenter](../../Scripts/Presentation/BattleSceneExecutionPresenter.cs)：按需 AddComponent LongRangeShootVsAttack / SpecialLongRangeDuel Player。
- [BattleWorldFollowProjectionDiagnostic](../../Scripts/UI/Debug/BattleWorldFollowProjectionDiagnostic.cs)：保留的手工诊断工具；[BattleUnitViewSpawner](../../Scripts/UI/BattleUnits/BattleUnitViewSpawner.cs) 正式 Runtime 不再自动添加/绑定它，需要投影诊断时才人工挂载/使用。
- Roll Panel 通过 Resources 路径实例化。没有 Scene m_Script 引用不等于没有 Runtime consumer。

## Unity Serialization / Assembly Risk

移动已有 asset 必须带原 .meta，保留 GUID；不得重建已有 meta。Scene/Prefab/Profile 的 m_Script、fileID、override、UnityEvent 及 Resources/Scene 名称字符串均是绑定面。
type/namespace rename、asmdef/Editor/Resources 跨界需要显式风险核验，GUID 相同也不保证编译和加载关系不变。文本检查不能替代用户 Unity 验收。

[Story asmdef](../../Scripts/Story/ProjectGuilt.Story.asmdef)：ProjectGuilt.Story，显式 Newtonsoft.Json.dll，autoReferenced。
[UGUI asmdef](../../Scripts/Story/UGUI/ProjectGuilt.Story.UGUI.asmdef)：引用 ProjectGuilt.Story、UnityEngine.UI。其他 Battle/UI/Presentation/Tests 当前在默认 Assembly-CSharp；无 Test asmdef/asmref。

## Physical / Logical Mismatch

- [Core/BattleSimpleUIController.cs](../../Scripts/Core/BattleSimpleUIController.cs) 还含 BattleAutomaticTurnCycle/Result；逻辑上覆盖 UI 与回合。
- Buff UI 位于 Battle/Buffs/UI；Camera 参与 Presentation flow。
- EnemyIntent 生成 owner 在 Bootstrap；卡牌 DTO 在 Cards/Data，其他 Definition 在 Data。
- BattleBulletRules 同文件还含 Pending/Modification/Conservation 规则；类名不保证同名独立文件。
- 顶层 Cards、Characters、Managers 没有 C#；人工支持分布在 Tests、Debug、Presentation、UI。这些是导航事实，不是迁移任务。

## Deferred Debt

以下均为 CURRENT + DEFERRED_DEBT：BattleSimpleUIController、BattleSceneExecutionPresenter、BattleResolver、BattleActionSlotManager、BattleExecutionPlanExecutor、BattleSceneBootstrap、BattleCameraDirector。

不为了架构洁癖阻碍 Demo。只有具体 feature、regression 或 maintainability 需要才讨论拆分；不得因为文件大自行启动重构。兼容路径按需核验，DEPRECATED 不等于可以删除。
