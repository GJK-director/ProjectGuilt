using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public enum BattleTestMode
{
    ClashUseCount,
    AbilityUseCount,
    ActionSlotBasic
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
        BattleCardManager.PrintCardStates(allyA);
    }

    // CreateTestEnemyIntent = 创建测试用敌人意图
    BattleEnemyIntent CreateTestEnemyIntent()
    {
        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            "enemy_intent_001",
            enemy,
            enemyAttackCardState,
            allyB
        );

        Debug.Log(
            "创建敌人意图：" +
            enemyIntent.GetEnemyName() +
            " 使用 " +
            enemyIntent.GetCardName() +
            " 攻击 " +
            enemyIntent.GetOriginalTargetName()
        );

        return enemyIntent;
    }

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
