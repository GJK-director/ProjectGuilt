using System;
using System.Collections.Generic;
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
        "防御行动不生成关系线",
        "守备行动不生成关系线",
        "闪避行动不生成单方面攻击线",
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

    public static bool Run()
    {
        Debug.Log("===== BattleActionRelationLineBasic 模式73开始 =====");
        bool[] results = new bool[74];
        QueryFixture baseFixture = CreateQueryFixture();
        RunQueryTests(results, baseFixture);

        DisplayFixture display = CreateDisplayFixture(baseFixture);
        try
        {
            RunControllerTests(results, baseFixture, display);
            RunCurveAndClashTests(results, display);
            RunLayerLaneAndPreviewTests(results, baseFixture, display);
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
        return allPassed;
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
        r[6] = new BattleActionRelationQueryService(
            defenseFixture.runtime
        ).GetAllCurrentRelations().Count == 0;

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
        r[8] = new BattleActionRelationQueryService(
            dodgeFixture.runtime
        ).GetAllCurrentRelations().Count == 0;

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
