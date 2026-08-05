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
    private float lastRangeStart;
    private float lastRangeEnd = 1f;
    private Vector2 lastSourceStart;
    private Vector2 lastSourceControl;
    private Vector2 lastSourceEnd;
    private bool lastActivationRaycastSafe = true;
    private float lastStep;
    private float totalLength;
    private bool isDestroying;
    private bool hasRendered;
    private bool hasPreviousRender;
    private bool previousDashed;
    private float previousRangeStart;
    private float previousRangeEnd = 1f;

    public int SegmentPoolCount => segmentPool.Count;
    public int MainSegmentPoolCount => segmentPool.Count;
    public int UnderlaySegmentPoolCount => underlaySegmentPool.Count;
    public int HierarchyMainSegmentCount => CountHierarchySegments(false, false);
    public int HierarchyUnderlaySegmentCount => CountHierarchySegments(true, false);
    public int UnregisteredMainSegmentCount => CountHierarchySegments(false, true);
    public int UnregisteredUnderlaySegmentCount =>
        CountHierarchySegments(true, true);
    internal bool HasUnderlayArrow => underlayArrow != null;
    public int ActiveSegmentCount => activeSegmentCount;
    public int ActiveMainSegmentCount => CountActiveSegments(segmentPool);
    public int ActiveUnderlaySegmentCount =>
        CountActiveSegments(underlaySegmentPool);
    public bool HasVisibleMainArrow => arrowImage != null &&
        arrowImage.gameObject.activeSelf && arrowImage.color.a > 0f;
    public bool HasVisibleUnderlayArrow => underlayArrow != null &&
        underlayArrow.gameObject.activeSelf && underlayArrow.color.a > 0f;
    public string CurrentLineStyle => hasRendered
        ? GetLineStyleName(lastDashed)
        : "None";
    public string PreviousLineStyle => hasPreviousRender
        ? GetLineStyleName(previousDashed)
        : "None";
    public Vector2 CurrentRange => new Vector2(lastRangeStart, lastRangeEnd);
    public Vector2 PreviousRange => new Vector2(
        previousRangeStart,
        previousRangeEnd
    );
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
    public float RangeStart => lastRangeStart;
    public float RangeEnd => lastRangeEnd;
    public Vector2 SourceCurveStart => lastSourceStart;
    public Vector2 SourceCurveControlPoint => lastSourceControl;
    public Vector2 SourceCurveEnd => lastSourceEnd;
    public bool LastActivationRaycastSafe => lastActivationRaycastSafe;
    public bool CanvasGroupIgnoresRaycasts => canvasGroup != null &&
        !canvasGroup.interactable && !canvasGroup.blocksRaycasts;
    public bool ArrowImagesIgnoreRaycasts =>
        (arrowImage == null || !arrowImage.raycastTarget) &&
        (underlayArrow == null || !underlayArrow.raycastTarget);
    public bool UnderlayImagesIgnoreRaycasts
    {
        get
        {
            if (underlayArrow != null && underlayArrow.raycastTarget)
            {
                return false;
            }
            for (int index = 0;
                 index < underlaySegmentPool.Count;
                 index++)
            {
                if (underlaySegmentPool[index] != null &&
                    underlaySegmentPool[index].raycastTarget)
                {
                    return false;
                }
            }
            return true;
        }
    }
    public bool IsVisible => canvasGroup != null
        ? canvasGroup.alpha > 0f
        : gameObject.activeSelf;
    public bool IsUnderlayVisible => useUnderlay && underlayArrow != null &&
        underlayArrow.gameObject.activeSelf;
    public bool ArrowActiveSelf => arrowImage != null &&
        arrowImage.gameObject.activeSelf;
    public float ArrowAlpha => arrowImage != null ? arrowImage.color.a : 0f;
    public Vector2 ArrowRenderedSize => arrowImage != null
        ? arrowImage.rectTransform.sizeDelta
        : Vector2.zero;
    public int ArrowSiblingIndex => arrowImage != null
        ? arrowImage.transform.GetSiblingIndex()
        : -1;
    public int ArrowInstanceID => arrowImage != null
        ? arrowImage.GetInstanceID()
        : 0;
    public int SegmentTemplateInstanceID => segmentTemplate != null
        ? segmentTemplate.GetInstanceID()
        : 0;
    public int UnderlayArrowInstanceID => underlayArrow != null
        ? underlayArrow.GetInstanceID()
        : 0;
    public int CanvasGroupInstanceID => canvasGroup != null
        ? canvasGroup.GetInstanceID()
        : 0;
    public bool HasDeterministicVisualLayerOrder
    {
        get
        {
            if (arrowImage == null)
            {
                return false;
            }

            int mainArrowIndex = arrowImage.transform.GetSiblingIndex();
            int lowestMainIndex = mainArrowIndex;
            for (int index = 0; index < segmentPool.Count; index++)
            {
                if (segmentPool[index] == null)
                {
                    continue;
                }
                int segmentIndex =
                    segmentPool[index].transform.GetSiblingIndex();
                lowestMainIndex = Mathf.Min(lowestMainIndex, segmentIndex);
                if (segmentIndex >= mainArrowIndex)
                {
                    return false;
                }
            }

            for (int index = 0; index < underlaySegmentPool.Count; index++)
            {
                if (underlaySegmentPool[index] != null &&
                    underlaySegmentPool[index].transform.GetSiblingIndex() >=
                    lowestMainIndex)
                {
                    return false;
                }
            }
            return underlayArrow == null ||
                underlayArrow.transform.GetSiblingIndex() < lowestMainIndex;
        }
    }
    public Vector2 MainSegmentSize => segmentSize *
        (lastHighlighted ? highlightScale : 1f);
    public Vector2 UnderlaySegmentSize => MainSegmentSize * underlayScale;
    public bool AllRenderedImagesIgnoreRaycasts
    {
        get
        {
            if ((segmentTemplate != null && segmentTemplate.raycastTarget) ||
                (arrowImage != null && arrowImage.raycastTarget) ||
                (underlayArrow != null && underlayArrow.raycastTarget))
            {
                return false;
            }
            for (int index = 0; index < segmentPool.Count; index++)
            {
                if (segmentPool[index] != null &&
                    segmentPool[index].raycastTarget)
                {
                    return false;
                }
            }
            for (int index = 0; index < underlaySegmentPool.Count; index++)
            {
                if (underlaySegmentPool[index] != null &&
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
        isDestroying = false;
        EnsureInitialized();
        SetVisible(false);
    }

    private void OnEnable()
    {
        isDestroying = false;
        EnsureInitialized();
    }

    private void OnDisable()
    {
        Clear();
    }

    private void OnDestroy()
    {
        isDestroying = true;
        Clear();
        segmentPool.Clear();
        underlaySegmentPool.Clear();
        underlayArrow = null;
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
        RenderRange(
            start,
            control,
            end,
            0f,
            1f,
            color,
            dashed,
            highlighted
        );
    }

    public void RenderRange(
        Vector2 sourceStart,
        Vector2 sourceControl,
        Vector2 sourceEnd,
        float rangeStart,
        float rangeEnd,
        Color color,
        bool dashed,
        bool highlighted
    )
    {
        if (isDestroying)
        {
            return;
        }
        EnsureInitialized();
        if (segmentTemplate == null || arrowImage == null)
        {
            Clear();
            return;
        }

        rangeStart = Mathf.Clamp01(rangeStart);
        rangeEnd = Mathf.Clamp01(rangeEnd);
        Vector2 start;
        Vector2 control;
        Vector2 end;
        ResolveQuadraticSubcurve(
            sourceStart,
            sourceControl,
            sourceEnd,
            rangeStart,
            rangeEnd,
            out start,
            out control,
            out end
        );

        if (hasRendered)
        {
            previousDashed = lastDashed;
            previousRangeStart = lastRangeStart;
            previousRangeEnd = lastRangeEnd;
            hasPreviousRender = true;
        }

        lastStart = start;
        lastControl = control;
        lastEnd = end;
        lastSourceStart = sourceStart;
        lastSourceControl = sourceControl;
        lastSourceEnd = sourceEnd;
        lastRangeStart = rangeStart;
        lastRangeEnd = rangeEnd;
        lastColor = color;
        lastDashed = dashed;
        lastHighlighted = highlighted;
        hasRendered = true;
        lastActivationRaycastSafe = true;

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
        ApplyVisualSiblingOrder();
        // 每次渲染先关闭两个池的完整可见集合，再只激活本次需要的N项。
        // 这样完整虚线切换为半段实线时，池尾和旧Underlay都不会残留。
        DisableUnusedSegments(segmentPool, 0);
        DisableUnusedSegments(underlaySegmentPool, 0);

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
        DisableUnusedSegments(segmentPool, requiredSegments);
        DisableUnusedSegments(underlaySegmentPool, requiredSegments);
        SetVisible(true);
    }

    public void Clear()
    {
        PruneDestroyedImageReferences();
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
            && index < segmentPool.Count && segmentPool[index] != null
            ? segmentPool[index].rectTransform.anchoredPosition
            : Vector2.zero;
    }

    public int GetActiveSegmentInstanceID(int index)
    {
        return index >= 0 && index < activeSegmentCount &&
            index < segmentPool.Count && segmentPool[index] != null
            ? segmentPool[index].GetInstanceID()
            : 0;
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

    internal void CollectUnregisteredSegmentImages(List<Image> output)
    {
        if (output == null)
        {
            return;
        }

        Image[] hierarchyImages = GetComponentsInChildren<Image>(true);
        for (int index = 0; index < hierarchyImages.Length; index++)
        {
            Image image = hierarchyImages[index];
            if (image == null || !IsRuntimeSegmentName(image.gameObject.name))
            {
                continue;
            }

            bool underlay = IsUnderlaySegmentName(image.gameObject.name);
            if (!(underlay
                    ? underlaySegmentPool.Contains(image)
                    : segmentPool.Contains(image)))
            {
                output.Add(image);
            }
        }
    }

    internal bool IsRegisteredMainSegment(Image image)
    {
        return image != null && segmentPool.Contains(image);
    }

    internal bool IsRegisteredUnderlaySegment(Image image)
    {
        return image != null && underlaySegmentPool.Contains(image);
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

    internal void SetRenderedRaycastTargetsForTesting(bool enabled)
    {
        if (arrowImage != null)
        {
            arrowImage.raycastTarget = enabled;
        }
        if (underlayArrow != null)
        {
            underlayArrow.raycastTarget = enabled;
        }
        for (int index = 0; index < segmentPool.Count; index++)
        {
            if (segmentPool[index] != null)
            {
                segmentPool[index].raycastTarget = enabled;
            }
        }
        for (int index = 0; index < underlaySegmentPool.Count; index++)
        {
            if (underlaySegmentPool[index] != null)
            {
                underlaySegmentPool[index].raycastTarget = enabled;
            }
        }
    }

    public void ApplyVisualSettings(
        Vector2 resolvedSegmentSize,
        Vector2 resolvedArrowSize,
        float resolvedDashedGap,
        float resolvedSolidOverlap,
        float resolvedUnderlayScale,
        float arrowScale
    )
    {
        if (isDestroying)
        {
            return;
        }
        EnsureInitialized();
        segmentSize = resolvedSegmentSize;
        arrowSize = resolvedArrowSize * Mathf.Max(0f, arrowScale);
        dashedGap = Mathf.Max(0f, resolvedDashedGap);
        solidOverlap = Mathf.Max(0f, resolvedSolidOverlap);
        underlayScale = Mathf.Max(1f, resolvedUnderlayScale);
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

    public static Vector2 EvaluateQuadraticTangent(
        Vector2 start,
        Vector2 control,
        Vector2 end,
        float t
    )
    {
        return 2f * ((1f - t) * (control - start) +
            t * (end - control));
    }

    public static void ResolveCenteredGapParameters(
        Vector2 start,
        Vector2 control,
        Vector2 end,
        float gap,
        out float leftT,
        out float rightT
    )
    {
        float targetGap = Mathf.Max(0f, gap);
        float low = 0f;
        float high = 0.5f;
        for (int index = 0; index < 24; index++)
        {
            float halfSpan = (low + high) * 0.5f;
            float testLeft = 0.5f - halfSpan;
            float testRight = 0.5f + halfSpan;
            float actualGap = Vector2.Distance(
                EvaluateQuadraticBezier(start, control, end, testLeft),
                EvaluateQuadraticBezier(start, control, end, testRight)
            );
            if (actualGap < targetGap)
            {
                low = halfSpan;
            }
            else
            {
                high = halfSpan;
            }
        }
        float resolvedHalfSpan = (low + high) * 0.5f;
        leftT = 0.5f - resolvedHalfSpan;
        rightT = 0.5f + resolvedHalfSpan;
    }

    public static void ResolveQuadraticSubcurve(
        Vector2 sourceStart,
        Vector2 sourceControl,
        Vector2 sourceEnd,
        float rangeStart,
        float rangeEnd,
        out Vector2 start,
        out Vector2 control,
        out Vector2 end
    )
    {
        start = EvaluateQuadraticBezier(
            sourceStart,
            sourceControl,
            sourceEnd,
            rangeStart
        );
        end = EvaluateQuadraticBezier(
            sourceStart,
            sourceControl,
            sourceEnd,
            rangeEnd
        );
        Vector2 startTangent = EvaluateQuadraticTangent(
            sourceStart,
            sourceControl,
            sourceEnd,
            rangeStart
        );
        control = start +
            startTangent * ((rangeEnd - rangeStart) * 0.5f);
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
        if (isDestroying)
        {
            return;
        }
        PruneDestroyedImageReferences();
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
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
        EnsurePoolRaycastSafety();
        EnsureSampleArrays();
        EnsureUnderlayArrow();
    }

    private void EnsurePoolRaycastSafety()
    {
        for (int index = 0; index < segmentPool.Count; index++)
        {
            if (segmentPool[index] != null)
            {
                segmentPool[index].raycastTarget = false;
            }
        }
        for (int index = 0; index < underlaySegmentPool.Count; index++)
        {
            if (underlaySegmentPool[index] != null)
            {
                underlaySegmentPool[index].raycastTarget = false;
            }
        }
        if (underlayArrow != null)
        {
            underlayArrow.raycastTarget = false;
        }
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
        if (isDestroying || underlayArrow != null || arrowImage == null)
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
        if (isDestroying || segmentTemplate == null)
        {
            return;
        }
        PruneDestroyedImageReferences();
        while (underlaySegmentPool.Count < required)
        {
            Image underlay = Instantiate(segmentTemplate, transform);
            underlay.name = "UnderlaySegment_" + underlaySegmentPool.Count;
            underlay.raycastTarget = false;
            underlay.gameObject.SetActive(false);
            underlay.transform.SetAsFirstSibling();
            underlaySegmentPool.Add(underlay);
        }
        while (segmentPool.Count < required)
        {
            Image segment = Instantiate(segmentTemplate, transform);
            segment.name = "Segment_" + segmentPool.Count;
            segment.raycastTarget = false;
            segment.gameObject.SetActive(false);
            segmentPool.Add(segment);
        }
    }

    private void ApplyVisualSiblingOrder()
    {
        // 每次扩容后重新固定层级，避免新线段追加到主箭头之上。
        int siblingIndex = 0;
        for (int index = 0; index < underlaySegmentPool.Count; index++)
        {
            if (underlaySegmentPool[index] != null)
            {
                underlaySegmentPool[index].transform.SetSiblingIndex(
                    siblingIndex++
                );
            }
        }
        if (underlayArrow != null)
        {
            underlayArrow.transform.SetSiblingIndex(siblingIndex++);
        }
        for (int index = 0; index < segmentPool.Count; index++)
        {
            if (segmentPool[index] != null)
            {
                segmentPool[index].transform.SetSiblingIndex(siblingIndex++);
            }
        }
        if (arrowImage != null)
        {
            arrowImage.transform.SetAsLastSibling();
        }
    }

    private void HideUnusedSegments(int usedCount)
    {
        PruneDestroyedImageReferences();
        DisableUnusedSegments(segmentPool, usedCount);
        DisableUnusedSegments(underlaySegmentPool, usedCount);
    }

    private static void DisableUnusedSegments(
        List<Image> pool,
        int usedCount
    )
    {
        if (pool == null)
        {
            return;
        }

        int firstUnusedIndex = Mathf.Clamp(usedCount, 0, pool.Count);
        for (int index = firstUnusedIndex; index < pool.Count; index++)
        {
            Image image = pool[index];
            if (image == null)
            {
                continue;
            }
            image.raycastTarget = false;
            image.gameObject.SetActive(false);
        }
    }

    private static int CountActiveSegments(List<Image> pool)
    {
        if (pool == null)
        {
            return 0;
        }

        int count = 0;
        for (int index = 0; index < pool.Count; index++)
        {
            if (pool[index] != null && pool[index].gameObject.activeSelf)
            {
                count++;
            }
        }
        return count;
    }

    private static string GetLineStyleName(bool dashed)
    {
        return dashed ? "Dashed" : "Solid";
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
        if (image == null)
        {
            return;
        }
        image.raycastTarget = false;
        image.gameObject.SetActive(false);
        if (!visible)
        {
            return;
        }
        image.sprite = lineSegmentSprite;
        image.color = color;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.Euler(0f, 0f, GetAngle(tangent));
        lastActivationRaycastSafe &= !image.raycastTarget;
        image.gameObject.SetActive(true);
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
        image.raycastTarget = false;
        image.gameObject.SetActive(false);
        if (!visible)
        {
            return;
        }
        image.sprite = arrowSprite;
        image.color = color;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = tip;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.Euler(0f, 0f, GetAngle(tangent));
        lastActivationRaycastSafe &= !image.raycastTarget;
        image.gameObject.SetActive(true);
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

    private void PruneDestroyedImageReferences()
    {
        for (int index = segmentPool.Count - 1; index >= 0; index--)
        {
            if (segmentPool[index] == null)
            {
                segmentPool.RemoveAt(index);
            }
        }
        for (int index = underlaySegmentPool.Count - 1;
             index >= 0;
             index--)
        {
            if (underlaySegmentPool[index] == null)
            {
                underlaySegmentPool.RemoveAt(index);
            }
        }
    }

    private int CountHierarchySegments(bool underlay, bool unregisteredOnly)
    {
        int count = 0;
        Image[] hierarchyImages = GetComponentsInChildren<Image>(true);
        for (int index = 0; index < hierarchyImages.Length; index++)
        {
            Image image = hierarchyImages[index];
            if (image == null ||
                IsUnderlaySegmentName(image.gameObject.name) != underlay ||
                !IsRuntimeSegmentName(image.gameObject.name))
            {
                continue;
            }

            bool registered = underlay
                ? underlaySegmentPool.Contains(image)
                : segmentPool.Contains(image);
            if (!unregisteredOnly || !registered)
            {
                count++;
            }
        }
        return count;
    }

    private static bool IsRuntimeSegmentName(string objectName)
    {
        return !string.IsNullOrEmpty(objectName) &&
            (objectName.StartsWith("Segment_") ||
             objectName.StartsWith("UnderlaySegment_"));
    }

    private static bool IsUnderlaySegmentName(string objectName)
    {
        return !string.IsNullOrEmpty(objectName) &&
            objectName.StartsWith("UnderlaySegment_");
    }
}
