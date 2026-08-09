using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BattleSecondaryInfoContent
{
    public readonly string title;
    public readonly string body;
    public readonly string footer;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(title) &&
        !string.IsNullOrWhiteSpace(body);

    public BattleSecondaryInfoContent(
        string contentTitle,
        string contentBody,
        string contentFooter = ""
    )
    {
        title = contentTitle ?? string.Empty;
        body = contentBody ?? string.Empty;
        footer = contentFooter ?? string.Empty;
    }
}

public struct BattleSecondaryInfoHoverRequest
{
    public GameObject source;
    public string targetKey;
    public BattleSecondaryInfoContent content;
    public Vector2 pointerScreenPosition;
    public bool isPointerInside;

    public BattleSecondaryInfoHoverRequest(
        GameObject requestSource,
        string requestTargetKey,
        BattleSecondaryInfoContent requestContent,
        Vector2 requestPointerScreenPosition,
        bool pointerInside
    )
    {
        source = requestSource;
        targetKey = requestTargetKey ?? string.Empty;
        content = requestContent;
        pointerScreenPosition = requestPointerScreenPosition;
        isPointerInside = pointerInside;
    }
}

// 二级信息面板宿主。
// 卡牌关键词与状态图标都只通过 HandlePointer(...) 进入本宿主，
// 面板创建、延时、定位和关闭逻辑不向业务 View 暴露第二套接口。
public sealed class BattleSecondaryInfoPanelHost : MonoBehaviour
{
    const float DefaultShowDelay = 0.45f;
    const float DefaultCloseGrace = 0.12f;
    const int OverlaySortingOrder = 32767;
    const float ReferenceScreenHeight = 1080f;
    const float MinPanelWidth = 300f;
    const float MaxPanelWidth = 440f;
    const float NarrowScreenMinPanelWidth = 180f;
    const float PanelWidthRatio = 0.28f;
    const float ScreenMargin = 16f;
    const float PointerGap = 18f;
    const float MaxLowResolutionCompensation = 2f;
    const float BaseTitleFontSize = 22f;
    const float BaseBodyFontSize = 17f;
    const float BaseFooterFontSize = 14f;
    static readonly Vector2 OverlayReferenceResolution =
        new Vector2(1920f, 1080f);

    static BattleSecondaryInfoPanelHost instance;

    [Header("预设体引用（可直接修改子节点 UI）")]
    [SerializeField] RectTransform panelRect;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] TMP_Text footerText;
    [SerializeField] VerticalLayoutGroup panelLayout;
    [SerializeField] Canvas overlayCanvas;

    RectTransform hostRect;
    Canvas sourceRootCanvas;

    GameObject activeSource;
    string activeTargetKey = string.Empty;
    BattleSecondaryInfoContent activeContent;
    Vector2 activePointerScreenPosition;
    bool sourceHovered;
    bool panelHovered;
    bool panelVisible;
    float showAtUnscaledTime;
    float closeAtUnscaledTime;
    float currentPanelWidth = MaxPanelWidth;
    float currentResolutionCompensation = 1f;
    int lastScreenWidth = -1;
    int lastScreenHeight = -1;
    Rect lastSafeArea;
    Vector2 lastHostSize = new Vector2(-1f, -1f);
    bool responsiveLayoutDirty = true;

    // 给所有二级信息触发源保留的唯一宿主调用接口。
    public static void HandlePointer(
        BattleSecondaryInfoHoverRequest request
    )
    {
        if (request.source == null)
        {
            return;
        }

        if (!request.isPointerInside)
        {
            instance?.ReceivePointerRequest(request);
            return;
        }

        BattleSecondaryInfoPanelHost host =
            GetOrCreateHost(request.source);
        host?.ReceivePointerRequest(request);
    }

    static BattleSecondaryInfoPanelHost GetOrCreateHost(
        GameObject source
    )
    {
        Canvas sourceCanvas = source.GetComponentInParent<Canvas>();
        if (sourceCanvas == null)
        {
            return null;
        }

        Canvas sourceRootCanvas = sourceCanvas.rootCanvas;
        if (instance != null)
        {
            instance.Initialize(sourceRootCanvas);
            return instance;
        }

        GameObject hostObject = new GameObject(
            "BattleSecondaryInfoPanelHost",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        hostObject.layer = sourceRootCanvas.gameObject.layer;

        Canvas createdCanvas = hostObject.GetComponent<Canvas>();
        createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        createdCanvas.targetDisplay = sourceRootCanvas.targetDisplay;
        createdCanvas.overrideSorting = true;
        createdCanvas.sortingOrder = OverlaySortingOrder;

        CanvasScaler createdScaler =
            hostObject.GetComponent<CanvasScaler>();
        createdScaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        createdScaler.referenceResolution =
            OverlayReferenceResolution;
        createdScaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        createdScaler.matchWidthOrHeight = 0.5f;
        createdScaler.referencePixelsPerUnit = 100f;

        instance = hostObject.AddComponent<
            BattleSecondaryInfoPanelHost
        >();
        instance.Initialize(sourceRootCanvas);
        return instance;
    }

    void Awake()
    {
        hostRect = transform as RectTransform;
        if (overlayCanvas == null)
        {
            overlayCanvas = GetComponent<Canvas>();
        }

        ResolvePrefabReferences();
        BuildPanel();
        EnsurePanelPointerRelay();
        HidePanelOnly();
        KeepCanvasInFront();

        if (instance == null)
        {
            instance = this;
        }
    }

    void Initialize(Canvas sourceCanvas)
    {
        bool sourceChanged = sourceRootCanvas != sourceCanvas;
        sourceRootCanvas = sourceCanvas;
        if (overlayCanvas != null && sourceRootCanvas != null)
        {
            int sourceDisplay = sourceRootCanvas.targetDisplay;
            if (overlayCanvas.targetDisplay != sourceDisplay)
            {
                overlayCanvas.targetDisplay = sourceDisplay;
                sourceChanged = true;
            }
        }

        if (sourceChanged)
        {
            responsiveLayoutDirty = true;
        }
    }

    void Update()
    {
        if (activeSource == null)
        {
            sourceHovered = false;
        }

        float now = Time.unscaledTime;
        if (!panelVisible &&
            sourceHovered &&
            activeContent != null &&
            activeContent.IsValid &&
            now >= showAtUnscaledTime)
        {
            ShowPanel();
        }

        if (panelVisible &&
            !sourceHovered &&
            !panelHovered &&
            now >= closeAtUnscaledTime)
        {
            CloseAndClear();
        }
    }

    void LateUpdate()
    {
        KeepCanvasInFront();

        if (!panelVisible)
        {
            return;
        }

        if (responsiveLayoutDirty ||
            HasResolutionOrSafeAreaChanged())
        {
            RefreshResponsiveLayoutAndPosition();
        }
    }

    void OnRectTransformDimensionsChange()
    {
        responsiveLayoutDirty = true;
    }

    void ReceivePointerRequest(
        BattleSecondaryInfoHoverRequest request
    )
    {
        int requestSourceID = request.source.GetInstanceID();
        int activeSourceID = activeSource != null
            ? activeSource.GetInstanceID()
            : 0;
        string requestKey = request.targetKey ?? string.Empty;

        if (!request.isPointerInside)
        {
            if (requestSourceID == activeSourceID &&
                requestKey == activeTargetKey)
            {
                sourceHovered = false;
                ScheduleClose();
            }

            return;
        }

        if (request.content == null ||
            !request.content.IsValid)
        {
            return;
        }

        bool targetChanged =
            requestSourceID != activeSourceID ||
            requestKey != activeTargetKey;

        activeSource = request.source;
        activeTargetKey = requestKey;
        activeContent = request.content;
        activePointerScreenPosition =
            request.pointerScreenPosition;
        sourceHovered = true;
        closeAtUnscaledTime = float.PositiveInfinity;

        if (targetChanged)
        {
            ApplySourceFont(request.source);
            panelHovered = false;
            HidePanelOnly();
            showAtUnscaledTime =
                Time.unscaledTime + DefaultShowDelay;
        }
    }

    void BuildPanel()
    {
        if (HasCompletePanelReferences())
        {
            return;
        }

        GameObject panelObject = new GameObject(
            "SecondaryInfoPanel",
            typeof(RectTransform),
            typeof(Image),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter),
            typeof(Outline)
        );
        panelObject.layer = gameObject.layer;
        panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.SetParent(transform, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(MaxPanelWidth, 160f);

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color32(24, 27, 35, 248);
        background.raycastTarget = true;

        Outline outline = panelObject.GetComponent<Outline>();
        outline.effectColor = new Color32(205, 177, 104, 210);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        panelLayout =
            panelObject.GetComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(18, 18, 14, 14);
        panelLayout.spacing = 8f;
        panelLayout.childAlignment = TextAnchor.UpperLeft;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter =
            panelObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit =
            ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        titleText = CreateText(
            "Title",
            BaseTitleFontSize,
            new Color32(238, 210, 137, 255),
            FontStyles.Bold
        );
        bodyText = CreateText(
            "Body",
            BaseBodyFontSize,
            new Color32(235, 237, 242, 255),
            FontStyles.Normal
        );
        footerText = CreateText(
            "Footer",
            BaseFooterFontSize,
            new Color32(167, 174, 190, 255),
            FontStyles.Normal
        );

        BattleSecondaryInfoPanelPointerRelay relay =
            panelObject.AddComponent<
                BattleSecondaryInfoPanelPointerRelay
            >();
        relay.Bind(this);

        panelObject.SetActive(false);
    }

    void ResolvePrefabReferences()
    {
        if (panelRect == null)
        {
            Transform panelTransform = transform.Find("SecondaryInfoPanel");
            panelRect = panelTransform as RectTransform;
        }

        if (panelRect == null)
        {
            return;
        }

        if (panelLayout == null)
        {
            panelLayout = panelRect.GetComponent<VerticalLayoutGroup>();
        }

        TMP_Text[] texts = panelRect.GetComponentsInChildren<TMP_Text>(true);
        for (int index = 0; index < texts.Length; index++)
        {
            TMP_Text text = texts[index];
            if (text == null)
            {
                continue;
            }

            if (titleText == null && text.gameObject.name == "Title")
            {
                titleText = text;
            }
            else if (bodyText == null && text.gameObject.name == "Body")
            {
                bodyText = text;
            }
            else if (footerText == null && text.gameObject.name == "Footer")
            {
                footerText = text;
            }
        }
    }

    bool HasCompletePanelReferences()
    {
        return panelRect != null &&
            panelLayout != null &&
            titleText != null &&
            bodyText != null &&
            footerText != null;
    }

    void EnsurePanelPointerRelay()
    {
        if (panelRect == null)
        {
            return;
        }

        BattleSecondaryInfoPanelPointerRelay relay =
            panelRect.GetComponent<BattleSecondaryInfoPanelPointerRelay>();
        if (relay == null)
        {
            relay = panelRect.gameObject.AddComponent<
                BattleSecondaryInfoPanelPointerRelay
            >();
        }

        relay.Bind(this);
    }

    TMP_Text CreateText(
        string objectName,
        float fontSize,
        Color color,
        FontStyles fontStyle
    )
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textObject.layer = gameObject.layer;
        RectTransform textRect =
            textObject.GetComponent<RectTransform>();
        textRect.SetParent(panelRect, false);

        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.richText = true;
        return text;
    }

    void ApplySourceFont(GameObject source)
    {
        if (source == null)
        {
            return;
        }

        TMP_Text sourceText =
            source.GetComponentInChildren<TMP_Text>(true);
        TMP_FontAsset resolvedFont = sourceText != null
            ? sourceText.font
            : null;

        // 状态图标里的层数文字可能仍使用默认西文字体。
        // 优先复用当前 Canvas 已加载的中文字体，避免详情面板出现方框字。
        if (!IsPreferredChineseFont(resolvedFont) &&
            sourceRootCanvas != null)
        {
            TMP_Text[] canvasTexts =
                sourceRootCanvas.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0;
                index < canvasTexts.Length;
                index++)
            {
                TMP_FontAsset candidate = canvasTexts[index] != null
                    ? canvasTexts[index].font
                    : null;
                if (IsPreferredChineseFont(candidate))
                {
                    resolvedFont = candidate;
                    break;
                }
            }
        }

        if (resolvedFont == null)
        {
            return;
        }

        titleText.font = resolvedFont;
        bodyText.font = resolvedFont;
        footerText.font = resolvedFont;
    }

    bool IsPreferredChineseFont(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null || string.IsNullOrEmpty(fontAsset.name))
        {
            return false;
        }

        return fontAsset.name.IndexOf(
            "CN",
            System.StringComparison.OrdinalIgnoreCase
        ) >= 0 ||
        fontAsset.name.IndexOf(
            "SIMHEI",
            System.StringComparison.OrdinalIgnoreCase
        ) >= 0;
    }

    void ShowPanel()
    {
        if (panelRect == null ||
            activeContent == null ||
            !activeContent.IsValid)
        {
            return;
        }

        ApplyContent(activeContent);
        panelRect.gameObject.SetActive(true);
        panelVisible = true;
        responsiveLayoutDirty = true;
        KeepCanvasInFront();
        RefreshResponsiveLayoutAndPosition();
    }

    void ApplyContent(BattleSecondaryInfoContent content)
    {
        if (content == null)
        {
            return;
        }

        titleText.text = content.title;
        bodyText.text = content.body;
        bool hasFooter = !string.IsNullOrWhiteSpace(content.footer);
        footerText.gameObject.SetActive(hasFooter);
        footerText.text = hasFooter ? content.footer : string.Empty;

        if (panelVisible)
        {
            responsiveLayoutDirty = true;
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
            for (int index = 1;
                index < sortingLayers.Length;
                index++)
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

    bool HasResolutionOrSafeAreaChanged()
    {
        if (hostRect == null)
        {
            return false;
        }

        Vector2 hostSize = hostRect.rect.size;
        Rect safeArea = Screen.safeArea;
        return Screen.width != lastScreenWidth ||
            Screen.height != lastScreenHeight ||
            (hostSize - lastHostSize).sqrMagnitude > 0.01f ||
            !ApproximatelyEqual(safeArea, lastSafeArea);
    }

    bool ApproximatelyEqual(Rect left, Rect right)
    {
        return Mathf.Abs(left.x - right.x) < 0.01f &&
            Mathf.Abs(left.y - right.y) < 0.01f &&
            Mathf.Abs(left.width - right.width) < 0.01f &&
            Mathf.Abs(left.height - right.height) < 0.01f;
    }

    void RefreshResponsiveLayoutAndPosition()
    {
        if (!panelVisible ||
            hostRect == null ||
            panelRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        Rect safeBounds = GetSafeBoundsLocal();
        ApplyResponsiveLayout(safeBounds);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        PositionPanel(activePointerScreenPosition, safeBounds);

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastSafeArea = Screen.safeArea;
        lastHostSize = hostRect.rect.size;
        responsiveLayoutDirty = false;
    }

    void ApplyResponsiveLayout(Rect safeBounds)
    {
        float canvasScaleFactor =
            overlayCanvas != null && overlayCanvas.scaleFactor > 0f
                ? overlayCanvas.scaleFactor
                : Mathf.Max(1f, Screen.height) /
                    ReferenceScreenHeight;
        currentResolutionCompensation = Mathf.Clamp(
            1f / Mathf.Max(0.01f, canvasScaleFactor),
            1f,
            MaxLowResolutionCompensation
        );

        float responsiveMargin =
            ScreenMargin * currentResolutionCompensation;
        float availableWidth = Mathf.Max(
            1f,
            safeBounds.width - responsiveMargin * 2f
        );
        float baseWidth = Mathf.Clamp(
            safeBounds.width * PanelWidthRatio,
            MinPanelWidth,
            MaxPanelWidth
        );
        float desiredWidth =
            baseWidth * currentResolutionCompensation;
        float minimumWidth = Mathf.Min(
            NarrowScreenMinPanelWidth,
            availableWidth
        );
        currentPanelWidth = Mathf.Clamp(
            desiredWidth,
            minimumWidth,
            availableWidth
        );

        float narrowScreenFactor = Mathf.Clamp(
            currentPanelWidth / MinPanelWidth,
            0.75f,
            1f
        );
        float textScale =
            currentResolutionCompensation * narrowScreenFactor;

        panelRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            currentPanelWidth
        );
        titleText.fontSize = BaseTitleFontSize * textScale;
        bodyText.fontSize = BaseBodyFontSize * textScale;
        footerText.fontSize = BaseFooterFontSize * textScale;

        if (panelLayout != null)
        {
            int horizontalPadding = Mathf.RoundToInt(
                18f * currentResolutionCompensation
            );
            int verticalPadding = Mathf.RoundToInt(
                14f * currentResolutionCompensation
            );
            panelLayout.padding = new RectOffset(
                horizontalPadding,
                horizontalPadding,
                verticalPadding,
                verticalPadding
            );
            panelLayout.spacing =
                8f * currentResolutionCompensation;
        }
    }

    Rect GetSafeBoundsLocal()
    {
        Rect hostBounds = hostRect != null
            ? hostRect.rect
            : new Rect();
        Rect safeArea = Screen.safeArea;
        if (hostRect == null ||
            safeArea.width <= 0f ||
            safeArea.height <= 0f)
        {
            return hostBounds;
        }

        Vector2 safeMin;
        Vector2 safeMax;
        bool hasMin =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                hostRect,
                safeArea.min,
                null,
                out safeMin
            );
        bool hasMax =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                hostRect,
                safeArea.max,
                null,
                out safeMax
            );
        if (!hasMin || !hasMax)
        {
            return hostBounds;
        }

        Rect safeBounds = Rect.MinMaxRect(
            Mathf.Max(hostBounds.xMin, safeMin.x),
            Mathf.Max(hostBounds.yMin, safeMin.y),
            Mathf.Min(hostBounds.xMax, safeMax.x),
            Mathf.Min(hostBounds.yMax, safeMax.y)
        );
        return safeBounds.width > 0f && safeBounds.height > 0f
            ? safeBounds
            : hostBounds;
    }

    void PositionPanel(Vector2 screenPosition, Rect safeBounds)
    {
        if (hostRect == null || panelRect == null)
        {
            return;
        }

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hostRect,
            screenPosition,
            null,
            out localPoint
        ))
        {
            return;
        }

        float panelHeight = Mathf.Max(
            1f,
            LayoutUtility.GetPreferredHeight(panelRect)
        );
        float margin =
            ScreenMargin * currentResolutionCompensation;
        float gap = PointerGap * currentResolutionCompensation;

        float minX = safeBounds.xMin + margin;
        float maxX = Mathf.Max(
            minX,
            safeBounds.xMax - currentPanelWidth - margin
        );
        float rightX = localPoint.x + gap;
        float leftX =
            localPoint.x - gap - currentPanelWidth;
        bool fitsRight =
            rightX + currentPanelWidth <=
                safeBounds.xMax - margin;
        bool fitsLeft = leftX >= minX;
        float spaceRight =
            safeBounds.xMax - localPoint.x;
        float spaceLeft =
            localPoint.x - safeBounds.xMin;
        float x = fitsRight
            ? rightX
            : fitsLeft
                ? leftX
                : spaceRight >= spaceLeft
                    ? rightX
                    : leftX;
        x = Mathf.Clamp(x, minX, maxX);

        float maxTopY = safeBounds.yMax - margin;
        float minTopY =
            safeBounds.yMin + panelHeight + margin;
        float belowTopY = localPoint.y - gap;
        float aboveTopY =
            localPoint.y + gap + panelHeight;
        bool fitsBelow =
            belowTopY - panelHeight >=
                safeBounds.yMin + margin;
        bool fitsAbove = aboveTopY <= maxTopY;
        float y = fitsBelow
            ? belowTopY
            : fitsAbove
                ? aboveTopY
                : maxTopY;
        if (minTopY <= maxTopY)
        {
            y = Mathf.Clamp(y, minTopY, maxTopY);
        }
        else
        {
            y = maxTopY;
        }

        panelRect.anchoredPosition = new Vector2(x, y);
    }

    internal void SetPanelPointerInside(bool pointerInside)
    {
        panelHovered = pointerInside;
        if (pointerInside)
        {
            closeAtUnscaledTime = float.PositiveInfinity;
            return;
        }

        ScheduleClose();
    }

    void ScheduleClose()
    {
        if (!sourceHovered && !panelHovered)
        {
            closeAtUnscaledTime =
                Time.unscaledTime + DefaultCloseGrace;
        }
    }

    void HidePanelOnly()
    {
        panelVisible = false;
        if (panelRect != null)
        {
            panelRect.gameObject.SetActive(false);
        }
    }

    void CloseAndClear()
    {
        HidePanelOnly();
        activeSource = null;
        activeTargetKey = string.Empty;
        activeContent = null;
        sourceHovered = false;
        panelHovered = false;
        showAtUnscaledTime = float.PositiveInfinity;
        closeAtUnscaledTime = float.PositiveInfinity;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

internal sealed class BattleSecondaryInfoPanelPointerRelay :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    BattleSecondaryInfoPanelHost host;

    internal void Bind(BattleSecondaryInfoPanelHost panelHost)
    {
        host = panelHost;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        host?.SetPanelPointerInside(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        host?.SetPanelPointerInside(false);
    }
}
