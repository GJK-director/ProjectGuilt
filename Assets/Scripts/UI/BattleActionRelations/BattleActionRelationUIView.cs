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
        if (primaryCurve != null)
        {
            primaryCurve.Clear();
        }
        if (secondaryCurve != null)
        {
            secondaryCurve.Clear();
        }
    }

    public void ShowUnilateral(
        BattleActionRelationDescriptor descriptor,
        Vector2 start,
        Vector2 end,
        Color color,
        bool highlighted,
        float baseCurveHeight,
        float distanceCurveFactor,
        float minCurveHeight,
        float maxCurveHeight,
        float laneSpacing
    )
    {
        EnsureRaycastSafety();
        if (descriptor == null || primaryCurve == null)
        {
            ClearView();
            return;
        }

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
        primaryCurve.Render(
            start,
            control,
            end,
            color,
            true,
            highlighted
        );
        secondaryCurve?.Clear();
        gameObject.SetActive(true);
    }

    public void ShowClash(
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
            return;
        }

        RelationID = descriptor.RelationID;
        Kind = descriptor.Kind;
        IsHighlighted = highlighted;

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
        gameObject.SetActive(true);
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
}
