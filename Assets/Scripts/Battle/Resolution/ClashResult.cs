// 脚本中文说明：拼点结果常量。负责保存无结果、任意结果、胜利、失败等拼点结果名称。
// ClashResult = 拼点结果常量表
// JSON 里的 requireClashResult 字段，尽量对应这里的名字
public static class ClashResult
{
    // None = 没有拼点结果
    // 例如：防御生效、非拼点生效
    public const string None = "None";

    // Any = 不限制拼点结果
    public const string Any = "Any";

    // Win = 拼点胜利
    public const string Win = "Win";

    // Lose = 拼点失败
    public const string Lose = "Lose";
}