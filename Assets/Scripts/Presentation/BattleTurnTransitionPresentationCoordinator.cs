using System;
using System.Collections;
using UnityEngine;

// 协调整回合结束后的表现等待，不参与战斗结算或下一回合数据准备。
public sealed class BattleTurnTransitionPresentationCoordinator : MonoBehaviour
{
    [SerializeField, Min(0f)] private float turnEndHoldDuration = 0.60f;

    private Coroutine turnEndPresentationCoroutine;
    private Action turnEndPresentationCompletion;
    private int playbackVersion;

    public bool IsPlaying => turnEndPresentationCoroutine != null;
    public float TurnEndHoldDuration => Mathf.Max(0f, turnEndHoldDuration);

    public bool TryPlayTurnEndPresentation(Action completion)
    {
        if (!isActiveAndEnabled || IsPlaying)
        {
            return false;
        }

        int version = ++playbackVersion;
        turnEndPresentationCompletion = completion;
        LogDevelopment("[TurnTransition] TurnEnd Presentation Start");
        turnEndPresentationCoroutine = StartCoroutine(
            RunTurnEndPresentation(version)
        );
        return true;
    }

    public void CancelTurnEndPresentation()
    {
        playbackVersion++;
        if (turnEndPresentationCoroutine != null)
        {
            StopCoroutine(turnEndPresentationCoroutine);
            turnEndPresentationCoroutine = null;
        }

        turnEndPresentationCompletion = null;
    }

    private void OnDisable()
    {
        // Scene Reload或对象禁用时不得让旧回调推进新场景的Lifecycle。
        CancelTurnEndPresentation();
    }

    private IEnumerator RunTurnEndPresentation(int version)
    {
        float duration = TurnEndHoldDuration;
        float elapsed = 0f;
        if (duration <= 0f)
        {
            // 零时长仍跨一帧完成，避免同步回调重入上层Lifecycle。
            yield return null;
        }

        while (elapsed < duration && version == playbackVersion)
        {
            elapsed = Mathf.Min(
                duration,
                elapsed + Time.unscaledDeltaTime
            );
            yield return null;
        }

        if (version != playbackVersion)
        {
            yield break;
        }

        turnEndPresentationCoroutine = null;
        Action completion = turnEndPresentationCompletion;
        turnEndPresentationCompletion = null;
        LogDevelopment("[TurnTransition] TurnEnd Presentation Complete");
        completion?.Invoke();
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogDevelopment(string message)
    {
        Debug.Log(message);
    }
}
