# Battle Cards

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`b10297c9be5bc244e6ed08592f32f5ab63f992b4`)

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

Legacy Mode tests for deck manifests and abilities are kept in `Assets/Tests/Legacy/Core/`; they are not Production Runtime files.
- `CardTestData.cs`

## Runtime Flow

`CardsTest.json` → `CardDataLoader` → `BattleUnitFactory`/`BattleDefinitionBootstrap` → `BattleCardState`；使用和资源提交由 `BattleCardManager`、Resolver、EventProcessor 协调。

## Data Sources

`ROOT/Assets/Resources/Data/CardsTest.json`。

## Related Tests

Modes 53–55、62–66、85–86、101、105–115、117–122、127–132 等相关测试。

## Known Technical Debt

`CardsTest.json` 文件名保留 Test；`CardTestData` 含兼容字段；其余卡牌兼容与 Legacy Mode 迁移仍处于过渡阶段。

## Migration Status

TRANSITIONAL；Batch 3A 已将嵌入 `BattleDeckManifest.cs` 的 Legacy Test classes 物理提取到 `Assets/Tests/Legacy/Core/`，Production 文件本身仍未迁移目录。
