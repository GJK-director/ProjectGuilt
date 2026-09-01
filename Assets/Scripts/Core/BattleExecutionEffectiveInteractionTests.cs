// 脚本中文说明：验证 Executor 正式消费 Effective Interaction 的基础契约。
using System.Collections.Generic;
using UnityEngine;

public static class BattleExecutionEffectiveInteractionTests
{
    public static bool Run()
    {
        bool[] results = new bool[14];
        results[0] = VerifyRespondedNoInteraction(CardType.Defense, CardType.Defense);
        results[1] = VerifyRespondedNoInteraction(CardType.Dodge, CardType.Defense);
        results[2] = VerifyRespondedNoInteraction(CardType.Defense, CardType.Dodge);
        results[3] = VerifyRespondedNoInteraction(CardType.Dodge, CardType.Dodge);
        results[4] = VerifyFreeNoInteraction(CardType.Defense);
        results[5] = VerifyFreeNoInteraction(CardType.Dodge);
        results[6] = VerifyAbilityFreeActionStillResolves();
        results[7] = VerifyUnrespondedNoInteraction(CardType.Defense);
        results[8] = VerifyUnrespondedNoInteraction(CardType.Dodge);
        results[9] = VerifyUnrespondedAttackUsesCompatibilityResolver();
        results[10] = VerifyPassiveDefenseEffectiveInteraction();
        results[11] = VerifyContinuousDodgeEffectiveInteractionCanBegin();
        results[12] = VerifyNoInteractionHasNoCardLifecycleSideEffects();
        results[13] = VerifyHandledNoInteractionSlotCannotGuardAgain();

        string[] names =
        {
            "Responded Defense + Defense合法NoInteraction",
            "Responded Dodge + Defense合法NoInteraction",
            "Responded Defense + Dodge合法NoInteraction",
            "Responded Dodge + Dodge合法NoInteraction",
            "FreeAction Defense合法NoInteraction",
            "FreeAction Dodge合法NoInteraction",
            "Ability FreeAction继续正式Resolve",
            "Unresponded Enemy Defense合法NoInteraction",
            "Unresponded Enemy Dodge合法NoInteraction",
            "Unresponded Enemy Attack继续旧单方攻击",
            "Runtime Passive Defense转为AttackVsDefense",
            "Runtime Continuous Dodge转为AttackVsDodge并可Begin",
            "NoInteraction不触发Resolved/CD/Resource",
            "NoInteraction槽位不会再次被守备选择"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式91 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }

        Debug.Log("模式91 Effective Interaction聚合结果：" + allPassed);
        return allPassed;
    }

    static bool VerifyRespondedNoInteraction(
        string responseCardType,
        string enemyCardType
    )
    {
        RespondedFixture fixture = CreateRespondedFixture(
            "mode91_responded_" + responseCardType + "_" + enemyCardType,
            responseCardType,
            enemyCardType
        );
        int targetHp = fixture.player.currentHP;

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(fixture.plan);

        return IsNoInteractionComplete(fixture.item) &&
            fixture.responseSlot.isUsed &&
            fixture.player.currentHP == targetHp &&
            CardLifecycleIsUntouched(fixture.playerCard) &&
            CardLifecycleIsUntouched(fixture.enemyCard) &&
            fixture.item.interactionType == BattleInteractionType.NoInteraction;
    }

    static bool VerifyFreeNoInteraction(string cardType)
    {
        CharacterData actor = CreateCharacter("mode91_free_" + cardType + "_actor");
        CharacterData target = CreateCharacter("mode91_free_" + cardType + "_target");
        BattleCardState card = CreateCard(actor, "mode91_free_" + cardType, cardType, 4);
        BattleActionSlot slot = new BattleActionSlot(actor, 1);
        slot.AssignFreeAction(actor, card, target);
        BattleExecutionItem item = new BattleExecutionItem(
            0,
            BattleExecutionItemType.FreeAction,
            null,
            slot
        );
        item.interactionType = BattleInteractionType.NoInteraction;
        BattleExecutionPlan plan = CreatePlan(item);

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(plan);

        return IsNoInteractionComplete(item) && slot.isUsed &&
            CardLifecycleIsUntouched(card) && target.currentHP == target.maxHP;
    }

    static bool VerifyAbilityFreeActionStillResolves()
    {
        CharacterData actor = CreateCharacter("mode91_ability_actor");
        BattleCardState card = CreateCard(actor, "mode91_ability", "Ability", 0);
        card.cardData.effects.Add(new CardEffectData
        {
            trigger = BattleTiming.OnPlay,
            effectType = CardEffectType.ApplyBuff,
            target = CardTargetType.Self,
            buffType = "Mode91AbilityOnPlay",
            buffName = "Mode91 Ability OnPlay",
            buffCategory = BuffCategory.AbilityBuff,
            stack = 1,
            duration = -1,
            checkTiming = BattleTiming.TurnEnd,
            expireRule = BuffExpireRule.Permanent,
            applyTiming = "Immediate"
        });
        BattleActionSlot slot = new BattleActionSlot(actor, 1);
        slot.AssignFreeAction(actor, card, actor);
        BattleExecutionItem item = new BattleExecutionItem(
            0,
            BattleExecutionItemType.FreeAction,
            null,
            slot
        );
        item.interactionType = BattleInteractionType.NoInteraction;
        BattleExecutionPlan plan = CreatePlan(item);
        BattleExecutionInteractionContext context =
            BattleExecutionInteractionContextFactory.BuildPlanned(item);

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(plan);

        bool effectiveNoInteraction = context.effectiveInteractionType ==
            BattleInteractionType.NoInteraction;
        bool resolverExecuted = item.status == BattleExecutionItemStatus.Executed &&
            item.outcomeReason == BattleExecutionItemOutcomeReason.None;
        bool itemCompleted = item.isCompleted;
        bool slotUsed = slot.isUsed;
        bool abilityOnPlayApplied = actor.GetBuffStack("Mode91AbilityOnPlay") == 1;
        int expectedCooldown = GetExpectedResolvedCooldown(card);
        bool abilityResolved = card.currentCooldown == expectedCooldown;

        Debug.Log(
            "[Test7 Ability] " +
            "effectiveNoInteraction=" + effectiveNoInteraction + ", " +
            "resolverExecuted=" + resolverExecuted + ", " +
            "itemCompleted=" + itemCompleted + ", " +
            "slotUsed=" + slotUsed + ", " +
            "abilityOnPlayApplied=" + abilityOnPlayApplied + ", " +
            "abilityResolved=" + abilityResolved + ", " +
            "currentCooldown=" + card.currentCooldown + ", " +
            "expectedCooldown=" + expectedCooldown
        );

        return effectiveNoInteraction && resolverExecuted && itemCompleted &&
            slotUsed && abilityOnPlayApplied && abilityResolved;
    }

    static bool VerifyUnrespondedNoInteraction(string enemyCardType)
    {
        UnrespondedFixture fixture = CreateUnrespondedFixture(
            "mode91_unresponded_" + enemyCardType,
            enemyCardType,
            4
        );
        BattleCardState unusedGuardCard = CreateCard(
            fixture.target,
            "mode91_unresponded_" + enemyCardType + "_unused_guard",
            CardType.Defense,
            6
        );
        BattleActionSlot unusedGuardSlot = new BattleActionSlot(fixture.target, 1);
        unusedGuardSlot.AssignPassiveGuard(fixture.target, unusedGuardCard);
        fixture.item.passiveGuardCandidates.Add(unusedGuardSlot);
        int targetHp = fixture.target.currentHP;

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(fixture.plan);

        return IsNoInteractionComplete(fixture.item) &&
            fixture.target.currentHP == targetHp &&
            CardLifecycleIsUntouched(fixture.enemyCard) &&
            !unusedGuardSlot.isUsed &&
            CardLifecycleIsUntouched(unusedGuardCard) &&
            fixture.item.interactionType == BattleInteractionType.NoInteraction;
    }

    static bool VerifyUnrespondedAttackUsesCompatibilityResolver()
    {
        UnrespondedFixture fixture = CreateUnrespondedFixture(
            "mode91_unresponded_attack",
            CardType.Attack,
            5
        );
        int targetHp = fixture.target.currentHP;
        BattleExecutionInteractionContext context =
            BattleExecutionInteractionContextFactory.BuildEffective(
                fixture.item,
                null
            );

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(fixture.plan);

        bool plannedInteraction = fixture.item.interactionType ==
            BattleInteractionType.UnilateralAttack;
        bool effectiveInteraction = context.effectiveInteractionType ==
            BattleInteractionType.UnilateralAttack;
        bool damageApplied = fixture.target.currentHP < targetHp;
        int expectedCooldown = GetExpectedResolvedCooldown(fixture.enemyCard);
        bool enemyResolved = fixture.enemyCard.currentCooldown == expectedCooldown;
        bool itemCompleted = fixture.item.status == BattleExecutionItemStatus.Executed &&
            fixture.item.outcomeReason == BattleExecutionItemOutcomeReason.None &&
            fixture.item.isCompleted;

        Debug.Log(
            "[Test10 Unilateral] " +
            "plannedInteraction=" + plannedInteraction + ", " +
            "effectiveInteraction=" + effectiveInteraction + ", " +
            "damageApplied=" + damageApplied + ", " +
            "enemyResolved=" + enemyResolved + ", " +
            "itemCompleted=" + itemCompleted + ", " +
            "hpBefore=" + targetHp + ", " +
            "hpAfter=" + fixture.target.currentHP + ", " +
            "currentCooldown=" + fixture.enemyCard.currentCooldown + ", " +
            "expectedCooldown=" + expectedCooldown
        );

        return plannedInteraction && effectiveInteraction && damageApplied &&
            enemyResolved && itemCompleted;
    }

    static bool VerifyPassiveDefenseEffectiveInteraction()
    {
        UnrespondedFixture fixture = CreateUnrespondedFixture(
            "mode91_passive_defense",
            CardType.Attack,
            4
        );
        BattleCardState defenseCard = CreateCard(
            fixture.target,
            "mode91_passive_defense_card",
            CardType.Defense,
            8
        );
        BattleActionSlot defenseSlot = new BattleActionSlot(fixture.target, 1);
        defenseSlot.AssignPassiveGuard(fixture.target, defenseCard);
        fixture.item.passiveGuardCandidates.Add(defenseSlot);
        int targetHp = fixture.target.currentHP;
        BattleExecutionInteractionContext context =
            BattleExecutionInteractionContextFactory.BuildEffective(
                fixture.item,
                defenseSlot
            );
        BattleGuardSelectionResult selection = BattleGuardSelectionManager
            .SelectHandlingCardForEnemyIntent(
                fixture.item.passiveGuardCandidates,
                fixture.item.enemyIntent
            );

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(fixture.plan);

        bool plannedUnilateral = fixture.item.interactionType ==
            BattleInteractionType.UnilateralAttack;
        bool effectiveAttackVsDefense = context.effectiveInteractionType ==
            BattleInteractionType.AttackVsDefense;
        bool guardSelected = object.ReferenceEquals(selection.slot, defenseSlot);
        int expectedDefenseCooldown = GetExpectedResolvedCooldown(defenseCard);
        int expectedEnemyCooldown = GetExpectedResolvedCooldown(fixture.enemyCard);
        bool defenseResolved = defenseCard.currentCooldown == expectedDefenseCooldown;
        bool enemyResolved = fixture.enemyCard.currentCooldown == expectedEnemyCooldown;
        bool slotUsed = defenseSlot.isUsed;
        bool fullBlockPreservedHp = fixture.target.currentHP == targetHp;
        bool itemCompleted = fixture.item.status == BattleExecutionItemStatus.Executed &&
            fixture.item.outcomeReason == BattleExecutionItemOutcomeReason.None &&
            fixture.item.isCompleted;

        Debug.Log(
            "[Test11 PassiveDefense] " +
            "plannedUnilateral=" + plannedUnilateral + ", " +
            "effectiveAttackVsDefense=" + effectiveAttackVsDefense + ", " +
            "guardSelected=" + guardSelected + ", " +
            "defenseResolved=" + defenseResolved + ", " +
            "enemyResolved=" + enemyResolved + ", " +
            "slotUsed=" + slotUsed + ", " +
            "fullBlockPreservedHp=" + fullBlockPreservedHp + ", " +
            "itemCompleted=" + itemCompleted + ", " +
            "defenseCooldown=" + defenseCard.currentCooldown +
                "/" + expectedDefenseCooldown + ", " +
            "enemyCooldown=" + fixture.enemyCard.currentCooldown +
                "/" + expectedEnemyCooldown
        );

        return plannedUnilateral && effectiveAttackVsDefense && guardSelected &&
            defenseResolved && enemyResolved && slotUsed &&
            fullBlockPreservedHp && itemCompleted;
    }

    static bool VerifyContinuousDodgeEffectiveInteractionCanBegin()
    {
        UnrespondedFixture fixture = CreateUnrespondedFixture(
            "mode91_continuous_dodge",
            CardType.Attack,
            3
        );
        BattleCardState dodgeCard = CreateCard(
            fixture.target,
            "mode91_continuous_dodge_card",
            CardType.Dodge,
            7
        );
        BattleActionSlot dodgeSlot = new BattleActionSlot(fixture.target, 1);
        dodgeSlot.AssignPassiveGuard(fixture.target, dodgeCard);
        dodgeSlot.ActivateContinuousDodge(
            ContinuousDodgeSource.PassiveGuard,
            7,
            fixture.enemy
        );
        fixture.item.passiveGuardCandidates.Add(dodgeSlot);
        BattleExecutionInteractionContext context =
            BattleExecutionInteractionContextFactory.BuildEffective(
                fixture.item,
                dodgeSlot
            );

        bool began = BattleExecutionPlanExecutor
            .TryBeginPausableUnrespondedEnemyIntent(
                fixture.item,
                null,
                out BattleActionSlot selectedSlot,
                out BattleClashSession session,
                out bool itemCompleted,
                out string failureMessage
            );

        return began && !itemCompleted && string.IsNullOrEmpty(failureMessage) &&
            object.ReferenceEquals(selectedSlot, dodgeSlot) && session != null &&
            context.effectiveInteractionType == BattleInteractionType.AttackVsDodge &&
            fixture.item.interactionType == BattleInteractionType.UnilateralAttack &&
            fixture.item.status == BattleExecutionItemStatus.Pending;
    }

    static bool VerifyNoInteractionHasNoCardLifecycleSideEffects()
    {
        RespondedFixture fixture = CreateRespondedFixture(
            "mode91_no_lifecycle",
            CardType.Defense,
            CardType.Dodge
        );
        fixture.player.AddBuff("Mode91Resource", 3, -1);
        fixture.playerCard.cardData.resourceRule = new CardResourceRuleData
        {
            resourceType = "BuffStack",
            resourceID = "Mode91Resource",
            requiredStackForNormalVersion = 1,
            consumeAmountOnSuccess = 1
        };

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(fixture.plan);

        return IsNoInteractionComplete(fixture.item) &&
            fixture.player.GetBuffStack("Mode91Resource") == 3 &&
            CardLifecycleIsUntouched(fixture.playerCard) &&
            CardLifecycleIsUntouched(fixture.enemyCard);
    }

    static bool VerifyHandledNoInteractionSlotCannotGuardAgain()
    {
        CharacterData actor = CreateCharacter("mode91_handled_actor");
        CharacterData enemy = CreateCharacter("mode91_handled_enemy");
        BattleCardState defenseCard = CreateCard(
            actor,
            "mode91_handled_defense",
            CardType.Defense,
            5
        );
        BattleActionSlot slot = new BattleActionSlot(actor, 1);
        slot.AssignFreeAction(actor, defenseCard, enemy);
        BattleExecutionItem freeItem = new BattleExecutionItem(
            0,
            BattleExecutionItemType.FreeAction,
            null,
            slot
        );
        freeItem.interactionType = BattleInteractionType.NoInteraction;
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(CreatePlan(freeItem));

        slot.slotType = BattleActionSlotType.PassiveGuard;
        slot.target = actor;
        BattleEnemyIntent laterIntent = CreateIntent(
            "mode91_handled_later_intent",
            enemy,
            CreateCard(enemy, "mode91_handled_attack", CardType.Attack, 4),
            actor
        );
        BattleGuardSelectionResult selection = BattleGuardSelectionManager
            .SelectHandlingCardForEnemyIntent(
                new List<BattleActionSlot> { slot },
                laterIntent
            );

        return IsNoInteractionComplete(freeItem) && slot.isUsed &&
            selection.slot == null &&
            selection.selectionType == BattleGuardSelectionType.None;
    }

    static bool IsNoInteractionComplete(BattleExecutionItem item)
    {
        return item != null && item.isCompleted &&
            item.status == BattleExecutionItemStatus.Skipped &&
            item.outcomeReason == BattleExecutionItemOutcomeReason.NoInteraction;
    }

    static bool CardLifecycleIsUntouched(BattleCardState card)
    {
        return card != null && card.currentCooldown == 0 &&
            card.currentUseCount == 0 && !card.isConsumed;
    }

    static int GetExpectedResolvedCooldown(BattleCardState card)
    {
        int baseCooldown = card != null
            ? BattleCardManager.GetBaseCooldown(card.cardData)
            : 0;
        return baseCooldown > 0 ? baseCooldown + 1 : 0;
    }

    static RespondedFixture CreateRespondedFixture(
        string id,
        string responseCardType,
        string enemyCardType
    )
    {
        CharacterData player = CreateCharacter(id + "_player");
        CharacterData enemy = CreateCharacter(id + "_enemy");
        BattleCardState playerCard = CreateCard(
            player,
            id + "_player_card",
            responseCardType,
            5
        );
        BattleCardState enemyCard = CreateCard(
            enemy,
            id + "_enemy_card",
            enemyCardType,
            5
        );
        BattleEnemyIntent intent = CreateIntent(id + "_intent", enemy, enemyCard, player);
        BattleActionSlot responseSlot = new BattleActionSlot(player, 1);
        responseSlot.AssignResponse(player, playerCard, intent, false);
        intent.MarkResponded();
        BattleExecutionItem item = new BattleExecutionItem(
            0,
            BattleExecutionItemType.RespondedEnemyIntent,
            intent,
            responseSlot
        );
        item.interactionType = BattleInteractionClassifier.Classify(
            playerCard,
            enemyCard
        );

        return new RespondedFixture
        {
            player = player,
            playerCard = playerCard,
            enemyCard = enemyCard,
            responseSlot = responseSlot,
            item = item,
            plan = CreatePlan(item)
        };
    }

    static UnrespondedFixture CreateUnrespondedFixture(
        string id,
        string enemyCardType,
        int point
    )
    {
        CharacterData target = CreateCharacter(id + "_target");
        CharacterData enemy = CreateCharacter(id + "_enemy");
        BattleCardState enemyCard = CreateCard(
            enemy,
            id + "_enemy_card",
            enemyCardType,
            point
        );
        BattleEnemyIntent intent = CreateIntent(id + "_intent", enemy, enemyCard, target);
        BattleExecutionItem item = new BattleExecutionItem(
            0,
            BattleExecutionItemType.UnrespondedEnemyIntent,
            intent,
            null
        );
        item.interactionType = BattleInteractionClassifier.Classify(enemyCard, null);

        return new UnrespondedFixture
        {
            target = target,
            enemy = enemy,
            enemyCard = enemyCard,
            item = item,
            plan = CreatePlan(item)
        };
    }

    static BattleExecutionPlan CreatePlan(BattleExecutionItem item)
    {
        BattleExecutionPlan plan = new BattleExecutionPlan();
        plan.AddItem(item);
        return plan;
    }

    static BattleEnemyIntent CreateIntent(
        string id,
        CharacterData enemy,
        BattleCardState enemyCard,
        CharacterData target
    )
    {
        return new BattleEnemyIntent(id, enemy, enemyCard, target, 1, 1);
    }

    static CharacterData CreateCharacter(string id)
    {
        return new CharacterData(id, 30, 5, 5);
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
            isClashable = cardType == CardType.Attack || cardType == CardType.Dodge,
            minPoint = point,
            maxPoint = point,
            cooldown = 2,
            damageFormula = "PointAsDamage",
            defenseFormula = cardType == CardType.Defense ? "PointAsDefense" : "",
            effects = new List<CardEffectData>()
        };
        return BattleCardManager.CreateBattleCard(owner, data, id + "_instance");
    }

    sealed class RespondedFixture
    {
        public CharacterData player;
        public BattleCardState playerCard;
        public BattleCardState enemyCard;
        public BattleActionSlot responseSlot;
        public BattleExecutionItem item;
        public BattleExecutionPlan plan;
    }

    sealed class UnrespondedFixture
    {
        public CharacterData target;
        public CharacterData enemy;
        public BattleCardState enemyCard;
        public BattleExecutionItem item;
        public BattleExecutionPlan plan;
    }
}
