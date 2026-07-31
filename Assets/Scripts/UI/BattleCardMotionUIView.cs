using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleCardMotionUIView : MonoBehaviour
{
    [Header("基础引用")]
    [SerializeField] private RectTransform cardRoot;
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private Canvas sortingCanvas;

    [Header("悬停与选中")]
    [Tooltip("卡牌悬停或选中展开后的目标世界Z轴角度。0代表无论手牌基础旋转多少，展开后都朝正。")]
    [SerializeField] private float expandedWorldRotationZ = 0f;

    [Tooltip("卡牌悬停或选中时，相对基础位置向上抬升的UI距离。")]
    [SerializeField] private float hoverLiftY = 100f;

    [Header("指数平滑")]
    [Tooltip("位置贴近目标的速度。13.3886在60 FPS下约等于每帧移动剩余距离20%。")]
    [Min(0f)]
    [SerializeField] private float positionSharpness = 13.3886f;

    [Tooltip("旋转贴近目标的速度。13.3886在60 FPS下约等于每帧移动剩余距离20%。")]
    [Min(0f)]
    [SerializeField] private float rotationSharpness = 13.3886f;

    [Tooltip("缩放贴近目标的速度。13.3886在60 FPS下约等于每帧移动剩余距离20%。")]
    [Min(0f)]
    [SerializeField] private float scaleSharpness = 13.3886f;

    [Tooltip("位置进入该距离后精确吸附到目标，避免协程永久运行。")]
    [Min(0f)]
    [SerializeField] private float positionSnapDistance = 0.1f;

    [Tooltip("旋转进入该角度后精确吸附到目标，避免协程永久运行。")]
    [Min(0f)]
    [SerializeField] private float rotationSnapAngle = 0.05f;

    [Tooltip("缩放进入该距离后精确吸附到目标，避免协程永久运行。")]
    [Min(0f)]
    [SerializeField] private float scaleSnapDistance = 0.001f;

    [Header("显示层级")]
    [SerializeField] private int normalSortingOrder;
    [SerializeField] private int hoverSortingOrder = 10;
    [SerializeField] private int selectedSortingOrder = 20;

    private Vector2 baseVisualAnchoredPosition;
    private Vector3 baseVisualScale;
    private Quaternion baseVisualRotation;
    private bool hasCachedBaseTransform;

    private bool originalOverrideSorting;
    private int originalSortingOrder;
    private bool hasCachedOriginalCanvasState;

    private bool isHovered;
    private bool isSelected;
    private bool warnedMissingVisualRoot;
    private bool warnedMissingGraphicRaycaster;

    private Coroutine transitionCoroutine;
    private Vector2 transitionTargetPosition;
    private Vector3 transitionTargetScale;
    private Quaternion transitionTargetRotation;

    public bool IsHovered => isHovered;
    public bool IsSelected => isSelected;
    public bool HasCachedBaseTransform => hasCachedBaseTransform;
    public bool HasCachedOriginalCanvasState =>
        hasCachedOriginalCanvasState;
    public bool OriginalOverrideSorting => originalOverrideSorting;
    public int OriginalSortingOrder => originalSortingOrder;
    public int ActiveTransitionCount => transitionCoroutine != null ? 1 : 0;
    public Vector2 BaseVisualAnchoredPosition => baseVisualAnchoredPosition;
    public Vector3 BaseVisualScale => baseVisualScale;
    public Quaternion BaseVisualRotation => baseVisualRotation;
    public Vector2 HoverTargetAnchoredPosition =>
        baseVisualAnchoredPosition + Vector2.up * hoverLiftY;
    public Vector3 HoverTargetScale => baseVisualScale;
    public int CurrentSortingOrder =>
        sortingCanvas != null ? sortingCanvas.sortingOrder : 0;
    public bool CurrentOverrideSorting =>
        sortingCanvas != null && sortingCanvas.overrideSorting;

    void Awake()
    {
        EnsureInitialized();
    }

    void OnEnable()
    {
        EnsureInitialized();

        if (hasCachedBaseTransform)
        {
            ResetVisualState();
        }
    }

    public void EnsureInitialized()
    {
        if (cardRoot == null)
        {
            cardRoot = transform as RectTransform;
        }

        if (sortingCanvas == null)
        {
            sortingCanvas = GetComponent<Canvas>();
        }

        if (sortingCanvas != null)
        {
            CacheOriginalCanvasStateOnce();

            if (!warnedMissingGraphicRaycaster &&
                sortingCanvas.GetComponent<GraphicRaycaster>() == null)
            {
                Debug.LogWarning(
                    "BattleCardMotionUIView 的嵌套 Canvas 缺少 GraphicRaycaster，卡牌无法接收 Hover 和 Click 事件。请在同一根对象上手动添加。",
                    this
                );
                warnedMissingGraphicRaycaster = true;
            }
        }

        if (visualRoot == null)
        {
            if (!warnedMissingVisualRoot)
            {
                Debug.LogWarning(
                    "BattleCardMotionUIView 缺少 VisualRoot 引用，卡牌悬停与选中动画不会运行。",
                    this
                );
                warnedMissingVisualRoot = true;
            }

            return;
        }

        if (!hasCachedBaseTransform)
        {
            CacheBaseVisualTransform();
        }
    }

    public void RecalculateBaseVisualTransform()
    {
        StopTransition();

        if (visualRoot == null || isHovered || isSelected)
        {
            return;
        }

        CacheBaseVisualTransform();
    }

    public void SetHovered(bool hovered)
    {
        isHovered = hovered;

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        ApplyCurrentState();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        ApplyCurrentState();
    }

    public void CompleteCurrentTransitionImmediately()
    {
        if (!hasCachedBaseTransform)
        {
            return;
        }

        StopTransition();
        ApplyVisualTransform(
            transitionTargetPosition,
            transitionTargetScale,
            transitionTargetRotation
        );
    }

    internal bool AdvanceCurrentTransitionForTesting(
        float unscaledDeltaTime
    )
    {
        if (!hasCachedBaseTransform || visualRoot == null)
        {
            return false;
        }

        bool completed = AdvanceTransition(unscaledDeltaTime);
        if (completed)
        {
            StopTransition();
        }

        return completed;
    }

    public void ResetVisualState()
    {
        RestoreVisualImmediately();
    }

    public void RestoreVisualImmediately()
    {
        StopTransition();
        isHovered = false;
        isSelected = false;

        if (hasCachedBaseTransform && visualRoot != null)
        {
            transitionTargetPosition = baseVisualAnchoredPosition;
            transitionTargetScale = baseVisualScale;
            transitionTargetRotation = baseVisualRotation;
            visualRoot.anchoredPosition = baseVisualAnchoredPosition;
            visualRoot.localScale = baseVisualScale;
            visualRoot.localRotation = baseVisualRotation;
        }

        RestoreOriginalSorting();
    }

    void OnDisable()
    {
        RestoreVisualImmediately();
    }

    void OnDestroy()
    {
        RestoreVisualImmediately();
    }

    private void ApplyCurrentState()
    {
        EnsureInitialized();

        if (!hasCachedBaseTransform || visualRoot == null)
        {
            return;
        }

        if (isSelected)
        {
            ApplySorting(selectedSortingOrder);
            SetTransitionTarget(
                HoverTargetAnchoredPosition,
                baseVisualScale,
                GetDisplayRotation()
            );
            return;
        }

        if (isHovered)
        {
            ApplySorting(hoverSortingOrder);
            SetTransitionTarget(
                HoverTargetAnchoredPosition,
                baseVisualScale,
                GetDisplayRotation()
            );
            return;
        }

        RestoreNormalSorting();
        SetTransitionTarget(
            baseVisualAnchoredPosition,
            baseVisualScale,
            baseVisualRotation
        );
    }

    private void CacheBaseVisualTransform()
    {
        baseVisualAnchoredPosition = visualRoot.anchoredPosition;
        baseVisualScale = visualRoot.localScale;
        baseVisualRotation = visualRoot.localRotation;
        transitionTargetPosition = baseVisualAnchoredPosition;
        transitionTargetScale = baseVisualScale;
        transitionTargetRotation = baseVisualRotation;
        hasCachedBaseTransform = true;
    }

    private void CacheOriginalCanvasStateOnce()
    {
        if (hasCachedOriginalCanvasState || sortingCanvas == null)
        {
            return;
        }

        originalOverrideSorting = sortingCanvas.overrideSorting;
        originalSortingOrder = sortingCanvas.sortingOrder;
        hasCachedOriginalCanvasState = true;
    }

    private void SetTransitionTarget(
        Vector2 targetPosition,
        Vector3 targetScale,
        Quaternion targetRotation
    )
    {
        transitionTargetPosition = targetPosition;
        transitionTargetScale = targetScale;
        transitionTargetRotation = targetRotation;

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            ApplyVisualTransform(targetPosition, targetScale, targetRotation);
            return;
        }

        if (transitionCoroutine == null)
        {
            transitionCoroutine = StartCoroutine(
                TrackTransition()
            );
        }
    }

    private IEnumerator TrackTransition()
    {
        while (true)
        {
            yield return null;

            if (AdvanceTransition(Time.unscaledDeltaTime))
            {
                transitionCoroutine = null;
                yield break;
            }
        }
    }

    private bool AdvanceTransition(float unscaledDeltaTime)
    {
        Vector2 nextPosition = BattleUIExponentialSmoothing.Smooth(
            visualRoot.anchoredPosition,
            transitionTargetPosition,
            positionSharpness,
            unscaledDeltaTime
        );
        Vector3 nextScale = BattleUIExponentialSmoothing.Smooth(
            visualRoot.localScale,
            transitionTargetScale,
            scaleSharpness,
            unscaledDeltaTime
        );
        Quaternion nextRotation = BattleUIExponentialSmoothing.Smooth(
            visualRoot.localRotation,
            transitionTargetRotation,
            rotationSharpness,
            unscaledDeltaTime
        );

        bool positionReached =
            Vector2.Distance(
                nextPosition,
                transitionTargetPosition
            ) <= Mathf.Max(0f, positionSnapDistance);
        bool scaleReached =
            Vector3.Distance(
                nextScale,
                transitionTargetScale
            ) <= Mathf.Max(0f, scaleSnapDistance);
        bool rotationReached =
            Quaternion.Angle(
                nextRotation,
                transitionTargetRotation
            ) <= Mathf.Max(0f, rotationSnapAngle);

        if (positionReached)
        {
            nextPosition = transitionTargetPosition;
        }

        if (scaleReached)
        {
            nextScale = transitionTargetScale;
        }

        if (rotationReached)
        {
            nextRotation = transitionTargetRotation;
        }

        ApplyVisualTransform(
            nextPosition,
            nextScale,
            nextRotation
        );

        return positionReached &&
            scaleReached &&
            rotationReached;
    }

    private void ApplyVisualTransform(
        Vector2 position,
        Vector3 scale,
        Quaternion rotation
    )
    {
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.anchoredPosition = position;
        visualRoot.localScale = scale;
        visualRoot.localRotation = rotation;
    }

    private Quaternion GetDisplayRotation()
    {
        if (visualRoot == null)
        {
            return Quaternion.identity;
        }

        Quaternion targetWorldRotation =
            Quaternion.Euler(0f, 0f, expandedWorldRotationZ);
        Transform parent = visualRoot.parent;

        if (parent == null)
        {
            return targetWorldRotation;
        }

        return Quaternion.Inverse(parent.rotation) *
            targetWorldRotation;
    }

    private void ApplySorting(int sortingOrder)
    {
        if (sortingCanvas == null)
        {
            return;
        }

        sortingCanvas.overrideSorting = true;
        sortingCanvas.sortingOrder = sortingOrder;
    }

    private void RestoreNormalSorting()
    {
        if (sortingCanvas == null)
        {
            return;
        }

        if (hasCachedOriginalCanvasState)
        {
            sortingCanvas.sortingOrder = originalSortingOrder;
            sortingCanvas.overrideSorting = originalOverrideSorting;
            return;
        }

        sortingCanvas.sortingOrder = normalSortingOrder;
        sortingCanvas.overrideSorting = false;
    }

    private void RestoreOriginalSorting()
    {
        if (sortingCanvas == null || !hasCachedOriginalCanvasState)
        {
            return;
        }

        sortingCanvas.sortingOrder = originalSortingOrder;
        sortingCanvas.overrideSorting = originalOverrideSorting;
    }

    private void StopTransition()
    {
        if (transitionCoroutine == null)
        {
            return;
        }

        StopCoroutine(transitionCoroutine);
        transitionCoroutine = null;
    }
}
