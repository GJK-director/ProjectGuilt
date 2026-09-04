public static class BattleCardPresentationVariant
{
    public const string Default = "Default";
    public const string SpecialLongRangeDuel = "SpecialLongRangeDuel";

    public static bool IsKnownSerializedValue(string value)
    {
        return string.IsNullOrEmpty(value) ||
            value == Default ||
            value == SpecialLongRangeDuel;
    }

    public static string ResolveOrDefault(string value)
    {
        return string.IsNullOrEmpty(value)
            ? Default
            : value;
    }
}
