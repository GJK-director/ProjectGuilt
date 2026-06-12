// 脚本中文说明：角色数据。负责保存角色血量、速度、Buff、待生效 Buff、战斗卡牌和负罪感。
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// CharacterData = 角色数据
// 用来保存角色的血量、速度、身上的状态等信息
public class CharacterData
{
    //角色名称characterName
    public string characterName;
    
    public int maxHP;
    //currentHP当前hp
    public int currentHP;
    // currentGuilt = 当前负罪感
    // 负罪感从 0 开始累计增加，不是消耗资源
    public int currentGuilt = 0;

    // minSpeed = 最低速度
    public int minSpeed;

    // maxSpeed = 最高速度
    public int maxSpeed;

    // turnSpeed = 本回合随机出来的速度
    // 每个回合开始时随机一次，本回合内保持不变
    public int turnSpeed;

    // 当前角色身上的所有状态
    public List<BuffData> buffs = new List<BuffData>();

    // 当前角色身上的待生效状态
    // 这些状态还没有真正生效，到未来回合开始时才会添加到 buffs 里
    public List<PendingBuffData> pendingBuffs = new List<PendingBuffData>();
    // 当前角色在本场战斗中持有的卡牌状态
    // 每张复制品都是一个 BattleCardState
    public List<BattleCardState> battleCards = new List<BattleCardState>();

    // 旧版构造函数：只有一个固定速度
    // 为了兼容旧代码，保留它
    // 如果只传一个速度，就让最低速度和最高速度都等于它

    // 新版构造函数：速度范围
    public CharacterData(string name, int hp, int characterMinSpeed, int characterMaxSpeed)
    {
        characterName = name;
        maxHP = hp;
        currentHP = hp;

        minSpeed = characterMinSpeed;
        maxSpeed = characterMaxSpeed;

        // 防止最高速度小于最低速度
        if (maxSpeed < minSpeed)
        {
            maxSpeed = minSpeed;
        }

        // 初始先给一个最低速度
        // 真正战斗时会在回合开始 RollTurnSpeed  这是一个安全措施
        turnSpeed = minSpeed;
    }

    // 受到伤害
    public void TakeDamage(int damage)
    {
        currentHP = currentHP - damage;

        if (currentHP < 0)
        {
            currentHP = 0;
        }

        Debug.Log(characterName + " 受到 " + damage + " 点伤害，剩余 HP：" + currentHP);
    }

    // 是否死亡
    public bool IsDead()
    {
        return currentHP <= 0;
    }

    // RollTurnSpeed = 投掷本回合速度
    // 每个回合开始时调用一次
    public void RollTurnSpeed()
    {
        turnSpeed = Random.Range(minSpeed, maxSpeed + 1);

        if (BattleDebugSettings.ShowSpeedLog)
        {
            Debug.Log(
                characterName +
                " 本回合速度投掷：" +
                turnSpeed +
                "（速度范围：" + minSpeed + "-" + maxSpeed + "）"
            );
        }
    }

    // GetCurrentSpeed = 计算当前速度
    // 当前速度 = 本回合速度 + SpeedUp层数 - SpeedDown层数
    public int GetCurrentSpeed()
    {
        int speedModifier = Mathf.RoundToInt(GetBuffFlatModifier("Speed"));
        int currentSpeed = turnSpeed + speedModifier;

        // 最低速度为 0
        if (currentSpeed < 0)
        {
            currentSpeed = 0;
        }

        return currentSpeed;
    }

    // ================================
    // 添加状态：完整版
    // ================================
    public void AddBuff(
        string buffID,
        string buffName,
        //buffCategory buff类别
        string buffCategory,
        //stack层数
        int stack,
        //duration持续回合数
        int duration,
        //checkTiming检查时机
        string checkTiming,
        //expireRule消失规则
        string expireRule
    )
    {
        if (string.IsNullOrEmpty(buffID))
        {
            Debug.LogWarning(characterName + " 添加 Buff 失败：buffID 为空");
            return;
        }

        if (stack <= 0)
        {
            Debug.LogWarning(characterName + " 添加 Buff 失败：" + buffID + " 层数无效：" + stack);
            return;
        }

        BuffDefinitionData definition;

        if (BuffDefinitionLoader.TryGetDefinition(buffID, out definition))
        {
            if (string.IsNullOrEmpty(buffName))
            {
                buffName = definition.buffName;
            }

            if (string.IsNullOrEmpty(buffCategory))
            {
                buffCategory = definition.buffCategory;
            }

            if (string.IsNullOrEmpty(checkTiming))
            {
                checkTiming = definition.defaultCheckTiming;
            }

            if (string.IsNullOrEmpty(expireRule))
            {
                expireRule = definition.defaultExpireRule;
            }
        }

        if (string.IsNullOrEmpty(buffName))
        {
            buffName = buffID;
        }

        if (string.IsNullOrEmpty(buffCategory))
        {
            buffCategory = GuessBuffCategory(buffID);
        }

        if (string.IsNullOrEmpty(checkTiming))
        {
            checkTiming = GuessCheckTiming(buffID);
        }

        if (string.IsNullOrEmpty(expireRule))
        {
            expireRule = GuessExpireRule(buffID);
        }

        BuffData sameBatch = FindSameActiveBuffBatch(
            buffID,
            buffCategory,
            duration,
            checkTiming,
            expireRule
        );

        if (sameBatch != null)
        {
            int oldStack = sameBatch.stack;
            sameBatch.stack += stack;

            // 方案2：给不同的强壮添加标签。
            if (BattleDebugSettings.ShowBuffLog)
            {
                Debug.Log(
                    "合并Buff批次：" +
                    buffID +
                    "，持续" +
                    duration +
                    "回合，层数 " +
                    oldStack +
                    " -> " +
                    sameBatch.stack
                );
            }

            return;
        }

        BuffData newBuff = new BuffData(
            buffID,
            buffName,
            buffCategory,
            stack,
            duration,
            checkTiming,
            expireRule
        );

        buffs.Add(newBuff);

        if (BattleDebugSettings.ShowBuffLog)
        {
            Debug.Log(
                "新增Buff批次：" +
                buffID +
                "，层数" +
                stack +
                "，持续" +
                duration +
                "回合"
            );
        }
    }

    // AddPendingBuff = 添加一个待生效状态
    // 这个状态不会立刻进入 buffs
    // 而是先进入 pendingBuffs，等未来回合开始时再正式生效
    public void AddPendingBuff(
        string buffID,
        string buffName,
        string buffCategory,
        int stack,
        int duration,
        string checkTiming,
        string expireRule,
        int delayTurns,
        int applyTimes,
        int intervalTurns
    )
    {
        if (string.IsNullOrEmpty(buffID))
        {
            Debug.LogWarning(characterName + " 添加待生效 Buff 失败：buffID 为空");
            return;
        }

        if (stack <= 0)
        {
            Debug.LogWarning(characterName + " 添加待生效 Buff 失败：" + buffID + " 层数无效：" + stack);
            return;
        }

        BuffDefinitionData definition;

        if (BuffDefinitionLoader.TryGetDefinition(buffID, out definition))
        {
            if (string.IsNullOrEmpty(buffName))
            {
                buffName = definition.buffName;
            }

            if (string.IsNullOrEmpty(buffCategory))
            {
                buffCategory = definition.buffCategory;
            }

            if (string.IsNullOrEmpty(checkTiming))
            {
                checkTiming = definition.defaultCheckTiming;
            }

            if (string.IsNullOrEmpty(expireRule))
            {
                expireRule = definition.defaultExpireRule;
            }
        }

        if (string.IsNullOrEmpty(buffName))
        {
            buffName = buffID;
        }

        if (string.IsNullOrEmpty(buffCategory))
        {
            buffCategory = GuessBuffCategory(buffID);
        }

        if (string.IsNullOrEmpty(checkTiming))
        {
            checkTiming = GuessCheckTiming(buffID);
        }

        if (string.IsNullOrEmpty(expireRule))
        {
            expireRule = GuessExpireRule(buffID);
        }

        // 防止填写错误
        if (delayTurns < 0)
        {
            delayTurns = 0;
        }

        if (applyTimes <= 0)
        {
            applyTimes = 1;
        }

        if (intervalTurns <= 0)
        {
            intervalTurns = 1;
        }

        PendingBuffData sameSchedule = FindSamePendingBuffSchedule(
            buffID,
            buffCategory,
            duration,
            checkTiming,
            expireRule,
            delayTurns,
            applyTimes,
            intervalTurns
        );

        if (sameSchedule != null)
        {
            int oldStack = sameSchedule.stack;
            sameSchedule.stack += stack;

            if (BattleDebugSettings.ShowBuffLog)
            {
                Debug.Log(
                    "合并待生效Buff排期：" +
                    buffID +
                    "，延迟" +
                    delayTurns +
                    "回合，生效次数" +
                    applyTimes +
                    "，层数 " +
                    oldStack +
                    " -> " +
                    sameSchedule.stack
                );
            }

            return;
        }

        PendingBuffData newPendingBuff = new PendingBuffData(
            buffID,
            buffName,
            buffCategory,
            stack,
            duration,
            checkTiming,
            expireRule,
            delayTurns,
            applyTimes,
            intervalTurns
        );

        pendingBuffs.Add(newPendingBuff);

        if (BattleDebugSettings.ShowBuffLog)
        {
            Debug.Log(
                characterName + " 获得待生效状态：" +
                buffName +
                "，类型：" + buffCategory +
                "，层数：" + stack +
                "，持续：" + duration +
                "，延迟回合：" + delayTurns +
                "，生效次数：" + applyTimes +
                "，间隔回合：" + intervalTurns
            );
        }
    }

    // ApplyPendingBuffsAtTurnStart = 回合开始时处理待生效状态
    // 每到一个回合开始，待生效状态的 delayTurns 会减少
    // 如果 delayTurns 到 0，就正式添加 Buff
    public void ApplyPendingBuffsAtTurnStart()
    {
        if (BattleDebugSettings.ShowBuffLog)
        {
            Debug.Log("===== 回合开始，处理 " + characterName + " 的待生效状态 =====");
        }

        for (int i = pendingBuffs.Count - 1; i >= 0; i--)
        {
            PendingBuffData pendingBuff = pendingBuffs[i];

            // 每个回合开始，延迟回合数减少 1
            pendingBuff.delayTurns--;

            if (BattleDebugSettings.ShowBuffLog)
            {
                Debug.Log(
                    pendingBuff.buffName +
                    " 距离生效剩余回合：" +
                    pendingBuff.delayTurns
                );
            }

            // 如果还没到生效时间，就跳过
            if (pendingBuff.delayTurns > 0)
            {
                continue;
                // continue;是进入下一个循环，这里我们拥有多个待生效的buff，如果他的回合还没到，那就跳过他，检测下一个 
            }

            // 到时间了，正式添加 Buff
            AddBuff(
                pendingBuff.buffID,
                pendingBuff.buffName,
                pendingBuff.buffCategory,
                pendingBuff.stack,
                pendingBuff.duration,
                pendingBuff.checkTiming,
                pendingBuff.expireRule
            );

            // 生效次数减少
            pendingBuff.applyTimes--;
            //比如说有下两个回合生效的，生效次数就是2

            // 如果已经没有剩余生效次数，就移除这个待生效状态
            if (pendingBuff.applyTimes <= 0)
            {
                if (BattleDebugSettings.ShowBuffLog)
                {
                    Debug.Log(pendingBuff.buffName + " 的待生效任务完成，移除");
                }

                pendingBuffs.RemoveAt(i);
            }
            else
            {
                // 如果还要继续生效，就重新设置延迟
                pendingBuff.delayTurns = pendingBuff.intervalTurns;

                if (BattleDebugSettings.ShowBuffLog)
                {
                    Debug.Log(
                        pendingBuff.buffName +
                        " 还会继续生效，剩余次数：" +
                        pendingBuff.applyTimes +
                        "，下次间隔：" +
                        pendingBuff.intervalTurns
                    );
                }
            }
        }
    }

    // ================================
    // 添加状态：兼容旧代码的简化版
    // 例如：AddBuff("Strength", 1, 1)
    // ================================
    public void AddBuff(string buffID, int stack, int duration)
    {
        BuffDefinitionData definition;

        if (BuffDefinitionLoader.TryGetDefinition(buffID, out definition))
        {
            AddBuff(
                buffID,
                definition.buffName,
                definition.buffCategory,
                stack,
                duration,
                definition.defaultCheckTiming,
                definition.defaultExpireRule
            );

            return;
        }

        string buffName = buffID;
        string buffCategory = GuessBuffCategory(buffID);
        string checkTiming = GuessCheckTiming(buffID);
        string expireRule = GuessExpireRule(buffID);

        AddBuff(buffID, buffName, buffCategory, stack, duration, checkTiming, expireRule);
    }

    public void AddPendingBuff(
        string buffID,
        int stack,
        int duration,
        int delayTurns,
        int applyTimes,
        int intervalTurns
    )
    {
        BuffDefinitionData definition;

        if (BuffDefinitionLoader.TryGetDefinition(buffID, out definition))
        {
            AddPendingBuff(
                buffID,
                definition.buffName,
                definition.buffCategory,
                stack,
                duration,
                definition.defaultCheckTiming,
                definition.defaultExpireRule,
                delayTurns,
                applyTimes,
                intervalTurns
            );

            return;
        }

        AddPendingBuff(
            buffID,
            buffID,
            GuessBuffCategory(buffID),
            stack,
            duration,
            GuessCheckTiming(buffID),
            GuessExpireRule(buffID),
            delayTurns,
            applyTimes,
            intervalTurns
        );
    }

    BuffData FindSameActiveBuffBatch(
        string buffID,
        string buffCategory,
        int duration,
        string checkTiming,
        string expireRule
    )
    {
        foreach (BuffData buff in buffs)
        {
            if (buff == null)
            {
                continue;
            }

            if (buff.buffID == buffID &&
                buff.buffCategory == buffCategory &&
                buff.duration == duration &&
                buff.checkTiming == checkTiming &&
                buff.expireRule == expireRule)
            {
                return buff;
            }
        }

        return null;
    }

    PendingBuffData FindSamePendingBuffSchedule(
        string buffID,
        string buffCategory,
        int duration,
        string checkTiming,
        string expireRule,
        int delayTurns,
        int applyTimes,
        int intervalTurns
    )
    {
        foreach (PendingBuffData pendingBuff in pendingBuffs)
        {
            if (pendingBuff == null)
            {
                continue;
            }

            if (pendingBuff.buffID == buffID &&
                pendingBuff.buffCategory == buffCategory &&
                pendingBuff.duration == duration &&
                pendingBuff.checkTiming == checkTiming &&
                pendingBuff.expireRule == expireRule &&
                pendingBuff.delayTurns == delayTurns &&
                pendingBuff.applyTimes == applyTimes &&
                pendingBuff.intervalTurns == intervalTurns)
            {
                return pendingBuff;
            }
        }

        return null;
    }

    // 根据 buffID 简单猜测分类
    // 这只是临时辅助，正式项目后面可以改成从 JSON 读取
    string GuessBuffCategory(string buffID)
    {
        if (buffID == "Strength" || buffID == "SpeedUp" || buffID == "DamageUp" || buffID == "DamageReduction")
        {
            return "UpBuff";
        }

        if (buffID == "Weakness" || buffID == "SpeedDown" || buffID == "Vulnerable" || buffID == "DamageDown")
        {
            return "Debuff";
        }

        return "AbilityBuff";
    }

    // 根据 buffID 简单猜测检测阶段
    string GuessCheckTiming(string buffID)
    {
        if (buffID == "Strength" || buffID == "Weakness")
        {
            return "TurnEnd";
        }

        if (buffID == "Bullet")
        {
            return "None";
        }

        if (buffID == "NextClashPointUp")
        {
            return "ClashStart";
        }

        return "TurnEnd";
    }

    // 根据 buffID 简单猜测消失规则
    string GuessExpireRule(string buffID)
    {
        if (buffID == "Bullet")
        {
            return "Permanent";
        }

        if (buffID == "NextClashPointUp")
        {
            return "ConsumeOnTrigger";
        }
         //等buff接入json后才会处理的东西
        return "DurationDown";
    }
    
    // ================================
    // 获取某个状态的总层数
    // 例如 Strength x1 + Strength x2 = 3
    // ================================
    public int GetBuffStack(string buffID)
    {
        int totalStack = 0;

        foreach (BuffData buff in buffs)
        {
            if (buff.buffID == buffID)
            {
                totalStack = totalStack + buff.stack;
            }
        }
        //用于处理同名 Buff 的叠加。
        return totalStack;
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

        float totalModifier = 0f;

        foreach (BuffData buff in buffs)
        {
            if (buff == null || string.IsNullOrEmpty(buff.buffID))
            {
                continue;
            }

            BuffDefinitionData definition;

            if (!BuffDefinitionLoader.TryGetDefinition(buff.buffID, out definition) || definition == null)
            {
                continue;
            }

            if (definition.effectType != effectType)
            {
                continue;
            }

            if (definition.targetStat != targetStat)
            {
                continue;
            }

            totalModifier += buff.stack * definition.valuePerStack;
        }

        return totalModifier;
    }

    public int GetExpiringBuffStackAtTurnEnd(string buffID)
    {
        if (string.IsNullOrEmpty(buffID))
        {
            return 0;
        }

        int totalStack = 0;

        foreach (BuffData buff in buffs)
        {
            if (buff == null)
            {
                continue;
            }

            if (buff.buffID == buffID &&
                buff.checkTiming == BattleTiming.TurnEnd &&
                buff.expireRule == "DurationDown" &&
                buff.duration == 1)
            {
                totalStack += buff.stack;
            }
        }

        return totalStack;
    }

    public List<BuffData> GetActiveBuffBatches(string buffID)
    {
        List<BuffData> result = new List<BuffData>();

        foreach (BuffData buff in buffs)
        {
            if (buff == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(buffID) || buff.buffID == buffID)
            {
                result.Add(CloneBuffData(buff));
            }
        }

        return result;
    }

    public int GetPendingBuffStackNextTurn(string buffID)
    {
        if (string.IsNullOrEmpty(buffID))
        {
            return 0;
        }

        int totalStack = 0;

        foreach (PendingBuffData pendingBuff in pendingBuffs)
        {
            if (pendingBuff == null)
            {
                continue;
            }

            if (pendingBuff.buffID == buffID && pendingBuff.delayTurns <= 1)
            {
                totalStack += pendingBuff.stack;
            }
        }

        return totalStack;
    }

    public List<PendingBuffData> GetPendingBuffBatches(string buffID)
    {
        List<PendingBuffData> result = new List<PendingBuffData>();

        foreach (PendingBuffData pendingBuff in pendingBuffs)
        {
            if (pendingBuff == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(buffID) || pendingBuff.buffID == buffID)
            {
                result.Add(ClonePendingBuffData(pendingBuff));
            }
        }

        return result;
    }

    BuffData CloneBuffData(BuffData buff)
    {
        return new BuffData(
            buff.buffID,
            buff.buffName,
            buff.buffCategory,
            buff.stack,
            buff.duration,
            buff.checkTiming,
            buff.expireRule
        );
    }

    PendingBuffData ClonePendingBuffData(PendingBuffData pendingBuff)
    {
        return new PendingBuffData(
            pendingBuff.buffID,
            pendingBuff.buffName,
            pendingBuff.buffCategory,
            pendingBuff.stack,
            pendingBuff.duration,
            pendingBuff.checkTiming,
            pendingBuff.expireRule,
            pendingBuff.delayTurns,
            pendingBuff.applyTimes,
            pendingBuff.intervalTurns
        );
    }

    // ================================
    // 按阶段检测状态
    // 这个函数只处理 Buff 自己的变化 / 消失
    // 不处理卡牌给不给 Buff
    // ================================
    public void CheckBuffsByTiming(string timing, bool consumeTriggeredBuffs = true)
    {
        if (BattleDebugSettings.ShowBuffLog)
        {
            Debug.Log("开始检测 " + characterName + " 的状态，阶段：" + timing);
        }

        // 从后往前遍历，方便安全删除
        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            BuffData buff = buffs[i];

            // 如果这个 Buff 不在当前阶段检测，就跳过
            if (buff.checkTiming != timing)
            {
                continue;
            }

            // 常驻型状态，不自动消失
            if (buff.expireRule == "Permanent")
            {
                if (BattleDebugSettings.ShowBuffLog)
                {
                    Debug.Log(buff.buffName + " 是常驻状态，不自动消失");
                }

                continue;
            }

            // 持续时间减少型
            if (buff.expireRule == "DurationDown")
            {
                buff.duration = buff.duration - 1;

                if (BattleDebugSettings.ShowBuffLog)
                {
                    Debug.Log(buff.buffName + " 持续时间减少，剩余：" + buff.duration);
                }

                if (buff.duration <= 0)
                {
                    if (BattleDebugSettings.ShowBuffLog)
                    {
                        Debug.Log(characterName + " 的状态结束：" + buff.buffName);
                    }

                    buffs.RemoveAt(i);
                }

                continue;
            }

            // 层数减少型
            if (buff.expireRule == "StackDown")
            {
                buff.stack = buff.stack - 1;

                if (BattleDebugSettings.ShowBuffLog)
                {
                    Debug.Log(buff.buffName + " 层数减少，剩余：" + buff.stack);
                }

                if (buff.stack <= 0)
                {
                    if (BattleDebugSettings.ShowBuffLog)
                    {
                        Debug.Log(characterName + " 的状态结束：" + buff.buffName);
                    }

                    buffs.RemoveAt(i);
                }
                //目前buff存在问题，持续时间和层数方面存在问题，有些是按照持续时间减少的，有些是按照层数减少的，这不合理，我们后面再改
                continue;
            }

            // 触发一次后消失
            if (buff.expireRule == "ConsumeOnTrigger")
            {
                if (!consumeTriggeredBuffs)
                {
                    if (BattleDebugSettings.ShowBuffLog)
                    {
                        Debug.Log(buff.buffName + " 本次检测暂缓触发后消失");
                    }

                    continue;
                }

                if (BattleDebugSettings.ShowBuffLog)
                {
                    Debug.Log(buff.buffName + " 触发后消失");
                }

                buffs.RemoveAt(i);
                continue;
            }

            // 特殊规则，暂时不自动处理
            if (buff.expireRule == "Custom")
            {
                if (BattleDebugSettings.ShowBuffLog)
                {
                    Debug.Log(buff.buffName + " 是特殊规则状态，暂时不自动处理");
                }

                continue;
            }

            Debug.LogWarning("未知的 Buff 消失规则：" + buff.expireRule);
        }
    }

    public int ConsumeTriggeredBuffs(string timing, params string[] buffIDs)
    {
        if (string.IsNullOrEmpty(timing) || buffIDs == null || buffIDs.Length == 0)
        {
            return 0;
        }

        int consumedStack = 0;

        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            BuffData buff = buffs[i];

            if (buff == null)
            {
                continue;
            }

            if (buff.checkTiming != timing)
            {
                continue;
            }

            if (buff.expireRule != "ConsumeOnTrigger")
            {
                continue;
            }

            if (!IsBuffIDInList(buff.buffID, buffIDs))
            {
                continue;
            }

            consumedStack += buff.stack;

            if (BattleDebugSettings.ShowBuffLog)
            {
                Debug.Log(characterName + " 消耗一次性数值Buff：" + buff.buffName + " x" + buff.stack);
            }

            buffs.RemoveAt(i);
        }

        return consumedStack;
    }

    public int ConsumeBuffsByRule(string consumeRule)
    {
        if (string.IsNullOrEmpty(consumeRule) || consumeRule == "None")
        {
            return 0;
        }

        int consumedStack = 0;

        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            BuffData buff = buffs[i];

            if (buff == null || string.IsNullOrEmpty(buff.buffID))
            {
                continue;
            }

            BuffDefinitionData definition;

            if (!BuffDefinitionLoader.TryGetDefinition(buff.buffID, out definition) || definition == null)
            {
                continue;
            }

            if (definition.defaultExpireRule != "ConsumeOnTrigger")
            {
                continue;
            }

            if (definition.consumeRule != consumeRule)
            {
                continue;
            }

            consumedStack += buff.stack;

            if (BattleDebugSettings.ShowBuffLog)
            {
                Debug.Log(characterName + " 按规则消耗一次性Buff：" + buff.buffName + " x" + buff.stack + "，规则：" + consumeRule);
            }

            buffs.RemoveAt(i);
        }

        return consumedStack;
    }

    public int ConsumeBuffStackByRule(string buffID, string consumeRule, int amount)
    {
        if (string.IsNullOrEmpty(buffID) ||
            string.IsNullOrEmpty(consumeRule) ||
            consumeRule == "None" ||
            amount <= 0)
        {
            return 0;
        }

        BuffDefinitionData definition;

        if (!BuffDefinitionLoader.TryGetDefinition(buffID, out definition) || definition == null)
        {
            Debug.LogWarning(characterName + " 按数量消费Buff失败：找不到Buff定义：" + buffID);
            return 0;
        }

        if (definition.defaultExpireRule != "ConsumeOnTrigger" ||
            definition.consumeRule != consumeRule)
        {
            return 0;
        }

        int remainingAmount = amount;
        int consumedStack = 0;

        for (int i = 0; i < buffs.Count && remainingAmount > 0; i++)
        {
            BuffData buff = buffs[i];

            if (buff == null || buff.buffID != buffID)
            {
                continue;
            }

            int consumeFromBatch = Mathf.Min(buff.stack, remainingAmount);
            buff.stack -= consumeFromBatch;
            remainingAmount -= consumeFromBatch;
            consumedStack += consumeFromBatch;

            if (BattleDebugSettings.ShowBuffLog)
            {
                Debug.Log(
                    characterName +
                    " 按数量消费一次性Buff：" +
                    buff.buffName +
                    " x" +
                    consumeFromBatch +
                    "，规则：" +
                    consumeRule
                );
            }

            if (buff.stack <= 0)
            {
                buffs.RemoveAt(i);
                i--;
            }
        }

        return consumedStack;
    }

    bool IsBuffIDInList(string buffID, string[] buffIDs)
    {
        if (string.IsNullOrEmpty(buffID) || buffIDs == null)
        {
            return false;
        }

        foreach (string allowedBuffID in buffIDs)
        {
            if (buffID == allowedBuffID)
            {
                return true;
            }
        }

        return false;
    }

    // 打印当前角色身上的全部状态
    public void PrintBuffs()
    {
        Debug.Log("===== " + characterName + " 当前状态列表 =====");

        if (buffs.Count == 0)
        {
            Debug.Log(characterName + " 当前没有任何状态");
            return;
        }

        foreach (BuffData buff in buffs)
        {
            Debug.Log(
                buff.buffName +
                " / ID：" + buff.buffID +
                " / 分类：" + buff.buffCategory +
                " / 层数：" + buff.stack +
                " / 持续：" + buff.duration +
                " / 检测阶段：" + buff.checkTiming +
                " / 消失规则：" + buff.expireRule
            );
        }
    }

    // PrintPendingBuffs = 打印当前角色身上的所有待生效状态
    public void PrintPendingBuffs()
    {
        Debug.Log("===== " + characterName + " 当前待生效状态列表 =====");

        if (pendingBuffs.Count == 0)
        {
            Debug.Log(characterName + " 当前没有待生效状态");
            return;
        }

        foreach (PendingBuffData pendingBuff in pendingBuffs)
        {
            Debug.Log(
                pendingBuff.buffName +
                " / ID：" + pendingBuff.buffID +
                " / 分类：" + pendingBuff.buffCategory +
                " / 层数：" + pendingBuff.stack +
                " / 持续：" + pendingBuff.duration +
                " / 延迟回合：" + pendingBuff.delayTurns +
                " / 生效次数：" + pendingBuff.applyTimes +
                " / 间隔回合：" + pendingBuff.intervalTurns
            );
        }
    }
}
