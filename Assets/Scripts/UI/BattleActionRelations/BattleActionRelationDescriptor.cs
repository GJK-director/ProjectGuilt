using System;

public enum BattleActionRelationKind
{
    EnemyUnilateralTarget,
    PlayerUnilateralTarget,
    AttackClash,
    DefenseResponse,
    EvadeResponse,

    // 保留旧名称，避免历史测试和序列化外代码在本轮被迫同步重命名。
    EnemyUnilateralAttack = EnemyUnilateralTarget,
    PlayerUnilateralAttack = PlayerUnilateralTarget,
    Clash = AttackClash
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
    public string PlayerActionType { get; }
    public string EnemyActionType { get; }
    public bool IsMutual { get; }
    public bool UsesMutualSolidVisual => IsMutual &&
        (Kind == BattleActionRelationKind.AttackClash ||
         Kind == BattleActionRelationKind.DefenseResponse ||
         Kind == BattleActionRelationKind.EvadeResponse);
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
        int targetOrder,
        string playerActionType = null,
        string enemyActionType = null,
        bool isMutual = false
    )
    {
        RelationID = relationID ?? string.Empty;
        Kind = kind;
        SourceSlotID = sourceSlotID ?? string.Empty;
        TargetSlotID = targetSlotID ?? string.Empty;
        PlayerSlotID = playerSlotID;
        EnemySlotID = enemySlotID;
        SourceSide = sourceSide;
        PlayerActionType = playerActionType ?? string.Empty;
        EnemyActionType = enemyActionType ?? string.Empty;
        IsMutual = isMutual;
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
