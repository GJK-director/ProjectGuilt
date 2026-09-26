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
    private sealed class ForegroundBinding
    {
        public string foregroundId = string.Empty;
        public Sprite sprite = null;
    }

    [Serializable]
    private sealed class BgmBinding
    {
        public string bgmId = string.Empty;
        public AudioClip clip = null;
        [Range(0f, 1f)] public float volume = 1f;
        [Min(0f)] public float loopFadeOutSeconds = 0f;
    }

    [Serializable]
    private sealed class StorySfxBinding
    {
        public string sfxId = string.Empty;
        public AudioClip clip = null;
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

    [Header("Center Screen Dialogue")]
    [SerializeField] private GameObject dialoguePanel = null;
    [SerializeField] private GameObject centerScreenPresentationRoot = null;
    [SerializeField] private Button centerScreenAdvanceButton = null;
    [SerializeField] private Text centerScreenText = null;

    [Header("Advance Input")]
    [SerializeField] private Button advanceInputSurfaceButton = null;

    [Header("Foreground Assets")]
    [SerializeField] private GameObject storyForegroundPresentationRoot = null;
    [SerializeField] private Image storyForegroundImage = null;
    [SerializeField] private List<ForegroundBinding> foregroundBindings =
        new List<ForegroundBinding>();

    [Header("Background Assets")]
    [SerializeField] private List<BackgroundBinding> backgroundBindings =
        new List<BackgroundBinding>();

    [Header("BGM Assets")]
    [SerializeField] private AudioSource bgmAudioSource = null;
    [SerializeField] private List<BgmBinding> bgmBindings =
        new List<BgmBinding>();

    [Header("Story SFX Assets")]
    [SerializeField] private List<StorySfxBinding> sfxBindings =
        new List<StorySfxBinding>();

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
    private Coroutine foregroundFadeTransition;
    private Coroutine dialogueFadeTransition;
    private Coroutine visualFadeTransition;
    private GameObject incomingBackgroundRoot;
    private CanvasGroup incomingBackgroundCanvasGroup;
    private Image incomingBackgroundImage;
    private Image incomingBackgroundOverlayImage;
    private Image incomingBackgroundForegroundImage;
    private BackgroundBinding incomingBackgroundBinding;
    private Sprite incomingBackgroundSprite;
    private bool incomingBackgroundHasSprite;
    private bool incomingBackgroundIsBlack;
    private Coroutine bgmFadeTransition;
    private string currentBgmId = string.Empty;
    private BgmBinding currentBgmBinding;
    private float previousBgmPlaybackTime;
    private bool hasPreviousBgmPlaybackTime;
    private bool storyUiVisible = true;
    private bool advanceInputEnabled;
    private bool centerScreenModeActive;
    private Vector2 centerScreenDefaultAnchoredPosition;
    private int centerScreenDefaultFontSize;
    private bool centerScreenDefaultsCached;
    private CanvasGroup dialoguePanelCanvasGroup;
    private AudioSource storySfxAudioSource;
    private AudioSource storySfxOverlayAudioSource;
    private AudioSource storyAmbientAudioSource;
    private AudioSource storyTypingAudioSource;
    private Coroutine ambientFadeTransition;
    private string currentStorySfxId = string.Empty;
    private string currentStorySfxOverlayId = string.Empty;
    private float visualFramingScale = 1f;
    private float visualFramingOffsetX;
    private float visualFramingOffsetY;

    private void Awake()
    {
        CacheCenterScreenDefaults();

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
        EnsureDialoguePanelCanvasGroup();
        EnsureStorySfxAudioSource();
        EnsureStorySfxOverlayAudioSource();
        EnsureStoryAmbientAudioSource();
        EnsureStoryTypingAudioSource();
        BindSceneButtons();
        SetAdvanceInputEnabled(false);
        storyFacade.StoryStarted += HandleStoryStarted;
        storyFacade.StoryEnded += HandleStoryEnded;
        storyFacade.StoryError += HandleStoryError;

        // 面板本身不主动决定播放哪条剧情，宿主只需调用 OpenStoryPanel(storyId)。
        SetStatus("等待宿主打开剧情面板", false);
    }

    private void OnDestroy()
    {
        if (backgroundTransition != null)
        {
            StopCoroutine(backgroundTransition);
            backgroundTransition = null;
        }

        StopForegroundTransition();
        StopDialogueFadeTransition();
        StopVisualFadeTransition();
        StopBgmFadeTransition();
        StopAmbient();
        StopTypingAudio();
        StopStorySfxOverlay();

        if (storySfxAudioSource != null)
        {
            storySfxAudioSource.Stop();
        }

        if (storyFacade == null)
        {
            return;
        }

        storyFacade.StoryStarted -= HandleStoryStarted;
        storyFacade.StoryEnded -= HandleStoryEnded;
        storyFacade.StoryError -= HandleStoryError;
    }

    private void Update()
    {
        UpdateLoopBgmFade();
    }

    public override void SetStoryVisible(bool visible)
    {
        if (!visible)
        {
            StopAmbient();
            StopTypingAudio();
            StopStorySfxOverlay();
        }

        if (visible)
        {
            SetActive(endPanel, false);
            SetActive(overlayRoot, false);
        }

        SetActive(storyRoot, visible);
        ApplyAdvanceInputVisibility();
    }

    public override void SetStoryUiVisible(bool visible)
    {
        storyUiVisible = visible;
        SetActive(storyUiRoot, visible);
        ApplyDialoguePresentationVisibility();
        ApplyAdvanceInputVisibility();
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

    public override void SetAdvanceInputEnabled(bool enabled)
    {
        advanceInputEnabled = enabled;
        ApplyAdvanceInputVisibility();
    }

    public override void ShowDialogue(
        string speakerId,
        string speakerName,
        StoryDialoguePresentationMode presentationMode,
        StoryCenterScreenStyleData centerScreenStyle,
        string fullText,
        string visibleRichText,
        int visibleCharacterCount,
        bool isComplete
    )
    {
        string safeVisibleText = visibleRichText ?? string.Empty;
        bool hasSpeakerName = !string.IsNullOrWhiteSpace(speakerName);
        centerScreenModeActive =
            presentationMode == StoryDialoguePresentationMode.CenterScreen;

        if (centerScreenModeActive)
        {
            ApplyCenterScreenStyle(centerScreenStyle);
        }
        else
        {
            ResetCenterScreenStyle();
        }

        RestoreDialoguePanelPresentation();
        ApplyDialoguePresentationVisibility();

        if (speakerText != null)
        {
            speakerText.text = !centerScreenModeActive && hasSpeakerName
                ? speakerName
                : string.Empty;
        }

        if (dialogueText != null)
        {
            dialogueText.alignment = TextAnchor.UpperLeft;

            dialogueText.text = centerScreenModeActive ? string.Empty : safeVisibleText;
        }

        if (centerScreenText != null)
        {
            centerScreenText.supportRichText = true;
            centerScreenText.text = centerScreenModeActive ? safeVisibleText : string.Empty;
        }
    }

    public override void ClearDialoguePresentation()
    {
        StopDialogueFadeTransition();

        if (speakerText != null)
        {
            speakerText.text = string.Empty;
        }

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        if (centerScreenText != null)
        {
            centerScreenText.text = string.Empty;
        }

        centerScreenModeActive = false;
        SetActive(dialoguePanel, false);
        SetActive(centerScreenPresentationRoot, false);
        SetContinueIndicator(false);
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

    public override void SetBackground(
        string backgroundId,
        float fadeSeconds,
        StoryBackgroundTransitionMode transitionMode,
        float fadeOutSeconds
    )
    {
        StopBackgroundTransition();

        BackgroundBinding binding;
        bool hasSprite = TryGetBackgroundBinding(backgroundId, out binding);
        Sprite sprite = hasSprite ? binding.sprite : null;
        bool isBlack = string.Equals(
            backgroundId,
            "black",
            StringComparison.OrdinalIgnoreCase
        );

        UpdateBackgroundLabel(backgroundId, hasSprite, isBlack);

        if (backgroundImage == null)
        {
            return;
        }

        float safeFadeSeconds = Mathf.Max(0f, fadeSeconds);
        float safeFadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);

        if (transitionMode == StoryBackgroundTransitionMode.Cut ||
            safeFadeSeconds <= 0f ||
            !isActiveAndEnabled)
        {
            ApplyBackground(binding, sprite, hasSprite, isBlack, 1f);
            return;
        }

        if (transitionMode == StoryBackgroundTransitionMode.CrossFade)
        {
            SetCurrentBackgroundAlpha(1f);
            PrepareIncomingBackground(binding, sprite, hasSprite, isBlack);

            if (incomingBackgroundRoot == null ||
                incomingBackgroundCanvasGroup == null ||
                incomingBackgroundImage == null)
            {
                ApplyBackground(binding, sprite, hasSprite, isBlack, 1f);
                return;
            }

            backgroundTransition = StartCoroutine(
                CrossFadeBackground(safeFadeOutSeconds, safeFadeSeconds)
            );
            return;
        }

        if (transitionMode == StoryBackgroundTransitionMode.FadeIn)
        {
            PrepareIncomingBackground(binding, sprite, hasSprite, isBlack);
            backgroundTransition = StartCoroutine(
                FadeInBackground(safeFadeSeconds)
            );
            return;
        }

        backgroundTransition = StartCoroutine(
            FadeBackground(binding, sprite, hasSprite, isBlack, safeFadeSeconds)
        );
    }

    public override void ShowForeground(
        string foregroundId,
        float fadeSeconds,
        float offsetX,
        float offsetY,
        float scale,
        bool flipX
    )
    {
        StopForegroundTransition();

        ForegroundBinding binding;
        if (!TryGetForegroundBinding(foregroundId, out binding))
        {
            SetStatus(
                "找不到 Foreground 绑定：" + (foregroundId ?? "未指定"),
                true
            );
            return;
        }

        if (storyForegroundPresentationRoot == null ||
            storyForegroundImage == null)
        {
            SetStatus("未绑定 Story Foreground 表现层", true);
            return;
        }

        RectTransform foregroundRect = storyForegroundImage.rectTransform;
        float safeScale = scale > 0f ? scale : 1f;
        float safeFadeSeconds = Mathf.Max(0f, fadeSeconds);

        storyForegroundImage.sprite = binding.sprite;
        storyForegroundImage.type = Image.Type.Simple;
        storyForegroundImage.preserveAspect = true;
        storyForegroundImage.raycastTarget = false;
        storyForegroundImage.SetNativeSize();

        foregroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        foregroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        foregroundRect.pivot = new Vector2(0.5f, 0.5f);
        foregroundRect.anchoredPosition = new Vector2(offsetX, offsetY);
        foregroundRect.localScale = new Vector3(
            flipX ? -safeScale : safeScale,
            safeScale,
            1f
        );

        SetActive(storyForegroundPresentationRoot, true);
        SetActive(storyForegroundImage.gameObject, true);

        Color color = storyForegroundImage.color;
        color.a = safeFadeSeconds > 0f ? 0f : 1f;
        storyForegroundImage.color = color;

        if (safeFadeSeconds > 0f && isActiveAndEnabled)
        {
            foregroundFadeTransition = StartCoroutine(
                FadeInForeground(safeFadeSeconds)
            );
        }

        ApplyVisualFraming();
    }

    public override bool PlaySfx(
        string sfxId,
        float volume,
        bool waitUntilComplete
    )
    {
        return PlaySfx(
            sfxId,
            volume,
            waitUntilComplete,
            StorySfxChannel.Primary
        );
    }

    public override bool PlaySfx(
        string sfxId,
        float volume,
        bool waitUntilComplete,
        StorySfxChannel channel
    )
    {
        StorySfxBinding binding;

        if (!TryGetSfxBinding(sfxId, out binding))
        {
            SetStatus(
                "找不到 Story SFX 绑定：" + (sfxId ?? "未指定"),
                true
            );
            return false;
        }

        if (binding.clip == null)
        {
            SetStatus("Story SFX 绑定没有 AudioClip：" + binding.sfxId, true);
            return false;
        }

        if (channel == StorySfxChannel.Overlay)
        {
            EnsureStorySfxOverlayAudioSource();
        }
        else
        {
            EnsureStorySfxAudioSource();
        }

        AudioSource audioSource = channel == StorySfxChannel.Overlay
            ? storySfxOverlayAudioSource
            : storySfxAudioSource;

        if (audioSource == null)
        {
            SetStatus("未创建 Story SFX AudioSource：" + binding.sfxId, true);
            return false;
        }

        audioSource.Stop();
        audioSource.clip = binding.clip;
        audioSource.volume = Mathf.Max(0f, volume);
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (channel == StorySfxChannel.Overlay)
        {
            currentStorySfxOverlayId = binding.sfxId ?? string.Empty;
        }
        else
        {
            currentStorySfxId = binding.sfxId ?? string.Empty;
        }

        audioSource.Play();
        return true;
    }

    public override bool PlayAmbient(
        string audioId,
        float volume,
        bool loop,
        float fadeInSeconds
    )
    {
        StorySfxBinding binding;

        if (!TryGetStoryAudioBinding(audioId, out binding))
        {
            SetStatus(
                "找不到 Story Ambient 绑定：" + (audioId ?? "未指定"),
                true
            );
            return false;
        }

        if (binding.clip == null)
        {
            SetStatus("Story Ambient 绑定没有 AudioClip：" + binding.sfxId, true);
            return false;
        }

        EnsureStoryAmbientAudioSource();

        if (storyAmbientAudioSource == null)
        {
            SetStatus("未创建 Story Ambient AudioSource：" + binding.sfxId, true);
            return false;
        }

        StopAmbient();
        storyAmbientAudioSource.clip = binding.clip;
        storyAmbientAudioSource.loop = loop;
        storyAmbientAudioSource.playOnAwake = false;
        storyAmbientAudioSource.spatialBlend = 0f;

        float targetVolume = Mathf.Max(0f, volume);
        float safeFadeInSeconds = Mathf.Max(0f, fadeInSeconds);
        storyAmbientAudioSource.volume = safeFadeInSeconds > 0f
            ? 0f
            : targetVolume;
        storyAmbientAudioSource.Play();

        if (safeFadeInSeconds > 0f && isActiveAndEnabled)
        {
            ambientFadeTransition = StartCoroutine(
                FadeAmbientIn(targetVolume, safeFadeInSeconds)
            );
        }

        return true;
    }

    public override void StopAmbient()
    {
        if (ambientFadeTransition != null)
        {
            StopCoroutine(ambientFadeTransition);
            ambientFadeTransition = null;
        }

        if (storyAmbientAudioSource == null)
        {
            return;
        }

        storyAmbientAudioSource.Stop();
        storyAmbientAudioSource.clip = null;
        storyAmbientAudioSource.volume = 0f;
        storyAmbientAudioSource.loop = true;
    }

    public override bool StartTypingAudio(string audioId, float volume)
    {
        StorySfxBinding binding;

        if (!TryGetStoryAudioBinding(audioId, out binding))
        {
            SetStatus(
                "找不到 Story Typing Audio 绑定：" + (audioId ?? "未指定"),
                true
            );
            return false;
        }

        if (binding.clip == null)
        {
            SetStatus("Story Typing Audio 绑定没有 AudioClip：" + binding.sfxId, true);
            return false;
        }

        EnsureStoryTypingAudioSource();

        if (storyTypingAudioSource == null)
        {
            SetStatus("未创建 Story Typing AudioSource：" + binding.sfxId, true);
            return false;
        }

        StopTypingAudio();
        storyTypingAudioSource.clip = binding.clip;
        storyTypingAudioSource.volume = Mathf.Max(0f, volume);
        storyTypingAudioSource.loop = true;
        storyTypingAudioSource.playOnAwake = false;
        storyTypingAudioSource.spatialBlend = 0f;
        storyTypingAudioSource.Play();
        return true;
    }

    public override void StopTypingAudio()
    {
        if (storyTypingAudioSource == null)
        {
            return;
        }

        storyTypingAudioSource.Stop();
        storyTypingAudioSource.clip = null;
        storyTypingAudioSource.volume = 0f;
        storyTypingAudioSource.loop = true;
    }

    public override bool IsStorySfxPlaying(string sfxId)
    {
        return IsStorySfxPlaying(sfxId, StorySfxChannel.Primary);
    }

    public override bool IsStorySfxPlaying(
        string sfxId,
        StorySfxChannel channel
    )
    {
        AudioSource audioSource = channel == StorySfxChannel.Overlay
            ? storySfxOverlayAudioSource
            : storySfxAudioSource;
        string currentSfxId = channel == StorySfxChannel.Overlay
            ? currentStorySfxOverlayId
            : currentStorySfxId;

        return audioSource != null &&
            audioSource.isPlaying &&
            string.Equals(
                currentSfxId,
                sfxId,
                StringComparison.OrdinalIgnoreCase
            );
    }

    public override void FadeDialogue(float targetAlpha, float fadeSeconds)
    {
        EnsureDialoguePanelCanvasGroup();

        if (dialoguePanelCanvasGroup == null)
        {
            return;
        }

        StopDialogueFadeTransition();

        float safeTargetAlpha = Mathf.Clamp01(targetAlpha);
        float safeFadeSeconds = Mathf.Max(0f, fadeSeconds);

        if (safeFadeSeconds <= 0f || !isActiveAndEnabled)
        {
            SetDialoguePanelAlpha(safeTargetAlpha);
            return;
        }

        dialogueFadeTransition = StartCoroutine(
            FadeDialoguePresentation(safeTargetAlpha, safeFadeSeconds)
        );
    }

    public override void SetVisualFraming(
        float scale,
        float offsetX,
        float offsetY
    )
    {
        visualFramingScale = scale > 0f ? scale : 1f;
        visualFramingOffsetX = offsetX;
        visualFramingOffsetY = offsetY;
        ApplyVisualFraming();
    }

    public override void FadeVisualToBlack(float fadeSeconds)
    {
        StopVisualFadeTransition();

        float safeFadeSeconds = Mathf.Max(0f, fadeSeconds);

        if (safeFadeSeconds <= 0f || !isActiveAndEnabled)
        {
            SetVisualImageAlpha(0f);
            SetActive(storyForegroundPresentationRoot, false);
            SetActive(storyForegroundImage != null
                ? storyForegroundImage.gameObject
                : null, false);
            CommitBlackBackground();
            SetVisualFraming(1f, 0f, 0f);
            return;
        }

        visualFadeTransition = StartCoroutine(
            FadeVisualToBlackPresentation(safeFadeSeconds)
        );
    }

    public override void ChangeBgm(
        string bgmId,
        float fadeOutSeconds,
        bool loop
    )
    {
        BgmBinding targetBinding;

        if (!TryGetBgmBinding(bgmId, out targetBinding))
        {
            ReportBgmIssue("找不到 BGM 绑定：" + (bgmId ?? "未指定"));
            return;
        }

        if (targetBinding.clip == null)
        {
            ReportBgmIssue("BGM 绑定没有 AudioClip：" + targetBinding.bgmId);
            return;
        }

        if (bgmAudioSource == null)
        {
            ReportBgmIssue("未绑定 BGM AudioSource：" + targetBinding.bgmId);
            return;
        }

        float targetVolume = Mathf.Clamp01(targetBinding.volume);
        bool isSameBgm = bgmAudioSource.isPlaying &&
            string.Equals(
                currentBgmId,
                targetBinding.bgmId,
                StringComparison.OrdinalIgnoreCase
            ) &&
            bgmAudioSource.clip == targetBinding.clip;

        if (isSameBgm)
        {
            StopBgmFadeTransition();
            currentBgmBinding = targetBinding;
            bgmAudioSource.loop = loop;
            bgmAudioSource.volume = targetVolume;
            ResetLoopBgmTracking();
            return;
        }

        StopBgmFadeTransition();

        float safeFadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);

        if (bgmAudioSource.isPlaying && safeFadeOutSeconds > 0f)
        {
            bgmFadeTransition = StartCoroutine(
                FadeOutAndPlayBgm(
                    targetBinding,
                    targetVolume,
                    loop,
                    safeFadeOutSeconds
                )
            );
            return;
        }

        bgmAudioSource.Stop();
        PlayBgm(targetBinding, targetVolume, loop);
    }

    private void UpdateBackgroundLabel(
        string backgroundId,
        bool hasSprite,
        bool isBlack
    )
    {
        if (backgroundLabel == null)
        {
            return;
        }

        backgroundLabel.gameObject.SetActive(!hasSprite && !isBlack);

        if (!hasSprite && !isBlack)
        {
            backgroundLabel.text = "背景占位 · " + (backgroundId ?? "未指定");
        }
    }

    private bool TryGetBgmBinding(
        string bgmId,
        out BgmBinding result
    )
    {
        result = null;

        if (string.IsNullOrWhiteSpace(bgmId))
        {
            return false;
        }

        if (bgmBindings == null)
        {
            return false;
        }

        foreach (BgmBinding binding in bgmBindings)
        {
            if (binding != null &&
                string.Equals(
                    binding.bgmId,
                    bgmId,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                result = binding;
                return true;
            }
        }

        return false;
    }

    private bool TryGetStoryAudioBinding(
        string audioId,
        out StorySfxBinding result
    )
    {
        result = null;

        if (string.IsNullOrWhiteSpace(audioId) || sfxBindings == null)
        {
            return false;
        }

        foreach (StorySfxBinding binding in sfxBindings)
        {
            if (binding != null &&
                string.Equals(
                    binding.sfxId,
                    audioId,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                result = binding;
                return true;
            }
        }

        return false;
    }

    private bool TryGetSfxBinding(
        string sfxId,
        out StorySfxBinding result
    )
    {
        return TryGetStoryAudioBinding(sfxId, out result);
    }

    private void StopStorySfxOverlay()
    {
        if (storySfxOverlayAudioSource == null)
        {
            return;
        }

        storySfxOverlayAudioSource.Stop();
        storySfxOverlayAudioSource.clip = null;
        storySfxOverlayAudioSource.volume = 0f;
        storySfxOverlayAudioSource.loop = false;
        currentStorySfxOverlayId = string.Empty;
    }

    private void PlayBgm(
        BgmBinding binding,
        float volume,
        bool loop
    )
    {
        bgmAudioSource.clip = binding.clip;
        bgmAudioSource.volume = Mathf.Clamp01(volume);
        bgmAudioSource.loop = loop;
        currentBgmId = binding.bgmId ?? string.Empty;
        currentBgmBinding = binding;
        bgmAudioSource.Play();
        ResetLoopBgmTracking();
    }

    private IEnumerator FadeOutAndPlayBgm(
        BgmBinding targetBinding,
        float targetVolume,
        bool loop,
        float fadeOutSeconds
    )
    {
        float startVolume = Mathf.Clamp01(bgmAudioSource.volume);
        float elapsed = 0f;

        while (elapsed < fadeOutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            bgmAudioSource.volume = Mathf.Lerp(
                startVolume,
                0f,
                Mathf.Clamp01(elapsed / fadeOutSeconds)
            );
            yield return null;
        }

        bgmAudioSource.Stop();
        PlayBgm(targetBinding, targetVolume, loop);
        bgmFadeTransition = null;
    }

    private void StopBgmFadeTransition()
    {
        if (bgmFadeTransition != null)
        {
            StopCoroutine(bgmFadeTransition);
            bgmFadeTransition = null;
        }
    }

    private IEnumerator FadeAmbientIn(float targetVolume, float fadeInSeconds)
    {
        float duration = Mathf.Max(0.01f, fadeInSeconds);
        float startVolume = storyAmbientAudioSource != null
            ? storyAmbientAudioSource.volume
            : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (storyAmbientAudioSource != null)
            {
                storyAmbientAudioSource.volume = Mathf.Lerp(
                    startVolume,
                    targetVolume,
                    Mathf.Clamp01(elapsed / duration)
                );
            }

            yield return null;
        }

        if (storyAmbientAudioSource != null)
        {
            storyAmbientAudioSource.volume = targetVolume;
        }

        ambientFadeTransition = null;
    }

    private void ResetLoopBgmTracking()
    {
        previousBgmPlaybackTime = 0f;
        hasPreviousBgmPlaybackTime = false;
    }

    private void UpdateLoopBgmFade()
    {
        if (bgmAudioSource == null ||
            !bgmAudioSource.isPlaying ||
            bgmAudioSource.clip == null ||
            !bgmAudioSource.loop ||
            currentBgmBinding == null ||
            currentBgmBinding.clip != bgmAudioSource.clip ||
            bgmFadeTransition != null)
        {
            return;
        }

        float currentTime = bgmAudioSource.time;
        float clipLength = bgmAudioSource.clip.length;

        if (hasPreviousBgmPlaybackTime &&
            currentTime < previousBgmPlaybackTime)
        {
            bgmAudioSource.volume = Mathf.Clamp01(currentBgmBinding.volume);
        }

        previousBgmPlaybackTime = currentTime;
        hasPreviousBgmPlaybackTime = true;

        float safeFadeDuration = Mathf.Min(
            Mathf.Max(0f, currentBgmBinding.loopFadeOutSeconds),
            clipLength
        );

        if (safeFadeDuration <= 0f || clipLength <= 0f)
        {
            bgmAudioSource.volume = Mathf.Clamp01(currentBgmBinding.volume);
            return;
        }

        float remaining = clipLength - currentTime;

        if (remaining <= safeFadeDuration)
        {
            float normalized = Mathf.Clamp01(remaining / safeFadeDuration);
            bgmAudioSource.volume = Mathf.Clamp01(currentBgmBinding.volume) * normalized;
            return;
        }

        bgmAudioSource.volume = Mathf.Clamp01(currentBgmBinding.volume);
    }

    private void ReportBgmIssue(string message)
    {
        SetStatus("BGM：" + message, true);
        Debug.LogWarning("[Story] " + message, this);
    }

    private void StopBackgroundTransition()
    {
        if (backgroundTransition != null)
        {
            StopCoroutine(backgroundTransition);
            backgroundTransition = null;
        }

        if (incomingBackgroundRoot != null &&
            incomingBackgroundRoot.activeSelf)
        {
            CommitIncomingBackground();
        }

        SetCurrentBackgroundAlpha(1f);
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

    private IEnumerator FadeInBackground(float fadeSeconds)
    {
        float duration = Mathf.Max(0.01f, fadeSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (incomingBackgroundCanvasGroup != null)
            {
                incomingBackgroundCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            }

            yield return null;
        }

        if (incomingBackgroundCanvasGroup != null)
        {
            incomingBackgroundCanvasGroup.alpha = 1f;
        }

        CommitIncomingBackground();
        backgroundTransition = null;
    }

    private IEnumerator CrossFadeBackground(
        float fadeOutSeconds,
        float fadeInSeconds
    )
    {
        float outgoingDuration = Mathf.Max(0.01f, fadeOutSeconds);
        float incomingDuration = Mathf.Max(0.01f, fadeInSeconds);
        float duration = Mathf.Max(outgoingDuration, incomingDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float outgoingAlpha = elapsed >= outgoingDuration
                ? 0f
                : 1f - Mathf.Clamp01(elapsed / outgoingDuration);
            float incomingAlpha = elapsed >= incomingDuration
                ? 1f
                : Mathf.Clamp01(elapsed / incomingDuration);

            SetCurrentBackgroundAlpha(outgoingAlpha);

            if (incomingBackgroundCanvasGroup != null)
            {
                incomingBackgroundCanvasGroup.alpha = incomingAlpha;
            }

            yield return null;
        }

        SetCurrentBackgroundAlpha(0f);

        if (incomingBackgroundCanvasGroup != null)
        {
            incomingBackgroundCanvasGroup.alpha = 1f;
        }

        CommitIncomingBackground();
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

    private bool TryGetForegroundBinding(
        string foregroundId,
        out ForegroundBinding result
    )
    {
        result = null;

        if (string.IsNullOrWhiteSpace(foregroundId))
        {
            return false;
        }

        foreach (ForegroundBinding binding in foregroundBindings)
        {
            if (binding != null &&
                binding.sprite != null &&
                string.Equals(
                    binding.foregroundId,
                    foregroundId,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                result = binding;
                return true;
            }
        }

        return false;
    }

    private void EnsureStorySfxAudioSource()
    {
        if (storySfxAudioSource != null)
        {
            return;
        }

        GameObject sfxObject = new GameObject("StorySfxAudio");
        sfxObject.transform.SetParent(transform, false);
        storySfxAudioSource = sfxObject.AddComponent<AudioSource>();
        storySfxAudioSource.playOnAwake = false;
        storySfxAudioSource.loop = false;
        storySfxAudioSource.volume = 1f;
        storySfxAudioSource.spatialBlend = 0f;
    }

    private void EnsureStorySfxOverlayAudioSource()
    {
        if (storySfxOverlayAudioSource != null)
        {
            return;
        }

        GameObject overlayObject = new GameObject("StorySfxOverlayAudio");
        overlayObject.transform.SetParent(transform, false);
        storySfxOverlayAudioSource = overlayObject.AddComponent<AudioSource>();
        storySfxOverlayAudioSource.playOnAwake = false;
        storySfxOverlayAudioSource.loop = false;
        storySfxOverlayAudioSource.volume = 1f;
        storySfxOverlayAudioSource.spatialBlend = 0f;
    }

    private void EnsureStoryAmbientAudioSource()
    {
        if (storyAmbientAudioSource != null)
        {
            return;
        }

        GameObject ambientObject = new GameObject("StoryAmbientAudio");
        ambientObject.transform.SetParent(transform, false);
        storyAmbientAudioSource = ambientObject.AddComponent<AudioSource>();
        storyAmbientAudioSource.playOnAwake = false;
        storyAmbientAudioSource.loop = true;
        storyAmbientAudioSource.volume = 0f;
        storyAmbientAudioSource.spatialBlend = 0f;
    }

    private void EnsureStoryTypingAudioSource()
    {
        if (storyTypingAudioSource != null)
        {
            return;
        }

        GameObject typingObject = new GameObject("StoryTypingAudio");
        typingObject.transform.SetParent(transform, false);
        storyTypingAudioSource = typingObject.AddComponent<AudioSource>();
        storyTypingAudioSource.playOnAwake = false;
        storyTypingAudioSource.loop = true;
        storyTypingAudioSource.volume = 0f;
        storyTypingAudioSource.spatialBlend = 0f;
    }

    private void EnsureDialoguePanelCanvasGroup()
    {
        if (dialoguePanel == null)
        {
            return;
        }

        if (dialoguePanelCanvasGroup == null)
        {
            dialoguePanelCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();

            if (dialoguePanelCanvasGroup == null)
            {
                dialoguePanelCanvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
            }
        }

        dialoguePanelCanvasGroup.alpha = Mathf.Clamp01(
            dialoguePanelCanvasGroup.alpha
        );
    }

    private void RestoreDialoguePanelPresentation()
    {
        EnsureDialoguePanelCanvasGroup();
        StopDialogueFadeTransition();

        if (dialoguePanelCanvasGroup == null)
        {
            return;
        }

        dialoguePanelCanvasGroup.alpha = 1f;
        dialoguePanelCanvasGroup.interactable = true;
        dialoguePanelCanvasGroup.blocksRaycasts = true;
    }

    private void StopDialogueFadeTransition()
    {
        if (dialogueFadeTransition != null)
        {
            StopCoroutine(dialogueFadeTransition);
            dialogueFadeTransition = null;
        }
    }

    private void SetDialoguePanelAlpha(float alpha)
    {
        if (dialoguePanelCanvasGroup == null)
        {
            return;
        }

        float safeAlpha = Mathf.Clamp01(alpha);
        dialoguePanelCanvasGroup.alpha = safeAlpha;
        dialoguePanelCanvasGroup.interactable = safeAlpha > 0.999f;
        dialoguePanelCanvasGroup.blocksRaycasts = safeAlpha > 0.001f;
    }

    private IEnumerator FadeDialoguePresentation(
        float targetAlpha,
        float fadeSeconds
    )
    {
        float duration = Mathf.Max(0.01f, fadeSeconds);
        float elapsed = 0f;
        float startAlpha = dialoguePanelCanvasGroup != null
            ? dialoguePanelCanvasGroup.alpha
            : 1f;

        if (dialoguePanelCanvasGroup != null)
        {
            dialoguePanelCanvasGroup.interactable = false;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            SetDialoguePanelAlpha(Mathf.Lerp(startAlpha, targetAlpha, normalized));
            yield return null;
        }

        SetDialoguePanelAlpha(targetAlpha);
        dialogueFadeTransition = null;
    }

    private void ApplyVisualFraming()
    {
        EnsureBackgroundLayerImages();
        ApplyVisualFramingToTransform(
            backgroundImage != null ? backgroundImage.rectTransform : null
        );
        ApplyVisualFramingToTransform(
            backgroundOverlayImage != null
                ? backgroundOverlayImage.rectTransform
                : null
        );
        ApplyVisualFramingToTransform(
            backgroundForegroundImage != null
                ? backgroundForegroundImage.rectTransform
                : null
        );
        ApplyVisualFramingToTransform(
            incomingBackgroundRoot != null
                ? incomingBackgroundRoot.GetComponent<RectTransform>()
                : null
        );
        ApplyVisualFramingToTransform(
            storyForegroundPresentationRoot != null
                ? storyForegroundPresentationRoot.GetComponent<RectTransform>()
                : null
        );
    }

    private void ApplyVisualFramingToTransform(RectTransform target)
    {
        if (target == null)
        {
            return;
        }

        target.localScale = new Vector3(
            visualFramingScale,
            visualFramingScale,
            1f
        );
        Vector3 localPosition = target.localPosition;
        localPosition.x = visualFramingOffsetX;
        localPosition.y = visualFramingOffsetY;
        target.localPosition = localPosition;
    }

    private void StopVisualFadeTransition()
    {
        if (visualFadeTransition != null)
        {
            StopCoroutine(visualFadeTransition);
            visualFadeTransition = null;
        }
    }

    private IEnumerator FadeVisualToBlackPresentation(float fadeSeconds)
    {
        float duration = Mathf.Max(0.01f, fadeSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetVisualImageAlpha(1f - Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        SetVisualImageAlpha(0f);
        SetActive(storyForegroundPresentationRoot, false);
        SetActive(
            storyForegroundImage != null ? storyForegroundImage.gameObject : null,
            false
        );
        CommitBlackBackground();
        SetVisualFraming(1f, 0f, 0f);
        visualFadeTransition = null;
    }

    private void CommitBlackBackground()
    {
        if (backgroundImage == null)
        {
            return;
        }

        // 把旧 CG 从 current background 中移除，而不是只留下 alpha=0 的旧 sprite。
        // 这样后续 FadeIn 的 source state 会是真正的 black，不会复活旧图。
        ApplyBackground(null, null, false, true, 1f);
    }

    private void SetVisualImageAlpha(float alpha)
    {
        SetImageAlpha(backgroundImage, alpha);
        SetImageAlpha(backgroundOverlayImage, alpha);
        SetImageAlpha(backgroundForegroundImage, alpha);
        SetImageAlpha(storyForegroundImage, alpha);

        if (incomingBackgroundCanvasGroup != null)
        {
            incomingBackgroundCanvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }

    private void StopForegroundTransition()
    {
        if (foregroundFadeTransition != null)
        {
            StopCoroutine(foregroundFadeTransition);
            foregroundFadeTransition = null;
        }
    }

    private IEnumerator FadeInForeground(float fadeSeconds)
    {
        float duration = Mathf.Max(0.01f, fadeSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (storyForegroundImage != null)
            {
                Color color = storyForegroundImage.color;
                color.a = Mathf.Clamp01(elapsed / duration);
                storyForegroundImage.color = color;
            }

            yield return null;
        }

        if (storyForegroundImage != null)
        {
            Color color = storyForegroundImage.color;
            color.a = 1f;
            storyForegroundImage.color = color;
        }

        foregroundFadeTransition = null;
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

    private void PrepareIncomingBackground(
        BackgroundBinding binding,
        Sprite sprite,
        bool hasSprite,
        bool isBlack
    )
    {
        EnsureIncomingBackgroundVisual();

        if (incomingBackgroundRoot == null ||
            incomingBackgroundCanvasGroup == null ||
            incomingBackgroundImage == null)
        {
            ApplyBackground(binding, sprite, hasSprite, isBlack, 1f);
            return;
        }

        incomingBackgroundBinding = binding;
        incomingBackgroundSprite = sprite;
        incomingBackgroundHasSprite = hasSprite;
        incomingBackgroundIsBlack = isBlack;
        incomingBackgroundRoot.SetActive(true);
        incomingBackgroundCanvasGroup.alpha = 0f;

        incomingBackgroundImage.sprite = hasSprite ? sprite : null;
        incomingBackgroundImage.type = Image.Type.Simple;
        incomingBackgroundImage.preserveAspect = hasSprite;
        incomingBackgroundImage.color = ResolveFallbackBackgroundColor(
            hasSprite,
            isBlack
        );
        incomingBackgroundImage.gameObject.SetActive(true);

        ApplyBackgroundLayer(
            incomingBackgroundOverlayImage,
            binding != null ? binding.overlaySprite : null,
            binding != null ? binding.overlayAnchoredPosition : Vector2.zero,
            binding != null ? binding.overlaySize : Vector2.zero,
            1f
        );
        ApplyBackgroundLayer(
            incomingBackgroundForegroundImage,
            binding != null ? binding.foregroundSprite : null,
            binding != null ? binding.foregroundAnchoredPosition : Vector2.zero,
            binding != null ? binding.foregroundSize : Vector2.zero,
            1f
        );
    }

    private void CommitIncomingBackground()
    {
        if (incomingBackgroundRoot == null ||
            !incomingBackgroundRoot.activeSelf)
        {
            return;
        }

        ApplyBackground(
            incomingBackgroundBinding,
            incomingBackgroundSprite,
            incomingBackgroundHasSprite,
            incomingBackgroundIsBlack,
            1f
        );
        ClearIncomingBackground();
    }

    private void ClearIncomingBackground()
    {
        if (incomingBackgroundCanvasGroup != null)
        {
            incomingBackgroundCanvasGroup.alpha = 0f;
        }

        SetActive(incomingBackgroundRoot, false);
        incomingBackgroundBinding = null;
        incomingBackgroundSprite = null;
        incomingBackgroundHasSprite = false;
        incomingBackgroundIsBlack = false;
    }

    private void EnsureIncomingBackgroundVisual()
    {
        if (incomingBackgroundRoot != null || backgroundImage == null)
        {
            return;
        }

        EnsureBackgroundLayerImages();

        Transform parent = backgroundImage.transform.parent;

        if (parent == null)
        {
            return;
        }

        incomingBackgroundRoot = new GameObject(
            "StoryBackgroundIncomingRoot",
            typeof(RectTransform),
            typeof(CanvasGroup)
        );
        incomingBackgroundRoot.layer = backgroundImage.gameObject.layer;
        incomingBackgroundRoot.transform.SetParent(parent, false);

        RectTransform rootRect = incomingBackgroundRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = Vector2.zero;
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        int siblingIndex = backgroundImage.transform.GetSiblingIndex() + 1;

        if (backgroundForegroundImage != null)
        {
            siblingIndex = backgroundForegroundImage.transform.GetSiblingIndex() + 1;
        }
        else if (backgroundOverlayImage != null)
        {
            siblingIndex = backgroundOverlayImage.transform.GetSiblingIndex() + 1;
        }

        incomingBackgroundRoot.transform.SetSiblingIndex(
            Mathf.Min(siblingIndex, parent.childCount - 1)
        );

        incomingBackgroundCanvasGroup =
            incomingBackgroundRoot.GetComponent<CanvasGroup>();
        incomingBackgroundCanvasGroup.alpha = 0f;
        incomingBackgroundCanvasGroup.interactable = false;
        incomingBackgroundCanvasGroup.blocksRaycasts = false;
        incomingBackgroundCanvasGroup.ignoreParentGroups = true;

        incomingBackgroundImage = CreateIncomingImage(
            incomingBackgroundRoot.transform,
            "IncomingBase"
        );
        incomingBackgroundOverlayImage = CreateIncomingImage(
            incomingBackgroundRoot.transform,
            "IncomingOverlay"
        );
        incomingBackgroundForegroundImage = CreateIncomingImage(
            incomingBackgroundRoot.transform,
            "IncomingForeground"
        );

        ApplyVisualFraming();
        SetActive(incomingBackgroundRoot, false);
    }

    private Image CreateIncomingImage(Transform parent, string objectName)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        imageObject.layer = backgroundImage.gameObject.layer;
        imageObject.transform.SetParent(parent, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = Vector2.zero;
        imageRect.pivot = new Vector2(0.5f, 0.5f);

        Image image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        imageObject.SetActive(false);
        return image;
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
        ApplyVisualFraming();
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

    private void SetCurrentBackgroundAlpha(float alpha)
    {
        if (backgroundImage == null)
        {
            return;
        }

        Color color = backgroundImage.color;
        color.a = Mathf.Clamp01(alpha);
        backgroundImage.color = color;
        SetBackgroundLayerAlpha(color.a);
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
        StopStorySfxOverlay();
        StopAmbient();
        StopTypingAudio();
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
        AddListener(centerScreenAdvanceButton, RequestAdvance);
        AddListener(advanceInputSurfaceButton, RequestAdvance);
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
        StopStorySfxOverlay();
        StopAmbient();
        StopTypingAudio();
        lastStartedStoryId = startedStoryId ?? string.Empty;
        SetStatus("运行中：" + startedStoryId, false);
    }

    private void HandleStoryEnded(string endedStoryId)
    {
        SetStatus("已结束：" + endedStoryId, false);
    }

    private void HandleStoryError(string message)
    {
        StopStorySfxOverlay();
        StopAmbient();
        StopTypingAudio();
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

    private void ApplyDialoguePresentationVisibility()
    {
        bool showCenterScreen = storyUiVisible && centerScreenModeActive;

        SetActive(centerScreenPresentationRoot, showCenterScreen);
        SetActive(dialoguePanel, storyUiVisible && !centerScreenModeActive);
    }

    private void ApplyAdvanceInputVisibility()
    {
        SetActive(
            advanceInputSurfaceButton != null
                ? advanceInputSurfaceButton.gameObject
                : null,
            advanceInputEnabled && storyUiVisible
        );
    }

    private void CacheCenterScreenDefaults()
    {
        if (centerScreenDefaultsCached || centerScreenText == null)
        {
            return;
        }

        centerScreenDefaultAnchoredPosition =
            centerScreenText.rectTransform.anchoredPosition;
        centerScreenDefaultFontSize = centerScreenText.fontSize;
        centerScreenDefaultsCached = true;
    }

    private void ResetCenterScreenStyle()
    {
        CacheCenterScreenDefaults();

        if (!centerScreenDefaultsCached || centerScreenText == null)
        {
            return;
        }

        centerScreenText.rectTransform.anchoredPosition =
            centerScreenDefaultAnchoredPosition;
        centerScreenText.fontSize = centerScreenDefaultFontSize;
    }

    private void ApplyCenterScreenStyle(StoryCenterScreenStyleData style)
    {
        ResetCenterScreenStyle();

        if (style == null || centerScreenText == null)
        {
            return;
        }

        centerScreenText.rectTransform.anchoredPosition =
            centerScreenDefaultAnchoredPosition + new Vector2(
                style.offsetX,
                style.offsetY
            );

        if (style.fontSize > 0)
        {
            centerScreenText.fontSize = style.fontSize;
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
