# Battle Bootstrap

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Responsibilities

记录正式 BattleScene 入口、Definition Loader、RuntimeState 和 Intent Provider。

## Does Not Own

不拥有 Data JSON 的内容编辑、不拥有 Resolver 规则、不拥有 UI 具体布局。

## Current Main Files

`BattleSceneBootstrap.cs`、`BattleDefinitionBootstrap.cs`。

## Runtime Flow

`BattleSceneBootstrap.Start()` → `InitializeBattleScene()` → `BattleDefinitionBootstrap.CreateRuntimeState(...)` → `BattleSimpleUIController.InitializeFromRuntimeState(...)`。

`BattleSceneBootstrap` 保存 `activeBootstrapResult`，并向后续回合提供正式 `CreateIntentQueueForTurn` provider。

## Data Sources

Characters、Enemies、Encounters、Cards JSON；Deck preset 由 `GameSettingsState`/Inspector fallback 决定。

## Related Tests

Modes 56、61、103、109、114、115、131、133。

## Known Technical Debt

正式 Bootstrap 与 Debug/Legacy 初始化路径共存；Controller 只消费 RuntimeState，但 provider 由 Scene Bootstrap 持有。

## Migration Status

TRANSITIONAL；未物理迁移。
