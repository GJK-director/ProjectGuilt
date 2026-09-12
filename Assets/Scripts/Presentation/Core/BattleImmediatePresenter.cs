// 无表现环境的默认Presenter。只完成请求，不直接推进Runner。
public sealed class BattleImmediatePresenter : IBattleExecutionPresenter
{
    public static readonly BattleImmediatePresenter Instance =
        new BattleImmediatePresenter();

    private BattleImmediatePresenter()
    {
    }

    public void Present(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion
    )
    {
        if (request != null && completion != null)
        {
            completion.TryComplete(request.RequestId);
        }
    }

    public void Cancel(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion
    )
    {
        if (request != null && completion != null)
        {
            completion.TryCancel(request.RequestId);
        }
    }
}
