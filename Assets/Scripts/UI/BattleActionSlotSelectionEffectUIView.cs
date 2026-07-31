using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleActionSlotSelectionEffectUIView : MonoBehaviour
{
    [Header("基础引用")]
    [SerializeField] private RectTransform effectRoot;
    [SerializeField] private Image effectImage;

    [Header("扩散动画")]
    [Min(0f)]
    [SerializeField] private float pulseStartScale = 0.15f;

    [Min(0f)]
    [SerializeField] private float pulseSharpness = 18f;

    [Min(0f)]
    [SerializeField] private float pulseSnapDistance = 0.001f;

    [SerializeField] private bool hideWhenIdle = true;

    private Vector3 targetScale;
    private bool hasCachedTargetScale;
    private bool persistentVisible;
    private bool warnedMissingRoot;
    private bool warnedMissingImage;
    private Coroutine pulseCoroutine;

    public bool IsPulsePlaying => pulseCoroutine != null;
    public bool IsPersistentVisible => persistentVisible;
    public bool IsVisible =>
        effectRoot != null &&
        effectRoot.gameObject.activeInHierarchy &&
        effectImage != null &&
        effectImage.enabled;
    public Vector3 TargetScale => targetScale;
    internal int ActivePulseCount => pulseCoroutine != null ? 1 : 0;

    void Awake()
    {
        EnsureInitialized();
    }

    void OnEnable()
    {
        EnsureInitialized();
    }

    public void SetPersistentVisible(bool visible)
    {
        EnsureInitialized();
        persistentVisible = visible;

        if (visible)
        {
            SetEffectVisible(true);
            return;
        }

        if (!IsPulsePlaying)
        {
            ApplyIdleVisibility();
        }
    }

    public void ShowImmediate()
    {
        EnsureInitialized();
        StopPulse();

        if (!CanDisplay())
        {
            return;
        }

        effectRoot.localScale = targetScale;
        SetEffectVisible(true);
    }

    public void HideImmediate()
    {
        EnsureInitialized();
        StopPulse();

        if (effectRoot != null && hasCachedTargetScale)
        {
            effectRoot.localScale = targetScale;
        }

        SetEffectVisible(false);
    }

    public void PlayPulse()
    {
        EnsureInitialized();
        StopPulse();

        if (!CanDisplay())
        {
            return;
        }

        SetEffectVisible(true);
        effectRoot.localScale =
            targetScale * Mathf.Max(0f, pulseStartScale);

        if (!isActiveAndEnabled ||
            !gameObject.activeInHierarchy ||
            pulseSharpness <= 0f)
        {
            CompletePulseImmediately();
            return;
        }

        pulseCoroutine = StartCoroutine(TrackPulse());
    }

    public void CompletePulseImmediately()
    {
        EnsureInitialized();
        StopPulse();

        if (effectRoot != null && hasCachedTargetScale)
        {
            effectRoot.localScale = targetScale;
        }

        ApplyIdleVisibility();
    }

    public void StopAndReset()
    {
        EnsureInitialized();
        StopPulse();
        persistentVisible = false;

        if (effectRoot != null && hasCachedTargetScale)
        {
            effectRoot.localScale = targetScale;
        }

        SetEffectVisible(false);
    }

    internal bool AdvancePulseForTesting(float unscaledDeltaTime)
    {
        if (!IsPulsePlaying || !CanDisplay())
        {
            return false;
        }

        bool completed = AdvancePulse(unscaledDeltaTime);
        if (completed)
        {
            StopPulse();
            ApplyIdleVisibility();
        }

        return completed;
    }

    internal void ConfigureTestVisuals(
        RectTransform root,
        Image image
    )
    {
        effectRoot = root;
        effectImage = image;
        hasCachedTargetScale = false;
        EnsureInitialized();
        StopAndReset();
    }

    private IEnumerator TrackPulse()
    {
        while (true)
        {
            yield return null;

            if (AdvancePulse(Time.unscaledDeltaTime))
            {
                pulseCoroutine = null;
                ApplyIdleVisibility();
                yield break;
            }
        }
    }

    private bool AdvancePulse(float unscaledDeltaTime)
    {
        Vector3 nextScale = BattleUIExponentialSmoothing.Smooth(
            effectRoot.localScale,
            targetScale,
            pulseSharpness,
            unscaledDeltaTime
        );
        bool reached =
            Vector3.Distance(nextScale, targetScale) <=
            Mathf.Max(0f, pulseSnapDistance);

        effectRoot.localScale = reached ? targetScale : nextScale;
        return reached;
    }

    private void EnsureInitialized()
    {
        if (effectRoot == null)
        {
            effectRoot = transform as RectTransform;
        }

        if (effectImage == null)
        {
            effectImage = GetComponent<Image>();
        }

        if (!hasCachedTargetScale && effectRoot != null)
        {
            targetScale = effectRoot.localScale;
            hasCachedTargetScale = true;
        }

        if (effectImage != null)
        {
            effectImage.raycastTarget = false;
        }

        WarnMissingReferencesOnce();
    }

    private bool CanDisplay()
    {
        return effectRoot != null &&
            effectImage != null &&
            hasCachedTargetScale;
    }

    private void ApplyIdleVisibility()
    {
        if (effectRoot != null && hasCachedTargetScale)
        {
            effectRoot.localScale = targetScale;
        }

        SetEffectVisible(persistentVisible || !hideWhenIdle);
    }

    private void SetEffectVisible(bool visible)
    {
        if (visible &&
            effectRoot != null &&
            !effectRoot.gameObject.activeSelf)
        {
            effectRoot.gameObject.SetActive(true);
        }

        if (effectImage != null)
        {
            effectImage.enabled = visible;
        }
    }

    private void StopPulse()
    {
        if (pulseCoroutine == null)
        {
            return;
        }

        StopCoroutine(pulseCoroutine);
        pulseCoroutine = null;
    }

    private void WarnMissingReferencesOnce()
    {
        if (effectRoot == null && !warnedMissingRoot)
        {
            Debug.LogWarning(
                "BattleActionSlotSelectionEffectUIView 缺少 Effect Root。",
                this
            );
            warnedMissingRoot = true;
        }

        if (effectImage == null && !warnedMissingImage)
        {
            Debug.LogWarning(
                "BattleActionSlotSelectionEffectUIView 缺少 Effect Image。",
                this
            );
            warnedMissingImage = true;
        }
    }

    void OnDisable()
    {
        StopAndReset();
    }

    void OnDestroy()
    {
        StopAndReset();
    }
}
