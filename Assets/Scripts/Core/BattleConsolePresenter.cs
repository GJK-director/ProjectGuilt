// ConsolePresenter用于验证协议；Manual模式由测试或调试入口显式完成Request。
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleConsolePresenter : IBattleExecutionPresenter
{
    private readonly bool completeImmediately;
    private readonly Dictionary<long, BattlePresentationCompletion> completions =
        new Dictionary<long, BattlePresentationCompletion>();
    private readonly List<BattlePresentationRequest> requests =
        new List<BattlePresentationRequest>();

    public IReadOnlyList<BattlePresentationRequest> Requests
    {
        get { return requests; }
    }

    public BattleConsolePresenter(bool completeImmediately)
    {
        this.completeImmediately = completeImmediately;
    }

    public void Present(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion
    )
    {
        if (request == null || completion == null)
        {
            return;
        }

        requests.Add(request);
        completions[request.RequestId] = completion;
        Debug.Log(
            "[Presentation]\n" +
            "RequestId: " + request.RequestId + "\n" +
            "Cue: " + request.Cue + "\n" +
            "Item: " + (request.ExecutionItem != null
                ? request.ExecutionItem.order.ToString()
                : "无") + "\n" +
            "Outcome: " + request.Outcome + "\n" +
            "ImpactIndex: " + request.ImpactIndex
        );

        if (completeImmediately)
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

    public bool TryCompleteRequest(long requestId)
    {
        return completions.TryGetValue(
                requestId,
                out BattlePresentationCompletion completion
            ) &&
            completion.TryComplete(requestId);
    }

    public BattlePresentationRequest GetLastRequest()
    {
        return requests.Count > 0 ? requests[requests.Count - 1] : null;
    }
}
