using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private BattleRuntimeState runtimeState;
    private BattleActionRelationQueryService queryService;
    private string hoveredSlotID;
    private string selectedSlotID;
    private string previewSourceSlotID;
    private bool revealAllHeld;
    private bool previewActive;
    private Vector2 previewPointerScreenPosition;
    private bool isShuttingDown;

    public int VisibleRelationCount => activeViews.Count;
    public string HoveredSlotID => hoveredSlotID;
    public string SelectedSlotID => selectedSlotID;
    public bool RevealAllHeld => revealAllHeld;
    public bool PreviewActive => previewActive;
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
        if (previewCurve != null)
        {
            previewCurve.Clear();
        }
    }

    public void ClearAll()
    {
        RecycleActiveViews();
        cachedRelations.Clear();
        hoveredSlotID = string.Empty;
        selectedSlotID = string.Empty;
        revealAllHeld = false;
        EndCardTargetingPreview();
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
            ShowRelation(relation, highlighted);
        }
    }

    private void ShowRelation(
        BattleActionRelationDescriptor relation,
        bool highlighted
    )
    {
        Vector2 source;
        Vector2 target;
        if (!TryGetSlotLocalPoint(relation.SourceSlotID, out source) ||
            !TryGetSlotLocalPoint(relation.TargetSlotID, out target))
        {
            return;
        }

        RectTransform root = highlighted
            ? highlightRoot
            : relation.UsesMutualSolidVisual
                ? normalClashRoot
                : normalDashedRoot;
        BattleActionRelationUIView view = GetPooledView(root);
        if (view == null)
        {
            return;
        }

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
            view.ShowClash(
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
            view.ShowUnilateral(
                relation,
                source,
                target,
                color,
                highlighted,
                baseCurveHeight,
                distanceCurveFactor,
                minCurveHeight,
                maxCurveHeight,
                laneSpacing
            );
        }
        activeViews.Add(view);
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
                !relationViewPool[index].gameObject.activeSelf)
            {
                view = relationViewPool[index];
                break;
            }
        }
        if (view == null)
        {
            view = Instantiate(relationViewTemplate, root);
            view.name = "RelationView_" + relationViewPool.Count;
            relationViewPool.Add(view);
        }
        view.gameObject.SetActive(false);
        view.transform.SetParent(root, false);
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
        return view;
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
