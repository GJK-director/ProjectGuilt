using UnityEngine;

[DefaultExecutionOrder(200)]
public sealed class BattleCharacterStatusWorldFollower : MonoBehaviour
{
    [Header("世界与Canvas")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Canvas targetCanvas;

    [Header("世界锚点")]
    [SerializeField] private Transform headWorldAnchor;
    [SerializeField] private Transform footWorldAnchor;
    [SerializeField] private Transform centerWorldAnchor;
    private SpriteRenderer visualBodyRenderer;

    [Header("UI目标")]
    [SerializeField] private RectTransform headSlotGroup;
    [SerializeField] private RectTransform footStatusGroup;
    [SerializeField] private RectTransform selfActionDropZone;

    [Header("局部偏移")]
    [SerializeField] private Vector2 headOffset;
    [SerializeField] private Vector2 footOffset;
    [SerializeField] private Vector2 centerOffset;

    [Header("运行设置")]
    [SerializeField] private bool followEveryLateUpdate = true;
    [SerializeField] private bool hideWhenBehindCamera = true;
    [SerializeField] private CanvasGroup visibilityCanvasGroup;

    private bool attemptedInitialReferenceLookup;
    private bool warnedMissingWorldCamera;
    private bool warnedMissingCanvas;
    private bool warnedMissingVisibilityCanvasGroup;
    private bool warnedMissingHeadGroup;
    private bool warnedMissingFootGroup;
    private bool warnedMissingSelfGroup;
    private Camera lastCanvasEventCamera;
    private GrayboxBattleCameraController cameraController;
    private Vector3 headBaseScale = Vector3.one;
    private Vector3 footBaseScale = Vector3.one;

    public bool IsBound =>
        worldCamera != null &&
        targetCanvas != null &&
        headWorldAnchor != null &&
        footWorldAnchor != null &&
        centerWorldAnchor != null;

    internal Canvas ResolvedTargetCanvas => targetCanvas;
    internal Camera LastCanvasEventCamera => lastCanvasEventCamera;

    private void Awake()
    {
        CacheUIBaseScales();
        ResolveInitialReferencesOnce();
    }

    private void OnEnable()
    {
        ResolveInitialReferencesOnce();
        RefreshNow();
    }

    private void LateUpdate()
    {
        // Billboard已先完成本帧朝向，再用同帧Camera与角色状态更新UI位置。
        if (followEveryLateUpdate)
        {
            RefreshNow();
        }
    }

    public void RefreshNow()
    {
        ResolveInitialReferencesOnce();

        if (worldCamera == null)
        {
            WarnOnce(
                ref warnedMissingWorldCamera,
                "角色状态UI跟随失败：未绑定世界摄像机。"
            );
            return;
        }

        if (targetCanvas == null)
        {
            WarnOnce(
                ref warnedMissingCanvas,
                "角色状态UI跟随失败：未绑定且无法在父级找到Canvas。"
            );
            return;
        }

        Vector3 headScreenPoint = default;
        Vector3 footScreenPoint = default;
        Vector3 centerScreenPoint = default;
        bool hasHead = TryProjectHead(
            out headScreenPoint,
            out Vector3 headWorldPoint
        );
        bool hasFoot = TryProjectFoot(
            out footScreenPoint,
            out Vector3 footWorldPoint
        );
        bool hasCenter = TryProjectAnchor(centerWorldAnchor, out centerScreenPoint);
        float perspectiveScale = GetPerspectiveScale(
            hasHead,
            headWorldPoint,
            hasFoot,
            footWorldPoint
        );
        ApplyPerspectiveScale(perspectiveScale);
        bool anyAnchorBehind =
            (hasHead && headScreenPoint.z <= 0f) ||
            (hasFoot && footScreenPoint.z <= 0f) ||
            (hasCenter && centerScreenPoint.z <= 0f);

        if (hideWhenBehindCamera && anyAnchorBehind)
        {
            if (visibilityCanvasGroup != null)
            {
                visibilityCanvasGroup.alpha = 0f;
                return;
            }
            else
            {
                WarnOnce(
                    ref warnedMissingVisibilityCanvasGroup,
                    "角色位于摄像机后方，但未绑定Visibility CanvasGroup；后方锚点将跳过，其他前方锚点继续更新。"
                );
            }
        }

        if (visibilityCanvasGroup != null)
        {
            visibilityCanvasGroup.alpha = 1f;
        }

        UpdateGroupFromProjectedPoint(
            hasHead,
            headScreenPoint,
            headSlotGroup,
            headOffset * perspectiveScale,
            ref warnedMissingHeadGroup,
            "HeadSlotGroup"
        );
        UpdateGroupFromProjectedPoint(
            hasFoot,
            footScreenPoint,
            footStatusGroup,
            footOffset * perspectiveScale,
            ref warnedMissingFootGroup,
            "FootStatusGroup"
        );
        UpdateGroupFromProjectedPoint(
            hasCenter,
            centerScreenPoint,
            selfActionDropZone,
            centerOffset,
            ref warnedMissingSelfGroup,
            "SelfActionDropZone"
        );
    }

    public void Bind(
        Camera camera,
        Canvas canvas,
        Transform headAnchor,
        Transform footAnchor,
        Transform centerAnchor
    )
    {
        worldCamera = camera;
        cameraController = null;
        targetCanvas = canvas;
        headWorldAnchor = headAnchor;
        footWorldAnchor = footAnchor;
        centerWorldAnchor = centerAnchor;
        ResolveCameraController();
        ResetMissingReferenceWarnings();
        RefreshNow();
    }

    public void SetVisualFootSource(SpriteRenderer renderer)
    {
        visualBodyRenderer = renderer;
        RefreshNow();
    }

    public bool TryGetFootProjectionWorldPointForDiagnostic(
        out Vector3 worldPoint
    )
    {
        return TryGetFootProjectionWorldPoint(out worldPoint);
    }

    public void SetWorldAnchors(
        Transform headAnchor,
        Transform footAnchor,
        Transform centerAnchor
    )
    {
        headWorldAnchor = headAnchor;
        footWorldAnchor = footAnchor;
        centerWorldAnchor = centerAnchor;
        RefreshNow();
    }

    public void ClearWorldAnchors()
    {
        headWorldAnchor = null;
        footWorldAnchor = null;
        centerWorldAnchor = null;
        visualBodyRenderer = null;
    }

    internal void ConfigureUIRootsForTesting(
        RectTransform headGroup,
        RectTransform footGroup,
        RectTransform selfGroup
    )
    {
        headSlotGroup = headGroup;
        footStatusGroup = footGroup;
        selfActionDropZone = selfGroup;
        CacheUIBaseScales();
    }

    internal void ConfigureOffsetsForTesting(
        Vector2 testHeadOffset,
        Vector2 testFootOffset,
        Vector2 testCenterOffset
    )
    {
        headOffset = testHeadOffset;
        footOffset = testFootOffset;
        centerOffset = testCenterOffset;
    }

    internal void ConfigureVisibilityForTesting(
        CanvasGroup group,
        bool hideBehindCamera
    )
    {
        visibilityCanvasGroup = group;
        hideWhenBehindCamera = hideBehindCamera;
    }

    internal void SetWorldCameraForTesting(Camera camera)
    {
        worldCamera = camera;
        cameraController = null;
        ResolveCameraController();
    }

    internal bool TryGetAnchorLocalPosition(
        Transform worldAnchor,
        RectTransform targetGroup,
        out Vector2 localPoint
    )
    {
        localPoint = Vector2.zero;
        if (worldCamera == null || targetCanvas == null ||
            worldAnchor == null || targetGroup == null)
        {
            return false;
        }

        Vector3 screenPoint = worldCamera.WorldToScreenPoint(
            worldAnchor.position
        );
        if (screenPoint.z <= 0f)
        {
            return false;
        }

        return TryConvertScreenPointToTargetParent(
            screenPoint,
            targetGroup,
            out localPoint
        );
    }

    private bool TryProjectAnchor(
        Transform worldAnchor,
        out Vector3 screenPoint
    )
    {
        screenPoint = default;
        if (worldAnchor == null)
        {
            return false;
        }
        screenPoint = worldCamera.WorldToScreenPoint(worldAnchor.position);
        return true;
    }

    private bool TryProjectFoot(out Vector3 screenPoint)
    {
        return TryProjectFoot(out screenPoint, out _);
    }

    private bool TryProjectFoot(
        out Vector3 screenPoint,
        out Vector3 worldPoint
    )
    {
        screenPoint = default;
        if (!TryGetFootProjectionWorldPoint(out worldPoint))
        {
            return false;
        }

        screenPoint = worldCamera.WorldToScreenPoint(worldPoint);
        return true;
    }

    private bool TryProjectHead(out Vector3 screenPoint)
    {
        return TryProjectHead(out screenPoint, out _);
    }

    private bool TryProjectHead(
        out Vector3 screenPoint,
        out Vector3 worldPoint
    )
    {
        screenPoint = default;
        if (!TryGetHeadProjectionWorldPoint(out worldPoint))
        {
            return false;
        }

        screenPoint = worldCamera.WorldToScreenPoint(worldPoint);
        return true;
    }

    private float GetPerspectiveScale(
        bool hasHead,
        Vector3 headWorldPoint,
        bool hasFoot,
        Vector3 footWorldPoint
    )
    {
        if (cameraController == null)
        {
            return 1f;
        }

        Vector3 referencePoint;
        if (hasHead && hasFoot)
        {
            referencePoint = (headWorldPoint + footWorldPoint) * 0.5f;
        }
        else if (hasHead)
        {
            referencePoint = headWorldPoint;
        }
        else if (hasFoot)
        {
            referencePoint = footWorldPoint;
        }
        else if (centerWorldAnchor != null)
        {
            referencePoint = centerWorldAnchor.position;
        }
        else
        {
            return 1f;
        }

        return cameraController.GetPerspectiveScaleRelativeToDefault(
            referencePoint
        );
    }

    private void ApplyPerspectiveScale(float perspectiveScale)
    {
        if (headSlotGroup != null)
        {
            headSlotGroup.localScale = headBaseScale * perspectiveScale;
        }

        if (footStatusGroup != null)
        {
            footStatusGroup.localScale = footBaseScale * perspectiveScale;
        }
    }

    private void CacheUIBaseScales()
    {
        headBaseScale = headSlotGroup != null
            ? headSlotGroup.localScale
            : Vector3.one;
        footBaseScale = footStatusGroup != null
            ? footStatusGroup.localScale
            : Vector3.one;
    }

    private bool TryGetHeadProjectionWorldPoint(out Vector3 worldPoint)
    {
        worldPoint = default;
        if (visualBodyRenderer != null &&
            visualBodyRenderer.sprite != null)
        {
            Sprite sprite = visualBodyRenderer.sprite;
            Vector3 spriteLocalHead = new Vector3(
                sprite.bounds.center.x,
                sprite.bounds.max.y,
                sprite.bounds.center.z
            );

            // 当前Bridge只验证真实视觉头顶投影，不代表最终表现层架构。
            worldPoint = visualBodyRenderer.transform.TransformPoint(
                spriteLocalHead
            );
            return true;
        }

        if (headWorldAnchor == null)
        {
            return false;
        }

        worldPoint = headWorldAnchor.position;
        return true;
    }

    private bool TryGetFootProjectionWorldPoint(out Vector3 worldPoint)
    {
        worldPoint = default;
        if (footWorldAnchor != null)
        {
            worldPoint = footWorldAnchor.position;
            return true;
        }

        if (centerWorldAnchor != null)
        {
            worldPoint = centerWorldAnchor.position;
            return true;
        }

        return false;
    }

    private void UpdateGroupFromProjectedPoint(
        bool hasProjectedPoint,
        Vector3 screenPoint,
        RectTransform targetGroup,
        Vector2 offset,
        ref bool warnedMissingTarget,
        string targetLabel
    )
    {
        if (!hasProjectedPoint)
        {
            return;
        }
        if (targetGroup == null)
        {
            WarnOnce(
                ref warnedMissingTarget,
                "角色状态UI跟随跳过：未绑定" + targetLabel + "。"
            );
            return;
        }
        if (screenPoint.z <= 0f)
        {
            return;
        }

        Vector2 localPoint;
        if (TryConvertScreenPointToTargetParent(
                screenPoint,
                targetGroup,
                out localPoint))
        {
            targetGroup.anchoredPosition = localPoint + offset;
        }
    }

    private bool TryConvertScreenPointToTargetParent(
        Vector3 screenPoint,
        RectTransform targetGroup,
        out Vector2 localPoint
    )
    {
        localPoint = Vector2.zero;
        RectTransform targetParent = targetGroup.parent as RectTransform;
        if (targetParent == null)
        {
            return false;
        }

        lastCanvasEventCamera = GetCanvasEventCamera(targetCanvas);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetParent,
            screenPoint,
            lastCanvasEventCamera,
            out localPoint
        );
    }

    private static Camera GetCanvasEventCamera(Canvas canvas)
    {
        if (canvas == null ||
            canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }
        return canvas.worldCamera;
    }

    private void ResolveInitialReferencesOnce()
    {
        if (attemptedInitialReferenceLookup)
        {
            return;
        }
        attemptedInitialReferenceLookup = true;

        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
        ResolveCameraController();
    }

    private void ResolveCameraController()
    {
        if (cameraController != null)
        {
            return;
        }

        if (worldCamera != null)
        {
            cameraController =
                worldCamera.GetComponent<GrayboxBattleCameraController>();
        }

        if (cameraController == null)
        {
            cameraController =
                FindFirstObjectByType<GrayboxBattleCameraController>();
        }
    }

    private void ResetMissingReferenceWarnings()
    {
        warnedMissingWorldCamera = false;
        warnedMissingCanvas = false;
        warnedMissingVisibilityCanvasGroup = false;
        warnedMissingHeadGroup = false;
        warnedMissingFootGroup = false;
        warnedMissingSelfGroup = false;
    }

    private void WarnOnce(ref bool warned, string message)
    {
        if (warned)
        {
            return;
        }
        warned = true;
        Debug.LogWarning(message, this);
    }

    [ContextMenu("验证角色状态UI跟随配置")]
    private void ValidateConfiguration()
    {
        Canvas resolvedCanvas = targetCanvas != null
            ? targetCanvas
            : GetComponentInParent<Canvas>();
        bool valid = true;

        if (worldCamera == null)
        {
            Debug.LogWarning("未绑定World Camera，应绑定Main Camera。", this);
            valid = false;
        }
        if (resolvedCanvas == null)
        {
            Debug.LogWarning("未绑定且无法在父级找到Target Canvas。", this);
            valid = false;
        }
        else
        {
            Debug.Log("Target Canvas模式：" + resolvedCanvas.renderMode, this);
            if (resolvedCanvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                resolvedCanvas.worldCamera != null)
            {
                Debug.LogWarning(
                    "Overlay Canvas的UI坐标转换应使用null Camera。",
                    this
                );
                valid = false;
            }
        }

        valid &= ValidateRequiredReference(headWorldAnchor, "HeadUIAnchor");
        valid &= ValidateRequiredReference(footWorldAnchor, "FootUIAnchor");
        if (centerWorldAnchor == null)
        {
            Debug.Log("CenterAnchor未绑定，Head和Foot仍可独立跟随。", this);
        }
        valid &= ValidateTargetGroup(headSlotGroup, "HeadSlotGroup", true);
        valid &= ValidateTargetGroup(footStatusGroup, "FootStatusGroup", true);
        ValidateTargetGroup(selfActionDropZone, "SelfActionDropZone", false);

        RectTransform rootRect = transform as RectTransform;
        if (rootRect == null)
        {
            Debug.LogWarning("CharacterStatus根对象不是RectTransform。", this);
            valid = false;
        }
        else
        {
            bool stretched = Approximately(rootRect.anchorMin, Vector2.zero) &&
                Approximately(rootRect.anchorMax, Vector2.one);
            bool zeroOffsets = Approximately(
                    rootRect.offsetMin,
                    Vector2.zero
                ) &&
                Approximately(rootRect.offsetMax, Vector2.zero);
            bool scaleOne = Approximately(rootRect.localScale, Vector3.one);
            bool rotationZero = Quaternion.Angle(
                rootRect.localRotation,
                Quaternion.identity
            ) < 0.01f;
            if (!stretched)
            {
                Debug.LogWarning(
                    "CharacterStatus根对象应四向拉伸：Anchor Min 0,0 / Max 1,1。",
                    this
                );
                valid = false;
            }
            if (!zeroOffsets)
            {
                Debug.LogWarning(
                    "CharacterStatus根对象四边偏移应为0。",
                    this
                );
                valid = false;
            }
            if (!scaleOne || !rotationZero)
            {
                Debug.LogWarning(
                    "CharacterStatus根对象Scale应为1，Rotation应为0。",
                    this
                );
                valid = false;
            }
        }

        Debug.Log(
            "Follower Offset：Head=" + headOffset +
            " Foot=" + footOffset +
            " Center=" + centerOffset,
            this
        );
        Debug.Log(
            valid
                ? "角色状态UI跟随配置验证通过。"
                : "角色状态UI跟随配置存在缺失，请检查以上日志。",
            this
        );
    }

    private bool ValidateRequiredReference(Object value, string label)
    {
        if (value != null)
        {
            return true;
        }
        Debug.LogWarning("未绑定" + label + "。", this);
        return false;
    }

    private bool ValidateTargetGroup(
        RectTransform targetGroup,
        string label,
        bool required
    )
    {
        if (targetGroup == null)
        {
            Debug.Log(
                label + (required ? "未绑定。" : "为空，当前允许跳过。"),
                this
            );
            return !required;
        }
        bool valid = true;
        if (!(targetGroup.parent is RectTransform))
        {
            Debug.LogWarning(label + "的父对象必须是RectTransform。", this);
            valid = false;
        }
        if (!targetGroup.IsChildOf(transform))
        {
            Debug.LogWarning(
                label + "不属于当前CharacterStatus根结构。",
                this
            );
            valid = false;
        }
        if (!targetGroup.gameObject.activeSelf)
        {
            Debug.Log(label + "当前为inactive，Follower不会自动激活它。", this);
        }
        return valid;
    }

    private static bool Approximately(Vector2 left, Vector2 right)
    {
        return Vector2.SqrMagnitude(left - right) < 0.0001f;
    }

    private static bool Approximately(Vector3 left, Vector3 right)
    {
        return Vector3.SqrMagnitude(left - right) < 0.0001f;
    }
}
