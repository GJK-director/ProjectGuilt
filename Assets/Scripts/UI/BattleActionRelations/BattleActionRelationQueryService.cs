using System;
using System.Collections.Generic;

// 只读取当前准备阶段正式槽位与敌人意图，不重新判断速度、拦截或卡牌资格。
public sealed class BattleActionRelationQueryService
{
    private readonly List<BattleActionRelationDescriptor> relations =
        new List<BattleActionRelationDescriptor>();
    private readonly List<BattleActionRelationDescriptor> filtered =
        new List<BattleActionRelationDescriptor>();
    private readonly List<BattleActionRelationDescriptor> intentFiltered =
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

    public IReadOnlyList<BattleActionRelationDescriptor>
        GetRelationsForIntent(BattleEnemyIntent intent)
    {
        RebuildRelations();
        intentFiltered.Clear();
        if (intent == null)
        {
            return intentFiltered;
        }

        for (int index = 0; index < relations.Count; index++)
        {
            if (object.ReferenceEquals(relations[index].SourceIntent, intent))
            {
                intentFiltered.Add(relations[index]);
            }
        }

        return intentFiltered;
    }

    public bool TryGetIntentBySlot(
        CharacterData enemy,
        int oneBasedSlotIndex,
        out BattleEnemyIntent intent
    )
    {
        intent = null;
        if (runtimeState == null || runtimeState.intentQueue == null ||
            enemy == null || oneBasedSlotIndex <= 0)
        {
            return false;
        }

        for (int index = 0; index < runtimeState.intentQueue.Count; index++)
        {
            BattleEnemyIntent candidate = runtimeState.intentQueue[index];
            if (candidate != null &&
                object.ReferenceEquals(candidate.enemy, enemy) &&
                candidate.enemySlotIndex == oneBasedSlotIndex)
            {
                intent = candidate;
                return true;
            }
        }

        return false;
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

        int enemyIndex = runtimeState.GetEnemyIndex(character);
        if (enemyIndex == 0)
        {
            return "Enemy:" + oneBasedSlotIndex;
        }

        if (enemyIndex > 0)
        {
            return "Enemy" + (enemyIndex + 1) + ":" +
                oneBasedSlotIndex;
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
            runtimeState.LifecyclePhase == BattleLifecyclePhase.Prepare &&
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
            if (!IsQueryableIntent(intent))
            {
                continue;
            }

            string enemySlotID = GetSlotID(
                intent.enemy,
                MathfMaxOne(intent.enemySlotIndex)
            );
            BattleActionSlot responseSlot = FindResponseSlot(intent);
            if (responseSlot != null &&
                IsMutualTarget(responseSlot, intent))
            {
                BattleActionRelationKind mutualKind;
                if (TryGetMutualRelationKind(
                        responseSlot.cardState,
                        intent.enemyCardState,
                        out mutualKind))
                {
                    string playerSlotID = GetSlotID(
                        responseSlot.owner,
                        responseSlot.slotIndex
                    );
                    AddRelation(
                        relationIDs,
                        new BattleActionRelationDescriptor(
                            playerSlotID + "<->" + enemySlotID,
                            mutualKind,
                            playerSlotID,
                            enemySlotID,
                            playerSlotID,
                            enemySlotID,
                            BattleActionRelationSide.Player,
                            GetPlayerOrder(responseSlot.owner, responseSlot.slotIndex),
                            intent.intentOrder,
                            responseSlot.cardState.cardData.cardType,
                            intent.enemyCardState.cardData.cardType,
                            true,
                            intent,
                            responseSlot
                        )
                    );
                }
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
                    BattleActionRelationKind.EnemyUnilateralTarget,
                    enemySlotID,
                    allySlotID,
                    allySlotID,
                    enemySlotID,
                    BattleActionRelationSide.Enemy,
                    intent.intentOrder,
                    GetPlayerOrder(
                        intent.actualTargetCharacter,
                        intent.actualTargetSlotIndex
                    ),
                    string.Empty,
                    intent.enemyCardState.cardData.cardType,
                    false,
                    intent
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
                slot.placementType == BattleActionPlacementType.Self ||
                !IsPlayerTargetCard(slot.cardState) ||
                slot.requestedEnemy == null)
            {
                continue;
            }

            if (slot.slotType == BattleActionSlotType.RespondToEnemyIntent &&
                slot.enemyIntent != null &&
                IsMutualTarget(slot, slot.enemyIntent))
            {
                continue;
            }

            string playerSlotID = GetSlotID(slot.owner, slot.slotIndex);
            int targetSlotIndex = slot.requestedEnemyIntent != null
                ? MathfMaxOne(slot.requestedEnemyIntent.enemySlotIndex)
                : MathfMaxOne(slot.requestedTargetSlotIndex);
            BattleEnemyIntent targetIntent = slot.requestedEnemyIntent;
            if (targetIntent == null)
            {
                TryGetIntentBySlot(
                    slot.requestedEnemy,
                    targetSlotIndex,
                    out targetIntent
                );
            }
            string enemySlotID = GetSlotID(
                slot.requestedEnemy,
                targetSlotIndex
            );
            AddRelation(
                relationIDs,
                new BattleActionRelationDescriptor(
                    playerSlotID + "->" + enemySlotID,
                    BattleActionRelationKind.PlayerUnilateralTarget,
                    playerSlotID,
                    enemySlotID,
                    playerSlotID,
                    enemySlotID,
                    BattleActionRelationSide.Player,
                    GetPlayerOrder(slot.owner, slot.slotIndex),
                    targetSlotIndex,
                    slot.cardState.cardData.cardType,
                    GetEnemyActionType(
                        slot.requestedEnemy,
                        targetSlotIndex
                    ),
                    false,
                    targetIntent,
                    slot
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

    private static bool IsQueryableIntent(BattleEnemyIntent intent)
    {
        return intent != null &&
            intent.enemyCardState != null &&
            intent.enemyCardState.cardData != null &&
            intent.enemy != null &&
            intent.enemySlotIndex > 0 &&
            !string.IsNullOrEmpty(intent.intentID);
    }

    private static bool IsPlayerTargetCard(BattleCardState cardState)
    {
        if (cardState == null || cardState.cardData == null)
        {
            return false;
        }

        string cardType = cardState.cardData.cardType;
        return cardType == CardType.Attack ||
            cardType == CardType.Defense ||
            cardType == CardType.Dodge;
    }

    private static bool IsMutualTarget(
        BattleActionSlot playerSlot,
        BattleEnemyIntent enemyIntent
    )
    {
        return playerSlot != null &&
            enemyIntent != null &&
            object.ReferenceEquals(
                enemyIntent.actualTargetCharacter,
                playerSlot.owner
            ) &&
            enemyIntent.actualTargetSlotIndex == playerSlot.slotIndex;
    }

    private static bool TryGetMutualRelationKind(
        BattleCardState playerCardState,
        BattleCardState enemyCardState,
        out BattleActionRelationKind kind
    )
    {
        kind = BattleActionRelationKind.AttackClash;
        if (playerCardState == null || playerCardState.cardData == null ||
            enemyCardState == null || enemyCardState.cardData == null)
        {
            return false;
        }

        string playerType = playerCardState.cardData.cardType;
        string enemyType = enemyCardState.cardData.cardType;
        if (playerType == CardType.Attack && enemyType == CardType.Attack)
        {
            kind = BattleActionRelationKind.AttackClash;
            return true;
        }
        if (playerType == CardType.Defense && enemyType == CardType.Attack ||
            playerType == CardType.Attack && enemyType == CardType.Defense)
        {
            kind = BattleActionRelationKind.DefenseResponse;
            return true;
        }
        if (playerType == CardType.Dodge && enemyType == CardType.Attack ||
            playerType == CardType.Attack && enemyType == CardType.Dodge)
        {
            kind = BattleActionRelationKind.EvadeResponse;
            return true;
        }
        return false;
    }

    private string GetEnemyActionType(
        CharacterData enemy,
        int enemySlotIndex
    )
    {
        if (runtimeState == null || runtimeState.intentQueue == null)
        {
            return string.Empty;
        }

        for (int index = 0; index < runtimeState.intentQueue.Count; index++)
        {
            BattleEnemyIntent intent = runtimeState.intentQueue[index];
            if (intent != null &&
                object.ReferenceEquals(intent.enemy, enemy) &&
                MathfMaxOne(intent.enemySlotIndex) == enemySlotIndex &&
                intent.enemyCardState != null &&
                intent.enemyCardState.cardData != null)
            {
                return intent.enemyCardState.cardData.cardType;
            }
        }
        return string.Empty;
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
