// 脚本中文说明：战斗事件处理器。负责统一接收战斗事件，并分发给卡牌管理等系统处理。
using UnityEngine;

// BattleEventProcessor = 战斗事件处理器
// 统一处理战斗事件
// 第一版先只打印事件，后面再接 CD / Buff / 罪卡 / 负罪感 / 成就 / UI
public static class BattleEventProcessor
{
    // ProcessEvent = 处理战斗事件
    public static void ProcessEvent(BattleEventContext context)
    {
        if (context == null)
        {
            Debug.LogWarning("BattleEventProcessor 收到空事件。");
            return;
        }

        PrintEventLog(context);

        BattleCardManager.HandleEvent(context);      // 卡牌 CD / 消耗
        // 后面会在这里逐步接入：
        // CardEffectExecutor.HandleEvent(context);     // 卡牌效果
        // BuffSystem.HandleEvent(context);             // 特殊 Buff 响应
        // GuiltSystem.HandleEvent(context);            // 负罪感系统
        // AchievementManager.HandleEvent(context);     // 成就系统
        // BattleUIEventBridge.HandleEvent(context);    // UI 表现
    }

    // PrintEventLog = 打印事件日志
    static void PrintEventLog(BattleEventContext context)
    {
        // 事件日志暂时归到详细战斗日志里
        if (!BattleDebugSettings.ShowDetailBattleLog)
        {
            return;
        }

        Debug.Log(
            "战斗事件：" + context.timing +
            " / 行动者：" + context.GetUserName() +
            " / 目标：" + context.GetTargetName() +
            " / 卡牌：" + context.GetCardName() +
            " / 拼点：" + context.clashPoint +
            " / 拼点结果：" + context.clashResult +
            " / 伤害：" + context.damage +
            " / 命中：" + context.isHit +
            " / 击杀：" + context.isKill
        );
    }
}