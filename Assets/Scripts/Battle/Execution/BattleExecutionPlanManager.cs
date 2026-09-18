// 脚本中文说明：战斗执行计划管理器。负责根据敌人意图和行动槽位生成执行计划，并打印计划内容。
using System.Collections.Generic;
using UnityEngine;

// BattleExecutionPlanManager = 战斗执行计划管理器
// Manager = 管理器，这里负责创建和打印执行计划，不负责执行计划。
public static class BattleExecutionPlanManager
{
    // CreateBasicExecutionPlan = 创建基础执行计划
    // Create = 创建，Basic = 基础，ExecutionPlan = 执行计划。
    public static BattleExecutionPlan CreateBasicExecutionPlan(
        // actionSlots = 玩家行动槽位列表
        // BattleActionSlot = 战斗行动槽位，记录玩家安排了哪张卡、是否响应敌人意图。
        List<BattleActionSlot> actionSlots,

        // intentQueue = 敌人意图队列
        // BattleEnemyIntent = 战斗敌人意图，记录敌人要攻击的角色和槽位。
        List<BattleEnemyIntent> intentQueue
    )
    {
        // 先创建一个空执行计划，后面再逐条加入执行项。
        BattleExecutionPlan executionPlan = new BattleExecutionPlan();

        // 没有敌人意图时，执行计划保持为空。
        if (intentQueue == null || intentQueue.Count == 0)
        {
            return executionPlan;
        }

        // order = 执行顺序编号
        // 当前从 1 开始，数字越小越先执行。
        int order = 1;

        // 第一轮：先把“已经被玩家响应”的敌人意图加入执行计划。
        // 当前设计倾向是：响应敌人意图优先进入执行队列。
        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent != null && intent.isResponded)
            {
                // 根据敌人意图，找到绑定这个意图的玩家行动槽位。
                BattleActionSlot actionSlot = FindValidResponseSlot(actionSlots, intent);

                if (actionSlot == null)
                {
                    Debug.LogWarning(
                        "敌人意图" +
                        intent.intentOrder +
                        " 标记为已响应但没有有效主响应槽位，已恢复原目标并按 Unresponded 生成"
                    );
                    intent.ResetResponseState();
                    continue;
                }

                BattleExecutionItem item = new BattleExecutionItem(
                    order,
                    BattleExecutionItemType.RespondedEnemyIntent,
                    intent,
                    actionSlot
                );
                PopulatePlannedInteractionType(item);
                executionPlan.AddItem(item);

                order++;
            }
        }

        // 第二轮：再把“无人响应”的敌人意图加入执行计划。
        // 这样无人响应攻击会排在已响应意图之后。
        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent != null && !intent.isResponded)
            {
                BattleExecutionItem item = new BattleExecutionItem(
                    order,
                    BattleExecutionItemType.UnrespondedEnemyIntent,
                    intent,
                    null,
                    BattleGuardSelectionManager.CollectGuardCandidates(actionSlots, intent)
                );
                PopulatePlannedInteractionType(item);
                executionPlan.AddItem(item);

                order++;
            }
        }

        return executionPlan;
    }

    // CreateSpeedBasedExecutionPlan = 创建统一速度排序版执行计划
    public static BattleExecutionPlan CreateSpeedBasedExecutionPlan(
        List<BattleActionSlot> actionSlots,
        List<BattleEnemyIntent> intentQueue
    )
    {
        return CreateSpeedBasedExecutionPlan(actionSlots, intentQueue, null);
    }

    public static BattleExecutionPlan CreateSpeedBasedExecutionPlan(
        List<BattleActionSlot> actionSlots,
        List<BattleEnemyIntent> intentQueue,
        BattleRuntimeState runtimeState
    )
    {
        BattleExecutionPlan executionPlan = new BattleExecutionPlan();
        List<BattleActionOrderCandidate> orderedCandidates =
            BattleActionOrderResolver.Resolve(
                actionSlots,
                intentQueue,
                runtimeState
            );

        ResetInvalidRespondedIntentsForExecution(actionSlots, intentQueue);

        for (int index = 0; index < orderedCandidates.Count; index++)
        {
            BattleActionOrderCandidate candidate = orderedCandidates[index];
            List<BattleActionSlot> passiveGuardCandidates =
                candidate.executionType ==
                BattleExecutionItemType.UnrespondedEnemyIntent
                    ? BattleGuardSelectionManager.CollectGuardCandidates(
                        actionSlots,
                        candidate.enemyIntent
                    )
                    : null;
            BattleExecutionItem item = new BattleExecutionItem(
                index + 1,
                candidate.executionType,
                candidate.enemyIntent,
                candidate.actionSlot,
                passiveGuardCandidates
            );
            item.orderingActor = candidate.orderingActor;
            item.priorityTier = candidate.priorityTier;
            item.effectiveSpeed = candidate.effectiveSpeed;
            item.responsePriority = candidate.responsePriority;
            item.actionSlotOrder = candidate.actionSlotOrder;
            item.actorPositionOrder = candidate.actorPositionOrder;
            item.stableOrder = candidate.stableOrder;
            item.actionAssignmentSequence = candidate.actionAssignmentSequence;
            item.firstStrikeSourceSequence = candidate.firstStrikeSourceSequence;
            PopulatePlannedInteractionType(item);
            executionPlan.AddItem(item);
        }

        return executionPlan;
    }

    public static BattlePlanningOrderSnapshot CreatePlanningOrderSnapshot(
        List<BattleActionSlot> actionSlots,
        List<BattleEnemyIntent> intentQueue,
        BattleRuntimeState runtimeState
    )
    {
        return BattlePlanningOrderSnapshot.Create(
            actionSlots,
            intentQueue,
            runtimeState
        );
    }

    static void ResetInvalidRespondedIntentsForExecution(
        List<BattleActionSlot> actionSlots,
        List<BattleEnemyIntent> intentQueue
    )
    {
        if (intentQueue == null)
        {
            return;
        }

        foreach (BattleEnemyIntent intent in intentQueue)
        {
            if (intent == null ||
                !intent.isResponded ||
                BattleActionOrderResolver.HasValidResponseSlot(
                    actionSlots,
                    intent
                ))
            {
                continue;
            }

            Debug.LogWarning(
                "敌人意图" +
                intent.intentOrder +
                " 标记为已响应但没有有效主响应槽位，已恢复原目标并按 Unresponded 生成"
            );
            intent.ResetResponseState();
        }
    }

    // PrintExecutionPlan = 打印执行计划
    // Print = 打印，方便在 Console 里确认顺序。
    public static void PrintExecutionPlan(BattleExecutionPlan executionPlan)
    {
        Debug.Log("===== 当前 BattleExecutionPlan =====");
        Debug.Log("提示：当前只生成并打印执行计划，不执行任何 item");

        if (executionPlan == null || executionPlan.executionItems == null || executionPlan.executionItems.Count == 0)
        {
            Debug.Log("当前 BattleExecutionPlan 没有执行项");
            Debug.Log("ExecutionPlan 项数量：0");
            return;
        }

        foreach (BattleExecutionItem item in executionPlan.executionItems)
        {
            if (item == null)
            {
                continue;
            }

            // 已响应敌人意图：打印玩家槽位如何处理这个敌人意图。
            if (item.executionType == BattleExecutionItemType.RespondedEnemyIntent)
            {
                PrintRespondedEnemyIntentItem(item);
                continue;
            }

            // 无人响应敌人意图：打印敌人意图未来会按实际目标执行。
            if (item.executionType == BattleExecutionItemType.UnrespondedEnemyIntent)
            {
                PrintUnrespondedEnemyIntentItem(item);
                continue;
            }

            // 自由行动：打印玩家自由行动的基础信息。
            if (item.executionType == BattleExecutionItemType.FreeAction)
            {
                PrintFreeActionItem(item);
            }
        }

        Debug.Log("ExecutionPlan 项数量：" + executionPlan.executionItems.Count);
    }

    // PrintFreeActionItem = 打印自由行动执行项
    static void PrintFreeActionItem(BattleExecutionItem item)
    {
        if (item.actionSlot == null)
        {
            Debug.Log(
                item.order +
                ". FreeAction：玩家自由行动，执行时将交给 BattleResolver.ResolveFreeAction(...) 处理，但当前缺少行动槽位"
            );
            return;
        }

        Debug.Log(
            item.order +
            ". FreeAction：玩家自由行动，执行时将交给 BattleResolver.ResolveFreeAction(...) 处理，槽位：" +
            item.actionSlot.GetActorName() +
            " 槽位" +
            item.actionSlot.slotIndex +
            "，卡牌：" +
            item.actionSlot.GetCardName() +
            "，目标：" +
            item.actionSlot.GetTargetName() +
            GetSortMetadataText(item)
        );
    }

    // PrintRespondedEnemyIntentItem = 打印已响应敌人意图执行项
    // item = 执行计划中的一条 BattleExecutionItem。
    static void PrintRespondedEnemyIntentItem(BattleExecutionItem item)
    {
        if (item.enemyIntent == null)
        {
            Debug.Log(item.order + ". RespondedEnemyIntent：敌人意图为空");
            return;
        }

        if (item.actionSlot == null)
        {
            Debug.Log(
                item.order +
                ". RespondedEnemyIntent：敌人意图" +
                item.enemyIntent.intentOrder +
                " 已响应，但未找到绑定槽位，当前实际目标：" +
                item.enemyIntent.GetActualTargetSlotText()
            );
            return;
        }

        int passiveGuardCandidateCount = item.passiveGuardCandidates != null
            ? item.passiveGuardCandidates.Count
            : 0;

        Debug.Log(
            item.order +
            ". RespondedEnemyIntent：" +
            item.actionSlot.GetActorName() +
            " 槽位" +
            item.actionSlot.slotIndex +
            " 处理 敌人意图" +
            item.enemyIntent.intentOrder +
            "，当前实际目标：" +
            item.enemyIntent.GetActualTargetSlotText() +
            "，被动守备候选数：" +
            passiveGuardCandidateCount +
            GetSortMetadataText(item)
        );
    }

    // PrintUnrespondedEnemyIntentItem = 打印无人响应敌人意图执行项
    // item = 执行计划中的一条 BattleExecutionItem。
    static void PrintUnrespondedEnemyIntentItem(BattleExecutionItem item)
    {
        if (item.enemyIntent == null)
        {
            Debug.Log(item.order + ". UnrespondedEnemyIntent：敌人意图为空");
            return;
        }

        int passiveGuardCandidateCount = item.passiveGuardCandidates != null
            ? item.passiveGuardCandidates.Count
            : 0;

        Debug.Log(
            item.order +
            ". UnrespondedEnemyIntent：敌人意图" +
            item.enemyIntent.intentOrder +
            " 未响应，未来按 actualTarget 执行，目标：" +
            item.enemyIntent.GetActualTargetSlotText() +
            "，被动守备候选数：" +
            passiveGuardCandidateCount +
            GetSortMetadataText(item)
        );
    }

    static string GetSortMetadataText(BattleExecutionItem item)
    {
        if (item == null)
        {
            return "";
        }

        return
            "，排序键：[速度=" + item.effectiveSpeed +
            "，响应优先=" + item.responsePriority +
            "，Interaction=" + item.interactionType +
            "，槽位=" + item.actionSlotOrder +
            "，站位=" + item.actorPositionOrder +
            "，稳定序=" + item.stableOrder +
            "]";
    }

    // 旧兼容方法：正式计划生成已不再为 Responded item 收集后备守备。
    static List<BattleActionSlot> CollectRespondedPassiveGuardCandidates(
        List<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent,
        BattleActionSlot responseSlot
    )
    {
        if (!ShouldCollectPassiveGuardForRespondedItem(enemyIntent, responseSlot))
        {
            return new List<BattleActionSlot>();
        }

        return CollectPassiveGuardCandidates(actionSlots, enemyIntent, true);
    }

    // ShouldCollectPassiveGuardForRespondedItem = 判断 Responded item 是否需要提前携带被动守备候选
    // 当前只有 Attack vs Attack EnemyWin 分支会在正常 Responded 结算中使用这些候选。
    // Defense / Dodge 响应者死亡或不可用时，由 Executor 在回落 Unresponded 前按运行时状态重新收集候选。
    static bool ShouldCollectPassiveGuardForRespondedItem(
        BattleEnemyIntent enemyIntent,
        BattleActionSlot responseSlot
    )
    {
        if (enemyIntent == null || responseSlot == null)
        {
            return false;
        }

        if (responseSlot.slotType != BattleActionSlotType.RespondToEnemyIntent)
        {
            return false;
        }

        if (responseSlot.cardState == null || responseSlot.cardState.cardData == null)
        {
            return false;
        }

        if (enemyIntent.enemyCardState == null || enemyIntent.enemyCardState.cardData == null)
        {
            return false;
        }

        if (enemyIntent.enemyCardState.cardData.cardType != CardType.Attack)
        {
            return false;
        }

        string playerCardType = responseSlot.cardState.cardData.cardType;

        return playerCardType == CardType.Attack;
    }

    // CollectPassiveGuardCandidates = 为敌人意图收集被动守备候选
    // 这里只保存候选引用，不在计划生成阶段最终决定使用哪一个。
    static List<BattleActionSlot> CollectPassiveGuardCandidates(
        List<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent,
        bool allowDodge
    )
    {
        if (actionSlots == null || enemyIntent == null || enemyIntent.actualTargetCharacter == null)
        {
            return new List<BattleActionSlot>();
        }

        return CollectPassiveGuardCandidatesForTarget(
            actionSlots,
            enemyIntent.actualTargetCharacter,
            allowDodge
        );
    }

    internal static List<BattleActionSlot> CollectPassiveGuardCandidatesForTarget(
        IReadOnlyList<BattleActionSlot> actionSlots,
        CharacterData target,
        bool allowDodge
    )
    {
        List<BattleActionSlot> candidates = new List<BattleActionSlot>();

        if (actionSlots == null || target == null)
        {
            return candidates;
        }

        foreach (BattleActionSlot slot in actionSlots)
        {
            if (!IsPassiveGuardCandidateForTarget(slot, target, allowDodge))
            {
                continue;
            }

            candidates.Add(slot);
        }

        candidates.Sort(CompareActionSlotIndex);
        return candidates;
    }

    // IsPassiveGuardCandidateForTarget = 判断槽位是否是目标角色的被动守备候选
    static bool IsPassiveGuardCandidateForTarget(BattleActionSlot slot, CharacterData target, bool allowDodge)
    {
        if (slot == null || target == null)
        {
            return false;
        }

        if (target.IsDead())
        {
            return false;
        }

        if (slot.slotType != BattleActionSlotType.PassiveGuard)
        {
            return false;
        }

        if (slot.isUsed)
        {
            return false;
        }

        if (slot.owner == null || slot.actor == null || slot.target == null)
        {
            return false;
        }

        if (slot.owner.IsDead() || slot.actor.IsDead() || slot.target.IsDead())
        {
            return false;
        }

        if (!object.ReferenceEquals(slot.owner, target) ||
            !object.ReferenceEquals(slot.actor, target) ||
            !object.ReferenceEquals(slot.target, target))
        {
            return false;
        }

        if (slot.cardState == null || slot.cardState.cardData == null)
        {
            return false;
        }

        if (slot.cardState.cardData.cardType == CardType.Defense)
        {
            return true;
        }

        return allowDodge && slot.cardState.cardData.cardType == CardType.Dodge;
    }

    // CompareActionSlotIndex = 按角色内槽位编号升序排序
    static int CompareActionSlotIndex(BattleActionSlot left, BattleActionSlot right)
    {
        if (left == null && right == null)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        return left.slotIndex.CompareTo(right.slotIndex);
    }

    // IsActionSlotReady = 判断槽位是否有可加入计划的行动
    static bool IsActionSlotReady(BattleActionSlot slot)
    {
        return
            slot != null &&
            !slot.IsEmpty() &&
            slot.actor != null &&
            slot.cardState != null &&
            slot.cardState.cardData != null;
    }

    static BattleActionSlot FindValidResponseSlot(
        List<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent
    )
    {
        BattleActionSlot slot = FindSlotByEnemyIntent(actionSlots, enemyIntent);

        if (!IsActionSlotReady(slot) ||
            slot.slotType != BattleActionSlotType.RespondToEnemyIntent ||
            slot.isUsed ||
            slot.actor.IsDead())
        {
            return null;
        }

        return slot;
    }

    // 只记录计划生成时已经确定的双方卡牌，不参与运行时 Guard / Dodge 的替换选择。
    static void PopulatePlannedInteractionType(BattleExecutionItem item)
    {
        if (item == null)
        {
            return;
        }

        if (item.executionType == BattleExecutionItemType.RespondedEnemyIntent)
        {
            item.interactionType = BattleInteractionClassifier.Classify(
                item.actionSlot != null ? item.actionSlot.cardState : null,
                item.enemyIntent != null ? item.enemyIntent.enemyCardState : null
            );
            return;
        }

        if (item.executionType == BattleExecutionItemType.UnrespondedEnemyIntent)
        {
            item.interactionType = BattleInteractionClassifier.Classify(
                item.enemyIntent != null ? item.enemyIntent.enemyCardState : null,
                null
            );
            return;
        }

        if (item.executionType == BattleExecutionItemType.FreeAction)
        {
            item.interactionType = BattleInteractionClassifier.Classify(
                item.actionSlot != null ? item.actionSlot.cardState : null,
                null
            );
            return;
        }

        item.interactionType = BattleInteractionType.NoInteraction;
    }

    // FindSlotByEnemyIntent = 根据敌人意图查找绑定的行动槽位
    // actionSlots = 所有玩家行动槽位。
    // enemyIntent = 要查找的敌人意图。
    static BattleActionSlot FindSlotByEnemyIntent(
        List<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent
    )
    {
        if (actionSlots == null || enemyIntent == null)
        {
            return null;
        }

        foreach (BattleActionSlot slot in actionSlots)
        {
            // ReferenceEquals = 判断是不是同一个对象实例。
            // 这里不是比较内容相同，而是确认槽位绑定的 enemyIntent 就是传进来的那一个。
            if (slot != null && object.ReferenceEquals(slot.enemyIntent, enemyIntent))
            {
                return slot;
            }
        }

        return null;
    }
}

public enum BattleGuardSelectionType
{
    None,
    ContinuousDodge,
    EnemySpecificGuard,
    PassiveGuard
}

public sealed class BattleGuardSelectionResult
{
    public BattleGuardSelectionType selectionType;
    public BattleActionSlot slot;

    public BattleGuardSelectionResult(
        BattleGuardSelectionType selectionType,
        BattleActionSlot slot
    )
    {
        this.selectionType = selectionType;
        this.slot = slot;
    }
}

// BattleGuardSelectionManager = 无人响应敌人攻击的唯一守备选择器。
// 守备槽位不独立进入执行队列，只在敌人攻击真正执行时按优先级重新验证。
public static class BattleGuardSelectionManager
{
    public static BattleEnemyIntent SelectEnemyDefensiveIntentForFreeAttack(
        IReadOnlyList<BattleEnemyIntent> intents,
        BattleActionSlot freeActionSlot
    )
    {
        CardTestData selectedCardData = freeActionSlot != null &&
            freeActionSlot.cardState != null
                ? freeActionSlot.cardState.cardData
                : null;
        Debug.Log(
            "[ReactiveGuardDiag] SELECT BEGIN\n" +
            "actor=" + (freeActionSlot != null && freeActionSlot.actor != null
                ? freeActionSlot.actor.characterName
                : "NONE") + "\n" +
            "actorSpeed=" + (freeActionSlot != null && freeActionSlot.actor != null
                ? freeActionSlot.actor.GetCurrentSpeed().ToString()
                : "NONE") + "\n" +
            "slotIndex=" + (freeActionSlot != null
                ? freeActionSlot.slotIndex.ToString()
                : "NONE") + "\n" +
            "cardID=" + (selectedCardData != null
                ? selectedCardData.cardID
                : "NONE") + "\n" +
            "cardName=" + (selectedCardData != null
                ? selectedCardData.cardName
                : "NONE") + "\n" +
            "cardType=" + (selectedCardData != null
                ? selectedCardData.cardType
                : "NONE") + "\n" +
            "FirstStrike=" + (freeActionSlot != null &&
                freeActionSlot.cardState != null &&
                freeActionSlot.cardState.HasTrait(BattleCardTrait.FirstStrike)) + "\n" +
            "slotType=" + (freeActionSlot != null
                ? freeActionSlot.slotType.ToString()
                : "NONE") + "\n" +
            "placementType=" + (freeActionSlot != null
                ? freeActionSlot.placementType.ToString()
                : "NONE") + "\n" +
            "target=" + (freeActionSlot != null && freeActionSlot.target != null
                ? freeActionSlot.target.characterName
                : "NONE") + "\n" +
            "intentsCount=" + (intents != null
                ? intents.Count.ToString()
                : "0")
        );
        if (intents == null || freeActionSlot == null ||
            freeActionSlot.slotType != BattleActionSlotType.FreeAction ||
            freeActionSlot.actor == null || freeActionSlot.actor.IsDead() ||
            freeActionSlot.target == null || freeActionSlot.target.IsDead() ||
            freeActionSlot.cardState == null || freeActionSlot.cardState.cardData == null ||
            freeActionSlot.cardState.cardData.cardType != CardType.Attack)
        {
            Debug.Log("[ReactiveGuardDiag] SELECT RESULT selected=NONE");
            return null;
        }

        BattleEnemyIntent selected = null;
        foreach (BattleEnemyIntent intent in intents)
        {
            if (!IsValidEnemyReactiveGuard(intent, freeActionSlot))
            {
                continue;
            }

            if (selected == null || intent.enemySlotIndex < selected.enemySlotIndex ||
                (intent.enemySlotIndex == selected.enemySlotIndex &&
                    intent.intentOrder < selected.intentOrder))
            {
                selected = intent;
            }
        }
        if (selected == null)
        {
            Debug.Log("[ReactiveGuardDiag] SELECT RESULT selected=NONE");
        }
        else
        {
            Debug.Log(
                "[ReactiveGuardDiag] SELECT RESULT " +
                "intentOrder=" + selected.intentOrder +
                " enemySlotIndex=" + selected.enemySlotIndex +
                " enemy=" + (selected.enemy != null
                    ? selected.enemy.characterName
                    : "NONE") +
                " cardID=" + selected.enemyCardState.cardData.cardID +
                " cardType=" + selected.enemyCardState.cardData.cardType +
                " cardName=" + selected.enemyCardState.cardData.cardName
            );
        }
        return selected;
    }

    static bool IsValidEnemyReactiveGuard(
        BattleEnemyIntent intent,
        BattleActionSlot freeActionSlot
    )
    {
        CardEligibilityResult eligibility = null;
        if (intent != null && intent.enemyCardState != null &&
            intent.enemyCardState.cardData != null &&
            (intent.enemyCardState.cardData.cardType == CardType.Defense ||
             intent.enemyCardState.cardData.cardType == CardType.Dodge))
        {
            eligibility = BattleCardManager.EvaluateCardEligibility(
                intent.enemy,
                freeActionSlot != null ? freeActionSlot.actor : null,
                intent.enemyCardState
            );
            Debug.Log(
                "[ReactiveGuardDiag] CANDIDATE\n" +
                "intentOrder=" + intent.intentOrder + "\n" +
                "enemySlotIndex=" + intent.enemySlotIndex + "\n" +
                "enemy=" + (intent.enemy != null
                    ? intent.enemy.characterName
                    : "NONE") + "\n" +
                "cardID=" + intent.enemyCardState.cardData.cardID + "\n" +
                "cardName=" + intent.enemyCardState.cardData.cardName + "\n" +
                "cardType=" + intent.enemyCardState.cardData.cardType + "\n" +
                "isResponded=" + intent.isResponded + "\n" +
                "isConsumedAsReactiveGuard=" + intent.isConsumedAsReactiveGuard + "\n" +
                "enemyIsDead=" + (intent.enemy != null && intent.enemy.IsDead()) + "\n" +
                "freeActionTargetIsNull=" + (freeActionSlot == null ||
                    freeActionSlot.target == null) + "\n" +
                "enemyTargetReferenceEquals=" + (freeActionSlot != null &&
                    object.ReferenceEquals(intent.enemy, freeActionSlot.target)) + "\n" +
                "eligibility.isEligible=" + (eligibility != null && eligibility.isEligible) + "\n" +
                "eligibility.failureReason=" + (eligibility != null
                    ? eligibility.failureReason.ToString()
                    : "NONE") + "\n" +
                "eligibility.failureMessage=" + (eligibility != null
                    ? eligibility.failureMessage
                    : "NONE")
            );
        }
        if (intent == null || intent.enemy == null || intent.enemy.IsDead() ||
            intent.isResponded || intent.isConsumedAsReactiveGuard ||
            !object.ReferenceEquals(intent.enemy, freeActionSlot.target) ||
            intent.enemyCardState == null || intent.enemyCardState.cardData == null)
        {
            return false;
        }

        string cardType = intent.enemyCardState.cardData.cardType;
        if (cardType != CardType.Defense && cardType != CardType.Dodge)
        {
            return false;
        }

        return eligibility != null && eligibility.isEligible;
    }

    public static BattleActionSlot SelectGuardForEnemyIntent(
        BattleRuntimeState runtimeState,
        BattleEnemyIntent enemyIntent
    )
    {
        return SelectGuardForEnemyIntent(
            runtimeState != null ? runtimeState.actionSlots : null,
            enemyIntent
        );
    }

    public static BattleActionSlot SelectGuardForEnemyIntent(
        IReadOnlyList<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent
    )
    {
        return SelectHandlingCardForEnemyIntent(actionSlots, enemyIntent).slot;
    }

    public static BattleGuardSelectionResult SelectHandlingCardForEnemyIntent(
        IReadOnlyList<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent
    )
    {
        BattleActionSlot selectedSlot = SelectContinuousDodgeForEnemyIntent(
            actionSlots,
            enemyIntent
        );
        BattleGuardSelectionType selectionType = selectedSlot != null
            ? BattleGuardSelectionType.ContinuousDodge
            : BattleGuardSelectionType.None;

        if (selectedSlot == null)
        {
            selectedSlot = SelectFirstValidGuardInScope(
                actionSlots,
                enemyIntent,
                BattleActionSlotType.EnemySpecificGuard
            );
            selectionType = selectedSlot != null
                ? BattleGuardSelectionType.EnemySpecificGuard
                : BattleGuardSelectionType.None;
        }

        if (selectedSlot == null)
        {
            selectedSlot = SelectFirstValidGuardInScope(
                actionSlots,
                enemyIntent,
                BattleActionSlotType.PassiveGuard
            );
            selectionType = selectedSlot != null
                ? BattleGuardSelectionType.PassiveGuard
                : BattleGuardSelectionType.None;
        }

        if (selectedSlot == null)
        {
            Debug.Log(
                "敌人：" +
                (enemyIntent != null ? enemyIntent.GetEnemyName() : "无敌人") +
                "，actualTarget：" +
                (enemyIntent != null ? enemyIntent.GetActualTargetName() : "无目标") +
                "，没有有效守备，按 UnrespondedEnemyIntent 直接处理"
            );
            return new BattleGuardSelectionResult(BattleGuardSelectionType.None, null);
        }

        if (selectionType == BattleGuardSelectionType.ContinuousDodge)
        {
            Debug.Log(
                "[ContinuousDodge Selected]\n" +
                "Enemy: " + enemyIntent.GetEnemyName() + "\n" +
                "ActualTarget: " + enemyIntent.GetActualTargetName() + "\n" +
                "Slot: " + selectedSlot.slotIndex + "\n" +
                "Card: " + selectedSlot.GetCardName() + "\n" +
                "SuccessfulCount: " + selectedSlot.successfulDodgeCount
            );
        }
        else
        {
            Debug.Log(
                "敌人：" +
                enemyIntent.GetEnemyName() +
                "，actualTarget：" +
                enemyIntent.GetActualTargetName() +
                "，选中的守备范围：" +
                selectedSlot.slotType +
                "，槽位：" +
                selectedSlot.slotIndex +
                "，卡牌：" +
                selectedSlot.GetCardName()
            );
        }

        return new BattleGuardSelectionResult(selectionType, selectedSlot);
    }

    public static bool WouldSelectContinuousDodgeForEnemyIntent(
        IReadOnlyList<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent,
        BattleActionSlot anticipatedActiveSlot
    )
    {
        BattleActionSlot selectedSlot = SelectContinuousDodgeForEnemyIntent(
            actionSlots,
            enemyIntent,
            anticipatedActiveSlot
        );
        return selectedSlot != null && object.ReferenceEquals(
            selectedSlot,
            anticipatedActiveSlot
        );
    }

    static BattleActionSlot SelectContinuousDodgeForEnemyIntent(
        IReadOnlyList<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent
    )
    {
        return SelectContinuousDodgeForEnemyIntent(
            actionSlots,
            enemyIntent,
            null
        );
    }

    static BattleActionSlot SelectContinuousDodgeForEnemyIntent(
        IReadOnlyList<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent,
        BattleActionSlot anticipatedActiveSlot
    )
    {
        if (actionSlots == null || enemyIntent == null)
        {
            return null;
        }

        BattleActionSlot selectedSlot = null;

        foreach (BattleActionSlot slot in actionSlots)
        {
            bool isAnticipatedActive = object.ReferenceEquals(
                slot,
                anticipatedActiveSlot
            );
            if (!IsValidContinuousDodgeSlot(
                    slot,
                    enemyIntent,
                    isAnticipatedActive
                ))
            {
                continue;
            }

            if (selectedSlot == null || slot.slotIndex < selectedSlot.slotIndex)
            {
                selectedSlot = slot;
            }
        }

        return selectedSlot;
    }

    static bool IsValidContinuousDodgeSlot(
        BattleActionSlot slot,
        BattleEnemyIntent enemyIntent,
        bool isAnticipatedActive = false
    )
    {
        if (slot == null ||
            enemyIntent == null ||
            enemyIntent.enemy == null ||
            enemyIntent.actualTargetCharacter == null ||
            (!slot.isContinuousDodgeActive && !isAnticipatedActive) ||
            slot.isCardUseFinalized ||
            slot.isUsed ||
            slot.owner == null ||
            slot.actor == null ||
            slot.cardState == null ||
            slot.cardState.cardData == null)
        {
            return false;
        }

        if (enemyIntent.enemy.IsDead() ||
            enemyIntent.actualTargetCharacter.IsDead() ||
            slot.owner.IsDead() ||
            slot.actor.IsDead())
        {
            return false;
        }

        if (!object.ReferenceEquals(slot.owner, slot.actor) ||
            !object.ReferenceEquals(slot.owner, enemyIntent.actualTargetCharacter) ||
            slot.cardState.cardData.cardType != CardType.Dodge)
        {
            return false;
        }

        // Active Continuous Dodge is the same deferred card use, not a new play.
        // Its ordinary CardUsed cooldown must not be re-evaluated here.
        return true;
    }

    public static List<BattleActionSlot> CollectGuardCandidates(
        IReadOnlyList<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent
    )
    {
        List<BattleActionSlot> candidates = new List<BattleActionSlot>();

        if (actionSlots == null ||
            enemyIntent == null ||
            enemyIntent.enemy == null ||
            enemyIntent.actualTargetCharacter == null)
        {
            return candidates;
        }

        foreach (BattleActionSlot slot in actionSlots)
        {
            if (IsStructurallyMatchedGuard(slot, enemyIntent))
            {
                candidates.Add(slot);
            }
        }

        return candidates;
    }

    static bool IsStructurallyMatchedGuard(
        BattleActionSlot slot,
        BattleEnemyIntent enemyIntent
    )
    {
        if (slot == null ||
            slot.owner == null ||
            slot.actor == null ||
            slot.target == null ||
            slot.cardState == null ||
            slot.cardState.cardData == null)
        {
            return false;
        }

        if (!object.ReferenceEquals(slot.owner, enemyIntent.actualTargetCharacter) ||
            !object.ReferenceEquals(slot.actor, enemyIntent.actualTargetCharacter))
        {
            return false;
        }

        string cardType = slot.cardState.cardData.cardType;
        if (cardType != CardType.Defense && cardType != CardType.Dodge)
        {
            return false;
        }

        if (slot.slotType == BattleActionSlotType.EnemySpecificGuard)
        {
            return
                object.ReferenceEquals(slot.requestedEnemy, enemyIntent.enemy) &&
                object.ReferenceEquals(slot.target, enemyIntent.enemy);
        }

        return
            slot.slotType == BattleActionSlotType.PassiveGuard &&
            object.ReferenceEquals(slot.target, enemyIntent.actualTargetCharacter);
    }

    static BattleActionSlot SelectFirstValidGuardInScope(
        IReadOnlyList<BattleActionSlot> actionSlots,
        BattleEnemyIntent enemyIntent,
        BattleActionSlotType requiredSlotType
    )
    {
        if (actionSlots == null || enemyIntent == null)
        {
            return null;
        }

        BattleActionSlot selectedSlot = null;

        foreach (BattleActionSlot slot in actionSlots)
        {
            if (!IsValidGuardSlot(slot, enemyIntent, requiredSlotType))
            {
                continue;
            }

            if (selectedSlot == null || slot.slotIndex < selectedSlot.slotIndex)
            {
                selectedSlot = slot;
            }
        }

        return selectedSlot;
    }

    static bool IsValidGuardSlot(
        BattleActionSlot slot,
        BattleEnemyIntent enemyIntent,
        BattleActionSlotType requiredSlotType
    )
    {
        if (slot == null ||
            enemyIntent == null ||
            enemyIntent.enemy == null ||
            enemyIntent.actualTargetCharacter == null)
        {
            return false;
        }

        if (slot.slotType != requiredSlotType ||
            slot.isUsed ||
            slot.owner == null ||
            slot.actor == null ||
            slot.target == null ||
            slot.cardState == null ||
            slot.cardState.cardData == null)
        {
            return false;
        }

        if (enemyIntent.enemy.IsDead() ||
            enemyIntent.actualTargetCharacter.IsDead() ||
            slot.owner.IsDead() ||
            slot.actor.IsDead() ||
            slot.target.IsDead())
        {
            return false;
        }

        if (!object.ReferenceEquals(slot.owner, enemyIntent.actualTargetCharacter) ||
            !object.ReferenceEquals(slot.actor, enemyIntent.actualTargetCharacter))
        {
            return false;
        }

        if (requiredSlotType == BattleActionSlotType.EnemySpecificGuard)
        {
            if (slot.requestedEnemy == null ||
                slot.requestedEnemy.IsDead() ||
                !object.ReferenceEquals(slot.requestedEnemy, enemyIntent.enemy) ||
                !object.ReferenceEquals(slot.target, enemyIntent.enemy))
            {
                return false;
            }
        }
        else if (!object.ReferenceEquals(slot.target, enemyIntent.actualTargetCharacter))
        {
            return false;
        }

        string cardType = slot.cardState.cardData.cardType;
        if (cardType != CardType.Defense && cardType != CardType.Dodge)
        {
            return false;
        }

        CardEligibilityResult eligibility = BattleCardManager.EvaluateCardEligibility(
            slot.actor,
            enemyIntent.enemy,
            slot.cardState
        );
        return eligibility != null && eligibility.isEligible;
    }
}
