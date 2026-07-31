using UnityEngine;

public static class BattleUIExponentialSmoothing
{
    public static float CalculateFactor(
        float sharpness,
        float unscaledDeltaTime
    )
    {
        if (unscaledDeltaTime <= 0f)
        {
            return 0f;
        }

        if (sharpness <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01(
            1f - Mathf.Exp(-sharpness * unscaledDeltaTime)
        );
    }

    public static Vector2 Smooth(
        Vector2 current,
        Vector2 target,
        float sharpness,
        float unscaledDeltaTime
    )
    {
        float factor = CalculateFactor(sharpness, unscaledDeltaTime);
        return Vector2.Lerp(current, target, factor);
    }

    public static Vector3 Smooth(
        Vector3 current,
        Vector3 target,
        float sharpness,
        float unscaledDeltaTime
    )
    {
        float factor = CalculateFactor(sharpness, unscaledDeltaTime);
        return Vector3.Lerp(current, target, factor);
    }

    public static Quaternion Smooth(
        Quaternion current,
        Quaternion target,
        float sharpness,
        float unscaledDeltaTime
    )
    {
        float factor = CalculateFactor(sharpness, unscaledDeltaTime);
        return Quaternion.Slerp(current, target, factor);
    }
}
