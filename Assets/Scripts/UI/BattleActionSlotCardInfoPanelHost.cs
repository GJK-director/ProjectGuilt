using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleActionSlotCardInfoHoverRequest
{
    public readonly GameObject source;
    public readonly bool isEnemySide;
    public readonly CharacterData owner;
    public readonly CharacterData target;
    public readonly BattleCardState cardState;
    public readonly bool isPointerInside;

    public BattleActionSlotCardInfoHoverRequest(
        GameObject requestSource,
        bool enemySide,
        CharacterData cardOwner,
        CharacterData cardTarget,
        BattleCardState requestCardState,
        bool pointerInside
    )
    {
        source = requestSource;
        isEnemySide = enemySide;
        owner = cardOwner;
        target = cardTarget;
        cardState = requestCardState;
        isPointerInside = pointerInside;
    }
}

// 行动槽位卡牌详情宿主。
// 为保持既有接线，入口和请求类型仍沿用 HandlePointer / HoverRequest 命名；
// 当前实际语义为“点击槽位后按阵营分别锁定”，友方与敌方面板可以同时显示。
public sealed class BattleActionSlotCardInfoPanelHost : MonoBehaviour
{
    const int OverlaySortingOrder = 32767;
    static readonly Vector2 ReferenceResolution =
        new Vector2(1920f, 1080f);

    static BattleActionSlotCardInfoPanelHost instance;

    [Header("预设体引用（可直接修改子节点 UI）")]
    [SerializeField] Canvas overlayCanvas;
    [SerializeField] RectTransform allyPanelRect;
    [SerializeField] RectTransform enemyPanelRect;
    [SerializeField] BattleActionSlotCardInfoPanelView allyPanelView;
    [SerializeField] BattleActionSlotCardInfoPanelView enemyPanelView;

    RectTransform hostRect;
    GameObject allyActiveSource;
    GameObject enemyActiveSource;
    BattleActionSlotCardInfoHoverRequest allyActiveRequest;
    BattleActionSlotCardInfoHoverRequest enemyActiveRequest;
    int lastScreenWidth = -1;
    int lastScreenHeight = -1;
    Rect lastSafeArea;

    // 给所有行动槽位保留的唯一宿主调用接口。
    // isPointerInside=true 表示点击选择或刷新当前槽位；false 表示来源失效。
    public static void HandlePointer(
        BattleActionSlotCardInfoHoverRequest request
    )
    {
        if (request == null || request.source == null)
        {
            return;
        }

        if (!request.isPointerInside)
        {
            instance?.ReceivePointerRequest(request);
            return;
        }

        BattleActionSlotCardInfoPanelHost host =
            GetOrCreateHost(request.source);
        host?.ReceivePointerRequest(request);
    }

    public static bool IsActiveSource(GameObject source)
    {
        return instance != null &&
            source != null &&
            (instance.allyActiveSource == source ||
                instance.enemyActiveSource == source);
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
        HidePanels();
        KeepCanvasInFront();

        if (instance == null)
        {
            instance = this;
        }
    }

    void Update()
    {
        if (allyActiveRequest != null && allyActiveSource == null)
        {
            CloseSide(false);
        }

        if (enemyActiveRequest != null && enemyActiveSource == null)
        {
            CloseSide(true);
        }
    }

    void LateUpdate()
    {
        KeepCanvasInFront();
        if (HasResolutionChanged())
        {
            RefreshResponsiveLayout();
        }
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
        if (!request.isPointerInside)
        {
            if (allyActiveSource == request.source)
            {
                CloseSide(false);
            }

            if (enemyActiveSource == request.source)
            {
                CloseSide(true);
            }

            return;
        }

        if (request.cardState == null)
        {
            if (GetActiveSource(request.isEnemySide) == request.source)
            {
                CloseSide(request.isEnemySide);
            }

            return;
        }

        if (request.isEnemySide)
        {
            enemyActiveSource = request.source;
            enemyActiveRequest = request;
        }
        else
        {
            allyActiveSource = request.source;
            allyActiveRequest = request;
        }

        ShowPanel(request);
    }

    void ShowPanel(BattleActionSlotCardInfoHoverRequest request)
    {
        if (request == null || request.cardState == null)
        {
            return;
        }

        BattleActionSlotCardInfoPanelView targetView =
            request.isEnemySide
                ? enemyPanelView
                : allyPanelView;
        targetView?.ShowCard(
            request.owner,
            request.target,
            request.cardState,
            request.isEnemySide
        );
        KeepCanvasInFront();
        RefreshResponsiveLayout();
    }

    GameObject GetActiveSource(bool enemySide)
    {
        return enemySide ? enemyActiveSource : allyActiveSource;
    }

    void CloseSide(bool enemySide)
    {
        if (enemySide)
        {
            enemyPanelView?.Hide();
            enemyActiveSource = null;
            enemyActiveRequest = null;
            return;
        }

        allyPanelView?.Hide();
        allyActiveSource = null;
        allyActiveRequest = null;
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
        float panelWidth = Mathf.Clamp(rootWidth * 0.43f, 280f, 640f);
        float panelHeight = Mathf.Clamp(rootHeight * 0.34f, 220f, 360f);
        float margin = Mathf.Clamp(rootWidth * 0.0125f, 12f, 28f);

        Rect safeArea = Screen.safeArea;
        float screenWidth = Mathf.Max(1f, Screen.width);
        float screenHeight = Mathf.Max(1f, Screen.height);
        float leftInset = safeArea.xMin / screenWidth * rootWidth;
        float rightInset =
            (screenWidth - safeArea.xMax) / screenWidth * rootWidth;
        float topInset =
            (screenHeight - safeArea.yMax) / screenHeight * rootHeight;

        ConfigureSideRect(
            allyPanelRect,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(leftInset + margin, -topInset - margin),
            panelWidth,
            panelHeight
        );
        ConfigureSideRect(
            enemyPanelRect,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-rightInset - margin, -topInset - margin),
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
