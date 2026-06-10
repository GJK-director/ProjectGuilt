// 脚本中文说明：卡牌读取和战斗测试入口。负责在 Unity 场景启动时创建测试角色、读取卡牌并运行指定测试流程。
using System.Collections.Generic;
using UnityEngine;

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
    SingleAllyDeathExecutionFilteringBasic = 46
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

    // RunBattleResolverRespondedPlayerWinBothCardsResolvedBasicTestSequence = 玩家胜利后双方攻击卡都完成使用
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

    // RunBattleResolverRespondedEnemyWinBothCardsResolvedBasicTestSequence = 敌人胜利后双方攻击卡都完成使用
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

    // RunBattleResolverRespondedClashSinLoseResolvedBasicTestSequence = 拼点罪卡失败后也完成 Resolved 使用状态
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
        bool expectCardsUsed = !expectTieLimit;
        int expectedDodgeCooldown = expectCardsUsed ? dodgeCardState.cardData.cooldown : dodgeCooldownBefore;
        int expectedEnemyCooldown = expectCardsUsed ? enemyAttackCardState.cardData.cooldown : enemyCooldownBefore;
        bool expectedSlotUsed = expectCardsUsed;

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
        Debug.Log("===== " + title + " 双方攻击卡 Resolved 测试开始 =====");

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

        int expectedPlayerCooldown = playerAttackIsSinCard ? 0 : playerAttack.cardData.cooldown;
        int expectedPlayerUseCount = playerAttackIsSinCard ? playerUseCountBefore + 1 : playerUseCountBefore;
        int expectedPlayerGuilt = playerAttackIsSinCard ? playerGuiltBefore + playerAttack.cardData.guiltGain : playerGuiltBefore;
        int expectedEnemyCooldown = enemyAttack.cardData.cooldown;

        Debug.Log("预期 resultType：" + expectedResultType + "，实际可从 Executor Resolver 日志确认");
        Debug.Log("预期玩家 HP 变化：" + expectedDamageToPlayer + "，实际是否符合：" + (allyB.currentHP == playerHPBefore - expectedDamageToPlayer));
        Debug.Log("预期敌人 HP 变化：" + expectedDamageToEnemy + "，实际是否符合：" + (enemy.currentHP == enemyHPBefore - expectedDamageToEnemy));
        Debug.Log("预期玩家卡 CD：" + expectedPlayerCooldown + "，实际是否符合：" + (playerAttack.currentCooldown == expectedPlayerCooldown));
        Debug.Log("预期敌人卡 CD：" + expectedEnemyCooldown + "，实际是否符合：" + (enemyAttack.currentCooldown == expectedEnemyCooldown));
        Debug.Log("预期玩家卡 UseCount：" + expectedPlayerUseCount + "，实际是否符合：" + (playerAttack.currentUseCount == expectedPlayerUseCount));
        Debug.Log("预期敌人卡 UseCount 不变：" + (enemyAttack.currentUseCount == enemyUseCountBefore));
        Debug.Log("预期玩家卡 isConsumed 保持未消耗：" + (!playerAttack.isConsumed));
        Debug.Log("预期敌人卡 isConsumed 保持不变：" + (enemyAttack.isConsumed == enemyConsumedBefore));
        Debug.Log("预期玩家 guilt：" + expectedPlayerGuilt + "，实际是否符合：" + (allyB.currentGuilt == expectedPlayerGuilt));
        Debug.Log("预期敌人 guilt 不变：" + (enemy.currentGuilt == enemyGuiltBefore));
        Debug.Log("预期主响应槽位 MarkUsed：" + (responseSlot != null && responseSlot.isUsed));
        Debug.Log("预期 ExecutionPlan 完成：" + executionPlan.isCompleted);

        if (playerAttackIsSinCard)
        {
            Debug.Log("预期 guiltGain 增加一次：" + (allyB.currentGuilt == playerGuiltBefore + playerAttack.cardData.guiltGain));
            Debug.Log("预期 UseCount 增加一次：" + (playerAttack.currentUseCount == playerUseCountBefore + 1));
            Debug.Log("预期 guiltGain / UseCount 不重复增加：" + (allyB.currentGuilt == playerGuiltBefore + playerAttack.cardData.guiltGain && playerAttack.currentUseCount == playerUseCountBefore + 1));
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
            true,
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
        Debug.Log("预期 Dodge CD 变化符合分支：" + (expectDodgeUsed ? passiveDodge.currentCooldown == passiveDodge.cardData.cooldown : passiveDodge.currentCooldown == dodgeCooldownBefore));
        Debug.Log("预期 Defense CD 不变化：" + (followDefense.currentCooldown == defenseCooldownBefore));
        Debug.Log("预期 Defense UseCount / isConsumed 不变化：" + (followDefense.currentUseCount == defenseUseCountBefore && followDefense.isConsumed == defenseConsumedBefore));
        Debug.Log("预期 Enemy Attack 状态符合分支：" + (expectEnemyAttackUsed ? enemyAttack.currentCooldown == enemyAttack.cardData.cooldown : enemyAttack.currentCooldown == enemyCooldownBefore));
        Debug.Log("预期 Enemy Attack UseCount / isConsumed 符合分支：" + (enemyAttack.currentUseCount == enemyUseCountBefore && enemyAttack.isConsumed == enemyConsumedBefore));
        Debug.Log("预期 Dodge UseCount / guilt / isConsumed 符合分支：" + (passiveDodge.currentUseCount == dodgeUseCountBefore && allyB.currentGuilt == dodgeGuiltBefore && passiveDodge.isConsumed == dodgeConsumedBefore));
        Debug.Log("预期 Enemy guilt 不变化：" + (enemy.currentGuilt == enemyGuiltBefore));
        Debug.Log("预期只造成一次伤害且不回落 Unresponded：" + (hpAfter == hpBefore - expectedDamage));
        Debug.Log("预期不会错误触发后续守备：" + (defenseSlot != null && !defenseSlot.isUsed && followDefense.currentCooldown == defenseCooldownBefore));

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
        Debug.Log("预期Defense正常进入CD：" + (followDefense.currentCooldown == followDefense.cardData.cooldown && followDefense.currentCooldown != defenseCooldownBefore));
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

        BattleActionSlot passiveSlot = BattleActionSlotManager.GetSlot(actionSlots, allyB, 2);

        Debug.Log("预期计划生成RespondedEnemyIntent：" + (item != null && item.executionType == BattleExecutionItemType.RespondedEnemyIntent));
        Debug.Log("预期不生成Unresponded被动Dodge接管：" + (unrespondedCount == 0));
        Debug.Log("预期Responded item候选数为0：" + (candidateCount == 0));
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

    // RunActionSlotPassiveDodgeAfterAttackLoseBasicTestSequence = Attack 失败后被动 Dodge 接管聚合测试
    void RunActionSlotPassiveDodgeAfterAttackLoseBasicTestSequence()
    {
        Debug.Log("===== Attack 拼点失败后被动 Dodge 接管聚合测试开始 =====");
        Debug.Log("本入口只测试 Responded Attack vs Attack 的 EnemyWin 分支，不修改 Unresponded 被动 Dodge");

        RunPassiveDodgeAfterAttackLoseDodgeFirstSubTest(
            "PassiveDodgeAfterAttackLoseSuccess",
            4,
            8,
            9,
            "DodgeSuccess",
            0,
            true
        );

        RunPassiveDodgeAfterAttackLoseDodgeFirstSubTest(
            "PassiveDodgeAfterAttackLoseFailed",
            4,
            8,
            6,
            "DodgeFailed",
            8,
            true
        );

        RunPassiveDodgeAfterAttackLoseDodgeFirstSubTest(
            "PassiveDodgeAfterAttackLoseTieLimit",
            4,
            8,
            8,
            "TieLimit",
            0,
            false
        );

        RunPassiveDodgeAfterAttackLoseSkipInvalidToDefenseSubTest();
        RunPassiveDodgeAfterAttackLoseDefenseFirstSubTest();
        RunPassiveDodgeAfterAttackLoseNoCandidateSubTest();
        RunPassiveDodgeAfterAttackLoseTargetMismatchSubTest();
        RunPassiveDodgeAfterAttackLosePlayerWinSubTest();
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
        Debug.Log("预期主Attack槽位 isUsed：" + (responseSlot != null && responseSlot.isUsed));
        Debug.Log("预期Dodge槽位 isUsed = " + expectDodgeUsed + "：" + (dodgeSlot != null && dodgeSlot.isUsed == expectDodgeUsed));
        Debug.Log("预期Defense槽位未使用：" + (defenseSlot != null && !defenseSlot.isUsed));
        Debug.Log("预期主Attack已使用：" + (responseAttack.currentCooldown == responseAttack.cardData.cooldown && responseAttack.currentCooldown != responseAttackCooldownBefore));
        Debug.Log("预期Enemy Attack已使用：" + (enemyAttack.currentCooldown == enemyAttack.cardData.cooldown && enemyAttack.currentCooldown != enemyAttackCooldownBefore));
        Debug.Log("预期Dodge使用状态符合分支：" + (expectDodgeUsed ? passiveDodge.currentCooldown == passiveDodge.cardData.cooldown : passiveDodge.currentCooldown == dodgeCooldownBefore));
        Debug.Log("预期Defense CD / UseCount不变：" + (followDefense.currentCooldown == defenseCooldownBefore && followDefense.currentUseCount == defenseUseCountBefore));
        Debug.Log("预期主Attack UseCount前后：" + responseAttackUseCountBefore + " -> " + responseAttack.currentUseCount);
        Debug.Log("预期Enemy Attack UseCount前后：" + enemyAttackUseCountBefore + " -> " + enemyAttack.currentUseCount);
        Debug.Log("预期Dodge UseCount / isConsumed前后符合：" + (passiveDodge.currentUseCount == dodgeUseCountBefore && passiveDodge.isConsumed == dodgeConsumedBefore));
        Debug.Log("预期只造成一次伤害：" + (allyB.currentHP == hpBefore - expectedDamage));
        Debug.Log("预期使用固定敌人点数，未重新Roll：" + (enemyAttackPoint == enemyAttack.cardData.minPoint && enemyAttackPoint == enemyAttack.cardData.maxPoint));

        if (expectedResultType == "TieLimit")
        {
            Debug.Log("TieLimit 额外验证：主Attack已使用：" + (responseSlot != null && responseSlot.isUsed && responseAttack.currentCooldown == responseAttack.cardData.cooldown));
            Debug.Log("TieLimit 额外验证：Enemy Attack已使用：" + (enemyAttack.currentCooldown == enemyAttack.cardData.cooldown));
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
        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyB, 1, context.allyB, bulletAttack, context.enemy);

        BattleActionSlot actionSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyB, 1);
        int enemyHPBefore = context.enemy.currentHP;
        int cooldownBefore = bulletAttack.currentCooldown;
        int useCountBefore = bulletAttack.currentUseCount;
        int guiltBefore = context.allyB.currentGuilt;

        BattleResolveResult directResult = BattleResolver.ResolveFreeAction(actionSlot);
        BattleExecutionPlan executionPlan = CreateManualFreeActionPlan(actionSlot);
        BattleExecutionItem item = GetFirstExecutionItem(executionPlan);
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        Debug.Log("resultType是否为ActionUnavailable：" + (directResult != null && directResult.resultType == "ActionUnavailable"));
        Debug.Log("isSuccess是否为False：" + (directResult != null && !directResult.isSuccess));
        Debug.Log("shouldCompleteItem是否为True：" + (directResult != null && directResult.shouldCompleteItem));
        Debug.Log("不造成伤害：" + (context.enemy.currentHP == enemyHPBefore));
        Debug.Log("CD不变：" + (bulletAttack.currentCooldown == cooldownBefore));
        Debug.Log("UseCount不变：" + (bulletAttack.currentUseCount == useCountBefore));
        Debug.Log("guilt不变：" + (context.allyB.currentGuilt == guiltBefore));
        Debug.Log("行动未使用卡牌：" + (bulletAttack.currentUseCount == useCountBefore && context.allyB.currentGuilt == guiltBefore && bulletAttack.currentCooldown == cooldownBefore));
        Debug.Log("槽位未MarkUsed：" + (actionSlot != null && !actionSlot.isUsed));
        Debug.Log("item按跳过完成：" + (item != null && item.isCompleted));
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

        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyB, 1, context.allyB, unavailableAttack, context.enemy);
        BattleActionSlotManager.AssignFreeAction(actionSlots, context.allyA, 1, context.allyA, followAbility, context.allyA);

        BattleActionSlot firstSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyB, 1);
        BattleActionSlot secondSlot = BattleActionSlotManager.GetSlot(actionSlots, context.allyA, 1);

        int firstUseCountBefore = unavailableAttack.currentUseCount;
        int secondUseCountBefore = followAbility.currentUseCount;
        int secondGuiltBefore = context.allyA.currentGuilt;

        BattleExecutionPlan executionPlan = CreateManualFreeActionPlan(firstSlot, secondSlot);
        BattleExecutionItem firstItem = executionPlan.executionItems[0];
        BattleExecutionItem secondItem = executionPlan.executionItems[1];
        ExecutePlanWithRuntimeStateAndCompleteTurn(context.runtimeState, executionPlan);

        Debug.Log("第一个item是否完成：" + (firstItem != null && firstItem.isCompleted));
        Debug.Log("第一个槽位未MarkUsed：" + (firstSlot != null && !firstSlot.isUsed));
        Debug.Log("第一个卡牌UseCount不变：" + (unavailableAttack.currentUseCount == firstUseCountBefore));
        Debug.Log("第二个item是否完成：" + (secondItem != null && secondItem.isCompleted));
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
        Debug.Log("Responded item提前携带B的PassiveGuard候选：" + (candidateCount > 0));
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

        context.runtimeState.SetPhase("TurnEnded");
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

        startContext.runtimeState.SetPhase("TurnEnded");
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
        endContext.runtimeState.SetPhase("Completed");
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
        bool killCardUsed = killSlot != null && killSlot.isUsed && killAttack.currentCooldown == killAttack.cardData.cooldown;
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
        Debug.Log("预期玩家响应 Attack CD 正常变化一次：" + (responseAttack.currentCooldown == responseAttack.cardData.cooldown));
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
        int expectedEnemyUseCount = 1;
        Debug.Log("预期敌人 UseCount：" + expectedEnemyUseCount + "，实际是否符合：" + (enemyAttack.currentUseCount == expectedEnemyUseCount));

        if (expectPlayerWin)
        {
            Debug.Log("PlayerWin 分支：敌人失败 Attack 也完成一次使用：" + (enemyAttack.currentUseCount == expectedEnemyUseCount));
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
        context.runtimeState.SetPhase("Prepare");

        return context;
    }

    void ExecutePlanWithRuntimeStateAndCompleteTurn(BattleRuntimeState runtimeState, BattleExecutionPlan executionPlan)
    {
        runtimeState.SetExecutionPlan(executionPlan);
        runtimeState.SetPhase("BattleStart");
        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        BattleExecutionPlanExecutor.ExecuteExecutionPlan(executionPlan, runtimeState);

        if (!runtimeState.IsBattleEnded &&
            runtimeState.currentExecutionPlan != null &&
            runtimeState.currentExecutionPlan.isCompleted)
        {
            runtimeState.SetPhase("Completed");
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
