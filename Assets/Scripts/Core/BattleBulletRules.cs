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
        return BaseMagazineCapacity;
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
