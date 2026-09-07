// 脚本中文说明：Bullet只由CharacterData.buffs承载；这里提供弹仓容量与封顶写入规则。
using UnityEngine;

public static class BattleBulletRules
{
    public const int BaseMagazineCapacity = 6;

    public static int GetBullet(CharacterData character)
    {
        return character != null
            ? Mathf.Max(0, character.GetBuffStack(BattleResourceID.Bullet))
            : 0;
    }

    public static int GetMagazineCapacity(CharacterData character)
    {
        return BattleModificationRules.IsActive(character)
            ? BattleModificationRules.ModifiedMagazineCapacity
            : BaseMagazineCapacity;
    }

    public static int AddBulletCapped(CharacterData character, int amount)
    {
        if (character == null || amount <= 0)
        {
            return GetBullet(character);
        }

        int current = GetBullet(character);
        int target = Mathf.Min(GetMagazineCapacity(character), current + amount);
        AddOrRemove(character, target - current);
        return target;
    }

    public static int ReloadToCapacity(CharacterData character)
    {
        if (character == null)
        {
            return 0;
        }

        int current = GetBullet(character);
        int target = GetMagazineCapacity(character);
        AddOrRemove(character, target - current);
        return target;
    }

    static void AddOrRemove(CharacterData character, int delta)
    {
        if (delta > 0)
        {
            character.AddBuff(BattleResourceID.Bullet, delta, -1);
            return;
        }

        if (delta < 0)
        {
            character.TryConsumeBuffStackAsResource(
                BattleResourceID.Bullet,
                -delta,
                out _
            );
        }
    }
}

public sealed class BattlePendingState
{
    public int nextUsedAttackPointBonus;
    public long breathGeneration;
    public bool reloadAtTurnEnd;
    public bool conservationPointGrant;
}

public static class BattlePendingRules
{
    public static void RegisterSuccessfulBreath(BattleCardState card)
    {
        if (card == null || card.owner == null || !card.cardUsedCommittedForCurrentAction ||
            card.breathSuccessPendingRegistered ||
            !card.HasTrait(BattleCardTrait.GrantNextClashPointUpOnSuccessfulDodge)) return;

        card.breathSuccessPendingRegistered = true;
        BattlePendingState pending = card.owner.battlePending;
        if (pending.nextUsedAttackPointBonus > 0) return;
        pending.nextUsedAttackPointBonus = 2;
        pending.breathGeneration++;
    }

    public static void CaptureAttackBonus(BattleCardState card)
    {
        if (card == null || card.owner == null || card.cardData == null ||
            card.cardData.cardType != CardType.Attack) return;
        card.borrowedBreathPointBonus = card.owner.battlePending.nextUsedAttackPointBonus;
        card.borrowedBreathGeneration = card.owner.battlePending.breathGeneration;
    }

    public static void HandleEvent(BattleEventContext context)
    {
        if (context == null || context.user == null) return;
        BattlePendingState pending = context.user.battlePending;
        if (context.timing == BattleTiming.CardUsed)
        {
            BattleCardState card = context.cardState;
            if (card == null || card.owner != context.user || !card.cardUsedCommittedForCurrentAction) return;
            if (card.cardData.cardType == CardType.Attack && card.borrowedBreathPointBonus > 0 &&
                card.borrowedBreathGeneration == pending.breathGeneration)
            {
                pending.nextUsedAttackPointBonus = 0;
            }
            if (card.cardData.cardType == CardType.Dodge &&
                card.HasTrait(BattleCardTrait.ReloadBulletOnDodgeResolution))
            {
                pending.reloadAtTurnEnd = true;
            }
        }
        else if (context.timing == BattleTiming.TurnEnd && pending.reloadAtTurnEnd)
        {
            pending.reloadAtTurnEnd = false;
            BattleBulletRules.ReloadToCapacity(context.user);
        }
    }
}

public static class BattleModificationRules
{
    public const int ModifiedMagazineCapacity = 4;
    public const int BulletConsumingCardPointBonus = 2;

    public static bool IsActive(CharacterData character)
    {
        return character != null &&
            character.GetBuffStack(BattleResourceID.Modification) > 0;
    }

    public static void Activate(CharacterData character)
    {
        if (character == null || IsActive(character))
        {
            return;
        }

        character.AddBuff(BattleResourceID.Modification, 1, -1);
        ClampToCapacity(character);
    }

    public static int GetCardPointBonus(CharacterData character, CardTestData card)
    {
        return IsActive(character) &&
            CardEffectExecutor.IsEligibleShootingAttack(card)
            ? BulletConsumingCardPointBonus
            : 0;
    }

    public static void ClampToCapacity(CharacterData character)
    {
        if (character == null)
        {
            return;
        }

        int current = BattleBulletRules.GetBullet(character);
        int capacity = BattleBulletRules.GetMagazineCapacity(character);
        if (current > capacity)
        {
            character.TryConsumeBuffStackAsResource(
                BattleResourceID.Bullet,
                current - capacity,
                out _
            );
        }
    }

}

// 节约规则只保存本回合的激活、单次卡实例转移和回合末结算。
public static class BattleConservationRules
{
    public static bool IsActive(CharacterData character)
    {
        return character != null &&
            character.GetBuffStack(BattleResourceID.Conservation) > 0;
    }

    public static void Activate(CharacterData character)
    {
        if (character == null)
        {
            return;
        }

        if (!IsActive(character))
        {
            character.AddBuff(BattleResourceID.Conservation, 1, -1);
        }
        character.conservationPointGrantPending = true;
    }

    public static bool HasPendingPointGrant(CharacterData character)
    {
        return character != null && character.conservationPointGrantPending;
    }

    public static bool IsShootingAttack(BattleCardState cardState)
    {
        return cardState != null && IsShootingAttack(cardState.cardData);
    }

    public static bool IsShootingAttack(CardTestData cardData)
    {
        return CardEffectExecutor.IsEligibleShootingAttack(cardData);
    }

    public static int GetPointBonusForBullet(int bullet)
    {
        switch (Mathf.Max(0, bullet))
        {
            case 1: return 6;
            case 2: return 4;
            case 3: return 3;
            case 4: return 2;
            case 5:
            case 6: return 1;
            default: return 0;
        }
    }

    public static bool TryAssignPendingBonus(
        CharacterData character,
        BattleCardState cardState
    )
    {
        if (!HasPendingPointGrant(character) || !IsShootingAttack(cardState) ||
            cardState.owner != character || cardState.hasConservationPointBonus)
        {
            return false;
        }

        int bonus = GetPointBonusForBullet(BattleBulletRules.GetBullet(character));
        cardState.conservationPointBonus = bonus;
        cardState.hasConservationPointBonus = bonus > 0;
        cardState.conservationKillReloadArmed = bonus > 0;
        character.conservationPointGrantPending = false;
        return true;
    }

    public static int GetAssignedPointBonus(BattleCardState cardState)
    {
        return cardState != null && cardState.hasConservationPointBonus
            ? Mathf.Max(0, cardState.conservationPointBonus)
            : 0;
    }

    public static void HandleEvent(BattleEventContext context)
    {
        if (context == null)
        {
            return;
        }

        if (context.timing == BattleTiming.AfterKill && context.isKill &&
            context.cardState != null &&
            context.cardState.conservationKillReloadArmed &&
            IsShootingAttack(context.cardState))
        {
            BattleBulletRules.ReloadToCapacity(context.user);
            context.cardState.conservationKillReloadArmed = false;
            return;
        }

        if (context.timing == BattleTiming.TurnEnd)
        {
            ResolveTurnEnd(context.user);
        }
    }

    public static int GetTurnEndPenaltyPercent(int bullet)
    {
        switch (Mathf.Max(0, bullet))
        {
            case 0: return 30;
            case 1: return 18;
            case 2: return 12;
            case 3: return 8;
            case 4: return 5;
            default: return 0;
        }
    }

    public static int ResolveTurnEnd(CharacterData character)
    {
        if (character == null || !IsActive(character))
        {
            return 0;
        }

        int bullet = BattleBulletRules.GetBullet(character);
        int penaltyPercent = GetTurnEndPenaltyPercent(bullet);
        int damage = Mathf.CeilToInt(character.maxHP * penaltyPercent / 100f);
        if (damage > 0)
        {
            character.TakeDamage(damage);
        }
        Clear(character);
        return damage;
    }

    public static void Clear(CharacterData character)
    {
        if (character == null)
        {
            return;
        }

        int stack = character.GetBuffStack(BattleResourceID.Conservation);
        if (stack > 0)
        {
            character.TryConsumeBuffStackAsResource(
                BattleResourceID.Conservation,
                stack,
                out _
            );
        }
        character.conservationPointGrantPending = false;
    }
}
