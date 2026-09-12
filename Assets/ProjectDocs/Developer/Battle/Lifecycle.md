# Battle Lifecycle

Status: CURRENT
Role: DOMAIN CONTRACT
Last Verified: 2026-09-11

路径与绑定 owner：[CodeMap](../CodeMap.md)。数据消费语义：[DataPipeline](../DataPipeline.md)。

## Responsibilities

阶段转换、执行启动、回合边界和终局。

## NOT Responsible

单卡全部资源规则、动画实现。

## Main Entry

BattleLifecycleController / BattleTurnProcessor。

## Data

RuntimeState、living participants、plan。

## Runtime Flow

Init → Prepare/PlanReady → Executing → TurnResolved → TurnEnding → TurnEnded → PreparingNextTurn → Prepare；终局走 BattleEnded。

## Invariants

进入状态须满足 controller guard；TurnStart/TurnEnd 与卡牌事件各有职责；表现结束与终局判定不能混写。

## Dependencies

Execution、Events、Units。

## Regression Tests

LifecycleController/PhaseContract/Timing/EndLock Legacy。实际 caller 见 [RegressionTestMap](../../Testing/RegressionTestMap.md)，需要时查 [Legacy inventory](../../Testing/LegacyModeMigration.md)。

## Manual Verification

完整回合及终局返回；步骤见 [ManualHarnesses](../../Testing/ManualHarnesses.md)。未运行不报告通过。

## Known Debt

同步和 scene-presented 回合入口并存。只在相关 feature/regression 需要时讨论，不因文件大小扩 scope。
