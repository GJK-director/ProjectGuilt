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

    // SourceIntent 保留具体敌人意图引用，Slot Identity 与 actualTarget 均从权威 Intent 读取。
    public BattleEnemyIntent SourceIntent { get; }
    public BattleActionSlot ResponseSlot { get; }
    public CharacterData IntentSourceCharacter =>
        SourceIntent != null ? SourceIntent.enemy : null;
    public int IntentSourceSlotIndex =>
        SourceIntent != null ? SourceIntent.enemySlotIndex : -1;
    public CharacterData ActualTargetCharacter =>
        SourceIntent != null ? SourceIntent.actualTargetCharacter : null;
    public int ActualTargetSlotIndex =>
        SourceIntent != null ? SourceIntent.actualTargetSlotIndex : -1;
    public BattleCardState IntentCardState =>
        SourceIntent != null ? SourceIntent.enemyCardState : null;

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
        bool isMutual = false,
        BattleEnemyIntent sourceIntent = null,
        BattleActionSlot responseSlot = null
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
        SourceIntent = sourceIntent;
        ResponseSlot = responseSlot;
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
