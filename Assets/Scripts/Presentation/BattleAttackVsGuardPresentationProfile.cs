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
