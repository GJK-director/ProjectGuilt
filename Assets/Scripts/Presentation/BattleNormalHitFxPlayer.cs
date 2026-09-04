using System.Collections;
using UnityEngine;

public enum BattleNormalHitFxVariant
{
    WorldBurstA,
    FollowTargetB
}

public sealed class BattleNormalHitFxPlayer : MonoBehaviour
{
    private SpriteRenderer fxRenderer;
    private BattleHitPresentationProfile profile;

    public void Play(
        SpriteRenderer targetRenderer,
        BattleHitPresentationProfile hitProfile
    )
    {
        fxRenderer = targetRenderer;
        profile = hitProfile;
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        if (fxRenderer == null || profile == null)
        {
            Destroy(gameObject);
            yield break;
        }

        if (profile.HitFxVariant == BattleNormalHitFxVariant.FollowTargetB)
        {
            yield return PlayFollowTargetB();
        }
        else
        {
            yield return PlayWorldBurstA();
        }

        SetAlpha(0f);
        Destroy(gameObject);
    }

    private IEnumerator PlayWorldBurstA()
    {
        float duration = profile.HitFxADuration;
        float baseScale = profile.HitFxBaseScale;
        float startScale = baseScale * profile.HitFxAStartScale;
        float endScale = baseScale * profile.HitFxAEndScale;
        float holdRatio = profile.HitFxAHoldRatio;
        transform.localScale = Vector3.one * startScale;

        if (duration <= 0f)
        {
            transform.localScale = Vector3.one * endScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float easedScale = BattlePresentationEasing.EaseOutQuad(
                normalizedTime
            );
            transform.localScale = Vector3.one * Mathf.Lerp(
                startScale,
                endScale,
                easedScale
            );
            SetAlpha(EvaluateWorldBurstAlpha(normalizedTime, holdRatio));

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = Vector3.one * endScale;
    }

    private IEnumerator PlayFollowTargetB()
    {
        float duration = profile.HitFxBDuration;
        float holdRatio = profile.HitFxBHoldRatio;
        transform.localScale = Vector3.one * profile.HitFxBaseScale;

        if (duration <= 0f)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            SetAlpha(EvaluateFollowTargetAlpha(normalizedTime, holdRatio));

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (fxRenderer == null)
        {
            return;
        }

        fxRenderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
    }

    private static float EvaluateWorldBurstAlpha(
        float normalizedTime,
        float holdRatio
    )
    {
        if (normalizedTime <= holdRatio)
        {
            return 1f;
        }

        float fadeDuration = 1f - holdRatio;
        if (fadeDuration <= Mathf.Epsilon)
        {
            return 1f;
        }

        float fadeT = Mathf.Clamp01(
            (normalizedTime - holdRatio) / fadeDuration
        );
        return 1f - fadeT * fadeT;
    }

    private static float EvaluateFollowTargetAlpha(
        float normalizedTime,
        float holdRatio
    )
    {
        if (normalizedTime <= holdRatio)
        {
            return 1f;
        }

        float fadeDuration = 1f - holdRatio;
        if (fadeDuration <= Mathf.Epsilon)
        {
            return 1f;
        }

        float fadeT = Mathf.Clamp01(
            (normalizedTime - holdRatio) / fadeDuration
        );
        return 1f - fadeT;
    }
}
