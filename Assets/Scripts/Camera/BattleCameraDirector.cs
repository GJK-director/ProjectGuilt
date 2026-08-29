using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 战斗镜头的上层演出入口；底层位置与旋转仍由Camera Controller统一计算。
public sealed class BattleCameraDirector : MonoBehaviour
{
    private enum TwoUnitFocusEasingMode
    {
        // Legacy测试曲线，当前不采用；暂时保留用于回归对比。
        CubicEaseOut,
        // 当前Generic Two Unit Focus正式曲线。
        SmoothStep
    }

    private enum BattleActionEffectType
    {
        None,
        ClashImpact,
        HitImpact,
        GuardImpact
    }

    private enum TurnEndRecoveryPhase
    {
        None,
        Closing,
        WaitingForNewTurn,
        Opening
    }

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
    private float recoveryStartOverlayAlpha = 0.85f;

    [SerializeField]
    private float recoveryStartOrbitRadius = 8.5f;

    // 当前正式Opening固定零Delay；保留旧字段，避免丢失既有Inspector数据。
#pragma warning disable CS0414
    [SerializeField, Min(0f)]
    private float recoveryCameraDelay = 0.10f;

    [SerializeField, Min(0.01f)]
    private float recoveryCameraDuration = 1.00f;

    [SerializeField, Min(0f)]
    private float recoveryFadeDelay = 0.05f;
#pragma warning restore CS0414

    [SerializeField, Min(0.01f)]
    private float recoveryFadeDuration = 1.00f;

    [SerializeField, Min(0f)]
    private float recoveryInteractionUnlockDelay = 0.5f;

    [Header("Turn End Closing")]
    [SerializeField, Range(0f, 1f)]
    private float turnEndClosingOverlayAlpha = 0.95f;

    [SerializeField, Min(0f)]
    private float turnEndClosingCameraDelay = 0f;

    [SerializeField, Min(0.01f)]
    private float turnEndClosingCameraDuration = 0.5f;

    [SerializeField, Min(0f)]
    private float turnEndClosingFadeDelay = 0f;

    [SerializeField, Min(0.01f)]
    private float turnEndClosingFadeDuration = 0.5f;

    [Header("Generic Battle Focus")]
    [SerializeField, Tooltip(
        "SmoothStep为当前正式版本；CubicEaseOut为Legacy回归测试曲线。"
    )]
    private TwoUnitFocusEasingMode twoUnitFocusEasingMode =
        TwoUnitFocusEasingMode.SmoothStep;

    [SerializeField, Min(0.01f)]
    private float twoUnitFocusDuration = 0.35f;

    [SerializeField]
    private float twoUnitFocusOrbitRadius = 9.5f;

    [SerializeField, Range(-1f, 1f)]
    private float twoUnitFocusVerticalViewProgress = 0.35f;

    [Header("Anchored Two Unit Approach")]
    [SerializeField, Range(0f, 0.5f)]
    private float anchoredApproachAttackerFramingWeight = 0.30f;

    [SerializeField, Min(0f)]
    private float anchoredApproachFarRadius = 9.5f;

    [SerializeField, Min(0f)]
    private float anchoredApproachNearRadius = 8.5f;

    [Header("Generic Clash Impact")]
    [SerializeField, Min(0f)]
    private float genericClashImpactDuration = 0.16f;

    [SerializeField, Min(0f)]
    private float genericClashImpactPushInRadiusAmount = 0.30f;

    [SerializeField, Min(0f)]
    private float genericClashImpactHorizontalShakeAmplitude = 0.09f;

    [SerializeField, Min(0f)]
    private float genericClashImpactShakeCycles = 1.0f;

    [Header("Generic Hit Impact")]
    [SerializeField, Min(0f)]
    private float genericHitImpactDuration = 0.16f;

    [SerializeField, Min(0f)]
    private float genericHitImpactPushInRadiusAmount = 0.38f;

    [SerializeField, Min(0f)]
    private float genericHitImpactHorizontalKickAmount = 0.12f;

    [Header("Generic Guard Impact")]
    [SerializeField, Min(0f)]
    private float fullBlockGuardImpactDuration = 0.14f;

    [SerializeField, Min(0f)]
    private float fullBlockGuardImpactPushInRadiusAmount = 0.22f;

    [SerializeField, Min(0f)]
    private float fullBlockGuardImpactHorizontalShakeAmplitude = 0.055f;

    [SerializeField, Min(0f)]
    private float fullBlockGuardImpactShakeCycles = 1.0f;

    [SerializeField, Min(0f)]
    private float reducedDamageGuardImpactDuration = 0.16f;

    [SerializeField, Min(0f)]
    private float reducedDamageGuardImpactPushInRadiusAmount = 0.30f;

    [SerializeField, Min(0f)]
    private float reducedDamageGuardImpactHorizontalShakeAmplitude = 0.045f;

    [SerializeField, Min(0f)]
    private float reducedDamageGuardImpactShakeCycles = 1.0f;

    private GameObject overlayRoot;
    private CanvasGroup overlayGroup;
    private Coroutine activePresentationCoroutine;
    private Coroutine activeBattleActionEffectCoroutine;
    private Coroutine anchoredApproachCoroutine;
    private bool isIntroPlaying;
    private TurnEndRecoveryPhase turnEndRecoveryPhase;
    private bool isTwoUnitFocusPlaying;
    private bool isAnchoredApproachPlaying;
    private BattleActionEffectType activeBattleActionEffectType;
    private bool presentationInteractionUnlocked;
    private float battleActionEffectBaseWorldX;
    private float battleActionEffectBaseRadius;
    private Transform anchoredApproachAttackerWorldRoot;
    private Transform anchoredApproachDefenderWorldRoot;
    private float anchoredApproachNearSeparation;
    private float anchoredApproachStartSeparation;
    private float anchoredApproachStartWorldX;
    private float anchoredApproachStartRadius;

    public bool IsIntroPlaying => isIntroPlaying;

    private bool IsPresentationPlaying =>
        isIntroPlaying ||
        turnEndRecoveryPhase != TurnEndRecoveryPhase.None ||
        isTwoUnitFocusPlaying ||
        isAnchoredApproachPlaying;

    private bool IsBattleActionEffectPlaying =>
        activeBattleActionEffectType != BattleActionEffectType.None ||
        activeBattleActionEffectCoroutine != null;

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

        CancelAnchoredTwoUnitApproach(false);
        CancelBattleActionEffect();

        HideOverlay();
        cameraController?.SetCinematicControl(false);
        isIntroPlaying = false;
        turnEndRecoveryPhase = TurnEndRecoveryPhase.None;
        isTwoUnitFocusPlaying = false;
    }

    public void PlayBattleIntro()
    {
        if (IsPresentationPlaying || IsBattleActionEffectPlaying)
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
        TryPlayTurnEndTransition(
            () => TryPlayNewTurnStart(null)
        );
    }

    public bool TryPlayTurnEndTransition(System.Action completion)
    {
        if (IsPresentationPlaying || IsBattleActionEffectPlaying)
        {
            return false;
        }

        ResolveReferences();
        if (cameraController == null)
        {
            return false;
        }

        turnEndRecoveryPhase = TurnEndRecoveryPhase.Closing;
        cameraController.SetCinematicControl(true);
        ShowOverlay(0f);
        activePresentationCoroutine = StartCoroutine(
            PlayTurnEndTransitionSequence(completion)
        );
        return true;
    }

    public bool TryPlayNewTurnStart(System.Action completion)
    {
        if (turnEndRecoveryPhase !=
                TurnEndRecoveryPhase.WaitingForNewTurn ||
            activePresentationCoroutine != null)
        {
            return false;
        }

        ResolveReferences();
        if (cameraController == null)
        {
            return false;
        }

        turnEndRecoveryPhase = TurnEndRecoveryPhase.Opening;
        presentationInteractionUnlocked = false;
        if (overlayGroup != null)
        {
            overlayGroup.interactable = true;
            overlayGroup.blocksRaycasts = true;
        }

        activePresentationCoroutine = StartCoroutine(
            PlayNewTurnStartSequence(completion)
        );
        return true;
    }

    public void CancelTurnEndRecovery()
    {
        if (turnEndRecoveryPhase == TurnEndRecoveryPhase.None)
        {
            return;
        }

        if (activePresentationCoroutine != null)
        {
            StopCoroutine(activePresentationCoroutine);
            activePresentationCoroutine = null;
        }

        turnEndRecoveryPhase = TurnEndRecoveryPhase.None;
        HideOverlay();
        cameraController?.SetCinematicControl(false);
    }

    public bool TryPlayTwoUnitFocus(
        BattleUnitViewHandle first,
        BattleUnitViewHandle second,
        bool releaseCinematicControlOnComplete,
        System.Action completion = null
    )
    {
        if (IsPresentationPlaying || IsBattleActionEffectPlaying)
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
                releaseCinematicControlOnComplete,
                completion
            )
        );
        return true;
    }

    public bool CancelTwoUnitFocus(
        bool releaseCinematicControl = true
    )
    {
        if (!isTwoUnitFocusPlaying)
        {
            return false;
        }

        if (activePresentationCoroutine != null)
        {
            StopCoroutine(activePresentationCoroutine);
        }

        activePresentationCoroutine = null;
        isTwoUnitFocusPlaying = false;
        if (releaseCinematicControl)
        {
            cameraController?.SetCinematicControl(false);
        }

        return true;
    }

    public bool TryPlayAnchoredTwoUnitApproach(
        BattleUnitViewHandle attacker,
        BattleUnitViewHandle defender,
        float nearSeparation
    )
    {
        if (IsPresentationPlaying || IsBattleActionEffectPlaying ||
            attacker == null || defender == null ||
            attacker.WorldRoot == null || defender.WorldRoot == null)
        {
            return false;
        }

        ResolveReferences();
        if (cameraController == null)
        {
            return false;
        }

        anchoredApproachAttackerWorldRoot =
            attacker.WorldRoot.transform;
        anchoredApproachDefenderWorldRoot =
            defender.WorldRoot.transform;
        anchoredApproachNearSeparation = Mathf.Max(0f, nearSeparation);
        anchoredApproachStartSeparation = Mathf.Max(
            anchoredApproachNearSeparation,
            Mathf.Abs(
                anchoredApproachAttackerWorldRoot.position.x -
                anchoredApproachDefenderWorldRoot.position.x
            )
        );
        anchoredApproachStartWorldX =
            cameraController.CurrentHorizontalWorldX;
        anchoredApproachStartRadius =
            cameraController.CurrentOrbitRadius;
        isAnchoredApproachPlaying = true;
        cameraController.SetCinematicControl(true);

        ApplyAnchoredTwoUnitApproachFraming();
        anchoredApproachCoroutine = StartCoroutine(
            PlayAnchoredTwoUnitApproachSequence()
        );
        return true;
    }

    public bool FinishAnchoredTwoUnitApproachTracking()
    {
        if (!isAnchoredApproachPlaying)
        {
            return false;
        }

        // Approach结束时用最终WorldRoot再计算一次，随后保留该构图。
        ApplyAnchoredTwoUnitApproachFraming();
        if (anchoredApproachCoroutine != null)
        {
            StopCoroutine(anchoredApproachCoroutine);
        }

        ClearAnchoredTwoUnitApproachState();
        return true;
    }

    public bool CancelAnchoredTwoUnitApproach(
        bool releaseCinematicControl = true
    )
    {
        if (!isAnchoredApproachPlaying)
        {
            return false;
        }

        if (anchoredApproachCoroutine != null)
        {
            StopCoroutine(anchoredApproachCoroutine);
        }

        if (cameraController != null)
        {
            cameraController.SetCinematicHorizontalWorldX(
                anchoredApproachStartWorldX
            );
            cameraController.SetCinematicOrbitRadius(
                anchoredApproachStartRadius
            );
        }

        ClearAnchoredTwoUnitApproachState();
        if (releaseCinematicControl)
        {
            cameraController?.SetCinematicControl(false);
        }

        return true;
    }

    public void ReleaseBattleActionCinematicControl()
    {
        CancelAnchoredTwoUnitApproach(false);
        CancelBattleActionEffect();
        if (IsPresentationPlaying)
        {
            return;
        }

        ResolveReferences();
        cameraController?.SetCinematicControl(false);
    }

    public bool TryPlayGenericClashImpact()
    {
        if (!TryBeginBattleActionEffect(BattleActionEffectType.ClashImpact))
        {
            return false;
        }

        activeBattleActionEffectCoroutine = StartCoroutine(
            PlayGenericClashImpactSequence()
        );
        return true;
    }

    public bool TryPlayGenericHitImpact(float directionSign)
    {
        if (!TryBeginBattleActionEffect(BattleActionEffectType.HitImpact))
        {
            return false;
        }

        float normalizedDirection = directionSign >= 0f ? 1f : -1f;
        activeBattleActionEffectCoroutine = StartCoroutine(
            PlayGenericHitImpactSequence(normalizedDirection)
        );
        return true;
    }

    public bool TryPlayGenericGuardImpact(bool isFullBlock)
    {
        if (!TryBeginBattleActionEffect(BattleActionEffectType.GuardImpact))
        {
            return false;
        }

        activeBattleActionEffectCoroutine = StartCoroutine(
            PlayGenericGuardImpactSequence(isFullBlock)
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

    private IEnumerator PlayTurnEndTransitionSequence(
        System.Action completion
    )
    {
        float startWorldX = cameraController.CurrentHorizontalWorldX;
        float defaultWorldX = cameraController.DefaultHorizontalWorldX;
        float startRadius = cameraController.CurrentOrbitRadius;
        float startVerticalProgress =
            cameraController.CurrentCinematicVerticalViewProgress;

        float cameraDelay = Mathf.Max(0f, turnEndClosingCameraDelay);
        float cameraDuration = Mathf.Max(0f, turnEndClosingCameraDuration);
        float fadeDelay = Mathf.Max(0f, turnEndClosingFadeDelay);
        float fadeDuration = Mathf.Max(0f, turnEndClosingFadeDuration);
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
            float easedCameraProgress = Mathf.Pow(cameraProgress, 3f);
            float easedFadeProgress = Mathf.Pow(fadeProgress, 3f);

            cameraController.SetCinematicHorizontalWorldX(
                Mathf.Lerp(
                    startWorldX,
                    defaultWorldX,
                    easedCameraProgress
                )
            );
            cameraController.SetCinematicOrbitRadius(
                Mathf.Lerp(
                    startRadius,
                    recoveryStartOrbitRadius,
                    easedCameraProgress
                )
            );
            cameraController.SetCinematicVerticalViewProgress(
                Mathf.Lerp(
                    startVerticalProgress,
                    0f,
                    easedCameraProgress
                )
            );
            SetOverlayAlpha(Mathf.Lerp(
                0f,
                turnEndClosingOverlayAlpha,
                easedFadeProgress
            ));

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        cameraController.SetCinematicHorizontalWorldX(defaultWorldX);
        cameraController.SetCinematicOrbitRadius(recoveryStartOrbitRadius);
        cameraController.SetCinematicVerticalViewProgress(0f);
        SetOverlayAlpha(turnEndClosingOverlayAlpha);
        activePresentationCoroutine = null;
        turnEndRecoveryPhase = TurnEndRecoveryPhase.WaitingForNewTurn;
        completion?.Invoke();
    }

    private IEnumerator PlayNewTurnStartSequence(System.Action completion)
    {
        float startRadius = cameraController.CurrentOrbitRadius;
        float startOverlayAlpha = overlayGroup != null
            ? overlayGroup.alpha
            : recoveryStartOverlayAlpha;
        float defaultRadius = cameraController.DefaultOrbitRadius;
        float defaultWorldX = cameraController.DefaultHorizontalWorldX;
        // Opening从midpoint立即启动，不复用Closing的起步Delay。
        float cameraDelay = 0f;
        float cameraDuration = Mathf.Max(0f, recoveryCameraDuration);
        float fadeDelay = 0f;
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

            cameraController.SetCinematicHorizontalWorldX(defaultWorldX);
            cameraController.SetCinematicOrbitRadius(
                Mathf.Lerp(startRadius, defaultRadius, easedCameraProgress)
            );
            cameraController.SetCinematicVerticalViewProgress(0f);
            SetOverlayAlpha(Mathf.Lerp(
                startOverlayAlpha,
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

        cameraController.SetCinematicHorizontalWorldX(defaultWorldX);
        cameraController.SetCinematicOrbitRadius(defaultRadius);
        cameraController.SetCinematicVerticalViewProgress(0f);
        SetOverlayAlpha(0f);
        FinishPresentation(true);
        completion?.Invoke();
    }

    private IEnumerator PlayTwoUnitFocusSequence(
        float startWorldX,
        float targetWorldX,
        float startRadius,
        float targetRadius,
        float startVerticalProgress,
        float targetVerticalProgress,
        bool releaseCinematicControlOnComplete,
        System.Action completion
    )
    {
        float duration = Mathf.Max(0f, twoUnitFocusDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = duration > Mathf.Epsilon
                ? Mathf.Clamp01(elapsed / duration)
                : 1f;
            float easedProgress = twoUnitFocusEasingMode ==
                TwoUnitFocusEasingMode.SmoothStep
                    ? Mathf.SmoothStep(0f, 1f, progress)
                    : 1f - Mathf.Pow(1f - progress, 3f);

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
        FinishTwoUnitFocus(
            releaseCinematicControlOnComplete,
            completion
        );
    }

    private IEnumerator PlayAnchoredTwoUnitApproachSequence()
    {
        while (isAnchoredApproachPlaying)
        {
            if (anchoredApproachAttackerWorldRoot == null ||
                anchoredApproachDefenderWorldRoot == null)
            {
                if (cameraController != null)
                {
                    cameraController.SetCinematicHorizontalWorldX(
                        anchoredApproachStartWorldX
                    );
                    cameraController.SetCinematicOrbitRadius(
                        anchoredApproachStartRadius
                    );
                }

                ClearAnchoredTwoUnitApproachState();
                yield break;
            }

            ApplyAnchoredTwoUnitApproachFraming();
            yield return null;
        }
    }

    private void ApplyAnchoredTwoUnitApproachFraming()
    {
        if (cameraController == null ||
            anchoredApproachAttackerWorldRoot == null ||
            anchoredApproachDefenderWorldRoot == null)
        {
            return;
        }

        float attackerX = anchoredApproachAttackerWorldRoot.position.x;
        float defenderX = anchoredApproachDefenderWorldRoot.position.x;
        float framingWeight = Mathf.Clamp(
            anchoredApproachAttackerFramingWeight,
            0f,
            0.5f
        );
        float targetWorldX = Mathf.Lerp(defenderX, attackerX, framingWeight);
        float separation = Mathf.Abs(attackerX - defenderX);
        float separationRange = anchoredApproachStartSeparation -
            anchoredApproachNearSeparation;
        float farProgress = separationRange > Mathf.Epsilon
            ? Mathf.Clamp01(
                (separation - anchoredApproachNearSeparation) /
                separationRange
            )
            : 0f;
        float nearRadius = Mathf.Max(0f, anchoredApproachNearRadius);
        float farRadius = Mathf.Max(
            nearRadius,
            anchoredApproachFarRadius
        );

        cameraController.SetCinematicHorizontalWorldX(targetWorldX);
        cameraController.SetCinematicOrbitRadius(
            Mathf.Lerp(nearRadius, farRadius, farProgress)
        );
    }

    private void ClearAnchoredTwoUnitApproachState()
    {
        anchoredApproachCoroutine = null;
        isAnchoredApproachPlaying = false;
        anchoredApproachAttackerWorldRoot = null;
        anchoredApproachDefenderWorldRoot = null;
        anchoredApproachNearSeparation = 0f;
        anchoredApproachStartSeparation = 0f;
    }

    private IEnumerator PlayGenericClashImpactSequence()
    {
        float duration = Mathf.Max(0f, genericClashImpactDuration);
        float elapsed = 0f;
        float pushInRadius = battleActionEffectBaseRadius -
            Mathf.Max(0f, genericClashImpactPushInRadiusAmount);

        while (elapsed < duration)
        {
            float progress = duration > Mathf.Epsilon
                ? Mathf.Clamp01(elapsed / duration)
                : 1f;
            float radius;
            if (progress <= 0.3f)
            {
                float pushProgress = Mathf.SmoothStep(
                    0f,
                    1f,
                    progress / 0.3f
                );
                radius = Mathf.Lerp(
                    battleActionEffectBaseRadius,
                    pushInRadius,
                    pushProgress
                );
            }
            else
            {
                float returnProgress = Mathf.SmoothStep(
                    0f,
                    1f,
                    (progress - 0.3f) / 0.7f
                );
                radius = Mathf.Lerp(
                    pushInRadius,
                    battleActionEffectBaseRadius,
                    returnProgress
                );
            }

            float shake = Mathf.Sin(
                progress * Mathf.PI * 2f *
                genericClashImpactShakeCycles
            ) * genericClashImpactHorizontalShakeAmplitude *
                (1f - progress);
            cameraController.SetCinematicOrbitRadius(radius);
            cameraController.SetCinematicHorizontalWorldX(
                battleActionEffectBaseWorldX + shake
            );

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        CompleteBattleActionEffect();
    }

    private IEnumerator PlayGenericHitImpactSequence(float directionSign)
    {
        float duration = Mathf.Max(0f, genericHitImpactDuration);
        float elapsed = 0f;
        float pushInRadius = battleActionEffectBaseRadius -
            Mathf.Max(0f, genericHitImpactPushInRadiusAmount);
        float fullKick = directionSign *
            Mathf.Max(0f, genericHitImpactHorizontalKickAmount);

        while (elapsed < duration)
        {
            float progress = duration > Mathf.Epsilon
                ? Mathf.Clamp01(elapsed / duration)
                : 1f;
            float radius = EvaluateImpactRadius(
                progress,
                battleActionEffectBaseRadius,
                pushInRadius
            );
            float kickProgress = progress <= 0.2f
                ? Mathf.SmoothStep(0f, 1f, progress / 0.2f)
                : Mathf.SmoothStep(
                    1f,
                    0f,
                    (progress - 0.2f) / 0.8f
                );

            cameraController.SetCinematicOrbitRadius(radius);
            cameraController.SetCinematicHorizontalWorldX(
                battleActionEffectBaseWorldX + fullKick * kickProgress
            );

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        CompleteBattleActionEffect();
    }

    private IEnumerator PlayGenericGuardImpactSequence(bool isFullBlock)
    {
        float duration = Mathf.Max(
            0f,
            isFullBlock
                ? fullBlockGuardImpactDuration
                : reducedDamageGuardImpactDuration
        );
        float pushInAmount = Mathf.Max(
            0f,
            isFullBlock
                ? fullBlockGuardImpactPushInRadiusAmount
                : reducedDamageGuardImpactPushInRadiusAmount
        );
        float shakeAmplitude = Mathf.Max(
            0f,
            isFullBlock
                ? fullBlockGuardImpactHorizontalShakeAmplitude
                : reducedDamageGuardImpactHorizontalShakeAmplitude
        );
        float shakeCycles = Mathf.Max(
            0f,
            isFullBlock
                ? fullBlockGuardImpactShakeCycles
                : reducedDamageGuardImpactShakeCycles
        );
        float pushInRadius = battleActionEffectBaseRadius - pushInAmount;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = duration > Mathf.Epsilon
                ? Mathf.Clamp01(elapsed / duration)
                : 1f;
            float radius = EvaluateImpactRadius(
                progress,
                battleActionEffectBaseRadius,
                pushInRadius
            );
            float shake = Mathf.Sin(
                progress * Mathf.PI * 2f * shakeCycles
            ) * shakeAmplitude * (1f - progress);

            cameraController.SetCinematicOrbitRadius(radius);
            cameraController.SetCinematicHorizontalWorldX(
                battleActionEffectBaseWorldX + shake
            );

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        CompleteBattleActionEffect();
    }

    private static float EvaluateImpactRadius(
        float progress,
        float baseRadius,
        float pushInRadius
    )
    {
        if (progress <= 0.3f)
        {
            float pushProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress / 0.3f
            );
            return Mathf.Lerp(baseRadius, pushInRadius, pushProgress);
        }

        float returnProgress = Mathf.SmoothStep(
            0f,
            1f,
            (progress - 0.3f) / 0.7f
        );
        return Mathf.Lerp(pushInRadius, baseRadius, returnProgress);
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
        turnEndRecoveryPhase = TurnEndRecoveryPhase.None;
        isTwoUnitFocusPlaying = false;
    }

    private void FinishTwoUnitFocus(
        bool releaseCinematicControlOnComplete,
        System.Action completion
    )
    {
        activePresentationCoroutine = null;
        isTwoUnitFocusPlaying = false;

        if (releaseCinematicControlOnComplete)
        {
            cameraController?.SetCinematicControl(false);
        }

        completion?.Invoke();
    }

    private bool TryBeginBattleActionEffect(BattleActionEffectType effectType)
    {
        if (IsPresentationPlaying || IsBattleActionEffectPlaying)
        {
            return false;
        }

        ResolveReferences();
        if (cameraController == null ||
            !cameraController.IsCinematicControlActive)
        {
            return false;
        }

        battleActionEffectBaseWorldX =
            cameraController.CurrentHorizontalWorldX;
        battleActionEffectBaseRadius =
            cameraController.CurrentOrbitRadius;
        activeBattleActionEffectType = effectType;
        return true;
    }

    private void CompleteBattleActionEffect()
    {
        RestoreBattleActionEffectSnapshot();
        activeBattleActionEffectCoroutine = null;
        activeBattleActionEffectType = BattleActionEffectType.None;
    }

    private void CancelBattleActionEffect()
    {
        if (!IsBattleActionEffectPlaying)
        {
            return;
        }

        if (activeBattleActionEffectCoroutine != null)
        {
            StopCoroutine(activeBattleActionEffectCoroutine);
        }

        CompleteBattleActionEffect();
    }

    private void RestoreBattleActionEffectSnapshot()
    {
        if (cameraController == null)
        {
            return;
        }

        cameraController.SetCinematicHorizontalWorldX(
            battleActionEffectBaseWorldX
        );
        cameraController.SetCinematicOrbitRadius(
            battleActionEffectBaseRadius
        );
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
