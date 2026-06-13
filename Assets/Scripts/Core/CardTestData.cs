// 脚本中文说明：卡牌测试数据。负责承接 CardsTest.json 里读取出来的一张卡牌模板数据。
using System.Collections.Generic;

// CardResourceRuleData = 卡牌软资源规则
// 软资源规则不会阻止卡牌安排或执行。
// 资源不足时选择降级基础点数，而不是返回ActionUnavailable。
// 硬性使用条件仍由useConditions系统处理。
public class CardResourceRuleData
{
    public string resourceType;
    public string resourceID;
    public int requiredStackForNormalVersion;
    public int fallbackMinPoint;
    public int fallbackMaxPoint;
    public int pointPerStack;
    public int exactStackForBonus;
    public int exactStackPointBonus;
    public int consumeAmountOnSuccess;
}

// CardTestData = 卡牌测试数据
// 用来接收 JSON 里的单张卡牌数据
public class CardTestData
{
    public string cardID;       // 卡牌ID
    public string cardName;     // 卡牌名称
    public string rarity;       // 稀有度
    public string cardType;     // 卡牌类型
    public bool isSinCard;      // 是否罪卡
    public bool consumeOnUse;   // 是否使用后消耗
    public CardUseConditionData[] useConditions;
    public CardResourceRuleData resourceRule;
    public CardResourceRuleData[] resourceRules;
    // sinCardCategory = 罪卡分类
    // Clash：拼点型罪卡
    // Ability：能力型罪卡
    public string sinCardCategory;
    // maxUseCount = 本场战斗最大可生效次数
    // 主要给消耗型罪卡使用
    // 0 或小于 0 表示不限制次数
    public int maxUseCount;
    public bool isClashable;    // 是否可拼点
                                // sinCardUseRule = 罪卡使用规则
                                // UseCount：按次数消耗
                                // Permanent：本场战斗内不因使用次数消失
    public string sinCardUseRule;
    public string damageFormula;  // 伤害公式
    public string defenseFormula; // 防御公式

    public int minPoint;        // 最小点数
    public int maxPoint;        // 最大点数

    public int speedModifier;   // 速度修正
    public int cooldown;        // 冷却
    public int guiltCost;       // 负罪感消耗
                                // guiltGain = 使用罪卡后增加的负罪感
                                // 注意：这不是消耗，而是累计增加
    public int guiltGain;

    public List<CardEffectData> effects; // 卡牌效果列表
}
