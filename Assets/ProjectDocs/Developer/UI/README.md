# UI Documentation

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Responsibilities

记录 Battle UI、Card Hand、Action Slot、Relation、Buff、Status、Roll 和 Menu UI。

## Does Not Own

不拥有 Card/Resolver 规则，不拥有 Camera 数学，不拥有正式数据 Loader。

## Current Main Files

见 `BattleUI.md`；Battle UI 的运行时总协调仍在 `BattleSimpleUIController.cs`。

## Runtime Flow

BattleScene 序列化 UI → `BattleSimpleUIController` 绑定 RuntimeState → 各 View/Host 刷新。

## Data Sources

Runtime State、ActionSlot、Intent、CardState 和 Prefab 序列化引用。

## Related Tests

Modes 60–75、95、98–100、102、104、115、132。

## Known Technical Debt

UI 文件数量大，Relation/UI 测试仍在旧路径；部分 Debug Preview 挂在 Prefab 上。

## Migration Status

TRANSITIONAL；未物理迁移。
