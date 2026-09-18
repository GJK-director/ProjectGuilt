using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BattleSecondaryInfoStat
{
    public readonly string label;
    public readonly string value;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(label) &&
        !string.IsNullOrWhiteSpace(value);

    public BattleSecondaryInfoStat(
        string statLabel,
        string statValue
    )
    {
        label = statLabel ?? string.Empty;
        value = statValue ?? string.Empty;
    }
}

public sealed class BattleSecondaryInfoBubbleData
{
    public readonly string detailsText;

    public bool HasDetails =>
        !string.IsNullOrWhiteSpace(detailsText);

    public BattleSecondaryInfoBubbleData(string details)
    {
        detailsText = details ?? string.Empty;
    }
}

public sealed class BattleSecondaryInfoContent
{
    public readonly string title;
    public readonly string body;
    public readonly string footer;
    public readonly BattleSecondaryInfoStat primaryStat;
    public readonly BattleSecondaryInfoStat secondaryStat;
    public readonly IReadOnlyList<BattleSecondaryInfoBubbleData> buffBubbles;

    public bool HasBuffBubbles =>
        buffBubbles != null && buffBubbles.Count > 0;

    public bool HasBuffSummary =>
        HasBuffBubbles ||
        (primaryStat != null && primaryStat.IsValid) ||
        (secondaryStat != null && secondaryStat.IsValid);

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(title) &&
        !string.IsNullOrWhiteSpace(body);

    public BattleSecondaryInfoContent(
        string contentTitle,
        string contentBody,
        string contentFooter = "",
        BattleSecondaryInfoStat contentPrimaryStat = null,
        BattleSecondaryInfoStat contentSecondaryStat = null,
        IReadOnlyList<BattleSecondaryInfoBubbleData> contentBuffBubbles = null
    )
    {
        title = contentTitle ?? string.Empty;
        body = contentBody ?? string.Empty;
        footer = contentFooter ?? string.Empty;
        primaryStat = contentPrimaryStat;
        secondaryStat = contentSecondaryStat;
        buffBubbles = contentBuffBubbles;
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
    [Header("Buff Summary")]
    [SerializeField] RectTransform buffSummaryRoot;
    [SerializeField] TMP_Text stackLabelText;
    [SerializeField] TMP_Text stackValueText;
    [SerializeField] TMP_Text durationLabelText;
    [SerializeField] TMP_Text durationValueText;
    [Header("Buff Details")]
    [SerializeField] RectTransform buffDetailRoot;
    [SerializeField] RectTransform bubbleContainer;
    [SerializeField] GameObject bubbleTemplate;
    [SerializeField] VerticalLayoutGroup panelLayout;
    [SerializeField] Canvas overlayCanvas;
    [Header("Hover")]
    [SerializeField, Min(0f)] float hoverOpenDelay = 0.45f;

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
    readonly List<GameObject> buffBubbleInstances =
        new List<GameObject>();

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
        EnsureBuffDetailReferences();
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
                Time.unscaledTime + Mathf.Max(0f, hoverOpenDelay);
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

    void EnsureBuffDetailReferences()
    {
        if (panelRect == null)
        {
            return;
        }

        if (buffDetailRoot == null)
        {
            GameObject detailObject = new GameObject(
                "BuffDetailRoot",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter)
            );
            detailObject.transform.SetParent(panelRect, false);
            buffDetailRoot = detailObject.GetComponent<RectTransform>();
            ConfigureVerticalLayout(
                detailObject.GetComponent<VerticalLayoutGroup>(),
                4f
            );
            ConfigureContentSizeFitter(
                detailObject.GetComponent<ContentSizeFitter>()
            );
        }

        if (bubbleContainer == null)
        {
            GameObject containerObject = new GameObject(
                "BubbleContainer",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter)
            );
            containerObject.transform.SetParent(buffDetailRoot, false);
            bubbleContainer = containerObject.GetComponent<RectTransform>();
            ConfigureVerticalLayout(
                containerObject.GetComponent<VerticalLayoutGroup>(),
                4f
            );
            ConfigureContentSizeFitter(
                containerObject.GetComponent<ContentSizeFitter>()
            );
        }

        if (bubbleTemplate == null)
        {
            bubbleTemplate = CreateBubbleTemplate();
            bubbleTemplate.transform.SetParent(bubbleContainer, false);
        }

        EnsureBubbleText(
            bubbleTemplate.transform,
            "Source",
            BaseBodyFontSize,
            new Color32(205, 177, 104, 255)
        );
        EnsureBubbleText(
            bubbleTemplate.transform,
            "Details",
            BaseFooterFontSize,
            new Color32(235, 237, 242, 255)
        );
        SetBubbleGraphicsRaycastTarget(bubbleTemplate, false);
        bubbleTemplate.SetActive(false);
        buffDetailRoot.gameObject.SetActive(false);
    }

    GameObject CreateBubbleTemplate()
    {
        GameObject template = new GameObject(
            "BubbleTemplate",
            typeof(RectTransform),
            typeof(Image),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter)
        );
        template.layer = gameObject.layer;
        Image image = template.GetComponent<Image>();
        image.color = new Color32(74, 68, 57, 245);
        image.raycastTarget = false;
        ConfigureVerticalLayout(
            template.GetComponent<VerticalLayoutGroup>(),
            2f
        );
        ConfigureContentSizeFitter(
            template.GetComponent<ContentSizeFitter>()
        );
        return template;
    }

    void ConfigureVerticalLayout(
        VerticalLayoutGroup layout,
        float spacing
    )
    {
        if (layout == null)
        {
            return;
        }

        layout.padding = new RectOffset(8, 8, 5, 5);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    void ConfigureContentSizeFitter(ContentSizeFitter fitter)
    {
        if (fitter == null)
        {
            return;
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    TMP_Text EnsureBubbleText(
        Transform parent,
        string objectName,
        float fontSize,
        Color color
    )
    {
        Transform existing = parent.Find(objectName);
        TMP_Text text = existing != null
            ? existing.GetComponent<TMP_Text>()
            : null;
        if (text != null)
        {
            text.raycastTarget = false;
            return text;
        }

        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textObject.layer = gameObject.layer;
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI createdText =
            textObject.GetComponent<TextMeshProUGUI>();
        createdText.fontSize = fontSize;
        createdText.color = color;
        createdText.alignment = TextAlignmentOptions.TopLeft;
        createdText.overflowMode = TextOverflowModes.Overflow;
        createdText.raycastTarget = false;
        return createdText;
    }

    void SetBubbleGraphicsRaycastTarget(
        GameObject bubble,
        bool value
    )
    {
        if (bubble == null)
        {
            return;
        }

        Graphic[] graphics = bubble.GetComponentsInChildren<Graphic>(true);
        for (int index = 0; index < graphics.Length; index++)
        {
            if (graphics[index] != null)
            {
                graphics[index].raycastTarget = value;
            }
        }
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

        if (buffSummaryRoot == null)
        {
            Transform summaryTransform = panelRect.Find(
                "BuffSummaryRoot"
            );
            buffSummaryRoot = summaryTransform as RectTransform;
        }

        stackLabelText = stackLabelText ?? FindPanelText(
            "BuffSummaryRoot/StackBubble/Label"
        );
        stackValueText = stackValueText ?? FindPanelText(
            "BuffSummaryRoot/StackBubble/Value"
        );
        durationLabelText = durationLabelText ?? FindPanelText(
            "BuffSummaryRoot/DurationBubble/Label"
        );
        durationValueText = durationValueText ?? FindPanelText(
            "BuffSummaryRoot/DurationBubble/Value"
        );

        if (buffDetailRoot == null)
        {
            Transform detailTransform = panelRect.Find("BuffDetailRoot");
            buffDetailRoot = detailTransform as RectTransform;
        }

        if (bubbleContainer == null && buffDetailRoot != null)
        {
            Transform containerTransform = buffDetailRoot.Find(
                "BubbleContainer"
            );
            bubbleContainer = containerTransform as RectTransform;
        }

        if (bubbleTemplate == null && bubbleContainer != null)
        {
            Transform templateTransform = bubbleContainer.Find(
                "BubbleTemplate"
            );
            bubbleTemplate = templateTransform != null
                ? templateTransform.gameObject
                : null;
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

    TMP_Text FindPanelText(string childPath)
    {
        if (panelRect == null)
        {
            return null;
        }

        Transform child = panelRect.Find(childPath);
        return child != null
            ? child.GetComponent<TMP_Text>()
            : null;
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
        if (stackLabelText != null)
        {
            stackLabelText.font = resolvedFont;
        }
        if (stackValueText != null)
        {
            stackValueText.font = resolvedFont;
        }
        if (durationLabelText != null)
        {
            durationLabelText.font = resolvedFont;
        }
        if (durationValueText != null)
        {
            durationValueText.font = resolvedFont;
        }

        if (panelRect != null)
        {
            TMP_Text[] panelTexts =
                panelRect.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < panelTexts.Length; index++)
            {
                if (panelTexts[index] != null)
                {
                    panelTexts[index].font = resolvedFont;
                }
            }
        }
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

        bool hasBuffBubbles = content.HasBuffBubbles;
        bool hasLegacyBuffSummary =
            !hasBuffBubbles && content.HasBuffSummary;
        if (buffSummaryRoot != null)
        {
            buffSummaryRoot.gameObject.SetActive(hasLegacyBuffSummary);
        }
        if (buffDetailRoot != null)
        {
            buffDetailRoot.gameObject.SetActive(hasBuffBubbles);
        }

        if (hasBuffBubbles)
        {
            ApplyBuffBubbles(content.buffBubbles);
            footerText.text = string.Empty;
            footerText.gameObject.SetActive(false);
            ClearStat(stackLabelText, stackValueText);
            ClearStat(durationLabelText, durationValueText);
        }
        else if (hasLegacyBuffSummary)
        {
            ClearBuffBubbles();
            footerText.text = string.Empty;
            footerText.gameObject.SetActive(false);
            ApplyStat(
                content.primaryStat,
                stackLabelText,
                stackValueText
            );
            ApplyStat(
                content.secondaryStat,
                durationLabelText,
                durationValueText
            );
        }
        else
        {
            ClearBuffBubbles();
            ClearStat(
                stackLabelText,
                stackValueText
            );
            ClearStat(
                durationLabelText,
                durationValueText
            );
        }

        bool hasFooter = !string.IsNullOrWhiteSpace(content.footer);
        if (!hasBuffBubbles && !hasLegacyBuffSummary)
        {
            footerText.gameObject.SetActive(hasFooter);
            footerText.text = hasFooter ? content.footer : string.Empty;
        }

        if (panelVisible)
        {
            responsiveLayoutDirty = true;
        }
    }

    void ApplyBuffBubbles(
        IReadOnlyList<BattleSecondaryInfoBubbleData> bubbles
    )
    {
        if (bubbleContainer == null || bubbleTemplate == null)
        {
            return;
        }

        for (int index = 0; index < buffBubbleInstances.Count; index++)
        {
            if (buffBubbleInstances[index] != null)
            {
                buffBubbleInstances[index].SetActive(false);
            }
        }

        int usedCount = 0;
        if (bubbles != null)
        {
            for (int index = 0; index < bubbles.Count; index++)
            {
                BattleSecondaryInfoBubbleData data = bubbles[index];
                if (data == null || !data.HasDetails)
                {
                    continue;
                }

                GameObject bubble = GetOrCreateBuffBubble(usedCount);
                ApplyBubbleText(bubble, "Details", data.detailsText);
                SetBubbleGraphicsRaycastTarget(bubble, false);
                bubble.SetActive(true);
                usedCount++;
            }
        }

        for (int index = usedCount;
            index < buffBubbleInstances.Count;
            index++)
        {
            if (buffBubbleInstances[index] != null)
            {
                buffBubbleInstances[index].SetActive(false);
            }
        }

        RefreshBuffBubbleLayout();
    }

    GameObject GetOrCreateBuffBubble(int index)
    {
        if (index < buffBubbleInstances.Count &&
            buffBubbleInstances[index] != null)
        {
            return buffBubbleInstances[index];
        }

        GameObject bubble = Instantiate(
            bubbleTemplate,
            bubbleContainer,
            false
        );
        bubble.name = "BuffBubble_" + index;
        bubble.SetActive(false);
        buffBubbleInstances.Add(bubble);
        return bubble;
    }

    void ApplyBubbleText(
        GameObject bubble,
        string childName,
        string value
    )
    {
        if (bubble == null)
        {
            return;
        }

        Transform child = bubble.transform.Find(childName);
        TMP_Text text = child != null
            ? child.GetComponent<TMP_Text>()
            : null;
        if (text == null)
        {
            return;
        }

        bool hasValue = !string.IsNullOrWhiteSpace(value);
        text.text = hasValue ? value : string.Empty;
        text.gameObject.SetActive(hasValue);
        if (hasValue)
        {
            text.ForceMeshUpdate(true);
        }
    }

    void RefreshBuffBubbleLayout()
    {
        if (bubbleContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleContainer);
        }

        if (buffDetailRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(buffDetailRoot);
        }
    }

    void ClearBuffBubbles()
    {
        for (int index = 0; index < buffBubbleInstances.Count; index++)
        {
            if (buffBubbleInstances[index] != null)
            {
                buffBubbleInstances[index].SetActive(false);
            }
        }
    }

    void ApplyStat(
        BattleSecondaryInfoStat stat,
        TMP_Text labelText,
        TMP_Text valueText
    )
    {
        bool hasStat = stat != null && stat.IsValid;
        if (labelText != null)
        {
            labelText.text = hasStat ? stat.label : string.Empty;
            labelText.gameObject.SetActive(hasStat);
        }
        if (valueText != null)
        {
            valueText.text = hasStat ? stat.value : string.Empty;
            valueText.gameObject.SetActive(hasStat);
        }

        Transform bubble = labelText != null
            ? labelText.transform.parent
            : valueText != null
                ? valueText.transform.parent
                : null;
        if (bubble != null)
        {
            bubble.gameObject.SetActive(hasStat);
        }
    }

    void ClearStat(TMP_Text labelText, TMP_Text valueText)
    {
        if (labelText != null)
        {
            labelText.text = string.Empty;
            labelText.gameObject.SetActive(false);
        }
        if (valueText != null)
        {
            valueText.text = string.Empty;
            valueText.gameObject.SetActive(false);
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
