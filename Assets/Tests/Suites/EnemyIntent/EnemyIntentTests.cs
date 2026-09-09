using System.Collections.Generic;

public static class EnemyIntentTests
{
    private static readonly string[][] ExpectedProductionCycleCardIDs =
    {
        new[] { "enemy_probe_001", "enemy_probe_001" },
        new[] { "enemy_probe_001", "enemy_guard_001" },
        new[] { "enemy_smash_001", "enemy_probe_001" },
        new[] { "enemy_fierce_001", "enemy_probe_001" },
        new[] { "enemy_guard_001", "enemy_guard_001" },
        new[] { "enemy_smash_001", "enemy_fierce_001" },
        new[] { "enemy_probe_001", "enemy_probe_001" },
        new[] { "enemy_smash_001", "enemy_smash_001" },
        new[] { "enemy_guard_001", "enemy_fierce_001" },
        new[] { "enemy_smash_001", "enemy_fierce_001" }
    };

    public static bool ProductionCycleDefinitionsUseExpectedFixedTargets(
        BattleTestContext fixture
    )
    {
        if (fixture == null || !fixture.IsValid ||
            fixture.EncounterDefinition == null ||
            fixture.EncounterDefinition.intentCycle == null ||
            fixture.EncounterDefinition.intentCycle.Length != 10)
        {
            return false;
        }

        foreach (EnemyIntentRoundDefinitionData round in
            fixture.EncounterDefinition.intentCycle)
        {
            if (round == null || round.intents == null ||
                round.intents.Length != 2 ||
                !IsExpectedFixedTarget(round.intents[0], 1) ||
                !IsExpectedFixedTarget(round.intents[1], 2))
            {
                return false;
            }
        }

        return true;
    }

    public static bool ProductionCycleRepeatFlagIsEnabled(
        BattleTestContext fixture
    )
    {
        return fixture != null && fixture.IsValid &&
            fixture.EncounterDefinition != null &&
            fixture.EncounterDefinition.repeatIntentPattern;
    }

    public static bool ProductionCycleRuntimeQueuesRepeatThroughTurn21(
        BattleTestContext fixture
    )
    {
        if (!IsValidProductionFixture(fixture))
        {
            return false;
        }

        for (int turn = 1; turn <= 21; turn++)
        {
            int cycleIndex = (turn - 1) % ExpectedProductionCycleCardIDs.Length;
            BattleDefinitionIntentQueueResult result =
                BattleDefinitionBootstrap.CreateIntentQueueForTurn(
                    fixture.Runtime,
                    fixture.EncounterDefinition,
                    fixture.EnemyDefinition,
                    fixture.AllyByID,
                    turn,
                    fixture.Runtime.actionSlots
                );

            if (result == null || !result.isSuccess ||
                result.intentQueue == null || result.intentQueue.Count != 2 ||
                !IsExpectedRuntimeIntent(
                    result.intentQueue[0],
                    fixture.Runtime.enemy,
                    1,
                    ExpectedProductionCycleCardIDs[cycleIndex][0],
                    fixture.Runtime.allyA,
                    1
                ) ||
                !IsExpectedRuntimeIntent(
                    result.intentQueue[1],
                    fixture.Runtime.enemy,
                    2,
                    ExpectedProductionCycleCardIDs[cycleIndex][1],
                    fixture.Runtime.allyA,
                    2
                ))
            {
                return false;
            }
        }

        return true;
    }

    public static bool DuplicateCardEntriesCreateDistinctRuntimeStates(
        BattleTestContext fixture
    )
    {
        if (!IsValidProductionFixture(fixture))
        {
            return false;
        }

        bool checkedDuplicateRound = false;
        for (int roundIndex = 0;
            roundIndex < ExpectedProductionCycleCardIDs.Length;
            roundIndex++)
        {
            string[] expectedCardIDs = ExpectedProductionCycleCardIDs[roundIndex];
            if (expectedCardIDs[0] != expectedCardIDs[1])
            {
                continue;
            }

            checkedDuplicateRound = true;
            BattleDefinitionIntentQueueResult result =
                BattleDefinitionBootstrap.CreateIntentQueueForTurn(
                    fixture.Runtime,
                    fixture.EncounterDefinition,
                    fixture.EnemyDefinition,
                    fixture.AllyByID,
                    roundIndex + 1,
                    fixture.Runtime.actionSlots
                );

            if (result == null || !result.isSuccess ||
                result.intentQueue == null || result.intentQueue.Count != 2 ||
                object.ReferenceEquals(
                    result.intentQueue[0].enemyCardState,
                    result.intentQueue[1].enemyCardState
                ))
            {
                return false;
            }
        }

        return checkedDuplicateRound;
    }

    public static bool LegacyPatternFallbackCreatesExpectedTwoSlotQueue(
        BattleTestContext fixture
    )
    {
        if (!IsValidProductionFixture(fixture))
        {
            return false;
        }

        EncounterDefinitionData legacyDefinition =
            new EncounterDefinitionData
            {
                encounterID = "mode103_legacy_pattern",
                encounterName = "Mode103 Legacy Pattern",
                allyCharacterIDs = fixture.EncounterDefinition.allyCharacterIDs,
                enemyID = fixture.EnemyDefinition.enemyID,
                intentPattern = fixture.EncounterDefinition.intentPattern,
                repeatIntentPattern = true,
                battleBackgroundKey = "mode103_background",
                battleMusicKey = "mode103_music"
            };
        string validationError;
        if (!EncounterDefinitionLoader.ValidateDefinition(
                legacyDefinition,
                out validationError))
        {
            return false;
        }

        BattleDefinitionIntentQueueResult result =
            BattleDefinitionBootstrap.CreateIntentQueueForTurn(
                fixture.Runtime,
                legacyDefinition,
                fixture.EnemyDefinition,
                fixture.AllyByID,
                2,
                fixture.Runtime.actionSlots
            );

        return result != null && result.isSuccess &&
            result.intentQueue != null && result.intentQueue.Count == 2 &&
            IsExpectedRuntimeIntent(
                result.intentQueue[0],
                fixture.Runtime.enemy,
                1,
                "enemy_probe_001",
                fixture.Runtime.allyA,
                1
            ) &&
            IsExpectedRuntimeIntent(
                result.intentQueue[1],
                fixture.Runtime.enemy,
                2,
                "enemy_probe_001",
                fixture.Runtime.allyA,
                2
            );
    }

    private static bool IsExpectedFixedTarget(
        EnemyIntentDefinitionData intent,
        int targetSlotIndex
    )
    {
        return intent != null &&
            intent.targetRule ==
                EncounterDefinitionLoader.TargetRuleFixedCharacterSlot &&
            intent.targetCharacterID == "ally_001" &&
            intent.targetSlotIndex == targetSlotIndex;
    }

    private static bool IsExpectedRuntimeIntent(
        BattleEnemyIntent intent,
        CharacterData enemy,
        int enemySlotIndex,
        string cardID,
        CharacterData target,
        int targetSlotIndex
    )
    {
        return intent != null &&
            object.ReferenceEquals(intent.enemy, enemy) &&
            intent.enemySlotIndex == enemySlotIndex &&
            intent.enemyCardState != null &&
            intent.enemyCardState.cardData != null &&
            intent.enemyCardState.cardData.cardID == cardID &&
            object.ReferenceEquals(intent.originalTargetCharacter, target) &&
            intent.originalTargetSlotIndex == targetSlotIndex &&
            object.ReferenceEquals(intent.actualTargetCharacter, target) &&
            intent.actualTargetSlotIndex == targetSlotIndex &&
            !intent.isResponded &&
            !intent.isConsumedAsReactiveGuard;
    }

    private static bool IsValidProductionFixture(BattleTestContext fixture)
    {
        return fixture != null && fixture.IsValid &&
            fixture.Runtime != null && fixture.Runtime.enemy != null &&
            fixture.Runtime.allyA != null && fixture.Runtime.actionSlots != null &&
            fixture.EncounterDefinition != null &&
            fixture.EnemyDefinition != null && fixture.AllyByID != null;
    }
}
