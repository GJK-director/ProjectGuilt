using System.Collections.Generic;

// Formal rule cases, called by the retained Mode82 wrapper. No Unity scene required.
public static class CardCombatDiceTests
{
    sealed class Fixture
    {
        public CharacterData ally, enemy;
        public BattleCardState card, opponent;
        public BattleActionSlot slot;
        public BattleEnemyIntent intent;
        public BattleClashSession session;
        public BattleResolutionPlan plan;
    }

    // Shared synthetic recipe for the Mode83 presentation protocol cases.
    internal static void SetFixedDice(CardTestData card, int clash = 10, int first = 2, int second = 3)
    {
        card.clashDie = new ClashDieData { min = clash, max = clash };
        card.damageDice = new[]
        {
            new DamageDieData { min = first, max = first, presentation = CardCombatDiceRules.Melee },
            new DamageDieData { min = second, max = second, presentation = CardCombatDiceRules.CloseRangeShoot }
        };
    }

    public static bool OptionalDataAndValidation()
    {
        CardTestData card = TestCardFactory.CreateFixedData("dice_validation", CardType.Attack, 1);
        bool legacy = CardCombatDiceRules.Validate(card, out _) && !CardCombatDiceRules.HasDice(card);
        card.clashDie = new ClashDieData { min = 1, max = 10 };
        bool partialRejected = !CardCombatDiceRules.Validate(card, out _);
        SetFixedDice(card);
        bool valid = CardCombatDiceRules.Validate(card, out _);
        card.damageDice[1].max = 0;
        bool invalidRange = !CardCombatDiceRules.Validate(card, out _);
        SetFixedDice(card);
        card.damageImpactPercents = new[] { 50, 50 };
        bool mixedRejected = !CardCombatDiceRules.Validate(card, out _);
        return legacy && partialRejected && valid && invalidRange && mixedRejected;
    }

    public static bool ProductionTemplateIsOptIn()
    {
        var cards = CardDataLoader.LoadCardData();
        CardTestData prototype = CardDataLoader.FindCardByID(cards, CardCombatDiceRules.PrototypeCardID);
        if (prototype == null || !CardCombatDiceRules.Validate(prototype, out _)) return false;
        int splitCount = 0;
        foreach (CardTestData card in cards)
        {
            if (CardCombatDiceRules.HasDice(card)) splitCount++;
            else if (card.clashDie != null || card.damageDice != null) return false;
        }
        return splitCount == 1 && prototype.clashDie.min == 1 && prototype.clashDie.max == 10 &&
            prototype.damageDice[0].min == 1 && prototype.damageDice[0].max == 5 &&
            prototype.damageDice[1].min == 1 && prototype.damageDice[1].max == 5;
    }

    public static bool ClashRangeAndPointBuffsDoNotBecomeDamage()
    {
        Fixture f = Create();
        f.ally.AddBuff("Strength", 1, 1);
        if (!Build(f)) return false;
        bool clash = f.session.SideAPoint == 12 && f.session.AttemptIndex == 1 &&
            BattleCalculator.GetFinalClashPoint(f.ally, f.card.cardData) == 12;
        bool pending = f.plan.requiresClashWinPresentation && f.plan.impacts.Count == 2 &&
            !f.plan.impacts[0].damageDieRolled && !f.plan.impacts[1].damageDieRolled && f.enemy.currentHP == 30;
        BattleResolver.PrepareDamageDie(f.plan.impacts[0]);
        f.card.cardData.damageDice[0].min = f.card.cardData.damageDice[0].max = 5;
        BattleResolver.PrepareDamageDie(f.plan.impacts[0]);
        bool capturedOnce = f.plan.impacts[0].damageDieRoll == 2 && !f.plan.impacts[1].damageDieRolled;
        BattleResolver.TryCommitNextResolutionStep(f.plan, out _);
        BattleResolver.TryCommitNextResolutionStep(f.plan, out BattleResolveResult result);
        return clash && pending && capturedOnce && result != null && result.damage == 5 &&
            f.plan.impacts[1].damageDieRoll == 3 && f.enemy.currentHP == 25;
    }

    public static bool EachHitHasEventsAndUsesLiveModifiers()
    {
        Fixture f = Create();
        f.ally.AddBuff("Bullet", 3);
        // The first committed damage grants a real existing damage-taken modifier for the second.
        f.card.cardData.effects.Add(new CardEffectData
        {
            trigger = BattleTiming.AfterDamage, effectType = CardEffectType.ApplyBuff,
            target = CardTargetType.Target, buffID = "Vulnerable", stackDelta = 1,
            hasIntensityDelta = true, intensityDelta = 90, applyTiming = "Immediate"
        });
        if (!Build(f)) return false;
        var counts = new Dictionary<string, int>();
        var hitImpacts = new List<BattleImpact>();
        var previous = BattleEventProcessor.TestEventObserver;
        try
        {
            BattleEventProcessor.TestEventObserver = context =>
            {
                if (context.cardState != f.card) return;
                counts.TryGetValue(context.timing, out int count);
                counts[context.timing] = count + 1;
                if (context.timing == BattleTiming.Hit) hitImpacts.Add(context.impact);
            };
            BattleResolver.TryCommitNextResolutionStep(f.plan, out BattleResolveResult first);
            int hp = f.enemy.currentHP;
            BattleResolver.CommitImpact(f.plan, f.plan.impacts[0]);
            bool firstOnly = first == null && hp == 28 && f.enemy.currentHP == hp &&
                !f.plan.impacts[1].damageDieRolled;
            BattleResolver.TryCommitNextResolutionStep(f.plan, out BattleResolveResult final);
            BattleResolver.TryCommitNextResolutionStep(f.plan, out _);
            return firstOnly && final != null && f.enemy.currentHP == 22 && final.damage == 8 &&
                Count(counts, BattleTiming.Hit) == 2 && Count(counts, BattleTiming.DamageModifier) == 2 &&
                Count(counts, BattleTiming.AfterDamage) == 2 && Count(counts, BattleTiming.CardUsed) == 1 &&
                Count(counts, BattleTiming.CardResolved) == 1 && Count(counts, BattleTiming.ClashWin) == 1 &&
                hitImpacts.Count == 2 && hitImpacts[0] != hitImpacts[1] &&
                f.ally.GetBuffStack("Bullet") == 3;
        }
        finally { BattleEventProcessor.TestEventObserver = previous; }
    }

    public static bool FirstLethalStillHitsTwiceAndDefeatsOnce()
    {
        Fixture f = Create(enemyHP: 1);
        if (!Build(f)) return false;
        int hits = 0, kills = 0;
        var previous = BattleEventProcessor.TestEventObserver;
        try
        {
            BattleEventProcessor.TestEventObserver = context =>
            {
                if (context.cardState != f.card) return;
                if (context.timing == BattleTiming.Hit) hits++;
                if (context.timing == BattleTiming.AfterKill) kills++;
            };
            BattleResolver.TryCommitNextResolutionStep(f.plan, out _, true);
            bool deferred = f.enemy.currentHP == 0 && !f.enemy.IsDefeated() && hits == 1 && kills == 0;
            BattleResolver.TryCommitNextResolutionStep(f.plan, out _, true);
            bool second = hits == 2 && !f.enemy.IsDefeated() && f.plan.impacts[1].actualDamage == 0 &&
                f.plan.impacts[1].state == BattleImpactState.Committed;
            BattleResolver.CommitDefeatCheckpoint(f.plan);
            BattleResolver.CommitDefeatCheckpoint(f.plan);
            return deferred && second && kills == 1 && f.enemy.IsDefeated() &&
                f.plan.impacts[0].didKill && !f.plan.impacts[1].didKill;
        }
        finally { BattleEventProcessor.TestEventObserver = previous; }
    }

    public static bool LossAndTieKeepLegacyOutcome()
    {
        Fixture loss = Create(clash: 1);
        if (!Build(loss)) return false;
        BattleResolver.TryCommitNextResolutionStep(loss.plan, out BattleResolveResult lost);
        Fixture tie = Create(clash: 5);
        if (!Build(tie)) return false;
        BattleResolver.TryCommitNextResolutionStep(tie.plan, out BattleResolveResult tied);
        return lost != null && lost.resultType == "EnemyWin" && !loss.plan.requiresClashWinPresentation &&
            loss.plan.impacts.Count == 1 && loss.plan.impacts[0].damageDie == null && loss.ally.currentHP == 25 &&
            tied != null && tied.resultType == "TieLimit" && tie.session.AttemptIndex == 10 &&
            tie.plan.impacts.Count == 0 && !tie.plan.requiresClashWinPresentation && tie.enemy.currentHP == 30;
    }

    public static bool SynchronousResolutionUsesBothDice()
    {
        Fixture f = Create();
        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(f.slot, f.intent);
        return result != null && result.isSuccess && result.damage == 5 &&
            result.clashAttemptCount == 1 && f.enemy.currentHP == 25;
    }

    public static bool UnsupportedEncountersFailBeforeUse()
    {
        Fixture f = Create();
        f.opponent.cardData.attackDeliveryMode = AttackDeliveryMode.LongRangeShoot;
        BattleResolveResult invalid = BattleResolver.TryBeginRespondedClash(f.slot, f.intent, out var session);
        BattleResolutionPlan unilateral = BattleResolver.BuildUnilateralAttackResolutionPlan(
            new BattleExecutionAction(f.ally, f.card, f.slot, null, f.enemy), null, null, out var failure);
        return invalid != null && !invalid.isSuccess && session == null && unilateral == null &&
            failure != null && !failure.isSuccess && f.ally.currentHP == 30 && f.enemy.currentHP == 30 &&
            f.card.currentCooldown == 0;
    }

    static int Count(Dictionary<string, int> counts, string timing) =>
        counts.TryGetValue(timing, out int count) ? count : 0;

    static Fixture Create(int clash = 10, int enemyHP = 30)
    {
        var f = new Fixture
        {
            ally = TestCharacterFactory.Create("dice_player"),
            enemy = TestCharacterFactory.Create("dice_enemy", enemyHP)
        };
        // Deliberately different legacy range: the new clash field must be authoritative.
        f.card = TestCardFactory.CreateState(f.ally, "dice_test", CardType.Attack, 1, cooldown: 0);
        SetFixedDice(f.card.cardData, clash);
        f.opponent = TestCardFactory.CreateState(f.enemy, "dice_opponent", CardType.Attack, 5, cooldown: 0);
        f.intent = new BattleEnemyIntent("dice_intent", f.enemy, f.opponent, f.ally, 1, 1);
        f.slot = new BattleActionSlot(f.ally, 1);
        f.slot.AssignResponse(f.ally, f.card, f.intent, false);
        return f;
    }

    static bool Build(Fixture f)
    {
        if (BattleResolver.TryBeginRespondedClash(f.slot, f.intent, out f.session) != null || f.session == null)
            return false;
        while (!f.session.IsFinalized)
            if (!f.session.RollNextAttempt()) return false;
        f.plan = BattleResolver.BuildRespondedClashResolutionPlan(f.slot, f.intent, f.session);
        return f.plan != null;
    }
}
