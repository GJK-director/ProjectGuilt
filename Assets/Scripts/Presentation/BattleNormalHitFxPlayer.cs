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

    public static bool TrySpawn(
        BattleHitPresentationProfile hitProfile,
        BattleCharacterPresentationController hitTarget,
        Transform hitTargetWorldRoot,
        float attackDirectionSign
    )
    {
        SpriteRenderer targetRenderer = hitTarget != null
            ? hitTarget.CharacterSpriteRenderer
            : null;
        if (hitProfile == null || targetRenderer == null ||
            hitTargetWorldRoot == null)
        {
            return false;
        }

        if (hitProfile.HitFxSprite == null)
        {
            Debug.LogWarning(
                "[BattleNormalHitFxPlayer] Normal Hit FX skipped: " +
                "HitFxSprite is not assigned.",
                hitProfile
            );
            return false;
        }

        float direction = attackDirectionSign >= 0f ? 1f : -1f;
        Vector3 hitPosition = targetRenderer.bounds.center +
            Vector3.right *
                (-direction * hitProfile.HitFxHorizontalOffset) +
            Vector3.up * hitProfile.HitFxVerticalOffset;
        GameObject fxObject = new GameObject("NormalHitFx");
        fxObject.transform.position = hitPosition;
        fxObject.transform.rotation = Quaternion.identity;

        SpriteRenderer spawnedRenderer =
            fxObject.AddComponent<SpriteRenderer>();
        spawnedRenderer.sprite = hitProfile.HitFxSprite;
        spawnedRenderer.color = Color.white;
        spawnedRenderer.flipX = false;
        spawnedRenderer.flipY = false;
        spawnedRenderer.sortingLayerID = targetRenderer.sortingLayerID;
        spawnedRenderer.sortingOrder = targetRenderer.sortingOrder + 10;

        if (hitProfile.HitFxVariant ==
            BattleNormalHitFxVariant.FollowTargetB)
        {
            fxObject.transform.SetParent(hitTargetWorldRoot, true);
        }

        BattleNormalHitFxPlayer fxPlayer =
            fxObject.AddComponent<BattleNormalHitFxPlayer>();
        fxPlayer.Play(spawnedRenderer, hitProfile);
        return true;
    }

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
        float baseScale = profile.HitFxBaseScale;
        float startScale = baseScale * profile.HitFxAStartScale;
        float endScale = baseScale * profile.HitFxAEndScale;
        float holdDuration = profile.HitFxHoldDuration;
        float expandDuration = Mathf.Min(
            profile.HitFxAExpandDuration,
            holdDuration
        );
        transform.localScale = Vector3.one * startScale;
        SetAlpha(1f);

        float elapsed = 0f;
        while (elapsed < expandDuration)
        {
            float normalizedExpand = expandDuration > Mathf.Epsilon
                ? elapsed / expandDuration
                : 1f;
            float easedScale = BattlePresentationEasing.EaseOutQuad(
                normalizedExpand
            );
            transform.localScale = Vector3.one * Mathf.Lerp(
                startScale,
                endScale,
                easedScale
            );
            yield return null;
            elapsed = Mathf.Min(
                expandDuration,
                elapsed + Time.deltaTime
            );
        }

        transform.localScale = Vector3.one * endScale;
        while (elapsed < holdDuration)
        {
            yield return null;
            elapsed = Mathf.Min(
                holdDuration,
                elapsed + Time.deltaTime
            );
        }

        float fadeDuration = profile.HitFxAFadeDuration;
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            SetAlpha(1f - fadeElapsed / fadeDuration);
            yield return null;
            fadeElapsed = Mathf.Min(
                fadeDuration,
                fadeElapsed + Time.deltaTime
            );
        }
    }

    private IEnumerator PlayFollowTargetB()
    {
        transform.localScale = Vector3.one * profile.HitFxBaseScale;
        SetAlpha(1f);

        float holdElapsed = 0f;
        float holdDuration = profile.HitFxHoldDuration;
        while (holdElapsed < holdDuration)
        {
            yield return null;
            holdElapsed = Mathf.Min(
                holdDuration,
                holdElapsed + Time.deltaTime
            );
        }

        float fadeElapsed = 0f;
        float fadeDuration = profile.HitFxBFadeDuration;
        while (fadeElapsed < fadeDuration)
        {
            SetAlpha(1f - fadeElapsed / fadeDuration);
            yield return null;
            fadeElapsed = Mathf.Min(
                fadeDuration,
                fadeElapsed + Time.deltaTime
            );
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

}
