using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
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
    // 引用
    // =========================================================

    [Header("引用")]

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
    // Camera Envelope / 移动范围
    // =========================================================

    [Header("镜头范围：水平")]

    [Tooltip("Camera 左右最大移动距离")]
    [SerializeField]
    private float horizontalLimit = 1.2f;


    [Header("镜头范围：纵向")]

    [Tooltip("从默认位置朝显示更多 Background 的方向允许平移的最大距离")]
    [FormerlySerializedAs("verticalPanLimit")]
    [SerializeField, Min(0f)]
    private float backgroundPanLimit = 1.2f;

    [Tooltip("从默认位置朝显示更多 Floor 的方向允许平移的最大距离")]
    [SerializeField, Min(0f)]
    private float floorPanLimit = 1.2f;

    [Tooltip("从默认位置到 Background Threshold 期间逐渐增加的 Camera X 轴 Pitch")]
    [SerializeField]
    private float backgroundPanPitchOffset = 0f;

    [Tooltip("从默认位置到 Floor Pan Limit 期间逐渐增加的 Camera X 轴 Pitch")]
    [SerializeField]
    private float floorPanPitchOffset = 0f;


    [Header("镜头范围：缩放")]

    [Tooltip("Camera 最近可以靠近 Pivot 的距离")]
    [SerializeField]
    private float minOrbitRadius = 8.5f;

    [Tooltip("Camera 最远可以离开 Pivot 的距离")]
    [SerializeField]
    private float maxOrbitRadius = 14.5f;


    // =========================================================
    // 第二阶段几何
    // =========================================================

    [Header("背景侧第二阶段")]

    [Tooltip("第二阶段 Progress = 1 时 Camera 公转的最终圆弧角度")]
    [FormerlySerializedAs("minOrbitAngle")]
    [SerializeField]
    private float finalOrbitAngle = 15f;

    [Tooltip("Camera 自转的最终 X 轴 Pitch 角度")]
    [SerializeField]
    private float finalCameraPitch = 13.5f;


    // =========================================================
    // Camera Feel
    // =========================================================

    [Header("拖拽手感")]

    [Tooltip("鼠标左右拖动灵敏度")]
    [SerializeField]
    private float horizontalSensitivity = 0.003f;

    [Tooltip("按住左键后，Horizontal Camera 开始响应前允许的水平鼠标移动距离（像素）。\n用于过滤点击时的轻微手抖。")]
    [SerializeField, Min(0f)]
    private float horizontalStartDistance = 8f;

    [Tooltip(
        "越过 Horizontal Start Distance 后，\n" +
        "Horizontal Input 从 0 平滑恢复到完整强度所需的额外鼠标距离（像素）。\n" +
        "数值越大，启动越柔和。"
    )]
    [SerializeField, Min(0f)]
    private float horizontalStartBlendDistance = 10f;

    [Tooltip("鼠标上下拖动控制纵向平移的灵敏度")]
    [SerializeField]
    private float verticalPanSensitivity = 0.003f;

    [Tooltip("第二阶段统一 Progress 的输入灵敏度，以公转角范围换算")]
    [SerializeField]
    private float orbitSensitivity = 0.05f;

    [Tooltip("从单击判定为 Camera 拖拽所需的最小鼠标位移（像素）")]
    [FormerlySerializedAs("horizontalDragThreshold")]
    [SerializeField, Min(0f)]
    private float cameraDragThreshold = 0.5f;

    [Tooltip("Camera 拖拽输入追上鼠标目标所需的时间；数值越大，移动越有重量感")]
    [SerializeField, Min(0f)]
    private float cameraDragFollowTime = 0.18f;

    [Tooltip("松开鼠标时保留的水平拖拽速度比例")]
    [SerializeField, Range(0f, 1f)]
    private float horizontalInertia = 0.35f;

    [Tooltip("水平惯性每秒衰减强度，数值越大停止越快")]
    [SerializeField, Min(0f)]
    private float horizontalInertiaDamping = 6f;


    [Header("缩放手感")]

    [Tooltip("鼠标滚轮缩放灵敏度")]
    [SerializeField]
    private float zoomSensitivity = 0.005f;

    [Tooltip("Camera 当前缩放半径追上滚轮目标半径所需的时间")]
    [SerializeField, Min(0f)]
    private float zoomFollowTime = 0.15f;


    // =========================================================
    // 实验功能
    // =========================================================

    [Header("实验：水平待机呼吸")]

    [Tooltip("实验功能，可随时关闭；不参与基础 Camera Position 状态")]
    [SerializeField]
    private bool enableIdleHorizontalSway = false;

    [Tooltip(
        "当鼠标位于明确标记为可交互的 CameraInputBlocker 区域，\n" +
        "或正在进行该 UI 的 Pointer Press 时，\n" +
        "暂时淡出水平待机呼吸。\n" +
        "仅在 Enable Idle Horizontal Sway 开启时生效。"
    )]
    [SerializeField]
    private bool pauseIdleSwayOnInteractiveUI = true;

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

    [Header("输入方向")]

    [Tooltip("反转鼠标水平拖动方向")]
    [SerializeField]
    private bool invertHorizontal = true;

    [Tooltip("反转鼠标纵向拖动方向")]
    [SerializeField]
    private bool invertVertical = false;

    [Tooltip("反转滚轮缩放方向")]
    [SerializeField]
    private bool invertZoom = false;


    // =========================================================
    // Runtime
    // =========================================================

    private Vector3 basePivotPosition;

    [Header("运行时状态（只读参考 / 调试）")]
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

    [SerializeField, Range(0f, 1f)]
    private float secondStageProgress;

    [SerializeField]
    private float secondStageStartOrbitAngle;

    [SerializeField]
    private float secondStageOrbitRadius;

    [SerializeField]
    private bool isInSecondStage;

    [FormerlySerializedAs("isHorizontalDragging")]
    [SerializeField]
    private bool isCameraDragging;

    [SerializeField]
    private float horizontalVelocity;

    [SerializeField]
    private float idleSwayOffset;

    [SerializeField, Range(0f, 1f)]
    private float idleSwayWeight;

    [SerializeField]
    private Vector3 orbitCenter;

    [SerializeField]
    private float distanceToOrbitCenter;

    private Vector2 cameraDragStartPosition;
    private Vector2 rawDragPosition;
    private Vector2 smoothedDragPosition;
    private Vector2 dragFollowVelocity;
    private bool isCameraDragFollowing;
    private float pendingHorizontalInertiaVelocity;
    private float zoomFollowVelocity;
    private float idleSwayElapsed;
    private float idleSwayBlendVelocity;
    private bool isCameraDragBlockedForCurrentPress;
    private bool isInteractiveUiPressActive;
    private bool ignoreCameraDragUntilPointerRelease;
    private bool isCinematicControlActive;
    private EventSystem pointerEventSystem;
    private PointerEventData pointerEventData;
    private readonly List<RaycastResult> pointerRaycastResults =
        new List<RaycastResult>();

    private const float HorizontalVelocityStopThreshold = 0.001f;
    private const float DragFollowStopThreshold = 0.01f;
    private const float ZoomFollowStopThreshold = 0.0001f;
    private const float IdleSwayWeightStopThreshold = 0.0001f;

    public bool IsCinematicControlActive => isCinematicControlActive;
    public float DefaultOrbitRadius => defaultOrbitRadius;
    public float CurrentOrbitRadius => currentOrbitRadius;

    public float GetPerspectiveScaleRelativeToDefault(Vector3 worldPoint)
    {
        if (orbitPivot == null)
            return 1f;

        float defaultAngleRad = defaultOrbitAngle * Mathf.Deg2Rad;
        Vector3 defaultCameraPosition =
            basePivotPosition
            + new Vector3(
                0f,
                Mathf.Sin(defaultAngleRad) * defaultOrbitRadius,
                -Mathf.Cos(defaultAngleRad) * defaultOrbitRadius
            );
        Quaternion defaultCameraRotation = Quaternion.Euler(
            GetDefaultCameraPitch(),
            0f,
            0f
        );

        float currentDepth = Vector3.Dot(
            worldPoint - transform.position,
            transform.forward
        );
        float defaultDepth = Vector3.Dot(
            worldPoint - defaultCameraPosition,
            defaultCameraRotation * Vector3.forward
        );

        if (!IsValidPositiveDepth(currentDepth) ||
            !IsValidPositiveDepth(defaultDepth))
        {
            return 1f;
        }

        float perspectiveScale = defaultDepth / currentDepth;
        return float.IsNaN(perspectiveScale) ||
            float.IsInfinity(perspectiveScale) ||
            perspectiveScale <= 0f
            ? 1f
            : perspectiveScale;
    }

    private static bool IsValidPositiveDepth(float depth)
    {
        return !float.IsNaN(depth) &&
            !float.IsInfinity(depth) &&
            depth > Mathf.Epsilon;
    }

    public void SetCinematicControl(bool active)
    {
        if (isCinematicControlActive == active)
            return;

        isCinematicControlActive = active;
        if (active)
        {
            ClearCameraDragState();
            ClearPointerPressOwnership();
            ignoreCameraDragUntilPointerRelease = false;
            targetOrbitRadius = currentOrbitRadius;
            zoomFollowVelocity = 0f;
            return;
        }

        // 运镜期间按住的鼠标不能在解锁后接续为一次旧 Drag。
        Mouse mouse = Mouse.current;
        ignoreCameraDragUntilPointerRelease =
            mouse != null && mouse.leftButton.isPressed;
    }

    public void SetCinematicOrbitRadius(float radius)
    {
        if (!isCinematicControlActive)
            return;

        float clampedRadius = Mathf.Clamp(
            radius,
            minOrbitRadius,
            maxOrbitRadius
        );
        currentOrbitRadius = clampedRadius;
        targetOrbitRadius = clampedRadius;
        zoomFollowVelocity = 0f;
        ApplyCameraTransform();
    }


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
        ClearPointerPressOwnership();
        ClearIdleHorizontalSway();
    }

    private void Update()
    {
        if (isCinematicControlActive)
        {
            // Cinematic 期间不读取任何玩家 Camera 输入，但继续更新最终几何。
            UpdateIdleHorizontalSway(false);
            ApplyCameraTransform();
            return;
        }

        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;


        Vector2 pointerPosition = mouse.position.ReadValue();
        Vector2 delta = mouse.delta.ReadValue();
        GetPointerCameraInputPolicy(
            pointerPosition,
            out bool isPointerOverInteractiveUi,
            out bool blockCameraDrag,
            out bool blockCameraZoom
        );


        // -----------------------------------------------------
        // 左键统一控制水平与纵向 Camera 拖拽。
        // -----------------------------------------------------

        if (ignoreCameraDragUntilPointerRelease &&
            !mouse.leftButton.isPressed)
        {
            ignoreCameraDragUntilPointerRelease = false;
        }

        if (!ignoreCameraDragUntilPointerRelease)
        {
            UpdateCameraDragInput(
                mouse,
                delta,
                pointerPosition,
                isPointerOverInteractiveUi,
                blockCameraDrag
            );
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            ClearPointerPressOwnership();
        }


        // -----------------------------------------------------
        // 鼠标滚轮 Zoom
        // 不需要按住右键
        // -----------------------------------------------------

        float scrollY = mouse.scroll.ReadValue().y;

        UpdateZoom(blockCameraZoom ? 0f : scrollY);

        UpdateIdleHorizontalSway(isPointerOverInteractiveUi);


        // -----------------------------------------------------
        // 最终统一应用 Camera Transform
        // -----------------------------------------------------

        ApplyCameraTransform();
    }


    // =========================================================
    // Camera Drag Input
    // =========================================================

    /// <summary>
    /// 统一处理左键二维拖拽、Follow Catch-up 与水平惯性切换。
    /// </summary>
    private void UpdateCameraDragInput(
        Mouse mouse,
        Vector2 mouseDelta,
        Vector2 mousePosition,
        bool isPointerOverInteractiveUi,
        bool blockCameraDrag
    )
    {
        if (mouse.leftButton.wasPressedThisFrame)
        {
            isInteractiveUiPressActive = isPointerOverInteractiveUi;
            isCameraDragBlockedForCurrentPress = blockCameraDrag;
            if (!isCameraDragBlockedForCurrentPress)
            {
                BeginCameraDrag(mousePosition);
            }
        }

        if (mouse.leftButton.isPressed)
        {
            // Press 所有权只在 Mouse Down 决定，之后穿过 UI 不会改变归属。
            if (isCameraDragBlockedForCurrentPress)
            {
                UpdateCameraMotionWithoutCurrentPress();
                return;
            }

            if (!isCameraDragging)
            {
                Vector2 dragDelta =
                    mousePosition
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
                rawDragPosition.x = GetSoftHorizontalDragPosition(
                    mousePosition.x - cameraDragStartPosition.x
                );
                rawDragPosition.y += mouseDelta.y;
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

    private void UpdateCameraMotionWithoutCurrentPress()
    {
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

    private float GetSoftHorizontalDragPosition(
        float horizontalMouseDisplacement
    )
    {
        float direction = Mathf.Sign(horizontalMouseDisplacement);
        float distance = Mathf.Abs(horizontalMouseDisplacement);
        float startDistance = Mathf.Max(0f, horizontalStartDistance);
        if (distance <= startDistance)
            return 0f;

        float effectiveDistance = distance - startDistance;
        float blendDistance = Mathf.Max(
            0f,
            horizontalStartBlendDistance
        );
        if (blendDistance <= Mathf.Epsilon ||
            effectiveDistance >= blendDistance)
        {
            return direction * effectiveDistance;
        }

        float blendProgress = Mathf.Clamp01(
            effectiveDistance / blendDistance
        );
        float blendWeight = Mathf.SmoothStep(
            0f,
            1f,
            blendProgress
        );
        return direction * effectiveDistance * blendWeight;
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

    private void ClearPointerPressOwnership()
    {
        isCameraDragBlockedForCurrentPress = false;
        isInteractiveUiPressActive = false;
    }


    // =========================================================
    // Camera Input Priority
    // =========================================================

    private void GetPointerCameraInputPolicy(
        Vector2 pointerPosition,
        out bool isOverInteractiveUi,
        out bool blockCameraDrag,
        out bool blockCameraZoom
    )
    {
        isOverInteractiveUi = false;
        blockCameraDrag = false;
        blockCameraZoom = false;

        EventSystem currentEventSystem = EventSystem.current;
        if (currentEventSystem == null)
        {
            pointerEventSystem = null;
            pointerEventData = null;
            pointerRaycastResults.Clear();
            return;
        }

        if (pointerEventSystem != currentEventSystem ||
            pointerEventData == null)
        {
            pointerEventSystem = currentEventSystem;
            pointerEventData = new PointerEventData(currentEventSystem);
        }

        pointerEventData.Reset();
        pointerEventData.position = pointerPosition;
        pointerRaycastResults.Clear();
        currentEventSystem.RaycastAll(
            pointerEventData,
            pointerRaycastResults
        );

        for (int i = 0; i < pointerRaycastResults.Count; i++)
        {
            GameObject hitObject = pointerRaycastResults[i].gameObject;
            CameraInputBlocker blocker = hitObject != null
                ? hitObject.GetComponentInParent<CameraInputBlocker>()
                : null;
            if (blocker == null || !blocker.isActiveAndEnabled)
                continue;

            isOverInteractiveUi = true;
            blockCameraDrag |= blocker.BlockCameraDrag;
            blockCameraZoom |= blocker.BlockCameraZoom;

            if (blockCameraDrag && blockCameraZoom)
                break;
        }

        pointerRaycastResults.Clear();
    }


    // =========================================================
    // Horizontal
    // =========================================================

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
    // Vertical / Pure Pan
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


    // =========================================================
    // Second Stage
    // =========================================================

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
            finalOrbitAngle - secondStageStartOrbitAngle
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
            finalOrbitAngle,
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

    private void UpdateIdleHorizontalSway(
        bool isPointerOverInteractiveUi
    )
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
        bool shouldPauseForInteractiveUi =
            enableIdleHorizontalSway &&
            pauseIdleSwayOnInteractiveUI &&
            (isPointerOverInteractiveUi ||
                isInteractiveUiPressActive);
        float targetWeight =
            enableIdleHorizontalSway &&
            !isCinematicControlActive &&
            !hasActiveHorizontalInput &&
            !shouldPauseForInteractiveUi
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
