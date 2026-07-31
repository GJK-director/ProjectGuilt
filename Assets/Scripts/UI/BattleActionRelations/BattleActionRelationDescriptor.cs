using System;

public enum BattleActionRelationKind
{
    EnemyUnilateralAttack,
    PlayerUnilateralAttack,
    Clash
}

public enum BattleActionRelationSide
{
    Player,
    Enemy
}

public sealed class BattleActionRelationDescriptor
{
    public string RelationID { get; }
    public BattleActionRelationKind Kind { get; }
    public string SourceSlotID { get; }
    public string TargetSlotID { get; }
    public string PlayerSlotID { get; }
    public string EnemySlotID { get; }
    public BattleActionRelationSide SourceSide { get; }
    public bool IsCurrentFinalEffective { get; }
    public int SourceOrder { get; }
    public int TargetOrder { get; }
    public int LaneIndex { get; internal set; }

    public BattleActionRelationDescriptor(
        string relationID,
        BattleActionRelationKind kind,
        string sourceSlotID,
        string targetSlotID,
        string playerSlotID,
        string enemySlotID,
        BattleActionRelationSide sourceSide,
        int sourceOrder,
        int targetOrder
    )
    {
        RelationID = relationID ?? string.Empty;
        Kind = kind;
        SourceSlotID = sourceSlotID ?? string.Empty;
        TargetSlotID = targetSlotID ?? string.Empty;
        PlayerSlotID = playerSlotID;
        EnemySlotID = enemySlotID;
        SourceSide = sourceSide;
        SourceOrder = sourceOrder;
        TargetOrder = targetOrder;
        IsCurrentFinalEffective = true;
    }

    public bool InvolvesSlot(string slotID)
    {
        return !string.IsNullOrEmpty(slotID) &&
            (string.Equals(SourceSlotID, slotID, StringComparison.Ordinal) ||
             string.Equals(TargetSlotID, slotID, StringComparison.Ordinal) ||
             string.Equals(PlayerSlotID, slotID, StringComparison.Ordinal) ||
             string.Equals(EnemySlotID, slotID, StringComparison.Ordinal));
    }
}
