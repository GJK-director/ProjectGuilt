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
        Debug.Log(
            "模式82 聚合结果：" +
            (a && b && c && d && e && f && g && h && i && j && k && l &&
             m && n && o)
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
