// Phase 3.5：纯代码表现等待协议。Presenter只能完成请求，不能拥有战斗规则。
public enum BattlePresentationCue
{
    // 新 Engagement 进入 Ready State；是否需要 Approach Movement 由独立策略决定。
    ActionBegin,
    // Roll 已确定，允许播放结果提交前的反馈。
    RollResult,
    // Combat 结果的正式视觉落点，例如 Hit、Block 或 Miss。
    Impact,
    // 当前 Engagement 表现收尾，并允许 Runner 推进下一 ExecutionItem。
    ActionComplete,
    // 全部ExecutionItem完成后的正式表现闭合。
    ExecutionComplete
}

public sealed class BattlePresentationRequest
{
    public long RequestId { get; private set; }
    public BattlePresentationCue Cue { get; private set; }
    public BattleExecutionItem ExecutionItem { get; private set; }
    public BattleClashSession ClashSession { get; private set; }
    public BattleResolutionPlan ResolutionPlan { get; private set; }
    public BattleImpact Impact { get; private set; }
    public int ImpactIndex { get; private set; }
    public string Outcome { get; private set; }
    public bool ContinueBattleActionCameraToNextItem { get; private set; }
    public BattlePresentationInteractionContext InteractionContext
    {
        get;
        private set;
    }

    public BattlePresentationRequest(
        long requestId,
        BattlePresentationCue cue,
        BattleExecutionItem executionItem,
        BattleClashSession clashSession,
        BattleResolutionPlan resolutionPlan,
        BattleImpact impact,
        string outcome,
        bool continueBattleActionCameraToNextItem = false,
        BattlePresentationInteractionContext interactionContext = null
    )
    {
        RequestId = requestId;
        Cue = cue;
        ExecutionItem = executionItem;
        ClashSession = clashSession;
        ResolutionPlan = resolutionPlan;
        Impact = impact;
        ImpactIndex = impact != null ? impact.impactIndex : -1;
        Outcome = outcome ?? string.Empty;
        ContinueBattleActionCameraToNextItem =
            continueBattleActionCameraToNextItem;
        InteractionContext = interactionContext;
    }
}

public sealed class BattlePresentationCompletion
{
    public long RequestId { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsConsumed { get; private set; }
    public bool IsCancelled { get; private set; }

    internal BattlePresentationCompletion(long requestId)
    {
        RequestId = requestId;
    }

    public bool TryComplete(long requestId)
    {
        if (requestId != RequestId || IsCompleted || IsConsumed || IsCancelled)
        {
            return false;
        }

        IsCompleted = true;
        return true;
    }

    internal bool TryConsume(long requestId)
    {
        if (requestId != RequestId || !IsCompleted || IsConsumed || IsCancelled)
        {
            return false;
        }

        IsConsumed = true;
        return true;
    }

    internal bool TryCancel(long requestId)
    {
        if (requestId != RequestId || IsConsumed || IsCancelled)
        {
            return false;
        }

        IsCancelled = true;
        return true;
    }
}

public interface IBattleExecutionPresenter
{
    void Present(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion
    );

    void Cancel(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion
    );
}
