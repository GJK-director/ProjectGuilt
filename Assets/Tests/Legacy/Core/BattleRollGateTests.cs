// Phase 3.3：验证可暂停Clash执行、Manual请求和Auto计时契约。
using System.Collections.Generic;
using UnityEngine;

public static class BattleRollGateTests
{
    sealed class TestContext
    {
        public CharacterData ally;
        public CharacterData allyB;
        public CharacterData enemy;
        public BattleCardState playerCard;
        public BattleCardState enemyCard;
        public BattleActionSlot responseSlot;
        public BattleEnemyIntent intent;
        public BattleExecutionItem item;
        public BattleExecutionPlan plan;
        public BattleRuntimeState runtimeState;
        public BattleLifecycleController controller;
    }

    public static void Run()
    {
        Debug.Log("===== BattleRollGateBasic 聚合测试开始 =====");

        bool a = VerifyManualReadyPauseBlocksEarlyRoll();
        bool b = VerifyEarlyManualRequestIsNotBuffered();
        bool c = VerifyManualCanWaitIndefinitely();
        bool d = VerifyOneManualRequestRollsOnce();
        bool e = VerifyAttackTieNeedsSecondRequest();
        bool f = VerifyAutoWaitsReadyAndDelay();
        bool g = VerifyAutoTieRepeatsBothWaits();
        bool h = VerifyFinalizedCommitsOnlyOnce();
        bool i = VerifyLifecycleStaysExecutingWhileWaiting();
        bool j = VerifyNonCombatItemsRemainSynchronousAndUnilateralWaits();
        bool k = VerifyAttackTieLimitCompletesNormally();
        bool l = VerifyDefenseUsesRollGateAndEqualityFullBlocks();
        bool m = VerifyDodgeUsesRollGateAndEqualitySucceeds();
        bool n = VerifyBattleEndedStillSkipsRemainingItems();

        Debug.Log("模式81 A Manual ReadyPause阻止提前Roll：" + a);
        Debug.Log("模式81 B Manual提前请求不预存：" + b);
        Debug.Log("模式81 C Manual WaitingForRoll可无限等待：" + c);
        Debug.Log("模式81 D 一次Manual请求只产生一个Attempt：" + d);
        Debug.Log("模式81 E AttackTie需要第二次新请求：" + e);
        Debug.Log("模式81 F Auto等待ReadyPause与AutoRollDelay：" + f);
        Debug.Log("模式81 G Auto AttackTie重新经过两段等待：" + g);
        Debug.Log("模式81 H Finalized后只提交一次：" + h);
        Debug.Log("模式81 I 等待期间Lifecycle保持Executing：" + i);
        Debug.Log("模式81 J 非Combat同步且Unilateral进入Generic Roll Gate：" + j);
        Debug.Log("模式81 K Attack第10次平局进入TieLimit并完成：" + k);
        Debug.Log("模式81 L Defense经过RollGate且相等时FullBlock：" + l);
        Debug.Log("模式81 M Dodge经过RollGate且相等时成功：" + m);
        Debug.Log("模式81 N BattleEnded后剩余Item沿用正式跳过语义：" + n);
        Debug.Log(
            "模式81 聚合结果：" +
            (a && b && c && d && e && f && g && h && i && j && k && l && m && n)
        );
    }

    static bool VerifyManualReadyPauseBlocksEarlyRoll()
    {
        TestContext context = CreateRespondedAttackContext("roll81_a", 6, 4);
        bool began = Begin(context, BattleRollMode.Manual, 1f, 0.5f);
        bool advanced = context.controller.AdvancePausableExecution(
            0.5f,
            out string failure
        );

        return began && advanced && string.IsNullOrEmpty(failure) &&
            context.controller.ExecutionRunner.Phase ==
                BattleExecutionRunnerPhase.ClashReadyPause &&
            context.controller.ExecutionRunner.CurrentClashSession.AttemptIndex == 0 &&
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.Executing &&
            !context.item.isCompleted;
    }

    static bool VerifyEarlyManualRequestIsNotBuffered()
    {
        TestContext context = CreateRespondedAttackContext("roll81_b", 6, 4);
        bool began = Begin(context, BattleRollMode.Manual, 1f, 0.5f);
        bool earlyRejected = !context.controller.TryRequestManualRoll(
            out string earlyFailure
        );
        bool pauseFinished = context.controller.AdvancePausableExecution(
            1f,
            out string advanceFailure
        );
        BattleExecutionRunner runner = context.controller.ExecutionRunner;
        bool stillWaiting = pauseFinished &&
            runner.Phase == BattleExecutionRunnerPhase.WaitingForRoll &&
            runner.CurrentClashSession.AttemptIndex == 0;
        bool freshRequest = context.controller.TryRequestManualRoll(
            out string requestFailure
        );
        bool committed = CommitPendingResolution(context);

        return began && earlyRejected && !string.IsNullOrEmpty(earlyFailure) &&
            string.IsNullOrEmpty(advanceFailure) && stillWaiting &&
            freshRequest && committed && string.IsNullOrEmpty(requestFailure) &&
            runner.CurrentClashSession.AttemptIndex == 1 && runner.IsCompleted;
    }

    static bool VerifyManualCanWaitIndefinitely()
    {
        TestContext context = CreateRespondedAttackContext("roll81_c", 6, 4);
        bool began = Begin(context, BattleRollMode.Manual, 1f, 0.5f);
        bool enteredWaiting = context.controller.AdvancePausableExecution(
            1f,
            out string firstFailure
        );
        bool waited = context.controller.AdvancePausableExecution(
            100f,
            out string waitFailure
        );
        BattleExecutionRunner runner = context.controller.ExecutionRunner;

        return began && enteredWaiting && waited &&
            string.IsNullOrEmpty(firstFailure) &&
            string.IsNullOrEmpty(waitFailure) &&
            runner.Phase == BattleExecutionRunnerPhase.WaitingForRoll &&
            runner.CurrentClashSession.AttemptIndex == 0 &&
            !context.item.isCompleted;
    }

    static bool VerifyOneManualRequestRollsOnce()
    {
        TestContext context = CreateRespondedAttackContext("roll81_d", 5, 5);
        bool began = Begin(context, BattleRollMode.Manual, 0f, 0f);
        bool requested = context.controller.TryRequestManualRoll(
            out string requestFailure
        );
        BattleExecutionRunner runner = context.controller.ExecutionRunner;
        bool waited = context.controller.AdvancePausableExecution(
            100f,
            out string waitFailure
        );

        return began && requested && waited &&
            string.IsNullOrEmpty(requestFailure) &&
            string.IsNullOrEmpty(waitFailure) &&
            runner.CurrentClashSession.AttemptIndex == 1 &&
            runner.CurrentClashSession.AttackTieCount == 1 &&
            runner.Phase == BattleExecutionRunnerPhase.WaitingForRoll &&
            !context.item.isCompleted;
    }

    static bool VerifyAttackTieNeedsSecondRequest()
    {
        TestContext context = CreateRespondedAttackContext("roll81_e", 5, 5);
        bool began = Begin(context, BattleRollMode.Manual, 0f, 0f);
        bool firstRequest = context.controller.TryRequestManualRoll(
            out string firstFailure
        );
        BattleClashSession session = context.controller.ExecutionRunner
            .CurrentClashSession;
        bool firstAttemptOnly = firstRequest && session.AttemptIndex == 1 &&
            session.AttackTieCount == 1 && !session.IsFinalized;
        bool firstResultShown = context.controller.AdvancePausableExecution(
            0f,
            out string firstResultFailure
        );

        SetNextAttackPoints(session, 6, 4);
        bool secondRequest = context.controller.TryRequestManualRoll(
            out string secondFailure
        );
        bool secondResultShown = context.controller.AdvancePausableExecution(
            0f,
            out string secondResultFailure
        );
        bool resolutionPending = context.controller.ExecutionRunner.Phase ==
            BattleExecutionRunnerPhase.ResolutionPending &&
            !context.item.isCompleted;
        bool committed = CommitPendingResolution(context);

        return began && firstAttemptOnly && firstResultShown && secondRequest &&
            secondResultShown && resolutionPending && committed &&
            string.IsNullOrEmpty(firstFailure) &&
            string.IsNullOrEmpty(firstResultFailure) &&
            string.IsNullOrEmpty(secondFailure) &&
            string.IsNullOrEmpty(secondResultFailure) &&
            session.AttemptIndex == 2 && session.IsFinalized &&
            session.FinalResult == BattleClashFinalResult.SideAWin &&
            context.item.isCompleted && context.plan.isCompleted;
    }

    static bool VerifyAutoWaitsReadyAndDelay()
    {
        TestContext context = CreateRespondedAttackContext("roll81_f", 6, 4);
        bool began = Begin(context, BattleRollMode.Auto, 1f, 0.5f);
        BattleExecutionRunner runner = context.controller.ExecutionRunner;
        bool first = context.controller.AdvancePausableExecution(
            0.9f,
            out string firstFailure
        );
        bool beforeReadyEnd = first && runner.CurrentClashSession.AttemptIndex == 0 &&
            runner.Phase == BattleExecutionRunnerPhase.ClashReadyPause;
        bool second = context.controller.AdvancePausableExecution(
            0.1f,
            out string secondFailure
        );
        bool readyEnded = second && runner.CurrentClashSession.AttemptIndex == 0 &&
            runner.Phase == BattleExecutionRunnerPhase.AutoRollDelay;
        bool third = context.controller.AdvancePausableExecution(
            0.49f,
            out string thirdFailure
        );
        bool delayNotDone = third && runner.CurrentClashSession.AttemptIndex == 0;
        bool fourth = context.controller.AdvancePausableExecution(
            0.01f,
            out string fourthFailure
        );
        bool rollResultShown = context.controller.AdvancePausableExecution(
            0f,
            out string rollResultFailure
        );
        bool resolutionPending = runner.Phase ==
            BattleExecutionRunnerPhase.ResolutionPending;
        bool committed = CommitPendingResolution(context);

        return began && beforeReadyEnd && readyEnded && delayNotDone && fourth &&
            rollResultShown && resolutionPending && committed &&
            string.IsNullOrEmpty(firstFailure) &&
            string.IsNullOrEmpty(secondFailure) &&
            string.IsNullOrEmpty(thirdFailure) &&
            string.IsNullOrEmpty(fourthFailure) &&
            string.IsNullOrEmpty(rollResultFailure) &&
            runner.CurrentClashSession.AttemptIndex == 1 && runner.IsCompleted;
    }

    static bool VerifyAutoTieRepeatsBothWaits()
    {
        TestContext context = CreateRespondedAttackContext("roll81_g", 5, 5);
        bool began = Begin(context, BattleRollMode.Auto, 1f, 0.5f);
        BattleExecutionRunner runner = context.controller.ExecutionRunner;
        context.controller.AdvancePausableExecution(1f, out string firstFailure);
        bool firstRoll = context.controller.AdvancePausableExecution(
            0.5f,
            out string secondFailure
        );
        BattleClashSession session = runner.CurrentClashSession;
        bool firstResultShown = context.controller.AdvancePausableExecution(
            0f,
            out string firstResultFailure
        );
        bool tieReturnedToReady = firstRoll && firstResultShown &&
            session.AttemptIndex == 1 &&
            session.AttackTieCount == 1 &&
            runner.Phase == BattleExecutionRunnerPhase.ClashReadyPause;

        SetNextAttackPoints(session, 6, 4);
        bool secondReady = context.controller.AdvancePausableExecution(
            1f,
            out string thirdFailure
        );
        bool noEarlySecondRoll = secondReady && session.AttemptIndex == 1 &&
            runner.Phase == BattleExecutionRunnerPhase.AutoRollDelay;
        bool secondRoll = context.controller.AdvancePausableExecution(
            0.5f,
            out string fourthFailure
        );
        bool secondResultShown = context.controller.AdvancePausableExecution(
            0f,
            out string secondResultFailure
        );
        bool resolutionPending = runner.Phase ==
            BattleExecutionRunnerPhase.ResolutionPending;
        bool committed = CommitPendingResolution(context);

        return began && tieReturnedToReady && noEarlySecondRoll && secondRoll &&
            secondResultShown && resolutionPending && committed &&
            string.IsNullOrEmpty(firstFailure) &&
            string.IsNullOrEmpty(secondFailure) &&
            string.IsNullOrEmpty(thirdFailure) &&
            string.IsNullOrEmpty(fourthFailure) &&
            string.IsNullOrEmpty(firstResultFailure) &&
            string.IsNullOrEmpty(secondResultFailure) &&
            session.AttemptIndex == 2 && session.IsFinalized && runner.IsCompleted;
    }

    static bool VerifyFinalizedCommitsOnlyOnce()
    {
        TestContext context = CreateRespondedAttackContext("roll81_h", 6, 4);
        context.ally.AddBuff("NextClashPointUp", 2, 1);
        int hpBefore = context.enemy.currentHP;
        int useCountBefore = context.playerCard.currentUseCount;
        bool began = Begin(context, BattleRollMode.Manual, 0f, 0f);
        bool requested = context.controller.TryRequestManualRoll(
            out string requestFailure
        );
        int hpAfterCalculate = context.enemy.currentHP;
        int buffAfterCalculate = context.ally.GetBuffStack("NextClashPointUp");
        bool committed = CommitPendingResolution(context);
        int hpAfterFinalize = context.enemy.currentHP;
        int useCountAfterFinalize = context.playerCard.currentUseCount;
        int buffAfterFinalize = context.ally.GetBuffStack("NextClashPointUp");
        bool extraAdvance = context.controller.AdvancePausableExecution(
            100f,
            out string advanceFailure
        );
        bool extraRequestRejected = !context.controller.TryRequestManualRoll(
            out string extraRequestFailure
        );

        return began && requested && committed && extraAdvance &&
            extraRequestRejected && hpAfterCalculate == hpBefore &&
            buffAfterCalculate == 2 &&
            string.IsNullOrEmpty(requestFailure) &&
            string.IsNullOrEmpty(advanceFailure) &&
            !string.IsNullOrEmpty(extraRequestFailure) &&
            hpAfterFinalize == hpBefore - 8 &&
            context.enemy.currentHP == hpAfterFinalize &&
            useCountAfterFinalize == useCountBefore &&
            context.playerCard.currentUseCount == useCountAfterFinalize &&
            buffAfterFinalize == 0 &&
            context.ally.GetBuffStack("NextClashPointUp") == 0 &&
            context.item.isCompleted && context.responseSlot.isUsed;
    }

    static bool VerifyLifecycleStaysExecutingWhileWaiting()
    {
        TestContext context = CreateRespondedAttackContext("roll81_i", 5, 5);
        bool began = Begin(context, BattleRollMode.Manual, 1f, 0f);
        bool beginExecuting = context.runtimeState.LifecyclePhase ==
            BattleLifecyclePhase.Executing;
        context.controller.AdvancePausableExecution(1f, out string firstFailure);
        bool waitingExecuting = context.runtimeState.LifecyclePhase ==
            BattleLifecyclePhase.Executing;
        context.controller.TryRequestManualRoll(out string firstRequestFailure);
        bool tieExecuting = context.runtimeState.LifecyclePhase ==
            BattleLifecyclePhase.Executing;
        BattleClashSession session = context.controller.ExecutionRunner
            .CurrentClashSession;
        SetNextAttackPoints(session, 6, 4);
        context.controller.AdvancePausableExecution(
            0f,
            out string firstResultFailure
        );
        context.controller.AdvancePausableExecution(1f, out string secondFailure);
        bool secondWaitingExecuting = context.runtimeState.LifecyclePhase ==
            BattleLifecyclePhase.Executing;
        bool secondRequest = context.controller.TryRequestManualRoll(
            out string secondRequestFailure
        );
        bool secondResultShown = context.controller.AdvancePausableExecution(
            0f,
            out string secondResultFailure
        );
        bool resolutionPending = context.runtimeState.LifecyclePhase ==
            BattleLifecyclePhase.Executing &&
            context.controller.ExecutionRunner.Phase ==
                BattleExecutionRunnerPhase.ResolutionPending;
        bool committed = CommitPendingResolution(context);

        return began && beginExecuting && waitingExecuting && tieExecuting &&
            secondWaitingExecuting && secondRequest && secondResultShown &&
            resolutionPending && committed &&
            string.IsNullOrEmpty(firstFailure) &&
            string.IsNullOrEmpty(firstRequestFailure) &&
            string.IsNullOrEmpty(firstResultFailure) &&
            string.IsNullOrEmpty(secondFailure) &&
            string.IsNullOrEmpty(secondRequestFailure) &&
            string.IsNullOrEmpty(secondResultFailure) &&
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.TurnResolved &&
            context.item.isCompleted && context.plan.isCompleted;
    }

    static bool VerifyNonCombatItemsRemainSynchronousAndUnilateralWaits()
    {
        TestContext abilityContext = CreateBaseContext("roll81_j_ability");
        BattleActionSlot abilitySlot = new BattleActionSlot(
            abilityContext.ally,
            1
        );
        abilityContext.playerCard = CreateCard(
            abilityContext.ally,
            "roll81_j_ability_card",
            "Ability",
            0
        );
        abilitySlot.AssignFreeAction(
            abilityContext.ally,
            abilityContext.playerCard,
            abilityContext.ally
        );
        abilityContext.item = new BattleExecutionItem(
            1,
            BattleExecutionItemType.FreeAction,
            null,
            abilitySlot
        );
        SetPlan(abilityContext, abilityContext.item);
        bool abilityBegan = Begin(
            abilityContext,
            BattleRollMode.Manual,
            1f,
            1f
        );
        bool abilityCompleted = abilityBegan && abilityContext.item.isCompleted &&
            abilityContext.plan.isCompleted &&
            abilityContext.runtimeState.LifecyclePhase ==
                BattleLifecyclePhase.TurnResolved;

        TestContext freeContext = CreateBaseContext("roll81_j_free");
        BattleActionSlot freeSlot = new BattleActionSlot(freeContext.ally, 1);
        freeContext.playerCard = CreateCard(
            freeContext.ally,
            "roll81_j_free_card",
            CardType.Attack,
            1
        );
        freeSlot.AssignFreeAction(
            freeContext.ally,
            freeContext.playerCard,
            freeContext.enemy
        );
        freeContext.item = new BattleExecutionItem(
            1,
            BattleExecutionItemType.FreeAction,
            null,
            freeSlot
        );
        SetPlan(freeContext, freeContext.item);
        bool freeBegan = Begin(freeContext, BattleRollMode.Manual, 0f, 1f);
        BattleExecutionRunner freeRunner = freeContext.controller.ExecutionRunner;
        bool freeWaiting = freeBegan && !freeContext.item.isCompleted &&
            freeRunner.Phase == BattleExecutionRunnerPhase.WaitingForRoll &&
            freeRunner.CurrentClashSession == null &&
            !freeRunner.CurrentResolutionPlan.freeActionHasRolled &&
            freeRunner.CurrentResolutionPlan.impacts.Count == 0;

        TestContext enemyContext = CreateBaseContext("roll81_j_enemy");
        enemyContext.enemyCard = CreateCard(
            enemyContext.enemy,
            "roll81_j_enemy_card",
            CardType.Attack,
            1
        );
        enemyContext.intent = new BattleEnemyIntent(
            "roll81_j_enemy_intent",
            enemyContext.enemy,
            enemyContext.enemyCard,
            enemyContext.ally,
            1,
            1
        );
        enemyContext.item = new BattleExecutionItem(
            1,
            BattleExecutionItemType.UnrespondedEnemyIntent,
            enemyContext.intent,
            null
        );
        SetPlan(enemyContext, enemyContext.item);
        bool enemyBegan = Begin(enemyContext, BattleRollMode.Auto, 0f, 1f);
        BattleExecutionRunner enemyRunner = enemyContext.controller.ExecutionRunner;
        bool enemyWaiting = enemyBegan && !enemyContext.item.isCompleted &&
            enemyRunner.Phase == BattleExecutionRunnerPhase.AutoRollDelay &&
            enemyRunner.CurrentClashSession == null &&
            !enemyRunner.CurrentResolutionPlan.freeActionHasRolled &&
            enemyRunner.CurrentResolutionPlan.impacts.Count == 0;

        return abilityCompleted && freeWaiting && enemyWaiting;
    }

    static bool VerifyAttackTieLimitCompletesNormally()
    {
        TestContext context = CreateRespondedAttackContext("roll81_k", 5, 5);
        int allyHpBefore = context.ally.currentHP;
        int enemyHpBefore = context.enemy.currentHP;
        int playerUseBefore = context.playerCard.currentUseCount;
        int enemyUseBefore = context.enemyCard.currentUseCount;
        bool began = Begin(context, BattleRollMode.Manual, 0f, 0f);
        bool allRequestsAccepted = true;
        for (int attempt = 0;
             attempt < BattleClashSession.MaxAttackTieCount;
             attempt++)
        {
            allRequestsAccepted &= context.controller.TryRequestManualRoll(
                out string requestFailure
            );
            allRequestsAccepted &= string.IsNullOrEmpty(requestFailure);
            allRequestsAccepted &= context.controller.AdvancePausableExecution(
                0f,
                out string resultFailure
            );
            allRequestsAccepted &= string.IsNullOrEmpty(resultFailure);
        }
        bool resolutionPending = context.controller.ExecutionRunner.Phase ==
            BattleExecutionRunnerPhase.ResolutionPending;
        bool committed = CommitPendingResolution(context);

        BattleClashSession session = context.controller.ExecutionRunner
            .CurrentClashSession;
        return began && allRequestsAccepted && resolutionPending && committed &&
            session.AttemptIndex == 10 &&
            session.AttackTieCount == 10 && session.IsFinalized &&
            session.FinalResult == BattleClashFinalResult.TieLimit &&
            context.item.status == BattleExecutionItemStatus.Executed &&
            context.item.outcomeReason ==
                BattleExecutionItemOutcomeReason.TieLimitReached &&
            context.item.isCompleted && context.plan.isCompleted &&
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.TurnResolved &&
            context.ally.currentHP == allyHpBefore &&
            context.enemy.currentHP == enemyHpBefore &&
            context.playerCard.currentUseCount == playerUseBefore &&
            context.enemyCard.currentUseCount == enemyUseBefore &&
            !context.responseSlot.isUsed;
    }

    static bool VerifyDefenseUsesRollGateAndEqualityFullBlocks()
    {
        TestContext context = CreateRespondedClashContext(
            "roll81_l",
            CardType.Defense,
            5,
            5
        );
        int allyHpBefore = context.ally.currentHP;
        bool began = Begin(context, BattleRollMode.Manual, 1f, 0f);
        bool paused = context.controller.AdvancePausableExecution(
            0.5f,
            out string pauseFailure
        );
        BattleExecutionRunner runner = context.controller.ExecutionRunner;
        bool noEarlyRoll = paused && runner.CurrentClashSession.AttemptIndex == 0;
        bool ready = context.controller.AdvancePausableExecution(
            0.5f,
            out string readyFailure
        );
        bool requested = context.controller.TryRequestManualRoll(
            out string requestFailure
        );
        bool resultShown = context.controller.AdvancePausableExecution(
            0f,
            out string resultFailure
        );
        bool resolutionPending = runner.Phase ==
            BattleExecutionRunnerPhase.ResolutionPending;
        bool committed = CommitPendingResolution(context);

        return began && noEarlyRoll && ready && requested && resultShown &&
            resolutionPending && committed &&
            string.IsNullOrEmpty(pauseFailure) &&
            string.IsNullOrEmpty(readyFailure) &&
            string.IsNullOrEmpty(requestFailure) &&
            string.IsNullOrEmpty(resultFailure) &&
            runner.CurrentClashSession.AttemptIndex == 1 &&
            runner.CurrentClashSession.FinalResult ==
                BattleClashFinalResult.DefenseFullBlock &&
            context.ally.currentHP == allyHpBefore &&
            context.item.isCompleted && context.plan.isCompleted;
    }

    static bool VerifyDodgeUsesRollGateAndEqualitySucceeds()
    {
        TestContext context = CreateRespondedClashContext(
            "roll81_m",
            CardType.Dodge,
            5,
            5
        );
        int allyHpBefore = context.ally.currentHP;
        bool began = Begin(context, BattleRollMode.Manual, 1f, 0f);
        bool paused = context.controller.AdvancePausableExecution(
            0.5f,
            out string pauseFailure
        );
        BattleExecutionRunner runner = context.controller.ExecutionRunner;
        bool noEarlyRoll = paused && runner.CurrentClashSession.AttemptIndex == 0;
        bool ready = context.controller.AdvancePausableExecution(
            0.5f,
            out string readyFailure
        );
        bool requested = context.controller.TryRequestManualRoll(
            out string requestFailure
        );
        bool resultShown = context.controller.AdvancePausableExecution(
            0f,
            out string resultFailure
        );
        bool resolutionPending = runner.Phase ==
            BattleExecutionRunnerPhase.ResolutionPending;
        bool committed = CommitPendingResolution(context);

        return began && noEarlyRoll && ready && requested && resultShown &&
            resolutionPending && committed &&
            string.IsNullOrEmpty(pauseFailure) &&
            string.IsNullOrEmpty(readyFailure) &&
            string.IsNullOrEmpty(requestFailure) &&
            string.IsNullOrEmpty(resultFailure) &&
            runner.CurrentClashSession.AttemptIndex == 1 &&
            runner.CurrentClashSession.FinalResult ==
                BattleClashFinalResult.DodgeSuccess &&
            context.ally.currentHP == allyHpBefore &&
            context.item.isCompleted && context.plan.isCompleted;
    }

    static bool VerifyBattleEndedStillSkipsRemainingItems()
    {
        TestContext context = CreateRespondedAttackContext("roll81_n", 40, 4);
        BattleCardState remainingCard = CreateCard(
            context.allyB,
            "roll81_n_remaining",
            CardType.Attack,
            3
        );
        BattleActionSlot remainingSlot = new BattleActionSlot(context.allyB, 1);
        remainingSlot.AssignFreeAction(
            context.allyB,
            remainingCard,
            context.enemy
        );
        BattleExecutionItem remainingItem = new BattleExecutionItem(
            2,
            BattleExecutionItemType.FreeAction,
            null,
            remainingSlot
        );
        context.plan.AddItem(remainingItem);
        int remainingUseCountBefore = remainingCard.currentUseCount;

        bool began = Begin(context, BattleRollMode.Manual, 0f, 0f);
        bool requested = context.controller.TryRequestManualRoll(
            out string requestFailure
        );
        BattleExecutionRunner runner = context.controller.ExecutionRunner;
        bool calculateDidNotEndBattle = requested &&
            !context.runtimeState.IsBattleEnded && !context.item.isCompleted;
        bool committed = CommitPendingResolution(context);
        bool fatalItemCompleted = committed && context.runtimeState.IsBattleEnded &&
            context.item.isCompleted && !remainingItem.isCompleted &&
            runner.Phase == BattleExecutionRunnerPhase.ItemCompleted;
        bool advanced = context.controller.AdvancePausableExecution(
            0f,
            out string advanceFailure
        );

        return began && calculateDidNotEndBattle && fatalItemCompleted && advanced &&
            string.IsNullOrEmpty(requestFailure) &&
            string.IsNullOrEmpty(advanceFailure) &&
            remainingItem.status == BattleExecutionItemStatus.Skipped &&
            remainingItem.outcomeReason ==
                BattleExecutionItemOutcomeReason.BattleEnded &&
            remainingCard.currentUseCount == remainingUseCountBefore &&
            !remainingSlot.isUsed && context.plan.isCompleted &&
            runner.IsCompleted;
    }

    static bool CommitPendingResolution(TestContext context)
    {
        if (context == null || context.controller == null)
        {
            return false;
        }

        for (int step = 0; step < 8 && !context.item.isCompleted; step++)
        {
            if (!context.controller.AdvancePausableExecution(
                    0f,
                    out string failureMessage
                ) ||
                !string.IsNullOrEmpty(failureMessage))
            {
                return false;
            }
        }

        return context.item.isCompleted;
    }

    static TestContext CreateRespondedAttackContext(
        string prefix,
        int playerPoint,
        int enemyPoint
    )
    {
        return CreateRespondedClashContext(
            prefix,
            CardType.Attack,
            playerPoint,
            enemyPoint
        );
    }

    static TestContext CreateRespondedClashContext(
        string prefix,
        string playerCardType,
        int playerPoint,
        int enemyPoint
    )
    {
        TestContext context = CreateBaseContext(prefix);
        context.playerCard = CreateCard(
            context.ally,
            prefix + "_player_card",
            playerCardType,
            playerPoint
        );
        context.enemyCard = CreateCard(
            context.enemy,
            prefix + "_enemy_attack",
            CardType.Attack,
            enemyPoint
        );
        context.intent = new BattleEnemyIntent(
            prefix + "_intent",
            context.enemy,
            context.enemyCard,
            context.ally,
            1,
            1
        );
        context.responseSlot = new BattleActionSlot(context.ally, 1);
        context.responseSlot.AssignResponse(
            context.ally,
            context.playerCard,
            context.intent,
            false
        );
        context.item = new BattleExecutionItem(
            1,
            BattleExecutionItemType.RespondedEnemyIntent,
            context.intent,
            context.responseSlot
        );
        context.runtimeState.SetActionSlots(
            new List<BattleActionSlot> { context.responseSlot }
        );
        context.runtimeState.SetIntentQueue(
            new List<BattleEnemyIntent> { context.intent }
        );
        SetPlan(context, context.item);
        return context;
    }

    static TestContext CreateBaseContext(string prefix)
    {
        TestContext context = new TestContext
        {
            ally = new CharacterData(prefix + "_A", 30, 10, 10),
            allyB = new CharacterData(prefix + "_B", 30, 8, 8),
            enemy = new CharacterData(prefix + "_Enemy", 30, 5, 5),
            runtimeState = new BattleRuntimeState()
        };
        context.runtimeState.SetCharacters(
            context.ally,
            context.allyB,
            context.enemy
        );
        context.controller = new BattleLifecycleController(context.runtimeState);
        context.controller.TryInitializeToPrepare(out string failureMessage);
        return context;
    }

    static void SetPlan(TestContext context, BattleExecutionItem item)
    {
        context.plan = new BattleExecutionPlan();
        context.plan.AddItem(item);
        context.runtimeState.SetExecutionPlan(context.plan);
    }

    static bool Begin(
        TestContext context,
        BattleRollMode mode,
        float readyPause,
        float autoDelay
    )
    {
        bool began = context.controller.TryBeginPausableExecution(
            new BattleRollGateSettings(mode, readyPause, autoDelay),
            out string failureMessage
        );
        if (!began || !string.IsNullOrEmpty(failureMessage))
        {
            return false;
        }

        // ImmediatePresenter也由下一次正式推进消费ActionBegin完成状态。
        return context.controller.AdvancePausableExecution(
                0f,
                out string presentationFailure
            ) &&
            string.IsNullOrEmpty(presentationFailure);
    }

    static void SetNextAttackPoints(
        BattleClashSession session,
        int sideAPoint,
        int sideBPoint
    )
    {
        session.SideA.resourceSnapshot.selectedMinPoint = sideAPoint;
        session.SideA.resourceSnapshot.selectedMaxPoint = sideAPoint;
        session.SideB.resourceSnapshot.selectedMinPoint = sideBPoint;
        session.SideB.resourceSnapshot.selectedMaxPoint = sideBPoint;
    }

    static BattleCardState CreateCard(
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
            isClashable = cardType == CardType.Attack || cardType == CardType.Dodge,
            minPoint = point,
            maxPoint = point,
            cooldown = 0,
            damageFormula = "PointAsDamage",
            defenseFormula = cardType == CardType.Defense
                ? "PointAsDefense"
                : "",
            effects = new List<CardEffectData>()
        };
        return BattleCardManager.CreateBattleCard(owner, cardData, id + "_instance");
    }
}
