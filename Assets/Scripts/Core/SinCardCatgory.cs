// SinCardCategory = 罪卡分类常量表
// 用来区分罪卡的大类型
public static class SinCardCategory
{
    // Clash = 拼点型罪卡
    // 这类罪卡会参与拼点，逻辑类似普通攻击/防御/闪避卡
    public const string Clash = "Clash";

    // Ability = 能力型罪卡
    // 这类罪卡不一定参与拼点，主要通过效果影响属性、Buff、回合、伤害等
    public const string Ability = "Ability";
}