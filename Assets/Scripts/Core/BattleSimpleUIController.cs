// 脚本中文说明：简易战斗 UI 控制器。连接手动搭建的 TMP 文本和按钮，用 RuntimeState / ViewData 刷新界面。
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BattleSimpleUIController : MonoBehaviour
{
    internal const string ClashSinTestCardID = "sin_attack_test_001";

    internal sealed class LegacyCardReferenceSet
    {
        public BattleCardState allyAAttack;
        public BattleCardState allyABulletAttack;
        public BattleCardState allyADefense;
        public BattleCardState allyADodge;
        public BattleCardState allyAAbility;
        public BattleCardState allyASinAttack;
        public BattleCardState allyBAttack;
        public BattleCardState allyBDefense;
        public BattleCardState allyBDodge;
        public BattleCardState allyBAbility;
        public BattleCardState allyBSinAttack;
        public BattleCardState enemyAttack;
        public BattleCardState enemy02Attack;
    }

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
    [SerializeField] private BattleCardUIView testCardView;
    [SerializeField] private BattleCardHandUIView testCardHandView;
    [SerializeField] private BattleRoundUIView roundView;
    [SerializeField] private BattleGuiltUIView guiltView;
    [SerializeField]
    private BattleActionRelationLineController actionRelationLineController;
    [SerializeField] private BattleUnitViewSpawner unitViewSpawner;
    [SerializeField]
    private BattleSceneExecutionPresenter sceneExecutionPresenter;

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
    [SerializeField] private Button qiehuanButton;

    private BattleRuntimeState runtimeState;
    private BattleLifecycleController lifecycleController;

    private CharacterData ally01;
    private CharacterData ally02;
    private CharacterData enemy01;
    private CharacterData enemy02;

    // 四个状态View只能由Spawner生成的Handle提供，不再接受场景静态引用。
    private BattleCharacterStatusUIView ally01StatusView;
    private BattleCharacterStatusUIView ally02StatusView;
    private BattleCharacterStatusUIView enemy01StatusView;
    private BattleCharacterStatusUIView enemy02StatusView;
    private bool runtimeUnitViewsReady;
    private bool warnedRuntimeUnitViewsUnavailable;
    private bool isInitialized;
    private bool isInitializing;
    private bool buttonEventsBound;

    private BattleCardState allyAAttackCardState;
    private BattleCardState allyABulletAttackCardState;
    private BattleCardState allyADefenseCardState;
    private BattleCardState allyADodgeCardState;
    private BattleCardState allyAAbilityCardState;
    private BattleCardState allyAClashSinCardState;
    private BattleCardState allyASinAttackCardState;

    private BattleCardState allyBAttackCardState;
    private BattleCardState allyBDefenseCardState;
    private BattleCardState allyBDodgeCardState;
    private BattleCardState allyBAbilityCardState;
    private BattleCardState allyBClashSinCardState;

    private BattleCardState enemyAttackCardState;
    private BattleCardState enemy02AttackCardState;

    private const string ActionModeFreeAttack = "FreeAttack";
    private const string ActionModeRespondIntent1 = "RespondIntent1";
    private const string ActionModePassiveGuard = "PassiveGuard";

    private CharacterData selectedActor;
    private int selectedSlotIndex;
    private BattleCardState selectedCardState;
    private string selectedActionMode;
    private bool showingSinCards = false;

    private bool isRunningCompleteTurnCycle;
    private bool isScenePresentedTurnCycleRunning;
    private BattleAutomaticTurnCycleResult scenePresentedTurnCycleResult;
    private bool terminalInteractionStateCleared;
    private readonly BattleCardSelectionController cardSelectionController =
        new BattleCardSelectionController();
    private BattleCardInteractionCoordinator cardInteractionCoordinator;
    private readonly List<RaycastResult> planningCancelRaycastResults =
        new List<RaycastResult>();

    private readonly string[] normalTestHandCardIDs =
    {
        "atk_001",
        "def_001",
        "dodge_001",
        "atk_bullet_001"
    };

    private readonly string[] sinTestHandCardIDs =
    {
        "sin_ability_001",
        "sin_attack_test_001"
    };

    private string lastLog = "等待初始化";

    public bool IsInitialized => isInitialized;
    public bool IsInitializing => isInitializing;
    public bool ButtonEventsBound => buttonEventsBound;
    public BattleRuntimeState RuntimeState => runtimeState;
    public CharacterData Ally01 => ally01;
    public CharacterData Ally02 => ally02;
    public CharacterData Enemy01 => enemy01;
    public CharacterData Enemy02 => enemy02;
    internal bool HasPlanningSlotSelection =>
        cardInteractionCoordinator != null &&
        cardInteractionCoordinator.SelectedActionSlotView != null;
    internal CharacterData PlanningHandOwner =>
        testCardHandView != null
            ? testCardHandView.LastDisplayedOwner
            : null;
    internal int VisiblePlanningCardCount =>
        testCardHandView != null
            ? testCardHandView.SpawnedCardViews.Count
            : 0;

    void Awake()
    {
        cardInteractionCoordinator =
            new BattleCardInteractionCoordinator(cardSelectionController);
        cardInteractionCoordinator.SourceSlotSelectionChanged +=
            OnSourceSlotSelectionChanged;
        cardSelectionController.SelectionChanged +=
            OnCardSelectionChanged;
    }

    void Update()
    {
        if (isScenePresentedTurnCycleRunning)
        {
            AdvanceScenePresentedTurnCycle();
        }

        HandleBlankAreaRightClick();
    }

    private void HandleBlankAreaRightClick()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.rightButton.wasPressedThisFrame)
        {
            return;
        }

        if (!IsBattleBlankArea(mouse.position.ReadValue()))
        {
            return;
        }

        // 只清理规划期临时 UI，不修改已经写入 Runtime 的行动安排。
        ClearPlanningSelectionAndHideCards();
        BattleActionSlotCardInfoPanelHost.CloseAllPanels();
    }

    private bool IsBattleBlankArea(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return true;
        }

        planningCancelRaycastResults.Clear();
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = screenPosition
        };
        eventSystem.RaycastAll(pointerData, planningCancelRaycastResults);

        for (int index = 0;
            index < planningCancelRaycastResults.Count;
            index++)
        {
            GameObject hitObject =
                planningCancelRaycastResults[index].gameObject;
            if (IsPlanningInteractionUI(hitObject))
            {
                planningCancelRaycastResults.Clear();
                return false;
            }
        }

        planningCancelRaycastResults.Clear();
        return true;
    }

    private static bool IsPlanningInteractionUI(GameObject hitObject)
    {
        Transform current = hitObject != null
            ? hitObject.transform
            : null;
        while (current != null)
        {
            GameObject currentObject = current.gameObject;
            if (currentObject.GetComponent<Selectable>() != null ||
                currentObject.GetComponent<IPointerClickHandler>() != null ||
                currentObject.GetComponent<IBeginDragHandler>() != null ||
                currentObject.GetComponent<IDragHandler>() != null ||
                currentObject.GetComponent<IScrollHandler>() != null)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    void OnDestroy()
    {
        cardSelectionController.SelectionChanged -=
            OnCardSelectionChanged;
        if (cardInteractionCoordinator != null)
        {
            cardInteractionCoordinator.SourceSlotSelectionChanged -=
                OnSourceSlotSelectionChanged;
        }
        if (testCardHandView != null)
        {
            testCardHandView.SetSelectionController(null);
        }

        cardInteractionCoordinator?.ClearAllSelections();
        if (unitViewSpawner != null)
        {
            unitViewSpawner.GeneratedViewsCleared -=
                OnRuntimeUnitViewsCleared;
        }
        unitViewSpawner?.ClearGeneratedViews();
        ClearRuntimeStatusViewReferences();
        actionRelationLineController?.ClearAll();
        UnbindButtonEvents();
    }

    // 正式战斗只消费Definition已经创建完成的RuntimeState，不在Controller内补造数据。
    public bool InitializeFromRuntimeState(
        BattleRuntimeState initializedRuntimeState
    )
    {
        if (!TryBeginInitialization("正式RuntimeState"))
        {
            return false;
        }

        try
        {
            string errorMessage;
            LegacyCardReferenceSet cardReferences;

            if (!ValidateRuntimeStateForInitialization(
                    initializedRuntimeState,
                    out errorMessage) ||
                !TryResolveLegacyCardReferences(
                    initializedRuntimeState,
                    out cardReferences,
                    out errorMessage))
            {
                ShowInitializationFailure(errorMessage);
                return false;
            }

            BindRuntimeReferences(initializedRuntimeState, cardReferences);

            if (!CompletePresentationInitialization())
            {
                ShowInitializationFailure(lastLog);
                return false;
            }

            lastLog = "正式战斗初始化完成：已进入 Prepare 阶段";
            RefreshView();
            isInitialized = true;
            return true;
        }
        finally
        {
            isInitializing = false;
        }
    }

    public bool InitializeDebugTestBattle()
    {
        if (!TryBeginInitialization("Debug测试战斗"))
        {
            return false;
        }

        Debug.LogWarning(
            "BattleScene正在使用Debug测试初始化，未使用正式Encounter Definition。",
            this
        );

        try
        {
            if (!InitializeTestBattleData())
            {
                ShowInitializationFailure(lastLog);
                return false;
            }

            if (!CompletePresentationInitialization())
            {
                ShowInitializationFailure(lastLog);
                return false;
            }

            RefreshView();
            isInitialized = true;
            return true;
        }
        finally
        {
            isInitializing = false;
        }
    }

    public void ShowInitializationFailure(string message)
    {
        lastLog = string.IsNullOrEmpty(message)
            ? "BattleScene初始化失败：未知错误"
            : message;
        Debug.LogError(lastLog, this);

        cardInteractionCoordinator?.ClearAllSelections();
        ClearSelectedActionState();
        testCardHandView?.SetSelectionController(null);
        UnbindButtonEvents();

        if (unitViewSpawner != null)
        {
            unitViewSpawner.ClearGeneratedViews();
        }

        ClearRuntimeStatusViewReferences();
        actionRelationLineController?.ClearAll();
        ClearRuntimeAndLegacyReferences();
        isInitialized = false;
        RefreshView();
    }

    private bool TryBeginInitialization(string initializationName)
    {
        if (isInitialized || isInitializing)
        {
            Debug.LogWarning(
                "BattleSimpleUIController拒绝重复初始化：" +
                initializationName,
                this
            );
            return false;
        }

        isInitializing = true;
        return true;
    }

    private bool CompletePresentationInitialization()
    {
        if (!SpawnRuntimeUnitViewsOnce() ||
            unitViewSpawner == null ||
            !unitViewSpawner.IsSpawned)
        {
            lastLog = "BattleScene初始化失败：运行时角色表现生成未完成";
            unitViewSpawner?.ClearGeneratedViews();
            ClearRuntimeStatusViewReferences();
            actionRelationLineController?.ClearAll();
            return false;
        }

        if (sceneExecutionPresenter != null)
        {
            // 正式角色表现生成完成后，再把同一个Spawner交给场景Presenter建立角色映射。
            sceneExecutionPresenter.Initialize(unitViewSpawner);
        }

        BindCharacterStatusSlotInteractions();
        BindCardHandInteractions();
        BindButtonEvents();
        // 初始化与每场战斗首次进入Prepare都不默认选择角色或展示手牌。
        ClearPlanningSelectionAndHideCards();
        return true;
    }

    internal static bool ValidateRuntimeStateForInitialization(
        BattleRuntimeState initializedRuntimeState,
        out string errorMessage
    )
    {
        errorMessage = string.Empty;

        if (initializedRuntimeState == null)
        {
            errorMessage = "BattleScene初始化失败：BattleRuntimeState为空";
            return false;
        }

        CharacterData allyA = initializedRuntimeState.allyA;
        CharacterData allyB = initializedRuntimeState.allyB;
        CharacterData enemy = initializedRuntimeState.enemy;
        CharacterData enemy2 = initializedRuntimeState.enemy2;

        if (allyA == null || allyB == null || enemy == null || enemy2 == null)
        {
            errorMessage = "BattleScene初始化失败：固定2+2角色引用不完整";
            return false;
        }

        if (!HasExactUnitReferences(
                initializedRuntimeState.allyUnits,
                allyA,
                allyB))
        {
            errorMessage = "BattleScene初始化失败：allyUnits必须恰好包含allyA和allyB";
            return false;
        }

        if (!HasExactUnitReferences(
                initializedRuntimeState.enemyUnits,
                enemy,
                enemy2))
        {
            errorMessage = "BattleScene初始化失败：enemyUnits必须恰好包含enemy和enemy2";
            return false;
        }

        CharacterData[] units = { allyA, allyB, enemy, enemy2 };
        if (!HasFourDistinctBattleUnits(
                initializedRuntimeState.battleUnits,
                units,
                out errorMessage) ||
            !HasUniqueRuntimeUnitIDs(units, out errorMessage) ||
            !HasValidInitialActionSlots(
                initializedRuntimeState.actionSlots,
                allyA,
                allyB,
                out errorMessage))
        {
            return false;
        }

        if (initializedRuntimeState.intentQueue == null)
        {
            errorMessage = "BattleScene初始化失败：intentQueue为空";
            return false;
        }

        if (initializedRuntimeState.LifecyclePhase !=
            BattleLifecyclePhase.Prepare)
        {
            errorMessage =
                "BattleScene初始化失败：初始阶段必须为Prepare，当前为" +
                initializedRuntimeState.currentPhase;
            return false;
        }

        for (int unitIndex = 0; unitIndex < units.Length; unitIndex++)
        {
            CharacterData unit = units[unitIndex];
            if (unit.battleCards == null)
            {
                errorMessage =
                    "BattleScene初始化失败：" + unit.runtimeUnitID +
                    " 的battleCards为空";
                return false;
            }

            for (int cardIndex = 0;
                cardIndex < unit.battleCards.Count;
                cardIndex++)
            {
                BattleCardState cardState = unit.battleCards[cardIndex];
                if (cardState == null ||
                    cardState.cardData == null ||
                    !object.ReferenceEquals(cardState.owner, unit))
                {
                    errorMessage =
                        "BattleScene初始化失败：" + unit.runtimeUnitID +
                        " 的卡牌owner或CardData无效，索引=" + cardIndex;
                    return false;
                }
            }
        }

        return true;
    }

    internal static bool TryResolveLegacyCardReferences(
        BattleRuntimeState initializedRuntimeState,
        out LegacyCardReferenceSet references,
        out string errorMessage
    )
    {
        references = new LegacyCardReferenceSet();
        errorMessage = string.Empty;

        if (initializedRuntimeState == null)
        {
            errorMessage = "BattleScene兼容卡牌绑定失败：RuntimeState为空";
            return false;
        }

        CharacterData allyA = initializedRuntimeState.allyA;
        CharacterData allyB = initializedRuntimeState.allyB;
        CharacterData enemy = initializedRuntimeState.enemy;
        CharacterData enemy2 = initializedRuntimeState.enemy2;

        if (!TryRequireCard(allyA, "atk_001", out references.allyAAttack, out errorMessage) ||
            !TryRequireCard(allyA, "def_001", out references.allyADefense, out errorMessage) ||
            !TryRequireCard(allyA, "dodge_001", out references.allyADodge, out errorMessage) ||
            !TryRequireCard(allyA, "sin_ability_001", out references.allyAAbility, out errorMessage) ||
            !TryRequireCard(allyA, ClashSinTestCardID, out references.allyASinAttack, out errorMessage) ||
            !TryRequireCard(allyB, "atk_001", out references.allyBAttack, out errorMessage) ||
            !TryRequireCard(allyB, "def_001", out references.allyBDefense, out errorMessage) ||
            !TryRequireCard(allyB, "dodge_001", out references.allyBDodge, out errorMessage) ||
            !TryRequireCard(allyB, "sin_ability_001", out references.allyBAbility, out errorMessage) ||
            !TryRequireCard(allyB, ClashSinTestCardID, out references.allyBSinAttack, out errorMessage))
        {
            return false;
        }

        // 单卡预览是兼容调试入口，正式卡组没有基础射击时保持空白。
        references.allyABulletAttack = FindOwnedCardByID(
            allyA,
            "atk_bullet_001"
        );
        references.enemyAttack = FindIntentCardForEnemy(
            initializedRuntimeState.intentQueue,
            enemy
        ) ?? FindOwnedCardByID(enemy, "enemy_atk_001");
        references.enemy02Attack = FindIntentCardForEnemy(
            initializedRuntimeState.intentQueue,
            enemy2
        ) ?? FindOwnedCardByID(enemy2, "enemy_atk_001");

        if (!ValidateEnemyCardReference(
                enemy,
                references.enemyAttack,
                out errorMessage) ||
            !ValidateEnemyCardReference(
                enemy2,
                references.enemy02Attack,
                out errorMessage))
        {
            return false;
        }

        if (object.ReferenceEquals(
                references.enemyAttack,
                references.enemy02Attack))
        {
            errorMessage = "BattleScene兼容卡牌绑定失败：两名敌人共享同一BattleCardState";
            return false;
        }

        return true;
    }

    private static bool HasExactUnitReferences(
        List<CharacterData> units,
        CharacterData first,
        CharacterData second
    )
    {
        return units != null &&
            units.Count == 2 &&
            object.ReferenceEquals(units[0], first) &&
            object.ReferenceEquals(units[1], second);
    }

    private static bool HasFourDistinctBattleUnits(
        List<CharacterData> battleUnits,
        CharacterData[] expectedUnits,
        out string errorMessage
    )
    {
        errorMessage = string.Empty;
        if (battleUnits == null || battleUnits.Count != 4)
        {
            errorMessage = "BattleScene初始化失败：battleUnits必须恰好为4个";
            return false;
        }

        for (int index = 0; index < expectedUnits.Length; index++)
        {
            if (!object.ReferenceEquals(battleUnits[index], expectedUnits[index]))
            {
                errorMessage = "BattleScene初始化失败：battleUnits顺序或引用不匹配";
                return false;
            }

            for (int otherIndex = index + 1;
                otherIndex < expectedUnits.Length;
                otherIndex++)
            {
                if (object.ReferenceEquals(
                        expectedUnits[index],
                        expectedUnits[otherIndex]))
                {
                    errorMessage = "BattleScene初始化失败：battleUnits存在重复角色引用";
                    return false;
                }
            }
        }

        return true;
    }

    private static bool HasUniqueRuntimeUnitIDs(
        CharacterData[] units,
        out string errorMessage
    )
    {
        errorMessage = string.Empty;
        HashSet<string> ids = new HashSet<string>();
        for (int index = 0; index < units.Length; index++)
        {
            string runtimeUnitID = units[index].runtimeUnitID;
            if (string.IsNullOrEmpty(runtimeUnitID) || !ids.Add(runtimeUnitID))
            {
                errorMessage =
                    "BattleScene初始化失败：runtimeUnitID为空或重复：" +
                    runtimeUnitID;
                return false;
            }
        }

        return true;
    }

    private static bool HasValidInitialActionSlots(
        List<BattleActionSlot> actionSlots,
        CharacterData allyA,
        CharacterData allyB,
        out string errorMessage
    )
    {
        errorMessage = string.Empty;
        if (actionSlots == null || actionSlots.Count != 4)
        {
            errorMessage = "BattleScene初始化失败：初始友方行动槽必须恰好为4个";
            return false;
        }

        bool allyA1 = false;
        bool allyA2 = false;
        bool allyB1 = false;
        bool allyB2 = false;

        for (int index = 0; index < actionSlots.Count; index++)
        {
            BattleActionSlot slot = actionSlots[index];
            if (slot == null || !slot.IsEmpty() || slot.actor != null)
            {
                errorMessage = "BattleScene初始化失败：初始行动槽为空引用或已经被安排";
                return false;
            }

            if (object.ReferenceEquals(slot.owner, allyA))
            {
                if (slot.slotIndex == 1 && !allyA1) allyA1 = true;
                else if (slot.slotIndex == 2 && !allyA2) allyA2 = true;
                else
                {
                    errorMessage = "BattleScene初始化失败：allyA行动槽编号重复或非法";
                    return false;
                }
            }
            else if (object.ReferenceEquals(slot.owner, allyB))
            {
                if (slot.slotIndex == 1 && !allyB1) allyB1 = true;
                else if (slot.slotIndex == 2 && !allyB2) allyB2 = true;
                else
                {
                    errorMessage = "BattleScene初始化失败：allyB行动槽编号重复或非法";
                    return false;
                }
            }
            else
            {
                errorMessage = "BattleScene初始化失败：初始行动槽包含非友方owner";
                return false;
            }
        }

        return allyA1 && allyA2 && allyB1 && allyB2;
    }

    private static bool TryRequireCard(
        CharacterData owner,
        string cardID,
        out BattleCardState cardState,
        out string errorMessage
    )
    {
        cardState = FindOwnedCardByID(owner, cardID);
        if (cardState != null)
        {
            errorMessage = string.Empty;
            return true;
        }

        string ownerID = owner != null
            ? owner.runtimeUnitID
            : "<null>";
        errorMessage =
            "BattleScene兼容卡牌绑定失败：角色 " + ownerID +
            " 缺少 " + cardID;
        return false;
    }

    private static BattleCardState FindOwnedCardByID(
        CharacterData owner,
        string cardID
    )
    {
        if (owner == null || owner.battleCards == null)
        {
            return null;
        }

        for (int index = 0; index < owner.battleCards.Count; index++)
        {
            BattleCardState cardState = owner.battleCards[index];
            if (cardState != null &&
                object.ReferenceEquals(cardState.owner, owner) &&
                cardState.cardData != null &&
                cardState.cardData.cardID == cardID)
            {
                return cardState;
            }
        }

        return null;
    }

    private static BattleCardState FindIntentCardForEnemy(
        List<BattleEnemyIntent> intentQueue,
        CharacterData enemy
    )
    {
        if (intentQueue == null || enemy == null)
        {
            return null;
        }

        for (int index = 0; index < intentQueue.Count; index++)
        {
            BattleEnemyIntent intent = intentQueue[index];
            if (intent != null &&
                object.ReferenceEquals(intent.enemy, enemy) &&
                intent.enemyCardState != null &&
                object.ReferenceEquals(intent.enemyCardState.owner, enemy))
            {
                return intent.enemyCardState;
            }
        }

        return null;
    }

    private static bool ValidateEnemyCardReference(
        CharacterData enemy,
        BattleCardState cardState,
        out string errorMessage
    )
    {
        if (enemy != null &&
            cardState != null &&
            cardState.cardData != null &&
            object.ReferenceEquals(cardState.owner, enemy))
        {
            errorMessage = string.Empty;
            return true;
        }

        errorMessage =
            "BattleScene兼容卡牌绑定失败：敌人 " +
            (enemy != null ? enemy.runtimeUnitID : "<null>") +
            " 缺少可用的enemy_atk_001正式实例";
        return false;
    }

    private void BindRuntimeReferences(
        BattleRuntimeState initializedRuntimeState,
        LegacyCardReferenceSet references
    )
    {
        runtimeState = initializedRuntimeState;
        lifecycleController = CreateLifecycleController(runtimeState);
        terminalInteractionStateCleared = false;
        ally01 = runtimeState.allyA;
        ally02 = runtimeState.allyB;
        enemy01 = runtimeState.enemy;
        enemy02 = runtimeState.enemy2;

        allyAAttackCardState = references.allyAAttack;
        allyABulletAttackCardState = references.allyABulletAttack;
        allyADefenseCardState = references.allyADefense;
        allyADodgeCardState = references.allyADodge;
        allyAAbilityCardState = references.allyAAbility;
        allyASinAttackCardState = references.allyASinAttack;
        allyAClashSinCardState = references.allyASinAttack;
        allyBAttackCardState = references.allyBAttack;
        allyBDefenseCardState = references.allyBDefense;
        allyBDodgeCardState = references.allyBDodge;
        allyBAbilityCardState = references.allyBAbility;
        allyBClashSinCardState = references.allyBSinAttack;
        enemyAttackCardState = references.enemyAttack;
        enemy02AttackCardState = references.enemy02Attack;
    }

    private void ClearRuntimeAndLegacyReferences()
    {
        runtimeState = null;
        lifecycleController = null;
        terminalInteractionStateCleared = false;
        ally01 = null;
        ally02 = null;
        enemy01 = null;
        enemy02 = null;
        allyAAttackCardState = null;
        allyABulletAttackCardState = null;
        allyADefenseCardState = null;
        allyADodgeCardState = null;
        allyAAbilityCardState = null;
        allyAClashSinCardState = null;
        allyASinAttackCardState = null;
        allyBAttackCardState = null;
        allyBDefenseCardState = null;
        allyBDodgeCardState = null;
        allyBAbilityCardState = null;
        allyBClashSinCardState = null;
        enemyAttackCardState = null;
        enemy02AttackCardState = null;
    }

    bool InitializeTestBattleData()
    {
        CreateTestCharacters();
        ApplyAlly01InitialBuffsFromDefinition();

        List<CardTestData> cards = CardDataLoader.LoadCardData();
        if (cards == null)
        {
            lastLog = "初始化失败：没有读取到卡牌数据";
            Debug.LogWarning(lastLog);
            return false;
        }

        CreateTestBattleCards(cards);

        runtimeState = new BattleRuntimeState();
        lifecycleController = CreateLifecycleController(runtimeState);
        runtimeState.SetCharacters(ally01, ally02, enemy01, enemy02);
        List<BattleActionSlot> initialActionSlots = BattleActionSlotManager.CreatePartyActionSlots(ally01, ally02, 2);
        runtimeState.SetActionSlots(initialActionSlots);
        runtimeState.SetIntentQueue(CreateFixedEnemyIntentQueue(initialActionSlots));
        string transitionFailure = "生命周期控制器为空";
        if (!lifecycleController.TryInitializeToPrepare(out transitionFailure))
        {
            lastLog = transitionFailure;
            Debug.LogError(lastLog);
            return false;
        }

        lastLog = "初始化完成：已进入 Prepare 阶段";
        return true;
    }

    private BattleLifecycleController CreateLifecycleController(
        BattleRuntimeState state
    )
    {
        if (sceneExecutionPresenter != null)
        {
            return new BattleLifecycleController(
                state,
                sceneExecutionPresenter
            );
        }

        // 未绑定正式场景Presenter时，保持原有Immediate同步行为。
        return new BattleLifecycleController(state);
    }

    void CreateTestCharacters()
    {
        ally01 = new CharacterData(
            "我方角色A", 30, 20, 20, "ui_ally_01"
        );
        ally02 = new CharacterData(
            "我方角色B", 30, 3, 5, "ui_ally_02"
        );
        enemy01 = new CharacterData(
            "敌人", 50, 5, 8, "ui_enemy_01"
        );
        enemy02 = new CharacterData(
            "敌人2", 50, 5, 8, "ui_enemy_02"
        );
    }

    void ApplyAlly01InitialBuffsFromDefinition()
    {
        if (ally01 == null)
        {
            return;
        }

        List<CharacterDefinitionData> definitions =
            CharacterDefinitionLoader.LoadDefinitions();
        CharacterDefinitionData ally01Definition =
            CharacterDefinitionLoader.FindByID(
                definitions,
                "ally_001"
            );
        if (ally01Definition == null)
        {
            Debug.LogWarning(
                "Simple UI 初始化失败：找不到角色定义 ally_001"
            );
            return;
        }

        // v0.1原型继续手工创建角色；初始Buff已统一走正式Definition与Factory入口。
        BattleUnitFactory.ApplyInitialBuffs(
            ally01,
            ally01Definition.initialBuffs
        );
    }

    void CreateTestBattleCards(List<CardTestData> cards)
    {
        CardTestData enemyCard = CardDataLoader.FindCardByID(cards, "enemy_atk_001");
        CardTestData allyAAttackCard = CardDataLoader.FindCardByID(cards, "atk_001");
        CardTestData defenseCard = CardDataLoader.FindCardByID(cards, "def_001");
        CardTestData dodgeCard = CardDataLoader.FindCardByID(cards, "dodge_001");
        CardTestData allyAAbilityCard = CardDataLoader.FindCardByID(cards, "sin_ability_001");
        CardTestData allyASinAttackCard = CardDataLoader.FindCardByID(cards, ClashSinTestCardID);
        CardTestData bulletAttackCard = CardDataLoader.FindCardByID(cards, "atk_bullet_001");

        enemyAttackCardState = BattleCardManager.CreateBattleCard(
            enemy01,
            enemyCard,
            "ui_enemy_atk_001_copy_0"
        );
        enemy02AttackCardState = BattleCardManager.CreateBattleCard(
            enemy02,
            enemyCard,
            "ui_enemy02_atk_001_copy_0"
        );

        allyAAttackCardState = BattleCardManager.CreateBattleCard(
            ally01,
            allyAAttackCard,
            "ui_allyA_atk_001_copy_0"
        );

        if (bulletAttackCard != null)
        {
            allyABulletAttackCardState = BattleCardManager.CreateBattleCard(
                ally01,
                bulletAttackCard,
                "ui_allyA_atk_bullet_001_copy_0"
            );
        }
        else
        {
            Debug.LogWarning("创建测试卡牌UI预览失败：找不到 atk_bullet_001");
        }

        allyADefenseCardState = BattleCardManager.CreateBattleCard(
            ally01,
            defenseCard,
            "ui_allyA_def_001_copy_0"
        );

        allyADodgeCardState = BattleCardManager.CreateBattleCard(
            ally01,
            dodgeCard,
            "ui_allyA_dodge_001_copy_0"
        );

        allyAAbilityCardState = BattleCardManager.CreateBattleCard(
            ally01,
            allyAAbilityCard,
            "ui_allyA_sin_ability_001_copy_0"
        );

        if (allyASinAttackCard != null)
        {
            allyASinAttackCardState = CreateClashSinCardState(
                ally01,
                allyASinAttackCard,
                "ui_allyA_sin_attack_test_001_copy_0"
            );
            allyAClashSinCardState = allyASinAttackCardState;
        }
        else
        {
            Debug.LogWarning("创建测试罪卡手牌失败：找不到 sin_attack_test_001");
        }

        allyBAttackCardState = BattleCardManager.CreateBattleCard(
            ally02,
            allyAAttackCard,
            "ui_allyB_atk_001_copy_0"
        );

        allyBDefenseCardState = BattleCardManager.CreateBattleCard(
            ally02,
            defenseCard,
            "ui_allyB_def_001_copy_0"
        );

        allyBDodgeCardState = BattleCardManager.CreateBattleCard(
            ally02,
            dodgeCard,
            "ui_allyB_dodge_001_copy_0"
        );

        allyBAbilityCardState = BattleCardManager.CreateBattleCard(
            ally02,
            allyAAbilityCard,
            "ui_allyB_sin_ability_001_copy_0"
        );

        allyBClashSinCardState = CreateClashSinCardState(
            ally02,
            allyASinAttackCard,
            "ui_allyB_sin_attack_test_001_copy_0"
        );
    }

    internal static BattleCardState CreateClashSinCardState(
        CharacterData owner,
        CardTestData cardData,
        string instanceID
    )
    {
        if (owner == null ||
            cardData == null ||
            cardData.cardID != ClashSinTestCardID)
        {
            Debug.LogWarning("创建拼点罪卡失败：必须使用 " + ClashSinTestCardID);
            return null;
        }

        return BattleCardManager.CreateBattleCard(owner, cardData, instanceID);
    }

    bool SpawnRuntimeUnitViewsOnce()
    {
        ClearRuntimeStatusViewReferences();

        if (unitViewSpawner == null)
        {
            lastLog = "运行时角色表现初始化失败：未绑定BattleUnitViewSpawner";
            Debug.LogError(lastLog, this);
            return false;
        }

        if (runtimeState == null)
        {
            lastLog = "运行时角色表现初始化失败：BattleRuntimeState为空";
            Debug.LogError(lastLog, this);
            return false;
        }

        unitViewSpawner.GeneratedViewsCleared -=
            OnRuntimeUnitViewsCleared;
        unitViewSpawner.GeneratedViewsCleared +=
            OnRuntimeUnitViewsCleared;

        if (!unitViewSpawner.Spawn(runtimeState))
        {
            lastLog = "运行时角色表现生成失败，请检查Spawner配置";
            Debug.LogError(lastLog, this);
            return false;
        }

        // 动态生成成功后，现有控制器继续复用原StatusView交互和刷新流程。
        BattleUnitViewHandle ally01Handle =
            unitViewSpawner.GetHandle(ally01);
        BattleUnitViewHandle ally02Handle =
            unitViewSpawner.GetHandle(ally02);
        BattleUnitViewHandle enemy01Handle =
            unitViewSpawner.GetHandle(enemy01);
        BattleUnitViewHandle enemy02Handle =
            unitViewSpawner.GetHandle(enemy02);

        if (!HasValidStatusView(ally01Handle) ||
            !HasValidStatusView(ally02Handle) ||
            !HasValidStatusView(enemy01Handle) ||
            !HasValidStatusView(enemy02Handle))
        {
            lastLog = "运行时角色表现初始化失败：四个动态Handle或StatusView不完整";
            Debug.LogError(lastLog, this);
            unitViewSpawner.ClearGeneratedViews();
            return false;
        }

        ally01StatusView = ally01Handle.StatusView;
        ally02StatusView = ally02Handle.StatusView;
        enemy01StatusView = enemy01Handle.StatusView;
        enemy02StatusView = enemy02Handle.StatusView;
        runtimeUnitViewsReady = true;
        warnedRuntimeUnitViewsUnavailable = false;
        return true;
    }

    private static bool HasValidStatusView(BattleUnitViewHandle handle)
    {
        return handle != null && handle.StatusView != null;
    }

    private void OnRuntimeUnitViewsCleared()
    {
        // Spawner可能先于Controller销毁，立即清掉所有动态对象引用。
        cardInteractionCoordinator?.ClearAllSelections();
        actionRelationLineController?.ClearSelectedSlot();
        ClearRuntimeStatusViewReferences();
    }

    private void ClearRuntimeStatusViewReferences()
    {
        ally01StatusView = null;
        ally02StatusView = null;
        enemy01StatusView = null;
        enemy02StatusView = null;
        runtimeUnitViewsReady = false;
        warnedRuntimeUnitViewsUnavailable = false;
    }

    List<BattleEnemyIntent> CreateFixedEnemyIntentQueue(List<BattleActionSlot> actionSlots)
    {
        // 正式初始化迁移第一轮：初始RuntimeState已由Definition创建；
        // 自动完整回合与后续回合意图仍暂用固定兼容逻辑，下一轮再迁移。
        return BattleAutomaticTurnCycle.CreateFixedEnemyIntentQueue(
            enemy01,
            enemyAttackCardState,
            enemy02,
            enemy02AttackCardState,
            ally01,
            ally02,
            actionSlots
        );
    }

    internal static CharacterData SelectFixedEnemyIntentTarget(
        CharacterData ally01,
        CharacterData ally02,
        List<BattleActionSlot> actionSlots,
        out int targetSlotIndex
    )
    {
        return BattleAutomaticTurnCycle.SelectFixedEnemyIntentTarget(
            ally01,
            ally02,
            actionSlots,
            out targetSlotIndex
        );
    }

    void BindButtonEvents()
    {
        if (buttonEventsBound)
        {
            return;
        }

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

        if (qiehuanButton != null)
        {
            qiehuanButton.onClick.AddListener(ToggleCardGroup);
        }

        buttonEventsBound = true;
    }

    void UnbindButtonEvents()
    {
        if (!buttonEventsBound)
        {
            return;
        }

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

        if (qiehuanButton != null)
        {
            qiehuanButton.onClick.RemoveListener(ToggleCardGroup);
        }

        buttonEventsBound = false;
    }

    private void ToggleCardGroup()
    {
        if (!CanEditActionSlots() ||
            cardInteractionCoordinator == null ||
            cardInteractionCoordinator.SelectedActionSlotView == null)
        {
            lastLog = runtimeState != null && runtimeState.IsBattleEnded
                ? "战斗已经结束，不能切换卡牌组"
                : "请先选择一个可用的我方行动槽位";
            RefreshView();
            return;
        }

        showingSinCards = cardInteractionCoordinator.ToggleCardMode(
            showingSinCards
        );
        RefreshView();
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
            ally01,
            1,
            ally01,
            allyAAttackCardState,
            enemy01
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
            ally01,
            1,
            ally01,
            allyAAbilityCardState,
            ally01
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
            ally02,
            1,
            ally02,
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
        TrySelectActor(ally01, "A");
    }

    private void OnClickSelectActorB()
    {
        TrySelectActor(ally02, "B");
    }

    void TrySelectActor(CharacterData actor, string actorLabel)
    {
        if (!CanEditActionSlots())
        {
            lastLog = runtimeState != null && runtimeState.IsBattleEnded
                ? "战斗已经结束，不能选择行动角色"
                : "当前不能选择行动角色";
            RefreshView();
            return;
        }

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
        if (!CanEditActionSlots())
        {
            lastLog = "当前不能选择行动槽位";
            RefreshView();
            return;
        }
        selectedSlotIndex = 1;
        lastLog = "Selected slot: 1";
        RefreshView();
    }

    private void OnClickSelectSlot2()
    {
        if (!CanEditActionSlots())
        {
            lastLog = "当前不能选择行动槽位";
            RefreshView();
            return;
        }
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
        SelectCardForCurrentActor(allyAClashSinCardState, allyBClashSinCardState, "ClashSin");
    }

    private void SelectCardForCurrentActor(BattleCardState allyACardState, BattleCardState allyBCardState, string cardLabel)
    {
        if (!CanEditActionSlots())
        {
            lastLog = "当前不能选择卡牌";
            RefreshView();
            return;
        }

        if (selectedActor == null)
        {
            lastLog = "Please select actor first";
            RefreshView();
            return;
        }

        if (object.ReferenceEquals(selectedActor, ally01))
        {
            selectedCardState = allyACardState;
        }
        else if (object.ReferenceEquals(selectedActor, ally02))
        {
            selectedCardState = allyBCardState;
        }

        lastLog = "Selected card: " + cardLabel;
        RefreshView();
    }

    private void OnClickSelectFreeAttackMode()
    {
        if (!CanEditActionSlots())
        {
            lastLog = "当前不能选择行动用途";
            RefreshView();
            return;
        }
        selectedActionMode = ActionModeFreeAttack;
        lastLog = "Selected mode: FreeAttack";
        RefreshView();
    }

    private void OnClickSelectRespondIntent1Mode()
    {
        if (!CanEditActionSlots())
        {
            lastLog = "当前不能选择行动用途";
            RefreshView();
            return;
        }
        selectedActionMode = ActionModeRespondIntent1;
        lastLog = "Selected mode: RespondIntent1";
        RefreshView();
    }

    private void OnClickSelectPassiveGuardMode()
    {
        if (!CanEditActionSlots())
        {
            lastLog = "当前不能选择行动用途";
            RefreshView();
            return;
        }
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
            : enemy01;

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

        if (result)
        {
            ClearPlanningSelectionAndHideCards();
        }
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

        if (result)
        {
            ClearPlanningSelectionAndHideCards();
        }
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

        if (result)
        {
            ClearPlanningSelectionAndHideCards();
        }
        RefreshView();
    }

    private void OnClickClearSelection()
    {
        ClearAllUISelectionState();
        lastLog = "Selection cleared";
        RefreshView();
    }

    void ClearSelectedActionState()
    {
        selectedActor = null;
        selectedSlotIndex = 0;
        selectedCardState = null;
        selectedActionMode = null;
    }

    private void ClearAllUISelectionState()
    {
        ClearPlanningSelectionAndHideCards();
    }

    // 规划交互的统一清理入口：不触碰正式槽位安排和正式关系数据。
    internal void ClearPlanningSelectionAndHideCards()
    {
        cardInteractionCoordinator?.ClearAllSelections();
        ClearSelectedActionState();
        testCardHandView?.ClearCards();
        testCardView?.SetEmpty();
        actionRelationLineController?.EndCardTargetingPreview();
        actionRelationLineController?.ClearSelectedSlot();
        actionRelationLineController?.SetCardTargetingDiagnosticState(
            false,
            string.Empty,
            string.Empty,
            false
        );
    }

    private void SynchronizeInteractionStateWithLifecycle()
    {
        bool battleEnded = runtimeState != null &&
            runtimeState.LifecyclePhase == BattleLifecyclePhase.BattleEnded;
        if (!battleEnded)
        {
            terminalInteractionStateCleared = false;
            return;
        }

        if (terminalInteractionStateCleared)
        {
            return;
        }

        // 只在首次观察到终局阶段时清理一次临时交互状态。
        ClearPlanningSelectionAndHideCards();
        terminalInteractionStateCleared = true;
    }

    private void RefreshBattleStartButtonState()
    {
        if (battleStartButton == null)
        {
            return;
        }

        battleStartButton.interactable =
            !isRunningCompleteTurnCycle &&
            BattleAutomaticTurnCycle.CanStart(runtimeState);
    }

    private void OnClickBattleStart()
    {
        if (isRunningCompleteTurnCycle)
        {
            lastLog = "当前回合正在处理中，请勿重复开始";
            RefreshView();
            return;
        }

        if (!BattleAutomaticTurnCycle.CanStart(runtimeState))
        {
            lastLog = "当前状态不能开始完整回合：必须处于 Prepare、战斗未结束且没有已有计划";
            RefreshView();
            return;
        }

        isRunningCompleteTurnCycle = true;
        ClearPlanningSelectionAndHideCards();
        actionRelationLineController?.ClearAll();
        RefreshBattleStartButtonState();

        if (sceneExecutionPresenter != null)
        {
            if (!TryBeginScenePresentedTurnCycle(out string failureMessage))
            {
                isRunningCompleteTurnCycle = false;
                lastLog = failureMessage;
                RefreshView();
                return;
            }

            lastLog = "完整回合已进入跨帧执行，阶段：" +
                runtimeState.LifecyclePhase;
            RefreshView();
            return;
        }

        try
        {
            BattleAutomaticTurnCycleResult result = BattleAutomaticTurnCycle.TryRun(
                runtimeState,
                ally01,
                ally02,
                enemy01,
                enemyAttackCardState,
                enemy02,
                enemy02AttackCardState
            );

            lastLog = result.message;

            if (result.advancedToNextTurn || result.battleEnded)
            {
                ClearAllUISelectionState();
            }
        }
        finally
        {
            isRunningCompleteTurnCycle = false;
            RefreshView();
        }
    }

    private bool TryBeginScenePresentedTurnCycle(
        out string failureMessage
    )
    {
        failureMessage = string.Empty;
        if (lifecycleController == null || runtimeState == null)
        {
            failureMessage = "完整回合启动失败：生命周期控制器或RuntimeState为空";
            return false;
        }
        if (!BattleAutomaticTurnCycle.CanStart(runtimeState))
        {
            failureMessage =
                "完整回合启动失败：必须处于Prepare、战斗未结束且没有已有计划";
            return false;
        }

        BattleAutomaticTurnCycleResult result =
            new BattleAutomaticTurnCycleResult
            {
                startingTurn = runtimeState.currentTurn,
                endingTurn = runtimeState.currentTurn,
                message = "完整回合跨帧执行尚未完成"
            };
        if (!lifecycleController.TryCreateExecutionPlan(
                false,
                out BattleExecutionPlan executionPlan,
                out failureMessage
            ))
        {
            Debug.LogWarning(
                "完整回合启动失败：ExecutionPlan为空，已安全返回Prepare"
            );
            return false;
        }

        result.executedPlan = executionPlan;
        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);

        BattleRollGateSettings settings = new BattleRollGateSettings(
            BattleRollMode.Auto,
            0f,
            0f
        );
        if (!lifecycleController.TryBeginPausableExecution(
                settings,
                out failureMessage
            ))
        {
            return false;
        }

        scenePresentedTurnCycleResult = result;
        isScenePresentedTurnCycleRunning = true;
        return true;
    }

    private void AdvanceScenePresentedTurnCycle()
    {
        BattleExecutionRunner runner = lifecycleController != null
            ? lifecycleController.ExecutionRunner
            : null;
        if (runner == null)
        {
            FailScenePresentedTurnCycle(
                "Pausable执行失败：正式场景Runner为空"
            );
            return;
        }
        if (runner.HasFailed)
        {
            FailScenePresentedTurnCycle(
                "Pausable执行失败：正式场景Runner已失败"
            );
            return;
        }

        if (!runner.IsCompleted)
        {
            if (!lifecycleController.AdvancePausableExecution(
                    Time.deltaTime,
                    out string failureMessage
                ))
            {
                FailScenePresentedTurnCycle(failureMessage);
                return;
            }

            runner = lifecycleController.ExecutionRunner;
            if (runner == null || runner.HasFailed)
            {
                FailScenePresentedTurnCycle(
                    "Pausable执行失败：正式场景Runner推进后失效"
                );
                return;
            }
        }

        if (!runner.IsCompleted)
        {
            return;
        }

        BattleAutomaticTurnCycleResult result =
            BattleAutomaticTurnCycle.CompleteTurnCycleAfterExecution(
                scenePresentedTurnCycleResult,
                lifecycleController,
                runtimeState,
                scenePresentedTurnCycleResult != null
                    ? scenePresentedTurnCycleResult.executedPlan
                    : null,
                ally01,
                ally02,
                enemy01,
                enemyAttackCardState,
                enemy02,
                enemy02AttackCardState
            );
        CompleteScenePresentedTurnCycle(result);
    }

    private void CompleteScenePresentedTurnCycle(
        BattleAutomaticTurnCycleResult result
    )
    {
        isScenePresentedTurnCycleRunning = false;
        scenePresentedTurnCycleResult = null;
        isRunningCompleteTurnCycle = false;
        lastLog = result != null
            ? result.message
            : "完整回合收尾失败：结果为空";

        if (result != null &&
            (result.advancedToNextTurn || result.battleEnded))
        {
            ClearAllUISelectionState();
        }

        RefreshView();
    }

    private void FailScenePresentedTurnCycle(string failureMessage)
    {
        isScenePresentedTurnCycleRunning = false;
        scenePresentedTurnCycleResult = null;
        isRunningCompleteTurnCycle = false;
        lastLog = string.IsNullOrEmpty(failureMessage)
            ? "完整回合跨帧执行失败"
            : failureMessage;
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

        BattleExecutionPlan executionPlan;
        string transitionFailure = "生命周期控制器为空";
        if (lifecycleController == null ||
            !lifecycleController.TryCreateExecutionPlan(
                true,
                out executionPlan,
                out transitionFailure
            ))
        {
            lastLog = transitionFailure;
            RefreshView();
            return;
        }

        ClearPlanningSelectionAndHideCards();

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

        ClearPlanningSelectionAndHideCards();

        string transitionFailure = "生命周期控制器为空";
        if (lifecycleController == null ||
            !BattleAutomaticTurnCycle.TryExecuteCurrentPlan(
                lifecycleController,
                sceneExecutionPresenter != null,
                out transitionFailure
            ))
        {
            lastLog = transitionFailure;
            RefreshView();
            return;
        }

        if (runtimeState.IsBattleEnded)
        {
            lastLog = "战斗结束：" + runtimeState.battleResult;
        }
        else
        {
            lastLog = "执行计划已执行，plan.isCompleted = " +
                runtimeState.currentExecutionPlan.isCompleted;
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

        string failureMessage = "生命周期控制器为空";
        if (lifecycleController == null ||
            !lifecycleController.TryEndCurrentTurn(out failureMessage))
        {
            lastLog = failureMessage;
            RefreshView();
            return;
        }
        ClearPlanningSelectionAndHideCards();
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

        List<BattleActionSlot> newActionSlots = BattleActionSlotManager.CreateLivingPartyActionSlots(ally01, ally02, 2);

        List<BattleEnemyIntent> newIntentQueue = newActionSlots != null &&
            newActionSlots.Count > 0
            ? CreateFixedEnemyIntentQueue(newActionSlots)
            : new List<BattleEnemyIntent>();
        string failureMessage = "生命周期控制器为空";
        if (lifecycleController != null &&
            lifecycleController.TryPrepareNextTurn(
                newActionSlots,
                newIntentQueue,
                out failureMessage
            ))
        {
            ClearPlanningSelectionAndHideCards();
            lastLog = "下一回合已准备，阶段：Prepare";
        }
        else
        {
            lastLog = failureMessage;
        }

        RefreshView();
    }

    private void RefreshView()
    {
        SynchronizeInteractionStateWithLifecycle();
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
        RefreshFixedStatusViews();
        RefreshCharacterStatusViews();
        RefreshActionSlotIntentViews();
        RefreshActionRelations();
        RefreshTestCardView();
        RefreshTestCardHandView();
        RefreshBattleStartButtonState();
    }

    private void RefreshFixedStatusViews()
    {
        if (roundView != null)
        {
            if (runtimeState != null)
            {
                roundView.SetRound(runtimeState.currentTurn);
            }
            else
            {
                roundView.Clear();
            }
        }

        if (guiltView != null)
        {
            int guilt = runtimeState != null ? runtimeState.currentGuilt : 0;
            guiltView.SetGuilt(guilt);
        }
    }

    private void RefreshCharacterStatusViews()
    {
        if (runtimeUnitViewsReady &&
            unitViewSpawner != null &&
            unitViewSpawner.IsSpawned)
        {
            unitViewSpawner.RefreshGeneratedViews();
            return;
        }

        if (!warnedRuntimeUnitViewsUnavailable)
        {
            warnedRuntimeUnitViewsUnavailable = true;
            Debug.LogWarning(
                "动态角色状态UI尚未就绪，本次刷新已安全跳过。",
                this
            );
        }
    }

    private void RefreshActionSlotIntentViews()
    {
        ResetActionSlotIntentBaseStates();

        if (runtimeState == null)
        {
            return;
        }

        if (runtimeState.intentQueue != null)
        {
            for (int intentIndex = 0; intentIndex < runtimeState.intentQueue.Count; intentIndex++)
            {
                BattleEnemyIntent intent = runtimeState.intentQueue[intentIndex];

                if (intent == null)
                {
                    continue;
                }

                BattleCharacterStatusUIView enemyStatusView = GetEnemyStatusView(intent.enemy);
                if (enemyStatusView == null)
                {
                    Debug.LogWarning("一级行动槽找不到敌人状态视图：" + intent.intentID);
                    continue;
                }

                int enemyUISlotIndex =
                    BattleCardAssignmentRouter.EnemySlotIndexToUIIndex(intent.enemySlotIndex);
                if (enemyUISlotIndex < 0)
                {
                    Debug.LogWarning(
                        "敌人意图 enemySlotIndex 超出一级 UI 范围：" +
                        intent.intentID +
                        " / " +
                        intent.enemySlotIndex
                    );
                    continue;
                }

                enemyStatusView.SetSlotState(
                    enemyUISlotIndex,
                    BattleActionSlotUIState.EnemyActionSet
                );
                enemyStatusView.SetBoundEnemyIntent(enemyUISlotIndex, intent);

                BattleCharacterStatusUIView targetStatusView =
                    GetAllyStatusView(intent.originalTargetCharacter);
                int targetUISlotIndex = intent.originalTargetSlotIndex - 1;

                if (targetStatusView == null ||
                    targetUISlotIndex < 0 ||
                    targetUISlotIndex > 1)
                {
                    Debug.LogWarning("敌人意图原始目标无法映射到友方一级行动槽：" + intent.intentID);
                    continue;
                }

                targetStatusView.SetSlotState(
                    targetUISlotIndex,
                    BattleActionSlotUIState.AllyTargetedNoAction
                );
            }
        }

        if (runtimeState.actionSlots == null)
        {
            return;
        }

        foreach (BattleActionSlot slot in runtimeState.actionSlots)
        {
            if (slot == null)
            {
                continue;
            }

            BattleCharacterStatusUIView ownerStatusView = GetAllyStatusView(slot.owner);
            int ownerUISlotIndex = slot.slotIndex - 1;

            if (ownerStatusView == null ||
                ownerUISlotIndex < 0 ||
                ownerUISlotIndex > 1)
            {
                continue;
            }

            ownerStatusView.SetBoundActionSlot(ownerUISlotIndex, slot);

            if (slot.IsEmpty())
            {
                continue;
            }

            // 已安排状态覆盖“被敌人瞄准但未行动”，选中贴图仍由 Slot View 单独叠加。
            ownerStatusView.SetSlotState(
                ownerUISlotIndex,
                BattleActionSlotUIState.AllyActionSet
            );
        }
    }

    private void ResetActionSlotIntentBaseStates()
    {
        SetTwoSlotStates(ally01StatusView, BattleActionSlotUIState.AllyEmpty);
        SetTwoSlotStates(ally02StatusView, BattleActionSlotUIState.AllyEmpty);
        SetTwoSlotStates(enemy01StatusView, BattleActionSlotUIState.EnemyEmpty);
        SetTwoSlotStates(enemy02StatusView, BattleActionSlotUIState.EnemyEmpty);

        if (enemy01StatusView != null)
        {
            enemy01StatusView.ClearBoundEnemyIntents();
        }

        if (enemy02StatusView != null)
        {
            enemy02StatusView.ClearBoundEnemyIntents();
        }

        ally01StatusView?.ClearBoundActionSlots();
        ally02StatusView?.ClearBoundActionSlots();
    }

    private void SetTwoSlotStates(
        BattleCharacterStatusUIView statusView,
        BattleActionSlotUIState state
    )
    {
        if (statusView == null)
        {
            return;
        }

        statusView.SetSlotState(0, state);
        statusView.SetSlotState(1, state);
    }

    private BattleCharacterStatusUIView GetAllyStatusView(CharacterData character)
    {
        if (object.ReferenceEquals(character, ally01))
        {
            return ally01StatusView;
        }

        if (object.ReferenceEquals(character, ally02))
        {
            return ally02StatusView;
        }

        return null;
    }

    private BattleCharacterStatusUIView GetEnemyStatusView(CharacterData character)
    {
        if (object.ReferenceEquals(character, enemy01))
        {
            return enemy01StatusView;
        }

        if (object.ReferenceEquals(character, enemy02))
        {
            return enemy02StatusView;
        }

        return null;
    }

    private void BindCharacterStatusSlotInteractions()
    {
        if (ally01StatusView != null)
        {
            ally01StatusView.SetAllySlotInteractionHandlers(
                OnAllyActionSlotClicked,
                OnAllyActionSlotRightClicked
            );
            ally01StatusView.SetSelfTargetClickHandler(
                OnSelfActionTargetClicked
            );
        }

        if (ally02StatusView != null)
        {
            ally02StatusView.SetAllySlotInteractionHandlers(
                OnAllyActionSlotClicked,
                OnAllyActionSlotRightClicked
            );
            ally02StatusView.SetSelfTargetClickHandler(
                OnSelfActionTargetClicked
            );
        }

        if (enemy01StatusView != null)
        {
            enemy01StatusView.SetEnemySlotClickHandler(
                OnEnemyActionSlotClicked
            );
        }

        if (enemy02StatusView != null)
        {
            enemy02StatusView.SetEnemySlotClickHandler(
                OnEnemyActionSlotClicked
            );
        }
    }

    private void BindCardHandInteractions()
    {
        if (testCardHandView != null)
        {
            testCardHandView.SetSelectionController(
                cardSelectionController
            );
        }
    }

    internal bool TrySelectActionSlotForPlanning(
        BattleActionSlotUIView slotView
    )
    {
        if (!IsValidPlanningSlotView(slotView) ||
            cardInteractionCoordinator == null ||
            !cardInteractionCoordinator.SelectSourceSlot(slotView))
        {
            return false;
        }

        RefreshTestCardView();
        RefreshTestCardHandView();
        RefreshCardTargetingPreview();
        return true;
    }

    private bool IsValidPlanningSlotView(BattleActionSlotUIView slotView)
    {
        if (!CanEditActionSlots() ||
            slotView == null ||
            slotView.IsEnemySlot ||
            slotView.BoundCharacter == null ||
            slotView.BoundCharacter.IsDead() ||
            runtimeState.allyUnits == null ||
            runtimeState.actionSlots == null ||
            !ContainsCharacterReference(
                runtimeState.allyUnits,
                slotView.BoundCharacter
            ))
        {
            return false;
        }

        for (int index = 0;
            index < runtimeState.actionSlots.Count;
            index++)
        {
            BattleActionSlot slot = runtimeState.actionSlots[index];
            if (slot != null &&
                object.ReferenceEquals(
                    slot.owner,
                    slotView.BoundCharacter
                ) &&
                slot.slotIndex == slotView.FormalSlotIndex)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsCharacterReference(
        List<CharacterData> characters,
        CharacterData target
    )
    {
        for (int index = 0; index < characters.Count; index++)
        {
            if (object.ReferenceEquals(characters[index], target))
            {
                return true;
            }
        }

        return false;
    }

    private void OnAllyActionSlotClicked(BattleActionSlotUIView clickedSlotView)
    {
        if (!IsValidPlanningSlotView(clickedSlotView))
        {
            return;
        }

        if (object.ReferenceEquals(
                cardInteractionCoordinator.SelectedActionSlotView,
                clickedSlotView) &&
            IsSelectedDefenseOrDodgeCard())
        {
            BattleCardInteractionOutcome outcome =
                cardInteractionCoordinator.ClickSelectedSourceSlotAsSelf(
                    runtimeState,
                    clickedSlotView
                );
            lastLog = outcome.assignmentResult != null
                ? outcome.assignmentResult.message
                : outcome.isSuccess
                    ? "卡牌点击安排成功"
                    : "卡牌点击安排失败";

            if (outcome.isSuccess)
            {
                CompleteSuccessfulCardAssignment();
                RefreshView();
            }
            else
            {
                SetText(logText, lastLog);
            }
            return;
        }

        TrySelectActionSlotForPlanning(clickedSlotView);
    }

    private void OnAllyActionSlotRightClicked(BattleActionSlotUIView clickedSlotView)
    {
        if (!IsValidPlanningSlotView(clickedSlotView))
        {
            return;
        }

        BattleActionSlot clickedRuntimeSlot =
            BattleActionSlotManager.GetSlot(
                runtimeState.actionSlots,
                clickedSlotView.BoundCharacter,
                clickedSlotView.FormalSlotIndex
            );
        if (clickedRuntimeSlot == null ||
            clickedRuntimeSlot.cardState == null)
        {
            ClearPlanningSelectionAndHideCards();
            lastLog = "已关闭角色卡牌栏";
            RefreshView();
            return;
        }

        BattleActionAssignmentResult result;
        bool cancelled = BattleCardAssignmentRouter.TryCancelSelectedSlot(
            runtimeState,
            clickedSlotView.BoundCharacter,
            clickedSlotView.FormalSlotIndex,
            out result
        );

        lastLog = result != null
            ? result.message
            : cancelled
                ? "已取消行动安排"
                : "取消行动安排失败";

        // 取消安排只改变槽位内容，保留当前一级槽位选择。
        if (cancelled)
        {
            EndCardTargetingSession(
                cardSelectionController,
                actionRelationLineController
            );
            cardInteractionCoordinator.SelectSourceSlot(clickedSlotView);
        }
        RefreshView();
    }

    private void OnEnemyActionSlotClicked(
        BattleActionSlotUIView targetSlotView
    )
    {
        if (!CanEditActionSlots())
        {
            return;
        }

        BattleCardInteractionOutcome outcome =
            cardInteractionCoordinator.ClickEnemySlot(
                runtimeState,
                targetSlotView
            );
        if (!outcome.hadSelectedCard)
        {
            return;
        }

        lastLog = outcome.assignmentResult != null
            ? outcome.assignmentResult.message
            : outcome.isSuccess
                ? "卡牌点击安排成功"
                : "卡牌点击安排失败";

        if (!outcome.isSuccess)
        {
            BattleActionSlotCardInfoPanelHost.SuppressNextClickLock(
                targetSlotView.gameObject
            );
            SetText(logText, lastLog);
            return;
        }

        BattleActionSlotCardInfoPanelHost
            .CloseAllPanelsAndSuppressNextClickLock(
                targetSlotView.gameObject
            );
        CompleteSuccessfulCardAssignment();
        RefreshView();
    }

    private void OnCardSelectionChanged(BattleCardUIView selectedCardView)
    {
        if (!CanEditActionSlots())
        {
            actionRelationLineController?.EndCardTargetingPreview();
            return;
        }
        RefreshCardTargetingPreview();
    }

    private void OnSourceSlotSelectionChanged(
        BattleActionSlotUIView selectedSlotView
    )
    {
        if (actionRelationLineController == null)
        {
            return;
        }

        if (!CanEditActionSlots())
        {
            actionRelationLineController.ClearSelectedSlot();
            return;
        }

        if (selectedSlotView == null)
        {
            actionRelationLineController.ClearSelectedSlot();
        }
        else
        {
            actionRelationLineController.SetSelectedSlot(selectedSlotView);
        }
    }

    private bool IsSelectedDefenseOrDodgeCard()
    {
        BattleCardUIView cardView = cardSelectionController.SelectedCardView;
        BattleCardState cardState = cardView != null
            ? cardView.BoundCardState
            : null;
        return cardState != null &&
            cardState.cardData != null &&
            (cardState.cardData.cardType == CardType.Defense ||
             cardState.cardData.cardType == CardType.Dodge);
    }

    private void RefreshCardTargetingPreview()
    {
        if (actionRelationLineController == null)
        {
            return;
        }

        BattleActionSlotUIView sourceSlot =
            cardInteractionCoordinator != null
                ? cardInteractionCoordinator.SelectedActionSlotView
                : null;
        BattleCardUIView selectedCardView =
            cardInteractionCoordinator != null
                ? cardInteractionCoordinator.SelectedCardView
                : null;
        string sourceSlotID = sourceSlot != null
            ? actionRelationLineController.GetSlotID(sourceSlot)
            : string.Empty;
        bool targetingActive = IsValidCardTargetingSession(
            sourceSlot,
            selectedCardView
        );

        actionRelationLineController.SetCardTargetingDiagnosticState(
            cardSelectionController.HasSelection,
            selectedCardView != null &&
                selectedCardView.BoundCardState != null &&
                selectedCardView.BoundCardState.cardData != null
                    ? selectedCardView.BoundCardState.cardData.cardID
                    : string.Empty,
            sourceSlotID,
            targetingActive
        );

        if (!targetingActive)
        {
            actionRelationLineController.EndCardTargetingPreview();
            return;
        }

        actionRelationLineController.BeginCardTargetingPreview(sourceSlotID);
    }

    private bool IsValidCardTargetingSession(
        BattleActionSlotUIView sourceSlot,
        BattleCardUIView selectedCardView
    )
    {
        if (!CanEditActionSlots() ||
            cardInteractionCoordinator == null ||
            !cardInteractionCoordinator.IsCardTargetingActive ||
            sourceSlot == null ||
            sourceSlot.IsEnemySlot ||
            sourceSlot.BoundCharacter == null ||
            sourceSlot.BoundCharacter.IsDead() ||
            selectedCardView == null ||
            selectedCardView.BoundCardState == null ||
            !object.ReferenceEquals(
                selectedCardView.BoundOwner,
                sourceSlot.BoundCharacter) ||
            !ShouldDisplayCardInHand(
                runtimeState,
                selectedCardView.BoundCardState))
        {
            return false;
        }

        BattleActionSlot runtimeSlot = BattleActionSlotManager.GetSlot(
            runtimeState.actionSlots,
            sourceSlot.BoundCharacter,
            sourceSlot.FormalSlotIndex
        );
        return runtimeSlot != null &&
            ContainsExactCardReference(
                sourceSlot.BoundCharacter,
                selectedCardView.BoundCardState
            );
    }

    private static bool ContainsExactCardReference(
        CharacterData owner,
        BattleCardState cardState
    )
    {
        if (owner == null || owner.battleCards == null || cardState == null)
        {
            return false;
        }

        for (int index = 0; index < owner.battleCards.Count; index++)
        {
            if (object.ReferenceEquals(owner.battleCards[index], cardState))
            {
                return true;
            }
        }

        return false;
    }

    private void CompleteSuccessfulCardAssignment()
    {
        // 正式安排已写入Runtime；本次规划会话完整结束，正式关系数据继续保留。
        ClearPlanningSelectionAndHideCards();
    }

    internal static void EndCardTargetingSession(
        BattleCardSelectionController selectionController,
        BattleActionRelationLineController relationLineController
    )
    {
        selectionController?.ClearSelection();
        relationLineController?.EndCardTargetingPreview();
    }

    private void RefreshActionRelations()
    {
        if (actionRelationLineController == null)
        {
            return;
        }

        actionRelationLineController.BindRuntimeState(runtimeState);
        actionRelationLineController.RefreshRelations();
        if (cardInteractionCoordinator != null &&
            cardInteractionCoordinator.SelectedActionSlotView != null)
        {
            actionRelationLineController.SetSelectedSlot(
                cardInteractionCoordinator.SelectedActionSlotView
            );
        }
        RefreshCardTargetingPreview();
    }

    private void OnSelfActionTargetClicked(
        BattleSelfActionDropZone targetView
    )
    {
        if (!CanEditActionSlots())
        {
            return;
        }

        BattleCardInteractionOutcome outcome =
            cardInteractionCoordinator.ClickSelfTarget(
                runtimeState,
                targetView
            );
        if (!outcome.hadSelectedCard)
        {
            return;
        }

        lastLog = outcome.assignmentResult != null
            ? outcome.assignmentResult.message
            : outcome.isSuccess
                ? "卡牌点击安排成功"
                : "卡牌点击安排失败";

        if (!outcome.isSuccess)
        {
            SetText(logText, lastLog);
            return;
        }

        CompleteSuccessfulCardAssignment();
        RefreshView();
    }

    void RefreshTestCardView()
    {
        if (testCardView == null)
        {
            return;
        }

        CharacterData handOwner = cardInteractionCoordinator != null
            ? cardInteractionCoordinator.SelectedCharacter
            : null;
        BattleCardState previewCard = FindCardStateByID(
            handOwner,
            "atk_bullet_001"
        );
        if (handOwner == null || previewCard == null)
        {
            testCardView.SetEmpty();
            return;
        }

        BattleCardUIPreviewData previewData = BattleCardUIPreviewBuilder.Build(
            handOwner,
            enemy01,
            previewCard
        );

        testCardView.SetCard(previewData);
    }

    void RefreshTestCardHandView()
    {
        if (testCardHandView == null)
        {
            return;
        }

        CharacterData selectedCharacter =
            cardInteractionCoordinator != null
                ? cardInteractionCoordinator.SelectedCharacter
                : null;
        BattleActionSlotUIView selectedSlotView =
            cardInteractionCoordinator != null
                ? cardInteractionCoordinator.SelectedActionSlotView
                : null;

        if (selectedCharacter == null ||
            selectedSlotView == null ||
            !IsValidPlanningSlotView(selectedSlotView) ||
            !object.ReferenceEquals(
                selectedSlotView.BoundCharacter,
                selectedCharacter
            ) ||
            selectedCharacter.battleCards == null)
        {
            testCardHandView.ClearCards();
            return;
        }

        string[] targetCardIDs = showingSinCards
            ? sinTestHandCardIDs
            : normalTestHandCardIDs;

        List<BattleCardState> cards = FindTestHandCardsByID(
            selectedCharacter,
            targetCardIDs
        );

        testCardHandView.SetCards(
            selectedCharacter,
            enemy01,
            cards
        );
    }

    private List<BattleCardState> FindTestHandCardsByID(CharacterData handOwner, string[] cardIDs)
    {
        List<BattleCardState> cards = new List<BattleCardState>();

        if (cardIDs == null || handOwner == null || handOwner.battleCards == null)
        {
            return cards;
        }

        for (int i = 0; i < cardIDs.Length; i++)
        {
            string cardID = cardIDs[i];
            BattleCardState cardState = FindCardStateByID(handOwner, cardID);

            if (cardState == null)
            {
                if (!HasCardStateByID(handOwner, cardID))
                {
                    Debug.LogWarning(handOwner.characterName + " 的测试手牌缺少卡牌：" + cardID);
                }

                continue;
            }

            if (!ShouldDisplayCardInHand(runtimeState, cardState))
            {
                continue;
            }

            cards.Add(cardState);
        }

        return cards;
    }

    internal static bool ShouldDisplayCardInHand(
        BattleRuntimeState runtimeState,
        BattleCardState cardState
    )
    {
        return cardState != null &&
            cardState.cardData != null &&
            !BattleCardAssignmentRouter.IsCardAssigned(runtimeState, cardState);
    }

    private BattleCardState FindCardStateByID(CharacterData handOwner, string cardID)
    {
        if (string.IsNullOrEmpty(cardID) || handOwner == null || handOwner.battleCards == null)
        {
            return null;
        }

        for (int i = 0; i < handOwner.battleCards.Count; i++)
        {
            BattleCardState cardState = handOwner.battleCards[i];

            if (cardState == null || cardState.cardData == null)
            {
                continue;
            }

            if (cardState.cardData.cardID == cardID)
            {
                return cardState;
            }
        }

        return null;
    }

    private bool HasCardStateByID(CharacterData handOwner, string cardID)
    {
        if (string.IsNullOrEmpty(cardID) || handOwner == null || handOwner.battleCards == null)
        {
            return false;
        }

        for (int i = 0; i < handOwner.battleCards.Count; i++)
        {
            BattleCardState cardState = handOwner.battleCards[i];

            if (cardState != null &&
                cardState.cardData != null &&
                cardState.cardData.cardID == cardID)
            {
                return true;
            }
        }

        return false;
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
        return runtimeState != null && !runtimeState.IsBattleEnded &&
            runtimeState.LifecyclePhase == BattleLifecyclePhase.Prepare &&
            !HasCurrentPlan();
    }

    private bool CanCreatePlan()
    {
        return runtimeState != null && !runtimeState.IsBattleEnded &&
            runtimeState.LifecyclePhase == BattleLifecyclePhase.Prepare &&
            !HasCurrentPlan();
    }

    private bool CanExecutePlan()
    {
        return runtimeState != null && !runtimeState.IsBattleEnded &&
            runtimeState.LifecyclePhase == BattleLifecyclePhase.PlanReady &&
            HasCurrentPlan() && !runtimeState.currentExecutionPlan.isCompleted;
    }

    private bool CanEndTurn()
    {
        return runtimeState != null &&
            !runtimeState.IsBattleEnded &&
            runtimeState.LifecyclePhase == BattleLifecyclePhase.TurnResolved &&
            HasCurrentPlan() &&
            IsCurrentPlanCompleted();
    }

    private bool CanPrepareNextTurn()
    {
        return runtimeState != null && !runtimeState.IsBattleEnded &&
            runtimeState.LifecyclePhase == BattleLifecyclePhase.TurnEnded;
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

        string slotText = selectedSlotIndex > 0
            ? "Slot" + selectedSlotIndex
            : "NoSlot";
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
        if (object.ReferenceEquals(selectedActor, ally01))
        {
            return "A";
        }

        if (object.ReferenceEquals(selectedActor, ally02))
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
            SetText(actionSlotA1Text, FormatOwnerActionSlot(viewData, ally01, 1, "A槽位1"));
            SetText(actionSlotA2Text, FormatOwnerActionSlot(viewData, ally01, 2, "A槽位2"));
            SetText(actionSlotB1Text, FormatOwnerActionSlot(viewData, ally02, 1, "B槽位1"));
            SetText(actionSlotB2Text, FormatOwnerActionSlot(viewData, ally02, 2, "B槽位2"));
            return;
        }

        SetText(actionSlot1Text, FormatOwnerActionSlotWithFallback(viewData, ally01, 1, "A槽位1", 1));
        SetText(actionSlot2Text, FormatOwnerActionSlotWithFallback(viewData, ally02, 1, "B槽位1", 2));
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

public sealed class BattleAutomaticTurnCycleResult
{
    public bool isSuccess;
    public bool executionPlanCompleted;
    public bool advancedToNextTurn;
    public bool battleEnded;
    public int startingTurn;
    public int endingTurn;
    public string message;
    public BattleExecutionPlan executedPlan;
}

// 正式“战斗开始”按钮与模式61共用这一数据闭环，UI只负责锁按钮、清选择和刷新。
public static class BattleAutomaticTurnCycle
{
    const int ActionSlotCountPerCharacter = 2;
    const string FixedEnemyAttackCardID = "enemy_atk_001";

    public static bool CanStart(BattleRuntimeState runtimeState)
    {
        return runtimeState != null &&
            !runtimeState.IsBattleEnded &&
            runtimeState.LifecyclePhase == BattleLifecyclePhase.Prepare &&
            runtimeState.currentExecutionPlan == null;
    }

    public static BattleAutomaticTurnCycleResult TryRun(
        BattleRuntimeState runtimeState,
        CharacterData ally01,
        CharacterData ally02,
        CharacterData enemy01,
        BattleCardState enemyAttackCardState
    )
    {
        return TryRun(
            runtimeState,
            ally01,
            ally02,
            enemy01,
            enemyAttackCardState,
            null,
            null
        );
    }

    public static BattleAutomaticTurnCycleResult TryRun(
        BattleRuntimeState runtimeState,
        CharacterData ally01,
        CharacterData ally02,
        CharacterData enemy01,
        BattleCardState enemyAttackCardState,
        CharacterData enemy02,
        BattleCardState enemy02AttackCardState
    )
    {
        return TryRun(
            runtimeState,
            ally01,
            ally02,
            enemy01,
            enemyAttackCardState,
            enemy02,
            enemy02AttackCardState,
            null
        );
    }

    public static BattleAutomaticTurnCycleResult TryRun(
        BattleRuntimeState runtimeState,
        CharacterData ally01,
        CharacterData ally02,
        CharacterData enemy01,
        BattleCardState enemyAttackCardState,
        CharacterData enemy02,
        BattleCardState enemy02AttackCardState,
        IBattleExecutionPresenter executionPresenter
    )
    {
        BattleAutomaticTurnCycleResult result = new BattleAutomaticTurnCycleResult
        {
            startingTurn = runtimeState != null ? runtimeState.currentTurn : 0,
            endingTurn = runtimeState != null ? runtimeState.currentTurn : 0,
            message = "完整回合未开始"
        };

        if (!CanStart(runtimeState))
        {
            result.message = "完整回合启动失败：必须处于 Prepare、战斗未结束且没有已有计划";
            return result;
        }

        BattleLifecycleController lifecycleController =
            executionPresenter != null
                ? new BattleLifecycleController(
                    runtimeState,
                    executionPresenter
                )
                : new BattleLifecycleController(runtimeState);
        BattleExecutionPlan executionPlan;
        string failureMessage;
        if (!lifecycleController.TryCreateExecutionPlan(
                false,
                out executionPlan,
                out failureMessage
            ))
        {
            Debug.LogWarning("完整回合启动失败：ExecutionPlan为空，已安全返回Prepare");
            result.message = failureMessage;
            return result;
        }

        result.executedPlan = executionPlan;
        BattleExecutionPlanManager.PrintExecutionPlan(executionPlan);
        if (!TryExecuteCurrentPlan(
                lifecycleController,
                executionPresenter != null,
                out failureMessage
            ))
        {
            result.executionPlanCompleted = executionPlan.isCompleted;
            result.battleEnded = runtimeState.IsBattleEnded;
            result.message = failureMessage;
            result.endingTurn = runtimeState.currentTurn;
            return result;
        }

        return CompleteTurnCycleAfterExecution(
            result,
            lifecycleController,
            runtimeState,
            executionPlan,
            ally01,
            ally02,
            enemy01,
            enemyAttackCardState,
            enemy02,
            enemy02AttackCardState
        );
    }

    internal static BattleAutomaticTurnCycleResult
        CompleteTurnCycleAfterExecution(
            BattleAutomaticTurnCycleResult result,
            BattleLifecycleController lifecycleController,
            BattleRuntimeState runtimeState,
            BattleExecutionPlan executionPlan,
            CharacterData ally01,
            CharacterData ally02,
            CharacterData enemy01,
            BattleCardState enemyAttackCardState,
            CharacterData enemy02,
            BattleCardState enemy02AttackCardState
        )
    {
        if (result == null)
        {
            result = new BattleAutomaticTurnCycleResult
            {
                startingTurn = runtimeState != null
                    ? runtimeState.currentTurn
                    : 0,
                message = "完整回合收尾失败：结果为空"
            };
        }
        if (lifecycleController == null || runtimeState == null ||
            executionPlan == null)
        {
            result.message = "完整回合收尾失败：运行时引用不完整";
            result.endingTurn = runtimeState != null
                ? runtimeState.currentTurn
                : result.startingTurn;
            return result;
        }

        result.executionPlanCompleted = executionPlan.isCompleted;
        result.battleEnded = runtimeState.IsBattleEnded;

        if (!executionPlan.isCompleted)
        {
            Debug.LogWarning("完整回合停止：ExecutionPlan仍有未完成项，保留现场用于诊断");
            result.message = "ExecutionPlan仍有未完成项，本回合未结束";
            result.endingTurn = runtimeState.currentTurn;
            return result;
        }

        if (runtimeState.IsBattleEnded)
        {
            result.isSuccess = true;
            result.message = "战斗结束：" + runtimeState.battleResult;
            result.endingTurn = runtimeState.currentTurn;
            return result;
        }

        if (!lifecycleController.TryEndCurrentTurn(out string failureMessage))
        {
            result.message = failureMessage;
            result.endingTurn = runtimeState.currentTurn;
            return result;
        }

        List<BattleActionSlot> newActionSlots =
            BattleActionSlotManager.CreateLivingPartyActionSlots(
                ally01,
                ally02,
                ActionSlotCountPerCharacter
            );

        if (newActionSlots == null || newActionSlots.Count == 0)
        {
            lifecycleController.EvaluateBattleEnd();
            result.battleEnded = runtimeState.IsBattleEnded;
            result.isSuccess = result.battleEnded;
            result.message = result.battleEnded
                ? "战斗结束：" + runtimeState.battleResult
                : "没有存活角色槽位，无法准备下一回合";
            result.endingTurn = runtimeState.currentTurn;
            return result;
        }

        List<BattleEnemyIntent> newIntentQueue = CreateFixedEnemyIntentQueue(
            enemy01,
            enemyAttackCardState,
            enemy02,
            enemy02AttackCardState,
            ally01,
            ally02,
            newActionSlots
        );

        if (!lifecycleController.TryPrepareNextTurn(
                newActionSlots,
                newIntentQueue,
                out failureMessage
            ))
        {
            result.message = failureMessage;
            result.battleEnded = runtimeState.IsBattleEnded;
            result.endingTurn = runtimeState.currentTurn;
            return result;
        }

        result.advancedToNextTurn =
            runtimeState.currentTurn == result.startingTurn + 1 &&
            runtimeState.LifecyclePhase == BattleLifecyclePhase.Prepare &&
            runtimeState.currentExecutionPlan == null;
        result.isSuccess = result.advancedToNextTurn;
        result.battleEnded = runtimeState.IsBattleEnded;
        result.endingTurn = runtimeState.currentTurn;
        result.message = result.advancedToNextTurn
            ? "当前回合已完成，自动进入回合 " + runtimeState.currentTurn
            : "下一回合准备失败";

        return result;
    }

    internal static bool TryExecuteCurrentPlan(
        BattleLifecycleController lifecycleController,
        bool usePausableExecution,
        out string failureMessage
    )
    {
        if (lifecycleController == null)
        {
            failureMessage = "执行计划失败：生命周期控制器为空";
            return false;
        }

        if (!usePausableExecution)
        {
            return lifecycleController.TryExecuteCurrentPlan(
                out failureMessage
            );
        }

        BattleRollGateSettings settings = new BattleRollGateSettings(
            BattleRollMode.Auto,
            0f,
            0f
        );
        if (!lifecycleController.TryBeginPausableExecution(
                settings,
                out failureMessage
            ))
        {
            return false;
        }

        // A3的场景Presenter会立即完成请求；推进仍由生命周期宿主逐步消费。
        const int maxAdvanceCount = 10000;
        for (int advanceCount = 0;
            advanceCount < maxAdvanceCount;
            advanceCount++)
        {
            BattleExecutionRunner runner = lifecycleController.ExecutionRunner;
            if (runner == null)
            {
                failureMessage = "Pausable执行失败：Runner为空";
                return false;
            }
            if (runner.HasFailed)
            {
                failureMessage = "Pausable执行失败：Runner已失败";
                return false;
            }
            if (runner.IsCompleted)
            {
                failureMessage = string.Empty;
                return true;
            }
            if (!lifecycleController.AdvancePausableExecution(
                    0f,
                    out failureMessage
                ))
            {
                return false;
            }
        }

        failureMessage = "Pausable执行失败：超过最大推进次数";
        return false;
    }

    public static List<BattleEnemyIntent> CreateFixedEnemyIntentQueue(
        CharacterData enemy01,
        BattleCardState enemyAttackCardState,
        CharacterData ally01,
        CharacterData ally02,
        List<BattleActionSlot> actionSlots
    )
    {
        return CreateFixedEnemyIntentQueue(
            enemy01,
            enemyAttackCardState,
            null,
            null,
            ally01,
            ally02,
            actionSlots
        );
    }

    public static List<BattleEnemyIntent> CreateFixedEnemyIntentQueue(
        CharacterData enemy01,
        BattleCardState enemy01AttackCardState,
        CharacterData enemy02,
        BattleCardState enemy02AttackCardState,
        CharacterData ally01,
        CharacterData ally02,
        List<BattleActionSlot> actionSlots
    )
    {
        List<BattleEnemyIntent> intents = new List<BattleEnemyIntent>();
        TryAddFixedEnemyIntent(
            intents,
            enemy01,
            enemy01AttackCardState,
            "Enemy01",
            ally01,
            ally02,
            actionSlots
        );
        if (enemy02 != null || enemy02AttackCardState != null)
        {
            TryAddFixedEnemyIntent(
                intents,
                enemy02,
                enemy02AttackCardState,
                "Enemy02",
                ally01,
                ally02,
                actionSlots
            );
        }
        return BattleEnemyIntentManager.CreateIntentQueue(intents.ToArray());
    }

    static bool TryAddFixedEnemyIntent(
        List<BattleEnemyIntent> intents,
        CharacterData enemy,
        BattleCardState attackCardState,
        string enemyLabel,
        CharacterData ally01,
        CharacterData ally02,
        List<BattleActionSlot> actionSlots
    )
    {
        if (intents == null || enemy == null || attackCardState == null ||
            attackCardState.cardData == null)
        {
            Debug.LogWarning(
                "创建固定敌人意图失败：" + enemyLabel +
                "或敌人卡牌数据不完整"
            );
            return false;
        }
        if (attackCardState.cardData.cardID != FixedEnemyAttackCardID)
        {
            Debug.LogWarning(
                "创建固定敌人意图失败：固定敌人卡必须是 " +
                FixedEnemyAttackCardID
            );
            return false;
        }

        int targetSlotIndex;
        CharacterData target = SelectFixedEnemyIntentTarget(
            ally01,
            ally02,
            actionSlots,
            out targetSlotIndex
        );
        if (target == null)
        {
            Debug.LogWarning("创建固定敌人意图失败：没有可用的存活目标槽位");
            return false;
        }

        CardEligibilityResult eligibility =
            BattleCardManager.EvaluateCardEligibility(
                enemy,
                target,
                attackCardState
            );
        if (eligibility == null || !eligibility.isEligible)
        {
            Debug.LogWarning(
                enemyLabel + "固定攻击当前不可用，不生成意图：" +
                (eligibility != null
                    ? eligibility.failureMessage
                    : "资格检查失败")
            );
            return false;
        }

        int intentOrder = intents.Count + 1;
        intents.Add(new BattleEnemyIntent(
            "ui_fixed_enemy_intent_" + intentOrder.ToString("000"),
            enemy,
            attackCardState,
            target,
            targetSlotIndex,
            intentOrder,
            1
        ));
        Debug.Log(
            "固定敌人意图：" + enemyLabel + " 使用 " +
            FixedEnemyAttackCardID + " 攻击 " +
            target.characterName + " 槽位" + targetSlotIndex
        );
        return true;
    }

    public static CharacterData SelectFixedEnemyIntentTarget(
        CharacterData ally01,
        CharacterData ally02,
        List<BattleActionSlot> actionSlots,
        out int targetSlotIndex
    )
    {
        if (TrySelectTargetSlot(ally01, actionSlots, out targetSlotIndex))
        {
            return ally01;
        }

        if (TrySelectTargetSlot(ally02, actionSlots, out targetSlotIndex))
        {
            return ally02;
        }

        targetSlotIndex = 1;
        return null;
    }

    static bool TrySelectTargetSlot(
        CharacterData owner,
        List<BattleActionSlot> actionSlots,
        out int targetSlotIndex
    )
    {
        targetSlotIndex = 1;

        if (owner == null || owner.IsDead() || actionSlots == null)
        {
            return false;
        }

        int lowestValidSlotIndex = int.MaxValue;

        foreach (BattleActionSlot slot in actionSlots)
        {
            if (slot == null || !object.ReferenceEquals(slot.owner, owner))
            {
                continue;
            }

            if (slot.slotIndex == 1)
            {
                return true;
            }

            if (slot.slotIndex > 0 && slot.slotIndex < lowestValidSlotIndex)
            {
                lowestValidSlotIndex = slot.slotIndex;
            }
        }

        if (lowestValidSlotIndex != int.MaxValue)
        {
            targetSlotIndex = lowestValidSlotIndex;
            Debug.LogWarning(
                "固定敌人目标 " +
                owner.characterName +
                " 缺少槽位1，降级使用最低有效槽位" +
                targetSlotIndex
            );
            return true;
        }

        Debug.LogWarning(
            "固定敌人目标 " +
            owner.characterName +
            " 没有有效行动槽位，尝试下一名存活角色"
        );
        return false;
    }
}
