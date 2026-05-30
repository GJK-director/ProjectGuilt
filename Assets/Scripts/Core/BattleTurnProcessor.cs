// 脚本中文说明：战斗回合处理器。负责回合开始和回合结束时处理速度、待生效 Buff、Buff 持续时间和卡牌 CD。
using System.Collections.Generic;
using UnityEngine;

// BattleTurnProcessor = 战斗回合处理器
// 专门负责回合开始、回合结束时要处理的角色状态
public static class BattleTurnProcessor
{
    // StartTurn = 开始回合
    // units = 本回合参与战斗的全部角色
    // Start = 开始，Turn = 回合。
    public static void StartTurn(List<CharacterData> units)
    {
        Debug.Log("===== 回合开始 =====");

        foreach (CharacterData unit in units)
        {
            if (unit == null)
            {
                continue;
            }

            // 第一步：回合开始时，先处理待生效状态
            // 例如：下回合获得伤害提升 / 下回合获得速度上升
            unit.ApplyPendingBuffsAtTurnStart();

            // 第二步：投掷本回合速度
            // 速度范围由角色自己的 minSpeed / maxSpeed 决定
            unit.RollTurnSpeed();

            // 第三步：打印当前速度
            // 当前速度 = 本回合速度 + SpeedUp - SpeedDown
            if (BattleDebugSettings.ShowSpeedLog)
            {
                Debug.Log(
                    unit.characterName +
                    " 当前速度：" +
                    unit.GetCurrentSpeed()
                );
            }
        }
    }

    // EndTurn = 结束回合
    // units = 本回合参与战斗的全部角色
    // End = 结束，Turn = 回合。
    public static void EndTurn(List<CharacterData> units)
    {
        Debug.Log("===== 回合结束，处理 Buff 持续时间 =====");

        foreach (CharacterData unit in units)
        {
            if (unit == null)
            {
                continue;
            }

            // 第一部分：旧 Buff 系统的回合结束检测
            // 例如：强壮 / 伤害提升 / 速度上升 持续时间减少
            //旧的buff不受到新的回合结束的检测，我们这里在回合结束的函数里单独让他处理一次回合结束
            unit.CheckBuffsByTiming("TurnEnd");

            // 第二部分：新事件系统的回合结束事件
            // 这里会通知 BattleCardManager，让卡牌 CD -1
            BattleEventContext context = new BattleEventContext(BattleTiming.TurnEnd)
                .SetUserAndTarget(unit, null);

            BattleEventProcessor.ProcessEvent(context);
        }
    }

    // PrintBattleState = 打印战斗状态
    // 目前先只打印全部角色的 Buff 和 PendingBuff，方便测试
    public static void PrintBattleState(List<CharacterData> units)
    {
        if (!BattleDebugSettings.ShowBattleStateLog)
        {
            return;
        }

        Debug.Log("===== 当前战斗状态 =====");

        foreach (CharacterData unit in units)
        {
            unit.PrintBuffs();
            unit.PrintPendingBuffs();
            GuiltManager.PrintGuilt(unit);
        }
    }
}
