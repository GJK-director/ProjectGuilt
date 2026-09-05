using UnityEngine;

[CreateAssetMenu(
    fileName = "BattleAttackVsGuardPresentationProfile",
    menuName = "Project Guilt/Battle/Attack Vs Guard Presentation Profile"
)]
public sealed class BattleAttackVsGuardPresentationProfile : ScriptableObject
{
    [SerializeField, Min(0f)] private float sprintDuration = 0.48f;
    [SerializeField, Min(0f)]
    private float afterimageSpawnInterval = 0.08f;
    [SerializeField, Min(0f)]
    private float guardApproachSeparation = 2.2f;
    [SerializeField, Range(0.01f, 1f)]
    private float engagementFinalMoveSpeedScale = 0.50f;
    [SerializeField, Min(0f)] private float hitStopDuration = 0.08f;
    [SerializeField] private BattleHitPresentationProfile meleeGuardReactionProfile;

    [Header("Long Range Guard")]
    [SerializeField, Min(0f)] private float longRangeGuardPreImpactHoldDuration = 0.10f;
    [SerializeField, Min(0f)] private float longRangeReducedCameraFollowRatio = 0.60f;

    public float LongRangeGuardPreImpactHoldDuration =>
        Mathf.Max(0f, longRangeGuardPreImpactHoldDuration);
    public float LongRangeReducedCameraFollowRatio =>
        Mathf.Max(0f, longRangeReducedCameraFollowRatio);

    [Header("Perfect Guard FX")]
    [SerializeField] private Sprite perfectGuardFxSprite;
    [SerializeField, Min(0f)] private float perfectGuardFxHorizontalOffset = 1.12f;
    [SerializeField] private float perfectGuardFxVerticalOffset = 2.458f;
    [SerializeField, Min(0f)] private float perfectGuardFxBaseScale = 1f;
    [SerializeField, Min(0f)] private float perfectGuardFxHoldDuration = 0.06f;
    [SerializeField, Min(0f)] private float perfectGuardFxFadeDuration = 0.10f;
    [SerializeField] private int perfectGuardFxSortingOrderOffset = 0;

    public Sprite PerfectGuardFxSprite => perfectGuardFxSprite;
    public float PerfectGuardFxHorizontalOffset => Mathf.Max(0f, perfectGuardFxHorizontalOffset);
    public float PerfectGuardFxVerticalOffset => perfectGuardFxVerticalOffset;
    public float PerfectGuardFxBaseScale => Mathf.Max(0f, perfectGuardFxBaseScale);
    public float PerfectGuardFxHoldDuration => Mathf.Max(0f, perfectGuardFxHoldDuration);
    public float PerfectGuardFxFadeDuration => Mathf.Max(0f, perfectGuardFxFadeDuration);
    public int PerfectGuardFxSortingOrderOffset => perfectGuardFxSortingOrderOffset;

    public BattleHitPresentationProfile MeleeGuardReactionProfile =>
        meleeGuardReactionProfile;

    // Profile 只保存双方Guard choreography共用的协调时间。
    public float SprintDuration => Mathf.Max(0f, sprintDuration);
    public float AfterimageSpawnInterval =>
        Mathf.Max(0f, afterimageSpawnInterval);
    public float GuardApproachSeparation =>
        Mathf.Max(0f, guardApproachSeparation);
    public float EngagementFinalMoveSpeedScale =>
        Mathf.Clamp(engagementFinalMoveSpeedScale, 0.01f, 1f);
    public float HitStopDuration => Mathf.Max(0f, hitStopDuration);
}
