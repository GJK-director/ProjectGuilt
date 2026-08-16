using System;
using System.Collections;
using UnityEngine;

// 协调普通攻击者与目标的局部表现，不读取或修改任何战斗规则数据。
public sealed class BattleDefaultAttackPresentationPlayer : MonoBehaviour
{
    [SerializeField] private float preAttackHoldDuration = 0.12f;
    [SerializeField] private float postImpactHoldDuration = 0.16f;
    [SerializeField] private float hitStopDuration = 0.08f;
    [SerializeField] private float minimumAttackPresentationDuration = 0.20f;

    public bool IsRunning { get; private set; }
    public bool IsFinished { get; private set; }

    private BattleCharacterPresentationController attacker;
    private BattleCharacterPresentationController target;
    private Action onVisualImpact;
    private Action onFinished;
    private Coroutine presentationCoroutine;
    private Coroutine slashCoroutine;
    private Coroutine hitReactionCoroutine;
    private Coroutine hitStopCoroutine;
    private bool slashFinished;
    private bool hitReactionFinished;
    private bool hitStopFinished;
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

        // 非显式取消的生命周期中断仍需释放Presenter等待边界。
        Action impactFallback = visualImpactInvoked ? null : onVisualImpact;
        Action finishedFallback = onFinished;
        playbackVersion++;
        StopOwnedCoroutines();
        onVisualImpact = null;
        onFinished = null;
        ResetCurrentActors();
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = true;
        impactFallback?.Invoke();
        finishedFallback?.Invoke();
    }

    public bool TryPlayDefaultAttack(
        BattleCharacterPresentationController attackController,
        BattleCharacterPresentationController targetController,
        float directionSign,
        Action visualImpactCallback,
        Action finishedCallback
    )
    {
        if (IsRunning || attackController == null || targetController == null)
        {
            return false;
        }

        attacker = attackController;
        target = targetController;
        attackDirectionSign = directionSign >= 0f ? 1f : -1f;
        onVisualImpact = visualImpactCallback;
        onFinished = finishedCallback;
        slashFinished = false;
        hitReactionFinished = true;
        hitStopFinished = true;
        visualImpactInvoked = false;
        IsRunning = true;
        IsFinished = false;
        playbackVersion++;

        int version = playbackVersion;
        presentationCoroutine = StartCoroutine(
            RunDefaultAttackPresentation(version)
        );
        return true;
    }

    public void CancelAndReset()
    {
        playbackVersion++;
        StopOwnedCoroutines();
        onVisualImpact = null;
        onFinished = null;
        ResetCurrentActors();
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = false;
    }

    private IEnumerator RunDefaultAttackPresentation(int version)
    {
        // Default Attack 的蓄势属于高层时序，不写入通用 Slash 动作。
        if (attacker != null)
        {
            attacker.SetIdle();
        }

        if (target != null)
        {
            target.SetIdle();
        }

        float preAttackElapsed = 0f;
        while (preAttackElapsed < Mathf.Max(0f, preAttackHoldDuration) &&
            IsCurrentPlayback(version))
        {
            preAttackElapsed += Time.deltaTime;
            yield return null;
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        float elapsed = 0f;
        float postImpactElapsed = 0f;
        if (attacker != null)
        {
            attacker.SetSlash();
            slashCoroutine = StartCoroutine(RunSlash(version));
        }
        else
        {
            slashFinished = true;
        }

        while (IsCurrentPlayback(version))
        {
            elapsed += Time.deltaTime;

            // Slash异常提前结束时也必须建立一次逻辑命中边界。
            if (slashFinished && !visualImpactInvoked)
            {
                ReachVisualImpact(version);
            }

            if (visualImpactInvoked)
            {
                postImpactElapsed += Time.deltaTime;
            }

            bool minimumDurationReached = elapsed >=
                Mathf.Max(0f, minimumAttackPresentationDuration);
            bool postImpactHoldReached = postImpactElapsed >=
                Mathf.Max(0f, postImpactHoldDuration);
            if (visualImpactInvoked && slashFinished &&
                hitReactionFinished && hitStopFinished &&
                minimumDurationReached && postImpactHoldReached)
            {
                break;
            }

            yield return null;
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        FinishNormally(version);
    }

    private IEnumerator RunSlash(int version)
    {
        if (attacker != null)
        {
            yield return attacker.PlaySlashPresentation(
                attackDirectionSign,
                () => ReachVisualImpact(version)
            );
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        slashFinished = true;
        slashCoroutine = null;
        if (!visualImpactInvoked)
        {
            ReachVisualImpact(version);
        }
    }

    private void ReachVisualImpact(int version)
    {
        if (!IsCurrentPlayback(version) || visualImpactInvoked)
        {
            return;
        }

        visualImpactInvoked = true;
        if (target != null)
        {
            target.SetHit();
            hitReactionFinished = false;
            hitReactionCoroutine = StartCoroutine(
                RunHitReaction(version)
            );
        }

        if (hitStopDuration > 0f && attacker != null && target != null)
        {
            attacker.SetPresentationPaused(true);
            target.SetPresentationPaused(true);
            hitStopFinished = false;
            hitStopCoroutine = StartCoroutine(RunHitStop(version));
        }

        Action callback = onVisualImpact;
        onVisualImpact = null;
        callback?.Invoke();
    }

    private IEnumerator RunHitReaction(int version)
    {
        if (target != null)
        {
            yield return target.PlaySustainedHitReaction(
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

    private IEnumerator RunHitStop(int version)
    {
        float elapsed = 0f;
        while (elapsed < hitStopDuration && IsCurrentPlayback(version))
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!IsCurrentPlayback(version))
        {
            yield break;
        }

        SetCurrentActorsPaused(false);
        hitStopFinished = true;
        hitStopCoroutine = null;
    }

    private void FinishNormally(int version)
    {
        if (!IsCurrentPlayback(version))
        {
            return;
        }

        ResetCurrentActors();
        Action callback = onFinished;
        onVisualImpact = null;
        onFinished = null;
        ClearPlaybackReferences();
        IsRunning = false;
        IsFinished = true;
        callback?.Invoke();
    }

    private void ResetCurrentActors()
    {
        SetCurrentActorsPaused(false);

        if (attacker != null)
        {
            attacker.ClearSlashEffect();
            attacker.FinishSlashPresentation();
        }

        if (target != null)
        {
            target.FinishHitReaction();
        }
    }

    private void SetCurrentActorsPaused(bool paused)
    {
        if (attacker != null)
        {
            attacker.SetPresentationPaused(paused);
        }

        if (target != null)
        {
            target.SetPresentationPaused(paused);
        }
    }

    private void StopOwnedCoroutines()
    {
        if (presentationCoroutine != null)
        {
            StopCoroutine(presentationCoroutine);
            presentationCoroutine = null;
        }

        if (slashCoroutine != null)
        {
            StopCoroutine(slashCoroutine);
            slashCoroutine = null;
        }

        if (hitReactionCoroutine != null)
        {
            StopCoroutine(hitReactionCoroutine);
            hitReactionCoroutine = null;
        }

        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
            hitStopCoroutine = null;
        }
    }

    private void ClearPlaybackReferences()
    {
        presentationCoroutine = null;
        slashCoroutine = null;
        hitReactionCoroutine = null;
        hitStopCoroutine = null;
        attacker = null;
        target = null;
    }

    private bool IsCurrentPlayback(int version)
    {
        return IsRunning && playbackVersion == version;
    }
}
