using System.Collections.Generic;
using UnityEngine;

public class BattleDefinitionBootstrapResult
{
    public bool isSuccess;
    public string errorMessage;
    public BattleRuntimeState runtimeState;
    public EncounterDefinitionData encounterDefinition;
    public CharacterDefinitionData allyADefinition;
    public CharacterDefinitionData allyBDefinition;
    public EnemyDefinitionData enemyDefinition;
    public Dictionary<string, CharacterData> allyByID;

    public static BattleDefinitionBootstrapResult Success(
        BattleRuntimeState runtimeState,
        EncounterDefinitionData encounterDefinition,
        CharacterDefinitionData allyADefinition,
        CharacterDefinitionData allyBDefinition,
        EnemyDefinitionData enemyDefinition,
        Dictionary<string, CharacterData> allyByID
    )
    {
        return new BattleDefinitionBootstrapResult
        {
            isSuccess = true,
            errorMessage = "",
            runtimeState = runtimeState,
            encounterDefinition = encounterDefinition,
            allyADefinition = allyADefinition,
            allyBDefinition = allyBDefinition,
            enemyDefinition = enemyDefinition,
            allyByID = allyByID
        };
    }

    public static BattleDefinitionBootstrapResult Failure(string errorMessage)
    {
        return new BattleDefinitionBootstrapResult
        {
            isSuccess = false,
            errorMessage = errorMessage,
            runtimeState = null,
            encounterDefinition = null,
            allyADefinition = null,
            allyBDefinition = null,
            enemyDefinition = null,
            allyByID = new Dictionary<string, CharacterData>()
        };
    }
}

public class BattleDefinitionIntentQueueResult
{
    public bool isSuccess;
    public string errorMessage;
    public List<string> warningMessages = new List<string>();
    public List<BattleEnemyIntent> intentQueue = new List<BattleEnemyIntent>();

    public static BattleDefinitionIntentQueueResult Failure(string errorMessage)
    {
        return new BattleDefinitionIntentQueueResult
        {
            isSuccess = false,
            errorMessage = errorMessage,
            intentQueue = new List<BattleEnemyIntent>()
        };
    }
}

public static class BattleDefinitionBootstrap
{
    public static BattleDefinitionBootstrapResult CreateRuntimeState(string encounterID)
    {
        return CreateRuntimeState(encounterID, false);
    }

    public static BattleDefinitionBootstrapResult CreateRuntimeState(
        string encounterID,
        bool useSingleUnitDemo
    )
    {
        List<CardTestData> cards = CardDataLoader.LoadCardData();
        List<CharacterDefinitionData> characterDefinitions = CharacterDefinitionLoader.LoadDefinitions();
        List<EnemyDefinitionData> enemyDefinitions = EnemyDefinitionLoader.LoadDefinitions();
        List<EncounterDefinitionData> encounterDefinitions = EncounterDefinitionLoader.LoadDefinitions();

        return CreateRuntimeStateFromDefinitions(
            encounterID,
            cards,
            characterDefinitions,
            enemyDefinitions,
            encounterDefinitions,
            useSingleUnitDemo
        );
    }

    public static BattleDefinitionBootstrapResult CreateRuntimeStateFromDefinitions(
        string encounterID,
        List<CardTestData> cards,
        List<CharacterDefinitionData> characterDefinitions,
        List<EnemyDefinitionData> enemyDefinitions,
        List<EncounterDefinitionData> encounterDefinitions
    )
    {
        return CreateRuntimeStateFromDefinitions(
            encounterID,
            cards,
            characterDefinitions,
            enemyDefinitions,
            encounterDefinitions,
            false
        );
    }

    public static BattleDefinitionBootstrapResult CreateRuntimeStateFromDefinitions(
        string encounterID,
        List<CardTestData> cards,
        List<CharacterDefinitionData> characterDefinitions,
        List<EnemyDefinitionData> enemyDefinitions,
        List<EncounterDefinitionData> encounterDefinitions,
        bool useSingleUnitDemo
    )
    {
        if (cards == null)
        {
            return BattleDefinitionBootstrapResult.Failure("创建Runtime失败：卡牌数据为空");
        }

        if (characterDefinitions == null)
        {
            return BattleDefinitionBootstrapResult.Failure("创建Runtime失败：角色定义为空");
        }

        if (enemyDefinitions == null)
        {
            return BattleDefinitionBootstrapResult.Failure("创建Runtime失败：敌人定义为空");
        }

        if (encounterDefinitions == null)
        {
            return BattleDefinitionBootstrapResult.Failure("创建Runtime失败：遭遇定义为空");
        }

        EncounterDefinitionData encounterDefinition = EncounterDefinitionLoader.FindByID(encounterDefinitions, encounterID);

        if (encounterDefinition == null)
        {
            return BattleDefinitionBootstrapResult.Failure("创建Runtime失败：找不到遭遇 " + encounterID);
        }

        string errorMessage;

        if (!ValidateEncounterReferences(
            encounterDefinition,
            cards,
            characterDefinitions,
            enemyDefinitions,
            out errorMessage
        ))
        {
            return BattleDefinitionBootstrapResult.Failure(errorMessage);
        }

        CharacterDefinitionData allyADefinition = CharacterDefinitionLoader.FindByID(characterDefinitions, encounterDefinition.allyCharacterIDs[0]);
        CharacterDefinitionData allyBDefinition = useSingleUnitDemo
            ? null
            : CharacterDefinitionLoader.FindByID(
                characterDefinitions,
                encounterDefinition.allyCharacterIDs[1]
            );
        EnemyDefinitionData enemyDefinition = EnemyDefinitionLoader.FindByID(enemyDefinitions, encounterDefinition.enemyID);

        BattleUnitFactoryResult allyAResult = BattleUnitFactory.CreatePlayer(allyADefinition, cards);

        if (!allyAResult.isSuccess)
        {
            return BattleDefinitionBootstrapResult.Failure(allyAResult.errorMessage);
        }

        BattleUnitFactoryResult allyBResult = useSingleUnitDemo
            ? null
            : BattleUnitFactory.CreatePlayer(allyBDefinition, cards);

        if (allyBResult != null && !allyBResult.isSuccess)
        {
            return BattleDefinitionBootstrapResult.Failure(allyBResult.errorMessage);
        }

        BattleUnitFactoryResult enemyResult = BattleUnitFactory.CreateEnemy(enemyDefinition, cards);

        if (!enemyResult.isSuccess)
        {
            return BattleDefinitionBootstrapResult.Failure(enemyResult.errorMessage);
        }

        // 2+2兼容模式继续创建独立的第二敌人；1v1 Demo只保留第一实例。
        BattleUnitFactoryResult enemy2Result = useSingleUnitDemo
            ? null
            : BattleUnitFactory.CreateEnemy(
                enemyDefinition,
                cards,
                enemyDefinition.enemyID + "_02"
            );

        if (enemy2Result != null && !enemy2Result.isSuccess)
        {
            return BattleDefinitionBootstrapResult.Failure(
                enemy2Result.errorMessage
            );
        }

        Dictionary<string, CharacterData> allyByID = new Dictionary<string, CharacterData>();
        allyByID.Add(allyADefinition.characterID, allyAResult.unit);
        if (allyBDefinition != null && allyBResult != null)
        {
            allyByID.Add(allyBDefinition.characterID, allyBResult.unit);
        }

        BattleRuntimeState runtimeState = new BattleRuntimeState();
        runtimeState.SetCharacters(
            allyAResult.unit,
            allyBResult != null ? allyBResult.unit : null,
            enemyResult.unit,
            enemy2Result != null ? enemy2Result.unit : null
        );
        runtimeState.SetActionSlots(BattleActionSlotManager.CreatePartyActionSlots(
            allyAResult.unit,
            allyBResult != null ? allyBResult.unit : null,
            2
        ));

        BattleDefinitionIntentQueueResult intentResult = CreateIntentQueueForTurn(
            runtimeState,
            encounterDefinition,
            enemyDefinition,
            allyByID,
            runtimeState.currentTurn
        );

        if (!intentResult.isSuccess)
        {
            return BattleDefinitionBootstrapResult.Failure(intentResult.errorMessage);
        }

        runtimeState.SetIntentQueue(intentResult.intentQueue);
        runtimeState.ClearExecutionPlan();
        string transitionFailure;
        BattleLifecycleController lifecycleController =
            new BattleLifecycleController(runtimeState);
        if (!lifecycleController.TryInitializeToPrepare(out transitionFailure))
        {
            return BattleDefinitionBootstrapResult.Failure(transitionFailure);
        }

        return BattleDefinitionBootstrapResult.Success(
            runtimeState,
            encounterDefinition,
            allyADefinition,
            allyBDefinition,
            enemyDefinition,
            allyByID
        );
    }

    public static BattleDefinitionIntentQueueResult CreateIntentQueueForTurn(
        BattleRuntimeState runtimeState,
        EncounterDefinitionData encounterDefinition,
        EnemyDefinitionData enemyDefinition,
        Dictionary<string, CharacterData> allyByID,
        int currentTurn
    )
    {
        return CreateIntentQueueForTurn(
            runtimeState,
            encounterDefinition,
            enemyDefinition,
            allyByID,
            currentTurn,
            runtimeState != null ? runtimeState.actionSlots : null
        );
    }

    public static BattleDefinitionIntentQueueResult CreateIntentQueueForTurn(
        BattleRuntimeState runtimeState,
        EncounterDefinitionData encounterDefinition,
        EnemyDefinitionData enemyDefinition,
        Dictionary<string, CharacterData> allyByID,
        int currentTurn,
        List<BattleActionSlot> targetActionSlots
    )
    {
        BattleDefinitionIntentQueueResult result = new BattleDefinitionIntentQueueResult();
        result.isSuccess = true;
        result.errorMessage = "";

        if (runtimeState == null)
        {
            return BattleDefinitionIntentQueueResult.Failure("创建敌人意图失败：runtimeState 为空");
        }

        if (encounterDefinition == null)
        {
            return BattleDefinitionIntentQueueResult.Failure("创建敌人意图失败：encounterDefinition 为空");
        }

        if (enemyDefinition == null)
        {
            return BattleDefinitionIntentQueueResult.Failure("创建敌人意图失败：enemyDefinition 为空");
        }

        if (allyByID == null)
        {
            return BattleDefinitionIntentQueueResult.Failure("创建敌人意图失败：allyByID 为空");
        }

        if (!encounterDefinition.repeatIntentPattern && currentTurn > 1)
        {
            result.intentQueue = BattleEnemyIntentManager.CreateIntentQueue();
            return result;
        }

        if (targetActionSlots == null)
        {
            return BattleDefinitionIntentQueueResult.Failure(
                "创建敌人意图失败：targetActionSlots 为空"
            );
        }

        string errorMessage;

        if (runtimeState.enemyUnits == null ||
            runtimeState.enemyUnits.Count < 1 ||
            runtimeState.enemyUnits.Count > 2)
        {
            return BattleDefinitionIntentQueueResult.Failure(
                "创建敌人意图失败：正式敌人数量必须为1到2"
            );
        }

        List<BattleEnemyIntent> intents = new List<BattleEnemyIntent>();

        for (int enemyIndex = 0;
            enemyIndex < runtimeState.enemyUnits.Count;
            enemyIndex++)
        {
            CharacterData enemyUnit = runtimeState.enemyUnits[enemyIndex];
            if (!ValidateIntentPatternAgainstEnemy(
                    encounterDefinition,
                    enemyUnit,
                    out errorMessage))
            {
                return BattleDefinitionIntentQueueResult.Failure(errorMessage);
            }

            HashSet<int> usedEnemyCardIndexes = new HashSet<int>();
            int enemySlotIndex = 0;
            foreach (EnemyIntentDefinitionData intentDefinition in
                encounterDefinition.intentPattern)
            {
                enemySlotIndex++;
                // 每名敌人的同一BattleCardState在一回合最多绑定一个意图。
                int enemyCardIndex = intentDefinition.enemyCardIndex;

                if (usedEnemyCardIndexes.Contains(enemyCardIndex))
                {
                    return BattleDefinitionIntentQueueResult.Failure(
                        "创建敌人意图失败：同一敌人同一回合重复使用 enemyCardIndex " +
                        enemyCardIndex
                    );
                }

                usedEnemyCardIndexes.Add(enemyCardIndex);

                BattleCardState enemyCardState =
                    enemyUnit.battleCards[enemyCardIndex - 1];
                CharacterData targetCharacter;
                int targetSlotIndex;

                if (!TryResolveTarget(
                    encounterDefinition,
                    allyByID,
                    targetActionSlots,
                    intentDefinition,
                    out targetCharacter,
                    out targetSlotIndex,
                    result.warningMessages
                ))
                {
                    continue;
                }

                CardEligibilityResult eligibility =
                    BattleCardManager.EvaluateCardEligibility(
                        enemyUnit,
                        targetCharacter,
                        enemyCardState
                    );

                if (!eligibility.isEligible)
                {
                    string warningMessage =
                        "敌人意图跳过：敌人 " +
                        enemyUnit.runtimeUnitID +
                        " 的卡牌 " +
                        enemyCardState.GetCardName() +
                        " 当前不可用，原因：" +
                        eligibility.failureMessage;

                    result.warningMessages.Add(warningMessage);
                    Debug.LogWarning(warningMessage);
                    continue;
                }

                int intentOrder = intents.Count + 1;
                BattleEnemyIntent intent = new BattleEnemyIntent(
                    encounterDefinition.encounterID +
                        "_enemy_" + (enemyIndex + 1) +
                        "_intent_" + intentOrder,
                    enemyUnit,
                    enemyCardState,
                    targetCharacter,
                    targetSlotIndex,
                    intentOrder,
                    enemySlotIndex
                );

                intents.Add(intent);
            }
        }

        result.intentQueue = BattleEnemyIntentManager.CreateIntentQueue(intents.ToArray());
        return result;
    }

    static bool ValidateEncounterReferences(
        EncounterDefinitionData encounterDefinition,
        List<CardTestData> cards,
        List<CharacterDefinitionData> characterDefinitions,
        List<EnemyDefinitionData> enemyDefinitions,
        out string errorMessage
    )
    {
        errorMessage = "";

        if (!EncounterDefinitionLoader.ValidateDefinition(encounterDefinition, out errorMessage))
        {
            return false;
        }

        foreach (string allyID in encounterDefinition.allyCharacterIDs)
        {
            CharacterDefinitionData allyDefinition = CharacterDefinitionLoader.FindByID(characterDefinitions, allyID);

            if (allyDefinition == null)
            {
                errorMessage = "创建Runtime失败：找不到角色定义 " + allyID;
                return false;
            }
        }

        EnemyDefinitionData enemyDefinition = EnemyDefinitionLoader.FindByID(enemyDefinitions, encounterDefinition.enemyID);

        if (enemyDefinition == null)
        {
            errorMessage = "创建Runtime失败：找不到敌人定义 " + encounterDefinition.enemyID;
            return false;
        }

        foreach (EnemyIntentDefinitionData intentDefinition in encounterDefinition.intentPattern)
        {
            if (intentDefinition.enemyCardIndex > enemyDefinition.cardIDs.Length)
            {
                errorMessage = "创建Runtime失败：enemyCardIndex 超出敌人卡牌数量：" + intentDefinition.enemyCardIndex;
                return false;
            }

            string enemyCardID = enemyDefinition.cardIDs[intentDefinition.enemyCardIndex - 1];

            if (CardDataLoader.FindCardByID(cards, enemyCardID) == null)
            {
                errorMessage = "创建Runtime失败：敌人意图引用不存在的卡牌 " + enemyCardID;
                return false;
            }

            if (intentDefinition.targetRule == EncounterDefinitionLoader.TargetRuleFixedCharacterSlot &&
                CharacterDefinitionLoader.FindByID(characterDefinitions, intentDefinition.targetCharacterID) == null)
            {
                errorMessage = "创建Runtime失败：固定目标角色不存在 " + intentDefinition.targetCharacterID;
                return false;
            }
        }

        return true;
    }

    static bool ValidateIntentPatternAgainstEnemy(
        EncounterDefinitionData encounterDefinition,
        CharacterData enemy,
        out string errorMessage
    )
    {
        errorMessage = "";

        if (enemy == null || enemy.battleCards == null)
        {
            errorMessage = "创建敌人意图失败：敌人或敌人卡牌为空";
            return false;
        }

        HashSet<int> enemyCardIndexes = new HashSet<int>();

        foreach (EnemyIntentDefinitionData intentDefinition in encounterDefinition.intentPattern)
        {
            if (intentDefinition == null)
            {
                errorMessage = "创建敌人意图失败：存在空意图定义";
                return false;
            }

            if (intentDefinition.enemyCardIndex <= 0 || intentDefinition.enemyCardIndex > enemy.battleCards.Count)
            {
                errorMessage = "创建敌人意图失败：enemyCardIndex 超出范围 " + intentDefinition.enemyCardIndex;
                return false;
            }

            if (enemyCardIndexes.Contains(intentDefinition.enemyCardIndex))
            {
                errorMessage = "创建敌人意图失败：同一回合重复使用 enemyCardIndex " + intentDefinition.enemyCardIndex;
                return false;
            }

            enemyCardIndexes.Add(intentDefinition.enemyCardIndex);
        }

        return true;
    }

    static bool TryResolveTarget(
        EncounterDefinitionData encounterDefinition,
        Dictionary<string, CharacterData> allyByID,
        List<BattleActionSlot> targetActionSlots,
        EnemyIntentDefinitionData intentDefinition,
        out CharacterData targetCharacter,
        out int targetSlotIndex,
        List<string> warningMessages
    )
    {
        targetCharacter = null;
        targetSlotIndex = intentDefinition.targetSlotIndex;

        if (intentDefinition.targetRule == EncounterDefinitionLoader.TargetRuleFixedCharacterSlot)
        {
            CharacterData fixedTarget = null;
            allyByID.TryGetValue(intentDefinition.targetCharacterID, out fixedTarget);

            // 只在创建本回合意图时处理死亡固定目标回落。
            // 意图创建后目标死亡，执行阶段仍按ActualTargetDead跳过，不临时转火。
            if (fixedTarget != null &&
                !fixedTarget.IsDead() &&
                HasActionSlot(targetActionSlots, fixedTarget, intentDefinition.targetSlotIndex))
            {
                targetCharacter = fixedTarget;
                return true;
            }

            CharacterData fallbackTarget;

            if (TryFindFirstLivingTarget(
                    encounterDefinition,
                    allyByID,
                    targetActionSlots,
                    intentDefinition.targetSlotIndex,
                    out fallbackTarget))
            {
                targetCharacter = fallbackTarget;
                return true;
            }

            AddWarning(warningMessages, "敌人意图跳过：固定目标死亡或无合法槽位，且没有可回落目标");
            return false;
        }

        if (intentDefinition.targetRule == EncounterDefinitionLoader.TargetRuleFirstLivingCharacterSlot)
        {
            CharacterData firstLivingTarget;

            if (TryFindFirstLivingTarget(
                    encounterDefinition,
                    allyByID,
                    targetActionSlots,
                    intentDefinition.targetSlotIndex,
                    out firstLivingTarget))
            {
                targetCharacter = firstLivingTarget;
                return true;
            }

            AddWarning(warningMessages, "敌人意图跳过：没有可用的第一存活目标");
            return false;
        }

        AddWarning(warningMessages, "敌人意图跳过：未知 targetRule " + intentDefinition.targetRule);
        return false;
    }

    static bool TryFindFirstLivingTarget(
        EncounterDefinitionData encounterDefinition,
        Dictionary<string, CharacterData> allyByID,
        List<BattleActionSlot> targetActionSlots,
        int targetSlotIndex,
        out CharacterData targetCharacter
    )
    {
        targetCharacter = null;

        foreach (string allyID in encounterDefinition.allyCharacterIDs)
        {
            CharacterData ally;

            if (!allyByID.TryGetValue(allyID, out ally))
            {
                continue;
            }

            if (ally == null || ally.IsDead())
            {
                continue;
            }

            if (!HasActionSlot(targetActionSlots, ally, targetSlotIndex))
            {
                continue;
            }

            targetCharacter = ally;
            return true;
        }

        return false;
    }

    static bool HasActionSlot(List<BattleActionSlot> actionSlots, CharacterData owner, int slotIndex)
    {
        if (actionSlots == null || owner == null)
        {
            return false;
        }

        foreach (BattleActionSlot slot in actionSlots)
        {
            if (slot != null &&
                object.ReferenceEquals(slot.owner, owner) &&
                slot.slotIndex == slotIndex)
            {
                return true;
            }
        }

        return false;
    }

    static void AddWarning(List<string> warningMessages, string message)
    {
        if (warningMessages != null)
        {
            warningMessages.Add(message);
        }

        Debug.LogWarning(message);
    }
}
