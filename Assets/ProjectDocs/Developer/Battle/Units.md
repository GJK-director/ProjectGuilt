# Battle Units

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Responsibilities

记录角色运行时状态、Definition → Unit、World View 绑定。

## Does Not Own

不拥有卡牌定义来源、不拥有 Presentation 具体时序、不拥有 Scene 入口。

## Current Main Files

`CharacterData.cs`、`BattleUnitFactory.cs`、`BattleUnitViewSpawner.cs`。

## Runtime Flow

Character/Enemy Definition → `BattleUnitFactory` → `CharacterData`/`BattleCardState` → `BattleUnitViewSpawner` → World/UI View。

## Data Sources

CharacterDefinitions、EnemyDefinitions、CardsTest、Prefab key。

## Related Tests

Modes 46、56、74、101–103、114、133。

## Known Technical Debt

运行时 Unit 与 Unity View 通过 ID、Prefab key 和序列化引用共同绑定；间接绑定需继续保留证据。

## Migration Status

TRANSITIONAL；未物理迁移。
