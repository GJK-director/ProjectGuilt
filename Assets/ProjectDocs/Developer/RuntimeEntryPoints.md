# Runtime Entry Points

Status: CURRENT
Role: RUNTIME AND SCENE ENTRY
Last Verified: 2026-09-11

## Formal Scene Flow

Menu → NewGameText → BattleScene → Menu。

MainMenuController.StartNewGame 读取 newGameSceneName；当前代码默认值和 Menu 序列化值均为 NewGameText。
IntroStoryHost 通过 StorySceneFacade 打开 prologue_501；剧情结束由 SceneLoadingOverlay 加载 BattleScene。
BattleSceneBootstrap.Start → InitializeBattleScene → BattleDefinitionBootstrap.CreateRuntimeState → BattleSimpleUIController.InitializeFromRuntimeState。
Bootstrap 持有 activeBootstrapResult，并提供下一回合 CreateIntentQueueForTurn provider。正式数据创建后，已绑定 Formal Harness 可在 UI 首次读取前准备场景；默认 scenario=None。
终局通过 BattleEndPanelController 返回 Menu。

Build 启用 Menu、NewGameText、BattleScene；SampleScene disabled。Sandbox、BattleSimpleUITest_02、BattleTest、StorySample 不在 Build List。路径由 [CodeMap](CodeMap.md) 维护。

## Dynamic Entries

BattleCameraDirector 在 BeforeSceneLoad 注册 sceneLoaded，按需在含 BattleSimpleUIController 的场景创建自身。
BattleEndPanelController 由 Bootstrap.Bind 路径按需创建。
BattleSceneExecutionPresenter 按需添加 LongRangeShootVsAttack / SpecialLongRangeDuel Player。
Spawner 可添加 WorldFollowProjectionDiagnostic；Roll Panel 由 Resources 实例化。
因此 Scene YAML 无脚本绑定不是不可达证明。

## Verification

源码默认值、Scene 字段、Build List、Resources 与动态 consumer 分别核对；Unity 编译、Missing Script 和实际场景跳转由用户在 Editor 验收。人工入口见 [ManualHarnesses](../Testing/ManualHarnesses.md)。
