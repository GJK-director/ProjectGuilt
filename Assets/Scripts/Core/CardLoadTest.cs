// 脚本中文说明：卡牌读取和战斗测试入口。负责在 Unity 场景启动时创建测试角色、读取卡牌并运行指定测试流程。
using System.Collections.Generic;
using UnityEngine;

public enum BattleTestMode
{
    BattleActionSlotOwnerBasic,
    BattleActionSlotOwnerAssignBasic,
    BattleRuntimeStateEndCurrentTurnBasic,
    BattleRuntimeStatePrepareNextTurnBasic,
    BattleStateViewDataEnemyIntentBasic,
    BattleStateViewDataActionSlotBasic,
    BattleStateViewDataOwnerActionSlotBasic,
    BattleResolverResolveRespondedAttackVsAttackBasic,
    BattleResolverResolveRespondedDefenseFullBlockBasic,
    BattleResolverResolveRespondedDefenseReducedDamageBasic,
    BattleResolverDefenseKnownEnemyPointBasic,
    ActionSlotLowSpeedOriginalSlotResponseBasic,
    ActionSlotLowSpeedIllegalResponseFail,
    ActionSlotResponseOverwriteBasic,
    ActionSlotExecutionPlanSpeedHighResponseOrderBasic,
    ActionSlotExecutionPlanSpeedLowResponseOrderBasic,
    ActionSlotExecutionPlanExecuteFreeAbilityBasic,
    ActionSlotExecutionPlanExecuteHighSpeedFreeAttackMixedBasic,
    ActionSlotExecutionPlanExecuteLowSpeedFreeAttackMixedBasic,
    ActionSlotExecutionPlanExecuteUnrespondedBasic,
    ActionSlotPassiveGuardCandidateOrderBasic,
    ActionSlotPassiveGuardSkipInvalidCandidateBasic,
    ActionSlotPassiveGuardFullBlockBasic,
    ActionSlotPassiveGuardReducedDamageBasic,
    ActionSlotPassiveGuardTargetMismatchBasic,
    ActionSlotPassiveGuardRespondedIntentNotTriggeredBasic,
    ActionSlotPassiveGuardAssignLegalityBasic,
    ActionSlotExecutionPlanExecuteRespondedBasic,
    ActionSlotExecutionPlanExecuteRespondedEnemyWin,
    ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardReducedDamageBasic,
    ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardFullBlockBasic,
    ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardCandidateOrderBasic,
    ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardSkipInvalidBasic,
    ActionSlotExecutionPlanExecuteRespondedEnemyWinNoPassiveGuardBasic,
    ActionSlotExecutionPlanExecuteRespondedPlayerWinPassiveGuardNotTriggeredBasic,
    ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardTargetMismatchBasic,
    ActionSlotExecutionPlanExecuteRespondedTieLimit,
    ActionSlotExecutionPlanExecuteMixedBasic
}

public class CardLoadTest : MonoBehaviour
{
    [SerializeField] private BattleTestMode testMode = BattleTestMode.BattleActionSlotOwnerBasic;

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

        if (testMode == BattleTestMode.BattleActionSlotOwnerBasic)
        {
            RunBattleActionSlotOwnerBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleActionSlotOwnerAssignBasic)
        {
            RunBattleActionSlotOwnerAssignBasicTestSequence();
            return;
        }

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

        if (testMode == BattleTestMode.BattleStateViewDataEnemyIntentBasic)
        {
            RunBattleStateViewDataEnemyIntentBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleStateViewDataActionSlotBasic)
        {
            RunBattleStateViewDataActionSlotBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleStateViewDataOwnerActionSlotBasic)
        {
            RunBattleStateViewDataOwnerActionSlotBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.BattleResolverResolveRespondedAttackVsAttackBasic)
        {
            RunBattleResolverResolveRespondedAttackVsAttackBasicTestSequence();
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

        if (testMode == BattleTestMode.ActionSlotLowSpeedOriginalSlotResponseBasic)
        {
            RunActionSlotLowSpeedOriginalSlotResponseBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotLowSpeedIllegalResponseFail)
        {
            RunActionSlotLowSpeedIllegalResponseFailTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotResponseOverwriteBasic)
        {
            RunActionSlotResponseOverwriteBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanSpeedHighResponseOrderBasic)
        {
            RunActionSlotExecutionPlanSpeedHighResponseOrderBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanSpeedLowResponseOrderBasic)
        {
            RunActionSlotExecutionPlanSpeedLowResponseOrderBasicTestSequence();
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

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteLowSpeedFreeAttackMixedBasic)
        {
            RunActionSlotExecutionPlanExecuteLowSpeedFreeAttackMixedBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteUnrespondedBasic)
        {
            RunActionSlotExecutionPlanExecuteUnrespondedBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotPassiveGuardCandidateOrderBasic)
        {
            RunActionSlotPassiveGuardCandidateOrderBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotPassiveGuardSkipInvalidCandidateBasic)
        {
            RunActionSlotPassiveGuardSkipInvalidCandidateBasicTestSequence();
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

        if (testMode == BattleTestMode.ActionSlotPassiveGuardTargetMismatchBasic)
        {
            RunActionSlotPassiveGuardTargetMismatchBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotPassiveGuardRespondedIntentNotTriggeredBasic)
        {
            RunActionSlotPassiveGuardRespondedIntentNotTriggeredBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotPassiveGuardAssignLegalityBasic)
        {
            RunActionSlotPassiveGuardAssignLegalityBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteRespondedBasic)
        {
            RunActionSlotExecutionPlanExecuteRespondedBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteRespondedEnemyWin)
        {
            RunActionSlotExecutionPlanExecuteRespondedEnemyWinTestSequence();
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

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardCandidateOrderBasic)
        {
            RunActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardCandidateOrderBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardSkipInvalidBasic)
        {
            RunActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardSkipInvalidBasicTestSequence();
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

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardTargetMismatchBasic)
        {
            RunActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardTargetMismatchBasicTestSequence();
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
        runtimeState.SetPhase("PlanReady");
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
        runtimeState.SetPhase("PlanReady");

        Debug.Log("===== 清理前 BattleRuntimeState =====");
        runtimeState.PrintRuntimeState();

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
        Debug.Log("预期清理后 currentPhase 为 TurnCleared：" + (runtimeState.currentPhase == "TurnCleared"));
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
        runtimeState.SetPhase("Completed");

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
        runtimeState.SetPhase("TurnEnded");

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
        runtimeState.SetPhase("Prepare");

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
        runtimeState.SetPhase("Prepare");

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
        runtimeState.SetPhase("Prepare");

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
        runtimeState.SetPhase("Prepare");

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
        runtimeState.SetPhase("Prepare");

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
        Debug.Log("预期 triggeredEventChain：False，实际是否符合：" + (result != null && !result.triggeredEventChain));
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
        Debug.Log("预期 Defense CD 进入配置 cooldown：" + (defenseCardState.currentCooldown == defenseCooldown));
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
        Debug.Log("预期 Defense 进入 CD：" + (defenseCardState.currentCooldown == defenseCard.cooldown));
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
        Debug.Log("预期 B槽位1 Defense 进入 CD：" + (guard1.currentCooldown == guard1.cardData.cooldown));
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
        Debug.Log("预期 B槽位2 Defense 进入 CD：" + (guard2.currentCooldown == guard2.cardData.cooldown));
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
        bool dodgeRejected = BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyA, 1, allyA, dodgeCard);
        bool thirdDefenseOnOccupiedSlot = BattleActionSlotManager.AssignPassiveGuard(actionSlots, allyB, 1, allyB, guard3);

        BattleActionSlotManager.PrintSlotStates(actionSlots);

        Debug.Log("预期同角色两个不同槽位可分别安排 Defense PassiveGuard：" + (assignB1 && assignB2));
        Debug.Log("预期同一 BattleCardState 不能重复安排：" + !repeatSameCard);
        Debug.Log("预期 Attack 不能 AssignPassiveGuard：" + !attackRejected);
        Debug.Log("预期 Ability 不能 AssignPassiveGuard：" + !abilityRejected);
        Debug.Log("预期 Dodge 不能 AssignPassiveGuard：" + !dodgeRejected);
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

    // RunActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardReducedDamageBasicTestSequence = 敌人胜利后触发 PassiveGuard 减伤
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
            true,
            false,
            3,
            true,
            "EnemyWinPassiveGuardReducedDamage"
        );
    }

    // RunActionSlotExecutionPlanExecuteRespondedEnemyWinPassiveGuardFullBlockBasicTestSequence = 敌人胜利后触发 PassiveGuard 完全防御
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
            true,
            false,
            0,
            true,
            "EnemyWinPassiveGuardFullBlock"
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
            damageFormula = "PointAsDamage"
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
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
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
        Debug.Log("预期 B HP 变化：" + expectedDamageToAllyB + "，实际是否符合：" + (allyB.currentHP == allyBHPBefore - expectedDamageToAllyB));
        Debug.Log("预期主响应 B槽位1 MarkUsed：" + (responseSlot != null && responseSlot.isUsed));
        Debug.Log("预期 B槽位2 / A槽位2 PassiveGuard 使用状态：" + expectGuardSlot2Used + "，实际是否符合：" + IsSlotUsedStateExpected(guardSlot2After, expectGuardSlot2Used));

        if (shouldCheckGuardSlot3)
        {
            Debug.Log("预期 B槽位3 PassiveGuard 使用状态：" + expectGuardSlot3Used + "，实际是否符合：" + IsSlotUsedStateExpected(guardSlot3After, expectGuardSlot3Used));
        }

        Debug.Log("预期 PassiveGuard 结果：" + expectPassiveGuardResult + "，实际可从 resultType 日志确认：" + expectedResultTypeInLog);
        int expectedEnemyUseCount = playerAttackPoint < enemyAttackPoint ? 1 : 0;
        Debug.Log("预期敌人 UseCount：" + expectedEnemyUseCount + "，实际是否符合：" + (enemyAttack.currentUseCount == expectedEnemyUseCount));
        Debug.Log("预期敌人卡没有被 known-point Defense 第二次完成：" + (enemyAttack.currentUseCount == expectedEnemyUseCount));
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

    // CreateTestAttackCardForCharacter = 给测试角色创建一张基础攻击卡实例
    BattleCardState CreateTestAttackCardForCharacter(CharacterData owner, string instanceID)
    {
        return BattleCardManager.CreateBattleCard(
            owner,
            allyAAttackCardState.cardData,
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
        Debug.Log("预期 Defense 进入 CD：" + (passiveGuard.currentCooldown == passiveGuard.cardData.cooldown));
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
}
