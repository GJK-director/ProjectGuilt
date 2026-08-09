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
// 槽位只调用 HandlePointer(...)；延时、左右分流、顶层 Canvas 和分辨率适配均封装在这里。
public sealed class BattleActionSlotCardInfoPanelHost : MonoBehaviour
{
    const int OverlaySortingOrder = 32767;
    const float DefaultShowDelay = 0.25f;
    static readonly Vector2 ReferenceResolution =
        new Vector2(1920f, 1080f);

    static BattleActionSlotCardInfoPanelHost instance;

    [Header("预设体引用（可直接修改子节点 UI）")]
    [SerializeField] Canvas overlayCanvas;
    [SerializeField] RectTransform allyPanelRect;
    [SerializeField] RectTransform enemyPanelRect;
    [SerializeField] BattleActionSlotCardInfoPanelView allyPanelView;
    [SerializeField] BattleActionSlotCardInfoPanelView enemyPanelView;
    [SerializeField, Min(0f)] float showDelay = DefaultShowDelay;

    RectTransform hostRect;
    GameObject activeSource;
    BattleActionSlotCardInfoHoverRequest activeRequest;
    float showAtUnscaledTime;
    bool panelVisible;
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

        if (!request.isPointerInside)
        {
            instance?.ReceivePointerRequest(request);
            return;
        }

        BattleActionSlotCardInfoPanelHost host =
            GetOrCreateHost(request.source);
        host?.ReceivePointerRequest(request);
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
        if (activeSource == null)
        {
            CloseAndClear();
            return;
        }

        if (!panelVisible &&
            activeRequest != null &&
            activeRequest.cardState != null &&
            Time.unscaledTime >= showAtUnscaledTime)
        {
            ShowActivePanel();
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
            if (activeSource == request.source)
            {
                CloseAndClear();
            }

            return;
        }

        if (request.cardState == null)
        {
            if (activeSource == request.source)
            {
                CloseAndClear();
            }

            return;
        }

        bool targetChanged = activeSource != request.source ||
            activeRequest == null ||
            activeRequest.cardState != request.cardState ||
            activeRequest.isEnemySide != request.isEnemySide;

        activeSource = request.source;
        activeRequest = request;

        if (targetChanged)
        {
            HidePanels();
            showAtUnscaledTime = Time.unscaledTime + showDelay;
        }
        else if (panelVisible)
        {
            ShowActivePanel();
        }
    }

    void ShowActivePanel()
    {
        if (activeRequest == null || activeRequest.cardState == null)
        {
            return;
        }

        HidePanels();
        BattleActionSlotCardInfoPanelView targetView =
            activeRequest.isEnemySide
                ? enemyPanelView
                : allyPanelView;
        targetView?.ShowCard(
            activeRequest.owner,
            activeRequest.target,
            activeRequest.cardState,
            activeRequest.isEnemySide
        );
        panelVisible = targetView != null;
        KeepCanvasInFront();
        RefreshResponsiveLayout();
    }

    void CloseAndClear()
    {
        HidePanels();
        activeSource = null;
        activeRequest = null;
        panelVisible = false;
    }

    void HidePanels()
    {
        allyPanelView?.Hide();
        enemyPanelView?.Hide();
        panelVisible = false;
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
