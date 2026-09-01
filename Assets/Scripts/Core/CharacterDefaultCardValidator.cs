// 脚本中文说明：统一验证角色默认卡引用，并通过 CardDataLoader 解析权威卡牌数据。
using System.Collections.Generic;

public static class CharacterDefaultCardValidator
{
    public static bool TryResolve(
        string ownerID,
        string[] defaultCardIDs,
        List<CardTestData> cards,
        out List<CardTestData> resolvedCards,
        out string errorMessage
    )
    {
        resolvedCards = new List<CardTestData>();
        errorMessage = string.Empty;

        string safeOwnerID = string.IsNullOrEmpty(ownerID)
            ? "<unknown>"
            : ownerID;

        if (cards == null)
        {
            errorMessage = safeOwnerID + " 默认卡校验失败：卡牌数据列表为空";
            return false;
        }

        if (defaultCardIDs == null || defaultCardIDs.Length == 0)
        {
            errorMessage = safeOwnerID + " 默认卡校验失败：默认 CardID 列表为空";
            return false;
        }

        for (int index = 0; index < defaultCardIDs.Length; index++)
        {
            string cardID = defaultCardIDs[index];
            if (string.IsNullOrEmpty(cardID))
            {
                errorMessage =
                    safeOwnerID + " 默认卡校验失败：索引 " + index +
                    " 的 CardID 为空";
                resolvedCards.Clear();
                return false;
            }

            CardTestData card;
            if (!CardDataLoader.TryFindCardByID(cards, cardID, out card))
            {
                errorMessage =
                    safeOwnerID + " 默认卡校验失败：找不到卡牌 " + cardID;
                resolvedCards.Clear();
                return false;
            }

            // 重复 ID 表示角色持有同一模板的多张复制品，属于合法数据。
            resolvedCards.Add(card);
        }

        return true;
    }
}
