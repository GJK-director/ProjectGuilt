// BattleCardState = 战斗中的卡牌状态
// CardTestData 是“卡牌模板”
// BattleCardState 是“这场战斗里某一张复制品的当前状态”
public class BattleCardState
{
    // instanceID = 这张卡牌复制品自己的唯一编号
    // 比如同样都是基础攻击，也要区分成 atk_001_0 / atk_001_1
    public string instanceID;

    // owner = 这张卡属于哪个角色
    public CharacterData owner;

    // cardData = 这张卡对应的卡牌模板
    public CardTestData cardData;

    // currentCooldown = 当前剩余 CD
    public int currentCooldown;

    // isConsumed = 是否已经被消耗
    // 主要给“能力型罪卡”使用
    public bool isConsumed;
    // maxUseCount = 本场战斗最大可使用次数
    public int maxUseCount;

    // currentUseCount = 当前已经使用 / 生效次数
    public int currentUseCount;

    public BattleCardState(CharacterData owner, CardTestData cardData, string instanceID)
    {
        this.owner = owner;
        this.cardData = cardData;
        this.instanceID = instanceID;

        currentCooldown = 0;
        isConsumed = false;

        currentUseCount = 0;

        if (cardData != null)
        {
            maxUseCount = cardData.maxUseCount;
        }
    }

    // GetCardName = 获取卡牌名称
    public string GetCardName()
    {
        if (cardData == null)
        {
            return "未知卡牌";
        }

        return cardData.cardName;
    }

    // IsSinCard = 是否罪卡
    public bool IsSinCard()
    {
        if (cardData == null)
        {
            return false;
        }

        return cardData.isSinCard;
    }

    // ConsumeOnUse = 是否使用后消耗
    public bool ConsumeOnUse()
    {
        if (cardData == null)
        {
            return false;
        }

        return cardData.consumeOnUse;
    }
    // GetSinCardUseRule = 获取罪卡使用规则
    public string GetSinCardUseRule()
    {
        if (cardData == null)
        {
            return SinCardUseRule.Permanent;
        }

        // 优先使用新字段
        if (!string.IsNullOrEmpty(cardData.sinCardUseRule))
        {
            return cardData.sinCardUseRule;
        }

        // 兼容旧字段 consumeOnUse
        // 旧数据里 consumeOnUse = true，就等同于 UseCount
        if (cardData.consumeOnUse)
        {
            return SinCardUseRule.UseCount;
        }

        // 默认永久型罪卡
        return SinCardUseRule.Permanent;
    }
    // GetSinCardCategory = 获取罪卡分类
    public string GetSinCardCategory()
    {
        if (cardData == null)
        {
            return SinCardCategory.Clash;
        }
        if (!string.IsNullOrEmpty(cardData.sinCardCategory))
        {
            return cardData.sinCardCategory;
        }
        // 默认当作拼点型罪卡
        return SinCardCategory.Clash;
    }
    // IsClashSinCard = 是否是拼点型罪卡
    public bool IsClashSinCard()
    {
        if (!IsSinCard())
        {
            return false;
        }
        return GetSinCardCategory() == SinCardCategory.Clash;
    }
    // IsAbilitySinCard = 是否是能力型罪卡
    public bool IsAbilitySinCard()
    {
        if (!IsSinCard())
        {
            return false;
        }
        return GetSinCardCategory() == SinCardCategory.Ability;
    }
    

    // IsUseCountSinCard = 是否是按次数消耗的罪卡
    public bool IsUseCountSinCard()
    {
        if (!IsSinCard())
        {
            return false;
        }

        return GetSinCardUseRule() == SinCardUseRule.UseCount;
    }

    // IsPermanentSinCard = 是否是永久型罪卡
    public bool IsPermanentSinCard()
    {
        if (!IsSinCard())
        {
            return false;
        }

        return GetSinCardUseRule() == SinCardUseRule.Permanent;
    }
}