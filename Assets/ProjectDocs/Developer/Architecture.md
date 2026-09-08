# Architecture

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

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
- `Assets/Tests` 尚未有测试代码；本批只建立目录 README。
- 大量测试仍在 `Assets/Scripts/Core`。
- `CardLoadTest.cs` 是主要 Legacy Mode Runner。

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

本轮尚未执行 Production Script 物理迁移。

## Assembly Policy

第一阶段不创建新的 Battle asmdef，不创建新的 Test asmdef，不改变现有 `Assembly-CSharp` 编译关系。

物理目录整理与 Assembly 边界调整分开执行，以避免同时引入两类风险。
