# Story System

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Responsibilities

记录独立 Story 核心、UGUI、Facade、内容来源和 Demo Host。

## Does Not Own

Story 模块不引用 ProjectGuilt Battle、Card、Character、Scene 或 UI 代码；宿主决定 Story 结束后的流程。

## Current Main Files

`ProjectGuilt.Story`、`ProjectGuilt.Story.UGUI`、`StorySceneFacade.cs`、`IntroStoryHost.cs`、`StoryPanelView.cs`。

## Runtime Flow

`NewGameText.unity` → `IntroStoryHost` → `StorySceneFacade` → `ResourcesStoryContentProvider` → Story Flow → `SceneLoadingOverlay` → `BattleScene`。

## Data Sources

`ROOT/Assets/Resources/Story/prologue_501.json`。

## Related Tests

`StoryTestHost.cs` 和 Story 相关 Demo/编辑器入口。

## Known Technical Debt

Story 是独立程序集，宿主接入和跨系统节点仍需要保持边界证据。

## Migration Status

TRANSITIONAL；未物理迁移。
