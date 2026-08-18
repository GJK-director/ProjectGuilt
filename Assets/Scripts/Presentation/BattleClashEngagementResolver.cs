using UnityEngine;

// 只计算接敌间距和双方位移份额，不读取战斗规则或移动场景对象。
public static class BattleClashEngagementResolver
{
    public static BattleClashEngagementResult Resolve(
        BattleClashEngagementProfile profile,
        string sideAPresentationKey,
        string sideBPresentationKey,
        float sideASpeed,
        float sideBSpeed
    )
    {
        if (profile == null)
        {
            return null;
        }

        float safeSideASpeed = Mathf.Max(0f, sideASpeed);
        float safeSideBSpeed = Mathf.Max(0f, sideBSpeed);
        float influence = profile.RelativeSpeedInfluence;
        float minShare = profile.MinMovementShare;
        float maxShare = profile.MaxMovementShare;

        // A、B都必须落在同一份额范围内，因此使用范围与其镜像的交集。
        float effectiveMinShare = Mathf.Max(minShare, 1f - maxShare);
        float effectiveMaxShare = Mathf.Min(maxShare, 1f - minShare);
        if (effectiveMinShare > effectiveMaxShare)
        {
            effectiveMinShare = 0.5f;
            effectiveMaxShare = 0.5f;
        }

        float totalSpeed = safeSideASpeed + safeSideBSpeed;
        float sideAShare = 0.5f;
        if (totalSpeed > Mathf.Epsilon)
        {
            float relativeAdvantage =
                (safeSideASpeed - safeSideBSpeed) / totalSpeed;
            sideAShare = 0.5f + relativeAdvantage * influence;
        }

        sideAShare = Mathf.Clamp(
            sideAShare,
            effectiveMinShare,
            effectiveMaxShare
        );
        float sideBShare = 1f - sideAShare;

        float spacingA = profile.GetCharacterSpacing(sideAPresentationKey);
        float spacingB = profile.GetCharacterSpacing(sideBPresentationKey);
        float pairGap = 0f;
        bool pairOverrideUsed = profile.TryGetPairGap(
            sideAPresentationKey,
            sideBPresentationKey,
            out pairGap
        );
        float defaultGap = profile.DefaultClashReadyGap;
        float finalGap = pairOverrideUsed
            ? pairGap
            : defaultGap + spacingA + spacingB;

        return new BattleClashEngagementResult(
            Mathf.Max(0f, finalGap),
            sideAShare,
            sideBShare,
            safeSideASpeed,
            safeSideBSpeed,
            spacingA,
            spacingB,
            pairOverrideUsed
        );
    }
}
