using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BattleActionSlotRelationHoverRelay : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private BattleActionRelationLineController controller;
    [SerializeField] private BattleActionSlotUIView slotView;

    private string activeSlotID;

    private void Awake()
    {
        if (slotView == null)
        {
            slotView = GetComponent<BattleActionSlotUIView>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (controller == null || slotView == null)
        {
            return;
        }
        activeSlotID = controller.GetSlotID(slotView);
        if (!string.IsNullOrEmpty(activeSlotID))
        {
            controller.SetHoveredSlot(activeSlotID);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (controller == null)
        {
            return;
        }
        controller.ClearHoveredSlot(activeSlotID);
        activeSlotID = string.Empty;
    }

    public void Bind(
        BattleActionRelationLineController relationController,
        BattleActionSlotUIView relationSlotView
    )
    {
        controller = relationController;
        slotView = relationSlotView;
    }

    private void OnDisable()
    {
        if (controller != null && !string.IsNullOrEmpty(activeSlotID))
        {
            controller.ClearHoveredSlot(activeSlotID);
        }
        activeSlotID = string.Empty;
    }
}
