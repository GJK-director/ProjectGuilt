// 脚本中文说明：由角色实际卡牌推导表现能力需求，并验证 Inspector 表现绑定。
using System;
using System.Collections.Generic;

[Flags]
public enum BattleCharacterPresentationCapability
{
    Base = 1 << 0,
    MeleeAttack = 1 << 1,
    LongRangeShoot = 1 << 2,
    CloseRangeShoot = 1 << 3,
    Defense = 1 << 4,
    Dodge = 1 << 5
}

public sealed class BattleCharacterPresentationRequirements
{
    public BattleCharacterPresentationCapability Capabilities { get; }

    public bool Requires(BattleCharacterPresentationCapability capability)
    {
        return (Capabilities & capability) == capability;
    }

    private BattleCharacterPresentationRequirements(
        BattleCharacterPresentationCapability capabilities
    )
    {
        Capabilities = capabilities;
    }

    public static BattleCharacterPresentationRequirements FromCards(
        IReadOnlyList<BattleCardState> cards
    )
    {
        BattleCharacterPresentationCapability capabilities =
            BattleCharacterPresentationCapability.Base;

        if (cards == null)
        {
            return new BattleCharacterPresentationRequirements(capabilities);
        }

        for (int index = 0; index < cards.Count; index++)
        {
            CardTestData card = cards[index] != null
                ? cards[index].cardData
                : null;
            if (card == null)
            {
                continue;
            }

            if (card.cardType == CardType.Attack)
            {
                string deliveryMode = card.GetAttackDeliveryMode();
                if (deliveryMode == AttackDeliveryMode.LongRangeShoot)
                {
                    capabilities |=
                        BattleCharacterPresentationCapability.LongRangeShoot;
                }
                else if (deliveryMode == AttackDeliveryMode.CloseRangeShoot)
                {
                    capabilities |=
                        BattleCharacterPresentationCapability.CloseRangeShoot;
                }
                else
                {
                    capabilities |=
                        BattleCharacterPresentationCapability.MeleeAttack;
                }
            }
            else if (card.cardType == CardType.Defense)
            {
                capabilities |= BattleCharacterPresentationCapability.Defense;
            }
            else if (card.cardType == CardType.Dodge)
            {
                capabilities |= BattleCharacterPresentationCapability.Dodge;
            }
        }

        return new BattleCharacterPresentationRequirements(capabilities);
    }
}

public sealed class BattleCharacterPresentationBindingSnapshot
{
    public bool HasCharacterSpriteRenderer;
    public bool HasBodyVisualRoot;
    public bool HasIdleSprite;
    public bool HasSprintSprite;
    public bool HasSlashSprite;
    public bool HasAimSprite;
    public bool HasShootSprite;
    public bool HasCloseRangeShootSprite;
    public bool HasHitSprite;
    public bool HasGuardSprite;
    public bool HasDodgeSprite;
    public bool HasLongRangeMuzzleFlashAnchor;
    public bool HasLongRangeMuzzleFlashEffect;
    public bool HasCloseRangeMuzzleFlashAnchor;
    public bool HasCloseRangeMuzzleFlashEffect;
    public bool HasSlashBackEffect;
    public bool HasSlashFrontEffect;
}

public static class BattleCharacterPresentationBindingValidator
{
    public static bool TryValidate(
        CharacterData character,
        BattleCharacterPresentationController controller,
        out BattleCharacterPresentationRequirements requirements,
        out string errorMessage
    )
    {
        requirements = BattleCharacterPresentationRequirements.FromCards(
            character != null ? character.battleCards : null
        );

        if (character == null)
        {
            errorMessage = "Character Presentation Binding校验失败：角色为空";
            return false;
        }
        if (controller == null)
        {
            errorMessage = character.characterName +
                " 缺少 BattleCharacterPresentationController";
            return false;
        }

        return TryValidate(
            character.characterName,
            requirements,
            controller.GetBindingSnapshot(),
            out errorMessage
        );
    }

    public static bool TryValidate(
        string characterLabel,
        BattleCharacterPresentationRequirements requirements,
        BattleCharacterPresentationBindingSnapshot bindings,
        out string errorMessage
    )
    {
        List<string> missing = new List<string>();
        if (requirements == null)
        {
            errorMessage = "Character Presentation Binding校验失败：Requirements为空";
            return false;
        }
        if (bindings == null)
        {
            errorMessage = "Character Presentation Binding校验失败：Snapshot为空";
            return false;
        }

        Require(bindings.HasCharacterSpriteRenderer, "Character SpriteRenderer", missing);
        Require(bindings.HasBodyVisualRoot, "Body Visual Root", missing);
        Require(bindings.HasIdleSprite, "Idle Sprite", missing);
        Require(bindings.HasHitSprite, "Hit Sprite", missing);

        if (requirements.Requires(
                BattleCharacterPresentationCapability.MeleeAttack))
        {
            Require(bindings.HasSprintSprite, "Sprint Sprite (Melee)", missing);
            Require(bindings.HasSlashSprite, "Slash Sprite", missing);
        }

        if (requirements.Requires(
                BattleCharacterPresentationCapability.LongRangeShoot))
        {
            Require(bindings.HasAimSprite, "Aim Sprite", missing);
            Require(bindings.HasShootSprite, "LongRange Shoot Sprite", missing);
            Require(
                bindings.HasLongRangeMuzzleFlashAnchor,
                "LongRange MuzzleFlash Anchor",
                missing
            );
            Require(
                bindings.HasLongRangeMuzzleFlashEffect,
                "LongRange MuzzleFlash Effect",
                missing
            );
        }

        if (requirements.Requires(
                BattleCharacterPresentationCapability.CloseRangeShoot))
        {
            Require(
                bindings.HasSprintSprite,
                "Sprint Sprite (CloseRange)",
                missing
            );
            Require(
                bindings.HasCloseRangeShootSprite,
                "CloseRange Shoot Sprite",
                missing
            );
            Require(
                bindings.HasCloseRangeMuzzleFlashAnchor,
                "CloseRange MuzzleFlash Anchor",
                missing
            );
            Require(
                bindings.HasCloseRangeMuzzleFlashEffect,
                "CloseRange MuzzleFlash Effect",
                missing
            );
        }

        if (requirements.Requires(
                BattleCharacterPresentationCapability.Defense))
        {
            Require(bindings.HasGuardSprite, "Guard Sprite", missing);
        }

        if (requirements.Requires(
                BattleCharacterPresentationCapability.Dodge))
        {
            Require(bindings.HasDodgeSprite, "Dodge Sprite", missing);
        }

        if (missing.Count == 0)
        {
            errorMessage = string.Empty;
            return true;
        }

        string safeLabel = string.IsNullOrEmpty(characterLabel)
            ? "<unknown character>"
            : characterLabel;
        errorMessage = safeLabel +
            " Presentation Binding缺失：" + string.Join(", ", missing);
        return false;
    }

    public static string CreateRequirementSummary(
        BattleCharacterPresentationRequirements requirements
    )
    {
        return requirements != null
            ? "Required Capabilities: " + requirements.Capabilities
            : "Required Capabilities: <null>";
    }

    private static void Require(
        bool available,
        string bindingName,
        List<string> missing
    )
    {
        if (!available)
        {
            missing.Add(bindingName);
        }
    }
}

public static class BattleCharacterPresentationFacing
{
    public static bool ShouldFlipX(
        bool sourceFacesRight,
        BattleUnitCamp camp
    )
    {
        bool desiredFacesRight = camp == BattleUnitCamp.Ally;
        return sourceFacesRight != desiredFacesRight;
    }
}
