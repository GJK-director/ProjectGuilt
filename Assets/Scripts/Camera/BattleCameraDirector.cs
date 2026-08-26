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

    [Header("Battle Intro")]
    [SerializeField]
    private float introStartOrbitRadius = 8.5f;

    [SerializeField, Min(0f)]
    private float introCameraDelay = 0.20f;

    [SerializeField, Min(0.01f)]
    private float introCameraDuration = 1.25f;

    [SerializeField, Min(0f)]
    private float introFadeDelay = 0.10f;

    [SerializeField, Min(0.01f)]
    private float introFadeDuration = 1.50f;

    private GameObject overlayRoot;
    private CanvasGroup overlayGroup;
    private Coroutine introCoroutine;
    private bool isIntroPlaying;

    public bool IsIntroPlaying => isIntroPlaying;

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
        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        HideOverlay();
        cameraController?.SetCinematicControl(false);
        isIntroPlaying = false;
    }

    public void PlayBattleIntro()
    {
        if (isIntroPlaying)
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
        ShowOverlay();

        if (battleUIController == null)
        {
            Debug.LogError(
                "BattleCameraDirector找不到BattleSimpleUIController，" +
                "Battle Intro已安全取消。",
                this
            );
            FinishIntro(false);
            return;
        }

        introCoroutine = StartCoroutine(PlayBattleIntroSequence());
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
                FinishIntro(false);
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
            SetOverlayAlpha(1f - easedFadeProgress);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        cameraController.SetCinematicOrbitRadius(defaultRadius);
        SetOverlayAlpha(0f);
        FinishIntro(true);
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

    private void ShowOverlay()
    {
        EnsureOverlay();
        overlayRoot.SetActive(true);
        overlayGroup.alpha = 1f;
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

    private void FinishIntro(bool completedNormally)
    {
        if (completedNormally && cameraController != null)
        {
            cameraController.SetCinematicOrbitRadius(
                cameraController.DefaultOrbitRadius
            );
        }

        HideOverlay();
        cameraController?.SetCinematicControl(false);
        introCoroutine = null;
        isIntroPlaying = false;
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
