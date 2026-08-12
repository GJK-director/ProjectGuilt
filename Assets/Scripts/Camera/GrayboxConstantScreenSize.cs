using UnityEngine;

/// <summary>
/// 灰盒测试：
/// 在 Perspective Camera 下，根据物体与摄像机的“视线深度”
/// 自动补偿 Scale，使角色在屏幕上的视觉大小尽量保持不变。
///
/// 这个脚本只修改 Scale，不修改 Position 和 Rotation。
/// </summary>
public class GrayboxConstantScreenSize : MonoBehaviour
{
    [Header("目标摄像机")]
    [SerializeField] private Camera targetCamera;

    [Header("调试")]
    [SerializeField] private float referenceDepth;
    [SerializeField] private float currentDepth;
    [SerializeField] private float currentScaleRatio = 1f;

    // 角色默认视觉尺寸
    private Vector3 baseScale;

    // 是否已经记录默认距离
    private bool initialized = false;

    private void Awake()
    {
        // 记录这个 Visual Root 原本的尺寸
        baseScale = transform.localScale;
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;

            if (targetCamera == null)
                return;
        }

        // 计算角色站位在 Camera 前方的“深度”
        Vector3 cameraToObject =
            transform.position - targetCamera.transform.position;

        currentDepth = Vector3.Dot(
            cameraToObject,
            targetCamera.transform.forward
        );

        // 防止物体跑到 Camera 后方时出现异常
        if (currentDepth <= 0.001f)
            return;

        // 第一次运行时，把当前默认镜头距离记录成基准
        if (!initialized)
        {
            referenceDepth = currentDepth;
            initialized = true;
        }

        // Perspective 下：
        // 距离越远 → 自动放大
        // 距离越近 → 自动缩小
        currentScaleRatio = currentDepth / referenceDepth;

        transform.localScale =
            baseScale * currentScaleRatio;
    }
}