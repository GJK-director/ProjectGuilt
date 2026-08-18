using System;
using System.Collections.Generic;
using UnityEngine;

public enum BattleFormalPresentationTestScenario
{
    None,
    AttackTie
}

// 正式BattleScene的开发测试输入入口；只准备规则数据，不驱动执行或表现。
public sealed class BattleFormalPresentationTestHarness : MonoBehaviour
{
    private const string AllyCardID = "[TEST]_FORMAL_TIE_ALLY_ATTACK";
    private const string EnemyCardID = "[TEST]_FORMAL_TIE_ENEMY_ATTACK";
    private const string IntentID = "[TEST]_FORMAL_TIE_ENEMY_INTENT";
    private const int TestSlotIndex = 1;

    [SerializeField]
    private BattleFormalPresentationTestScenario scenario =
        BattleFormalPresentationTestScenario.None;

    private bool hasPreparedScenario;
    private BattleRuntimeState preparedRuntimeState;
    private bool hasLoggedReleaseSkip;

    public BattleFormalPresentationTestScenario Scenario => scenario;

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
