using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleCardUIView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text pointText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text cooldownText;

    private CharacterData boundOwner;
    private BattleCardState boundCardState;
    private Action<BattleCardUIView> dragEndedHandler;

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
    private bool isDragging;

    public CharacterData BoundOwner => boundOwner;
    public BattleCardState BoundCardState => boundCardState;
    public bool IsDragging => isDragging;
    public bool CanBeginDrag =>
        boundOwner != null &&
        boundCardState != null &&
        boundCardState.cardData != null &&
        boundCardState.currentCooldown <= 0;

    public void BindCard(
        CharacterData owner,
        BattleCardState cardState,
        BattleCardUIPreviewData data,
        Action<BattleCardUIView> onDragEnded
    )
    {
        boundOwner = owner;
        boundCardState = cardState;
        dragEndedHandler = onDragEnded;
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
        SetText(cooldownText, data.cooldownText);
    }

    public void SetEmpty()
    {
        boundOwner = null;
        boundCardState = null;
        dragEndedHandler = null;
        SetText(cardNameText, "空");
        SetText(pointText, "—");
        SetText(typeText, "");
        SetText(descriptionText, "");
        SetText(cooldownText, "CD：—");
    }

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
        draggedRect.SetParent(rootCanvas.transform, true);
        draggedRect.SetAsLastSibling();
        canvasGroup.alpha = 0.85f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
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
    }

    private void UpdateDragPosition(PointerEventData eventData)
    {
        RectTransform canvasRect = rootCanvas != null
            ? rootCanvas.transform as RectTransform
            : null;

        if (canvasRect == null || draggedRect == null)
        {
            return;
        }

        Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : eventData.pressEventCamera;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventCamera,
                out localPoint))
        {
            draggedRect.position = canvasRect.TransformPoint(localPoint);
        }
    }

    private void RestoreDragVisual()
    {
        isDragging = false;

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
}
