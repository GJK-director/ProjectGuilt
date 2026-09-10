// 战斗生命周期的内部权威阶段。显示文本由BattleRuntimeState统一映射。
public enum BattleLifecyclePhase
{
    Init,
    Prepare,
    PlanReady,
    Executing,
    TurnResolved,
    TurnEnding,
    TurnEnded,
    PreparingNextTurn,
    BattleEnded
}
