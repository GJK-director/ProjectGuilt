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

        public CharacterData DefenseAttacker;
        public CharacterData DefenseDefender;
        public BattleUnitViewHandle DefenseAttackerHandle;
        public BattleUnitViewHandle DefenseDefenderHandle;
        public BattleCharacterPresentationController DefenseAttackerPresentation;
        public BattleCharacterPresentationController DefenseDefenderPresentation;
        public BattleGuardPresentationResult GuardPresentationResult;
        public bool HasGuardPresentationResult;
        public bool GuardPresentationStarted;
        public bool GuardImpactReached;
        public bool GuardTailFinished;
        public long GuardImpactRequestId;

        public CharacterData DodgeAttacker;
        public CharacterData DodgeDefender;
        public BattleUnitViewHandle DodgeAttackerHandle;
        public BattleUnitViewHandle DodgeDefenderHandle;
        public BattleCharacterPresentationController DodgeAttackerPresentation;
        public BattleCharacterPresentationController DodgeDefenderPresentation;
        public BattleDodgePresentationResult DodgePresentationResult;
        public bool DodgeRollStarted;
        public bool DodgeRollResultReady;
        public bool DodgeTailFinished;
        public bool DodgeImpactStarted;
        public bool DodgeImpactFinished;
        public long DodgeRollRequestId;
        public long DodgeImpactRequestId;

        public bool DefaultAttackStarted;
        public bool DefaultAttackImpactReached;
        public bool DefaultAttackFinished;
        public long DefaultAttackImpactRequestId;
        public bool CameraCinematicOwned;
        public bool AttackVsAttackParallelBeginActive;
        public bool AttackVsAttackFocusFinished;
        public bool AttackVsAttackApproachFinished;
        public bool DefenseVsAttackAnchoredApproachActive;

        public BattleClashSideState LongRangeShooterSide;
        public BattleClashSideState LongRangeMeleeSide;
        public CharacterData LongRangeShooter;
        public CharacterData LongRangeMeleeActor;
        public BattleUnitViewHandle LongRangeShooterHandle;
        public BattleUnitViewHandle LongRangeMeleeHandle;
        public BattleCharacterPresentationController LongRangeShooterPresentation;
        public BattleCharacterPresentationController LongRangeMeleePresentation;
        public bool LongRangeShotAvailable;
        public bool LongRangeShooterWon;
        public bool LongRangeShotImpactStarted;
        public bool LongRangeShotImpactReached;
        public bool LongRangeShotImpactFinished;
        public long LongRangeShotImpactRequestId;

        public bool UnavailableShootResponseIsLongRange;
        public BattleCharacterPresentationController
            UnavailableShootResponderPresentation;

        public long LastRequestId;
        public bool Cancelled;
    }

    [SerializeField] private BattleUnitViewSpawner unitViewSpawner;
    [SerializeField]
    private BattleDefaultAttackPresentationPlayer defaultAttackPresentationPlayer;
    [SerializeField]
    private BattleAttackVsAttackPresentationPlayer attackVsAttackPresentationPlayer;
    [SerializeField]
    private BattleAttackVsGuardPresentationPlayer attackVsGuardPresentationPlayer;
    [SerializeField]
    private BattleAttackVsDodgePresentationPlayer attackVsDodgePresentationPlayer;
    [SerializeField]
    private BattleLongRangeShootVsAttackPresentationPlayer
        longRangeShootVsAttackPresentationPlayer;
    [SerializeField]
    private BattleClashEngagementProfile clashEngagementProfile;
    [SerializeField]
    private BattleCameraDirector battleCameraDirector;
    [SerializeField] private bool verboseLogging = false;

    private ActionPresentationContext activeContext;
    private Coroutine activePresentationCoroutine;
    private long activePresentationRequestId;

    void Awake()
    {
        ResolveDefaultAttackPresentationPlayer();
        ResolveAttackVsAttackPresentationPlayer();
        ValidateAttackVsGuardPresentationPlayer();
        ValidateAttackVsDodgePresentationPlayer();
        ResolveLongRangeShootVsAttackPresentationPlayer();
    }

    public void Initialize(BattleUnitViewSpawner spawner)
    {
        unitViewSpawner = spawner;
        ResolveDefaultAttackPresentationPlayer();
        ResolveAttackVsAttackPresentationPlayer();
        ValidateAttackVsGuardPresentationPlayer();
        ValidateAttackVsDodgePresentationPlayer();
        ResolveLongRangeShootVsAttackPresentationPlayer();
    }

    void OnDisable()
    {
        if (activePresentationCoroutine != null)
        {
            StopCoroutine(activePresentationCoroutine);
        }

        activePresentationCoroutine = null;
        activePresentationRequestId = 0L;
        if (activeContext != null)
        {
            activeContext.Cancelled = true;
            ClearAttackVsAttackParallelBeginState(activeContext);
            CancelOrReleaseCameraForContext(activeContext);
        }

        attackVsAttackPresentationPlayer?.CancelAndReset();
        attackVsGuardPresentationPlayer?.CancelAndReset();
        attackVsDodgePresentationPlayer?.CancelAndReset();
        longRangeShootVsAttackPresentationPlayer?.CancelAndReset();
        RestoreClashActorsToIdle(activeContext);
        BattleActionRollPanelHost.HideImmediate();
        activeContext = null;
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

        ActionPresentationContext cancellingContext =
            activeContext != null &&
            object.ReferenceEquals(
                activeContext.ExecutionItem,
                request.ExecutionItem)
                ? activeContext
                : null;
        if (cancellingContext != null)
        {
            cancellingContext.Cancelled = true;
            ClearAttackVsAttackParallelBeginState(cancellingContext);
            CancelOrReleaseCameraForContext(cancellingContext);
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
            attackVsGuardPresentationPlayer?.CancelAndReset();
            attackVsDodgePresentationPlayer?.CancelAndReset();
            longRangeShootVsAttackPresentationPlayer?.CancelAndReset();
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
            if (activeContext.GuardPresentationStarted &&
                attackVsGuardPresentationPlayer != null)
            {
                attackVsGuardPresentationPlayer.CancelAndReset();
            }
            if (activeContext.DodgeRollStarted &&
                attackVsDodgePresentationPlayer != null)
            {
                attackVsDodgePresentationPlayer.CancelAndReset();
            }
            if (activeContext.LongRangeShotImpactStarted &&
                longRangeShootVsAttackPresentationPlayer != null)
            {
                longRangeShootVsAttackPresentationPlayer.CancelAndReset();
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
        bool unavailableShootResponse =
            TryResolveUnavailableShootResponse(activeContext);
        LogRequest(request, activeContext);
        BattleActionRollPanelHost.ShowForActionBegin(request);

        if (unavailableShootResponse)
        {
            PrepareLongRangeEngagement(activeContext);
            if (activeContext.UnavailableShootResponseIsLongRange)
            {
                activeContext.UnavailableShootResponderPresentation?.SetAim();
            }
            activeContext.LongRangeMeleePresentation?.SetSprint();
            CompleteRequest(request, completion);
            return;
        }

        if (TryResolveLongRangeShootVsMelee(activeContext))
        {
            PrepareLongRangeEngagement(activeContext);
            if (activeContext.LongRangeShotAvailable &&
                longRangeShootVsAttackPresentationPlayer != null)
            {
                longRangeShootVsAttackPresentationPlayer
                    .TryApplyActionBeginAim(
                        activeContext.LongRangeShooterPresentation,
                        true
                    );
                activeContext.LongRangeMeleePresentation?.SetSprint();
            }

            CompleteRequest(request, completion);
            return;
        }

        if (IsAnyLongRangeAttackVsAttack(request))
        {
            Debug.LogWarning(
                "[ScenePresenter] 暂不支持LongRangeShoot vs LongRangeShoot表现，" +
                "本次仅安全完成表现请求。",
                this
            );
            CompleteRequest(request, completion);
            return;
        }

        if (ShouldPlayAttackVsAttackApproach(request))
        {
            if (ShouldPlayGenericMeleeAttackVsAttackFocus(request) &&
                TryStartAttackVsAttackFocusAndApproach(
                    request,
                    completion,
                    activeContext
                ))
            {
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
            return;
        }

        if (ShouldPlayDefenseVsAttackApproach(request))
        {
            if (ShouldPlayGenericMeleeDefenseVsAttackCamera(request) &&
                TryStartDefenseVsAttackAnchoredApproach(
                    request,
                    completion,
                    activeContext
                ))
            {
                return;
            }

            if (!TryStartDefenseVsAttackApproach(
                    request,
                    completion,
                    activeContext
                ))
            {
                CompleteRequest(request, completion);
            }
            return;
        }

        if (ShouldPlayDodgeVsAttackApproach(request))
        {
            if (!TryStartDodgeVsAttackApproach(
                    request,
                    completion,
                    activeContext
                ))
            {
                CompleteRequest(request, completion);
            }
            return;
        }

        CompleteRequest(request, completion);
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

    private static bool IsAnyLongRangeAttackVsAttack(
        BattlePresentationRequest request
    )
    {
        BattleClashSession session = request != null
            ? request.ClashSession
            : null;
        if (session == null ||
            session.ClashType != BattleClashType.AttackVsAttack ||
            session.SideA == null || session.SideB == null)
        {
            return false;
        }

        return session.SideA.cardState != null &&
                session.SideA.cardState.IsLongRangeShoot() ||
            session.SideB.cardState != null &&
                session.SideB.cardState.IsLongRangeShoot();
    }

    private static bool IsCloseRangeShootVsMelee(
        BattlePresentationRequest request
    )
    {
        BattleClashSession session = request != null
            ? request.ClashSession
            : null;
        if (session == null ||
            session.ClashType != BattleClashType.AttackVsAttack ||
            session.SideA == null || session.SideB == null ||
            session.SideA.cardState == null ||
            session.SideB.cardState == null)
        {
            return false;
        }

        return session.SideA.cardState.IsCloseRangeShoot() &&
                session.SideB.cardState.IsMeleeAttack() ||
            session.SideB.cardState.IsCloseRangeShoot() &&
                session.SideA.cardState.IsMeleeAttack();
    }

    private bool ShouldPlayGenericMeleeAttackVsAttackFocus(
        BattlePresentationRequest request
    )
    {
        BattleClashSession session = request != null
            ? request.ClashSession
            : null;
        return ShouldPlayAttackVsAttackApproach(request) &&
            !IsAnyLongRangeAttackVsAttack(request) &&
            !IsCloseRangeShootVsMelee(request) &&
            session != null &&
            session.SideA != null && session.SideB != null &&
            session.SideA.cardState != null &&
            session.SideB.cardState != null &&
            session.SideA.cardState.IsMeleeAttack() &&
            session.SideB.cardState.IsMeleeAttack();
    }

    private bool TryResolveLongRangeShootVsMelee(
        ActionPresentationContext context
    )
    {
        BattleClashSession session = context != null
            ? context.ClashSession
            : null;
        if (session == null ||
            session.ClashType != BattleClashType.AttackVsAttack ||
            session.SideA == null || session.SideB == null ||
            session.SideA.cardState == null ||
            session.SideB.cardState == null)
        {
            return false;
        }

        if (session.SideA.cardState.IsLongRangeShoot() &&
            session.SideB.cardState.IsMeleeAttack())
        {
            context.LongRangeShooterSide = session.SideA;
            context.LongRangeMeleeSide = session.SideB;
            context.LongRangeShooter = context.SideAActor;
            context.LongRangeMeleeActor = context.SideBActor;
            context.LongRangeShooterHandle = context.SideAHandle;
            context.LongRangeMeleeHandle = context.SideBHandle;
            context.LongRangeShooterPresentation = context.SideAPresentation;
            context.LongRangeMeleePresentation = context.SideBPresentation;
        }
        else if (session.SideB.cardState.IsLongRangeShoot() &&
            session.SideA.cardState.IsMeleeAttack())
        {
            context.LongRangeShooterSide = session.SideB;
            context.LongRangeMeleeSide = session.SideA;
            context.LongRangeShooter = context.SideBActor;
            context.LongRangeMeleeActor = context.SideAActor;
            context.LongRangeShooterHandle = context.SideBHandle;
            context.LongRangeMeleeHandle = context.SideAHandle;
            context.LongRangeShooterPresentation = context.SideBPresentation;
            context.LongRangeMeleePresentation = context.SideAPresentation;
        }
        else
        {
            return false;
        }

        BattleClashResourceSnapshot resourceSnapshot =
            context.LongRangeShooterSide.resourceSnapshot;
        context.LongRangeShotAvailable = resourceSnapshot != null &&
            resourceSnapshot.normalVersionEnabled;
        context.LongRangeShooterWon =
            object.ReferenceEquals(
                context.LongRangeShooterSide,
                session.SideA) &&
                session.FinalResult == BattleClashFinalResult.SideAWin ||
            object.ReferenceEquals(
                context.LongRangeShooterSide,
                session.SideB) &&
                session.FinalResult == BattleClashFinalResult.SideBWin;
        return true;
    }

    private bool TryResolveUnavailableShootResponse(
        ActionPresentationContext context
    )
    {
        BattleExecutionItem item = context != null
            ? context.ExecutionItem
            : null;
        BattleActionSlot actionSlot = item != null ? item.actionSlot : null;
        BattleEnemyIntent enemyIntent = item != null ? item.enemyIntent : null;
        if (item == null ||
            item.responseAttemptState !=
                BattleResponseAttemptState.UnavailableResource ||
            actionSlot == null || actionSlot.cardState == null ||
            enemyIntent == null || enemyIntent.enemyCardState == null ||
            !enemyIntent.enemyCardState.IsMeleeAttack())
        {
            return false;
        }

        bool isLongRangeShoot = actionSlot.cardState.IsLongRangeShoot();
        if (!isLongRangeShoot &&
            !actionSlot.cardState.IsCloseRangeShoot())
        {
            return false;
        }

        context.LongRangeShooterSide = null;
        context.LongRangeMeleeSide = null;
        context.UnavailableShootResponseIsLongRange = isLongRangeShoot;
        ResolvePresentation(
            actionSlot.actor,
            out _,
            out context.UnavailableShootResponderPresentation
        );

        // 资源不足后敌方目标已经由Core恢复，表现必须跟随恢复后的正式目标。
        context.LongRangeShooter = enemyIntent.actualTargetCharacter;
        context.LongRangeMeleeActor = enemyIntent.enemy;
        ResolvePresentation(
            context.LongRangeShooter,
            out context.LongRangeShooterHandle,
            out context.LongRangeShooterPresentation
        );
        ResolvePresentation(
            context.LongRangeMeleeActor,
            out context.LongRangeMeleeHandle,
            out context.LongRangeMeleePresentation
        );
        context.SideAActor = actionSlot.actor;
        ResolvePresentation(
            context.SideAActor,
            out context.SideAHandle,
            out context.SideAPresentation
        );
        context.SideBActor = context.LongRangeMeleeActor;
        context.SideBHandle = context.LongRangeMeleeHandle;
        context.SideBPresentation = context.LongRangeMeleePresentation;
        context.LongRangeShotAvailable = false;
        context.LongRangeShooterWon = false;
        return true;
    }

    private bool HasCompleteLongRangePresentationMapping(
        ActionPresentationContext context
    )
    {
        return context != null &&
            context.LongRangeShooterHandle != null &&
            context.LongRangeMeleeHandle != null &&
            context.LongRangeShooterHandle.WorldRoot != null &&
            context.LongRangeMeleeHandle.WorldRoot != null &&
            context.LongRangeShooterPresentation != null &&
            context.LongRangeMeleePresentation != null;
    }

    private void PrepareLongRangeEngagement(ActionPresentationContext context)
    {
        if (context == null || context.ClashEngagement != null ||
            clashEngagementProfile == null ||
            !HasCompleteLongRangePresentationMapping(context))
        {
            return;
        }

        // Cash-out时按Melee/Shooter顺序复用同一份Pair-relative移动份额。
        context.ClashEngagement = BattleClashEngagementResolver.Resolve(
            clashEngagementProfile,
            context.LongRangeMeleePresentation.PresentationKey,
            context.LongRangeShooterPresentation.PresentationKey,
            GetPresentationSpeed(context.LongRangeMeleeActor),
            GetPresentationSpeed(context.LongRangeShooter)
        );
    }

    private void HandleLongRangeRollResult(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        if (!TryResolveLongRangeShootVsMelee(context))
        {
            CompleteRequest(request, completion);
            return;
        }

        BattleClashSession session = request.ClashSession;
        bool hasTerminalWinner = session != null && session.IsFinalized &&
            (session.FinalResult == BattleClashFinalResult.SideAWin ||
                session.FinalResult == BattleClashFinalResult.SideBWin);
        if (!hasTerminalWinner || !context.LongRangeShotAvailable)
        {
            // Tie保持Aim并重新进入Manual Roll；无Bullet连Aim/Shoot/Flash都不触发。
            CompleteRequest(request, completion);
            return;
        }

        if (!HasCompleteLongRangePresentationMapping(context) ||
            longRangeShootVsAttackPresentationPlayer == null ||
            !longRangeShootVsAttackPresentationPlayer.isActiveAndEnabled)
        {
            CompleteRequest(request, completion);
            return;
        }

        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        activePresentationRequestId = requestId;
        bool started = longRangeShootVsAttackPresentationPlayer
            .TryPlayTerminalClash(
                context.LongRangeShooterPresentation,
                context.LongRangeMeleePresentation,
                !context.LongRangeShooterWon,
                () => CompleteLongRangeTerminalClash(
                    context,
                    executionItem,
                    requestId,
                    completion
                )
            );
        if (started)
        {
            return;
        }

        if (activePresentationRequestId == requestId)
        {
            activePresentationRequestId = 0L;
        }
        CompleteRequest(request, completion);
    }

    private void CompleteLongRangeTerminalClash(
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
        if (context.ClashEngagement == null)
        {
            context.ClashEngagement =
                ResolveAttackVsAttackClashEngagement(context);
        }
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

    private bool TryStartAttackVsAttackFocusAndApproach(
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

        BattleCameraDirector director = ResolveBattleCameraDirector();
        if (director == null)
        {
            return false;
        }

        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        context.ClashEngagement =
            ResolveAttackVsAttackClashEngagement(context);
        context.AttackVsAttackParallelBeginActive = true;
        context.AttackVsAttackFocusFinished = false;
        context.AttackVsAttackApproachFinished = false;
        activePresentationRequestId = requestId;

        bool focusStarted = director.TryPlayTwoUnitFocus(
            context.SideAHandle,
            context.SideBHandle,
            false,
            () => MarkAttackVsAttackFocusFinished(
                completion,
                context,
                executionItem,
                requestId
            )
        );
        if (!focusStarted)
        {
            ClearAttackVsAttackParallelBeginState(context);
            if (activePresentationRequestId == requestId)
            {
                activePresentationRequestId = 0L;
            }
            return false;
        }

        context.CameraCinematicOwned = true;
        LogApproachStarted(requestId, context);
        bool approachStarted = attackVsAttackPresentationPlayer
            .TryPlayClashReadyApproach(
                context.SideAPresentation,
                context.SideAHandle.WorldRoot.transform,
                context.SideBPresentation,
                context.SideBHandle.WorldRoot.transform,
                context.ClashEngagement,
                () => MarkAttackVsAttackApproachFinished(
                    completion,
                    context,
                    executionItem,
                    requestId
                )
            );
        if (approachStarted)
        {
            return true;
        }

        if (!director.CancelTwoUnitFocus(true))
        {
            director.ReleaseBattleActionCinematicControl();
        }
        context.CameraCinematicOwned = false;
        ClearAttackVsAttackParallelBeginState(context);
        if (activePresentationRequestId == requestId)
        {
            activePresentationRequestId = 0L;
        }
        LogApproachFallback(requestId, "共享AttackVsAttack Player启动失败");
        return false;
    }

    private void MarkAttackVsAttackFocusFinished(
        BattlePresentationCompletion completion,
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId
    )
    {
        if (!IsOwnedAttackVsAttackParallelBegin(
                context,
                executionItem,
                requestId
            ))
        {
            return;
        }

        context.AttackVsAttackFocusFinished = true;
        TryCompleteAttackVsAttackParallelBegin(
            completion,
            context,
            executionItem,
            requestId
        );
    }

    private void MarkAttackVsAttackApproachFinished(
        BattlePresentationCompletion completion,
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId
    )
    {
        if (!IsOwnedAttackVsAttackParallelBegin(
                context,
                executionItem,
                requestId
            ))
        {
            return;
        }

        context.AttackVsAttackApproachFinished = true;
        LogApproachCompleted(requestId);
        TryCompleteAttackVsAttackParallelBegin(
            completion,
            context,
            executionItem,
            requestId
        );
    }

    private void TryCompleteAttackVsAttackParallelBegin(
        BattlePresentationCompletion completion,
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId
    )
    {
        if (!context.AttackVsAttackFocusFinished ||
            !context.AttackVsAttackApproachFinished ||
            !IsOwnedAttackVsAttackParallelBegin(
                context,
                executionItem,
                requestId
            ))
        {
            return;
        }

        context.AttackVsAttackParallelBeginActive = false;
        activePresentationRequestId = 0L;
        completion.TryComplete(requestId);
    }

    private bool IsOwnedAttackVsAttackParallelBegin(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId
    )
    {
        return context != null &&
            context.AttackVsAttackParallelBeginActive &&
            IsCurrentPresentationRequest(requestId) &&
            object.ReferenceEquals(activeContext, context) &&
            object.ReferenceEquals(context.ExecutionItem, executionItem);
    }

    private BattleClashEngagementResult
        ResolveAttackVsAttackClashEngagement(
            ActionPresentationContext context
        )
    {
        return BattleClashEngagementResolver.Resolve(
            clashEngagementProfile,
            context.SideAPresentation.PresentationKey,
            context.SideBPresentation.PresentationKey,
            GetPresentationSpeed(context.SideAActor),
            GetPresentationSpeed(context.SideBActor)
        );
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

    private static bool ShouldPlayDefenseVsAttackApproach(
        BattlePresentationRequest request
    )
    {
        return request.ExecutionItem != null &&
            request.ExecutionItem.executionType ==
                BattleExecutionItemType.RespondedEnemyIntent &&
            request.ClashSession != null &&
            request.ClashSession.ClashType == BattleClashType.DefenseVsAttack;
    }

    private static bool ShouldPlayGenericMeleeDefenseVsAttackCamera(
        BattlePresentationRequest request
    )
    {
        BattleClashSession session = request != null
            ? request.ClashSession
            : null;
        if (!ShouldPlayDefenseVsAttackApproach(request) || session == null ||
            session.SideA == null || session.SideB == null ||
            session.SideA.cardState == null ||
            session.SideB.cardState == null)
        {
            return false;
        }

        return session.SideA.cardState.IsMeleeAttack() ||
            session.SideB.cardState.IsMeleeAttack();
    }

    private bool TryStartDefenseVsAttackAnchoredApproach(
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

        if (!TryResolveDefensePresentationActors(context) ||
            attackVsGuardPresentationPlayer == null ||
            !attackVsGuardPresentationPlayer.isActiveAndEnabled)
        {
            LogApproachFallback(request, "Defense角色表现映射不完整");
            return false;
        }

        BattleCameraDirector director = ResolveBattleCameraDirector();
        if (director == null)
        {
            return false;
        }

        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        context.ClashEngagement = BattleClashEngagementResolver.Resolve(
            clashEngagementProfile,
            context.DefenseDefenderPresentation.PresentationKey,
            context.DefenseAttackerPresentation.PresentationKey,
            GetPresentationSpeed(context.DefenseDefender),
            GetPresentationSpeed(context.DefenseAttacker)
        );
        activePresentationRequestId = requestId;

        bool cameraStarted = director.TryPlayAnchoredTwoUnitApproach(
            context.DefenseAttackerHandle,
            context.DefenseDefenderHandle,
            attackVsGuardPresentationPlayer.GuardApproachSeparation
        );
        if (!cameraStarted)
        {
            activePresentationRequestId = 0L;
            return false;
        }

        context.CameraCinematicOwned = true;
        context.DefenseVsAttackAnchoredApproachActive = true;
        LogApproachStarted(requestId, context);
        bool approachStarted = attackVsGuardPresentationPlayer
            .TryPlayClashReadyApproach(
                context.DefenseDefenderPresentation,
                context.DefenseDefenderHandle.WorldRoot.transform,
                context.DefenseAttackerPresentation,
                context.DefenseAttackerHandle.WorldRoot.transform,
                context.ClashEngagement,
                true,
                () => CompleteDefenseVsAttackAnchoredApproach(
                    context,
                    executionItem,
                    requestId,
                    completion
                )
            );
        if (approachStarted)
        {
            return true;
        }

        director.CancelAnchoredTwoUnitApproach(true);
        context.CameraCinematicOwned = false;
        context.DefenseVsAttackAnchoredApproachActive = false;
        if (activePresentationRequestId == requestId)
        {
            activePresentationRequestId = 0L;
        }
        LogApproachFallback(requestId, "共享AttackVsGuard Player启动失败");
        return false;
    }

    private void CompleteDefenseVsAttackAnchoredApproach(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem) ||
            !context.DefenseVsAttackAnchoredApproachActive)
        {
            return;
        }

        ResolveBattleCameraDirector()?
            .FinishAnchoredTwoUnitApproachTracking();
        context.DefenseVsAttackAnchoredApproachActive = false;
        activePresentationRequestId = 0L;
        LogApproachCompleted(requestId);
        completion.TryComplete(requestId);
    }

    private bool TryStartDefenseVsAttackApproach(
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

        if (!TryResolveDefensePresentationActors(context) ||
            attackVsGuardPresentationPlayer == null ||
            !attackVsGuardPresentationPlayer.isActiveAndEnabled)
        {
            LogApproachFallback(request, "Defense角色表现映射不完整");
            return false;
        }

        // 同一次Defense Clash只在ActionBegin读取正式速度并解析一次接敌结果。
        context.ClashEngagement = BattleClashEngagementResolver.Resolve(
            clashEngagementProfile,
            context.DefenseDefenderPresentation.PresentationKey,
            context.DefenseAttackerPresentation.PresentationKey,
            GetPresentationSpeed(context.DefenseDefender),
            GetPresentationSpeed(context.DefenseAttacker)
        );

        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        activePresentationRequestId = requestId;
        bool started = attackVsGuardPresentationPlayer
            .TryPlayClashReadyApproach(
                context.DefenseDefenderPresentation,
                context.DefenseDefenderHandle.WorldRoot.transform,
                context.DefenseAttackerPresentation,
                context.DefenseAttackerHandle.WorldRoot.transform,
                context.ClashEngagement,
                () => CompleteDefenseVsAttackApproach(
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
        LogApproachFallback(requestId, "共享AttackVsGuard Player启动失败");
        return false;
    }

    private void CompleteDefenseVsAttackApproach(
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

    private static bool ShouldPlayDodgeVsAttackApproach(
        BattlePresentationRequest request
    )
    {
        return request.ExecutionItem != null &&
            request.ClashSession != null &&
            request.ClashSession.ClashType == BattleClashType.DodgeVsAttack;
    }

    private static BattleCardState GetDodgeAttackCardState(
        BattleClashSession session
    )
    {
        if (session == null ||
            session.ClashType != BattleClashType.DodgeVsAttack)
        {
            return null;
        }

        if (session.SideA != null && session.SideA.cardState != null &&
            session.SideA.cardState.cardData != null &&
            session.SideA.cardState.cardData.cardType == CardType.Attack)
        {
            return session.SideA.cardState;
        }

        if (session.SideB != null && session.SideB.cardState != null &&
            session.SideB.cardState.cardData != null &&
            session.SideB.cardState.cardData.cardType == CardType.Attack)
        {
            return session.SideB.cardState;
        }

        return null;
    }

    private bool TryStartDodgeVsAttackApproach(
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

        if (!TryResolveDodgePresentationActors(context) ||
            attackVsDodgePresentationPlayer == null ||
            !attackVsDodgePresentationPlayer.isActiveAndEnabled)
        {
            LogApproachFallback(request, "Dodge角色表现映射不完整");
            return false;
        }

        context.ClashEngagement = BattleClashEngagementResolver.Resolve(
            clashEngagementProfile,
            context.DodgeDefenderPresentation.PresentationKey,
            context.DodgeAttackerPresentation.PresentationKey,
            GetPresentationSpeed(context.DodgeDefender),
            GetPresentationSpeed(context.DodgeAttacker)
        );

        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        BattleCardState attackCardState = GetDodgeAttackCardState(
            request.ClashSession
        );
        bool useRealApproach = attackCardState != null &&
            attackCardState.IsCloseRangeShoot();
        activePresentationRequestId = requestId;
        bool started = attackVsDodgePresentationPlayer
            .TryPlayClashReadyApproach(
                context.DodgeDefenderPresentation,
                context.DodgeDefenderHandle.WorldRoot.transform,
                context.DodgeAttackerPresentation,
                context.DodgeAttackerHandle.WorldRoot.transform,
                context.ClashEngagement,
                useRealApproach,
                () => CompleteDodgeVsAttackApproach(
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
        LogApproachFallback(requestId, "共享AttackVsDodge Player启动失败");
        return false;
    }

    private void CompleteDodgeVsAttackApproach(
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

    private static bool TryResolveDefensePresentationActors(
        ActionPresentationContext context
    )
    {
        BattleClashSession session = context != null
            ? context.ClashSession
            : null;
        if (session == null ||
            session.ClashType != BattleClashType.DefenseVsAttack ||
            session.SideA == null || session.SideB == null ||
            session.SideA.cardState == null ||
            session.SideB.cardState == null ||
            session.SideA.cardState.cardData == null ||
            session.SideB.cardState.cardData == null)
        {
            return false;
        }

        string sideAType = session.SideA.cardState.cardData.cardType;
        string sideBType = session.SideB.cardState.cardData.cardType;
        if (sideAType == CardType.Defense && sideBType == CardType.Attack)
        {
            context.DefenseDefender = context.SideAActor;
            context.DefenseDefenderHandle = context.SideAHandle;
            context.DefenseDefenderPresentation = context.SideAPresentation;
            context.DefenseAttacker = context.SideBActor;
            context.DefenseAttackerHandle = context.SideBHandle;
            context.DefenseAttackerPresentation = context.SideBPresentation;
        }
        else if (sideAType == CardType.Attack &&
            sideBType == CardType.Defense)
        {
            context.DefenseAttacker = context.SideAActor;
            context.DefenseAttackerHandle = context.SideAHandle;
            context.DefenseAttackerPresentation = context.SideAPresentation;
            context.DefenseDefender = context.SideBActor;
            context.DefenseDefenderHandle = context.SideBHandle;
            context.DefenseDefenderPresentation = context.SideBPresentation;
        }
        else
        {
            return false;
        }

        return context.DefenseAttacker != null &&
            context.DefenseDefender != null &&
            context.DefenseAttackerHandle != null &&
            context.DefenseDefenderHandle != null &&
            context.DefenseAttackerHandle.WorldRoot != null &&
            context.DefenseDefenderHandle.WorldRoot != null &&
            context.DefenseAttackerPresentation != null &&
            context.DefenseDefenderPresentation != null;
    }

    private static bool TryResolveDodgePresentationActors(
        ActionPresentationContext context
    )
    {
        BattleClashSession session = context != null
            ? context.ClashSession
            : null;
        if (session == null ||
            session.ClashType != BattleClashType.DodgeVsAttack ||
            session.SideA == null || session.SideB == null ||
            session.SideA.cardState == null ||
            session.SideB.cardState == null ||
            session.SideA.cardState.cardData == null ||
            session.SideB.cardState.cardData == null)
        {
            return false;
        }

        string sideAType = session.SideA.cardState.cardData.cardType;
        string sideBType = session.SideB.cardState.cardData.cardType;
        if (sideAType == CardType.Dodge && sideBType == CardType.Attack)
        {
            context.DodgeDefender = context.SideAActor;
            context.DodgeDefenderHandle = context.SideAHandle;
            context.DodgeDefenderPresentation = context.SideAPresentation;
            context.DodgeAttacker = context.SideBActor;
            context.DodgeAttackerHandle = context.SideBHandle;
            context.DodgeAttackerPresentation = context.SideBPresentation;
        }
        else if (sideAType == CardType.Attack &&
            sideBType == CardType.Dodge)
        {
            context.DodgeAttacker = context.SideAActor;
            context.DodgeAttackerHandle = context.SideAHandle;
            context.DodgeAttackerPresentation = context.SideAPresentation;
            context.DodgeDefender = context.SideBActor;
            context.DodgeDefenderHandle = context.SideBHandle;
            context.DodgeDefenderPresentation = context.SideBPresentation;
        }
        else
        {
            return false;
        }

        return context.DodgeAttacker != null &&
            context.DodgeDefender != null &&
            context.DodgeAttackerHandle != null &&
            context.DodgeDefenderHandle != null &&
            context.DodgeAttackerHandle.WorldRoot != null &&
            context.DodgeDefenderHandle.WorldRoot != null &&
            context.DodgeAttackerPresentation != null &&
            context.DodgeDefenderPresentation != null;
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
            context.SideAPresentation.ResetToStableIdlePresentation();
        }

        if (context.SideBPresentation != null)
        {
            context.SideBPresentation.ResetToStableIdlePresentation();
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

        if (IsAnyLongRangeAttackVsAttack(request))
        {
            HandleLongRangeRollResult(request, completion, context);
            return;
        }

        if (IsCloseRangeShootVsMelee(request) &&
            ShouldPlayAttackTieResult(request))
        {
            // CloseRange平点保持双方Sprint，等待下一次Manual Roll。
            CompleteRequest(request, completion);
            return;
        }

        if (ShouldPlayDodgeRollResult(request))
        {
            if (!TryStartDodgeRollResult(request, completion, context))
            {
                CompleteRequest(request, completion);
            }
            return;
        }

        if (TryCacheGuardPresentationResult(request, context))
        {
            CompleteRequest(request, completion);
            return;
        }

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

    private static bool TryCacheGuardPresentationResult(
        BattlePresentationRequest request,
        ActionPresentationContext context
    )
    {
        BattleClashSession session = request.ClashSession;
        if (context == null || session == null ||
            session.ClashType != BattleClashType.DefenseVsAttack ||
            !session.IsFinalized)
        {
            return false;
        }

        if (session.FinalResult == BattleClashFinalResult.DefenseFullBlock)
        {
            context.GuardPresentationResult =
                BattleGuardPresentationResult.FullBlock;
        }
        else if (session.FinalResult ==
            BattleClashFinalResult.DefenseReducedDamage)
        {
            context.GuardPresentationResult =
                BattleGuardPresentationResult.ReducedDamage;
        }
        else
        {
            return false;
        }

        context.HasGuardPresentationResult = true;
        return true;
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
            () => HandleAttackTieCollisionCamera(
                context,
                executionItem
            ),
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

    private void HandleAttackTieCollisionCamera(
        ActionPresentationContext context,
        BattleExecutionItem executionItem
    )
    {
        if (context == null ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem) ||
            context.Cancelled ||
            !context.CameraCinematicOwned)
        {
            return;
        }

        ResolveBattleCameraDirector()?.TryPlayGenericClashImpact();
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

        bool unavailableShootResponse =
            TryResolveUnavailableShootResponse(context);
        LogRequest(request, context);
        if (unavailableShootResponse)
        {
            if (!HasCompleteLongRangePresentationMapping(context) ||
                !object.ReferenceEquals(
                    context.CurrentAttacker,
                    context.LongRangeMeleeActor) ||
                !object.ReferenceEquals(
                    context.CurrentTarget,
                    context.LongRangeShooter) ||
                !TryStartLongRangeMeleeCashOut(
                    request,
                    completion,
                    context,
                    true
                ))
            {
                CompleteRequest(request, completion);
            }
            return;
        }

        if (IsAnyLongRangeAttackVsAttack(request))
        {
            HandleLongRangeImpact(request, completion, context);
            return;
        }

        if (ShouldPlayDodgeFailedImpact(request, context))
        {
            if (!TryStartDodgeFailedImpact(request, completion, context))
            {
                attackVsDodgePresentationPlayer?.CancelAndReset();
                context.DodgeRollStarted = false;
                context.DodgeTailFinished = true;
                CompleteRequest(request, completion);
            }
            return;
        }

        if (ShouldPlayDefenseGuardImpact(request, context))
        {
            if (!TryStartDefenseGuardImpact(request, completion, context))
            {
                CompleteRequest(request, completion);
            }
            return;
        }

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

    private void HandleLongRangeImpact(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        if (!TryResolveLongRangeShootVsMelee(context) ||
            !HasCompleteLongRangePresentationMapping(context))
        {
            CompleteRequest(request, completion);
            return;
        }

        if (context.LongRangeShooterWon)
        {
            if (!context.LongRangeShotAvailable ||
                !object.ReferenceEquals(
                    context.CurrentAttacker,
                    context.LongRangeShooter) ||
                !object.ReferenceEquals(
                    context.CurrentTarget,
                    context.LongRangeMeleeActor) ||
                !TryStartLongRangeShotImpact(
                    request,
                    completion,
                    context
                ))
            {
                CompleteRequest(request, completion);
            }
            return;
        }

        if (!object.ReferenceEquals(
                context.CurrentAttacker,
                context.LongRangeMeleeActor) ||
            !object.ReferenceEquals(
                context.CurrentTarget,
                context.LongRangeShooter) ||
            !TryStartLongRangeMeleeCashOut(request, completion, context))
        {
            CompleteRequest(request, completion);
        }
    }

    private bool TryStartLongRangeShotImpact(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        if (longRangeShootVsAttackPresentationPlayer == null ||
            !longRangeShootVsAttackPresentationPlayer.isActiveAndEnabled)
        {
            return false;
        }

        float directionSign = GetAttackDirectionSign(
            context.LongRangeShooterHandle,
            context.LongRangeMeleeHandle
        );
        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        context.LongRangeShotImpactStarted = true;
        context.LongRangeShotImpactReached = false;
        context.LongRangeShotImpactFinished = false;
        context.LongRangeShotImpactRequestId = requestId;
        activePresentationRequestId = requestId;

        bool started = longRangeShootVsAttackPresentationPlayer.TryPlayShotHit(
            context.LongRangeShooterPresentation,
            context.LongRangeMeleePresentation,
            context.LongRangeMeleeHandle.WorldRoot.transform,
            directionSign,
            () => CompleteLongRangeShotImpact(
                context,
                executionItem,
                requestId,
                completion
            ),
            () => MarkLongRangeShotImpactFinished(context, executionItem)
        );
        if (started)
        {
            return true;
        }

        context.LongRangeShotImpactStarted = false;
        context.LongRangeShotImpactRequestId = 0L;
        if (activePresentationRequestId == requestId)
        {
            activePresentationRequestId = 0L;
        }
        return false;
    }

    private void CompleteLongRangeShotImpact(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (!IsOwnedLongRangeShotImpact(context, executionItem) ||
            context.LongRangeShotImpactReached ||
            context.LongRangeShotImpactRequestId != requestId ||
            activePresentationRequestId != requestId)
        {
            return;
        }

        context.LongRangeShotImpactReached = true;
        activePresentationRequestId = 0L;
        completion.TryComplete(requestId);
    }

    private void MarkLongRangeShotImpactFinished(
        ActionPresentationContext context,
        BattleExecutionItem executionItem
    )
    {
        if (IsOwnedLongRangeShotImpact(context, executionItem))
        {
            context.LongRangeShotImpactFinished = true;
        }
    }

    private bool TryStartLongRangeMeleeCashOut(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context,
        bool forceSingleActorApproach = false
    )
    {
        if (attackVsAttackPresentationPlayer == null ||
            !attackVsAttackPresentationPlayer.isActiveAndEnabled)
        {
            return false;
        }

        PrepareLongRangeEngagement(context);
        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        context.DefaultAttackStarted = true;
        context.DefaultAttackImpactReached = false;
        context.DefaultAttackFinished = false;
        context.DefaultAttackImpactRequestId = requestId;
        activePresentationRequestId = requestId;

        if (context.ClashEngagement != null &&
            (context.LongRangeShotAvailable || forceSingleActorApproach) &&
            BattleClashEngagementResolver.RequiresApproach(
                context.LongRangeMeleeHandle.WorldRoot.transform.position,
                context.LongRangeShooterHandle.WorldRoot.transform.position,
                context.ClashEngagement
            ))
        {
            activePresentationCoroutine = StartCoroutine(
                RunLongRangeMeleeSingleActorApproach(
                    context,
                    executionItem,
                    requestId,
                    completion
                )
            );
            return true;
        }

        // NoBullet路径保持既有双边Cash-out，本轮只修有Bullet的Shooter Lose。
        if (context.ClashEngagement != null &&
            !context.LongRangeShotAvailable &&
            !forceSingleActorApproach)
        {
            bool approachStarted = attackVsAttackPresentationPlayer
                .TryPlayResolvedReengagementApproach(
                    context.LongRangeMeleePresentation,
                    context.LongRangeMeleeHandle.WorldRoot.transform,
                    context.LongRangeShooterPresentation,
                    context.LongRangeShooterHandle.WorldRoot.transform,
                    context.ClashEngagement,
                    () => ContinueLongRangeMeleeCashOut(
                        context,
                        executionItem,
                        requestId,
                        completion
                    )
                );
            if (approachStarted)
            {
                return true;
            }
        }

        if (TryStartLongRangeMeleeResolvedAttack(
                context,
                executionItem,
                requestId,
                completion
            ))
        {
            return true;
        }

        ResetFailedLongRangeMeleeCashOut(context, requestId);
        return false;
    }

    private IEnumerator RunLongRangeMeleeSingleActorApproach(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        yield return BattleClashReadyApproachMotion.PlaySingleActorApproach(
            context.LongRangeMeleePresentation,
            context.LongRangeMeleeHandle.WorldRoot.transform,
            context.LongRangeShooterPresentation,
            context.LongRangeShooterHandle.WorldRoot.transform,
            context.ClashEngagement.FinalGap,
            attackVsAttackPresentationPlayer.SprintDuration,
            attackVsAttackPresentationPlayer.AfterimageSpawnInterval,
            () => IsCurrentPresentationRequest(requestId) &&
                object.ReferenceEquals(activeContext, context) &&
                object.ReferenceEquals(context.ExecutionItem, executionItem)
        );

        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem))
        {
            yield break;
        }

        activePresentationCoroutine = null;
        ContinueLongRangeMeleeCashOut(
            context,
            executionItem,
            requestId,
            completion
        );
    }

    private void ContinueLongRangeMeleeCashOut(
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

        if (TryStartLongRangeMeleeResolvedAttack(
                context,
                executionItem,
                requestId,
                completion
            ))
        {
            return;
        }

        ResetFailedLongRangeMeleeCashOut(context, requestId);
        completion.TryComplete(requestId);
    }

    private bool TryStartLongRangeMeleeResolvedAttack(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem) ||
            attackVsAttackPresentationPlayer == null)
        {
            return false;
        }

        float directionSign = GetAttackDirectionSign(
            context.LongRangeMeleeHandle,
            context.LongRangeShooterHandle
        );
        return attackVsAttackPresentationPlayer.TryPlayResolvedWinnerAttack(
            context.LongRangeMeleePresentation,
            context.LongRangeShooterPresentation,
            context.LongRangeShooterHandle.WorldRoot.transform,
            directionSign,
            () => CompleteDefaultAttackImpact(
                context,
                executionItem,
                requestId,
                completion
            ),
            () => MarkDefaultAttackFinished(context, executionItem)
        );
    }

    private void ResetFailedLongRangeMeleeCashOut(
        ActionPresentationContext context,
        long requestId
    )
    {
        context.DefaultAttackStarted = false;
        context.DefaultAttackImpactRequestId = 0L;
        if (activePresentationRequestId == requestId)
        {
            activePresentationRequestId = 0L;
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

        if (context.DodgeRollStarted)
        {
            HandleDodgeActionComplete(request, completion, context);
            return;
        }

        if (context.GuardPresentationStarted)
        {
            HandleDefenseActionComplete(request, completion, context);
            return;
        }

        if (context.LongRangeShotImpactStarted)
        {
            HandleLongRangeActionComplete(request, completion, context);
            return;
        }

        if (!context.DefaultAttackStarted)
        {
            BattleActionRollPanelHost.HideImmediate();
            ReleaseCameraForContext(context);
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

    private void HandleDodgeActionComplete(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        if (context.DodgeTailFinished)
        {
            BattleActionRollPanelHost.HideImmediate();
            CompleteRequest(request, completion);
            activeContext = null;
            return;
        }

        activePresentationRequestId = request.RequestId;
        activePresentationCoroutine = StartCoroutine(
            WaitForDodgeTail(request, completion, context)
        );
    }

    private void HandleLongRangeActionComplete(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        if (context.LongRangeShotImpactFinished)
        {
            BattleActionRollPanelHost.HideImmediate();
            CompleteRequest(request, completion);
            activeContext = null;
            return;
        }

        activePresentationRequestId = request.RequestId;
        activePresentationCoroutine = StartCoroutine(
            WaitForLongRangeShotImpactTail(request, completion, context)
        );
    }

    private IEnumerator WaitForLongRangeShotImpactTail(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        while (IsCurrentPresentationRequest(requestId) &&
            IsOwnedLongRangeShotImpact(context, executionItem) &&
            !context.LongRangeShotImpactFinished)
        {
            if (longRangeShootVsAttackPresentationPlayer == null ||
                (!longRangeShootVsAttackPresentationPlayer.IsRunning &&
                    !longRangeShootVsAttackPresentationPlayer.IsFinished))
            {
                context.LongRangeShotImpactFinished = true;
                break;
            }

            yield return null;
        }

        if (!IsCurrentPresentationRequest(requestId) ||
            !IsOwnedLongRangeShotImpact(context, executionItem))
        {
            yield break;
        }

        activePresentationCoroutine = null;
        activePresentationRequestId = 0L;
        BattleActionRollPanelHost.HideImmediate();
        ReleaseCameraForContext(context);
        completion.TryComplete(requestId);
        activeContext = null;
    }

    private bool IsOwnedLongRangeShotImpact(
        ActionPresentationContext context,
        BattleExecutionItem executionItem
    )
    {
        return context != null &&
            object.ReferenceEquals(activeContext, context) &&
            object.ReferenceEquals(context.ExecutionItem, executionItem) &&
            context.LongRangeShotImpactStarted &&
            !context.Cancelled;
    }

    private IEnumerator WaitForDodgeTail(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        while (IsCurrentPresentationRequest(requestId) &&
            IsOwnedDodgePresentationContext(context, executionItem) &&
            !context.DodgeTailFinished)
        {
            if (attackVsDodgePresentationPlayer == null ||
                (!attackVsDodgePresentationPlayer.IsRunning &&
                    !attackVsDodgePresentationPlayer.IsFinished))
            {
                context.DodgeTailFinished = true;
                break;
            }

            yield return null;
        }

        if (!IsCurrentPresentationRequest(requestId) ||
            !IsOwnedDodgePresentationContext(context, executionItem))
        {
            yield break;
        }

        activePresentationCoroutine = null;
        activePresentationRequestId = 0L;
        BattleActionRollPanelHost.HideImmediate();
        completion.TryComplete(requestId);
        activeContext = null;
    }

    private bool IsOwnedDodgePresentationContext(
        ActionPresentationContext context,
        BattleExecutionItem executionItem
    )
    {
        return context != null &&
            object.ReferenceEquals(activeContext, context) &&
            object.ReferenceEquals(context.ExecutionItem, executionItem) &&
            context.DodgeRollStarted &&
            !context.Cancelled;
    }

    private bool ShouldPlayDefenseGuardImpact(
        BattlePresentationRequest request,
        ActionPresentationContext context
    )
    {
        if (request.ExecutionItem == null ||
            request.ClashSession == null ||
            request.ClashSession.ClashType !=
                BattleClashType.DefenseVsAttack ||
            request.Impact == null || request.ImpactIndex != 0 ||
            context == null || !context.HasGuardPresentationResult ||
            !TryResolveDefensePresentationActors(context) ||
            attackVsGuardPresentationPlayer == null ||
            !attackVsGuardPresentationPlayer.isActiveAndEnabled)
        {
            return false;
        }

        return object.ReferenceEquals(
                context.CurrentAttacker,
                context.DefenseAttacker) &&
            object.ReferenceEquals(
                context.CurrentTarget,
                context.DefenseDefender);
    }

    private bool TryStartDefenseGuardImpact(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        float directionSign = GetAttackDirectionSign(
            context.DefenseAttackerHandle,
            context.DefenseDefenderHandle
        );
        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;

        context.GuardPresentationStarted = true;
        context.GuardImpactReached = false;
        context.GuardTailFinished = false;
        context.GuardImpactRequestId = requestId;
        activePresentationRequestId = requestId;

        bool useCloseRangeShoot = request.Impact.sourceCardState != null &&
            request.Impact.sourceCardState.IsCloseRangeShoot();
        bool useMeleeGuardCamera = request.Impact.sourceCardState != null &&
            request.Impact.sourceCardState.IsMeleeAttack();
        bool started = attackVsGuardPresentationPlayer.TryPlayGuardImpact(
            context.DefenseAttackerPresentation,
            context.DefenseDefenderPresentation,
            directionSign,
            context.GuardPresentationResult,
            useCloseRangeShoot,
            () => HandleDefenseGuardTrueVisualContact(
                context,
                executionItem,
                requestId,
                completion,
                useMeleeGuardCamera
            ),
            () => MarkDefenseGuardTailFinished(context, executionItem)
        );

        if (started)
        {
            return true;
        }

        context.GuardPresentationStarted = false;
        context.GuardImpactRequestId = 0L;
        if (activePresentationRequestId == requestId)
        {
            activePresentationRequestId = 0L;
        }
        return false;
    }

    private void HandleDefenseGuardTrueVisualContact(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion,
        bool playGuardCameraImpact
    )
    {
        if (!IsOwnedGuardPresentationContext(context, executionItem) ||
            context.GuardImpactReached ||
            context.GuardImpactRequestId != requestId ||
            activePresentationRequestId != requestId)
        {
            return;
        }

        if (playGuardCameraImpact && context.CameraCinematicOwned)
        {
            bool isFullBlock = context.GuardPresentationResult ==
                BattleGuardPresentationResult.FullBlock;
            BattleCameraDirector director = ResolveBattleCameraDirector();
            director?.FinishAnchoredTwoUnitApproachTracking();
            director?.TryPlayGenericGuardImpact(isFullBlock);
        }

        CompleteDefenseGuardImpact(
            context,
            executionItem,
            requestId,
            completion
        );
    }

    private void CompleteDefenseGuardImpact(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (!IsOwnedGuardPresentationContext(context, executionItem) ||
            context.GuardImpactReached ||
            context.GuardImpactRequestId != requestId ||
            activePresentationRequestId != requestId)
        {
            return;
        }

        // 视觉接触点只完成Impact请求，真实Hit/Damage仍由Runner随后提交。
        context.GuardImpactReached = true;
        activePresentationRequestId = 0L;
        completion.TryComplete(requestId);
    }

    private void MarkDefenseGuardTailFinished(
        ActionPresentationContext context,
        BattleExecutionItem executionItem
    )
    {
        if (IsOwnedGuardPresentationContext(context, executionItem))
        {
            context.GuardTailFinished = true;
        }
    }

    private bool ShouldPlayDodgeFailedImpact(
        BattlePresentationRequest request,
        ActionPresentationContext context
    )
    {
        if (request.ExecutionItem == null ||
            !IsSupportedDodgePresentationExecution(request) ||
            request.ClashSession == null ||
            request.ClashSession.ClashType != BattleClashType.DodgeVsAttack ||
            request.ClashSession.FinalResult !=
                BattleClashFinalResult.DodgeFailed ||
            request.Impact == null || request.ImpactIndex != 0 ||
            context == null || !context.DodgeRollStarted ||
            !context.DodgeRollResultReady ||
            !TryResolveDodgePresentationActors(context) ||
            attackVsDodgePresentationPlayer == null ||
            !attackVsDodgePresentationPlayer.isActiveAndEnabled)
        {
            return false;
        }

        return object.ReferenceEquals(
                context.CurrentAttacker,
                context.DodgeAttacker) &&
            object.ReferenceEquals(
                context.CurrentTarget,
                context.DodgeDefender);
    }

    private static bool IsSupportedDodgePresentationExecution(
        BattlePresentationRequest request
    )
    {
        if (request == null || request.ExecutionItem == null)
        {
            return false;
        }

        if (request.ExecutionItem.executionType ==
            BattleExecutionItemType.RespondedEnemyIntent)
        {
            return true;
        }

        return request.ExecutionItem.executionType ==
                BattleExecutionItemType.UnrespondedEnemyIntent &&
            request.ClashSession != null &&
            request.ClashSession.IsContinuousDodgeContinuation;
    }

    private bool TryStartDodgeFailedImpact(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        float directionSign = GetAttackDirectionSign(
            context.DodgeAttackerHandle,
            context.DodgeDefenderHandle
        );
        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        context.DodgeImpactStarted = true;
        context.DodgeImpactFinished = false;
        context.DodgeImpactRequestId = requestId;
        activePresentationRequestId = requestId;

        bool started = attackVsDodgePresentationPlayer
            .TryPlayDodgeFailedImpact(
                context.DodgeDefenderPresentation,
                context.DodgeDefenderHandle.WorldRoot.transform,
                directionSign,
                () => CompleteDodgeFailedImpact(
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

        context.DodgeImpactStarted = false;
        context.DodgeImpactRequestId = 0L;
        if (activePresentationRequestId == requestId)
        {
            activePresentationRequestId = 0L;
        }
        return false;
    }

    private void CompleteDodgeFailedImpact(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (!IsOwnedDodgePresentationContext(context, executionItem) ||
            !context.DodgeImpactStarted || context.DodgeImpactFinished ||
            context.DodgeImpactRequestId != requestId ||
            activePresentationRequestId != requestId)
        {
            return;
        }

        // Hit表现完成后只释放Impact；伤害仍由Runner下一步提交。
        context.DodgeImpactFinished = true;
        activePresentationRequestId = 0L;
        completion.TryComplete(requestId);
    }

    private static bool ShouldPlayDodgeRollResult(
        BattlePresentationRequest request
    )
    {
        BattleClashSession session = request.ClashSession;
        return session != null &&
            session.ClashType == BattleClashType.DodgeVsAttack &&
            session.IsFinalized &&
            (session.FinalResult == BattleClashFinalResult.DodgeSuccess ||
                session.FinalResult == BattleClashFinalResult.DodgeFailed);
    }

    private bool TryStartDodgeRollResult(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        if (!TryResolveDodgePresentationActors(context) ||
            attackVsDodgePresentationPlayer == null ||
            !attackVsDodgePresentationPlayer.isActiveAndEnabled)
        {
            return false;
        }

        BattleClashFinalResult finalResult = request.ClashSession.FinalResult;
        context.DodgePresentationResult = finalResult ==
                BattleClashFinalResult.DodgeSuccess
            ? BattleDodgePresentationResult.DodgeSuccess
            : BattleDodgePresentationResult.DodgeFailed;

        float directionSign = GetAttackDirectionSign(
            context.DodgeAttackerHandle,
            context.DodgeDefenderHandle
        );
        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        context.DodgeRollStarted = true;
        context.DodgeRollResultReady = false;
        context.DodgeTailFinished = false;
        context.DodgeImpactStarted = false;
        context.DodgeImpactFinished = false;
        context.DodgeRollRequestId = requestId;
        activePresentationRequestId = requestId;

        BattleCardState attackCardState = GetDodgeAttackCardState(
            request.ClashSession
        );
        bool useCloseRangeShoot = attackCardState != null &&
            attackCardState.IsCloseRangeShoot();
        bool started = attackVsDodgePresentationPlayer
            .TryPlayDodgeRollResult(
                context.DodgeAttackerPresentation,
                context.DodgeDefenderPresentation,
                directionSign,
                context.DodgePresentationResult,
                useCloseRangeShoot,
                () => CompleteDodgeRollResult(
                    context,
                    executionItem,
                    requestId,
                    completion
                ),
                () => MarkDodgeTailFinished(context, executionItem)
            );
        if (started)
        {
            return true;
        }

        context.DodgeRollStarted = false;
        context.DodgeRollRequestId = 0L;
        if (activePresentationRequestId == requestId)
        {
            activePresentationRequestId = 0L;
        }
        return false;
    }

    private void CompleteDodgeRollResult(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (!IsOwnedDodgePresentationContext(context, executionItem) ||
            context.DodgeRollResultReady ||
            context.DodgeRollRequestId != requestId ||
            activePresentationRequestId != requestId)
        {
            return;
        }

        context.DodgeRollResultReady = true;
        activePresentationRequestId = 0L;
        completion.TryComplete(requestId);
    }

    private void MarkDodgeTailFinished(
        ActionPresentationContext context,
        BattleExecutionItem executionItem
    )
    {
        if (IsOwnedDodgePresentationContext(context, executionItem))
        {
            context.DodgeTailFinished = true;
        }
    }

    private void HandleDefenseActionComplete(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        if (context.GuardTailFinished)
        {
            BattleActionRollPanelHost.HideImmediate();
            ReleaseCameraForContext(context);
            CompleteRequest(request, completion);
            activeContext = null;
            return;
        }

        activePresentationRequestId = request.RequestId;
        activePresentationCoroutine = StartCoroutine(
            WaitForDefenseGuardTail(request, completion, context)
        );
    }

    private IEnumerator WaitForDefenseGuardTail(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        while (IsCurrentPresentationRequest(requestId) &&
            IsOwnedGuardPresentationContext(context, executionItem) &&
            !context.GuardTailFinished)
        {
            if (attackVsGuardPresentationPlayer == null ||
                (!attackVsGuardPresentationPlayer.IsRunning &&
                    !attackVsGuardPresentationPlayer.IsFinished))
            {
                context.GuardTailFinished = true;
                break;
            }

            yield return null;
        }

        if (!IsCurrentPresentationRequest(requestId) ||
            !IsOwnedGuardPresentationContext(context, executionItem))
        {
            yield break;
        }

        activePresentationCoroutine = null;
        activePresentationRequestId = 0L;
        BattleActionRollPanelHost.HideImmediate();
        ReleaseCameraForContext(context);
        completion.TryComplete(requestId);
        activeContext = null;
    }

    private bool IsOwnedGuardPresentationContext(
        ActionPresentationContext context,
        BattleExecutionItem executionItem
    )
    {
        return context != null &&
            object.ReferenceEquals(activeContext, context) &&
            object.ReferenceEquals(context.ExecutionItem, executionItem) &&
            context.GuardPresentationStarted &&
            !context.Cancelled;
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
            context.CurrentTargetHandle.WorldRoot != null &&
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

        bool closeRangeShootWon = request.Impact.sourceCardState != null &&
            request.Impact.sourceCardState.IsCloseRangeShoot();
        bool meleeAttackWon = request.Impact.sourceCardState != null &&
            request.Impact.sourceCardState.IsMeleeAttack();
        bool started = closeRangeShootWon
            ? attackVsAttackPresentationPlayer
                .TryPlayResolvedWinnerCloseRangeShoot(
                    context.CurrentAttackerPresentation,
                    context.CurrentTargetPresentation,
                    context.CurrentTargetHandle.WorldRoot.transform,
                    directionSign,
                    () => CompleteDefaultAttackImpact(
                        context,
                        executionItem,
                        requestId,
                        completion
                    ),
                    () => MarkDefaultAttackFinished(context, executionItem)
                )
            : attackVsAttackPresentationPlayer.TryPlayResolvedWinnerAttack(
                context.CurrentAttackerPresentation,
                context.CurrentTargetPresentation,
                context.CurrentTargetHandle.WorldRoot.transform,
                directionSign,
                meleeAttackWon
                    ? () => HandleDefaultAttackTrueVisualImpact(
                        context,
                        executionItem,
                        request.Impact.sourceCardState,
                        directionSign
                    )
                    : null,
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

    private void HandleDefaultAttackTrueVisualImpact(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        BattleCardState sourceCardState,
        float directionSign
    )
    {
        if (context == null ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem) ||
            context.Cancelled ||
            !context.CameraCinematicOwned ||
            sourceCardState == null ||
            !sourceCardState.IsMeleeAttack())
        {
            return;
        }

        ResolveBattleCameraDirector()?.TryPlayGenericHitImpact(directionSign);
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
        ReleaseCameraForContext(context);
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

    private void ValidateAttackVsGuardPresentationPlayer()
    {
        if (attackVsGuardPresentationPlayer == null)
        {
            Debug.LogError(
                "BattleSceneExecutionPresenter缺少持久化的" +
                "BattleAttackVsGuardPresentationPlayer。",
                this
            );
        }
    }

    private void ValidateAttackVsDodgePresentationPlayer()
    {
        if (attackVsDodgePresentationPlayer == null)
        {
            Debug.LogError(
                "BattleSceneExecutionPresenter缺少持久化的" +
                "BattleAttackVsDodgePresentationPlayer。",
                this
            );
        }
    }

    private void ResolveLongRangeShootVsAttackPresentationPlayer()
    {
        if (longRangeShootVsAttackPresentationPlayer == null)
        {
            longRangeShootVsAttackPresentationPlayer =
                GetComponent<BattleLongRangeShootVsAttackPresentationPlayer>();
        }

        // Player没有场景配置参数，可安全作为Presenter的运行时专用能力补齐。
        if (longRangeShootVsAttackPresentationPlayer == null)
        {
            longRangeShootVsAttackPresentationPlayer = gameObject.AddComponent<
                BattleLongRangeShootVsAttackPresentationPlayer>();
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

    private BattleCameraDirector ResolveBattleCameraDirector()
    {
        if (battleCameraDirector == null)
        {
            battleCameraDirector =
                FindFirstObjectByType<BattleCameraDirector>();
        }

        return battleCameraDirector;
    }

    private void ReleaseCameraForContext(
        ActionPresentationContext context
    )
    {
        if (context == null || !context.CameraCinematicOwned)
        {
            return;
        }

        context.CameraCinematicOwned = false;
        ResolveBattleCameraDirector()?
            .ReleaseBattleActionCinematicControl();
    }

    private void CancelOrReleaseCameraForContext(
        ActionPresentationContext context
    )
    {
        if (context == null || !context.CameraCinematicOwned)
        {
            return;
        }

        context.CameraCinematicOwned = false;
        BattleCameraDirector director = ResolveBattleCameraDirector();
        if (director == null)
        {
            return;
        }

        if (!director.CancelTwoUnitFocus(true) &&
            !director.CancelAnchoredTwoUnitApproach(true))
        {
            director.ReleaseBattleActionCinematicControl();
        }
    }

    private static void ClearAttackVsAttackParallelBeginState(
        ActionPresentationContext context
    )
    {
        if (context == null)
        {
            return;
        }

        context.AttackVsAttackParallelBeginActive = false;
        context.AttackVsAttackFocusFinished = false;
        context.AttackVsAttackApproachFinished = false;
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
