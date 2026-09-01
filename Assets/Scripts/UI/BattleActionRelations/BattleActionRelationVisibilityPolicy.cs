// 脚本中文说明：只根据具体Relation与Slot级交互状态决定可见性，不负责查询或绘线。
public static class BattleActionRelationVisibilityPolicy
{
    public static bool IsVisible(
        BattleActionRelationDescriptor relation,
        string hoveredSlotID,
        string selectedSlotID,
        bool revealAll
    )
    {
        if (relation == null)
        {
            return false;
        }

        return revealAll ||
            InvolvesSlot(relation, hoveredSlotID) ||
            InvolvesSlot(relation, selectedSlotID);
    }

    public static bool IsHighlighted(
        BattleActionRelationDescriptor relation,
        string hoveredSlotID,
        string selectedSlotID,
        bool revealAll
    )
    {
        if (relation == null)
        {
            return false;
        }

        bool hovered = InvolvesSlot(relation, hoveredSlotID);
        bool selected = InvolvesSlot(relation, selectedSlotID);
        return revealAll
            ? !string.IsNullOrEmpty(hoveredSlotID)
                ? hovered
                : selected
            : hovered || selected;
    }

    private static bool InvolvesSlot(
        BattleActionRelationDescriptor relation,
        string slotID
    )
    {
        return !string.IsNullOrEmpty(slotID) &&
            relation.InvolvesSlot(slotID);
    }
}
