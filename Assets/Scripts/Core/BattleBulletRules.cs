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
        return IsActive(character) && UsesBullet(card)
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

    static bool UsesBullet(CardTestData card)
    {
        if (card == null)
        {
            return false;
        }

        CardResourceRuleData rule = card.resourceRule;
        if (rule == null && card.resourceRules != null && card.resourceRules.Length > 0)
        {
            rule = card.resourceRules[0];
        }

        return rule != null &&
            rule.resourceType == "BuffStack" &&
            rule.resourceID == BattleResourceID.Bullet &&
            rule.consumeAmountOnSuccess > 0;
    }
}
