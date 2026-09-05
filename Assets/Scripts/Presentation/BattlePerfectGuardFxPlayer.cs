using System;
using System.Collections;
using UnityEngine;

public sealed class BattlePerfectGuardFxPlayer : MonoBehaviour
{
    private BattleCharacterPresentationController target;
    private SpriteRenderer fxRenderer;
    private Action onFinished;
    private Func<bool> isPlaybackCurrent;
    private bool finished;

    public static bool TrySpawn(
        BattleAttackVsGuardPresentationProfile profile,
        BattleCharacterPresentationController target,
        float attackDirectionSign,
        Action completion = null,
        Func<bool> isPlaybackCurrent = null
    )
    {
        if (profile == null || target == null || !target.isActiveAndEnabled ||
            target.CharacterSpriteRenderer == null)
        {
            return false;
        }
        if (profile.PerfectGuardFxSprite == null)
        {
            Debug.LogWarning(
                "[BattlePerfectGuardFxPlayer] Perfect Guard FX skipped: Sprite is not assigned.",
                profile
            );
            return false;
        }

        float direction = attackDirectionSign >= 0f ? 1f : -1f;
        GameObject fxObject = new GameObject("PerfectGuardFx");
        fxObject.transform.SetParent(target.transform, false);
        fxObject.transform.localPosition = new Vector3(
            -direction * profile.PerfectGuardFxHorizontalOffset,
            profile.PerfectGuardFxVerticalOffset,
            0f
        );
        fxObject.transform.localRotation = Quaternion.identity;
        fxObject.transform.localScale = Vector3.one * profile.PerfectGuardFxBaseScale;

        SpriteRenderer renderer = fxObject.AddComponent<SpriteRenderer>();
        renderer.sprite = profile.PerfectGuardFxSprite;
        renderer.color = Color.white;
        renderer.flipX = direction > 0f;
        renderer.flipY = false;
        renderer.sortingLayerID = target.CharacterSpriteRenderer.sortingLayerID;
        renderer.sortingOrder = target.CharacterSpriteRenderer.sortingOrder +
            profile.PerfectGuardFxSortingOrderOffset;

        BattlePerfectGuardFxPlayer player = fxObject.AddComponent<BattlePerfectGuardFxPlayer>();
        player.target = target;
        player.fxRenderer = renderer;
        player.onFinished = completion;
        player.isPlaybackCurrent = isPlaybackCurrent;
        player.StartCoroutine(player.PlaySequence(profile));
        return true;
    }

    private IEnumerator PlaySequence(BattleAttackVsGuardPresentationProfile profile)
    {
        yield return PlayPhase(profile.PerfectGuardFxHoldDuration, false);
        yield return PlayPhase(profile.PerfectGuardFxFadeDuration, true);
        Finish();
    }

    private IEnumerator PlayPhase(float duration, bool fade)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (target == null || !target.isActiveAndEnabled || fxRenderer == null ||
                (isPlaybackCurrent != null && !isPlaybackCurrent()))
            {
                yield break;
            }
            if (target.IsPresentationPaused)
            {
                yield return null;
                continue;
            }

            elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
            if (fade)
            {
                float alpha = 1f - BattlePresentationEasing.EaseOutQuad(elapsed / duration);
                fxRenderer.color = new Color(1f, 1f, 1f, alpha);
            }
            yield return null;
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        Finish();
    }

    private void Finish()
    {
        if (finished)
        {
            return;
        }
        finished = true;
        if (fxRenderer != null)
        {
            fxRenderer.enabled = false;
        }
        Action callback = onFinished;
        onFinished = null;
        isPlaybackCurrent = null;
        Destroy(gameObject);
        callback?.Invoke();
    }
}
