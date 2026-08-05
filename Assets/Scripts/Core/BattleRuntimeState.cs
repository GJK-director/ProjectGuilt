// 脚本中文说明：战斗运行时状态。集中保存当前战斗中的角色、槽位、敌人意图和执行计划，方便后续 UI 读取。
using System.Collections.Generic;
using UnityEngine;

public enum BattleResult
{
    None,
    Victory,
    Defeat
}

// BattleRuntimeState = 战斗运行时状态
// 第一版只做状态容器，不执行战斗逻辑，也不处理回合结算。
public class BattleRuntimeState
{
    // 当前测试战斗里的三个主要角色。
    public CharacterData allyA;
    public CharacterData allyB;
    public CharacterData enemy;
    public CharacterData enemy2;

    // 固定2+2第一版的阵营集合。旧字段继续代表A/B与第一名敌人。
    public List<CharacterData> allyUnits;
    public List<CharacterData> enemyUnits;

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

    // currentGuilt = 当前战斗中玩家队伍共享的公共负罪感。
    public int currentGuilt;

    private BattleLifecyclePhase lifecyclePhase;

    public BattleLifecyclePhase LifecyclePhase
    {
        get { return lifecyclePhase; }
    }

    // 旧UI和测试暂时继续读取该文本；内部权威状态只使用LifecyclePhase。
    public string currentPhase
    {
        get { return GetCompatiblePhaseText(lifecyclePhase); }
    }

    public BattleResult battleResult;

    public bool IsBattleEnded
    {
        get { return lifecyclePhase == BattleLifecyclePhase.BattleEnded; }
    }

    public BattleRuntimeState()
    {
        battleUnits = new List<CharacterData>();
        allyUnits = new List<CharacterData>();
        enemyUnits = new List<CharacterData>();
        actionSlots = new List<BattleActionSlot>();
        intentQueue = new List<BattleEnemyIntent>();
        currentExecutionPlan = null;
        currentTurn = 1;
        currentGuilt = 0;
        lifecyclePhase = BattleLifecyclePhase.Init;
        battleResult = BattleResult.None;
    }

    // SetCharacters = 设置当前战斗主要角色，并重建 battleUnits。
    public void SetCharacters(CharacterData allyA, CharacterData allyB, CharacterData enemy)
    {
        SetCharacters(allyA, allyB, enemy, null);
    }

    public void SetCharacters(
        CharacterData allyA,
        CharacterData allyB,
        CharacterData enemy,
        CharacterData enemy2
    )
    {
        UnbindSharedGuiltFromCurrentAllies();

        this.allyA = allyA;
        this.allyB = allyB;
        this.enemy = enemy;
        this.enemy2 = enemy2;

        BindSharedGuiltToAlly(allyA);
        BindSharedGuiltToAlly(allyB);

        battleUnits.Clear();
        allyUnits.Clear();
        enemyUnits.Clear();

        AddUnitIfNotNull(allyUnits, allyA);
        AddUnitIfNotNull(allyUnits, allyB);
        AddUnitIfNotNull(enemyUnits, enemy);
        AddUnitIfNotNull(enemyUnits, enemy2);

        AddUnitIfNotNull(battleUnits, allyA);
        AddUnitIfNotNull(battleUnits, allyB);
        AddUnitIfNotNull(battleUnits, enemy);
        AddUnitIfNotNull(battleUnits, enemy2);
    }

    public bool ContainsEnemy(CharacterData character)
    {
        return GetCharacterReferenceIndex(enemyUnits, character) >= 0;
    }

    public int GetEnemyIndex(CharacterData character)
    {
        return GetCharacterReferenceIndex(enemyUnits, character);
    }

    // AddGuilt = 增加本场战斗的公共负罪感。
    public void AddGuilt(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentGuilt += amount;
    }

    // GetBattlePositionIndex = 获取角色在 battleUnits 中的1-based战斗位置。
    // 未登记角色返回 int.MaxValue，由执行计划的稳定顺序继续兜底。
    public int GetBattlePositionIndex(CharacterData character)
    {
        if (character == null || battleUnits == null)
        {
            return int.MaxValue;
        }

        for (int index = 0; index < battleUnits.Count; index++)
        {
            if (object.ReferenceEquals(battleUnits[index], character))
            {
                return index + 1;
            }
        }

        return int.MaxValue;
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
    }

    internal bool TryTransitionTo(
        BattleLifecyclePhase nextPhase,
        out string failureMessage
    )
    {
        BattleLifecyclePhase previousPhase = lifecyclePhase;
        if (previousPhase == nextPhase)
        {
            failureMessage =
                "非法生命周期转换：" + previousPhase + " -> " + nextPhase +
                "；不能重复进入相同阶段。";
            Debug.LogWarning(failureMessage);
            return false;
        }

        if (!IsLegalTransition(previousPhase, nextPhase))
        {
            failureMessage =
                "非法生命周期转换：" + previousPhase + " -> " + nextPhase;
            Debug.LogWarning(failureMessage);
            return false;
        }

        lifecyclePhase = nextPhase;
        failureMessage = string.Empty;
        Debug.Log("生命周期转换：" + previousPhase + " -> " + nextPhase);
        return true;
    }

    // AdvanceTurn = 当前回合数递增。
    public void AdvanceTurn()
    {
        currentTurn++;
    }

    static bool IsLegalTransition(
        BattleLifecyclePhase current,
        BattleLifecyclePhase next
    )
    {
        if (next == BattleLifecyclePhase.BattleEnded)
        {
            return current == BattleLifecyclePhase.Executing ||
                current == BattleLifecyclePhase.TurnResolved ||
                current == BattleLifecyclePhase.TurnEnding ||
                current == BattleLifecyclePhase.TurnEnded ||
                current == BattleLifecyclePhase.PreparingNextTurn;
        }

        switch (current)
        {
            case BattleLifecyclePhase.Init:
                return next == BattleLifecyclePhase.Prepare;
            case BattleLifecyclePhase.Prepare:
                return next == BattleLifecyclePhase.PlanReady ||
                    next == BattleLifecyclePhase.Executing;
            case BattleLifecyclePhase.PlanReady:
                return next == BattleLifecyclePhase.Executing;
            case BattleLifecyclePhase.Executing:
                return next == BattleLifecyclePhase.TurnResolved;
            case BattleLifecyclePhase.TurnResolved:
                return next == BattleLifecyclePhase.TurnEnding;
            case BattleLifecyclePhase.TurnEnding:
                return next == BattleLifecyclePhase.TurnEnded;
            case BattleLifecyclePhase.TurnEnded:
                return next == BattleLifecyclePhase.PreparingNextTurn;
            case BattleLifecyclePhase.PreparingNextTurn:
                return next == BattleLifecyclePhase.Prepare;
            default:
                return false;
        }
    }

    static string GetCompatiblePhaseText(BattleLifecyclePhase phase)
    {
        switch (phase)
        {
            case BattleLifecyclePhase.Executing:
                return "BattleStart";
            case BattleLifecyclePhase.TurnResolved:
                return "Completed";
            default:
                return phase.ToString();
        }
    }

    // PrintRuntimeState = 打印当前运行时状态，只读不改状态。
    public void PrintRuntimeState()
    {
        Debug.Log("===== BattleRuntimeState 当前状态 =====");
        Debug.Log("当前回合：" + currentTurn);
        Debug.Log("当前公共负罪感：" + currentGuilt);
        Debug.Log("当前阶段：" + currentPhase);
        Debug.Log("战斗结果：" + battleResult);
        Debug.Log("allyA：" + GetCharacterSummary(allyA));
        Debug.Log("allyB：" + GetCharacterSummary(allyB));
        Debug.Log("enemy：" + GetCharacterSummary(enemy));
        Debug.Log("enemy2：" + GetCharacterSummary(enemy2));
        Debug.Log("allyUnits 数量：" + GetListCount(allyUnits));
        Debug.Log("enemyUnits 数量：" + GetListCount(enemyUnits));
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

    void AddUnitIfNotNull(List<CharacterData> units, CharacterData unit)
    {
        if (units != null && unit != null)
        {
            units.Add(unit);
        }
    }

    int GetCharacterReferenceIndex(
        List<CharacterData> units,
        CharacterData target
    )
    {
        if (units == null || target == null)
        {
            return -1;
        }

        for (int index = 0; index < units.Count; index++)
        {
            if (object.ReferenceEquals(units[index], target))
            {
                return index;
            }
        }

        return -1;
    }

    void BindSharedGuiltToAlly(CharacterData ally)
    {
        if (ally != null)
        {
            ally.BindSharedGuiltRuntimeState(this);
        }
    }

    void UnbindSharedGuiltFromCurrentAllies()
    {
        if (allyA != null)
        {
            allyA.UnbindSharedGuiltRuntimeState(this);
        }

        if (allyB != null && !object.ReferenceEquals(allyB, allyA))
        {
            allyB.UnbindSharedGuiltRuntimeState(this);
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
