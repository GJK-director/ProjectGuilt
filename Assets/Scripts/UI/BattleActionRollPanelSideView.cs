using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 行动结算 Roll 面板的单侧内容：复用正式卡面，并显示本次有效区间与实际点数。
public sealed class BattleActionRollPanelSideView : MonoBehaviour
{
    const string AttackPointStat = "AttackPoint";

    [Header("卡牌预览")]
    [SerializeField] RectTransform cardViewport;
    [SerializeField] BattleCardUIView cardPreviewPrefab;

    [Header("Roll 点数 UI")]
    [SerializeField] Image rollFrameImage;
    [SerializeField] Image rangeBadgeImage;
    [SerializeField] TMP_Text rangeText;
    [SerializeField] TMP_Text rolledPointText;

    [Header("可替换字体与图片资源")]
    [SerializeField] Sprite rollFrameSprite;
    [SerializeField] Sprite rangeBadgeSprite;
    [SerializeField] TMP_FontAsset rangeFont;
    [SerializeField] TMP_FontAsset rolledPointFont;

    BattleCardUIView cardPreviewView;
    RectTransform cardPreviewRect;

    internal bool ShowPending(
        BattleClashSideState side,
        CharacterData target
    )
    {
        return Show(side, target, false, 0);
    }

    internal bool ShowRoll(
        BattleClashSideState side,
        CharacterData target,
        int rolledPoint
    )
    {
        return Show(side, target, true, rolledPoint);
    }

    bool Show(
        BattleClashSideState side,
        CharacterData target,
        bool hasRolledPoint,
        int rolledPoint
    )
    {
        if (side == null || side.cardState == null ||
            side.cardState.cardData == null)
        {
            Hide();
            return false;
        }

        ApplyReplaceableResources();
        EnsureCardPreview();
        if (cardPreviewView == null)
        {
            Hide();
            return false;
        }

        BattleCardUIPreviewData preview = BattleCardUIPreviewBuilder.Build(
            side.actor,
            target,
            side.cardState
        );
        cardPreviewView.BindCard(side.actor, side.cardState, preview, null);
        cardPreviewView.SetSelected(false);
        cardPreviewView.gameObject.SetActive(true);

        GetEffectivePointRange(side, out int minPoint, out int maxPoint);
        if (rangeText != null)
        {
            rangeText.text = minPoint + "~" + maxPoint;
        }
        if (rolledPointText != null)
        {
            rolledPointText.text = hasRolledPoint
                ? rolledPoint.ToString()
                : "—";
        }

        gameObject.SetActive(true);
        RefreshCardPreviewLayout();
        return true;
    }

    internal void Hide()
    {
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        ApplyReplaceableResources();
        RefreshCardPreviewLayout();
    }

    void OnRectTransformDimensionsChange()
    {
        RefreshCardPreviewLayout();
    }

    void ApplyReplaceableResources()
    {
        if (rollFrameImage != null && rollFrameSprite != null)
        {
            rollFrameImage.sprite = rollFrameSprite;
        }
        if (rangeBadgeImage != null && rangeBadgeSprite != null)
        {
            rangeBadgeImage.sprite = rangeBadgeSprite;
        }
        if (rangeText != null && rangeFont != null)
        {
            rangeText.font = rangeFont;
        }
        if (rolledPointText != null && rolledPointFont != null)
        {
            rolledPointText.font = rolledPointFont;
        }
    }

    void EnsureCardPreview()
    {
        if (cardPreviewView != null || cardPreviewPrefab == null ||
            cardViewport == null)
        {
            return;
        }

        cardPreviewView = Instantiate(cardPreviewPrefab, cardViewport);
        cardPreviewView.name = "ActionCardPreview";
        cardPreviewRect = cardPreviewView.transform as RectTransform;

        BattleCardMotionUIView motionView =
            cardPreviewView.GetComponent<BattleCardMotionUIView>();
        if (motionView != null)
        {
            motionView.enabled = false;
        }

        GraphicRaycaster raycaster =
            cardPreviewView.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = false;
        }

        CanvasGroup cardCanvasGroup =
            cardPreviewView.GetComponent<CanvasGroup>();
        if (cardCanvasGroup != null)
        {
            cardCanvasGroup.interactable = false;
            cardCanvasGroup.blocksRaycasts = false;
        }

        Graphic[] graphics =
            cardPreviewView.GetComponentsInChildren<Graphic>(true);
        for (int index = 0; index < graphics.Length; index++)
        {
            graphics[index].raycastTarget = false;
        }

        RefreshCardPreviewLayout();
    }

    void RefreshCardPreviewLayout()
    {
        if (cardViewport == null || cardPreviewRect == null)
        {
            return;
        }

        float viewportWidth = Mathf.Max(1f, cardViewport.rect.width);
        float viewportHeight = Mathf.Max(1f, cardViewport.rect.height);
        float cardWidth = Mathf.Max(1f, cardPreviewRect.sizeDelta.x);
        float cardHeight = Mathf.Max(1f, cardPreviewRect.sizeDelta.y);
        float scale = Mathf.Min(
            viewportWidth / cardWidth,
            viewportHeight / cardHeight
        );

        cardPreviewRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardPreviewRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardPreviewRect.pivot = new Vector2(0.5f, 0.5f);
        cardPreviewRect.anchoredPosition = Vector2.zero;
        cardPreviewRect.localRotation = Quaternion.identity;
        cardPreviewRect.localScale = Vector3.one * Mathf.Max(0f, scale);
    }

    static void GetEffectivePointRange(
        BattleClashSideState side,
        out int minPoint,
        out int maxPoint
    )
    {
        CardTestData cardData = side.cardState.cardData;
        BattleClashResourceSnapshot resource = side.resourceSnapshot;
        BattleClashPointSnapshot pointSnapshot = side.pointSnapshot;

        int selectedMin = resource != null
            ? resource.selectedMinPoint
            : cardData.minPoint;
        int selectedMax = resource != null
            ? resource.selectedMaxPoint
            : cardData.maxPoint;
        if (selectedMin > selectedMax)
        {
            int swap = selectedMin;
            selectedMin = selectedMax;
            selectedMax = swap;
        }

        int modifier = resource != null
            ? resource.pointModifierFromResource
            : 0;
        if (pointSnapshot != null)
        {
            modifier += pointSnapshot.nextCardPointModifier;
            if (cardData.isClashable)
            {
                modifier += pointSnapshot.nextClashPointModifier;
            }
        }
        if (side.actor != null && cardData.cardType == CardType.Attack)
        {
            modifier += Mathf.RoundToInt(
                side.actor.GetBuffFlatModifier(AttackPointStat)
            );
        }

        minPoint = Mathf.Max(0, selectedMin + modifier);
        maxPoint = Mathf.Max(0, selectedMax + modifier);
    }
}
