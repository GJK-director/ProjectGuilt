public sealed class BattleClashEngagementResult
{
    public float FinalGap { get; }
    public float SideAMovementShare { get; }
    public float SideBMovementShare { get; }
    public float SideASpeed { get; }
    public float SideBSpeed { get; }
    public float CharacterSpacingA { get; }
    public float CharacterSpacingB { get; }
    public bool WasPairOverrideUsed { get; }

    public BattleClashEngagementResult(
        float finalGap,
        float sideAMovementShare,
        float sideBMovementShare,
        float sideASpeed,
        float sideBSpeed,
        float characterSpacingA,
        float characterSpacingB,
        bool wasPairOverrideUsed
    )
    {
        FinalGap = finalGap;
        SideAMovementShare = sideAMovementShare;
        SideBMovementShare = sideBMovementShare;
        SideASpeed = sideASpeed;
        SideBSpeed = sideBSpeed;
        CharacterSpacingA = characterSpacingA;
        CharacterSpacingB = characterSpacingB;
        WasPairOverrideUsed = wasPairOverrideUsed;
    }
}
