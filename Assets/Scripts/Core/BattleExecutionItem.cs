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
    // 只保存槽位引用，不复制槽位。当前只给 UnrespondedEnemyIntent 使用。
    public List<BattleActionSlot> passiveGuardCandidates;

    // isCompleted = 是否已经完成
    // 用来记录这个执行项是否已经被执行过。
    public bool isCompleted;

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
        isCompleted = false;
    }
}
