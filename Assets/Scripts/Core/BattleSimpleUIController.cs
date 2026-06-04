// 脚本中文说明：简易战斗 UI 控制器。连接手动搭建的 TMP 文本和按钮，用 RuntimeState / ViewData 刷新界面。
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleSimpleUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text topInfoText;
    [SerializeField] private TMP_Text enemyStateText;
    [SerializeField] private TMP_Text allyAStateText;
    [SerializeField] private TMP_Text allyBStateText;
    [SerializeField] private TMP_Text intentListText;
    [SerializeField] private TMP_Text actionSlot1Text;
    [SerializeField] private TMP_Text actionSlot2Text;
    [SerializeField] private TMP_Text actionSlotA1Text;
    [SerializeField] private TMP_Text actionSlotA2Text;
    [SerializeField] private TMP_Text actionSlotB1Text;
    [SerializeField] private TMP_Text actionSlotB2Text;
    [SerializeField] private TMP_Text logText;

    [SerializeField] private Button assignA1FreeAttackButton;
    [SerializeField] private Button assignA1AbilityButton;
    [SerializeField] private Button assignB1RespondIntent1Button;
    [SerializeField] private Button battleStartButton;
    [SerializeField] private Button createExecutionPlanButton;
    [SerializeField] private Button executePlanButton;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button prepareNextTurnButton;
    [SerializeField] private Button refreshViewButton;

    private BattleRuntimeState runtimeState;

    private CharacterData allyA;
    private CharacterData allyB;
    private CharacterData enemy;

    private BattleCardState allyAAttackCardState;
    private BattleCardState allyBAttackCardState;
    private BattleCardState allyAAbilityCardState;
    private BattleCardState enemyAttackCardState;

    private string lastLog = "等待初始化";

    void Start()
    {
        InitializeTestBattleData();
        BindButtonEvents();
        RefreshView();
    }

    void OnDestroy()
    {
        UnbindButtonEvents();
    }

    void InitializeTestBattleData()
    {
        CreateTestCharacters();
        AddTestBuffs();

        List<CardTestData> cards = CardDataLoader.LoadCardData();
        if (cards == null)
        {
            lastLog = "初始化失败：没有读取到卡牌数据";
            Debug.LogWarning(lastLog);
            return;
        }

        CreateTestBattleCards(cards);

        runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(allyA, allyB, enemy);
        runtimeState.SetActionSlots(BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2));
        runtimeState.SetIntentQueue(CreateFixedEnemyIntentQueue());
        runtimeState.SetPhase("Prepare");

        lastLog = "初始化完成：已进入 Prepare 阶段";
    }

    void CreateTestCharacters()
    {
        allyA = new CharacterData("我方角色A", 30, 20, 20);
        allyB = new CharacterData("我方角色B", 30, 3, 5);
        enemy = new CharacterData("敌人", 999, 5, 8);
    }

    void AddTestBuffs()
    {
        if (allyA == null)
        {
            return;
        }

        allyA.AddBuff("Bullet", "子弹", "AbilityBuff", 6, -1, "None", "Permanent");
    }

    void CreateTestBattleCards(List<CardTestData> cards)
    {
        CardTestData enemyCard = CardDataLoader.FindCardByID(cards, "enemy_atk_001");
        CardTestData allyAAttackCard = CardDataLoader.FindCardByID(cards, "atk_001");
        CardTestData allyAAbilityCard = CardDataLoader.FindCardByID(cards, "sin_ability_001");

        enemyAttackCardState = BattleCardManager.CreateBattleCard(
            enemy,
            enemyCard,
            "ui_enemy_atk_001_copy_0"
        );

        allyAAttackCardState = BattleCardManager.CreateBattleCard(
            allyA,
            allyAAttackCard,
            "ui_allyA_atk_001_copy_0"
        );

        allyBAttackCardState = BattleCardManager.CreateBattleCard(
            allyB,
            allyAAttackCard,
            "ui_allyB_atk_001_copy_0"
        );

        allyAAbilityCardState = BattleCardManager.CreateBattleCard(
            allyA,
            allyAAbilityCard,
            "ui_allyA_sin_ability_001_copy_0"
        );
    }

    List<BattleEnemyIntent> CreateFixedEnemyIntentQueue()
    {
        if (enemy == null || enemyAttackCardState == null || allyB == null)
        {
            Debug.LogWarning("创建固定敌人意图失败：敌人 / 敌人卡牌 / 目标角色数据不完整");
            return new List<BattleEnemyIntent>();
        }

        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            "ui_fixed_enemy_intent_001",
            enemy,
            enemyAttackCardState,
            allyB,
            1,
            1
        );

        return BattleEnemyIntentManager.CreateIntentQueue(enemyIntent);
    }

    void BindButtonEvents()
    {
        if (assignA1FreeAttackButton != null)
        {
            assignA1FreeAttackButton.onClick.AddListener(OnClickAssignA1FreeAttack);
        }

        if (assignA1AbilityButton != null)
        {
            assignA1AbilityButton.onClick.AddListener(OnClickAssignA1Ability);
        }

        if (assignB1RespondIntent1Button != null)
        {
            assignB1RespondIntent1Button.onClick.AddListener(OnClickAssignB1RespondIntent1);
        }

        if (battleStartButton != null)
        {
            battleStartButton.onClick.AddListener(OnClickBattleStart);
        }

        if (createExecutionPlanButton != null)
        {
            createExecutionPlanButton.onClick.AddListener(OnClickCreateExecutionPlan);
        }

        if (executePlanButton != null)
        {
            executePlanButton.onClick.AddListener(OnClickExecutePlan);
        }

        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnClickEndTurn);
        }

        if (prepareNextTurnButton != null)
        {
            prepareNextTurnButton.onClick.AddListener(OnClickPrepareNextTurn);
        }

        if (refreshViewButton != null)
        {
            refreshViewButton.onClick.AddListener(RefreshView);
        }
    }

    void UnbindButtonEvents()
    {
        if (assignA1FreeAttackButton != null)
        {
            assignA1FreeAttackButton.onClick.RemoveListener(OnClickAssignA1FreeAttack);
        }

        if (assignA1AbilityButton != null)
        {
            assignA1AbilityButton.onClick.RemoveListener(OnClickAssignA1Ability);
        }

        if (assignB1RespondIntent1Button != null)
        {
            assignB1RespondIntent1Button.onClick.RemoveListener(OnClickAssignB1RespondIntent1);
        }

        if (battleStartButton != null)
        {
            battleStartButton.onClick.RemoveListener(OnClickBattleStart);
        }

        if (createExecutionPlanButton != null)
        {
            createExecutionPlanButton.onClick.RemoveListener(OnClickCreateExecutionPlan);
        }

        if (executePlanButton != null)
        {
            executePlanButton.onClick.RemoveListener(OnClickExecutePlan);
        }

        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveListener(OnClickEndTurn);
        }

        if (prepareNextTurnButton != null)
        {
            prepareNextTurnButton.onClick.RemoveListener(OnClickPrepareNextTurn);
        }

        if (refreshViewButton != null)
        {
            refreshViewButton.onClick.RemoveListener(RefreshView);
        }
    }

    private void OnClickAssignA1FreeAttack()
    {
        if (!HasRuntimeState())
        {
            return;
        }

        if (!CanEditActionSlots())
        {
            lastLog = "当前不能修改行动槽位，请在准备阶段选择行动";
            RefreshView();
            return;
        }

        bool result = BattleActionSlotManager.AssignFreeAction(
            runtimeState.actionSlots,
            allyA,
            1,
            allyA,
            allyAAttackCardState,
            enemy
        );

        lastLog = result
            ? "A槽位1已安排：我方角色A 使用基础攻击偷刀敌人"
            : "安排失败：A槽位1无法安排基础攻击 FreeAction";

        RefreshView();
    }

    private void OnClickAssignA1Ability()
    {
        if (!HasRuntimeState())
        {
            return;
        }

        if (!CanEditActionSlots())
        {
            lastLog = "当前不能修改行动槽位，请在准备阶段选择行动";
            RefreshView();
            return;
        }

        bool result = BattleActionSlotManager.AssignFreeAction(
            runtimeState.actionSlots,
            allyA,
            1,
            allyA,
            allyAAbilityCardState,
            allyA
        );

        lastLog = result
            ? "A槽位1已安排：我方角色A 使用 Ability FreeAction"
            : "安排失败：A槽位1无法安排 Ability FreeAction";

        RefreshView();
    }

    private void OnClickAssignB1RespondIntent1()
    {
        if (runtimeState == null)
        {
            lastLog = "战斗状态未初始化，无法响应敌人意图";
            RefreshView();
            return;
        }

        if (!CanEditActionSlots())
        {
            lastLog = "当前不能修改行动槽位，请在准备阶段选择行动";
            RefreshView();
            return;
        }

        BattleEnemyIntent intent = BattleEnemyIntentManager.FindIntentByOrder(runtimeState.intentQueue, 1);

        if (intent == null)
        {
            lastLog = "没有找到敌人意图1，无法响应";
            RefreshView();
            return;
        }

        bool result = BattleActionSlotManager.AssignResponseToEnemyIntent(
            runtimeState.actionSlots,
            allyB,
            1,
            allyB,
            allyBAttackCardState,
            intent
        );

        lastLog = result
            ? "B槽位1已安排：我方角色B 使用基础攻击响应敌人意图1"
            : "安排失败：我方角色B 槽位1无法响应敌人意图1";

        RefreshView();
    }

    private void OnClickBattleStart()
    {
        if (runtimeState == null)
        {
            lastLog = "战斗状态未初始化，无法开始战斗";
            RefreshView();
            return;
        }

        if (IsCurrentPlanCompleted())
        {
            lastLog = "当前计划已经执行完成，请结束回合或准备下一回合";
            RefreshView();
            return;
        }

        if (IsPhase("TurnEnded"))
        {
            lastLog = "当前回合已经结束，请准备下一回合";
            RefreshView();
            return;
        }

        if (HasCurrentPlan())
        {
            runtimeState.SetPhase("BattleStart");
            BattleExecutionPlanManager.PrintExecutionPlan(runtimeState.currentExecutionPlan);

            BattleExecutionPlanExecutor.ExecuteExecutionPlan(runtimeState.currentExecutionPlan);
            runtimeState.SetPhase("Completed");

            lastLog = "战斗开始：已执行当前已有计划";
            RefreshView();
            return;
        }

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            runtimeState.actionSlots,
            runtimeState.intentQueue
        );

        runtimeState.SetExecutionPlan(executionPlan);

        if (executionPlan == null || executionPlan.executionItems == null || executionPlan.executionItems.Count == 0)
        {
            lastLog = "生成执行计划失败，无法开始战斗";
            RefreshView();
            return;
        }

        runtimeState.SetPhase("BattleStart");
        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(runtimeState.currentExecutionPlan);
        runtimeState.SetPhase("Completed");

        lastLog = "战斗开始：已生成并执行本回合计划";
        RefreshView();
    }

    private void OnClickCreateExecutionPlan()
    {
        if (!HasRuntimeState())
        {
            return;
        }

        if (!CanCreatePlan())
        {
            lastLog = "当前不能生成计划，可能已经有计划或不在准备阶段";
            RefreshView();
            return;
        }

        BattleExecutionPlan executionPlan = BattleExecutionPlanManager.CreateSpeedBasedExecutionPlan(
            runtimeState.actionSlots,
            runtimeState.intentQueue
        );

        runtimeState.SetExecutionPlan(executionPlan);
        runtimeState.SetPhase("PlanReady");

        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);

        int itemCount = executionPlan != null && executionPlan.executionItems != null
            ? executionPlan.executionItems.Count
            : 0;

        lastLog = "执行计划已生成，item 数量：" + itemCount;
        RefreshView();
    }

    private void OnClickExecutePlan()
    {
        if (!HasRuntimeState())
        {
            return;
        }

        if (!HasCurrentPlan())
        {
            lastLog = "当前没有执行计划，请先生成计划或点击战斗开始";
            RefreshView();
            return;
        }

        if (IsCurrentPlanCompleted())
        {
            lastLog = "当前计划已经执行完成，请结束回合或准备下一回合";
            RefreshView();
            return;
        }

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(runtimeState.currentExecutionPlan);
        runtimeState.SetPhase("Completed");

        lastLog = "执行计划已执行，plan.isCompleted = " + runtimeState.currentExecutionPlan.isCompleted;
        RefreshView();
    }

    private void OnClickEndTurn()
    {
        if (!HasRuntimeState())
        {
            return;
        }

        if (!CanEndTurn())
        {
            lastLog = "当前不能结束回合，请先完成战斗结算";
            RefreshView();
            return;
        }

        runtimeState.EndCurrentTurnAndClearRuntimeObjects();
        lastLog = "当前回合已结束，临时对象已清理";
        RefreshView();
    }

    private void OnClickPrepareNextTurn()
    {
        if (!HasRuntimeState())
        {
            return;
        }

        if (!CanPrepareNextTurn())
        {
            lastLog = "当前不能准备下一回合，请先结束当前回合";
            RefreshView();
            return;
        }

        List<BattleActionSlot> newActionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);
        List<BattleEnemyIntent> newIntentQueue = CreateFixedEnemyIntentQueue();

        runtimeState.PrepareNextTurnWithRuntimeObjects(newActionSlots, newIntentQueue);
        lastLog = "下一回合已准备，阶段：Prepare";
        RefreshView();
    }

    private void RefreshView()
    {
        BattleStateViewData viewData = BattleStateViewData.FromRuntimeState(runtimeState);

        SetText(topInfoText, "回合：" + viewData.currentTurn + "\n阶段：" + viewData.currentPhase);
        SetText(enemyStateText, FormatEnemyState(viewData));
        SetText(allyAStateText, FormatAllyState("A", viewData.allyAName, viewData.allyAHP, viewData.allyAMaxHP, viewData.allyASpeed, viewData.allyAGuilt));
        SetText(allyBStateText, FormatAllyState("B", viewData.allyBName, viewData.allyBHP, viewData.allyBMaxHP, viewData.allyBSpeed, viewData.allyBGuilt));
        SetText(intentListText, FormatIntentList(viewData));
        RefreshActionSlotTexts(viewData);
        SetText(logText, lastLog);
    }

    private bool IsPhase(string phaseName)
    {
        return runtimeState != null && runtimeState.currentPhase == phaseName;
    }

    private bool HasCurrentPlan()
    {
        return runtimeState != null && runtimeState.currentExecutionPlan != null;
    }

    private bool IsCurrentPlanCompleted()
    {
        return HasCurrentPlan() && runtimeState.currentExecutionPlan.isCompleted;
    }

    private bool CanEditActionSlots()
    {
        return IsPhase("Prepare") && !HasCurrentPlan();
    }

    private bool CanCreatePlan()
    {
        return IsPhase("Prepare") && !HasCurrentPlan();
    }

    private bool CanExecutePlan()
    {
        return HasCurrentPlan() && !runtimeState.currentExecutionPlan.isCompleted;
    }

    private bool CanEndTurn()
    {
        return IsPhase("Completed");
    }

    private bool CanPrepareNextTurn()
    {
        return IsPhase("TurnEnded");
    }

    bool HasRuntimeState()
    {
        if (runtimeState != null)
        {
            return true;
        }

        lastLog = "操作失败：BattleRuntimeState 尚未初始化";
        RefreshView();
        return false;
    }

    string FormatEnemyState(BattleStateViewData viewData)
    {
        return
            "敌人：" + viewData.enemyName +
            "\nHP：" + viewData.enemyHP + " / " + viewData.enemyMaxHP +
            "\n速度：" + viewData.enemySpeed;
    }

    string FormatAllyState(string label, string characterName, int hp, int maxHp, int speed, int guilt)
    {
        return
            "我方角色" + label + "：" + characterName +
            "\nHP：" + hp + " / " + maxHp +
            "\n速度：" + speed +
            "\n负罪感：" + guilt;
    }

    string FormatIntentList(BattleStateViewData viewData)
    {
        if (viewData.enemyIntentViews == null || viewData.enemyIntentViews.Count == 0)
        {
            return "暂无敌人意图";
        }

        StringBuilder builder = new StringBuilder();

        foreach (EnemyIntentViewData intentView in viewData.enemyIntentViews)
        {
            if (intentView == null)
            {
                continue;
            }

            builder.Append("意图").Append(intentView.intentOrder)
                .Append("：").Append(intentView.enemyName)
                .Append(" 使用 ").Append(intentView.enemyCardName)
                .AppendLine();
            builder.Append("原目标：").Append(intentView.originalTargetName)
                .Append(" 槽位").Append(intentView.originalTargetSlotIndex)
                .AppendLine();
            builder.Append("实际目标：").Append(intentView.actualTargetName)
                .Append(" 槽位").Append(intentView.actualTargetSlotIndex)
                .AppendLine();
            builder.Append("已响应：").Append(intentView.isResponded)
                .AppendLine();
        }

        return builder.ToString();
    }

    void RefreshActionSlotTexts(BattleStateViewData viewData)
    {
        if (HasNewActionSlotTextBindings())
        {
            SetText(actionSlotA1Text, FormatOwnerActionSlot(viewData, allyA, 1, "A槽位1"));
            SetText(actionSlotA2Text, FormatOwnerActionSlot(viewData, allyA, 2, "A槽位2"));
            SetText(actionSlotB1Text, FormatOwnerActionSlot(viewData, allyB, 1, "B槽位1"));
            SetText(actionSlotB2Text, FormatOwnerActionSlot(viewData, allyB, 2, "B槽位2"));
            return;
        }

        SetText(actionSlot1Text, FormatOwnerActionSlotWithFallback(viewData, allyA, 1, "A槽位1", 1));
        SetText(actionSlot2Text, FormatOwnerActionSlotWithFallback(viewData, allyB, 1, "B槽位1", 2));
    }

    bool HasNewActionSlotTextBindings()
    {
        return actionSlotA1Text != null ||
            actionSlotA2Text != null ||
            actionSlotB1Text != null ||
            actionSlotB2Text != null;
    }

    string FormatActionSlot(BattleStateViewData viewData, int slotIndex)
    {
        ActionSlotViewData slotView = FindActionSlotView(viewData, slotIndex);

        return FormatActionSlotView(slotView, "槽位" + slotIndex);
    }

    string FormatOwnerActionSlotWithFallback(
        BattleStateViewData viewData,
        CharacterData owner,
        int slotIndex,
        string fallbackDisplayName,
        int fallbackViewSlotIndex
    )
    {
        ActionSlotViewData slotView = FindOwnerActionSlotView(viewData, owner, slotIndex);

        if (slotView == null)
        {
            return FormatActionSlot(viewData, fallbackViewSlotIndex);
        }

        return FormatActionSlotView(slotView, fallbackDisplayName);
    }

    string FormatOwnerActionSlot(
        BattleStateViewData viewData,
        CharacterData owner,
        int slotIndex,
        string fallbackDisplayName
    )
    {
        ActionSlotViewData slotView = FindOwnerActionSlotView(viewData, owner, slotIndex);
        return FormatActionSlotView(slotView, fallbackDisplayName);
    }

    string FormatActionSlotView(ActionSlotViewData slotView, string fallbackDisplayName)
    {
        if (slotView == null)
        {
            return fallbackDisplayName + "\n空";
        }

        string displayName = string.IsNullOrEmpty(slotView.displaySlotName)
            ? fallbackDisplayName
            : slotView.displaySlotName;

        if (slotView.isEmpty)
        {
            return displayName + "\n空";
        }

        string enemyIntentText = slotView.hasEnemyIntent
            ? slotView.enemyIntentOrder.ToString()
            : "无";

        return
            displayName +
            "\n类型：" + slotView.slotType +
            "\n行动者：" + slotView.actorName +
            "\n卡牌：" + slotView.cardName +
            "\n卡牌类型：" + slotView.cardType +
            "\n目标：" + slotView.targetName +
            "\n敌人意图：" + enemyIntentText +
            "\n已使用：" + slotView.isUsed;
    }

    ActionSlotViewData FindOwnerActionSlotView(BattleStateViewData viewData, CharacterData owner, int slotIndex)
    {
        if (viewData == null || viewData.actionSlotViews == null || owner == null)
        {
            return null;
        }

        foreach (ActionSlotViewData slotView in viewData.actionSlotViews)
        {
            if (slotView == null)
            {
                continue;
            }

            if (slotView.ownerName == owner.characterName && slotView.slotIndex == slotIndex)
            {
                return slotView;
            }
        }

        return null;
    }

    ActionSlotViewData FindActionSlotView(BattleStateViewData viewData, int slotIndex)
    {
        if (viewData == null || viewData.actionSlotViews == null)
        {
            return null;
        }

        foreach (ActionSlotViewData slotView in viewData.actionSlotViews)
        {
            if (slotView != null && slotView.slotIndex == slotIndex)
            {
                return slotView;
            }
        }

        return null;
    }

    void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
