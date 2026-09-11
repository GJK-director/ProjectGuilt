# Camera

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-11

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

入场、焦点、接敌、恢复和投影/输入支持。

## NOT Responsible

战斗顺序、伤害、行动安排。

## Main Entry

BattleCameraDirector / GrayboxBattleCameraController。

## Data

Camera 参数、位置与相关表现 Profile。

## Runtime Flow

sceneLoaded 按需 Director → Presenter/Coordinator 请求 → Camera 更新。

## Invariants

动态入口不能因无 YAML 引用而删除；视觉调整不改变规则执行顺序。

## Dependencies

Presenter、TurnCoordinator、Spawner。

## Regression Tests

相关 Presentation Legacy；人工视觉验收。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

BattleScene 热键、Sandbox；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

CURRENT + DEFERRED_DEBT：Director；fallback/场景参数共存。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
