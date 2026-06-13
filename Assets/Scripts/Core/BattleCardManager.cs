// 脚本中文说明：战斗卡牌管理器。负责创建战斗卡牌状态、判断卡牌能否使用、处理 CD、使用次数和消耗。
using UnityEngine;

public enum CardEligibilityFailureReason
{
    None,
    InvalidActor,
    ActorDead,
    InvalidCardState,
    InvalidCardData,
    CardOnCooldown,
    CardConsumed,
    UseCountUnavailable,
    GuiltRequirementNotMet,
    BuffStackRequirementNotMet,
    UnsupportedCondition,
    InvalidSlot,
    SlotOccupied,
    CardAlreadyAssigned,
    InvalidEnemyIntent
}

public sealed class CardEligibilityResult
{
    public bool isEligible;
    public CardEligibilityFailureReason failureReason;
    public string failureMessage;
    public string failedConditionType;
    public string buffID;
    public int requiredValue;
    public int currentValue;

    public static CardEligibilityResult Success()
    {
        return new CardEligibilityResult
        {
            isEligible = true,
            failureReason = CardEligibilityFailureReason.None,
            failureMessage = ""
        };
    }

    public static CardEligibilityResult Failure(
        CardEligibilityFailureReason failureReason,
        string failureMessage,
        string failedConditionType = "",
        string buffID = "",
        int requiredValue = 0,
        int currentValue = 0
    )
    {
        return new CardEligibilityResult
        {
            isEligible = false,
            failureReason = failureReason,
            failureMessage = failureMessage,
            failedConditionType = failedConditionType,
            buffID = buffID,
            requiredValue = requiredValue,
            currentValue = currentValue
        };
    }
}

// BattleCardManager = 战斗卡牌管理器
// 负责处理战斗中卡牌的 CD、消耗、可用性判断
public static class BattleCardManager
{
    // HandleEvent = 处理战斗事件
    // 以后 BattleEventProcessor 会把事件传进来
    // context = 战斗事件上下文，里面有事件时机、使用者、目标和卡牌状态。
    public static void HandleEvent(BattleEventContext context)
    {
        if (context == null)
        {
            return;
        }

        // 生效阶段：
        // 普通卡进入 CD
        // 罪卡按 UseCount / Permanent 规则处理使用次数或负罪感
        if (context.timing == BattleTiming.Resolved)
        {
            ApplyCooldownOnResolved(context.cardState);
            return;
        }

        // 回合结束：
        // 当前角色所有卡牌 CD -1
        // context.user = 当前正在处理回合结束的角色。
        if (context.timing == BattleTiming.TurnEnd)
        {
            ReduceCooldownsAtTurnEnd(context.user);
            return;
        }

        // 下面这些阶段先预留窗口
        // 后面可以处理：
        // 命中后某些卡 CD -1
        // 造成伤害后全部卡 CD -X
        // 击杀后全部卡 CD -5
        if (context.timing == BattleTiming.Hit)
        {
            // 暂时不处理
            return;
        }

        if (context.timing == BattleTiming.AfterDamage)
        {
            // 暂时不处理
            // 后面由卡牌效果 / Buff / 罪卡能力来决定是否减少 CD
            return;
        }

        if (context.timing == BattleTiming.AfterKill)
        {
            // 暂时不处理
            return;
        }
    }

    // CreateBattleCard = 创建一张战斗中的卡牌状态
    // Create = 创建，BattleCard = 战斗卡牌。
    // owner = 持有这张卡的角色。
    // cardData = 卡牌模板数据。
    // instanceID = 本场战斗中这张卡牌实例的唯一编号。
    public static BattleCardState CreateBattleCard(
        CharacterData owner,
        CardTestData cardData,
        string instanceID
    )
    {
        // BattleCardState = 战斗卡牌状态。
        // 同一张卡牌模板可以创建多个不同实例。
        BattleCardState cardState = new BattleCardState(owner, cardData, instanceID);

        if (owner != null)
        {
            if (owner.battleCards == null)
            {
                // 如果角色还没有战斗卡牌列表，就先创建一个。
                owner.battleCards = new System.Collections.Generic.List<BattleCardState>();
            }

            // 把新卡牌状态加入角色的战斗卡牌列表。
            owner.battleCards.Add(cardState);
        }

        if (BattleDebugSettings.ShowDetailBattleLog)
        {
            Debug.Log(
                "创建战斗卡牌：" +
                cardState.GetCardName() +
                " / 实例ID：" +
                instanceID +
                " / 持有者：" +
                GetCharacterName(owner)
            );
        }

        return cardState;
    }

    // CanUseCard = 判断这张卡当前能不能使用
    // Can = 可以，Use = 使用，Card = 卡牌。
    // 这个版本只检查卡牌状态本身：是否为空、是否已消耗、是否还在 CD。
    // 注意：该重载不包含角色、死亡、useConditions 等完整资格检查。
    public static bool CanUseCard(BattleCardState cardState)
    {
        CardEligibilityResult result = EvaluateCardStateOnly(cardState);

        if (!result.isEligible)
        {
            Debug.Log(result.failureMessage);
            return false;
        }

        return true;
    }

    // EvaluateCardEligibility = 统一卡牌资格检查。
    // 卡牌资格检查必须是纯读取。
    // 准备阶段安排与执行阶段复检共享同一套 useConditions 解释，
    // 但准备阶段不得触发事件、消耗 Buff 或修改卡牌状态。
    public static CardEligibilityResult EvaluateCardEligibility(
        CharacterData user,
        CharacterData target,
        BattleCardState cardState
    )
    {
        if (user == null)
        {
            return CardEligibilityResult.Failure(
                CardEligibilityFailureReason.InvalidActor,
                "卡牌不能使用：行动者为空"
            );
        }

        if (user.IsDead())
        {
            return CardEligibilityResult.Failure(
                CardEligibilityFailureReason.ActorDead,
                user.characterName + " 已死亡，不能安排或使用卡牌"
            );
        }

        CardEligibilityResult baseResult = EvaluateCardStateOnly(cardState);

        if (!baseResult.isEligible)
        {
            return baseResult;
        }

        return SinCardConditionChecker.EvaluateUseConditions(user, target, cardState.cardData);
    }

    // CanUseCard = 判断这张卡当前能不能使用
    // 这个版本会额外检查角色、死亡状态和通用 useConditions。
    // user = 使用者，target = 目标，cardState = 要检查的卡牌状态。
    public static bool CanUseCard(
        CharacterData user,
        CharacterData target,
        BattleCardState cardState
    )
    {
        CardEligibilityResult result = EvaluateCardEligibility(user, target, cardState);

        if (!result.isEligible)
        {
            Debug.Log(GetCardNameForLog(cardState) + " 不能使用，原因：" + result.failureMessage);
            return false;
        }

        return true;
    }

    static CardEligibilityResult EvaluateCardStateOnly(BattleCardState cardState)
    {
        if (cardState == null)
        {
            return CardEligibilityResult.Failure(
                CardEligibilityFailureReason.InvalidCardState,
                "卡牌不能使用：卡牌状态为空"
            );
        }

        if (cardState.cardData == null)
        {
            return CardEligibilityResult.Failure(
                CardEligibilityFailureReason.InvalidCardData,
                "卡牌不能使用：cardData 为空。实例ID：" + cardState.instanceID
            );
        }

        if (cardState.isConsumed)
        {
            return CardEligibilityResult.Failure(
                CardEligibilityFailureReason.CardConsumed,
                cardState.GetCardName() + " 已经被消耗，本场战斗不能再次使用"
            );
        }

        if (cardState.currentCooldown > 0)
        {
            return CardEligibilityResult.Failure(
                CardEligibilityFailureReason.CardOnCooldown,
                cardState.GetCardName() + " 还在冷却中，剩余 CD：" + cardState.currentCooldown,
                "",
                "",
                0,
                cardState.currentCooldown
            );
        }

        return CardEligibilityResult.Success();
    }

    // ApplyCooldownOnResolved = 卡牌生效后处理 CD / 消耗
    // Apply = 应用，Cooldown = 冷却，Resolved = 卡牌已经生效。
    // cardState = 已经成功生效的卡牌状态。
    public static void ApplyCooldownOnResolved(BattleCardState cardState)
    {
        if (cardState == null)
        {
            return;
        }

        // 罪卡默认不进入普通 CD
        if (cardState.IsSinCard())
        {
            // 罪卡成功结算后，增加负罪感
            // guiltGain = 使用这张罪卡后增加的负罪感。
            if (cardState.cardData != null && cardState.cardData.guiltGain > 0)
            {
                GuiltManager.AddGuilt(
                    cardState.owner,
                    cardState.cardData.guiltGain,
                    "使用罪卡：" + cardState.GetCardName()
                );
            }

            if (cardState.IsUseCountSinCard())
            {
                // UseCount 型罪卡：成功生效后增加使用次数。
                // 达到上限后会标记 isConsumed。
                ConsumeCardUse(cardState);
            }
            else
            {
                // Permanent 型罪卡：不进入普通 CD，也不按次数消耗。
                if (BattleDebugSettings.ShowDetailBattleLog)
                {
                    Debug.Log(cardState.GetCardName() + " 是罪卡，不进入普通 CD");
                }
            }

            // 罪卡处理完后直接结束，不继续进入普通 CD 逻辑
            return;
        }

        // 普通卡：根据卡牌数据或品质计算基础 CD。
        int cooldown = GetBaseCooldown(cardState.cardData);

        if (cooldown < 0)
        {
            cooldown = 0;
        }

        cardState.currentCooldown = cooldown;

        if (BattleDebugSettings.ShowDetailBattleLog)
        {
            Debug.Log(
                cardState.GetCardName() +
                " 生效，进入 CD：" +
                cardState.currentCooldown
            );
        }
    }

    // ReduceCooldownsAtTurnEnd = 回合结束时减少某个角色所有卡牌的 CD
    // owner = 要处理卡牌 CD 的角色。
    public static void ReduceCooldownsAtTurnEnd(CharacterData owner)
    {
        if (owner == null)
        {
            return;
        }

        if (owner.battleCards == null || owner.battleCards.Count == 0)
        {
            return;
        }

        foreach (BattleCardState cardState in owner.battleCards)
        {
            if (cardState == null)
            {
                continue;
            }

            // 每回合结束默认让每张卡 CD -1。
            ReduceCardCooldown(cardState, 1);
        }
    }

    // ConsumeCardUse = 消耗一次卡牌使用次数
    // cardState = 要增加使用次数的卡牌状态。
    public static void ConsumeCardUse(BattleCardState cardState)
    {
        if (cardState == null)
        {
            return;
        }

        if (cardState.isConsumed)
        {
            // 已经消耗的卡，不再重复增加使用次数。
            return;
        }

        // currentUseCount = 当前已经成功使用 / 生效次数。
        cardState.currentUseCount++;

        int maxUseCount = cardState.maxUseCount;

        // 如果没有填写 maxUseCount，但 consumeOnUse = true
        // 默认按一次性消耗处理
        if (maxUseCount <= 0)
        {
            maxUseCount = 1;
        }

        if (BattleDebugSettings.ShowDetailBattleLog)
        {
            Debug.Log(
                cardState.GetCardName() +
                " 消耗次数：" +
                cardState.currentUseCount +
                " / " +
                maxUseCount
            );
        }

        if (cardState.currentUseCount >= maxUseCount)
        {
            cardState.isConsumed = true;

            Debug.Log(cardState.GetCardName() + " 已达到最大使用次数，本场战斗不能再次使用");
        }
    }

    // ReduceCardCooldown = 减少单张卡的 CD
    // cardState = 要减少 CD 的卡牌。
    // amount = 要减少的数量。
    public static void ReduceCardCooldown(BattleCardState cardState, int amount)
    {
        if (cardState == null)
        {
            return;
        }

        if (amount <= 0)
        {
            // amount <= 0 没有减少意义。
            return;
        }

        if (cardState.currentCooldown <= 0)
        {
            // 当前没有 CD 时，不需要处理。
            return;
        }

        int oldCooldown = cardState.currentCooldown;

        cardState.currentCooldown -= amount;

        if (cardState.currentCooldown < 0)
        {
            cardState.currentCooldown = 0;
        }

        if (BattleDebugSettings.ShowDetailBattleLog)
        {
            Debug.Log(
                cardState.GetCardName() +
                " CD 减少：" +
                oldCooldown +
                " → " +
                cardState.currentCooldown
            );
        }
    }

    // ReduceAllCooldowns = 减少某个角色全部卡牌 CD
    // 后面“击杀后全部卡 CD -5”可以用这个
    // owner = 要减少 CD 的角色。
    // amount = 要减少的 CD 数量。
    public static void ReduceAllCooldowns(CharacterData owner, int amount)
    {
        if (owner == null)
        {
            return;
        }

        if (owner.battleCards == null || owner.battleCards.Count == 0)
        {
            return;
        }

        foreach (BattleCardState cardState in owner.battleCards)
        {
            ReduceCardCooldown(cardState, amount);
        }
    }

    // ReduceCooldownsByCardType = 按卡牌类型减少 CD
    // 例如：Attack / Defense / Dodge
    // owner = 要处理的角色。
    // cardType = 卡牌类型。
    // amount = 要减少的 CD 数量。
    public static void ReduceCooldownsByCardType(CharacterData owner, string cardType, int amount)
    {
        if (owner == null)
        {
            return;
        }

        if (owner.battleCards == null || owner.battleCards.Count == 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(cardType))
        {
            return;
        }

        foreach (BattleCardState cardState in owner.battleCards)
        {
            if (cardState == null || cardState.cardData == null)
            {
                continue;
            }

            if (cardState.cardData.cardType == cardType)
            {
                // 只处理指定类型的卡牌。
                ReduceCardCooldown(cardState, amount);
            }
        }
    }

    // ReduceCooldownsByRarity = 按品质减少 CD
    // 例如：White / Green / Blue / Purple / Gold
    // rarity = 卡牌品质。
    // amount = 要减少的 CD 数量。
    public static void ReduceCooldownsByRarity(CharacterData owner, string rarity, int amount)
    {
        if (owner == null)
        {
            return;
        }

        if (owner.battleCards == null || owner.battleCards.Count == 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(rarity))
        {
            return;
        }

        foreach (BattleCardState cardState in owner.battleCards)
        {
            if (cardState == null || cardState.cardData == null)
            {
                continue;
            }

            if (cardState.cardData.rarity == rarity)
            {
                // 只处理指定品质的卡牌。
                ReduceCardCooldown(cardState, amount);
            }
        }
    }

    // ReduceCooldownsByCardID = 按卡牌 ID 减少 CD
    // 例如：只减少 cardID 为 atk_001 的所有复制品
    // cardID = 卡牌模板 ID。
    // amount = 要减少的 CD 数量。
    public static void ReduceCooldownsByCardID(CharacterData owner, string cardID, int amount)
    {
        if (owner == null)
        {
            return;
        }

        if (owner.battleCards == null || owner.battleCards.Count == 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(cardID))
        {
            return;
        }

        foreach (BattleCardState cardState in owner.battleCards)
        {
            if (cardState == null || cardState.cardData == null)
            {
                continue;
            }

            if (cardState.cardData.cardID == cardID)
            {
                // 同一个 cardID 可能有多个复制品，这里会全部减少。
                ReduceCardCooldown(cardState, amount);
            }
        }
    }
    // GetBaseCooldown = 获取卡牌基础 CD
    // 优先使用 cardData.cooldown > 0 的手动 CD
    // 如果没有手动 CD，就按品质自动计算
    public static int GetBaseCooldown(CardTestData cardData)
    {
        if (cardData == null)
        {
            return 0;
        }

        // 罪卡默认无普通 CD
        // 罪卡的使用限制由 UseCount / Permanent 等规则处理。
        if (cardData.isSinCard)
        {
            return 0;
        }

        // 如果 JSON 里手动填了大于 0 的 cooldown，就优先使用
        if (cardData.cooldown > 0)
        {
            return cardData.cooldown;
        }

        return GetCooldownByRarity(cardData.rarity);
    }

    // GetCooldownByRarity = 根据品质获取 CD
    // 暂定：白卡 0，每高一个品质 +2
    // rarity = 卡牌品质字符串。
    public static int GetCooldownByRarity(string rarity)
    {
        if (string.IsNullOrEmpty(rarity))
        {
            return 0;
        }

        switch (rarity)
        {
            case "White":
                return 0;

            case "Green":
                return 2;

            case "Blue":
                return 4;

            case "Purple":
                return 6;

            case "Gold":
                return 8;

            case "Sin":
                return 0;

            default:
                Debug.LogWarning("未知卡牌品质：" + rarity + "，默认 CD 为 0");
                return 0;
        }
    }

    // PrintCardStates = 打印某个角色所有卡牌状态
    // 后面调试 CD 时可以用
    // owner = 要打印卡牌状态的角色。
    public static void PrintCardStates(CharacterData owner)
    {
        if (owner == null)
        {
            return;
        }

        Debug.Log("===== " + owner.characterName + " 的战斗卡牌状态 =====");

        if (owner.battleCards == null || owner.battleCards.Count == 0)
        {
            Debug.Log(owner.characterName + " 当前没有战斗卡牌状态");
            return;
        }

        foreach (BattleCardState cardState in owner.battleCards)
        {
            if (cardState == null)
            {
                continue;
            }

            // sinInfoText = 罪卡额外信息文本。
            // 只有罪卡才打印罪卡规则和罪卡分类。
            string sinInfoText = "";
            if (cardState.IsSinCard())
            {
                sinInfoText =
                    " / 罪卡规则：" + cardState.GetSinCardUseRule() +
                    " / 罪卡分类：" + cardState.GetSinCardCategory();
            }
            Debug.Log(
                cardState.GetCardName() +
                " / 实例ID：" + cardState.instanceID +
                " / CD：" + cardState.currentCooldown +
                " / 使用次数：" + cardState.currentUseCount +
                " / " + cardState.maxUseCount +
                " / 已消耗：" + cardState.isConsumed +
                sinInfoText
            );

        }
    }

    static string GetCharacterName(CharacterData character)
    {
        // character 为空时返回“无角色”，避免打印日志时报错。
        if (character == null)
        {
            return "无角色";
        }

        return character.characterName;
    }

    static string GetCardNameForLog(BattleCardState cardState)
    {
        if (cardState == null)
        {
            return "卡牌";
        }

        return cardState.GetCardName();
    }
}
