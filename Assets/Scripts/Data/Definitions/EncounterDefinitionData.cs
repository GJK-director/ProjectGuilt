// DefinitionData只保存不会在战斗中变化的模板数据。
// currentHP、Buff批次、卡牌CD等运行时状态必须由CharacterData和BattleCardState保存。
public class EncounterDefinitionData
{
    public string encounterID;
    public string encounterName;
    public string[] allyCharacterIDs;
    public string enemyID;
    public EnemyIntentDefinitionData[] intentPattern;
    public bool repeatIntentPattern;
    public string battleBackgroundKey;
    public string battleMusicKey;
}
