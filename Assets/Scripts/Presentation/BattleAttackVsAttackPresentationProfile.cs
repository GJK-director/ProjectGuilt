using UnityEngine;

[CreateAssetMenu(
    fileName = "BattleAttackVsAttackPresentationProfile",
    menuName = "Project Guilt/Battle/Attack Vs Attack Presentation Profile"
)]
public sealed class BattleAttackVsAttackPresentationProfile : ScriptableObject
{
    [SerializeField, Min(0f)] private float sprintDuration = 0.48f;
    [SerializeField, Min(0f)]
    private float afterimageSpawnInterval = 0.08f;
    [SerializeField, Min(0f)] private float slashHoldDuration = 0.5f;
    [SerializeField, Min(0f)] private float hitStopDuration = 0.08f;

    [Header("Attack Tie")]
    [SerializeField, Min(0f)] private float tieRecoilDistance = 1.2f;
    [SerializeField, Min(0f)] private float tieRecoilDuration = 0.05f;

    // Profile 是 AttackVsAttack 公共演出的唯一时间参数源。
    public float SprintDuration => Mathf.Max(0f, sprintDuration);
    public float AfterimageSpawnInterval =>
        Mathf.Max(0f, afterimageSpawnInterval);
    public float SlashHoldDuration => Mathf.Max(0f, slashHoldDuration);
    public float HitStopDuration => Mathf.Max(0f, hitStopDuration);
    public float TieRecoilDistance => Mathf.Max(0f, tieRecoilDistance);
    public float TieRecoilDuration => Mathf.Max(0f, tieRecoilDuration);
}
