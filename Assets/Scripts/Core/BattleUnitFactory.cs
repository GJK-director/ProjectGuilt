using System.Collections.Generic;

public class BattleUnitFactoryResult
{
    public bool isSuccess;
    public string errorMessage;
    public CharacterData unit;

    public static BattleUnitFactoryResult Success(CharacterData unit)
    {
        return new BattleUnitFactoryResult
        {
            isSuccess = true,
            errorMessage = "",
            unit = unit
        };
    }

    public static BattleUnitFactoryResult Failure(string errorMessage)
    {
        return new BattleUnitFactoryResult
        {
            isSuccess = false,
            errorMessage = errorMessage,
            unit = null
        };
    }
}

public static class BattleUnitFactory
{
    // Factory先完成跨文件引用校验，再创建运行时对象，避免cardID或buffID错误时留下半完成角色。
    public static BattleUnitFactoryResult CreatePlayer(CharacterDefinitionData definition, List<CardTestData> cards)
    {
        return CreatePlayer(
            definition,
            cards,
            definition != null ? definition.characterID : null
        );
    }

    public static BattleUnitFactoryResult CreatePlayer(
        CharacterDefinitionData definition,
        List<CardTestData> cards,
        string runtimeUnitID
    )
    {
        if (definition == null)
        {
            return BattleUnitFactoryResult.Failure("角色定义为空");
        }

        if (definition.actionSlotCount != 2)
        {
            return BattleUnitFactoryResult.Failure(definition.characterID + " 的 actionSlotCount 必须等于2");
        }

        List<CardTestData> resolvedCards;
        string errorMessage;

        if (!ResolveCardReferences(definition.characterID, definition.startingCardIDs, cards, out resolvedCards, out errorMessage))
        {
            return BattleUnitFactoryResult.Failure(errorMessage);
        }

        if (!ValidateInitialBuffReferences(definition.characterID, definition.initialBuffs, out errorMessage))
        {
            return BattleUnitFactoryResult.Failure(errorMessage);
        }

        CharacterData unit = new CharacterData(
            definition.characterName,
            definition.maxHP,
            definition.minSpeed,
            definition.maxSpeed,
            runtimeUnitID
        );

        CreateBattleCards(unit, runtimeUnitID, resolvedCards);
        ApplyInitialBuffs(unit, definition.initialBuffs);

        return BattleUnitFactoryResult.Success(unit);
    }

    // Factory先完成跨文件引用校验，再创建运行时对象，避免cardID或buffID错误时留下半完成角色。
    public static BattleUnitFactoryResult CreateEnemy(EnemyDefinitionData definition, List<CardTestData> cards)
    {
        return CreateEnemy(
            definition,
            cards,
            definition != null ? definition.enemyID : null
        );
    }

    public static BattleUnitFactoryResult CreateEnemy(
        EnemyDefinitionData definition,
        List<CardTestData> cards,
        string runtimeUnitID
    )
    {
        if (definition == null)
        {
            return BattleUnitFactoryResult.Failure("敌人定义为空");
        }

        List<CardTestData> resolvedCards;
        string errorMessage;

        if (!ResolveCardReferences(definition.enemyID, definition.cardIDs, cards, out resolvedCards, out errorMessage))
        {
            return BattleUnitFactoryResult.Failure(errorMessage);
        }

        if (!ValidateInitialBuffReferences(definition.enemyID, definition.initialBuffs, out errorMessage))
        {
            return BattleUnitFactoryResult.Failure(errorMessage);
        }

        CharacterData unit = new CharacterData(
            definition.enemyName,
            definition.maxHP,
            definition.minSpeed,
            definition.maxSpeed,
            runtimeUnitID
        );

        CreateBattleCards(unit, runtimeUnitID, resolvedCards);
        ApplyInitialBuffs(unit, definition.initialBuffs);

        return BattleUnitFactoryResult.Success(unit);
    }

    static bool ResolveCardReferences(
        string ownerID,
        string[] cardIDs,
        List<CardTestData> cards,
        out List<CardTestData> resolvedCards,
        out string errorMessage
    )
    {
        resolvedCards = new List<CardTestData>();
        errorMessage = "";

        if (cards == null)
        {
            errorMessage = ownerID + " 创建失败：卡牌列表为空";
            return false;
        }

        if (cardIDs == null || cardIDs.Length == 0)
        {
            errorMessage = ownerID + " 创建失败：卡牌ID列表为空";
            return false;
        }

        foreach (string cardID in cardIDs)
        {
            if (string.IsNullOrEmpty(cardID))
            {
                errorMessage = ownerID + " 创建失败：存在空 cardID";
                return false;
            }

            CardTestData card = CardDataLoader.FindCardByID(cards, cardID);

            if (card == null)
            {
                errorMessage = ownerID + " 创建失败：找不到卡牌 " + cardID;
                return false;
            }

            resolvedCards.Add(card);
        }

        return true;
    }

    static bool ValidateInitialBuffReferences(
        string ownerID,
        InitialBuffDefinitionData[] initialBuffs,
        out string errorMessage
    )
    {
        errorMessage = "";

        if (initialBuffs == null)
        {
            return true;
        }

        foreach (InitialBuffDefinitionData initialBuff in initialBuffs)
        {
            if (initialBuff == null)
            {
                errorMessage = ownerID + " 创建失败：存在空 initialBuff";
                return false;
            }

            if (string.IsNullOrEmpty(initialBuff.buffID))
            {
                errorMessage = ownerID + " 创建失败：initialBuff buffID 为空";
                return false;
            }

            if (initialBuff.stack <= 0 || (initialBuff.duration != -1 && initialBuff.duration <= 0))
            {
                errorMessage = ownerID + " 创建失败：initialBuff 数值非法：" + initialBuff.buffID;
                return false;
            }

            BuffDefinitionData definition;

            // 正式角色JSON中的buffID必须存在于BuffDefinitions。
            // 不允许回落到GuessBuff兼容逻辑。
            if (!BuffDefinitionLoader.TryGetDefinition(initialBuff.buffID, out definition))
            {
                errorMessage = ownerID + " 创建失败：找不到 BuffDefinitions 中的 buffID " + initialBuff.buffID;
                return false;
            }
        }

        return true;
    }

    static void CreateBattleCards(CharacterData owner, string ownerID, List<CardTestData> resolvedCards)
    {
        Dictionary<string, int> copyIndexByCardID = new Dictionary<string, int>();

        foreach (CardTestData card in resolvedCards)
        {
            int copyIndex = 0;

            if (copyIndexByCardID.ContainsKey(card.cardID))
            {
                copyIndex = copyIndexByCardID[card.cardID];
            }

            string instanceID = ownerID + "_" + card.cardID + "_copy_" + copyIndex;
            BattleCardManager.CreateBattleCard(owner, card, instanceID);
            copyIndexByCardID[card.cardID] = copyIndex + 1;
        }
    }

    internal static void ApplyInitialBuffs(
        CharacterData unit,
        InitialBuffDefinitionData[] initialBuffs
    )
    {
        if (unit == null || initialBuffs == null)
        {
            return;
        }

        foreach (InitialBuffDefinitionData initialBuff in initialBuffs)
        {
            if (initialBuff == null ||
                unit.GetBuffStack(initialBuff.buffID) > 0)
            {
                continue;
            }

            unit.AddBuff(initialBuff.buffID, initialBuff.stack, initialBuff.duration);
        }
    }
}
