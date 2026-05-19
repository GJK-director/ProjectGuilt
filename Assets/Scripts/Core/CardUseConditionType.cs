// CardUseConditionType = 卡牌使用条件类型常量表
// JSON 里的 conditionType 字段，尽量对应这里的名字
public static class CardUseConditionType
{
    // None = 无条件
    public const string None = "None";

    // HpBelowPercent = HP 低于百分比
    // 例如 value = 50，表示 HP 低于 50%
    public const string HpBelowPercent = "HpBelowPercent";

    // HpAbovePercent = HP 高于百分比
    // 例如 value = 50，表示 HP 高于 50%
    public const string HpAbovePercent = "HpAbovePercent";

    // HasBuff = 拥有指定 Buff / 状态
    // 例如 buffType = "Bullet"，表示需要拥有子弹状态
    public const string HasBuff = "HasBuff";

    // BuffStackAtLeast = 指定 Buff 层数至少为多少
    // 例如 buffType = "Bullet"，value = 3，表示子弹层数至少 3
    public const string BuffStackAtLeast = "BuffStackAtLeast";

    // GuiltAtLeast = 负罪感至少为多少
    // 先预留，等 GuiltManager 做完后再正式接入
    public const string GuiltAtLeast = "GuiltAtLeast";

    // GuiltBelow = 负罪感低于多少
    // 先预留，等 GuiltManager 做完后再正式接入
    public const string GuiltBelow = "GuiltBelow";
}