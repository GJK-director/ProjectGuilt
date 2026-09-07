// 脚本中文说明：卡牌效果执行器。负责按照触发时机执行卡牌 effects，例如添加 Buff 或减少 CD。
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// CardEffectExecutor = 卡牌效果执行器
//他会根据别人传进来的标签是否匹配来执行相应的效果
// Effect = 效果，Executor = 执行器。
// 专门负责执行卡牌 JSON 里 effects 列表中的效果。
// 注意：这个脚本只负责“效果怎么执行”，不负责判断卡牌能不能使用，也不负责拼点胜负。
public sealed class BattleRuleEvaluationContext
{
    public BattleEventContext battleEvent;
    public CharacterData user;
    public CharacterData target;
    public BattleCardState sourceCardState;
    public BattleCardState candidateCardState;
    public BattleCardState opponentCardState;
    public BattleRuntimeInteraction runtimeInteraction;
    public BattleImpact impact;
    public BattleClashResourceSnapshot resourceSnapshot;
    public string timing;
    public string clashResult;
    public CardTestData candidateCardData;
}

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
        BattleEventContext context = new BattleEventContext(trigger)
            .SetUserAndTarget(user, target)
            .SetCardData(card)
            .SetClashResult(currentClashResult);
        HandleEvent(context);
    }

    public static void HandleEvent(BattleEventContext context)
    {
        BattleRuleEvaluationContext evaluation = CreateEvaluationContext(context);
        CardTestData card = evaluation != null
            ? evaluation.candidateCardData
            : null;
        if (card == null || card.effects == null || card.effects.Count == 0)
        {
            return;
        }

        foreach (CardEffectData effect in card.effects)
        {
            if (!IsTriggerMatched(effect, evaluation.timing) ||
                !IsClashResultMatched(effect, evaluation.clashResult) ||
                !AreConditionsMet(effect.conditions, evaluation) ||
                !AreFiltersMatched(effect.filters, evaluation))
            {
                continue;
            }

            int resolvedValue = 0;
            bool hasResolvedValue = effect.formula != null;
            if (hasResolvedValue && !TryEvaluateFormula(
                effect.formula,
                evaluation,
                out resolvedValue
            ))
            {
                Debug.LogWarning("卡牌效果公式无法计算，跳过效果：" +
                    (card.cardName ?? card.cardID));
                continue;
            }

            CharacterData effectTarget = GetEffectTarget(
                evaluation.user,
                evaluation.target,
                effect.target
            );
            if (effectTarget == null)
            {
                Debug.LogWarning("找不到效果目标：" + effect.target);
                continue;
            }

            ExecuteEffect(effectTarget, effect, hasResolvedValue, resolvedValue);
        }
    }

    internal static BattleRuleEvaluationContext CreateEvaluationContext(
        BattleEventContext context
    )
    {
        if (context == null)
        {
            return null;
        }

        BattleRuleEvaluationContext evaluation = new BattleRuleEvaluationContext
        {
            battleEvent = context,
            user = context.user,
            target = context.target,
            sourceCardState = context.cardState,
            candidateCardState = context.cardState,
            runtimeInteraction = context.runtimeInteraction,
            impact = context.impact,
            resourceSnapshot = context.resourceSnapshot,
            timing = context.timing,
            clashResult = string.IsNullOrEmpty(context.clashResult)
                ? ClashResult.None
                : context.clashResult,
            candidateCardData = context.cardState != null
                ? context.cardState.cardData
                : context.cardData
        };
        evaluation.opponentCardState = FindOpponentCardState(evaluation);
        return evaluation;
    }

    static BattleCardState FindOpponentCardState(
        BattleRuleEvaluationContext context
    )
    {
        if (context == null || context.runtimeInteraction == null ||
            context.sourceCardState == null)
        {
            return null;
        }

        BattleExecutionAction sideA = context.runtimeInteraction.sideA;
        BattleExecutionAction sideB = context.runtimeInteraction.sideB;
        if (sideA != null && object.ReferenceEquals(
            sideA.cardState,
            context.sourceCardState
        ))
        {
            return sideB != null ? sideB.cardState : null;
        }
        if (sideB != null && object.ReferenceEquals(
            sideB.cardState,
            context.sourceCardState
        ))
        {
            return sideA != null ? sideA.cardState : null;
        }
        return null;
    }

    static void ExecuteEffect(
        CharacterData effectTarget,
        CardEffectData effect,
        bool hasResolvedValue,
        int resolvedValue
    )
    {
        if (effect.effectType == CardEffectType.ApplyBuff)
        {
            ApplyBuffEffect(effectTarget, effect, hasResolvedValue
                ? resolvedValue
                : (int?)null);
        }
        else if (effect.effectType == CardEffectType.ReduceCooldown)
        {
            ApplyReduceCooldownEffect(effectTarget, effect, hasResolvedValue
                ? resolvedValue
                : (int?)null);
        }
        else if (effect.effectType == CardEffectType.EnableAngerMechanic)
        {
            effectTarget.SetAngerMechanicEnabledForBattle(true);
        }
        else if (effect.effectType == CardEffectType.ActivateModification)
        {
            BattleModificationRules.Activate(effectTarget);
        }
        else if (effect.effectType == CardEffectType.ActivateConservation)
        {
            BattleConservationRules.Activate(effectTarget);
        }
        else
        {
            Debug.LogWarning("暂未处理的效果类型：" + effect.effectType);
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

    internal static bool AreConditionsMet(
        CardEffectConditionData[] conditions,
        BattleRuleEvaluationContext context
    )
    {
        if (conditions == null || conditions.Length == 0)
        {
            return true;
        }

        foreach (CardEffectConditionData condition in conditions)
        {
            if (!EvaluateCondition(condition, context))
            {
                return false;
            }
        }
        return true;
    }

    static bool EvaluateCondition(
        CardEffectConditionData condition,
        BattleRuleEvaluationContext context
    )
    {
        if (condition == null || context == null)
        {
            return false;
        }

        if (condition.conditionType ==
            CardEffectConditionType.OpponentCardTypeIs)
        {
            return context.opponentCardState != null &&
                context.opponentCardState.cardData != null &&
                context.opponentCardState.cardData.cardType == condition.cardType;
        }

        if (condition.conditionType ==
            CardEffectConditionType.ResourceStackAtLeast)
        {
            CharacterData resourceOwner = GetEffectTarget(
                context.user,
                context.target,
                condition.target
            );
            return resourceOwner != null &&
                !string.IsNullOrEmpty(condition.resourceID) &&
                resourceOwner.GetBuffStack(condition.resourceID) >=
                    condition.value;
        }

        if (condition.conditionType ==
            CardEffectConditionType.ClashResultIs)
        {
            return !string.IsNullOrEmpty(condition.clashResult) &&
                context.clashResult == condition.clashResult;
        }

        Debug.LogWarning("未知的卡牌效果条件：" + condition.conditionType);
        return false;
    }

    internal static bool AreFiltersMatched(
        CardEffectFilterData[] filters,
        BattleRuleEvaluationContext context
    )
    {
        if (filters == null || filters.Length == 0)
        {
            return true;
        }

        foreach (CardEffectFilterData filter in filters)
        {
            if (!EvaluateFilter(filter, context))
            {
                return false;
            }
        }
        return true;
    }

    static bool EvaluateFilter(
        CardEffectFilterData filter,
        BattleRuleEvaluationContext context
    )
    {
        CardTestData candidate = context != null
            ? context.candidateCardData
            : null;
        if (filter == null || candidate == null)
        {
            return false;
        }

        if (filter.filterType == CardEffectFilterType.CardTypeIs)
        {
            return candidate.cardType == filter.cardType;
        }

        if (filter.filterType == CardEffectFilterType.CardConsumesResource)
        {
            return CardConsumesResource(candidate, filter.resourceID);
        }

        if (filter.filterType == CardEffectFilterType.EligibleShootingAttack)
        {
            return IsEligibleShootingAttack(candidate);
        }

        Debug.LogWarning("未知的卡牌效果过滤器：" + filter.filterType);
        return false;
    }

    internal static bool CardConsumesResource(
        CardTestData card,
        string resourceID
    )
    {
        if (card == null || string.IsNullOrEmpty(resourceID))
        {
            return false;
        }

        if (ConsumesResource(card.resourceRule, resourceID))
        {
            return true;
        }

        if (card.resourceRules != null)
        {
            foreach (CardResourceRuleData rule in card.resourceRules)
            {
                if (ConsumesResource(rule, resourceID))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // 正式射击候选：攻击、射击投递方式，并且真实消耗 Bullet。
    internal static bool IsEligibleShootingAttack(CardTestData card)
    {
        return card != null && card.cardType == CardType.Attack &&
            (card.IsLongRangeShoot() || card.IsCloseRangeShoot()) &&
            CardConsumesResource(card, BattleResourceID.Bullet);
    }

    static bool ConsumesResource(CardResourceRuleData rule, string resourceID)
    {
        return rule != null && rule.resourceType == "BuffStack" &&
            rule.resourceID == resourceID &&
            (rule.consumeAmountOnSuccess > 0 ||
                rule.consumeAllCapturedOnSuccess);
    }

    internal static bool TryEvaluateFormula(
        CardEffectFormulaData formula,
        BattleRuleEvaluationContext context,
        out int value
    )
    {
        value = 0;
        if (formula == null || context == null)
        {
            return false;
        }

        int input;
        if (formula.inputType == CardEffectFormulaInputType.CurrentAnger)
        {
            if (context.user == null)
            {
                return false;
            }
            input = BattleAngerRules.GetAnger(context.user);
        }
        else if (formula.inputType == CardEffectFormulaInputType.ResourceSnapshot)
        {
            if (context.resourceSnapshot == null ||
                string.IsNullOrEmpty(formula.resourceID) ||
                context.resourceSnapshot.resourceID != formula.resourceID)
            {
                return false;
            }
            input = context.resourceSnapshot.capturedStack;
        }
        else
        {
            Debug.LogWarning("未知的卡牌效果公式输入：" + formula.inputType);
            return false;
        }

        if (formula.lookup != null && formula.lookup.Length > 0)
        {
            foreach (CardEffectFormulaLookupEntryData entry in formula.lookup)
            {
                if (entry != null && entry.input == input)
                {
                    value = entry.value;
                    return true;
                }
            }
            return false;
        }

        value = input * formula.multiplier + formula.additive;
        return true;
    }

    // ApplyBuffEffect = 执行添加 Buff 的效果
    // Apply = 应用，BuffEffect = Buff 效果。
    // effectTarget = 被添加 Buff 的角色。
    // effect = 单个效果数据，里面包含 buffType、stack、duration 等字段。
    static void ApplyBuffEffect(
        CharacterData effectTarget,
        CardEffectData effect,
        int? resolvedStack = null
    )
    {
        string applyTiming = effect.applyTiming;

        if (string.IsNullOrEmpty(applyTiming))
        {
            // 如果没有填写生效时机，默认立即生效。
            applyTiming = "Immediate";
        }

        string buffID = effect.buffType;

        if (string.IsNullOrEmpty(buffID))
        {
            Debug.LogError("ApplyBuff 失败：buffType 为空");
            return;
        }

        int stack = resolvedStack ?? effect.stack;
        int duration = effect.duration;

        if (stack <= 0)
        {
            Debug.LogError("ApplyBuff 失败：" + buffID + " 层数无效：" + stack);
            return;
        }

        BuffDefinitionData definition;
        bool hasDefinition = BuffDefinitionLoader.TryGetDefinition(buffID, out definition);

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

            if (hasDefinition)
            {
                effectTarget.AddPendingBuff(buffID, stack, duration, delayTurns, applyTimes, intervalTurns);
                return;
            }

            if (HasCompleteLegacyBuffFields(effect))
            {
                Debug.LogWarning("Buff定义不存在，使用旧卡牌字段兼容：" + buffID);
                effectTarget.AddPendingBuff(
                    buffID,
                    effect.buffName,
                    effect.buffCategory,
                    stack,
                    duration,
                    effect.checkTiming,
                    effect.expireRule,
                    delayTurns,
                    applyTimes,
                    intervalTurns
                );
                return;
            }

            Debug.LogError("ApplyBuff 失败：找不到Buff定义且legacy字段不完整：" + buffID);
            return;
        }

        if (hasDefinition)
        {
            effectTarget.AddBuff(buffID, stack, duration);
            return;
        }

        if (HasCompleteLegacyBuffFields(effect))
        {
            Debug.LogWarning("Buff定义不存在，使用旧卡牌字段兼容：" + buffID);
            effectTarget.AddBuff(
                buffID,
                effect.buffName,
                effect.buffCategory,
                stack,
                duration,
                effect.checkTiming,
                effect.expireRule
            );
            return;
        }

        Debug.LogError("ApplyBuff 失败：找不到Buff定义且legacy字段不完整：" + buffID);
    }

    static bool HasCompleteLegacyBuffFields(CardEffectData effect)
    {
        return effect != null &&
            !string.IsNullOrEmpty(effect.buffName) &&
            !string.IsNullOrEmpty(effect.buffCategory) &&
            !string.IsNullOrEmpty(effect.checkTiming) &&
            !string.IsNullOrEmpty(effect.expireRule);
    }

    // ApplyReduceCooldownEffect = 执行减少冷却效果
    // Reduce = 减少，Cooldown = 冷却。
    // effectTarget = 被减少 CD 的角色。
    // effect = 单个效果数据，里面包含减少数量和减少范围。
    static void ApplyReduceCooldownEffect(
        CharacterData effectTarget,
        CardEffectData effect,
        int? resolvedCooldownAmount = null
    )
    {
        // cooldownAmount = 明确填写的 CD 减少数量。
        int amount = resolvedCooldownAmount ?? effect.cooldownAmount;

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

public static class BuffDefinitionLoader
{
    const string ResourcePath = "Data/Buffs/BuffDefinitions";

    static List<BuffDefinitionData> cachedDefinitions;
    static Dictionary<string, BuffDefinitionData> cachedDefinitionByID;

    public static List<BuffDefinitionData> LoadBuffDefinitions()
    {
        if (cachedDefinitions != null && cachedDefinitionByID != null)
        {
            return cachedDefinitions;
        }

        cachedDefinitions = new List<BuffDefinitionData>();
        cachedDefinitionByID = new Dictionary<string, BuffDefinitionData>();

        TextAsset jsonFile = Resources.Load<TextAsset>(ResourcePath);

        if (jsonFile == null)
        {
            Debug.LogError("没有找到 BuffDefinitions.json，请检查路径：Assets/Resources/Data/Buffs/BuffDefinitions.json");
            return cachedDefinitions;
        }

        string jsonText = Encoding.UTF8.GetString(jsonFile.bytes);
        List<BuffDefinitionData> definitions = JsonConvert.DeserializeObject<List<BuffDefinitionData>>(jsonText);

        if (definitions == null)
        {
            Debug.LogError("BuffDefinitions.json 解析失败，definitions 为空");
            return cachedDefinitions;
        }

        foreach (BuffDefinitionData definition in definitions)
        {
            if (definition == null)
            {
                Debug.LogError("BuffDefinitions 中存在空定义");
                continue;
            }

            if (string.IsNullOrEmpty(definition.buffID))
            {
                Debug.LogError("BuffDefinitions 中存在 buffID 为空的定义");
                continue;
            }

            if (cachedDefinitionByID.ContainsKey(definition.buffID))
            {
                Debug.LogError("BuffDefinitions 中发现重复 buffID：" + definition.buffID + "，已忽略重复定义");
                continue;
            }

            NormalizeDefinition(definition);
            cachedDefinitions.Add(definition);
            cachedDefinitionByID.Add(definition.buffID, definition);
        }

        Debug.Log("成功读取 Buff 定义，共 " + cachedDefinitions.Count + " 种");

        return cachedDefinitions;
    }

    public static bool TryGetDefinition(string buffID, out BuffDefinitionData definition)
    {
        definition = null;

        if (string.IsNullOrEmpty(buffID))
        {
            return false;
        }

        LoadBuffDefinitions();

        if (cachedDefinitionByID == null)
        {
            return false;
        }

        return cachedDefinitionByID.TryGetValue(buffID, out definition);
    }

    public static BuffDefinitionData GetDefinition(string buffID)
    {
        BuffDefinitionData definition;

        if (TryGetDefinition(buffID, out definition))
        {
            return definition;
        }

        Debug.LogWarning("找不到 Buff 定义：" + buffID);
        return null;
    }

    internal static void ClearCacheForTest()
    {
        cachedDefinitions = null;
        cachedDefinitionByID = null;
    }

    static void NormalizeDefinition(BuffDefinitionData definition)
    {
        if (string.IsNullOrEmpty(definition.consumeRule))
        {
            definition.consumeRule = "None";
        }

        if (definition.description == null)
        {
            definition.description = "";
        }
    }
}
