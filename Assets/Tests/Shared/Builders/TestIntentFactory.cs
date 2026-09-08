public static class TestIntentFactory
{
    public static BattleEnemyIntent Create(
        string intentID,
        CharacterData enemy,
        BattleCardState enemyCardState,
        CharacterData originalTargetCharacter,
        int originalTargetSlotIndex = 1,
        int intentOrder = 1,
        int enemySlotIndex = 1
    )
    {
        return new BattleEnemyIntent(
            intentID,
            enemy,
            enemyCardState,
            originalTargetCharacter,
            originalTargetSlotIndex,
            intentOrder,
            enemySlotIndex
        );
    }
}
