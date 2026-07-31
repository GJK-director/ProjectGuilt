using UnityEngine;

public sealed class BattleActionRelationUIView : MonoBehaviour
{
    [SerializeField] private BattleBezierRelationLineUIView primaryCurve;
    [SerializeField] private BattleBezierRelationLineUIView secondaryCurve;

    public string RelationID { get; private set; }
    public BattleActionRelationKind Kind { get; private set; }
    public bool IsHighlighted { get; private set; }
    public BattleBezierRelationLineUIView PrimaryCurve => primaryCurve;
    public BattleBezierRelationLineUIView SecondaryCurve => secondaryCurve;

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
        float clashCurveHeight,
        float distanceCurveFactor,
        float minCurveHeight,
        float maxCurveHeight,
        float laneSpacing,
        float clashArrowGap
    )
    {
        if (descriptor == null || primaryCurve == null ||
            secondaryCurve == null)
        {
            ClearView();
            return;
        }

        RelationID = descriptor.RelationID;
        Kind = descriptor.Kind;
        IsHighlighted = highlighted;

        float height = Mathf.Clamp(
            clashCurveHeight +
                Mathf.Abs(enemyStart.x - playerStart.x) *
                    distanceCurveFactor +
                descriptor.LaneIndex * laneSpacing,
            minCurveHeight,
            maxCurveHeight + descriptor.LaneIndex * laneSpacing
        );
        Vector2 center = new Vector2(
            (playerStart.x + enemyStart.x) * 0.5f,
            Mathf.Max(playerStart.y, enemyStart.y) + height
        );
        Vector2 direction = enemyStart - playerStart;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.right;
        }
        direction.Normalize();

        float halfGap = Mathf.Max(0f, clashArrowGap) * 0.5f;
        Vector2 playerTip = center - direction * halfGap;
        Vector2 enemyTip = center + direction * halfGap;
        Vector2 playerControl = Vector2.Lerp(
            playerStart,
            playerTip,
            0.55f
        );
        playerControl.y = playerTip.y;
        Vector2 enemyControl = Vector2.Lerp(
            enemyStart,
            enemyTip,
            0.55f
        );
        enemyControl.y = enemyTip.y;

        primaryCurve.Render(
            playerStart,
            playerControl,
            playerTip,
            playerColor,
            false,
            highlighted
        );
        secondaryCurve.Render(
            enemyStart,
            enemyControl,
            enemyTip,
            enemyColor,
            false,
            highlighted
        );
        gameObject.SetActive(true);
    }

    public void ClearView()
    {
        RelationID = string.Empty;
        IsHighlighted = false;
        primaryCurve?.Clear();
        secondaryCurve?.Clear();
        gameObject.SetActive(false);
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
    }
}
