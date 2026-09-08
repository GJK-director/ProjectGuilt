# Camera

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Responsibilities

记录 Camera Director、实际 Graybox Camera、输入屏蔽和相关 helper。

## Does Not Own

不拥有战斗 Interaction 分类、不拥有卡牌 Resolver、不拥有 UI 数据。

## Current Main Files

`BattleCameraDirector.cs`、`GrayboxBattleCameraController.cs`、`CameraInputBlocker.cs`、`GrayboxBillboard.cs`、`GrayboxConstantScreenSize.cs`。

## Runtime Flow

Scene Camera Controller 由 `BattleCameraDirector` 驱动；Scene Presenter 和 Turn Transition Coordinator 请求 Camera Grammar。

## Data Sources

Camera Profile、BattleScene 序列化对象、运行时角色位置。

## Related Tests

Modes 74、83–85、94–98、102–104；Presentation Sandbox 另有独立测试入口。

## Known Technical Debt

Camera 参数和 Camera Controller 的 Scene 序列化关系需要同时检查代码与 YAML。

## Migration Status

TRANSITIONAL；未物理迁移。
