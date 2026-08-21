using UnityEngine;

[CreateAssetMenu(
    fileName = "BattleAttackVsDodgePresentationProfile",
    menuName = "Project Guilt/Battle/Attack Vs Dodge Presentation Profile"
)]
public sealed class BattleAttackVsDodgePresentationProfile : ScriptableObject
{
    [SerializeField, Min(0f)] private float sprintDuration = 0.48f;
    [SerializeField, Min(0f)]
    private float afterimageSpawnInterval = 0.08f;

    // Dodge独立Profile只保存共享Approach需要的协调时间。
    public float SprintDuration => Mathf.Max(0f, sprintDuration);
    public float AfterimageSpawnInterval =>
        Mathf.Max(0f, afterimageSpawnInterval);
}
