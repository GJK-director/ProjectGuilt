// DefinitionData只保存不会在战斗中变化的模板数据。
// currentHP、Buff批次、卡牌CD等运行时状态必须由CharacterData和BattleCardState保存。
public class EncounterDefinitionData
{
    public string encounterID;
    public string encounterName;
    public string[] allyCharacterIDs;
    public string enemyID;
    public EnemyIntentDefinitionData[] intentPattern;
    public EnemyIntentRoundDefinitionData[] intentCycle;
    public bool repeatIntentPattern;
    public string battleBackgroundKey;
    public string battleMusicKey;
}

// 每个回合定义一组敌方行动；运行时卡牌实例仍由EnemyDefinition持有。
[System.Serializable]
public sealed class EnemyIntentRoundDefinitionData
{
    public EnemyIntentDefinitionData[] intents;
}
