// 脚本中文说明：敌人意图管理器。负责创建敌人意图队列、查找敌人意图、打印敌人意图状态。
using System.Collections.Generic;
using UnityEngine;

// BattleEnemyIntentManager = 敌人意图管理器
// Manager = 管理器，这里负责创建、查找、筛选和打印敌人意图。
public static class BattleEnemyIntentManager
{
    // CreateIntentQueue = 创建敌人意图队列
    // params BattleEnemyIntent[] = 可以一次传入多个敌人意图。
    public static List<BattleEnemyIntent> CreateIntentQueue(params BattleEnemyIntent[] intents)
    {
        // intentQueue = 敌人意图队列
        // List<BattleEnemyIntent> = 多条敌人意图组成的列表。
        List<BattleEnemyIntent> intentQueue = new List<BattleEnemyIntent>();

        if (intents == null)
        {
            Debug.LogWarning("创建敌人意图队列失败：敌人意图为空");
            return intentQueue;
        }

        foreach (BattleEnemyIntent intent in intents)
        {
            // 跳过空意图，避免把无效数据放进队列。
            if (intent == null)
            {
                continue;
            }

            intentQueue.Add(intent);
        }

        Debug.Log("成功创建敌人意图队列，数量：" + intentQueue.Count);
        return intentQueue;
    }

    // FindIntentByOrder = 按顺序编号查找敌人意图
    // intentQueue = 敌人意图队列。
    // intentOrder = 敌人意图编号，例如 1 或 2。
    public static BattleEnemyIntent FindIntentByOrder(List<BattleEnemyIntent> intentQueue, int intentOrder)
    {
        if (intentQueue == null)
        {
            Debug.LogWarning("查找敌人意图失败：敌人意图队列为空");
            return null;
        }

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            // 找到编号相同的敌人意图就返回。
            if (intent != null && intent.intentOrder == intentOrder)
            {
                Debug.Log("找到敌人意图" + intentOrder + "：" + intent.GetEnemyName() + " 使用 " + intent.GetCardName());
                return intent;
            }
        }

        Debug.LogWarning("查找敌人意图失败：找不到敌人意图" + intentOrder);
        return null;
    }

    // PrintIntentQueue = 打印敌人意图队列
    // 用于在 Console 里查看本回合敌人准备做什么。
    public static void PrintIntentQueue(List<BattleEnemyIntent> intentQueue)
    {
        Debug.Log("===== 当前敌人意图队列 =====");

        if (intentQueue == null || intentQueue.Count == 0)
        {
            Debug.Log("当前没有敌人意图");
            return;
        }

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            // 每个敌人意图交给 PrintIntentState 打印详细状态。
            PrintIntentState(intent);
        }
    }

    // PrintUnrespondedIntents = 打印未响应敌人意图
    // 未响应 = 当前没有玩家槽位绑定这个敌人意图。
    public static void PrintUnrespondedIntents(List<BattleEnemyIntent> intentQueue)
    {
        Debug.Log("===== 当前未响应敌人意图 =====");

        if (intentQueue == null || intentQueue.Count == 0)
        {
            Debug.Log("当前没有敌人意图");
            return;
        }

        // 先筛选出未响应意图，再逐条打印。
        List<BattleEnemyIntent> unrespondedIntents = GetUnrespondedIntents(intentQueue);

        foreach (BattleEnemyIntent intent in unrespondedIntents)
        {
            PrintIntentState(intent);
        }

        if (unrespondedIntents.Count == 0)
        {
            Debug.Log("当前没有未响应敌人意图");
        }
    }

    // GetUnrespondedIntents = 获取未响应敌人意图列表
    // 返回所有 isResponded == false 的敌人意图。
    public static List<BattleEnemyIntent> GetUnrespondedIntents(List<BattleEnemyIntent> intentQueue)
    {
        List<BattleEnemyIntent> unrespondedIntents = new List<BattleEnemyIntent>();

        if (intentQueue == null || intentQueue.Count == 0)
        {
            return unrespondedIntents;
        }

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            // isResponded = 是否已响应。
            // false 表示还没有玩家槽位响应这个敌人意图。
            if (intent != null && !intent.isResponded)
            {
                unrespondedIntents.Add(intent);
            }
        }

        return unrespondedIntents;
    }

    // PrintIntentHandlingPreview = 打印敌人意图处理预览
    // Preview = 预览，只显示未来会怎么处理，不执行、不 Roll、不扣血。
    public static void PrintIntentHandlingPreview(List<BattleEnemyIntent> intentQueue)
    {
        Debug.Log("===== 敌人意图处理预览 =====");

        if (intentQueue == null || intentQueue.Count == 0)
        {
            Debug.Log("当前没有敌人意图，无法生成处理预览");
            return;
        }

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent == null)
            {
                continue;
            }

            if (intent.isResponded)
            {
                // 已响应：未来会进入玩家响应处理。
                Debug.Log(
                    "敌人意图" + intent.intentOrder +
                    "：已响应，未来将进入玩家响应处理，当前实际目标：" +
                    intent.GetActualTargetSlotText()
                );
            }
            else
            {
                // 未响应：未来敌人会按 actualTarget 攻击。
                Debug.Log(
                    "敌人意图" + intent.intentOrder +
                    "：未响应，未来将按当前 actualTarget 执行，目标：" +
                    intent.GetActualTargetSlotText()
                );
            }
        }
    }

    // PrintIntentState = 打印单个敌人意图状态
    // 包括敌人、卡牌、原目标、实际目标、是否已响应。
    public static void PrintIntentState(BattleEnemyIntent intent)
    {
        if (intent == null)
        {
            Debug.LogWarning("敌人意图状态打印失败：敌人意图为空");
            return;
        }

        Debug.Log(
            "敌人意图" + intent.intentOrder +
            " / 敌人：" + intent.GetEnemyName() +
            " / 卡牌：" + intent.GetCardName() +
            " / 原目标：" + intent.GetOriginalTargetSlotText() +
            " / 实际目标：" + intent.GetActualTargetSlotText() +
            " / 已响应：" + intent.isResponded
        );
    }
}
