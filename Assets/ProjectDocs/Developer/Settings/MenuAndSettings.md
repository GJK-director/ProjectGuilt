# Menu And Settings

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`b10297c9be5bc244e6ed08592f32f5ab63f992b4`)

## Responsibilities

记录主菜单跳转、Deck preference、Fullscreen、Resolution 设置。

## Does Not Own

不拥有 Battle Deck Manifest 内容、不拥有 Battle Resolver、不拥有 Story 内部执行。

## Current Main Files

`MainMenuController.cs`、`GameSettingsState.cs`、`Menu.unity`；`BattleGameSettingsIntegrationTests` 位于 `Assets/Tests/Legacy/Core/`。

## Runtime Flow

Menu 的 `MainMenuController` 加载 `NewGameText`；GameSettings 通过 PlayerPrefs 保存 Deck/Display 选项；BattleScene Bootstrap 读取 Deck preference。

## Data Sources

PlayerPrefs、Menu Scene 序列化字段、Deck Manifest。

## Related Tests

Mode133、Menu/Story/Battle Scene 的入口检查。

## Known Technical Debt

Settings 读取与 Battle Bootstrap 跨越默认程序集；场景字段和代码默认值需要同时核对。Mode133 仍是 Legacy Mode，不属于 Runtime Settings。

## Migration Status

TRANSITIONAL；未物理迁移。
