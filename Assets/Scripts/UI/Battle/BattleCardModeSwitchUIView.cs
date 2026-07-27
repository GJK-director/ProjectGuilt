// 脚本中文说明：
// 普通卡 / 罪卡切换按钮的视觉与交互脚本。
// 负责鼠标悬停、按下和切换翻面动画。
// 不直接依赖战斗控制器，通过 onModeChanged 事件通知外部切换手牌。

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BattleCardModeSwitchUIView :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler
{
    [Serializable]
    public sealed class ModeChangedEvent : UnityEvent<bool>
    {
    }

    [Header("视觉引用")]

    [Tooltip("真正播放位置、缩放、旋转动画的对象。建议使用按钮下面的 VisualRoot。")]
    [SerializeField]
    private RectTransform visualRoot;

    [Tooltip("负责显示两张卡牌图片的 Image。")]
    [SerializeField]
    private Image stateImage;

    [Header("状态图片")]

    [Tooltip("普通卡牌栏状态：普通卡在前。")]
    [SerializeField]
    private Sprite normalModeSprite;

    [Tooltip("罪卡栏状态：红色罪卡在前。")]
    [SerializeField]
    private Sprite sinModeSprite;

    [Tooltip("进入场景时是否默认显示罪卡栏。")]
    [SerializeField]
    private bool startInSinMode;

    [Header("悬停动画")]

    [Min(1f)]
    [SerializeField]
    private float hoverScale = 1.06f;

    [SerializeField]
    private float hoverYOffset = 6f;

    [SerializeField]
    private float hoverRotation = 2f;

    [Min(0.01f)]
    [SerializeField]
    private float hoverDuration = 0.14f;

    [Header("按下动画")]

    [Range(0.1f, 1f)]
    [SerializeField]
    private float pressedScale = 0.94f;

    [Min(0.01f)]
    [SerializeField]
    private float pressedDuration = 0.06f;

    [Header("切换动画")]

    [Tooltip("图片横向收窄到多薄。建议不要直接设为0。")]
    [Range(0.01f, 0.3f)]
    [SerializeField]
    private float flipMinimumXScale = 0.05f;

    [Tooltip("切换动画收窄阶段的额外旋转角度。")]
    [SerializeField]
    private float flipRotation = 8f;

    [Min(0.01f)]
    [SerializeField]
    private float flipCloseDuration = 0.09f;

    [Min(0.01f)]
    [SerializeField]
    private float flipOpenDuration = 0.11f;

    [Header("切换事件")]

    [Tooltip("参数为 true 时切换到罪卡栏，false 时切换到普通卡栏。")]
    [SerializeField]
    private ModeChangedEvent onModeChanged = new ModeChangedEvent();

    // 当前是否处于罪卡模式。
    private bool isSinMode;

    // 鼠标当前是否位于按钮上。
    private bool isHovered;

    // 鼠标是否正在按住按钮。
    private bool isPressed;

    // 是否正在播放正式切换动画。
    private bool isSwitchAnimating;

    // 启动时记录的基础Transform数据。
    private Vector2 baseAnchoredPosition;
    private Vector3 baseLocalScale;
    private float baseRotationZ;

    private Coroutine stateAnimationCoroutine;
    private Coroutine switchAnimationCoroutine;

    /// <summary>
    /// 当前是否为罪卡模式。
    /// </summary>
    public bool IsSinMode
    {
        get { return isSinMode; }
    }

    /// <summary>
    /// 当前是否正在播放切换动画。
    /// </summary>
    public bool IsSwitchAnimating
    {
        get { return isSwitchAnimating; }
    }

    private void Awake()
    {
        if (visualRoot == null)
        {
            Debug.LogError(
                nameof(BattleCardModeSwitchUIView) +
                " 缺少 Visual Root 引用。",
                this
            );

            enabled = false;
            return;
        }

        if (stateImage == null)
        {
            Debug.LogError(
                nameof(BattleCardModeSwitchUIView) +
                " 缺少 State Image 引用。",
                this
            );

            enabled = false;
            return;
        }

        baseAnchoredPosition = visualRoot.anchoredPosition;
        baseLocalScale = visualRoot.localScale;
        baseRotationZ = NormalizeAngle(visualRoot.localEulerAngles.z);

        isSinMode = startInSinMode;

        ApplyCurrentSprite();
        ApplyRestingStateImmediately();
    }

    private void OnDisable()
    {
        if (stateAnimationCoroutine != null)
        {
            StopCoroutine(stateAnimationCoroutine);
            stateAnimationCoroutine = null;
        }

        if (switchAnimationCoroutine != null)
        {
            StopCoroutine(switchAnimationCoroutine);
            switchAnimationCoroutine = null;
        }

        isHovered = false;
        isPressed = false;
        isSwitchAnimating = false;

        if (visualRoot != null)
        {
            ApplyRestingStateImmediately();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        if (!isSwitchAnimating)
        {
            AnimateToCurrentPointerState();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;

        if (!isSwitchAnimating)
        {
            AnimateToCurrentPointerState();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (isSwitchAnimating)
        {
            return;
        }

        isPressed = true;
        AnimateToCurrentPointerState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        isPressed = false;

        if (!isSwitchAnimating)
        {
            AnimateToCurrentPointerState();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        PlaySwitchAnimation();
    }

    /// <summary>
    /// 播放一次普通卡 / 罪卡切换动画。
    /// 也可以从其他脚本或Button事件调用。
    /// </summary>
    public void PlaySwitchAnimation()
    {
        if (!isActiveAndEnabled || isSwitchAnimating)
        {
            return;
        }

        switchAnimationCoroutine = StartCoroutine(SwitchAnimationRoutine());
    }

    /// <summary>
    /// 不播放动画，直接同步当前状态。
    /// 外部系统初始化或强制切换模式时可以调用。
    /// </summary>
    public void SetModeImmediately(bool useSinMode)
    {
        if (stateAnimationCoroutine != null)
        {
            StopCoroutine(stateAnimationCoroutine);
            stateAnimationCoroutine = null;
        }

        if (switchAnimationCoroutine != null)
        {
            StopCoroutine(switchAnimationCoroutine);
            switchAnimationCoroutine = null;
        }

        isSwitchAnimating = false;
        isPressed = false;
        isSinMode = useSinMode;

        ApplyCurrentSprite();
        ApplyRestingStateImmediately();
    }

    private IEnumerator SwitchAnimationRoutine()
    {
        isSwitchAnimating = true;
        isPressed = false;

        if (stateAnimationCoroutine != null)
        {
            StopCoroutine(stateAnimationCoroutine);
            stateAnimationCoroutine = null;
        }

        // 第一阶段：横向收窄，模拟卡牌转到侧面。
        Vector3 edgeScale = new Vector3(
            baseLocalScale.x * flipMinimumXScale,
            baseLocalScale.y * 1.04f,
            baseLocalScale.z
        );

        Vector2 edgePosition =
            baseAnchoredPosition +
            new Vector2(0f, hoverYOffset * 0.5f);

        float rotationDirection = isSinMode ? -1f : 1f;
        float edgeRotation =
            baseRotationZ +
            flipRotation * rotationDirection;

        yield return TweenTransform(
            visualRoot.localScale,
            edgeScale,
            visualRoot.anchoredPosition,
            edgePosition,
            NormalizeAngle(visualRoot.localEulerAngles.z),
            edgeRotation,
            flipCloseDuration,
            false
        );

        // 动画最窄处切换逻辑和图片。
        isSinMode = !isSinMode;
        ApplyCurrentSprite();

        // 通知外部系统刷新普通卡或罪卡手牌。
        onModeChanged?.Invoke(isSinMode);

        // 第二阶段：从侧面展开到当前鼠标状态。
        GetCurrentTargetState(
            out Vector3 targetScale,
            out Vector2 targetPosition,
            out float targetRotation
        );

        yield return TweenTransform(
            edgeScale,
            targetScale,
            edgePosition,
            targetPosition,
            edgeRotation,
            targetRotation,
            flipOpenDuration,
            true
        );

        isSwitchAnimating = false;
        switchAnimationCoroutine = null;

        // 动画期间鼠标可能已经移出，结束时重新校正一次。
        AnimateToCurrentPointerState();
    }

    private void AnimateToCurrentPointerState()
    {
        if (!isActiveAndEnabled || isSwitchAnimating)
        {
            return;
        }

        if (stateAnimationCoroutine != null)
        {
            StopCoroutine(stateAnimationCoroutine);
        }

        GetCurrentTargetState(
            out Vector3 targetScale,
            out Vector2 targetPosition,
            out float targetRotation
        );

        float duration = isPressed ? pressedDuration : hoverDuration;

        stateAnimationCoroutine = StartCoroutine(
            StateAnimationRoutine(
                targetScale,
                targetPosition,
                targetRotation,
                duration
            )
        );
    }

    private IEnumerator StateAnimationRoutine(
        Vector3 targetScale,
        Vector2 targetPosition,
        float targetRotation,
        float duration
    )
    {
        yield return TweenTransform(
            visualRoot.localScale,
            targetScale,
            visualRoot.anchoredPosition,
            targetPosition,
            NormalizeAngle(visualRoot.localEulerAngles.z),
            targetRotation,
            duration,
            false
        );

        stateAnimationCoroutine = null;
    }

    private void GetCurrentTargetState(
        out Vector3 targetScale,
        out Vector2 targetPosition,
        out float targetRotation
    )
    {
        if (isPressed)
        {
            targetScale = MultiplyScale(baseLocalScale, pressedScale);

            targetPosition =
                baseAnchoredPosition +
                new Vector2(0f, hoverYOffset * 0.35f);

            targetRotation = GetHoverRotation();
            return;
        }

        if (isHovered)
        {
            targetScale = MultiplyScale(baseLocalScale, hoverScale);

            targetPosition =
                baseAnchoredPosition +
                new Vector2(0f, hoverYOffset);

            targetRotation = GetHoverRotation();
            return;
        }

        targetScale = baseLocalScale;
        targetPosition = baseAnchoredPosition;
        targetRotation = baseRotationZ;
    }

    private float GetHoverRotation()
    {
        // 两种状态使用相反的轻微倾斜方向。
        float direction = isSinMode ? 1f : -1f;
        return baseRotationZ + hoverRotation * direction;
    }

    private IEnumerator TweenTransform(
        Vector3 startScale,
        Vector3 targetScale,
        Vector2 startPosition,
        Vector2 targetPosition,
        float startRotation,
        float targetRotation,
        float duration,
        bool useBackEase
    )
    {
        duration = Mathf.Max(0.01f, duration);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime = Mathf.Clamp01(elapsed / duration);

            float easedTime = useBackEase
                ? EaseOutBack(normalizedTime)
                : SmoothStep(normalizedTime);

            visualRoot.localScale = Vector3.LerpUnclamped(
                startScale,
                targetScale,
                easedTime
            );

            visualRoot.anchoredPosition = Vector2.LerpUnclamped(
                startPosition,
                targetPosition,
                easedTime
            );

            float rotation = Mathf.LerpAngle(
                startRotation,
                targetRotation,
                easedTime
            );

            visualRoot.localEulerAngles = new Vector3(0f, 0f, rotation);

            yield return null;
        }

        visualRoot.localScale = targetScale;
        visualRoot.anchoredPosition = targetPosition;
        visualRoot.localEulerAngles =
            new Vector3(0f, 0f, targetRotation);
    }

    private void ApplyCurrentSprite()
    {
        if (stateImage == null)
        {
            return;
        }

        Sprite targetSprite = isSinMode
            ? sinModeSprite
            : normalModeSprite;

        if (targetSprite == null)
        {
            Debug.LogWarning(
                nameof(BattleCardModeSwitchUIView) +
                " 当前状态缺少对应 Sprite。",
                this
            );

            return;
        }

        stateImage.sprite = targetSprite;
        stateImage.preserveAspect = true;
    }

    private void ApplyRestingStateImmediately()
    {
        GetCurrentTargetState(
            out Vector3 targetScale,
            out Vector2 targetPosition,
            out float targetRotation
        );

        visualRoot.localScale = targetScale;
        visualRoot.anchoredPosition = targetPosition;
        visualRoot.localEulerAngles =
            new Vector3(0f, 0f, targetRotation);
    }

    private static Vector3 MultiplyScale(
        Vector3 originalScale,
        float multiplier
    )
    {
        return new Vector3(
            originalScale.x * multiplier,
            originalScale.y * multiplier,
            originalScale.z
        );
    }

    private static float SmoothStep(float value)
    {
        return value * value * (3f - 2f * value);
    }

    private static float EaseOutBack(float value)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        float shifted = value - 1f;

        return 1f +
               c3 * shifted * shifted * shifted +
               c1 * shifted * shifted;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f)
        {
            angle -= 360f;
        }

        while (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }
}