// 脚本中文说明：描述一次 Execution Interaction 中某一侧实际参与的 Action。
public sealed class BattleExecutionAction
{
    public CharacterData actor;
    public BattleCardState cardState;
    public BattleActionSlot actionSlot;
    public BattleEnemyIntent enemyIntent;
    public CharacterData target;

    public BattleExecutionAction(
        CharacterData actor,
        BattleCardState cardState,
        BattleActionSlot actionSlot,
        BattleEnemyIntent enemyIntent,
        CharacterData target
    )
    {
        this.actor = actor;
        this.cardState = cardState;
        this.actionSlot = actionSlot;
        this.enemyIntent = enemyIntent;
        this.target = target;
    }
}
