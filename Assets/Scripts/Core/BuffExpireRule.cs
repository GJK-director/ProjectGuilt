// 脚本中文说明：Buff 消失规则常量。负责保存持续时间减少、层数减少、触发后消耗、常驻等规则名称。
// BuffExpireRule = Buff / 状态消失规则常量表
// JSON 里的 expireRule 字段，尽量对应这里的名字
public static class BuffExpireRule
{
    // DurationDown = 持续时间递减
    // 例如：每到 checkTiming 检测阶段，duration -1，归零后消失
    public const string DurationDown = "DurationDown";

    // Permanent = 永久存在
    // 例如：子弹、特殊资源、长期能力状态
    public const string Permanent = "Permanent";
}