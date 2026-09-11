using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class BuffDefinitionLoader
{
    const string ResourcePath = "Data/Buffs/BuffDefinitions";

    static List<BuffDefinitionData> cachedDefinitions;
    static Dictionary<string, BuffDefinitionData> cachedDefinitionByID;

    public static List<BuffDefinitionData> LoadBuffDefinitions()
    {
        if (cachedDefinitions != null && cachedDefinitionByID != null)
        {
            return cachedDefinitions;
        }

        cachedDefinitions = new List<BuffDefinitionData>();
        cachedDefinitionByID = new Dictionary<string, BuffDefinitionData>();

        TextAsset jsonFile = Resources.Load<TextAsset>(ResourcePath);

        if (jsonFile == null)
        {
            Debug.LogError("没有找到 BuffDefinitions.json，请检查路径：Assets/Resources/Data/Buffs/BuffDefinitions.json");
            return cachedDefinitions;
        }

        string jsonText = Encoding.UTF8.GetString(jsonFile.bytes);
        List<BuffDefinitionData> definitions = JsonConvert.DeserializeObject<List<BuffDefinitionData>>(jsonText);

        if (definitions == null)
        {
            Debug.LogError("BuffDefinitions.json 解析失败，definitions 为空");
            return cachedDefinitions;
        }

        foreach (BuffDefinitionData definition in definitions)
        {
            if (definition == null)
            {
                Debug.LogError("BuffDefinitions 中存在空定义");
                continue;
            }

            if (string.IsNullOrEmpty(definition.buffID))
            {
                Debug.LogError("BuffDefinitions 中存在 buffID 为空的定义");
                continue;
            }

            if (cachedDefinitionByID.ContainsKey(definition.buffID))
            {
                Debug.LogError("BuffDefinitions 中发现重复 buffID：" + definition.buffID + "，已忽略重复定义");
                continue;
            }

            NormalizeDefinition(definition);
            cachedDefinitions.Add(definition);
            cachedDefinitionByID.Add(definition.buffID, definition);
        }

        Debug.Log("成功读取 Buff 定义，共 " + cachedDefinitions.Count + " 种");

        return cachedDefinitions;
    }

    public static bool TryGetDefinition(string buffID, out BuffDefinitionData definition)
    {
        definition = null;

        if (string.IsNullOrEmpty(buffID))
        {
            return false;
        }

        LoadBuffDefinitions();

        if (cachedDefinitionByID == null)
        {
            return false;
        }

        return cachedDefinitionByID.TryGetValue(buffID, out definition);
    }

    public static BuffDefinitionData GetDefinition(string buffID)
    {
        BuffDefinitionData definition;

        if (TryGetDefinition(buffID, out definition))
        {
            return definition;
        }

        Debug.LogWarning("找不到 Buff 定义：" + buffID);
        return null;
    }

    internal static void ClearCacheForTest()
    {
        cachedDefinitions = null;
        cachedDefinitionByID = null;
    }

    static void NormalizeDefinition(BuffDefinitionData definition)
    {
        if (string.IsNullOrEmpty(definition.consumeRule))
        {
            definition.consumeRule = "None";
        }

        if (definition.description == null)
        {
            definition.description = "";
        }
    }
}
