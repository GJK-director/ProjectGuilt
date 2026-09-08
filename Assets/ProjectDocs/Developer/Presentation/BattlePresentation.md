# Battle Presentation

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Responsibilities

记录正式 Scene Presentation、Interaction Router、Players、Profiles 和 Turn Transition。

## Does Not Own

不拥有战斗数值、卡牌资源提交、Enemy Intent 生成或 UI Planning 数据所有权。

## Current Main Files

`BattleSceneExecutionPresenter.cs`、`BattlePresentationRouter.cs`、Attack/Defense/Dodge/LongRange/Special Presentation Players、各 Profile、`BattleTurnTransitionPresentationCoordinator.cs`。

## Runtime Flow

Execution item → Presentation Protocol → Scene Presenter → Presentation Interaction Context/Router → Player + Camera + Character Controller → completion callback。

## Data Sources

`ROOT/Assets/Settings` 下 Profile assets，以及 BattleScene/Prefab 序列化引用。

## Related Tests

Modes 83、84、95–98、102–104；部分测试使用 GameObject，部分只验证 Contract。

## Known Technical Debt

正式 Player、Debug Harness、Sandbox 和兼容 Presenter 并存；实际引用需要保持 Unity 序列化证据。

## Migration Status

TRANSITIONAL；未物理迁移。
