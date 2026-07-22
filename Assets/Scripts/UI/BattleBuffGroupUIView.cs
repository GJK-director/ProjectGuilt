using System;
using UnityEngine;

[Serializable]
public class BattleBuffIconBinding
{
    public string buffID;
    public string displayName;
    public Sprite iconSprite;
    public BattleBuffIconUIView iconView;
}

public class BattleBuffGroupUIView : MonoBehaviour
{
    [SerializeField] private BattleBuffIconBinding[] buffBindings;

    public void SetCharacter(CharacterData characterData)
    {
        if (buffBindings == null)
        {
            return;
        }

        for (int i = 0; i < buffBindings.Length; i++)
        {
            BattleBuffIconBinding binding = buffBindings[i];

            if (binding == null || binding.iconView == null)
            {
                continue;
            }

            if (characterData == null || string.IsNullOrEmpty(binding.buffID))
            {
                binding.iconView.SetEmpty();
                continue;
            }

            int stack = characterData.GetBuffStack(binding.buffID);

            if (stack > 0)
            {
                binding.iconView.SetBuff(binding.iconSprite, stack);
            }
            else
            {
                binding.iconView.SetEmpty();
            }
        }
    }

    public void Clear()
    {
        if (buffBindings == null)
        {
            return;
        }

        for (int i = 0; i < buffBindings.Length; i++)
        {
            BattleBuffIconBinding binding = buffBindings[i];

            if (binding != null && binding.iconView != null)
            {
                binding.iconView.SetEmpty();
            }
        }
    }
}
