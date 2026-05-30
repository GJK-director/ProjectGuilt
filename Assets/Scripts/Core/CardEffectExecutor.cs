// 脚本中文说明：卡牌效果执行器。负责按照触发时机执行卡牌 effects，例如添加 Buff 或减少 CD。
using UnityEngine;

// CardEffectExecutor = 卡牌效果执行器
//他会根据别人传进来的标签是否匹配来执行相应的效果
// Effect = 效果，Executor = 执行器。
// 专门负责执行卡牌 JSON 里 effects 列表中的效果。
// 注意：这个脚本只负责“效果怎么执行”，不负责判断卡牌能不能使用，也不负责拼点胜负。
public static class CardEffectExecutor
{
    // ExecuteCardEffects = 执行卡牌效果
    // Execute = 执行，CardEffects = 卡牌效果。
    // user = 使用卡牌的角色。
    // target = 卡牌目标角色。
    // card = 使用的卡牌模板数据。
    // trigger = 当前触发时机，例如 OnPlay / AfterDamage。
    // currentClashResult = 当前拼点结果，例如 Win / Lose / None。
    public static void ExecuteCardEffects(
      CharacterData user,
      CharacterData target,
      CardTestData card,
      string trigger,
      string currentClashResult = "None"
    )
    {
        // effects 为空，说明这张卡没有需要执行的效果。
        if (card.effects == null || card.effects.Count == 0)
        {
            return;
        }

        // 遍历这张卡的所有效果。
        // 一张卡可以有多个效果，例如：先加 Buff，再减少 CD。
        foreach (CardEffectData effect in card.effects)
        {
            // 如果效果触发时机不等于当前时机，就跳过。
            // 例如当前是 OnPlay，就不执行 AfterDamage 的效果。
            if (!IsTriggerMatched(effect, trigger))
            {
                continue;
            }

            // 如果效果要求特定拼点结果，但当前结果不满足，也跳过。
            // 例如只在 ClashWin 时触发的效果，拼点失败时不会执行。
            if (!IsClashResultMatched(effect, currentClashResult))
            {
                continue;
            }

            // ApplyBuff = 添加 Buff / 状态。
            if (effect.effectType == CardEffectType.ApplyBuff)
            {
                // 先根据 effect.target 找到效果目标。
                // 例如 Self 表示自己，Target 表示卡牌目标。
                CharacterData effectTarget = GetEffectTarget(user, target, effect.target);

                if (effectTarget == null)
                {
                    Debug.LogWarning("找不到效果目标：" + effect.target);
                    continue;
                }

                ApplyBuffEffect(effectTarget, effect);
            }
            // ReduceCooldown = 减少 CD。
            else if (effect.effectType == CardEffectType.ReduceCooldown)
            {
                // 减少 CD 也需要先找到作用目标。
                // 例如减少自己的全部卡 CD，或者减少目标角色的某类卡 CD。
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
                // 未知效果类型暂时只打印警告，避免静默失败。
                Debug.LogWarning("暂未处理的效果类型：" + effect.effectType);
            }
        }
    }

    // GetEffectTarget = 获取效果目标
    // Get = 获取，EffectTarget = 效果目标。
    // targetType = 目标类型字符串，例如 Self / Target。
    static CharacterData GetEffectTarget(CharacterData user, CharacterData target, string targetType)
    {
        if (string.IsNullOrEmpty(targetType))
        {
            // 如果 JSON 没填 target，默认把效果作用到卡牌目标身上。
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

    // IsTriggerMatched = 检查触发阶段是否匹配
    // Trigger = 触发时机。
    // Matched = 匹配。
    // effect = 单个卡牌效果数据。
    // currentTrigger = 当前正在处理的触发时机。
    static bool IsTriggerMatched(CardEffectData effect, string currentTrigger)
    {
        if (effect == null)
        {
            // 没有效果数据，肯定不能匹配。
            return false;
        }

        if (string.IsNullOrEmpty(effect.trigger))
        {
            // 效果自己没有写触发时机，暂时不执行。
            return false;
        }

        if (string.IsNullOrEmpty(currentTrigger))
        {
            // 当前系统没有传入触发时机，也不能匹配。
            return false;
        }

        // 正常完全匹配
        if (effect.trigger == currentTrigger)
        {
            return true;
        }

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

    // IsClashResultMatched = 检查拼点结果是否匹配
    // Clash = 拼点，Result = 结果，Matched = 匹配。
    // 有些效果只允许在拼点胜利 / 失败时触发。
    static bool IsClashResultMatched(CardEffectData effect, string currentClashResult)
    {
        if (effect == null)
        {
            // 没有效果数据，肯定不能匹配。
            return false;
        }

        // 没填 requireClashResult，就代表不限制
        if (string.IsNullOrEmpty(effect.requireClashResult))
        {
            return true;
        }

        // Any 也代表不限制
        if (effect.requireClashResult == ClashResult.Any)
        {
            return true;
        }

        if (string.IsNullOrEmpty(currentClashResult))
        {
            // 当前没有拼点结果时，按 None 处理。
            currentClashResult = ClashResult.None;
        }

        return effect.requireClashResult == currentClashResult;
    }

    // ApplyBuffEffect = 执行添加 Buff 的效果
    // Apply = 应用，BuffEffect = Buff 效果。
    // effectTarget = 被添加 Buff 的角色。
    // effect = 单个效果数据，里面包含 buffType、stack、duration 等字段。
    static void ApplyBuffEffect(CharacterData effectTarget, CardEffectData effect)
    {
        string applyTiming = effect.applyTiming;

        if (string.IsNullOrEmpty(applyTiming))
        {
            // 如果没有填写生效时机，默认立即生效。
            applyTiming = "Immediate";
        }

        if (applyTiming == "Delayed")
        {
            // Delayed = 延迟生效。
            // 这里不会立刻添加到 buffs，而是放进 pendingBuffs 等回合处理。
            int delayTurns = effect.delayTurns;
            int applyTimes = effect.applyTimes;
            int intervalTurns = effect.intervalTurns;

            if (delayTurns <= 0)
            {
                // delayTurns = 延迟多少回合后生效。
                // 没填或填错时，默认延迟 1 回合。
                delayTurns = 1;
            }

            if (applyTimes <= 0)
            {
                // applyTimes = 总共生效多少次。
                // 没填或填错时，默认生效 1 次。
                applyTimes = 1;
            }

            if (intervalTurns <= 0)
            {
                // intervalTurns = 多次生效之间间隔多少回合。
                // 没填或填错时，默认间隔 1 回合。
                intervalTurns = 1;
            }

            // 把延迟 Buff 交给角色保存，后续由回合处理器推动它生效。
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
            // Immediate = 立即生效。
            // 直接把 Buff 加到角色当前 buffs 列表里。
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
    // Reduce = 减少，Cooldown = 冷却。
    // effectTarget = 被减少 CD 的角色。
    // effect = 单个效果数据，里面包含减少数量和减少范围。
    static void ApplyReduceCooldownEffect(CharacterData effectTarget, CardEffectData effect)
    {
        // cooldownAmount = 明确填写的 CD 减少数量。
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
            // 如果没填减少范围，默认减少全部卡牌 CD。
            cooldownTarget = CooldownTargetType.All;
        }

        if (cooldownTarget == CooldownTargetType.All)
        {
            // All = 全部卡牌。
            BattleCardManager.ReduceAllCooldowns(effectTarget, amount);

            if (BattleDebugSettings.ShowDetailBattleLog)
            {
                Debug.Log(effectTarget.characterName + " 的全部卡牌 CD -" + amount);
            }

            return;
        }

        if (cooldownTarget == CooldownTargetType.CardType)
        {
            // CardType = 指定卡牌类型。
            // 例如只减少 Attack 类型卡牌的 CD。
            BattleCardManager.ReduceCooldownsByCardType(effectTarget, effect.targetCardType, amount);

            if (BattleDebugSettings.ShowDetailBattleLog)
            {
                Debug.Log(effectTarget.characterName + " 的 " + effect.targetCardType + " 类型卡牌 CD -" + amount);
            }

            return;
        }

        if (cooldownTarget == CooldownTargetType.Rarity)
        {
            // Rarity = 指定品质。
            // 例如只减少 Blue 品质卡牌的 CD。
            BattleCardManager.ReduceCooldownsByRarity(effectTarget, effect.targetRarity, amount);

            if (BattleDebugSettings.ShowDetailBattleLog)
            {
                Debug.Log(effectTarget.characterName + " 的 " + effect.targetRarity + " 品质卡牌 CD -" + amount);
            }

            return;
        }

        if (cooldownTarget == CooldownTargetType.CardID)
        {
            // CardID = 指定卡牌 ID。
            // 同一个 cardID 可能有多个复制品，这里由 BattleCardManager 统一处理。
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
