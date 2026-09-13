using UnityEngine;

// 角色目标选择的独立轮廓表现，不修改主体 SpriteRenderer 材质。
[DisallowMultipleComponent]
public sealed class BattleCharacterTargetOutline : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sourceSpriteRenderer;
    [SerializeField] private SpriteRenderer outlineSpriteRenderer;
    [SerializeField] private Color outlineColor =
        new Color(0.15f, 0.45f, 1f, 1f);
    [SerializeField, Min(1f)] private float outlineScale = 1.04f;

    private MaterialPropertyBlock propertyBlock;
    private Vector3 outlineBaseScale = Vector3.one;
    private bool baseScaleCached;
    private bool isVisible;
    private bool warnedMissingOutlineRenderer;

    public bool IsVisible => isVisible &&
        outlineSpriteRenderer != null &&
        outlineSpriteRenderer.enabled;

    private void Awake()
    {
        CacheReferences();
        SetVisible(false);
    }

    private void OnEnable()
    {
        CacheReferences();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (isVisible)
        {
            SyncOutline();
        }
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if (!visible)
        {
            if (outlineSpriteRenderer != null)
            {
                outlineSpriteRenderer.enabled = false;
            }
            return;
        }

        CacheReferences();
        if (outlineSpriteRenderer == null)
        {
            if (!warnedMissingOutlineRenderer)
            {
                warnedMissingOutlineRenderer = true;
                Debug.LogWarning(
                    "BattleCharacterTargetOutline 缺少显式 Outline SpriteRenderer，已安全关闭轮廓。",
                    this
                );
            }
            isVisible = false;
            return;
        }

        if (!CanRenderOutline())
        {
            isVisible = false;
            return;
        }

        SyncOutline();
        outlineSpriteRenderer.enabled = true;
    }

    private void CacheReferences()
    {
        if (sourceSpriteRenderer == null)
        {
            BattleCharacterPresentationController presentation =
                GetComponentInChildren<BattleCharacterPresentationController>(true);
            sourceSpriteRenderer = presentation != null
                ? presentation.CharacterSpriteRenderer
                : null;
        }

        if (outlineSpriteRenderer != null && !baseScaleCached)
        {
            outlineBaseScale = outlineSpriteRenderer.transform.localScale;
            baseScaleCached = true;
        }
    }

    private bool CanRenderOutline()
    {
        return sourceSpriteRenderer != null &&
            outlineSpriteRenderer != null &&
            sourceSpriteRenderer != outlineSpriteRenderer &&
            sourceSpriteRenderer.sprite != null;
    }

    private void SyncOutline()
    {
        if (!CanRenderOutline())
        {
            return;
        }

        outlineSpriteRenderer.sprite = sourceSpriteRenderer.sprite;
        outlineSpriteRenderer.flipX = sourceSpriteRenderer.flipX;
        outlineSpriteRenderer.flipY = sourceSpriteRenderer.flipY;
        outlineSpriteRenderer.sortingLayerID =
            sourceSpriteRenderer.sortingLayerID;
        outlineSpriteRenderer.sortingOrder =
            sourceSpriteRenderer.sortingOrder - 1;
        outlineSpriteRenderer.transform.localScale =
            outlineBaseScale * Mathf.Max(1f, outlineScale);

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        outlineSpriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_OutlineColor", outlineColor);
        outlineSpriteRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDisable()
    {
        isVisible = false;
        if (outlineSpriteRenderer != null)
        {
            outlineSpriteRenderer.enabled = false;
        }
    }
}
