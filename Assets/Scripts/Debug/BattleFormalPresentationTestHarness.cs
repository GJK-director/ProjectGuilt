using System;
using System.Collections.Generic;
using UnityEngine;

public enum BattleFormalPresentationTestScenario
{
    None,
    AttackTie,
    AttackVsGuardFullBlock,
    AttackVsGuardReducedDamage,
    AttackVsDodgeContinuousSuccessSuccess,
    AttackVsDodgeContinuousSuccessFailed
}

// 正式BattleScene的开发测试输入入口；只准备规则数据，不驱动执行或表现。
public sealed class BattleFormalPresentationTestHarness : MonoBehaviour
{
    private const string AllyCardID = "[TEST]_FORMAL_TIE_ALLY_ATTACK";
    private const string EnemyCardID = "[TEST]_FORMAL_TIE_ENEMY_ATTACK";
    private const string IntentID = "[TEST]_FORMAL_TIE_ENEMY_INTENT";
    private const string FullDefenseCardID =
        "[TEST]_FORMAL_GUARD_FULL_DEFENSE";
    private const string FullAttackCardID =
        "[TEST]_FORMAL_GUARD_FULL_ATTACK";
    private const string FullIntentID =
        "[TEST]_FORMAL_GUARD_FULL_INTENT";
    private const string ReducedDefenseCardID =
        "[TEST]_FORMAL_GUARD_REDUCED_DEFENSE";
    private const string ReducedAttackCardID =
        "[TEST]_FORMAL_GUARD_REDUCED_ATTACK";
    private const string ReducedIntentID =
        "[TEST]_FORMAL_GUARD_REDUCED_INTENT";
    private const string ContinuousSuccessSuccessDodgeCardID =
        "[TEST]_FORMAL_CONTINUOUS_DODGE_SUCCESS_SUCCESS";
    private const string ContinuousSuccessSuccessAttackOneCardID =
        "[TEST]_FORMAL_CONTINUOUS_DODGE_SUCCESS_SUCCESS_ATTACK_1";
    private const string ContinuousSuccessSuccessAttackTwoCardID =
        "[TEST]_FORMAL_CONTINUOUS_DODGE_SUCCESS_SUCCESS_ATTACK_2";
    private const string ContinuousSuccessSuccessIntentOneID =
        "[TEST]_FORMAL_CONTINUOUS_DODGE_SUCCESS_SUCCESS_INTENT_1";
    private const string ContinuousSuccessSuccessIntentTwoID =
        "[TEST]_FORMAL_CONTINUOUS_DODGE_SUCCESS_SUCCESS_INTENT_2";
    private const string ContinuousSuccessFailedDodgeCardID =
        "[TEST]_FORMAL_CONTINUOUS_DODGE_SUCCESS_FAILED";
    private const string ContinuousSuccessFailedAttackOneCardID =
        "[TEST]_FORMAL_CONTINUOUS_DODGE_SUCCESS_FAILED_ATTACK_1";
    private const string ContinuousSuccessFailedAttackTwoCardID =
        "[TEST]_FORMAL_CONTINUOUS_DODGE_SUCCESS_FAILED_ATTACK_2";
    private const string ContinuousSuccessFailedIntentOneID =
        "[TEST]_FORMAL_CONTINUOUS_DODGE_SUCCESS_FAILED_INTENT_1";
    private const string ContinuousSuccessFailedIntentTwoID =
        "[TEST]_FORMAL_CONTINUOUS_DODGE_SUCCESS_FAILED_INTENT_2";
    private const int TestSlotIndex = 1;

    [SerializeField]
    private BattleFormalPresentationTestScenario scenario =
        BattleFormalPresentationTestScenario.None;

    private bool hasPreparedScenario;
    private BattleRuntimeState preparedRuntimeState;
    private bool hasLoggedReleaseSkip;

    public BattleFormalPresentationTestScenario Scenario => scenario;

    // 只有Harness已成功完成同一个RuntimeState的全部注入与校验，UI才可读取预安排槽位。
    public bool HasPreparedScenarioFor(BattleRuntimeState runtimeState)
    {
        return hasPreparedScenario &&
            ReferenceEquals(preparedRuntimeState, runtimeState);
    }

    public bool TryPrepareScenario(
        BattleRuntimeState runtimeState,
        out string failureMessage
    )
    {
        failureMessage = string.Empty;

        if (scenario == BattleFormalPresentationTestScenario.None)
        {
            return true;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (hasPreparedScenario)
        {
            if (ReferenceEquals(preparedRuntimeState, runtimeState))
            {
                return true;
            }

            return Fail(
                "测试场景已经为另一个BattleRuntimeState完成准备，拒绝重复注入。",
                out failureMessage
            );
        }

        switch (scenario)
        {
            case BattleFormalPresentationTestScenario.AttackTie:
                return TryPrepareAttackTie(runtimeState, out failureMessage);
            case BattleFormalPresentationTestScenario.AttackVsGuardFullBlock:
                return TryPrepareAttackVsGuard(
                    runtimeState,
                    true,
                    out failureMessage
                );
            case BattleFormalPresentationTestScenario.AttackVsGuardReducedDamage:
                return TryPrepareAttackVsGuard(
                    runtimeState,
                    false,
                    out failureMessage
                );
            case BattleFormalPresentationTestScenario.AttackVsDodgeContinuousSuccessSuccess:
                return TryPrepareContinuousDodge(
                    runtimeState,
                    true,
                    out failureMessage
                );
            case BattleFormalPresentationTestScenario.AttackVsDodgeContinuousSuccessFailed:
                return TryPrepareContinuousDodge(
                    runtimeState,
                    false,
                    out failureMessage
                );
            default:
                return Fail(
                    "不支持的测试场景：" + scenario,
                    out failureMessage
                );
        }
#else
        // Release版本即使误保存了测试枚举，也绝不修改正式RuntimeState。
        if (!hasLoggedReleaseSkip)
        {
            hasLoggedReleaseSkip = true;
            Debug.LogWarning(
                "[FormalPresentationTest] Release Build已忽略测试场景：" +
                scenario,
                this
            );
        }
        return true;
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool TryPrepareAttackTie(
        BattleRuntimeState runtimeState,
        out string failureMessage
    )
    {
        CharacterData ally;
        CharacterData enemy;
        BattleActionSlot allySlot;

        if (!TryValidateAttackTiePrerequisites(
                runtimeState,
                out ally,
                out enemy,
                out allySlot,
                out failureMessage))
        {
            return false;
        }

        List<BattleEnemyIntent> originalIntentQueue = runtimeState.intentQueue;
        BattleCardState allyCard = null;
        BattleCardState enemyCard = null;

        try
        {
            allyCard = BattleCardManager.CreateBattleCard(
                ally,
                CreateTestAttackCard(AllyCardID, "[TEST] Tie Attack A", 4),
                AllyCardID + "_INSTANCE"
            );
            enemyCard = BattleCardManager.CreateBattleCard(
                enemy,
                CreateTestAttackCard(EnemyCardID, "[TEST] Tie Attack B", 5),
                EnemyCardID + "_INSTANCE"
            );

            if (!IsOwnedCard(allyCard, ally) || !IsOwnedCard(enemyCard, enemy))
            {
                RollbackAttackTie(
                    runtimeState,
                    originalIntentQueue,
                    allySlot,
                    ally,
                    allyCard,
                    enemy,
                    enemyCard
                );
                return Fail("Runtime测试卡创建或Owner绑定失败。", out failureMessage);
            }

            BattleEnemyIntent testIntent = new BattleEnemyIntent(
                IntentID,
                enemy,
                enemyCard,
                ally,
                TestSlotIndex,
                1,
                1
            );

            runtimeState.SetIntentQueue(
                new List<BattleEnemyIntent> { testIntent }
            );

            BattleActionAssignmentResult assignmentResult;
            bool assigned = BattleActionSlotManager.TryAssignToEnemyIntent(
                runtimeState,
                ally,
                TestSlotIndex,
                allyCard,
                testIntent,
                out assignmentResult
            );

            if (!assigned || !IsValidAttackTieRelation(
                    allySlot,
                    ally,
                    allyCard,
                    enemy,
                    enemyCard,
                    testIntent,
                    assignmentResult))
            {
                string assignmentMessage = assignmentResult != null
                    ? assignmentResult.message
                    : "无安排结果";
                RollbackAttackTie(
                    runtimeState,
                    originalIntentQueue,
                    allySlot,
                    ally,
                    allyCard,
                    enemy,
                    enemyCard
                );
                return Fail(
                    "未能建立正式RespondedEnemyIntent关系：" + assignmentMessage,
                    out failureMessage
                );
            }

            hasPreparedScenario = true;
            preparedRuntimeState = runtimeState;
            Debug.Log(
                "[FormalPresentationTest] AttackTie scenario prepared. " +
                "Ally Slot1: fixed 4 (+1 = 5), " +
                "Enemy Slot1: fixed 5 (+0 = 5)",
                this
            );
            return true;
        }
        catch (Exception exception)
        {
            RollbackAttackTie(
                runtimeState,
                originalIntentQueue,
                allySlot,
                ally,
                allyCard,
                enemy,
                enemyCard
            );
            return Fail(
                "准备AttackTie场景时发生异常，已回滚：" + exception.Message,
                out failureMessage
            );
        }
    }

    private bool TryValidateAttackTiePrerequisites(
        BattleRuntimeState runtimeState,
        out CharacterData ally,
        out CharacterData enemy,
        out BattleActionSlot allySlot,
        out string failureMessage
    )
    {
        ally = runtimeState != null ? runtimeState.allyA : null;
        enemy = runtimeState != null ? runtimeState.enemy : null;
        allySlot = null;

        if (runtimeState == null)
        {
            return Fail("BattleRuntimeState为空。", out failureMessage);
        }

        if (runtimeState.LifecyclePhase != BattleLifecyclePhase.Prepare ||
            runtimeState.currentExecutionPlan != null)
        {
            return Fail(
                "AttackTie只能在Prepare且尚未创建ExecutionPlan时准备。当前Phase=" +
                runtimeState.LifecyclePhase +
                "，ExecutionPlan为空=" + (runtimeState.currentExecutionPlan == null),
                out failureMessage
            );
        }

        if (ally == null || enemy == null || ally.IsDead() || enemy.IsDead())
        {
            return Fail(
                "缺少存活的Ally A或Enemy。Ally=" + (ally != null) +
                "，Enemy=" + (enemy != null),
                out failureMessage
            );
        }

        allySlot = BattleActionSlotManager.GetSlot(
            runtimeState.actionSlots,
            ally,
            TestSlotIndex
        );
        if (allySlot == null)
        {
            return Fail("找不到Ally A Slot1。", out failureMessage);
        }

        if (!AreAllActionSlotsEmpty(runtimeState.actionSlots))
        {
            return Fail(
                "AttackTie准备前要求当前正式行动槽位全部为空，拒绝覆盖已有安排。",
                out failureMessage
            );
        }

        int allyAttackModifier = GetRoundedModifier(ally, "AttackPoint");
        int enemyAttackModifier = GetRoundedModifier(enemy, "AttackPoint");
        int allyClashModifier = GetRoundedModifier(ally, "ClashPoint");
        int enemyClashModifier = GetRoundedModifier(enemy, "ClashPoint");
        int allyCardModifier = GetRoundedModifier(ally, "CardPoint");
        int enemyCardModifier = GetRoundedModifier(enemy, "CardPoint");
        int allyNextClash = ally.GetBuffStack("NextClashPointUp");
        int enemyNextClash = enemy.GetBuffStack("NextClashPointUp");
        int allyNextCard = ally.GetBuffStack("NextCardPointUp");
        int enemyNextCard = enemy.GetBuffStack("NextCardPointUp");

        bool modifiersMatch =
            allyAttackModifier == 1 &&
            enemyAttackModifier == 0 &&
            allyClashModifier == 0 &&
            enemyClashModifier == 0 &&
            allyCardModifier == 0 &&
            enemyCardModifier == 0 &&
            allyNextClash == 0 &&
            enemyNextClash == 0 &&
            allyNextCard == 0 &&
            enemyNextCard == 0;

        if (!modifiersMatch)
        {
            return Fail(
                "AttackTie前置点数不成立。期望 Ally Attack/Clash/Card=1/0/0，" +
                "Enemy=0/0/0，双方NextClashPointUp/NextCardPointUp=0；实际 Ally=" +
                allyAttackModifier + "/" + allyClashModifier + "/" + allyCardModifier +
                "，Enemy=" + enemyAttackModifier + "/" + enemyClashModifier + "/" +
                enemyCardModifier + "，Next Ally=" + allyNextClash + "/" + allyNextCard +
                "，Next Enemy=" + enemyNextClash + "/" + enemyNextCard + "。",
                out failureMessage
            );
        }

        failureMessage = string.Empty;
        return true;
    }

    private bool TryPrepareAttackVsGuard(
        BattleRuntimeState runtimeState,
        bool expectFullBlock,
        out string failureMessage
    )
    {
        CharacterData ally;
        CharacterData enemy;
        BattleActionSlot allySlot;
        if (!TryValidateAttackVsGuardPrerequisites(
                runtimeState,
                expectFullBlock,
                out ally,
                out enemy,
                out allySlot,
                out failureMessage))
        {
            return false;
        }

        int defensePoint = expectFullBlock ? 6 : 2;
        int attackPoint = 5;
        string defenseCardID = expectFullBlock
            ? FullDefenseCardID
            : ReducedDefenseCardID;
        string attackCardID = expectFullBlock
            ? FullAttackCardID
            : ReducedAttackCardID;
        string intentID = expectFullBlock
            ? FullIntentID
            : ReducedIntentID;
        string scenarioName = expectFullBlock
            ? "AttackVsGuardFullBlock"
            : "AttackVsGuardReducedDamage";

        List<BattleEnemyIntent> originalIntentQueue = runtimeState.intentQueue;
        BattleCardState defenseCard = null;
        BattleCardState attackCard = null;

        try
        {
            defenseCard = BattleCardManager.CreateBattleCard(
                ally,
                CreateTestDefenseCard(
                    defenseCardID,
                    expectFullBlock
                        ? "[TEST] Full Defense"
                        : "[TEST] Reduced Defense",
                    defensePoint
                ),
                defenseCardID + "_INSTANCE"
            );
            attackCard = BattleCardManager.CreateBattleCard(
                enemy,
                CreateTestAttackCard(
                    attackCardID,
                    expectFullBlock
                        ? "[TEST] Full Attack"
                        : "[TEST] Reduced Attack",
                    attackPoint
                ),
                attackCardID + "_INSTANCE"
            );

            if (!IsOwnedCard(defenseCard, ally) ||
                !IsOwnedCard(attackCard, enemy))
            {
                RollbackAttackVsGuard(
                    runtimeState,
                    originalIntentQueue,
                    allySlot,
                    ally,
                    defenseCard,
                    enemy,
                    attackCard
                );
                return Fail(
                    "Runtime Guard测试卡创建或Owner绑定失败。",
                    out failureMessage
                );
            }

            BattleEnemyIntent testIntent = new BattleEnemyIntent(
                intentID,
                enemy,
                attackCard,
                ally,
                TestSlotIndex,
                1,
                1
            );
            runtimeState.SetIntentQueue(
                new List<BattleEnemyIntent> { testIntent }
            );

            BattleActionAssignmentResult assignmentResult;
            bool assigned = BattleActionSlotManager.TryAssignToEnemyIntent(
                runtimeState,
                ally,
                TestSlotIndex,
                defenseCard,
                testIntent,
                out assignmentResult
            );

            if (!assigned || !IsValidAttackVsGuardRelation(
                    allySlot,
                    ally,
                    defenseCard,
                    enemy,
                    attackCard,
                    testIntent,
                    assignmentResult))
            {
                string assignmentMessage = assignmentResult != null
                    ? assignmentResult.message
                    : "无安排结果";
                RollbackAttackVsGuard(
                    runtimeState,
                    originalIntentQueue,
                    allySlot,
                    ally,
                    defenseCard,
                    enemy,
                    attackCard
                );
                return Fail(
                    "未能建立正式Defense RespondedEnemyIntent关系：" +
                    assignmentMessage,
                    out failureMessage
                );
            }

            hasPreparedScenario = true;
            preparedRuntimeState = runtimeState;
            Debug.Log(
                "[FormalPresentationTest] " + scenarioName +
                " scenario prepared. Ally Slot1 Defense=" + defensePoint +
                "，Enemy Intent1 Attack=" + attackPoint +
                "，Expected=" +
                (expectFullBlock
                    ? "DefenseFullBlock"
                    : "DefenseReducedDamage(RemainingAttackPoint=3)"),
                this
            );
            return true;
        }
        catch (Exception exception)
        {
            RollbackAttackVsGuard(
                runtimeState,
                originalIntentQueue,
                allySlot,
                ally,
                defenseCard,
                enemy,
                attackCard
            );
            return Fail(
                "准备" + scenarioName + "场景时发生异常，已回滚：" +
                exception.Message,
                out failureMessage
            );
        }
    }

    private bool TryValidateAttackVsGuardPrerequisites(
        BattleRuntimeState runtimeState,
        bool expectFullBlock,
        out CharacterData ally,
        out CharacterData enemy,
        out BattleActionSlot allySlot,
        out string failureMessage
    )
    {
        ally = runtimeState != null ? runtimeState.allyA : null;
        enemy = runtimeState != null ? runtimeState.enemy : null;
        allySlot = null;
        string scenarioName = expectFullBlock
            ? "AttackVsGuardFullBlock"
            : "AttackVsGuardReducedDamage";

        if (runtimeState == null)
        {
            return Fail("BattleRuntimeState为空。", out failureMessage);
        }

        if (runtimeState.LifecyclePhase != BattleLifecyclePhase.Prepare ||
            runtimeState.currentExecutionPlan != null)
        {
            return Fail(
                scenarioName +
                "只能在Prepare且尚未创建ExecutionPlan时准备。当前Phase=" +
                runtimeState.LifecyclePhase +
                "，ExecutionPlan为空=" +
                (runtimeState.currentExecutionPlan == null),
                out failureMessage
            );
        }

        if (ally == null || enemy == null || ally.IsDead() || enemy.IsDead())
        {
            return Fail(
                "缺少存活的Ally A或Enemy。Ally=" + (ally != null) +
                "，Enemy=" + (enemy != null),
                out failureMessage
            );
        }

        allySlot = BattleActionSlotManager.GetSlot(
            runtimeState.actionSlots,
            ally,
            TestSlotIndex
        );
        if (allySlot == null)
        {
            return Fail("找不到Ally A Slot1。", out failureMessage);
        }

        if (!AreAllActionSlotsEmpty(runtimeState.actionSlots))
        {
            return Fail(
                scenarioName +
                "准备前要求当前正式行动槽位全部为空，拒绝覆盖已有安排。",
                out failureMessage
            );
        }

        int defenderDefense = GetRoundedModifier(ally, "DefensePoint");
        int attackerAttack = GetRoundedModifier(enemy, "AttackPoint");
        int defenderCard = GetRoundedModifier(ally, "CardPoint");
        int attackerCard = GetRoundedModifier(enemy, "CardPoint");
        int defenderClash = GetRoundedModifier(ally, "ClashPoint");
        int attackerClash = GetRoundedModifier(enemy, "ClashPoint");
        int defenderNextClash = ally.GetBuffStack("NextClashPointUp");
        int attackerNextClash = enemy.GetBuffStack("NextClashPointUp");
        int defenderNextCard = ally.GetBuffStack("NextCardPointUp");
        int attackerNextCard = enemy.GetBuffStack("NextCardPointUp");
        int damageMultiplier = Mathf.RoundToInt(
            100f +
            enemy.GetBuffPercentModifier("DamageDealt") +
            ally.GetBuffPercentModifier("DamageTaken")
        );

        bool pointModifiersMatch =
            defenderDefense == 0 && attackerAttack == 0 &&
            defenderCard == 0 && attackerCard == 0 &&
            defenderClash == 0 && attackerClash == 0 &&
            defenderNextClash == 0 && attackerNextClash == 0 &&
            defenderNextCard == 0 && attackerNextCard == 0;
        bool damageCanRemainPositive = expectFullBlock ||
            damageMultiplier > 0;

        if (!pointModifiersMatch || !damageCanRemainPositive)
        {
            return Fail(
                scenarioName +
                "前置点数不成立。期望 Defender Defense/Card/Clash=0/0/0，" +
                "Attacker Attack/Card/Clash=0/0/0，双方" +
                "NextClashPointUp/NextCardPointUp=0，Reduced伤害倍率>0；实际 " +
                "Defender=" + defenderDefense + "/" + defenderCard + "/" +
                defenderClash + "，Attacker=" + attackerAttack + "/" +
                attackerCard + "/" + attackerClash + "，Next Defender=" +
                defenderNextClash + "/" + defenderNextCard +
                "，Next Attacker=" + attackerNextClash + "/" +
                attackerNextCard + "，DamageMultiplier=" +
                damageMultiplier + "% 。测试卡无ResourceRule，因此" +
                "Resource Point Modifier固定为0。",
                out failureMessage
            );
        }

        failureMessage = string.Empty;
        return true;
    }

    private bool TryPrepareContinuousDodge(
        BattleRuntimeState runtimeState,
        bool expectSecondSuccess,
        out string failureMessage
    )
    {
        CharacterData ally;
        CharacterData enemy;
        BattleActionSlot allySlot;
        if (!TryValidateContinuousDodgePrerequisites(
                runtimeState,
                expectSecondSuccess,
                out ally,
                out enemy,
                out allySlot,
                out failureMessage))
        {
            return false;
        }

        int dodgePoint = expectSecondSuccess ? 6 : 5;
        int attackOnePoint = 3;
        int attackTwoPoint = expectSecondSuccess ? 4 : 7;
        string dodgeCardID = expectSecondSuccess
            ? ContinuousSuccessSuccessDodgeCardID
            : ContinuousSuccessFailedDodgeCardID;
        string attackOneCardID = expectSecondSuccess
            ? ContinuousSuccessSuccessAttackOneCardID
            : ContinuousSuccessFailedAttackOneCardID;
        string attackTwoCardID = expectSecondSuccess
            ? ContinuousSuccessSuccessAttackTwoCardID
            : ContinuousSuccessFailedAttackTwoCardID;
        string intentOneID = expectSecondSuccess
            ? ContinuousSuccessSuccessIntentOneID
            : ContinuousSuccessFailedIntentOneID;
        string intentTwoID = expectSecondSuccess
            ? ContinuousSuccessSuccessIntentTwoID
            : ContinuousSuccessFailedIntentTwoID;
        string scenarioName = expectSecondSuccess
            ? "Continuous Dodge Success→Success"
            : "Continuous Dodge Success→Failed";

        List<BattleEnemyIntent> originalIntentQueue = runtimeState.intentQueue;
        BattleCardState dodgeCard = null;
        BattleCardState attackOneCard = null;
        BattleCardState attackTwoCard = null;

        try
        {
            dodgeCard = BattleCardManager.CreateBattleCard(
                ally,
                CreateTestDodgeCard(
                    dodgeCardID,
                    "[TEST] Continuous Dodge",
                    dodgePoint
                ),
                dodgeCardID + "_INSTANCE"
            );
            attackOneCard = BattleCardManager.CreateBattleCard(
                enemy,
                CreateTestAttackCard(
                    attackOneCardID,
                    "[TEST] Attack 1",
                    attackOnePoint
                ),
                attackOneCardID + "_INSTANCE"
            );
            attackTwoCard = BattleCardManager.CreateBattleCard(
                enemy,
                CreateTestAttackCard(
                    attackTwoCardID,
                    expectSecondSuccess
                        ? "[TEST] Attack 2"
                        : "[TEST] Strong Attack",
                    attackTwoPoint
                ),
                attackTwoCardID + "_INSTANCE"
            );

            if (!IsOwnedCard(dodgeCard, ally) ||
                !IsOwnedCard(attackOneCard, enemy) ||
                !IsOwnedCard(attackTwoCard, enemy))
            {
                RollbackContinuousDodge(
                    runtimeState,
                    originalIntentQueue,
                    allySlot,
                    ally,
                    dodgeCard,
                    enemy,
                    attackOneCard,
                    attackTwoCard
                );
                return Fail(
                    "Runtime Continuous Dodge测试卡创建或Owner绑定失败。",
                    out failureMessage
                );
            }

            string eligibilityFailure;
            if (!AreContinuousDodgeCardsEligible(
                    ally,
                    enemy,
                    dodgeCard,
                    attackOneCard,
                    attackTwoCard,
                    out eligibilityFailure))
            {
                RollbackContinuousDodge(
                    runtimeState,
                    originalIntentQueue,
                    allySlot,
                    ally,
                    dodgeCard,
                    enemy,
                    attackOneCard,
                    attackTwoCard
                );
                return Fail(eligibilityFailure, out failureMessage);
            }

            BattleEnemyIntent firstIntent = new BattleEnemyIntent(
                intentOneID,
                enemy,
                attackOneCard,
                ally,
                TestSlotIndex,
                1,
                1
            );
            BattleEnemyIntent secondIntent = new BattleEnemyIntent(
                intentTwoID,
                enemy,
                attackTwoCard,
                ally,
                TestSlotIndex,
                2,
                2
            );
            runtimeState.SetIntentQueue(
                new List<BattleEnemyIntent> { firstIntent, secondIntent }
            );

            BattleActionAssignmentResult assignmentResult;
            bool assigned = BattleActionSlotManager.TryAssignToEnemyIntent(
                runtimeState,
                ally,
                TestSlotIndex,
                dodgeCard,
                firstIntent,
                out assignmentResult
            );

            string relationFailure = "Assignment未成功";
            if (!assigned || !IsValidContinuousDodgeInitialState(
                    runtimeState,
                    allySlot,
                    ally,
                    dodgeCard,
                    enemy,
                    attackOneCard,
                    attackTwoCard,
                    firstIntent,
                    secondIntent,
                    assignmentResult,
                    out relationFailure))
            {
                string assignmentMessage = assignmentResult != null
                    ? assignmentResult.message
                    : "无安排结果";
                RollbackContinuousDodge(
                    runtimeState,
                    originalIntentQueue,
                    allySlot,
                    ally,
                    dodgeCard,
                    enemy,
                    attackOneCard,
                    attackTwoCard
                );
                return Fail(
                    "未能建立连续Dodge初始关系：" + relationFailure +
                    " Assignment=" + assignmentMessage,
                    out failureMessage
                );
            }

            string executionOrderFailure;
            if (!HasExpectedContinuousDodgeExecutionOrder(
                    runtimeState,
                    allySlot,
                    firstIntent,
                    secondIntent,
                    out executionOrderFailure))
            {
                RollbackContinuousDodge(
                    runtimeState,
                    originalIntentQueue,
                    allySlot,
                    ally,
                    dodgeCard,
                    enemy,
                    attackOneCard,
                    attackTwoCard
                );
                return Fail(executionOrderFailure, out failureMessage);
            }

            hasPreparedScenario = true;
            preparedRuntimeState = runtimeState;
            Debug.Log(
                "[FormalPresentationTest] " + scenarioName +
                " scenario prepared. Dodge Point=" + dodgePoint +
                "，Attack1 Point=" + attackOnePoint +
                "，Attack2 Point=" + attackTwoPoint +
                "。Intent1=Responded，Intent2=Unresponded，" +
                "Expected=" +
                (expectSecondSuccess
                    ? "DodgeSuccess→DodgeSuccess"
                    : "DodgeSuccess→DodgeFailed"),
                this
            );
            return true;
        }
        catch (Exception exception)
        {
            RollbackContinuousDodge(
                runtimeState,
                originalIntentQueue,
                allySlot,
                ally,
                dodgeCard,
                enemy,
                attackOneCard,
                attackTwoCard
            );
            return Fail(
                "准备" + scenarioName + "场景时发生异常，已回滚：" +
                exception.Message,
                out failureMessage
            );
        }
    }

    private bool TryValidateContinuousDodgePrerequisites(
        BattleRuntimeState runtimeState,
        bool expectSecondSuccess,
        out CharacterData ally,
        out CharacterData enemy,
        out BattleActionSlot allySlot,
        out string failureMessage
    )
    {
        ally = runtimeState != null ? runtimeState.allyA : null;
        enemy = runtimeState != null ? runtimeState.enemy : null;
        allySlot = null;
        string scenarioName = expectSecondSuccess
            ? "ContinuousDodgeSuccessSuccess"
            : "ContinuousDodgeSuccessFailed";

        if (runtimeState == null)
        {
            return Fail("BattleRuntimeState为空。", out failureMessage);
        }

        if (runtimeState.LifecyclePhase != BattleLifecyclePhase.Prepare ||
            runtimeState.currentExecutionPlan != null)
        {
            return Fail(
                scenarioName +
                "只能在Prepare且尚未创建ExecutionPlan时准备。当前Phase=" +
                runtimeState.LifecyclePhase +
                "，ExecutionPlan为空=" +
                (runtimeState.currentExecutionPlan == null),
                out failureMessage
            );
        }

        if (ally == null || enemy == null || ally.IsDead() || enemy.IsDead())
        {
            return Fail(
                "缺少存活的Ally A或Enemy。Ally=" + (ally != null) +
                "，Enemy=" + (enemy != null),
                out failureMessage
            );
        }

        allySlot = BattleActionSlotManager.GetSlot(
            runtimeState.actionSlots,
            ally,
            TestSlotIndex
        );
        if (allySlot == null)
        {
            return Fail("找不到Ally A Slot1。", out failureMessage);
        }

        if (!AreAllActionSlotsEmpty(runtimeState.actionSlots))
        {
            return Fail(
                scenarioName +
                "准备前要求当前正式行动槽位全部为空，拒绝覆盖已有安排。",
                out failureMessage
            );
        }

        int allyDodgeModifier = GetRoundedModifier(ally, "DodgePoint");
        int allyClashModifier = GetRoundedModifier(ally, "ClashPoint");
        int allyCardModifier = GetRoundedModifier(ally, "CardPoint");
        int enemyAttackModifier = GetRoundedModifier(enemy, "AttackPoint");
        int enemyClashModifier = GetRoundedModifier(enemy, "ClashPoint");
        int enemyCardModifier = GetRoundedModifier(enemy, "CardPoint");
        int allyNextClash = ally.GetBuffStack("NextClashPointUp");
        int allyNextCard = ally.GetBuffStack("NextCardPointUp");
        int enemyNextClash = enemy.GetBuffStack("NextClashPointUp");
        int enemyNextCard = enemy.GetBuffStack("NextCardPointUp");
        int damageMultiplier = Mathf.RoundToInt(
            100f +
            enemy.GetBuffPercentModifier("DamageDealt") +
            ally.GetBuffPercentModifier("DamageTaken")
        );

        bool modifiersMatch =
            allyDodgeModifier == 0 &&
            allyClashModifier == 0 &&
            allyCardModifier == 0 &&
            enemyAttackModifier == 0 &&
            enemyClashModifier == 0 &&
            enemyCardModifier == 0 &&
            allyNextClash == 0 &&
            allyNextCard == 0 &&
            enemyNextClash == 0 &&
            enemyNextCard == 0 &&
            (expectSecondSuccess ||
                (damageMultiplier == 100 && ally.currentHP > 7));

        if (!modifiersMatch)
        {
            return Fail(
                scenarioName +
                "前置点数不稳定。期望 Ally Dodge/Clash/Card=0/0/0，" +
                "Enemy Attack/Clash/Card=0/0/0，双方" +
                "NextClashPointUp/NextCardPointUp=0；实际 Ally=" +
                allyDodgeModifier + "/" + allyClashModifier + "/" +
                allyCardModifier + "，Enemy=" + enemyAttackModifier + "/" +
                enemyClashModifier + "/" + enemyCardModifier +
                "，Next Ally=" + allyNextClash + "/" + allyNextCard +
                "，Next Enemy=" + enemyNextClash + "/" + enemyNextCard +
                "，DamageMultiplier=" + damageMultiplier +
                "% ，Ally HP=" + ally.currentHP +
                "。失败场景还要求伤害倍率=100%且Ally HP>7；" +
                "测试卡无ResourceRule，因此Resource Point Modifier固定为0。",
                out failureMessage
            );
        }

        int dodgePoint = expectSecondSuccess ? 6 : 5;
        int attackOnePoint = 3;
        int attackTwoPoint = expectSecondSuccess ? 4 : 7;
        bool expectedRelationsHold =
            dodgePoint > attackOnePoint &&
            (expectSecondSuccess
                ? dodgePoint > attackTwoPoint
                : attackTwoPoint > dodgePoint);
        if (!expectedRelationsHold)
        {
            return Fail(
                scenarioName +
                "固定点数关系不成立。Dodge=" + dodgePoint +
                "，Attack1=" + attackOnePoint +
                "，Attack2=" + attackTwoPoint,
                out failureMessage
            );
        }

        failureMessage = string.Empty;
        return true;
    }

    private static bool AreContinuousDodgeCardsEligible(
        CharacterData ally,
        CharacterData enemy,
        BattleCardState dodgeCard,
        BattleCardState attackOneCard,
        BattleCardState attackTwoCard,
        out string failureMessage
    )
    {
        CardEligibilityResult dodgeEligibility =
            BattleCardManager.EvaluateCardEligibility(ally, enemy, dodgeCard);
        CardEligibilityResult attackOneEligibility =
            BattleCardManager.EvaluateCardEligibility(enemy, ally, attackOneCard);
        CardEligibilityResult attackTwoEligibility =
            BattleCardManager.EvaluateCardEligibility(enemy, ally, attackTwoCard);

        if (dodgeEligibility == null || !dodgeEligibility.isEligible)
        {
            failureMessage = "Continuous Dodge测试卡不可用：" +
                GetEligibilityFailure(dodgeEligibility);
            return false;
        }

        if (attackOneEligibility == null || !attackOneEligibility.isEligible)
        {
            failureMessage = "Attack #1测试卡不可用：" +
                GetEligibilityFailure(attackOneEligibility);
            return false;
        }

        if (attackTwoEligibility == null || !attackTwoEligibility.isEligible)
        {
            failureMessage = "Attack #2测试卡不可用：" +
                GetEligibilityFailure(attackTwoEligibility);
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

    private static string GetEligibilityFailure(CardEligibilityResult result)
    {
        return result != null
            ? result.failureReason + " / " + result.failureMessage
            : "CardEligibilityResult为空";
    }

    private static bool IsValidContinuousDodgeInitialState(
        BattleRuntimeState runtimeState,
        BattleActionSlot allySlot,
        CharacterData ally,
        BattleCardState dodgeCard,
        CharacterData enemy,
        BattleCardState attackOneCard,
        BattleCardState attackTwoCard,
        BattleEnemyIntent firstIntent,
        BattleEnemyIntent secondIntent,
        BattleActionAssignmentResult assignmentResult,
        out string failureMessage
    )
    {
        bool firstRelationValid =
            dodgeCard != null && dodgeCard.cardData != null &&
            dodgeCard.cardData.cardType == CardType.Dodge &&
            attackOneCard != null && attackOneCard.cardData != null &&
            attackOneCard.cardData.cardType == CardType.Attack &&
            allySlot != null && assignmentResult != null &&
            assignmentResult.isSuccess &&
            !assignmentResult.wasAutoDowngraded &&
            assignmentResult.placementType ==
                BattleActionPlacementType.ExactEnemyIntent &&
            assignmentResult.effectiveSlotType ==
                BattleActionSlotType.RespondToEnemyIntent &&
            ReferenceEquals(allySlot.owner, ally) &&
            ReferenceEquals(allySlot.actor, ally) &&
            ReferenceEquals(allySlot.cardState, dodgeCard) &&
            allySlot.slotIndex == TestSlotIndex &&
            allySlot.slotType == BattleActionSlotType.RespondToEnemyIntent &&
            allySlot.placementType ==
                BattleActionPlacementType.ExactEnemyIntent &&
            ReferenceEquals(allySlot.requestedEnemyIntent, firstIntent) &&
            ReferenceEquals(allySlot.enemyIntent, firstIntent) &&
            ReferenceEquals(allySlot.target, enemy) &&
            firstIntent.intentOrder == 1 &&
            firstIntent.enemySlotIndex == 1 &&
            ReferenceEquals(firstIntent.enemy, enemy) &&
            ReferenceEquals(firstIntent.enemyCardState, attackOneCard) &&
            ReferenceEquals(firstIntent.originalTargetCharacter, ally) &&
            firstIntent.originalTargetSlotIndex == TestSlotIndex &&
            firstIntent.isResponded &&
            ReferenceEquals(firstIntent.actualTargetCharacter, ally) &&
            firstIntent.actualTargetSlotIndex == TestSlotIndex;

        bool secondIntentValid =
            attackTwoCard != null && attackTwoCard.cardData != null &&
            attackTwoCard.cardData.cardType == CardType.Attack &&
            secondIntent != null &&
            !ReferenceEquals(firstIntent, secondIntent) &&
            !ReferenceEquals(attackOneCard, attackTwoCard) &&
            secondIntent.intentOrder == 2 &&
            secondIntent.enemySlotIndex == 2 &&
            ReferenceEquals(secondIntent.enemy, enemy) &&
            ReferenceEquals(secondIntent.enemyCardState, attackTwoCard) &&
            ReferenceEquals(secondIntent.originalTargetCharacter, ally) &&
            secondIntent.originalTargetSlotIndex == TestSlotIndex &&
            !secondIntent.isResponded &&
            ReferenceEquals(secondIntent.actualTargetCharacter, ally) &&
            secondIntent.actualTargetSlotIndex == TestSlotIndex;

        bool queueValid = runtimeState != null &&
            runtimeState.intentQueue != null &&
            runtimeState.intentQueue.Count == 2 &&
            ReferenceEquals(runtimeState.intentQueue[0], firstIntent) &&
            ReferenceEquals(runtimeState.intentQueue[1], secondIntent);

        if (!firstRelationValid || !secondIntentValid || !queueValid)
        {
            failureMessage =
                "FirstRelation=" + firstRelationValid +
                "，SecondIntent=" + secondIntentValid +
                "，Queue=" + queueValid;
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

    private static bool HasExpectedContinuousDodgeExecutionOrder(
        BattleRuntimeState runtimeState,
        BattleActionSlot allySlot,
        BattleEnemyIntent firstIntent,
        BattleEnemyIntent secondIntent,
        out string failureMessage
    )
    {
        BattleExecutionPlan previewPlan =
            BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
                runtimeState.actionSlots,
                runtimeState.intentQueue,
                runtimeState
            );
        bool hasTwoItems = previewPlan != null &&
            previewPlan.executionItems != null &&
            previewPlan.executionItems.Count == 2;
        BattleExecutionItem firstItem = hasTwoItems
            ? previewPlan.executionItems[0]
            : null;
        BattleExecutionItem secondItem = hasTwoItems
            ? previewPlan.executionItems[1]
            : null;

        bool firstValid = firstItem != null &&
            firstItem.order == 1 &&
            firstItem.executionType ==
                BattleExecutionItemType.RespondedEnemyIntent &&
            ReferenceEquals(firstItem.enemyIntent, firstIntent) &&
            ReferenceEquals(firstItem.actionSlot, allySlot);
        bool secondValid = secondItem != null &&
            secondItem.order == 2 &&
            secondItem.executionType ==
                BattleExecutionItemType.UnrespondedEnemyIntent &&
            ReferenceEquals(secondItem.enemyIntent, secondIntent) &&
            secondItem.actionSlot == null;

        if (!hasTwoItems || !firstValid || !secondValid)
        {
            failureMessage =
                "Continuous Dodge ExecutionPlan顺序不符合#1 Responded -> " +
                "#2 Unresponded。Count=" +
                (previewPlan != null && previewPlan.executionItems != null
                    ? previewPlan.executionItems.Count
                    : 0) +
                "，First=" + firstValid + "，Second=" + secondValid;
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

    private static CardTestData CreateTestAttackCard(
        string cardID,
        string cardName,
        int fixedPoint
    )
    {
        return new CardTestData
        {
            cardID = cardID,
            cardName = cardName,
            cardType = CardType.Attack,
            isSinCard = false,
            consumeOnUse = false,
            isClashable = true,
            minPoint = fixedPoint,
            maxPoint = fixedPoint,
            cooldown = 0,
            maxUseCount = 0,
            damageFormula = "PointAsDamage"
        };
    }

    private static CardTestData CreateTestDefenseCard(
        string cardID,
        string cardName,
        int fixedPoint
    )
    {
        return new CardTestData
        {
            cardID = cardID,
            cardName = cardName,
            cardType = CardType.Defense,
            isSinCard = false,
            consumeOnUse = false,
            isClashable = true,
            minPoint = fixedPoint,
            maxPoint = fixedPoint,
            cooldown = 0,
            maxUseCount = 0,
            defenseFormula = "PointAsDefense"
        };
    }

    private static CardTestData CreateTestDodgeCard(
        string cardID,
        string cardName,
        int fixedPoint
    )
    {
        return new CardTestData
        {
            cardID = cardID,
            cardName = cardName,
            cardType = CardType.Dodge,
            isSinCard = false,
            consumeOnUse = false,
            isClashable = true,
            minPoint = fixedPoint,
            maxPoint = fixedPoint,
            cooldown = 0,
            maxUseCount = 0
        };
    }

    private static bool IsValidAttackTieRelation(
        BattleActionSlot allySlot,
        CharacterData ally,
        BattleCardState allyCard,
        CharacterData enemy,
        BattleCardState enemyCard,
        BattleEnemyIntent intent,
        BattleActionAssignmentResult assignmentResult
    )
    {
        return allySlot != null &&
            assignmentResult != null &&
            assignmentResult.isSuccess &&
            !assignmentResult.wasAutoDowngraded &&
            assignmentResult.placementType == BattleActionPlacementType.ExactEnemyIntent &&
            assignmentResult.effectiveSlotType == BattleActionSlotType.RespondToEnemyIntent &&
            ReferenceEquals(allySlot.owner, ally) &&
            ReferenceEquals(allySlot.actor, ally) &&
            ReferenceEquals(allySlot.cardState, allyCard) &&
            allySlot.slotIndex == TestSlotIndex &&
            allySlot.slotType == BattleActionSlotType.RespondToEnemyIntent &&
            allySlot.placementType == BattleActionPlacementType.ExactEnemyIntent &&
            ReferenceEquals(allySlot.requestedEnemyIntent, intent) &&
            ReferenceEquals(allySlot.enemyIntent, intent) &&
            ReferenceEquals(allySlot.target, enemy) &&
            intent.intentOrder == 1 &&
            intent.enemySlotIndex == 1 &&
            ReferenceEquals(intent.enemy, enemy) &&
            ReferenceEquals(intent.enemyCardState, enemyCard) &&
            ReferenceEquals(intent.originalTargetCharacter, ally) &&
            intent.originalTargetSlotIndex == TestSlotIndex &&
            intent.isResponded &&
            ReferenceEquals(intent.actualTargetCharacter, ally) &&
            intent.actualTargetSlotIndex == TestSlotIndex;
    }

    private static bool IsValidAttackVsGuardRelation(
        BattleActionSlot allySlot,
        CharacterData ally,
        BattleCardState defenseCard,
        CharacterData enemy,
        BattleCardState attackCard,
        BattleEnemyIntent intent,
        BattleActionAssignmentResult assignmentResult
    )
    {
        return defenseCard != null && defenseCard.cardData != null &&
            defenseCard.cardData.cardType == CardType.Defense &&
            attackCard != null && attackCard.cardData != null &&
            attackCard.cardData.cardType == CardType.Attack &&
            allySlot != null && assignmentResult != null &&
            assignmentResult.isSuccess &&
            !assignmentResult.wasAutoDowngraded &&
            assignmentResult.placementType ==
                BattleActionPlacementType.ExactEnemyIntent &&
            assignmentResult.effectiveSlotType ==
                BattleActionSlotType.RespondToEnemyIntent &&
            ReferenceEquals(allySlot.owner, ally) &&
            ReferenceEquals(allySlot.actor, ally) &&
            ReferenceEquals(allySlot.cardState, defenseCard) &&
            allySlot.slotIndex == TestSlotIndex &&
            allySlot.slotType == BattleActionSlotType.RespondToEnemyIntent &&
            allySlot.placementType ==
                BattleActionPlacementType.ExactEnemyIntent &&
            ReferenceEquals(allySlot.requestedEnemyIntent, intent) &&
            ReferenceEquals(allySlot.enemyIntent, intent) &&
            ReferenceEquals(allySlot.target, enemy) &&
            intent.intentOrder == 1 && intent.enemySlotIndex == 1 &&
            ReferenceEquals(intent.enemy, enemy) &&
            ReferenceEquals(intent.enemyCardState, attackCard) &&
            ReferenceEquals(intent.originalTargetCharacter, ally) &&
            intent.originalTargetSlotIndex == TestSlotIndex &&
            intent.isResponded &&
            ReferenceEquals(intent.actualTargetCharacter, ally) &&
            intent.actualTargetSlotIndex == TestSlotIndex;
    }

    private static bool IsOwnedCard(BattleCardState card, CharacterData owner)
    {
        return card != null &&
            card.cardData != null &&
            ReferenceEquals(card.owner, owner) &&
            owner != null &&
            owner.battleCards != null &&
            owner.battleCards.Contains(card);
    }

    private static bool AreAllActionSlotsEmpty(List<BattleActionSlot> actionSlots)
    {
        if (actionSlots == null || actionSlots.Count == 0)
        {
            return false;
        }

        foreach (BattleActionSlot slot in actionSlots)
        {
            if (slot == null || !slot.IsEmpty())
            {
                return false;
            }
        }

        return true;
    }

    private static int GetRoundedModifier(CharacterData character, string stat)
    {
        return Mathf.RoundToInt(character.GetBuffFlatModifier(stat));
    }

    private static void RollbackAttackTie(
        BattleRuntimeState runtimeState,
        List<BattleEnemyIntent> originalIntentQueue,
        BattleActionSlot allySlot,
        CharacterData ally,
        BattleCardState allyCard,
        CharacterData enemy,
        BattleCardState enemyCard
    )
    {
        if (allySlot != null)
        {
            allySlot.Clear();
        }

        if (runtimeState != null)
        {
            runtimeState.SetIntentQueue(originalIntentQueue);
        }

        RemoveTestCard(ally, allyCard);
        RemoveTestCard(enemy, enemyCard);
    }

    private static void RollbackAttackVsGuard(
        BattleRuntimeState runtimeState,
        List<BattleEnemyIntent> originalIntentQueue,
        BattleActionSlot allySlot,
        CharacterData ally,
        BattleCardState defenseCard,
        CharacterData enemy,
        BattleCardState attackCard
    )
    {
        if (allySlot != null)
        {
            allySlot.Clear();
        }

        if (runtimeState != null)
        {
            runtimeState.SetIntentQueue(originalIntentQueue);
        }

        RemoveTestCard(ally, defenseCard);
        RemoveTestCard(enemy, attackCard);
    }

    private static void RollbackContinuousDodge(
        BattleRuntimeState runtimeState,
        List<BattleEnemyIntent> originalIntentQueue,
        BattleActionSlot allySlot,
        CharacterData ally,
        BattleCardState dodgeCard,
        CharacterData enemy,
        BattleCardState attackOneCard,
        BattleCardState attackTwoCard
    )
    {
        if (allySlot != null)
        {
            allySlot.Clear();
        }

        if (runtimeState != null)
        {
            runtimeState.SetIntentQueue(originalIntentQueue);
        }

        RemoveTestCard(ally, dodgeCard);
        RemoveTestCard(enemy, attackOneCard);
        RemoveTestCard(enemy, attackTwoCard);
    }

    private static void RemoveTestCard(CharacterData owner, BattleCardState card)
    {
        if (owner != null && owner.battleCards != null && card != null)
        {
            owner.battleCards.Remove(card);
        }
    }

    private bool Fail(string detail, out string failureMessage)
    {
        failureMessage = "[FormalPresentationTest] " + detail;
        Debug.LogError(failureMessage, this);
        return false;
    }
#endif
}
