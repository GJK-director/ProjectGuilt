using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class CharacterDefinitionLoader
{
    const string ResourcePath = "Data/Characters/CharacterDefinitions";

    public static List<CharacterDefinitionData> LoadDefinitions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(ResourcePath);

        if (jsonFile == null)
        {
            Debug.LogError("没有找到 CharacterDefinitions.json，请检查路径：Assets/Resources/Data/Characters/CharacterDefinitions.json");
            return null;
        }

        string jsonText = Encoding.UTF8.GetString(jsonFile.bytes);
        List<CharacterDefinitionData> definitions = JsonConvert.DeserializeObject<List<CharacterDefinitionData>>(jsonText);

        if (definitions == null)
        {
            Debug.LogError("CharacterDefinitions.json 解析失败");
            return null;
        }

        Dictionary<string, CharacterDefinitionData> idMap = new Dictionary<string, CharacterDefinitionData>();

        foreach (CharacterDefinitionData definition in definitions)
        {
            string errorMessage;

            if (!ValidateDefinition(definition, out errorMessage))
            {
                Debug.LogError("CharacterDefinitions.json 校验失败：" + errorMessage);
                return null;
            }

            if (idMap.ContainsKey(definition.characterID))
            {
                Debug.LogError("CharacterDefinitions.json 中发现重复 characterID：" + definition.characterID);
                return null;
            }

            idMap.Add(definition.characterID, definition);
        }

        Debug.Log("成功读取角色定义，共 " + definitions.Count + " 个");
        return definitions;
    }

    public static CharacterDefinitionData FindByID(List<CharacterDefinitionData> definitions, string characterID)
    {
        if (definitions == null || string.IsNullOrEmpty(characterID))
        {
            return null;
        }

        foreach (CharacterDefinitionData definition in definitions)
        {
            if (definition != null && definition.characterID == characterID)
            {
                return definition;
            }
        }

        return null;
    }

    static bool ValidateDefinition(CharacterDefinitionData definition, out string errorMessage)
    {
        errorMessage = "";

        if (definition == null)
        {
            errorMessage = "存在空角色定义";
            return false;
        }

        if (string.IsNullOrEmpty(definition.characterID))
        {
            errorMessage = "characterID 为空";
            return false;
        }

        if (string.IsNullOrEmpty(definition.characterName))
        {
            errorMessage = definition.characterID + " 的 characterName 为空";
            return false;
        }

        if (definition.maxHP <= 0)
        {
            errorMessage = definition.characterID + " 的 maxHP 必须大于0";
            return false;
        }

        if (definition.minSpeed < 0 || definition.maxSpeed < definition.minSpeed)
        {
            errorMessage = definition.characterID + " 的速度范围非法";
            return false;
        }

        if (definition.actionSlotCount != 2)
        {
            errorMessage = definition.characterID + " 的 actionSlotCount 必须等于2";
            return false;
        }

        if (!ValidateStringArray(definition.startingCardIDs))
        {
            errorMessage = definition.characterID + " 的 startingCardIDs 为空或包含空ID";
            return false;
        }

        if (!ValidateInitialBuffs(definition.initialBuffs, definition.characterID, out errorMessage))
        {
            return false;
        }

        if (string.IsNullOrEmpty(definition.prefabKey) || string.IsNullOrEmpty(definition.portraitKey))
        {
            errorMessage = definition.characterID + " 的 prefabKey 或 portraitKey 为空";
            return false;
        }

        return true;
    }

    static bool ValidateStringArray(string[] values)
    {
        if (values == null || values.Length == 0)
        {
            return false;
        }

        foreach (string value in values)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
        }

        return true;
    }

    static bool ValidateInitialBuffs(InitialBuffDefinitionData[] initialBuffs, string ownerID, out string errorMessage)
    {
        errorMessage = "";

        if (initialBuffs == null)
        {
            return true;
        }

        foreach (InitialBuffDefinitionData initialBuff in initialBuffs)
        {
            if (initialBuff == null)
            {
                errorMessage = ownerID + " 存在空 initialBuff";
                return false;
            }

            if (string.IsNullOrEmpty(initialBuff.buffID))
            {
                errorMessage = ownerID + " 存在 buffID 为空的 initialBuff";
                return false;
            }

            if (initialBuff.stack <= 0)
            {
                errorMessage = ownerID + " 的 initialBuff " + initialBuff.buffID + " stack 必须大于0";
                return false;
            }

            if (initialBuff.duration != -1 && initialBuff.duration <= 0)
            {
                errorMessage = ownerID + " 的 initialBuff " + initialBuff.buffID + " duration 必须为-1或大于0";
                return false;
            }
        }

        return true;
    }
}
