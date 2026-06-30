using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class EnemyDefinitionLoader
{
    const string ResourcePath = "Data/Enemies/EnemyDefinitions";

    public static List<EnemyDefinitionData> LoadDefinitions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(ResourcePath);

        if (jsonFile == null)
        {
            Debug.LogError("没有找到 EnemyDefinitions.json，请检查路径：Assets/Resources/Data/Enemies/EnemyDefinitions.json");
            return null;
        }

        string jsonText = Encoding.UTF8.GetString(jsonFile.bytes);
        List<EnemyDefinitionData> definitions = JsonConvert.DeserializeObject<List<EnemyDefinitionData>>(jsonText);

        if (definitions == null)
        {
            Debug.LogError("EnemyDefinitions.json 解析失败");
            return null;
        }

        Dictionary<string, EnemyDefinitionData> idMap = new Dictionary<string, EnemyDefinitionData>();

        foreach (EnemyDefinitionData definition in definitions)
        {
            string errorMessage;

            if (!ValidateDefinition(definition, out errorMessage))
            {
                Debug.LogError("EnemyDefinitions.json 校验失败：" + errorMessage);
                return null;
            }

            if (idMap.ContainsKey(definition.enemyID))
            {
                Debug.LogError("EnemyDefinitions.json 中发现重复 enemyID：" + definition.enemyID);
                return null;
            }

            idMap.Add(definition.enemyID, definition);
        }

        Debug.Log("成功读取敌人定义，共 " + definitions.Count + " 个");
        return definitions;
    }

    public static EnemyDefinitionData FindByID(List<EnemyDefinitionData> definitions, string enemyID)
    {
        if (definitions == null || string.IsNullOrEmpty(enemyID))
        {
            return null;
        }

        foreach (EnemyDefinitionData definition in definitions)
        {
            if (definition != null && definition.enemyID == enemyID)
            {
                return definition;
            }
        }

        return null;
    }

    static bool ValidateDefinition(EnemyDefinitionData definition, out string errorMessage)
    {
        errorMessage = "";

        if (definition == null)
        {
            errorMessage = "存在空敌人定义";
            return false;
        }

        if (string.IsNullOrEmpty(definition.enemyID))
        {
            errorMessage = "enemyID 为空";
            return false;
        }

        if (string.IsNullOrEmpty(definition.enemyName))
        {
            errorMessage = definition.enemyID + " 的 enemyName 为空";
            return false;
        }

        if (definition.maxHP <= 0)
        {
            errorMessage = definition.enemyID + " 的 maxHP 必须大于0";
            return false;
        }

        if (definition.minSpeed < 0 || definition.maxSpeed < definition.minSpeed)
        {
            errorMessage = definition.enemyID + " 的速度范围非法";
            return false;
        }

        if (!ValidateStringArray(definition.cardIDs))
        {
            errorMessage = definition.enemyID + " 的 cardIDs 为空或包含空ID";
            return false;
        }

        if (!ValidateInitialBuffs(definition.initialBuffs, definition.enemyID, out errorMessage))
        {
            return false;
        }

        if (string.IsNullOrEmpty(definition.prefabKey) || string.IsNullOrEmpty(definition.portraitKey))
        {
            errorMessage = definition.enemyID + " 的 prefabKey 或 portraitKey 为空";
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
