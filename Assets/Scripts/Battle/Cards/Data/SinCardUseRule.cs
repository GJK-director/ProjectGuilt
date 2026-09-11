// 脚本中文说明：罪卡使用规则常量。负责保存罪卡按次数消耗或永久存在等规则名称。
// SinCardUseRule = 罪卡使用规则常量表
// 用来决定罪卡在结算后怎么处理
public static class SinCardUseRule
{
    // UseCount = 按使用次数消耗
    // 例如 maxUseCount = 3，使用 3 次后本场战斗不可再用
    public const string UseCount = "UseCount";

    // Permanent = 永久型罪卡
    // 本场战斗内不会因为使用次数消失，也不进入普通 CD
    public const string Permanent = "Permanent";
}