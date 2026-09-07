// 脚本中文说明：战斗时机常量。负责保存回合、行动、拼点、伤害及结算事件时机名称。
// BattleTiming = 战斗触发时机常量表
// 以后 JSON 里的 trigger 字段，尽量都从这里找对应名字
public static class BattleTiming
{
    // ================================
    // 回合阶段
    // ================================

    // TurnStart = 回合开始
    // 例如：回合开始时获得 Buff、处理待生效状态
    public const string TurnStart = "TurnStart";

    // ExecutionStart = 完成 Planning，正式进入本回合执行阶段
    public const string ExecutionStart = "ExecutionStart";

    // ================================
    // 单次行动 / 使用阶段
    // ================================

    // ActionStart = 当前 ExecutionItem / Action 正式开始处理
    // 晚于回合开始与硬性使用条件检查。
    public const string ActionStart = "ActionStart";

    // CardUsed = 卡牌已经被正式判定为 Used
    public const string CardUsed = "CardUsed";

    // ================================
    // Legacy 使用阶段
    // ================================

    // BeforeUse = Legacy / 兼容旧代码的卡牌使用前时机
    public const string BeforeUse = "BeforeUse";

    // OnPlay = Legacy / 兼容旧 JSON 的旧版使用时机
    public const string OnPlay = "OnPlay";


    // ================================
    // 拼点阶段
    // ================================

    // ClashStart = 拼点开始前
    // 主要给 Buff 检测用，例如拼点开始时修改点数
    public const string ClashStart = "ClashStart";

    // Clash = 参与拼点
    // 只要这张卡参与拼点，不管输赢，都会触发
    public const string Clash = "Clash";

    // ClashWin = 拼点胜利
    // 发生在 Resolved 生效之前
    // 注意：这时候普通卡通常还没有进入 CD
    public const string ClashWin = "ClashWin";

    // ClashLose = 拼点失败
    // 失败方触发
    // 可以用于失败补偿、失败惩罚、垃圾卡失败奖励等
    public const string ClashLose = "ClashLose";


    // ================================
    // 伤害阶段
    // ================================

    // DamageModifier = 伤害正式写入 HP 前的伤害修正阶段
    public const string DamageModifier = "DamageModifier";

    // Hit = 命中
    // 攻击打到目标时触发
    // 闪避成功不算命中
    public const string Hit = "Hit";

    // AfterDamage = 造成伤害后
    // 一般用于实际扣血后触发的效果
    public const string AfterDamage = "AfterDamage";

    // AfterKill = 击杀后
    // 用于击杀奖励、击杀后 CD 减少、负罪感变化等
    public const string AfterKill = "AfterKill";

    // ================================
    // 卡牌 / 行动结束阶段
    // ================================

    // CardResolved = 已 Used 的卡完成自身直接逻辑后的新结算时机
    public const string CardResolved = "CardResolved";

    // ActionFinished = Execution Action / Item 在程序层正式结束
    public const string ActionFinished = "ActionFinished";

    // ================================
    // Legacy 结算阶段
    // ================================

    // Resolved = Legacy / 兼容旧代码的卡牌结算时机
    public const string Resolved = "Resolved";

    // ================================
    // 回合结束
    // ================================

    // TurnEnd = 回合结束
    // 例如：Buff 持续时间减少、卡牌自然 CD -1
    public const string TurnEnd = "TurnEnd";
}
