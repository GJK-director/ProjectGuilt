// BattleEnemyIntent = 敌人行动意图
// 第一版只用于测试：敌人使用一张卡攻击一个原始目标
public class BattleEnemyIntent
{
    public string intentID;
    public CharacterData enemy;
    public BattleCardState enemyCardState;
    public CharacterData originalTarget;

    public BattleEnemyIntent(
        string intentID,
        CharacterData enemy,
        BattleCardState enemyCardState,
        CharacterData originalTarget
    )
    {
        this.intentID = intentID;
        this.enemy = enemy;
        this.enemyCardState = enemyCardState;
        this.originalTarget = originalTarget;
    }

    public string GetEnemyName()
    {
        if (enemy == null)
        {
            return "无敌人";
        }

        return enemy.characterName;
    }

    public string GetCardName()
    {
        if (enemyCardState == null)
        {
            return "无卡牌";
        }

        return enemyCardState.GetCardName();
    }

    public string GetOriginalTargetName()
    {
        if (originalTarget == null)
        {
            return "无目标";
        }

        return originalTarget.characterName;
    }
}
