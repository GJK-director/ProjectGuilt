using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 战斗场景灰盒 Camera 控制器。
///
/// 当前功能：
/// 1. 鼠标右键左右拖动：Camera 横向移动
/// 2. 鼠标右键上下拖动：先纵向平移，仅向下侧到达极限后再 Tilt
/// 3. 鼠标滚轮：改变圆弧半径，实现 Zoom
///
/// 设计原则：
/// - Floor 完全不动
/// - Character Root 完全不动
/// - Background / Character Visual 由 Billboard 脚本保持正对 Camera
/// </summary>
public class GrayboxBattleCameraController : MonoBehaviour
{
    // =========================================================
    // Pivot
    // =========================================================

    [Header("圆弧中心")]

    [Tooltip("Camera 平移、Orbit 与 Zoom 共用的当前圆心")]
    [SerializeField]
    private Transform orbitPivot;


    // =========================================================
    // 默认镜头
    // =========================================================

    [Header("默认镜头")]

    [Tooltip("Camera 默认位于圆弧上的角度")]
    [SerializeField]
    private float defaultOrbitAngle = 31f;

    [Tooltip(
        "Camera 构图俯仰偏移。\n" +
        "例如 Orbit Angle = 31，Offset = 6，最终 Camera Rotation X = 25。"
    )]
    [SerializeField]
    private float framingPitchOffset = 6f;

    [Tooltip("默认 Camera 到圆弧中心的距离")]
    [SerializeField]
    private float defaultOrbitRadius = 11.66f;


    // =========================================================
    // 上下平移
    // =========================================================

    [Header("上下平移")]

    [Tooltip("Camera 与 Pivot 沿 Camera Up 方向允许平移的最大距离")]
    [SerializeField]
    private float verticalPanLimit = 2f;

    [Tooltip("鼠标上下拖动控制纵向平移的灵敏度")]
    [SerializeField]
    private float verticalPanSensitivity = 0.003f;


    // =========================================================
    // 上下 Tilt
    // =========================================================

    [Header("上下平移极限后的 Tilt")]

    [Tooltip("最低允许的圆弧角度，目前只是灰盒测试值")]
    [SerializeField]
    private float minOrbitAngle = 15f;

    [Tooltip("最高允许的圆弧角度，目前只是灰盒测试值")]
#pragma warning disable 0414 // 单侧 Orbit 暂不使用上限，保留字段以避免丢失 Inspector 数据。
    [SerializeField]
    private float maxOrbitAngle = 40f;
#pragma warning restore 0414

    [Tooltip("纵向平移到达极限后，鼠标继续拖动时的 Tilt 灵敏度")]
    [SerializeField]
    private float orbitSensitivity = 0.05f;

    [Tooltip("第二阶段达到 Tilt 极限时，Camera 沿世界 Y 负方向下降的总距离")]
    [SerializeField]
    private float secondStageVerticalDrop = 0f;


    // =========================================================
    // 左右移动
    // =========================================================

    [Header("左右移动")]

    [Tooltip("Camera 左右最大移动距离")]
    [SerializeField]
    private float horizontalLimit = 1.2f;

    [Tooltip("鼠标左右拖动灵敏度")]
    [SerializeField]
    private float horizontalSensitivity = 0.003f;


    // =========================================================
    // Zoom
    // =========================================================

    [Header("滚轮缩放")]

    [Tooltip("Camera 最近可以靠近 Pivot 的距离")]
    [SerializeField]
    private float minOrbitRadius = 8.5f;

    [Tooltip("Camera 最远可以离开 Pivot 的距离")]
    [SerializeField]
    private float maxOrbitRadius = 14.5f;

    [Tooltip("鼠标滚轮缩放灵敏度")]
    [SerializeField]
    private float zoomSensitivity = 0.005f;


    // =========================================================
    // 输入方向
    // =========================================================

    [Header("拖动方向")]

    [SerializeField]
    private bool invertHorizontal = true;

    [SerializeField]
    private bool invertVertical = false;

    [SerializeField]
    private bool invertZoom = false;


    // =========================================================
    // Runtime
    // =========================================================

    [Header("运行时状态（调试用）")]
    private Vector3 basePivotPosition;
    [SerializeField]
    private float currentX;

    [SerializeField]
    private float currentVerticalPan;

    [SerializeField]
    private float currentOrbitAngle;

    [SerializeField]
    private float currentOrbitRadius;


    // =========================================================
    // Unity
    // =========================================================

    private void Start()
    {
        if (orbitPivot != null)
        {
            // 记录圆心最初的位置
            basePivotPosition = orbitPivot.position;
        }

        ResetToDefault();
    }
    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;


        // -----------------------------------------------------
        // 右键拖动 Camera
        // -----------------------------------------------------

        if (mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();

            UpdateHorizontal(delta.x);
            UpdateVertical(delta.y);
        }


        // -----------------------------------------------------
        // 鼠标滚轮 Zoom
        // 不需要按住右键
        // -----------------------------------------------------

        float scrollY = mouse.scroll.ReadValue().y;

        UpdateZoom(scrollY);


        // -----------------------------------------------------
        // 最终统一应用 Camera Transform
        // -----------------------------------------------------

        ApplyCameraTransform();
    }


    // =========================================================
    // Horizontal
    // =========================================================

    /// <summary>
    /// Camera 左右平移。
    ///
    /// 这里只改变 X，
    /// 不产生 Yaw，
    /// 所以 Camera 始终保持正面观看战斗舞台。
    /// </summary>
    private void UpdateHorizontal(float mouseDeltaX)
    {
        float direction = invertHorizontal ? -1f : 1f;

        currentX +=
            mouseDeltaX
            * horizontalSensitivity
            * direction;

        currentX = Mathf.Clamp(
            currentX,
            -horizontalLimit,
            horizontalLimit
        );
    }


    // =========================================================
    // Vertical Pan
    // =========================================================

    /// <summary>
    /// 先应用 invertVertical，再由最终逻辑方向决定 Pan-only 或 Pan→Orbit。
    /// 从 Tilt 反向时必须先恢复默认角度，之后才能离开 Pan 边界。
    /// </summary>
    private void UpdateVertical(float mouseDeltaY)
    {
        float inputDirection = invertVertical ? -1f : 1f;
        float logicalVerticalDelta = mouseDeltaY * inputDirection;
        if (Mathf.Abs(logicalVerticalDelta) <= Mathf.Epsilon)
            return;

        bool isPanOnlyDirection = logicalVerticalDelta > 0f;
        float remainingMouseDelta = Mathf.Abs(logicalVerticalDelta);
        float safeLimit = Mathf.Abs(verticalPanLimit);

        if (isPanOnlyDirection)
        {
            // 逻辑正方向只允许 Pan；若已有 Tilt，必须先原路恢复。
            if (currentOrbitAngle < defaultOrbitAngle)
            {
                remainingMouseDelta = ConsumeOrbitInputToward(
                    defaultOrbitAngle,
                    remainingMouseDelta
                );
            }

            if (remainingMouseDelta > Mathf.Epsilon)
            {
                ConsumePanInputToward(safeLimit, remainingMouseDelta);
            }

            // Pan-only 一侧抵达极限后的剩余输入直接丢弃。
            return;
        }

        // 逻辑负方向先走完整个 Pure Pan 区域。
        remainingMouseDelta = ConsumePanInputToward(
            -safeLimit,
            remainingMouseDelta
        );

        if (remainingMouseDelta <= Mathf.Epsilon)
            return;

        // Pan→Tilt 一侧到达极限后，才允许继续降低俯视角度。
        ConsumeOrbitInputToward(minOrbitAngle, remainingMouseDelta);
    }

    private float ConsumePanInputToward(
        float target,
        float availableMouseDelta
    )
    {
        float distance = Mathf.Abs(target - currentVerticalPan);
        if (distance <= Mathf.Epsilon)
            return availableMouseDelta;

        float sensitivity = Mathf.Abs(verticalPanSensitivity);
        if (sensitivity <= Mathf.Epsilon)
            return 0f;

        float movement = Mathf.Min(
            distance,
            availableMouseDelta * sensitivity
        );
        currentVerticalPan = Mathf.MoveTowards(
            currentVerticalPan,
            target,
            movement
        );

        return Mathf.Max(
            0f,
            availableMouseDelta - movement / sensitivity
        );
    }

    private float ConsumeOrbitInputToward(
        float target,
        float availableMouseDelta
    )
    {
        float distance = Mathf.Abs(target - currentOrbitAngle);
        if (distance <= Mathf.Epsilon)
            return availableMouseDelta;

        float sensitivity = Mathf.Abs(orbitSensitivity);
        if (sensitivity <= Mathf.Epsilon)
            return 0f;

        float movement = Mathf.Min(
            distance,
            availableMouseDelta * sensitivity
        );
        currentOrbitAngle = Mathf.MoveTowards(
            currentOrbitAngle,
            target,
            movement
        );

        return Mathf.Max(
            0f,
            availableMouseDelta - movement / sensitivity
        );
    }


    // =========================================================
    // Zoom
    // =========================================================

    /// <summary>
    /// 鼠标滚轮改变 Camera 与 Pivot 的距离。
    ///
    /// 滚轮向上：
    /// Radius 变小
    /// Camera 靠近
    /// 画面放大
    ///
    /// 滚轮向下：
    /// Radius 变大
    /// Camera 远离
    /// 画面缩小
    /// </summary>
    private void UpdateZoom(float scrollY)
    {
        if (Mathf.Abs(scrollY) < 0.01f)
            return;

        float direction = invertZoom ? -1f : 1f;

        currentOrbitRadius -=
            scrollY
            * zoomSensitivity
            * direction;

        currentOrbitRadius = Mathf.Clamp(
            currentOrbitRadius,
            minOrbitRadius,
            maxOrbitRadius
        );
    }


    // =========================================================
    // Apply Camera
    // =========================================================

    /// <summary>
    /// 根据：
    ///
    /// Pivot
    /// +
    /// Horizontal / Vertical Pan Offset
    /// +
    /// Position Angle / Tilt Angle
    /// +
    /// Orbit Radius
    ///
    /// 计算 Camera 最终位置与旋转。
    /// </summary>
    private void ApplyCameraTransform()
    {
        if (orbitPivot == null)
            return;


        // -----------------------------------------------------
        // Camera 的圆弧位置始终使用默认角度。
        // 第二阶段只改变 Pitch，不再沿圆弧改变 Camera Position。
        // -----------------------------------------------------

        float angleRad =
            defaultOrbitAngle * Mathf.Deg2Rad;


        // -----------------------------------------------------
        // Camera Rotation
        //
        // currentOrbitAngle 暂时沿用原字段名，但只负责 Tilt / Pitch。
        // -----------------------------------------------------

        float cameraPitch =
            currentOrbitAngle - framingPitchOffset;

        Quaternion cameraRotation = Quaternion.Euler(
            cameraPitch,
            0f,
            0f
        );


        // -----------------------------------------------------
        // 平移当前圆心
        //
        // 横向沿世界 X；纵向沿默认构图时的 Camera Up。
        // Pan 方向固定后，Tilt 阶段 Pivot 会停在已经达到的边界位置。
        // -----------------------------------------------------

        Quaternion defaultCameraRotation = Quaternion.Euler(
            defaultOrbitAngle - framingPitchOffset,
            0f,
            0f
        );
        Vector3 verticalDirection =
            defaultCameraRotation * Vector3.up;
        Vector3 verticalOffset =
            verticalDirection * currentVerticalPan;

        orbitPivot.position =
            basePivotPosition
            + Vector3.right * currentX
            + verticalOffset;

        // 当前圆弧中心就是实际 Pivot 的位置
        Vector3 currentOrbitCenter =
            orbitPivot.position;
        // -----------------------------------------------------
        // 根据圆周运动计算 Camera Y / Z
        //
        // Y = sin(angle) * radius
        // Z = -cos(angle) * radius
        //
        // 因此 Camera 始终位于 Pivot 的后上方。
        // -----------------------------------------------------

        Vector3 orbitOffset = new Vector3(
            0f,
            Mathf.Sin(angleRad) * currentOrbitRadius,
            -Mathf.Cos(angleRad) * currentOrbitRadius
        );


        // -----------------------------------------------------
        // 第二阶段 Camera Down
        //
        // Tilt 只改变旋转；Camera 额外沿世界 Y 负方向下降。
        // 不移动 Pivot，也不改变由 Zoom 决定的圆弧 Z 与半径。
        // -----------------------------------------------------

        float tiltRange =
            defaultOrbitAngle - minOrbitAngle;
        float tiltProgress =
            Mathf.Abs(tiltRange) > Mathf.Epsilon
                ? Mathf.Clamp01(
                    (defaultOrbitAngle - currentOrbitAngle)
                    / tiltRange
                )
                : 0f;
        float cameraDrop =
            secondStageVerticalDrop * tiltProgress;
        Vector3 secondStageDropOffset =
            Vector3.down * cameraDrop;


        // -----------------------------------------------------
        // Camera 世界坐标
        // -----------------------------------------------------

        transform.position =
            currentOrbitCenter
            + orbitOffset
            + secondStageDropOffset;

        // -----------------------------------------------------
        // 应用前面已用于纵向 Pan 计算的同一旋转。
        // -----------------------------------------------------

        transform.rotation = cameraRotation;
    }


    // =========================================================
    // Reset
    // =========================================================

    /// <summary>
    /// 恢复 1v1 默认镜头。
    ///
    /// 可以在组件菜单中右键调用：
    /// Reset To Default
    /// </summary>
    [ContextMenu("Reset To Default")]
    public void ResetToDefault()
    {
        currentX = 0f;
        currentVerticalPan = 0f;

        currentOrbitAngle =
            defaultOrbitAngle;

        currentOrbitRadius =
            defaultOrbitRadius;

        ApplyCameraTransform();
    }
}
