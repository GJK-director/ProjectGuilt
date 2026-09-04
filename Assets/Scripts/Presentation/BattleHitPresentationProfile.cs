using UnityEngine;

[CreateAssetMenu(
    fileName = "BattleHitPresentationProfile",
    menuName = "Project Guilt/Battle/Hit Presentation Profile"
)]
public sealed class BattleHitPresentationProfile : ScriptableObject
{
    [SerializeField, Min(0f)] private float impactBurstDistance = 0.35f;
    [SerializeField, Min(0f)] private float impactBurstDuration = 0.04f;
    [SerializeField, Min(0f)] private float followKnockbackDistance = 0.65f;
    [SerializeField, Min(0f)] private float followKnockbackDuration = 0.16f;
    [SerializeField, Min(0f)] private float recoveryDuration = 0.18f;

    public float ImpactBurstDistance => Mathf.Max(0f, impactBurstDistance);
    public float ImpactBurstDuration => Mathf.Max(0f, impactBurstDuration);
    public float FollowKnockbackDistance => Mathf.Max(0f, followKnockbackDistance);
    public float FollowKnockbackDuration => Mathf.Max(0f, followKnockbackDuration);
    public float RecoveryDuration => Mathf.Max(0f, recoveryDuration);
}
