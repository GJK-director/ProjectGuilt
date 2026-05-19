// PendingBuffData = 待生效状态数据
// 用来记录“未来某个回合才会真正添加到角色身上的 Buff”
public class PendingBuffData
{
    // buffID = 状态ID
    // 例如：Strength / Weakness / SpeedUp
    public string buffID;

    // buffName = 状态中文名
    // 例如：强壮 / 虚弱 / 速度上升
    public string buffName;

    // buffCategory = 状态分类
    // UpBuff / Debuff / AbilityBuff
    public string buffCategory;

    // stack = 层数
    public int stack;

    // duration = 这个 Buff 正式生效后的持续时间
    public int duration;

    // checkTiming = 正式生效后，在什么阶段检测
    // 例如 TurnEnd / ClashStart / None
    public string checkTiming;

    // expireRule = 正式生效后的消失规则
    // 例如 DurationDown / ConsumeOnTrigger / Permanent
    public string expireRule;

    // delayTurns = 还要延迟几个回合才生效
    // 1 = 下回合开始生效
    // 2 = 两个回合后生效
    public int delayTurns;

    // applyTimes = 总共生效几次
    // 1 = 只生效一次
    // 2 = 连续两个未来回合各生效一次
    public int applyTimes;

    // intervalTurns = 每次生效之间间隔几个回合
    // 一般写 1，表示每回合都检查一次
    public int intervalTurns;

    public PendingBuffData(
        string id,
        string name,
        string category,
        int buffStack,
        int buffDuration,
        string timing,
        string rule,
        int delay,
        int times,
        int interval
    )
    {
        buffID = id;
        buffName = name;
        buffCategory = category;
        stack = buffStack;
        duration = buffDuration;
        checkTiming = timing;
        expireRule = rule;

        delayTurns = delay;
        applyTimes = times;
        intervalTurns = interval;
    }
}