// 点击交互只把 UI 语义转换为正式准备阶段 API 调用，不复制资格或速度规则。
public static class BattleCardAssignmentRouter
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
        return TryAssignToEnemySlot(
            runtimeState,
            selectedOwner,
            selectedFormalSlotIndex,
            cardOwner,
            cardState,
            targetEnemy,
            boundEnemyIntent,
            1,
            out result
        );
    }

    public static bool TryAssignToEnemySlot(
        BattleRuntimeState runtimeState,
        CharacterData selectedOwner,
        int selectedFormalSlotIndex,
        CharacterData cardOwner,
        BattleCardState cardState,
        CharacterData targetEnemy,
        BattleEnemyIntent boundEnemyIntent,
        int targetEnemySlotIndex,
        out BattleActionAssignmentResult result
    )
    {
        if (!ValidatePlanningRuntime(runtimeState, out result))
        {
            return false;
        }

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
            targetEnemySlotIndex,
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
        if (!ValidatePlanningRuntime(runtimeState, out result))
        {
            return false;
        }

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
                "卡牌安排失败：自身目标不属于当前选中角色",
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
        if (!ValidatePlanningRuntime(runtimeState, out result))
        {
            return false;
        }

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
                "卡牌安排失败：请先选择友方行动槽",
                CardEligibilityFailureReason.InvalidSlot
            );
            return false;
        }

        if (cardState == null)
        {
            result = CreateFailure(
                "卡牌安排失败：卡牌状态为空",
                CardEligibilityFailureReason.InvalidCardState
            );
            return false;
        }

        if (cardOwner == null ||
            !object.ReferenceEquals(cardOwner, selectedOwner) ||
            !object.ReferenceEquals(cardState.owner, selectedOwner))
        {
            result = CreateFailure(
                "卡牌安排失败：卡牌不属于当前选中角色",
                CardEligibilityFailureReason.InvalidActor
            );
            return false;
        }

        result = null;
        return true;
    }

    private static bool ValidatePlanningRuntime(
        BattleRuntimeState runtimeState,
        out BattleActionAssignmentResult result
    )
    {
        if (runtimeState == null)
        {
            result = CreateFailure(
                "卡牌安排失败：BattleRuntimeState为空",
                CardEligibilityFailureReason.InvalidSlot
            );
            return false;
        }

        if (runtimeState.LifecyclePhase == BattleLifecyclePhase.BattleEnded)
        {
            result = CreateFailure(
                "卡牌安排失败：战斗已经结束",
                CardEligibilityFailureReason.UnsupportedCondition
            );
            return false;
        }

        if (runtimeState.LifecyclePhase != BattleLifecyclePhase.Prepare ||
            runtimeState.currentExecutionPlan != null)
        {
            result = CreateFailure(
                "卡牌安排失败：当前不在可编辑的Prepare阶段",
                CardEligibilityFailureReason.UnsupportedCondition
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

// 旧名称只保留给历史模式，所有业务实现都委托给中性 Router。
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
        return BattleCardAssignmentRouter.TryAssignToEnemySlot(
            runtimeState,
            selectedOwner,
            selectedFormalSlotIndex,
            cardOwner,
            cardState,
            targetEnemy,
            boundEnemyIntent,
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
        return BattleCardAssignmentRouter.TryAssignToSelf(
            runtimeState,
            selectedOwner,
            selectedFormalSlotIndex,
            cardOwner,
            cardState,
            selfTarget,
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
        return BattleCardAssignmentRouter.TryCancelSelectedSlot(
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
        return BattleCardAssignmentRouter.IsCardAssigned(
            runtimeState,
            cardState
        );
    }

    public static int EnemySlotIndexToUIIndex(int enemySlotIndex)
    {
        return BattleCardAssignmentRouter.EnemySlotIndexToUIIndex(
            enemySlotIndex
        );
    }
}
