// 脚本中文说明：Buff 分类常量。负责保存增益、减益、能力状态等 Buff 分类名称。
// BuffCategory = Buff / 状态分类常量表
// JSON 里的 buffCategory 字段，尽量对应这里的名字
public static class BuffCategory
{
    // UpBuff = 正面状态
    public const string UpBuff = "UpBuff";

    // DownBuff = 负面状态
    public const string DownBuff = "DownBuff";
    public const string Debuff = "Debuff";

    // AbilityBuff = 能力状态 / 资源状态
    // 例如：子弹、弹药、护盾层数、特殊计数等
    public const string AbilityBuff = "AbilityBuff";
}
