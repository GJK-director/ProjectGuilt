// 脚本中文说明：状态数据。负责记录角色身上的一个 Buff 的 ID、名称、层数、持续时间和消失规则。
// BuffData = 状态数据
// 它只负责描述“挂在角色身上的一个状态”
//
// 注意：
// BuffData 不负责决定“哪张卡给了这个状态”
// 卡牌给状态，是战斗逻辑 / 卡牌效果系统负责的
using System;
using System.Collections.Generic;

public class BuffData
{
    // buffID = 状态ID
    // 用英文，方便代码判断
    // 例如：Strength / Weakness / Bullet / NextCardPointUp
    public string buffID;

    // buffName = 状态显示名
    // 可以用中文，方便以后 UI 显示
    // 例如：强壮 / 虚弱 / 子弹 / 下一张卡点数增加
    public string buffName;
    
    // buffCategory = 状态分类
    // UpBuff = 正面状态
    // Debuff = 负面状态
    // AbilityBuff = 能力状态 / 特殊状态
    public string buffCategory;

    // stack = 层数
    // 例如：强壮 x2，子弹 x6
    public int stack;

    // duration = 持续时间
    // 普通状态用这个
    // 常驻状态可以写 -1
    public int duration;

    // checkTiming = 检测阶段
    // TurnStart      回合开始
    // ClashStart     拼点开始
    // ClashEnd       拼点结束
    // AfterDamage    造成伤害后
    // AfterDamaged   受到伤害后
    // AfterKill      杀死敌人后
    // TurnEnd        回合结束
    // None           不自动检测
    public string checkTiming;

    // expireRule = 消失 / 变化规则
    // DurationDown      检测时持续时间 -1
    // StackDown         检测时层数 -1
    // ConsumeOnTrigger  检测时直接消耗掉
    // Permanent         常驻，不自动消失
    // Custom            特殊规则，暂时只记录，不自动处理
    public string expireRule;

    // 构造函数：创建一个完整的 Buff
    public BuffData(
        string id,
        string name,
        string category,
        int buffStack,
        int buffDuration,
        string timing,
        string rule
    )
    {
        buffID = id;
        buffName = name;
        buffCategory = category;
        stack = buffStack;
        duration = buffDuration;
        checkTiming = timing;
        expireRule = rule;
    }
}

[Serializable]
public class BuffDefinitionData
{
    public string buffID;
    public string buffName;
    public string buffCategory;
    public string effectType;
    public string targetStat;
    public float valuePerStack;
    public string defaultCheckTiming;
    public string defaultExpireRule;
    public string consumeRule;
    public string description;
}

[Serializable]
public class BuffDefinitionList
{
    public List<BuffDefinitionData> buffs;
}
