# Enemy Intent

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Responsibilities

记录 Enemy Intent 定义、目标角色/槽位、Pattern/Cycle 和响应关系。

## Does Not Own

不拥有玩家 UI 选择、不拥有 Resolver 伤害公式、不拥有敌人 AI 决策实现。

## Current Main Files

`BattleEnemyIntent.cs`、`BattleEnemyIntentManager.cs`、`BattleDefinitionBootstrap.cs`、`EncounterDefinitions.json`。

## Runtime Flow

Encounter Definition → `BattleDefinitionBootstrap.CreateIntentQueueForTurn` → Enemy Intent Queue → ActionSlot response/FreeAction → ExecutionPlan。

## Data Sources

`ROOT/Assets/Resources/Data/Encounters/EncounterDefinitions.json`。当前 `encounter_test_001` 有 2 项默认 pattern 和 10 项 intent cycle。

## Related Tests

Modes 13、32–40、56–61、88–100、103、114、129、131。

## Known Technical Debt

`BattleSimpleUIController` 仍保留无正式 Context 时的 Fixed Compatibility Builder。

## Migration Status

TRANSITIONAL；未物理迁移。
