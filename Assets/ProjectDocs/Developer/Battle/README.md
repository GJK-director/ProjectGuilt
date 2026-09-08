# Battle Documentation

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Responsibilities

本目录记录 Battle Runtime 的第一版功能导航。

## Does Not Own

不拥有最终目录迁移决策，不替代当前代码、JSON 或 Unity 序列化事实。

## Current Main Files

见 `Cards.md`、`Buffs.md`、`Execution.md`、`Resolution.md`、`Lifecycle.md`、`EnemyIntent.md`、`Units.md`、`Bootstrap.md`。

## Runtime Flow

`BattleSceneBootstrap` 创建 RuntimeState，`BattleSimpleUIController` 消费状态，Execution/Resolver/Presentation 继续按当前代码运行。

## Data Sources

`Resources/Data` 下的 Cards、Characters、Enemies、Encounters、Buffs JSON。

## Related Tests

Legacy Mode 位于 `CardLoadTest` 和 Core/Presentation/UI 测试文件。

## Known Technical Debt

Core 目录承担多种职责，测试与 Runtime 代码仍共享默认程序集。

## Migration Status

TRANSITIONAL；本批只建立文档，不迁移代码。
