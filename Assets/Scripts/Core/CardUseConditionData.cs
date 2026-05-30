// 脚本中文说明：卡牌使用条件数据。负责记录 JSON 中一条卡牌使用条件的类型、目标、数值和 Buff 要求。
using System;

// CardUseConditionData = 卡牌使用条件数据
// 用于描述“这张卡在什么条件下才能使用”
[Serializable]
public class CardUseConditionData
{
    // conditionType = 条件类型
    // 例如 HpBelowPercent / HasBuff / BuffStackAtLeast
    public string conditionType;

    // target = 检查目标
    // 例如 Self / Target
    // 先复用 CardTargetType 里的字符串
    public string target;

    // value = 条件数值
    // 例如 HP 百分比、Buff 层数、负罪感数值
    public int value;

    // buffType = Buff ID / 状态 ID
    // 例如 Bullet / Strength / DamageUp
    public string buffType;
}