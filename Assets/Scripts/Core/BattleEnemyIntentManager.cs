using System.Collections.Generic;
using UnityEngine;

public static class BattleEnemyIntentManager
{
    public static List<BattleEnemyIntent> CreateIntentQueue(params BattleEnemyIntent[] intents)
    {
        List<BattleEnemyIntent> intentQueue = new List<BattleEnemyIntent>();

        if (intents == null)
        {
            Debug.LogWarning("创建敌人意图队列失败：敌人意图为空");
            return intentQueue;
        }

        foreach (BattleEnemyIntent intent in intents)
        {
            if (intent == null)
            {
                continue;
            }

            intentQueue.Add(intent);
        }

        Debug.Log("成功创建敌人意图队列，数量：" + intentQueue.Count);
        return intentQueue;
    }

    public static BattleEnemyIntent FindIntentByOrder(List<BattleEnemyIntent> intentQueue, int intentOrder)
    {
        if (intentQueue == null)
        {
            Debug.LogWarning("查找敌人意图失败：敌人意图队列为空");
            return null;
        }

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent != null && intent.intentOrder == intentOrder)
            {
                Debug.Log("找到敌人意图" + intentOrder + "：" + intent.GetEnemyName() + " 使用 " + intent.GetCardName());
                return intent;
            }
        }

        Debug.LogWarning("查找敌人意图失败：找不到敌人意图" + intentOrder);
        return null;
    }

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
            PrintIntentState(intent);
        }
    }

    public static void PrintUnrespondedIntents(List<BattleEnemyIntent> intentQueue)
    {
        Debug.Log("===== 当前未响应敌人意图 =====");

        if (intentQueue == null || intentQueue.Count == 0)
        {
            Debug.Log("当前没有敌人意图");
            return;
        }

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

    public static List<BattleEnemyIntent> GetUnrespondedIntents(List<BattleEnemyIntent> intentQueue)
    {
        List<BattleEnemyIntent> unrespondedIntents = new List<BattleEnemyIntent>();

        if (intentQueue == null || intentQueue.Count == 0)
        {
            return unrespondedIntents;
        }

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent != null && !intent.isResponded)
            {
                unrespondedIntents.Add(intent);
            }
        }

        return unrespondedIntents;
    }

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
