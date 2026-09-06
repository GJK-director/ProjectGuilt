// 脚本中文说明：卡牌类型常量。负责保存攻击、防御、闪避等卡牌类型名称。
// CardType = 卡牌类型常量表
// JSON 里的 cardType 字段，尽量对应这里的名字
public static class CardType
{
    // Attack = 攻击卡
    public const string Attack = "Attack";

    // Defense = 防御卡
    public const string Defense = "Defense";

    // Dodge = 闪避卡
    public const string Dodge = "Dodge";

    // Ability = 能力卡
    public const string Ability = "Ability";
}

// BattleResourceID = 由 CharacterData.buffs 承载的稳定战斗资源标识。
public static class BattleResourceID
{
    public const string Bullet = "Bullet";
    public const string Anger = "Anger";
    public const string Modification = "Modification";
    public const string Conservation = "Conservation";
}

// AttackDeliveryMode = Attack 卡的空间 / 演出兑现方式常量表。
// 它与 CardType 正交，不改变攻击、防御、闪避的规则分类。
public static class AttackDeliveryMode
{
    public const string Melee = "Melee";
    public const string LongRangeShoot = "LongRangeShoot";
    public const string CloseRangeShoot = "CloseRangeShoot";

    public static bool IsKnownSerializedValue(string value)
    {
        return string.IsNullOrEmpty(value) ||
            value == Melee ||
            value == LongRangeShoot ||
            value == CloseRangeShoot;
    }

    public static string ResolveOrDefault(string value)
    {
        return string.IsNullOrEmpty(value) ? Melee : value;
    }
}
