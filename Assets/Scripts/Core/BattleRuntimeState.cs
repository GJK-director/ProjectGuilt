// 脚本中文说明：战斗运行时状态。集中保存当前战斗中的角色、槽位、敌人意图和执行计划，方便后续 UI 读取。
using System.Collections.Generic;
using UnityEngine;

// BattleRuntimeState = 战斗运行时状态
// 第一版只做状态容器，不执行战斗逻辑，也不处理回合结算。
public class BattleRuntimeState
{
    // 当前测试战斗里的三个主要角色。
    public CharacterData allyA;
    public CharacterData allyB;
    public CharacterData enemy;

    // battleUnits = 当前战斗中的全部角色。
    public List<CharacterData> battleUnits;

    // actionSlots = 当前准备阶段玩家行动槽位。
    public List<BattleActionSlot> actionSlots;

    // intentQueue = 当前敌人意图队列。
    public List<BattleEnemyIntent> intentQueue;

    // currentExecutionPlan = 当前生成的执行计划。
    public BattleExecutionPlan currentExecutionPlan;

    // currentTurn = 当前回合数，第一版从 1 开始。
    public int currentTurn;

    // currentPhase = 当前阶段文本，第一版先用字符串，不急着做 enum。
    public string currentPhase;

    public BattleRuntimeState()
    {
        battleUnits = new List<CharacterData>();
        actionSlots = new List<BattleActionSlot>();
        intentQueue = new List<BattleEnemyIntent>();
        currentExecutionPlan = null;
        currentTurn = 1;
        currentPhase = "Init";
    }

    // SetCharacters = 设置当前战斗主要角色，并重建 battleUnits。
    public void SetCharacters(CharacterData allyA, CharacterData allyB, CharacterData enemy)
    {
        this.allyA = allyA;
        this.allyB = allyB;
        this.enemy = enemy;

        battleUnits.Clear();

        AddUnitIfNotNull(allyA);
        AddUnitIfNotNull(allyB);
        AddUnitIfNotNull(enemy);
    }

    // SetActionSlots = 保存当前行动槽位。传入 null 时使用空列表，避免 UI 读取时空引用。
    public void SetActionSlots(List<BattleActionSlot> slots)
    {
        actionSlots = slots != null ? slots : new List<BattleActionSlot>();
    }

    // SetIntentQueue = 保存当前敌人意图队列。传入 null 时使用空列表，避免 UI 读取时空引用。
    public void SetIntentQueue(List<BattleEnemyIntent> intents)
    {
        intentQueue = intents != null ? intents : new List<BattleEnemyIntent>();
    }

    // SetExecutionPlan = 保存当前执行计划。执行计划允许为空，表示当前还没有生成计划。
    public void SetExecutionPlan(BattleExecutionPlan plan)
    {
        currentExecutionPlan = plan;
    }

    // ClearActionSlots = 清空当前槽位列表引用内容，不调用 slot.Clear()，不做回合逻辑。
    public void ClearActionSlots()
    {
        actionSlots.Clear();
    }

    // ClearIntentQueue = 清空当前敌人意图队列，不生成新意图。
    public void ClearIntentQueue()
    {
        intentQueue.Clear();
    }

    // ClearExecutionPlan = 清空当前执行计划引用。
    public void ClearExecutionPlan()
    {
        currentExecutionPlan = null;
    }

    // ClearCurrentTurnRuntimeObjects = 清理当前回合临时战斗对象
    // 只清空槽位、敌人意图和执行计划，不处理回合结束、Buff、CD 或下一回合生成。
    public void ClearCurrentTurnRuntimeObjects()
    {
        ClearActionSlots();
        ClearIntentQueue();
        ClearExecutionPlan();
        SetPhase("TurnCleared");
    }

    // EndCurrentTurnAndClearRuntimeObjects = 结束当前回合并清理运行时临时对象
    // 第一版只组合 BattleTurnProcessor.EndTurn 和 RuntimeState 清理，不生成下一回合。
    public void EndCurrentTurnAndClearRuntimeObjects()
    {
        BattleTurnProcessor.EndTurn(battleUnits);
        ClearCurrentTurnRuntimeObjects();
        SetPhase("TurnEnded");
    }

    // PrepareNextTurnWithRuntimeObjects = 准备下一回合运行时对象
    // 外部负责创建新槽位和新敌人意图，这里只接收并保存，不写死敌人意图生成规则。
    public void PrepareNextTurnWithRuntimeObjects(
        List<BattleActionSlot> newActionSlots,
        List<BattleEnemyIntent> newIntentQueue
    )
    {
        AdvanceTurn();
        BattleTurnProcessor.StartTurn(battleUnits);
        SetActionSlots(newActionSlots);
        SetIntentQueue(newIntentQueue);
        ClearExecutionPlan();
        SetPhase("Prepare");
    }

    // SetPhase = 设置当前阶段文本。
    public void SetPhase(string phase)
    {
        currentPhase = string.IsNullOrEmpty(phase) ? "Unknown" : phase;
    }

    // AdvanceTurn = 当前回合数递增。
    public void AdvanceTurn()
    {
        currentTurn++;
    }

    // PrintRuntimeState = 打印当前运行时状态，只读不改状态。
    public void PrintRuntimeState()
    {
        Debug.Log("===== BattleRuntimeState 当前状态 =====");
        Debug.Log("当前回合：" + currentTurn);
        Debug.Log("当前阶段：" + currentPhase);
        Debug.Log("allyA：" + GetCharacterSummary(allyA));
        Debug.Log("allyB：" + GetCharacterSummary(allyB));
        Debug.Log("enemy：" + GetCharacterSummary(enemy));
        Debug.Log("battleUnits 数量：" + GetListCount(battleUnits));
        Debug.Log("actionSlots 数量：" + GetListCount(actionSlots));
        Debug.Log("intentQueue 数量：" + GetListCount(intentQueue));
        Debug.Log("currentExecutionPlan 是否为空：" + (currentExecutionPlan == null));

        if (currentExecutionPlan != null)
        {
            int itemCount = currentExecutionPlan.executionItems != null
                ? currentExecutionPlan.executionItems.Count
                : 0;

            Debug.Log("currentExecutionPlan item 数量：" + itemCount);
            Debug.Log("currentExecutionPlan 是否完成：" + currentExecutionPlan.isCompleted);
        }
    }

    void AddUnitIfNotNull(CharacterData unit)
    {
        if (unit != null)
        {
            battleUnits.Add(unit);
        }
    }

    string GetCharacterSummary(CharacterData character)
    {
        if (character == null)
        {
            return "空";
        }

        return character.characterName + " HP：" + character.currentHP + " / " + character.maxHP;
    }

    int GetListCount<T>(List<T> list)
    {
        return list != null ? list.Count : 0;
    }
}
