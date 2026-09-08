# Battle Execution

Status: TRANSITIONAL
Last Verified: 2026-09-08
Repository Basis: 当前本地 HEAD (`ce43786241b06f41deb439c0729d151b86c20c27`)

## Responsibilities

记录 ExecutionPlan、ExecutionItem、排序、Runner、Pause/Resume 和 ActionFinished 相关入口。

## Does Not Own

不拥有卡牌伤害公式、不拥有 Scene 表现、不拥有最终 Impact 伤害写入。

## Current Main Files

`BattleExecutionPlan.cs`、`BattleExecutionItem.cs`、`BattleExecutionPlanManager.cs`、`BattleExecutionPlanExecutor.cs`、`BattleExecutionRunner.cs`。

## Runtime Flow

ActionSlot/Intent → `BattleExecutionPlanManager` → ExecutionPlan → `BattleExecutionRunner`/`BattleExecutionPlanExecutor` → Resolver/Presentation。

## Data Sources

Runtime ActionSlot、Enemy Intent、BattleCardState。

## Related Tests

Modes 19–22、39–40、45、51、57–59、76–83、86、88–91、96、119、124、129。

## Known Technical Debt

Execution 与测试文件均在 Core/default Assembly-CSharp；Pausable 与同步路径存在多层兼容入口。

## Migration Status

TRANSITIONAL；未物理迁移。
