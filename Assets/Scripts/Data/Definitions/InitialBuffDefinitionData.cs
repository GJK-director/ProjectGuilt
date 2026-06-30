// DefinitionData只保存不会在战斗中变化的模板数据。
// currentHP、Buff批次、卡牌CD等运行时状态必须由CharacterData和BattleCardState保存。
public class InitialBuffDefinitionData
{
    public string buffID;
    public int stack;
    public int duration;
}
