// 脚本中文说明：卡牌读取和战斗测试入口。负责在 Unity 场景启动时创建测试角色、读取卡牌并运行指定测试流程。
using System.Collections.Generic;
using UnityEngine;

public enum BattleTestMode
{
    ClashUseCount,
    AbilityUseCount,
    ActionSlotBasic,
    ActionSlotInterceptFail,
    ActionSlotInterceptEqualFail,
    ActionSlotMultiIntentBasic,
    ActionSlotResponseOverwriteBasic,
    ActionSlotResponseOverwriteFailKeepOld,
    ActionSlotExecutionPlanBasic,
    ActionSlotExecutionPlanEmpty,
    ActionSlotExecutionPlanMissingSlot,
    ActionSlotExecutionPlanMultiBasic,
    ActionSlotExecutionPlanStepPreviewBasic,
    ActionSlotExecutionPlanStepPreviewEmpty,
    ActionSlotExecutionPlanExecuteUnrespondedBasic,
    ActionSlotExecutionPlanExecuteRespondedBasic,
    ActionSlotExecutionPlanExecuteRespondedEnemyWin,
    ActionSlotExecutionPlanExecuteRespondedTieLimit,
    ActionSlotExecutionPlanExecuteMixedBasic
}

public class CardLoadTest : MonoBehaviour
{
    [SerializeField] private BattleTestMode testMode = BattleTestMode.ClashUseCount;

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

        if (testMode == BattleTestMode.ClashUseCount)
        {
            RunClashUseCountTestSequence();
            return;
        }

        if (testMode == BattleTestMode.AbilityUseCount)
        {
            RunAbilityUseCountTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotBasic)
        {
            RunActionSlotBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotInterceptFail)
        {
            RunActionSlotInterceptFailTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotInterceptEqualFail)
        {
            RunActionSlotInterceptEqualFailTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotMultiIntentBasic)
        {
            RunActionSlotMultiIntentBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotResponseOverwriteBasic)
        {
            RunActionSlotResponseOverwriteBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotResponseOverwriteFailKeepOld)
        {
            RunActionSlotResponseOverwriteFailKeepOldTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanBasic)
        {
            RunActionSlotExecutionPlanBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanEmpty)
        {
            RunActionSlotExecutionPlanEmptyTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanMissingSlot)
        {
            RunActionSlotExecutionPlanMissingSlotTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanMultiBasic)
        {
            RunActionSlotExecutionPlanMultiBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanStepPreviewBasic)
        {
            RunActionSlotExecutionPlanStepPreviewBasicTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanStepPreviewEmpty)
        {
            RunActionSlotExecutionPlanStepPreviewEmptyTestSequence();
            return;
        }

        if (testMode == BattleTestMode.ActionSlotExecutionPlanExecuteUnrespondedBasic)
        {
            RunActionSlotExecutionPlanExecuteUnrespondedBasicTestSequence();
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

        Debug.Log("执行后 我方角色A HP：" + allyA.currentHP + " / " + allyA.maxHP);
        Debug.Log("执行后 我方角色B HP：" + allyB.currentHP + " / " + allyB.maxHP);
        Debug.Log("执行后 敌人 HP：" + enemy.currentHP + " / " + enemy.maxHP);
        Debug.Log("ExecutionPlan 是否完成：" + executionPlan.isCompleted);
        Debug.Log("敌人胜利分支验证：我方角色A 应作为 actualTargetCharacter 扣血");
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

    // ================================
    // Action Slot 测试辅助方法
    // ================================

    // CreateTestAttackCardForCharacter = 给测试角色创建一张基础攻击卡实例
    BattleCardState CreateTestAttackCardForCharacter(CharacterData owner, string instanceID)
    {
        return BattleCardManager.CreateBattleCard(
            owner,
            allyAAttackCardState.cardData,
            instanceID
        );
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
