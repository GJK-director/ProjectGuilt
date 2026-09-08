# Runtime Entry Points

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## 正式流程

```text
Menu
→ NewGameText
→ BattleScene
```

### Menu

- `ROOT/Assets/Scenes/Menu.unity`
- `MainMenuController.StartNewGame()`
- 序列化 `newGameSceneName = NewGameText`
- `SceneManager.LoadSceneAsync(newGameSceneName)`

### NewGameText

- `ROOT/Assets/Scenes/NewGameText.unity`
- `IntroStoryHost`
- `StorySceneFacade`
- `storyId = prologue_501`
- Story 结束后由 `SceneLoadingOverlay` 加载 `BattleScene`

### BattleScene

- `ROOT/Assets/Scenes/BattleScene.unity`
- `BattleSceneBootstrap.Start()` → `InitializeBattleScene()`
- `BattleDefinitionBootstrap.CreateRuntimeState(...)`
- `BattleSimpleUIController.InitializeFromRuntimeState(...)`
- `BattleSceneBootstrap` 保存 `activeBootstrapResult`
- `BattleFormalPresentationTestHarness` 可在正式初始化前准备场景测试数据

战斗结束后，`BattleEndPanelController` 使用 `SceneManager.LoadSceneAsync` 返回 `Menu`。

## Build Scene

Enabled：

- `Assets/Scenes/Menu.unity`
- `Assets/Scenes/NewGameText.unity`
- `Assets/Scenes/BattleScene.unity`

Disabled：

- `Assets/Scenes/SampleScene.unity`

`BattlePresentationSandbox.unity`、`BattleSimpleUITest_02.unity`、`BattleTest.unity`、`StorySample.unity` 当前不在 Build Settings。
