# Battle Buffs

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`3dc4f7132996bdced6ca5a5cbb128925eb1a071e`)

## Responsibilities

记录 Buff 定义、运行时 Buff、Pending Buff 和 Buff UI 入口。

## Does Not Own

不拥有全部 Card Effect 规则、不拥有 Presentation 的视觉状态。

## Current Main Files

Runtime：

- `Assets/Scripts/Battle/Buffs/Runtime/BuffData.cs`
- `Assets/Scripts/Battle/Buffs/Runtime/PendingBuffData.cs`
- `Assets/Scripts/Battle/Buffs/Runtime/BuffApplyTiming.cs`
- `Assets/Scripts/Battle/Buffs/Runtime/BuffCategory.cs`
- `Assets/Scripts/Battle/Buffs/Runtime/BuffExpireRule.cs`

UI：

- `Assets/Scripts/Battle/Buffs/UI/BattleBuffGroupUIView.cs`
- `Assets/Scripts/Battle/Buffs/UI/BattleBuffIconUIView.cs`

相关数据与兼容入口仍为 `BuffDefinitions.json`、`CardEffectExecutor.cs`。

## Runtime Flow

Buff JSON 由 `BuffDefinitionLoader` 读取；Factory/Effect Executor 和 `CharacterData` 使用运行时状态；TurnProcessor 处理时机。

## Data Sources

`ROOT/Assets/Resources/Data/Buffs/BuffDefinitions.json`，当前 16 个定义。

## Related Tests

Modes 47–50、70–72、105–113、126–131。

## Known Technical Debt

`BuffDefinitionLoader` 位于 `CardEffectExecutor.cs`；Debug Preview 组件仍位于 UI Prefab。

## Migration Status

TRANSITIONAL；Batch 2B 已完成上述 7 个 Buff Runtime/UI 文件的 feature-first 物理归类，未改变代码内容、namespace 或运行时行为。整体工程仍未完成最终模块化迁移。
