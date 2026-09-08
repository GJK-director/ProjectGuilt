using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class BattleInteractionStateAndEndLockTests
{
    private sealed class TestContext
    {
        public GameObject root;
        public CharacterData allyA;
        public CharacterData allyB;
        public CharacterData enemy;
        public BattleCardState allyAAttack;
        public BattleCardState allyADefense;
        public BattleCardState allyBAttack;
        public BattleCardState enemyAttack;
        public BattleRuntimeState runtimeState;
        public BattleLifecycleController lifecycleController;
        public BattleSimpleUIController uiController;
        public BattleCardHandUIView handView;
        public BattleCardSelectionController selectionController;
        public BattleActionRelationLineController relationLineController;
        public BattleActionSlotUIView allyASlotView;
        public BattleActionSlotUIView allyBSlotView;
        public BattleActionSlotUIView enemySlotView;
        public BattleEnemyIntent enemyIntent;
    }

    public static bool Run()
    {
        bool[] results = new bool[16];
        TestContext context = null;
        try
        {
            context = CreateInteractionContext();
            RunPlanningInteractionTests(context, results);
            RunBattleEndedLogTests(results);
            RunBattleEndedInteractionTests(context, results);
        }
        catch (Exception exception)
        {
            Debug.LogError("模式78测试夹具异常：" + exception);
        }
        finally
        {
            if (context != null && context.root != null)
            {
                UnityEngine.Object.Destroy(context.root);
            }
        }

        string[] names =
        {
            "Prepare初始无槽位选择且不显示卡牌",
            "合法我方槽位显示正确owner卡牌",
            "成功安排后清除选择并隐藏卡牌",
            "切换槽位后显示对应owner卡牌",
            "安排失败保留槽位选择和卡牌",
            "已安排槽位可再次显示卡牌",
            "成功替换后再次隐藏卡牌",
            "取消安排后保持槽位选择和卡牌",
            "新回合Prepare不默认显示角色1卡牌",
            "Victory首次BattleEnded只打印一次游戏结束",
            "Defeat首次BattleEnded只打印一次游戏结束",
            "重复Evaluate与重建Controller不重复打印",
            "BattleEnded后战斗交互入口均拒绝",
            "BattleEnded后自动回合不创建下一回合",
            "BattleEnded清除临时选择与卡牌展示",
            "BattleEnded保留角色战斗数据与结果"
        };

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式78 测试" + (index + 1) + " " + names[index] +
                "：" + results[index]
            );
            allPassed &= results[index];
        }
        Debug.Log("模式78 16项聚合结果：" + allPassed);
        return allPassed;
    }

    private static void RunPlanningInteractionTests(
        TestContext context,
        bool[] results
    )
    {
        InvokePrivate(context.uiController, "RefreshView");
        results[0] =
            !context.uiController.HasPlanningSlotSelection &&
            context.uiController.VisiblePlanningCardCount == 0 &&
            context.uiController.PlanningHandOwner == null;

        results[1] = context.uiController.TrySelectActionSlotForPlanning(
                context.allyASlotView
            ) &&
            context.uiController.HasPlanningSlotSelection &&
            object.ReferenceEquals(
                context.uiController.PlanningHandOwner,
                context.allyA
            ) &&
            context.uiController.VisiblePlanningCardCount > 0;

        BattleCardUIView allyAttackView = FindSpawnedCardView(
            context.handView,
            "atk_001"
        );
        context.selectionController.SelectCard(allyAttackView);
        InvokePrivate(
            context.uiController,
            "OnEnemyActionSlotClicked",
            context.enemySlotView
        );
        BattleActionSlot allyASlot = FindSlot(
            context.runtimeState,
            context.allyA,
            1
        );
        results[2] = allyASlot != null &&
            object.ReferenceEquals(allyASlot.cardState, context.allyAAttack) &&
            !context.uiController.HasPlanningSlotSelection &&
            !context.selectionController.HasSelection &&
            context.uiController.VisiblePlanningCardCount == 0;

        results[3] = context.uiController.TrySelectActionSlotForPlanning(
                context.allyBSlotView
            ) &&
            object.ReferenceEquals(
                context.uiController.PlanningHandOwner,
                context.allyB
            ) &&
            context.uiController.VisiblePlanningCardCount > 0;

        BattleCardUIView allyBAttackView = FindSpawnedCardView(
            context.handView,
            "atk_001"
        );
        context.selectionController.SelectCard(allyBAttackView);
        context.allyBAttack.currentCooldown = 1;
        InvokePrivate(
            context.uiController,
            "OnEnemyActionSlotClicked",
            context.enemySlotView
        );
        BattleActionSlot allyBSlot = FindSlot(
            context.runtimeState,
            context.allyB,
            1
        );
        results[4] = allyBSlot != null && allyBSlot.IsEmpty() &&
            context.uiController.HasPlanningSlotSelection &&
            context.selectionController.HasSelection &&
            context.uiController.VisiblePlanningCardCount > 0;
        context.allyBAttack.currentCooldown = 0;

        results[5] = context.uiController.TrySelectActionSlotForPlanning(
                context.allyASlotView
            ) &&
            object.ReferenceEquals(
                context.uiController.PlanningHandOwner,
                context.allyA
            ) &&
            context.uiController.VisiblePlanningCardCount > 0;

        BattleCardUIView defenseView = FindSpawnedCardView(
            context.handView,
            "def_001"
        );
        context.selectionController.SelectCard(defenseView);
        InvokePrivate(
            context.uiController,
            "OnEnemyActionSlotClicked",
            context.enemySlotView
        );
        results[6] = allyASlot != null &&
            object.ReferenceEquals(allyASlot.cardState, context.allyADefense) &&
            !context.uiController.HasPlanningSlotSelection &&
            context.uiController.VisiblePlanningCardCount == 0;

        context.uiController.TrySelectActionSlotForPlanning(
            context.allyASlotView
        );
        InvokePrivate(
            context.uiController,
            "OnAllyActionSlotRightClicked",
            context.allyASlotView
        );
        results[7] = allyASlot != null && allyASlot.IsEmpty() &&
            context.uiController.HasPlanningSlotSelection &&
            object.ReferenceEquals(
                context.uiController.PlanningHandOwner,
                context.allyA
            ) &&
            context.uiController.VisiblePlanningCardCount > 0;

        context.uiController.ClearPlanningSelectionAndHideCards();
        context.runtimeState.SetActionSlots(
            BattleActionSlotManager.CreateLivingPartyActionSlots(
                context.allyA,
                context.allyB,
                2
            )
        );
        InvokePrivate(context.uiController, "RefreshView");
        results[8] =
            context.runtimeState.LifecyclePhase == BattleLifecyclePhase.Prepare &&
            !context.uiController.HasPlanningSlotSelection &&
            context.uiController.VisiblePlanningCardCount == 0 &&
            context.uiController.PlanningHandOwner == null;
    }

    private static void RunBattleEndedLogTests(bool[] results)
    {
        int gameEndedLogCount = 0;
        Application.LogCallback callback =
            (condition, stackTrace, type) =>
            {
                if (condition == "游戏结束")
                {
                    gameEndedLogCount++;
                }
            };

        Application.logMessageReceived += callback;
        try
        {
            BattleRuntimeState victory = CreateTerminalRuntime(
                "interaction78_victory"
            );
            victory.enemy.currentHP = 0;
            BattleLifecycleController victoryController =
                new BattleLifecycleController(victory);
            int beforeVictory = gameEndedLogCount;
            BattleResult victoryResult = victoryController.EvaluateBattleEnd();
            int afterVictory = gameEndedLogCount;
            results[9] = victoryResult == BattleResult.Victory &&
                victory.IsBattleEnded &&
                afterVictory == beforeVictory + 1;

            victoryController.EvaluateBattleEnd();
            new BattleLifecycleController(victory).EvaluateBattleEnd();
            int afterVictoryRepeats = gameEndedLogCount;

            BattleRuntimeState defeat = CreateTerminalRuntime(
                "interaction78_defeat"
            );
            defeat.allyA.currentHP = 0;
            defeat.allyB.currentHP = 0;
            BattleLifecycleController defeatController =
                new BattleLifecycleController(defeat);
            int beforeDefeat = gameEndedLogCount;
            BattleResult defeatResult = defeatController.EvaluateBattleEnd();
            int afterDefeat = gameEndedLogCount;
            results[10] = defeatResult == BattleResult.Defeat &&
                defeat.IsBattleEnded &&
                afterDefeat == beforeDefeat + 1;

            defeatController.EvaluateBattleEnd();
            new BattleLifecycleController(defeat).EvaluateBattleEnd();
            results[11] = afterVictoryRepeats == afterVictory &&
                gameEndedLogCount == afterDefeat &&
                gameEndedLogCount == 2;
        }
        finally
        {
            Application.logMessageReceived -= callback;
        }
    }

    private static void RunBattleEndedInteractionTests(
        TestContext context,
        bool[] results
    )
    {
        context.uiController.TrySelectActionSlotForPlanning(
            context.allyASlotView
        );
        BattleCardUIView selectedView = FindSpawnedCardView(
            context.handView,
            "atk_001"
        );
        context.selectionController.SelectCard(selectedView);
        SetPrivateField(
            context.uiController,
            "actionRelationLineController",
            context.relationLineController
        );
        context.relationLineController.BindRuntimeState(context.runtimeState);
        context.relationLineController.SetSelectedSlot("interaction78_temp");
        SetPrivateField(
            context.relationLineController,
            "previewActive",
            true
        );

        context.allyA.AddBuff("Bullet", 2, -1);
        context.allyAAttack.currentCooldown = 1;
        int allyHPBefore = context.allyA.currentHP;
        int buffBefore = context.allyA.GetBuffStack("Bullet");
        int cooldownBefore = context.allyAAttack.currentCooldown;
        List<BattleActionSlot> slotsBefore = context.runtimeState.actionSlots;
        List<BattleEnemyIntent> intentsBefore = context.runtimeState.intentQueue;
        int slotCountBefore = slotsBefore.Count;
        int intentCountBefore = intentsBefore.Count;
        int turnBefore = context.runtimeState.currentTurn;

        BattleLifecyclePhaseContractTests.TryReachPhaseForTest(
            context.runtimeState,
            BattleLifecyclePhase.Executing
        );
        context.enemy.currentHP = 0;
        context.lifecycleController.EvaluateBattleEnd();
        InvokePrivate(context.uiController, "RefreshView");

        results[14] = context.runtimeState.IsBattleEnded &&
            !context.uiController.HasPlanningSlotSelection &&
            !context.selectionController.HasSelection &&
            context.uiController.VisiblePlanningCardCount == 0 &&
            !context.allyASlotView.IsSelected &&
            !context.relationLineController.PreviewActive &&
            string.IsNullOrEmpty(
                context.relationLineController.SelectedSlotID
            );

        BattleResult battleResultBefore = context.runtimeState.battleResult;
        BattleExecutionPlan rejectedPlan;
        string failureMessage;
        BattleActionAssignmentResult assignmentResult;
        bool planRejected = !context.lifecycleController.TryCreateExecutionPlan(
            true,
            out rejectedPlan,
            out failureMessage
        );
        bool executeRejected =
            !context.lifecycleController.TryExecuteCurrentPlan(
                out failureMessage
            );
        bool endRejected = !context.lifecycleController.TryEndCurrentTurn(
            out failureMessage
        );
        bool prepareRejected =
            !context.lifecycleController.TryPrepareNextTurn(
                BattleActionSlotManager.CreateLivingPartyActionSlots(
                    context.allyA,
                    context.allyB,
                    2
                ),
                new List<BattleEnemyIntent>(),
                out failureMessage
            );
        bool assignRejected = !BattleCardAssignmentRouter.TryAssignToEnemySlot(
            context.runtimeState,
            context.allyA,
            1,
            context.allyA,
            context.allyAAttack,
            context.enemy,
            null,
            out assignmentResult
        );
        bool cancelRejected = !BattleCardAssignmentRouter.TryCancelSelectedSlot(
            context.runtimeState,
            context.allyA,
            1,
            out assignmentResult
        );
        bool slotSelectionRejected =
            !context.uiController.TrySelectActionSlotForPlanning(
                context.allyASlotView
            );
        bool showingSinCardsBefore = GetPrivateField<bool>(
            context.uiController,
            "showingSinCards"
        );
        InvokePrivate(context.uiController, "ToggleCardGroup");
        bool modeSwitchRejected = showingSinCardsBefore ==
            GetPrivateField<bool>(context.uiController, "showingSinCards");

        results[12] = planRejected && executeRejected && endRejected &&
            prepareRejected && assignRejected && cancelRejected &&
            slotSelectionRejected && modeSwitchRejected &&
            context.runtimeState.currentTurn == turnBefore &&
            object.ReferenceEquals(context.runtimeState.actionSlots, slotsBefore) &&
            object.ReferenceEquals(context.runtimeState.intentQueue, intentsBefore) &&
            context.runtimeState.battleResult == battleResultBefore;

        BattleAutomaticTurnCycleResult automaticResult =
            BattleAutomaticTurnCycle.TryRun(
                context.runtimeState,
                context.allyA,
                context.allyB,
                context.enemy,
                context.enemyAttack
            );
        results[13] = automaticResult != null &&
            !automaticResult.isSuccess &&
            !automaticResult.advancedToNextTurn &&
            context.runtimeState.currentTurn == turnBefore &&
            object.ReferenceEquals(context.runtimeState.actionSlots, slotsBefore) &&
            object.ReferenceEquals(context.runtimeState.intentQueue, intentsBefore);

        results[15] = object.ReferenceEquals(
                context.runtimeState.allyA,
                context.allyA
            ) &&
            context.allyA.currentHP == allyHPBefore &&
            context.allyA.GetBuffStack("Bullet") == buffBefore &&
            context.allyAAttack.currentCooldown == cooldownBefore &&
            object.ReferenceEquals(context.runtimeState.actionSlots, slotsBefore) &&
            context.runtimeState.actionSlots.Count == slotCountBefore &&
            object.ReferenceEquals(context.runtimeState.intentQueue, intentsBefore) &&
            context.runtimeState.intentQueue.Count == intentCountBefore &&
            context.runtimeState.battleResult == BattleResult.Victory;
    }

    private static TestContext CreateInteractionContext()
    {
        TestContext context = new TestContext
        {
            root = new GameObject("Interaction78Root"),
            allyA = new CharacterData("interaction78_A", 30, 10, 10),
            allyB = new CharacterData("interaction78_B", 30, 9, 9),
            enemy = new CharacterData("interaction78_Enemy", 50, 5, 5)
        };

        context.allyAAttack = CreateCard(
            context.allyA,
            "atk_001",
            CardType.Attack,
            "interaction78_a_attack"
        );
        context.allyADefense = CreateCard(
            context.allyA,
            "def_001",
            CardType.Defense,
            "interaction78_a_defense"
        );
        CreateCard(
            context.allyA,
            "dodge_001",
            CardType.Dodge,
            "interaction78_a_dodge"
        );
        CreateCard(
            context.allyA,
            "atk_bullet_001",
            CardType.Attack,
            "interaction78_a_bullet"
        );
        context.allyBAttack = CreateCard(
            context.allyB,
            "atk_001",
            CardType.Attack,
            "interaction78_b_attack"
        );
        CreateCard(
            context.allyB,
            "def_001",
            CardType.Defense,
            "interaction78_b_defense"
        );
        CreateCard(
            context.allyB,
            "dodge_001",
            CardType.Dodge,
            "interaction78_b_dodge"
        );
        CreateCard(
            context.allyB,
            "atk_bullet_001",
            CardType.Attack,
            "interaction78_b_bullet"
        );
        context.enemyAttack = CreateCard(
            context.enemy,
            "enemy_atk_001",
            CardType.Attack,
            "interaction78_enemy_attack"
        );

        context.runtimeState = new BattleRuntimeState();
        context.runtimeState.SetCharacters(
            context.allyA,
            context.allyB,
            context.enemy
        );
        context.runtimeState.SetActionSlots(
            BattleActionSlotManager.CreateLivingPartyActionSlots(
                context.allyA,
                context.allyB,
                2
            )
        );
        context.enemyIntent = new BattleEnemyIntent(
            "interaction78_intent",
            context.enemy,
            context.enemyAttack,
            context.allyB,
            1,
            1
        );
        context.runtimeState.SetIntentQueue(
            BattleEnemyIntentManager.CreateIntentQueue(context.enemyIntent)
        );
        context.lifecycleController = new BattleLifecycleController(
            context.runtimeState
        );
        string failureMessage;
        context.lifecycleController.TryInitializeToPrepare(out failureMessage);

        GameObject controllerObject = new GameObject(
            "Interaction78Controller"
        );
        controllerObject.transform.SetParent(context.root.transform, false);
        context.uiController =
            controllerObject.AddComponent<BattleSimpleUIController>();

        GameObject handObject = new GameObject(
            "Interaction78Hand",
            typeof(RectTransform),
            typeof(BattleCardHandUIView)
        );
        handObject.transform.SetParent(context.root.transform, false);
        context.handView = handObject.GetComponent<BattleCardHandUIView>();
        BattleCardUIView template = CreateCardTemplate(context.root.transform);
        SetPrivateField(context.handView, "cardViewPrefab", template);
        SetPrivateField(context.handView, "cardContainer", handObject.transform);

        SetPrivateField(context.uiController, "runtimeState", context.runtimeState);
        SetPrivateField(
            context.uiController,
            "lifecycleController",
            context.lifecycleController
        );
        SetPrivateField(context.uiController, "ally01", context.allyA);
        SetPrivateField(context.uiController, "ally02", context.allyB);
        SetPrivateField(context.uiController, "enemy01", context.enemy);
        SetPrivateField(context.uiController, "enemyAttackCardState", context.enemyAttack);
        SetPrivateField(context.uiController, "testCardHandView", context.handView);
        SetPrivateField(
            context.uiController,
            "warnedRuntimeUnitViewsUnavailable",
            true
        );
        context.selectionController = GetPrivateField<BattleCardSelectionController>(
            context.uiController,
            "cardSelectionController"
        );
        context.handView.SetSelectionController(context.selectionController);

        GameObject relationObject = new GameObject(
            "Interaction78Relations",
            typeof(RectTransform),
            typeof(BattleActionRelationLineController)
        );
        relationObject.transform.SetParent(context.root.transform, false);
        context.relationLineController = relationObject.GetComponent<
            BattleActionRelationLineController
        >();

        context.allyASlotView = CreateSlotView(
            context.root.transform,
            "Interaction78AllyASlot",
            context.allyA,
            0,
            false
        );
        context.allyBSlotView = CreateSlotView(
            context.root.transform,
            "Interaction78AllyBSlot",
            context.allyB,
            0,
            false
        );
        context.enemySlotView = CreateSlotView(
            context.root.transform,
            "Interaction78EnemySlot",
            context.enemy,
            0,
            true
        );
        context.enemySlotView.SetBoundEnemyIntent(context.enemyIntent);
        return context;
    }

    private static BattleRuntimeState CreateTerminalRuntime(string prefix)
    {
        CharacterData allyA = new CharacterData(prefix + "_A", 30, 10, 10);
        CharacterData allyB = new CharacterData(prefix + "_B", 30, 8, 8);
        CharacterData enemy = new CharacterData(prefix + "_Enemy", 50, 5, 5);
        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(allyA, allyB, enemy);
        runtimeState.SetActionSlots(
            BattleActionSlotManager.CreateLivingPartyActionSlots(
                allyA,
                allyB,
                2
            )
        );
        runtimeState.SetIntentQueue(new List<BattleEnemyIntent>());
        BattleLifecyclePhaseContractTests.TryReachPhaseForTest(
            runtimeState,
            BattleLifecyclePhase.Executing
        );
        return runtimeState;
    }

    private static BattleCardState CreateCard(
        CharacterData owner,
        string cardID,
        string cardType,
        string instanceID
    )
    {
        return BattleCardManager.CreateBattleCard(
            owner,
            new CardTestData
            {
                cardID = cardID,
                cardName = cardID,
                cardType = cardType,
                isClashable = cardType != "Ability",
                minPoint = 6,
                maxPoint = 6,
                cooldown = 0,
                damageFormula = "PointAsDamage"
            },
            instanceID
        );
    }

    private static BattleCardUIView CreateCardTemplate(Transform parent)
    {
        GameObject templateObject = new GameObject(
            "Interaction78CardTemplate",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(BattleCardVisualStyle),
            typeof(BattleCardUIView)
        );
        templateObject.transform.SetParent(parent, false);
        BattleCardUIView view = templateObject.GetComponent<BattleCardUIView>();
        BattleCardVisualStyle style =
            templateObject.GetComponent<BattleCardVisualStyle>();
        TMP_Text nameText = CreateText(templateObject.transform, "Name");
        TMP_Text pointText = CreateText(templateObject.transform, "Point");
        TMP_Text typeText = CreateText(templateObject.transform, "Type");
        TMP_Text descriptionText = CreateText(
            templateObject.transform,
            "Description"
        );
        SetPrivateField(view, "cardNameText", nameText);
        SetPrivateField(view, "pointText", pointText);
        SetPrivateField(view, "typeText", typeText);
        SetPrivateField(view, "descriptionText", descriptionText);
        SetPrivateField(view, "visualStyle", style);
        SetPrivateField(
            style,
            "frameImage",
            templateObject.GetComponent<Image>()
        );
        templateObject.SetActive(false);
        return view;
    }

    private static TMP_Text CreateText(Transform parent, string name)
    {
        GameObject textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        textObject.transform.SetParent(parent, false);
        return textObject.GetComponent<TextMeshProUGUI>();
    }

    private static BattleActionSlotUIView CreateSlotView(
        Transform parent,
        string name,
        CharacterData character,
        int zeroBasedSlotIndex,
        bool enemySlot
    )
    {
        GameObject slotObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(BattleActionSlotUIView)
        );
        slotObject.transform.SetParent(parent, false);
        BattleActionSlotUIView view =
            slotObject.GetComponent<BattleActionSlotUIView>();
        view.BindInteraction(
            character,
            zeroBasedSlotIndex,
            enemySlot,
            null,
            null
        );
        return view;
    }

    private static BattleActionSlot FindSlot(
        BattleRuntimeState runtimeState,
        CharacterData owner,
        int formalSlotIndex
    )
    {
        if (runtimeState == null || runtimeState.actionSlots == null)
        {
            return null;
        }
        foreach (BattleActionSlot slot in runtimeState.actionSlots)
        {
            if (slot != null &&
                object.ReferenceEquals(slot.owner, owner) &&
                slot.slotIndex == formalSlotIndex)
            {
                return slot;
            }
        }
        return null;
    }

    private static BattleCardUIView FindSpawnedCardView(
        BattleCardHandUIView handView,
        string cardID
    )
    {
        if (handView == null)
        {
            return null;
        }
        foreach (BattleCardUIView view in handView.SpawnedCardViews)
        {
            if (view != null &&
                view.BoundCardState != null &&
                view.BoundCardState.cardData != null &&
                view.BoundCardState.cardData.cardID == cardID)
            {
                return view;
            }
        }
        return null;
    }

    private static void InvokePrivate(
        object target,
        string methodName,
        params object[] arguments
    )
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        if (method == null)
        {
            throw new MissingMethodException(target.GetType().Name, methodName);
        }
        method.Invoke(target, arguments);
    }

    private static void SetPrivateField(
        object target,
        string fieldName,
        object value
    )
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        if (field == null)
        {
            throw new MissingFieldException(target.GetType().Name, fieldName);
        }
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        if (field == null)
        {
            throw new MissingFieldException(target.GetType().Name, fieldName);
        }
        return (T)field.GetValue(target);
    }
}
