using System;
using System.Collections.Generic;

// 只读取当前准备阶段正式槽位与敌人意图，不重新判断速度、拦截或卡牌资格。
public sealed class BattleActionRelationQueryService
{
    private readonly List<BattleActionRelationDescriptor> relations =
        new List<BattleActionRelationDescriptor>();
    private readonly List<BattleActionRelationDescriptor> filtered =
        new List<BattleActionRelationDescriptor>();
    private BattleRuntimeState runtimeState;

    public BattleActionRelationQueryService(BattleRuntimeState state)
    {
        runtimeState = state;
    }

    public void SetRuntimeState(BattleRuntimeState state)
    {
        runtimeState = state;
    }

    public IReadOnlyList<BattleActionRelationDescriptor>
        GetAllCurrentRelations()
    {
        RebuildRelations();
        return relations;
    }

    public IReadOnlyList<BattleActionRelationDescriptor>
        GetRelationsForSlot(string slotID)
    {
        RebuildRelations();
        filtered.Clear();
        for (int index = 0; index < relations.Count; index++)
        {
            if (relations[index].InvolvesSlot(slotID))
            {
                filtered.Add(relations[index]);
            }
        }

        return filtered;
    }

    public string GetSlotID(
        CharacterData character,
        int oneBasedSlotIndex
    )
    {
        if (runtimeState == null || character == null ||
            oneBasedSlotIndex <= 0)
        {
            return string.Empty;
        }

        if (object.ReferenceEquals(character, runtimeState.allyA))
        {
            return "AllyA:" + oneBasedSlotIndex;
        }

        if (object.ReferenceEquals(character, runtimeState.allyB))
        {
            return "AllyB:" + oneBasedSlotIndex;
        }

        if (object.ReferenceEquals(character, runtimeState.enemy))
        {
            return "Enemy:" + oneBasedSlotIndex;
        }

        return string.Empty;
    }

    private void RebuildRelations()
    {
        relations.Clear();
        if (!IsPlanningState())
        {
            return;
        }

        HashSet<string> relationIDs =
            new HashSet<string>(StringComparer.Ordinal);
        AddEnemyIntentRelations(relationIDs);
        AddPlayerUnilateralRelations(relationIDs);
        relations.Sort(CompareRelations);
        AssignStableLanes();
    }

    private bool IsPlanningState()
    {
        return runtimeState != null &&
            !runtimeState.IsBattleEnded &&
            runtimeState.currentPhase == "Prepare" &&
            runtimeState.currentExecutionPlan == null;
    }

    private void AddEnemyIntentRelations(HashSet<string> relationIDs)
    {
        if (runtimeState.intentQueue == null)
        {
            return;
        }

        for (int index = 0; index < runtimeState.intentQueue.Count; index++)
        {
            BattleEnemyIntent intent = runtimeState.intentQueue[index];
            if (!IsAttackIntent(intent))
            {
                continue;
            }

            string enemySlotID = GetSlotID(
                intent.enemy,
                MathfMaxOne(intent.enemySlotIndex)
            );
            BattleActionSlot responseSlot = FindResponseSlot(intent);
            if (responseSlot != null)
            {
                if (IsAttackCard(responseSlot.cardState))
                {
                    string playerSlotID = GetSlotID(
                        responseSlot.owner,
                        responseSlot.slotIndex
                    );
                    AddRelation(
                        relationIDs,
                        new BattleActionRelationDescriptor(
                            playerSlotID + "<->" + enemySlotID,
                            BattleActionRelationKind.Clash,
                            playerSlotID,
                            enemySlotID,
                            playerSlotID,
                            enemySlotID,
                            BattleActionRelationSide.Player,
                            GetPlayerOrder(responseSlot.owner, responseSlot.slotIndex),
                            intent.intentOrder
                        )
                    );
                }

                // Defense/Dodge直接响应已经处理该攻击，但不属于线条关系。
                continue;
            }

            if (intent.isResponded)
            {
                continue;
            }

            string allySlotID = GetSlotID(
                intent.actualTargetCharacter,
                intent.actualTargetSlotIndex
            );
            AddRelation(
                relationIDs,
                new BattleActionRelationDescriptor(
                    enemySlotID + "->" + allySlotID,
                    BattleActionRelationKind.EnemyUnilateralAttack,
                    enemySlotID,
                    allySlotID,
                    allySlotID,
                    enemySlotID,
                    BattleActionRelationSide.Enemy,
                    intent.intentOrder,
                    GetPlayerOrder(
                        intent.actualTargetCharacter,
                        intent.actualTargetSlotIndex
                    )
                )
            );
        }
    }

    private void AddPlayerUnilateralRelations(
        HashSet<string> relationIDs
    )
    {
        if (runtimeState.actionSlots == null)
        {
            return;
        }

        for (int index = 0; index < runtimeState.actionSlots.Count; index++)
        {
            BattleActionSlot slot = runtimeState.actionSlots[index];
            if (slot == null || slot.IsEmpty() || slot.isUsed ||
                slot.slotType != BattleActionSlotType.FreeAction ||
                slot.placementType != BattleActionPlacementType.SpecificEnemy ||
                !IsAttackCard(slot.cardState) || slot.target == null)
            {
                continue;
            }

            string playerSlotID = GetSlotID(slot.owner, slot.slotIndex);
            int targetSlotIndex = slot.requestedTargetSlotIndex > 0
                ? slot.requestedTargetSlotIndex
                : 1;
            string enemySlotID = GetSlotID(slot.target, targetSlotIndex);
            AddRelation(
                relationIDs,
                new BattleActionRelationDescriptor(
                    playerSlotID + "->" + enemySlotID,
                    BattleActionRelationKind.PlayerUnilateralAttack,
                    playerSlotID,
                    enemySlotID,
                    playerSlotID,
                    enemySlotID,
                    BattleActionRelationSide.Player,
                    GetPlayerOrder(slot.owner, slot.slotIndex),
                    targetSlotIndex
                )
            );
        }
    }

    private BattleActionSlot FindResponseSlot(BattleEnemyIntent intent)
    {
        if (!intent.isResponded || runtimeState.actionSlots == null)
        {
            return null;
        }

        for (int index = 0; index < runtimeState.actionSlots.Count; index++)
        {
            BattleActionSlot slot = runtimeState.actionSlots[index];
            if (slot != null && !slot.IsEmpty() && !slot.isUsed &&
                slot.slotType == BattleActionSlotType.RespondToEnemyIntent &&
                object.ReferenceEquals(slot.enemyIntent, intent))
            {
                return slot;
            }
        }

        return null;
    }

    private static bool IsAttackIntent(BattleEnemyIntent intent)
    {
        return intent != null &&
            intent.enemyCardState != null &&
            IsAttackCard(intent.enemyCardState) &&
            !string.IsNullOrEmpty(intent.intentID);
    }

    private static bool IsAttackCard(BattleCardState cardState)
    {
        return cardState != null && cardState.cardData != null &&
            cardState.cardData.cardType == CardType.Attack;
    }

    private void AddRelation(
        HashSet<string> relationIDs,
        BattleActionRelationDescriptor relation
    )
    {
        if (relation == null ||
            string.IsNullOrEmpty(relation.RelationID) ||
            string.IsNullOrEmpty(relation.SourceSlotID) ||
            string.IsNullOrEmpty(relation.TargetSlotID) ||
            !relationIDs.Add(relation.RelationID))
        {
            return;
        }

        relations.Add(relation);
    }

    private void AssignStableLanes()
    {
        Dictionary<string, int> nextLaneByEndpoint =
            new Dictionary<string, int>(StringComparer.Ordinal);
        for (int index = 0; index < relations.Count; index++)
        {
            BattleActionRelationDescriptor relation = relations[index];
            int sourceLane;
            int targetLane;
            nextLaneByEndpoint.TryGetValue(
                relation.SourceSlotID,
                out sourceLane
            );
            nextLaneByEndpoint.TryGetValue(
                relation.TargetSlotID,
                out targetLane
            );
            int lane = sourceLane > targetLane
                ? sourceLane
                : targetLane;
            relation.LaneIndex = lane;
            nextLaneByEndpoint[relation.SourceSlotID] = lane + 1;
            nextLaneByEndpoint[relation.TargetSlotID] = lane + 1;
        }
    }

    private int GetPlayerOrder(CharacterData character, int slotIndex)
    {
        int characterOrder = object.ReferenceEquals(character, runtimeState.allyA)
            ? 0
            : object.ReferenceEquals(character, runtimeState.allyB)
                ? 1
                : 2;
        return characterOrder * 100 + MathfMaxOne(slotIndex);
    }

    private static int CompareRelations(
        BattleActionRelationDescriptor left,
        BattleActionRelationDescriptor right
    )
    {
        int result = left.Kind.CompareTo(right.Kind);
        if (result != 0) return result;
        result = left.SourceOrder.CompareTo(right.SourceOrder);
        if (result != 0) return result;
        result = left.TargetOrder.CompareTo(right.TargetOrder);
        if (result != 0) return result;
        return string.CompareOrdinal(left.RelationID, right.RelationID);
    }

    private static int MathfMaxOne(int value)
    {
        return value > 0 ? value : 1;
    }
}
