using UnityEngine;

// 标记明确的交互区域，并声明它是否优先占用 Camera 输入。
public sealed class CameraInputBlocker : MonoBehaviour
{
    [SerializeField]
    private bool blockCameraDrag = true;

    [SerializeField]
    private bool blockCameraZoom = true;

    public bool BlockCameraDrag => blockCameraDrag;
    public bool BlockCameraZoom => blockCameraZoom;
}
