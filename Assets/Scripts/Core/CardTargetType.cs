// CardTargetType = 卡牌效果目标类型常量表
// JSON 里的 target 字段，尽量对应这里的名字
public static class CardTargetType
{
    // Self = 自己
    public const string Self = "Self";

    // Target = 当前目标
    public const string Target = "Target";

    // AllAlly = 全体友方
    // 先预留，后面群体效果再实现
    public const string AllAlly = "AllAlly";

    // AllEnemy = 全体敌方
    // 先预留，后面群体效果再实现
    public const string AllEnemy = "AllEnemy";
}