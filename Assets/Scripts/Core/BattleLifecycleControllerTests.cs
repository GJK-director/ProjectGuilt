using System.Collections.Generic;
using UnityEngine;

public static class BattleLifecycleControllerTests
{
    private sealed class TestContext
    {
        public CharacterData allyA;
        public CharacterData allyB;
        public CharacterData enemy;
        public BattleCardState playerCard;
        public BattleCardState enemyCard;
        public BattleRuntimeState runtimeState;
        public BattleLifecycleController controller;
    }

    public static bool Run()
    {
        bool[] results = new bool[15];
        results[0] = VerifyNullRuntimeSafety();
        results[1] = VerifyInitialization();
        results[2] = VerifyManualPlanCreation();
        results[3] = VerifyAutomaticPlanCreation();
        results[4] = VerifyBothExecutionEntryPhases();
        results[5] = VerifyCompletedPlanReachesTurnResolved();
        results[6] = VerifyIncompletePlanCannotEndTurn();
        results[7] = VerifyEndTurnClearsRuntimeObjects();
        results[8] = VerifyNextTurnAdvancesOnce();
        results[9] = VerifyNextTurnObjectsSaved();
        results[10] = VerifyIllegalOperationsHaveNoSideEffects();
        results[11] = VerifyBattleEndedRejectsOperations();
        results[12] = VerifyVictoryAndDefeat();
        results[13] = VerifyAutomaticFullTurnCycle();
        results[14] = VerifyManualAndAutomaticFinalStateMatch();

        string[] names =
        {
            "null Runtime安全失败",
            "Init只能通过Controller进入Prepare",
            "手动计划创建进入PlanReady",
            "自动计划创建保持Prepare",
            "Prepare和PlanReady均可进入Executing",
            "完整计划执行后进入TurnResolved",
            "未完成计划不能结束回合",
            "完成计划进入TurnEnded并清理回合对象",
            "下一回合只增加一次并回到Prepare",
            "下一回合槽位意图保存且计划为空",
            "非法阶段操作不产生数据副作用",
            "BattleEnded后拒绝所有生命周期操作",
            "Victory和Defeat均通过Controller进入BattleEnded",
            "自动完整回合从回合1进入回合2",
            "手动按钮路径与自动路径最终状态一致"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式77 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }
        Debug.Log("模式77 15项聚合结果：" + allPassed);
        return allPassed;
    }

    private static bool VerifyNullRuntimeSafety()
    {
        BattleLifecycleController controller =
            new BattleLifecycleController(null);
        string failureMessage;
        BattleExecutionPlan plan;
        return controller.RuntimeState == null &&
            !controller.TryInitializeToPrepare(out failureMessage) &&
            !string.IsNullOrEmpty(failureMessage) &&
            !controller.TryCreateExecutionPlan(
                true,
                out plan,
                out failureMessage
            ) && plan == null &&
            !controller.TryExecuteCurrentPlan(out failureMessage) &&
            !controller.TryEndCurrentTurn(out failureMessage) &&
            !controller.TryPrepareNextTurn(
                new List<BattleActionSlot>(),
                new List<BattleEnemyIntent>(),
                out failureMessage
            ) && controller.EvaluateBattleEnd() == BattleResult.None;
    }

    private static bool VerifyInitialization()
    {
        TestContext context = CreateContext(false, "lifecycle77_2");
        string failureMessage;
        bool initialized = context.controller.TryInitializeToPrepare(
            out failureMessage
        );
        bool repeated = context.controller.TryInitializeToPrepare(
            out failureMessage
        );
        return initialized && !repeated &&
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.Prepare;
    }

    private static bool VerifyManualPlanCreation()
    {
        TestContext context = CreateContext(true, "lifecycle77_3");
        BattleExecutionPlan plan;
        string failureMessage;
        return context.controller.TryCreateExecutionPlan(
                true,
                out plan,
                out failureMessage
            ) && plan != null && plan.executionItems.Count > 0 &&
            object.ReferenceEquals(context.runtimeState.currentExecutionPlan, plan) &&
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.PlanReady;
    }

    private static bool VerifyAutomaticPlanCreation()
    {
        TestContext context = CreateContext(true, "lifecycle77_4");
        BattleExecutionPlan plan;
        string failureMessage;
        return context.controller.TryCreateExecutionPlan(
                false,
                out plan,
                out failureMessage
            ) && plan != null && plan.executionItems.Count > 0 &&
            object.ReferenceEquals(context.runtimeState.currentExecutionPlan, plan) &&
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.Prepare;
    }

    private static bool VerifyBothExecutionEntryPhases()
    {
        TestContext prepare = CreateContext(true, "lifecycle77_5_prepare");
        TestContext planReady = CreateContext(true, "lifecycle77_5_ready");
        BattleExecutionPlan preparePlan;
        BattleExecutionPlan readyPlan;
        string failureMessage;
        if (!prepare.controller.TryCreateExecutionPlan(
                false,
                out preparePlan,
                out failureMessage
            ) || !planReady.controller.TryCreateExecutionPlan(
                true,
                out readyPlan,
                out failureMessage
            ))
        {
            return false;
        }

        preparePlan.executionItems[0].MarkFailed(
            BattleExecutionItemOutcomeReason.ResolverFailure
        );
        readyPlan.executionItems[0].MarkFailed(
            BattleExecutionItemOutcomeReason.ResolverFailure
        );
        bool prepareResult = prepare.controller.TryExecuteCurrentPlan(
            out failureMessage
        );
        bool readyResult = planReady.controller.TryExecuteCurrentPlan(
            out failureMessage
        );
        return !prepareResult && !readyResult &&
            prepare.runtimeState.LifecyclePhase == BattleLifecyclePhase.Executing &&
            planReady.runtimeState.LifecyclePhase == BattleLifecyclePhase.Executing;
    }

    private static bool VerifyCompletedPlanReachesTurnResolved()
    {
        TestContext context = CreateContext(true, "lifecycle77_6");
        return CreateAndExecute(context, true) &&
            context.runtimeState.currentExecutionPlan.isCompleted &&
            context.runtimeState.LifecyclePhase ==
                BattleLifecyclePhase.TurnResolved;
    }

    private static bool VerifyIncompletePlanCannotEndTurn()
    {
        TestContext context = CreateContext(true, "lifecycle77_7");
        if (!CreateAndExecute(context, false))
        {
            return false;
        }
        context.runtimeState.currentExecutionPlan.isCompleted = false;
        int slotCount = context.runtimeState.actionSlots.Count;
        int intentCount = context.runtimeState.intentQueue.Count;
        string failureMessage;
        return !context.controller.TryEndCurrentTurn(out failureMessage) &&
            context.runtimeState.LifecyclePhase ==
                BattleLifecyclePhase.TurnResolved &&
            context.runtimeState.actionSlots.Count == slotCount &&
            context.runtimeState.intentQueue.Count == intentCount &&
            context.runtimeState.currentExecutionPlan != null;
    }

    private static bool VerifyEndTurnClearsRuntimeObjects()
    {
        TestContext context = CreateContext(true, "lifecycle77_8");
        if (!CreateAndExecute(context, true))
        {
            return false;
        }
        string failureMessage;
        return context.controller.TryEndCurrentTurn(out failureMessage) &&
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.TurnEnded &&
            context.runtimeState.actionSlots.Count == 0 &&
            context.runtimeState.intentQueue.Count == 0 &&
            context.runtimeState.currentExecutionPlan == null;
    }

    private static bool VerifyNextTurnAdvancesOnce()
    {
        TestContext context = CreateEndedTurnContext("lifecycle77_9");
        if (context == null)
        {
            return false;
        }
        List<BattleActionSlot> slots = CreateNextSlots(context);
        List<BattleEnemyIntent> intents = CreateNextIntents(context, slots);
        string failureMessage;
        bool first = context.controller.TryPrepareNextTurn(
            slots,
            intents,
            out failureMessage
        );
        bool second = context.controller.TryPrepareNextTurn(
            slots,
            intents,
            out failureMessage
        );
        return first && !second && context.runtimeState.currentTurn == 2 &&
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.Prepare;
    }

    private static bool VerifyNextTurnObjectsSaved()
    {
        TestContext context = CreateEndedTurnContext("lifecycle77_10");
        if (context == null)
        {
            return false;
        }
        List<BattleActionSlot> slots = CreateNextSlots(context);
        List<BattleEnemyIntent> intents = CreateNextIntents(context, slots);
        string failureMessage;
        return context.controller.TryPrepareNextTurn(
                slots,
                intents,
                out failureMessage
            ) && context.runtimeState.actionSlots.Count == slots.Count &&
            ContainsAllSlotReferences(context.runtimeState.actionSlots, slots) &&
            slots.Count == 4 && intents.Count == 1 &&
            context.runtimeState.intentQueue.Count == intents.Count &&
            object.ReferenceEquals(context.runtimeState.intentQueue[0], intents[0]) &&
            context.runtimeState.currentExecutionPlan == null;
    }

    private static bool VerifyIllegalOperationsHaveNoSideEffects()
    {
        TestContext context = CreateContext(false, "lifecycle77_11");
        List<BattleActionSlot> slotsBefore = context.runtimeState.actionSlots;
        List<BattleEnemyIntent> intentsBefore = context.runtimeState.intentQueue;
        int turnBefore = context.runtimeState.currentTurn;
        BattleExecutionPlan plan;
        string failureMessage;
        bool create = context.controller.TryCreateExecutionPlan(
            true,
            out plan,
            out failureMessage
        );
        bool execute = context.controller.TryExecuteCurrentPlan(out failureMessage);
        bool end = context.controller.TryEndCurrentTurn(out failureMessage);
        bool prepare = context.controller.TryPrepareNextTurn(
            CreateNextSlots(context),
            new List<BattleEnemyIntent>(),
            out failureMessage
        );
        return !create && !execute && !end && !prepare && plan == null &&
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.Init &&
            context.runtimeState.currentTurn == turnBefore &&
            object.ReferenceEquals(context.runtimeState.actionSlots, slotsBefore) &&
            object.ReferenceEquals(context.runtimeState.intentQueue, intentsBefore) &&
            context.runtimeState.currentExecutionPlan == null;
    }

    private static bool VerifyBattleEndedRejectsOperations()
    {
        TestContext context = CreateContext(true, "lifecycle77_12");
        BattleExecutionPlan plan;
        string failureMessage;
        if (!context.controller.TryCreateExecutionPlan(
                false,
                out plan,
                out failureMessage
            ))
        {
            return false;
        }
        context.enemy.currentHP = 0;
        if (!context.controller.TryExecuteCurrentPlan(out failureMessage) ||
            !context.runtimeState.IsBattleEnded)
        {
            return false;
        }

        int turnBefore = context.runtimeState.currentTurn;
        BattleResult resultBefore = context.runtimeState.battleResult;
        BattleExecutionPlan rejectedPlan;
        return !context.controller.TryInitializeToPrepare(out failureMessage) &&
            !context.controller.TryCreateExecutionPlan(
                true,
                out rejectedPlan,
                out failureMessage
            ) && !context.controller.TryExecuteCurrentPlan(out failureMessage) &&
            !context.controller.TryEndCurrentTurn(out failureMessage) &&
            !context.controller.TryPrepareNextTurn(
                CreateNextSlots(context),
                new List<BattleEnemyIntent>(),
                out failureMessage
            ) && context.controller.EvaluateBattleEnd() == resultBefore &&
            context.runtimeState.currentTurn == turnBefore &&
            context.runtimeState.IsBattleEnded;
    }

    private static bool VerifyVictoryAndDefeat()
    {
        TestContext victory = CreateContext(true, "lifecycle77_13_victory");
        TestContext defeat = CreateContext(true, "lifecycle77_13_defeat");
        BattleExecutionPlan victoryPlan;
        BattleExecutionPlan defeatPlan;
        string failureMessage;
        if (!victory.controller.TryCreateExecutionPlan(
                false,
                out victoryPlan,
                out failureMessage
            ) || !defeat.controller.TryCreateExecutionPlan(
                false,
                out defeatPlan,
                out failureMessage
            ))
        {
            return false;
        }

        victory.enemy.currentHP = 1;
        defeat.allyA.currentHP = 0;
        defeat.allyB.currentHP = 0;
        victory.controller.TryExecuteCurrentPlan(out failureMessage);
        defeat.controller.TryExecuteCurrentPlan(out failureMessage);
        return victory.runtimeState.IsBattleEnded &&
            victory.runtimeState.battleResult == BattleResult.Victory &&
            defeat.runtimeState.IsBattleEnded &&
            defeat.runtimeState.battleResult == BattleResult.Defeat;
    }

    private static bool VerifyAutomaticFullTurnCycle()
    {
        TestContext context = CreateContext(true, "lifecycle77_14");
        BattleAutomaticTurnCycleResult result = BattleAutomaticTurnCycle.TryRun(
            context.runtimeState,
            context.allyA,
            context.allyB,
            context.enemy,
            context.enemyCard
        );
        return result != null && result.isSuccess &&
            result.advancedToNextTurn && context.runtimeState.currentTurn == 2 &&
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.Prepare &&
            context.runtimeState.actionSlots.Count == 4 &&
            context.runtimeState.intentQueue.Count == 1 &&
            context.runtimeState.currentExecutionPlan == null;
    }

    private static bool VerifyManualAndAutomaticFinalStateMatch()
    {
        TestContext manual = CreateContext(true, "lifecycle77_15_manual");
        TestContext automatic = CreateContext(true, "lifecycle77_15_auto");
        if (!CreateAndExecute(manual, true))
        {
            return false;
        }
        string failureMessage;
        if (!manual.controller.TryEndCurrentTurn(out failureMessage))
        {
            return false;
        }
        List<BattleActionSlot> manualSlots = CreateNextSlots(manual);
        if (!manual.controller.TryPrepareNextTurn(
                manualSlots,
                CreateNextIntents(manual, manualSlots),
                out failureMessage
            ))
        {
            return false;
        }

        BattleAutomaticTurnCycleResult autoResult =
            BattleAutomaticTurnCycle.TryRun(
                automatic.runtimeState,
                automatic.allyA,
                automatic.allyB,
                automatic.enemy,
                automatic.enemyCard
            );
        return autoResult != null && autoResult.isSuccess &&
            manual.runtimeState.currentTurn == automatic.runtimeState.currentTurn &&
            manual.runtimeState.LifecyclePhase ==
                automatic.runtimeState.LifecyclePhase &&
            manual.runtimeState.actionSlots.Count ==
                automatic.runtimeState.actionSlots.Count &&
            manual.runtimeState.intentQueue.Count ==
                automatic.runtimeState.intentQueue.Count &&
            manual.runtimeState.currentExecutionPlan == null &&
            automatic.runtimeState.currentExecutionPlan == null;
    }

    private static TestContext CreateEndedTurnContext(string prefix)
    {
        TestContext context = CreateContext(true, prefix);
        if (!CreateAndExecute(context, false))
        {
            return null;
        }
        string failureMessage;
        return context.controller.TryEndCurrentTurn(out failureMessage)
            ? context
            : null;
    }

    private static bool CreateAndExecute(
        TestContext context,
        bool enterPlanReady
    )
    {
        BattleExecutionPlan plan;
        string failureMessage;
        return context.controller.TryCreateExecutionPlan(
                enterPlanReady,
                out plan,
                out failureMessage
            ) && context.controller.TryExecuteCurrentPlan(out failureMessage);
    }

    private static List<BattleActionSlot> CreateNextSlots(TestContext context)
    {
        return BattleActionSlotManager.CreateLivingPartyActionSlots(
            context.allyA,
            context.allyB,
            2
        );
    }

    private static List<BattleEnemyIntent> CreateNextIntents(
        TestContext context,
        List<BattleActionSlot> slots
    )
    {
        return BattleAutomaticTurnCycle.CreateFixedEnemyIntentQueue(
            context.enemy,
            context.enemyCard,
            context.allyA,
            context.allyB,
            slots
        );
    }

    private static bool ContainsAllSlotReferences(
        List<BattleActionSlot> actual,
        List<BattleActionSlot> expected
    )
    {
        if (actual == null || expected == null || actual.Count != expected.Count)
        {
            return false;
        }
        foreach (BattleActionSlot expectedSlot in expected)
        {
            bool found = false;
            foreach (BattleActionSlot actualSlot in actual)
            {
                if (object.ReferenceEquals(actualSlot, expectedSlot))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                return false;
            }
        }
        return true;
    }

    private static TestContext CreateContext(bool initialize, string prefix)
    {
        TestContext context = new TestContext
        {
            allyA = new CharacterData(prefix + "_A", 30, 10, 10),
            allyB = new CharacterData(prefix + "_B", 30, 8, 8),
            enemy = new CharacterData(prefix + "_Enemy", 100, 5, 5)
        };
        context.playerCard = BattleCardManager.CreateBattleCard(
            context.allyA,
            CreateAttackData(prefix + "_player_attack"),
            prefix + "_player_instance"
        );
        context.enemyCard = BattleCardManager.CreateBattleCard(
            context.enemy,
            CreateAttackData("enemy_atk_001"),
            prefix + "_enemy_instance"
        );
        context.runtimeState = new BattleRuntimeState();
        context.runtimeState.SetCharacters(
            context.allyA,
            context.allyB,
            context.enemy
        );
        List<BattleActionSlot> slots =
            BattleActionSlotManager.CreateLivingPartyActionSlots(
                context.allyA,
                context.allyB,
                2
            );
        BattleActionSlotManager.AssignFreeAction(
            slots,
            context.allyA,
            1,
            context.allyA,
            context.playerCard,
            context.enemy
        );
        context.runtimeState.SetActionSlots(slots);
        context.runtimeState.SetIntentQueue(BattleEnemyIntentManager.CreateIntentQueue(
            new BattleEnemyIntent(
                prefix + "_intent",
                context.enemy,
                context.enemyCard,
                context.allyB,
                1,
                1
            )
        ));
        context.controller = new BattleLifecycleController(
            context.runtimeState
        );
        if (initialize)
        {
            string failureMessage;
            context.controller.TryInitializeToPrepare(out failureMessage);
        }
        return context;
    }

    private static CardTestData CreateAttackData(string cardID)
    {
        return new CardTestData
        {
            cardID = cardID,
            cardName = cardID,
            cardType = CardType.Attack,
            isClashable = true,
            minPoint = 1,
            maxPoint = 1,
            cooldown = 0,
            damageFormula = "PointAsDamage"
        };
    }
}
