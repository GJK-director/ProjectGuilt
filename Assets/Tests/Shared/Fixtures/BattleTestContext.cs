using System.Collections.Generic;

public sealed class BattleTestContext
{
    public List<CardTestData> Cards { get; }
    public List<CharacterDefinitionData> Characters { get; }
    public List<EnemyDefinitionData> Enemies { get; }
    public List<EncounterDefinitionData> Encounters { get; }
    public BattleDefinitionBootstrapResult Bootstrap { get; }

    public BattleRuntimeState Runtime
        => Bootstrap?.runtimeState;

    public CharacterData AllyA
        => Runtime?.allyA;

    public CharacterData AllyB
        => Runtime?.allyB;

    public CharacterData Enemy
        => Runtime?.enemy;

    public CharacterData Enemy2
        => Runtime?.enemy2;

    public CharacterDefinitionData AllyDefinition
        => Bootstrap?.allyADefinition;

    public EnemyDefinitionData EnemyDefinition
        => Bootstrap?.enemyDefinition;

    public EncounterDefinitionData EncounterDefinition
        => Bootstrap?.encounterDefinition;

    public Dictionary<string, CharacterData> AllyByID
        => Bootstrap?.allyByID;

    public bool IsValid
        => Bootstrap != null
           && Bootstrap.isSuccess
           && Runtime != null;

    public BattleTestContext(
        List<CardTestData> cards,
        List<CharacterDefinitionData> characters,
        List<EnemyDefinitionData> enemies,
        List<EncounterDefinitionData> encounters,
        BattleDefinitionBootstrapResult bootstrap
    )
    {
        Cards = cards;
        Characters = characters;
        Enemies = enemies;
        Encounters = encounters;
        Bootstrap = bootstrap;
    }
}
