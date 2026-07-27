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
    [SerializeField] private float hoverLiftY = 100f;
    [SerializeField] private float hoverDuration = 0.12f;
    [SerializeField] private float returnDuration = 0.10f;

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
            StartTransition(
                HoverTargetAnchoredPosition,
                baseVisualScale,
                GetDisplayRotation(),
                hoverDuration
            );
            return;
        }

        if (isHovered)
        {
            ApplySorting(hoverSortingOrder);
            StartTransition(
                HoverTargetAnchoredPosition,
                baseVisualScale,
                GetDisplayRotation(),
                hoverDuration
            );
            return;
        }

        RestoreNormalSorting();
        StartTransition(
            baseVisualAnchoredPosition,
            baseVisualScale,
            baseVisualRotation,
            returnDuration
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

    private void StartTransition(
        Vector2 targetPosition,
        Vector3 targetScale,
        Quaternion targetRotation,
        float duration
    )
    {
        transitionTargetPosition = targetPosition;
        transitionTargetScale = targetScale;
        transitionTargetRotation = targetRotation;
        StopTransition();

        if (!isActiveAndEnabled || duration <= 0f)
        {
            ApplyVisualTransform(targetPosition, targetScale, targetRotation);
            return;
        }

        transitionCoroutine = StartCoroutine(
            AnimateVisualTransform(
                targetPosition,
                targetScale,
                targetRotation,
                duration
            )
        );
    }

    private IEnumerator AnimateVisualTransform(
        Vector2 targetPosition,
        Vector3 targetScale,
        Quaternion targetRotation,
        float duration
    )
    {
        Vector2 startPosition = visualRoot.anchoredPosition;
        Vector3 startScale = visualRoot.localScale;
        Quaternion startRotation = visualRoot.localRotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float smoothTime = Mathf.SmoothStep(0f, 1f, normalizedTime);

            ApplyVisualTransform(
                Vector2.Lerp(startPosition, targetPosition, smoothTime),
                Vector3.Lerp(startScale, targetScale, smoothTime),
                Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.LerpAngle(
                        startRotation.eulerAngles.z,
                        targetRotation.eulerAngles.z,
                        smoothTime
                    )
                )
            );

            yield return null;
        }

        ApplyVisualTransform(targetPosition, targetScale, targetRotation);
        transitionCoroutine = null;
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
        return Quaternion.Euler(0f, 0f, 0f);
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
