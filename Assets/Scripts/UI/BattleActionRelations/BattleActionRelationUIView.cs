using UnityEngine;

public sealed class BattleActionRelationUIView : MonoBehaviour
{
    [SerializeField] private BattleBezierRelationLineUIView primaryCurve;
    [SerializeField] private BattleBezierRelationLineUIView secondaryCurve;
    [SerializeField] private CanvasGroup canvasGroup;

    public string RelationID { get; private set; }
    public BattleActionRelationKind Kind { get; private set; }
    public bool IsHighlighted { get; private set; }
    public BattleBezierRelationLineUIView PrimaryCurve => primaryCurve;
    public BattleBezierRelationLineUIView SecondaryCurve => secondaryCurve;
    public bool OwnsPrimaryCurve => OwnsCurve(primaryCurve);
    public bool OwnsSecondaryCurve => OwnsCurve(secondaryCurve);
    public int SiblingIndex => transform.GetSiblingIndex();
    public float UnilateralArrowEndpointOffset { get; private set; }
    public bool CanvasGroupIgnoresRaycasts => canvasGroup != null &&
        !canvasGroup.interactable && !canvasGroup.blocksRaycasts;
    private bool isDestroying;

    private void Awake()
    {
        isDestroying = false;
        EnsureRaycastSafety();
    }

    private void OnEnable()
    {
        isDestroying = false;
        EnsureRaycastSafety();
    }

    private void OnDestroy()
    {
        isDestroying = true;
        RelationID = string.Empty;
        IsHighlighted = false;
        if (OwnsPrimaryCurve)
        {
            primaryCurve.Clear();
        }
        if (OwnsSecondaryCurve)
        {
            secondaryCurve.Clear();
        }
    }

    public bool ShowUnilateral(
        BattleActionRelationDescriptor descriptor,
        Vector2 start,
        Vector2 end,
        Color color,
        bool highlighted,
        float baseCurveHeight,
        float distanceCurveFactor,
        float minCurveHeight,
        float maxCurveHeight,
        float laneSpacing,
        float arrowEndpointOffset = 0f
    )
    {
        EnsureRaycastSafety();
        if (descriptor == null || primaryCurve == null)
        {
            ClearView();
            return false;
        }

        ClearCurvesWhenDisplayTypeChanges(descriptor.Kind);
        // 对象池模板为 inactive；必须先激活完成 OnEnable 初始化，再绘制箭头。
        gameObject.SetActive(true);
        RelationID = descriptor.RelationID;
        Kind = descriptor.Kind;
        IsHighlighted = highlighted;
        Vector2 control =
            BattleBezierRelationLineUIView.ResolveControlPoint(
                start,
                end,
                baseCurveHeight,
                distanceCurveFactor,
                minCurveHeight,
                maxCurveHeight,
                descriptor.LaneIndex * laneSpacing
            );
        // 共享端点只做视觉避让，关系描述中的正式槽位ID保持不变。
        Vector2 visualEnd = ResolveVisualArrowEndpoint(
            control,
            end,
            arrowEndpointOffset
        );
        UnilateralArrowEndpointOffset = arrowEndpointOffset;
        primaryCurve.Render(
            start,
            control,
            visualEnd,
            color,
            true,
            highlighted
        );
        secondaryCurve?.Clear();
        return primaryCurve.IsVisible && primaryCurve.ArrowActiveSelf;
    }

    public bool ShowClash(
        BattleActionRelationDescriptor descriptor,
        Vector2 playerStart,
        Vector2 enemyStart,
        Color playerColor,
        Color enemyColor,
        bool highlighted,
        float baseCurveHeight,
        float distanceCurveFactor,
        float minCurveHeight,
        float maxCurveHeight,
        float laneSpacing,
        float clashArrowGap
    )
    {
        EnsureRaycastSafety();
        if (descriptor == null || primaryCurve == null ||
            secondaryCurve == null)
        {
            ClearView();
            return false;
        }

        ClearCurvesWhenDisplayTypeChanges(descriptor.Kind);
        // 同一 RelationView 的两条半曲线都在激活后绘制，避免 OnEnable 清掉箭头。
        gameObject.SetActive(true);
        RelationID = descriptor.RelationID;
        Kind = descriptor.Kind;
        IsHighlighted = highlighted;
        UnilateralArrowEndpointOffset = 0f;

        Vector2 sharedControl =
            BattleBezierRelationLineUIView.ResolveControlPoint(
                playerStart,
                enemyStart,
                baseCurveHeight,
                distanceCurveFactor,
                minCurveHeight,
                maxCurveHeight,
                descriptor.LaneIndex * laneSpacing
            );
        float playerRangeEnd;
        float enemyRangeEnd;
        BattleBezierRelationLineUIView.ResolveCenteredGapParameters(
            playerStart,
            sharedControl,
            enemyStart,
            clashArrowGap,
            out playerRangeEnd,
            out enemyRangeEnd
        );

        primaryCurve.RenderRange(
            playerStart,
            sharedControl,
            enemyStart,
            0f,
            playerRangeEnd,
            playerColor,
            false,
            highlighted
        );
        secondaryCurve.RenderRange(
            playerStart,
            sharedControl,
            enemyStart,
            1f,
            enemyRangeEnd,
            enemyColor,
            false,
            highlighted
        );
        return primaryCurve.IsVisible && primaryCurve.ArrowActiveSelf &&
            secondaryCurve.IsVisible && secondaryCurve.ArrowActiveSelf;
    }

    public void ApplyVisualSettings(
        Vector2 segmentSize,
        Vector2 arrowSize,
        float dashedGap,
        float solidOverlap,
        float underlayScale,
        float arrowScale
    )
    {
        EnsureRaycastSafety();
        primaryCurve?.ApplyVisualSettings(
            segmentSize,
            arrowSize,
            dashedGap,
            solidOverlap,
            underlayScale,
            arrowScale
        );
        secondaryCurve?.ApplyVisualSettings(
            segmentSize,
            arrowSize,
            dashedGap,
            solidOverlap,
            underlayScale,
            arrowScale
        );
    }

    public void PrepareForReuse()
    {
        EnsureRaycastSafety();
    }

    internal bool EnsureOwnedCurveReferences()
    {
        // 模板必须在序列化层级中持有自己的Curve；禁止克隆活动中的外部Preview视觉。
        return ValidateCurveOwnership(true);
    }

    internal bool ValidateCurveOwnership(bool logError)
    {
        bool valid = OwnsPrimaryCurve && OwnsSecondaryCurve &&
            primaryCurve != secondaryCurve;
        if (valid)
        {
            valid = primaryCurve.ArrowInstanceID != 0 &&
                secondaryCurve.ArrowInstanceID != 0 &&
                primaryCurve.ArrowInstanceID !=
                    secondaryCurve.ArrowInstanceID &&
                primaryCurve.SegmentTemplateInstanceID != 0 &&
                secondaryCurve.SegmentTemplateInstanceID != 0 &&
                primaryCurve.SegmentTemplateInstanceID !=
                    secondaryCurve.SegmentTemplateInstanceID &&
                primaryCurve.CanvasGroupInstanceID != 0 &&
                secondaryCurve.CanvasGroupInstanceID != 0 &&
                primaryCurve.CanvasGroupInstanceID !=
                    secondaryCurve.CanvasGroupInstanceID;
        }

        if (!valid && logError)
        {
            Debug.LogError(
                "RelationView Curve所有权无效：View=" + GetInstanceID() +
                "，PrimaryCurve=" + GetCurveInstanceID(primaryCurve) +
                "，PrimaryArrow=" + GetArrowInstanceID(primaryCurve) +
                "，SecondaryCurve=" + GetCurveInstanceID(secondaryCurve) +
                "，SecondaryArrow=" + GetArrowInstanceID(secondaryCurve) +
                "，OwnsPrimary=" + OwnsPrimaryCurve +
                "，OwnsSecondary=" + OwnsSecondaryCurve,
                this
            );
        }
        return valid;
    }

    internal bool SharesVisualInstancesWith(
        BattleActionRelationUIView other
    )
    {
        if (other == null)
        {
            return false;
        }

        BattleBezierRelationLineUIView[] ownCurves =
            { primaryCurve, secondaryCurve };
        BattleBezierRelationLineUIView[] otherCurves =
            { other.primaryCurve, other.secondaryCurve };
        for (int ownIndex = 0; ownIndex < ownCurves.Length; ownIndex++)
        {
            BattleBezierRelationLineUIView own = ownCurves[ownIndex];
            if (own == null)
            {
                continue;
            }
            for (int otherIndex = 0;
                 otherIndex < otherCurves.Length;
                 otherIndex++)
            {
                BattleBezierRelationLineUIView candidate =
                    otherCurves[otherIndex];
                if (candidate != null &&
                    (own == candidate ||
                     SharesNonZeroInstance(
                         own.ArrowInstanceID,
                         candidate.ArrowInstanceID
                     ) ||
                     SharesNonZeroInstance(
                         own.SegmentTemplateInstanceID,
                         candidate.SegmentTemplateInstanceID
                     ) ||
                     SharesNonZeroInstance(
                         own.UnderlayArrowInstanceID,
                         candidate.UnderlayArrowInstanceID
                     ) ||
                     SharesNonZeroInstance(
                         own.CanvasGroupInstanceID,
                         candidate.CanvasGroupInstanceID
                     )))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void EnsureRaycastSafety()
    {
        if (isDestroying)
        {
            return;
        }
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void ClearView()
    {
        RelationID = string.Empty;
        IsHighlighted = false;
        UnilateralArrowEndpointOffset = 0f;
        if (primaryCurve != null)
        {
            primaryCurve.Clear();
        }
        if (secondaryCurve != null)
        {
            secondaryCurve.Clear();
        }
        if (!isDestroying && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void ClearCurvesWhenDisplayTypeChanges(
        BattleActionRelationKind nextKind
    )
    {
        if (string.IsNullOrEmpty(RelationID) || Kind == nextKind)
        {
            return;
        }

        // 同一活动View从单向虚线切到双方实线时，先清空旧类型视觉。
        primaryCurve?.Clear();
        secondaryCurve?.Clear();
    }

    public bool ValidateConfiguration()
    {
        bool valid = true;
        if (primaryCurve == null)
        {
            Debug.LogWarning("Relation View Template 缺少 PrimaryCurve。", this);
            valid = false;
        }
        else
        {
            valid &= primaryCurve.ValidateConfiguration("PrimaryCurve");
        }
        if (secondaryCurve == null)
        {
            Debug.LogWarning(
                "Relation View Template 缺少 SecondaryCurve；Clash 无法显示。",
                this
            );
            valid = false;
        }
        else
        {
            valid &= secondaryCurve.ValidateConfiguration("SecondaryCurve");
        }
        return valid;
    }

    internal void ConfigureForTesting(
        BattleBezierRelationLineUIView primary,
        BattleBezierRelationLineUIView secondary
    )
    {
        primaryCurve = primary;
        secondaryCurve = secondary;
        EnsureRaycastSafety();
    }

    private bool OwnsCurve(BattleBezierRelationLineUIView curve)
    {
        return curve != null && curve.transform.IsChildOf(transform);
    }

    private static bool SharesNonZeroInstance(int left, int right)
    {
        return left != 0 && left == right;
    }

    private static int GetCurveInstanceID(
        BattleBezierRelationLineUIView curve
    )
    {
        return curve != null ? curve.GetInstanceID() : 0;
    }

    private static int GetArrowInstanceID(
        BattleBezierRelationLineUIView curve
    )
    {
        return curve != null ? curve.ArrowInstanceID : 0;
    }

    private static Vector2 ResolveVisualArrowEndpoint(
        Vector2 control,
        Vector2 end,
        float offset
    )
    {
        if (Mathf.Abs(offset) <= 0.001f)
        {
            return end;
        }

        Vector2 tangent = end - control;
        if (tangent.sqrMagnitude <= 0.0001f)
        {
            return end;
        }

        Vector2 normal = new Vector2(-tangent.y, tangent.x).normalized;
        return end + normal * offset;
    }
}
