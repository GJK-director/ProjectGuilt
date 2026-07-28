using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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
    private BattleCardSelectionController selectionController;
    private bool warnedMissingVisualStyle;

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

    void OnDisable()
    {
        // Motion 组件独立负责视觉生命周期，这里只清理全局选择引用。
        selectionController?.ClearSelectionIfSelected(this);
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
