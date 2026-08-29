using System;
using System.Collections;
using UnityEngine;

// 协调整回合结束后的表现等待，不参与战斗结算或下一回合数据准备。
public sealed class BattleTurnTransitionPresentationCoordinator : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip(
        "Legacy fallback：仅在找不到BattleCameraDirector时使用。"
    )]
    private float turnEndHoldDuration = 0.60f;
    [SerializeField] private BattleCameraDirector battleCameraDirector;

    private Coroutine turnEndPresentationCoroutine;
    private Action turnEndPresentationCompletion;
    private bool isPlaying;
    private int playbackVersion;

    public bool IsPlaying => isPlaying;
    public float TurnEndHoldDuration => Mathf.Max(0f, turnEndHoldDuration);

    public bool TryPlayTurnEndPresentation(Action completion)
    {
        if (!isActiveAndEnabled || IsPlaying)
        {
            return false;
        }

        int version = ++playbackVersion;
        isPlaying = true;
        turnEndPresentationCompletion = completion;
        LogDevelopment("[TurnTransition] TurnEnd Presentation Start");

        ResolveBattleCameraDirector();
        if (battleCameraDirector != null &&
            battleCameraDirector.TryPlayTurnEndTransition(
                () => CompletePresentation(
                    version,
                    "[TurnTransition] TurnEnd Presentation Complete"
                )
            ))
        {
            return true;
        }

        turnEndPresentationCoroutine = StartCoroutine(
            RunTurnEndPresentation(version)
        );
        return true;
    }

    public bool TryPlayNewTurnPresentation(Action completion)
    {
        if (!isActiveAndEnabled || IsPlaying)
        {
            return false;
        }

        int version = ++playbackVersion;
        isPlaying = true;
        turnEndPresentationCompletion = completion;
        LogDevelopment("[TurnTransition] NewTurn Presentation Start");

        ResolveBattleCameraDirector();
        if (battleCameraDirector != null)
        {
            if (battleCameraDirector.TryPlayNewTurnStart(
                    () => CompletePresentation(
                        version,
                        "[TurnTransition] NewTurn Presentation Complete"
                    )))
            {
                return true;
            }

            // Director存在却拒绝Opening时立即安全解锁，不制造可见Hold。
            battleCameraDirector.CancelTurnEndRecovery();
            isPlaying = false;
            turnEndPresentationCompletion = null;
            return false;
        }

        // 无Director的旧场景才保留原固定等待回退。
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

        battleCameraDirector?.CancelTurnEndRecovery();
        isPlaying = false;
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

        CompletePresentation(
            version,
            "[TurnTransition] Presentation Fallback Complete"
        );
    }

    private void CompletePresentation(int version, string logMessage)
    {
        if (version != playbackVersion)
        {
            return;
        }

        turnEndPresentationCoroutine = null;
        isPlaying = false;
        Action completion = turnEndPresentationCompletion;
        turnEndPresentationCompletion = null;
        LogDevelopment(logMessage);
        completion?.Invoke();
    }

    private void ResolveBattleCameraDirector()
    {
        if (battleCameraDirector == null)
        {
            battleCameraDirector =
                FindFirstObjectByType<BattleCameraDirector>();
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogDevelopment(string message)
    {
        Debug.Log(message);
    }
}
