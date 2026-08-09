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
    }

    internal void Hide()
    {
        gameObject.SetActive(false);
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
