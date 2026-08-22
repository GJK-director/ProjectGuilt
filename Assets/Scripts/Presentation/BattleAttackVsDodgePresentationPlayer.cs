using System;
using System.Collections;
using UnityEngine;

public enum BattleDodgePresentationResult
{
    DodgeSuccess,
    DodgeFailed
}

// 只协调DodgeVsAttack表现，不读取胜负规则或提交伤害。
public sealed class BattleAttackVsDodgePresentationPlayer : MonoBehaviour
{
    private enum PlaybackStage
    {
        None,
        ClashReadyApproach,
        DodgeResult
    }

    [SerializeField]
    private BattleAttackVsDodgePresentationProfile presentationProfile;

    public bool IsRunning { get; private set; }
    public bool IsFinished { get; private set; }

    private PlaybackStage playbackStage;
    private BattleCharacterPresentationController attacker;
    private BattleCharacterPresentationController defender;
    private BattleDodgePresentationResult presentationResult;
    private bool useCloseRangeShoot;
    private float attackDirectionSign = 1f;
    private Action onClashReadyFinished;
    private Action onRollResultReady;
    private Action onImpactFinished;
    private Action onActionTailFinished;
    private Coroutine playbackCoroutine;
    private Coroutine slashCoroutine;
    private Coroutine hitCoroutine;
    private bool slashFinished;
    private bool dodgeFinished;
    private bool rollResultReady;
    private bool impactStarted;
    private bool hitFinished;
    private int playbackVersion;

    void OnDisable()
    {
        CancelAndReset();
    }

    public bool TryPlayClashReadyApproach(
        BattleCharacterPresentationController dodgeSide,
        Transform dodgeWorldRoot,
        BattleCharacterPresentationController attackSide,
        Transform attackWorldRoot,
        BattleClashEngagementResult engagementResult,
        Action completion
    )
    {
        return TryPlayClashReadyApproach(
            dodgeSide,
            dodgeWorldRoot,
            attackSide,
            attackWorldRoot,
            engagementResult,
            false,
            completion
        );
    }

    public bool TryPlayClashReadyApproach(
        BattleCharacterPresentationController dodgeSide,
        Transform dodgeWorldRoot,
        BattleCharacterPresentationController attackSide,
        Transform attackWorldRoot,
        BattleClashEngagementResult engagementResult,
        bool useRealApproach,
        Action completion
    )
    {
        if (!CanStartPlayback() || dodgeSide == null || attackSide == null ||
            dodgeWorldRoot == null || attackWorldRoot == null ||
            engagementResult == null)
        {
            return false;
        }

        if (!useRealApproach)
        {
            // 旧路径保持原行为：Dodge保留当前Pose，Attack只进入Sprint等待Pose。
            attackSide.SetSprint();
            IsFinished = true;
            completion?.Invoke();
            return true;
        }

        if (!BattleClashEngagementResolver.RequiresApproach(
                dodgeWorldRoot.position,
                attackWorldRoot.position,
                engagementResult
            ))
        {
            // 已满足FinalGap时保留上一动作Pose与WorldRoot，不重复接敌。
            IsFinished = true;
            completion?.Invoke();
            return true;
        }

        playbackVersion++;
        playbackStage = PlaybackStage.ClashReadyApproach;
        attacker = attackSide;
        defender = dodgeSide;
        onClashReadyFinished = completion;
        IsRunning = true;
        IsFinished = false;
        playbackCoroutine = StartCoroutine(
            RunClashReadyApproach(
                playbackVersion,
                dodgeSide,
                dodgeWorldRoot,
                attackSide,
                attackWorldRoot,
                engagementResult
            )
        );
        return true;
    }

    private IEnumerator RunClashReadyApproach(
        int version,
        BattleCharacterPresentationController dodgeSide,
        Transform dodgeWorldRoot,
        BattleCharacterPresentationController attackSide,
        Transform attackWorldRoot,
        BattleClashEngagementResult engagementResult
    )
    {
        yield return BattleClashReadyApproachMotion.Play(
            dodgeSide,
            dodgeWorldRoot,
            attackSide,
            attackWorldRoot,
            engagementResult,
            presentationProfile.SprintDuration,
            presentationProfile.AfterimageSpawnInterval,
            () => IsCurrentPlayback(version)
        );

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        Action callback = onClashReadyFinished;
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = true;
        callback?.Invoke();
    }

    public bool TryPlayDodgeRollResult(
        BattleCharacterPresentationController attackActor,
        BattleCharacterPresentationController dodgeActor,
        float directionSign,
        BattleDodgePresentationResult result,
        Action rollResultCompletion,
        Action actionTailCompletion
    )
    {
        return TryPlayDodgeRollResult(
            attackActor,
            dodgeActor,
            directionSign,
            result,
            false,
            rollResultCompletion,
            actionTailCompletion
        );
    }

    public bool TryPlayDodgeRollResult(
        BattleCharacterPresentationController attackActor,
        BattleCharacterPresentationController dodgeActor,
        float directionSign,
        BattleDodgePresentationResult result,
        bool playCloseRangeShoot,
        Action rollResultCompletion,
        Action actionTailCompletion
    )
    {
        if (!CanStartPlayback() || attackActor == null || dodgeActor == null)
        {
            return false;
        }

        playbackVersion++;
        playbackStage = PlaybackStage.DodgeResult;
        attacker = attackActor;
        defender = dodgeActor;
        presentationResult = result;
        useCloseRangeShoot = playCloseRangeShoot;
        attackDirectionSign = directionSign >= 0f ? 1f : -1f;
        onRollResultReady = rollResultCompletion;
        onActionTailFinished = actionTailCompletion;
        slashFinished = false;
        dodgeFinished = false;
        rollResultReady = false;
        impactStarted = false;
        hitFinished = result == BattleDodgePresentationResult.DodgeSuccess;
        IsRunning = true;
        IsFinished = false;
        playbackCoroutine = StartCoroutine(RunDodgeResult(playbackVersion));
        return true;
    }

    public bool TryPlayDodgeFailedImpact(
        BattleCharacterPresentationController dodgeActor,
        Transform dodgeWorldRoot,
        float directionSign,
        Action completion
    )
    {
        if (!IsRunning || playbackStage != PlaybackStage.DodgeResult ||
            presentationResult != BattleDodgePresentationResult.DodgeFailed ||
            !rollResultReady || impactStarted || dodgeActor == null ||
            dodgeWorldRoot == null || !object.ReferenceEquals(defender, dodgeActor))
        {
            return false;
        }

        impactStarted = true;
        hitFinished = false;
        attackDirectionSign = directionSign >= 0f ? 1f : -1f;
        onImpactFinished = completion;
        hitCoroutine = StartCoroutine(
            RunFailedHit(playbackVersion, dodgeWorldRoot)
        );
        return true;
    }

    public void CancelAndReset()
    {
        playbackVersion++;
        StopOwnedCoroutines();
        ResetActorsToStableIdle();
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = false;
    }

    private IEnumerator RunDodgeResult(int version)
    {
        PrepareResultActors();
        if (useCloseRangeShoot)
        {
            attacker.SetCloseRangeShoot();
        }
        else
        {
            attacker.SetSlash();
        }
        defender.SetDodge();

        dodgeFinished = !defender.PlayDodgeMotion(
            attackDirectionSign,
            () => MarkDodgeFinished(version)
        );
        if (useCloseRangeShoot)
        {
            // Flash开始即为开枪接触点；完成回调则等价于原Slash结束边界。
            attacker.PlayCloseRangeMuzzleFlash(
                () => MarkCloseRangeAttackFinished(version)
            );
            if (presentationResult == BattleDodgePresentationResult.DodgeFailed)
            {
                ReachAttackVisualImpact(version);
            }
        }
        else
        {
            slashCoroutine = StartCoroutine(RunSlash(version));
        }

        while (IsCurrentPlayback(version))
        {
            if (presentationResult == BattleDodgePresentationResult.DodgeSuccess &&
                slashFinished && dodgeFinished)
            {
                FinishDodgeResult(version);
                yield break;
            }

            if (presentationResult == BattleDodgePresentationResult.DodgeFailed &&
                slashFinished && impactStarted && hitFinished)
            {
                FinishDodgeResult(version);
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator RunSlash(int version)
    {
        yield return attacker.PlaySlashPresentation(
            attackDirectionSign,
            () => ReachAttackVisualImpact(version)
        );

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        slashFinished = true;
        slashCoroutine = null;
        attacker.FinishSlashPresentation();
        if (!rollResultReady &&
            presentationResult == BattleDodgePresentationResult.DodgeFailed)
        {
            ReachAttackVisualImpact(version);
        }
    }

    private void MarkCloseRangeAttackFinished(int version)
    {
        if (IsCurrentPlayback(version))
        {
            // 沿用既有完成标记，避免改变Dodge状态机的收尾条件。
            slashFinished = true;
        }
    }

    private IEnumerator RunFailedHit(int version, Transform dodgeWorldRoot)
    {
        defender.FinishDodgePresentation();
        defender.SetHit();
        yield return defender.PlayHitReaction(
            dodgeWorldRoot,
            attackDirectionSign
        );

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        hitFinished = true;
        hitCoroutine = null;
        Action callback = onImpactFinished;
        onImpactFinished = null;
        callback?.Invoke();
    }

    private void ReachAttackVisualImpact(int version)
    {
        if (!IsCurrentPlayback(version) || rollResultReady ||
            presentationResult != BattleDodgePresentationResult.DodgeFailed)
        {
            return;
        }

        // Melee使用Slash接触点；CloseRange使用Flash开始时刻释放RollResult。
        defender.FinishDodgePresentation();
        dodgeFinished = true;
        rollResultReady = true;
        Action callback = onRollResultReady;
        onRollResultReady = null;
        callback?.Invoke();
    }

    private void MarkDodgeFinished(int version)
    {
        if (IsCurrentPlayback(version))
        {
            dodgeFinished = true;
        }
    }

    private void FinishDodgeResult(int version)
    {
        if (!IsCurrentPlayback(version))
        {
            return;
        }

        CleanupResultActorsPreservingPose();
        Action rollCallback = onRollResultReady;
        Action tailCallback = onActionTailFinished;
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = true;

        // DodgeSuccess没有Impact，完整尾段结束后才释放RollResult。
        rollCallback?.Invoke();
        tailCallback?.Invoke();
    }

    private void PrepareResultActors()
    {
        attacker.SetPresentationPaused(false);
        attacker.ClearSlashEffect();
        attacker.ClearBodyVisualOffsets();
        defender.SetPresentationPaused(false);
        defender.ClearSlashEffect();
        defender.ClearBodyVisualOffsets();
    }

    private void CleanupResultActorsPreservingPose()
    {
        if (attacker != null)
        {
            attacker.SetPresentationPaused(false);
            attacker.ClearSlashEffect();
            if (!useCloseRangeShoot)
            {
                attacker.FinishSlashPresentation();
            }
        }

        if (defender != null)
        {
            defender.SetPresentationPaused(false);
            defender.FinishDodgePresentation();
            if (presentationResult == BattleDodgePresentationResult.DodgeFailed)
            {
                defender.FinishHitReaction();
            }
        }
    }

    private void ResetActorsToStableIdle()
    {
        attacker?.ResetToStableIdlePresentation();
        defender?.ResetToStableIdlePresentation();
    }

    private void StopOwnedCoroutines()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
        }
        if (slashCoroutine != null)
        {
            StopCoroutine(slashCoroutine);
        }
        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
        }
    }

    private void ClearPlaybackReferences()
    {
        playbackStage = PlaybackStage.None;
        attacker = null;
        defender = null;
        onClashReadyFinished = null;
        onRollResultReady = null;
        onImpactFinished = null;
        onActionTailFinished = null;
        playbackCoroutine = null;
        slashCoroutine = null;
        hitCoroutine = null;
        slashFinished = false;
        dodgeFinished = false;
        rollResultReady = false;
        impactStarted = false;
        hitFinished = false;
        useCloseRangeShoot = false;
        attackDirectionSign = 1f;
    }

    private bool CanStartPlayback()
    {
        if (IsRunning || !isActiveAndEnabled)
        {
            return false;
        }

        if (presentationProfile != null)
        {
            return true;
        }

        Debug.LogError(
            "[AttackVsDodgePresentationPlayer] 播放失败：未配置表现Profile。",
            this
        );
        return false;
    }

    private bool IsCurrentPlayback(int version)
    {
        return IsRunning && playbackVersion == version;
    }
}
