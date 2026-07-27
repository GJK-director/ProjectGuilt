// 脚本中文说明：
// 战斗开始按钮的视觉动画。
// 鼠标悬停时改变文字颜色并显示装饰图片。
// 鼠标按下时缩小文字与装饰图片，产生按压感。
// 真正的战斗开始逻辑仍然由原 Button.onClick 负责。

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BattleStartButtonUIView :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("组件引用")]

    [Tooltip("同时包含文字和start-2的动画根节点。")]
    [SerializeField]
    private RectTransform visualRoot;

    [Tooltip("开始按钮的文字。")]
    [SerializeField]
    private TMP_Text labelText;

    [Tooltip("start-2对象上的CanvasGroup。")]
    [SerializeField]
    private CanvasGroup hoverDecoration;

    [Tooltip("当前对象上的Button组件。")]
    [SerializeField]
    private Button targetButton;

    [Header("文字颜色")]

    [Tooltip("鼠标未悬停时的文字颜色。")]
    [SerializeField]
    private Color normalTextColor = Color.white;

    [Tooltip("鼠标悬停时的文字颜色。")]
    [SerializeField]
    private Color hoverTextColor = new Color(1f, 0.75f, 0.35f, 1f);

    [Tooltip("按钮不可使用时的文字颜色。")]
    [SerializeField]
    private Color disabledTextColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("悬停动画")]

    [Min(0.01f)]
    [SerializeField]
    private float hoverDuration = 0.12f;

    [Header("按下动画")]

    [Tooltip("按下时VisualRoot缩放比例。")]
    [Range(0.5f, 1f)]
    [SerializeField]
    private float pressedScale = 0.92f;

    [Min(0.01f)]
    [SerializeField]
    private float pressedDuration = 0.06f;

    [Tooltip("松开后恢复缩放所需时间。")]
    [Min(0.01f)]
    [SerializeField]
    private float releaseDuration = 0.09f;

    private Vector3 baseScale;

    private bool isHovered;
    private bool isPressed;

    private Coroutine colorCoroutine;
    private Coroutine decorationCoroutine;
    private Coroutine scaleCoroutine;

    private void Awake()
    {
        if (visualRoot == null)
        {
            Debug.LogError(
                nameof(BattleStartButtonUIView) +
                " 缺少 Visual Root 引用。",
                this
            );

            enabled = false;
            return;
        }

        if (labelText == null)
        {
            Debug.LogError(
                nameof(BattleStartButtonUIView) +
                " 缺少 Label Text 引用。",
                this
            );

            enabled = false;
            return;
        }

        if (hoverDecoration == null)
        {
            Debug.LogError(
                nameof(BattleStartButtonUIView) +
                " 缺少 Hover Decoration 引用。",
                this
            );

            enabled = false;
            return;
        }

        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();
        }

        baseScale = visualRoot.localScale;

        ApplyImmediateState();
    }

    private void OnEnable()
    {
        if (visualRoot == null || labelText == null || hoverDecoration == null)
        {
            return;
        }

        ApplyImmediateState();
    }

    private void OnDisable()
    {
        StopAllVisualCoroutines();

        isHovered = false;
        isPressed = false;

        if (visualRoot != null)
        {
            visualRoot.localScale = baseScale;
        }

        if (labelText != null)
        {
            labelText.color = normalTextColor;
        }

        if (hoverDecoration != null)
        {
            hoverDecoration.alpha = 0f;
        }
    }

    private void Update()
    {
        // 战斗结束或回合结算期间，Button可能会被其他脚本禁用。
        // 禁用后自动恢复为非悬停状态，避免装饰图残留。
        if (!IsButtonInteractable() && (isHovered || isPressed))
        {
            isHovered = false;
            isPressed = false;

            RefreshVisualState();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsButtonInteractable())
        {
            return;
        }

        isHovered = true;
        RefreshVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;

        RefreshVisualState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (!IsButtonInteractable())
        {
            return;
        }

        isPressed = true;
        RefreshScale();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        isPressed = false;
        RefreshScale();
    }

    private void RefreshVisualState()
    {
        RefreshColor();
        RefreshDecoration();
        RefreshScale();
    }

    private void RefreshColor()
    {
        Color targetColor;

        if (!IsButtonInteractable())
        {
            targetColor = disabledTextColor;
        }
        else if (isHovered)
        {
            targetColor = hoverTextColor;
        }
        else
        {
            targetColor = normalTextColor;
        }

        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
        }

        colorCoroutine = StartCoroutine(
            AnimateTextColor(targetColor, hoverDuration)
        );
    }

    private void RefreshDecoration()
    {
        float targetAlpha =
            IsButtonInteractable() && isHovered
                ? 1f
                : 0f;

        if (decorationCoroutine != null)
        {
            StopCoroutine(decorationCoroutine);
        }

        decorationCoroutine = StartCoroutine(
            AnimateDecorationAlpha(targetAlpha, hoverDuration)
        );
    }

    private void RefreshScale()
    {
        Vector3 targetScale = isPressed && IsButtonInteractable()
            ? MultiplyScale(baseScale, pressedScale)
            : baseScale;

        float duration = isPressed
            ? pressedDuration
            : releaseDuration;

        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }

        scaleCoroutine = StartCoroutine(
            AnimateScale(targetScale, duration)
        );
    }

    private IEnumerator AnimateTextColor(
        Color targetColor,
        float duration
    )
    {
        Color startColor = labelText.color;
        float elapsed = 0f;

        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = SmoothStep(progress);

            labelText.color = Color.Lerp(
                startColor,
                targetColor,
                easedProgress
            );

            yield return null;
        }

        labelText.color = targetColor;
        colorCoroutine = null;
    }

    private IEnumerator AnimateDecorationAlpha(
        float targetAlpha,
        float duration
    )
    {
        float startAlpha = hoverDecoration.alpha;
        float elapsed = 0f;

        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = SmoothStep(progress);

            hoverDecoration.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                easedProgress
            );

            yield return null;
        }

        hoverDecoration.alpha = targetAlpha;
        decorationCoroutine = null;
    }

    private IEnumerator AnimateScale(
        Vector3 targetScale,
        float duration
    )
    {
        Vector3 startScale = visualRoot.localScale;
        float elapsed = 0f;

        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = SmoothStep(progress);

            visualRoot.localScale = Vector3.Lerp(
                startScale,
                targetScale,
                easedProgress
            );

            yield return null;
        }

        visualRoot.localScale = targetScale;
        scaleCoroutine = null;
    }

    private void ApplyImmediateState()
    {
        isHovered = false;
        isPressed = false;

        visualRoot.localScale = baseScale;

        labelText.color = IsButtonInteractable()
            ? normalTextColor
            : disabledTextColor;

        hoverDecoration.alpha = 0f;
        hoverDecoration.interactable = false;
        hoverDecoration.blocksRaycasts = false;
    }

    private bool IsButtonInteractable()
    {
        return targetButton == null || targetButton.interactable;
    }

    private void StopAllVisualCoroutines()
    {
        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
            colorCoroutine = null;
        }

        if (decorationCoroutine != null)
        {
            StopCoroutine(decorationCoroutine);
            decorationCoroutine = null;
        }

        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
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
}