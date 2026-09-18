using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// CharacterData owns mutable combat state, including one canonical BuffData
// for each active buffID.
public class CharacterData
{
    private static int nextGeneratedRuntimeUnitID = 1;

    public string characterName;
    public string runtimeUnitID { get; private set; }
    public int maxHP;
    public int currentHP;
    private bool defeated;
    private int legacyCurrentGuilt;
    private BattleRuntimeState sharedGuiltRuntimeState;

    public int currentGuilt
    {
        get => sharedGuiltRuntimeState != null
            ? sharedGuiltRuntimeState.currentGuilt
            : legacyCurrentGuilt;
        set
        {
            if (sharedGuiltRuntimeState != null)
            {
                sharedGuiltRuntimeState.currentGuilt = Mathf.Max(0, value);
                return;
            }
            legacyCurrentGuilt = Mathf.Max(0, value);
        }
    }

    public int minSpeed;
    public int maxSpeed;
    public int turnSpeed;
    public List<BuffData> buffs = new List<BuffData>();
    public List<PendingBuffData> pendingBuffs = new List<PendingBuffData>();
    public List<BattleCardState> battleCards = new List<BattleCardState>();

    private bool angerMechanicEnabled;
    public bool IsAngerMechanicEnabled => angerMechanicEnabled;

    public readonly BattlePendingState battlePending = new BattlePendingState();

    public void SetAngerMechanicEnabledForBattle(bool enabled)
    {
        angerMechanicEnabled = enabled;
    }

    public CharacterData(string name, int hp, int characterMinSpeed, int characterMaxSpeed)
        : this(name, hp, characterMinSpeed, characterMaxSpeed, null)
    {
    }

    public CharacterData(
        string name,
        int hp,
        int characterMinSpeed,
        int characterMaxSpeed,
        string explicitRuntimeUnitID
    )
    {
        characterName = name;
        runtimeUnitID = string.IsNullOrEmpty(explicitRuntimeUnitID)
            ? "runtime_unit_" + nextGeneratedRuntimeUnitID++
            : explicitRuntimeUnitID;
        maxHP = hp;
        currentHP = hp;
        minSpeed = characterMinSpeed;
        maxSpeed = Mathf.Max(characterMinSpeed, characterMaxSpeed);
        turnSpeed = minSpeed;
    }

    internal BattleRuntimeState GetSharedGuiltRuntimeState() => sharedGuiltRuntimeState;

    internal void BindSharedGuiltRuntimeState(BattleRuntimeState runtimeState)
    {
        sharedGuiltRuntimeState = runtimeState;
    }

    internal void UnbindSharedGuiltRuntimeState(BattleRuntimeState runtimeState)
    {
        if (object.ReferenceEquals(sharedGuiltRuntimeState, runtimeState))
        {
            sharedGuiltRuntimeState = null;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHP = Mathf.Max(0, currentHP - damage);
        Debug.Log(characterName + " 受到 " + damage + " 点伤害，剩余 HP：" + currentHP);
    }

    public bool IsDead() => currentHP <= 0;
    public bool IsDefeated() => defeated;
    public void MarkDefeated() => defeated = true;

    public void RollTurnSpeed()
    {
        turnSpeed = UnityEngine.Random.Range(minSpeed, maxSpeed + 1);
        if (BattleDebugSettings.ShowSpeedLog)
        {
            Debug.Log(characterName + " 本回合速度投掷：" + turnSpeed +
                "（速度范围：" + minSpeed + "-" + maxSpeed + "）");
        }
    }

    public int GetCurrentSpeed()
    {
        int currentSpeed = turnSpeed + Mathf.RoundToInt(GetBuffFlatModifier("Speed"));
        return Mathf.Max(0, currentSpeed);
    }

    public void AddBuff(string buffID, int stackDelta, int? intensityDelta = null)
    {
        if (string.IsNullOrEmpty(buffID) ||
            (stackDelta == 0 && !intensityDelta.HasValue))
        {
            return;
        }

        BuffDefinitionData definition;
        BuffDefinitionLoader.TryGetDefinition(buffID, out definition);
        BuffData state = FindBuffState(buffID);

        if (state == null)
        {
            int initialIntensity = definition != null ? definition.defaultIntensity : 0;
            state = new BuffData(buffID, 0, ClampIntensity(initialIntensity, definition));
            buffs.Add(state);
        }

        state.stack = ClampStacks(state.stack + stackDelta, definition);
        if (intensityDelta.HasValue)
        {
            state.intensity = ClampIntensity(state.intensity + intensityDelta.Value, definition);
        }

        RemoveIfZero(state, definition);

        if (BattleDebugSettings.ShowBuffLog)
        {
            Debug.Log(characterName + " 获得 Buff：" + buffID +
                "，层数变化 " + stackDelta + "，当前层数 " + GetBuffStack(buffID));
        }
    }

    public bool EnsureBuffState(string buffID)
    {
        if (string.IsNullOrEmpty(buffID))
        {
            return false;
        }

        BuffData existingState = FindBuffState(buffID);
        if (existingState != null)
        {
            return true;
        }

        BuffDefinitionData definition;
        if (!BuffDefinitionLoader.TryGetDefinition(buffID, out definition) ||
            definition == null)
        {
            return false;
        }

        BuffData state = new BuffData(
            buffID,
            0,
            ClampIntensity(definition.defaultIntensity, definition)
        );
        buffs.Add(state);
        RemoveIfZero(state, definition);
        return FindBuffState(buffID) != null;
    }

    public void SetInitialBuffState(string buffID, int stacks, int intensity = 0)
    {
        if (string.IsNullOrEmpty(buffID) || stacks < 0)
        {
            return;
        }

        BuffDefinitionData definition;
        BuffDefinitionLoader.TryGetDefinition(buffID, out definition);
        BuffData state = FindBuffState(buffID);
        int initialIntensity = intensity != 0 || definition == null
            ? intensity
            : definition.defaultIntensity;
        if (state == null)
        {
            state = new BuffData(buffID, stacks, ClampIntensity(initialIntensity, definition));
            buffs.Add(state);
        }
        else
        {
            state.stack = ClampStacks(stacks, definition);
            state.intensity = ClampIntensity(initialIntensity, definition);
        }
        RemoveIfZero(state, definition);
    }

    public void AddPendingBuff(
        string buffID,
        int stackDelta,
        int delayTurns,
        int applyTimes = 1,
        int intervalTurns = 1,
        int? intensityDelta = null
    )
    {
        if (string.IsNullOrEmpty(buffID) ||
            (stackDelta == 0 && !intensityDelta.HasValue))
        {
            return;
        }

        pendingBuffs.Add(new PendingBuffData(
            buffID,
            stackDelta,
            Mathf.Max(0, delayTurns),
            Mathf.Max(1, applyTimes),
            Mathf.Max(1, intervalTurns),
            intensityDelta ?? 0,
            intensityDelta.HasValue
        ));
    }

    public void ApplyPendingBuffsAtTurnStart()
    {
        for (int index = pendingBuffs.Count - 1; index >= 0; index--)
        {
            PendingBuffData pending = pendingBuffs[index];
            if (pending == null)
            {
                pendingBuffs.RemoveAt(index);
                continue;
            }

            pending.delayTurns--;
            if (pending.delayTurns > 0)
            {
                continue;
            }

            AddBuff(
                pending.buffID,
                pending.stackDelta,
                pending.hasIntensityDelta ? (int?)pending.intensityDelta : null
            );

            pending.applyTimes--;
            if (pending.applyTimes <= 0)
            {
                pendingBuffs.RemoveAt(index);
            }
            else
            {
                pending.delayTurns = pending.intervalTurns;
            }
        }
    }

    public int GetBuffStack(string buffID)
    {
        BuffData state = FindBuffState(buffID);
        return state != null ? Mathf.Max(0, state.stack) : 0;
    }

    public int GetBuffIntensity(string buffID)
    {
        BuffData state = FindBuffState(buffID);
        return state != null ? state.intensity : 0;
    }

    public BuffData GetBuffState(string buffID)
    {
        BuffData state = FindBuffState(buffID);
        return state == null ? null : CloneBuffData(state);
    }

    public float GetBuffFlatModifier(string targetStat)
    {
        return GetBuffModifierByDefinition("FlatModifier", targetStat);
    }

    public float GetBuffPercentModifier(string targetStat)
    {
        return GetBuffModifierByDefinition("PercentModifier", targetStat);
    }

    float GetBuffModifierByDefinition(string effectType, string targetStat)
    {
        if (string.IsNullOrEmpty(effectType) || string.IsNullOrEmpty(targetStat))
        {
            return 0f;
        }

        float total = 0f;
        for (int index = 0; index < buffs.Count; index++)
        {
            BuffData state = buffs[index];
            BuffDefinitionData definition;
            if (state == null || state.stack <= 0 ||
                !BuffDefinitionLoader.TryGetDefinition(state.buffID, out definition) ||
                definition == null || definition.effectType != effectType ||
                definition.targetStat != targetStat)
            {
                continue;
            }

            // Modifiers read their explicit current intensity. Stacks only
            // express resource quantity, existence, or remaining charges;
            // they are never implicitly multiplied into effect strength.
            total += state.intensity;
        }
        return total;
    }

    public int GetPendingBuffStackNextTurn(string buffID)
    {
        int total = 0;
        for (int index = 0; index < pendingBuffs.Count; index++)
        {
            PendingBuffData pending = pendingBuffs[index];
            if (pending != null && pending.buffID == buffID && pending.delayTurns <= 1)
            {
                total += pending.stackDelta;
            }
        }
        return total;
    }

    public int ConsumeBuffsByRule(string consumeRule)
    {
        if (string.IsNullOrEmpty(consumeRule) || consumeRule == BuffConsumeRule.None)
        {
            return 0;
        }

        int consumed = 0;
        for (int index = buffs.Count - 1; index >= 0; index--)
        {
            BuffData state = buffs[index];
            BuffDefinitionData definition;
            if (state == null ||
                !BuffDefinitionLoader.TryGetDefinition(state.buffID, out definition) ||
                definition == null || definition.consumeRule != consumeRule)
            {
                continue;
            }
            consumed += ConsumeOneBuffStack(state.buffID);
        }
        return consumed;
    }

    public int ConsumeBuffStackByRule(string buffID, string consumeRule, int amount)
    {
        BuffDefinitionData definition;
        if (string.IsNullOrEmpty(buffID) || amount <= 0 ||
            !BuffDefinitionLoader.TryGetDefinition(buffID, out definition) ||
            definition == null || definition.consumeRule != consumeRule)
        {
            return 0;
        }
        return ConsumeBuffStacks(buffID, amount);
    }

    public int ConsumeBuffStacks(string buffID, int amount)
    {
        if (string.IsNullOrEmpty(buffID) || amount <= 0)
        {
            return 0;
        }

        BuffData state = FindBuffState(buffID);
        if (state == null)
        {
            return 0;
        }

        int consumed = Mathf.Min(Mathf.Max(0, state.stack), amount);
        state.stack -= consumed;
        BuffDefinitionData definition;
        BuffDefinitionLoader.TryGetDefinition(buffID, out definition);
        RemoveIfZero(state, definition);
        return consumed;
    }

    public int ConsumeOneBuffStack(string buffID)
    {
        return ConsumeBuffStacks(buffID, 1);
    }

    public bool TryConsumeBuffStackAsResource(string buffID, int amount, out int consumedAmount)
    {
        consumedAmount = ConsumeBuffStacks(buffID, amount);
        return amount <= 0 || consumedAmount == amount;
    }

    public int ClearBuff(string buffID)
    {
        if (string.IsNullOrEmpty(buffID))
        {
            return 0;
        }
        BuffData state = FindBuffState(buffID);
        if (state == null)
        {
            return 0;
        }
        buffs.Remove(state);
        return 1;
    }

    BuffData FindBuffState(string buffID)
    {
        if (string.IsNullOrEmpty(buffID))
        {
            return null;
        }
        for (int index = 0; index < buffs.Count; index++)
        {
            if (buffs[index] != null && buffs[index].buffID == buffID)
            {
                return buffs[index];
            }
        }
        return null;
    }

    static int ClampStacks(int value, BuffDefinitionData definition)
    {
        int clamped = Mathf.Max(0, value);
        if (definition != null && definition.maxStacks > 0)
        {
            clamped = Mathf.Min(clamped, definition.maxStacks);
        }
        return clamped;
    }

    static int ClampIntensity(int value, BuffDefinitionData definition)
    {
        if (definition != null && definition.maxIntensity > 0)
        {
            return Mathf.Min(value, definition.maxIntensity);
        }
        return value;
    }

    void RemoveIfZero(BuffData state, BuffDefinitionData definition)
    {
        if (state == null || state.stack > 0)
        {
            return;
        }
        if (definition == null || !definition.retainWhenZero)
        {
            buffs.Remove(state);
        }
    }

    static BuffData CloneBuffData(BuffData state)
    {
        return state == null ? null : new BuffData(state.buffID, state.stack, state.intensity);
    }

    public void PrintBuffs()
    {
        Debug.Log("===== " + characterName + " 当前状态列表 =====");
        for (int index = 0; index < buffs.Count; index++)
        {
            BuffData state = buffs[index];
            Debug.Log(state.buffID + " / 层数：" + state.stack +
                " / 强度：" + state.intensity);
        }
    }

    public void PrintPendingBuffs()
    {
        Debug.Log("===== " + characterName + " 当前待生效状态列表 =====");
        for (int index = 0; index < pendingBuffs.Count; index++)
        {
            PendingBuffData pending = pendingBuffs[index];
            Debug.Log(pending.buffID + " / 层数变化：" + pending.stackDelta +
                " / 延迟回合：" + pending.delayTurns +
                " / 生效次数：" + pending.applyTimes);
        }
    }
}
