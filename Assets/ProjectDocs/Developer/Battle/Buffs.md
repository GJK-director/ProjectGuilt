# Battle Buffs

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Responsibilities

记录 Buff 定义、运行时 Buff、Pending Buff 和 Buff UI 入口。

## Does Not Own

不拥有全部 Card Effect 规则、不拥有 Presentation 的视觉状态。

## Current Main Files

`BuffData.cs`、`PendingBuffData.cs`、`BuffApplyTiming.cs`、`BuffCategory.cs`、`BuffExpireRule.cs`、`BuffDefinitions.json`、`CardEffectExecutor.cs`、`BattleBuffGroupUIView.cs`、`BattleBuffIconUIView.cs`。

## Runtime Flow

Buff JSON 由 `BuffDefinitionLoader` 读取；Factory/Effect Executor 和 `CharacterData` 使用运行时状态；TurnProcessor 处理时机。

## Data Sources

`ROOT/Assets/Resources/Data/Buffs/BuffDefinitions.json`，当前 16 个定义。

## Related Tests

Modes 47–50、70–72、105–113、126–131。

## Known Technical Debt

`BuffDefinitionLoader` 位于 `CardEffectExecutor.cs`；Debug Preview 组件仍位于 UI Prefab。

## Migration Status

TRANSITIONAL；未物理迁移。
