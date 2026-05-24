public enum BattleHandlingPreviewType
{
    RespondedIntent,
    UnrespondedIntent
}

public class BattleHandlingPreviewItem
{
    public int order;
    public BattleHandlingPreviewType handlingType;
    public BattleEnemyIntent enemyIntent;
    public BattleActionSlot actionSlot;

    public BattleHandlingPreviewItem(
        int order,
        BattleHandlingPreviewType handlingType,
        BattleEnemyIntent enemyIntent,
        BattleActionSlot actionSlot
    )
    {
        this.order = order;
        this.handlingType = handlingType;
        this.enemyIntent = enemyIntent;
        this.actionSlot = actionSlot;
    }
}
