using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class EncounterDefinitionLoader
{
    public const string TargetRuleFixedCharacterSlot = "FixedCharacterSlot";
    public const string TargetRuleFirstLivingCharacterSlot = "FirstLivingCharacterSlot";

    const string ResourcePath = "Data/Encounters/EncounterDefinitions";

    public static List<EncounterDefinitionData> LoadDefinitions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(ResourcePath);

        if (jsonFile == null)
        {
            Debug.LogError("没有找到 EncounterDefinitions.json，请检查路径：Assets/Resources/Data/Encounters/EncounterDefinitions.json");
            return null;
        }

        string jsonText = Encoding.UTF8.GetString(jsonFile.bytes);
        List<EncounterDefinitionData> definitions = JsonConvert.DeserializeObject<List<EncounterDefinitionData>>(jsonText);

        if (definitions == null)
        {
            Debug.LogError("EncounterDefinitions.json 解析失败");
            return null;
        }

        Dictionary<string, EncounterDefinitionData> idMap = new Dictionary<string, EncounterDefinitionData>();

        foreach (EncounterDefinitionData definition in definitions)
        {
            string errorMessage;

            if (!ValidateDefinition(definition, out errorMessage))
            {
                Debug.LogError("EncounterDefinitions.json 校验失败：" + errorMessage);
                return null;
            }

            if (idMap.ContainsKey(definition.encounterID))
            {
                Debug.LogError("EncounterDefinitions.json 中发现重复 encounterID：" + definition.encounterID);
                return null;
            }

            idMap.Add(definition.encounterID, definition);
        }

        Debug.Log("成功读取遭遇定义，共 " + definitions.Count + " 个");
        return definitions;
    }

    public static EncounterDefinitionData FindByID(List<EncounterDefinitionData> definitions, string encounterID)
    {
        if (definitions == null || string.IsNullOrEmpty(encounterID))
        {
            return null;
        }

        foreach (EncounterDefinitionData definition in definitions)
        {
            if (definition != null && definition.encounterID == encounterID)
            {
                return definition;
            }
        }

        return null;
    }

    public static bool ValidateDefinition(EncounterDefinitionData definition, out string errorMessage)
    {
        errorMessage = "";

        if (definition == null)
        {
            errorMessage = "存在空遭遇定义";
            return false;
        }

        if (string.IsNullOrEmpty(definition.encounterID))
        {
            errorMessage = "encounterID 为空";
            return false;
        }

        if (string.IsNullOrEmpty(definition.encounterName))
        {
            errorMessage = definition.encounterID + " 的 encounterName 为空";
            return false;
        }

        if (definition.allyCharacterIDs == null || definition.allyCharacterIDs.Length != 2)
        {
            errorMessage = definition.encounterID + " 的 allyCharacterIDs 必须正好有2个";
            return false;
        }

        foreach (string allyID in definition.allyCharacterIDs)
        {
            if (string.IsNullOrEmpty(allyID))
            {
                errorMessage = definition.encounterID + " 的 allyCharacterIDs 包含空ID";
                return false;
            }
        }

        if (string.IsNullOrEmpty(definition.enemyID))
        {
            errorMessage = definition.encounterID + " 的 enemyID 为空";
            return false;
        }

        if (definition.intentPattern == null || definition.intentPattern.Length == 0)
        {
            errorMessage = definition.encounterID + " 的 intentPattern 为空";
            return false;
        }

        HashSet<int> enemyCardIndexes = new HashSet<int>();

        foreach (EnemyIntentDefinitionData intentDefinition in definition.intentPattern)
        {
            if (intentDefinition == null)
            {
                errorMessage = definition.encounterID + " 存在空 intentPattern 项";
                return false;
            }

            if (intentDefinition.enemyCardIndex <= 0)
            {
                errorMessage = definition.encounterID + " 的 enemyCardIndex 必须大于0";
                return false;
            }

            if (enemyCardIndexes.Contains(intentDefinition.enemyCardIndex))
            {
                errorMessage = definition.encounterID + " 同一轮 intentPattern 重复使用 enemyCardIndex：" + intentDefinition.enemyCardIndex;
                return false;
            }

            enemyCardIndexes.Add(intentDefinition.enemyCardIndex);

            if (intentDefinition.targetRule != TargetRuleFixedCharacterSlot &&
                intentDefinition.targetRule != TargetRuleFirstLivingCharacterSlot)
            {
                errorMessage = definition.encounterID + " 的 targetRule 非法：" + intentDefinition.targetRule;
                return false;
            }

            if (intentDefinition.targetRule == TargetRuleFixedCharacterSlot &&
                string.IsNullOrEmpty(intentDefinition.targetCharacterID))
            {
                errorMessage = definition.encounterID + " 的 FixedCharacterSlot 缺少 targetCharacterID";
                return false;
            }

            if (intentDefinition.targetSlotIndex <= 0)
            {
                errorMessage = definition.encounterID + " 的 targetSlotIndex 必须大于0";
                return false;
            }
        }

        if (string.IsNullOrEmpty(definition.battleBackgroundKey) || string.IsNullOrEmpty(definition.battleMusicKey))
        {
            errorMessage = definition.encounterID + " 的 battleBackgroundKey 或 battleMusicKey 为空";
            return false;
        }

        return true;
    }
}
