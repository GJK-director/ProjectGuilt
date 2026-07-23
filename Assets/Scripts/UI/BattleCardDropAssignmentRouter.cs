// 拖放路由只把 UI 语义转换为正式准备阶段 API 调用，不复制资格或速度规则。
public static class BattleCardDropAssignmentRouter
{
    public static bool TryAssignToEnemySlot(
        BattleRuntimeState runtimeState,
        CharacterData selectedOwner,
        int selectedFormalSlotIndex,
        CharacterData cardOwner,
        BattleCardState cardState,
        CharacterData targetEnemy,
        BattleEnemyIntent boundEnemyIntent,
        out BattleActionAssignmentResult result
    )
    {
        if (!ValidateSelectedCard(
                selectedOwner,
                selectedFormalSlotIndex,
                cardOwner,
                cardState,
                out result))
        {
            return false;
        }

        if (boundEnemyIntent != null)
        {
            return BattleActionSlotManager.TryAssignToEnemyIntent(
                runtimeState,
                selectedOwner,
                selectedFormalSlotIndex,
                cardState,
                boundEnemyIntent,
                out result
            );
        }

        return BattleActionSlotManager.TryAssignToEnemy(
            runtimeState,
            selectedOwner,
            selectedFormalSlotIndex,
            cardState,
            targetEnemy,
            out result
        );
    }

    public static bool TryAssignToSelf(
        BattleRuntimeState runtimeState,
        CharacterData selectedOwner,
        int selectedFormalSlotIndex,
        CharacterData cardOwner,
        BattleCardState cardState,
        CharacterData selfTarget,
        out BattleActionAssignmentResult result
    )
    {
        if (!ValidateSelectedCard(
                selectedOwner,
                selectedFormalSlotIndex,
                cardOwner,
                cardState,
                out result))
        {
            return false;
        }

        if (selfTarget == null || !object.ReferenceEquals(selectedOwner, selfTarget))
        {
            result = CreateFailure(
                "拖放安排失败：自身放置区不属于当前选中角色",
                CardEligibilityFailureReason.InvalidActor
            );
            return false;
        }

        return BattleActionSlotManager.TryAssignToSelf(
            runtimeState,
            selectedOwner,
            selectedFormalSlotIndex,
            cardState,
            out result
        );
    }

    public static bool TryCancelSelectedSlot(
        BattleRuntimeState runtimeState,
        CharacterData owner,
        int formalSlotIndex,
        out BattleActionAssignmentResult result
    )
    {
        if (owner == null || formalSlotIndex <= 0)
        {
            result = CreateFailure(
                "取消安排失败：没有有效的角色行动槽",
                CardEligibilityFailureReason.InvalidSlot
            );
            return false;
        }

        return BattleActionSlotManager.TryCancelAssignment(
            runtimeState,
            owner,
            formalSlotIndex,
            out result
        );
    }

    public static bool IsCardAssigned(
        BattleRuntimeState runtimeState,
        BattleCardState cardState
    )
    {
        if (runtimeState == null ||
            runtimeState.actionSlots == null ||
            cardState == null)
        {
            return false;
        }

        foreach (BattleActionSlot slot in runtimeState.actionSlots)
        {
            if (slot != null &&
                !slot.IsEmpty() &&
                object.ReferenceEquals(slot.cardState, cardState))
            {
                return true;
            }
        }

        return false;
    }

    public static int EnemySlotIndexToUIIndex(int enemySlotIndex)
    {
        return enemySlotIndex >= 1 && enemySlotIndex <= 2
            ? enemySlotIndex - 1
            : -1;
    }

    private static bool ValidateSelectedCard(
        CharacterData selectedOwner,
        int selectedFormalSlotIndex,
        CharacterData cardOwner,
        BattleCardState cardState,
        out BattleActionAssignmentResult result
    )
    {
        if (selectedOwner == null || selectedFormalSlotIndex <= 0)
        {
            result = CreateFailure(
                "拖放安排失败：请先选择友方行动槽",
                CardEligibilityFailureReason.InvalidSlot
            );
            return false;
        }

        if (cardState == null)
        {
            result = CreateFailure(
                "拖放安排失败：卡牌状态为空",
                CardEligibilityFailureReason.InvalidCardState
            );
            return false;
        }

        if (cardOwner == null ||
            !object.ReferenceEquals(cardOwner, selectedOwner) ||
            !object.ReferenceEquals(cardState.owner, selectedOwner))
        {
            result = CreateFailure(
                "拖放安排失败：卡牌不属于当前选中角色",
                CardEligibilityFailureReason.InvalidActor
            );
            return false;
        }

        result = null;
        return true;
    }

    private static BattleActionAssignmentResult CreateFailure(
        string message,
        CardEligibilityFailureReason reason
    )
    {
        CardEligibilityResult eligibility = CardEligibilityResult.Failure(reason, message);
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
}

// 仅封装拖放刷新标记，便于验证 Drop 与 DragEnd 之间不会提前重建手牌。
public static class BattleCardDragRefreshUtility
{
    public static void MarkPending(
        ref bool pending,
        ref bool pendingSuccessfulAssignment,
        bool assignmentSucceeded
    )
    {
        pending = true;
        pendingSuccessfulAssignment = assignmentSucceeded;
    }

    public static bool ConsumePending(
        ref bool pending,
        ref bool pendingSuccessfulAssignment,
        out bool assignmentSucceeded
    )
    {
        assignmentSucceeded = false;

        if (!pending)
        {
            return false;
        }

        assignmentSucceeded = pendingSuccessfulAssignment;
        pending = false;
        pendingSuccessfulAssignment = false;
        return true;
    }

    public static void ClearSelectedActionSlot(
        ref CharacterData selectedCharacter,
        ref int selectedActionSlotIndex,
        ref BattleActionSlotUIView selectedActionSlotView
    )
    {
        if (selectedActionSlotView != null)
        {
            selectedActionSlotView.SetSelected(false);
        }

        selectedCharacter = null;
        selectedActionSlotIndex = -1;
        selectedActionSlotView = null;
    }
}
