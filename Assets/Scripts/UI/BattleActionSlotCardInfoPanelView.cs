using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 行动槽位卡牌详情的单侧视图。布局和文字引用全部序列化，
// 可直接在 BattleActionSlotCardInfoPanel 预设体中调整。
public sealed class BattleActionSlotCardInfoPanelView : MonoBehaviour
{
    [Header("可编辑 UI 引用")]
    [SerializeField] Image accentImage;
    [SerializeField] Image artworkImage;
    [SerializeField] BattleCardUIView cardPreviewPrefab;
    [SerializeField] TMP_Text sideLabelText;
    [SerializeField] TMP_Text ownerText;
    [SerializeField] TMP_Text cardNameText;
    [SerializeField] TMP_Text pointText;
    [SerializeField] TMP_Text typeAndCooldownText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] TMP_Text stateText;
    [SerializeField] TMP_Text keywordText;

    [Header("阵营配色")]
    [SerializeField] Color allyAccentColor =
        new Color32(83, 183, 127, 255);
    [SerializeField] Color enemyAccentColor =
        new Color32(174, 87, 215, 255);

    RectTransform cardPreviewRect;
    BattleCardUIView cardPreviewView;

    internal void ShowCard(
        CharacterData owner,
        CharacterData target,
        BattleCardState cardState,
        bool enemySide
    )
    {
        if (cardState == null)
        {
            gameObject.SetActive(false);
            return;
        }

        BattleCardUIPreviewData preview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);

        EnsureCardPreview();
        if (cardPreviewView != null)
        {
            cardPreviewView.BindCard(owner, cardState, preview, null);
            cardPreviewView.SetSelected(false);
            cardPreviewView.gameObject.SetActive(true);
        }

        SetText(sideLabelText, enemySide ? "敌方意图" : "我方行动");
        SetText(
            ownerText,
            owner != null ? owner.characterName : "未知角色"
        );
        SetText(cardNameText, preview.cardName);
        SetText(pointText, "点数  " + preview.pointText);
        SetText(
            typeAndCooldownText,
            "类型  " + preview.typeText +
            "    基础 CD  " + preview.cooldownText
        );
        SetText(
            descriptionText,
            string.IsNullOrWhiteSpace(preview.descriptionText)
                ? "暂无卡牌说明。"
                : preview.descriptionText
        );
        SetText(stateText, BuildStateText(cardState));
        SetText(keywordText, BuildKeywordText(preview.keywords));

        if (accentImage != null)
        {
            accentImage.color = enemySide
                ? enemyAccentColor
                : allyAccentColor;
        }

        if (artworkImage != null)
        {
            artworkImage.color = enemySide
                ? new Color32(76, 47, 93, 255)
                : new Color32(41, 78, 61, 255);
        }

        gameObject.SetActive(true);
        RefreshCardPreviewLayout();
    }

    internal void Hide()
    {
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        RefreshCardPreviewLayout();
    }

    void OnRectTransformDimensionsChange()
    {
        RefreshCardPreviewLayout();
    }

    void RefreshCardPreviewLayout()
    {
        if (artworkImage == null || cardPreviewRect == null)
        {
            return;
        }

        float viewportWidth = Mathf.Max(
            0f,
            artworkImage.rectTransform.rect.width - 20f
        );
        float viewportHeight = Mathf.Max(
            0f,
            artworkImage.rectTransform.rect.height - 20f
        );
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

    void EnsureCardPreview()
    {
        if (cardPreviewView != null ||
            cardPreviewPrefab == null ||
            artworkImage == null)
        {
            return;
        }

        cardPreviewView = Instantiate(
            cardPreviewPrefab,
            artworkImage.rectTransform
        );
        cardPreviewView.name = "CardPreview";
        cardPreviewRect =
            cardPreviewView.transform as RectTransform;

        BattleCardMotionUIView cardMotion =
            cardPreviewView.GetComponent<BattleCardMotionUIView>();
        if (cardMotion != null)
        {
            cardMotion.enabled = false;
        }

        GraphicRaycaster cardRaycaster =
            cardPreviewView.GetComponent<GraphicRaycaster>();
        if (cardRaycaster != null)
        {
            cardRaycaster.enabled = false;
        }

        CanvasGroup cardCanvasGroup =
            cardPreviewView.GetComponent<CanvasGroup>();
        if (cardCanvasGroup != null)
        {
            cardCanvasGroup.interactable = false;
            cardCanvasGroup.blocksRaycasts = false;
        }

        cardPreviewView.gameObject.SetActive(true);
        RefreshCardPreviewLayout();
    }

    static string BuildStateText(BattleCardState cardState)
    {
        if (cardState == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        if (cardState.isConsumed)
        {
            builder.Append("当前状态：已消耗");
        }
        else if (cardState.currentCooldown > 0)
        {
            builder.Append("当前剩余 CD：");
            builder.Append(cardState.currentCooldown);
        }
        else
        {
            builder.Append("当前状态：可行动");
        }

        if (cardState.maxUseCount > 0)
        {
            builder.Append("    使用次数：");
            builder.Append(cardState.currentUseCount);
            builder.Append('/');
            builder.Append(cardState.maxUseCount);
        }

        return builder.ToString();
    }

    static string BuildKeywordText(CardKeywordData[] keywords)
    {
        if (keywords == null || keywords.Length == 0)
        {
            return "关键词：无";
        }

        StringBuilder builder = new StringBuilder("关键词：");
        bool hasKeyword = false;
        for (int index = 0; index < keywords.Length; index++)
        {
            CardKeywordData keyword = keywords[index];
            if (keyword == null ||
                string.IsNullOrWhiteSpace(keyword.displayName))
            {
                continue;
            }

            if (hasKeyword)
            {
                builder.Append(" / ");
            }

            builder.Append(keyword.displayName);
            hasKeyword = true;
        }

        return hasKeyword ? builder.ToString() : "关键词：无";
    }

    static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value ?? string.Empty;
        }
    }
}
