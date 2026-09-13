using UnityEngine;

public sealed class BattleAutoClashController : MonoBehaviour
{
    [SerializeField] private bool enableAutoClash = false;
    [SerializeField, Min(0f)] private float autoClashDelay = 0.1f;

    public bool EnableAutoClash
    {
        get { return enableAutoClash; }
    }

    public float AutoClashDelay
    {
        get { return Mathf.Max(0f, autoClashDelay); }
    }
}
