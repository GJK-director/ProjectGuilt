// Phase 3.4：验证Calculate与Commit分离、Impact幂等和同步兼容。
using System.Collections.Generic;
using UnityEngine;

public static class BattleResolutionPlanTests
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
        public BattleClashSession session;
        public BattleResolutionPlan resolutionPlan;
        public BattleExecutionItem item;
        public BattleExecutionPlan executionPlan;
        public BattleRuntimeState runtimeState;
        public BattleLifecycleController controller;
    }

    public static void Run()
    {
        Debug.Log("===== BattleResolutionPlanBasic 聚合测试开始 =====");

        bool a = VerifyCalculateDoesNotDamage();
        bool b = VerifyFirstCommitAppliesDamage();
        bool c = VerifyCommittedImpactIsIdempotent();
        bool d = VerifyActivationCommitsOnce();
        bool e = VerifyDefenseFullBlockContact();
        bool f = VerifyDodgeSuccessCompletesWithoutImpact();
        bool g = VerifyTieLimitCompletesWithoutImpact();
        bool h = VerifyAttackHasOneImpactAndMatchesResult();
        bool i = VerifySynchronousCompatibility();
        bool j = VerifyPausableLifecycleWaitsForCommit();
        bool k = VerifyFatalDamageEndsAfterCompletion();
        bool l = VerifyCompletedPlanIsIdempotent();
        bool m = VerifyImpactReadsLiveDamageModifier();
        bool n = VerifyDefenseRemainingAttackIsFixed();
        bool o = VerifyDeadTargetDoesNotRepeatKill();
        bool p = VerifyFreeAttackPlanDelaysDamage();
        bool q = VerifyFreeAttackSynchronousCompatibility();
        bool r = VerifyOnlyMeleeFreeAttackIsPausable();
        bool s = VerifyFreeAttackUsesOneSidedRollGate();
        bool t = VerifyGenericUnilateralRollPanelSupport();

        Debug.Log("模式82 A Calculate后Plan存在且HP不变：" + a);
        Debug.Log("模式82 B 首次Commit才提交第一个Impact伤害：" + b);
        Debug.Log("模式82 C 已提交Impact重复调用不重复事件与伤害：" + c);
        Debug.Log("模式82 D Activation的Buff资源Resolved卡牌与Guilt只提交一次：" + d);
        Debug.Log("模式82 E DefenseFullBlock保留Hit(0)且无伤害事件：" + e);
        Debug.Log("模式82 F DodgeSuccess为0 Impact且完成时只注册一次连续闪避：" + f);
        Debug.Log("模式82 G TieLimit为0 Impact且Item正常完成：" + g);
        Debug.Log("模式82 H 普通Attack为1 Clash与1 damaging Impact：" + h);
        Debug.Log("模式82 I 同步Resolver自动drain同一ResolutionPlan：" + i);
        Debug.Log("模式82 J ResolutionPending期间Lifecycle保持Executing：" + j);
        Debug.Log("模式82 K 致命伤只在Commit后于Item完成时进入BattleEnded：" + k);
        Debug.Log("模式82 L Completed Plan重复推进保持幂等：" + l);
        Debug.Log("模式82 M Impact读取提交时DamageTaken：" + m);
        Debug.Log("模式82 N Defense使用Session固定remainingAttack：" + n);
        Debug.Log("模式82 O 已死亡目标后续Impact跳过且不重复AfterKill：" + o);
        Debug.Log("模式82 P FreeAttack Build不扣血且Impact Commit后才扣血：" + p);
        Debug.Log("模式82 Q FreeAttack同步入口仍返回旧结果语义：" + q);
        Debug.Log("模式82 R 只有Melee FreeAction进入Pausable，Ability保持同步：" + r);
        Debug.Log("模式82 S FreeAttack等待单方Roll后才建立Impact：" + s);
        Debug.Log("模式82 T 单方Roll面板支持双方全部Attack Delivery：" + t);
        Debug.Log(
            "模式82 聚合结果：" +
            (a && b && c && d && e && f && g && h && i && j && k && l &&
             m && n && o && p && q && r && s && t)
        );
    }

    static bool VerifyCalculateDoesNotDamage()
    {
        TestContext context = CreateFinalizedContext("resolution82_a", CardType.Attack, 6, 4);
        int hpBefore = context.enemy.currentHP;
        context.resolutionPlan = BuildPlan(context);
        return context.resolutionPlan != null &&
            context.resolutionPlan.State == BattleResolutionPlanState.Pending &&
            context.resolutionPlan.impacts.Count == 1 &&
            context.enemy.currentHP == hpBefore;
    }

    static bool VerifyFirstCommitAppliesDamage()
    {
        TestContext context = CreateFinalizedContext("resolution82_b", CardType.Attack, 6, 4);
        int hpBefore = context.enemy.currentHP;
        context.resolutionPlan = BuildPlan(context);
        bool committed = BattleResolver.TryCommitNextResolutionStep(
            context.resolutionPlan,
            out BattleResolveResult result
        );
        return committed && result != null &&
            context.enemy.currentHP == hpBefore - 6 &&
            context.resolutionPlan.State == BattleResolutionPlanState.Completed &&
            context.resolutionPlan.impacts[0].state == BattleImpactState.Committed;
    }

    static bool VerifyCommittedImpactIsIdempotent()
    {
        TestContext context = CreateFinalizedContext("resolution82_c", CardType.Attack, 6, 4);
        AddProbeEffect(context.playerCard, BattleTiming.Hit, 1);
        context.resolutionPlan = BuildPlan(context);
        BattleImpact impact = context.resolutionPlan.impacts[0];
        bool first = BattleResolver.CommitImpact(context.resolutionPlan, impact);
        int hpAfterFirst = context.enemy.currentHP;
        int hitProbeAfterFirst = context.ally.GetBuffStack("Bullet");
        bool second = BattleResolver.CommitImpact(context.resolutionPlan, impact);
        return first && second && hpAfterFirst == 24 &&
            context.enemy.currentHP == hpAfterFirst &&
            hitProbeAfterFirst == 1 &&
            context.ally.GetBuffStack("Bullet") == hitProbeAfterFirst;
    }

    static bool VerifyActivationCommitsOnce()
    {
        TestContext normal = CreateFinalizedContext("resolution82_d_normal", CardType.Attack, 6, 4);
        normal.playerCard.cardData.cooldown = 1;
        normal.playerCard.cardData.resourceRule = CreateBulletResourceRule(1);
        AddNamedProbeEffect(
            normal.playerCard,
            BattleTiming.Resolved,
            "GuardUp",
            1
        );
        normal.ally.AddBuff("Bullet", 3, -1);
        normal.ally.AddBuff("NextClashPointUp", 1, 1);
        normal = RecreateFinalizedContext(normal, CardType.Attack, 6, 4);
        normal.resolutionPlan = BuildPlan(normal);
        normal.ally.AddBuff("Bullet", 2, -1);
        BattleResolver.TryCommitNextResolutionStep(normal.resolutionPlan, out BattleResolveResult firstResult);
        int bulletAfterFirst = normal.ally.GetBuffStack("Bullet");
        int clashBuffAfterFirst = normal.ally.GetBuffStack("NextClashPointUp");
        int cooldownAfterFirst = normal.playerCard.currentCooldown;
        BattleResolver.TryCommitNextResolutionStep(normal.resolutionPlan, out BattleResolveResult secondResult);
        bool normalOnce = firstResult != null && object.ReferenceEquals(firstResult, secondResult) &&
            bulletAfterFirst == 4 && normal.ally.GetBuffStack("Bullet") == 4 &&
            clashBuffAfterFirst == 0 && normal.ally.GetBuffStack("NextClashPointUp") == 0 &&
            cooldownAfterFirst == 2 && normal.playerCard.currentCooldown == 2 &&
            normal.ally.GetBuffStack("GuardUp") == 1;

        TestContext sin = CreateFinalizedContext("resolution82_d_sin", CardType.Attack, 6, 4);
        sin.playerCard.cardData.isSinCard = true;
        sin.playerCard.cardData.sinCardCategory = SinCardCategory.Clash;
        sin.playerCard.cardData.sinCardUseRule = SinCardUseRule.Permanent;
        sin.playerCard.cardData.guiltGain = 3;
        sin = RecreateFinalizedContext(sin, CardType.Attack, 6, 4);
        sin.resolutionPlan = BuildPlan(sin);
        BattleResolver.TryCommitNextResolutionStep(sin.resolutionPlan, out BattleResolveResult sinFirst);
        int guiltAfterFirst = GuiltManager.GetCurrentGuilt(sin.ally);
        BattleResolver.TryCommitNextResolutionStep(sin.resolutionPlan, out BattleResolveResult sinSecond);
        return normalOnce && sinFirst != null && object.ReferenceEquals(sinFirst, sinSecond) &&
            guiltAfterFirst == 3 && GuiltManager.GetCurrentGuilt(sin.ally) == 3;
    }

    static bool VerifyDefenseFullBlockContact()
    {
        TestContext context = CreateFinalizedContext("resolution82_e", CardType.Defense, 5, 5);
        AddProbeEffect(context.enemyCard, BattleTiming.Hit, 1);
        AddProbeEffect(context.enemyCard, BattleTiming.AfterDamage, 10);
        AddProbeEffect(context.enemyCard, BattleTiming.AfterKill, 100);
        int hpBefore = context.ally.currentHP;
        context.resolutionPlan = BuildPlan(context);
        bool shape = context.resolutionPlan.impacts.Count == 1 &&
            !context.resolutionPlan.impacts[0].allowsDamage &&
            context.resolutionPlan.impacts[0].shouldTriggerHit;
        BattleResolver.TryCommitNextResolutionStep(context.resolutionPlan, out BattleResolveResult result);
        return shape && result != null && result.resultType == "DefenseFullBlock" &&
            !result.hasDamage && context.ally.currentHP == hpBefore &&
            context.enemy.GetBuffStack("Bullet") == 1;
    }

    static bool VerifyDodgeSuccessCompletesWithoutImpact()
    {
        TestContext context = CreateRunnerContext("resolution82_f", CardType.Dodge, 5, 5, 30);
        bool began = BeginAndRoll(context);
        BattleResolutionPlan plan = context.controller.ExecutionRunner.CurrentResolutionPlan;
        bool beforeCommit = began && plan != null && plan.impacts.Count == 0 &&
            !context.slot.isContinuousDodgeActive && !context.item.isCompleted;
        bool committed = CommitRunner(context);
        bool secondNoOp = CommitRunner(context);
        return beforeCommit && committed && secondNoOp &&
            context.slot.isContinuousDodgeActive && context.item.isCompleted &&
            plan.IsActionCompleted;
    }

    static bool VerifyTieLimitCompletesWithoutImpact()
    {
        TestContext context = CreateRunnerContext("resolution82_g", CardType.Attack, 5, 5, 30);
        bool began = context.controller.TryBeginPausableExecution(
            new BattleRollGateSettings(BattleRollMode.Manual, 0f, 0f),
            out string beginFailure
        );
        bool actionBeginShown = began && string.IsNullOrEmpty(beginFailure) &&
            context.controller.AdvancePausableExecution(
                0f,
                out string actionBeginFailure
            ) &&
            string.IsNullOrEmpty(actionBeginFailure);
        bool rolled = actionBeginShown;
        for (int index = 0; index < BattleClashSession.MaxAttackTieCount; index++)
        {
            rolled &= context.controller.TryRequestManualRoll(out string failure) &&
                string.IsNullOrEmpty(failure);
            rolled &= context.controller.AdvancePausableExecution(
                    0f,
                    out string resultFailure
                ) &&
                string.IsNullOrEmpty(resultFailure);
        }
        BattleResolutionPlan plan = context.controller.ExecutionRunner.CurrentResolutionPlan;
        bool committed = CommitRunner(context);
        return rolled && plan != null && plan.impacts.Count == 0 && committed &&
            plan.CompletedResult != null && plan.CompletedResult.resultType == "TieLimit" &&
            !plan.CompletedResult.playerCardUsed && !plan.CompletedResult.enemyCardUsed &&
            context.item.isCompleted && !context.slot.isUsed;
    }

    static bool VerifyAttackHasOneImpactAndMatchesResult()
    {
        TestContext context = CreateFinalizedContext("resolution82_h", CardType.Attack, 7, 4);
        context.resolutionPlan = BuildPlan(context);
        bool shape = context.resolutionPlan.impacts.Count == 1 &&
            context.resolutionPlan.impacts[0].allowsDamage &&
            context.resolutionPlan.impacts[0].basePower == 7;
        BattleResolver.TryCommitNextResolutionStep(context.resolutionPlan, out BattleResolveResult result);
        return shape && result != null && result.resultType == "PlayerWin" &&
            result.damage == 7 && result.playerCardUsed &&
            context.enemy.currentHP == 23 && context.playerCard.currentCooldown == 0;
    }

    static bool VerifySynchronousCompatibility()
    {
        TestContext context = CreateContext("resolution82_i", CardType.Attack, 6, 4, 30);
        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            context.slot,
            context.intent
        );
        return result != null && result.resultType == "PlayerWin" &&
            result.damage == 6 && context.enemy.currentHP == 24 &&
            context.playerCard.currentCooldown == 0;
    }

    static bool VerifyPausableLifecycleWaitsForCommit()
    {
        TestContext context = CreateRunnerContext("resolution82_j", CardType.Attack, 6, 4, 30);
        bool rolled = BeginAndRoll(context);
        bool pending = rolled && context.runtimeState.LifecyclePhase ==
            BattleLifecyclePhase.Executing && !context.item.isCompleted &&
            context.controller.ExecutionRunner.Phase ==
                BattleExecutionRunnerPhase.ResolutionPending;
        bool advanced = context.controller.AdvancePausableExecution(
            100f,
            out string advanceFailure
        );
        bool stillPending = advanced && string.IsNullOrEmpty(advanceFailure) &&
            context.enemy.currentHP == 30 && !context.item.isCompleted;
        bool committed = CommitRunner(context);
        return pending && stillPending && committed &&
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.TurnResolved;
    }

    static bool VerifyFatalDamageEndsAfterCompletion()
    {
        TestContext context = CreateRunnerContext("resolution82_k", CardType.Attack, 10, 4, 6);
        bool rolled = BeginAndRoll(context);
        bool calculateAlive = rolled && !context.enemy.IsDead() &&
            !context.runtimeState.IsBattleEnded && !context.item.isCompleted;
        bool committed = CommitRunner(context);
        return calculateAlive && committed && context.enemy.IsDead() &&
            context.item.isCompleted && context.runtimeState.IsBattleEnded;
    }

    static bool VerifyCompletedPlanIsIdempotent()
    {
        TestContext context = CreateRunnerContext("resolution82_l", CardType.Attack, 6, 4, 30);
        bool rolled = BeginAndRoll(context);
        bool first = CommitRunner(context);
        int hp = context.enemy.currentHP;
        int cooldown = context.playerCard.currentCooldown;
        bool used = context.slot.isUsed;
        bool second = CommitRunner(context);
        return rolled && first && second && context.enemy.currentHP == hp &&
            context.playerCard.currentCooldown == cooldown &&
            context.slot.isUsed == used;
    }

    static bool VerifyImpactReadsLiveDamageModifier()
    {
        TestContext context = CreateFinalizedContext("resolution82_m", CardType.Attack, 5, 4);
        context.resolutionPlan = BuildPlan(context);
        context.enemy.AddBuff("Vulnerable", 10, 2);
        BattleResolver.TryCommitNextResolutionStep(context.resolutionPlan, out BattleResolveResult result);
        return result != null && context.resolutionPlan.impacts[0].basePower == 5 &&
            result.damage == 10 && context.enemy.currentHP == 20;
    }

    static bool VerifyDefenseRemainingAttackIsFixed()
    {
        TestContext context = CreateFinalizedContext("resolution82_n", CardType.Defense, 2, 6);
        context.resolutionPlan = BuildPlan(context);
        int fixedRemaining = context.session.RemainingAttackPoint;
        context.ally.AddBuff(
            "GuardUp",
            "守势",
            "UpBuff",
            100,
            2,
            BattleTiming.ClashStart,
            "ConsumeOnTrigger"
        );
        BattleResolver.TryCommitNextResolutionStep(context.resolutionPlan, out BattleResolveResult result);
        return fixedRemaining == 4 && context.resolutionPlan.impacts[0].basePower == 4 &&
            result != null && result.damage == 4 && context.ally.currentHP == 26 &&
            context.ally.GetBuffStack("GuardUp") == 100;
    }

    static bool VerifyDeadTargetDoesNotRepeatKill()
    {
        TestContext context = CreateFinalizedContext("resolution82_o", CardType.Attack, 40, 4);
        AddProbeEffect(context.playerCard, BattleTiming.AfterKill, 1);
        context.resolutionPlan = BuildPlan(context);
        context.resolutionPlan.impacts.Add(new BattleImpact(
            1,
            context.ally,
            context.enemy,
            context.playerCard,
            40,
            40,
            ClashResult.Win,
            true,
            true
        ));
        BattleResolver.TryCommitNextResolutionStep(context.resolutionPlan, out BattleResolveResult firstResult);
        int killProbe = context.ally.GetBuffStack("Bullet");
        BattleResolver.TryCommitNextResolutionStep(context.resolutionPlan, out BattleResolveResult finalResult);
        return firstResult == null && finalResult != null && context.enemy.IsDead() &&
            killProbe == 1 && context.ally.GetBuffStack("Bullet") == 1 &&
            context.resolutionPlan.impacts[1].state == BattleImpactState.Skipped;
    }

    static bool VerifyFreeAttackPlanDelaysDamage()
    {
        TestContext context = CreateFreeAttackContext(
            "resolution82_p",
            CardType.Attack,
            6,
            30
        );
        int hpBefore = context.enemy.currentHP;
        BattleResolveResult failure;
        context.resolutionPlan = BattleResolver.BuildFreeAttackResolutionPlan(
            context.item,
            context.slot,
            out failure
        );
        bool built = context.resolutionPlan != null && failure == null &&
            context.resolutionPlan.planKind ==
                BattleResolutionPlanKind.FreeActionAttack &&
            !context.resolutionPlan.freeActionHasRolled &&
            context.resolutionPlan.impacts.Count == 0 &&
            context.enemy.currentHP == hpBefore;
        bool earlyCommitRejected = !BattleResolver.TryCommitNextResolutionStep(
            context.resolutionPlan,
            out BattleResolveResult earlyResult
        ) && earlyResult == null && context.enemy.currentHP == hpBefore;
        bool rolled = BattleResolver.TryRollFreeAttackResolutionPlan(
            context.resolutionPlan,
            out int rolledPoint
        ) && rolledPoint == 6 &&
            context.resolutionPlan.freeActionHasRolled &&
            context.resolutionPlan.impacts.Count == 1 &&
            context.enemy.currentHP == hpBefore;
        BattleResolver.TryCommitNextResolutionStep(
            context.resolutionPlan,
            out BattleResolveResult result
        );
        return built && earlyCommitRejected && rolled && result != null &&
            result.resultType == "FreeAttack" &&
            context.enemy.currentHP == hpBefore - 6;
    }

    static bool VerifyFreeAttackSynchronousCompatibility()
    {
        TestContext context = CreateFreeAttackContext(
            "resolution82_q",
            CardType.Attack,
            6,
            30
        );
        BattleResolveResult result = BattleResolver.ResolveFreeAction(
            context.slot
        );
        return result != null && result.isSuccess &&
            result.resultType == "FreeAttack" && result.damage == 6 &&
            context.enemy.currentHP == 24 && result.playerCardUsed;
    }

    static bool VerifyOnlyMeleeFreeAttackIsPausable()
    {
        TestContext melee = CreateFreeAttackContext(
            "resolution82_r_melee",
            CardType.Attack,
            6,
            30
        );
        BattleExecutionItem meleeItem = new BattleExecutionItem(
            1,
            BattleExecutionItemType.FreeAction,
            null,
            melee.slot
        );

        TestContext ability = CreateFreeAttackContext(
            "resolution82_r_ability",
            "Ability",
            0,
            30
        );
        BattleExecutionItem abilityItem = new BattleExecutionItem(
            1,
            BattleExecutionItemType.FreeAction,
            null,
            ability.slot
        );
        return BattleExecutionPlanExecutor.IsPausableMeleeFreeAttack(meleeItem) &&
            !BattleExecutionPlanExecutor.IsPausableMeleeFreeAttack(abilityItem);
    }

    static bool VerifyFreeAttackUsesOneSidedRollGate()
    {
        TestContext context = CreateFreeAttackContext(
            "resolution82_s",
            CardType.Attack,
            6,
            30
        );
        context.runtimeState = new BattleRuntimeState();
        context.runtimeState.SetCharacters(
            context.ally,
            context.allyB,
            context.enemy
        );
        context.runtimeState.SetActionSlots(
            new List<BattleActionSlot> { context.slot }
        );
        context.runtimeState.SetIntentQueue(new List<BattleEnemyIntent>());
        context.executionPlan = new BattleExecutionPlan();
        context.executionPlan.AddItem(context.item);
        context.runtimeState.SetExecutionPlan(context.executionPlan);
        context.controller = new BattleLifecycleController(context.runtimeState);
        context.controller.TryInitializeToPrepare(out string initializeFailure);

        bool began = string.IsNullOrEmpty(initializeFailure) &&
            context.controller.TryBeginPausableExecution(
                new BattleRollGateSettings(BattleRollMode.Manual, 0f, 0f),
                out string beginFailure
            ) && string.IsNullOrEmpty(beginFailure);
        BattleExecutionRunner runner = context.controller.ExecutionRunner;
        BattleResolutionPlan plan = runner != null
            ? runner.CurrentResolutionPlan
            : null;
        bool preparedWithoutRoll = began && plan != null &&
            runner.CurrentClashSession == null &&
            !plan.freeActionHasRolled && plan.impacts.Count == 0 &&
            context.enemy.currentHP == 30;
        bool actionBeginCompleted = preparedWithoutRoll &&
            context.controller.AdvancePausableExecution(
                0f,
                out string actionBeginFailure
            ) && string.IsNullOrEmpty(actionBeginFailure) &&
            runner.Phase == BattleExecutionRunnerPhase.WaitingForRoll;
        bool rolled = actionBeginCompleted &&
            context.controller.TryRequestManualRoll(out string rollFailure) &&
            string.IsNullOrEmpty(rollFailure) &&
            runner.CurrentClashSession == null && plan.freeActionHasRolled &&
            plan.freeActionPoint == 6 && plan.impacts.Count == 1 &&
            context.enemy.currentHP == 30;
        bool rollResultCompleted = rolled &&
            context.controller.AdvancePausableExecution(
                0f,
                out string rollResultFailure
            ) && string.IsNullOrEmpty(rollResultFailure) &&
            runner.Phase == BattleExecutionRunnerPhase.ResolutionPending;
        return rollResultCompleted;
    }

    static TestContext CreateFreeAttackContext(
        string prefix,
        string cardType,
        int point,
        int enemyHP
    )
    {
        TestContext context = CreateContext(
            prefix,
            cardType,
            point,
            0,
            enemyHP
        );
        context.slot.AssignFreeAction(
            context.ally,
            context.playerCard,
            context.enemy
        );
        context.item = new BattleExecutionItem(
            1,
            BattleExecutionItemType.FreeAction,
            null,
            context.slot
        );
        return context;
    }

    static TestContext CreateFinalizedContext(
        string prefix,
        string playerType,
        int playerPoint,
        int enemyPoint
    )
    {
        TestContext context = CreateContext(prefix, playerType, playerPoint, enemyPoint, 30);
        BattleResolveResult failure = BattleResolver.TryBeginRespondedClash(
            context.slot,
            context.intent,
            out context.session
        );
        if (failure == null)
        {
            while (!context.session.IsFinalized)
            {
                context.session.RollNextAttempt();
            }
        }
        return context;
    }

    static TestContext RecreateFinalizedContext(
        TestContext context,
        string playerType,
        int playerPoint,
        int enemyPoint
    )
    {
        context.playerCard.cardData.cardType = playerType;
        context.playerCard.cardData.minPoint = playerPoint;
        context.playerCard.cardData.maxPoint = playerPoint;
        context.enemyCard.cardData.minPoint = enemyPoint;
        context.enemyCard.cardData.maxPoint = enemyPoint;
        BattleResolver.TryBeginRespondedClash(
            context.slot,
            context.intent,
            out context.session
        );
        while (!context.session.IsFinalized)
        {
            context.session.RollNextAttempt();
        }
        return context;
    }

    static BattleResolutionPlan BuildPlan(TestContext context)
    {
        return BattleResolver.BuildRespondedClashResolutionPlan(
            context.slot,
            context.intent,
            context.session
        );
    }

    static TestContext CreateRunnerContext(
        string prefix,
        string playerType,
        int playerPoint,
        int enemyPoint,
        int enemyHP
    )
    {
        TestContext context = CreateContext(prefix, playerType, playerPoint, enemyPoint, enemyHP);
        context.runtimeState = new BattleRuntimeState();
        context.runtimeState.SetCharacters(context.ally, context.allyB, context.enemy);
        context.runtimeState.SetActionSlots(new List<BattleActionSlot> { context.slot });
        context.runtimeState.SetIntentQueue(new List<BattleEnemyIntent> { context.intent });
        context.item = new BattleExecutionItem(
            1,
            BattleExecutionItemType.RespondedEnemyIntent,
            context.intent,
            context.slot
        );
        context.executionPlan = new BattleExecutionPlan();
        context.executionPlan.AddItem(context.item);
        context.runtimeState.SetExecutionPlan(context.executionPlan);
        context.controller = new BattleLifecycleController(context.runtimeState);
        context.controller.TryInitializeToPrepare(out string failureMessage);
        return context;
    }

    static bool BeginAndRoll(TestContext context)
    {
        bool began = context.controller.TryBeginPausableExecution(
                new BattleRollGateSettings(BattleRollMode.Manual, 0f, 0f),
                out string beginFailure
            );
        bool actionBeginShown = began && string.IsNullOrEmpty(beginFailure) &&
            context.controller.AdvancePausableExecution(
                0f,
                out string actionBeginFailure
            ) &&
            string.IsNullOrEmpty(actionBeginFailure);
        bool rolled = actionBeginShown &&
            context.controller.TryRequestManualRoll(out string rollFailure) &&
            string.IsNullOrEmpty(rollFailure);
        bool rollResultShown = rolled &&
            context.controller.AdvancePausableExecution(
                0f,
                out string rollResultFailure
            ) &&
            string.IsNullOrEmpty(rollResultFailure);
        return began && actionBeginShown && rolled && rollResultShown;
    }

    static bool CommitRunner(TestContext context)
    {
        if (context.item.isCompleted)
        {
            return context.controller.TryCommitNextResolutionStep(
                    out string completedFailure
                ) &&
                string.IsNullOrEmpty(completedFailure);
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

    static TestContext CreateContext(
        string prefix,
        string playerType,
        int playerPoint,
        int enemyPoint,
        int enemyHP
    )
    {
        TestContext context = new TestContext
        {
            ally = new CharacterData(prefix + "_A", 30, 10, 10),
            allyB = new CharacterData(prefix + "_B", 30, 8, 8),
            enemy = new CharacterData(prefix + "_Enemy", enemyHP, 5, 5)
        };
        context.playerCard = CreateCard(context.ally, prefix + "_player", playerType, playerPoint);
        context.enemyCard = CreateCard(context.enemy, prefix + "_enemy", CardType.Attack, enemyPoint);
        context.intent = new BattleEnemyIntent(
            prefix + "_intent",
            context.enemy,
            context.enemyCard,
            context.ally,
            1,
            1
        );
        context.slot = new BattleActionSlot(context.ally, 1);
        context.slot.AssignResponse(context.ally, context.playerCard, context.intent, false);
        return context;
    }

    static BattleCardState CreateCard(
        CharacterData owner,
        string id,
        string cardType,
        int point
    )
    {
        CardTestData data = new CardTestData
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
        return BattleCardManager.CreateBattleCard(owner, data, id + "_instance");
    }

    static CardResourceRuleData CreateBulletResourceRule(int consumeAmount)
    {
        return new CardResourceRuleData
        {
            resourceType = "BuffStack",
            resourceID = "Bullet",
            requiredStackForNormalVersion = 1,
            fallbackMinPoint = 1,
            fallbackMaxPoint = 1,
            consumeAmountOnSuccess = consumeAmount
        };
    }

    static bool VerifyGenericUnilateralRollPanelSupport()
    {
        bool allDeliveriesSupported = true;
        string[] deliveries =
        {
            AttackDeliveryMode.Melee,
            AttackDeliveryMode.CloseRangeShoot,
            AttackDeliveryMode.LongRangeShoot
        };
        for (int index = 0; index < deliveries.Length; index++)
        {
            allDeliveriesSupported &= VerifySupportedUnilateralRollPanelPlan(
                BattleResolutionPlanKind.FreeActionAttack,
                true,
                deliveries[index]
            );
            allDeliveriesSupported &= VerifySupportedUnilateralRollPanelPlan(
                BattleResolutionPlanKind.UnrespondedEnemyAttack,
                false,
                deliveries[index]
            );
        }

        BattleResolutionPlan pending = CreateUnilateralRollPanelPlan(
            BattleResolutionPlanKind.FreeActionAttack,
            true,
            AttackDeliveryMode.Melee,
            false
        );
        BattleResolutionPlan rolled = CreateUnilateralRollPanelPlan(
            BattleResolutionPlanKind.UnrespondedEnemyAttack,
            false,
            AttackDeliveryMode.Melee,
            true
        );
        BattleResolutionPlan invalidKind = CreateUnilateralRollPanelPlan(
            BattleResolutionPlanKind.RespondedClash,
            true,
            AttackDeliveryMode.Melee,
            true
        );
        BattleResolutionPlan invalidCard = CreateUnilateralRollPanelPlan(
            BattleResolutionPlanKind.FreeActionAttack,
            true,
            AttackDeliveryMode.Melee,
            true
        );
        invalidCard.sourceCardState.cardData.cardType = CardType.Defense;
        BattleResolutionPlan invalidOwnership = CreateUnilateralRollPanelPlan(
            BattleResolutionPlanKind.FreeActionAttack,
            true,
            AttackDeliveryMode.Melee,
            true
        );
        invalidOwnership.enemyCardUsed = true;

        return allDeliveriesSupported &&
            BattleActionRollPanelHost.IsSupportedUnilateralAttackPlan(
                pending,
                false
            ) && !BattleActionRollPanelHost.IsSupportedUnilateralAttackPlan(
                pending,
                true
            ) && BattleActionRollPanelHost.IsSupportedUnilateralAttackPlan(
                rolled,
                true
            ) &&
            BattleActionRollPanelHost.ShouldShowUnilateralOnAllySide(pending) &&
            !BattleActionRollPanelHost.ShouldShowUnilateralOnAllySide(rolled) &&
            !BattleActionRollPanelHost.IsSupportedUnilateralAttackPlan(
                invalidKind,
                true
            ) && !BattleActionRollPanelHost.IsSupportedUnilateralAttackPlan(
                invalidCard,
                true
            ) && !BattleActionRollPanelHost.IsSupportedUnilateralAttackPlan(
                invalidOwnership,
                true
            );
    }

    static bool VerifySupportedUnilateralRollPanelPlan(
        BattleResolutionPlanKind planKind,
        bool playerSource,
        string delivery,
        bool rolled = true
    )
    {
        BattleResolutionPlan plan = CreateUnilateralRollPanelPlan(
            planKind,
            playerSource,
            delivery,
            rolled
        );
        return BattleActionRollPanelHost.IsSupportedUnilateralAttackPlan(
                plan,
                true
            ) && BattleActionRollPanelHost.ShouldShowUnilateralOnAllySide(
                plan
            ) == playerSource;
    }

    static BattleResolutionPlan CreateUnilateralRollPanelPlan(
        BattleResolutionPlanKind planKind,
        bool playerSource,
        string delivery,
        bool rolled
    )
    {
        CharacterData attacker = new CharacterData(
            "roll_panel_attacker",
            30,
            10,
            10
        );
        CharacterData target = new CharacterData(
            "roll_panel_target",
            30,
            10,
            10
        );
        CardTestData data = new CardTestData
        {
            cardID = "roll_panel_attack",
            cardName = "RollPanel Attack",
            cardType = CardType.Attack,
            attackDeliveryMode = delivery,
            minPoint = 1,
            maxPoint = 1
        };
        BattleResolutionPlan plan = new BattleResolutionPlan(
            null,
            null,
            null,
            null
        );
        plan.planKind = planKind;
        plan.attacker = attacker;
        plan.target = target;
        plan.sourceCardState = BattleCardManager.CreateBattleCard(
            attacker,
            data,
            "roll_panel_attack_instance"
        );
        plan.playerCardUsed = playerSource;
        plan.enemyCardUsed = !playerSource;
        plan.freeActionHasRolled = rolled;
        return plan;
    }

    static void AddProbeEffect(
        BattleCardState cardState,
        string timing,
        int stack
    )
    {
        cardState.cardData.effects.Add(new CardEffectData
        {
            trigger = timing,
            effectType = CardEffectType.ApplyBuff,
            target = CardTargetType.Self,
            buffType = "Bullet",
            stack = stack,
            duration = -1,
            applyTiming = "Immediate"
        });
    }

    static void AddNamedProbeEffect(
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
            duration = 2,
            applyTiming = "Immediate"
        });
    }
}


// 验证 Roll Panel 只消费 Presenter 已确定的生命周期语义，不参与规则判断。
public static class BattleActionRollPanelLifecycleTests
{
    public static void Run()
    {
        Debug.Log("===== BattleActionRollPanelLifecycle 聚合测试开始 =====");

        bool a = VerifyActionBeginDoesNotCompleteBeforePresentation();
        bool b = VerifyReadablePanelBlocksActionBeginCompletion();
        bool c = VerifyUnifiedClashCoverage();
        bool d = VerifyTerminalResultUsesExitLifecycle();
        bool e = VerifyVisibleTieRefreshDoesNotFadeInAgain();
        bool f = VerifyOrdinaryAttackTieUsesTiePresentation();
        bool g = VerifyLongRangeTieIsSilentAndKeepsPanel();
        bool h = VerifyImmediateSafetyCleanupRemainsAvailable();
        bool i = VerifyActionBeginGateReleasesCurrentRequest();
        bool j = VerifyTerminalRollResultGateReleasesCurrentRequest();
        bool k = VerifyStaleRequestCannotReleaseOwnership();

        bool[] results = { a, b, c, d, e, f, g, h, i, j, k };
        string[] names =
        {
            "A ActionBegin初始阶段不会提前完成",
            "B WaitingForRoll前必须等Panel可读",
            "C Attack/Defense/Dodge共用Roll Panel",
            "D Terminal RollResult使用Hold与FadeOut",
            "E Tie刷新Visible Panel不重新FadeIn",
            "F 普通Attack Tie保留Panel并播放Tie表现",
            "G LongRange Tie保留Panel且不播放Tie表现",
            "H Cancel/Disable异常清理仍可立即隐藏Panel",
            "I ActionBegin局部与Panel完成后释放当前Request",
            "J Terminal RollResult FadeOut后释放当前Request",
            "K Stale Request callback不能释放新Request"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log("模式104 " + names[index] + "：" + results[index]);
            allPassed &= results[index];
        }

        Debug.Log("模式104 聚合结果：" + allPassed);
    }

    static bool VerifyActionBeginDoesNotCompleteBeforePresentation()
    {
        return !BattleActionRollPanelHost.CanCompleteActionBegin(
            false,
            false,
            false
        );
    }

    static bool VerifyReadablePanelBlocksActionBeginCompletion()
    {
        return !BattleActionRollPanelHost.CanCompleteActionBegin(
                true,
                true,
                false
            ) && BattleActionRollPanelHost.CanCompleteActionBegin(
                true,
                true,
                true
            );
    }

    static bool VerifyUnifiedClashCoverage()
    {
        return BattleActionRollPanelHost.IsSupportedClashType(
                BattleClashType.AttackVsAttack
            ) && BattleActionRollPanelHost.IsSupportedClashType(
                BattleClashType.DefenseVsAttack
            ) && BattleActionRollPanelHost.IsSupportedClashType(
                BattleClashType.DodgeVsAttack
            );
    }

    static bool VerifyTerminalResultUsesExitLifecycle()
    {
        return BattleActionRollPanelHost.ShouldUseTerminalExit(
                BattlePresentationResultKind.SideAWin
            ) && BattleActionRollPanelHost.ShouldUseTerminalExit(
                BattlePresentationResultKind.DefenseFullBlock
            ) && BattleActionRollPanelHost.ShouldUseTerminalExit(
                BattlePresentationResultKind.DefenseReducedDamage
            ) && BattleActionRollPanelHost.ShouldUseTerminalExit(
                BattlePresentationResultKind.DodgeSuccess
            ) && BattleActionRollPanelHost.ShouldUseTerminalExit(
                BattlePresentationResultKind.DodgeFailed
            ) && BattleActionRollPanelHost.ShouldUseTerminalExit(
                BattlePresentationResultKind.UnilateralAttack
            );
    }

    static bool VerifyVisibleTieRefreshDoesNotFadeInAgain()
    {
        return !BattleActionRollPanelHost.ShouldStartFadeIn(
                BattleActionRollPanelLifecycleState.Visible
            ) && BattleActionRollPanelHost.ShouldStartFadeIn(
                BattleActionRollPanelLifecycleState.Hidden
            );
    }

    static bool VerifyOrdinaryAttackTieUsesTiePresentation()
    {
        return !BattleActionRollPanelHost.ShouldUseTerminalExit(
                BattlePresentationResultKind.AttackTie
            ) && BattleSceneExecutionPresenter.ShouldPlayAnimatedAttackTie(
                BattlePresentationHandlerKind.AttackVsAttack,
                BattlePresentationGrammarKind.MeleeClash,
                BattlePresentationResultKind.AttackTie
            );
    }

    static bool VerifyLongRangeTieIsSilentAndKeepsPanel()
    {
        return !BattleActionRollPanelHost.ShouldUseTerminalExit(
                BattlePresentationResultKind.AttackTie
            ) && !BattleSceneExecutionPresenter.ShouldPlayAnimatedAttackTie(
                BattlePresentationHandlerKind.AttackVsAttack,
                BattlePresentationGrammarKind.LongRangeVsMeleeClash,
                BattlePresentationResultKind.AttackTie
            );
    }

    static bool VerifyImmediateSafetyCleanupRemainsAvailable()
    {
        BattleActionRollPanelHost.HideImmediate();
        return BattleActionRollPanelHost.IsHidden;
    }

    static bool VerifyActionBeginGateReleasesCurrentRequest()
    {
        const long requestId = 10401L;
        long activeRequestId = requestId;
        bool gateReady = BattleActionRollPanelHost.CanCompleteActionBegin(
            true,
            true,
            true
        );

        return gateReady &&
            BattleSceneExecutionPresenter
                .TryReleasePresentationRequestOwnership(
                    ref activeRequestId,
                    requestId
                ) &&
            activeRequestId == 0L;
    }

    static bool VerifyTerminalRollResultGateReleasesCurrentRequest()
    {
        const long requestId = 10402L;
        long activeRequestId = requestId;
        bool blockedBeforeFadeOut =
            !BattleSceneExecutionPresenter.CanCompleteRollResult(
                true,
                true,
                false
            );
        bool gateReady = BattleSceneExecutionPresenter.CanCompleteRollResult(
            true,
            true,
            true
        );

        return blockedBeforeFadeOut && gateReady &&
            BattleSceneExecutionPresenter
                .TryReleasePresentationRequestOwnership(
                    ref activeRequestId,
                    requestId
                ) &&
            activeRequestId == 0L;
    }

    static bool VerifyStaleRequestCannotReleaseOwnership()
    {
        const long activeRequest = 10403L;
        const long staleRequest = 10402L;
        long activeRequestId = activeRequest;

        return !BattleSceneExecutionPresenter
                .TryReleasePresentationRequestOwnership(
                    ref activeRequestId,
                    staleRequest
                ) &&
            activeRequestId == activeRequest;
    }
}


// Mode85：只验证LongRangeShoot终局资源提交，不包含任何射击表现。
public static class BattleLongRangeShootResourceContractTests
{
    sealed class TestContext
    {
        public CharacterData ally;
        public CharacterData enemy;
        public BattleCardState playerCard;
        public BattleCardState enemyCard;
        public BattleActionSlot slot;
        public BattleEnemyIntent intent;
        public BattleClashSession session;
    }

    public static void Run()
    {
        Debug.Log("===== BattleLongRangeShootResourceContractBasic 聚合测试开始 =====");

        bool a = VerifyMeleeLoseKeepsResource();
        bool b = VerifyLongRangeShootWinConsumesOnce();
        bool c = VerifyLongRangeShootLoseConsumesOnceWithoutWinnerEffects();
        bool d = VerifyTieDoesNotConsume();
        bool e = VerifyMultipleTiesThenWinConsumesOnce();
        bool f = VerifyMultipleTiesThenLoseConsumesOnce();
        bool g = VerifyDodgeSuccessConsumesWithoutDamage();
        bool h = VerifyDodgeFailedConsumesWithDamage();
        bool i = VerifyGuardResultsConsume();
        bool j = VerifyUnrespondedConsumesExactlyOnce();
        bool k = VerifyTieLimitDoesNotConsume();

        Debug.Log("模式85 A Melee Attack Lose保持旧资源语义：" + a);
        Debug.Log("模式85 B LongRangeShoot Win只支付一次：" + b);
        Debug.Log("模式85 C LongRangeShoot Lose支付且不触发败方胜者效果：" + c);
        Debug.Log("模式85 D LongRangeShoot Tie不支付：" + d);
        Debug.Log("模式85 E 多次Tie后Win总共支付一次：" + e);
        Debug.Log("模式85 F 多次Tie后Lose总共支付一次：" + f);
        Debug.Log("模式85 G DodgeSuccess支付且零伤害：" + g);
        Debug.Log("模式85 H DodgeFailed支付且正常伤害：" + h);
        Debug.Log("模式85 I Guard FullBlock与ReducedDamage均支付：" + i);
        Debug.Log("模式85 J Unresponded LongRangeShoot只支付一次：" + j);
        Debug.Log("模式85 K TieLimit安全结束且不支付：" + k);
        Debug.Log("模式85 聚合结果：" +
            (a && b && c && d && e && f && g && h && i && j && k));
    }

    static bool VerifyMeleeLoseKeepsResource()
    {
        TestContext context = CreateRespondedContext(
            "shoot85_a",
            CardType.Attack,
            4,
            false,
            true,
            6,
            false,
            false
        );
        BattleResolveResult result = ResolveFinalized(context, out _);
        return result != null && result.resultType == "EnemyWin" &&
            !result.playerCardUsed && context.ally.GetBuffStack("Bullet") == 3;
    }

    static bool VerifyLongRangeShootWinConsumesOnce()
    {
        TestContext context = CreateRespondedContext(
            "shoot85_b",
            CardType.Attack,
            6,
            true,
            true,
            4,
            false,
            false
        );
        BattleResolveResult result = ResolveFinalized(context, out BattleResolutionPlan plan);
        int bulletAfterFirst = context.ally.GetBuffStack("Bullet");
        bool repeated = RepeatCompletedPlan(plan, result);
        return result != null && result.resultType == "PlayerWin" &&
            result.playerCardUsed && result.damage == 6 &&
            bulletAfterFirst == 2 && context.ally.GetBuffStack("Bullet") == 2 &&
            repeated;
    }

    static bool VerifyLongRangeShootLoseConsumesOnceWithoutWinnerEffects()
    {
        TestContext context = CreateRespondedContext(
            "shoot85_c",
            CardType.Attack,
            4,
            true,
            true,
            6,
            false,
            false
        );
        AddResolvedProbe(context.playerCard, "GuardUp");
        BattleResolveResult result = ResolveFinalized(context, out BattleResolutionPlan plan);
        int bulletAfterFirst = context.ally.GetBuffStack("Bullet");
        bool repeated = RepeatCompletedPlan(plan, result);
        return result != null && result.resultType == "EnemyWin" &&
            !result.playerCardUsed && context.enemy.currentHP == 30 &&
            context.ally.currentHP == 24 &&
            context.ally.GetBuffStack("GuardUp") == 0 &&
            bulletAfterFirst == 2 && context.ally.GetBuffStack("Bullet") == 2 &&
            repeated;
    }

    static bool VerifyTieDoesNotConsume()
    {
        TestContext context = CreateRespondedContext(
            "shoot85_d",
            CardType.Attack,
            5,
            true,
            true,
            5,
            false,
            false
        );
        bool began = TryBegin(context);
        bool rolled = began && context.session.RollNextAttempt();
        return rolled && !context.session.IsFinalized &&
            context.session.AttemptResult == BattleClashAttemptResult.AttackTie &&
            context.ally.GetBuffStack("Bullet") == 3;
    }

    static bool VerifyMultipleTiesThenWinConsumesOnce()
    {
        TestContext context = CreateRespondedContext(
            "shoot85_e",
            CardType.Attack,
            5,
            true,
            true,
            5,
            false,
            false
        );
        if (!RollTwoTies(context))
        {
            return false;
        }

        SetPoint(context.session.SideA, 6);
        if (!context.session.RollNextAttempt() || !context.session.IsFinalized)
        {
            return false;
        }

        BattleResolveResult result = CommitFinalizedSession(context, out BattleResolutionPlan plan);
        int bulletAfterFirst = context.ally.GetBuffStack("Bullet");
        return result != null && result.resultType == "PlayerWin" &&
            context.session.AttemptIndex == 3 && bulletAfterFirst == 2 &&
            RepeatCompletedPlan(plan, result) &&
            context.ally.GetBuffStack("Bullet") == 2;
    }

    static bool VerifyMultipleTiesThenLoseConsumesOnce()
    {
        TestContext context = CreateRespondedContext(
            "shoot85_f",
            CardType.Attack,
            5,
            true,
            true,
            5,
            false,
            false
        );
        if (!RollTwoTies(context))
        {
            return false;
        }

        SetPoint(context.session.SideB, 6);
        if (!context.session.RollNextAttempt() || !context.session.IsFinalized)
        {
            return false;
        }

        BattleResolveResult result = CommitFinalizedSession(context, out BattleResolutionPlan plan);
        int bulletAfterFirst = context.ally.GetBuffStack("Bullet");
        return result != null && result.resultType == "EnemyWin" &&
            context.session.AttemptIndex == 3 && bulletAfterFirst == 2 &&
            RepeatCompletedPlan(plan, result) &&
            context.ally.GetBuffStack("Bullet") == 2;
    }

    static bool VerifyDodgeSuccessConsumesWithoutDamage()
    {
        TestContext context = CreateRespondedContext(
            "shoot85_g",
            CardType.Dodge,
            6,
            false,
            false,
            4,
            true,
            true
        );
        BattleResolveResult result = ResolveFinalized(context, out _);
        return result != null && result.resultType == "DodgeSuccess" &&
            !result.hasDamage && context.ally.currentHP == 30 &&
            context.enemy.GetBuffStack("Bullet") == 2;
    }

    static bool VerifyDodgeFailedConsumesWithDamage()
    {
        TestContext context = CreateRespondedContext(
            "shoot85_h",
            CardType.Dodge,
            2,
            false,
            false,
            6,
            true,
            true
        );
        BattleResolveResult result = ResolveFinalized(context, out _);
        return result != null && result.resultType == "DodgeFailed" &&
            result.damage == 6 && context.ally.currentHP == 24 &&
            context.enemy.GetBuffStack("Bullet") == 2;
    }

    static bool VerifyGuardResultsConsume()
    {
        TestContext fullBlock = CreateRespondedContext(
            "shoot85_i_full",
            CardType.Defense,
            6,
            false,
            false,
            4,
            true,
            true
        );
        BattleResolveResult fullResult = ResolveFinalized(fullBlock, out _);

        TestContext reduced = CreateRespondedContext(
            "shoot85_i_reduced",
            CardType.Defense,
            2,
            false,
            false,
            6,
            true,
            true
        );
        BattleResolveResult reducedResult = ResolveFinalized(reduced, out _);

        return fullResult != null && fullResult.resultType == "DefenseFullBlock" &&
            !fullResult.hasDamage && fullBlock.ally.currentHP == 30 &&
            fullBlock.enemy.GetBuffStack("Bullet") == 2 &&
            reducedResult != null && reducedResult.resultType == "DefenseReducedDamage" &&
            reducedResult.hasDamage && reduced.ally.currentHP < 30 &&
            reduced.enemy.GetBuffStack("Bullet") == 2;
    }

    static bool VerifyUnrespondedConsumesExactlyOnce()
    {
        CharacterData target = new CharacterData("shoot85_j_target", 30, 10, 10);
        CharacterData shooter = new CharacterData("shoot85_j_shooter", 30, 5, 5);
        BattleCardState shootCard = CreateCard(
            shooter,
            "shoot85_j_card",
            CardType.Attack,
            6,
            true,
            true
        );
        BattleEnemyIntent intent = new BattleEnemyIntent(
            "shoot85_j_intent",
            shooter,
            shootCard,
            target,
            1,
            1
        );

        BattleResolveResult result = BattleResolver.ResolveUnrespondedEnemyIntent(intent);
        return result != null && result.enemyCardUsed && result.damage == 6 &&
            target.currentHP == 24 && shooter.GetBuffStack("Bullet") == 2;
    }

    static bool VerifyTieLimitDoesNotConsume()
    {
        TestContext context = CreateRespondedContext(
            "shoot85_k",
            CardType.Attack,
            5,
            true,
            true,
            5,
            false,
            false
        );
        if (!TryBegin(context))
        {
            return false;
        }

        for (int index = 0; index < BattleClashSession.MaxAttackTieCount; index++)
        {
            if (!context.session.RollNextAttempt())
            {
                return false;
            }
        }

        BattleResolveResult result = CommitFinalizedSession(context, out BattleResolutionPlan plan);
        int bulletAfterFirst = context.ally.GetBuffStack("Bullet");
        return result != null && result.resultType == "TieLimit" &&
            bulletAfterFirst == 3 && RepeatCompletedPlan(plan, result) &&
            context.ally.GetBuffStack("Bullet") == 3;
    }

    static TestContext CreateRespondedContext(
        string prefix,
        string playerCardType,
        int playerPoint,
        bool playerLongRangeShoot,
        bool playerUsesBullet,
        int enemyPoint,
        bool enemyLongRangeShoot,
        bool enemyUsesBullet
    )
    {
        TestContext context = new TestContext
        {
            ally = new CharacterData(prefix + "_ally", 30, 10, 10),
            enemy = new CharacterData(prefix + "_enemy", 30, 5, 5)
        };
        context.playerCard = CreateCard(
            context.ally,
            prefix + "_player_card",
            playerCardType,
            playerPoint,
            playerLongRangeShoot,
            playerUsesBullet
        );
        context.enemyCard = CreateCard(
            context.enemy,
            prefix + "_enemy_card",
            CardType.Attack,
            enemyPoint,
            enemyLongRangeShoot,
            enemyUsesBullet
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
        return context;
    }

    static BattleCardState CreateCard(
        CharacterData owner,
        string id,
        string cardType,
        int point,
        bool longRangeShoot,
        bool usesBullet
    )
    {
        CardTestData data = new CardTestData
        {
            cardID = id + "_data",
            cardName = id,
            cardType = cardType,
            attackDeliveryMode = longRangeShoot
                ? AttackDeliveryMode.LongRangeShoot
                : null,
            isClashable = cardType == CardType.Attack || cardType == CardType.Dodge,
            minPoint = point,
            maxPoint = point,
            damageFormula = "PointAsDamage",
            defenseFormula = cardType == CardType.Defense ? "PointAsDefense" : "",
            effects = new List<CardEffectData>(),
            resourceRule = usesBullet ? CreateBulletResourceRule() : null
        };
        if (usesBullet)
        {
            owner.AddBuff("Bullet", 3, -1);
        }
        return BattleCardManager.CreateBattleCard(owner, data, id + "_instance");
    }

    static CardResourceRuleData CreateBulletResourceRule()
    {
        return new CardResourceRuleData
        {
            resourceType = "BuffStack",
            resourceID = "Bullet",
            requiredStackForNormalVersion = 1,
            fallbackMinPoint = 0,
            fallbackMaxPoint = 0,
            consumeAmountOnSuccess = 1
        };
    }

    static void AddResolvedProbe(BattleCardState cardState, string buffID)
    {
        cardState.cardData.effects.Add(new CardEffectData
        {
            trigger = BattleTiming.Resolved,
            effectType = CardEffectType.ApplyBuff,
            target = CardTargetType.Self,
            buffType = buffID,
            stack = 1,
            duration = -1,
            applyTiming = "Immediate"
        });
    }

    static bool TryBegin(TestContext context)
    {
        BattleResolveResult failure = BattleResolver.TryBeginRespondedClash(
            context.slot,
            context.intent,
            out context.session
        );
        return failure == null && context.session != null;
    }

    static BattleResolveResult ResolveFinalized(
        TestContext context,
        out BattleResolutionPlan plan
    )
    {
        plan = null;
        if (!TryBegin(context))
        {
            return null;
        }

        for (int index = 0; index <= BattleClashSession.MaxAttackTieCount &&
            !context.session.IsFinalized; index++)
        {
            if (!context.session.RollNextAttempt())
            {
                return null;
            }
        }
        return CommitFinalizedSession(context, out plan);
    }

    static BattleResolveResult CommitFinalizedSession(
        TestContext context,
        out BattleResolutionPlan plan
    )
    {
        plan = BattleResolver.BuildRespondedClashResolutionPlan(
            context.slot,
            context.intent,
            context.session
        );
        if (plan == null)
        {
            return null;
        }

        BattleResolveResult result = null;
        for (int step = 0; step < 4 &&
            plan.State != BattleResolutionPlanState.Completed; step++)
        {
            if (!BattleResolver.TryCommitNextResolutionStep(plan, out result))
            {
                return null;
            }
        }
        return plan.CompletedResult ?? result;
    }

    static bool RollTwoTies(TestContext context)
    {
        if (!TryBegin(context))
        {
            return false;
        }

        for (int index = 0; index < 2; index++)
        {
            if (!context.session.RollNextAttempt() || context.session.IsFinalized ||
                context.session.AttemptResult != BattleClashAttemptResult.AttackTie ||
                context.ally.GetBuffStack("Bullet") != 3)
            {
                return false;
            }
        }
        return true;
    }

    static void SetPoint(BattleClashSideState side, int point)
    {
        side.resourceSnapshot.selectedMinPoint = point;
        side.resourceSnapshot.selectedMaxPoint = point;
    }

    static bool RepeatCompletedPlan(
        BattleResolutionPlan plan,
        BattleResolveResult expectedResult
    )
    {
        return plan != null && expectedResult != null &&
            BattleResolver.TryCommitNextResolutionStep(
                plan,
                out BattleResolveResult repeatedResult
            ) &&
            object.ReferenceEquals(expectedResult, repeatedResult);
    }
}
