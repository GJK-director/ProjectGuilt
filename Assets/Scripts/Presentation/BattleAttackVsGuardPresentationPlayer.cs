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
    private enum PlaybackStage
    {
        None,
        ClashReadyApproach,
        GuardImpact
    }

    [SerializeField]
    private BattleAttackVsGuardPresentationProfile presentationProfile;

    public bool IsRunning { get; private set; }
    public bool IsFinished { get; private set; }

    private PlaybackStage playbackStage;
    private BattleCharacterPresentationController approachSideA;
    private BattleCharacterPresentationController approachSideB;
    private Transform approachSideAWorldRoot;
    private Transform approachSideBWorldRoot;
    private BattleClashEngagementResult clashEngagementResult;
    private BattleCharacterPresentationController attacker;
    private BattleCharacterPresentationController defender;
    private BattleGuardPresentationResult presentationResult;
    private float attackDirectionSign = 1f;
    private Action onVisualImpact;
    private Action onFinished;
    private Coroutine playbackCoroutine;
    private Coroutine parryRecoilCoroutine;
    private Coroutine perfectGuardEffectCoroutine;
    private Coroutine guardRecoilCoroutine;
    private Coroutine dualHitStopCoroutine;
    private int playbackVersion;
    private bool visualImpactReached;

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
        if (IsRunning || !isActiveAndEnabled || defenseSide == null ||
            attackSide == null || defenseWorldRoot == null ||
            attackWorldRoot == null || engagementResult == null ||
            !ValidatePresentationProfile())
        {
            return false;
        }

        if (!BattleClashEngagementResolver.RequiresApproach(
                defenseWorldRoot.position,
                attackWorldRoot.position,
                engagementResult
            ))
        {
            // 已满足FinalGap时直接完成ActionBegin，保留双方当前Pose。
            IsFinished = true;
            completion?.Invoke();
            return true;
        }

        playbackVersion++;
        playbackStage = PlaybackStage.ClashReadyApproach;
        approachSideA = defenseSide;
        approachSideB = attackSide;
        approachSideAWorldRoot = defenseWorldRoot;
        approachSideBWorldRoot = attackWorldRoot;
        clashEngagementResult = engagementResult;
        onFinished = completion;
        IsRunning = true;
        IsFinished = false;
        playbackCoroutine = StartCoroutine(
            RunClashReadyApproach(playbackVersion)
        );
        return true;
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
        if (IsRunning || !isActiveAndEnabled || attackActor == null ||
            guardActor == null || !ValidatePresentationProfile())
        {
            return false;
        }

        playbackVersion++;
        playbackStage = PlaybackStage.GuardImpact;
        attacker = attackActor;
        defender = guardActor;
        presentationResult = result;
        attackDirectionSign = directionSign >= 0f ? 1f : -1f;
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

    private IEnumerator RunClashReadyApproach(int version)
    {
        yield return RunApproachMovement(version);

        if (IsCurrentPlayback(version))
        {
            FinishApproach(version);
        }
    }

    private IEnumerator RunApproachMovement(int version)
    {
        yield return BattleClashReadyApproachMotion.Play(
            approachSideA,
            approachSideAWorldRoot,
            approachSideB,
            approachSideBWorldRoot,
            clashEngagementResult,
            presentationProfile.SprintDuration,
            presentationProfile.AfterimageSpawnInterval,
            () => IsCurrentPlayback(version)
        );
    }

    private IEnumerator RunGuardImpact(int version)
    {
        PrepareGuardActors();
        defender.SetGuard();
        attacker.SetSlash();

        bool playAttackEffect =
            presentationResult == BattleGuardPresentationResult.ReducedDamage;
        yield return attacker.PlaySlashPresentation(
            attackDirectionSign,
            () => ReachVisualImpact(version),
            playAttackEffect
        );

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
            perfectGuardEffectCoroutine = StartCoroutine(
                RunPerfectGuardEffect(version)
            );
            parryRecoilCoroutine = StartCoroutine(
                RunParryRecoil(version)
            );
        }
        else
        {
            guardRecoilCoroutine = StartCoroutine(
                RunGuardRecoil(version)
            );
        }

        float hitStopDuration = presentationProfile.HitStopDuration;
        if (hitStopDuration > 0f)
        {
            SetGuardActorsPaused(true);
            dualHitStopCoroutine = StartCoroutine(
                RunDualHitStop(version, hitStopDuration)
            );
        }

        // 命中边界来自Character的Slash回调，并且每次播放最多通知一次宿主。
        Action callback = onVisualImpact;
        onVisualImpact = null;
        callback?.Invoke();
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

    private IEnumerator RunPerfectGuardEffect(int version)
    {
        if (defender != null)
        {
            yield return defender.PlayPerfectGuardEffect();
        }

        if (IsCurrentPlayback(version))
        {
            perfectGuardEffectCoroutine = null;
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

    private void FinishApproach(int version)
    {
        if (!IsCurrentPlayback(version))
        {
            return;
        }

        // 到达ClashReady后只结束位移，双方保持Sprint Pose等待正式Roll。
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
        if (playbackStage == PlaybackStage.ClashReadyApproach)
        {
            approachSideA?.ResetToStableIdlePresentation();
            approachSideB?.ResetToStableIdlePresentation();
            return;
        }

        attacker?.ResetToStableIdlePresentation();
        defender?.ResetToStableIdlePresentation();
    }

    private void CleanupGuardActorsPreservingPose()
    {
        // 正常完成只清瞬时反馈，攻击方Slash与防守方Guard Pose继续保留。
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
        return parryRecoilCoroutine != null ||
            perfectGuardEffectCoroutine != null ||
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
        if (parryRecoilCoroutine != null)
        {
            StopCoroutine(parryRecoilCoroutine);
            parryRecoilCoroutine = null;
        }

        if (perfectGuardEffectCoroutine != null)
        {
            StopCoroutine(perfectGuardEffectCoroutine);
            perfectGuardEffectCoroutine = null;
        }

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
        playbackStage = PlaybackStage.None;
        approachSideA = null;
        approachSideB = null;
        approachSideAWorldRoot = null;
        approachSideBWorldRoot = null;
        clashEngagementResult = null;
        attacker = null;
        defender = null;
        onVisualImpact = null;
        onFinished = null;
        playbackCoroutine = null;
        parryRecoilCoroutine = null;
        perfectGuardEffectCoroutine = null;
        guardRecoilCoroutine = null;
        dualHitStopCoroutine = null;
        visualImpactReached = false;
        attackDirectionSign = 1f;
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
