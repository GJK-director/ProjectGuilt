using System;
using System.Collections;
using UnityEngine;

// 复用Sandbox Digit9基线，只播放AttackVsAttack视觉，不读取战斗规则或输入。
public sealed class BattleAttackVsAttackPresentationPlayer : MonoBehaviour
{
    private enum PlaybackStage
    {
        None,
        ResolvedReengagementApproach,
        AttackTieResult,
        ResolvedWinnerAttack
    }

    [SerializeField]
    private BattleAttackVsAttackPresentationProfile presentationProfile;

    public bool IsRunning { get; private set; }
    public bool IsFinished { get; private set; }
    public float SprintDuration => presentationProfile != null
        ? presentationProfile.SprintDuration
        : 0f;
    public float AfterimageSpawnInterval => presentationProfile != null
        ? presentationProfile.AfterimageSpawnInterval
        : 0f;

    private PlaybackStage playbackStage;
    private BattleCharacterPresentationController firstActor;
    private BattleCharacterPresentationController secondActor;
    private Transform firstWorldRoot;
    private Transform secondWorldRoot;
    private BattleCharacterPresentationController winner;
    private BattleCharacterPresentationController loser;
    private Transform loserWorldRoot;
    private Action onVisualImpact;
    private Action onFinished;
    private Coroutine playbackCoroutine;
    private Coroutine loserHitCoroutine;
    private Coroutine dualHitStopCoroutine;
    private Coroutine firstTieSlashCoroutine;
    private Coroutine secondTieSlashCoroutine;
    private Coroutine tieRecoilCoroutine;
    private BattleClashEngagementResult clashEngagementResult;
    private bool visualImpactInvoked;
    private bool firstTieImpactReached;
    private bool secondTieImpactReached;
    private bool tieCollisionReached;
    private bool firstTieSlashCompleted;
    private bool secondTieSlashCompleted;
    private bool tieRecoilCompleted;
    private int playbackVersion;
    private float attackDirectionSign;
    private float tieDirectionSign;

    void OnDisable()
    {
        if (!IsRunning)
        {
            CancelAndReset();
            return;
        }

        Action impactFallback = playbackStage ==
                PlaybackStage.ResolvedWinnerAttack &&
            !visualImpactInvoked
                ? onVisualImpact
                : null;
        Action finishedFallback = onFinished;
        playbackVersion++;
        StopOwnedCoroutines();
        ResetCurrentActorsToStableIdle();
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = true;
        impactFallback?.Invoke();
        finishedFallback?.Invoke();
    }

    public bool TryPlayClashReadyApproach(
        BattleCharacterPresentationController sideA,
        Transform sideAWorldRoot,
        BattleCharacterPresentationController sideB,
        Transform sideBWorldRoot,
        BattleClashEngagementResult engagementResult,
        Action finishedCallback
    )
    {
        if (!ValidatePresentationProfile(nameof(TryPlayClashReadyApproach)))
        {
            return false;
        }

        if (IsRunning || sideA == null || sideB == null ||
            sideAWorldRoot == null || sideBWorldRoot == null ||
            engagementResult == null)
        {
            return false;
        }

        // ClashReady只切换拼点等待Pose，不拥有WorldRoot接敌位移。
        sideA.SetSprint();
        sideB.SetSprint();
        IsFinished = true;
        finishedCallback?.Invoke();
        return true;
    }

    // 仅供结果阶段需要真实接敌的路径使用，不属于ClashReady。
    public bool TryPlayResolvedReengagementApproach(
        BattleCharacterPresentationController sideA,
        Transform sideAWorldRoot,
        BattleCharacterPresentationController sideB,
        Transform sideBWorldRoot,
        BattleClashEngagementResult engagementResult,
        Action finishedCallback
    )
    {
        if (!ValidatePresentationProfile(
                nameof(TryPlayResolvedReengagementApproach)
            ))
        {
            return false;
        }

        if (IsRunning || sideA == null || sideB == null ||
            sideAWorldRoot == null || sideBWorldRoot == null ||
            engagementResult == null)
        {
            return false;
        }

        if (!BattleClashEngagementResolver.RequiresApproach(
                sideAWorldRoot.position,
                sideBWorldRoot.position,
                engagementResult
            ))
        {
            IsFinished = true;
            finishedCallback?.Invoke();
            return true;
        }

        playbackStage = PlaybackStage.ResolvedReengagementApproach;
        firstActor = sideA;
        secondActor = sideB;
        firstWorldRoot = sideAWorldRoot;
        secondWorldRoot = sideBWorldRoot;
        clashEngagementResult = engagementResult;
        onFinished = finishedCallback;
        IsRunning = true;
        IsFinished = false;
        playbackVersion++;

        int version = playbackVersion;
        playbackCoroutine = StartCoroutine(
            RunResolvedReengagementApproach(version)
        );
        return true;
    }

    public bool TryPlayResolvedWinnerAttack(
        BattleCharacterPresentationController winnerController,
        BattleCharacterPresentationController loserController,
        Transform loserRoot,
        float directionSign,
        Action visualImpactCallback,
        Action finishedCallback
    )
    {
        if (!ValidatePresentationProfile(
                nameof(TryPlayResolvedWinnerAttack)
            ))
        {
            return false;
        }

        if (IsRunning || winnerController == null || loserController == null ||
            loserRoot == null)
        {
            return false;
        }

        playbackStage = PlaybackStage.ResolvedWinnerAttack;
        winner = winnerController;
        loser = loserController;
        loserWorldRoot = loserRoot;
        attackDirectionSign = directionSign >= 0f ? 1f : -1f;
        onVisualImpact = visualImpactCallback;
        onFinished = finishedCallback;
        visualImpactInvoked = false;
        IsRunning = true;
        IsFinished = false;
        playbackVersion++;

        int version = playbackVersion;
        playbackCoroutine = StartCoroutine(
            RunResolvedWinnerAttack(version)
        );
        return true;
    }

    public bool TryPlayTieResult(
        BattleCharacterPresentationController sideA,
        Transform sideAWorldRoot,
        BattleCharacterPresentationController sideB,
        Transform sideBWorldRoot,
        BattleClashEngagementResult engagementResult,
        Action finishedCallback
    )
    {
        if (!ValidatePresentationProfile(nameof(TryPlayTieResult)))
        {
            return false;
        }

        if (IsRunning || sideA == null || sideB == null ||
            sideAWorldRoot == null || sideBWorldRoot == null ||
            engagementResult == null)
        {
            return false;
        }

        playbackStage = PlaybackStage.AttackTieResult;
        firstActor = sideA;
        secondActor = sideB;
        firstWorldRoot = sideAWorldRoot;
        secondWorldRoot = sideBWorldRoot;
        clashEngagementResult = engagementResult;
        onFinished = finishedCallback;
        ResetTiePlaybackState();
        IsRunning = true;
        IsFinished = false;
        playbackVersion++;

        int version = playbackVersion;
        playbackCoroutine = StartCoroutine(RunTieResult(version));
        return true;
    }

    public void CancelAndReset()
    {
        playbackVersion++;
        StopOwnedCoroutines();
        ResetCurrentActorsToStableIdle();
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = false;
    }

    private IEnumerator RunApproachMovement(int version)
    {
        yield return BattleClashReadyApproachMotion.Play(
            firstActor,
            firstWorldRoot,
            secondActor,
            secondWorldRoot,
            clashEngagementResult,
            SprintDuration,
            presentationProfile.AfterimageSpawnInterval,
            () => IsCurrentPlayback(version)
        );
    }

    private IEnumerator RunResolvedReengagementApproach(int version)
    {
        yield return RunApproachMovement(version);

        if (IsCurrentPlayback(version))
        {
            FinishResolvedReengagementApproach(version);
        }
    }

    private IEnumerator RunTieResult(int version)
    {
        if (!HasValidApproachActors())
        {
            FinishTieResult(version);
            yield break;
        }

        float horizontalDelta =
            secondWorldRoot.position.x - firstWorldRoot.position.x;
        tieDirectionSign = Mathf.Abs(horizontalDelta) > 0.0001f
            ? Mathf.Sign(horizontalDelta)
            : 1f;
        float slashStartedAt = Time.time;

        firstActor.SetSlash();
        secondActor.SetSlash();
        firstTieSlashCoroutine = StartCoroutine(
            RunTieSlashPresentation(
                version,
                firstActor,
                tieDirectionSign,
                true
            )
        );
        secondTieSlashCoroutine = StartCoroutine(
            RunTieSlashPresentation(
                version,
                secondActor,
                -tieDirectionSign,
                false
            )
        );

        while ((!firstTieSlashCompleted || !secondTieSlashCompleted ||
                !tieRecoilCompleted) &&
            IsCurrentPlayback(version))
        {
            if (!HasValidApproachActors())
            {
                ResetCurrentActorsToStableIdle();
                FinishTieResult(version);
                yield break;
            }

            if (firstTieSlashCompleted && secondTieSlashCompleted &&
                !tieCollisionReached)
            {
                ResetCurrentActorsToStableIdle();
                FinishTieResult(version);
                yield break;
            }

            yield return null;
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        firstTieSlashCoroutine = null;
        secondTieSlashCoroutine = null;
        float remainingSlashHold = presentationProfile.SlashHoldDuration -
            (Time.time - slashStartedAt);
        while (remainingSlashHold > 0f && IsCurrentPlayback(version))
        {
            remainingSlashHold -= Time.deltaTime;
            yield return null;
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        // 同一帧清理双方Slash本地状态，再从当前Recoil位置重新接敌。
        firstActor.ClearSlashEffect();
        secondActor.ClearSlashEffect();
        firstActor.FinishSlashPresentation();
        secondActor.FinishSlashPresentation();

        yield return RunApproachMovement(version);
        if (IsCurrentPlayback(version))
        {
            FinishTieResult(version);
        }
    }

    private IEnumerator RunTieSlashPresentation(
        int version,
        BattleCharacterPresentationController actor,
        float directionSign,
        bool isFirstActor
    )
    {
        if (actor != null)
        {
            yield return actor.PlaySlashPresentation(
                directionSign,
                () => MarkTieImpactReached(version, isFirstActor)
            );
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        if (isFirstActor)
        {
            firstTieSlashCompleted = true;
        }
        else
        {
            secondTieSlashCompleted = true;
        }
    }

    private void MarkTieImpactReached(int version, bool isFirstActor)
    {
        if (!IsCurrentPlayback(version) || tieCollisionReached)
        {
            return;
        }

        if (isFirstActor)
        {
            firstTieImpactReached = true;
        }
        else
        {
            secondTieImpactReached = true;
        }

        // Tie只汇合视觉命中标记，不创建Combat Impact或Damage。
        tieCollisionReached = firstTieImpactReached &&
            secondTieImpactReached;
        if (tieCollisionReached)
        {
            tieRecoilCoroutine = StartCoroutine(
                RunTieRecoil(version, tieDirectionSign)
            );
        }
    }

    private IEnumerator RunTieRecoil(int version, float directionSign)
    {
        if (!HasValidApproachActors())
        {
            tieRecoilCompleted = true;
            yield break;
        }

        Vector3 firstStart = firstWorldRoot.position;
        Vector3 secondStart = secondWorldRoot.position;
        float distance = presentationProfile.TieRecoilDistance;
        float duration = presentationProfile.TieRecoilDuration;
        Vector3 firstTarget = firstStart -
            Vector3.right * directionSign * distance;
        Vector3 secondTarget = secondStart +
            Vector3.right * directionSign * distance;

        if (duration <= 0f)
        {
            firstWorldRoot.position = firstTarget;
            secondWorldRoot.position = secondTarget;
            tieRecoilCompleted = true;
            tieRecoilCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration && IsCurrentPlayback(version))
        {
            if (!HasValidApproachActors())
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float linearT = Mathf.Clamp01(elapsed / duration);
            float easedT = BattlePresentationEasing.EaseOutQuad(linearT);
            firstWorldRoot.position = firstStart +
                (firstTarget - firstStart) * easedT;
            secondWorldRoot.position = secondStart +
                (secondTarget - secondStart) * easedT;
            yield return null;
        }

        if (IsCurrentPlayback(version) && HasValidApproachActors())
        {
            firstWorldRoot.position = firstTarget;
            secondWorldRoot.position = secondTarget;
            tieRecoilCompleted = true;
            tieRecoilCoroutine = null;
        }
    }

    private IEnumerator RunResolvedWinnerAttack(int version)
    {
        winner.SetSlash();
        float slashStartedAt = Time.time;
        yield return winner.PlaySlashPresentation(
            attackDirectionSign,
            () => ReachVisualImpact(version)
        );

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        if (!visualImpactInvoked)
        {
            ReachVisualImpact(version);
        }

        float remainingSlashHold = presentationProfile.SlashHoldDuration -
            (Time.time - slashStartedAt);
        while (remainingSlashHold > 0f && IsCurrentPlayback(version))
        {
            remainingSlashHold -= Time.deltaTime;
            yield return null;
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        FinishResolvedAttack(version);
    }

    private void ReachVisualImpact(int version)
    {
        if (!IsCurrentPlayback(version) || visualImpactInvoked)
        {
            return;
        }

        visualImpactInvoked = true;
        if (loser != null)
        {
            loser.SetHit();
            loserHitCoroutine = StartCoroutine(
                RunLoserSustainedHit(version)
            );
        }

        float safeHitStopDuration = presentationProfile.HitStopDuration;
        if (safeHitStopDuration > 0f && winner != null && loser != null)
        {
            winner.SetPresentationPaused(true);
            loser.SetPresentationPaused(true);
            dualHitStopCoroutine = StartCoroutine(
                RunDualHitStop(version, safeHitStopDuration)
            );
        }

        Action callback = onVisualImpact;
        onVisualImpact = null;
        callback?.Invoke();
    }

    private IEnumerator RunLoserSustainedHit(int version)
    {
        if (loser != null)
        {
            yield return loser.PlaySustainedHitReaction(
                loserWorldRoot,
                attackDirectionSign
            );
        }

        if (IsCurrentPlayback(version))
        {
            loserHitCoroutine = null;
        }
    }

    private IEnumerator RunDualHitStop(int version, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && IsCurrentPlayback(version))
        {
            if (winner == null || loser == null)
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

        SetResolvedActorsPaused(false);
        dualHitStopCoroutine = null;
    }

    private void FinishResolvedAttack(int version)
    {
        if (!IsCurrentPlayback(version))
        {
            return;
        }

        StopSecondaryCoroutines();
        CleanupResolvedActorsPreservingPose();
        Action callback = onFinished;
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = true;
        callback?.Invoke();
    }

    private void FinishResolvedReengagementApproach(int version)
    {
        if (!IsCurrentPlayback(version))
        {
            return;
        }

        Action callback = onFinished;
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = true;
        callback?.Invoke();
    }

    private void FinishTieResult(int version)
    {
        if (!IsCurrentPlayback(version))
        {
            return;
        }

        Action callback = onFinished;
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = true;
        callback?.Invoke();
    }

    private void ResetCurrentActorsToStableIdle()
    {
        if (playbackStage == PlaybackStage.ResolvedWinnerAttack)
        {
            winner?.ResetToStableIdlePresentation();
            loser?.ResetToStableIdlePresentation();
            return;
        }

        firstActor?.ResetToStableIdlePresentation();
        secondActor?.ResetToStableIdlePresentation();
    }

    private void CleanupResolvedActorsPreservingPose()
    {
        SetResolvedActorsPaused(false);
        winner?.ClearSlashEffect();

        // 正常完成只清局部瞬时状态，Winner/Loser保留Slash与Hit最终Pose。
        winner?.FinishSlashPresentation();
        loser?.FinishHitReaction();
    }

    private void SetResolvedActorsPaused(bool paused)
    {
        winner?.SetPresentationPaused(paused);
        loser?.SetPresentationPaused(paused);
    }

    private bool HasValidApproachActors()
    {
        return firstActor != null && secondActor != null &&
            firstWorldRoot != null && secondWorldRoot != null;
    }

    private void StopOwnedCoroutines()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
        }

        StopSecondaryCoroutines();
    }

    private void StopSecondaryCoroutines()
    {
        if (loserHitCoroutine != null)
        {
            StopCoroutine(loserHitCoroutine);
            loserHitCoroutine = null;
        }

        if (dualHitStopCoroutine != null)
        {
            StopCoroutine(dualHitStopCoroutine);
            dualHitStopCoroutine = null;
        }

        if (firstTieSlashCoroutine != null)
        {
            StopCoroutine(firstTieSlashCoroutine);
            firstTieSlashCoroutine = null;
        }

        if (secondTieSlashCoroutine != null)
        {
            StopCoroutine(secondTieSlashCoroutine);
            secondTieSlashCoroutine = null;
        }

        if (tieRecoilCoroutine != null)
        {
            StopCoroutine(tieRecoilCoroutine);
            tieRecoilCoroutine = null;
        }
    }

    private void ClearPlaybackReferences()
    {
        playbackStage = PlaybackStage.None;
        firstActor = null;
        secondActor = null;
        firstWorldRoot = null;
        secondWorldRoot = null;
        winner = null;
        loser = null;
        loserWorldRoot = null;
        onVisualImpact = null;
        onFinished = null;
        playbackCoroutine = null;
        loserHitCoroutine = null;
        dualHitStopCoroutine = null;
        firstTieSlashCoroutine = null;
        secondTieSlashCoroutine = null;
        tieRecoilCoroutine = null;
        clashEngagementResult = null;
        ResetTiePlaybackState();
    }

    private bool IsCurrentPlayback(int version)
    {
        return IsRunning && playbackVersion == version;
    }

    private void ResetTiePlaybackState()
    {
        firstTieImpactReached = false;
        secondTieImpactReached = false;
        tieCollisionReached = false;
        firstTieSlashCompleted = false;
        secondTieSlashCompleted = false;
        tieRecoilCompleted = false;
        tieDirectionSign = 1f;
    }

    private bool ValidatePresentationProfile(string requestName)
    {
        if (presentationProfile != null)
        {
            return true;
        }

        // 配置缺失时显式拒绝请求，不隐藏回退到第二套时间参数。
        Debug.LogError(
            "[AttackVsAttackPresentationPlayer] " + requestName +
            " 失败：未配置 BattleAttackVsAttackPresentationProfile。",
            this
        );
        return false;
    }
}
