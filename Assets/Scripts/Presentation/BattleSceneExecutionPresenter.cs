using System.Collections;
using UnityEngine;

// 正式战斗表现协议：按 Cue 解析运行时角色，并逐步接入异步战斗表现。
public sealed class BattleSceneExecutionPresenter : MonoBehaviour,
    IBattleExecutionPresenter
{
    private sealed class ActionPresentationContext
    {
        public BattleExecutionItem ExecutionItem;
        public BattleClashSession ClashSession;
        public BattleResolutionPlan ResolutionPlan;
        public string Outcome;

        public CharacterData SideAActor;
        public CharacterData SideBActor;
        public BattleUnitViewHandle SideAHandle;
        public BattleUnitViewHandle SideBHandle;
        public BattleCharacterPresentationController SideAPresentation;
        public BattleCharacterPresentationController SideBPresentation;
        public BattleClashEngagementResult ClashEngagement;

        public CharacterData CurrentAttacker;
        public CharacterData CurrentTarget;
        public BattleUnitViewHandle CurrentAttackerHandle;
        public BattleUnitViewHandle CurrentTargetHandle;
        public BattleCharacterPresentationController CurrentAttackerPresentation;
        public BattleCharacterPresentationController CurrentTargetPresentation;
        public int ImpactIndex = -1;

        public bool DefaultAttackStarted;
        public bool DefaultAttackImpactReached;
        public bool DefaultAttackFinished;
        public long DefaultAttackImpactRequestId;

        public long LastRequestId;
        public bool Cancelled;
    }

    [SerializeField] private BattleUnitViewSpawner unitViewSpawner;
    [SerializeField]
    private BattleDefaultAttackPresentationPlayer defaultAttackPresentationPlayer;
    [SerializeField]
    private BattleAttackVsAttackPresentationPlayer attackVsAttackPresentationPlayer;
    [SerializeField]
    private BattleClashEngagementProfile clashEngagementProfile;
    [SerializeField] private bool verboseLogging = false;

    private ActionPresentationContext activeContext;
    private Coroutine activePresentationCoroutine;
    private long activePresentationRequestId;

    void Awake()
    {
        ResolveDefaultAttackPresentationPlayer();
        ResolveAttackVsAttackPresentationPlayer();
    }

    public void Initialize(BattleUnitViewSpawner spawner)
    {
        unitViewSpawner = spawner;
        ResolveDefaultAttackPresentationPlayer();
        ResolveAttackVsAttackPresentationPlayer();
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

        switch (request.Cue)
        {
            case BattlePresentationCue.ActionBegin:
                HandleActionBegin(request, completion);
                break;
            case BattlePresentationCue.RollResult:
                HandleRollResult(request, completion);
                break;
            case BattlePresentationCue.Impact:
                HandleImpact(request, completion);
                break;
            case BattlePresentationCue.ActionComplete:
                HandleActionComplete(request, completion);
                break;
            default:
                CompleteRequest(request, completion);
                break;
        }
    }

    public void Cancel(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion
    )
    {
        if (request == null || completion == null)
        {
            return;
        }

        if (activePresentationRequestId == request.RequestId)
        {
            if (activePresentationCoroutine != null)
            {
                StopCoroutine(activePresentationCoroutine);
            }
            activePresentationCoroutine = null;
            activePresentationRequestId = 0L;
            attackVsAttackPresentationPlayer?.CancelAndReset();
            RestoreClashActorsToIdle(activeContext);
        }

        if (activeContext != null &&
            object.ReferenceEquals(
                activeContext.ExecutionItem,
                request.ExecutionItem))
        {
            BattleActionRollPanelHost.HideImmediate();
            activeContext.Cancelled = true;
            if (activeContext.DefaultAttackStarted &&
                attackVsAttackPresentationPlayer != null)
            {
                attackVsAttackPresentationPlayer.CancelAndReset();
            }
            activeContext = null;
        }

        completion.TryCancel(request.RequestId);
    }

    private void HandleActionBegin(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion
    )
    {
        BattleActionRollPanelHost.HideImmediate();
        activeContext = CreateContext(request);
        LogRequest(request, activeContext);

        if (!ShouldPlayAttackVsAttackApproach(request))
        {
            CompleteRequest(request, completion);
            return;
        }

        if (!TryStartAttackVsAttackApproach(
                request,
                completion,
                activeContext
            ))
        {
            CompleteRequest(request, completion);
        }
    }

    private bool ShouldPlayAttackVsAttackApproach(
        BattlePresentationRequest request
    )
    {
        return request.ExecutionItem != null &&
            request.ExecutionItem.executionType ==
                BattleExecutionItemType.RespondedEnemyIntent &&
            request.ClashSession != null &&
            request.ClashSession.ClashType == BattleClashType.AttackVsAttack;
    }

    private bool TryStartAttackVsAttackApproach(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        if (clashEngagementProfile == null)
        {
            LogApproachFallback(request, "接敌Profile未配置");
            return false;
        }

        if (!HasCompleteClashPresentationMapping(context) ||
            attackVsAttackPresentationPlayer == null ||
            !attackVsAttackPresentationPlayer.isActiveAndEnabled)
        {
            LogApproachFallback(request, "角色表现映射不完整");
            return false;
        }

        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        context.ClashEngagement = BattleClashEngagementResolver.Resolve(
            clashEngagementProfile,
            context.SideAPresentation.PresentationKey,
            context.SideBPresentation.PresentationKey,
            GetPresentationSpeed(context.SideAActor),
            GetPresentationSpeed(context.SideBActor)
        );
        activePresentationRequestId = requestId;
        LogApproachStarted(requestId, context);
        bool started = attackVsAttackPresentationPlayer
            .TryPlayClashReadyApproach(
                context.SideAPresentation,
                context.SideAHandle.WorldRoot.transform,
                context.SideBPresentation,
                context.SideBHandle.WorldRoot.transform,
                context.ClashEngagement,
                () => CompleteAttackVsAttackApproach(
                    context,
                    executionItem,
                    requestId,
                    completion
                )
            );

        if (started)
        {
            return true;
        }

        if (activePresentationRequestId == requestId)
        {
            activePresentationRequestId = 0L;
        }
        LogApproachFallback(requestId, "共享AttackVsAttack Player启动失败");
        return false;
    }

    private void CompleteAttackVsAttackApproach(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem))
        {
            return;
        }

        activePresentationRequestId = 0L;
        LogApproachCompleted(requestId);
        completion.TryComplete(requestId);
    }

    private bool HasCompleteClashPresentationMapping(
        ActionPresentationContext context
    )
    {
        return context != null &&
            context.SideAHandle != null &&
            context.SideBHandle != null &&
            context.SideAHandle.WorldRoot != null &&
            context.SideBHandle.WorldRoot != null &&
            context.SideAPresentation != null &&
            context.SideBPresentation != null;
    }

    private bool IsCurrentPresentationRequest(long requestId)
    {
        return requestId != 0L &&
            activePresentationRequestId == requestId &&
            activeContext != null &&
            activeContext.LastRequestId == requestId &&
            !activeContext.Cancelled;
    }

    private static void RestoreClashActorsToIdle(
        ActionPresentationContext context
    )
    {
        if (context == null)
        {
            return;
        }

        if (context.SideAPresentation != null)
        {
            context.SideAPresentation.SetIdle();
        }

        if (context.SideBPresentation != null)
        {
            context.SideBPresentation.SetIdle();
        }
    }

    private void HandleRollResult(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion
    )
    {
        ActionPresentationContext context = EnsureContext(request);
        RefreshRequestState(context, request);
        RefreshClashActors(context);
        LogRequest(request, context);
        BattleActionRollPanelHost.ShowForRoll(request);

        if (!ShouldPlayAttackTieResult(request))
        {
            CompleteRequest(request, completion);
            return;
        }

        if (!TryStartAttackTieResult(request, completion, context))
        {
            CompleteRequest(request, completion);
        }
    }

    private static bool ShouldPlayAttackTieResult(
        BattlePresentationRequest request
    )
    {
        BattleClashSession session = request.ClashSession;
        return session != null &&
            session.ClashType == BattleClashType.AttackVsAttack &&
            !session.IsFinalized &&
            session.AttemptResult == BattleClashAttemptResult.AttackTie &&
            session.RequiresAnotherRoll;
    }

    private bool TryStartAttackTieResult(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        if (!HasCompleteClashPresentationMapping(context) ||
            context.ClashEngagement == null ||
            attackVsAttackPresentationPlayer == null ||
            !attackVsAttackPresentationPlayer.isActiveAndEnabled)
        {
            return false;
        }

        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        activePresentationRequestId = requestId;
        bool started = attackVsAttackPresentationPlayer.TryPlayTieResult(
            context.SideAPresentation,
            context.SideAHandle.WorldRoot.transform,
            context.SideBPresentation,
            context.SideBHandle.WorldRoot.transform,
            context.ClashEngagement,
            () => CompleteAttackTieResult(
                context,
                executionItem,
                requestId,
                completion
            )
        );
        if (started)
        {
            return true;
        }

        if (activePresentationRequestId == requestId)
        {
            activePresentationRequestId = 0L;
        }
        return false;
    }

    private void CompleteAttackTieResult(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem))
        {
            return;
        }

        activePresentationRequestId = 0L;
        completion.TryComplete(requestId);
    }

    private void HandleImpact(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion
    )
    {
        ActionPresentationContext context = EnsureContext(request);
        RefreshRequestState(context, request);

        BattleImpact impact = request.Impact;
        context.CurrentAttacker = impact != null ? impact.attacker : null;
        context.CurrentTarget = impact != null ? impact.target : null;
        context.ImpactIndex = request.ImpactIndex;
        ResolvePresentation(
            context.CurrentAttacker,
            out context.CurrentAttackerHandle,
            out context.CurrentAttackerPresentation
        );
        ResolvePresentation(
            context.CurrentTarget,
            out context.CurrentTargetHandle,
            out context.CurrentTargetPresentation
        );

        LogRequest(request, context);
        if (!ShouldPlayDefaultAttackImpact(request, context))
        {
            CompleteRequest(request, completion);
            return;
        }

        if (!TryStartDefaultAttackImpact(request, completion, context))
        {
            CompleteRequest(request, completion);
        }
    }

    private void HandleActionComplete(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion
    )
    {
        ActionPresentationContext context = EnsureContext(request);
        RefreshRequestState(context, request);
        LogRequest(request, context);

        if (!context.DefaultAttackStarted)
        {
            BattleActionRollPanelHost.HideImmediate();
            CompleteRequest(request, completion);
            activeContext = null;
            return;
        }

        activePresentationRequestId = request.RequestId;
        activePresentationCoroutine = StartCoroutine(
            WaitForDefaultAttackTail(
                request,
                completion,
                context
            )
        );
    }

    private bool ShouldPlayDefaultAttackImpact(
        BattlePresentationRequest request,
        ActionPresentationContext context
    )
    {
        // Phase 4.2-C1只接第一个Default Attack Impact；多段演出后续处理。
        return request.ExecutionItem != null &&
            request.ExecutionItem.executionType ==
                BattleExecutionItemType.RespondedEnemyIntent &&
            request.ClashSession != null &&
            request.ClashSession.ClashType == BattleClashType.AttackVsAttack &&
            request.Impact != null &&
            request.ImpactIndex == 0 &&
            context != null &&
            context.CurrentAttackerHandle != null &&
            context.CurrentTargetHandle != null &&
            context.CurrentAttackerPresentation != null &&
            context.CurrentTargetPresentation != null &&
            attackVsAttackPresentationPlayer != null &&
            attackVsAttackPresentationPlayer.isActiveAndEnabled;
    }

    private bool TryStartDefaultAttackImpact(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        float directionSign = GetAttackDirectionSign(
            context.CurrentAttackerHandle,
            context.CurrentTargetHandle
        );
        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;

        context.DefaultAttackStarted = true;
        context.DefaultAttackImpactReached = false;
        context.DefaultAttackFinished = false;
        context.DefaultAttackImpactRequestId = requestId;
        activePresentationRequestId = requestId;

        bool started = attackVsAttackPresentationPlayer
            .TryPlayResolvedWinnerAttack(
                context.CurrentAttackerPresentation,
                context.CurrentTargetPresentation,
                directionSign,
                () => CompleteDefaultAttackImpact(
                    context,
                    executionItem,
                    requestId,
                    completion
                ),
                () => MarkDefaultAttackFinished(context, executionItem)
            );

        if (started)
        {
            return true;
        }

        context.DefaultAttackStarted = false;
        context.DefaultAttackImpactRequestId = 0L;
        if (activePresentationRequestId == requestId)
        {
            activePresentationRequestId = 0L;
        }
        return false;
    }

    private void CompleteDefaultAttackImpact(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (!IsOwnedDefaultAttackContext(context, executionItem) ||
            context.DefaultAttackImpactReached ||
            context.DefaultAttackImpactRequestId != requestId ||
            activePresentationRequestId != requestId)
        {
            return;
        }

        context.DefaultAttackImpactReached = true;
        activePresentationRequestId = 0L;
        completion.TryComplete(requestId);
    }

    private void MarkDefaultAttackFinished(
        ActionPresentationContext context,
        BattleExecutionItem executionItem
    )
    {
        if (!IsOwnedDefaultAttackContext(context, executionItem))
        {
            return;
        }

        context.DefaultAttackFinished = true;
    }

    private IEnumerator WaitForDefaultAttackTail(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        while (IsCurrentPresentationRequest(requestId) &&
            IsOwnedDefaultAttackContext(context, executionItem) &&
            !context.DefaultAttackFinished)
        {
            if (attackVsAttackPresentationPlayer == null ||
                (!attackVsAttackPresentationPlayer.IsRunning &&
                    !attackVsAttackPresentationPlayer.IsFinished))
            {
                context.DefaultAttackFinished = true;
                break;
            }

            yield return null;
        }

        if (!IsCurrentPresentationRequest(requestId) ||
            !IsOwnedDefaultAttackContext(context, executionItem))
        {
            yield break;
        }

        activePresentationCoroutine = null;
        activePresentationRequestId = 0L;
        BattleActionRollPanelHost.HideImmediate();
        completion.TryComplete(requestId);
        activeContext = null;
    }

    private bool IsOwnedDefaultAttackContext(
        ActionPresentationContext context,
        BattleExecutionItem executionItem
    )
    {
        return context != null &&
            object.ReferenceEquals(activeContext, context) &&
            object.ReferenceEquals(context.ExecutionItem, executionItem) &&
            context.DefaultAttackStarted &&
            !context.Cancelled;
    }

    private static float GetAttackDirectionSign(
        BattleUnitViewHandle attackerHandle,
        BattleUnitViewHandle targetHandle
    )
    {
        if (attackerHandle != null && targetHandle != null &&
            attackerHandle.WorldRoot != null &&
            targetHandle.WorldRoot != null)
        {
            float horizontalDelta =
                targetHandle.WorldRoot.transform.position.x -
                attackerHandle.WorldRoot.transform.position.x;
            if (Mathf.Abs(horizontalDelta) > 0.0001f)
            {
                return Mathf.Sign(horizontalDelta);
            }
        }

        return attackerHandle != null &&
            attackerHandle.WorldRenderer != null &&
            attackerHandle.WorldRenderer.flipX
                ? -1f
                : 1f;
    }

    private void ResolveDefaultAttackPresentationPlayer()
    {
        if (defaultAttackPresentationPlayer == null)
        {
            defaultAttackPresentationPlayer =
                GetComponent<BattleDefaultAttackPresentationPlayer>();
        }
    }

    private void ResolveAttackVsAttackPresentationPlayer()
    {
        if (attackVsAttackPresentationPlayer == null)
        {
            attackVsAttackPresentationPlayer =
                GetComponent<BattleAttackVsAttackPresentationPlayer>();
        }

        if (attackVsAttackPresentationPlayer == null)
        {
            Debug.LogError(
                "BattleSceneExecutionPresenter缺少持久化的" +
                "BattleAttackVsAttackPresentationPlayer。",
                this
            );
        }
    }

    private ActionPresentationContext EnsureContext(
        BattlePresentationRequest request
    )
    {
        if (activeContext == null ||
            !object.ReferenceEquals(
                activeContext.ExecutionItem,
                request.ExecutionItem))
        {
            activeContext = CreateContext(request);
        }

        return activeContext;
    }

    private ActionPresentationContext CreateContext(
        BattlePresentationRequest request
    )
    {
        ActionPresentationContext context = new ActionPresentationContext();
        RefreshRequestState(context, request);
        RefreshClashActors(context);
        return context;
    }

    private void RefreshRequestState(
        ActionPresentationContext context,
        BattlePresentationRequest request
    )
    {
        context.ExecutionItem = request.ExecutionItem;
        context.ClashSession = request.ClashSession;
        context.ResolutionPlan = request.ResolutionPlan;
        context.Outcome = request.Outcome;
        context.LastRequestId = request.RequestId;
        context.Cancelled = false;
    }

    private void RefreshClashActors(ActionPresentationContext context)
    {
        BattleClashSession session = context.ClashSession;
        context.SideAActor = session != null && session.SideA != null
            ? session.SideA.actor
            : null;
        context.SideBActor = session != null && session.SideB != null
            ? session.SideB.actor
            : null;

        ResolvePresentation(
            context.SideAActor,
            out context.SideAHandle,
            out context.SideAPresentation
        );
        ResolvePresentation(
            context.SideBActor,
            out context.SideBHandle,
            out context.SideBPresentation
        );
    }

    private void ResolvePresentation(
        CharacterData actor,
        out BattleUnitViewHandle handle,
        out BattleCharacterPresentationController presentation
    )
    {
        handle = unitViewSpawner != null && actor != null
            ? unitViewSpawner.GetHandle(actor)
            : null;
        presentation = handle != null
            ? handle.PresentationController
            : null;
    }

    private void CompleteRequest(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion
    )
    {
        completion.TryComplete(request.RequestId);
    }

    private void LogApproachStarted(
        long requestId,
        ActionPresentationContext context
    )
    {
        if (!verboseLogging)
        {
            return;
        }

        Debug.Log(
            "[ScenePresenter] RequestId=" + requestId +
            " / ActionBegin Shared Approach Start" +
            " / SideA=" + GetRuntimeUnitID(context.SideAActor) +
            " / SideB=" + GetRuntimeUnitID(context.SideBActor) +
            " / Duration=" + attackVsAttackPresentationPlayer.SprintDuration +
            " / Gap=" + context.ClashEngagement.FinalGap +
            " / Speed=" + context.ClashEngagement.SideASpeed +
            "/" + context.ClashEngagement.SideBSpeed +
            " / Share=" + context.ClashEngagement.SideAMovementShare +
            "/" + context.ClashEngagement.SideBMovementShare,
            this
        );
    }

    private void LogApproachCompleted(long requestId)
    {
        if (!verboseLogging)
        {
            return;
        }

        Debug.Log(
            "[ScenePresenter] RequestId=" + requestId +
            " / ActionBegin Approach Complete",
            this
        );
    }

    private void LogApproachFallback(
        BattlePresentationRequest request,
        string reason
    )
    {
        LogApproachFallback(request.RequestId, reason);
    }

    private void LogApproachFallback(long requestId, string reason)
    {
        if (!verboseLogging)
        {
            return;
        }

        Debug.Log(
            "[ScenePresenter] RequestId=" + requestId +
            " / ActionBegin Approach Fallback=" + reason,
            this
        );
    }

    private void LogRequest(
        BattlePresentationRequest request,
        ActionPresentationContext context
    )
    {
        if (!verboseLogging)
        {
            return;
        }

        if (request.Cue == BattlePresentationCue.ActionBegin)
        {
            Debug.Log(
                "[ScenePresenter] RequestId=" + request.RequestId +
                " / Cue=" + request.Cue +
                " / SideA=" + GetRuntimeUnitID(context.SideAActor) +
                " Handle=" + (context.SideAHandle != null) +
                " Presentation=" + (context.SideAPresentation != null) +
                " / SideB=" + GetRuntimeUnitID(context.SideBActor) +
                " Handle=" + (context.SideBHandle != null) +
                " Presentation=" + (context.SideBPresentation != null),
                this
            );
            return;
        }

        if (request.Cue == BattlePresentationCue.Impact)
        {
            Debug.Log(
                "[ScenePresenter] RequestId=" + request.RequestId +
                " / Cue=" + request.Cue +
                " / ImpactIndex=" + context.ImpactIndex +
                " / Attacker=" +
                GetRuntimeUnitID(context.CurrentAttacker) +
                " Handle=" + (context.CurrentAttackerHandle != null) +
                " Presentation=" +
                (context.CurrentAttackerPresentation != null) +
                " / Target=" + GetRuntimeUnitID(context.CurrentTarget) +
                " Handle=" + (context.CurrentTargetHandle != null) +
                " Presentation=" +
                (context.CurrentTargetPresentation != null),
                this
            );
            return;
        }

        Debug.Log(
            "[BattleSceneExecutionPresenter] Cue=" + request.Cue +
            " / RequestId=" + request.RequestId +
            " / SideA=" + IsMapped(context.SideAPresentation) +
            " / SideB=" + IsMapped(context.SideBPresentation) +
            " / Attacker=" + IsMapped(context.CurrentAttackerPresentation) +
            " / Target=" + IsMapped(context.CurrentTargetPresentation),
            this
        );
    }

    private static string GetRuntimeUnitID(CharacterData actor)
    {
        return actor != null && !string.IsNullOrEmpty(actor.runtimeUnitID)
            ? actor.runtimeUnitID
            : "<null>";
    }

    private static int GetPresentationSpeed(CharacterData actor)
    {
        return actor != null ? actor.GetCurrentSpeed() : 0;
    }

    private static bool IsMapped(
        BattleCharacterPresentationController presentation
    )
    {
        return presentation != null;
    }
}
