using UnityEngine;

// Battle Presentation 当前共用的最小缓动函数集合。
public static class BattlePresentationEasing
{
    public static float EaseOutQuad(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t);
    }
}
