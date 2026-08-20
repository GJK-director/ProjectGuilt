using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 控制单个战斗角色当前显示的静态姿态，不负责动画流程或角色移动。
public sealed class BattleCharacterPresentationController : MonoBehaviour
{
    private sealed class ActiveAfterimage
    {
        public GameObject root;
        public Coroutine fadeCoroutine;
    }

    [SerializeField] private string presentationKey;
    [SerializeField] private SpriteRenderer characterSprite;
    [SerializeField] private Transform bodyVisualRoot;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite sprintSprite;
    [SerializeField] private Sprite slashSprite;
    [SerializeField] private Sprite hitSprite;
    [SerializeField] private Sprite guardSprite;
    [SerializeField] private Sprite dodgeSprite;
    [SerializeField] private SpriteRenderer perfectGuardEffect;
    [SerializeField] private SpriteRenderer slashBackEffect;
    [SerializeField] private SpriteRenderer slashFrontEffect;
    [SerializeField] private float slashEffectFadeInDuration = 0.05f;
    [SerializeField] private float slashEffectShakeDuration = 0.12f;
    [SerializeField] private float slashEffectShakeAmplitude = 0.08f;
    [SerializeField] private float slashEffectFadeOutDuration = 0.08f;
    [SerializeField] private float slashImpactDelay = 0.05f;
    [SerializeField] private float afterimageLifetime = 0.18f;
    [SerializeField, Range(0f, 1f)]
    private float afterimageStartAlpha = 0.35f;
    [SerializeField] private float hitRecoilDistance = 0.15f;
    [SerializeField] private float hitRecoilDuration = 0.06f;
    [SerializeField] private float hitShakeAmplitude = 0.04f;
    [SerializeField] private float hitShakeDuration = 0.12f;
    [SerializeField] private float hitPoseHoldDuration = 0.16f;
    [SerializeField] private float parryRecoilDistance = 0.20f;
    [SerializeField] private float parryRecoilDuration = 0.08f;
    [SerializeField] private float parryShakeAmplitude = 0.03f;
    [SerializeField] private float parryShakeDuration = 0.10f;
    [SerializeField] private float parryHoldDuration = 0.10f;
    [SerializeField] private float guardRecoilDistance = 0.10f;
    [SerializeField] private float guardRecoilDuration = 0.06f;
    [SerializeField] private float guardShakeAmplitude = 0.025f;
    [SerializeField] private float guardShakeDuration = 0.10f;
    [SerializeField] private float guardHoldDuration = 0.10f;
    [SerializeField] private float perfectGuardEffectHoldDuration = 0.06f;
    [SerializeField] private float perfectGuardEffectFadeOutDuration = 0.10f;
    [SerializeField] private float dodgeHorizontalDistance = 0.20f;
    [SerializeField] private float dodgeDuration = 0.18f;

    private Vector3 bodyBaseLocalPosition;
    private Vector3 bodyMotionOffset;
    private Vector3 bodyShakeOffset;
    private Vector3 dodgeMotionOffset;
    private Coroutine dodgeMotionCoroutine;
    private Action dodgeMotionFinishedCallback;
    private bool bodyBaseStateCached;
    private Vector3 slashBackOriginalLocalPosition;
    private Vector3 slashFrontOriginalLocalPosition;
    private Color slashBackOriginalColor;
    private Color slashFrontOriginalColor;
    private bool slashBackStateCached;
    private bool slashFrontStateCached;
    private Color perfectGuardEffectOriginalColor;
    private bool perfectGuardEffectStateCached;
    private bool presentationPaused;
    private readonly List<ActiveAfterimage> activeAfterimages =
        new List<ActiveAfterimage>();

    public string PresentationKey => presentationKey ?? string.Empty;
    public SpriteRenderer CharacterSpriteRenderer => characterSprite;

    void Awake()
    {
        CacheBodyVisualState();
        ApplyBodyVisualOffset();
        CacheSlashEffectState();
        CachePerfectGuardEffectState();
        SetIdle();
        ClearSlashEffect();
        ClearPerfectGuardEffect();
    }

    void OnDisable()
    {
        presentationPaused = false;
        ClearBodyVisualOffsets();
        ClearAfterimages();
        ClearSlashEffect();
        ClearPerfectGuardEffect();
        SetIdle();
    }

    public void SetIdle()
    {
        SetPose(idleSprite);
    }

    public void SetSprint()
    {
        SetPose(sprintSprite);
    }

    public void SetSlash()
    {
        SetPose(slashSprite);
    }

    public void SetHit()
    {
        SetPose(hitSprite);
    }

    public void SetGuard()
    {
        SetPose(guardSprite);
    }

    public void SetDodge()
    {
        SetPose(dodgeSprite);
    }

    public void SetPresentationPaused(bool paused)
    {
        presentationPaused = paused;
    }

    public void ResetToStableIdlePresentation()
    {
        presentationPaused = false;
        ClearBodyVisualOffsets();
        ClearAfterimages();
        ClearSlashEffect();
        ClearPerfectGuardEffect();
        SetIdle();
    }

    public void FinishSlashPresentation()
    {
        // 正常收尾只清瞬时状态，最后明确设置的Slash Pose继续保留。
        presentationPaused = false;
        ClearSlashEffect();
        bodyMotionOffset = Vector3.zero;
        bodyShakeOffset = Vector3.zero;
        ApplyBodyVisualOffset();
    }

    public void FinishHitReaction()
    {
        // 回到Body局部中心不等于恢复Idle，Hit Pose由后续生命周期决定。
        presentationPaused = false;
        bodyMotionOffset = Vector3.zero;
        bodyShakeOffset = Vector3.zero;
        ApplyBodyVisualOffset();
    }

    public void FinishGuardPresentation()
    {
        // Guard正常结束后关闭瞬时反馈，但保留最后的Guard Pose。
        presentationPaused = false;
        ClearPerfectGuardEffect();
        bodyMotionOffset = Vector3.zero;
        bodyShakeOffset = Vector3.zero;
        ApplyBodyVisualOffset();
    }

    public void ClearPerfectGuardEffect()
    {
        CachePerfectGuardEffectState();
        if (perfectGuardEffect == null)
        {
            return;
        }

        if (perfectGuardEffectStateCached)
        {
            perfectGuardEffect.color = perfectGuardEffectOriginalColor;
        }

        perfectGuardEffect.enabled = false;
    }

    public void ClearBodyVisualOffsets()
    {
        CancelDodgeMotion();
        bodyMotionOffset = Vector3.zero;
        bodyShakeOffset = Vector3.zero;
        dodgeMotionOffset = Vector3.zero;
        ApplyBodyVisualOffset();
    }

    public bool PlayDodgeMotion(
        float firstDirectionSign,
        Action finishedCallback = null
    )
    {
        FinishDodgePresentation();
        CacheBodyVisualState();
        if (!isActiveAndEnabled || !bodyBaseStateCached || bodyVisualRoot == null)
        {
            return false;
        }

        float safeDistance = Mathf.Max(0f, dodgeHorizontalDistance);
        float safeDuration = Mathf.Max(0f, dodgeDuration);
        dodgeMotionFinishedCallback = finishedCallback;
        if (safeDistance <= 0f || safeDuration <= 0f)
        {
            CompleteDodgeMotion();
            return true;
        }

        float normalizedDirection = firstDirectionSign >= 0f ? 1f : -1f;
        dodgeMotionCoroutine = StartCoroutine(
            RunDodgeMotion(normalizedDirection, safeDistance, safeDuration)
        );
        return true;
    }

    public void FinishDodgePresentation()
    {
        CancelDodgeMotion();
        dodgeMotionOffset = Vector3.zero;
        ApplyBodyVisualOffset();
    }

    public void ShowSlashEffect()
    {
        if (slashBackEffect != null)
        {
            slashBackEffect.enabled = true;
        }

        if (slashFrontEffect != null)
        {
            slashFrontEffect.enabled = true;
        }
    }

    public void ClearSlashEffect()
    {
        CacheSlashEffectState();

        if (slashBackEffect != null)
        {
            if (slashBackStateCached)
            {
                slashBackEffect.transform.localPosition =
                    slashBackOriginalLocalPosition;
                slashBackEffect.color = slashBackOriginalColor;
            }
            slashBackEffect.enabled = false;
        }

        if (slashFrontEffect != null)
        {
            if (slashFrontStateCached)
            {
                slashFrontEffect.transform.localPosition =
                    slashFrontOriginalLocalPosition;
                slashFrontEffect.color = slashFrontOriginalColor;
            }
            slashFrontEffect.enabled = false;
        }
    }

    public IEnumerator PlaySlashEffect(Action onImpact = null)
    {
        CacheSlashEffectState();
        RestoreSlashEffectPositions();
        SetSlashEffectAlpha(0f);
        ShowSlashEffect();

        bool impactTriggered = false;
        if (slashEffectFadeInDuration <= 0f)
        {
            SetSlashEffectAlpha(1f);
            InvokeImpactOnce(onImpact, ref impactTriggered);
        }

        float revealAndShakeDuration = Mathf.Max(
            Mathf.Max(0f, slashEffectFadeInDuration),
            Mathf.Max(0f, slashEffectShakeDuration)
        );
        float elapsed = 0f;
        while (elapsed < revealAndShakeDuration)
        {
            if (!isActiveAndEnabled)
            {
                ClearSlashEffect();
                yield break;
            }

            if (presentationPaused)
            {
                yield return null;
                continue;
            }

            elapsed = Mathf.Min(
                revealAndShakeDuration,
                elapsed + Time.deltaTime
            );
            float fadeInT = slashEffectFadeInDuration <= 0f
                ? 1f
                : BattlePresentationEasing.EaseOutQuad(
                    elapsed / slashEffectFadeInDuration
                );
            SetSlashEffectAlpha(fadeInT);
            ApplySlashEffectShake(elapsed);

            if (!impactTriggered &&
                elapsed >= slashEffectFadeInDuration)
            {
                SetSlashEffectAlpha(1f);
                InvokeImpactOnce(onImpact, ref impactTriggered);
            }

            yield return null;
        }

        SetSlashEffectAlpha(1f);
        InvokeImpactOnce(onImpact, ref impactTriggered);

        // Impact 回调可能刚刚开启 Hit Stop，等待恢复后再进入淡出阶段。
        while (presentationPaused)
        {
            if (!isActiveAndEnabled)
            {
                ClearSlashEffect();
                yield break;
            }

            yield return null;
        }

        RestoreSlashEffectPositions();

        if (slashEffectFadeOutDuration > 0f)
        {
            elapsed = 0f;
            while (elapsed < slashEffectFadeOutDuration)
            {
                if (!isActiveAndEnabled)
                {
                    ClearSlashEffect();
                    yield break;
                }

                if (presentationPaused)
                {
                    yield return null;
                    continue;
                }

                float fadeOutT = BattlePresentationEasing.EaseOutQuad(
                    elapsed / slashEffectFadeOutDuration
                );
                SetSlashEffectAlpha(1f - fadeOutT);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        SetSlashEffectAlpha(0f);
        ClearSlashEffect();
    }

    public IEnumerator PlaySlashPresentation(
        float directionSign,
        Action onImpact = null,
        bool playAttackEffect = true
    )
    {
        // Default Slash只定义姿态、特效与命中时序；角色/卡牌专属位移由更高层组合。
        _ = directionSign;
        float safeImpactDelay = Mathf.Max(0f, slashImpactDelay);
        float impactElapsed = 0f;
        bool impactTriggered = false;
        IEnumerator effect = playAttackEffect
            ? PlaySlashEffect()
            : null;
        bool effectRunning = effect != null;

        while (!impactTriggered || effectRunning)
        {
            if (!isActiveAndEnabled)
            {
                ClearSlashEffect();
                yield break;
            }

            if (effectRunning)
            {
                effectRunning = effect.MoveNext();
            }

            if (!impactTriggered && !presentationPaused)
            {
                impactElapsed = Mathf.Min(
                    safeImpactDelay,
                    impactElapsed + Time.deltaTime
                );
                if (impactElapsed >= safeImpactDelay)
                {
                    InvokeImpactOnce(onImpact, ref impactTriggered);
                }
            }

            if (!impactTriggered || effectRunning)
            {
                yield return null;
            }
        }
    }

    public IEnumerator PlayHitReaction(
        Transform worldRoot,
        float recoilDirectionSign
    )
    {
        yield return PlayHitReactionInternal(
            worldRoot,
            recoilDirectionSign,
            true
        );
    }

    public IEnumerator PlaySustainedHitReaction(
        Transform worldRoot,
        float recoilDirectionSign
    )
    {
        yield return PlayHitReactionInternal(
            worldRoot,
            recoilDirectionSign,
            false
        );
    }

    public IEnumerator PlayParryRecoil(float attackDirectionSign)
    {
        Vector3 startMotionOffset = bodyMotionOffset;
        bodyShakeOffset = Vector3.zero;
        ApplyBodyVisualOffset();

        float normalizedAttackDirection = attackDirectionSign >= 0f
            ? 1f
            : -1f;
        Vector3 maximumRecoilOffset = Vector3.right *
            -normalizedAttackDirection * parryRecoilDistance;
        float recoilDuration = Mathf.Max(0f, parryRecoilDuration);
        float shakeDuration = Mathf.Max(0f, parryShakeDuration);
        float holdDuration = Mathf.Max(0f, parryHoldDuration);
        float recoilElapsed = 0f;
        float shakeElapsed = 0f;
        float holdElapsed = 0f;

        if (recoilDuration <= 0f)
        {
            bodyMotionOffset = maximumRecoilOffset;
        }

        ApplyBodyVisualOffset();
        while (recoilElapsed < recoilDuration ||
            shakeElapsed < shakeDuration ||
            holdElapsed < holdDuration)
        {
            if (!isActiveAndEnabled)
            {
                FinishSlashPresentation();
                yield break;
            }

            if (presentationPaused)
            {
                yield return null;
                continue;
            }

            float deltaTime = Time.deltaTime;
            if (recoilElapsed < recoilDuration)
            {
                recoilElapsed = Mathf.Min(
                    recoilDuration,
                    recoilElapsed + deltaTime
                );
                float recoilT = BattlePresentationEasing.EaseOutQuad(
                    recoilElapsed / recoilDuration
                );
                bodyMotionOffset = startMotionOffset +
                    (maximumRecoilOffset - startMotionOffset) * recoilT;
            }
            else
            {
                bodyMotionOffset = maximumRecoilOffset;
            }

            if (shakeElapsed < shakeDuration)
            {
                shakeElapsed = Mathf.Min(
                    shakeDuration,
                    shakeElapsed + deltaTime
                );
                float shakeT = shakeElapsed / shakeDuration;
                float currentAmplitude = Mathf.Max(
                    0f,
                    parryShakeAmplitude
                ) * (1f - BattlePresentationEasing.EaseOutQuad(shakeT));
                Vector2 randomDirection = UnityEngine.Random.insideUnitCircle;
                bodyShakeOffset = new Vector3(
                    randomDirection.x,
                    randomDirection.y,
                    0f
                ) * currentAmplitude;
            }
            else
            {
                bodyShakeOffset = Vector3.zero;
            }

            holdElapsed = Mathf.Min(holdDuration, holdElapsed + deltaTime);
            ApplyBodyVisualOffset();
            yield return null;
        }

        bodyMotionOffset = maximumRecoilOffset;
        bodyShakeOffset = Vector3.zero;
        ApplyBodyVisualOffset();
    }

    public IEnumerator PlayGuardRecoil(float recoilDirectionSign)
    {
        SetGuard();
        bodyMotionOffset = Vector3.zero;
        bodyShakeOffset = Vector3.zero;
        ApplyBodyVisualOffset();

        float normalizedDirection = recoilDirectionSign >= 0f ? 1f : -1f;
        Vector3 maximumRecoilOffset = Vector3.right *
            normalizedDirection * guardRecoilDistance;
        float recoilDuration = Mathf.Max(0f, guardRecoilDuration);
        float shakeDuration = Mathf.Max(0f, guardShakeDuration);
        float holdDuration = Mathf.Max(0f, guardHoldDuration);
        float recoilElapsed = 0f;
        float shakeElapsed = 0f;
        float holdElapsed = 0f;

        if (recoilDuration <= 0f)
        {
            bodyMotionOffset = maximumRecoilOffset;
        }

        ApplyBodyVisualOffset();
        while (recoilElapsed < recoilDuration ||
            shakeElapsed < shakeDuration ||
            holdElapsed < holdDuration)
        {
            if (!isActiveAndEnabled)
            {
                FinishGuardPresentation();
                yield break;
            }

            if (presentationPaused)
            {
                yield return null;
                continue;
            }

            float deltaTime = Time.deltaTime;
            if (recoilElapsed < recoilDuration)
            {
                recoilElapsed = Mathf.Min(
                    recoilDuration,
                    recoilElapsed + deltaTime
                );
                float recoilT = BattlePresentationEasing.EaseOutQuad(
                    recoilElapsed / recoilDuration
                );
                bodyMotionOffset = maximumRecoilOffset * recoilT;
            }
            else
            {
                bodyMotionOffset = maximumRecoilOffset;
            }

            if (shakeElapsed < shakeDuration)
            {
                shakeElapsed = Mathf.Min(
                    shakeDuration,
                    shakeElapsed + deltaTime
                );
                float shakeT = shakeElapsed / shakeDuration;
                float currentAmplitude = Mathf.Max(
                    0f,
                    guardShakeAmplitude
                ) * (1f - BattlePresentationEasing.EaseOutQuad(shakeT));
                Vector2 randomDirection = UnityEngine.Random.insideUnitCircle;
                bodyShakeOffset = new Vector3(
                    randomDirection.x,
                    randomDirection.y,
                    0f
                ) * currentAmplitude;
            }
            else
            {
                bodyShakeOffset = Vector3.zero;
            }

            holdElapsed = Mathf.Min(holdDuration, holdElapsed + deltaTime);
            ApplyBodyVisualOffset();
            yield return null;
        }

        bodyMotionOffset = maximumRecoilOffset;
        bodyShakeOffset = Vector3.zero;
        ApplyBodyVisualOffset();
    }

    public IEnumerator PlayPerfectGuardEffect()
    {
        CachePerfectGuardEffectState();
        if (!perfectGuardEffectStateCached || perfectGuardEffect == null)
        {
            yield break;
        }

        SetPerfectGuardEffectAlpha(1f);
        perfectGuardEffect.enabled = true;

        yield return WaitForPerfectGuardEffect(
            perfectGuardEffectHoldDuration
        );
        yield return RunPerfectGuardEffectPhase(
            perfectGuardEffectFadeOutDuration,
            1f,
            0f
        );

        ClearPerfectGuardEffect();
    }

    private IEnumerator PlayHitReactionInternal(
        Transform worldRoot,
        float recoilDirectionSign,
        bool finishAutomatically
    )
    {
        SetHit();
        bodyMotionOffset = Vector3.zero;
        bodyShakeOffset = Vector3.zero;
        ApplyBodyVisualOffset();

        if (worldRoot == null)
        {
            if (finishAutomatically)
            {
                FinishHitReaction();
            }

            yield break;
        }

        float normalizedDirection = recoilDirectionSign >= 0f ? 1f : -1f;
        float recoilStartX = worldRoot.position.x;
        float recoilTargetX = recoilStartX +
            normalizedDirection * hitRecoilDistance;
        float recoilDuration = Mathf.Max(0f, hitRecoilDuration);
        float shakeDuration = Mathf.Max(0f, hitShakeDuration);
        float holdDuration = finishAutomatically
            ? Mathf.Max(0f, hitPoseHoldDuration)
            : 0f;
        float recoilElapsed = 0f;
        float shakeElapsed = 0f;
        float holdElapsed = 0f;

        if (recoilDuration <= 0f)
        {
            SetWorldRootPositionX(worldRoot, recoilTargetX);
        }

        if (shakeDuration <= 0f)
        {
            bodyShakeOffset = Vector3.zero;
        }

        ApplyBodyVisualOffset();

        while (recoilElapsed < recoilDuration ||
            shakeElapsed < shakeDuration ||
            holdElapsed < holdDuration)
        {
            if (!isActiveAndEnabled)
            {
                FinishHitReaction();
                yield break;
            }

            if (presentationPaused)
            {
                yield return null;
                continue;
            }

            float deltaTime = Time.deltaTime;
            if (recoilElapsed < recoilDuration)
            {
                recoilElapsed = Mathf.Min(
                    recoilDuration,
                    recoilElapsed + deltaTime
                );
                float recoilT = BattlePresentationEasing.EaseOutQuad(
                    recoilElapsed / recoilDuration
                );
                SetWorldRootPositionX(
                    worldRoot,
                    Mathf.Lerp(recoilStartX, recoilTargetX, recoilT)
                );
            }
            else
            {
                SetWorldRootPositionX(worldRoot, recoilTargetX);
            }

            if (shakeElapsed < shakeDuration)
            {
                shakeElapsed = Mathf.Min(
                    shakeDuration,
                    shakeElapsed + deltaTime
                );
                float shakeT = shakeElapsed / shakeDuration;
                float currentAmplitude = Mathf.Max(0f, hitShakeAmplitude) *
                    (1f - BattlePresentationEasing.EaseOutQuad(shakeT));
                Vector2 randomDirection = UnityEngine.Random.insideUnitCircle;
                bodyShakeOffset = new Vector3(
                    randomDirection.x,
                    randomDirection.y,
                    0f
                ) * currentAmplitude;
            }
            else
            {
                bodyShakeOffset = Vector3.zero;
            }

            holdElapsed = Mathf.Min(holdDuration, holdElapsed + deltaTime);
            ApplyBodyVisualOffset();
            yield return null;
        }

        // 受击后退属于真实战斗空间结果；局部通道只保留短暂抖动。
        SetWorldRootPositionX(worldRoot, recoilTargetX);
        bodyMotionOffset = Vector3.zero;
        bodyShakeOffset = Vector3.zero;
        ApplyBodyVisualOffset();
        if (finishAutomatically)
        {
            FinishHitReaction();
        }
    }

    private static void SetWorldRootPositionX(Transform worldRoot, float x)
    {
        if (worldRoot == null)
        {
            return;
        }

        Vector3 position = worldRoot.position;
        position.x = x;
        worldRoot.position = position;
    }

    public void SpawnAfterimage()
    {
        SpawnAfterimageInternal(afterimageLifetime, afterimageStartAlpha);
    }

    private void SpawnAfterimageInternal(float lifetime, float startAlpha)
    {
        if (characterSprite == null || characterSprite.sprite == null)
        {
            return;
        }

        Transform sourceTransform = characterSprite.transform;
        GameObject afterimageRoot = new GameObject("BattleCharacterAfterimage");
        afterimageRoot.layer = characterSprite.gameObject.layer;
        afterimageRoot.transform.SetPositionAndRotation(
            sourceTransform.position,
            sourceTransform.rotation
        );
        // 残影不设置父节点，以生成瞬间的世界缩放固定在原地。
        afterimageRoot.transform.localScale = sourceTransform.lossyScale;

        SpriteRenderer afterimage = afterimageRoot.AddComponent<SpriteRenderer>();
        afterimage.sprite = characterSprite.sprite;
        afterimage.flipX = characterSprite.flipX;
        afterimage.flipY = characterSprite.flipY;
        afterimage.sortingLayerID = characterSprite.sortingLayerID;
        afterimage.sortingOrder = characterSprite.sortingOrder - 1;

        Color sourceColor = characterSprite.color;
        sourceColor.a *= Mathf.Clamp01(startAlpha);
        afterimage.color = sourceColor;

        if (lifetime <= 0f)
        {
            Destroy(afterimageRoot);
            return;
        }

        ActiveAfterimage entry = new ActiveAfterimage
        {
            root = afterimageRoot
        };
        activeAfterimages.Add(entry);
        entry.fadeCoroutine = StartCoroutine(
            FadeAfterimage(entry, afterimage, sourceColor, lifetime)
        );
    }

    public void ClearAfterimages()
    {
        for (int index = activeAfterimages.Count - 1; index >= 0; index--)
        {
            ActiveAfterimage entry = activeAfterimages[index];
            if (entry == null)
            {
                continue;
            }

            if (entry.fadeCoroutine != null)
            {
                StopCoroutine(entry.fadeCoroutine);
            }

            if (entry.root != null)
            {
                Destroy(entry.root);
            }
        }

        activeAfterimages.Clear();
    }

    private IEnumerator FadeAfterimage(
        ActiveAfterimage entry,
        SpriteRenderer afterimage,
        Color startColor,
        float lifetime
    )
    {
        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            if (entry.root == null || afterimage == null)
            {
                activeAfterimages.Remove(entry);
                yield break;
            }

            if (presentationPaused)
            {
                yield return null;
                continue;
            }

            float easedT = BattlePresentationEasing.EaseOutQuad(
                elapsed / lifetime
            );
            Color color = startColor;
            color.a = startColor.a + (0f - startColor.a) * easedT;
            afterimage.color = color;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (afterimage != null)
        {
            Color finalColor = startColor;
            finalColor.a = 0f;
            afterimage.color = finalColor;
        }

        activeAfterimages.Remove(entry);
        if (entry.root != null)
        {
            Destroy(entry.root);
        }
    }

    private static void InvokeImpactOnce(
        Action onImpact,
        ref bool impactTriggered
    )
    {
        if (impactTriggered)
        {
            return;
        }

        impactTriggered = true;
        onImpact?.Invoke();
    }

    private IEnumerator RunDodgeMotion(
        float normalizedDirection,
        float distance,
        float duration
    )
    {
        float segmentDuration = duration / 3f;
        Vector3 firstSide = Vector3.right * normalizedDirection * distance;
        Vector3 oppositeSide = -firstSide;

        // Dodge只占用局部视觉偏移，不移动角色WorldRoot或UI Anchor。
        yield return RunDodgeMotionSegment(Vector3.zero, firstSide, segmentDuration);
        yield return RunDodgeMotionSegment(firstSide, oppositeSide, segmentDuration);
        yield return RunDodgeMotionSegment(oppositeSide, Vector3.zero, segmentDuration);
        CompleteDodgeMotion();
    }

    private IEnumerator RunDodgeMotionSegment(
        Vector3 startOffset,
        Vector3 targetOffset,
        float duration
    )
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (presentationPaused)
            {
                yield return null;
                continue;
            }

            elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
            float easedT = BattlePresentationEasing.EaseOutQuad(
                elapsed / duration
            );
            dodgeMotionOffset = startOffset +
                (targetOffset - startOffset) * easedT;
            ApplyBodyVisualOffset();
            yield return null;
        }

        dodgeMotionOffset = targetOffset;
        ApplyBodyVisualOffset();
    }

    private void CompleteDodgeMotion()
    {
        dodgeMotionCoroutine = null;
        dodgeMotionOffset = Vector3.zero;
        ApplyBodyVisualOffset();

        Action callback = dodgeMotionFinishedCallback;
        dodgeMotionFinishedCallback = null;
        callback?.Invoke();
    }

    private void CancelDodgeMotion()
    {
        if (dodgeMotionCoroutine != null)
        {
            StopCoroutine(dodgeMotionCoroutine);
            dodgeMotionCoroutine = null;
        }

        dodgeMotionFinishedCallback = null;
    }

    private void CacheBodyVisualState()
    {
        if (bodyBaseStateCached || bodyVisualRoot == null)
        {
            return;
        }

        bodyBaseLocalPosition = bodyVisualRoot.localPosition;
        bodyBaseStateCached = true;
    }

    private void ApplyBodyVisualOffset()
    {
        if (!bodyBaseStateCached || bodyVisualRoot == null)
        {
            return;
        }

        bodyVisualRoot.localPosition = bodyBaseLocalPosition +
            bodyMotionOffset + bodyShakeOffset + dodgeMotionOffset;
    }

    private void CacheSlashEffectState()
    {
        if (!slashBackStateCached && slashBackEffect != null)
        {
            slashBackOriginalLocalPosition =
                slashBackEffect.transform.localPosition;
            slashBackOriginalColor = slashBackEffect.color;
            slashBackStateCached = true;
        }

        if (!slashFrontStateCached && slashFrontEffect != null)
        {
            slashFrontOriginalLocalPosition =
                slashFrontEffect.transform.localPosition;
            slashFrontOriginalColor = slashFrontEffect.color;
            slashFrontStateCached = true;
        }
    }

    private void CachePerfectGuardEffectState()
    {
        if (perfectGuardEffectStateCached || perfectGuardEffect == null)
        {
            return;
        }

        perfectGuardEffectOriginalColor = perfectGuardEffect.color;
        perfectGuardEffectStateCached = true;
    }

    private IEnumerator RunPerfectGuardEffectPhase(
        float duration,
        float startAlpha,
        float targetAlpha
    )
    {
        float safeDuration = Mathf.Max(0f, duration);
        if (safeDuration <= 0f)
        {
            SetPerfectGuardEffectAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            if (!isActiveAndEnabled || perfectGuardEffect == null)
            {
                ClearPerfectGuardEffect();
                yield break;
            }

            if (presentationPaused)
            {
                yield return null;
                continue;
            }

            elapsed = Mathf.Min(safeDuration, elapsed + Time.deltaTime);
            float easedT = BattlePresentationEasing.EaseOutQuad(
                elapsed / safeDuration
            );
            SetPerfectGuardEffectAlpha(
                startAlpha + (targetAlpha - startAlpha) * easedT
            );
            yield return null;
        }

        SetPerfectGuardEffectAlpha(targetAlpha);
    }

    private IEnumerator WaitForPerfectGuardEffect(float duration)
    {
        float safeDuration = Mathf.Max(0f, duration);
        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            if (!isActiveAndEnabled || perfectGuardEffect == null)
            {
                ClearPerfectGuardEffect();
                yield break;
            }

            if (presentationPaused)
            {
                yield return null;
                continue;
            }

            elapsed = Mathf.Min(safeDuration, elapsed + Time.deltaTime);
            yield return null;
        }
    }

    private void SetPerfectGuardEffectAlpha(float alpha)
    {
        if (!perfectGuardEffectStateCached || perfectGuardEffect == null)
        {
            return;
        }

        Color color = perfectGuardEffectOriginalColor;
        color.a *= Mathf.Clamp01(alpha);
        perfectGuardEffect.color = color;
    }

    private void ApplySlashEffectShake(float elapsed)
    {
        if (slashEffectShakeDuration <= 0f ||
            slashEffectShakeAmplitude <= 0f)
        {
            RestoreSlashEffectPositions();
            return;
        }

        float shakeT = Mathf.Clamp01(elapsed / slashEffectShakeDuration);
        float amplitude = slashEffectShakeAmplitude *
            (1f - BattlePresentationEasing.EaseOutQuad(shakeT));
        float phase = elapsed * 90f;
        Vector3 shakeOffset = new Vector3(
            Mathf.Sin(phase),
            Mathf.Cos(phase),
            0f
        ) * amplitude;

        if (slashBackStateCached && slashBackEffect != null)
        {
            slashBackEffect.transform.localPosition =
                slashBackOriginalLocalPosition + shakeOffset;
        }

        if (slashFrontStateCached && slashFrontEffect != null)
        {
            slashFrontEffect.transform.localPosition =
                slashFrontOriginalLocalPosition + shakeOffset;
        }
    }

    private void RestoreSlashEffectPositions()
    {
        if (slashBackStateCached && slashBackEffect != null)
        {
            slashBackEffect.transform.localPosition =
                slashBackOriginalLocalPosition;
        }

        if (slashFrontStateCached && slashFrontEffect != null)
        {
            slashFrontEffect.transform.localPosition =
                slashFrontOriginalLocalPosition;
        }
    }

    private void SetSlashEffectAlpha(float alpha)
    {
        float safeAlpha = Mathf.Clamp01(alpha);
        if (slashBackStateCached && slashBackEffect != null)
        {
            Color color = slashBackOriginalColor;
            color.a *= safeAlpha;
            slashBackEffect.color = color;
        }

        if (slashFrontStateCached && slashFrontEffect != null)
        {
            Color color = slashFrontOriginalColor;
            color.a *= safeAlpha;
            slashFrontEffect.color = color;
        }
    }

    private void SetPose(Sprite poseSprite)
    {
        if (characterSprite == null || poseSprite == null)
        {
            return;
        }

        characterSprite.sprite = poseSprite;
    }
}
