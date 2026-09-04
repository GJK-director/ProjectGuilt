using UnityEngine;

[CreateAssetMenu(
    fileName = "BattleSpecialLongRangeDuelPresentationProfile",
    menuName = "Project Guilt/Battle/Special Long Range Duel Presentation Profile"
)]
public sealed class BattleSpecialLongRangeDuelPresentationProfile :
    ScriptableObject
{
    [SerializeField, Min(0f)]
    private float fastApproachThreshold = 5.5f;

    [SerializeField, Min(0f)]
    private float finalRollSeparation = 5f;

    [SerializeField, Min(0f)]
    private float fastApproachDuration = 0.22f;

    [SerializeField, Min(0f)]
    private float focusTransitionDuration = 0.18f;

    [Header("Special Final Focus Experiment")]
    [SerializeField]
    private bool enableShooterBiasedFinalFocus = true;

    [SerializeField, Range(0f, 1f)]
    private float finalShooterFramingWeight = 0.35f;

    [SerializeField, Min(0f)]
    private float finalFocusOrbitRadius = 8.5f;

    public float FinalRollSeparation => Mathf.Max(0f, finalRollSeparation);
    public float FastApproachThreshold => Mathf.Max(
        FinalRollSeparation,
        fastApproachThreshold
    );
    public float FastApproachDuration => Mathf.Max(0f, fastApproachDuration);
    public float FocusTransitionDuration =>
        Mathf.Max(0f, focusTransitionDuration);
    public bool EnableShooterBiasedFinalFocus =>
        enableShooterBiasedFinalFocus;
    public float FinalShooterFramingWeight =>
        Mathf.Clamp01(finalShooterFramingWeight);
    public float FinalFocusOrbitRadius =>
        Mathf.Max(0f, finalFocusOrbitRadius);
}
