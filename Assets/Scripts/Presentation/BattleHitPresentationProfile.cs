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
    [SerializeField, Min(0f)] private float verticalShakeAmplitude = 0.15f;
    [SerializeField, Min(0f)] private float verticalShakeOscillations = 2f;
    [SerializeField, Min(0f)] private float hitTiltAngle = 5f;
    [SerializeField]
    private Color hitTintColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField, Range(0f, 1f)] private float hitTintStrength = 0.65f;

    public float ImpactBurstDistance => Mathf.Max(0f, impactBurstDistance);
    public float ImpactBurstDuration => Mathf.Max(0f, impactBurstDuration);
    public float FollowKnockbackDistance => Mathf.Max(0f, followKnockbackDistance);
    public float FollowKnockbackDuration => Mathf.Max(0f, followKnockbackDuration);
    public float RecoveryDuration => Mathf.Max(0f, recoveryDuration);
    public float VerticalShakeAmplitude => Mathf.Max(0f, verticalShakeAmplitude);
    public float VerticalShakeOscillations =>
        Mathf.Max(0f, verticalShakeOscillations);
    public float HitTiltAngle => Mathf.Max(0f, hitTiltAngle);
    public Color HitTintColor => hitTintColor;
    public float HitTintStrength => Mathf.Clamp01(hitTintStrength);
}
