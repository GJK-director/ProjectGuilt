// 脚本中文说明：卡牌效果类型常量。负责保存添加 Buff、减少 CD 等效果类型名称。
// CardEffectType = 卡牌效果类型常量表
// JSON 里的 effectType 字段，尽量对应这里的名字
public static class CardEffectType
{
    // ApplyBuff = 添加 Buff / 状态
    public const string ApplyBuff = "ApplyBuff";

    // ReduceCooldown = 减少卡牌冷却
    public const string ReduceCooldown = "ReduceCooldown";

    // EnableAngerMechanic = 开启本场战斗的愤怒机制
    public const string EnableAngerMechanic = "EnableAngerMechanic";

    // ActivateModification = 开启改装状态并压缩弹仓
    public const string ActivateModification = "ActivateModification";

    // ActivateConservation = 激活本回合节约状态并准备下一张射击强化。
    public const string ActivateConservation = "ActivateConservation";
}
