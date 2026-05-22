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

        slot.AssignResponse(actor, cardState, enemyIntent);

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

            Debug.Log(
                "槽位 " + slot.slotIndex +
                " / 类型：" + slot.slotType +
                " / 行动者：" + slot.GetActorName() +
                " / 卡牌：" + slot.GetCardName() +
                " / 目标：" + slot.GetTargetName() +
                intentText
            );
        }
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
}
