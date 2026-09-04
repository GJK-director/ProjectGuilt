using System;
using System.Collections;
using UnityEngine;

// 只编排LongRangeShoot正式视觉阶段，不读取或修改战斗规则与资源。
public sealed class BattleLongRangeShootVsAttackPresentationPlayer : MonoBehaviour
{
    private enum PlaybackStage
    {
        None,
        TerminalClash,
        ShotHit,
        ResponseWinner
    }

    public bool IsRunning { get; private set; }
    public bool IsFinished { get; private set; }

    private BattleHitPresentationProfile normalHitProfile;
    private BattleSpecialLongRangeDuelPresentationProfile
        specialShotHitProfile;

    private PlaybackStage playbackStage;
    private BattleCharacterPresentationController shooter;
    private BattleCharacterPresentationController meleeActor;
    private BattleCharacterPresentationController hitTarget;
    private BattleCharacterPresentationController responseWinner;
    private Transform hitTargetWorldRoot;
    private Action onTrueVisualImpact;
    private Action onVisualImpact;
    private Action onFinished;
    private Coroutine playbackCoroutine;
    private Coroutine deflectionSlashCoroutine;
    private Coroutine hitReactionCoroutine;
    private Coroutine responseEffectCoroutine;
    private bool muzzleFlashFinished;
    private bool deflectionSlashFinished;
    private bool hitReactionFinished;
    private bool visualImpactInvoked;
    private bool responseMotionFinished;
    private bool playShotVisual;
    private bool shotVisualFinished;
    private int playbackVersion;
    private float attackDirectionSign;

    public void ConfigureNormalHitProfile(
        BattleHitPresentationProfile profile
    )
    {
        normalHitProfile = profile;
    }

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
        return TryPlayShotHit(
            shooterController,
            targetController,
            targetWorldRoot,
            directionSign,
            false,
            null,
            visualImpactCallback,
            finishedCallback
        );
    }

    public bool TryPlayShotHit(
        BattleCharacterPresentationController shooterController,
        BattleCharacterPresentationController targetController,
        Transform targetWorldRoot,
        float directionSign,
        Action trueVisualImpactCallback,
        Action visualImpactCallback,
        Action finishedCallback
    )
    {
        return TryPlayShotHit(
            shooterController,
            targetController,
            targetWorldRoot,
            directionSign,
            false,
            trueVisualImpactCallback,
            visualImpactCallback,
            finishedCallback
        );
    }

    public bool TryPlayShotHit(
        BattleCharacterPresentationController shooterController,
        BattleCharacterPresentationController targetController,
        Transform targetWorldRoot,
        float directionSign,
        bool shouldPlayShotVisual,
        Action trueVisualImpactCallback,
        Action visualImpactCallback,
        Action finishedCallback
    )
    {
        return TryPlayShotHit(
            shooterController,
            targetController,
            targetWorldRoot,
            directionSign,
            shouldPlayShotVisual,
            null,
            trueVisualImpactCallback,
            visualImpactCallback,
            finishedCallback
        );
    }

    public bool TryPlayShotHit(
        BattleCharacterPresentationController shooterController,
        BattleCharacterPresentationController targetController,
        Transform targetWorldRoot,
        float directionSign,
        bool shouldPlayShotVisual,
        BattleSpecialLongRangeDuelPresentationProfile specialHitProfile,
        Action trueVisualImpactCallback,
        Action visualImpactCallback,
        Action finishedCallback
    )
    {
        if (normalHitProfile == null)
        {
            Debug.LogError(
                nameof(TryPlayShotHit) +
                " failed: NormalHitProfile is not assigned."
            );
            return false;
        }

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
        onTrueVisualImpact = trueVisualImpactCallback;
        onVisualImpact = visualImpactCallback;
        onFinished = finishedCallback;
        hitReactionFinished = false;
        visualImpactInvoked = false;
        playShotVisual = shouldPlayShotVisual;
        shotVisualFinished = !shouldPlayShotVisual;
        specialShotHitProfile = specialHitProfile != null &&
            specialHitProfile.EnableSpecialShotHitTuning
                ? specialHitProfile
                : null;
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

    public bool TryPlayGuardWinner(
        BattleCharacterPresentationController guardController,
        Action finishedCallback
    )
    {
        if (IsRunning || guardController == null)
        {
            return false;
        }

        BeginResponseWinner(guardController, finishedCallback);
        int version = playbackVersion;
        responseWinner.SetGuard();
        responseMotionFinished = false;
        responseEffectCoroutine = StartCoroutine(RunGuardWinnerEffect(version));
        playbackCoroutine = StartCoroutine(RunResponseWinner(version, true));
        return true;
    }

    public bool TryPlayDodgeWinner(
        BattleCharacterPresentationController dodgeController,
        float directionSign,
        Action finishedCallback
    )
    {
        if (IsRunning || dodgeController == null)
        {
            return false;
        }

        BeginResponseWinner(dodgeController, finishedCallback);
        int version = playbackVersion;
        responseWinner.SetDodge();
        responseMotionFinished = !responseWinner.PlayDodgeMotion(
            directionSign,
            () => MarkResponseMotionFinished(version)
        );
        playbackCoroutine = StartCoroutine(RunResponseWinner(version, false));
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
        if (playShotVisual)
        {
            shooter.SetShoot();
            shooter.PlayMuzzleFlash(
                () => MarkShotVisualFinished(version)
            );
        }

        hitTarget.SetHit();
        BattleNormalHitFxPlayer.TrySpawn(
            normalHitProfile,
            normalHitProfile.ShootHitFxSprite,
            hitTarget,
            hitTargetWorldRoot,
            attackDirectionSign
        );
        hitReactionCoroutine = StartCoroutine(RunHitReaction(version));

        // 没有弹道时，目标进入Hit即作为正式Impact的视觉提交边界。
        ReachVisualImpact(version);

        while (IsCurrentPlayback(version) &&
            (!hitReactionFinished || !shotVisualFinished))
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
            if (specialShotHitProfile != null)
            {
                yield return hitTarget.PlaySustainedHitReaction(
                    hitTargetWorldRoot,
                    attackDirectionSign,
                    normalHitProfile,
                    specialShotHitProfile.SpecialFollowKnockbackDistance
                );
            }
            else
            {
                yield return hitTarget.PlaySustainedHitReaction(
                    hitTargetWorldRoot,
                    attackDirectionSign,
                    normalHitProfile
                );
            }
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        hitReactionFinished = true;
        hitReactionCoroutine = null;
    }

    private IEnumerator RunResponseWinner(int version, bool isGuard)
    {
        // Guard至少保留到下一帧；Dodge等待角色层真实Motion结束。
        yield return null;
        while (IsCurrentPlayback(version) && !responseMotionFinished)
        {
            yield return null;
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        if (isGuard)
        {
            responseWinner?.FinishGuardPresentation();
        }
        else
        {
            responseWinner?.FinishDodgePresentation();
        }
        FinishNormally(version);
    }

    private IEnumerator RunGuardWinnerEffect(int version)
    {
        if (responseWinner != null)
        {
            // 复用Guard成功结果的现有效果，但不启动近战攻击方反冲。
            yield return responseWinner.PlayPerfectGuardEffect();
        }

        if (IsCurrentPlayback(version))
        {
            responseMotionFinished = true;
            responseEffectCoroutine = null;
        }
    }

    private void BeginResponseWinner(
        BattleCharacterPresentationController winnerController,
        Action finishedCallback
    )
    {
        playbackStage = PlaybackStage.ResponseWinner;
        responseWinner = winnerController;
        onFinished = finishedCallback;
        responseMotionFinished = false;
        IsRunning = true;
        IsFinished = false;
        playbackVersion++;
    }

    private void MarkResponseMotionFinished(int version)
    {
        if (IsCurrentPlayback(version))
        {
            responseMotionFinished = true;
        }
    }

    private void MarkMuzzleFlashFinished(int version)
    {
        if (IsCurrentPlayback(version))
        {
            muzzleFlashFinished = true;
        }
    }

    private void MarkShotVisualFinished(int version)
    {
        if (IsCurrentPlayback(version))
        {
            shotVisualFinished = true;
        }
    }

    private void ReachVisualImpact(int version)
    {
        if (!IsCurrentPlayback(version) || visualImpactInvoked)
        {
            return;
        }

        visualImpactInvoked = true;
        Action trueImpactCallback = onTrueVisualImpact;
        Action callback = onVisualImpact;
        onTrueVisualImpact = null;
        onVisualImpact = null;
        trueImpactCallback?.Invoke();
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
        responseWinner?.ResetToStableIdlePresentation();
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

        if (responseEffectCoroutine != null)
        {
            StopCoroutine(responseEffectCoroutine);
        }
    }

    private void ClearPlaybackReferences()
    {
        playbackStage = PlaybackStage.None;
        shooter = null;
        meleeActor = null;
        hitTarget = null;
        responseWinner = null;
        hitTargetWorldRoot = null;
        onTrueVisualImpact = null;
        onVisualImpact = null;
        onFinished = null;
        playbackCoroutine = null;
        deflectionSlashCoroutine = null;
        hitReactionCoroutine = null;
        responseEffectCoroutine = null;
        muzzleFlashFinished = false;
        deflectionSlashFinished = false;
        hitReactionFinished = false;
        visualImpactInvoked = false;
        responseMotionFinished = false;
        playShotVisual = false;
        shotVisualFinished = false;
        specialShotHitProfile = null;
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
