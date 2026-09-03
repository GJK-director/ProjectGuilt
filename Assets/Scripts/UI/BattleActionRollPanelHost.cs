using System;
using System.Collections;
using UnityEngine;

internal enum BattleActionRollPanelLifecycleState
{
    Hidden,
    ShowPending,
    FadingIn,
    Visible,
    TerminalHold,
    FadingOut
}

// 动态生成的行动 Roll 面板宿主。仅负责表现，不参与拼点规则与随机数生成。
[DefaultExecutionOrder(300)]
public sealed class BattleActionRollPanelHost : MonoBehaviour
{
    const string ResourcePath = "UI/BattleActionRollPanel";
    const string BehindCharacterSortingLayer = "Battle_Environment";
    const int BehindCharacterSortingOrder = 32765;

    static BattleActionRollPanelHost instance;
    static bool missingPrefabLogged;

    [Header("预设体引用")]
    [SerializeField] Canvas overlayCanvas;
    [SerializeField] RectTransform safeAreaRoot;
    [SerializeField] CanvasGroup panelCanvasGroup;
    [SerializeField] BattleActionRollPanelSideView allySideView;
    [SerializeField] BattleActionRollPanelSideView enemySideView;

    [Header("生成表现")]
    [SerializeField, Min(0f)] float fadeInDuration = 0.2f;
    [SerializeField, Min(0f)] float terminalHoldDuration = 0.2f;
    [SerializeField, Min(0f)] float fadeOutDuration = 0.2f;

    [Header("角色跟随")]
    [SerializeField, Min(0f)] float followHorizontalGap = 24f;
    [SerializeField, Min(0f)] float followVerticalGap = 18f;
    [SerializeField, Min(0f)] float topHudReservedHeight = 112f;
    [SerializeField] bool clampToSafeArea = true;

    Coroutine fadeCoroutine;
    Action transitionCompletion;
    bool visible;
    BattleActionRollPanelLifecycleState lifecycleState =
        BattleActionRollPanelLifecycleState.Hidden;
    Rect lastSafeArea;
    Vector2Int lastScreenSize;
    RectTransform allySideRect;
    RectTransform enemySideRect;
    CharacterData allyFollowActor;
    CharacterData enemyFollowActor;
    BattleUnitViewHandle allyFollowHandle;
    BattleUnitViewHandle enemyFollowHandle;
    BattleUnitViewSpawner unitViewSpawner;
    Camera worldCamera;

    public static bool ShowForActionBegin(
        BattlePresentationRequest request,
        Action visibleCompletion = null
    )
    {
        BattleClashSession session = GetSupportedSession(request);
        BattleResolutionPlan unilateralAttackPlan =
            GetSupportedUnilateralAttackPlan(
            request,
            false
            );
        if (session == null && unilateralAttackPlan == null)
        {
            return false;
        }

        BattleActionRollPanelHost host = ResolveOrCreateInstance();
        if (host != null)
        {
            if (session != null)
            {
                return host.ShowSession(
                    request,
                    session,
                    false,
                    visibleCompletion
                );
            }
            return host.ShowOneSidedUnilateralAttack(
                unilateralAttackPlan,
                false,
                visibleCompletion
            );
        }

        return false;
    }

    public static bool ShowForRoll(BattlePresentationRequest request)
    {
        BattleClashSession session = GetSupportedSession(request);
        BattleResolutionPlan unilateralAttackPlan =
            GetSupportedUnilateralAttackPlan(
            request,
            true
            );
        if (session == null && unilateralAttackPlan == null)
        {
            return false;
        }

        BattleActionRollPanelHost host = ResolveOrCreateInstance();
        if (host != null)
        {
            if (session != null)
            {
                return host.ShowSession(request, session, true, null);
            }
            return host.ShowOneSidedUnilateralAttack(
                unilateralAttackPlan,
                true,
                null
            );
        }

        return false;
    }

    public static bool ShowTerminalRollResult(
        BattlePresentationRequest request,
        Action hiddenCompletion
    )
    {
        if (!ShowForRoll(request) || instance == null)
        {
            return false;
        }

        instance.BeginTerminalExit(hiddenCompletion);
        return true;
    }

    public static void HideImmediate()
    {
        if (instance != null)
        {
            instance.HideInternal();
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (overlayCanvas != null)
        {
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = BehindCharacterSortingOrder;
        }
        CacheSideRects();
        ApplyFallbackLayout();
        SetCanvasState(0f);
        SetSideViewsActive(false);
        ApplySafeArea(true);
    }

    void LateUpdate()
    {
        if (visible)
        {
            RefreshFollowLayout();
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    void OnRectTransformDimensionsChange()
    {
        ApplySafeArea(false);
    }

    bool ShowSession(
        BattlePresentationRequest request,
        BattleClashSession session,
        bool hasRolledPoint,
        Action visibleCompletion
    )
    {
        ApplySafeArea(false);
        ResolveSessionDisplaySides(
            request,
            session,
            out BattleClashSideState allySide,
            out BattleClashSideState enemySide
        );
        BindFollowActors(
            allySide != null ? allySide.actor : null,
            enemySide != null ? enemySide.actor : null
        );

        CharacterData allyTarget = enemySide != null
            ? enemySide.actor
            : null;
        CharacterData enemyTarget = allySide != null
            ? allySide.actor
            : null;
        bool includeClashPointModifier = session.ClashType !=
            BattleClashType.DefenseVsAttack;
        bool allyShown = allySideView != null && (hasRolledPoint
            ? allySideView.ShowRoll(
                allySide,
                allyTarget,
                GetRolledPoint(session, allySide),
                includeClashPointModifier
            )
            : allySideView.ShowPending(
                allySide,
                allyTarget,
                includeClashPointModifier
            ));
        bool enemyShown = enemySideView != null && (hasRolledPoint
            ? enemySideView.ShowRoll(
                enemySide,
                enemyTarget,
                GetRolledPoint(session, enemySide),
                includeClashPointModifier
            )
            : enemySideView.ShowPending(
                enemySide,
                enemyTarget,
                includeClashPointModifier
            ));
        if (!allyShown && !enemyShown)
        {
            HideInternal();
            return false;
        }

        RefreshFollowLayout();
        if (!ShouldStartFadeIn(lifecycleState))
        {
            visible = true;
            SetCanvasState(1f);
            lifecycleState = BattleActionRollPanelLifecycleState.Visible;
            visibleCompletion?.Invoke();
            return true;
        }

        BeginEntrance(visibleCompletion);
        return true;
    }

    bool ShowOneSidedUnilateralAttack(
        BattleResolutionPlan plan,
        bool hasRolledPoint,
        Action visibleCompletion
    )
    {
        if (plan == null)
        {
            return false;
        }

        bool showOnAllySide = ShouldShowUnilateralOnAllySide(plan);
        ApplySafeArea(false);
        BindFollowActors(
            showOnAllySide ? plan.attacker : plan.target,
            showOnAllySide ? plan.target : plan.attacker
        );
        BattleClashSideState attackerSide = new BattleClashSideState(
            plan.attacker,
            plan.sourceCardState,
            plan.freeActionPointSnapshot,
            plan.freeActionResourceSnapshot
        );
        BattleActionRollPanelSideView shownSideView = showOnAllySide
            ? allySideView
            : enemySideView;
        BattleActionRollPanelSideView hiddenSideView = showOnAllySide
            ? enemySideView
            : allySideView;
        bool shown = shownSideView != null && (hasRolledPoint
            ? shownSideView.ShowOneSidedAttackRoll(
                attackerSide,
                plan.target,
                plan.freeActionPoint
            )
            : shownSideView.ShowOneSidedAttackPending(
                attackerSide,
                plan.target
            ));
        hiddenSideView?.Hide();
        if (!shown)
        {
            HideInternal();
            return false;
        }

        RefreshFollowLayout();
        if (!ShouldStartFadeIn(lifecycleState))
        {
            visible = true;
            SetCanvasState(1f);
            lifecycleState = BattleActionRollPanelLifecycleState.Visible;
            visibleCompletion?.Invoke();
            return true;
        }

        BeginEntrance(visibleCompletion);
        return true;
    }

    static BattleClashSession GetSupportedSession(
        BattlePresentationRequest request
    )
    {
        BattleClashSession session = request != null
            ? request.ClashSession
            : null;
        return session != null && IsSupportedClashType(session.ClashType)
            ? session
            : null;
    }

    static void ResolveSessionDisplaySides(
        BattlePresentationRequest request,
        BattleClashSession session,
        out BattleClashSideState allySide,
        out BattleClashSideState enemySide
    )
    {
        allySide = session != null ? session.SideA : null;
        enemySide = session != null ? session.SideB : null;
        if (session == null || request == null ||
            request.InteractionContext == null)
        {
            return;
        }

        BattleExecutionAction sideAAction =
            request.InteractionContext.SideA;
        BattleExecutionAction sideBAction =
            request.InteractionContext.SideB;
        BattleExecutionAction allyAction = IsAllyAction(sideAAction)
            ? sideAAction
            : IsAllyAction(sideBAction)
                ? sideBAction
                : null;
        if (MatchesSessionSide(allyAction, session.SideB))
        {
            allySide = session.SideB;
            enemySide = session.SideA;
        }
    }

    static bool IsAllyAction(BattleExecutionAction action)
    {
        return action != null && action.actionSlot != null;
    }

    static bool MatchesSessionSide(
        BattleExecutionAction action,
        BattleClashSideState side
    )
    {
        return action != null && side != null &&
            object.ReferenceEquals(action.actor, side.actor) &&
            object.ReferenceEquals(action.cardState, side.cardState);
    }

    static int GetRolledPoint(
        BattleClashSession session,
        BattleClashSideState side
    )
    {
        return session != null && object.ReferenceEquals(side, session.SideB)
            ? session.SideBPoint
            : session != null
                ? session.SideAPoint
                : 0;
    }

    internal static bool IsSupportedClashType(BattleClashType clashType)
    {
        return clashType == BattleClashType.AttackVsAttack ||
            clashType == BattleClashType.DefenseVsAttack ||
            clashType == BattleClashType.DodgeVsAttack;
    }

    internal static bool ShouldUseTerminalExit(
        BattlePresentationResultKind resultKind
    )
    {
        return resultKind != BattlePresentationResultKind.AttackTie;
    }

    internal static bool CanCompleteActionBegin(
        bool actionBeginPresentationFinished,
        bool panelEntranceRequired,
        bool panelEntranceFinished
    )
    {
        return actionBeginPresentationFinished &&
            (!panelEntranceRequired || panelEntranceFinished);
    }

    internal static bool ShouldStartFadeIn(
        BattleActionRollPanelLifecycleState state
    )
    {
        return state == BattleActionRollPanelLifecycleState.Hidden;
    }

    internal BattleActionRollPanelLifecycleState LifecycleState =>
        lifecycleState;

    internal static bool IsHidden => instance == null ||
        instance.lifecycleState ==
            BattleActionRollPanelLifecycleState.Hidden;

    static BattleResolutionPlan GetSupportedUnilateralAttackPlan(
        BattlePresentationRequest request,
        bool requireRolledPoint
    )
    {
        BattleResolutionPlan plan = request != null
            ? request.ResolutionPlan
            : null;
        return IsSupportedUnilateralAttackPlan(plan, requireRolledPoint)
                ? plan
                : null;
    }

    internal static bool IsSupportedUnilateralAttackPlan(
        BattleResolutionPlan plan,
        bool requireRolledPoint
    )
    {
        return plan != null &&
            (plan.planKind == BattleResolutionPlanKind.FreeActionAttack ||
             plan.planKind ==
                BattleResolutionPlanKind.UnrespondedEnemyAttack) &&
            plan.sourceCardState != null &&
            plan.sourceCardState.cardData != null &&
            plan.sourceCardState.cardData.cardType == CardType.Attack &&
            plan.attacker != null && plan.target != null &&
            plan.playerCardUsed != plan.enemyCardUsed &&
            (!requireRolledPoint || plan.freeActionHasRolled);
    }

    internal static bool ShouldShowUnilateralOnAllySide(
        BattleResolutionPlan plan
    )
    {
        return plan != null && plan.playerCardUsed && !plan.enemyCardUsed;
    }

    void BeginEntrance(Action visibleCompletion)
    {
        StopTransitionCoroutine();
        transitionCompletion = visibleCompletion;
        visible = true;
        lifecycleState = BattleActionRollPanelLifecycleState.ShowPending;
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        lifecycleState = BattleActionRollPanelLifecycleState.FadingIn;
        if (fadeInDuration <= 0f)
        {
            SetCanvasState(1f);
            fadeCoroutine = null;
            CompleteTransition(
                BattleActionRollPanelLifecycleState.Visible
            );
            yield break;
        }

        float elapsed = 0f;
        SetCanvasState(0f);
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetCanvasState(Mathf.Clamp01(elapsed / fadeInDuration));
            yield return null;
        }

        SetCanvasState(1f);
        fadeCoroutine = null;
        CompleteTransition(BattleActionRollPanelLifecycleState.Visible);
    }

    void BeginTerminalExit(Action hiddenCompletion)
    {
        StopTransitionCoroutine();
        transitionCompletion = hiddenCompletion;
        visible = true;
        SetCanvasState(1f);
        fadeCoroutine = StartCoroutine(TerminalHoldAndFadeOut());
    }

    IEnumerator TerminalHoldAndFadeOut()
    {
        lifecycleState = BattleActionRollPanelLifecycleState.TerminalHold;
        float elapsed = 0f;
        while (elapsed < terminalHoldDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        lifecycleState = BattleActionRollPanelLifecycleState.FadingOut;
        elapsed = 0f;
        float startAlpha = panelCanvasGroup != null
            ? panelCanvasGroup.alpha
            : 1f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = fadeOutDuration > Mathf.Epsilon
                ? Mathf.Clamp01(elapsed / fadeOutDuration)
                : 1f;
            SetCanvasState(Mathf.Lerp(startAlpha, 0f, progress));
            yield return null;
        }

        SetCanvasState(0f);
        visible = false;
        ClearFollowActors();
        SetSideViewsActive(false);
        fadeCoroutine = null;
        CompleteTransition(BattleActionRollPanelLifecycleState.Hidden);
    }

    void HideInternal()
    {
        StopTransitionCoroutine();
        transitionCompletion = null;

        visible = false;
        lifecycleState = BattleActionRollPanelLifecycleState.Hidden;
        ClearFollowActors();
        SetCanvasState(0f);
        SetSideViewsActive(false);
    }

    void ClearFollowActors()
    {
        allyFollowActor = null;
        enemyFollowActor = null;
        allyFollowHandle = null;
        enemyFollowHandle = null;
    }

    void StopTransitionCoroutine()
    {
        if (fadeCoroutine == null)
        {
            return;
        }

        StopCoroutine(fadeCoroutine);
        fadeCoroutine = null;
    }

    void CompleteTransition(
        BattleActionRollPanelLifecycleState completedState
    )
    {
        lifecycleState = completedState;
        Action completion = transitionCompletion;
        transitionCompletion = null;
        completion?.Invoke();
    }

    void SetCanvasState(float alpha)
    {
        if (panelCanvasGroup == null)
        {
            return;
        }

        panelCanvasGroup.alpha = alpha;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
    }

    void SetSideViewsActive(bool active)
    {
        if (allySideView != null)
        {
            allySideView.gameObject.SetActive(active);
        }
        if (enemySideView != null)
        {
            enemySideView.gameObject.SetActive(active);
        }
    }

    void ApplySafeArea(bool force)
    {
        if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize)
        {
            return;
        }

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;
        safeAreaRoot.anchorMin = new Vector2(
            safeArea.xMin / Screen.width,
            safeArea.yMin / Screen.height
        );
        safeAreaRoot.anchorMax = new Vector2(
            safeArea.xMax / Screen.width,
            safeArea.yMax / Screen.height
        );
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;

        if (visible)
        {
            RefreshFollowLayout();
        }
    }

    void CacheSideRects()
    {
        allySideRect = allySideView != null
            ? allySideView.transform as RectTransform
            : null;
        enemySideRect = enemySideView != null
            ? enemySideView.transform as RectTransform
            : null;
    }

    void BindFollowActors(CharacterData allyActor, CharacterData enemyActor)
    {
        bool allyChanged = !object.ReferenceEquals(
            allyFollowActor,
            allyActor
        );
        bool enemyChanged = !object.ReferenceEquals(
            enemyFollowActor,
            enemyActor
        );
        allyFollowActor = allyActor;
        enemyFollowActor = enemyActor;

        ResolveFollowContext();
        if (allyChanged || allyFollowHandle == null)
        {
            allyFollowHandle = unitViewSpawner != null
                ? unitViewSpawner.GetHandle(allyFollowActor)
                : null;
        }
        if (enemyChanged || enemyFollowHandle == null)
        {
            enemyFollowHandle = unitViewSpawner != null
                ? unitViewSpawner.GetHandle(enemyFollowActor)
                : null;
        }
    }

    void ResolveFollowContext()
    {
        if (unitViewSpawner == null)
        {
            unitViewSpawner = FindFirstObjectByType<BattleUnitViewSpawner>();
        }

        Camera resolvedCamera = unitViewSpawner != null &&
            unitViewSpawner.WorldCamera != null
                ? unitViewSpawner.WorldCamera
                : Camera.main;
        if (worldCamera != resolvedCamera)
        {
            worldCamera = resolvedCamera;
            ConfigureCanvasLayering();
        }
    }

    void ConfigureCanvasLayering()
    {
        if (overlayCanvas == null)
        {
            return;
        }

        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = BehindCharacterSortingOrder;

        int sortingLayerID = SortingLayer.NameToID(
            BehindCharacterSortingLayer
        );
        if (worldCamera == null || !SortingLayer.IsValid(sortingLayerID))
        {
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.worldCamera = null;
            return;
        }

        overlayCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        overlayCanvas.worldCamera = worldCamera;
        overlayCanvas.sortingLayerID = sortingLayerID;
        float minimumPlaneDistance = worldCamera.nearClipPlane + 0.01f;
        float maximumPlaneDistance = Mathf.Max(
            minimumPlaneDistance,
            worldCamera.farClipPlane - 0.01f
        );
        overlayCanvas.planeDistance = Mathf.Clamp(
            100f,
            minimumPlaneDistance,
            maximumPlaneDistance
        );
    }

    void RefreshFollowLayout()
    {
        if (safeAreaRoot == null)
        {
            return;
        }

        ResolveFollowContext();
        if (unitViewSpawner != null)
        {
            if (allyFollowHandle == null && allyFollowActor != null)
            {
                allyFollowHandle = unitViewSpawner.GetHandle(allyFollowActor);
            }
            if (enemyFollowHandle == null && enemyFollowActor != null)
            {
                enemyFollowHandle = unitViewSpawner.GetHandle(enemyFollowActor);
            }
        }

        if (!TryPositionSide(allySideRect, allyFollowHandle, false))
        {
            ApplyFallbackLayout(allySideRect, false);
        }
        if (!TryPositionSide(enemySideRect, enemyFollowHandle, true))
        {
            ApplyFallbackLayout(enemySideRect, true);
        }
    }

    bool TryPositionSide(
        RectTransform sideRect,
        BattleUnitViewHandle handle,
        bool enemySide
    )
    {
        if (sideRect == null || handle == null || worldCamera == null ||
            !TryGetOuterTopWorldPoint(handle, enemySide, out Vector3 worldPoint))
        {
            return false;
        }

        Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPoint);
        Camera canvasCamera = overlayCanvas != null &&
            overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? overlayCanvas.worldCamera
                : null;
        if (screenPoint.z <= 0f ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                safeAreaRoot,
                screenPoint,
                canvasCamera,
                out Vector2 localPoint))
        {
            return false;
        }

        sideRect.anchorMin = new Vector2(0.5f, 0.5f);
        sideRect.anchorMax = sideRect.anchorMin;
        sideRect.pivot = new Vector2(enemySide ? 0f : 1f, 0f);

        Vector2 desiredPosition = localPoint + new Vector2(
            enemySide ? followHorizontalGap : -followHorizontalGap,
            followVerticalGap
        );
        Rect availableRect = GetAvailableFollowRect();
        sideRect.anchoredPosition = clampToSafeArea
            ? ClampPanelPivotToRect(
                availableRect,
                sideRect.rect.size,
                sideRect.pivot,
                desiredPosition
            )
            : desiredPosition;
        return true;
    }

    Rect GetAvailableFollowRect()
    {
        Rect availableRect = safeAreaRoot.rect;
        float reservedHeight = Mathf.Clamp(
            topHudReservedHeight,
            0f,
            availableRect.height
        );
        availableRect.yMax -= reservedHeight;
        return availableRect;
    }

    static bool TryGetOuterTopWorldPoint(
        BattleUnitViewHandle handle,
        bool enemySide,
        out Vector3 worldPoint
    )
    {
        worldPoint = Vector3.zero;
        if (handle == null)
        {
            return false;
        }

        SpriteRenderer renderer = handle.WorldRenderer;
        if (renderer != null && renderer.sprite != null)
        {
            Bounds bounds = renderer.bounds;
            worldPoint = new Vector3(
                enemySide ? bounds.max.x : bounds.min.x,
                bounds.max.y,
                bounds.center.z
            );
            return true;
        }

        Transform anchor = handle.HeadUIAnchor != null
            ? handle.HeadUIAnchor
            : handle.CenterAnchor;
        if (anchor == null && handle.WorldRoot != null)
        {
            anchor = handle.WorldRoot.transform;
        }
        if (anchor == null)
        {
            return false;
        }

        worldPoint = anchor.position;
        return true;
    }

    internal static Vector2 ClampPanelPivotToRect(
        Rect containerRect,
        Vector2 panelSize,
        Vector2 pivot,
        Vector2 desiredPosition
    )
    {
        float minX = containerRect.xMin + panelSize.x * pivot.x;
        float maxX = containerRect.xMax - panelSize.x * (1f - pivot.x);
        float minY = containerRect.yMin + panelSize.y * pivot.y;
        float maxY = containerRect.yMax - panelSize.y * (1f - pivot.y);

        return new Vector2(
            ClampOrCenter(desiredPosition.x, minX, maxX),
            ClampOrCenter(desiredPosition.y, minY, maxY)
        );
    }

    static float ClampOrCenter(float value, float minimum, float maximum)
    {
        return minimum <= maximum
            ? Mathf.Clamp(value, minimum, maximum)
            : (minimum + maximum) * 0.5f;
    }

    void ApplyFallbackLayout()
    {
        ApplyFallbackLayout(allySideRect, false);
        ApplyFallbackLayout(enemySideRect, true);
    }

    void ApplyFallbackLayout(RectTransform sideRect, bool enemySide)
    {
        if (sideRect == null)
        {
            return;
        }

        sideRect.anchorMin = new Vector2(enemySide ? 0.75f : 0.25f, 1f);
        sideRect.anchorMax = sideRect.anchorMin;
        sideRect.pivot = new Vector2(0.5f, 1f);
        sideRect.anchoredPosition = new Vector2(
            0f,
            -Mathf.Max(36f, topHudReservedHeight)
        );
    }

    static BattleActionRollPanelHost ResolveOrCreateInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<BattleActionRollPanelHost>(
            FindObjectsInactive.Include
        );
        if (instance != null)
        {
            return instance;
        }

        GameObject prefab = Resources.Load<GameObject>(ResourcePath);
        if (prefab == null)
        {
            if (!missingPrefabLogged)
            {
                Debug.LogError(
                    "找不到行动 Roll 面板预设体：Resources/" +
                    ResourcePath
                );
                missingPrefabLogged = true;
            }
            return null;
        }

        GameObject created = Instantiate(prefab);
        created.name = "BattleActionRollPanel";
        instance = created.GetComponent<BattleActionRollPanelHost>();
        if (instance == null)
        {
            Debug.LogError("行动 Roll 面板预设体缺少宿主组件。");
            Destroy(created);
        }
        return instance;
    }
}
