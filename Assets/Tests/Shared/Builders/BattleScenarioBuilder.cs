using System.Collections.Generic;

public static class BattleScenarioBuilder
{
    public static BattleTestContext CreateProductionEncounter(
        string encounterID = "encounter_test_001",
        bool useSingleUnitDemo = true
    )
    {
        List<CardTestData> cards = CardDataLoader.LoadCardData();
        List<CharacterDefinitionData> characters =
            CharacterDefinitionLoader.LoadDefinitions();
        List<EnemyDefinitionData> enemies =
            EnemyDefinitionLoader.LoadDefinitions();
        List<EncounterDefinitionData> encounters =
            EncounterDefinitionLoader.LoadDefinitions();

        BattleDefinitionBootstrapResult bootstrap =
            BattleDefinitionBootstrap.CreateRuntimeStateFromDefinitions(
                encounterID,
                cards,
                characters,
                enemies,
                encounters,
                useSingleUnitDemo
            );

        return new BattleTestContext(
            cards,
            characters,
            enemies,
            encounters,
            bootstrap
        );
    }
}
