// Phase 3.5：验证表现等待边界、RequestId幂等与旧同步入口兼容。
using System.Collections.Generic;
using UnityEngine;

public static class BattlePresentationProtocolTests
{
    sealed class TestContext
    {
        public CharacterData ally;
        public CharacterData allyB;
        public CharacterData enemy;
        public BattleCardState playerCard;
        public BattleCardState enemyCard;
        public BattleActionSlot slot;
        public BattleEnemyIntent intent;
        public BattleExecutionItem item;
        public BattleExecutionPlan plan;
        public BattleRuntimeState runtimeState;
        public BattleLifecycleController controller;
        public BattleConsolePresenter presenter;
    }

    public static void Run()
    {
        Debug.Log("===== BattlePresentationProtocolBasic 聚合测试开始 =====");

        bool a = VerifyActionBeginBlocks();
        bool b = VerifyActionBeginCompletesOnNextAdvance();
        bool c = VerifyRollResultBlocks();
        bool d = VerifyAttackTieWaitsForPresentation();
        bool e = VerifyImpactBlocksAllCommitEffects();
        bool f = VerifyImpactCommitsOnce();
        bool g = VerifyDuplicateCompletionDoesNotRepeatImpact();
        bool h = VerifyStaleRequestIsIgnored();
        bool i = VerifyFullBlockWaitsForImpact();
        bool j = VerifyDodgeSuccessHasNoFakeImpact();
        bool k = VerifyTieLimitHasNoFakeImpact();
        bool l = VerifyActionCompleteBlocksItem();
        bool m = VerifyActionCompleteFinishesItem();
        bool n = VerifyLifecycleStaysExecuting();
        bool o = VerifyCancelRejectsLateCompletion();
        bool p = VerifyImmediatePresenterDoesNotCrossBoundary();
        bool q = VerifyTwoImpactsCommitOnePerCompletion();
        bool r = VerifySynchronousResolverBypassesPresenter();

        bool[] results =
        {
            a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r
        };
        string[] names =
        {
            "A ActionBegin未完成前不能进入ClashReady",
            "B ActionBegin完成后下一次Advance才进入ClashReady",
            "C RollResult未完成前不能进入ResolutionPending或下一次Roll",
            "D AttackTie完成RollResult后才重新ClashReady",
            "E Impact未完成前HP、Hit、Buff、CD均不变化",
            "F Impact完成后只提交一次",
            "G 重复Completion不重复Impact",
            "H 过期RequestId无副作用",
            "I FullBlock Presentation后才Hit(0)",
            "J DodgeSuccess不创建Fake Impact",
            "K TieLimit不创建Fake Impact且不MarkUsed",
            "L ActionComplete未完成时Item不完成",
            "M ActionComplete完成后才MarkUsed与ItemComplete",
            "N 所有Presentation等待期间Lifecycle保持Executing",
            "O Cancel后迟到Completion无副作用",
            "P ImmediatePresenter不在同一次推进穿透新Boundary",
            "Q 两个Impact一次Completion最多提交一个",
            "R 同步Resolver facade不经过Presenter"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log("模式83 " + names[index] + "：" + results[index]);
            allPassed &= results[index];
        }
        Debug.Log("模式83 聚合结果：" + allPassed);
    }

    static bool VerifyActionBeginBlocks()
    {
        TestContext context = CreateContext("presentation83_a", CardType.Attack, 6, 4);
        bool began = Begin(context);
        BattlePresentationRequest request = context.presenter.GetLastRequest();
        bool advanced = Advance(context);
        return began && request != null && request.Cue == BattlePresentationCue.ActionBegin &&
            advanced && context.controller.ExecutionRunner.Phase ==
                BattleExecutionRunnerPhase.WaitingForPresentation &&
            context.controller.ExecutionRunner.CurrentClashSession.AttemptIndex == 0;
    }

    static bool VerifyActionBeginCompletesOnNextAdvance()
    {
        TestContext context = CreateContext("presentation83_b", CardType.Attack, 6, 4);
        bool began = Begin(context);
        bool completed = CompleteCurrent(context);
        bool beforeAdvance = context.controller.ExecutionRunner.Phase ==
            BattleExecutionRunnerPhase.WaitingForPresentation;
        bool advanced = Advance(context);
        return began && completed && beforeAdvance && advanced &&
            context.controller.ExecutionRunner.Phase ==
                BattleExecutionRunnerPhase.WaitingForRoll;
    }

    static bool VerifyRollResultBlocks()
    {
        TestContext context = CreateContext("presentation83_c", CardType.Attack, 6, 4);
        bool ready = BeginAndCompleteActionBegin(context);
        bool rolled = context.controller.TryRequestManualRoll(out string failure);
        BattlePresentationRequest request = context.presenter.GetLastRequest();
        bool secondRollRejected = !context.controller.TryRequestManualRoll(out string secondFailure);
        return ready && rolled && string.IsNullOrEmpty(failure) && request != null &&
            request.Cue == BattlePresentationCue.RollResult && secondRollRejected &&
            !string.IsNullOrEmpty(secondFailure) &&
            context.controller.ExecutionRunner.Phase ==
                BattleExecutionRunnerPhase.WaitingForPresentation &&
            context.controller.ExecutionRunner.CurrentResolutionPlan != null &&
            context.enemy.currentHP == 30;
    }

    static bool VerifyAttackTieWaitsForPresentation()
    {
        TestContext context = CreateContext("presentation83_d", CardType.Attack, 5, 5);
        bool ready = BeginAndCompleteActionBegin(context);
        bool rolled = context.controller.TryRequestManualRoll(out string failure);
        bool blocked = context.controller.ExecutionRunner.Phase ==
            BattleExecutionRunnerPhase.WaitingForPresentation;
        bool completed = CompleteCurrent(context);
        bool advanced = Advance(context);
        return ready && rolled && string.IsNullOrEmpty(failure) && blocked &&
            completed && advanced && context.controller.ExecutionRunner.Phase ==
                BattleExecutionRunnerPhase.WaitingForRoll &&
            context.controller.ExecutionRunner.CurrentClashSession.AttackTieCount == 1;
    }

    static bool VerifyImpactBlocksAllCommitEffects()
    {
        TestContext context = CreateContext("presentation83_e", CardType.Attack, 6, 4);
        context.playerCard.cardData.cooldown = 1;
        AddProbeEffect(context.playerCard, BattleTiming.Hit, "GuardUp", 1);
        context.ally.AddBuff("NextClashPointUp", 1, 1);
        bool pending = ReachResolutionPending(context);
        bool requested = Advance(context);
        BattlePresentationRequest request = context.presenter.GetLastRequest();
        return pending && requested && request != null &&
            request.Cue == BattlePresentationCue.Impact &&
            context.enemy.currentHP == 30 &&
            context.ally.GetBuffStack("GuardUp") == 0 &&
            context.ally.GetBuffStack("NextClashPointUp") == 1 &&
            context.playerCard.currentCooldown == 0 && !context.item.isCompleted;
    }

    static bool VerifyImpactCommitsOnce()
    {
        TestContext context = CreateContext("presentation83_f", CardType.Attack, 6, 4);
        bool impactReady = ReachImpactPresentation(context);
        bool completed = CompleteCurrent(context);
        bool advanced = Advance(context);
        int hpAfter = context.enemy.currentHP;
        return impactReady && completed && advanced && hpAfter == 24 &&
            context.controller.ExecutionRunner.CurrentResolutionPlan.State ==
                BattleResolutionPlanState.Completed &&
            context.presenter.GetLastRequest().Cue == BattlePresentationCue.ActionComplete &&
            !context.item.isCompleted;
    }

    static bool VerifyDuplicateCompletionDoesNotRepeatImpact()
    {
        TestContext context = CreateContext("presentation83_g", CardType.Attack, 6, 4);
        bool impactReady = ReachImpactPresentation(context);
        BattlePresentationRequest impactRequest = context.presenter.GetLastRequest();
        bool firstComplete = context.presenter.TryCompleteRequest(impactRequest.RequestId);
        bool advanced = Advance(context);
        int hpAfter = context.enemy.currentHP;
        bool duplicateRejected = !context.presenter.TryCompleteRequest(impactRequest.RequestId);
        bool waited = Advance(context);
        return impactReady && firstComplete && advanced && duplicateRejected && waited &&
            context.enemy.currentHP == hpAfter && hpAfter == 24 &&
            context.controller.ExecutionRunner.Phase ==
                BattleExecutionRunnerPhase.WaitingForPresentation;
    }

    static bool VerifyStaleRequestIsIgnored()
    {
        TestContext context = CreateContext("presentation83_h", CardType.Attack, 6, 4);
        bool began = Begin(context);
        BattlePresentationRequest oldRequest = context.presenter.GetLastRequest();
        bool completed = CompleteCurrent(context);
        bool advanced = Advance(context);
        bool rolled = context.controller.TryRequestManualRoll(out string failure);
        BattlePresentationRequest currentRequest = context.presenter.GetLastRequest();
        bool staleRejected = !context.presenter.TryCompleteRequest(oldRequest.RequestId);
        return began && completed && advanced && rolled &&
            string.IsNullOrEmpty(failure) && currentRequest != null &&
            currentRequest.RequestId != oldRequest.RequestId && staleRejected &&
            !context.controller.ExecutionRunner.CurrentPresentationCompletion.IsCompleted &&
            context.controller.ExecutionRunner.Phase ==
                BattleExecutionRunnerPhase.WaitingForPresentation &&
            context.enemy.currentHP == 30 && !context.item.isCompleted;
    }

    static bool VerifyFullBlockWaitsForImpact()
    {
        TestContext context = CreateContext("presentation83_i", CardType.Defense, 5, 5);
        AddProbeEffect(context.enemyCard, BattleTiming.Hit, "Bullet", 1);
        int hpBefore = context.ally.currentHP;
        bool impactReady = ReachImpactPresentation(context);
        bool before = context.enemy.GetBuffStack("Bullet") == 0 &&
            context.ally.currentHP == hpBefore;
        bool completed = CompleteCurrent(context);
        bool advanced = Advance(context);
        return impactReady && before && completed && advanced &&
            context.enemy.GetBuffStack("Bullet") == 1 &&
            context.ally.currentHP == hpBefore &&
            context.controller.ExecutionRunner.CurrentResolutionPlan.CompletedResult != null &&
            context.controller.ExecutionRunner.CurrentResolutionPlan.CompletedResult.resultType ==
                "DefenseFullBlock";
    }

    static bool VerifyDodgeSuccessHasNoFakeImpact()
    {
        TestContext context = CreateContext("presentation83_j", CardType.Dodge, 5, 5);
        bool pending = ReachResolutionPending(context);
        int requestCount = context.presenter.Requests.Count;
        bool advanced = Advance(context);
        BattleResolutionPlan plan = context.controller.ExecutionRunner.CurrentResolutionPlan;
        return pending && plan != null && plan.impacts.Count == 0 && advanced &&
            context.presenter.Requests.Count == requestCount + 1 &&
            context.presenter.GetLastRequest().Cue == BattlePresentationCue.ActionComplete &&
            plan.State == BattleResolutionPlanState.Completed &&
            !context.item.isCompleted;
    }

    static bool VerifyTieLimitHasNoFakeImpact()
    {
        TestContext context = CreateContext("presentation83_k", CardType.Attack, 5, 5);
        bool ready = BeginAndCompleteActionBegin(context);
        bool rolled = ready;
        for (int index = 0; index < BattleClashSession.MaxAttackTieCount; index++)
        {
            rolled &= context.controller.TryRequestManualRoll(out string failure) &&
                string.IsNullOrEmpty(failure);
            rolled &= CompleteCurrent(context) && Advance(context);
        }

        BattleResolutionPlan plan = context.controller.ExecutionRunner.CurrentResolutionPlan;
        bool committed = Advance(context);
        bool actionCompleted = CompleteCurrent(context) && Advance(context);
        return rolled && plan != null && plan.impacts.Count == 0 && committed &&
            actionCompleted && plan.CompletedResult != null &&
            plan.CompletedResult.resultType == "TieLimit" &&
            context.item.outcomeReason == BattleExecutionItemOutcomeReason.TieLimitReached &&
            !context.slot.isUsed;
    }

    static bool VerifyActionCompleteBlocksItem()
    {
        TestContext context = CreateContext("presentation83_l", CardType.Attack, 6, 4);
        bool impactReady = ReachImpactPresentation(context);
        bool committed = CompleteCurrent(context) && Advance(context);
        return impactReady && committed && context.presenter.GetLastRequest().Cue ==
            BattlePresentationCue.ActionComplete && !context.item.isCompleted &&
            !context.slot.isUsed && context.runtimeState.LifecyclePhase ==
                BattleLifecyclePhase.Executing;
    }

    static bool VerifyActionCompleteFinishesItem()
    {
        TestContext context = CreateContext("presentation83_m", CardType.Attack, 6, 4);
        bool impactReady = ReachImpactPresentation(context);
        bool committed = CompleteCurrent(context) && Advance(context);
        bool completed = CompleteCurrent(context) && Advance(context);
        return impactReady && committed && completed && context.item.isCompleted &&
            context.slot.isUsed && context.plan.isCompleted &&
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.TurnResolved;
    }

    static bool VerifyLifecycleStaysExecuting()
    {
        TestContext context = CreateContext("presentation83_n", CardType.Attack, 6, 4);
        bool began = Begin(context);
        bool actionWaiting = IsExecutingAndWaiting(context, BattlePresentationCue.ActionBegin);
        bool actionDone = CompleteCurrent(context) && Advance(context);
        bool rolled = context.controller.TryRequestManualRoll(out string failure);
        bool rollWaiting = IsExecutingAndWaiting(context, BattlePresentationCue.RollResult);
        bool rollDone = CompleteCurrent(context) && Advance(context);
        bool impactRequested = Advance(context);
        bool impactWaiting = IsExecutingAndWaiting(context, BattlePresentationCue.Impact);
        bool impactDone = CompleteCurrent(context) && Advance(context);
        bool completeWaiting = IsExecutingAndWaiting(context, BattlePresentationCue.ActionComplete);
        return began && actionWaiting && actionDone && rolled &&
            string.IsNullOrEmpty(failure) && rollWaiting && rollDone &&
            impactRequested && impactWaiting && impactDone && completeWaiting;
    }

    static bool VerifyCancelRejectsLateCompletion()
    {
        TestContext context = CreateContext("presentation83_o", CardType.Attack, 6, 4);
        bool began = Begin(context);
        BattlePresentationRequest request = context.presenter.GetLastRequest();
        int hpBefore = context.enemy.currentHP;
        bool cancelled = context.controller.CancelPausableExecution("Mode83 O");
        bool lateRejected = !context.presenter.TryCompleteRequest(request.RequestId);
        bool advanceRejected = !Advance(context);
        return began && cancelled && lateRejected && advanceRejected &&
            context.enemy.currentHP == hpBefore && !context.item.isCompleted &&
            !context.slot.isUsed;
    }

    static bool VerifyImmediatePresenterDoesNotCrossBoundary()
    {
        TestContext context = CreateContext(
            "presentation83_p",
            CardType.Attack,
            6,
            4,
            null,
            true
        );
        bool began = Begin(context);
        BattleExecutionRunner runner = context.controller.ExecutionRunner;
        bool beginStopped = runner.Phase == BattleExecutionRunnerPhase.WaitingForPresentation &&
            runner.CurrentPresentationRequest.Cue == BattlePresentationCue.ActionBegin;
        bool firstAdvance = Advance(context);
        bool ready = runner.Phase == BattleExecutionRunnerPhase.WaitingForRoll;
        bool rolled = context.controller.TryRequestManualRoll(out string failure);
        bool rollStopped = runner.Phase == BattleExecutionRunnerPhase.WaitingForPresentation &&
            runner.CurrentPresentationRequest.Cue == BattlePresentationCue.RollResult;
        bool secondAdvance = Advance(context);
        bool resolutionPending = runner.Phase == BattleExecutionRunnerPhase.ResolutionPending;
        bool thirdAdvance = Advance(context);
        bool impactStopped = runner.Phase == BattleExecutionRunnerPhase.WaitingForPresentation &&
            runner.CurrentPresentationRequest.Cue == BattlePresentationCue.Impact &&
            context.enemy.currentHP == 30;
        bool fourthAdvance = Advance(context);
        bool actionCompleteStopped = runner.Phase == BattleExecutionRunnerPhase.WaitingForPresentation &&
            runner.CurrentPresentationRequest.Cue == BattlePresentationCue.ActionComplete &&
            !context.item.isCompleted;
        return began && beginStopped && firstAdvance && ready && rolled &&
            string.IsNullOrEmpty(failure) && rollStopped && secondAdvance &&
            resolutionPending && thirdAdvance && impactStopped && fourthAdvance &&
            actionCompleteStopped;
    }

    static bool VerifyTwoImpactsCommitOnePerCompletion()
    {
        TestContext context = CreateContext("presentation83_q", CardType.Attack, 6, 4);
        bool pending = ReachResolutionPending(context);
        BattleResolutionPlan plan = context.controller.ExecutionRunner.CurrentResolutionPlan;
        plan.impacts.Add(new BattleImpact(
            1,
            context.ally,
            context.enemy,
            context.playerCard,
            6,
            6,
            ClashResult.Win,
            true,
            true
        ));
        bool firstRequested = Advance(context);
        bool firstCommitted = CompleteCurrent(context) && Advance(context);
        int hpAfterFirst = context.enemy.currentHP;
        bool onlyFirst = plan.impacts[0].state == BattleImpactState.Committed &&
            plan.impacts[1].state == BattleImpactState.Pending &&
            plan.State == BattleResolutionPlanState.Activated;
        bool secondRequested = Advance(context);
        return pending && firstRequested && firstCommitted && onlyFirst &&
            hpAfterFirst == 24 && secondRequested &&
            context.presenter.GetLastRequest().Cue == BattlePresentationCue.Impact &&
            context.presenter.GetLastRequest().ImpactIndex == 1 &&
            context.enemy.currentHP == hpAfterFirst;
    }

    static bool VerifySynchronousResolverBypassesPresenter()
    {
        TestContext context = CreateContext("presentation83_r", CardType.Attack, 6, 4);
        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            context.slot,
            context.intent
        );
        return result != null && result.resultType == "PlayerWin" &&
            result.damage == 6 && context.enemy.currentHP == 24 &&
            context.presenter.Requests.Count == 0;
    }

    static TestContext CreateContext(
        string prefix,
        string playerCardType,
        int playerPoint,
        int enemyPoint,
        BattleConsolePresenter presenter = null,
        bool useImmediatePresenter = false
    )
    {
        TestContext context = new TestContext
        {
            ally = new CharacterData(prefix + "_A", 30, 10, 10),
            allyB = new CharacterData(prefix + "_B", 30, 8, 8),
            enemy = new CharacterData(prefix + "_Enemy", 30, 5, 5),
            runtimeState = new BattleRuntimeState(),
            presenter = presenter
        };
        context.presenter = presenter ?? (useImmediatePresenter
            ? null
            : new BattleConsolePresenter(false));
        context.playerCard = CreateCard(
            context.ally,
            prefix + "_player",
            playerCardType,
            playerPoint
        );
        context.enemyCard = CreateCard(
            context.enemy,
            prefix + "_enemy",
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
        context.slot = new BattleActionSlot(context.ally, 1);
        context.slot.AssignResponse(
            context.ally,
            context.playerCard,
            context.intent,
            false
        );
        context.item = new BattleExecutionItem(
            1,
            BattleExecutionItemType.RespondedEnemyIntent,
            context.intent,
            context.slot
        );
        context.plan = new BattleExecutionPlan();
        context.plan.AddItem(context.item);
        context.runtimeState.SetCharacters(context.ally, context.allyB, context.enemy);
        context.runtimeState.SetActionSlots(new List<BattleActionSlot> { context.slot });
        context.runtimeState.SetIntentQueue(new List<BattleEnemyIntent> { context.intent });
        context.runtimeState.SetExecutionPlan(context.plan);
        IBattleExecutionPresenter executionPresenter = useImmediatePresenter
            ? BattleImmediatePresenter.Instance
            : context.presenter;
        context.controller = new BattleLifecycleController(
            context.runtimeState,
            executionPresenter
        );
        context.controller.TryInitializeToPrepare(out string failureMessage);
        return context;
    }

    static bool Begin(TestContext context)
    {
        return context.controller.TryBeginPausableExecution(
                new BattleRollGateSettings(BattleRollMode.Manual, 0f, 0f),
                out string failure
            ) &&
            string.IsNullOrEmpty(failure);
    }

    static bool BeginAndCompleteActionBegin(TestContext context)
    {
        return Begin(context) && CompleteCurrent(context) && Advance(context) &&
            context.controller.ExecutionRunner.Phase ==
                BattleExecutionRunnerPhase.WaitingForRoll;
    }

    static bool ReachResolutionPending(TestContext context)
    {
        return BeginAndCompleteActionBegin(context) &&
            context.controller.TryRequestManualRoll(out string failure) &&
            string.IsNullOrEmpty(failure) &&
            CompleteCurrent(context) &&
            Advance(context) &&
            context.controller.ExecutionRunner.Phase ==
                BattleExecutionRunnerPhase.ResolutionPending;
    }

    static bool ReachImpactPresentation(TestContext context)
    {
        return ReachResolutionPending(context) && Advance(context) &&
            context.presenter.GetLastRequest() != null &&
            context.presenter.GetLastRequest().Cue == BattlePresentationCue.Impact;
    }

    static bool CompleteCurrent(TestContext context)
    {
        BattlePresentationRequest request = context != null && context.presenter != null
            ? context.presenter.GetLastRequest()
            : null;
        return request != null && context.presenter.TryCompleteRequest(request.RequestId);
    }

    static bool Advance(TestContext context)
    {
        return context != null && context.controller != null &&
            context.controller.AdvancePausableExecution(
                0f,
                out string failure
            ) &&
            string.IsNullOrEmpty(failure);
    }

    static bool IsExecutingAndWaiting(
        TestContext context,
        BattlePresentationCue cue
    )
    {
        BattleExecutionRunner runner = context.controller.ExecutionRunner;
        return context.runtimeState.LifecyclePhase == BattleLifecyclePhase.Executing &&
            runner.Phase == BattleExecutionRunnerPhase.WaitingForPresentation &&
            runner.CurrentPresentationRequest != null &&
            runner.CurrentPresentationRequest.Cue == cue;
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
            defenseFormula = cardType == CardType.Defense ? "PointAsDefense" : "",
            effects = new List<CardEffectData>()
        };
        return BattleCardManager.CreateBattleCard(owner, cardData, id + "_instance");
    }

    static void AddProbeEffect(
        BattleCardState cardState,
        string timing,
        string buffID,
        int stack
    )
    {
        cardState.cardData.effects.Add(new CardEffectData
        {
            trigger = timing,
            effectType = CardEffectType.ApplyBuff,
            target = CardTargetType.Self,
            buffType = buffID,
            stack = stack,
            duration = -1,
            applyTiming = "Immediate"
        });
    }
}
