using System.Collections.Generic;
using UnityEngine;

public static class BattleLifecyclePhaseContractTests
{
    public static bool Run()
    {
        bool[] results = new bool[15];

        BattleRuntimeState initPrepare = new BattleRuntimeState();
        results[0] = Transition(initPrepare, BattleLifecyclePhase.Prepare) &&
            initPrepare.LifecyclePhase == BattleLifecyclePhase.Prepare;

        BattleRuntimeState planReady = new BattleRuntimeState();
        results[1] = Transition(planReady, BattleLifecyclePhase.Prepare) &&
            Transition(planReady, BattleLifecyclePhase.PlanReady) &&
            Transition(planReady, BattleLifecyclePhase.Executing);

        BattleRuntimeState directExecuting = new BattleRuntimeState();
        results[2] = Transition(directExecuting, BattleLifecyclePhase.Prepare) &&
            Transition(directExecuting, BattleLifecyclePhase.Executing);
        results[3] = Transition(
            directExecuting,
            BattleLifecyclePhase.TurnResolved
        );
        results[4] = Transition(
                directExecuting,
                BattleLifecyclePhase.TurnEnding
            ) && Transition(
                directExecuting,
                BattleLifecyclePhase.TurnEnded
            );
        results[5] = Transition(
                directExecuting,
                BattleLifecyclePhase.PreparingNextTurn
            ) && Transition(
                directExecuting,
                BattleLifecyclePhase.Prepare
            );

        results[6] = VerifyAllLegalBattleEndedTransitions();

        BattleRuntimeState ended = CreateAtPhase(BattleLifecyclePhase.Executing);
        Transition(ended, BattleLifecyclePhase.BattleEnded);
        BattleLifecyclePhase endedBefore = ended.LifecyclePhase;
        string endedFailure;
        results[7] = !ended.TryTransitionTo(
                BattleLifecyclePhase.Prepare,
                out endedFailure
            ) && ended.LifecyclePhase == endedBefore &&
            !string.IsNullOrEmpty(endedFailure);

        BattleRuntimeState illegal = CreateAtPhase(BattleLifecyclePhase.Prepare);
        BattleLifecyclePhase illegalBefore = illegal.LifecyclePhase;
        string illegalFailure;
        results[8] = !illegal.TryTransitionTo(
                BattleLifecyclePhase.TurnEnded,
                out illegalFailure
            ) && illegal.LifecyclePhase == illegalBefore &&
            illegalFailure.Contains("Prepare -> TurnEnded");
        string samePhaseFailure;
        results[9] = illegal.LifecyclePhase == illegalBefore &&
            !illegal.TryTransitionTo(
                BattleLifecyclePhase.Prepare,
                out samePhaseFailure
            ) && illegal.LifecyclePhase == illegalBefore &&
            samePhaseFailure.Contains("Prepare -> Prepare");

        results[10] = VerifyCompatiblePhaseText();

        BattleRuntimeState clearRuntime = CreateAtPhase(
            BattleLifecyclePhase.PlanReady
        );
        clearRuntime.SetActionSlots(new List<BattleActionSlot>
        {
            new BattleActionSlot(null, 1)
        });
        clearRuntime.SetIntentQueue(new List<BattleEnemyIntent>());
        clearRuntime.SetExecutionPlan(new BattleExecutionPlan());
        BattleLifecyclePhase clearBefore = clearRuntime.LifecyclePhase;
        clearRuntime.ClearCurrentTurnRuntimeObjects();
        results[11] = clearRuntime.LifecyclePhase == clearBefore &&
            clearRuntime.actionSlots.Count == 0 &&
            clearRuntime.intentQueue.Count == 0 &&
            clearRuntime.currentExecutionPlan == null;

        results[12] = VerifyAutomaticFullTurnCycle();
        results[13] = VerifyVictoryAndDefeatTransitions();
        results[14] = VerifySinglePlayerDeathTransitionsToDefeat();

        string[] names =
        {
            "Init进入Prepare",
            "Prepare经PlanReady进入Executing",
            "Prepare直接进入Executing",
            "Executing进入TurnResolved",
            "TurnResolved经TurnEnding进入TurnEnded",
            "TurnEnded经PreparingNextTurn回到Prepare",
            "所有允许的非终局阶段均可进入BattleEnded",
            "BattleEnded不能返回Prepare",
            "Prepare不能直接进入TurnEnded",
            "非法转换与同阶段重复转换均不改变原阶段",
            "currentPhase兼容文本映射正确",
            "ClearCurrentTurnRuntimeObjects不修改阶段",
            "自动完整回合进入下一回合Prepare",
            "Victory与Defeat均进入BattleEnded",
            "单人战斗中玩家死亡进入Defeat与BattleEnded"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式76 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }
        Debug.Log("模式76 15项聚合结果：" + allPassed);
        return allPassed;
    }

    public static bool TryReachPhaseForTest(
        BattleRuntimeState runtimeState,
        BattleLifecyclePhase targetPhase
    )
    {
        if (runtimeState == null)
        {
            return false;
        }

        for (int step = 0; step < 12; step++)
        {
            BattleLifecyclePhase current = runtimeState.LifecyclePhase;
            if (current == targetPhase)
            {
                return true;
            }
            if (current == BattleLifecyclePhase.BattleEnded)
            {
                return false;
            }

            BattleLifecyclePhase next;
            switch (current)
            {
                case BattleLifecyclePhase.Init:
                    next = BattleLifecyclePhase.Prepare;
                    break;
                case BattleLifecyclePhase.Prepare:
                    next = targetPhase == BattleLifecyclePhase.PlanReady
                        ? BattleLifecyclePhase.PlanReady
                        : BattleLifecyclePhase.Executing;
                    break;
                case BattleLifecyclePhase.PlanReady:
                    next = BattleLifecyclePhase.Executing;
                    break;
                case BattleLifecyclePhase.Executing:
                    next = targetPhase == BattleLifecyclePhase.BattleEnded
                        ? BattleLifecyclePhase.BattleEnded
                        : BattleLifecyclePhase.TurnResolved;
                    break;
                case BattleLifecyclePhase.TurnResolved:
                    next = targetPhase == BattleLifecyclePhase.BattleEnded
                        ? BattleLifecyclePhase.BattleEnded
                        : BattleLifecyclePhase.TurnEnding;
                    break;
                case BattleLifecyclePhase.TurnEnding:
                    next = targetPhase == BattleLifecyclePhase.BattleEnded
                        ? BattleLifecyclePhase.BattleEnded
                        : BattleLifecyclePhase.TurnEnded;
                    break;
                case BattleLifecyclePhase.TurnEnded:
                    next = targetPhase == BattleLifecyclePhase.BattleEnded
                        ? BattleLifecyclePhase.BattleEnded
                        : BattleLifecyclePhase.PreparingNextTurn;
                    break;
                case BattleLifecyclePhase.PreparingNextTurn:
                    next = targetPhase == BattleLifecyclePhase.BattleEnded
                        ? BattleLifecyclePhase.BattleEnded
                        : BattleLifecyclePhase.Prepare;
                    break;
                default:
                    return false;
            }

            if (!Transition(runtimeState, next))
            {
                return false;
            }
        }
        return false;
    }

    private static bool VerifyAllLegalBattleEndedTransitions()
    {
        BattleLifecyclePhase[] sources =
        {
            BattleLifecyclePhase.Executing,
            BattleLifecyclePhase.TurnResolved,
            BattleLifecyclePhase.TurnEnding,
            BattleLifecyclePhase.TurnEnded,
            BattleLifecyclePhase.PreparingNextTurn
        };
        for (int index = 0; index < sources.Length; index++)
        {
            BattleRuntimeState runtimeState = CreateAtPhase(sources[index]);
            if (runtimeState.LifecyclePhase != sources[index] ||
                !Transition(runtimeState, BattleLifecyclePhase.BattleEnded))
            {
                return false;
            }
        }
        return true;
    }

    private static bool VerifyCompatiblePhaseText()
    {
        BattleLifecyclePhase[] phases =
        {
            BattleLifecyclePhase.Init,
            BattleLifecyclePhase.Prepare,
            BattleLifecyclePhase.PlanReady,
            BattleLifecyclePhase.Executing,
            BattleLifecyclePhase.TurnResolved,
            BattleLifecyclePhase.TurnEnding,
            BattleLifecyclePhase.TurnEnded,
            BattleLifecyclePhase.PreparingNextTurn,
            BattleLifecyclePhase.BattleEnded
        };
        string[] expected =
        {
            "Init",
            "Prepare",
            "PlanReady",
            "BattleStart",
            "Completed",
            "TurnEnding",
            "TurnEnded",
            "PreparingNextTurn",
            "BattleEnded"
        };
        for (int index = 0; index < phases.Length; index++)
        {
            BattleRuntimeState runtimeState = CreateAtPhase(phases[index]);
            if (runtimeState.LifecyclePhase != phases[index] ||
                runtimeState.currentPhase != expected[index])
            {
                return false;
            }
        }
        return true;
    }

    private static bool VerifyAutomaticFullTurnCycle()
    {
        CharacterData allyA = new CharacterData("lifecycle76_A", 30, 10, 10);
        CharacterData allyB = new CharacterData("lifecycle76_B", 30, 8, 8);
        CharacterData enemy = new CharacterData("lifecycle76_Enemy", 50, 5, 5);
        CardTestData playerCardData = CreateAttackData("lifecycle76_player", 1);
        CardTestData enemyCardData = CreateAttackData("enemy_atk_001", 1);
        BattleCardState playerCard = BattleCardManager.CreateBattleCard(
            allyA,
            playerCardData,
            "lifecycle76_player_instance"
        );
        BattleCardState enemyCard = BattleCardManager.CreateBattleCard(
            enemy,
            enemyCardData,
            "lifecycle76_enemy_instance"
        );

        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(allyA, allyB, enemy);
        List<BattleActionSlot> slots =
            BattleActionSlotManager.CreateLivingPartyActionSlots(allyA, allyB, 2);
        BattleActionSlotManager.AssignFreeAction(
            slots,
            allyA,
            1,
            allyA,
            playerCard,
            enemy
        );
        runtimeState.SetActionSlots(slots);
        runtimeState.SetIntentQueue(BattleEnemyIntentManager.CreateIntentQueue(
            new BattleEnemyIntent(
                "lifecycle76_intent",
                enemy,
                enemyCard,
                allyB,
                1,
                1
            )
        ));
        bool initialIntentValid = runtimeState.intentQueue != null &&
            runtimeState.intentQueue.Count == 1;
        if (!TryReachPhaseForTest(runtimeState, BattleLifecyclePhase.Prepare))
        {
            return false;
        }

        BattleAutomaticTurnCycleResult result = BattleAutomaticTurnCycle.TryRun(
            runtimeState,
            allyA,
            allyB,
            enemy,
            enemyCard
        );
        BattleEnemyIntent nextIntent = runtimeState.intentQueue != null &&
            runtimeState.intentQueue.Count == 1
            ? runtimeState.intentQueue[0]
            : null;
        CharacterData nextTarget = nextIntent != null
            ? nextIntent.originalTargetCharacter
            : null;
        bool nextTargetIsLivingAlly = nextTarget != null &&
            !nextTarget.IsDead() &&
            (object.ReferenceEquals(nextTarget, allyA) ||
             object.ReferenceEquals(nextTarget, allyB));
        bool nextTargetSlotIsValid = nextIntent != null &&
            nextTarget != null &&
            BattleActionSlotManager.GetSlot(
                runtimeState.actionSlots,
                nextTarget,
                nextIntent.originalTargetSlotIndex
            ) != null;

        return initialIntentValid && result != null && result.isSuccess &&
            result.advancedToNextTurn && runtimeState.currentTurn == 2 &&
            runtimeState.LifecyclePhase == BattleLifecyclePhase.Prepare &&
            runtimeState.actionSlots.Count == 4 &&
            runtimeState.intentQueue != null &&
            runtimeState.intentQueue.Count == 1 &&
            nextIntent != null &&
            object.ReferenceEquals(nextIntent.enemy, enemy) &&
            nextIntent.enemyCardState != null &&
            nextIntent.enemyCardState.cardData != null &&
            nextIntent.enemyCardState.cardData.cardID == "enemy_atk_001" &&
            nextTargetIsLivingAlly && nextTargetSlotIsValid &&
            runtimeState.currentExecutionPlan == null;
    }

    private static bool VerifyVictoryAndDefeatTransitions()
    {
        BattleRuntimeState victory = CreateBattleEndRuntime(false);
        BattleRuntimeState defeat = CreateBattleEndRuntime(true);
        return new BattleLifecycleController(victory).EvaluateBattleEnd() ==
                BattleResult.Victory &&
            victory.LifecyclePhase == BattleLifecyclePhase.BattleEnded &&
            new BattleLifecycleController(defeat).EvaluateBattleEnd() ==
                BattleResult.Defeat &&
            defeat.LifecyclePhase == BattleLifecyclePhase.BattleEnded;
    }

    private static bool VerifySinglePlayerDeathTransitionsToDefeat()
    {
        CharacterData player = new CharacterData(
            "lifecycle76_single_player",
            30,
            1,
            1
        );
        CharacterData enemy = new CharacterData(
            "lifecycle76_single_enemy",
            30,
            1,
            1
        );
        player.currentHP = 0;

        BattleRuntimeState runtimeState = CreateAtPhase(
            BattleLifecyclePhase.Executing
        );
        runtimeState.SetCharacters(player, null, enemy);

        BattleResult result =
            new BattleLifecycleController(runtimeState).EvaluateBattleEnd();
        return result == BattleResult.Defeat &&
            runtimeState.battleResult == BattleResult.Defeat &&
            runtimeState.LifecyclePhase == BattleLifecyclePhase.BattleEnded;
    }

    private static BattleRuntimeState CreateBattleEndRuntime(bool defeat)
    {
        CharacterData allyA = new CharacterData("lifecycle76_end_A", 30, 1, 1);
        CharacterData allyB = new CharacterData("lifecycle76_end_B", 30, 1, 1);
        CharacterData enemy = new CharacterData("lifecycle76_end_Enemy", 30, 1, 1);
        if (defeat)
        {
            allyA.currentHP = 0;
            allyB.currentHP = 0;
        }
        else
        {
            enemy.currentHP = 0;
        }

        BattleRuntimeState runtimeState = CreateAtPhase(
            BattleLifecyclePhase.Executing
        );
        runtimeState.SetCharacters(allyA, allyB, enemy);
        return runtimeState;
    }

    private static CardTestData CreateAttackData(string id, int point)
    {
        return new CardTestData
        {
            cardID = id,
            cardName = id,
            cardType = CardType.Attack,
            isClashable = true,
            minPoint = point,
            maxPoint = point,
            cooldown = 0,
            damageFormula = "PointAsDamage"
        };
    }

    private static BattleRuntimeState CreateAtPhase(
        BattleLifecyclePhase phase
    )
    {
        BattleRuntimeState runtimeState = new BattleRuntimeState();
        TryReachPhaseForTest(runtimeState, phase);
        return runtimeState;
    }

    private static bool Transition(
        BattleRuntimeState runtimeState,
        BattleLifecyclePhase nextPhase
    )
    {
        string failureMessage;
        return runtimeState.TryTransitionTo(nextPhase, out failureMessage);
    }
}
