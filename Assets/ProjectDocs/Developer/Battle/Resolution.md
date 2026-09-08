# Battle Resolution

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Responsibilities

记录 Clash、Roll、ResolutionPlan、BattleImpact、DamageModifier 和伤害结算入口。

## Does Not Own

不拥有卡牌 UI、不拥有 Camera、不替代 CardUsed/CardResolved 的生命周期文档。

## Current Main Files

`BattleResolver.cs`、`BattleResolutionPlan.cs`、`BattleCalculator.cs`、`BattleClashSession.cs`、`ClashResult.cs`。

## Runtime Flow

Execution Item → Resolver 建立 Clash/ResolutionPlan → Roll → Impact → EventProcessor 广播 Damage/Hit/AfterDamage/AfterKill。

## Data Sources

BattleCardState、BattleExecutionAction、Interaction Context、CardTestData。

## Related Tests

Modes 7–13、25–43、80–82、87–93、103、106–113、125–131。

## Known Technical Debt

Resolver 文件规模较大，并保留部分兼容适配路径；具体旧分支的最终用途需单独审计。

## Migration Status

TRANSITIONAL；未物理迁移。
