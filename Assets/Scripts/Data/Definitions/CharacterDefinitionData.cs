// DefinitionData只保存不会在战斗中变化的模板数据。
// currentHP、Buff批次、卡牌CD等运行时状态必须由CharacterData和BattleCardState保存。
public class CharacterDefinitionData
{
    public string characterID;
    public string characterName;
    public int maxHP;
    public int minSpeed;
    public int maxSpeed;
    public int actionSlotCount;
    public string[] startingCardIDs;
    public InitialBuffDefinitionData[] initialBuffs;
    public string prefabKey;
    public string portraitKey;
}
