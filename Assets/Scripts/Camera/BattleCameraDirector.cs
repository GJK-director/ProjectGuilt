using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 战斗镜头的上层演出入口；底层位置与旋转仍由Camera Controller统一计算。
public sealed class BattleCameraDirector : MonoBehaviour
{
    [SerializeField]
    private GrayboxBattleCameraController cameraController;

    [SerializeField]
    private BattleSimpleUIController battleUIController;

    [Header("开发测试")]
    [Tooltip(
        "开发阶段临时入口。Play Mode后等待Battle初始化完成并自动播放Intro。" +
        "正式剧情/场景流程接入后关闭，由PlayBattleIntro()外部触发。"
    )]
    [SerializeField]
    private bool autoPlayIntroOnStart = true;

    [SerializeField, Min(0f)]
    private float initializationTimeout = 10f;

    [Header("Battle Startup Intro")]
    [SerializeField, Range(0f, 1f)]
    private float introStartOverlayAlpha = 1f;

    [SerializeField]
    private float introStartOrbitRadius = 8.5f;

    [SerializeField, Min(0f)]
    private float introCameraDelay = 0.10f;

    [SerializeField, Min(0.01f)]
    private float introCameraDuration = 0.85f;

    [SerializeField, Min(0f)]
    private float introFadeDelay = 0.05f;

    [SerializeField, Min(0.01f)]
    private float introFadeDuration = 0.95f;

    [Header("Turn End Recovery")]
    [SerializeField, Range(0f, 1f)]
    private float recoveryStartOverlayAlpha = 0.65f;

    [SerializeField]
    private float recoveryStartOrbitRadius = 8.5f;

    [SerializeField, Min(0f)]
    private float recoveryCameraDelay = 0.10f;

    [SerializeField, Min(0.01f)]
    private float recoveryCameraDuration = 1.00f;

    [SerializeField, Min(0f)]
    private float recoveryFadeDelay = 0.05f;

    [SerializeField, Min(0.01f)]
    private float recoveryFadeDuration = 1.00f;

    [SerializeField, Min(0f)]
    private float recoveryInteractionUnlockDelay = 0.5f;

    [Header("Generic Battle Focus")]
    [SerializeField, Min(0.01f)]
    private float twoUnitFocusDuration = 0.35f;

    [SerializeField]
    private float twoUnitFocusOrbitRadius = 9.5f;

    [SerializeField, Range(-1f, 1f)]
    private float twoUnitFocusVerticalViewProgress = 0.35f;

    private GameObject overlayRoot;
    private CanvasGroup overlayGroup;
    private Coroutine activePresentationCoroutine;
    private bool isIntroPlaying;
    private bool isTurnEndRecoveryPlaying;
    private bool isTwoUnitFocusPlaying;
    private bool presentationInteractionUnlocked;

    public bool IsIntroPlaying => isIntroPlaying;

    private bool IsPresentationPlaying =>
        isIntroPlaying ||
        isTurnEndRecoveryPlaying ||
        isTwoUnitFocusPlaying;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterDevelopmentRuntimeDirector()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        EnsureDevelopmentRuntimeDirector(scene);
    }

    private static void EnsureDevelopmentRuntimeDirector(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded ||
            FindComponentInScene<BattleCameraDirector>(scene) != null ||
            FindComponentInScene<BattleSimpleUIController>(scene) == null)
        {
            return;
        }

        GameObject directorObject = new GameObject(nameof(BattleCameraDirector));
        SceneManager.MoveGameObjectToScene(directorObject, scene);
        directorObject.AddComponent<BattleCameraDirector>();
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        T[] components = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].gameObject.scene == scene)
            {
                return components[i];
            }
        }

        return null;
    }

    private void Start()
    {
        ResolveReferences();
        if (autoPlayIntroOnStart)
        {
            PlayBattleIntro();
        }
    }

    private void OnDisable()
    {
        if (activePresentationCoroutine != null)
        {
            StopCoroutine(activePresentationCoroutine);
            activePresentationCoroutine = null;
        }

        HideOverlay();
        cameraController?.SetCinematicControl(false);
        isIntroPlaying = false;
        isTurnEndRecoveryPlaying = false;
        isTwoUnitFocusPlaying = false;
    }

    public void PlayBattleIntro()
    {
        if (IsPresentationPlaying)
        {
            return;
        }

        ResolveReferences();
        if (cameraController == null)
        {
            Debug.LogError(
                "BattleCameraDirector找不到GrayboxBattleCameraController，" +
                "Battle Intro已取消。",
                this
            );
            return;
        }

        isIntroPlaying = true;
        cameraController.SetCinematicControl(true);
        ShowOverlay(introStartOverlayAlpha);

        if (battleUIController == null)
        {
            Debug.LogError(
                "BattleCameraDirector找不到BattleSimpleUIController，" +
                "Battle Intro已安全取消。",
                this
            );
            FinishPresentation(false);
            return;
        }

        activePresentationCoroutine =
            StartCoroutine(PlayBattleIntroSequence());
    }

    public void PlayTurnEndRecovery()
    {
        if (IsPresentationPlaying)
        {
            return;
        }

        ResolveReferences();
        if (cameraController == null)
        {
            Debug.LogError(
                "BattleCameraDirector找不到GrayboxBattleCameraController，" +
                "Turn End Recovery已取消。",
                this
            );
            return;
        }

        isTurnEndRecoveryPlaying = true;
        cameraController.SetCinematicControl(true);
        ShowOverlay(recoveryStartOverlayAlpha);
        activePresentationCoroutine =
            StartCoroutine(PlayTurnEndRecoverySequence());
    }

    public bool TryPlayTwoUnitFocus(
        BattleUnitViewHandle first,
        BattleUnitViewHandle second,
        bool releaseCinematicControlOnComplete
    )
    {
        if (IsPresentationPlaying)
        {
            return false;
        }

        if (first == null || second == null ||
            first.WorldRoot == null || second.WorldRoot == null)
        {
            Debug.LogWarning(
                "Two Unit Focus启动失败：单位表现引用无效。",
                this
            );
            return false;
        }

        ResolveReferences();
        if (cameraController == null)
        {
            Debug.LogWarning(
                "Two Unit Focus启动失败：找不到GrayboxBattleCameraController。",
                this
            );
            return false;
        }

        float targetWorldX =
            (first.WorldRoot.transform.position.x
            + second.WorldRoot.transform.position.x)
            * 0.5f;
        float startWorldX =
            cameraController.CurrentHorizontalWorldX;
        float startRadius = cameraController.CurrentOrbitRadius;
        float startVerticalProgress =
            cameraController.CurrentCinematicVerticalViewProgress;

        isTwoUnitFocusPlaying = true;
        cameraController.SetCinematicControl(true);
        activePresentationCoroutine = StartCoroutine(
            PlayTwoUnitFocusSequence(
                startWorldX,
                targetWorldX,
                startRadius,
                twoUnitFocusOrbitRadius,
                startVerticalProgress,
                twoUnitFocusVerticalViewProgress,
                releaseCinematicControlOnComplete
            )
        );
        return true;
    }

    private IEnumerator PlayBattleIntroSequence()
    {
        float initializationElapsed = 0f;
        float safeTimeout = Mathf.Max(0f, initializationTimeout);
        while (!battleUIController.IsInitialized)
        {
            if (initializationElapsed >= safeTimeout)
            {
                Debug.LogError(
                    "Battle Intro等待BattleScene初始化超时，已解除黑幕与输入锁。",
                    this
                );
                FinishPresentation(false);
                yield break;
            }

            initializationElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        cameraController.ResetToDefault();
        cameraController.SetCinematicControl(true);

        float defaultRadius = cameraController.DefaultOrbitRadius;
        cameraController.SetCinematicOrbitRadius(introStartOrbitRadius);
        float startRadius = cameraController.CurrentOrbitRadius;

        float cameraDelay = Mathf.Max(0f, introCameraDelay);
        float cameraDuration = Mathf.Max(0f, introCameraDuration);
        float fadeDelay = Mathf.Max(0f, introFadeDelay);
        float fadeDuration = Mathf.Max(0f, introFadeDuration);
        float totalDuration = Mathf.Max(
            cameraDelay + cameraDuration,
            fadeDelay + fadeDuration
        );
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            float cameraProgress = GetTimelineProgress(
                elapsed,
                cameraDelay,
                cameraDuration
            );
            float fadeProgress = GetTimelineProgress(
                elapsed,
                fadeDelay,
                fadeDuration
            );
            float easedCameraProgress = Mathf.SmoothStep(
                0f,
                1f,
                cameraProgress
            );
            float easedFadeProgress = Mathf.SmoothStep(
                0f,
                1f,
                fadeProgress
            );

            cameraController.SetCinematicOrbitRadius(
                Mathf.Lerp(startRadius, defaultRadius, easedCameraProgress)
            );
            SetOverlayAlpha(Mathf.Lerp(
                introStartOverlayAlpha,
                0f,
                easedFadeProgress
            ));

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        cameraController.SetCinematicOrbitRadius(defaultRadius);
        SetOverlayAlpha(0f);
        FinishPresentation(true);
    }

    private IEnumerator PlayTurnEndRecoverySequence()
    {
        cameraController.ResetToDefault();
        cameraController.SetCinematicControl(true);

        float defaultRadius = cameraController.DefaultOrbitRadius;
        cameraController.SetCinematicOrbitRadius(
            recoveryStartOrbitRadius
        );
        float startRadius = cameraController.CurrentOrbitRadius;

        float cameraDelay = Mathf.Max(0f, recoveryCameraDelay);
        float cameraDuration = Mathf.Max(0f, recoveryCameraDuration);
        float fadeDelay = Mathf.Max(0f, recoveryFadeDelay);
        float fadeDuration = Mathf.Max(0f, recoveryFadeDuration);
        float totalDuration = Mathf.Max(
            cameraDelay + cameraDuration,
            fadeDelay + fadeDuration
        );
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            float cameraProgress = GetTimelineProgress(
                elapsed,
                cameraDelay,
                cameraDuration
            );
            float fadeProgress = GetTimelineProgress(
                elapsed,
                fadeDelay,
                fadeDuration
            );
            float easedCameraProgress =
                1f - Mathf.Pow(1f - cameraProgress, 3f);
            float easedFadeProgress =
                1f - Mathf.Pow(1f - fadeProgress, 3f);

            cameraController.SetCinematicOrbitRadius(
                Mathf.Lerp(startRadius, defaultRadius, easedCameraProgress)
            );
            SetOverlayAlpha(Mathf.Lerp(
                recoveryStartOverlayAlpha,
                0f,
                easedFadeProgress
            ));
            if (elapsed >= recoveryInteractionUnlockDelay)
            {
                UnlockPresentationPointerInteraction();
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        cameraController.SetCinematicOrbitRadius(defaultRadius);
        SetOverlayAlpha(0f);
        FinishPresentation(true);
    }

    private IEnumerator PlayTwoUnitFocusSequence(
        float startWorldX,
        float targetWorldX,
        float startRadius,
        float targetRadius,
        float startVerticalProgress,
        float targetVerticalProgress,
        bool releaseCinematicControlOnComplete
    )
    {
        float duration = Mathf.Max(0f, twoUnitFocusDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = duration > Mathf.Epsilon
                ? Mathf.Clamp01(elapsed / duration)
                : 1f;
            float easedProgress =
                1f - Mathf.Pow(1f - progress, 3f);

            cameraController.SetCinematicHorizontalWorldX(
                Mathf.Lerp(startWorldX, targetWorldX, easedProgress)
            );
            cameraController.SetCinematicOrbitRadius(
                Mathf.Lerp(startRadius, targetRadius, easedProgress)
            );
            cameraController.SetCinematicVerticalViewProgress(
                Mathf.Lerp(
                    startVerticalProgress,
                    targetVerticalProgress,
                    easedProgress
                )
            );

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        cameraController.SetCinematicHorizontalWorldX(targetWorldX);
        cameraController.SetCinematicOrbitRadius(targetRadius);
        cameraController.SetCinematicVerticalViewProgress(
            targetVerticalProgress
        );
        FinishTwoUnitFocus(releaseCinematicControlOnComplete);
    }

    private void ResolveReferences()
    {
        if (cameraController == null)
        {
            cameraController =
                FindFirstObjectByType<GrayboxBattleCameraController>();
        }

        if (battleUIController == null)
        {
            battleUIController = FindFirstObjectByType<BattleSimpleUIController>();
        }
    }

    private void ShowOverlay(float startAlpha)
    {
        EnsureOverlay();
        presentationInteractionUnlocked = false;
        overlayRoot.SetActive(true);
        overlayGroup.alpha = Mathf.Clamp01(startAlpha);
        overlayGroup.interactable = true;
        overlayGroup.blocksRaycasts = true;
    }

    private void HideOverlay()
    {
        if (overlayGroup != null)
        {
            overlayGroup.alpha = 0f;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;
        }

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }
    }

    private void UnlockPresentationPointerInteraction()
    {
        if (presentationInteractionUnlocked)
        {
            return;
        }

        presentationInteractionUnlocked = true;
        if (overlayGroup != null)
        {
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;
        }
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (overlayGroup != null)
        {
            overlayGroup.alpha = Mathf.Clamp01(alpha);
        }
    }

    private void EnsureOverlay()
    {
        if (overlayRoot != null)
        {
            return;
        }

        overlayRoot = new GameObject(
            "BattleIntroOverlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup)
        );
        overlayRoot.transform.SetParent(transform, false);

        Canvas canvas = overlayRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;

        CanvasScaler scaler = overlayRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        overlayGroup = overlayRoot.GetComponent<CanvasGroup>();

        GameObject shieldObject = new GameObject(
            "PointerShield",
            typeof(RectTransform),
            typeof(Image),
            typeof(BattleCinematicInputShield)
        );
        shieldObject.transform.SetParent(overlayRoot.transform, false);

        RectTransform shieldRect = shieldObject.GetComponent<RectTransform>();
        shieldRect.anchorMin = Vector2.zero;
        shieldRect.anchorMax = Vector2.one;
        shieldRect.offsetMin = Vector2.zero;
        shieldRect.offsetMax = Vector2.zero;

        Image shieldImage = shieldObject.GetComponent<Image>();
        shieldImage.color = Color.black;
        shieldImage.raycastTarget = true;
    }

    private void FinishPresentation(bool completedNormally)
    {
        if (completedNormally && cameraController != null)
        {
            cameraController.SetCinematicOrbitRadius(
                cameraController.DefaultOrbitRadius
            );
        }

        HideOverlay();
        cameraController?.SetCinematicControl(false);
        activePresentationCoroutine = null;
        isIntroPlaying = false;
        isTurnEndRecoveryPlaying = false;
        isTwoUnitFocusPlaying = false;
    }

    private void FinishTwoUnitFocus(
        bool releaseCinematicControlOnComplete
    )
    {
        if (releaseCinematicControlOnComplete)
        {
            cameraController?.SetCinematicControl(false);
        }

        activePresentationCoroutine = null;
        isTwoUnitFocusPlaying = false;
    }

    private static float GetTimelineProgress(
        float elapsed,
        float delay,
        float duration
    )
    {
        if (elapsed < delay)
        {
            return 0f;
        }

        if (duration <= Mathf.Epsilon)
        {
            return 1f;
        }

        return Mathf.Clamp01((elapsed - delay) / duration);
    }
}
