using System;
using System.Collections;
using UnityEngine;

public sealed class BattleSpecialLongRangeDuelPresentationPlayer :
    MonoBehaviour
{
    private const float PositionTolerance = 0.0001f;

    public bool IsRunning { get; private set; }

    private BattleUnitViewHandle shooterHandle;
    private BattleUnitViewHandle opponentHandle;
    private BattleCharacterPresentationController shooterPresentation;
    private BattleCharacterPresentationController opponentPresentation;
    private BattleCameraDirector cameraDirector;
    private BattleSpecialLongRangeDuelPresentationProfile profile;
    private Transform shooterWorldRoot;
    private Transform opponentWorldRoot;
    private Action onFinished;
    private Coroutine playbackCoroutine;
    private int playbackVersion;

    void OnDisable()
    {
        CancelAndReset();
    }

    public bool TryPlay(
        BattleUnitViewHandle shooter,
        BattleCharacterPresentationController shooterController,
        BattleUnitViewHandle opponent,
        BattleCharacterPresentationController opponentController,
        BattleCameraDirector director,
        BattleSpecialLongRangeDuelPresentationProfile presentationProfile,
        Action finishedCallback
    )
    {
        if (IsRunning || shooter == null || opponent == null ||
            shooter.WorldRoot == null || opponent.WorldRoot == null ||
            shooterController == null || opponentController == null ||
            director == null || presentationProfile == null)
        {
            return false;
        }

        shooterHandle = shooter;
        opponentHandle = opponent;
        shooterPresentation = shooterController;
        opponentPresentation = opponentController;
        cameraDirector = director;
        profile = presentationProfile;
        shooterWorldRoot = shooter.WorldRoot.transform;
        opponentWorldRoot = opponent.WorldRoot.transform;
        onFinished = finishedCallback;

        if (!cameraDirector.TryBeginExternallyDrivenSingleActorApproach(
                opponentHandle,
                shooterHandle
            ))
        {
            ClearPlaybackReferences();
            return false;
        }

        shooterPresentation.SetAim();
        opponentPresentation.SetSprint();
        IsRunning = true;
        playbackVersion++;
        int version = playbackVersion;

        if (GetSeparation() <= profile.FinalRollSeparation +
            PositionTolerance)
        {
            if (!CompleteCameraFocusImmediately())
            {
                AbortPlayback(version);
                return true;
            }

            FinishNormally(version);
            return true;
        }

        playbackCoroutine = StartCoroutine(RunSequence(version));
        return true;
    }

    public void CancelAndReset()
    {
        playbackVersion++;
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
        }

        shooterPresentation?.ResetToStableIdlePresentation();
        opponentPresentation?.ResetToStableIdlePresentation();
        cameraDirector?.CancelExternallyDrivenSingleActorApproach(true);
        ClearPlaybackReferences();
        IsRunning = false;
    }

    private IEnumerator RunSequence(int version)
    {
        if (GetSeparation() > profile.FastApproachThreshold +
            PositionTolerance)
        {
            yield return RunFastApproach(version);
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        yield return RunFocusTransition(version);
        if (IsCurrentPlayback(version))
        {
            FinishNormally(version);
        }
    }

    private IEnumerator RunFastApproach(int version)
    {
        float targetX = GetOpponentTargetX(profile.FastApproachThreshold);
        float startX = opponentWorldRoot.position.x;
        float duration = profile.FastApproachDuration;
        if (duration <= Mathf.Epsilon)
        {
            SetOpponentWorldX(targetX);
            cameraDirector.ApplyExternallyDrivenSingleActorFollow();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration && IsCurrentPlayback(version))
        {
            elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
            float linearT = Mathf.Clamp01(elapsed / duration);
            float moveT = BattlePresentationEasing.EaseOutQuad(linearT);
            SetOpponentWorldX(Mathf.Lerp(startX, targetX, moveT));
            if (!cameraDirector.ApplyExternallyDrivenSingleActorFollow())
            {
                AbortPlayback(version);
                yield break;
            }
            yield return null;
        }

        if (IsCurrentPlayback(version))
        {
            SetOpponentWorldX(targetX);
            cameraDirector.ApplyExternallyDrivenSingleActorFollow();
        }
    }

    private IEnumerator RunFocusTransition(int version)
    {
        float currentSeparation = GetSeparation();
        float finalSeparation = profile.FinalRollSeparation;
        if (currentSeparation <= finalSeparation + PositionTolerance)
        {
            if (!CompleteCameraFocusImmediately())
            {
                AbortPlayback(version);
            }
            yield break;
        }

        if (!BeginCameraFocusTransition())
        {
            AbortPlayback(version);
            yield break;
        }

        float thresholdRange = profile.FastApproachThreshold -
            finalSeparation;
        float remainingRatio = thresholdRange > Mathf.Epsilon
            ? (currentSeparation - finalSeparation) / thresholdRange
            : 1f;
        float duration = profile.FocusTransitionDuration *
            Mathf.Clamp01(remainingRatio);
        float startX = opponentWorldRoot.position.x;
        float targetX = GetOpponentTargetX(finalSeparation);
        if (duration <= Mathf.Epsilon)
        {
            SetOpponentWorldX(targetX);
            cameraDirector.ApplyExternallyDrivenTwoUnitBlend(1f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration && IsCurrentPlayback(version))
        {
            elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
            float linearT = Mathf.Clamp01(elapsed / duration);
            float focusT = BattlePresentationEasing.EaseOutQuad(linearT);
            SetOpponentWorldX(Mathf.Lerp(startX, targetX, focusT));
            if (!cameraDirector.ApplyExternallyDrivenTwoUnitBlend(focusT))
            {
                AbortPlayback(version);
                yield break;
            }
            yield return null;
        }

        if (IsCurrentPlayback(version))
        {
            SetOpponentWorldX(targetX);
            cameraDirector.ApplyExternallyDrivenTwoUnitBlend(1f);
        }
    }

    private bool CompleteCameraFocusImmediately()
    {
        return BeginCameraFocusTransition() &&
            cameraDirector.ApplyExternallyDrivenTwoUnitBlend(1f);
    }

    private bool BeginCameraFocusTransition()
    {
        if (!profile.EnableShooterBiasedFinalFocus)
        {
            return cameraDirector.BeginExternallyDrivenTwoUnitBlend();
        }

        return cameraDirector.BeginExternallyDrivenTwoUnitBlend(
            shooterHandle,
            profile.FinalShooterFramingWeight,
            profile.FinalFocusOrbitRadius
        );
    }

    private float GetSeparation()
    {
        return Mathf.Abs(
            shooterWorldRoot.position.x - opponentWorldRoot.position.x
        );
    }

    private float GetOpponentTargetX(float desiredSeparation)
    {
        float delta = shooterWorldRoot.position.x -
            opponentWorldRoot.position.x;
        float directionSign = Mathf.Abs(delta) > PositionTolerance
            ? Mathf.Sign(delta)
            : 1f;
        return shooterWorldRoot.position.x -
            directionSign * Mathf.Max(0f, desiredSeparation);
    }

    private void SetOpponentWorldX(float worldX)
    {
        Vector3 position = opponentWorldRoot.position;
        position.x = worldX;
        opponentWorldRoot.position = position;
    }

    private void FinishNormally(int version)
    {
        if (!IsCurrentPlayback(version))
        {
            return;
        }

        cameraDirector.FinishExternallyDrivenSingleActorApproach();
        Action callback = onFinished;
        ClearPlaybackReferences();
        IsRunning = false;
        callback?.Invoke();
    }

    private void AbortPlayback(int version)
    {
        if (!IsCurrentPlayback(version))
        {
            return;
        }

        shooterPresentation?.ResetToStableIdlePresentation();
        opponentPresentation?.ResetToStableIdlePresentation();
        cameraDirector?.CancelExternallyDrivenSingleActorApproach(true);
        Action callback = onFinished;
        ClearPlaybackReferences();
        IsRunning = false;
        callback?.Invoke();
    }

    private bool IsCurrentPlayback(int version)
    {
        return IsRunning && playbackVersion == version &&
            shooterWorldRoot != null && opponentWorldRoot != null &&
            cameraDirector != null && profile != null;
    }

    private void ClearPlaybackReferences()
    {
        shooterHandle = null;
        opponentHandle = null;
        shooterPresentation = null;
        opponentPresentation = null;
        cameraDirector = null;
        profile = null;
        shooterWorldRoot = null;
        opponentWorldRoot = null;
        onFinished = null;
        playbackCoroutine = null;
    }
}
