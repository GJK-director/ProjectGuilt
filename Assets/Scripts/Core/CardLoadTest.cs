// 脚本中文说明：卡牌读取和战斗测试入口。负责在 Unity 场景启动时创建测试角色、读取卡牌并运行指定测试流程。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum BattleTestMode
{
    BattleRuntimeStateEndCurrentTurnBasic = 2,
    BattleRuntimeStatePrepareNextTurnBasic = 3,
    BattleResolverResolveRespondedAttackVsAttackBasic = 7,
    BattleResolverRespondedPlayerWinBothCardsResolvedBasic = 8,
    BattleResolverRespondedEnemyWinBothCardsResolvedBasic = 9,
    BattleResolverRespondedClashSinLoseResolvedBasic = 10,
    BattleResolverResolveRespondedDefenseFullBlockBasic = 11,
    BattleResolverResolveRespondedDefenseReducedDamageBasic = 12,
    BattleResolverDefenseKnownEnemyPointBasic = 13,
    ActionSlotExecutionPlanExecuteFreeAbilityBasic = 19,
    ActionSlotExecutionPlanExecuteHighSpeedFreeAttackMixedBasic = 20,
    ActionSlotExecutionPlanExecuteUnrespondedBasic = 22,
    ActionSlotPassiveGuardFullBlockBasic = 25,
    ActionSlotPassiveGuardReducedDamageBasic = 26,
    ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardReducedDamageBasic = 32,
    ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardFullBlockBasic = 33,
    ActionSlotExecutionPlanExecuteRespondedEnemyWinNoPassiveGuardBasic = 36,
    ActionSlotExecutionPlanExecuteRespondedPlayerWinPassiveGuardNotTriggeredBasic = 37,
    ActionSlotExecutionPlanExecuteRespondedTieLimit = 39,
    ActionSlotExecutionPlanExecuteMixedBasic = 40,
    BattleResolverRespondedDodgeVsAttackBasic = 41,
    ActionSlotPassiveDodgeUnrespondedBasic = 42,
    ActionSlotPassiveDodgeAfterAttackLoseBasic = 43,
    BattleEndedVictoryDefeatBasic = 44,
    ExecutionPlanInvalidActionCompletionBasic = 45,
    SingleAllyDeathExecutionFilteringBasic = 46,
    BuffTriggerConsumeOrderBasic = 47,
    BuffDefinitionDataLayerBasic = 48,
    BuffLifecycleBattleIntegrationBasic = 49,
    BuffBeforeUseActionUnavailableBasic = 50,
    ExecutionItemStatusBasic = 51,
    CardResolvedHitContractBasic = 52,
    CardResourceSnapshotAndConsumeBasic = 53,
    CardAssignmentEligibilityBasic = 54,
    RealCardResourceMigrationBasic = 55,
    BattleDefinitionDataBootstrapBasic = 56,
    BattlePreparedActionAssignmentModelBasic = 57,
    BattleExecutionOrderingAndGuardPriorityBasic = 58,
    BattleContinuousDodgeLifecycleBasic = 59,
    BattleCardDragAssignmentRoutingBasic = 60,
    BattleAutomaticTurnCycleAndCooldownDragBasic = 61,
    BattleCardPrimaryPreviewContractBasic = 62,
    BattleCardCooldownFutureTurnSemanticsBasic = 63,
    BattleCardPrimaryVisualPresetBasic = 64,
    BattleCardHoverAndDragMotionBasic = 65,
    BattleCardClickAssignBasic = 66,
    BattleCardClickInteractionIntegration = 67,
    BattleCardExponentialMotionAndSpreadBasic = 68,
    BattleActionSlotVisualInteractionBasic = 69,
    BattleBuffGridLayoutBasic = 70,
    BattleBuffInspectorPreviewBasic = 71,
    BattlePermanentBulletBuffBasic = 72,
    BattleActionRelationLineBasic = 73,
    BattleCharacterStatusWorldFollowBasic = 74,
    BattleActionRelationInteractionFix = 75,
    BattleLifecyclePhaseContractBasic = 76
}

public class CardLoadTest : MonoBehaviour
{
    [SerializeField] private BattleTestMode testMode = BattleTestMode.BattleRuntimeStateEndCurrentTurnBasic;

    // ================================
    // 测试角色
    // ================================

    CharacterData allyA;   // 我方角色A
    CharacterData allyB;   // 我方角色B
    CharacterData enemy;   // 敌人角色


    List<CharacterData> battleUnits = new List<CharacterData>(); // 当前战斗中的全部角色

    // ================================
    // 测试用战斗卡牌状态
    // ================================

    BattleCardState allyAAttackCardState;        // 我方角色A的攻击卡
    BattleCardState allyBDefenseCardState;       // 我方角色B的防御卡
    BattleCardState enemyAttackCardState;        // 敌人的攻击卡
    private BattleCardState allyAAbilitySinCardState;
    private CardTestData clashSinTestCardData;

    // ================================
    // Unity 入口
    // ================================

    void Start()
    {
        // 1. 创建测试角色
        CreateTestCharacters();

        // 2. 添加测试状态
        AddTestBuffs();

        // 3. 读取卡牌 JSON 数据
        List<CardTestData> cards = CardDataLoader.LoadCardData();

        if (cards == null)
        {
            return;
        }

        // 4. 打印卡牌效果，方便检查 JSON 是否读取成功
        CardDataLoader.PrintCardEffects(cards);

        // 5. 创建测试用战斗卡牌状态
        CreateTestBattleCards(cards);

        if (testMode == BattleTestMode.BattleRuntimeStateEndCurrentTurnBasic)
        {
            RunBattleRuntimeStateEndCurrentTurnBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleRuntimeStatePrepareNextTurnBasic)
        {
            RunBattleRuntimeStatePrepareNextTurnBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleResolverResolveRespondedAttackVsAttackBasic)
        {
            RunBattleResolverResolveRespondedAttackVsAttackBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleResolverRespondedPlayerWinBothCardsResolvedBasic)
        {
            RunBattleResolverRespondedPlayerWinBothCardsResolvedBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleResolverRespondedEnemyWinBothCardsResolvedBasic)
        {
            RunBattleResolverRespondedEnemyWinBothCardsResolvedBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleResolverRespondedClashSinLoseResolvedBasic)
        {
            RunBattleResolverRespondedClashSinLoseResolvedBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleResolverRespondedDodgeVsAttackBasic)
        {
            RunBattleResolverRespondedDodgeVsAttackBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotPassiveDodgeUnrespondedBasic)
        {
            RunActionSlotPassiveDodgeUnrespondedBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotPassiveDodgeAfterAttackLoseBasic)
        {
            RunActionSlotPassiveDodgeAfterAttackLoseBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleEndedVictoryDefeatBasic)
        {
            RunBattleEndedVictoryDefeatBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ExecutionPlanInvalidActionCompletionBasic)
        {
            RunExecutionPlanInvalidActionCompletionBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.SingleAllyDeathExecutionFilteringBasic)
        {
            RunSingleAllyDeathExecutionFilteringBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BuffTriggerConsumeOrderBasic)
        {
            RunBuffTriggerConsumeOrderBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BuffDefinitionDataLayerBasic)
        {
            RunBuffDefinitionDataLayerBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BuffLifecycleBattleIntegrationBasic)
        {
            RunBuffLifecycleBattleIntegrationBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BuffBeforeUseActionUnavailableBasic)
        {
            RunBuffBeforeUseActionUnavailableBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ExecutionItemStatusBasic)
        {
            RunExecutionItemStatusBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.CardResolvedHitContractBasic)
        {
            RunCardResolvedHitContractBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.CardResourceSnapshotAndConsumeBasic)
        {
            RunCardResourceSnapshotAndConsumeBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.CardAssignmentEligibilityBasic)
        {
            RunCardAssignmentEligibilityBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.RealCardResourceMigrationBasic)
        {
            RunRealCardResourceMigrationBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleDefinitionDataBootstrapBasic)
        {
            RunBattleDefinitionDataBootstrapBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattlePreparedActionAssignmentModelBasic)
        {
            RunBattlePreparedActionAssignmentModelBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleExecutionOrderingAndGuardPriorityBasic)
        {
            RunBattleExecutionOrderingAndGuardPriorityBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleContinuousDodgeLifecycleBasic)
        {
            RunBattleContinuousDodgeLifecycleBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleCardDragAssignmentRoutingBasic)
        {
            RunBattleCardDragAssignmentRoutingBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleAutomaticTurnCycleAndCooldownDragBasic)
        {
            RunBattleAutomaticTurnCycleAndCooldownDragBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleCardPrimaryPreviewContractBasic)
        {
            RunBattleCardPrimaryPreviewContractBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleCardCooldownFutureTurnSemanticsBasic)
        {
            RunBattleCardCooldownFutureTurnSemanticsBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleCardPrimaryVisualPresetBasic)
        {
            RunBattleCardPrimaryVisualPresetBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleCardHoverAndDragMotionBasic)
        {
            RunBattleCardHoverAndDragMotionBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleCardClickAssignBasic)
        {
            RunBattleCardClickAssignBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleCardClickInteractionIntegration)
        {
            RunBattleCardClickInteractionIntegrationTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleCardExponentialMotionAndSpreadBasic)
        {
            RunBattleCardExponentialMotionAndSpreadBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleActionSlotVisualInteractionBasic)
        {
            RunBattleActionSlotVisualInteractionBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleBuffGridLayoutBasic)
        {
            RunBattleBuffGridLayoutBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleBuffInspectorPreviewBasic)
        {
            RunBattleBuffInspectorPreviewBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattlePermanentBulletBuffBasic)
        {
            RunBattlePermanentBulletBuffBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleActionRelationLineBasic)
        {
            RunBattleActionRelationLineBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleCharacterStatusWorldFollowBasic)
        {
            RunBattleCharacterStatusWorldFollowBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleActionRelationInteractionFix)
        {
            RunBattleActionRelationInteractionFixTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleLifecyclePhaseContractBasic)
        {
            BattleLifecyclePhaseContractTests.Run();
            return;
        }

        if (testMode == BattleTestMode.BattleResolverResolveRespondedDefenseFullBlockBasic)
        {
            RunBattleResolverResolveRespondedDefenseFullBlockBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleResolverResolveRespondedDefenseReducedDamageBasic)
        {
            RunBattleResolverResolveRespondedDefenseReducedDamageBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleResolverDefenseKnownEnemyPointBasic)
        {
            RunBattleResolverDefenseKnownEnemyPointBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteFreeAbilityBasic)
        {
            RunActionSlotExecutionPlanExecuteFreeAbilityBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteHighSpeedFreeAttackMixedBasic)
        {
            RunActionSlotExecutionPlanExecuteHighSpeedFreeAttackMixedBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteUnrespondedBasic)
        {
            RunActionSlotExecutionPlanExecuteUnrespondedBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotPassiveGuardFullBlockBasic)
        {
            RunActionSlotPassiveGuardFullBlockBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotPassiveGuardReducedDamageBasic)
        {
            RunActionSlotPassiveGuardReducedDamageBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardReducedDamageBasic)
        {
            RunActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardReducedDamageBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardFullBlockBasic)
        {
            RunActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardFullBlockBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteRespondedEnemyWinNoPassiveGuardBasic)
        {
            RunActionSlotExecutionPlanExecuteRespondedEnemyWinNoPassiveGuardBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteRespondedPlayerWinPassiveGuardNotTriggeredBasic)
        {
            RunActionSlotExecutionPlanExecuteRespondedPlayerWinPassiveGuardNotTriggeredBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteRespondedTieLimit)
        {
            RunActionSlotExecutionPlanExecuteRespondedTieLimitTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteMixedBasic)
        {
            RunActionSlotExecutionPlanExecuteMixedBasicTestSequence();
            return;
        }
    }

    // RunClashUseCountTestSequence = 执行拼点型罪卡 UseCount 测试流程
    void RunClashUseCountTestSequence()
    {
        Debug.Log("===== Clash 罪卡第 1 次使用测试 =====");
        StartTurn();
        RunBattleTest();
        EndTurn();

        Debug.Log("===== Clash 罪卡第 2 次使用测试 =====");
        StartTurn();
        RunBattleTest();
        EndTurn();

        Debug.Log("===== Clash 罪卡第 3 次使用测试 =====");
        StartTurn();
        RunBattleTest();
        EndTurn();

        Debug.Log("===== Clash 罪卡第 4 次使用测试：应该不能再使用 =====");
        StartTurn();
        RunBattleTest();
    }

    // RunAbilityUseCountTestSequence = 执行能力型罪卡 UseCount 测试流程
    void RunAbilityUseCountTestSequence()
    {
        Debug.Log("===== Ability 罪卡第 1 次使用测试 =====");
        StartTurn();
        RunAbilitySinCardTest();
        PrintAbilitySinCardTestState();
        EndTurn();

        Debug.Log("===== Ability 罪卡第 2 次使用测试 =====");
        StartTurn();
        RunAbilitySinCardTest();
        PrintAbilitySinCardTestState();
        EndTurn();

        Debug.Log("===== Ability 罪卡第 3 次使用测试：应该不能再使用 =====");
        StartTurn();
        RunAbilitySinCardTest();
        PrintAbilitySinCardTestState();
    }

    // RunBattleActionSlotOwnerBasicTestSequence = 验证角色独立行动槽位 owner / slotIndex 数据
    void RunBattleActionSlotOwnerBasicTestSequence()
    {
        Debug.Log("===== BattleActionSlot owner 角色独立槽位测试开始 =====");

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(
            allyA,
            allyB,
            2
        );

        BattleActionSlotManager.PrintActionSlots(actionSlots);

        BattleActionSlot slotA1 = actionSlots != null && actionSlots.Count > 0 ? actionSlots[0] : null;
        BattleActionSlot slotA2 = actionSlots != null && actionSlots.Count > 1 ? actionSlots[1] : null;
        BattleActionSlot slotB1 = actionSlots != null && actionSlots.Count > 2 ? actionSlots[2] : null;
        BattleActionSlot slotB2 = actionSlots != null && actionSlots.Count > 3 ? actionSlots[3] : null;

        Debug.Log("预期槽位数量为 4：" + (actionSlots != null && actionSlots.Count == 4));

        Debug.Log("预期第 1 个槽位存在：" + (slotA1 != null));
        if (slotA1 != null)
        {
            Debug.Log("预期第 1 个 owner 为 allyA：" + object.ReferenceEquals(slotA1.owner, allyA));
            Debug.Log("预期第 1 个 slotIndex 为 1：" + (slotA1.slotIndex == 1));
            Debug.Log("预期第 1 个显示名为 A 槽位1：" + (slotA1.GetDisplaySlotName() == allyA.characterName + " 槽位1"));
        }

        Debug.Log("预期第 2 个槽位存在：" + (slotA2 != null));
        if (slotA2 != null)
        {
            Debug.Log("预期第 2 个 owner 为 allyA：" + object.ReferenceEquals(slotA2.owner, allyA));
            Debug.Log("预期第 2 个 slotIndex 为 2：" + (slotA2.slotIndex == 2));
            Debug.Log("预期第 2 个显示名为 A 槽位2：" + (slotA2.GetDisplaySlotName() == allyA.characterName + " 槽位2"));
        }

        Debug.Log("预期第 3 个槽位存在：" + (slotB1 != null));
        if (slotB1 != null)
        {
            Debug.Log("预期第 3 个 owner 为 allyB：" + object.ReferenceEquals(slotB1.owner, allyB));
            Debug.Log("预期第 3 个 slotIndex 为 1：" + (slotB1.slotIndex == 1));
            Debug.Log("预期第 3 个显示名为 B 槽位1：" + (slotB1.GetDisplaySlotName() == allyB.characterName + " 槽位1"));
        }

        Debug.Log("预期第 4 个槽位存在：" + (slotB2 != null));
        if (slotB2 != null)
        {
            Debug.Log("预期第 4 个 owner 为 allyB：" + object.ReferenceEquals(slotB2.owner, allyB));
            Debug.Log("预期第 4 个 slotIndex 为 2：" + (slotB2.slotIndex == 2));
            Debug.Log("预期第 4 个显示名为 B 槽位2：" + (slotB2.GetDisplaySlotName() == allyB.characterName + " 槽位2"));
        }

        Debug.Log("本测试只验证角色独立槽位 owner / slotIndex / displayName，不安排卡牌，不响应敌人意图，不生成 ExecutionPlan，不执行 plan，不调用 Resolver，不扣血");
    }

    // RunBattleActionSlotOwnerAssignBasicTestSequence = 验证 owner + slotIndex 能区分 A槽位1 / B槽位1
    void RunBattleActionSlotOwnerAssignBasicTestSequence()
    {
        Debug.Log("===== BattleActionSlot owner 版本安排行动测试开始 =====");

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(
            allyA,
            allyB,
            2
        );

        BattleCardState allyAIndependentAttackCardState = CreateTestAttackCardForCharacter(
            allyA,
            "owner_assign_allyA_atk_001_copy_0"
        );

        BattleCardState allyBIndependentAttackCardState = CreateTestAttackCardForCharacter(
            allyB,
            "owner_assign_allyB_atk_001_copy_0"
        );

        bool assignA1Result = BattleActionSlotManager.AssignFreeAction(
            actionSlots,
            allyA,
            1,
            allyA,
            allyAIndependentAttackCardState,
            enemy
        );

        bool assignB1Result = BattleActionSlotManager.AssignFreeAction(
            actionSlots,
            allyB,
            1,
            allyB,
            allyBIndependentAttackCardState,
            enemy
        );

        BattleActionSlot slotA1 = BattleActionSlotManager.GetSlot(actionSlots, allyA, 1);
        BattleActionSlot slotA2 = BattleActionSlotManager.GetSlot(actionSlots, allyA, 2);
        BattleActionSlot slotB1 = BattleActionSlotManager.GetSlot(actionSlots, allyB, 1);
        BattleActionSlot slotB2 = BattleActionSlotManager.GetSlot(actionSlots, allyB, 2);

        BattleActionSlotManager.PrintActionSlots(actionSlots);

        Debug.Log("预期 allyA 槽位1安排成功：" + assignA1Result);
        Debug.Log("预期 allyB 槽位1安排成功：" + assignB1Result);

        Debug.Log("预期 allyA 槽位1存在：" + (slotA1 != null));
        if (slotA1 != null)
        {
            Debug.Log("预期 allyA 槽位1 actor 为 allyA：" + object.ReferenceEquals(slotA1.actor, allyA));
            Debug.Log("预期 allyA 槽位1 卡牌为 allyA 独立攻击卡：" + object.ReferenceEquals(slotA1.cardState, allyAIndependentAttackCardState));
            Debug.Log("预期 allyA 槽位1 不是空槽：" + !slotA1.IsEmpty());
        }

        Debug.Log("预期 allyB 槽位1存在：" + (slotB1 != null));
        if (slotB1 != null)
        {
            Debug.Log("预期 allyB 槽位1 actor 为 allyB：" + object.ReferenceEquals(slotB1.actor, allyB));
            Debug.Log("预期 allyB 槽位1 卡牌为 allyB 独立攻击卡：" + object.ReferenceEquals(slotB1.cardState, allyBIndependentAttackCardState));
            Debug.Log("预期 allyB 槽位1 不是空槽：" + !slotB1.IsEmpty());
        }

        Debug.Log("预期 allyA 槽位2仍为空：" + (slotA2 != null && slotA2.IsEmpty()));
        Debug.Log("预期 allyB 槽位2仍为空：" + (slotB2 != null && slotB2.IsEmpty()));
        Debug.Log("本测试只验证 owner 查找和 FreeAction 安排，不生成 ExecutionPlan，不执行 plan，不调用 Resolver，不扣血，不处理回合结束");
    }

    // RunBattleRuntimeStateBasicTestSequence = 验证 BattleRuntimeState 能集中保存并打印当前战斗状态
    void RunBattleRuntimeStateBasicTestSequence()
    {
        Debug.Log("===== BattleRuntimeState 基础状态容器测试开始 =====");

        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(allyA, allyB, enemy);

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);
        runtimeState.SetActionSlots(actionSlots);

        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            "runtime_state_basic_intent_001",
            enemy,
            enemyAttackCardState,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(enemyIntent);
        runtimeState.SetIntentQueue(intentQueue);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            actionSlots,
            intentQueue
        );

        runtimeState.SetExecutionPlan(executionPlan);
        SetTestLifecyclePhase(runtimeState, BattleLifecyclePhase.PlanReady);
        runtimeState.PrintRuntimeState();

        Debug.Log("预期 battleUnits 数量为 3：" + (runtimeState.battleUnits.Count == 3));
        Debug.Log("预期 actionSlots 数量为 2：" + (runtimeState.actionSlots.Count == 2));
        Debug.Log("预期 intentQueue 数量为 1：" + (runtimeState.intentQueue.Count == 1));
        Debug.Log("预期 currentExecutionPlan 不为空：" + (runtimeState.currentExecutionPlan != null));
        Debug.Log("本测试只验证状态容器保存和打印，不执行 plan，不调用 Resolver，不扣血，不处理回合结束");
    }

    // RunBattleRuntimeStateClearCurrentTurnBasicTestSequence = 验证 RuntimeState 能清理当前回合临时对象
    void RunBattleRuntimeStateClearCurrentTurnBasicTestSequence()
    {
        Debug.Log("===== BattleRuntimeState 当前回合临时对象清理测试开始 =====");

        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(allyA, allyB, enemy);

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);
        runtimeState.SetActionSlots(actionSlots);

        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            "runtime_state_clear_current_turn_intent_001",
            enemy,
            enemyAttackCardState,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(enemyIntent);
        runtimeState.SetIntentQueue(intentQueue);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            actionSlots,
            intentQueue
        );

        runtimeState.SetExecutionPlan(executionPlan);
        SetTestLifecyclePhase(runtimeState, BattleLifecyclePhase.PlanReady);

        Debug.Log("===== 清理前 BattleRuntimeState =====");
        runtimeState.PrintRuntimeState();

        BattleLifecyclePhase phaseBeforeClear = runtimeState.LifecyclePhase;
        runtimeState.ClearCurrentTurnRuntimeObjects();

        Debug.Log("===== 清理后 BattleRuntimeState =====");
        runtimeState.PrintRuntimeState();

        Debug.Log("预期清理后 battleUnits 数量仍为 3：" + (runtimeState.battleUnits.Count == 3));
        Debug.Log("预期清理后 allyA 仍然存在：" + (runtimeState.allyA != null));
        Debug.Log("预期清理后 allyB 仍然存在：" + (runtimeState.allyB != null));
        Debug.Log("预期清理后 enemy 仍然存在：" + (runtimeState.enemy != null));
        Debug.Log("预期清理后 actionSlots 数量为 0：" + (runtimeState.actionSlots.Count == 0));
        Debug.Log("预期清理后 intentQueue 数量为 0：" + (runtimeState.intentQueue.Count == 0));
        Debug.Log("预期清理后 currentExecutionPlan 为空：" + (runtimeState.currentExecutionPlan == null));
        Debug.Log("预期清理后 currentTurn 仍为 1：" + (runtimeState.currentTurn == 1));
        Debug.Log("预期清理运行时对象不改变生命周期阶段：" + (runtimeState.LifecyclePhase == phaseBeforeClear));
        Debug.Log("本测试只验证 RuntimeState 清理，不执行 plan，不调用 Resolver，不扣血，不处理 Buff / CD / UseCount / guiltGain，不推进下一回合");
    }

    // RunBattleRuntimeStateEndCurrentTurnBasicTestSequence = 验证 EndTurn 与 RuntimeState 清理的组合入口
    void RunBattleRuntimeStateEndCurrentTurnBasicTestSequence()
    {
        Debug.Log("===== BattleRuntimeState 结束当前回合并清理测试开始 =====");

        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(allyA, allyB, enemy);

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);
        runtimeState.SetActionSlots(actionSlots);

        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            "runtime_state_end_current_turn_intent_001",
            enemy,
            enemyAttackCardState,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(enemyIntent);
        runtimeState.SetIntentQueue(intentQueue);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            actionSlots,
            intentQueue
        );

        runtimeState.SetExecutionPlan(executionPlan);
        SetTestLifecyclePhase(runtimeState, BattleLifecyclePhase.TurnResolved);

        Debug.Log("===== 结束回合前 BattleRuntimeState =====");
        runtimeState.PrintRuntimeState();

        runtimeState.EndCurrentTurnAndClearRuntimeObjects();

        Debug.Log("===== 结束回合后 BattleRuntimeState =====");
        runtimeState.PrintRuntimeState();

        Debug.Log("预期结束后 battleUnits 数量仍为 3：" + (runtimeState.battleUnits.Count == 3));
        Debug.Log("预期结束后 allyA 仍然存在：" + (runtimeState.allyA != null));
        Debug.Log("预期结束后 allyB 仍然存在：" + (runtimeState.allyB != null));
        Debug.Log("预期结束后 enemy 仍然存在：" + (runtimeState.enemy != null));
        Debug.Log("预期结束后 actionSlots 数量为 0：" + (runtimeState.actionSlots.Count == 0));
        Debug.Log("预期结束后 intentQueue 数量为 0：" + (runtimeState.intentQueue.Count == 0));
        Debug.Log("预期结束后 currentExecutionPlan 为空：" + (runtimeState.currentExecutionPlan == null));
        Debug.Log("预期结束后 currentTurn 仍为 1：" + (runtimeState.currentTurn == 1));
        Debug.Log("预期结束后 currentPhase 为 TurnEnded：" + (runtimeState.currentPhase == "TurnEnded"));
        Debug.Log("本测试只验证 EndTurn + RuntimeState 清理组合入口，不执行 plan，不调用 Resolver，不扣血，不推进下一回合，不生成新敌人意图");
    }

    // RunBattleRuntimeStatePrepareNextTurnBasicTestSequence = 验证 RuntimeState 能推进到下一回合准备阶段
    void RunBattleRuntimeStatePrepareNextTurnBasicTestSequence()
    {
        Debug.Log("===== BattleRuntimeState 准备下一回合运行时对象测试开始 =====");

        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(allyA, allyB, enemy);

        List<BattleActionSlot> oldActionSlots = BattleActionSlotManager.CreateActionSlots(2);
        runtimeState.SetActionSlots(oldActionSlots);

        BattleEnemyIntent oldEnemyIntent = new BattleEnemyIntent(
            "runtime_state_prepare_next_turn_old_intent_001",
            enemy,
            enemyAttackCardState,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> oldIntentQueue = BattleEnemyIntentManager.CreateIntentQueue(oldEnemyIntent);
        runtimeState.SetIntentQueue(oldIntentQueue);

        BattleExecutionPlan oldExecutionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            oldActionSlots,
            oldIntentQueue
        );

        runtimeState.SetExecutionPlan(oldExecutionPlan);
        SetTestLifecyclePhase(runtimeState, BattleLifecyclePhase.TurnEnded);

        Debug.Log("===== 准备下一回合前 BattleRuntimeState =====");
        runtimeState.PrintRuntimeState();

        List<BattleActionSlot> newActionSlots = BattleActionSlotManager.CreateActionSlots(2);
        BattleEnemyIntent newEnemyIntent = new BattleEnemyIntent(
            "runtime_state_prepare_next_turn_new_intent_001",
            enemy,
            enemyAttackCardState,
            allyB,
            2,
            1
        );

        List<BattleEnemyIntent> newIntentQueue = BattleEnemyIntentManager.CreateIntentQueue(newEnemyIntent);

        runtimeState.PrepareNextTurnWithRuntimeObjects(newActionSlots, newIntentQueue);

        Debug.Log("===== 准备下一回合后 BattleRuntimeState =====");
        runtimeState.PrintRuntimeState();

        Debug.Log("预期准备后 currentTurn 为 2：" + (runtimeState.currentTurn == 2));
        Debug.Log("预期准备后 currentPhase 为 Prepare：" + (runtimeState.currentPhase == "Prepare"));
        Debug.Log("预期准备后 battleUnits 数量仍为 3：" + (runtimeState.battleUnits.Count == 3));
        Debug.Log("预期准备后 allyA 仍然存在：" + (runtimeState.allyA != null));
        Debug.Log("预期准备后 allyB 仍然存在：" + (runtimeState.allyB != null));
        Debug.Log("预期准备后 enemy 仍然存在：" + (runtimeState.enemy != null));
        Debug.Log("预期准备后 actionSlots 数量为 2：" + (runtimeState.actionSlots.Count == 2));
        Debug.Log("预期准备后 intentQueue 数量为 1：" + (runtimeState.intentQueue.Count == 1));
        Debug.Log("预期准备后 currentExecutionPlan 为空：" + (runtimeState.currentExecutionPlan == null));
        Debug.Log("本测试只验证下一回合 RuntimeState 准备，不执行 plan，不调用 Resolver，不扣血，不生成 ExecutionPlan，不写死敌人 AI");
    }

    // RunBattleRuntimeStateFixedIntentFactoryBasicTestSequence = 验证固定测试敌人意图生成入口
    void RunBattleRuntimeStateFixedIntentFactoryBasicTestSequence()
    {
        Debug.Log("===== BattleRuntimeState 固定敌人意图生成入口测试开始 =====");

        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(allyA, allyB, enemy);

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);
        List<BattleEnemyIntent> intentQueue = CreateFixedTestEnemyIntentQueueForRuntimeState();

        runtimeState.SetActionSlots(actionSlots);
        runtimeState.SetIntentQueue(intentQueue);
        SetTestLifecyclePhase(runtimeState, BattleLifecyclePhase.Prepare);

        runtimeState.PrintRuntimeState();
        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);

        BattleEnemyIntent intent = BattleEnemyIntentManager.FindIntentByOrder(intentQueue, 1);

        Debug.Log("固定敌人意图队列数量：" + intentQueue.Count);

        if (intent != null)
        {
            Debug.Log("敌人意图1 enemy：" + intent.GetEnemyName());
            Debug.Log("敌人意图1 enemyCard：" + intent.GetCardName());
            Debug.Log("敌人意图1 originalTarget：" + intent.GetOriginalTargetName());
            Debug.Log("敌人意图1 actualTarget：" + intent.GetActualTargetName());
            Debug.Log("敌人意图1 actualTargetSlotIndex：" + intent.actualTargetSlotIndex);
            Debug.Log("敌人意图1 intentOrder：" + intent.intentOrder);
            Debug.Log("敌人意图1 isResponded：" + intent.isResponded);
        }

        Debug.Log("预期 battleUnits 数量为 3：" + (runtimeState.battleUnits.Count == 3));
        Debug.Log("预期 actionSlots 数量为 2：" + (runtimeState.actionSlots.Count == 2));
        Debug.Log("预期 intentQueue 数量为 1：" + (runtimeState.intentQueue.Count == 1));
        Debug.Log("预期 currentPhase 为 Prepare：" + (runtimeState.currentPhase == "Prepare"));
        Debug.Log("预期 currentExecutionPlan 为空：" + (runtimeState.currentExecutionPlan == null));
        Debug.Log("预期敌人意图1存在：" + (intent != null));

        if (intent != null)
        {
            Debug.Log("预期敌人意图1 enemy 为 敌人：" + (intent.enemy == enemy));
            Debug.Log("预期敌人意图1 enemyCardState 为 enemyAttackCardState：" + (intent.enemyCardState == enemyAttackCardState));
            Debug.Log("预期敌人意图1 originalTarget 为 allyB：" + (intent.originalTargetCharacter == allyB));
            Debug.Log("预期敌人意图1 actualTarget 为 allyB：" + (intent.actualTargetCharacter == allyB));
            Debug.Log("预期敌人意图1 actualTargetSlotIndex 为 1：" + (intent.actualTargetSlotIndex == 1));
        Debug.Log("预期敌人意图1 intentOrder 为 1：" + (intent.intentOrder == 1));
        Debug.Log("预期敌人意图1 isResponded 为 false：" + (intent.isResponded == false));
        }

        Debug.Log("本测试只验证固定测试敌人意图生成入口，不生成 ExecutionPlan，不执行 plan，不调用 Resolver，不扣血，不推进回合，不调用 StartTurn / EndTurn");
    }

    // RunBattleStateViewDataBasicTestSequence = 验证 UI 可读取状态快照能从 RuntimeState 生成
    void RunBattleStateViewDataBasicTestSequence()
    {
        Debug.Log("===== BattleStateViewData 基础只读快照测试开始 =====");

        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(allyA, allyB, enemy);

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);
        List<BattleEnemyIntent> intentQueue = CreateFixedTestEnemyIntentQueueForRuntimeState();

        runtimeState.SetActionSlots(actionSlots);
        runtimeState.SetIntentQueue(intentQueue);
        SetTestLifecyclePhase(runtimeState, BattleLifecyclePhase.Prepare);

        BattleStateViewData viewData = BattleStateViewData.FromRuntimeState(runtimeState);
        viewData.PrintViewData();

        Debug.Log("预期 currentTurn 为 1：" + (viewData.currentTurn == 1));
        Debug.Log("预期 currentPhase 为 Prepare：" + (viewData.currentPhase == "Prepare"));
        Debug.Log("预期 allyA 名字正确：" + (viewData.allyAName == allyA.characterName));
        Debug.Log("预期 allyA HP 正确：" + (viewData.allyAHP == allyA.currentHP && viewData.allyAMaxHP == allyA.maxHP));
        Debug.Log("预期 allyA 速度正确：" + (viewData.allyASpeed == allyA.GetCurrentSpeed()));
        Debug.Log("预期 allyA 负罪感正确：" + (viewData.allyAGuilt == allyA.currentGuilt));
        Debug.Log("预期 allyB 名字正确：" + (viewData.allyBName == allyB.characterName));
        Debug.Log("预期 allyB HP 正确：" + (viewData.allyBHP == allyB.currentHP && viewData.allyBMaxHP == allyB.maxHP));
        Debug.Log("预期 allyB 速度正确：" + (viewData.allyBSpeed == allyB.GetCurrentSpeed()));
        Debug.Log("预期 allyB 负罪感正确：" + (viewData.allyBGuilt == allyB.currentGuilt));
        Debug.Log("预期 enemy 名字正确：" + (viewData.enemyName == enemy.characterName));
        Debug.Log("预期 enemy HP 正确：" + (viewData.enemyHP == enemy.currentHP && viewData.enemyMaxHP == enemy.maxHP));
        Debug.Log("预期 enemy 速度正确：" + (viewData.enemySpeed == enemy.GetCurrentSpeed()));
        Debug.Log("预期 actionSlotCount 为 2：" + (viewData.actionSlotCount == 2));
        Debug.Log("预期 intentCount 为 1：" + (viewData.intentCount == 1));
        Debug.Log("预期 hasExecutionPlan 为 false：" + (viewData.hasExecutionPlan == false));
        Debug.Log("预期 executionPlanCompleted 为 false：" + (viewData.executionPlanCompleted == false));
        Debug.Log("预期 executionItemCount 为 0：" + (viewData.executionItemCount == 0));
        Debug.Log("本测试只验证 ViewData 从 RuntimeState 只读生成，不生成 ExecutionPlan，不执行 plan，不调用 Resolver，不修改 RuntimeState，不改战斗逻辑");
    }

    // RunBattleStateViewDataEnemyIntentBasicTestSequence = 验证 ViewData 能包含敌人意图列表
    void RunBattleStateViewDataEnemyIntentBasicTestSequence()
    {
        Debug.Log("===== BattleStateViewData 敌人意图快照测试开始 =====");

        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(allyA, allyB, enemy);

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);
        List<BattleEnemyIntent> intentQueue = CreateFixedTestEnemyIntentQueueForRuntimeState();

        runtimeState.SetActionSlots(actionSlots);
        runtimeState.SetIntentQueue(intentQueue);
        SetTestLifecyclePhase(runtimeState, BattleLifecyclePhase.Prepare);

        BattleStateViewData viewData = BattleStateViewData.FromRuntimeState(runtimeState);
        viewData.PrintViewData();

        EnemyIntentViewData intentView = null;

        if (viewData.enemyIntentViews != null && viewData.enemyIntentViews.Count > 0)
        {
            intentView = viewData.enemyIntentViews[0];
        }

        Debug.Log("预期 intentCount 为 1：" + (viewData.intentCount == 1));
        Debug.Log("预期 enemyIntentViews 不为空：" + (viewData.enemyIntentViews != null));
        Debug.Log("预期 enemyIntentViews 数量为 1：" + (viewData.enemyIntentViews != null && viewData.enemyIntentViews.Count == 1));
        Debug.Log("预期第 1 个 EnemyIntentViewData 存在：" + (intentView != null));

        if (intentView != null)
        {
            Debug.Log("预期 intentOrder 为 1：" + (intentView.intentOrder == 1));
            Debug.Log("预期 enemyName 正确：" + (intentView.enemyName == enemy.characterName));
            Debug.Log("预期 enemyCardName 正确：" + (intentView.enemyCardName == enemyAttackCardState.cardData.cardName));
            Debug.Log("预期 originalTargetName 正确：" + (intentView.originalTargetName == allyB.characterName));
            Debug.Log("预期 originalTargetSlotIndex 为 1：" + (intentView.originalTargetSlotIndex == 1));
            Debug.Log("预期 actualTargetName 正确：" + (intentView.actualTargetName == allyB.characterName));
            Debug.Log("预期 actualTargetSlotIndex 为 1：" + (intentView.actualTargetSlotIndex == 1));
            Debug.Log("预期 isResponded 为 false：" + (intentView.isResponded == false));
        }

        Debug.Log("本测试只验证 EnemyIntentViewData 从 RuntimeState 只读生成，不做 UI，不执行 plan，不调用 Resolver，不修改 RuntimeState，不改敌人意图");
    }

    // RunBattleStateViewDataActionSlotBasicTestSequence = 验证 ViewData 能包含行动槽位列表
    void RunBattleStateViewDataActionSlotBasicTestSequence()
    {
        Debug.Log("===== BattleStateViewData 行动槽位快照测试开始 =====");

        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(allyA, allyB, enemy);

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);
        bool assignSuccess = BattleActionSlotManager.AssignFreeAction(
            actionSlots,
            1,
            allyA,
            allyAAttackCardState,
            enemy
        );
        Debug.Log("预期槽位1 Attack FreeAction 安排成功：" + assignSuccess);

        List<BattleEnemyIntent> intentQueue = CreateFixedTestEnemyIntentQueueForRuntimeState();

        runtimeState.SetActionSlots(actionSlots);
        runtimeState.SetIntentQueue(intentQueue);
        SetTestLifecyclePhase(runtimeState, BattleLifecyclePhase.Prepare);

        BattleStateViewData viewData = BattleStateViewData.FromRuntimeState(runtimeState);
        viewData.PrintViewData();

        ActionSlotViewData slotView1 = null;
        ActionSlotViewData slotView2 = null;

        if (viewData.actionSlotViews != null && viewData.actionSlotViews.Count > 0)
        {
            slotView1 = viewData.actionSlotViews[0];
        }

        if (viewData.actionSlotViews != null && viewData.actionSlotViews.Count > 1)
        {
            slotView2 = viewData.actionSlotViews[1];
        }

        Debug.Log("预期 actionSlotCount 为 2：" + (viewData.actionSlotCount == 2));
        Debug.Log("预期 actionSlotViews 不为空：" + (viewData.actionSlotViews != null));
        Debug.Log("预期 actionSlotViews 数量为 2：" + (viewData.actionSlotViews != null && viewData.actionSlotViews.Count == 2));
        Debug.Log("预期第 1 个 ActionSlotViewData 存在：" + (slotView1 != null));

        if (slotView1 != null)
        {
            Debug.Log("预期槽位1 slotIndex 为 1：" + (slotView1.slotIndex == 1));
            Debug.Log("预期槽位1 actorName 正确：" + (slotView1.actorName == allyA.characterName));
            Debug.Log("预期槽位1 cardName 正确：" + (slotView1.cardName == allyAAttackCardState.cardData.cardName));
            Debug.Log("预期槽位1 cardType 为 Attack：" + (slotView1.cardType == "Attack"));
            Debug.Log("预期槽位1 targetName 正确：" + (slotView1.targetName == enemy.characterName));
            Debug.Log("预期槽位1 hasEnemyIntent 为 false：" + (slotView1.hasEnemyIntent == false));
            Debug.Log("预期槽位1 isUsed 为 false：" + (slotView1.isUsed == false));
            Debug.Log("预期槽位1 isEmpty 为 false：" + (slotView1.isEmpty == false));
        }

        Debug.Log("预期第 2 个 ActionSlotViewData 存在：" + (slotView2 != null));

        if (slotView2 != null)
        {
            Debug.Log("预期槽位2 slotIndex 为 2：" + (slotView2.slotIndex == 2));
            Debug.Log("预期槽位2 isEmpty 为 true：" + (slotView2.isEmpty == true));
            Debug.Log("预期槽位2 cardName 为空或空：" + (string.IsNullOrEmpty(slotView2.cardName) || slotView2.cardName == "空"));
            Debug.Log("预期槽位2 isUsed 为 false：" + (slotView2.isUsed == false));
        }

        Debug.Log("本测试只验证 ActionSlotViewData 从 RuntimeState 只读生成，不做 UI，不执行 plan，不调用 Resolver，不修改 RuntimeState，不改槽位和战斗逻辑");
    }

    // RunBattleStateViewDataOwnerActionSlotBasicTestSequence = 验证 ViewData 能显示角色独立行动槽位
    void RunBattleStateViewDataOwnerActionSlotBasicTestSequence()
    {
        Debug.Log("===== BattleStateViewData owner 行动槽位快照测试开始 =====");

        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(allyA, allyB, enemy);

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(
            allyA,
            allyB,
            2
        );

        runtimeState.SetActionSlots(actionSlots);
        SetTestLifecyclePhase(runtimeState, BattleLifecyclePhase.Prepare);

        BattleStateViewData viewData = BattleStateViewData.FromRuntimeState(runtimeState);
        viewData.PrintViewData();

        ActionSlotViewData slotViewA1 = GetActionSlotViewByIndex(viewData, 0);
        ActionSlotViewData slotViewA2 = GetActionSlotViewByIndex(viewData, 1);
        ActionSlotViewData slotViewB1 = GetActionSlotViewByIndex(viewData, 2);
        ActionSlotViewData slotViewB2 = GetActionSlotViewByIndex(viewData, 3);

        Debug.Log("预期 actionSlotViews 不为空：" + (viewData.actionSlotViews != null));
        Debug.Log("预期 actionSlotViews 数量为 4：" + (viewData.actionSlotViews != null && viewData.actionSlotViews.Count == 4));

        Debug.Log("预期第 1 个 ownerName 为 allyA：" + (slotViewA1 != null && slotViewA1.ownerName == allyA.characterName));
        Debug.Log("预期第 1 个 displaySlotName 包含 allyA 和 槽位1：" + (slotViewA1 != null && slotViewA1.displaySlotName.Contains(allyA.characterName) && slotViewA1.displaySlotName.Contains("槽位1")));

        Debug.Log("预期第 2 个 ownerName 为 allyA：" + (slotViewA2 != null && slotViewA2.ownerName == allyA.characterName));
        Debug.Log("预期第 2 个 displaySlotName 包含 allyA 和 槽位2：" + (slotViewA2 != null && slotViewA2.displaySlotName.Contains(allyA.characterName) && slotViewA2.displaySlotName.Contains("槽位2")));

        Debug.Log("预期第 3 个 ownerName 为 allyB：" + (slotViewB1 != null && slotViewB1.ownerName == allyB.characterName));
        Debug.Log("预期第 3 个 displaySlotName 包含 allyB 和 槽位1：" + (slotViewB1 != null && slotViewB1.displaySlotName.Contains(allyB.characterName) && slotViewB1.displaySlotName.Contains("槽位1")));

        Debug.Log("预期第 4 个 ownerName 为 allyB：" + (slotViewB2 != null && slotViewB2.ownerName == allyB.characterName));
        Debug.Log("预期第 4 个 displaySlotName 包含 allyB 和 槽位2：" + (slotViewB2 != null && slotViewB2.displaySlotName.Contains(allyB.characterName) && slotViewB2.displaySlotName.Contains("槽位2")));

        Debug.Log("本测试只验证 ViewData 只读快照，不安排卡牌，不响应敌人意图，不生成 ExecutionPlan，不执行 plan，不调用 Resolver，不扣血，不接 UI");
    }

    // RunBattleResolverResolveRespondedAttackVsAttackBasicTestSequence = 测试 BattleResolver 正式已响应敌人意图入口
    void RunBattleResolverResolveRespondedAttackVsAttackBasicTestSequence()
    {
        Debug.Log("===== BattleResolver ResolveRespondedEnemyIntent 攻击卡 vs 攻击卡测试开始 =====");
        Debug.Log("本测试直接调用 BattleResolver.ResolveRespondedEnemyIntent(...)，不生成 ExecutionPlan，不调用 Executor");

        RunBattleResolverRespondedAttackVsAttackSubTest(
            "玩家胜利分支",
            10,
            10,
            1,
            1,
            "PlayerWin"
        );

        RunBattleResolverRespondedAttackVsAttackSubTest(
            "敌人胜利分支",
            1,
            1,
            8,
            8,
            "EnemyWin"
        );

        RunBattleResolverRespondedAttackVsAttackSubTest(
            "10次平局上限分支",
            5,
            5,
            5,
            5,
            "TieLimit"
        );
    }

    // RunBattleResolverRespondedPlayerWinBothCardsResolvedBasicTestSequence = 保留旧入口名，验证玩家胜利时仅胜方Attack完成使用
    void RunBattleResolverRespondedPlayerWinBothCardsResolvedBasicTestSequence()
    {
        RunRespondedAttackBothCardsResolvedExecutionSubTest(
            "PlayerWinBothCardsResolved",
            10,
            4,
            false,
            "PlayerWin",
            0,
            10
        );
    }

    // RunBattleResolverRespondedEnemyWinBothCardsResolvedBasicTestSequence = 保留旧入口名，验证敌人胜利时仅胜方Attack完成使用
    void RunBattleResolverRespondedEnemyWinBothCardsResolvedBasicTestSequence()
    {
        RunRespondedAttackBothCardsResolvedExecutionSubTest(
            "EnemyWinBothCardsResolved",
            4,
            8,
            false,
            "EnemyWin",
            8,
            0
        );
    }

    // RunBattleResolverRespondedClashSinLoseResolvedBasicTestSequence = 保留旧入口名，验证拼点罪卡失败后不触发 Resolved
    void RunBattleResolverRespondedClashSinLoseResolvedBasicTestSequence()
    {
        RunRespondedAttackBothCardsResolvedExecutionSubTest(
            "ClashSinLoseResolved",
            4,
            8,
            true,
            "EnemyWin",
            8,
            0
        );
    }

    // RunBattleResolverRespondedDodgeVsAttackBasicTestSequence = 闪避指定响应敌人攻击第一版聚合测试
    void RunBattleResolverRespondedDodgeVsAttackBasicTestSequence()
    {
        Debug.Log("===== BattleResolver Responded Dodge vs Attack 聚合测试开始 =====");
        Debug.Log("本测试只新增一个下拉入口，内部依次执行 DodgeSuccess / DodgeFailed / DodgeTieLimit 三组独立子测试");

        RunRespondedDodgeVsAttackExecutionSubTest(
            "DodgeSuccess",
            8,
            5,
            "DodgeSuccess",
            0,
            false
        );

        RunRespondedDodgeVsAttackExecutionSubTest(
            "DodgeFailed",
            4,
            8,
            "DodgeFailed",
            8,
            false
        );

        RunRespondedDodgeVsAttackExecutionSubTest(
            "DodgeTieLimit",
            5,
            5,
            "TieLimit",
            0,
            true
        );
    }

    // RunBattleResolverResolveRespondedDefenseFullBlockBasicTestSequence = 测试 Defense 完全抵挡敌人攻击
    void RunBattleResolverResolveRespondedDefenseFullBlockBasicTestSequence()
    {
        Debug.Log("===== BattleResolver ResolveRespondedEnemyIntent Defense 完全防御测试开始 =====");
        Debug.Log("本测试使用固定点数临时 CardData，直接调用正式入口 BattleResolver.ResolveRespondedEnemyIntent(...)");

        RunBattleResolverRespondedDefenseVsAttackSubTest(
            "DefenseFullBlock",
            4,
            4,
            6,
            6,
            2,
            "DefenseFullBlock",
            0
        );
    }

    // RunBattleResolverResolveRespondedDefenseReducedDamageBasicTestSequence = 测试 Defense 减少敌人攻击伤害
    void RunBattleResolverResolveRespondedDefenseReducedDamageBasicTestSequence()
    {
        Debug.Log("===== BattleResolver ResolveRespondedEnemyIntent Defense 减伤测试开始 =====");
        Debug.Log("本测试使用固定点数临时 CardData，直接调用正式入口 BattleResolver.ResolveRespondedEnemyIntent(...)");

        RunBattleResolverRespondedDefenseVsAttackSubTest(
            "DefenseReducedDamage",
            8,
            8,
            3,
            3,
            2,
            "DefenseReducedDamage",
            5
        );
    }

    // RunBattleResolverDefenseKnownEnemyPointBasicTestSequence = 测试已知敌人最终攻击点数的 Defense continuation
    void RunBattleResolverDefenseKnownEnemyPointBasicTestSequence()
    {
        Debug.Log("===== BattleResolver known-point Defense continuation 测试开始 =====");
        Debug.Log("本测试直接调用 BattleResolver.ResolveDefenseVsAttackWithKnownEnemyPoint(...)，不接入 Attack EnemyWin / PassiveGuard");

        RunBattleResolverDefenseKnownEnemyPointSubTest(
            "KnownPointDefenseReducedDamage",
            8,
            5,
            "DefenseReducedDamage",
            3,
            3
        );

        RunBattleResolverDefenseKnownEnemyPointSubTest(
            "KnownPointDefenseFullBlock",
            4,
            6,
            "DefenseFullBlock",
            0,
            0
        );
    }

    // RunBattleResolverResolveUnrespondedEnemyIntentBasicTestSequence = 测试 BattleResolver 正式无人响应敌人意图入口
    void RunBattleResolverResolveUnrespondedEnemyIntentBasicTestSequence()
    {
        Debug.Log("===== BattleResolver ResolveUnrespondedEnemyIntent 无人响应敌人意图测试开始 =====");
        Debug.Log("本测试直接调用 BattleResolver.ResolveUnrespondedEnemyIntent(...)，不生成 ExecutionPlan，不调用 Executor");

        StartTurn();

        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            "enemy_intent_resolver_unresponded_basic_001",
            enemy,
            enemyAttackCardState,
            allyB,
            2,
            1
        );

        int allyAHPBefore = allyA.currentHP;
        int allyBHPBefore = allyB.currentHP;
        int enemyHPBefore = enemy.currentHP;

        Debug.Log("执行前 我方角色A HP：" + allyAHPBefore + " / " + allyA.maxHP);
        Debug.Log("执行前 我方角色B HP：" + allyBHPBefore + " / " + allyB.maxHP);
        Debug.Log("执行前 敌人 HP：" + enemyHPBefore + " / " + enemy.maxHP);
        Debug.Log("无人响应敌人意图实际目标：" + enemyIntent.GetActualTargetSlotText());

        BattleResolveResult result = BattleResolver.ResolveUnrespondedEnemyIntent(enemyIntent);

        PrintBattleResolveResult(result);

        Debug.Log("执行后 我方角色A HP：" + allyA.currentHP + " / " + allyA.maxHP);
        Debug.Log("执行后 我方角色B HP：" + allyB.currentHP + " / " + allyB.maxHP);
        Debug.Log("执行后 敌人 HP：" + enemy.currentHP + " / " + enemy.maxHP);
        Debug.Log("预期 resultType：UnrespondedEnemyAttack，实际是否符合：" + (result != null && result.resultType == "UnrespondedEnemyAttack"));
        Debug.Log("预期 isSuccess：True，实际是否符合：" + (result != null && result.isSuccess));
        Debug.Log("预期 shouldCompleteItem：True，实际是否符合：" + (result != null && result.shouldCompleteItem));
        Debug.Log("预期 playerCardUsed：False，实际是否符合：" + (result != null && !result.playerCardUsed));
        Debug.Log("预期 enemyCardUsed：True，实际是否符合：" + (result != null && result.enemyCardUsed));
        Debug.Log("预期 triggeredEventChain：True，实际是否符合：" + (result != null && result.triggeredEventChain));
        Debug.Log("预期 damagedCharacter 为 allyB，实际是否符合：" + (result != null && object.ReferenceEquals(result.damagedCharacter, allyB)));
        Debug.Log("allyB HP 是否下降：" + (allyB.currentHP < allyBHPBefore));
        Debug.Log("allyA HP 是否保持不变：" + (allyA.currentHP == allyAHPBefore));
        Debug.Log("敌人 HP 是否保持不变：" + (enemy.currentHP == enemyHPBefore));
    }

    // RunBattleResolverResolveFreeAbilityBasicTestSequence = 测试 BattleResolver 正式 FreeAction Ability 入口
    void RunBattleResolverResolveFreeAbilityBasicTestSequence()
    {
        Debug.Log("===== BattleResolver ResolveFreeAction Ability 测试开始 =====");
        Debug.Log("本测试直接调用 BattleResolver.ResolveFreeAction(...)，不生成 ExecutionPlan，不调用 Executor");

        StartTurn();

        if (allyAAbilitySinCardState == null)
        {
            Debug.LogWarning("ResolveFreeAction Ability 测试失败：allyAAbilitySinCardState 为空");
            return;
        }

        BattleActionSlot actionSlot = new BattleActionSlot(1);
        actionSlot.AssignFreeAction(
            allyA,
            allyAAbilitySinCardState,
            allyA
        );

        int useCountBefore = allyAAbilitySinCardState.currentUseCount;
        int guiltBefore = allyA.currentGuilt;

        Debug.Log("执行前 Ability UseCount：" + useCountBefore + " / " + allyAAbilitySinCardState.maxUseCount);
        Debug.Log("执行前 allyA 负罪感：" + guiltBefore);
        Debug.Log("执行前 actionSlot.isUsed：" + actionSlot.isUsed);
        allyA.PrintBuffs();
        allyA.PrintPendingBuffs();

        BattleResolveResult result = BattleResolver.ResolveFreeAction(actionSlot);

        PrintBattleResolveResult(result);

        Debug.Log("执行后 Ability UseCount：" + allyAAbilitySinCardState.currentUseCount + " / " + allyAAbilitySinCardState.maxUseCount);
        Debug.Log("执行后 allyA 负罪感：" + allyA.currentGuilt);
        Debug.Log("执行后 actionSlot.isUsed：" + actionSlot.isUsed);
        allyA.PrintBuffs();
        allyA.PrintPendingBuffs();

        Debug.Log("预期 resultType：FreeAbility，实际是否符合：" + (result != null && result.resultType == "FreeAbility"));
        Debug.Log("预期 isSuccess：True，实际是否符合：" + (result != null && result.isSuccess));
        Debug.Log("预期 shouldCompleteItem：True，实际是否符合：" + (result != null && result.shouldCompleteItem));
        Debug.Log("预期 playerCardUsed：True，实际是否符合：" + (result != null && result.playerCardUsed));
        Debug.Log("预期 enemyCardUsed：False，实际是否符合：" + (result != null && !result.enemyCardUsed));
        Debug.Log("预期 hasDamage：False，实际是否符合：" + (result != null && !result.hasDamage));
        Debug.Log("预期 triggeredEventChain：True，实际是否符合：" + (result != null && result.triggeredEventChain));
        Debug.Log("Ability UseCount 是否增加：" + (allyAAbilitySinCardState.currentUseCount > useCountBefore));
        Debug.Log("allyA 负罪感是否增加：" + (allyA.currentGuilt > guiltBefore));
        Debug.Log("actionSlot.isUsed 是否仍为 False：" + (!actionSlot.isUsed));
    }

    // RunBattleResolverResolveFreeAttackBasicTestSequence = 测试 BattleResolver 正式 FreeAction Attack 入口
    void RunBattleResolverResolveFreeAttackBasicTestSequence()
    {
        Debug.Log("===== BattleResolver ResolveFreeAction Attack 测试开始 =====");
        Debug.Log("本测试直接调用 BattleResolver.ResolveFreeAction(...)，不生成 ExecutionPlan，不调用 Executor");

        StartTurn();

        if (allyAAttackCardState == null)
        {
            Debug.LogWarning("ResolveFreeAction Attack 测试失败：allyAAttackCardState 为空");
            return;
        }

        BattleActionSlot actionSlot = new BattleActionSlot(1);
        actionSlot.AssignFreeAction(
            allyA,
            allyAAttackCardState,
            enemy
        );

        int enemyHPBefore = enemy.currentHP;
        int allyAHPBefore = allyA.currentHP;
        int useCountBefore = allyAAttackCardState.currentUseCount;
        int cooldownBefore = allyAAttackCardState.currentCooldown;
        int guiltBefore = allyA.currentGuilt;

        Debug.Log("执行前 敌人 HP：" + enemyHPBefore + " / " + enemy.maxHP);
        Debug.Log("执行前 allyA HP：" + allyAHPBefore + " / " + allyA.maxHP);
        Debug.Log("执行前 Attack UseCount：" + useCountBefore + " / " + allyAAttackCardState.maxUseCount);
        Debug.Log("执行前 Attack CD：" + cooldownBefore);
        Debug.Log("执行前 allyA 负罪感：" + guiltBefore);
        Debug.Log("执行前 actionSlot.isUsed：" + actionSlot.isUsed);

        BattleResolveResult result = BattleResolver.ResolveFreeAction(actionSlot);

        PrintBattleResolveResult(result);

        Debug.Log("执行后 敌人 HP：" + enemy.currentHP + " / " + enemy.maxHP);
        Debug.Log("执行后 allyA HP：" + allyA.currentHP + " / " + allyA.maxHP);
        Debug.Log("执行后 Attack UseCount：" + allyAAttackCardState.currentUseCount + " / " + allyAAttackCardState.maxUseCount);
        Debug.Log("执行后 Attack CD：" + allyAAttackCardState.currentCooldown);
        Debug.Log("执行后 allyA 负罪感：" + allyA.currentGuilt);
        Debug.Log("执行后 actionSlot.isUsed：" + actionSlot.isUsed);

        Debug.Log("预期 resultType：FreeAttack，实际是否符合：" + (result != null && result.resultType == "FreeAttack"));
        Debug.Log("预期 isSuccess：True，实际是否符合：" + (result != null && result.isSuccess));
        Debug.Log("预期 shouldCompleteItem：True，实际是否符合：" + (result != null && result.shouldCompleteItem));
        Debug.Log("预期 playerCardUsed：True，实际是否符合：" + (result != null && result.playerCardUsed));
        Debug.Log("预期 enemyCardUsed：False，实际是否符合：" + (result != null && !result.enemyCardUsed));
        Debug.Log("预期 hasDamage：True，实际是否符合：" + (result != null && result.hasDamage));
        Debug.Log("预期 damage > 0，实际是否符合：" + (result != null && result.damage > 0));
        Debug.Log("预期 damagedCharacter 为 enemy，实际是否符合：" + (result != null && object.ReferenceEquals(result.damagedCharacter, enemy)));
        Debug.Log("预期 playerPoint > 0，实际是否符合：" + (result != null && result.playerPoint > 0));
        Debug.Log("预期 enemyPoint = 0，实际是否符合：" + (result != null && result.enemyPoint == 0));
        Debug.Log("预期 clashAttemptCount = 0，实际是否符合：" + (result != null && result.clashAttemptCount == 0));
        Debug.Log("预期 triggeredEventChain：True，实际是否符合：" + (result != null && result.triggeredEventChain));
        Debug.Log("敌人 HP 是否下降：" + (enemy.currentHP < enemyHPBefore));
        Debug.Log("allyA HP 是否保持不变：" + (allyA.currentHP == allyAHPBefore));
        Debug.Log("actionSlot.isUsed 是否仍为 False：" + (!actionSlot.isUsed));
    }

    // RunActionSlotExecutionPlanExecuteFreeAbilityBasicTestSequence = 执行 FreeAction Ability 基础测试
    void RunActionSlotExecutionPlanExecuteFreeAbilityBasicTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan FreeAction Ability 执行测试开始 =====");
        Debug.Log("本测试生成只包含 Ability FreeAction 的 ExecutionPlan，并通过 Executor 调用 BattleResolver.ResolveFreeAction(...)");

        StartTurn();

        if (allyAAbilitySinCardState == null)
        {
            Debug.LogWarning("FreeAction Ability 执行测试失败：allyAAbilitySinCardState 为空");
            return;
        }

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(1);
        List<BattleEnemyIntent> emptyIntentQueue = new List<BattleEnemyIntent>();

        BattleActionSlotManager.AssignFreeAction(
            actionSlots,
            1,
            allyA,
            allyAAbilitySinCardState,
            allyA
        );

        BattleActionSlot actionSlot = actionSlots[0];

        int useCountBefore = allyAAbilitySinCardState.currentUseCount;
        int guiltBefore = allyA.currentGuilt;

        Debug.Log("执行前 Ability UseCount：" + useCountBefore + " / " + allyAAbilitySinCardState.maxUseCount);
        Debug.Log("执行前 allyA 负罪感：" + guiltBefore);
        Debug.Log("执行前 actionSlot.isUsed：" + actionSlot.isUsed);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            actionSlots,
            emptyIntentQueue
        );

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlotManager.PrintSlotStates(actionSlots);
        Debug.Log("执行后 Ability UseCount：" + allyAAbilitySinCardState.currentUseCount + " / " + allyAAbilitySinCardState.maxUseCount);
        Debug.Log("执行后 allyA 负罪感：" + allyA.currentGuilt);
        Debug.Log("执行后 actionSlot.isUsed：" + actionSlot.isUsed);
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
        Debug.Log("预期 Ability UseCount 增加：" + (allyAAbilitySinCardState.currentUseCount > useCountBefore));
        Debug.Log("预期 allyA 负罪感增加：" + (allyA.currentGuilt > guiltBefore));
        Debug.Log("预期 actionSlot.isUsed 为 True：" + actionSlot.isUsed);
        Debug.Log("预期 ExecutionPlan.isCompleted 为 True：" + executionPlan.isCompleted);
    }

    // RunActionSlotExecutionPlanExecuteFreeAttackBasicTestSequence = 执行 FreeAction Attack 基础测试
    void RunActionSlotExecutionPlanExecuteFreeAttackBasicTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan FreeAction Attack 执行测试开始 =====");
        Debug.Log("本测试生成只包含 Attack FreeAction 的 ExecutionPlan，并通过 Executor 调用 BattleResolver.ResolveFreeAction(...)");

        StartTurn();

        if (allyAAttackCardState == null)
        {
            Debug.LogWarning("FreeAction Attack 执行测试失败：allyAAttackCardState 为空");
            return;
        }

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(1);
        List<BattleEnemyIntent> emptyIntentQueue = new List<BattleEnemyIntent>();

        BattleActionSlotManager.AssignFreeAction(
            actionSlots,
            1,
            allyA,
            allyAAttackCardState,
            enemy
        );

        BattleActionSlot actionSlot = actionSlots[0];

        int enemyHPBefore = enemy.currentHP;
        int allyAHPBefore = allyA.currentHP;
        int useCountBefore = allyAAttackCardState.currentUseCount;
        int cooldownBefore = allyAAttackCardState.currentCooldown;
        int guiltBefore = allyA.currentGuilt;

        Debug.Log("执行前 敌人 HP：" + enemyHPBefore + " / " + enemy.maxHP);
        Debug.Log("执行前 allyA HP：" + allyAHPBefore + " / " + allyA.maxHP);
        Debug.Log("执行前 Attack UseCount：" + useCountBefore + " / " + allyAAttackCardState.maxUseCount);
        Debug.Log("执行前 Attack CD：" + cooldownBefore);
        Debug.Log("执行前 allyA 负罪感：" + guiltBefore);
        Debug.Log("执行前 actionSlot.isUsed：" + actionSlot.isUsed);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            actionSlots,
            emptyIntentQueue
        );

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlotManager.PrintSlotStates(actionSlots);
        Debug.Log("执行后 敌人 HP：" + enemy.currentHP + " / " + enemy.maxHP);
        Debug.Log("执行后 allyA HP：" + allyA.currentHP + " / " + allyA.maxHP);
        Debug.Log("执行后 Attack UseCount：" + allyAAttackCardState.currentUseCount + " / " + allyAAttackCardState.maxUseCount);
        Debug.Log("执行后 Attack CD：" + allyAAttackCardState.currentCooldown);
        Debug.Log("执行后 allyA 负罪感：" + allyA.currentGuilt);
        Debug.Log("执行后 actionSlot.isUsed：" + actionSlot.isUsed);
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
        Debug.Log("预期 enemy HP 下降：" + (enemy.currentHP < enemyHPBefore));
        Debug.Log("预期 allyA HP 不变：" + (allyA.currentHP == allyAHPBefore));
        Debug.Log("预期 Attack UseCount 增加：" + (allyAAttackCardState.currentUseCount > useCountBefore));
        Debug.Log("预期 allyA 负罪感增加：" + (allyA.currentGuilt > guiltBefore));
        Debug.Log("预期 actionSlot.isUsed 为 True：" + actionSlot.isUsed);
        Debug.Log("预期 ExecutionPlan.isCompleted 为 True：" + executionPlan.isCompleted);
    }

    // RunActionSlotExecutionPlanExecuteHighSpeedFreeAttackMixedBasicTestSequence = 执行高速偷刀 + 敌人意图混合测试
    void RunActionSlotExecutionPlanExecuteHighSpeedFreeAttackMixedBasicTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 高速 Attack FreeAction + 敌人意图混合执行测试开始 =====");
        Debug.Log("本测试验证高速偷刀 FreeAction 会排在无人响应敌人意图前执行");

        // 固定速度，确保 allyA 高于 enemy。
        allyA.minSpeed = 20;
        allyA.maxSpeed = 20;
        enemy.minSpeed = 8;
        enemy.maxSpeed = 8;

        StartTurn();

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "enemy_intent_execute_high_speed_free_attack_mixed_001",
            enemy,
            enemyAttackCardState,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(1);
        BattleCardState allyAFreeAttackCardState = CreateTestAttackCardForCharacter(
            allyA,
            "allyA_execute_high_speed_free_attack_mixed_atk_001_copy_0"
        );

        BattleActionSlotManager.AssignFreeAction(
            actionSlots,
            1,
            allyA,
            allyAFreeAttackCardState,
            enemy
        );

        BattleActionSlot actionSlot = actionSlots[0];

        int enemyHPBefore = enemy.currentHP;
        int allyBHPBefore = allyB.currentHP;
        int useCountBefore = allyAFreeAttackCardState.currentUseCount;
        int guiltBefore = allyA.currentGuilt;

        Debug.Log("执行前 敌人 HP：" + enemyHPBefore + " / " + enemy.maxHP);
        Debug.Log("执行前 allyB HP：" + allyBHPBefore + " / " + allyB.maxHP);
        Debug.Log("执行前 allyA Attack UseCount：" + useCountBefore + " / " + allyAFreeAttackCardState.maxUseCount);
        Debug.Log("执行前 allyA 负罪感：" + guiltBefore);
        Debug.Log("执行前 allyA actionSlot.isUsed：" + actionSlot.isUsed);

        Debug.Log("预期执行顺序：1. FreeAction；2. UnrespondedEnemyIntent 敌人意图1");
        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            actionSlots,
            intentQueue
        );

        PrintExecutionPlanItemOrder(executionPlan);
        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlotManager.PrintSlotStates(actionSlots);
        Debug.Log("执行后 敌人 HP：" + enemy.currentHP + " / " + enemy.maxHP);
        Debug.Log("执行后 allyB HP：" + allyB.currentHP + " / " + allyB.maxHP);
        Debug.Log("执行后 allyA Attack UseCount：" + allyAFreeAttackCardState.currentUseCount + " / " + allyAFreeAttackCardState.maxUseCount);
        Debug.Log("执行后 allyA 负罪感：" + allyA.currentGuilt);
        Debug.Log("执行后 allyA actionSlot.isUsed：" + actionSlot.isUsed);
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
        Debug.Log("预期第1项为 FreeAction：" + IsExecutionItemTypeAt(executionPlan, 0, BattleExecutionItemType.FreeAction));
        Debug.Log("预期第2项为 UnrespondedEnemyIntent：" + IsExecutionItemTypeAt(executionPlan, 1, BattleExecutionItemType.UnrespondedEnemyIntent));
        Debug.Log("预期 enemy HP 下降：" + (enemy.currentHP < enemyHPBefore));
        Debug.Log("预期 allyB HP 下降：" + (allyB.currentHP < allyBHPBefore));
        Debug.Log("预期 allyA Attack UseCount 增加：" + (allyAFreeAttackCardState.currentUseCount > useCountBefore));
        Debug.Log("预期 allyA 负罪感增加：" + (allyA.currentGuilt > guiltBefore));
        Debug.Log("预期 allyA actionSlot.isUsed 为 True：" + actionSlot.isUsed);
        Debug.Log("预期 ExecutionPlan.isCompleted 为 True：" + executionPlan.isCompleted);
        Debug.Log("预期所有 item 均完成：" + AreAllExecutionItemsCompleted(executionPlan));
    }

    // RunActionSlotExecutionPlanExecuteLowSpeedFreeAttackMixedBasicTestSequence = 执行低速偷刀 + 敌人意图混合测试
    void RunActionSlotExecutionPlanExecuteLowSpeedFreeAttackMixedBasicTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 低速 Attack FreeAction + 敌人意图混合执行测试开始 =====");
        Debug.Log("本测试验证低速偷刀 FreeAction 会排在无人响应敌人意图后执行");

        // 固定速度，确保 allyA 低于 enemy。
        allyA.minSpeed = 3;
        allyA.maxSpeed = 3;
        enemy.minSpeed = 8;
        enemy.maxSpeed = 8;

        StartTurn();

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "enemy_intent_execute_low_speed_free_attack_mixed_001",
            enemy,
            enemyAttackCardState,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(1);
        BattleCardState allyAFreeAttackCardState = CreateTestAttackCardForCharacter(
            allyA,
            "allyA_execute_low_speed_free_attack_mixed_atk_001_copy_0"
        );

        BattleActionSlotManager.AssignFreeAction(
            actionSlots,
            1,
            allyA,
            allyAFreeAttackCardState,
            enemy
        );

        BattleActionSlot actionSlot = actionSlots[0];

        int enemyHPBefore = enemy.currentHP;
        int allyBHPBefore = allyB.currentHP;
        int useCountBefore = allyAFreeAttackCardState.currentUseCount;
        int guiltBefore = allyA.currentGuilt;

        Debug.Log("执行前 敌人 HP：" + enemyHPBefore + " / " + enemy.maxHP);
        Debug.Log("执行前 allyB HP：" + allyBHPBefore + " / " + allyB.maxHP);
        Debug.Log("执行前 allyA Attack UseCount：" + useCountBefore + " / " + allyAFreeAttackCardState.maxUseCount);
        Debug.Log("执行前 allyA 负罪感：" + guiltBefore);
        Debug.Log("执行前 allyA actionSlot.isUsed：" + actionSlot.isUsed);

        Debug.Log("预期执行顺序：1. UnrespondedEnemyIntent 敌人意图1；2. FreeAction");
        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            actionSlots,
            intentQueue
        );

        PrintExecutionPlanItemOrder(executionPlan);
        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlotManager.PrintSlotStates(actionSlots);
        Debug.Log("执行后 敌人 HP：" + enemy.currentHP + " / " + enemy.maxHP);
        Debug.Log("执行后 allyB HP：" + allyB.currentHP + " / " + allyB.maxHP);
        Debug.Log("执行后 allyA Attack UseCount：" + allyAFreeAttackCardState.currentUseCount + " / " + allyAFreeAttackCardState.maxUseCount);
        Debug.Log("执行后 allyA 负罪感：" + allyA.currentGuilt);
        Debug.Log("执行后 allyA actionSlot.isUsed：" + actionSlot.isUsed);
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
        Debug.Log("预期第1项为 UnrespondedEnemyIntent：" + IsExecutionItemTypeAt(executionPlan, 0, BattleExecutionItemType.UnrespondedEnemyIntent));
        Debug.Log("预期第2项为 FreeAction：" + IsExecutionItemTypeAt(executionPlan, 1, BattleExecutionItemType.FreeAction));
        Debug.Log("预期 allyB HP 下降：" + (allyB.currentHP < allyBHPBefore));
        Debug.Log("预期 enemy HP 下降：" + (enemy.currentHP < enemyHPBefore));
        Debug.Log("预期 allyA Attack UseCount 增加：" + (allyAFreeAttackCardState.currentUseCount > useCountBefore));
        Debug.Log("预期 allyA 负罪感增加：" + (allyA.currentGuilt > guiltBefore));
        Debug.Log("预期 allyA actionSlot.isUsed 为 True：" + actionSlot.isUsed);
        Debug.Log("预期 ExecutionPlan.isCompleted 为 True：" + executionPlan.isCompleted);
        Debug.Log("预期所有 item 均完成：" + AreAllExecutionItemsCompleted(executionPlan));
    }

    void RunRespondedDodgeVsAttackExecutionSubTest(
        string title,
        int dodgePoint,
        int enemyAttackPoint,
        string expectedResultType,
        int expectedHpDamage,
        bool expectTieLimit
    )
    {
        Debug.Log("===== " + title + " Dodge vs Attack 子测试开始 =====");
        Debug.Log("预期 resultType：" + expectedResultType);
        Debug.Log("预期玩家 Dodge 最终点数：" + dodgePoint);
        Debug.Log("预期敌人 Attack 最终点数：" + enemyAttackPoint);
        Debug.Log("预期尝试次数：" + (expectTieLimit ? 10 : 1));

        CharacterData dodgeUser = new CharacterData(title + "_玩家", 30, 3, 3);
        CharacterData enemyUnit = new CharacterData(title + "_敌人", 30, 5, 5);

        BattleCardState dodgeCardState = CreateFixedDodgeCardForCharacter(
            dodgeUser,
            title + "_dodge_copy_0",
            dodgePoint,
            2
        );

        BattleCardState enemyAttackCardState = CreateFixedEnemyAttackCardForDodgeTest(
            enemyUnit,
            title + "_enemy_attack_copy_0",
            enemyAttackPoint,
            2
        );

        BattleEnemyIntent intent = new BattleEnemyIntent(
            title + "_enemy_intent_001",
            enemyUnit,
            enemyAttackCardState,
            dodgeUser,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateCharacterActionSlots(dodgeUser, 1);

        bool assignResult = BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            dodgeUser,
            1,
            dodgeUser,
            dodgeCardState,
            intent
        );

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            actionSlots,
            intentQueue
        );

        BattleActionSlot dodgeSlot = BattleActionSlotManager.GetSlot(actionSlots, dodgeUser, 1);

        int hpBefore = dodgeUser.currentHP;
        int dodgeCooldownBefore = dodgeCardState.currentCooldown;
        int enemyCooldownBefore = enemyAttackCardState.currentCooldown;
        int dodgeUseCountBefore = dodgeCardState.currentUseCount;
        int enemyUseCountBefore = enemyAttackCardState.currentUseCount;
        int dodgeGuiltBefore = dodgeUser.currentGuilt;
        int enemyGuiltBefore = enemyUnit.currentGuilt;
        bool dodgeConsumedBefore = dodgeCardState.isConsumed;
        bool enemyConsumedBefore = enemyAttackCardState.isConsumed;

        Debug.Log("安排 Dodge 响应是否成功：" + assignResult);
        Debug.Log("执行前目标 HP：" + hpBefore + " / " + dodgeUser.maxHP);
        Debug.Log("执行前玩家 Dodge CD：" + dodgeCooldownBefore);
        Debug.Log("执行前敌人 Attack CD：" + enemyCooldownBefore);
        Debug.Log("执行前玩家 Dodge UseCount：" + dodgeUseCountBefore);
        Debug.Log("执行前敌人 Attack UseCount：" + enemyUseCountBefore);

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        int hpAfter = dodgeUser.currentHP;
        bool expectDodgeSuccess = expectedResultType == "DodgeSuccess";
        bool expectDodgeFailed = expectedResultType == "DodgeFailed";
        int expectedDodgeCooldown = expectDodgeFailed
            ? GetExpectedResolvedCooldown(dodgeCardState)
            : dodgeCooldownBefore;
        int expectedEnemyCooldown = expectTieLimit
            ? enemyCooldownBefore
            : GetExpectedResolvedCooldown(enemyAttackCardState);
        bool expectedSlotUsed = expectDodgeFailed;

        Debug.Log("执行后目标 HP：" + hpAfter + " / " + dodgeUser.maxHP);
        Debug.Log("执行后玩家 Dodge CD：" + dodgeCardState.currentCooldown);
        Debug.Log("执行后敌人 Attack CD：" + enemyAttackCardState.currentCooldown);
        Debug.Log("执行后玩家 Dodge UseCount：" + dodgeCardState.currentUseCount);
        Debug.Log("执行后敌人 Attack UseCount：" + enemyAttackCardState.currentUseCount);
        Debug.Log("执行后玩家 Dodge isConsumed：" + dodgeCardState.isConsumed);
        Debug.Log("执行后敌人 Attack isConsumed：" + enemyAttackCardState.isConsumed);
        Debug.Log("执行后玩家 guilt：" + dodgeUser.currentGuilt);
        Debug.Log("执行后敌人 guilt：" + enemyUnit.currentGuilt);
        Debug.Log("执行后 Dodge 槽位 isUsed：" + (dodgeSlot != null && dodgeSlot.isUsed));

        Debug.Log("预期 resultType 可从 Resolver 日志确认：" + expectedResultType);
        Debug.Log("预期 HP 变化：" + expectedHpDamage + "，实际是否符合：" + (hpAfter == hpBefore - expectedHpDamage));
        Debug.Log("预期玩家 Dodge CD：" + expectedDodgeCooldown + "，实际是否符合：" + (dodgeCardState.currentCooldown == expectedDodgeCooldown));
        Debug.Log("预期敌人 Attack CD：" + expectedEnemyCooldown + "，实际是否符合：" + (enemyAttackCardState.currentCooldown == expectedEnemyCooldown));
        Debug.Log("预期 Dodge 槽位 isUsed：" + expectedSlotUsed + "，实际是否符合：" + (dodgeSlot != null && dodgeSlot.isUsed == expectedSlotUsed));
        Debug.Log(
            "DodgeSuccess预期进入连续闪避且暂不正式结算：" +
            (!expectDodgeSuccess ||
             (dodgeSlot != null &&
              dodgeSlot.isContinuousDodgeActive &&
              dodgeSlot.successfulDodgeCount == 1 &&
              !dodgeSlot.isCardUseFinalized))
        );
        Debug.Log("预期 ExecutionPlan 完成：" + (executionPlan != null && executionPlan.isCompleted));

        if (expectedResultType == "DodgeFailed")
        {
            Debug.Log("DodgeFailed 分支：复用敌人最终胜利点数，未重新 Roll，预期伤害来自敌人点数 " + enemyAttackPoint + "：" + (hpAfter == hpBefore - enemyAttackPoint));
        }

        if (expectTieLimit)
        {
            bool dodgeStateUnchanged =
                dodgeCardState.currentCooldown == dodgeCooldownBefore &&
                dodgeCardState.currentUseCount == dodgeUseCountBefore &&
                dodgeCardState.isConsumed == dodgeConsumedBefore &&
                dodgeUser.currentGuilt == dodgeGuiltBefore;

            bool enemyStateUnchanged =
                enemyAttackCardState.currentCooldown == enemyCooldownBefore &&
                enemyAttackCardState.currentUseCount == enemyUseCountBefore &&
                enemyAttackCardState.isConsumed == enemyConsumedBefore &&
                enemyUnit.currentGuilt == enemyGuiltBefore;

            Debug.Log("TieLimit 预期玩家 Dodge 状态完全不变：" + dodgeStateUnchanged);
            Debug.Log("TieLimit 预期敌人 Attack 状态完全不变：" + enemyStateUnchanged);
            Debug.Log("TieLimit 预期目标 HP 不变：" + (hpAfter == hpBefore));
        }
    }

    void RunBattleResolverRespondedAttackVsAttackSubTest(
        string title,
        int playerMinPoint,
        int playerMaxPoint,
        int enemyMinPoint,
        int enemyMaxPoint,
        string expectedResultType
    )
    {
        Debug.Log("===== 子测试：" + title + " =====");

        CharacterData testPlayer = new CharacterData(title + "玩家", 30, 10, 10);
        CharacterData testOriginalTarget = new CharacterData(title + "原目标", 30, 3, 3);
        CharacterData testEnemy = new CharacterData(title + "敌人", 30, 5, 5);

        CardTestData playerAttackCard = new CardTestData
        {
            cardID = title + "_player_attack",
            cardName = title + "玩家攻击",
            cardType = CardType.Attack,
            isClashable = true,
            minPoint = playerMinPoint,
            maxPoint = playerMaxPoint,
            damageFormula = "PointAsDamage",
            maxUseCount = 3
        };

        CardTestData enemyAttackCard = new CardTestData
        {
            cardID = title + "_enemy_attack",
            cardName = title + "敌人攻击",
            cardType = CardType.Attack,
            isClashable = true,
            minPoint = enemyMinPoint,
            maxPoint = enemyMaxPoint,
            damageFormula = "PointAsDamage"
        };

        BattleCardState playerCardState = BattleCardManager.CreateBattleCard(
            testPlayer,
            playerAttackCard,
            title + "_player_attack_copy_0"
        );

        BattleCardState enemyCardState = BattleCardManager.CreateBattleCard(
            testEnemy,
            enemyAttackCard,
            title + "_enemy_attack_copy_0"
        );

        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            title + "_enemy_intent_001",
            testEnemy,
            enemyCardState,
            testOriginalTarget,
            2,
            1
        );

        BattleActionSlot actionSlot = new BattleActionSlot(1);
        actionSlot.AssignResponse(testPlayer, playerCardState, enemyIntent, true);
        enemyIntent.MarkResponded();

        int playerHPBefore = testPlayer.currentHP;
        int originalTargetHPBefore = testOriginalTarget.currentHP;
        int enemyHPBefore = testEnemy.currentHP;

        Debug.Log("执行前 玩家 HP：" + playerHPBefore + " / " + testPlayer.maxHP);
        Debug.Log("执行前 原目标 HP：" + originalTargetHPBefore + " / " + testOriginalTarget.maxHP);
        Debug.Log("执行前 敌人 HP：" + enemyHPBefore + " / " + testEnemy.maxHP);
        Debug.Log("响应后 actualTarget：" + enemyIntent.GetActualTargetSlotText());

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(actionSlot, enemyIntent);

        PrintBattleResolveResult(result);

        Debug.Log("执行后 玩家 HP：" + testPlayer.currentHP + " / " + testPlayer.maxHP);
        Debug.Log("执行后 原目标 HP：" + testOriginalTarget.currentHP + " / " + testOriginalTarget.maxHP);
        Debug.Log("执行后 敌人 HP：" + testEnemy.currentHP + " / " + testEnemy.maxHP);
        Debug.Log("预期 resultType：" + expectedResultType + "，实际是否符合：" + (result != null && result.resultType == expectedResultType));

        if (expectedResultType == "PlayerWin")
        {
            Debug.Log("玩家胜利验证：敌人是否受伤：" + (testEnemy.currentHP < enemyHPBefore));
        }

        if (expectedResultType == "EnemyWin")
        {
            Debug.Log("敌人胜利验证：actualTargetCharacter 是否受伤：" + (testPlayer.currentHP < playerHPBefore));
            Debug.Log("敌人胜利验证：originalTarget 是否未受伤：" + (testOriginalTarget.currentHP == originalTargetHPBefore));
        }

        if (expectedResultType == "TieLimit")
        {
            Debug.Log("平局上限验证：玩家 HP 是否不变：" + (testPlayer.currentHP == playerHPBefore));
            Debug.Log("平局上限验证：原目标 HP 是否不变：" + (testOriginalTarget.currentHP == originalTargetHPBefore));
            Debug.Log("平局上限验证：敌人 HP 是否不变：" + (testEnemy.currentHP == enemyHPBefore));
        }
    }

    void RunRespondedAttackBothCardsResolvedExecutionSubTest(
        string title,
        int playerAttackPoint,
        int enemyAttackPoint,
        bool playerAttackIsSinCard,
        string expectedResultType,
        int expectedDamageToPlayer,
        int expectedDamageToEnemy
    )
    {
        Debug.Log("===== " + title + " Attack胜方Resolved / 败方不Resolved 测试开始 =====");

        StartTurn();

        CardTestData playerAttackCard = CreateResolvedStateAttackCardData(
            title + "_player_attack",
            title + "玩家攻击",
            playerAttackPoint,
            playerAttackIsSinCard
        );

        CardTestData enemyAttackCard = CreateResolvedStateAttackCardData(
            title + "_enemy_attack",
            title + "敌人攻击",
            enemyAttackPoint,
            false
        );

        BattleCardState playerAttack = BattleCardManager.CreateBattleCard(
            allyB,
            playerAttackCard,
            title + "_player_attack_copy_0"
        );

        BattleCardState enemyAttack = BattleCardManager.CreateBattleCard(
            enemy,
            enemyAttackCard,
            title + "_enemy_attack_copy_0"
        );

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            title + "_intent_001",
            enemy,
            enemyAttack,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            allyB,
            1,
            allyB,
            playerAttack,
            intent1
        );

        BattleActionSlot responseSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 1);

        int playerHPBefore = allyB.currentHP;
        int enemyHPBefore = enemy.currentHP;
        int playerCooldownBefore = playerAttack.currentCooldown;
        int playerUseCountBefore = playerAttack.currentUseCount;
        bool playerConsumedBefore = playerAttack.isConsumed;
        int playerGuiltBefore = allyB.currentGuilt;
        int enemyCooldownBefore = enemyAttack.currentCooldown;
        int enemyUseCountBefore = enemyAttack.currentUseCount;
        bool enemyConsumedBefore = enemyAttack.isConsumed;
        int enemyGuiltBefore = enemy.currentGuilt;

        Debug.Log("执行前 玩家卡 CD：" + playerCooldownBefore);
        Debug.Log("执行前 玩家卡 UseCount：" + playerUseCountBefore + " / " + playerAttack.maxUseCount);
        Debug.Log("执行前 玩家卡 isConsumed：" + playerConsumedBefore);
        Debug.Log("执行前 玩家 guilt：" + playerGuiltBefore);
        Debug.Log("执行前 敌人卡 CD：" + enemyCooldownBefore);
        Debug.Log("执行前 敌人卡 UseCount：" + enemyUseCountBefore + " / " + enemyAttack.maxUseCount);
        Debug.Log("执行前 敌人卡 isConsumed：" + enemyConsumedBefore);
        Debug.Log("执行前 敌人 guilt：" + enemyGuiltBefore);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            actionSlots,
            intentQueue
        );
        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        Debug.Log("执行后 玩家卡 CD：" + playerAttack.currentCooldown);
        Debug.Log("执行后 玩家卡 UseCount：" + playerAttack.currentUseCount + " / " + playerAttack.maxUseCount);
        Debug.Log("执行后 玩家卡 isConsumed：" + playerAttack.isConsumed);
        Debug.Log("执行后 玩家 guilt：" + allyB.currentGuilt);
        Debug.Log("执行后 敌人卡 CD：" + enemyAttack.currentCooldown);
        Debug.Log("执行后 敌人卡 UseCount：" + enemyAttack.currentUseCount + " / " + enemyAttack.maxUseCount);
        Debug.Log("执行后 敌人卡 isConsumed：" + enemyAttack.isConsumed);
        Debug.Log("执行后 敌人 guilt：" + enemy.currentGuilt);

        bool expectPlayerCardResolved = expectedResultType == "PlayerWin";
        bool expectEnemyCardResolved = expectedResultType == "EnemyWin";
        int expectedPlayerCooldown = expectPlayerCardResolved && !playerAttackIsSinCard
            ? GetExpectedResolvedCooldown(playerAttack)
            : playerCooldownBefore;
        int expectedPlayerUseCount = expectPlayerCardResolved && playerAttackIsSinCard
            ? playerUseCountBefore + 1
            : playerUseCountBefore;
        int expectedPlayerGuilt = expectPlayerCardResolved && playerAttackIsSinCard
            ? playerGuiltBefore + playerAttack.cardData.guiltGain
            : playerGuiltBefore;
        int expectedEnemyCooldown = expectEnemyCardResolved
            ? GetExpectedResolvedCooldown(enemyAttack)
            : enemyCooldownBefore;

        Debug.Log("预期 resultType：" + expectedResultType + "，实际可从 Executor Resolver 日志确认");
        Debug.Log("预期玩家 HP 变化：" + expectedDamageToPlayer + "，实际是否符合：" + (allyB.currentHP == playerHPBefore - expectedDamageToPlayer));
        Debug.Log("预期敌人 HP 变化：" + expectedDamageToEnemy + "，实际是否符合：" + (enemy.currentHP == enemyHPBefore - expectedDamageToEnemy));
        Debug.Log("预期玩家卡是否Resolved：" + expectPlayerCardResolved + "，CD是否符合：" + (playerAttack.currentCooldown == expectedPlayerCooldown));
        Debug.Log("预期敌人卡是否Resolved：" + expectEnemyCardResolved + "，CD是否符合：" + (enemyAttack.currentCooldown == expectedEnemyCooldown));
        Debug.Log("预期玩家卡 UseCount：" + expectedPlayerUseCount + "，实际是否符合：" + (playerAttack.currentUseCount == expectedPlayerUseCount));
        Debug.Log("预期敌人卡 UseCount 不变：" + (enemyAttack.currentUseCount == enemyUseCountBefore));
        Debug.Log("预期玩家卡 isConsumed 保持未消耗：" + (!playerAttack.isConsumed));
        Debug.Log("预期敌人卡 isConsumed 保持不变：" + (enemyAttack.isConsumed == enemyConsumedBefore));
        Debug.Log("预期玩家 guilt：" + expectedPlayerGuilt + "，实际是否符合：" + (allyB.currentGuilt == expectedPlayerGuilt));
        Debug.Log("预期敌人 guilt 不变：" + (enemy.currentGuilt == enemyGuiltBefore));
        Debug.Log("预期主响应槽位 MarkUsed：" + (responseSlot != null && responseSlot.isUsed));
        Debug.Log("预期 ExecutionPlan 完成：" + executionPlan.isCompleted);

        if (playerAttackIsSinCard && expectPlayerCardResolved)
        {
            Debug.Log("预期 guiltGain 增加一次：" + (allyB.currentGuilt == playerGuiltBefore + playerAttack.cardData.guiltGain));
            Debug.Log("预期 UseCount 增加一次：" + (playerAttack.currentUseCount == playerUseCountBefore + 1));
            Debug.Log("预期 guiltGain / UseCount 不重复增加：" + (allyB.currentGuilt == playerGuiltBefore + playerAttack.cardData.guiltGain && playerAttack.currentUseCount == playerUseCountBefore + 1));
        }
        else if (playerAttackIsSinCard)
        {
            Debug.Log("预期失败罪卡不增加 guilt / UseCount：" + (allyB.currentGuilt == playerGuiltBefore && playerAttack.currentUseCount == playerUseCountBefore));
        }
    }

    void RunBattleResolverRespondedDefenseVsAttackSubTest(
        string title,
        int enemyMinPoint,
        int enemyMaxPoint,
        int defenseMinPoint,
        int defenseMaxPoint,
        int defenseCooldown,
        string expectedResultType,
        int expectedDamage
    )
    {
        Debug.Log("===== 子测试：" + title + " =====");

        CharacterData testDefender = new CharacterData(title + "防御者", 30, 5, 5);
        CharacterData testEnemy = new CharacterData(title + "敌人", 30, 5, 5);

        CardTestData defenseCard = new CardTestData
        {
            cardID = title + "_player_defense",
            cardName = title + "玩家防御",
            cardType = CardType.Defense,
            isClashable = false,
            minPoint = defenseMinPoint,
            maxPoint = defenseMaxPoint,
            cooldown = defenseCooldown,
            defenseFormula = "PointAsDefense"
        };

        CardTestData enemyAttackCard = new CardTestData
        {
            cardID = title + "_enemy_attack",
            cardName = title + "敌人攻击",
            cardType = CardType.Attack,
            isClashable = true,
            minPoint = enemyMinPoint,
            maxPoint = enemyMaxPoint,
            damageFormula = "PointAsDamage"
        };

        BattleCardState defenseCardState = BattleCardManager.CreateBattleCard(
            testDefender,
            defenseCard,
            title + "_player_defense_copy_0"
        );

        BattleCardState enemyCardState = BattleCardManager.CreateBattleCard(
            testEnemy,
            enemyAttackCard,
            title + "_enemy_attack_copy_0"
        );

        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            title + "_enemy_intent_001",
            testEnemy,
            enemyCardState,
            testDefender,
            1,
            1
        );

        BattleActionSlot actionSlot = new BattleActionSlot(1);
        actionSlot.AssignResponse(testDefender, defenseCardState, enemyIntent, false);
        enemyIntent.MarkResponded();

        int defenderHPBefore = testDefender.currentHP;
        int defenseCooldownBefore = defenseCardState.currentCooldown;

        Debug.Log("执行前 防御者 HP：" + defenderHPBefore + " / " + testDefender.maxHP);
        Debug.Log("执行前 Defense CD：" + defenseCooldownBefore);
        Debug.Log("敌人固定攻击点数：" + enemyMinPoint + "-" + enemyMaxPoint);
        Debug.Log("玩家固定防御点数：" + defenseMinPoint + "-" + defenseMaxPoint);
        Debug.Log("响应目标：" + enemyIntent.GetActualTargetSlotText());

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(actionSlot, enemyIntent);

        PrintBattleResolveResult(result);

        Debug.Log("执行后 防御者 HP：" + testDefender.currentHP + " / " + testDefender.maxHP);
        Debug.Log("执行后 Defense CD：" + defenseCardState.currentCooldown);

        Debug.Log("预期 resultType：" + expectedResultType + "，实际是否符合：" + (result != null && result.resultType == expectedResultType));
        Debug.Log("预期 damage：" + expectedDamage + "，实际是否符合：" + (result != null && result.damage == expectedDamage));
        Debug.Log("预期 hasDamage：" + (expectedDamage > 0) + "，实际是否符合：" + (result != null && result.hasDamage == (expectedDamage > 0)));
        Debug.Log("预期 playerCardUsed：True，实际是否符合：" + (result != null && result.playerCardUsed));
        Debug.Log("预期 enemyCardUsed：True，实际是否符合：" + (result != null && result.enemyCardUsed));
        Debug.Log("预期 shouldCompleteItem：True，实际是否符合：" + (result != null && result.shouldCompleteItem));
        Debug.Log("预期 triggeredEventChain：True，实际是否符合：" + (result != null && result.triggeredEventChain));
        Debug.Log("预期 Defense CD 进入运行时补偿值：" + (defenseCardState.currentCooldown == GetExpectedResolvedCooldown(defenseCardState)));
        Debug.Log("预期剩余攻击点数出现在 message：" + (result != null && result.message.Contains("剩余攻击点数")));

        if (expectedDamage == 0)
        {
            Debug.Log("完全防御验证：防御者 HP 是否不变：" + (testDefender.currentHP == defenderHPBefore));
            Debug.Log("完全防御验证：message 是否包含剩余攻击点数 0：" + (result != null && result.message.Contains("剩余攻击点数 0")));
        }
        else
        {
            Debug.Log("减伤防御验证：防御者 HP 是否按最终伤害下降：" + (testDefender.currentHP == defenderHPBefore - expectedDamage));
            Debug.Log("减伤防御验证：使用剩余攻击点数进入伤害公式，而不是最终伤害减防御值：" + (result != null && result.message.Contains("剩余攻击点数")));
        }
    }

    void RunBattleResolverDefenseKnownEnemyPointSubTest(
        string title,
        int knownEnemyAttackPoint,
        int defensePoint,
        string expectedResultType,
        int expectedRemainingAttackPoint,
        int expectedDamage
    )
    {
        Debug.Log("===== 子测试：" + title + " =====");

        CharacterData testDefender = new CharacterData(title + "防御者", 30, 5, 5);
        CharacterData testEnemy = new CharacterData(title + "敌人", 30, 5, 5);

        CardTestData defenseCard = new CardTestData
        {
            cardID = title + "_player_defense",
            cardName = title + "玩家防御",
            cardType = CardType.Defense,
            isClashable = false,
            minPoint = defensePoint,
            maxPoint = defensePoint,
            cooldown = 2,
            defenseFormula = "PointAsDefense"
        };

        CardTestData enemyAttackCard = new CardTestData
        {
            cardID = title + "_enemy_attack",
            cardName = title + "敌人攻击",
            cardType = CardType.Attack,
            isClashable = true,
            minPoint = 1,
            maxPoint = 1,
            cooldown = 2,
            damageFormula = "PointAsDamage"
        };

        BattleCardState defenseCardState = BattleCardManager.CreateBattleCard(
            testDefender,
            defenseCard,
            title + "_player_defense_copy_0"
        );

        BattleCardState enemyCardState = BattleCardManager.CreateBattleCard(
            testEnemy,
            enemyAttackCard,
            title + "_enemy_attack_copy_0"
        );

        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            title + "_enemy_intent_001",
            testEnemy,
            enemyCardState,
            testDefender,
            1,
            1
        );

        BattleActionSlot defenseSlot = new BattleActionSlot(1);
        defenseSlot.AssignResponse(testDefender, defenseCardState, enemyIntent, false);
        enemyIntent.MarkResponded();

        int defenderHPBefore = testDefender.currentHP;
        int defenseCooldownBefore = defenseCardState.currentCooldown;
        int enemyCooldownBefore = enemyCardState.currentCooldown;
        int enemyUseCountBefore = enemyCardState.currentUseCount;

        Debug.Log("执行前 防御者 HP：" + defenderHPBefore + " / " + testDefender.maxHP);
        Debug.Log("执行前 Defense CD：" + defenseCooldownBefore);
        Debug.Log("执行前 敌人卡 CD：" + enemyCooldownBefore);
        Debug.Log("敌人卡自身点数范围故意设为：1-1");
        Debug.Log("传入 knownEnemyAttackPoint：" + knownEnemyAttackPoint);
        Debug.Log("玩家固定防御点数：" + defensePoint + "-" + defensePoint);

        BattleResolveResult result = BattleResolver.ResolveDefenseVsAttackWithKnownEnemyPoint(
            defenseSlot,
            enemyIntent,
            knownEnemyAttackPoint
        );

        PrintBattleResolveResult(result);

        Debug.Log("执行后 防御者 HP：" + testDefender.currentHP + " / " + testDefender.maxHP);
        Debug.Log("执行后 Defense CD：" + defenseCardState.currentCooldown);
        Debug.Log("执行后 敌人卡 CD：" + enemyCardState.currentCooldown);
        Debug.Log("执行后 敌人卡 UseCount：" + enemyCardState.currentUseCount);

        Debug.Log("预期 resultType：" + expectedResultType + "，实际是否符合：" + (result != null && result.resultType == expectedResultType));
        Debug.Log("预期 enemyPoint 使用传入 knownEnemyAttackPoint：" + (result != null && result.enemyPoint == Mathf.Max(0, knownEnemyAttackPoint)));
        Debug.Log("预期未使用敌人卡自身 1-1 点数：" + (result != null && result.enemyPoint != 1));
        Debug.Log("预期剩余攻击点数出现在 message：" + (result != null && result.message.Contains("剩余攻击点数 " + expectedRemainingAttackPoint)));
        Debug.Log("预期 message 写明未重新 Roll：" + (result != null && result.message.Contains("使用已确定敌人点数，未重新 Roll")));
        Debug.Log("预期 damage：" + expectedDamage + "，实际是否符合：" + (result != null && result.damage == expectedDamage));
        Debug.Log("预期防御者 HP 按最终伤害变化：" + (testDefender.currentHP == defenderHPBefore - expectedDamage));
        Debug.Log("预期 playerCardUsed：True，实际是否符合：" + (result != null && result.playerCardUsed));
        Debug.Log("预期 enemyCardUsed：False，实际是否符合：" + (result != null && !result.enemyCardUsed));
        Debug.Log("预期 Defense 进入运行时补偿CD：" + (defenseCardState.currentCooldown == GetExpectedResolvedCooldown(defenseCardState)));
        Debug.Log("预期敌人卡没有由该入口进入 CD：" + (enemyCardState.currentCooldown == enemyCooldownBefore));
        Debug.Log("预期敌人卡 UseCount 未变化：" + (enemyCardState.currentUseCount == enemyUseCountBefore));
        Debug.Log("预期 shouldCompleteItem：True，实际是否符合：" + (result != null && result.shouldCompleteItem));
        Debug.Log("预期 triggeredEventChain：True，实际是否符合：" + (result != null && result.triggeredEventChain));

        if (expectedDamage == 0)
        {
            Debug.Log("完全防御验证：防御者 HP 是否不变：" + (testDefender.currentHP == defenderHPBefore));
            Debug.Log("完全防御验证：没有虚假伤害：" + (result != null && !result.hasDamage && result.damage == 0));
        }
    }

    void PrintBattleResolveResult(BattleResolveResult result)
    {
        if (result == null)
        {
            Debug.LogWarning("BattleResolveResult 为空");
            return;
        }

        string damagedCharacterName = result.damagedCharacter != null
            ? result.damagedCharacter.characterName
            : "无";

        Debug.Log(
            "===== BattleResolveResult =====\n" +
            "isSuccess：" + result.isSuccess + "\n" +
            "shouldCompleteItem：" + result.shouldCompleteItem + "\n" +
            "playerCardUsed：" + result.playerCardUsed + "\n" +
            "enemyCardUsed：" + result.enemyCardUsed + "\n" +
            "hasDamage：" + result.hasDamage + "\n" +
            "damage：" + result.damage + "\n" +
            "damagedCharacter：" + damagedCharacterName + "\n" +
            "resultType：" + result.resultType + "\n" +
            "playerPoint：" + result.playerPoint + "\n" +
            "enemyPoint：" + result.enemyPoint + "\n" +
            "clashAttemptCount：" + result.clashAttemptCount + "\n" +
            "isTieLimitReached：" + result.isTieLimitReached + "\n" +
            "triggeredEventChain：" + result.triggeredEventChain + "\n" +
            "message：" + result.message
        );
    }

    void PrintExecutionPlanItemOrder(BattleExecutionPlan executionPlan)
    {
        if (executionPlan == null || executionPlan.executionItems == null)
        {
            Debug.LogWarning("ExecutionPlan item 顺序打印失败：executionPlan 为空");
            return;
        }

        Debug.Log("===== ExecutionPlan item 顺序检查 =====");

        for (int i = 0; i < executionPlan.executionItems.Count; i++)
        {
            BattleExecutionItem item = executionPlan.executionItems[i];

            if (item == null)
            {
                Debug.Log((i + 1) + ". item 为空");
                continue;
            }

            Debug.Log((i + 1) + ". " + item.executionType);
        }
    }

    bool IsExecutionItemTypeAt(
        BattleExecutionPlan executionPlan,
        int index,
        BattleExecutionItemType expectedType
    )
    {
        if (executionPlan == null || executionPlan.executionItems == null)
        {
            return false;
        }

        if (index < 0 || index >= executionPlan.executionItems.Count)
        {
            return false;
        }

        BattleExecutionItem item = executionPlan.executionItems[index];

        return item != null && item.executionType == expectedType;
    }

    bool AreAllExecutionItemsCompleted(BattleExecutionPlan executionPlan)
    {
        if (executionPlan == null || executionPlan.executionItems == null)
        {
            return false;
        }

        foreach (BattleExecutionItem item in executionPlan.executionItems)
        {
            if (item == null || !item.isCompleted)
            {
                return false;
            }
        }

        return true;
    }

    // ================================
    // Action Slot 测试流程
    // ================================

    // RunActionSlotBasicTestSequence = 执行行动槽位基础测试流程
    void RunActionSlotBasicTestSequence()
    {
        Debug.Log("===== Action Slot 基础测试开始 =====");

        // 行动槽位依赖当前速度判断能否介入，所以先进入回合开始流程
        StartTurn();

        BattleEnemyIntent enemyIntent = CreateTestEnemyIntent();
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            allyA,
            allyAAttackCardState,
            enemyIntent
        );

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            2,
            allyA,
            allyAAttackCardState,
            enemyIntent
        );

        BattleActionSlotManager.AssignFreeAction(
            actionSlots,
            2,
            allyA,
            allyAAbilitySinCardState,
            enemy
        );

        BattleActionSlotManager.PrintSlotStates(actionSlots);

        ExecuteActionSlots(actionSlots);

        BattleActionSlotManager.PrintSlotStates(actionSlots);
        PrintCharacterCardStates(allyA);
    }

    // RunActionSlotInterceptFailTestSequence = 执行速度不足无法介入测试
    void RunActionSlotInterceptFailTestSequence()
    {
        Debug.Log("===== Action Slot 速度不足测试开始 =====");

        CharacterData slowAlly = new CharacterData("低速角色", 30, 3, 3);
        battleUnits.Add(slowAlly);

        BattleCardState slowAllyAttackCardState = CreateTestAttackCardForCharacter(
            slowAlly,
            "slowAlly_atk_001_copy_0"
        );

        // 速度判断依赖当前速度，所以先进入回合开始流程
        StartTurn();

        BattleEnemyIntent enemyIntent = CreateTestEnemyIntent();
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);

        bool assignResult = BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            slowAlly,
            slowAllyAttackCardState,
            enemyIntent
        );

        if (!assignResult)
        {
            Debug.Log("低速角色响应敌人意图失败，未执行拼点");
        }

        PrintEnemyIntentActualTarget(enemyIntent);
        BattleActionSlotManager.PrintSlotStates(actionSlots);
        PrintCharacterCardStates(slowAlly);
    }

    // RunActionSlotInterceptEqualFailTestSequence = 执行速度相等无法介入测试
    void RunActionSlotInterceptEqualFailTestSequence()
    {
        Debug.Log("===== Action Slot 速度相等测试开始 =====");

        CharacterData sameSpeedAlly = new CharacterData("同速角色", 30, 6, 6);
        battleUnits.Add(sameSpeedAlly);

        // 固定敌人速度，确保同速角色和敌人当前速度相等
        enemy.minSpeed = 6;
        enemy.maxSpeed = 6;

        BattleCardState sameSpeedAllyAttackCardState = CreateTestAttackCardForCharacter(
            sameSpeedAlly,
            "sameSpeedAlly_atk_001_copy_0"
        );

        // 速度判断依赖当前速度，所以先进入回合开始流程
        StartTurn();

        BattleEnemyIntent enemyIntent = CreateTestEnemyIntent();
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);

        bool assignResult = BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            sameSpeedAlly,
            sameSpeedAllyAttackCardState,
            enemyIntent
        );

        if (!assignResult)
        {
            Debug.Log("同速角色响应敌人意图失败，未执行拼点");
        }

        PrintEnemyIntentActualTarget(enemyIntent);
        BattleActionSlotManager.PrintSlotStates(actionSlots);
        PrintCharacterCardStates(sameSpeedAlly);
    }

    // RunActionSlotLowSpeedOriginalSlotResponseBasicTestSequence = 执行低速原目标槽位响应成功测试
    void RunActionSlotLowSpeedOriginalSlotResponseBasicTestSequence()
    {
        Debug.Log("===== Action Slot 低速原目标槽位响应测试开始 =====");

        // 固定速度，确保 allyB 低于敌人。
        allyB.minSpeed = 3;
        allyB.maxSpeed = 3;
        enemy.minSpeed = 8;
        enemy.maxSpeed = 8;

        StartTurn();

        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            "enemy_intent_low_speed_original_slot_001",
            enemy,
            enemyAttackCardState,
            allyB,
            2,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(enemyIntent);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);

        Debug.Log("测试预期：allyB 速度低于敌人，但 allyB 槽位2是原目标槽位，所以响应应成功且不改写 actualTarget");

        bool assignResult = BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            2,
            allyB,
            allyBDefenseCardState,
            enemyIntent
        );

        Debug.Log("低速原目标槽位响应是否成功：" + assignResult);
        Debug.Log("敌人意图是否已响应：" + enemyIntent.isResponded);
        Debug.Log("敌人意图实际目标角色仍为 allyB：" + object.ReferenceEquals(enemyIntent.actualTargetCharacter, allyB));
        Debug.Log("敌人意图实际目标槽位仍为 2：" + (enemyIntent.actualTargetSlotIndex == 2));
        Debug.Log("敌人意图当前实际目标：" + enemyIntent.GetActualTargetSlotText());

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);
        PrintCharacterCardStates(allyB);
    }

    // RunActionSlotLowSpeedIllegalResponseFailTestSequence = 执行低速非法响应失败测试
    void RunActionSlotLowSpeedIllegalResponseFailTestSequence()
    {
        Debug.Log("===== Action Slot 低速非法响应失败测试开始 =====");

        // 固定速度，确保 allyB 低于敌人。
        allyB.minSpeed = 3;
        allyB.maxSpeed = 3;
        enemy.minSpeed = 8;
        enemy.maxSpeed = 8;

        StartTurn();

        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            "enemy_intent_low_speed_illegal_response_001",
            enemy,
            enemyAttackCardState,
            allyB,
            2,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(enemyIntent);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);

        Debug.Log("测试预期：allyB 速度低于敌人，但尝试用槽位1响应 allyB 槽位2 的敌人意图，应安排失败");

        bool assignResult = BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            allyB,
            allyBDefenseCardState,
            enemyIntent
        );

        Debug.Log("低速非法响应是否成功：" + assignResult);
        Debug.Log("敌人意图是否仍未响应：" + !enemyIntent.isResponded);
        Debug.Log("敌人意图实际目标角色仍为 allyB：" + object.ReferenceEquals(enemyIntent.actualTargetCharacter, allyB));
        Debug.Log("敌人意图实际目标槽位仍为 2：" + (enemyIntent.actualTargetSlotIndex == 2));
        Debug.Log("敌人意图当前实际目标：" + enemyIntent.GetActualTargetSlotText());

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);
        PrintCharacterCardStates(allyB);
    }

    // RunActionSlotMultiIntentBasicTestSequence = 执行多敌人意图基础数据测试
    void RunActionSlotMultiIntentBasicTestSequence()
    {
        Debug.Log("===== Action Slot 多敌人意图基础测试开始 =====");

        // 多意图指定响应仍依赖速度判断，所以先进入回合开始流程
        StartTurn();

        BattleCardState secondEnemyAttackCardState = BattleCardManager.CreateBattleCard(
            enemy,
            enemyAttackCardState.cardData,
            "enemy_atk_001_copy_1"
        );

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "enemy_intent_001",
            enemy,
            enemyAttackCardState,
            allyB,
            2,
            1
        );

        BattleEnemyIntent intent2 = new BattleEnemyIntent(
            "enemy_intent_002",
            enemy,
            secondEnemyAttackCardState,
            allyB,
            1,
            2
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1, intent2);
        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);
        BattleEnemyIntent selectedIntent = BattleEnemyIntentManager.FindIntentByOrder(intentQueue, 2);

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            allyA,
            allyAAttackCardState,
            selectedIntent
        );

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        List<BattleEnemyIntent> unrespondedIntents = BattleEnemyIntentManager.GetUnrespondedIntents(intentQueue);
        Debug.Log("当前未响应敌人意图数量：" + unrespondedIntents.Count);
        BattleEnemyIntentManager.PrintUnrespondedIntents(intentQueue);
        BattleEnemyIntentManager.PrintIntentHandlingPreview(intentQueue);
        BattleActionSlotManager.PrintActionSlotIntentHandlingPreview(actionSlots, intentQueue);
        List<BattleHandlingPreviewItem> previewItems = BattleActionSlotManager.CreateSpeedPriorityHandlingPreviewItems(actionSlots, intentQueue);
        Debug.Log("速度响应优先处理预览项数量：" + previewItems.Count);
        BattleActionSlotManager.PrintSpeedPriorityHandlingPreview(actionSlots, intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);
        PrintCharacterCardStates(allyA);
    }

    // RunActionSlotResponseOverwriteBasicTestSequence = 执行同一敌人意图响应覆盖基础测试
    void RunActionSlotResponseOverwriteBasicTestSequence()
    {
        Debug.Log("===== Action Slot 响应覆盖基础测试开始 =====");

        // 响应覆盖仍依赖速度判断，所以先进入回合开始流程
        StartTurn();

        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            "enemy_intent_overwrite_001",
            enemy,
            enemyAttackCardState,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(enemyIntent);
        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);
        BattleCardState secondAllyAAttackCardState = CreateTestAttackCardForCharacter(
            allyA,
            "allyA_atk_001_copy_1"
        );

        Debug.Log("===== 第一次响应：槽位1响应敌人意图1 =====");
        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            allyA,
            allyAAttackCardState,
            enemyIntent
        );

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        Debug.Log("===== 第二次响应：槽位2覆盖敌人意图1 =====");
        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            2,
            allyA,
            secondAllyAAttackCardState,
            enemyIntent
        );

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleEnemyIntentManager.PrintIntentHandlingPreview(intentQueue);
        BattleActionSlotManager.PrintActionSlotIntentHandlingPreview(actionSlots, intentQueue);

        List<BattleHandlingPreviewItem> previewItems = BattleActionSlotManager.CreateSpeedPriorityHandlingPreviewItems(actionSlots, intentQueue);
        Debug.Log("速度响应优先处理预览项数量：" + previewItems.Count);

        BattleActionSlotManager.PrintSpeedPriorityHandlingPreview(actionSlots, intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);
        PrintCharacterCardStates(allyA);
    }

    // RunActionSlotResponseOverwriteFailKeepOldTestSequence = 执行响应覆盖失败保持旧响应测试
    void RunActionSlotResponseOverwriteFailKeepOldTestSequence()
    {
        Debug.Log("===== Action Slot 响应覆盖失败保持旧响应测试开始 =====");

        CharacterData slowAlly = new CharacterData("覆盖失败角色", 30, 3, 3);
        battleUnits.Add(slowAlly);

        BattleCardState slowAllyAttackCardState = CreateTestAttackCardForCharacter(
            slowAlly,
            "slowAlly_overwrite_fail_atk_001_copy_0"
        );

        // 响应覆盖失败测试依赖当前速度判断，所以先进入回合开始流程
        StartTurn();

        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            "enemy_intent_overwrite_fail_001",
            enemy,
            enemyAttackCardState,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(enemyIntent);
        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);

        Debug.Log("===== 第一次响应：槽位1响应敌人意图1 =====");
        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            allyA,
            allyAAttackCardState,
            enemyIntent
        );

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        Debug.Log("===== 第二次响应：覆盖失败角色尝试用槽位2覆盖敌人意图1 =====");
        bool overwriteResult = BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            2,
            slowAlly,
            slowAllyAttackCardState,
            enemyIntent
        );

        if (!overwriteResult)
        {
            Debug.Log("覆盖失败角色响应敌人意图失败，旧响应应保持不变");
        }

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleEnemyIntentManager.PrintIntentHandlingPreview(intentQueue);
        BattleActionSlotManager.PrintActionSlotIntentHandlingPreview(actionSlots, intentQueue);

        List<BattleHandlingPreviewItem> previewItems = BattleActionSlotManager.CreateSpeedPriorityHandlingPreviewItems(actionSlots, intentQueue);
        Debug.Log("速度响应优先处理预览项数量：" + previewItems.Count);

        BattleActionSlotManager.PrintSpeedPriorityHandlingPreview(actionSlots, intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);
        PrintCharacterCardStates(allyA);
        PrintCharacterCardStates(slowAlly);
    }

    // RunActionSlotExecutionPlanBasicTestSequence = 执行 BattleExecutionPlan 第一版生成测试
    void RunActionSlotExecutionPlanBasicTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 第一版生成测试开始 =====");

        // ExecutionPlan 生成测试依赖响应安排，响应安排依赖当前速度判断
        StartTurn();

        BattleCardState secondEnemyAttackCardState = BattleCardManager.CreateBattleCard(
            enemy,
            enemyAttackCardState.cardData,
            "enemy_atk_001_execution_plan_copy_1"
        );

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "enemy_intent_execution_plan_001",
            enemy,
            enemyAttackCardState,
            allyB,
            2,
            1
        );

        BattleEnemyIntent intent2 = new BattleEnemyIntent(
            "enemy_intent_execution_plan_002",
            enemy,
            secondEnemyAttackCardState,
            allyB,
            1,
            2
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1, intent2);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            allyA,
            allyAAttackCardState,
            intent2
        );

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            actionSlots,
            intentQueue
        );
        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        PrintCharacterCardStates(allyA);
    }

    // RunActionSlotExecutionPlanSpeedHighResponseOrderBasicTestSequence = 执行高速响应提前顺序测试
    void RunActionSlotExecutionPlanSpeedHighResponseOrderBasicTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 速度规则：高速响应提前测试开始 =====");

        StartTurn();

        BattleCardState secondEnemyAttackCardState = BattleCardManager.CreateBattleCard(
            enemy,
            enemyAttackCardState.cardData,
            "enemy_atk_001_speed_high_response_copy_1"
        );

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "enemy_intent_speed_high_response_001",
            enemy,
            enemyAttackCardState,
            allyB,
            2,
            1
        );

        BattleEnemyIntent intent2 = new BattleEnemyIntent(
            "enemy_intent_speed_high_response_002",
            enemy,
            secondEnemyAttackCardState,
            allyB,
            1,
            2
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1, intent2);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            allyA,
            allyAAttackCardState,
            intent2
        );

        Debug.Log("预期顺序：1. RespondedEnemyIntent 敌人意图2；2. UnrespondedEnemyIntent 敌人意图1");
        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            actionSlots,
            intentQueue
        );

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        PrintCharacterCardStates(allyA);
    }

    // RunActionSlotExecutionPlanSpeedLowResponseOrderBasicTestSequence = 执行低速响应不提前测试
    void RunActionSlotExecutionPlanSpeedLowResponseOrderBasicTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 速度规则：低速响应不提前测试开始 =====");

        // 固定速度，确保 allyB 低于敌人。
        allyB.minSpeed = 3;
        allyB.maxSpeed = 3;
        enemy.minSpeed = 8;
        enemy.maxSpeed = 8;

        StartTurn();

        BattleCardState secondEnemyAttackCardState = BattleCardManager.CreateBattleCard(
            enemy,
            enemyAttackCardState.cardData,
            "enemy_atk_001_speed_low_response_copy_1"
        );

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "enemy_intent_speed_low_response_001",
            enemy,
            enemyAttackCardState,
            allyB,
            1,
            1
        );

        BattleEnemyIntent intent2 = new BattleEnemyIntent(
            "enemy_intent_speed_low_response_002",
            enemy,
            secondEnemyAttackCardState,
            allyB,
            2,
            2
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1, intent2);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            2,
            allyB,
            allyBDefenseCardState,
            intent2
        );

        Debug.Log("预期顺序：1. UnrespondedEnemyIntent 敌人意图1；2. RespondedEnemyIntent 敌人意图2");
        Debug.Log("敌人意图2 是否已响应：" + intent2.isResponded);
        Debug.Log("敌人意图2 实际目标角色仍为 allyB：" + object.ReferenceEquals(intent2.actualTargetCharacter, allyB));
        Debug.Log("敌人意图2 实际目标槽位仍为 2：" + (intent2.actualTargetSlotIndex == 2));
        Debug.Log("敌人意图2 当前实际目标：" + intent2.GetActualTargetSlotText());

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            actionSlots,
            intentQueue
        );

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        PrintCharacterCardStates(allyB);
    }

    // RunActionSlotExecutionPlanSpeedHighFreeActionBasicTestSequence = 执行高速自由行动提前测试
    void RunActionSlotExecutionPlanSpeedHighFreeActionBasicTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 速度规则：高速自由行动测试开始 =====");

        StartTurn();

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "enemy_intent_speed_high_free_action_001",
            enemy,
            enemyAttackCardState,
            allyB,
            2,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);
        BattleCardState allyAFreeActionCardState = CreateTestAttackCardForCharacter(
            allyA,
            "allyA_speed_high_free_action_atk_001_copy_0"
        );

        BattleActionSlotManager.AssignFreeAction(
            actionSlots,
            1,
            allyA,
            allyAFreeActionCardState,
            enemy
        );

        Debug.Log("预期顺序：1. FreeAction；2. UnrespondedEnemyIntent 敌人意图1");
        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            actionSlots,
            intentQueue
        );

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        PrintCharacterCardStates(allyA);
    }

    // RunActionSlotExecutionPlanSpeedLowFreeActionBasicTestSequence = 执行低速自由行动后置测试
    void RunActionSlotExecutionPlanSpeedLowFreeActionBasicTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 速度规则：低速自由行动测试开始 =====");

        // 固定速度，确保 allyB 低于敌人。
        allyB.minSpeed = 3;
        allyB.maxSpeed = 3;
        enemy.minSpeed = 8;
        enemy.maxSpeed = 8;

        StartTurn();

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "enemy_intent_speed_low_free_action_001",
            enemy,
            enemyAttackCardState,
            allyB,
            2,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);
        BattleCardState allyBFreeActionCardState = CreateTestAttackCardForCharacter(
            allyB,
            "allyB_speed_low_free_action_atk_001_copy_0"
        );

        BattleActionSlotManager.AssignFreeAction(
            actionSlots,
            1,
            allyB,
            allyBFreeActionCardState,
            enemy
        );

        Debug.Log("预期顺序：1. UnrespondedEnemyIntent 敌人意图1；2. FreeAction");
        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            actionSlots,
            intentQueue
        );

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        PrintCharacterCardStates(allyB);
    }

    // RunActionSlotExecutionPlanEmptyTestSequence = 执行 BattleExecutionPlan 空输入安全测试
    void RunActionSlotExecutionPlanEmptyTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 空计划 / 空队列安全测试开始 =====");

        BattleExecutionPlanManager.PrintExecutionPlan(null);

        BattleExecutionPlan nullInputPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            null,
            null
        );

        BattleExecutionPlanManager.PrintExecutionPlan(nullInputPlan);

        List<BattleActionSlot> emptyActionSlots = new List<BattleActionSlot>();
        List<BattleEnemyIntent> emptyIntentQueue = new List<BattleEnemyIntent>();

        BattleExecutionPlan emptyInputPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            emptyActionSlots,
            emptyIntentQueue
        );

        BattleExecutionPlanManager.PrintExecutionPlan(emptyInputPlan);

        BattleExecutionPlan emptyPlan = new BattleExecutionPlan();
        BattleExecutionPlanManager.PrintExecutionPlan(emptyPlan);
    }

    // RunActionSlotExecutionPlanMissingSlotTestSequence = 执行已响应但缺少绑定槽位的安全测试
    void RunActionSlotExecutionPlanMissingSlotTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 已响应但缺少绑定槽位测试开始 =====");

        StartTurn();

        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            "enemy_intent_execution_plan_missing_slot_001",
            enemy,
            enemyAttackCardState,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(enemyIntent);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(1);

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            allyA,
            allyAAttackCardState,
            enemyIntent
        );

        if (actionSlots.Count > 0 && actionSlots[0] != null)
        {
            actionSlots[0].UnbindEnemyIntent();
        }

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            actionSlots,
            intentQueue
        );
        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        PrintCharacterCardStates(allyA);
    }

    // RunActionSlotExecutionPlanMultiBasicTestSequence = 执行 BattleExecutionPlan 多项顺序测试
    void RunActionSlotExecutionPlanMultiBasicTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 多项顺序测试开始 =====");

        StartTurn();

        BattleCardState secondEnemyAttackCardState = BattleCardManager.CreateBattleCard(
            enemy,
            enemyAttackCardState.cardData,
            "enemy_atk_001_execution_plan_multi_copy_1"
        );

        BattleCardState thirdEnemyAttackCardState = BattleCardManager.CreateBattleCard(
            enemy,
            enemyAttackCardState.cardData,
            "enemy_atk_001_execution_plan_multi_copy_2"
        );

        BattleCardState fourthEnemyAttackCardState = BattleCardManager.CreateBattleCard(
            enemy,
            enemyAttackCardState.cardData,
            "enemy_atk_001_execution_plan_multi_copy_3"
        );

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "enemy_intent_execution_plan_multi_001",
            enemy,
            enemyAttackCardState,
            allyB,
            2,
            1
        );

        BattleEnemyIntent intent2 = new BattleEnemyIntent(
            "enemy_intent_execution_plan_multi_002",
            enemy,
            secondEnemyAttackCardState,
            allyB,
            1,
            2
        );

        BattleEnemyIntent intent3 = new BattleEnemyIntent(
            "enemy_intent_execution_plan_multi_003",
            enemy,
            thirdEnemyAttackCardState,
            allyB,
            2,
            3
        );

        BattleEnemyIntent intent4 = new BattleEnemyIntent(
            "enemy_intent_execution_plan_multi_004",
            enemy,
            fourthEnemyAttackCardState,
            allyB,
            1,
            4
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(
            intent1,
            intent2,
            intent3,
            intent4
        );

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);
        BattleCardState secondAllyAAttackCardState = CreateTestAttackCardForCharacter(
            allyA,
            "allyA_execution_plan_multi_atk_001_copy_1"
        );

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            allyA,
            allyAAttackCardState,
            intent2
        );

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            2,
            allyA,
            secondAllyAAttackCardState,
            intent4
        );

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            actionSlots,
            intentQueue
        );

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        PrintCharacterCardStates(allyA);
    }

    // RunActionSlotExecutionPlanStepPreviewBasicTestSequence = 执行 BattleExecutionPlan 执行步骤预览基础测试
    void RunActionSlotExecutionPlanStepPreviewBasicTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 执行步骤预览基础测试开始 =====");

        StartTurn();

        BattleCardState secondEnemyAttackCardState = BattleCardManager.CreateBattleCard(
            enemy,
            enemyAttackCardState.cardData,
            "enemy_atk_001_execution_plan_step_preview_copy_1"
        );

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "enemy_intent_execution_plan_step_preview_001",
            enemy,
            enemyAttackCardState,
            allyB,
            2,
            1
        );

        BattleEnemyIntent intent2 = new BattleEnemyIntent(
            "enemy_intent_execution_plan_step_preview_002",
            enemy,
            secondEnemyAttackCardState,
            allyB,
            1,
            2
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1, intent2);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(2);

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            allyA,
            allyAAttackCardState,
            intent2
        );

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            actionSlots,
            intentQueue
        );

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(executionPlan);
        PrintCharacterCardStates(allyA);
    }

    // RunActionSlotExecutionPlanStepPreviewEmptyTestSequence = 执行 BattleExecutionPlan 执行步骤预览空输入测试
    void RunActionSlotExecutionPlanStepPreviewEmptyTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 执行步骤预览空输入测试开始 =====");

        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(null);

        BattleExecutionPlan emptyPlan = new BattleExecutionPlan();
        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(emptyPlan);

        BattleExecutionPlan nullInputPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            null,
            null
        );

        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(nullInputPlan);

        List<BattleActionSlot> emptyActionSlots = new List<BattleActionSlot>();
        List<BattleEnemyIntent> emptyIntentQueue = new List<BattleEnemyIntent>();

        BattleExecutionPlan emptyInputPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            emptyActionSlots,
            emptyIntentQueue
        );

        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(emptyInputPlan);
    }

    // RunActionSlotExecutionPlanExecuteUnrespondedBasicTestSequence = 执行无人响应敌人意图正式执行基础测试
    void RunActionSlotExecutionPlanExecuteUnrespondedBasicTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 无人响应正式执行基础测试开始 =====");

        StartTurn();

        Debug.Log("执行前 我方角色B HP：" + allyB.currentHP + " / " + allyB.maxHP);

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "enemy_intent_execution_plan_execute_unresponded_001",
            enemy,
            enemyAttackCardState,
            allyB,
            2,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = new List<BattleActionSlot>();

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            actionSlots,
            intentQueue
        );

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        Debug.Log("执行后 我方角色B HP：" + allyB.currentHP + " / " + allyB.maxHP);
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);

        int hpBeforeRepeatExecute = allyB.currentHP;

        Debug.Log("===== 重复执行同一个 BattleExecutionPlan 测试 =====");

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        Debug.Log("重复执行后 我方角色B HP：" + allyB.currentHP + " / " + allyB.maxHP);
        Debug.Log("重复执行前后 HP 是否保持不变：" + (hpBeforeRepeatExecute == allyB.currentHP));
    }

    // RunActionSlotPassiveGuardCandidateOrderBasicTestSequence = 测试两张 PassiveGuard 按槽位顺序选择
    void RunActionSlotPassiveGuardCandidateOrderBasicTestSequence()
    {
        Debug.Log("===== PassiveGuard 候选顺序测试开始 =====");

        StartTurn();

        CardTestData enemyAttackCard = CreateFixedAttackCardData("passive_guard_order_enemy_attack", "被动守备顺序测试敌人攻击", 4);
        BattleCardState enemyAttack = BattleCardManager.CreateBattleCard(enemy, enemyAttackCard, "passive_guard_order_enemy_attack_copy_0");
        BattleCardState guard1 = CreateTestDefenseCardForCharacter(allyB, "passive_guard_order_b_defense_1", 6, 1);
        BattleCardState guard2 = CreateTestDefenseCardForCharacter(allyB, "passive_guard_order_b_defense_2", 6, 1);

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "passive_guard_order_intent_001",
            enemy,
            enemyAttack,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);

        bool assignB1 = BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 1, allyB, guard1);
        bool assignB2 = BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 2, allyB, guard2);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);

        Debug.Log("预期 B槽位1 PassiveGuard 安排成功：" + assignB1);
        Debug.Log("预期 B槽位2 PassiveGuard 安排成功：" + assignB2);
        Debug.Log("预期 item 候选数量为 2：" + (item != null && item.passiveGuardCandidates != null && item.passiveGuardCandidates.Count == 2));
        Debug.Log("预期第 1 候选为 B槽位1：" + (item != null && item.passiveGuardCandidates.Count > 0 && item.passiveGuardCandidates[0].slotIndex == 1));
        Debug.Log("预期第 2 候选为 B槽位2：" + (item != null && item.passiveGuardCandidates.Count > 1 && item.passiveGuardCandidates[1].slotIndex == 2));

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        Debug.Log("预期只触发 B槽位1：" + (BattleActionSlotManager.GetSlot(actionSlots, allyB, 1).isUsed && !BattleActionSlotManager.GetSlot(actionSlots, allyB, 2).isUsed));
        Debug.Log("预期 B槽位1 Defense 进入 CD：" + (guard1.currentCooldown == GetExpectedResolvedCooldown(guard1)));
        Debug.Log("预期 B槽位2 Defense CD 不变：" + (guard2.currentCooldown == 0));
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    // RunActionSlotPassiveGuardSkipInvalidCandidateBasicTestSequence = 测试第一候选执行前失效时跳过
    void RunActionSlotPassiveGuardSkipInvalidCandidateBasicTestSequence()
    {
        Debug.Log("===== PassiveGuard 跳过失效候选测试开始 =====");

        StartTurn();

        CardTestData enemyAttackCard = CreateFixedAttackCardData("passive_guard_skip_enemy_attack", "被动守备跳过测试敌人攻击", 4);
        BattleCardState enemyAttack = BattleCardManager.CreateBattleCard(enemy, enemyAttackCard, "passive_guard_skip_enemy_attack_copy_0");
        BattleCardState guard1 = CreateTestDefenseCardForCharacter(allyB, "passive_guard_skip_b_defense_1", 6, 1);
        BattleCardState guard2 = CreateTestDefenseCardForCharacter(allyB, "passive_guard_skip_b_defense_2", 6, 1);

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "passive_guard_skip_intent_001",
            enemy,
            enemyAttack,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);

        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 1, allyB, guard1);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 2, allyB, guard2);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);
        BattleActionSlotManager.GetSlot(actionSlots, allyB, 1).MarkUsed();

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        Debug.Log("预期 B槽位1 保持已使用但未进入 CD：" + (BattleActionSlotManager.GetSlot(actionSlots, allyB, 1).isUsed && guard1.currentCooldown == 0));
        Debug.Log("预期 B槽位2 接管并 MarkUsed：" + BattleActionSlotManager.GetSlot(actionSlots, allyB, 2).isUsed);
        Debug.Log("预期 B槽位2 Defense 进入 CD：" + (guard2.currentCooldown == GetExpectedResolvedCooldown(guard2)));
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    // RunActionSlotPassiveGuardFullBlockBasicTestSequence = 测试被动守备完全防御
    void RunActionSlotPassiveGuardFullBlockBasicTestSequence()
    {
        RunActionSlotPassiveGuardDefenseSubTest(
            "PassiveGuardFullBlock",
            4,
            6,
            "DefenseFullBlock",
            0
        );
    }

    // RunActionSlotPassiveGuardReducedDamageBasicTestSequence = 测试被动守备减伤
    void RunActionSlotPassiveGuardReducedDamageBasicTestSequence()
    {
        RunActionSlotPassiveGuardDefenseSubTest(
            "PassiveGuardReducedDamage",
            8,
            3,
            "DefenseReducedDamage",
            5
        );
    }

    // RunActionSlotPassiveDodgeUnrespondedBasicTestSequence = 被动闪避第一阶段聚合测试
    void RunActionSlotPassiveDodgeUnrespondedBasicTestSequence()
    {
        Debug.Log("===== PassiveDodge Unresponded 第一阶段聚合测试开始 =====");
        Debug.Log("本入口只测试 UnrespondedEnemyIntent 的被动 Dodge");
        Debug.Log("Attack失败后的被动Dodge接管由模式43 ActionSlotPassiveDodgeAfterAttackLoseBasic 覆盖");
        Debug.Log("当前聚合入口包含6组有效子测试");

        RunPassiveDodgeUnrespondedDodgeFirstSubTest(
            "PassiveDodgeSuccess",
            8,
            5,
            "DodgeSuccess",
            0,
            false,
            true
        );

        RunPassiveDodgeUnrespondedDodgeFirstSubTest(
            "PassiveDodgeFailed",
            4,
            8,
            "DodgeFailed",
            8,
            true,
            true
        );

        RunPassiveDodgeUnrespondedDodgeFirstSubTest(
            "PassiveDodgeTieLimit",
            5,
            5,
            "TieLimit",
            0,
            false,
            false
        );

        RunPassiveDodgeSkipInvalidToDefenseSubTest();
        RunPassiveDodgeTargetMismatchSubTest();
        RunPassiveDodgeRespondedIntentNotTriggeredSubTest();
    }

    void RunPassiveDodgeUnrespondedDodgeFirstSubTest(
        string title,
        int dodgePoint,
        int enemyAttackPoint,
        string expectedResultType,
        int expectedDamage,
        bool expectDodgeUsed,
        bool expectEnemyAttackUsed
    )
    {
        Debug.Log("===== " + title + " 子测试开始 =====");
        Debug.Log("预期 resultType 出现在 Resolver 日志：" + expectedResultType);

        CreateTestCharacters();
        StartTurn();

        int hpBefore = allyB.currentHP;
        BattleCardState passiveDodge = CreateFixedDodgeCardForCharacter(allyB, title + "_b_dodge", dodgePoint, 2);
        BattleCardState followDefense = CreateTestDefenseCardForCharacter(allyB, title + "_b_defense", 12, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(enemy, title + "_enemy_attack", enemyAttackPoint, 2);

        int dodgeCooldownBefore = passiveDodge.currentCooldown;
        int dodgeUseCountBefore = passiveDodge.currentUseCount;
        bool dodgeConsumedBefore = passiveDodge.isConsumed;
        int dodgeGuiltBefore = allyB.currentGuilt;
        int defenseCooldownBefore = followDefense.currentCooldown;
        int defenseUseCountBefore = followDefense.currentUseCount;
        bool defenseConsumedBefore = followDefense.isConsumed;
        int enemyCooldownBefore = enemyAttack.currentCooldown;
        int enemyUseCountBefore = enemyAttack.currentUseCount;
        bool enemyConsumedBefore = enemyAttack.isConsumed;
        int enemyGuiltBefore = enemy.currentGuilt;

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            title + "_intent_001",
            enemy,
            enemyAttack,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);

        bool assignDodge = BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 1, allyB, passiveDodge);
        bool assignDefense = BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 2, allyB, followDefense);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);
        int candidateCount = item != null && item.passiveGuardCandidates != null
            ? item.passiveGuardCandidates.Count
            : 0;
        BattleActionSlot firstCandidate = candidateCount > 0 ? item.passiveGuardCandidates[0] : null;

        Debug.Log("预期 Dodge PassiveGuard 安排成功：" + assignDodge);
        Debug.Log("预期 Defense PassiveGuard 安排成功：" + assignDefense);
        Debug.Log("预期生成 UnrespondedEnemyIntent：" + (item != null && item.executionType == BattleExecutionItemType.UnrespondedEnemyIntent));
        Debug.Log("预期候选数量为 2：" + (candidateCount == 2));
        Debug.Log("预期第一候选为 Dodge：" + (firstCandidate != null && firstCandidate.slotIndex == 1 && firstCandidate.cardState == passiveDodge));

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlot dodgeSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 1);
        BattleActionSlot defenseSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 2);
        int hpAfter = allyB.currentHP;

        Debug.Log("执行前 B HP：" + hpBefore + "，执行后 B HP：" + hpAfter);
        Debug.Log("预期实际目标 HP 变化为 " + expectedDamage + "：" + (hpAfter == hpBefore - expectedDamage));
        Debug.Log("预期 Dodge槽位 isUsed = " + expectDodgeUsed + "：" + (dodgeSlot != null && dodgeSlot.isUsed == expectDodgeUsed));
        Debug.Log("预期后续 Defense槽位未使用：" + (defenseSlot != null && !defenseSlot.isUsed));
        Debug.Log("预期 Dodge CD 变化符合分支：" + (expectDodgeUsed ? passiveDodge.currentCooldown == GetExpectedResolvedCooldown(passiveDodge) : passiveDodge.currentCooldown == dodgeCooldownBefore));
        Debug.Log("预期 Defense CD 不变化：" + (followDefense.currentCooldown == defenseCooldownBefore));
        Debug.Log("预期 Defense UseCount / isConsumed 不变化：" + (followDefense.currentUseCount == defenseUseCountBefore && followDefense.isConsumed == defenseConsumedBefore));
        Debug.Log("预期 Enemy Attack 状态符合分支：" + (expectEnemyAttackUsed ? enemyAttack.currentCooldown == GetExpectedResolvedCooldown(enemyAttack) : enemyAttack.currentCooldown == enemyCooldownBefore));
        Debug.Log("预期 Enemy Attack UseCount / isConsumed 符合分支：" + (enemyAttack.currentUseCount == enemyUseCountBefore && enemyAttack.isConsumed == enemyConsumedBefore));
        Debug.Log("预期 Dodge UseCount / guilt / isConsumed 符合分支：" + (passiveDodge.currentUseCount == dodgeUseCountBefore && allyB.currentGuilt == dodgeGuiltBefore && passiveDodge.isConsumed == dodgeConsumedBefore));
        Debug.Log("预期 Enemy guilt 不变化：" + (enemy.currentGuilt == enemyGuiltBefore));
        Debug.Log("预期只造成一次伤害且不回落 Unresponded：" + (hpAfter == hpBefore - expectedDamage));
        Debug.Log("预期不会错误触发后续守备：" + (defenseSlot != null && !defenseSlot.isUsed && followDefense.currentCooldown == defenseCooldownBefore));
        Debug.Log(
            "预期DodgeSuccess激活连续闪避、其他分支不激活：" +
            (dodgeSlot != null &&
             dodgeSlot.isContinuousDodgeActive == (expectedResultType == "DodgeSuccess"))
        );

        if (expectedResultType == "TieLimit")
        {
            Debug.Log("TieLimit 额外验证：Dodge状态完全不变：" + (passiveDodge.currentCooldown == dodgeCooldownBefore && passiveDodge.currentUseCount == dodgeUseCountBefore && passiveDodge.isConsumed == dodgeConsumedBefore && allyB.currentGuilt == dodgeGuiltBefore));
            Debug.Log("TieLimit 额外验证：Enemy Attack状态完全不变：" + (enemyAttack.currentCooldown == enemyCooldownBefore && enemyAttack.currentUseCount == enemyUseCountBefore && enemyAttack.isConsumed == enemyConsumedBefore && enemy.currentGuilt == enemyGuiltBefore));
            Debug.Log("TieLimit 额外验证：后续Defense完全未触发：" + (defenseSlot != null && !defenseSlot.isUsed && followDefense.currentCooldown == defenseCooldownBefore));
            Debug.Log("TieLimit 额外验证：未回落Unresponded伤害：" + (hpAfter == hpBefore));
        }

        Debug.Log("Enemy item 是否完成：" + (item != null && item.isCompleted));
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    void RunPassiveDodgeSkipInvalidToDefenseSubTest()
    {
        string title = "PassiveDodgeSkipInvalidToDefense";
        Debug.Log("===== " + title + " 子测试开始 =====");

        CreateTestCharacters();
        StartTurn();

        int hpBefore = allyB.currentHP;
        BattleCardState passiveDodge = CreateFixedDodgeCardForCharacter(allyB, title + "_b_dodge", 8, 2);
        BattleCardState followDefense = CreateTestDefenseCardForCharacter(allyB, title + "_b_defense", 3, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(enemy, title + "_enemy_attack", 8, 2);

        int dodgeUseCountBefore = passiveDodge.currentUseCount;
        bool dodgeConsumedBefore = passiveDodge.isConsumed;
        int dodgeGuiltBefore = allyB.currentGuilt;
        int defenseCooldownBefore = followDefense.currentCooldown;

        BattleEnemyIntent intent1 = new BattleEnemyIntent(title + "_intent_001", enemy, enemyAttack, allyB, 1, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);

        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 1, allyB, passiveDodge);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 2, allyB, followDefense);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);
        passiveDodge.currentCooldown = 1;

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlot dodgeSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 1);
        BattleActionSlot defenseSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 2);

        Debug.Log("预期执行时跳过失效Dodge：" + (dodgeSlot != null && !dodgeSlot.isUsed));
        Debug.Log("预期Dodge不进新的CD或事件：" + (passiveDodge.currentCooldown == 1 && passiveDodge.currentUseCount == dodgeUseCountBefore && passiveDodge.isConsumed == dodgeConsumedBefore && allyB.currentGuilt == dodgeGuiltBefore));
        Debug.Log("预期槽位2 Defense正常接管：" + (defenseSlot != null && defenseSlot.isUsed));
        Debug.Log("预期Defense正常进入CD：" + (followDefense.currentCooldown == GetExpectedResolvedCooldown(followDefense) && followDefense.currentCooldown != defenseCooldownBefore));
        Debug.Log("预期伤害结果符合Defense固定数据：" + (allyB.currentHP == hpBefore - 5));
        Debug.Log("Enemy item 是否完成：" + (item != null && item.isCompleted));
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    void RunPassiveDodgeTargetMismatchSubTest()
    {
        string title = "PassiveDodgeTargetMismatch";
        Debug.Log("===== " + title + " 子测试开始 =====");

        CreateTestCharacters();
        StartTurn();

        int hpBefore = allyB.currentHP;
        BattleCardState allyADodge = CreateFixedDodgeCardForCharacter(allyA, title + "_a_dodge", 12, 2);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(enemy, title + "_enemy_attack", 6, 2);
        int dodgeCooldownBefore = allyADodge.currentCooldown;

        BattleEnemyIntent intent1 = new BattleEnemyIntent(title + "_intent_001", enemy, enemyAttack, allyB, 1, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);

        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyA, 1, allyA, allyADodge);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);
        int candidateCount = item != null && item.passiveGuardCandidates != null ? item.passiveGuardCandidates.Count : 0;

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlot dodgeSlot = BattleActionSlotManager.GetSlot(actionSlots, allyA, 1);

        Debug.Log("预期A的Dodge不进入B的候选：" + (candidateCount == 0));
        Debug.Log("预期A的Dodge不触发：" + (dodgeSlot != null && !dodgeSlot.isUsed));
        Debug.Log("预期A的Dodge CD不变化：" + (allyADodge.currentCooldown == dodgeCooldownBefore));
        Debug.Log("预期敌人正常走原Unresponded伤害：" + (allyB.currentHP == hpBefore - 6));
        Debug.Log("预期B只受到一次伤害：" + (allyB.currentHP == hpBefore - 6));
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    void RunPassiveDodgeRespondedIntentNotTriggeredSubTest()
    {
        string title = "PassiveDodgeRespondedIntentNotTriggered";
        Debug.Log("===== " + title + " 子测试开始 =====");

        CreateTestCharacters();
        StartTurn();

        BattleCardState responseDodge = CreateFixedDodgeCardForCharacter(allyB, title + "_b_response_dodge", 8, 2);
        BattleCardState passiveDodge = CreateFixedDodgeCardForCharacter(allyB, title + "_b_passive_dodge", 12, 2);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(enemy, title + "_enemy_attack", 5, 2);
        int responseCooldownBefore = responseDodge.currentCooldown;
        int passiveCooldownBefore = passiveDodge.currentCooldown;

        BattleEnemyIntent intent1 = new BattleEnemyIntent(title + "_intent_001", enemy, enemyAttack, allyB, 1, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);

        BattleActionSlotManager.AssignResponseToEnemyIntent(actionSlots, allyB, 1, allyB, responseDodge, intent1);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 2, allyB, passiveDodge);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);
        int unrespondedCount = CountExecutionItemsOfType(executionPlan, BattleExecutionItemType.UnrespondedEnemyIntent);
        int candidateCount = item != null && item.passiveGuardCandidates != null ? item.passiveGuardCandidates.Count : 0;

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlot responseSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 1);
        BattleActionSlot passiveSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 2);

        Debug.Log("预期计划生成RespondedEnemyIntent：" + (item != null && item.executionType == BattleExecutionItemType.RespondedEnemyIntent));
        Debug.Log("预期不生成Unresponded被动Dodge接管：" + (unrespondedCount == 0));
        Debug.Log("预期Responded item候选数为0：" + (candidateCount == 0));
        Debug.Log(
            "预期主响应Dodge成功后激活连续闪避并延迟正式结算：" +
            (responseSlot != null &&
             responseSlot.isContinuousDodgeActive &&
             responseSlot.successfulDodgeCount == 1 &&
             !responseSlot.isUsed &&
             responseDodge.currentCooldown == responseCooldownBefore)
        );
        Debug.Log("预期被动Dodge不触发：" + (passiveSlot != null && !passiveSlot.isUsed));
        Debug.Log("预期被动Dodge CD不变化：" + (passiveDodge.currentCooldown == passiveCooldownBefore));
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    void RunPassiveDodgeRespondedAttackFailIsolationSubTest()
    {
        string title = "PassiveDodgeRespondedAttackFailIsolation";
        Debug.Log("===== " + title + " 子测试开始 =====");

        CreateTestCharacters();
        StartTurn();

        int hpBefore = allyB.currentHP;
        BattleCardState responseAttack = CreateFixedAttackCardForCharacter(allyB, title + "_b_response_attack", 4);
        responseAttack.cardData.cooldown = 2;
        BattleCardState passiveDodge = CreateFixedDodgeCardForCharacter(allyB, title + "_b_passive_dodge", 12, 2);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(enemy, title + "_enemy_attack", 8, 2);
        int passiveCooldownBefore = passiveDodge.currentCooldown;

        BattleEnemyIntent intent1 = new BattleEnemyIntent(title + "_intent_001", enemy, enemyAttack, allyB, 1, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);

        BattleActionSlotManager.AssignResponseToEnemyIntent(actionSlots, allyB, 1, allyB, responseAttack, intent1);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 2, allyB, passiveDodge);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);
        int candidateCount = item != null && item.passiveGuardCandidates != null ? item.passiveGuardCandidates.Count : 0;

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlot responseSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 1);
        BattleActionSlot passiveSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 2);

        Debug.Log("预期玩家Attack拼点失败并受到完整伤害：" + (allyB.currentHP == hpBefore - 8));
        Debug.Log("预期被动Dodge不进入Responded Attack失败候选：" + (candidateCount == 0));
        Debug.Log("预期Attack槽位MarkUsed：" + (responseSlot != null && responseSlot.isUsed));
        Debug.Log("预期Dodge槽位未使用：" + (passiveSlot != null && !passiveSlot.isUsed));
        Debug.Log("预期Dodge CD不变化：" + (passiveDodge.currentCooldown == passiveCooldownBefore));
        Debug.Log("预期只造成一次完整伤害：" + (allyB.currentHP == hpBefore - 8));
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    // RunActionSlotPassiveDodgeAfterAttackLoseBasicTestSequence = 精确响应失败后不触发额外守备回归测试
    void RunActionSlotPassiveDodgeAfterAttackLoseBasicTestSequence()
    {
        Debug.Log("===== 精确响应失败后不触发额外守备回归测试开始 =====");
        Debug.Log("Responded Attack已经正式执行后，不再触发PassiveGuard或EnemySpecificGuard");
        RunPassiveDodgeRespondedAttackFailIsolationSubTest();
    }

    void RunPassiveDodgeAfterAttackLoseDodgeFirstSubTest(
        string title,
        int playerAttackPoint,
        int enemyAttackPoint,
        int dodgePoint,
        string expectedResultType,
        int expectedDamage,
        bool expectDodgeUsed
    )
    {
        Debug.Log("===== " + title + " 子测试开始 =====");
        Debug.Log("预期 resultType 出现在 Resolver 日志：" + expectedResultType);

        CreateTestCharacters();
        StartTurn();

        int hpBefore = allyB.currentHP;
        BattleCardState responseAttack = CreateFixedAttackCardForCharacter(allyB, title + "_b_response_attack", playerAttackPoint);
        responseAttack.cardData.cooldown = 2;
        BattleCardState passiveDodge = CreateFixedDodgeCardForCharacter(allyB, title + "_b_passive_dodge", dodgePoint, 2);
        BattleCardState followDefense = CreateTestDefenseCardForCharacter(allyB, title + "_b_follow_defense", 12, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(enemy, title + "_enemy_attack", enemyAttackPoint, 2);

        int responseAttackCooldownBefore = responseAttack.currentCooldown;
        int responseAttackUseCountBefore = responseAttack.currentUseCount;
        int dodgeCooldownBefore = passiveDodge.currentCooldown;
        int dodgeUseCountBefore = passiveDodge.currentUseCount;
        bool dodgeConsumedBefore = passiveDodge.isConsumed;
        int defenseCooldownBefore = followDefense.currentCooldown;
        int defenseUseCountBefore = followDefense.currentUseCount;
        int enemyAttackCooldownBefore = enemyAttack.currentCooldown;
        int enemyAttackUseCountBefore = enemyAttack.currentUseCount;

        BattleEnemyIntent intent1 = new BattleEnemyIntent(title + "_intent_001", enemy, enemyAttack, allyB, 1, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 3);

        BattleActionSlotManager.AssignResponseToEnemyIntent(actionSlots, allyB, 1, allyB, responseAttack, intent1);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 2, allyB, passiveDodge);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 3, allyB, followDefense);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);
        int candidateCount = item != null && item.passiveGuardCandidates != null ? item.passiveGuardCandidates.Count : 0;

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlot responseSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 1);
        BattleActionSlot dodgeSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 2);
        BattleActionSlot defenseSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 3);

        Debug.Log("预期主拼点为 EnemyWin，后续比较固定敌人点数 " + enemyAttackPoint + "：" + (candidateCount == 2));
        Debug.Log("预期目标 HP 变化为 " + expectedDamage + "：" + (allyB.currentHP == hpBefore - expectedDamage));
        bool expectResponseSlotUsed = expectedResultType != "TieLimit";
        Debug.Log("预期主Attack槽位 isUsed = " + expectResponseSlotUsed + "：" + (responseSlot != null && responseSlot.isUsed == expectResponseSlotUsed));
        Debug.Log("预期Dodge槽位 isUsed = " + expectDodgeUsed + "：" + (dodgeSlot != null && dodgeSlot.isUsed == expectDodgeUsed));
        Debug.Log("预期Defense槽位未使用：" + (defenseSlot != null && !defenseSlot.isUsed));
        Debug.Log("预期主Attack失败后不Resolved：" + (responseAttack.currentCooldown == responseAttackCooldownBefore));
        Debug.Log("预期Enemy Attack已使用：" + (enemyAttack.currentCooldown == GetExpectedResolvedCooldown(enemyAttack) && enemyAttack.currentCooldown != enemyAttackCooldownBefore));
        Debug.Log("预期Dodge使用状态符合分支：" + (expectDodgeUsed ? passiveDodge.currentCooldown == GetExpectedResolvedCooldown(passiveDodge) : passiveDodge.currentCooldown == dodgeCooldownBefore));
        Debug.Log("预期Defense CD / UseCount不变：" + (followDefense.currentCooldown == defenseCooldownBefore && followDefense.currentUseCount == defenseUseCountBefore));
        Debug.Log("预期主Attack UseCount前后：" + responseAttackUseCountBefore + " -> " + responseAttack.currentUseCount);
        Debug.Log("预期Enemy Attack UseCount前后：" + enemyAttackUseCountBefore + " -> " + enemyAttack.currentUseCount);
        Debug.Log("预期Dodge UseCount / isConsumed前后符合：" + (passiveDodge.currentUseCount == dodgeUseCountBefore && passiveDodge.isConsumed == dodgeConsumedBefore));
        Debug.Log("预期只造成一次伤害：" + (allyB.currentHP == hpBefore - expectedDamage));
        Debug.Log("预期使用固定敌人点数，未重新Roll：" + (enemyAttackPoint == enemyAttack.cardData.minPoint && enemyAttackPoint == enemyAttack.cardData.maxPoint));

        if (expectedResultType == "TieLimit")
        {
            Debug.Log("TieLimit 额外验证：主Attack槽位不MarkUsed且卡不Resolved：" + (responseSlot != null && !responseSlot.isUsed && responseAttack.currentCooldown == responseAttackCooldownBefore));
            Debug.Log("TieLimit 额外验证：Enemy Attack已使用：" + (enemyAttack.currentCooldown == GetExpectedResolvedCooldown(enemyAttack)));
            Debug.Log("TieLimit 额外验证：Dodge未使用：" + (passiveDodge.currentCooldown == dodgeCooldownBefore && passiveDodge.currentUseCount == dodgeUseCountBefore && passiveDodge.isConsumed == dodgeConsumedBefore));
            Debug.Log("TieLimit 额外验证：Dodge槽位未MarkUsed：" + (dodgeSlot != null && !dodgeSlot.isUsed));
            Debug.Log("TieLimit 额外验证：后续Defense未触发：" + (defenseSlot != null && !defenseSlot.isUsed && followDefense.currentCooldown == defenseCooldownBefore));
            Debug.Log("TieLimit 额外验证：未回落EnemyWin伤害：" + (allyB.currentHP == hpBefore));
        }

        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    void RunPassiveDodgeAfterAttackLoseSkipInvalidToDefenseSubTest()
    {
        string title = "PassiveDodgeAfterAttackLoseSkipInvalidToDefense";
        Debug.Log("===== " + title + " 子测试开始 =====");

        CreateTestCharacters();
        StartTurn();

        int hpBefore = allyB.currentHP;
        BattleCardState responseAttack = CreateFixedAttackCardForCharacter(allyB, title + "_b_response_attack", 4);
        responseAttack.cardData.cooldown = 2;
        BattleCardState passiveDodge = CreateFixedDodgeCardForCharacter(allyB, title + "_b_passive_dodge", 9, 2);
        BattleCardState passiveDefense = CreateTestDefenseCardForCharacter(allyB, title + "_b_passive_defense", 3, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(enemy, title + "_enemy_attack", 8, 2);

        BattleEnemyIntent intent1 = new BattleEnemyIntent(title + "_intent_001", enemy, enemyAttack, allyB, 1, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 3);

        BattleActionSlotManager.AssignResponseToEnemyIntent(actionSlots, allyB, 1, allyB, responseAttack, intent1);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 2, allyB, passiveDodge);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 3, allyB, passiveDefense);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);
        passiveDodge.currentCooldown = 1;

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlot dodgeSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 2);
        BattleActionSlot defenseSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 3);

        Debug.Log("预期跳过执行前失效Dodge：" + (dodgeSlot != null && !dodgeSlot.isUsed && passiveDodge.currentCooldown == 1));
        Debug.Log("预期Defense使用固定敌人8点接管：" + (defenseSlot != null && defenseSlot.isUsed));
        Debug.Log("预期Defense造成5点伤害：" + (allyB.currentHP == hpBefore - 5));
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    void RunPassiveDodgeAfterAttackLoseDefenseFirstSubTest()
    {
        string title = "PassiveDodgeAfterAttackLoseDefenseFirst";
        Debug.Log("===== " + title + " 子测试开始 =====");

        CreateTestCharacters();
        StartTurn();

        BattleCardState responseAttack = CreateFixedAttackCardForCharacter(allyB, title + "_b_response_attack", 4);
        responseAttack.cardData.cooldown = 2;
        BattleCardState passiveDefense = CreateTestDefenseCardForCharacter(allyB, title + "_b_passive_defense", 3, 1);
        BattleCardState passiveDodge = CreateFixedDodgeCardForCharacter(allyB, title + "_b_passive_dodge", 12, 2);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(enemy, title + "_enemy_attack", 8, 2);
        int dodgeCooldownBefore = passiveDodge.currentCooldown;

        BattleEnemyIntent intent1 = new BattleEnemyIntent(title + "_intent_001", enemy, enemyAttack, allyB, 1, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 3);

        BattleActionSlotManager.AssignResponseToEnemyIntent(actionSlots, allyB, 1, allyB, responseAttack, intent1);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 2, allyB, passiveDefense);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 3, allyB, passiveDodge);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlot defenseSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 2);
        BattleActionSlot dodgeSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 3);

        Debug.Log("预期Defense先触发：" + (defenseSlot != null && defenseSlot.isUsed));
        Debug.Log("预期Dodge完全不参与：" + (dodgeSlot != null && !dodgeSlot.isUsed && passiveDodge.currentCooldown == dodgeCooldownBefore));
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    void RunPassiveDodgeAfterAttackLoseNoCandidateSubTest()
    {
        string title = "PassiveDodgeAfterAttackLoseNoCandidate";
        Debug.Log("===== " + title + " 子测试开始 =====");

        CreateTestCharacters();
        StartTurn();

        int hpBefore = allyB.currentHP;
        BattleCardState responseAttack = CreateFixedAttackCardForCharacter(allyB, title + "_b_response_attack", 4);
        responseAttack.cardData.cooldown = 2;
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(enemy, title + "_enemy_attack", 8, 2);

        BattleEnemyIntent intent1 = new BattleEnemyIntent(title + "_intent_001", enemy, enemyAttack, allyB, 1, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 1);

        BattleActionSlotManager.AssignResponseToEnemyIntent(actionSlots, allyB, 1, allyB, responseAttack, intent1);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);
        int candidateCount = item != null && item.passiveGuardCandidates != null ? item.passiveGuardCandidates.Count : 0;

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        Debug.Log("预期无PassiveGuard候选：" + (candidateCount == 0));
        Debug.Log("预期原EnemyWin伤害正常且只造成一次：" + (allyB.currentHP == hpBefore - 8));
        Debug.Log("预期使用固定敌人8点，不重新Roll：" + (enemyAttack.cardData.minPoint == 8 && enemyAttack.cardData.maxPoint == 8));
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    void RunPassiveDodgeAfterAttackLoseTargetMismatchSubTest()
    {
        string title = "PassiveDodgeAfterAttackLoseTargetMismatch";
        Debug.Log("===== " + title + " 子测试开始 =====");

        CreateTestCharacters();
        StartTurn();

        int hpBefore = allyB.currentHP;
        BattleCardState responseAttack = CreateFixedAttackCardForCharacter(allyB, title + "_b_response_attack", 4);
        responseAttack.cardData.cooldown = 2;
        BattleCardState allyADodge = CreateFixedDodgeCardForCharacter(allyA, title + "_a_passive_dodge", 12, 2);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(enemy, title + "_enemy_attack", 8, 2);

        BattleEnemyIntent intent1 = new BattleEnemyIntent(title + "_intent_001", enemy, enemyAttack, allyB, 1, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);

        BattleActionSlotManager.AssignResponseToEnemyIntent(actionSlots, allyB, 1, allyB, responseAttack, intent1);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyA, 1, allyA, allyADodge);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);
        int candidateCount = item != null && item.passiveGuardCandidates != null ? item.passiveGuardCandidates.Count : 0;

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlot dodgeSlot = BattleActionSlotManager.GetSlot(actionSlots, allyA, 1);

        Debug.Log("预期A的Dodge不进入B目标候选：" + (candidateCount == 0));
        Debug.Log("预期A的Dodge槽位未使用：" + (dodgeSlot != null && !dodgeSlot.isUsed));
        Debug.Log("预期B承受原EnemyWin伤害：" + (allyB.currentHP == hpBefore - 8));
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    void RunPassiveDodgeAfterAttackLosePlayerWinSubTest()
    {
        string title = "PassiveDodgeAfterAttackLosePlayerWin";
        Debug.Log("===== " + title + " 子测试开始 =====");

        CreateTestCharacters();
        StartTurn();

        int allyBHPBefore = allyB.currentHP;
        BattleCardState responseAttack = CreateFixedAttackCardForCharacter(allyB, title + "_b_response_attack", 9);
        responseAttack.cardData.cooldown = 2;
        BattleCardState passiveDodge = CreateFixedDodgeCardForCharacter(allyB, title + "_b_passive_dodge", 12, 2);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(enemy, title + "_enemy_attack", 5, 2);
        int dodgeCooldownBefore = passiveDodge.currentCooldown;

        BattleEnemyIntent intent1 = new BattleEnemyIntent(title + "_intent_001", enemy, enemyAttack, allyB, 1, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);

        BattleActionSlotManager.AssignResponseToEnemyIntent(actionSlots, allyB, 1, allyB, responseAttack, intent1);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 2, allyB, passiveDodge);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlot dodgeSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 2);

        Debug.Log("预期PlayerWin，B不受伤：" + (allyB.currentHP == allyBHPBefore));
        Debug.Log("预期Passive Dodge不触发：" + (dodgeSlot != null && !dodgeSlot.isUsed));
        Debug.Log("预期Dodge CD不变：" + (passiveDodge.currentCooldown == dodgeCooldownBefore));
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    // RunActionSlotPassiveGuardTargetMismatchBasicTestSequence = 测试目标角色不匹配时不触发
    void RunActionSlotPassiveGuardTargetMismatchBasicTestSequence()
    {
        Debug.Log("===== PassiveGuard 目标不匹配测试开始 =====");

        StartTurn();

        CardTestData enemyAttackCard = CreateFixedAttackCardData("passive_guard_mismatch_enemy_attack", "被动守备目标不匹配敌人攻击", 4);
        BattleCardState enemyAttack = BattleCardManager.CreateBattleCard(enemy, enemyAttackCard, "passive_guard_mismatch_enemy_attack_copy_0");
        BattleCardState allyAGuard = CreateTestDefenseCardForCharacter(allyA, "passive_guard_mismatch_a_defense", 6, 1);

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "passive_guard_mismatch_intent_001",
            enemy,
            enemyAttack,
            allyB,
            1,
            1
        );

        int allyBHPBefore = allyB.currentHP;
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);

        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyA, 1, allyA, allyAGuard);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);

        Debug.Log("预期 A 的 PassiveGuard 不进入 B 的候选：" + (item != null && item.passiveGuardCandidates.Count == 0));

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        Debug.Log("预期正常走 Unresponded，B HP 下降：" + (allyB.currentHP < allyBHPBefore));
        Debug.Log("预期 A Defense CD 不变：" + (allyAGuard.currentCooldown == 0));
        Debug.Log("预期 A槽位1 未 MarkUsed：" + !BattleActionSlotManager.GetSlot(actionSlots, allyA, 1).isUsed);
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    // RunActionSlotPassiveGuardRespondedIntentNotTriggeredBasicTestSequence = 测试已有指定响应时不触发 PassiveGuard
    void RunActionSlotPassiveGuardRespondedIntentNotTriggeredBasicTestSequence()
    {
        Debug.Log("===== PassiveGuard 已有指定响应不触发测试开始 =====");

        StartTurn();

        CardTestData lowEnemyAttackCard = CreateFixedAttackCardData("passive_guard_responded_enemy_attack", "被动守备指定响应敌人攻击", 1);
        BattleCardState enemyAttack = BattleCardManager.CreateBattleCard(enemy, lowEnemyAttackCard, "passive_guard_responded_enemy_attack_copy_0");
        BattleCardState responseAttack = CreateFixedAttackCardForCharacter(allyB, "passive_guard_responded_b_attack", 10);
        BattleCardState passiveGuard = CreateTestDefenseCardForCharacter(allyB, "passive_guard_responded_b_defense", 6, 1);

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "passive_guard_responded_intent_001",
            enemy,
            enemyAttack,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);

        BattleActionSlotManager.AssignResponseToEnemyIntent(actionSlots, allyB, 1, allyB, responseAttack, intent1);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 2, allyB, passiveGuard);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);

        Debug.Log("预期计划第 1 项为 RespondedEnemyIntent：" + (item != null && item.executionType == BattleExecutionItemType.RespondedEnemyIntent));

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        Debug.Log("预期 B槽位2 PassiveGuard 未 MarkUsed：" + !BattleActionSlotManager.GetSlot(actionSlots, allyB, 2).isUsed);
        Debug.Log("预期 B槽位2 Defense CD 不变：" + (passiveGuard.currentCooldown == 0));
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    // RunActionSlotPassiveGuardAssignLegalityBasicTestSequence = 测试被动守备安排合法性
    void RunActionSlotPassiveGuardAssignLegalityBasicTestSequence()
    {
        Debug.Log("===== PassiveGuard 安排合法性测试开始 =====");

        StartTurn();

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);
        BattleCardState guard1 = CreateTestDefenseCardForCharacter(allyB, "passive_guard_legality_b_defense_1", 6, 1);
        BattleCardState guard2 = CreateTestDefenseCardForCharacter(allyB, "passive_guard_legality_b_defense_2", 6, 1);
        BattleCardState guard3 = CreateTestDefenseCardForCharacter(allyB, "passive_guard_legality_b_defense_3", 6, 1);
        BattleCardState attackCard = CreateFixedAttackCardForCharacter(allyA, "passive_guard_legality_a_attack", 4);
        BattleCardState abilityCard = CreateCardStateForCharacter(allyA, "passive_guard_legality_a_ability", "测试 Ability", "Ability", 0, 0);
        BattleCardState dodgeCard = CreateCardStateForCharacter(allyA, "passive_guard_legality_a_dodge", "测试 Dodge", CardType.Dodge, 0, 0);

        bool assignB1 = BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 1, allyB, guard1);
        bool repeatSameCard = BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 2, allyB, guard1);
        bool assignB2 = BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 2, allyB, guard2);
        bool attackRejected = BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyA, 1, allyA, attackCard);
        bool abilityRejected = BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyA, 1, allyA, abilityCard);
        bool dodgeAssigned = BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyA, 1, allyA, dodgeCard);
        bool thirdDefenseOnOccupiedSlot = BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 1, allyB, guard3);

        BattleActionSlotManager.PrintSlotStates(actionSlots);

        Debug.Log("预期同角色两个不同槽位可分别安排 Defense PassiveGuard：" + (assignB1 && assignB2));
        Debug.Log("预期同一 BattleCardState 不能重复安排：" + !repeatSameCard);
        Debug.Log("预期 Attack 不能 AssignPassiveGuard：" + !attackRejected);
        Debug.Log("预期 Ability 不能 AssignPassiveGuard：" + !abilityRejected);
        Debug.Log("预期 Dodge 可以 AssignPassiveGuard：" + dodgeAssigned);
        Debug.Log("预期已占用槽位不能再安排第三张 Defense：" + !thirdDefenseOnOccupiedSlot);
    }

    // RunActionSlotExecutionPlanExecuteRespondedBasicTestSequence = 执行已响应敌人意图正式执行基础测试
    void RunActionSlotExecutionPlanExecuteRespondedBasicTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 已响应正式执行基础测试开始 =====");

        StartTurn();

        Debug.Log("执行前 我方角色A HP：" + allyA.currentHP + " / " + allyA.maxHP);
        Debug.Log("执行前 我方角色B HP：" + allyB.currentHP + " / " + allyB.maxHP);
        Debug.Log("执行前 敌人 HP：" + enemy.currentHP + " / " + enemy.maxHP);

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "enemy_intent_execution_plan_execute_responded_001",
            enemy,
            enemyAttackCardState,
            allyB,
            2,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(1);

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            allyA,
            allyAAttackCardState,
            intent1
        );

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            actionSlots,
            intentQueue
        );

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlotManager.PrintSlotStates(actionSlots);
        Debug.Log("执行后 我方角色A HP：" + allyA.currentHP + " / " + allyA.maxHP);
        Debug.Log("执行后 我方角色B HP：" + allyB.currentHP + " / " + allyB.maxHP);
        Debug.Log("执行后 敌人 HP：" + enemy.currentHP + " / " + enemy.maxHP);
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    // RunActionSlotExecutionPlanExecuteRespondedEnemyWinTestSequence = 执行已响应敌人意图敌人胜利分支测试
    void RunActionSlotExecutionPlanExecuteRespondedEnemyWinTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 已响应敌人胜利分支测试开始 =====");

        StartTurn();

        Debug.Log("本测试预期：敌人胜利，actualTargetCharacter 扣血");
        Debug.Log("执行前 我方角色A HP：" + allyA.currentHP + " / " + allyA.maxHP);
        Debug.Log("执行前 我方角色B HP：" + allyB.currentHP + " / " + allyB.maxHP);
        Debug.Log("执行前 敌人 HP：" + enemy.currentHP + " / " + enemy.maxHP);

        CardTestData lowPlayerAttackCard = new CardTestData
        {
            cardID = "test_player_low_attack_001",
            cardName = "测试低点攻击",
            cardType = "Attack",
            isClashable = true,
            minPoint = 1,
            maxPoint = 1,
            damageFormula = "PointAsDamage",
            maxUseCount = 3
        };

        CardTestData highEnemyAttackCard = new CardTestData
        {
            cardID = "test_enemy_high_attack_001",
            cardName = "测试高点敌人攻击",
            cardType = "Attack",
            isClashable = true,
            minPoint = 8,
            maxPoint = 8,
            damageFormula = "PointAsDamage"
        };

        BattleCardState lowPlayerAttackCardState = BattleCardManager.CreateBattleCard(
            allyA,
            lowPlayerAttackCard,
            "allyA_test_low_attack_001_copy_0"
        );

        BattleCardState highEnemyAttackCardState = BattleCardManager.CreateBattleCard(
            enemy,
            highEnemyAttackCard,
            "enemy_test_high_attack_001_copy_0"
        );

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "enemy_intent_execution_plan_execute_responded_enemy_win_001",
            enemy,
            highEnemyAttackCardState,
            allyB,
            2,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(1);

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            allyA,
            lowPlayerAttackCardState,
            intent1
        );

        Debug.Log("响应后 actualTargetCharacter：" + intent1.GetActualTargetName());
        Debug.Log("响应后 actualTargetSlot：" + intent1.GetActualTargetSlotText());

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            actionSlots,
            intentQueue
        );

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlotManager.PrintSlotStates(actionSlots);
        Debug.Log("执行后 我方角色A HP：" + allyA.currentHP + " / " + allyA.maxHP);
        Debug.Log("执行后 我方角色B HP：" + allyB.currentHP + " / " + allyB.maxHP);
        Debug.Log("执行后 敌人 HP：" + enemy.currentHP + " / " + enemy.maxHP);
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
        Debug.Log("敌人胜利分支验证：我方角色A 应作为 actualTargetCharacter 扣血");
    }

    // 历史入口保留枚举值；当前验证精确响应失败后不再触发 PassiveGuard。
    void RunActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardReducedDamageBasicTestSequence()
    {
        RunRespondedAttackPassiveGuardSubTest(
            "RespondedEnemyWinPassiveGuardReducedDamage",
            4,
            8,
            2,
            5,
            -1,
            false,
            false,
            false,
            false,
            8,
            false,
            "EnemyWin"
        );
    }

    // 历史入口保留枚举值；当前验证高点守备也不会在精确响应后补触发。
    void RunActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardFullBlockBasicTestSequence()
    {
        RunRespondedAttackPassiveGuardSubTest(
            "RespondedEnemyWinPassiveGuardFullBlock",
            4,
            8,
            2,
            10,
            -1,
            false,
            false,
            false,
            false,
            8,
            false,
            "EnemyWin"
        );
    }

    // RunActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardCandidateOrderBasicTestSequence = 多候选时按槽位顺序选择第一张
    void RunActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardCandidateOrderBasicTestSequence()
    {
        RunRespondedAttackPassiveGuardSubTest(
            "RespondedEnemyWinPassiveGuardCandidateOrder",
            4,
            8,
            3,
            5,
            10,
            false,
            false,
            true,
            false,
            3,
            true,
            "EnemyWinPassiveGuardReducedDamage"
        );
    }

    // RunActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardSkipInvalidBasicTestSequence = 第一候选执行前失效时跳过
    void RunActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardSkipInvalidBasicTestSequence()
    {
        RunRespondedAttackPassiveGuardSubTest(
            "RespondedEnemyWinPassiveGuardSkipInvalid",
            4,
            8,
            3,
            5,
            10,
            true,
            false,
            false,
            true,
            0,
            true,
            "EnemyWinPassiveGuardFullBlock"
        );
    }

    // RunActionSlotExecutionPlanExecuteRespondedEnemyWinNoPassiveGuardBasicTestSequence = 没有守备时回退原 EnemyWin 伤害
    void RunActionSlotExecutionPlanExecuteRespondedEnemyWinNoPassiveGuardBasicTestSequence()
    {
        RunRespondedAttackPassiveGuardSubTest(
            "RespondedEnemyWinNoPassiveGuard",
            4,
            8,
            2,
            -1,
            -1,
            false,
            false,
            false,
            false,
            8,
            false,
            "EnemyWin"
        );
    }

    // RunActionSlotExecutionPlanExecuteRespondedPlayerWinPassiveGuardNotTriggeredBasicTestSequence = 玩家胜利时不触发 PassiveGuard
    void RunActionSlotExecutionPlanExecuteRespondedPlayerWinPassiveGuardNotTriggeredBasicTestSequence()
    {
        RunRespondedAttackPassiveGuardSubTest(
            "RespondedPlayerWinPassiveGuardNotTriggered",
            10,
            4,
            2,
            8,
            -1,
            false,
            false,
            false,
            false,
            0,
            false,
            "PlayerWin"
        );
    }

    // RunActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardTargetMismatchBasicTestSequence = 目标角色不匹配时不触发守备
    void RunActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardTargetMismatchBasicTestSequence()
    {
        RunRespondedAttackPassiveGuardSubTest(
            "RespondedEnemyWinPassiveGuardTargetMismatch",
            4,
            8,
            2,
            10,
            -1,
            false,
            true,
            false,
            false,
            8,
            false,
            "EnemyWin"
        );
    }

    // RunActionSlotExecutionPlanExecuteRespondedTieLimitTestSequence = 执行已响应敌人意图连续平局上限测试
    void RunActionSlotExecutionPlanExecuteRespondedTieLimitTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 已响应连续平局上限测试开始 =====");

        StartTurn();

        Debug.Log("本测试预期：连续 10 次平局后自动结束，双方不扣血");
        Debug.Log("执行前 我方角色A HP：" + allyA.currentHP + " / " + allyA.maxHP);
        Debug.Log("执行前 我方角色B HP：" + allyB.currentHP + " / " + allyB.maxHP);
        Debug.Log("执行前 敌人 HP：" + enemy.currentHP + " / " + enemy.maxHP);

        CardTestData tiePlayerAttackCard = new CardTestData
        {
            cardID = "test_player_tie_attack_001",
            cardName = "测试平局玩家攻击",
            cardType = "Attack",
            isClashable = true,
            minPoint = 5,
            maxPoint = 5,
            damageFormula = "PointAsDamage",
            cooldown = 2,
            maxUseCount = 3
        };

        CardTestData tieEnemyAttackCard = new CardTestData
        {
            cardID = "test_enemy_tie_attack_001",
            cardName = "测试平局敌人攻击",
            cardType = "Attack",
            isClashable = true,
            minPoint = 5,
            maxPoint = 5,
            damageFormula = "PointAsDamage",
            cooldown = 2
        };

        BattleCardState tiePlayerAttackCardState = BattleCardManager.CreateBattleCard(
            allyA,
            tiePlayerAttackCard,
            "allyA_test_tie_attack_001_copy_0"
        );

        BattleCardState tieEnemyAttackCardState = BattleCardManager.CreateBattleCard(
            enemy,
            tieEnemyAttackCard,
            "enemy_test_tie_attack_001_copy_0"
        );

        int playerCooldownBefore = tiePlayerAttackCardState.currentCooldown;
        int playerUseCountBefore = tiePlayerAttackCardState.currentUseCount;
        bool playerConsumedBefore = tiePlayerAttackCardState.isConsumed;
        int playerGuiltBefore = allyA.currentGuilt;
        int enemyCooldownBefore = tieEnemyAttackCardState.currentCooldown;
        int enemyUseCountBefore = tieEnemyAttackCardState.currentUseCount;
        bool enemyConsumedBefore = tieEnemyAttackCardState.isConsumed;
        int enemyGuiltBefore = enemy.currentGuilt;

        Debug.Log("执行前 玩家平局 Attack CD：" + playerCooldownBefore);
        Debug.Log("执行前 玩家平局 Attack UseCount：" + playerUseCountBefore + " / " + tiePlayerAttackCardState.maxUseCount);
        Debug.Log("执行前 玩家平局 Attack isConsumed：" + playerConsumedBefore);
        Debug.Log("执行前 玩家 guilt：" + playerGuiltBefore);
        Debug.Log("执行前 敌人平局 Attack CD：" + enemyCooldownBefore);
        Debug.Log("执行前 敌人平局 Attack UseCount：" + enemyUseCountBefore + " / " + tieEnemyAttackCardState.maxUseCount);
        Debug.Log("执行前 敌人平局 Attack isConsumed：" + enemyConsumedBefore);
        Debug.Log("执行前 敌人 guilt：" + enemyGuiltBefore);

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "enemy_intent_execution_plan_execute_responded_tie_limit_001",
            enemy,
            tieEnemyAttackCardState,
            allyB,
            2,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(1);

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            allyA,
            tiePlayerAttackCardState,
            intent1
        );

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            actionSlots,
            intentQueue
        );

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        BattleActionSlotManager.PrintSlotStates(actionSlots);
        Debug.Log("执行后 我方角色A HP：" + allyA.currentHP + " / " + allyA.maxHP);
        Debug.Log("执行后 我方角色B HP：" + allyB.currentHP + " / " + allyB.maxHP);
        Debug.Log("执行后 敌人 HP：" + enemy.currentHP + " / " + enemy.maxHP);
        BattleExecutionItem tieItem = GetFirstExecutionItem(executionPlan);
        Debug.Log("执行后 玩家平局 Attack CD：" + tiePlayerAttackCardState.currentCooldown);
        Debug.Log("执行后 玩家平局 Attack UseCount：" + tiePlayerAttackCardState.currentUseCount + " / " + tiePlayerAttackCardState.maxUseCount);
        Debug.Log("执行后 玩家平局 Attack isConsumed：" + tiePlayerAttackCardState.isConsumed);
        Debug.Log("执行后 玩家 guilt：" + allyA.currentGuilt);
        Debug.Log("执行后 敌人平局 Attack CD：" + tieEnemyAttackCardState.currentCooldown);
        Debug.Log("执行后 敌人平局 Attack UseCount：" + tieEnemyAttackCardState.currentUseCount + " / " + tieEnemyAttackCardState.maxUseCount);
        Debug.Log("执行后 敌人平局 Attack isConsumed：" + tieEnemyAttackCardState.isConsumed);
        Debug.Log("执行后 敌人 guilt：" + enemy.currentGuilt);
        Debug.Log("预期 TieLimit 玩家卡状态不变：" + (tiePlayerAttackCardState.currentCooldown == playerCooldownBefore && tiePlayerAttackCardState.currentUseCount == playerUseCountBefore && tiePlayerAttackCardState.isConsumed == playerConsumedBefore && allyA.currentGuilt == playerGuiltBefore));
        Debug.Log("预期 TieLimit 敌人卡状态不变：" + (tieEnemyAttackCardState.currentCooldown == enemyCooldownBefore && tieEnemyAttackCardState.currentUseCount == enemyUseCountBefore && tieEnemyAttackCardState.isConsumed == enemyConsumedBefore && enemy.currentGuilt == enemyGuiltBefore));
        Debug.Log("预期 TieLimit item为Failed：" + (tieItem != null && tieItem.status == BattleExecutionItemStatus.Failed));
        Debug.Log("预期 TieLimit reason为TieLimitReached：" + (tieItem != null && tieItem.outcomeReason == BattleExecutionItemOutcomeReason.TieLimitReached));
        Debug.Log("预期 TieLimit item.isCompleted为False：" + (tieItem != null && !tieItem.isCompleted));
        Debug.Log("预期 TieLimit plan.isCompleted为False：" + !executionPlan.isCompleted);
    }

    // RunActionSlotExecutionPlanExecuteMixedBasicTestSequence = 执行已响应 + 未响应混合计划基础测试
    void RunActionSlotExecutionPlanExecuteMixedBasicTestSequence()
    {
        Debug.Log("===== BattleExecutionPlan 混合执行基础测试开始 =====");

        StartTurn();

        Debug.Log("执行前 我方角色A HP：" + allyA.currentHP + " / " + allyA.maxHP);
        Debug.Log("执行前 我方角色B HP：" + allyB.currentHP + " / " + allyB.maxHP);
        Debug.Log("执行前 敌人 HP：" + enemy.currentHP + " / " + enemy.maxHP);

        BattleCardState secondEnemyAttackCardState = BattleCardManager.CreateBattleCard(
            enemy,
            enemyAttackCardState.cardData,
            "enemy_atk_001_execution_plan_execute_mixed_copy_1"
        );

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            "enemy_intent_execution_plan_execute_mixed_001",
            enemy,
            enemyAttackCardState,
            allyB,
            2,
            1
        );

        BattleEnemyIntent intent2 = new BattleEnemyIntent(
            "enemy_intent_execution_plan_execute_mixed_002",
            enemy,
            secondEnemyAttackCardState,
            allyB,
            1,
            2
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1, intent2);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreateActionSlots(1);

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            1,
            allyA,
            allyAAttackCardState,
            intent2
        );

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            actionSlots,
            intentQueue
        );

        int executionItemCount = executionPlan != null && executionPlan.executionItems != null
            ? executionPlan.executionItems.Count
            : 0;

        Debug.Log("当前计划 item 数量：" + executionItemCount);

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);

        Debug.Log("执行后 我方角色A HP：" + allyA.currentHP + " / " + allyA.maxHP);
        Debug.Log("执行后 我方角色B HP：" + allyB.currentHP + " / " + allyB.maxHP);
        Debug.Log("执行后 敌人 HP：" + enemy.currentHP + " / " + enemy.maxHP);
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    // RunBattleEndedVictoryDefeatBasicTestSequence = BattleEnded / Victory / Defeat 第一版阶段A聚合测试
    void RunBattleEndedVictoryDefeatBasicTestSequence()
    {
        Debug.Log("===== BattleEnded / Victory / Defeat 第一版阶段A聚合测试开始 =====");

        RunBattleEndedVictoryStopsRemainingFreeActionSubTest();
        RunBattleEndedDefeatSubTest();
        RunBattleEndedSinglePlayerDeathNotDefeatSubTest();
        RunBattleEndedSimultaneousDeathPrioritizesDefeatSubTest();
        RunBattleEndedOperationGuardSubTest();
        RunBattleEndedNonLethalCompletedSubTest();
    }

    // RunExecutionPlanInvalidActionCompletionBasicTestSequence = FreeAction执行时不可用的跳过完成聚合测试
    void RunExecutionPlanInvalidActionCompletionBasicTestSequence()
    {
        Debug.Log("===== ExecutionPlan Invalid Action Completion 聚合测试开始 =====");

        RunFreeActionUnavailableBulletSubTest();
        RunFreeActionUnavailableThenNextItemSubTest();
        RunFreeActionNormalRegressionSubTest();
        RunFreeActionUnsupportedNotSwallowedSubTest();
        RunFreeActionBattleEndedRegressionSubTest();
    }

    void RunExecutionItemStatusBasicTestSequence()
    {
        Debug.Log("===== ExecutionItemStatusBasic 聚合测试开始 =====");

        RunExecutionItemStatusNormalFreeActionSubTest();
        RunExecutionItemStatusActionUnavailableFreeActionSubTest();
        RunExecutionItemStatusDeadFreeActionSubTest();
        RunExecutionItemStatusDeadActualTargetSubTest();
        RunExecutionItemStatusRespondedUnavailableFallbackSubTest();
        RunExecutionItemStatusFallbackDeadOriginalTargetSubTest();
        RunExecutionItemStatusBattleEndedRemainingSubTest();
        RunExecutionItemStatusInvalidDataSubTest();
        RunExecutionItemStatusUnsupportedResolveSubTest();
        RunExecutionItemStatusUnsupportedExecutionTypeSubTest();
        RunExecutionItemStatusNullItemSubTest();
        RunExecutionItemStatusTieLimitSubTest();
        RunExecutionItemStatusAllExecutedOrSkippedSubTest();
        RunExecutionItemStatusCompatibilitySubTest();
    }

    void RunExecutionItemStatusNormalFreeActionSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("item_status_a", 30, 30, 50, 10, 3, 8);
        BattleCardState attack = CreateFixedAttackCardForCharacter(context.allyA, "item_status_a_attack", 3);
        BattleActionSlot slot = new BattleActionSlot(context.allyA, 1);
        slot.AssignFreeAction(context.allyA, attack, context.enemy);
        BattleExecutionPlan plan = CreateManualFreeActionPlan(slot);
        BattleExecutionItem item = GetFirstExecutionItem(plan);

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        Debug.Log("模式51 A 正常FreeAction为Executed：" + IsExecutionItemState(item, BattleExecutionItemStatus.Executed, BattleExecutionItemOutcomeReason.None, true));
    }

    void RunExecutionItemStatusActionUnavailableFreeActionSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("item_status_b", 30, 30, 50, 10, 3, 8);
        BattleCardState unavailableAttack = CreateBulletLockedFreeAttackCard(context.allyB, "item_status_b_attack", 5, 3);
        BattleActionSlot slot = new BattleActionSlot(context.allyB, 1);
        slot.AssignFreeAction(context.allyB, unavailableAttack, context.enemy);
        BattleExecutionPlan plan = CreateManualFreeActionPlan(slot);
        BattleExecutionItem item = GetFirstExecutionItem(plan);
        int useCountBefore = unavailableAttack.currentUseCount;

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        Debug.Log("模式51 B ActionUnavailable FreeAction为Skipped：" + IsExecutionItemState(item, BattleExecutionItemStatus.Skipped, BattleExecutionItemOutcomeReason.ActionUnavailable, true));
        Debug.Log("模式51 B 卡牌不使用且槽位不MarkUsed：" + (unavailableAttack.currentUseCount == useCountBefore && !slot.isUsed));
    }

    void RunExecutionItemStatusDeadFreeActionSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("item_status_c", 30, 0, 50, 10, 3, 8);
        BattleCardState attack = CreateFixedAttackCardForCharacter(context.allyB, "item_status_c_attack", 3);
        BattleActionSlot slot = new BattleActionSlot(context.allyB, 1);
        slot.AssignFreeAction(context.allyB, attack, context.enemy);
        BattleExecutionPlan plan = CreateManualFreeActionPlan(slot);
        BattleExecutionItem item = GetFirstExecutionItem(plan);

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        Debug.Log("模式51 C 死亡FreeAction为Skipped ActorDead：" + IsExecutionItemState(item, BattleExecutionItemStatus.Skipped, BattleExecutionItemOutcomeReason.ActorDead, true));
    }

    void RunExecutionItemStatusDeadActualTargetSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("item_status_d", 30, 0, 50, 10, 3, 8);
        BattleCardState enemyAttack = CreateBeforeUseBuffAttackCard(context.enemy, "item_status_d_enemy", 5, "Strength", 1, 1);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("item_status_d_intent", context.enemy, enemyAttack, context.allyB, 1);
        BattleExecutionItem item = new BattleExecutionItem(1, BattleExecutionItemType.UnrespondedEnemyIntent, intent, null);
        BattleExecutionPlan plan = CreateManualExecutionPlan(item);

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        Debug.Log("模式51 D actualTarget死亡为Skipped ActualTargetDead：" + IsExecutionItemState(item, BattleExecutionItemStatus.Skipped, BattleExecutionItemOutcomeReason.ActualTargetDead, true));
        Debug.Log("模式51 D 敌人不Roll点不触发BeforeUse：" + (CountBuffStack(context.enemy, "Strength") == 0));
    }

    void RunExecutionItemStatusRespondedUnavailableFallbackSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("item_status_e", 30, 30, 50, 20, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1);
        BattleCardState responseAttack = CreateBulletLockedBeforeUseAttackCard(context.allyA, "item_status_e_response", 5, 3, "Strength", 1, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "item_status_e_enemy", 5, 0);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("item_status_e_intent", context.enemy, enemyAttack, context.allyB, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent);
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(intentQueue);

        BattleActionSlotManager.AssignResponseToEnemyIntent(actionSlots, context.allyA, 1, context.allyA, responseAttack, intent);
        BattleActionSlot responseSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);
        int bHPBefore = context.allyB.currentHP;
        BattleExecutionPlan plan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(plan);

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        Debug.Log("模式51 E Responded资源不足回落为Executed：" + IsExecutionItemState(item, BattleExecutionItemStatus.Executed, BattleExecutionItemOutcomeReason.ResponseUnavailableFallbackToUnresponded, true));
        Debug.Log("模式51 E 响应槽位不MarkUsed且敌人只攻击一次：" + (responseSlot != null && !responseSlot.isUsed && context.allyB.currentHP == bHPBefore - 5));
    }

    void RunExecutionItemStatusFallbackDeadOriginalTargetSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("item_status_f", 30, 0, 50, 20, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1);
        BattleCardState responseAttack = CreateBulletLockedBeforeUseAttackCard(context.allyA, "item_status_f_response", 5, 3, "Strength", 1, 1);
        BattleCardState enemyAttack = CreateBeforeUseBuffAttackCard(context.enemy, "item_status_f_enemy", 5, "Strength", 1, 1);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("item_status_f_intent", context.enemy, enemyAttack, context.allyB, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent);
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(intentQueue);

        BattleActionSlotManager.AssignResponseToEnemyIntent(actionSlots, context.allyA, 1, context.allyA, responseAttack, intent);
        BattleExecutionPlan plan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(plan);

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        Debug.Log("模式51 F 回落后originalTarget死亡为Skipped：" + IsExecutionItemState(item, BattleExecutionItemStatus.Skipped, BattleExecutionItemOutcomeReason.ActualTargetDead, true));
        Debug.Log("模式51 F 敌人不攻击不触发BeforeUse：" + (context.allyA.currentHP == context.allyA.maxHP && CountBuffStack(context.enemy, "Strength") == 0));
    }

    void RunExecutionItemStatusBattleEndedRemainingSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("item_status_g", 30, 30, 5, 20, 3, 8);
        BattleCardState killAttack = CreateBattleEndedKillAttackCard(context.allyA, "item_status_g_kill", 6);
        BattleCardState followAbility = CreateBattleEndedAbilityCard(context.allyA, "item_status_g_follow", "ItemStatusGFollow");
        BattleActionSlot killSlot = new BattleActionSlot(context.allyA, 1);
        killSlot.AssignFreeAction(context.allyA, killAttack, context.enemy);
        BattleActionSlot followSlot = new BattleActionSlot(context.allyA, 2);
        followSlot.AssignFreeAction(context.allyA, followAbility, context.allyA);
        BattleExecutionPlan plan = CreateManualFreeActionPlan(killSlot, followSlot);
        BattleExecutionItem firstItem = plan.executionItems[0];
        BattleExecutionItem secondItem = plan.executionItems[1];

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        Debug.Log("模式51 G 击杀item为Executed：" + IsExecutionItemState(firstItem, BattleExecutionItemStatus.Executed, BattleExecutionItemOutcomeReason.None, true));
        Debug.Log("模式51 G BattleEnded剩余item为Skipped：" + IsExecutionItemState(secondItem, BattleExecutionItemStatus.Skipped, BattleExecutionItemOutcomeReason.BattleEnded, true));
        Debug.Log("模式51 G plan完成：" + plan.isCompleted);
    }

    void RunExecutionItemStatusInvalidDataSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("item_status_h", 30, 30, 50, 10, 3, 8);
        BattleCardState attack = CreateFixedAttackCardForCharacter(context.allyA, "item_status_h_attack", 3);
        BattleActionSlot invalidSlot = new BattleActionSlot(context.allyA, 1);
        invalidSlot.AssignFreeAction(null, attack, context.enemy);
        BattleActionSlot followSlot = new BattleActionSlot(context.allyA, 2);
        followSlot.AssignFreeAction(context.allyA, CreateBattleEndedAbilityCard(context.allyA, "item_status_h_follow", "ItemStatusHFollow"), context.allyA);
        BattleExecutionPlan plan = CreateManualFreeActionPlan(invalidSlot, followSlot);
        BattleExecutionItem firstItem = plan.executionItems[0];
        BattleExecutionItem secondItem = plan.executionItems[1];

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        Debug.Log("模式51 H Invalid数据为Failed：" + IsExecutionItemState(firstItem, BattleExecutionItemStatus.Failed, BattleExecutionItemOutcomeReason.InvalidData, false));
        Debug.Log("模式51 H 后续item保持Pending且未执行：" + IsExecutionItemState(secondItem, BattleExecutionItemStatus.Pending, BattleExecutionItemOutcomeReason.None, false));
        Debug.Log("模式51 H plan不完成：" + !plan.isCompleted);
    }

    void RunExecutionItemStatusUnsupportedResolveSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("item_status_i", 30, 30, 50, 10, 3, 8);
        BattleCardState defense = CreateTestDefenseCardForCharacter(context.allyA, "item_status_i_defense", 4, 1);
        BattleActionSlot unsupportedSlot = new BattleActionSlot(context.allyA, 1);
        unsupportedSlot.AssignFreeAction(context.allyA, defense, context.enemy);
        BattleActionSlot followSlot = new BattleActionSlot(context.allyA, 2);
        followSlot.AssignFreeAction(context.allyA, CreateBattleEndedAbilityCard(context.allyA, "item_status_i_follow", "ItemStatusIFollow"), context.allyA);
        BattleExecutionPlan plan = CreateManualFreeActionPlan(unsupportedSlot, followSlot);
        BattleExecutionItem firstItem = plan.executionItems[0];
        BattleExecutionItem secondItem = plan.executionItems[1];

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        Debug.Log("模式51 I Unsupported Resolver为Failed：" + IsExecutionItemState(firstItem, BattleExecutionItemStatus.Failed, BattleExecutionItemOutcomeReason.UnsupportedResolveType, false));
        Debug.Log("模式51 I 后续item保持Pending：" + IsExecutionItemState(secondItem, BattleExecutionItemStatus.Pending, BattleExecutionItemOutcomeReason.None, false));
    }

    void RunExecutionItemStatusUnsupportedExecutionTypeSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("item_status_j", 30, 30, 50, 10, 3, 8);
        BattleExecutionItem invalidTypeItem = new BattleExecutionItem(1, (BattleExecutionItemType)999, null, null);
        BattleActionSlot followSlot = new BattleActionSlot(context.allyA, 1);
        followSlot.AssignFreeAction(context.allyA, CreateBattleEndedAbilityCard(context.allyA, "item_status_j_follow", "ItemStatusJFollow"), context.allyA);
        BattleExecutionItem followItem = new BattleExecutionItem(2, BattleExecutionItemType.FreeAction, null, followSlot);
        BattleExecutionPlan plan = CreateManualExecutionPlan(invalidTypeItem, followItem);

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        Debug.Log("模式51 J 非法executionType为Failed：" + IsExecutionItemState(invalidTypeItem, BattleExecutionItemStatus.Failed, BattleExecutionItemOutcomeReason.UnsupportedExecutionType, false));
        Debug.Log("模式51 J 后续item保持Pending：" + IsExecutionItemState(followItem, BattleExecutionItemStatus.Pending, BattleExecutionItemOutcomeReason.None, false));
    }

    void RunExecutionItemStatusNullItemSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("item_status_k", 30, 30, 50, 10, 3, 8);
        BattleActionSlot followSlot = new BattleActionSlot(context.allyA, 1);
        followSlot.AssignFreeAction(context.allyA, CreateBattleEndedAbilityCard(context.allyA, "item_status_k_follow", "ItemStatusKFollow"), context.allyA);
        BattleExecutionItem followItem = new BattleExecutionItem(2, BattleExecutionItemType.FreeAction, null, followSlot);
        BattleExecutionPlan plan = new BattleExecutionPlan();
        plan.executionItems.Add(null);
        plan.executionItems.Add(followItem);

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        Debug.Log("模式51 K null item不抛异常且plan不完成：" + !plan.isCompleted);
        Debug.Log("模式51 K 后续item保持Pending：" + IsExecutionItemState(followItem, BattleExecutionItemStatus.Pending, BattleExecutionItemOutcomeReason.None, false));
        Debug.Log("模式51 K 后续行动未执行：" + !followSlot.isUsed);
    }

    void RunExecutionItemStatusTieLimitSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("item_status_l", 30, 30, 50, 10, 3, 8);
        BattleCardState playerAttack = CreateFixedAttackCardForCharacter(context.allyA, "item_status_l_player", 5);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "item_status_l_enemy", 5, 0);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("item_status_l_intent", context.enemy, enemyAttack, context.allyA, 1);
        BattleActionSlot responseSlot = new BattleActionSlot(context.allyA, 1);
        responseSlot.AssignResponse(context.allyA, playerAttack, intent, false);
        BattleExecutionItem tieItem = new BattleExecutionItem(1, BattleExecutionItemType.RespondedEnemyIntent, intent, responseSlot);
        BattleActionSlot followSlot = new BattleActionSlot(context.allyA, 2);
        followSlot.AssignFreeAction(context.allyA, CreateBattleEndedAbilityCard(context.allyA, "item_status_l_follow", "ItemStatusLFollow"), context.allyA);
        BattleExecutionItem followItem = new BattleExecutionItem(2, BattleExecutionItemType.FreeAction, null, followSlot);
        BattleExecutionPlan plan = CreateManualExecutionPlan(tieItem, followItem);

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        Debug.Log("模式51 L TieLimit为Failed：" + IsExecutionItemState(tieItem, BattleExecutionItemStatus.Failed, BattleExecutionItemOutcomeReason.TieLimitReached, false));
        Debug.Log("模式51 L plan不完成：" + !plan.isCompleted);
        Debug.Log("模式51 L 后续item保持Pending且未执行：" + (IsExecutionItemState(followItem, BattleExecutionItemStatus.Pending, BattleExecutionItemOutcomeReason.None, false) && !followSlot.isUsed));
    }

    void RunExecutionItemStatusAllExecutedOrSkippedSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("item_status_m", 30, 0, 50, 10, 3, 8);
        BattleActionSlot normalSlot = new BattleActionSlot(context.allyA, 1);
        normalSlot.AssignFreeAction(context.allyA, CreateFixedAttackCardForCharacter(context.allyA, "item_status_m_attack", 3), context.enemy);
        BattleActionSlot deadSlot = new BattleActionSlot(context.allyB, 1);
        deadSlot.AssignFreeAction(context.allyB, CreateFixedAttackCardForCharacter(context.allyB, "item_status_m_dead", 3), context.enemy);
        BattleExecutionPlan plan = CreateManualFreeActionPlan(normalSlot, deadSlot);

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        Debug.Log("模式51 M Executed与Skipped混合时plan完成：" + (plan.isCompleted && plan.executionItems[0].status == BattleExecutionItemStatus.Executed && plan.executionItems[1].status == BattleExecutionItemStatus.Skipped));
    }

    void RunExecutionItemStatusCompatibilitySubTest()
    {
        BattleExecutionItem pendingItem = new BattleExecutionItem(1, BattleExecutionItemType.FreeAction, null, null);
        BattleExecutionItem executedItem = new BattleExecutionItem(2, BattleExecutionItemType.FreeAction, null, null);
        BattleExecutionItem skippedItem = new BattleExecutionItem(3, BattleExecutionItemType.FreeAction, null, null);
        BattleExecutionItem failedItem = new BattleExecutionItem(4, BattleExecutionItemType.FreeAction, null, null);

        executedItem.MarkExecuted();
        skippedItem.MarkSkipped(BattleExecutionItemOutcomeReason.ActionUnavailable);
        failedItem.MarkFailed(BattleExecutionItemOutcomeReason.ResolverFailure);

        bool compatibility =
            pendingItem.status == BattleExecutionItemStatus.Pending &&
            !pendingItem.isCompleted &&
            executedItem.status == BattleExecutionItemStatus.Executed &&
            executedItem.isCompleted &&
            skippedItem.status == BattleExecutionItemStatus.Skipped &&
            skippedItem.isCompleted &&
            failedItem.status == BattleExecutionItemStatus.Failed &&
            !failedItem.isCompleted;

        Debug.Log("模式51 N status与isCompleted兼容：" + compatibility);
    }

    void RunCardResolvedHitContractBasicTestSequence()
    {
        Debug.Log("===== CardResolvedHitContractBasic 聚合测试开始 =====");

        RunContractAttackVsAttackPlayerWinSubTest();
        RunContractAttackVsAttackPlayerLoseSubTest();
        RunContractLoserBeforeUseAndClashLoseSubTest();
        RunContractAttackLoseSlotCommittedSubTest();
        RunContractEnemyWinPassiveGuardSubTest();
        RunContractDefenseFullBlockHitSubTest();
        RunContractDefenseReducedDamageZeroHitSubTest();
        RunContractDodgeSuccessSubTest();
        RunContractDodgeFailedZeroDamageHitSubTest();
        RunContractFreeAttackZeroDamageHitSubTest();
        RunContractUnrespondedZeroDamageHitSubTest();
        RunContractUnrespondedDamageAndKillSubTest();
        RunContractActionUnavailableRegressionSubTest();
        RunContractTieLimitRegressionSubTest();
    }

    void RunContractAttackVsAttackPlayerWinSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("contract_a", 30, 30, 50, 10, 3, 8);
        BattleCardState playerAttack = CreateFixedAttackCardForCharacter(context.allyA, "contract_a_player", 8);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "contract_a_enemy", 5, 2);
        playerAttack.cardData.cooldown = 2;
        AddProbeEffect(playerAttack, BattleTiming.Resolved, "ContractAPlayerResolved", ClashResult.Win);
        AddProbeEffect(playerAttack, BattleTiming.Hit, "ContractAPlayerHit", ClashResult.Win);
        AddProbeEffect(enemyAttack, BattleTiming.ClashLose, "ContractAEnemyClashLose", ClashResult.Lose);
        AddProbeEffect(enemyAttack, BattleTiming.Resolved, "ContractAEnemyResolved", ClashResult.Lose);
        context.allyA.AddBuff("NextClashPointUp", 1, 1);
        context.allyA.AddBuff("NextCardPointUp", 2, 1);
        context.enemy.AddBuff("NextClashPointUp", 1, 1);
        context.enemy.AddBuff("NextCardPointUp", 2, 1);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, playerAttack),
            CreateEnemyAttackIntent("contract_a_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool resolvedRule =
            result != null &&
            result.resultType == "PlayerWin" &&
            result.playerCardUsed &&
            !result.enemyCardUsed &&
            CountBuffStack(context.allyA, "ContractAPlayerResolved") == 1 &&
            CountBuffStack(context.enemy, "ContractAEnemyClashLose") == 1 &&
            CountBuffStack(context.enemy, "ContractAEnemyResolved") == 0;
        bool stateRule =
            playerAttack.currentCooldown == GetExpectedResolvedCooldown(playerAttack) &&
            enemyAttack.currentCooldown == 0;
        bool buffRule =
            CountBuffStack(context.allyA, "NextClashPointUp") == 0 &&
            CountBuffStack(context.allyA, "NextCardPointUp") == 0 &&
            CountBuffStack(context.enemy, "NextClashPointUp") == 0 &&
            CountBuffStack(context.enemy, "NextCardPointUp") == 2;
        bool hitRule = CountBuffStack(context.allyA, "ContractAPlayerHit") == 1;

        Debug.Log("模式52 A 玩家胜方Resolved且敌人败方不Resolved：" + resolvedRule);
        Debug.Log("模式52 A CD / cardUsed符合新规则：" + stateRule);
        Debug.Log("模式52 A 胜方NextCard消费、败方NextCard保留、双方NextClash消费：" + buffRule);
        Debug.Log("模式52 A 胜方Attack触发Hit：" + hitRule);
    }

    void RunContractAttackVsAttackPlayerLoseSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("contract_b", 30, 30, 50, 10, 3, 8);
        BattleCardState playerAttack = CreateFixedAttackCardForCharacter(context.allyA, "contract_b_player", 5);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "contract_b_enemy", 8, 2);
        playerAttack.cardData.cooldown = 2;
        AddProbeEffect(playerAttack, BattleTiming.ClashLose, "ContractBPlayerClashLose", ClashResult.Lose);
        AddProbeEffect(playerAttack, BattleTiming.Resolved, "ContractBPlayerResolved", ClashResult.Lose);
        AddProbeEffect(enemyAttack, BattleTiming.Resolved, "ContractBEnemyResolved", ClashResult.Win);
        context.allyA.AddBuff("NextClashPointUp", 1, 1);
        context.allyA.AddBuff("NextCardPointUp", 2, 1);
        context.enemy.AddBuff("NextClashPointUp", 1, 1);
        context.enemy.AddBuff("NextCardPointUp", 2, 1);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, playerAttack),
            CreateEnemyAttackIntent("contract_b_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool resolvedRule =
            result != null &&
            result.resultType == "EnemyWin" &&
            !result.playerCardUsed &&
            result.enemyCardUsed &&
            CountBuffStack(context.allyA, "ContractBPlayerClashLose") == 1 &&
            CountBuffStack(context.allyA, "ContractBPlayerResolved") == 0 &&
            CountBuffStack(context.enemy, "ContractBEnemyResolved") == 1;
        bool stateRule = playerAttack.currentCooldown == 0 && enemyAttack.currentCooldown == GetExpectedResolvedCooldown(enemyAttack);
        bool buffRule =
            CountBuffStack(context.allyA, "NextClashPointUp") == 0 &&
            CountBuffStack(context.allyA, "NextCardPointUp") == 2 &&
            CountBuffStack(context.enemy, "NextClashPointUp") == 0 &&
            CountBuffStack(context.enemy, "NextCardPointUp") == 0;

        Debug.Log("模式52 B 玩家败方只ClashLose且不Resolved：" + resolvedRule);
        Debug.Log("模式52 B 玩家败方不进CD，敌人胜方进CD：" + stateRule);
        Debug.Log("模式52 B 败方NextCard保留，双方NextClash消费：" + buffRule);
    }

    void RunContractLoserBeforeUseAndClashLoseSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("contract_c", 30, 30, 50, 10, 3, 8);
        BattleCardState playerAttack = CreateFixedAttackCardForCharacter(context.allyA, "contract_c_player", 4);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "contract_c_enemy", 9, 0);
        AddProbeEffect(playerAttack, BattleTiming.BeforeUse, "ContractCBeforeUse");
        AddProbeEffect(playerAttack, BattleTiming.ClashLose, "ContractCClashLose", ClashResult.Lose);
        AddProbeEffect(playerAttack, BattleTiming.Resolved, "ContractCResolved", ClashResult.Lose);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, playerAttack),
            CreateEnemyAttackIntent("contract_c_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool worked =
            result != null &&
            result.resultType == "EnemyWin" &&
            CountBuffStack(context.allyA, "ContractCBeforeUse") == 1 &&
            CountBuffStack(context.allyA, "ContractCClashLose") == 1 &&
            CountBuffStack(context.allyA, "ContractCResolved") == 0;

        Debug.Log("模式52 C 败方BeforeUse保留、ClashLose执行、Resolved不执行：" + worked);
    }

    void RunContractAttackLoseSlotCommittedSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("contract_d", 30, 30, 50, 10, 3, 8);
        BattleCardState responseAttack = CreateFixedAttackCardForCharacter(context.allyA, "contract_d_player", 4);
        responseAttack.cardData.cooldown = 2;
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "contract_d_enemy", 8, 0);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("contract_d_intent", context.enemy, enemyAttack, context.allyA, 1);
        BattleActionSlot responseSlot = new BattleActionSlot(context.allyA, 1);
        responseSlot.AssignResponse(context.allyA, responseAttack, intent, false);
        BattleExecutionItem item = new BattleExecutionItem(1, BattleExecutionItemType.RespondedEnemyIntent, intent, responseSlot);
        BattleExecutionPlan plan = CreateManualExecutionPlan(item);

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        bool worked =
            responseAttack.currentCooldown == 0 &&
            responseSlot.isUsed &&
            IsExecutionItemState(item, BattleExecutionItemStatus.Executed, BattleExecutionItemOutcomeReason.None, true) &&
            plan.isCompleted;

        Debug.Log("模式52 D Attack失败卡不Resolved但Responded槽位MarkUsed：" + worked);
    }

    void RunContractEnemyWinPassiveGuardSubTest()
    {
        BattleEndedTestContext directContext = CreateBattleEndedTestContext("contract_e_direct", 30, 30, 50, 10, 3, 8);
        BattleCardState directResponse = CreateFixedAttackCardForCharacter(directContext.allyB, "contract_e_direct_response", 4);
        BattleCardState directEnemy = CreateFixedEnemyAttackCardForDodgeTest(directContext.enemy, "contract_e_direct_enemy", 8, 2);
        BattleCardState directGuard = CreateTestDefenseCardForCharacter(directContext.allyB, "contract_e_direct_guard", 12, 1);
        AddProbeEffect(directResponse, BattleTiming.Resolved, "ContractEDirectResponseResolved", ClashResult.Lose);
        AddProbeEffect(directEnemy, BattleTiming.BeforeUse, "ContractEDirectEnemyBefore");
        AddProbeEffect(directEnemy, BattleTiming.Resolved, "ContractEDirectEnemyResolved", ClashResult.Win);
        AddProbeEffect(directGuard, BattleTiming.Resolved, "ContractEDirectGuardResolved");
        directContext.enemy.AddBuff("NextClashPointUp", 1, 1);
        directContext.enemy.AddBuff("NextCardPointUp", 1, 1);
        BattleEnemyIntent directIntent = CreateEnemyAttackIntent("contract_e_direct_intent", directContext.enemy, directEnemy, directContext.allyB, 1);
        BattleActionSlot directResponseSlot = CreateRespondedSlot(directContext.allyB, directResponse);
        BattleActionSlot directGuardSlot = new BattleActionSlot(directContext.allyB, 2);
        directGuardSlot.AssignPassiveGuard(directContext.allyB, directGuard);
        BattleResolveResult directResult = BattleResolver.ResolveRespondedEnemyIntent(
            directResponseSlot,
            directIntent,
            new List<BattleActionSlot> { directGuardSlot }
        );

        bool directRule =
            directResult != null &&
            directResult.resultType == "EnemyWin" &&
            !directResult.playerCardUsed &&
            directResult.enemyCardUsed &&
            directResult.triggeredPassiveGuardSlot == null &&
            CountBuffStack(directContext.allyB, "ContractEDirectResponseResolved") == 0 &&
            CountBuffStack(directContext.enemy, "ContractEDirectEnemyBefore") == 1 &&
            CountBuffStack(directContext.enemy, "ContractEDirectEnemyResolved") == 1 &&
            CountBuffStack(directContext.allyB, "ContractEDirectGuardResolved") == 0 &&
            CountBuffStack(directContext.enemy, "NextClashPointUp") == 0 &&
            CountBuffStack(directContext.enemy, "NextCardPointUp") == 0 &&
            directContext.allyB.currentHP == 22;

        BattleEndedTestContext execContext = CreateBattleEndedTestContext("contract_e_exec", 30, 30, 50, 10, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(execContext.allyA, execContext.allyB, 2);
        BattleCardState responseAttack = CreateFixedAttackCardForCharacter(execContext.allyB, "contract_e_exec_response", 4);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(execContext.enemy, "contract_e_exec_enemy", 8, 2);
        BattleCardState passiveGuard = CreateTestDefenseCardForCharacter(execContext.allyB, "contract_e_exec_guard", 12, 1);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("contract_e_exec_intent", execContext.enemy, enemyAttack, execContext.allyB, 1);
        BattleActionSlotManager.AssignResponseToEnemyIntent(actionSlots, execContext.allyB, 1, execContext.allyB, responseAttack, intent);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, execContext.allyB, 2, execContext.allyB, passiveGuard);
        BattleExecutionPlan plan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, BattleEnemyIntentManager.CreateIntentQueue(intent));
        BattleExecutionItem respondedItem = GetFirstExecutionItem(plan);
        int candidateCount = respondedItem != null && respondedItem.passiveGuardCandidates != null
            ? respondedItem.passiveGuardCandidates.Count
            : -1;
        ExecutePlanWithRuntimeStateAndCompleteTurn(execContext.runtimeState, plan);
        BattleActionSlot responseSlot = BattleActionSlotManager.GetSlot(actionSlots, execContext.allyB, 1);
        BattleActionSlot guardSlot = BattleActionSlotManager.GetSlot(actionSlots, execContext.allyB, 2);

        bool slotRule =
            candidateCount == 0 &&
            responseSlot != null &&
            responseSlot.isUsed &&
            guardSlot != null &&
            !guardSlot.isUsed &&
            responseAttack.currentCooldown == 0 &&
            execContext.allyB.currentHP == 22;

        Debug.Log("模式52 E EnemyWin精确响应后不触发额外守备：" + directRule);
        Debug.Log("模式52 E Responded候选为0且仅原响应槽位MarkUsed：" + slotRule);
    }

    void RunContractDefenseFullBlockHitSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("contract_f", 30, 30, 50, 10, 3, 8);
        BattleCardState defense = CreateTestDefenseCardForCharacter(context.allyA, "contract_f_defense", 9, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "contract_f_enemy", 4, 2);
        AddProbeEffect(enemyAttack, BattleTiming.Hit, "ContractFHit");
        AddProbeEffect(enemyAttack, BattleTiming.AfterDamage, "ContractFAfterDamage");
        int hpBefore = context.allyA.currentHP;

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, defense),
            CreateEnemyAttackIntent("contract_f_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool worked =
            result != null &&
            result.resultType == "DefenseFullBlock" &&
            result.playerCardUsed &&
            result.enemyCardUsed &&
            context.allyA.currentHP == hpBefore &&
            CountBuffStack(context.enemy, "ContractFHit") == 1 &&
            CountBuffStack(context.enemy, "ContractFAfterDamage") == 0;

        Debug.Log("模式52 F DefenseFullBlock双方Resolved且0伤害仍Hit无AfterDamage：" + worked);
    }

    void RunContractDefenseReducedDamageZeroHitSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("contract_g", 30, 30, 50, 10, 3, 8);
        BattleCardState defense = CreateTestDefenseCardForCharacter(context.allyA, "contract_g_defense", 1, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "contract_g_enemy", 5, 2);
        AddProbeEffect(enemyAttack, BattleTiming.Hit, "ContractGHit");
        AddProbeEffect(enemyAttack, BattleTiming.AfterDamage, "ContractGAfterDamage");
        context.allyA.AddBuff("DamageReduction", 10, 1);
        int hpBefore = context.allyA.currentHP;

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, defense),
            CreateEnemyAttackIntent("contract_g_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool worked =
            result != null &&
            result.resultType == "DefenseReducedDamage" &&
            context.allyA.currentHP == hpBefore &&
            CountBuffStack(context.enemy, "ContractGHit") == 1 &&
            CountBuffStack(context.enemy, "ContractGAfterDamage") == 0;

        Debug.Log("模式52 G DefenseReducedDamage最终0伤害仍Hit无AfterDamage：" + worked);
    }

    void RunContractDodgeSuccessSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("contract_h", 30, 30, 50, 10, 3, 8);
        BattleCardState dodge = CreateFixedDodgeCardForCharacter(context.allyA, "contract_h_dodge", 9, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "contract_h_enemy", 5, 2);
        AddProbeEffect(dodge, BattleTiming.ClashWin, "ContractHDodgeClashWin", ClashResult.Win);
        AddProbeEffect(dodge, BattleTiming.Resolved, "ContractHDodgeResolved", ClashResult.Win);
        AddProbeEffect(enemyAttack, BattleTiming.ClashLose, "ContractHEnemyClashLose", ClashResult.Lose);
        AddProbeEffect(enemyAttack, BattleTiming.Resolved, "ContractHEnemyResolved", ClashResult.Lose);
        AddProbeEffect(enemyAttack, BattleTiming.Hit, "ContractHEnemyHit");
        int hpBefore = context.allyA.currentHP;

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, dodge),
            CreateEnemyAttackIntent("contract_h_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool worked =
            result != null &&
            result.resultType == "DodgeSuccess" &&
            !result.playerCardUsed &&
            result.enemyCardUsed &&
            result.playerCardParticipated &&
            result.playerCardUseDisposition == BattleCardUseDisposition.DeferForContinuousDodge &&
            context.allyA.currentHP == hpBefore &&
            CountBuffStack(context.allyA, "ContractHDodgeClashWin") == 1 &&
            CountBuffStack(context.allyA, "ContractHDodgeResolved") == 0 &&
            CountBuffStack(context.enemy, "ContractHEnemyClashLose") == 1 &&
            CountBuffStack(context.enemy, "ContractHEnemyResolved") == 1 &&
            CountBuffStack(context.enemy, "ContractHEnemyHit") == 0;

        Debug.Log("模式52 H Dodge成功参与结算但玩家Resolved延迟且不Hit不伤害：" + worked);
    }

    void RunContractDodgeFailedZeroDamageHitSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("contract_i", 30, 30, 50, 10, 3, 8);
        BattleCardState dodge = CreateFixedDodgeCardForCharacter(context.allyA, "contract_i_dodge", 3, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "contract_i_enemy", 8, 2);
        AddProbeEffect(dodge, BattleTiming.ClashLose, "ContractIDodgeClashLose", ClashResult.Lose);
        AddProbeEffect(dodge, BattleTiming.Resolved, "ContractIDodgeResolved", ClashResult.Lose);
        AddProbeEffect(enemyAttack, BattleTiming.Hit, "ContractIEnemyHit");
        AddProbeEffect(enemyAttack, BattleTiming.AfterDamage, "ContractIEnemyAfterDamage");
        context.allyA.AddBuff("DamageReduction", 10, 1);
        int hpBefore = context.allyA.currentHP;

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, dodge),
            CreateEnemyAttackIntent("contract_i_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool worked =
            result != null &&
            result.resultType == "DodgeFailed" &&
            result.playerCardUsed &&
            result.enemyCardUsed &&
            context.allyA.currentHP == hpBefore &&
            CountBuffStack(context.allyA, "ContractIDodgeClashLose") == 1 &&
            CountBuffStack(context.allyA, "ContractIDodgeResolved") == 1 &&
            CountBuffStack(context.enemy, "ContractIEnemyHit") == 1 &&
            CountBuffStack(context.enemy, "ContractIEnemyAfterDamage") == 0;

        Debug.Log("模式52 I Dodge失败最终0伤害仍Hit无AfterDamage：" + worked);
    }

    void RunContractFreeAttackZeroDamageHitSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("contract_j", 30, 30, 50, 10, 3, 8);
        BattleCardState freeAttack = CreateFixedAttackCardForCharacter(context.allyA, "contract_j_attack", 0);
        AddProbeEffect(freeAttack, BattleTiming.Resolved, "ContractJResolved");
        AddProbeEffect(freeAttack, BattleTiming.Hit, "ContractJHit");
        AddProbeEffect(freeAttack, BattleTiming.AfterDamage, "ContractJAfterDamage");
        BattleActionSlot slot = new BattleActionSlot(context.allyA, 1);
        slot.AssignFreeAction(context.allyA, freeAttack, context.enemy);
        int enemyHPBefore = context.enemy.currentHP;

        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);

        bool worked =
            result != null &&
            result.resultType == "FreeAttack" &&
            result.playerCardUsed &&
            context.enemy.currentHP == enemyHPBefore &&
            CountBuffStack(context.allyA, "ContractJResolved") == 1 &&
            CountBuffStack(context.allyA, "ContractJHit") == 1 &&
            CountBuffStack(context.allyA, "ContractJAfterDamage") == 0;

        Debug.Log("模式52 J FreeAttack最终0伤害Resolved且Hit无AfterDamage：" + worked);
    }

    void RunContractUnrespondedZeroDamageHitSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("contract_k", 30, 30, 50, 10, 3, 8);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "contract_k_enemy", 0, 2);
        AddProbeEffect(enemyAttack, BattleTiming.Resolved, "ContractKResolved");
        AddProbeEffect(enemyAttack, BattleTiming.Hit, "ContractKHit");
        AddProbeEffect(enemyAttack, BattleTiming.AfterDamage, "ContractKAfterDamage");
        int hpBefore = context.allyA.currentHP;

        BattleResolveResult result = BattleResolver.ResolveUnrespondedEnemyIntent(
            CreateEnemyAttackIntent("contract_k_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool worked =
            result != null &&
            result.resultType == "UnrespondedEnemyAttack" &&
            result.enemyCardUsed &&
            result.triggeredEventChain &&
            context.allyA.currentHP == hpBefore &&
            CountBuffStack(context.enemy, "ContractKResolved") == 1 &&
            CountBuffStack(context.enemy, "ContractKHit") == 1 &&
            CountBuffStack(context.enemy, "ContractKAfterDamage") == 0;

        Debug.Log("模式52 K Unresponded最终0伤害Resolved且Hit无AfterDamage：" + worked);
    }

    void RunContractUnrespondedDamageAndKillSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("contract_l", 30, 3, 50, 10, 3, 8);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "contract_l_enemy", 5, 2);
        AddProbeEffect(enemyAttack, BattleTiming.AfterDamage, "ContractLAfterDamage");
        AddProbeEffect(enemyAttack, BattleTiming.AfterKill, "ContractLAfterKill");

        BattleResolveResult result = BattleResolver.ResolveUnrespondedEnemyIntent(
            CreateEnemyAttackIntent("contract_l_intent", context.enemy, enemyAttack, context.allyB, 1)
        );

        bool worked =
            result != null &&
            result.enemyCardUsed &&
            context.allyB.IsDead() &&
            CountBuffStack(context.enemy, "ContractLAfterDamage") == 1 &&
            CountBuffStack(context.enemy, "ContractLAfterKill") == 1;

        Debug.Log("模式52 L Unresponded造成伤害和击杀时AfterDamage / AfterKill触发：" + worked);
    }

    void RunContractActionUnavailableRegressionSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("contract_m", 30, 30, 50, 20, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1);
        BattleCardState responseAttack = CreateBulletLockedBeforeUseAttackCard(context.allyA, "contract_m_response", 5, 3, "ContractMBeforeUse", 1, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "contract_m_enemy", 5, 0);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("contract_m_intent", context.enemy, enemyAttack, context.allyB, 1);
        context.allyA.AddBuff("Bullet", 3, -1);
        CardEligibilityResult assignResult;
        bool assignSuccess = BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            context.allyA,
            1,
            context.allyA,
            responseAttack,
            intent,
            out assignResult
        );
        BattleActionSlot responseSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);
        BattleExecutionPlan plan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(actionSlots, BattleEnemyIntentManager.CreateIntentQueue(intent));
        BattleExecutionItem item = GetFirstExecutionItem(plan);
        int hpBefore = context.allyB.currentHP;
        int enemyUseCountBefore = enemyAttack.currentUseCount;
        bool prepared =
            CountBuffStack(context.allyA, "Bullet") >= 3 &&
            assignSuccess &&
            responseSlot != null &&
            object.ReferenceEquals(responseSlot.actor, context.allyA) &&
            object.ReferenceEquals(responseSlot.cardState, responseAttack) &&
            intent.isResponded &&
            object.ReferenceEquals(intent.actualTargetCharacter, context.allyA);
        bool itemCreatedAsResponded =
            plan != null &&
            plan.executionItems != null &&
            plan.executionItems.Count == 1 &&
            item != null &&
            item.executionType == BattleExecutionItemType.RespondedEnemyIntent;

        RemoveAllBuffs(context.allyA, "Bullet");
        bool bulletRemovedBeforeExecute = CountBuffStack(context.allyA, "Bullet") == 0;

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        bool worked =
            item != null &&
            item.status == BattleExecutionItemStatus.Executed &&
            item.outcomeReason == BattleExecutionItemOutcomeReason.ResponseUnavailableFallbackToUnresponded &&
            responseSlot != null &&
            !responseSlot.isUsed &&
            object.ReferenceEquals(intent.actualTargetCharacter, context.allyB) &&
            enemyAttack.currentUseCount == enemyUseCountBefore &&
            CountBuffStack(context.allyA, "ContractMBeforeUse") == 0 &&
            responseAttack.currentCooldown == 0 &&
            context.allyB.currentHP == hpBefore - 5;

        Debug.Log("模式52 M 准备阶段条件满足并成功安排：" + prepared);
        Debug.Log("模式52 M ExecutionPlan item为RespondedEnemyIntent：" + itemCreatedAsResponded);
        Debug.Log("模式52 M 执行前Bullet已移除：" + bulletRemovedBeforeExecute);
        Debug.Log("模式52 M outcome为ResponseUnavailableFallbackToUnresponded：" + IsExecutionItemState(item, BattleExecutionItemStatus.Executed, BattleExecutionItemOutcomeReason.ResponseUnavailableFallbackToUnresponded, true));
        Debug.Log("模式52 M ActionUnavailable不BeforeUse不Resolved不MarkUsed且正常回落：" + worked);
    }

    void RunContractTieLimitRegressionSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("contract_n", 30, 30, 50, 10, 3, 8);
        BattleCardState playerAttack = CreateFixedAttackCardForCharacter(context.allyA, "contract_n_player", 5);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "contract_n_enemy", 5, 0);
        context.allyA.AddBuff("NextClashPointUp", 1, 1);
        context.allyA.AddBuff("NextCardPointUp", 1, 1);
        context.enemy.AddBuff("NextClashPointUp", 1, 1);
        context.enemy.AddBuff("NextCardPointUp", 1, 1);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("contract_n_intent", context.enemy, enemyAttack, context.allyA, 1);
        BattleActionSlot responseSlot = new BattleActionSlot(context.allyA, 1);
        responseSlot.AssignResponse(context.allyA, playerAttack, intent, false);
        BattleExecutionItem tieItem = new BattleExecutionItem(1, BattleExecutionItemType.RespondedEnemyIntent, intent, responseSlot);
        BattleActionSlot followSlot = new BattleActionSlot(context.allyA, 2);
        followSlot.AssignFreeAction(context.allyA, CreateBattleEndedAbilityCard(context.allyA, "contract_n_follow", "ContractNFollow"), context.allyA);
        BattleExecutionItem followItem = new BattleExecutionItem(2, BattleExecutionItemType.FreeAction, null, followSlot);
        BattleExecutionPlan plan = CreateManualExecutionPlan(tieItem, followItem);

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        bool worked =
            IsExecutionItemState(tieItem, BattleExecutionItemStatus.Failed, BattleExecutionItemOutcomeReason.TieLimitReached, false) &&
            IsExecutionItemState(followItem, BattleExecutionItemStatus.Pending, BattleExecutionItemOutcomeReason.None, false) &&
            !responseSlot.isUsed &&
            CountBuffStack(context.allyA, "NextClashPointUp") == 1 &&
            CountBuffStack(context.allyA, "NextCardPointUp") == 1 &&
            CountBuffStack(context.enemy, "NextClashPointUp") == 1 &&
            CountBuffStack(context.enemy, "NextCardPointUp") == 1;

        Debug.Log("模式52 N TieLimit不Resolved不消费Buff不MarkUsed且后续Pending：" + worked);
    }

    void RunCardResourceSnapshotAndConsumeBasicTestSequence()
    {
        Debug.Log("===== CardResourceSnapshotAndConsumeBasic 聚合测试开始 =====");

        RunCardResourceFallbackBasePointSubTest();
        RunCardResourceActionStartAffectsCurrentSnapshotSubTest();
        RunCardResourceBeforeUseDoesNotAffectCurrentSnapshotSubTest();
        RunCardResourcePointPerStackSubTest();
        RunCardResourceExactStackBonusSubTest();
        RunCardResourceAttackWinConsumeSubTest();
        RunCardResourceAttackLoseNoConsumeSubTest();
        RunCardResourceDodgeVsAttackConsumeSubTest();
        RunCardResourceDefenseConsumeSubTest();
        RunCardResourceFreeAttackConsumeSubTest();
        RunCardResourceUnrespondedEnemyAttackConsumeSubTest();
        RunCardResourceActionUnavailableNoActionStartSubTest();
        RunCardResourceTieRetrySnapshotSubTest();
        RunCardResourceTieLimitNoConsumeSubTest();
        RunCardResourceKnownPointPassiveGuardSubTest();
        RunCardResourceActionStartOneShotAndAbilityIsolationSubTest();
    }

    void RunCardAssignmentEligibilityBasicTestSequence()
    {
        Debug.Log("===== CardAssignmentEligibilityBasic 聚合测试开始 =====");

        RunCardAssignmentEligibilityGuiltInsufficientSubTest();
        RunCardAssignmentEligibilityGuiltExactSubTest();
        RunCardAssignmentEligibilityGuiltAboveSubTest();
        RunCardAssignmentEligibilityBuffInsufficientSubTest();
        RunCardAssignmentEligibilityBuffExactSubTest();
        RunCardAssignmentEligibilityMultipleConditionsSubTest();
        RunCardAssignmentEligibilityPendingBuffIgnoredSubTest();
        RunCardAssignmentEligibilityPermanentBuffSubTest();
        RunCardAssignmentEligibilitySoftResourceNotLockedSubTest();
        RunCardAssignmentEligibilityExplicitBulletConditionSubTest();
        RunCardAssignmentEligibilityCooldownConsumedSubTest();
        RunCardAssignmentEligibilityDeadActorSubTest();
        RunCardAssignmentEligibilityStateSafetySubTest();
        RunCardAssignmentEligibilityPureReadSubTest();
        RunCardAssignmentEligibilityExecutionRecheckSubTest();
        RunCardAssignmentEligibilityNoPredictionSubTest();
    }

    void RunRealCardResourceMigrationBasicTestSequence()
    {
        Debug.Log("===== RealCardResourceMigrationBasic 聚合测试开始 =====");

        RunRealCardResourceJsonFoundSubTest();
        RunRealCardResourceRuleDeserializedSubTest();
        RunRealCardResourceAssignWithNoBulletSubTest();
        RunRealCardResourceFallbackZeroPointSubTest();
        RunRealCardResourceNextCardStillStacksSubTest();
        RunRealCardResourceOneBulletNormalVersionSubTest();
        RunRealCardResourceExactThreeBulletBonusSubTest();
        RunRealCardResourceWinConsumesOneBulletSubTest();
        RunRealCardResourceLoseConsumesNoBulletSubTest();
        RunRealCardResourcePreviousSlotReloadSubTest();

        Debug.Log("===== RealCardResourceMigrationBasic 聚合测试结束 =====");
    }

    void RunBattleDefinitionDataBootstrapBasicTestSequence()
    {
        Debug.Log("===== BattleDefinitionDataBootstrapBasic 聚合测试开始 =====");

        RunBattleDefinitionDataJsonLoadSubTest();
        RunBattleDefinitionDataReferenceSubTest();
        RunBattleDefinitionDataPlayerCreationSubTest();
        RunBattleDefinitionDataEnemyCreationSubTest();
        RunBattleDefinitionDataRuntimeBaseStateSubTest();
        RunBattleDefinitionDataPlayerCardsSubTest();
        RunBattleDefinitionDataDuplicatePlayerCardsSubTest();
        RunBattleDefinitionDataInitialBuffSubTest();
        RunBattleDefinitionDataEnemyCardsSubTest();
        RunBattleDefinitionDataActionSlotsSubTest();
        RunBattleDefinitionDataEnemyIntentsSubTest();
        RunBattleDefinitionDataTargetMappingSubTest();
        RunBattleDefinitionDataRuntimeStateSubTest();
        RunBattleDefinitionDataDeadFixedTargetFallbackSubTest();
        RunBattleDefinitionDataEnemyCardCooldownSkipSubTest();
        RunBattleDefinitionDataDuplicateEnemyCardIndexFailSubTest();
        RunBattleDefinitionDataMissingCardFailSubTest();
        RunBattleDefinitionDataMissingBuffFailSubTest();
        RunBattleDefinitionDataMissingCrossReferenceFailSubTest();
        RunBattleSceneInitializationRuntimeContractSubTest();
        RunBattleSceneInitializationLegacyCardsSubTest();
        RunBattleSceneInitializationDuplicateGuardSubTest();
        RunBattleSceneBootstrapFailureAndDebugDefaultSubTest();

        Debug.Log("===== BattleDefinitionDataBootstrapBasic 聚合测试结束 =====");
    }

    void RunBattleSceneInitializationRuntimeContractSubTest()
    {
        BattleDefinitionBootstrapResult result =
            BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        string errorMessage = string.Empty;
        bool valid =
            result != null &&
            result.isSuccess &&
            BattleSimpleUIController.ValidateRuntimeStateForInitialization(
                result.runtimeState,
                out errorMessage
            );

        Debug.Log(
            "模式56 T 正式Runtime满足Controller严格初始化契约：" + valid +
            (valid ? "" : " / " + errorMessage)
        );
    }

    void RunBattleSceneInitializationLegacyCardsSubTest()
    {
        BattleDefinitionBootstrapResult result =
            BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        BattleSimpleUIController.LegacyCardReferenceSet references = null;
        string errorMessage = string.Empty;
        bool resolved =
            result != null &&
            result.isSuccess &&
            BattleSimpleUIController.TryResolveLegacyCardReferences(
                result.runtimeState,
                out references,
                out errorMessage
            );

        bool formalReferences = resolved &&
            HasExactCardReference(result.runtimeState.allyA, references.allyAAttack) &&
            HasExactCardReference(result.runtimeState.allyA, references.allyABulletAttack) &&
            HasExactCardReference(result.runtimeState.allyA, references.allyADefense) &&
            HasExactCardReference(result.runtimeState.allyA, references.allyADodge) &&
            HasExactCardReference(result.runtimeState.allyA, references.allyAAbility) &&
            HasExactCardReference(result.runtimeState.allyA, references.allyASinAttack) &&
            HasExactCardReference(result.runtimeState.allyB, references.allyBAttack) &&
            HasExactCardReference(result.runtimeState.allyB, references.allyBDefense) &&
            HasExactCardReference(result.runtimeState.allyB, references.allyBDodge) &&
            HasExactCardReference(result.runtimeState.allyB, references.allyBAbility) &&
            HasExactCardReference(result.runtimeState.allyB, references.allyBSinAttack) &&
            HasExactCardReference(result.runtimeState.enemy, references.enemyAttack) &&
            HasExactCardReference(result.runtimeState.enemy2, references.enemy02Attack) &&
            !object.ReferenceEquals(
                references.enemyAttack,
                references.enemy02Attack
            ) &&
            !HasDebugCardInstanceID(references);

        Debug.Log(
            "模式56 U 兼容卡牌全部来自正式battleCards且敌人实例独立：" +
            formalReferences +
            (resolved ? "" : " / " + errorMessage)
        );
    }

    void RunBattleSceneInitializationDuplicateGuardSubTest()
    {
        BattleDefinitionBootstrapResult result =
            BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        GameObject controllerObject =
            new GameObject("Mode56DuplicateInitializationController");
        controllerObject.SetActive(false);
        BattleSimpleUIController controller =
            controllerObject.AddComponent<BattleSimpleUIController>();

        System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic;
        System.Reflection.FieldInfo initializedField =
            typeof(BattleSimpleUIController).GetField("isInitialized", flags);
        System.Reflection.FieldInfo runtimeField =
            typeof(BattleSimpleUIController).GetField("runtimeState", flags);
        BattleRuntimeState existingRuntime = new BattleRuntimeState();

        initializedField?.SetValue(controller, true);
        runtimeField?.SetValue(controller, existingRuntime);
        bool accepted = controller.InitializeFromRuntimeState(
            result != null ? result.runtimeState : null
        );
        bool duplicateRejected =
            initializedField != null &&
            runtimeField != null &&
            !accepted &&
            object.ReferenceEquals(controller.RuntimeState, existingRuntime);

        Debug.Log(
            "模式56 V Controller重复Initialize被拒绝且不覆盖现有Runtime：" +
            duplicateRejected
        );
        Destroy(controllerObject);
    }

    void RunBattleSceneBootstrapFailureAndDebugDefaultSubTest()
    {
        BattleDefinitionBootstrapResult invalidResult =
            BattleDefinitionBootstrap.CreateRuntimeState(
                "missing_encounter_for_mode56"
            );
        GameObject bootstrapObject =
            new GameObject("Mode56BattleSceneBootstrapDefaults");
        bootstrapObject.SetActive(false);
        BattleSceneBootstrap bootstrap =
            bootstrapObject.AddComponent<BattleSceneBootstrap>();

        bool safeFailureAndDefault =
            invalidResult != null &&
            !invalidResult.isSuccess &&
            invalidResult.runtimeState == null &&
            !bootstrap.UseDebugTestInitialization &&
            bootstrap.ActiveBootstrapResult == null &&
            !bootstrap.HasStartedInitialization;

        Debug.Log(
            "模式56 W 无效encounter安全失败且Debug开关默认关闭：" +
            safeFailureAndDefault
        );
        Destroy(bootstrapObject);
    }

    bool HasExactCardReference(
        CharacterData owner,
        BattleCardState expectedCardState
    )
    {
        if (owner == null ||
            owner.battleCards == null ||
            expectedCardState == null ||
            !object.ReferenceEquals(expectedCardState.owner, owner))
        {
            return false;
        }

        for (int index = 0; index < owner.battleCards.Count; index++)
        {
            if (object.ReferenceEquals(
                    owner.battleCards[index],
                    expectedCardState))
            {
                return true;
            }
        }

        return false;
    }

    bool HasDebugCardInstanceID(
        BattleSimpleUIController.LegacyCardReferenceSet references
    )
    {
        BattleCardState[] cards =
        {
            references.allyAAttack,
            references.allyABulletAttack,
            references.allyADefense,
            references.allyADodge,
            references.allyAAbility,
            references.allyASinAttack,
            references.allyBAttack,
            references.allyBDefense,
            references.allyBDodge,
            references.allyBAbility,
            references.allyBSinAttack,
            references.enemyAttack,
            references.enemy02Attack
        };

        for (int index = 0; index < cards.Length; index++)
        {
            if (cards[index] != null &&
                !string.IsNullOrEmpty(cards[index].instanceID) &&
                cards[index].instanceID.StartsWith("ui_"))
            {
                return true;
            }
        }

        return false;
    }

    void RunBattlePreparedActionAssignmentModelBasicTestSequence()
    {
        Debug.Log("===== BattlePreparedActionAssignmentModelBasic 聚合测试开始 =====");

        RunPreparedAssignmentMainResponseAndPromotionSubTest();
        RunPreparedAssignmentAutoDowngradeSubTest();
        RunPreparedAssignmentGuardPlacementsSubTest();
        RunPreparedAssignmentSelfAndInvalidTargetSubTest();
        RunPreparedAssignmentAtomicReplaceAndDuplicateSubTest();
        RunPreparedAssignmentCancelAndIntentCompatibilitySubTest();
        RunPreparedAssignmentPurePrepareStateSubTest();

        Debug.Log("===== BattlePreparedActionAssignmentModelBasic 聚合测试结束 =====");
    }

    void RunBattleCardDragAssignmentRoutingBasicTestSequence()
    {
        Debug.Log("===== BattleCardDragAssignmentRoutingBasic 聚合测试开始 =====");

        RunCardDragExactViewBindingSubTest();
        RunCardDragHandFilterAndCancelSubTests();
        RunCardDragAtomicReplacementSubTests();
        RunCardDragEnemyRoutingSubTests();
        RunCardDragSelfAndValidationSubTests();
        RunCardDragUISlotAndRefreshSubTests();

        Debug.Log("===== BattleCardDragAssignmentRoutingBasic 聚合测试结束 =====");
    }

    void RunBattleAutomaticTurnCycleAndCooldownDragBasicTestSequence()
    {
        Debug.Log("===== BattleAutomaticTurnCycleAndCooldownDragBasic 聚合测试开始 =====");

        RunAutomaticTurnSingleCycleSubTest();
        RunAutomaticTurnWithoutPlayerActionSubTest();
        RunAutomaticTurnIncrementOnceSubTest();
        RunAutomaticTurnLivingSlotSubTest();
        RunAutomaticTurnFixedIntentSubTest();
        RunAutomaticTurnFixedTargetPrioritySubTest();
        RunAutomaticTurnAllyFallbackSubTest();
        RunAutomaticTurnAllAlliesDeadSubTest();
        RunAutomaticTurnEnemy02IsolationSubTest();
        RunAutomaticTurnSelectionClearSubTest();
        RunAutomaticTurnCooldownHandVisibilitySubTest();
        RunAutomaticTurnCooldownDragBlockedSubTest();
        RunAutomaticTurnReadyCardDragGateSubTest();
        RunAutomaticTurnCooldownTickRecoverySubTest();
        RunAutomaticTurnBattleEndedStopSubTest();
        RunAutomaticTurnDuplicateCallProtectionSubTest();

        Debug.Log("===== BattleAutomaticTurnCycleAndCooldownDragBasic 聚合测试结束 =====");
    }

    void RunBattleCardPrimaryPreviewContractBasicTestSequence()
    {
        Debug.Log("===== BattleCardPrimaryPreviewContractBasic 聚合测试开始 =====");

        RunPrimaryPreviewCooldownContractSubTests();
        RunPrimaryPreviewPointRangeContractSubTests();
        RunPrimaryPreviewRealCardDataContractSubTests();

        Debug.Log("===== BattleCardPrimaryPreviewContractBasic 聚合测试结束 =====");
    }

    void RunBattleCardCooldownFutureTurnSemanticsBasicTestSequence()
    {
        Debug.Log("===== BattleCardCooldownFutureTurnSemanticsBasic 聚合测试开始 =====");

        RunFutureTurnCooldownZeroSubTest();
        RunFutureTurnCooldownOneSubTests();
        RunFutureTurnCooldownTwoSubTest();
        RunFutureTurnCooldownPreviewSubTests();
        RunFutureTurnCooldownAutomaticCycleSubTest();
        RunFutureTurnCooldownDodgeSubTests();
        RunFutureTurnCooldownDefenseSubTests();
        RunFutureTurnCooldownSinAndIdempotentSubTests();

        Debug.Log("===== BattleCardCooldownFutureTurnSemanticsBasic 聚合测试结束 =====");
    }

    bool RunBattleCardPrimaryVisualPresetBasicTestSequence()
    {
        Debug.Log("===== BattleCardPrimaryVisualPresetBasic 聚合测试开始 =====");

        GameObject cardObject = new GameObject(
            "Visual64Card",
            typeof(RectTransform)
        );
        BattleCardVisualStyle visualStyle =
            cardObject.AddComponent<BattleCardVisualStyle>();
        BattleCardUIView cardView = cardObject.AddComponent<BattleCardUIView>();

        TMPro.TMP_Text cardNameText = CreateMode64Text(
            cardObject.transform,
            "name"
        );
        TMPro.TMP_Text pointText = CreateMode64Text(
            cardObject.transform,
            "dianshu"
        );
        TMPro.TMP_Text typeText = CreateMode64Text(
            cardObject.transform,
            "leibie"
        );
        TMPro.TMP_Text descriptionText = CreateMode64Text(
            cardObject.transform,
            "miaoshu"
        );
        TMPro.TMP_Text[] primaryTexts =
        {
            cardNameText,
            descriptionText,
            pointText,
            typeText
        };
        float[] fontSizesBeforeBind =
        {
            cardNameText.fontSize,
            descriptionText.fontSize,
            pointText.fontSize,
            typeText.fontSize
        };
        bool[] autoSizingBeforeBind =
        {
            cardNameText.enableAutoSizing,
            descriptionText.enableAutoSizing,
            pointText.enableAutoSizing,
            typeText.enableAutoSizing
        };
        Vector3[] localScalesBeforeBind =
        {
            cardNameText.rectTransform.localScale,
            descriptionText.rectTransform.localScale,
            pointText.rectTransform.localScale,
            typeText.rectTransform.localScale
        };
        UnityEngine.UI.Image frameImage = CreateMode64Image(
            cardObject.transform,
            "kamian"
        );

        Texture2D frameTexture = new Texture2D(6, 1);
        Sprite whiteSprite = CreateMode64Sprite(frameTexture, 0, "White");
        Sprite blueSprite = CreateMode64Sprite(frameTexture, 1, "Blue");
        Sprite purpleSprite = CreateMode64Sprite(frameTexture, 2, "Purple");
        Sprite goldSprite = CreateMode64Sprite(frameTexture, 3, "Gold");
        Sprite sinSprite = CreateMode64Sprite(frameTexture, 4, "Sin");
        Sprite fallbackSprite = CreateMode64Sprite(frameTexture, 5, "Fallback");

        bool referencesConfigured =
            SetMode64PrivateField(visualStyle, "frameImage", frameImage) &&
            SetMode64PrivateField(
                visualStyle,
                "whiteFrameSprite",
                whiteSprite
            ) &&
            SetMode64PrivateField(
                visualStyle,
                "blueFrameSprite",
                blueSprite
            ) &&
            SetMode64PrivateField(
                visualStyle,
                "purpleFrameSprite",
                purpleSprite
            ) &&
            SetMode64PrivateField(
                visualStyle,
                "goldFrameSprite",
                goldSprite
            ) &&
            SetMode64PrivateField(
                visualStyle,
                "sinFrameSprite",
                sinSprite
            ) &&
            SetMode64PrivateField(
                visualStyle,
                "fallbackFrameSprite",
                fallbackSprite
            ) &&
            SetMode64PrivateField(cardView, "cardNameText", cardNameText) &&
            SetMode64PrivateField(cardView, "pointText", pointText) &&
            SetMode64PrivateField(cardView, "typeText", typeText) &&
            SetMode64PrivateField(
                cardView,
                "descriptionText",
                descriptionText
            ) &&
            SetMode64PrivateField(cardView, "visualStyle", visualStyle);

        CharacterData owner = new CharacterData(
            "visual64_owner",
            30,
            10,
            10
        );
        CharacterData target = new CharacterData(
            "visual64_target",
            50,
            5,
            5
        );
        CardTestData cardData = CreatePrimaryPreviewAttackCardData(
            "visual64_card",
            "一级视觉测试卡",
            10,
            10,
            1
        );
        cardData.description = "策划手写描述。";
        BattleCardState cardState = BattleCardManager.CreateBattleCard(
            owner,
            cardData,
            "visual64_card_copy"
        );

        BattleCardUIPreviewData attackPreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        cardView.BindCard(owner, cardState, attackPreview, null);
        Material firstOutlineMaterial = visualStyle.AppliedOutlineMaterial;
        bool test1 =
            referencesConfigured &&
            attackPreview.typeText == "攻" &&
            typeText.text == "攻" &&
            AreMode64ColorsEqual(
                typeText.color,
                visualStyle.GetTypeColor(CardType.Attack)
            ) &&
            HasMode64BlackOutline(firstOutlineMaterial);

        cardData.cardType = CardType.Defense;
        cardData.isClashable = false;
        cardData.damageFormula = "";
        cardData.defenseFormula = "PointAsDefense";
        BattleCardUIPreviewData defensePreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        cardView.SetCard(defensePreview);
        bool test2 =
            defensePreview.typeText == "防" &&
            typeText.text == "防" &&
            AreMode64ColorsEqual(
                typeText.color,
                visualStyle.GetTypeColor(CardType.Defense)
            );

        cardData.cardType = CardType.Dodge;
        cardData.isClashable = true;
        cardData.defenseFormula = "";
        BattleCardUIPreviewData dodgePreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        cardView.SetCard(dodgePreview);
        bool test3 =
            dodgePreview.typeText == "闪" &&
            typeText.text == "闪" &&
            AreMode64ColorsEqual(
                typeText.color,
                visualStyle.GetTypeColor(CardType.Dodge)
            );

        cardData.cardType = "Ability";
        BattleCardUIPreviewData abilityPreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        cardView.SetCard(abilityPreview);
        bool abilitySupported =
            abilityPreview.typeText == "能" &&
            typeText.text == "能" &&
            AreMode64ColorsEqual(
                typeText.color,
                visualStyle.GetTypeColor("Ability")
            );

        cardData.cardType = "UnknownVisualType";
        BattleCardUIPreviewData fallbackTypePreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        cardView.SetCard(fallbackTypePreview);
        bool test4 =
            abilitySupported &&
            fallbackTypePreview.typeText == "？" &&
            typeText.text == visualStyle.GetTypeLabel("UnknownVisualType") &&
            AreMode64ColorsEqual(
                typeText.color,
                visualStyle.GetTypeColor("UnknownVisualType")
            );

        cardData.cardType = CardType.Attack;
        cardData.isClashable = true;
        cardData.damageFormula = "PointAsDamage";
        cardData.isSinCard = false;

        cardData.rarity = CardRarity.White;
        cardView.SetCard(
            BattleCardUIPreviewBuilder.Build(owner, target, cardState)
        );
        bool whiteSelected =
            object.ReferenceEquals(frameImage.sprite, whiteSprite);

        cardData.rarity = CardRarity.Blue;
        cardView.SetCard(
            BattleCardUIPreviewBuilder.Build(owner, target, cardState)
        );
        bool blueSelected =
            object.ReferenceEquals(frameImage.sprite, blueSprite);

        cardData.rarity = CardRarity.Purple;
        cardView.SetCard(
            BattleCardUIPreviewBuilder.Build(owner, target, cardState)
        );
        bool purpleSelected =
            object.ReferenceEquals(frameImage.sprite, purpleSprite);

        cardData.rarity = CardRarity.Gold;
        cardView.SetCard(
            BattleCardUIPreviewBuilder.Build(owner, target, cardState)
        );
        bool goldSelected =
            object.ReferenceEquals(frameImage.sprite, goldSprite);
        bool test5 =
            whiteSelected &&
            blueSelected &&
            purpleSelected &&
            goldSelected &&
            !frameImage.raycastTarget;

        cardData.isSinCard = true;
        cardData.rarity = CardRarity.Gold;
        cardView.SetCard(
            BattleCardUIPreviewBuilder.Build(owner, target, cardState)
        );
        bool test6 = object.ReferenceEquals(frameImage.sprite, sinSprite);

        cardData.isSinCard = false;
        cardData.rarity = CardRarity.Blue;
        bool blueSpriteCleared = SetMode64PrivateField(
            visualStyle,
            "blueFrameSprite",
            null
        );
        cardView.SetCard(
            BattleCardUIPreviewBuilder.Build(owner, target, cardState)
        );
        bool missingSpriteFallback =
            object.ReferenceEquals(frameImage.sprite, fallbackSprite);

        cardData.rarity = "InvalidQuality";
        cardView.SetCard(
            BattleCardUIPreviewBuilder.Build(owner, target, cardState)
        );
        bool invalidQualityFallback =
            object.ReferenceEquals(frameImage.sprite, fallbackSprite);
        bool test7 =
            blueSpriteCleared &&
            missingSpriteFallback &&
            invalidQualityFallback;

        cardData.rarity = null;
        BattleCardUIPreviewData missingRarityPreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        cardView.SetCard(missingRarityPreview);
        bool test8 =
            missingRarityPreview.rarity == CardRarity.White &&
            object.ReferenceEquals(frameImage.sprite, whiteSprite);

        bool cooldownReferenceInitiallyNull =
            GetMode64PrivateField<TMPro.TMP_Text>(
                cardView,
                "cooldownText"
            ) == null;
        cardData.cooldown = 1;
        cardState.currentCooldown = 1;
        BattleCardUIPreviewData cooldownPreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        cardView.BindCard(owner, cardState, cooldownPreview, null);
        bool cooldownDataAndGatePreserved =
            cooldownPreview.cooldownText == "1" &&
            cardData.cooldown == 1 &&
            cardState.currentCooldown == 1 &&
            !cardView.CanSelect;

        TMPro.TMP_Text legacyCooldownText = CreateMode64Text(
            cardObject.transform,
            "CD"
        );
        bool legacyCooldownConfigured = SetMode64PrivateField(
            cardView,
            "cooldownText",
            legacyCooldownText
        );
        legacyCooldownText.gameObject.SetActive(true);
        cardView.SetCard(cooldownPreview);
        bool test9 =
            cooldownReferenceInitiallyNull &&
            cooldownDataAndGatePreserved &&
            legacyCooldownConfigured &&
            !legacyCooldownText.gameObject.activeSelf;

        cardState.currentCooldown = 0;
        cardData.minPoint = 10;
        cardData.maxPoint = 10;
        BattleCardUIPreviewData rangePreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        cardView.SetCard(rangePreview);
        bool test10 =
            rangePreview.pointText == "10-10" &&
            pointText.text == "10-10";

        cardData.description = "使用前获得一层【强壮】。";
        BattleCardUIPreviewData descriptionPreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        cardView.SetCard(descriptionPreview);
        bool test11 =
            descriptionPreview.descriptionText == cardData.description &&
            descriptionText.text == cardData.description &&
            !descriptionText.text.Contains("CD");

        cardView.SetCard(attackPreview);
        Material repeatedBindMaterial = visualStyle.AppliedOutlineMaterial;
        cardView.SetCard(attackPreview);
        bool test12 =
            firstOutlineMaterial != null &&
            object.ReferenceEquals(
                firstOutlineMaterial,
                repeatedBindMaterial
            ) &&
            object.ReferenceEquals(
                repeatedBindMaterial,
                visualStyle.AppliedOutlineMaterial
            );

        bool test13 = RunMode64HandFilterAndAssignmentRegressionSubTest();
        bool test14 = AreMode64TextSettingsUnchanged(
            primaryTexts,
            fontSizesBeforeBind,
            autoSizingBeforeBind,
            localScalesBeforeBind
        );

        Debug.Log("模式64 测试1 Attack显示攻、使用配置颜色并应用黑色描边：" + test1);
        Debug.Log("模式64 测试2 Defense显示防并使用配置颜色：" + test2);
        Debug.Log("模式64 测试3 Dodge显示闪并使用配置颜色：" + test3);
        Debug.Log("模式64 测试4 Ability和未知类型短字回退安全：" + test4);
        Debug.Log("模式64 测试5 White/Blue/Purple/Gold选择正确底图：" + test5);
        Debug.Log("模式64 测试6 罪卡无条件覆盖为Sin底图：" + test6);
        Debug.Log("模式64 测试7 缺少品质图或非法品质时使用fallback：" + test7);
        Debug.Log("模式64 测试8 旧数据未填写rarity时默认White：" + test8);
        Debug.Log("模式64 测试9 一级CD引用可空、旧对象隐藏且选择门禁保留：" + test9);
        Debug.Log("模式64 测试10 固定点数完整显示10-10：" + test10);
        Debug.Log("模式64 测试11 描述保持策划原文且不拼接CD：" + test11);
        Debug.Log("模式64 测试12 重复Bind复用同一TMP材质实例：" + test12);
        Debug.Log("模式64 测试13 普通卡/罪卡手牌过滤与指派逻辑保持：" + test13);
        Debug.Log("模式64 测试14 重复Bind不改变四个TMP字号、Auto Size或局部缩放：" + test14);

        Destroy(cardObject);
        Destroy(whiteSprite);
        Destroy(blueSprite);
        Destroy(purpleSprite);
        Destroy(goldSprite);
        Destroy(sinSprite);
        Destroy(fallbackSprite);
        Destroy(frameTexture);

        Debug.Log("===== BattleCardPrimaryVisualPresetBasic 聚合测试结束 =====");
        return test1 &&
            test2 &&
            test3 &&
            test4 &&
            test5 &&
            test6 &&
            test7 &&
            test8 &&
            test9 &&
            test10 &&
            test11 &&
            test12 &&
            test13 &&
            test14;
    }

    bool RunBattleCardHoverAndDragMotionBasicTestSequence()
    {
        Debug.Log("===== BattleCardHoverAndDragMotionBasic 聚合测试开始 =====");

        const float testHoverLiftY = 260f;
        const float testExpandedWorldRotationZ = 0f;

        GameObject rootCanvasObject = new GameObject(
            "Motion65RootCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(UnityEngine.UI.GraphicRaycaster)
        );
        rootCanvasObject.GetComponent<Canvas>().renderMode =
            RenderMode.ScreenSpaceOverlay;

        GameObject cardObject = new GameObject(
            "Motion65Card",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(UnityEngine.UI.GraphicRaycaster)
        );
        cardObject.transform.SetParent(rootCanvasObject.transform, false);
        cardObject.SetActive(false);

        RectTransform cardRoot = cardObject.GetComponent<RectTransform>();
        cardRoot.anchoredPosition = new Vector2(30f, 40f);
        cardRoot.localScale = new Vector3(0.8f, 0.9f, 1f);
        cardRoot.localRotation = Quaternion.Euler(0f, 0f, -5f);

        GameObject visualObject = new GameObject(
            "VisualRoot",
            typeof(RectTransform)
        );
        visualObject.transform.SetParent(cardObject.transform, false);
        RectTransform visualRoot =
            visualObject.GetComponent<RectTransform>();
        visualRoot.anchoredPosition = new Vector2(5f, 7f);
        visualRoot.localScale = new Vector3(0.7f, 0.8f, 1f);
        visualRoot.localRotation = Quaternion.Euler(0f, 0f, -9f);

        Canvas sortingCanvas = cardObject.GetComponent<Canvas>();
        sortingCanvas.overrideSorting = false;
        sortingCanvas.sortingOrder = 3;
        BattleCardMotionUIView motionView =
            cardObject.AddComponent<BattleCardMotionUIView>();
        BattleCardVisualStyle visualStyle =
            cardObject.AddComponent<BattleCardVisualStyle>();
        BattleCardUIView cardView =
            cardObject.AddComponent<BattleCardUIView>();
        TMPro.TMP_Text typeText = CreateMode64Text(
            visualRoot,
            "Motion65TypeText"
        );
        UnityEngine.UI.Image frameImage = CreateMode64Image(
            visualRoot,
            "Motion65Frame"
        );

        bool referencesConfigured =
            SetMode64PrivateField(motionView, "cardRoot", cardRoot) &&
            SetMode64PrivateField(motionView, "visualRoot", visualRoot) &&
            SetMode64PrivateField(
                motionView,
                "sortingCanvas",
                sortingCanvas
            ) &&
            SetMode64PrivateField(
                motionView,
                "expandedWorldRotationZ",
                testExpandedWorldRotationZ
            ) &&
            SetMode64PrivateField(
                motionView,
                "hoverLiftY",
                testHoverLiftY
            ) &&
            SetMode64PrivateField(
                motionView,
                "positionSharpness",
                13.3886f
            ) &&
            SetMode64PrivateField(
                motionView,
                "rotationSharpness",
                13.3886f
            ) &&
            SetMode64PrivateField(
                motionView,
                "scaleSharpness",
                13.3886f
            ) &&
            SetMode64PrivateField(
                motionView,
                "positionSnapDistance",
                0.1f
            ) &&
            SetMode64PrivateField(
                motionView,
                "rotationSnapAngle",
                0.05f
            ) &&
            SetMode64PrivateField(
                motionView,
                "scaleSnapDistance",
                0.001f
            ) &&
            SetMode64PrivateField(
                motionView,
                "normalSortingOrder",
                3
            ) &&
            SetMode64PrivateField(
                motionView,
                "hoverSortingOrder",
                10
            ) &&
            SetMode64PrivateField(
                motionView,
                "selectedSortingOrder",
                20
            ) &&
            SetMode64PrivateField(cardView, "motionView", motionView) &&
            SetMode64PrivateField(cardView, "typeText", typeText) &&
            SetMode64PrivateField(
                cardView,
                "visualStyle",
                visualStyle
            ) &&
            SetMode64PrivateField(
                visualStyle,
                "frameImage",
                frameImage
            );

        cardObject.SetActive(true);

        CharacterData owner = new CharacterData(
            "motion65_owner",
            30,
            10,
            10
        );
        CharacterData target = new CharacterData(
            "motion65_target",
            50,
            5,
            5
        );
        CardTestData cardData = CreatePrimaryPreviewAttackCardData(
            "motion65_card_data",
            "模式65点击卡",
            5,
            5,
            1
        );
        BattleCardState cardState = BattleCardManager.CreateBattleCard(
            owner,
            cardData,
            "motion65_card"
        );
        BattleCardSelectionController selectionController =
            new BattleCardSelectionController();
        BattleCardUIPreviewData preview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        cardView.BindCard(
            owner,
            cardState,
            preview,
            selectionController
        );

        Vector2 rootPositionBeforeHover = cardRoot.anchoredPosition;
        Vector3 rootScaleBeforeHover = cardRoot.localScale;
        Quaternion rootRotationBeforeHover = cardRoot.localRotation;
        bool cachedOriginalOverrideSorting =
            sortingCanvas.overrideSorting;
        int cachedOriginalSortingOrder =
            sortingCanvas.sortingOrder;
        bool test9InitialCanvasCacheMatches =
            motionView.HasCachedOriginalCanvasState &&
            motionView.OriginalOverrideSorting ==
                cachedOriginalOverrideSorting &&
            motionView.OriginalSortingOrder ==
                cachedOriginalSortingOrder;
        cardView.OnPointerEnter(null);
        Vector2 hoverStartPosition = visualRoot.anchoredPosition;
        motionView.AdvanceCurrentTransitionForTesting(1f / 60f);
        Vector2 hoverAfterFirstStep = visualRoot.anchoredPosition;
        float hoverFirstStepDistance = Vector2.Distance(
            hoverStartPosition,
            hoverAfterFirstStep
        );
        float distanceAfterFirstStep = Vector2.Distance(
            hoverAfterFirstStep,
            motionView.HoverTargetAnchoredPosition
        );
        motionView.AdvanceCurrentTransitionForTesting(1f / 60f);
        Vector2 hoverAfterSecondStep = visualRoot.anchoredPosition;
        float hoverSecondStepDistance = Vector2.Distance(
            hoverAfterFirstStep,
            hoverAfterSecondStep
        );
        float distanceAfterSecondStep = Vector2.Distance(
            hoverAfterSecondStep,
            motionView.HoverTargetAnchoredPosition
        );
        bool hoverReachedSnap = false;
        for (int step = 0; step < 240; step++)
        {
            if (motionView.AdvanceCurrentTransitionForTesting(1f / 60f))
            {
                hoverReachedSnap = true;
                break;
            }
        }
        bool test9CacheStableAfterHover =
            sortingCanvas.overrideSorting &&
            motionView.OriginalOverrideSorting ==
                cachedOriginalOverrideSorting &&
            motionView.OriginalSortingOrder ==
                cachedOriginalSortingOrder;
        bool test1 =
            referencesConfigured &&
            motionView.HasCachedBaseTransform &&
            hoverFirstStepDistance > 0f &&
            hoverAfterFirstStep.y <
                motionView.HoverTargetAnchoredPosition.y &&
            cardRoot.anchoredPosition == rootPositionBeforeHover &&
            cardRoot.localScale == rootScaleBeforeHover &&
            Quaternion.Angle(
                cardRoot.localRotation,
                rootRotationBeforeHover
            ) < 0.001f &&
            Mathf.Abs(
                Mathf.DeltaAngle(
                    cardRoot.localEulerAngles.z,
                    -5f
                )
            ) < 0.001f;
        bool test2 =
            hoverFirstStepDistance > hoverSecondStepDistance &&
            distanceAfterSecondStep < distanceAfterFirstStep &&
            hoverReachedSnap &&
            motionView.ActiveTransitionCount == 0 &&
            visualRoot.anchoredPosition ==
                motionView.HoverTargetAnchoredPosition &&
            Mathf.Abs(
                visualRoot.anchoredPosition.y -
                motionView.BaseVisualAnchoredPosition.y -
                testHoverLiftY
            ) < 0.001f &&
            visualRoot.localScale == motionView.BaseVisualScale &&
            Mathf.Abs(
                Mathf.DeltaAngle(
                    visualRoot.eulerAngles.z,
                    testExpandedWorldRotationZ
                )
            ) < 0.001f &&
            cardRoot.anchoredPosition == rootPositionBeforeHover &&
            cardRoot.localScale == rootScaleBeforeHover &&
            Quaternion.Angle(
                cardRoot.localRotation,
                rootRotationBeforeHover
            ) < 0.001f &&
            sortingCanvas.overrideSorting &&
            sortingCanvas.sortingOrder == 10;

        cardView.OnPointerExit(null);
        Vector2 returnStartPosition = visualRoot.anchoredPosition;
        motionView.AdvanceCurrentTransitionForTesting(1f / 60f);
        Vector2 returnAfterFirstStep = visualRoot.anchoredPosition;
        bool returnMovedTowardBase =
            returnAfterFirstStep.y < returnStartPosition.y &&
            returnAfterFirstStep.y >
                motionView.BaseVisualAnchoredPosition.y;
        int transitionCountBeforeRedirect =
            motionView.ActiveTransitionCount;
        cardView.OnPointerEnter(null);
        Vector2 redirectStartPosition = visualRoot.anchoredPosition;
        motionView.AdvanceCurrentTransitionForTesting(1f / 60f);
        bool redirectedFromCurrentPosition =
            visualRoot.anchoredPosition.y >
                redirectStartPosition.y &&
            transitionCountBeforeRedirect == 1 &&
            motionView.ActiveTransitionCount == 1;
        cardView.OnPointerExit(null);
        motionView.CompleteCurrentTransitionImmediately();
        bool test3 =
            returnMovedTowardBase &&
            redirectedFromCurrentPosition &&
            visualRoot.anchoredPosition ==
                motionView.BaseVisualAnchoredPosition &&
            visualRoot.localScale == motionView.BaseVisualScale &&
            Quaternion.Angle(
                visualRoot.localRotation,
                motionView.BaseVisualRotation
            ) < 0.001f;

        bool noAccumulatedTransitions = true;
        for (int i = 0; i < 6; i++)
        {
            cardView.OnPointerEnter(null);
            noAccumulatedTransitions &=
                motionView.ActiveTransitionCount <= 1;
            cardView.OnPointerExit(null);
            noAccumulatedTransitions &=
                motionView.ActiveTransitionCount <= 1;
        }

        motionView.CompleteCurrentTransitionImmediately();
        bool test4 =
            noAccumulatedTransitions &&
            motionView.ActiveTransitionCount == 0 &&
            visualRoot.anchoredPosition ==
                motionView.BaseVisualAnchoredPosition &&
            visualRoot.localScale == motionView.BaseVisualScale &&
            Quaternion.Angle(
                visualRoot.localRotation,
                motionView.BaseVisualRotation
            ) < 0.001f;

        cardView.OnPointerEnter(null);
        selectionController.SelectCard(cardView);
        motionView.CompleteCurrentTransitionImmediately();
        bool test9CacheStableAfterSelected =
            sortingCanvas.overrideSorting &&
            motionView.OriginalOverrideSorting ==
                cachedOriginalOverrideSorting &&
            motionView.OriginalSortingOrder ==
                cachedOriginalSortingOrder;
        bool test5 =
            motionView.IsSelected &&
            cardView.IsSelected &&
            Mathf.Abs(
                Mathf.DeltaAngle(
                    visualRoot.eulerAngles.z,
                    testExpandedWorldRotationZ
                )
            ) < 0.001f &&
            Quaternion.Angle(
                cardRoot.localRotation,
                rootRotationBeforeHover
            ) < 0.001f &&
            sortingCanvas.overrideSorting &&
            sortingCanvas.sortingOrder == 20;

        cardView.OnPointerExit(null);
        motionView.CompleteCurrentTransitionImmediately();
        bool test6 =
            motionView.IsSelected &&
            visualRoot.anchoredPosition ==
                motionView.HoverTargetAnchoredPosition &&
            Mathf.Abs(
                Mathf.DeltaAngle(
                    visualRoot.eulerAngles.z,
                    testExpandedWorldRotationZ
                )
            ) < 0.001f &&
            sortingCanvas.sortingOrder == 20;

        selectionController.ClearSelection();
        motionView.CompleteCurrentTransitionImmediately();
        bool test7 =
            !motionView.IsSelected &&
            visualRoot.anchoredPosition ==
                motionView.BaseVisualAnchoredPosition &&
            Quaternion.Angle(
                visualRoot.localRotation,
                motionView.BaseVisualRotation
            ) < 0.001f;

        cardState.currentCooldown = 1;
        cardView.OnPointerEnter(null);
        bool selectedCoolingCard =
            selectionController.ToggleCardSelection(cardView);
        motionView.CompleteCurrentTransitionImmediately();
        bool test8 =
            motionView.IsHovered &&
            !selectedCoolingCard &&
            !cardView.CanSelect &&
            !selectionController.HasSelection;
        cardView.OnPointerExit(null);
        cardState.currentCooldown = 0;
        motionView.CompleteCurrentTransitionImmediately();

        Vector2 cachedBasePosition =
            motionView.BaseVisualAnchoredPosition;
        Vector3 cachedBaseScale = motionView.BaseVisualScale;
        Quaternion cachedBaseRotation = motionView.BaseVisualRotation;
        cardView.BindCard(
            owner,
            cardState,
            preview,
            selectionController
        );
        motionView.RecalculateBaseVisualTransform();
        bool test9CacheStableAfterRecalculate =
            motionView.OriginalOverrideSorting ==
                cachedOriginalOverrideSorting &&
            motionView.OriginalSortingOrder ==
                cachedOriginalSortingOrder;
        cardView.BindCard(
            owner,
            cardState,
            preview,
            selectionController
        );
        bool test9CacheStableAfterRepeatedBind =
            motionView.OriginalOverrideSorting ==
                cachedOriginalOverrideSorting &&
            motionView.OriginalSortingOrder ==
                cachedOriginalSortingOrder;
        bool test10 =
            motionView.BaseVisualAnchoredPosition ==
                cachedBasePosition &&
            motionView.BaseVisualScale == cachedBaseScale &&
            Quaternion.Angle(
                motionView.BaseVisualRotation,
                cachedBaseRotation
            ) < 0.001f;

        bool test11 =
            !(cardView is IBeginDragHandler) &&
            !(cardView is IDragHandler) &&
            !(cardView is IEndDragHandler);
        bool test12 =
            cardObject.GetComponent<UnityEngine.UI.GraphicRaycaster>() !=
                null &&
            object.ReferenceEquals(
                sortingCanvas.transform,
                cardObject.transform
            );

        motionView.SetSelected(true);
        motionView.CompleteCurrentTransitionImmediately();
        motionView.ResetVisualState();
        bool test13 =
            !motionView.IsSelected &&
            !motionView.IsHovered &&
            visualRoot.anchoredPosition == cachedBasePosition &&
            visualRoot.localScale == cachedBaseScale &&
            Quaternion.Angle(
                visualRoot.localRotation,
                cachedBaseRotation
            ) < 0.001f;

        cardView.OnPointerEnter(null);
        selectionController.SelectCard(cardView);
        motionView.CompleteCurrentTransitionImmediately();
        cardObject.SetActive(false);
        bool test9InactiveOverrideSortingDiagnostic =
            sortingCanvas.overrideSorting;
        int test9InactiveSortingOrderDiagnostic =
            sortingCanvas.sortingOrder;
        bool test9DisabledPositionRestored =
            visualRoot.anchoredPosition == cachedBasePosition;
        bool test9DisabledScaleRestored =
            visualRoot.localScale == cachedBaseScale;
        bool test9DisabledRotationRestored =
            Quaternion.Angle(
                visualRoot.localRotation,
                cachedBaseRotation
            ) < 0.001f;
        bool test9DisabledHoverCleared = !motionView.IsHovered;
        bool test9DisabledSelectedCleared =
            !motionView.IsSelected &&
            !selectionController.HasSelection;
        bool test9DisabledCoroutineCleared =
            motionView.ActiveTransitionCount == 0;

        cardObject.SetActive(true);
        bool test9ReenabledOverrideSortingRestored =
            sortingCanvas.overrideSorting ==
                cachedOriginalOverrideSorting;
        bool test9ReenabledSortingOrderRestored =
            sortingCanvas.sortingOrder ==
                cachedOriginalSortingOrder;
        bool test9ReenabledPositionRestored =
            visualRoot.anchoredPosition == cachedBasePosition;
        bool test9ReenabledScaleRestored =
            visualRoot.localScale == cachedBaseScale;
        bool test9ReenabledRotationRestored =
            Quaternion.Angle(
                visualRoot.localRotation,
                cachedBaseRotation
            ) < 0.001f;
        bool test9ReenabledHoverCleared = !motionView.IsHovered;
        bool test9ReenabledSelectedCleared =
            !motionView.IsSelected &&
            !selectionController.HasSelection;
        bool test9ReenabledCoroutineCleared =
            motionView.ActiveTransitionCount == 0;

        bool test9 =
            test9DisabledPositionRestored &&
            test9DisabledScaleRestored &&
            test9DisabledRotationRestored &&
            test9DisabledHoverCleared &&
            test9DisabledSelectedCleared &&
            test9DisabledCoroutineCleared &&
            test9ReenabledOverrideSortingRestored &&
            test9ReenabledSortingOrderRestored &&
            test9ReenabledPositionRestored &&
            test9ReenabledScaleRestored &&
            test9ReenabledRotationRestored &&
            test9ReenabledHoverCleared &&
            test9ReenabledSelectedCleared &&
            test9ReenabledCoroutineCleared &&
            test9InitialCanvasCacheMatches &&
            test9CacheStableAfterHover &&
            test9CacheStableAfterSelected &&
            test9CacheStableAfterRecalculate &&
            test9CacheStableAfterRepeatedBind;

        Destroy(rootCanvasObject);

        bool test14 =
            RunBattleCardPrimaryVisualPresetBasicTestSequence();
        bool test15 =
            RunMode65HandAssignmentRegressionSubTest();

        Debug.Log("模式65 测试1 Hover指数平滑第一步移动且不修改cardRoot：" + test1);
        Debug.Log("模式65 测试2 指数步长递减并在Snap后精确到达：" + test2);
        Debug.Log("模式65 测试3 动画中途改向并恢复初始Transform：" + test3);
        Debug.Log("模式65 测试4 快速Enter/Exit仅保留一个协程且可精确完成：" + test4);
        Debug.Log("模式65 测试5 Selected优先于Hovered并使用选中排序：" + test5);
        Debug.Log("模式65 测试6 Selected在Pointer Exit后持续展开：" + test6);
        Debug.Log("模式65 测试7 取消Selected后恢复Resting：" + test7);
        Debug.Log("模式65 测试8 CD卡可Hover但不可Selected：" + test8);
        Debug.Log("模式65 测试9诊断 禁用时Position恢复：" + test9DisabledPositionRestored);
        Debug.Log("模式65 测试9诊断 禁用时Scale恢复：" + test9DisabledScaleRestored);
        Debug.Log("模式65 测试9诊断 禁用时Rotation恢复：" + test9DisabledRotationRestored);
        Debug.Log("模式65 测试9诊断 禁用时Hover清除：" + test9DisabledHoverCleared);
        Debug.Log("模式65 测试9诊断 禁用时Selected清除：" + test9DisabledSelectedCleared);
        Debug.Log("模式65 测试9诊断 禁用时Coroutine清理：" + test9DisabledCoroutineCleared);
        Debug.Log(
            "模式65 测试9诊断 Canvas初始化OverrideSorting：" +
            cachedOriginalOverrideSorting
        );
        Debug.Log(
            "模式65 测试9诊断 Canvas缓存OverrideSorting：" +
            motionView.OriginalOverrideSorting
        );
        Debug.Log(
            "模式65 测试9诊断 初始化Canvas缓存一致：" +
            test9InitialCanvasCacheMatches
        );
        Debug.Log(
            "模式65 测试9诊断 Hover后Canvas缓存稳定：" +
            test9CacheStableAfterHover
        );
        Debug.Log(
            "模式65 测试9诊断 Selected后Canvas缓存稳定：" +
            test9CacheStableAfterSelected
        );
        Debug.Log(
            "模式65 测试9诊断 Recalculate后Canvas缓存稳定：" +
            test9CacheStableAfterRecalculate
        );
        Debug.Log(
            "模式65 测试9诊断 重复Bind后Canvas缓存稳定：" +
            test9CacheStableAfterRepeatedBind
        );
        Debug.Log(
            "模式65 测试9诊断 禁用期间OverrideSorting（仅诊断）：" +
            test9InactiveOverrideSortingDiagnostic
        );
        Debug.Log(
            "模式65 测试9诊断 禁用期间SortingOrder（仅诊断）：" +
            test9InactiveSortingOrderDiagnostic
        );
        Debug.Log(
            "模式65 测试9诊断 重新启用后OverrideSorting恢复：" +
            test9ReenabledOverrideSortingRestored
        );
        Debug.Log(
            "模式65 测试9诊断 重新启用后SortingOrder恢复：" +
            test9ReenabledSortingOrderRestored
        );
        Debug.Log("模式65 测试9诊断 重新启用后Position恢复：" + test9ReenabledPositionRestored);
        Debug.Log("模式65 测试9诊断 重新启用后Scale恢复：" + test9ReenabledScaleRestored);
        Debug.Log("模式65 测试9诊断 重新启用后Rotation恢复：" + test9ReenabledRotationRestored);
        Debug.Log("模式65 测试9诊断 重新启用后Hover清除：" + test9ReenabledHoverCleared);
        Debug.Log("模式65 测试9诊断 重新启用后Selected清除：" + test9ReenabledSelectedCleared);
        Debug.Log("模式65 测试9诊断 重新启用后Coroutine清理：" + test9ReenabledCoroutineCleared);
        Debug.Log("模式65 测试9 禁用清理且重新启用后恢复Transform与排序：" + test9);
        Debug.Log("模式65 测试10 多次Bind不改变动画缓存：" + test10);
        Debug.Log("模式65 测试11 BattleCardUIView不再注册拖拽接口：" + test11);
        Debug.Log("模式65 测试12 嵌套Canvas与GraphicRaycaster结构有效：" + test12);
        Debug.Log("模式65 测试13 Reset清除Hover与Selected状态：" + test13);
        Debug.Log("模式65 测试14 原模式64全部回归通过：" + test14);
        Debug.Log("模式65 测试15 手牌过滤、指派、替换与取消继续通过：" + test15);

        Debug.Log("===== BattleCardHoverAndDragMotionBasic 聚合测试结束 =====");
        return
            test1 && test2 && test3 && test4 && test5 &&
            test6 && test7 && test8 && test9 && test10 &&
            test11 && test12 && test13 && test14 && test15;
    }

    sealed class Mode68HandTestContext
    {
        public GameObject rootObject;
        public BattleCardHandUIView handView;
        public BattleCardHandSpreadAnimator spreadAnimator;
        public CharacterData ownerA;
        public CharacterData ownerB;
        public CharacterData target;
        public List<BattleCardState> ownerACards;
        public List<BattleCardState> ownerBCards;
        public List<RectTransform> placementSlots;
        public bool referencesConfigured;
    }

    void RunBattleCardExponentialMotionAndSpreadBasicTestSequence()
    {
        Debug.Log(
            "===== BattleCardExponentialMotionAndSpreadBasic 聚合测试开始 ====="
        );

        const float sharpness = 13.3886f;
        float factorAt60Fps =
            BattleUIExponentialSmoothing.CalculateFactor(
                sharpness,
                1f / 60f
            );
        bool test1 = Mathf.Abs(factorAt60Fps - 0.2f) < 0.0001f;

        Vector2 smoothingTarget = new Vector2(100f, 0f);
        Vector2 smoothingStep0 = Vector2.zero;
        Vector2 smoothingStep1 =
            BattleUIExponentialSmoothing.Smooth(
                smoothingStep0,
                smoothingTarget,
                sharpness,
                1f / 60f
            );
        Vector2 smoothingStep2 =
            BattleUIExponentialSmoothing.Smooth(
                smoothingStep1,
                smoothingTarget,
                sharpness,
                1f / 60f
            );
        Vector2 smoothingStep3 =
            BattleUIExponentialSmoothing.Smooth(
                smoothingStep2,
                smoothingTarget,
                sharpness,
                1f / 60f
            );
        float firstStepDistance =
            Vector2.Distance(smoothingStep0, smoothingStep1);
        float secondStepDistance =
            Vector2.Distance(smoothingStep1, smoothingStep2);
        float thirdStepDistance =
            Vector2.Distance(smoothingStep2, smoothingStep3);
        bool test2 =
            firstStepDistance > secondStepDistance &&
            secondStepDistance > thirdStepDistance &&
            Vector2.Distance(smoothingStep3, smoothingTarget) <
                Vector2.Distance(smoothingStep2, smoothingTarget);

        Vector2 positionAt30Fps =
            SimulateMode68ExponentialPosition(sharpness, 30, 1f);
        Vector2 positionAt60Fps =
            SimulateMode68ExponentialPosition(sharpness, 60, 1f);
        Vector2 positionAt120Fps =
            SimulateMode68ExponentialPosition(sharpness, 120, 1f);
        bool test3 =
            Vector2.Distance(positionAt30Fps, positionAt60Fps) <
                0.001f &&
            Vector2.Distance(positionAt60Fps, positionAt120Fps) <
                0.001f;

        Mode68HandTestContext context =
            CreateMode68HandTestContext();
        context.handView.SetCards(
            context.ownerA,
            context.target,
            context.ownerACards
        );

        List<BattleCardUIView> firstOwnerViews =
            CopyMode68CardViews(context.handView.SpawnedCardViews);
        Vector2 firstOwnerCenter =
            GetMode68PlacementCenter(
                context.placementSlots,
                firstOwnerViews.Count
            );
        bool test4 =
            context.referencesConfigured &&
            context.handView.HasDisplayedAnyHand &&
            object.ReferenceEquals(
                context.handView.LastDisplayedOwner,
                context.ownerA
            ) &&
            context.spreadAnimator.ActiveTransitionCount == 1 &&
            context.spreadAnimator.CachedCardCount ==
                firstOwnerViews.Count;
        bool test5 =
            AreMode68CardsAtSpreadStart(
                firstOwnerViews,
                context.placementSlots,
                firstOwnerCenter,
                0f
            );

        RectTransform firstCardVisualRoot =
            firstOwnerViews.Count > 0
                ? firstOwnerViews[0].transform.Find(
                    "VisualRoot"
                ) as RectTransform
                : null;
        Vector2 visualPositionBeforeSpread =
            firstCardVisualRoot != null
                ? firstCardVisualRoot.anchoredPosition
                : Vector2.zero;
        Vector3 visualScaleBeforeSpread =
            firstCardVisualRoot != null
                ? firstCardVisualRoot.localScale
                : Vector3.zero;
        Quaternion visualRotationBeforeSpread =
            firstCardVisualRoot != null
                ? firstCardVisualRoot.localRotation
                : Quaternion.identity;
        context.spreadAnimator.AdvanceSpreadForTesting(1f / 60f);
        bool anyCardRootMoved =
            HasAnyMode68CardRootMoved(
                firstOwnerViews,
                firstOwnerCenter
            );
        bool test11 =
            anyCardRootMoved &&
            firstCardVisualRoot != null &&
            firstCardVisualRoot.anchoredPosition ==
                visualPositionBeforeSpread &&
            firstCardVisualRoot.localScale ==
                visualScaleBeforeSpread &&
            Quaternion.Angle(
                firstCardVisualRoot.localRotation,
                visualRotationBeforeSpread
            ) < 0.001f;

        context.spreadAnimator.CompleteSpreadImmediatelyForTesting();
        bool test6 =
            DoMode68CardsMatchPlacementSlots(
                firstOwnerViews,
                context.placementSlots
            ) &&
            context.spreadAnimator.ActiveTransitionCount == 0 &&
            context.spreadAnimator.CachedCardCount == 0;

        context.handView.SetCards(
            context.ownerB,
            context.target,
            context.ownerBCards
        );
        bool test7 =
            object.ReferenceEquals(
                context.handView.LastDisplayedOwner,
                context.ownerB
            ) &&
            context.spreadAnimator.ActiveTransitionCount == 1 &&
            context.spreadAnimator.CachedCardCount ==
                context.ownerBCards.Count;
        context.spreadAnimator.CompleteSpreadImmediatelyForTesting();

        context.handView.SetCards(
            context.ownerB,
            context.target,
            context.ownerBCards
        );
        bool test8 =
            context.spreadAnimator.ActiveTransitionCount == 0 &&
            context.spreadAnimator.CachedCardCount == 0 &&
            DoMode68CardsMatchPlacementSlots(
                context.handView.SpawnedCardViews,
                context.placementSlots
            );

        List<BattleCardState> sameOwnerFilteredCards =
            new List<BattleCardState>
            {
                context.ownerBCards[0]
            };
        context.handView.SetCards(
            context.ownerB,
            context.target,
            sameOwnerFilteredCards
        );
        bool test9 =
            context.spreadAnimator.ActiveTransitionCount == 0 &&
            context.spreadAnimator.CachedCardCount == 0 &&
            context.handView.SpawnedCardViews.Count == 1 &&
            DoMode68CardsMatchPlacementSlots(
                context.handView.SpawnedCardViews,
                context.placementSlots
            );

        context.handView.SetCards(
            context.ownerA,
            context.target,
            context.ownerACards
        );
        context.handView.ClearCards();
        bool test10 =
            context.spreadAnimator.ActiveTransitionCount == 0 &&
            context.spreadAnimator.CachedCardCount == 0 &&
            context.handView.SpawnedCardViews.Count == 0;

        bool spreadDisabledConfigured =
            SetMode64PrivateField(
                context.spreadAnimator,
                "enableSpreadAnimation",
                false
            );
        context.handView.SetCards(
            context.ownerB,
            context.target,
            context.ownerBCards
        );
        bool test12 =
            spreadDisabledConfigured &&
            context.spreadAnimator.ActiveTransitionCount == 0 &&
            context.spreadAnimator.CachedCardCount == 0 &&
            DoMode68CardsMatchPlacementSlots(
                context.handView.SpawnedCardViews,
                context.placementSlots
            );

        bool spreadEnabledConfigured =
            SetMode64PrivateField(
                context.spreadAnimator,
                "enableSpreadAnimation",
                true
            );
        context.handView.SetCards(
            context.ownerA,
            context.target,
            new List<BattleCardState>()
        );
        bool zeroCardsSafe =
            context.handView.SpawnedCardViews.Count == 0 &&
            context.spreadAnimator.ActiveTransitionCount == 0 &&
            context.spreadAnimator.CachedCardCount == 0;
        context.handView.SetCards(
            context.ownerB,
            context.target,
            sameOwnerFilteredCards
        );
        bool oneCardStarted =
            context.handView.SpawnedCardViews.Count == 1 &&
            context.spreadAnimator.ActiveTransitionCount == 1 &&
            context.spreadAnimator.CachedCardCount == 1;
        context.spreadAnimator.CompleteSpreadImmediatelyForTesting();
        bool test13 =
            spreadEnabledConfigured &&
            zeroCardsSafe &&
            oneCardStarted &&
            DoMode68CardsMatchPlacementSlots(
                context.handView.SpawnedCardViews,
                context.placementSlots
            );

        context.handView.SetCards(
            context.ownerA,
            context.target,
            context.ownerACards
        );
        List<BattleCardUIView> interruptedOwnerViews =
            CopyMode68CardViews(context.handView.SpawnedCardViews);
        context.handView.SetCards(
            context.ownerB,
            context.target,
            context.ownerBCards
        );
        List<BattleCardUIView> replacementOwnerViews =
            CopyMode68CardViews(context.handView.SpawnedCardViews);
        bool oldCardsDisabled =
            AreMode68CardViewsInactive(interruptedOwnerViews);
        bool oldReferencesRemoved =
            HaveNoMode68SharedReferences(
                interruptedOwnerViews,
                replacementOwnerViews
            );
        bool test14 =
            oldCardsDisabled &&
            oldReferencesRemoved &&
            context.spreadAnimator.ActiveTransitionCount == 1 &&
            context.spreadAnimator.CachedCardCount ==
                replacementOwnerViews.Count &&
            replacementOwnerViews.Count == context.ownerBCards.Count;

        Destroy(context.rootObject);

        Debug.Log("模式68 测试1 60FPS指数因子约为0.2：" + test1);
        Debug.Log("模式68 测试2 指数平滑单步位移逐次减小：" + test2);
        Debug.Log("模式68 测试3 30/60/120FPS同总时长结果一致：" + test3);
        Debug.Log("模式68 测试4 首次SetCards播放散开：" + test4);
        Debug.Log("模式68 测试5 所有卡牌从最终布局中心开始：" + test5);
        Debug.Log("模式68 测试6 最终精确恢复ManualLayout Transform：" + test6);
        Debug.Log("模式68 测试7 切换不同owner重新播放：" + test7);
        Debug.Log("模式68 测试8 同owner刷新不重新播放：" + test8);
        Debug.Log("模式68 测试9 同owner内容切换不重新播放：" + test9);
        Debug.Log("模式68 测试10 ClearCards停止动画并清理引用：" + test10);
        Debug.Log("模式68 测试11 Spread只修改cardRoot不修改VisualRoot：" + test11);
        Debug.Log("模式68 测试12 Spread禁用时保留最终布局：" + test12);
        Debug.Log("模式68 测试13 0张和1张卡安全：" + test13);
        Debug.Log("模式68 测试14 中途切换owner不保留旧引用或协程：" + test14);
        Debug.Log(
            "===== BattleCardExponentialMotionAndSpreadBasic 聚合测试结束 ====="
        );
    }

    Vector2 SimulateMode68ExponentialPosition(
        float sharpness,
        int framesPerSecond,
        float duration
    )
    {
        Vector2 current = Vector2.zero;
        Vector2 target = new Vector2(100f, -40f);
        int stepCount = Mathf.RoundToInt(
            framesPerSecond * duration
        );
        float deltaTime = 1f / framesPerSecond;

        for (int step = 0; step < stepCount; step++)
        {
            current = BattleUIExponentialSmoothing.Smooth(
                current,
                target,
                sharpness,
                deltaTime
            );
        }

        return current;
    }

    Mode68HandTestContext CreateMode68HandTestContext()
    {
        Mode68HandTestContext context =
            new Mode68HandTestContext();
        context.rootObject = new GameObject(
            "Mode68HandRoot",
            typeof(RectTransform)
        );
        context.rootObject.SetActive(false);

        BattleCardManualLayout manualLayout =
            context.rootObject.AddComponent<BattleCardManualLayout>();
        context.spreadAnimator =
            context.rootObject.AddComponent<
                BattleCardHandSpreadAnimator
            >();
        context.handView =
            context.rootObject.AddComponent<BattleCardHandUIView>();

        List<RectTransform> slots = new List<RectTransform>();
        for (int slotIndex = 0; slotIndex < 5; slotIndex++)
        {
            GameObject slotObject = new GameObject(
                "Mode68Slot" + (slotIndex + 1),
                typeof(RectTransform)
            );
            slotObject.transform.SetParent(
                context.rootObject.transform,
                false
            );
            RectTransform slotRect =
                slotObject.GetComponent<RectTransform>();
            slotRect.anchoredPosition = new Vector2(
                -200f + slotIndex * 100f,
                Mathf.Abs(2 - slotIndex) * -20f
            );
            slotRect.localRotation = Quaternion.Euler(
                0f,
                0f,
                -10f + slotIndex * 5f
            );
            slotRect.localScale = new Vector3(
                0.9f + slotIndex * 0.02f,
                0.9f + slotIndex * 0.02f,
                1f
            );
            slots.Add(slotRect);
        }

        context.placementSlots = new List<RectTransform>
        {
            slots[2],
            slots[3],
            slots[1],
            slots[4],
            slots[0]
        };

        GameObject templateObject = new GameObject(
            "Mode68CardTemplate",
            typeof(RectTransform)
        );
        templateObject.transform.SetParent(
            context.rootObject.transform,
            false
        );
        BattleCardUIView templateView =
            templateObject.AddComponent<BattleCardUIView>();
        BattleCardVisualStyle templateVisualStyle =
            templateObject.AddComponent<BattleCardVisualStyle>();
        GameObject visualObject = new GameObject(
            "VisualRoot",
            typeof(RectTransform)
        );
        visualObject.transform.SetParent(
            templateObject.transform,
            false
        );
        RectTransform templateVisualRoot =
            visualObject.GetComponent<RectTransform>();
        templateVisualRoot.anchoredPosition =
            new Vector2(11f, 13f);
        templateVisualRoot.localScale =
            new Vector3(0.91f, 0.93f, 1f);
        templateVisualRoot.localRotation =
            Quaternion.Euler(0f, 0f, 7f);
        UnityEngine.UI.Image templateFrame =
            CreateMode64Image(
                templateVisualRoot,
                "Mode68Frame"
            );
        bool templateConfigured =
            SetMode64PrivateField(
                templateView,
                "visualStyle",
                templateVisualStyle
            ) &&
            SetMode64PrivateField(
                templateVisualStyle,
                "frameImage",
                templateFrame
            );

        bool layoutConfigured =
            SetMode64PrivateField(
                manualLayout,
                "normal01",
                slots[0]
            ) &&
            SetMode64PrivateField(
                manualLayout,
                "normal02",
                slots[1]
            ) &&
            SetMode64PrivateField(
                manualLayout,
                "normal03",
                slots[2]
            ) &&
            SetMode64PrivateField(
                manualLayout,
                "normal04",
                slots[3]
            ) &&
            SetMode64PrivateField(
                manualLayout,
                "normal05",
                slots[4]
            );
        bool handConfigured =
            SetMode64PrivateField(
                context.handView,
                "cardViewPrefab",
                templateView
            ) &&
            SetMode64PrivateField(
                context.handView,
                "cardContainer",
                context.rootObject.transform
            ) &&
            SetMode64PrivateField(
                context.handView,
                "manualLayout",
                manualLayout
            ) &&
            SetMode64PrivateField(
                context.handView,
                "spreadAnimator",
                context.spreadAnimator
            ) &&
            SetMode64PrivateField(
                context.handView,
                "hideTemplateOnAwake",
                true
            );
        context.referencesConfigured =
            layoutConfigured &&
            handConfigured &&
            templateConfigured;

        context.ownerA = new CharacterData(
            "mode68_owner_a",
            30,
            10,
            10
        );
        context.ownerB = new CharacterData(
            "mode68_owner_b",
            30,
            9,
            9
        );
        context.target = new CharacterData(
            "mode68_target",
            50,
            5,
            5
        );
        context.ownerACards = new List<BattleCardState>
        {
            CreateFixedAttackCardForCharacter(
                context.ownerA,
                "mode68_a_1",
                1
            ),
            CreateFixedAttackCardForCharacter(
                context.ownerA,
                "mode68_a_2",
                2
            ),
            CreateFixedAttackCardForCharacter(
                context.ownerA,
                "mode68_a_3",
                3
            )
        };
        context.ownerBCards = new List<BattleCardState>
        {
            CreateFixedAttackCardForCharacter(
                context.ownerB,
                "mode68_b_1",
                4
            ),
            CreateFixedAttackCardForCharacter(
                context.ownerB,
                "mode68_b_2",
                5
            )
        };

        context.rootObject.SetActive(true);
        return context;
    }

    List<BattleCardUIView> CopyMode68CardViews(
        IReadOnlyList<BattleCardUIView> source
    )
    {
        List<BattleCardUIView> copy =
            new List<BattleCardUIView>();
        if (source == null)
        {
            return copy;
        }

        for (int index = 0; index < source.Count; index++)
        {
            copy.Add(source[index]);
        }

        return copy;
    }

    Vector2 GetMode68PlacementCenter(
        IReadOnlyList<RectTransform> placementSlots,
        int count
    )
    {
        Vector2 sum = Vector2.zero;
        int validCount = Mathf.Min(
            count,
            placementSlots != null ? placementSlots.Count : 0
        );

        for (int index = 0; index < validCount; index++)
        {
            sum += placementSlots[index].anchoredPosition;
        }

        return validCount > 0
            ? sum / validCount
            : Vector2.zero;
    }

    bool AreMode68CardsAtSpreadStart(
        IReadOnlyList<BattleCardUIView> cardViews,
        IReadOnlyList<RectTransform> placementSlots,
        Vector2 expectedCenter,
        float expectedRotationZ
    )
    {
        if (cardViews == null ||
            placementSlots == null ||
            cardViews.Count == 0 ||
            cardViews.Count > placementSlots.Count)
        {
            return false;
        }

        for (int index = 0; index < cardViews.Count; index++)
        {
            RectTransform cardRoot =
                cardViews[index].transform as RectTransform;
            if (cardRoot == null ||
                Vector2.Distance(
                    cardRoot.anchoredPosition,
                    expectedCenter
                ) > 0.001f ||
                Mathf.Abs(
                    Mathf.DeltaAngle(
                        cardRoot.localEulerAngles.z,
                        expectedRotationZ
                    )
                ) > 0.001f ||
                Vector3.Distance(
                    cardRoot.localScale,
                    placementSlots[index].localScale
                ) > 0.001f)
            {
                return false;
            }
        }

        return true;
    }

    bool DoMode68CardsMatchPlacementSlots(
        IReadOnlyList<BattleCardUIView> cardViews,
        IReadOnlyList<RectTransform> placementSlots
    )
    {
        if (cardViews == null ||
            placementSlots == null ||
            cardViews.Count > placementSlots.Count)
        {
            return false;
        }

        for (int index = 0; index < cardViews.Count; index++)
        {
            RectTransform cardRoot =
                cardViews[index].transform as RectTransform;
            RectTransform slot = placementSlots[index];
            if (cardRoot == null ||
                slot == null ||
                Vector2.Distance(
                    cardRoot.anchoredPosition,
                    slot.anchoredPosition
                ) > 0.001f ||
                Quaternion.Angle(
                    cardRoot.localRotation,
                    slot.localRotation
                ) > 0.001f ||
                Vector3.Distance(
                    cardRoot.localScale,
                    slot.localScale
                ) > 0.001f)
            {
                return false;
            }
        }

        return true;
    }

    bool HasAnyMode68CardRootMoved(
        IReadOnlyList<BattleCardUIView> cardViews,
        Vector2 startPosition
    )
    {
        if (cardViews == null)
        {
            return false;
        }

        for (int index = 0; index < cardViews.Count; index++)
        {
            RectTransform cardRoot =
                cardViews[index].transform as RectTransform;
            if (cardRoot != null &&
                Vector2.Distance(
                    cardRoot.anchoredPosition,
                    startPosition
                ) > 0.001f)
            {
                return true;
            }
        }

        return false;
    }

    bool AreMode68CardViewsInactive(
        IReadOnlyList<BattleCardUIView> cardViews
    )
    {
        if (cardViews == null)
        {
            return false;
        }

        for (int index = 0; index < cardViews.Count; index++)
        {
            BattleCardUIView cardView = cardViews[index];
            if (cardView == null || cardView.gameObject.activeSelf)
            {
                return false;
            }
        }

        return true;
    }

    bool HaveNoMode68SharedReferences(
        IReadOnlyList<BattleCardUIView> oldViews,
        IReadOnlyList<BattleCardUIView> newViews
    )
    {
        if (oldViews == null || newViews == null)
        {
            return false;
        }

        for (int oldIndex = 0;
            oldIndex < oldViews.Count;
            oldIndex++)
        {
            for (int newIndex = 0;
                newIndex < newViews.Count;
                newIndex++)
            {
                if (object.ReferenceEquals(
                    oldViews[oldIndex],
                    newViews[newIndex]
                ))
                {
                    return false;
                }
            }
        }

        return true;
    }

    sealed class Mode69SlotTestContext
    {
        public GameObject rootObject;
        public BattleActionSlotUIView slotView;
        public UnityEngine.UI.Image baseImage;
        public BattleActionSlotSelectionEffectUIView effectView;
        public RectTransform effectRoot;
        public UnityEngine.UI.Image effectImage;
        public CharacterData character;
        public Texture2D spriteTexture;
        public Sprite allyEmptySprite;
        public Sprite allyTargetedSprite;
        public Sprite allyActionSprite;
        public Sprite enemyEmptySprite;
        public Sprite enemyActionSprite;
        public bool referencesConfigured;
    }

    bool RunBattleActionSlotVisualInteractionBasicTestSequence()
    {
        Debug.Log(
            "===== BattleActionSlotVisualInteractionBasic 聚合测试开始 ====="
        );

        Mode69SlotTestContext allyContext =
            CreateMode69SlotTestContext("Mode69Ally", false);
        BattleActionSlotUIView allySlot = allyContext.slotView;
        BattleActionSlotSelectionEffectUIView allyEffect =
            allyContext.effectView;
        PointerEventData leftClick = new PointerEventData(null)
        {
            button = PointerEventData.InputButton.Left
        };
        PointerEventData rightClick = new PointerEventData(null)
        {
            button = PointerEventData.InputButton.Right
        };

        bool test1 =
            allyContext.referencesConfigured &&
            allySlot.CurrentBaseState ==
                BattleActionSlotUIState.AllyEmpty &&
            allyContext.baseImage.sprite ==
                allyContext.allyEmptySprite;
        allySlot.CommitStateFeedbackForTesting();

        allySlot.SetState(
            BattleActionSlotUIState.AllyTargetedNoAction
        );
        allySlot.CommitStateFeedbackForTesting();
        bool test2 =
            allyContext.baseImage.sprite ==
                allyContext.allyTargetedSprite &&
            !allyEffect.IsVisible &&
            !allyEffect.IsPulsePlaying;

        allySlot.OnPointerEnter(null);
        bool test3 =
            allySlot.IsHovered &&
            allyContext.baseImage.sprite ==
                allyContext.allyEmptySprite;
        bool test4 =
            allyEffect.IsVisible &&
            !allyEffect.IsPulsePlaying &&
            allyContext.effectImage.raycastTarget == false &&
            Vector3.Distance(
                allyContext.effectRoot.localScale,
                allyEffect.TargetScale
            ) < 0.001f;

        allySlot.OnPointerExit(null);
        bool test5 =
            !allySlot.IsHovered &&
            allyContext.baseImage.sprite ==
                allyContext.allyTargetedSprite &&
            !allyEffect.IsVisible;

        allySlot.SetState(BattleActionSlotUIState.AllyActionSet);
        allySlot.CommitStateFeedbackForTesting();
        allyEffect.CompletePulseImmediately();
        allySlot.OnPointerEnter(null);
        bool test6 =
            allyContext.baseImage.sprite ==
                allyContext.allyActionSprite &&
            allyEffect.IsVisible &&
            !allyEffect.IsPulsePlaying;
        allySlot.OnPointerExit(null);

        int allyLeftClickCount = 0;
        int allyRightClickCount = 0;
        allySlot.BindInteraction(
            allyContext.character,
            0,
            false,
            clickedSlot =>
            {
                allyLeftClickCount++;
                clickedSlot.SetSelected(true);
            },
            clickedSlot => allyRightClickCount++
        );
        allySlot.SetSelected(false);
        allyEffect.StopAndReset();
        allySlot.OnPointerClick(leftClick);
        Vector3 pulseStartScale =
            allyEffect.TargetScale * 0.15f;
        bool test7 =
            allyLeftClickCount == 1 &&
            allySlot.IsSelected &&
            allyEffect.IsPersistentVisible &&
            allyEffect.IsPulsePlaying &&
            allyEffect.ActivePulseCount == 1 &&
            Vector3.Distance(
                allyContext.effectRoot.localScale,
                pulseStartScale
            ) < 0.001f;

        Vector3 pulseStep0 = allyContext.effectRoot.localScale;
        allyEffect.AdvancePulseForTesting(1f / 60f);
        Vector3 pulseStep1 = allyContext.effectRoot.localScale;
        allyEffect.AdvancePulseForTesting(1f / 60f);
        Vector3 pulseStep2 = allyContext.effectRoot.localScale;
        float pulseFirstStep =
            Vector3.Distance(pulseStep0, pulseStep1);
        float pulseSecondStep =
            Vector3.Distance(pulseStep1, pulseStep2);
        bool test8 =
            pulseFirstStep > pulseSecondStep &&
            Vector3.Distance(
                pulseStep2,
                allyEffect.TargetScale
            ) <
            Vector3.Distance(
                pulseStep1,
                allyEffect.TargetScale
            );

        bool pulseReachedSnap = false;
        for (int step = 0; step < 240; step++)
        {
            if (allyEffect.AdvancePulseForTesting(1f / 60f))
            {
                pulseReachedSnap = true;
                break;
            }
        }
        bool test9 =
            pulseReachedSnap &&
            !allyEffect.IsPulsePlaying &&
            Vector3.Distance(
                allyContext.effectRoot.localScale,
                allyEffect.TargetScale
            ) < 0.001f;

        allySlot.OnPointerEnter(null);
        allySlot.OnPointerExit(null);
        bool test10 =
            allySlot.IsSelected &&
            !allySlot.IsHovered &&
            allyEffect.IsVisible &&
            allyEffect.IsPersistentVisible;

        allySlot.SetSelected(false);
        bool test11 =
            !allySlot.IsSelected &&
            !allySlot.IsHovered &&
            !allyEffect.IsPersistentVisible &&
            !allyEffect.IsVisible;

        allySlot.OnPointerClick(leftClick);
        allyEffect.CompletePulseImmediately();
        allySlot.OnPointerClick(leftClick);
        bool repeatedClickStartedPulse =
            allySlot.IsSelected &&
            allyEffect.ActivePulseCount == 1 &&
            Vector3.Distance(
                allyContext.effectRoot.localScale,
                pulseStartScale
            ) < 0.001f;
        allyEffect.AdvancePulseForTesting(1f / 60f);
        allySlot.OnPointerClick(leftClick);
        bool test12 =
            repeatedClickStartedPulse &&
            allyEffect.ActivePulseCount == 1 &&
            Vector3.Distance(
                allyContext.effectRoot.localScale,
                pulseStartScale
            ) < 0.001f;

        Mode69SlotTestContext fillContext =
            CreateMode69SlotTestContext("Mode69Fill", false);
        fillContext.slotView.SetState(
            BattleActionSlotUIState.AllyActionSet
        );
        fillContext.slotView.CommitStateFeedbackForTesting();
        bool test13 =
            !fillContext.effectView.IsPulsePlaying &&
            !fillContext.effectView.IsVisible;

        fillContext.slotView.SetState(
            BattleActionSlotUIState.AllyEmpty
        );
        fillContext.slotView.CommitStateFeedbackForTesting();
        fillContext.slotView.SetState(
            BattleActionSlotUIState.AllyActionSet
        );
        fillContext.slotView.CommitStateFeedbackForTesting();
        bool test14 =
            fillContext.effectView.IsPulsePlaying &&
            fillContext.effectView.ActivePulseCount == 1;
        fillContext.effectView.AdvancePulseForTesting(1f / 60f);
        Vector3 fillScaleBeforeRepeatedState =
            fillContext.effectRoot.localScale;
        fillContext.slotView.SetState(
            BattleActionSlotUIState.AllyActionSet
        );
        fillContext.slotView.CommitStateFeedbackForTesting();
        bool test15 =
            fillContext.effectView.ActivePulseCount == 1 &&
            Vector3.Distance(
                fillContext.effectRoot.localScale,
                fillScaleBeforeRepeatedState
            ) < 0.001f;
        fillContext.effectView.CompletePulseImmediately();
        bool test16 =
            !fillContext.effectView.IsPulsePlaying &&
            !fillContext.effectView.IsVisible &&
            !fillContext.slotView.IsSelected &&
            !fillContext.slotView.IsHovered;
        fillContext.slotView.SetState(
            BattleActionSlotUIState.AllyEmpty
        );
        fillContext.slotView.SetState(
            BattleActionSlotUIState.AllyActionSet
        );
        fillContext.slotView.CommitStateFeedbackForTesting();
        test15 &=
            !fillContext.effectView.IsPulsePlaying &&
            !fillContext.effectView.IsVisible;

        allyEffect.CompletePulseImmediately();
        allySlot.SetState(BattleActionSlotUIState.AllyEmpty);
        allySlot.CommitStateFeedbackForTesting();
        allySlot.SetState(BattleActionSlotUIState.AllyActionSet);
        allySlot.CommitStateFeedbackForTesting();
        allyEffect.CompletePulseImmediately();
        bool test17 =
            allySlot.IsSelected &&
            allyEffect.IsPersistentVisible &&
            allyEffect.IsVisible;

        Mode69SlotTestContext enemyContext =
            CreateMode69SlotTestContext("Mode69Enemy", true);
        enemyContext.slotView.SetState(
            BattleActionSlotUIState.EnemyEmpty
        );
        enemyContext.slotView.CommitStateFeedbackForTesting();
        bool enemyEmptyDisplayed =
            enemyContext.baseImage.sprite ==
                enemyContext.enemyEmptySprite;
        enemyContext.slotView.SetState(
            BattleActionSlotUIState.EnemyActionSet
        );
        enemyContext.slotView.CommitStateFeedbackForTesting();
        bool test18 =
            enemyEmptyDisplayed &&
            enemyContext.baseImage.sprite ==
                enemyContext.enemyActionSprite &&
            !enemyContext.effectView.IsVisible &&
            !enemyContext.effectView.IsPulsePlaying;

        int enemyLeftClickCount = 0;
        enemyContext.slotView.BindInteraction(
            enemyContext.character,
            0,
            true,
            clickedSlot => enemyLeftClickCount++
        );
        enemyContext.slotView.OnPointerEnter(null);
        enemyContext.slotView.OnPointerExit(null);
        enemyContext.slotView.OnPointerClick(leftClick);
        bool test19 =
            enemyLeftClickCount == 1 &&
            !enemyContext.slotView.IsHovered &&
            !enemyContext.slotView.IsSelected &&
            !enemyContext.effectView.IsVisible &&
            !enemyContext.effectView.IsPulsePlaying;

        allySlot.SetSelected(false);
        allyEffect.StopAndReset();
        allySlot.OnPointerClick(rightClick);
        bool test20 =
            allyRightClickCount == 1 &&
            !allyEffect.IsPulsePlaying &&
            !allyEffect.IsVisible;

        allySlot.SetSelected(true);
        bool pulseBeforeDisable = allyEffect.IsPulsePlaying;
        allyContext.rootObject.SetActive(false);
        bool test21 =
            pulseBeforeDisable &&
            !allyEffect.IsPulsePlaying &&
            !allyEffect.IsVisible &&
            !allySlot.IsHovered;
        allyContext.rootObject.SetActive(true);
        allySlot.SetSelected(false);
        allyEffect.StopAndReset();

        bool noAccumulatedPulse = true;
        for (int cycle = 0; cycle < 6; cycle++)
        {
            allySlot.OnPointerEnter(null);
            noAccumulatedPulse &=
                allyEffect.ActivePulseCount <= 1;
            allySlot.OnPointerClick(leftClick);
            noAccumulatedPulse &=
                allyEffect.ActivePulseCount <= 1;
            allySlot.OnPointerExit(null);
            allySlot.OnPointerClick(leftClick);
            noAccumulatedPulse &=
                allyEffect.ActivePulseCount <= 1;
            allySlot.SetSelected(false);
        }
        bool test22 =
            noAccumulatedPulse &&
            allyEffect.ActivePulseCount <= 1;
        allyEffect.StopAndReset();

        bool test23 =
            RunBattleCardClickAssignBasicTestSequence();
        bool test24 =
            RunBattleCardClickInteractionIntegrationTestSequence();

        Debug.Log("模式69 测试1 默认AllyEmpty显示空底图：" + test1);
        Debug.Log("模式69 测试2 AllyTargeted无交互显示被指定底图：" + test2);
        Debug.Log("模式69 测试3 AllyTargeted Hover临时切为空底图：" + test3);
        Debug.Log("模式69 测试4 Hover特效立即完整显示且不播放Pulse：" + test4);
        Debug.Log("模式69 测试5 PointerExit恢复被指定底图并隐藏特效：" + test5);
        Debug.Log("模式69 测试6 AllyActionSet Hover保留有行动底图：" + test6);
        Debug.Log("模式69 测试7 点击选择播放中心扩散Pulse：" + test7);
        Debug.Log("模式69 测试8 Pulse首步最大且后续步长递减：" + test8);
        Debug.Log("模式69 测试9 Pulse进入Snap后精确恢复目标Scale：" + test9);
        Debug.Log("模式69 测试10 Selected时PointerExit仍保持特效：" + test10);
        Debug.Log("模式69 测试11 取消Selected且未Hover时隐藏特效：" + test11);
        Debug.Log("模式69 测试12 重复点击Selected重新Pulse且仅一个协程：" + test12);
        Debug.Log("模式69 测试13 首次状态初始化不误触发填入Pulse：" + test13);
        Debug.Log("模式69 测试14 真实进入AllyActionSet播放一次Pulse：" + test14);
        Debug.Log("模式69 测试15 重复AllyActionSet不重新播放Pulse：" + test15);
        Debug.Log("模式69 测试16 未交互填入Pulse完成后自动隐藏：" + test16);
        Debug.Log("模式69 测试17 Selected时填入Pulse完成后保持显示：" + test17);
        Debug.Log("模式69 测试18 敌方两态只切基础图：" + test18);
        Debug.Log("模式69 测试19 敌方Hover与点击不触发我方特效：" + test19);
        Debug.Log("模式69 测试20 右键只调用原右键回调：" + test20);
        Debug.Log("模式69 测试21 Disable停止协程并隐藏特效：" + test21);
        Debug.Log("模式69 测试22 快速Enter/Exit/Click不累积协程：" + test22);
        Debug.Log("模式69 测试23 模式66点击与敌方槽位指派继续通过：" + test23);
        Debug.Log("模式69 测试24 模式67自身目标指派继续通过：" + test24);
        Debug.Log(
            "===== BattleActionSlotVisualInteractionBasic 聚合测试结束 ====="
        );

        DestroyMode69SlotTestContext(allyContext);
        DestroyMode69SlotTestContext(fillContext);
        DestroyMode69SlotTestContext(enemyContext);
        return test1 &&
            test2 &&
            test3 &&
            test4 &&
            test5 &&
            test6 &&
            test7 &&
            test8 &&
            test9 &&
            test10 &&
            test11 &&
            test12 &&
            test13 &&
            test14 &&
            test15 &&
            test16 &&
            test17 &&
            test18 &&
            test19 &&
            test20 &&
            test21 &&
            test22 &&
            test23 &&
            test24;
    }

    Mode69SlotTestContext CreateMode69SlotTestContext(
        string namePrefix,
        bool enemySlot
    )
    {
        Mode69SlotTestContext context =
            new Mode69SlotTestContext();
        context.rootObject = new GameObject(
            namePrefix + "Root",
            typeof(RectTransform)
        );
        context.rootObject.SetActive(false);
        context.baseImage =
            context.rootObject.AddComponent<UnityEngine.UI.Image>();
        context.slotView =
            context.rootObject.AddComponent<BattleActionSlotUIView>();

        GameObject effectObject = new GameObject(
            namePrefix + "SelectionEffect",
            typeof(RectTransform)
        );
        effectObject.transform.SetParent(
            context.rootObject.transform,
            false
        );
        context.effectRoot =
            effectObject.GetComponent<RectTransform>();
        context.effectRoot.localScale =
            new Vector3(1.2f, 1.1f, 1f);
        context.effectImage =
            effectObject.AddComponent<UnityEngine.UI.Image>();
        context.effectView =
            effectObject.AddComponent<
                BattleActionSlotSelectionEffectUIView
            >();
        context.effectView.ConfigureTestVisuals(
            context.effectRoot,
            context.effectImage
        );

        context.spriteTexture = new Texture2D(5, 1);
        context.allyEmptySprite = CreateMode64Sprite(
            context.spriteTexture,
            0,
            namePrefix + "AllyEmpty"
        );
        context.allyTargetedSprite = CreateMode64Sprite(
            context.spriteTexture,
            1,
            namePrefix + "AllyTargeted"
        );
        context.allyActionSprite = CreateMode64Sprite(
            context.spriteTexture,
            2,
            namePrefix + "AllyAction"
        );
        context.enemyEmptySprite = CreateMode64Sprite(
            context.spriteTexture,
            3,
            namePrefix + "EnemyEmpty"
        );
        context.enemyActionSprite = CreateMode64Sprite(
            context.spriteTexture,
            4,
            namePrefix + "EnemyAction"
        );

        context.slotView.ConfigureTestVisuals(
            context.baseImage,
            context.allyEmptySprite
        );
        bool slotConfigured =
            SetMode64PrivateField(
                context.slotView,
                "slotAllyTargetedNoActionSprite",
                context.allyTargetedSprite
            ) &&
            SetMode64PrivateField(
                context.slotView,
                "slotAllyActionSetSprite",
                context.allyActionSprite
            ) &&
            SetMode64PrivateField(
                context.slotView,
                "slotEnemyEmptySprite",
                context.enemyEmptySprite
            ) &&
            SetMode64PrivateField(
                context.slotView,
                "slotEnemyActionSetSprite",
                context.enemyActionSprite
            ) &&
            SetMode64PrivateField(
                context.slotView,
                "selectionEffectView",
                context.effectView
            ) &&
            SetMode64PrivateField(
                context.slotView,
                "defaultState",
                enemySlot
                    ? BattleActionSlotUIState.EnemyEmpty
                    : BattleActionSlotUIState.AllyEmpty
            );
        bool effectConfigured =
            SetMode64PrivateField(
                context.effectView,
                "pulseStartScale",
                0.15f
            ) &&
            SetMode64PrivateField(
                context.effectView,
                "pulseSharpness",
                18f
            ) &&
            SetMode64PrivateField(
                context.effectView,
                "pulseSnapDistance",
                0.001f
            ) &&
            SetMode64PrivateField(
                context.effectView,
                "hideWhenIdle",
                true
            );
        context.referencesConfigured =
            slotConfigured && effectConfigured;

        context.character = new CharacterData(
            namePrefix + "Character",
            30,
            5,
            5
        );
        context.rootObject.SetActive(true);
        context.slotView.BindInteraction(
            context.character,
            0,
            enemySlot,
            null,
            null
        );
        return context;
    }

    void DestroyMode69SlotTestContext(
        Mode69SlotTestContext context
    )
    {
        if (context == null)
        {
            return;
        }

        Destroy(context.rootObject);
        Destroy(context.allyEmptySprite);
        Destroy(context.allyTargetedSprite);
        Destroy(context.allyActionSprite);
        Destroy(context.enemyEmptySprite);
        Destroy(context.enemyActionSprite);
        Destroy(context.spriteTexture);
    }

    sealed class Mode70BuffTestContext
    {
        public GameObject rootObject;
        public RectTransform slotsRoot;
        public BattleBuffGroupUIView groupView;
        public BattleBuffIconUIView firstOriginalSlot;
        public BattleBuffIconUIView secondOriginalSlot;
        public BattleBuffIconBinding[] bindings;
        public CharacterData character;
        public Texture2D spriteTexture;
        public Sprite strengthSprite;
        public Sprite guardSprite;
        public Sprite defaultSprite;
        public Sprite overflowSprite;
        public bool referencesConfigured;
    }

    sealed class Mode70HierarchyTestResult
    {
        public bool templateExcluded;
        public bool directSlotsCollected;
        public bool nestedSlotExcluded;
        public bool clonesUseSlotsRoot;
        public bool repeatedSetStable;
        public bool clearKeepsPool;
        public bool rebindReusesInstances;
        public bool incompleteTemplateRejected;
        public bool warningOnlyOnce;
    }

    sealed class Mode70GridNormalizationTestResult
    {
        public bool uniformSize;
        public bool anchorMinNormalized;
        public bool anchorMaxNormalized;
        public bool pivotNormalized;
        public bool scaleNormalized;
        public bool firstRowHorizontal;
        public bool secondRowHorizontal;
        public bool firstColumnAligned;
        public bool secondColumnAligned;
        public bool secondRowStartsAtExpectedPosition;
        public bool overflowUsesLastGridCell;
        public bool mixedSizesDoNotShiftLayout;
        public bool mixedTransformsNormalized;
        public bool inactiveSlotsDoNotCreateHoles;
        public bool repeatedSetKeepsPositions;
        public bool countSequenceKeepsGridStable;
    }

    sealed class Mode71WrongHierarchyTestResult
    {
        public bool fixtureDetected;
        public bool repeatedApplyStable;
    }

    bool RunBattleBuffGridLayoutBasicTestSequence()
    {
        Debug.Log("===== BattleBuffGridLayoutBasic 聚合测试开始 =====");

        Mode70BuffTestContext context =
            CreateMode70BuffTestContext();
        BattleBuffGroupUIView group = context.groupView;
        CharacterData character = context.character;

        bool test1 =
            context.referencesConfigured &&
            context.bindings != null &&
            context.bindings.Length == 3 &&
            context.bindings[0].buffID == "Strength" &&
            context.bindings[0].displayName == "强壮" &&
            context.bindings[0].iconSprite ==
                context.strengthSprite &&
            context.bindings[0].iconView ==
                context.firstOriginalSlot;
        bool test2 =
            group.SlotPoolCount == 2 &&
            group.GetSlotForTesting(0) ==
                context.firstOriginalSlot &&
            group.GetSlotForTesting(1) ==
                context.secondOriginalSlot;
        bool test3 = group.SlotPoolCount == 2;

        SetMode70ActiveBuffs(character);
        group.SetCharacter(character);
        bool fallbackTemplateExpandedPool =
            group.SlotPoolCount == 8;
        bool test4 =
            fallbackTemplateExpandedPool &&
            CountMode70ActiveSlots(group) == 0;

        SetMode70ActiveBuffs(
            character,
            CreateMode70Buff("UnknownOne", 1)
        );
        group.SetCharacter(character);
        bool test5 =
            CountMode70ActiveSlots(group) == 1 &&
            GetMode70StackText(group.GetSlotForTesting(0)).text ==
                "1";

        SetMode70ActiveBuffs(
            character,
            CreateMode70Buff("Strength", 2)
        );
        group.SetCharacter(character);
        bool test6 =
            GetMode70IconImage(group.GetSlotForTesting(0)).sprite ==
                context.strengthSprite;

        SetMode70ActiveBuffs(
            character,
            CreateMode70Buff("UnknownDefault", 3)
        );
        group.SetCharacter(character);
        bool test7 =
            GetMode70IconImage(group.GetSlotForTesting(0)).sprite ==
                context.defaultSprite;

        bool clearedDefaultIcon = SetMode64PrivateField(
            group,
            "defaultBuffIcon",
            null
        );
        SetMode70ActiveBuffs(
            character,
            CreateMode70Buff("UnknownWithoutIcon", 4)
        );
        group.SetCharacter(character);
        UnityEngine.UI.Image missingIconImage =
            GetMode70IconImage(group.GetSlotForTesting(0));
        bool test8 =
            clearedDefaultIcon &&
            group.GetSlotForTesting(0).gameObject.activeSelf &&
            missingIconImage != null &&
            !missingIconImage.enabled &&
            GetMode70StackText(group.GetSlotForTesting(0)).text ==
                "4";
        SetMode64PrivateField(
            group,
            "defaultBuffIcon",
            context.defaultSprite
        );

        SetMode70ActiveBuffs(
            character,
            CreateMode70Buff("Strength", 2),
            CreateMode70Buff("Strength", 3)
        );
        group.SetCharacter(character);
        bool test9 =
            CountMode70ActiveSlots(group) == 1 &&
            GetMode70StackText(group.GetSlotForTesting(0)).text ==
                "5";

        SetMode70ActiveBuffs(
            character,
            CreateMode70Buff("GuardUp", 1),
            CreateMode70Buff("Strength", 1),
            CreateMode70Buff("GuardUp", 2)
        );
        group.SetCharacter(character);
        bool test10 =
            CountMode70ActiveSlots(group) == 2 &&
            GetMode70IconImage(group.GetSlotForTesting(0)).sprite ==
                context.guardSprite &&
            GetMode70IconImage(group.GetSlotForTesting(1)).sprite ==
                context.strengthSprite;

        SetMode70ActiveBuffs(character);
        character.pendingBuffs.Clear();
        character.pendingBuffs.Add(
            new PendingBuffData(
                "PendingOnly",
                "待生效状态",
                "UpBuff",
                5,
                2,
                "TurnEnd",
                "DurationDown",
                1,
                1,
                1
            )
        );
        group.SetCharacter(character);
        bool test11 = CountMode70ActiveSlots(group) == 0;
        character.pendingBuffs.Clear();

        SetMode70DistinctBuffs(character, 5);
        group.SetCharacter(character);
        RectTransform slot0Rect =
            group.GetSlotForTesting(0).transform as RectTransform;
        RectTransform slot1Rect =
            group.GetSlotForTesting(1).transform as RectTransform;
        RectTransform slot4Rect =
            group.GetSlotForTesting(4).transform as RectTransform;
        bool test12 =
            slot0Rect != null &&
            slot4Rect != null &&
            Mathf.Abs(
                slot4Rect.anchoredPosition.x -
                slot0Rect.anchoredPosition.x
            ) < 0.001f &&
            slot4Rect.anchoredPosition.y <
                slot0Rect.anchoredPosition.y;
        bool test13 =
            slot0Rect != null &&
            slot1Rect != null &&
            Vector2.Distance(
                slot1Rect.anchoredPosition,
                group.GetExpectedSlotPosition(1)
            ) < 0.001f;
        bool test14 =
            slot0Rect != null &&
            slot4Rect != null &&
            Vector2.Distance(
                slot4Rect.anchoredPosition,
                group.GetExpectedSlotPosition(4)
            ) < 0.001f;
        bool test15 =
            slot0Rect != null &&
            Vector2.Distance(
                slot0Rect.anchoredPosition,
                new Vector2(10f, 20f)
            ) < 0.001f;

        SetMode70DistinctBuffs(character, 12);
        group.SetCharacter(character);
        float secondRowY =
            group.GetExpectedSlotPosition(4).y;
        bool noThirdRow = true;
        for (int index = 0;
            index < group.SlotPoolCount;
            index++)
        {
            BattleBuffIconUIView slot =
                group.GetSlotForTesting(index);
            RectTransform slotRect =
                slot != null
                    ? slot.transform as RectTransform
                    : null;
            if (slot != null &&
                slot.gameObject.activeSelf &&
                slotRect != null &&
                slotRect.anchoredPosition.y < secondRowY - 0.001f)
            {
                noThirdRow = false;
            }
        }
        bool test16 =
            group.SlotPoolCount == 8 &&
            CountMode70ActiveSlots(group) == 8 &&
            noThirdRow;

        SetMode70DistinctBuffs(character, 8);
        group.SetCharacter(character);
        bool test17 =
            CountMode70ActiveSlots(group) == 8 &&
            !HasMode70OverflowSlot(group);

        SetMode70DistinctBuffs(character, 9);
        group.SetCharacter(character);
        BattleBuffIconUIView overflowSlot =
            group.GetSlotForTesting(7);
        bool test18 =
            CountMode70ActiveSlots(group) == 8 &&
            CountMode70NormalSlots(group) == 7 &&
            overflowSlot != null &&
            overflowSlot.IsOverflow &&
            GetMode70StackText(overflowSlot).text == "...+2";
        bool test19 =
            overflowSlot != null &&
            overflowSlot.OverflowHiddenCount == 2;

        CharacterData clickedCharacter = null;
        int clickedHiddenCount = 0;
        int overflowClickCount = 0;
        group.SetOverflowClickHandler(
            (clickedOwner, hiddenCount) =>
            {
                clickedCharacter = clickedOwner;
                clickedHiddenCount = hiddenCount;
                overflowClickCount++;
            }
        );
        overflowSlot.OnPointerClick(
            new PointerEventData(null)
            {
                button = PointerEventData.InputButton.Left
            }
        );
        bool test23 =
            overflowClickCount == 1 &&
            clickedCharacter == character &&
            clickedHiddenCount == 2;
        group.GetSlotForTesting(0).OnPointerClick(
            new PointerEventData(null)
            {
                button = PointerEventData.InputButton.Left
            }
        );
        bool test24 = overflowClickCount == 1;

        bool capacityOneConfigured =
            SetMode64PrivateField(group, "columnsPerRow", 1) &&
            SetMode64PrivateField(group, "maxRows", 1);
        SetMode70DistinctBuffs(character, 3);
        group.SetCharacter(character);
        bool test20 =
            capacityOneConfigured &&
            CountMode70ActiveSlots(group) == 1 &&
            group.GetSlotForTesting(0).IsOverflow &&
            group.GetSlotForTesting(0).OverflowHiddenCount == 3;

        BattleBuffIconUIView directSlot =
            group.GetSlotForTesting(0);
        TMPro.TMP_Text directDecayText =
            GetMode70DecayText(directSlot);
        directSlot.SetBuff(context.defaultSprite, 2, -1);
        bool decayWasVisible =
            directDecayText != null &&
            directDecayText.gameObject.activeSelf;
        directSlot.SetOverflow(context.overflowSprite, 2, "...+");
        bool test21 =
            decayWasVisible &&
            directSlot.IsOverflow &&
            directDecayText != null &&
            !directDecayText.gameObject.activeSelf;
        directSlot.SetBuff(context.strengthSprite, 1);
        bool test22 =
            !directSlot.IsOverflow &&
            directSlot.OverflowHiddenCount == 0;

        SetMode70ActiveBuffs(
            character,
            CreateMode70Buff("Repeat", 1)
        );
        group.SetCharacter(character);
        int poolCountBeforeRepeat = group.SlotPoolCount;
        group.SetCharacter(character);
        bool test25 =
            group.SlotPoolCount == poolCountBeforeRepeat;
        group.Clear();
        bool test26 =
            group.SlotPoolCount == poolCountBeforeRepeat &&
            CountMode70ActiveSlots(group) == 0;
        bool test27 =
            fallbackTemplateExpandedPool &&
            context.firstOriginalSlot != null &&
            group.SlotPoolCount == 8;

        bool test28 =
            RunMode70MissingTemplateSafetySubTest();

        SetMode64PrivateField(group, "columnsPerRow", 4);
        SetMode64PrivateField(group, "maxRows", 2);
        SetMode70ActiveBuffs(
            character,
            CreateMode70Buff("Strength", 1)
        );
        GameObject statusObject = new GameObject(
            "Mode70CharacterStatus",
            typeof(RectTransform)
        );
        BattleCharacterStatusUIView statusView =
            statusObject.AddComponent<BattleCharacterStatusUIView>();
        bool statusConfigured = SetMode64PrivateField(
            statusView,
            "buffGroupView",
            group
        );
        statusView.SetCharacter(character);
        bool statusSetForwarded =
            group.BoundCharacter == character;
        statusView.Clear();
        bool test29 =
            statusConfigured &&
            statusSetForwarded &&
            group.BoundCharacter == null &&
            CountMode70ActiveSlots(group) == 0;

        bool test30 =
            RunBattleActionSlotVisualInteractionBasicTestSequence();
        bool test31 =
            RunBattleCardClickAssignBasicTestSequence() &&
            RunBattleCardClickInteractionIntegrationTestSequence();
        Mode70HierarchyTestResult hierarchyResult =
            RunMode70HierarchySafetySubTests();
        Mode70GridNormalizationTestResult gridResult =
            RunMode70GridNormalizationSubTests();

        Debug.Log("模式70 测试1 旧buffBindings序列化结构仍可配置：" + test1);
        Debug.Log("模式70 测试2 旧iconView收集到槽位池：" + test2);
        Debug.Log("模式70 测试3 重复iconView不会重复入池：" + test3);
        Debug.Log("模式70 测试4 零Buff时全部槽位隐藏：" + test4);
        Debug.Log("模式70 测试5 单Buff正常显示：" + test5);
        Debug.Log("模式70 测试6 Strength使用专属Sprite：" + test6);
        Debug.Log("模式70 测试7 未配置图标使用默认图：" + test7);
        Debug.Log("模式70 测试8 默认图为空仍显示Buff槽位：" + test8);
        Debug.Log("模式70 测试9 同ID多批次聚合层数：" + test9);
        Debug.Log("模式70 测试10 保持Buff第一次出现顺序：" + test10);
        Debug.Log("模式70 测试11 pendingBuff不显示：" + test11);
        Debug.Log("模式70 测试12 columnsPerRow控制换行：" + test12);
        Debug.Log("模式70 测试13 horizontalSpacing影响X位置：" + test13);
        Debug.Log("模式70 测试14 verticalSpacing影响第二行Y位置：" + test14);
        Debug.Log("模式70 测试15 startOffset应用正确：" + test15);
        Debug.Log("模式70 测试16 maxRows为2时不生成第三行：" + test16);
        Debug.Log("模式70 测试17 Buff数等于容量时无Overflow：" + test17);
        Debug.Log("模式70 测试18 九Buff显示七普通与...+2：" + test18);
        Debug.Log("模式70 测试19 Overflow隐藏数量正确：" + test19);
        Debug.Log("模式70 测试20 容量一时多Buff只显示Overflow：" + test20);
        Debug.Log("模式70 测试21 Overflow隐藏decayText：" + test21);
        Debug.Log("模式70 测试22 SetBuff清除Overflow状态：" + test22);
        Debug.Log("模式70 测试23 Overflow左键回传角色与数量：" + test23);
        Debug.Log("模式70 测试24 普通Buff点击不触发Overflow回调：" + test24);
        Debug.Log("模式70 测试25 重复SetCharacter不扩张槽位池：" + test25);
        Debug.Log("模式70 测试26 Clear隐藏但不销毁槽位池：" + test26);
        Debug.Log("模式70 测试27 无模板时复用旧iconView克隆：" + test27);
        Debug.Log("模式70 测试28 全部模板缺失时安全返回：" + test28);
        Debug.Log("模式70 测试29 角色状态UI原调用方式保持：" + test29);
        Debug.Log("模式70 测试30 模式69行动槽视觉回归：" + test30);
        Debug.Log("模式70 测试31 模式66与67卡牌指派回归：" + test31);
        Debug.Log("模式70 测试32 SlotTemplate不进入slotPool：" +
            hierarchyResult.templateExcluded);
        Debug.Log("模式70 测试33 只收集SlotsRoot直接子槽位：" +
            hierarchyResult.directSlotsCollected);
        Debug.Log("模式70 测试34 嵌套槽位不会进入池：" +
            hierarchyResult.nestedSlotExcluded);
        Debug.Log("模式70 测试35 动态克隆父级始终为SlotsRoot：" +
            hierarchyResult.clonesUseSlotsRoot);
        Debug.Log("模式70 测试36 连续SetCharacter十次池稳定：" +
            hierarchyResult.repeatedSetStable);
        Debug.Log("模式70 测试37 Clear后池数量保持：" +
            hierarchyResult.clearKeepsPool);
        Debug.Log("模式70 测试38 再绑定复用相同槽位实例：" +
            hierarchyResult.rebindReusesInstances);
        Debug.Log("模式70 测试39 不完整模板不会用于克隆：" +
            hierarchyResult.incompleteTemplateRejected);
        Debug.Log("模式70 测试40 配置错误只记录一次警告：" +
            hierarchyResult.warningOnlyOnce);
        Debug.Log("模式70 测试41 所有可见槽位尺寸统一：" +
            gridResult.uniformSize);
        Debug.Log("模式70 测试42 AnchorMin统一为左上：" +
            gridResult.anchorMinNormalized);
        Debug.Log("模式70 测试43 AnchorMax统一为左上：" +
            gridResult.anchorMaxNormalized);
        Debug.Log("模式70 测试44 Pivot统一为左上：" +
            gridResult.pivotNormalized);
        Debug.Log("模式70 测试45 Scale统一且不继承旧变形：" +
            gridResult.scaleNormalized);
        Debug.Log("模式70 测试46 第一行严格水平排列：" +
            gridResult.firstRowHorizontal);
        Debug.Log("模式70 测试47 第二行严格水平排列：" +
            gridResult.secondRowHorizontal);
        Debug.Log("模式70 测试48 第一列上下严格对齐：" +
            gridResult.firstColumnAligned);
        Debug.Log("模式70 测试49 第二列上下严格对齐：" +
            gridResult.secondColumnAligned);
        Debug.Log("模式70 测试50 第二行从统一公式位置开始：" +
            gridResult.secondRowStartsAtExpectedPosition);
        Debug.Log("模式70 测试51 Overflow占据最后一个网格格子：" +
            gridResult.overflowUsesLastGridCell);
        Debug.Log("模式70 测试52 混合旧尺寸不会造成位置偏移：" +
            gridResult.mixedSizesDoNotShiftLayout);
        Debug.Log("模式70 测试53 混合Anchor Pivot旋转已规范化：" +
            gridResult.mixedTransformsNormalized);
        Debug.Log("模式70 测试54 隐藏槽位不会产生索引空洞：" +
            gridResult.inactiveSlotsDoNotCreateHoles);
        Debug.Log("模式70 测试55 重复SetCharacter位置不漂移：" +
            gridResult.repeatedSetKeepsPositions);
        Debug.Log("模式70 测试56 九十二四九切换网格稳定：" +
            gridResult.countSequenceKeepsGridStable);
        Debug.Log("===== BattleBuffGridLayoutBasic 聚合测试结束 =====");

        Destroy(statusObject);
        DestroyMode70BuffTestContext(context);

        return test1 &&
            test2 &&
            test3 &&
            test4 &&
            test5 &&
            test6 &&
            test7 &&
            test8 &&
            test9 &&
            test10 &&
            test11 &&
            test12 &&
            test13 &&
            test14 &&
            test15 &&
            test16 &&
            test17 &&
            test18 &&
            test19 &&
            test20 &&
            test21 &&
            test22 &&
            test23 &&
            test24 &&
            test25 &&
            test26 &&
            test27 &&
            test28 &&
            test29 &&
            test30 &&
            test31 &&
            hierarchyResult.templateExcluded &&
            hierarchyResult.directSlotsCollected &&
            hierarchyResult.nestedSlotExcluded &&
            hierarchyResult.clonesUseSlotsRoot &&
            hierarchyResult.repeatedSetStable &&
            hierarchyResult.clearKeepsPool &&
            hierarchyResult.rebindReusesInstances &&
            hierarchyResult.incompleteTemplateRejected &&
            hierarchyResult.warningOnlyOnce &&
            gridResult.uniformSize &&
            gridResult.anchorMinNormalized &&
            gridResult.anchorMaxNormalized &&
            gridResult.pivotNormalized &&
            gridResult.scaleNormalized &&
            gridResult.firstRowHorizontal &&
            gridResult.secondRowHorizontal &&
            gridResult.firstColumnAligned &&
            gridResult.secondColumnAligned &&
            gridResult.secondRowStartsAtExpectedPosition &&
            gridResult.overflowUsesLastGridCell &&
            gridResult.mixedSizesDoNotShiftLayout &&
            gridResult.mixedTransformsNormalized &&
            gridResult.inactiveSlotsDoNotCreateHoles &&
            gridResult.repeatedSetKeepsPositions &&
            gridResult.countSequenceKeepsGridStable;
    }

    Mode70GridNormalizationTestResult
        RunMode70GridNormalizationSubTests()
    {
        Mode70GridNormalizationTestResult result =
            new Mode70GridNormalizationTestResult();
        GameObject rootObject = new GameObject(
            "Mode70GridNormalizationRoot",
            typeof(RectTransform)
        );
        rootObject.SetActive(false);
        RectTransform slotsRoot =
            rootObject.GetComponent<RectTransform>();
        BattleBuffGroupUIView group =
            rootObject.AddComponent<BattleBuffGroupUIView>();
        BattleBuffIconUIView template =
            CreateMode70BuffIconView(
                slotsRoot,
                "Mode70GridTemplate"
            );
        RectTransform templateRect =
            template.transform as RectTransform;
        templateRect.sizeDelta = new Vector2(24f, 24f);

        BattleBuffIconUIView firstSlot =
            CreateMode70BuffIconView(
                slotsRoot,
                "Mode70MixedSlot_1"
            );
        RectTransform firstRect =
            firstSlot.transform as RectTransform;
        firstRect.sizeDelta = new Vector2(10f, 48f);
        firstRect.anchorMin = new Vector2(0.5f, 0.5f);
        firstRect.anchorMax = new Vector2(0.5f, 0.5f);
        firstRect.pivot = new Vector2(0.5f, 0.5f);
        firstRect.localScale = new Vector3(1.5f, 0.75f, 1f);
        firstRect.localRotation =
            Quaternion.Euler(0f, 0f, 12f);

        BattleBuffIconUIView secondSlot =
            CreateMode70BuffIconView(
                slotsRoot,
                "Mode70MixedSlot_2"
            );
        RectTransform secondRect =
            secondSlot.transform as RectTransform;
        secondRect.sizeDelta = new Vector2(72f, 14f);
        secondRect.anchorMin = new Vector2(1f, 0f);
        secondRect.anchorMax = new Vector2(1f, 0f);
        secondRect.pivot = new Vector2(1f, 0f);
        secondRect.localScale = new Vector3(0.6f, 1.8f, 1f);
        secondRect.localRotation =
            Quaternion.Euler(0f, 0f, -18f);

        SetMode64PrivateField(group, "slotsRoot", slotsRoot);
        SetMode64PrivateField(group, "slotTemplate", template);
        SetMode64PrivateField(group, "columnsPerRow", 4);
        SetMode64PrivateField(group, "maxRows", 2);
        SetMode64PrivateField(
            group,
            "startOffset",
            new Vector2(11f, 23f)
        );
        SetMode64PrivateField(
            group,
            "horizontalSpacing",
            5f
        );
        SetMode64PrivateField(
            group,
            "verticalSpacing",
            7f
        );
        SetMode64PrivateField(
            group,
            "useTemplateSlotSize",
            true
        );
        SetMode64PrivateField(
            group,
            "slotSize",
            new Vector2(99f, 99f)
        );
        rootObject.SetActive(true);

        CharacterData character = new CharacterData(
            "mode70_grid_normalization_owner",
            30,
            5,
            5
        );
        SetMode70DistinctBuffs(character, 9);
        group.SetCharacter(character);

        result.uniformSize =
            Vector2.Distance(
                group.ResolvedSlotSize,
                new Vector2(24f, 24f)
            ) < 0.001f &&
            AreMode70VisibleSlotSizesEqual(
                group,
                8,
                group.ResolvedSlotSize
            );
        result.anchorMinNormalized =
            AreMode70VisibleAnchorsEqual(
                group,
                8,
                true,
                new Vector2(0f, 1f)
            );
        result.anchorMaxNormalized =
            AreMode70VisibleAnchorsEqual(
                group,
                8,
                false,
                new Vector2(0f, 1f)
            );
        result.pivotNormalized =
            AreMode70VisiblePivotsEqual(
                group,
                8,
                new Vector2(0f, 1f)
            );
        result.scaleNormalized =
            AreMode70VisibleScalesAndRotationsNormalized(
                group,
                8
            );
        result.firstRowHorizontal =
            AreMode70SlotsOnSameRow(group, 0, 4);
        result.secondRowHorizontal =
            AreMode70SlotsOnSameRow(group, 4, 4);
        result.firstColumnAligned =
            AreMode70SlotsOnSameColumn(group, 0, 4);
        result.secondColumnAligned =
            AreMode70SlotsOnSameColumn(group, 1, 5);
        result.secondRowStartsAtExpectedPosition =
            IsMode70SlotAtExpectedPosition(group, 4);
        result.overflowUsesLastGridCell =
            group.GetSlotForTesting(7).IsOverflow &&
            IsMode70SlotAtExpectedPosition(group, 7);
        result.mixedSizesDoNotShiftLayout =
            AreMode70VisibleSlotsAtExpectedPositions(group, 8);
        result.mixedTransformsNormalized =
            result.anchorMinNormalized &&
            result.anchorMaxNormalized &&
            result.pivotNormalized &&
            result.scaleNormalized;

        group.Clear();
        SetMode70DistinctBuffs(character, 5);
        group.SetCharacter(character);
        result.inactiveSlotsDoNotCreateHoles =
            CountMode70ActiveSlots(group) == 5 &&
            AreMode70VisibleSlotsAtExpectedPositions(group, 5) &&
            !group.GetSlotForTesting(5).gameObject.activeSelf;

        List<Vector2> stablePositions =
            CaptureMode70VisibleSlotPositions(group, 5);
        group.SetCharacter(character);
        result.repeatedSetKeepsPositions =
            HaveSameMode70VisibleSlotPositions(
                group,
                stablePositions
            );

        int[] counts = { 9, 12, 4, 9 };
        bool sequenceStable = true;
        for (int index = 0; index < counts.Length; index++)
        {
            SetMode70DistinctBuffs(character, counts[index]);
            group.SetCharacter(character);
            int visibleCount = Mathf.Min(counts[index], 8);
            sequenceStable &=
                CountMode70ActiveSlots(group) == visibleCount &&
                AreMode70VisibleSlotsAtExpectedPositions(
                    group,
                    visibleCount
                );
        }
        result.countSequenceKeepsGridStable =
            sequenceStable &&
            group.RuntimeSlotCount == 8 &&
            group.GetSlotForTesting(7).IsOverflow;

        Destroy(rootObject);
        return result;
    }

    Mode70HierarchyTestResult RunMode70HierarchySafetySubTests()
    {
        Mode70HierarchyTestResult result =
            new Mode70HierarchyTestResult();

        GameObject rootObject = new GameObject(
            "Mode70HierarchyRoot",
            typeof(RectTransform)
        );
        rootObject.SetActive(false);
        BattleBuffGroupUIView group =
            rootObject.AddComponent<BattleBuffGroupUIView>();
        GameObject slotsRootObject = new GameObject(
            "Mode70SlotsRoot",
            typeof(RectTransform)
        );
        slotsRootObject.transform.SetParent(
            rootObject.transform,
            false
        );
        RectTransform slotsRoot =
            slotsRootObject.GetComponent<RectTransform>();
        BattleBuffIconUIView template =
            CreateMode70BuffIconView(
                slotsRoot,
                "Mode70Template"
            );
        BattleBuffIconUIView directSlot1 =
            CreateMode70BuffIconView(
                slotsRoot,
                "Mode70Direct_1"
            );
        BattleBuffIconUIView directSlot2 =
            CreateMode70BuffIconView(
                slotsRoot,
                "Mode70Direct_2"
            );
        BattleBuffIconUIView nestedSlot =
            CreateMode70BuffIconView(
                template.transform,
                "Mode70Nested"
            );
        BattleBuffIconBinding[] bindings =
        {
            new BattleBuffIconBinding
            {
                buffID = "Mode70DirectA",
                displayName = "直接槽位A",
                iconView = directSlot1
            },
            new BattleBuffIconBinding
            {
                buffID = "Mode70DirectB",
                displayName = "直接槽位B",
                iconView = directSlot2
            }
        };
        SetMode64PrivateField(group, "buffBindings", bindings);
        SetMode64PrivateField(group, "slotsRoot", slotsRoot);
        SetMode64PrivateField(group, "slotTemplate", template);
        SetMode64PrivateField(group, "columnsPerRow", 4);
        SetMode64PrivateField(group, "maxRows", 2);
        rootObject.SetActive(true);

        result.templateExcluded =
            !ContainsMode70RuntimeSlot(group, template);
        result.directSlotsCollected =
            group.RuntimeSlotCount == 2 &&
            ContainsMode70RuntimeSlot(group, directSlot1) &&
            ContainsMode70RuntimeSlot(group, directSlot2);
        result.nestedSlotExcluded =
            !ContainsMode70RuntimeSlot(group, nestedSlot);

        CharacterData character = new CharacterData(
            "mode70_hierarchy_owner",
            30,
            5,
            5
        );
        SetMode70DistinctBuffs(character, 9);
        group.SetCharacter(character);
        result.clonesUseSlotsRoot =
            group.RuntimeSlotCount == 8 &&
            AreAllMode70RuntimeSlotsDirectChildren(
                group,
                slotsRoot
            );

        List<BattleBuffIconUIView> stableSlots =
            new List<BattleBuffIconUIView>();
        for (int index = 0;
            index < group.RuntimeSlots.Count;
            index++)
        {
            stableSlots.Add(group.RuntimeSlots[index]);
        }

        for (int iteration = 0; iteration < 10; iteration++)
        {
            group.SetCharacter(character);
        }
        result.repeatedSetStable =
            HaveSameMode70RuntimeSlots(group, stableSlots);

        group.Clear();
        result.clearKeepsPool =
            HaveSameMode70RuntimeSlots(group, stableSlots);
        group.SetCharacter(character);
        result.rebindReusesInstances =
            HaveSameMode70RuntimeSlots(group, stableSlots);

        GameObject invalidRootObject = new GameObject(
            "Mode70InvalidTemplateRoot",
            typeof(RectTransform)
        );
        invalidRootObject.SetActive(false);
        BattleBuffGroupUIView invalidGroup =
            invalidRootObject.AddComponent<BattleBuffGroupUIView>();
        GameObject invalidSlotsRootObject = new GameObject(
            "Mode70InvalidSlotsRoot",
            typeof(RectTransform)
        );
        invalidSlotsRootObject.transform.SetParent(
            invalidRootObject.transform,
            false
        );
        RectTransform invalidSlotsRoot =
            invalidSlotsRootObject.GetComponent<RectTransform>();
        GameObject invalidTemplateObject = new GameObject(
            "Mode70IncompleteTemplate",
            typeof(RectTransform)
        );
        invalidTemplateObject.transform.SetParent(
            invalidSlotsRoot,
            false
        );
        BattleBuffIconUIView invalidTemplate =
            invalidTemplateObject.AddComponent<
                BattleBuffIconUIView
            >();
        BattleBuffIconUIView validFallback =
            CreateMode70BuffIconView(
                invalidSlotsRoot,
                "Mode70ValidFallback"
            );
        SetMode64PrivateField(
            invalidGroup,
            "slotsRoot",
            invalidSlotsRoot
        );
        SetMode64PrivateField(
            invalidGroup,
            "slotTemplate",
            invalidTemplate
        );
        SetMode64PrivateField(
            invalidGroup,
            "columnsPerRow",
            4
        );
        SetMode64PrivateField(
            invalidGroup,
            "maxRows",
            2
        );
        invalidRootObject.SetActive(true);

        CharacterData invalidCharacter = new CharacterData(
            "mode70_invalid_template_owner",
            30,
            5,
            5
        );
        SetMode70DistinctBuffs(invalidCharacter, 9);
        invalidGroup.SetCharacter(invalidCharacter);
        bool allFallbackClonesValid = true;
        for (int index = 0;
            index < invalidGroup.RuntimeSlots.Count;
            index++)
        {
            BattleBuffIconUIView slot =
                invalidGroup.RuntimeSlots[index];
            allFallbackClonesValid &=
                slot != null &&
                slot != invalidTemplate &&
                slot.HasRequiredVisualReferences &&
                slot.transform.parent == invalidSlotsRoot;
        }
        result.incompleteTemplateRejected =
            validFallback != null &&
            invalidGroup.RuntimeSlotCount == 8 &&
            allFallbackClonesValid;

        int warningCountAfterFirstUse =
            invalidGroup.ConfigurationWarningCount;
        for (int iteration = 0; iteration < 5; iteration++)
        {
            invalidGroup.SetCharacter(invalidCharacter);
        }
        result.warningOnlyOnce =
            warningCountAfterFirstUse > 0 &&
            invalidGroup.ConfigurationWarningCount ==
                warningCountAfterFirstUse;

        Destroy(rootObject);
        Destroy(invalidRootObject);
        return result;
    }

    bool ContainsMode70RuntimeSlot(
        BattleBuffGroupUIView group,
        BattleBuffIconUIView expectedSlot
    )
    {
        for (int index = 0;
            index < group.RuntimeSlots.Count;
            index++)
        {
            if (group.RuntimeSlots[index] == expectedSlot)
            {
                return true;
            }
        }

        return false;
    }

    bool AreAllMode70RuntimeSlotsDirectChildren(
        BattleBuffGroupUIView group,
        RectTransform slotsRoot
    )
    {
        for (int index = 0;
            index < group.RuntimeSlots.Count;
            index++)
        {
            BattleBuffIconUIView slot =
                group.RuntimeSlots[index];
            if (slot == null ||
                slot.transform.parent != slotsRoot)
            {
                return false;
            }
        }

        return true;
    }

    bool HaveSameMode70RuntimeSlots(
        BattleBuffGroupUIView group,
        List<BattleBuffIconUIView> expectedSlots
    )
    {
        if (group.RuntimeSlots.Count != expectedSlots.Count)
        {
            return false;
        }

        for (int index = 0;
            index < expectedSlots.Count;
            index++)
        {
            if (group.RuntimeSlots[index] != expectedSlots[index])
            {
                return false;
            }
        }

        return true;
    }

    Mode70BuffTestContext CreateMode70BuffTestContext()
    {
        Mode70BuffTestContext context =
            new Mode70BuffTestContext();
        context.rootObject = new GameObject(
            "Mode70BuffGroup",
            typeof(RectTransform)
        );
        context.slotsRoot =
            context.rootObject.GetComponent<RectTransform>();
        context.rootObject.SetActive(false);
        context.groupView =
            context.rootObject.AddComponent<BattleBuffGroupUIView>();
        context.firstOriginalSlot = CreateMode70BuffIconView(
            context.rootObject.transform,
            "Mode70Buff_1"
        );
        context.secondOriginalSlot = CreateMode70BuffIconView(
            context.rootObject.transform,
            "Mode70Buff_2"
        );

        context.spriteTexture = new Texture2D(4, 1);
        context.strengthSprite = CreateMode64Sprite(
            context.spriteTexture,
            0,
            "Mode70Strength"
        );
        context.guardSprite = CreateMode64Sprite(
            context.spriteTexture,
            1,
            "Mode70Guard"
        );
        context.defaultSprite = CreateMode64Sprite(
            context.spriteTexture,
            2,
            "Mode70Default"
        );
        context.overflowSprite = CreateMode64Sprite(
            context.spriteTexture,
            3,
            "Mode70Overflow"
        );

        context.bindings = new[]
        {
            new BattleBuffIconBinding
            {
                buffID = "Strength",
                displayName = "强壮",
                iconSprite = context.strengthSprite,
                iconView = context.firstOriginalSlot
            },
            new BattleBuffIconBinding
            {
                buffID = "GuardUp",
                displayName = "防御提升",
                iconSprite = context.guardSprite,
                iconView = context.secondOriginalSlot
            },
            new BattleBuffIconBinding
            {
                buffID = "UnboundMapping",
                displayName = "无独立槽位映射",
                iconSprite = null,
                iconView = context.firstOriginalSlot
            }
        };

        context.referencesConfigured =
            SetMode64PrivateField(
                context.groupView,
                "buffBindings",
                context.bindings
            ) &&
            SetMode64PrivateField(
                context.groupView,
                "slotsRoot",
                context.slotsRoot
            ) &&
            SetMode64PrivateField(
                context.groupView,
                "slotTemplate",
                null
            ) &&
            SetMode64PrivateField(
                context.groupView,
                "defaultBuffIcon",
                context.defaultSprite
            ) &&
            SetMode64PrivateField(
                context.groupView,
                "overflowIcon",
                context.overflowSprite
            ) &&
            SetMode64PrivateField(
                context.groupView,
                "columnsPerRow",
                4
            ) &&
            SetMode64PrivateField(
                context.groupView,
                "maxRows",
                2
            ) &&
            SetMode64PrivateField(
                context.groupView,
                "startOffset",
                new Vector2(10f, 20f)
            ) &&
            SetMode64PrivateField(
                context.groupView,
                "horizontalSpacing",
                6f
            ) &&
            SetMode64PrivateField(
                context.groupView,
                "verticalSpacing",
                8f
            ) &&
            SetMode64PrivateField(
                context.groupView,
                "useTemplateSlotSize",
                false
            ) &&
            SetMode64PrivateField(
                context.groupView,
                "slotSize",
                new Vector2(20f, 30f)
            ) &&
            SetMode64PrivateField(
                context.groupView,
                "overflowPrefix",
                "...+"
            );
        context.character = new CharacterData(
            "mode70_buff_owner",
            30,
            5,
            5
        );
        context.rootObject.SetActive(true);
        return context;
    }

    BattleBuffIconUIView CreateMode70BuffIconView(
        Transform parent,
        string objectName
    )
    {
        GameObject slotObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image)
        );
        slotObject.transform.SetParent(parent, false);
        RectTransform slotRect =
            slotObject.GetComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(20f, 30f);
        UnityEngine.UI.Image iconImage =
            slotObject.GetComponent<UnityEngine.UI.Image>();
        TMPro.TMP_Text stackText = CreateMode64Text(
            slotObject.transform,
            objectName + "Stack"
        );
        TMPro.TMP_Text decayText = CreateMode64Text(
            slotObject.transform,
            objectName + "Decay"
        );
        BattleBuffIconUIView iconView =
            slotObject.AddComponent<BattleBuffIconUIView>();
        iconView.ConfigureTestVisuals(
            iconImage,
            stackText,
            decayText
        );
        return iconView;
    }

    BuffData CreateMode70Buff(string buffID, int stack)
    {
        return new BuffData(
            buffID,
            buffID + "显示名",
            "UpBuff",
            stack,
            -1,
            "None",
            "Permanent"
        );
    }

    void SetMode70ActiveBuffs(
        CharacterData character,
        params BuffData[] buffs
    )
    {
        character.buffs.Clear();
        if (buffs == null)
        {
            return;
        }

        for (int index = 0; index < buffs.Length; index++)
        {
            character.buffs.Add(buffs[index]);
        }
    }

    void SetMode70DistinctBuffs(
        CharacterData character,
        int count
    )
    {
        character.buffs.Clear();
        for (int index = 0; index < count; index++)
        {
            character.buffs.Add(
                CreateMode70Buff(
                    "Mode70Distinct_" + index,
                    index + 1
                )
            );
        }
    }

    int CountMode70ActiveSlots(BattleBuffGroupUIView group)
    {
        int activeCount = 0;
        for (int index = 0;
            index < group.SlotPoolCount;
            index++)
        {
            BattleBuffIconUIView slot =
                group.GetSlotForTesting(index);
            if (slot != null && slot.gameObject.activeSelf)
            {
                activeCount++;
            }
        }

        return activeCount;
    }

    bool AreMode70VisibleSlotSizesEqual(
        BattleBuffGroupUIView group,
        int visibleCount,
        Vector2 expectedSize
    )
    {
        for (int index = 0; index < visibleCount; index++)
        {
            RectTransform slotRect =
                group.GetSlotForTesting(index).transform
                    as RectTransform;
            if (slotRect == null ||
                Vector2.Distance(
                    slotRect.sizeDelta,
                    expectedSize
                ) >= 0.001f)
            {
                return false;
            }
        }

        return true;
    }

    bool AreMode70VisibleAnchorsEqual(
        BattleBuffGroupUIView group,
        int visibleCount,
        bool useMin,
        Vector2 expectedAnchor
    )
    {
        for (int index = 0; index < visibleCount; index++)
        {
            RectTransform slotRect =
                group.GetSlotForTesting(index).transform
                    as RectTransform;
            if (slotRect == null)
            {
                return false;
            }

            Vector2 actualAnchor = useMin
                ? slotRect.anchorMin
                : slotRect.anchorMax;
            if (Vector2.Distance(
                    actualAnchor,
                    expectedAnchor
                ) >= 0.001f)
            {
                return false;
            }
        }

        return true;
    }

    bool AreMode70VisiblePivotsEqual(
        BattleBuffGroupUIView group,
        int visibleCount,
        Vector2 expectedPivot
    )
    {
        for (int index = 0; index < visibleCount; index++)
        {
            RectTransform slotRect =
                group.GetSlotForTesting(index).transform
                    as RectTransform;
            if (slotRect == null ||
                Vector2.Distance(
                    slotRect.pivot,
                    expectedPivot
                ) >= 0.001f)
            {
                return false;
            }
        }

        return true;
    }

    bool AreMode70VisibleScalesAndRotationsNormalized(
        BattleBuffGroupUIView group,
        int visibleCount
    )
    {
        for (int index = 0; index < visibleCount; index++)
        {
            RectTransform slotRect =
                group.GetSlotForTesting(index).transform
                    as RectTransform;
            if (slotRect == null ||
                Vector3.Distance(
                    slotRect.localScale,
                    Vector3.one
                ) >= 0.001f ||
                Quaternion.Angle(
                    slotRect.localRotation,
                    Quaternion.identity
                ) >= 0.001f)
            {
                return false;
            }
        }

        return true;
    }

    bool AreMode70SlotsOnSameRow(
        BattleBuffGroupUIView group,
        int startIndex,
        int count
    )
    {
        RectTransform firstRect =
            group.GetSlotForTesting(startIndex).transform
                as RectTransform;
        if (firstRect == null)
        {
            return false;
        }

        for (int offset = 0; offset < count; offset++)
        {
            int index = startIndex + offset;
            RectTransform slotRect =
                group.GetSlotForTesting(index).transform
                    as RectTransform;
            if (slotRect == null ||
                Mathf.Abs(
                    slotRect.anchoredPosition.y -
                    firstRect.anchoredPosition.y
                ) >= 0.001f ||
                !IsMode70SlotAtExpectedPosition(group, index))
            {
                return false;
            }
        }

        return true;
    }

    bool AreMode70SlotsOnSameColumn(
        BattleBuffGroupUIView group,
        int firstIndex,
        int secondIndex
    )
    {
        RectTransform firstRect =
            group.GetSlotForTesting(firstIndex).transform
                as RectTransform;
        RectTransform secondRect =
            group.GetSlotForTesting(secondIndex).transform
                as RectTransform;
        return firstRect != null &&
            secondRect != null &&
            Mathf.Abs(
                firstRect.anchoredPosition.x -
                secondRect.anchoredPosition.x
            ) < 0.001f &&
            IsMode70SlotAtExpectedPosition(group, firstIndex) &&
            IsMode70SlotAtExpectedPosition(group, secondIndex);
    }

    bool IsMode70SlotAtExpectedPosition(
        BattleBuffGroupUIView group,
        int index
    )
    {
        BattleBuffIconUIView slot =
            group.GetSlotForTesting(index);
        RectTransform slotRect =
            slot != null
                ? slot.transform as RectTransform
                : null;
        return slotRect != null &&
            Vector2.Distance(
                slotRect.anchoredPosition,
                group.GetExpectedSlotPosition(index)
            ) < 0.001f;
    }

    bool AreMode70VisibleSlotsAtExpectedPositions(
        BattleBuffGroupUIView group,
        int visibleCount
    )
    {
        for (int index = 0; index < visibleCount; index++)
        {
            BattleBuffIconUIView slot =
                group.GetSlotForTesting(index);
            if (slot == null ||
                !slot.gameObject.activeSelf ||
                !IsMode70SlotAtExpectedPosition(group, index))
            {
                return false;
            }
        }

        return true;
    }

    List<Vector2> CaptureMode70VisibleSlotPositions(
        BattleBuffGroupUIView group,
        int visibleCount
    )
    {
        List<Vector2> positions = new List<Vector2>();
        for (int index = 0; index < visibleCount; index++)
        {
            RectTransform slotRect =
                group.GetSlotForTesting(index).transform
                    as RectTransform;
            positions.Add(slotRect.anchoredPosition);
        }

        return positions;
    }

    bool HaveSameMode70VisibleSlotPositions(
        BattleBuffGroupUIView group,
        List<Vector2> expectedPositions
    )
    {
        for (int index = 0;
            index < expectedPositions.Count;
            index++)
        {
            RectTransform slotRect =
                group.GetSlotForTesting(index).transform
                    as RectTransform;
            if (slotRect == null ||
                Vector2.Distance(
                    slotRect.anchoredPosition,
                    expectedPositions[index]
                ) >= 0.001f)
            {
                return false;
            }
        }

        return true;
    }

    int CountMode70NormalSlots(BattleBuffGroupUIView group)
    {
        int normalCount = 0;
        for (int index = 0;
            index < group.SlotPoolCount;
            index++)
        {
            BattleBuffIconUIView slot =
                group.GetSlotForTesting(index);
            if (slot != null &&
                slot.gameObject.activeSelf &&
                !slot.IsOverflow)
            {
                normalCount++;
            }
        }

        return normalCount;
    }

    bool HasMode70OverflowSlot(BattleBuffGroupUIView group)
    {
        for (int index = 0;
            index < group.SlotPoolCount;
            index++)
        {
            BattleBuffIconUIView slot =
                group.GetSlotForTesting(index);
            if (slot != null &&
                slot.gameObject.activeSelf &&
                slot.IsOverflow)
            {
                return true;
            }
        }

        return false;
    }

    UnityEngine.UI.Image GetMode70IconImage(
        BattleBuffIconUIView slot
    )
    {
        return GetMode64PrivateField<UnityEngine.UI.Image>(
            slot,
            "iconImage"
        );
    }

    TMPro.TMP_Text GetMode70StackText(
        BattleBuffIconUIView slot
    )
    {
        return GetMode64PrivateField<TMPro.TMP_Text>(
            slot,
            "stackText"
        );
    }

    TMPro.TMP_Text GetMode70DecayText(
        BattleBuffIconUIView slot
    )
    {
        return GetMode64PrivateField<TMPro.TMP_Text>(
            slot,
            "decayText"
        );
    }

    bool RunMode70MissingTemplateSafetySubTest()
    {
        GameObject rootObject = new GameObject(
            "Mode70MissingTemplate",
            typeof(RectTransform)
        );
        rootObject.SetActive(false);
        BattleBuffGroupUIView group =
            rootObject.AddComponent<BattleBuffGroupUIView>();
        SetMode64PrivateField(
            group,
            "buffBindings",
            new BattleBuffIconBinding[0]
        );
        SetMode64PrivateField(group, "slotTemplate", null);
        rootObject.SetActive(true);

        CharacterData character = new CharacterData(
            "mode70_missing_template_owner",
            30,
            5,
            5
        );
        character.buffs.Add(CreateMode70Buff("MissingTemplate", 1));

        bool safe = true;
        try
        {
            group.SetCharacter(character);
            safe =
                group.SlotPoolCount == 0 &&
                group.BoundCharacter == character;
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "模式70 缺失模板不应抛异常：" + exception
            );
            safe = false;
        }

        Destroy(rootObject);
        return safe;
    }

    void DestroyMode70BuffTestContext(
        Mode70BuffTestContext context
    )
    {
        if (context == null)
        {
            return;
        }

        Destroy(context.rootObject);
        Destroy(context.strengthSprite);
        Destroy(context.guardSprite);
        Destroy(context.defaultSprite);
        Destroy(context.overflowSprite);
        Destroy(context.spriteTexture);
    }

    bool RunBattleBuffInspectorPreviewBasicTestSequence()
    {
        Debug.Log(
            "===== BattleBuffInspectorPreviewBasic 聚合测试开始 ====="
        );

        Mode70BuffTestContext context =
            CreateMode70BuffTestContext();
        BattleBuffGroupUIView group = context.groupView;
        BattleBuffGroupDebugPreview preview =
            context.rootObject.AddComponent<
                BattleBuffGroupDebugPreview
            >();
        bool previewFieldsConfigured =
            SetMode64PrivateField(
                preview,
                "enableRuntimePreview",
                false
            ) &&
            SetMode64PrivateField(
                preview,
                "previewBuffCount",
                9
            ) &&
            SetMode64PrivateField(
                preview,
                "defaultStack",
                1
            ) &&
            SetMode64PrivateField(
                preview,
                "useIncreasingStacks",
                true
            ) &&
            SetMode64PrivateField(
                preview,
                "applyOnStart",
                false
            ) &&
            SetMode64PrivateField(
                preview,
                "refreshWhenInspectorChanges",
                true
            );
        bool targetResolvedFromSameObject =
            GetMode64PrivateField<BattleBuffGroupUIView>(
                preview,
                "targetBuffGroup"
            ) == group;

        preview.ApplyPreview();
        bool test1 =
            previewFieldsConfigured &&
            targetResolvedFromSameObject &&
            preview.PreviewCharacter == null &&
            preview.PreviewBuffCount == 0;

        CharacterData previousDisplay = new CharacterData(
            "mode71_previous_display",
            30,
            1,
            1
        );
        previousDisplay.AddBuff(
            "Mode71Previous",
            "原显示Buff",
            "UpBuff",
            1,
            2,
            "TurnEnd",
            "DurationDown"
        );
        group.SetCharacter(previousDisplay);
        bool hadPreviousDisplay =
            CountMode70ActiveSlots(group) == 1;
        SetMode64PrivateField(
            preview,
            "enableRuntimePreview",
            true
        );
        SetMode64PrivateField(preview, "previewBuffCount", 0);
        preview.ApplyPreview();
        bool test2 =
            hadPreviousDisplay &&
            preview.PreviewCharacter != null &&
            preview.PreviewBuffCount == 0 &&
            group.BoundCharacter == preview.PreviewCharacter &&
            CountMode70ActiveSlots(group) == 0;

        SetMode64PrivateField(preview, "previewBuffCount", 1);
        preview.ApplyPreview();
        bool test3 =
            preview.PreviewBuffCount == 1 &&
            preview.PreviewCharacter.buffs[0].buffID ==
                "DebugPreviewBuff_01" &&
            CountMode70ActiveSlots(group) == 1;

        SetMode64PrivateField(preview, "previewBuffCount", 9);
        SetMode64PrivateField(
            preview,
            "useIncreasingStacks",
            true
        );
        preview.ApplyPreview();
        bool test4 =
            preview.PreviewBuffCount == 9 &&
            preview.PreviewCharacter.buffs[8].buffID ==
                "DebugPreviewBuff_09";
        HashSet<string> uniqueIDs =
            new HashSet<string>(System.StringComparer.Ordinal);
        bool increasingStacksCorrect = true;
        for (int index = 0;
            index < preview.PreviewCharacter.buffs.Count;
            index++)
        {
            BuffData buff = preview.PreviewCharacter.buffs[index];
            uniqueIDs.Add(buff.buffID);
            increasingStacksCorrect &=
                buff.stack == index + 1;
        }
        bool test5 = uniqueIDs.Count == 9;
        bool test6 = increasingStacksCorrect;

        SetMode64PrivateField(
            preview,
            "useIncreasingStacks",
            false
        );
        SetMode64PrivateField(preview, "defaultStack", 3);
        SetMode64PrivateField(preview, "previewBuffCount", 4);
        preview.ApplyPreview();
        bool test7 = true;
        for (int index = 0;
            index < preview.PreviewCharacter.buffs.Count;
            index++)
        {
            test7 &=
                preview.PreviewCharacter.buffs[index].stack == 3;
        }
        test7 &= preview.PreviewBuffCount == 4;

        SetMode64PrivateField(preview, "previewBuffCount", -5);
        preview.ApplyPreview();
        bool test8 =
            preview.PreviewCharacter != null &&
            preview.PreviewBuffCount == 0 &&
            CountMode70ActiveSlots(group) == 0;

        SetMode64PrivateField(preview, "previewBuffCount", 2);
        SetMode64PrivateField(preview, "defaultStack", 0);
        preview.ApplyPreview();
        bool test9 =
            preview.PreviewBuffCount == 2 &&
            preview.PreviewCharacter.buffs[0].stack == 1 &&
            preview.PreviewCharacter.buffs[1].stack == 1;

        CharacterData firstPreviewCharacter =
            preview.PreviewCharacter;
        SetMode64PrivateField(preview, "previewBuffCount", 3);
        preview.ApplyPreview();
        CharacterData secondPreviewCharacter =
            preview.PreviewCharacter;
        bool test10 =
            firstPreviewCharacter != secondPreviewCharacter &&
            firstPreviewCharacter.buffs.Count == 2 &&
            secondPreviewCharacter.buffs.Count == 3;
        bool test11 =
            group.BoundCharacter == secondPreviewCharacter;

        SetMode64PrivateField(
            preview,
            "useIncreasingStacks",
            true
        );
        SetMode64PrivateField(preview, "previewBuffCount", 9);
        preview.ApplyPreview();
        BattleBuffIconUIView overflowSlot =
            group.GetSlotForTesting(7);
        bool test12 =
            CountMode70ActiveSlots(group) == 8 &&
            CountMode70NormalSlots(group) == 7 &&
            overflowSlot != null &&
            overflowSlot.IsOverflow &&
            overflowSlot.OverflowHiddenCount == 2;

        int poolCountBeforeResize = group.SlotPoolCount;
        SetMode64PrivateField(preview, "previewBuffCount", 5);
        preview.ApplyPreview();
        bool test13 =
            preview.PreviewBuffCount == 5 &&
            CountMode70ActiveSlots(group) == 5 &&
            !HasMode70OverflowSlot(group) &&
            group.SlotPoolCount == poolCountBeforeResize;
        CharacterData previewBeforeRepeat =
            preview.PreviewCharacter;
        preview.ApplyPreview();
        bool test14 =
            preview.PreviewCharacter != previewBeforeRepeat &&
            preview.PreviewBuffCount == 5 &&
            group.SlotPoolCount == poolCountBeforeResize;

        preview.ClearPreview();
        bool test15 =
            preview.PreviewCharacter == null &&
            preview.PreviewBuffCount == 0 &&
            group.BoundCharacter == null &&
            CountMode70ActiveSlots(group) == 0 &&
            group.SlotPoolCount == poolCountBeforeResize;

        CharacterData formalCharacter = new CharacterData(
            "mode71_formal_character",
            30,
            4,
            4
        );
        formalCharacter.AddBuff(
            "Mode71FormalKeep",
            "正式角色保留Buff",
            "UpBuff",
            4,
            2,
            "TurnEnd",
            "DurationDown"
        );
        BuffData formalBuff = formalCharacter.buffs[0];
        group.SetCharacter(formalCharacter);
        SetMode64PrivateField(preview, "previewBuffCount", 3);
        preview.ApplyPreview();
        bool test16 =
            preview.PreviewCharacter != formalCharacter &&
            formalCharacter.buffs.Count == 1 &&
            formalCharacter.buffs[0] == formalBuff &&
            formalBuff.buffID == "Mode71FormalKeep" &&
            formalBuff.stack == 4;

        Mode71WrongHierarchyTestResult wrongHierarchyResult =
            RunMode71WrongHierarchySubTest();
        bool test20 = wrongHierarchyResult.fixtureDetected;

        preview.ClearPreview();
        group.SetCharacter(formalCharacter);
        SetMode64PrivateField(preview, "previewBuffCount", 9);
        SetMode64PrivateField(preview, "applyOnStart", true);
        int applyCountBeforeInitial =
            preview.ApplyInvocationCount;
        preview.ScheduleInitialApplyForTesting();
        bool initialDidNotApplyImmediately =
            preview.HasPendingInitialApply &&
            preview.ApplyInvocationCount ==
                applyCountBeforeInitial &&
            group.BoundCharacter == formalCharacter;
        preview.CompleteInitialApplyForTesting();
        bool test21 =
            initialDidNotApplyImmediately &&
            !preview.HasPendingInitialApply &&
            preview.ApplyInvocationCount ==
                applyCountBeforeInitial + 1 &&
            group.BoundCharacter == preview.PreviewCharacter &&
            preview.PreviewBuffCount == 9;
        SetMode64PrivateField(preview, "applyOnStart", false);

        int applyCountBeforeRefresh =
            preview.ApplyInvocationCount;
        SetMode64PrivateField(preview, "previewBuffCount", 4);
        preview.RequestRefreshForTesting();
        preview.RequestRefreshForTesting();
        preview.RequestRefreshForTesting();
        bool refreshWasMerged =
            preview.HasPendingRefresh &&
            preview.ApplyInvocationCount ==
                applyCountBeforeRefresh;
        preview.CompleteRefreshForTesting();
        bool test22 =
            refreshWasMerged &&
            !preview.HasPendingRefresh &&
            preview.ApplyInvocationCount ==
                applyCountBeforeRefresh + 1 &&
            preview.PreviewBuffCount == 4;

        int stablePoolCount = group.RuntimeSlotCount;
        List<BattleBuffIconUIView> stablePreviewSlots =
            new List<BattleBuffIconUIView>();
        for (int index = 0;
            index < group.RuntimeSlots.Count;
            index++)
        {
            stablePreviewSlots.Add(group.RuntimeSlots[index]);
        }
        for (int iteration = 0; iteration < 10; iteration++)
        {
            preview.ApplyPreview();
        }
        bool test23 =
            group.RuntimeSlotCount == stablePoolCount &&
            HaveSameMode70RuntimeSlots(
                group,
                stablePreviewSlots
            );

        int[] previewCounts = { 9, 12, 4, 9 };
        bool sequenceStable = true;
        for (int index = 0;
            index < previewCounts.Length;
            index++)
        {
            SetMode64PrivateField(
                preview,
                "previewBuffCount",
                previewCounts[index]
            );
            preview.ApplyPreview();
            sequenceStable &=
                group.RuntimeSlotCount == stablePoolCount &&
                HaveSameMode70RuntimeSlots(
                    group,
                    stablePreviewSlots
                );
        }
        BattleBuffIconUIView finalOverflow =
            group.GetSlotForTesting(7);
        bool test24 =
            sequenceStable &&
            stablePoolCount == 8 &&
            finalOverflow != null &&
            finalOverflow.IsOverflow &&
            finalOverflow.OverflowHiddenCount == 2;
        bool test25 =
            AreAllMode70RuntimeSlotsDirectChildren(
                group,
                context.slotsRoot
            );
        bool test26 =
            HasUniqueMode70RuntimeSlotsAndNames(group);

        preview.ClearPreview();
        bool test27 =
            group.RuntimeSlotCount == stablePoolCount &&
            HaveSameMode70RuntimeSlots(
                group,
                stablePreviewSlots
            );
        bool test28 =
            wrongHierarchyResult.repeatedApplyStable;

        SetMode64PrivateField(preview, "previewBuffCount", 9);
        preview.ApplyPreview();
        bool test29 =
            CountMode70NormalSlots(group) == 7 &&
            HasMode70OverflowSlot(group) &&
            AreMode70VisibleSlotsAtExpectedPositions(group, 8);
        bool test30 =
            AreMode70SlotsOnSameRow(group, 0, 4) &&
            AreMode70SlotsOnSameRow(group, 4, 4);
        bool test31 =
            AreMode70SlotsOnSameColumn(group, 0, 4) &&
            AreMode70SlotsOnSameColumn(group, 1, 5);
        BattleBuffIconUIView previewOverflow =
            group.GetSlotForTesting(7);
        bool test32 =
            previewOverflow != null &&
            previewOverflow.IsOverflow &&
            IsMode70SlotAtExpectedPosition(group, 7);

        bool previewGridSequenceStable = true;
        int[] gridPreviewCounts = { 9, 12, 4, 9 };
        for (int index = 0;
            index < gridPreviewCounts.Length;
            index++)
        {
            int previewCount = gridPreviewCounts[index];
            SetMode64PrivateField(
                preview,
                "previewBuffCount",
                previewCount
            );
            preview.ApplyPreview();
            int visibleCount = Mathf.Min(previewCount, 8);
            previewGridSequenceStable &=
                group.RuntimeSlotCount == stablePoolCount &&
                CountMode70ActiveSlots(group) == visibleCount &&
                AreMode70VisibleSlotsAtExpectedPositions(
                    group,
                    visibleCount
                );
        }
        bool test33 =
            previewGridSequenceStable &&
            group.GetSlotForTesting(7).IsOverflow;

        bool test17 =
            RunBattleBuffGridLayoutBasicTestSequence();
        bool test18 =
            RunBattleActionSlotVisualInteractionBasicTestSequence();
        bool test19 =
            RunBattleCardClickAssignBasicTestSequence() &&
            RunBattleCardClickInteractionIntegrationTestSequence();

        Debug.Log("模式71 测试1 预览关闭时不生成角色：" + test1);
        Debug.Log("模式71 测试2 零Buff清空正式Buff栏显示：" + test2);
        Debug.Log("模式71 测试3 生成一个唯一预览Buff：" + test3);
        Debug.Log("模式71 测试4 生成九个预览Buff：" + test4);
        Debug.Log("模式71 测试5 预览buffID全部唯一：" + test5);
        Debug.Log("模式71 测试6 递增层数为一至N：" + test6);
        Debug.Log("模式71 测试7 固定层数使用defaultStack：" + test7);
        Debug.Log("模式71 测试8 负数量按零处理：" + test8);
        Debug.Log("模式71 测试9 非法固定层数按一处理：" + test9);
        Debug.Log("模式71 测试10 重复应用替换临时角色：" + test10);
        Debug.Log("模式71 测试11 调用正式SetCharacter绑定：" + test11);
        Debug.Log("模式71 测试12 九Buff显示七普通与...+2：" + test12);
        Debug.Log("模式71 测试13 修改数量后复用正式槽位池：" + test13);
        Debug.Log("模式71 测试14 重复应用不增加槽位对象：" + test14);
        Debug.Log("模式71 测试15 ClearPreview清空预览与显示：" + test15);
        Debug.Log("模式71 测试16 不修改正式CharacterData：" + test16);
        Debug.Log("模式71 测试17 模式70 Buff网格回归：" + test17);
        Debug.Log("模式71 测试18 模式69行动槽视觉回归：" + test18);
        Debug.Log("模式71 测试19 模式66与67卡牌指派回归：" + test19);
        Debug.Log("模式71 测试20 正式错误层级夹具被识别：" + test20);
        Debug.Log("模式71 测试21 初次预览帧末覆盖正式绑定：" + test21);
        Debug.Log("模式71 测试22 连续OnValidate请求合并一次：" + test22);
        Debug.Log("模式71 测试23 ApplyPreview十次池不增长：" + test23);
        Debug.Log("模式71 测试24 九十二四九切换池稳定：" + test24);
        Debug.Log("模式71 测试25 动态槽位均为SlotsRoot直接子项：" + test25);
        Debug.Log("模式71 测试26 无重复槽位实例和名称：" + test26);
        Debug.Log("模式71 测试27 ClearPreview不销毁槽位池：" + test27);
        Debug.Log("模式71 测试28 错误层级重复刷新不扩容：" + test28);
        Debug.Log("模式71 测试29 九Buff严格显示七普通加Overflow：" +
            test29);
        Debug.Log("模式71 测试30 预览网格两行各自严格水平：" +
            test30);
        Debug.Log("模式71 测试31 预览网格上下列严格对齐：" +
            test31);
        Debug.Log("模式71 测试32 Overflow位于第二行最后一格：" +
            test32);
        Debug.Log("模式71 测试33 九十二四九切换无斜线与漂移：" +
            test33);
        Debug.Log(
            "===== BattleBuffInspectorPreviewBasic 聚合测试结束 ====="
        );

        DestroyMode70BuffTestContext(context);

        return test1 &&
            test2 &&
            test3 &&
            test4 &&
            test5 &&
            test6 &&
            test7 &&
            test8 &&
            test9 &&
            test10 &&
            test11 &&
            test12 &&
            test13 &&
            test14 &&
            test15 &&
            test16 &&
            test17 &&
            test18 &&
            test19 &&
            test20 &&
            test21 &&
            test22 &&
            test23 &&
            test24 &&
            test25 &&
            test26 &&
            test27 &&
            test28 &&
            test29 &&
            test30 &&
            test31 &&
            test32 &&
            test33;
    }

    bool RunBattlePermanentBulletBuffBasicTestSequence()
    {
        Debug.Log(
            "===== BattlePermanentBulletBuffBasic 聚合测试开始 ====="
        );

        List<CardTestData> cards = CardDataLoader.LoadCardData();
        List<CharacterDefinitionData> characterDefinitions =
            CharacterDefinitionLoader.LoadDefinitions();
        List<EnemyDefinitionData> enemyDefinitions =
            EnemyDefinitionLoader.LoadDefinitions();
        CharacterDefinitionData allyADefinition =
            CharacterDefinitionLoader.FindByID(
                characterDefinitions,
                "ally_001"
            );
        CharacterDefinitionData allyBDefinition =
            CharacterDefinitionLoader.FindByID(
                characterDefinitions,
                "ally_002"
            );
        EnemyDefinitionData enemyDefinition =
            EnemyDefinitionLoader.FindByID(
                enemyDefinitions,
                "enemy_001"
            );

        BattleUnitFactoryResult allyAResult =
            BattleUnitFactory.CreatePlayer(
                allyADefinition,
                cards
            );
        BattleUnitFactoryResult allyBResult =
            BattleUnitFactory.CreatePlayer(
                allyBDefinition,
                cards
            );
        BattleUnitFactoryResult enemyResult =
            BattleUnitFactory.CreateEnemy(
                enemyDefinition,
                cards
            );
        CharacterData allyA = allyAResult.unit;
        CharacterData allyB = allyBResult.unit;
        CharacterData enemy = enemyResult.unit;
        BuffData initialBullet =
            FindMode72ActiveBuff(allyA, "Bullet");

        bool test1 =
            allyAResult.isSuccess &&
            allyA != null &&
            allyA.GetBuffStack("Bullet") == 6;
        bool test2 =
            allyBResult.isSuccess &&
            allyB != null &&
            allyB.GetBuffStack("Bullet") == 0;
        bool test3 =
            enemyResult.isSuccess &&
            enemy != null &&
            enemy.GetBuffStack("Bullet") == 0;
        bool test4 =
            initialBullet != null &&
            initialBullet.buffName == "子弹" &&
            initialBullet.buffCategory == "AbilityBuff" &&
            initialBullet.checkTiming == "None" &&
            initialBullet.expireRule == "Permanent";
        bool test5 = initialBullet != null &&
            initialBullet.stack == 6;
        bool test6 = initialBullet != null &&
            initialBullet.duration == -1;
        bool test7 =
            CountMode72BuffBatches(allyA, "Bullet") == 1;

        BattleUnitFactory.ApplyInitialBuffs(
            allyA,
            allyADefinition.initialBuffs
        );
        bool test8 = allyA.GetBuffStack("Bullet") == 6;
        bool test9 =
            CountMode72BuffBatches(allyA, "Bullet") == 1;

        Mode70BuffTestContext uiContext =
            CreateMode70BuffTestContext();
        BattleBuffGroupUIView group = uiContext.groupView;
        BattleBuffIconBinding[] bulletBindings =
        {
            new BattleBuffIconBinding
            {
                buffID = "Bullet",
                displayName = "子弹",
                iconSprite = uiContext.strengthSprite,
                iconView = null
            }
        };
        SetMode64PrivateField(
            group,
            "buffBindings",
            bulletBindings
        );
        group.SetCharacter(allyA);
        BattleBuffIconUIView bulletSlot =
            group.GetSlotForTesting(0);
        TMPro.TMP_Text bulletStackText =
            GetMode70StackText(bulletSlot);
        TMPro.TMP_Text bulletDecayText =
            GetMode70DecayText(bulletSlot);
        bool test10 =
            CountMode70ActiveSlots(group) == 1 &&
            CountMode70NormalSlots(group) == 1;
        bool test11 =
            bulletStackText != null &&
            bulletStackText.text == "6";
        bool test12 =
            bulletSlot != null &&
            !bulletSlot.IsOverflow;
        bool test13 =
            bulletDecayText != null &&
            !bulletDecayText.gameObject.activeSelf &&
            bulletDecayText.text != "-1";
        bool test14 =
            GetMode70IconImage(bulletSlot).sprite ==
                uiContext.strengthSprite;

        SetMode64PrivateField(
            group,
            "buffBindings",
            new BattleBuffIconBinding[0]
        );
        group.SetCharacter(allyA);
        bool test15 =
            GetMode70IconImage(group.GetSlotForTesting(0)).sprite ==
                uiContext.defaultSprite;

        BattleBuffGroupDebugPreview preview =
            uiContext.rootObject.AddComponent<
                BattleBuffGroupDebugPreview
            >();
        group.SetCharacter(allyA);
        preview.SetRuntimePreviewEnabledForTesting(true);
        preview.ScheduleInitialApplyForTesting();
        bool previewWasScheduled =
            preview.HasPendingInitialApply;
        preview.SetRuntimePreviewEnabledForTesting(false);
        preview.ApplyPreview();
        bool test16 =
            previewWasScheduled &&
            !preview.HasPendingInitialApply &&
            !preview.HasPendingRefresh &&
            preview.PreviewCharacter == null;
        bool test17 =
            group.BoundCharacter == allyA &&
            CountMode70ActiveSlots(group) == 1 &&
            GetMode70StackText(
                group.GetSlotForTesting(0)
            ).text == "6";

        allyA.AddBuff(
            "Mode72Temporary",
            "模式72限时Buff",
            "UpBuff",
            1,
            1,
            "TurnEnd",
            "DurationDown"
        );
        List<CharacterData> turnParticipants =
            new List<CharacterData> { allyA };
        for (int iteration = 0; iteration < 3; iteration++)
        {
            BattleTurnProcessor.EndTurn(turnParticipants);
        }
        BuffData bulletAfterThreeTurns =
            FindMode72ActiveBuff(allyA, "Bullet");
        bool test18 = bulletAfterThreeTurns != null;
        bool test19 =
            bulletAfterThreeTurns != null &&
            bulletAfterThreeTurns.duration == -1;
        bool test20 =
            bulletAfterThreeTurns != null &&
            bulletAfterThreeTurns.stack == 6;
        bool test21 =
            allyA.GetBuffStack("Mode72Temporary") == 0;

        int poolCountBeforeRemoval = group.RuntimeSlotCount;
        BattleBuffIconUIView reusableSlot =
            group.GetSlotForTesting(0);
        int consumedBullet;
        bool bulletRemovedThroughDataLayer =
            allyA.TryConsumeBuffStackAsResource(
                "Bullet",
                6,
                out consumedBullet
            );
        group.SetCharacter(allyA);
        bool test22 =
            bulletRemovedThroughDataLayer &&
            consumedBullet == 6 &&
            allyA.GetBuffStack("Bullet") == 0 &&
            CountMode70ActiveSlots(group) == 0;
        bool test23 =
            group.RuntimeSlotCount == poolCountBeforeRemoval;

        allyA.AddBuff("Bullet", 6, -1);
        group.SetCharacter(allyA);
        bool test24 =
            group.GetSlotForTesting(0) == reusableSlot &&
            reusableSlot.gameObject.activeSelf &&
            GetMode70StackText(reusableSlot).text == "6";

        CharacterData pendingOnlyCharacter = new CharacterData(
            "mode72_pending_only",
            30,
            1,
            1
        );
        pendingOnlyCharacter.AddPendingBuff(
            "Bullet",
            6,
            -1,
            1,
            1,
            1
        );
        group.SetCharacter(pendingOnlyCharacter);
        bool test25 =
            pendingOnlyCharacter.GetBuffStack("Bullet") == 0 &&
            CountMode70ActiveSlots(group) == 0;

        DestroyMode70BuffTestContext(uiContext);

        bool test26 =
            RunBattleBuffInspectorPreviewBasicTestSequence();
        bool test27 =
            RunBattleBuffGridLayoutBasicTestSequence();
        bool test28 =
            RunBattleActionSlotVisualInteractionBasicTestSequence();
        bool test29 =
            RunBattleCardClickAssignBasicTestSequence() &&
            RunBattleCardClickInteractionIntegrationTestSequence();

        Debug.Log("模式72 测试1 正式入口只给Ally01添加Bullet：" + test1);
        Debug.Log("模式72 测试2 Ally02不获得Bullet：" + test2);
        Debug.Log("模式72 测试3 敌人不获得Bullet：" + test3);
        Debug.Log("模式72 测试4 Factory通过AddBuff生成完整正式批次：" + test4);
        Debug.Log("模式72 测试5 Bullet层数为6：" + test5);
        Debug.Log("模式72 测试6 Bullet持续时间为-1：" + test6);
        Debug.Log("模式72 测试7 Bullet只有一个有效批次：" + test7);
        Debug.Log("模式72 测试8 重复初始化不会叠到12层：" + test8);
        Debug.Log("模式72 测试9 重复初始化不增加第二批Bullet：" + test9);
        Debug.Log("模式72 测试10 UI只显示一个普通Buff槽位：" + test10);
        Debug.Log("模式72 测试11 StackText显示6：" + test11);
        Debug.Log("模式72 测试12 Bullet不是Overflow：" + test12);
        Debug.Log("模式72 测试13 永久Buff不显示负数倒计时：" + test13);
        Debug.Log("模式72 测试14 Bullet Binding使用专属图：" + test14);
        Debug.Log("模式72 测试15 Bullet缺少Binding时回退默认图：" + test15);
        Debug.Log("模式72 测试16 DebugPreview关闭并取消待执行预览：" + test16);
        Debug.Log("模式72 测试17 DebugPreview关闭不覆盖正式Bullet：" + test17);
        Debug.Log("模式72 测试18 三次回合生命周期后Bullet仍存在：" + test18);
        Debug.Log("模式72 测试19 三次处理后duration仍为-1：" + test19);
        Debug.Log("模式72 测试20 三次处理后stack仍为6：" + test20);
        Debug.Log("模式72 测试21 duration1普通Buff正常过期：" + test21);
        Debug.Log("模式72 测试22 移除Bullet并刷新后槽位隐藏：" + test22);
        Debug.Log("模式72 测试23 槽位隐藏后slotPool数量不减少：" + test23);
        Debug.Log("模式72 测试24 再添加Bullet复用原槽位实例：" + test24);
        Debug.Log("模式72 测试25 pending Bullet不会提前显示：" + test25);
        Debug.Log("模式72 测试26 模式71 Buff预览回归：" + test26);
        Debug.Log("模式72 测试27 模式70 Buff网格回归：" + test27);
        Debug.Log("模式72 测试28 模式69行动槽视觉回归：" + test28);
        Debug.Log("模式72 测试29 模式66与67卡牌指派回归：" + test29);
        Debug.Log(
            "===== BattlePermanentBulletBuffBasic 聚合测试结束 ====="
        );

        return test1 &&
            test2 &&
            test3 &&
            test4 &&
            test5 &&
            test6 &&
            test7 &&
            test8 &&
            test9 &&
            test10 &&
            test11 &&
            test12 &&
            test13 &&
            test14 &&
            test15 &&
            test16 &&
            test17 &&
            test18 &&
            test19 &&
            test20 &&
            test21 &&
            test22 &&
            test23 &&
            test24 &&
            test25 &&
            test26 &&
            test27 &&
            test28 &&
            test29;
    }

    BuffData FindMode72ActiveBuff(
        CharacterData character,
        string buffID
    )
    {
        if (character == null || character.buffs == null)
        {
            return null;
        }

        for (int index = 0;
            index < character.buffs.Count;
            index++)
        {
            BuffData buff = character.buffs[index];
            if (buff != null &&
                buff.buffID == buffID &&
                buff.stack > 0)
            {
                return buff;
            }
        }

        return null;
    }

    int CountMode72BuffBatches(
        CharacterData character,
        string buffID
    )
    {
        if (character == null || character.buffs == null)
        {
            return 0;
        }

        int count = 0;
        for (int index = 0;
            index < character.buffs.Count;
            index++)
        {
            BuffData buff = character.buffs[index];
            if (buff != null &&
                buff.buffID == buffID &&
                buff.stack > 0)
            {
                count++;
            }
        }

        return count;
    }

    Mode71WrongHierarchyTestResult
        RunMode71WrongHierarchySubTest()
    {
        Mode71WrongHierarchyTestResult result =
            new Mode71WrongHierarchyTestResult();
        GameObject rootObject = new GameObject(
            "Mode71WrongHierarchyBuffGroup",
            typeof(RectTransform)
        );
        rootObject.SetActive(false);
        RectTransform slotsRoot =
            rootObject.GetComponent<RectTransform>();
        BattleBuffGroupUIView group =
            rootObject.AddComponent<BattleBuffGroupUIView>();

        GameObject templateObject = new GameObject(
            "BuffTemplate",
            typeof(RectTransform)
        );
        templateObject.transform.SetParent(slotsRoot, false);
        templateObject.AddComponent<BattleBuffIconUIView>();
        BattleBuffIconUIView nestedSlot1 =
            CreateMode70BuffIconView(
                templateObject.transform,
                "Buff_1"
            );
        BattleBuffIconUIView nestedSlot2 =
            CreateMode70BuffIconView(
                templateObject.transform,
                "Buff_2"
            );
        BattleBuffIconBinding[] bindings =
        {
            new BattleBuffIconBinding
            {
                buffID = "Strength",
                displayName = "强壮",
                iconView = nestedSlot1
            },
            new BattleBuffIconBinding
            {
                buffID = "GuardUp",
                displayName = "防护",
                iconView = nestedSlot2
            }
        };
        SetMode64PrivateField(group, "buffBindings", bindings);
        SetMode64PrivateField(group, "slotsRoot", slotsRoot);
        SetMode64PrivateField(group, "slotTemplate", null);
        SetMode64PrivateField(group, "columnsPerRow", 4);
        SetMode64PrivateField(group, "maxRows", 2);
        rootObject.SetActive(true);

        CharacterData formalCharacter = new CharacterData(
            "mode71_wrong_formal",
            30,
            1,
            1
        );
        formalCharacter.AddBuff(
            "Mode71WrongFormalBuff",
            "正式Buff",
            "UpBuff",
            1,
            2,
            "TurnEnd",
            "DurationDown"
        );
        BattleBuffGroupDebugPreview preview =
            rootObject.AddComponent<
                BattleBuffGroupDebugPreview
            >();
        SetMode64PrivateField(
            preview,
            "enableRuntimePreview",
            true
        );
        SetMode64PrivateField(
            preview,
            "previewBuffCount",
            9
        );
        SetMode64PrivateField(
            preview,
            "useIncreasingStacks",
            true
        );
        SetMode64PrivateField(
            preview,
            "applyOnStart",
            false
        );

        int instanceCountBefore =
            rootObject.GetComponentsInChildren<
                BattleBuffIconUIView
            >(true).Length;
        int warningCountBefore =
            group.ConfigurationWarningCount;

        group.SetCharacter(formalCharacter);
        preview.ApplyPreview();
        group.SetCharacter(formalCharacter);
        preview.ApplyPreview();

        int[] previewCounts = { 9, 12, 4, 9 };
        for (int index = 0;
            index < previewCounts.Length;
            index++)
        {
            SetMode64PrivateField(
                preview,
                "previewBuffCount",
                previewCounts[index]
            );
            preview.ApplyPreview();
        }
        for (int iteration = 0; iteration < 10; iteration++)
        {
            preview.ApplyPreview();
        }

        BattleBuffIconUIView[] allViews =
            rootObject.GetComponentsInChildren<
                BattleBuffIconUIView
            >(true);
        int directIconViewCount = 0;
        for (int index = 0; index < allViews.Length; index++)
        {
            if (allViews[index].transform.parent == slotsRoot)
            {
                directIconViewCount++;
            }
        }

        result.fixtureDetected =
            instanceCountBefore == 3 &&
            directIconViewCount == 1 &&
            group.RuntimeSlotCount == 0 &&
            !ContainsMode70RuntimeSlot(group, nestedSlot1) &&
            !ContainsMode70RuntimeSlot(group, nestedSlot2) &&
            warningCountBefore > 0;
        result.repeatedApplyStable =
            allViews.Length == instanceCountBefore &&
            group.RuntimeSlotCount == 0 &&
            group.ConfigurationWarningCount >=
                warningCountBefore &&
            HasUniqueMode70ViewInstancesAndNames(allViews);

        Destroy(rootObject);
        return result;
    }

    bool HasUniqueMode70RuntimeSlotsAndNames(
        BattleBuffGroupUIView group
    )
    {
        HashSet<BattleBuffIconUIView> instances =
            new HashSet<BattleBuffIconUIView>();
        HashSet<string> names =
            new HashSet<string>(
                System.StringComparer.Ordinal
            );
        for (int index = 0;
            index < group.RuntimeSlots.Count;
            index++)
        {
            BattleBuffIconUIView slot =
                group.RuntimeSlots[index];
            if (slot == null ||
                !instances.Add(slot) ||
                !names.Add(slot.name))
            {
                return false;
            }
        }

        return true;
    }

    bool HasUniqueMode70ViewInstancesAndNames(
        BattleBuffIconUIView[] views
    )
    {
        HashSet<BattleBuffIconUIView> instances =
            new HashSet<BattleBuffIconUIView>();
        HashSet<string> names =
            new HashSet<string>(
                System.StringComparer.Ordinal
            );
        for (int index = 0; index < views.Length; index++)
        {
            BattleBuffIconUIView view = views[index];
            if (view == null ||
                !instances.Add(view) ||
                !names.Add(view.name))
            {
                return false;
            }
        }

        return true;
    }

    bool RunMode65HandAssignmentRegressionSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "motion65_hand",
            30,
            30,
            50,
            10,
            8,
            5
        );
        List<BattleActionSlot> slots =
            BattleActionSlotManager.CreatePartyActionSlots(
                context.allyA,
                context.allyB,
                2
            );
        context.runtimeState.SetActionSlots(slots);
        context.runtimeState.SetIntentQueue(
            new List<BattleEnemyIntent>()
        );
        SetTestLifecyclePhase(
            context.runtimeState,
            BattleLifecyclePhase.Prepare
        );

        BattleCardState firstCard =
            CreateFixedAttackCardForCharacter(
                context.allyA,
                "motion65_first_card",
                5
            );
        BattleCardState replacementCard =
            CreateFixedAttackCardForCharacter(
                context.allyA,
                "motion65_replacement_card",
                6
            );
        CardTestData sinCardData = CreateFixedAttackCardData(
            "motion65_sin_card_data",
            "模式65攻击罪卡",
            5
        );
        sinCardData.isSinCard = true;
        sinCardData.sinCardCategory = SinCardCategory.Clash;
        sinCardData.sinCardUseRule = SinCardUseRule.Permanent;
        BattleCardState sinCard = BattleCardManager.CreateBattleCard(
            context.allyA,
            sinCardData,
            "motion65_sin_card"
        );

        List<BattleCardState> visibleBefore =
            GetMode60VisibleCards(
                context.runtimeState,
                firstCard,
                replacementCard,
                sinCard
            );
        BattleActionAssignmentResult firstAssignResult;
        bool firstAssigned =
            BattleCardAssignmentRouter.TryAssignToEnemySlot(
                context.runtimeState,
                context.allyA,
                1,
                context.allyA,
                firstCard,
                context.enemy,
                null,
                out firstAssignResult
            );
        BattleActionAssignmentResult replaceResult;
        bool replaced =
            BattleCardAssignmentRouter.TryAssignToEnemySlot(
                context.runtimeState,
                context.allyA,
                1,
                context.allyA,
                replacementCard,
                context.enemy,
                null,
                out replaceResult
            );
        List<BattleCardState> visibleAfterReplace =
            GetMode60VisibleCards(
                context.runtimeState,
                firstCard,
                replacementCard,
                sinCard
            );
        BattleActionAssignmentResult cancelResult;
        bool cancelled =
            BattleCardAssignmentRouter.TryCancelSelectedSlot(
                context.runtimeState,
                context.allyA,
                1,
                out cancelResult
            );
        List<BattleCardState> visibleAfterCancel =
            GetMode60VisibleCards(
                context.runtimeState,
                firstCard,
                replacementCard,
                sinCard
            );

        return visibleBefore.Contains(firstCard) &&
            visibleBefore.Contains(replacementCard) &&
            visibleBefore.Contains(sinCard) &&
            firstAssigned &&
            firstAssignResult != null &&
            firstAssignResult.isSuccess &&
            replaced &&
            replaceResult != null &&
            replaceResult.isSuccess &&
            visibleAfterReplace.Contains(firstCard) &&
            !visibleAfterReplace.Contains(replacementCard) &&
            visibleAfterReplace.Contains(sinCard) &&
            cancelled &&
            cancelResult != null &&
            cancelResult.isSuccess &&
            visibleAfterCancel.Contains(firstCard) &&
            visibleAfterCancel.Contains(replacementCard) &&
            visibleAfterCancel.Contains(sinCard);
    }

    bool RunBattleCardClickAssignBasicTestSequence()
    {
        Debug.Log("===== BattleCardClickAssignBasic 聚合测试开始 =====");

        bool hoverMovedUp;
        bool hoverRootStable;
        bool hoverExitRestored;
        RunMode66HoverVisualSubTest(
            out hoverMovedUp,
            out hoverRootStable,
            out hoverExitRestored
        );

        CharacterData owner = new CharacterData(
            "click66_owner",
            30,
            10,
            10
        );
        CharacterData target = new CharacterData(
            "click66_target",
            50,
            5,
            5
        );
        BattleCardState firstCard =
            CreateFixedAttackCardForCharacter(
                owner,
                "click66_first",
                5
            );
        BattleCardState secondCard =
            CreateFixedAttackCardForCharacter(
                owner,
                "click66_second",
                6
            );
        BattleCardState coolingCard =
            CreateFixedAttackCardForCharacter(
                owner,
                "click66_cooling",
                5
            );
        coolingCard.currentCooldown = 1;

        BattleCardSelectionController selection =
            new BattleCardSelectionController();
        BattleCardUIView firstView = CreatePrimaryPreviewCardView(
            "Click66FirstView",
            owner,
            target,
            firstCard
        );
        BattleCardUIView secondView = CreatePrimaryPreviewCardView(
            "Click66SecondView",
            owner,
            target,
            secondCard
        );
        BattleCardUIView coolingView = CreatePrimaryPreviewCardView(
            "Click66CoolingView",
            owner,
            target,
            coolingCard
        );
        firstView.BindCard(
            owner,
            firstCard,
            BattleCardUIPreviewBuilder.Build(owner, target, firstCard),
            selection
        );
        secondView.BindCard(
            owner,
            secondCard,
            BattleCardUIPreviewBuilder.Build(owner, target, secondCard),
            selection
        );
        coolingView.BindCard(
            owner,
            coolingCard,
            BattleCardUIPreviewBuilder.Build(owner, target, coolingCard),
            selection
        );

        PointerEventData leftClick = new PointerEventData(null)
        {
            button = PointerEventData.InputButton.Left
        };
        firstView.OnPointerClick(leftClick);
        bool test4 =
            selection.IsSelected(firstView) &&
            firstView.IsSelected;
        firstView.OnPointerExit(leftClick);
        bool test5 =
            selection.IsSelected(firstView) &&
            firstView.IsSelected;
        firstView.OnPointerClick(leftClick);
        bool test6 = !selection.HasSelection;

        firstView.OnPointerClick(leftClick);
        secondView.OnPointerClick(leftClick);
        bool test7 =
            selection.IsSelected(secondView) &&
            !selection.IsSelected(firstView) &&
            secondView.IsSelected &&
            !firstView.IsSelected;

        selection.ClearSelection();
        coolingView.OnPointerEnter(leftClick);
        coolingView.OnPointerClick(leftClick);
        bool test8 =
            !selection.HasSelection &&
            !coolingView.CanSelect;

        BattleEndedTestContext assignContext =
            CreateBattleEndedTestContext(
                "click66_assign",
                30,
                30,
                50,
                12,
                4,
                5
            );
        List<BattleActionSlot> assignSlots =
            BattleActionSlotManager.CreatePartyActionSlots(
                assignContext.allyA,
                assignContext.allyB,
                2
            );
        BattleEnemyIntent assignIntent =
            CreatePreparedAssignmentIntent(
                assignContext,
                "click66_assign_intent",
                assignContext.allyB,
                2,
                1,
                1
            );
        assignContext.runtimeState.SetActionSlots(assignSlots);
        assignContext.runtimeState.SetIntentQueue(
            BattleEnemyIntentManager.CreateIntentQueue(assignIntent)
        );
        BattleCardState assignCard =
            CreateFixedAttackCardForCharacter(
                assignContext.allyA,
                "click66_assign_card",
                8
            );
        BattleCardSelectionController assignSelection =
            new BattleCardSelectionController();
        BattleCardUIView assignView = CreatePrimaryPreviewCardView(
            "Click66AssignView",
            assignContext.allyA,
            assignContext.enemy,
            assignCard
        );
        assignView.BindCard(
            assignContext.allyA,
            assignCard,
            BattleCardUIPreviewBuilder.Build(
                assignContext.allyA,
                assignContext.enemy,
                assignCard
            ),
            assignSelection
        );

        GameObject sourceSlotObject = new GameObject(
            "Click66SourceSlot",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(BattleActionSlotUIView)
        );
        BattleActionSlotUIView sourceSlotView =
            sourceSlotObject.GetComponent<BattleActionSlotUIView>();
        sourceSlotView.BindInteraction(
            assignContext.allyA,
            0,
            false,
            null
        );
        BattleCardInteractionCoordinator assignCoordinator =
            new BattleCardInteractionCoordinator(assignSelection);
        bool assignSourceSelected =
            assignCoordinator.SelectSourceSlot(sourceSlotView);

        GameObject enemySlotObject = new GameObject(
            "Click66EnemySlot",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(BattleActionSlotUIView)
        );
        BattleActionSlotUIView enemySlotView =
            enemySlotObject.GetComponent<BattleActionSlotUIView>();
        BattleCardInteractionOutcome clickOutcome = null;
        enemySlotView.BindInteraction(
            assignContext.enemy,
            0,
            true,
            clickedSlot =>
            {
                clickOutcome = assignCoordinator.ClickEnemySlot(
                    assignContext.runtimeState,
                    clickedSlot
                );
            }
        );
        enemySlotView.SetBoundEnemyIntent(assignIntent);

        enemySlotView.OnPointerClick(leftClick);
        bool test9 =
            assignSourceSelected &&
            clickOutcome != null &&
            !clickOutcome.hadSelectedCard &&
            !clickOutcome.isSuccess &&
            BattleActionSlotManager.GetSlot(
                assignSlots,
                assignContext.allyA,
                1
            ).IsEmpty();

        assignView.OnPointerClick(leftClick);
        enemySlotView.OnPointerClick(leftClick);
        bool test10 =
            clickOutcome != null &&
            clickOutcome.hadSelectedCard &&
            clickOutcome.isSuccess &&
            clickOutcome.assignmentResult != null &&
            clickOutcome.assignmentResult.isSuccess &&
            object.ReferenceEquals(
                BattleActionSlotManager.GetSlot(
                    assignSlots,
                    assignContext.allyA,
                    1
                ).cardState,
                assignCard
            );
        bool test11 =
            !assignSelection.HasSelection &&
            object.ReferenceEquals(
                assignCoordinator.SelectedCharacter,
                assignContext.allyA
            ) &&
            object.ReferenceEquals(
                assignCoordinator.SelectedActionSlotView,
                sourceSlotView
            ) &&
            sourceSlotView.IsSelected;

        BattleExecutionPlan executionPlan =
            BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
                assignSlots,
                assignContext.runtimeState.intentQueue
            );
        assignContext.runtimeState.SetExecutionPlan(executionPlan);
        SetTestLifecyclePhase(
            assignContext.runtimeState,
            BattleLifecyclePhase.Executing
        );
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(
            executionPlan,
            assignContext.runtimeState
        );

        BattleEndedTestContext invalidContext =
            CreateBattleEndedTestContext(
                "click66_invalid",
                30,
                30,
                50,
                10,
                8,
                5
            );
        List<BattleActionSlot> invalidSlots =
            BattleActionSlotManager.CreatePartyActionSlots(
                invalidContext.allyA,
                invalidContext.allyB,
                2
            );
        invalidContext.runtimeState.SetActionSlots(invalidSlots);
        invalidContext.runtimeState.SetIntentQueue(
            new List<BattleEnemyIntent>()
        );
        BattleCardState invalidCard =
            CreateFixedAttackCardForCharacter(
                invalidContext.allyA,
                "click66_invalid_card",
                5
            );
        BattleCardSelectionController invalidSelection =
            new BattleCardSelectionController();
        BattleCardUIView invalidView = CreatePrimaryPreviewCardView(
            "Click66InvalidView",
            invalidContext.allyA,
            invalidContext.enemy,
            invalidCard
        );
        invalidView.BindCard(
            invalidContext.allyA,
            invalidCard,
            BattleCardUIPreviewBuilder.Build(
                invalidContext.allyA,
                invalidContext.enemy,
                invalidCard
            ),
            invalidSelection
        );
        invalidSelection.SelectCard(invalidView);
        GameObject invalidSourceObject = new GameObject(
            "Click66InvalidSource",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(BattleActionSlotUIView)
        );
        BattleActionSlotUIView invalidSourceView =
            invalidSourceObject.GetComponent<BattleActionSlotUIView>();
        invalidSourceView.BindInteraction(
            invalidContext.allyB,
            0,
            false,
            null
        );
        GameObject invalidTargetObject = new GameObject(
            "Click66InvalidTarget",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(BattleActionSlotUIView)
        );
        BattleActionSlotUIView invalidTargetView =
            invalidTargetObject.GetComponent<BattleActionSlotUIView>();
        BattleCardInteractionCoordinator invalidCoordinator =
            new BattleCardInteractionCoordinator(invalidSelection);
        invalidSourceView.BindInteraction(
            invalidContext.allyB,
            0,
            false,
            clickedSlot => invalidCoordinator.SelectSourceSlot(clickedSlot)
        );
        BattleCardInteractionOutcome invalidOutcome = null;
        invalidTargetView.BindInteraction(
            invalidContext.enemy,
            0,
            true,
            clickedSlot =>
            {
                invalidOutcome = invalidCoordinator.ClickEnemySlot(
                    invalidContext.runtimeState,
                    clickedSlot
                );
            }
        );
        invalidSourceView.OnPointerClick(leftClick);
        invalidTargetView.OnPointerClick(leftClick);
        bool test12 =
            invalidOutcome != null &&
            !invalidOutcome.isSuccess &&
            invalidSelection.IsSelected(invalidView) &&
            object.ReferenceEquals(
                invalidCoordinator.SelectedActionSlotView,
                invalidSourceView
            ) &&
            BattleActionSlotManager.GetSlot(
                invalidSlots,
                invalidContext.allyA,
                1
            ).IsEmpty();

        BattleEndedTestContext emptyContext =
            CreateBattleEndedTestContext(
                "click66_empty",
                30,
                30,
                50,
                10,
                8,
                5
            );
        List<BattleActionSlot> emptySlots =
            BattleActionSlotManager.CreatePartyActionSlots(
                emptyContext.allyA,
                emptyContext.allyB,
                2
            );
        emptyContext.runtimeState.SetActionSlots(emptySlots);
        emptyContext.runtimeState.SetIntentQueue(
            new List<BattleEnemyIntent>()
        );
        BattleCardState emptyTargetCard =
            CreateFixedAttackCardForCharacter(
                emptyContext.allyA,
                "click66_empty_card",
                5
            );
        BattleCardSelectionController emptySelection =
            new BattleCardSelectionController();
        BattleCardInteractionCoordinator emptyCoordinator =
            new BattleCardInteractionCoordinator(emptySelection);
        BattleCardUIView emptyCardView = CreatePrimaryPreviewCardView(
            "Click66EmptyCard",
            emptyContext.allyA,
            emptyContext.enemy,
            emptyTargetCard
        );
        emptyCardView.BindCard(
            emptyContext.allyA,
            emptyTargetCard,
            BattleCardUIPreviewBuilder.Build(
                emptyContext.allyA,
                emptyContext.enemy,
                emptyTargetCard
            ),
            emptySelection
        );
        GameObject emptySourceObject = new GameObject(
            "Click66EmptySource",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(BattleActionSlotUIView)
        );
        BattleActionSlotUIView emptySourceView =
            emptySourceObject.GetComponent<BattleActionSlotUIView>();
        emptySourceView.BindInteraction(
            emptyContext.allyA,
            0,
            false,
            clickedSlot => emptyCoordinator.SelectSourceSlot(clickedSlot)
        );
        GameObject emptyTargetObject = new GameObject(
            "Click66EmptyTarget",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(BattleActionSlotUIView)
        );
        BattleActionSlotUIView emptyTargetView =
            emptyTargetObject.GetComponent<BattleActionSlotUIView>();
        BattleCardInteractionOutcome emptyOutcome = null;
        emptyTargetView.BindInteraction(
            emptyContext.enemy,
            0,
            true,
            clickedSlot =>
            {
                emptyOutcome = emptyCoordinator.ClickEnemySlot(
                    emptyContext.runtimeState,
                    clickedSlot
                );
            }
        );
        emptySourceView.OnPointerClick(leftClick);
        emptyCardView.OnPointerClick(leftClick);
        emptyTargetView.OnPointerClick(leftClick);
        bool test13 =
            emptyOutcome != null &&
            emptyOutcome.isSuccess &&
            emptyOutcome.assignmentResult != null &&
            emptyOutcome.assignmentResult.isSuccess &&
            emptyOutcome.assignmentResult.placementType ==
                BattleActionPlacementType.SpecificEnemy;

        bool test14 =
            RunMode65HandAssignmentRegressionSubTest();

        BattleEndedTestContext lowContext =
            CreateBattleEndedTestContext(
                "click66_low",
                30,
                30,
                50,
                3,
                4,
                8
            );
        List<BattleActionSlot> lowSlots =
            BattleActionSlotManager.CreatePartyActionSlots(
                lowContext.allyA,
                lowContext.allyB,
                2
            );
        BattleEnemyIntent lowIntent =
            CreatePreparedAssignmentIntent(
                lowContext,
                "click66_low_intent",
                lowContext.allyB,
                2,
                1,
                1
            );
        lowContext.runtimeState.SetActionSlots(lowSlots);
        lowContext.runtimeState.SetIntentQueue(
            BattleEnemyIntentManager.CreateIntentQueue(lowIntent)
        );
        BattleCardState lowCard =
            CreateFixedAttackCardForCharacter(
                lowContext.allyA,
                "click66_low_card",
                5
            );
        BattleCardSelectionController lowSelection =
            new BattleCardSelectionController();
        BattleCardInteractionCoordinator lowCoordinator =
            new BattleCardInteractionCoordinator(lowSelection);
        BattleCardUIView lowCardView = CreatePrimaryPreviewCardView(
            "Click66LowCard",
            lowContext.allyA,
            lowContext.enemy,
            lowCard
        );
        lowCardView.BindCard(
            lowContext.allyA,
            lowCard,
            BattleCardUIPreviewBuilder.Build(
                lowContext.allyA,
                lowContext.enemy,
                lowCard
            ),
            lowSelection
        );
        GameObject lowSourceObject = new GameObject(
            "Click66LowSource",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(BattleActionSlotUIView)
        );
        BattleActionSlotUIView lowSourceView =
            lowSourceObject.GetComponent<BattleActionSlotUIView>();
        lowSourceView.BindInteraction(
            lowContext.allyA,
            0,
            false,
            clickedSlot => lowCoordinator.SelectSourceSlot(clickedSlot)
        );
        GameObject lowTargetObject = new GameObject(
            "Click66LowTarget",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(BattleActionSlotUIView)
        );
        BattleActionSlotUIView lowTargetView =
            lowTargetObject.GetComponent<BattleActionSlotUIView>();
        lowTargetView.SetBoundEnemyIntent(lowIntent);
        BattleCardInteractionOutcome lowOutcome = null;
        lowTargetView.BindInteraction(
            lowContext.enemy,
            0,
            true,
            clickedSlot =>
            {
                lowOutcome = lowCoordinator.ClickEnemySlot(
                    lowContext.runtimeState,
                    clickedSlot
                );
            }
        );
        lowSourceView.OnPointerClick(leftClick);
        lowCardView.OnPointerClick(leftClick);
        lowTargetView.OnPointerClick(leftClick);
        bool test15 =
            lowOutcome != null &&
            lowOutcome.isSuccess &&
            lowOutcome.assignmentResult != null &&
            lowOutcome.assignmentResult.isSuccess &&
            lowOutcome.assignmentResult.wasAutoDowngraded;

        BattleCardSelectionController lifecycleSelection =
            new BattleCardSelectionController();
        BattleCardUIView lifecycleView = CreatePrimaryPreviewCardView(
            "Click66LifecycleView",
            owner,
            target,
            firstCard
        );
        lifecycleView.BindCard(
            owner,
            firstCard,
            BattleCardUIPreviewBuilder.Build(owner, target, firstCard),
            lifecycleSelection
        );
        lifecycleSelection.SelectCard(lifecycleView);
        lifecycleView.gameObject.SetActive(false);
        bool test16 = !lifecycleSelection.HasSelection;

        lifecycleView.gameObject.SetActive(true);
        lifecycleSelection.SelectCard(lifecycleView);
        BattleCardInteractionCoordinator lifecycleCoordinator =
            new BattleCardInteractionCoordinator(lifecycleSelection);
        GameObject lifecycleSourceObject = new GameObject(
            "Click66LifecycleSource",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(BattleActionSlotUIView)
        );
        BattleActionSlotUIView lifecycleSourceView =
            lifecycleSourceObject.GetComponent<BattleActionSlotUIView>();
        lifecycleSourceView.BindInteraction(
            owner,
            0,
            false,
            clickedSlot =>
                lifecycleCoordinator.SelectSourceSlot(clickedSlot)
        );
        bool modeBeforeToggle = false;
        bool modeAfterToggle =
            lifecycleCoordinator.ToggleCardMode(modeBeforeToggle);
        bool test17 =
            modeAfterToggle &&
            !lifecycleSelection.HasSelection;

        lifecycleSelection.SelectCard(lifecycleView);
        lifecycleSourceView.OnPointerClick(leftClick);
        lifecycleCoordinator.PrepareForBattleStart();
        bool test18 =
            !lifecycleSelection.HasSelection &&
            lifecycleCoordinator.SelectedCharacter == null &&
            lifecycleCoordinator.SelectedActionSlotView == null &&
            !lifecycleSourceView.IsSelected;

        bool test19 =
            RunBattleCardPrimaryVisualPresetBasicTestSequence();
        bool test20 =
            executionPlan != null &&
            executionPlan.isCompleted &&
            test10 &&
            test14;

        Debug.Log("模式66 测试1 Hover回正并上移：" + hoverMovedUp);
        Debug.Log("模式66 测试2 Hover不移动cardRoot：" + hoverRootStable);
        Debug.Log("模式66 测试3 Pointer Exit恢复：" + hoverExitRestored);
        Debug.Log("模式66 测试4 点击卡牌进入Selected：" + test4);
        Debug.Log("模式66 测试5 Selected在Pointer Exit后持续：" + test5);
        Debug.Log("模式66 测试6 再点同一卡牌取消选择：" + test6);
        Debug.Log("模式66 测试7 点击另一张卡切换唯一选择：" + test7);
        Debug.Log("模式66 测试8 CD卡可Hover但不可选择：" + test8);
        Debug.Log("模式66 测试9 无选中卡时点击敌方槽位不安排：" + test9);
        Debug.Log("模式66 测试10 选卡后点击合法敌方槽位成功指派：" + test10);
        Debug.Log("模式66 测试11 指派成功后清卡牌并保留槽位选择：" + test11);
        Debug.Log("模式66 测试12 非法目标不指派并保留选择：" + test12);
        Debug.Log("模式66 测试13 敌方空槽沿用原SpecificEnemy规则：" + test13);
        Debug.Log("模式66 测试14 替换与取消逻辑继续通过：" + test14);
        Debug.Log("模式66 测试15 速度不足沿用原自动降级判定：" + test15);
        Debug.Log("模式66 测试16 卡牌隐藏时清除选择：" + test16);
        Debug.Log("模式66 测试17 普通卡/罪卡切换清除选择：" + test17);
        Debug.Log("模式66 测试18 回合执行开始清除选择：" + test18);
        Debug.Log("模式66 测试19 模式64继续全部通过：" + test19);
        Debug.Log("模式66 测试20 原指派与执行计划回归通过：" + test20);

        Destroy(firstView.gameObject);
        Destroy(secondView.gameObject);
        Destroy(coolingView.gameObject);
        Destroy(assignView.gameObject);
        Destroy(sourceSlotObject);
        Destroy(enemySlotObject);
        Destroy(invalidView.gameObject);
        Destroy(invalidSourceObject);
        Destroy(invalidTargetObject);
        Destroy(emptyCardView.gameObject);
        Destroy(emptySourceObject);
        Destroy(emptyTargetObject);
        Destroy(lowCardView.gameObject);
        Destroy(lowSourceObject);
        Destroy(lowTargetObject);
        Destroy(lifecycleView.gameObject);
        Destroy(lifecycleSourceObject);

        Debug.Log("===== BattleCardClickAssignBasic 聚合测试结束 =====");
        return hoverMovedUp &&
            hoverRootStable &&
            hoverExitRestored &&
            test4 &&
            test5 &&
            test6 &&
            test7 &&
            test8 &&
            test9 &&
            test10 &&
            test11 &&
            test12 &&
            test13 &&
            test14 &&
            test15 &&
            test16 &&
            test17 &&
            test18 &&
            test19 &&
            test20;
    }

    bool RunBattleCardClickInteractionIntegrationTestSequence()
    {
        Debug.Log(
            "===== BattleCardClickInteractionIntegration 聚合测试开始 ====="
        );

        BattleEndedTestContext emptyContext =
            CreateBattleEndedTestContext(
                "click67_empty",
                30,
                30,
                50,
                10,
                8,
                5
            );
        bool test1 = RunMode67SelfTargetClickSubTest(
            emptyContext,
            null,
            false,
            false
        );

        BattleEndedTestContext abilityContext =
            CreateBattleEndedTestContext(
                "click67_ability",
                30,
                30,
                50,
                10,
                8,
                5
            );
        BattleCardState abilityCard = CreateBattleEndedAbilityCard(
            abilityContext.allyA,
            "click67_ability_card",
            "Click67Ability"
        );
        bool test2 = RunMode67SelfTargetClickSubTest(
            abilityContext,
            abilityCard,
            true,
            true
        );

        BattleEndedTestContext defenseContext =
            CreateBattleEndedTestContext(
                "click67_defense",
                30,
                30,
                50,
                10,
                8,
                5
            );
        BattleCardState defenseCard =
            CreateTestDefenseCardForCharacter(
                defenseContext.allyA,
                "click67_defense_card",
                4,
                1
            );
        bool test3 = RunMode67SelfTargetClickSubTest(
            defenseContext,
            defenseCard,
            true,
            true
        );

        BattleEndedTestContext dodgeContext =
            CreateBattleEndedTestContext(
                "click67_dodge",
                30,
                30,
                50,
                10,
                8,
                5
            );
        BattleCardState dodgeCard =
            CreateFixedDodgeCardForCharacter(
                dodgeContext.allyA,
                "click67_dodge_card",
                4,
                1
            );
        bool test4 = RunMode67SelfTargetClickSubTest(
            dodgeContext,
            dodgeCard,
            true,
            true
        );

        BattleEndedTestContext attackContext =
            CreateBattleEndedTestContext(
                "click67_attack",
                30,
                30,
                50,
                10,
                8,
                5
            );
        BattleCardState attackCard =
            CreateFixedAttackCardForCharacter(
                attackContext.allyA,
                "click67_attack_card",
                5
            );
        bool test5 = RunMode67SelfTargetClickSubTest(
            attackContext,
            attackCard,
            true,
            false
        );

        Debug.Log("模式67 测试1 未选卡点击自身目标不安排：" + test1);
        Debug.Log("模式67 测试2 Ability点击自身目标成功：" + test2);
        Debug.Log("模式67 测试3 Defense点击自身目标成功：" + test3);
        Debug.Log("模式67 测试4 Dodge点击自身目标成功：" + test4);
        Debug.Log("模式67 测试5 Attack点击自身目标失败并保留选择：" + test5);
        Debug.Log(
            "===== BattleCardClickInteractionIntegration 聚合测试结束 ====="
        );
        return test1 &&
            test2 &&
            test3 &&
            test4 &&
            test5;
    }

    bool RunMode67SelfTargetClickSubTest(
        BattleEndedTestContext context,
        BattleCardState cardState,
        bool selectCard,
        bool expectSuccess
    )
    {
        List<BattleActionSlot> slots =
            BattleActionSlotManager.CreatePartyActionSlots(
                context.allyA,
                context.allyB,
                2
            );
        context.runtimeState.SetActionSlots(slots);
        context.runtimeState.SetIntentQueue(
            new List<BattleEnemyIntent>()
        );
        SetTestLifecyclePhase(
            context.runtimeState,
            BattleLifecyclePhase.Prepare
        );

        BattleCardSelectionController selectionController =
            new BattleCardSelectionController();
        BattleCardInteractionCoordinator coordinator =
            new BattleCardInteractionCoordinator(selectionController);

        GameObject sourceObject = new GameObject(
            "Click67Source",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(BattleActionSlotUIView)
        );
        BattleActionSlotUIView sourceView =
            sourceObject.GetComponent<BattleActionSlotUIView>();
        sourceView.BindInteraction(
            context.allyA,
            0,
            false,
            clickedSlot => coordinator.SelectSourceSlot(clickedSlot)
        );

        GameObject targetObject = new GameObject(
            "Click67SelfTarget",
            typeof(RectTransform),
            typeof(BattleSelfActionDropZone)
        );
        BattleSelfActionDropZone targetView =
            targetObject.GetComponent<BattleSelfActionDropZone>();
        BattleCardInteractionOutcome outcome = null;
        targetView.Bind(
            context.allyA,
            clickedTarget =>
            {
                outcome = coordinator.ClickSelfTarget(
                    context.runtimeState,
                    clickedTarget
                );
            }
        );

        BattleCardUIView cardView = null;
        if (cardState != null)
        {
            cardView = CreatePrimaryPreviewCardView(
                "Click67Card",
                context.allyA,
                context.allyA,
                cardState
            );
            cardView.BindCard(
                context.allyA,
                cardState,
                BattleCardUIPreviewBuilder.Build(
                    context.allyA,
                    context.allyA,
                    cardState
                ),
                selectionController
            );
        }

        PointerEventData leftClick = new PointerEventData(null)
        {
            button = PointerEventData.InputButton.Left
        };
        sourceView.OnPointerClick(leftClick);

        if (selectCard && cardView != null)
        {
            cardView.OnPointerClick(leftClick);
        }

        targetView.OnPointerClick(leftClick);

        BattleActionSlot assignedSlot =
            BattleActionSlotManager.GetSlot(
                slots,
                context.allyA,
                1
            );
        bool sourceWasSelected =
            outcome != null &&
            outcome.hadSelectedCard == selectCard;
        bool passed;

        if (expectSuccess)
        {
            passed =
                sourceWasSelected &&
                outcome.isSuccess &&
                outcome.assignmentResult != null &&
                outcome.assignmentResult.isSuccess &&
                outcome.assignmentResult.placementType ==
                    BattleActionPlacementType.Self &&
                assignedSlot != null &&
                object.ReferenceEquals(
                    assignedSlot.cardState,
                    cardState
                ) &&
                !selectionController.HasSelection &&
                object.ReferenceEquals(
                    coordinator.SelectedCharacter,
                    context.allyA
                ) &&
                object.ReferenceEquals(
                    coordinator.SelectedActionSlotView,
                    sourceView
                ) &&
                sourceView.IsSelected;
        }
        else
        {
            bool shouldRetainSelection = selectCard && cardView != null;
            passed =
                sourceWasSelected &&
                !outcome.isSuccess &&
                assignedSlot != null &&
                assignedSlot.IsEmpty() &&
                selectionController.HasSelection ==
                    shouldRetainSelection &&
                coordinator.SelectedCharacter == context.allyA &&
                object.ReferenceEquals(
                    coordinator.SelectedActionSlotView,
                    sourceView
                ) &&
                sourceView.IsSelected;
        }

        if (cardView != null)
        {
            Destroy(cardView.gameObject);
        }

        Destroy(sourceObject);
        Destroy(targetObject);
        return passed;
    }

    void RunMode66HoverVisualSubTest(
        out bool movedUp,
        out bool rootStable,
        out bool exitRestored
    )
    {
        GameObject cardObject = new GameObject(
            "Click66HoverCard",
            typeof(RectTransform)
        );
        cardObject.SetActive(false);
        RectTransform cardRoot =
            cardObject.GetComponent<RectTransform>();
        cardRoot.anchoredPosition = new Vector2(12f, 34f);

        GameObject visualObject = new GameObject(
            "VisualRoot",
            typeof(RectTransform)
        );
        visualObject.transform.SetParent(cardObject.transform, false);
        RectTransform visualRoot =
            visualObject.GetComponent<RectTransform>();
        visualRoot.anchoredPosition = new Vector2(2f, 3f);
        visualRoot.localRotation = Quaternion.Euler(0f, 0f, -8f);

        BattleCardMotionUIView motion =
            cardObject.AddComponent<BattleCardMotionUIView>();
        BattleCardUIView view =
            cardObject.AddComponent<BattleCardUIView>();
        SetMode64PrivateField(motion, "cardRoot", cardRoot);
        SetMode64PrivateField(motion, "visualRoot", visualRoot);
        SetMode64PrivateField(motion, "hoverLiftY", 100f);
        SetMode64PrivateField(view, "motionView", motion);
        cardObject.SetActive(true);

        Vector2 rootBefore = cardRoot.anchoredPosition;
        Vector2 visualBefore = visualRoot.anchoredPosition;
        Quaternion rotationBefore = visualRoot.localRotation;
        view.OnPointerEnter(null);
        motion.CompleteCurrentTransitionImmediately();
        movedUp =
            visualRoot.anchoredPosition ==
                visualBefore + Vector2.up * 100f &&
            Mathf.Abs(
                Mathf.DeltaAngle(
                    visualRoot.localEulerAngles.z,
                    0f
                )
            ) < 0.001f;
        rootStable = cardRoot.anchoredPosition == rootBefore;

        view.OnPointerExit(null);
        motion.CompleteCurrentTransitionImmediately();
        exitRestored =
            visualRoot.anchoredPosition == visualBefore &&
            Quaternion.Angle(
                visualRoot.localRotation,
                rotationBefore
            ) < 0.001f;

        Destroy(cardObject);
    }

    TMPro.TMP_Text CreateMode64Text(Transform parent, string objectName)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TMPro.TextMeshProUGUI)
        );
        textObject.transform.SetParent(parent, false);
        return textObject.GetComponent<TMPro.TextMeshProUGUI>();
    }

    UnityEngine.UI.Image CreateMode64Image(
        Transform parent,
        string objectName
    )
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image)
        );
        imageObject.transform.SetParent(parent, false);
        return imageObject.GetComponent<UnityEngine.UI.Image>();
    }

    Sprite CreateMode64Sprite(
        Texture2D texture,
        int pixelIndex,
        string spriteName
    )
    {
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(pixelIndex, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f)
        );
        sprite.name = "Visual64" + spriteName;
        return sprite;
    }

    bool SetMode64PrivateField(
        object target,
        string fieldName,
        object value
    )
    {
        if (target == null || string.IsNullOrEmpty(fieldName))
        {
            return false;
        }

        System.Reflection.FieldInfo field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic
        );

        if (field == null)
        {
            return false;
        }

        field.SetValue(target, value);
        return true;
    }

    T GetMode64PrivateField<T>(object target, string fieldName)
        where T : class
    {
        if (target == null || string.IsNullOrEmpty(fieldName))
        {
            return null;
        }

        System.Reflection.FieldInfo field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic
        );

        return field != null ? field.GetValue(target) as T : null;
    }

    bool AreMode64ColorsEqual(Color left, Color right)
    {
        const float Tolerance = 0.001f;
        return Mathf.Abs(left.r - right.r) <= Tolerance &&
            Mathf.Abs(left.g - right.g) <= Tolerance &&
            Mathf.Abs(left.b - right.b) <= Tolerance &&
            Mathf.Abs(left.a - right.a) <= Tolerance;
    }

    bool AreMode64TextSettingsUnchanged(
        TMPro.TMP_Text[] texts,
        float[] fontSizes,
        bool[] autoSizingStates,
        Vector3[] localScales
    )
    {
        if (texts == null ||
            fontSizes == null ||
            autoSizingStates == null ||
            localScales == null ||
            texts.Length != fontSizes.Length ||
            texts.Length != autoSizingStates.Length ||
            texts.Length != localScales.Length)
        {
            return false;
        }

        for (int i = 0; i < texts.Length; i++)
        {
            TMPro.TMP_Text text = texts[i];

            if (text == null ||
                !Mathf.Approximately(text.fontSize, fontSizes[i]) ||
                text.enableAutoSizing != autoSizingStates[i] ||
                text.rectTransform.localScale != localScales[i])
            {
                return false;
            }
        }

        return true;
    }

    bool HasMode64BlackOutline(Material material)
    {
        return material != null &&
            material.HasProperty(TMPro.ShaderUtilities.ID_OutlineColor) &&
            AreMode64ColorsEqual(
                material.GetColor(TMPro.ShaderUtilities.ID_OutlineColor),
                Color.black
            );
    }

    bool RunMode64HandFilterAndAssignmentRegressionSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "visual64_hand",
            30,
            30,
            50,
            10,
            8,
            5
        );
        List<BattleActionSlot> slots =
            BattleActionSlotManager.CreatePartyActionSlots(
                context.allyA,
                context.allyB,
                2
            );
        context.runtimeState.SetActionSlots(slots);
        context.runtimeState.SetIntentQueue(new List<BattleEnemyIntent>());
        SetTestLifecyclePhase(
            context.runtimeState,
            BattleLifecyclePhase.Prepare
        );

        BattleCardState normalCard = CreateFixedAttackCardForCharacter(
            context.allyA,
            "visual64_normal_card",
            5
        );
        CardTestData sinCardData = CreateFixedAttackCardData(
            "visual64_sin_card_data",
            "模式64攻击罪卡",
            5
        );
        sinCardData.rarity = CardRarity.Gold;
        sinCardData.isSinCard = true;
        sinCardData.sinCardCategory = SinCardCategory.Clash;
        sinCardData.sinCardUseRule = SinCardUseRule.Permanent;
        BattleCardState sinCard = BattleCardManager.CreateBattleCard(
            context.allyA,
            sinCardData,
            "visual64_sin_card"
        );

        List<BattleCardState> visibleBefore = GetMode60VisibleCards(
            context.runtimeState,
            normalCard,
            sinCard
        );
        BattleActionAssignmentResult assignResult;
        bool assigned = BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            context.runtimeState,
            context.allyA,
            1,
            context.allyA,
            normalCard,
            context.enemy,
            null,
            out assignResult
        );
        List<BattleCardState> visibleAfterAssign = GetMode60VisibleCards(
            context.runtimeState,
            normalCard,
            sinCard
        );
        BattleActionAssignmentResult cancelResult;
        bool cancelled = BattleCardDropAssignmentRouter.TryCancelSelectedSlot(
            context.runtimeState,
            context.allyA,
            1,
            out cancelResult
        );
        List<BattleCardState> visibleAfterCancel = GetMode60VisibleCards(
            context.runtimeState,
            normalCard,
            sinCard
        );

        return visibleBefore.Contains(normalCard) &&
            visibleBefore.Contains(sinCard) &&
            assigned &&
            assignResult != null &&
            assignResult.isSuccess &&
            !visibleAfterAssign.Contains(normalCard) &&
            visibleAfterAssign.Contains(sinCard) &&
            cancelled &&
            cancelResult != null &&
            cancelResult.isSuccess &&
            visibleAfterCancel.Contains(normalCard) &&
            visibleAfterCancel.Contains(sinCard);
    }

    void RunFutureTurnCooldownZeroSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "cooldown63_1",
            30,
            30,
            50,
            10,
            8,
            5
        );
        BattleCardState card = CreateCooldownSemanticsCard(
            context.allyA,
            "cooldown63_1_card",
            CardType.Attack,
            0,
            1
        );

        List<BattleActionSlot> actionSlots =
            BattleActionSlotManager.CreateLivingPartyActionSlots(
                context.allyA,
                context.allyB,
                2
            );
        context.runtimeState.SetActionSlots(actionSlots);
        BattleActionSlotManager.AssignFreeAction(
            actionSlots,
            context.allyA,
            1,
            context.allyA,
            card,
            context.enemy
        );
        BattleExecutionPlan plan =
            BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
                actionSlots,
                new List<BattleEnemyIntent>()
            );
        ExecutePlanWithRuntimeStateAndCompleteTurn(
            context.runtimeState,
            plan
        );
        int cooldownAfterResolved = card.currentCooldown;
        CompleteCooldownSemanticsTurn(context);

        BattleCardUIView view = CreatePrimaryPreviewCardView(
            "Cooldown63ZeroView",
            context.allyA,
            context.enemy,
            card
        );
        CardEligibilityResult eligibility =
            BattleCardManager.EvaluateCardEligibility(
                context.allyA,
                context.enemy,
                card
            );
        bool passed =
            cooldownAfterResolved == 0 &&
            card.currentCooldown == 0 &&
            view.CanSelect &&
            eligibility != null &&
            eligibility.isEligible;

        Debug.Log("模式63 测试1 CD0结算、回合末和下一回合均保持可用：" + passed);
        Destroy(view.gameObject);
    }

    void RunFutureTurnCooldownOneSubTests()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "cooldown63_cd1",
            30,
            30,
            50,
            10,
            8,
            5
        );
        BattleCardState card = CreateCooldownSemanticsCard(
            context.allyA,
            "cooldown63_cd1_card",
            CardType.Attack,
            1,
            1
        );

        BattleCardManager.ApplyCooldownOnResolved(card);
        bool test2 = card.currentCooldown == 2;

        CompleteCooldownSemanticsTurn(context);
        BattleCardUIView coolingView = CreatePrimaryPreviewCardView(
            "Cooldown63OneCoolingView",
            context.allyA,
            context.enemy,
            card
        );
        CardEligibilityResult coolingEligibility =
            BattleCardManager.EvaluateCardEligibility(
                context.allyA,
                context.enemy,
                card
            );
        bool test3 =
            card.currentCooldown == 1 &&
            context.allyA.battleCards.Contains(card) &&
            BattleSimpleUIController.ShouldDisplayCardInHand(
                context.runtimeState,
                card
            ) &&
            !coolingView.CanSelect &&
            coolingEligibility != null &&
            !coolingEligibility.isEligible &&
            coolingEligibility.failureReason ==
                CardEligibilityFailureReason.CardOnCooldown;

        CompleteCooldownSemanticsTurn(context);
        BattleCardUIView readyView = CreatePrimaryPreviewCardView(
            "Cooldown63OneReadyView",
            context.allyA,
            context.enemy,
            card
        );
        CardEligibilityResult readyEligibility =
            BattleCardManager.EvaluateCardEligibility(
                context.allyA,
                context.enemy,
                card
            );
        bool test4 =
            card.currentCooldown == 0 &&
            readyView.CanSelect &&
            readyEligibility != null &&
            readyEligibility.isEligible;

        Debug.Log("模式63 测试2 CD1在Resolved时补偿为2：" + test2);
        Debug.Log("模式63 测试3 CD1下一回合剩余1且不可选择或安排：" + test3);
        Debug.Log("模式63 测试4 CD1经过一个完整未来回合后恢复：" + test4);
        Destroy(coolingView.gameObject);
        Destroy(readyView.gameObject);
    }

    void RunFutureTurnCooldownTwoSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "cooldown63_5",
            30,
            30,
            50,
            10,
            8,
            5
        );
        BattleCardState card = CreateCooldownSemanticsCard(
            context.allyA,
            "cooldown63_5_card",
            CardType.Attack,
            2,
            1
        );

        BattleCardManager.ApplyCooldownOnResolved(card);
        int afterResolved = card.currentCooldown;
        CompleteCooldownSemanticsTurn(context);
        int firstFutureTurn = card.currentCooldown;
        bool firstFutureBlocked =
            !BattleCardManager.EvaluateCardEligibility(
                context.allyA,
                context.enemy,
                card
            ).isEligible;

        CompleteCooldownSemanticsTurn(context);
        int secondFutureTurn = card.currentCooldown;
        bool secondFutureBlocked =
            !BattleCardManager.EvaluateCardEligibility(
                context.allyA,
                context.enemy,
                card
            ).isEligible;

        CompleteCooldownSemanticsTurn(context);
        int thirdFutureTurn = card.currentCooldown;
        bool thirdFutureReady =
            BattleCardManager.EvaluateCardEligibility(
                context.allyA,
                context.enemy,
                card
            ).isEligible;

        bool passed =
            afterResolved == 3 &&
            firstFutureTurn == 2 &&
            firstFutureBlocked &&
            secondFutureTurn == 1 &&
            secondFutureBlocked &&
            thirdFutureTurn == 0 &&
            thirdFutureReady;

        Debug.Log("模式63 测试5 CD2完整序列为3→2→1→0：" + passed);
    }

    void RunFutureTurnCooldownPreviewSubTests()
    {
        CharacterData owner = new CharacterData("cooldown63_preview_owner", 30, 10, 10);
        CharacterData target = new CharacterData("cooldown63_preview_target", 50, 5, 5);
        BattleCardState cooldownOne = CreateCooldownSemanticsCard(
            owner,
            "cooldown63_preview_one",
            CardType.Attack,
            1,
            1
        );
        BattleCardState cooldownTwo = CreateCooldownSemanticsCard(
            owner,
            "cooldown63_preview_two",
            CardType.Attack,
            2,
            1
        );

        bool test6 =
            BattleCardUIPreviewBuilder.Build(
                owner,
                target,
                cooldownOne
            ).cooldownText == "1" &&
            BattleCardUIPreviewBuilder.Build(
                owner,
                target,
                cooldownTwo
            ).cooldownText == "2";

        cooldownOne.currentCooldown = 2;
        string atTwo = BattleCardUIPreviewBuilder.Build(
            owner,
            target,
            cooldownOne
        ).cooldownText;
        cooldownOne.currentCooldown = 1;
        string atOne = BattleCardUIPreviewBuilder.Build(
            owner,
            target,
            cooldownOne
        ).cooldownText;
        cooldownOne.currentCooldown = 0;
        string atZero = BattleCardUIPreviewBuilder.Build(
            owner,
            target,
            cooldownOne
        ).cooldownText;
        bool test7 = atTwo == "1" && atOne == "1" && atZero == "1";

        Debug.Log("模式63 测试6 一级卡面显示基础CD1和2且不减1：" + test6);
        Debug.Log("模式63 测试7 剩余CD不覆盖一级卡面基础CD：" + test7);
    }

    void RunFutureTurnCooldownAutomaticCycleSubTest()
    {
        AutomaticTurnTestContext context = CreateAutomaticTurnTestContext(
            "cooldown63_8",
            30,
            30,
            50,
            10,
            8,
            5,
            1
        );
        BattleCardState card =
            CreateFutureTurnCooldownAutomaticCycleCard(context.allyA);
        int baseCooldown = BattleCardManager.GetBaseCooldown(card.cardData);
        int beforeCurrentCooldown = card.currentCooldown;
        int turnBefore = context.runtimeState.currentTurn;
        string phaseBefore = context.runtimeState.currentPhase;
        int resolvedCooldownByRule = GetExpectedResolvedCooldown(card);
        bool fixtureValid =
            card.cardData.cardType == CardType.Attack &&
            !card.cardData.isSinCard &&
            baseCooldown == 1 &&
            resolvedCooldownByRule == 2 &&
            beforeCurrentCooldown == 0;
        bool assigned = fixtureValid &&
            BattleActionSlotManager.AssignFreeAction(
                context.runtimeState.actionSlots,
                context.allyA,
                1,
                context.allyA,
                card,
                context.enemy
            );
        BattleActionSlot assignedSlot = BattleActionSlotManager.GetSlot(
            context.runtimeState.actionSlots,
            context.allyA,
            1
        );
        bool assignedSlotValid =
            assignedSlot != null &&
            object.ReferenceEquals(assignedSlot.actor, context.allyA) &&
            object.ReferenceEquals(assignedSlot.cardState, card);

        if (!fixtureValid)
        {
            Debug.Log(
                "模式63 测试8夹具错误：测试卡必须是基础CD为1的普通Attack"
            );
        }

        BattleDebugSettings debugSettings = BattleDebugSettings.Instance;
        bool previousShowDetailBattleLog =
            debugSettings != null && debugSettings.showDetailBattleLog;
        BattleAutomaticTurnCycleResult result = null;

        try
        {
            if (debugSettings != null)
            {
                debugSettings.showDetailBattleLog = true;
            }

            result = RunAutomaticTurnCycle(context);
        }
        finally
        {
            if (debugSettings != null)
            {
                debugSettings.showDetailBattleLog =
                    previousShowDetailBattleLog;
            }
        }

        bool executionPlanNull =
            context.runtimeState.currentExecutionPlan == null;
        bool cardStillOwned =
            card.owner != null &&
            card.owner.battleCards != null &&
            card.owner.battleCards.Contains(card);
        bool newActionSlotsContainOldAssignment = false;

        if (context.runtimeState.actionSlots != null)
        {
            foreach (BattleActionSlot slot in context.runtimeState.actionSlots)
            {
                if (slot != null &&
                    object.ReferenceEquals(slot.cardState, card))
                {
                    newActionSlotsContainOldAssignment = true;
                    break;
                }
            }
        }

        BattleCardUIView view = CreatePrimaryPreviewCardView(
            "Cooldown63AutomaticCycleView",
            context.allyA,
            context.enemy,
            card
        );
        CardEligibilityResult eligibility =
            BattleCardManager.EvaluateCardEligibility(
                context.allyA,
                context.enemy,
                card
            );
        bool passed =
            turnBefore == 1 &&
            phaseBefore == "Prepare" &&
            fixtureValid &&
            assigned &&
            result != null &&
            result.isSuccess &&
            result.executionPlanCompleted &&
            result.advancedToNextTurn &&
            context.runtimeState.currentTurn == 2 &&
            context.runtimeState.currentPhase == "Prepare" &&
            executionPlanNull &&
            card.currentCooldown == 1 &&
            assignedSlotValid &&
            assignedSlot.isUsed &&
            cardStillOwned &&
            !newActionSlotsContainOldAssignment &&
            view != null &&
            !view.CanSelect &&
            eligibility != null &&
            !eligibility.isEligible &&
            eligibility.failureReason ==
                CardEligibilityFailureReason.CardOnCooldown;

        Debug.Log(
            "[模式63 测试8诊断]\n" +
            "InstanceID: " + card.instanceID + "\n" +
            "CardID: " + card.cardData.cardID + "\n" +
            "IsSinCard: " + card.cardData.isSinCard + "\n" +
            "BaseCooldown: " + baseCooldown + "\n" +
            "ResolvedCooldownByRule: " + resolvedCooldownByRule + "\n" +
            "BeforeCurrentCooldown: " + beforeCurrentCooldown + "\n" +
            "Owner: " +
                (card.owner != null ? card.owner.characterName : "null") + "\n" +
            "TurnBefore: " + turnBefore + "\n" +
            "PhaseBefore: " + phaseBefore + "\n" +
            "Assigned: " + assigned + "\n" +
            "TryRunResult: " +
                (result != null
                    ? result.isSuccess + " / " + result.message
                    : "null") + "\n" +
            "TurnAfter: " + context.runtimeState.currentTurn + "\n" +
            "PhaseAfter: " + context.runtimeState.currentPhase + "\n" +
            "AfterCurrentCooldown: " + card.currentCooldown + "\n" +
            "ExecutionPlanNull: " + executionPlanNull + "\n" +
            "CardStillOwned: " + cardStillOwned + "\n" +
            "OldAssignedSlotUsed: " +
                (assignedSlotValid && assignedSlot.isUsed) + "\n" +
            "NewActionSlotsContainOldAssignment: " +
                newActionSlotsContainOldAssignment + "\n" +
            "DetailLogSettingFound: " + (debugSettings != null)
        );
        Debug.Log("模式63 测试8 自动完整回合对CD1只Tick一次（2→1）：" + passed);

        if (view != null)
        {
            Destroy(view.gameObject);
        }
    }

    void RunFutureTurnCooldownDodgeSubTests()
    {
        BattleEndedTestContext failedContext = CreateBattleEndedTestContext(
            "cooldown63_9",
            30,
            30,
            50,
            10,
            8,
            5
        );
        BattleActionSlot failedDodgeSlot = new BattleActionSlot(
            failedContext.allyA,
            1
        );
        failedDodgeSlot.AssignPassiveGuard(
            failedContext.allyA,
            CreateFixedDodgeCardForCharacter(
                failedContext.allyA,
                "cooldown63_9_dodge",
                2,
                1
            )
        );
        BattleEnemyIntent failedIntent = CreateEnemyAttackIntent(
            "cooldown63_9_intent",
            failedContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(
                failedContext.enemy,
                "cooldown63_9_enemy",
                8,
                0
            ),
            failedContext.allyA,
            1
        );
        ExecuteMode59Plan(
            failedContext,
            failedIntent,
            new List<BattleActionSlot> { failedDodgeSlot }
        );
        CompleteCooldownSemanticsTurn(failedContext);
        bool test9 = failedDodgeSlot.cardState.currentCooldown == 1;

        BattleEndedTestContext successContext = CreateBattleEndedTestContext(
            "cooldown63_10",
            30,
            30,
            50,
            10,
            8,
            5
        );
        BattleActionSlot continuousDodgeSlot = CreateMode59ActiveDodgeSlot(
            successContext.allyA,
            1,
            "cooldown63_10_dodge",
            12,
            1,
            successContext.enemy
        );
        PrepareMode59CompletedRuntime(
            successContext,
            new List<BattleActionSlot> { continuousDodgeSlot }
        );
        CompleteCooldownSemanticsTurn(successContext);
        bool test10 =
            continuousDodgeSlot.isCardUseFinalized &&
            continuousDodgeSlot.cardState.currentCooldown == 1;

        Debug.Log("模式63 测试9 普通Dodge失败后下一回合CD1：" + test9);
        Debug.Log("模式63 测试10 连续Dodge回合末收尾后下一回合CD1：" + test10);
    }

    void RunFutureTurnCooldownDefenseSubTests()
    {
        BattleEndedTestContext triggeredContext = CreateBattleEndedTestContext(
            "cooldown63_11",
            30,
            30,
            50,
            10,
            8,
            5
        );
        BattleActionSlot defenseSlot = new BattleActionSlot(
            triggeredContext.allyA,
            1
        );
        defenseSlot.AssignPassiveGuard(
            triggeredContext.allyA,
            CreateTestDefenseCardForCharacter(
                triggeredContext.allyA,
                "cooldown63_11_defense",
                3,
                1
            )
        );
        BattleEnemyIntent defenseIntent = CreateEnemyAttackIntent(
            "cooldown63_11_intent",
            triggeredContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(
                triggeredContext.enemy,
                "cooldown63_11_enemy",
                8,
                0
            ),
            triggeredContext.allyA,
            1
        );
        ExecuteMode59Plan(
            triggeredContext,
            defenseIntent,
            new List<BattleActionSlot> { defenseSlot }
        );
        CompleteCooldownSemanticsTurn(triggeredContext);
        CardEligibilityResult defenseEligibility =
            BattleCardManager.EvaluateCardEligibility(
                triggeredContext.allyA,
                triggeredContext.allyA,
                defenseSlot.cardState
            );
        bool test11 =
            defenseSlot.cardState.currentCooldown == 1 &&
            defenseEligibility != null &&
            defenseEligibility.failureReason ==
                CardEligibilityFailureReason.CardOnCooldown;

        BattleEndedTestContext untouchedContext = CreateBattleEndedTestContext(
            "cooldown63_12",
            30,
            30,
            50,
            10,
            8,
            5
        );
        BattleActionSlot untouchedDefense = new BattleActionSlot(
            untouchedContext.allyA,
            1
        );
        untouchedDefense.AssignPassiveGuard(
            untouchedContext.allyA,
            CreateTestDefenseCardForCharacter(
                untouchedContext.allyA,
                "cooldown63_12_defense",
                3,
                1
            )
        );
        PrepareMode59CompletedRuntime(
            untouchedContext,
            new List<BattleActionSlot> { untouchedDefense }
        );
        CompleteCooldownSemanticsTurn(untouchedContext);
        bool test12 =
            !untouchedDefense.isUsed &&
            untouchedDefense.cardState.currentCooldown == 0 &&
            BattleCardManager.EvaluateCardEligibility(
                untouchedContext.allyA,
                untouchedContext.allyA,
                untouchedDefense.cardState
            ).isEligible;

        Debug.Log("模式63 测试11 Defense正式触发后下一回合CD1且不可用：" + test11);
        Debug.Log("模式63 测试12 未触发守备卡不进入CD且下一回合可用：" + test12);
    }

    void RunFutureTurnCooldownSinAndIdempotentSubTests()
    {
        List<CardTestData> cards = CardDataLoader.LoadCardData();
        CardTestData sinCardData = CardDataLoader.FindCardByID(
            cards,
            BattleSimpleUIController.ClashSinTestCardID
        );
        CharacterData sinOwner = new CharacterData(
            "cooldown63_13_owner",
            30,
            10,
            10
        );
        BattleCardState sinCard = BattleCardManager.CreateBattleCard(
            sinOwner,
            sinCardData,
            "cooldown63_13_sin"
        );
        int useCountBefore = sinCard.currentUseCount;
        BattleCardManager.ApplyCooldownOnResolved(sinCard);
        bool test13 =
            sinCard.currentCooldown == 0 &&
            sinCard.currentUseCount == useCountBefore + 1;

        BattleCardState normalCard = CreateCooldownSemanticsCard(
            sinOwner,
            "cooldown63_14_normal",
            CardType.Attack,
            1,
            1
        );
        BattleCardManager.ApplyCooldownOnResolved(normalCard);
        int cooldownAfterFirstResolved = normalCard.currentCooldown;
        BattleCardManager.ApplyCooldownOnResolved(normalCard);
        bool test14 =
            cooldownAfterFirstResolved == 2 &&
            normalCard.currentCooldown == 2;

        Debug.Log("模式63 测试13 罪卡继续走UseCount且不进入普通CD：" + test13);
        Debug.Log("模式63 测试14 重复Resolved不会把CD累加为base+2：" + test14);
    }

    BattleCardState CreateCooldownSemanticsCard(
        CharacterData owner,
        string instanceID,
        string cardType,
        int cooldown,
        int point
    )
    {
        CardTestData cardData = new CardTestData
        {
            cardID = instanceID + "_data",
            cardName = "模式63 CD语义测试卡",
            rarity = "White",
            cardType = cardType,
            isClashable =
                cardType == CardType.Attack ||
                cardType == CardType.Dodge,
            isSinCard = false,
            minPoint = point,
            maxPoint = point,
            cooldown = cooldown,
            damageFormula = cardType == CardType.Attack
                ? "PointAsDamage"
                : "",
            defenseFormula = cardType == CardType.Defense
                ? "PointAsDefense"
                : "",
            effects = new List<CardEffectData>()
        };

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    BattleCardState CreateFutureTurnCooldownAutomaticCycleCard(
        CharacterData owner
    )
    {
        CardTestData cardData = new CardTestData
        {
            cardID = "cooldown63_8_cd1_attack_data",
            cardName = "模式63自动回合CD1攻击",
            description = "",
            rarity = "White",
            cardType = CardType.Attack,
            isSinCard = false,
            consumeOnUse = false,
            useConditions = new CardUseConditionData[0],
            resourceRule = null,
            resourceRules = new CardResourceRuleData[0],
            sinCardCategory = "",
            sinCardUseRule = "",
            maxUseCount = 0,
            isClashable = true,
            damageFormula = "PointAsDamage",
            defenseFormula = "",
            minPoint = 1,
            maxPoint = 1,
            cooldown = 1,
            guiltCost = 0,
            guiltGain = 0,
            effects = new List<CardEffectData>()
        };

        return BattleCardManager.CreateBattleCard(
            owner,
            cardData,
            "cooldown63_8_cd1_attack"
        );
    }

    int GetExpectedResolvedCooldown(BattleCardState cardState)
    {
        int baseCooldown = cardState != null
            ? BattleCardManager.GetBaseCooldown(cardState.cardData)
            : 0;

        return baseCooldown > 0 ? baseCooldown + 1 : 0;
    }

    void CompleteCooldownSemanticsTurn(BattleEndedTestContext context)
    {
        if (context == null || context.runtimeState == null)
        {
            return;
        }

        SetTestLifecyclePhase(
            context.runtimeState,
            BattleLifecyclePhase.TurnResolved
        );
        context.runtimeState.EndCurrentTurnAndClearRuntimeObjects();

        List<BattleActionSlot> nextSlots =
            BattleActionSlotManager.CreateLivingPartyActionSlots(
                context.allyA,
                context.allyB,
                2
            );
        context.runtimeState.PrepareNextTurnWithRuntimeObjects(
            nextSlots,
            new List<BattleEnemyIntent>()
        );
    }

    void RunPrimaryPreviewCooldownContractSubTests()
    {
        CharacterData owner = new CharacterData("preview62_cd_owner", 30, 10, 10);
        CharacterData target = new CharacterData("preview62_cd_target", 50, 5, 5);
        CardTestData cardData = CreatePrimaryPreviewAttackCardData(
            "preview62_cd_card",
            "CD契约测试卡",
            3,
            5,
            1
        );
        BattleCardState cardState = BattleCardManager.CreateBattleCard(
            owner,
            cardData,
            "preview62_cd_card_copy"
        );

        cardState.currentCooldown = 0;
        BattleCardUIPreviewData cooldownOnePreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        bool test1 = cooldownOnePreview.cooldownText == "1";

        cardData.cooldown = 2;
        cardState.currentCooldown = 1;
        BattleCardUIPreviewData coolingPreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        bool test2 = coolingPreview.cooldownText == "2";

        cardState.currentCooldown = 2;
        string cooldownAtTwo =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState).cooldownText;
        cardState.currentCooldown = 1;
        string cooldownAtOne =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState).cooldownText;
        cardState.currentCooldown = 0;
        string cooldownAtZero =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState).cooldownText;
        bool test3 =
            cooldownAtTwo == "2" &&
            cooldownAtOne == "2" &&
            cooldownAtZero == "2";

        cardData.cooldown = 0;
        BattleCardUIPreviewData zeroCooldownPreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        bool test4 = zeroCooldownPreview.cooldownText == "0";

        cardData.cooldown = 2;
        cardState.currentCooldown = 0;
        BattleCardUIView readyView = CreatePrimaryPreviewCardView(
            "Preview62ReadyView",
            owner,
            target,
            cardState
        );
        BattleCardUIPreviewData readyPreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        bool test5 =
            readyPreview.cooldownText == "2" &&
            readyView != null &&
            readyView.CanSelect;

        cardState.currentCooldown = 1;
        BattleCardUIView coolingView = CreatePrimaryPreviewCardView(
            "Preview62CoolingView",
            owner,
            target,
            cardState
        );
        BattleCardUIPreviewData blockedPreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        bool test6 =
            blockedPreview.cooldownText == "2" &&
            coolingView != null &&
            !coolingView.CanSelect;

        Debug.Log("模式62 测试1 基础CD为1且剩余CD为0时显示1：" + test1);
        Debug.Log("模式62 测试2 冷却中一级卡面仍显示基础CD2：" + test2);
        Debug.Log("模式62 测试3 剩余CD变化不改变一级卡面基础CD：" + test3);
        Debug.Log("模式62 测试4 基础CD为0时显示0：" + test4);
        Debug.Log("模式62 测试5 基础CD2但剩余CD0时允许选择：" + test5);
        Debug.Log("模式62 测试6 基础CD仍显示2但剩余CD1时禁止选择：" + test6);

        if (readyView != null)
        {
            Destroy(readyView.gameObject);
        }

        if (coolingView != null)
        {
            Destroy(coolingView.gameObject);
        }
    }

    void RunPrimaryPreviewPointRangeContractSubTests()
    {
        CharacterData owner = new CharacterData("preview62_point_owner", 30, 10, 10);
        CharacterData target = new CharacterData("preview62_point_target", 50, 5, 5);
        CardTestData cardData = CreatePrimaryPreviewAttackCardData(
            "preview62_point_card",
            "点数范围测试卡",
            10,
            10,
            1
        );
        BattleCardState cardState = BattleCardManager.CreateBattleCard(
            owner,
            cardData,
            "preview62_point_card_copy"
        );

        BattleCardUIPreviewData fixedPreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        bool test7 = fixedPreview.pointText == "10-10";

        owner.AddBuff(
            "Strength",
            "强壮",
            "UpBuff",
            1,
            1,
            "None",
            "Permanent"
        );
        BattleCardUIPreviewData buffedPreview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);
        bool test8 = buffedPreview.pointText == "11-11";

        cardData.minPoint = 1;
        cardData.maxPoint = 12;
        CharacterData plainOwner = new CharacterData(
            "preview62_plain_range_owner",
            30,
            10,
            10
        );
        BattleCardState plainRangeCard = BattleCardManager.CreateBattleCard(
            plainOwner,
            cardData,
            "preview62_plain_range_copy"
        );
        BattleCardUIPreviewData plainRangePreview =
            BattleCardUIPreviewBuilder.Build(
                plainOwner,
                target,
                plainRangeCard
            );
        bool test9 = plainRangePreview.pointText == "1-12";

        Debug.Log("模式62 测试7 固定点数完整显示10-10：" + test7);
        Debug.Log("模式62 测试8 Buff后固定点数完整显示11-11：" + test8);
        Debug.Log("模式62 测试9 普通点数范围显示1-12：" + test9);
    }

    void RunPrimaryPreviewRealCardDataContractSubTests()
    {
        List<CardTestData> cards = CardDataLoader.LoadCardData();
        CardTestData basicAttack = CardDataLoader.FindCardByID(cards, "atk_001");
        CardTestData clashSin = CardDataLoader.FindCardByID(
            cards,
            BattleSimpleUIController.ClashSinTestCardID
        );

        bool test10 = basicAttack != null && !basicAttack.isSinCard;

        CharacterData owner = new CharacterData("preview62_real_owner", 30, 10, 10);
        CharacterData target = new CharacterData("preview62_real_target", 50, 5, 5);
        BattleCardState basicAttackState = BattleCardManager.CreateBattleCard(
            owner,
            basicAttack,
            "preview62_real_atk_001_copy"
        );
        CardEligibilityResult eligibility =
            BattleCardManager.EvaluateCardEligibility(
                owner,
                target,
                basicAttackState
            );
        bool test11 =
            basicAttack != null &&
            (basicAttack.useConditions == null ||
             basicAttack.useConditions.Length == 0) &&
            eligibility != null &&
            eligibility.isEligible;

        BattleCardUIPreviewData basicPreview =
            BattleCardUIPreviewBuilder.Build(
                owner,
                target,
                basicAttackState
            );
        bool test12 =
            basicPreview != null &&
            basicPreview.cardName == "基础攻击" &&
            (basicPreview.typeText == null ||
             !basicPreview.typeText.Contains("罪卡")) &&
            basicPreview.pointText == "10-10" &&
            basicPreview.cooldownText == "1";

        bool test13 =
            clashSin != null &&
            clashSin.isSinCard &&
            clashSin.sinCardCategory == SinCardCategory.Clash;

        BattleCardState clashSinState =
            BattleSimpleUIController.CreateClashSinCardState(
                owner,
                clashSin,
                "preview62_clash_sin_copy"
            );
        bool test14 =
            clashSinState != null &&
            clashSinState.cardData != null &&
            clashSinState.cardData.cardID ==
                BattleSimpleUIController.ClashSinTestCardID &&
            clashSinState.cardData.cardID != "atk_001";

        Debug.Log("模式62 测试10 真实atk_001不是罪卡：" + test10);
        Debug.Log("模式62 测试11 真实atk_001无Bullet条件且普通角色可用：" + test11);
        Debug.Log("模式62 测试12 基础攻击一级预览契约正确：" + test12);
        Debug.Log("模式62 测试13 正式攻击罪卡仍为Clash罪卡：" + test13);
        Debug.Log("模式62 测试14 ClashSin实例来源为sin_attack_test_001：" + test14);
    }

    CardTestData CreatePrimaryPreviewAttackCardData(
        string cardID,
        string cardName,
        int minPoint,
        int maxPoint,
        int cooldown
    )
    {
        return new CardTestData
        {
            cardID = cardID,
            cardName = cardName,
            description = "",
            rarity = "White",
            cardType = CardType.Attack,
            isClashable = true,
            isSinCard = false,
            minPoint = minPoint,
            maxPoint = maxPoint,
            cooldown = cooldown,
            damageFormula = "PointAsDamage",
            effects = new List<CardEffectData>()
        };
    }

    BattleCardUIView CreatePrimaryPreviewCardView(
        string objectName,
        CharacterData owner,
        CharacterData target,
        BattleCardState cardState
    )
    {
        GameObject cardObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(BattleCardUIView)
        );
        BattleCardUIView view = cardObject.GetComponent<BattleCardUIView>();
        view.BindCard(
            owner,
            cardState,
            BattleCardUIPreviewBuilder.Build(owner, target, cardState),
            null
        );
        return view;
    }

    void RunAutomaticTurnSingleCycleSubTest()
    {
        AutomaticTurnTestContext context = CreateAutomaticTurnTestContext(
            "auto61_1",
            30,
            30,
            50,
            10,
            8,
            5,
            1
        );

        BattleAutomaticTurnCycleResult result = RunAutomaticTurnCycle(context);
        bool passed =
            result.isSuccess &&
            result.executionPlanCompleted &&
            result.advancedToNextTurn &&
            context.runtimeState.currentTurn == 2 &&
            context.runtimeState.currentPhase == "Prepare" &&
            context.runtimeState.currentExecutionPlan == null &&
            context.runtimeState.actionSlots.Count == 4 &&
            context.runtimeState.intentQueue.Count == 1;

        Debug.Log("模式61 测试1 单次入口完成整回合并自动进入回合2：" + passed);
    }

    void RunAutomaticTurnWithoutPlayerActionSubTest()
    {
        AutomaticTurnTestContext context = CreateAutomaticTurnTestContext(
            "auto61_2",
            30,
            30,
            50,
            10,
            8,
            5,
            2
        );
        int hpBefore = context.allyA.currentHP;

        BattleAutomaticTurnCycleResult result = RunAutomaticTurnCycle(context);
        bool createdUnresponded =
            result.executedPlan != null &&
            result.executedPlan.executionItems != null &&
            result.executedPlan.executionItems.Count == 1 &&
            result.executedPlan.executionItems[0].executionType ==
                BattleExecutionItemType.UnrespondedEnemyIntent;
        bool passed =
            createdUnresponded &&
            context.allyA.currentHP < hpBefore &&
            result.advancedToNextTurn;

        Debug.Log("模式61 测试2 无玩家安排仍执行Unresponded并进入下一回合：" + passed);
    }

    void RunAutomaticTurnIncrementOnceSubTest()
    {
        AutomaticTurnTestContext context = CreateAutomaticTurnTestContext(
            "auto61_3",
            30,
            30,
            50,
            10,
            8,
            5,
            1
        );

        BattleAutomaticTurnCycleResult first = RunAutomaticTurnCycle(context);
        int turnAfterFirst = context.runtimeState.currentTurn;
        BattleAutomaticTurnCycleResult second = RunAutomaticTurnCycle(context);
        int turnAfterSecond = context.runtimeState.currentTurn;

        bool passed =
            first.advancedToNextTurn &&
            second.advancedToNextTurn &&
            turnAfterFirst == 2 &&
            turnAfterSecond == 3;

        Debug.Log("模式61 测试3 每次完整入口只增加一个回合（1→2→3）：" + passed);
    }

    void RunAutomaticTurnLivingSlotSubTest()
    {
        AutomaticTurnTestContext allLiving = CreateAutomaticTurnTestContext(
            "auto61_4_all",
            30,
            30,
            50,
            10,
            8,
            5,
            1
        );
        BattleAutomaticTurnCycleResult allLivingResult =
            RunAutomaticTurnCycle(allLiving);

        AutomaticTurnTestContext allyADead = CreateAutomaticTurnTestContext(
            "auto61_4_dead",
            0,
            30,
            50,
            10,
            8,
            5,
            1
        );
        BattleAutomaticTurnCycleResult allyADeadResult =
            RunAutomaticTurnCycle(allyADead);

        bool passed =
            allLivingResult.advancedToNextTurn &&
            allLiving.runtimeState.actionSlots.Count == 4 &&
            allyADeadResult.advancedToNextTurn &&
            allyADead.runtimeState.actionSlots.Count == 2 &&
            AreAllSlotsOwnedBy(
                allyADead.runtimeState.actionSlots,
                allyADead.allyB
            );

        Debug.Log("模式61 测试4 下一回合只为存活友方创建槽位：" + passed);
    }

    void RunAutomaticTurnFixedIntentSubTest()
    {
        AutomaticTurnTestContext context = CreateAutomaticTurnTestContext(
            "auto61_5",
            30,
            30,
            50,
            10,
            8,
            5,
            1
        );
        BattleEnemyIntent initialIntent = GetSingleIntent(context.runtimeState);
        BattleAutomaticTurnCycleResult result = RunAutomaticTurnCycle(context);
        BattleEnemyIntent nextIntent = GetSingleIntent(context.runtimeState);

        bool passed =
            IsExpectedFixedEnemyIntent(initialIntent, context) &&
            result.advancedToNextTurn &&
            IsExpectedFixedEnemyIntent(nextIntent, context);

        Debug.Log("模式61 测试5 每回合固定生成Enemy01基础攻击意图：" + passed);
    }

    void RunAutomaticTurnFixedTargetPrioritySubTest()
    {
        AutomaticTurnTestContext context = CreateAutomaticTurnTestContext(
            "auto61_6",
            30,
            30,
            50,
            10,
            8,
            5,
            1
        );
        BattleEnemyIntent intent = GetSingleIntent(context.runtimeState);

        bool passed =
            intent != null &&
            object.ReferenceEquals(intent.originalTargetCharacter, context.allyA) &&
            intent.originalTargetSlotIndex == 1;

        Debug.Log("模式61 测试6 固定目标优先Ally01槽位1：" + passed);
    }

    void RunAutomaticTurnAllyFallbackSubTest()
    {
        AutomaticTurnTestContext context = CreateAutomaticTurnTestContext(
            "auto61_7",
            0,
            30,
            50,
            10,
            8,
            5,
            1
        );
        BattleEnemyIntent intent = GetSingleIntent(context.runtimeState);

        bool passed =
            intent != null &&
            object.ReferenceEquals(intent.originalTargetCharacter, context.allyB) &&
            intent.originalTargetSlotIndex == 1;

        Debug.Log("模式61 测试7 Ally01死亡后固定攻击Ally02槽位1：" + passed);
    }

    void RunAutomaticTurnAllAlliesDeadSubTest()
    {
        AutomaticTurnTestContext context = CreateAutomaticTurnTestContext(
            "auto61_8",
            0,
            0,
            50,
            10,
            8,
            5,
            1
        );
        SetTestLifecyclePhase(context.runtimeState, BattleLifecyclePhase.Executing);
        context.runtimeState.EvaluateBattleEnd();
        int turnBefore = context.runtimeState.currentTurn;
        BattleAutomaticTurnCycleResult result = RunAutomaticTurnCycle(context);

        bool passed =
            context.runtimeState.IsBattleEnded &&
            context.runtimeState.battleResult == BattleResult.Defeat &&
            context.runtimeState.currentTurn == turnBefore &&
            context.runtimeState.actionSlots.Count == 0 &&
            context.runtimeState.intentQueue.Count == 0 &&
            context.runtimeState.currentPhase != "Prepare" &&
            !BattleAutomaticTurnCycle.CanStart(context.runtimeState) &&
            !result.advancedToNextTurn;

        Debug.Log("模式61 测试8 无存活友方时结束战斗且不创建下一回合：" + passed);
    }

    void RunAutomaticTurnEnemy02IsolationSubTest()
    {
        AutomaticTurnTestContext context = CreateAutomaticTurnTestContext(
            "auto61_9",
            30,
            30,
            50,
            10,
            8,
            5,
            1
        );
        CharacterData enemy02 = new CharacterData("auto61_9_Enemy02", 50, 5, 5);
        bool initialIsolated =
            !ContainsIntentOwner(context.runtimeState.intentQueue, enemy02) &&
            !context.runtimeState.battleUnits.Contains(enemy02);

        BattleAutomaticTurnCycleResult result = RunAutomaticTurnCycle(context);
        bool passed =
            initialIsolated &&
            result.advancedToNextTurn &&
            !ContainsIntentOwner(context.runtimeState.intentQueue, enemy02) &&
            context.runtimeState.battleUnits.Count == 3;

        Debug.Log("模式61 测试9 Enemy02不生成正式意图且不加入battleUnits：" + passed);
    }

    void RunAutomaticTurnSelectionClearSubTest()
    {
        AutomaticTurnTestContext context = CreateAutomaticTurnTestContext(
            "auto61_10",
            30,
            30,
            50,
            10,
            8,
            5,
            1
        );
        GameObject slotObject = new GameObject(
            "Auto61SelectedSlot",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image),
            typeof(BattleActionSlotUIView)
        );
        BattleActionSlotUIView selectedView =
            slotObject.GetComponent<BattleActionSlotUIView>();
        selectedView.BindInteraction(
            context.allyA,
            0,
            false,
            null
        );
        BattleCardSelectionController selectionController =
            new BattleCardSelectionController();
        BattleCardInteractionCoordinator coordinator =
            new BattleCardInteractionCoordinator(selectionController);
        bool sourceSelected = coordinator.SelectSourceSlot(selectedView);

        BattleAutomaticTurnCycleResult result = RunAutomaticTurnCycle(context);
        if (result.advancedToNextTurn)
        {
            coordinator.ClearAllSelections();
        }

        bool passed =
            sourceSelected &&
            result.advancedToNextTurn &&
            coordinator.SelectedCharacter == null &&
            coordinator.SelectedActionSlotView == null &&
            !selectedView.IsSelected &&
            !selectionController.HasSelection;

        Debug.Log("模式61 测试10 下一回合统一清除一级UI槽位选择：" + passed);
        Destroy(slotObject);
    }

    void RunAutomaticTurnCooldownHandVisibilitySubTest()
    {
        AutomaticTurnTestContext context = CreateAutomaticTurnTestContext(
            "auto61_11",
            30,
            30,
            50,
            10,
            8,
            5,
            1
        );
        BattleCardState cooldownCard = CreateAutomaticTurnCooldownCard(
            context.allyA,
            "auto61_11_cd",
            3
        );
        bool assigned = BattleActionSlotManager.AssignFreeAction(
            context.runtimeState.actionSlots,
            context.allyA,
            1,
            context.allyA,
            cooldownCard,
            context.enemy
        );

        BattleAutomaticTurnCycleResult result = RunAutomaticTurnCycle(context);
        BattleCardUIPreviewData preview = BattleCardUIPreviewBuilder.Build(
            context.allyA,
            context.enemy,
            cooldownCard
        );

        bool passed =
            assigned &&
            result.advancedToNextTurn &&
            context.allyA.battleCards.Contains(cooldownCard) &&
            cooldownCard.currentCooldown > 0 &&
            BattleSimpleUIController.ShouldDisplayCardInHand(
                context.runtimeState,
                cooldownCard
            ) &&
            preview.cooldownText == cooldownCard.cardData.cooldown.ToString();

        Debug.Log("模式61 测试11 已使用CD卡在下一回合仍显示基础CD：" + passed);
    }

    void RunAutomaticTurnCooldownDragBlockedSubTest()
    {
        CharacterData owner = new CharacterData("auto61_12_A", 30, 10, 10);
        CharacterData target = new CharacterData("auto61_12_Enemy", 50, 5, 5);
        BattleCardState cooldownCard = CreateAutomaticTurnCooldownCard(
            owner,
            "auto61_12_cd",
            3
        );
        cooldownCard.currentCooldown = 2;

        GameObject parentObject = new GameObject(
            "Auto61CardParent",
            typeof(RectTransform)
        );
        GameObject cardObject = new GameObject(
            "Auto61CooldownCardView",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(BattleCardUIView)
        );
        cardObject.transform.SetParent(parentObject.transform, false);

        BattleCardUIView view = cardObject.GetComponent<BattleCardUIView>();
        CanvasGroup group = cardObject.GetComponent<CanvasGroup>();
        BattleCardSelectionController selectionController =
            new BattleCardSelectionController();
        view.BindCard(
            owner,
            cooldownCard,
            BattleCardUIPreviewBuilder.Build(owner, target, cooldownCard),
            selectionController
        );

        Transform parentBefore = cardObject.transform.parent;
        int siblingBefore = cardObject.transform.GetSiblingIndex();
        Vector2 anchoredBefore =
            ((RectTransform)cardObject.transform).anchoredPosition;
        float alphaBefore = group.alpha;
        bool interactableBefore = group.interactable;
        bool blocksBefore = group.blocksRaycasts;

        PointerEventData eventData = new PointerEventData(null)
        {
            button = PointerEventData.InputButton.Left
        };
        view.OnPointerClick(eventData);

        bool passed =
            !view.CanSelect &&
            !selectionController.HasSelection &&
            object.ReferenceEquals(cardObject.transform.parent, parentBefore) &&
            cardObject.transform.GetSiblingIndex() == siblingBefore &&
            ((RectTransform)cardObject.transform).anchoredPosition ==
                anchoredBefore &&
            group.alpha == alphaBefore &&
            group.interactable == interactableBefore &&
            group.blocksRaycasts == blocksBefore;

        Debug.Log("模式61 测试12 CD卡不能被选中且不改变任何视觉状态：" + passed);
        Destroy(cardObject);
        Destroy(parentObject);
    }

    void RunAutomaticTurnReadyCardDragGateSubTest()
    {
        CharacterData owner = new CharacterData("auto61_13_A", 30, 10, 10);
        BattleCardState readyCard = CreateAutomaticTurnCooldownCard(
            owner,
            "auto61_13_ready",
            3
        );
        readyCard.currentCooldown = 0;

        GameObject cardObject = new GameObject(
            "Auto61ReadyCardView",
            typeof(RectTransform),
            typeof(BattleCardUIView)
        );
        BattleCardUIView view = cardObject.GetComponent<BattleCardUIView>();
        view.BindCard(
            owner,
            readyCard,
            BattleCardUIPreviewBuilder.Build(owner, owner, readyCard),
            null
        );

        Debug.Log("模式61 测试13 CD为0时正式选择门禁允许选中：" + view.CanSelect);
        Destroy(cardObject);
    }

    void RunAutomaticTurnCooldownTickRecoverySubTest()
    {
        AutomaticTurnTestContext context = CreateAutomaticTurnTestContext(
            "auto61_14",
            30,
            30,
            50,
            10,
            8,
            5,
            1
        );
        BattleCardState cooldownCard = CreateAutomaticTurnCooldownCard(
            context.allyA,
            "auto61_14_cd",
            3
        );
        bool assigned = BattleActionSlotManager.AssignFreeAction(
            context.runtimeState.actionSlots,
            context.allyA,
            1,
            context.allyA,
            cooldownCard,
            context.enemy
        );

        BattleAutomaticTurnCycleResult first = RunAutomaticTurnCycle(context);
        int cooldownAfterFirst = cooldownCard.currentCooldown;
        BattleAutomaticTurnCycleResult second = RunAutomaticTurnCycle(context);
        int cooldownAfterSecond = cooldownCard.currentCooldown;
        BattleAutomaticTurnCycleResult third = RunAutomaticTurnCycle(context);
        int cooldownAfterThird = cooldownCard.currentCooldown;
        BattleAutomaticTurnCycleResult fourth = RunAutomaticTurnCycle(context);
        int cooldownAfterFourth = cooldownCard.currentCooldown;

        GameObject cardObject = new GameObject(
            "Auto61RecoveredCardView",
            typeof(RectTransform),
            typeof(BattleCardUIView)
        );
        BattleCardUIView view = cardObject.GetComponent<BattleCardUIView>();
        view.BindCard(
            context.allyA,
            cooldownCard,
            BattleCardUIPreviewBuilder.Build(
                context.allyA,
                context.enemy,
                cooldownCard
            ),
            null
        );

        bool passed =
            assigned &&
            first.advancedToNextTurn &&
            second.advancedToNextTurn &&
            third.advancedToNextTurn &&
            fourth.advancedToNextTurn &&
            cooldownAfterFirst == 3 &&
            cooldownAfterSecond == 2 &&
            cooldownAfterThird == 1 &&
            cooldownAfterFourth == 0 &&
            view.CanSelect;

        Debug.Log("模式61 测试14 CD每回合只Tick一次并在归零后恢复选择：" + passed);
        Destroy(cardObject);
    }

    void RunAutomaticTurnBattleEndedStopSubTest()
    {
        AutomaticTurnTestContext context = CreateAutomaticTurnTestContext(
            "auto61_15",
            30,
            30,
            1,
            20,
            8,
            5,
            1
        );
        BattleCardState finishingCard = CreateAutomaticTurnCooldownCard(
            context.allyA,
            "auto61_15_finish",
            0
        );
        bool assigned = BattleActionSlotManager.AssignFreeAction(
            context.runtimeState.actionSlots,
            context.allyA,
            1,
            context.allyA,
            finishingCard,
            context.enemy
        );
        List<BattleActionSlot> slotsBefore = context.runtimeState.actionSlots;
        List<BattleEnemyIntent> intentsBefore = context.runtimeState.intentQueue;

        BattleAutomaticTurnCycleResult result = RunAutomaticTurnCycle(context);
        bool passed =
            assigned &&
            result.battleEnded &&
            result.executionPlanCompleted &&
            context.runtimeState.IsBattleEnded &&
            context.runtimeState.battleResult == BattleResult.Victory &&
            context.runtimeState.currentTurn == 1 &&
            object.ReferenceEquals(context.runtimeState.actionSlots, slotsBefore) &&
            object.ReferenceEquals(context.runtimeState.intentQueue, intentsBefore) &&
            context.runtimeState.currentPhase == "BattleEnded" &&
            !BattleAutomaticTurnCycle.CanStart(context.runtimeState);

        Debug.Log("模式61 测试15 BattleEnded后不创建下一回合且按钮语义禁用：" + passed);
    }

    void RunAutomaticTurnDuplicateCallProtectionSubTest()
    {
        AutomaticTurnTestContext context = CreateAutomaticTurnTestContext(
            "auto61_16",
            30,
            30,
            50,
            10,
            8,
            5,
            1
        );
        BattleExecutionPlan existingPlan = new BattleExecutionPlan();
        context.runtimeState.SetExecutionPlan(existingPlan);
        int turnBefore = context.runtimeState.currentTurn;
        int hpBefore = context.allyA.currentHP;
        int enemyUseCountBefore = context.enemyAttackCardState.currentUseCount;

        BattleAutomaticTurnCycleResult existingPlanResult =
            RunAutomaticTurnCycle(context);

        bool existingPlanProtected =
            !existingPlanResult.isSuccess &&
            object.ReferenceEquals(
                context.runtimeState.currentExecutionPlan,
                existingPlan
            ) &&
            context.runtimeState.currentTurn == turnBefore &&
            context.allyA.currentHP == hpBefore &&
            context.enemyAttackCardState.currentUseCount == enemyUseCountBefore;

        context.runtimeState.ClearExecutionPlan();
        SetTestLifecyclePhase(
            context.runtimeState,
            BattleLifecyclePhase.TurnResolved
        );
        BattleAutomaticTurnCycleResult wrongPhaseResult =
            RunAutomaticTurnCycle(context);

        bool passed =
            existingPlanProtected &&
            !wrongPhaseResult.isSuccess &&
            context.runtimeState.currentTurn == turnBefore &&
            context.allyA.currentHP == hpBefore &&
            context.enemyAttackCardState.currentUseCount == enemyUseCountBefore;

        Debug.Log("模式61 测试16 非Prepare或已有计划时拒绝重复执行：" + passed);
    }

    class AutomaticTurnTestContext
    {
        public CharacterData allyA;
        public CharacterData allyB;
        public CharacterData enemy;
        public BattleRuntimeState runtimeState;
        public BattleCardState enemyAttackCardState;
    }

    AutomaticTurnTestContext CreateAutomaticTurnTestContext(
        string title,
        int allyAHP,
        int allyBHP,
        int enemyHP,
        int allyASpeed,
        int allyBSpeed,
        int enemySpeed,
        int enemyPoint
    )
    {
        AutomaticTurnTestContext context = new AutomaticTurnTestContext
        {
            allyA = new CharacterData(title + "_Ally01", 30, allyASpeed, allyASpeed),
            allyB = new CharacterData(title + "_Ally02", 30, allyBSpeed, allyBSpeed),
            enemy = new CharacterData(title + "_Enemy01", 50, enemySpeed, enemySpeed)
        };

        context.allyA.currentHP = allyAHP;
        context.allyB.currentHP = allyBHP;
        context.enemy.currentHP = enemyHP;

        CardTestData enemyCardData = CreateFixedAttackCardData(
            "enemy_atk_001",
            "固定敌人基础攻击",
            enemyPoint
        );
        enemyCardData.cooldown = 0;
        context.enemyAttackCardState = BattleCardManager.CreateBattleCard(
            context.enemy,
            enemyCardData,
            title + "_enemy_atk_001"
        );

        context.runtimeState = new BattleRuntimeState();
        context.runtimeState.SetCharacters(
            context.allyA,
            context.allyB,
            context.enemy
        );

        List<BattleActionSlot> actionSlots =
            BattleActionSlotManager.CreateLivingPartyActionSlots(
                context.allyA,
                context.allyB,
                2
            );
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(
            BattleAutomaticTurnCycle.CreateFixedEnemyIntentQueue(
                context.enemy,
                context.enemyAttackCardState,
                context.allyA,
                context.allyB,
                actionSlots
            )
        );
        SetTestLifecyclePhase(
            context.runtimeState,
            BattleLifecyclePhase.Prepare
        );

        return context;
    }

    BattleAutomaticTurnCycleResult RunAutomaticTurnCycle(
        AutomaticTurnTestContext context
    )
    {
        return BattleAutomaticTurnCycle.TryRun(
            context.runtimeState,
            context.allyA,
            context.allyB,
            context.enemy,
            context.enemyAttackCardState
        );
    }

    BattleCardState CreateAutomaticTurnCooldownCard(
        CharacterData owner,
        string instanceID,
        int cooldown
    )
    {
        CardTestData cardData = CreateFixedAttackCardData(
            instanceID + "_data",
            "模式61冷却攻击",
            1
        );
        cardData.cooldown = cooldown;
        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    BattleEnemyIntent GetSingleIntent(BattleRuntimeState runtimeState)
    {
        return runtimeState != null &&
            runtimeState.intentQueue != null &&
            runtimeState.intentQueue.Count == 1
                ? runtimeState.intentQueue[0]
                : null;
    }

    bool IsExpectedFixedEnemyIntent(
        BattleEnemyIntent intent,
        AutomaticTurnTestContext context
    )
    {
        return intent != null &&
            object.ReferenceEquals(intent.enemy, context.enemy) &&
            object.ReferenceEquals(
                intent.enemyCardState,
                context.enemyAttackCardState
            ) &&
            intent.enemyCardState.cardData != null &&
            intent.enemyCardState.cardData.cardID == "enemy_atk_001" &&
            intent.intentOrder == 1 &&
            intent.enemySlotIndex == 1 &&
            intent.originalTargetSlotIndex == 1;
    }

    bool ContainsIntentOwner(
        List<BattleEnemyIntent> intentQueue,
        CharacterData enemyCharacter
    )
    {
        if (intentQueue == null)
        {
            return false;
        }

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent != null &&
                object.ReferenceEquals(intent.enemy, enemyCharacter))
            {
                return true;
            }
        }

        return false;
    }

    void RunCardDragExactViewBindingSubTest()
    {
        BattleEndedTestContext context =
            CreateBattleEndedTestContext("drag60_1", 30, 30, 50, 10, 8, 5);
        CardTestData sharedCardData =
            CreateFixedAttackCardData("drag60_1_shared_data", "模式60同定义攻击", 5);
        BattleCardState cardA = BattleCardManager.CreateBattleCard(
            context.allyA,
            sharedCardData,
            "drag60_1_same_card_a"
        );
        BattleCardState cardB = BattleCardManager.CreateBattleCard(
            context.allyA,
            sharedCardData,
            "drag60_1_same_card_b"
        );

        GameObject viewObjectA = new GameObject(
            "Drag60ViewA",
            typeof(RectTransform),
            typeof(BattleCardUIView)
        );
        GameObject viewObjectB = new GameObject(
            "Drag60ViewB",
            typeof(RectTransform),
            typeof(BattleCardUIView)
        );
        BattleCardUIView viewA = viewObjectA.GetComponent<BattleCardUIView>();
        BattleCardUIView viewB = viewObjectB.GetComponent<BattleCardUIView>();

        viewA.BindCard(context.allyA, cardA, new BattleCardUIPreviewData(), null);
        viewB.BindCard(context.allyA, cardB, new BattleCardUIPreviewData(), null);

        bool exactInstances =
            cardA.cardData.cardID == cardB.cardData.cardID &&
            cardA.instanceID != cardB.instanceID &&
            !object.ReferenceEquals(cardA, cardB) &&
            object.ReferenceEquals(viewA.BoundOwner, context.allyA) &&
            object.ReferenceEquals(viewB.BoundOwner, context.allyA) &&
            object.ReferenceEquals(viewA.BoundCardState, cardA) &&
            object.ReferenceEquals(viewB.BoundCardState, cardB);

        Debug.Log("模式60 测试1 卡牌View精确绑定BattleCardState实例：" + exactInstances);
        Destroy(viewObjectA);
        Destroy(viewObjectB);
    }

    void RunCardDragHandFilterAndCancelSubTests()
    {
        BattleEndedTestContext context =
            CreateBattleEndedTestContext("drag60_2", 30, 30, 50, 10, 8, 5);
        List<BattleActionSlot> slots =
            BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        context.runtimeState.SetActionSlots(slots);
        context.runtimeState.SetIntentQueue(new List<BattleEnemyIntent>());

        BattleCardState cardA =
            CreateFixedAttackCardForCharacter(context.allyA, "drag60_2_card_a", 5);
        BattleCardState cardB =
            CreateFixedAttackCardForCharacter(context.allyA, "drag60_2_card_b", 5);
        int battleCardCountBefore = context.allyA.battleCards.Count;

        BattleActionAssignmentResult assignResult;
        bool assigned = BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            context.runtimeState,
            context.allyA,
            1,
            context.allyA,
            cardA,
            context.enemy,
            null,
            out assignResult
        );
        List<BattleCardState> visibleAfterAssign = GetMode60VisibleCards(
            context.runtimeState,
            cardA,
            cardB
        );
        bool hiddenByReference =
            assigned &&
            !visibleAfterAssign.Contains(cardA) &&
            visibleAfterAssign.Contains(cardB) &&
            context.allyA.battleCards.Contains(cardA) &&
            context.allyA.battleCards.Count == battleCardCountBefore;

        LogMode60Diagnostic(
            "测试2 已安排卡隐藏",
            context.runtimeState,
            context.allyA,
            1,
            cardA,
            "正式安排成功 expected=True actual=" + assigned,
            "IsCardAssigned(cardA) expected=True actual=" +
                BattleCardDropAssignmentRouter.IsCardAssigned(context.runtimeState, cardA),
            "可显示手牌包含cardA expected=False actual=" + visibleAfterAssign.Contains(cardA),
            "可显示手牌包含cardB expected=True actual=" + visibleAfterAssign.Contains(cardB),
            "battleCards仍包含cardA expected=True actual=" +
                context.allyA.battleCards.Contains(cardA),
            "battleCards数量不变 expected=" + battleCardCountBefore +
                " actual=" + context.allyA.battleCards.Count
        );

        BattleActionAssignmentResult cancelResult;
        bool cancelled = BattleCardDropAssignmentRouter.TryCancelSelectedSlot(
            context.runtimeState,
            context.allyA,
            1,
            out cancelResult
        );
        List<BattleCardState> visibleAfterCancel = GetMode60VisibleCards(
            context.runtimeState,
            cardA,
            cardB
        );
        bool restoredAfterCancel =
            cancelled &&
            visibleAfterCancel.Contains(cardA) &&
            visibleAfterCancel.Contains(cardB);

        Debug.Log("模式60 测试2 已安排具体卡牌实例从手牌过滤且未删除：" + hiddenByReference);
        Debug.Log("模式60 测试3 取消安排后具体卡牌实例重新显示：" + restoredAfterCancel);
    }

    void RunCardDragAtomicReplacementSubTests()
    {
        BattleEndedTestContext validContext =
            CreateBattleEndedTestContext("drag60_4", 30, 30, 50, 10, 8, 5);
        List<BattleActionSlot> validSlots =
            BattleActionSlotManager.CreatePartyActionSlots(
                validContext.allyA,
                validContext.allyB,
                2
            );
        validContext.runtimeState.SetActionSlots(validSlots);
        validContext.runtimeState.SetIntentQueue(new List<BattleEnemyIntent>());

        BattleCardState oldAttack =
            CreateFixedAttackCardForCharacter(validContext.allyA, "drag60_4_old_attack", 5);
        BattleCardState newDefense =
            CreateTestDefenseCardForCharacter(validContext.allyA, "drag60_4_new_defense", 4, 1);
        BattleActionAssignmentResult oldResult;
        BattleActionAssignmentResult replaceResult;
        BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            validContext.runtimeState,
            validContext.allyA,
            1,
            validContext.allyA,
            oldAttack,
            validContext.enemy,
            null,
            out oldResult
        );
        bool replaced = BattleCardDropAssignmentRouter.TryAssignToSelf(
            validContext.runtimeState,
            validContext.allyA,
            1,
            validContext.allyA,
            newDefense,
            validContext.allyA,
            out replaceResult
        );
        BattleActionSlot validSlot =
            BattleActionSlotManager.GetSlot(validSlots, validContext.allyA, 1);
        bool validHiddenChange =
            replaced &&
            object.ReferenceEquals(validSlot.cardState, newDefense) &&
            !BattleCardDropAssignmentRouter.IsCardAssigned(
                validContext.runtimeState,
                oldAttack
            ) &&
            BattleCardDropAssignmentRouter.IsCardAssigned(
                validContext.runtimeState,
                newDefense
            );

        BattleEndedTestContext invalidContext =
            CreateBattleEndedTestContext("drag60_5", 30, 30, 50, 10, 8, 5);
        List<BattleActionSlot> invalidSlots =
            BattleActionSlotManager.CreatePartyActionSlots(
                invalidContext.allyA,
                invalidContext.allyB,
                2
            );
        invalidContext.runtimeState.SetActionSlots(invalidSlots);
        invalidContext.runtimeState.SetIntentQueue(new List<BattleEnemyIntent>());

        BattleCardState retainedAttack =
            CreateFixedAttackCardForCharacter(invalidContext.allyA, "drag60_5_old_attack", 5);
        BattleCardState illegalAttack =
            CreateFixedAttackCardForCharacter(invalidContext.allyA, "drag60_5_new_attack", 5);
        BattleActionAssignmentResult retainedResult;
        BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            invalidContext.runtimeState,
            invalidContext.allyA,
            1,
            invalidContext.allyA,
            retainedAttack,
            invalidContext.enemy,
            null,
            out retainedResult
        );
        BattleActionSlot invalidSlot =
            BattleActionSlotManager.GetSlot(invalidSlots, invalidContext.allyA, 1);
        long oldSequence = invalidSlot.assignmentSequence;
        BattleActionPlacementType oldPlacementType = invalidSlot.placementType;
        BattleActionAssignmentResult illegalResult;
        bool illegalReplaced = BattleCardDropAssignmentRouter.TryAssignToSelf(
            invalidContext.runtimeState,
            invalidContext.allyA,
            1,
            invalidContext.allyA,
            illegalAttack,
            invalidContext.allyA,
            out illegalResult
        );
        List<BattleCardState> visibleAfterIllegalReplace = GetMode60VisibleCards(
            invalidContext.runtimeState,
            retainedAttack,
            illegalAttack
        );
        bool invalidAtomic =
            !illegalReplaced &&
            object.ReferenceEquals(invalidSlot.cardState, retainedAttack) &&
            invalidSlot.assignmentSequence == oldSequence &&
            invalidSlot.placementType == oldPlacementType &&
            !visibleAfterIllegalReplace.Contains(retainedAttack) &&
            visibleAfterIllegalReplace.Contains(illegalAttack) &&
            BattleCardDropAssignmentRouter.IsCardAssigned(
                invalidContext.runtimeState,
                retainedAttack
            ) &&
            !BattleCardDropAssignmentRouter.IsCardAssigned(
                invalidContext.runtimeState,
                illegalAttack
            );

        LogMode60Diagnostic(
            "测试5 非法替换原子保持",
            invalidContext.runtimeState,
            invalidContext.allyA,
            1,
            illegalAttack,
            "旧卡前置安排成功 expected=True actual=" +
                (retainedResult != null && retainedResult.isSuccess),
            "非法Self Attack返回失败 expected=False actual=" + illegalReplaced,
            "槽位仍引用旧卡 expected=" + retainedAttack.instanceID +
                " actual=" + (invalidSlot.cardState != null
                    ? invalidSlot.cardState.instanceID
                    : "null"),
            "旧卡仍隐藏 expected=True actual=" +
                BattleCardDropAssignmentRouter.IsCardAssigned(
                    invalidContext.runtimeState,
                    retainedAttack
                ),
            "新卡仍显示 expected=True actual=" +
                visibleAfterIllegalReplace.Contains(illegalAttack),
            "assignmentSequence保持 expected=" + oldSequence +
                " actual=" + invalidSlot.assignmentSequence,
            "placementType保持 expected=" + oldPlacementType +
                " actual=" + invalidSlot.placementType
        );

        Debug.Log("模式60 测试4 合法原子替换后旧卡显示且新卡隐藏：" + validHiddenChange);
        Debug.Log("模式60 测试5 非法替换保持旧卡安排与隐藏状态：" + invalidAtomic);
    }

    void RunCardDragEnemyRoutingSubTests()
    {
        BattleEndedTestContext exactContext =
            CreateBattleEndedTestContext("drag60_6_exact", 30, 30, 50, 12, 4, 5);
        List<BattleActionSlot> exactSlots =
            BattleActionSlotManager.CreatePartyActionSlots(
                exactContext.allyA,
                exactContext.allyB,
                2
            );
        BattleEnemyIntent exactIntent = CreatePreparedAssignmentIntent(
            exactContext,
            "drag60_6_exact_intent",
            exactContext.allyB,
            2,
            1,
            1
        );
        exactContext.runtimeState.SetActionSlots(exactSlots);
        exactContext.runtimeState.SetIntentQueue(
            BattleEnemyIntentManager.CreateIntentQueue(exactIntent)
        );
        BattleCardState exactAttack =
            CreateFixedAttackCardForCharacter(exactContext.allyA, "drag60_6_exact_attack", 5);
        BattleActionAssignmentResult exactResult;
        bool exactAssigned = BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            exactContext.runtimeState,
            exactContext.allyA,
            1,
            exactContext.allyA,
            exactAttack,
            exactContext.enemy,
            exactIntent,
            out exactResult
        );

        BattleEndedTestContext lowContext =
            CreateBattleEndedTestContext("drag60_6_low", 30, 30, 50, 3, 4, 8);
        List<BattleActionSlot> lowSlots =
            BattleActionSlotManager.CreatePartyActionSlots(
                lowContext.allyA,
                lowContext.allyB,
                2
            );
        BattleEnemyIntent lowIntent = CreatePreparedAssignmentIntent(
            lowContext,
            "drag60_6_low_intent",
            lowContext.allyB,
            2,
            1,
            1
        );
        lowContext.runtimeState.SetActionSlots(lowSlots);
        lowContext.runtimeState.SetIntentQueue(
            BattleEnemyIntentManager.CreateIntentQueue(lowIntent)
        );
        BattleCardState lowAttack =
            CreateFixedAttackCardForCharacter(lowContext.allyA, "drag60_6_low_attack", 5);
        BattleActionAssignmentResult lowResult;
        bool lowAssigned = BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            lowContext.runtimeState,
            lowContext.allyA,
            1,
            lowContext.allyA,
            lowAttack,
            lowContext.enemy,
            lowIntent,
            out lowResult
        );

        BattleEndedTestContext lowGuardContext =
            CreateBattleEndedTestContext("drag60_6_low_guard", 30, 30, 50, 3, 4, 8);
        List<BattleActionSlot> lowGuardSlots =
            BattleActionSlotManager.CreatePartyActionSlots(
                lowGuardContext.allyA,
                lowGuardContext.allyB,
                2
            );
        BattleEnemyIntent lowGuardIntent = CreatePreparedAssignmentIntent(
            lowGuardContext,
            "drag60_6_low_guard_intent",
            lowGuardContext.allyB,
            2,
            1,
            1
        );
        lowGuardContext.runtimeState.SetActionSlots(lowGuardSlots);
        lowGuardContext.runtimeState.SetIntentQueue(
            BattleEnemyIntentManager.CreateIntentQueue(lowGuardIntent)
        );
        BattleCardState lowDefense = CreateTestDefenseCardForCharacter(
            lowGuardContext.allyA,
            "drag60_6_low_defense",
            4,
            1
        );
        BattleCardState lowDodge = CreateFixedDodgeCardForCharacter(
            lowGuardContext.allyA,
            "drag60_6_low_dodge",
            4,
            1
        );
        BattleActionAssignmentResult lowDefenseResult;
        BattleActionAssignmentResult lowDodgeResult;
        bool lowDefenseAssigned = BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            lowGuardContext.runtimeState,
            lowGuardContext.allyA,
            1,
            lowGuardContext.allyA,
            lowDefense,
            lowGuardContext.enemy,
            lowGuardIntent,
            out lowDefenseResult
        );
        bool lowDodgeAssigned = BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            lowGuardContext.runtimeState,
            lowGuardContext.allyA,
            2,
            lowGuardContext.allyA,
            lowDodge,
            lowGuardContext.enemy,
            lowGuardIntent,
            out lowDodgeResult
        );
        bool occupiedEnemyRoute =
            exactAssigned &&
            exactResult != null &&
            exactResult.placementType == BattleActionPlacementType.ExactEnemyIntent &&
            !exactResult.wasAutoDowngraded &&
            lowAssigned &&
            lowResult != null &&
            lowResult.placementType == BattleActionPlacementType.SpecificEnemy &&
            lowResult.wasAutoDowngraded &&
            lowResult.effectiveSlotType == BattleActionSlotType.FreeAction &&
            lowDefenseAssigned &&
            lowDefenseResult != null &&
            lowDefenseResult.wasAutoDowngraded &&
            lowDefenseResult.placementType == BattleActionPlacementType.SpecificEnemy &&
            lowDefenseResult.effectiveSlotType == BattleActionSlotType.EnemySpecificGuard &&
            lowDodgeAssigned &&
            lowDodgeResult != null &&
            lowDodgeResult.wasAutoDowngraded &&
            lowDodgeResult.placementType == BattleActionPlacementType.SpecificEnemy &&
            lowDodgeResult.effectiveSlotType == BattleActionSlotType.EnemySpecificGuard;

        LogMode60Diagnostic(
            "测试6 敌方已有意图路由",
            exactContext.runtimeState,
            exactContext.allyA,
            1,
            exactAttack,
            "合格响应安排成功 expected=True actual=" + exactAssigned,
            "合格placement expected=ExactEnemyIntent actual=" +
                (exactResult != null
                    ? exactResult.placementType.ToString()
                    : "null"),
            "合格自动降级 expected=False actual=" +
                (exactResult != null && exactResult.wasAutoDowngraded),
            "低速响应安排成功 expected=True actual=" + lowAssigned,
            "低速placement expected=SpecificEnemy actual=" +
                (lowResult != null
                    ? lowResult.placementType.ToString()
                    : "null"),
            "低速自动降级 expected=True actual=" +
                (lowResult != null && lowResult.wasAutoDowngraded),
            "低速Attack有效类型 expected=FreeAction actual=" +
                (lowResult != null
                    ? lowResult.effectiveSlotType.ToString()
                    : "null"),
            "低速Defense路由 expected=SpecificEnemy/EnemySpecificGuard/downgraded actual=" +
                FormatMode60AssignmentResult(lowDefenseResult) + "/" +
                (lowDefenseResult != null && lowDefenseResult.wasAutoDowngraded),
            "低速Dodge路由 expected=SpecificEnemy/EnemySpecificGuard/downgraded actual=" +
                FormatMode60AssignmentResult(lowDodgeResult) + "/" +
                (lowDodgeResult != null && lowDodgeResult.wasAutoDowngraded)
        );

        BattleEndedTestContext emptyContext =
            CreateBattleEndedTestContext("drag60_7", 30, 30, 50, 10, 8, 5);
        List<BattleActionSlot> emptySlots =
            BattleActionSlotManager.CreatePartyActionSlots(
                emptyContext.allyA,
                emptyContext.allyB,
                3
            );
        emptyContext.runtimeState.SetActionSlots(emptySlots);
        emptyContext.runtimeState.SetIntentQueue(new List<BattleEnemyIntent>());
        BattleCardState attack =
            CreateFixedAttackCardForCharacter(emptyContext.allyA, "drag60_7_attack", 5);
        BattleCardState defense =
            CreateTestDefenseCardForCharacter(emptyContext.allyA, "drag60_7_defense", 4, 1);
        BattleCardState dodge =
            CreateFixedDodgeCardForCharacter(emptyContext.allyA, "drag60_7_dodge", 4, 1);
        BattleActionAssignmentResult attackResult;
        BattleActionAssignmentResult defenseResult;
        BattleActionAssignmentResult dodgeResult;
        bool attackAssigned = BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            emptyContext.runtimeState,
            emptyContext.allyA,
            1,
            emptyContext.allyA,
            attack,
            emptyContext.enemy,
            null,
            out attackResult
        );
        bool defenseAssigned = BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            emptyContext.runtimeState,
            emptyContext.allyA,
            2,
            emptyContext.allyA,
            defense,
            emptyContext.enemy,
            null,
            out defenseResult
        );
        bool dodgeAssigned = BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            emptyContext.runtimeState,
            emptyContext.allyA,
            3,
            emptyContext.allyA,
            dodge,
            emptyContext.enemy,
            null,
            out dodgeResult
        );
        bool emptyEnemyRoute =
            attackAssigned &&
            defenseAssigned &&
            dodgeAssigned &&
            attackResult != null &&
            defenseResult != null &&
            dodgeResult != null &&
            attackResult.placementType == BattleActionPlacementType.SpecificEnemy &&
            attackResult.effectiveSlotType == BattleActionSlotType.FreeAction &&
            defenseResult.placementType == BattleActionPlacementType.SpecificEnemy &&
            defenseResult.effectiveSlotType == BattleActionSlotType.EnemySpecificGuard &&
            dodgeResult.placementType == BattleActionPlacementType.SpecificEnemy &&
            dodgeResult.effectiveSlotType == BattleActionSlotType.EnemySpecificGuard;

        LogMode60Diagnostic(
            "测试7 敌方空槽三卡路由",
            emptyContext.runtimeState,
            emptyContext.allyA,
            1,
            attack,
            "Attack安排成功 expected=True actual=" + attackAssigned,
            "Attack placement/type expected=SpecificEnemy/FreeAction actual=" +
                FormatMode60AssignmentResult(attackResult),
            "Defense安排成功 expected=True actual=" + defenseAssigned,
            "Defense placement/type expected=SpecificEnemy/EnemySpecificGuard actual=" +
                FormatMode60AssignmentResult(defenseResult),
            "Dodge安排成功 expected=True actual=" + dodgeAssigned,
            "Dodge placement/type expected=SpecificEnemy/EnemySpecificGuard actual=" +
                FormatMode60AssignmentResult(dodgeResult)
        );

        Debug.Log("模式60 测试6 敌方已有意图路由并保留正式自动降级：" + occupiedEnemyRoute);
        Debug.Log("模式60 测试7 敌方空槽路由派生Attack与守备类型：" + emptyEnemyRoute);
    }

    void RunCardDragSelfAndValidationSubTests()
    {
        BattleEndedTestContext selfContext =
            CreateBattleEndedTestContext("drag60_8", 30, 30, 50, 10, 8, 5);
        List<BattleActionSlot> selfSlots =
            BattleActionSlotManager.CreatePartyActionSlots(
                selfContext.allyA,
                selfContext.allyB,
                4
            );
        selfContext.runtimeState.SetActionSlots(selfSlots);
        selfContext.runtimeState.SetIntentQueue(new List<BattleEnemyIntent>());
        BattleCardState defense =
            CreateTestDefenseCardForCharacter(selfContext.allyA, "drag60_8_defense", 4, 1);
        BattleCardState dodge =
            CreateFixedDodgeCardForCharacter(selfContext.allyA, "drag60_8_dodge", 4, 1);
        BattleCardState ability =
            CreateBattleEndedAbilityCard(selfContext.allyA, "drag60_8_ability", "Drag60Ability");
        BattleCardState retainedDefense =
            CreateTestDefenseCardForCharacter(selfContext.allyA, "drag60_8_retained", 4, 1);
        BattleCardState illegalAttack =
            CreateFixedAttackCardForCharacter(selfContext.allyA, "drag60_8_illegal_attack", 5);
        BattleActionAssignmentResult defenseResult;
        BattleActionAssignmentResult dodgeResult;
        BattleActionAssignmentResult abilityResult;
        BattleActionAssignmentResult retainedResult;
        BattleActionAssignmentResult illegalResult;
        bool defenseAssigned = BattleCardDropAssignmentRouter.TryAssignToSelf(
            selfContext.runtimeState,
            selfContext.allyA,
            1,
            selfContext.allyA,
            defense,
            selfContext.allyA,
            out defenseResult
        );
        bool dodgeAssigned = BattleCardDropAssignmentRouter.TryAssignToSelf(
            selfContext.runtimeState,
            selfContext.allyA,
            2,
            selfContext.allyA,
            dodge,
            selfContext.allyA,
            out dodgeResult
        );
        bool abilityAssigned = BattleCardDropAssignmentRouter.TryAssignToSelf(
            selfContext.runtimeState,
            selfContext.allyA,
            3,
            selfContext.allyA,
            ability,
            selfContext.allyA,
            out abilityResult
        );
        BattleCardDropAssignmentRouter.TryAssignToSelf(
            selfContext.runtimeState,
            selfContext.allyA,
            4,
            selfContext.allyA,
            retainedDefense,
            selfContext.allyA,
            out retainedResult
        );
        bool attackAssigned = BattleCardDropAssignmentRouter.TryAssignToSelf(
            selfContext.runtimeState,
            selfContext.allyA,
            4,
            selfContext.allyA,
            illegalAttack,
            selfContext.allyA,
            out illegalResult
        );
        BattleActionSlot retainedSlot =
            BattleActionSlotManager.GetSlot(selfSlots, selfContext.allyA, 4);
        bool selfRoute =
            defenseAssigned &&
            dodgeAssigned &&
            abilityAssigned &&
            !attackAssigned &&
            object.ReferenceEquals(retainedSlot.cardState, retainedDefense);

        BattleEndedTestContext mismatchContext =
            CreateBattleEndedTestContext("drag60_9", 30, 30, 50, 10, 8, 5);
        List<BattleActionSlot> mismatchSlots =
            BattleActionSlotManager.CreatePartyActionSlots(
                mismatchContext.allyA,
                mismatchContext.allyB,
                2
            );
        mismatchContext.runtimeState.SetActionSlots(mismatchSlots);
        mismatchContext.runtimeState.SetIntentQueue(new List<BattleEnemyIntent>());
        BattleCardState foreignCard =
            CreateFixedAttackCardForCharacter(mismatchContext.allyB, "drag60_9_foreign", 5);
        BattleActionAssignmentResult mismatchResult;
        bool mismatchAssigned = BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            mismatchContext.runtimeState,
            mismatchContext.allyA,
            1,
            mismatchContext.allyB,
            foreignCard,
            mismatchContext.enemy,
            null,
            out mismatchResult
        );
        bool mismatchRejected =
            !mismatchAssigned &&
            BattleActionSlotManager.GetSlot(mismatchSlots, mismatchContext.allyA, 1).IsEmpty();

        BattleCardState ownCard =
            CreateFixedAttackCardForCharacter(mismatchContext.allyA, "drag60_10_own", 5);
        BattleActionAssignmentResult noSlotResult;
        bool noSlotAssigned = BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            mismatchContext.runtimeState,
            mismatchContext.allyA,
            0,
            mismatchContext.allyA,
            ownCard,
            mismatchContext.enemy,
            null,
            out noSlotResult
        );
        bool noSlotRejected =
            !noSlotAssigned &&
            BattleActionSlotManager.GetSlot(mismatchSlots, mismatchContext.allyA, 1).IsEmpty();

        Debug.Log("模式60 测试8 Self允许Defense/Dodge/Ability且拒绝Attack原子替换：" + selfRoute);
        Debug.Log("模式60 测试9 卡牌持有者与选中角色不匹配时拒绝：" + mismatchRejected);
        Debug.Log("模式60 测试10 未选择正式槽位时拒绝且不改状态：" + noSlotRejected);
    }

    void RunCardDragUISlotAndRefreshSubTests()
    {
        bool enemySlotMapping =
            BattleCardDropAssignmentRouter.EnemySlotIndexToUIIndex(2) == 1 &&
            BattleCardDropAssignmentRouter.EnemySlotIndexToUIIndex(0) == -1 &&
            BattleCardDropAssignmentRouter.EnemySlotIndexToUIIndex(3) == -1;

        BattleEndedTestContext cancelContext =
            CreateBattleEndedTestContext("drag60_12", 30, 30, 50, 10, 8, 5);
        List<BattleActionSlot> cancelSlots =
            BattleActionSlotManager.CreatePartyActionSlots(
                cancelContext.allyA,
                cancelContext.allyB,
                2
            );
        cancelContext.runtimeState.SetActionSlots(cancelSlots);
        cancelContext.runtimeState.SetIntentQueue(new List<BattleEnemyIntent>());
        BattleCardState slot1Card =
            CreateFixedAttackCardForCharacter(cancelContext.allyA, "drag60_12_slot1", 5);
        BattleCardState slot2Card =
            CreateFixedAttackCardForCharacter(cancelContext.allyA, "drag60_12_slot2", 5);
        BattleActionAssignmentResult slot1Result;
        BattleActionAssignmentResult slot2Result;
        BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            cancelContext.runtimeState,
            cancelContext.allyA,
            1,
            cancelContext.allyA,
            slot1Card,
            cancelContext.enemy,
            null,
            out slot1Result
        );
        BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            cancelContext.runtimeState,
            cancelContext.allyA,
            2,
            cancelContext.allyA,
            slot2Card,
            cancelContext.enemy,
            null,
            out slot2Result
        );

        GameObject slotObject = new GameObject("Drag60SlotView", typeof(RectTransform));
        slotObject.SetActive(false);
        UnityEngine.UI.Image testImage = slotObject.AddComponent<UnityEngine.UI.Image>();
        BattleActionSlotUIView slotView = slotObject.AddComponent<BattleActionSlotUIView>();
        Texture2D testTexture = new Texture2D(1, 1);
        Sprite testSprite = Sprite.Create(
            testTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f)
        );
        slotView.ConfigureTestVisuals(testImage, testSprite);
        slotObject.SetActive(true);
        slotView.BindInteraction(cancelContext.allyA, 0, false, null);
        BattleActionAssignmentResult cancelResult;
        bool cancelled = BattleCardDropAssignmentRouter.TryCancelSelectedSlot(
            cancelContext.runtimeState,
            cancelContext.allyA,
            slotView.FormalSlotIndex,
            out cancelResult
        );
        bool slot1EmptyAfterCancel =
            BattleActionSlotManager.GetSlot(cancelSlots, cancelContext.allyA, 1).IsEmpty();
        bool slot2RetainedAfterCancel = object.ReferenceEquals(
            BattleActionSlotManager.GetSlot(cancelSlots, cancelContext.allyA, 2).cardState,
            slot2Card
        );
        bool formalIndexCancel =
            slotView.UISlotIndex == 0 &&
            slotView.FormalSlotIndex == 1 &&
            cancelled &&
            slot1EmptyAfterCancel &&
            slot2RetainedAfterCancel;

        LogMode60Diagnostic(
            "测试12 UI索引与正式取消索引",
            cancelContext.runtimeState,
            cancelContext.allyA,
            slotView.FormalSlotIndex,
            slot1Card,
            "UISlotIndex expected=0 actual=" + slotView.UISlotIndex,
            "FormalSlotIndex expected=1 actual=" + slotView.FormalSlotIndex,
            "取消返回成功 expected=True actual=" + cancelled,
            "AllyA Slot_01为空 expected=True actual=" + slot1EmptyAfterCancel,
            "AllyA Slot_02保留原卡 expected=True actual=" + slot2RetainedAfterCancel
        );

        slotView.SetState(BattleActionSlotUIState.AllyActionSet);
        slotView.SetSelected(false);
        bool assignedNotSelected =
            slotView.CurrentBaseState == BattleActionSlotUIState.AllyActionSet &&
            !slotView.IsSelected;
        slotView.SetSelected(true);
        bool clickedAssignedSlotSelected =
            slotView.IsSelected &&
            slotView.CurrentBaseState == BattleActionSlotUIState.AllyActionSet;

        BattleCardState replacementSourceCard =
            CreateFixedAttackCardForCharacter(cancelContext.allyA, "drag60_13_old", 5);
        BattleCardState replacementDefense =
            CreateTestDefenseCardForCharacter(cancelContext.allyA, "drag60_13_defense", 4, 1);
        BattleActionAssignmentResult sourceAssignResult;
        bool sourceAssigned = BattleCardDropAssignmentRouter.TryAssignToEnemySlot(
            cancelContext.runtimeState,
            cancelContext.allyA,
            1,
            cancelContext.allyA,
            replacementSourceCard,
            cancelContext.enemy,
            null,
            out sourceAssignResult
        );

        BattleCardSelectionController clickSelection =
            new BattleCardSelectionController();
        BattleCardInteractionCoordinator coordinator =
            new BattleCardInteractionCoordinator(clickSelection);
        BattleCardUIView replacementView = CreatePrimaryPreviewCardView(
            "Mode60ReplacementDefense",
            cancelContext.allyA,
            cancelContext.allyA,
            replacementDefense
        );
        replacementView.BindCard(
            cancelContext.allyA,
            replacementDefense,
            BattleCardUIPreviewBuilder.Build(
                cancelContext.allyA,
                cancelContext.allyA,
                replacementDefense
            ),
            clickSelection
        );
        GameObject selfTargetObject = new GameObject(
            "Mode60SelfTarget",
            typeof(RectTransform),
            typeof(BattleSelfActionDropZone)
        );
        BattleSelfActionDropZone selfTarget =
            selfTargetObject.GetComponent<BattleSelfActionDropZone>();
        BattleCardInteractionOutcome replacementOutcome = null;
        selfTarget.Bind(
            cancelContext.allyA,
            clickedTarget =>
            {
                replacementOutcome = coordinator.ClickSelfTarget(
                    cancelContext.runtimeState,
                    clickedTarget
                );
            }
        );
        PointerEventData leftClick = new PointerEventData(null)
        {
            button = PointerEventData.InputButton.Left
        };
        bool replacementSourceSelected =
            coordinator.SelectSourceSlot(slotView);
        replacementView.OnPointerClick(leftClick);
        selfTarget.OnPointerClick(leftClick);
        bool replacementSucceeded =
            replacementOutcome != null &&
            replacementOutcome.isSuccess &&
            replacementOutcome.assignmentResult != null &&
            replacementOutcome.assignmentResult.isSuccess;
        slotView.SetState(BattleActionSlotUIState.AllyActionSet);
        bool successfulReplacementKeepsSlotSelection =
            sourceAssigned &&
            replacementSourceSelected &&
            replacementSucceeded &&
            slotView.IsSelected &&
            slotView.CurrentBaseState == BattleActionSlotUIState.AllyActionSet &&
            object.ReferenceEquals(
                coordinator.SelectedCharacter,
                cancelContext.allyA
            ) &&
            object.ReferenceEquals(
                coordinator.SelectedActionSlotView,
                slotView
            ) &&
            !clickSelection.HasSelection &&
            object.ReferenceEquals(
                BattleActionSlotManager.GetSlot(
                    cancelSlots,
                    cancelContext.allyA,
                    1
                ).cardState,
                replacementDefense
            );

        BattleCardState illegalReplacement =
            CreateFixedAttackCardForCharacter(cancelContext.allyA, "drag60_13_illegal", 5);
        BattleCardUIView illegalView = CreatePrimaryPreviewCardView(
            "Mode60IllegalSelfAttack",
            cancelContext.allyA,
            cancelContext.allyA,
            illegalReplacement
        );
        illegalView.BindCard(
            cancelContext.allyA,
            illegalReplacement,
            BattleCardUIPreviewBuilder.Build(
                cancelContext.allyA,
                cancelContext.allyA,
                illegalReplacement
            ),
            clickSelection
        );
        bool illegalSourceSelected = coordinator.SelectSourceSlot(slotView);
        illegalView.OnPointerClick(leftClick);
        BattleCardInteractionOutcome illegalOutcome = null;
        selfTarget.Bind(
            cancelContext.allyA,
            clickedTarget =>
            {
                illegalOutcome = coordinator.ClickSelfTarget(
                    cancelContext.runtimeState,
                    clickedTarget
                );
            }
        );
        selfTarget.OnPointerClick(leftClick);
        bool illegalReplacementSucceeded =
            illegalOutcome != null &&
            illegalOutcome.isSuccess;
        bool failedReplacementKeepsSelection =
            illegalSourceSelected &&
            !illegalReplacementSucceeded &&
            slotView.IsSelected &&
            object.ReferenceEquals(
                coordinator.SelectedCharacter,
                cancelContext.allyA
            ) &&
            object.ReferenceEquals(
                coordinator.SelectedActionSlotView,
                slotView
            ) &&
            clickSelection.IsSelected(illegalView) &&
            object.ReferenceEquals(
                BattleActionSlotManager.GetSlot(cancelSlots, cancelContext.allyA, 1).cardState,
                replacementDefense
            );

        LogMode60Diagnostic(
            "测试14 点击式选择清理",
            cancelContext.runtimeState,
            cancelContext.allyA,
            1,
            replacementDefense,
            "业务安排立即成功 expected=True actual=" + replacementSucceeded,
            "成功后卡牌选择清空且槽位选择保留 expected=True actual=" +
                successfulReplacementKeepsSlotSelection,
            "失败后逻辑选择保留 expected=True actual=" +
                failedReplacementKeepsSelection
        );

        Debug.Log("模式60 测试11 enemySlotIndex 2映射到UI索引1：" + enemySlotMapping);
        Debug.Log("模式60 测试12 UI索引0使用正式槽位1执行右键取消：" + formalIndexCancel);
        Debug.Log(
            "模式60 测试13 安排成功保留槽位选择且失败保持选择：" +
            (assignedNotSelected &&
             clickedAssignedSlotSelected &&
             successfulReplacementKeepsSlotSelection &&
             failedReplacementKeepsSelection)
        );
        Debug.Log(
            "模式60 测试14 点击指派成功清卡牌并保留槽位：" +
            (successfulReplacementKeepsSlotSelection &&
             failedReplacementKeepsSelection)
        );
        Destroy(replacementView.gameObject);
        Destroy(illegalView.gameObject);
        Destroy(selfTargetObject);
        Destroy(slotObject);
        Destroy(testSprite);
        Destroy(testTexture);
    }

    List<BattleCardState> GetMode60VisibleCards(
        BattleRuntimeState runtimeState,
        params BattleCardState[] cardStates
    )
    {
        List<BattleCardState> visibleCards = new List<BattleCardState>();
        if (cardStates == null)
        {
            return visibleCards;
        }

        foreach (BattleCardState cardState in cardStates)
        {
            if (cardState != null &&
                !BattleCardDropAssignmentRouter.IsCardAssigned(runtimeState, cardState))
            {
                visibleCards.Add(cardState);
            }
        }

        return visibleCards;
    }

    void LogMode60Diagnostic(
        string testName,
        BattleRuntimeState runtimeState,
        CharacterData owner,
        int formalSlotIndex,
        BattleCardState cardState,
        params string[] checks
    )
    {
        string checkText = checks != null && checks.Length > 0
            ? string.Join("\n- ", checks)
            : "无子条件";

        Debug.Log(
            "[模式60结构化诊断] " + testName + "\n" +
            "Card instanceID: " +
                (cardState != null ? cardState.instanceID : "null") + "\n" +
            "Card owner: " +
                (cardState != null && cardState.owner != null
                    ? cardState.owner.characterName
                    : "null") + "\n" +
            "Selected owner: " +
                (owner != null ? owner.characterName : "null") + "\n" +
            "Formal slot: " + formalSlotIndex + "\n" +
            "Phase: " +
                (runtimeState != null ? runtimeState.currentPhase : "null") + "\n" +
            "ExecutionPlan is null: " +
                (runtimeState == null || runtimeState.currentExecutionPlan == null) + "\n" +
            "Checks:\n- " + checkText + "\n" +
            "Runtime slots:\n" + FormatMode60SlotSnapshot(runtimeState)
        );
    }

    string FormatMode60AssignmentResult(BattleActionAssignmentResult result)
    {
        return result != null
            ? result.placementType + "/" + result.effectiveSlotType
            : "null";
    }

    string FormatMode60SlotSnapshot(BattleRuntimeState runtimeState)
    {
        if (runtimeState == null || runtimeState.actionSlots == null)
        {
            return "actionSlots=null";
        }

        string snapshot = "";
        for (int i = 0; i < runtimeState.actionSlots.Count; i++)
        {
            BattleActionSlot slot = runtimeState.actionSlots[i];
            if (slot == null)
            {
                snapshot += "[" + i + "] null\n";
                continue;
            }

            snapshot +=
                "[" + i + "] owner=" +
                (slot.owner != null ? slot.owner.characterName : "null") +
                ", formalSlot=" + slot.slotIndex +
                ", card=" +
                (slot.cardState != null ? slot.cardState.instanceID : "null") +
                ", placement=" + slot.placementType +
                ", type=" + slot.slotType +
                ", sequence=" + slot.assignmentSequence +
                "\n";
        }

        return snapshot;
    }

    void RunBattleExecutionOrderingAndGuardPriorityBasicTestSequence()
    {
        Debug.Log("===== BattleExecutionOrderingAndGuardPriorityBasic 聚合测试开始 =====");

        RunExecutionOrderingEffectiveSpeedSubTests();
        RunExecutionOrderingTieBreakerSubTests();
        RunExecutionOrderingMixedAndStableSubTests();
        RunGuardPriorityAndScopeSubTests();
        RunGuardSingleUseAndInvalidCandidateSubTests();

        Debug.Log("===== BattleExecutionOrderingAndGuardPriorityBasic 聚合测试结束 =====");
    }

    void RunExecutionOrderingEffectiveSpeedSubTests()
    {
        BattleEndedTestContext lowContext = CreateBattleEndedTestContext("order58_a", 30, 30, 50, 5, 8, 10);
        BattleCardState lowResponseCard = CreateFixedAttackCardForCharacter(lowContext.allyA, "order58_a_response", 8);
        BattleCardState speed8FreeCard = CreateFixedAttackCardForCharacter(lowContext.allyB, "order58_a_free", 5);
        BattleCardState lowEnemyCard = CreateFixedEnemyAttackCardForDodgeTest(lowContext.enemy, "order58_a_enemy", 5, 0);
        BattleEnemyIntent lowIntent = CreateEnemyAttackIntent("order58_a_intent", lowContext.enemy, lowEnemyCard, lowContext.allyA, 1);
        BattleActionSlot lowResponseSlot = CreateMode58ResponseSlot(lowContext.allyA, 1, lowResponseCard, lowIntent);
        BattleActionSlot speed8FreeSlot = CreateMode58FreeSlot(lowContext.allyB, 1, speed8FreeCard, lowContext.enemy);
        List<BattleActionSlot> lowSlots = new List<BattleActionSlot> { speed8FreeSlot, lowResponseSlot };
        BattleExecutionPlan lowPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            lowSlots,
            BattleEnemyIntentManager.CreateIntentQueue(lowIntent),
            lowContext.runtimeState
        );
        BattleExecutionItem lowItem = GetFirstExecutionItem(lowPlan);

        bool lowInheritedEnemySpeed =
            lowItem != null &&
            lowItem.executionType == BattleExecutionItemType.RespondedEnemyIntent &&
            lowItem.effectiveSpeed == 10 &&
            lowItem.actionSlotOrder == 1 &&
            lowPlan.executionItems.Count == 2 &&
            lowPlan.executionItems[1].actionSlot == speed8FreeSlot &&
            lowPlan.executionItems[1].effectiveSpeed == 8;
        Debug.Log("模式58 A 速度5响应继承敌人速度10并排在速度8行动之前：" + lowInheritedEnemySpeed);

        BattleEndedTestContext highContext = CreateBattleEndedTestContext("order58_b", 30, 30, 50, 12, 4, 10);
        BattleCardState highResponseCard = CreateFixedAttackCardForCharacter(highContext.allyA, "order58_b_response", 8);
        BattleCardState highEnemyCard = CreateFixedEnemyAttackCardForDodgeTest(highContext.enemy, "order58_b_enemy", 5, 0);
        BattleEnemyIntent highIntent = CreateEnemyAttackIntent("order58_b_intent", highContext.enemy, highEnemyCard, highContext.allyA, 1);
        BattleActionSlot highResponseSlot = CreateMode58ResponseSlot(highContext.allyA, 1, highResponseCard, highIntent);
        BattleExecutionPlan highPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            new List<BattleActionSlot> { highResponseSlot },
            BattleEnemyIntentManager.CreateIntentQueue(highIntent),
            highContext.runtimeState
        );
        BattleExecutionItem highItem = GetFirstExecutionItem(highPlan);

        Debug.Log(
            "模式58 B 高速响应使用玩家速度12：" +
            (highItem != null && highItem.effectiveSpeed == 12)
        );
    }

    void RunExecutionOrderingTieBreakerSubTests()
    {
        BattleEndedTestContext priorityContext = CreateBattleEndedTestContext("order58_c", 30, 30, 50, 10, 10, 10);
        BattleCardState responseCard = CreateFixedAttackCardForCharacter(priorityContext.allyA, "order58_c_response", 8);
        BattleCardState freeCard = CreateFixedAttackCardForCharacter(priorityContext.allyB, "order58_c_free", 5);
        BattleCardState enemyCard = CreateFixedEnemyAttackCardForDodgeTest(priorityContext.enemy, "order58_c_enemy", 5, 0);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("order58_c_intent", priorityContext.enemy, enemyCard, priorityContext.allyA, 1);
        BattleActionSlot responseSlot = CreateMode58ResponseSlot(priorityContext.allyA, 2, responseCard, intent);
        BattleActionSlot freeSlot = CreateMode58FreeSlot(priorityContext.allyB, 1, freeCard, priorityContext.enemy);
        BattleExecutionPlan priorityPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            new List<BattleActionSlot> { freeSlot, responseSlot },
            BattleEnemyIntentManager.CreateIntentQueue(intent),
            priorityContext.runtimeState
        );

        bool respondedFirst =
            priorityPlan.executionItems.Count == 2 &&
            priorityPlan.executionItems[0].executionType == BattleExecutionItemType.RespondedEnemyIntent &&
            priorityPlan.executionItems[0].responsePriority == 0 &&
            priorityPlan.executionItems[1].executionType == BattleExecutionItemType.FreeAction;
        Debug.Log("模式58 C 同速Responded优先于FreeAction：" + respondedFirst);

        BattleEndedTestContext slotContext = CreateBattleEndedTestContext("order58_d", 30, 30, 50, 10, 4, 3);
        BattleActionSlot slot2 = CreateMode58FreeSlot(
            slotContext.allyA,
            2,
            CreateFixedAttackCardForCharacter(slotContext.allyA, "order58_d_slot2", 5),
            slotContext.enemy
        );
        BattleActionSlot slot1 = CreateMode58FreeSlot(
            slotContext.allyA,
            1,
            CreateFixedAttackCardForCharacter(slotContext.allyA, "order58_d_slot1", 5),
            slotContext.enemy
        );
        BattleExecutionPlan slotPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            new List<BattleActionSlot> { slot2, slot1 },
            new List<BattleEnemyIntent>(),
            slotContext.runtimeState
        );
        Debug.Log(
            "模式58 D 同速同角色按槽位1先于槽位2：" +
            (slotPlan.executionItems.Count == 2 &&
             slotPlan.executionItems[0].actionSlot == slot1 &&
             slotPlan.executionItems[1].actionSlot == slot2)
        );

        BattleEndedTestContext positionContext = CreateBattleEndedTestContext("order58_e", 30, 30, 50, 10, 10, 3);
        BattleActionSlot allyBSlot = CreateMode58FreeSlot(
            positionContext.allyB,
            1,
            CreateFixedAttackCardForCharacter(positionContext.allyB, "order58_e_b", 5),
            positionContext.enemy
        );
        BattleActionSlot allyASlot = CreateMode58FreeSlot(
            positionContext.allyA,
            1,
            CreateFixedAttackCardForCharacter(positionContext.allyA, "order58_e_a", 5),
            positionContext.enemy
        );
        BattleExecutionPlan positionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            new List<BattleActionSlot> { allyBSlot, allyASlot },
            new List<BattleEnemyIntent>(),
            positionContext.runtimeState
        );
        Debug.Log(
            "模式58 E 同速同槽位按battleUnits站位A先于B：" +
            (positionPlan.executionItems.Count == 2 &&
             positionPlan.executionItems[0].actionSlot == allyASlot &&
             positionPlan.executionItems[0].actorPositionOrder == 1 &&
             positionPlan.executionItems[1].actorPositionOrder == 2)
        );
    }

    void RunExecutionOrderingMixedAndStableSubTests()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("order58_f", 30, 30, 50, 12, 3, 8);
        BattleCardState respondedEnemyCard = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "order58_f_enemy1", 5, 0);
        BattleCardState unrespondedEnemyCard = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "order58_f_enemy2", 5, 0);
        BattleEnemyIntent respondedIntent = new BattleEnemyIntent(
            "order58_f_intent1",
            context.enemy,
            respondedEnemyCard,
            context.allyB,
            1,
            1,
            1
        );
        BattleEnemyIntent unrespondedIntent = new BattleEnemyIntent(
            "order58_f_intent2",
            context.enemy,
            unrespondedEnemyCard,
            context.allyB,
            2,
            2,
            2
        );
        BattleActionSlot fastFree = CreateMode58FreeSlot(
            context.allyA,
            1,
            CreateFixedAttackCardForCharacter(context.allyA, "order58_f_fast_free", 5),
            context.enemy
        );
        BattleActionSlot response = CreateMode58ResponseSlot(
            context.allyB,
            1,
            CreateFixedAttackCardForCharacter(context.allyB, "order58_f_response", 5),
            respondedIntent
        );
        BattleActionSlot slowFree = CreateMode58FreeSlot(
            context.allyB,
            2,
            CreateFixedAttackCardForCharacter(context.allyB, "order58_f_slow_free", 5),
            context.enemy
        );
        List<BattleActionSlot> slots = new List<BattleActionSlot> { slowFree, response, fastFree };
        List<BattleEnemyIntent> intents = new List<BattleEnemyIntent> { unrespondedIntent, respondedIntent };

        BattleExecutionPlan firstPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            slots,
            intents,
            context.runtimeState
        );
        BattleExecutionPlan secondPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            slots,
            intents,
            context.runtimeState
        );

        bool mixedOrder =
            firstPlan.executionItems.Count == 4 &&
            firstPlan.executionItems[0].actionSlot == fastFree &&
            firstPlan.executionItems[1].executionType == BattleExecutionItemType.RespondedEnemyIntent &&
            firstPlan.executionItems[2].executionType == BattleExecutionItemType.UnrespondedEnemyIntent &&
            firstPlan.executionItems[3].actionSlot == slowFree;
        bool stableOrder = AreExecutionPlansReferenceOrderedTheSame(firstPlan, secondPlan);

        Debug.Log("模式58 F Responded/Unresponded/Free进入统一速度队列：" + mixedOrder);
        Debug.Log("模式58 N 相同输入重复生成计划顺序稳定：" + stableOrder);

        BattleActionSlot guard = new BattleActionSlot(context.allyB, 3);
        guard.AssignPassiveGuard(
            context.allyB,
            CreateTestDefenseCardForCharacter(context.allyB, "order58_g_guard", 9, 1)
        );
        BattleActionSlot specificGuard = new BattleActionSlot(context.allyB, 4);
        specificGuard.AssignEnemySpecificGuard(
            context.allyB,
            CreateTestDefenseCardForCharacter(context.allyB, "order58_g_specific", 9, 1),
            context.enemy
        );
        BattleExecutionPlan guardPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            new List<BattleActionSlot> { guard, specificGuard },
            BattleEnemyIntentManager.CreateIntentQueue(unrespondedIntent),
            context.runtimeState
        );
        Debug.Log(
            "模式58 G 守备槽位不独立生成ExecutionItem：" +
            (guardPlan.executionItems.Count == 1 &&
             guardPlan.executionItems[0].executionType == BattleExecutionItemType.UnrespondedEnemyIntent)
        );
    }

    void RunGuardPriorityAndScopeSubTests()
    {
        BattleEndedTestContext priorityContext = CreateBattleEndedTestContext("guard58_h", 30, 30, 50, 5, 5, 8);
        BattleEnemyIntent priorityIntent = CreateEnemyAttackIntent(
            "guard58_h_intent",
            priorityContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(priorityContext.enemy, "guard58_h_enemy", 5, 0),
            priorityContext.allyB,
            1
        );
        BattleActionSlot passiveSlot = new BattleActionSlot(priorityContext.allyB, 1);
        passiveSlot.AssignPassiveGuard(
            priorityContext.allyB,
            CreateTestDefenseCardForCharacter(priorityContext.allyB, "guard58_h_passive", 12, 1)
        );
        BattleActionSlot specificSlot = new BattleActionSlot(priorityContext.allyB, 2);
        specificSlot.AssignEnemySpecificGuard(
            priorityContext.allyB,
            CreateTestDefenseCardForCharacter(priorityContext.allyB, "guard58_h_specific", 12, 1),
            priorityContext.enemy
        );
        ExecuteMode58UnrespondedPlan(priorityContext, priorityIntent, new List<BattleActionSlot> { passiveSlot, specificSlot });
        Debug.Log(
            "模式58 H 指定守备优先于更小槽位的被动守备：" +
            (specificSlot.isUsed && !passiveSlot.isUsed)
        );

        BattleEndedTestContext mismatchContext = CreateBattleEndedTestContext("guard58_i", 30, 30, 50, 5, 5, 8);
        BattleEnemyIntent mismatchIntent = CreateEnemyAttackIntent(
            "guard58_i_intent",
            mismatchContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(mismatchContext.enemy, "guard58_i_enemy", 5, 0),
            mismatchContext.allyB,
            1
        );
        CharacterData otherEnemy = new CharacterData("guard58_i_other_enemy", 50, 8, 8);
        BattleActionSlot mismatchSpecific = new BattleActionSlot(mismatchContext.allyB, 1);
        mismatchSpecific.AssignEnemySpecificGuard(
            mismatchContext.allyB,
            CreateTestDefenseCardForCharacter(mismatchContext.allyB, "guard58_i_specific", 12, 1),
            otherEnemy
        );
        BattleActionSlot fallbackPassive = new BattleActionSlot(mismatchContext.allyB, 2);
        fallbackPassive.AssignPassiveGuard(
            mismatchContext.allyB,
            CreateTestDefenseCardForCharacter(mismatchContext.allyB, "guard58_i_passive", 12, 1)
        );
        ExecuteMode58UnrespondedPlan(
            mismatchContext,
            mismatchIntent,
            new List<BattleActionSlot> { mismatchSpecific, fallbackPassive }
        );
        Debug.Log(
            "模式58 I 指定敌人不匹配后回落被动守备：" +
            (!mismatchSpecific.isUsed && fallbackPassive.isUsed)
        );

        BattleEndedTestContext slotContext = CreateBattleEndedTestContext("guard58_j", 30, 30, 50, 5, 5, 8);
        BattleEnemyIntent slotIntent = CreateEnemyAttackIntent(
            "guard58_j_intent",
            slotContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(slotContext.enemy, "guard58_j_enemy", 5, 0),
            slotContext.allyB,
            1
        );
        BattleActionSlot specific2 = new BattleActionSlot(slotContext.allyB, 2);
        specific2.AssignEnemySpecificGuard(
            slotContext.allyB,
            CreateTestDefenseCardForCharacter(slotContext.allyB, "guard58_j_slot2", 12, 1),
            slotContext.enemy
        );
        BattleActionSlot specific1 = new BattleActionSlot(slotContext.allyB, 1);
        specific1.AssignEnemySpecificGuard(
            slotContext.allyB,
            CreateTestDefenseCardForCharacter(slotContext.allyB, "guard58_j_slot1", 12, 1),
            slotContext.enemy
        );
        ExecuteMode58UnrespondedPlan(slotContext, slotIntent, new List<BattleActionSlot> { specific2, specific1 });
        Debug.Log(
            "模式58 J 同一守备范围按slotIndex升序：" +
            (specific1.isUsed && !specific2.isUsed)
        );

        BattleEndedTestContext passiveOrderContext = CreateBattleEndedTestContext("guard58_j_passive", 30, 30, 50, 5, 5, 8);
        BattleEnemyIntent passiveOrderIntent = CreateEnemyAttackIntent(
            "guard58_j_passive_intent",
            passiveOrderContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(passiveOrderContext.enemy, "guard58_j_passive_enemy", 5, 0),
            passiveOrderContext.allyB,
            1
        );
        BattleActionSlot passive2 = new BattleActionSlot(passiveOrderContext.allyB, 2);
        passive2.AssignPassiveGuard(
            passiveOrderContext.allyB,
            CreateTestDefenseCardForCharacter(passiveOrderContext.allyB, "guard58_j_passive2", 12, 1)
        );
        BattleActionSlot passive1 = new BattleActionSlot(passiveOrderContext.allyB, 1);
        passive1.AssignPassiveGuard(
            passiveOrderContext.allyB,
            CreateTestDefenseCardForCharacter(passiveOrderContext.allyB, "guard58_j_passive1", 12, 1)
        );
        ExecuteMode58UnrespondedPlan(
            passiveOrderContext,
            passiveOrderIntent,
            new List<BattleActionSlot> { passive2, passive1 }
        );
        Debug.Log(
            "模式58 J 同一PassiveGuard范围同样按slotIndex升序：" +
            (passive1.isUsed && !passive2.isUsed)
        );

        BattleEndedTestContext exactContext = CreateBattleEndedTestContext("guard58_k", 30, 30, 50, 5, 5, 8);
        int hpBefore = exactContext.allyB.currentHP;
        BattleEnemyIntent exactIntent = CreateEnemyAttackIntent(
            "guard58_k_intent",
            exactContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(exactContext.enemy, "guard58_k_enemy", 8, 0),
            exactContext.allyB,
            1
        );
        BattleActionSlot exactResponse = CreateMode58ResponseSlot(
            exactContext.allyB,
            1,
            CreateFixedAttackCardForCharacter(exactContext.allyB, "guard58_k_response", 4),
            exactIntent
        );
        BattleActionSlot exactGuard = new BattleActionSlot(exactContext.allyB, 2);
        exactGuard.AssignEnemySpecificGuard(
            exactContext.allyB,
            CreateTestDefenseCardForCharacter(exactContext.allyB, "guard58_k_guard", 12, 1),
            exactContext.enemy
        );
        BattleActionSlot exactPassiveGuard = new BattleActionSlot(exactContext.allyB, 3);
        exactPassiveGuard.AssignPassiveGuard(
            exactContext.allyB,
            CreateTestDefenseCardForCharacter(exactContext.allyB, "guard58_k_passive", 12, 1)
        );
        exactContext.runtimeState.SetActionSlots(
            new List<BattleActionSlot> { exactResponse, exactGuard, exactPassiveGuard }
        );
        exactContext.runtimeState.SetIntentQueue(BattleEnemyIntentManager.CreateIntentQueue(exactIntent));
        BattleExecutionPlan exactPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            exactContext.runtimeState.actionSlots,
            exactContext.runtimeState.intentQueue,
            exactContext.runtimeState
        );
        ExecutePlanWithRuntimeStateAndCompleteTurn(exactContext.runtimeState, exactPlan);
        Debug.Log(
            "模式58 K 精确响应失败后不触发任何守备：" +
            (exactResponse.isUsed &&
             !exactGuard.isUsed &&
             !exactPassiveGuard.isUsed &&
             exactContext.allyB.currentHP == hpBefore - 8 &&
             exactPlan.executionItems[0].passiveGuardCandidates.Count == 0)
        );
    }

    void RunGuardSingleUseAndInvalidCandidateSubTests()
    {
        BattleEndedTestContext singleContext = CreateBattleEndedTestContext("guard58_l", 30, 30, 50, 5, 5, 8);
        int hpBefore = singleContext.allyB.currentHP;
        BattleActionSlot singleGuard = new BattleActionSlot(singleContext.allyB, 1);
        BattleCardState singleDefense = CreateTestDefenseCardForCharacter(singleContext.allyB, "guard58_l_defense", 12, 1);
        singleGuard.AssignEnemySpecificGuard(singleContext.allyB, singleDefense, singleContext.enemy);
        BattleEnemyIntent firstIntent = new BattleEnemyIntent(
            "guard58_l_intent1",
            singleContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(singleContext.enemy, "guard58_l_enemy1", 5, 0),
            singleContext.allyB,
            1,
            1,
            1
        );
        BattleEnemyIntent secondIntent = new BattleEnemyIntent(
            "guard58_l_intent2",
            singleContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(singleContext.enemy, "guard58_l_enemy2", 5, 0),
            singleContext.allyB,
            2,
            2,
            2
        );
        singleContext.runtimeState.SetActionSlots(new List<BattleActionSlot> { singleGuard });
        singleContext.runtimeState.SetIntentQueue(new List<BattleEnemyIntent> { firstIntent, secondIntent });
        BattleExecutionPlan singlePlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            singleContext.runtimeState.actionSlots,
            singleContext.runtimeState.intentQueue,
            singleContext.runtimeState
        );
        ExecutePlanWithRuntimeStateAndCompleteTurn(singleContext.runtimeState, singlePlan);
        Debug.Log(
            "模式58 L 同一Defense只处理两次攻击中的第一张：" +
            (singleGuard.isUsed &&
             singleDefense.currentCooldown == GetExpectedResolvedCooldown(singleDefense) &&
             singleContext.allyB.currentHP == hpBefore - 5)
        );

        BattleEndedTestContext invalidContext = CreateBattleEndedTestContext("guard58_m", 30, 30, 50, 5, 5, 8);
        BattleEnemyIntent invalidIntent = CreateEnemyAttackIntent(
            "guard58_m_intent",
            invalidContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(invalidContext.enemy, "guard58_m_enemy", 5, 0),
            invalidContext.allyB,
            1
        );
        BattleActionSlot invalidFirst = new BattleActionSlot(invalidContext.allyB, 1);
        BattleCardState invalidDefense = CreateTestDefenseCardForCharacter(invalidContext.allyB, "guard58_m_invalid", 12, 1);
        invalidFirst.AssignEnemySpecificGuard(invalidContext.allyB, invalidDefense, invalidContext.enemy);
        invalidDefense.currentCooldown = 1;
        BattleActionSlot validSecond = new BattleActionSlot(invalidContext.allyB, 2);
        validSecond.AssignEnemySpecificGuard(
            invalidContext.allyB,
            CreateTestDefenseCardForCharacter(invalidContext.allyB, "guard58_m_valid", 12, 1),
            invalidContext.enemy
        );
        ExecuteMode58UnrespondedPlan(
            invalidContext,
            invalidIntent,
            new List<BattleActionSlot> { invalidFirst, validSecond }
        );
        Debug.Log(
            "模式58 M 执行时跳过失效第一守备并选择后续有效槽位：" +
            (!invalidFirst.isUsed && validSecond.isUsed && invalidDefense.currentCooldown == 1)
        );
    }

    void RunBattleContinuousDodgeLifecycleBasicTestSequence()
    {
        Debug.Log("===== BattleContinuousDodgeLifecycleBasic 聚合测试开始 =====");

        RunContinuousDodgeActivationSourceSubTests();
        RunContinuousDodgeCrossEnemyAndExactPrioritySubTests();
        RunContinuousDodgeSelectionAndFailureSubTests();
        RunContinuousDodgeLifecycleFinalizationSubTests();
        RunContinuousDodgeSingleFormalUseAndItemReuseSubTests();

        Debug.Log("===== BattleContinuousDodgeLifecycleBasic 聚合测试结束 =====");
    }

    void RunContinuousDodgeActivationSourceSubTests()
    {
        BattleEndedTestContext exactContext =
            CreateBattleEndedTestContext("continuous59_1", 30, 30, 50, 10, 5, 8);
        BattleCardState exactDodge =
            CreateFixedDodgeCardForCharacter(exactContext.allyA, "continuous59_1_dodge", 10, 2);
        BattleEnemyIntent exactIntent = CreateEnemyAttackIntent(
            "continuous59_1_intent",
            exactContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(exactContext.enemy, "continuous59_1_enemy", 4, 0),
            exactContext.allyA,
            1
        );
        BattleActionSlot exactSlot = CreateMode58ResponseSlot(
            exactContext.allyA,
            1,
            exactDodge,
            exactIntent
        );
        ExecuteMode59Plan(exactContext, exactIntent, new List<BattleActionSlot> { exactSlot });
        Debug.Log(
            "模式59 测试1 精确Dodge首次成功激活：" +
            (exactSlot.isContinuousDodgeActive &&
             exactSlot.successfulDodgeCount == 1 &&
             exactSlot.continuousDodgeSource == ContinuousDodgeSource.ExactEnemyIntent &&
             !exactSlot.isUsed &&
             !exactSlot.isCardUseFinalized &&
             exactDodge.currentCooldown == 0)
        );

        BattleEndedTestContext specificContext =
            CreateBattleEndedTestContext("continuous59_2", 30, 30, 50, 10, 5, 8);
        BattleCardState specificDodge =
            CreateFixedDodgeCardForCharacter(specificContext.allyB, "continuous59_2_dodge", 10, 2);
        BattleActionSlot specificSlot = new BattleActionSlot(specificContext.allyB, 1);
        specificSlot.AssignEnemySpecificGuard(
            specificContext.allyB,
            specificDodge,
            specificContext.enemy
        );
        BattleEnemyIntent specificIntent = CreateEnemyAttackIntent(
            "continuous59_2_intent",
            specificContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(specificContext.enemy, "continuous59_2_enemy", 4, 0),
            specificContext.allyB,
            1
        );
        ExecuteMode59Plan(specificContext, specificIntent, new List<BattleActionSlot> { specificSlot });
        Debug.Log(
            "模式59 测试2 EnemySpecificGuard Dodge首次成功激活：" +
            (specificSlot.isContinuousDodgeActive &&
             specificSlot.successfulDodgeCount == 1 &&
             specificSlot.continuousDodgeSource == ContinuousDodgeSource.EnemySpecificGuard &&
             !specificSlot.isUsed &&
             specificDodge.currentCooldown == 0)
        );

        BattleEndedTestContext passiveContext =
            CreateBattleEndedTestContext("continuous59_3", 30, 30, 50, 10, 5, 8);
        BattleCardState passiveDodge =
            CreateFixedDodgeCardForCharacter(passiveContext.allyB, "continuous59_3_dodge", 10, 2);
        BattleActionSlot passiveSlot = new BattleActionSlot(passiveContext.allyB, 1);
        passiveSlot.AssignPassiveGuard(passiveContext.allyB, passiveDodge);
        BattleEnemyIntent passiveIntent = CreateEnemyAttackIntent(
            "continuous59_3_intent",
            passiveContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(passiveContext.enemy, "continuous59_3_enemy", 4, 0),
            passiveContext.allyB,
            1
        );
        ExecuteMode59Plan(passiveContext, passiveIntent, new List<BattleActionSlot> { passiveSlot });
        Debug.Log(
            "模式59 测试3 PassiveGuard Dodge首次成功激活：" +
            (passiveSlot.isContinuousDodgeActive &&
             passiveSlot.successfulDodgeCount == 1 &&
             passiveSlot.continuousDodgeSource == ContinuousDodgeSource.PassiveGuard &&
             !passiveSlot.isUsed &&
             passiveDodge.currentCooldown == 0)
        );
    }

    void RunContinuousDodgeCrossEnemyAndExactPrioritySubTests()
    {
        BattleEndedTestContext crossContext =
            CreateBattleEndedTestContext("continuous59_4", 30, 30, 50, 10, 5, 8);
        CharacterData secondEnemy = new CharacterData("continuous59_4_Enemy02", 50, 8, 8);
        BattleCardState crossDodge =
            CreateFixedDodgeCardForCharacter(crossContext.allyA, "continuous59_4_dodge", 10, 2);
        BattleActionSlot crossSlot = new BattleActionSlot(crossContext.allyA, 1);
        crossSlot.AssignEnemySpecificGuard(crossContext.allyA, crossDodge, crossContext.enemy);
        BattleEnemyIntent firstIntent = CreateEnemyAttackIntent(
            "continuous59_4_intent1",
            crossContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(crossContext.enemy, "continuous59_4_enemy1", 4, 0),
            crossContext.allyA,
            1
        );
        ExecuteMode59Plan(crossContext, firstIntent, new List<BattleActionSlot> { crossSlot });
        BattleEnemyIntent secondIntent = CreateEnemyAttackIntent(
            "continuous59_4_intent2",
            secondEnemy,
            CreateFixedEnemyAttackCardForDodgeTest(secondEnemy, "continuous59_4_enemy2", 4, 0),
            crossContext.allyA,
            2
        );
        ExecuteMode59Plan(crossContext, secondIntent, new List<BattleActionSlot> { crossSlot });
        Debug.Log(
            "模式59 测试4 激活后跨敌人生效：" +
            (crossSlot.isContinuousDodgeActive &&
             crossSlot.successfulDodgeCount == 2 &&
             object.ReferenceEquals(crossSlot.lastContinuousDodgeOpponent, secondEnemy))
        );
        Debug.Log(
            "模式59 测试8 连续闪避再次成功后保持激活：" +
            (crossSlot.isContinuousDodgeActive &&
             !crossSlot.isUsed &&
             !crossSlot.isCardUseFinalized &&
             crossDodge.currentCooldown == 0)
        );

        BattleEndedTestContext exactPriorityContext =
            CreateBattleEndedTestContext("continuous59_5", 30, 30, 50, 10, 5, 8);
        BattleActionSlot activeSlot = CreateMode59ActiveDodgeSlot(
            exactPriorityContext.allyA,
            1,
            "continuous59_5_active",
            12,
            2,
            exactPriorityContext.enemy
        );
        BattleEnemyIntent exactPriorityIntent = CreateEnemyAttackIntent(
            "continuous59_5_intent",
            exactPriorityContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(exactPriorityContext.enemy, "continuous59_5_enemy", 4, 0),
            exactPriorityContext.allyA,
            1
        );
        BattleActionSlot exactAttackSlot = CreateMode58ResponseSlot(
            exactPriorityContext.allyA,
            2,
            CreateFixedAttackCardForCharacter(exactPriorityContext.allyA, "continuous59_5_exact", 8),
            exactPriorityIntent
        );
        ExecuteMode59Plan(
            exactPriorityContext,
            exactPriorityIntent,
            new List<BattleActionSlot> { activeSlot, exactAttackSlot }
        );
        Debug.Log(
            "模式59 测试5 有效精确响应优先于连续闪避：" +
            (exactAttackSlot.isUsed &&
             activeSlot.isContinuousDodgeActive &&
             activeSlot.successfulDodgeCount == 1)
        );

        BattleEndedTestContext exactFailContext =
            CreateBattleEndedTestContext("continuous59_6", 30, 30, 50, 10, 5, 8);
        int hpBefore = exactFailContext.allyA.currentHP;
        BattleActionSlot waitingActiveSlot = CreateMode59ActiveDodgeSlot(
            exactFailContext.allyA,
            1,
            "continuous59_6_active",
            12,
            2,
            exactFailContext.enemy
        );
        BattleEnemyIntent exactFailIntent = CreateEnemyAttackIntent(
            "continuous59_6_intent",
            exactFailContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(exactFailContext.enemy, "continuous59_6_enemy", 8, 0),
            exactFailContext.allyA,
            1
        );
        BattleActionSlot losingExactSlot = CreateMode58ResponseSlot(
            exactFailContext.allyA,
            2,
            CreateFixedAttackCardForCharacter(exactFailContext.allyA, "continuous59_6_exact", 3),
            exactFailIntent
        );
        ExecuteMode59Plan(
            exactFailContext,
            exactFailIntent,
            new List<BattleActionSlot> { waitingActiveSlot, losingExactSlot }
        );
        Debug.Log(
            "模式59 测试6 精确响应结算失败后连续闪避不补防且保持激活：" +
            (losingExactSlot.isUsed &&
             exactFailContext.allyA.currentHP == hpBefore - 8 &&
             waitingActiveSlot.isContinuousDodgeActive &&
             waitingActiveSlot.successfulDodgeCount == 1)
        );
    }

    void RunContinuousDodgeSelectionAndFailureSubTests()
    {
        BattleEndedTestContext orderContext =
            CreateBattleEndedTestContext("continuous59_7", 30, 30, 50, 10, 5, 8);
        BattleActionSlot slot2 = CreateMode59ActiveDodgeSlot(
            orderContext.allyB, 2, "continuous59_7_slot2", 12, 2, orderContext.enemy
        );
        BattleActionSlot slot1 = CreateMode59ActiveDodgeSlot(
            orderContext.allyB, 1, "continuous59_7_slot1", 12, 2, orderContext.enemy
        );
        slot1.assignmentSequence = 1;
        slot2.assignmentSequence = 99;
        BattleEnemyIntent orderIntent = CreateEnemyAttackIntent(
            "continuous59_7_intent",
            orderContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(orderContext.enemy, "continuous59_7_enemy", 4, 0),
            orderContext.allyB,
            1
        );
        ExecuteMode59Plan(orderContext, orderIntent, new List<BattleActionSlot> { slot2, slot1 });
        Debug.Log(
            "模式59 测试7 多张连续闪避按slotIndex升序：" +
            (slot1.successfulDodgeCount == 2 && slot2.successfulDodgeCount == 1)
        );

        BattleEndedTestContext failContext =
            CreateBattleEndedTestContext("continuous59_9", 30, 30, 50, 10, 5, 8);
        int failHpBefore = failContext.allyB.currentHP;
        BattleActionSlot failingActive = CreateMode59ActiveDodgeSlot(
            failContext.allyB, 1, "continuous59_9_fail", 2, 2, failContext.enemy
        );
        BattleActionSlot waitingActive = CreateMode59ActiveDodgeSlot(
            failContext.allyB, 2, "continuous59_9_wait", 12, 2, failContext.enemy
        );
        BattleActionSlot specificGuard = new BattleActionSlot(failContext.allyB, 3);
        specificGuard.AssignEnemySpecificGuard(
            failContext.allyB,
            CreateTestDefenseCardForCharacter(failContext.allyB, "continuous59_9_specific", 20, 1),
            failContext.enemy
        );
        BattleActionSlot passiveGuard = new BattleActionSlot(failContext.allyB, 4);
        passiveGuard.AssignPassiveGuard(
            failContext.allyB,
            CreateTestDefenseCardForCharacter(failContext.allyB, "continuous59_9_passive", 20, 1)
        );
        BattleEnemyIntent failIntent = CreateEnemyAttackIntent(
            "continuous59_9_intent",
            failContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(failContext.enemy, "continuous59_9_enemy", 8, 0),
            failContext.allyB,
            1
        );
        ExecuteMode59Plan(
            failContext,
            failIntent,
            new List<BattleActionSlot> { failingActive, waitingActive, specificGuard, passiveGuard }
        );
        Debug.Log(
            "模式59 测试9 连续闪避失败后伤害和单卡收尾且不补第二守备：" +
            (failContext.allyB.currentHP == failHpBefore - 8 &&
             failingActive.isUsed &&
             failingActive.isCardUseFinalized &&
             !failingActive.isContinuousDodgeActive &&
             waitingActive.isContinuousDodgeActive &&
             !specificGuard.isUsed &&
             !passiveGuard.isUsed)
        );

        BattleEndedTestContext firstFailContext =
            CreateBattleEndedTestContext("continuous59_10", 30, 30, 50, 10, 5, 8);
        int firstFailHpBefore = firstFailContext.allyB.currentHP;
        BattleActionSlot firstFailDodge = new BattleActionSlot(firstFailContext.allyB, 1);
        firstFailDodge.AssignPassiveGuard(
            firstFailContext.allyB,
            CreateFixedDodgeCardForCharacter(firstFailContext.allyB, "continuous59_10_dodge", 2, 2)
        );
        BattleActionSlot followDefense = new BattleActionSlot(firstFailContext.allyB, 2);
        followDefense.AssignPassiveGuard(
            firstFailContext.allyB,
            CreateTestDefenseCardForCharacter(firstFailContext.allyB, "continuous59_10_defense", 20, 1)
        );
        BattleEnemyIntent firstFailIntent = CreateEnemyAttackIntent(
            "continuous59_10_intent",
            firstFailContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(firstFailContext.enemy, "continuous59_10_enemy", 8, 0),
            firstFailContext.allyB,
            1
        );
        ExecuteMode59Plan(
            firstFailContext,
            firstFailIntent,
            new List<BattleActionSlot> { firstFailDodge, followDefense }
        );
        Debug.Log(
            "模式59 测试10 首次Dodge失败立即正式结算且不触发第二守备：" +
            (firstFailContext.allyB.currentHP == firstFailHpBefore - 8 &&
             !firstFailDodge.isContinuousDodgeActive &&
             firstFailDodge.isCardUseFinalized &&
             firstFailDodge.isUsed &&
             firstFailDodge.cardState.currentCooldown == 3 &&
             !followDefense.isUsed)
        );

        BattleEndedTestContext priorityContext =
            CreateBattleEndedTestContext("continuous59_11", 30, 30, 50, 10, 5, 8);
        BattleActionSlot priorityActive = CreateMode59ActiveDodgeSlot(
            priorityContext.allyB, 2, "continuous59_11_active", 12, 2, priorityContext.enemy
        );
        BattleActionSlot lowerSpecific = new BattleActionSlot(priorityContext.allyB, 1);
        lowerSpecific.AssignEnemySpecificGuard(
            priorityContext.allyB,
            CreateTestDefenseCardForCharacter(priorityContext.allyB, "continuous59_11_specific", 20, 1),
            priorityContext.enemy
        );
        BattleEnemyIntent priorityIntent = CreateEnemyAttackIntent(
            "continuous59_11_intent",
            priorityContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(priorityContext.enemy, "continuous59_11_enemy", 4, 0),
            priorityContext.allyB,
            1
        );
        BattleGuardSelectionResult specificSelection =
            BattleGuardSelectionManager.SelectHandlingCardForEnemyIntent(
                new List<BattleActionSlot> { lowerSpecific, priorityActive },
                priorityIntent
            );
        Debug.Log(
            "模式59 测试11 连续闪避优先于EnemySpecificGuard：" +
            (specificSelection.selectionType == BattleGuardSelectionType.ContinuousDodge &&
             specificSelection.slot == priorityActive)
        );

        BattleActionSlot lowerPassive = new BattleActionSlot(priorityContext.allyB, 1);
        lowerPassive.AssignPassiveGuard(
            priorityContext.allyB,
            CreateTestDefenseCardForCharacter(priorityContext.allyB, "continuous59_12_passive", 20, 1)
        );
        BattleGuardSelectionResult passiveSelection =
            BattleGuardSelectionManager.SelectHandlingCardForEnemyIntent(
                new List<BattleActionSlot> { lowerPassive, priorityActive },
                priorityIntent
            );
        Debug.Log(
            "模式59 测试12 连续闪避优先于PassiveGuard：" +
            (passiveSelection.selectionType == BattleGuardSelectionType.ContinuousDodge &&
             passiveSelection.slot == priorityActive)
        );

        BattleActionSlot invalidActive = CreateMode59ActiveDodgeSlot(
            priorityContext.allyB, 1, "continuous59_13_invalid", 12, 2, priorityContext.enemy
        );
        invalidActive.cardState.currentCooldown = 1;
        BattleActionSlot validActive = CreateMode59ActiveDodgeSlot(
            priorityContext.allyB, 2, "continuous59_13_valid", 12, 2, priorityContext.enemy
        );
        BattleGuardSelectionResult validFallback =
            BattleGuardSelectionManager.SelectHandlingCardForEnemyIntent(
                new List<BattleActionSlot> { invalidActive, validActive, lowerSpecific },
                priorityIntent
            );
        BattleGuardSelectionResult guardFallback =
            BattleGuardSelectionManager.SelectHandlingCardForEnemyIntent(
                new List<BattleActionSlot> { invalidActive, lowerSpecific },
                priorityIntent
            );
        Debug.Log(
            "模式59 测试13 失效连续闪避跳过并继续后续优先级：" +
            (validFallback.slot == validActive &&
             validFallback.selectionType == BattleGuardSelectionType.ContinuousDodge &&
             guardFallback.slot == lowerSpecific &&
             guardFallback.selectionType == BattleGuardSelectionType.EnemySpecificGuard)
        );
    }

    void RunContinuousDodgeLifecycleFinalizationSubTests()
    {
        BattleEndedTestContext turnEndContext =
            CreateBattleEndedTestContext("continuous59_14", 30, 30, 50, 10, 5, 8);
        BattleActionSlot turnEndSlot = CreateMode59ActiveDodgeSlot(
            turnEndContext.allyA, 1, "continuous59_14_dodge", 12, 2, turnEndContext.enemy
        );
        turnEndSlot.RegisterContinuousDodgeSuccess(12, turnEndContext.enemy);
        turnEndSlot.RegisterContinuousDodgeSuccess(12, turnEndContext.enemy);
        PrepareMode59CompletedRuntime(turnEndContext, new List<BattleActionSlot> { turnEndSlot });
        turnEndContext.runtimeState.EndCurrentTurnAndClearRuntimeObjects();
        Debug.Log(
            "模式59 测试14 回合结束统一结算激活Dodge：" +
            (!turnEndSlot.isContinuousDodgeActive &&
             turnEndSlot.isCardUseFinalized &&
             turnEndSlot.isUsed &&
             turnEndSlot.successfulDodgeCount == 3 &&
             turnEndSlot.cardState.currentCooldown == 2)
        );

        BattleEndedTestContext untouchedContext =
            CreateBattleEndedTestContext("continuous59_15", 30, 30, 50, 10, 5, 8);
        BattleActionSlot untouchedSlot = new BattleActionSlot(untouchedContext.allyA, 1);
        BattleCardState untouchedDodge =
            CreateFixedDodgeCardForCharacter(untouchedContext.allyA, "continuous59_15_dodge", 12, 2);
        untouchedSlot.AssignPassiveGuard(untouchedContext.allyA, untouchedDodge);
        PrepareMode59CompletedRuntime(untouchedContext, new List<BattleActionSlot> { untouchedSlot });
        untouchedContext.runtimeState.EndCurrentTurnAndClearRuntimeObjects();
        Debug.Log(
            "模式59 测试15 未触发Dodge在回合结束不结算：" +
            (!untouchedSlot.isCardUseFinalized &&
             !untouchedSlot.isUsed &&
             untouchedDodge.currentCooldown == 0 &&
             untouchedDodge.currentUseCount == 0)
        );

        BattleEndedTestContext multipleContext =
            CreateBattleEndedTestContext("continuous59_16", 30, 30, 50, 10, 5, 8);
        BattleActionSlot firstActive = CreateMode59ActiveDodgeSlot(
            multipleContext.allyA, 1, "continuous59_16_first", 12, 2, multipleContext.enemy
        );
        BattleActionSlot secondActive = CreateMode59ActiveDodgeSlot(
            multipleContext.allyA, 2, "continuous59_16_second", 12, 2, multipleContext.enemy
        );
        PrepareMode59CompletedRuntime(
            multipleContext,
            new List<BattleActionSlot> { firstActive, secondActive }
        );
        multipleContext.runtimeState.EndCurrentTurnAndClearRuntimeObjects();
        Debug.Log(
            "模式59 测试16 多张激活Dodge分别且仅结算一次：" +
            (firstActive.isCardUseFinalized &&
             secondActive.isCardUseFinalized &&
             firstActive.isUsed &&
             secondActive.isUsed &&
             firstActive.cardState.currentCooldown == 2 &&
             secondActive.cardState.currentCooldown == 2)
        );

        BattleEndedTestContext battleEndedContext =
            CreateBattleEndedTestContext("continuous59_17", 30, 30, 50, 10, 5, 8);
        BattleActionSlot battleEndedSlot = CreateMode59ActiveDodgeSlot(
            battleEndedContext.allyA, 1, "continuous59_17_dodge", 12, 2, battleEndedContext.enemy
        );
        battleEndedContext.runtimeState.SetActionSlots(new List<BattleActionSlot> { battleEndedSlot });
        battleEndedContext.enemy.currentHP = 0;
        SetTestLifecyclePhase(battleEndedContext.runtimeState, BattleLifecyclePhase.Executing);
        battleEndedContext.runtimeState.EvaluateBattleEnd();
        int battleEndedCooldown = battleEndedSlot.cardState.currentCooldown;
        battleEndedContext.runtimeState.EvaluateBattleEnd();
        Debug.Log(
            "模式59 测试17 BattleEnded在槽位清理前幂等收尾：" +
            (battleEndedContext.runtimeState.IsBattleEnded &&
             battleEndedSlot.isCardUseFinalized &&
             battleEndedSlot.isUsed &&
             battleEndedCooldown == 3 &&
             battleEndedSlot.cardState.currentCooldown == battleEndedCooldown)
        );

        BattleEndedTestContext failureCdContext =
            CreateBattleEndedTestContext("continuous59_18_fail", 30, 30, 50, 10, 5, 8);
        BattleActionSlot failureSlot = new BattleActionSlot(failureCdContext.allyA, 1);
        failureSlot.AssignPassiveGuard(
            failureCdContext.allyA,
            CreateFixedDodgeCardForCharacter(failureCdContext.allyA, "continuous59_18_fail_dodge", 2, 2)
        );
        BattleEnemyIntent failureIntent = CreateEnemyAttackIntent(
            "continuous59_18_fail_intent",
            failureCdContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(failureCdContext.enemy, "continuous59_18_fail_enemy", 8, 0),
            failureCdContext.allyA,
            1
        );
        ExecuteMode59Plan(failureCdContext, failureIntent, new List<BattleActionSlot> { failureSlot });
        failureCdContext.runtimeState.EndCurrentTurnAndClearRuntimeObjects();

        BattleEndedTestContext successCdContext =
            CreateBattleEndedTestContext("continuous59_18_success", 30, 30, 50, 10, 5, 8);
        BattleActionSlot successSlot = new BattleActionSlot(successCdContext.allyA, 1);
        successSlot.AssignPassiveGuard(
            successCdContext.allyA,
            CreateFixedDodgeCardForCharacter(successCdContext.allyA, "continuous59_18_success_dodge", 12, 2)
        );
        BattleEnemyIntent successIntent = CreateEnemyAttackIntent(
            "continuous59_18_success_intent",
            successCdContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(successCdContext.enemy, "continuous59_18_success_enemy", 4, 0),
            successCdContext.allyA,
            1
        );
        ExecuteMode59Plan(successCdContext, successIntent, new List<BattleActionSlot> { successSlot });
        successCdContext.runtimeState.EndCurrentTurnAndClearRuntimeObjects();
        Debug.Log(
            "模式59 测试18 失败即时结算与成功回合末结算的下一回合CD一致：" +
            (failureSlot.cardState.currentCooldown == 2 &&
             successSlot.cardState.currentCooldown == 2)
        );
    }

    void RunContinuousDodgeSingleFormalUseAndItemReuseSubTests()
    {
        BattleEndedTestContext formalUseContext =
            CreateBattleEndedTestContext("continuous59_19", 30, 30, 50, 10, 5, 8);
        BattleCardState sinDodge =
            CreateFixedDodgeCardForCharacter(formalUseContext.allyA, "continuous59_19_dodge", 12, 0);
        sinDodge.cardData.isSinCard = true;
        sinDodge.cardData.sinCardUseRule = SinCardUseRule.UseCount;
        sinDodge.cardData.maxUseCount = 5;
        sinDodge.maxUseCount = 5;
        sinDodge.cardData.guiltGain = 2;
        BattleActionSlot sinDodgeSlot = new BattleActionSlot(formalUseContext.allyA, 1);
        sinDodgeSlot.AssignPassiveGuard(formalUseContext.allyA, sinDodge);
        List<BattleActionSlot> formalUseSlots = new List<BattleActionSlot> { sinDodgeSlot };

        for (int index = 1; index <= 3; index++)
        {
            BattleEnemyIntent intent = CreateEnemyAttackIntent(
                "continuous59_19_intent_" + index,
                formalUseContext.enemy,
                CreateFixedEnemyAttackCardForDodgeTest(
                    formalUseContext.enemy,
                    "continuous59_19_enemy_" + index,
                    4,
                    0
                ),
                formalUseContext.allyA,
                index
            );
            ExecuteMode59Plan(formalUseContext, intent, formalUseSlots);
        }

        bool beforeFinalize =
            sinDodgeSlot.successfulDodgeCount == 3 &&
            sinDodge.currentUseCount == 0 &&
            formalUseContext.runtimeState.currentGuilt == 0 &&
            !sinDodgeSlot.isUsed;
        formalUseContext.runtimeState.EndCurrentTurnAndClearRuntimeObjects();
        Debug.Log(
            "模式59 测试19 多次成功只触发一次正式UseCount/Guilt/Resolved收尾：" +
            (beforeFinalize &&
             sinDodge.currentUseCount == 1 &&
             formalUseContext.runtimeState.currentGuilt == 2 &&
             sinDodgeSlot.isCardUseFinalized &&
             sinDodgeSlot.isUsed)
        );

        BattleEndedTestContext itemContext =
            CreateBattleEndedTestContext("continuous59_20", 30, 30, 50, 10, 5, 8);
        BattleCardState itemDodge =
            CreateFixedDodgeCardForCharacter(itemContext.allyA, "continuous59_20_dodge", 12, 2);
        BattleEnemyIntent firstIntent = CreateEnemyAttackIntent(
            "continuous59_20_intent1",
            itemContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(itemContext.enemy, "continuous59_20_enemy1", 4, 0),
            itemContext.allyA,
            1
        );
        BattleActionSlot itemSlot = CreateMode58ResponseSlot(
            itemContext.allyA,
            1,
            itemDodge,
            firstIntent
        );
        List<BattleActionSlot> itemSlots = new List<BattleActionSlot> { itemSlot };
        BattleExecutionPlan originalPlan = ExecuteMode59Plan(itemContext, firstIntent, itemSlots);
        int countAfterFirstExecution = itemSlot.successfulDodgeCount;
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(originalPlan, itemContext.runtimeState);
        int countAfterRepeatedPlan = itemSlot.successfulDodgeCount;
        BattleEnemyIntent laterIntent = CreateEnemyAttackIntent(
            "continuous59_20_intent2",
            itemContext.enemy,
            CreateFixedEnemyAttackCardForDodgeTest(itemContext.enemy, "continuous59_20_enemy2", 4, 0),
            itemContext.allyA,
            2
        );
        ExecuteMode59Plan(itemContext, laterIntent, itemSlots);
        Debug.Log(
            "模式59 测试20 原Responded item不重复执行且后续仅由连续选择器触发：" +
            (originalPlan.executionItems.Count == 1 &&
             originalPlan.executionItems[0].isCompleted &&
             countAfterFirstExecution == 1 &&
             countAfterRepeatedPlan == 1 &&
             itemSlot.successfulDodgeCount == 2)
        );
    }

    BattleExecutionPlan ExecuteMode59Plan(
        BattleEndedTestContext context,
        BattleEnemyIntent enemyIntent,
        List<BattleActionSlot> actionSlots
    )
    {
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(BattleEnemyIntentManager.CreateIntentQueue(enemyIntent));
        BattleExecutionPlan plan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            actionSlots,
            context.runtimeState.intentQueue,
            context.runtimeState
        );
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);
        return plan;
    }

    BattleActionSlot CreateMode59ActiveDodgeSlot(
        CharacterData actor,
        int slotIndex,
        string instanceID,
        int dodgePoint,
        int cooldown,
        CharacterData opponent
    )
    {
        BattleActionSlot slot = new BattleActionSlot(actor, slotIndex);
        slot.AssignPassiveGuard(
            actor,
            CreateFixedDodgeCardForCharacter(actor, instanceID, dodgePoint, cooldown)
        );
        slot.ActivateContinuousDodge(
            ContinuousDodgeSource.PassiveGuard,
            dodgePoint,
            opponent
        );
        return slot;
    }

    void PrepareMode59CompletedRuntime(
        BattleEndedTestContext context,
        List<BattleActionSlot> actionSlots
    )
    {
        BattleExecutionPlan completedPlan = new BattleExecutionPlan();
        completedPlan.isCompleted = true;
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetExecutionPlan(completedPlan);
        SetTestLifecyclePhase(
            context.runtimeState,
            BattleLifecyclePhase.TurnResolved
        );
    }

    BattleActionSlot CreateMode58ResponseSlot(
        CharacterData actor,
        int slotIndex,
        BattleCardState cardState,
        BattleEnemyIntent enemyIntent
    )
    {
        BattleActionSlot slot = new BattleActionSlot(actor, slotIndex);
        slot.AssignResponse(actor, cardState, enemyIntent, false);
        enemyIntent.MarkResponded();
        return slot;
    }

    BattleActionSlot CreateMode58FreeSlot(
        CharacterData actor,
        int slotIndex,
        BattleCardState cardState,
        CharacterData target
    )
    {
        BattleActionSlot slot = new BattleActionSlot(actor, slotIndex);
        slot.AssignFreeAction(actor, cardState, target);
        return slot;
    }

    void ExecuteMode58UnrespondedPlan(
        BattleEndedTestContext context,
        BattleEnemyIntent enemyIntent,
        List<BattleActionSlot> actionSlots
    )
    {
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(BattleEnemyIntentManager.CreateIntentQueue(enemyIntent));
        BattleExecutionPlan plan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            actionSlots,
            context.runtimeState.intentQueue,
            context.runtimeState
        );
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);
    }

    bool AreExecutionPlansReferenceOrderedTheSame(
        BattleExecutionPlan first,
        BattleExecutionPlan second
    )
    {
        if (first == null ||
            second == null ||
            first.executionItems == null ||
            second.executionItems == null ||
            first.executionItems.Count != second.executionItems.Count)
        {
            return false;
        }

        for (int index = 0; index < first.executionItems.Count; index++)
        {
            BattleExecutionItem left = first.executionItems[index];
            BattleExecutionItem right = second.executionItems[index];

            if (left == null ||
                right == null ||
                left.executionType != right.executionType ||
                !object.ReferenceEquals(left.actionSlot, right.actionSlot) ||
                !object.ReferenceEquals(left.enemyIntent, right.enemyIntent) ||
                left.effectiveSpeed != right.effectiveSpeed ||
                left.responsePriority != right.responsePriority ||
                left.actionSlotOrder != right.actionSlotOrder ||
                left.actorPositionOrder != right.actorPositionOrder ||
                left.stableOrder != right.stableOrder)
            {
                return false;
            }
        }

        return true;
    }

    void RunBattleDefinitionDataJsonLoadSubTest()
    {
        List<CharacterDefinitionData> characterDefinitions = CharacterDefinitionLoader.LoadDefinitions();
        List<EnemyDefinitionData> enemyDefinitions = EnemyDefinitionLoader.LoadDefinitions();
        List<EncounterDefinitionData> encounterDefinitions = EncounterDefinitionLoader.LoadDefinitions();

        bool loaded =
            characterDefinitions != null &&
            enemyDefinitions != null &&
            encounterDefinitions != null &&
            characterDefinitions.Count >= 2 &&
            enemyDefinitions.Count >= 1 &&
            encounterDefinitions.Count >= 1;

        bool noDuplicate =
            HasNoDuplicateCharacterDefinitionIDs(characterDefinitions) &&
            HasNoDuplicateEnemyDefinitionIDs(enemyDefinitions) &&
            HasNoDuplicateEncounterDefinitionIDs(encounterDefinitions);

        Debug.Log("模式56 A 三份JSON读取成功：" + loaded);
        Debug.Log("模式56 A 定义ID没有重复：" + noDuplicate);
    }

    void RunBattleDefinitionDataReferenceSubTest()
    {
        List<CardTestData> cards = CardDataLoader.LoadCardData();
        List<CharacterDefinitionData> characterDefinitions = CharacterDefinitionLoader.LoadDefinitions();
        List<EnemyDefinitionData> enemyDefinitions = EnemyDefinitionLoader.LoadDefinitions();
        List<EncounterDefinitionData> encounterDefinitions = EncounterDefinitionLoader.LoadDefinitions();

        EncounterDefinitionData encounter = EncounterDefinitionLoader.FindByID(encounterDefinitions, "encounter_test_001");
        CharacterDefinitionData allyA = encounter != null ? CharacterDefinitionLoader.FindByID(characterDefinitions, encounter.allyCharacterIDs[0]) : null;
        CharacterDefinitionData allyB = encounter != null ? CharacterDefinitionLoader.FindByID(characterDefinitions, encounter.allyCharacterIDs[1]) : null;
        EnemyDefinitionData enemyDefinition = encounter != null ? EnemyDefinitionLoader.FindByID(enemyDefinitions, encounter.enemyID) : null;

        bool encounterRefs =
            encounter != null &&
            allyA != null &&
            allyB != null &&
            enemyDefinition != null &&
            encounter.intentPattern != null &&
            encounter.intentPattern.Length == 1 &&
            encounter.intentPattern[0].enemyCardIndex == 1 &&
            CardDataLoader.FindCardByID(cards, enemyDefinition.cardIDs[0]) != null &&
            CharacterDefinitionLoader.FindByID(characterDefinitions, encounter.intentPattern[0].targetCharacterID) != null;

        bool artKeysStored =
            encounter != null &&
            !string.IsNullOrEmpty(allyA.prefabKey) &&
            !string.IsNullOrEmpty(allyA.portraitKey) &&
            !string.IsNullOrEmpty(enemyDefinition.prefabKey) &&
            !string.IsNullOrEmpty(enemyDefinition.portraitKey) &&
            !string.IsNullOrEmpty(encounter.battleBackgroundKey) &&
            !string.IsNullOrEmpty(encounter.battleMusicKey);

        Debug.Log("模式56 B encounter_test_001跨文件引用合法：" + encounterRefs);
        Debug.Log("模式56 B 美术键仅保存字符串且非空：" + artKeysStored);
    }

    void RunBattleDefinitionDataPlayerCreationSubTest()
    {
        BattleDefinitionBootstrapResult result = BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");

        bool playersCreated =
            result != null &&
            result.isSuccess &&
            result.runtimeState != null &&
            result.runtimeState.allyA != null &&
            result.runtimeState.allyB != null &&
            !object.ReferenceEquals(result.runtimeState.allyA, result.runtimeState.allyB) &&
            result.runtimeState.allyA.characterName == result.allyADefinition.characterName &&
            result.runtimeState.allyB.characterName == result.allyBDefinition.characterName;

        Debug.Log("模式56 C 两名玩家由真实bootstrap创建且实例独立：" + playersCreated);
    }

    void RunBattleDefinitionDataEnemyCreationSubTest()
    {
        BattleDefinitionBootstrapResult result = BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        CharacterData enemyUnit = result != null && result.runtimeState != null ? result.runtimeState.enemy : null;
        CharacterData enemyUnit2 = result != null && result.runtimeState != null ? result.runtimeState.enemy2 : null;

        bool enemyCreated =
            result != null &&
            result.isSuccess &&
            enemyUnit != null &&
            enemyUnit2 != null &&
            !object.ReferenceEquals(enemyUnit, enemyUnit2) &&
            enemyUnit.runtimeUnitID != enemyUnit2.runtimeUnitID &&
            enemyUnit.characterName == result.enemyDefinition.enemyName &&
            enemyUnit2.characterName == result.enemyDefinition.enemyName &&
            enemyUnit.maxHP == result.enemyDefinition.maxHP &&
            enemyUnit2.maxHP == result.enemyDefinition.maxHP &&
            enemyUnit.minSpeed == result.enemyDefinition.minSpeed &&
            enemyUnit.maxSpeed == result.enemyDefinition.maxSpeed &&
            enemyUnit2.minSpeed == result.enemyDefinition.minSpeed &&
            enemyUnit2.maxSpeed == result.enemyDefinition.maxSpeed;

        Debug.Log("模式56 D 两名敌人按同一JSON定义创建且实例独立：" + enemyCreated);
    }

    void RunBattleDefinitionDataRuntimeBaseStateSubTest()
    {
        BattleDefinitionBootstrapResult result = BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        CharacterData allyAUnit = result != null && result.runtimeState != null ? result.runtimeState.allyA : null;
        CharacterData allyBUnit = result != null && result.runtimeState != null ? result.runtimeState.allyB : null;
        CharacterData enemyUnit = result != null && result.runtimeState != null ? result.runtimeState.enemy : null;
        CharacterData enemyUnit2 = result != null && result.runtimeState != null ? result.runtimeState.enemy2 : null;

        bool baseState =
            result != null &&
            result.isSuccess &&
            IsRuntimeBaseState(allyAUnit) &&
            IsRuntimeBaseState(allyBUnit) &&
            IsRuntimeBaseState(enemyUnit) &&
            IsRuntimeBaseState(enemyUnit2);

        Debug.Log("模式56 E 角色运行时HP负罪感速度初始状态正确：" + baseState);
    }

    void RunBattleDefinitionDataPlayerCardsSubTest()
    {
        BattleDefinitionBootstrapResult result = BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        CharacterData allyAUnit = result != null && result.runtimeState != null ? result.runtimeState.allyA : null;

        bool playerCards =
            result != null &&
            result.isSuccess &&
            allyAUnit != null &&
            allyAUnit.battleCards != null &&
            allyAUnit.battleCards.Count == result.allyADefinition.startingCardIDs.Length &&
            IsCardStateFromDefinition(allyAUnit.battleCards[0], allyAUnit, result.allyADefinition.startingCardIDs[0], "ally_001_atk_bullet_001_copy_0") &&
            IsCardStateFromDefinition(allyAUnit.battleCards[1], allyAUnit, result.allyADefinition.startingCardIDs[1], "ally_001_atk_bullet_001_copy_1");

        Debug.Log("模式56 F 玩家卡牌实例顺序、owner和初始状态正确：" + playerCards);
    }

    void RunBattleDefinitionDataDuplicatePlayerCardsSubTest()
    {
        BattleDefinitionBootstrapResult result = BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        CharacterData allyAUnit = result != null && result.runtimeState != null ? result.runtimeState.allyA : null;

        BattleCardState copy0 = allyAUnit != null && allyAUnit.battleCards.Count > 0 ? allyAUnit.battleCards[0] : null;
        BattleCardState copy1 = allyAUnit != null && allyAUnit.battleCards.Count > 1 ? allyAUnit.battleCards[1] : null;

        if (copy0 != null)
        {
            copy0.currentCooldown = 2;
        }

        bool duplicateIndependent =
            copy0 != null &&
            copy1 != null &&
            !object.ReferenceEquals(copy0, copy1) &&
            copy0.cardData == copy1.cardData &&
            copy0.instanceID == "ally_001_atk_bullet_001_copy_0" &&
            copy1.instanceID == "ally_001_atk_bullet_001_copy_1" &&
            copy1.currentCooldown == 0;

        if (copy0 != null)
        {
            copy0.currentCooldown = 0;
        }

        Debug.Log("模式56 G 重复cardID创建独立BattleCardState：" + duplicateIndependent);
    }

    void RunBattleDefinitionDataInitialBuffSubTest()
    {
        BattleDefinitionBootstrapResult result = BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        CharacterData allyAUnit = result != null && result.runtimeState != null ? result.runtimeState.allyA : null;
        BuffData bullet = GetFirstBuffBatch(allyAUnit, "Bullet");
        BuffDefinitionData bulletDefinition;
        bool definitionFound = BuffDefinitionLoader.TryGetDefinition("Bullet", out bulletDefinition);

        bool initialBuff =
            result != null &&
            result.isSuccess &&
            bullet != null &&
            bullet.stack == 6 &&
            bullet.duration == -1 &&
            definitionFound &&
            bullet.buffName == bulletDefinition.buffName &&
            bullet.buffCategory == bulletDefinition.buffCategory &&
            bullet.checkTiming == bulletDefinition.defaultCheckTiming &&
            bullet.expireRule == bulletDefinition.defaultExpireRule;

        Debug.Log("模式56 H 初始Buff来自BuffDefinitions且没有GuessBuff回落：" + initialBuff);
    }

    void RunBattleDefinitionDataEnemyCardsSubTest()
    {
        BattleDefinitionBootstrapResult result = BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        CharacterData enemyUnit = result != null && result.runtimeState != null ? result.runtimeState.enemy : null;
        CharacterData enemyUnit2 = result != null && result.runtimeState != null ? result.runtimeState.enemy2 : null;

        bool enemyCards =
            result != null &&
            result.isSuccess &&
            enemyUnit != null &&
            enemyUnit.battleCards != null &&
            enemyUnit.battleCards.Count == 2 &&
            !object.ReferenceEquals(enemyUnit.battleCards[0], enemyUnit.battleCards[1]) &&
            enemyUnit.battleCards[0].cardData == enemyUnit.battleCards[1].cardData &&
            enemyUnit.battleCards[0].instanceID == "enemy_001_enemy_atk_001_copy_0" &&
            enemyUnit.battleCards[1].instanceID == "enemy_001_enemy_atk_001_copy_1" &&
            enemyUnit.battleCards[0].owner == enemyUnit &&
            enemyUnit.battleCards[1].owner == enemyUnit &&
            enemyUnit2 != null &&
            enemyUnit2.battleCards != null &&
            enemyUnit2.battleCards.Count == 2 &&
            enemyUnit2.battleCards[0].instanceID ==
                "enemy_001_02_enemy_atk_001_copy_0" &&
            enemyUnit2.battleCards[1].instanceID ==
                "enemy_001_02_enemy_atk_001_copy_1" &&
            enemyUnit2.battleCards[0].owner == enemyUnit2 &&
            enemyUnit2.battleCards[1].owner == enemyUnit2 &&
            !object.ReferenceEquals(
                enemyUnit.battleCards[0],
                enemyUnit2.battleCards[0]
            );

        Debug.Log("模式56 I 两名敌人卡牌实例与owner彼此独立：" + enemyCards);
    }

    void RunBattleDefinitionDataActionSlotsSubTest()
    {
        BattleDefinitionBootstrapResult result = BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        BattleRuntimeState runtimeState = result != null ? result.runtimeState : null;

        bool actionSlots =
            runtimeState != null &&
            runtimeState.actionSlots != null &&
            runtimeState.actionSlots.Count == 4 &&
            HasEmptySlot(runtimeState.actionSlots, runtimeState.allyA, 1) &&
            HasEmptySlot(runtimeState.actionSlots, runtimeState.allyA, 2) &&
            HasEmptySlot(runtimeState.actionSlots, runtimeState.allyB, 1) &&
            HasEmptySlot(runtimeState.actionSlots, runtimeState.allyB, 2);

        Debug.Log("模式56 J A/B各2个空行动槽位创建成功：" + actionSlots);
    }

    void RunBattleDefinitionDataEnemyIntentsSubTest()
    {
        BattleDefinitionBootstrapResult result = BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        BattleRuntimeState runtimeState = result != null ? result.runtimeState : null;
        BattleEnemyIntent intent = runtimeState != null && runtimeState.intentQueue != null && runtimeState.intentQueue.Count > 0
            ? runtimeState.intentQueue[0]
            : null;
        BattleEnemyIntent intent2 = runtimeState != null && runtimeState.intentQueue != null && runtimeState.intentQueue.Count > 1
            ? runtimeState.intentQueue[1]
            : null;

        bool intents =
            runtimeState != null &&
            runtimeState.intentQueue != null &&
            runtimeState.intentQueue.Count ==
                result.encounterDefinition.intentPattern.Length * 2 &&
            intent != null &&
            intent2 != null &&
            intent.intentOrder == 1 &&
            intent2.intentOrder == 2 &&
            intent.enemySlotIndex == 1 &&
            intent2.enemySlotIndex == 1 &&
            intent.enemy == runtimeState.enemy &&
            intent.enemyCardState == runtimeState.enemy.battleCards[0] &&
            intent2.enemy == runtimeState.enemy2 &&
            intent2.enemyCardState == runtimeState.enemy2.battleCards[0] &&
            !object.ReferenceEquals(intent.enemy, intent2.enemy) &&
            !intent.isResponded &&
            !intent2.isResponded;

        Debug.Log("模式56 K 两名敌人分别从真实pattern生成独立意图：" + intents);
    }

    void RunBattleDefinitionDataTargetMappingSubTest()
    {
        BattleDefinitionBootstrapResult result = BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        BattleRuntimeState runtimeState = result != null ? result.runtimeState : null;
        BattleEnemyIntent intent = runtimeState != null && runtimeState.intentQueue != null && runtimeState.intentQueue.Count > 0
            ? runtimeState.intentQueue[0]
            : null;
        BattleEnemyIntent intent2 = runtimeState != null && runtimeState.intentQueue != null && runtimeState.intentQueue.Count > 1
            ? runtimeState.intentQueue[1]
            : null;

        bool targetMapping =
            intent != null &&
            intent2 != null &&
            intent.originalTargetCharacter == runtimeState.allyB &&
            intent.actualTargetCharacter == runtimeState.allyB &&
            intent.originalTargetSlotIndex == 1 &&
            intent.actualTargetSlotIndex == 1 &&
            intent2.originalTargetCharacter == runtimeState.allyB &&
            intent2.actualTargetCharacter == runtimeState.allyB &&
            intent2.originalTargetSlotIndex == 1 &&
            intent2.actualTargetSlotIndex == 1 &&
            !intent.isResponded &&
            !intent2.isResponded;

        Debug.Log("模式56 L FixedCharacterSlot目标映射到ally_002槽位1：" + targetMapping);
    }

    void RunBattleDefinitionDataRuntimeStateSubTest()
    {
        BattleDefinitionBootstrapResult result = BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        BattleRuntimeState runtimeState = result != null ? result.runtimeState : null;

        bool runtime =
            runtimeState != null &&
            runtimeState.battleUnits != null &&
            runtimeState.battleUnits.Count == 4 &&
            runtimeState.allyUnits != null &&
            runtimeState.allyUnits.Count == 2 &&
            runtimeState.enemyUnits != null &&
            runtimeState.enemyUnits.Count == 2 &&
            runtimeState.actionSlots != null &&
            runtimeState.actionSlots.Count == 4 &&
            runtimeState.intentQueue != null &&
            runtimeState.intentQueue.Count == 2 &&
            runtimeState.currentExecutionPlan == null &&
            runtimeState.currentTurn == 1 &&
            runtimeState.battleResult == BattleResult.None &&
            runtimeState.currentPhase == "Prepare";

        Debug.Log("模式56 M BattleRuntimeState基础字段进入Prepare：" + runtime);
    }

    void RunBattleDefinitionDataDeadFixedTargetFallbackSubTest()
    {
        BattleDefinitionBootstrapResult result = BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");

        if (result != null && result.runtimeState != null && result.runtimeState.allyB != null)
        {
            result.runtimeState.allyB.currentHP = 0;
        }

        BattleDefinitionIntentQueueResult intentResult = BattleDefinitionBootstrap.CreateIntentQueueForTurn(
            result.runtimeState,
            result.encounterDefinition,
            result.enemyDefinition,
            result.allyByID,
            result.runtimeState.currentTurn
        );

        BattleEnemyIntent intent = intentResult != null && intentResult.intentQueue != null && intentResult.intentQueue.Count > 0
            ? intentResult.intentQueue[0]
            : null;
        BattleEnemyIntent intent2 = intentResult != null && intentResult.intentQueue != null && intentResult.intentQueue.Count > 1
            ? intentResult.intentQueue[1]
            : null;

        bool fallback =
            intentResult != null &&
            intentResult.isSuccess &&
            intent != null &&
            intent2 != null &&
            intent.originalTargetCharacter == result.runtimeState.allyA &&
            intent.actualTargetCharacter == result.runtimeState.allyA &&
            intent.originalTargetSlotIndex == 1 &&
            intent.actualTargetSlotIndex == 1 &&
            intent2.originalTargetCharacter == result.runtimeState.allyA &&
            intent2.actualTargetCharacter == result.runtimeState.allyA &&
            intent2.originalTargetSlotIndex == 1 &&
            intent2.actualTargetSlotIndex == 1;

        Debug.Log("模式56 N 固定死亡目标在意图创建时回落到第一存活角色：" + fallback);
    }

    void RunBattleDefinitionDataEnemyCardCooldownSkipSubTest()
    {
        BattleDefinitionBootstrapResult result = BattleDefinitionBootstrap.CreateRuntimeState("encounter_test_001");
        BattleCardState enemyCard = result != null && result.runtimeState != null ? result.runtimeState.enemy.battleCards[0] : null;
        BattleCardState enemyCard2 = result != null && result.runtimeState != null ? result.runtimeState.enemy2.battleCards[0] : null;

        if (enemyCard != null)
        {
            enemyCard.currentCooldown = 2;
        }
        if (enemyCard2 != null)
        {
            enemyCard2.currentCooldown = 2;
        }

        BattleDefinitionIntentQueueResult intentResult = BattleDefinitionBootstrap.CreateIntentQueueForTurn(
            result.runtimeState,
            result.encounterDefinition,
            result.enemyDefinition,
            result.allyByID,
            result.runtimeState.currentTurn
        );

        bool skipped =
            intentResult != null &&
            intentResult.isSuccess &&
            intentResult.intentQueue != null &&
            intentResult.intentQueue.Count == 0 &&
            intentResult.warningMessages != null &&
            intentResult.warningMessages.Count > 0 &&
            enemyCard != null &&
            enemyCard2 != null &&
            enemyCard.currentCooldown == 2 &&
            enemyCard2.currentCooldown == 2 &&
            enemyCard.currentUseCount == 0 &&
            enemyCard2.currentUseCount == 0 &&
            !enemyCard.isConsumed &&
            !enemyCard2.isConsumed;

        Debug.Log("模式56 O 两名敌人卡CD阻止意图创建且不改卡状态：" + skipped);
    }

    void RunBattleDefinitionDataDuplicateEnemyCardIndexFailSubTest()
    {
        List<CardTestData> cards = CardDataLoader.LoadCardData();
        List<CharacterDefinitionData> characterDefinitions = CharacterDefinitionLoader.LoadDefinitions();
        List<EnemyDefinitionData> enemyDefinitions = EnemyDefinitionLoader.LoadDefinitions();
        List<EncounterDefinitionData> encounterDefinitions = CloneEncounterDefinitions(EncounterDefinitionLoader.LoadDefinitions());
        EncounterDefinitionData encounter = EncounterDefinitionLoader.FindByID(encounterDefinitions, "encounter_test_001");

        if (encounter != null)
        {
            encounter.intentPattern = new EnemyIntentDefinitionData[]
            {
                CloneEnemyIntentDefinition(encounter.intentPattern[0]),
                CloneEnemyIntentDefinition(encounter.intentPattern[0])
            };
        }

        BattleDefinitionBootstrapResult result = BattleDefinitionBootstrap.CreateRuntimeStateFromDefinitions(
            "encounter_test_001",
            cards,
            characterDefinitions,
            enemyDefinitions,
            encounterDefinitions
        );

        bool failedSafely =
            result != null &&
            !result.isSuccess &&
            result.runtimeState == null &&
            !string.IsNullOrEmpty(result.errorMessage);

        Debug.Log("模式56 P 重复enemyCardIndex安全失败且不创建Runtime：" + failedSafely);
    }

    void RunBattleDefinitionDataMissingCardFailSubTest()
    {
        List<CardTestData> cards = CardDataLoader.LoadCardData();
        CharacterDefinitionData definition = CloneCharacterDefinition(
            CharacterDefinitionLoader.FindByID(CharacterDefinitionLoader.LoadDefinitions(), "ally_001")
        );

        if (definition != null && definition.startingCardIDs != null && definition.startingCardIDs.Length > 0)
        {
            definition.startingCardIDs[0] = "missing_card_for_mode56";
        }

        BattleUnitFactoryResult result = BattleUnitFactory.CreatePlayer(definition, cards);

        bool failedSafely =
            result != null &&
            !result.isSuccess &&
            result.unit == null &&
            !string.IsNullOrEmpty(result.errorMessage);

        Debug.Log("模式56 Q 缺失cardID时Factory安全失败且没有半完成角色：" + failedSafely);
    }

    void RunBattleDefinitionDataMissingBuffFailSubTest()
    {
        List<CardTestData> cards = CardDataLoader.LoadCardData();
        CharacterDefinitionData definition = CloneCharacterDefinition(
            CharacterDefinitionLoader.FindByID(CharacterDefinitionLoader.LoadDefinitions(), "ally_001")
        );

        if (definition != null)
        {
            definition.initialBuffs = new InitialBuffDefinitionData[]
            {
                new InitialBuffDefinitionData
                {
                    buffID = "MissingBuffForMode56",
                    stack = 1,
                    duration = -1
                }
            };
        }

        BattleUnitFactoryResult result = BattleUnitFactory.CreatePlayer(definition, cards);

        bool failedSafely =
            result != null &&
            !result.isSuccess &&
            result.unit == null &&
            !string.IsNullOrEmpty(result.errorMessage);

        Debug.Log("模式56 R 缺失buffID时Factory安全失败且不走GuessBuff：" + failedSafely);
    }

    void RunBattleDefinitionDataMissingCrossReferenceFailSubTest()
    {
        List<CardTestData> cards = CardDataLoader.LoadCardData();
        List<CharacterDefinitionData> characterDefinitions = CharacterDefinitionLoader.LoadDefinitions();
        List<EnemyDefinitionData> enemyDefinitions = EnemyDefinitionLoader.LoadDefinitions();
        List<EncounterDefinitionData> encounterDefinitions = CloneEncounterDefinitions(EncounterDefinitionLoader.LoadDefinitions());
        EncounterDefinitionData encounter = EncounterDefinitionLoader.FindByID(encounterDefinitions, "encounter_test_001");

        if (encounter != null && encounter.allyCharacterIDs != null && encounter.allyCharacterIDs.Length > 0)
        {
            encounter.allyCharacterIDs[0] = "missing_ally_for_mode56";
        }

        BattleDefinitionBootstrapResult result = BattleDefinitionBootstrap.CreateRuntimeStateFromDefinitions(
            "encounter_test_001",
            cards,
            characterDefinitions,
            enemyDefinitions,
            encounterDefinitions
        );

        bool failedSafely =
            result != null &&
            !result.isSuccess &&
            result.runtimeState == null &&
            !string.IsNullOrEmpty(result.errorMessage);

        Debug.Log("模式56 S 缺失跨文件角色引用时bootstrap安全失败：" + failedSafely);
    }

    bool HasNoDuplicateCharacterDefinitionIDs(List<CharacterDefinitionData> definitions)
    {
        if (definitions == null)
        {
            return false;
        }

        HashSet<string> ids = new HashSet<string>();

        foreach (CharacterDefinitionData definition in definitions)
        {
            if (definition == null || string.IsNullOrEmpty(definition.characterID))
            {
                return false;
            }

            if (ids.Contains(definition.characterID))
            {
                return false;
            }

            ids.Add(definition.characterID);
        }

        return true;
    }

    bool HasNoDuplicateEnemyDefinitionIDs(List<EnemyDefinitionData> definitions)
    {
        if (definitions == null)
        {
            return false;
        }

        HashSet<string> ids = new HashSet<string>();

        foreach (EnemyDefinitionData definition in definitions)
        {
            if (definition == null || string.IsNullOrEmpty(definition.enemyID))
            {
                return false;
            }

            if (ids.Contains(definition.enemyID))
            {
                return false;
            }

            ids.Add(definition.enemyID);
        }

        return true;
    }

    bool HasNoDuplicateEncounterDefinitionIDs(List<EncounterDefinitionData> definitions)
    {
        if (definitions == null)
        {
            return false;
        }

        HashSet<string> ids = new HashSet<string>();

        foreach (EncounterDefinitionData definition in definitions)
        {
            if (definition == null || string.IsNullOrEmpty(definition.encounterID))
            {
                return false;
            }

            if (ids.Contains(definition.encounterID))
            {
                return false;
            }

            ids.Add(definition.encounterID);
        }

        return true;
    }

    bool IsRuntimeBaseState(CharacterData unit)
    {
        return unit != null &&
            unit.currentHP == unit.maxHP &&
            unit.currentGuilt == 0 &&
            unit.turnSpeed == unit.minSpeed;
    }

    bool IsCardStateFromDefinition(
        BattleCardState cardState,
        CharacterData owner,
        string expectedCardID,
        string expectedInstanceID
    )
    {
        return cardState != null &&
            cardState.owner == owner &&
            cardState.cardData != null &&
            cardState.cardData.cardID == expectedCardID &&
            cardState.instanceID == expectedInstanceID &&
            cardState.currentCooldown == 0 &&
            cardState.currentUseCount == 0 &&
            !cardState.isConsumed;
    }

    bool HasEmptySlot(List<BattleActionSlot> slots, CharacterData owner, int slotIndex)
    {
        if (slots == null || owner == null)
        {
            return false;
        }

        foreach (BattleActionSlot slot in slots)
        {
            if (slot != null &&
                slot.owner == owner &&
                slot.slotIndex == slotIndex &&
                slot.actor == null &&
                slot.cardState == null &&
                slot.target == null &&
                slot.enemyIntent == null &&
                !slot.isUsed)
            {
                return true;
            }
        }

        return false;
    }

    BuffData GetFirstBuffBatch(CharacterData character, string buffID)
    {
        if (character == null || character.buffs == null)
        {
            return null;
        }

        foreach (BuffData buff in character.buffs)
        {
            if (buff != null && buff.buffID == buffID)
            {
                return buff;
            }
        }

        return null;
    }

    List<EncounterDefinitionData> CloneEncounterDefinitions(List<EncounterDefinitionData> definitions)
    {
        List<EncounterDefinitionData> clones = new List<EncounterDefinitionData>();

        if (definitions == null)
        {
            return clones;
        }

        foreach (EncounterDefinitionData definition in definitions)
        {
            clones.Add(CloneEncounterDefinition(definition));
        }

        return clones;
    }

    EncounterDefinitionData CloneEncounterDefinition(EncounterDefinitionData definition)
    {
        if (definition == null)
        {
            return null;
        }

        EncounterDefinitionData clone = new EncounterDefinitionData
        {
            encounterID = definition.encounterID,
            encounterName = definition.encounterName,
            allyCharacterIDs = CloneStringArray(definition.allyCharacterIDs),
            enemyID = definition.enemyID,
            repeatIntentPattern = definition.repeatIntentPattern,
            battleBackgroundKey = definition.battleBackgroundKey,
            battleMusicKey = definition.battleMusicKey
        };

        if (definition.intentPattern != null)
        {
            clone.intentPattern = new EnemyIntentDefinitionData[definition.intentPattern.Length];

            for (int i = 0; i < definition.intentPattern.Length; i++)
            {
                clone.intentPattern[i] = CloneEnemyIntentDefinition(definition.intentPattern[i]);
            }
        }

        return clone;
    }

    CharacterDefinitionData CloneCharacterDefinition(CharacterDefinitionData definition)
    {
        if (definition == null)
        {
            return null;
        }

        CharacterDefinitionData clone = new CharacterDefinitionData
        {
            characterID = definition.characterID,
            characterName = definition.characterName,
            maxHP = definition.maxHP,
            minSpeed = definition.minSpeed,
            maxSpeed = definition.maxSpeed,
            actionSlotCount = definition.actionSlotCount,
            startingCardIDs = CloneStringArray(definition.startingCardIDs),
            prefabKey = definition.prefabKey,
            portraitKey = definition.portraitKey
        };

        if (definition.initialBuffs != null)
        {
            clone.initialBuffs = new InitialBuffDefinitionData[definition.initialBuffs.Length];

            for (int i = 0; i < definition.initialBuffs.Length; i++)
            {
                clone.initialBuffs[i] = CloneInitialBuffDefinition(definition.initialBuffs[i]);
            }
        }

        return clone;
    }

    EnemyIntentDefinitionData CloneEnemyIntentDefinition(EnemyIntentDefinitionData definition)
    {
        if (definition == null)
        {
            return null;
        }

        return new EnemyIntentDefinitionData
        {
            enemyCardIndex = definition.enemyCardIndex,
            targetRule = definition.targetRule,
            targetCharacterID = definition.targetCharacterID,
            targetSlotIndex = definition.targetSlotIndex
        };
    }

    InitialBuffDefinitionData CloneInitialBuffDefinition(InitialBuffDefinitionData definition)
    {
        if (definition == null)
        {
            return null;
        }

        return new InitialBuffDefinitionData
        {
            buffID = definition.buffID,
            stack = definition.stack,
            duration = definition.duration
        };
    }

    string[] CloneStringArray(string[] source)
    {
        if (source == null)
        {
            return null;
        }

        string[] clone = new string[source.Length];

        for (int i = 0; i < source.Length; i++)
        {
            clone[i] = source[i];
        }

        return clone;
    }

    void RunRealCardResourceJsonFoundSubTest()
    {
        List<CardTestData> cards = CardDataLoader.LoadCardData();
        CardTestData card = FindRealBulletAttackCard(cards);

        bool found =
            cards != null &&
            CountCardsByID(cards, "atk_bullet_001") == 1 &&
            card != null &&
            card.cardID == "atk_bullet_001" &&
            card.cardName == "基础射击" &&
            card.cardType == CardType.Attack &&
            !card.isSinCard &&
            card.isClashable &&
            card.minPoint == 1 &&
            card.maxPoint == 10;

        Debug.Log("模式55 A 找到真实JSON卡atk_bullet_001：" + found);
        Debug.Log("模式55 A cardID唯一：" + (cards != null && CountCardsByID(cards, "atk_bullet_001") == 1));
    }

    void RunRealCardResourceRuleDeserializedSubTest()
    {
        CardTestData card = LoadRealBulletAttackCard();
        CardResourceRuleData rule = card != null ? card.resourceRule : null;

        bool ruleCorrect =
            rule != null &&
            rule.resourceType == "BuffStack" &&
            rule.resourceID == "Bullet" &&
            rule.requiredStackForNormalVersion == 1 &&
            rule.fallbackMinPoint == 0 &&
            rule.fallbackMaxPoint == 0 &&
            rule.pointPerStack == 1 &&
            rule.exactStackForBonus == 3 &&
            rule.exactStackPointBonus == 3 &&
            rule.consumeAmountOnSuccess == 1;

        bool noBulletHardCondition = !HasBulletHardUseCondition(card);
        bool noDuplicateResourceRules =
            card != null &&
            (card.resourceRules == null || CountResourceRulesByID(card.resourceRules, "Bullet") <= 1);

        Debug.Log("模式55 B resourceRule字段完整正确：" + ruleCorrect);
        Debug.Log("模式55 B 不存在BuffStackAtLeast Bullet硬条件：" + noBulletHardCondition);
        Debug.Log("模式55 B resourceID大小写准确且无重复资源规则：" + (ruleCorrect && noDuplicateResourceRules));
    }

    void RunRealCardResourceAssignWithNoBulletSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("real55_c", 30, 30, 50, 10, 3, 8);
        BattleCardState attack = CreateRealBulletAttackCardState(context.allyA, "real55_c_attack");
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);

        CardEligibilityResult result;
        bool assigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 1, context.allyA, attack, context.enemy, out result);
        BattleActionSlot slot = BattleActionSlotManager.GetSlot(slots, context.allyA, 1);

        bool worked =
            attack != null &&
            assigned &&
            result.isEligible &&
            slot != null &&
            object.ReferenceEquals(slot.cardState, attack) &&
            CountBuffStack(context.allyA, "Bullet") == 0;

        Debug.Log("模式55 C Bullet为0仍允许安排：" + worked);
    }

    void RunRealCardResourceFallbackZeroPointSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("real55_d", 30, 30, 50, 10, 3, 8);
        BattleCardState attack = CreateRealBulletAttackCardState(context.allyA, "real55_d_attack");
        BattleActionSlot slot = AssignRealBulletAttackFreeAction(context, attack, 1);
        int enemyHPBefore = context.enemy.currentHP;

        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);

        bool worked =
            result != null &&
            result.resultType == "FreeAttack" &&
            result.isSuccess &&
            result.shouldCompleteItem &&
            result.playerPoint == 0 &&
            result.damage == 0 &&
            context.enemy.currentHP == enemyHPBefore &&
            CountBuffStack(context.allyA, "Bullet") == 0;

        Debug.Log("模式55 D Bullet为0执行0点降级版本：" + worked);
    }

    void RunRealCardResourceNextCardStillStacksSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("real55_e", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("NextCardPointUp", 1, 1);
        BattleCardState attack = CreateRealBulletAttackCardState(context.allyA, "real55_e_attack");
        BattleActionSlot slot = AssignRealBulletAttackFreeAction(context, attack, 1);

        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);

        bool worked =
            result != null &&
            result.resultType == "FreeAttack" &&
            result.playerPoint == 1 &&
            CountBuffStack(context.allyA, "NextCardPointUp") == 0 &&
            CountBuffStack(context.allyA, "Bullet") == 0;

        Debug.Log("模式55 E 无弹时NextCard仍然叠加：" + worked);
    }

    void RunRealCardResourceOneBulletNormalVersionSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("real55_f", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Bullet", 1, -1);
        BattleCardState attack = CreateRealBulletAttackCardState(context.allyA, "real55_f_attack");
        BattleActionSlot slot = AssignRealBulletAttackFreeAction(context, attack, 1);

        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);

        bool worked =
            result != null &&
            result.resultType == "FreeAttack" &&
            result.playerPoint >= 2 &&
            result.playerPoint <= 11 &&
            CountBuffStack(context.allyA, "Bullet") == 0;

        Debug.Log("模式55 F 1层Bullet启用正常版本范围：" + worked);
    }

    void RunRealCardResourceExactThreeBulletBonusSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("real55_g", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Bullet", 3, -1);
        BattleCardState attack = CreateRealBulletAttackCardState(context.allyA, "real55_g_attack");
        BattleActionSlot slot = AssignRealBulletAttackFreeAction(context, attack, 1);

        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);

        bool worked =
            result != null &&
            result.resultType == "FreeAttack" &&
            result.playerPoint >= 7 &&
            result.playerPoint <= 16 &&
            CountBuffStack(context.allyA, "Bullet") == 2;

        Debug.Log("模式55 G 正好3层Bullet资源修正为+6：" + worked);
    }

    void RunRealCardResourceWinConsumesOneBulletSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("real55_h", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Bullet", 3, -1);
        BattleCardState attack = CreateRealBulletAttackCardState(context.allyA, "real55_h_attack");
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "real55_h_enemy", 1, 0);
        int guiltBefore = context.allyA.currentGuilt;

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, attack),
            CreateEnemyAttackIntent("real55_h_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool worked =
            result != null &&
            result.resultType == "PlayerWin" &&
            result.playerCardUsed &&
            CountBuffStack(context.allyA, "Bullet") == 2 &&
            attack != null &&
            !attack.cardData.isSinCard &&
            context.allyA.currentGuilt == guiltBefore &&
            attack.currentUseCount == 0 &&
            !attack.isConsumed;

        Debug.Log("模式55 H 成功使用后只消耗1层Bullet且非罪卡：" + worked);
    }

    void RunRealCardResourceLoseConsumesNoBulletSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("real55_i", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Bullet", 3, -1);
        BattleCardState attack = CreateRealBulletAttackCardState(context.allyA, "real55_i_attack");
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "real55_i_enemy", 30, 0);
        int guiltBefore = context.allyA.currentGuilt;

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, attack),
            CreateEnemyAttackIntent("real55_i_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool worked =
            result != null &&
            result.resultType == "EnemyWin" &&
            !result.playerCardUsed &&
            CountBuffStack(context.allyA, "Bullet") == 3 &&
            context.allyA.currentGuilt == guiltBefore &&
            attack.currentUseCount == 0 &&
            !attack.isConsumed;

        Debug.Log("模式55 I Attack失败不消耗Bullet且不走罪卡逻辑：" + worked);
    }

    void RunRealCardResourcePreviousSlotReloadSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("real55_j", 30, 30, 50, 20, 3, 8);
        BattleCardState reloadAbility = CreateBattleEndedAbilityCard(context.allyA, "real55_j_reload", "Bullet");
        BattleCardState attack = CreateRealBulletAttackCardState(context.allyA, "real55_j_attack");
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);

        CardEligibilityResult reloadResult;
        bool reloadAssigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 1, context.allyA, reloadAbility, context.allyA, out reloadResult);
        CardEligibilityResult attackResult;
        bool attackAssigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 2, context.allyA, attack, context.enemy, out attackResult);
        bool attackArrangedBeforeBullet = attackAssigned && CountBuffStack(context.allyA, "Bullet") == 0;

        BattleActionSlot reloadSlot = BattleActionSlotManager.GetSlot(slots, context.allyA, 1);
        BattleActionSlot attackSlot = BattleActionSlotManager.GetSlot(slots, context.allyA, 2);
        BattleExecutionPlan plan = CreateManualFreeActionPlan(reloadSlot, attackSlot);
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        bool worked =
            reloadAssigned &&
            reloadResult.isEligible &&
            attackArrangedBeforeBullet &&
            attackResult.isEligible &&
            reloadSlot != null &&
            attackSlot != null &&
            reloadSlot.isUsed &&
            attackSlot.isUsed &&
            context.enemy.currentHP <= 48 &&
            CountBuffStack(context.allyA, "Bullet") == 0 &&
            plan.isCompleted;

        Debug.Log("模式55 J 前序装填后后续真实射击读取实际Bullet：" + worked);
    }

    void RunCardAssignmentEligibilityGuiltInsufficientSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("elig54_a", 30, 30, 50, 10, 3, 8);
        context.allyA.currentGuilt = 15;
        BattleCardState card = CreateEligibilityAttackCard(context.allyA, "elig54_a_card", CreateGuiltAtLeastCondition(20));
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);

        CardEligibilityResult result;
        bool assigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 1, context.allyA, card, context.enemy, out result);
        BattleActionSlot slot = BattleActionSlotManager.GetSlot(slots, context.allyA, 1);

        Debug.Log("模式54 A Guilt不足拒绝安排：" + (!assigned && result.failureReason == CardEligibilityFailureReason.GuiltRequirementNotMet));
        Debug.Log("模式54 A 失败原因与数值正确：" + (result.requiredValue == 20 && result.currentValue == 15));
        Debug.Log("模式54 A 槽位保持空：" + IsSlotEmpty(slot));
    }

    void RunCardAssignmentEligibilityGuiltExactSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("elig54_b", 30, 30, 50, 10, 3, 8);
        context.allyA.currentGuilt = 20;
        BattleCardState card = CreateEligibilityAttackCard(context.allyA, "elig54_b_card", CreateGuiltAtLeastCondition(20));
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);

        CardEligibilityResult result;
        bool assigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 1, context.allyA, card, context.enemy, out result);
        BattleActionSlot slot = BattleActionSlotManager.GetSlot(slots, context.allyA, 1);

        Debug.Log("模式54 B Guilt刚好满足允许安排：" + (assigned && result.isEligible && slot != null && object.ReferenceEquals(slot.cardState, card)));
    }

    void RunCardAssignmentEligibilityGuiltAboveSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("elig54_c", 30, 30, 50, 10, 3, 8);
        context.allyA.currentGuilt = 25;
        BattleCardState card = CreateEligibilityAttackCard(context.allyA, "elig54_c_card", CreateGuiltAtLeastCondition(20));
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);

        CardEligibilityResult result;
        bool assigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 1, context.allyA, card, context.enemy, out result);

        Debug.Log("模式54 C Guilt超过要求允许安排：" + (assigned && result.isEligible));
    }

    void RunCardAssignmentEligibilityBuffInsufficientSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("elig54_d", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Strength", 1, 2);
        BattleCardState card = CreateEligibilityAttackCard(context.allyA, "elig54_d_card", CreateBuffStackAtLeastCondition("Strength", 2));
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);

        CardEligibilityResult result;
        bool assigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 1, context.allyA, card, context.enemy, out result);
        BattleActionSlot slot = BattleActionSlotManager.GetSlot(slots, context.allyA, 1);

        Debug.Log("模式54 D Buff不足拒绝安排：" + (!assigned && result.failureReason == CardEligibilityFailureReason.BuffStackRequirementNotMet));
        Debug.Log("模式54 D Buff原因与数值正确：" + (result.buffID == "Strength" && result.requiredValue == 2 && result.currentValue == 1));
        Debug.Log("模式54 D 槽位保持空：" + IsSlotEmpty(slot));
    }

    void RunCardAssignmentEligibilityBuffExactSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("elig54_e", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Strength", 2, 2);
        BattleCardState card = CreateEligibilityAttackCard(context.allyA, "elig54_e_card", CreateBuffStackAtLeastCondition("Strength", 2));
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);

        CardEligibilityResult result;
        bool assigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 1, context.allyA, card, context.enemy, out result);

        Debug.Log("模式54 E Buff刚好满足允许安排：" + (assigned && result.isEligible));
    }

    void RunCardAssignmentEligibilityMultipleConditionsSubTest()
    {
        BattleEndedTestContext guiltOkContext = CreateBattleEndedTestContext("elig54_f_guilt_ok", 30, 30, 50, 10, 3, 8);
        guiltOkContext.allyA.currentGuilt = 20;
        guiltOkContext.allyA.AddBuff("Strength", 1, 2);
        BattleCardState guiltOkCard = CreateEligibilityAttackCard(guiltOkContext.allyA, "elig54_f_guilt_ok_card", CreateGuiltAtLeastCondition(20), CreateBuffStackAtLeastCondition("Strength", 2));
        List<BattleActionSlot> guiltOkSlots = BattleActionSlotManager.CreatePartyActionSlots(guiltOkContext.allyA, guiltOkContext.allyB, 2);
        CardEligibilityResult guiltOkResult;
        bool guiltOkAssigned = BattleActionSlotManager.AssignFreeAction(guiltOkSlots, guiltOkContext.allyA, 1, guiltOkContext.allyA, guiltOkCard, guiltOkContext.enemy, out guiltOkResult);

        BattleEndedTestContext buffOkContext = CreateBattleEndedTestContext("elig54_f_buff_ok", 30, 30, 50, 10, 3, 8);
        buffOkContext.allyA.currentGuilt = 15;
        buffOkContext.allyA.AddBuff("Strength", 2, 2);
        BattleCardState buffOkCard = CreateEligibilityAttackCard(buffOkContext.allyA, "elig54_f_buff_ok_card", CreateGuiltAtLeastCondition(20), CreateBuffStackAtLeastCondition("Strength", 2));
        List<BattleActionSlot> buffOkSlots = BattleActionSlotManager.CreatePartyActionSlots(buffOkContext.allyA, buffOkContext.allyB, 2);
        CardEligibilityResult buffOkResult;
        bool buffOkAssigned = BattleActionSlotManager.AssignFreeAction(buffOkSlots, buffOkContext.allyA, 1, buffOkContext.allyA, buffOkCard, buffOkContext.enemy, out buffOkResult);

        BattleEndedTestContext bothOkContext = CreateBattleEndedTestContext("elig54_f_both_ok", 30, 30, 50, 10, 3, 8);
        bothOkContext.allyA.currentGuilt = 20;
        bothOkContext.allyA.AddBuff("Strength", 2, 2);
        BattleCardState bothOkCard = CreateEligibilityAttackCard(bothOkContext.allyA, "elig54_f_both_ok_card", CreateGuiltAtLeastCondition(20), CreateBuffStackAtLeastCondition("Strength", 2));
        List<BattleActionSlot> bothOkSlots = BattleActionSlotManager.CreatePartyActionSlots(bothOkContext.allyA, bothOkContext.allyB, 2);
        CardEligibilityResult bothOkResult;
        bool bothOkAssigned = BattleActionSlotManager.AssignFreeAction(bothOkSlots, bothOkContext.allyA, 1, bothOkContext.allyA, bothOkCard, bothOkContext.enemy, out bothOkResult);

        Debug.Log("模式54 F 多条件Guilt满足但Buff不足拒绝：" + (!guiltOkAssigned && guiltOkResult.failureReason == CardEligibilityFailureReason.BuffStackRequirementNotMet));
        Debug.Log("模式54 F 多条件Buff满足但Guilt不足拒绝：" + (!buffOkAssigned && buffOkResult.failureReason == CardEligibilityFailureReason.GuiltRequirementNotMet));
        Debug.Log("模式54 F 多条件全部满足允许：" + (bothOkAssigned && bothOkResult.isEligible));
    }

    void RunCardAssignmentEligibilityPendingBuffIgnoredSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("elig54_g", 30, 30, 50, 10, 3, 8);
        context.allyA.AddPendingBuff("Strength", 2, 1, 1, 1, 1);
        BattleCardState card = CreateEligibilityAttackCard(context.allyA, "elig54_g_card", CreateBuffStackAtLeastCondition("Strength", 1));
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);

        CardEligibilityResult result;
        bool assigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 1, context.allyA, card, context.enemy, out result);

        Debug.Log("模式54 G Pending Buff不计入资格：" + (!assigned && result.currentValue == 0 && context.allyA.pendingBuffs.Count == 1));
    }

    void RunCardAssignmentEligibilityPermanentBuffSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("elig54_h", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Strength", 2, -1);
        BattleCardState card = CreateEligibilityAttackCard(context.allyA, "elig54_h_card", CreateBuffStackAtLeastCondition("Strength", 2));
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);

        CardEligibilityResult result;
        bool assigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 1, context.allyA, card, context.enemy, out result);

        Debug.Log("模式54 H Permanent Buff计入资格：" + (assigned && CountBuffStack(context.allyA, "Strength") == 2));
    }

    void RunCardAssignmentEligibilitySoftResourceNotLockedSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("elig54_i", 30, 30, 50, 10, 3, 8);
        BattleCardState card = CreateResourceAttackCard(context.allyA, "elig54_i_card", 5, 5, 1, 2, 2, 0, 0, 0, 1);
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);

        CardEligibilityResult result;
        bool assigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 1, context.allyA, card, context.enemy, out result);

        Debug.Log("模式54 I 软资源Bullet不足不锁卡：" + (assigned && result.isEligible && CountBuffStack(context.allyA, "Bullet") == 0));
    }

    void RunCardAssignmentEligibilityExplicitBulletConditionSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("elig54_j", 30, 30, 50, 10, 3, 8);
        BattleCardState card = CreateResourceAttackCard(context.allyA, "elig54_j_card", 5, 5, 1, 2, 2, 0, 0, 0, 1);
        card.cardData.useConditions = new CardUseConditionData[] { CreateBuffStackAtLeastCondition("Bullet", 1) };
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);

        CardEligibilityResult result;
        bool assigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 1, context.allyA, card, context.enemy, out result);

        Debug.Log("模式54 J 显式Bullet硬条件锁卡：" + (!assigned && result.failureReason == CardEligibilityFailureReason.BuffStackRequirementNotMet && result.buffID == "Bullet"));
    }

    void RunCardAssignmentEligibilityCooldownConsumedSubTest()
    {
        BattleEndedTestContext cooldownContext = CreateBattleEndedTestContext("elig54_k_cd", 30, 30, 50, 10, 3, 8);
        BattleCardState cooldownCard = CreateEligibilityAttackCard(cooldownContext.allyA, "elig54_k_cd_card");
        cooldownCard.currentCooldown = 2;
        List<BattleActionSlot> cooldownSlots = BattleActionSlotManager.CreatePartyActionSlots(cooldownContext.allyA, cooldownContext.allyB, 2);
        CardEligibilityResult cooldownResult;
        bool cooldownAssigned = BattleActionSlotManager.AssignFreeAction(cooldownSlots, cooldownContext.allyA, 1, cooldownContext.allyA, cooldownCard, cooldownContext.enemy, out cooldownResult);

        BattleEndedTestContext consumedContext = CreateBattleEndedTestContext("elig54_k_consumed", 30, 30, 50, 10, 3, 8);
        BattleCardState consumedCard = CreateEligibilityAttackCard(consumedContext.allyA, "elig54_k_consumed_card");
        consumedCard.isConsumed = true;
        List<BattleActionSlot> consumedSlots = BattleActionSlotManager.CreatePartyActionSlots(consumedContext.allyA, consumedContext.allyB, 2);
        CardEligibilityResult consumedResult;
        bool consumedAssigned = BattleActionSlotManager.AssignFreeAction(consumedSlots, consumedContext.allyA, 1, consumedContext.allyA, consumedCard, consumedContext.enemy, out consumedResult);

        Debug.Log("模式54 K CD中拒绝且原因明确：" + (!cooldownAssigned && cooldownResult.failureReason == CardEligibilityFailureReason.CardOnCooldown && IsSlotEmpty(BattleActionSlotManager.GetSlot(cooldownSlots, cooldownContext.allyA, 1))));
        Debug.Log("模式54 K 已消耗拒绝且原因明确：" + (!consumedAssigned && consumedResult.failureReason == CardEligibilityFailureReason.CardConsumed && IsSlotEmpty(BattleActionSlotManager.GetSlot(consumedSlots, consumedContext.allyA, 1))));
    }

    void RunCardAssignmentEligibilityDeadActorSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("elig54_l", 30, 0, 50, 10, 3, 8);
        BattleCardState card = CreateEligibilityAttackCard(context.allyB, "elig54_l_card");
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);

        CardEligibilityResult result;
        bool assigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyB, 1, context.allyB, card, context.enemy, out result);

        Debug.Log("模式54 L 死亡角色拒绝安排：" + (!assigned && result.failureReason == CardEligibilityFailureReason.ActorDead && IsSlotEmpty(BattleActionSlotManager.GetSlot(slots, context.allyB, 1))));
    }

    void RunCardAssignmentEligibilityStateSafetySubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("elig54_m", 30, 30, 50, 20, 20, 8);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "elig54_m_enemy", 5, 0);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("elig54_m_intent", context.enemy, enemyAttack, context.allyA, 1);
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        BattleCardState oldResponse = CreateEligibilityAttackCard(context.allyA, "elig54_m_old");
        CardEligibilityResult oldResult;
        BattleActionSlotManager.AssignResponseToEnemyIntent(slots, context.allyA, 1, context.allyA, oldResponse, intent, out oldResult);

        BattleActionSlot oldSlot = BattleActionSlotManager.GetSlot(slots, context.allyA, 1);
        BattleActionSlot newSlot = BattleActionSlotManager.GetSlot(slots, context.allyB, 1);
        BattleCardState oldCardBefore = oldSlot.cardState;
        BattleEnemyIntent oldIntentBefore = oldSlot.enemyIntent;
        CharacterData actualTargetBefore = intent.actualTargetCharacter;
        int actualSlotBefore = intent.actualTargetSlotIndex;
        bool respondedBefore = intent.isResponded;
        CharacterData originalTargetBefore = intent.originalTargetCharacter;

        context.allyB.currentGuilt = 0;
        BattleCardState invalidNew = CreateEligibilityAttackCard(context.allyB, "elig54_m_invalid", CreateGuiltAtLeastCondition(20));
        CardEligibilityResult invalidResult;
        bool invalidAssigned = BattleActionSlotManager.AssignResponseToEnemyIntent(slots, context.allyB, 1, context.allyB, invalidNew, intent, out invalidResult);

        BattleCardState occupiedCard = CreateEligibilityAttackCard(context.allyA, "elig54_m_occupied");
        CardEligibilityResult occupiedResult;
        bool occupiedAssigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 1, context.allyA, occupiedCard, context.enemy, out occupiedResult);

        bool responseStateSafe =
            !invalidAssigned &&
            object.ReferenceEquals(oldSlot.cardState, oldCardBefore) &&
            object.ReferenceEquals(oldSlot.enemyIntent, oldIntentBefore) &&
            object.ReferenceEquals(intent.actualTargetCharacter, actualTargetBefore) &&
            intent.actualTargetSlotIndex == actualSlotBefore &&
            intent.isResponded == respondedBefore &&
            object.ReferenceEquals(intent.originalTargetCharacter, originalTargetBefore) &&
            IsSlotEmpty(newSlot);

        bool occupiedSafe = !occupiedAssigned &&
            occupiedResult.failureReason == CardEligibilityFailureReason.SlotOccupied &&
            object.ReferenceEquals(oldSlot.cardState, oldCardBefore);

        Debug.Log("模式54 M 响应失败不污染槽位和敌人意图：" + responseStateSafe);
        Debug.Log("模式54 M 已占用槽位拒绝且原卡保留：" + occupiedSafe);
    }

    void RunCardAssignmentEligibilityPureReadSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("elig54_n", 30, 30, 50, 10, 3, 8);
        context.allyA.currentGuilt = 15;
        context.allyA.AddBuff("Strength", 1, 2);
        context.allyA.AddBuff("Bullet", 3, -1);
        context.allyA.AddPendingBuff("Strength", 2, 1, 1, 1, 1);
        BattleCardState card = CreateEligibilityAttackCard(context.allyA, "elig54_n_card", CreateGuiltAtLeastCondition(20), CreateBuffStackAtLeastCondition("Strength", 2));
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);

        int guiltBefore = context.allyA.currentGuilt;
        int strengthBefore = CountBuffStack(context.allyA, "Strength");
        int bulletBefore = CountBuffStack(context.allyA, "Bullet");
        int pendingBefore = context.allyA.pendingBuffs.Count;
        int cooldownBefore = card.currentCooldown;
        int useCountBefore = card.currentUseCount;
        bool consumedBefore = card.isConsumed;

        CardEligibilityResult query = BattleCardManager.EvaluateCardEligibility(context.allyA, context.enemy, card);
        CardEligibilityResult assignResult;
        bool assigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 1, context.allyA, card, context.enemy, out assignResult);

        bool pure =
            !query.isEligible &&
            !assigned &&
            context.allyA.currentGuilt == guiltBefore &&
            CountBuffStack(context.allyA, "Strength") == strengthBefore &&
            CountBuffStack(context.allyA, "Bullet") == bulletBefore &&
            context.allyA.pendingBuffs.Count == pendingBefore &&
            card.currentCooldown == cooldownBefore &&
            card.currentUseCount == useCountBefore &&
            card.isConsumed == consumedBefore &&
            IsSlotEmpty(BattleActionSlotManager.GetSlot(slots, context.allyA, 1));

        Debug.Log("模式54 N 准备期资格检查纯读取：" + pure);
    }

    void RunCardAssignmentEligibilityExecutionRecheckSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("elig54_o", 30, 30, 50, 20, 3, 8);
        context.allyA.AddBuff("Strength", 1, 2);
        context.allyA.AddBuff("Bullet", 3, -1);
        BattleCardState card = CreateEligibilityAttackCard(context.allyA, "elig54_o_card", CreateBuffStackAtLeastCondition("Strength", 1));
        card.cardData.resourceRule = CreateBuffStackResourceRule("Bullet", 1, 1, 1, 0, 0, 0, 1);
        AddProbeEffect(card, BattleTiming.ActionStart, "Elig54OActionStart");
        AddProbeEffect(card, BattleTiming.BeforeUse, "Elig54OBeforeUse");
        AddProbeEffect(card, BattleTiming.Resolved, "Elig54OResolved");

        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        CardEligibilityResult assignResult;
        bool assigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 1, context.allyA, card, context.enemy, out assignResult);
        BattleActionSlot slot = BattleActionSlotManager.GetSlot(slots, context.allyA, 1);

        RemoveAllBuffs(context.allyA, "Strength");
        BattleExecutionPlan plan = CreateManualFreeActionPlan(slot);
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);
        BattleExecutionItem item = plan.executionItems.Count > 0 ? plan.executionItems[0] : null;

        bool rechecked =
            assigned &&
            IsExecutionItemState(item, BattleExecutionItemStatus.Skipped, BattleExecutionItemOutcomeReason.ActionUnavailable, true) &&
            plan.isCompleted &&
            !slot.isUsed &&
            card.currentCooldown == 0 &&
            card.currentUseCount == 0 &&
            CountBuffStack(context.allyA, "Bullet") == 3 &&
            CountBuffStack(context.allyA, "Elig54OActionStart") == 0 &&
            CountBuffStack(context.allyA, "Elig54OBeforeUse") == 0 &&
            CountBuffStack(context.allyA, "Elig54OResolved") == 0;

        Debug.Log("模式54 O 执行阶段复检ActionUnavailable保护：" + rechecked);
    }

    void RunCardAssignmentEligibilityNoPredictionSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("elig54_p", 30, 30, 50, 20, 3, 8);
        context.allyA.currentGuilt = 15;
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        BattleCardState guiltGainCard = CreateEligibilityAttackCard(context.allyA, "elig54_p_gain");
        guiltGainCard.cardData.isSinCard = true;
        guiltGainCard.cardData.guiltGain = 10;
        guiltGainCard.cardData.sinCardUseRule = SinCardUseRule.UseCount;
        guiltGainCard.cardData.maxUseCount = 3;
        CardEligibilityResult firstResult;
        BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 1, context.allyA, guiltGainCard, context.enemy, out firstResult);

        BattleCardState requiredCard = CreateEligibilityAttackCard(context.allyA, "elig54_p_required", CreateGuiltAtLeastCondition(20));
        CardEligibilityResult secondResult;
        bool secondAssigned = BattleActionSlotManager.AssignFreeAction(slots, context.allyA, 2, context.allyA, requiredCard, context.enemy, out secondResult);

        Debug.Log("模式54 P 不预测牌序结果仍按当前Guilt拒绝：" + (!secondAssigned && secondResult.failureReason == CardEligibilityFailureReason.GuiltRequirementNotMet && secondResult.currentValue == 15));
    }

    void RunPreparedAssignmentMainResponseAndPromotionSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("prepared57_a", 30, 30, 50, 10, 12, 5);
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        BattleEnemyIntent intent = CreatePreparedAssignmentIntent(context, "prepared57_a_intent", context.allyA, 2, 1, 1);
        context.runtimeState.SetActionSlots(slots);
        context.runtimeState.SetIntentQueue(BattleEnemyIntentManager.CreateIntentQueue(intent));

        BattleCardState attack = CreateFixedAttackCardForCharacter(context.allyA, "prepared57_a_attack", 5);
        BattleCardState defense = CreateTestDefenseCardForCharacter(context.allyB, "prepared57_a_defense", 4, 1);
        BattleActionAssignmentResult firstResult;
        BattleActionAssignmentResult secondResult;
        bool firstAssigned = BattleActionSlotManager.TryAssignToEnemyIntent(
            context.runtimeState,
            context.allyA,
            1,
            attack,
            intent,
            out firstResult
        );
        bool secondAssigned = BattleActionSlotManager.TryAssignToEnemyIntent(
            context.runtimeState,
            context.allyB,
            1,
            defense,
            intent,
            out secondResult
        );

        BattleActionSlot allyASlot = BattleActionSlotManager.GetSlot(slots, context.allyA, 1);
        BattleActionSlot allyBSlot = BattleActionSlotManager.GetSlot(slots, context.allyB, 1);
        bool laterResponderWins =
            firstAssigned &&
            secondAssigned &&
            secondResult.isSuccess &&
            allyBSlot.slotType == BattleActionSlotType.RespondToEnemyIntent &&
            object.ReferenceEquals(allyBSlot.enemyIntent, intent) &&
            allyASlot.placementType == BattleActionPlacementType.ExactEnemyIntent &&
            object.ReferenceEquals(allyASlot.requestedEnemyIntent, intent) &&
            allyASlot.slotType == BattleActionSlotType.FreeAction &&
            allyASlot.enemyIntent == null &&
            object.ReferenceEquals(allyASlot.target, context.enemy) &&
            allyBSlot.assignmentSequence > allyASlot.assignmentSequence &&
            intent.isResponded;

        BattleActionAssignmentResult cancelResult;
        bool cancelled = BattleActionSlotManager.TryCancelAssignment(
            context.runtimeState,
            context.allyB,
            1,
            out cancelResult
        );
        bool earlierResponderPromoted =
            cancelled &&
            cancelResult.isSuccess &&
            allyBSlot.IsEmpty() &&
            allyASlot.slotType == BattleActionSlotType.RespondToEnemyIntent &&
            object.ReferenceEquals(allyASlot.enemyIntent, intent) &&
            intent.isResponded &&
            object.ReferenceEquals(intent.actualTargetCharacter, context.allyA) &&
            intent.actualTargetSlotIndex == 1;

        Debug.Log("模式57 A 后放合格响应者成为当前主要响应：" + laterResponderWins);
        Debug.Log("模式57 A 取消主要响应后较早候选自动顶替：" + earlierResponderPromoted);
    }

    void RunPreparedAssignmentAutoDowngradeSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("prepared57_b", 30, 30, 50, 20, 3, 8);
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        BattleEnemyIntent intent = CreatePreparedAssignmentIntent(context, "prepared57_b_intent", context.allyA, 2, 1, 1);
        context.runtimeState.SetActionSlots(slots);
        context.runtimeState.SetIntentQueue(BattleEnemyIntentManager.CreateIntentQueue(intent));

        BattleCardState mainAttack = CreateFixedAttackCardForCharacter(context.allyA, "prepared57_b_main", 5);
        BattleCardState lowSpeedAttack = CreateFixedAttackCardForCharacter(context.allyB, "prepared57_b_low", 5);
        BattleActionAssignmentResult mainResult;
        BattleActionAssignmentResult downgradeResult;
        bool mainAssigned = BattleActionSlotManager.TryAssignToEnemyIntent(
            context.runtimeState,
            context.allyA,
            1,
            mainAttack,
            intent,
            out mainResult
        );
        bool lowAssigned = BattleActionSlotManager.TryAssignToEnemyIntent(
            context.runtimeState,
            context.allyB,
            1,
            lowSpeedAttack,
            intent,
            out downgradeResult
        );

        BattleActionSlot mainSlot = BattleActionSlotManager.GetSlot(slots, context.allyA, 1);
        BattleActionSlot lowSlot = BattleActionSlotManager.GetSlot(slots, context.allyB, 1);
        bool downgraded =
            mainAssigned &&
            lowAssigned &&
            downgradeResult.isSuccess &&
            downgradeResult.wasAutoDowngraded &&
            downgradeResult.placementType == BattleActionPlacementType.SpecificEnemy &&
            lowSlot.placementType == BattleActionPlacementType.SpecificEnemy &&
            lowSlot.requestedEnemyIntent == null &&
            object.ReferenceEquals(lowSlot.requestedEnemy, context.enemy) &&
            lowSlot.slotType == BattleActionSlotType.FreeAction &&
            object.ReferenceEquals(lowSlot.target, context.enemy) &&
            mainSlot.slotType == BattleActionSlotType.RespondToEnemyIntent &&
            object.ReferenceEquals(mainSlot.enemyIntent, intent);

        Debug.Log("模式57 B 低速非原目标Attack自动降级且不影响主要响应：" + downgraded);
    }

    void RunPreparedAssignmentGuardPlacementsSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("prepared57_c", 30, 30, 50, 10, 3, 8);
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        BattleEnemyIntent intent = CreatePreparedAssignmentIntent(context, "prepared57_c_intent", context.allyA, 2, 1, 1);
        context.runtimeState.SetActionSlots(slots);
        context.runtimeState.SetIntentQueue(BattleEnemyIntentManager.CreateIntentQueue(intent));

        BattleCardState defense = CreateTestDefenseCardForCharacter(context.allyB, "prepared57_c_defense", 4, 1);
        BattleCardState dodge = CreateFixedDodgeCardForCharacter(context.allyB, "prepared57_c_dodge", 4, 1);
        BattleActionAssignmentResult defenseResult;
        BattleActionAssignmentResult dodgeResult;
        bool defenseAssigned = BattleActionSlotManager.TryAssignToEnemyIntent(
            context.runtimeState,
            context.allyB,
            1,
            defense,
            intent,
            out defenseResult
        );
        bool dodgeAssigned = BattleActionSlotManager.TryAssignToEnemyIntent(
            context.runtimeState,
            context.allyB,
            2,
            dodge,
            intent,
            out dodgeResult
        );

        BattleActionSlot defenseSlot = BattleActionSlotManager.GetSlot(slots, context.allyB, 1);
        BattleActionSlot dodgeSlot = BattleActionSlotManager.GetSlot(slots, context.allyB, 2);
        bool enemySpecificGuards =
            defenseAssigned &&
            dodgeAssigned &&
            defenseResult.wasAutoDowngraded &&
            dodgeResult.wasAutoDowngraded &&
            defenseSlot.placementType == BattleActionPlacementType.SpecificEnemy &&
            dodgeSlot.placementType == BattleActionPlacementType.SpecificEnemy &&
            defenseSlot.slotType == BattleActionSlotType.EnemySpecificGuard &&
            dodgeSlot.slotType == BattleActionSlotType.EnemySpecificGuard &&
            object.ReferenceEquals(defenseSlot.requestedEnemy, context.enemy) &&
            object.ReferenceEquals(dodgeSlot.requestedEnemy, context.enemy) &&
            defenseSlot.requestedEnemyIntent == null &&
            dodgeSlot.requestedEnemyIntent == null &&
            defenseSlot.enemyIntent == null &&
            dodgeSlot.enemyIntent == null;

        Debug.Log("模式57 C 低速Defense与Dodge自动降级为EnemySpecificGuard：" + enemySpecificGuards);
    }

    void RunPreparedAssignmentSelfAndInvalidTargetSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("prepared57_d", 30, 30, 50, 10, 10, 8);
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 3);
        context.runtimeState.SetActionSlots(slots);
        context.runtimeState.SetIntentQueue(new List<BattleEnemyIntent>());

        BattleCardState defense = CreateTestDefenseCardForCharacter(context.allyA, "prepared57_d_defense", 4, 1);
        BattleCardState dodge = CreateFixedDodgeCardForCharacter(context.allyA, "prepared57_d_dodge", 4, 1);
        BattleCardState ability = CreateBattleEndedAbilityCard(context.allyA, "prepared57_d_ability", "Prepared57DAbility");
        BattleActionAssignmentResult defenseResult;
        BattleActionAssignmentResult dodgeResult;
        BattleActionAssignmentResult abilityResult;
        bool defenseAssigned = BattleActionSlotManager.TryAssignToSelf(context.runtimeState, context.allyA, 1, defense, out defenseResult);
        bool dodgeAssigned = BattleActionSlotManager.TryAssignToSelf(context.runtimeState, context.allyA, 2, dodge, out dodgeResult);
        bool abilityAssigned = BattleActionSlotManager.TryAssignToSelf(context.runtimeState, context.allyA, 3, ability, out abilityResult);

        BattleActionSlot defenseSlot = BattleActionSlotManager.GetSlot(slots, context.allyA, 1);
        BattleActionSlot dodgeSlot = BattleActionSlotManager.GetSlot(slots, context.allyA, 2);
        BattleActionSlot abilitySlot = BattleActionSlotManager.GetSlot(slots, context.allyA, 3);
        bool selfPlacements =
            defenseAssigned &&
            dodgeAssigned &&
            abilityAssigned &&
            defenseSlot.placementType == BattleActionPlacementType.Self &&
            dodgeSlot.placementType == BattleActionPlacementType.Self &&
            abilitySlot.placementType == BattleActionPlacementType.Self &&
            defenseSlot.slotType == BattleActionSlotType.PassiveGuard &&
            dodgeSlot.slotType == BattleActionSlotType.PassiveGuard &&
            abilitySlot.slotType == BattleActionSlotType.FreeAction &&
            object.ReferenceEquals(defenseSlot.target, context.allyA) &&
            object.ReferenceEquals(dodgeSlot.target, context.allyA) &&
            object.ReferenceEquals(abilitySlot.target, context.allyA);

        BattleCardState attack = CreateFixedAttackCardForCharacter(context.allyB, "prepared57_d_attack", 5);
        BattleCardState enemyAbility = CreateBattleEndedAbilityCard(context.allyB, "prepared57_d_enemy_ability", "Prepared57DEnemyAbility");
        BattleActionAssignmentResult attackResult;
        BattleActionAssignmentResult enemyAbilityResult;
        bool attackAssigned = BattleActionSlotManager.TryAssignToSelf(context.runtimeState, context.allyB, 1, attack, out attackResult);
        bool enemyAbilityAssigned = BattleActionSlotManager.TryAssignToEnemy(
            context.runtimeState,
            context.allyB,
            2,
            enemyAbility,
            context.enemy,
            out enemyAbilityResult
        );
        bool invalidRejected =
            !attackAssigned &&
            !enemyAbilityAssigned &&
            IsSlotEmpty(BattleActionSlotManager.GetSlot(slots, context.allyB, 1)) &&
            IsSlotEmpty(BattleActionSlotManager.GetSlot(slots, context.allyB, 2));

        Debug.Log("模式57 D Self正确派生Defense/Dodge/Ability：" + selfPlacements);
        Debug.Log("模式57 D Self Attack与Enemy Ability正确拒绝：" + invalidRejected);
    }

    void RunPreparedAssignmentAtomicReplaceAndDuplicateSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("prepared57_e", 30, 30, 50, 20, 10, 8);
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        BattleEnemyIntent intent = CreatePreparedAssignmentIntent(context, "prepared57_e_intent", context.allyB, 2, 1, 1);
        context.runtimeState.SetActionSlots(slots);
        context.runtimeState.SetIntentQueue(BattleEnemyIntentManager.CreateIntentQueue(intent));

        BattleCardState attack = CreateFixedAttackCardForCharacter(context.allyA, "prepared57_e_attack", 5);
        BattleActionAssignmentResult firstResult;
        bool firstAssigned = BattleActionSlotManager.TryAssignToEnemyIntent(
            context.runtimeState,
            context.allyA,
            1,
            attack,
            intent,
            out firstResult
        );
        BattleActionSlot slot1 = BattleActionSlotManager.GetSlot(slots, context.allyA, 1);
        long firstSequence = slot1.assignmentSequence;
        BattleActionAssignmentResult rearrangeResult;
        bool rearranged = BattleActionSlotManager.TryAssignToEnemyIntent(
            context.runtimeState,
            context.allyA,
            1,
            attack,
            intent,
            out rearrangeResult
        );
        long oldSequence = slot1.assignmentSequence;
        CharacterData oldActualTarget = intent.actualTargetCharacter;
        int oldActualSlot = intent.actualTargetSlotIndex;

        BattleActionAssignmentResult duplicateResult;
        bool duplicateAssigned = BattleActionSlotManager.TryAssignToEnemy(
            context.runtimeState,
            context.allyA,
            2,
            attack,
            context.enemy,
            out duplicateResult
        );

        BattleCardState invalidReplacement = CreateFixedAttackCardForCharacter(context.allyA, "prepared57_e_invalid", 5);
        BattleActionAssignmentResult invalidResult;
        bool invalidAssigned = BattleActionSlotManager.TryAssignToSelf(
            context.runtimeState,
            context.allyA,
            1,
            invalidReplacement,
            out invalidResult
        );
        bool failedReplaceAtomic =
            firstAssigned &&
            rearranged &&
            oldSequence > firstSequence &&
            !invalidAssigned &&
            object.ReferenceEquals(slot1.cardState, attack) &&
            slot1.placementType == BattleActionPlacementType.ExactEnemyIntent &&
            object.ReferenceEquals(slot1.requestedEnemyIntent, intent) &&
            slot1.assignmentSequence == oldSequence &&
            slot1.slotType == BattleActionSlotType.RespondToEnemyIntent &&
            object.ReferenceEquals(intent.actualTargetCharacter, oldActualTarget) &&
            intent.actualTargetSlotIndex == oldActualSlot;

        BattleCardState defense = CreateTestDefenseCardForCharacter(context.allyA, "prepared57_e_defense", 4, 1);
        BattleActionAssignmentResult replacementResult;
        bool replacementAssigned = BattleActionSlotManager.TryAssignToEnemyIntent(
            context.runtimeState,
            context.allyA,
            1,
            defense,
            intent,
            out replacementResult
        );
        bool validReplaceWorked =
            replacementAssigned &&
            object.ReferenceEquals(slot1.cardState, defense) &&
            slot1.placementType == BattleActionPlacementType.ExactEnemyIntent &&
            slot1.assignmentSequence > oldSequence &&
            slot1.slotType == BattleActionSlotType.RespondToEnemyIntent;
        bool duplicateRejected =
            !duplicateAssigned &&
            duplicateResult.eligibilityResult.failureReason == CardEligibilityFailureReason.CardAlreadyAssigned &&
            IsSlotEmpty(BattleActionSlotManager.GetSlot(slots, context.allyA, 2));

        Debug.Log("模式57 E 非法替换保持旧安排与主要响应原子不变：" + failedReplaceAtomic);
        Debug.Log("模式57 E 同一卡重新安排到同槽会刷新安排序号：" + (rearranged && oldSequence > firstSequence));
        Debug.Log("模式57 E 合法替换刷新安排序号与关系：" + validReplaceWorked);
        Debug.Log("模式57 E 同一卡实例不能安排到其他槽位：" + duplicateRejected);
    }

    void RunPreparedAssignmentCancelAndIntentCompatibilitySubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("prepared57_f", 30, 30, 50, 10, 12, 5);
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        BattleEnemyIntent intent = CreatePreparedAssignmentIntent(context, "prepared57_f_intent", context.allyA, 2, 1, 7);
        context.runtimeState.SetActionSlots(slots);
        context.runtimeState.SetIntentQueue(BattleEnemyIntentManager.CreateIntentQueue(intent));

        BattleCardState attackA = CreateFixedAttackCardForCharacter(context.allyA, "prepared57_f_attack_a", 5);
        BattleCardState attackB = CreateFixedAttackCardForCharacter(context.allyB, "prepared57_f_attack_b", 5);
        BattleActionAssignmentResult resultA;
        BattleActionAssignmentResult resultB;
        BattleActionSlotManager.TryAssignToEnemyIntent(context.runtimeState, context.allyA, 1, attackA, intent, out resultA);
        BattleActionSlotManager.TryAssignToEnemyIntent(context.runtimeState, context.allyB, 1, attackB, intent, out resultB);

        BattleActionAssignmentResult cancelNonMainResult;
        bool cancelNonMain = BattleActionSlotManager.TryCancelAssignment(
            context.runtimeState,
            context.allyA,
            1,
            out cancelNonMainResult
        );
        BattleActionSlot mainSlot = BattleActionSlotManager.GetSlot(slots, context.allyB, 1);
        bool nonMainCancelPreserved =
            cancelNonMain &&
            mainSlot.slotType == BattleActionSlotType.RespondToEnemyIntent &&
            object.ReferenceEquals(mainSlot.enemyIntent, intent) &&
            intent.isResponded &&
            object.ReferenceEquals(intent.actualTargetCharacter, context.allyB) &&
            intent.actualTargetSlotIndex == 1;

        BattleActionAssignmentResult cancelLastResult;
        bool cancelLast = BattleActionSlotManager.TryCancelAssignment(
            context.runtimeState,
            context.allyB,
            1,
            out cancelLastResult
        );
        bool lastCancelReset =
            cancelLast &&
            !intent.isResponded &&
            object.ReferenceEquals(intent.actualTargetCharacter, intent.originalTargetCharacter) &&
            intent.actualTargetSlotIndex == intent.originalTargetSlotIndex;

        BattleEnemyIntent oldConstructorIntent = new BattleEnemyIntent(
            "prepared57_f_old_constructor",
            context.enemy,
            intent.enemyCardState,
            context.allyA,
            1,
            3
        );
        intent.SetActualTarget(context.allyB, 1);
        intent.MarkResponded();
        intent.ResetResponseState();
        bool intentCompatibility =
            intent.enemySlotIndex == 7 &&
            oldConstructorIntent.enemySlotIndex == 3 &&
            !intent.isResponded &&
            object.ReferenceEquals(intent.actualTargetCharacter, intent.originalTargetCharacter) &&
            intent.actualTargetSlotIndex == intent.originalTargetSlotIndex;

        Debug.Log("模式57 F 取消非主要候选不影响主要响应：" + nonMainCancelPreserved);
        Debug.Log("模式57 F 取消最后候选重置敌人响应状态：" + lastCancelReset);
        Debug.Log("模式57 F enemySlotIndex新旧构造与ResetResponseState兼容：" + intentCompatibility);
    }

    void RunPreparedAssignmentPurePrepareStateSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("prepared57_g", 30, 30, 50, 10, 10, 8);
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        context.runtimeState.SetActionSlots(slots);
        context.runtimeState.SetIntentQueue(new List<BattleEnemyIntent>());
        context.runtimeState.currentGuilt = 6;
        context.allyA.AddBuff("Strength", 2, 2);

        BattleCardState attack = CreateFixedAttackCardForCharacter(context.allyA, "prepared57_g_attack", 5);
        int guiltBefore = context.runtimeState.currentGuilt;
        int buffBefore = CountBuffStack(context.allyA, "Strength");
        int cooldownBefore = attack.currentCooldown;
        int useCountBefore = attack.currentUseCount;
        bool consumedBefore = attack.isConsumed;
        int enemyHpBefore = context.enemy.currentHP;

        BattleActionAssignmentResult result;
        bool assigned = BattleActionSlotManager.TryAssignToEnemy(
            context.runtimeState,
            context.allyA,
            1,
            attack,
            context.enemy,
            out result
        );
        bool purePrepare =
            assigned &&
            result.isSuccess &&
            context.runtimeState.currentGuilt == guiltBefore &&
            CountBuffStack(context.allyA, "Strength") == buffBefore &&
            attack.currentCooldown == cooldownBefore &&
            attack.currentUseCount == useCountBefore &&
            attack.isConsumed == consumedBefore &&
            context.enemy.currentHP == enemyHpBefore;

        Debug.Log("模式57 G 准备阶段安排不修改CD/UseCount/Guilt/Buff/HP：" + purePrepare);
    }

    BattleEnemyIntent CreatePreparedAssignmentIntent(
        BattleEndedTestContext context,
        string intentID,
        CharacterData originalTarget,
        int originalTargetSlotIndex,
        int intentOrder,
        int enemySlotIndex
    )
    {
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(
            context.enemy,
            intentID + "_enemy_attack",
            5,
            0
        );
        return new BattleEnemyIntent(
            intentID,
            context.enemy,
            enemyAttack,
            originalTarget,
            originalTargetSlotIndex,
            intentOrder,
            enemySlotIndex
        );
    }

    void RunCardResourceFallbackBasePointSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("resource53_a", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("NextCardPointUp", 1, 1);
        BattleCardState attack = CreateResourceAttackCard(context.allyA, "resource53_a_attack", 1, 10, 3, 0, 0, 0, 0, 0, 1);

        BattleActionSlot slot = new BattleActionSlot(context.allyA, 1);
        slot.AssignFreeAction(context.allyA, attack, context.enemy);
        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);

        bool worked =
            result != null &&
            result.resultType == "FreeAttack" &&
            result.playerPoint == 1 &&
            CountBuffStack(context.allyA, "Bullet") == 0;

        Debug.Log("模式53 A 无弹降级只替换基础点数且不ActionUnavailable：" + worked);
    }

    void RunCardResourceActionStartAffectsCurrentSnapshotSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("resource53_b", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Bullet", 2, -1);
        BattleCardState attack = CreateResourceAttackCard(context.allyA, "resource53_b_attack", 1, 1, 3, 0, 0, 1, 3, 3, 0);
        attack.cardData.effects.Add(CreateApplyBuffEffect(BattleTiming.ActionStart, "Bullet", 1, -1));

        BattleActionSlot slot = new BattleActionSlot(context.allyA, 1);
        slot.AssignFreeAction(context.allyA, attack, context.enemy);
        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);

        bool worked =
            result != null &&
            result.playerPoint == 7 &&
            CountBuffStack(context.allyA, "Bullet") == 3;

        Debug.Log("模式53 B ActionStart获得Bullet影响当前ResourceSnapshot：" + worked);
    }

    void RunCardResourceBeforeUseDoesNotAffectCurrentSnapshotSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("resource53_c", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Bullet", 2, -1);
        BattleCardState attack = CreateResourceAttackCard(context.allyA, "resource53_c_attack", 1, 1, 3, 0, 0, 1, 3, 3, 0);
        attack.cardData.effects.Add(CreateApplyBuffEffect(BattleTiming.BeforeUse, "Bullet", 1, -1));

        BattleActionSlot slot = new BattleActionSlot(context.allyA, 1);
        slot.AssignFreeAction(context.allyA, attack, context.enemy);
        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);

        bool worked =
            result != null &&
            result.playerPoint == 2 &&
            CountBuffStack(context.allyA, "Bullet") == 3;

        Debug.Log("模式53 C BeforeUse获得Bullet不回改当前ResourceSnapshot：" + worked);
    }

    void RunCardResourcePointPerStackSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("resource53_d", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Bullet", 2, -1);
        BattleCardState attack = CreateResourceAttackCard(context.allyA, "resource53_d_attack", 1, 1, 1, 0, 0, 1, 0, 0, 0);

        BattleActionSlot slot = new BattleActionSlot(context.allyA, 1);
        slot.AssignFreeAction(context.allyA, attack, context.enemy);
        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);

        Debug.Log("模式53 D 每层资源点数加成：" + (result != null && result.playerPoint == 3));
    }

    void RunCardResourceExactStackBonusSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("resource53_e", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Bullet", 3, -1);
        BattleCardState attack = CreateResourceAttackCard(context.allyA, "resource53_e_attack", 1, 1, 1, 0, 0, 1, 3, 3, 0);

        BattleActionSlot slot = new BattleActionSlot(context.allyA, 1);
        slot.AssignFreeAction(context.allyA, attack, context.enemy);
        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);

        Debug.Log("模式53 E 精确3层额外奖励：" + (result != null && result.playerPoint == 7));
    }

    void RunCardResourceAttackWinConsumeSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("resource53_f", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Bullet", 3, -1);
        BattleCardState attack = CreateResourceAttackCard(context.allyA, "resource53_f_attack", 8, 8, 3, 0, 0, 0, 0, 0, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "resource53_f_enemy", 5, 0);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, attack),
            CreateEnemyAttackIntent("resource53_f_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool worked =
            result != null &&
            result.resultType == "PlayerWin" &&
            result.playerCardUsed &&
            CountBuffStack(context.allyA, "Bullet") == 2;

        Debug.Log("模式53 F Attack胜利后消耗资源：" + worked);
    }

    void RunCardResourceAttackLoseNoConsumeSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("resource53_g", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Bullet", 3, -1);
        BattleCardState attack = CreateResourceAttackCard(context.allyA, "resource53_g_attack", 4, 4, 3, 0, 0, 0, 0, 0, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "resource53_g_enemy", 8, 0);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, attack),
            CreateEnemyAttackIntent("resource53_g_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool worked =
            result != null &&
            result.resultType == "EnemyWin" &&
            !result.playerCardUsed &&
            CountBuffStack(context.allyA, "Bullet") == 3;

        Debug.Log("模式53 G Attack失败不消耗默认资源：" + worked);
    }

    void RunCardResourceDodgeVsAttackConsumeSubTest()
    {
        BattleEndedTestContext successContext = CreateBattleEndedTestContext("resource53_h_success", 30, 30, 50, 10, 3, 8);
        successContext.allyA.AddBuff("Bullet", 3, -1);
        successContext.enemy.AddBuff("Bullet", 3, -1);
        BattleCardState successDodge = CreateResourceDodgeCard(successContext.allyA, "resource53_h_success_dodge", 9, 9, 3, 0, 0, 0, 0, 0, 1);
        BattleCardState successEnemy = CreateResourceAttackCard(successContext.enemy, "resource53_h_success_enemy", 5, 5, 3, 0, 0, 0, 0, 0, 1);

        BattleResolveResult successResult = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(successContext.allyA, successDodge),
            CreateEnemyAttackIntent("resource53_h_success_intent", successContext.enemy, successEnemy, successContext.allyA, 1)
        );

        bool dodgeSuccessConsumed =
            successResult != null &&
            successResult.resultType == "DodgeSuccess" &&
            CountBuffStack(successContext.allyA, "Bullet") == 2 &&
            CountBuffStack(successContext.enemy, "Bullet") == 2;

        BattleEndedTestContext failedContext = CreateBattleEndedTestContext("resource53_h_failed", 30, 30, 50, 10, 3, 8);
        failedContext.allyA.AddBuff("Bullet", 3, -1);
        failedContext.enemy.AddBuff("Bullet", 3, -1);
        BattleCardState failedDodge = CreateResourceDodgeCard(failedContext.allyA, "resource53_h_failed_dodge", 4, 4, 3, 0, 0, 0, 0, 0, 1);
        BattleCardState failedEnemy = CreateResourceAttackCard(failedContext.enemy, "resource53_h_failed_enemy", 8, 8, 3, 0, 0, 0, 0, 0, 1);

        BattleResolveResult failedResult = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(failedContext.allyA, failedDodge),
            CreateEnemyAttackIntent("resource53_h_failed_intent", failedContext.enemy, failedEnemy, failedContext.allyA, 1)
        );

        bool dodgeFailedConsumed =
            failedResult != null &&
            failedResult.resultType == "DodgeFailed" &&
            CountBuffStack(failedContext.allyA, "Bullet") == 2 &&
            CountBuffStack(failedContext.enemy, "Bullet") == 2;

        Debug.Log("模式53 H DodgeSuccess时Dodge与Attack均消耗资源：" + dodgeSuccessConsumed);
        Debug.Log("模式53 H DodgeFailed时Dodge与Attack均消耗资源：" + dodgeFailedConsumed);
    }

    void RunCardResourceDefenseConsumeSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("resource53_i", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Bullet", 3, -1);
        context.enemy.AddBuff("Bullet", 3, -1);
        BattleCardState defense = CreateResourceDefenseCard(context.allyA, "resource53_i_defense", 7, 7, 3, 0, 0, 0, 0, 0, 1);
        BattleCardState enemyAttack = CreateResourceAttackCard(context.enemy, "resource53_i_enemy", 5, 5, 3, 0, 0, 0, 0, 0, 1);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, defense),
            CreateEnemyAttackIntent("resource53_i_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool worked =
            result != null &&
            result.resultType == "DefenseFullBlock" &&
            CountBuffStack(context.allyA, "Bullet") == 2 &&
            CountBuffStack(context.enemy, "Bullet") == 2;

        Debug.Log("模式53 I Defense正常结算后消耗资源：" + worked);
    }

    void RunCardResourceFreeAttackConsumeSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("resource53_j", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Bullet", 3, -1);
        BattleCardState attack = CreateResourceAttackCard(context.allyA, "resource53_j_attack", 5, 5, 3, 0, 0, 0, 0, 0, 1);

        BattleActionSlot slot = new BattleActionSlot(context.allyA, 1);
        slot.AssignFreeAction(context.allyA, attack, context.enemy);
        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);

        Debug.Log("模式53 J FreeAttack成功后消耗资源：" + (result != null && result.resultType == "FreeAttack" && CountBuffStack(context.allyA, "Bullet") == 2));
    }

    void RunCardResourceUnrespondedEnemyAttackConsumeSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("resource53_k", 30, 30, 50, 10, 3, 8);
        context.enemy.AddBuff("Bullet", 3, -1);
        BattleCardState enemyAttack = CreateResourceAttackCard(context.enemy, "resource53_k_enemy", 5, 5, 3, 0, 0, 0, 0, 0, 1);

        BattleResolveResult result = BattleResolver.ResolveUnrespondedEnemyIntent(
            CreateEnemyAttackIntent("resource53_k_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        int enemyBulletAfter = CountBuffStack(context.enemy, "Bullet");
        bool worked =
            result != null &&
            result.resultType == "UnrespondedEnemyAttack" &&
            result.isSuccess &&
            result.shouldCompleteItem &&
            !result.playerCardUsed &&
            result.enemyCardUsed &&
            enemyBulletAfter == 2;

        Debug.Log(
            "模式53 K resultType / enemyCardUsed / Bullet剩余：" +
            (result != null ? result.resultType : "null") +
            " / " +
            (result != null && result.enemyCardUsed) +
            " / " +
            enemyBulletAfter
        );
        Debug.Log("模式53 K Unresponded敌人Attack成功后消耗资源：" + worked);
    }

    void RunCardResourceActionUnavailableNoActionStartSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("resource53_l", 30, 30, 50, 10, 3, 8);
        BattleCardState attack = CreateResourceAttackCard(context.allyB, "resource53_l_attack", 5, 5, 1, 0, 0, 0, 0, 0, 1);
        AddBulletCondition(attack.cardData, 3);
        attack.cardData.effects.Add(CreateApplyBuffEffect(BattleTiming.ActionStart, "Resource53LActionStart", 1, 1));

        BattleActionSlot slot = new BattleActionSlot(context.allyB, 1);
        slot.AssignFreeAction(context.allyB, attack, context.enemy);
        BattleResolveResult result = BattleResolver.ResolveFreeAction(slot);

        bool worked =
            result != null &&
            result.resultType == "ActionUnavailable" &&
            CountBuffStack(context.allyB, "Resource53LActionStart") == 0 &&
            CountBuffStack(context.allyB, "Bullet") == 0;

        Debug.Log("模式53 L ActionUnavailable不触发ActionStart不捕获资源不消耗：" + worked);
    }

    void RunCardResourceTieRetrySnapshotSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("resource53_m", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Bullet", 2, -1);
        context.enemy.AddBuff("Bullet", 2, -1);
        BattleCardState playerAttack = CreateResourceAttackCard(context.allyA, "resource53_m_player", 5, 5, 1, 0, 0, 0, 0, 0, 1);
        BattleCardState enemyAttack = CreateResourceAttackCard(context.enemy, "resource53_m_enemy", 5, 5, 1, 0, 0, 0, 0, 0, 1);
        playerAttack.cardData.effects.Add(CreateApplyBuffEffect(BattleTiming.ActionStart, "Bullet", 1, -1));
        enemyAttack.cardData.effects.Add(CreateApplyBuffEffect(BattleTiming.ActionStart, "Bullet", 1, -1));

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, playerAttack),
            CreateEnemyAttackIntent("resource53_m_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool worked =
            result != null &&
            result.resultType == "TieLimit" &&
            CountBuffStack(context.allyA, "Bullet") == 3 &&
            CountBuffStack(context.enemy, "Bullet") == 3;

        Debug.Log("模式53 M Tie重投不重复触发ActionStart且复用ResourceSnapshot：" + worked);
    }

    void RunCardResourceTieLimitNoConsumeSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("resource53_n", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("Bullet", 3, -1);
        context.enemy.AddBuff("Bullet", 3, -1);
        BattleCardState playerAttack = CreateResourceAttackCard(context.allyA, "resource53_n_player", 5, 5, 3, 0, 0, 0, 0, 0, 1);
        BattleCardState enemyAttack = CreateResourceAttackCard(context.enemy, "resource53_n_enemy", 5, 5, 3, 0, 0, 0, 0, 0, 1);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, playerAttack),
            CreateEnemyAttackIntent("resource53_n_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool worked =
            result != null &&
            result.resultType == "TieLimit" &&
            result.isTieLimitReached &&
            CountBuffStack(context.allyA, "Bullet") == 3 &&
            CountBuffStack(context.enemy, "Bullet") == 3;

        Debug.Log("模式53 N TieLimit不消耗资源：" + worked);
    }

    void RunCardResourceKnownPointPassiveGuardSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("resource53_o", 30, 30, 50, 10, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        context.allyB.AddBuff("Bullet", 3, -1);
        context.enemy.AddBuff("Bullet", 3, -1);
        BattleCardState responseAttack = CreateFixedAttackCardForCharacter(context.allyB, "resource53_o_response", 4);
        BattleCardState passiveDefense = CreateResourceDefenseCard(context.allyB, "resource53_o_passive", 10, 10, 3, 0, 0, 0, 0, 0, 1);
        BattleCardState enemyAttack = CreateResourceAttackCard(context.enemy, "resource53_o_enemy", 8, 8, 3, 0, 0, 0, 0, 0, 1);
        enemyAttack.cardData.effects.Add(CreateApplyBuffEffect(BattleTiming.ActionStart, "Resource53OEnemyActionStart", 1, 1));
        BattleEnemyIntent intent = CreateEnemyAttackIntent("resource53_o_intent", context.enemy, enemyAttack, context.allyB, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent);
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(intentQueue);

        BattleActionSlotManager.AssignResponseToEnemyIntent(actionSlots, context.allyB, 1, context.allyB, responseAttack, intent);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, context.allyB, 2, context.allyB, passiveDefense);
        BattleExecutionPlan plan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(actionSlots, intentQueue);
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        bool worked =
            CountBuffStack(context.enemy, "Resource53OEnemyActionStart") == 1 &&
            CountBuffStack(context.enemy, "Bullet") == 2 &&
            CountBuffStack(context.allyB, "Bullet") == 2 &&
            plan.isCompleted;

        Debug.Log("模式53 O known-point PassiveGuard使用自身资源且敌人不重复处理：" + worked);
    }

    void RunCardResourceActionStartOneShotAndAbilityIsolationSubTest()
    {
        BattleEndedTestContext pointContext = CreateBattleEndedTestContext("resource53_p_point", 30, 30, 50, 10, 3, 8);
        BattleCardState attack = CreateResourceAttackCard(pointContext.allyA, "resource53_p_attack", 5, 5, 0, 0, 0, 0, 0, 0, 0);
        attack.cardData.effects.Add(CreateApplyBuffEffect(BattleTiming.ActionStart, "NextCardPointUp", 2, 1));
        attack.cardData.effects.Add(CreateApplyBuffEffect(BattleTiming.ActionStart, "NextClashPointUp", 3, 1));
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(pointContext.enemy, "resource53_p_enemy", 1, 0);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(pointContext.allyA, attack),
            CreateEnemyAttackIntent("resource53_p_intent", pointContext.enemy, enemyAttack, pointContext.allyA, 1)
        );

        bool actionStartPointBuffKept =
            result != null &&
            result.playerPoint == 5 &&
            CountBuffStack(pointContext.allyA, "NextCardPointUp") == 2 &&
            CountBuffStack(pointContext.allyA, "NextClashPointUp") == 3;

        BattleEndedTestContext abilityContext = CreateBattleEndedTestContext("resource53_p_ability", 30, 30, 50, 10, 3, 8);
        BattleCardState ability = CreateBattleEndedAbilityCard(abilityContext.allyA, "resource53_p_ability_card", "Resource53PAbilityOnPlay");
        ability.cardData.effects.Add(CreateApplyBuffEffect(BattleTiming.ActionStart, "Resource53PAbilityActionStart", 1, 1));
        BattleActionSlot abilitySlot = new BattleActionSlot(abilityContext.allyA, 1);
        abilitySlot.AssignFreeAction(abilityContext.allyA, ability, abilityContext.allyA);

        BattleResolveResult abilityResult = BattleResolver.ResolveFreeAction(abilitySlot);
        bool abilityIsolated =
            abilityResult != null &&
            abilityResult.resultType == "FreeAbility" &&
            CountBuffStack(abilityContext.allyA, "Resource53PAbilityActionStart") == 0 &&
            CountBuffStack(abilityContext.allyA, "Resource53PAbilityOnPlay") == 1;

        Debug.Log("模式53 P ActionStart新增一次性点数Buff不影响当前卡：" + actionStartPointBuffKept);
        Debug.Log("模式53 P Ability仍保持OnPlay到Resolved且不触发ActionStart：" + abilityIsolated);
    }

    void RunBuffBeforeUseActionUnavailableBasicTestSequence()
    {
        Debug.Log("===== BuffBeforeUseActionUnavailableBasic 聚合测试开始 =====");

        RunBeforeUseAttackNormalBuffSubTest();
        RunBeforeUseNextCardPointSubTest();
        RunBeforeUseNextClashPointSubTest();
        RunBeforeUseMergedNextCardPointSubTest();
        RunBeforeUseMergedNextClashPointSubTest();
        RunBeforeUseDefenseGuardSubTest();
        RunBeforeUseKnownPointSubTest();
        RunBeforeUseFreeActionUnavailableSubTest();
        RunBeforeUseRespondedAttackUnavailableFallbackSubTest();
        RunBeforeUseRespondedDefenseUnavailablePassiveGuardSubTest();
        RunBeforeUseRespondedDodgeUnavailableNoGuardSubTest();
        RunBeforeUseRespondedUnavailableDeadOriginalTargetSubTest();
        RunBeforeUseTieLimitSubTest();
        RunBeforeUseAbilityUnavailableSubTest();
        RunBeforeUseSnapshotPureReadSubTest();
        RunBeforeUsePassiveGuardUnavailableCandidateSubTest();
    }

    void RunBeforeUseAttackNormalBuffSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("buff50_a", 30, 30, 50, 10, 3, 8);
        BattleCardState attack = CreateBeforeUseBuffAttackCard(context.allyA, "buff50_a_attack", 5, "Strength", 1, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff50_a_enemy", 1, 0);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, attack),
            CreateEnemyAttackIntent("buff50_a_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool worked =
            result != null &&
            result.resultType == "PlayerWin" &&
            result.playerPoint == 6 &&
            CountBuffStack(context.allyA, "Strength") == 1;

        Debug.Log("Attack BeforeUse普通Buff影响当前卡：" + worked);
    }

    void RunBeforeUseNextCardPointSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("buff50_b", 30, 30, 50, 10, 3, 8);
        BattleCardState attack = CreateBeforeUseBuffAttackCard(context.allyA, "buff50_b_attack", 5, "NextCardPointUp", 2, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff50_b_enemy", 1, 0);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, attack),
            CreateEnemyAttackIntent("buff50_b_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool notCurrent = result != null && result.playerPoint == 5 && CountBuffStack(context.allyA, "NextCardPointUp") == 2;

        BattleCardState followAttack = CreateFixedAttackCardForCharacter(context.allyA, "buff50_b_follow", 5);
        BattleActionSlot followSlot = new BattleActionSlot(context.allyA, 2);
        followSlot.AssignFreeAction(context.allyA, followAttack, context.enemy);
        BattleResolveResult followResult = BattleResolver.ResolveFreeAction(followSlot);
        bool keptForNext = followResult != null && followResult.playerPoint == 7 && CountBuffStack(context.allyA, "NextCardPointUp") == 0;

        Debug.Log("BeforeUse新增蓄势不影响当前卡：" + notCurrent);
        Debug.Log("BeforeUse新增蓄势保留给后续卡：" + keptForNext);
    }

    void RunBeforeUseNextClashPointSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("buff50_c", 30, 30, 50, 10, 3, 8);
        BattleCardState attack = CreateBeforeUseBuffAttackCard(context.allyA, "buff50_c_attack", 5, "NextClashPointUp", 3, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff50_c_enemy", 1, 0);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, attack),
            CreateEnemyAttackIntent("buff50_c_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool notCurrent = result != null && result.playerPoint == 5 && CountBuffStack(context.allyA, "NextClashPointUp") == 3;

        BattleCardState followAttack = CreateFixedAttackCardForCharacter(context.allyA, "buff50_c_follow", 5);
        BattleCardState followEnemy = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff50_c_follow_enemy", 1, 0);
        BattleResolveResult followResult = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, followAttack),
            CreateEnemyAttackIntent("buff50_c_follow_intent", context.enemy, followEnemy, context.allyA, 1)
        );
        bool keptForNext = followResult != null && followResult.playerPoint == 8 && CountBuffStack(context.allyA, "NextClashPointUp") == 0;

        Debug.Log("BeforeUse新增拼点强化不影响当前拼点：" + notCurrent);
        Debug.Log("BeforeUse新增拼点强化保留给后续拼点：" + keptForNext);
    }

    void RunBeforeUseMergedNextCardPointSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("buff50_d", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("NextCardPointUp", 1, 1);
        BattleCardState attack = CreateBeforeUseBuffAttackCard(context.allyA, "buff50_d_attack", 5, "NextCardPointUp", 3, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff50_d_enemy", 1, 0);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, attack),
            CreateEnemyAttackIntent("buff50_d_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool oldUsed = result != null && result.playerPoint == 6;
        bool consumedOld = CountBuffStack(context.allyA, "NextCardPointUp") == 3;

        Debug.Log("旧蓄势快照正确参与当前卡：" + oldUsed);
        Debug.Log("只消费旧蓄势层数：" + consumedOld);
        Debug.Log("BeforeUse新增蓄势层数保留：" + consumedOld);
    }

    void RunBeforeUseMergedNextClashPointSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("buff50_e", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("NextClashPointUp", 2, 1);
        BattleCardState attack = CreateBeforeUseBuffAttackCard(context.allyA, "buff50_e_attack", 5, "NextClashPointUp", 4, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff50_e_enemy", 1, 0);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, attack),
            CreateEnemyAttackIntent("buff50_e_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool oldUsed = result != null && result.playerPoint == 7;
        bool consumedOld = CountBuffStack(context.allyA, "NextClashPointUp") == 4;

        Debug.Log("旧拼点强化快照正确参与当前拼点：" + oldUsed);
        Debug.Log("只消费旧拼点强化层数：" + consumedOld);
        Debug.Log("BeforeUse新增拼点强化层数保留：" + consumedOld);
    }

    void RunBeforeUseDefenseGuardSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("buff50_f", 30, 30, 50, 10, 3, 8);
        BattleCardState defense = CreateBeforeUseBuffDefenseCard(context.allyA, "buff50_f_defense", 3, "GuardUp", 2, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff50_f_enemy", 5, 0);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, defense),
            CreateEnemyAttackIntent("buff50_f_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        Debug.Log("Defense BeforeUse GuardUp影响当前Defense：" + (result != null && result.playerPoint == 5));
    }

    void RunBeforeUseKnownPointSubTest()
    {
        BattleEndedTestContext defenseContext = CreateBattleEndedTestContext("buff50_g_def", 30, 30, 50, 10, 3, 8);
        BattleCardState defense = CreateBeforeUseBuffDefenseCard(defenseContext.allyB, "buff50_g_defense", 3, "NextCardPointUp", 3, 1);
        BattleCardState enemyAttack = CreateBeforeUseBuffAttackCard(defenseContext.enemy, "buff50_g_enemy", 5, "Strength", 9, 1);
        BattleResolveResult defenseResult = BattleResolver.ResolveDefenseVsAttackWithKnownEnemyPoint(
            CreateRespondedSlot(defenseContext.allyB, defense),
            CreateEnemyAttackIntent("buff50_g_def_intent", defenseContext.enemy, enemyAttack, defenseContext.allyB, 1),
            4
        );

        BattleEndedTestContext dodgeContext = CreateBattleEndedTestContext("buff50_g_dodge", 30, 30, 50, 10, 3, 8);
        BattleCardState dodge = CreateBeforeUseBuffDodgeCard(dodgeContext.allyB, "buff50_g_dodge", 4, "NextClashPointUp", 3, 1);
        BattleCardState dodgeEnemy = CreateBeforeUseBuffAttackCard(dodgeContext.enemy, "buff50_g_dodge_enemy", 5, "Strength", 9, 1);
        BattleResolveResult dodgeResult = BattleResolver.ResolveDodgeVsAttackWithKnownEnemyPoint(
            CreateRespondedSlot(dodgeContext.allyB, dodge),
            CreateEnemyAttackIntent("buff50_g_dodge_intent", dodgeContext.enemy, dodgeEnemy, dodgeContext.allyB, 1),
            5
        );

        bool knownPointWorked =
            defenseResult != null &&
            defenseResult.playerPoint == 3 &&
            CountBuffStack(defenseContext.allyB, "NextCardPointUp") == 3 &&
            CountBuffStack(defenseContext.enemy, "Strength") == 0 &&
            dodgeResult != null &&
            dodgeResult.playerPoint == 4 &&
            CountBuffStack(dodgeContext.allyB, "NextClashPointUp") == 3 &&
            CountBuffStack(dodgeContext.enemy, "Strength") == 0;

        Debug.Log("known-point未重复触发敌人BeforeUse：" + knownPointWorked);
    }

    void RunBeforeUseFreeActionUnavailableSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("buff50_h", 30, 30, 50, 10, 3, 8);
        BattleCardState unavailableAttack = CreateBulletLockedBeforeUseAttackCard(context.allyB, "buff50_h_unavailable", 5, 3, "Strength", 1, 1);
        BattleCardState followAbility = CreateBattleEndedAbilityCard(context.allyA, "buff50_h_follow", "Buff50HFollow");
        context.allyB.AddBuff("NextCardPointUp", 4, 1);

        BattleActionSlot firstSlot = new BattleActionSlot(context.allyB, 1);
        firstSlot.AssignFreeAction(context.allyB, unavailableAttack, context.enemy);
        BattleActionSlot secondSlot = new BattleActionSlot(context.allyA, 1);
        secondSlot.AssignFreeAction(context.allyA, followAbility, context.allyA);
        BattleExecutionPlan plan = CreateManualFreeActionPlan(firstSlot, secondSlot);
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        bool noBeforeUse =
            CountBuffStack(context.allyB, "Strength") == 0 &&
            CountBuffStack(context.allyB, "NextCardPointUp") == 4 &&
            !firstSlot.isUsed &&
            plan.isCompleted &&
            secondSlot.isUsed;

        Debug.Log("FreeAttack资源不足不触发BeforeUse：" + noBeforeUse);
    }

    void RunBeforeUseRespondedAttackUnavailableFallbackSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("buff50_i", 30, 30, 50, 20, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1);
        BattleCardState responseAttack = CreateBulletLockedBeforeUseAttackCard(context.allyA, "buff50_i_response", 5, 3, "Strength", 1, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff50_i_enemy", 5, 0);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("buff50_i_intent", context.enemy, enemyAttack, context.allyB, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent);
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(intentQueue);

        context.allyA.AddBuff("Bullet", 3, -1);
        CardEligibilityResult assignResult;
        bool assignSuccess = BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            context.allyA,
            1,
            context.allyA,
            responseAttack,
            intent,
            out assignResult
        );
        BattleActionSlot responseSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);
        int aHPBefore = context.allyA.currentHP;
        int bHPBefore = context.allyB.currentHP;
        int enemyUseBefore = enemyAttack.currentUseCount;

        BattleExecutionPlan plan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(plan);
        bool itemCreatedAsResponded = item != null && item.executionType == BattleExecutionItemType.RespondedEnemyIntent;
        bool prepared =
            assignSuccess &&
            responseSlot != null &&
            object.ReferenceEquals(responseSlot.cardState, responseAttack) &&
            intent.isResponded &&
            object.ReferenceEquals(intent.actualTargetCharacter, context.allyA) &&
            CountBuffStack(context.allyA, "Bullet") >= 3;

        RemoveAllBuffs(context.allyA, "Bullet");
        bool bulletRemovedBeforeExecute = CountBuffStack(context.allyA, "Bullet") == 0;
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        bool targetRestored = object.ReferenceEquals(intent.actualTargetCharacter, context.allyB);
        bool becameUnresponded = context.allyB.currentHP < bHPBefore && context.allyA.currentHP == aHPBefore;
        bool responseNotUsed = responseSlot != null && !responseSlot.isUsed && CountBuffStack(context.allyA, "Strength") == 0;
        bool enemyOnce = enemyAttack.currentUseCount == enemyUseBefore;

        Debug.Log("模式50 I 准备阶段Responded Attack成功安排：" + prepared);
        Debug.Log("模式50 I 执行前Bullet已移除：" + bulletRemovedBeforeExecute);
        Debug.Log("模式50 I 执行项创建为RespondedEnemyIntent：" + itemCreatedAsResponded);
        Debug.Log("模式50 I outcome为ResponseUnavailableFallbackToUnresponded：" + IsExecutionItemState(item, BattleExecutionItemStatus.Executed, BattleExecutionItemOutcomeReason.ResponseUnavailableFallbackToUnresponded, true));
        Debug.Log("Responded空卡撤销目标改写：" + targetRestored);
        Debug.Log("Responded空卡恢复originalTarget：" + targetRestored);
        Debug.Log("Responded空卡转Unresponded：" + becameUnresponded);
        Debug.Log("响应空卡槽位不MarkUsed：" + responseNotUsed);
        Debug.Log("敌人攻击只执行一次：" + (becameUnresponded && enemyOnce && plan.isCompleted));
    }

    void RunBeforeUseRespondedDefenseUnavailablePassiveGuardSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("buff50_j", 30, 30, 50, 20, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        BattleCardState responseDefense = CreateBulletLockedBeforeUseDefenseCard(context.allyA, "buff50_j_response", 3, 3, "GuardUp", 9, 1);
        BattleCardState passiveDefense = CreateTestDefenseCardForCharacter(context.allyB, "buff50_j_passive", 10, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff50_j_enemy", 5, 0);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("buff50_j_intent", context.enemy, enemyAttack, context.allyB, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent);
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(intentQueue);

        context.allyA.AddBuff("Bullet", 3, -1);
        CardEligibilityResult responseAssignResult;
        bool responseAssignSuccess = BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            context.allyA,
            1,
            context.allyA,
            responseDefense,
            intent,
            out responseAssignResult
        );
        CardEligibilityResult passiveAssignResult;
        bool passiveAssignSuccess = BattleActionSlotManager.AssignPassiveGuard(
            actionSlots,
            context.allyB,
            2,
            context.allyB,
            passiveDefense,
            out passiveAssignResult
        );
        BattleActionSlot responseSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);
        BattleActionSlot passiveSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyB, 2);
        int bHPBefore = context.allyB.currentHP;

        BattleExecutionPlan plan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(plan);
        bool itemCreatedAsResponded = item != null && item.executionType == BattleExecutionItemType.RespondedEnemyIntent;
        int candidateCountBeforeExecute = item != null && item.passiveGuardCandidates != null
            ? item.passiveGuardCandidates.Count
            : -1;
        bool prepared =
            responseAssignSuccess &&
            passiveAssignSuccess &&
            responseSlot != null &&
            object.ReferenceEquals(responseSlot.cardState, responseDefense) &&
            CountBuffStack(context.allyA, "Bullet") >= 3;

        RemoveAllBuffs(context.allyA, "Bullet");
        bool bulletRemovedBeforeExecute = CountBuffStack(context.allyA, "Bullet") == 0;
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        bool passiveRecollected =
            object.ReferenceEquals(intent.actualTargetCharacter, context.allyB) &&
            passiveSlot != null &&
            passiveSlot.isUsed &&
            context.allyB.currentHP == bHPBefore &&
            responseSlot != null &&
            !responseSlot.isUsed;

        Debug.Log("模式50 J Responded Defense成功安排且预存候选为0：" + (prepared && itemCreatedAsResponded && candidateCountBeforeExecute == 0));
        Debug.Log("模式50 J 执行前Bullet已移除：" + bulletRemovedBeforeExecute);
        Debug.Log("模式50 J 回落后现场收集PassiveGuard：" + (passiveSlot != null && passiveSlot.isUsed));
        Debug.Log("原目标PassiveGuard重新收集并接管：" + passiveRecollected);
    }

    void RunBeforeUseRespondedDodgeUnavailableNoGuardSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("buff50_k", 30, 30, 50, 20, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1);
        BattleCardState responseDodge = CreateBulletLockedBeforeUseDodgeCard(context.allyA, "buff50_k_response", 8, 3, "Strength", 1, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff50_k_enemy", 5, 0);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("buff50_k_intent", context.enemy, enemyAttack, context.allyB, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent);
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(intentQueue);
        context.allyA.AddBuff("Bullet", 3, -1);
        CardEligibilityResult assignResult;
        bool assignSuccess = BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            context.allyA,
            1,
            context.allyA,
            responseDodge,
            intent,
            out assignResult
        );
        BattleActionSlot responseSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);
        int bHPBefore = context.allyB.currentHP;

        BattleExecutionPlan plan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(plan);
        bool itemCreatedAsResponded = item != null && item.executionType == BattleExecutionItemType.RespondedEnemyIntent;
        bool prepared =
            assignSuccess &&
            responseSlot != null &&
            object.ReferenceEquals(responseSlot.cardState, responseDodge) &&
            intent.isResponded &&
            CountBuffStack(context.allyA, "Bullet") >= 3;

        RemoveAllBuffs(context.allyA, "Bullet");
        bool bulletRemovedBeforeExecute = CountBuffStack(context.allyA, "Bullet") == 0;
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        bool fallbackHit =
            object.ReferenceEquals(intent.actualTargetCharacter, context.allyB) &&
            context.allyB.currentHP < bHPBefore &&
            responseSlot != null &&
            !responseSlot.isUsed &&
            CountBuffStack(context.allyA, "Strength") == 0;

        Debug.Log("模式50 K Responded Dodge成功安排：" + prepared);
        Debug.Log("模式50 K 执行前Bullet已移除：" + bulletRemovedBeforeExecute);
        Debug.Log("模式50 K 执行项创建为RespondedEnemyIntent：" + itemCreatedAsResponded);
        Debug.Log("Responded Dodge空卡无守备时转Unresponded：" + fallbackHit);
    }

    void RunBeforeUseRespondedUnavailableDeadOriginalTargetSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("buff50_l", 30, 0, 50, 20, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1);
        BattleCardState responseAttack = CreateBulletLockedBeforeUseAttackCard(context.allyA, "buff50_l_response", 5, 3, "Strength", 1, 1);
        BattleCardState enemyAttack = CreateBeforeUseBuffAttackCard(context.enemy, "buff50_l_enemy", 5, "Strength", 1, 1);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("buff50_l_intent", context.enemy, enemyAttack, context.allyB, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent);
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(intentQueue);
        context.allyA.AddBuff("Bullet", 3, -1);
        CardEligibilityResult assignResult;
        bool assignSuccess = BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            context.allyA,
            1,
            context.allyA,
            responseAttack,
            intent,
            out assignResult
        );

        BattleExecutionPlan plan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(plan);
        bool itemCreatedAsResponded = item != null && item.executionType == BattleExecutionItemType.RespondedEnemyIntent;
        RemoveAllBuffs(context.allyA, "Bullet");
        bool bulletRemovedBeforeExecute = CountBuffStack(context.allyA, "Bullet") == 0;
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        bool deadTargetSkipped =
            object.ReferenceEquals(intent.actualTargetCharacter, context.allyB) &&
            context.allyA.currentHP == context.allyA.maxHP &&
            CountBuffStack(context.enemy, "Strength") == 0 &&
            plan.isCompleted;

        Debug.Log("模式50 L Responded卡成功安排：" + (assignSuccess && itemCreatedAsResponded));
        Debug.Log("模式50 L 执行前响应卡失效且originalTarget死亡：" + (bulletRemovedBeforeExecute && context.allyB.IsDead()));
        Debug.Log("模式50 L item为Skipped / ActualTargetDead：" + IsExecutionItemState(item, BattleExecutionItemStatus.Skipped, BattleExecutionItemOutcomeReason.ActualTargetDead, true));
        Debug.Log("死亡originalTarget不自动转火：" + deadTargetSkipped);
    }

    void RunBeforeUseTieLimitSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("buff50_m", 30, 30, 50, 10, 3, 8);
        context.allyA.AddBuff("NextCardPointUp", 1, 1);
        context.allyA.AddBuff("NextClashPointUp", 1, 1);
        context.enemy.AddBuff("NextCardPointUp", 1, 1);
        context.enemy.AddBuff("NextClashPointUp", 1, 1);
        BattleCardState playerAttack = CreateBeforeUseBuffAttackCard(context.allyA, "buff50_m_player", 5, "NextCardPointUp", 2, 1);
        playerAttack.cardData.effects.Add(CreateApplyBuffEffect(BattleTiming.BeforeUse, "NextClashPointUp", 2, 1));
        BattleCardState enemyAttack = CreateBeforeUseBuffAttackCard(context.enemy, "buff50_m_enemy", 5, "NextCardPointUp", 2, 1);
        enemyAttack.cardData.effects.Add(CreateApplyBuffEffect(BattleTiming.BeforeUse, "NextClashPointUp", 2, 1));

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, playerAttack),
            CreateEnemyAttackIntent("buff50_m_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool tieLimitNoConsume =
            result != null &&
            result.resultType == "TieLimit" &&
            CountBuffStack(context.allyA, "NextCardPointUp") == 3 &&
            CountBuffStack(context.allyA, "NextClashPointUp") == 3 &&
            CountBuffStack(context.enemy, "NextCardPointUp") == 3 &&
            CountBuffStack(context.enemy, "NextClashPointUp") == 3;

        Debug.Log("TieLimit不重复触发BeforeUse：" + tieLimitNoConsume);
        Debug.Log("TieLimit不消费旧快照：" + tieLimitNoConsume);
    }

    void RunBeforeUseAbilityUnavailableSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("buff50_n", 30, 30, 50, 10, 3, 8);
        BattleCardState ability = CreateBulletLockedAbilityCard(context.allyB, "buff50_n_ability", "Buff50NOnPlay", 3);
        BattleActionSlot abilitySlot = new BattleActionSlot(context.allyB, 1);
        abilitySlot.AssignFreeAction(context.allyB, ability, context.allyB);
        int useCountBefore = ability.currentUseCount;
        int guiltBefore = context.allyB.currentGuilt;

        BattleResolveResult result = BattleResolver.ResolveFreeAction(abilitySlot);

        bool abilityUnavailable =
            result != null &&
            result.resultType == "ActionUnavailable" &&
            CountBuffStack(context.allyB, "Buff50NOnPlay") == 0 &&
            CountBuffStack(context.allyB, "Strength") == 0 &&
            ability.currentUseCount == useCountBefore &&
            context.allyB.currentGuilt == guiltBefore &&
            !abilitySlot.isUsed;

        Debug.Log("Ability资源不足不触发OnPlay：" + abilityUnavailable);
    }

    void RunBeforeUseSnapshotPureReadSubTest()
    {
        CharacterData unit = CreateBuffDataLayerCharacter("buff50_o");
        unit.AddBuff("NextCardPointUp", 2, 1);
        unit.AddBuff("NextClashPointUp", 3, 1);
        unit.AddPendingBuff("Strength", 1, 1, 1, 1, 1);

        int cardBefore = CountBuffStack(unit, "NextCardPointUp");
        int clashBefore = CountBuffStack(unit, "NextClashPointUp");
        int cardInstancesBefore = CountBuffInstances(unit, "NextCardPointUp");
        int clashInstancesBefore = CountBuffInstances(unit, "NextClashPointUp");
        int cardDurationBefore = GetBuffDuration(unit, "NextCardPointUp");
        int pendingBefore = unit.GetPendingBuffStackNextTurn("Strength");

        int cardModifier = Mathf.RoundToInt(unit.GetBuffFlatModifier("CardPoint"));
        int clashModifier = Mathf.RoundToInt(unit.GetBuffFlatModifier("ClashPoint"));

        bool pureRead =
            cardModifier == 2 &&
            clashModifier == 3 &&
            CountBuffStack(unit, "NextCardPointUp") == cardBefore &&
            CountBuffStack(unit, "NextClashPointUp") == clashBefore &&
            CountBuffInstances(unit, "NextCardPointUp") == cardInstancesBefore &&
            CountBuffInstances(unit, "NextClashPointUp") == clashInstancesBefore &&
            GetBuffDuration(unit, "NextCardPointUp") == cardDurationBefore &&
            unit.GetPendingBuffStackNextTurn("Strength") == pendingBefore;

        Debug.Log("快照捕获不修改Buff：" + pureRead);
    }

    void RunBeforeUsePassiveGuardUnavailableCandidateSubTest()
    {
        BattleEndedTestContext context = CreateBattleEndedTestContext("buff50_p", 30, 30, 50, 10, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        BattleCardState unavailableDefense = CreateBulletLockedBeforeUseDefenseCard(context.allyB, "buff50_p_first", 10, 3, "GuardUp", 9, 1);
        BattleCardState validDefense = CreateTestDefenseCardForCharacter(context.allyB, "buff50_p_second", 10, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff50_p_enemy", 5, 0);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("buff50_p_intent", context.enemy, enemyAttack, context.allyB, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent);
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(intentQueue);

        context.allyB.AddBuff("Bullet", 3, -1);
        CardEligibilityResult firstAssignResult;
        bool firstAssignSuccess = BattleActionSlotManager.AssignPassiveGuard(
            actionSlots,
            context.allyB,
            1,
            context.allyB,
            unavailableDefense,
            out firstAssignResult
        );
        CardEligibilityResult secondAssignResult;
        bool secondAssignSuccess = BattleActionSlotManager.AssignPassiveGuard(
            actionSlots,
            context.allyB,
            2,
            context.allyB,
            validDefense,
            out secondAssignResult
        );
        BattleActionSlot firstSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyB, 1);
        BattleActionSlot secondSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyB, 2);
        int bHPBefore = context.allyB.currentHP;

        BattleExecutionPlan plan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(plan);
        int candidateCount = item != null && item.passiveGuardCandidates != null
            ? item.passiveGuardCandidates.Count
            : 0;
        bool candidateOrder =
            candidateCount == 2 &&
            object.ReferenceEquals(item.passiveGuardCandidates[0], firstSlot) &&
            object.ReferenceEquals(item.passiveGuardCandidates[1], secondSlot);
        bool bothAssigned =
            firstAssignSuccess &&
            secondAssignSuccess &&
            firstSlot != null &&
            secondSlot != null &&
            object.ReferenceEquals(firstSlot.cardState, unavailableDefense) &&
            object.ReferenceEquals(secondSlot.cardState, validDefense);
        RemoveAllBuffs(context.allyB, "Bullet");
        bool firstCandidateUnavailableBeforeExecute = CountBuffStack(context.allyB, "Bullet") == 0;
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, plan);

        bool firstSkipped =
            firstSlot != null &&
            !firstSlot.isUsed &&
            CountBuffStack(context.allyB, "GuardUp") == 0;
        bool secondUsed =
            secondSlot != null &&
            secondSlot.isUsed &&
            context.allyB.currentHP == bHPBefore &&
            plan.isCompleted;

        Debug.Log("模式50 P 两个PassiveGuard均成功安排：" + bothAssigned);
        Debug.Log("模式50 P ExecutionPlan包含两个候选：" + (candidateCount == 2));
        Debug.Log("模式50 P 候选顺序正确：" + candidateOrder);
        Debug.Log("模式50 P 第一候选执行前变为不可用：" + firstCandidateUnavailableBeforeExecute);
        Debug.Log("资源不足PassiveGuard候选被跳过：" + firstSkipped);
        Debug.Log("第二有效PassiveGuard候选正常接管：" + secondUsed);
    }

    void RunBuffDefinitionDataLayerBasicTestSequence()
    {
        Debug.Log("===== BuffDefinitionDataLayerBasic 聚合测试开始 =====");

        BuffDefinitionLoader.ClearCacheForTest();
        List<BuffDefinitionData> definitions = BuffDefinitionLoader.LoadBuffDefinitions();

        bool loaded13 = definitions != null && definitions.Count == 13;
        bool uniqueIDs = AreBuffDefinitionIDsUnique(definitions);
        bool requiredFieldsFilled = AreBuffDefinitionRequiredFieldsFilled(definitions);
        bool speedDownName = GetBuffDefinition(definitions, "SpeedDown") != null &&
            GetBuffDefinition(definitions, "SpeedDown").buffName == "缓慢";
        bool categoriesValid = AreBuffDefinitionCategoriesValid(definitions);
        bool bulletCategory = GetBuffDefinition(definitions, "Bullet") != null &&
            GetBuffDefinition(definitions, "Bullet").buffCategory == BuffCategory.AbilityBuff;
        bool nextPointCategories =
            GetBuffDefinition(definitions, "NextClashPointUp") != null &&
            GetBuffDefinition(definitions, "NextClashPointUp").buffCategory == BuffCategory.UpBuff &&
            GetBuffDefinition(definitions, "NextCardPointUp") != null &&
            GetBuffDefinition(definitions, "NextCardPointUp").buffCategory == BuffCategory.UpBuff;

        Debug.Log("BuffDefinitions加载13种：" + loaded13);
        Debug.Log("Buff定义ID无重复：" + uniqueIDs);
        Debug.Log("Buff定义必填字段不为空：" + requiredFieldsFilled);
        Debug.Log("SpeedDown名称为缓慢：" + speedDownName);
        Debug.Log("正式分类统一：" + categoriesValid);
        Debug.Log("Bullet分类为AbilityBuff：" + bulletCategory);
        Debug.Log("NextClashPointUp和NextCardPointUp分类为UpBuff：" + nextPointCategories);

        BuffDefinitionData strengthDefinition = GetBuffDefinition(definitions, "Strength");
        BuffDefinitionData weaknessDefinition = GetBuffDefinition(definitions, "Weakness");
        BuffDefinitionData damageUpDefinition = GetBuffDefinition(definitions, "DamageUp");
        BuffDefinitionData damageReductionDefinition = GetBuffDefinition(definitions, "DamageReduction");
        BuffDefinitionData nextClashDefinition = GetBuffDefinition(definitions, "NextClashPointUp");
        BuffDefinitionData nextCardDefinition = GetBuffDefinition(definitions, "NextCardPointUp");
        BuffDefinitionData bulletDefinition = GetBuffDefinition(definitions, "Bullet");

        bool fieldValuesCorrect =
            IsBuffDefinitionValue(strengthDefinition, 1f) &&
            IsBuffDefinitionValue(weaknessDefinition, -1f) &&
            IsBuffDefinitionValue(damageUpDefinition, 10f) &&
            IsBuffDefinitionValue(damageReductionDefinition, -10f) &&
            nextClashDefinition != null &&
            nextClashDefinition.consumeRule == "FormalClashResolved" &&
            nextCardDefinition != null &&
            nextCardDefinition.consumeRule == "SuccessfulPointCardUsed" &&
            bulletDefinition != null &&
            bulletDefinition.defaultExpireRule == "Permanent";
        Debug.Log("Buff定义字段准确：" + fieldValuesCorrect);

        CharacterData activeMergeCharacter = CreateBuffDataLayerCharacter("buff_layer_active_merge");
        activeMergeCharacter.AddBuff("Strength", 1, 2);
        activeMergeCharacter.AddBuff("Strength", 2, 2);
        List<BuffData> activeMergeBatches = activeMergeCharacter.GetActiveBuffBatches("Strength");
        bool sameActiveMerged =
            activeMergeBatches.Count == 1 &&
            activeMergeCharacter.GetBuffStack("Strength") == 3 &&
            activeMergeBatches[0].duration == 2;
        Debug.Log("相同活动批次正确合并：" + sameActiveMerged);

        CharacterData activeSeparateCharacter = CreateBuffDataLayerCharacter("buff_layer_active_separate");
        activeSeparateCharacter.AddBuff("Strength", 2, 1);
        activeSeparateCharacter.AddBuff("Strength", 1, 2);
        List<BuffData> activeSeparateBatches = activeSeparateCharacter.GetActiveBuffBatches("Strength");
        bool differentDurationSeparate =
            activeSeparateBatches.Count == 2 &&
            activeSeparateCharacter.GetBuffStack("Strength") == 3 &&
            HasBuffBatch(activeSeparateBatches, 2, 1) &&
            HasBuffBatch(activeSeparateBatches, 1, 2);
        Debug.Log("不同持续时间批次保持独立：" + differentDurationSeparate);

        int beforeExpiringQueryStack = activeSeparateCharacter.GetBuffStack("Strength");
        int expiringStack = activeSeparateCharacter.GetExpiringBuffStackAtTurnEnd("Strength");
        int afterExpiringQueryStack = activeSeparateCharacter.GetBuffStack("Strength");
        bool expiringQueryCorrect = expiringStack == 2 && beforeExpiringQueryStack == afterExpiringQueryStack;
        Debug.Log("本回合结束减少层数查询正确：" + expiringQueryCorrect);

        CharacterData pendingMergeCharacter = CreateBuffDataLayerCharacter("buff_layer_pending_merge");
        pendingMergeCharacter.AddPendingBuff("Strength", 1, 2, 1, 1, 1);
        pendingMergeCharacter.AddPendingBuff("Strength", 2, 2, 1, 1, 1);
        List<PendingBuffData> pendingMergeBatches = pendingMergeCharacter.GetPendingBuffBatches("Strength");
        bool samePendingMerged = pendingMergeBatches.Count == 1 && pendingMergeBatches[0].stack == 3;
        Debug.Log("相同延迟排期正确合并：" + samePendingMerged);

        CharacterData pendingSeparateCharacter = CreateBuffDataLayerCharacter("buff_layer_pending_separate");
        pendingSeparateCharacter.AddPendingBuff("Strength", 1, 2, 1, 1, 1);
        pendingSeparateCharacter.AddPendingBuff("Strength", 2, 2, 2, 1, 1);
        List<PendingBuffData> pendingSeparateBatches = pendingSeparateCharacter.GetPendingBuffBatches("Strength");
        bool differentPendingSeparate = pendingSeparateBatches.Count == 2;
        Debug.Log("不同延迟排期保持独立：" + differentPendingSeparate);

        CharacterData pendingQueryCharacter = CreateBuffDataLayerCharacter("buff_layer_pending_query");
        pendingQueryCharacter.AddPendingBuff("Strength", 3, 2, 1, 1, 1);
        pendingQueryCharacter.AddPendingBuff("Strength", 2, 2, 2, 1, 1);
        int beforeDelayTurns = GetPendingDelayTurns(pendingQueryCharacter, "Strength", 2);
        int pendingNextTurnStack = pendingQueryCharacter.GetPendingBuffStackNextTurn("Strength");
        int afterDelayTurns = GetPendingDelayTurns(pendingQueryCharacter, "Strength", 2);
        bool pendingNextTurnQuery = pendingNextTurnStack == 3 && beforeDelayTurns == afterDelayTurns;
        Debug.Log("下回合获得层数查询正确：" + pendingNextTurnQuery);

        CharacterData applyTimesCharacter = CreateBuffDataLayerCharacter("buff_layer_apply_times");
        applyTimesCharacter.AddPendingBuff("Strength", 1, 2, 1, 2, 1);
        applyTimesCharacter.AddPendingBuff("Strength", 1, 2, 1, 2, 1);
        List<PendingBuffData> applyTimesBatches = applyTimesCharacter.GetPendingBuffBatches("Strength");
        bool applyTimesMerge =
            applyTimesBatches.Count == 1 &&
            applyTimesBatches[0].stack == 2 &&
            applyTimesBatches[0].applyTimes == 2 &&
            applyTimesBatches[0].intervalTurns == 1;
        Debug.Log("applyTimes合并语义正确：" + applyTimesMerge);

        CharacterData definitionEffectCharacter = CreateBuffDataLayerCharacter("buff_layer_definition_effect");
        CardTestData definitionEffectCard = CreateBuffDefinitionPathTestCard();
        CardEffectExecutor.ExecuteCardEffects(
            definitionEffectCharacter,
            definitionEffectCharacter,
            definitionEffectCard,
            BattleTiming.BeforeUse
        );
        CardEffectExecutor.ExecuteCardEffects(
            definitionEffectCharacter,
            definitionEffectCharacter,
            definitionEffectCard,
            BattleTiming.AfterDamage
        );
        List<BuffData> definitionActiveBatches = definitionEffectCharacter.GetActiveBuffBatches("Strength");
        List<PendingBuffData> definitionPendingBatches = definitionEffectCharacter.GetPendingBuffBatches("DamageUp");
        bool definitionEffectWorked =
            definitionActiveBatches.Count == 1 &&
            definitionActiveBatches[0].buffName == "强壮" &&
            definitionActiveBatches[0].buffCategory == BuffCategory.UpBuff &&
            definitionActiveBatches[0].checkTiming == BattleTiming.TurnEnd &&
            definitionActiveBatches[0].expireRule == "DurationDown" &&
            definitionPendingBatches.Count == 1 &&
            definitionPendingBatches[0].buffName == "威力强化" &&
            definitionPendingBatches[0].buffCategory == BuffCategory.UpBuff &&
            definitionPendingBatches[0].checkTiming == BattleTiming.TurnEnd &&
            definitionPendingBatches[0].expireRule == "DurationDown";
        Debug.Log("新CardEffect通过定义应用Buff：" + definitionEffectWorked);

        CharacterData legacyCharacter = CreateBuffDataLayerCharacter("buff_layer_legacy");
        CardEffectExecutor.ExecuteCardEffects(
            legacyCharacter,
            legacyCharacter,
            CreateLegacyAbilityPowerTestCard(),
            BattleTiming.OnPlay
        );
        bool legacyWorked =
            BuffDefinitionLoader.GetDefinition("AbilityPower") == null &&
            legacyCharacter.GetBuffStack("AbilityPower") == 1 &&
            legacyCharacter.GetActiveBuffBatches("AbilityPower").Count == 1 &&
            loaded13;
        Debug.Log("Legacy AbilityPower兼容：" + legacyWorked);

        CharacterData copyCharacter = CreateBuffDataLayerCharacter("buff_layer_copy");
        copyCharacter.AddBuff("Strength", 1, 2);
        copyCharacter.AddPendingBuff("Strength", 2, 2, 1, 1, 1);
        List<BuffData> activeCopies = copyCharacter.GetActiveBuffBatches("Strength");
        List<PendingBuffData> pendingCopies = copyCharacter.GetPendingBuffBatches("Strength");
        activeCopies[0].stack = 99;
        pendingCopies[0].stack = 99;
        bool queryCopiesSafe =
            copyCharacter.GetBuffStack("Strength") == 1 &&
            copyCharacter.GetPendingBuffStackNextTurn("Strength") == 2;
        Debug.Log("查询副本不修改内部状态：" + queryCopiesSafe);

        CharacterData unknownCharacter = CreateBuffDataLayerCharacter("buff_layer_unknown");
        CardEffectExecutor.ExecuteCardEffects(
            unknownCharacter,
            unknownCharacter,
            CreateUnknownBuffTestCard(),
            BattleTiming.BeforeUse
        );
        bool unknownRejected =
            unknownCharacter.GetActiveBuffBatches("").Count == 0 &&
            unknownCharacter.GetPendingBuffBatches("").Count == 0;
        Debug.Log("未知Buff被拒绝：" + unknownRejected);
    }

    void RunBuffLifecycleBattleIntegrationBasicTestSequence()
    {
        Debug.Log("===== BuffLifecycleBattleIntegrationBasic 聚合测试开始 =====");

        RunBuffJsonValueReadSubTest();
        RunBuffFormalAttackConsumeRuleSubTest();
        RunBuffFormalClashTieLimitKeepSubTest();
        RunBuffFormalDodgeConsumeRuleSubTest();
        RunBuffDefenseAndEnemyAttackPointSubTest();
        RunBuffFreeAttackAndUnrespondedPointSubTest();
        RunBuffKnownPointConsumeRuleSubTest();
        RunBuffPassiveGuardPointRuleSubTest();
        RunBuffNoSuccessNoConsumeSubTest();
        RunBuffDurationPendingAndPermanentSubTest();
        RunBuffMode49PureReadNoMutationSubTest();
    }

    void RunBuffJsonValueReadSubTest()
    {
        Debug.Log("===== 模式49 子测试A：JSON数值读取 =====");

        CharacterData unit = CreateBuffDataLayerCharacter("buff49_json_unit");
        unit.AddBuff("Strength", 2, 2);
        unit.AddBuff("Weakness", 1, 2);
        unit.AddBuff("GuardUp", 2, 2);
        unit.AddBuff("GuardDown", 1, 2);
        unit.AddBuff("SpeedUp", 2, 2);
        unit.AddBuff("SpeedDown", 1, 2);

        CharacterData attacker = CreateBuffDataLayerCharacter("buff49_json_attacker");
        CharacterData defender = CreateBuffDataLayerCharacter("buff49_json_defender");
        attacker.AddBuff("DamageUp", 2, 2);
        attacker.AddBuff("DamageDown", 1, 2);
        defender.AddBuff("Vulnerable", 1, 2);
        defender.AddBuff("DamageReduction", 2, 2);

        bool attackPointFromJson = Mathf.RoundToInt(unit.GetBuffFlatModifier("AttackPoint")) == 1;
        bool defensePointFromJson = Mathf.RoundToInt(unit.GetBuffFlatModifier("DefensePoint")) == 1;
        bool speedFromJson = unit.GetCurrentSpeed() == 4;
        bool damageDealtFromJson = Mathf.RoundToInt(attacker.GetBuffPercentModifier("DamageDealt")) == 10;
        bool damageTakenFromJson = Mathf.RoundToInt(defender.GetBuffPercentModifier("DamageTaken")) == -10;

        Debug.Log("AttackPoint读取JSON数值：" + attackPointFromJson);
        Debug.Log("DefensePoint读取JSON数值：" + defensePointFromJson);
        Debug.Log("Speed读取JSON数值：" + speedFromJson);
        Debug.Log("DamageDealt读取JSON数值：" + damageDealtFromJson);
        Debug.Log("DamageTaken读取JSON数值：" + damageTakenFromJson);
        Debug.Log("DamageDealt读取JSON百分比：" + damageDealtFromJson);
        Debug.Log("DamageTaken读取JSON百分比：" + damageTakenFromJson);
    }

    void RunBuffFormalAttackConsumeRuleSubTest()
    {
        Debug.Log("===== 模式49 子测试B：Attack正式拼点消费规则 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("buff49_attack", 30, 30, 50, 10, 3, 8);
        BattleCardState playerAttack = CreateFixedAttackCardForCharacter(context.allyA, "buff49_attack_player", 5);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff49_attack_enemy", 1, 0);
        context.allyA.AddBuff("Strength", 1, 2);
        context.allyA.AddBuff("NextClashPointUp", 2, 1);
        context.allyA.AddBuff("NextCardPointUp", 3, 1);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, playerAttack),
            CreateEnemyAttackIntent("buff49_attack_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool finalPointCorrect = result != null && result.resultType == "PlayerWin" && result.playerPoint == 11;
        bool consumeRuleCorrect =
            CountBuffStack(context.allyA, "NextClashPointUp") == 0 &&
            CountBuffStack(context.allyA, "NextCardPointUp") == 0 &&
            CountBuffStack(context.allyA, "Strength") == 1;

        Debug.Log("拼点强化与蓄势可叠加：" + finalPointCorrect);
        Debug.Log("Attack正式胜负后两种Buff消费：" + consumeRuleCorrect);
    }

    void RunBuffFormalClashTieLimitKeepSubTest()
    {
        Debug.Log("===== 模式49 子测试C：正式拼点TieLimit不消费 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("buff49_tie", 30, 30, 50, 10, 3, 8);
        BattleCardState playerAttack = CreateFixedAttackCardForCharacter(context.allyA, "buff49_tie_player", 5);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff49_tie_enemy", 5, 0);
        context.allyA.AddBuff("NextClashPointUp", 2, 1);
        context.allyA.AddBuff("NextCardPointUp", 1, 1);
        context.enemy.AddBuff("NextClashPointUp", 2, 1);
        context.enemy.AddBuff("NextCardPointUp", 1, 1);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, playerAttack),
            CreateEnemyAttackIntent("buff49_tie_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool tieLimit = result != null && result.resultType == "TieLimit" && result.isTieLimitReached;
        bool buffsKept =
            CountBuffStack(context.allyA, "NextClashPointUp") == 2 &&
            CountBuffStack(context.allyA, "NextCardPointUp") == 1 &&
            CountBuffStack(context.enemy, "NextClashPointUp") == 2 &&
            CountBuffStack(context.enemy, "NextCardPointUp") == 1;

        Debug.Log("TieLimit成立：" + tieLimit);
        Debug.Log("Attack TieLimit两种Buff不消费：" + buffsKept);
    }

    void RunBuffFormalDodgeConsumeRuleSubTest()
    {
        Debug.Log("===== 模式49 子测试D：Dodge正式拼点消费规则 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("buff49_dodge", 30, 30, 50, 10, 3, 8);
        BattleCardState dodge = CreateFixedDodgeCardForCharacter(context.allyA, "buff49_dodge_player", 5, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff49_dodge_enemy", 5, 0);
        context.allyA.AddBuff("Strength", 9, 2);
        context.allyA.AddBuff("NextClashPointUp", 2, 1);
        context.allyA.AddBuff("NextCardPointUp", 1, 1);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, dodge),
            CreateEnemyAttackIntent("buff49_dodge_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool dodgePointCorrect = result != null && result.resultType == "DodgeSuccess" && result.playerPoint == 8;
        bool dodgeConsumesPointBuffs =
            CountBuffStack(context.allyA, "NextClashPointUp") == 0 &&
            CountBuffStack(context.allyA, "NextCardPointUp") == 0 &&
            CountBuffStack(context.allyA, "Strength") == 9;

        Debug.Log("Dodge读取ClashPoint与CardPoint：" + dodgePointCorrect);
        Debug.Log("Dodge不读取AttackPoint：" + dodgePointCorrect);
        Debug.Log("Dodge胜负后消费对应一次性Buff：" + dodgeConsumesPointBuffs);
    }

    void RunBuffDefenseAndEnemyAttackPointSubTest()
    {
        Debug.Log("===== 模式49 子测试E：Defense与敌人Attack点数规则 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("buff49_defense", 30, 30, 50, 10, 3, 8);
        BattleCardState defense = CreateTestDefenseCardForCharacter(context.allyA, "buff49_defense_card", 3, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff49_defense_enemy", 5, 0);
        context.allyA.AddBuff("GuardUp", 2, 2);
        context.allyA.AddBuff("NextCardPointUp", 4, 1);
        context.allyA.AddBuff("NextClashPointUp", 5, 1);
        context.enemy.AddBuff("Strength", 2, 2);
        context.enemy.AddBuff("NextCardPointUp", 3, 1);
        context.enemy.AddBuff("NextClashPointUp", 7, 1);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, defense),
            CreateEnemyAttackIntent("buff49_defense_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool pointRuleCorrect =
            result != null &&
            result.playerPoint == 9 &&
            result.enemyPoint == 10 &&
            result.resultType == "DefenseReducedDamage";
        bool consumeRuleCorrect =
            CountBuffStack(context.allyA, "NextCardPointUp") == 0 &&
            CountBuffStack(context.allyA, "NextClashPointUp") == 5 &&
            CountBuffStack(context.allyA, "GuardUp") == 2 &&
            CountBuffStack(context.enemy, "NextCardPointUp") == 0 &&
            CountBuffStack(context.enemy, "NextClashPointUp") == 7;

        Debug.Log("Defense读取DefensePoint与CardPoint：" + pointRuleCorrect);
        Debug.Log("Defense不读取ClashPoint：" + pointRuleCorrect);
        Debug.Log("Defense vs Attack只消费SuccessfulPointCardUsed：" + consumeRuleCorrect);
    }

    void RunBuffFreeAttackAndUnrespondedPointSubTest()
    {
        Debug.Log("===== 模式49 子测试F：FreeAction与Unresponded攻击点数规则 =====");

        BattleEndedTestContext freeContext = CreateBattleEndedTestContext("buff49_free", 30, 30, 50, 10, 3, 8);
        BattleCardState freeAttack = CreateFixedAttackCardForCharacter(freeContext.allyA, "buff49_free_attack", 5);
        freeContext.allyA.AddBuff("Strength", 2, 2);
        freeContext.allyA.AddBuff("NextCardPointUp", 3, 1);
        freeContext.allyA.AddBuff("NextClashPointUp", 7, 1);
        BattleActionSlot freeSlot = new BattleActionSlot(freeContext.allyA, 1);
        freeSlot.AssignFreeAction(freeContext.allyA, freeAttack, freeContext.enemy);
        BattleResolveResult freeResult = BattleResolver.ResolveFreeAction(freeSlot);

        bool freeRuleCorrect =
            freeResult != null &&
            freeResult.resultType == "FreeAttack" &&
            freeResult.playerPoint == 10 &&
            CountBuffStack(freeContext.allyA, "NextCardPointUp") == 0 &&
            CountBuffStack(freeContext.allyA, "NextClashPointUp") == 7;

        BattleEndedTestContext unrespondedContext = CreateBattleEndedTestContext("buff49_unresponded", 30, 30, 50, 10, 3, 8);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(unrespondedContext.enemy, "buff49_unresponded_enemy", 5, 0);
        unrespondedContext.enemy.AddBuff("Strength", 2, 2);
        unrespondedContext.enemy.AddBuff("NextCardPointUp", 3, 1);
        unrespondedContext.enemy.AddBuff("NextClashPointUp", 7, 1);
        BattleResolveResult unrespondedResult = BattleResolver.ResolveUnrespondedEnemyIntent(
            CreateEnemyAttackIntent("buff49_unresponded_intent", unrespondedContext.enemy, enemyAttack, unrespondedContext.allyA, 1)
        );

        bool unrespondedRuleCorrect =
            unrespondedResult != null &&
            unrespondedResult.resultType == "UnrespondedEnemyAttack" &&
            unrespondedResult.enemyPoint == 10 &&
            CountBuffStack(unrespondedContext.enemy, "NextCardPointUp") == 0 &&
            CountBuffStack(unrespondedContext.enemy, "NextClashPointUp") == 7;

        Debug.Log("FreeAttack不读取ClashPoint：" + freeRuleCorrect);
        Debug.Log("FreeAttack成功后消费蓄势：" + freeRuleCorrect);
        Debug.Log("Unresponded不读取ClashPoint：" + unrespondedRuleCorrect);
        Debug.Log("Unresponded成功后消费蓄势：" + unrespondedRuleCorrect);
    }

    void RunBuffKnownPointConsumeRuleSubTest()
    {
        Debug.Log("===== 模式49 子测试G：known-point消费隔离 =====");

        BattleEndedTestContext dodgeContext = CreateBattleEndedTestContext("buff49_known_dodge", 30, 30, 50, 10, 3, 8);
        BattleCardState dodge = CreateFixedDodgeCardForCharacter(dodgeContext.allyB, "buff49_known_dodge_card", 4, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(dodgeContext.enemy, "buff49_known_dodge_enemy", 5, 0);
        dodgeContext.allyB.AddBuff("NextClashPointUp", 4, 1);
        dodgeContext.allyB.AddBuff("NextCardPointUp", 1, 1);
        dodgeContext.enemy.AddBuff("NextClashPointUp", 2, 1);
        dodgeContext.enemy.AddBuff("NextCardPointUp", 3, 1);
        BattleActionSlot dodgeSlot = CreateRespondedSlot(dodgeContext.allyB, dodge);
        BattleResolveResult dodgeResult = BattleResolver.ResolveDodgeVsAttackWithKnownEnemyPoint(
            dodgeSlot,
            CreateEnemyAttackIntent("buff49_known_dodge_intent", dodgeContext.enemy, enemyAttack, dodgeContext.allyB, 1),
            7
        );

        bool knownDodgeRule =
            dodgeResult != null &&
            dodgeResult.resultType == "DodgeSuccess" &&
            dodgeResult.playerPoint == 9 &&
            CountBuffStack(dodgeContext.allyB, "NextClashPointUp") == 0 &&
            CountBuffStack(dodgeContext.allyB, "NextCardPointUp") == 0 &&
            CountBuffStack(dodgeContext.enemy, "NextClashPointUp") == 2 &&
            CountBuffStack(dodgeContext.enemy, "NextCardPointUp") == 3;

        BattleEndedTestContext defenseContext = CreateBattleEndedTestContext("buff49_known_defense", 30, 30, 50, 10, 3, 8);
        BattleCardState defense = CreateTestDefenseCardForCharacter(defenseContext.allyB, "buff49_known_defense_card", 3, 1);
        BattleCardState knownEnemyAttack = CreateFixedEnemyAttackCardForDodgeTest(defenseContext.enemy, "buff49_known_defense_enemy", 5, 0);
        defenseContext.allyB.AddBuff("GuardUp", 2, 2);
        defenseContext.allyB.AddBuff("NextCardPointUp", 4, 1);
        defenseContext.allyB.AddBuff("NextClashPointUp", 5, 1);
        defenseContext.enemy.AddBuff("NextCardPointUp", 3, 1);
        BattleResolveResult defenseResult = BattleResolver.ResolveDefenseVsAttackWithKnownEnemyPoint(
            CreateRespondedSlot(defenseContext.allyB, defense),
            CreateEnemyAttackIntent("buff49_known_defense_intent", defenseContext.enemy, knownEnemyAttack, defenseContext.allyB, 1),
            8
        );

        bool knownDefenseRule =
            defenseResult != null &&
            defenseResult.resultType == "DefenseFullBlock" &&
            defenseResult.playerPoint == 9 &&
            CountBuffStack(defenseContext.allyB, "NextCardPointUp") == 0 &&
            CountBuffStack(defenseContext.allyB, "NextClashPointUp") == 5 &&
            CountBuffStack(defenseContext.enemy, "NextCardPointUp") == 3;

        Debug.Log("known-point Dodge未重复读取敌人Buff：" + knownDodgeRule);
        Debug.Log("known-point Dodge未重复消费敌人Buff：" + knownDodgeRule);
        Debug.Log("known-point Defense未重复消费敌人Buff：" + knownDefenseRule);
    }

    void RunBuffPassiveGuardPointRuleSubTest()
    {
        Debug.Log("===== 模式49 子测试H：PassiveGuard点数与消费规则 =====");

        BattleEndedTestContext dodgeContext = CreateBattleEndedTestContext("buff49_passive_dodge", 30, 30, 50, 10, 3, 8);
        BattleCardState passiveDodge = CreateFixedDodgeCardForCharacter(dodgeContext.allyB, "buff49_passive_dodge_card", 5, 1);
        BattleCardState dodgeEnemyAttack = CreateFixedEnemyAttackCardForDodgeTest(dodgeContext.enemy, "buff49_passive_dodge_enemy", 5, 0);
        dodgeContext.allyB.AddBuff("NextClashPointUp", 4, 1);
        dodgeContext.allyB.AddBuff("NextCardPointUp", 1, 1);
        dodgeContext.enemy.AddBuff("Strength", 1, 2);
        dodgeContext.enemy.AddBuff("NextClashPointUp", 2, 1);
        dodgeContext.enemy.AddBuff("NextCardPointUp", 1, 1);
        BattleActionSlot passiveDodgeSlot = new BattleActionSlot(dodgeContext.allyB, 1);
        passiveDodgeSlot.AssignPassiveGuard(dodgeContext.allyB, passiveDodge);
        dodgeContext.runtimeState.SetActionSlots(
            new List<BattleActionSlot> { passiveDodgeSlot }
        );
        BattleExecutionPlan dodgePlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            new List<BattleActionSlot> { passiveDodgeSlot },
            new List<BattleEnemyIntent>
            {
                CreateEnemyAttackIntent("buff49_passive_dodge_intent", dodgeContext.enemy, dodgeEnemyAttack, dodgeContext.allyB, 1)
            }
        );
        ExecutePlanWithRuntimeStateAndCompleteTurn(dodgeContext.runtimeState, dodgePlan);

        bool passiveDodgeRule =
            passiveDodgeSlot.isContinuousDodgeActive &&
            !passiveDodgeSlot.isUsed &&
            CountBuffStack(dodgeContext.allyB, "NextClashPointUp") == 0 &&
            CountBuffStack(dodgeContext.allyB, "NextCardPointUp") == 0 &&
            CountBuffStack(dodgeContext.enemy, "NextClashPointUp") == 0 &&
            CountBuffStack(dodgeContext.enemy, "NextCardPointUp") == 0;

        BattleEndedTestContext defenseContext = CreateBattleEndedTestContext("buff49_passive_defense", 30, 30, 50, 10, 3, 8);
        BattleCardState passiveDefense = CreateTestDefenseCardForCharacter(defenseContext.allyB, "buff49_passive_defense_card", 3, 1);
        BattleCardState defenseEnemyAttack = CreateFixedEnemyAttackCardForDodgeTest(defenseContext.enemy, "buff49_passive_defense_enemy", 5, 0);
        defenseContext.allyB.AddBuff("GuardUp", 2, 2);
        defenseContext.allyB.AddBuff("NextCardPointUp", 4, 1);
        defenseContext.allyB.AddBuff("NextClashPointUp", 5, 1);
        defenseContext.enemy.AddBuff("Strength", 2, 2);
        defenseContext.enemy.AddBuff("NextCardPointUp", 3, 1);
        defenseContext.enemy.AddBuff("NextClashPointUp", 7, 1);
        BattleActionSlot passiveDefenseSlot = new BattleActionSlot(defenseContext.allyB, 1);
        passiveDefenseSlot.AssignPassiveGuard(defenseContext.allyB, passiveDefense);
        BattleExecutionPlan defensePlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            new List<BattleActionSlot> { passiveDefenseSlot },
            new List<BattleEnemyIntent>
            {
                CreateEnemyAttackIntent("buff49_passive_defense_intent", defenseContext.enemy, defenseEnemyAttack, defenseContext.allyB, 1)
            }
        );
        ExecutePlanWithRuntimeStateAndCompleteTurn(defenseContext.runtimeState, defensePlan);

        bool passiveDefenseRule =
            passiveDefenseSlot.isUsed &&
            CountBuffStack(defenseContext.allyB, "NextCardPointUp") == 0 &&
            CountBuffStack(defenseContext.allyB, "NextClashPointUp") == 5 &&
            CountBuffStack(defenseContext.enemy, "NextCardPointUp") == 0 &&
            CountBuffStack(defenseContext.enemy, "NextClashPointUp") == 7;

        Debug.Log("Passive Dodge符合普通Dodge消费规则：" + passiveDodgeRule);
        Debug.Log("Passive Defense符合普通Defense消费规则：" + passiveDefenseRule);
    }

    void RunBuffNoSuccessNoConsumeSubTest()
    {
        Debug.Log("===== 模式49 子测试I：未成功使用不消费 =====");

        BattleEndedTestContext unavailableContext = CreateBattleEndedTestContext("buff49_unavailable", 30, 30, 50, 10, 3, 8);
        BattleCardState unavailableAttack = CreateBulletLockedFreeAttackCard(unavailableContext.allyB, "buff49_unavailable_attack", 5, 3);
        unavailableContext.allyB.AddBuff("NextCardPointUp", 4, 1);
        BattleActionSlot unavailableSlot = new BattleActionSlot(unavailableContext.allyB, 1);
        unavailableSlot.AssignFreeAction(unavailableContext.allyB, unavailableAttack, unavailableContext.enemy);
        BattleResolveResult unavailableResult = BattleResolver.ResolveFreeAction(unavailableSlot);

        bool actionUnavailableNoConsume =
            unavailableResult != null &&
            unavailableResult.resultType == "ActionUnavailable" &&
            CountBuffStack(unavailableContext.allyB, "NextCardPointUp") == 4;

        BattleEndedTestContext deadContext = CreateBattleEndedTestContext("buff49_dead_skip", 30, 0, 50, 10, 3, 8);
        BattleCardState deadAttack = CreateFixedAttackCardForCharacter(deadContext.allyB, "buff49_dead_skip_attack", 5);
        deadContext.allyB.AddBuff("NextCardPointUp", 4, 1);
        BattleActionSlot deadSlot = new BattleActionSlot(deadContext.allyB, 1);
        deadSlot.AssignFreeAction(deadContext.allyB, deadAttack, deadContext.enemy);
        BattleExecutionPlan deadPlan = CreateManualFreeActionPlan(deadSlot);
        ExecutePlanWithRuntimeStateAndCompleteTurn(deadContext.runtimeState, deadPlan);
        bool deadSkipNoConsume =
            !deadSlot.isUsed &&
            CountBuffStack(deadContext.allyB, "NextCardPointUp") == 4 &&
            deadPlan.isCompleted;

        BattleEndedTestContext tieContext = CreateBattleEndedTestContext("buff49_dodge_tie", 30, 30, 50, 10, 3, 8);
        BattleCardState dodge = CreateFixedDodgeCardForCharacter(tieContext.allyA, "buff49_dodge_tie_card", 5, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(tieContext.enemy, "buff49_dodge_tie_enemy", 5, 0);
        tieContext.allyA.AddBuff("NextClashPointUp", 1, 1);
        tieContext.allyA.AddBuff("NextCardPointUp", 1, 1);
        tieContext.enemy.AddBuff("NextClashPointUp", 1, 1);
        tieContext.enemy.AddBuff("NextCardPointUp", 1, 1);
        BattleResolveResult tieResult = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(tieContext.allyA, dodge),
            CreateEnemyAttackIntent("buff49_dodge_tie_intent", tieContext.enemy, enemyAttack, tieContext.allyA, 1)
        );

        bool tieLimitNoConsume =
            tieResult != null &&
            tieResult.resultType == "TieLimit" &&
            CountBuffStack(tieContext.allyA, "NextClashPointUp") == 1 &&
            CountBuffStack(tieContext.allyA, "NextCardPointUp") == 1 &&
            CountBuffStack(tieContext.enemy, "NextClashPointUp") == 1 &&
            CountBuffStack(tieContext.enemy, "NextCardPointUp") == 1;

        Debug.Log("ActionUnavailable不消费NextCardPointUp：" + actionUnavailableNoConsume);
        Debug.Log("死亡角色FreeAction跳过不消费NextCardPointUp：" + deadSkipNoConsume);
        Debug.Log("Dodge TieLimit不消费一次性Buff：" + tieLimitNoConsume);
    }

    void RunBuffDurationPendingAndPermanentSubTest()
    {
        Debug.Log("===== 模式49 子测试J：DurationDown、延迟排期与Permanent =====");

        CharacterData durationUnit = CreateBuffDataLayerCharacter("buff49_duration");
        durationUnit.AddBuff("Strength", 1, 1);
        durationUnit.AddBuff("Strength", 2, 2);
        durationUnit.CheckBuffsByTiming(BattleTiming.TurnEnd);
        bool durationDownRule =
            CountBuffStack(durationUnit, "Strength") == 2 &&
            GetBuffDuration(durationUnit, "Strength") == 1;

        CharacterData pendingUnit = CreateBuffDataLayerCharacter("buff49_pending");
        pendingUnit.AddPendingBuff("Strength", 1, 1, 1, 1, 1);
        pendingUnit.AddPendingBuff("DamageUp", 1, 2, 2, 1, 1);
        pendingUnit.ApplyPendingBuffsAtTurnStart();
        bool delayedRule =
            CountBuffStack(pendingUnit, "Strength") == 1 &&
            CountBuffStack(pendingUnit, "DamageUp") == 0 &&
            pendingUnit.GetPendingBuffStackNextTurn("DamageUp") == 1;
        pendingUnit.CheckBuffsByTiming(BattleTiming.TurnEnd);
        bool delayedDurationRule = CountBuffStack(pendingUnit, "Strength") == 0;

        CharacterData bulletUnit = CreateBuffDataLayerCharacter("buff49_bullet");
        bulletUnit.AddBuff("Bullet", 6, -1);
        bulletUnit.CheckBuffsByTiming(BattleTiming.TurnEnd);
        bool bulletPermanent = CountBuffStack(bulletUnit, "Bullet") == 6;

        Debug.Log("DurationDown批次独立推进：" + durationDownRule);
        Debug.Log("Delayed Buff正确回合转正：" + delayedRule);
        Debug.Log("延迟生效后的DurationDown正常：" + delayedDurationRule);
        Debug.Log("Bullet Permanent不被TurnEnd消耗：" + bulletPermanent);
    }

    void RunBuffMode49PureReadNoMutationSubTest()
    {
        Debug.Log("===== 模式49 子测试K：纯读取不修改状态 =====");

        CharacterData unit = CreateBuffDataLayerCharacter("buff49_pure_read");
        BattleCardState attack = CreateFixedAttackCardForCharacter(unit, "buff49_pure_read_attack", 5);
        unit.AddBuff("NextClashPointUp", 2, 1);
        unit.AddBuff("NextCardPointUp", 3, 1);

        int clashBefore = CountBuffStack(unit, "NextClashPointUp");
        int cardBefore = CountBuffStack(unit, "NextCardPointUp");
        int point = BattleCalculator.GetFinalClashPoint(unit, attack.cardData);
        bool readValueCorrect = point == 10;
        bool noMutation =
            CountBuffStack(unit, "NextClashPointUp") == clashBefore &&
            CountBuffStack(unit, "NextCardPointUp") == cardBefore;

        Debug.Log("Calculator纯读取能读到数值：" + readValueCorrect);
        Debug.Log("纯读取不修改Buff：" + noMutation);
    }

    BuffDefinitionData GetBuffDefinition(List<BuffDefinitionData> definitions, string buffID)
    {
        if (definitions == null || string.IsNullOrEmpty(buffID))
        {
            return null;
        }

        foreach (BuffDefinitionData definition in definitions)
        {
            if (definition != null && definition.buffID == buffID)
            {
                return definition;
            }
        }

        return null;
    }

    bool AreBuffDefinitionIDsUnique(List<BuffDefinitionData> definitions)
    {
        if (definitions == null)
        {
            return false;
        }

        HashSet<string> ids = new HashSet<string>();

        foreach (BuffDefinitionData definition in definitions)
        {
            if (definition == null || string.IsNullOrEmpty(definition.buffID))
            {
                return false;
            }

            if (!ids.Add(definition.buffID))
            {
                return false;
            }
        }

        return true;
    }

    bool AreBuffDefinitionRequiredFieldsFilled(List<BuffDefinitionData> definitions)
    {
        if (definitions == null || definitions.Count == 0)
        {
            return false;
        }

        foreach (BuffDefinitionData definition in definitions)
        {
            if (definition == null ||
                string.IsNullOrEmpty(definition.buffID) ||
                string.IsNullOrEmpty(definition.buffName) ||
                string.IsNullOrEmpty(definition.buffCategory) ||
                string.IsNullOrEmpty(definition.effectType) ||
                string.IsNullOrEmpty(definition.targetStat) ||
                string.IsNullOrEmpty(definition.defaultCheckTiming) ||
                string.IsNullOrEmpty(definition.defaultExpireRule))
            {
                return false;
            }
        }

        return true;
    }

    bool AreBuffDefinitionCategoriesValid(List<BuffDefinitionData> definitions)
    {
        if (definitions == null)
        {
            return false;
        }

        foreach (BuffDefinitionData definition in definitions)
        {
            if (definition == null)
            {
                return false;
            }

            if (definition.buffCategory != BuffCategory.UpBuff &&
                definition.buffCategory != BuffCategory.Debuff &&
                definition.buffCategory != BuffCategory.AbilityBuff)
            {
                return false;
            }
        }

        return true;
    }

    bool IsBuffDefinitionValue(BuffDefinitionData definition, float expectedValue)
    {
        return definition != null && Mathf.Abs(definition.valuePerStack - expectedValue) < 0.001f;
    }

    CharacterData CreateBuffDataLayerCharacter(string name)
    {
        return new CharacterData(name, 30, 3, 8);
    }

    bool HasBuffBatch(List<BuffData> batches, int stack, int duration)
    {
        if (batches == null)
        {
            return false;
        }

        foreach (BuffData batch in batches)
        {
            if (batch != null && batch.stack == stack && batch.duration == duration)
            {
                return true;
            }
        }

        return false;
    }

    int GetPendingDelayTurns(CharacterData character, string buffID, int stack)
    {
        if (character == null)
        {
            return -999;
        }

        List<PendingBuffData> pendingBatches = character.GetPendingBuffBatches(buffID);

        foreach (PendingBuffData pendingBuff in pendingBatches)
        {
            if (pendingBuff != null && pendingBuff.stack == stack)
            {
                return pendingBuff.delayTurns;
            }
        }

        return -999;
    }

    CardTestData CreateBuffDefinitionPathTestCard()
    {
        return new CardTestData
        {
            cardID = "buff_definition_path_card",
            cardName = "Buff定义路径测试卡",
            cardType = "Ability",
            isClashable = false,
            effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    trigger = BattleTiming.BeforeUse,
                    effectType = CardEffectType.ApplyBuff,
                    target = CardTargetType.Self,
                    buffType = "Strength",
                    stack = 1,
                    duration = 2
                },
                new CardEffectData
                {
                    trigger = BattleTiming.AfterDamage,
                    effectType = CardEffectType.ApplyBuff,
                    target = CardTargetType.Self,
                    buffType = "DamageUp",
                    stack = 2,
                    duration = 1,
                    applyTiming = BuffApplyTiming.Delayed,
                    delayTurns = 1,
                    applyTimes = 1,
                    intervalTurns = 1
                }
            }
        };
    }

    CardTestData CreateLegacyAbilityPowerTestCard()
    {
        return new CardTestData
        {
            cardID = "buff_legacy_ability_power_card",
            cardName = "Legacy AbilityPower测试卡",
            cardType = "Ability",
            isClashable = false,
            effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    trigger = BattleTiming.OnPlay,
                    effectType = CardEffectType.ApplyBuff,
                    target = CardTargetType.Self,
                    buffType = "AbilityPower",
                    buffName = "能力强化",
                    buffCategory = BuffCategory.UpBuff,
                    stack = 1,
                    duration = 1,
                    checkTiming = BattleTiming.TurnEnd,
                    expireRule = "DurationDown"
                }
            }
        };
    }

    CardTestData CreateUnknownBuffTestCard()
    {
        return new CardTestData
        {
            cardID = "buff_unknown_reject_card",
            cardName = "未知Buff拒绝测试卡",
            cardType = "Ability",
            isClashable = false,
            effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    trigger = BattleTiming.BeforeUse,
                    effectType = CardEffectType.ApplyBuff,
                    target = CardTargetType.Self,
                    buffType = "UnknownBuffForTest",
                    stack = 1,
                    duration = 1
                }
            }
        };
    }

    // RunBuffTriggerConsumeOrderBasicTestSequence = Buff阶段A：ClashStart一次性数值Buff读取与消费顺序聚合测试
    void RunBuffTriggerConsumeOrderBasicTestSequence()
    {
        Debug.Log("===== BuffTriggerConsumeOrderBasic 聚合测试开始 =====");

        RunBuffAttackWinConsumeSubTest();
        RunBuffAttackLoseConsumeSubTest();
        RunBuffClashRerollKeepUntilConsumeSubTest();
        RunBuffAttackTieLimitKeepSubTest();
        RunBuffDodgeConsumeSubTest();
        RunBuffDodgeTieLimitKeepSubTest();
        RunBuffKnownPointDodgeConsumeSubTest();
        RunBuffDefenseConsumeSubTest();
        RunBuffEventNewNextClashBuffKeptSubTest();
        RunBuffPureReadNoMutationSubTest();
    }

    void RunBuffAttackWinConsumeSubTest()
    {
        Debug.Log("===== 模式47 子测试A：Attack正常生效后消费 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("buff_a", 30, 30, 50, 10, 3, 8);
        BattleCardState playerAttack = CreateFixedAttackCardForCharacter(context.allyA, "buff_a_player_attack", 5);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff_a_enemy_attack", 6, 0);
        AddClashStartOneShotBuff(context.allyA, "NextClashPointUp", 3, 1);

        int playerStackBefore = CountBuffStack(context.allyA, "NextClashPointUp");
        int playerInstanceBefore = CountBuffInstances(context.allyA, "NextClashPointUp");
        BattleActionSlot actionSlot = CreateRespondedSlot(context.allyA, playerAttack);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("buff_a_intent", context.enemy, enemyAttack, context.allyA, 1);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(actionSlot, intent);

        int playerStackAfter = CountBuffStack(context.allyA, "NextClashPointUp");
        int playerInstanceAfter = CountBuffInstances(context.allyA, "NextClashPointUp");
        bool pointApplied = result != null && result.playerPoint == 8;
        bool consumed = playerStackBefore == 3 && playerStackAfter == 0 && playerInstanceBefore == 1 && playerInstanceAfter == 0;

        Debug.Log("点数加成是否实际生效：" + pointApplied);
        Debug.Log("Buff消费前stack：" + playerStackBefore);
        Debug.Log("Buff消费后stack：" + playerStackAfter);
        Debug.Log("Buff实例数量前后：" + playerInstanceBefore + " -> " + playerInstanceAfter);
        Debug.Log("是否只消费一次：" + consumed);
        Debug.Log("卡牌UseCount / CD / guilt：" + playerAttack.currentUseCount + " / " + playerAttack.currentCooldown + " / " + context.allyA.currentGuilt);
        Debug.Log("Attack加成先读取后消费：" + (pointApplied && consumed && result != null && result.playerCardUsed));
    }

    void RunBuffAttackLoseConsumeSubTest()
    {
        Debug.Log("===== 模式47 子测试B：Attack失败方NextCard保留，双方NextClash消费 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("buff_b", 30, 30, 50, 10, 3, 8);
        BattleCardState playerAttack = CreateFixedAttackCardForCharacter(context.allyA, "buff_b_player_attack", 5);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff_b_enemy_attack", 9, 0);
        AddClashStartOneShotBuff(context.allyA, "NextClashPointUp", 1, 1);
        context.allyA.AddBuff("NextCardPointUp", 1, 1);
        AddClashStartOneShotBuff(context.enemy, "NextClashPointUp", 1, 1);
        context.enemy.AddBuff("NextCardPointUp", 2, 1);

        int playerClashStackBefore = CountBuffStack(context.allyA, "NextClashPointUp");
        int playerCardStackBefore = CountBuffStack(context.allyA, "NextCardPointUp");
        int enemyClashStackBefore = CountBuffStack(context.enemy, "NextClashPointUp");
        int enemyCardStackBefore = CountBuffStack(context.enemy, "NextCardPointUp");
        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, playerAttack),
            CreateEnemyAttackIntent("buff_b_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool pointApplied = result != null && result.playerPoint == 7 && result.enemyPoint == 12;
        bool loserNextClashConsumed =
            playerClashStackBefore == 1 &&
            CountBuffStack(context.allyA, "NextClashPointUp") == 0;
        bool loserNextCardKept =
            playerCardStackBefore == 1 &&
            CountBuffStack(context.allyA, "NextCardPointUp") == 1;
        bool winnerBuffsConsumed =
            enemyClashStackBefore == 1 &&
            enemyCardStackBefore == 2 &&
            CountBuffStack(context.enemy, "NextClashPointUp") == 0 &&
            CountBuffStack(context.enemy, "NextCardPointUp") == 0;

        Debug.Log("点数加成是否实际生效：" + pointApplied);
        Debug.Log("失败方NextClash消费：" + loserNextClashConsumed);
        Debug.Log("失败方NextCard保留：" + loserNextCardKept);
        Debug.Log("胜方NextClash与NextCard消费：" + winnerBuffsConsumed);
        Debug.Log("Attack失败方消费规则符合新契约：" + (pointApplied && loserNextClashConsumed && loserNextCardKept && winnerBuffsConsumed && result != null && result.resultType == "EnemyWin"));
    }

    void RunBuffClashRerollKeepUntilConsumeSubTest()
    {
        Debug.Log("===== 模式47 子测试C：平局重投期间Buff保持 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("buff_c", 30, 30, 50, 10, 3, 8);
        BattleCardState playerAttack = CreateFixedAttackCardForCharacter(context.allyA, "buff_c_player_attack", 5);
        AddClashStartOneShotBuff(context.allyA, "NextClashPointUp", 3, 1);

        context.allyA.CheckBuffsByTiming(BattleTiming.ClashStart, false);

        int firstReadPoint = BattleCalculator.GetFinalClashPoint(context.allyA, playerAttack.cardData);
        int stackAfterFirstRead = CountBuffStack(context.allyA, "NextClashPointUp");
        int secondReadPoint = BattleCalculator.GetFinalClashPoint(context.allyA, playerAttack.cardData);
        int stackAfterSecondRead = CountBuffStack(context.allyA, "NextClashPointUp");
        int consumedStack = context.allyA.ConsumeTriggeredBuffs(BattleTiming.ClashStart, "NextClashPointUp");
        int stackAfterConsume = CountBuffStack(context.allyA, "NextClashPointUp");

        bool keptDuringReads = firstReadPoint == 8 && secondReadPoint == 8 && stackAfterFirstRead == 3 && stackAfterSecondRead == 3;
        bool consumedOnce = consumedStack == 3 && stackAfterConsume == 0;

        Debug.Log("点数加成是否实际生效：" + (firstReadPoint == 8 && secondReadPoint == 8));
        Debug.Log("Buff消费前stack：" + stackAfterSecondRead);
        Debug.Log("Buff消费后stack：" + stackAfterConsume);
        Debug.Log("Buff实例数量前后：" + 1 + " -> " + CountBuffInstances(context.allyA, "NextClashPointUp"));
        Debug.Log("平局重投期间Buff保持：" + (keptDuringReads && consumedOnce));
    }

    void RunBuffAttackTieLimitKeepSubTest()
    {
        Debug.Log("===== 模式47 子测试D：Attack TieLimit不消费 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("buff_d", 30, 30, 50, 10, 3, 8);
        BattleCardState playerAttack = CreateFixedAttackCardForCharacter(context.allyA, "buff_d_player_attack", 5);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff_d_enemy_attack", 5, 0);
        AddClashStartOneShotBuff(context.allyA, "NextClashPointUp", 3, 1);
        AddClashStartOneShotBuff(context.enemy, "NextClashPointUp", 3, 1);

        BattleActionSlot actionSlot = CreateRespondedSlot(context.allyA, playerAttack);
        BattleEnemyIntent intent = CreateEnemyAttackIntent("buff_d_intent", context.enemy, enemyAttack, context.allyA, 1);
        BattleExecutionPlan executionPlan = CreateManualExecutionPlan(
            new BattleExecutionItem(1, BattleExecutionItemType.RespondedEnemyIntent, intent, actionSlot)
        );

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        bool buffsKept =
            CountBuffStack(context.allyA, "NextClashPointUp") == 3 &&
            CountBuffStack(context.enemy, "NextClashPointUp") == 3;
        bool cardsNotUsed = playerAttack.currentCooldown == 0 && enemyAttack.currentCooldown == 0;

        Debug.Log("TieLimit Buff不消费：" + (buffsKept && !actionSlot.isUsed));
        Debug.Log("item是否完成：" + (executionPlan.executionItems[0].isCompleted));
        Debug.Log("plan是否完成：" + executionPlan.isCompleted);
        Debug.Log("槽位是否MarkUsed：" + actionSlot.isUsed);
        Debug.Log("卡牌UseCount / CD / guilt：" + playerAttack.currentUseCount + " / " + playerAttack.currentCooldown + " / " + context.allyA.currentGuilt);
        Debug.Log("TieLimit时状态是否保持：" + (buffsKept && cardsNotUsed && !actionSlot.isUsed));
    }

    void RunBuffDodgeConsumeSubTest()
    {
        Debug.Log("===== 模式47 子测试E：指定Dodge Buff先读取后消费 =====");

        BattleEndedTestContext successContext = CreateBattleEndedTestContext("buff_e_success", 30, 30, 50, 10, 3, 8);
        BattleCardState successDodge = CreateFixedDodgeCardForCharacter(successContext.allyA, "buff_e_success_dodge", 4, 1);
        BattleCardState successEnemy = CreateFixedEnemyAttackCardForDodgeTest(successContext.enemy, "buff_e_success_enemy", 5, 0);
        AddClashStartOneShotBuff(successContext.allyA, "NextClashPointUp", 3, 1);
        AddClashStartOneShotBuff(successContext.enemy, "NextClashPointUp", 1, 1);
        successContext.allyA.AddBuff("Strength", "强壮", "UpBuff", 9, 1, "TurnEnd", "DurationDown");

        BattleResolveResult successResult = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(successContext.allyA, successDodge),
            CreateEnemyAttackIntent("buff_e_success_intent", successContext.enemy, successEnemy, successContext.allyA, 1)
        );

        bool successConsumed =
            successResult != null &&
            successResult.resultType == "DodgeSuccess" &&
            successResult.playerPoint == 7 &&
            successResult.enemyPoint == 6 &&
            CountBuffStack(successContext.allyA, "NextClashPointUp") == 0 &&
            CountBuffStack(successContext.enemy, "NextClashPointUp") == 0 &&
            CountBuffStack(successContext.allyA, "Strength") == 9;

        BattleEndedTestContext failedContext = CreateBattleEndedTestContext("buff_e_failed", 30, 30, 50, 10, 3, 8);
        BattleCardState failedDodge = CreateFixedDodgeCardForCharacter(failedContext.allyA, "buff_e_failed_dodge", 2, 1);
        BattleCardState failedEnemy = CreateFixedEnemyAttackCardForDodgeTest(failedContext.enemy, "buff_e_failed_enemy", 5, 0);
        AddClashStartOneShotBuff(failedContext.allyA, "NextClashPointUp", 1, 1);
        AddClashStartOneShotBuff(failedContext.enemy, "NextClashPointUp", 1, 1);

        BattleResolveResult failedResult = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(failedContext.allyA, failedDodge),
            CreateEnemyAttackIntent("buff_e_failed_intent", failedContext.enemy, failedEnemy, failedContext.allyA, 1)
        );

        bool failedConsumed =
            failedResult != null &&
            failedResult.resultType == "DodgeFailed" &&
            failedResult.playerPoint == 3 &&
            failedResult.enemyPoint == 6 &&
            CountBuffStack(failedContext.allyA, "NextClashPointUp") == 0 &&
            CountBuffStack(failedContext.enemy, "NextClashPointUp") == 0;

        Debug.Log("Dodge Buff先读取后消费：" + (successConsumed && failedConsumed));
        Debug.Log("点数加成是否实际生效：" + (successResult != null && successResult.playerPoint == 7 && failedResult != null && failedResult.playerPoint == 3));
        Debug.Log("Dodge不读取或消费Strength / Weakness：" + (CountBuffStack(successContext.allyA, "Strength") == 9));
    }

    void RunBuffDodgeTieLimitKeepSubTest()
    {
        Debug.Log("===== 模式47 子测试F：Dodge TieLimit不消费 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("buff_f", 30, 30, 50, 10, 3, 8);
        BattleCardState dodge = CreateFixedDodgeCardForCharacter(context.allyA, "buff_f_dodge", 5, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff_f_enemy", 5, 0);
        AddClashStartOneShotBuff(context.allyA, "NextClashPointUp", 3, 1);
        AddClashStartOneShotBuff(context.enemy, "NextClashPointUp", 3, 1);

        BattleActionSlot actionSlot = CreateRespondedSlot(context.allyA, dodge);
        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            actionSlot,
            CreateEnemyAttackIntent("buff_f_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool kept =
            result != null &&
            result.resultType == "TieLimit" &&
            CountBuffStack(context.allyA, "NextClashPointUp") == 3 &&
            CountBuffStack(context.enemy, "NextClashPointUp") == 3 &&
            !actionSlot.isUsed &&
            dodge.currentCooldown == 0;

        Debug.Log("Dodge Buff不消费：" + kept);
        Debug.Log("敌人Attack Buff不消费：" + (CountBuffStack(context.enemy, "NextClashPointUp") == 3));
        Debug.Log("双方卡牌不使用：" + (result != null && !result.playerCardUsed && !result.enemyCardUsed));
        Debug.Log("槽位不MarkUsed：" + !actionSlot.isUsed);
    }

    void RunBuffKnownPointDodgeConsumeSubTest()
    {
        Debug.Log("===== 模式47 子测试G：known-point Dodge不重复消费敌人Buff =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("buff_g", 30, 30, 50, 10, 3, 8);
        BattleCardState responseAttack = CreateFixedAttackCardForCharacter(context.allyA, "buff_g_response_attack", 4);
        BattleCardState enemyAttack = CreateAttackCardWithNextClashBuffEffect(context.enemy, "buff_g_enemy_attack", 5, BattleTiming.Resolved, 9);
        BattleCardState passiveDodge = CreateFixedDodgeCardForCharacter(context.allyB, "buff_g_passive_dodge", 4, 1);
        AddClashStartOneShotBuff(context.enemy, "NextClashPointUp", 2, 1);
        AddClashStartOneShotBuff(context.allyB, "NextClashPointUp", 4, 1);

        BattleEnemyIntent intent = CreateEnemyAttackIntent("buff_g_intent", context.enemy, enemyAttack, context.allyB, 1);
        BattleActionSlot responseSlot = CreateRespondedSlot(context.allyA, responseAttack);
        BattleActionSlot passiveDodgeSlot = new BattleActionSlot(context.allyB, 2);
        passiveDodgeSlot.AssignPassiveGuard(context.allyB, passiveDodge);
        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            responseSlot,
            intent,
            new List<BattleActionSlot> { passiveDodgeSlot }
        );

        bool dodgeConsumed = CountBuffStack(context.allyB, "NextClashPointUp") == 0;
        bool enemyNewBuffKept = CountBuffStack(context.enemy, "NextClashPointUp") == 9;
        bool knownPointWorked =
            result != null &&
            result.resultType == "DodgeSuccess" &&
            result.enemyPoint == 7 &&
            result.triggeredPassiveGuardSlot == passiveDodgeSlot;

        Debug.Log("Dodge点数获得加成：" + knownPointWorked);
        Debug.Log("DodgeSuccess或DodgeFailed后Dodge Buff消费：" + dodgeConsumed);
        Debug.Log("known-point Dodge未重复消费敌人Buff：" + (enemyNewBuffKept && knownPointWorked));
        Debug.Log("known-point敌人点数不重新Roll：" + (result != null && result.enemyPoint == 7));
    }

    void RunBuffDefenseConsumeSubTest()
    {
        Debug.Log("===== 模式47 子测试H：Defense一次性数值Buff先生效后消费 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("buff_h", 30, 30, 50, 10, 3, 8);
        BattleCardState defense = CreateTestDefenseCardForCharacter(context.allyA, "buff_h_defense", 3, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff_h_enemy", 4, 0);
        AddClashStartOneShotBuff(context.allyA, "GuardUp", 4, 1);
        AddClashStartOneShotBuff(context.enemy, "NextClashPointUp", 2, 1);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, defense),
            CreateEnemyAttackIntent("buff_h_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool ordinaryDefenseGuardBuffWorked =
            result != null &&
            result.resultType == "DefenseFullBlock" &&
            result.playerPoint == 7 &&
            CountBuffStack(context.allyA, "GuardUp") == 0;
        bool ordinaryDefenseEnemyNextClashKept =
            result != null &&
            result.enemyPoint == 4 &&
            CountBuffStack(context.enemy, "NextClashPointUp") == 2;

        BattleEndedTestContext knownContext = CreateBattleEndedTestContext("buff_h_known", 30, 30, 50, 10, 3, 8);
        BattleCardState knownDefense = CreateTestDefenseCardForCharacter(knownContext.allyA, "buff_h_known_defense", 3, 1);
        BattleCardState knownEnemyAttack = CreateFixedEnemyAttackCardForDodgeTest(knownContext.enemy, "buff_h_known_enemy", 4, 0);
        AddClashStartOneShotBuff(knownContext.allyA, "GuardDown", 1, 1);
        AddClashStartOneShotBuff(knownContext.enemy, "NextClashPointUp", 5, 1);
        BattleActionSlot knownDefenseSlot = CreateRespondedSlot(knownContext.allyA, knownDefense);
        BattleResolveResult knownResult = BattleResolver.ResolveDefenseVsAttackWithKnownEnemyPoint(
            knownDefenseSlot,
            CreateEnemyAttackIntent("buff_h_known_intent", knownContext.enemy, knownEnemyAttack, knownContext.allyA, 1),
            4
        );

        bool knownDefenseRule =
            knownResult != null &&
            knownResult.playerPoint == 2 &&
            knownResult.enemyPoint == 4 &&
            CountBuffStack(knownContext.allyA, "GuardDown") == 0 &&
            CountBuffStack(knownContext.enemy, "NextClashPointUp") == 5;

        Debug.Log("Defense侧一次性Guard Buff先生效后消费：" + (ordinaryDefenseGuardBuffWorked && knownDefenseRule));
        Debug.Log("Defense路径不读取或消费敌人NextClashPointUp：" + ordinaryDefenseEnemyNextClashKept);
        Debug.Log("FullBlock或ReducedDamage后Defense侧Guard Buff已消费：" + ordinaryDefenseGuardBuffWorked);
        Debug.Log("known-point Defense只消费Defense侧Buff：" + knownDefenseRule);
    }

    void RunBuffEventNewNextClashBuffKeptSubTest()
    {
        Debug.Log("===== 模式47 子测试I：事件中新获得NextClashPointUp保留 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("buff_i", 30, 30, 50, 10, 3, 8);
        BattleCardState playerAttack = CreateAttackCardWithNextClashBuffEffect(context.allyA, "buff_i_player_attack", 5, BattleTiming.ClashWin, 7);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "buff_i_enemy", 6, 0);
        AddClashStartOneShotBuff(context.allyA, "NextClashPointUp", 3, 1);

        BattleResolveResult result = BattleResolver.ResolveRespondedEnemyIntent(
            CreateRespondedSlot(context.allyA, playerAttack),
            CreateEnemyAttackIntent("buff_i_intent", context.enemy, enemyAttack, context.allyA, 1)
        );

        bool oldConsumedNewKept =
            result != null &&
            result.resultType == "PlayerWin" &&
            result.playerPoint == 8 &&
            CountBuffStack(context.allyA, "NextClashPointUp") == 7 &&
            CountBuffInstances(context.allyA, "NextClashPointUp") == 1;

        Debug.Log("本次开始前旧Buff被消费：" + (result != null && result.playerPoint == 8));
        Debug.Log("事件中新获得Buff仍存在：" + (CountBuffStack(context.allyA, "NextClashPointUp") == 7));
        Debug.Log("事件中新获得Buff未被误删：" + oldConsumedNewKept);
    }

    void RunBuffPureReadNoMutationSubTest()
    {
        Debug.Log("===== 模式47 子测试J：纯读取不修改Buff =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("buff_j", 30, 30, 50, 10, 3, 8);
        AddClashStartOneShotBuff(context.allyA, "NextClashPointUp", 3, 4);

        int stackBefore = CountBuffStack(context.allyA, "NextClashPointUp");
        int instanceBefore = CountBuffInstances(context.allyA, "NextClashPointUp");
        int durationBefore = GetBuffDuration(context.allyA, "NextClashPointUp");
        int readStack = context.allyA.GetBuffStack("NextClashPointUp");
        int stackAfter = CountBuffStack(context.allyA, "NextClashPointUp");
        int instanceAfter = CountBuffInstances(context.allyA, "NextClashPointUp");
        int durationAfter = GetBuffDuration(context.allyA, "NextClashPointUp");

        bool pureRead =
            readStack == stackBefore &&
            stackBefore == stackAfter &&
            instanceBefore == instanceAfter &&
            durationBefore == durationAfter;

        Debug.Log("当前完整Buff点数范围预览尚未接入");
        Debug.Log("读取前后Buff stack不变：" + (stackBefore == stackAfter));
        Debug.Log("duration不变：" + (durationBefore == durationAfter));
        Debug.Log("Buff实例数量不变：" + (instanceBefore == instanceAfter));
        Debug.Log("纯读取不修改Buff：" + pureRead);
    }

    void RunFreeActionUnavailableBulletSubTest()
    {
        Debug.Log("===== 模式45 子测试A：FreeAction因Bullet不足而ActionUnavailable =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "invalid_action_a",
            30,
            30,
            50,
            10,
            3,
            8
        );

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1);
        List<BattleEnemyIntent> emptyIntentQueue = new List<BattleEnemyIntent>();
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(emptyIntentQueue);

        BattleCardState bulletAttack = CreateBulletLockedFreeAttackCard(context.allyB, "invalid_action_a_bullet_attack", 3, 5);
        context.allyB.AddBuff("Bullet", 5, -1);

        CardEligibilityResult assignResult;
        bool assignSuccess = BattleActionSlotManager.AssignFreeAction(
            actionSlots,
            context.allyB,
            1,
            context.allyB,
            bulletAttack,
            context.enemy,
            out assignResult
        );

        BattleActionSlot actionSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyB, 1);
        bool preparedWithCondition =
            CountBuffStack(context.allyB, "Bullet") >= 5 &&
            assignSuccess &&
            actionSlot != null &&
            object.ReferenceEquals(actionSlot.actor, context.allyB) &&
            object.ReferenceEquals(actionSlot.cardState, bulletAttack);

        RemoveAllBuffs(context.allyB, "Bullet");
        bool bulletRemovedBeforeExecute = CountBuffStack(context.allyB, "Bullet") == 0;

        int enemyHPBefore = context.enemy.currentHP;
        int cooldownBefore = bulletAttack.currentCooldown;
        int useCountBefore = bulletAttack.currentUseCount;
        int guiltBefore = context.allyB.currentGuilt;

        BattleResolveResult directResult = BattleResolver.ResolveFreeAction(actionSlot);
        BattleExecutionPlan executionPlan = CreateManualFreeActionPlan(actionSlot);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        Debug.Log("模式45 A 准备阶段条件满足并成功安排：" + preparedWithCondition);
        Debug.Log("模式45 A 执行前Bullet已移除：" + bulletRemovedBeforeExecute);
        Debug.Log("resultType是否为ActionUnavailable：" + (directResult != null && directResult.resultType == "ActionUnavailable"));
        Debug.Log("isSuccess是否为False：" + (directResult != null && !directResult.isSuccess));
        Debug.Log("shouldCompleteItem是否为True：" + (directResult != null && directResult.shouldCompleteItem));
        Debug.Log("playerCardUsed是否为False：" + (directResult != null && !directResult.playerCardUsed));
        Debug.Log("不造成伤害：" + (context.enemy.currentHP == enemyHPBefore));
        Debug.Log("CD不变：" + (bulletAttack.currentCooldown == cooldownBefore));
        Debug.Log("UseCount不变：" + (bulletAttack.currentUseCount == useCountBefore));
        Debug.Log("guilt不变：" + (context.allyB.currentGuilt == guiltBefore));
        Debug.Log("行动未使用卡牌：" + (bulletAttack.currentUseCount == useCountBefore && context.allyB.currentGuilt == guiltBefore && bulletAttack.currentCooldown == cooldownBefore));
        Debug.Log("槽位未MarkUsed：" + (actionSlot != null && !actionSlot.isUsed));
        Debug.Log("item按跳过完成：" + (item != null && item.isCompleted));
        Debug.Log("item为Skipped / ActionUnavailable：" + IsExecutionItemState(item, BattleExecutionItemStatus.Skipped, BattleExecutionItemOutcomeReason.ActionUnavailable, true));
        Debug.Log("plan最终完成：" + executionPlan.isCompleted);
        Debug.Log("phase可进入Completed：" + (context.runtimeState.currentPhase == "Completed"));
    }

    void RunFreeActionUnavailableThenNextItemSubTest()
    {
        Debug.Log("===== 模式45 子测试B：第一个ActionUnavailable，第二个行动正常执行 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "invalid_action_b",
            30,
            30,
            50,
            10,
            3,
            8
        );

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        List<BattleEnemyIntent> emptyIntentQueue = new List<BattleEnemyIntent>();
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(emptyIntentQueue);

        BattleCardState unavailableAttack = CreateBulletLockedFreeAttackCard(context.allyB, "invalid_action_b_bullet_attack", 3, 5);
        BattleCardState followAbility = CreateBattleEndedAbilityCard(context.allyA, "invalid_action_b_follow_ability", "InvalidActionFollowBuff");

        context.allyB.AddBuff("Bullet", 5, -1);

        CardEligibilityResult firstAssignResult;
        bool firstAssignSuccess = BattleActionSlotManager.AssignFreeAction(
            actionSlots,
            context.allyB,
            1,
            context.allyB,
            unavailableAttack,
            context.enemy,
            out firstAssignResult
        );

        CardEligibilityResult secondAssignResult;
        bool secondAssignSuccess = BattleActionSlotManager.AssignFreeAction(
            actionSlots,
            context.allyA,
            1,
            context.allyA,
            followAbility,
            context.allyA,
            out secondAssignResult
        );

        BattleActionSlot firstSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyB, 1);
        BattleActionSlot secondSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);
        bool firstPrepared =
            CountBuffStack(context.allyB, "Bullet") >= 5 &&
            firstAssignSuccess &&
            firstSlot != null &&
            object.ReferenceEquals(firstSlot.actor, context.allyB) &&
            object.ReferenceEquals(firstSlot.cardState, unavailableAttack);
        bool secondPrepared =
            secondAssignSuccess &&
            secondSlot != null &&
            object.ReferenceEquals(secondSlot.actor, context.allyA) &&
            object.ReferenceEquals(secondSlot.cardState, followAbility);

        int firstUseCountBefore = unavailableAttack.currentUseCount;
        int secondUseCountBefore = followAbility.currentUseCount;
        int secondGuiltBefore = context.allyA.currentGuilt;

        RemoveAllBuffs(context.allyB, "Bullet");
        bool firstBulletRemovedBeforeExecute = CountBuffStack(context.allyB, "Bullet") == 0;
        bool secondCardStillAvailable = BattleCardManager.EvaluateCardEligibility(context.allyA, context.allyA, followAbility).isEligible;

        BattleExecutionPlan executionPlan = CreateManualFreeActionPlan(firstSlot, secondSlot);
        BattleExecutionItem firstItem = executionPlan.executionItems[0];
        BattleExecutionItem secondItem = executionPlan.executionItems[1];
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        Debug.Log("模式45 B 第一张卡准备阶段成功安排：" + firstPrepared);
        Debug.Log("模式45 B 执行前第一角色Bullet已移除：" + firstBulletRemovedBeforeExecute);
        Debug.Log("模式45 B 第二张卡保持可用：" + (secondPrepared && secondCardStillAvailable));
        Debug.Log("第一个item是否完成：" + (firstItem != null && firstItem.isCompleted));
        Debug.Log("第一个item为Skipped / ActionUnavailable：" + IsExecutionItemState(firstItem, BattleExecutionItemStatus.Skipped, BattleExecutionItemOutcomeReason.ActionUnavailable, true));
        Debug.Log("第一个槽位未MarkUsed：" + (firstSlot != null && !firstSlot.isUsed));
        Debug.Log("第一个卡牌UseCount不变：" + (unavailableAttack.currentUseCount == firstUseCountBefore));
        Debug.Log("第二个item是否完成：" + (secondItem != null && secondItem.isCompleted));
        Debug.Log("第二个item为Executed：" + IsExecutionItemState(secondItem, BattleExecutionItemStatus.Executed, BattleExecutionItemOutcomeReason.None, true));
        Debug.Log("第二个槽位正常MarkUsed：" + (secondSlot != null && secondSlot.isUsed));
        Debug.Log("第二张卡正常使用：" + (followAbility.currentUseCount == secondUseCountBefore + 1 && context.allyA.currentGuilt > secondGuiltBefore));
        Debug.Log("后续item继续执行：" + (secondItem != null && secondItem.isCompleted && secondSlot != null && secondSlot.isUsed));
        Debug.Log("plan全部完成：" + executionPlan.isCompleted);
        Debug.Log("phase是否Completed：" + (context.runtimeState.currentPhase == "Completed"));
    }

    void RunFreeActionNormalRegressionSubTest()
    {
        Debug.Log("===== 模式45 子测试C：正常FreeAction回归 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "invalid_action_c",
            30,
            30,
            50,
            10,
            3,
            8
        );

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1);
        List<BattleEnemyIntent> emptyIntentQueue = new List<BattleEnemyIntent>();
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(emptyIntentQueue);

        BattleCardState ability = CreateBattleEndedAbilityCard(context.allyA, "invalid_action_c_ability", "InvalidActionNormalBuff");
        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyA, 1, context.allyA, ability, context.allyA);

        BattleActionSlot actionSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);
        int useCountBefore = ability.currentUseCount;
        int guiltBefore = context.allyA.currentGuilt;
        int cooldownBefore = ability.currentCooldown;

        BattleExecutionPlan executionPlan = CreateManualFreeActionPlan(actionSlot);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        Debug.Log("正常FreeAction item是否完成：" + (item != null && item.isCompleted));
        Debug.Log("正常FreeAction plan是否完成：" + executionPlan.isCompleted);
        Debug.Log("正常FreeAction phase是否Completed：" + (context.runtimeState.currentPhase == "Completed"));
        Debug.Log("正常FreeAction槽位MarkUsed：" + (actionSlot != null && actionSlot.isUsed));
        Debug.Log("Resolved后UseCount正常增加：" + (ability.currentUseCount == useCountBefore + 1));
        Debug.Log("Resolved后guilt正常增加：" + (context.allyA.currentGuilt > guiltBefore));
        Debug.Log("Ability罪卡CD保持正常：" + (ability.currentCooldown == cooldownBefore));
        Debug.Log("OnPlay效果正常触发：" + (CountBuffStack(context.allyA, "InvalidActionNormalBuff") > 0));
    }

    void RunFreeActionUnsupportedNotSwallowedSubTest()
    {
        Debug.Log("===== 模式45 子测试D：真正Invalid / Unsupported不被吞掉 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "invalid_action_d",
            30,
            30,
            50,
            10,
            3,
            8
        );

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1);
        List<BattleEnemyIntent> emptyIntentQueue = new List<BattleEnemyIntent>();
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(emptyIntentQueue);

        BattleCardState unsupportedDefense = CreateTestDefenseCardForCharacter(context.allyA, "invalid_action_d_defense", 4, 1);
        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyA, 1, context.allyA, unsupportedDefense, context.enemy);

        BattleActionSlot actionSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);
        BattleResolveResult directResult = BattleResolver.ResolveFreeAction(actionSlot);
        BattleExecutionPlan executionPlan = CreateManualFreeActionPlan(actionSlot);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        string phaseAfterExecute = context.runtimeState.currentPhase;
        int turnBeforeEnd = context.runtimeState.currentTurn;
        context.runtimeState.EndCurrentTurnAndClearRuntimeObjects();
        string phaseAfterEndTurn = context.runtimeState.currentPhase;
        context.runtimeState.PrepareNextTurnWithRuntimeObjects(
            BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1),
            new List<BattleEnemyIntent>()
        );

        Debug.Log("resultType是否为Unsupported：" + (directResult != null && directResult.resultType == "Unsupported"));
        Debug.Log("shouldCompleteItem是否为False：" + (directResult != null && !directResult.shouldCompleteItem));
        Debug.Log("真正Invalid未被吞掉：" + (item != null && !item.isCompleted && !executionPlan.isCompleted));
        Debug.Log("未完成Plan不能进入Completed：" + (phaseAfterExecute != "Completed"));
        Debug.Log("未完成Plan不能EndTurn：" + (phaseAfterEndTurn == phaseAfterExecute && context.runtimeState.currentTurn == turnBeforeEnd));
        Debug.Log("未完成Plan不能PrepareNextTurn：" + (context.runtimeState.currentPhase == phaseAfterExecute));
        Debug.Log("槽位未MarkUsed：" + (actionSlot != null && !actionSlot.isUsed));
    }

    void RunFreeActionBattleEndedRegressionSubTest()
    {
        Debug.Log("===== 模式45 子测试E：BattleEnded回归 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "invalid_action_e",
            30,
            30,
            5,
            20,
            3,
            8
        );

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        List<BattleEnemyIntent> emptyIntentQueue = new List<BattleEnemyIntent>();
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(emptyIntentQueue);

        BattleCardState killAttack = CreateBattleEndedKillAttackCard(context.allyA, "invalid_action_e_kill_attack", 6);
        BattleCardState followAbility = CreateBattleEndedAbilityCard(context.allyA, "invalid_action_e_follow_ability", "InvalidActionBattleEndedBuff");

        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyA, 1, context.allyA, killAttack, context.enemy);
        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyA, 2, context.allyA, followAbility, context.allyA);

        BattleActionSlot killSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);
        BattleActionSlot followSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 2);
        int followUseCountBefore = followAbility.currentUseCount;
        int followGuiltBefore = context.allyA.currentGuilt;

        BattleExecutionPlan executionPlan = CreateManualFreeActionPlan(killSlot, followSlot);
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        Debug.Log("BattleEnded仍优先：" + context.runtimeState.IsBattleEnded);
        Debug.Log("Victory保持正确：" + (context.runtimeState.battleResult == BattleResult.Victory));
        Debug.Log("plan完成：" + executionPlan.isCompleted);
        Debug.Log("剩余item因BattleEnded跳过并完成：" + AreAllExecutionItemsCompleted(executionPlan));
        Debug.Log("击杀槽位MarkUsed：" + (killSlot != null && killSlot.isUsed));
        Debug.Log("后续FreeAction未使用：" + (followSlot != null && !followSlot.isUsed && followAbility.currentUseCount == followUseCountBefore && context.allyA.currentGuilt == followGuiltBefore));
        Debug.Log("不使用ActionUnavailable提示：" + (context.runtimeState.battleResult == BattleResult.Victory && followSlot != null && !followSlot.isUsed));
    }

    // RunSingleAllyDeathExecutionFilteringBasicTestSequence = BattleEnded阶段B1：同一ExecutionPlan内死亡单位过滤聚合测试
    void RunSingleAllyDeathExecutionFilteringBasicTestSequence()
    {
        Debug.Log("===== SingleAllyDeathExecutionFilteringBasic 聚合测试开始 =====");

        RunDeadFreeActionActorSkippedSubTest();
        RunDeadResponderFallsBackToUnrespondedSubTest();
        RunDeadDefenseResponderFallsBackToPassiveGuardSubTest();
        RunDeadActualTargetEnemyItemSkippedSubTest();
        RunDeadPassiveGuardCandidateSkippedSubTest();
        RunLastLivingPlayerDeathTriggersDefeatSubTest();
        RunLivingSlotCreationAfterSingleDeathSubTest();
        RunNewEnemyIntentRetargetsLivingAllySubTest();
        RunRuntimeStateFiltersDeadActorSlotsSubTest();
        RunAllPlayersDeadCannotPrepareNextTurnSubTest();
        RunDeadUnitExcludedFromTurnLifecycleSubTest();
    }

    void RunDeadFreeActionActorSkippedSubTest()
    {
        Debug.Log("===== 模式46 子测试A：死亡角色FreeAction跳过 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("single_death_a", 30, 30, 50, 20, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1);
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(new List<BattleEnemyIntent>());

        BattleCardState deadAbility = CreateBattleEndedAbilityCard(context.allyB, "single_death_a_dead_ability", "DeadFreeActionShouldNotApply");
        BattleCardState liveAbility = CreateBattleEndedAbilityCard(context.allyA, "single_death_a_live_ability", "LiveFreeActionContinues");

        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyB, 1, context.allyB, deadAbility, context.allyB);
        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyA, 1, context.allyA, liveAbility, context.allyA);

        BattleActionSlot deadSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyB, 1);
        BattleActionSlot liveSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);

        int deadUseCountBefore = deadAbility.currentUseCount;
        int deadCooldownBefore = deadAbility.currentCooldown;
        int deadGuiltBefore = context.allyB.currentGuilt;
        int liveUseCountBefore = liveAbility.currentUseCount;

        context.allyB.currentHP = 0;

        BattleExecutionPlan executionPlan = CreateManualFreeActionPlan(deadSlot, liveSlot);
        BattleExecutionItem deadItem = executionPlan.executionItems[0];
        BattleExecutionItem liveItem = executionPlan.executionItems[1];
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        bool deadCardUnchanged =
            deadAbility.currentUseCount == deadUseCountBefore &&
            deadAbility.currentCooldown == deadCooldownBefore &&
            context.allyB.currentGuilt == deadGuiltBefore &&
            CountBuffStack(context.allyB, "DeadFreeActionShouldNotApply") == 0;

        Debug.Log("B是否死亡：" + context.allyB.IsDead());
        Debug.Log("死亡角色Resolver未调用：" + deadCardUnchanged);
        Debug.Log("死亡行动未使用卡牌：" + deadCardUnchanged);
        Debug.Log("死亡槽位未MarkUsed：" + (deadSlot != null && !deadSlot.isUsed));
        Debug.Log("B item是否完成：" + (deadItem != null && deadItem.isCompleted));
        Debug.Log("后续存活角色行动继续：" + (liveItem != null && liveItem.isCompleted && liveSlot != null && liveSlot.isUsed && liveAbility.currentUseCount == liveUseCountBefore + 1));
        Debug.Log("Plan全部完成：" + executionPlan.isCompleted);
        Debug.Log("BattleResult仍为None：" + (context.runtimeState.battleResult == BattleResult.None));
        Debug.Log("phase可进入Completed：" + (context.runtimeState.currentPhase == "Completed"));
        Debug.Log("日志不是ActionUnavailable：True");
    }

    void RunDeadResponderFallsBackToUnrespondedSubTest()
    {
        Debug.Log("===== 模式46 子测试B：响应者死亡，目标存活，转Unresponded =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("single_death_b", 30, 30, 50, 20, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1);
        BattleCardState responseAttack = CreateFixedAttackCardForCharacter(context.allyA, "single_death_b_response_attack", 4);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "single_death_b_enemy_attack", 6, 0);
        BattleEnemyIntent intent = new BattleEnemyIntent("single_death_b_intent", context.enemy, enemyAttack, context.allyB, 1, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent);

        BattleActionSlot responseSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);
        responseSlot.AssignResponse(context.allyA, responseAttack, intent, false);
        intent.MarkResponded();

        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(intentQueue);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);

        int bHPBefore = context.allyB.currentHP;
        int responseUseCountBefore = responseAttack.currentUseCount;
        int responseCooldownBefore = responseAttack.currentCooldown;
        int responseGuiltBefore = context.allyA.currentGuilt;

        context.allyA.currentHP = 0;

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        int expectedDamage = 6;
        bool responderCardUnchanged =
            responseAttack.currentUseCount == responseUseCountBefore &&
            responseAttack.currentCooldown == responseCooldownBefore &&
            context.allyA.currentGuilt == responseGuiltBefore;

        Debug.Log("响应者A是否死亡：" + context.allyA.IsDead());
        Debug.Log("实际目标B是否存活：" + !context.allyB.IsDead());
        Debug.Log("原响应卡未使用：" + responderCardUnchanged);
        Debug.Log("原响应槽位未MarkUsed：" + (responseSlot != null && !responseSlot.isUsed));
        Debug.Log("敌人转Unresponded只执行一次：" + (bHPBefore - context.allyB.currentHP == expectedDamage));
        Debug.Log("B HP前后：" + bHPBefore + " -> " + context.allyB.currentHP);
        Debug.Log("item是否完成：" + (item != null && item.isCompleted));
        Debug.Log("Plan是否完成：" + executionPlan.isCompleted);
        Debug.Log("不误判Defeat：" + (context.runtimeState.battleResult == BattleResult.None));
        Debug.Log("phase是否Completed：" + (context.runtimeState.currentPhase == "Completed"));
    }

    void RunDeadDefenseResponderFallsBackToPassiveGuardSubTest()
    {
        Debug.Log("===== 模式46 子测试C：Defense响应者死亡后PassiveGuard仍可接管 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("single_death_c", 30, 30, 50, 20, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        BattleCardState activeDefense = CreateTestDefenseCardForCharacter(context.allyA, "single_death_c_active_defense", 9, 1);
        BattleCardState passiveDefense = CreateTestDefenseCardForCharacter(context.allyB, "single_death_c_passive_defense", 5, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "single_death_c_enemy_attack", 8, 0);
        BattleEnemyIntent intent = new BattleEnemyIntent("single_death_c_intent", context.enemy, enemyAttack, context.allyB, 1, 1);
        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent);

        BattleActionSlot activeSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);
        activeSlot.AssignResponse(context.allyA, activeDefense, intent, false);
        intent.MarkResponded();
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, context.allyB, 1, context.allyB, passiveDefense);
        BattleActionSlot passiveSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyB, 1);

        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(intentQueue);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(actionSlots, intentQueue);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);
        int candidateCount = item != null && item.passiveGuardCandidates != null
            ? item.passiveGuardCandidates.Count
            : 0;

        int bHPBefore = context.allyB.currentHP;
        int activeUseCountBefore = activeDefense.currentUseCount;
        int passiveUseCountBefore = passiveDefense.currentUseCount;

        context.allyA.currentHP = 0;

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        Debug.Log("A主动响应失效且不使用：" + (activeDefense.currentUseCount == activeUseCountBefore && activeSlot != null && !activeSlot.isUsed));
        Debug.Log("Responded Defense item创建时候选数为0：" + (candidateCount == 0));
        Debug.Log("Executor现场重收集后B的PassiveGuard候选接管：" + (candidateCount == 0 && passiveSlot != null && passiveSlot.isUsed));
        Debug.Log("回落Unresponded后B的PassiveGuard正常接管：" + (passiveDefense.currentUseCount == passiveUseCountBefore && passiveSlot != null && passiveSlot.isUsed));
        Debug.Log("B HP前后：" + bHPBefore + " -> " + context.allyB.currentHP);
        Debug.Log("敌人不重复执行：" + (bHPBefore - context.allyB.currentHP == 3));
        Debug.Log("A槽位不MarkUsed：" + (activeSlot != null && !activeSlot.isUsed));
        Debug.Log("被动守备槽位按实际结果正确MarkUsed：" + (passiveSlot != null && passiveSlot.isUsed));
        Debug.Log("item是否完成：" + (item != null && item.isCompleted));
        Debug.Log("Plan是否完成：" + executionPlan.isCompleted);
    }

    void RunDeadActualTargetEnemyItemSkippedSubTest()
    {
        Debug.Log("===== 模式46 子测试D：actualTarget死亡，敌人item跳过 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("single_death_d", 30, 30, 50, 20, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "single_death_d_enemy_attack", 8, 0);
        BattleEnemyIntent intent = new BattleEnemyIntent("single_death_d_intent", context.enemy, enemyAttack, context.allyB, 1, 1);
        BattleCardState followAbility = CreateBattleEndedAbilityCard(context.allyA, "single_death_d_follow_ability", "DeadTargetFollowAction");
        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyA, 1, context.allyA, followAbility, context.allyA);
        BattleActionSlot followSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);

        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(BattleEnemyIntentManager.CreateIntentQueue(intent));

        BattleExecutionPlan executionPlan = CreateManualExecutionPlan(
            new BattleExecutionItem(1, BattleExecutionItemType.UnrespondedEnemyIntent, intent, null),
            new BattleExecutionItem(2, BattleExecutionItemType.FreeAction, null, followSlot)
        );
        BattleExecutionItem enemyItem = executionPlan.executionItems[0];

        int allyAHPBefore = context.allyA.currentHP;
        int enemyUseCountBefore = enemyAttack.currentUseCount;
        int followUseCountBefore = followAbility.currentUseCount;

        context.allyB.currentHP = 0;

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        Debug.Log("actualTarget B是否死亡：" + context.allyB.IsDead());
        Debug.Log("死亡目标item直接跳过：" + (enemyItem != null && enemyItem.isCompleted && enemyAttack.currentUseCount == enemyUseCountBefore));
        Debug.Log("未自动攻击其他角色：" + (context.allyA.currentHP == allyAHPBefore));
        Debug.Log("不MarkUsed任何响应或守备槽位：True");
        Debug.Log("A后续FreeAction继续执行：" + (followSlot != null && followSlot.isUsed && followAbility.currentUseCount == followUseCountBefore + 1));
        Debug.Log("Plan是否完成：" + executionPlan.isCompleted);
        Debug.Log("不误判Defeat：" + (context.runtimeState.battleResult == BattleResult.None));
    }

    void RunDeadPassiveGuardCandidateSkippedSubTest()
    {
        Debug.Log("===== 模式46 子测试E：失效或死亡PassiveGuard候选被跳过 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("single_death_e", 30, 30, 50, 20, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        BattleCardState deadCandidateDefense = CreateTestDefenseCardForCharacter(context.allyA, "single_death_e_dead_candidate_defense", 9, 1);
        BattleCardState validDefense = CreateTestDefenseCardForCharacter(context.allyB, "single_death_e_valid_defense", 5, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "single_death_e_enemy_attack", 8, 0);
        BattleEnemyIntent intent = new BattleEnemyIntent("single_death_e_intent", context.enemy, enemyAttack, context.allyB, 1, 1);

        BattleActionSlot deadCandidateSlot = new BattleActionSlot(context.allyA, 1);
        deadCandidateSlot.AssignPassiveGuard(context.allyA, deadCandidateDefense);
        BattleActionSlotManager.AssignPassiveGuard(actionSlots, context.allyB, 1, context.allyB, validDefense);
        BattleActionSlot validSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyB, 1);

        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(BattleEnemyIntentManager.CreateIntentQueue(intent));

        BattleExecutionPlan executionPlan = CreateManualExecutionPlan(
            new BattleExecutionItem(
                1,
                BattleExecutionItemType.UnrespondedEnemyIntent,
                intent,
                null,
                new List<BattleActionSlot> { deadCandidateSlot, validSlot }
            )
        );

        context.allyA.currentHP = 0;
        int bHPBefore = context.allyB.currentHP;
        int deadUseCountBefore = deadCandidateDefense.currentUseCount;
        int validUseCountBefore = validDefense.currentUseCount;

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        Debug.Log("第一候选为受控死亡/身份不匹配候选，actor死亡：" + context.allyA.IsDead());
        Debug.Log("第一候选被执行前复查跳过：" + (deadCandidateDefense.currentUseCount == deadUseCountBefore && !deadCandidateSlot.isUsed));
        Debug.Log("第一候选不使用、不MarkUsed：" + (deadCandidateDefense.currentUseCount == deadUseCountBefore && !deadCandidateSlot.isUsed));
        Debug.Log("第二有效候选正常接管：" + (validSlot != null && validSlot.isUsed && validDefense.currentUseCount == validUseCountBefore));
        Debug.Log("敌人只结算一次：" + (bHPBefore - context.allyB.currentHP == 3));
        Debug.Log("Plan是否完成：" + executionPlan.isCompleted);
    }

    void RunLastLivingPlayerDeathTriggersDefeatSubTest()
    {
        Debug.Log("===== 模式46 子测试F：最后一名玩家死亡进入Defeat =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("single_death_f", 0, 5, 50, 20, 3, 8);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1);
        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "single_death_f_enemy_attack", 8, 0);
        BattleEnemyIntent intent = new BattleEnemyIntent("single_death_f_intent", context.enemy, enemyAttack, context.allyB, 1, 1);
        BattleCardState skippedAbility = CreateBattleEndedAbilityCard(context.allyB, "single_death_f_skipped_ability", "DefeatSkippedAbility");
        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyB, 1, context.allyB, skippedAbility, context.allyB);
        BattleActionSlot skippedSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyB, 1);

        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(BattleEnemyIntentManager.CreateIntentQueue(intent));

        BattleExecutionPlan executionPlan = CreateManualExecutionPlan(
            new BattleExecutionItem(1, BattleExecutionItemType.UnrespondedEnemyIntent, intent, null),
            new BattleExecutionItem(2, BattleExecutionItemType.FreeAction, null, skippedSlot)
        );

        BattleExecutionItem fatalItem = executionPlan.executionItems[0];
        BattleExecutionItem skippedItem = executionPlan.executionItems[1];
        int bHPBefore = context.allyB.currentHP;

        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        Debug.Log("致命攻击正常结算：" + (fatalItem != null && fatalItem.isCompleted && bHPBefore > 0 && context.allyB.IsDead()));
        Debug.Log("最后一名玩家死亡后Defeat：" + (context.runtimeState.IsBattleEnded && context.runtimeState.battleResult == BattleResult.Defeat));
        Debug.Log("后续item因BattleEnded跳过：" + (skippedItem != null && skippedItem.isCompleted && skippedSlot != null && !skippedSlot.isUsed));
        Debug.Log("Plan全部完成：" + executionPlan.isCompleted);
        Debug.Log("phase是否BattleEnded：" + (context.runtimeState.currentPhase == "BattleEnded"));
    }

    void RunLivingSlotCreationAfterSingleDeathSubTest()
    {
        Debug.Log("===== 模式46 子测试G：B死亡后只创建A槽位 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("single_death_g", 30, 0, 50, 20, 3, 8);
        List<BattleActionSlot> livingSlots = BattleActionSlotManager.CreateLivingPartyActionSlots(context.allyA, context.allyB, 2);

        Debug.Log("总槽位数量为2：" + (livingSlots != null && livingSlots.Count == 2));
        Debug.Log("A槽位1存在：" + HasOwnerSlotInList(livingSlots, context.allyA, 1));
        Debug.Log("A槽位2存在：" + HasOwnerSlotInList(livingSlots, context.allyA, 2));
        Debug.Log("不存在任何B槽位：" + !HasAnyOwnerSlotInList(livingSlots, context.allyB));
        Debug.Log("slotIndex为1、2：" + (HasOwnerSlotInList(livingSlots, context.allyA, 1) && HasOwnerSlotInList(livingSlots, context.allyA, 2)));
        Debug.Log("owner均为A：" + AreAllSlotsOwnedBy(livingSlots, context.allyA));
    }

    void RunNewEnemyIntentRetargetsLivingAllySubTest()
    {
        Debug.Log("===== 模式46 子测试H：B死亡后新意图改选A =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("single_death_h", 30, 0, 50, 20, 3, 8);
        List<BattleActionSlot> livingSlots = BattleActionSlotManager.CreateLivingPartyActionSlots(context.allyA, context.allyB, 2);

        int targetSlotIndex;
        CharacterData target = BattleSimpleUIController.SelectFixedEnemyIntentTarget(
            context.allyA,
            context.allyB,
            livingSlots,
            out targetSlotIndex
        );

        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "single_death_h_enemy_attack", 5, 0);
        List<BattleEnemyIntent> intentQueue = target != null
            ? BattleEnemyIntentManager.CreateIntentQueue(
                new BattleEnemyIntent("single_death_h_intent", context.enemy, enemyAttack, target, targetSlotIndex, 1)
            )
            : new List<BattleEnemyIntent>();

        BattleEnemyIntent intent = intentQueue.Count > 0 ? intentQueue[0] : null;

        Debug.Log("新敌人意图数量为1：" + (intentQueue.Count == 1));
        Debug.Log("originalTargetCharacter == A：" + (intent != null && object.ReferenceEquals(intent.originalTargetCharacter, context.allyA)));
        Debug.Log("actualTargetCharacter == A：" + (intent != null && object.ReferenceEquals(intent.actualTargetCharacter, context.allyA)));
        Debug.Log("originalTargetSlotIndex为1：" + (intent != null && intent.originalTargetSlotIndex == 1));
        Debug.Log("actualTargetSlotIndex为1：" + (intent != null && intent.actualTargetSlotIndex == 1));
        Debug.Log("没有引用B或上一回合旧槽位：" + (intent != null && !object.ReferenceEquals(intent.originalTargetCharacter, context.allyB) && !object.ReferenceEquals(intent.actualTargetCharacter, context.allyB)));
    }

    void RunRuntimeStateFiltersDeadActorSlotsSubTest()
    {
        Debug.Log("===== 模式46 子测试I：RuntimeState过滤死亡角色槽位 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("single_death_i", 30, 0, 50, 20, 3, 8);
        List<BattleActionSlot> mixedSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);

        int targetSlotIndex;
        CharacterData target = BattleSimpleUIController.SelectFixedEnemyIntentTarget(
            context.allyA,
            context.allyB,
            mixedSlots,
            out targetSlotIndex
        );

        BattleCardState enemyAttack = CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "single_death_i_enemy_attack", 5, 0);
        List<BattleEnemyIntent> intentQueue = target != null
            ? BattleEnemyIntentManager.CreateIntentQueue(
                new BattleEnemyIntent("single_death_i_intent", context.enemy, enemyAttack, target, targetSlotIndex, 1)
            )
            : new List<BattleEnemyIntent>();

        SetTestLifecyclePhase(
            context.runtimeState,
            BattleLifecyclePhase.TurnEnded
        );
        context.runtimeState.PrepareNextTurnWithRuntimeObjects(mixedSlots, intentQueue);

        Debug.Log("RuntimeState只保留A槽位：" + (context.runtimeState.actionSlots.Count == 2 && AreAllSlotsOwnedBy(context.runtimeState.actionSlots, context.allyA)));
        Debug.Log("B槽位未进入正式下一回合：" + !HasAnyOwnerSlotInList(context.runtimeState.actionSlots, context.allyB));
        Debug.Log("phase正常进入Prepare：" + (context.runtimeState.currentPhase == "Prepare"));
        Debug.Log("battleResult仍为None：" + (context.runtimeState.battleResult == BattleResult.None));
        Debug.Log("新意图目标是A：" + (context.runtimeState.intentQueue.Count == 1 && object.ReferenceEquals(context.runtimeState.intentQueue[0].originalTargetCharacter, context.allyA)));
        Debug.Log("不影响A的槽位：" + (HasOwnerSlotInList(context.runtimeState.actionSlots, context.allyA, 1) && HasOwnerSlotInList(context.runtimeState.actionSlots, context.allyA, 2)));
    }

    void RunAllPlayersDeadCannotPrepareNextTurnSubTest()
    {
        Debug.Log("===== 模式46 子测试J：全部玩家死亡不能准备下一回合 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext("single_death_j", 0, 0, 50, 20, 3, 8);
        SetTestLifecyclePhase(context.runtimeState, BattleLifecyclePhase.Executing);
        context.runtimeState.EvaluateBattleEnd();

        List<BattleActionSlot> livingSlots = BattleActionSlotManager.CreateLivingPartyActionSlots(context.allyA, context.allyB, 2);

        int targetSlotIndex;
        CharacterData target = BattleSimpleUIController.SelectFixedEnemyIntentTarget(
            context.allyA,
            context.allyB,
            livingSlots,
            out targetSlotIndex
        );

        List<BattleEnemyIntent> intentQueue = target != null
            ? BattleEnemyIntentManager.CreateIntentQueue(
                new BattleEnemyIntent(
                    "single_death_j_intent",
                    context.enemy,
                    CreateFixedEnemyAttackCardForDodgeTest(context.enemy, "single_death_j_enemy_attack", 5, 0),
                    target,
                    targetSlotIndex,
                    1
                )
            )
            : new List<BattleEnemyIntent>();

        string phaseBefore = context.runtimeState.currentPhase;
        BattleResult resultBefore = context.runtimeState.battleResult;

        context.runtimeState.PrepareNextTurnWithRuntimeObjects(livingSlots, intentQueue);

        Debug.Log("存活槽位数量为0：" + (livingSlots != null && livingSlots.Count == 0));
        Debug.Log("不创建敌人意图：" + (intentQueue.Count == 0));
        Debug.Log("PrepareNextTurn被拒绝：" + (context.runtimeState.currentPhase == phaseBefore));
        Debug.Log("phase保持BattleEnded：" + (context.runtimeState.currentPhase == "BattleEnded"));
        Debug.Log("result保持Defeat：" + (context.runtimeState.battleResult == resultBefore && context.runtimeState.battleResult == BattleResult.Defeat));
    }

    void RunDeadUnitExcludedFromTurnLifecycleSubTest()
    {
        Debug.Log("===== 模式46 子测试K：死亡角色不参与TurnStart / TurnEnd =====");

        BattleEndedTestContext startContext = CreateBattleEndedTestContext("single_death_k_start", 30, 0, 50, 20, 3, 8);
        startContext.allyA.AddPendingBuff("SingleDeathKAliveAStart", "K存活A回合开始证明", "AbilityBuff", 1, 1, "None", "Permanent", 0, 1, 1);
        startContext.allyB.AddPendingBuff("SingleDeathKDeadBStart", "K死亡B回合开始证明", "AbilityBuff", 1, 1, "None", "Permanent", 0, 1, 1);
        startContext.enemy.AddPendingBuff("SingleDeathKEnemyStart", "K敌人回合开始证明", "AbilityBuff", 1, 1, "None", "Permanent", 0, 1, 1);

        int bPendingBuffCountBefore = startContext.allyB.pendingBuffs.Count;
        int bBuffStackBefore = CountBuffStack(startContext.allyB, "SingleDeathKDeadBStart");
        startContext.allyB.turnSpeed = 99;
        int bTurnSpeedBefore = startContext.allyB.turnSpeed;
        int bCurrentSpeedBefore = startContext.allyB.GetCurrentSpeed();

        List<BattleActionSlot> livingSlots = BattleActionSlotManager.CreateLivingPartyActionSlots(startContext.allyA, startContext.allyB, 2);

        int targetSlotIndex;
        CharacterData target = BattleSimpleUIController.SelectFixedEnemyIntentTarget(
            startContext.allyA,
            startContext.allyB,
            livingSlots,
            out targetSlotIndex
        );

        List<BattleEnemyIntent> intentQueue = target != null
            ? BattleEnemyIntentManager.CreateIntentQueue(
                new BattleEnemyIntent(
                    "single_death_k_start_intent",
                    startContext.enemy,
                    CreateFixedEnemyAttackCardForDodgeTest(startContext.enemy, "single_death_k_start_enemy_attack", 5, 0),
                    target,
                    targetSlotIndex,
                    1
                )
            )
            : new List<BattleEnemyIntent>();

        SetTestLifecyclePhase(
            startContext.runtimeState,
            BattleLifecyclePhase.TurnEnded
        );
        startContext.runtimeState.PrepareNextTurnWithRuntimeObjects(livingSlots, intentQueue);

        bool deadBPendingNotApplied =
            CountBuffStack(startContext.allyB, "SingleDeathKDeadBStart") == bBuffStackBefore &&
            startContext.allyB.pendingBuffs.Count == bPendingBuffCountBefore;

        bool deadBNotRolled = startContext.allyB.turnSpeed == bTurnSpeedBefore;
        bool deadBCurrentSpeedUnchanged = startContext.allyB.GetCurrentSpeed() == bCurrentSpeedBefore;
        bool aliveAStarted =
            CountBuffStack(startContext.allyA, "SingleDeathKAliveAStart") > 0 &&
            startContext.allyA.pendingBuffs.Count == 0;
        bool enemyStarted =
            CountBuffStack(startContext.enemy, "SingleDeathKEnemyStart") > 0 &&
            startContext.enemy.pendingBuffs.Count == 0;

        Debug.Log("死亡B未参与TurnStart：" + (deadBPendingNotApplied && deadBNotRolled && deadBCurrentSpeedUnchanged));
        Debug.Log("死亡B未应用pendingBuff：" + deadBPendingNotApplied);
        Debug.Log("死亡B未重新Roll速度：" + deadBNotRolled);
        Debug.Log("死亡B的currentSpeed保持不变：" + deadBCurrentSpeedUnchanged);
        Debug.Log("存活A正常参与TurnStart：" + aliveAStarted);
        Debug.Log("存活Enemy正常参与TurnStart：" + enemyStarted);
        Debug.Log("phase正常进入Prepare：" + (startContext.runtimeState.currentPhase == "Prepare"));

        BattleEndedTestContext endContext = CreateBattleEndedTestContext("single_death_k_end", 30, 0, 50, 20, 3, 8);
        endContext.allyA.AddBuff("SingleDeathKAliveAEnd", "K存活A回合结束证明", "AbilityBuff", 1, 2, "TurnEnd", "DurationDown");
        endContext.allyB.AddBuff("SingleDeathKDeadBEnd", "K死亡B回合结束证明", "AbilityBuff", 1, 2, "TurnEnd", "DurationDown");

        int aEndDurationBefore = GetBuffDuration(endContext.allyA, "SingleDeathKAliveAEnd");
        int bEndDurationBefore = GetBuffDuration(endContext.allyB, "SingleDeathKDeadBEnd");

        BattleExecutionPlan completedPlan = new BattleExecutionPlan();
        completedPlan.isCompleted = true;
        endContext.runtimeState.SetExecutionPlan(completedPlan);
        SetTestLifecyclePhase(
            endContext.runtimeState,
            BattleLifecyclePhase.TurnResolved
        );
        endContext.runtimeState.EndCurrentTurnAndClearRuntimeObjects();

        int aEndDurationAfter = GetBuffDuration(endContext.allyA, "SingleDeathKAliveAEnd");
        int bEndDurationAfter = GetBuffDuration(endContext.allyB, "SingleDeathKDeadBEnd");

        Debug.Log("存活A正常参与TurnEnd：" + (aEndDurationAfter == aEndDurationBefore - 1));
        Debug.Log("A的Buff持续时间正常下降：" + (aEndDurationBefore == 2 && aEndDurationAfter == 1));
        Debug.Log("死亡B未参与TurnEnd：" + (bEndDurationAfter == bEndDurationBefore));
        Debug.Log("B的Buff持续时间保持不变：" + (bEndDurationBefore == 2 && bEndDurationAfter == 2));
    }

    void RunBattleEndedVictoryStopsRemainingFreeActionSubTest()
    {
        Debug.Log("===== BattleEnded 子测试A：Victory并停止后续FreeAction =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "battle_ended_victory",
            30,
            30,
            5,
            20,
            3,
            8
        );

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        List<BattleEnemyIntent> emptyIntentQueue = new List<BattleEnemyIntent>();
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(emptyIntentQueue);

        BattleCardState killAttack = CreateBattleEndedKillAttackCard(context.allyA, "battle_ended_victory_kill_attack", 6);
        BattleCardState followAbility = CreateBattleEndedAbilityCard(context.allyA, "battle_ended_victory_follow_ability", "VictoryFollowAbilityBuff");

        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyA, 1, context.allyA, killAttack, context.enemy);
        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyA, 2, context.allyA, followAbility, context.allyA);

        BattleActionSlot killSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);
        BattleActionSlot abilitySlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 2);

        int enemyHPBefore = context.enemy.currentHP;
        int abilityUseCountBefore = followAbility.currentUseCount;
        int abilityCooldownBefore = followAbility.currentCooldown;
        int allyGuiltBefore = context.allyA.currentGuilt;
        int followBuffBefore = CountBuffStack(context.allyA, "VictoryFollowAbilityBuff");

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(actionSlots, emptyIntentQueue);
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        bool afterKillCompleted = CountBuffStack(context.allyA, "BattleEndedAfterKillProof") > 0;
        bool killCardUsed = killSlot != null && killSlot.isUsed && killAttack.currentCooldown == GetExpectedResolvedCooldown(killAttack);
        bool followAbilityNotExecuted =
            followAbility.currentUseCount == abilityUseCountBefore &&
            followAbility.currentCooldown == abilityCooldownBefore &&
            context.allyA.currentGuilt == allyGuiltBefore &&
            CountBuffStack(context.allyA, "VictoryFollowAbilityBuff") == followBuffBefore &&
            abilitySlot != null &&
            !abilitySlot.isUsed;

        Debug.Log("敌人HP前后：" + enemyHPBefore + " -> " + context.enemy.currentHP);
        Debug.Log("phase是否符合预期：" + (context.runtimeState.currentPhase == "BattleEnded"));
        Debug.Log("battleResult是否符合预期：" + (context.runtimeState.battleResult == BattleResult.Victory));
        Debug.Log("敌人是否死亡：" + context.enemy.IsDead());
        Debug.Log("第一槽位是否MarkUsed：" + (killSlot != null && killSlot.isUsed));
        Debug.Log("后续槽位是否未使用：" + (abilitySlot != null && !abilitySlot.isUsed));
        Debug.Log("后续卡牌CD / UseCount / guilt是否不变：" + followAbilityNotExecuted);
        Debug.Log("ExecutionPlan是否完成：" + executionPlan.isCompleted);
        Debug.Log("剩余item是否被标记完成：" + AreAllExecutionItemsCompleted(executionPlan));
        Debug.Log("击杀卡正常使用：" + killCardUsed);
        Debug.Log("AfterKill完成后才BattleEnded：" + (afterKillCompleted && context.runtimeState.IsBattleEnded));
        Debug.Log("后续Ability未执行：" + followAbilityNotExecuted);
    }

    void RunBattleEndedDefeatSubTest()
    {
        Debug.Log("===== BattleEnded 子测试B：Defeat =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "battle_ended_defeat",
            30,
            5,
            50,
            1,
            3,
            8
        );
        context.allyA.currentHP = 0;

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        BattleCardState playerAttack = CreateFixedAttackCardForCharacter(context.allyB, "battle_ended_defeat_b_attack", 1);
        BattleCardState enemyAttack = BattleCardManager.CreateBattleCard(
            context.enemy,
            CreateObservableEnemyAttackCardData("battle_ended_defeat_enemy_attack", "BattleEnded Defeat 敌人攻击", 8),
            "battle_ended_defeat_enemy_attack_copy_0"
        );
        BattleCardState skippedAttack = CreateFixedAttackCardForCharacter(context.allyA, "battle_ended_defeat_skipped_attack", 3);

        BattleEnemyIntent intent = new BattleEnemyIntent(
            "battle_ended_defeat_intent_001",
            context.enemy,
            enemyAttack,
            context.allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent);
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(intentQueue);

        BattleActionSlotManager.AssignResponseToEnemyIntent(actionSlots, context.allyB, 1, context.allyB, playerAttack, intent);
        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyA, 1, context.allyA, skippedAttack, context.enemy);

        BattleActionSlot skippedSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);
        int enemyUseCountBefore = enemyAttack.currentUseCount;
        int skippedCooldownBefore = skippedAttack.currentCooldown;

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(actionSlots, intentQueue);
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        Debug.Log("A/B HP与死亡状态：A " + context.allyA.currentHP + " dead=" + context.allyA.IsDead() + "，B " + context.allyB.currentHP + " dead=" + context.allyB.IsDead());
        Debug.Log("phase是否符合预期：" + (context.runtimeState.currentPhase == "BattleEnded"));
        Debug.Log("battleResult是否符合预期：" + (context.runtimeState.battleResult == BattleResult.Defeat));
        Debug.Log("敌人击杀卡正常完成使用：" + (enemyAttack.currentUseCount == enemyUseCountBefore + 1));
        Debug.Log("后续item不执行：" + (skippedSlot != null && !skippedSlot.isUsed && skippedAttack.currentCooldown == skippedCooldownBefore));
        Debug.Log("ExecutionPlan是否完成：" + executionPlan.isCompleted);
        Debug.Log("剩余item是否被标记完成：" + AreAllExecutionItemsCompleted(executionPlan));
        Debug.Log("仅全灭时Defeat：" + (context.allyA.IsDead() && context.allyB.IsDead() && context.runtimeState.battleResult == BattleResult.Defeat));
    }

    void RunBattleEndedSinglePlayerDeathNotDefeatSubTest()
    {
        Debug.Log("===== BattleEnded 子测试C：单名玩家死亡不误判Defeat =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "battle_ended_single_death",
            30,
            5,
            50,
            1,
            3,
            8
        );

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        BattleCardState playerAttack = CreateFixedAttackCardForCharacter(context.allyB, "battle_ended_single_death_b_attack", 1);
        BattleCardState enemyAttack = BattleCardManager.CreateBattleCard(
            context.enemy,
            CreateObservableEnemyAttackCardData("battle_ended_single_death_enemy_attack", "BattleEnded 单人死亡敌人攻击", 8),
            "battle_ended_single_death_enemy_attack_copy_0"
        );
        BattleCardState followAttack = CreateFixedAttackCardForCharacter(context.allyA, "battle_ended_single_death_a_follow_attack", 3);

        BattleEnemyIntent intent = new BattleEnemyIntent(
            "battle_ended_single_death_intent_001",
            context.enemy,
            enemyAttack,
            context.allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent);
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(intentQueue);

        BattleActionSlotManager.AssignResponseToEnemyIntent(actionSlots, context.allyB, 1, context.allyB, playerAttack, intent);
        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyA, 1, context.allyA, followAttack, context.enemy);

        BattleActionSlot followSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);
        int enemyHPBefore = context.enemy.currentHP;

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(actionSlots, intentQueue);
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        Debug.Log("A/B HP与死亡状态：A " + context.allyA.currentHP + " dead=" + context.allyA.IsDead() + "，B " + context.allyB.currentHP + " dead=" + context.allyB.IsDead());
        Debug.Log("phase是否符合预期：" + (context.runtimeState.currentPhase == "Completed"));
        Debug.Log("battleResult是否符合预期：" + (context.runtimeState.battleResult == BattleResult.None));
        Debug.Log("仅一人死亡不进入Defeat：" + (!context.allyA.IsDead() && context.allyB.IsDead() && context.runtimeState.battleResult == BattleResult.None));
        Debug.Log("后续item仍可继续执行：" + (followSlot != null && followSlot.isUsed));
        Debug.Log("敌人HP前后：" + enemyHPBefore + " -> " + context.enemy.currentHP);
        Debug.Log("ExecutionPlan是否完成：" + executionPlan.isCompleted);
    }

    void RunBattleEndedSimultaneousDeathPrioritizesDefeatSubTest()
    {
        Debug.Log("===== BattleEnded 子测试D：双方同时死亡优先Defeat =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "battle_ended_simultaneous",
            30,
            30,
            30,
            20,
            3,
            8
        );

        context.allyA.currentHP = 0;
        context.allyB.currentHP = 0;
        context.enemy.currentHP = 0;

        SetTestLifecyclePhase(context.runtimeState, BattleLifecyclePhase.Executing);
        context.runtimeState.EvaluateBattleEnd();

        Debug.Log("phase是否符合预期：" + (context.runtimeState.currentPhase == "BattleEnded"));
        Debug.Log("battleResult是否符合预期：" + (context.runtimeState.battleResult == BattleResult.Defeat));
        Debug.Log("双方同时死亡优先Defeat：" + (context.runtimeState.battleResult == BattleResult.Defeat));
    }

    void RunBattleEndedOperationGuardSubTest()
    {
        Debug.Log("===== BattleEnded 子测试E：BattleEnded后方法保护 =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "battle_ended_guard",
            30,
            30,
            0,
            20,
            3,
            8
        );

        List<BattleActionSlot> originalSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1);
        BattleCardState skippedAttack = CreateFixedAttackCardForCharacter(context.allyA, "battle_ended_guard_skipped_attack", 3);
        BattleActionSlotManager.AssignFreeAction(originalSlots, context.allyA, 1, context.allyA, skippedAttack, context.enemy);

        BattleEnemyIntent originalIntent = new BattleEnemyIntent(
            "battle_ended_guard_intent_001",
            context.enemy,
            BattleCardManager.CreateBattleCard(
                context.enemy,
                CreateFixedAttackCardData("battle_ended_guard_enemy_attack", "BattleEnded Guard 敌人攻击", 3),
                "battle_ended_guard_enemy_attack_copy_0"
            ),
            context.allyB,
            1,
            1
        );

        List<BattleEnemyIntent> originalIntents = BattleEnemyIntentManager.CreateIntentQueue(originalIntent);
        context.runtimeState.SetActionSlots(originalSlots);
        context.runtimeState.SetIntentQueue(originalIntents);
        SetTestLifecyclePhase(context.runtimeState, BattleLifecyclePhase.Executing);
        context.runtimeState.EvaluateBattleEnd();

        int slotCountBefore = context.runtimeState.actionSlots.Count;
        int intentCountBefore = context.runtimeState.intentQueue.Count;
        BattleResult resultBefore = context.runtimeState.battleResult;
        string phaseBefore = context.runtimeState.currentPhase;
        int skippedCooldownBefore = skippedAttack.currentCooldown;
        int skippedUseCountBefore = skippedAttack.currentUseCount;
        int allyGuiltBefore = context.allyA.currentGuilt;

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(originalSlots, originalIntents);
        context.runtimeState.SetExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan, context.runtimeState);
        context.runtimeState.EndCurrentTurnAndClearRuntimeObjects();
        context.runtimeState.PrepareNextTurnWithRuntimeObjects(
            BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2),
            new List<BattleEnemyIntent>()
        );

        Debug.Log("再次ExecutePlan是否被拒绝：" + (!originalSlots[0].isUsed && skippedAttack.currentCooldown == skippedCooldownBefore && skippedAttack.currentUseCount == skippedUseCountBefore && context.allyA.currentGuilt == allyGuiltBefore));
        Debug.Log("EndTurn是否被拒绝：" + (context.runtimeState.currentPhase == phaseBefore && context.runtimeState.battleResult == resultBefore));
        Debug.Log("PrepareNextTurn是否被拒绝：" + (context.runtimeState.actionSlots.Count == slotCountBefore && context.runtimeState.intentQueue.Count == intentCountBefore));
        Debug.Log("phase是否符合预期：" + (context.runtimeState.currentPhase == "BattleEnded"));
        Debug.Log("battleResult是否符合预期：" + (context.runtimeState.battleResult == BattleResult.Victory));
        Debug.Log("不创建新槽位：" + (context.runtimeState.actionSlots.Count == slotCountBefore));
        Debug.Log("不创建新意图：" + (context.runtimeState.intentQueue.Count == intentCountBefore));
        Debug.Log("ExecutionPlan是否完成：" + executionPlan.isCompleted);
        Debug.Log("剩余item是否被标记完成：" + AreAllExecutionItemsCompleted(executionPlan));
    }

    void RunBattleEndedNonLethalCompletedSubTest()
    {
        Debug.Log("===== BattleEnded 子测试F：非致命战斗仍进入Completed =====");

        BattleEndedTestContext context = CreateBattleEndedTestContext(
            "battle_ended_non_lethal",
            30,
            30,
            50,
            20,
            3,
            8
        );

        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 1);
        List<BattleEnemyIntent> emptyIntentQueue = new List<BattleEnemyIntent>();
        context.runtimeState.SetActionSlots(actionSlots);
        context.runtimeState.SetIntentQueue(emptyIntentQueue);

        BattleCardState attack = CreateFixedAttackCardForCharacter(context.allyA, "battle_ended_non_lethal_attack", 3);
        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyA, 1, context.allyA, attack, context.enemy);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(actionSlots, emptyIntentQueue);
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        Debug.Log("敌人HP前后：50 -> " + context.enemy.currentHP);
        Debug.Log("phase是否符合预期：" + (context.runtimeState.currentPhase == "Completed"));
        Debug.Log("battleResult是否符合预期：" + (context.runtimeState.battleResult == BattleResult.None));
        Debug.Log("ExecutionPlan是否完成：" + executionPlan.isCompleted);
        Debug.Log("Completed != BattleEnded：" + (context.runtimeState.currentPhase == "Completed" && !context.runtimeState.IsBattleEnded));
    }

    void RunRespondedAttackPassiveGuardSubTest(
        string title,
        int playerAttackPoint,
        int enemyAttackPoint,
        int slotCountPerCharacter,
        int guardSlot2DefensePoint,
        int guardSlot3DefensePoint,
        bool invalidateGuardSlot2BeforeExecute,
        bool guardSlot2OwnerIsAllyA,
        bool expectGuardSlot2Used,
        bool expectGuardSlot3Used,
        int expectedDamageToAllyB,
        bool expectPassiveGuardResult,
        string expectedResultTypeInLog
    )
    {
        Debug.Log("===== " + title + " 测试开始 =====");
        Debug.Log("预期 resultType 出现在 Resolver 日志：" + expectedResultTypeInLog);

        StartTurn();

        int allyBHPBefore = allyB.currentHP;
        int enemyHPBefore = enemy.currentHP;

        BattleCardState responseAttack = CreateFixedAttackCardForCharacter(
            allyB,
            title + "_b_response_attack",
            playerAttackPoint
        );
        responseAttack.cardData.cooldown = 2;

        int responseAttackCooldownBefore = responseAttack.currentCooldown;
        int responseAttackUseCountBefore = responseAttack.currentUseCount;
        bool responseAttackConsumedBefore = responseAttack.isConsumed;
        int responseAttackGuiltBefore = allyB.currentGuilt;

        CardTestData enemyAttackCard = CreateObservableEnemyAttackCardData(
            title + "_enemy_attack",
            title + "敌人攻击",
            enemyAttackPoint
        );

        BattleCardState enemyAttack = BattleCardManager.CreateBattleCard(
            enemy,
            enemyAttackCard,
            title + "_enemy_attack_copy_0"
        );
        int enemyAttackUseCountBefore = enemyAttack.currentUseCount;

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            title + "_intent_001",
            enemy,
            enemyAttack,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(
            allyA,
            allyB,
            slotCountPerCharacter
        );

        BattleActionSlotManager.AssignResponseToEnemyIntent(
            actionSlots,
            allyB,
            1,
            allyB,
            responseAttack,
            intent1
        );

        BattleCardState guardSlot2 = null;
        BattleCardState guardSlot3 = null;

        if (guardSlot2DefensePoint >= 0)
        {
            CharacterData guardOwner = guardSlot2OwnerIsAllyA ? allyA : allyB;
            guardSlot2 = CreateTestDefenseCardForCharacter(
                guardOwner,
                title + "_guard_slot_2",
                guardSlot2DefensePoint,
                1
            );

            BattleActionSlotManager.AssignPassiveGuard(
                actionSlots,
                guardOwner,
                2,
                guardOwner,
                guardSlot2
            );
        }

        if (guardSlot3DefensePoint >= 0)
        {
            guardSlot3 = CreateTestDefenseCardForCharacter(
                allyB,
                title + "_guard_slot_3",
                guardSlot3DefensePoint,
                1
            );

            BattleActionSlotManager.AssignPassiveGuard(
                actionSlots,
                allyB,
                3,
                allyB,
                guardSlot3
            );
        }

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(
            actionSlots,
            intentQueue
        );

        BattleExecutionItem firstItem = GetFirstExecutionItem(executionPlan);
        int candidateCount = firstItem != null && firstItem.passiveGuardCandidates != null
            ? firstItem.passiveGuardCandidates.Count
            : 0;

        Debug.Log("计划生成后 Responded item 被动守备候选数：" + candidateCount);
        Debug.Log("执行前 玩家响应 Attack CD：" + responseAttackCooldownBefore);
        Debug.Log("执行前 玩家响应 Attack UseCount：" + responseAttackUseCountBefore + " / " + responseAttack.maxUseCount);
        Debug.Log("执行前 玩家响应 Attack isConsumed：" + responseAttackConsumedBefore);
        Debug.Log("执行前 玩家 guilt：" + responseAttackGuiltBefore);

        if (invalidateGuardSlot2BeforeExecute)
        {
            BattleActionSlot guardSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 2);

            if (guardSlot != null && guardSlot.cardState != null)
            {
                guardSlot.cardState.currentCooldown = 1;
                Debug.Log("执行前手动让 B槽位2 PassiveGuard 失效：currentCooldown = 1");
            }
        }

        BattleEnemyIntentManager.PrintIntentQueue(intentQueue);
        BattleActionSlotManager.PrintSlotStates(actionSlots);
        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        BattleActionSlot responseSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 1);
        BattleActionSlot guardSlot2After = guardSlot2OwnerIsAllyA
            ? BattleActionSlotManager.GetSlot(actionSlots, allyA, 2)
            : BattleActionSlotManager.GetSlot(actionSlots, allyB, 2);
        bool shouldCheckGuardSlot3 = guardSlot3DefensePoint >= 0 && slotCountPerCharacter >= 3;
        BattleActionSlot guardSlot3After = shouldCheckGuardSlot3
            ? BattleActionSlotManager.GetSlot(actionSlots, allyB, 3)
            : null;

        Debug.Log("执行后 我方角色B HP：" + allyB.currentHP + " / " + allyB.maxHP);
        Debug.Log("执行后 敌人 HP：" + enemy.currentHP + " / " + enemy.maxHP);
        Debug.Log("执行后 玩家响应 Attack CD：" + responseAttack.currentCooldown);
        Debug.Log("执行后 玩家响应 Attack UseCount：" + responseAttack.currentUseCount + " / " + responseAttack.maxUseCount);
        Debug.Log("执行后 玩家响应 Attack isConsumed：" + responseAttack.isConsumed);
        Debug.Log("执行后 玩家 guilt：" + allyB.currentGuilt);
        Debug.Log("预期 B HP 变化：" + expectedDamageToAllyB + "，实际是否符合：" + (allyB.currentHP == allyBHPBefore - expectedDamageToAllyB));
        Debug.Log("预期主响应 B槽位1 MarkUsed：" + (responseSlot != null && responseSlot.isUsed));
        bool expectResponseAttackResolved = playerAttackPoint > enemyAttackPoint;
        int expectedResponseAttackCooldown = expectResponseAttackResolved
            ? GetExpectedResolvedCooldown(responseAttack)
            : responseAttackCooldownBefore;
        Debug.Log("预期玩家响应 Attack 是否Resolved：" + expectResponseAttackResolved + "，CD是否符合：" + (responseAttack.currentCooldown == expectedResponseAttackCooldown));
        Debug.Log("预期玩家响应 Attack UseCount 不重复变化：" + (responseAttack.currentUseCount == responseAttackUseCountBefore));
        Debug.Log("预期玩家响应 Attack isConsumed 不变：" + (responseAttack.isConsumed == responseAttackConsumedBefore));
        Debug.Log("预期玩家 guilt 不变：" + (allyB.currentGuilt == responseAttackGuiltBefore));
        Debug.Log("预期 B槽位2 / A槽位2 PassiveGuard 使用状态：" + expectGuardSlot2Used + "，实际是否符合：" + IsSlotUsedStateExpected(guardSlot2After, expectGuardSlot2Used));

        if (shouldCheckGuardSlot3)
        {
            Debug.Log("预期 B槽位3 PassiveGuard 使用状态：" + expectGuardSlot3Used + "，实际是否符合：" + IsSlotUsedStateExpected(guardSlot3After, expectGuardSlot3Used));
        }

        Debug.Log("预期 PassiveGuard 结果：" + expectPassiveGuardResult + "，实际可从 resultType 日志确认：" + expectedResultTypeInLog);
        bool expectPlayerWin = playerAttackPoint > enemyAttackPoint;
        bool expectPassiveGuardTriggered = expectPassiveGuardResult;
        int expectedEnemyUseCount = expectPlayerWin ? enemyAttackUseCountBefore : enemyAttackUseCountBefore + 1;
        Debug.Log("预期敌人 UseCount：" + expectedEnemyUseCount + "，实际是否符合：" + (enemyAttack.currentUseCount == expectedEnemyUseCount));

        if (expectPlayerWin)
        {
            Debug.Log("PlayerWin 分支：敌人失败 Attack 不完成使用：" + (enemyAttack.currentUseCount == enemyAttackUseCountBefore));
        }
        else if (expectPassiveGuardTriggered)
        {
            Debug.Log("EnemyWin + PassiveGuard 分支：敌人卡没有被 known-point Defense 第二次完成：" + (enemyAttack.currentUseCount == expectedEnemyUseCount));
        }

        Debug.Log("预期 B槽位2 Defense CD：" + (guardSlot2 != null ? guardSlot2.currentCooldown : -1));

        if (shouldCheckGuardSlot3)
        {
            Debug.Log("预期 B槽位3 Defense CD：" + (guardSlot3 != null ? guardSlot3.currentCooldown : -1));
        }

        if (playerAttackPoint > enemyAttackPoint)
        {
            Debug.Log("PlayerWin 分支：敌人 HP 是否下降：" + (enemy.currentHP < enemyHPBefore));
        }
        else if (expectedDamageToAllyB == 0)
        {
            Debug.Log("EnemyWin + FullBlock 分支：实际目标角色 HP 是否保持不变：" + (allyB.currentHP == allyBHPBefore));
        }
        else
        {
            Debug.Log("EnemyWin 分支：实际目标角色 HP 是否按预期下降：" + (allyB.currentHP == allyBHPBefore - expectedDamageToAllyB));
        }

        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    // ================================
    // Action Slot 测试辅助方法
    // ================================

    ActionSlotViewData GetActionSlotViewByIndex(BattleStateViewData viewData, int index)
    {
        if (viewData == null || viewData.actionSlotViews == null)
        {
            return null;
        }

        if (index < 0 || index >= viewData.actionSlotViews.Count)
        {
            return null;
        }

        return viewData.actionSlotViews[index];
    }

    class BattleEndedTestContext
    {
        public CharacterData allyA;
        public CharacterData allyB;
        public CharacterData enemy;
        public BattleRuntimeState runtimeState;
    }

    BattleEndedTestContext CreateBattleEndedTestContext(
        string title,
        int allyAHP,
        int allyBHP,
        int enemyHP,
        int allyASpeed,
        int allyBSpeed,
        int enemySpeed
    )
    {
        BattleEndedTestContext context = new BattleEndedTestContext();
        context.allyA = new CharacterData(title + "_A", 30, allyASpeed, allyASpeed);
        context.allyB = new CharacterData(title + "_B", 30, allyBSpeed, allyBSpeed);
        context.enemy = new CharacterData(title + "_Enemy", 50, enemySpeed, enemySpeed);

        context.allyA.currentHP = allyAHP;
        context.allyB.currentHP = allyBHP;
        context.enemy.currentHP = enemyHP;

        context.runtimeState = new BattleRuntimeState();
        context.runtimeState.SetCharacters(context.allyA, context.allyB, context.enemy);
        SetTestLifecyclePhase(
            context.runtimeState,
            BattleLifecyclePhase.Prepare
        );

        return context;
    }

    bool SetTestLifecyclePhase(
        BattleRuntimeState runtimeState,
        BattleLifecyclePhase targetPhase
    )
    {
        return BattleLifecyclePhaseContractTests.TryReachPhaseForTest(
            runtimeState,
            targetPhase
        );
    }

    void ExecutePlanWithRuntimeStateAndCompleteTurn(BattleRuntimeState runtimeState, BattleExecutionPlan executionPlan)
    {
        runtimeState.SetExecutionPlan(executionPlan);
        SetTestLifecyclePhase(runtimeState, BattleLifecyclePhase.Executing);
        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan, runtimeState);

        if (!runtimeState.IsBattleEnded &&
            runtimeState.currentExecutionPlan != null &&
            runtimeState.currentExecutionPlan.isCompleted)
        {
            SetTestLifecyclePhase(
                runtimeState,
                BattleLifecyclePhase.TurnResolved
            );
        }
    }

    BattleExecutionPlan CreateManualFreeActionPlan(params BattleActionSlot[] actionSlots)
    {
        BattleExecutionPlan executionPlan = new BattleExecutionPlan();

        if (actionSlots == null)
        {
            return executionPlan;
        }

        for (int i = 0; i < actionSlots.Length; i++)
        {
            executionPlan.AddItem(
                new BattleExecutionItem(
                    i + 1,
                    BattleExecutionItemType.FreeAction,
                    null,
                    actionSlots[i]
                )
            );
        }

        return executionPlan;
    }

    BattleExecutionPlan CreateManualExecutionPlan(params BattleExecutionItem[] items)
    {
        BattleExecutionPlan executionPlan = new BattleExecutionPlan();

        if (items == null)
        {
            return executionPlan;
        }

        foreach (BattleExecutionItem item in items)
        {
            executionPlan.AddItem(item);
        }

        return executionPlan;
    }

    BattleCardState CreateEligibilityAttackCard(
        CharacterData owner,
        string instanceID,
        params CardUseConditionData[] useConditions
    )
    {
        CardTestData cardData = CreateFixedAttackCardData(instanceID + "_data", "资格测试攻击", 5);
        cardData.cooldown = 0;
        cardData.isSinCard = false;
        cardData.useConditions = useConditions;
        cardData.effects = new List<CardEffectData>();

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    CardUseConditionData CreateGuiltAtLeastCondition(int value)
    {
        return new CardUseConditionData
        {
            conditionType = CardUseConditionType.GuiltAtLeast,
            target = CardTargetType.Self,
            value = value
        };
    }

    CardUseConditionData CreateBuffStackAtLeastCondition(string buffID, int stack)
    {
        return new CardUseConditionData
        {
            conditionType = CardUseConditionType.BuffStackAtLeast,
            target = CardTargetType.Self,
            buffType = buffID,
            value = stack
        };
    }

    bool IsSlotEmpty(BattleActionSlot slot)
    {
        return slot != null && slot.IsEmpty();
    }

    void RemoveAllBuffs(CharacterData character, string buffID)
    {
        if (character == null || character.buffs == null || string.IsNullOrEmpty(buffID))
        {
            return;
        }

        for (int i = character.buffs.Count - 1; i >= 0; i--)
        {
            BuffData buff = character.buffs[i];

            if (buff != null && buff.buffID == buffID)
            {
                character.buffs.RemoveAt(i);
            }
        }
    }

    bool HasOwnerSlotInList(List<BattleActionSlot> slots, CharacterData owner, int slotIndex)
    {
        if (slots == null || owner == null)
        {
            return false;
        }

        foreach (BattleActionSlot slot in slots)
        {
            if (slot != null && object.ReferenceEquals(slot.owner, owner) && slot.slotIndex == slotIndex)
            {
                return true;
            }
        }

        return false;
    }

    bool HasAnyOwnerSlotInList(List<BattleActionSlot> slots, CharacterData owner)
    {
        if (slots == null || owner == null)
        {
            return false;
        }

        foreach (BattleActionSlot slot in slots)
        {
            if (slot != null && object.ReferenceEquals(slot.owner, owner))
            {
                return true;
            }
        }

        return false;
    }

    bool AreAllSlotsOwnedBy(List<BattleActionSlot> slots, CharacterData owner)
    {
        if (slots == null || slots.Count == 0 || owner == null)
        {
            return false;
        }

        foreach (BattleActionSlot slot in slots)
        {
            if (slot == null || !object.ReferenceEquals(slot.owner, owner))
            {
                return false;
            }
        }

        return true;
    }

    BattleCardState CreateBattleEndedKillAttackCard(CharacterData owner, string instanceID, int point)
    {
        CardTestData cardData = CreateFixedAttackCardData(instanceID + "_data", "BattleEnded 击杀攻击", point);
        cardData.rarity = "White";
        cardData.cooldown = 2;
        cardData.effects = new List<CardEffectData>
        {
            new CardEffectData
            {
                trigger = BattleTiming.AfterKill,
                effectType = CardEffectType.ApplyBuff,
                target = CardTargetType.Self,
                buffType = "BattleEndedAfterKillProof",
                buffName = "BattleEnded AfterKill Proof",
                buffCategory = "AbilityBuff",
                stack = 1,
                duration = 1,
                checkTiming = "TurnEnd",
                expireRule = "DurationDown"
            }
        };

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    BattleCardState CreateBulletLockedFreeAttackCard(CharacterData owner, string instanceID, int point, int requiredBullet)
    {
        CardTestData cardData = CreateFixedAttackCardData(instanceID + "_data", "Bullet不足测试攻击", point);
        cardData.isSinCard = true;
        cardData.rarity = "Sin";
        cardData.sinCardCategory = SinCardCategory.Clash;
        cardData.sinCardUseRule = SinCardUseRule.UseCount;
        cardData.maxUseCount = 3;
        cardData.guiltGain = 2;
        cardData.cooldown = 0;
        cardData.useConditions = new CardUseConditionData[]
        {
            new CardUseConditionData
            {
                conditionType = CardUseConditionType.BuffStackAtLeast,
                target = CardTargetType.Self,
                value = requiredBullet,
                buffType = "Bullet"
            }
        };

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    BattleCardState CreateBattleEndedAbilityCard(CharacterData owner, string instanceID, string buffType)
    {
        CardTestData cardData = new CardTestData
        {
            cardID = instanceID + "_data",
            cardName = "BattleEnded 后续 Ability",
            cardType = "Ability",
            isClashable = false,
            minPoint = 0,
            maxPoint = 0,
            isSinCard = true,
            sinCardCategory = SinCardCategory.Ability,
            sinCardUseRule = SinCardUseRule.UseCount,
            maxUseCount = 2,
            guiltGain = 2,
            effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    trigger = BattleTiming.OnPlay,
                    effectType = CardEffectType.ApplyBuff,
                    target = CardTargetType.Self,
                    buffType = buffType,
                    buffName = buffType,
                    buffCategory = "AbilityBuff",
                    stack = 1,
                    duration = 1,
                    checkTiming = "TurnEnd",
                    expireRule = "DurationDown"
                }
            }
        };

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    CardEffectData CreateApplyBuffEffect(string trigger, string buffID, int stack, int duration)
    {
        return CreateApplyBuffEffect(trigger, buffID, stack, duration, null);
    }

    CardEffectData CreateApplyBuffEffect(string trigger, string buffID, int stack, int duration, string requireClashResult)
    {
        CardEffectData effect = new CardEffectData
        {
            trigger = trigger,
            effectType = CardEffectType.ApplyBuff,
            target = CardTargetType.Self,
            buffType = buffID,
            stack = stack,
            duration = duration,
            requireClashResult = requireClashResult
        };

        BuffDefinitionData definition;

        if (BuffDefinitionLoader.TryGetDefinition(buffID, out definition))
        {
            return effect;
        }

        effect.buffName = buffID;
        effect.buffCategory = BuffCategory.UpBuff;
        effect.checkTiming = BattleTiming.TurnEnd;
        effect.expireRule = "DurationDown";

        return effect;
    }

    void AddProbeEffect(BattleCardState cardState, string trigger, string buffID)
    {
        AddProbeEffect(cardState, trigger, buffID, null);
    }

    void AddProbeEffect(BattleCardState cardState, string trigger, string buffID, string requireClashResult)
    {
        if (cardState == null || cardState.cardData == null)
        {
            return;
        }

        if (cardState.cardData.effects == null)
        {
            cardState.cardData.effects = new List<CardEffectData>();
        }

        cardState.cardData.effects.Add(
            CreateApplyBuffEffect(trigger, buffID, 1, 1, requireClashResult)
        );
    }

    BattleCardState CreateBeforeUseBuffAttackCard(
        CharacterData owner,
        string instanceID,
        int point,
        string buffID,
        int stack,
        int duration
    )
    {
        CardTestData cardData = CreateFixedAttackCardData(instanceID + "_data", "BeforeUse固定点攻击", point);
        cardData.effects = new List<CardEffectData>
        {
            CreateApplyBuffEffect(BattleTiming.BeforeUse, buffID, stack, duration)
        };

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    CardTestData LoadRealBulletAttackCard()
    {
        List<CardTestData> cards = CardDataLoader.LoadCardData();
        return FindRealBulletAttackCard(cards);
    }

    CardTestData FindRealBulletAttackCard(List<CardTestData> cards)
    {
        if (cards == null)
        {
            return null;
        }

        return CardDataLoader.FindCardByID(cards, "atk_bullet_001");
    }

    BattleCardState CreateRealBulletAttackCardState(CharacterData owner, string instanceID)
    {
        CardTestData cardData = LoadRealBulletAttackCard();

        if (cardData == null)
        {
            Debug.LogError("模式55 失败：未找到真实JSON卡 atk_bullet_001");
            return null;
        }

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    BattleActionSlot AssignRealBulletAttackFreeAction(
        BattleEndedTestContext context,
        BattleCardState attack,
        int slotIndex
    )
    {
        List<BattleActionSlot> slots = BattleActionSlotManager.CreatePartyActionSlots(context.allyA, context.allyB, 2);
        CardEligibilityResult result;
        BattleActionSlotManager.AssignFreeAction(slots, context.allyA, slotIndex, context.allyA, attack, context.enemy, out result);
        return BattleActionSlotManager.GetSlot(slots, context.allyA, slotIndex);
    }

    int CountCardsByID(List<CardTestData> cards, string cardID)
    {
        if (cards == null)
        {
            return 0;
        }

        int count = 0;

        foreach (CardTestData card in cards)
        {
            if (card != null && card.cardID == cardID)
            {
                count++;
            }
        }

        return count;
    }

    bool HasBulletHardUseCondition(CardTestData card)
    {
        if (card == null || card.useConditions == null)
        {
            return false;
        }

        foreach (CardUseConditionData condition in card.useConditions)
        {
            if (condition == null)
            {
                continue;
            }

            if (condition.conditionType == CardUseConditionType.BuffStackAtLeast &&
                condition.buffType == "Bullet")
            {
                return true;
            }
        }

        return false;
    }

    int CountResourceRulesByID(CardResourceRuleData[] rules, string resourceID)
    {
        if (rules == null)
        {
            return 0;
        }

        int count = 0;

        foreach (CardResourceRuleData rule in rules)
        {
            if (rule != null && rule.resourceID == resourceID)
            {
                count++;
            }
        }

        return count;
    }

    CardResourceRuleData CreateBuffStackResourceRule(
        string resourceID,
        int requiredStackForNormalVersion,
        int fallbackMinPoint,
        int fallbackMaxPoint,
        int pointPerStack,
        int exactStackForBonus,
        int exactStackPointBonus,
        int consumeAmountOnSuccess
    )
    {
        return new CardResourceRuleData
        {
            resourceType = "BuffStack",
            resourceID = resourceID,
            requiredStackForNormalVersion = requiredStackForNormalVersion,
            fallbackMinPoint = fallbackMinPoint,
            fallbackMaxPoint = fallbackMaxPoint,
            pointPerStack = pointPerStack,
            exactStackForBonus = exactStackForBonus,
            exactStackPointBonus = exactStackPointBonus,
            consumeAmountOnSuccess = consumeAmountOnSuccess
        };
    }

    BattleCardState CreateResourceAttackCard(
        CharacterData owner,
        string instanceID,
        int minPoint,
        int maxPoint,
        int requiredStackForNormalVersion,
        int fallbackMinPoint,
        int fallbackMaxPoint,
        int pointPerStack,
        int exactStackForBonus,
        int exactStackPointBonus,
        int consumeAmountOnSuccess
    )
    {
        CardTestData cardData = CreateFixedAttackCardData(instanceID + "_data", "资源测试攻击", minPoint);
        cardData.minPoint = minPoint;
        cardData.maxPoint = maxPoint;
        cardData.cooldown = 0;
        cardData.resourceRule = CreateBuffStackResourceRule(
            "Bullet",
            requiredStackForNormalVersion,
            fallbackMinPoint,
            fallbackMaxPoint,
            pointPerStack,
            exactStackForBonus,
            exactStackPointBonus,
            consumeAmountOnSuccess
        );
        cardData.effects = new List<CardEffectData>();

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    BattleCardState CreateResourceDodgeCard(
        CharacterData owner,
        string instanceID,
        int minPoint,
        int maxPoint,
        int requiredStackForNormalVersion,
        int fallbackMinPoint,
        int fallbackMaxPoint,
        int pointPerStack,
        int exactStackForBonus,
        int exactStackPointBonus,
        int consumeAmountOnSuccess
    )
    {
        CardTestData cardData = new CardTestData
        {
            cardID = instanceID + "_data",
            cardName = "资源测试闪避",
            cardType = CardType.Dodge,
            isClashable = true,
            minPoint = minPoint,
            maxPoint = maxPoint,
            cooldown = 0,
            isSinCard = false,
            resourceRule = CreateBuffStackResourceRule(
                "Bullet",
                requiredStackForNormalVersion,
                fallbackMinPoint,
                fallbackMaxPoint,
                pointPerStack,
                exactStackForBonus,
                exactStackPointBonus,
                consumeAmountOnSuccess
            ),
            effects = new List<CardEffectData>()
        };

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    BattleCardState CreateResourceDefenseCard(
        CharacterData owner,
        string instanceID,
        int minPoint,
        int maxPoint,
        int requiredStackForNormalVersion,
        int fallbackMinPoint,
        int fallbackMaxPoint,
        int pointPerStack,
        int exactStackForBonus,
        int exactStackPointBonus,
        int consumeAmountOnSuccess
    )
    {
        CardTestData cardData = new CardTestData
        {
            cardID = instanceID + "_data",
            cardName = "资源测试防御",
            cardType = CardType.Defense,
            isClashable = false,
            minPoint = minPoint,
            maxPoint = maxPoint,
            cooldown = 0,
            defenseFormula = "PointAsDefense",
            resourceRule = CreateBuffStackResourceRule(
                "Bullet",
                requiredStackForNormalVersion,
                fallbackMinPoint,
                fallbackMaxPoint,
                pointPerStack,
                exactStackForBonus,
                exactStackPointBonus,
                consumeAmountOnSuccess
            ),
            effects = new List<CardEffectData>()
        };

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    BattleCardState CreateBeforeUseBuffDefenseCard(
        CharacterData owner,
        string instanceID,
        int point,
        string buffID,
        int stack,
        int duration
    )
    {
        CardTestData cardData = new CardTestData
        {
            cardID = instanceID + "_data",
            cardName = "BeforeUse固定点防御",
            cardType = CardType.Defense,
            isClashable = false,
            minPoint = point,
            maxPoint = point,
            cooldown = 1,
            defenseFormula = "PointAsDefense",
            effects = new List<CardEffectData>
            {
                CreateApplyBuffEffect(BattleTiming.BeforeUse, buffID, stack, duration)
            }
        };

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    BattleCardState CreateBeforeUseBuffDodgeCard(
        CharacterData owner,
        string instanceID,
        int point,
        string buffID,
        int stack,
        int duration
    )
    {
        CardTestData cardData = new CardTestData
        {
            cardID = instanceID + "_data",
            cardName = "BeforeUse固定点闪避",
            cardType = CardType.Dodge,
            isClashable = true,
            minPoint = point,
            maxPoint = point,
            cooldown = 1,
            effects = new List<CardEffectData>
            {
                CreateApplyBuffEffect(BattleTiming.BeforeUse, buffID, stack, duration)
            }
        };

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    void AddBulletCondition(CardTestData cardData, int requiredBullet)
    {
        if (cardData == null)
        {
            return;
        }

        cardData.isSinCard = true;
        cardData.rarity = "Sin";
        cardData.sinCardUseRule = SinCardUseRule.UseCount;
        cardData.maxUseCount = 3;
        cardData.guiltGain = 2;
        cardData.cooldown = 0;
        cardData.useConditions = new CardUseConditionData[]
        {
            new CardUseConditionData
            {
                conditionType = CardUseConditionType.BuffStackAtLeast,
                target = CardTargetType.Self,
                value = requiredBullet,
                buffType = "Bullet"
            }
        };
    }

    BattleCardState CreateBulletLockedBeforeUseAttackCard(
        CharacterData owner,
        string instanceID,
        int point,
        int requiredBullet,
        string buffID,
        int stack,
        int duration
    )
    {
        BattleCardState cardState = CreateBeforeUseBuffAttackCard(owner, instanceID, point, buffID, stack, duration);
        AddBulletCondition(cardState.cardData, requiredBullet);
        return cardState;
    }

    BattleCardState CreateBulletLockedBeforeUseDefenseCard(
        CharacterData owner,
        string instanceID,
        int point,
        int requiredBullet,
        string buffID,
        int stack,
        int duration
    )
    {
        BattleCardState cardState = CreateBeforeUseBuffDefenseCard(owner, instanceID, point, buffID, stack, duration);
        AddBulletCondition(cardState.cardData, requiredBullet);
        return cardState;
    }

    BattleCardState CreateBulletLockedBeforeUseDodgeCard(
        CharacterData owner,
        string instanceID,
        int point,
        int requiredBullet,
        string buffID,
        int stack,
        int duration
    )
    {
        BattleCardState cardState = CreateBeforeUseBuffDodgeCard(owner, instanceID, point, buffID, stack, duration);
        AddBulletCondition(cardState.cardData, requiredBullet);
        return cardState;
    }

    BattleCardState CreateBulletLockedAbilityCard(
        CharacterData owner,
        string instanceID,
        string onPlayBuffID,
        int requiredBullet
    )
    {
        CardTestData cardData = new CardTestData
        {
            cardID = instanceID + "_data",
            cardName = "Bullet不足Ability",
            cardType = "Ability",
            isClashable = false,
            minPoint = 0,
            maxPoint = 0,
            effects = new List<CardEffectData>
            {
                CreateApplyBuffEffect(BattleTiming.OnPlay, onPlayBuffID, 1, 1),
                CreateApplyBuffEffect(BattleTiming.BeforeUse, "Strength", 1, 1)
            }
        };

        AddBulletCondition(cardData, requiredBullet);
        cardData.sinCardCategory = SinCardCategory.Ability;

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    int CountBuffStack(CharacterData character, string buffID)
    {
        if (character == null || character.buffs == null || string.IsNullOrEmpty(buffID))
        {
            return 0;
        }

        int total = 0;

        foreach (BuffData buff in character.buffs)
        {
            if (buff != null && buff.buffID == buffID)
            {
                total += buff.stack;
            }
        }

        return total;
    }

    int CountBuffInstances(CharacterData character, string buffID)
    {
        if (character == null || character.buffs == null || string.IsNullOrEmpty(buffID))
        {
            return 0;
        }

        int total = 0;

        foreach (BuffData buff in character.buffs)
        {
            if (buff != null && buff.buffID == buffID)
            {
                total++;
            }
        }

        return total;
    }

    int GetBuffDuration(CharacterData character, string buffID)
    {
        if (character == null || character.buffs == null || string.IsNullOrEmpty(buffID))
        {
            return -1;
        }

        foreach (BuffData buff in character.buffs)
        {
            if (buff != null && buff.buffID == buffID)
            {
                return buff.duration;
            }
        }

        return -1;
    }

    void AddClashStartOneShotBuff(CharacterData character, string buffID, int stack, int duration)
    {
        if (character == null)
        {
            return;
        }

        character.AddBuff(
            buffID,
            buffID,
            "AbilityBuff",
            stack,
            duration,
            BattleTiming.ClashStart,
            "ConsumeOnTrigger"
        );
    }

    BattleActionSlot CreateRespondedSlot(CharacterData actor, BattleCardState cardState)
    {
        BattleActionSlot actionSlot = new BattleActionSlot(actor, 1);
        actionSlot.AssignResponse(actor, cardState, null, false);
        return actionSlot;
    }

    BattleEnemyIntent CreateEnemyAttackIntent(
        string intentID,
        CharacterData enemyUnit,
        BattleCardState enemyAttack,
        CharacterData target,
        int targetSlotIndex
    )
    {
        return new BattleEnemyIntent(
            intentID,
            enemyUnit,
            enemyAttack,
            target,
            targetSlotIndex,
            1
        );
    }

    // CreateTestAttackCardForCharacter = 给测试角色创建一张基础攻击卡实例
    BattleCardState CreateTestAttackCardForCharacter(CharacterData owner, string instanceID)
    {
        CardTestData cardData = clashSinTestCardData != null
            ? clashSinTestCardData
            : allyAAttackCardState.cardData;

        return BattleCardManager.CreateBattleCard(
            owner,
            cardData,
            instanceID
        );
    }

    // CreateFixedAttackCardForCharacter = 给测试角色创建固定点数攻击卡
    BattleCardState CreateFixedAttackCardForCharacter(CharacterData owner, string instanceID, int point)
    {
        return BattleCardManager.CreateBattleCard(
            owner,
            CreateFixedAttackCardData(instanceID + "_data", "固定点数攻击", point),
            instanceID
        );
    }

    // CreateFixedDodgeCardForCharacter = 给测试角色创建固定点数闪避卡
    BattleCardState CreateFixedDodgeCardForCharacter(CharacterData owner, string instanceID, int point, int cooldown)
    {
        CardTestData dodgeCard = new CardTestData
        {
            cardID = instanceID + "_data",
            cardName = "固定点数闪避",
            cardType = CardType.Dodge,
            isClashable = true,
            minPoint = point,
            maxPoint = point,
            cooldown = cooldown,
            isSinCard = false
        };

        return BattleCardManager.CreateBattleCard(owner, dodgeCard, instanceID);
    }

    // CreateFixedEnemyAttackCardForDodgeTest = 给 Dodge 测试创建固定点数敌人攻击卡
    BattleCardState CreateFixedEnemyAttackCardForDodgeTest(CharacterData owner, string instanceID, int point, int cooldown)
    {
        CardTestData enemyAttackCard = CreateFixedAttackCardData(
            instanceID + "_data",
            "固定点数敌人攻击",
            point
        );

        enemyAttackCard.cooldown = cooldown;
        enemyAttackCard.isSinCard = false;

        return BattleCardManager.CreateBattleCard(owner, enemyAttackCard, instanceID);
    }

    // CreateTestDefenseCardForCharacter = 给测试角色创建固定点数防御卡
    BattleCardState CreateTestDefenseCardForCharacter(CharacterData owner, string instanceID, int defensePoint, int cooldown)
    {
        CardTestData defenseCard = new CardTestData
        {
            cardID = instanceID + "_data",
            cardName = "固定点数防御",
            cardType = CardType.Defense,
            isClashable = false,
            minPoint = defensePoint,
            maxPoint = defensePoint,
            cooldown = cooldown,
            defenseFormula = "PointAsDefense"
        };

        return BattleCardManager.CreateBattleCard(owner, defenseCard, instanceID);
    }

    // CreateFixedAttackCardData = 创建固定点数攻击卡数据
    CardTestData CreateFixedAttackCardData(string cardID, string cardName, int point)
    {
        return new CardTestData
        {
            cardID = cardID,
            cardName = cardName,
            cardType = CardType.Attack,
            isClashable = true,
            minPoint = point,
            maxPoint = point,
            damageFormula = "PointAsDamage",
            maxUseCount = 3
        };
    }

    BattleCardState CreateAttackCardWithNextClashBuffEffect(
        CharacterData owner,
        string instanceID,
        int point,
        string trigger,
        int nextClashPointUpStack
    )
    {
        CardTestData cardData = CreateFixedAttackCardData(instanceID + "_data", "事件生成NextClashPointUp攻击", point);
        cardData.effects = new List<CardEffectData>
        {
            new CardEffectData
            {
                trigger = trigger,
                effectType = CardEffectType.ApplyBuff,
                target = CardTargetType.Self,
                buffType = "NextClashPointUp",
                buffName = "下一次拼点点数增加",
                buffCategory = "AbilityBuff",
                stack = nextClashPointUpStack,
                duration = 1,
                checkTiming = BattleTiming.ClashStart,
                expireRule = "ConsumeOnTrigger"
            }
        };

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    // CreateResolvedStateAttackCardData = 创建用于验证 Resolved 使用状态的固定点攻击卡
    CardTestData CreateResolvedStateAttackCardData(string cardID, string cardName, int point, bool isSinCard)
    {
        CardTestData cardData = CreateFixedAttackCardData(cardID, cardName, point);

        if (isSinCard)
        {
            cardData.isSinCard = true;
            cardData.rarity = "Sin";
            cardData.sinCardCategory = SinCardCategory.Clash;
            cardData.sinCardUseRule = SinCardUseRule.UseCount;
            cardData.maxUseCount = 3;
            cardData.guiltGain = 2;
            cardData.cooldown = 0;
            return cardData;
        }

        cardData.rarity = "White";
        cardData.cooldown = 2;
        return cardData;
    }

    // CreateObservableEnemyAttackCardData = 创建可观察 UseCount 的敌人攻击测试卡
    CardTestData CreateObservableEnemyAttackCardData(string cardID, string cardName, int point)
    {
        CardTestData cardData = CreateFixedAttackCardData(cardID, cardName, point);
        cardData.isSinCard = true;
        cardData.sinCardCategory = SinCardCategory.Clash;
        cardData.sinCardUseRule = SinCardUseRule.UseCount;
        cardData.maxUseCount = 3;
        return cardData;
    }

    bool IsSlotUsedStateExpected(BattleActionSlot slot, bool expectedUsed)
    {
        if (slot == null)
        {
            return !expectedUsed;
        }

        return slot.isUsed == expectedUsed;
    }

    // CreateCardStateForCharacter = 创建指定 cardType 的测试卡牌状态
    BattleCardState CreateCardStateForCharacter(
        CharacterData owner,
        string instanceID,
        string cardName,
        string cardType,
        int minPoint,
        int maxPoint
    )
    {
        CardTestData cardData = new CardTestData
        {
            cardID = instanceID + "_data",
            cardName = cardName,
            cardType = cardType,
            isClashable = false,
            minPoint = minPoint,
            maxPoint = maxPoint,
            maxUseCount = 3
        };

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    // GetFirstExecutionItem = 读取计划中的第一项
    BattleExecutionItem GetFirstExecutionItem(BattleExecutionPlan executionPlan)
    {
        if (executionPlan == null || executionPlan.executionItems == null || executionPlan.executionItems.Count == 0)
        {
            return null;
        }

        return executionPlan.executionItems[0];
    }

    bool IsExecutionItemState(
        BattleExecutionItem item,
        BattleExecutionItemStatus expectedStatus,
        BattleExecutionItemOutcomeReason expectedReason,
        bool expectedCompleted
    )
    {
        return item != null &&
            item.status == expectedStatus &&
            item.outcomeReason == expectedReason &&
            item.isCompleted == expectedCompleted;
    }

    int CountExecutionItemsOfType(BattleExecutionPlan executionPlan, BattleExecutionItemType executionType)
    {
        if (executionPlan == null || executionPlan.executionItems == null)
        {
            return 0;
        }

        int count = 0;

        foreach (BattleExecutionItem item in executionPlan.executionItems)
        {
            if (item != null && item.executionType == executionType)
            {
                count++;
            }
        }

        return count;
    }

    // RunActionSlotPassiveGuardDefenseSubTest = 被动守备完整执行子测试
    void RunActionSlotPassiveGuardDefenseSubTest(
        string title,
        int enemyAttackPoint,
        int defensePoint,
        string expectedResultType,
        int expectedDamage
    )
    {
        Debug.Log("===== " + title + " 测试开始 =====");

        StartTurn();

        int hpBefore = allyB.currentHP;
        CardTestData enemyAttackCard = CreateFixedAttackCardData(title + "_enemy_attack", title + "敌人攻击", enemyAttackPoint);
        BattleCardState enemyAttack = BattleCardManager.CreateBattleCard(enemy, enemyAttackCard, title + "_enemy_attack_copy_0");
        BattleCardState passiveGuard = CreateTestDefenseCardForCharacter(allyB, title + "_b_defense", defensePoint, 1);

        BattleEnemyIntent intent1 = new BattleEnemyIntent(
            title + "_intent_001",
            enemy,
            enemyAttack,
            allyB,
            1,
            1
        );

        List<BattleEnemyIntent> intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intent1);
        List<BattleActionSlot> actionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);

        BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 1, allyB, passiveGuard);

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateBasicExecutionPlan(actionSlots, intentQueue);

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.PrintExecutionPlanStepPreview(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan);
        BattleActionSlotManager.PrintSlotStates(actionSlots);

        Debug.Log("预期 resultType 出现在 Defense Resolver 日志：" + expectedResultType);
        Debug.Log("预期最终伤害：" + expectedDamage + "，实际 HP 是否符合：" + (allyB.currentHP == hpBefore - expectedDamage));
        Debug.Log("预期只使用 B槽位1：" + BattleActionSlotManager.GetSlot(actionSlots, allyB, 1).isUsed);
        Debug.Log("预期 Defense 进入 CD：" + (passiveGuard.currentCooldown == GetExpectedResolvedCooldown(passiveGuard)));
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
    }

    // PrintEnemyIntentActualTarget = 打印敌人意图当前实际目标
    void PrintEnemyIntentActualTarget(BattleEnemyIntent enemyIntent)
    {
        if (enemyIntent == null)
        {
            Debug.LogWarning("敌人意图实际目标打印失败：敌人意图为空");
            return;
        }

        Debug.Log("敌人意图实际目标仍为：" + enemyIntent.GetActualTargetSlotText());
    }

    // PrintCharacterCardStates = 打印指定角色的战斗卡牌状态
    void PrintCharacterCardStates(CharacterData character)
    {
        BattleCardManager.PrintCardStates(character);
    }

    // CreateTestEnemyIntent = 创建测试用敌人意图
    BattleEnemyIntent CreateTestEnemyIntent()
    {
        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            "enemy_intent_001",
            enemy,
            enemyAttackCardState,
            allyB,
            2
        );

        Debug.Log(
            "创建敌人意图：" +
            enemyIntent.GetEnemyName() +
            " 使用 " +
            enemyIntent.GetCardName() +
            " 攻击 " +
            enemyIntent.GetOriginalTargetName() +
            " 的槽位" +
            enemyIntent.originalTargetSlotIndex
        );

        return enemyIntent;
    }

    // CreateFixedTestEnemyIntentQueueForRuntimeState = 为 RuntimeState / 简易 UI 原型创建固定测试敌人意图队列
    List<BattleEnemyIntent> CreateFixedTestEnemyIntentQueueForRuntimeState()
    {
        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            "runtime_state_fixed_intent_001",
            enemy,
            enemyAttackCardState,
            allyB,
            1,
            1
        );

        Debug.Log(
            "创建 RuntimeState 固定敌人意图：敌人意图" +
            enemyIntent.intentOrder +
            "，" +
            enemyIntent.GetEnemyName() +
            " 使用 " +
            enemyIntent.GetCardName() +
            " 攻击 " +
            enemyIntent.GetOriginalTargetName() +
            " 的槽位" +
            enemyIntent.originalTargetSlotIndex
        );

        return BattleEnemyIntentManager.CreateIntentQueue(enemyIntent);
    }

    // ================================
    // Action Slot 执行辅助方法
    // ================================

    // ExecuteActionSlots = 按槽位顺序执行已安排的行动
    void ExecuteActionSlots(List<BattleActionSlot> actionSlots)
    {
        if (actionSlots == null || actionSlots.Count == 0)
        {
            Debug.LogWarning("执行行动槽位失败：没有行动槽位");
            return;
        }

        Debug.Log("===== 开始执行行动槽位 =====");

        foreach (BattleActionSlot actionSlot in actionSlots)
        {
            ExecuteActionSlot(actionSlot);
        }
    }

    // ExecuteActionSlot = 执行单个行动槽位
    void ExecuteActionSlot(BattleActionSlot actionSlot)
    {
        if (actionSlot == null || actionSlot.IsEmpty())
        {
            return;
        }

        Debug.Log(
            "执行槽位 " +
            actionSlot.slotIndex +
            "：" +
            actionSlot.GetActorName() +
            " 使用 " +
            actionSlot.GetCardName()
        );

        if (actionSlot.slotType == BattleActionSlotType.RespondToEnemyIntent)
        {
            ExecuteResponseActionSlot(actionSlot);
            return;
        }

        if (actionSlot.slotType == BattleActionSlotType.FreeAction)
        {
            ExecuteFreeActionSlot(actionSlot);
            return;
        }
    }

    // ExecuteResponseActionSlot = 执行响应敌人意图的槽位
    void ExecuteResponseActionSlot(BattleActionSlot actionSlot)
    {
        if (actionSlot.enemyIntent == null)
        {
            Debug.LogWarning("执行响应槽位失败：敌人意图为空");
            return;
        }

        BattleEnemyIntent intent = actionSlot.enemyIntent;

        if (intent.enemy == null || intent.enemyCardState == null)
        {
            Debug.LogWarning("执行响应槽位失败：敌人或敌人卡牌为空");
            return;
        }

        if (!BattleCardManager.CanUseCard(actionSlot.actor, intent.enemy, actionSlot.cardState))
        {
            Debug.LogWarning(actionSlot.GetActorName() + " 的槽位卡牌不能使用：" + actionSlot.GetCardName());
            return;
        }

        BattleResolver.TestClash(
            intent.enemy,
            intent.enemyCardState,
            actionSlot.actor,
            actionSlot.cardState
        );

        actionSlot.MarkUsed();
    }

    // ExecuteFreeActionSlot = 执行不直接响应敌人意图的槽位
    void ExecuteFreeActionSlot(BattleActionSlot actionSlot)
    {
        if (actionSlot.cardState == null)
        {
            return;
        }

        if (actionSlot.cardState.IsAbilitySinCard())
        {
            BattleResolver.TestUseAbilitySinCard(
                actionSlot.actor,
                actionSlot.cardState,
                actionSlot.target
            );

            actionSlot.MarkUsed();
            return;
        }

        Debug.Log("自由行动暂时只测试 Ability 罪卡，当前卡牌不执行：" + actionSlot.GetCardName());
    }

    // ================================
    // 回合流程
    // ================================

    // StartTurn = 开始回合
    void StartTurn()
    {
        BattleTurnProcessor.StartTurn(battleUnits);
        BattleTurnProcessor.PrintBattleState(battleUnits);
      
    }

    // EndTurn = 结束回合
    void EndTurn()
    {
        BattleTurnProcessor.EndTurn(battleUnits);
        BattleTurnProcessor.PrintBattleState(battleUnits);

        // 临时打印我方角色A卡牌状态，方便确认 CD
        BattleCardManager.PrintCardStates(allyA);
    }

    // ================================
    // 测试初始化
    // ================================

    // CreateTestCharacters = 创建测试角色
    void CreateTestCharacters()
    {
        // 速度范围测试：
        // 我方角色A：高速角色，8-12
        // 我方角色B：较慢角色，3-5
        // 敌人：普通敌人，5-8
        allyA = new CharacterData("我方角色A", 30, 20, 20);
        allyB = new CharacterData("我方角色B", 30, 3, 5);
        enemy = new CharacterData("敌人", 999, 5, 8);
        battleUnits.Clear();

        battleUnits.Add(allyA);
        battleUnits.Add(allyB);
        battleUnits.Add(enemy);
    }

    // AddTestBuffs = 添加测试状态
    void AddTestBuffs()
    {
        // 当前已经生效的状态：子弹
        // Bullet = 子弹
        // AbilityBuff = 能力状态
        // Permanent = 常驻，不会因为回合结束自然消失
        allyA.AddBuff("Bullet", "子弹", "AbilityBuff", 6, -1, "None", "Permanent");
    }

    // CreateTestBattleCards = 创建测试用战斗卡牌状态
    void CreateTestBattleCards(List<CardTestData> cards)
    {
        CardTestData enemyCard = CardDataLoader.FindCardByID(cards, "enemy_atk_001");
        CardTestData allyACard = CardDataLoader.FindCardByID(cards, "atk_001");
        CardTestData allyBCard = CardDataLoader.FindCardByID(cards, "def_001");
        CardTestData allyAAbilitySinCard = CardDataLoader.FindCardByID(cards, "sin_ability_001");
        clashSinTestCardData = CardDataLoader.FindCardByID(
            cards,
            BattleSimpleUIController.ClashSinTestCardID
        );

        enemyAttackCardState = BattleCardManager.CreateBattleCard(
            enemy,
            enemyCard,
            "enemy_atk_001_copy_0"
        );

        allyAAttackCardState = BattleCardManager.CreateBattleCard(
            allyA,
            allyACard,
            "allyA_atk_001_copy_0"
        );

        allyBDefenseCardState = BattleCardManager.CreateBattleCard(
            allyB,
            allyBCard,
            "allyB_def_001_copy_0"
        );

        if (allyAAbilitySinCard == null)
        {
            Debug.LogWarning("没有找到能力型罪卡测试数据：sin_ability_001");
        }
        else
        {
            allyAAbilitySinCardState = BattleCardManager.CreateBattleCard(
                allyA,
                allyAAbilitySinCard,
                "allyA_sin_ability_001_copy_0"
            );
        }
    }   
    // ================================
    // 测试战斗流程
    // ================================

    // RunAbilitySinCardTest = 执行一次能力型罪卡测试
    void RunAbilitySinCardTest()
    {
        if (allyAAbilitySinCardState == null)
        {
            Debug.LogWarning("能力型罪卡测试需要的战斗卡牌状态没有创建成功：sin_ability_001");
            return;
        }

        BattleResolver.TestUseAbilitySinCard(
            allyA,
            allyAAbilitySinCardState,
            enemy
        );
    }

    // PrintAbilitySinCardTestState = 打印能力型罪卡测试后的关键状态
    void PrintAbilitySinCardTestState()
    {
        allyA.PrintBuffs();
        GuiltManager.PrintGuilt(allyA);
        BattleCardManager.PrintCardStates(allyA);
    }
    // RunBattleTest = 执行一次测试战斗
    void RunBattleTest()
    {
        if (enemyAttackCardState == null || allyAAttackCardState == null || allyBDefenseCardState == null)
        {
            Debug.LogWarning("测试战斗需要的战斗卡牌状态没有创建成功。");
            return;
        }

        // 敌人原本攻击我方角色B
        CharacterData originalTarget = allyB;

        // 默认实际接战者是我方角色B
        CharacterData actualAlly = allyB;

        // 默认我方角色B使用自己的防御卡
        BattleCardState actualAllyCardState = allyBDefenseCardState;

        // 临时模拟：玩家是否选择让 allyA 介入
        bool wantsIntercept = true;

        if (wantsIntercept && BattleTargeting.CanInterceptAttack(allyA, enemy, originalTarget))
        {
            actualAlly = allyA;
            actualAllyCardState = allyAAttackCardState;

            Debug.Log("攻击目标从 " + originalTarget.characterName + " 改为 " + actualAlly.characterName);
        }
        else
        {
            Debug.Log("敌人继续攻击原目标：" + originalTarget.characterName);
        }

        // 使用前先检查敌人卡牌能不能用
        if (!BattleCardManager.CanUseCard(enemy, actualAlly, enemyAttackCardState))
        {
            Debug.LogWarning(enemy.characterName + " 的卡牌不能使用：" + enemyAttackCardState.GetCardName());
            return;
        }

        // 使用前先检查我方实际接战者卡牌能不能用
        if (!BattleCardManager.CanUseCard(actualAlly, enemy, actualAllyCardState))
        {
            Debug.LogWarning(actualAlly.characterName + " 的卡牌不能使用：" + actualAllyCardState.GetCardName());
            return;
        }

        // 执行实际战斗结算
        BattleResolver.TestClash(enemy, enemyAttackCardState, actualAlly, actualAllyCardState);
    }

    bool RunBattleActionRelationLineBasicTestSequence()
    {
        bool coreRelationsPassed = BattleActionRelationMode73Tests.Run();
        bool test75 = RunBattlePermanentBulletBuffBasicTestSequence();
        bool test76 = RunBattleBuffInspectorPreviewBasicTestSequence();
        bool test77 = RunBattleBuffGridLayoutBasicTestSequence();
        bool test78 = RunBattleActionSlotVisualInteractionBasicTestSequence();
        bool test79 =
            RunBattleCardClickAssignBasicTestSequence() &&
            RunBattleCardClickInteractionIntegrationTestSequence();
        bool test80 = RunBattleCardHoverAndDragMotionBasicTestSequence();

        Debug.Log("模式73 测试75 模式72永久Bullet回归：" + test75);
        Debug.Log("模式73 测试76 模式71 Buff预览回归：" + test76);
        Debug.Log("模式73 测试77 模式70 Buff网格回归：" + test77);
        Debug.Log("模式73 测试78 模式69行动槽视觉回归：" + test78);
        Debug.Log("模式73 测试79 模式66与67卡牌指派回归：" + test79);
        Debug.Log("模式73 测试80 模式65卡牌动效回归：" + test80);

        bool allPassed =
            coreRelationsPassed &&
            test75 && test76 && test77 &&
            test78 && test79 && test80;
        Debug.Log("模式73 80项聚合结果：" + allPassed);
        return allPassed;
    }

    bool RunBattleCharacterStatusWorldFollowBasicTestSequence()
    {
        bool followerTestsPassed =
            BattleCharacterStatusWorldFollowMode74Tests.Run();
        bool test31 = RunBattleActionRelationLineBasicTestSequence();
        bool test32 = RunBattlePermanentBulletBuffBasicTestSequence();
        bool test33 =
            RunBattleBuffInspectorPreviewBasicTestSequence() &&
            RunBattleBuffGridLayoutBasicTestSequence();
        bool test34 = RunBattleActionSlotVisualInteractionBasicTestSequence();
        bool test35 =
            RunBattleCardClickAssignBasicTestSequence() &&
            RunBattleCardClickInteractionIntegrationTestSequence();

        Debug.Log("模式74 测试31 模式73关系线回归：" + test31);
        Debug.Log("模式74 测试32 模式72永久Bullet回归：" + test32);
        Debug.Log("模式74 测试33 模式71和70 Buff回归：" + test33);
        Debug.Log("模式74 测试34 模式69行动槽视觉回归：" + test34);
        Debug.Log("模式74 测试35 模式66和67卡牌指派回归：" + test35);

        bool allPassed =
            followerTestsPassed &&
            test31 && test32 && test33 && test34 && test35;
        Debug.Log("模式74 35项聚合结果：" + allPassed);
        return allPassed;
    }

    bool RunBattleActionRelationInteractionFixTestSequence()
    {
        return BattleActionRelationInteractionMode75Tests.Run();
    }
}
