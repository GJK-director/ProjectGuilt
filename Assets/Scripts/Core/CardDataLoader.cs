using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// CardDataLoader = 卡牌数据读取器
// 负责读取 JSON、打印卡牌效果、按 ID 查找卡牌
public static class CardDataLoader
{
    // LoadCardData = 读取卡牌 JSON 数据
    public static List<CardTestData> LoadCardData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/CardsTest");

        if (jsonFile == null)
        {
            Debug.LogError("没有找到 CardsTest.json，请检查路径：Assets/Resources/Data/CardsTest.json");
            return null;
        }

        string jsonText = Encoding.UTF8.GetString(jsonFile.bytes);

        List<CardTestData> cards = JsonConvert.DeserializeObject<List<CardTestData>>(jsonText);

        if (cards == null)
        {
            Debug.LogError("JSON 转换失败：cards 是空的。");
            return null;
        }

        // showJsonLog = 是否显示完整 JSON 原文
        if (BattleDebugSettings.ShowJsonLog)
        {
            Debug.Log("成功读取到 JSON 文件，原文内容是：\n" + jsonText);
        }
        else
        {
            Debug.Log("成功读取卡牌数据，共 " + cards.Count + " 张卡牌");
        }

        return cards;
    }

    // PrintCardEffects = 打印所有卡牌的效果
    public static void PrintCardEffects(List<CardTestData> cards)
    {
        // 卡牌效果列表属于详细调试信息
        // showDetailBattleLog 不打开时，不打印这部分
        if (!BattleDebugSettings.ShowDetailBattleLog)
        {
            return;
        }

        foreach (CardTestData card in cards)
        {
            if (card == null)
            {
                Debug.LogWarning("发现一条空卡牌数据。");
                continue;
            }

            if (string.IsNullOrEmpty(card.cardName))
            {
                Debug.LogWarning("读取到一张卡，但 cardName 是空的。卡牌ID：" + card.cardID);
            }

            if (card.effects != null && card.effects.Count > 0)
            {
                foreach (CardEffectData effect in card.effects)
                {
                    Debug.Log(
                        card.cardName + " 拥有效果：" +
                        effect.trigger + " / " +
                        effect.effectType + " / " +
                        effect.target + " / " +
                        effect.buffType + " x" +
                        effect.stack + "，持续 " +
                        effect.duration + " 回合"
                    );
                }
            }
        }
    }

    // FindCardByID = 根据 cardID 查找卡牌
    public static CardTestData FindCardByID(List<CardTestData> cards, string targetID)
    {
        foreach (CardTestData card in cards)
        {
            if (card == null)
            {
                continue;
            }

            if (card.cardID == targetID)
            {
                return card;
            }
        }

        return null;
    }
}