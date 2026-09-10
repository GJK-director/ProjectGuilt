// 脚本中文说明：行动槽位管理器。负责创建槽位、安排响应行动或自由行动、检查重复安排、打印槽位状态。
using System.Collections.Generic;
using UnityEngine;

// BattleActionAssignmentResult = 正式准备阶段安排结果
// UI 可以直接读取结构化结果，不需要解析日志文本。
public sealed class BattleActionAssignmentResult
{
    public bool isSuccess;
    public bool wasAutoDowngraded;
    public string message;
    public BattleActionPlacementType placementType;
    public BattleActionSlotType effectiveSlotType;
    public CardEligibilityResult eligibilityResult;
}

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

        if (allyA != null)
        {
            slots.AddRange(CreateCharacterActionSlots(
                allyA,
                slotCountPerCharacter
            ));
        }
        if (allyB != null)
        {
            slots.AddRange(CreateCharacterActionSlots(
                allyB,
                slotCountPerCharacter
            ));
        }

        Debug.Log("成功创建队伍行动槽位，数量：" + slots.Count);
        return slots;
    }

    // CreateLivingPartyActionSlots = 为仍存活的我方角色创建下一回合行动槽位
    // 保留 CreatePartyActionSlots 原行为，避免影响历史测试主动创建死亡角色槽位的能力。
    public static List<BattleActionSlot> CreateLivingPartyActionSlots(
        CharacterData allyA,
        CharacterData allyB,
        int slotCountPerCharacter
    )
    {
        List<BattleActionSlot> slots = new List<BattleActionSlot>();

        AddLivingCharacterActionSlots(slots, allyA, slotCountPerCharacter, "A");
        AddLivingCharacterActionSlots(slots, allyB, slotCountPerCharacter, "B");

        Debug.Log("下一回合只为存活角色创建行动槽位，数量：" + slots.Count);
        return slots;
    }

    static void AddLivingCharacterActionSlots(
        List<BattleActionSlot> slots,
        CharacterData character,
        int slotCountPerCharacter,
        string label
    )
    {
        if (slots == null)
        {
            return;
        }

        if (character == null)
        {
            Debug.LogWarning("创建存活角色行动槽位：角色" + label + "为空，跳过");
            return;
        }

        if (character.IsDead())
        {
            Debug.Log("创建存活角色行动槽位：" + character.characterName + " 已死亡，跳过");
            return;
        }

        slots.AddRange(CreateCharacterActionSlots(character, slotCountPerCharacter));
    }

    // TryAssignToEnemyIntent = 把卡牌放到敌人已有意图
    // 无法精确响应时不会拒绝，而是自动降级为针对该敌人的单方面行动。
    public static bool TryAssignToEnemyIntent(
        BattleRuntimeState runtimeState,
        CharacterData owner,
        int slotIndex,
        BattleCardState cardState,
        BattleEnemyIntent enemyIntent,
        out BattleActionAssignmentResult result
    )
    {
        BattleActionSlot slot;
        CardEligibilityResult eligibility;

        if (!ValidateEnemyIntentTarget(runtimeState, enemyIntent, out result))
        {
            return false;
        }

        if (!ValidatePreparedAssignment(
                runtimeState,
                owner,
                slotIndex,
                cardState,
                enemyIntent.enemy,
                out slot,
                out eligibility,
                out result))
        {
            return false;
        }

        if (!IsAllowedForEnemyPlacement(cardState))
        {
            result = CreateAssignmentFailure(
                "安排到敌人意图失败：只允许 Attack、Defense 或 Dodge",
                CardEligibilityFailureReason.UnsupportedCondition
            );
            return false;
        }

        bool canRespond = CanExactlyRespondToEnemyIntent(
            slot,
            owner,
            cardState,
            enemyIntent
        );
        BattleActionPlacementType placementType = canRespond
            ? BattleActionPlacementType.ExactEnemyIntent
            : BattleActionPlacementType.SpecificEnemy;

        CommitPreparedAssignment(
            runtimeState,
            slot,
            owner,
            cardState,
            placementType,
            canRespond ? enemyIntent : null,
            enemyIntent.enemy
        );

        string message = canRespond
            ? "已精确安排到敌人意图"
            : "不具备精确响应资格，已按针对该敌人的单方面行动安排。";

        result = CreateAssignmentSuccess(
            slot,
            eligibility,
            message,
            !canRespond
        );
        return true;
    }

    // TryAssignToEnemy = 把卡牌放到指定敌人的空槽语义
    public static bool TryAssignToEnemy(
        BattleRuntimeState runtimeState,
        CharacterData owner,
        int slotIndex,
        BattleCardState cardState,
        CharacterData enemy,
        out BattleActionAssignmentResult result
    )
    {
        return TryAssignToEnemy(
            runtimeState,
            owner,
            slotIndex,
            cardState,
            enemy,
            1,
            out result
        );
    }

    public static bool TryAssignToEnemy(
        BattleRuntimeState runtimeState,
        CharacterData owner,
        int slotIndex,
        BattleCardState cardState,
        CharacterData enemy,
        int targetEnemySlotIndex,
        out BattleActionAssignmentResult result
    )
    {
        BattleActionSlot slot;
        CardEligibilityResult eligibility;

        if (!ValidateEnemyTarget(runtimeState, enemy, out result))
        {
            return false;
        }

        if (!ValidatePreparedAssignment(
                runtimeState,
                owner,
                slotIndex,
                cardState,
                enemy,
                out slot,
                out eligibility,
                out result))
        {
            return false;
        }

        if (!IsAllowedForEnemyPlacement(cardState))
        {
            result = CreateAssignmentFailure(
                "安排到指定敌人失败：只允许 Attack、Defense 或 Dodge",
                CardEligibilityFailureReason.UnsupportedCondition
            );
            return false;
        }

        CommitPreparedAssignment(
            runtimeState,
            slot,
            owner,
            cardState,
            BattleActionPlacementType.SpecificEnemy,
            null,
            enemy
        );
        slot.requestedTargetSlotIndex =
            Mathf.Max(1, targetEnemySlotIndex);

        result = CreateAssignmentSuccess(slot, eligibility, "已安排到指定敌人", false);
        return true;
    }

    // TryAssignToSelf = 把卡牌放到角色自己的通用判定区域
    public static bool TryAssignToSelf(
        BattleRuntimeState runtimeState,
        CharacterData owner,
        int slotIndex,
        BattleCardState cardState,
        out BattleActionAssignmentResult result
    )
    {
        BattleActionSlot slot;
        CardEligibilityResult eligibility;

        if (!ValidatePreparedAssignment(
                runtimeState,
                owner,
                slotIndex,
                cardState,
                owner,
                out slot,
                out eligibility,
                out result))
        {
            return false;
        }

        if (!IsAllowedForSelfPlacement(cardState))
        {
            result = CreateAssignmentFailure(
                "安排到自身失败：只允许 Defense、Dodge 或 Ability",
                CardEligibilityFailureReason.UnsupportedCondition
            );
            return false;
        }

        CommitPreparedAssignment(
            runtimeState,
            slot,
            owner,
            cardState,
            BattleActionPlacementType.Self,
            null,
            null
        );

        result = CreateAssignmentSuccess(slot, eligibility, "已安排到自身", false);
        return true;
    }

    // TryCancelAssignment = 取消一个 owner + slotIndex 对应的完整准备阶段安排
    public static bool TryCancelAssignment(
        BattleRuntimeState runtimeState,
        CharacterData owner,
        int slotIndex,
        out BattleActionAssignmentResult result
    )
    {
        BattleActionSlot slot;

        if (!ValidatePreparedRuntime(runtimeState, owner, slotIndex, out slot, out result))
        {
            return false;
        }

        if (slot.IsEmpty())
        {
            result = new BattleActionAssignmentResult
            {
                isSuccess = true,
                wasAutoDowngraded = false,
                message = "目标槽位本来就是空槽位，无需取消",
                placementType = BattleActionPlacementType.None,
                effectiveSlotType = slot.slotType,
                eligibilityResult = CardEligibilityResult.Success()
            };
            return true;
        }

        slot.Clear();
        RebuildPreparedActionRoles(runtimeState);

        result = new BattleActionAssignmentResult
        {
            isSuccess = true,
            wasAutoDowngraded = false,
            message = "已取消行动安排",
            placementType = BattleActionPlacementType.None,
            effectiveSlotType = slot.slotType,
            eligibilityResult = CardEligibilityResult.Success()
        };
        return true;
    }

    // RebuildPreparedActionRoles = 从原始放置关系统一重建当前生效槽位类型与主要响应者
    public static void RebuildPreparedActionRoles(BattleRuntimeState runtimeState)
    {
        if (runtimeState == null)
        {
            return;
        }

        RebuildPreparedActionRoles(runtimeState.actionSlots, runtimeState.intentQueue);
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
        CardEligibilityResult result;
        return AssignResponseToEnemyIntent(slots, slotIndex, actor, cardState, enemyIntent, out result);
    }

    public static bool AssignResponseToEnemyIntent(
        List<BattleActionSlot> slots,
        int slotIndex,
        CharacterData actor,
        BattleCardState cardState,
        BattleEnemyIntent enemyIntent,
        out CardEligibilityResult result
    )
    {
        // 先根据槽位编号找到目标槽位。
        BattleActionSlot slot = GetSlot(slots, slotIndex);

        if (slot == null)
        {
            result = CreateFailure(CardEligibilityFailureReason.InvalidSlot, "安排响应行动失败：找不到槽位 " + slotIndex);
            Debug.LogWarning(result.failureMessage);
            return false;
        }

        return AssignResponseToEnemyIntentToSlot(slots, slot, actor, cardState, enemyIntent, out result);
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
        CardEligibilityResult result;
        return AssignResponseToEnemyIntent(slots, owner, slotIndex, actor, cardState, enemyIntent, out result);
    }

    public static bool AssignResponseToEnemyIntent(
        List<BattleActionSlot> slots,
        CharacterData owner,
        int slotIndex,
        CharacterData actor,
        BattleCardState cardState,
        BattleEnemyIntent enemyIntent,
        out CardEligibilityResult result
    )
    {
        BattleActionSlot slot = GetSlot(slots, owner, slotIndex);

        if (slot == null)
        {
            result = CreateFailure(CardEligibilityFailureReason.InvalidSlot, "安排响应行动失败：找不到目标槽位");
            return false;
        }

        return AssignResponseToEnemyIntentToSlot(slots, slot, actor, cardState, enemyIntent, out result);
    }

    // AssignResponseToEnemyIntentToSlot = 对指定槽位安排响应敌人意图
    // 旧 slotIndex 入口和新 owner + slotIndex 入口共用这套逻辑。
    static bool AssignResponseToEnemyIntentToSlot(
        List<BattleActionSlot> slots,
        BattleActionSlot slot,
        CharacterData actor,
        BattleCardState cardState,
        BattleEnemyIntent enemyIntent,
        out CardEligibilityResult result
    )
    {
        // 检查槽位能不能放这张卡。
        // 例如槽位不能为空、同一张卡不能重复安排。
        if (!CanAssignCardToSlot(slots, slot, actor, cardState, out result))
        {
            return false;
        }

        if (IsAbilityCard(cardState))
        {
            result = CreateFailure(
                CardEligibilityFailureReason.UnsupportedCondition,
                "安排响应行动失败：Ability 只能安排到使用者自己的行动槽"
            );
            return false;
        }

        // 响应敌人意图必须有行动者、敌人意图、敌人和原始目标。
        if (actor == null || enemyIntent == null || enemyIntent.enemy == null || enemyIntent.originalTargetCharacter == null)
        {
            result = CreateFailure(CardEligibilityFailureReason.InvalidEnemyIntent, "安排响应行动失败：响应行动数据不完整");
            Debug.LogWarning(result.failureMessage);
            return false;
        }

        result = BattleCardManager.EvaluateCardEligibility(actor, enemyIntent.enemy, cardState);
        if (!result.isEligible)
        {
            Debug.Log(result.failureMessage);
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
            result = CreateFailure(CardEligibilityFailureReason.InvalidEnemyIntent, "速度不足，且不是原目标槽位，无法响应该敌人意图");
            Debug.Log(result.failureMessage);
            return false;
        }

        // 必须在解除旧响应、写入槽位和改写 actualTarget 之前完成资格检查，
        // 避免非法安排留下半完成状态。

        // 记录改写前的目标文本，方便打印“从谁改到谁”。
        string actualTargetBeforeResponse = enemyIntent.GetActualTargetSlotText();

        long nextSequence = GetNextAssignmentSequence(slots);

        // 写入原始精确放置关系，再统一选择最后放置的主要响应者。
        // 更早的合格响应不会被删除，而是保留为该敌人的单方面行动。
        slot.AssignResponse(actor, cardState, enemyIntent, canRewriteActualTarget);
        slot.assignmentSequence = nextSequence;
        RebuildLegacyPreparedActionRoles(slots, enemyIntent);

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

        result = CardEligibilityResult.Success();
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
        CardEligibilityResult result;
        return AssignFreeAction(slots, slotIndex, actor, cardState, target, out result);
    }

    public static bool AssignFreeAction(
        List<BattleActionSlot> slots,
        int slotIndex,
        CharacterData actor,
        BattleCardState cardState,
        CharacterData target,
        out CardEligibilityResult result
    )
    {
        // 先根据槽位编号找到目标槽位。
        BattleActionSlot slot = GetSlot(slots, slotIndex);

        if (slot == null)
        {
            result = CreateFailure(CardEligibilityFailureReason.InvalidSlot, "安排自由行动失败：找不到槽位 " + slotIndex);
            Debug.LogWarning(result.failureMessage);
            return false;
        }

        return AssignFreeActionToSlot(slots, slot, actor, cardState, target, out result);
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
        CardEligibilityResult result;
        return AssignFreeAction(slots, owner, slotIndex, actor, cardState, target, out result);
    }

    public static bool AssignFreeAction(
        List<BattleActionSlot> slots,
        CharacterData owner,
        int slotIndex,
        CharacterData actor,
        BattleCardState cardState,
        CharacterData target,
        out CardEligibilityResult result
    )
    {
        BattleActionSlot slot = GetSlot(slots, owner, slotIndex);

        if (slot == null)
        {
            result = CreateFailure(CardEligibilityFailureReason.InvalidSlot, "安排自由行动失败：找不到目标槽位");
            return false;
        }

        return AssignFreeActionToSlot(slots, slot, actor, cardState, target, out result);
    }

    // AssignFreeActionToSlot = 对指定槽位安排自由行动
    // 旧 slotIndex 入口和新 owner + slotIndex 入口共用这套逻辑。
    static bool AssignFreeActionToSlot(
        List<BattleActionSlot> slots,
        BattleActionSlot slot,
        CharacterData actor,
        BattleCardState cardState,
        CharacterData target,
        out CardEligibilityResult result
    )
    {
        // 检查槽位是否为空、卡牌是否为空、卡牌是否已经被安排过。
        if (!CanAssignCardToSlot(slots, slot, actor, cardState, out result))
        {
            return false;
        }

        // 自由行动必须有行动者。
        if (actor == null)
        {
            result = CreateFailure(CardEligibilityFailureReason.InvalidActor, "安排自由行动失败：行动者为空");
            Debug.LogWarning(result.failureMessage);
            return false;
        }

        if (IsAbilityCard(cardState) &&
            (!object.ReferenceEquals(actor, target) ||
                (slot.owner != null &&
                    !object.ReferenceEquals(slot.owner, actor))))
        {
            result = CreateFailure(
                CardEligibilityFailureReason.UnsupportedCondition,
                "安排自由行动失败：Ability 只能以使用者自己为目标并占用自己的行动槽"
            );
            return false;
        }

        result = BattleCardManager.EvaluateCardEligibility(actor, target, cardState);
        if (!result.isEligible)
        {
            Debug.Log(result.failureMessage);
            return false;
        }

        long nextSequence = GetNextAssignmentSequence(slots);

        // 把自由行动写入槽位，不绑定敌人意图。
        slot.AssignFreeAction(actor, cardState, target);
        slot.assignmentSequence = nextSequence;

        Debug.Log(
            slot.GetDisplaySlotName() +
            " 安排自由行动成功：" +
            slot.GetActorName() +
            " 使用 " +
            slot.GetCardName()
        );

        result = CardEligibilityResult.Success();
        return true;
    }

    // AssignPassiveGuard = owner 版本安排被动守备
    // 被动守备只写入槽位，不绑定敌人意图，不处理 CD / 事件 / 伤害。
    public static bool AssignPassiveGuard(
        List<BattleActionSlot> slots,
        CharacterData owner,
        int slotIndex,
        CharacterData actor,
        BattleCardState cardState
    )
    {
        CardEligibilityResult result;
        return AssignPassiveGuard(slots, owner, slotIndex, actor, cardState, out result);
    }

    public static bool AssignPassiveGuard(
        List<BattleActionSlot> slots,
        CharacterData owner,
        int slotIndex,
        CharacterData actor,
        BattleCardState cardState,
        out CardEligibilityResult result
    )
    {
        if (slots == null)
        {
            result = CreateFailure(CardEligibilityFailureReason.InvalidSlot, "安排被动守备失败：槽位列表为空");
            Debug.LogWarning(result.failureMessage);
            return false;
        }

        if (owner == null)
        {
            result = CreateFailure(CardEligibilityFailureReason.InvalidActor, "安排被动守备失败：owner 为空");
            Debug.LogWarning(result.failureMessage);
            return false;
        }

        if (actor == null)
        {
            result = CreateFailure(CardEligibilityFailureReason.InvalidActor, "安排被动守备失败：行动者为空");
            Debug.LogWarning(result.failureMessage);
            return false;
        }

        if (!object.ReferenceEquals(owner, actor))
        {
            result = CreateFailure(CardEligibilityFailureReason.InvalidActor, "安排被动守备失败：第一版要求 owner 与 actor 相同");
            Debug.LogWarning(result.failureMessage);
            return false;
        }

        if (cardState == null)
        {
            result = CreateFailure(CardEligibilityFailureReason.InvalidCardState, "安排被动守备失败：卡牌状态为空");
            Debug.LogWarning(result.failureMessage);
            return false;
        }

        if (cardState.cardData == null)
        {
            result = CreateFailure(CardEligibilityFailureReason.InvalidCardData, "安排被动守备失败：卡牌数据为空");
            Debug.LogWarning(result.failureMessage);
            return false;
        }

        if (cardState.cardData.cardType != CardType.Defense &&
            cardState.cardData.cardType != CardType.Dodge)
        {
            result = CreateFailure(CardEligibilityFailureReason.UnsupportedCondition, "安排被动守备失败：当前只允许 Defense 或 Dodge，当前卡牌类型：" + cardState.cardData.cardType);
            Debug.LogWarning(result.failureMessage);
            return false;
        }

        BattleActionSlot slot = GetSlot(slots, owner, slotIndex);

        if (slot == null)
        {
            result = CreateFailure(CardEligibilityFailureReason.InvalidSlot, "安排被动守备失败：找不到目标槽位");
            return false;
        }

        if (!CanAssignCardToSlot(slots, slot, actor, cardState, out result))
        {
            return false;
        }

        result = BattleCardManager.EvaluateCardEligibility(actor, actor, cardState);
        if (!result.isEligible)
        {
            Debug.Log(result.failureMessage);
            return false;
        }

        long nextSequence = GetNextAssignmentSequence(slots);
        slot.AssignPassiveGuard(actor, cardState);
        slot.assignmentSequence = nextSequence;

        Debug.Log(
            slot.GetDisplaySlotName() +
            " 安排被动守备成功：" +
            slot.GetActorName() +
            " 使用 " +
            slot.GetCardName()
        );

        result = CardEligibilityResult.Success();
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
                " / 原始放置：" + slot.placementType +
                " / 安排序号：" + slot.assignmentSequence +
                " / 指定敌人：" + GetCharacterName(slot.requestedEnemy) +
                " / 原始意图：" + GetIntentName(slot.requestedEnemyIntent) +
                " / 当前类型：" + slot.slotType +
                " / 当前意图：" + GetIntentName(slot.enemyIntent) +
                " / 主要响应：" + (slot.slotType == BattleActionSlotType.RespondToEnemyIntent && slot.enemyIntent != null) +
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

    static bool ValidatePreparedAssignment(
        BattleRuntimeState runtimeState,
        CharacterData owner,
        int slotIndex,
        BattleCardState cardState,
        CharacterData eligibilityTarget,
        out BattleActionSlot slot,
        out CardEligibilityResult eligibility,
        out BattleActionAssignmentResult result
    )
    {
        eligibility = null;

        if (!ValidatePreparedRuntime(runtimeState, owner, slotIndex, out slot, out result))
        {
            return false;
        }

        if (cardState == null)
        {
            result = CreateAssignmentFailure(
                "准备阶段安排失败：卡牌状态为空",
                CardEligibilityFailureReason.InvalidCardState
            );
            return false;
        }

        if (cardState.cardData == null)
        {
            result = CreateAssignmentFailure(
                "准备阶段安排失败：卡牌数据为空",
                CardEligibilityFailureReason.InvalidCardData
            );
            return false;
        }

        if (!object.ReferenceEquals(cardState.owner, owner))
        {
            result = CreateAssignmentFailure(
                "准备阶段安排失败：卡牌不属于目标槽位 owner",
                CardEligibilityFailureReason.InvalidActor
            );
            return false;
        }

        if (IsCardAssignedToOtherSlot(runtimeState.actionSlots, slot, cardState))
        {
            result = CreateAssignmentFailure(
                "准备阶段安排失败：同一张卡已经安排在其他槽位",
                CardEligibilityFailureReason.CardAlreadyAssigned
            );
            return false;
        }

        if (HasOtherFirstStrikeAssignment(
                runtimeState.actionSlots,
                slot,
                owner,
                cardState))
        {
            result = CreateAssignmentFailure(
                "准备阶段安排失败：同一角色每回合最多只能安排一张 FirstStrike 卡",
                CardEligibilityFailureReason.UnsupportedCondition
            );
            return false;
        }

        eligibility = BattleCardManager.EvaluateCardEligibility(owner, eligibilityTarget, cardState);

        if (!eligibility.isEligible)
        {
            result = CreateAssignmentFailure(eligibility.failureMessage, eligibility);
            return false;
        }

        return true;
    }

    static bool ValidatePreparedRuntime(
        BattleRuntimeState runtimeState,
        CharacterData owner,
        int slotIndex,
        out BattleActionSlot slot,
        out BattleActionAssignmentResult result
    )
    {
        slot = null;

        if (runtimeState == null)
        {
            result = CreateAssignmentFailure(
                "准备阶段安排失败：BattleRuntimeState为空",
                CardEligibilityFailureReason.InvalidSlot
            );
            return false;
        }

        if (runtimeState.IsBattleEnded)
        {
            result = CreateAssignmentFailure(
                "准备阶段安排失败：战斗已经结束",
                CardEligibilityFailureReason.UnsupportedCondition
            );
            return false;
        }

        if (runtimeState.LifecyclePhase != BattleLifecyclePhase.Prepare)
        {
            result = CreateAssignmentFailure(
                "准备阶段安排失败：当前阶段不是Prepare",
                CardEligibilityFailureReason.UnsupportedCondition
            );
            return false;
        }

        if (runtimeState.currentExecutionPlan != null)
        {
            result = CreateAssignmentFailure(
                "准备阶段安排失败：当前已经存在ExecutionPlan",
                CardEligibilityFailureReason.UnsupportedCondition
            );
            return false;
        }

        if (owner == null)
        {
            result = CreateAssignmentFailure(
                "准备阶段安排失败：owner为空",
                CardEligibilityFailureReason.InvalidActor
            );
            return false;
        }

        if (owner.IsDead())
        {
            result = CreateAssignmentFailure(
                "准备阶段安排失败：owner已经死亡",
                CardEligibilityFailureReason.ActorDead
            );
            return false;
        }

        if (!IsCharacterInBattle(runtimeState, owner))
        {
            result = CreateAssignmentFailure(
                "准备阶段安排失败：owner不属于当前战斗",
                CardEligibilityFailureReason.InvalidActor
            );
            return false;
        }

        slot = GetSlot(runtimeState.actionSlots, owner, slotIndex);

        if (slot == null)
        {
            result = CreateAssignmentFailure(
                "准备阶段安排失败：找不到owner对应槽位",
                CardEligibilityFailureReason.InvalidSlot
            );
            return false;
        }

        result = null;
        return true;
    }

    static bool ValidateEnemyIntentTarget(
        BattleRuntimeState runtimeState,
        BattleEnemyIntent enemyIntent,
        out BattleActionAssignmentResult result
    )
    {
        if (runtimeState == null)
        {
            result = CreateAssignmentFailure(
                "安排敌人意图失败：BattleRuntimeState为空",
                CardEligibilityFailureReason.InvalidEnemyIntent
            );
            return false;
        }

        if (enemyIntent == null ||
            runtimeState.intentQueue == null ||
            !ContainsIntentReference(runtimeState.intentQueue, enemyIntent))
        {
            result = CreateAssignmentFailure(
                "安排敌人意图失败：意图不属于当前战斗",
                CardEligibilityFailureReason.InvalidEnemyIntent
            );
            return false;
        }

        if (!ValidateEnemyTarget(runtimeState, enemyIntent.enemy, out result))
        {
            return false;
        }

        if (enemyIntent.originalTargetCharacter == null ||
            !IsCharacterInBattle(runtimeState, enemyIntent.originalTargetCharacter) ||
            enemyIntent.originalTargetCharacter.IsDead())
        {
            result = CreateAssignmentFailure(
                "安排敌人意图失败：原始目标无效或已经死亡",
                CardEligibilityFailureReason.InvalidEnemyIntent
            );
            return false;
        }

        return true;
    }

    static bool ValidateEnemyTarget(
        BattleRuntimeState runtimeState,
        CharacterData enemy,
        out BattleActionAssignmentResult result
    )
    {
        if (runtimeState == null)
        {
            result = CreateAssignmentFailure(
                "安排指定敌人失败：BattleRuntimeState为空",
                CardEligibilityFailureReason.InvalidActor
            );
            return false;
        }

        if (enemy == null || !IsEnemyInBattle(runtimeState, enemy))
        {
            result = CreateAssignmentFailure(
                "安排指定敌人失败：目标不属于当前战斗敌人",
                CardEligibilityFailureReason.InvalidActor
            );
            return false;
        }

        if (enemy.IsDead())
        {
            result = CreateAssignmentFailure(
                "安排指定敌人失败：目标敌人已经死亡",
                CardEligibilityFailureReason.ActorDead
            );
            return false;
        }

        result = null;
        return true;
    }

    static void CommitPreparedAssignment(
        BattleRuntimeState runtimeState,
        BattleActionSlot slot,
        CharacterData owner,
        BattleCardState cardState,
        BattleActionPlacementType placementType,
        BattleEnemyIntent requestedEnemyIntent,
        CharacterData requestedEnemy
    )
    {
        long nextSequence = GetNextAssignmentSequence(runtimeState.actionSlots);

        slot.actor = owner;
        slot.cardState = cardState;
        slot.placementType = placementType;
        slot.requestedEnemyIntent = requestedEnemyIntent;
        slot.requestedEnemy = requestedEnemy;
        slot.requestedTargetSlotIndex = requestedEnemyIntent != null
            ? requestedEnemyIntent.enemySlotIndex
            : 0;
        slot.assignmentSequence = nextSequence;
        slot.target = null;
        slot.enemyIntent = null;
        slot.isUsed = false;

        RebuildPreparedActionRoles(runtimeState);
    }

    static void RebuildPreparedActionRoles(
        List<BattleActionSlot> actionSlots,
        List<BattleEnemyIntent> intentQueue
    )
    {
        if (intentQueue != null)
        {
            foreach (BattleEnemyIntent intent in intentQueue)
            {
                if (intent != null)
                {
                    intent.ResetResponseState();
                }
            }
        }

        if (actionSlots != null)
        {
            foreach (BattleActionSlot slot in actionSlots)
            {
                if (slot == null || slot.IsEmpty())
                {
                    continue;
                }

                EnsureLegacyPlacementMetadata(slot);
                ApplyBasePreparedRole(slot);
            }
        }

        if (intentQueue == null || actionSlots == null)
        {
            return;
        }

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent == null)
            {
                continue;
            }

            BattleActionSlot winner = null;

            foreach (BattleActionSlot slot in actionSlots)
            {
                if (!IsExactResponseCandidate(slot, intent))
                {
                    continue;
                }

                if (winner == null || slot.assignmentSequence > winner.assignmentSequence)
                {
                    winner = slot;
                }
            }

            if (winner == null)
            {
                continue;
            }

            winner.slotType = BattleActionSlotType.RespondToEnemyIntent;
            winner.enemyIntent = intent;
            winner.target = intent.enemy;
            intent.MarkResponded();

            if (winner.actor.GetCurrentSpeed() > intent.enemy.GetCurrentSpeed())
            {
                intent.SetActualTarget(winner.actor, winner.slotIndex);
            }
        }
    }

    static void ApplyBasePreparedRole(BattleActionSlot slot)
    {
        slot.enemyIntent = null;
        slot.isUsed = false;

        if (slot.placementType == BattleActionPlacementType.Self)
        {
            slot.target = slot.owner;
            slot.slotType = IsDefenseOrDodge(slot.cardState)
                ? BattleActionSlotType.PassiveGuard
                : BattleActionSlotType.FreeAction;
            return;
        }

        if (slot.placementType == BattleActionPlacementType.ExactEnemyIntent &&
            slot.requestedEnemy == null &&
            slot.requestedEnemyIntent != null)
        {
            slot.requestedEnemy = slot.requestedEnemyIntent.enemy;
        }

        slot.target = slot.requestedEnemy;
        slot.slotType = IsDefenseOrDodge(slot.cardState)
            ? BattleActionSlotType.EnemySpecificGuard
            : BattleActionSlotType.FreeAction;
    }

    static bool IsExactResponseCandidate(BattleActionSlot slot, BattleEnemyIntent intent)
    {
        if (slot == null ||
            slot.IsEmpty() ||
            slot.placementType != BattleActionPlacementType.ExactEnemyIntent ||
            !object.ReferenceEquals(slot.requestedEnemyIntent, intent) ||
            slot.assignmentSequence <= 0 ||
            slot.owner == null ||
            slot.actor == null ||
            !object.ReferenceEquals(slot.owner, slot.actor) ||
            slot.owner.IsDead() ||
            slot.cardState == null ||
            slot.cardState.cardData == null ||
            !object.ReferenceEquals(slot.cardState.owner, slot.owner) ||
            !IsAllowedForEnemyPlacement(slot.cardState) ||
            intent.enemy == null ||
            intent.enemy.IsDead() ||
            !CanExactlyRespondToEnemyIntent(
                slot,
                slot.owner,
                slot.cardState,
                intent
            ))
        {
            return false;
        }

        CardEligibilityResult eligibility = BattleCardManager.EvaluateCardEligibility(
            slot.owner,
            intent.enemy,
            slot.cardState
        );
        return eligibility.isEligible;
    }

    static bool CanRespondToEnemyIntent(
        BattleActionSlot slot,
        CharacterData owner,
        BattleEnemyIntent enemyIntent
    )
    {
        if (slot == null || owner == null || enemyIntent == null || enemyIntent.enemy == null)
        {
            return false;
        }

        return owner.GetCurrentSpeed() > enemyIntent.enemy.GetCurrentSpeed() ||
            (
                object.ReferenceEquals(owner, enemyIntent.originalTargetCharacter) &&
                slot.slotIndex == enemyIntent.originalTargetSlotIndex
            );
    }

    static bool CanExactlyRespondToEnemyIntent(
        BattleActionSlot slot,
        CharacterData owner,
        BattleCardState candidateCardState,
        BattleEnemyIntent enemyIntent
    )
    {
        if (!CanRespondToEnemyIntent(slot, owner, enemyIntent) ||
            candidateCardState == null ||
            enemyIntent == null ||
            enemyIntent.enemyCardState == null)
        {
            return false;
        }

        BattleInteractionType interactionType = BattleInteractionClassifier.Classify(
            candidateCardState,
            enemyIntent.enemyCardState
        );
        return interactionType == BattleInteractionType.AttackVsAttack ||
            interactionType == BattleInteractionType.AttackVsDefense ||
            interactionType == BattleInteractionType.AttackVsDodge;
    }

    static bool IsAllowedForEnemyPlacement(BattleCardState cardState)
    {
        if (cardState == null || cardState.cardData == null || IsAbilityCard(cardState))
        {
            return false;
        }

        string cardType = cardState.cardData.cardType;
        return cardType == CardType.Attack ||
            cardType == CardType.Defense ||
            cardType == CardType.Dodge;
    }

    static bool IsAllowedForSelfPlacement(BattleCardState cardState)
    {
        if (cardState == null || cardState.cardData == null)
        {
            return false;
        }

        return cardState.cardData.cardType == CardType.Defense ||
            cardState.cardData.cardType == CardType.Dodge ||
            IsAbilityCard(cardState);
    }

    static bool IsAbilityCard(BattleCardState cardState)
    {
        return cardState != null &&
            cardState.cardData != null &&
            (cardState.cardData.cardType == "Ability" || cardState.IsAbilitySinCard());
    }

    static bool IsDefenseOrDodge(BattleCardState cardState)
    {
        return cardState != null &&
            cardState.cardData != null &&
            (cardState.cardData.cardType == CardType.Defense ||
             cardState.cardData.cardType == CardType.Dodge);
    }

    static bool IsCharacterInBattle(BattleRuntimeState runtimeState, CharacterData character)
    {
        if (runtimeState == null || character == null)
        {
            return false;
        }

        if (object.ReferenceEquals(runtimeState.allyA, character) ||
            object.ReferenceEquals(runtimeState.allyB, character) ||
            object.ReferenceEquals(runtimeState.enemy, character))
        {
            return true;
        }

        if (runtimeState.battleUnits == null)
        {
            return false;
        }

        foreach (CharacterData unit in runtimeState.battleUnits)
        {
            if (object.ReferenceEquals(unit, character))
            {
                return true;
            }
        }

        return false;
    }

    static bool IsEnemyInBattle(BattleRuntimeState runtimeState, CharacterData enemy)
    {
        if (!IsCharacterInBattle(runtimeState, enemy))
        {
            return false;
        }

        if (object.ReferenceEquals(runtimeState.allyA, enemy) ||
            object.ReferenceEquals(runtimeState.allyB, enemy))
        {
            return false;
        }

        return runtimeState.ContainsEnemy(enemy);
    }

    static bool ContainsCharacterReference(List<CharacterData> characters, CharacterData target)
    {
        if (characters == null || target == null)
        {
            return false;
        }

        foreach (CharacterData character in characters)
        {
            if (object.ReferenceEquals(character, target))
            {
                return true;
            }
        }

        return false;
    }

    static bool ContainsIntentReference(List<BattleEnemyIntent> intents, BattleEnemyIntent target)
    {
        if (intents == null || target == null)
        {
            return false;
        }

        foreach (BattleEnemyIntent intent in intents)
        {
            if (object.ReferenceEquals(intent, target))
            {
                return true;
            }
        }

        return false;
    }

    static bool IsCardAssignedToOtherSlot(
        List<BattleActionSlot> slots,
        BattleActionSlot targetSlot,
        BattleCardState cardState
    )
    {
        if (slots == null || cardState == null)
        {
            return false;
        }

        foreach (BattleActionSlot slot in slots)
        {
            if (slot == null || object.ReferenceEquals(slot, targetSlot))
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

    static bool HasOtherFirstStrikeAssignment(
        List<BattleActionSlot> slots,
        BattleActionSlot targetSlot,
        CharacterData actor,
        BattleCardState cardState
    )
    {
        if (slots == null || actor == null || cardState == null ||
            !cardState.HasTrait(BattleCardTrait.FirstStrike))
        {
            return false;
        }

        foreach (BattleActionSlot slot in slots)
        {
            if (slot == null || slot.IsEmpty() ||
                object.ReferenceEquals(slot, targetSlot) ||
                !slot.cardState.HasTrait(BattleCardTrait.FirstStrike))
            {
                continue;
            }

            CharacterData assignedActor = slot.actor != null
                ? slot.actor
                : slot.owner;
            if (object.ReferenceEquals(assignedActor, actor))
            {
                return true;
            }
        }

        return false;
    }

    static long GetNextAssignmentSequence(List<BattleActionSlot> slots)
    {
        long maxSequence = 0;

        if (slots != null)
        {
            foreach (BattleActionSlot slot in slots)
            {
                if (slot != null && slot.assignmentSequence > maxSequence)
                {
                    maxSequence = slot.assignmentSequence;
                }
            }
        }

        return maxSequence + 1;
    }

    static void EnsureLegacyPlacementMetadata(BattleActionSlot slot)
    {
        if (slot == null || slot.IsEmpty() || slot.placementType != BattleActionPlacementType.None)
        {
            return;
        }

        if (slot.enemyIntent != null || slot.slotType == BattleActionSlotType.RespondToEnemyIntent)
        {
            slot.placementType = BattleActionPlacementType.ExactEnemyIntent;
            slot.requestedEnemyIntent = slot.enemyIntent;
            slot.requestedEnemy = slot.enemyIntent != null ? slot.enemyIntent.enemy : slot.target;
        }
        else if (slot.slotType == BattleActionSlotType.PassiveGuard ||
                 object.ReferenceEquals(slot.actor, slot.target))
        {
            slot.placementType = BattleActionPlacementType.Self;
            slot.requestedEnemyIntent = null;
            slot.requestedEnemy = null;
        }
        else
        {
            slot.placementType = BattleActionPlacementType.SpecificEnemy;
            slot.requestedEnemyIntent = null;
            slot.requestedEnemy = slot.target;
        }

        if (slot.assignmentSequence <= 0)
        {
            slot.assignmentSequence = 1;
        }
    }

    static void RebuildLegacyPreparedActionRoles(
        List<BattleActionSlot> slots,
        BattleEnemyIntent extraIntent
    )
    {
        List<BattleEnemyIntent> intents = new List<BattleEnemyIntent>();

        AddIntentReferenceIfMissing(intents, extraIntent);

        if (slots != null)
        {
            foreach (BattleActionSlot slot in slots)
            {
                if (slot == null)
                {
                    continue;
                }

                AddIntentReferenceIfMissing(intents, slot.requestedEnemyIntent);
                AddIntentReferenceIfMissing(intents, slot.enemyIntent);
            }
        }

        RebuildPreparedActionRoles(slots, intents);
    }

    static void AddIntentReferenceIfMissing(
        List<BattleEnemyIntent> intents,
        BattleEnemyIntent intent
    )
    {
        if (intents != null && intent != null && !ContainsIntentReference(intents, intent))
        {
            intents.Add(intent);
        }
    }

    static BattleActionAssignmentResult CreateAssignmentSuccess(
        BattleActionSlot slot,
        CardEligibilityResult eligibility,
        string message,
        bool wasAutoDowngraded
    )
    {
        return new BattleActionAssignmentResult
        {
            isSuccess = true,
            wasAutoDowngraded = wasAutoDowngraded,
            message = message,
            placementType = slot != null ? slot.placementType : BattleActionPlacementType.None,
            effectiveSlotType = slot != null ? slot.slotType : BattleActionSlotType.FreeAction,
            eligibilityResult = eligibility ?? CardEligibilityResult.Success()
        };
    }

    static BattleActionAssignmentResult CreateAssignmentFailure(
        string message,
        CardEligibilityFailureReason reason
    )
    {
        return CreateAssignmentFailure(message, CardEligibilityResult.Failure(reason, message));
    }

    static BattleActionAssignmentResult CreateAssignmentFailure(
        string message,
        CardEligibilityResult eligibility
    )
    {
        return new BattleActionAssignmentResult
        {
            isSuccess = false,
            wasAutoDowngraded = false,
            message = message,
            placementType = BattleActionPlacementType.None,
            effectiveSlotType = BattleActionSlotType.FreeAction,
            eligibilityResult = eligibility
        };
    }

    static string GetCharacterName(CharacterData character)
    {
        return character != null ? character.characterName : "无";
    }

    static string GetIntentName(BattleEnemyIntent intent)
    {
        return intent != null ? "意图" + intent.intentOrder : "无";
    }

    // CanAssignCardToSlot = 判断一张卡能不能安排到目标槽位
    // targetSlot = 目标槽位。
    // cardState = 要安排的卡牌状态。
    static bool CanAssignCardToSlot(
        List<BattleActionSlot> slots,
        BattleActionSlot targetSlot,
        CharacterData actor,
        BattleCardState cardState,
        out CardEligibilityResult result
    )
    {
        if (targetSlot == null)
        {
            result = CreateFailure(CardEligibilityFailureReason.InvalidSlot, "安排行动失败：目标槽位为空");
            return false;
        }

        // 目标槽位必须为空。
        if (!targetSlot.IsEmpty())
        {
            result = CreateFailure(CardEligibilityFailureReason.SlotOccupied, targetSlot.GetDisplaySlotName() + " 已经安排了行动");
            Debug.Log(result.failureMessage);
            return false;
        }

        // 卡牌状态不能为空。
        if (cardState == null)
        {
            result = CreateFailure(CardEligibilityFailureReason.InvalidCardState, "安排行动失败：卡牌状态为空");
            Debug.LogWarning(result.failureMessage);
            return false;
        }

        // 同一张 BattleCardState 本回合不能重复安排到多个槽位。
        if (IsCardAlreadyAssigned(slots, cardState))
        {
            result = CreateFailure(CardEligibilityFailureReason.CardAlreadyAssigned, "同一张卡本回合已经被安排");
            Debug.Log(result.failureMessage);
            return false;
        }

        if (HasOtherFirstStrikeAssignment(
                slots,
                targetSlot,
                actor,
                cardState))
        {
            result = CreateFailure(
                CardEligibilityFailureReason.UnsupportedCondition,
                "安排行动失败：同一角色每回合最多只能安排一张 FirstStrike 卡"
            );
            return false;
        }

        result = CardEligibilityResult.Success();
        return true;
    }

    static CardEligibilityResult CreateFailure(CardEligibilityFailureReason reason, string message)
    {
        return CardEligibilityResult.Failure(reason, message);
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
