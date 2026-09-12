// 脚本中文说明：验证角色卡牌能力到表现绑定需求的通用映射，不依赖真实美术资源。
using System.Collections.Generic;
using UnityEngine;

public static class CharacterPresentationBindingContractTests
{
    public static bool Run()
    {
        BattleCharacterPresentationRequirements baseRequirements =
            Requirements();
        BattleCharacterPresentationRequirements meleeRequirements =
            Requirements(CreateCard(CardType.Attack, AttackDeliveryMode.Melee));
        BattleCharacterPresentationRequirements longRangeRequirements =
            Requirements(CreateCard(
                CardType.Attack,
                AttackDeliveryMode.LongRangeShoot
            ));
        BattleCharacterPresentationRequirements closeRangeRequirements =
            Requirements(CreateCard(
                CardType.Attack,
                AttackDeliveryMode.CloseRangeShoot
            ));
        BattleCharacterPresentationRequirements defenseRequirements =
            Requirements(CreateCard(CardType.Defense, null));
        BattleCharacterPresentationRequirements dodgeRequirements =
            Requirements(CreateCard(CardType.Dodge, null));

        bool[] results =
        {
            Validate(baseRequirements, BaseBindings()),
            meleeRequirements.Requires(
                BattleCharacterPresentationCapability.MeleeAttack),
            longRangeRequirements.Requires(
                BattleCharacterPresentationCapability.LongRangeShoot),
            closeRangeRequirements.Requires(
                BattleCharacterPresentationCapability.CloseRangeShoot),
            defenseRequirements.Requires(
                BattleCharacterPresentationCapability.Defense),
            dodgeRequirements.Requires(
                BattleCharacterPresentationCapability.Dodge),
            VerifyMeleeDefenseDodgeDoesNotRequireShoot(),
            VerifyMissingBinding(
                longRangeRequirements,
                LongRangeBindings(aim: false),
                "Aim Sprite"
            ),
            VerifyMissingBinding(
                longRangeRequirements,
                LongRangeBindings(shoot: false),
                "LongRange Shoot Sprite"
            ),
            VerifyLongRangeMuzzleFlashContract(longRangeRequirements),
            VerifyMissingBinding(
                closeRangeRequirements,
                CloseRangeBindings(closeRangeShoot: false),
                "CloseRange Shoot Sprite"
            ),
            VerifyCloseRangeDoesNotUseLongRangeShoot(closeRangeRequirements),
            VerifyCloseRangeMuzzleFlashContract(closeRangeRequirements),
            VerifyMissingBinding(
                defenseRequirements,
                BaseBindings(),
                "Guard Sprite"
            ),
            VerifyMissingBinding(
                dodgeRequirements,
                BaseBindings(),
                "Dodge Sprite"
            ),
            Validate(baseRequirements, BaseBindings()),
            !BattleCharacterPresentationFacing.ShouldFlipX(
                true,
                BattleUnitCamp.Ally
            ),
            BattleCharacterPresentationFacing.ShouldFlipX(
                true,
                BattleUnitCamp.Enemy
            ) && meleeRequirements.Requires(
                BattleCharacterPresentationCapability.MeleeAttack),
            BattleCharacterPresentationFacing.ShouldFlipX(
                false,
                BattleUnitCamp.Ally
            ) && !BattleCharacterPresentationFacing.ShouldFlipX(
                false,
                BattleUnitCamp.Enemy
            ),
            VerifyArbitraryCharacterUsesOnlyCapabilitiesAndBindings()
        };

        string[] names =
        {
            "Base Idle/Hit/Renderer/VisualRoot完整时通过",
            "Melee Card自动产生Melee Requirement",
            "LongRange Card自动产生LongRange Requirement",
            "CloseRange Card自动产生CloseRange Requirement",
            "Defense Card自动产生Defense Requirement",
            "Dodge Card自动产生Dodge Requirement",
            "Melee/Defense/Dodge不要求LongRange或CloseRange绑定",
            "LongRange缺Aim明确失败",
            "LongRange缺Shoot明确失败",
            "LongRange枪口Anchor/Effect使用独立Required Contract",
            "CloseRange缺专用Shoot明确失败",
            "LongRange Shoot不能冒充CloseRange Shoot",
            "CloseRange枪口Anchor/Effect不读取LongRange引用",
            "Defense缺Guard明确失败",
            "Dodge缺Dodge明确失败",
            "无Dodge Card时不要求Dodge绑定",
            "sourceFacesRight=true用于Ally不翻转",
            "同一能力用于Enemy只改变flipX",
            "sourceFacesRight=false在两个Camp得到相反flipX",
            "任意新角色只靠Card Capability+Binding即可通过"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式102 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }

        Debug.Log(
            "模式102 Character Presentation Binding Contract聚合结果：" +
            allPassed
        );
        return allPassed;
    }

    private static bool VerifyMeleeDefenseDodgeDoesNotRequireShoot()
    {
        BattleCharacterPresentationRequirements requirements = Requirements(
            CreateCard(CardType.Attack, AttackDeliveryMode.Melee),
            CreateCard(CardType.Defense, null),
            CreateCard(CardType.Dodge, null)
        );
        BattleCharacterPresentationBindingSnapshot bindings = BaseBindings();
        bindings.HasSprintSprite = true;
        bindings.HasSlashSprite = true;
        bindings.HasGuardSprite = true;
        bindings.HasDodgeSprite = true;
        return Validate(requirements, bindings) &&
            !requirements.Requires(
                BattleCharacterPresentationCapability.LongRangeShoot) &&
            !requirements.Requires(
                BattleCharacterPresentationCapability.CloseRangeShoot);
    }

    private static bool VerifyLongRangeMuzzleFlashContract(
        BattleCharacterPresentationRequirements requirements
    )
    {
        bool missingAnchor = VerifyMissingBinding(
            requirements,
            LongRangeBindings(anchor: false),
            "LongRange MuzzleFlash Anchor"
        );
        bool missingEffect = VerifyMissingBinding(
            requirements,
            LongRangeBindings(effect: false),
            "LongRange MuzzleFlash Effect"
        );
        return missingAnchor && missingEffect &&
            Validate(requirements, LongRangeBindings());
    }

    private static bool VerifyCloseRangeDoesNotUseLongRangeShoot(
        BattleCharacterPresentationRequirements requirements
    )
    {
        BattleCharacterPresentationBindingSnapshot bindings =
            CloseRangeBindings(closeRangeShoot: false);
        bindings.HasShootSprite = true;
        return VerifyMissingBinding(
            requirements,
            bindings,
            "CloseRange Shoot Sprite"
        );
    }

    private static bool VerifyCloseRangeMuzzleFlashContract(
        BattleCharacterPresentationRequirements requirements
    )
    {
        BattleCharacterPresentationBindingSnapshot bindings =
            CloseRangeBindings(closeAnchor: false, closeEffect: false);
        bindings.HasLongRangeMuzzleFlashAnchor = true;
        bindings.HasLongRangeMuzzleFlashEffect = true;

        string error;
        bool failed = !BattleCharacterPresentationBindingValidator.TryValidate(
            "Mode102 CloseRange",
            requirements,
            bindings,
            out error
        );
        return failed &&
            error.Contains("CloseRange MuzzleFlash Anchor") &&
            error.Contains("CloseRange MuzzleFlash Effect") &&
            Validate(requirements, CloseRangeBindings());
    }

    private static bool VerifyArbitraryCharacterUsesOnlyCapabilitiesAndBindings()
    {
        CharacterData character = new CharacterData(
            "Arbitrary Mode102 Character",
            20,
            2,
            4,
            "unrelated_runtime_id"
        );
        AddCard(
            character,
            CreateCard(CardType.Attack, AttackDeliveryMode.CloseRangeShoot),
            "unrelated_card_instance"
        );
        AddCard(
            character,
            CreateCard(CardType.Defense, null),
            "another_unrelated_card_instance"
        );

        BattleCharacterPresentationRequirements requirements =
            BattleCharacterPresentationRequirements.FromCards(
                character.battleCards
            );
        BattleCharacterPresentationBindingSnapshot bindings =
            CloseRangeBindings();
        bindings.HasGuardSprite = true;
        return Validate(requirements, bindings) &&
            requirements.Requires(
                BattleCharacterPresentationCapability.CloseRangeShoot) &&
            requirements.Requires(
                BattleCharacterPresentationCapability.Defense);
    }

    private static bool VerifyMissingBinding(
        BattleCharacterPresentationRequirements requirements,
        BattleCharacterPresentationBindingSnapshot bindings,
        string expectedName
    )
    {
        string error;
        return !BattleCharacterPresentationBindingValidator.TryValidate(
                "Mode102 Character",
                requirements,
                bindings,
                out error
            ) &&
            !string.IsNullOrEmpty(error) && error.Contains(expectedName);
    }

    private static bool Validate(
        BattleCharacterPresentationRequirements requirements,
        BattleCharacterPresentationBindingSnapshot bindings
    )
    {
        string error;
        return BattleCharacterPresentationBindingValidator.TryValidate(
            "Mode102 Character",
            requirements,
            bindings,
            out error
        );
    }

    private static BattleCharacterPresentationRequirements Requirements(
        params CardTestData[] cards
    )
    {
        CharacterData owner = new CharacterData(
            "Mode102 Requirements Owner",
            10,
            1,
            1
        );
        if (cards != null)
        {
            for (int index = 0; index < cards.Length; index++)
            {
                AddCard(owner, cards[index], "mode102_card_" + index);
            }
        }
        return BattleCharacterPresentationRequirements.FromCards(
            owner.battleCards
        );
    }

    private static void AddCard(
        CharacterData owner,
        CardTestData card,
        string instanceID
    )
    {
        owner.battleCards.Add(new BattleCardState(owner, card, instanceID));
    }

    private static CardTestData CreateCard(
        string cardType,
        string deliveryMode
    )
    {
        return new CardTestData
        {
            cardID = "mode102_fixture",
            cardName = "Mode102 Fixture",
            cardType = cardType,
            attackDeliveryMode = deliveryMode,
            traits = new BattleCardTrait[0],
            effects = new List<CardEffectData>()
        };
    }

    private static BattleCharacterPresentationBindingSnapshot BaseBindings()
    {
        return new BattleCharacterPresentationBindingSnapshot
        {
            HasCharacterSpriteRenderer = true,
            HasBodyVisualRoot = true,
            HasIdleSprite = true,
            HasHitSprite = true
        };
    }

    private static BattleCharacterPresentationBindingSnapshot LongRangeBindings(
        bool aim = true,
        bool shoot = true,
        bool anchor = true,
        bool effect = true
    )
    {
        BattleCharacterPresentationBindingSnapshot bindings = BaseBindings();
        bindings.HasAimSprite = aim;
        bindings.HasShootSprite = shoot;
        bindings.HasLongRangeMuzzleFlashAnchor = anchor;
        bindings.HasLongRangeMuzzleFlashEffect = effect;
        return bindings;
    }

    private static BattleCharacterPresentationBindingSnapshot CloseRangeBindings(
        bool closeRangeShoot = true,
        bool closeAnchor = true,
        bool closeEffect = true
    )
    {
        BattleCharacterPresentationBindingSnapshot bindings = BaseBindings();
        bindings.HasSprintSprite = true;
        bindings.HasCloseRangeShootSprite = closeRangeShoot;
        bindings.HasCloseRangeMuzzleFlashAnchor = closeAnchor;
        bindings.HasCloseRangeMuzzleFlashEffect = closeEffect;
        return bindings;
    }
}
