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
    private sealed class BackgroundBinding
    {
        public string backgroundId = string.Empty;
        public Sprite sprite = null;
        public Sprite overlaySprite = null;
        public Vector2 overlayAnchoredPosition = Vector2.zero;
        public Vector2 overlaySize = Vector2.zero;
        public Sprite foregroundSprite = null;
        public Vector2 foregroundAnchoredPosition = Vector2.zero;
        public Vector2 foregroundSize = Vector2.zero;
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
    [SerializeField] private Image backgroundOverlayImage = null;
    [SerializeField] private Image backgroundForegroundImage = null;
    [SerializeField] private Text backgroundLabel = null;
    [SerializeField] private Text speakerText = null;
    [SerializeField] private Text dialogueText = null;
    [SerializeField] private Text continueText = null;
    [SerializeField] private Text statusText = null;

    [Header("Background Assets")]
    [SerializeField] private List<BackgroundBinding> backgroundBindings =
        new List<BackgroundBinding>();

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

        EnsureBackgroundLayerImages();
        BindSceneButtons();
        storyFacade.StoryStarted += HandleStoryStarted;
        storyFacade.StoryEnded += HandleStoryEnded;
        storyFacade.StoryError += HandleStoryError;

        // 面板本身不主动决定播放哪条剧情，宿主只需调用 OpenStoryPanel(storyId)。
        SetStatus("等待宿主打开剧情面板", false);
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
        bool hasSprite = TryGetBackgroundBinding(backgroundId, out binding);
        Sprite sprite = hasSprite ? binding.sprite : null;
        bool isBlack = string.Equals(
            backgroundId,
            "black",
            StringComparison.OrdinalIgnoreCase
        );

        if (backgroundLabel != null)
        {
            backgroundLabel.gameObject.SetActive(!hasSprite && !isBlack);

            if (!hasSprite && !isBlack)
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
            ApplyBackground(binding, sprite, hasSprite, isBlack, 1f);
            return;
        }

        backgroundTransition = StartCoroutine(
            FadeBackground(binding, sprite, hasSprite, isBlack, safeFadeSeconds)
        );
    }

    private IEnumerator FadeBackground(
        BackgroundBinding binding,
        Sprite sprite,
        bool hasSprite,
        bool isBlack,
        float fadeSeconds
    )
    {
        float halfDuration = Mathf.Max(0.01f, fadeSeconds * 0.5f);
        float elapsed = 0f;
        Color startColor = backgroundImage.color;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, elapsed / halfDuration);
            backgroundImage.color = color;
            SetBackgroundLayerAlpha(color.a);
            yield return null;
        }

        ApplyBackground(binding, sprite, hasSprite, isBlack, 0f);
        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Color color = backgroundImage.color;
            color.a = Mathf.Clamp01(elapsed / halfDuration);
            backgroundImage.color = color;
            SetBackgroundLayerAlpha(color.a);
            yield return null;
        }

        Color completedColor = backgroundImage.color;
        completedColor.a = 1f;
        backgroundImage.color = completedColor;
        SetBackgroundLayerAlpha(1f);
        backgroundTransition = null;
    }

    private void ApplyBackground(
        BackgroundBinding binding,
        Sprite sprite,
        bool hasSprite,
        bool isBlack,
        float alpha
    )
    {
        backgroundImage.sprite = hasSprite ? sprite : null;
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.preserveAspect = hasSprite;

        Color color = ResolveFallbackBackgroundColor(hasSprite, isBlack);
        color.a = Mathf.Clamp01(alpha);
        backgroundImage.color = color;
        ApplyBackgroundLayers(binding, alpha);
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

        foreach (BackgroundBinding binding in backgroundBindings)
        {
            if (binding != null &&
                binding.sprite != null &&
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

    // 只有少数 CG 需要叠层（当前为手机在后、手在前）。运行时创建，避免
    // 把每一种可能差分都固化到预制体层级中。
    private void EnsureBackgroundLayerImages()
    {
        if (backgroundImage == null)
        {
            return;
        }

        Transform parent = backgroundImage.transform.parent;

        if (parent == null)
        {
            return;
        }

        if (backgroundOverlayImage == null)
        {
            backgroundOverlayImage = CreateBackgroundLayerImage(
                parent,
                "StoryBackgroundOverlay",
                backgroundImage.transform.GetSiblingIndex() + 1
            );
        }

        if (backgroundForegroundImage == null)
        {
            backgroundForegroundImage = CreateBackgroundLayerImage(
                parent,
                "StoryBackgroundForeground",
                backgroundOverlayImage.transform.GetSiblingIndex() + 1
            );
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
        EnsureBackgroundLayerImages();
        ApplyBackgroundLayer(
            backgroundOverlayImage,
            binding != null ? binding.overlaySprite : null,
            binding != null ? binding.overlayAnchoredPosition : Vector2.zero,
            binding != null ? binding.overlaySize : Vector2.zero,
            alpha
        );
        ApplyBackgroundLayer(
            backgroundForegroundImage,
            binding != null ? binding.foregroundSprite : null,
            binding != null ? binding.foregroundAnchoredPosition : Vector2.zero,
            binding != null ? binding.foregroundSize : Vector2.zero,
            alpha
        );
    }

    private static void ApplyBackgroundLayer(
        Image layerImage,
        Sprite sprite,
        Vector2 anchoredPosition,
        Vector2 size,
        float alpha
    )
    {
        bool visible = layerImage != null && sprite != null;

        if (layerImage == null)
        {
            return;
        }

        layerImage.gameObject.SetActive(visible);

        if (!visible)
        {
            return;
        }

        RectTransform layerRect = layerImage.rectTransform;
        layerRect.anchorMin = new Vector2(0.5f, 0.5f);
        layerRect.anchorMax = new Vector2(0.5f, 0.5f);
        layerRect.pivot = new Vector2(0.5f, 0.5f);
        layerRect.anchoredPosition = anchoredPosition;
        layerRect.sizeDelta = size;
        layerImage.sprite = sprite;

        Color color = Color.white;
        color.a = Mathf.Clamp01(alpha);
        layerImage.color = color;
    }

    private void SetBackgroundLayerAlpha(float alpha)
    {
        SetBackgroundLayerAlpha(backgroundOverlayImage, alpha);
        SetBackgroundLayerAlpha(backgroundForegroundImage, alpha);
    }

    private static void SetBackgroundLayerAlpha(Image layerImage, float alpha)
    {
        if (layerImage == null || !layerImage.gameObject.activeSelf)
        {
            return;
        }

        Color color = layerImage.color;
        color.a = Mathf.Clamp01(alpha);
        layerImage.color = color;
    }

    private static Color ResolveFallbackBackgroundColor(bool hasSprite, bool isBlack)
    {
        if (hasSprite)
        {
            return Color.white;
        }

        return isBlack
            ? Color.black
            : new Color32(25, 29, 42, 255);
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
