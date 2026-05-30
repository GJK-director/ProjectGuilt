// 脚本中文说明：处理预览项。负责描述敌人意图未来会被响应处理还是无人响应处理，只用于预览顺序。
// BattleHandlingPreviewType = 战斗处理预览类型
// Preview = 预览，表示这里只是查看未来处理路径，不等于真正执行。
public enum BattleHandlingPreviewType
{
    RespondedIntent,
    UnrespondedIntent
}

// BattleHandlingPreviewItem = 战斗处理预览项
// Item = 项目 / 条目，用来保存一条预览顺序。
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
