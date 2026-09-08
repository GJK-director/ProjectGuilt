# Battle Cards

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Responsibilities

记录卡牌定义、运行时卡牌实例、资源规则、Deck Manifest 和卡牌效果入口。

## Does Not Own

不拥有 Resolver 的最终结算、不拥有 UI 的显示布局、不替代 JSON Source of Truth。

## Current Main Files

- `BattleCardManager.cs`
- `BattleCardState.cs`
- `BattleBulletRules.cs`
- `BattleDeckManifest.cs`
- `CardEffectExecutor.cs`
- `CardKeywordData.cs`
- `CardDataLoader.cs`
- `CardTestData.cs`

## Runtime Flow

`CardsTest.json` → `CardDataLoader` → `BattleUnitFactory`/`BattleDefinitionBootstrap` → `BattleCardState`；使用和资源提交由 `BattleCardManager`、Resolver、EventProcessor 协调。

## Data Sources

`ROOT/Assets/Resources/Data/CardsTest.json`。

## Related Tests

Modes 53–55、62–66、85–86、101、105–115、117–122、127–132 等相关测试。

## Known Technical Debt

`CardsTest.json` 文件名保留 Test；`CardTestData` 含兼容字段；Deck Manifest 与测试类共存于一个文件。

## Migration Status

TRANSITIONAL；未物理迁移。
