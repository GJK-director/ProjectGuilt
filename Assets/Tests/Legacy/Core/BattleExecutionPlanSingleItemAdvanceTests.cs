// Phase 3.1：验证ExecutionPlan单项推进边界及旧同步执行入口兼容性。
using System.Collections.Generic;
using UnityEngine;

public static class BattleExecutionPlanSingleItemAdvanceTests
{
    private sealed class TestContext
    {
        public CharacterData allyA;
        public CharacterData allyB;
        public CharacterData enemy;
        public BattleRuntimeState runtimeState;
        public BattleLifecycleController lifecycleController;
    }

    public static void Run()
    {
        Debug.Log("===== BattleExecutionPlanSingleItemAdvanceBasic 聚合测试开始 =====");

        bool singleStep = VerifySingleStepProcessesExactlyOneItem();
        bool synchronousCompatibility = VerifySynchronousEntryCompletesPlan();
        bool tieLimitContinues = VerifyTieLimitCompletesAndContinues();
        bool realErrorsStop = VerifyRealErrorsStopPlan();
        bool battleEndedSkipsRemaining = VerifyBattleEndedSkipsRemainingItems();

        Debug.Log("模式79 A 单项入口每次只推进一个Item：" + singleStep);
        Debug.Log("模式79 B 旧同步入口仍完整执行计划：" + synchronousCompatibility);
        Debug.Log("模式79 C TieLimit合法完成并继续后续Item：" + tieLimitContinues);
        Debug.Log("模式79 D Invalid、Unsupported与既有Failed仍停止计划：" + realErrorsStop);
        Debug.Log("模式79 E BattleEnded后剩余Item逐项跳过且计划完成：" + battleEndedSkipsRemaining);
        Debug.Log(
            "模式79 聚合结果：" +
            (singleStep && synchronousCompatibility && tieLimitContinues &&
             realErrorsStop && battleEndedSkipsRemaining)
        );
    }

    private static bool VerifySingleStepProcessesExactlyOneItem()
    {
        TestContext context = CreateContext("single_advance79_a", 100);
        BattleActionSlot firstSlot = CreateFreeAttackSlot(context, 1, "first", 1);
        BattleActionSlot secondSlot = CreateFreeAttackSlot(context, 2, "second", 1);
        BattleActionSlot thirdSlot = CreateFreeAttackSlot(context, 3, "third", 1);
        BattleExecutionPlan plan = CreateFreeActionPlan(firstSlot, secondSlot, thirdSlot);
        context.runtimeState.SetExecutionPlan(plan);

        bool firstResult = context.lifecycleController.TryExecuteNextItem(
            out bool firstPlanCompleted,
            out string firstFailure
        );
        bool firstStepValid = firstResult && string.IsNullOrEmpty(firstFailure) &&
            !firstPlanCompleted && !plan.isCompleted &&
            IsItemState(plan.executionItems[0], BattleExecutionItemStatus.Executed,
                BattleExecutionItemOutcomeReason.None, true) &&
            IsItemState(plan.executionItems[1], BattleExecutionItemStatus.Pending,
                BattleExecutionItemOutcomeReason.None, false) &&
            IsItemState(plan.executionItems[2], BattleExecutionItemStatus.Pending,
                BattleExecutionItemOutcomeReason.None, false) &&
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.Executing;

        bool secondResult = context.lifecycleController.TryExecuteNextItem(
            out bool secondPlanCompleted,
            out string secondFailure
        );
        bool secondStepValid = secondResult && string.IsNullOrEmpty(secondFailure) &&
            !secondPlanCompleted && !plan.isCompleted &&
            IsItemState(plan.executionItems[1], BattleExecutionItemStatus.Executed,
                BattleExecutionItemOutcomeReason.None, true) &&
            IsItemState(plan.executionItems[2], BattleExecutionItemStatus.Pending,
                BattleExecutionItemOutcomeReason.None, false) &&
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.Executing;

        bool thirdResult = context.lifecycleController.TryExecuteNextItem(
            out bool thirdPlanCompleted,
            out string thirdFailure
        );
        bool thirdStepValid = thirdResult && string.IsNullOrEmpty(thirdFailure) &&
            thirdPlanCompleted && plan.isCompleted &&
            IsItemState(plan.executionItems[2], BattleExecutionItemStatus.Executed,
                BattleExecutionItemOutcomeReason.None, true) &&
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.TurnResolved;

        return firstStepValid && secondStepValid && thirdStepValid;
    }

    private static bool VerifySynchronousEntryCompletesPlan()
    {
        TestContext context = CreateContext("single_advance79_b", 100);
        BattleActionSlot firstSlot = CreateFreeAttackSlot(context, 1, "first", 1);
        BattleActionSlot secondSlot = CreateFreeAttackSlot(context, 2, "second", 1);
        BattleExecutionPlan plan = CreateFreeActionPlan(firstSlot, secondSlot);

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(plan);

        return plan.isCompleted && firstSlot.isUsed && secondSlot.isUsed &&
            IsItemState(plan.executionItems[0], BattleExecutionItemStatus.Executed,
                BattleExecutionItemOutcomeReason.None, true) &&
            IsItemState(plan.executionItems[1], BattleExecutionItemStatus.Executed,
                BattleExecutionItemOutcomeReason.None, true);
    }

    private static bool VerifyTieLimitCompletesAndContinues()
    {
        TestContext context = CreateContext("single_advance79_c", 100);
        BattleCardState playerAttack = CreateAttackCard(
            context.allyA,
            "single_advance79_c_player",
            5
        );
        BattleCardState enemyAttack = CreateAttackCard(
            context.enemy,
            "single_advance79_c_enemy",
            5
        );
        BattleEnemyIntent intent = new BattleEnemyIntent(
            "single_advance79_c_intent",
            context.enemy,
            enemyAttack,
            context.allyA,
            1,
            1
        );
        BattleActionSlot responseSlot = new BattleActionSlot(context.allyA, 1);
        responseSlot.AssignResponse(context.allyA, playerAttack, intent, false);
        BattleExecutionItem tieItem = new BattleExecutionItem(
            1,
            BattleExecutionItemType.RespondedEnemyIntent,
            intent,
            responseSlot
        );
        BattleActionSlot followSlot = CreateFreeAttackSlot(context, 2, "follow", 1);
        BattleExecutionItem followItem = new BattleExecutionItem(
            2,
            BattleExecutionItemType.FreeAction,
            null,
            followSlot
        );
        BattleExecutionPlan plan = CreatePlan(tieItem, followItem);

        int playerUseCountBefore = playerAttack.currentUseCount;
        int enemyUseCountBefore = enemyAttack.currentUseCount;
        int playerCooldownBefore = playerAttack.currentCooldown;
        int enemyCooldownBefore = enemyAttack.currentCooldown;

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(plan);

        return plan.isCompleted &&
            IsItemState(tieItem, BattleExecutionItemStatus.Executed,
                BattleExecutionItemOutcomeReason.TieLimitReached, true) &&
            IsItemState(followItem, BattleExecutionItemStatus.Executed,
                BattleExecutionItemOutcomeReason.None, true) &&
            !responseSlot.isUsed && followSlot.isUsed &&
            playerAttack.currentUseCount == playerUseCountBefore &&
            enemyAttack.currentUseCount == enemyUseCountBefore &&
            playerAttack.currentCooldown == playerCooldownBefore &&
            enemyAttack.currentCooldown == enemyCooldownBefore;
    }

    private static bool VerifyRealErrorsStopPlan()
    {
        TestContext invalidContext = CreateContext("single_advance79_d_invalid", 100);
        BattleActionSlot invalidSlot = CreateFreeAttackSlot(
            invalidContext,
            1,
            "invalid",
            1
        );
        invalidSlot.actor = null;
        BattleActionSlot invalidFollowSlot = CreateFreeAttackSlot(
            invalidContext,
            2,
            "follow",
            1
        );
        BattleExecutionPlan invalidPlan = CreateFreeActionPlan(
            invalidSlot,
            invalidFollowSlot
        );
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(invalidPlan);
        bool invalidStops =
            IsItemState(invalidPlan.executionItems[0],
                BattleExecutionItemStatus.Failed,
                BattleExecutionItemOutcomeReason.InvalidData,
                false) &&
            IsItemState(invalidPlan.executionItems[1],
                BattleExecutionItemStatus.Pending,
                BattleExecutionItemOutcomeReason.None,
                false) &&
            !invalidPlan.isCompleted && !invalidFollowSlot.isUsed;

        TestContext unsupportedContext = CreateContext(
            "single_advance79_d_unsupported",
            100
        );
        BattleActionSlot unsupportedSlot = new BattleActionSlot(
            unsupportedContext.allyA,
            1
        );
        unsupportedSlot.AssignFreeAction(
            unsupportedContext.allyA,
            CreateCard(unsupportedContext.allyA, "unsupported", CardType.Defense, 1),
            unsupportedContext.enemy
        );
        BattleActionSlot unsupportedFollowSlot = CreateFreeAttackSlot(
            unsupportedContext,
            2,
            "follow",
            1
        );
        BattleExecutionPlan unsupportedPlan = CreateFreeActionPlan(
            unsupportedSlot,
            unsupportedFollowSlot
        );
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(unsupportedPlan);
        bool unsupportedStops =
            IsItemState(unsupportedPlan.executionItems[0],
                BattleExecutionItemStatus.Failed,
                BattleExecutionItemOutcomeReason.UnsupportedResolveType,
                false) &&
            IsItemState(unsupportedPlan.executionItems[1],
                BattleExecutionItemStatus.Pending,
                BattleExecutionItemOutcomeReason.None,
                false) &&
            !unsupportedPlan.isCompleted && !unsupportedFollowSlot.isUsed;

        TestContext failedContext = CreateContext("single_advance79_d_failed", 100);
        BattleActionSlot failedSlot = CreateFreeAttackSlot(failedContext, 1, "failed", 1);
        BattleActionSlot failedFollowSlot = CreateFreeAttackSlot(
            failedContext,
            2,
            "follow",
            1
        );
        BattleExecutionPlan failedPlan = CreateFreeActionPlan(
            failedSlot,
            failedFollowSlot
        );
        failedPlan.executionItems[0].MarkFailed(
            BattleExecutionItemOutcomeReason.ResolverFailure
        );
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(failedPlan);
        bool existingFailedStops =
            IsItemState(failedPlan.executionItems[0],
                BattleExecutionItemStatus.Failed,
                BattleExecutionItemOutcomeReason.ResolverFailure,
                false) &&
            IsItemState(failedPlan.executionItems[1],
                BattleExecutionItemStatus.Pending,
                BattleExecutionItemOutcomeReason.None,
                false) &&
            !failedPlan.isCompleted && !failedFollowSlot.isUsed;

        return invalidStops && unsupportedStops && existingFailedStops;
    }

    private static bool VerifyBattleEndedSkipsRemainingItems()
    {
        TestContext context = CreateContext("single_advance79_e", 1);
        BattleActionSlot killSlot = CreateFreeAttackSlot(context, 1, "kill", 10);
        BattleActionSlot followSlot = CreateFreeAttackSlot(context, 2, "follow", 10);
        BattleExecutionPlan plan = CreateFreeActionPlan(killSlot, followSlot);
        context.runtimeState.SetExecutionPlan(plan);

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(
            plan,
            context.runtimeState
        );

        return context.runtimeState.IsBattleEnded &&
            context.runtimeState.battleResult == BattleResult.Victory &&
            plan.isCompleted && killSlot.isUsed && !followSlot.isUsed &&
            IsItemState(plan.executionItems[0], BattleExecutionItemStatus.Executed,
                BattleExecutionItemOutcomeReason.None, true) &&
            IsItemState(plan.executionItems[1], BattleExecutionItemStatus.Skipped,
                BattleExecutionItemOutcomeReason.BattleEnded, true);
    }

    private static TestContext CreateContext(string prefix, int enemyHP)
    {
        TestContext context = new TestContext
        {
            allyA = new CharacterData(prefix + "_A", 30, 10, 10),
            allyB = new CharacterData(prefix + "_B", 30, 8, 8),
            enemy = new CharacterData(prefix + "_Enemy", enemyHP, 5, 5),
            runtimeState = new BattleRuntimeState()
        };
        context.runtimeState.SetCharacters(
            context.allyA,
            context.allyB,
            context.enemy
        );
        context.lifecycleController = new BattleLifecycleController(
            context.runtimeState
        );
        context.lifecycleController.TryInitializeToPrepare(out string failureMessage);
        return context;
    }

    private static BattleActionSlot CreateFreeAttackSlot(
        TestContext context,
        int slotIndex,
        string suffix,
        int point
    )
    {
        BattleActionSlot slot = new BattleActionSlot(context.allyA, slotIndex);
        slot.AssignFreeAction(
            context.allyA,
            CreateAttackCard(context.allyA, suffix, point),
            context.enemy
        );
        return slot;
    }

    private static BattleCardState CreateAttackCard(
        CharacterData owner,
        string id,
        int point
    )
    {
        return CreateCard(owner, id, CardType.Attack, point);
    }

    private static BattleCardState CreateCard(
        CharacterData owner,
        string id,
        string cardType,
        int point
    )
    {
        CardTestData cardData = new CardTestData
        {
            cardID = id + "_data",
            cardName = id,
            cardType = cardType,
            isSinCard = false,
            isClashable = cardType == CardType.Attack,
            minPoint = point,
            maxPoint = point,
            cooldown = 0,
            damageFormula = "PointAsDamage",
            effects = new List<CardEffectData>()
        };
        return BattleCardManager.CreateBattleCard(owner, cardData, id + "_instance");
    }

    private static BattleExecutionPlan CreateFreeActionPlan(
        params BattleActionSlot[] slots
    )
    {
        BattleExecutionPlan plan = new BattleExecutionPlan();
        for (int index = 0; index < slots.Length; index++)
        {
            plan.AddItem(new BattleExecutionItem(
                index + 1,
                BattleExecutionItemType.FreeAction,
                null,
                slots[index]
            ));
        }
        return plan;
    }

    private static BattleExecutionPlan CreatePlan(
        params BattleExecutionItem[] items
    )
    {
        BattleExecutionPlan plan = new BattleExecutionPlan();
        foreach (BattleExecutionItem item in items)
        {
            plan.AddItem(item);
        }
        return plan;
    }

    private static bool IsItemState(
        BattleExecutionItem item,
        BattleExecutionItemStatus status,
        BattleExecutionItemOutcomeReason reason,
        bool isCompleted
    )
    {
        return item != null && item.status == status &&
            item.outcomeReason == reason && item.isCompleted == isCompleted;
    }
}
