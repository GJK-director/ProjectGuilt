// Phase 3.2：验证ClashSession单次RollAttempt推进与同步Resolver兼容性。
using System.Collections.Generic;
using UnityEngine;

public static class BattleClashSessionTests
{
    public static void Run()
    {
        Debug.Log("===== BattleClashSessionBasic 聚合测试开始 =====");

        bool singleAttempt = VerifyAttackSingleAttemptAdvance();
        bool attackWinner = VerifyAttackWinnerFinalizesOnce();
        bool attackTieLimit = VerifyAttackTieLimitAfterTenActiveRolls();
        bool dodgeEquality = VerifyDodgeEqualitySucceedsEverywhere();
        bool defenseEquality = VerifyDefenseEqualityFullBlocks();
        bool synchronousCompatibility = VerifySynchronousResolverCompatibility();
        bool initializationOnce = VerifyClashInitializationRunsOnce();
        bool continuousDodge = VerifyContinuousDodgeCompatibility();

        Debug.Log("模式80 A Attack平局每次只推进一个Attempt：" + singleAttempt);
        Debug.Log("模式80 B Attack正常胜负一次Finalize：" + attackWinner);
        Debug.Log("模式80 C Attack第10次平局进入TieLimit：" + attackTieLimit);
        Debug.Log("模式80 D Dodge相等在普通与known-point路径均成功：" + dodgeEquality);
        Debug.Log("模式80 E Defense相等为FullBlock且不重Roll：" + defenseEquality);
        Debug.Log("模式80 F 旧同步Resolver入口仍完整结算：" + synchronousCompatibility);
        Debug.Log("模式80 G 多次AttackAttempt不重复初始化与快照：" + initializationOnce);
        Debug.Log("模式80 H Continuous Dodge延迟结算语义保持：" + continuousDodge);
        Debug.Log(
            "模式80 聚合结果：" +
            (singleAttempt && attackWinner && attackTieLimit &&
             dodgeEquality && defenseEquality && synchronousCompatibility &&
             initializationOnce && continuousDodge)
        );
    }

    static bool VerifyAttackSingleAttemptAdvance()
    {
        CharacterData sideA = CreateCharacter("clash80_a_side_a");
        CharacterData sideB = CreateCharacter("clash80_a_side_b");
        BattleClashSession session = CreateAttackSession(sideA, sideB, 5, 5);

        bool firstRoll = session.RollNextAttempt();
        bool firstAttempt = firstRoll &&
            session.AttemptIndex == 1 &&
            session.AttackTieCount == 1 &&
            session.AttemptResult == BattleClashAttemptResult.AttackTie &&
            session.RequiresAnotherRoll &&
            !session.IsFinalized;

        bool secondRoll = session.RollNextAttempt();
        bool secondAttempt = secondRoll &&
            session.AttemptIndex == 2 &&
            session.AttackTieCount == 2 &&
            session.AttemptResult == BattleClashAttemptResult.AttackTie &&
            session.RequiresAnotherRoll &&
            !session.IsFinalized;

        return firstAttempt && secondAttempt;
    }

    static bool VerifyAttackWinnerFinalizesOnce()
    {
        CharacterData sideA = CreateCharacter("clash80_b_side_a");
        CharacterData sideB = CreateCharacter("clash80_b_side_b");
        BattleClashSession session = CreateAttackSession(sideA, sideB, 6, 4);

        bool rolled = session.RollNextAttempt();
        bool rejectedExtraRoll = !session.RollNextAttempt();

        return rolled && rejectedExtraRoll &&
            session.AttemptIndex == 1 && session.AttackTieCount == 0 &&
            session.SideAPoint == 6 && session.SideBPoint == 4 &&
            session.AttemptResult == BattleClashAttemptResult.SideAWin &&
            session.FinalResult == BattleClashFinalResult.SideAWin &&
            !session.RequiresAnotherRoll && session.IsFinalized;
    }

    static bool VerifyAttackTieLimitAfterTenActiveRolls()
    {
        CharacterData sideA = CreateCharacter("clash80_c_side_a");
        CharacterData sideB = CreateCharacter("clash80_c_side_b");
        BattleClashSession session = CreateAttackSession(sideA, sideB, 5, 5);

        bool everyRollAccepted = true;
        for (int index = 0; index < BattleClashSession.MaxAttackTieCount; index++)
        {
            everyRollAccepted &= session.RollNextAttempt();
        }

        return everyRollAccepted &&
            session.AttemptIndex == 10 && session.AttackTieCount == 10 &&
            session.AttemptResult == BattleClashAttemptResult.TieLimit &&
            session.FinalResult == BattleClashFinalResult.TieLimit &&
            !session.RequiresAnotherRoll && session.IsFinalized &&
            !session.RollNextAttempt();
    }

    static bool VerifyDodgeEqualitySucceedsEverywhere()
    {
        CharacterData directDodgeUser = CreateCharacter("clash80_d_direct_user");
        CharacterData directEnemy = CreateCharacter("clash80_d_direct_enemy");
        BattleClashSession directSession = BattleClashSession.CreateDodgeVsAttack(
            CreateSideState(directDodgeUser,
                CreateCard(directDodgeUser, "clash80_d_direct_dodge", CardType.Dodge, 5)),
            CreateSideState(directEnemy,
                CreateCard(directEnemy, "clash80_d_direct_attack", CardType.Attack, 5)),
            directDodgeUser
        );
        directSession.RollNextAttempt();
        bool directEquality = directSession.AttemptIndex == 1 &&
            directSession.AttackTieCount == 0 && directSession.IsFinalized &&
            directSession.AttemptResult == BattleClashAttemptResult.DodgeSuccess &&
            directSession.FinalResult == BattleClashFinalResult.DodgeSuccess;

        CharacterData respondedUser = CreateCharacter("clash80_d_responded_user");
        CharacterData respondedEnemy = CreateCharacter("clash80_d_responded_enemy");
        BattleCardState respondedDodge = CreateCard(
            respondedUser,
            "clash80_d_responded_dodge",
            CardType.Dodge,
            5
        );
        BattleCardState respondedAttack = CreateCard(
            respondedEnemy,
            "clash80_d_responded_attack",
            CardType.Attack,
            5
        );
        BattleEnemyIntent respondedIntent = CreateIntent(
            "clash80_d_responded_intent",
            respondedEnemy,
            respondedAttack,
            respondedUser
        );
        BattleResolveResult respondedResult = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(respondedUser, respondedDodge, respondedIntent),
            respondedIntent
        );
        bool respondedEquality = IsDodgeEqualitySuccess(respondedResult);

        CharacterData knownUser = CreateCharacter("clash80_d_known_user");
        CharacterData knownEnemy = CreateCharacter("clash80_d_known_enemy");
        BattleCardState knownDodge = CreateCard(
            knownUser,
            "clash80_d_known_dodge",
            CardType.Dodge,
            5
        );
        BattleCardState knownAttack = CreateCard(
            knownEnemy,
            "clash80_d_known_attack",
            CardType.Attack,
            9
        );
        BattleEnemyIntent knownIntent = CreateIntent(
            "clash80_d_known_intent",
            knownEnemy,
            knownAttack,
            knownUser
        );
        BattleResolveResult knownResult = BattleResolver.ResolveDodgeVsAttackWithKnownEnemyPoint(
            CreateRespondedSlot(knownUser, knownDodge, knownIntent),
            knownIntent,
            5
        );
        bool knownEquality = IsDodgeEqualitySuccess(knownResult);

        return directEquality && respondedEquality && knownEquality;
    }

    static bool VerifyDefenseEqualityFullBlocks()
    {
        CharacterData defenseUser = CreateCharacter("clash80_e_defense_user");
        CharacterData enemy = CreateCharacter("clash80_e_enemy");
        BattleCardState defense = CreateCard(
            defenseUser,
            "clash80_e_defense",
            CardType.Defense,
            5
        );
        BattleCardState attack = CreateCard(
            enemy,
            "clash80_e_attack",
            CardType.Attack,
            5
        );
        BattleClashSession session = BattleClashSession.CreateDefenseVsAttack(
            CreateSideState(defenseUser, defense),
            CreateSideState(enemy, attack),
            defenseUser
        );
        bool rolled = session.RollNextAttempt();

        BattleEnemyIntent intent = CreateIntent(
            "clash80_e_intent",
            enemy,
            attack,
            defenseUser
        );
        BattleResolveResult resolveResult = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(defenseUser, defense, intent),
            intent
        );

        return rolled && session.AttemptIndex == 1 &&
            session.AttackTieCount == 0 && session.SideAPoint == 5 &&
            session.SideBPoint == 5 && session.RemainingAttackPoint == 0 &&
            session.IsFullBlock && session.IsFinalized &&
            session.FinalResult == BattleClashFinalResult.DefenseFullBlock &&
            resolveResult != null &&
            resolveResult.resultType == "DefenseFullBlock" &&
            resolveResult.playerPoint == 5 && resolveResult.enemyPoint == 5 &&
            resolveResult.clashAttemptCount == 0;
    }

    static bool VerifySynchronousResolverCompatibility()
    {
        CharacterData attackUser = CreateCharacter("clash80_f_attack_user");
        CharacterData attackEnemy = CreateCharacter("clash80_f_attack_enemy");
        BattleCardState playerAttack = CreateCard(
            attackUser,
            "clash80_f_player_attack",
            CardType.Attack,
            6
        );
        BattleCardState enemyAttack = CreateCard(
            attackEnemy,
            "clash80_f_enemy_attack",
            CardType.Attack,
            4
        );
        BattleEnemyIntent attackIntent = CreateIntent(
            "clash80_f_attack_intent",
            attackEnemy,
            enemyAttack,
            attackUser
        );
        BattleResolveResult attackResult = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(attackUser, playerAttack, attackIntent),
            attackIntent
        );
        bool attackWorked = attackResult != null &&
            attackResult.resultType == "PlayerWin" &&
            attackResult.playerPoint == 6 && attackResult.enemyPoint == 4 &&
            attackResult.clashAttemptCount == 1;

        CharacterData defenseUser = CreateCharacter("clash80_f_defense_user");
        CharacterData defenseEnemy = CreateCharacter("clash80_f_defense_enemy");
        BattleCardState defense = CreateCard(
            defenseUser,
            "clash80_f_defense",
            CardType.Defense,
            3
        );
        BattleCardState defenseAttack = CreateCard(
            defenseEnemy,
            "clash80_f_defense_attack",
            CardType.Attack,
            5
        );
        BattleEnemyIntent defenseIntent = CreateIntent(
            "clash80_f_defense_intent",
            defenseEnemy,
            defenseAttack,
            defenseUser
        );
        BattleResolveResult defenseResult = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(defenseUser, defense, defenseIntent),
            defenseIntent
        );
        bool defenseWorked = defenseResult != null &&
            defenseResult.resultType == "DefenseReducedDamage" &&
            defenseResult.playerPoint == 3 && defenseResult.enemyPoint == 5 &&
            defenseResult.damage == 2 && defenseResult.clashAttemptCount == 0;

        CharacterData dodgeUser = CreateCharacter("clash80_f_dodge_user");
        CharacterData dodgeEnemy = CreateCharacter("clash80_f_dodge_enemy");
        BattleCardState dodge = CreateCard(
            dodgeUser,
            "clash80_f_dodge",
            CardType.Dodge,
            4
        );
        BattleCardState dodgeAttack = CreateCard(
            dodgeEnemy,
            "clash80_f_dodge_attack",
            CardType.Attack,
            5
        );
        BattleEnemyIntent dodgeIntent = CreateIntent(
            "clash80_f_dodge_intent",
            dodgeEnemy,
            dodgeAttack,
            dodgeUser
        );
        BattleResolveResult dodgeResult = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(dodgeUser, dodge, dodgeIntent),
            dodgeIntent
        );
        bool dodgeWorked = dodgeResult != null &&
            dodgeResult.resultType == "DodgeFailed" &&
            dodgeResult.playerPoint == 4 && dodgeResult.enemyPoint == 5 &&
            dodgeResult.damage == 5 && dodgeResult.clashAttemptCount == 1;

        return attackWorked && defenseWorked && dodgeWorked;
    }

    static bool VerifyClashInitializationRunsOnce()
    {
        CharacterData player = CreateCharacter("clash80_g_player");
        CharacterData enemy = CreateCharacter("clash80_g_enemy");
        BattleCardState playerAttack = CreateCard(
            player,
            "clash80_g_player_attack",
            CardType.Attack,
            5
        );
        BattleCardState enemyAttack = CreateCard(
            enemy,
            "clash80_g_enemy_attack",
            CardType.Attack,
            5
        );
        AddInitializationProbeEffects(playerAttack);
        AddInitializationProbeEffects(enemyAttack);
        player.AddBuff("NextClashPointUp", 1, 1);
        enemy.AddBuff("NextClashPointUp", 1, 1);
        player.AddBuff(
            "Clash80GPlayerProbe",
            "Clash80GPlayerProbe",
            "AbilityBuff",
            1,
            2,
            BattleTiming.ClashStart,
            "DurationDown"
        );
        enemy.AddBuff(
            "Clash80GEnemyProbe",
            "Clash80GEnemyProbe",
            "AbilityBuff",
            1,
            2,
            BattleTiming.ClashStart,
            "DurationDown"
        );

        BattleEnemyIntent intent = CreateIntent(
            "clash80_g_intent",
            enemy,
            enemyAttack,
            player
        );
        BattleClashSession session =
            BattleResolver.CreateRespondedAttackClashSession(
            CreateRespondedSlot(player, playerAttack, intent),
            intent
        );
        BattleClashPointSnapshot playerPointSnapshot =
            session.SideA.pointSnapshot;
        BattleClashPointSnapshot enemyPointSnapshot =
            session.SideB.pointSnapshot;
        BattleClashResourceSnapshot playerResourceSnapshot =
            session.SideA.resourceSnapshot;
        BattleClashResourceSnapshot enemyResourceSnapshot =
            session.SideB.resourceSnapshot;
        bool initializationState =
            player.GetBuffStack("Bullet") == 2 &&
            enemy.GetBuffStack("Bullet") == 2 &&
            GetBuffDuration(player, "Clash80GPlayerProbe") == 1 &&
            GetBuffDuration(enemy, "Clash80GEnemyProbe") == 1 &&
            playerPointSnapshot.nextClashPointStack == 1 &&
            enemyPointSnapshot.nextClashPointStack == 1;

        bool firstRoll = session.RollNextAttempt();
        bool secondRoll = session.RollNextAttempt();

        return initializationState && firstRoll && secondRoll &&
            session.AttemptIndex == 2 && session.AttackTieCount == 2 &&
            !session.IsFinalized && session.RequiresAnotherRoll &&
            player.GetBuffStack("Bullet") == 2 &&
            enemy.GetBuffStack("Bullet") == 2 &&
            player.GetBuffStack("NextClashPointUp") == 1 &&
            enemy.GetBuffStack("NextClashPointUp") == 1 &&
            GetBuffDuration(player, "Clash80GPlayerProbe") == 1 &&
            GetBuffDuration(enemy, "Clash80GEnemyProbe") == 1 &&
            object.ReferenceEquals(
                session.SideA.pointSnapshot,
                playerPointSnapshot
            ) &&
            object.ReferenceEquals(
                session.SideB.pointSnapshot,
                enemyPointSnapshot
            ) &&
            object.ReferenceEquals(
                session.SideA.resourceSnapshot,
                playerResourceSnapshot
            ) &&
            object.ReferenceEquals(
                session.SideB.resourceSnapshot,
                enemyResourceSnapshot
            );
    }

    static bool VerifyContinuousDodgeCompatibility()
    {
        CharacterData dodgeUser = CreateCharacter("clash80_h_dodge_user");
        CharacterData enemy = CreateCharacter("clash80_h_enemy");
        BattleCardState dodge = CreateCard(
            dodgeUser,
            "clash80_h_dodge",
            CardType.Dodge,
            5
        );
        BattleCardState attack = CreateCard(
            enemy,
            "clash80_h_attack",
            CardType.Attack,
            5
        );
        BattleEnemyIntent intent = CreateIntent(
            "clash80_h_intent",
            enemy,
            attack,
            dodgeUser
        );
        BattleActionSlot slot = CreateRespondedSlot(dodgeUser, dodge, intent);
        int hpBefore = dodgeUser.currentHP;

        BattleResolveResult result = BattleResolver.ResolveContinuousDodgeVsAttack(
            slot,
            intent
        );

        return result != null && result.resultType == "DodgeSuccess" &&
            result.clashAttemptCount == 1 && !result.isTieLimitReached &&
            result.playerCardParticipated && !result.playerCardUsed &&
            result.enemyCardUsed &&
            result.playerCardUseDisposition ==
                BattleCardUseDisposition.DeferForContinuousDodge &&
            !result.hasDamage && result.damage == 0 &&
            dodgeUser.currentHP == hpBefore;
    }

    static BattleClashSession CreateAttackSession(
        CharacterData sideA,
        CharacterData sideB,
        int sideAPoint,
        int sideBPoint
    )
    {
        BattleCardState sideACard = CreateCard(
            sideA,
            sideA.characterName + "_attack",
            CardType.Attack,
            sideAPoint
        );
        BattleCardState sideBCard = CreateCard(
            sideB,
            sideB.characterName + "_attack",
            CardType.Attack,
            sideBPoint
        );
        return BattleClashSession.CreateAttackVsAttack(
            CreateSideState(sideA, sideACard),
            CreateSideState(sideB, sideBCard),
            sideA
        );
    }

    static BattleClashSideState CreateSideState(
        CharacterData actor,
        BattleCardState cardState
    )
    {
        return new BattleClashSideState(
            actor,
            cardState,
            new BattleClashPointSnapshot(),
            new BattleClashResourceSnapshot
            {
                cardState = cardState,
                selectedMinPoint = cardState.cardData.minPoint,
                selectedMaxPoint = cardState.cardData.maxPoint
            }
        );
    }

    static CharacterData CreateCharacter(string name)
    {
        return new CharacterData(name, 30, 5, 5);
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

    static BattleEnemyIntent CreateIntent(
        string id,
        CharacterData enemy,
        BattleCardState enemyAttack,
        CharacterData target
    )
    {
        return new BattleEnemyIntent(
            id,
            enemy,
            enemyAttack,
            target,
            1,
            1
        );
    }

    static BattleActionSlot CreateRespondedSlot(
        CharacterData actor,
        BattleCardState cardState,
        BattleEnemyIntent intent
    )
    {
        BattleActionSlot slot = new BattleActionSlot(actor, 1);
        slot.AssignResponse(actor, cardState, intent, false);
        return slot;
    }

    static bool IsDodgeEqualitySuccess(BattleResolveResult result)
    {
        return result != null && result.resultType == "DodgeSuccess" &&
            result.playerPoint == 5 && result.enemyPoint == 5 &&
            result.clashAttemptCount == 1 && !result.isTieLimitReached &&
            result.isSuccess && result.shouldCompleteItem && !result.hasDamage;
    }

    static void AddInitializationProbeEffects(BattleCardState cardState)
    {
        cardState.cardData.effects.Add(CreateProbeEffect(BattleTiming.ActionStart));
        cardState.cardData.effects.Add(CreateProbeEffect(BattleTiming.BeforeUse));
    }

    static CardEffectData CreateProbeEffect(string timing)
    {
        return new CardEffectData
        {
            trigger = timing,
            effectType = CardEffectType.ApplyBuff,
            target = CardTargetType.Self,
            buffType = "Bullet",
            stack = 1,
            duration = -1,
            applyTiming = "Immediate"
        };
    }

    static int GetBuffDuration(CharacterData character, string buffID)
    {
        if (character == null || character.buffs == null)
        {
            return int.MinValue;
        }

        foreach (BuffData buff in character.buffs)
        {
            if (buff != null && buff.buffID == buffID)
            {
                return buff.duration;
            }
        }

        return int.MinValue;
    }
}
