using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// 战斗场景灰盒 Camera 控制器。
///
/// 当前功能：
/// 1. 鼠标左键左右拖动：Camera 横向移动，松开后保留短暂惯性
/// 2. 鼠标左键上下拖动：先纵向平移，背景侧到达极限后再 Orbit + Pitch
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

    [Tooltip("从默认位置朝显示更多 Background 的方向允许平移的最大距离")]
    [FormerlySerializedAs("verticalPanLimit")]
    [SerializeField, Min(0f)]
    private float backgroundPanLimit = 1.2f;

    [Tooltip("从默认位置朝显示更多 Floor 的方向允许平移的最大距离")]
    [SerializeField, Min(0f)]
    private float floorPanLimit = 1.2f;

    [Tooltip("鼠标上下拖动控制纵向平移的灵敏度")]
    [SerializeField]
    private float verticalPanSensitivity = 0.003f;

    [Tooltip("从默认位置到 Background Threshold 期间逐渐增加的 Camera X 轴 Pitch")]
    [SerializeField]
    private float backgroundPanPitchOffset = 0f;

    [Tooltip("从默认位置到 Floor Pan Limit 期间逐渐增加的 Camera X 轴 Pitch")]
    [SerializeField]
    private float floorPanPitchOffset = 0f;


    // =========================================================
    // 第二阶段 Orbit + Pitch
    // =========================================================

    [Header("背景侧第二阶段")]

    [Tooltip("Camera 公转的最终圆弧角度")]
    [SerializeField]
    private float minOrbitAngle = 15f;

    [Tooltip("最高允许的圆弧角度，单侧 Orbit 暂不使用")]
#pragma warning disable 0414 // 保留字段以避免丢失 Inspector 数据。
    [SerializeField]
    private float maxOrbitAngle = 40f;
#pragma warning restore 0414

    [Tooltip("第二阶段统一 Progress 的输入灵敏度，以公转角范围换算")]
    [SerializeField]
    private float orbitSensitivity = 0.05f;

    [Tooltip("Camera 自转的最终 X 轴 Pitch 角度")]
    [SerializeField]
    private float finalCameraPitch = 13.5f;

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

    [Tooltip("松开鼠标时保留的水平拖拽速度比例")]
    [SerializeField, Range(0f, 1f)]
    private float horizontalInertia = 0.35f;

    [Tooltip("水平惯性每秒衰减强度，数值越大停止越快")]
    [SerializeField, Min(0f)]
    private float horizontalInertiaDamping = 6f;

    [Tooltip("从单击判定为 Camera 拖拽所需的最小鼠标位移（像素）")]
    [FormerlySerializedAs("horizontalDragThreshold")]
    [SerializeField, Min(0f)]
    private float cameraDragThreshold = 0.5f;

    [Tooltip("Camera 拖拽输入追上鼠标目标所需的时间；数值越大，移动越有重量感")]
    [SerializeField, Min(0f)]
    private float cameraDragFollowTime = 0.18f;


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

    [Tooltip("Camera 当前缩放半径追上滚轮目标半径所需的时间")]
    [SerializeField, Min(0f)]
    private float zoomFollowTime = 0.15f;


    // =========================================================
    // Idle Horizontal Sway
    // =========================================================

    [Header("水平待机呼吸")]

    [Tooltip("是否启用实验性的 Camera 水平待机呼吸")]
    [SerializeField]
    private bool enableIdleHorizontalSway = false;

    [Tooltip("待机呼吸相对玩家水平位置的最大偏移")]
    [SerializeField, Min(0f)]
    private float idleSwayAmplitude = 0.03f;

    [Tooltip("待机呼吸完成一整轮左右往复所需的秒数")]
    [SerializeField, Min(0f)]
    private float idleSwayCycleDuration = 8f;

    [Tooltip("玩家操作与待机呼吸之间淡入淡出所需的时间")]
    [SerializeField, Min(0f)]
    private float idleSwayBlendTime = 0.25f;


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
    private float currentPitchAngle;

    [SerializeField]
    private float currentOrbitRadius;

    [SerializeField]
    private float targetOrbitRadius;

    [SerializeField]
    private float idleSwayOffset;

    [SerializeField, Range(0f, 1f)]
    private float idleSwayWeight;

    [SerializeField, Range(0f, 1f)]
    private float secondStageProgress;

    [SerializeField]
    private float secondStageStartOrbitAngle;

    [SerializeField]
    private float secondStageOrbitRadius;

    [SerializeField]
    private Vector3 orbitCenter;

    [SerializeField]
    private float distanceToOrbitCenter;

    [SerializeField]
    private bool isInSecondStage;

    [FormerlySerializedAs("isHorizontalDragging")]
    [SerializeField]
    private bool isCameraDragging;

    [SerializeField]
    private float horizontalVelocity;

    private Vector2 cameraDragStartPosition;
    private Vector2 rawDragPosition;
    private Vector2 smoothedDragPosition;
    private Vector2 dragFollowVelocity;
    private bool isCameraDragFollowing;
    private float pendingHorizontalInertiaVelocity;
    private float zoomFollowVelocity;
    private float idleSwayElapsed;
    private float idleSwayBlendVelocity;

    private const float HorizontalVelocityStopThreshold = 0.001f;
    private const float DragFollowStopThreshold = 0.01f;
    private const float ZoomFollowStopThreshold = 0.0001f;
    private const float IdleSwayWeightStopThreshold = 0.0001f;


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

    private void OnDisable()
    {
        ClearCameraDragState();
        ClearIdleHorizontalSway();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;


        Vector2 delta = mouse.delta.ReadValue();


        // -----------------------------------------------------
        // 左键统一控制水平与纵向 Camera 拖拽。
        // -----------------------------------------------------

        UpdateCameraDragInput(mouse, delta);


        // -----------------------------------------------------
        // 鼠标滚轮 Zoom
        // 不需要按住右键
        // -----------------------------------------------------

        float scrollY = mouse.scroll.ReadValue().y;

        UpdateZoom(scrollY);

        UpdateIdleHorizontalSway();


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
    private void UpdateCameraDragInput(
        Mouse mouse,
        Vector2 mouseDelta
    )
    {
        if (mouse.leftButton.wasPressedThisFrame)
        {
            BeginCameraDrag(mouse.position.ReadValue());
        }

        if (mouse.leftButton.isPressed)
        {
            if (!isCameraDragging)
            {
                Vector2 dragDelta =
                    mouse.position.ReadValue()
                    - cameraDragStartPosition;
                isCameraDragging = dragDelta.magnitude >= Mathf.Max(
                    0f,
                    cameraDragThreshold
                );

                if (isCameraDragging)
                    isCameraDragFollowing = true;
            }

            if (isCameraDragging)
            {
                rawDragPosition += mouseDelta;
                UpdateCameraDragFollow();
            }

            return;
        }

        if (isCameraDragging)
        {
            pendingHorizontalInertiaVelocity =
                horizontalVelocity;
            isCameraDragging = false;
        }

        if (isCameraDragFollowing)
        {
            if (UpdateCameraDragFollow())
                CompleteCameraDragFollow();

            return;
        }

        UpdateHorizontalInertia();
    }

    private void BeginCameraDrag(Vector2 mousePosition)
    {
        isCameraDragging = false;
        isCameraDragFollowing = false;
        horizontalVelocity = 0f;
        pendingHorizontalInertiaVelocity = 0f;
        cameraDragStartPosition = mousePosition;
        rawDragPosition = Vector2.zero;
        smoothedDragPosition = Vector2.zero;
        dragFollowVelocity = Vector2.zero;
    }

    private bool UpdateCameraDragFollow()
    {
        float deltaTime = Time.unscaledDeltaTime;
        Vector2 nextSmoothedPosition;
        if (cameraDragFollowTime <= Mathf.Epsilon ||
            deltaTime <= Mathf.Epsilon)
        {
            nextSmoothedPosition = rawDragPosition;
            dragFollowVelocity = Vector2.zero;
        }
        else
        {
            nextSmoothedPosition = Vector2.SmoothDamp(
                smoothedDragPosition,
                rawDragPosition,
                ref dragFollowVelocity,
                cameraDragFollowTime,
                Mathf.Infinity,
                deltaTime
            );
        }

        bool hasCaughtUp =
            (rawDragPosition - nextSmoothedPosition).sqrMagnitude
                <= DragFollowStopThreshold
                * DragFollowStopThreshold &&
            dragFollowVelocity.sqrMagnitude
                <= DragFollowStopThreshold
                * DragFollowStopThreshold;
        if (hasCaughtUp)
        {
            nextSmoothedPosition = rawDragPosition;
            dragFollowVelocity = Vector2.zero;
        }

        Vector2 processedDelta =
            nextSmoothedPosition - smoothedDragPosition;
        smoothedDragPosition = nextSmoothedPosition;

        // X/Y 共用同一个 Follow 结果，再分别进入既有 Camera 逻辑。
        UpdateHorizontalDrag(processedDelta.x);
        UpdateVertical(processedDelta.y);

        return hasCaughtUp;
    }

    private void CompleteCameraDragFollow()
    {
        isCameraDragFollowing = false;
        rawDragPosition = Vector2.zero;
        smoothedDragPosition = Vector2.zero;
        dragFollowVelocity = Vector2.zero;

        horizontalVelocity =
            pendingHorizontalInertiaVelocity
            * Mathf.Clamp01(horizontalInertia);
        pendingHorizontalInertiaVelocity = 0f;
    }

    private void ClearCameraDragState()
    {
        isCameraDragging = false;
        isCameraDragFollowing = false;
        horizontalVelocity = 0f;
        pendingHorizontalInertiaVelocity = 0f;
        cameraDragStartPosition = Vector2.zero;
        rawDragPosition = Vector2.zero;
        smoothedDragPosition = Vector2.zero;
        dragFollowVelocity = Vector2.zero;
    }

    private void UpdateHorizontalDrag(float mouseDeltaX)
    {
        float direction = invertHorizontal ? -1f : 1f;
        float previousX = currentX;
        float requestedX = currentX
            + mouseDeltaX
            * horizontalSensitivity
            * direction;
        float safeLimit = Mathf.Abs(horizontalLimit);

        currentX = Mathf.Clamp(
            requestedX,
            -safeLimit,
            safeLimit
        );

        if (!Mathf.Approximately(currentX, requestedX))
        {
            horizontalVelocity = 0f;
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        float actualMovement = currentX - previousX;
        if (Mathf.Abs(actualMovement) > Mathf.Epsilon &&
            deltaTime > Mathf.Epsilon)
        {
            horizontalVelocity = actualMovement / deltaTime;
            return;
        }

        DampHorizontalVelocity(deltaTime);
    }

    private void UpdateHorizontalInertia()
    {
        if (Mathf.Abs(horizontalVelocity)
            <= HorizontalVelocityStopThreshold)
        {
            horizontalVelocity = 0f;
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        if (deltaTime <= Mathf.Epsilon)
            return;

        float safeLimit = Mathf.Abs(horizontalLimit);
        float requestedX = currentX
            + horizontalVelocity * deltaTime;
        currentX = Mathf.Clamp(
            requestedX,
            -safeLimit,
            safeLimit
        );

        // 硬边界不保留朝外速度，避免越界、回弹或持续积累。
        if (!Mathf.Approximately(currentX, requestedX))
        {
            horizontalVelocity = 0f;
            return;
        }

        DampHorizontalVelocity(deltaTime);
    }

    private void DampHorizontalVelocity(float deltaTime)
    {
        float damping = Mathf.Max(0f, horizontalInertiaDamping);
        if (damping <= Mathf.Epsilon)
        {
            horizontalVelocity = 0f;
            return;
        }

        horizontalVelocity *= Mathf.Exp(-damping * deltaTime);
        if (Mathf.Abs(horizontalVelocity)
            <= HorizontalVelocityStopThreshold)
        {
            horizontalVelocity = 0f;
        }
    }


    // =========================================================
    // Vertical Pan
    // =========================================================

    /// <summary>
    /// 先应用 invertVertical，再由最终逻辑方向决定 Floor Pan 或 Background Pan→Orbit/Pitch。
    /// 反向时必须先同步恢复公转和 Pitch，之后才能离开 Pan 边界。
    /// </summary>
    private void UpdateVertical(float mouseDeltaY)
    {
        float inputDirection = invertVertical ? -1f : 1f;
        float logicalVerticalDelta = mouseDeltaY * inputDirection;
        if (Mathf.Abs(logicalVerticalDelta) <= Mathf.Epsilon)
            return;

        bool isFloorViewDirection = logicalVerticalDelta < 0f;
        float remainingMouseDelta = Mathf.Abs(logicalVerticalDelta);
        float safeBackgroundLimit = Mathf.Max(
            0f,
            backgroundPanLimit
        );
        float safeFloorLimit = Mathf.Max(0f, floorPanLimit);

        currentVerticalPan = Mathf.Clamp(
            currentVerticalPan,
            -safeFloorLimit,
            safeBackgroundLimit
        );

        // Threshold 以下绝对属于 Pure Pan。
        // 即使外部状态异常残留，也不允许它抢占纵向输入。
        if (currentVerticalPan < safeBackgroundLimit &&
            isInSecondStage)
        {
            ClearSecondStageForPurePan();
        }

        if (isFloorViewDirection)
        {
            // Floor 方向只允许 Pan；若已进入第二阶段，必须先恢复统一 Progress。
            if (isInSecondStage)
            {
                remainingMouseDelta = ConsumeSecondStageProgressToward(
                    0f,
                    remainingMouseDelta
                );

                if (secondStageProgress <= Mathf.Epsilon)
                    ClearSecondStageForPurePan();
            }

            if (remainingMouseDelta > Mathf.Epsilon)
            {
                ConsumePanInputToward(
                    -safeFloorLimit,
                    remainingMouseDelta
                );
            }

            // Floor 一侧抵达极限后的剩余输入直接丢弃。
            return;
        }

        // Threshold 前 Pure Pan 拥有绝对优先级。
        if (currentVerticalPan < safeBackgroundLimit)
        {
            remainingMouseDelta = ConsumePanInputToward(
                safeBackgroundLimit,
                remainingMouseDelta
            );

            if (currentVerticalPan < safeBackgroundLimit)
                return;
        }

        if (remainingMouseDelta <= Mathf.Epsilon)
            return;

        if (!EnsureSecondStageStarted(safeBackgroundLimit))
            return;

        // 到达 Background 侧阈值后，输入只推进唯一 Progress。
        ConsumeSecondStageProgressToward(
            1f,
            remainingMouseDelta
        );
    }

    private float ConsumePanInputToward(
        float target,
        float availableMouseDelta
    )
    {
        float distance = Mathf.Abs(target - currentVerticalPan);
        if (distance <= Mathf.Epsilon)
        {
            currentVerticalPan = target;
            return availableMouseDelta;
        }

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

    private float ConsumeSecondStageProgressToward(
        float targetProgress,
        float availableMouseDelta
    )
    {
        targetProgress = Mathf.Clamp01(targetProgress);
        float distance = Mathf.Abs(
            targetProgress - secondStageProgress
        );
        if (distance <= Mathf.Epsilon)
        {
            secondStageProgress = targetProgress;
            UpdateSecondStageAnglesFromProgress();
            return availableMouseDelta;
        }

        float progressSensitivity =
            GetSecondStageProgressSensitivity();
        if (progressSensitivity <= Mathf.Epsilon)
            return 0f;

        float movement = Mathf.Min(
            distance,
            availableMouseDelta * progressSensitivity
        );
        secondStageProgress = Mathf.MoveTowards(
            secondStageProgress,
            targetProgress,
            movement
        );
        UpdateSecondStageAnglesFromProgress();

        return Mathf.Abs(secondStageProgress - targetProgress)
                <= Mathf.Epsilon
            ? Mathf.Max(
                0f,
                availableMouseDelta
                - movement / progressSensitivity
            )
            : 0f;
    }

    private bool EnsureSecondStageStarted(float backgroundThreshold)
    {
        if (currentVerticalPan < backgroundThreshold ||
            orbitPivot == null)
        {
            return false;
        }

        if (isInSecondStage)
            return true;

        // 先把 Camera 精确放到 Pure Pan 的 Threshold Pose，
        // 再相对真实 Pivot 反算公转起点，避免切换跳变。
        ApplyCameraTransform();

        Vector3 startOffset =
            transform.position - orbitPivot.position;
        secondStageOrbitRadius = startOffset.magnitude;
        secondStageStartOrbitAngle = Mathf.Atan2(
            startOffset.y,
            -startOffset.z
        ) * Mathf.Rad2Deg;

        secondStageProgress = 0f;
        UpdateSecondStageAnglesFromProgress();
        isInSecondStage = true;
        return true;
    }

    private void ClearSecondStageForPurePan()
    {
        isInSecondStage = false;
        secondStageProgress = 0f;
        UpdateSecondStageAnglesFromProgress();
    }

    private float GetSecondStageProgressSensitivity()
    {
        float referenceAngleRange = Mathf.Abs(
            minOrbitAngle - secondStageStartOrbitAngle
        );
        if (referenceAngleRange <= Mathf.Epsilon)
        {
            referenceAngleRange = Mathf.Abs(
                finalCameraPitch - GetBackgroundThresholdPitch()
            );
        }

        return referenceAngleRange > Mathf.Epsilon
            ? Mathf.Abs(orbitSensitivity) / referenceAngleRange
            : 0f;
    }

    private void UpdateSecondStageAnglesFromProgress()
    {
        secondStageProgress = Mathf.Clamp01(
            secondStageProgress
        );
        currentOrbitAngle = Mathf.Lerp(
            secondStageStartOrbitAngle,
            minOrbitAngle,
            secondStageProgress
        );
        currentPitchAngle = Mathf.Lerp(
            GetBackgroundThresholdPitch(),
            finalCameraPitch,
            secondStageProgress
        );
    }

    private float GetDefaultCameraPitch()
    {
        return defaultOrbitAngle - framingPitchOffset;
    }

    private float GetBackgroundThresholdPitch()
    {
        return GetDefaultCameraPitch() + backgroundPanPitchOffset;
    }

    private float GetFloorLimitPitch()
    {
        return GetDefaultCameraPitch() + floorPanPitchOffset;
    }

    private float GetFloorPanProgress()
    {
        float safeFloorLimit = Mathf.Max(0f, floorPanLimit);
        if (safeFloorLimit <= Mathf.Epsilon)
            return 1f;

        return Mathf.Clamp01(
            (currentVerticalPan + safeFloorLimit)
            / safeFloorLimit
        );
    }

    private float GetBackgroundPanProgress()
    {
        float safeBackgroundLimit = Mathf.Max(
            0f,
            backgroundPanLimit
        );
        if (safeBackgroundLimit <= Mathf.Epsilon)
            return 1f;

        return Mathf.Clamp01(
            currentVerticalPan / safeBackgroundLimit
        );
    }

    private float GetPurePanCameraPitch()
    {
        if (currentVerticalPan < 0f)
        {
            return Mathf.Lerp(
                GetFloorLimitPitch(),
                GetDefaultCameraPitch(),
                GetFloorPanProgress()
            );
        }

        return Mathf.Lerp(
            GetDefaultCameraPitch(),
            GetBackgroundThresholdPitch(),
            GetBackgroundPanProgress()
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
        if (Mathf.Abs(scrollY) >= 0.01f)
        {
            float direction = invertZoom ? -1f : 1f;
            targetOrbitRadius -=
                scrollY
                * zoomSensitivity
                * direction;

            // Target 始终处于硬边界内，不积累隐藏的越界输入。
            targetOrbitRadius = Mathf.Clamp(
                targetOrbitRadius,
                minOrbitRadius,
                maxOrbitRadius
            );
        }

        UpdateZoomFollow();
    }

    private void UpdateZoomFollow()
    {
        float previousOrbitRadius = currentOrbitRadius;
        float deltaTime = Time.unscaledDeltaTime;
        if (zoomFollowTime <= Mathf.Epsilon)
        {
            currentOrbitRadius = targetOrbitRadius;
            zoomFollowVelocity = 0f;
        }
        else if (deltaTime > Mathf.Epsilon)
        {
            currentOrbitRadius = Mathf.SmoothDamp(
                currentOrbitRadius,
                targetOrbitRadius,
                ref zoomFollowVelocity,
                zoomFollowTime,
                Mathf.Infinity,
                deltaTime
            );
        }

        currentOrbitRadius = Mathf.Clamp(
            currentOrbitRadius,
            minOrbitRadius,
            maxOrbitRadius
        );

        if (Mathf.Abs(currentOrbitRadius - targetOrbitRadius)
            <= ZoomFollowStopThreshold)
        {
            currentOrbitRadius = targetOrbitRadius;
            zoomFollowVelocity = 0f;
        }

        float actualRadiusDelta =
            currentOrbitRadius - previousOrbitRadius;

        if (isInSecondStage)
        {
            secondStageOrbitRadius = Mathf.Max(
                Mathf.Epsilon,
                secondStageOrbitRadius
                + actualRadiusDelta
            );
        }
    }


    // =========================================================
    // Idle Horizontal Sway
    // =========================================================

    private void UpdateIdleHorizontalSway()
    {
        float deltaTime = Time.unscaledDeltaTime;
        if (enableIdleHorizontalSway &&
            idleSwayCycleDuration > Mathf.Epsilon &&
            deltaTime > Mathf.Epsilon)
        {
            idleSwayElapsed = Mathf.Repeat(
                idleSwayElapsed + deltaTime,
                idleSwayCycleDuration
            );
        }

        bool hasActiveHorizontalInput =
            isCameraDragging ||
            isCameraDragFollowing ||
            Mathf.Abs(horizontalVelocity)
                > HorizontalVelocityStopThreshold;
        float targetWeight =
            enableIdleHorizontalSway &&
            !hasActiveHorizontalInput
                ? 1f
                : 0f;

        if (idleSwayBlendTime <= Mathf.Epsilon)
        {
            idleSwayWeight = targetWeight;
            idleSwayBlendVelocity = 0f;
        }
        else if (deltaTime > Mathf.Epsilon)
        {
            idleSwayWeight = Mathf.SmoothDamp(
                idleSwayWeight,
                targetWeight,
                ref idleSwayBlendVelocity,
                idleSwayBlendTime,
                Mathf.Infinity,
                deltaTime
            );
        }

        idleSwayWeight = Mathf.Clamp01(idleSwayWeight);
        if (Mathf.Abs(idleSwayWeight - targetWeight)
            <= IdleSwayWeightStopThreshold)
        {
            idleSwayWeight = targetWeight;
            idleSwayBlendVelocity = 0f;
        }

        if (!enableIdleHorizontalSway &&
            idleSwayWeight <= IdleSwayWeightStopThreshold)
        {
            idleSwayOffset = 0f;
            return;
        }

        float phase = idleSwayCycleDuration > Mathf.Epsilon
            ? idleSwayElapsed
                / idleSwayCycleDuration
                * Mathf.PI
                * 2f
            : 0f;
        idleSwayOffset =
            Mathf.Sin(phase)
            * idleSwayAmplitude
            * idleSwayWeight;
    }

    private void ClearIdleHorizontalSway()
    {
        idleSwayOffset = 0f;
        idleSwayWeight = 0f;
        idleSwayElapsed = 0f;
        idleSwayBlendVelocity = 0f;
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
    /// Orbit Position Angle / Camera Pitch Angle
    /// +
    /// Orbit Radius
    ///
    /// 计算 Camera 最终位置与旋转。
    /// </summary>
    private void ApplyCameraTransform()
    {
        if (orbitPivot == null)
            return;

        if (isInSecondStage)
            UpdateSecondStageAnglesFromProgress();
        else
            currentPitchAngle = GetPurePanCameraPitch();


        // -----------------------------------------------------
        // 公转角度只负责 Camera 在圆上的位置。
        // 第二阶段以外一直使用默认公转角度。
        // -----------------------------------------------------

        float positionAngle = isInSecondStage
            ? currentOrbitAngle
            : defaultOrbitAngle;
        float angleRad = positionAngle * Mathf.Deg2Rad;


        // -----------------------------------------------------
        // Camera Rotation
        //
        // Camera 自转只使用独立 Pitch，不参与公转位置计算。
        // -----------------------------------------------------

        Quaternion cameraRotation = Quaternion.Euler(
            currentPitchAngle,
            0f,
            0f
        );


        // -----------------------------------------------------
        // 计算独立 Camera Vertical Pan Offset
        //
        // Threshold 前的 Pure Pan 只沿世界 Y 轴运动，
        // 不因 Camera 自身 Pitch 额外改变 Z。
        // -----------------------------------------------------

        Vector3 verticalOffset =
            Vector3.up * currentVerticalPan;

        // Horizontal 继续让 Camera 与 Pivot 一起沿世界 X 平移；
        // Pivot 的 Y/Z 始终保持启动时记录的舞台空间基准。
        float safeHorizontalLimit = Mathf.Abs(horizontalLimit);
        float displayedX = Mathf.Clamp(
            currentX + idleSwayOffset,
            -safeHorizontalLimit,
            safeHorizontalLimit
        );
        orbitPivot.position =
            basePivotPosition
            + Vector3.right * displayedX;

        // 第二阶段始终直接使用 Inspector 绑定的真实 Pivot。
        orbitCenter = orbitPivot.position;
        Vector3 currentOrbitCenter = isInSecondStage
            ? orbitCenter
            : orbitCenter + verticalOffset;
        // -----------------------------------------------------
        // 根据圆周运动计算 Camera Y / Z
        //
        // Y = sin(angle) * radius
        // Z = -cos(angle) * radius
        //
        // 因此 Camera 始终位于 Pivot 的后上方。
        // -----------------------------------------------------

        float activeOrbitRadius = isInSecondStage
            ? secondStageOrbitRadius
            : currentOrbitRadius;
        Vector3 orbitOffset = new Vector3(
            0f,
            Mathf.Sin(angleRad) * activeOrbitRadius,
            -Mathf.Cos(angleRad) * activeOrbitRadius
        );

        // -----------------------------------------------------
        // Camera 世界坐标
        // -----------------------------------------------------

        transform.position =
            currentOrbitCenter
            + orbitOffset;

        distanceToOrbitCenter = Vector3.Distance(
            transform.position,
            orbitCenter
        );

        // -----------------------------------------------------
        // 应用独立 Camera Pitch。
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
        ClearCameraDragState();
        ClearIdleHorizontalSway();
        currentVerticalPan = 0f;

        currentOrbitAngle =
            defaultOrbitAngle;

        currentPitchAngle =
            GetDefaultCameraPitch();

        currentOrbitRadius =
            defaultOrbitRadius;
        targetOrbitRadius = currentOrbitRadius;
        zoomFollowVelocity = 0f;

        secondStageStartOrbitAngle =
            defaultOrbitAngle;
        secondStageOrbitRadius =
            defaultOrbitRadius;
        secondStageProgress = 0f;
        isInSecondStage = false;

        UpdateSecondStageAnglesFromProgress();

        ApplyCameraTransform();
    }
}
