using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class BattleActionRelationLineController : MonoBehaviour
{
    [Header("层级")]
    [SerializeField] private RectTransform lineLayer;
    [SerializeField] private RectTransform normalDashedRoot;
    [SerializeField] private RectTransform normalClashRoot;
    [SerializeField] private RectTransform highlightRoot;
    [SerializeField] private RectTransform previewRoot;
    [SerializeField] private BattleActionRelationUIView relationViewTemplate;
    [SerializeField] private BattleBezierRelationLineUIView previewCurve;
    [SerializeField] private CanvasGroup relationLayerCanvasGroup;

    [Header("坐标")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Camera uiCamera;

    [Header("颜色")]
    [SerializeField] private Color playerColor = new Color(0.12f, 0.72f, 1f, 1f);
    [SerializeField] private Color enemyColor = new Color(1f, 0.28f, 0.22f, 1f);

    [Header("普通关系曲线")]
    [SerializeField] private float baseCurveHeight = 130f;
    [SerializeField] private float distanceCurveFactor = 0.12f;
    [SerializeField] private float minCurveHeight = 100f;
    [SerializeField] private float maxCurveHeight = 320f;
    [SerializeField] private float laneSpacing = 28f;
    [SerializeField, Min(0f)]
    [Tooltip("多条单向关系共享同一箭头终点时，沿曲线末端法线分开的间距。")]
    private float sharedArrowEndpointSpacing = 18f;

    [Header("线条尺寸")]
    [SerializeField] private Vector2 segmentSize = new Vector2(12f, 4f);
    [SerializeField] private Vector2 arrowSize = new Vector2(18f, 12f);
    [SerializeField, Min(0f)] private float dashedGap = 8f;
    [SerializeField, Min(0f)] private float solidOverlap = 1f;
    [SerializeField, Min(1f)] private float underlayScale = 1.35f;
    [SerializeField, Min(0f)] private float previewArrowScale = 1f;
    [SerializeField, Min(0f)] private float clashArrowScale = 1f;

    [Header("拼点")]
    [SerializeField, Min(0f)]
    [Tooltip("只控制两支拼点箭头尖端之间的屏幕空间间隔；拼点与普通关系线共用曲线高度。")]
    private float clashArrowGap = 10f;

    [Header("卡牌目标预览")]
    [SerializeField] private float previewCurveHeight = 75f;
    [SerializeField] private float previewDistanceCurveFactor = 0.05f;
    [SerializeField] private float previewMinCurveHeight = 55f;
    [SerializeField] private float previewMaxCurveHeight = 180f;

    private readonly Dictionary<string, RectTransform> slotAnchors =
        new Dictionary<string, RectTransform>();
    private readonly Dictionary<string, BattleActionSlotUIView> slotViews =
        new Dictionary<string, BattleActionSlotUIView>();
    private readonly List<BattleActionRelationUIView> relationViewPool =
        new List<BattleActionRelationUIView>();
    private readonly List<BattleActionRelationUIView> activeViews =
        new List<BattleActionRelationUIView>();
    private readonly List<BattleActionRelationDescriptor> cachedRelations =
        new List<BattleActionRelationDescriptor>();
    private readonly Dictionary<string, int> visibleTargetCounts =
        new Dictionary<string, int>();
    private readonly Dictionary<string, int> visibleTargetOrdinals =
        new Dictionary<string, int>();
    private readonly Dictionary<string, float> endpointOffsetByRelationID =
        new Dictionary<string, float>();
    private readonly HashSet<int> usedViewInstanceIDs = new HashSet<int>();
    private BattleRuntimeState runtimeState;
    private BattleActionRelationQueryService queryService;
    private string hoveredSlotID;
    private string selectedSlotID;
    private string previewSourceSlotID;
    private string previewTargetSlotID;
    private bool revealAllHeld;
    private bool previewActive;
    private Vector2 previewPointerScreenPosition;
    private bool isShuttingDown;
    private bool diagnosticHasCardSelection;
    private string diagnosticSelectedCardID;
    private string diagnosticSelectedActionSlotID;
    private bool diagnosticIsCardTargetingActive;

    public int VisibleRelationCount => activeViews.Count;
    public string HoveredSlotID => hoveredSlotID;
    public string SelectedSlotID => selectedSlotID;
    public bool RevealAllHeld => revealAllHeld;
    public bool PreviewActive => previewActive;
    public string PreviewSourceSlotID => previewSourceSlotID;
    public string PreviewTargetSlotID => previewTargetSlotID;
    public int RelationViewPoolCount => relationViewPool.Count;
    public IReadOnlyList<BattleActionRelationDescriptor> CachedRelations =>
        cachedRelations;
    internal CanvasGroup RelationLayerCanvasGroup =>
        relationLayerCanvasGroup;
    internal bool IsShuttingDown => isShuttingDown;

    private void Awake()
    {
        isShuttingDown = false;
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }
        if (relationViewTemplate != null)
        {
            relationViewTemplate.gameObject.SetActive(false);
            relationViewTemplate.PrepareForReuse();
        }
        EnsureRelationLayerRaycastSafety();
        ApplyPreviewVisualSettings();
        previewCurve?.Clear();
    }

    private void OnEnable()
    {
        isShuttingDown = false;
        EnsureRelationLayerRaycastSafety();
        ApplyPreviewVisualSettings();
    }

    private void Update()
    {
        if (!IsPlanningState())
        {
            if (activeViews.Count > 0 || previewActive || revealAllHeld)
            {
                ClearAll();
            }
            return;
        }

        bool tabHeld = IsRevealAllInputHeld(Keyboard.current);
        if (tabHeld != revealAllHeld)
        {
            SetRevealAllHeld(tabHeld);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 仅在按住 Tab 时响应一次 F8，避免开发诊断在 Update 中持续刷日志。
        if (revealAllHeld && IsRelationDiagnosticsInputPressed(Keyboard.current))
        {
            LogCurrentRelationDiagnostics();
        }
#endif

        if (previewActive)
        {
            Vector2 pointerScreenPosition;
            if (TryReadPointerScreenPosition(out pointerScreenPosition))
            {
                UpdateCardTargetingPointer(pointerScreenPosition);
            }
        }
    }

    public void BindRuntimeState(BattleRuntimeState state)
    {
        runtimeState = state;
        if (queryService == null)
        {
            queryService = new BattleActionRelationQueryService(state);
        }
        else
        {
            queryService.SetRuntimeState(state);
        }
        RefreshRelations();
    }

    public void RegisterSlotView(BattleActionSlotUIView slotView)
    {
        if (slotView == null || queryService == null)
        {
            return;
        }
        string slotID = queryService.GetSlotID(
            slotView.BoundCharacter,
            slotView.FormalSlotIndex
        );
        if (string.IsNullOrEmpty(slotID))
        {
            return;
        }
        slotAnchors[slotID] = slotView.RelationLineAnchor;
        slotViews[slotID] = slotView;
    }

    public void RegisterSlotViews(
        IEnumerable<BattleActionSlotUIView> slotViews
    )
    {
        if (slotViews == null)
        {
            return;
        }
        foreach (BattleActionSlotUIView slotView in slotViews)
        {
            RegisterSlotView(slotView);
        }
    }

    public void UnregisterSlotView(BattleActionSlotUIView slotView)
    {
        if (slotView == null || queryService == null)
        {
            return;
        }

        string slotID = GetSlotID(slotView);
        BattleActionSlotUIView registeredView;
        if (string.IsNullOrEmpty(slotID) ||
            !slotViews.TryGetValue(slotID, out registeredView) ||
            !object.ReferenceEquals(registeredView, slotView))
        {
            return;
        }

        slotViews.Remove(slotID);
        slotAnchors.Remove(slotID);
        if (hoveredSlotID == slotID)
        {
            hoveredSlotID = string.Empty;
        }
        if (selectedSlotID == slotID)
        {
            selectedSlotID = string.Empty;
        }
    }

    public void UnregisterSlotViews(
        IEnumerable<BattleActionSlotUIView> views
    )
    {
        if (views == null)
        {
            return;
        }

        foreach (BattleActionSlotUIView view in views)
        {
            UnregisterSlotView(view);
        }

        if (!isShuttingDown)
        {
            RefreshRelations();
        }
    }

    public string GetSlotID(BattleActionSlotUIView slotView)
    {
        return slotView != null && queryService != null
            ? queryService.GetSlotID(
                slotView.BoundCharacter,
                slotView.FormalSlotIndex
            )
            : string.Empty;
    }

    public bool IsSlotViewRegistered(BattleActionSlotUIView slotView)
    {
        string slotID = GetSlotID(slotView);
        BattleActionSlotUIView registeredView;
        return !string.IsNullOrEmpty(slotID) &&
            slotViews.TryGetValue(slotID, out registeredView) &&
            object.ReferenceEquals(registeredView, slotView);
    }

    public void RefreshRelations()
    {
        if (isShuttingDown)
        {
            return;
        }
        RecycleActiveViews();
        cachedRelations.Clear();
        if (!IsPlanningState() || queryService == null)
        {
            selectedSlotID = string.Empty;
            EndCardTargetingPreview();
            return;
        }

        IReadOnlyList<BattleActionRelationDescriptor> relations =
            queryService.GetAllCurrentRelations();
        for (int index = 0; index < relations.Count; index++)
        {
            cachedRelations.Add(relations[index]);
        }
        RefreshVisibleRelations();
    }

    public void SetHoveredSlot(string slotID)
    {
        if (isShuttingDown || !IsPlanningState())
        {
            return;
        }
        hoveredSlotID = slotID ?? string.Empty;
        previewTargetSlotID = previewActive
            ? hoveredSlotID
            : string.Empty;
        RefreshVisibleRelations();
    }

    public void ClearHoveredSlot(string expectedSlotID)
    {
        if (!string.IsNullOrEmpty(expectedSlotID) &&
            hoveredSlotID != expectedSlotID)
        {
            return;
        }
        hoveredSlotID = string.Empty;
        previewTargetSlotID = string.Empty;
        RefreshVisibleRelations();
    }

    public void SetSelectedSlot(BattleActionSlotUIView slotView)
    {
        SetSelectedSlot(GetSlotID(slotView));
    }

    public void SetSelectedSlot(string slotID)
    {
        if (isShuttingDown || !IsPlanningState())
        {
            return;
        }
        selectedSlotID = slotID ?? string.Empty;
        RefreshVisibleRelations();
    }

    public void ClearSelectedSlot()
    {
        selectedSlotID = string.Empty;
        if (!isShuttingDown)
        {
            RefreshVisibleRelations();
        }
    }

    public void SetRevealAllHeld(bool held)
    {
        if (isShuttingDown)
        {
            revealAllHeld = false;
            return;
        }
        revealAllHeld = IsPlanningState() && held;
        RefreshVisibleRelations();
    }

    [ContextMenu("输出当前真实关系线诊断")]
    public void LogCurrentRelationDiagnostics()
    {
        StringBuilder builder = new StringBuilder(16384);
        Dictionary<string, int> expectedViewCounts =
            new Dictionary<string, int>();
        Dictionary<string, int> activeViewCounts =
            new Dictionary<string, int>();
        Dictionary<int, string> viewOwners =
            new Dictionary<int, string>();
        Dictionary<int, string> curveOwners =
            new Dictionary<int, string>();
        Dictionary<int, string> arrowOwners =
            new Dictionary<int, string>();
        List<string> conflicts = new List<string>();
        int expectedVisibleCount = 0;

        builder.AppendLine("[关系线真实场景诊断]");
        builder.Append("Controller=").Append(name)
            .Append(" InstanceID=").Append(GetInstanceID())
            .Append(" Planning=").Append(IsPlanningState())
            .Append(" RevealAllHeld=").Append(revealAllHeld)
            .Append(" HoveredSlotID=").Append(hoveredSlotID)
            .Append(" SelectedSlotID=").Append(selectedSlotID)
            .AppendLine();
        builder.Append("PreviewActive=").Append(previewActive)
            .Append(" PreviewSourceSlotID=").Append(previewSourceSlotID)
            .Append(" PreviewTargetSlotID=").Append(previewTargetSlotID)
            .Append(" HasCardSelection=").Append(diagnosticHasCardSelection)
            .Append(" SelectedCardID=").Append(diagnosticSelectedCardID)
            .Append(" SelectedActionSlotID=")
            .Append(diagnosticSelectedActionSlotID)
            .Append(" IsCardTargetingActive=")
            .Append(diagnosticIsCardTargetingActive)
            .AppendLine();
        builder.Append("cachedRelations数量=")
            .Append(cachedRelations.Count)
            .AppendLine();

        for (int index = 0; index < cachedRelations.Count; index++)
        {
            BattleActionRelationDescriptor relation = cachedRelations[index];
            if (relation == null)
            {
                builder.Append("[Descriptor ").Append(index)
                    .AppendLine("] null");
                continue;
            }

            bool shouldDisplay = ShouldDisplayRelation(relation);
            builder.Append("[Descriptor ").Append(index).Append("] ")
                .Append("RelationID=").Append(relation.RelationID)
                .Append(" RelationKind=").Append(relation.Kind)
                .Append(" SourceSlotID=").Append(relation.SourceSlotID)
                .Append(" TargetSlotID=").Append(relation.TargetSlotID)
                .Append(" SourceSide=").Append(relation.SourceSide)
                .Append(" ShouldDisplay=").Append(shouldDisplay)
                .AppendLine();

            if (!shouldDisplay)
            {
                continue;
            }

            expectedVisibleCount++;
            IncrementRelationCount(expectedViewCounts, relation.RelationID);
        }

        builder.Append("应显示关系数量=").Append(expectedVisibleCount)
            .AppendLine();
        builder.Append("activeViews数量=").Append(activeViews.Count)
            .AppendLine();

        for (int index = 0; index < activeViews.Count; index++)
        {
            BattleActionRelationUIView view = activeViews[index];
            if (view == null)
            {
                builder.Append("[ActiveView ").Append(index)
                    .AppendLine("] null");
                conflicts.Add("activeViews中存在null引用，索引=" + index);
                continue;
            }

            string relationID = view.RelationID ?? string.Empty;
            int viewInstanceID = view.GetInstanceID();
            string rootName = GetTransformPath(view.transform.parent);
            builder.Append("[ActiveView ").Append(index).Append("] ")
                .Append("RelationID=").Append(relationID)
                .Append(" ViewInstanceID=").Append(viewInstanceID)
                .Append(" Root=").Append(rootName)
                .Append(" SiblingIndex=").Append(view.transform.GetSiblingIndex())
                .Append(" OwnsPrimaryCurve=").Append(view.OwnsPrimaryCurve)
                .Append(" OwnsSecondaryCurve=").Append(view.OwnsSecondaryCurve)
                .AppendLine();

            IncrementRelationCount(activeViewCounts, relationID);
            RegisterOwnedInstance(
                viewOwners,
                viewInstanceID,
                relationID,
                "View",
                conflicts
            );
            AppendCurveDiagnostics(
                builder,
                view.PrimaryCurve,
                "PrimaryCurve",
                relationID,
                curveOwners,
                arrowOwners,
                conflicts
            );
            AppendCurveDiagnostics(
                builder,
                view.SecondaryCurve,
                "SecondaryCurve",
                relationID,
                curveOwners,
                arrowOwners,
                conflicts
            );
        }

        int missingViewCount = 0;
        foreach (KeyValuePair<string, int> expected in expectedViewCounts)
        {
            int actualCount;
            activeViewCounts.TryGetValue(expected.Key, out actualCount);
            if (actualCount >= expected.Value)
            {
                continue;
            }

            int missingCount = expected.Value - actualCount;
            missingViewCount += missingCount;
            builder.Append("[缺失] Descriptor RelationID=")
                .Append(expected.Key)
                .Append(" 缺少activeView数量=").Append(missingCount)
                .AppendLine();
        }

        foreach (KeyValuePair<string, int> active in activeViewCounts)
        {
            int expectedCount;
            expectedViewCounts.TryGetValue(active.Key, out expectedCount);
            if (active.Value <= expectedCount)
            {
                continue;
            }

            conflicts.Add(
                "RelationID=" + active.Key +
                " 的activeView数量多于应显示Descriptor数量：" +
                active.Value + "/" + expectedCount
            );
        }

        builder.Append("Descriptor无对应activeView数量=")
            .Append(missingViewCount).AppendLine();
        builder.Append("实例分配冲突数量=").Append(conflicts.Count)
            .AppendLine();
        for (int index = 0; index < conflicts.Count; index++)
        {
            builder.Append("[冲突 ").Append(index).Append("] ")
                .Append(conflicts[index]).AppendLine();
        }
        AppendHierarchyVisualDiagnostics(builder);
        builder.AppendLine("[关系线真实场景诊断结束]");
        Debug.Log(builder.ToString(), this);
    }

    private void AppendHierarchyVisualDiagnostics(StringBuilder builder)
    {
        builder.AppendLine("[RelationLineLayer全层级视觉扫描]");
        if (lineLayer == null)
        {
            builder.AppendLine("LineLayer=null，无法扫描层级视觉。");
            return;
        }

        BattleActionRelationUIView[] hierarchyViews =
            lineLayer.GetComponentsInChildren<BattleActionRelationUIView>(true);
        BattleBezierRelationLineUIView[] hierarchyCurves =
            lineLayer.GetComponentsInChildren<BattleBezierRelationLineUIView>(true);
        Image[] hierarchyImages = lineLayer.GetComponentsInChildren<Image>(true);

        builder.Append("HierarchyRelationView数量=")
            .Append(hierarchyViews.Length)
            .Append(" HierarchyCurve数量=").Append(hierarchyCurves.Length)
            .Append(" HierarchyImage数量=").Append(hierarchyImages.Length)
            .AppendLine();

        List<BattleActionRelationUIView> untrackedVisibleViews =
            new List<BattleActionRelationUIView>();
        List<BattleBezierRelationLineUIView> untrackedVisibleCurves =
            new List<BattleBezierRelationLineUIView>();
        List<BattleBezierRelationLineUIView> layerCountMismatches =
            new List<BattleBezierRelationLineUIView>();

        builder.AppendLine("[全部RelationView]");
        for (int index = 0; index < hierarchyViews.Length; index++)
        {
            BattleActionRelationUIView view = hierarchyViews[index];
            if (view == null)
            {
                builder.Append("[HierarchyView ").Append(index)
                    .AppendLine("] null");
                continue;
            }

            int activeIndex = IndexOfView(activeViews, view);
            int poolIndex = IndexOfView(relationViewPool, view);
            bool isTrackedActive = activeIndex >= 0;
            bool isPooled = poolIndex >= 0;
            bool hasVisibleVisuals = HasVisibleVisuals(view.PrimaryCurve) ||
                HasVisibleVisuals(view.SecondaryCurve);
            if (!isTrackedActive && hasVisibleVisuals)
            {
                untrackedVisibleViews.Add(view);
            }

            builder.Append("[HierarchyView ").Append(index).Append("] ")
                .Append("ViewInstanceID=").Append(view.GetInstanceID())
                .Append(" Path=").Append(GetTransformPath(view.transform))
                .Append(" activeSelf=").Append(view.gameObject.activeSelf)
                .Append(" activeInHierarchy=")
                .Append(view.gameObject.activeInHierarchy)
                .Append(" RelationID=").Append(view.RelationID)
                .Append(" LastDisplayType=").Append(view.Kind)
                .Append(" IsTrackedActiveView=").Append(isTrackedActive)
                .Append(" ActiveViewIndex=").Append(activeIndex)
                .Append(" IsPooledView=").Append(isPooled)
                .Append(" PoolIndex=").Append(poolIndex)
                .Append(" IsTemplate=")
                .Append(object.ReferenceEquals(view, relationViewTemplate))
                .Append(" RootType=").Append(GetRootType(view.transform))
                .Append(" HasVisibleVisuals=").Append(hasVisibleVisuals)
                .AppendLine();
            AppendHierarchyCurveSummary(
                builder,
                "PrimaryCurve",
                view.PrimaryCurve
            );
            AppendHierarchyCurveSummary(
                builder,
                "SecondaryCurve",
                view.SecondaryCurve
            );
        }

        builder.AppendLine("[全部Curve]");
        for (int index = 0; index < hierarchyCurves.Length; index++)
        {
            BattleBezierRelationLineUIView curve = hierarchyCurves[index];
            if (curve == null)
            {
                builder.Append("[HierarchyCurve ").Append(index)
                    .AppendLine("] null");
                continue;
            }

            BattleActionRelationUIView ownerView = FindOwningView(
                curve,
                hierarchyViews
            );
            int activeIndex = IndexOfView(activeViews, ownerView);
            int poolIndex = IndexOfView(relationViewPool, ownerView);
            bool belongsToActiveView = activeIndex >= 0;
            bool belongsToPooledView = poolIndex >= 0;
            bool belongsToPreview = object.ReferenceEquals(curve, previewCurve);
            bool belongsToTemplate = IsTemplateCurve(curve);
            bool isOrphan = ownerView == null &&
                !belongsToPreview && !belongsToTemplate;
            bool hasVisibleVisuals = HasVisibleVisuals(curve);
            if (curve.ActiveUnderlaySegmentCount > 0 &&
                curve.ActiveMainSegmentCount !=
                    curve.ActiveUnderlaySegmentCount)
            {
                layerCountMismatches.Add(curve);
            }
            if (!belongsToActiveView && hasVisibleVisuals)
            {
                untrackedVisibleCurves.Add(curve);
            }

            builder.Append("[HierarchyCurve ").Append(index).Append("] ")
                .Append("CurveInstanceID=").Append(curve.GetInstanceID())
                .Append(" Path=").Append(GetTransformPath(curve.transform))
                .Append(" activeSelf=").Append(curve.gameObject.activeSelf)
                .Append(" activeInHierarchy=")
                .Append(curve.gameObject.activeInHierarchy)
                .Append(" ActiveSegmentCount=")
                .Append(curve.ActiveSegmentCount)
                .Append(" ActiveMainSegmentCount=")
                .Append(curve.ActiveMainSegmentCount)
                .Append(" ActiveUnderlaySegmentCount=")
                .Append(curve.ActiveUnderlaySegmentCount)
                .Append(" MainSegmentPoolCount=")
                .Append(curve.MainSegmentPoolCount)
                .Append(" UnderlaySegmentPoolCount=")
                .Append(curve.UnderlaySegmentPoolCount)
                .Append(" ArrowInstanceID=").Append(curve.ArrowInstanceID)
                .Append(" ArrowActiveSelf=").Append(curve.ArrowActiveSelf)
                .Append(" ArrowAlpha=").Append(curve.ArrowAlpha)
                .Append(" HasVisibleMainArrow=")
                .Append(curve.HasVisibleMainArrow)
                .Append(" HasVisibleUnderlayArrow=")
                .Append(curve.HasVisibleUnderlayArrow)
                .Append(" CurrentLineStyle=").Append(curve.CurrentLineStyle)
                .Append(" PreviousLineStyle=").Append(curve.PreviousLineStyle)
                .Append(" CurrentRange=").Append(curve.CurrentRange)
                .Append(" PreviousRange=").Append(curve.PreviousRange)
                .Append(" RangeStart=").Append(curve.RangeStart)
                .Append(" RangeEnd=").Append(curve.RangeEnd)
                .Append(" ArrowTip=").Append(curve.ArrowTip)
                .Append(" OwnerViewInstanceID=")
                .Append(ownerView != null ? ownerView.GetInstanceID() : 0)
                .Append(" OwnerRelationID=")
                .Append(ownerView != null ? ownerView.RelationID : string.Empty)
                .Append(" BelongsToPreviewCurve=").Append(belongsToPreview)
                .Append(" BelongsToActiveView=").Append(belongsToActiveView)
                .Append(" ActiveViewIndex=").Append(activeIndex)
                .Append(" BelongsToPooledView=").Append(belongsToPooledView)
                .Append(" PoolIndex=").Append(poolIndex)
                .Append(" BelongsToTemplate=").Append(belongsToTemplate)
                .Append(" IsOrphanCurve=").Append(isOrphan)
                .Append(" HasVisibleVisuals=").Append(hasVisibleVisuals)
                .AppendLine();
        }

        AppendLayerCountMismatchDiagnostics(builder, layerCountMismatches);
        AppendUntrackedVisibleViewDiagnostics(builder, untrackedVisibleViews);
        AppendUntrackedVisibleCurveDiagnostics(builder, untrackedVisibleCurves);
        AppendPreviewCurveDiagnostics(builder);
        AppendPoolDiagnostics(builder);
        AppendHierarchyImageDiagnostics(
            builder,
            hierarchyImages,
            hierarchyCurves,
            hierarchyViews
        );
    }

    private static void AppendLayerCountMismatchDiagnostics(
        StringBuilder builder,
        List<BattleBezierRelationLineUIView> curves
    )
    {
        builder.AppendLine("[同一Curve主层与底层数量不一致]");
        builder.Append("数量=").Append(curves.Count).AppendLine();
        for (int index = 0; index < curves.Count; index++)
        {
            BattleBezierRelationLineUIView curve = curves[index];
            builder.Append("[").Append(index).Append("] ")
                .Append("CurveInstanceID=").Append(curve.GetInstanceID())
                .Append(" MainActive=")
                .Append(curve.ActiveMainSegmentCount)
                .Append(" UnderlayActive=")
                .Append(curve.ActiveUnderlaySegmentCount)
                .Append(" CurrentLineStyle=").Append(curve.CurrentLineStyle)
                .Append(" PreviousLineStyle=").Append(curve.PreviousLineStyle)
                .Append(" CurrentRange=").Append(curve.CurrentRange)
                .Append(" PreviousRange=").Append(curve.PreviousRange)
                .AppendLine();
        }
    }

    private void AppendUntrackedVisibleViewDiagnostics(
        StringBuilder builder,
        List<BattleActionRelationUIView> views
    )
    {
        builder.AppendLine("[未追踪但可见的RelationView]");
        builder.Append("数量=").Append(views.Count).AppendLine();
        for (int index = 0; index < views.Count; index++)
        {
            BattleActionRelationUIView view = views[index];
            builder.Append("[").Append(index).Append("] ")
                .Append("ViewInstanceID=").Append(view.GetInstanceID())
                .Append(" Path=").Append(GetTransformPath(view.transform))
                .Append(" RelationID=").Append(view.RelationID)
                .Append(" LastDisplayType=").Append(view.Kind)
                .Append(" PoolIndex=")
                .Append(IndexOfView(relationViewPool, view))
                .Append(" PrimarySegments=")
                .Append(GetActiveSegmentCount(view.PrimaryCurve))
                .Append(" PrimaryArrowVisible=")
                .Append(IsArrowActuallyVisible(view.PrimaryCurve))
                .Append(" SecondarySegments=")
                .Append(GetActiveSegmentCount(view.SecondaryCurve))
                .Append(" SecondaryArrowVisible=")
                .Append(IsArrowActuallyVisible(view.SecondaryCurve))
                .AppendLine();
        }
    }

    private void AppendUntrackedVisibleCurveDiagnostics(
        StringBuilder builder,
        List<BattleBezierRelationLineUIView> curves
    )
    {
        builder.AppendLine("[未追踪但可见的Curve]");
        builder.Append("数量=").Append(curves.Count).AppendLine();
        for (int index = 0; index < curves.Count; index++)
        {
            BattleBezierRelationLineUIView curve = curves[index];
            builder.Append("[").Append(index).Append("] ")
                .Append("CurveInstanceID=").Append(curve.GetInstanceID())
                .Append(" Path=").Append(GetTransformPath(curve.transform))
                .Append(" activeInHierarchy=")
                .Append(curve.gameObject.activeInHierarchy)
                .Append(" ActiveSegmentCount=")
                .Append(curve.ActiveSegmentCount)
                .Append(" ArrowActiveSelf=").Append(curve.ArrowActiveSelf)
                .Append(" ArrowAlpha=").Append(curve.ArrowAlpha)
                .AppendLine();
        }
    }

    private void AppendPreviewCurveDiagnostics(StringBuilder builder)
    {
        builder.AppendLine("[Preview视觉状态]");
        if (previewCurve == null)
        {
            builder.AppendLine("previewCurve=null");
            return;
        }

        builder.Append("PreviewActive=").Append(previewActive)
            .Append(" CurveInstanceID=").Append(previewCurve.GetInstanceID())
            .Append(" Path=").Append(GetTransformPath(previewCurve.transform))
            .Append(" activeSelf=").Append(previewCurve.gameObject.activeSelf)
            .Append(" activeInHierarchy=")
            .Append(previewCurve.gameObject.activeInHierarchy)
            .Append(" ActiveSegmentCount=")
            .Append(previewCurve.ActiveSegmentCount)
            .Append(" ActiveMainSegmentCount=")
            .Append(previewCurve.ActiveMainSegmentCount)
            .Append(" ActiveUnderlaySegmentCount=")
            .Append(previewCurve.ActiveUnderlaySegmentCount)
            .Append(" MainSegmentPoolCount=")
            .Append(previewCurve.MainSegmentPoolCount)
            .Append(" UnderlaySegmentPoolCount=")
            .Append(previewCurve.UnderlaySegmentPoolCount)
            .Append(" ArrowInstanceID=").Append(previewCurve.ArrowInstanceID)
            .Append(" ArrowActiveSelf=").Append(previewCurve.ArrowActiveSelf)
            .Append(" ArrowAlpha=").Append(previewCurve.ArrowAlpha)
            .Append(" HasVisibleMainArrow=")
            .Append(previewCurve.HasVisibleMainArrow)
            .Append(" HasVisibleUnderlayArrow=")
            .Append(previewCurve.HasVisibleUnderlayArrow)
            .Append(" CurrentLineStyle=")
            .Append(previewCurve.CurrentLineStyle)
            .Append(" PreviousLineStyle=")
            .Append(previewCurve.PreviousLineStyle)
            .Append(" CurrentRange=").Append(previewCurve.CurrentRange)
            .Append(" PreviousRange=").Append(previewCurve.PreviousRange)
            .Append(" RangeStart=").Append(previewCurve.RangeStart)
            .Append(" RangeEnd=").Append(previewCurve.RangeEnd)
            .Append(" ArrowTip=").Append(previewCurve.ArrowTip)
            .AppendLine();
    }

    private void AppendPoolDiagnostics(StringBuilder builder)
    {
        builder.AppendLine("[对象池状态]");
        builder.Append("relationViewPool总数量=")
            .Append(relationViewPool.Count)
            .Append(" activeViews数量=").Append(activeViews.Count)
            .AppendLine();
        for (int index = 0; index < relationViewPool.Count; index++)
        {
            BattleActionRelationUIView view = relationViewPool[index];
            if (view == null)
            {
                builder.Append("[PoolView ").Append(index)
                    .AppendLine("] null");
                continue;
            }

            builder.Append("[PoolView ").Append(index).Append("] ")
                .Append("ViewInstanceID=").Append(view.GetInstanceID())
                .Append(" InActiveViews=")
                .Append(IndexOfView(activeViews, view) >= 0)
                .Append(" RelationID=").Append(view.RelationID)
                .Append(" LastDisplayType=").Append(view.Kind)
                .Append(" activeSelf=").Append(view.gameObject.activeSelf)
                .Append(" activeInHierarchy=")
                .Append(view.gameObject.activeInHierarchy)
                .Append(" PrimaryActiveSegmentCount=")
                .Append(GetActiveSegmentCount(view.PrimaryCurve))
                .Append(" PrimaryMainActive=")
                .Append(view.PrimaryCurve != null
                    ? view.PrimaryCurve.ActiveMainSegmentCount
                    : 0)
                .Append(" PrimaryUnderlayActive=")
                .Append(view.PrimaryCurve != null
                    ? view.PrimaryCurve.ActiveUnderlaySegmentCount
                    : 0)
                .Append(" SecondaryActiveSegmentCount=")
                .Append(GetActiveSegmentCount(view.SecondaryCurve))
                .Append(" SecondaryMainActive=")
                .Append(view.SecondaryCurve != null
                    ? view.SecondaryCurve.ActiveMainSegmentCount
                    : 0)
                .Append(" SecondaryUnderlayActive=")
                .Append(view.SecondaryCurve != null
                    ? view.SecondaryCurve.ActiveUnderlaySegmentCount
                    : 0)
                .Append(" RootPath=")
                .Append(GetTransformPath(view.transform.parent))
                .AppendLine();
        }
    }

    private void AppendHierarchyImageDiagnostics(
        StringBuilder builder,
        Image[] images,
        BattleBezierRelationLineUIView[] curves,
        BattleActionRelationUIView[] views
    )
    {
        builder.AppendLine("[全部Arrow与Segment Image]");
        for (int index = 0; index < images.Length; index++)
        {
            Image image = images[index];
            if (image == null)
            {
                builder.Append("[Image ").Append(index).AppendLine("] null");
                continue;
            }

            BattleBezierRelationLineUIView ownerCurve = FindOwningCurve(
                image,
                curves
            );
            BattleActionRelationUIView ownerView = FindOwningView(
                ownerCurve,
                views
            );
            builder.Append("[Image ").Append(index).Append("] ")
                .Append("InstanceID=").Append(image.GetInstanceID())
                .Append(" Type=").Append(GetRelationImageType(image, ownerCurve))
                .Append(" Path=").Append(GetTransformPath(image.transform))
                .Append(" activeSelf=").Append(image.gameObject.activeSelf)
                .Append(" activeInHierarchy=")
                .Append(image.gameObject.activeInHierarchy)
                .Append(" Alpha=").Append(image.color.a)
                .Append(" OwnerCurveInstanceID=")
                .Append(ownerCurve != null ? ownerCurve.GetInstanceID() : 0)
                .Append(" OwnerViewInstanceID=")
                .Append(ownerView != null ? ownerView.GetInstanceID() : 0)
                .Append(" OwnerRelationID=")
                .Append(ownerView != null ? ownerView.RelationID : string.Empty)
                .Append(" OwnerIsActiveView=")
                .Append(IndexOfView(activeViews, ownerView) >= 0)
                .Append(" OwnerPoolIndex=")
                .Append(IndexOfView(relationViewPool, ownerView))
                .AppendLine();
        }
    }

    public bool BeginCardTargetingPreview(string sourceSlotID)
    {
        if (isShuttingDown || !IsPlanningState() || previewCurve == null ||
            string.IsNullOrEmpty(sourceSlotID) ||
            !slotAnchors.ContainsKey(sourceSlotID))
        {
            EndCardTargetingPreview();
            return false;
        }
        previewSourceSlotID = sourceSlotID;
        previewTargetSlotID = hoveredSlotID ?? string.Empty;
        previewActive = true;
        if (previewRoot != null &&
            previewCurve.transform.parent != previewRoot)
        {
            previewCurve.transform.SetParent(previewRoot, false);
        }
        Vector2 pointerScreenPosition;
        if (TryReadPointerScreenPosition(out pointerScreenPosition))
        {
            UpdateCardTargetingPointer(pointerScreenPosition);
        }
        return true;
    }

    public void UpdateCardTargetingPointer(Vector2 screenPosition)
    {
        previewPointerScreenPosition = screenPosition;
        if (!previewActive || !IsPlanningState() ||
            previewCurve == null || lineLayer == null)
        {
            return;
        }

        RectTransform sourceAnchor;
        Vector2 start;
        Vector2 end;
        if (!slotAnchors.TryGetValue(
                previewSourceSlotID,
                out sourceAnchor) ||
            !TryConvertRectAnchorToLayerLocal(
                sourceAnchor,
                lineLayer,
                targetCanvas,
                uiCamera,
                out start) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                lineLayer,
                screenPosition,
                GetEventCamera(targetCanvas, uiCamera),
                out end))
        {
            previewCurve.Clear();
            return;
        }

        Vector2 control =
            BattleBezierRelationLineUIView.ResolveControlPoint(
                start,
                end,
                previewCurveHeight,
                previewDistanceCurveFactor,
                previewMinCurveHeight,
                previewMaxCurveHeight,
                0f
            );
        ApplyPreviewVisualSettings();
        previewCurve.Render(
            start,
            control,
            end,
            playerColor,
            true,
            true
        );
    }

    public void EndCardTargetingPreview()
    {
        previewActive = false;
        previewSourceSlotID = string.Empty;
        previewTargetSlotID = string.Empty;
        if (previewCurve != null)
        {
            previewCurve.Clear();
        }
    }

    public void SetCardTargetingDiagnosticState(
        bool hasCardSelection,
        string selectedCardID,
        string selectedActionSlotID,
        bool isCardTargetingActive
    )
    {
        diagnosticHasCardSelection = hasCardSelection;
        diagnosticSelectedCardID = selectedCardID ?? string.Empty;
        diagnosticSelectedActionSlotID =
            selectedActionSlotID ?? string.Empty;
        diagnosticIsCardTargetingActive = isCardTargetingActive;
    }

    public void ClearAll()
    {
        RecycleActiveViews();
        cachedRelations.Clear();
        hoveredSlotID = string.Empty;
        selectedSlotID = string.Empty;
        revealAllHeld = false;
        EndCardTargetingPreview();
        SetCardTargetingDiagnosticState(false, string.Empty, string.Empty, false);
    }

    [ContextMenu("验证行动关系线配置")]
    public void ValidateConfiguration()
    {
        bool valid = true;
        valid &= ValidateReference(lineLayer, "Line Layer");
        valid &= ValidateReference(normalDashedRoot, "NormalDashedRoot");
        valid &= ValidateReference(normalClashRoot, "NormalClashRoot");
        valid &= ValidateReference(highlightRoot, "HighlightRoot");
        valid &= ValidateReference(previewRoot, "PreviewRoot");
        if (relationViewTemplate == null)
        {
            Debug.LogWarning("行动关系线缺少 Relation View Template。", this);
            valid = false;
        }
        else
        {
            valid &= relationViewTemplate.ValidateConfiguration();
        }
        if (previewCurve == null)
        {
            Debug.LogWarning("行动关系线缺少 Preview Curve。", this);
            valid = false;
        }
        else
        {
            valid &= previewCurve.ValidateConfiguration("PreviewCurve");
        }
        if (targetCanvas == null && GetComponentInParent<Canvas>() == null)
        {
            Debug.LogWarning("行动关系线找不到 Canvas。", this);
            valid = false;
        }
        valid &= ValidateRootParent(normalDashedRoot, "NormalDashedRoot");
        valid &= ValidateRootParent(normalClashRoot, "NormalClashRoot");
        valid &= ValidateRootParent(highlightRoot, "HighlightRoot");
        valid &= ValidateRootParent(previewRoot, "PreviewRoot");
        if (normalDashedRoot != null && normalClashRoot != null &&
            highlightRoot != null && previewRoot != null &&
            !(normalDashedRoot.GetSiblingIndex() <
                normalClashRoot.GetSiblingIndex() &&
              normalClashRoot.GetSiblingIndex() <
                highlightRoot.GetSiblingIndex() &&
              highlightRoot.GetSiblingIndex() <
                previewRoot.GetSiblingIndex()))
        {
            Debug.LogWarning(
                "关系层顺序应为 NormalDashed < NormalClash < Highlight < Preview。",
                this
            );
            valid = false;
        }
        foreach (KeyValuePair<string, RectTransform> pair in slotAnchors)
        {
            if (pair.Value == null)
            {
                Debug.LogWarning(pair.Key + " 缺少 Relation Line Anchor。", this);
                valid = false;
            }
        }
        foreach (KeyValuePair<string, BattleActionSlotUIView> pair in slotViews)
        {
            BattleActionSlotUIView slotView = pair.Value;
            if (slotView == null)
            {
                continue;
            }
            if (!slotView.HasExplicitRelationLineAnchor)
            {
                Debug.LogWarning(
                    pair.Key +
                    " 未绑定专用 Relation Line Anchor，将回退到槽位中心。",
                    slotView
                );
                valid = false;
            }
            if (slotView.GetComponent<
                    BattleActionSlotRelationHoverRelay>() == null)
            {
                Debug.LogWarning(
                    pair.Key + " 缺少 Hover Relay。",
                    slotView
                );
                valid = false;
            }
        }
        Debug.Log(valid
            ? "行动关系线配置验证通过。"
            : "行动关系线配置不完整，请按以上日志检查。", this);
    }

    public static bool TryConvertRectAnchorToLayerLocal(
        RectTransform sourceAnchor,
        RectTransform targetLineLayer,
        Canvas canvas,
        Camera camera,
        out Vector2 localPoint
    )
    {
        localPoint = Vector2.zero;
        if (sourceAnchor == null || targetLineLayer == null)
        {
            return false;
        }
        Camera eventCamera = GetEventCamera(canvas, camera);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            eventCamera,
            sourceAnchor.TransformPoint(sourceAnchor.rect.center)
        );
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetLineLayer,
            screenPoint,
            eventCamera,
            out localPoint
        );
    }

    internal Vector2 PreviewPointerScreenPosition =>
        previewPointerScreenPosition;

    internal static bool IsRevealAllInputHeld(Keyboard keyboard)
    {
        return keyboard != null && keyboard.tabKey.isPressed;
    }

    internal void ConfigureForTesting(
        RectTransform testLineLayer,
        RectTransform dashedRoot,
        RectTransform clashRoot,
        RectTransform testHighlightRoot,
        RectTransform testPreviewRoot,
        BattleActionRelationUIView template,
        BattleBezierRelationLineUIView testPreviewCurve,
        Canvas canvas
    )
    {
        lineLayer = testLineLayer;
        normalDashedRoot = dashedRoot;
        normalClashRoot = clashRoot;
        highlightRoot = testHighlightRoot;
        previewRoot = testPreviewRoot;
        relationViewTemplate = template;
        previewCurve = testPreviewCurve;
        targetCanvas = canvas;
        if (relationViewTemplate != null)
        {
            relationViewTemplate.gameObject.SetActive(false);
            relationViewTemplate.PrepareForReuse();
        }
        EnsureRelationLayerRaycastSafety();
        ApplyPreviewVisualSettings();
    }

    internal BattleActionRelationUIView GetVisibleView(int index)
    {
        return index >= 0 && index < activeViews.Count
            ? activeViews[index]
            : null;
    }

    private void RefreshVisibleRelations()
    {
        if (isShuttingDown)
        {
            return;
        }
        RecycleActiveViews();
        if (!IsPlanningState())
        {
            return;
        }

        BuildVisibleArrowEndpointOffsets();
        usedViewInstanceIDs.Clear();
        for (int index = 0; index < cachedRelations.Count; index++)
        {
            BattleActionRelationDescriptor relation = cachedRelations[index];
            bool hovered = !string.IsNullOrEmpty(hoveredSlotID) &&
                relation.InvolvesSlot(hoveredSlotID);
            bool selected = !string.IsNullOrEmpty(selectedSlotID) &&
                relation.InvolvesSlot(selectedSlotID);
            bool highlighted = revealAllHeld
                ? !string.IsNullOrEmpty(hoveredSlotID)
                    ? hovered
                    : selected
                : hovered || selected;
            if (!revealAllHeld && !hovered && !selected)
            {
                continue;
            }
            ShowRelation(
                relation,
                highlighted,
                GetStableArrowEndpointOffset(relation)
            );
        }
    }

    private bool ShowRelation(
        BattleActionRelationDescriptor relation,
        bool highlighted,
        float arrowEndpointOffset
    )
    {
        Vector2 source;
        Vector2 target;
        if (!TryGetSlotLocalPoint(relation.SourceSlotID, out source) ||
            !TryGetSlotLocalPoint(relation.TargetSlotID, out target))
        {
            return false;
        }

        RectTransform root = highlighted
            ? highlightRoot
            : relation.UsesMutualSolidVisual
                ? normalClashRoot
                : normalDashedRoot;
        BattleActionRelationUIView view = GetPooledView(root);
        if (view == null)
        {
            return false;
        }

        bool shown;
        if (relation.UsesMutualSolidVisual)
        {
            Vector2 playerStart = relation.SourceSide ==
                    BattleActionRelationSide.Player
                ? source
                : target;
            Vector2 enemyStart = relation.SourceSide ==
                    BattleActionRelationSide.Enemy
                ? source
                : target;
            ApplyRelationVisualSettings(view, clashArrowScale);
            shown = view.ShowClash(
                relation,
                playerStart,
                enemyStart,
                playerColor,
                enemyColor,
                highlighted,
                baseCurveHeight,
                distanceCurveFactor,
                minCurveHeight,
                maxCurveHeight,
                laneSpacing,
                clashArrowGap
            );
        }
        else
        {
            Color color = relation.SourceSide ==
                    BattleActionRelationSide.Player
                ? playerColor
                : enemyColor;
            ApplyRelationVisualSettings(view, 1f);
            shown = view.ShowUnilateral(
                relation,
                source,
                target,
                color,
                highlighted,
                baseCurveHeight,
                distanceCurveFactor,
                minCurveHeight,
                maxCurveHeight,
                laneSpacing,
                arrowEndpointOffset
            );
        }
        if (!shown)
        {
            view.ClearView();
            return false;
        }

        activeViews.Add(view);
        return true;
    }

    private BattleActionRelationUIView GetPooledView(RectTransform root)
    {
        if (isShuttingDown || relationViewTemplate == null || root == null)
        {
            return null;
        }
        RemoveDestroyedViewReferences();
        BattleActionRelationUIView view = null;
        for (int index = 0; index < relationViewPool.Count; index++)
        {
            if (relationViewPool[index] != null &&
                !relationViewPool[index].gameObject.activeSelf &&
                !usedViewInstanceIDs.Contains(
                    relationViewPool[index].GetInstanceID()
                ))
            {
                view = relationViewPool[index];
                break;
            }
        }
        if (view == null)
        {
            view = Instantiate(relationViewTemplate, root);
            view.name = "RelationView_" + relationViewPool.Count;
            if (!ValidateNewPooledViewOwnership(view))
            {
                Destroy(view.gameObject);
                return null;
            }
            relationViewPool.Add(view);
        }
        view.gameObject.SetActive(false);
        view.transform.SetParent(root, false);
        // 对象池中的旧 sibling 位置不可复用；当前稳定查询顺序决定本次绘制顺序。
        view.transform.SetAsLastSibling();
        RectTransform rect = view.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
        view.PrepareForReuse();
        // 单次刷新中一个View只能绑定一条关系，不能因inactive状态被重复取出。
        usedViewInstanceIDs.Add(view.GetInstanceID());
        return view;
    }

    private bool ValidateNewPooledViewOwnership(
        BattleActionRelationUIView view
    )
    {
        if (view == null || !view.EnsureOwnedCurveReferences())
        {
            return false;
        }

        for (int index = 0; index < relationViewPool.Count; index++)
        {
            BattleActionRelationUIView existing = relationViewPool[index];
            if (existing == null ||
                !view.SharesVisualInstancesWith(existing))
            {
                continue;
            }

            Debug.LogError(
                "RelationView视觉实例被多个池对象共享：" +
                "NewView=" + view.GetInstanceID() +
                "，ExistingView=" + existing.GetInstanceID() +
                "，PrimaryCurve=" +
                view.PrimaryCurve.GetInstanceID() +
                "，PrimaryArrow=" +
                view.PrimaryCurve.ArrowInstanceID +
                "，SecondaryCurve=" +
                view.SecondaryCurve.GetInstanceID() +
                "，SecondaryArrow=" +
                view.SecondaryCurve.ArrowInstanceID,
                view
            );
            return false;
        }
        return true;
    }

    private void BuildVisibleArrowEndpointOffsets()
    {
        visibleTargetCounts.Clear();
        visibleTargetOrdinals.Clear();
        endpointOffsetByRelationID.Clear();
        for (int index = 0; index < cachedRelations.Count; index++)
        {
            BattleActionRelationDescriptor relation = cachedRelations[index];
            if (relation.UsesMutualSolidVisual ||
                !ShouldDisplayRelation(relation))
            {
                continue;
            }

            int count;
            visibleTargetCounts.TryGetValue(relation.TargetSlotID, out count);
            visibleTargetCounts[relation.TargetSlotID] = count + 1;
        }

        // 偏移按稳定RelationID分别保存，不能以TargetSlotID覆盖前一条关系。
        for (int index = 0; index < cachedRelations.Count; index++)
        {
            BattleActionRelationDescriptor relation = cachedRelations[index];
            if (relation.UsesMutualSolidVisual ||
                !ShouldDisplayRelation(relation))
            {
                continue;
            }

            int count;
            visibleTargetCounts.TryGetValue(relation.TargetSlotID, out count);
            int ordinal;
            visibleTargetOrdinals.TryGetValue(
                relation.TargetSlotID,
                out ordinal
            );
            visibleTargetOrdinals[relation.TargetSlotID] = ordinal + 1;
            endpointOffsetByRelationID[relation.RelationID] = count > 1
                ? (ordinal - (count - 1) * 0.5f) *
                    sharedArrowEndpointSpacing
                : 0f;
        }
    }

    private float GetStableArrowEndpointOffset(
        BattleActionRelationDescriptor relation
    )
    {
        if (relation == null || relation.UsesMutualSolidVisual ||
            sharedArrowEndpointSpacing <= 0f)
        {
            return 0f;
        }

        float offset;
        return endpointOffsetByRelationID.TryGetValue(
            relation.RelationID,
            out offset
        ) ? offset : 0f;
    }

    private bool ShouldDisplayRelation(
        BattleActionRelationDescriptor relation
    )
    {
        if (relation == null)
        {
            return false;
        }

        bool hovered = !string.IsNullOrEmpty(hoveredSlotID) &&
            relation.InvolvesSlot(hoveredSlotID);
        bool selected = !string.IsNullOrEmpty(selectedSlotID) &&
            relation.InvolvesSlot(selectedSlotID);
        return revealAllHeld || hovered || selected;
    }

    private static int IndexOfView(
        List<BattleActionRelationUIView> views,
        BattleActionRelationUIView target
    )
    {
        if (views == null || target == null)
        {
            return -1;
        }

        for (int index = 0; index < views.Count; index++)
        {
            if (object.ReferenceEquals(views[index], target))
            {
                return index;
            }
        }
        return -1;
    }

    private static bool HasVisibleVisuals(
        BattleBezierRelationLineUIView curve
    )
    {
        return curve != null &&
            (curve.ActiveSegmentCount > 0 || IsArrowActuallyVisible(curve));
    }

    private static bool IsArrowActuallyVisible(
        BattleBezierRelationLineUIView curve
    )
    {
        return curve != null && curve.ArrowActiveSelf &&
            curve.ArrowAlpha > 0f && curve.gameObject.activeInHierarchy &&
            curve.IsVisible;
    }

    private static int GetActiveSegmentCount(
        BattleBezierRelationLineUIView curve
    )
    {
        return curve != null ? curve.ActiveSegmentCount : 0;
    }

    private static BattleActionRelationUIView FindOwningView(
        BattleBezierRelationLineUIView curve,
        BattleActionRelationUIView[] hierarchyViews
    )
    {
        if (curve == null)
        {
            return null;
        }

        if (hierarchyViews != null)
        {
            for (int index = 0; index < hierarchyViews.Length; index++)
            {
                BattleActionRelationUIView view = hierarchyViews[index];
                if (view != null &&
                    (object.ReferenceEquals(view.PrimaryCurve, curve) ||
                     object.ReferenceEquals(view.SecondaryCurve, curve)))
                {
                    return view;
                }
            }
        }

        return curve.GetComponentInParent<BattleActionRelationUIView>();
    }

    private static BattleBezierRelationLineUIView FindOwningCurve(
        Image image,
        BattleBezierRelationLineUIView[] hierarchyCurves
    )
    {
        if (image == null)
        {
            return null;
        }

        if (hierarchyCurves == null)
        {
            return null;
        }

        int imageInstanceID = image.GetInstanceID();
        for (int index = 0; index < hierarchyCurves.Length; index++)
        {
            BattleBezierRelationLineUIView curve = hierarchyCurves[index];
            if (curve != null &&
                (image.transform.IsChildOf(curve.transform) ||
                 curve.ArrowInstanceID == imageInstanceID ||
                 curve.UnderlayArrowInstanceID == imageInstanceID ||
                 curve.SegmentTemplateInstanceID == imageInstanceID))
            {
                return curve;
            }
        }
        return null;
    }

    private static string GetRelationImageType(
        Image image,
        BattleBezierRelationLineUIView ownerCurve
    )
    {
        if (image == null)
        {
            return "Null";
        }
        if (ownerCurve == null)
        {
            return "UnownedImage";
        }

        int imageInstanceID = image.GetInstanceID();
        if (ownerCurve.ArrowInstanceID == imageInstanceID)
        {
            return "MainArrow";
        }
        if (ownerCurve.UnderlayArrowInstanceID == imageInstanceID)
        {
            return "UnderlayArrow";
        }
        if (ownerCurve.SegmentTemplateInstanceID == imageInstanceID)
        {
            return "SegmentTemplate";
        }
        return image.gameObject.name.Contains("Underlay")
            ? "UnderlaySegment"
            : "MainSegment";
    }

    private bool IsTemplateCurve(BattleBezierRelationLineUIView curve)
    {
        if (curve == null || relationViewTemplate == null)
        {
            return false;
        }
        return object.ReferenceEquals(curve, relationViewTemplate.PrimaryCurve) ||
            object.ReferenceEquals(curve, relationViewTemplate.SecondaryCurve) ||
            curve.transform.IsChildOf(relationViewTemplate.transform);
    }

    private string GetRootType(Transform target)
    {
        if (IsUnderRoot(target, normalDashedRoot))
        {
            return "NormalDashedRoot";
        }
        if (IsUnderRoot(target, normalClashRoot))
        {
            return "NormalClashRoot";
        }
        if (IsUnderRoot(target, highlightRoot))
        {
            return "HighlightRoot";
        }
        if (IsUnderRoot(target, previewRoot))
        {
            return "PreviewRoot";
        }
        return "Other";
    }

    private static bool IsUnderRoot(Transform target, RectTransform root)
    {
        return target != null && root != null &&
            (object.ReferenceEquals(target, root) || target.IsChildOf(root));
    }

    private static void AppendHierarchyCurveSummary(
        StringBuilder builder,
        string label,
        BattleBezierRelationLineUIView curve
    )
    {
        builder.Append("  [").Append(label).Append("] ");
        if (curve == null)
        {
            builder.AppendLine("null");
            return;
        }

        builder.Append("InstanceID=").Append(curve.GetInstanceID())
            .Append(" ActiveSegmentCount=").Append(curve.ActiveSegmentCount)
            .Append(" ActiveMainSegmentCount=")
            .Append(curve.ActiveMainSegmentCount)
            .Append(" ActiveUnderlaySegmentCount=")
            .Append(curve.ActiveUnderlaySegmentCount)
            .Append(" MainSegmentPoolCount=")
            .Append(curve.MainSegmentPoolCount)
            .Append(" UnderlaySegmentPoolCount=")
            .Append(curve.UnderlaySegmentPoolCount)
            .Append(" ArrowActiveSelf=").Append(curve.ArrowActiveSelf)
            .Append(" ArrowAlpha=").Append(curve.ArrowAlpha)
            .Append(" HasVisibleMainArrow=")
            .Append(curve.HasVisibleMainArrow)
            .Append(" HasVisibleUnderlayArrow=")
            .Append(curve.HasVisibleUnderlayArrow)
            .Append(" CurrentLineStyle=").Append(curve.CurrentLineStyle)
            .Append(" PreviousLineStyle=").Append(curve.PreviousLineStyle)
            .Append(" CurrentRange=").Append(curve.CurrentRange)
            .Append(" PreviousRange=").Append(curve.PreviousRange)
            .AppendLine();
    }

    private static void IncrementRelationCount(
        Dictionary<string, int> counts,
        string relationID
    )
    {
        string key = relationID ?? string.Empty;
        int count;
        counts.TryGetValue(key, out count);
        counts[key] = count + 1;
    }

    private static void RegisterOwnedInstance(
        Dictionary<int, string> owners,
        int instanceID,
        string relationID,
        string objectType,
        List<string> conflicts
    )
    {
        if (instanceID == 0)
        {
            return;
        }

        string owner;
        if (!owners.TryGetValue(instanceID, out owner))
        {
            owners.Add(instanceID, relationID ?? string.Empty);
            return;
        }

        conflicts.Add(
            objectType + " InstanceID=" + instanceID +
            " 被重复分配，首次RelationID=" + owner +
            "，当前RelationID=" + (relationID ?? string.Empty)
        );
    }

    private static void AppendCurveDiagnostics(
        StringBuilder builder,
        BattleBezierRelationLineUIView curve,
        string label,
        string relationID,
        Dictionary<int, string> curveOwners,
        Dictionary<int, string> arrowOwners,
        List<string> conflicts
    )
    {
        builder.Append("  [").Append(label).Append("] ");
        if (curve == null)
        {
            builder.AppendLine("null");
            return;
        }

        int curveInstanceID = curve.GetInstanceID();
        int arrowInstanceID = curve.ArrowInstanceID;
        builder.Append("InstanceID=").Append(curveInstanceID)
            .Append(" activeSelf=").Append(curve.gameObject.activeSelf)
            .Append(" ActiveSegmentCount=").Append(curve.ActiveSegmentCount)
            .Append(" ActiveMainSegmentCount=")
            .Append(curve.ActiveMainSegmentCount)
            .Append(" ActiveUnderlaySegmentCount=")
            .Append(curve.ActiveUnderlaySegmentCount)
            .Append(" MainSegmentPoolCount=")
            .Append(curve.MainSegmentPoolCount)
            .Append(" UnderlaySegmentPoolCount=")
            .Append(curve.UnderlaySegmentPoolCount)
            .Append(" ArrowInstanceID=").Append(arrowInstanceID)
            .Append(" ArrowActiveSelf=").Append(curve.ArrowActiveSelf)
            .Append(" ArrowAlpha=").Append(curve.ArrowAlpha)
            .Append(" HasVisibleMainArrow=")
            .Append(curve.HasVisibleMainArrow)
            .Append(" HasVisibleUnderlayArrow=")
            .Append(curve.HasVisibleUnderlayArrow)
            .Append(" CurrentLineStyle=").Append(curve.CurrentLineStyle)
            .Append(" PreviousLineStyle=").Append(curve.PreviousLineStyle)
            .Append(" CurrentRange=").Append(curve.CurrentRange)
            .Append(" PreviousRange=").Append(curve.PreviousRange)
            .Append(" ArrowTip=").Append(curve.ArrowTip)
            .Append(" RangeStart=").Append(curve.RangeStart)
            .Append(" RangeEnd=").Append(curve.RangeEnd)
            .AppendLine();

        RegisterOwnedInstance(
            curveOwners,
            curveInstanceID,
            relationID,
            label,
            conflicts
        );
        RegisterOwnedInstance(
            arrowOwners,
            arrowInstanceID,
            relationID,
            label + " Arrow",
            conflicts
        );
    }

    private static string GetTransformPath(Transform target)
    {
        if (target == null)
        {
            return "<null>";
        }

        StringBuilder path = new StringBuilder(target.name);
        Transform parent = target.parent;
        while (parent != null)
        {
            path.Insert(0, parent.name + "/");
            parent = parent.parent;
        }
        return path.ToString();
    }

    private static bool IsRelationDiagnosticsInputPressed(Keyboard keyboard)
    {
        return keyboard != null && keyboard.f8Key.wasPressedThisFrame;
    }

    private void RecycleActiveViews()
    {
        for (int index = 0; index < activeViews.Count; index++)
        {
            if (activeViews[index] != null)
            {
                activeViews[index].ClearView();
            }
        }
        activeViews.Clear();
        RemoveDestroyedViewReferences();
    }

    private void RemoveDestroyedViewReferences()
    {
        for (int index = relationViewPool.Count - 1; index >= 0; index--)
        {
            if (relationViewPool[index] == null)
            {
                relationViewPool.RemoveAt(index);
            }
        }
    }

    private bool TryGetSlotLocalPoint(
        string slotID,
        out Vector2 localPoint
    )
    {
        localPoint = Vector2.zero;
        RectTransform anchor;
        return slotAnchors.TryGetValue(slotID, out anchor) &&
            anchor != null &&
            TryConvertRectAnchorToLayerLocal(
                anchor,
                lineLayer,
                targetCanvas,
                uiCamera,
                out localPoint
            );
    }

    private bool IsPlanningState()
    {
        return runtimeState != null &&
            !runtimeState.IsBattleEnded &&
            runtimeState.currentPhase == "Prepare" &&
            runtimeState.currentExecutionPlan == null;
    }

    private static Camera GetEventCamera(Canvas canvas, Camera fallback)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }
        return canvas.worldCamera != null ? canvas.worldCamera : fallback;
    }

    private static bool TryReadPointerScreenPosition(
        out Vector2 screenPosition
    )
    {
        screenPosition = Vector2.zero;
        Pointer pointer = Pointer.current;
        if (pointer == null)
        {
            return false;
        }
        screenPosition = pointer.position.ReadValue();
        return true;
    }

    private void EnsureRelationLayerRaycastSafety()
    {
        if (isShuttingDown || lineLayer == null)
        {
            return;
        }
        if (relationLayerCanvasGroup == null)
        {
            relationLayerCanvasGroup =
                lineLayer.GetComponent<CanvasGroup>();
            if (relationLayerCanvasGroup == null)
            {
                relationLayerCanvasGroup =
                    lineLayer.gameObject.AddComponent<CanvasGroup>();
            }
        }
        relationLayerCanvasGroup.interactable = false;
        relationLayerCanvasGroup.blocksRaycasts = false;
    }

    private void ApplyRelationVisualSettings(
        BattleActionRelationUIView view,
        float resolvedArrowScale
    )
    {
        if (isShuttingDown || view == null)
        {
            return;
        }
        view.ApplyVisualSettings(
            segmentSize,
            arrowSize,
            dashedGap,
            solidOverlap,
            underlayScale,
            resolvedArrowScale
        );
    }

    private void ApplyPreviewVisualSettings()
    {
        if (isShuttingDown || previewCurve == null)
        {
            return;
        }
        previewCurve.ApplyVisualSettings(
            segmentSize,
            arrowSize,
            dashedGap,
            solidOverlap,
            underlayScale,
            previewArrowScale
        );
    }

    private bool ValidateReference(Object value, string label)
    {
        if (value != null)
        {
            return true;
        }
        Debug.LogWarning("行动关系线缺少 " + label + "。", this);
        return false;
    }

    private bool ValidateRootParent(RectTransform root, string label)
    {
        if (root == null || lineLayer == null)
        {
            return false;
        }
        if (root.parent == lineLayer)
        {
            return true;
        }
        Debug.LogWarning(label + " 必须直接位于 Line Layer 下。", this);
        return false;
    }

    private void OnDisable()
    {
        isShuttingDown = true;
        ClearAll();
    }

    private void OnDestroy()
    {
        isShuttingDown = true;
        ClearAll();
        slotAnchors.Clear();
        slotViews.Clear();
        relationViewPool.Clear();
        activeViews.Clear();
    }
}
