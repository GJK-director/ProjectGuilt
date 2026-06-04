// 脚本中文说明：行动槽位管理器。负责创建槽位、安排响应行动或自由行动、检查重复安排、打印槽位状态。
using System.Collections.Generic;
using UnityEngine;

// BattleActionSlotManager = 行动槽位管理器
// 第一版只负责创建、安排、去重和打印，不执行真正战斗结算
public static class BattleActionSlotManager
{
    // CreateActionSlots = 创建行动槽位
    // slotCount = 要创建几个槽位。
    public static List<BattleActionSlot> CreateActionSlots(int slotCount)
    {
        // slots = 行动槽位列表。
        List<BattleActionSlot> slots = new List<BattleActionSlot>();

        if (slotCount <= 0)
        {
            Debug.LogWarning("创建行动槽位失败：槽位数量必须大于 0");
            return slots;
        }

        for (int i = 0; i < slotCount; i++)
        {
            // 槽位编号从 1 开始，所以这里用 i + 1。
            slots.Add(new BattleActionSlot(i + 1));
        }

        Debug.Log("成功创建 " + slotCount + " 个行动槽位");
        return slots;
    }

    // CreateCharacterActionSlots = 为单个角色创建角色内行动槽位
    // owner = 槽位归属角色，slotCount = 该角色拥有几个槽位。
    public static List<BattleActionSlot> CreateCharacterActionSlots(CharacterData owner, int slotCount)
    {
        List<BattleActionSlot> slots = new List<BattleActionSlot>();

        if (owner == null)
        {
            Debug.LogWarning("创建角色行动槽位失败：owner 为空");
            return slots;
        }

        if (slotCount <= 0)
        {
            Debug.LogWarning("创建角色行动槽位失败：槽位数量必须大于 0");
            return slots;
        }

        for (int i = 0; i < slotCount; i++)
        {
            slots.Add(new BattleActionSlot(owner, i + 1));
        }

        Debug.Log("成功为 " + owner.characterName + " 创建 " + slotCount + " 个行动槽位");
        return slots;
    }

    // CreatePartyActionSlots = 为我方 A / B 创建角色独立行动槽位
    // 第一版用于创建 allyA 槽位1/2、allyB 槽位1/2。
    public static List<BattleActionSlot> CreatePartyActionSlots(
        CharacterData allyA,
        CharacterData allyB,
        int slotCountPerCharacter
    )
    {
        List<BattleActionSlot> slots = new List<BattleActionSlot>();

        slots.AddRange(CreateCharacterActionSlots(allyA, slotCountPerCharacter));
        slots.AddRange(CreateCharacterActionSlots(allyB, slotCountPerCharacter));

        Debug.Log("成功创建队伍行动槽位，数量：" + slots.Count);
        return slots;
    }

    // AssignResponseToEnemyIntent = 安排一个槽位响应敌人意图
    // slots = 所有行动槽位。
    // slotIndex = 要放入的槽位编号。
    // actor = 行动者，例如玩家 A。
    // cardState = 要放入槽位的卡牌状态。
    // enemyIntent = 要响应的敌人意图。
    public static bool AssignResponseToEnemyIntent(
        List<BattleActionSlot> slots,
        int slotIndex,
        CharacterData actor,
        BattleCardState cardState,
        BattleEnemyIntent enemyIntent
    )
    {
        // 先根据槽位编号找到目标槽位。
        BattleActionSlot slot = GetSlot(slots, slotIndex);

        if (slot == null)
        {
            Debug.LogWarning("安排响应行动失败：找不到槽位 " + slotIndex);
            return false;
        }

        return AssignResponseToEnemyIntentToSlot(slots, slot, actor, cardState, enemyIntent);
    }

    // AssignResponseToEnemyIntent = owner 版本安排响应敌人意图
    // owner + slotIndex 用于区分“我方角色A 槽位1”和“我方角色B 槽位1”。
    public static bool AssignResponseToEnemyIntent(
        List<BattleActionSlot> slots,
        CharacterData owner,
        int slotIndex,
        CharacterData actor,
        BattleCardState cardState,
        BattleEnemyIntent enemyIntent
    )
    {
        BattleActionSlot slot = GetSlot(slots, owner, slotIndex);

        if (slot == null)
        {
            return false;
        }

        return AssignResponseToEnemyIntentToSlot(slots, slot, actor, cardState, enemyIntent);
    }

    // AssignResponseToEnemyIntentToSlot = 对指定槽位安排响应敌人意图
    // 旧 slotIndex 入口和新 owner + slotIndex 入口共用这套逻辑。
    static bool AssignResponseToEnemyIntentToSlot(
        List<BattleActionSlot> slots,
        BattleActionSlot slot,
        CharacterData actor,
        BattleCardState cardState,
        BattleEnemyIntent enemyIntent
    )
    {
        // 检查槽位能不能放这张卡。
        // 例如槽位不能为空、同一张卡不能重复安排。
        if (!CanAssignCardToSlot(slots, slot, cardState))
        {
            return false;
        }

        // 响应敌人意图必须有行动者、敌人意图、敌人和原始目标。
        if (actor == null || enemyIntent == null || enemyIntent.enemy == null || enemyIntent.originalTargetCharacter == null)
        {
            Debug.LogWarning("安排响应行动失败：响应行动数据不完整");
            return false;
        }

        int actorSpeed = actor.GetCurrentSpeed();
        int enemySpeed = enemyIntent.enemy.GetCurrentSpeed();
        bool canRewriteActualTarget = actorSpeed > enemySpeed;
        bool isOriginalTargetSlot =
            object.ReferenceEquals(actor, enemyIntent.originalTargetCharacter) &&
            slot.slotIndex == enemyIntent.originalTargetSlotIndex;

        if (!canRewriteActualTarget && !isOriginalTargetSlot)
        {
            Debug.Log("速度不足，且不是原目标槽位，无法响应该敌人意图");
            return false;
        }

        // 记录改写前的目标文本，方便打印“从谁改到谁”。
        string actualTargetBeforeResponse = enemyIntent.GetActualTargetSlotText();

        // 同一个敌人意图只能有一个主要响应槽位。
        // 先找旧绑定槽位，后面解除旧绑定。
        List<BattleActionSlot> oldBoundSlots = FindSlotsByEnemyIntent(slots, enemyIntent);

        foreach (BattleActionSlot oldSlot in oldBoundSlots)
        {
            // 如果旧槽位就是当前槽位，不需要解除自己。
            if (object.ReferenceEquals(oldSlot, slot))
            {
                continue;
            }

            // 解除其他槽位对同一个敌人意图的绑定。
            oldSlot.UnbindEnemyIntent();
            Debug.Log(oldSlot.GetDisplaySlotName() + " 已解除对敌人意图" + enemyIntent.intentOrder + "的响应绑定");
        }

        // 真正把角色、卡牌、敌人意图写入槽位。
        slot.AssignResponse(actor, cardState, enemyIntent, canRewriteActualTarget);

        // 敌人意图标记为已响应。
        enemyIntent.MarkResponded();

        Debug.Log(
            slot.GetDisplaySlotName() +
            " 安排响应成功：" +
            slot.GetActorName() +
            " 使用 " +
            slot.GetCardName() +
            " 响应敌人意图"
        );

        if (canRewriteActualTarget)
        {
            Debug.Log(
                "高速响应成功：敌人意图目标从 " +
                actualTargetBeforeResponse +
                " 改为 " +
                enemyIntent.GetActualTargetSlotText()
            );
        }
        else
        {
            Debug.Log(
                "低速原目标槽位响应成功：不改写 actualTarget，敌人意图仍命中 " +
                enemyIntent.GetActualTargetSlotText()
            );
        }

        return true;
    }

    // AssignFreeAction = 安排自由行动
    // 自由行动不响应敌人意图，例如第一版 Ability 罪卡测试。
    public static bool AssignFreeAction(
        List<BattleActionSlot> slots,
        int slotIndex,
        CharacterData actor,
        BattleCardState cardState,
        CharacterData target
    )
    {
        // 先根据槽位编号找到目标槽位。
        BattleActionSlot slot = GetSlot(slots, slotIndex);

        if (slot == null)
        {
            Debug.LogWarning("安排自由行动失败：找不到槽位 " + slotIndex);
            return false;
        }

        return AssignFreeActionToSlot(slots, slot, actor, cardState, target);
    }

    // AssignFreeAction = owner 版本安排自由行动
    // owner + slotIndex 用于区分“我方角色A 槽位1”和“我方角色B 槽位1”。
    public static bool AssignFreeAction(
        List<BattleActionSlot> slots,
        CharacterData owner,
        int slotIndex,
        CharacterData actor,
        BattleCardState cardState,
        CharacterData target
    )
    {
        BattleActionSlot slot = GetSlot(slots, owner, slotIndex);

        if (slot == null)
        {
            return false;
        }

        return AssignFreeActionToSlot(slots, slot, actor, cardState, target);
    }

    // AssignFreeActionToSlot = 对指定槽位安排自由行动
    // 旧 slotIndex 入口和新 owner + slotIndex 入口共用这套逻辑。
    static bool AssignFreeActionToSlot(
        List<BattleActionSlot> slots,
        BattleActionSlot slot,
        CharacterData actor,
        BattleCardState cardState,
        CharacterData target
    )
    {
        // 检查槽位是否为空、卡牌是否为空、卡牌是否已经被安排过。
        if (!CanAssignCardToSlot(slots, slot, cardState))
        {
            return false;
        }

        // 自由行动必须有行动者。
        if (actor == null)
        {
            Debug.LogWarning("安排自由行动失败：行动者为空");
            return false;
        }

        // 把自由行动写入槽位，不绑定敌人意图。
        slot.AssignFreeAction(actor, cardState, target);

        Debug.Log(
            slot.GetDisplaySlotName() +
            " 安排自由行动成功：" +
            slot.GetActorName() +
            " 使用 " +
            slot.GetCardName()
        );

        return true;
    }

    // PrintSlotStates = 打印当前所有行动槽位状态
    // 只用于调试查看，不执行任何战斗逻辑。
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
                // 空槽位只打印“空”。
                Debug.Log(
                    slot.GetDisplaySlotName() +
                    "：空 / owner：" + slot.GetOwnerName() +
                    " / slotIndex：" + slot.slotIndex +
                    " / 已使用：" + slot.isUsed
                );
                continue;
            }

            // intentText = 敌人意图说明文本。
            // 只有响应敌人意图的槽位才会附加这段文本。
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
                slot.GetDisplaySlotName() +
                " / owner：" + slot.GetOwnerName() +
                " / slotIndex：" + slot.slotIndex +
                " / 类型：" + slot.slotType +
                " / 行动者：" + slot.GetActorName() +
                " / 卡牌：" + slot.GetCardName() +
                " / 目标：" + slot.GetTargetName() +
                " / 已使用：" + slot.isUsed +
                intentText
            );
        }
    }

    // PrintActionSlots = 打印行动槽位
    // 当前复用 PrintSlotStates，保留一个更直观的入口名称给 Runtime/UI 测试使用。
    public static void PrintActionSlots(List<BattleActionSlot> slots)
    {
        PrintSlotStates(slots);
    }

    // PrintActionSlotIntentHandlingPreview = 打印行动槽位处理敌人意图预览
    // Preview = 预览，只显示绑定关系和未来处理方向，不代表正式执行顺序。
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
                // 没有玩家响应时，未来按敌人意图当前实际目标处理。
                Debug.Log(
                    "敌人意图" + intent.intentOrder +
                    "：未响应，未来按当前 actualTarget 执行，目标：" +
                    intent.GetActualTargetSlotText()
                );
                continue;
            }

            // 已响应时，需要找到绑定这个敌人意图的行动槽位。
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

    // PrintSpeedPriorityHandlingPreview = 打印速度响应优先处理顺序预览
    // 当前只是简化预览：已响应优先，未响应补后。
    // 注意：这还不是最终速度队列规则。
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

        // 先创建预览项列表，再逐条打印。
        // BattleHandlingPreviewItem = 战斗处理预览项。
        List<BattleHandlingPreviewItem> previewItems = CreateSpeedPriorityHandlingPreviewItems(actionSlots, intentQueue);

        foreach (BattleHandlingPreviewItem previewItem in previewItems)
        {
            if (previewItem.handlingType == BattleHandlingPreviewType.RespondedIntent)
            {
                // RespondedIntent = 已响应意图。
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

            // UnrespondedIntent = 未响应意图。
            Debug.Log(
                previewItem.order +
                ". 未响应：敌人意图" +
                previewItem.enemyIntent.intentOrder +
                " 未来按当前 actualTarget 执行，目标：" +
                previewItem.enemyIntent.GetActualTargetSlotText()
            );
        }
    }

    // CreateSpeedPriorityHandlingPreviewItems = 创建速度优先处理预览项
    // 当前只是按“已响应优先、未响应补后”的简化规则创建预览列表。
    public static List<BattleHandlingPreviewItem> CreateSpeedPriorityHandlingPreviewItems(
        List<BattleActionSlot> actionSlots,
        List<BattleEnemyIntent> intentQueue
    )
    {
        List<BattleHandlingPreviewItem> previewItems = new List<BattleHandlingPreviewItem>();

        // previewIntentOrder = 预览用敌人意图顺序。
        List<BattleEnemyIntent> previewIntentOrder = GetSpeedPriorityPreviewIntentOrder(intentQueue);

        // order = 预览顺序编号。
        int order = 1;

        foreach (BattleEnemyIntent intent in previewIntentOrder)
        {
            if (intent.isResponded)
            {
                // 已响应意图会尝试找到对应行动槽位。
                previewItems.Add(new BattleHandlingPreviewItem(
                    order,
                    BattleHandlingPreviewType.RespondedIntent,
                    intent,
                    FindSlotByEnemyIntent(actionSlots, intent)
                ));
            }
            else
            {
                // 未响应意图没有行动槽位。
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

    // GetSpeedPriorityPreviewIntentOrder = 获取速度优先预览用的敌人意图顺序
    // 当前名字里有 SpeedPriority，但实际规则仍然只是“已响应先、未响应后”。
    // 真正按速度和槽位生成顺序的规则还在代办里。
    static List<BattleEnemyIntent> GetSpeedPriorityPreviewIntentOrder(List<BattleEnemyIntent> intentQueue)
    {
        List<BattleEnemyIntent> previewIntentOrder = new List<BattleEnemyIntent>();

        if (intentQueue == null || intentQueue.Count == 0)
        {
            return previewIntentOrder;
        }

        // 第一轮：加入已响应意图。
        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent != null && intent.isResponded)
            {
                previewIntentOrder.Add(intent);
            }
        }

        // 第二轮：加入未响应意图。
        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent != null && !intent.isResponded)
            {
                previewIntentOrder.Add(intent);
            }
        }

        return previewIntentOrder;
    }

    // GetSlot = 根据 owner + 槽位编号查找行动槽位
    // 用于角色独立槽位，例如“我方角色A 槽位1”和“我方角色B 槽位1”。
    public static BattleActionSlot GetSlot(
        List<BattleActionSlot> slots,
        CharacterData owner,
        int slotIndex
    )
    {
        if (slots == null)
        {
            Debug.LogWarning("按 owner 查找槽位失败：槽位列表为空");
            return null;
        }

        if (owner == null)
        {
            Debug.LogWarning("按 owner 查找槽位失败：owner 为空");
            return null;
        }

        foreach (BattleActionSlot slot in slots)
        {
            if (slot == null)
            {
                continue;
            }

            if (object.ReferenceEquals(slot.owner, owner) && slot.slotIndex == slotIndex)
            {
                return slot;
            }
        }

        Debug.LogWarning("找不到 " + owner.characterName + " 槽位" + slotIndex);
        return null;
    }

    // GetSlot = 根据槽位编号查找行动槽位
    // slots = 所有行动槽位，slotIndex = 要找的槽位编号。
    static BattleActionSlot GetSlot(List<BattleActionSlot> slots, int slotIndex)
    {
        if (slots == null)
        {
            return null;
        }

        foreach (BattleActionSlot slot in slots)
        {
            // 找到编号相同的槽位就返回。
            if (slot != null && slot.slotIndex == slotIndex)
            {
                return slot;
            }
        }

        return null;
    }

    // CanAssignCardToSlot = 判断一张卡能不能安排到目标槽位
    // targetSlot = 目标槽位。
    // cardState = 要安排的卡牌状态。
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

        // 目标槽位必须为空。
        if (!targetSlot.IsEmpty())
        {
            Debug.Log(targetSlot.GetDisplaySlotName() + " 已经安排了行动");
            return false;
        }

        // 卡牌状态不能为空。
        if (cardState == null)
        {
            Debug.LogWarning("安排行动失败：卡牌状态为空");
            return false;
        }

        // 同一张 BattleCardState 本回合不能重复安排到多个槽位。
        if (IsCardAlreadyAssigned(slots, cardState))
        {
            Debug.Log("同一张卡本回合已经被安排");
            return false;
        }

        return true;
    }

    // IsCardAlreadyAssigned = 判断同一张卡是否已经被安排过
    // 注意：这里判断的是同一个 BattleCardState 实例，不是同名卡牌。
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
                // ReferenceEquals = 判断两个变量是否指向同一个对象实例。
                return true;
            }
        }

        return false;
    }

    // FindSlotsByEnemyIntent = 找出所有绑定某个敌人意图的槽位
    // 用于解除旧响应绑定，保证同一个敌人意图只有一个主要响应槽位。
    static List<BattleActionSlot> FindSlotsByEnemyIntent(
        List<BattleActionSlot> slots,
        BattleEnemyIntent enemyIntent
    )
    {
        // boundSlots = 已绑定这个敌人意图的槽位列表。
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
                // 找到绑定同一个敌人意图的槽位。
                boundSlots.Add(slot);
            }
        }

        return boundSlots;
    }

    // FindSlotByEnemyIntent = 查找绑定某个敌人意图的第一个槽位
    // 用于打印预览或生成执行计划时找到响应槽位。
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
                // 找到第一个绑定同一个敌人意图的槽位就返回。
                return slot;
            }
        }

        return null;
    }
}
