using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public static class BattleActionRelationMode73Tests
{
    private sealed class QueryFixture
    {
        public BattleRuntimeState runtime;
        public CharacterData allyA;
        public CharacterData allyB;
        public CharacterData enemy;
        public CardTestData attackData;
        public CardTestData defenseData;
        public CardTestData dodgeData;
        public BattleCardState allyAttackA;
        public BattleCardState allyAttackB;
        public BattleCardState allyDefense;
        public BattleCardState allyDodge;
        public BattleCardState enemyAttackA;
        public BattleCardState enemyAttackB;
        public BattleActionSlot slotA1;
        public BattleActionSlot slotA2;
        public BattleActionSlot slotB1;
        public BattleActionSlot slotB2;
        public BattleEnemyIntent intent1;
    }

    private sealed class DisplayFixture
    {
        public GameObject root;
        public Texture2D texture;
        public Sprite sprite;
        public Canvas canvas;
        public RectTransform lineLayer;
        public RectTransform dashedRoot;
        public RectTransform clashRoot;
        public RectTransform highlightRoot;
        public RectTransform previewRoot;
        public BattleActionRelationLineController controller;
        public BattleBezierRelationLineUIView previewCurve;
        public Dictionary<string, BattleActionSlotUIView> slots;
    }

    private static readonly string[] TestNames =
    {
        "敌方单方面攻击生成EnemyUnilateralAttack",
        "玩家单方面攻击生成PlayerUnilateralAttack",
        "EnemyUnilateralAttack使用敌方槽位为源",
        "PlayerUnilateralAttack使用我方槽位为源",
        "形成Clash后生成一个Clash关系",
        "Clash替代同一对行动的单方面关系",
        "防御直接响应生成DefenseResponse",
        "守备行动不生成关系线",
        "闪避直接响应生成EvadeResponse",
        "已取消行动不生成关系",
        "被替换关系不生成历史线",
        "同一关系从双方槽位查询得到相同relationID",
        "GetAllCurrentRelations按relationID去重",
        "敌方空槽有incoming攻击时可以查到关系",
        "我方空槽有incoming攻击时可以查到关系",
        "没有关系的空槽返回空集合",
        "多个敌方攻击同一槽位时全部返回",
        "多个玩家行动指向不同敌方槽位时全部返回",
        "悬停我方槽位只显示相关关系",
        "悬停敌方槽位只显示相关关系",
        "PointerExit隐藏悬停关系",
        "快速槽位切换不会被旧Exit错误清空",
        "Tab按住显示全部关系",
        "Tab松开且仍有Hover时恢复Hover关系",
        "Tab松开且没有Hover时隐藏全部正式关系",
        "Tab全显时Hover关系进入Highlight",
        "Tab全显时其他关系保持Normal",
        "正式关系不会从两端重复绘制",
        "卡牌预览线不因Tab松开而隐藏",
        "Tab全显不会复制预览线",
        "普通曲线起点等于源Anchor",
        "普通曲线箭头尖端等于目标Anchor",
        "曲线控制点位于双方上方",
        "起终点交换后曲线不会翻到下方",
        "线段沿近似弧长等距排列",
        "虚线步长大于线段长度",
        "实线步长小于等于线段长度",
        "箭头方向符合末端切线",
        "所有Image Raycast Target为false",
        "重复刷新不增加Segment池",
        "长距离后池扩展一次并稳定",
        "短距离后多余Segment隐藏但不销毁",
        "Clash启用两条Curve",
        "玩家Curve使用Player Color",
        "敌方Curve使用Enemy Color",
        "两条Curve均为Solid",
        "玩家箭头指向拼点中心",
        "敌方箭头指向拼点中心",
        "两个箭头方向相反",
        "两箭头尖端间距等于Clash Arrow Gap",
        "两箭头不重叠",
        "Clash高亮时两条Curve同时高亮",
        "Clash整体进入HighlightRoot",
        "取消高亮后回到NormalClashRoot",
        "NormalDashedRoot低于NormalClashRoot",
        "NormalClashRoot低于HighlightRoot",
        "HighlightRoot低于PreviewRoot",
        "玩家预览线始终位于PreviewRoot",
        "多条关系共享目标时LaneIndex不同",
        "重复Refresh后LaneIndex稳定",
        "Tab切换前后LaneIndex稳定",
        "Hover高亮不改变曲线弧度",
        "Underlay关闭不影响主线",
        "Underlay开启时底边尺寸大于主线",
        "可用卡牌进入目标选择后显示蓝色虚线",
        "起点为当前选择我方行动槽",
        "终点跟随测试鼠标位置",
        "不可用CD卡不显示预览线",
        "非法目标点击后预览线继续存在",
        "成功指派后预览线隐藏",
        "取消卡牌后预览线隐藏",
        "切换源槽位后旧预览线隐藏或正确重建",
        "进入执行阶段后全部关系线隐藏",
        "返回新规划阶段后可以重新显示"
    };

    private static readonly string[] VisualSafetyTestNames =
    {
        "RelationLineLayer不阻挡射线",
        "RelationLineLayer不可交互",
        "运行时Graphic激活前已关闭RaycastTarget",
        "对象池复用后Segment仍不参与射线",
        "Underlay不参与射线",
        "Arrow不参与射线",
        "Preview全部Graphic不参与射线",
        "RelationView与PrimarySecondary均不阻挡射线",
        "Clash使用普通关系线共享控制点入口",
        "Clash没有额外曲线高度",
        "Clash两段引用同一条完整曲线",
        "玩家段覆盖共享曲线左半部分",
        "敌人段反向覆盖共享曲线右半部分",
        "两支箭头沿共享曲线切线相向",
        "ClashArrowGap只改变中间间隔",
        "Highlight不改变Clash曲线形状",
        "普通单向关系线几何保持不变",
        "Hover与Tab显示行为保持不变"
    };

    public static bool Run()
    {
        Debug.Log("===== BattleActionRelationLineBasic 模式73开始 =====");
        bool[] results = new bool[74];
        bool inputCompatibilityPassed = false;
        bool visualSafetyPassed = false;
        QueryFixture baseFixture = CreateQueryFixture();
        RunQueryTests(results, baseFixture);

        DisplayFixture display = CreateDisplayFixture(baseFixture);
        try
        {
            RunControllerTests(results, baseFixture, display);
            RunCurveAndClashTests(results, display);
            RunLayerLaneAndPreviewTests(results, baseFixture, display);
            visualSafetyPassed = RunRaycastAndSharedClashTests(
                display,
                baseFixture
            );
            inputCompatibilityPassed =
                RunInputCompatibilityTests(display);
        }
        finally
        {
            DestroyDisplayFixture(display);
        }

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式73 测试" + (index + 1) + " " +
                TestNames[index] + "：" + results[index]
            );
            allPassed &= results[index];
        }
        Debug.Log("===== BattleActionRelationLineBasic 模式73核心测试结束 =====");
        return allPassed && inputCompatibilityPassed &&
            visualSafetyPassed;
    }

    private static bool RunRaycastAndSharedClashTests(
        DisplayFixture display,
        QueryFixture fixture
    )
    {
        bool[] results = new bool[VisualSafetyTestNames.Length];
        CanvasGroup layerGroup = display.controller.RelationLayerCanvasGroup;
        results[0] = layerGroup != null && !layerGroup.blocksRaycasts;
        results[1] = layerGroup != null && !layerGroup.interactable;

        BattleBezierRelationLineUIView preview = display.previewCurve;
        Vector2 previewStart = new Vector2(-120f, -30f);
        Vector2 previewEnd = new Vector2(180f, 45f);
        Vector2 previewControl =
            BattleBezierRelationLineUIView.ResolveControlPoint(
                previewStart,
                previewEnd,
                130f,
                0.12f,
                100f,
                320f,
                0f
            );
        preview.ConfigureGeometryForTesting(
            new Vector2(12f, 4f),
            8f,
            1f,
            true
        );
        preview.Render(
            previewStart,
            previewControl,
            previewEnd,
            Color.cyan,
            true,
            false
        );
        results[2] = preview.ActiveSegmentCount > 0 &&
            preview.LastActivationRaycastSafe;
        preview.SetRenderedRaycastTargetsForTesting(true);
        preview.Render(
            previewStart,
            previewControl,
            previewEnd,
            Color.cyan,
            true,
            false
        );
        results[3] = preview.AllRenderedImagesIgnoreRaycasts &&
            preview.LastActivationRaycastSafe;
        results[4] = preview.UnderlayImagesIgnoreRaycasts;
        results[5] = preview.ArrowImagesIgnoreRaycasts;
        results[6] = preview.AllRenderedImagesIgnoreRaycasts &&
            preview.CanvasGroupIgnoresRaycasts;

        BattleActionRelationUIView clashView =
            CreateStandaloneRelationView(display);
        results[7] = clashView.CanvasGroupIgnoresRaycasts &&
            clashView.PrimaryCurve.CanvasGroupIgnoresRaycasts &&
            clashView.SecondaryCurve.CanvasGroupIgnoresRaycasts;

        BattleActionRelationDescriptor clashDescriptor =
            new BattleActionRelationDescriptor(
                "Mode73SharedClash",
                BattleActionRelationKind.Clash,
                "AllyA:1",
                "Enemy:1",
                "AllyA:1",
                "Enemy:1",
                BattleActionRelationSide.Player,
                1,
                1
            );
        Vector2 playerStart = new Vector2(-220f, -55f);
        Vector2 enemyStart = new Vector2(210f, 35f);
        const float baseHeight = 130f;
        const float distanceFactor = 0.12f;
        const float minHeight = 100f;
        const float maxHeight = 320f;
        const float laneSpacing = 28f;
        const float firstGap = 10f;
        Vector2 expectedSharedControl =
            BattleBezierRelationLineUIView.ResolveControlPoint(
                playerStart,
                enemyStart,
                baseHeight,
                distanceFactor,
                minHeight,
                maxHeight,
                clashDescriptor.LaneIndex * laneSpacing
            );
        clashView.ShowClash(
            clashDescriptor,
            playerStart,
            enemyStart,
            Color.cyan,
            Color.red,
            false,
            baseHeight,
            distanceFactor,
            minHeight,
            maxHeight,
            laneSpacing,
            firstGap
        );
        BattleBezierRelationLineUIView primary = clashView.PrimaryCurve;
        BattleBezierRelationLineUIView secondary = clashView.SecondaryCurve;
        results[8] = Approximately(
            primary.SourceCurveControlPoint,
            expectedSharedControl
        );
        results[9] = Approximately(
            secondary.SourceCurveControlPoint,
            expectedSharedControl
        );
        results[10] = Approximately(
            primary.SourceCurveStart,
            secondary.SourceCurveStart
        ) && Approximately(
            primary.SourceCurveControlPoint,
            secondary.SourceCurveControlPoint
        ) && Approximately(
            primary.SourceCurveEnd,
            secondary.SourceCurveEnd
        );
        Vector2 expectedPlayerTip =
            BattleBezierRelationLineUIView.EvaluateQuadraticBezier(
                playerStart,
                expectedSharedControl,
                enemyStart,
                primary.RangeEnd
            );
        results[11] = Mathf.Approximately(primary.RangeStart, 0f) &&
            primary.RangeEnd < 0.5f &&
            Approximately(primary.ArrowTip, expectedPlayerTip);
        Vector2 expectedEnemyTip =
            BattleBezierRelationLineUIView.EvaluateQuadraticBezier(
                playerStart,
                expectedSharedControl,
                enemyStart,
                secondary.RangeEnd
            );
        results[12] = Mathf.Approximately(secondary.RangeStart, 1f) &&
            secondary.RangeEnd > 0.5f &&
            Approximately(secondary.ArrowTip, expectedEnemyTip);

        Vector2 playerTangent =
            BattleBezierRelationLineUIView.EvaluateQuadraticTangent(
                playerStart,
                expectedSharedControl,
                enemyStart,
                primary.RangeEnd
            );
        Vector2 enemyTangent =
            -BattleBezierRelationLineUIView.EvaluateQuadraticTangent(
                playerStart,
                expectedSharedControl,
                enemyStart,
                secondary.RangeEnd
            );
        float expectedPlayerAngle = Mathf.Atan2(
            playerTangent.y,
            playerTangent.x
        ) * Mathf.Rad2Deg;
        float expectedEnemyAngle = Mathf.Atan2(
            enemyTangent.y,
            enemyTangent.x
        ) * Mathf.Rad2Deg;
        results[13] = Mathf.Abs(Mathf.DeltaAngle(
            primary.ArrowAngle,
            expectedPlayerAngle
        )) < 4f && Mathf.Abs(Mathf.DeltaAngle(
            secondary.ArrowAngle,
            expectedEnemyAngle
        )) < 4f;

        Vector2 sharedBeforeGap = primary.SourceCurveControlPoint;
        clashView.ShowClash(
            clashDescriptor,
            playerStart,
            enemyStart,
            Color.cyan,
            Color.red,
            false,
            baseHeight,
            distanceFactor,
            minHeight,
            maxHeight,
            laneSpacing,
            30f
        );
        float largerGap = Vector2.Distance(
            primary.ArrowTip,
            secondary.ArrowTip
        );
        results[14] = Mathf.Abs(largerGap - 30f) < 0.01f &&
            Approximately(
                sharedBeforeGap,
                primary.SourceCurveControlPoint
            );

        clashView.ShowClash(
            clashDescriptor,
            playerStart,
            enemyStart,
            Color.cyan,
            Color.red,
            false,
            baseHeight,
            distanceFactor,
            minHeight,
            maxHeight,
            laneSpacing,
            firstGap
        );
        Vector2 controlBeforeHighlight =
            primary.SourceCurveControlPoint;
        Vector2 playerTipBeforeHighlight = primary.ArrowTip;
        Vector2 enemyTipBeforeHighlight = secondary.ArrowTip;
        clashView.ShowClash(
            clashDescriptor,
            playerStart,
            enemyStart,
            Color.cyan,
            Color.red,
            true,
            baseHeight,
            distanceFactor,
            minHeight,
            maxHeight,
            laneSpacing,
            firstGap
        );
        results[15] = Approximately(
            controlBeforeHighlight,
            primary.SourceCurveControlPoint
        ) && Approximately(
            playerTipBeforeHighlight,
            primary.ArrowTip
        ) && Approximately(
            enemyTipBeforeHighlight,
            secondary.ArrowTip
        );

        BattleActionRelationDescriptor unilateralDescriptor =
            new BattleActionRelationDescriptor(
                "Mode73UnilateralGeometry",
                BattleActionRelationKind.EnemyUnilateralAttack,
                "Enemy:1",
                "AllyA:1",
                "Enemy:1",
                "AllyA:1",
                BattleActionRelationSide.Enemy,
                1,
                1
            );
        clashView.ShowUnilateral(
            unilateralDescriptor,
            playerStart,
            enemyStart,
            Color.red,
            false,
            baseHeight,
            distanceFactor,
            minHeight,
            maxHeight,
            laneSpacing
        );
        results[16] = Approximately(
            clashView.PrimaryCurve.ControlPoint,
            expectedSharedControl
        );

        display.controller.BindRuntimeState(fixture.runtime);
        RegisterDisplaySlots(display, fixture);
        display.controller.SetHoveredSlot("AllyA:2");
        display.controller.SetRevealAllHeld(false);
        int hoveredCount = display.controller.VisibleRelationCount;
        display.controller.SetRevealAllHeld(true);
        bool allVisible = display.controller.VisibleRelationCount ==
            display.controller.CachedRelations.Count;
        display.controller.SetRevealAllHeld(false);
        bool hoverRestored = display.controller.VisibleRelationCount ==
            hoveredCount;
        results[17] = hoveredCount > 0 && allVisible && hoverRestored;

        UnityEngine.Object.Destroy(clashView.gameObject);

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式73 关系线安全测试" + (index + 1) + " " +
                VisualSafetyTestNames[index] + "：" + results[index]
            );
            allPassed &= results[index];
        }
        return allPassed;
    }

    private static bool RunInputCompatibilityTests(DisplayFixture display)
    {
        display.controller.SetHoveredSlot("AllyA:2");
        display.controller.SetRevealAllHeld(true);
        bool revealAllVisible =
            display.controller.VisibleRelationCount ==
            display.controller.CachedRelations.Count;

        display.controller.SetRevealAllHeld(false);
        bool hoverRestored =
            display.controller.VisibleRelationCount ==
            CountRelationsForSlot(
                display.controller.CachedRelations,
                "AllyA:2"
            );

        display.controller.ClearHoveredSlot("AllyA:2");
        display.controller.SetRevealAllHeld(false);
        bool noHoverHidden = display.controller.VisibleRelationCount == 0;

        display.controller.SetRevealAllHeld(true);
        display.root.SetActive(false);
        bool disableCleared = !display.controller.RevealAllHeld;
        bool nullKeyboardNotHeld =
            !BattleActionRelationLineController.IsRevealAllInputHeld(null);
        display.root.SetActive(true);

        Debug.Log("模式73 输入兼容 Tab按住显示全部：" + revealAllVisible);
        Debug.Log("模式73 输入兼容 Tab松开恢复Hover：" + hoverRestored);
        Debug.Log("模式73 输入兼容 Tab松开且无Hover隐藏：" + noHoverHidden);
        Debug.Log("模式73 输入兼容 OnDisable清除全显状态：" + disableCleared);
        Debug.Log("模式73 输入兼容 Keyboard为空视为未按下：" + nullKeyboardNotHeld);

        return revealAllVisible && hoverRestored && noHoverHidden &&
            disableCleared && nullKeyboardNotHeld;
    }

    private static void RunQueryTests(bool[] r, QueryFixture fixture)
    {
        BattleActionRelationQueryService query =
            new BattleActionRelationQueryService(fixture.runtime);
        IReadOnlyList<BattleActionRelationDescriptor> all =
            query.GetAllCurrentRelations();
        BattleActionRelationDescriptor enemyRelation = FindKind(
            all,
            BattleActionRelationKind.EnemyUnilateralAttack
        );
        BattleActionRelationDescriptor playerRelation = FindKind(
            all,
            BattleActionRelationKind.PlayerUnilateralAttack
        );
        r[0] = enemyRelation != null;
        r[1] = playerRelation != null;
        r[2] = enemyRelation != null &&
            enemyRelation.SourceSide == BattleActionRelationSide.Enemy &&
            enemyRelation.SourceSlotID == "Enemy:1";
        r[3] = playerRelation != null &&
            playerRelation.SourceSide == BattleActionRelationSide.Player &&
            playerRelation.SourceSlotID == "AllyA:1";

        QueryFixture clashFixture = CreateQueryFixture();
        clashFixture.slotA1.AssignResponse(
            clashFixture.allyA,
            clashFixture.allyAttackA,
            clashFixture.intent1,
            false
        );
        clashFixture.intent1.isResponded = true;
        clashFixture.intent1.SetActualTarget(
            clashFixture.allyA,
            clashFixture.slotA1.slotIndex
        );
        BattleActionRelationQueryService clashQuery =
            new BattleActionRelationQueryService(clashFixture.runtime);
        IReadOnlyList<BattleActionRelationDescriptor> clashRelations =
            clashQuery.GetAllCurrentRelations();
        r[4] = CountKind(
            clashRelations,
            BattleActionRelationKind.Clash
        ) == 1;
        r[5] = clashRelations.Count == 1 &&
            CountKind(
                clashRelations,
                BattleActionRelationKind.EnemyUnilateralAttack
            ) == 0 &&
            CountKind(
                clashRelations,
                BattleActionRelationKind.PlayerUnilateralAttack
            ) == 0;

        QueryFixture defenseFixture = CreateQueryFixture();
        defenseFixture.slotA1.AssignResponse(
            defenseFixture.allyA,
            defenseFixture.allyDefense,
            defenseFixture.intent1,
            false
        );
        defenseFixture.intent1.isResponded = true;
        defenseFixture.intent1.SetActualTarget(
            defenseFixture.allyA,
            defenseFixture.slotA1.slotIndex
        );
        r[6] = CountKind(
            new BattleActionRelationQueryService(
                defenseFixture.runtime
            ).GetAllCurrentRelations(),
            BattleActionRelationKind.DefenseResponse
        ) == 1;

        QueryFixture guardFixture = CreateQueryFixture();
        guardFixture.slotA1.AssignPassiveGuard(
            guardFixture.allyA,
            guardFixture.allyDefense
        );
        IReadOnlyList<BattleActionRelationDescriptor> guardRelations =
            new BattleActionRelationQueryService(
                guardFixture.runtime
            ).GetAllCurrentRelations();
        r[7] = FindSource(guardRelations, "AllyA:1") == null;

        QueryFixture dodgeFixture = CreateQueryFixture();
        dodgeFixture.slotA1.AssignResponse(
            dodgeFixture.allyA,
            dodgeFixture.allyDodge,
            dodgeFixture.intent1,
            false
        );
        dodgeFixture.intent1.isResponded = true;
        dodgeFixture.intent1.SetActualTarget(
            dodgeFixture.allyA,
            dodgeFixture.slotA1.slotIndex
        );
        r[8] = CountKind(
            new BattleActionRelationQueryService(
                dodgeFixture.runtime
            ).GetAllCurrentRelations(),
            BattleActionRelationKind.EvadeResponse
        ) == 1;

        fixture.slotA1.Clear();
        IReadOnlyList<BattleActionRelationDescriptor> afterCancel =
            query.GetAllCurrentRelations();
        r[9] = CountKind(
            afterCancel,
            BattleActionRelationKind.PlayerUnilateralAttack
        ) == 0;
        fixture.slotA1.AssignFreeAction(
            fixture.allyA,
            fixture.allyAttackA,
            fixture.enemy
        );
        fixture.slotA1.requestedTargetSlotIndex = 1;
        IReadOnlyList<BattleActionRelationDescriptor> afterReplace =
            query.GetAllCurrentRelations();
        r[10] = ContainsRelation(
            afterReplace,
            "AllyA:1->Enemy:1"
        ) && !ContainsRelation(
            afterReplace,
            "AllyA:1->Enemy:2"
        );

        string fromSource = query.GetRelationsForSlot("Enemy:1")[0].RelationID;
        IReadOnlyList<BattleActionRelationDescriptor> fromTargetList =
            query.GetRelationsForSlot("AllyA:2");
        r[11] = fromTargetList.Count > 0 &&
            fromSource == fromTargetList[0].RelationID;
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        bool unique = true;
        all = query.GetAllCurrentRelations();
        for (int index = 0; index < all.Count; index++)
        {
            unique &= ids.Add(all[index].RelationID);
        }
        r[12] = unique;

        fixture.slotA1.requestedTargetSlotIndex = 2;
        r[13] = query.GetRelationsForSlot("Enemy:2").Count == 1;
        r[14] = query.GetRelationsForSlot("AllyA:2").Count == 1;
        r[15] = query.GetRelationsForSlot("AllyB:2").Count == 0;

        BattleEnemyIntent secondIntent = new BattleEnemyIntent(
            "relation73_intent_2",
            fixture.enemy,
            fixture.enemyAttackB,
            fixture.allyA,
            2,
            2,
            2
        );
        fixture.runtime.intentQueue.Add(secondIntent);
        r[16] = query.GetRelationsForSlot("AllyA:2").Count == 2;

        fixture.slotB1.AssignFreeAction(
            fixture.allyB,
            fixture.allyAttackB,
            fixture.enemy
        );
        fixture.slotB1.requestedTargetSlotIndex = 1;
        r[17] = CountKind(
            query.GetAllCurrentRelations(),
            BattleActionRelationKind.PlayerUnilateralAttack
        ) == 2;
    }

    private static void RunControllerTests(
        bool[] r,
        QueryFixture fixture,
        DisplayFixture display
    )
    {
        display.controller.SetHoveredSlot("AllyA:2");
        r[18] = display.controller.VisibleRelationCount == 2;
        display.controller.SetHoveredSlot("Enemy:2");
        r[19] = display.controller.VisibleRelationCount >= 1;
        display.controller.ClearHoveredSlot("Enemy:2");
        r[20] = display.controller.VisibleRelationCount == 0;
        display.controller.SetHoveredSlot("AllyA:2");
        display.controller.SetHoveredSlot("Enemy:2");
        display.controller.ClearHoveredSlot("AllyA:2");
        r[21] = display.controller.HoveredSlotID == "Enemy:2" &&
            display.controller.VisibleRelationCount >= 1;

        display.controller.SetRevealAllHeld(true);
        int allCount = display.controller.CachedRelations.Count;
        r[22] = display.controller.VisibleRelationCount == allCount;
        display.controller.SetRevealAllHeld(false);
        r[23] = display.controller.VisibleRelationCount ==
            CountRelationsForSlot(
                display.controller.CachedRelations,
                "Enemy:2"
            );
        display.controller.ClearHoveredSlot("Enemy:2");
        r[24] = display.controller.VisibleRelationCount == 0;

        display.controller.SetHoveredSlot("AllyA:2");
        display.controller.SetRevealAllHeld(true);
        bool hasHighlight = false;
        bool hasNormal = false;
        for (int index = 0;
             index < display.controller.VisibleRelationCount;
             index++)
        {
            BattleActionRelationUIView view =
                display.controller.GetVisibleView(index);
            hasHighlight |= view != null && view.IsHighlighted;
            hasNormal |= view != null && !view.IsHighlighted;
        }
        r[25] = hasHighlight;
        r[26] = hasNormal;
        r[27] = display.controller.VisibleRelationCount == allCount;

        display.controller.BeginCardTargetingPreview("AllyA:1");
        display.controller.SetRevealAllHeld(false);
        r[28] = display.controller.PreviewActive &&
            display.previewCurve.IsVisible;
        int previewPoolBefore = display.previewCurve.SegmentPoolCount;
        display.controller.SetRevealAllHeld(true);
        r[29] = display.previewCurve.SegmentPoolCount == previewPoolBefore;
    }

    private static void RunCurveAndClashTests(
        bool[] r,
        DisplayFixture display
    )
    {
        BattleBezierRelationLineUIView curve = display.previewCurve;
        curve.ConfigureGeometryForTesting(
            new Vector2(10f, 3f),
            6f,
            2f,
            true
        );
        Vector2 start = new Vector2(-200f, -50f);
        Vector2 end = new Vector2(220f, 30f);
        Vector2 control =
            BattleBezierRelationLineUIView.ResolveControlPoint(
                start, end, 100f, 0.1f, 80f, 240f, 0f
            );
        curve.Render(start, control, end, Color.cyan, true, false);
        r[30] = Approximately(curve.StartPoint, start);
        r[31] = Approximately(curve.ArrowTip, end);
        r[32] = control.y > Mathf.Max(start.y, end.y);
        Vector2 swapped =
            BattleBezierRelationLineUIView.ResolveControlPoint(
                end, start, 100f, 0.1f, 80f, 240f, 0f
            );
        r[33] = swapped.y > Mathf.Max(start.y, end.y);

        bool nearEqualSpacing = curve.ActiveSegmentCount > 3;
        if (nearEqualSpacing)
        {
            float firstDistance = Vector2.Distance(
                curve.GetSegmentPosition(0),
                curve.GetSegmentPosition(1)
            );
            float secondDistance = Vector2.Distance(
                curve.GetSegmentPosition(1),
                curve.GetSegmentPosition(2)
            );
            nearEqualSpacing = Mathf.Abs(firstDistance - secondDistance) < 2f;
        }
        r[34] = nearEqualSpacing;
        r[35] = curve.SegmentStep > curve.MainSegmentSize.x;
        curve.Render(start, control, end, Color.cyan, false, false);
        r[36] = curve.SegmentStep <= curve.MainSegmentSize.x;
        float expectedAngle = Mathf.Atan2(
            end.y - control.y,
            end.x - control.x
        ) * Mathf.Rad2Deg;
        r[37] = Mathf.Abs(Mathf.DeltaAngle(
            curve.ArrowAngle,
            expectedAngle
        )) < 4f;
        r[38] = curve.AllRenderedImagesIgnoreRaycasts;
        int poolAfterFirst = curve.SegmentPoolCount;
        curve.Render(start, control, end, Color.cyan, false, false);
        r[39] = curve.SegmentPoolCount == poolAfterFirst;
        Vector2 farEnd = new Vector2(900f, 200f);
        Vector2 farControl =
            BattleBezierRelationLineUIView.ResolveControlPoint(
                start, farEnd, 100f, 0.1f, 80f, 300f, 0f
            );
        curve.Render(start, farControl, farEnd, Color.cyan, false, false);
        int expandedPool = curve.SegmentPoolCount;
        curve.Render(start, farControl, farEnd, Color.cyan, false, false);
        r[40] = expandedPool > poolAfterFirst &&
            curve.SegmentPoolCount == expandedPool;
        curve.Render(
            Vector2.zero,
            new Vector2(20f, 25f),
            new Vector2(40f, 0f),
            Color.cyan,
            false,
            false
        );
        r[41] = curve.SegmentPoolCount == expandedPool &&
            curve.ActiveSegmentCount < expandedPool;

        BattleActionRelationUIView clashView =
            CreateStandaloneRelationView(display);
        BattleActionRelationDescriptor descriptor =
            new BattleActionRelationDescriptor(
                "AllyA:1<->Enemy:1",
                BattleActionRelationKind.Clash,
                "AllyA:1",
                "Enemy:1",
                "AllyA:1",
                "Enemy:1",
                BattleActionRelationSide.Player,
                1,
                1
            );
        Color player = Color.cyan;
        Color enemy = new Color(1f, 0.2f, 0.1f, 1f);
        const float arrowGap = 36f;
        clashView.ShowClash(
            descriptor,
            new Vector2(-180f, -40f),
            new Vector2(180f, 20f),
            player,
            enemy,
            true,
            160f,
            0.1f,
            100f,
            300f,
            25f,
            arrowGap
        );
        BattleBezierRelationLineUIView primary = clashView.PrimaryCurve;
        BattleBezierRelationLineUIView secondary = clashView.SecondaryCurve;
        r[42] = primary.IsVisible && secondary.IsVisible;
        r[43] = Approximately(primary.LastColor, player);
        r[44] = Approximately(secondary.LastColor, enemy);
        r[45] = !primary.IsDashed && !secondary.IsDashed;
        Vector2 arrowDirection = secondary.ArrowTip - primary.ArrowTip;
        float playerAngle = Mathf.Atan2(
            arrowDirection.y,
            arrowDirection.x
        ) * Mathf.Rad2Deg;
        r[46] = Mathf.Abs(Mathf.DeltaAngle(
            primary.ArrowAngle,
            playerAngle
        )) < 15f;
        r[47] = Mathf.Abs(Mathf.DeltaAngle(
            secondary.ArrowAngle,
            playerAngle + 180f
        )) < 15f;
        r[48] = Mathf.Abs(Mathf.Abs(Mathf.DeltaAngle(
            primary.ArrowAngle,
            secondary.ArrowAngle
        )) - 180f) < 5f;
        float actualGap = Vector2.Distance(
            primary.ArrowTip,
            secondary.ArrowTip
        );
        r[49] = Mathf.Abs(actualGap - arrowGap) < 0.01f;
        r[50] = actualGap > 0.01f;
        r[51] = primary.IsHighlighted && secondary.IsHighlighted;
        UnityEngine.Object.Destroy(clashView.gameObject);
    }

    private static void RunLayerLaneAndPreviewTests(
        bool[] r,
        QueryFixture fixture,
        DisplayFixture display
    )
    {
        QueryFixture clashFixture = CreateQueryFixture();
        clashFixture.slotA1.AssignResponse(
            clashFixture.allyA,
            clashFixture.allyAttackA,
            clashFixture.intent1,
            false
        );
        clashFixture.intent1.isResponded = true;
        display.controller.BindRuntimeState(clashFixture.runtime);
        RegisterDisplaySlots(display, clashFixture);
        display.controller.SetHoveredSlot("AllyA:1");
        BattleActionRelationUIView clash =
            display.controller.GetVisibleView(0);
        r[52] = clash != null &&
            clash.transform.parent == display.highlightRoot;
        display.controller.ClearHoveredSlot("AllyA:1");
        display.controller.SetRevealAllHeld(true);
        clash = display.controller.GetVisibleView(0);
        r[53] = clash != null &&
            clash.transform.parent == display.clashRoot;

        r[54] = display.dashedRoot.GetSiblingIndex() <
            display.clashRoot.GetSiblingIndex();
        r[55] = display.clashRoot.GetSiblingIndex() <
            display.highlightRoot.GetSiblingIndex();
        r[56] = display.highlightRoot.GetSiblingIndex() <
            display.previewRoot.GetSiblingIndex();
        r[57] = display.previewCurve.transform.parent == display.previewRoot;

        QueryFixture laneFixture = CreateQueryFixture();
        laneFixture.runtime.intentQueue.Add(new BattleEnemyIntent(
            "relation73_lane_2",
            laneFixture.enemy,
            laneFixture.enemyAttackB,
            laneFixture.allyA,
            2,
            2,
            2
        ));
        BattleActionRelationQueryService laneQuery =
            new BattleActionRelationQueryService(laneFixture.runtime);
        IReadOnlyList<BattleActionRelationDescriptor> first =
            laneQuery.GetAllCurrentRelations();
        Dictionary<string, int> firstLanes = CaptureLanes(first);
        r[58] = first.Count >= 3 &&
            first[0].LaneIndex != first[1].LaneIndex;
        IReadOnlyList<BattleActionRelationDescriptor> second =
            laneQuery.GetAllCurrentRelations();
        r[59] = LanesMatch(firstLanes, second);
        Dictionary<string, int> beforeTab = CaptureLanes(second);
        display.controller.SetRevealAllHeld(false);
        display.controller.SetRevealAllHeld(true);
        r[60] = LanesMatch(beforeTab, laneQuery.GetAllCurrentRelations());

        display.controller.BindRuntimeState(fixture.runtime);
        RegisterDisplaySlots(display, fixture);
        display.controller.SetRevealAllHeld(false);
        display.controller.SetHoveredSlot("AllyA:2");
        BattleActionRelationUIView normalView =
            display.controller.GetVisibleView(0);
        Vector2 normalControl = normalView != null
            ? normalView.PrimaryCurve.ControlPoint
            : Vector2.zero;
        display.controller.SetRevealAllHeld(true);
        BattleActionRelationUIView highlightedView = FindVisibleRelation(
            display.controller,
            normalView != null ? normalView.RelationID : string.Empty
        );
        r[61] = highlightedView != null &&
            Approximately(
                normalControl,
                highlightedView.PrimaryCurve.ControlPoint
            );

        display.previewCurve.ConfigureGeometryForTesting(
            new Vector2(10f, 3f),
            6f,
            2f,
            false
        );
        display.previewCurve.Render(
            Vector2.zero,
            new Vector2(50f, 100f),
            new Vector2(100f, 0f),
            Color.cyan,
            true,
            false
        );
        r[62] = display.previewCurve.IsVisible &&
            !display.previewCurve.IsUnderlayVisible;
        display.previewCurve.ConfigureGeometryForTesting(
            new Vector2(10f, 3f),
            6f,
            2f,
            true
        );
        display.previewCurve.Render(
            Vector2.zero,
            new Vector2(50f, 100f),
            new Vector2(100f, 0f),
            Color.cyan,
            true,
            false
        );
        r[63] = display.previewCurve.IsUnderlayVisible &&
            display.previewCurve.UnderlaySegmentSize.x >
                display.previewCurve.MainSegmentSize.x;

        display.controller.EndCardTargetingPreview();
        bool began = display.controller.BeginCardTargetingPreview("AllyA:1");
        display.controller.UpdateCardTargetingPointer(new Vector2(500f, 350f));
        r[64] = began && display.previewCurve.IsVisible &&
            display.previewCurve.IsDashed &&
            display.previewCurve.LastColor.b >
                display.previewCurve.LastColor.r;
        Vector2 expectedStart;
        BattleActionRelationLineController.TryConvertRectAnchorToLayerLocal(
            display.slots["AllyA:1"].RelationLineAnchor,
            display.lineLayer,
            display.canvas,
            null,
            out expectedStart
        );
        r[65] = Approximately(display.previewCurve.StartPoint, expectedStart);
        Vector2 expectedEnd;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            display.lineLayer,
            new Vector2(620f, 410f),
            null,
            out expectedEnd
        );
        display.controller.UpdateCardTargetingPointer(new Vector2(620f, 410f));
        r[66] = Approximately(display.previewCurve.ArrowTip, expectedEnd);

        GameObject cardObject = new GameObject(
            "Mode73CooldownCard",
            typeof(RectTransform),
            typeof(BattleCardUIView)
        );
        CharacterData owner = fixture.allyA;
        BattleCardState cooldownCard = new BattleCardState(
            owner,
            fixture.attackData,
            "relation73_cd"
        );
        cooldownCard.currentCooldown = 1;
        owner.battleCards.Add(cooldownCard);
        BattleCardUIView cardView = cardObject.GetComponent<BattleCardUIView>();
        BattleCardSelectionController selection =
            new BattleCardSelectionController();
        cardView.BindCard(
            owner,
            cooldownCard,
            new BattleCardUIPreviewData(),
            selection
        );
        display.controller.EndCardTargetingPreview();
        r[67] = !selection.ToggleCardSelection(cardView) &&
            !display.controller.PreviewActive;
        UnityEngine.Object.Destroy(cardObject);
        owner.battleCards.Remove(cooldownCard);

        display.controller.BeginCardTargetingPreview("AllyA:1");
        display.controller.UpdateCardTargetingPointer(new Vector2(300f, 220f));
        r[68] = display.controller.PreviewActive;
        display.controller.EndCardTargetingPreview();
        r[69] = !display.controller.PreviewActive;
        display.controller.BeginCardTargetingPreview("AllyA:1");
        display.controller.EndCardTargetingPreview();
        r[70] = !display.controller.PreviewActive;
        display.controller.BeginCardTargetingPreview("AllyA:1");
        Vector2 oldStart = display.previewCurve.StartPoint;
        bool rebuilt = display.controller.BeginCardTargetingPreview("AllyB:1");
        display.controller.UpdateCardTargetingPointer(new Vector2(400f, 250f));
        r[71] = rebuilt && !Approximately(
            oldStart,
            display.previewCurve.StartPoint
        );

        fixture.runtime.SetPhase("Executing");
        display.controller.RefreshRelations();
        r[72] = display.controller.VisibleRelationCount == 0 &&
            !display.controller.PreviewActive;
        fixture.runtime.SetPhase("Prepare");
        display.controller.RefreshRelations();
        display.controller.SetRevealAllHeld(true);
        r[73] = display.controller.VisibleRelationCount ==
            display.controller.CachedRelations.Count &&
            display.controller.VisibleRelationCount > 0;
    }

    private static QueryFixture CreateQueryFixture()
    {
        QueryFixture f = new QueryFixture();
        f.allyA = new CharacterData("relation73_A", 30, 10, 10);
        f.allyB = new CharacterData("relation73_B", 30, 9, 9);
        f.enemy = new CharacterData("relation73_Enemy", 50, 5, 5);
        f.attackData = CreateCardData("relation73_attack", CardType.Attack);
        f.defenseData = CreateCardData("relation73_defense", CardType.Defense);
        f.dodgeData = CreateCardData("relation73_dodge", CardType.Dodge);
        f.allyAttackA = new BattleCardState(
            f.allyA, f.attackData, "relation73_a_attack"
        );
        f.allyAttackB = new BattleCardState(
            f.allyB, f.attackData, "relation73_b_attack"
        );
        f.allyDefense = new BattleCardState(
            f.allyA, f.defenseData, "relation73_defense"
        );
        f.allyDodge = new BattleCardState(
            f.allyA, f.dodgeData, "relation73_dodge"
        );
        f.enemyAttackA = new BattleCardState(
            f.enemy, f.attackData, "relation73_enemy_attack_1"
        );
        f.enemyAttackB = new BattleCardState(
            f.enemy, f.attackData, "relation73_enemy_attack_2"
        );
        f.slotA1 = new BattleActionSlot(f.allyA, 1);
        f.slotA2 = new BattleActionSlot(f.allyA, 2);
        f.slotB1 = new BattleActionSlot(f.allyB, 1);
        f.slotB2 = new BattleActionSlot(f.allyB, 2);
        f.slotA1.AssignFreeAction(f.allyA, f.allyAttackA, f.enemy);
        f.slotA1.requestedTargetSlotIndex = 2;
        f.intent1 = new BattleEnemyIntent(
            "relation73_intent_1",
            f.enemy,
            f.enemyAttackA,
            f.allyA,
            2,
            1,
            1
        );
        f.runtime = new BattleRuntimeState();
        f.runtime.SetCharacters(f.allyA, f.allyB, f.enemy);
        f.runtime.SetActionSlots(new List<BattleActionSlot>
        {
            f.slotA1,
            f.slotA2,
            f.slotB1,
            f.slotB2
        });
        f.runtime.SetIntentQueue(new List<BattleEnemyIntent>
        {
            f.intent1
        });
        f.runtime.SetPhase("Prepare");
        return f;
    }

    private static CardTestData CreateCardData(string id, string type)
    {
        return new CardTestData
        {
            cardID = id,
            cardName = id,
            cardType = type,
            minPoint = 1,
            maxPoint = 1,
            cooldown = 0,
            damageFormula = "PointAsDamage"
        };
    }

    private static DisplayFixture CreateDisplayFixture(QueryFixture fixture)
    {
        DisplayFixture d = new DisplayFixture();
        d.root = new GameObject(
            "Mode73RelationRoot",
            typeof(RectTransform),
            typeof(Canvas)
        );
        d.canvas = d.root.GetComponent<Canvas>();
        d.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        d.texture = new Texture2D(1, 1);
        d.texture.SetPixel(0, 0, Color.white);
        d.texture.Apply();
        d.sprite = Sprite.Create(
            d.texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f)
        );
        d.lineLayer = CreateRect("LineLayer", d.root.transform);
        d.dashedRoot = CreateRect("NormalDashedRoot", d.lineLayer);
        d.clashRoot = CreateRect("NormalClashRoot", d.lineLayer);
        d.highlightRoot = CreateRect("HighlightRoot", d.lineLayer);
        d.previewRoot = CreateRect("PreviewRoot", d.lineLayer);

        BattleActionRelationUIView template = CreateStandaloneRelationView(d);
        template.transform.SetParent(d.lineLayer, false);
        template.gameObject.SetActive(false);
        d.previewCurve = CreateCurve("PreviewCurve", d.previewRoot, d.sprite);
        d.controller = d.root.AddComponent<
            BattleActionRelationLineController
        >();
        d.controller.ConfigureForTesting(
            d.lineLayer,
            d.dashedRoot,
            d.clashRoot,
            d.highlightRoot,
            d.previewRoot,
            template,
            d.previewCurve,
            d.canvas
        );
        d.slots = new Dictionary<string, BattleActionSlotUIView>();
        RegisterDisplaySlots(d, fixture);
        d.controller.BindRuntimeState(fixture.runtime);
        RegisterDisplaySlots(d, fixture);
        d.controller.RefreshRelations();
        return d;
    }

    private static void RegisterDisplaySlots(
        DisplayFixture d,
        QueryFixture fixture
    )
    {
        RegisterDisplaySlot(d, "AllyA:1", fixture.allyA, 0, false, new Vector2(-300f, -120f));
        RegisterDisplaySlot(d, "AllyA:2", fixture.allyA, 1, false, new Vector2(-150f, -120f));
        RegisterDisplaySlot(d, "AllyB:1", fixture.allyB, 0, false, new Vector2(100f, -120f));
        RegisterDisplaySlot(d, "AllyB:2", fixture.allyB, 1, false, new Vector2(250f, -120f));
        RegisterDisplaySlot(d, "Enemy:1", fixture.enemy, 0, true, new Vector2(-120f, 150f));
        RegisterDisplaySlot(d, "Enemy:2", fixture.enemy, 1, true, new Vector2(180f, 150f));
    }

    private static void RegisterDisplaySlot(
        DisplayFixture d,
        string id,
        CharacterData character,
        int zeroBasedIndex,
        bool enemy,
        Vector2 position
    )
    {
        BattleActionSlotUIView view;
        if (!d.slots.TryGetValue(id, out view) || view == null)
        {
            GameObject slotObject = new GameObject(
                id,
                typeof(RectTransform),
                typeof(Image),
                typeof(BattleActionSlotUIView)
            );
            slotObject.transform.SetParent(d.root.transform, false);
            view = slotObject.GetComponent<BattleActionSlotUIView>();
            d.slots[id] = view;
        }
        view.GetComponent<RectTransform>().anchoredPosition = position;
        view.BindInteraction(character, zeroBasedIndex, enemy, null, null);
        d.controller.RegisterSlotView(view);
    }

    private static BattleActionRelationUIView CreateStandaloneRelationView(
        DisplayFixture display
    )
    {
        GameObject viewObject = new GameObject(
            "RelationView",
            typeof(RectTransform),
            typeof(BattleActionRelationUIView)
        );
        BattleActionRelationUIView view =
            viewObject.GetComponent<BattleActionRelationUIView>();
        BattleBezierRelationLineUIView primary = CreateCurve(
            "PrimaryCurve",
            viewObject.transform,
            display.sprite
        );
        BattleBezierRelationLineUIView secondary = CreateCurve(
            "SecondaryCurve",
            viewObject.transform,
            display.sprite
        );
        view.ConfigureForTesting(primary, secondary);
        return view;
    }

    private static BattleBezierRelationLineUIView CreateCurve(
        string name,
        Transform parent,
        Sprite sprite
    )
    {
        GameObject curveObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(BattleBezierRelationLineUIView)
        );
        curveObject.transform.SetParent(parent, false);
        GameObject segmentObject = new GameObject(
            "SegmentTemplate",
            typeof(RectTransform),
            typeof(Image)
        );
        segmentObject.transform.SetParent(curveObject.transform, false);
        Image segment = segmentObject.GetComponent<Image>();
        segment.sprite = sprite;
        GameObject arrowObject = new GameObject(
            "Arrow",
            typeof(RectTransform),
            typeof(Image)
        );
        arrowObject.transform.SetParent(curveObject.transform, false);
        Image arrow = arrowObject.GetComponent<Image>();
        arrow.sprite = sprite;
        BattleBezierRelationLineUIView curve =
            curveObject.GetComponent<BattleBezierRelationLineUIView>();
        curve.ConfigureForTesting(
            segment,
            arrow,
            curveObject.GetComponent<CanvasGroup>()
        );
        return curve;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject value = new GameObject(name, typeof(RectTransform));
        value.transform.SetParent(parent, false);
        RectTransform rect = value.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static void DestroyDisplayFixture(DisplayFixture display)
    {
        if (display == null)
        {
            return;
        }
        if (display.root != null)
        {
            UnityEngine.Object.Destroy(display.root);
        }
        if (display.sprite != null)
        {
            UnityEngine.Object.Destroy(display.sprite);
        }
        if (display.texture != null)
        {
            UnityEngine.Object.Destroy(display.texture);
        }
    }

    private static BattleActionRelationDescriptor FindKind(
        IReadOnlyList<BattleActionRelationDescriptor> relations,
        BattleActionRelationKind kind
    )
    {
        for (int index = 0; index < relations.Count; index++)
        {
            if (relations[index].Kind == kind)
            {
                return relations[index];
            }
        }
        return null;
    }

    private static BattleActionRelationDescriptor FindSource(
        IReadOnlyList<BattleActionRelationDescriptor> relations,
        string sourceID
    )
    {
        for (int index = 0; index < relations.Count; index++)
        {
            if (relations[index].SourceSlotID == sourceID)
            {
                return relations[index];
            }
        }
        return null;
    }

    private static int CountKind(
        IReadOnlyList<BattleActionRelationDescriptor> relations,
        BattleActionRelationKind kind
    )
    {
        int count = 0;
        for (int index = 0; index < relations.Count; index++)
        {
            if (relations[index].Kind == kind)
            {
                count++;
            }
        }
        return count;
    }

    private static int CountRelationsForSlot(
        IReadOnlyList<BattleActionRelationDescriptor> relations,
        string slotID
    )
    {
        int count = 0;
        for (int index = 0; index < relations.Count; index++)
        {
            if (relations[index].InvolvesSlot(slotID))
            {
                count++;
            }
        }
        return count;
    }

    private static bool ContainsRelation(
        IReadOnlyList<BattleActionRelationDescriptor> relations,
        string relationID
    )
    {
        for (int index = 0; index < relations.Count; index++)
        {
            if (relations[index].RelationID == relationID)
            {
                return true;
            }
        }
        return false;
    }

    private static Dictionary<string, int> CaptureLanes(
        IReadOnlyList<BattleActionRelationDescriptor> relations
    )
    {
        Dictionary<string, int> result = new Dictionary<string, int>();
        for (int index = 0; index < relations.Count; index++)
        {
            result[relations[index].RelationID] = relations[index].LaneIndex;
        }
        return result;
    }

    private static bool LanesMatch(
        Dictionary<string, int> expected,
        IReadOnlyList<BattleActionRelationDescriptor> actual
    )
    {
        if (expected.Count != actual.Count)
        {
            return false;
        }
        for (int index = 0; index < actual.Count; index++)
        {
            int lane;
            if (!expected.TryGetValue(actual[index].RelationID, out lane) ||
                lane != actual[index].LaneIndex)
            {
                return false;
            }
        }
        return true;
    }

    private static BattleActionRelationUIView FindVisibleRelation(
        BattleActionRelationLineController controller,
        string relationID
    )
    {
        for (int index = 0; index < controller.VisibleRelationCount; index++)
        {
            BattleActionRelationUIView view = controller.GetVisibleView(index);
            if (view != null && view.RelationID == relationID)
            {
                return view;
            }
        }
        return null;
    }

    private static bool Approximately(Vector2 left, Vector2 right)
    {
        return Vector2.Distance(left, right) < 0.01f;
    }

    private static bool Approximately(Color left, Color right)
    {
        return Mathf.Abs(left.r - right.r) < 0.001f &&
            Mathf.Abs(left.g - right.g) < 0.001f &&
            Mathf.Abs(left.b - right.b) < 0.001f &&
            Mathf.Abs(left.a - right.a) < 0.001f;
    }
}

public static class BattleActionRelationInteractionMode75Tests
{
    private static readonly string[] Names =
    {
        "Segment列表包含已销毁Image时Clear不抛异常",
        "Underlay列表包含已销毁Image时Clear不抛异常",
        "HideUnusedSegments主动移除已销毁Segment",
        "HideUnusedSegments主动移除已销毁Underlay",
        "Arrow已销毁时Clear安全",
        "Arrow Underlay已销毁时Clear安全",
        "Clear连续调用两次安全",
        "OnDisable后Clear安全",
        "OnDestroy生命周期清理安全",
        "Controller ClearAll连续调用安全",
        "Preview内部Image先销毁时EndPreview安全",
        "Relation View先销毁时Controller清理安全",
        "BattleSimpleUIController销毁顺序不抛异常",
        "销毁阶段不会重新创建关系视图",
        "对象池复用时移除伪null引用",
        "攻击卡只允许EnemyActionSlot目标",
        "攻击卡进入敌方目标选择",
        "攻击卡选择目标时显示Preview",
        "防御卡允许Self目标",
        "防御卡允许EnemyActionSlot目标",
        "闪避卡允许Self目标",
        "闪避卡允许EnemyActionSlot目标",
        "防御确认Self后正式安排成功",
        "闪避确认Self后正式安排成功",
        "防御确认敌方槽位后保存正式TargetSlot",
        "闪避确认敌方槽位后保存正式TargetSlot",
        "Self目标不依赖SelfActionDropZone",
        "点击当前selectedSlot可以确认Self目标",
        "取消目标选择不会清除selectedSlot",
        "安排成功后selectedSlot仍保留",
        "非攻击卡安排后槽位视觉更新",
        "非攻击卡安排后卡牌状态更新",
        "Self防御不生成关系Descriptor",
        "Self闪避不生成关系Descriptor",
        "玩家单方面攻击生成PlayerUnilateralTarget",
        "玩家单方面防御生成PlayerUnilateralTarget",
        "玩家单方面闪避生成PlayerUnilateralTarget",
        "PlayerUnilateralTarget使用玩家侧语义",
        "PlayerUnilateralTarget箭头朝向敌方槽位",
        "EnemyUnilateralTarget继续使用敌方侧语义",
        "双方攻击互指生成AttackClash",
        "防御互指生成DefenseResponse",
        "闪避互指生成EvadeResponse",
        "DefenseResponse使用双方实线共享曲线",
        "EvadeResponse使用双方实线共享曲线",
        "DefenseResponse不分类为AttackClash",
        "EvadeResponse不分类为AttackClash",
        "单方面防御不生成DefenseResponse",
        "单方面闪避不生成EvadeResponse",
        "双方互动只根据具体行动槽互指成立",
        "取消目标后旧关系立即消失",
        "更换目标后旧关系被替换",
        "AttackClash替换双方单向关系",
        "DefenseResponse替换双方单向关系",
        "EvadeResponse替换双方单向关系",
        "最终关系没有重复单向线和互动线",
        "关系查询读取最终有效目标",
        "点击我方行动槽后设置selectedSlot",
        "PointerExit不会清除selectedSlot",
        "selectedSlot显示敌方指向它的关系",
        "selectedSlot显示它发出的攻击关系",
        "selectedSlot显示它发出的防御关系",
        "selectedSlot显示它发出的闪避关系",
        "selectedSlot显示AttackClash",
        "selectedSlot显示DefenseResponse",
        "selectedSlot显示EvadeResponse",
        "Self防御无关系但selectedSlot保留",
        "Self闪避无关系但selectedSlot保留",
        "选择另一我方槽位替换selectedSlot",
        "正式取消选择清除selectedSlot",
        "Preview结束后恢复selectedSlot关系",
        "Tab松开后恢复selectedSlot关系",
        "有selectedSlot时Hover不隐藏原关系",
        "Tab下Hover关系优先高亮",
        "Tab下无Hover时selectedSlot关系高亮",
        "进入执行阶段清除selectedSlot",
        "UI解绑和销毁时取消选择事件订阅",
        "Controller销毁后不再接收槽位选择事件",
        "Preview显示在PreviewRoot最高层",
        "Preview不会删除selectedSlot正式关系",
        "Tab显示所有当前有效关系",
        "Hover只影响高亮不改变关系集合",
        "无交互状态时隐藏关系",
        "取消攻击目标后selectedSlot保持",
        "取消防御目标后selectedSlot保持",
        "取消闪避目标后selectedSlot保持",
        "普通敌方虚线几何回归不变",
        "双方互动继续复用普通共享曲线",
        "Clash Arrow Gap只影响中间间距",
        "Highlight不改变曲线高度",
        "Tab显示两名友方指向不同敌人的全部箭头",
        "两名友方共享敌方终点时两支箭头均可见",
        "两名敌人共享友方终点时两支箭头均可见",
        "混合关系数量与全部箭头视觉状态正确",
        "Tab Hover Selected反复切换后箭头稳定",
        "对象池复用后关系View顺序与箭头稳定",
        "外部共享Curve模板被拒绝且不创建池View",
        "Preview与模板隔离且首个池View无未登记Segment",
        "首次Clash只显示两条独立半实线",
        "F8诊断不改变现场且对象池复用仍独立"
    };

    private sealed class Fixture
    {
        public BattleRuntimeState runtime;
        public CharacterData allyA;
        public CharacterData allyB;
        public CharacterData enemy;
        public CharacterData enemy2;
        public BattleActionSlot sourceSlot;
        public BattleEnemyIntent intent;
        public BattleEnemyIntent intent2;
        public BattleCardState attack;
        public BattleCardState attack2;
        public BattleCardState allyBAttack;
        public BattleCardState allyBAttack2;
        public BattleCardState defense;
        public BattleCardState dodge;
    }

    private sealed class AssignmentProbe
    {
        public bool selected;
        public bool cardSelected;
        public bool success;
        public bool selectionRetained;
        public bool slotVisualRetained;
        public BattleActionSlot slot;
        public BattleActionAssignmentResult result;
    }

    private sealed class Display
    {
        public GameObject root;
        public Texture2D texture;
        public Sprite sprite;
        public RectTransform lineLayer;
        public RectTransform dashedRoot;
        public RectTransform clashRoot;
        public RectTransform highlightRoot;
        public RectTransform previewRoot;
        public BattleActionRelationLineController controller;
        public BattleBezierRelationLineUIView preview;
        public BattleActionRelationUIView relationTemplate;
        public BattleBezierRelationLineUIView externalPrimaryCurve;
    }

    private sealed class RelationVisualSnapshot
    {
        public Vector2 arrowTip;
        public int siblingIndex;
        public string parentName;
    }

    public static bool Run()
    {
        bool[] results = new bool[Names.Length];
        RunDestroySafety(results);
        RunAssignments(results);
        RunRelations(results);
        RunSelectionAndPriority(results);
        RunRevealAllArrowStability(results);
        RunCurveOwnershipRegressionTests(results);
        bool previewLifecyclePassed = RunPreviewLifecycleRegressionTests();
        bool curveTransitionPassed = RunCurveTransitionRegressionTests();

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式75 测试" + (index + 1) + " " +
                Names[index] + "：" + results[index]
            );
            allPassed &= results[index];
        }
        allPassed &= previewLifecyclePassed;
        allPassed &= curveTransitionPassed;
        Debug.Log("模式75 " + Names.Length + "项聚合结果：" + allPassed);
        return allPassed;
    }

    private static bool RunCurveTransitionRegressionTests()
    {
        bool unilateralToClashPassed = false;
        bool rangeShrinkPassed = false;
        bool dashedToSolidPassed = false;
        bool formalOrderPassed = false;
        bool repeatedTransitionPassed = false;
        Display display = null;
        BattleActionRelationUIView view = null;
        BattleBezierRelationLineUIView rangeCurve = null;
        BattleBezierRelationLineUIView styleCurve = null;

        try
        {
            display = CreateDisplay(CreateFixture());
            view = CreateRelationView(display.root.transform, display.sprite);
            rangeCurve = CreateCurve(
                "Mode75RangeShrinkCurve",
                display.root.transform,
                display.sprite
            );
            styleCurve = CreateCurve(
                "Mode75StyleSwitchCurve",
                display.root.transform,
                display.sprite
            );

            Vector2 start = new Vector2(-220f, -80f);
            Vector2 control = new Vector2(0f, 170f);
            Vector2 end = new Vector2(220f, 100f);
            BattleActionRelationDescriptor unilateral =
                new BattleActionRelationDescriptor(
                    "Mode75TransitionUnilateral",
                    BattleActionRelationKind.PlayerUnilateralTarget,
                    "AllyA:1",
                    "Enemy:1",
                    "AllyA:1",
                    "Enemy:1",
                    BattleActionRelationSide.Player,
                    1,
                    1,
                    CardType.Attack,
                    CardType.Attack,
                    false
                );
            BattleActionRelationDescriptor clash =
                new BattleActionRelationDescriptor(
                    "Mode75TransitionClash",
                    BattleActionRelationKind.AttackClash,
                    "AllyA:1",
                    "Enemy:1",
                    "AllyA:1",
                    "Enemy:1",
                    BattleActionRelationSide.Player,
                    1,
                    1,
                    CardType.Attack,
                    CardType.Attack,
                    true
                );

            bool unilateralShown = view.ShowUnilateral(
                unilateral,
                start,
                end,
                Color.cyan,
                false,
                130f,
                0.12f,
                100f,
                320f,
                28f
            );
            int fullMainCount = view.PrimaryCurve.ActiveMainSegmentCount;
            int fullUnderlayCount =
                view.PrimaryCurve.ActiveUnderlaySegmentCount;
            bool clashShown = view.ShowClash(
                clash,
                start,
                end,
                Color.cyan,
                Color.red,
                false,
                130f,
                0.12f,
                100f,
                320f,
                28f,
                10f
            );
            unilateralToClashPassed = unilateralShown && clashShown &&
                fullMainCount > 0 && fullUnderlayCount == fullMainCount &&
                IsCurvePoolSynchronized(view.PrimaryCurve) &&
                IsCurvePoolSynchronized(view.SecondaryCurve) &&
                view.PrimaryCurve.CurrentLineStyle == "Solid" &&
                view.PrimaryCurve.PreviousLineStyle == "Dashed" &&
                view.PrimaryCurve.RangeEnd < 1f &&
                view.SecondaryCurve.RangeStart > 0f &&
                view.PrimaryCurve.HasVisibleMainArrow &&
                view.SecondaryCurve.HasVisibleMainArrow;

            rangeCurve.RenderRange(
                start,
                control,
                end,
                0f,
                1f,
                Color.cyan,
                true,
                false
            );
            int fullRangeCount = rangeCurve.ActiveSegmentCount;
            int fullRangePoolCount = rangeCurve.MainSegmentPoolCount;
            rangeCurve.RenderRange(
                start,
                control,
                end,
                0f,
                0.5f,
                Color.cyan,
                true,
                false
            );
            rangeShrinkPassed = fullRangeCount > 0 &&
                rangeCurve.ActiveSegmentCount < fullRangeCount &&
                rangeCurve.MainSegmentPoolCount == fullRangePoolCount &&
                IsCurvePoolSynchronized(rangeCurve) &&
                rangeCurve.CurrentRange == new Vector2(0f, 0.5f) &&
                rangeCurve.PreviousRange == new Vector2(0f, 1f);

            styleCurve.Render(
                start,
                control,
                end,
                Color.cyan,
                true,
                false
            );
            int dashedPoolCount = styleCurve.MainSegmentPoolCount;
            styleCurve.Render(
                start,
                control,
                end,
                Color.cyan,
                false,
                false
            );
            dashedToSolidPassed =
                styleCurve.PreviousLineStyle == "Dashed" &&
                styleCurve.CurrentLineStyle == "Solid" &&
                styleCurve.MainSegmentPoolCount >= dashedPoolCount &&
                IsCurvePoolSynchronized(styleCurve);

            view.ClearView();
            bool startedWithoutVisuals =
                view.PrimaryCurve.ActiveMainSegmentCount == 0 &&
                view.PrimaryCurve.ActiveUnderlaySegmentCount == 0 &&
                view.SecondaryCurve.ActiveMainSegmentCount == 0 &&
                view.SecondaryCurve.ActiveUnderlaySegmentCount == 0;
            bool formalUnilateralShown = view.ShowUnilateral(
                unilateral,
                start,
                end,
                Color.cyan,
                false,
                130f,
                0.12f,
                100f,
                320f,
                28f
            );
            bool formalClashShown = view.ShowClash(
                clash,
                start,
                end,
                Color.cyan,
                Color.red,
                false,
                130f,
                0.12f,
                100f,
                320f,
                28f,
                10f
            );
            formalOrderPassed = startedWithoutVisuals &&
                formalUnilateralShown && formalClashShown &&
                view.Kind == BattleActionRelationKind.AttackClash &&
                view.RelationID == clash.RelationID &&
                IsCurvePoolSynchronized(view.PrimaryCurve) &&
                IsCurvePoolSynchronized(view.SecondaryCurve) &&
                view.PrimaryCurve.CurrentLineStyle == "Solid" &&
                view.SecondaryCurve.CurrentLineStyle == "Solid";

            int primaryPoolBeforeRepeat =
                view.PrimaryCurve.MainSegmentPoolCount;
            int primaryUnderlayPoolBeforeRepeat =
                view.PrimaryCurve.UnderlaySegmentPoolCount;
            int secondaryPoolBeforeRepeat =
                view.SecondaryCurve.MainSegmentPoolCount;
            int secondaryUnderlayPoolBeforeRepeat =
                view.SecondaryCurve.UnderlaySegmentPoolCount;
            view.ClearView();
            bool repeatedUnilateralShown = view.ShowUnilateral(
                unilateral,
                start,
                end,
                Color.cyan,
                false,
                130f,
                0.12f,
                100f,
                320f,
                28f
            );
            bool repeatedClashShown = view.ShowClash(
                clash,
                start,
                end,
                Color.cyan,
                Color.red,
                false,
                130f,
                0.12f,
                100f,
                320f,
                28f,
                10f
            );
            repeatedTransitionPassed = repeatedUnilateralShown &&
                repeatedClashShown &&
                view.PrimaryCurve.MainSegmentPoolCount ==
                    primaryPoolBeforeRepeat &&
                view.PrimaryCurve.UnderlaySegmentPoolCount ==
                    primaryUnderlayPoolBeforeRepeat &&
                view.SecondaryCurve.MainSegmentPoolCount ==
                    secondaryPoolBeforeRepeat &&
                view.SecondaryCurve.UnderlaySegmentPoolCount ==
                    secondaryUnderlayPoolBeforeRepeat &&
                IsCurvePoolSynchronized(view.PrimaryCurve) &&
                IsCurvePoolSynchronized(view.SecondaryCurve) &&
                view.PrimaryCurve.HasVisibleMainArrow &&
                view.SecondaryCurve.HasVisibleMainArrow;
        }
        catch (Exception exception)
        {
            Debug.LogError("模式75 Curve转换回归异常：" + exception);
        }
        finally
        {
            DestroyDisplay(display);
        }

        Debug.Log(
            "模式75 Curve转换A 同一View单向转Clash：" +
            unilateralToClashPassed
        );
        Debug.Log(
            "模式75 Curve转换B 完整Range缩短后池尾关闭：" +
            rangeShrinkPassed
        );
        Debug.Log(
            "模式75 Curve转换C 虚线转实线无旧样式残留：" +
            dashedToSolidPassed
        );
        Debug.Log(
            "模式75 Curve转换D 无关系到单向再到Clash：" +
            formalOrderPassed
        );
        Debug.Log(
            "模式75 Curve转换E 反复转换池容量稳定：" +
            repeatedTransitionPassed
        );
        return unilateralToClashPassed && rangeShrinkPassed &&
            dashedToSolidPassed && formalOrderPassed &&
            repeatedTransitionPassed;
    }

    private static bool IsCurvePoolSynchronized(
        BattleBezierRelationLineUIView curve
    )
    {
        return curve != null && curve.ActiveSegmentCount >= 0 &&
            curve.ActiveMainSegmentCount == curve.ActiveSegmentCount &&
            curve.ActiveUnderlaySegmentCount == curve.ActiveSegmentCount &&
            curve.ActiveMainSegmentCount <= curve.MainSegmentPoolCount &&
            curve.ActiveUnderlaySegmentCount <=
                curve.UnderlaySegmentPoolCount;
    }

    private static bool RunPreviewLifecycleRegressionTests()
    {
        const string successName = "成功安排后Preview关闭且后续Hover不重开";
        const string clashName = "正式拼点不与Preview虚线重叠";
        const string cancelName = "取消安排后Preview保持关闭且重新选卡才开启";
        const string sourceName = "保留来源槽位但无卡牌时Targeting关闭";
        const string formalName = "关闭Preview不影响正式关系与Tab显示";
        bool successPassed = false;
        bool clashPassed = false;
        bool cancelPassed = false;
        bool sourcePassed = false;
        bool formalPassed = false;
        Display display = null;
        BattleActionSlotUIView sourceView = null;
        BattleActionSlotUIView targetView = null;
        BattleActionSlotUIView otherTargetView = null;
        BattleCardUIView cardView = null;

        try
        {
            Fixture fixture = CreateFixture();
            display = CreateDisplay(fixture);
            BattleCardSelectionController selection =
                new BattleCardSelectionController();
            BattleCardInteractionCoordinator coordinator =
                new BattleCardInteractionCoordinator(selection);
            sourceView = CreateSlotView(
                "Mode75PreviewLifecycleSource",
                fixture.allyA,
                0,
                false,
                null
            );
            targetView = CreateSlotView(
                "Mode75PreviewLifecycleIntentTarget",
                fixture.enemy,
                0,
                true,
                null
            );
            targetView.SetBoundEnemyIntent(fixture.intent);
            otherTargetView = CreateSlotView(
                "Mode75PreviewLifecycleOtherTarget",
                fixture.enemy,
                1,
                true,
                null
            );
            cardView = CreateCardView(
                "Mode75PreviewLifecycleCard",
                fixture.allyA,
                fixture.enemy,
                fixture.attack,
                selection
            );

            bool sourceSelected = coordinator.SelectSourceSlot(sourceView);
            bool cardSelected = selection.SelectCard(cardView);
            display.controller.SetSelectedSlot(sourceView);
            bool previewStarted = coordinator.IsCardTargetingActive &&
                display.controller.BeginCardTargetingPreview("AllyA:1");
            display.controller.SetHoveredSlot("Enemy:1");
            display.controller.UpdateCardTargetingPointer(
                new Vector2(300f, 180f)
            );
            bool previewWasVisible = previewStarted &&
                display.controller.PreviewActive &&
                display.preview.ArrowActiveSelf;

            BattleCardInteractionOutcome outcome =
                coordinator.ClickEnemySlot(fixture.runtime, targetView);
            BattleSimpleUIController.EndCardTargetingSession(
                selection,
                display.controller
            );
            display.controller.RefreshRelations();
            display.controller.SetSelectedSlot(sourceView);
            BattleActionRelationDescriptor clash = FindKind(
                display.controller.CachedRelations,
                BattleActionRelationKind.AttackClash
            );
            BattleActionRelationUIView clashView = clash != null
                ? FindVisibleRelationByID(
                    display.controller,
                    clash.RelationID
                )
                : null;

            display.controller.SetHoveredSlot("Enemy:2");
            successPassed = sourceSelected && cardSelected &&
                previewWasVisible && outcome != null && outcome.isSuccess &&
                !selection.HasSelection &&
                !coordinator.IsCardTargetingActive &&
                !display.controller.PreviewActive &&
                !display.preview.ArrowActiveSelf;
            clashPassed = clash != null && clashView != null &&
                HasVisiblePrimaryArrow(clashView) &&
                clashView.SecondaryCurve != null &&
                clashView.SecondaryCurve.ArrowActiveSelf &&
                !display.controller.PreviewActive &&
                !display.preview.ArrowActiveSelf;

            BattleActionAssignmentResult cancelResult;
            bool cancelled = BattleCardAssignmentRouter.TryCancelSelectedSlot(
                fixture.runtime,
                fixture.allyA,
                1,
                out cancelResult
            );
            BattleSimpleUIController.EndCardTargetingSession(
                selection,
                display.controller
            );
            display.controller.RefreshRelations();
            display.controller.SetHoveredSlot("Enemy:1");
            bool stayedClosedAfterCancel =
                !display.controller.PreviewActive &&
                !display.preview.ArrowActiveSelf;
            bool sourceRetained = object.ReferenceEquals(
                coordinator.SelectedActionSlotView,
                sourceView
            );
            bool noCardIsNotTargeting = sourceRetained &&
                !selection.HasSelection &&
                !coordinator.IsCardTargetingActive;
            bool cardReturnedToHand = BattleSimpleUIController
                .ShouldDisplayCardInHand(fixture.runtime, fixture.attack);
            bool reselected = selection.SelectCard(cardView);
            bool restartedOnlyAfterReselect = reselected &&
                coordinator.IsCardTargetingActive &&
                display.controller.BeginCardTargetingPreview("AllyA:1");
            cancelPassed = cancelled && cardReturnedToHand &&
                stayedClosedAfterCancel &&
                restartedOnlyAfterReselect;
            sourcePassed = noCardIsNotTargeting;

            BattleSimpleUIController.EndCardTargetingSession(
                selection,
                display.controller
            );
            bool selectedAgain = selection.SelectCard(cardView);
            BattleCardInteractionOutcome unilateralOutcome =
                coordinator.ClickEnemySlot(
                    fixture.runtime,
                    otherTargetView
                );
            BattleSimpleUIController.EndCardTargetingSession(
                selection,
                display.controller
            );
            display.controller.RefreshRelations();
            int formalRelationCount =
                display.controller.CachedRelations.Count;
            bool hasEnemyRelation = FindKind(
                display.controller.CachedRelations,
                BattleActionRelationKind.EnemyUnilateralTarget
            ) != null;
            bool hasPlayerRelation = FindKind(
                display.controller.CachedRelations,
                BattleActionRelationKind.PlayerUnilateralTarget
            ) != null;
            display.controller.SetRevealAllHeld(true);
            bool tabShowsAll = display.controller.VisibleRelationCount ==
                formalRelationCount;
            display.controller.SetRevealAllHeld(false);
            display.controller.SetSelectedSlot(sourceView);
            display.controller.SetHoveredSlot("Enemy:2");
            formalPassed = selectedAgain &&
                unilateralOutcome != null && unilateralOutcome.isSuccess &&
                formalRelationCount == 2 &&
                hasEnemyRelation && hasPlayerRelation && tabShowsAll &&
                display.controller.CachedRelations.Count ==
                    formalRelationCount &&
                !display.controller.PreviewActive;
        }
        catch (Exception exception)
        {
            Debug.LogError("模式75 Preview生命周期回归异常：" + exception);
        }
        finally
        {
            if (cardView != null)
            {
                UnityEngine.Object.DestroyImmediate(cardView.gameObject);
            }
            if (sourceView != null)
            {
                UnityEngine.Object.DestroyImmediate(sourceView.gameObject);
            }
            if (targetView != null)
            {
                UnityEngine.Object.DestroyImmediate(targetView.gameObject);
            }
            if (otherTargetView != null)
            {
                UnityEngine.Object.DestroyImmediate(otherTargetView.gameObject);
            }
            DestroyDisplay(display);
        }

        Debug.Log("模式75 Preview回归1 " + successName + "：" + successPassed);
        Debug.Log("模式75 Preview回归2 " + clashName + "：" + clashPassed);
        Debug.Log("模式75 Preview回归3 " + cancelName + "：" + cancelPassed);
        Debug.Log("模式75 Preview回归4 " + sourceName + "：" + sourcePassed);
        Debug.Log("模式75 Preview回归5 " + formalName + "：" + formalPassed);
        return successPassed && clashPassed && cancelPassed &&
            sourcePassed && formalPassed;
    }

    private static void RunDestroySafety(bool[] r)
    {
        GameObject root = null;
        try
        {
            root = new GameObject("Mode75DestroyRoot", typeof(RectTransform));
            Texture2D texture = new Texture2D(2, 2);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f)
            );
            BattleBezierRelationLineUIView curve =
                CreateCurve("DestroyCurve", root.transform, sprite);
            RenderTestCurve(curve);
            int segmentBefore = curve.SegmentPoolCount;
            Transform segment = root.transform.Find(
                "DestroyCurve/Segment_0"
            );
            if (segment != null)
            {
                UnityEngine.Object.DestroyImmediate(segment.gameObject);
            }
            r[0] = DoesNotThrow(curve.Clear);
            r[2] = curve.SegmentPoolCount < segmentBefore;

            RenderTestCurve(curve);
            int underlayBefore = curve.UnderlaySegmentPoolCount;
            Transform underlay = root.transform.Find(
                "DestroyCurve/UnderlaySegment_0"
            );
            if (underlay != null)
            {
                UnityEngine.Object.DestroyImmediate(underlay.gameObject);
            }
            r[1] = DoesNotThrow(curve.Clear);
            r[3] = curve.UnderlaySegmentPoolCount < underlayBefore;

            Transform arrow = root.transform.Find("DestroyCurve/Arrow");
            if (arrow != null)
            {
                UnityEngine.Object.DestroyImmediate(arrow.gameObject);
            }
            r[4] = DoesNotThrow(curve.Clear);

            RenderTestCurve(curve);
            Transform underlayArrow = root.transform.Find(
                "DestroyCurve/Arrow_Underlay"
            );
            if (underlayArrow != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    underlayArrow.gameObject
                );
            }
            r[5] = DoesNotThrow(curve.Clear);
            r[6] = DoesNotThrow(() =>
            {
                curve.Clear();
                curve.Clear();
            });
            curve.gameObject.SetActive(false);
            r[7] = DoesNotThrow(curve.Clear);

            GameObject lifecycle = curve.gameObject;
            r[8] = DoesNotThrow(() =>
                UnityEngine.Object.DestroyImmediate(lifecycle));

            Fixture fixture = CreateFixture();
            Display display = CreateDisplay(fixture);
            display.controller.SetRevealAllHeld(true);
            r[9] = DoesNotThrow(() =>
            {
                display.controller.ClearAll();
                display.controller.ClearAll();
            });

            Transform previewImage = display.preview.transform.Find(
                "SegmentTemplate"
            );
            if (previewImage != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    previewImage.gameObject
                );
            }
            r[10] = DoesNotThrow(
                display.controller.EndCardTargetingPreview
            );

            display.controller.SetRevealAllHeld(true);
            BattleActionRelationUIView active =
                display.controller.GetVisibleView(0);
            if (active != null)
            {
                UnityEngine.Object.DestroyImmediate(active.gameObject);
            }
            r[11] = DoesNotThrow(display.controller.ClearAll);

            GameObject simple = new GameObject(
                "Mode75SimpleController",
                typeof(BattleSimpleUIController)
            );
            r[12] = DoesNotThrow(() =>
                UnityEngine.Object.DestroyImmediate(simple));

            int poolBeforeDestroy = display.controller.RelationViewPoolCount;
            display.root.SetActive(false);
            r[13] = display.controller.RelationViewPoolCount <=
                poolBeforeDestroy;
            display.root.SetActive(true);
            display.controller.BindRuntimeState(fixture.runtime);
            RegisterSlots(display, fixture);
            display.controller.SetRevealAllHeld(true);
            r[14] = display.controller.RelationViewPoolCount >= 0 &&
                DoesNotThrow(display.controller.ClearAll);
            DestroyDisplay(display);
            UnityEngine.Object.DestroyImmediate(sprite);
            UnityEngine.Object.DestroyImmediate(texture);
        }
        catch (Exception exception)
        {
            Debug.LogError("模式75 销毁安全组异常：" + exception);
        }
        finally
        {
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }

    private static void RunAssignments(bool[] r)
    {
        Fixture attackFixture = CreateFixture();
        BattleActionAssignmentResult attackSelfResult;
        bool attackSelf = BattleCardAssignmentRouter.TryAssignToSelf(
            attackFixture.runtime,
            attackFixture.allyA,
            1,
            attackFixture.allyA,
            attackFixture.attack,
            attackFixture.allyA,
            out attackSelfResult
        );
        BattleActionAssignmentResult attackEnemyResult;
        bool attackEnemy = BattleCardAssignmentRouter.TryAssignToEnemySlot(
            attackFixture.runtime,
            attackFixture.allyA,
            1,
            attackFixture.allyA,
            attackFixture.attack,
            attackFixture.enemy,
            attackFixture.intent,
            1,
            out attackEnemyResult
        );
        r[15] = !attackSelf && attackEnemy;

        Fixture selectionFixture = CreateFixture();
        AssignmentProbe pendingAttack = ProbePendingTarget(
            selectionFixture,
            selectionFixture.attack
        );
        r[16] = pendingAttack.selected && pendingAttack.cardSelected &&
            pendingAttack.slot.IsEmpty();

        Display previewDisplay = CreateDisplay(selectionFixture);
        previewDisplay.controller.SetSelectedSlot("AllyA:1");
        r[17] = previewDisplay.controller.BeginCardTargetingPreview(
            "AllyA:1"
        ) && previewDisplay.controller.PreviewActive;
        DestroyDisplay(previewDisplay);

        AssignmentProbe defenseSelf = ProbeSelf(
            CreateFixture(),
            CardType.Defense
        );
        AssignmentProbe defenseEnemy = ProbeEnemy(
            CreateFixture(),
            CardType.Defense,
            2
        );
        AssignmentProbe dodgeSelf = ProbeSelf(
            CreateFixture(),
            CardType.Dodge
        );
        AssignmentProbe dodgeEnemy = ProbeEnemy(
            CreateFixture(),
            CardType.Dodge,
            2
        );
        r[18] = defenseSelf.success;
        r[19] = defenseEnemy.success;
        r[20] = dodgeSelf.success;
        r[21] = dodgeEnemy.success;
        r[22] = defenseSelf.success &&
            defenseSelf.slot.placementType == BattleActionPlacementType.Self;
        r[23] = dodgeSelf.success &&
            dodgeSelf.slot.placementType == BattleActionPlacementType.Self;
        r[24] = defenseEnemy.success &&
            defenseEnemy.slot.requestedTargetSlotIndex == 2;
        r[25] = dodgeEnemy.success &&
            dodgeEnemy.slot.requestedTargetSlotIndex == 2;
        r[26] = defenseSelf.success && dodgeSelf.success;
        r[27] = defenseSelf.result != null &&
            defenseSelf.result.placementType == BattleActionPlacementType.Self;
        r[28] = pendingAttack.selectionRetained;
        r[29] = defenseSelf.selectionRetained &&
            dodgeEnemy.selectionRetained;
        r[30] = defenseSelf.slotVisualRetained &&
            dodgeSelf.slotVisualRetained;
        r[31] = defenseSelf.slot.cardState != null &&
            dodgeEnemy.slot.cardState != null;
    }

    private static void RunRelations(bool[] r)
    {
        IReadOnlyList<BattleActionRelationDescriptor> selfDefense =
            GetRelations(CardType.Defense, true, false, 1);
        IReadOnlyList<BattleActionRelationDescriptor> selfDodge =
            GetRelations(CardType.Dodge, true, false, 1);
        IReadOnlyList<BattleActionRelationDescriptor> attackOneWay =
            GetRelations(CardType.Attack, false, false, 2);
        IReadOnlyList<BattleActionRelationDescriptor> defenseOneWay =
            GetRelations(CardType.Defense, false, false, 2);
        IReadOnlyList<BattleActionRelationDescriptor> dodgeOneWay =
            GetRelations(CardType.Dodge, false, false, 2);
        BattleActionRelationDescriptor playerAttack = FindKind(
            attackOneWay,
            BattleActionRelationKind.PlayerUnilateralTarget
        );
        BattleActionRelationDescriptor playerDefense = FindKind(
            defenseOneWay,
            BattleActionRelationKind.PlayerUnilateralTarget
        );
        BattleActionRelationDescriptor playerDodge = FindKind(
            dodgeOneWay,
            BattleActionRelationKind.PlayerUnilateralTarget
        );
        BattleActionRelationDescriptor enemyOneWay = FindKind(
            attackOneWay,
            BattleActionRelationKind.EnemyUnilateralTarget
        );
        BattleActionRelationDescriptor clash = FindKind(
            GetRelations(CardType.Attack, false, true, 1),
            BattleActionRelationKind.AttackClash
        );
        BattleActionRelationDescriptor defenseResponse = FindKind(
            GetRelations(CardType.Defense, false, true, 1),
            BattleActionRelationKind.DefenseResponse
        );
        BattleActionRelationDescriptor evadeResponse = FindKind(
            GetRelations(CardType.Dodge, false, true, 1),
            BattleActionRelationKind.EvadeResponse
        );

        r[32] = FindPlayerRelation(selfDefense) == null;
        r[33] = FindPlayerRelation(selfDodge) == null;
        r[34] = playerAttack != null;
        r[35] = playerDefense != null;
        r[36] = playerDodge != null;
        r[37] = playerAttack != null &&
            playerAttack.SourceSide == BattleActionRelationSide.Player;
        r[38] = playerAttack != null &&
            playerAttack.TargetSlotID == "Enemy:2";
        r[39] = enemyOneWay != null &&
            enemyOneWay.SourceSide == BattleActionRelationSide.Enemy;
        r[40] = clash != null;
        r[41] = defenseResponse != null;
        r[42] = evadeResponse != null;
        r[43] = defenseResponse != null &&
            defenseResponse.UsesMutualSolidVisual;
        r[44] = evadeResponse != null &&
            evadeResponse.UsesMutualSolidVisual;
        r[45] = defenseResponse != null &&
            defenseResponse.Kind != BattleActionRelationKind.AttackClash;
        r[46] = evadeResponse != null &&
            evadeResponse.Kind != BattleActionRelationKind.AttackClash;
        r[47] = FindKind(
            defenseOneWay,
            BattleActionRelationKind.DefenseResponse
        ) == null;
        r[48] = FindKind(
            dodgeOneWay,
            BattleActionRelationKind.EvadeResponse
        ) == null;
        r[49] = playerAttack != null && enemyOneWay != null &&
            playerAttack.TargetSlotID != enemyOneWay.SourceSlotID;

        Fixture cancelFixture = CreateFixture();
        BattleActionAssignmentResult assignment;
        BattleActionSlotManager.TryAssignToEnemy(
            cancelFixture.runtime,
            cancelFixture.allyA,
            1,
            cancelFixture.attack,
            cancelFixture.enemy,
            2,
            out assignment
        );
        BattleActionRelationQueryService cancelQuery =
            new BattleActionRelationQueryService(cancelFixture.runtime);
        bool hadOld = ContainsRelation(
            cancelQuery.GetAllCurrentRelations(),
            "AllyA:1->Enemy:2"
        );
        BattleActionSlotManager.TryCancelAssignment(
            cancelFixture.runtime,
            cancelFixture.allyA,
            1,
            out assignment
        );
        r[50] = hadOld && !ContainsRelation(
            cancelQuery.GetAllCurrentRelations(),
            "AllyA:1->Enemy:2"
        );
        BattleActionSlotManager.TryAssignToEnemy(
            cancelFixture.runtime,
            cancelFixture.allyA,
            1,
            cancelFixture.attack,
            cancelFixture.enemy,
            1,
            out assignment
        );
        r[51] = ContainsRelation(
            cancelQuery.GetAllCurrentRelations(),
            "AllyA:1->Enemy:1"
        ) && !ContainsRelation(
            cancelQuery.GetAllCurrentRelations(),
            "AllyA:1->Enemy:2"
        );
        r[52] = clash != null &&
            CountRelations(GetRelations(
                CardType.Attack, false, true, 1
            )) == 1;
        r[53] = defenseResponse != null &&
            CountRelations(GetRelations(
                CardType.Defense, false, true, 1
            )) == 1;
        r[54] = evadeResponse != null &&
            CountRelations(GetRelations(
                CardType.Dodge, false, true, 1
            )) == 1;
        r[55] = HasUniqueIDs(attackOneWay) &&
            HasUniqueIDs(defenseOneWay) && HasUniqueIDs(dodgeOneWay);
        r[56] = playerAttack != null &&
            playerAttack.TargetSlotID == "Enemy:2" &&
            enemyOneWay != null &&
            enemyOneWay.TargetSlotID == "AllyA:1";
    }

    private static void RunSelectionAndPriority(bool[] r)
    {
        Fixture fixture = CreateFixture();
        BattleActionAssignmentResult assignment;
        BattleActionSlotManager.TryAssignToEnemy(
            fixture.runtime,
            fixture.allyA,
            1,
            fixture.attack,
            fixture.enemy,
            2,
            out assignment
        );
        Display display = CreateDisplay(fixture);
        BattleCardSelectionController cardSelection =
            new BattleCardSelectionController();
        BattleCardInteractionCoordinator coordinator =
            new BattleCardInteractionCoordinator(cardSelection);
        BattleActionSlotUIView sourceView = CreateSlotView(
            "Mode75SelectedSource",
            fixture.allyA,
            0,
            false,
            null
        );
        Action<BattleActionSlotUIView> selectionHandler = slot =>
        {
            if (slot == null) display.controller.ClearSelectedSlot();
            else display.controller.SetSelectedSlot(slot);
        };
        coordinator.SourceSlotSelectionChanged += selectionHandler;
        bool selected = coordinator.SelectSourceSlot(sourceView);
        int selectedVisible = display.controller.VisibleRelationCount;
        r[57] = selected && display.controller.SelectedSlotID == "AllyA:1";
        display.controller.SetHoveredSlot("AllyA:1");
        display.controller.ClearHoveredSlot("AllyA:1");
        r[58] = display.controller.SelectedSlotID == "AllyA:1";
        r[59] = selectedVisible > 0;
        r[60] = FindKind(
            display.controller.CachedRelations,
            BattleActionRelationKind.PlayerUnilateralTarget
        ) != null && selectedVisible > 0;
        r[61] = HasSelectedRelation(
            CardType.Defense,
            BattleActionRelationKind.PlayerUnilateralTarget
        );
        r[62] = HasSelectedRelation(
            CardType.Dodge,
            BattleActionRelationKind.PlayerUnilateralTarget
        );
        r[63] = HasSelectedRelation(
            CardType.Attack,
            BattleActionRelationKind.AttackClash
        );
        r[64] = HasSelectedRelation(
            CardType.Defense,
            BattleActionRelationKind.DefenseResponse
        );
        r[65] = HasSelectedRelation(
            CardType.Dodge,
            BattleActionRelationKind.EvadeResponse
        );
        r[66] = HasSelectedSelfWithoutRelation(CardType.Defense);
        r[67] = HasSelectedSelfWithoutRelation(CardType.Dodge);

        BattleActionSlotUIView secondView = CreateSlotView(
            "Mode75SelectedSecond",
            fixture.allyA,
            1,
            false,
            null
        );
        coordinator.SelectSourceSlot(secondView);
        r[68] = display.controller.SelectedSlotID == "AllyA:2";
        coordinator.ClearSourceSlot();
        r[69] = string.IsNullOrEmpty(display.controller.SelectedSlotID);

        coordinator.SelectSourceSlot(sourceView);
        int beforePreview = display.controller.VisibleRelationCount;
        bool previewStarted = display.controller.BeginCardTargetingPreview(
            "AllyA:1"
        );
        display.controller.EndCardTargetingPreview();
        r[70] = previewStarted &&
            display.controller.VisibleRelationCount == beforePreview;
        display.controller.SetRevealAllHeld(true);
        display.controller.SetRevealAllHeld(false);
        r[71] = display.controller.VisibleRelationCount == beforePreview;
        display.controller.SetHoveredSlot("Enemy:1");
        r[72] = display.controller.VisibleRelationCount >= beforePreview;
        display.controller.SetRevealAllHeld(true);
        BattleActionRelationUIView hoverView = FindHighlighted(display.controller);
        r[73] = hoverView != null &&
            hoverView.RelationID.Contains("Enemy:1");
        display.controller.ClearHoveredSlot("Enemy:1");
        r[74] = FindHighlighted(display.controller) != null;
        fixture.runtime.SetPhase("PlanReady");
        display.controller.RefreshRelations();
        r[75] = string.IsNullOrEmpty(display.controller.SelectedSlotID);

        coordinator.SourceSlotSelectionChanged -= selectionHandler;
        string selectedBeforeUnbound = display.controller.SelectedSlotID;
        coordinator.SelectSourceSlot(sourceView);
        r[76] = display.controller.SelectedSlotID == selectedBeforeUnbound;
        display.root.SetActive(false);
        r[77] = display.controller.IsShuttingDown &&
            string.IsNullOrEmpty(display.controller.SelectedSlotID);

        fixture.runtime.SetPhase("Prepare");
        display.root.SetActive(true);
        display.controller.BindRuntimeState(fixture.runtime);
        RegisterSlots(display, fixture);
        display.controller.RefreshRelations();
        display.controller.SetSelectedSlot("AllyA:1");
        r[78] = display.previewRoot.GetSiblingIndex() >
            display.highlightRoot.GetSiblingIndex();
        int cachedBeforePreview = display.controller.CachedRelations.Count;
        int formalBeforePreview = display.controller.VisibleRelationCount;
        display.controller.BeginCardTargetingPreview("AllyA:1");
        r[79] = display.controller.PreviewActive &&
            display.controller.CachedRelations.Count == cachedBeforePreview &&
            display.controller.VisibleRelationCount == formalBeforePreview;
        display.controller.SetRevealAllHeld(true);
        r[80] = display.controller.VisibleRelationCount ==
            display.controller.CachedRelations.Count;
        int cachedBeforeHover = display.controller.CachedRelations.Count;
        display.controller.SetHoveredSlot("Enemy:1");
        r[81] = display.controller.CachedRelations.Count == cachedBeforeHover;
        display.controller.EndCardTargetingPreview();
        display.controller.SetRevealAllHeld(false);
        display.controller.ClearHoveredSlot("Enemy:1");
        display.controller.ClearSelectedSlot();
        r[82] = display.controller.VisibleRelationCount == 0;
        display.controller.SetSelectedSlot("AllyA:1");
        r[83] = CancelTargetKeepsSelection(display.controller);
        r[84] = CancelTargetKeepsSelection(display.controller);
        r[85] = CancelTargetKeepsSelection(display.controller);

        Vector2 start = new Vector2(-200f, -40f);
        Vector2 end = new Vector2(210f, 30f);
        Vector2 control = BattleBezierRelationLineUIView.ResolveControlPoint(
            start, end, 130f, 0.12f, 100f, 320f, 0f
        );
        display.preview.Render(start, control, end, Color.red, true, false);
        r[86] = display.preview.IsDashed &&
            Approximately(display.preview.ControlPoint, control);
        BattleActionRelationUIView mutualView = CreateRelationView(
            display.root.transform,
            display.sprite
        );
        BattleActionRelationDescriptor descriptor =
            new BattleActionRelationDescriptor(
                "Mode75MutualGeometry",
                BattleActionRelationKind.AttackClash,
                "AllyA:1", "Enemy:1", "AllyA:1", "Enemy:1",
                BattleActionRelationSide.Player, 1, 1,
                CardType.Attack, CardType.Attack, true
            );
        mutualView.ShowClash(
            descriptor, start, end, Color.cyan, Color.red, false,
            130f, 0.12f, 100f, 320f, 28f, 10f
        );
        r[87] = Approximately(
            mutualView.PrimaryCurve.SourceCurveControlPoint,
            mutualView.SecondaryCurve.SourceCurveControlPoint
        );
        Vector2 controlBeforeGap =
            mutualView.PrimaryCurve.SourceCurveControlPoint;
        mutualView.ShowClash(
            descriptor, start, end, Color.cyan, Color.red, false,
            130f, 0.12f, 100f, 320f, 28f, 30f
        );
        r[88] = Approximately(
            controlBeforeGap,
            mutualView.PrimaryCurve.SourceCurveControlPoint
        ) && Mathf.Abs(Vector2.Distance(
            mutualView.PrimaryCurve.ArrowTip,
            mutualView.SecondaryCurve.ArrowTip
        ) - 30f) < 0.01f;
        Vector2 controlBeforeHighlight =
            mutualView.PrimaryCurve.SourceCurveControlPoint;
        mutualView.ShowClash(
            descriptor, start, end, Color.cyan, Color.red, true,
            130f, 0.12f, 100f, 320f, 28f, 30f
        );
        r[89] = Approximately(
            controlBeforeHighlight,
            mutualView.PrimaryCurve.SourceCurveControlPoint
        );

        UnityEngine.Object.DestroyImmediate(sourceView.gameObject);
        UnityEngine.Object.DestroyImmediate(secondView.gameObject);
        UnityEngine.Object.DestroyImmediate(mutualView.gameObject);
        DestroyDisplay(display);
    }

    private static void RunRevealAllArrowStability(bool[] r)
    {
        Fixture distinct = CreateMultiRelationFixture();
        bool distinctAAssigned = AssignPlayerAttack(
            distinct,
            distinct.allyA,
            distinct.attack,
            distinct.enemy
        );
        bool distinctBAssigned = AssignPlayerAttack(
            distinct,
            distinct.allyB,
            distinct.allyBAttack,
            distinct.enemy2
        );
        Display distinctDisplay = CreateDisplay(distinct);
        RevealAllAndForceLayout(distinctDisplay);
        BattleActionRelationUIView distinctA = FindVisibleRelationByID(
            distinctDisplay.controller,
            "AllyA:1->Enemy:1"
        );
        BattleActionRelationUIView distinctB = FindVisibleRelationByID(
            distinctDisplay.controller,
            "AllyB:1->Enemy2:1"
        );
        r[90] = distinctAAssigned && distinctBAssigned &&
            distinctA != null && distinctB != null &&
            HasVisiblePrimaryArrow(distinctA) &&
            HasVisiblePrimaryArrow(distinctB) &&
            distinctDisplay.controller.CachedRelations.Count == 4 &&
            HasUniqueIDs(distinctDisplay.controller.CachedRelations) &&
            distinctDisplay.controller.VisibleRelationCount ==
                distinctDisplay.controller.CachedRelations.Count &&
            AllVisibleViewsAreIndependent(distinctDisplay.controller);
        if (!r[90])
        {
            LogRevealAllDiagnostic(
                "测试91 distinctAAssigned=" + distinctAAssigned +
                "，distinctBAssigned=" + distinctBAssigned,
                distinctDisplay,
                4
            );
        }
        DestroyDisplay(distinctDisplay);

        Fixture sharedPlayerTarget = CreateMultiRelationFixture();
        bool sharedPlayerAAssigned = AssignPlayerAttack(
            sharedPlayerTarget,
            sharedPlayerTarget.allyA,
            sharedPlayerTarget.attack,
            sharedPlayerTarget.enemy
        );
        bool sharedPlayerBAssigned = AssignPlayerAttack(
            sharedPlayerTarget,
            sharedPlayerTarget.allyB,
            sharedPlayerTarget.allyBAttack,
            sharedPlayerTarget.enemy
        );
        Display sharedPlayerDisplay = CreateDisplay(sharedPlayerTarget);
        RevealAllAndForceLayout(sharedPlayerDisplay);
        BattleActionRelationUIView sharedPlayerA = FindVisibleRelationByID(
            sharedPlayerDisplay.controller,
            "AllyA:1->Enemy:1"
        );
        BattleActionRelationUIView sharedPlayerB = FindVisibleRelationByID(
            sharedPlayerDisplay.controller,
            "AllyB:1->Enemy:1"
        );
        r[91] = sharedPlayerAAssigned && sharedPlayerBAssigned &&
            sharedPlayerDisplay.controller.CachedRelations.Count == 4 &&
            HasUniqueIDs(sharedPlayerDisplay.controller.CachedRelations) &&
            HaveSeparatedVisibleArrows(sharedPlayerA, sharedPlayerB) &&
            AllVisibleViewsAreIndependent(sharedPlayerDisplay.controller);
        if (!r[91])
        {
            LogRevealAllDiagnostic(
                "测试92 assignedA=" + sharedPlayerAAssigned +
                "，assignedB=" + sharedPlayerBAssigned,
                sharedPlayerDisplay,
                4
            );
        }
        DestroyDisplay(sharedPlayerDisplay);

        Fixture sharedEnemyTarget = CreateMultiRelationFixture();
        SetIntentTarget(
            sharedEnemyTarget.intent,
            sharedEnemyTarget.allyA,
            1
        );
        SetIntentTarget(
            sharedEnemyTarget.intent2,
            sharedEnemyTarget.allyA,
            1
        );
        Display sharedEnemyDisplay = CreateDisplay(sharedEnemyTarget);
        RevealAllAndForceLayout(sharedEnemyDisplay);
        BattleActionRelationUIView sharedEnemyA = FindVisibleRelationByID(
            sharedEnemyDisplay.controller,
            "Enemy:1->AllyA:1"
        );
        BattleActionRelationUIView sharedEnemyB = FindVisibleRelationByID(
            sharedEnemyDisplay.controller,
            "Enemy2:1->AllyA:1"
        );
        r[92] = sharedEnemyDisplay.controller.CachedRelations.Count == 2 &&
            HasUniqueIDs(sharedEnemyDisplay.controller.CachedRelations) &&
            HaveSeparatedVisibleArrows(sharedEnemyA, sharedEnemyB) &&
            AllVisibleViewsAreIndependent(sharedEnemyDisplay.controller);
        if (!r[92])
        {
            LogRevealAllDiagnostic(
                "测试93 两个敌人共享AllyA:1",
                sharedEnemyDisplay,
                2
            );
        }
        DestroyDisplay(sharedEnemyDisplay);

        Fixture mixed = CreateMultiRelationFixture();
        SetIntentTarget(mixed.intent, mixed.allyA, 1);
        BattleActionAssignmentResult mixedResponseResult;
        bool mixedResponseAssigned =
            BattleActionSlotManager.TryAssignToEnemyIntent(
                mixed.runtime,
                mixed.allyA,
                1,
                mixed.attack,
                mixed.intent,
                out mixedResponseResult
            );
        bool mixedUnilateralAssigned = AssignPlayerAttack(
            mixed,
            mixed.allyB,
            mixed.allyBAttack,
            mixed.enemy2
        );
        Display mixedDisplay = CreateDisplay(mixed);
        RevealAllAndForceLayout(mixedDisplay);
        r[93] = mixedResponseAssigned && mixedUnilateralAssigned &&
            mixedDisplay.controller.CachedRelations.Count == 3 &&
            mixedDisplay.controller.VisibleRelationCount == 3 &&
            AllVisibleCurvesHaveArrows(mixedDisplay.controller) &&
            AllVisibleViewsAreIndependent(mixedDisplay.controller) &&
            ValidateClashCurveIndependence(mixedDisplay);
        if (!r[93])
        {
            LogRevealAllDiagnostic(
                "测试94 responseAssigned=" + mixedResponseAssigned +
                "，unilateralAssigned=" + mixedUnilateralAssigned,
                mixedDisplay,
                3
            );
        }
        DestroyDisplay(mixedDisplay);

        Fixture repeated = CreateMultiRelationFixture();
        AssignPlayerAttack(
            repeated,
            repeated.allyA,
            repeated.attack,
            repeated.enemy
        );
        AssignPlayerAttack(
            repeated,
            repeated.allyB,
            repeated.allyBAttack,
            repeated.enemy
        );
        Display repeatedDisplay = CreateDisplay(repeated);
        RevealAllAndForceLayout(repeatedDisplay);
        Dictionary<string, RelationVisualSnapshot> repeatedExpected =
            CaptureVisibleRelationVisuals(repeatedDisplay.controller);
        bool repeatedStable = repeatedExpected.Count ==
            repeatedDisplay.controller.CachedRelations.Count;
        for (int cycle = 0; cycle < 3; cycle++)
        {
            repeatedDisplay.controller.SetRevealAllHeld(false);
            Canvas.ForceUpdateCanvases();
            repeatedStable &= repeatedDisplay.controller.VisibleRelationCount == 0;
            repeatedDisplay.controller.SetHoveredSlot("AllyA:1");
            Canvas.ForceUpdateCanvases();
            repeatedStable &= VisibleRelationIDsMatchSlots(
                repeatedDisplay.controller,
                "AllyA:1",
                null
            );
            repeatedDisplay.controller.SetSelectedSlot("AllyB:1");
            Canvas.ForceUpdateCanvases();
            repeatedStable &= VisibleRelationIDsMatchSlots(
                repeatedDisplay.controller,
                "AllyA:1",
                "AllyB:1"
            );
            repeatedDisplay.controller.ClearHoveredSlot("AllyA:1");
            Canvas.ForceUpdateCanvases();
            repeatedStable &= VisibleRelationIDsMatchSlots(
                repeatedDisplay.controller,
                "AllyB:1",
                null
            );
            repeatedDisplay.controller.SetRevealAllHeld(true);
            Canvas.ForceUpdateCanvases();
            repeatedStable &= VisibleRelationIDsMatchAll(
                repeatedDisplay.controller
            );
            repeatedDisplay.controller.ClearSelectedSlot();
            Canvas.ForceUpdateCanvases();
            repeatedStable &= VisibleVisualsMatch(
                repeatedDisplay.controller,
                repeatedExpected
            );
        }
        r[94] = repeatedStable;
        if (!r[94])
        {
            LogRevealAllDiagnostic(
                "测试95 Tab/Hover/Selected切换后视觉集合不稳定",
                repeatedDisplay,
                repeatedExpected.Count
            );
        }

        repeatedDisplay.controller.SetRevealAllHeld(false);
        repeatedDisplay.controller.ClearSelectedSlot();
        bool cancelledInitial = CancelAllPlayerAssignments(repeated);
        bool sceneAReady = SetupRelationSceneA(repeated);
        RevealAllAndForceLayout(repeatedDisplay);
        Dictionary<string, RelationVisualSnapshot> sceneAExpected =
            CaptureVisibleRelationVisuals(repeatedDisplay.controller);
        bool sceneAValid = cancelledInitial && sceneAReady &&
            ValidateSceneA(repeatedDisplay.controller);

        bool sceneBCancelled = CancelAllPlayerAssignments(repeated);
        bool sceneBReady = SetupRelationSceneB(repeated);
        RevealAllAndForceLayout(repeatedDisplay);
        bool sceneBValid = sceneBCancelled && sceneBReady &&
            ValidateSceneB(repeatedDisplay.controller);

        bool sceneCCancelled = CancelAllPlayerAssignments(repeated);
        bool sceneCReady = SetupRelationSceneC(repeated);
        RevealAllAndForceLayout(repeatedDisplay);
        bool sceneCValid = sceneCCancelled && sceneCReady &&
            ValidateSceneC(repeatedDisplay.controller);

        bool sceneAReturnCancelled = CancelAllPlayerAssignments(repeated);
        bool sceneAReturnReady = SetupRelationSceneA(repeated);
        RevealAllAndForceLayout(repeatedDisplay);
        bool sceneAReturnStable = sceneAReturnCancelled &&
            sceneAReturnReady && ValidateSceneA(repeatedDisplay.controller) &&
            VisibleVisualsMatch(
                repeatedDisplay.controller,
                sceneAExpected
            );
        r[95] = sceneAValid && sceneBValid && sceneCValid &&
            sceneAReturnStable;
        if (!r[95])
        {
            LogRevealAllDiagnostic(
                "测试96 sceneA=" + sceneAValid +
                "，sceneB=" + sceneBValid +
                "，sceneC=" + sceneCValid +
                "，sceneAReturn=" + sceneAReturnStable,
                repeatedDisplay,
                5
            );
        }
        DestroyDisplay(repeatedDisplay);
    }

    private static void RunCurveOwnershipRegressionTests(bool[] r)
    {
        const int firstResultIndex = 96;
        r[firstResultIndex] = false;
        r[firstResultIndex + 1] = false;
        r[firstResultIndex + 2] = false;
        r[firstResultIndex + 3] = false;
        Display invalidDisplay = null;
        Display display = null;
        try
        {
            // Curve所有权回归需要两名独立敌人与两个独立Intent，不能使用单Intent基础夹具。
            Fixture fixture = CreateMultiRelationFixture();
            string fixtureFailureReason;
            if (!TryValidateCurveOwnershipFixture(
                    fixture,
                    out fixtureFailureReason
                ))
            {
                Debug.LogError(
                    "模式75 Curve所有权测试夹具初始化失败：" +
                    fixtureFailureReason
                );
                return;
            }

            bool setupPassed = SetupTwoClashRelations(fixture);
            if (!setupPassed)
            {
                Debug.LogError(
                    "模式75 Curve所有权测试初始化失败：" +
                    "两条独立拼点关系未能通过正式Manager完成安排。"
                );
                return;
            }

            invalidDisplay = CreateDisplay(fixture, true);
            bool externalTemplateRejected =
                invalidDisplay.relationTemplate != null &&
                invalidDisplay.externalPrimaryCurve != null &&
                !invalidDisplay.relationTemplate.OwnsPrimaryCurve &&
                !invalidDisplay.controller.ConfigurationValid &&
                invalidDisplay.controller.RelationViewPoolCount == 0 &&
                invalidDisplay.controller.VisibleRelationCount == 0;
            r[96] = externalTemplateRejected;
            DestroyDisplay(invalidDisplay);
            invalidDisplay = null;

            display = CreateDisplay(fixture);
            bool isolatedConfiguration =
                display.controller.ConfigurationValid &&
                display.relationTemplate.OwnsPrimaryCurve &&
                display.relationTemplate.OwnsSecondaryCurve &&
                display.preview != display.relationTemplate.PrimaryCurve &&
                display.preview != display.relationTemplate.SecondaryCurve;

            // 先经正式Preview入口生成运行时Segment，再首次创建对象池View。
            bool previewBegan = display.controller.BeginCardTargetingPreview(
                "AllyA:1"
            );
            display.controller.UpdateCardTargetingPointer(
                new Vector2(420f, 280f)
            );
            bool previewHasRuntimeSegments = previewBegan &&
                display.controller.PreviewActive &&
                display.preview.ActiveSegmentCount > 0;
            RevealAllAndForceLayout(display);
            List<BattleActionRelationUIView> clashViews =
                GetVisibleClashViews(display.controller);
            bool allViewsOwnCurvesWithoutUnregisteredSegments = true;
            for (int index = 0; index < clashViews.Count; index++)
            {
                BattleActionRelationUIView view = clashViews[index];
                allViewsOwnCurvesWithoutUnregisteredSegments &= view != null &&
                    view.OwnsPrimaryCurve && view.OwnsSecondaryCurve &&
                    view.PrimaryCurve.UnregisteredMainSegmentCount == 0 &&
                    view.PrimaryCurve.UnregisteredUnderlaySegmentCount == 0 &&
                    view.SecondaryCurve.UnregisteredMainSegmentCount == 0 &&
                    view.SecondaryCurve.UnregisteredUnderlaySegmentCount == 0;
            }
            r[97] = isolatedConfiguration && previewHasRuntimeSegments &&
                clashViews.Count == 2 &&
                allViewsOwnCurvesWithoutUnregisteredSegments;

            display.controller.EndCardTargetingPreview();
            bool onlyIndependentHalfSolidCurves = clashViews.Count == 2 &&
                AllVisibleViewsAreIndependent(display.controller) &&
                AreClashCurveResourcesUnique(clashViews);
            for (int index = 0; index < clashViews.Count; index++)
            {
                BattleActionRelationUIView view = clashViews[index];
                onlyIndependentHalfSolidCurves &=
                    IsHalfSolidClashCurve(view.PrimaryCurve) &&
                    IsHalfSolidClashCurve(view.SecondaryCurve);
            }
            r[98] = onlyIndependentHalfSolidCurves;

            int visibleBeforeDiagnostic = display.controller.VisibleRelationCount;
            int poolBeforeDiagnostic = display.controller.RelationViewPoolCount;
            bool revealBeforeDiagnostic = display.controller.RevealAllHeld;
            string hoveredBeforeDiagnostic = display.controller.HoveredSlotID;
            string selectedBeforeDiagnostic = display.controller.SelectedSlotID;
            string relationIDsBeforeDiagnostic =
                GetVisibleRelationIDSnapshot(display.controller);
            display.controller.LogCurrentRelationDiagnostics();
            bool diagnosticDidNotMutate =
                display.controller.VisibleRelationCount ==
                    visibleBeforeDiagnostic &&
                display.controller.RelationViewPoolCount ==
                    poolBeforeDiagnostic &&
                display.controller.RevealAllHeld == revealBeforeDiagnostic &&
                display.controller.HoveredSlotID == hoveredBeforeDiagnostic &&
                display.controller.SelectedSlotID == selectedBeforeDiagnostic &&
                GetVisibleRelationIDSnapshot(display.controller) ==
                    relationIDsBeforeDiagnostic;
            display.controller.SetRevealAllHeld(false);
            Canvas.ForceUpdateCanvases();
            display.controller.SetRevealAllHeld(true);
            Canvas.ForceUpdateCanvases();
            clashViews = GetVisibleClashViews(display.controller);
            r[99] = diagnosticDidNotMutate && clashViews.Count == 2 &&
                AllVisibleViewsAreIndependent(display.controller) &&
                AreClashCurveResourcesUnique(clashViews);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "模式75 Curve所有权测试初始化或执行失败：" + exception
            );
        }
        finally
        {
            DestroyDisplay(invalidDisplay);
            DestroyDisplay(display);
        }
    }

    private static bool IsHalfSolidClashCurve(
        BattleBezierRelationLineUIView curve
    )
    {
        return curve != null && curve.CurrentLineStyle == "Solid" &&
            curve.ActiveSegmentCount > 0 && curve.ArrowActiveSelf &&
            Mathf.Abs(curve.RangeEnd - curve.RangeStart) < 0.75f;
    }

    private static string GetVisibleRelationIDSnapshot(
        BattleActionRelationLineController controller
    )
    {
        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < controller.VisibleRelationCount; index++)
        {
            BattleActionRelationUIView view = controller.GetVisibleView(index);
            builder.Append(index).Append(':')
                .Append(view != null ? view.RelationID : "null")
                .Append('|');
        }
        return builder.ToString();
    }

    private static bool TryValidateCurveOwnershipFixture(
        Fixture fixture,
        out string failureReason
    )
    {
        failureReason = string.Empty;
        if (fixture == null)
        {
            failureReason = "fixture为null。";
            return false;
        }
        if (fixture.runtime == null)
        {
            failureReason = "runtime为null。";
            return false;
        }
        if (fixture.allyA == null || fixture.allyB == null)
        {
            failureReason = "allyA或allyB为null。";
            return false;
        }
        if (fixture.enemy == null)
        {
            failureReason = "enemy1为null。";
            return false;
        }
        if (fixture.enemy2 == null)
        {
            failureReason = "enemy2为null。";
            return false;
        }
        if (object.ReferenceEquals(fixture.enemy, fixture.enemy2))
        {
            failureReason = "enemy1与enemy2引用相同。";
            return false;
        }
        if (fixture.intent == null)
        {
            failureReason = "enemyIntent1为null。";
            return false;
        }
        if (fixture.intent2 == null)
        {
            failureReason = "enemyIntent2为null。";
            return false;
        }
        if (object.ReferenceEquals(fixture.intent, fixture.intent2))
        {
            failureReason = "enemyIntent1与enemyIntent2引用相同。";
            return false;
        }
        if (!object.ReferenceEquals(fixture.intent.enemy, fixture.enemy) ||
            !object.ReferenceEquals(fixture.intent2.enemy, fixture.enemy2))
        {
            failureReason = "Intent与所属敌人引用不匹配。";
            return false;
        }
        if (fixture.intent.enemyCardState == null ||
            fixture.intent2.enemyCardState == null)
        {
            failureReason = "Intent缺少敌人卡牌状态。";
            return false;
        }
        if (fixture.intent.enemySlotIndex <= 0 ||
            fixture.intent2.enemySlotIndex <= 0)
        {
            failureReason = "Intent的enemySlotIndex不合法。";
            return false;
        }
        if (fixture.attack == null || fixture.attack2 == null)
        {
            failureReason = "AllyA两张独立攻击卡未完整创建。";
            return false;
        }
        if (fixture.runtime.intentQueue == null ||
            !fixture.runtime.intentQueue.Contains(fixture.intent) ||
            !fixture.runtime.intentQueue.Contains(fixture.intent2))
        {
            failureReason = "RuntimeState未登记两个独立Intent。";
            return false;
        }
        return true;
    }

    private static bool SetupTwoClashRelations(Fixture fixture)
    {
        SetIntentTarget(fixture.intent, fixture.allyA, 1);
        SetIntentTarget(fixture.intent2, fixture.allyA, 2);
        bool targetsInitialized =
            object.ReferenceEquals(
                fixture.intent.actualTargetCharacter,
                fixture.allyA
            ) && fixture.intent.actualTargetSlotIndex == 1 &&
            object.ReferenceEquals(
                fixture.intent2.actualTargetCharacter,
                fixture.allyA
            ) && fixture.intent2.actualTargetSlotIndex == 2;
        BattleActionAssignmentResult firstResult;
        BattleActionAssignmentResult secondResult;
        bool firstAssigned = BattleActionSlotManager.TryAssignToEnemyIntent(
                fixture.runtime,
                fixture.allyA,
                1,
                fixture.attack,
                fixture.intent,
                out firstResult
            );
        bool secondAssigned = BattleActionSlotManager.TryAssignToEnemyIntent(
                fixture.runtime,
                fixture.allyA,
                2,
                fixture.attack2,
                fixture.intent2,
                out secondResult
            );
        return targetsInitialized && firstAssigned && secondAssigned &&
            firstResult != null && firstResult.isSuccess &&
            secondResult != null && secondResult.isSuccess &&
            fixture.intent.isResponded && fixture.intent2.isResponded &&
            object.ReferenceEquals(
                fixture.intent.actualTargetCharacter,
                fixture.allyA
            ) && fixture.intent.actualTargetSlotIndex == 1 &&
            object.ReferenceEquals(
                fixture.intent2.actualTargetCharacter,
                fixture.allyA
            ) && fixture.intent2.actualTargetSlotIndex == 2;
    }

    private static List<BattleActionRelationUIView> GetVisibleClashViews(
        BattleActionRelationLineController controller
    )
    {
        List<BattleActionRelationUIView> views =
            new List<BattleActionRelationUIView>();
        for (int index = 0; index < controller.VisibleRelationCount; index++)
        {
            BattleActionRelationUIView view = controller.GetVisibleView(index);
            if (view != null &&
                (view.Kind == BattleActionRelationKind.AttackClash ||
                 view.Kind == BattleActionRelationKind.DefenseResponse ||
                 view.Kind == BattleActionRelationKind.EvadeResponse))
            {
                views.Add(view);
            }
        }
        return views;
    }

    private static bool AreClashCurveResourcesUnique(
        List<BattleActionRelationUIView> views
    )
    {
        HashSet<int> curveIDs = new HashSet<int>();
        HashSet<int> arrowIDs = new HashSet<int>();
        for (int index = 0; index < views.Count; index++)
        {
            BattleActionRelationUIView view = views[index];
            if (view == null || !view.ValidateCurveOwnership(false) ||
                !curveIDs.Add(view.PrimaryCurve.GetInstanceID()) ||
                !curveIDs.Add(view.SecondaryCurve.GetInstanceID()) ||
                !arrowIDs.Add(view.PrimaryCurve.ArrowInstanceID) ||
                !arrowIDs.Add(view.SecondaryCurve.ArrowInstanceID) ||
                view.PrimaryCurve.ActiveSegmentCount <= 0 ||
                view.SecondaryCurve.ActiveSegmentCount <= 0)
            {
                return false;
            }
        }
        return views.Count > 0;
    }

    private static BattleActionRelationDescriptor FindRelationByID(
        IReadOnlyList<BattleActionRelationDescriptor> relations,
        string relationID
    )
    {
        for (int index = 0; index < relations.Count; index++)
        {
            if (relations[index] != null &&
                relations[index].RelationID == relationID)
            {
                return relations[index];
            }
        }
        return null;
    }

    private static AssignmentProbe ProbePendingTarget(
        Fixture fixture,
        BattleCardState card
    )
    {
        AssignmentProbe probe = new AssignmentProbe();
        BattleCardSelectionController selection =
            new BattleCardSelectionController();
        BattleCardInteractionCoordinator coordinator =
            new BattleCardInteractionCoordinator(selection);
        BattleActionSlotUIView source = CreateSlotView(
            "Mode75PendingSource", fixture.allyA, 0, false, null
        );
        BattleCardUIView cardView = CreateCardView(
            "Mode75PendingCard", fixture.allyA, fixture.enemy,
            card, selection
        );
        probe.selected = coordinator.SelectSourceSlot(source);
        probe.cardSelected = selection.SelectCard(cardView);
        probe.selectionRetained =
            object.ReferenceEquals(coordinator.SelectedActionSlotView, source);
        probe.slot = fixture.sourceSlot;
        UnityEngine.Object.DestroyImmediate(cardView.gameObject);
        UnityEngine.Object.DestroyImmediate(source.gameObject);
        return probe;
    }

    private static AssignmentProbe ProbeSelf(Fixture fixture, string type)
    {
        BattleCardState card = GetCard(fixture, type);
        BattleCardSelectionController selection =
            new BattleCardSelectionController();
        BattleCardInteractionCoordinator coordinator =
            new BattleCardInteractionCoordinator(selection);
        BattleActionSlotUIView source = CreateSlotView(
            "Mode75SelfSource", fixture.allyA, 0, false, null
        );
        BattleCardUIView cardView = CreateCardView(
            "Mode75SelfCard", fixture.allyA, fixture.allyA,
            card, selection
        );
        AssignmentProbe probe = new AssignmentProbe();
        probe.selected = coordinator.SelectSourceSlot(source);
        probe.cardSelected = selection.SelectCard(cardView);
        BattleCardInteractionOutcome outcome =
            coordinator.ClickSelectedSourceSlotAsSelf(
                fixture.runtime,
                source
            );
        probe.success = outcome != null && outcome.isSuccess;
        probe.result = outcome != null ? outcome.assignmentResult : null;
        probe.slot = fixture.sourceSlot;
        probe.selectionRetained =
            object.ReferenceEquals(coordinator.SelectedActionSlotView, source);
        probe.slotVisualRetained = source.IsSelected;
        UnityEngine.Object.DestroyImmediate(cardView.gameObject);
        UnityEngine.Object.DestroyImmediate(source.gameObject);
        return probe;
    }

    private static AssignmentProbe ProbeEnemy(
        Fixture fixture,
        string type,
        int targetSlotIndex
    )
    {
        BattleCardState card = GetCard(fixture, type);
        BattleCardSelectionController selection =
            new BattleCardSelectionController();
        BattleCardInteractionCoordinator coordinator =
            new BattleCardInteractionCoordinator(selection);
        BattleActionSlotUIView source = CreateSlotView(
            "Mode75EnemySource", fixture.allyA, 0, false, null
        );
        BattleActionSlotUIView target = CreateSlotView(
            "Mode75EnemyTarget", fixture.enemy,
            targetSlotIndex - 1, true, null
        );
        BattleCardUIView cardView = CreateCardView(
            "Mode75EnemyCard", fixture.allyA, fixture.enemy,
            card, selection
        );
        coordinator.SelectSourceSlot(source);
        selection.SelectCard(cardView);
        BattleCardInteractionOutcome outcome = coordinator.ClickEnemySlot(
            fixture.runtime,
            target
        );
        AssignmentProbe probe = new AssignmentProbe
        {
            success = outcome != null && outcome.isSuccess,
            result = outcome != null ? outcome.assignmentResult : null,
            slot = fixture.sourceSlot,
            selectionRetained = object.ReferenceEquals(
                coordinator.SelectedActionSlotView,
                source
            ),
            slotVisualRetained = source.IsSelected
        };
        UnityEngine.Object.DestroyImmediate(cardView.gameObject);
        UnityEngine.Object.DestroyImmediate(source.gameObject);
        UnityEngine.Object.DestroyImmediate(target.gameObject);
        return probe;
    }

    private static IReadOnlyList<BattleActionRelationDescriptor>
        GetRelations(string type, bool self, bool mutual, int enemySlot)
    {
        Fixture fixture = CreateFixture();
        BattleActionAssignmentResult result;
        BattleCardState card = GetCard(fixture, type);
        if (self)
        {
            BattleActionSlotManager.TryAssignToSelf(
                fixture.runtime, fixture.allyA, 1, card, out result
            );
        }
        else if (mutual)
        {
            BattleActionSlotManager.TryAssignToEnemyIntent(
                fixture.runtime, fixture.allyA, 1, card,
                fixture.intent, out result
            );
        }
        else
        {
            BattleActionSlotManager.TryAssignToEnemy(
                fixture.runtime, fixture.allyA, 1, card,
                fixture.enemy, enemySlot, out result
            );
        }
        return new BattleActionRelationQueryService(
            fixture.runtime
        ).GetAllCurrentRelations();
    }

    private static bool HasSelectedRelation(
        string cardType,
        BattleActionRelationKind kind
    )
    {
        Fixture fixture = CreateFixture();
        BattleActionAssignmentResult result;
        bool isUnilateral =
            kind == BattleActionRelationKind.PlayerUnilateralTarget;
        bool assigned = isUnilateral
            ? BattleActionSlotManager.TryAssignToEnemy(
                fixture.runtime,
                fixture.allyA,
                1,
                GetCard(fixture, cardType),
                fixture.enemy,
                2,
                out result
            )
            : BattleActionSlotManager.TryAssignToEnemyIntent(
                fixture.runtime,
                fixture.allyA,
                1,
                GetCard(fixture, cardType),
                fixture.intent,
                out result
            );
        Display display = CreateDisplay(fixture);
        display.controller.SetSelectedSlot("AllyA:1");
        string expectedEnemySlotID = isUnilateral ? "Enemy:2" : "Enemy:1";
        BattleActionRelationDescriptor expectedRelation =
            FindSelectedRelation(
                display.controller.CachedRelations,
                kind,
                cardType,
                "AllyA:1",
                expectedEnemySlotID
            );
        bool visible = expectedRelation != null &&
            IsRelationVisible(
                display.controller,
                expectedRelation.RelationID
            );
        bool passed = assigned && result != null && result.isSuccess &&
            display.controller.SelectedSlotID == "AllyA:1" &&
            expectedRelation != null &&
            expectedRelation.InvolvesSlot("AllyA:1") &&
            visible;
        if (!passed)
        {
            LogSelectedRelationDiagnostic(
                display.controller,
                "AllyA:1"
            );
        }
        DestroyDisplay(display);
        return passed;
    }

    private static BattleActionRelationDescriptor FindSelectedRelation(
        IReadOnlyList<BattleActionRelationDescriptor> relations,
        BattleActionRelationKind kind,
        string playerActionType,
        string playerSlotID,
        string enemySlotID
    )
    {
        for (int index = 0; index < relations.Count; index++)
        {
            BattleActionRelationDescriptor relation = relations[index];
            if (relation.Kind == kind &&
                relation.PlayerActionType == playerActionType &&
                relation.PlayerSlotID == playerSlotID &&
                relation.EnemySlotID == enemySlotID)
            {
                return relation;
            }
        }
        return null;
    }

    private static bool IsRelationVisible(
        BattleActionRelationLineController controller,
        string relationID
    )
    {
        for (int index = 0; index < controller.VisibleRelationCount; index++)
        {
            BattleActionRelationUIView view = controller.GetVisibleView(index);
            if (view != null && view.RelationID == relationID)
            {
                return true;
            }
        }
        return false;
    }

    private static BattleActionRelationUIView FindVisibleRelationByID(
        BattleActionRelationLineController controller,
        string relationID
    )
    {
        for (int index = 0; index < controller.VisibleRelationCount; index++)
        {
            BattleActionRelationUIView view = controller.GetVisibleView(index);
            if (view != null && view.RelationID == relationID)
            {
                return view;
            }
        }
        return null;
    }

    private static BattleActionRelationUIView FindVisibleRelationByKind(
        BattleActionRelationLineController controller,
        BattleActionRelationKind kind
    )
    {
        for (int index = 0; index < controller.VisibleRelationCount; index++)
        {
            BattleActionRelationUIView view = controller.GetVisibleView(index);
            if (view != null && view.Kind == kind)
            {
                return view;
            }
        }
        return null;
    }

    private static void LogSelectedRelationDiagnostic(
        BattleActionRelationLineController controller,
        string selectedSlotID
    )
    {
        Debug.Log(
            "[模式75 selectedSlot关系诊断] selectedSlotID=" +
            selectedSlotID + "，relationCount=" +
            controller.CachedRelations.Count
        );
        for (int index = 0; index < controller.CachedRelations.Count; index++)
        {
            BattleActionRelationDescriptor relation =
                controller.CachedRelations[index];
            Debug.Log(
                "[模式75 selectedSlot关系诊断] Kind=" + relation.Kind +
                "，PlayerActionType=" + relation.PlayerActionType +
                "，PlayerSlotID=" + relation.PlayerSlotID +
                "，EnemySlotID=" + relation.EnemySlotID +
                "，InvolvesSelected=" +
                relation.InvolvesSlot(selectedSlotID) +
                "，Visible=" +
                IsRelationVisible(controller, relation.RelationID)
            );
        }
    }

    private static bool HasSelectedSelfWithoutRelation(string cardType)
    {
        Fixture fixture = CreateFixture();
        BattleActionAssignmentResult result;
        BattleActionSlotManager.TryAssignToSelf(
            fixture.runtime, fixture.allyA, 1,
            GetCard(fixture, cardType), out result
        );
        fixture.runtime.SetIntentQueue(new List<BattleEnemyIntent>());
        Display display = CreateDisplay(fixture);
        display.controller.SetSelectedSlot("AllyA:1");
        bool passed = display.controller.SelectedSlotID == "AllyA:1" &&
            display.controller.VisibleRelationCount == 0;
        DestroyDisplay(display);
        return passed;
    }

    private static bool CancelTargetKeepsSelection(
        BattleActionRelationLineController controller
    )
    {
        string selected = controller.SelectedSlotID;
        controller.EndCardTargetingPreview();
        return controller.SelectedSlotID == selected;
    }

    private static void RevealAllAndForceLayout(Display display)
    {
        display.controller.RefreshRelations();
        display.controller.SetRevealAllHeld(true);
        Canvas.ForceUpdateCanvases();
    }

    private static void LogRevealAllDiagnostic(
        string label,
        Display display,
        int expectedRelationCount
    )
    {
        BattleActionRelationLineController controller = display.controller;
        HashSet<int> viewIDs = new HashSet<int>();
        HashSet<int> primaryCurveIDs = new HashSet<int>();
        HashSet<int> arrowIDs = new HashSet<int>();
        Debug.Log(
            "[模式75多关系诊断] " + label +
            "，ExpectedDescriptors=" + expectedRelationCount +
            "，ActualDescriptors=" + controller.CachedRelations.Count +
            "，ExpectedViews=" + expectedRelationCount +
            "，ActualViews=" + controller.VisibleRelationCount
        );

        for (int index = 0; index < controller.CachedRelations.Count; index++)
        {
            BattleActionRelationDescriptor relation =
                controller.CachedRelations[index];
            Debug.Log(
                "[模式75 Descriptor] Index=" + index +
                "，RelationID=" + relation.RelationID +
                "，SourceSlotID=" + relation.SourceSlotID +
                "，TargetSlotID=" + relation.TargetSlotID +
                "，Kind=" + relation.Kind +
                "，SourceSide=" + relation.SourceSide +
                "，LaneIndex=" + relation.LaneIndex
            );
        }

        for (int index = 0; index < controller.VisibleRelationCount; index++)
        {
            BattleActionRelationUIView view = controller.GetVisibleView(index);
            if (view == null)
            {
                Debug.Log("[模式75 View] Index=" + index + "，View=null");
                continue;
            }

            viewIDs.Add(view.GetInstanceID());
            if (view.PrimaryCurve != null)
            {
                primaryCurveIDs.Add(view.PrimaryCurve.GetInstanceID());
                arrowIDs.Add(view.PrimaryCurve.ArrowInstanceID);
            }
            if (view.SecondaryCurve != null &&
                view.SecondaryCurve.IsVisible)
            {
                arrowIDs.Add(view.SecondaryCurve.ArrowInstanceID);
            }

            Debug.Log(
                "[模式75 View] Index=" + index +
                "，RelationID=" + view.RelationID +
                "，ViewInstanceID=" + view.GetInstanceID() +
                "，Root=" +
                (view.transform.parent != null
                    ? view.transform.parent.name
                    : "null") +
                "，SiblingIndex=" + view.SiblingIndex +
                "，EndpointOffset=" +
                view.UnilateralArrowEndpointOffset
            );
            LogCurveDiagnostic("Primary", view.PrimaryCurve);
            LogCurveDiagnostic("Secondary", view.SecondaryCurve);
        }

        Debug.Log(
            "[模式75多关系诊断] DistinctViewIDs=" + viewIDs.Count +
            "，DistinctPrimaryCurveIDs=" + primaryCurveIDs.Count +
            "，DistinctArrowIDs=" + arrowIDs.Count
        );
    }

    private static void LogCurveDiagnostic(
        string label,
        BattleBezierRelationLineUIView curve
    )
    {
        if (curve == null)
        {
            Debug.Log("[模式75 Curve] " + label + "=null");
            return;
        }

        Debug.Log(
            "[模式75 Curve] " + label +
            "，CurveInstanceID=" + curve.GetInstanceID() +
            "，ArrowInstanceID=" + curve.ArrowInstanceID +
            "，SegmentTemplateInstanceID=" +
            curve.SegmentTemplateInstanceID +
            "，UnderlayArrowInstanceID=" +
            curve.UnderlayArrowInstanceID +
            "，CanvasGroupInstanceID=" + curve.CanvasGroupInstanceID +
            "，ArrowActive=" + curve.ArrowActiveSelf +
            "，ArrowAlpha=" + curve.ArrowAlpha +
            "，ArrowSize=" + curve.ArrowRenderedSize +
            "，ActiveSegmentCount=" + curve.ActiveSegmentCount +
            "，FirstSegmentInstanceID=" +
            curve.GetActiveSegmentInstanceID(0) +
            "，ArrowTip=" + curve.ArrowTip +
            "，ArrowSiblingIndex=" + curve.ArrowSiblingIndex +
            "，LayerOrderValid=" +
            curve.HasDeterministicVisualLayerOrder
        );
    }

    private static bool AssignPlayerAttack(
        Fixture fixture,
        CharacterData owner,
        BattleCardState card,
        CharacterData targetEnemy,
        int ownerSlotIndex = 1,
        int targetEnemySlotIndex = 1
    )
    {
        BattleActionAssignmentResult result;
        return BattleActionSlotManager.TryAssignToEnemy(
            fixture.runtime,
            owner,
            ownerSlotIndex,
            card,
            targetEnemy,
            targetEnemySlotIndex,
            out result
        ) && result != null && result.isSuccess;
    }

    private static bool CancelAllPlayerAssignments(Fixture fixture)
    {
        bool allCancelled = true;
        CharacterData[] allies = { fixture.allyA, fixture.allyB };
        for (int allyIndex = 0; allyIndex < allies.Length; allyIndex++)
        {
            for (int slotIndex = 1; slotIndex <= 2; slotIndex++)
            {
                BattleActionAssignmentResult result;
                allCancelled &= BattleActionSlotManager.TryCancelAssignment(
                    fixture.runtime,
                    allies[allyIndex],
                    slotIndex,
                    out result
                );
            }
        }
        fixture.intent.ResetResponseState();
        fixture.intent2.ResetResponseState();
        return allCancelled;
    }

    private static bool SetupRelationSceneA(Fixture fixture)
    {
        SetIntentTarget(fixture.intent, fixture.allyA, 1);
        SetIntentTarget(fixture.intent2, fixture.allyB, 2);
        BattleActionAssignmentResult responseResult;
        bool responseAssigned =
            BattleActionSlotManager.TryAssignToEnemyIntent(
                fixture.runtime,
                fixture.allyA,
                1,
                fixture.attack,
                fixture.intent,
                out responseResult
            );
        return responseAssigned &&
            AssignPlayerAttack(
                fixture,
                fixture.allyA,
                fixture.attack2,
                fixture.enemy2,
                2,
                1
            ) && AssignPlayerAttack(
                fixture,
                fixture.allyB,
                fixture.allyBAttack,
                fixture.enemy2,
                1,
                2
            ) && AssignPlayerAttack(
                fixture,
                fixture.allyB,
                fixture.allyBAttack2,
                fixture.enemy2,
                2,
                2
            );
    }

    private static bool SetupRelationSceneB(Fixture fixture)
    {
        SetIntentTarget(fixture.intent, fixture.allyA, 2);
        SetIntentTarget(fixture.intent2, fixture.allyB, 2);
        return AssignPlayerAttack(
            fixture,
            fixture.allyB,
            fixture.allyBAttack,
            fixture.enemy2,
            1,
            2
        ) && AssignPlayerAttack(
            fixture,
            fixture.allyB,
            fixture.allyBAttack2,
            fixture.enemy2,
            2,
            2
        );
    }

    private static bool SetupRelationSceneC(Fixture fixture)
    {
        SetIntentTarget(fixture.intent, fixture.allyA, 2);
        SetIntentTarget(fixture.intent2, fixture.allyB, 2);
        return AssignPlayerAttack(
            fixture,
            fixture.allyA,
            fixture.attack,
            fixture.enemy2,
            1,
            2
        ) && AssignPlayerAttack(
            fixture,
            fixture.allyA,
            fixture.attack2,
            fixture.enemy2,
            2,
            2
        ) && AssignPlayerAttack(
            fixture,
            fixture.allyB,
            fixture.allyBAttack,
            fixture.enemy2,
            1,
            2
        ) && AssignPlayerAttack(
            fixture,
            fixture.allyB,
            fixture.allyBAttack2,
            fixture.enemy2,
            2,
            2
        );
    }

    private static bool ValidateSceneA(
        BattleActionRelationLineController controller
    )
    {
        BattleActionRelationUIView clash = FindVisibleRelationByKind(
            controller,
            BattleActionRelationKind.AttackClash
        );
        return controller.CachedRelations.Count == 5 &&
            controller.VisibleRelationCount == 5 &&
            CountRelationsByKind(
                controller.CachedRelations,
                BattleActionRelationKind.AttackClash
            ) == 1 &&
            CountRelationsByKind(
                controller.CachedRelations,
                BattleActionRelationKind.PlayerUnilateralTarget
            ) == 3 &&
            clash != null && IsCurveArrowReady(clash.PrimaryCurve) &&
            IsCurveArrowReady(clash.SecondaryCurve) &&
            AllVisibleCurvesHaveArrows(controller) &&
            AllVisibleViewsAreIndependent(controller);
    }

    private static bool ValidateSceneB(
        BattleActionRelationLineController controller
    )
    {
        BattleActionRelationUIView first = FindVisibleRelationByID(
            controller,
            "AllyB:1->Enemy2:2"
        );
        BattleActionRelationUIView second = FindVisibleRelationByID(
            controller,
            "AllyB:2->Enemy2:2"
        );
        return controller.CachedRelations.Count == 4 &&
            controller.VisibleRelationCount == 4 &&
            CountRelationsByKind(
                controller.CachedRelations,
                BattleActionRelationKind.PlayerUnilateralTarget
            ) == 2 &&
            HaveSeparatedVisibleArrows(first, second) &&
            AllVisibleViewsAreIndependent(controller);
    }

    private static bool ValidateSceneC(
        BattleActionRelationLineController controller
    )
    {
        return controller.CachedRelations.Count == 6 &&
            controller.VisibleRelationCount == 6 &&
            CountRelationsByKind(
                controller.CachedRelations,
                BattleActionRelationKind.PlayerUnilateralTarget
            ) == 4 &&
            HaveDistinctPlayerArrowTips(
                controller,
                "Enemy2:2",
                4
            ) && AllVisibleCurvesHaveArrows(controller) &&
            AllVisibleViewsAreIndependent(controller);
    }

    private static int CountRelationsByKind(
        IReadOnlyList<BattleActionRelationDescriptor> relations,
        BattleActionRelationKind kind
    )
    {
        int count = 0;
        for (int index = 0; index < relations.Count; index++)
        {
            if (relations[index].Kind == kind)
            {
                count++;
            }
        }
        return count;
    }

    private static bool HaveDistinctPlayerArrowTips(
        BattleActionRelationLineController controller,
        string targetSlotID,
        int expectedCount
    )
    {
        List<BattleActionRelationUIView> views =
            new List<BattleActionRelationUIView>();
        for (int index = 0; index < controller.CachedRelations.Count; index++)
        {
            BattleActionRelationDescriptor relation =
                controller.CachedRelations[index];
            if (relation.Kind !=
                    BattleActionRelationKind.PlayerUnilateralTarget ||
                relation.TargetSlotID != targetSlotID)
            {
                continue;
            }
            BattleActionRelationUIView view = FindVisibleRelationByID(
                controller,
                relation.RelationID
            );
            if (!HasVisiblePrimaryArrow(view))
            {
                return false;
            }
            views.Add(view);
        }

        if (views.Count != expectedCount)
        {
            return false;
        }
        for (int left = 0; left < views.Count; left++)
        {
            for (int right = left + 1; right < views.Count; right++)
            {
                if (views[left].PrimaryCurve.ArrowInstanceID ==
                        views[right].PrimaryCurve.ArrowInstanceID ||
                    Approximately(
                        views[left].PrimaryCurve.ArrowTip,
                        views[right].PrimaryCurve.ArrowTip
                    ))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static void SetIntentTarget(
        BattleEnemyIntent intent,
        CharacterData target,
        int slotIndex
    )
    {
        intent.originalTargetCharacter = target;
        intent.originalTargetSlotIndex = slotIndex;
        intent.ResetResponseState();
    }

    private static bool HasVisiblePrimaryArrow(
        BattleActionRelationUIView view
    )
    {
        return view != null &&
            IsCurveArrowReady(view.PrimaryCurve);
    }

    private static bool HaveSeparatedVisibleArrows(
        BattleActionRelationUIView left,
        BattleActionRelationUIView right
    )
    {
        return HasVisiblePrimaryArrow(left) &&
            HasVisiblePrimaryArrow(right) &&
            Mathf.Abs(left.UnilateralArrowEndpointOffset) > 0.001f &&
            Mathf.Abs(right.UnilateralArrowEndpointOffset) > 0.001f &&
            Vector2.Distance(
                left.PrimaryCurve.ArrowTip,
                right.PrimaryCurve.ArrowTip
            ) > 0.01f;
    }

    private static bool AllVisibleCurvesHaveArrows(
        BattleActionRelationLineController controller
    )
    {
        for (int index = 0; index < controller.VisibleRelationCount; index++)
        {
            BattleActionRelationUIView view = controller.GetVisibleView(index);
            if (view == null || !IsCurveArrowReady(view.PrimaryCurve))
            {
                return false;
            }
            if (view.SecondaryCurve != null &&
                view.SecondaryCurve.IsVisible &&
                !IsCurveArrowReady(view.SecondaryCurve))
            {
                return false;
            }
        }
        return controller.VisibleRelationCount > 0;
    }

    private static bool AllVisibleViewsAreIndependent(
        BattleActionRelationLineController controller
    )
    {
        HashSet<int> viewIDs = new HashSet<int>();
        HashSet<int> curveIDs = new HashSet<int>();
        HashSet<int> arrowIDs = new HashSet<int>();
        for (int index = 0; index < controller.VisibleRelationCount; index++)
        {
            BattleActionRelationUIView view = controller.GetVisibleView(index);
            if (view == null || !viewIDs.Add(view.GetInstanceID()) ||
                view.PrimaryCurve == null ||
                !curveIDs.Add(view.PrimaryCurve.GetInstanceID()) ||
                view.PrimaryCurve.ArrowInstanceID == 0 ||
                !arrowIDs.Add(view.PrimaryCurve.ArrowInstanceID))
            {
                return false;
            }

            if (view.SecondaryCurve != null &&
                view.SecondaryCurve.IsVisible &&
                (!curveIDs.Add(view.SecondaryCurve.GetInstanceID()) ||
                 view.SecondaryCurve.ArrowInstanceID == 0 ||
                 !arrowIDs.Add(view.SecondaryCurve.ArrowInstanceID) ||
                 view.PrimaryCurve.SegmentTemplateInstanceID ==
                    view.SecondaryCurve.SegmentTemplateInstanceID ||
                 view.PrimaryCurve.CanvasGroupInstanceID ==
                    view.SecondaryCurve.CanvasGroupInstanceID ||
                 view.PrimaryCurve.UnderlayArrowInstanceID ==
                    view.SecondaryCurve.UnderlayArrowInstanceID ||
                 view.PrimaryCurve.GetActiveSegmentInstanceID(0) ==
                    view.SecondaryCurve.GetActiveSegmentInstanceID(0)))
            {
                return false;
            }
        }
        return viewIDs.Count == controller.VisibleRelationCount;
    }

    private static bool ValidateClashCurveIndependence(Display display)
    {
        BattleActionRelationUIView clashView = FindVisibleRelationByKind(
            display.controller,
            BattleActionRelationKind.AttackClash
        );
        if (clashView == null || clashView.PrimaryCurve == null ||
            clashView.SecondaryCurve == null ||
            !IsCurveArrowReady(clashView.PrimaryCurve) ||
            !IsCurveArrowReady(clashView.SecondaryCurve))
        {
            return false;
        }

        int primarySegments = clashView.PrimaryCurve.ActiveSegmentCount;
        Vector2 primaryTip = clashView.PrimaryCurve.ArrowTip;
        float primaryAlpha = clashView.PrimaryCurve.ArrowAlpha;
        clashView.SecondaryCurve.Clear();
        bool primarySurvivedSecondaryClear =
            clashView.PrimaryCurve.ArrowActiveSelf &&
            clashView.PrimaryCurve.ActiveSegmentCount == primarySegments &&
            Approximately(clashView.PrimaryCurve.ArrowTip, primaryTip) &&
            Mathf.Abs(clashView.PrimaryCurve.ArrowAlpha - primaryAlpha) <
                0.001f;

        RevealAllAndForceLayout(display);
        clashView = FindVisibleRelationByKind(
            display.controller,
            BattleActionRelationKind.AttackClash
        );
        if (clashView == null || clashView.PrimaryCurve == null ||
            clashView.SecondaryCurve == null)
        {
            return false;
        }
        int secondarySegments = clashView.SecondaryCurve.ActiveSegmentCount;
        Vector2 secondaryTip = clashView.SecondaryCurve.ArrowTip;
        float secondaryAlpha = clashView.SecondaryCurve.ArrowAlpha;
        clashView.PrimaryCurve.Clear();
        bool secondarySurvivedPrimaryClear =
            clashView.SecondaryCurve.ArrowActiveSelf &&
            clashView.SecondaryCurve.ActiveSegmentCount == secondarySegments &&
            Approximately(clashView.SecondaryCurve.ArrowTip, secondaryTip) &&
            Mathf.Abs(clashView.SecondaryCurve.ArrowAlpha - secondaryAlpha) <
                0.001f;
        RevealAllAndForceLayout(display);
        return primarySurvivedSecondaryClear &&
            secondarySurvivedPrimaryClear;
    }

    private static bool IsCurveArrowReady(
        BattleBezierRelationLineUIView curve
    )
    {
        return curve != null && curve.IsVisible &&
            curve.ArrowActiveSelf && curve.ArrowAlpha > 0f &&
            curve.ArrowRenderedSize.x > 0f &&
            curve.ArrowRenderedSize.y > 0f &&
            curve.HasDeterministicVisualLayerOrder;
    }

    private static Dictionary<string, RelationVisualSnapshot>
        CaptureVisibleRelationVisuals(
            BattleActionRelationLineController controller
        )
    {
        Dictionary<string, RelationVisualSnapshot> snapshots =
            new Dictionary<string, RelationVisualSnapshot>(
                StringComparer.Ordinal
            );
        for (int index = 0; index < controller.VisibleRelationCount; index++)
        {
            BattleActionRelationUIView view = controller.GetVisibleView(index);
            if (view == null || view.PrimaryCurve == null)
            {
                continue;
            }
            snapshots[view.RelationID] = new RelationVisualSnapshot
            {
                arrowTip = view.PrimaryCurve.ArrowTip,
                siblingIndex = view.SiblingIndex,
                parentName = view.transform.parent != null
                    ? view.transform.parent.name
                    : string.Empty
            };
        }
        return snapshots;
    }

    private static bool VisibleRelationIDsMatchAll(
        BattleActionRelationLineController controller
    )
    {
        HashSet<string> expected = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < controller.CachedRelations.Count; index++)
        {
            expected.Add(controller.CachedRelations[index].RelationID);
        }
        return VisibleRelationIDsMatch(controller, expected);
    }

    private static bool VisibleRelationIDsMatchSlots(
        BattleActionRelationLineController controller,
        string firstSlotID,
        string secondSlotID
    )
    {
        HashSet<string> expected = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < controller.CachedRelations.Count; index++)
        {
            BattleActionRelationDescriptor relation =
                controller.CachedRelations[index];
            if (relation.InvolvesSlot(firstSlotID) ||
                (!string.IsNullOrEmpty(secondSlotID) &&
                 relation.InvolvesSlot(secondSlotID)))
            {
                expected.Add(relation.RelationID);
            }
        }
        return VisibleRelationIDsMatch(controller, expected);
    }

    private static bool VisibleRelationIDsMatch(
        BattleActionRelationLineController controller,
        HashSet<string> expected
    )
    {
        if (controller.VisibleRelationCount != expected.Count)
        {
            return false;
        }
        for (int index = 0; index < controller.VisibleRelationCount; index++)
        {
            BattleActionRelationUIView view = controller.GetVisibleView(index);
            if (view == null || !expected.Contains(view.RelationID))
            {
                return false;
            }
        }
        return true;
    }

    private static bool VisibleVisualsMatch(
        BattleActionRelationLineController controller,
        Dictionary<string, RelationVisualSnapshot> expected
    )
    {
        if (expected == null ||
            controller.VisibleRelationCount != expected.Count ||
            !AllVisibleCurvesHaveArrows(controller))
        {
            return false;
        }

        Dictionary<string, RelationVisualSnapshot> actual =
            CaptureVisibleRelationVisuals(controller);
        for (int index = 0; index < controller.VisibleRelationCount; index++)
        {
            BattleActionRelationUIView view = controller.GetVisibleView(index);
            RelationVisualSnapshot snapshot;
            if (view == null || view.PrimaryCurve == null ||
                !expected.TryGetValue(view.RelationID, out snapshot) ||
                !Approximately(view.PrimaryCurve.ArrowTip, snapshot.arrowTip) ||
                view.transform.parent == null ||
                view.transform.parent.name != snapshot.parentName)
            {
                return false;
            }
        }

        foreach (KeyValuePair<string, RelationVisualSnapshot> left in expected)
        {
            foreach (KeyValuePair<string, RelationVisualSnapshot> right in expected)
            {
                if (string.CompareOrdinal(left.Key, right.Key) >= 0 ||
                    left.Value.parentName != right.Value.parentName)
                {
                    continue;
                }
                RelationVisualSnapshot actualLeft;
                RelationVisualSnapshot actualRight;
                if (!actual.TryGetValue(left.Key, out actualLeft) ||
                    !actual.TryGetValue(right.Key, out actualRight) ||
                    Math.Sign(left.Value.siblingIndex - right.Value.siblingIndex) !=
                    Math.Sign(actualLeft.siblingIndex - actualRight.siblingIndex))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static Fixture CreateFixture()
    {
        Fixture fixture = new Fixture();
        fixture.allyA = new CharacterData("mode75_A", 30, 10, 10);
        fixture.allyB = new CharacterData("mode75_B", 30, 8, 8);
        fixture.enemy = new CharacterData("mode75_Enemy", 50, 5, 5);
        fixture.attack = BattleCardManager.CreateBattleCard(
            fixture.allyA,
            CreateCard("mode75_attack", CardType.Attack),
            "mode75_attack_instance"
        );
        fixture.defense = BattleCardManager.CreateBattleCard(
            fixture.allyA,
            CreateCard("mode75_defense", CardType.Defense),
            "mode75_defense_instance"
        );
        fixture.dodge = BattleCardManager.CreateBattleCard(
            fixture.allyA,
            CreateCard("mode75_dodge", CardType.Dodge),
            "mode75_dodge_instance"
        );
        BattleCardState enemyAttack = BattleCardManager.CreateBattleCard(
            fixture.enemy,
            CreateCard("mode75_enemy_attack", CardType.Attack),
            "mode75_enemy_attack_instance"
        );
        List<BattleActionSlot> slots =
            BattleActionSlotManager.CreatePartyActionSlots(
                fixture.allyA,
                fixture.allyB,
                2
            );
        fixture.sourceSlot = BattleActionSlotManager.GetSlot(
            slots,
            fixture.allyA,
            1
        );
        fixture.intent = new BattleEnemyIntent(
            "mode75_intent",
            fixture.enemy,
            enemyAttack,
            fixture.allyA,
            1,
            1,
            1
        );
        fixture.runtime = new BattleRuntimeState();
        fixture.runtime.SetCharacters(
            fixture.allyA,
            fixture.allyB,
            fixture.enemy
        );
        fixture.runtime.SetActionSlots(slots);
        fixture.runtime.SetIntentQueue(
            new List<BattleEnemyIntent> { fixture.intent }
        );
        fixture.runtime.SetPhase("Prepare");
        return fixture;
    }

    private static Fixture CreateMultiRelationFixture()
    {
        Fixture fixture = new Fixture();
        fixture.allyA = new CharacterData("mode75_multi_A", 30, 10, 10);
        fixture.allyB = new CharacterData("mode75_multi_B", 30, 8, 8);
        fixture.enemy = new CharacterData("mode75_multi_Enemy1", 50, 5, 5);
        fixture.enemy2 = new CharacterData("mode75_multi_Enemy2", 50, 4, 4);
        fixture.attack = BattleCardManager.CreateBattleCard(
            fixture.allyA,
            CreateCard("mode75_multi_attack_a", CardType.Attack),
            "mode75_multi_attack_a_instance"
        );
        fixture.attack2 = BattleCardManager.CreateBattleCard(
            fixture.allyA,
            CreateCard("mode75_multi_attack_a_2", CardType.Attack),
            "mode75_multi_attack_a_2_instance"
        );
        fixture.allyBAttack = BattleCardManager.CreateBattleCard(
            fixture.allyB,
            CreateCard("mode75_multi_attack_b", CardType.Attack),
            "mode75_multi_attack_b_instance"
        );
        fixture.allyBAttack2 = BattleCardManager.CreateBattleCard(
            fixture.allyB,
            CreateCard("mode75_multi_attack_b_2", CardType.Attack),
            "mode75_multi_attack_b_2_instance"
        );
        BattleCardState enemyAttack = BattleCardManager.CreateBattleCard(
            fixture.enemy,
            CreateCard("mode75_multi_enemy_attack_1", CardType.Attack),
            "mode75_multi_enemy_attack_1_instance"
        );
        BattleCardState enemy2Attack = BattleCardManager.CreateBattleCard(
            fixture.enemy2,
            CreateCard("mode75_multi_enemy_attack_2", CardType.Attack),
            "mode75_multi_enemy_attack_2_instance"
        );
        List<BattleActionSlot> slots =
            BattleActionSlotManager.CreatePartyActionSlots(
                fixture.allyA,
                fixture.allyB,
                2
            );
        fixture.sourceSlot = BattleActionSlotManager.GetSlot(
            slots,
            fixture.allyA,
            1
        );
        fixture.intent = new BattleEnemyIntent(
            "mode75_multi_intent_1",
            fixture.enemy,
            enemyAttack,
            fixture.allyA,
            2,
            1,
            1
        );
        fixture.intent2 = new BattleEnemyIntent(
            "mode75_multi_intent_2",
            fixture.enemy2,
            enemy2Attack,
            fixture.allyB,
            2,
            2,
            1
        );
        fixture.runtime = new BattleRuntimeState();
        fixture.runtime.SetCharacters(
            fixture.allyA,
            fixture.allyB,
            fixture.enemy,
            fixture.enemy2
        );
        fixture.runtime.SetActionSlots(slots);
        fixture.runtime.SetIntentQueue(
            new List<BattleEnemyIntent>
            {
                fixture.intent,
                fixture.intent2
            }
        );
        fixture.runtime.SetPhase("Prepare");
        return fixture;
    }

    private static CardTestData CreateCard(string id, string type)
    {
        return new CardTestData
        {
            cardID = id,
            cardName = id,
            cardType = type,
            minPoint = 1,
            maxPoint = 1,
            cooldown = 0,
            damageFormula = "PointAsDamage"
        };
    }

    private static BattleCardState GetCard(Fixture fixture, string type)
    {
        if (type == CardType.Defense) return fixture.defense;
        if (type == CardType.Dodge) return fixture.dodge;
        return fixture.attack;
    }

    private static BattleCardUIView CreateCardView(
        string name,
        CharacterData owner,
        CharacterData target,
        BattleCardState card,
        BattleCardSelectionController selection
    )
    {
        GameObject cardObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(BattleCardVisualStyle),
            typeof(BattleCardUIView)
        );
        BattleCardUIView view = cardObject.GetComponent<BattleCardUIView>();
        view.BindCard(
            owner,
            card,
            BattleCardUIPreviewBuilder.Build(owner, target, card),
            selection
        );
        return view;
    }

    private static BattleActionSlotUIView CreateSlotView(
        string name,
        CharacterData character,
        int zeroBasedIndex,
        bool enemy,
        Action<BattleActionSlotUIView> click
    )
    {
        GameObject slotObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image),
            typeof(BattleActionSlotUIView)
        );
        BattleActionSlotUIView view =
            slotObject.GetComponent<BattleActionSlotUIView>();
        view.BindInteraction(character, zeroBasedIndex, enemy, click);
        return view;
    }

    private static Display CreateDisplay(Fixture fixture)
    {
        return CreateDisplay(fixture, false);
    }

    private static Display CreateDisplay(
        Fixture fixture,
        bool useExternalPrimaryTemplate
    )
    {
        Display display = new Display();
        display.root = new GameObject(
            "Mode75Display",
            typeof(RectTransform),
            typeof(Canvas)
        );
        Canvas canvas = display.root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        display.texture = new Texture2D(2, 2);
        display.sprite = Sprite.Create(
            display.texture,
            new Rect(0f, 0f, 2f, 2f),
            new Vector2(0.5f, 0.5f)
        );
        display.lineLayer = CreateRect("LineLayer", display.root.transform);
        display.dashedRoot = CreateRect("Dashed", display.lineLayer);
        display.clashRoot = CreateRect("Clash", display.lineLayer);
        display.highlightRoot = CreateRect("Highlight", display.lineLayer);
        display.previewRoot = CreateRect("Preview", display.lineLayer);
        BattleActionRelationUIView template = useExternalPrimaryTemplate
            ? CreateRelationViewWithExternalPrimary(
                display.lineLayer,
                display.sprite,
                out display.externalPrimaryCurve
            )
            : CreateRelationView(display.lineLayer, display.sprite);
        display.relationTemplate = template;
        template.gameObject.SetActive(false);
        display.preview = CreateCurve(
            "PreviewCurve",
            display.previewRoot,
            display.sprite
        );
        display.controller = display.root.AddComponent<
            BattleActionRelationLineController
        >();
        display.controller.ConfigureForTesting(
            display.lineLayer,
            display.dashedRoot,
            display.clashRoot,
            display.highlightRoot,
            display.previewRoot,
            template,
            display.preview,
            canvas
        );
        display.controller.BindRuntimeState(fixture.runtime);
        RegisterSlots(display, fixture);
        display.controller.RefreshRelations();
        return display;
    }

    private static BattleActionRelationUIView
        CreateRelationViewWithExternalPrimary(
            Transform parent,
            Sprite sprite,
            out BattleBezierRelationLineUIView externalPrimary
        )
    {
        externalPrimary = CreateCurve(
            "PrimaryCurve",
            parent,
            sprite
        );
        GameObject value = new GameObject(
            "Mode75ExternalPrimaryRelationView",
            typeof(RectTransform),
            typeof(BattleActionRelationUIView)
        );
        value.transform.SetParent(parent, false);
        BattleActionRelationUIView view =
            value.GetComponent<BattleActionRelationUIView>();
        view.ConfigureForTesting(
            externalPrimary,
            CreateCurve("SecondaryCurve", value.transform, sprite)
        );
        return view;
    }

    private static void RegisterSlots(Display display, Fixture fixture)
    {
        RegisterSlot(display, fixture.allyA, 0, false, new Vector2(-220f, -100f));
        RegisterSlot(display, fixture.allyA, 1, false, new Vector2(-80f, -100f));
        RegisterSlot(display, fixture.allyB, 0, false, new Vector2(80f, -100f));
        RegisterSlot(display, fixture.allyB, 1, false, new Vector2(220f, -100f));
        RegisterSlot(display, fixture.enemy, 0, true, new Vector2(-80f, 140f));
        RegisterSlot(display, fixture.enemy, 1, true, new Vector2(120f, 140f));
        if (fixture.enemy2 != null)
        {
            RegisterSlot(
                display,
                fixture.enemy2,
                0,
                true,
                new Vector2(260f, 140f)
            );
            RegisterSlot(
                display,
                fixture.enemy2,
                1,
                true,
                new Vector2(400f, 140f)
            );
        }
    }

    private static void RegisterSlot(
        Display display,
        CharacterData character,
        int index,
        bool enemy,
        Vector2 position
    )
    {
        BattleActionSlotUIView view = CreateSlotView(
            "Mode75Slot_" + character.characterName + "_" + index,
            character,
            index,
            enemy,
            null
        );
        view.transform.SetParent(display.root.transform, false);
        view.GetComponent<RectTransform>().anchoredPosition = position;
        display.controller.RegisterSlotView(view);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject value = new GameObject(name, typeof(RectTransform));
        value.transform.SetParent(parent, false);
        return value.GetComponent<RectTransform>();
    }

    private static BattleActionRelationUIView CreateRelationView(
        Transform parent,
        Sprite sprite
    )
    {
        GameObject value = new GameObject(
            "Mode75RelationView",
            typeof(RectTransform),
            typeof(BattleActionRelationUIView)
        );
        value.transform.SetParent(parent, false);
        BattleActionRelationUIView view =
            value.GetComponent<BattleActionRelationUIView>();
        view.ConfigureForTesting(
            CreateCurve("Primary", value.transform, sprite),
            CreateCurve("Secondary", value.transform, sprite)
        );
        return view;
    }

    private static BattleBezierRelationLineUIView CreateCurve(
        string name,
        Transform parent,
        Sprite sprite
    )
    {
        GameObject value = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(BattleBezierRelationLineUIView)
        );
        value.transform.SetParent(parent, false);
        GameObject segmentObject = new GameObject(
            "SegmentTemplate",
            typeof(RectTransform),
            typeof(Image)
        );
        segmentObject.transform.SetParent(value.transform, false);
        Image segment = segmentObject.GetComponent<Image>();
        segment.sprite = sprite;
        GameObject arrowObject = new GameObject(
            "Arrow",
            typeof(RectTransform),
            typeof(Image)
        );
        arrowObject.transform.SetParent(value.transform, false);
        Image arrow = arrowObject.GetComponent<Image>();
        arrow.sprite = sprite;
        BattleBezierRelationLineUIView curve =
            value.GetComponent<BattleBezierRelationLineUIView>();
        curve.ConfigureForTesting(
            segment,
            arrow,
            value.GetComponent<CanvasGroup>()
        );
        return curve;
    }

    private static void RenderTestCurve(BattleBezierRelationLineUIView curve)
    {
        curve.Render(
            new Vector2(-180f, -30f),
            new Vector2(0f, 160f),
            new Vector2(180f, 20f),
            Color.cyan,
            true,
            false
        );
    }

    private static void DestroyDisplay(Display display)
    {
        if (display == null) return;
        if (display.root != null)
            UnityEngine.Object.DestroyImmediate(display.root);
        if (display.sprite != null)
            UnityEngine.Object.DestroyImmediate(display.sprite);
        if (display.texture != null)
            UnityEngine.Object.DestroyImmediate(display.texture);
    }

    private static BattleActionRelationDescriptor FindKind(
        IReadOnlyList<BattleActionRelationDescriptor> relations,
        BattleActionRelationKind kind
    )
    {
        for (int index = 0; index < relations.Count; index++)
        {
            if (relations[index].Kind == kind) return relations[index];
        }
        return null;
    }

    private static BattleActionRelationDescriptor FindPlayerRelation(
        IReadOnlyList<BattleActionRelationDescriptor> relations
    )
    {
        for (int index = 0; index < relations.Count; index++)
        {
            if (relations[index].SourceSide == BattleActionRelationSide.Player)
                return relations[index];
        }
        return null;
    }

    private static BattleActionRelationUIView FindHighlighted(
        BattleActionRelationLineController controller
    )
    {
        for (int index = 0; index < controller.VisibleRelationCount; index++)
        {
            BattleActionRelationUIView view = controller.GetVisibleView(index);
            if (view != null && view.IsHighlighted) return view;
        }
        return null;
    }

    private static bool ContainsRelation(
        IReadOnlyList<BattleActionRelationDescriptor> relations,
        string id
    )
    {
        for (int index = 0; index < relations.Count; index++)
        {
            if (relations[index].RelationID == id) return true;
        }
        return false;
    }

    private static bool HasUniqueIDs(
        IReadOnlyList<BattleActionRelationDescriptor> relations
    )
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < relations.Count; index++)
        {
            if (!ids.Add(relations[index].RelationID)) return false;
        }
        return true;
    }

    private static int CountRelations(
        IReadOnlyList<BattleActionRelationDescriptor> relations
    )
    {
        return relations != null ? relations.Count : 0;
    }

    private static bool DoesNotThrow(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("模式75 捕获异常：" + exception);
            return false;
        }
    }

    private static bool Approximately(Vector2 left, Vector2 right)
    {
        return Vector2.Distance(left, right) < 0.01f;
    }
}
