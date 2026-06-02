// 脚本中文说明：战斗状态只读快照。用于给简易 UI 读取 RuntimeState 的当前状态，不修改战斗逻辑。
using System.Collections.Generic;
using UnityEngine;

public class BattleStateViewData
{
    public int currentTurn;
    public string currentPhase;

    public string allyAName;
    public int allyAHP;
    public int allyAMaxHP;
    public int allyASpeed;
    public int allyAGuilt;

    public string allyBName;
    public int allyBHP;
    public int allyBMaxHP;
    public int allyBSpeed;
    public int allyBGuilt;

    public string enemyName;
    public int enemyHP;
    public int enemyMaxHP;
    public int enemySpeed;

    public int actionSlotCount;
    public int intentCount;

    public bool hasExecutionPlan;
    public bool executionPlanCompleted;
    public int executionItemCount;

    public List<ActionSlotViewData> actionSlotViews;
    public List<EnemyIntentViewData> enemyIntentViews;

    // FromRuntimeState = 从 BattleRuntimeState 生成 UI 可读取的只读快照。
    public static BattleStateViewData FromRuntimeState(BattleRuntimeState runtimeState)
    {
        BattleStateViewData viewData = new BattleStateViewData();
        viewData.actionSlotViews = new List<ActionSlotViewData>();
        viewData.enemyIntentViews = new List<EnemyIntentViewData>();

        if (runtimeState == null)
        {
            viewData.currentPhase = "None";
            return viewData;
        }

        viewData.currentTurn = runtimeState.currentTurn;
        viewData.currentPhase = runtimeState.currentPhase;

        FillAllyA(viewData, runtimeState.allyA);
        FillAllyB(viewData, runtimeState.allyB);
        FillEnemy(viewData, runtimeState.enemy);

        viewData.actionSlotCount = runtimeState.actionSlots != null
            ? runtimeState.actionSlots.Count
            : 0;

        viewData.actionSlotViews = CreateActionSlotViews(runtimeState.actionSlots);

        viewData.intentCount = runtimeState.intentQueue != null
            ? runtimeState.intentQueue.Count
            : 0;

        viewData.enemyIntentViews = CreateEnemyIntentViews(runtimeState.intentQueue);

        viewData.hasExecutionPlan = runtimeState.currentExecutionPlan != null;

        if (runtimeState.currentExecutionPlan != null)
        {
            viewData.executionPlanCompleted = runtimeState.currentExecutionPlan.isCompleted;
            viewData.executionItemCount = runtimeState.currentExecutionPlan.executionItems != null
                ? runtimeState.currentExecutionPlan.executionItems.Count
                : 0;
        }

        return viewData;
    }

    public void PrintViewData()
    {
        Debug.Log("===== BattleStateViewData 当前状态快照 =====");
        Debug.Log("当前回合：" + currentTurn);
        Debug.Log("当前阶段：" + currentPhase);
        Debug.Log("allyA：" + allyAName + " HP：" + allyAHP + " / " + allyAMaxHP + "，速度：" + allyASpeed + "，负罪感：" + allyAGuilt);
        Debug.Log("allyB：" + allyBName + " HP：" + allyBHP + " / " + allyBMaxHP + "，速度：" + allyBSpeed + "，负罪感：" + allyBGuilt);
        Debug.Log("enemy：" + enemyName + " HP：" + enemyHP + " / " + enemyMaxHP + "，速度：" + enemySpeed);
        Debug.Log("actionSlotCount：" + actionSlotCount);
        Debug.Log("intentCount：" + intentCount);
        Debug.Log("hasExecutionPlan：" + hasExecutionPlan);
        Debug.Log("executionPlanCompleted：" + executionPlanCompleted);
        Debug.Log("executionItemCount：" + executionItemCount);

        PrintActionSlotViews();
        PrintEnemyIntentViews();
    }

    void PrintActionSlotViews()
    {
        Debug.Log("===== ActionSlotViewData 行动槽位快照 =====");

        if (actionSlotViews == null || actionSlotViews.Count == 0)
        {
            Debug.Log("当前没有行动槽位快照");
            return;
        }

        foreach (ActionSlotViewData slotView in actionSlotViews)
        {
            if (slotView == null)
            {
                continue;
            }

            if (slotView.isEmpty)
            {
                Debug.Log("槽位" + slotView.slotIndex + "：空槽位");
                continue;
            }

            string enemyIntentText = slotView.hasEnemyIntent
                ? slotView.enemyIntentOrder.ToString()
                : "无";

            Debug.Log(
                "槽位" + slotView.slotIndex +
                "：actor=" + slotView.actorName +
                " / card=" + slotView.cardName +
                " / type=" + slotView.cardType +
                " / target=" + slotView.targetName +
                " / enemyIntent=" + enemyIntentText +
                " / isUsed=" + slotView.isUsed +
                " / isEmpty=" + slotView.isEmpty
            );
        }
    }

    void PrintEnemyIntentViews()
    {
        Debug.Log("===== EnemyIntentViewData 敌人意图快照 =====");

        if (enemyIntentViews == null || enemyIntentViews.Count == 0)
        {
            Debug.Log("当前没有敌人意图快照");
            return;
        }

        foreach (EnemyIntentViewData intentView in enemyIntentViews)
        {
            if (intentView == null)
            {
                continue;
            }

            Debug.Log(
                "意图" + intentView.intentOrder +
                "：" + intentView.enemyName +
                " 使用 " + intentView.enemyCardName
            );

            Debug.Log(
                "原目标：" + intentView.originalTargetName +
                " 槽位" + intentView.originalTargetSlotIndex
            );

            Debug.Log(
                "实际目标：" + intentView.actualTargetName +
                " 槽位" + intentView.actualTargetSlotIndex
            );

            Debug.Log("已响应：" + intentView.isResponded);
        }
    }

    static List<ActionSlotViewData> CreateActionSlotViews(List<BattleActionSlot> actionSlots)
    {
        List<ActionSlotViewData> views = new List<ActionSlotViewData>();

        if (actionSlots == null)
        {
            return views;
        }

        foreach (BattleActionSlot actionSlot in actionSlots)
        {
            views.Add(ActionSlotViewData.FromActionSlot(actionSlot));
        }

        return views;
    }

    static List<EnemyIntentViewData> CreateEnemyIntentViews(List<BattleEnemyIntent> intentQueue)
    {
        List<EnemyIntentViewData> views = new List<EnemyIntentViewData>();

        if (intentQueue == null)
        {
            return views;
        }

        foreach (BattleEnemyIntent enemyIntent in intentQueue)
        {
            views.Add(EnemyIntentViewData.FromEnemyIntent(enemyIntent));
        }

        return views;
    }

    static void FillAllyA(BattleStateViewData viewData, CharacterData character)
    {
        viewData.allyAName = GetCharacterName(character);
        viewData.allyAHP = GetCurrentHP(character);
        viewData.allyAMaxHP = GetMaxHP(character);
        viewData.allyASpeed = GetCurrentSpeed(character);
        viewData.allyAGuilt = GetCurrentGuilt(character);
    }

    static void FillAllyB(BattleStateViewData viewData, CharacterData character)
    {
        viewData.allyBName = GetCharacterName(character);
        viewData.allyBHP = GetCurrentHP(character);
        viewData.allyBMaxHP = GetMaxHP(character);
        viewData.allyBSpeed = GetCurrentSpeed(character);
        viewData.allyBGuilt = GetCurrentGuilt(character);
    }

    static void FillEnemy(BattleStateViewData viewData, CharacterData character)
    {
        viewData.enemyName = GetCharacterName(character);
        viewData.enemyHP = GetCurrentHP(character);
        viewData.enemyMaxHP = GetMaxHP(character);
        viewData.enemySpeed = GetCurrentSpeed(character);
    }

    static string GetCharacterName(CharacterData character)
    {
        return character != null ? character.characterName : "";
    }

    static int GetCurrentHP(CharacterData character)
    {
        return character != null ? character.currentHP : 0;
    }

    static int GetMaxHP(CharacterData character)
    {
        return character != null ? character.maxHP : 0;
    }

    static int GetCurrentSpeed(CharacterData character)
    {
        return character != null ? character.GetCurrentSpeed() : 0;
    }

    static int GetCurrentGuilt(CharacterData character)
    {
        return character != null ? character.currentGuilt : 0;
    }
}

public class ActionSlotViewData
{
    public int slotIndex;
    public string slotType;

    public string actorName;
    public string cardName;
    public string cardType;

    public string targetName;

    public int enemyIntentOrder;
    public bool hasEnemyIntent;

    public bool isUsed;
    public bool isEmpty;

    public static ActionSlotViewData FromActionSlot(BattleActionSlot actionSlot)
    {
        ActionSlotViewData viewData = new ActionSlotViewData();

        if (actionSlot == null)
        {
            viewData.isEmpty = true;
            return viewData;
        }

        viewData.slotIndex = actionSlot.slotIndex;
        viewData.slotType = actionSlot.slotType.ToString();
        viewData.actorName = actionSlot.GetActorName();
        viewData.cardName = actionSlot.GetCardName();
        viewData.cardType = GetCardType(actionSlot.cardState);
        viewData.targetName = actionSlot.GetTargetName();
        viewData.hasEnemyIntent = actionSlot.enemyIntent != null;
        viewData.enemyIntentOrder = actionSlot.enemyIntent != null
            ? actionSlot.enemyIntent.intentOrder
            : 0;
        viewData.isUsed = actionSlot.isUsed;
        viewData.isEmpty = actionSlot.IsEmpty();

        return viewData;
    }

    static string GetCardType(BattleCardState cardState)
    {
        if (cardState == null || cardState.cardData == null)
        {
            return "";
        }

        return cardState.cardData.cardType;
    }
}

public class EnemyIntentViewData
{
    public int intentOrder;

    public string enemyName;
    public string enemyCardName;

    public string originalTargetName;
    public int originalTargetSlotIndex;

    public string actualTargetName;
    public int actualTargetSlotIndex;

    public bool isResponded;

    public static EnemyIntentViewData FromEnemyIntent(BattleEnemyIntent enemyIntent)
    {
        EnemyIntentViewData viewData = new EnemyIntentViewData();

        if (enemyIntent == null)
        {
            return viewData;
        }

        viewData.intentOrder = enemyIntent.intentOrder;
        viewData.enemyName = enemyIntent.GetEnemyName();
        viewData.enemyCardName = enemyIntent.GetCardName();
        viewData.originalTargetName = enemyIntent.GetOriginalTargetName();
        viewData.originalTargetSlotIndex = enemyIntent.originalTargetSlotIndex;
        viewData.actualTargetName = enemyIntent.GetActualTargetName();
        viewData.actualTargetSlotIndex = enemyIntent.actualTargetSlotIndex;
        viewData.isResponded = enemyIntent.isResponded;

        return viewData;
    }
}
