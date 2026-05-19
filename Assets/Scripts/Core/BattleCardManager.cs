using UnityEngine;

// BattleCardManager = 战斗卡牌管理器
// 负责处理战斗中卡牌的 CD、消耗、可用性判断
public static class BattleCardManager
{
    // HandleEvent = 处理战斗事件
    // 以后 BattleEventProcessor 会把事件传进来
    public static void HandleEvent(BattleEventContext context)
    {
        if (context == null)
        {
            return;
        }

        // 生效阶段：
        // 普通卡进入 CD
        // 罪卡如果 consumeOnUse，则标记为已消耗
        if (context.timing == BattleTiming.Resolved)
        {
            ApplyCooldownOnResolved(context.cardState);
            return;
        }

        // 回合结束：
        // 当前角色所有卡牌 CD -1
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
    // CreateBattleCard = 创建一张战斗中的卡牌状态
    public static BattleCardState CreateBattleCard(
        CharacterData owner,
        CardTestData cardData,
        string instanceID
    )
    {
        BattleCardState cardState = new BattleCardState(owner, cardData, instanceID);

        if (owner != null)
        {
            if (owner.battleCards == null)
            {
                owner.battleCards = new System.Collections.Generic.List<BattleCardState>();
            }

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
    // CanUseCard = 判断这张卡当前能不能使用
    public static bool CanUseCard(BattleCardState cardState)
    {
        if (cardState == null)
        {
            Debug.LogWarning("卡牌不能使用：卡牌状态为空");
            return false;
        }

        if (cardState.cardData == null)
        {
            Debug.LogWarning("卡牌不能使用：cardData 为空。实例ID：" + cardState.instanceID);
            return false;
        }

        if (cardState.isConsumed)
        {
            Debug.Log(
                cardState.GetCardName() +
                " 已经被消耗，本场战斗不能再次使用"
            );

            return false;
        }

        if (cardState.currentCooldown > 0)
        {
            Debug.Log(
                cardState.GetCardName() +
                " 还在冷却中，剩余 CD：" +
                cardState.currentCooldown
            );

            return false;
        }

        return true;
    }
    // ApplyCooldownOnResolved = 卡牌生效后处理 CD / 消耗
    // CanUseCard = 判断这张卡当前能不能使用
    // 这个版本会额外检查罪卡使用条件
    public static bool CanUseCard(
        CharacterData user,
        CharacterData target,
        BattleCardState cardState
    )
    {
        // 先走原本的 CD / 消耗检查
        if (!CanUseCard(cardState))
        {
            return false;
        }

        if (cardState == null || cardState.cardData == null)
        {
            return false;
        }

        // 如果是罪卡，再检查罪卡使用条件
        if (cardState.cardData.isSinCard)
        {
            string failReason;

            bool canUseSinCard = SinCardConditionChecker.CanUseSinCard(
                user,
                target,
                cardState.cardData,
                out failReason
            );

            if (!canUseSinCard)
            {
                Debug.Log(
                    cardState.GetCardName() +
                    " 不能使用，原因：" +
                    failReason
                );

                return false;
            }
        }

        return true;
    }
    public static void ApplyCooldownOnResolved(BattleCardState cardState)
    {
        // 罪卡默认不进入普通 CD
        if (cardState.IsSinCard())
        {
            // 罪卡成功结算后，增加负罪感
            if (cardState.cardData != null && cardState.cardData.guiltGain > 0)
            {
                GuiltManager.AddGuilt(
                    cardState.owner,
                    cardState.cardData.guiltGain,
                    "使用罪卡：" + cardState.GetCardName()
                );
            }

            if (cardState.ConsumeOnUse())
            {
                ConsumeCardUse(cardState);
            }
            else
            {
                if (BattleDebugSettings.ShowDetailBattleLog)
                {
                    Debug.Log(cardState.GetCardName() + " 是罪卡，不进入普通 CD");
                }
            }

            // 罪卡处理完后直接结束，不继续进入普通 CD 逻辑
            return;
        }
        // 罪卡默认不进入普通 CD
        // 罪卡默认不进入普通 CD
        if (cardState.IsSinCard())
        {
            if (cardState.IsUseCountSinCard())
            {
                ConsumeCardUse(cardState);
            }
            else
            {
                if (BattleDebugSettings.ShowDetailBattleLog)
                {
                    Debug.Log(cardState.GetCardName() + " 是罪卡，不进入普通 CD");
                }
            }

            // 罪卡处理完后直接结束，不继续进入普通 CD 逻辑
            return;
        }

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

            ReduceCardCooldown(cardState, 1);
        }
    }

    // ReduceCardCooldown = 减少单张卡的 CD
    // ConsumeCardUse = 消耗一次卡牌使用次数
    public static void ConsumeCardUse(BattleCardState cardState)
    {
        if (cardState == null)
        {
            return;
        }

        if (cardState.isConsumed)
        {
            return;
        }

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
    public static void ReduceCardCooldown(BattleCardState cardState, int amount)
    {
        if (cardState == null)
        {
            return;
        }

        if (amount <= 0)
        {
            return;
        }

        if (cardState.currentCooldown <= 0)
        {
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
                ReduceCardCooldown(cardState, amount);
            }
        }
    }

    // ReduceCooldownsByRarity = 按品质减少 CD
    // 例如：White / Green / Blue / Purple / Gold
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
                ReduceCardCooldown(cardState, amount);
            }
        }
    }

    // ReduceCooldownsByCardID = 按卡牌 ID 减少 CD
    // 例如：只减少 cardID 为 atk_001 的所有复制品
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
        if (character == null)
        {
            return "无角色";
        }

        return character.characterName;
    }
}