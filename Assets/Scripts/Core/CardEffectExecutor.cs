using UnityEngine;

// CardEffectExecutor = 卡牌效果执行器
// 专门负责执行卡牌 effects 里的效果
public static class CardEffectExecutor
{
    // ExecuteCardEffects = 执行卡牌效果
    // user = 使用卡牌的角色
    // target = 卡牌目标角色
    // card = 使用的卡牌
    // trigger = 当前触发时机，例如 OnPlay / AfterDamage
    public static void ExecuteCardEffects(
      CharacterData user,
      CharacterData target,
      CardTestData card,
      string trigger,
      string currentClashResult = "None"
    )
    {
        if (card.effects == null || card.effects.Count == 0)
        {
            return;
        }

        foreach (CardEffectData effect in card.effects)
        {
            if (!IsTriggerMatched(effect, trigger))
            {
                continue;
            }
            if (!IsClashResultMatched(effect, currentClashResult))
            {
                continue;
            }
            if (effect.effectType == CardEffectType.ApplyBuff)
            {
                CharacterData effectTarget = GetEffectTarget(user, target, effect.target);

                if (effectTarget == null)
                {
                    Debug.LogWarning("找不到效果目标：" + effect.target);
                    continue;
                }

                ApplyBuffEffect(effectTarget, effect);
            }
            else if (effect.effectType == CardEffectType.ReduceCooldown)
            {
                CharacterData effectTarget = GetEffectTarget(user, target, effect.target);

                if (effectTarget == null)
                {
                    Debug.LogWarning("找不到减少冷却的目标：" + effect.target);
                    continue;
                }

                ApplyReduceCooldownEffect(effectTarget, effect);
            }
            else
            {
                Debug.LogWarning("暂未处理的效果类型：" + effect.effectType);
            }
        }
    }

    // GetEffectTarget = 获取效果目标
    static CharacterData GetEffectTarget(CharacterData user, CharacterData target, string targetType)
    {
        if (string.IsNullOrEmpty(targetType))
        {
            targetType = CardTargetType.Target;
        }

        if (targetType == CardTargetType.Self)
        {
            return user;
        }

        if (targetType == CardTargetType.Target)
        {
            return target;
        }

        Debug.LogWarning("暂未处理的效果目标类型：" + targetType);
        return null;
    }
    // IsClashResultMatched = 检查拼点结果条件是否满足
    // IsTriggerMatched = 检查触发阶段是否匹配
    static bool IsTriggerMatched(CardEffectData effect, string currentTrigger)
    {
        if (effect == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(effect.trigger))
        {
            return false;
        }

        if (string.IsNullOrEmpty(currentTrigger))
        {
            return false;
        }

        // 正常完全匹配
        if (effect.trigger == currentTrigger)
        {
            return true;
        }

        // 兼容旧字段：
        // 旧的 OnPlay 等同于新的 BeforeUse
        // 兼容旧字段：
        // 旧的 OnPlay 等同于新的 BeforeUse
        if (currentTrigger == BattleTiming.BeforeUse && effect.trigger == BattleTiming.OnPlay)
        {
            return true;
        }

        // 如果以后还有旧代码调用 OnPlay，也能正常响应新的 BeforeUse
        if (currentTrigger == BattleTiming.OnPlay && effect.trigger == BattleTiming.BeforeUse)
        {
            return true;
        }

        return false;
    }
    static bool IsClashResultMatched(CardEffectData effect, string currentClashResult)
    {
        if (effect == null)
        {
            return false;
        }

        // 没填 requireClashResult，就代表不限制
        if (string.IsNullOrEmpty(effect.requireClashResult))
        {
            return true;
        }

        // Any 也代表不限制
        // Any 也代表不限制
        if (effect.requireClashResult == ClashResult.Any)
        {
            return true;
        }

        if (string.IsNullOrEmpty(currentClashResult))
        {
            currentClashResult = ClashResult.None;
        }

        return effect.requireClashResult == currentClashResult;
    }
    // ApplyBuffEffect = 执行添加 Buff 的效果
    static void ApplyBuffEffect(CharacterData effectTarget, CardEffectData effect)
    {
        string applyTiming = effect.applyTiming;

        if (string.IsNullOrEmpty(applyTiming))
        {
            applyTiming = "Immediate";
        }

        if (applyTiming == "Delayed")
        {
            int delayTurns = effect.delayTurns;
            int applyTimes = effect.applyTimes;
            int intervalTurns = effect.intervalTurns;

            if (delayTurns <= 0)
            {
                delayTurns = 1;
            }

            if (applyTimes <= 0)
            {
                applyTimes = 1;
            }

            if (intervalTurns <= 0)
            {
                intervalTurns = 1;
            }

            effectTarget.AddPendingBuff(
                effect.buffType,
                effect.buffName,
                effect.buffCategory,
                effect.stack,
                effect.duration,
                effect.checkTiming,
                effect.expireRule,
                delayTurns,
                applyTimes,
                intervalTurns
            );
        }
        else
        {
            effectTarget.AddBuff(
                effect.buffType,
                effect.buffName,
                effect.buffCategory,
                effect.stack,
                effect.duration,
                effect.checkTiming,
                effect.expireRule
            );
        }
    }
    // ApplyReduceCooldownEffect = 执行减少冷却效果
    static void ApplyReduceCooldownEffect(CharacterData effectTarget, CardEffectData effect)
    {
        int amount = effect.cooldownAmount;

        // 如果 cooldownAmount 没填，就临时兼容 stack
        if (amount <= 0)
        {
            amount = effect.stack;
        }

        // 如果还是没填，就默认减少 1
        if (amount <= 0)
        {
            amount = 1;
        }

        string cooldownTarget = effect.cooldownTarget;

        if (string.IsNullOrEmpty(cooldownTarget))
        {
            cooldownTarget = CooldownTargetType.All;
        }

        if (cooldownTarget == CooldownTargetType.All)
        {
            BattleCardManager.ReduceAllCooldowns(effectTarget, amount);

            if (BattleDebugSettings.ShowDetailBattleLog)
            {
                Debug.Log(effectTarget.characterName + " 的全部卡牌 CD -" + amount);
            }

            return;
        }

        if (cooldownTarget == CooldownTargetType.CardType)
        {
            BattleCardManager.ReduceCooldownsByCardType(effectTarget, effect.targetCardType, amount);

            if (BattleDebugSettings.ShowDetailBattleLog)
            {
                Debug.Log(effectTarget.characterName + " 的 " + effect.targetCardType + " 类型卡牌 CD -" + amount);
            }

            return;
        }

        if (cooldownTarget == CooldownTargetType.Rarity)
        {
            BattleCardManager.ReduceCooldownsByRarity(effectTarget, effect.targetRarity, amount);

            if (BattleDebugSettings.ShowDetailBattleLog)
            {
                Debug.Log(effectTarget.characterName + " 的 " + effect.targetRarity + " 品质卡牌 CD -" + amount);
            }

            return;
        }

        if (cooldownTarget == CooldownTargetType.CardID)
        {
            BattleCardManager.ReduceCooldownsByCardID(effectTarget, effect.targetCardID, amount);

            if (BattleDebugSettings.ShowDetailBattleLog)
            {
                Debug.Log(effectTarget.characterName + " 的指定卡牌 " + effect.targetCardID + " CD -" + amount);
            }

            return;
        }

        Debug.LogWarning("未知的减少冷却范围：" + cooldownTarget);
    }
}