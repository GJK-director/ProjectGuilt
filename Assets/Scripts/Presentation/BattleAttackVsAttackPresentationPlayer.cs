using System;
using System.Collections;
using UnityEngine;

// 复用Sandbox Digit9基线，只播放AttackVsAttack视觉，不读取战斗规则或输入。
public sealed class BattleAttackVsAttackPresentationPlayer : MonoBehaviour
{
    private enum PlaybackStage
    {
        None,
        ClashReadyApproach,
        ResolvedWinnerAttack
    }

    [SerializeField] private float dualClashReadyGap = 3.5f;
    [SerializeField] private float sprintDuration = 0.35f;
    [SerializeField] private float afterimageSpawnInterval = 0.08f;
    [SerializeField] private float slashHoldDuration = 0.5f;
    [SerializeField] private float hitStopDuration = 0.08f;

    public bool IsRunning { get; private set; }
    public bool IsFinished { get; private set; }
    public float ClashReadyGap => Mathf.Max(0f, dualClashReadyGap);
    public float SprintDuration => Mathf.Max(0f, sprintDuration);

    private PlaybackStage playbackStage;
    private BattleCharacterPresentationController firstActor;
    private BattleCharacterPresentationController secondActor;
    private Transform firstWorldRoot;
    private Transform secondWorldRoot;
    private BattleCharacterPresentationController winner;
    private BattleCharacterPresentationController loser;
    private Action onVisualImpact;
    private Action onFinished;
    private Coroutine playbackCoroutine;
    private Coroutine loserHitCoroutine;
    private Coroutine dualHitStopCoroutine;
    private bool visualImpactInvoked;
    private int playbackVersion;
    private float attackDirectionSign;

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
        ResetCurrentActors();
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
        Action finishedCallback
    )
    {
        if (IsRunning || sideA == null || sideB == null ||
            sideAWorldRoot == null || sideBWorldRoot == null)
        {
            return false;
        }

        playbackStage = PlaybackStage.ClashReadyApproach;
        firstActor = sideA;
        secondActor = sideB;
        firstWorldRoot = sideAWorldRoot;
        secondWorldRoot = sideBWorldRoot;
        onFinished = finishedCallback;
        visualImpactInvoked = false;
        IsRunning = true;
        IsFinished = false;
        playbackVersion++;

        int version = playbackVersion;
        playbackCoroutine = StartCoroutine(RunClashReadyApproach(version));
        return true;
    }

    public bool TryPlayResolvedWinnerAttack(
        BattleCharacterPresentationController winnerController,
        BattleCharacterPresentationController loserController,
        float directionSign,
        Action visualImpactCallback,
        Action finishedCallback
    )
    {
        if (IsRunning || winnerController == null || loserController == null)
        {
            return false;
        }

        playbackStage = PlaybackStage.ResolvedWinnerAttack;
        winner = winnerController;
        loser = loserController;
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

    public void CancelAndReset()
    {
        playbackVersion++;
        StopOwnedCoroutines();
        ResetCurrentActors();
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = false;
    }

    private IEnumerator RunClashReadyApproach(int version)
    {
        firstActor.SetSprint();
        secondActor.SetSprint();

        Vector3 firstStart = firstWorldRoot.position;
        Vector3 secondStart = secondWorldRoot.position;
        float horizontalDelta = secondStart.x - firstStart.x;
        float directionSign = Mathf.Abs(horizontalDelta) > 0.0001f
            ? Mathf.Sign(horizontalDelta)
            : 1f;
        float safeGap = ClashReadyGap;
        float horizontalDistance = Mathf.Abs(horizontalDelta);

        if (horizontalDistance <= safeGap)
        {
            FinishApproach(version);
            yield break;
        }

        float midpointX = (firstStart.x + secondStart.x) * 0.5f;
        Vector3 firstTarget = firstStart;
        Vector3 secondTarget = secondStart;
        firstTarget.x = midpointX - directionSign * safeGap * 0.5f;
        secondTarget.x = midpointX + directionSign * safeGap * 0.5f;

        float safeDuration = SprintDuration;
        if (safeDuration <= 0f)
        {
            firstWorldRoot.position = firstTarget;
            secondWorldRoot.position = secondTarget;
            FinishApproach(version);
            yield break;
        }

        firstActor.SpawnAfterimage();
        secondActor.SpawnAfterimage();

        float elapsed = 0f;
        float afterimageElapsed = 0f;
        bool spawnRepeatedAfterimages = afterimageSpawnInterval > 0f;
        while (elapsed < safeDuration && IsCurrentPlayback(version))
        {
            if (!HasValidApproachActors())
            {
                FinishApproach(version);
                yield break;
            }

            elapsed += Time.deltaTime;
            float linearT = Mathf.Clamp01(elapsed / safeDuration);
            float easedT = BattlePresentationEasing.EaseOutQuad(linearT);
            firstWorldRoot.position = firstStart +
                (firstTarget - firstStart) * easedT;
            secondWorldRoot.position = secondStart +
                (secondTarget - secondStart) * easedT;

            if (spawnRepeatedAfterimages)
            {
                afterimageElapsed += Time.deltaTime;
                if (afterimageElapsed >= afterimageSpawnInterval &&
                    elapsed < safeDuration)
                {
                    firstActor.SpawnAfterimage();
                    secondActor.SpawnAfterimage();
                    afterimageElapsed = 0f;
                }
            }

            yield return null;
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        if (HasValidApproachActors())
        {
            firstWorldRoot.position = firstTarget;
            secondWorldRoot.position = secondTarget;
        }

        // 到达后保留Sprint Pose；Manual Roll等待由Runner负责。
        FinishApproach(version);
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

        float remainingSlashHold = Mathf.Max(0f, slashHoldDuration) -
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

        if (hitStopDuration > 0f && winner != null && loser != null)
        {
            winner.SetPresentationPaused(true);
            loser.SetPresentationPaused(true);
            dualHitStopCoroutine = StartCoroutine(RunDualHitStop(version));
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
                attackDirectionSign
            );
        }

        if (IsCurrentPlayback(version))
        {
            loserHitCoroutine = null;
        }
    }

    private IEnumerator RunDualHitStop(int version)
    {
        float elapsed = 0f;
        while (elapsed < hitStopDuration && IsCurrentPlayback(version))
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

    private void FinishApproach(int version)
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

    private void FinishResolvedAttack(int version)
    {
        if (!IsCurrentPlayback(version))
        {
            return;
        }

        StopSecondaryCoroutines();
        ResetResolvedActors();
        Action callback = onFinished;
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = true;
        callback?.Invoke();
    }

    private void ResetCurrentActors()
    {
        if (playbackStage == PlaybackStage.ResolvedWinnerAttack)
        {
            ResetResolvedActors();
            return;
        }

        firstActor?.ResetToStableIdlePresentation();
        secondActor?.ResetToStableIdlePresentation();
    }

    private void ResetResolvedActors()
    {
        SetResolvedActorsPaused(false);
        winner?.ClearSlashEffect();

        // Winner与Loser同步收尾，避免渲染出单方先恢复的中间帧。
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
        onVisualImpact = null;
        onFinished = null;
        playbackCoroutine = null;
        loserHitCoroutine = null;
        dualHitStopCoroutine = null;
    }

    private bool IsCurrentPlayback(int version)
    {
        return IsRunning && playbackVersion == version;
    }
}
