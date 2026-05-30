// 脚本中文说明：冷却目标类型常量。负责保存减少 CD 时作用于全部卡、某类型卡、某品质卡或某 ID 卡的范围名称。
// CooldownTargetType = 减少冷却目标范围常量表
// JSON 里的 cooldownTarget 字段，尽量对应这里的名字
public static class CooldownTargetType
{
    // All = 全部卡牌
    public const string All = "All";

    // CardType = 指定卡牌类型
    // 例如 Attack / Defense / Dodge
    public const string CardType = "CardType";

    // Rarity = 指定品质
    // 例如 White / Green / Blue / Purple / Gold
    public const string Rarity = "Rarity";

    // CardID = 指定卡牌 ID
    // 例如 atk_001
    public const string CardID = "CardID";
}