using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleBezierRelationLineUIView : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Image segmentTemplate;
    [SerializeField] private Image arrowImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("可染色素材")]
    [SerializeField] private Sprite lineSegmentSprite;
    [SerializeField] private Sprite arrowSprite;

    [Header("曲线采样")]
    [SerializeField, Min(8)] private int curveSampleCount = 48;
    [SerializeField] private Vector2 segmentSize = new Vector2(12f, 4f);
    [SerializeField] private Vector2 arrowSize = new Vector2(18f, 12f);
    [SerializeField, Min(0f)] private float dashedGap = 8f;
    [SerializeField, Min(0f)] private float solidOverlap = 1f;
    [SerializeField, Min(0f)] private float arrowInset = 4f;

    [Header("显示")]
    [SerializeField, Range(0f, 1f)] private float normalAlpha = 0.75f;
    [SerializeField, Range(0f, 1f)] private float highlightAlpha = 1f;
    [SerializeField, Min(1f)] private float highlightScale = 1.12f;
    [SerializeField] private bool useUnderlay = true;
    [SerializeField] private Color underlayColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);
    [SerializeField, Min(1f)] private float underlayScale = 1.35f;

    private readonly List<Image> segmentPool = new List<Image>();
    private readonly List<Image> underlaySegmentPool = new List<Image>();
    private Vector2[] sampledPoints;
    private float[] cumulativeLengths;
    private Image underlayArrow;
    private int activeSegmentCount;
    private Vector2 lastStart;
    private Vector2 lastControl;
    private Vector2 lastEnd;
    private Color lastColor;
    private bool lastDashed;
    private bool lastHighlighted;
    private float lastStep;
    private float totalLength;

    public int SegmentPoolCount => segmentPool.Count;
    public int ActiveSegmentCount => activeSegmentCount;
    public Vector2 StartPoint => lastStart;
    public Vector2 ControlPoint => lastControl;
    public Vector2 ArrowTip => lastEnd;
    public float ArrowAngle => arrowImage != null
        ? arrowImage.rectTransform.localEulerAngles.z
        : 0f;
    public bool IsDashed => lastDashed;
    public bool IsHighlighted => lastHighlighted;
    public Color LastColor => lastColor;
    public float TotalLength => totalLength;
    public float SegmentStep => lastStep;
    public bool IsVisible => canvasGroup != null
        ? canvasGroup.alpha > 0f
        : gameObject.activeSelf;
    public bool IsUnderlayVisible => useUnderlay && underlayArrow != null &&
        underlayArrow.gameObject.activeSelf;
    public Vector2 MainSegmentSize => segmentSize *
        (lastHighlighted ? highlightScale : 1f);
    public Vector2 UnderlaySegmentSize => MainSegmentSize * underlayScale;
    public bool AllRenderedImagesIgnoreRaycasts
    {
        get
        {
            if ((arrowImage != null && arrowImage.raycastTarget) ||
                (underlayArrow != null && underlayArrow.raycastTarget))
            {
                return false;
            }
            for (int index = 0; index < segmentPool.Count; index++)
            {
                if (segmentPool[index].raycastTarget ||
                    underlaySegmentPool[index].raycastTarget)
                {
                    return false;
                }
            }
            return true;
        }
    }

    private void Awake()
    {
        EnsureInitialized();
        SetVisible(false);
    }

    public void Render(
        Vector2 start,
        Vector2 control,
        Vector2 end,
        Color color,
        bool dashed,
        bool highlighted
    )
    {
        EnsureInitialized();
        if (segmentTemplate == null || arrowImage == null)
        {
            Clear();
            return;
        }

        lastStart = start;
        lastControl = control;
        lastEnd = end;
        lastColor = color;
        lastDashed = dashed;
        lastHighlighted = highlighted;

        BuildArcLengthTable(start, control, end);
        float segmentLength = Mathf.Max(1f, segmentSize.x);
        lastStep = dashed
            ? segmentLength + dashedGap
            : Mathf.Max(1f, segmentLength - solidOverlap);
        float usableLength = Mathf.Max(
            0f,
            totalLength - arrowInset - arrowSize.x
        );
        int requiredSegments = usableLength <= 0f
            ? 0
            : Mathf.FloorToInt(usableLength / lastStep) + 1;

        EnsureSegmentCapacity(requiredSegments);
        HideUnusedSegments(requiredSegments);

        float alpha = highlighted ? highlightAlpha : normalAlpha;
        Color mainColor = color;
        mainColor.a *= alpha;
        Color resolvedUnderlayColor = underlayColor;
        resolvedUnderlayColor.a *= alpha;
        float resolvedScale = highlighted ? highlightScale : 1f;

        for (int index = 0; index < requiredSegments; index++)
        {
            float distance = Mathf.Min(index * lastStep, usableLength);
            Vector2 position;
            Vector2 tangent;
            GetPointAndTangentAtDistance(distance, out position, out tangent);
            ConfigureSegment(
                underlaySegmentPool[index],
                position,
                tangent,
                segmentSize * resolvedScale * underlayScale,
                resolvedUnderlayColor,
                useUnderlay
            );
            ConfigureSegment(
                segmentPool[index],
                position,
                tangent,
                segmentSize * resolvedScale,
                mainColor,
                true
            );
        }

        activeSegmentCount = requiredSegments;
        ConfigureArrow(
            underlayArrow,
            end,
            GetEndTangent(),
            arrowSize * resolvedScale * underlayScale,
            resolvedUnderlayColor,
            useUnderlay
        );
        ConfigureArrow(
            arrowImage,
            end,
            GetEndTangent(),
            arrowSize * resolvedScale,
            mainColor,
            true
        );
        SetVisible(true);
    }

    public void Clear()
    {
        activeSegmentCount = 0;
        HideUnusedSegments(0);
        if (arrowImage != null)
        {
            arrowImage.gameObject.SetActive(false);
        }
        if (underlayArrow != null)
        {
            underlayArrow.gameObject.SetActive(false);
        }
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public Vector2 GetSegmentPosition(int index)
    {
        return index >= 0 && index < activeSegmentCount
            ? segmentPool[index].rectTransform.anchoredPosition
            : Vector2.zero;
    }

    public bool ValidateConfiguration(string label)
    {
        bool valid = true;
        if (segmentTemplate == null)
        {
            Debug.LogWarning(label + " 缺少 Segment Template。", this);
            valid = false;
        }
        if (arrowImage == null)
        {
            Debug.LogWarning(label + " 缺少 Arrow Image。", this);
            valid = false;
        }
        if (lineSegmentSprite == null)
        {
            Debug.LogWarning(label + " 缺少 Line Segment Sprite。", this);
            valid = false;
        }
        if (arrowSprite == null)
        {
            Debug.LogWarning(label + " 缺少 Arrow Sprite。", this);
            valid = false;
        }
        return valid;
    }

    internal void ConfigureForTesting(
        Image testSegmentTemplate,
        Image testArrow,
        CanvasGroup testCanvasGroup
    )
    {
        segmentTemplate = testSegmentTemplate;
        arrowImage = testArrow;
        canvasGroup = testCanvasGroup;
        lineSegmentSprite = testSegmentTemplate != null
            ? testSegmentTemplate.sprite
            : null;
        arrowSprite = testArrow != null ? testArrow.sprite : null;
        EnsureInitialized();
    }

    internal void ConfigureGeometryForTesting(
        Vector2 testSegmentSize,
        float testDashedGap,
        float testSolidOverlap,
        bool testUseUnderlay
    )
    {
        segmentSize = testSegmentSize;
        dashedGap = testDashedGap;
        solidOverlap = testSolidOverlap;
        useUnderlay = testUseUnderlay;
    }

    public static Vector2 EvaluateQuadraticBezier(
        Vector2 start,
        Vector2 control,
        Vector2 end,
        float t
    )
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * start +
            2f * oneMinusT * t * control +
            t * t * end;
    }

    public static Vector2 ResolveControlPoint(
        Vector2 start,
        Vector2 end,
        float baseCurveHeight,
        float distanceCurveFactor,
        float minCurveHeight,
        float maxCurveHeight,
        float laneOffset
    )
    {
        float resolvedHeight = Mathf.Clamp(
            baseCurveHeight +
                Mathf.Abs(end.x - start.x) * distanceCurveFactor +
                laneOffset,
            minCurveHeight,
            maxCurveHeight + Mathf.Max(0f, laneOffset)
        );
        return new Vector2(
            (start.x + end.x) * 0.5f,
            Mathf.Max(start.y, end.y) + resolvedHeight
        );
    }

    private void EnsureInitialized()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        if (segmentTemplate != null)
        {
            segmentTemplate.raycastTarget = false;
            segmentTemplate.gameObject.SetActive(false);
        }
        if (arrowImage != null)
        {
            arrowImage.raycastTarget = false;
            arrowImage.rectTransform.pivot = new Vector2(1f, 0.5f);
            arrowImage.gameObject.SetActive(false);
        }
        EnsureSampleArrays();
        EnsureUnderlayArrow();
    }

    private void EnsureSampleArrays()
    {
        int sampleCount = Mathf.Max(8, curveSampleCount);
        if (sampledPoints == null || sampledPoints.Length != sampleCount + 1)
        {
            sampledPoints = new Vector2[sampleCount + 1];
            cumulativeLengths = new float[sampleCount + 1];
        }
    }

    private void EnsureUnderlayArrow()
    {
        if (underlayArrow != null || arrowImage == null)
        {
            return;
        }
        underlayArrow = Instantiate(arrowImage, arrowImage.transform.parent);
        underlayArrow.name = arrowImage.name + "_Underlay";
        underlayArrow.transform.SetSiblingIndex(
            Mathf.Max(0, arrowImage.transform.GetSiblingIndex())
        );
        arrowImage.transform.SetAsLastSibling();
        underlayArrow.raycastTarget = false;
        underlayArrow.gameObject.SetActive(false);
    }

    private void BuildArcLengthTable(
        Vector2 start,
        Vector2 control,
        Vector2 end
    )
    {
        EnsureSampleArrays();
        int sampleCount = sampledPoints.Length - 1;
        sampledPoints[0] = start;
        cumulativeLengths[0] = 0f;
        totalLength = 0f;
        for (int index = 1; index <= sampleCount; index++)
        {
            float t = index / (float)sampleCount;
            sampledPoints[index] = EvaluateQuadraticBezier(
                start,
                control,
                end,
                t
            );
            totalLength += Vector2.Distance(
                sampledPoints[index - 1],
                sampledPoints[index]
            );
            cumulativeLengths[index] = totalLength;
        }
    }

    private void EnsureSegmentCapacity(int required)
    {
        while (segmentPool.Count < required)
        {
            Image underlay = Instantiate(segmentTemplate, transform);
            underlay.name = "UnderlaySegment_" + underlaySegmentPool.Count;
            underlay.raycastTarget = false;
            underlay.gameObject.SetActive(false);
            underlay.transform.SetAsFirstSibling();
            underlaySegmentPool.Add(underlay);

            Image segment = Instantiate(segmentTemplate, transform);
            segment.name = "Segment_" + segmentPool.Count;
            segment.raycastTarget = false;
            segment.gameObject.SetActive(false);
            segmentPool.Add(segment);
        }
    }

    private void HideUnusedSegments(int usedCount)
    {
        for (int index = usedCount; index < segmentPool.Count; index++)
        {
            segmentPool[index].gameObject.SetActive(false);
            underlaySegmentPool[index].gameObject.SetActive(false);
        }
    }

    private void ConfigureSegment(
        Image image,
        Vector2 position,
        Vector2 tangent,
        Vector2 size,
        Color color,
        bool visible
    )
    {
        image.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }
        image.sprite = lineSegmentSprite;
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.Euler(0f, 0f, GetAngle(tangent));
    }

    private void ConfigureArrow(
        Image image,
        Vector2 tip,
        Vector2 tangent,
        Vector2 size,
        Color color,
        bool visible
    )
    {
        if (image == null)
        {
            return;
        }
        image.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }
        image.sprite = arrowSprite;
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = tip;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.Euler(0f, 0f, GetAngle(tangent));
    }

    private void GetPointAndTangentAtDistance(
        float distance,
        out Vector2 position,
        out Vector2 tangent
    )
    {
        if (distance <= 0f || totalLength <= 0f)
        {
            position = sampledPoints[0];
            tangent = sampledPoints[1] - sampledPoints[0];
            return;
        }
        int upper = 1;
        while (upper < cumulativeLengths.Length - 1 &&
               cumulativeLengths[upper] < distance)
        {
            upper++;
        }
        int lower = upper - 1;
        float span = cumulativeLengths[upper] - cumulativeLengths[lower];
        float ratio = span > 0.0001f
            ? (distance - cumulativeLengths[lower]) / span
            : 0f;
        position = Vector2.Lerp(
            sampledPoints[lower],
            sampledPoints[upper],
            ratio
        );
        tangent = sampledPoints[upper] - sampledPoints[lower];
    }

    private Vector2 GetEndTangent()
    {
        int last = sampledPoints != null ? sampledPoints.Length - 1 : 0;
        return last > 0
            ? sampledPoints[last] - sampledPoints[last - 1]
            : Vector2.right;
    }

    private static float GetAngle(Vector2 tangent)
    {
        return tangent.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg
            : 0f;
    }
}
