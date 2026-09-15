using System.Collections.Generic;

// BattlePlanningOrderSnapshot = Planning 阶段只读的行动顺序快照。
// 快照只保存 Resolver 的结果，不回写槽位、意图或其他运行时状态。
public sealed class BattlePlanningOrderSnapshot
{
    readonly Dictionary<BattleActionSlot, int> actionSlotDisplayOrders;
    readonly Dictionary<BattleEnemyIntent, int> enemyIntentDisplayOrders;

    BattlePlanningOrderSnapshot(
        IReadOnlyList<BattleActionSlot> actionSlots,
        IReadOnlyList<BattleEnemyIntent> intentQueue,
        IReadOnlyList<BattleActionOrderCandidate> candidates
    )
    {
        actionSlotDisplayOrders = new Dictionary<BattleActionSlot, int>();
        enemyIntentDisplayOrders = new Dictionary<BattleEnemyIntent, int>();

        SeedPassiveAndUnrespondedDisplayOrders(actionSlots, intentQueue);

        int displayOrder = 1;
        if (candidates == null)
        {
            return;
        }

        foreach (BattleActionOrderCandidate candidate in candidates)
        {
            if (candidate == null)
            {
                continue;
            }

            if (IsPositiveDisplayCandidate(candidate))
            {
                if (candidate.actionSlot != null)
                {
                    actionSlotDisplayOrders[candidate.actionSlot] = displayOrder;
                }

                if (candidate.enemyIntent != null)
                {
                    enemyIntentDisplayOrders[candidate.enemyIntent] = displayOrder;
                }

                displayOrder++;
                continue;
            }

            if (candidate.actionSlot != null)
            {
                actionSlotDisplayOrders[candidate.actionSlot] = 0;
            }

            if (candidate.enemyIntent != null)
            {
                enemyIntentDisplayOrders[candidate.enemyIntent] = 0;
            }
        }
    }

    public static BattlePlanningOrderSnapshot Create(
        IReadOnlyList<BattleActionSlot> actionSlots,
        IReadOnlyList<BattleEnemyIntent> intentQueue,
        BattleRuntimeState runtimeState
    )
    {
        List<BattleActionOrderCandidate> candidates =
            BattleActionOrderResolver.Resolve(
                actionSlots,
                intentQueue,
                runtimeState
            );
        return new BattlePlanningOrderSnapshot(
            actionSlots,
            intentQueue,
            candidates
        );
    }

    public int? GetActionSlotDisplayOrder(BattleActionSlot slot)
    {
        int displayOrder;
        return slot != null &&
            actionSlotDisplayOrders.TryGetValue(slot, out displayOrder)
                ? displayOrder
                : (int?)null;
    }

    public int? GetEnemyIntentDisplayOrder(BattleEnemyIntent intent)
    {
        int displayOrder;
        return intent != null &&
            enemyIntentDisplayOrders.TryGetValue(intent, out displayOrder)
                ? displayOrder
                : (int?)null;
    }

    static bool IsPositiveDisplayCandidate(
        BattleActionOrderCandidate candidate
    )
    {
        if (candidate.executionType ==
            BattleExecutionItemType.RespondedEnemyIntent)
        {
            return true;
        }

        if (candidate.executionType == BattleExecutionItemType.FreeAction)
        {
            string cardType = candidate.actionSlot != null &&
                candidate.actionSlot.cardState != null &&
                candidate.actionSlot.cardState.cardData != null
                    ? candidate.actionSlot.cardState.cardData.cardType
                    : string.Empty;
            return cardType != CardType.Defense && cardType != CardType.Dodge;
        }

        string enemyCardType = candidate.enemyIntent != null &&
            candidate.enemyIntent.enemyCardState != null &&
            candidate.enemyIntent.enemyCardState.cardData != null
                ? candidate.enemyIntent.enemyCardState.cardData.cardType
                : string.Empty;
        return enemyCardType == CardType.Attack;
    }

    void SeedPassiveAndUnrespondedDisplayOrders(
        IReadOnlyList<BattleActionSlot> actionSlots,
        IReadOnlyList<BattleEnemyIntent> intentQueue
    )
    {
        if (actionSlots != null)
        {
            foreach (BattleActionSlot slot in actionSlots)
            {
                if (slot == null || slot.cardState == null ||
                    slot.cardState.cardData == null)
                {
                    continue;
                }

                string cardType = slot.cardState.cardData.cardType;
                if (cardType == CardType.Defense || cardType == CardType.Dodge)
                {
                    actionSlotDisplayOrders[slot] = 0;
                }
            }
        }

        if (intentQueue != null)
        {
            foreach (BattleEnemyIntent intent in intentQueue)
            {
                if (intent == null || intent.isResponded ||
                    intent.enemyCardState == null ||
                    intent.enemyCardState.cardData == null)
                {
                    continue;
                }

                string cardType = intent.enemyCardState.cardData.cardType;
                if (cardType == CardType.Defense || cardType == CardType.Dodge)
                {
                    enemyIntentDisplayOrders[intent] = 0;
                }
            }
        }
    }
}
