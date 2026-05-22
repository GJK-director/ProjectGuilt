// BattleEnemyIntent = 敌人行动意图
// 第一版只用于测试：敌人使用一张卡攻击一个角色的某个槽位
public class BattleEnemyIntent
{
    public string intentID;
    public int intentOrder;
    public CharacterData enemy;
    public BattleCardState enemyCardState;
    public CharacterData originalTargetCharacter;
    public int originalTargetSlotIndex;
    public CharacterData actualTargetCharacter;
    public int actualTargetSlotIndex;
    public bool isResponded;

    public BattleEnemyIntent(
        string intentID,
        CharacterData enemy,
        BattleCardState enemyCardState,
        CharacterData originalTargetCharacter,
        int originalTargetSlotIndex,
        int intentOrder = 1
    )
    {
        this.intentID = intentID;
        this.intentOrder = intentOrder;
        this.enemy = enemy;
        this.enemyCardState = enemyCardState;
        this.originalTargetCharacter = originalTargetCharacter;
        this.originalTargetSlotIndex = originalTargetSlotIndex;
        actualTargetCharacter = originalTargetCharacter;
        actualTargetSlotIndex = originalTargetSlotIndex;
        isResponded = false;
    }

    public void SetActualTarget(CharacterData actualTargetCharacter, int actualTargetSlotIndex)
    {
        this.actualTargetCharacter = actualTargetCharacter;
        this.actualTargetSlotIndex = actualTargetSlotIndex;
    }

    public void MarkResponded()
    {
        isResponded = true;
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
        if (originalTargetCharacter == null)
        {
            return "无目标";
        }

        return originalTargetCharacter.characterName;
    }

    public string GetActualTargetName()
    {
        if (actualTargetCharacter == null)
        {
            return "无目标";
        }

        return actualTargetCharacter.characterName;
    }

    public string GetOriginalTargetSlotText()
    {
        return GetTargetSlotText(originalTargetCharacter, originalTargetSlotIndex);
    }

    public string GetActualTargetSlotText()
    {
        return GetTargetSlotText(actualTargetCharacter, actualTargetSlotIndex);
    }

    static string GetTargetSlotText(CharacterData targetCharacter, int slotIndex)
    {
        if (targetCharacter == null)
        {
            return "无目标";
        }

        return targetCharacter.characterName + " 槽位" + slotIndex;
    }
}
