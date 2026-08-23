using System.Collections;
using UnityEngine;

// 动态生成的行动 Roll 面板宿主。仅负责表现，不参与拼点规则与随机数生成。
public sealed class BattleActionRollPanelHost : MonoBehaviour
{
    const string ResourcePath = "UI/BattleActionRollPanel";

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

    Coroutine fadeCoroutine;
    bool visible;
    Rect lastSafeArea;
    Vector2Int lastScreenSize;

    public static void ShowForActionBegin(BattlePresentationRequest request)
    {
        BattleClashSession session = GetSupportedSession(request);
        if (session == null)
        {
            return;
        }

        BattleActionRollPanelHost host = ResolveOrCreateInstance();
        if (host != null)
        {
            host.ShowSession(session, false);
        }
    }

    public static void ShowForRoll(BattlePresentationRequest request)
    {
        BattleClashSession session = GetSupportedSession(request);
        if (session == null)
        {
            return;
        }

        BattleActionRollPanelHost host = ResolveOrCreateInstance();
        if (host != null)
        {
            host.ShowSession(session, true);
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
        }
        SetCanvasState(0f);
        SetSideViewsActive(false);
        ApplySafeArea(true);
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
