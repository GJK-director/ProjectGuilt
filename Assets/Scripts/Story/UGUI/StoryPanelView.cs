using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectGuilt.Story
{
/// <summary>
/// Story 模块内置的 UGUI 面板。
/// 宿主无需继承 StoryViewBehaviour，只需调用 StorySceneFacade.OpenStoryPanel / CloseStoryPanel。
/// </summary>
[DisallowMultipleComponent]
public sealed class StoryPanelView : StoryViewBehaviour
{
    [Serializable]
    private sealed class BackgroundLayerBinding
    {
        [Tooltip("仅用于 Inspector 中辨认图层，不参与运行时查找。")]
        public string layerName = "Layer";

        [Tooltip("该图层使用的 Sprite；为空时跳过此层。")]
        public Sprite sprite = null;

        [Tooltip("图层在背景容器中的锚点下限。全屏层通常为 (0, 0)。")]
        public Vector2 anchorMin = Vector2.zero;

        [Tooltip("图层在背景容器中的锚点上限。全屏层通常为 (1, 1)。")]
        public Vector2 anchorMax = Vector2.one;

        public Vector2 pivot = new Vector2(0.5f, 0.5f);
        public Vector2 anchoredPosition = Vector2.zero;
        public Vector2 sizeDelta = Vector2.zero;
        public Color tint = Color.white;
        public bool preserveAspect = true;
    }

    [Serializable]
    private sealed class BackgroundBinding
    {
        [Tooltip("必须与 Story JSON 中 ChangeBackground 的 backgroundId 一致。")]
        public string backgroundId = string.Empty;

        [Tooltip("按列表顺序从后向前叠加；Element 0 是最底层，层数不限。")]
        public List<BackgroundLayerBinding> layers =
            new List<BackgroundLayerBinding>();
    }

    [Serializable]
    private sealed class TypographySettings
    {
        [Tooltip("StoryPanel 下全部 UGUI Text 使用的字体。为空时保留各 Text 当前字体。")]
        public Font font = null;

        [Min(1)] public int speakerFontSize = 28;
        [Min(1)] public int dialogueFontSize = 27;
        [Min(1)] public int choiceFontSize = 24;
        [Min(1)] public int portraitFontSize = 28;
        [Min(1)] public int controlFontSize = 18;
        [Min(1)] public int historyFontSize = 22;
        [Min(1)] public int endFontSize = 30;
        [Min(1)] public int statusFontSize = 18;
        [Min(1)] public int backgroundLabelFontSize = 18;
    }

    [Serializable]
    private sealed class ImageAssetBinding
    {
        [Tooltip("仅用于 Inspector 中辨认素材槽位。")]
        public string slotName = "Image Slot";
        public Image target = null;
        public Sprite sprite = null;
        public Material material = null;
        public Color color = Color.white;
        public Image.Type imageType = Image.Type.Sliced;
        public bool preserveAspect = false;
    }

    [Serializable]
    private sealed class PortraitBinding
    {
        public GameObject root = null;
        public Image panel = null;
        public Text label = null;
    }

    [Serializable]
    private sealed class ChoiceBinding
    {
        public GameObject root = null;
        public Button button = null;
        public Text label = null;

        [NonSerialized] public string optionId;
    }

    [Header("Facade")]
    [SerializeField] private StorySceneFacade storyFacade = null;

    [Header("Scene Roots")]
    [SerializeField] private GameObject storyRoot = null;
    [SerializeField] private GameObject storyUiRoot = null;
    [SerializeField] private GameObject overlayRoot = null;
    [SerializeField] private GameObject choicePanel = null;
    [SerializeField] private GameObject endPanel = null;

    [Header("Background And Dialogue")]
    [SerializeField] private Image backgroundImage = null;
    [SerializeField] private Text backgroundLabel = null;
    [SerializeField] private Text speakerText = null;
    [SerializeField] private Text dialogueText = null;
    [SerializeField] private Text continueText = null;
    [SerializeField] private Text statusText = null;

    [Header("Background Assets")]
    [SerializeField] private List<BackgroundBinding> backgroundBindings =
        new List<BackgroundBinding>();

    [Header("Programmer Debug - Typography")]
    [SerializeField] private TypographySettings typography =
        new TypographySettings();

    [Header("Programmer Debug - Replaceable UI Assets")]
    [Tooltip("集中替换对话框、选项框、历史框等 Image 的 Sprite/Material/颜色。")]
    [SerializeField] private List<ImageAssetBinding> replaceableUiAssets =
        new List<ImageAssetBinding>();

    [Header("Portraits And Choices")]
    [SerializeField] private PortraitBinding leftPortrait = new PortraitBinding();
    [SerializeField] private PortraitBinding rightPortrait = new PortraitBinding();
    [SerializeField] private List<ChoiceBinding> choiceBindings = new List<ChoiceBinding>();

    [Header("Playback Buttons")]
    [SerializeField] private Button autoButton = null;
    [SerializeField] private Text autoButtonText = null;
    [SerializeField] private Button skipButton = null;
    [SerializeField] private Text skipButtonText = null;
    [SerializeField] private Button historyButton = null;
    [SerializeField] private Button skipToEndButton = null;

    [Header("Dialogue And Overlay Buttons")]
    [SerializeField] private Button advanceButton = null;
    [SerializeField] private Text historyText = null;
    [SerializeField] private Button closeHistoryButton = null;
    [SerializeField] private Text endText = null;
    [SerializeField] private Button restartButton = null;

    private static readonly Color MutedInk = new Color32(160, 173, 194, 255);
    private static readonly Color Accent = new Color32(171, 66, 78, 255);
    private static readonly Color ButtonNormal = new Color32(44, 53, 70, 245);
    private string lastStartedStoryId = string.Empty;
    private Coroutine backgroundTransition;
    private readonly List<Image> runtimeBackgroundLayers = new List<Image>();
    private readonly List<float> runtimeBackgroundLayerBaseAlphas =
        new List<float>();

    private void Awake()
    {
        if (storyFacade == null)
        {
            storyFacade = GetComponent<StorySceneFacade>();
        }

        if (storyFacade == null)
        {
            SetStatus("未绑定 StorySceneFacade", true);
            return;
        }

        ApplyInspectorVisualSettings();
        EnsureBackgroundLayerImages(1);
        BindSceneButtons();
        storyFacade.StoryStarted += HandleStoryStarted;
        storyFacade.StoryEnded += HandleStoryEnded;
        storyFacade.StoryError += HandleStoryError;

        // 面板本身不主动决定播放哪条剧情，宿主只需调用 OpenStoryPanel(storyId)。
        SetStatus("等待宿主打开剧情面板", false);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyInspectorVisualSettings();
        }
    }

    private void OnDestroy()
    {
        if (storyFacade == null)
        {
            return;
        }

        storyFacade.StoryStarted -= HandleStoryStarted;
        storyFacade.StoryEnded -= HandleStoryEnded;
        storyFacade.StoryError -= HandleStoryError;
    }

    public override void SetStoryVisible(bool visible)
    {
        if (visible)
        {
            SetActive(endPanel, false);
            SetActive(overlayRoot, false);
        }

        SetActive(storyRoot, visible);
    }

    public override void SetStoryUiVisible(bool visible)
    {
        SetActive(storyUiRoot, visible);
    }

    public override void SetOverlayOpen(bool isOpen)
    {
        SetActive(overlayRoot, isOpen);
    }

    public override void SetPlaybackMode(StoryPlaybackMode mode)
    {
        bool isAuto = mode == StoryPlaybackMode.Auto;
        bool isSkip = mode == StoryPlaybackMode.Skip;

        if (autoButtonText != null)
        {
            autoButtonText.text = isAuto ? "自动：开" : "自动";
        }

        if (skipButtonText != null)
        {
            skipButtonText.text = isSkip ? "快进：开" : "快进";
        }

        SetButtonSelected(autoButton, isAuto);
        SetButtonSelected(skipButton, isSkip);
    }

    public override void SetContinueIndicator(bool visible)
    {
        if (continueText != null)
        {
            continueText.gameObject.SetActive(visible);
        }
    }

    public override void ShowDialogue(
        string speakerName,
        string fullText,
        int visibleCharacterCount,
        bool isComplete
    )
    {
        string safeText = fullText ?? string.Empty;
        int count = Mathf.Clamp(visibleCharacterCount, 0, safeText.Length);

        if (speakerText != null)
        {
            speakerText.text = string.IsNullOrWhiteSpace(speakerName)
                ? ""
                : speakerName;
        }

        if (dialogueText != null)
        {
            dialogueText.text = safeText.Substring(0, count);
        }
    }

    public override void ShowChoices(IReadOnlyList<StoryChoiceViewData> choices)
    {
        int choiceCount = choices != null ? choices.Count : 0;
        SetActive(choicePanel, choiceCount > 0);

        for (int index = 0; index < choiceBindings.Count; index++)
        {
            ChoiceBinding binding = choiceBindings[index];
            bool shouldShow = binding != null && index < choiceCount;

            if (binding == null)
            {
                continue;
            }

            SetActive(binding.root, shouldShow);
            binding.optionId = shouldShow ? choices[index].optionId : string.Empty;

            if (!shouldShow)
            {
                continue;
            }

            if (binding.label != null)
            {
                binding.label.text = choices[index].text;
            }

            if (binding.button != null)
            {
                binding.button.interactable = choices[index].interactable;
            }
        }

        if (choiceCount > choiceBindings.Count)
        {
            SetStatus(
                "当前 Choice 有 " + choiceCount +
                " 项，但场景只配置了 " + choiceBindings.Count + " 个选项槽位",
                true
            );
        }
    }

    public override void HideChoices()
    {
        SetActive(choicePanel, false);

        foreach (ChoiceBinding binding in choiceBindings)
        {
            if (binding == null)
            {
                continue;
            }

            binding.optionId = string.Empty;
            SetActive(binding.root, false);
        }
    }

    public override void SetBackground(string backgroundId, float fadeSeconds)
    {
        if (backgroundTransition != null)
        {
            StopCoroutine(backgroundTransition);
            backgroundTransition = null;
        }

        BackgroundBinding binding;
        bool hasBackground = TryGetBackgroundBinding(backgroundId, out binding);
        bool isBlack = string.Equals(
            backgroundId,
            "black",
            StringComparison.OrdinalIgnoreCase
        );

        if (backgroundLabel != null)
        {
            backgroundLabel.gameObject.SetActive(!hasBackground && !isBlack);

            if (!hasBackground && !isBlack)
            {
                backgroundLabel.text = "背景占位 · " + (backgroundId ?? "未指定");
            }
        }

        if (backgroundImage == null)
        {
            return;
        }

        float safeFadeSeconds = Mathf.Max(0f, fadeSeconds);

        if (safeFadeSeconds <= 0f || !isActiveAndEnabled)
        {
            ApplyBackground(binding, hasBackground, isBlack, 1f);
            return;
        }

        backgroundTransition = StartCoroutine(
            FadeBackground(binding, hasBackground, isBlack, safeFadeSeconds)
        );
    }

    private IEnumerator FadeBackground(
        BackgroundBinding binding,
        bool hasBackground,
        bool isBlack,
        float fadeSeconds
    )
    {
        float halfDuration = Mathf.Max(0.01f, fadeSeconds * 0.5f);
        float elapsed = 0f;
        float startAlpha = GetCurrentBackgroundLayerAlpha();

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetBackgroundLayerAlpha(
                Mathf.Lerp(
                    startAlpha,
                    0f,
                    Mathf.Clamp01(elapsed / halfDuration)
                )
            );
            yield return null;
        }

        ApplyBackground(binding, hasBackground, isBlack, 0f);
        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetBackgroundLayerAlpha(Mathf.Clamp01(elapsed / halfDuration));
            yield return null;
        }

        SetBackgroundLayerAlpha(1f);
        backgroundTransition = null;
    }

    private void ApplyBackground(
        BackgroundBinding binding,
        bool hasBackground,
        bool isBlack,
        float alpha
    )
    {
        if (hasBackground)
        {
            ApplyBackgroundLayers(binding, alpha);
            return;
        }

        EnsureBackgroundLayerImages(1);
        ConfigureFullScreenRect(backgroundImage.rectTransform);
        backgroundImage.sprite = null;
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.preserveAspect = false;
        backgroundImage.gameObject.SetActive(true);

        Color color = ResolveFallbackBackgroundColor(isBlack);
        runtimeBackgroundLayerBaseAlphas[0] = color.a;
        color.a *= Mathf.Clamp01(alpha);
        backgroundImage.color = color;
        HideUnusedBackgroundLayers(1);
    }

    private bool TryGetBackgroundBinding(
        string backgroundId,
        out BackgroundBinding result
    )
    {
        result = null;

        if (string.IsNullOrWhiteSpace(backgroundId))
        {
            return false;
        }

        if (backgroundBindings == null)
        {
            return false;
        }

        foreach (BackgroundBinding binding in backgroundBindings)
        {
            if (binding != null &&
                HasRenderableLayer(binding) &&
                string.Equals(
                    binding.backgroundId,
                    backgroundId,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                result = binding;
                return true;
            }
        }

        return false;
    }

    private static bool HasRenderableLayer(BackgroundBinding binding)
    {
        if (binding == null || binding.layers == null)
        {
            return false;
        }

        foreach (BackgroundLayerBinding layer in binding.layers)
        {
            if (layer != null && layer.sprite != null)
            {
                return true;
            }
        }

        return false;
    }

    // Element 0 复用预制体内的 Background Image；额外层按需建立对象池。
    // 图层配置仍完全来自 Inspector，不把具体剧情素材固化进层级结构。
    private void EnsureBackgroundLayerImages(int requiredCount)
    {
        if (backgroundImage == null)
        {
            return;
        }

        if (runtimeBackgroundLayers.Count == 0)
        {
            runtimeBackgroundLayers.Add(backgroundImage);
            runtimeBackgroundLayerBaseAlphas.Add(backgroundImage.color.a);
        }

        Transform parent = backgroundImage.transform.parent;

        if (parent == null)
        {
            return;
        }

        int safeRequiredCount = Mathf.Max(1, requiredCount);

        while (runtimeBackgroundLayers.Count < safeRequiredCount)
        {
            int layerIndex = runtimeBackgroundLayers.Count;
            Image layerImage = CreateBackgroundLayerImage(
                parent,
                "StoryBackgroundLayer_" + layerIndex,
                backgroundImage.transform.GetSiblingIndex() + layerIndex
            );
            runtimeBackgroundLayers.Add(layerImage);
            runtimeBackgroundLayerBaseAlphas.Add(1f);
        }
    }

    private Image CreateBackgroundLayerImage(
        Transform parent,
        string objectName,
        int siblingIndex
    )
    {
        GameObject layerObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        layerObject.layer = backgroundImage.gameObject.layer;
        layerObject.transform.SetParent(parent, false);
        layerObject.transform.SetSiblingIndex(
            Mathf.Min(
                siblingIndex,
                parent.childCount - 1
            )
        );

        Image layerImage = layerObject.GetComponent<Image>();
        layerImage.raycastTarget = false;
        layerImage.type = Image.Type.Simple;
        layerImage.preserveAspect = true;
        layerObject.SetActive(false);
        return layerImage;
    }

    private void ApplyBackgroundLayers(BackgroundBinding binding, float alpha)
    {
        int renderableLayerCount = CountRenderableLayers(binding);
        EnsureBackgroundLayerImages(renderableLayerCount);

        int targetIndex = 0;

        foreach (BackgroundLayerBinding layer in binding.layers)
        {
            if (layer == null || layer.sprite == null)
            {
                continue;
            }

            ApplyBackgroundLayer(
                runtimeBackgroundLayers[targetIndex],
                layer,
                alpha
            );
            runtimeBackgroundLayerBaseAlphas[targetIndex] = layer.tint.a;
            targetIndex++;
        }

        HideUnusedBackgroundLayers(targetIndex);
    }

    private static void ApplyBackgroundLayer(
        Image layerImage,
        BackgroundLayerBinding layer,
        float alpha
    )
    {
        if (layerImage == null || layer == null || layer.sprite == null)
        {
            return;
        }

        layerImage.gameObject.SetActive(true);
        RectTransform layerRect = layerImage.rectTransform;
        layerRect.anchorMin = layer.anchorMin;
        layerRect.anchorMax = layer.anchorMax;
        layerRect.pivot = layer.pivot;
        layerRect.anchoredPosition = layer.anchoredPosition;
        layerRect.sizeDelta = layer.sizeDelta;
        layerImage.sprite = layer.sprite;
        layerImage.type = Image.Type.Simple;
        layerImage.preserveAspect = layer.preserveAspect;

        Color color = layer.tint;
        color.a *= Mathf.Clamp01(alpha);
        layerImage.color = color;
    }

    private void SetBackgroundLayerAlpha(float alpha)
    {
        float safeAlpha = Mathf.Clamp01(alpha);

        for (int index = 0; index < runtimeBackgroundLayers.Count; index++)
        {
            Image layerImage = runtimeBackgroundLayers[index];

            if (layerImage == null || !layerImage.gameObject.activeSelf)
            {
                continue;
            }

            Color color = layerImage.color;
            color.a = runtimeBackgroundLayerBaseAlphas[index] * safeAlpha;
            layerImage.color = color;
        }
    }

    private float GetCurrentBackgroundLayerAlpha()
    {
        for (int index = 0; index < runtimeBackgroundLayers.Count; index++)
        {
            Image layerImage = runtimeBackgroundLayers[index];
            float baseAlpha = runtimeBackgroundLayerBaseAlphas[index];

            if (layerImage != null &&
                layerImage.gameObject.activeSelf &&
                baseAlpha > 0f)
            {
                return Mathf.Clamp01(layerImage.color.a / baseAlpha);
            }
        }

        return 1f;
    }

    private static int CountRenderableLayers(BackgroundBinding binding)
    {
        int count = 0;

        if (binding == null || binding.layers == null)
        {
            return count;
        }

        foreach (BackgroundLayerBinding layer in binding.layers)
        {
            if (layer != null && layer.sprite != null)
            {
                count++;
            }
        }

        return count;
    }

    private void HideUnusedBackgroundLayers(int usedCount)
    {
        for (int index = usedCount; index < runtimeBackgroundLayers.Count; index++)
        {
            Image layerImage = runtimeBackgroundLayers[index];

            if (layerImage != null)
            {
                layerImage.sprite = null;
                layerImage.gameObject.SetActive(false);
            }
        }
    }

    private static void ConfigureFullScreenRect(RectTransform target)
    {
        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = Vector2.zero;
        target.sizeDelta = Vector2.zero;
    }

    private static Color ResolveFallbackBackgroundColor(bool isBlack)
    {
        return isBlack ? Color.black : new Color32(25, 29, 42, 255);
    }

    public override void ApplyPortraits(
        IReadOnlyList<StoryPortraitStateData> portraits,
        string activeSpeakerId
    )
    {
        SetPortraitVisible(leftPortrait, false);
        SetPortraitVisible(rightPortrait, false);

        if (portraits == null)
        {
            return;
        }

        foreach (StoryPortraitStateData portrait in portraits)
        {
            if (portrait == null || !portrait.visible)
            {
                continue;
            }

            PortraitBinding binding = string.Equals(
                portrait.positionId,
                "right",
                StringComparison.OrdinalIgnoreCase
            )
                ? rightPortrait
                : leftPortrait;
            bool isActive = string.Equals(
                portrait.characterId,
                activeSpeakerId,
                StringComparison.OrdinalIgnoreCase
            );
            ApplyPortrait(binding, portrait, isActive);
        }
    }

    public override void NotifyStoryEnded(string endedStoryId)
    {
        SetActive(endPanel, true);

        if (endText != null)
        {
            endText.text = "剧情已结束\n" + endedStoryId;
        }

        lastStartedStoryId = endedStoryId ?? string.Empty;
        SetStatus("剧情已结束，可重新播放或由宿主关闭面板", false);
    }

    private void BindSceneButtons()
    {
        AddListener(advanceButton, RequestAdvance);
        AddListener(autoButton, ToggleAuto);
        AddListener(skipButton, ToggleSkip);
        AddListener(historyButton, OpenHistory);
        AddListener(skipToEndButton, SkipToEnd);
        AddListener(closeHistoryButton, CloseHistory);
        AddListener(restartButton, RestartStory);

        for (int index = 0; index < choiceBindings.Count; index++)
        {
            int capturedIndex = index;
            ChoiceBinding binding = choiceBindings[index];

            if (binding != null && binding.button != null)
            {
                binding.button.onClick.AddListener(
                    delegate { SubmitChoice(capturedIndex); }
                );
            }
        }
    }

    private void RestartStory()
    {
        if (storyFacade == null)
        {
            SetStatus("无法重新播放：StorySceneFacade 为空", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(lastStartedStoryId))
        {
            SetStatus("无法重新播放：没有上一条剧情 ID", true);
            return;
        }

        if (!storyFacade.OpenStoryPanel(lastStartedStoryId))
        {
            SetStatus("剧情重新播放失败，请查看 Console", true);
        }
    }

    private void RequestAdvance()
    {
        if (storyFacade != null)
        {
            storyFacade.RequestAdvance();
        }
    }

    private void SubmitChoice(int bindingIndex)
    {
        if (storyFacade == null ||
            bindingIndex < 0 ||
            bindingIndex >= choiceBindings.Count)
        {
            return;
        }

        ChoiceBinding binding = choiceBindings[bindingIndex];

        if (binding != null && !string.IsNullOrWhiteSpace(binding.optionId))
        {
            storyFacade.SubmitChoice(binding.optionId);
        }
    }

    private void ToggleAuto()
    {
        if (storyFacade != null)
        {
            bool enable = autoButtonText == null || autoButtonText.text == "自动";
            storyFacade.SetAuto(enable);
        }
    }

    private void ToggleSkip()
    {
        if (storyFacade != null)
        {
            bool enable = skipButtonText == null || skipButtonText.text == "快进";
            storyFacade.SetSkip(enable);
        }
    }

    private void SkipToEnd()
    {
        if (storyFacade != null)
        {
            storyFacade.SkipToEnd();
        }
    }

    private void OpenHistory()
    {
        if (storyFacade == null || historyText == null)
        {
            return;
        }

        IReadOnlyList<StoryHistoryEntryData> entries = storyFacade.GetHistorySnapshot();
        StringBuilder builder = new StringBuilder();

        if (entries == null || entries.Count == 0)
        {
            builder.Append("当前还没有历史记录。");
        }
        else
        {
            foreach (StoryHistoryEntryData entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(entry.speakerName))
                {
                    builder.Append(entry.speakerName).Append("：");
                }

                builder.AppendLine(entry.text ?? string.Empty).AppendLine();
            }
        }

        historyText.text = builder.ToString();
        storyFacade.OpenOverlay();
    }

    private void CloseHistory()
    {
        if (storyFacade != null)
        {
            storyFacade.CloseOverlay();
        }
    }

    private void HandleStoryStarted(string startedStoryId)
    {
        lastStartedStoryId = startedStoryId ?? string.Empty;
        SetStatus("运行中：" + startedStoryId, false);
    }

    private void HandleStoryEnded(string endedStoryId)
    {
        SetStatus("已结束：" + endedStoryId, false);
    }

    private void HandleStoryError(string message)
    {
        SetStatus("错误：" + message, true);
    }

    private void ApplyInspectorVisualSettings()
    {
        ApplyTypographySettings();
        ApplyReplaceableUiAssets();
    }

    private void ApplyTypographySettings()
    {
        if (typography == null)
        {
            return;
        }

        if (typography.font != null)
        {
            Text[] allTexts = GetComponentsInChildren<Text>(true);

            foreach (Text textComponent in allTexts)
            {
                if (textComponent != null)
                {
                    textComponent.font = typography.font;
                }
            }
        }

        ApplyTextSize(speakerText, typography.speakerFontSize);
        ApplyTextSize(dialogueText, typography.dialogueFontSize);
        ApplyTextSize(continueText, typography.controlFontSize);
        ApplyTextSize(historyText, typography.historyFontSize);
        ApplyTextSize(endText, typography.endFontSize);
        ApplyTextSize(statusText, typography.statusFontSize);
        ApplyTextSize(backgroundLabel, typography.backgroundLabelFontSize);

        ApplyTextSize(
            leftPortrait != null ? leftPortrait.label : null,
            typography.portraitFontSize
        );
        ApplyTextSize(
            rightPortrait != null ? rightPortrait.label : null,
            typography.portraitFontSize
        );

        if (choiceBindings != null)
        {
            foreach (ChoiceBinding binding in choiceBindings)
            {
                if (binding != null)
                {
                    ApplyTextSize(binding.label, typography.choiceFontSize);
                }
            }
        }

        ApplyButtonTextSize(autoButton, typography.controlFontSize);
        ApplyButtonTextSize(skipButton, typography.controlFontSize);
        ApplyButtonTextSize(historyButton, typography.controlFontSize);
        ApplyButtonTextSize(skipToEndButton, typography.controlFontSize);
        ApplyButtonTextSize(closeHistoryButton, typography.controlFontSize);
        ApplyButtonTextSize(restartButton, typography.controlFontSize);
    }

    private void ApplyReplaceableUiAssets()
    {
        if (replaceableUiAssets == null)
        {
            return;
        }

        foreach (ImageAssetBinding binding in replaceableUiAssets)
        {
            if (binding == null || binding.target == null)
            {
                continue;
            }

            binding.target.sprite = binding.sprite;
            binding.target.material = binding.material;
            binding.target.color = binding.color;
            binding.target.type = binding.imageType;
            binding.target.preserveAspect = binding.preserveAspect;
        }
    }

    private static void ApplyTextSize(Text target, int fontSize)
    {
        if (target != null)
        {
            target.fontSize = Mathf.Max(1, fontSize);
        }
    }

    private static void ApplyButtonTextSize(Button button, int fontSize)
    {
        if (button == null)
        {
            return;
        }

        ApplyTextSize(button.GetComponentInChildren<Text>(true), fontSize);
    }

    private void SetStatus(string message, bool isError)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message;
        statusText.color = isError
            ? new Color32(255, 132, 132, 255)
            : MutedInk;
    }

    private static void ApplyPortrait(
        PortraitBinding binding,
        StoryPortraitStateData portrait,
        bool isActive
    )
    {
        if (binding == null)
        {
            return;
        }

        SetActive(binding.root, true);

        if (binding.panel != null)
        {
            binding.panel.color = isActive
                ? new Color32(73, 43, 53, 245)
                : new Color32(27, 35, 49, 220);
        }

        if (binding.label != null)
        {
            binding.label.text =
                GetMonogram(portrait.characterId) + "\n" +
                GetCharacterName(portrait.characterId) + "\n" +
                "表情：" +
                (string.IsNullOrWhiteSpace(portrait.expressionId)
                    ? "neutral"
                    : portrait.expressionId) +
                (isActive ? " · 发言中" : string.Empty);
        }

        if (binding.root != null)
        {
            binding.root.transform.localScale =
                Vector3.one * Mathf.Max(0.1f, portrait.scale);
        }
    }

    private static void SetPortraitVisible(PortraitBinding binding, bool visible)
    {
        if (binding != null)
        {
            SetActive(binding.root, visible);
        }
    }

    private static string GetCharacterName(string characterId)
    {
        switch (characterId)
        {
            case "lin":
                return "林默";
            case "yu":
                return "余烬";
            default:
                return string.IsNullOrWhiteSpace(characterId) ? "未知角色" : characterId;
        }
    }

    private static string GetMonogram(string characterId)
    {
        switch (characterId)
        {
            case "lin":
                return "林";
            case "yu":
                return "余";
            default:
                return "?";
        }
    }

    private static void SetButtonSelected(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.targetGraphic as Image;

        if (image != null)
        {
            image.color = selected ? Accent : ButtonNormal;
        }
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

}
}
