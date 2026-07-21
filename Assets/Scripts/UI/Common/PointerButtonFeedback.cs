using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PointerButtonFeedback : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [SerializeField] private Selectable selectable;
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private Graphic colorTarget;
    [SerializeField] private bool enableHoverColor = true;
    [SerializeField] private Color hoverColor = new Color32(201, 150, 74, 255);
    [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.9f;
    [SerializeField, Min(0.01f)] private float responseSpeed = 14f;

    private bool isPointerInside;
    private bool isPointerPressed;
    private Vector3 normalScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;
    private Color normalColor = Color.white;
    private Color targetColor = Color.white;
    private bool hasCachedInitialState;

    void Awake()
    {
        if (selectable == null)
        {
            selectable = GetComponent<Selectable>();
        }

        CacheInitialState();
        RestoreDefaultVisualImmediate();
    }

    void OnEnable()
    {
        ResetPointerState();
        RestoreDefaultVisualImmediate();
    }

    void OnDisable()
    {
        ResetPointerState();
        RestoreDefaultVisualImmediate();
    }

    void Update()
    {
        if (!HasScaleTarget("Update"))
        {
            return;
        }

        if (!IsSelectableInteractable())
        {
            SetTargetToDefault();
        }

        float interpolationFactor = 1f - Mathf.Exp(-responseSpeed * Time.unscaledDeltaTime);

        scaleTarget.localScale = Vector3.Lerp(
            scaleTarget.localScale,
            targetScale,
            interpolationFactor
        );

        if (IsScaleCloseEnough(scaleTarget.localScale, targetScale))
        {
            scaleTarget.localScale = targetScale;
        }

        if (enableHoverColor && colorTarget != null)
        {
            colorTarget.color = Color.Lerp(
                colorTarget.color,
                targetColor,
                interpolationFactor
            );

            if (IsColorCloseEnough(colorTarget.color, targetColor))
            {
                colorTarget.color = targetColor;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;

        if (!IsSelectableInteractable())
        {
            SetTargetToDefault();
            return;
        }

        if (enableHoverColor && colorTarget != null)
        {
            targetColor = hoverColor;
        }

        if (isPointerPressed)
        {
            targetScale = normalScale * pressedScale;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        targetScale = normalScale;
        targetColor = normalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (!IsSelectableInteractable())
        {
            SetTargetToDefault();
            return;
        }

        isPointerPressed = true;

        if (isPointerInside)
        {
            targetScale = normalScale * pressedScale;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        isPointerPressed = false;
        targetScale = normalScale;

        if (isPointerInside && IsSelectableInteractable() && enableHoverColor && colorTarget != null)
        {
            targetColor = hoverColor;
        }
        else
        {
            targetColor = normalColor;
        }
    }

    void CacheInitialState()
    {
        if (hasCachedInitialState)
        {
            return;
        }

        if (scaleTarget != null)
        {
            normalScale = scaleTarget.localScale;
            targetScale = normalScale;
        }
        else
        {
            Debug.LogError("PointerButtonFeedback 初始化失败：scaleTarget 未绑定。");
        }

        if (colorTarget != null)
        {
            normalColor = colorTarget.color;
            targetColor = normalColor;
        }

        hasCachedInitialState = true;
    }

    void ResetPointerState()
    {
        isPointerInside = false;
        isPointerPressed = false;
    }

    void RestoreDefaultVisualImmediate()
    {
        CacheInitialState();
        SetTargetToDefault();

        if (scaleTarget != null)
        {
            scaleTarget.localScale = normalScale;
        }

        if (enableHoverColor && colorTarget != null)
        {
            colorTarget.color = normalColor;
        }
    }

    void SetTargetToDefault()
    {
        targetScale = normalScale;
        targetColor = normalColor;
    }

    bool IsSelectableInteractable()
    {
        return selectable == null || selectable.interactable;
    }

    bool HasScaleTarget(string callerName)
    {
        if (scaleTarget != null)
        {
            return true;
        }

        Debug.LogError(callerName + " 失败：scaleTarget 未绑定。");
        return false;
    }

    bool IsScaleCloseEnough(Vector3 current, Vector3 target)
    {
        return (current - target).sqrMagnitude <= 0.000001f;
    }

    bool IsColorCloseEnough(Color current, Color target)
    {
        float difference =
            Mathf.Abs(current.r - target.r) +
            Mathf.Abs(current.g - target.g) +
            Mathf.Abs(current.b - target.b) +
            Mathf.Abs(current.a - target.a);

        return difference <= 0.001f;
    }
}
