using System;
using System.Collections.Generic;
using System.Text;

[System.Serializable]
public class CardKeywordData
{
    public string keywordID;      // 词条ID，例如 Strength
    public string displayName;    // 显示名称，例如 强壮
    public string tooltipText;    // 二级说明文字，例如 攻击卡点数 +1
}

// The types below are presentation-only companions to CardKeywordData.
public enum BattleCardDescriptionTokenKind { GlobalKeyword, CardLocalKeyword }

public sealed class BattleCardKeywordBinding
{
    public readonly string linkID, keywordID, displayName, tooltipText;
    public readonly BattleCardDescriptionTokenKind kind;
    public readonly CardTestData cardData;
    public BattleCardKeywordBinding(string linkID, string keywordID, string displayName,
        string tooltipText, BattleCardDescriptionTokenKind kind, CardTestData cardData)
    {
        this.linkID = linkID ?? string.Empty; this.keywordID = keywordID ?? string.Empty;
        this.displayName = displayName ?? string.Empty; this.tooltipText = tooltipText ?? string.Empty;
        this.kind = kind; this.cardData = cardData;
    }
}

public sealed class BattleCardDescriptionFormatResult
{
    readonly Dictionary<string, BattleCardKeywordBinding> bindings;
    public string richText { get; }
    public IReadOnlyDictionary<string, BattleCardKeywordBinding> keywordBindings => bindings;
    public BattleCardDescriptionFormatResult(string richText,
        Dictionary<string, BattleCardKeywordBinding> bindings)
    {
        this.richText = richText ?? string.Empty;
        this.bindings = bindings ?? new Dictionary<string, BattleCardKeywordBinding>();
    }
    public bool TryGetBinding(string linkID, out BattleCardKeywordBinding binding)
    {
        return bindings.TryGetValue(linkID ?? string.Empty, out binding);
    }
}

// Presentation-only global vocabulary. Gameplay must never consult this table.
public static class BattleGlobalKeywordLibrary
{
    static readonly CardKeywordData[] globalKeywords =
    {
        Keyword("bullet", "子弹", "射击攻击使用的资源。"),
        Keyword("anger", "怒", "部分刀系能力会生成或消耗的战斗资源。\n怒层数会强化部分攻击。\n不同卡牌对怒的使用方式不同。"),
        Keyword("conservation", "节约", "强化下一张符合条件的射击攻击。\n剩余子弹越少，获得的点数提升越高。\n强化转移给符合条件的射击后不会因该攻击失败而返还。\n如果强化射击击杀目标，会重新装填。"),
        Keyword("modification", "改装", "将弹仓容量调整为4。\n当前子弹会被限制在新的容量内。\n符合条件的射击攻击获得点数强化。"),
        Keyword("global_multiplier", "倍率", "全局倍率说明。")
    };
    public static IReadOnlyList<CardKeywordData> GlobalKeywords => globalKeywords;
    public static bool TryGetGlobalKeyword(string keywordID, out CardKeywordData keyword)
    {
        keyword = null;
        foreach (CardKeywordData candidate in globalKeywords)
        {
            if (candidate != null && candidate.keywordID == keywordID)
            {
                keyword = candidate; return true;
            }
        }
        return false;
    }
    public static List<CardKeywordData> GetCardLocalKeywords(CardTestData cardData)
    {
        List<CardKeywordData> keywords = new List<CardKeywordData>();
        if (cardData != null && cardData.keywords != null)
            foreach (CardKeywordData keyword in cardData.keywords) if (keyword != null) keywords.Add(keyword);
        if (cardData != null && cardData.cardID == "shoot_all_in_001")
            keywords.Add(Keyword("allin_multiplier", "倍率", "剩余子弹 → 伤害倍率\n1 → 100%\n2 → 180%\n3 → 230%\n4 → 270%\n5 → 300%\n6 → 320%"));
        return keywords;
    }
    static CardKeywordData Keyword(string id, string name, string tooltip)
    {
        return new CardKeywordData { keywordID = id, displayName = name, tooltipText = tooltip };
    }
}

public static class BattleCardTooltipResolver
{
    public static bool TryResolve(BattleCardKeywordBinding binding, CharacterData owner,
        out BattleSecondaryInfoContent content)
    {
        content = null;
        if (binding == null || string.IsNullOrEmpty(binding.displayName)) return false;
        string body = binding.tooltipText;
        if (binding.kind == BattleCardDescriptionTokenKind.GlobalKeyword && binding.keywordID == "bullet")
        {
            int capacity = BattleBulletRules.GetMagazineCapacity(owner);
            body = "射击攻击使用的资源。\n当前弹仓容量：" + capacity + "。\n部分射击会在 CardUsed 时消耗子弹。";
        }
        if (string.IsNullOrWhiteSpace(body)) return false;
        content = new BattleSecondaryInfoContent(binding.displayName, body,
            binding.kind == BattleCardDescriptionTokenKind.GlobalKeyword ? "全局关键词" : "卡牌关键词");
        return true;
    }
}

// One-pass formatting: Timing > card-local keyword > global keyword.
public static class BattleCardDescriptionFormatter
{
    public const string TimingColor = "#7FA9C9";
    public const string KeywordColor = "#E8C56A";
    static readonly string[] timingPhrases =
    {
        "造成伤害后", "回合开始时", "行动开始时", "回合结束时", "拼点胜利",
        "拼点失败", "结算时", "使用时", "拼点时", "命中时", "击杀时"
    };

    public static BattleCardDescriptionFormatResult Format(string rawDescription, CardTestData cardData)
    {
        string description = rawDescription ?? string.Empty;
        Dictionary<string, BattleCardKeywordBinding> bindings = new Dictionary<string, BattleCardKeywordBinding>();
        if (string.IsNullOrEmpty(description)) return new BattleCardDescriptionFormatResult(description, bindings);
        List<CardKeywordData> localKeywords = BattleGlobalKeywordLibrary.GetCardLocalKeywords(cardData);
        IReadOnlyList<CardKeywordData> globalKeywords = BattleGlobalKeywordLibrary.GlobalKeywords;
        StringBuilder builder = new StringBuilder(description.Length + 64);
        int index = 0;
        while (index < description.Length)
        {
            if (description[index] == '<')
            {
                int tagEnd = description.IndexOf('>', index);
                if (tagEnd >= index) { builder.Append(description, index, tagEnd - index + 1); index = tagEnd + 1; continue; }
            }
            string timing = FindLongestMatch(description, index, timingPhrases);
            if (!string.IsNullOrEmpty(timing))
            {
                builder.Append("<color=").Append(TimingColor).Append(">").Append(timing).Append("</color>");
                index += timing.Length; continue;
            }
            CardKeywordData local = FindLongestKeyword(description, index, localKeywords);
            if (local != null)
            {
                AppendKeyword(builder, bindings, local, cardData, BattleCardDescriptionTokenKind.CardLocalKeyword);
                index += local.displayName.Length; continue;
            }
            CardKeywordData global = FindLongestKeyword(description, index, globalKeywords);
            if (global != null)
            {
                AppendKeyword(builder, bindings, global, cardData, BattleCardDescriptionTokenKind.GlobalKeyword);
                index += global.displayName.Length; continue;
            }
            builder.Append(description[index++]);
        }
        return new BattleCardDescriptionFormatResult(builder.ToString(), bindings);
    }

    static string FindLongestMatch(string text, int index, IReadOnlyList<string> candidates)
    {
        string match = null;
        foreach (string candidate in candidates)
        {
            if (string.IsNullOrEmpty(candidate) || (match != null && candidate.Length <= match.Length) ||
                index + candidate.Length > text.Length) continue;
            if (string.CompareOrdinal(text, index, candidate, 0, candidate.Length) == 0) match = candidate;
        }
        return match;
    }

    static CardKeywordData FindLongestKeyword(string text, int index, IReadOnlyList<CardKeywordData> candidates)
    {
        CardKeywordData match = null;
        foreach (CardKeywordData candidate in candidates)
        {
            if (candidate == null || string.IsNullOrEmpty(candidate.displayName) ||
                (match != null && candidate.displayName.Length <= match.displayName.Length) ||
                index + candidate.displayName.Length > text.Length) continue;
            if (string.CompareOrdinal(text, index, candidate.displayName, 0, candidate.displayName.Length) == 0)
                match = candidate;
        }
        return match;
    }

    static void AppendKeyword(StringBuilder builder,
        Dictionary<string, BattleCardKeywordBinding> bindings, CardKeywordData keyword,
        CardTestData cardData, BattleCardDescriptionTokenKind kind)
    {
        string linkID = kind == BattleCardDescriptionTokenKind.GlobalKeyword
            ? "global:" + keyword.keywordID
            : "local:" + (cardData != null ? cardData.cardID : "card") + ":" + keyword.keywordID;
        bindings[linkID] = new BattleCardKeywordBinding(linkID, keyword.keywordID,
            keyword.displayName, keyword.tooltipText, kind, cardData);
        builder.Append("<link=\"").Append(linkID).Append("\"><color=").Append(KeywordColor)
            .Append("><u>").Append(keyword.displayName).Append("</u></color></link>");
    }
}
