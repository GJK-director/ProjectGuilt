// 脚本中文说明：战斗事件上下文。负责保存一次战斗事件中的使用者、目标、卡牌、点数、伤害和结果信息。
// BattleEventContext = 战斗事件上下文
// 用来记录“这次战斗事件发生了什么”
// 后面 CD / Buff / 罪卡 / 负罪感 / 成就 / UI 都可以读取这里的信息
public class BattleEventContext
{
    // timing = 当前事件阶段。
    // 新时间点词汇包括：
    // TurnStart / ExecutionStart / ActionStart / CardUsed / ClashStart /
    // Clash / ClashWin / ClashLose / DamageModifier / Hit / AfterDamage /
    // AfterKill / CardResolved / ActionFinished / TurnEnd。
    // BeforeUse / OnPlay / Resolved 仍属于 Legacy / 兼容旧代码的时间点。
    public string timing;

    // user = 行动者
    // 例如：使用卡牌的人 / 造成伤害的人
    public CharacterData user;

    // target = 目标
    // 例如：被攻击的人 / 被造成伤害的人
    public CharacterData target;

    // cardData = 本次事件相关的卡牌模板
    public CardTestData cardData;

    // cardState = 本次事件相关的战斗卡牌状态
    // 现在我们还没正式接 BattleCardState，可以先预留
    public BattleCardState cardState;

    // impact = 本次事件对应的精确 BattleImpact（DamageModifier/Hit/AfterDamage/AfterKill）。
    public BattleImpact impact;

    // clashPoint = 本次拼点点数
    public int clashPoint;
    // clashResult = 拼点结果
    // None = 没有拼点结果
    // Win = 拼点胜利
    // Lose = 拼点失败
    public string clashResult;
    // damage = 当前时间点的伤害值。
    // DamageModifier/Hit 为写入HP前的候选伤害；AfterDamage/AfterKill为实际HP损失。
    public int damage;

    // isHit = 是否命中
    public bool isHit;

    // isKill = 是否击杀
    public bool isKill;

    // 构造函数：最简版本
    public BattleEventContext(string timing)
    {
        this.timing = timing;
    }

    // SetUserAndTarget = 设置行动者和目标
    public BattleEventContext SetUserAndTarget(CharacterData user, CharacterData target)
    {
        this.user = user;
        this.target = target;

        return this;
    }

    // SetCardData = 设置卡牌模板
    public BattleEventContext SetCardData(CardTestData cardData)
    {
        this.cardData = cardData;

        return this;
    }

    // SetCardState = 设置战斗卡牌状态
    public BattleEventContext SetCardState(BattleCardState cardState)
    {
        this.cardState = cardState;

        if (cardState != null)
        {
            this.cardData = cardState.cardData;
        }

        return this;
    }

    public BattleEventContext SetImpact(BattleImpact impact)
    {
        this.impact = impact;
        return this;
    }

    // SetClashPoint = 设置拼点值
    public BattleEventContext SetClashPoint(int clashPoint)
    {
        this.clashPoint = clashPoint;

        return this;
    }
    // SetClashResult = 设置拼点结果
    public BattleEventContext SetClashResult(string clashResult)
    {
        this.clashResult = clashResult;

        return this;
    }

    // SetDamage = 设置伤害值
    public BattleEventContext SetDamage(int damage)
    {
        this.damage = damage;

        return this;
    }

    // SetHit = 设置是否命中
    public BattleEventContext SetHit(bool isHit)
    {
        this.isHit = isHit;

        return this;
    }

    // SetKill = 设置是否击杀
    public BattleEventContext SetKill(bool isKill)
    {
        this.isKill = isKill;

        return this;
    }

    // GetCardName = 获取卡牌名称，方便打印日志
    public string GetCardName()
    {
        if (cardData == null)
        {
            return "无卡牌";
        }

        return cardData.cardName;
    }

    // GetUserName = 获取行动者名称
    public string GetUserName()
    {
        if (user == null)
        {
            return "无行动者";
        }

        return user.characterName;
    }

    // GetTargetName = 获取目标名称
    public string GetTargetName()
    {
        if (target == null)
        {
            return "无目标";
        }

        return target.characterName;
    }
}
