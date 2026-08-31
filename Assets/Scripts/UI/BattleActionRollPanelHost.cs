using System.Collections;
using UnityEngine;

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

    [Header("角色跟随")]
    [SerializeField, Min(0f)] float followHorizontalGap = 24f;
    [SerializeField, Min(0f)] float followVerticalGap = 18f;
    [SerializeField, Min(0f)] float topHudReservedHeight = 112f;
    [SerializeField] bool clampToSafeArea = true;

    Coroutine fadeCoroutine;
    bool visible;
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

    public static void ShowForActionBegin(BattlePresentationRequest request)
    {
        BattleClashSession session = GetSupportedSession(request);
        BattleResolutionPlan freeAttackPlan = GetSupportedFreeAttackPlan(
            request,
            false
        );
        if (session == null && freeAttackPlan == null)
        {
            return;
        }

        BattleActionRollPanelHost host = ResolveOrCreateInstance();
        if (host != null)
        {
            if (session != null)
            {
                host.ShowSession(session, false);
            }
            else
            {
                host.ShowOneSidedFreeAttack(freeAttackPlan, false);
            }
        }
    }

    public static void ShowForRoll(BattlePresentationRequest request)
    {
        BattleClashSession session = GetSupportedSession(request);
        BattleResolutionPlan freeAttackPlan = GetSupportedFreeAttackPlan(
            request,
            true
        );
        if (session == null && freeAttackPlan == null)
        {
            return;
        }

        BattleActionRollPanelHost host = ResolveOrCreateInstance();
        if (host != null)
        {
            if (session != null)
            {
                host.ShowSession(session, true);
            }
            else
            {
                host.ShowOneSidedFreeAttack(freeAttackPlan, true);
            }
        }
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

    void ShowSession(BattleClashSession session, bool hasRolledPoint)
    {
        ApplySafeArea(false);
        BindFollowActors(
            session.SideA != null ? session.SideA.actor : null,
            session.SideB != null ? session.SideB.actor : null
        );

        CharacterData allyTarget = session.SideB != null
            ? session.SideB.actor
            : null;
        CharacterData enemyTarget = session.SideA != null
            ? session.SideA.actor
            : null;
        bool allyShown = allySideView != null && (hasRolledPoint
            ? allySideView.ShowRoll(
                session.SideA,
                allyTarget,
                session.SideAPoint
            )
            : allySideView.ShowPending(session.SideA, allyTarget));
        bool enemyShown = enemySideView != null && (hasRolledPoint
            ? enemySideView.ShowRoll(
                session.SideB,
                enemyTarget,
                session.SideBPoint
            )
            : enemySideView.ShowPending(session.SideB, enemyTarget));
        if (!allyShown && !enemyShown)
        {
            HideInternal();
            return;
        }

        RefreshFollowLayout();

        // 平点重投时只刷新卡牌和点数，不重新播放淡入。
        if (visible)
        {
            SetCanvasState(1f);
            return;
        }

        visible = true;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    void ShowOneSidedFreeAttack(
        BattleResolutionPlan plan,
        bool hasRolledPoint
    )
    {
        if (plan == null)
        {
            return;
        }

        ApplySafeArea(false);
        BindFollowActors(plan.attacker, plan.target);
        BattleClashSideState attackerSide = new BattleClashSideState(
            plan.attacker,
            plan.sourceCardState,
            plan.freeActionPointSnapshot,
            plan.freeActionResourceSnapshot
        );
        bool allyShown = allySideView != null && (hasRolledPoint
            ? allySideView.ShowOneSidedAttackRoll(
                attackerSide,
                plan.target,
                plan.freeActionPoint
            )
            : allySideView.ShowOneSidedAttackPending(
                attackerSide,
                plan.target
            ));
        enemySideView?.Hide();
        if (!allyShown)
        {
            HideInternal();
            return;
        }

        RefreshFollowLayout();
        if (visible)
        {
            SetCanvasState(1f);
            return;
        }

        visible = true;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    static BattleClashSession GetSupportedSession(
        BattlePresentationRequest request
    )
    {
        BattleClashSession session = request != null
            ? request.ClashSession
            : null;
        return session != null &&
            session.ClashType == BattleClashType.AttackVsAttack
                ? session
                : null;
    }

    static BattleResolutionPlan GetSupportedFreeAttackPlan(
        BattlePresentationRequest request,
        bool requireRolledPoint
    )
    {
        BattleResolutionPlan plan = request != null
            ? request.ResolutionPlan
            : null;
        return plan != null &&
            plan.planKind == BattleResolutionPlanKind.FreeActionAttack &&
            plan.sourceCardState != null &&
            plan.sourceCardState.IsMeleeAttack() &&
            (!requireRolledPoint || plan.freeActionHasRolled)
                ? plan
                : null;
    }

    IEnumerator FadeIn()
    {
        if (fadeInDuration <= 0f)
        {
            SetCanvasState(1f);
            fadeCoroutine = null;
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
    }

    void HideInternal()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        visible = false;
        allyFollowActor = null;
        enemyFollowActor = null;
        allyFollowHandle = null;
        enemyFollowHandle = null;
        SetCanvasState(0f);
        SetSideViewsActive(false);
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
