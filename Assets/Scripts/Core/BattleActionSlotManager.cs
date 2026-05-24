using System.Collections.Generic;
using UnityEngine;

// BattleActionSlotManager = 行动槽位管理器
// 第一版只负责创建、安排、去重和打印，不执行真正战斗结算
public static class BattleActionSlotManager
{
    public static List<BattleActionSlot> CreateActionSlots(int slotCount)
    {
        List<BattleActionSlot> slots = new List<BattleActionSlot>();

        if (slotCount <= 0)
        {
            Debug.LogWarning("创建行动槽位失败：槽位数量必须大于 0");
            return slots;
        }

        for (int i = 0; i < slotCount; i++)
        {
            slots.Add(new BattleActionSlot(i + 1));
        }

        Debug.Log("成功创建 " + slotCount + " 个行动槽位");
        return slots;
    }

    public static bool AssignResponseToEnemyIntent(
        List<BattleActionSlot> slots,
        int slotIndex,
        CharacterData actor,
        BattleCardState cardState,
        BattleEnemyIntent enemyIntent
    )
    {
        BattleActionSlot slot = GetSlot(slots, slotIndex);

        if (slot == null)
        {
            Debug.LogWarning("安排响应行动失败：找不到槽位 " + slotIndex);
            return false;
        }

        if (!CanAssignCardToSlot(slots, slot, cardState))
        {
            return false;
        }

        if (actor == null || enemyIntent == null || enemyIntent.enemy == null || enemyIntent.originalTargetCharacter == null)
        {
            Debug.LogWarning("安排响应行动失败：响应行动数据不完整");
            return false;
        }

        if (!BattleTargeting.CanInterceptAttack(actor, enemyIntent.enemy, enemyIntent.originalTargetCharacter))
        {
            Debug.Log("速度不足，无法改变该敌人卡牌目标");
            return false;
        }

        string originalTargetText = enemyIntent.GetOriginalTargetSlotText();
        List<BattleActionSlot> oldBoundSlots = FindSlotsByEnemyIntent(slots, enemyIntent);

        foreach (BattleActionSlot oldSlot in oldBoundSlots)
        {
            if (object.ReferenceEquals(oldSlot, slot))
            {
                continue;
            }

            oldSlot.UnbindEnemyIntent();
            Debug.Log("槽位 " + oldSlot.slotIndex + " 已解除对敌人意图" + enemyIntent.intentOrder + "的响应绑定");
        }

        slot.AssignResponse(actor, cardState, enemyIntent);
        enemyIntent.MarkResponded();

        Debug.Log(
            "槽位 " + slot.slotIndex +
            " 安排响应成功：" +
            slot.GetActorName() +
            " 使用 " +
            slot.GetCardName() +
            " 介入敌人意图"
        );

        Debug.Log(
            "敌人意图目标从 " +
            originalTargetText +
            " 改为 " +
            enemyIntent.GetActualTargetSlotText()
        );

        return true;
    }

    public static bool AssignFreeAction(
        List<BattleActionSlot> slots,
        int slotIndex,
        CharacterData actor,
        BattleCardState cardState,
        CharacterData target
    )
    {
        BattleActionSlot slot = GetSlot(slots, slotIndex);

        if (slot == null)
        {
            Debug.LogWarning("安排自由行动失败：找不到槽位 " + slotIndex);
            return false;
        }

        if (!CanAssignCardToSlot(slots, slot, cardState))
        {
            return false;
        }

        if (actor == null)
        {
            Debug.LogWarning("安排自由行动失败：行动者为空");
            return false;
        }

        slot.AssignFreeAction(actor, cardState, target);

        Debug.Log(
            "槽位 " + slot.slotIndex +
            " 安排自由行动成功：" +
            slot.GetActorName() +
            " 使用 " +
            slot.GetCardName()
        );

        return true;
    }

    public static void PrintSlotStates(List<BattleActionSlot> slots)
    {
        Debug.Log("===== 当前行动槽位状态 =====");

        if (slots == null || slots.Count == 0)
        {
            Debug.Log("当前没有行动槽位");
            return;
        }

        foreach (BattleActionSlot slot in slots)
        {
            if (slot == null)
            {
                continue;
            }

            if (slot.IsEmpty())
            {
                Debug.Log("槽位 " + slot.slotIndex + "：空");
                continue;
            }

            string intentText = "";

            if (slot.slotType == BattleActionSlotType.RespondToEnemyIntent && slot.enemyIntent != null)
            {
                intentText =
                    " / 响应意图：" +
                    slot.enemyIntent.GetEnemyName() +
                    " 使用 " +
                    slot.enemyIntent.GetCardName() +
                    " 攻击 " +
                    slot.enemyIntent.GetOriginalTargetSlotText();
            }
            else if (slot.slotType == BattleActionSlotType.RespondToEnemyIntent && slot.enemyIntent == null)
            {
                intentText = " / 响应意图：无 / 已解除绑定";
            }

            Debug.Log(
                "槽位 " + slot.slotIndex +
                " / 类型：" + slot.slotType +
                " / 行动者：" + slot.GetActorName() +
                " / 卡牌：" + slot.GetCardName() +
                " / 目标：" + slot.GetTargetName() +
                " / 已使用：" + slot.isUsed +
                intentText
            );
        }
    }

    public static void PrintActionSlotIntentHandlingPreview(
        List<BattleActionSlot> actionSlots,
        List<BattleEnemyIntent> intentQueue
    )
    {
        Debug.Log("===== 行动槽位处理敌人意图预览 =====");
        Debug.Log("提示：当前仅为行动槽位与敌人意图绑定关系 / 处理路径预览，不代表正式执行顺序");
        Debug.Log("提示：未来正式执行队列采用速度响应优先方向，高速响应行动可能提前处理其指定敌人意图");

        if (intentQueue == null || intentQueue.Count == 0)
        {
            Debug.Log("当前没有敌人意图，无法生成行动槽位处理预览");
            return;
        }

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent == null)
            {
                continue;
            }

            if (!intent.isResponded)
            {
                Debug.Log(
                    "敌人意图" + intent.intentOrder +
                    "：未响应，未来按当前 actualTarget 执行，目标：" +
                    intent.GetActualTargetSlotText()
                );
                continue;
            }

            BattleActionSlot boundSlot = FindSlotByEnemyIntent(actionSlots, intent);

            if (boundSlot == null)
            {
                Debug.Log(
                    "敌人意图" + intent.intentOrder +
                    "：已响应，但未找到绑定的行动槽位"
                );
                continue;
            }

            Debug.Log(
                "敌人意图" + intent.intentOrder +
                "：已响应，未来由 " +
                boundSlot.GetActorName() +
                " 槽位" +
                boundSlot.slotIndex +
                " 处理，当前实际目标：" +
                intent.GetActualTargetSlotText()
            );
        }
    }

    public static void PrintSpeedPriorityHandlingPreview(
        List<BattleActionSlot> actionSlots,
        List<BattleEnemyIntent> intentQueue
    )
    {
        Debug.Log("===== 速度响应优先处理顺序预览 =====");
        Debug.Log("提示：当前只是第一版处理顺序预览，不执行任何槽位或敌人意图");
        Debug.Log("提示：当前预览采用“已响应优先、未响应补后”的简化规则，不代表最终完整速度队列");

        if (intentQueue == null || intentQueue.Count == 0)
        {
            Debug.Log("当前没有敌人意图，无法生成速度响应优先处理顺序预览");
            return;
        }

        List<BattleHandlingPreviewItem> previewItems = CreateSpeedPriorityHandlingPreviewItems(actionSlots, intentQueue);

        foreach (BattleHandlingPreviewItem previewItem in previewItems)
        {
            if (previewItem.handlingType == BattleHandlingPreviewType.RespondedIntent)
            {
                if (previewItem.actionSlot == null)
                {
                    Debug.Log(
                        previewItem.order +
                        ". 已响应：敌人意图" +
                        previewItem.enemyIntent.intentOrder +
                        " 已响应，但未找到绑定槽位"
                    );
                    continue;
                }

                Debug.Log(
                    previewItem.order +
                    ". 已响应：" +
                    previewItem.actionSlot.GetActorName() +
                    " 槽位" +
                    previewItem.actionSlot.slotIndex +
                    " 处理 敌人意图" +
                    previewItem.enemyIntent.intentOrder +
                    "，当前实际目标：" +
                    previewItem.enemyIntent.GetActualTargetSlotText()
                );
                continue;
            }

            Debug.Log(
                previewItem.order +
                ". 未响应：敌人意图" +
                previewItem.enemyIntent.intentOrder +
                " 未来按当前 actualTarget 执行，目标：" +
                previewItem.enemyIntent.GetActualTargetSlotText()
            );
        }
    }

    public static List<BattleHandlingPreviewItem> CreateSpeedPriorityHandlingPreviewItems(
        List<BattleActionSlot> actionSlots,
        List<BattleEnemyIntent> intentQueue
    )
    {
        List<BattleHandlingPreviewItem> previewItems = new List<BattleHandlingPreviewItem>();
        List<BattleEnemyIntent> previewIntentOrder = GetSpeedPriorityPreviewIntentOrder(intentQueue);

        int order = 1;

        foreach (BattleEnemyIntent intent in previewIntentOrder)
        {
            if (intent.isResponded)
            {
                previewItems.Add(new BattleHandlingPreviewItem(
                    order,
                    BattleHandlingPreviewType.RespondedIntent,
                    intent,
                    FindSlotByEnemyIntent(actionSlots, intent)
                ));
            }
            else
            {
                previewItems.Add(new BattleHandlingPreviewItem(
                    order,
                    BattleHandlingPreviewType.UnrespondedIntent,
                    intent,
                    null
                ));
            }

            order++;
        }

        return previewItems;
    }

    static List<BattleEnemyIntent> GetSpeedPriorityPreviewIntentOrder(List<BattleEnemyIntent> intentQueue)
    {
        List<BattleEnemyIntent> previewIntentOrder = new List<BattleEnemyIntent>();

        if (intentQueue == null || intentQueue.Count == 0)
        {
            return previewIntentOrder;
        }

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent != null && intent.isResponded)
            {
                previewIntentOrder.Add(intent);
            }
        }

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent != null && !intent.isResponded)
            {
                previewIntentOrder.Add(intent);
            }
        }

        return previewIntentOrder;
    }

    static BattleActionSlot GetSlot(List<BattleActionSlot> slots, int slotIndex)
    {
        if (slots == null)
        {
            return null;
        }

        foreach (BattleActionSlot slot in slots)
        {
            if (slot != null && slot.slotIndex == slotIndex)
            {
                return slot;
            }
        }

        return null;
    }

    static bool CanAssignCardToSlot(
        List<BattleActionSlot> slots,
        BattleActionSlot targetSlot,
        BattleCardState cardState
    )
    {
        if (targetSlot == null)
        {
            return false;
        }

        if (!targetSlot.IsEmpty())
        {
            Debug.Log("槽位 " + targetSlot.slotIndex + " 已经安排了行动");
            return false;
        }

        if (cardState == null)
        {
            Debug.LogWarning("安排行动失败：卡牌状态为空");
            return false;
        }

        if (IsCardAlreadyAssigned(slots, cardState))
        {
            Debug.Log("同一张卡本回合已经被安排");
            return false;
        }

        return true;
    }

    static bool IsCardAlreadyAssigned(List<BattleActionSlot> slots, BattleCardState cardState)
    {
        if (slots == null || cardState == null)
        {
            return false;
        }

        foreach (BattleActionSlot slot in slots)
        {
            if (slot == null || slot.cardState == null)
            {
                continue;
            }

            if (object.ReferenceEquals(slot.cardState, cardState))
            {
                return true;
            }
        }

        return false;
    }

    static List<BattleActionSlot> FindSlotsByEnemyIntent(
        List<BattleActionSlot> slots,
        BattleEnemyIntent enemyIntent
    )
    {
        List<BattleActionSlot> boundSlots = new List<BattleActionSlot>();

        if (slots == null || enemyIntent == null)
        {
            return boundSlots;
        }

        foreach (BattleActionSlot slot in slots)
        {
            if (slot == null || slot.IsEmpty())
            {
                continue;
            }

            if (object.ReferenceEquals(slot.enemyIntent, enemyIntent))
            {
                boundSlots.Add(slot);
            }
        }

        return boundSlots;
    }

    static BattleActionSlot FindSlotByEnemyIntent(
        List<BattleActionSlot> slots,
        BattleEnemyIntent enemyIntent
    )
    {
        if (slots == null || enemyIntent == null)
        {
            return null;
        }

        foreach (BattleActionSlot slot in slots)
        {
            if (slot == null)
            {
                continue;
            }

            if (object.ReferenceEquals(slot.enemyIntent, enemyIntent))
            {
                return slot;
            }
        }

        return null;
    }
}
