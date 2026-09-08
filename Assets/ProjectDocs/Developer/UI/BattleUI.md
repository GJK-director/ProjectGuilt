# Battle UI

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Responsibilities

记录 `BattleSimpleUIController`、Card UI、Action Slot UI、Relation UI、Buff UI、Status UI、Roll UI。

## Does Not Own

不拥有正式战斗规则、Resolver、Camera Director 或 Bootstrap Context 所有权。

## Current Main Files

- `BattleSimpleUIController.cs`
- `BattleCardHandUIView.cs`、`BattleCardUIView.cs`、`BattleCardInteractionCoordinator.cs`
- `BattleActionSlotUIView.cs`、`BattleSelfActionDropZone.cs`
- `BattleActionRelationLineController.cs` 及 Relation UI 文件
- `BattleBuffGroupUIView.cs`、`BattleBuffIconUIView.cs`
- `BattleCharacterStatusUIView.cs`、`BattleHpUIView.cs`、`BattleGuiltUIView.cs`
- `BattleActionRollPanelHost.cs`、`BattleActionRollPanelSideView.cs`

## Runtime Flow

Scene/Prefab UI 引用 → Controller 绑定 RuntimeState → Planning/Intent/Execution View 更新；Self Placement 不由 Relation Line 绘制。

## Data Sources

Runtime State、CardState、ActionSlot、EnemyIntent、Prefab/Scene YAML。

## Related Tests

Modes 60–75、95、98–100、102、104、115、132。

## Known Technical Debt

`BattleSimpleUIController.cs` 同时承担初始化、Planning、Turn Cycle、Legacy 兼容和 UI 刷新。

## Migration Status

TRANSITIONAL；未物理迁移。
