using UnityEngine;

/// <summary>
/// 灰盒阶段使用：
/// 让二维物体始终与目标摄像机保持相同朝向。
/// 只修改 Rotation，不修改 Position。
/// </summary>
[DefaultExecutionOrder(100)]
public class GrayboxBillboard : MonoBehaviour
{
    [Header("目标摄像机")]
    [SerializeField] private Camera targetCamera;

    [Header("额外旋转修正")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    private void LateUpdate()
    {
        // 本帧先完成角色视觉朝向，随后World-Follow UI再投影最终状态。
        // 如果 Inspector 没有手动指定，就尝试寻找 Main Camera
        if (targetCamera == null)
        {
            targetCamera = Camera.main;

            if (targetCamera == null)
                return;
        }

        // 保持与摄像机平行
        // rotationOffset 用于以后某些图片正反面需要额外翻转时修正
        transform.rotation =
            targetCamera.transform.rotation *
            Quaternion.Euler(rotationOffset);
    }
}
