// BuffApplyTiming = Buff / 状态生效时机常量表
// JSON 里的 applyTiming 字段，尽量对应这里的名字
public static class BuffApplyTiming
{
    // Immediate = 立即生效
    // 例如：使用卡牌后马上获得强壮
    public const string Immediate = "Immediate";

    // Delayed = 延迟生效
    // 例如：下一回合开始时获得伤害提升
    public const string Delayed = "Delayed";
}