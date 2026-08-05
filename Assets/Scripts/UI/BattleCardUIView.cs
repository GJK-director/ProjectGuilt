using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class BattleCardUIView : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerMoveHandler,
    IPointerClickHandler
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text pointText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private BattleCardVisualStyle visualStyle;
    [SerializeField] private BattleCardMotionUIView motionView;

    private CharacterData boundOwner;
    private BattleCardState boundCardState;
    private BattleCardSelectionController selectionController;
    private bool warnedMissingVisualStyle;
    private readonly Dictionary<string, CardKeywordData>
        keywordByLinkID =
            new Dictionary<string, CardKeywordData>();
    private string activeKeywordLinkID;

    public CharacterData BoundOwner => boundOwner;
    public BattleCardState BoundCardState => boundCardState;
    public bool IsSelected =>
        selectionController != null &&
        selectionController.IsSelected(this);
    public bool CanSelect =>
        boundOwner != null &&
        boundOwner.battleCards != null &&
        boundCardState != null &&
        boundCardState.cardData != null &&
        boundCardState.currentCooldown <= 0 &&
        boundOwner.battleCards.Contains(boundCardState);

    void Awake()
    {
        if (motionView == null)
        {
            motionView = GetComponent<BattleCardMotionUIView>();
        }

        motionView?.EnsureInitialized();
        HideLegacyCooldown();
    }

    public void BindCard(
        CharacterData owner,
        BattleCardState cardState,
        BattleCardUIPreviewData data,
        BattleCardSelectionController cardSelectionController = null
    )
    {
        boundOwner = owner;
        boundCardState = cardState;
        selectionController = cardSelectionController;
        motionView?.EnsureInitialized();
        SetCard(data);
    }

    public void SetCard(BattleCardUIPreviewData data)
    {
        if (data == null)
        {
            SetEmpty();
            return;
        }

        SetText(cardNameText, data.cardName);
        SetText(pointText, data.pointText);
        SetText(typeText, data.typeText);
        SetKeywordDescription(data.descriptionText, data.keywords);
        HideLegacyCooldown();

        if (visualStyle != null)
        {
            visualStyle.Apply(data, typeText);
        }
        else if (!warnedMissingVisualStyle)
        {
            Debug.LogWarning(
                "BattleCardUIView 缺少 BattleCardVisualStyle，已保留基础文字显示。",
                this
            );
            warnedMissingVisualStyle = true;
        }
    }

    public void SetEmpty()
    {
        selectionController?.ClearSelectionIfSelected(this);
        boundOwner = null;
        boundCardState = null;
        selectionController = null;
        ClearKeywordHover();
        keywordByLinkID.Clear();
        SetText(cardNameText, "空");
        SetText(pointText, "—");
        SetText(typeText, "");
        SetText(descriptionText, "");
        HideLegacyCooldown();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        motionView?.SetHovered(true);
        RefreshKeywordHover(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        motionView?.SetHovered(false);
        ClearKeywordHover();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        RefreshKeywordHover(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null ||
            eventData.button != PointerEventData.InputButton.Left ||
            selectionController == null)
        {
            return;
        }

        selectionController.ToggleCardSelection(this);
    }

    public void SetSelected(bool selected)
    {
        motionView?.SetSelected(selected);
    }

    void OnDisable()
    {
        // Motion 组件独立负责视觉生命周期，这里只清理全局选择引用。
        selectionController?.ClearSelectionIfSelected(this);
        ClearKeywordHover();
    }

    void SetKeywordDescription(
        string description,
        CardKeywordData[] keywords
    )
    {
        ClearKeywordHover();
        keywordByLinkID.Clear();

        if (descriptionText == null)
        {
            return;
        }

        descriptionText.richText = true;
        descriptionText.text = BuildKeywordRichText(
            description ?? string.Empty,
            keywords
        );
    }

    string BuildKeywordRichText(
        string description,
        CardKeywordData[] keywords
    )
    {
        if (string.IsNullOrEmpty(description) ||
            keywords == null ||
            keywords.Length == 0)
        {
            return description;
        }

        StringBuilder builder = new StringBuilder();
        int textIndex = 0;
        while (textIndex < description.Length)
        {
            int matchedKeywordIndex = -1;
            int matchedLength = 0;

            for (int keywordIndex = 0;
                keywordIndex < keywords.Length;
                keywordIndex++)
            {
                CardKeywordData keyword = keywords[keywordIndex];
                if (keyword == null ||
                    string.IsNullOrEmpty(keyword.displayName) ||
                    string.IsNullOrEmpty(keyword.tooltipText) ||
                    keyword.displayName.Length <= matchedLength ||
                    textIndex + keyword.displayName.Length >
                        description.Length)
                {
                    continue;
                }

                if (string.CompareOrdinal(
                    description,
                    textIndex,
                    keyword.displayName,
                    0,
                    keyword.displayName.Length
                ) == 0)
                {
                    matchedKeywordIndex = keywordIndex;
                    matchedLength = keyword.displayName.Length;
                }
            }

            if (matchedKeywordIndex < 0)
            {
                builder.Append(description[textIndex]);
                textIndex++;
                continue;
            }

            CardKeywordData matchedKeyword =
                keywords[matchedKeywordIndex];
            string linkID =
                "battle-card-keyword-" + matchedKeywordIndex;
            keywordByLinkID[linkID] = matchedKeyword;

            builder.Append("<link=\"");
            builder.Append(linkID);
            builder.Append("\"><color=#E8C56A><u>");
            builder.Append(matchedKeyword.displayName);
            builder.Append("</u></color></link>");
            textIndex += matchedLength;
        }

        return builder.ToString();
    }

    void RefreshKeywordHover(PointerEventData eventData)
    {
        if (eventData == null ||
            descriptionText == null ||
            keywordByLinkID.Count == 0)
        {
            ClearKeywordHover();
            return;
        }

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            descriptionText,
            eventData.position,
            eventData.enterEventCamera
        );
        if (linkIndex < 0 ||
            linkIndex >= descriptionText.textInfo.linkCount)
        {
            ClearKeywordHover();
            return;
        }

        string linkID =
            descriptionText.textInfo.linkInfo[linkIndex].GetLinkID();
        CardKeywordData keyword;
        if (!keywordByLinkID.TryGetValue(linkID, out keyword) ||
            keyword == null)
        {
            ClearKeywordHover();
            return;
        }

        if (!string.IsNullOrEmpty(activeKeywordLinkID) &&
            activeKeywordLinkID != linkID)
        {
            ClearKeywordHover();
        }

        activeKeywordLinkID = linkID;
        BattleSecondaryInfoContent content =
            new BattleSecondaryInfoContent(
                keyword.displayName,
                keyword.tooltipText,
                "卡牌关键词"
            );
        BattleSecondaryInfoPanelHost.HandlePointer(
            new BattleSecondaryInfoHoverRequest(
                gameObject,
                linkID,
                content,
                eventData.position,
                true
            )
        );
    }

    void ClearKeywordHover()
    {
        if (string.IsNullOrEmpty(activeKeywordLinkID))
        {
            return;
        }

        BattleSecondaryInfoPanelHost.HandlePointer(
            new BattleSecondaryInfoHoverRequest(
                gameObject,
                activeKeywordLinkID,
                null,
                Vector2.zero,
                false
            )
        );
        activeKeywordLinkID = null;
    }

    void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    void HideLegacyCooldown()
    {
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }
    }
}
