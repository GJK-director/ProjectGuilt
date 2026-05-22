// BattleActionSlotType = 行动槽位类型
// RespondToEnemyIntent：响应敌人意图
// FreeAction：不直接响应敌人意图的自由行动，第一版只用于 Ability 罪卡测试
public enum BattleActionSlotType
{
    RespondToEnemyIntent,
    FreeAction
}

// BattleActionSlot = 行动槽位
// 槽位只记录准备阶段安排，不负责真正战斗结算
public class BattleActionSlot
{
    public int slotIndex;
    public BattleActionSlotType slotType;
    public CharacterData actor;
    public BattleCardState cardState;
    public CharacterData target;
    public BattleEnemyIntent enemyIntent;
    public bool isUsed;

    public BattleActionSlot(int slotIndex)
    {
        this.slotIndex = slotIndex;
        Clear();
    }

    public bool IsEmpty()
    {
        return cardState == null;
    }

    public void AssignResponse(
        CharacterData actor,
        BattleCardState cardState,
        BattleEnemyIntent enemyIntent
    )
    {
        slotType = BattleActionSlotType.RespondToEnemyIntent;
        this.actor = actor;
        this.cardState = cardState;
        this.enemyIntent = enemyIntent;
        isUsed = false;

        if (enemyIntent != null)
        {
            target = enemyIntent.enemy;
            enemyIntent.SetActualTarget(actor, slotIndex);
        }
        else
        {
            target = null;
        }
    }

    public void AssignFreeAction(
        CharacterData actor,
        BattleCardState cardState,
        CharacterData target
    )
    {
        slotType = BattleActionSlotType.FreeAction;
        this.actor = actor;
        this.cardState = cardState;
        this.target = target;
        enemyIntent = null;
        isUsed = false;
    }

    public void MarkUsed()
    {
        isUsed = true;
    }

    public void Clear()
    {
        slotType = BattleActionSlotType.FreeAction;
        actor = null;
        cardState = null;
        target = null;
        enemyIntent = null;
        isUsed = false;
    }

    public string GetActorName()
    {
        if (actor == null)
        {
            return "无行动者";
        }

        return actor.characterName;
    }

    public string GetCardName()
    {
        if (cardState == null)
        {
            return "空";
        }

        return cardState.GetCardName();
    }

    public string GetTargetName()
    {
        if (target == null)
        {
            return "无目标";
        }

        return target.characterName;
    }
}
