public enum BattleExecutionItemType
{
    RespondedEnemyIntent,
    UnrespondedEnemyIntent,
    FreeAction
}

public class BattleExecutionItem
{
    public int order;
    public BattleExecutionItemType executionType;
    public BattleEnemyIntent enemyIntent;
    public BattleActionSlot actionSlot;
    public bool isCompleted;

    public BattleExecutionItem(
        int order,
        BattleExecutionItemType executionType,
        BattleEnemyIntent enemyIntent,
        BattleActionSlot actionSlot
    )
    {
        this.order = order;
        this.executionType = executionType;
        this.enemyIntent = enemyIntent;
        this.actionSlot = actionSlot;
        isCompleted = false;
    }
}
