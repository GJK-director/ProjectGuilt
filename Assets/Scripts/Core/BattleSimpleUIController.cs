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
    [SerializeField] private TMP_Text selectionInfoText;
    [SerializeField] private TMP_Text logText;

    [SerializeField] private Button assignA1FreeAttackButton;
    [SerializeField] private Button assignA1AbilityButton;
    [SerializeField] private Button assignB1RespondIntent1Button;
    [SerializeField] private Button selectActorAButton;
    [SerializeField] private Button selectActorBButton;
    [SerializeField] private Button selectSlot1Button;
    [SerializeField] private Button selectSlot2Button;
    [SerializeField] private Button selectAttackCardButton;
    [SerializeField] private Button selectDefenseCardButton;
    [SerializeField] private Button selectDodgeCardButton;
    [SerializeField] private Button selectAbilityCardButton;
    [SerializeField] private Button selectClashSinCardButton;
    [SerializeField] private Button selectFreeAttackModeButton;
    [SerializeField] private Button selectRespondIntent1ModeButton;
    [SerializeField] private Button selectPassiveGuardModeButton;
    [SerializeField] private Button confirmAssignSelectedActionButton;
    [SerializeField] private Button clearSelectionButton;
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
    private BattleCardState allyADefenseCardState;
    private BattleCardState allyADodgeCardState;
    private BattleCardState allyAAbilityCardState;
    private BattleCardState allyAClashSinCardState;

    private BattleCardState allyBAttackCardState;
    private BattleCardState allyBDefenseCardState;
    private BattleCardState allyBDodgeCardState;
    private BattleCardState allyBAbilityCardState;
    private BattleCardState allyBClashSinCardState;

    private BattleCardState enemyAttackCardState;

    private const string ActionModeFreeAttack = "FreeAttack";
    private const string ActionModeRespondIntent1 = "RespondIntent1";
    private const string ActionModePassiveGuard = "PassiveGuard";

    private CharacterData selectedActor;
    private int selectedSlotIndex = 1;
    private BattleCardState selectedCardState;
    private string selectedActionMode;

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
        List<BattleActionSlot> initialActionSlots = BattleActionSlotManager.CreatePartyActionSlots(allyA, allyB, 2);
        runtimeState.SetActionSlots(initialActionSlots);
        runtimeState.SetIntentQueue(CreateFixedEnemyIntentQueue(initialActionSlots));
        runtimeState.SetPhase("Prepare");

        lastLog = "初始化完成：已进入 Prepare 阶段";
    }

    void CreateTestCharacters()
    {
        allyA = new CharacterData("我方角色A", 30, 20, 20);
        allyB = new CharacterData("我方角色B", 30, 3, 5);
        enemy = new CharacterData("敌人", 50, 5, 8);
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
        CardTestData defenseCard = CardDataLoader.FindCardByID(cards, "def_001");
        CardTestData dodgeCard = CardDataLoader.FindCardByID(cards, "dodge_001");
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

        allyADefenseCardState = BattleCardManager.CreateBattleCard(
            allyA,
            defenseCard,
            "ui_allyA_def_001_copy_0"
        );

        allyADodgeCardState = BattleCardManager.CreateBattleCard(
            allyA,
            dodgeCard,
            "ui_allyA_dodge_001_copy_0"
        );

        allyAAbilityCardState = BattleCardManager.CreateBattleCard(
            allyA,
            allyAAbilityCard,
            "ui_allyA_sin_ability_001_copy_0"
        );

        // Temporary: atk_001 is reused as clash sin card data until JSON splits normal attack and clash sin cards.
        allyAClashSinCardState = BattleCardManager.CreateBattleCard(
            allyA,
            allyAAttackCard,
            "ui_allyA_clash_sin_atk_001_copy_0"
        );

        allyBAttackCardState = BattleCardManager.CreateBattleCard(
            allyB,
            allyAAttackCard,
            "ui_allyB_atk_001_copy_0"
        );

        allyBDefenseCardState = BattleCardManager.CreateBattleCard(
            allyB,
            defenseCard,
            "ui_allyB_def_001_copy_0"
        );

        allyBDodgeCardState = BattleCardManager.CreateBattleCard(
            allyB,
            dodgeCard,
            "ui_allyB_dodge_001_copy_0"
        );

        allyBAbilityCardState = BattleCardManager.CreateBattleCard(
            allyB,
            allyAAbilityCard,
            "ui_allyB_sin_ability_001_copy_0"
        );

        allyBClashSinCardState = BattleCardManager.CreateBattleCard(
            allyB,
            allyAAttackCard,
            "ui_allyB_clash_sin_atk_001_copy_0"
        );
    }

    List<BattleEnemyIntent> CreateFixedEnemyIntentQueue(List<BattleActionSlot> actionSlots)
    {
        if (enemy == null || enemyAttackCardState == null)
        {
            Debug.LogWarning("创建固定敌人意图失败：敌人 / 敌人卡牌数据不完整");
            return new List<BattleEnemyIntent>();
        }

        int targetSlotIndex;
        CharacterData target = SelectFixedEnemyIntentTarget(allyA, allyB, actionSlots, out targetSlotIndex);

        if (target == null)
        {
            Debug.LogWarning("创建固定敌人意图失败：没有可用的存活目标槽位");
            return new List<BattleEnemyIntent>();
        }

        BattleEnemyIntent enemyIntent = new BattleEnemyIntent(
            "ui_fixed_enemy_intent_001",
            enemy,
            enemyAttackCardState,
            target,
            targetSlotIndex,
            1
        );

        Debug.Log("敌人新意图目标改为：" + target.characterName + " 槽位" + targetSlotIndex);

        return BattleEnemyIntentManager.CreateIntentQueue(enemyIntent);
    }

    internal static CharacterData SelectFixedEnemyIntentTarget(
        CharacterData allyA,
        CharacterData allyB,
        List<BattleActionSlot> actionSlots,
        out int targetSlotIndex
    )
    {
        targetSlotIndex = 1;

        if (allyB != null && !allyB.IsDead())
        {
            return HasOwnerSlot(actionSlots, allyB, targetSlotIndex)
                ? allyB
                : null;
        }

        if (allyA != null && !allyA.IsDead())
        {
            return HasOwnerSlot(actionSlots, allyA, targetSlotIndex)
                ? allyA
                : null;
        }

        return null;
    }

    static bool HasOwnerSlot(List<BattleActionSlot> actionSlots, CharacterData owner, int slotIndex)
    {
        if (actionSlots == null || owner == null)
        {
            return false;
        }

        foreach (BattleActionSlot slot in actionSlots)
        {
            if (slot != null && object.ReferenceEquals(slot.owner, owner) && slot.slotIndex == slotIndex)
            {
                return true;
            }
        }

        Debug.LogWarning("存活目标 " + owner.characterName + " 缺少槽位" + slotIndex + "，不创建敌人意图");
        return false;
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

        if (selectActorAButton != null)
        {
            selectActorAButton.onClick.AddListener(OnClickSelectActorA);
        }

        if (selectActorBButton != null)
        {
            selectActorBButton.onClick.AddListener(OnClickSelectActorB);
        }

        if (selectSlot1Button != null)
        {
            selectSlot1Button.onClick.AddListener(OnClickSelectSlot1);
        }

        if (selectSlot2Button != null)
        {
            selectSlot2Button.onClick.AddListener(OnClickSelectSlot2);
        }

        if (selectAttackCardButton != null)
        {
            selectAttackCardButton.onClick.AddListener(OnClickSelectAttackCard);
        }

        if (selectDefenseCardButton != null)
        {
            selectDefenseCardButton.onClick.AddListener(OnClickSelectDefenseCard);
        }

        if (selectDodgeCardButton != null)
        {
            selectDodgeCardButton.onClick.AddListener(OnClickSelectDodgeCard);
        }

        if (selectAbilityCardButton != null)
        {
            selectAbilityCardButton.onClick.AddListener(OnClickSelectAbilityCard);
        }

        if (selectClashSinCardButton != null)
        {
            selectClashSinCardButton.onClick.AddListener(OnClickSelectClashSinCard);
        }

        if (selectFreeAttackModeButton != null)
        {
            selectFreeAttackModeButton.onClick.AddListener(OnClickSelectFreeAttackMode);
        }

        if (selectRespondIntent1ModeButton != null)
        {
            selectRespondIntent1ModeButton.onClick.AddListener(OnClickSelectRespondIntent1Mode);
        }

        if (selectPassiveGuardModeButton != null)
        {
            selectPassiveGuardModeButton.onClick.AddListener(OnClickSelectPassiveGuardMode);
        }

        if (confirmAssignSelectedActionButton != null)
        {
            confirmAssignSelectedActionButton.onClick.AddListener(OnClickConfirmAssignSelectedAction);
        }

        if (clearSelectionButton != null)
        {
            clearSelectionButton.onClick.AddListener(OnClickClearSelection);
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

        if (selectActorAButton != null)
        {
            selectActorAButton.onClick.RemoveListener(OnClickSelectActorA);
        }

        if (selectActorBButton != null)
        {
            selectActorBButton.onClick.RemoveListener(OnClickSelectActorB);
        }

        if (selectSlot1Button != null)
        {
            selectSlot1Button.onClick.RemoveListener(OnClickSelectSlot1);
        }

        if (selectSlot2Button != null)
        {
            selectSlot2Button.onClick.RemoveListener(OnClickSelectSlot2);
        }

        if (selectAttackCardButton != null)
        {
            selectAttackCardButton.onClick.RemoveListener(OnClickSelectAttackCard);
        }

        if (selectDefenseCardButton != null)
        {
            selectDefenseCardButton.onClick.RemoveListener(OnClickSelectDefenseCard);
        }

        if (selectDodgeCardButton != null)
        {
            selectDodgeCardButton.onClick.RemoveListener(OnClickSelectDodgeCard);
        }

        if (selectAbilityCardButton != null)
        {
            selectAbilityCardButton.onClick.RemoveListener(OnClickSelectAbilityCard);
        }

        if (selectClashSinCardButton != null)
        {
            selectClashSinCardButton.onClick.RemoveListener(OnClickSelectClashSinCard);
        }

        if (selectFreeAttackModeButton != null)
        {
            selectFreeAttackModeButton.onClick.RemoveListener(OnClickSelectFreeAttackMode);
        }

        if (selectRespondIntent1ModeButton != null)
        {
            selectRespondIntent1ModeButton.onClick.RemoveListener(OnClickSelectRespondIntent1Mode);
        }

        if (selectPassiveGuardModeButton != null)
        {
            selectPassiveGuardModeButton.onClick.RemoveListener(OnClickSelectPassiveGuardMode);
        }

        if (confirmAssignSelectedActionButton != null)
        {
            confirmAssignSelectedActionButton.onClick.RemoveListener(OnClickConfirmAssignSelectedAction);
        }

        if (clearSelectionButton != null)
        {
            clearSelectionButton.onClick.RemoveListener(OnClickClearSelection);
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

    private void OnClickSelectActorA()
    {
        TrySelectActor(allyA, "A");
    }

    private void OnClickSelectActorB()
    {
        TrySelectActor(allyB, "B");
    }

    void TrySelectActor(CharacterData actor, string actorLabel)
    {
        if (actor == null)
        {
            ClearSelectedActionState();
            lastLog = "Selected actor failed: actor is null";
            RefreshView();
            return;
        }

        if (actor.IsDead())
        {
            ClearSelectedActionState();
            lastLog = "该角色已经死亡，不能安排行动";
            RefreshView();
            return;
        }

        selectedActor = actor;
        selectedCardState = null;
        lastLog = "Selected actor: " + actorLabel;
        RefreshView();
    }

    private void OnClickSelectSlot1()
    {
        selectedSlotIndex = 1;
        lastLog = "Selected slot: 1";
        RefreshView();
    }

    private void OnClickSelectSlot2()
    {
        selectedSlotIndex = 2;
        lastLog = "Selected slot: 2";
        RefreshView();
    }

    private void OnClickSelectAttackCard()
    {
        SelectCardForCurrentActor(allyAAttackCardState, allyBAttackCardState, "Attack");
    }

    private void OnClickSelectDefenseCard()
    {
        SelectCardForCurrentActor(allyADefenseCardState, allyBDefenseCardState, "Defense");
    }

    private void OnClickSelectDodgeCard()
    {
        SelectCardForCurrentActor(allyADodgeCardState, allyBDodgeCardState, "Dodge");
    }

    private void OnClickSelectAbilityCard()
    {
        SelectCardForCurrentActor(allyAAbilityCardState, allyBAbilityCardState, "Ability");
    }

    private void OnClickSelectClashSinCard()
    {
        SelectCardForCurrentActor(allyAClashSinCardState, allyBClashSinCardState, "ClashSin(atk_001 temp)");
    }

    private void SelectCardForCurrentActor(BattleCardState allyACardState, BattleCardState allyBCardState, string cardLabel)
    {
        if (selectedActor == null)
        {
            lastLog = "Please select actor first";
            RefreshView();
            return;
        }

        if (object.ReferenceEquals(selectedActor, allyA))
        {
            selectedCardState = allyACardState;
        }
        else if (object.ReferenceEquals(selectedActor, allyB))
        {
            selectedCardState = allyBCardState;
        }

        lastLog = "Selected card: " + cardLabel;
        RefreshView();
    }

    private void OnClickSelectFreeAttackMode()
    {
        selectedActionMode = ActionModeFreeAttack;
        lastLog = "Selected mode: FreeAttack";
        RefreshView();
    }

    private void OnClickSelectRespondIntent1Mode()
    {
        selectedActionMode = ActionModeRespondIntent1;
        lastLog = "Selected mode: RespondIntent1";
        RefreshView();
    }

    private void OnClickSelectPassiveGuardMode()
    {
        selectedActionMode = ActionModePassiveGuard;
        lastLog = "Selected mode: PassiveGuard";
        RefreshView();
    }

    private void OnClickConfirmAssignSelectedAction()
    {
        if (!HasRuntimeState())
        {
            return;
        }

        if (!CanEditActionSlots())
        {
            lastLog = runtimeState.IsBattleEnded
                ? "战斗已经结束，无法继续操作"
                : "Cannot edit slots outside Prepare phase or after plan creation";
            RefreshView();
            return;
        }

        if (selectedActor == null)
        {
            lastLog = "Confirm failed: select actor first";
            RefreshView();
            return;
        }

        if (selectedActor.IsDead())
        {
            ClearSelectedActionState();
            lastLog = "该角色已经死亡，不能安排行动";
            RefreshView();
            return;
        }

        if (selectedSlotIndex != 1 && selectedSlotIndex != 2)
        {
            lastLog = "Confirm failed: slot index must be 1 or 2";
            RefreshView();
            return;
        }

        if (selectedCardState == null)
        {
            lastLog = "Confirm failed: select card first";
            RefreshView();
            return;
        }

        if (!object.ReferenceEquals(selectedCardState.owner, selectedActor))
        {
            lastLog = "Confirm failed: selected card does not belong to selected actor";
            RefreshView();
            return;
        }

        if (string.IsNullOrEmpty(selectedActionMode))
        {
            lastLog = "Confirm failed: select action mode first";
            RefreshView();
            return;
        }

        if (IsSelectedDefenseCard())
        {
            if (selectedActionMode == ActionModeFreeAttack)
            {
                lastLog = "防御卡不能以敌人本体作为目标，请选择敌人意图";
                RefreshView();
                return;
            }

            if (selectedActionMode != ActionModeRespondIntent1 && selectedActionMode != ActionModePassiveGuard)
            {
                lastLog = "Defense v1 only supports RespondIntent1";
                RefreshView();
                return;
            }
        }

        if (IsSelectedDodgeCard())
        {
            if (selectedActionMode == ActionModeFreeAttack)
            {
                lastLog = "闪避卡不能以敌人本体作为目标，请选择敌人意图";
                RefreshView();
                return;
            }

            if (selectedActionMode != ActionModeRespondIntent1 && selectedActionMode != ActionModePassiveGuard)
            {
                lastLog = "Dodge v1 only supports RespondIntent1 or PassiveGuard";
                RefreshView();
                return;
            }
        }

        if (selectedActionMode == ActionModePassiveGuard)
        {
            ConfirmAssignSelectedPassiveGuard();
            return;
        }

        if (selectedActionMode == ActionModeFreeAttack)
        {
            ConfirmAssignSelectedFreeAction();
            return;
        }

        if (selectedActionMode == ActionModeRespondIntent1)
        {
            ConfirmAssignSelectedRespondIntent1();
            return;
        }

        lastLog = "Confirm failed: unknown action mode";
        RefreshView();
    }

    private void ConfirmAssignSelectedFreeAction()
    {
        CharacterData target = IsSelectedAbilityCard()
            ? selectedActor
            : enemy;

        if (!CanAssignSelectedCard(target))
        {
            RefreshView();
            return;
        }

        CardEligibilityResult assignResult;
        bool result = BattleActionSlotManager.AssignFreeAction(
            runtimeState.actionSlots,
            selectedActor,
            selectedSlotIndex,
            selectedActor,
            selectedCardState,
            target,
            out assignResult
        );

        lastLog = result
            ? GetSelectedActorLabel() + " slot " + selectedSlotIndex + " assigned FreeAction: " + selectedCardState.GetCardName()
            : "Assign failed: " + assignResult.failureMessage;

        RefreshView();
    }

    private void ConfirmAssignSelectedPassiveGuard()
    {
        if (!HasRuntimeState())
        {
            return;
        }

        if (!CanEditActionSlots())
        {
            lastLog = runtimeState.IsBattleEnded
                ? "战斗已经结束，无法继续操作"
                : "Cannot edit slots outside Prepare phase or after plan creation";
            RefreshView();
            return;
        }

        if (selectedActor == null)
        {
            lastLog = "Confirm failed: select actor first";
            RefreshView();
            return;
        }

        if (selectedSlotIndex != 1 && selectedSlotIndex != 2)
        {
            lastLog = "Confirm failed: slot index must be 1 or 2";
            RefreshView();
            return;
        }

        if (selectedCardState == null || selectedCardState.cardData == null)
        {
            lastLog = "Confirm failed: select card first";
            RefreshView();
            return;
        }

        if (IsSelectedClashSinCard())
        {
            lastLog = "拼点罪卡不能作为被动守备";
            RefreshView();
            return;
        }

        if (IsSelectedAttackCard())
        {
            lastLog = "攻击卡不能作为被动守备";
            RefreshView();
            return;
        }

        if (IsSelectedAbilityCard())
        {
            lastLog = "能力牌不能作为被动守备";
            RefreshView();
            return;
        }

        if (!IsSelectedDefenseCard() && !IsSelectedDodgeCard())
        {
            lastLog = "PassiveGuard v1 only supports Defense or Dodge";
            RefreshView();
            return;
        }

        if (!CanAssignSelectedCard(selectedActor))
        {
            RefreshView();
            return;
        }

        CardEligibilityResult assignResult;
        bool result = BattleActionSlotManager.AssignPassiveGuard(
            runtimeState.actionSlots,
            selectedActor,
            selectedSlotIndex,
            selectedActor,
            selectedCardState,
            out assignResult
        );

        lastLog = result
            ? GetSelectedActorLabel() + " slot " + selectedSlotIndex + " assigned PassiveGuard: " + selectedCardState.GetCardName()
            : "Assign failed: " + assignResult.failureMessage;

        RefreshView();
    }

    private void ConfirmAssignSelectedRespondIntent1()
    {
        if (IsSelectedAbilityCard())
        {
            lastLog = "Ability v1 cannot respond to enemy intent";
            RefreshView();
            return;
        }

        if (!IsSelectedAttackCard() && !IsSelectedDefenseCard() && !IsSelectedDodgeCard())
        {
            lastLog = "RespondIntent1 v1 only supports Attack, ClashSin, Defense, or Dodge";
            RefreshView();
            return;
        }

        BattleEnemyIntent intent = BattleEnemyIntentManager.FindIntentByOrder(runtimeState.intentQueue, 1);

        if (intent == null)
        {
            lastLog = "Enemy intent 1 not found";
            RefreshView();
            return;
        }

        if (!CanAssignSelectedCard(intent.enemy))
        {
            RefreshView();
            return;
        }

        CardEligibilityResult assignResult;
        bool result = BattleActionSlotManager.AssignResponseToEnemyIntent(
            runtimeState.actionSlots,
            selectedActor,
            selectedSlotIndex,
            selectedActor,
            selectedCardState,
            intent,
            out assignResult
        );

        lastLog = result
            ? GetSelectedActorLabel() + " slot " + selectedSlotIndex + " assigned RespondIntent1: " + selectedCardState.GetCardName()
            : "Assign failed: " + assignResult.failureMessage;

        RefreshView();
    }

    private void OnClickClearSelection()
    {
        ClearSelectedActionState();
        lastLog = "Selection cleared";
        RefreshView();
    }

    void ClearSelectedActionState()
    {
        selectedActor = null;
        selectedSlotIndex = 1;
        selectedCardState = null;
        selectedActionMode = null;
    }

    private void OnClickBattleStart()
    {
        if (runtimeState == null)
        {
            lastLog = "战斗状态未初始化，无法开始战斗";
            RefreshView();
            return;
        }

        if (runtimeState.IsBattleEnded)
        {
            lastLog = "战斗已经结束，不能再次开始战斗";
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

            BattleExecutionPlanExecutor.ExecuteExecutionPlan(runtimeState.currentExecutionPlan, runtimeState);

            if (!runtimeState.IsBattleEnded)
            {
                if (IsCurrentPlanCompleted())
                {
                    runtimeState.SetPhase("Completed");
                    lastLog = "战斗开始：已执行当前已有计划";
                }
                else
                {
                    lastLog = "ExecutionPlan仍有未完成项，不能进入Completed";
                }
            }

            if (runtimeState.IsBattleEnded)
            {
                lastLog = "战斗结束：" + runtimeState.battleResult;
            }

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

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(runtimeState.currentExecutionPlan, runtimeState);

        if (!runtimeState.IsBattleEnded)
        {
            if (IsCurrentPlanCompleted())
            {
                runtimeState.SetPhase("Completed");
                lastLog = "战斗开始：已生成并执行本回合计划";
            }
            else
            {
                lastLog = "ExecutionPlan仍有未完成项，不能进入Completed";
            }
        }

        if (runtimeState.IsBattleEnded)
        {
            lastLog = "战斗结束：" + runtimeState.battleResult;
        }

        RefreshView();
    }

    private void OnClickCreateExecutionPlan()
    {
        if (!HasRuntimeState())
        {
            return;
        }

        if (runtimeState.IsBattleEnded)
        {
            lastLog = "战斗已经结束，无法继续操作";
            RefreshView();
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

        if (runtimeState.IsBattleEnded)
        {
            lastLog = "战斗已经结束，无法继续操作";
            RefreshView();
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

        BattleExecutionPlanExecutor.ExecuteExecutionPlan(runtimeState.currentExecutionPlan, runtimeState);

        if (!runtimeState.IsBattleEnded)
        {
            if (IsCurrentPlanCompleted())
            {
                runtimeState.SetPhase("Completed");
                lastLog = "执行计划已执行，plan.isCompleted = " + runtimeState.currentExecutionPlan.isCompleted;
            }
            else
            {
                lastLog = "ExecutionPlan仍有未完成项，不能进入Completed";
            }
        }

        if (runtimeState.IsBattleEnded)
        {
            lastLog = "战斗结束：" + runtimeState.battleResult;
        }

        RefreshView();
    }

    private void OnClickEndTurn()
    {
        if (!HasRuntimeState())
        {
            return;
        }

        if (runtimeState.IsBattleEnded)
        {
            lastLog = "战斗已经结束，无法继续操作";
            RefreshView();
            return;
        }

        if (!CanEndTurn())
        {
            lastLog = HasCurrentPlan() && !IsCurrentPlanCompleted()
                ? "执行计划尚未完成，不能结束回合"
                : "当前不能结束回合，请先完成战斗结算";
            RefreshView();
            return;
        }

        runtimeState.EndCurrentTurnAndClearRuntimeObjects();
        ClearSelectedActionState();
        lastLog = "当前回合已结束，临时对象已清理";
        RefreshView();
    }

    private void OnClickPrepareNextTurn()
    {
        if (!HasRuntimeState())
        {
            return;
        }

        if (runtimeState.IsBattleEnded)
        {
            lastLog = "战斗已经结束，无法继续操作";
            RefreshView();
            return;
        }

        if (!CanPrepareNextTurn())
        {
            lastLog = "当前不能准备下一回合，请先结束当前回合";
            RefreshView();
            return;
        }

        List<BattleActionSlot> newActionSlots = BattleActionSlotManager.CreateLivingPartyActionSlots(allyA, allyB, 2);

        if (newActionSlots == null || newActionSlots.Count == 0)
        {
            runtimeState.EvaluateBattleEnd();
            lastLog = runtimeState.IsBattleEnded
                ? "战斗已经结束，无法继续操作"
                : "没有存活角色行动槽位，不能准备下一回合";
            RefreshView();
            return;
        }

        List<BattleEnemyIntent> newIntentQueue = CreateFixedEnemyIntentQueue(newActionSlots);

        runtimeState.PrepareNextTurnWithRuntimeObjects(newActionSlots, newIntentQueue);

        if (IsPhase("Prepare"))
        {
            ClearSelectedActionState();
            lastLog = "下一回合已准备，阶段：Prepare";
        }
        else
        {
            lastLog = "准备下一回合失败";
        }

        RefreshView();
    }

    private void RefreshView()
    {
        BattleStateViewData viewData = BattleStateViewData.FromRuntimeState(runtimeState);

        SetText(
            topInfoText,
            "回合：" + viewData.currentTurn +
            "\n阶段：" + viewData.currentPhase +
            "\n战斗结果：" + viewData.battleResult
        );
        SetText(enemyStateText, FormatEnemyState(viewData));
        SetText(allyAStateText, FormatAllyState("A", viewData.allyAName, viewData.allyAHP, viewData.allyAMaxHP, viewData.allyASpeed, viewData.allyAGuilt));
        SetText(allyBStateText, FormatAllyState("B", viewData.allyBName, viewData.allyBHP, viewData.allyBMaxHP, viewData.allyBSpeed, viewData.allyBGuilt));
        SetText(intentListText, FormatIntentList(viewData));
        RefreshActionSlotTexts(viewData);
        RefreshSelectionInfo();
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
        return runtimeState != null && !runtimeState.IsBattleEnded && IsPhase("Prepare") && !HasCurrentPlan();
    }

    private bool CanCreatePlan()
    {
        return runtimeState != null && !runtimeState.IsBattleEnded && IsPhase("Prepare") && !HasCurrentPlan();
    }

    private bool CanExecutePlan()
    {
        return runtimeState != null && !runtimeState.IsBattleEnded && HasCurrentPlan() && !runtimeState.currentExecutionPlan.isCompleted;
    }

    private bool CanEndTurn()
    {
        return runtimeState != null &&
            !runtimeState.IsBattleEnded &&
            IsPhase("Completed") &&
            HasCurrentPlan() &&
            IsCurrentPlanCompleted();
    }

    private bool CanPrepareNextTurn()
    {
        return runtimeState != null && !runtimeState.IsBattleEnded && IsPhase("TurnEnded");
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

    void RefreshSelectionInfo()
    {
        string actorText = selectedActor != null
            ? GetSelectedActorLabel()
            : "NoActor";

        string slotText = "Slot" + selectedSlotIndex;
        string cardText = GetSelectedCardText();
        string modeText = GetSelectedActionModeText();

        SetText(
            selectionInfoText,
            "Current selection:\n" +
            actorText +
            " / " +
            slotText +
            " / " +
            cardText +
            " / " +
            modeText
        );
    }

    string GetSelectedActorLabel()
    {
        if (object.ReferenceEquals(selectedActor, allyA))
        {
            return "A";
        }

        if (object.ReferenceEquals(selectedActor, allyB))
        {
            return "B";
        }

        return selectedActor != null ? selectedActor.characterName : "NoActor";
    }

    string GetSelectedCardText()
    {
        if (selectedCardState == null)
        {
            return "NoCard";
        }

        if (IsSelectedDefenseCard())
        {
            return "Defense / " + selectedCardState.GetCardName();
        }

        if (IsSelectedDodgeCard())
        {
            return "Dodge / " + selectedCardState.GetCardName();
        }

        if (IsSelectedAbilityCard())
        {
            return "Ability / " + selectedCardState.GetCardName();
        }

        if (IsSelectedClashSinCard())
        {
            return "ClashSin / " + selectedCardState.GetCardName();
        }

        if (IsSelectedAttackCard())
        {
            return "Attack / " + selectedCardState.GetCardName();
        }

        return selectedCardState.GetCardName();
    }

    string GetSelectedActionModeText()
    {
        if (selectedActionMode == ActionModeFreeAttack)
        {
            return "FreeAttack";
        }

        if (selectedActionMode == ActionModeRespondIntent1)
        {
            return "RespondIntent1";
        }

        if (selectedActionMode == ActionModePassiveGuard)
        {
            return "PassiveGuard";
        }

        return "NoMode";
    }

    bool IsSelectedAbilityCard()
    {
        return selectedCardState != null &&
            selectedCardState.cardData != null &&
            selectedCardState.cardData.cardType == "Ability";
    }

    bool IsSelectedDefenseCard()
    {
        return selectedCardState != null &&
            selectedCardState.cardData != null &&
            selectedCardState.cardData.cardType == "Defense";
    }

    bool IsSelectedDodgeCard()
    {
        return selectedCardState != null &&
            selectedCardState.cardData != null &&
            selectedCardState.cardData.cardType == "Dodge";
    }

    bool IsSelectedClashSinCard()
    {
        return object.ReferenceEquals(selectedCardState, allyAClashSinCardState) ||
            object.ReferenceEquals(selectedCardState, allyBClashSinCardState);
    }

    bool IsSelectedAttackCard()
    {
        return selectedCardState != null &&
            selectedCardState.cardData != null &&
            selectedCardState.cardData.cardType == "Attack";
    }

    bool CanAssignSelectedCard(CharacterData target)
    {
        CardEligibilityResult result = BattleCardManager.EvaluateCardEligibility(
            selectedActor,
            target,
            selectedCardState
        );

        if (!result.isEligible)
        {
            lastLog = "Assign failed: " + result.failureMessage;
            return false;
        }

        return true;
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
