// 脚本中文说明：战斗执行项。负责记录执行计划中的单个处理项目，例如已响应敌人意图、无人响应敌人意图或自由行动。
using System.Collections.Generic;

// BattleExecutionItemType = 战斗执行项类型
// Type = 类型，用来区分这一项到底是响应敌人意图、无人响应敌人意图，还是自由行动。
public enum BattleExecutionItemType
{
    // RespondedEnemyIntent = 已响应的敌人意图
    // 表示这个执行项会处理“已经被玩家槽位响应”的敌人意图。
    RespondedEnemyIntent,

    // UnrespondedEnemyIntent = 无人响应的敌人意图
    // 表示这个执行项会处理“没有任何玩家槽位响应”的敌人意图。
    UnrespondedEnemyIntent,

    // FreeAction = 自由行动
    // 表示这个执行项不是响应敌人意图，而是玩家自己主动安排的普通行动。
    FreeAction
}

// BattleExecutionItemStatus = 战斗执行项状态
public enum BattleExecutionItemStatus
{
    Pending,
    Executed,
    Skipped,
    Failed
}

// BattleExecutionItemOutcomeReason = 战斗执行项结果原因
public enum BattleExecutionItemOutcomeReason
{
    None,
    ActionUnavailable,
    ActorDead,
    ActualTargetDead,
    BattleEnded,
    ResponseUnavailableFallbackToUnresponded,
    InvalidData,
    UnsupportedExecutionType,
    UnsupportedResolveType,
    ResolverFailure,
    TieLimitReached
}

// BattleExecutionItem = 战斗执行项
// Item = 项目 / 条目，表示执行计划中的一小步。
public class BattleExecutionItem
{
    // order = 执行顺序
    // 数字越小，越先处理。
    public int order;

    // executionType = 执行项类型
    // 使用 BattleExecutionItemType 枚举，决定这一项属于哪种处理类型。
    public BattleExecutionItemType executionType;

    // enemyIntent = 敌人意图
    // BattleEnemyIntent = 战斗敌人意图，记录敌人要攻击谁、攻击哪个槽位、实际目标是谁。
    // 如果这一项是 FreeAction，自由行动可能不需要绑定 enemyIntent。
    public BattleEnemyIntent enemyIntent;

    // actionSlot = 行动槽位
    // BattleActionSlot = 战斗行动槽位，记录玩家把哪张卡放进哪个槽位，以及是否响应敌人意图。
    // 如果这一项是 UnrespondedEnemyIntent，无人响应敌人意图时可能没有 actionSlot。
    public BattleActionSlot actionSlot;

    // passiveGuardCandidates = 被动守备候选槽位
    // 由 PlanManager 收集并按槽位序号升序保存，只保存槽位引用，不复制槽位。
    // 当前同时服务 UnrespondedEnemyIntent，以及 RespondedEnemyIntent 中的 Attack vs Attack 敌人胜利分支。
    // 执行或结算时会再次验证候选是否仍有效；一张敌人卡最多触发一张守备。
    // 未触发的候选不会 MarkUsed，也不会进入 CD。
    public List<BattleActionSlot> passiveGuardCandidates;

    // isCompleted = 是否已经完成
    // 兼容旧逻辑的完成字段；正式状态以 status / outcomeReason 为准。
    public bool isCompleted;

    // status = 第一版正式执行项状态
    public BattleExecutionItemStatus status;

    // outcomeReason = 状态对应的结果原因
    public BattleExecutionItemOutcomeReason outcomeReason;

    // BattleExecutionItem = 战斗执行项构造函数
    // 构造函数负责创建一个新的执行项，并把执行顺序、执行类型、敌人意图、行动槽位保存进去。
    public BattleExecutionItem(
        // order = 执行顺序
        int order,

        // executionType = 执行项类型
        // BattleExecutionItemType = 战斗执行项类型枚举。
        BattleExecutionItemType executionType,

        // enemyIntent = 敌人意图
        // BattleEnemyIntent = 战斗敌人意图。
        BattleEnemyIntent enemyIntent,

        // actionSlot = 行动槽位
        // BattleActionSlot = 战斗行动槽位。
        BattleActionSlot actionSlot,

        // passiveGuardCandidates = 被动守备候选槽位
        // 如果没有候选，允许传 null，构造函数会转为空列表。
        List<BattleActionSlot> passiveGuardCandidates = null
    )
    {
        this.order = order;
        this.executionType = executionType;
        this.enemyIntent = enemyIntent;
        this.actionSlot = actionSlot;
        this.passiveGuardCandidates = passiveGuardCandidates != null
            ? passiveGuardCandidates
            : new List<BattleActionSlot>();
        status = BattleExecutionItemStatus.Pending;
        outcomeReason = BattleExecutionItemOutcomeReason.None;
        isCompleted = false;
    }

    public void MarkExecuted(BattleExecutionItemOutcomeReason reason = BattleExecutionItemOutcomeReason.None)
    {
        SetStatus(BattleExecutionItemStatus.Executed, reason, true);
    }

    public void MarkSkipped(BattleExecutionItemOutcomeReason reason)
    {
        SetStatus(BattleExecutionItemStatus.Skipped, reason, true);
    }

    public void MarkFailed(BattleExecutionItemOutcomeReason reason)
    {
        SetStatus(BattleExecutionItemStatus.Failed, reason, false);
    }

    void SetStatus(
        BattleExecutionItemStatus newStatus,
        BattleExecutionItemOutcomeReason reason,
        bool completed
    )
    {
        status = newStatus;
        outcomeReason = reason;
        isCompleted = completed;
    }
}
