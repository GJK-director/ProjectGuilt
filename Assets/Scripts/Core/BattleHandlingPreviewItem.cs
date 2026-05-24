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
}
