using System;
using System.Collections;
using UnityEngine;

// 只编排LongRangeShoot对Melee Attack的视觉阶段，不读取或修改战斗规则与资源。
public sealed class BattleLongRangeShootVsAttackPresentationPlayer : MonoBehaviour
{
    private enum PlaybackStage
    {
        None,
        TerminalClash,
        ShotHit
    }

    public bool IsRunning { get; private set; }
    public bool IsFinished { get; private set; }

    private PlaybackStage playbackStage;
    private BattleCharacterPresentationController shooter;
    private BattleCharacterPresentationController meleeActor;
    private BattleCharacterPresentationController hitTarget;
    private Transform hitTargetWorldRoot;
    private Action onVisualImpact;
    private Action onFinished;
    private Coroutine playbackCoroutine;
    private Coroutine deflectionSlashCoroutine;
    private Coroutine hitReactionCoroutine;
    private bool muzzleFlashFinished;
    private bool deflectionSlashFinished;
    private bool hitReactionFinished;
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

        Action impactFallback = playbackStage == PlaybackStage.ShotHit &&
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

    public bool TryApplyActionBeginAim(
        BattleCharacterPresentationController shooterController,
        bool shotAvailable
    )
    {
        if (shooterController == null)
        {
            return false;
        }

        if (shotAvailable)
        {
            shooterController.SetAim();
        }

        return true;
    }

    public bool TryPlayTerminalClash(
        BattleCharacterPresentationController shooterController,
        BattleCharacterPresentationController meleeController,
        bool playDeflectionSlash,
        Action finishedCallback
    )
    {
        if (IsRunning || shooterController == null ||
            (playDeflectionSlash && meleeController == null))
        {
            return false;
        }

        playbackStage = PlaybackStage.TerminalClash;
        shooter = shooterController;
        meleeActor = meleeController;
        onFinished = finishedCallback;
        muzzleFlashFinished = false;
        deflectionSlashFinished = !playDeflectionSlash;
        IsRunning = true;
        IsFinished = false;
        playbackVersion++;

        int version = playbackVersion;
        shooter.SetShoot();
        shooter.PlayMuzzleFlash(() => MarkMuzzleFlashFinished(version));

        if (playDeflectionSlash)
        {
            float directionSign = GetDirectionSign(shooter, meleeActor);
            meleeActor.SetSlash();
            Coroutine startedSlash = StartCoroutine(
                RunDeflectionSlash(version, directionSign)
            );
            if (IsCurrentPlayback(version))
            {
                deflectionSlashCoroutine = startedSlash;
            }
        }

        Coroutine startedPlayback = StartCoroutine(RunTerminalClash(version));
        if (IsCurrentPlayback(version))
        {
            playbackCoroutine = startedPlayback;
        }
        return true;
    }

    public bool TryPlayShotHit(
        BattleCharacterPresentationController shooterController,
        BattleCharacterPresentationController targetController,
        Transform targetWorldRoot,
        float directionSign,
        Action visualImpactCallback,
        Action finishedCallback
    )
    {
        if (IsRunning || shooterController == null ||
            targetController == null || targetWorldRoot == null)
        {
            return false;
        }

        playbackStage = PlaybackStage.ShotHit;
        shooter = shooterController;
        hitTarget = targetController;
        hitTargetWorldRoot = targetWorldRoot;
        attackDirectionSign = directionSign >= 0f ? 1f : -1f;
        onVisualImpact = visualImpactCallback;
        onFinished = finishedCallback;
        hitReactionFinished = false;
        visualImpactInvoked = false;
        IsRunning = true;
        IsFinished = false;
        playbackVersion++;

        int version = playbackVersion;
        Coroutine startedPlayback = StartCoroutine(RunShotHit(version));
        if (IsCurrentPlayback(version))
        {
            playbackCoroutine = startedPlayback;
        }
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

    private IEnumerator RunTerminalClash(int version)
    {
        while (IsCurrentPlayback(version) &&
            (!muzzleFlashFinished || !deflectionSlashFinished))
        {
            yield return null;
        }

        if (IsCurrentPlayback(version))
        {
            FinishNormally(version);
        }
    }

    private IEnumerator RunDeflectionSlash(int version, float directionSign)
    {
        if (meleeActor != null)
        {
            // 远距离Deflection只播放Slash，故意不提供Visual Impact回调。
            yield return meleeActor.PlaySlashPresentation(directionSign);
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        meleeActor?.ClearSlashEffect();
        meleeActor?.FinishSlashPresentation();
        deflectionSlashFinished = true;
        deflectionSlashCoroutine = null;
    }

    private IEnumerator RunShotHit(int version)
    {
        hitTarget.SetHit();
        hitReactionCoroutine = StartCoroutine(RunHitReaction(version));

        // 没有弹道时，目标进入Hit即作为正式Impact的视觉提交边界。
        ReachVisualImpact(version);

        while (IsCurrentPlayback(version) && !hitReactionFinished)
        {
            yield return null;
        }

        if (IsCurrentPlayback(version))
        {
            FinishNormally(version);
        }
    }

    private IEnumerator RunHitReaction(int version)
    {
        if (hitTarget != null)
        {
            yield return hitTarget.PlaySustainedHitReaction(
                hitTargetWorldRoot,
                attackDirectionSign
            );
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        hitReactionFinished = true;
        hitReactionCoroutine = null;
    }

    private void MarkMuzzleFlashFinished(int version)
    {
        if (IsCurrentPlayback(version))
        {
            muzzleFlashFinished = true;
        }
    }

    private void ReachVisualImpact(int version)
    {
        if (!IsCurrentPlayback(version) || visualImpactInvoked)
        {
            return;
        }

        visualImpactInvoked = true;
        Action callback = onVisualImpact;
        onVisualImpact = null;
        callback?.Invoke();
    }

    private void FinishNormally(int version)
    {
        if (!IsCurrentPlayback(version))
        {
            return;
        }

        if (playbackStage == PlaybackStage.ShotHit)
        {
            hitTarget?.FinishHitReaction();
        }

        Action callback = onFinished;
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = true;
        callback?.Invoke();
    }

    private void ResetCurrentActorsToStableIdle()
    {
        shooter?.ResetToStableIdlePresentation();
        meleeActor?.ResetToStableIdlePresentation();
        hitTarget?.ResetToStableIdlePresentation();
    }

    private void StopOwnedCoroutines()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
        }

        if (deflectionSlashCoroutine != null)
        {
            StopCoroutine(deflectionSlashCoroutine);
        }

        if (hitReactionCoroutine != null)
        {
            StopCoroutine(hitReactionCoroutine);
        }
    }

    private void ClearPlaybackReferences()
    {
        playbackStage = PlaybackStage.None;
        shooter = null;
        meleeActor = null;
        hitTarget = null;
        hitTargetWorldRoot = null;
        onVisualImpact = null;
        onFinished = null;
        playbackCoroutine = null;
        deflectionSlashCoroutine = null;
        hitReactionCoroutine = null;
        muzzleFlashFinished = false;
        deflectionSlashFinished = false;
        hitReactionFinished = false;
        visualImpactInvoked = false;
    }

    private bool IsCurrentPlayback(int version)
    {
        return IsRunning && playbackVersion == version;
    }

    private static float GetDirectionSign(
        BattleCharacterPresentationController shooterController,
        BattleCharacterPresentationController meleeController
    )
    {
        if (shooterController == null || meleeController == null)
        {
            return 1f;
        }

        float horizontalDelta = shooterController.transform.position.x -
            meleeController.transform.position.x;
        return Mathf.Abs(horizontalDelta) > 0.0001f
            ? Mathf.Sign(horizontalDelta)
            : 1f;
    }
}
