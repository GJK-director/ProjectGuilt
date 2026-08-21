using System;
using System.Collections;
using UnityEngine;

// 三种Clash共享的接敌位移能力；不拥有规则结果或后续动作时序。
public static class BattleClashReadyApproachMotion
{
    public static IEnumerator Play(
        BattleCharacterPresentationController sideA,
        Transform sideAWorldRoot,
        BattleCharacterPresentationController sideB,
        Transform sideBWorldRoot,
        BattleClashEngagementResult engagementResult,
        float sprintDuration,
        float afterimageSpawnInterval,
        Func<bool> shouldContinue
    )
    {
        if (!HasValidActors(
                sideA,
                sideAWorldRoot,
                sideB,
                sideBWorldRoot
            ) || engagementResult == null ||
            !CanContinue(shouldContinue))
        {
            yield break;
        }

        Vector3 sideAStart = sideAWorldRoot.position;
        Vector3 sideBStart = sideBWorldRoot.position;
        if (!BattleClashEngagementResolver.RequiresApproach(
                sideAStart,
                sideBStart,
                engagementResult
            ))
        {
            yield break;
        }

        float horizontalDelta = sideBStart.x - sideAStart.x;
        float directionSign = Mathf.Abs(horizontalDelta) > 0.0001f
            ? Mathf.Sign(horizontalDelta)
            : 1f;
        float horizontalDistance = Mathf.Abs(horizontalDelta);
        float closeDistance = horizontalDistance -
            Mathf.Max(0f, engagementResult.FinalGap);

        Vector3 sideATarget = sideAStart;
        Vector3 sideBTarget = sideBStart;
        sideATarget.x += directionSign * closeDistance *
            engagementResult.SideAMovementShare;
        sideBTarget.x -= directionSign * closeDistance *
            engagementResult.SideBMovementShare;

        sideA.SetSprint();
        sideB.SetSprint();

        float safeDuration = Mathf.Max(0f, sprintDuration);
        if (safeDuration <= 0f)
        {
            sideAWorldRoot.position = sideATarget;
            sideBWorldRoot.position = sideBTarget;
            yield break;
        }

        sideA.SpawnAfterimage();
        sideB.SpawnAfterimage();

        float elapsed = 0f;
        float afterimageElapsed = 0f;
        float safeAfterimageInterval = Mathf.Max(0f, afterimageSpawnInterval);
        bool spawnRepeatedAfterimages = safeAfterimageInterval > 0f;
        while (elapsed < safeDuration && CanContinue(shouldContinue))
        {
            if (!HasValidActors(
                    sideA,
                    sideAWorldRoot,
                    sideB,
                    sideBWorldRoot
                ))
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float linearT = Mathf.Clamp01(elapsed / safeDuration);
            float easedT = BattlePresentationEasing.EaseOutQuad(linearT);
            sideAWorldRoot.position = sideAStart +
                (sideATarget - sideAStart) * easedT;
            sideBWorldRoot.position = sideBStart +
                (sideBTarget - sideBStart) * easedT;

            if (spawnRepeatedAfterimages)
            {
                afterimageElapsed += Time.deltaTime;
                if (afterimageElapsed >= safeAfterimageInterval &&
                    elapsed < safeDuration)
                {
                    sideA.SpawnAfterimage();
                    sideB.SpawnAfterimage();
                    afterimageElapsed = 0f;
                }
            }

            yield return null;
        }

        if (CanContinue(shouldContinue) && HasValidActors(
                sideA,
                sideAWorldRoot,
                sideB,
                sideBWorldRoot
            ))
        {
            // 接敌只改变X，最终精确吸附到双方共享FinalGap。
            sideAWorldRoot.position = sideATarget;
            sideBWorldRoot.position = sideBTarget;
        }
    }

    private static bool HasValidActors(
        BattleCharacterPresentationController sideA,
        Transform sideAWorldRoot,
        BattleCharacterPresentationController sideB,
        Transform sideBWorldRoot
    )
    {
        return sideA != null && sideAWorldRoot != null &&
            sideB != null && sideBWorldRoot != null;
    }

    private static bool CanContinue(Func<bool> shouldContinue)
    {
        return shouldContinue == null || shouldContinue();
    }
}
