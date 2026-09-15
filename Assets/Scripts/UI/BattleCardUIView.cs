using System.Collections.Generic;
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
    [SerializeField, Range(0f, 1f)] private float consumedAlpha = 0.45f;
    [Header("关键词 Hover")]
    [SerializeField, Min(0f)] private float keywordHoverPaddingX;
    [SerializeField, Min(0f)] private float keywordHoverPaddingY;

    private CharacterData boundOwner;
    private BattleCardState boundCardState;
    private BattleCardSelectionController selectionController;
    private bool warnedMissingVisualStyle;
    private CanvasGroup availabilityCanvasGroup;
    private float availableCanvasGroupAlpha = 1f;
    private bool hasCachedAvailableCanvasGroupAlpha;
    private readonly Dictionary<string, BattleCardKeywordBinding>
        keywordByLinkID =
            new Dictionary<string, BattleCardKeywordBinding>();
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
        !boundCardState.isConsumed &&
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

        ApplyConsumedState();
    }

    public void SetEmpty()
    {
        selectionController?.ClearSelectionIfSelected(this);
        ApplyConsumedVisual(false);
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

    void ApplyConsumedState()
    {
        bool isConsumed =
            boundCardState != null && boundCardState.isConsumed;

        if (isConsumed)
        {
            selectionController?.ClearSelectionIfSelected(this);
        }

        ApplyConsumedVisual(isConsumed);
    }

    void ApplyConsumedVisual(bool isConsumed)
    {
        if (!isConsumed && availabilityCanvasGroup == null)
        {
            return;
        }

        if (availabilityCanvasGroup == null)
        {
            availabilityCanvasGroup = GetComponent<CanvasGroup>();
            if (availabilityCanvasGroup == null)
            {
                availabilityCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (!hasCachedAvailableCanvasGroupAlpha)
        {
            availableCanvasGroupAlpha = availabilityCanvasGroup.alpha;
            hasCachedAvailableCanvasGroupAlpha = true;
        }

        availabilityCanvasGroup.alpha = isConsumed
            ? availableCanvasGroupAlpha * Mathf.Clamp01(consumedAlpha)
            : availableCanvasGroupAlpha;
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
        CardKeywordData[] previewKeywords
    )
    {
        ClearKeywordHover();
        keywordByLinkID.Clear();

        if (descriptionText == null)
        {
            return;
        }

        descriptionText.richText = true;
        CardTestData cardData =
            boundCardState != null ? boundCardState.cardData : null;

        // SetCard is also used by unbound previews, whose local keywords live
        // only in BattleCardUIPreviewData.
        if (previewKeywords != null &&
            (cardData == null ||
                !object.ReferenceEquals(previewKeywords, cardData.keywords)))
        {
            cardData = new CardTestData
            {
                cardID = cardData != null ? cardData.cardID : "preview",
                keywords = previewKeywords
            };
        }

        BattleCardDescriptionFormatResult result =
            BattleCardDescriptionFormatter.Format(description, cardData);
        foreach (KeyValuePair<string, BattleCardKeywordBinding> pair in
            result.keywordBindings)
        {
            keywordByLinkID[pair.Key] = pair.Value;
        }
        descriptionText.text = result.richText;
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
        if (linkIndex < 0)
        {
            linkIndex = FindIntersectingKeywordLinkWithPadding(
                eventData.position,
                eventData.enterEventCamera
            );
        }
        if (linkIndex < 0 ||
            linkIndex >= descriptionText.textInfo.linkCount)
        {
            ClearKeywordHover();
            return;
        }

        string linkID =
            descriptionText.textInfo.linkInfo[linkIndex].GetLinkID();
        BattleCardKeywordBinding keyword;
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
        if (!BattleCardTooltipResolver.TryResolve(keyword, boundOwner, out
                BattleSecondaryInfoContent content))
        {
            ClearKeywordHover();
            return;
        }
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

    int FindIntersectingKeywordLinkWithPadding(
        Vector2 pointerScreenPosition,
        Camera eventCamera
    )
    {
        if (descriptionText == null ||
            descriptionText.textInfo.linkCount == 0)
        {
            return -1;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                descriptionText.rectTransform,
                pointerScreenPosition,
                eventCamera,
                out Vector2 localPointerPosition))
        {
            return -1;
        }

        float paddingX = Mathf.Max(0f, keywordHoverPaddingX);
        float paddingY = Mathf.Max(0f, keywordHoverPaddingY);
        int bestLinkIndex = -1;
        float bestDistanceSquared = float.PositiveInfinity;

        for (int linkIndex = 0;
            linkIndex < descriptionText.textInfo.linkCount;
            linkIndex++)
        {
            TMP_LinkInfo linkInfo =
                descriptionText.textInfo.linkInfo[linkIndex];
            bool hasSegment = false;
            int segmentLineNumber = -1;
            float segmentMinX = 0f;
            float segmentMaxX = 0f;
            float segmentMinY = 0f;
            float segmentMaxY = 0f;

            for (int characterOffset = 0;
                characterOffset < linkInfo.linkTextLength;
                characterOffset++)
            {
                int characterIndex =
                    linkInfo.linkTextfirstCharacterIndex + characterOffset;
                if (characterIndex < 0 ||
                    characterIndex >= descriptionText.textInfo.characterInfo.Length)
                {
                    continue;
                }

                TMP_CharacterInfo characterInfo =
                    descriptionText.textInfo.characterInfo[characterIndex];
                if (descriptionText.overflowMode == TextOverflowModes.Page &&
                    characterInfo.pageNumber + 1 != descriptionText.pageToDisplay)
                {
                    continue;
                }

                if (!hasSegment ||
                    segmentLineNumber != characterInfo.lineNumber)
                {
                    if (hasSegment)
                    {
                        ConsiderKeywordSegment(
                            linkIndex,
                            localPointerPosition,
                            paddingX,
                            paddingY,
                            segmentMinX,
                            segmentMaxX,
                            segmentMinY,
                            segmentMaxY,
                            ref bestLinkIndex,
                            ref bestDistanceSquared
                        );
                    }

                    hasSegment = true;
                    segmentLineNumber = characterInfo.lineNumber;
                    segmentMinX = characterInfo.bottomLeft.x;
                    segmentMaxX = characterInfo.topRight.x;
                    segmentMinY = characterInfo.descender;
                    segmentMaxY = characterInfo.ascender;
                    continue;
                }

                segmentMinX = Mathf.Min(
                    segmentMinX,
                    characterInfo.bottomLeft.x
                );
                segmentMaxX = Mathf.Max(
                    segmentMaxX,
                    characterInfo.topRight.x
                );
                segmentMinY = Mathf.Min(
                    segmentMinY,
                    characterInfo.descender
                );
                segmentMaxY = Mathf.Max(
                    segmentMaxY,
                    characterInfo.ascender
                );
            }

            if (hasSegment)
            {
                ConsiderKeywordSegment(
                    linkIndex,
                    localPointerPosition,
                    paddingX,
                    paddingY,
                    segmentMinX,
                    segmentMaxX,
                    segmentMinY,
                    segmentMaxY,
                    ref bestLinkIndex,
                    ref bestDistanceSquared
                );
            }
        }

        return bestLinkIndex;
    }

    static void ConsiderKeywordSegment(
        int linkIndex,
        Vector2 pointerPosition,
        float paddingX,
        float paddingY,
        float minX,
        float maxX,
        float minY,
        float maxY,
        ref int bestLinkIndex,
        ref float bestDistanceSquared
    )
    {
        Rect originalBounds = Rect.MinMaxRect(
            minX,
            minY,
            maxX,
            maxY
        );
        Rect paddedBounds = Rect.MinMaxRect(
            minX - paddingX,
            minY - paddingY,
            maxX + paddingX,
            maxY + paddingY
        );
        if (!paddedBounds.Contains(pointerPosition))
        {
            return;
        }

        float distanceX = pointerPosition.x < originalBounds.xMin
            ? originalBounds.xMin - pointerPosition.x
            : pointerPosition.x > originalBounds.xMax
                ? pointerPosition.x - originalBounds.xMax
                : 0f;
        float distanceY = pointerPosition.y < originalBounds.yMin
            ? originalBounds.yMin - pointerPosition.y
            : pointerPosition.y > originalBounds.yMax
                ? pointerPosition.y - originalBounds.yMax
                : 0f;
        float distanceSquared =
            distanceX * distanceX + distanceY * distanceY;

        if (bestLinkIndex < 0 ||
            distanceSquared < bestDistanceSquared ||
            (Mathf.Approximately(distanceSquared, bestDistanceSquared) &&
                linkIndex < bestLinkIndex))
        {
            bestLinkIndex = linkIndex;
            bestDistanceSquared = distanceSquared;
        }
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
