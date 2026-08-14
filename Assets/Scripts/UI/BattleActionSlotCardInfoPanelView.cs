using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 行动槽位放大卡牌的单侧视图。
// 面板只保留正式卡面和关闭按钮，关键词仍由 BattleCardUIView 处理。
public sealed class BattleActionSlotCardInfoPanelView : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("可编辑 UI 引用")]
    [SerializeField] Image artworkImage;
    [SerializeField] BattleCardUIView cardPreviewPrefab;
    [SerializeField] Button closeButton;

    RectTransform cardPreviewRect;
    BattleCardUIView cardPreviewView;
    Action closeHandler;
    Action<bool> pointerPresenceHandler;
    bool closeButtonBound;
    bool pointerInside;

    internal void SetCloseHandler(Action handler)
    {
        closeHandler = handler;
        BindCloseButton();
    }

    internal void SetPointerPresenceHandler(Action<bool> handler)
    {
        pointerPresenceHandler = handler;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetPointerInside(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetPointerInside(false);
    }

    internal void ShowCard(
        CharacterData owner,
        CharacterData target,
        BattleCardState cardState,
        bool enemySide
    )
    {
        if (cardState == null)
        {
            Hide();
            return;
        }

        BattleCardUIPreviewData preview =
            BattleCardUIPreviewBuilder.Build(owner, target, cardState);

        EnsureCardPreview();
        if (cardPreviewView == null)
        {
            Hide();
            return;
        }

        cardPreviewView.BindCard(owner, cardState, preview, null);
        cardPreviewView.SetSelected(false);
        cardPreviewView.gameObject.SetActive(true);
        gameObject.SetActive(true);
        RefreshCardPreviewLayout();
    }

    internal void Hide()
    {
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        pointerInside = false;
        BindCloseButton();
        RefreshCardPreviewLayout();
    }

    void OnDisable()
    {
        pointerInside = false;
    }

    void OnRectTransformDimensionsChange()
    {
        RefreshCardPreviewLayout();
    }

    void OnDestroy()
    {
        if (closeButtonBound && closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
        }
    }

    void BindCloseButton()
    {
        if (closeButtonBound || closeButton == null)
        {
            return;
        }

        closeButton.onClick.AddListener(HandleCloseButtonClicked);
        closeButtonBound = true;
    }

    void HandleCloseButtonClicked()
    {
        closeHandler?.Invoke();
    }

    void SetPointerInside(bool inside)
    {
        if (pointerInside == inside)
        {
            return;
        }

        pointerInside = inside;
        pointerPresenceHandler?.Invoke(inside);
    }

    void RefreshCardPreviewLayout()
    {
        if (artworkImage == null || cardPreviewRect == null)
        {
            return;
        }

        float viewportWidth = Mathf.Max(
            0f,
            artworkImage.rectTransform.rect.width - 8f
        );
        float viewportHeight = Mathf.Max(
            0f,
            artworkImage.rectTransform.rect.height - 8f
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
        cardPreviewRect = cardPreviewView.transform as RectTransform;

        // 固定面板中的卡面位置，但保留 BattleCardUIView 的射线与关键词交互。
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
            cardRaycaster.enabled = true;
        }

        CanvasGroup cardCanvasGroup =
            cardPreviewView.GetComponent<CanvasGroup>();
        if (cardCanvasGroup != null)
        {
            cardCanvasGroup.interactable = true;
            cardCanvasGroup.blocksRaycasts = true;
        }

        cardPreviewView.gameObject.SetActive(true);
        RefreshCardPreviewLayout();
    }
}
