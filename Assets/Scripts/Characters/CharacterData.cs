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
        int speedUpStack = GetBuffStack("SpeedUp");
        //int speedUpStack = 获取这个角色身上名叫 "SpeedUp" 的 Buff 总层数;
        int speedDownStack = GetBuffStack("SpeedDown");

        int currentSpeed = turnSpeed + speedUpStack - speedDownStack;

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
                characterName + " 获得状态：" +
                buffName +
                "，类型：" + buffCategory +
                "，层数：" + stack +
                "，持续：" + duration +
                "，检测阶段：" + checkTiming +
                "，消失规则：" + expireRule
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
        string buffName = buffID;
        string buffCategory = GuessBuffCategory(buffID);
        string checkTiming = GuessCheckTiming(buffID);
        string expireRule = GuessExpireRule(buffID);

        AddBuff(buffID, buffName, buffCategory, stack, duration, checkTiming, expireRule);
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

    // ================================
    // 按阶段检测状态
    // 这个函数只处理 Buff 自己的变化 / 消失
    // 不处理卡牌给不给 Buff
    // ================================
    public void CheckBuffsByTiming(string timing)
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