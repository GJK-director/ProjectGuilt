using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum BattleActionSlotCardInfoPointerEvent
{
    HoverEnter,
    HoverExit,
    ClickLock,
    Refresh,
    SourceInvalidated
}

public sealed class BattleActionSlotCardInfoHoverRequest
{
    public readonly GameObject source;
    public readonly bool isEnemySide;
    public readonly CharacterData owner;
    public readonly CharacterData target;
    public readonly BattleCardState cardState;
    public readonly BattleActionSlotCardInfoPointerEvent pointerEvent;

    public BattleActionSlotCardInfoHoverRequest(
        GameObject requestSource,
        bool enemySide,
        CharacterData cardOwner,
        CharacterData cardTarget,
        BattleCardState requestCardState,
        BattleActionSlotCardInfoPointerEvent requestPointerEvent
    )
    {
        source = requestSource;
        isEnemySide = enemySide;
        owner = cardOwner;
        target = cardTarget;
        cardState = requestCardState;
        pointerEvent = requestPointerEvent;
    }
}

// 行动槽位放大卡牌宿主。
// 为保持既有接线，入口和请求类型仍沿用 HandlePointer / HoverRequest 命名。
// 悬停请求临时覆盖当前侧的点击锁定；移出后恢复锁定内容或关闭。
public sealed class BattleActionSlotCardInfoPanelHost : MonoBehaviour
{
    // 通用关键词二级面板使用 32767；这里低一层，确保关键词说明在卡面上方。
    const int OverlaySortingOrder = 32766;
    const float ReferencePanelGap = 80f;
    static readonly Vector2 ReferenceResolution =
        new Vector2(1920f, 1080f);

    static BattleActionSlotCardInfoPanelHost instance;
    static int suppressedClickLockSourceID;

    [Header("预设体引用（可直接修改子节点 UI）")]
    [SerializeField] Canvas overlayCanvas;
    [SerializeField] RectTransform allyPanelRect;
    [SerializeField] RectTransform enemyPanelRect;
    [SerializeField] BattleActionSlotCardInfoPanelView allyPanelView;
    [SerializeField] BattleActionSlotCardInfoPanelView enemyPanelView;

    sealed class SideDisplayState
    {
        public GameObject hoveredSource;
        public BattleActionSlotCardInfoHoverRequest hoveredRequest;
        public GameObject lockedSource;
        public BattleActionSlotCardInfoHoverRequest lockedRequest;
        public bool sourcePointerInside;
        public bool panelPointerInside;
        public bool hoverExitPending;

        public bool Tracks(GameObject source)
        {
            return source != null &&
                (hoveredSource == source || lockedSource == source);
        }
    }

    RectTransform hostRect;
    readonly SideDisplayState allyState = new SideDisplayState();
    readonly SideDisplayState enemyState = new SideDisplayState();
    int lastScreenWidth = -1;
    int lastScreenHeight = -1;
    Rect lastSafeArea;

    // 给所有行动槽位保留的唯一宿主调用接口。
    public static void HandlePointer(
        BattleActionSlotCardInfoHoverRequest request
    )
    {
        if (request == null || request.source == null)
        {
            return;
        }

        if (request.pointerEvent ==
            BattleActionSlotCardInfoPointerEvent.ClickLock &&
            ConsumeSuppressedClickLock(request.source))
        {
            return;
        }

        if (request.pointerEvent ==
            BattleActionSlotCardInfoPointerEvent.HoverExit ||
            request.pointerEvent ==
            BattleActionSlotCardInfoPointerEvent.SourceInvalidated)
        {
            instance?.ReceivePointerRequest(request);
            return;
        }

        BattleActionSlotCardInfoPanelHost host =
            GetOrCreateHost(request.source);
        host?.ReceivePointerRequest(request);
    }

    // 战斗空白区右键时同时关闭友方与敌方的临时/锁定卡面。
    public static void CloseAllPanels()
    {
        suppressedClickLockSourceID = 0;
        instance?.CloseBothSides();
    }

    // 敌方槽位完成行动后，槽位 View 仍会继续发送本次左键的锁定通知。
    // 记录该来源并只忽略紧随其后的这一条通知，普通敌方槽位点击仍可锁定查看。
    public static void CloseAllPanelsAndSuppressNextClickLock(
        GameObject source
    )
    {
        SuppressNextClickLock(source);
        instance?.CloseBothSides();
    }

    public static void SuppressNextClickLock(GameObject source)
    {
        suppressedClickLockSourceID = source != null
            ? source.GetInstanceID()
            : 0;
    }

    public static bool IsActiveSource(GameObject source)
    {
        return instance != null &&
            source != null &&
            (instance.allyState.Tracks(source) ||
                instance.enemyState.Tracks(source));
    }

    static bool ConsumeSuppressedClickLock(GameObject source)
    {
        if (suppressedClickLockSourceID == 0)
        {
            return false;
        }

        bool shouldSuppress = source != null &&
            source.GetInstanceID() == suppressedClickLockSourceID;
        suppressedClickLockSourceID = 0;
        return shouldSuppress;
    }

    static BattleActionSlotCardInfoPanelHost GetOrCreateHost(
        GameObject source
    )
    {
        if (instance != null)
        {
            instance.MatchSourceDisplay(source);
            return instance;
        }

        Canvas sourceCanvas = source.GetComponentInParent<Canvas>();
        if (sourceCanvas == null)
        {
            return null;
        }

        GameObject hostObject = new GameObject(
            "BattleActionSlotCardInfoPanelHost",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        hostObject.layer = sourceCanvas.gameObject.layer;

        Canvas canvas = hostObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = OverlaySortingOrder;
        canvas.targetDisplay = sourceCanvas.rootCanvas.targetDisplay;

        CanvasScaler scaler = hostObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return hostObject.AddComponent<
            BattleActionSlotCardInfoPanelHost
        >();
    }

    void Awake()
    {
        hostRect = transform as RectTransform;
        if (overlayCanvas == null)
        {
            overlayCanvas = GetComponent<Canvas>();
        }

        ResolvePrefabReferences();
        BuildFallbackPanels();
        allyPanelView?.SetCloseHandler(() => CloseSide(false));
        enemyPanelView?.SetCloseHandler(() => CloseSide(true));
        allyPanelView?.SetPointerPresenceHandler(
            inside => SetPanelPointerInside(false, inside)
        );
        enemyPanelView?.SetPointerPresenceHandler(
            inside => SetPanelPointerInside(true, inside)
        );
        HidePanels();
        KeepCanvasInFront();

        if (instance == null)
        {
            instance = this;
        }
    }

    void Update()
    {
        ClearDestroyedSources(allyState, false);
        ClearDestroyedSources(enemyState, true);
    }

    void LateUpdate()
    {
        KeepCanvasInFront();
        if (HasResolutionChanged())
        {
            RefreshResponsiveLayout();
        }

        // 等 EventSystem 派发完槽位 Exit 与面板 Enter，再决定是否真正关闭。
        FlushPendingHoverExit(allyState, false);
        FlushPendingHoverExit(enemyState, true);
    }

    void OnRectTransformDimensionsChange()
    {
        lastScreenWidth = -1;
    }

    void OnDisable()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    void ReceivePointerRequest(
        BattleActionSlotCardInfoHoverRequest request
    )
    {
        SideDisplayState state = request.isEnemySide
            ? enemyState
            : allyState;

        switch (request.pointerEvent)
        {
            case BattleActionSlotCardInfoPointerEvent.HoverEnter:
                SetHoveredRequest(state, request);
                break;
            case BattleActionSlotCardInfoPointerEvent.HoverExit:
                QueueHoveredExit(state, request.source);
                break;
            case BattleActionSlotCardInfoPointerEvent.ClickLock:
                SetLockedRequest(state, request);
                break;
            case BattleActionSlotCardInfoPointerEvent.Refresh:
                RefreshTrackedRequest(state, request);
                break;
            case BattleActionSlotCardInfoPointerEvent.SourceInvalidated:
                ClearSource(state, request.source);
                break;
        }

        RefreshSide(state, request.isEnemySide);
    }

    static void SetHoveredRequest(
        SideDisplayState state,
        BattleActionSlotCardInfoHoverRequest request
    )
    {
        if (request.cardState == null)
        {
            return;
        }

        state.hoveredSource = request.source;
        state.hoveredRequest = request;
        state.sourcePointerInside = true;
        state.hoverExitPending = false;
    }

    static void QueueHoveredExit(
        SideDisplayState state,
        GameObject source
    )
    {
        if (state.hoveredSource != source)
        {
            return;
        }

        state.sourcePointerInside = false;
        state.hoverExitPending = true;
    }

    static void ClearHoveredRequest(
        SideDisplayState state,
        GameObject source
    )
    {
        if (state.hoveredSource != source)
        {
            return;
        }

        state.hoveredSource = null;
        state.hoveredRequest = null;
    }

    static void SetLockedRequest(
        SideDisplayState state,
        BattleActionSlotCardInfoHoverRequest request
    )
    {
        if (request.cardState == null)
        {
            return;
        }

        state.lockedSource = request.source;
        state.lockedRequest = request;
    }

    static void RefreshTrackedRequest(
        SideDisplayState state,
        BattleActionSlotCardInfoHoverRequest request
    )
    {
        if (state.hoveredSource == request.source)
        {
            if (request.cardState == null)
            {
                state.hoveredSource = null;
                state.hoveredRequest = null;
            }
            else
            {
                state.hoveredRequest = request;
            }
        }

        if (state.lockedSource == request.source)
        {
            if (request.cardState == null)
            {
                state.lockedSource = null;
                state.lockedRequest = null;
            }
            else
            {
                state.lockedRequest = request;
            }
        }
    }

    static void ClearSource(SideDisplayState state, GameObject source)
    {
        bool clearedHoveredSource = state.hoveredSource == source;
        ClearHoveredRequest(state, source);
        if (clearedHoveredSource)
        {
            state.sourcePointerInside = false;
            state.hoverExitPending = false;
        }
        if (state.lockedSource == source)
        {
            state.lockedSource = null;
            state.lockedRequest = null;
        }
    }

    void RefreshSide(SideDisplayState state, bool enemySide)
    {
        BattleActionSlotCardInfoHoverRequest request =
            state.hoveredRequest ?? state.lockedRequest;
        if (request == null || request.cardState == null)
        {
            state.panelPointerInside = false;
            state.hoverExitPending = false;
            GetPanelView(enemySide)?.Hide();
            return;
        }

        GetPanelView(enemySide)?.ShowCard(
            request.owner,
            request.target,
            request.cardState,
            enemySide
        );
        KeepCanvasInFront();
        RefreshResponsiveLayout();
    }

    BattleActionSlotCardInfoPanelView GetPanelView(bool enemySide)
    {
        return enemySide ? enemyPanelView : allyPanelView;
    }

    void ClearDestroyedSources(SideDisplayState state, bool enemySide)
    {
        bool changed = false;
        if (state.hoveredRequest != null && state.hoveredSource == null)
        {
            state.hoveredRequest = null;
            state.sourcePointerInside = false;
            state.hoverExitPending = false;
            changed = true;
        }

        if (state.lockedRequest != null && state.lockedSource == null)
        {
            state.lockedRequest = null;
            changed = true;
        }

        if (changed)
        {
            RefreshSide(state, enemySide);
        }
    }

    void CloseSide(bool enemySide)
    {
        SideDisplayState state = enemySide ? enemyState : allyState;
        state.hoveredSource = null;
        state.hoveredRequest = null;
        state.lockedSource = null;
        state.lockedRequest = null;
        state.sourcePointerInside = false;
        state.panelPointerInside = false;
        state.hoverExitPending = false;
        GetPanelView(enemySide)?.Hide();
    }

    void CloseBothSides()
    {
        CloseSide(false);
        CloseSide(true);
    }

    void SetPanelPointerInside(bool enemySide, bool inside)
    {
        SideDisplayState state = enemySide ? enemyState : allyState;
        state.panelPointerInside = inside;
        if (inside)
        {
            state.hoverExitPending = false;
            return;
        }

        if (!state.sourcePointerInside && state.hoveredRequest != null)
        {
            state.hoverExitPending = true;
        }
    }

    void FlushPendingHoverExit(
        SideDisplayState state,
        bool enemySide
    )
    {
        if (!state.hoverExitPending)
        {
            return;
        }

        state.hoverExitPending = false;
        if (state.sourcePointerInside || state.panelPointerInside)
        {
            return;
        }

        GameObject exitedSource = state.hoveredSource;
        ClearHoveredRequest(state, exitedSource);
        RefreshSide(state, enemySide);
    }

    void HidePanels()
    {
        allyPanelView?.Hide();
        enemyPanelView?.Hide();
    }

    void MatchSourceDisplay(GameObject source)
    {
        Canvas sourceCanvas = source != null
            ? source.GetComponentInParent<Canvas>()
            : null;
        if (overlayCanvas != null && sourceCanvas != null)
        {
            overlayCanvas.targetDisplay =
                sourceCanvas.rootCanvas.targetDisplay;
        }
    }

    void KeepCanvasInFront()
    {
        if (overlayCanvas == null)
        {
            return;
        }

        if (overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = OverlaySortingOrder;

        SortingLayer[] sortingLayers = SortingLayer.layers;
        if (sortingLayers != null && sortingLayers.Length > 0)
        {
            int highestLayerID = sortingLayers[0].id;
            int highestLayerValue = sortingLayers[0].value;
            for (int index = 1; index < sortingLayers.Length; index++)
            {
                if (sortingLayers[index].value > highestLayerValue)
                {
                    highestLayerID = sortingLayers[index].id;
                    highestLayerValue = sortingLayers[index].value;
                }
            }

            overlayCanvas.sortingLayerID = highestLayerID;
        }

        transform.SetAsLastSibling();
    }

    bool HasResolutionChanged()
    {
        Rect safeArea = Screen.safeArea;
        bool changed = lastScreenWidth != Screen.width ||
            lastScreenHeight != Screen.height ||
            lastSafeArea != safeArea;
        if (changed)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastSafeArea = safeArea;
        }

        return changed;
    }

    void RefreshResponsiveLayout()
    {
        if (hostRect == null ||
            hostRect.rect.width <= 0f ||
            hostRect.rect.height <= 0f)
        {
            return;
        }

        float rootWidth = hostRect.rect.width;
        float rootHeight = hostRect.rect.height;
        float margin = Mathf.Clamp(rootWidth * 0.0125f, 12f, 28f);

        Rect safeArea = Screen.safeArea;
        float screenWidth = Mathf.Max(1f, Screen.width);
        float screenHeight = Mathf.Max(1f, Screen.height);
        float leftInset = safeArea.xMin / screenWidth * rootWidth;
        float rightInset =
            (screenWidth - safeArea.xMax) / screenWidth * rootWidth;
        float topInset =
            (screenHeight - safeArea.yMax) / screenHeight * rootHeight;
        float bottomInset = safeArea.yMin / screenHeight * rootHeight;
        float safeCenterOffsetX =
            (leftInset - rightInset) * 0.5f;
        float centerGap = Mathf.Clamp(
            rootWidth * (ReferencePanelGap / ReferenceResolution.x),
            32f,
            ReferencePanelGap
        );

        const float cardAspect = 780f / 1150f;
        float availableHeight = Mathf.Max(
            180f,
            rootHeight - topInset - bottomInset - margin * 2f
        );
        float panelHeight = Mathf.Min(
            Mathf.Clamp(rootHeight * 0.5f, 260f, 560f),
            availableHeight
        );
        float panelWidth = panelHeight * cardAspect;
        float maxPanelWidth = Mathf.Max(176f, rootWidth * 0.36f);
        if (panelWidth > maxPanelWidth)
        {
            panelWidth = maxPanelWidth;
            panelHeight = panelWidth / cardAspect;
        }

        ConfigureSideRect(
            allyPanelRect,
            new Vector2(0.5f, 1f),
            new Vector2(1f, 1f),
            new Vector2(
                safeCenterOffsetX - centerGap * 0.5f,
                -topInset - margin
            ),
            panelWidth,
            panelHeight
        );
        ConfigureSideRect(
            enemyPanelRect,
            new Vector2(0.5f, 1f),
            new Vector2(0f, 1f),
            new Vector2(
                safeCenterOffsetX + centerGap * 0.5f,
                -topInset - margin
            ),
            panelWidth,
            panelHeight
        );
    }

    static void ConfigureSideRect(
        RectTransform rect,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 position,
        float width,
        float height
    )
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(width, height);
    }

    void ResolvePrefabReferences()
    {
        ResolveSide(
            "AllyCardInfoPanel",
            ref allyPanelRect,
            ref allyPanelView
        );
        ResolveSide(
            "EnemyCardInfoPanel",
            ref enemyPanelRect,
            ref enemyPanelView
        );
    }

    void ResolveSide(
        string objectName,
        ref RectTransform rect,
        ref BattleActionSlotCardInfoPanelView view
    )
    {
        if (rect == null)
        {
            rect = transform.Find(objectName) as RectTransform;
        }

        if (view == null && rect != null)
        {
            view = rect.GetComponent<BattleActionSlotCardInfoPanelView>();
        }
    }

    void BuildFallbackPanels()
    {
        if (allyPanelRect == null || allyPanelView == null)
        {
            allyPanelView = CreateFallbackPanel(
                "AllyCardInfoPanel",
                false,
                out allyPanelRect
            );
        }

        if (enemyPanelRect == null || enemyPanelView == null)
        {
            enemyPanelView = CreateFallbackPanel(
                "EnemyCardInfoPanel",
                true,
                out enemyPanelRect
            );
        }

        RefreshResponsiveLayout();
    }

    BattleActionSlotCardInfoPanelView CreateFallbackPanel(
        string objectName,
        bool enemySide,
        out RectTransform panelRect
    )
    {
        GameObject panelObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image),
            typeof(Outline),
            typeof(BattleActionSlotCardInfoPanelView)
        );
        panelObject.layer = gameObject.layer;
        panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.SetParent(transform, false);

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color32(17, 19, 27, 248);
        background.raycastTarget = false;
        Outline outline = panelObject.GetComponent<Outline>();
        outline.effectColor = enemySide
            ? new Color32(174, 87, 215, 230)
            : new Color32(83, 183, 127, 230);

        BattleActionSlotCardInfoPanelView view =
            panelObject.GetComponent<BattleActionSlotCardInfoPanelView>();
        TMP_Text fallbackText = CreateFallbackText(panelRect, enemySide);
        UnityEngine.Debug.LogWarning(
            "BattleScene 未放置行动槽位卡牌详情预设体，已启用简化运行时兜底。",
            this
        );
        fallbackText.text = enemySide
            ? "敌方行动卡牌"
            : "我方行动卡牌";
        return view;
    }

    TMP_Text CreateFallbackText(RectTransform parent, bool enemySide)
    {
        GameObject textObject = new GameObject(
            "FallbackHint",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textObject.layer = gameObject.layer;
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(20f, 20f);
        rect.offsetMax = new Vector2(-20f, -20f);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 24f;
        text.color = enemySide
            ? new Color32(224, 192, 245, 255)
            : new Color32(190, 239, 207, 255);
        text.raycastTarget = false;
        return text;
    }
}
