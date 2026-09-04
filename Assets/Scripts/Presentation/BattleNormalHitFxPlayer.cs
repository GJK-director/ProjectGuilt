using System.Collections;
using UnityEngine;

public sealed class BattleNormalHitFxPlayer : MonoBehaviour
{
    private SpriteRenderer fxRenderer;
    private BattleHitPresentationProfile profile;

    public static bool TrySpawn(
        BattleHitPresentationProfile hitProfile,
        Sprite hitFxSprite,
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

        if (hitFxSprite == null)
        {
            Debug.LogWarning(
                "[BattleNormalHitFxPlayer] Normal Hit FX skipped: " +
                "the requested Hit FX Sprite is not assigned.",
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
        spawnedRenderer.sprite = hitFxSprite;
        spawnedRenderer.color = Color.white;
        spawnedRenderer.flipX = false;
        spawnedRenderer.flipY = false;
        spawnedRenderer.sortingLayerID = targetRenderer.sortingLayerID;
        spawnedRenderer.sortingOrder = targetRenderer.sortingOrder + 10;

        fxObject.transform.SetParent(hitTargetWorldRoot, true);

        BattleNormalHitFxPlayer fxPlayer =
            fxObject.AddComponent<BattleNormalHitFxPlayer>();
        fxPlayer.Play(spawnedRenderer, hitProfile);
        return true;
    }

    public static bool TrySpawn(
        BattleHitPresentationProfile hitProfile,
        BattleCharacterPresentationController hitTarget,
        Transform hitTargetWorldRoot,
        float attackDirectionSign
    )
    {
        return TrySpawn(
            hitProfile,
            hitProfile != null ? hitProfile.MeleeHitFxSprite : null,
            hitTarget,
            hitTargetWorldRoot,
            attackDirectionSign
        );
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
        float fadeDuration = profile.HitFxFadeDuration;
        while (fadeElapsed < fadeDuration)
        {
            SetAlpha(1f - fadeElapsed / fadeDuration);
            yield return null;
            fadeElapsed = Mathf.Min(
                fadeDuration,
                fadeElapsed + Time.deltaTime
            );
        }

        SetAlpha(0f);
        Destroy(gameObject);
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
