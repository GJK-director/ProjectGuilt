using System.Collections.Generic;

// BattleActionOrderCandidate = 纯排序候选。
// Resolver 只读取当前准备状态，不修改槽位、意图或角色运行时状态。
public sealed class BattleActionOrderCandidate
{
    public BattleExecutionItemType executionType;
    public BattleEnemyIntent enemyIntent;
    public BattleActionSlot actionSlot;
    public CharacterData orderingActor;
    public BattleExecutionPriorityTier priorityTier;
    public int effectiveSpeed;
    public int responsePriority;
    public int actionSlotOrder;
    public int actorPositionOrder;
    public int stableOrder;
    public long actionAssignmentSequence;
    public long firstStrikeSourceSequence;

    public BattleActionOrderCandidate(
        BattleExecutionItemType executionType,
        BattleEnemyIntent enemyIntent,
        BattleActionSlot actionSlot,
        CharacterData orderingActor,
        BattleExecutionPriorityTier priorityTier,
        int effectiveSpeed,
        int responsePriority,
        int actionSlotOrder,
        int actorPositionOrder,
        int stableOrder,
        long actionAssignmentSequence,
        long firstStrikeSourceSequence
    )
    {
        this.executionType = executionType;
        this.enemyIntent = enemyIntent;
        this.actionSlot = actionSlot;
        this.orderingActor = orderingActor;
        this.priorityTier = priorityTier;
        this.effectiveSpeed = effectiveSpeed;
        this.responsePriority = responsePriority;
        this.actionSlotOrder = actionSlotOrder;
        this.actorPositionOrder = actorPositionOrder;
        this.stableOrder = stableOrder;
        this.actionAssignmentSequence = actionAssignmentSequence;
        this.firstStrikeSourceSequence = firstStrikeSourceSequence;
    }
}

// BattleActionOrderResolver = Planning / Execution 共用的纯排序核心。
public static class BattleActionOrderResolver
{
    public static List<BattleActionOrderCandidate> Resolve(
        IReadOnlyList<BattleActionSlot> actionSlots,
        IReadOnlyList<BattleEnemyIntent> intentQueue,
        BattleRuntimeState runtimeState
    )
    {
        List<BattleActionOrderCandidate> candidates =
            new List<BattleActionOrderCandidate>();
        List<CharacterData> fallbackBattleOrder =
            BuildFallbackBattleOrder(actionSlots, intentQueue);
        int stableOrder = 1;

        if (intentQueue != null)
        {
            foreach (BattleEnemyIntent intent in intentQueue)
            {
                if (intent == null)
                {
                    continue;
                }

                if (!intent.isResponded && IsReactiveEnemyDefensiveIntent(intent))
                {
                    continue;
                }

                BattleActionSlot responseSlot = intent.isResponded
                    ? FindValidResponseSlot(actionSlots, intent)
                    : null;
                BattleExecutionItemType itemType = responseSlot != null
                    ? BattleExecutionItemType.RespondedEnemyIntent
                    : BattleExecutionItemType.UnrespondedEnemyIntent;

                int responseActorSpeed = responseSlot != null
                    ? GetSpeed(responseSlot.actor)
                    : int.MinValue;
                int enemySpeed = GetSpeed(intent.enemy);
                bool responseActorDrivesOrdering = responseSlot != null &&
                    responseActorSpeed > enemySpeed;
                CharacterData orderingActor = responseActorDrivesOrdering
                    ? responseSlot.actor
                    : intent.enemy;
                int orderingSlot = responseActorDrivesOrdering
                    ? responseSlot.slotIndex
                    : intent.enemySlotIndex;
                BattleExecutionPriorityTier priorityTier = GetPriorityTier(
                    responseSlot,
                    intent
                );

                candidates.Add(
                    new BattleActionOrderCandidate(
                        itemType,
                        intent,
                        responseSlot,
                        orderingActor,
                        priorityTier,
                        responseSlot != null
                            ? System.Math.Max(responseActorSpeed, enemySpeed)
                            : enemySpeed,
                        responseSlot != null && responseActorSpeed == enemySpeed
                            ? 0
                            : 1,
                        orderingSlot,
                        GetBattlePositionIndex(
                            runtimeState,
                            fallbackBattleOrder,
                            orderingActor
                        ),
                        stableOrder,
                        responseSlot != null
                            ? responseSlot.assignmentSequence
                            : 0,
                        GetFirstStrikeSourceSequence(responseSlot)
                    )
                );
                stableOrder++;
            }
        }

        if (actionSlots != null)
        {
            foreach (BattleActionSlot slot in actionSlots)
            {
                if (!IsActionSlotReady(slot) ||
                    slot.slotType != BattleActionSlotType.FreeAction ||
                    slot.isUsed ||
                    slot.actor.IsDead())
                {
                    continue;
                }

                candidates.Add(
                    new BattleActionOrderCandidate(
                        BattleExecutionItemType.FreeAction,
                        null,
                        slot,
                        slot.actor,
                        GetPriorityTier(slot, null),
                        GetSpeed(slot.actor),
                        1,
                        slot.slotIndex,
                        GetBattlePositionIndex(
                            runtimeState,
                            fallbackBattleOrder,
                            slot.actor
                        ),
                        stableOrder,
                        slot.assignmentSequence,
                        GetFirstStrikeSourceSequence(slot)
                    )
                );
                stableOrder++;
            }
        }

        candidates.Sort(CompareCandidates);
        return candidates;
    }

    // ExecutionPlanManager 在正式执行边界使用该纯检查决定是否恢复失效响应状态。
    public static bool HasValidResponseSlot(
        IReadOnlyList<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent
    )
    {
        return FindValidResponseSlot(actionSlots, enemyIntent) != null;
    }

    static bool IsReactiveEnemyDefensiveIntent(BattleEnemyIntent intent)
    {
        string cardType = intent != null && intent.enemyCardState != null &&
            intent.enemyCardState.cardData != null
                ? intent.enemyCardState.cardData.cardType
                : string.Empty;
        return cardType == CardType.Defense || cardType == CardType.Dodge;
    }

    static bool IsActionSlotReady(BattleActionSlot slot)
    {
        return slot != null &&
            !slot.IsEmpty() &&
            slot.actor != null &&
            slot.cardState != null &&
            slot.cardState.cardData != null;
    }

    static BattleActionSlot FindValidResponseSlot(
        IReadOnlyList<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent
    )
    {
        if (actionSlots == null || enemyIntent == null)
        {
            return null;
        }

        BattleActionSlot slot = null;
        foreach (BattleActionSlot candidate in actionSlots)
        {
            if (candidate != null &&
                object.ReferenceEquals(candidate.enemyIntent, enemyIntent))
            {
                slot = candidate;
                break;
            }
        }

        if (!IsActionSlotReady(slot) ||
            slot.slotType != BattleActionSlotType.RespondToEnemyIntent ||
            slot.isUsed ||
            slot.actor.IsDead())
        {
            return null;
        }

        return slot;
    }

    static BattleExecutionPriorityTier GetPriorityTier(
        BattleActionSlot actionSlot,
        BattleEnemyIntent enemyIntent
    )
    {
        bool actionCardIsFirstStrike = actionSlot != null &&
            actionSlot.cardState != null &&
            actionSlot.cardState.HasTrait(BattleCardTrait.FirstStrike);
        bool enemyCardIsFirstStrike = enemyIntent != null &&
            enemyIntent.enemyCardState != null &&
            enemyIntent.enemyCardState.HasTrait(BattleCardTrait.FirstStrike);
        return actionCardIsFirstStrike || enemyCardIsFirstStrike
            ? BattleExecutionPriorityTier.FirstStrike
            : BattleExecutionPriorityTier.Normal;
    }

    static long GetFirstStrikeSourceSequence(BattleActionSlot actionSlot)
    {
        return actionSlot != null &&
            actionSlot.cardState != null &&
            actionSlot.cardState.HasTrait(BattleCardTrait.FirstStrike)
                ? actionSlot.assignmentSequence
                : 0;
    }

    static int GetSpeed(CharacterData character)
    {
        return character != null ? character.GetCurrentSpeed() : int.MinValue;
    }

    static int GetBattlePositionIndex(
        BattleRuntimeState runtimeState,
        List<CharacterData> fallbackBattleOrder,
        CharacterData character
    )
    {
        if (runtimeState != null)
        {
            int runtimePosition = runtimeState.GetBattlePositionIndex(character);
            if (runtimePosition != int.MaxValue)
            {
                return runtimePosition;
            }
        }

        if (character == null || fallbackBattleOrder == null)
        {
            return int.MaxValue;
        }

        for (int index = 0; index < fallbackBattleOrder.Count; index++)
        {
            if (object.ReferenceEquals(fallbackBattleOrder[index], character))
            {
                return index + 1;
            }
        }

        return int.MaxValue;
    }

    static List<CharacterData> BuildFallbackBattleOrder(
        IReadOnlyList<BattleActionSlot> actionSlots,
        IReadOnlyList<BattleEnemyIntent> intentQueue
    )
    {
        List<CharacterData> battleOrder = new List<CharacterData>();

        if (actionSlots != null)
        {
            foreach (BattleActionSlot slot in actionSlots)
            {
                if (slot == null)
                {
                    continue;
                }

                AddCharacterReferenceIfMissing(battleOrder, slot.owner);
                AddCharacterReferenceIfMissing(battleOrder, slot.actor);
            }
        }

        if (intentQueue != null)
        {
            foreach (BattleEnemyIntent intent in intentQueue)
            {
                if (intent == null)
                {
                    continue;
                }

                AddCharacterReferenceIfMissing(battleOrder, intent.enemy);
                AddCharacterReferenceIfMissing(
                    battleOrder,
                    intent.originalTargetCharacter
                );
                AddCharacterReferenceIfMissing(
                    battleOrder,
                    intent.actualTargetCharacter
                );
            }
        }

        return battleOrder;
    }

    static void AddCharacterReferenceIfMissing(
        List<CharacterData> characters,
        CharacterData character
    )
    {
        if (characters == null || character == null)
        {
            return;
        }

        foreach (CharacterData existingCharacter in characters)
        {
            if (object.ReferenceEquals(existingCharacter, character))
            {
                return;
            }
        }

        characters.Add(character);
    }

    static int CompareCandidates(
        BattleActionOrderCandidate left,
        BattleActionOrderCandidate right
    )
    {
        int result = left.priorityTier.CompareTo(right.priorityTier);
        if (result != 0)
        {
            return result;
        }

        if (left.priorityTier == BattleExecutionPriorityTier.FirstStrike &&
            right.priorityTier == BattleExecutionPriorityTier.FirstStrike)
        {
            result = right.firstStrikeSourceSequence.CompareTo(
                left.firstStrikeSourceSequence
            );
            if (result != 0)
            {
                return result;
            }
        }

        result = right.effectiveSpeed.CompareTo(left.effectiveSpeed);
        if (result != 0)
        {
            return result;
        }

        result = left.responsePriority.CompareTo(right.responsePriority);
        if (result != 0)
        {
            return result;
        }

        if (left.responsePriority == 0 && right.responsePriority == 0)
        {
            result = right.actionAssignmentSequence.CompareTo(
                left.actionAssignmentSequence
            );
            if (result != 0)
            {
                return result;
            }
        }

        result = left.actionSlotOrder.CompareTo(right.actionSlotOrder);
        if (result != 0)
        {
            return result;
        }

        result = left.actorPositionOrder.CompareTo(right.actorPositionOrder);
        if (result != 0)
        {
            return result;
        }

        return left.stableOrder.CompareTo(right.stableOrder);
    }
}
