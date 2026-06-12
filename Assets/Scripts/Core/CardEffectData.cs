// 脚本中文说明：卡牌效果数据。负责承接 JSON 中一条卡牌效果的触发时机、目标、Buff、CD 等参数。
// CardEffectData = 卡牌效果数据
// 用来描述一张卡牌拥有什么特殊效果
public class CardEffectData
{
    public string trigger;       // 触发时机，例如 OnPlay / AfterDamage
    public string effectType;    // 效果类型，例如 ApplyBuff
    public string target;        // 效果目标，例如 Self / Enemy

    // requireClashResult = 要求的拼点结果
    // 空 / Any = 不限制
    // Win = 只有拼点胜利时触发
    // Lose = 只有拼点失败时触发
    // None = 只有非拼点生效时触发
    public string requireClashResult;
    // buffName / buffCategory / checkTiming / expireRule are legacy compatibility fields.
    // New ApplyBuff JSON should only need buffType, stack, duration and schedule fields.
    public string buffType;      // 状态ID，例如 Strength
    public string buffName;      // 状态中文名，例如 强壮
    public string buffCategory;  // 状态分类：UpBuff / Debuff / AbilityBuff

    public int value;            // 通用数值
    public int stack;            // 层数
    public int duration;         // 持续时间

    public string checkTiming;   // Buff 检测阶段
    public string expireRule;    // Buff 消失规则

    public string applyTiming;   // 生效方式：Immediate / Delayed
    public int delayTurns;       // 延迟几个回合后生效
    public int applyTimes;       // 总共生效几次
    public int intervalTurns;    // 每次生效之间间隔几个回合
                                 // ================================
                                 // ReduceCooldown 减少冷却专用字段
                                 // ================================

    // cooldownTarget = 减少冷却的目标范围
    // All = 全部卡
    // CardType = 指定卡牌类型
    // Rarity = 指定品质
    // CardID = 指定卡牌ID
    public string cooldownTarget;

    // cooldownAmount = 减少多少 CD
    public int cooldownAmount;

    // targetCardType = 指定卡牌类型
    // 例如 Attack / Defense / Dodge
    public string targetCardType;

    // targetRarity = 指定品质
    // 例如 White / Green / Blue / Purple / Gold
    public string targetRarity;

    // targetCardID = 指定卡牌ID
    // 例如 atk_001
    public string targetCardID;
}
