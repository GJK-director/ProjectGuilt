// 脚本中文说明：战斗时机常量。负责保存回合开始、拼点开始、卡牌生效、命中、伤害后等事件时机名称。
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

    // TurnEnd = 回合结束
    // 例如：Buff 持续时间减少、卡牌自然 CD -1
    public const string TurnEnd = "TurnEnd";


    // ================================
    // 卡牌使用阶段
    // ================================

    // BeforeUse = 卡牌使用前
    // 旧名字 OnPlay 等同于 BeforeUse
    // 以后新 JSON 统一写 BeforeUse
    public const string BeforeUse = "BeforeUse";

    // OnPlay = 旧版字段
    // 只用于兼容旧 JSON，不建议新卡继续使用
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
    // 卡牌结果阶段
    // ================================

    // Resolved = 卡牌生效
    // 普通卡牌在这里进入 CD
    // 如果要写“这张卡生效后减少自己的 CD”，优先用这个阶段
    public const string Resolved = "Resolved";

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
}