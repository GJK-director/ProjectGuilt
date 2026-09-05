using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 控制单个战斗角色当前显示的静态姿态，不负责动画流程或角色移动。
public sealed class BattleCharacterPresentationController : MonoBehaviour
{
    private static readonly int HitTintColorId =
        Shader.PropertyToID("_HitTintColor");
    private static readonly int HitTintStrengthId =
        Shader.PropertyToID("_HitTintStrength");

    private enum HitBodyReactionMode
    {
        LegacyRandom,
        DirectionalDamped
    }

    private sealed class ActiveAfterimage
    {
        public GameObject root;
        public Coroutine fadeCoroutine;
    }

    [SerializeField] private string presentationKey;
    [SerializeField] private bool sourceFacesRight = true;
    [SerializeField] private Vector3 cameraFramingOffset = Vector3.zero;
    [SerializeField] private SpriteRenderer characterSprite;
    [SerializeField] private Transform bodyVisualRoot;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite sprintSprite;
    [SerializeField] private Sprite slashSprite;
    [SerializeField] private Sprite aimSprite;
    [SerializeField] private Sprite shootSprite;
    [SerializeField] private Sprite closeRangeShootSprite;
    [SerializeField] private Sprite hitSprite;
    [SerializeField] private Sprite guardSprite;
    [SerializeField] private Sprite dodgeSprite;
    [SerializeField] private Transform muzzleFlashAnchor;
    [SerializeField] private SpriteRenderer muzzleFlashEffect;
    [SerializeField] private Transform closeRangeMuzzleFlashAnchor;
    [SerializeField] private SpriteRenderer closeRangeMuzzleFlashEffect;
    [SerializeField] private float muzzleFlashDuration = 0.08f;
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
    [SerializeField] private float hitRecoilDuration = 0.12f;
    [SerializeField] private float hitShakeAmplitude = 0.04f;
    [SerializeField] private float hitShakeDuration = 0.12f;
    [SerializeField] private float hitPoseHoldDuration = 0.16f;
    [SerializeField] private float parryRecoilDistance = 0.20f;
    [SerializeField] private float parryRecoilDuration = 0.12f;
    [SerializeField] private float parryShakeAmplitude = 0.03f;
    [SerializeField] private float parryShakeDuration = 0.10f;
    [SerializeField] private float parryHoldDuration = 0.10f;
    [SerializeField] private float guardRecoilDistance = 0.10f;
    [SerializeField] private float guardRecoilDuration = 0.12f;
    [SerializeField] private float guardShakeAmplitude = 0.025f;
    [SerializeField] private float guardShakeDuration = 0.10f;
    [SerializeField] private float guardHoldDuration = 0.10f;
    [SerializeField] private float dodgeHorizontalDistance = 0.20f;
    [SerializeField] private float dodgeDuration = 0.30f;

    private Vector3 bodyBaseLocalPosition;
    private Quaternion bodyBaseLocalRotation;
    private Vector3 bodyMotionOffset;
    private Vector3 bodyShakeOffset;
    private Vector3 dodgeMotionOffset;
    private float bodyHitRotationZ;
    private MaterialPropertyBlock characterPropertyBlock;
    private Coroutine dodgeMotionCoroutine;
    private Action dodgeMotionFinishedCallback;
    private Coroutine muzzleFlashCoroutine;
    private Action muzzleFlashFinishedCallback;
    private int muzzleFlashPlaybackVersion;
    private bool bodyBaseStateCached;
    private Vector3 slashBackOriginalLocalPosition;
    private Vector3 slashFrontOriginalLocalPosition;
    private Color slashBackOriginalColor;
    private Color slashFrontOriginalColor;
    private bool slashBackStateCached;
    private bool slashFrontStateCached;
    private bool presentationPaused;
    private readonly List<ActiveAfterimage> activeAfterimages =
        new List<ActiveAfterimage>();

    public string PresentationKey => presentationKey ?? string.Empty;
    public bool SourceFacesRight => sourceFacesRight;
    public SpriteRenderer CharacterSpriteRenderer => characterSprite;
    public bool IsPresentationPaused => presentationPaused;
    public float DodgeMotionDuration => Mathf.Max(0f, dodgeDuration);

    public BattleCharacterPresentationBindingSnapshot GetBindingSnapshot()
    {
        return new BattleCharacterPresentationBindingSnapshot
        {
            HasCharacterSpriteRenderer = characterSprite != null,
            HasBodyVisualRoot = bodyVisualRoot != null,
            HasIdleSprite = idleSprite != null,
            HasSprintSprite = sprintSprite != null,
            HasSlashSprite = slashSprite != null,
            HasAimSprite = aimSprite != null,
            HasShootSprite = shootSprite != null,
            HasCloseRangeShootSprite = closeRangeShootSprite != null,
            HasHitSprite = hitSprite != null,
            HasGuardSprite = guardSprite != null,
            HasDodgeSprite = dodgeSprite != null,
            HasLongRangeMuzzleFlashAnchor = muzzleFlashAnchor != null,
            HasLongRangeMuzzleFlashEffect = muzzleFlashEffect != null,
            HasCloseRangeMuzzleFlashAnchor =
                closeRangeMuzzleFlashAnchor != null,
            HasCloseRangeMuzzleFlashEffect =
                closeRangeMuzzleFlashEffect != null,
            HasSlashBackEffect = slashBackEffect != null,
            HasSlashFrontEffect = slashFrontEffect != null
        };
    }

    public bool TryGetCameraFramingWorldPosition(out Vector3 worldPosition)
    {
        // Offset是显式world-space构图修正，不受Sprite、Pose或Flip影响。
        worldPosition = transform.position + cameraFramingOffset;
        return true;
    }

    public bool TryGetStableVisualRootWorldPosition(
        out Vector3 worldPosition
    )
    {
        CacheBodyVisualState();
        if (!bodyBaseStateCached || bodyVisualRoot == null)
        {
            worldPosition = transform.position;
            return false;
        }

        Transform parent = bodyVisualRoot.parent;
        worldPosition = parent != null
            ? parent.TransformPoint(bodyBaseLocalPosition)
            : bodyBaseLocalPosition;
        return true;
    }

    void Awake()
    {
        CacheBodyVisualState();
        ApplyBodyVisualOffset();
        ApplyBodyVisualRotation();
        CacheSlashEffectState();
        SetIdle();
        ClearMuzzleFlashVisual();
        ClearSlashEffect();
    }

    void OnDisable()
    {
        presentationPaused = false;
        ClearBodyVisualOffsets();
        ClearAfterimages();
        CancelMuzzleFlash();
        ClearSlashEffect();
        ClearHitTint();
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

    public void SetAim()
    {
        SetPose(aimSprite);
    }

    public void SetShoot()
    {
        SetPose(shootSprite);
    }

    public void SetCloseRangeShoot()
    {
        SetPose(closeRangeShootSprite);
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
        CancelMuzzleFlash();
        ClearSlashEffect();
        ClearHitTint();
        SetIdle();
    }

    public void PlayMuzzleFlash(Action finishedCallback = null)
    {
        PlayMuzzleFlashInternal(
            muzzleFlashAnchor,
            muzzleFlashEffect,
            finishedCallback
        );
    }

    public void PlayCloseRangeMuzzleFlash(Action finishedCallback = null)
    {
        PlayMuzzleFlashInternal(
            closeRangeMuzzleFlashAnchor,
            closeRangeMuzzleFlashEffect,
            finishedCallback
        );
    }

    private void PlayMuzzleFlashInternal(
        Transform anchor,
        SpriteRenderer effect,
        Action finishedCallback
    )
    {
        CancelMuzzleFlash();
        int playbackVersion = ++muzzleFlashPlaybackVersion;
        muzzleFlashFinishedCallback = finishedCallback;

        if (!isActiveAndEnabled || anchor == null || effect == null)
        {
            CompleteMuzzleFlash(playbackVersion);
            return;
        }

        // 枪口位置由Prefab/Inspector明确提供，不在运行时猜测武器节点。
        effect.transform.SetPositionAndRotation(
            anchor.position,
            anchor.rotation
        );
        effect.enabled = true;

        float safeDuration = Mathf.Max(0f, muzzleFlashDuration);
        if (safeDuration <= 0f)
        {
            CompleteMuzzleFlash(playbackVersion);
            return;
        }

        muzzleFlashCoroutine = StartCoroutine(
            RunMuzzleFlash(playbackVersion, safeDuration)
        );
    }

    public void CancelMuzzleFlash()
    {
        muzzleFlashPlaybackVersion++;
        if (muzzleFlashCoroutine != null)
        {
            StopCoroutine(muzzleFlashCoroutine);
            muzzleFlashCoroutine = null;
        }

        muzzleFlashFinishedCallback = null;
        ClearMuzzleFlashVisual();
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
        bodyHitRotationZ = 0f;
        ApplyBodyVisualOffset();
        ApplyBodyVisualRotation();
        ClearHitTint();
    }

    public void FinishGuardPresentation()
    {
        // Guard正常结束后关闭瞬时反馈，但保留最后的Guard Pose。
        presentationPaused = false;
        bodyMotionOffset = Vector3.zero;
        bodyShakeOffset = Vector3.zero;
        ApplyBodyVisualOffset();
    }

    public void ClearBodyVisualOffsets()
    {
        CancelDodgeMotion();
        bodyMotionOffset = Vector3.zero;
        bodyShakeOffset = Vector3.zero;
        dodgeMotionOffset = Vector3.zero;
        bodyHitRotationZ = 0f;
        ApplyBodyVisualOffset();
        ApplyBodyVisualRotation();
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
            hitRecoilDistance,
            HitBodyReactionMode.LegacyRandom,
            0f,
            0f,
            0f,
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
            hitRecoilDistance,
            HitBodyReactionMode.LegacyRandom,
            0f,
            0f,
            0f,
            false
        );
    }

    public IEnumerator PlaySustainedHitReaction(
        Transform worldRoot,
        float recoilDirectionSign,
        float worldKnockbackDistance
    )
    {
        yield return PlayHitReactionInternal(
            worldRoot,
            recoilDirectionSign,
            Mathf.Max(0f, worldKnockbackDistance),
            HitBodyReactionMode.LegacyRandom,
            0f,
            0f,
            0f,
            false
        );
    }

    public IEnumerator PlaySustainedHitReaction(
        Transform worldRoot,
        float recoilDirectionSign,
        BattleHitPresentationProfile profile
    )
    {
        yield return PlaySustainedHitReaction(
            worldRoot,
            recoilDirectionSign,
            profile,
            profile != null ? profile.FollowKnockbackDistance : 0f
        );
    }

    public IEnumerator PlaySustainedHitReaction(
        Transform worldRoot,
        float recoilDirectionSign,
        BattleHitPresentationProfile profile,
        float followKnockbackDistance
    )
    {
        yield return PlaySustainedHitReaction(
            worldRoot, recoilDirectionSign, profile, followKnockbackDistance, true
        );
    }

    public IEnumerator PlaySustainedHitReaction(
        Transform worldRoot,
        float recoilDirectionSign,
        BattleHitPresentationProfile profile,
        float followKnockbackDistance,
        bool applyHitTint
    )
    {
        SetHit();
        bodyMotionOffset = Vector3.zero;
        bodyShakeOffset = Vector3.zero;
        bodyHitRotationZ = 0f;
        ApplyBodyVisualOffset();
        ApplyBodyVisualRotation();

        if (worldRoot == null || profile == null)
        {
            FinishHitReaction();
            yield break;
        }

        float direction = recoilDirectionSign >= 0f ? 1f : -1f;
        float startX = worldRoot.position.x;
        float burstTargetX = startX + direction * profile.ImpactBurstDistance;
        float finalTargetX = burstTargetX + direction *
            Mathf.Max(0f, followKnockbackDistance);
        float impactBurstDuration = profile.ImpactBurstDuration;
        float followKnockbackDuration = profile.FollowKnockbackDuration;
        float activeHitDuration = impactBurstDuration + followKnockbackDuration;

        bodyHitRotationZ = -direction * profile.HitTiltAngle;
        ApplyBodyVisualRotation();
        ApplyNormalHitBodyVisual(profile, 0f, activeHitDuration);
        if (applyHitTint)
        {
            SetHitTint(profile.HitTintColor, profile.HitTintStrength);
        }
        else
        {
            ClearHitTint();
        }

        yield return MoveHitWorldRootX(
            worldRoot,
            startX,
            burstTargetX,
            impactBurstDuration,
            false,
            progress => ApplyNormalHitBodyVisual(
                profile,
                progress * impactBurstDuration,
                activeHitDuration
            )
        );
        yield return MoveHitWorldRootX(
            worldRoot,
            burstTargetX,
            finalTargetX,
            followKnockbackDuration,
            true,
            progress => ApplyNormalHitBodyVisual(
                profile,
                impactBurstDuration + progress * followKnockbackDuration,
                activeHitDuration
            )
        );

        SetWorldRootPositionX(worldRoot, finalTargetX);
        bodyShakeOffset = Vector3.zero;
        bodyHitRotationZ = 0f;
        ApplyBodyVisualOffset();
        ApplyBodyVisualRotation();
        ClearHitTint();
        float recoveryElapsed = 0f;
        float recoveryDuration = profile.RecoveryDuration;
        while (recoveryElapsed < recoveryDuration)
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

            recoveryElapsed = Mathf.Min(
                recoveryDuration,
                recoveryElapsed + Time.deltaTime
            );
            yield return null;
        }

        SetWorldRootPositionX(worldRoot, finalTargetX);
        FinishHitReaction();
    }

    public IEnumerator PlaySustainedDirectionalHitReaction(
        Transform worldRoot,
        float recoilDirectionSign,
        float worldKnockbackDistance,
        float bodyReactionAmplitude,
        float bodyReactionDuration,
        float bodyReactionOscillations
    )
    {
        yield return PlayHitReactionInternal(
            worldRoot,
            recoilDirectionSign,
            Mathf.Max(0f, worldKnockbackDistance),
            HitBodyReactionMode.DirectionalDamped,
            Mathf.Max(0f, bodyReactionAmplitude),
            Mathf.Max(0f, bodyReactionDuration),
            Mathf.Max(0f, bodyReactionOscillations),
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

    public IEnumerator PlayGuardShake()
    {
        SetGuard();
        bodyMotionOffset = Vector3.zero;
        bodyShakeOffset = Vector3.zero;
        ApplyBodyVisualOffset();

        float shakeDuration = Mathf.Max(0f, guardShakeDuration);
        float shakeElapsed = 0f;
        while (shakeElapsed < shakeDuration)
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

            shakeElapsed = Mathf.Min(
                shakeDuration,
                shakeElapsed + Time.deltaTime
            );
            float shakeT = shakeElapsed / shakeDuration;
            float currentAmplitude = Mathf.Max(0f, guardShakeAmplitude) *
                (1f - BattlePresentationEasing.EaseOutQuad(shakeT));
            Vector2 randomDirection = UnityEngine.Random.insideUnitCircle;
            bodyShakeOffset = new Vector3(
                randomDirection.x,
                randomDirection.y,
                0f
            ) * currentAmplitude;
            ApplyBodyVisualOffset();
            yield return null;
        }

        bodyMotionOffset = Vector3.zero;
        bodyShakeOffset = Vector3.zero;
        ApplyBodyVisualOffset();
    }

    private IEnumerator PlayHitReactionInternal(
        Transform worldRoot,
        float recoilDirectionSign,
        float worldKnockbackDistance,
        HitBodyReactionMode bodyReactionMode,
        float bodyReactionAmplitude,
        float bodyReactionDuration,
        float bodyReactionOscillations,
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
            normalizedDirection * worldKnockbackDistance;
        float recoilDuration = Mathf.Max(0f, hitRecoilDuration);
        float shakeDuration = bodyReactionMode ==
                HitBodyReactionMode.DirectionalDamped
            ? Mathf.Max(0f, bodyReactionDuration)
            : Mathf.Max(0f, hitShakeDuration);
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
                if (bodyReactionMode == HitBodyReactionMode.DirectionalDamped)
                {
                    float phase = shakeT * Mathf.PI * 2f *
                        Mathf.Max(0f, bodyReactionOscillations);
                    float envelope = 1f - BattlePresentationEasing.EaseOutQuad(
                        shakeT
                    );
                    float offsetX = normalizedDirection *
                        Mathf.Max(0f, bodyReactionAmplitude) * envelope *
                        Mathf.Cos(phase);
                    bodyShakeOffset = new Vector3(offsetX, 0f, 0f);
                }
                else
                {
                    float currentAmplitude = Mathf.Max(0f, hitShakeAmplitude) *
                        (1f - BattlePresentationEasing.EaseOutQuad(shakeT));
                    Vector2 randomDirection = UnityEngine.Random.insideUnitCircle;
                    bodyShakeOffset = new Vector3(
                        randomDirection.x,
                        randomDirection.y,
                        0f
                    ) * currentAmplitude;
                }
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

    private IEnumerator MoveHitWorldRootX(
        Transform worldRoot,
        float startX,
        float targetX,
        float duration,
        bool useEaseOutQuad,
        Action<float> progressCallback
    )
    {
        float safeDuration = Mathf.Max(0f, duration);
        if (safeDuration <= 0f)
        {
            SetWorldRootPositionX(worldRoot, targetX);
            progressCallback?.Invoke(1f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            if (!isActiveAndEnabled)
            {
                yield break;
            }

            if (presentationPaused)
            {
                yield return null;
                continue;
            }

            elapsed = Mathf.Min(safeDuration, elapsed + Time.deltaTime);
            float progress = elapsed / safeDuration;
            float easedProgress = useEaseOutQuad
                ? BattlePresentationEasing.EaseOutQuad(progress)
                : progress;
            SetWorldRootPositionX(
                worldRoot,
                Mathf.Lerp(startX, targetX, easedProgress)
            );
            progressCallback?.Invoke(progress);
            yield return null;
        }

        SetWorldRootPositionX(worldRoot, targetX);
        progressCallback?.Invoke(1f);
    }

    private void ApplyNormalHitBodyVisual(
        BattleHitPresentationProfile profile,
        float activeHitElapsed,
        float activeHitDuration
    )
    {
        if (profile == null || activeHitDuration <= 0f)
        {
            bodyShakeOffset = Vector3.zero;
            ApplyBodyVisualOffset();
            return;
        }

        float shakeT = Mathf.Clamp01(activeHitElapsed / activeHitDuration);
        float phase = shakeT * Mathf.PI * 2f *
            profile.VerticalShakeOscillations;
        float envelope = 1f - BattlePresentationEasing.EaseOutQuad(shakeT);
        float offsetY = profile.VerticalShakeAmplitude * envelope *
            Mathf.Cos(phase);
        bodyShakeOffset = new Vector3(0f, offsetY, 0f);
        ApplyBodyVisualOffset();
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

    private IEnumerator RunMuzzleFlash(int playbackVersion, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (!isActiveAndEnabled ||
                playbackVersion != muzzleFlashPlaybackVersion)
            {
                yield break;
            }

            if (presentationPaused)
            {
                yield return null;
                continue;
            }

            elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
            yield return null;
        }

        CompleteMuzzleFlash(playbackVersion);
    }

    private void CompleteMuzzleFlash(int playbackVersion)
    {
        if (playbackVersion != muzzleFlashPlaybackVersion)
        {
            return;
        }

        muzzleFlashCoroutine = null;
        ClearMuzzleFlashVisual();

        Action callback = muzzleFlashFinishedCallback;
        muzzleFlashFinishedCallback = null;
        callback?.Invoke();
    }

    private void ClearMuzzleFlashVisual()
    {
        if (muzzleFlashEffect != null)
        {
            muzzleFlashEffect.enabled = false;
        }

        if (closeRangeMuzzleFlashEffect != null)
        {
            closeRangeMuzzleFlashEffect.enabled = false;
        }
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
        bodyBaseLocalRotation = bodyVisualRoot.localRotation;
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

    private void ApplyBodyVisualRotation()
    {
        if (!bodyBaseStateCached || bodyVisualRoot == null)
        {
            return;
        }

        bodyVisualRoot.localRotation = bodyBaseLocalRotation *
            Quaternion.Euler(0f, 0f, bodyHitRotationZ);
    }

    private void SetHitTint(Color color, float strength)
    {
        if (characterSprite == null)
        {
            return;
        }

        if (characterPropertyBlock == null)
        {
            characterPropertyBlock = new MaterialPropertyBlock();
        }

        characterSprite.GetPropertyBlock(characterPropertyBlock);
        characterPropertyBlock.SetColor(HitTintColorId, color);
        characterPropertyBlock.SetFloat(
            HitTintStrengthId,
            Mathf.Clamp01(strength)
        );
        characterSprite.SetPropertyBlock(characterPropertyBlock);
    }

    private void ClearHitTint()
    {
        if (characterSprite == null)
        {
            return;
        }

        if (characterPropertyBlock == null)
        {
            characterPropertyBlock = new MaterialPropertyBlock();
        }

        characterSprite.GetPropertyBlock(characterPropertyBlock);
        characterPropertyBlock.SetFloat(HitTintStrengthId, 0f);
        characterSprite.SetPropertyBlock(characterPropertyBlock);
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
