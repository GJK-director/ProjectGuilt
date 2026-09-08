// 脚本中文说明：验证角色通用默认卡列表与 CardData 权威语义的数据契约。
using System.Collections.Generic;
using UnityEngine;

public static class CharacterDefaultCardDataContractTests
{
    public static bool Run()
    {
        List<CardTestData> productionCards = CardDataLoader.LoadCardData();
        List<CharacterDefinitionData> characterDefinitions =
            CharacterDefinitionLoader.LoadDefinitions();
        List<EnemyDefinitionData> enemyDefinitions =
            EnemyDefinitionLoader.LoadDefinitions();

        CardTestData melee = Find(productionCards, "atk_001");
        CardTestData defense = Find(productionCards, "def_001");
        CardTestData dodge = Find(productionCards, "dodge_001");
        CardTestData longRange = Find(productionCards, "atk_bullet_001");
        CardTestData closeRange = CreateAttack(
            "mode101_close_range",
            AttackDeliveryMode.CloseRangeShoot,
            null
        );
        CardTestData firstStrike = CreateAttack(
            "mode101_first_strike_melee",
            AttackDeliveryMode.Melee,
            new[] { BattleCardTrait.FirstStrike }
        );

        bool[] results =
        {
            VerifyProductionReferences(
                productionCards,
                characterDefinitions,
                enemyDefinitions
            ),
            HasType(melee, CardType.Attack),
            HasType(defense, CardType.Defense),
            HasType(dodge, CardType.Dodge),
            melee != null && melee.IsMeleeAttack(),
            longRange != null && longRange.IsLongRangeShoot(),
            closeRange.IsCloseRangeShoot(),
            longRange != null && longRange.IsLongRangeShoot() &&
                !longRange.HasTrait(BattleCardTrait.FirstStrike),
            firstStrike.IsMeleeAttack() &&
                firstStrike.HasTrait(BattleCardTrait.FirstStrike),
            defense != null && dodge != null &&
                !defense.IsMeleeAttack() && !defense.IsLongRangeShoot() &&
                !defense.IsCloseRangeShoot() &&
                !dodge.IsMeleeAttack() && !dodge.IsLongRangeShoot() &&
                !dodge.IsCloseRangeShoot(),
            VerifyResourceComesFromCardData(),
            VerifyCooldownComesFromCardData(),
            VerifyMissingReferenceFails(),
            VerifyEmptyReferencesFailSafely(),
            VerifyDifferentCharactersUseSameContract(),
            VerifyCampDoesNotChangeCardData(),
            VerifyIDsDoNotDefineCombatSemantics(),
            VerifyCompleteFixtureCardCreatesRuntimeState()
        };

        string[] names =
        {
            "生产 Character/Enemy 默认卡引用全部可解析",
            "通用列表中的 Attack 类型来自 CardData",
            "通用列表中的 Defense 类型来自 CardData",
            "通用列表中的 Dodge 类型来自 CardData",
            "Melee Attack 由 CardType+DeliveryMode 表达",
            "LongRangeShoot 仍属于 Attack",
            "CloseRangeShoot 仍属于 Attack",
            "LongRangeShoot 不自动获得 FirstStrike",
            "FirstStrike 完全由 Trait Data 决定",
            "Defense/Dodge 不产生 Attack Delivery 行为",
            "Resource Rule 权威来自 CardData",
            "Cooldown 权威来自 CardData",
            "不存在 CardID 明确失败且不 fallback",
            "空或 null CardID 安全失败",
            "不同 Character 可引用不同 Attack",
            "Ally/Enemy 共享同一 Card Definition 语义",
            "解析不依赖 CardID 前缀、角色ID或 Camp",
            "新普通卡只靠数据组合即可创建 Runtime Card"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式101 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }

        Debug.Log(
            "模式101 Character Default Card Data Contract聚合结果：" +
            allPassed
        );
        return allPassed;
    }

    private static bool VerifyProductionReferences(
        List<CardTestData> cards,
        List<CharacterDefinitionData> characters,
        List<EnemyDefinitionData> enemies
    )
    {
        if (cards == null || characters == null || enemies == null ||
            characters.Count == 0 || enemies.Count == 0)
        {
            return false;
        }

        for (int index = 0; index < characters.Count; index++)
        {
            CharacterDefinitionData definition = characters[index];
            if (definition == null || !CanResolve(
                    definition.characterID,
                    definition.startingCardIDs,
                    cards
                ))
            {
                return false;
            }
        }

        for (int index = 0; index < enemies.Count; index++)
        {
            EnemyDefinitionData definition = enemies[index];
            if (definition == null || !CanResolve(
                    definition.enemyID,
                    definition.cardIDs,
                    cards
                ))
            {
                return false;
            }
        }

        return true;
    }

    private static bool VerifyResourceComesFromCardData()
    {
        CardTestData card = CreateAttack(
            "mode101_resource",
            AttackDeliveryMode.LongRangeShoot,
            null
        );
        card.resourceRule = new CardResourceRuleData
        {
            resourceType = "BuffStack",
            resourceID = "Mode101Resource",
            requiredStackForNormalVersion = 2,
            consumeAmountOnSuccess = 1
        };

        return ResolveSingle(card, out CardTestData resolved) &&
            object.ReferenceEquals(resolved.resourceRule, card.resourceRule) &&
            resolved.resourceRule.requiredStackForNormalVersion == 2;
    }

    private static bool VerifyCooldownComesFromCardData()
    {
        CardTestData card = CreateAttack(
            "mode101_cooldown",
            AttackDeliveryMode.Melee,
            null
        );
        card.cooldown = 4;
        return ResolveSingle(card, out CardTestData resolved) &&
            resolved.cooldown == 4 &&
            BattleCardManager.GetBaseCooldown(resolved) == 4;
    }

    private static bool VerifyMissingReferenceFails()
    {
        List<CardTestData> resolved;
        string error;
        bool success = CharacterDefaultCardValidator.TryResolve(
            "mode101_missing_owner",
            new[] { "mode101_missing_card" },
            new List<CardTestData>(),
            out resolved,
            out error
        );
        return !success && resolved != null && resolved.Count == 0 &&
            !string.IsNullOrEmpty(error) &&
            error.Contains("mode101_missing_card");
    }

    private static bool VerifyEmptyReferencesFailSafely()
    {
        List<CardTestData> cards = new List<CardTestData>
        {
            CreateAttack("mode101_safe", AttackDeliveryMode.Melee, null)
        };
        bool nullList = !TryResolve("null_list", null, cards);
        bool emptyList = !TryResolve("empty_list", new string[0], cards);
        bool nullID = !TryResolve("null_id", new string[] { null }, cards);
        bool emptyID = !TryResolve("empty_id", new[] { string.Empty }, cards);
        return nullList && emptyList && nullID && emptyID;
    }

    private static bool VerifyDifferentCharactersUseSameContract()
    {
        CardTestData first = CreateAttack(
            "mode101_character_a_card",
            AttackDeliveryMode.Melee,
            null
        );
        CardTestData second = CreateAttack(
            "mode101_character_b_card",
            AttackDeliveryMode.CloseRangeShoot,
            null
        );
        List<CardTestData> cards = new List<CardTestData> { first, second };
        return TryResolveSingle(
                "unrelated_character_a",
                first.cardID,
                cards,
                out CardTestData resolvedA
            ) &&
            TryResolveSingle(
                "unrelated_character_b",
                second.cardID,
                cards,
                out CardTestData resolvedB
            ) &&
            resolvedA.IsMeleeAttack() && resolvedB.IsCloseRangeShoot();
    }

    private static bool VerifyCampDoesNotChangeCardData()
    {
        CardTestData shared = CreateAttack(
            "mode101_shared_definition",
            AttackDeliveryMode.LongRangeShoot,
            null
        );
        List<CardTestData> cards = new List<CardTestData> { shared };
        return TryResolveSingle(
                "ally_arbitrary",
                shared.cardID,
                cards,
                out CardTestData allyCard
            ) &&
            TryResolveSingle(
                "enemy_arbitrary",
                shared.cardID,
                cards,
                out CardTestData enemyCard
            ) &&
            object.ReferenceEquals(allyCard, enemyCard) &&
            allyCard.GetAttackDeliveryMode() ==
                AttackDeliveryMode.LongRangeShoot;
    }

    private static bool VerifyIDsDoNotDefineCombatSemantics()
    {
        CardTestData arbitrary = CreateAttack(
            "completely_arbitrary_identifier",
            AttackDeliveryMode.CloseRangeShoot,
            new[] { BattleCardTrait.FirstStrike }
        );
        return TryResolveSingle(
                "unrelated_owner_identifier",
                arbitrary.cardID,
                new List<CardTestData> { arbitrary },
                out CardTestData resolved
            ) &&
            resolved.IsCloseRangeShoot() &&
            resolved.HasTrait(BattleCardTrait.FirstStrike);
    }

    private static bool VerifyCompleteFixtureCardCreatesRuntimeState()
    {
        CardTestData card = CreateAttack(
            "mode101_complete_fixture",
            AttackDeliveryMode.Melee,
            new[] { BattleCardTrait.FirstStrike }
        );
        card.resourceRule = new CardResourceRuleData
        {
            resourceType = "BuffStack",
            resourceID = "Mode101Resource",
            requiredStackForNormalVersion = 1
        };
        card.cooldown = 3;
        CharacterDefinitionData definition = new CharacterDefinitionData
        {
            characterID = "mode101_character",
            characterName = "Mode101 Character",
            maxHP = 20,
            minSpeed = 2,
            maxSpeed = 4,
            actionSlotCount = 2,
            startingCardIDs = new[] { card.cardID },
            initialBuffs = new InitialBuffDefinitionData[0],
            prefabKey = "unused_in_factory",
            portraitKey = "unused_in_factory"
        };

        BattleUnitFactoryResult result = BattleUnitFactory.CreatePlayer(
            definition,
            new List<CardTestData> { card }
        );
        return result.isSuccess && result.unit != null &&
            result.unit.battleCards.Count == 1 &&
            result.unit.battleCards[0].HasTrait(BattleCardTrait.FirstStrike) &&
            result.unit.battleCards[0].IsMeleeAttack() &&
            result.unit.battleCards[0].cardData.cooldown == 3 &&
            object.ReferenceEquals(
                result.unit.battleCards[0].cardData.resourceRule,
                card.resourceRule
            );
    }

    private static bool ResolveSingle(
        CardTestData card,
        out CardTestData resolved
    )
    {
        return TryResolveSingle(
            "mode101_owner",
            card.cardID,
            new List<CardTestData> { card },
            out resolved
        );
    }

    private static bool TryResolveSingle(
        string ownerID,
        string cardID,
        List<CardTestData> cards,
        out CardTestData resolved
    )
    {
        resolved = null;
        List<CardTestData> resolvedCards;
        string error;
        if (!CharacterDefaultCardValidator.TryResolve(
                ownerID,
                new[] { cardID },
                cards,
                out resolvedCards,
                out error
            ) || resolvedCards.Count != 1)
        {
            return false;
        }

        resolved = resolvedCards[0];
        return true;
    }

    private static bool CanResolve(
        string ownerID,
        string[] cardIDs,
        List<CardTestData> cards
    )
    {
        return TryResolve(ownerID, cardIDs, cards);
    }

    private static bool TryResolve(
        string ownerID,
        string[] cardIDs,
        List<CardTestData> cards
    )
    {
        List<CardTestData> resolved;
        string error;
        return CharacterDefaultCardValidator.TryResolve(
            ownerID,
            cardIDs,
            cards,
            out resolved,
            out error
        );
    }

    private static bool HasType(CardTestData card, string cardType)
    {
        return card != null && card.cardType == cardType;
    }

    private static CardTestData Find(
        List<CardTestData> cards,
        string cardID
    )
    {
        return CardDataLoader.FindCardByID(cards, cardID);
    }

    private static CardTestData CreateAttack(
        string cardID,
        string deliveryMode,
        BattleCardTrait[] traits
    )
    {
        return new CardTestData
        {
            cardID = cardID,
            cardName = cardID,
            rarity = "White",
            cardType = CardType.Attack,
            attackDeliveryMode = deliveryMode,
            traits = traits,
            minPoint = 1,
            maxPoint = 2,
            cooldown = 1,
            damageFormula = "PointAsDamage",
            effects = new List<CardEffectData>()
        };
    }
}
