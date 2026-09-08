# Architecture

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`3e16a1d9c9eefcdac9c357a3d5cba12095767bec`)

## CURRENT

当前 `Assets/Scripts` 主要目录：

- `Camera`
- `Characters`
- `Core`
- `Data`
- `Debug`
- `Presentation`
- `Story`
- `StoryDemo`
- `UI`

当前观察：

- Core 当前承担卡牌、Buff、Resolver、Execution、Lifecycle、Bootstrap 等多种职责。
- Battle Runtime 基本位于默认 `Assembly-CSharp`。
- Story 核心有独立 `ProjectGuilt.Story` asmdef。
- Story UGUI 有独立 `ProjectGuilt.Story.UGUI` asmdef。
- `Assets/Tests` 已存在，Legacy physical isolation 已开始。
- `CardLoadTest.cs` 已位于 `Assets/Tests/Legacy/Runner/CardLoadTest.cs`。
- 22 个 standalone Core Regression 已位于 `Assets/Tests/Legacy/Core/`。
- 这些测试仍与 Battle Runtime 处于默认 `Assembly-CSharp` 编译关系，没有独立 Test asmdef。
- 113 个 active `BattleTestMode` enum members 尚未重构；当前 Migration Inventory 也有对应的 113 条记录。

## TARGET

已冻结的目标功能域记录如下：

```text
Assets/Scripts/
├─ Battle/
│  ├─ Actions
│  ├─ Bootstrap
│  ├─ Buffs
│  ├─ Cards
│  ├─ EnemyIntent
│  ├─ Events
│  ├─ Execution
│  ├─ Guilt
│  ├─ Interactions
│  ├─ Lifecycle
│  ├─ Resolution
│  ├─ State
│  ├─ Targeting
│  ├─ Turn
│  └─ Units
├─ Presentation
├─ UI
├─ Data
├─ Settings
├─ Story
└─ StoryDemo

Assets/Tests/
Assets/ProjectDocs/
```

本阶段只完成 Legacy 测试的第一层物理隔离，尚未执行 Production Script 迁移。

## Assembly Policy

第一阶段不创建新的 Battle asmdef，不创建新的 Test asmdef，不改变现有 `Assembly-CSharp` 编译关系。

物理目录整理与 Assembly 边界调整分开执行，以避免同时引入两类风险。
