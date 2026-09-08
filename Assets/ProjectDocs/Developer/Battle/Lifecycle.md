# Battle Lifecycle

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Responsibilities

记录 Init、Prepare、PlanReady、Executing、TurnResolved、BattleEnded 的当前入口。

## Does Not Own

不拥有单张卡牌的完整资源规则，不拥有 Scene Presentation 的视觉完成时机。

## Current Main Files

`BattleLifecycleController.cs`、`BattleLifecyclePhase.cs`、`BattleTurnProcessor.cs`。

## Runtime Flow

Bootstrap → Init/Prepare → Planning → ExecutionStart → Execution → TurnEnd/TurnResolved → Next Turn 或 BattleEnded。

## Data Sources

`BattleRuntimeState`、ActionSlots、ExecutionPlan、Living Participants。

## Related Tests

Modes 2、3、44–46、76–79、81、96、98、103、116、123、124、129。

## Known Technical Debt

Legacy/Debug 初始化与正式 Bootstrap 并存；当前测试仍位于默认程序集。

## Migration Status

TRANSITIONAL；未物理迁移。
