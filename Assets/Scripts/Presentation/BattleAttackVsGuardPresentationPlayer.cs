using System;
using System.Collections;
using UnityEngine;

public enum BattleGuardPresentationResult
{
    FullBlock,
    ReducedDamage
}

// 只协调AttackVsGuard视觉流程，不读取战斗规则、输入或伤害数据。
public sealed class BattleAttackVsGuardPresentationPlayer : MonoBehaviour
{
    [SerializeField]
    private BattleAttackVsGuardPresentationProfile presentationProfile;

    [SerializeField, Min(0.01f), Tooltip(
        "普通近战Guard接敌移动速度倍率；1表示使用基础SprintDuration。"
    )]
    private float guardApproachMoveSpeedMultiplier = 1.00f;

    public bool IsRunning { get; private set; }
    public bool IsFinished { get; private set; }
    public BattleHitPresentationProfile MeleeGuardReactionProfile =>
        presentationProfile != null ? presentationProfile.MeleeGuardReactionProfile : null;
    public float MeleeGuardReactionActiveDuration => MeleeGuardReactionProfile != null
        ? MeleeGuardReactionProfile.ImpactBurstDuration +
            MeleeGuardReactionProfile.FollowKnockbackDuration
        : 0f;
    public float GuardApproachSeparation => presentationProfile != null
        ? presentationProfile.GuardApproachSeparation
        : 0f;
    public float EngagementFinalMoveSpeedScale => presentationProfile != null
        ? presentationProfile.EngagementFinalMoveSpeedScale
        : 0.50f;

    private BattleCharacterPresentationController attacker;
    private BattleCharacterPresentationController defender;
    private BattleGuardPresentationResult presentationResult;
    private float attackDirectionSign = 1f;
    private Action onVisualImpact;
    private Action onReactionStarted;
    private Transform attackerWorldRoot;
    private Transform defenderWorldRoot;
    private Coroutine meleeGuardReactionCoroutine;
    private bool meleeGuardReactionRunning;
    private Action onFinished;
    private Coroutine playbackCoroutine;
    private Coroutine parryRecoilCoroutine;
    private bool perfectGuardFxRunning;
    private Coroutine guardRecoilCoroutine;
    private Coroutine dualHitStopCoroutine;
    private int playbackVersion;
    private bool visualImpactReached;
    private BattlePresentationAttackDeliveryKind attackerDelivery;

    void OnDisable()
    {
        CancelAndReset();
    }

    public bool TryPlayClashReadyApproach(
        BattleCharacterPresentationController defenseSide,
        Transform defenseWorldRoot,
        BattleCharacterPresentationController attackSide,
        Transform attackWorldRoot,
        BattleClashEngagementResult engagementResult,
        Action completion
    )
    {
        return TryPlayClashReadyApproach(
            defenseSide,
            defenseWorldRoot,
            attackSide,
            attackWorldRoot,
            engagementResult,
            false,
            completion
        );
    }

    public bool TryPlayClashReadyApproach(
        BattleCharacterPresentationController defenseSide,
        Transform defenseWorldRoot,
        BattleCharacterPresentationController attackSide,
        Transform attackWorldRoot,
        BattleClashEngagementResult engagementResult,
        bool useRealApproach,
        Action completion
    )
    {
        return TryPlayClashReadyApproach(
            defenseSide,
            defenseWorldRoot,
            attackSide,
            attackWorldRoot,
            engagementResult,
            useRealApproach,
            0f,
            completion
        );
    }

    public bool TryPlayClashReadyApproach(
        BattleCharacterPresentationController defenseSide,
        Transform defenseWorldRoot,
        BattleCharacterPresentationController attackSide,
        Transform attackWorldRoot,
        BattleClashEngagementResult engagementResult,
        bool useRealApproach,
        float engagementTriggerSeparation,
        Action completion
    )
    {
        return TryPlaySingleActorClashReadyApproach(
            defenseSide,
            defenseWorldRoot,
            attackSide,
            attackWorldRoot,
            engagementResult,
            useRealApproach,
            presentationProfile != null
                ? presentationProfile.GuardApproachSeparation
                : 0f,
            engagementTriggerSeparation,
            false,
            completion
        );
    }

    public bool TryPlayCloseRangeClashReadyApproach(
        BattleCharacterPresentationController defenseSide,
        Transform defenseWorldRoot,
        BattleCharacterPresentationController attackSide,
        Transform attackWorldRoot,
        BattleClashEngagementResult engagementResult,
        float engagementTriggerSeparation,
        Action completion
    )
    {
        return TryPlaySingleActorClashReadyApproach(
            defenseSide,
            defenseWorldRoot,
            attackSide,
            attackWorldRoot,
            engagementResult,
            true,
            engagementResult != null ? engagementResult.FinalGap : 0f,
            engagementTriggerSeparation,
            true,
            completion
        );
    }

    private bool TryPlaySingleActorClashReadyApproach(
        BattleCharacterPresentationController defenseSide,
        Transform defenseWorldRoot,
        BattleCharacterPresentationController attackSide,
        Transform attackWorldRoot,
        BattleClashEngagementResult engagementResult,
        bool useRealApproach,
        float finalGap,
        float engagementTriggerSeparation,
        bool setSprintWhenAlreadyReady,
        Action completion
    )
    {
        if (IsRunning || !isActiveAndEnabled || defenseSide == null ||
            attackSide == null || defenseWorldRoot == null ||
            attackWorldRoot == null || engagementResult == null ||
            !ValidatePresentationProfile())
        {
            return false;
        }

        if (!useRealApproach)
        {
            // 兼容旧调用：只进入Sprint等待Pose，不改变WorldRoot。
            attackSide.SetSprint();
            IsFinished = true;
            completion?.Invoke();
            return true;
        }

        float currentSeparation = Mathf.Abs(
            attackWorldRoot.position.x - defenseWorldRoot.position.x
        );
        float safeFinalGap = Mathf.Max(0f, finalGap);
        if (currentSeparation <= safeFinalGap + 0.0001f)
        {
            if (setSprintWhenAlreadyReady)
            {
                attackSide.SetSprint();
            }

            IsFinished = true;
            completion?.Invoke();
            return true;
        }

        playbackVersion++;
        attacker = attackSide;
        defender = defenseSide;
        onFinished = completion;
        IsRunning = true;
        IsFinished = false;
        playbackCoroutine = StartCoroutine(
            RunClashReadyApproach(
                playbackVersion,
                attackSide,
                attackWorldRoot,
                defenseSide,
                defenseWorldRoot,
                safeFinalGap,
                engagementTriggerSeparation
            )
        );
        return true;
    }

    private IEnumerator RunClashReadyApproach(
        int version,
        BattleCharacterPresentationController attackSide,
        Transform attackWorldRoot,
        BattleCharacterPresentationController defenseSide,
        Transform defenseWorldRoot,
        float finalGap,
        float engagementTriggerSeparation
    )
    {
        float moveSpeedMultiplier = Mathf.Max(
            0.01f,
            guardApproachMoveSpeedMultiplier
        );
        float approachDuration =
            presentationProfile.SprintDuration / moveSpeedMultiplier;
        yield return BattleClashReadyApproachMotion.PlaySingleActorApproach(
            attackSide,
            attackWorldRoot,
            defenseSide,
            defenseWorldRoot,
            finalGap,
            approachDuration,
            presentationProfile.AfterimageSpawnInterval,
            engagementTriggerSeparation,
            presentationProfile.EngagementFinalMoveSpeedScale,
            () => IsCurrentPlayback(version)
        );

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        Action callback = onFinished;
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = true;
        callback?.Invoke();
    }

    public bool TryPlayGuardImpact(
        BattleCharacterPresentationController attackActor,
        BattleCharacterPresentationController guardActor,
        float directionSign,
        BattleGuardPresentationResult result,
        Action onImpactReached,
        Action completion
    )
    {
        return TryPlayGuardImpact(
            attackActor,
            guardActor,
            directionSign,
            result,
            BattlePresentationAttackDeliveryKind.Melee,
            onImpactReached,
            completion
        );
    }

    public bool TryPlayGuardImpact(
        BattleCharacterPresentationController attackActor,
        BattleCharacterPresentationController guardActor,
        float directionSign,
        BattleGuardPresentationResult result,
        bool useCloseRangeShoot,
        Action onImpactReached,
        Action completion
    )
    {
        return TryPlayGuardImpact(
            attackActor,
            guardActor,
            directionSign,
            result,
            useCloseRangeShoot
                ? BattlePresentationAttackDeliveryKind.CloseRangeShoot
                : BattlePresentationAttackDeliveryKind.Melee,
            onImpactReached,
            completion
        );
    }

    public bool TryPlayGuardImpact(
        BattleCharacterPresentationController attackActor,
        BattleCharacterPresentationController guardActor,
        float directionSign,
        BattleGuardPresentationResult result,
        BattlePresentationAttackDeliveryKind attackDelivery,
        Action onImpactReached,
        Action completion
    )
    {
        return TryPlayGuardImpact(
            attackActor, guardActor, directionSign, result, attackDelivery,
            onImpactReached, completion, null, null, null
        );
    }

    public bool TryPlayGuardImpact(
        BattleCharacterPresentationController attackActor,
        BattleCharacterPresentationController guardActor,
        float directionSign,
        BattleGuardPresentationResult result,
        BattlePresentationAttackDeliveryKind attackDelivery,
        Action onImpactReached,
        Action completion,
        Transform attackWorldRoot,
        Transform guardWorldRoot,
        Action reactionStarted
    )
    {
        if (IsRunning || !isActiveAndEnabled || attackActor == null ||
            guardActor == null || !ValidatePresentationProfile() ||
            !IsSupportedAttackDelivery(attackDelivery))
        {
            return false;
        }

        playbackVersion++;
        attacker = attackActor;
        defender = guardActor;
        presentationResult = result;
        attackDirectionSign = directionSign >= 0f ? 1f : -1f;
        attackerDelivery = attackDelivery;
        attackerWorldRoot = attackWorldRoot;
        defenderWorldRoot = guardWorldRoot;
        onReactionStarted = reactionStarted;
        onVisualImpact = onImpactReached;
        onFinished = completion;
        visualImpactReached = false;
        IsRunning = true;
        IsFinished = false;
        playbackCoroutine = StartCoroutine(
            RunGuardImpact(playbackVersion)
        );
        return true;
    }

    public void CancelAndReset()
    {
        playbackVersion++;
        StopOwnedCoroutines();
        ResetGuardActorsToStableIdle();
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = false;
    }

    private IEnumerator RunGuardImpact(int version)
    {
        PrepareGuardActors();
        defender.SetGuard();

        if (UsesCloseRangeShootVisual(attackerDelivery))
        {
            attacker.SetCloseRangeShoot();
            bool muzzleFlashFinished = false;
            attacker.PlayCloseRangeMuzzleFlash(
                () => muzzleFlashFinished = true
            );

            // 枪火出现即沿用原Guard视觉接触点，真实结算仍由Runner提交。
            ReachVisualImpact(version);
            while (!muzzleFlashFinished && IsCurrentPlayback(version))
            {
                yield return null;
            }
        }
        else if (UsesLongRangeShootVisual(attackerDelivery))
        {
            attacker.SetShoot();
            bool muzzleFlashFinished = false;
            attacker.PlayMuzzleFlash(() => muzzleFlashFinished = true);

            // 远程枪火出现即为Guard视觉接触点，规则结算仍由Runner提交。
            ReachVisualImpact(version);
            while (!muzzleFlashFinished && IsCurrentPlayback(version))
            {
                yield return null;
            }
        }
        else
        {
            attacker.SetSlash();
            bool playAttackEffect = presentationResult ==
                BattleGuardPresentationResult.ReducedDamage;
            yield return attacker.PlaySlashPresentation(
                attackDirectionSign,
                () => ReachVisualImpact(version),
                playAttackEffect
            );
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        if (!visualImpactReached)
        {
            ReachVisualImpact(version);
        }

        while (IsCurrentPlayback(version) && HasActiveTail())
        {
            yield return null;
        }

        if (IsCurrentPlayback(version))
        {
            FinishPlayback(version);
        }
    }

    private void ReachVisualImpact(int version)
    {
        if (!IsCurrentPlayback(version) || visualImpactReached)
        {
            return;
        }

        visualImpactReached = true;
        defender.SetGuard();
        if (presentationResult == BattleGuardPresentationResult.FullBlock)
        {
            perfectGuardFxRunning = true;
            if (!BattlePerfectGuardFxPlayer.TrySpawn(
                presentationProfile,
                defender,
                attackDirectionSign,
                () =>
                {
                    if (this != null && IsCurrentPlayback(version))
                    {
                        perfectGuardFxRunning = false;
                    }
                },
                () => this != null && isActiveAndEnabled && IsCurrentPlayback(version)
            ))
            {
                perfectGuardFxRunning = false;
            }
        }

        float hitStopDuration = presentationProfile.HitStopDuration;
        if (hitStopDuration > 0f)
        {
            SetGuardActorsPaused(true);
            dualHitStopCoroutine = StartCoroutine(
                RunDualHitStop(version, hitStopDuration)
            );
        }

        // 命中边界由当前攻击表现触发，并且每次播放最多通知一次宿主。
        meleeGuardReactionRunning = UsesMeleeVisual(attackerDelivery) ||
            presentationResult == BattleGuardPresentationResult.ReducedDamage;
        Action callback = onVisualImpact;
        onVisualImpact = null;
        callback?.Invoke();
        if (IsCurrentPlayback(version) && meleeGuardReactionRunning)
        {
            Coroutine reaction = StartCoroutine(RunMeleeGuardReaction(version));
            meleeGuardReactionCoroutine = meleeGuardReactionRunning ? reaction : null;
        }
    }

    private IEnumerator RunMeleeGuardReaction(int version)
    {
        while (IsCurrentPlayback(version) && dualHitStopCoroutine != null)
        {
            yield return null;
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        BattleHitPresentationProfile profile = MeleeGuardReactionProfile;
        if (profile == null || attackerWorldRoot == null || defenderWorldRoot == null)
        {
            Debug.LogError(
                "[AttackVsGuardPresentationPlayer] Melee Guard Reaction skipped: " +
                "bind MeleeGuardReactionProfile and provide both WorldRoots.", this
            );
        }
        else
        {
            bool fullBlock = presentationResult == BattleGuardPresentationResult.FullBlock;
            Action callback = onReactionStarted;
            onReactionStarted = null;
            callback?.Invoke();
            if (!IsCurrentPlayback(version))
            {
                yield break;
            }
            yield return (fullBlock ? attacker : defender).PlaySustainedHitReaction(
                fullBlock ? attackerWorldRoot : defenderWorldRoot,
                fullBlock ? -attackDirectionSign : attackDirectionSign,
                profile,
                profile.FollowKnockbackDistance,
                !fullBlock
            );
        }

        if (IsCurrentPlayback(version))
        {
            meleeGuardReactionRunning = false;
            meleeGuardReactionCoroutine = null;
        }
    }

    private IEnumerator RunParryRecoil(int version)
    {
        if (attacker != null)
        {
            yield return attacker.PlayParryRecoil(attackDirectionSign);
        }

        if (IsCurrentPlayback(version))
        {
            parryRecoilCoroutine = null;
        }
    }

    private IEnumerator RunGuardRecoil(int version)
    {
        if (defender != null)
        {
            yield return defender.PlayGuardRecoil(attackDirectionSign);
        }

        if (IsCurrentPlayback(version))
        {
            guardRecoilCoroutine = null;
        }
    }

    private IEnumerator RunReducedGuardShake(int version)
    {
        if (defender != null)
        {
            // 普通近战的不完美防御只震动视觉，不增加任何后退位移。
            yield return defender.PlayGuardShake();
        }

        if (IsCurrentPlayback(version))
        {
            guardRecoilCoroutine = null;
        }
    }

    private IEnumerator RunDualHitStop(int version, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && IsCurrentPlayback(version))
        {
            if (attacker == null || defender == null)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        SetGuardActorsPaused(false);
        dualHitStopCoroutine = null;
    }

    private void FinishPlayback(int version)
    {
        if (!IsCurrentPlayback(version))
        {
            return;
        }

        StopSecondaryCoroutines();
        CleanupGuardActorsPreservingPose();
        Action callback = onFinished;
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = true;
        callback?.Invoke();
    }

    private void PrepareGuardActors()
    {
        attacker.SetPresentationPaused(false);
        attacker.ClearSlashEffect();
        attacker.ClearPerfectGuardEffect();
        attacker.ClearBodyVisualOffsets();

        defender.SetPresentationPaused(false);
        defender.ClearSlashEffect();
        defender.ClearPerfectGuardEffect();
        defender.ClearBodyVisualOffsets();
    }

    private void ResetGuardActorsToStableIdle()
    {
        attacker?.ResetToStableIdlePresentation();
        defender?.ResetToStableIdlePresentation();
    }

    private void CleanupGuardActorsPreservingPose()
    {
        // 正常完成只清瞬时反馈，攻击方最终Pose与防守方Guard Pose继续保留。
        if (attacker != null)
        {
            attacker.SetPresentationPaused(false);
            attacker.ClearSlashEffect();
            attacker.FinishSlashPresentation();
        }

        if (defender != null)
        {
            defender.SetPresentationPaused(false);
            defender.ClearPerfectGuardEffect();
            defender.FinishGuardPresentation();
        }
    }

    private void SetGuardActorsPaused(bool paused)
    {
        if (attacker != null)
        {
            attacker.SetPresentationPaused(paused);
        }

        if (defender != null)
        {
            defender.SetPresentationPaused(paused);
        }
    }

    private bool HasActiveTail()
    {
        return meleeGuardReactionRunning || parryRecoilCoroutine != null ||
            perfectGuardFxRunning ||
            guardRecoilCoroutine != null ||
            dualHitStopCoroutine != null;
    }

    private void StopOwnedCoroutines()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        StopSecondaryCoroutines();
    }

    private void StopSecondaryCoroutines()
    {
        if (meleeGuardReactionCoroutine != null)
        {
            StopCoroutine(meleeGuardReactionCoroutine);
            meleeGuardReactionCoroutine = null;
        }
        meleeGuardReactionRunning = false;
        if (parryRecoilCoroutine != null)
        {
            StopCoroutine(parryRecoilCoroutine);
            parryRecoilCoroutine = null;
        }

        perfectGuardFxRunning = false;

        if (guardRecoilCoroutine != null)
        {
            StopCoroutine(guardRecoilCoroutine);
            guardRecoilCoroutine = null;
        }

        if (dualHitStopCoroutine != null)
        {
            StopCoroutine(dualHitStopCoroutine);
            dualHitStopCoroutine = null;
        }
    }

    private void ClearPlaybackReferences()
    {
        attacker = null;
        defender = null;
        onVisualImpact = null;
        onFinished = null;
        onReactionStarted = null;
        attackerWorldRoot = null;
        defenderWorldRoot = null;
        meleeGuardReactionCoroutine = null;
        meleeGuardReactionRunning = false;
        playbackCoroutine = null;
        parryRecoilCoroutine = null;
        perfectGuardFxRunning = false;
        guardRecoilCoroutine = null;
        dualHitStopCoroutine = null;
        visualImpactReached = false;
        attackerDelivery = BattlePresentationAttackDeliveryKind.None;
        attackDirectionSign = 1f;
    }

    internal static bool IsSupportedAttackDelivery(
        BattlePresentationAttackDeliveryKind delivery
    )
    {
        return delivery == BattlePresentationAttackDeliveryKind.Melee ||
            delivery ==
                BattlePresentationAttackDeliveryKind.CloseRangeShoot ||
            delivery ==
                BattlePresentationAttackDeliveryKind.LongRangeShoot;
    }

    internal static bool UsesMeleeVisual(
        BattlePresentationAttackDeliveryKind delivery
    )
    {
        return delivery == BattlePresentationAttackDeliveryKind.Melee;
    }

    internal static bool UsesCloseRangeShootVisual(
        BattlePresentationAttackDeliveryKind delivery
    )
    {
        return delivery ==
            BattlePresentationAttackDeliveryKind.CloseRangeShoot;
    }

    internal static bool UsesLongRangeShootVisual(
        BattlePresentationAttackDeliveryKind delivery
    )
    {
        return delivery ==
            BattlePresentationAttackDeliveryKind.LongRangeShoot;
    }

    private bool IsCurrentPlayback(int version)
    {
        return IsRunning && playbackVersion == version;
    }

    private bool ValidatePresentationProfile()
    {
        if (presentationProfile != null)
        {
            return true;
        }

        Debug.LogError(
            "[AttackVsGuardPresentationPlayer] 播放失败：未配置 " +
            "BattleAttackVsGuardPresentationProfile。",
            this
        );
        return false;
    }
}
