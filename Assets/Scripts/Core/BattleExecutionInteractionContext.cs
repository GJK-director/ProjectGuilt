// 脚本中文说明：统一保存当前 ExecutionItem 的两侧 Action 与运行时有效 Interaction。
public sealed class BattleExecutionInteractionContext
{
    public BattleExecutionItem executionItem;
    public BattleExecutionAction sideA;
    public BattleExecutionAction sideB;
    public BattleInteractionType effectiveInteractionType;

    public BattleExecutionInteractionContext(
        BattleExecutionItem executionItem,
        BattleExecutionAction sideA,
        BattleExecutionAction sideB
    )
    {
        this.executionItem = executionItem;
        this.sideA = sideA;
        this.sideB = sideB;
        effectiveInteractionType = BattleInteractionClassifier.Classify(
            sideA != null ? sideA.cardState : null,
            sideB != null ? sideB.cardState : null
        );
    }
}

public static class BattleExecutionInteractionContextFactory
{
    public static BattleExecutionInteractionContext BuildPlanned(
        BattleExecutionItem item
    )
    {
        if (item == null)
        {
            return new BattleExecutionInteractionContext(null, null, null);
        }

        if (item.executionType == BattleExecutionItemType.RespondedEnemyIntent)
        {
            return BuildResponded(item, item.actionSlot);
        }

        if (item.executionType == BattleExecutionItemType.UnrespondedEnemyIntent)
        {
            return BuildUnresponded(item);
        }

        if (item.executionType == BattleExecutionItemType.FreeAction)
        {
            BattleActionSlot slot = item.actionSlot;
            return new BattleExecutionInteractionContext(
                item,
                CreateSlotAction(slot, null),
                null
            );
        }

        return new BattleExecutionInteractionContext(item, null, null);
    }

    // Runtime response 只替换 Unresponded Item 的有效配对；计划阶段 interactionType 保持不变。
    public static BattleExecutionInteractionContext BuildEffective(
        BattleExecutionItem item,
        BattleActionSlot runtimeResponseSlot
    )
    {
        if (item == null ||
            item.executionType != BattleExecutionItemType.UnrespondedEnemyIntent ||
            runtimeResponseSlot == null)
        {
            return BuildPlanned(item);
        }

        return BuildResponded(item, runtimeResponseSlot);
    }

    public static BattleExecutionInteractionContext BuildEffectiveFreeAction(
        BattleExecutionItem item,
        BattleEnemyIntent reactiveGuardIntent
    )
    {
        if (item == null || item.executionType != BattleExecutionItemType.FreeAction ||
            reactiveGuardIntent == null)
        {
            return BuildPlanned(item);
        }

        return new BattleExecutionInteractionContext(
            item,
            CreateSlotAction(item.actionSlot, reactiveGuardIntent),
            CreateIntentAction(reactiveGuardIntent)
        );
    }

    private static BattleExecutionInteractionContext BuildResponded(
        BattleExecutionItem item,
        BattleActionSlot responseSlot
    )
    {
        BattleEnemyIntent intent = item != null ? item.enemyIntent : null;
        BattleExecutionAction responseAction = CreateSlotAction(
            responseSlot,
            intent
        );
        BattleExecutionAction intentAction = CreateIntentAction(intent);

        return new BattleExecutionInteractionContext(
            item,
            responseAction,
            intentAction
        );
    }

    private static BattleExecutionInteractionContext BuildUnresponded(
        BattleExecutionItem item
    )
    {
        return new BattleExecutionInteractionContext(
            item,
            CreateIntentAction(item != null ? item.enemyIntent : null),
            null
        );
    }

    private static BattleExecutionAction CreateSlotAction(
        BattleActionSlot slot,
        BattleEnemyIntent intent
    )
    {
        if (slot == null)
        {
            return null;
        }

        CharacterData target = intent != null ? intent.enemy : slot.target;
        return new BattleExecutionAction(
            slot.actor,
            slot.cardState,
            slot,
            intent,
            target
        );
    }

    private static BattleExecutionAction CreateIntentAction(
        BattleEnemyIntent intent
    )
    {
        if (intent == null)
        {
            return null;
        }

        return new BattleExecutionAction(
            intent.enemy,
            intent.enemyCardState,
            null,
            intent,
            intent.actualTargetCharacter
        );
    }
}
