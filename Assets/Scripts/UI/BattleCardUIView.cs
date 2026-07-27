using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleCardUIView : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
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
    private Action<BattleCardUIView> dragEndedHandler;
    private BattleCardSelectionController selectionController;

    private RectTransform draggedRect;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private Vector2 originalPivot;
    private Vector2 originalAnchoredPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalLocalScale;
    private float originalAlpha;
    private bool originalInteractable;
    private bool originalBlocksRaycasts;
    private Vector2 dragPointerOffset;
    private bool hasDragPointerOffset;
    private bool isDragging;
    private bool warnedMissingVisualStyle;

    public CharacterData BoundOwner => boundOwner;
    public BattleCardState BoundCardState => boundCardState;
    public bool IsDragging => isDragging;
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
    public bool CanBeginDrag =>
        CanSelect;

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
        Action<BattleCardUIView> onDragEnded
    )
    {
        BindCard(owner, cardState, data, onDragEnded, null);
    }

    public void BindCard(
        CharacterData owner,
        BattleCardState cardState,
        BattleCardUIPreviewData data,
        Action<BattleCardUIView> onDragEnded,
        BattleCardSelectionController cardSelectionController
    )
    {
        boundOwner = owner;
        boundCardState = cardState;
        dragEndedHandler = onDragEnded;
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
        SetText(descriptionText, data.descriptionText);
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
        dragEndedHandler = null;
        selectionController = null;
        SetText(cardNameText, "空");
        SetText(pointText, "—");
        SetText(typeText, "");
        SetText(descriptionText, "");
        HideLegacyCooldown();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        motionView?.SetHovered(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        motionView?.SetHovered(false);
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

    // 旧拖拽方法仅保留给历史测试和兼容调用，BattleCardUIView 不再注册拖拽接口。
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = false;

        if (!CanBeginDrag || eventData == null)
        {
            return;
        }

        draggedRect = transform as RectTransform;
        Canvas nearestCanvas = GetComponentInParent<Canvas>();
        rootCanvas = nearestCanvas != null ? nearestCanvas.rootCanvas : null;

        if (draggedRect == null || rootCanvas == null)
        {
            Debug.LogWarning("BattleCardUIView 拖动失败：找不到 RectTransform 或根 Canvas。");
            return;
        }

        originalParent = draggedRect.parent;
        originalSiblingIndex = draggedRect.GetSiblingIndex();
        originalAnchorMin = draggedRect.anchorMin;
        originalAnchorMax = draggedRect.anchorMax;
        originalPivot = draggedRect.pivot;
        originalAnchoredPosition = draggedRect.anchoredPosition;
        originalLocalRotation = draggedRect.localRotation;
        originalLocalScale = draggedRect.localScale;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        originalAlpha = canvasGroup.alpha;
        originalInteractable = canvasGroup.interactable;
        originalBlocksRaycasts = canvasGroup.blocksRaycasts;

        isDragging = true;
        hasDragPointerOffset = false;
        draggedRect.SetParent(rootCanvas.transform, true);

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect != null)
        {
            Vector3 worldPosition = draggedRect.position;
            draggedRect.anchorMin = canvasRect.pivot;
            draggedRect.anchorMax = canvasRect.pivot;
            draggedRect.position = worldPosition;
        }

        canvasGroup.alpha = 0.85f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Vector2 pointerAnchoredPosition;
        if (TryGetPointerAnchoredPosition(
                eventData,
                out pointerAnchoredPosition))
        {
            dragPointerOffset =
                draggedRect.anchoredPosition - pointerAnchoredPosition;
            hasDragPointerOffset = true;
        }

        UpdateDragPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || eventData == null)
        {
            return;
        }

        UpdateDragPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        RestoreDragVisual();
        dragEndedHandler?.Invoke(this);
    }

    void OnDisable()
    {
        if (isDragging)
        {
            RestoreDragVisual();
        }

        selectionController?.ClearSelectionIfSelected(this);
        motionView?.ResetVisualState();
    }

    private void UpdateDragPosition(PointerEventData eventData)
    {
        if (draggedRect == null)
        {
            return;
        }

        Vector2 pointerAnchoredPosition;
        if (!TryGetPointerAnchoredPosition(
                eventData,
                out pointerAnchoredPosition))
        {
            return;
        }

        Vector2 targetPosition = pointerAnchoredPosition +
            (hasDragPointerOffset ? dragPointerOffset : Vector2.zero);

        draggedRect.anchoredPosition = targetPosition;
    }

    private bool TryGetPointerAnchoredPosition(
        PointerEventData eventData,
        out Vector2 anchoredPosition
    )
    {
        anchoredPosition = Vector2.zero;
        RectTransform canvasRect = rootCanvas != null
            ? rootCanvas.transform as RectTransform
            : null;

        if (canvasRect == null || eventData == null)
        {
            return false;
        }

        Camera eventCamera = rootCanvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
            ? null
            : eventData.pressEventCamera;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            eventCamera,
            out anchoredPosition
        );
    }

    private void RestoreDragVisual()
    {
        isDragging = false;
        hasDragPointerOffset = false;

        if (draggedRect != null && originalParent != null)
        {
            draggedRect.SetParent(originalParent, false);
            draggedRect.anchorMin = originalAnchorMin;
            draggedRect.anchorMax = originalAnchorMax;
            draggedRect.pivot = originalPivot;
            draggedRect.anchoredPosition = originalAnchoredPosition;
            draggedRect.localRotation = originalLocalRotation;
            draggedRect.localScale = originalLocalScale;
            draggedRect.SetSiblingIndex(originalSiblingIndex);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = originalAlpha;
            canvasGroup.interactable = originalInteractable;
            canvasGroup.blocksRaycasts = originalBlocksRaycasts;
        }
    }

    void SetText(TMP_Text text, string value)
    {
        if (text == null)
        {
            return;
        }

        text.text = value;
    }

    void HideLegacyCooldown()
    {
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }
    }
}
