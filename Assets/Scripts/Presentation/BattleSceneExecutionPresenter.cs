using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 正式战斗表现协议：按 Cue 解析运行时角色，并逐步接入异步战斗表现。
public sealed class BattleSceneExecutionPresenter : MonoBehaviour,
    IBattleExecutionPresenter
{
    private sealed class ActionPresentationContext
    {
        public BattleExecutionItem ExecutionItem;
        public BattlePresentationInteractionContext InteractionContext;
        public BattlePresentationRoute Route;
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
        public bool DodgeRollPresentationPending;
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
        public bool DefenseVsAttackEngagementBegun;
        public bool DefenseVsAttackCameraEntryFinished;
        public bool DefenseVsAttackApproachFinished;
        public bool DodgeVsAttackAnchoredApproachActive;
        public bool DodgeVsAttackEngagementBegun;
        public bool DodgeVsAttackCameraEntryFinished;
        public bool DodgeVsAttackApproachFinished;

        public BattlePresentationRequest ActionBeginRequest;
        public BattlePresentationCompletion ActionBeginCompletion;
        public bool ActionBeginPresentationFinished;
        public bool RollPanelEntranceAttempted;
        public bool RollPanelEntranceRequired;
        public bool RollPanelEntranceFinished;

        public BattlePresentationCompletion RollResultCompletion;
        public bool RollResultPresentationFinished;
        public bool RollPanelExitRequired;
        public bool RollPanelExitFinished;

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
        public bool LongRangeResponseImpactStarted;
        public long LongRangeResponseImpactRequestId;
        public bool LongRangeCameraFocusActive;
        public bool SpecialLongRangeDuelPreRollActive;

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
    private BattleSpecialLongRangeDuelPresentationProfile
        specialLongRangeDuelPresentationProfile;
    [SerializeField]
    private BattleCameraDirector battleCameraDirector;
    [SerializeField] private bool verboseLogging = false;

    private ActionPresentationContext activeContext;
    private Coroutine activePresentationCoroutine;
    private BattleSpecialLongRangeDuelPresentationPlayer
        specialLongRangeDuelPresentationPlayer;
    private long activePresentationRequestId;
    private bool battleActionCameraCarryPending;
    private readonly List<CharacterData> previousActionParticipants =
        new List<CharacterData>();
    private readonly List<CharacterData> currentActionParticipants =
        new List<CharacterData>();
    private readonly List<CharacterData> previousOnlyActionParticipants =
        new List<CharacterData>();

    void Awake()
    {
        ResolveDefaultAttackPresentationPlayer();
        ResolveAttackVsAttackPresentationPlayer();
        ValidateAttackVsGuardPresentationPlayer();
        ValidateAttackVsDodgePresentationPlayer();
        ResolveLongRangeShootVsAttackPresentationPlayer();
        ResolveSpecialLongRangeDuelPresentationPlayer();
    }

    public void Initialize(BattleUnitViewSpawner spawner)
    {
        unitViewSpawner = spawner;
        ResolveDefaultAttackPresentationPlayer();
        ResolveAttackVsAttackPresentationPlayer();
        ValidateAttackVsGuardPresentationPlayer();
        ValidateAttackVsDodgePresentationPlayer();
        ResolveLongRangeShootVsAttackPresentationPlayer();
        ResolveSpecialLongRangeDuelPresentationPlayer();
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
            CancelSpecialLongRangeDuelForContext(activeContext);
            CancelOrReleaseCameraForContext(activeContext);
        }
        ReleasePendingBattleActionCamera();

        attackVsAttackPresentationPlayer?.CancelAndReset();
        attackVsGuardPresentationPlayer?.CancelAndReset();
        attackVsDodgePresentationPlayer?.CancelAndReset();
        longRangeShootVsAttackPresentationPlayer?.CancelAndReset();
        specialLongRangeDuelPresentationPlayer?.CancelAndReset();
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

        BattlePresentationRouter.TryCreateRoute(request, out var route);
        switch (request.Cue)
        {
            case BattlePresentationCue.ActionBegin:
                HandleActionBegin(request, completion, route);
                break;
            case BattlePresentationCue.RollResult:
                HandleRollResult(request, completion, route);
                break;
            case BattlePresentationCue.Impact:
                HandleImpact(request, completion, route);
                break;
            case BattlePresentationCue.ActionComplete:
                HandleActionComplete(request, completion, route);
                break;
            case BattlePresentationCue.ExecutionComplete:
                HandleExecutionComplete(request, completion);
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
            CancelSpecialLongRangeDuelForContext(cancellingContext);
            CancelOrReleaseCameraForContext(cancellingContext);
        }
        ReleasePendingBattleActionCamera();

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
            specialLongRangeDuelPresentationPlayer?.CancelAndReset();
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
            if (activeContext.LongRangeResponseImpactStarted &&
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
        BattlePresentationCompletion completion,
        BattlePresentationRoute route
    )
    {
        BattleActionRollPanelHost.HideImmediate();
        activeContext = CreateContext(request);
        activeContext.Route = route;
        activeContext.ActionBeginRequest = request;
        activeContext.ActionBeginCompletion = completion;
        activePresentationRequestId = request.RequestId;
        if (battleActionCameraCarryPending &&
            !IsContinuousDodgeContinuation(request))
        {
            ReleasePendingBattleActionCamera();
        }
        bool unavailableShootResponse =
            TryResolveUnavailableShootResponse(activeContext);
        LogRequest(request, activeContext);
        ApplyPreviousActionHandoff(activeContext.InteractionContext);

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

        if (route == null)
        {
            CompleteRequest(request, completion);
            return;
        }

        if (TryStartSpecialLongRangeDuelActionBegin(
                request,
                completion,
                activeContext,
                route
            ))
        {
            return;
        }

        // Ready先按实际Actor独立建立；后续距离判断只决定是否发生Movement。
        ApplyReadyState(route);

        if (route.HandlerKind ==
                BattlePresentationHandlerKind.UnilateralAttack)
        {
            if (route.AttackDelivery ==
                BattlePresentationAttackDeliveryKind.LongRangeShoot)
            {
                if (!TryPrepareUnilateralLongRangeContext(activeContext) ||
                    !TryStartUnilateralLongRangeActionBegin(
                        request,
                        completion,
                        activeContext
                    ))
                {
                    CompleteRequest(request, completion);
                }
                return;
            }

            if (!TryStartUnilateralNearRangeActionBegin(
                    request,
                    completion,
                    activeContext
                ))
            {
                CompleteRequest(request, completion);
            }
            return;
        }

        if (route.UsesLongRangeGrammar &&
            TryResolveLongRangeShootVsMelee(activeContext))
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
            }

            if (!TryStartLongRangeCameraGrammar(
                    request,
                    completion,
                    activeContext
                ))
            {
                CompleteRequest(request, completion);
            }
            return;
        }

        if (route.UsesLongRangeGrammar)
        {
            Debug.LogWarning(
                "[ScenePresenter] 当前LongRange兼容Player无法解析该配对，" +
                "本次仅安全完成表现请求。",
                this
            );
            CompleteRequest(request, completion);
            return;
        }

        if (route.HandlerKind ==
            BattlePresentationHandlerKind.AttackVsAttack)
        {
            if (route.UsesNearRangeAttackGrammar &&
                TryStartClashCameraAndApproach(
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

        if (route.HandlerKind ==
            BattlePresentationHandlerKind.AttackVsDefense)
        {
            if (route.AttackDelivery ==
                BattlePresentationAttackDeliveryKind.LongRangeShoot)
            {
                TryResolveDefensePresentationActors(activeContext);
                CompleteRequest(request, completion);
                return;
            }

            if (route.UsesNearRangeAttackGrammar &&
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

        if (route.HandlerKind ==
            BattlePresentationHandlerKind.AttackVsDodge)
        {
            if (route.AttackDelivery ==
                BattlePresentationAttackDeliveryKind.LongRangeShoot)
            {
                ReleasePendingBattleActionCamera();
                TryResolveDodgePresentationActors(activeContext);
                CompleteRequest(request, completion);
                return;
            }

            if (route.UsesNearRangeAttackGrammar &&
                TryStartDodgeVsAttackAnchoredApproach(
                    request,
                    completion,
                    activeContext
                ))
            {
                return;
            }

            ReleasePendingBattleActionCamera();

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

    private bool TryStartSpecialLongRangeDuelActionBegin(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context,
        BattlePresentationRoute route
    )
    {
        if (!TryResolveSpecialLongRangeDuelActors(
                context,
                route,
                out BattleUnitViewHandle shooterHandle,
                out BattleCharacterPresentationController shooterPresentation,
                out BattleUnitViewHandle opponentHandle,
                out BattleCharacterPresentationController opponentPresentation
            ))
        {
            return false;
        }

        ResolveSpecialLongRangeDuelPresentationPlayer();
        BattleCameraDirector director = ResolveBattleCameraDirector();
        if (specialLongRangeDuelPresentationPlayer == null ||
            specialLongRangeDuelPresentationProfile == null ||
            director == null)
        {
            Debug.LogWarning(
                "[ScenePresenter] SpecialLongRangeDuel ActionBegin缺少" +
                "Player、Profile或Camera，回退现有ActionBegin。",
                this
            );
            return false;
        }

        BattleExecutionItem executionItem = context.ExecutionItem;
        long requestId = request.RequestId;
        context.SpecialLongRangeDuelPreRollActive = true;
        context.CameraCinematicOwned = true;
        bool started = specialLongRangeDuelPresentationPlayer.TryPlay(
            shooterHandle,
            shooterPresentation,
            opponentHandle,
            opponentPresentation,
            director,
            specialLongRangeDuelPresentationProfile,
            () => CompleteSpecialLongRangeDuelActionBegin(
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

        context.SpecialLongRangeDuelPreRollActive = false;
        context.CameraCinematicOwned = false;
        specialLongRangeDuelPresentationPlayer.CancelAndReset();
        director.CancelExternallyDrivenSingleActorApproach(true);
        Debug.LogWarning(
            "[ScenePresenter] SpecialLongRangeDuel ActionBegin启动失败，" +
            "回退现有ActionBegin。",
            this
        );
        return false;
    }

    private bool TryResolveSpecialLongRangeDuelActors(
        ActionPresentationContext context,
        BattlePresentationRoute route,
        out BattleUnitViewHandle shooterHandle,
        out BattleCharacterPresentationController shooterPresentation,
        out BattleUnitViewHandle opponentHandle,
        out BattleCharacterPresentationController opponentPresentation
    )
    {
        shooterHandle = null;
        shooterPresentation = null;
        opponentHandle = null;
        opponentPresentation = null;
        BattlePresentationInteractionContext interaction = context != null
            ? context.InteractionContext
            : null;
        if (interaction == null || route == null)
        {
            return false;
        }

        if (route.HandlerKind ==
            BattlePresentationHandlerKind.AttackVsAttack)
        {
            BattleExecutionAction sideA = interaction.AttackActionA;
            BattleExecutionAction sideB = interaction.AttackActionB;
            bool sideASpecial = IsSpecialLongRangeDuelAction(sideA);
            bool sideBSpecial = IsSpecialLongRangeDuelAction(sideB);
            if (sideASpecial == sideBSpecial)
            {
                return false;
            }

            BattleExecutionAction opponentAction = sideASpecial
                ? sideB
                : sideA;
            if (opponentAction == null ||
                opponentAction.cardState == null ||
                !opponentAction.cardState.IsMeleeAttack() ||
                !TryResolveLongRangeShootVsMelee(context))
            {
                return false;
            }

            PrepareLongRangeEngagement(context);
            if (context.ClashEngagement == null ||
                !HasCompleteLongRangePresentationMapping(context))
            {
                Debug.LogWarning(
                    "[ScenePresenter] SpecialLongRangeDuel无法建立现有" +
                    "LongRange Context，回退现有ActionBegin。",
                    this
                );
                return false;
            }

            shooterHandle = context.LongRangeShooterHandle;
            shooterPresentation = context.LongRangeShooterPresentation;
            opponentHandle = context.LongRangeMeleeHandle;
            opponentPresentation = context.LongRangeMeleePresentation;
            return true;
        }

        if (route.HandlerKind ==
            BattlePresentationHandlerKind.AttackVsDefense)
        {
            BattleExecutionAction attackAction = interaction.AttackAction;
            if (!IsSpecialLongRangeDuelAction(attackAction) ||
                interaction.DefenseAction == null ||
                !TryResolveDefensePresentationActors(context))
            {
                return false;
            }

            shooterHandle = context.DefenseAttackerHandle;
            shooterPresentation = context.DefenseAttackerPresentation;
            opponentHandle = context.DefenseDefenderHandle;
            opponentPresentation = context.DefenseDefenderPresentation;
            return true;
        }

        return false;
    }

    private static bool IsSpecialLongRangeDuelAction(
        BattleExecutionAction action
    )
    {
        return action != null && action.cardState != null &&
            action.cardState.IsLongRangeShoot() &&
            action.cardState.IsSpecialLongRangeDuelPresentation();
    }

    private void CompleteSpecialLongRangeDuelActionBegin(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (context == null ||
            !context.SpecialLongRangeDuelPreRollActive ||
            !IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem))
        {
            return;
        }

        context.SpecialLongRangeDuelPreRollActive = false;
        BeginRollPanelEntrance(context, executionItem, requestId);
        MarkActionBeginPresentationFinished(
            context,
            requestId,
            completion
        );
    }

    private void ApplyReadyState(BattlePresentationRoute route)
    {
        BattlePresentationReadyContract readyContract =
            BattlePresentationReadyPolicy.Create(route);
        ApplyReadyDirective(readyContract.Primary);
        ApplyReadyDirective(readyContract.Secondary);
    }

    private void ApplyReadyDirective(
        BattlePresentationReadyDirective directive
    )
    {
        if (directive == null || !directive.ShouldApplyReady)
        {
            return;
        }

        ResolvePresentation(
            directive.Actor,
            out _,
            out BattleCharacterPresentationController presentation
        );
        if (presentation == null)
        {
            return;
        }

        switch (directive.PoseKind)
        {
            case BattlePresentationReadyPoseKind.Idle:
                presentation.SetIdle();
                break;
            case BattlePresentationReadyPoseKind.Sprint:
                presentation.SetSprint();
                break;
            case BattlePresentationReadyPoseKind.Aim:
                presentation.SetAim();
                break;
            case BattlePresentationReadyPoseKind.Guard:
                presentation.SetGuard();
                break;
            case BattlePresentationReadyPoseKind.Dodge:
                presentation.SetDodge();
                break;
        }
    }

    private void ApplyPreviousActionHandoff(
        BattlePresentationInteractionContext currentInteraction
    )
    {
        CollectActionParticipants(
            currentInteraction,
            currentActionParticipants
        );
        CollectPreviousOnlyParticipants(
            previousActionParticipants,
            currentActionParticipants,
            previousOnlyActionParticipants
        );

        foreach (CharacterData actor in previousOnlyActionParticipants)
        {
            ResolvePresentation(
                actor,
                out _,
                out BattleCharacterPresentationController presentation
            );
            presentation?.SetIdle();
        }
    }

    private void CapturePreviousActionParticipants(
        ActionPresentationContext context
    )
    {
        CollectActionParticipants(
            context != null ? context.InteractionContext : null,
            previousActionParticipants
        );
    }

    private void ClearPreviousActionParticipantTracking()
    {
        ClearActionParticipantTracking(
            previousActionParticipants,
            currentActionParticipants,
            previousOnlyActionParticipants
        );
    }

    internal static void CollectActionParticipants(
        BattlePresentationInteractionContext interaction,
        List<CharacterData> participants
    )
    {
        if (participants == null)
        {
            return;
        }

        participants.Clear();
        if (interaction == null)
        {
            return;
        }

        if (interaction.InteractionType == BattleInteractionType.AttackVsAttack)
        {
            AddActionParticipant(participants, interaction.AttackActionA);
            AddActionParticipant(participants, interaction.AttackActionB);
            return;
        }

        if (interaction.InteractionType == BattleInteractionType.AttackVsDefense)
        {
            AddActionParticipant(participants, interaction.AttackAction);
            AddActionParticipant(participants, interaction.DefenseAction);
            return;
        }

        if (interaction.InteractionType == BattleInteractionType.AttackVsDodge)
        {
            AddActionParticipant(participants, interaction.AttackAction);
            AddActionParticipant(participants, interaction.DodgeAction);
            return;
        }

        if (interaction.InteractionType == BattleInteractionType.UnilateralAttack)
        {
            AddActionParticipant(participants, interaction.AttackAction);
            AddParticipant(participants, interaction.Target);
            return;
        }

        AddActionParticipant(participants, interaction.SideA);
        AddActionParticipant(participants, interaction.SideB);
    }

    internal static void CollectPreviousOnlyParticipants(
        List<CharacterData> previousParticipants,
        List<CharacterData> currentParticipants,
        List<CharacterData> previousOnlyParticipants
    )
    {
        if (previousOnlyParticipants == null)
        {
            return;
        }

        previousOnlyParticipants.Clear();
        if (previousParticipants == null)
        {
            return;
        }

        foreach (CharacterData actor in previousParticipants)
        {
            if (!ContainsParticipant(currentParticipants, actor))
            {
                AddParticipant(previousOnlyParticipants, actor);
            }
        }
    }

    internal static void ClearActionParticipantTracking(
        List<CharacterData> previousParticipants,
        List<CharacterData> currentParticipants,
        List<CharacterData> previousOnlyParticipants
    )
    {
        previousParticipants?.Clear();
        currentParticipants?.Clear();
        previousOnlyParticipants?.Clear();
    }

    private static void AddActionParticipant(
        List<CharacterData> participants,
        BattleExecutionAction action
    )
    {
        if (action != null)
        {
            AddParticipant(participants, action.actor);
        }
    }

    private static void AddParticipant(
        List<CharacterData> participants,
        CharacterData actor
    )
    {
        if (participants == null || actor == null ||
            ContainsParticipant(participants, actor))
        {
            return;
        }

        participants.Add(actor);
    }

    private static bool ContainsParticipant(
        List<CharacterData> participants,
        CharacterData actor
    )
    {
        if (participants == null || actor == null)
        {
            return false;
        }

        foreach (CharacterData participant in participants)
        {
            if (object.ReferenceEquals(participant, actor))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNearRangeAttack(BattleCardState cardState)
    {
        return cardState != null &&
            (cardState.IsMeleeAttack() || cardState.IsCloseRangeShoot());
    }

    private static BattlePresentationAttackDeliveryKind ResolveAttackDelivery(
        BattleCardState cardState
    )
    {
        if (cardState != null && cardState.IsLongRangeShoot())
        {
            return BattlePresentationAttackDeliveryKind.LongRangeShoot;
        }
        if (cardState != null && cardState.IsCloseRangeShoot())
        {
            return BattlePresentationAttackDeliveryKind.CloseRangeShoot;
        }
        return BattlePresentationAttackDeliveryKind.Melee;
    }

    private static bool TryStartOneSidedAttackCameraGrammar(
        BattleCameraDirector director,
        BattleUnitViewHandle attacker,
        BattleUnitViewHandle target,
        float finalGap,
        System.Action engagementBegun,
        System.Action<bool> completion,
        bool establishBattleFocusPose = false
    )
    {
        return director != null &&
            director.TryPlayImmediateSingleActorApproachFollow(
                attacker,
                target,
                finalGap,
                engagementBegun,
                completion,
                establishBattleFocusPose
            );
    }

    private bool TryStartUnilateralNearRangeActionBegin(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        BattleExecutionAction attackAction = context.InteractionContext != null
            ? context.InteractionContext.AttackAction
            : null;
        if (attackAction == null || attackAction.actor == null ||
            attackAction.target == null)
        {
            return false;
        }

        context.SideAActor = attackAction.actor;
        ResolvePresentation(
            context.SideAActor,
            out context.SideAHandle,
            out context.SideAPresentation
        );
        context.SideBActor = attackAction.target;
        ResolvePresentation(
            context.SideBActor,
            out context.SideBHandle,
            out context.SideBPresentation
        );
        context.CurrentAttacker = context.SideAActor;
        context.CurrentAttackerHandle = context.SideAHandle;
        context.CurrentAttackerPresentation = context.SideAPresentation;
        context.CurrentTarget = context.SideBActor;
        context.CurrentTargetHandle = context.SideBHandle;
        context.CurrentTargetPresentation = context.SideBPresentation;

        if (clashEngagementProfile == null ||
            !HasCompleteClashPresentationMapping(context) ||
            attackVsAttackPresentationPlayer == null)
        {
            return false;
        }

        context.ClashEngagement =
            ResolveAttackVsAttackClashEngagement(context);
        if (context.ClashEngagement == null)
        {
            return false;
        }

        long requestId = request.RequestId;
        activePresentationRequestId = requestId;
        activePresentationCoroutine = StartCoroutine(
            RunFreeMeleeSingleActorApproach(
                context,
                request.ExecutionItem,
                requestId,
                completion
            )
        );
        return activePresentationCoroutine != null;
    }

    private bool TryPrepareUnilateralLongRangeContext(
        ActionPresentationContext context
    )
    {
        BattleExecutionAction attackAction = context != null &&
            context.InteractionContext != null
                ? context.InteractionContext.AttackAction
                : null;
        if (attackAction == null || attackAction.actor == null ||
            attackAction.target == null)
        {
            return false;
        }

        context.LongRangeShooterSide = null;
        context.LongRangeMeleeSide = null;
        context.LongRangeShooter = attackAction.actor;
        context.LongRangeMeleeActor = attackAction.target;
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
        context.SideAActor = context.LongRangeShooter;
        context.SideAHandle = context.LongRangeShooterHandle;
        context.SideAPresentation = context.LongRangeShooterPresentation;
        context.SideBActor = context.LongRangeMeleeActor;
        context.SideBHandle = context.LongRangeMeleeHandle;
        context.SideBPresentation = context.LongRangeMeleePresentation;
        context.LongRangeShotAvailable = true;
        context.LongRangeShooterWon = true;
        return HasCompleteLongRangePresentationMapping(context);
    }

    private bool TryStartUnilateralLongRangeActionBegin(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        PrepareLongRangeEngagement(context);
        if (longRangeShootVsAttackPresentationPlayer != null)
        {
            longRangeShootVsAttackPresentationPlayer.TryApplyActionBeginAim(
                context.LongRangeShooterPresentation,
                true
            );
        }

        return TryStartLongRangeCameraGrammar(
            request,
            completion,
            context
        );
    }

    private IEnumerator RunFreeMeleeSingleActorApproach(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        BattleCameraDirector director = ResolveBattleCameraDirector();
        bool cameraReady = false;
        bool cameraSucceeded = false;
        bool cameraStarted = TryStartOneSidedAttackCameraGrammar(
            director,
            context.CurrentAttackerHandle,
            context.CurrentTargetHandle,
            context.ClashEngagement.FinalGap,
            () => BeginRollPanelEntrance(
                context,
                executionItem,
                requestId
            ),
            success =>
            {
                cameraSucceeded = success;
                cameraReady = true;
            },
            true
        );
        if (cameraStarted)
        {
            context.CameraCinematicOwned = true;
        }

        LogApproachStarted(requestId, context);
        yield return BattleClashReadyApproachMotion.PlaySingleActorApproach(
            context.CurrentAttackerPresentation,
            context.CurrentAttackerHandle.WorldRoot.transform,
            context.CurrentTargetPresentation,
            context.CurrentTargetHandle.WorldRoot.transform,
            context.ClashEngagement.FinalGap,
            attackVsAttackPresentationPlayer.SprintDuration,
            attackVsAttackPresentationPlayer.AfterimageSpawnInterval,
            () => IsCurrentPresentationRequest(requestId) &&
                object.ReferenceEquals(activeContext, context) &&
                object.ReferenceEquals(context.ExecutionItem, executionItem)
        );

        while (cameraStarted && !cameraReady &&
            IsCurrentPresentationRequest(requestId) &&
            object.ReferenceEquals(activeContext, context) &&
            object.ReferenceEquals(context.ExecutionItem, executionItem))
        {
            yield return null;
        }

        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem))
        {
            yield break;
        }

        if (cameraStarted)
        {
            if (cameraSucceeded)
            {
                director?.FinishAnchoredTwoUnitApproachTracking();
            }
            else
            {
                director?.CancelAnchoredTwoUnitApproach(false);
                context.CameraCinematicOwned = false;
            }
        }

        activePresentationCoroutine = null;
        LogApproachCompleted(requestId);
        MarkActionBeginPresentationFinished(
            context,
            requestId,
            completion
        );
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
            IsLongRangeOpponentCard(session.SideB.cardState))
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
            IsLongRangeOpponentCard(session.SideA.cardState))
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

    private static bool IsLongRangeOpponentCard(BattleCardState cardState)
    {
        return cardState != null && cardState.cardData != null &&
            (cardState.IsMeleeAttack() ||
                cardState.cardData.cardType == CardType.Defense ||
                cardState.cardData.cardType == CardType.Dodge);
    }

    private static bool IsLongRangeResponseWinnerCard(BattleCardState cardState)
    {
        return cardState != null && cardState.cardData != null &&
            (cardState.cardData.cardType == CardType.Defense ||
                cardState.cardData.cardType == CardType.Dodge);
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

    private bool TryStartLongRangeCameraGrammar(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        if (!HasCompleteLongRangePresentationMapping(context))
        {
            return false;
        }

        BattleCameraDirector director = ResolveBattleCameraDirector();
        if (director == null)
        {
            return false;
        }

        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        context.LongRangeCameraFocusActive = true;
        context.CameraCinematicOwned = true;
        activePresentationRequestId = requestId;

        bool started = director.TryPlayTwoUnitFocus(
            context.LongRangeShooterHandle,
            context.LongRangeMeleeHandle,
            false,
            () => CompleteLongRangeCameraFocus(
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

        context.LongRangeCameraFocusActive = false;
        context.CameraCinematicOwned = false;
        return false;
    }

    private void CompleteLongRangeCameraFocus(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem) ||
            !context.LongRangeCameraFocusActive)
        {
            return;
        }

        context.LongRangeCameraFocusActive = false;
        BeginRollPanelEntrance(context, executionItem, requestId);
        MarkActionBeginPresentationFinished(
            context,
            requestId,
            completion
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
        bool playDeflectionSlash = !context.LongRangeShooterWon &&
            context.LongRangeMeleeSide.cardState != null &&
            context.LongRangeMeleeSide.cardState.IsMeleeAttack();
        bool started = longRangeShootVsAttackPresentationPlayer
            .TryPlayTerminalClash(
                context.LongRangeShooterPresentation,
                context.LongRangeMeleePresentation,
                playDeflectionSlash,
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

        MarkRollResultPresentationFinished(
            context,
            requestId,
            completion
        );
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

        LogApproachFallback(requestId, "共享AttackVsAttack Player启动失败");
        return false;
    }

    private bool TryStartClashCameraAndApproach(
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
        BeginRollPanelEntrance(context, executionItem, requestId);
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
        MarkActionBeginPresentationFinished(
            context,
            requestId,
            completion
        );
    }

    private void CompleteLongRangeResponseImpact(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem) ||
            !context.LongRangeResponseImpactStarted ||
            context.LongRangeResponseImpactRequestId != requestId)
        {
            return;
        }

        context.LongRangeResponseImpactStarted = false;
        context.LongRangeResponseImpactRequestId = 0L;
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

        LogApproachCompleted(requestId);
        MarkActionBeginPresentationFinished(
            context,
            requestId,
            completion
        );
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

    private static BattleCardState GetDefenseAttackCardState(
        BattleClashSession session
    )
    {
        if (session == null ||
            session.ClashType != BattleClashType.DefenseVsAttack)
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
        if (context.ClashEngagement == null)
        {
            return false;
        }

        BattleCardState attackCardState = context.InteractionContext != null &&
            context.InteractionContext.AttackAction != null
                ? context.InteractionContext.AttackAction.cardState
                : GetDefenseAttackCardState(request.ClashSession);
        bool useCloseRangeShoot = attackCardState != null &&
            attackCardState.IsCloseRangeShoot();
        float finalGap = context.ClashEngagement.FinalGap;
        context.DefenseVsAttackAnchoredApproachActive = true;
        context.DefenseVsAttackEngagementBegun = false;
        context.DefenseVsAttackCameraEntryFinished = false;
        context.DefenseVsAttackApproachFinished = false;
        activePresentationRequestId = requestId;
        float engagementTriggerSeparation =
            director.EngagementBlendStartSeparation;

        bool cameraStarted = TryStartOneSidedAttackCameraGrammar(
            director,
            context.DefenseAttackerHandle,
            context.DefenseDefenderHandle,
            finalGap,
            () => MarkDefenseVsAttackEngagementBegun(
                context,
                executionItem,
                requestId
            ),
            success => MarkDefenseVsAttackCameraEntryFinished(
                context,
                executionItem,
                requestId,
                completion,
                success
            ),
            true
        );
        if (!cameraStarted)
        {
            ClearDefenseVsAttackApproachState(context);
            return false;
        }

        context.CameraCinematicOwned = true;
        LogApproachStarted(requestId, context);
        bool approachStarted = useCloseRangeShoot
            ? attackVsGuardPresentationPlayer
                .TryPlayCloseRangeClashReadyApproach(
                    context.DefenseDefenderPresentation,
                    context.DefenseDefenderHandle.WorldRoot.transform,
                    context.DefenseAttackerPresentation,
                    context.DefenseAttackerHandle.WorldRoot.transform,
                    context.ClashEngagement,
                    engagementTriggerSeparation,
                    () => MarkDefenseVsAttackApproachFinished(
                        context,
                        executionItem,
                        requestId,
                        completion
                    )
                )
            : attackVsGuardPresentationPlayer.TryPlayClashReadyApproach(
                context.DefenseDefenderPresentation,
                context.DefenseDefenderHandle.WorldRoot.transform,
                context.DefenseAttackerPresentation,
                context.DefenseAttackerHandle.WorldRoot.transform,
                context.ClashEngagement,
                true,
                engagementTriggerSeparation,
                () => MarkDefenseVsAttackApproachFinished(
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

        ResolveBattleCameraDirector()?.CancelAnchoredTwoUnitApproach(true);
        context.CameraCinematicOwned = false;
        ClearDefenseVsAttackApproachState(context);
        LogApproachFallback(requestId, "共享AttackVsGuard Player启动失败");
        return false;
    }

    private void MarkDefenseVsAttackEngagementBegun(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId
    )
    {
        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem) ||
            !context.DefenseVsAttackAnchoredApproachActive ||
            context.DefenseVsAttackEngagementBegun)
        {
            return;
        }

        // 单一Engagement Begin边界，后续二级拼点UI可绑定在这里。
        context.DefenseVsAttackEngagementBegun = true;
        BeginRollPanelEntrance(context, executionItem, requestId);
    }

    private void MarkDefenseVsAttackCameraEntryFinished(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion,
        bool success
    )
    {
        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem) ||
            !context.DefenseVsAttackAnchoredApproachActive)
        {
            return;
        }

        if (!success)
        {
            attackVsGuardPresentationPlayer?.CancelAndReset();
            context.CameraCinematicOwned = false;
            ClearDefenseVsAttackApproachState(context);
            LogApproachFallback(requestId, "Guard Camera Entry启动失败");
            MarkActionBeginPresentationFinished(
                context,
                requestId,
                completion
            );
            return;
        }

        context.DefenseVsAttackCameraEntryFinished = true;
        TryCompleteDefenseVsAttackAnchoredApproach(
            context,
            executionItem,
            requestId,
            completion
        );
    }

    private void MarkDefenseVsAttackApproachFinished(
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

        context.DefenseVsAttackApproachFinished = true;
        TryCompleteDefenseVsAttackAnchoredApproach(
            context,
            executionItem,
            requestId,
            completion
        );
    }

    private void TryCompleteDefenseVsAttackAnchoredApproach(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (!context.DefenseVsAttackCameraEntryFinished ||
            !context.DefenseVsAttackApproachFinished ||
            !IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem) ||
            !context.DefenseVsAttackAnchoredApproachActive)
        {
            return;
        }

        ResolveBattleCameraDirector()?
            .FinishAnchoredTwoUnitApproachTracking();
        ClearDefenseVsAttackApproachState(context);
        LogApproachCompleted(requestId);
        MarkActionBeginPresentationFinished(
            context,
            requestId,
            completion
        );
    }

    private static void ClearDefenseVsAttackApproachState(
        ActionPresentationContext context
    )
    {
        if (context == null)
        {
            return;
        }

        context.DefenseVsAttackAnchoredApproachActive = false;
        context.DefenseVsAttackEngagementBegun = false;
        context.DefenseVsAttackCameraEntryFinished = false;
        context.DefenseVsAttackApproachFinished = false;
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
        BattleCardState attackCardState = context.InteractionContext != null &&
            context.InteractionContext.AttackAction != null
                ? context.InteractionContext.AttackAction.cardState
                : GetDefenseAttackCardState(request.ClashSession);
        bool useCloseRangeShoot = attackCardState != null &&
            attackCardState.IsCloseRangeShoot();
        activePresentationRequestId = requestId;
        bool started = useCloseRangeShoot
            ? attackVsGuardPresentationPlayer
                .TryPlayCloseRangeClashReadyApproach(
                    context.DefenseDefenderPresentation,
                    context.DefenseDefenderHandle.WorldRoot.transform,
                    context.DefenseAttackerPresentation,
                    context.DefenseAttackerHandle.WorldRoot.transform,
                    context.ClashEngagement,
                    0f,
                    () => CompleteDefenseVsAttackApproach(
                        context,
                        executionItem,
                        requestId,
                        completion
                    )
                )
            : attackVsGuardPresentationPlayer.TryPlayClashReadyApproach(
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

        MarkActionBeginPresentationFinished(
            context,
            requestId,
            completion
        );
    }

    private static bool IsContinuousDodgeContinuation(
        BattlePresentationRequest request
    )
    {
        return request != null && request.InteractionContext != null &&
            request.InteractionContext.InteractionType ==
                BattleInteractionType.AttackVsDodge &&
            request.InteractionContext.ContinuationPolicy ==
                BattlePresentationContinuationPolicy.PreserveDodgePose;
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
        bool useCloseRangeShoot = attackCardState != null &&
            attackCardState.IsCloseRangeShoot();
        activePresentationRequestId = requestId;
        bool started = useCloseRangeShoot
            ? attackVsDodgePresentationPlayer
                .TryPlaySingleActorClashReadyApproach(
                    context.DodgeDefenderPresentation,
                    context.DodgeDefenderHandle.WorldRoot.transform,
                    context.DodgeAttackerPresentation,
                    context.DodgeAttackerHandle.WorldRoot.transform,
                    context.ClashEngagement,
                    0f,
                    () => CompleteDodgeVsAttackApproach(
                        context,
                        executionItem,
                        requestId,
                        completion
                    )
                )
            : attackVsDodgePresentationPlayer.TryPlayClashReadyApproach(
                context.DodgeDefenderPresentation,
                context.DodgeDefenderHandle.WorldRoot.transform,
                context.DodgeAttackerPresentation,
                context.DodgeAttackerHandle.WorldRoot.transform,
                context.ClashEngagement,
                false,
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

        MarkActionBeginPresentationFinished(
            context,
            requestId,
            completion
        );
    }

    private bool TryResolveDefensePresentationActors(
        ActionPresentationContext context
    )
    {
        BattlePresentationInteractionContext interaction = context != null
            ? context.InteractionContext
            : null;
        if (interaction != null && interaction.AttackAction != null &&
            interaction.DefenseAction != null)
        {
            context.DefenseAttacker = interaction.AttackAction.actor;
            context.DefenseDefender = interaction.DefenseAction.actor;
            ResolvePresentation(
                context.DefenseAttacker,
                out context.DefenseAttackerHandle,
                out context.DefenseAttackerPresentation
            );
            ResolvePresentation(
                context.DefenseDefender,
                out context.DefenseDefenderHandle,
                out context.DefenseDefenderPresentation
            );
            return HasCompleteDefensePresentationMapping(context);
        }

        // 旧请求若没有冻结的中立Context，保留Session解析作为兼容兜底。
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

        return HasCompleteDefensePresentationMapping(context);
    }

    private static bool HasCompleteDefensePresentationMapping(
        ActionPresentationContext context
    )
    {
        return context != null && context.DefenseAttacker != null &&
            context.DefenseDefender != null &&
            context.DefenseAttackerHandle != null &&
            context.DefenseDefenderHandle != null &&
            context.DefenseAttackerHandle.WorldRoot != null &&
            context.DefenseDefenderHandle.WorldRoot != null &&
            context.DefenseAttackerPresentation != null &&
            context.DefenseDefenderPresentation != null;
    }

    private bool TryResolveDodgePresentationActors(
        ActionPresentationContext context
    )
    {
        BattlePresentationInteractionContext interaction = context != null
            ? context.InteractionContext
            : null;
        if (interaction != null && interaction.AttackAction != null &&
            interaction.DodgeAction != null)
        {
            context.DodgeAttacker = interaction.AttackAction.actor;
            context.DodgeDefender = interaction.DodgeAction.actor;
            ResolvePresentation(
                context.DodgeAttacker,
                out context.DodgeAttackerHandle,
                out context.DodgeAttackerPresentation
            );
            ResolvePresentation(
                context.DodgeDefender,
                out context.DodgeDefenderHandle,
                out context.DodgeDefenderPresentation
            );
            return HasCompleteDodgePresentationMapping(context);
        }

        // 旧请求若没有冻结的中立Context，保留Session解析作为兼容兜底。
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

        return HasCompleteDodgePresentationMapping(context);
    }

    private static bool HasCompleteDodgePresentationMapping(
        ActionPresentationContext context
    )
    {
        return context != null && context.DodgeAttacker != null &&
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
        BattlePresentationCompletion completion,
        BattlePresentationRoute route
    )
    {
        ActionPresentationContext context = EnsureContext(request);
        RefreshRequestState(context, request);
        context.Route = route;
        activePresentationRequestId = request.RequestId;
        context.DodgeRollPresentationPending = route != null &&
            route.HandlerKind ==
                BattlePresentationHandlerKind.AttackVsDodge &&
            IsDodgeResult(route.ResultKind);
        RefreshClashActors(context);
        LogRequest(request, context);
        PrepareRollPanelResultLifecycle(
            request,
            completion,
            route,
            context
        );

        if (route == null)
        {
            CompleteRequest(request, completion);
            return;
        }

        if (route.UsesLongRangeGrammar)
        {
            HandleLongRangeRollResult(request, completion, context);
            return;
        }

        if (route.GrammarKind ==
                BattlePresentationGrammarKind.CloseRangeClash &&
            route.ResultKind == BattlePresentationResultKind.AttackTie)
        {
            // CloseRange平点保持双方Sprint，等待下一次Manual Roll。
            CompleteRequest(request, completion);
            return;
        }

        if (route.HandlerKind ==
                BattlePresentationHandlerKind.AttackVsDodge &&
            IsDodgeResult(route.ResultKind))
        {
            TryStartPendingDodgeRollResult(
                context,
                request.RequestId
            );
            return;
        }

        if (route.HandlerKind ==
                BattlePresentationHandlerKind.AttackVsDefense &&
            TryCacheGuardPresentationResult(route.ResultKind, context))
        {
            CompleteRequest(request, completion);
            return;
        }

        if (!ShouldPlayAnimatedAttackTie(route))
        {
            CompleteRequest(request, completion);
            return;
        }

        if (!TryStartAttackTieResult(request, completion, context))
        {
            CompleteRequest(request, completion);
        }
    }

    internal static bool ShouldPlayAnimatedAttackTie(
        BattlePresentationRoute route
    )
    {
        return route != null && ShouldPlayAnimatedAttackTie(
            route.HandlerKind,
            route.GrammarKind,
            route.ResultKind
        );
    }

    internal static bool ShouldPlayAnimatedAttackTie(
        BattlePresentationHandlerKind handlerKind,
        BattlePresentationGrammarKind grammarKind,
        BattlePresentationResultKind resultKind
    )
    {
        return handlerKind ==
                BattlePresentationHandlerKind.AttackVsAttack &&
            grammarKind == BattlePresentationGrammarKind.MeleeClash &&
            resultKind == BattlePresentationResultKind.AttackTie;
    }

    private static bool TryCacheGuardPresentationResult(
        BattlePresentationResultKind resultKind,
        ActionPresentationContext context
    )
    {
        if (context == null)
        {
            return false;
        }

        if (resultKind ==
            BattlePresentationResultKind.DefenseFullBlock)
        {
            context.GuardPresentationResult =
                BattleGuardPresentationResult.FullBlock;
        }
        else if (resultKind ==
            BattlePresentationResultKind.DefenseReducedDamage)
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

    private static bool IsDodgeResult(
        BattlePresentationResultKind resultKind
    )
    {
        return resultKind == BattlePresentationResultKind.DodgeSuccess ||
            resultKind == BattlePresentationResultKind.DodgeFailed;
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

        return false;
    }

    private bool TryStartDodgeVsAttackAnchoredApproach(
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

        BattleCameraDirector director = ResolveBattleCameraDirector();
        if (director == null)
        {
            return false;
        }

        context.ClashEngagement = BattleClashEngagementResolver.Resolve(
            clashEngagementProfile,
            context.DodgeDefenderPresentation.PresentationKey,
            context.DodgeAttackerPresentation.PresentationKey,
            GetPresentationSpeed(context.DodgeDefender),
            GetPresentationSpeed(context.DodgeAttacker)
        );
        if (context.ClashEngagement == null)
        {
            return false;
        }

        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        float engagementTriggerSeparation =
            director.EngagementBlendStartSeparation;
        float finalGap = context.ClashEngagement.FinalGap;
        context.DodgeVsAttackAnchoredApproachActive = true;
        context.DodgeVsAttackEngagementBegun = false;
        context.DodgeVsAttackCameraEntryFinished = false;
        context.DodgeVsAttackApproachFinished = false;
        activePresentationRequestId = requestId;

        bool cameraStarted = TryStartOneSidedAttackCameraGrammar(
            director,
            context.DodgeAttackerHandle,
            context.DodgeDefenderHandle,
            finalGap,
            () => MarkDodgeVsAttackEngagementBegun(
                context,
                executionItem,
                requestId
            ),
            success => MarkDodgeVsAttackCameraEntryFinished(
                context,
                executionItem,
                requestId,
                completion,
                success
            ),
            true
        );
        if (!cameraStarted)
        {
            ClearDodgeVsAttackApproachState(context);
            return false;
        }

        battleActionCameraCarryPending = false;
        context.CameraCinematicOwned = true;
        LogApproachStarted(requestId, context);
        BattleCardState attackCardState = context.InteractionContext != null &&
            context.InteractionContext.AttackAction != null
                ? context.InteractionContext.AttackAction.cardState
                : GetDodgeAttackCardState(request.ClashSession);
        bool useCloseRangeShoot = attackCardState != null &&
            attackCardState.IsCloseRangeShoot();
        bool approachStarted = useCloseRangeShoot
            ? attackVsDodgePresentationPlayer
                .TryPlaySingleActorClashReadyApproach(
                    context.DodgeDefenderPresentation,
                    context.DodgeDefenderHandle.WorldRoot.transform,
                    context.DodgeAttackerPresentation,
                    context.DodgeAttackerHandle.WorldRoot.transform,
                    context.ClashEngagement,
                    engagementTriggerSeparation,
                    () => MarkDodgeVsAttackApproachFinished(
                        context,
                        executionItem,
                        requestId,
                        completion
                    )
                )
            : attackVsDodgePresentationPlayer.TryPlayMeleeClashReadyApproach(
                context.DodgeDefenderPresentation,
                context.DodgeDefenderHandle.WorldRoot.transform,
                context.DodgeAttackerPresentation,
                context.DodgeAttackerHandle.WorldRoot.transform,
                context.ClashEngagement,
                engagementTriggerSeparation,
                () => MarkDodgeVsAttackApproachFinished(
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
        ClearDodgeVsAttackApproachState(context);
        LogApproachFallback(requestId, "普通近战Dodge Approach启动失败");
        return false;
    }

    private void MarkDodgeVsAttackEngagementBegun(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId
    )
    {
        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem) ||
            !context.DodgeVsAttackAnchoredApproachActive ||
            context.DodgeVsAttackEngagementBegun)
        {
            return;
        }

        // 单一Dodge Engagement Begin边界；后续Roll Panel和专属镜头可绑定在这里。
        context.DodgeVsAttackEngagementBegun = true;
        BeginRollPanelEntrance(context, executionItem, requestId);
    }

    private void MarkDodgeVsAttackCameraEntryFinished(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion,
        bool success
    )
    {
        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem) ||
            !context.DodgeVsAttackAnchoredApproachActive)
        {
            return;
        }

        if (!success)
        {
            attackVsDodgePresentationPlayer?.CancelAndReset();
            context.CameraCinematicOwned = false;
            ClearDodgeVsAttackApproachState(context);
            LogApproachFallback(requestId, "Dodge Camera Entry启动失败");
            MarkActionBeginPresentationFinished(
                context,
                requestId,
                completion
            );
            return;
        }

        context.DodgeVsAttackCameraEntryFinished = true;
        TryCompleteDodgeVsAttackAnchoredApproach(
            context,
            executionItem,
            requestId,
            completion
        );
    }

    private void MarkDodgeVsAttackApproachFinished(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem) ||
            !context.DodgeVsAttackAnchoredApproachActive)
        {
            return;
        }

        context.DodgeVsAttackApproachFinished = true;
        TryCompleteDodgeVsAttackAnchoredApproach(
            context,
            executionItem,
            requestId,
            completion
        );
    }

    private void TryCompleteDodgeVsAttackAnchoredApproach(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (!context.DodgeVsAttackCameraEntryFinished ||
            !context.DodgeVsAttackApproachFinished ||
            !IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem) ||
            !context.DodgeVsAttackAnchoredApproachActive)
        {
            return;
        }

        ResolveBattleCameraDirector()?
            .FinishAnchoredTwoUnitApproachTracking();
        ClearDodgeVsAttackApproachState(context);
        LogApproachCompleted(requestId);
        MarkActionBeginPresentationFinished(
            context,
            requestId,
            completion
        );
    }

    private static void ClearDodgeVsAttackApproachState(
        ActionPresentationContext context
    )
    {
        if (context == null)
        {
            return;
        }

        context.DodgeVsAttackAnchoredApproachActive = false;
        context.DodgeVsAttackEngagementBegun = false;
        context.DodgeVsAttackCameraEntryFinished = false;
        context.DodgeVsAttackApproachFinished = false;
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

        MarkRollResultPresentationFinished(
            context,
            requestId,
            completion
        );
    }

    private void HandleImpact(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        BattlePresentationRoute route
    )
    {
        ActionPresentationContext context = EnsureContext(request);
        RefreshRequestState(context, request);
        context.Route = route;

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

        if (route == null)
        {
            CompleteRequest(request, completion);
            return;
        }

        if (route.HandlerKind ==
                BattlePresentationHandlerKind.UnilateralAttack &&
            route.AttackDelivery ==
                BattlePresentationAttackDeliveryKind.LongRangeShoot)
        {
            if (!TryPrepareUnilateralLongRangeContext(context) ||
                !object.ReferenceEquals(
                    context.CurrentAttacker,
                    context.LongRangeShooter
                ) ||
                !object.ReferenceEquals(
                    context.CurrentTarget,
                    context.LongRangeMeleeActor
                ) ||
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

        if (route.UsesLongRangeGrammar)
        {
            HandleLongRangeImpact(request, completion, context);
            return;
        }

        if (ShouldPlayDodgeFailedImpact(route, request, context))
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

        if (ShouldPlayDefenseGuardImpact(route, request, context))
        {
            if (!TryStartDefenseGuardImpact(request, completion, context))
            {
                CompleteRequest(request, completion);
            }
            return;
        }

        if (!ShouldPlayDefaultAttackImpact(route, request, context))
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

        if (IsLongRangeResponseWinnerCard(
                context.LongRangeMeleeSide.cardState
            ))
        {
            if (!TryStartLongRangeResponseImpact(
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

    private bool TryStartLongRangeResponseImpact(
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

        long requestId = request.RequestId;
        BattleExecutionItem executionItem = request.ExecutionItem;
        float directionSign = GetAttackDirectionSign(
            context.LongRangeShooterHandle,
            context.LongRangeMeleeHandle
        );
        activePresentationRequestId = requestId;
        context.LongRangeResponseImpactStarted = true;
        context.LongRangeResponseImpactRequestId = requestId;

        bool isGuardResponse = context.LongRangeMeleeSide.cardState.cardData
            .cardType == CardType.Defense;
        BattleAttackVsGuardPresentationProfile perfectGuardFxProfile =
            attackVsGuardPresentationPlayer != null
                ? attackVsGuardPresentationPlayer.PresentationProfile
                : null;
        bool started = isGuardResponse
            ? longRangeShootVsAttackPresentationPlayer.TryPlayGuardWinner(
                context.LongRangeMeleePresentation,
                directionSign,
                perfectGuardFxProfile,
                () => CompleteLongRangeResponseImpact(
                    context,
                    executionItem,
                    requestId,
                    completion
                )
            )
            : longRangeShootVsAttackPresentationPlayer.TryPlayDodgeWinner(
                context.LongRangeMeleePresentation,
                directionSign,
                () => CompleteLongRangeResponseImpact(
                    context,
                    executionItem,
                    requestId,
                    completion
                )
            );
        if (!started)
        {
            context.LongRangeResponseImpactStarted = false;
            context.LongRangeResponseImpactRequestId = 0L;
            if (activePresentationRequestId == requestId)
            {
                activePresentationRequestId = 0L;
            }
            return false;
        }

        // 响应胜利复用既有结果镜头；镜头失败不阻塞角色反馈与正式结算。
        BattleCameraDirector director = ResolveBattleCameraDirector();
        if (!isGuardResponse &&
            context.CameraCinematicOwned && director != null)
        {
            director.TryPlayDodgeCameraSway(
                directionSign,
                context.LongRangeMeleePresentation.DodgeMotionDuration
            );
        }
        return true;
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
        bool playShotVisual = context.Route != null &&
            context.Route.HandlerKind ==
                BattlePresentationHandlerKind.UnilateralAttack;
        BattleSpecialLongRangeDuelPresentationProfile specialHitProfile =
            ResolveSpecialLongRangeDuelHitProfile(context);
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
            playShotVisual,
            specialHitProfile,
            () => HandleLongRangeShotTrueVisualImpact(
                context,
                executionItem,
                directionSign,
                specialHitProfile
            ),
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

    private void HandleLongRangeShotTrueVisualImpact(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        float directionSign,
        BattleSpecialLongRangeDuelPresentationProfile specialHitProfile
    )
    {
        if (!IsOwnedLongRangeShotImpact(context, executionItem) ||
            !context.CameraCinematicOwned ||
            !context.LongRangeShotAvailable)
        {
            return;
        }

        BattleCameraDirector director = ResolveBattleCameraDirector();
        BattleHitPresentationProfile normalHitProfile =
            attackVsAttackPresentationPlayer != null
                ? attackVsAttackPresentationPlayer.NormalHitProfile
                : null;
        float activeHitDuration = attackVsAttackPresentationPlayer != null
            ? attackVsAttackPresentationPlayer.NormalHitActiveDuration
            : 0f;
        if (specialHitProfile != null && normalHitProfile != null)
        {
            float expectedTargetTravelDistance =
                normalHitProfile.ImpactBurstDistance +
                specialHitProfile.SpecialFollowKnockbackDistance;
            director?.TryPlayNormalHitImpact(
                context.LongRangeMeleeHandle.WorldRoot.transform,
                directionSign,
                activeHitDuration,
                expectedTargetTravelDistance,
                specialHitProfile.SpecialCameraHorizontalDistance
            );
            TryPlaySharedAttackImpactShake();
            return;
        }

        director?.TryPlayNormalHitImpact(
            context.LongRangeMeleeHandle.WorldRoot.transform,
            directionSign,
            activeHitDuration
        );
        TryPlaySharedAttackImpactShake();
    }

    private BattleSpecialLongRangeDuelPresentationProfile
        ResolveSpecialLongRangeDuelHitProfile(
            ActionPresentationContext context
        )
    {
        BattleCardState shooterCard = context != null &&
            context.LongRangeShooterSide != null
                ? context.LongRangeShooterSide.cardState
                : null;
        return shooterCard != null &&
            shooterCard.IsSpecialLongRangeDuelPresentation() &&
            specialLongRangeDuelPresentationProfile != null &&
            specialLongRangeDuelPresentationProfile
                .EnableSpecialShotHitTuning
                ? specialLongRangeDuelPresentationProfile
                : null;
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
        BattleCameraDirector director = ResolveBattleCameraDirector();
        bool cameraReady = false;
        bool cameraSucceeded = false;
        bool cameraStarted = TryStartOneSidedAttackCameraGrammar(
            director,
            context.LongRangeMeleeHandle,
            context.LongRangeShooterHandle,
            context.ClashEngagement.FinalGap,
            null,
            success =>
            {
                cameraSucceeded = success;
                cameraReady = true;
            }
        );
        if (cameraStarted)
        {
            // LongRange Focus直接交给One-Sided tracking，不释放控制权。
            context.CameraCinematicOwned = true;
        }

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

        while (cameraStarted && !cameraReady &&
            IsCurrentPresentationRequest(requestId) &&
            object.ReferenceEquals(activeContext, context) &&
            object.ReferenceEquals(context.ExecutionItem, executionItem))
        {
            yield return null;
        }

        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem))
        {
            yield break;
        }

        if (cameraStarted)
        {
            if (cameraSucceeded)
            {
                director?.FinishAnchoredTwoUnitApproachTracking();
            }
            else
            {
                director?.CancelAnchoredTwoUnitApproach(false);
            }
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
            () => HandleDefaultAttackTrueVisualImpact(
                context,
                executionItem,
                context.LongRangeMeleeSide != null
                    ? context.LongRangeMeleeSide.cardState
                    : null,
                directionSign
            ),
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
        BattlePresentationCompletion completion,
        BattlePresentationRoute route
    )
    {
        ActionPresentationContext context = EnsureContext(request);
        RefreshRequestState(context, request);
        context.Route = route;
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
            CapturePreviousActionParticipants(context);
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

    private void HandleExecutionComplete(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion
    )
    {
        ClearPreviousActionParticipantTracking();
        BattleActionRollPanelHost.HideImmediate();
        ReleasePendingBattleActionCamera();
        CompleteRequest(request, completion);
    }

    private void HandleDodgeActionComplete(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        if (context.DodgeTailFinished)
        {
            CompleteDodgeActionWithCameraTail(request, completion, context);
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
            CapturePreviousActionParticipants(context);
            ReleaseCameraForContext(context);
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
        CapturePreviousActionParticipants(context);
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
        CompleteDodgeActionWithCameraTail(request, completion, context);
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

    private void CompleteDodgeActionWithCameraTail(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        ResolveBattleCameraDirector()?.FinishDodgeCameraSway();

        if (request != null &&
            request.ContinueBattleActionCameraToNextItem &&
            context != null && context.CameraCinematicOwned)
        {
            CapturePreviousActionParticipants(context);
            ClearDodgeVsAttackApproachState(context);
            context.CameraCinematicOwned = false;
            battleActionCameraCarryPending = true;
            CompleteRequest(request, completion);
            activeContext = null;
            return;
        }

        CompleteActionWithCameraTail(request, completion, context);
    }

    private bool ShouldPlayDefenseGuardImpact(
        BattlePresentationRoute route,
        BattlePresentationRequest request,
        ActionPresentationContext context
    )
    {
        if (route == null ||
            route.HandlerKind !=
                BattlePresentationHandlerKind.AttackVsDefense ||
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

        BattlePresentationAttackDeliveryKind attackDelivery =
            ResolveAttackDelivery(request.Impact.sourceCardState);
        bool useMeleeGuardCamera = request.Impact.sourceCardState != null &&
            request.Impact.sourceCardState.IsMeleeAttack();
        bool started = attackVsGuardPresentationPlayer.TryPlayGuardImpact(
            context.DefenseAttackerPresentation,
            context.DefenseDefenderPresentation,
            directionSign,
            context.GuardPresentationResult,
            attackDelivery,
            () => HandleDefenseGuardTrueVisualContact(
                context,
                executionItem,
                requestId,
                completion,
                useMeleeGuardCamera
            ),
            () => MarkDefenseGuardTailFinished(context, executionItem),
            context.DefenseAttackerHandle?.WorldRoot != null
                ? context.DefenseAttackerHandle.WorldRoot.transform : null,
            context.DefenseDefenderHandle?.WorldRoot != null
                ? context.DefenseDefenderHandle.WorldRoot.transform : null,
            () => HandleDefenseGuardReactionStarted(
                context, executionItem, directionSign, attackDelivery
            )
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
            BattleCameraDirector director = ResolveBattleCameraDirector();
            director?.FinishAnchoredTwoUnitApproachTracking();
        }

        CompleteDefenseGuardImpact(
            context,
            executionItem,
            requestId,
            completion
        );
    }

    private void HandleDefenseGuardReactionStarted(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        float directionSign,
        BattlePresentationAttackDeliveryKind attackDelivery
    )
    {
        // Reaction belongs to the Guard tail; the Impact request is already complete.
        if (!IsOwnedGuardPresentationContext(context, executionItem) ||
            !context.GuardImpactReached || context.GuardTailFinished ||
            !context.CameraCinematicOwned ||
            context.DefenseAttackerHandle?.WorldRoot == null ||
            context.DefenseDefenderHandle?.WorldRoot == null ||
            attackVsGuardPresentationPlayer == null ||
            !attackVsGuardPresentationPlayer.IsRunning)
        {
            return;
        }

        bool fullBlock = context.GuardPresentationResult == BattleGuardPresentationResult.FullBlock;
        Transform target = fullBlock
            ? context.DefenseAttackerHandle.WorldRoot.transform
            : context.DefenseDefenderHandle.WorldRoot.transform;
        float? followRatioOverride = context.Route != null &&
            context.Route.HandlerKind == BattlePresentationHandlerKind.AttackVsDefense &&
            attackDelivery == BattlePresentationAttackDeliveryKind.LongRangeShoot &&
            context.GuardPresentationResult == BattleGuardPresentationResult.ReducedDamage
                ? (float?)attackVsGuardPresentationPlayer.LongRangeReducedCameraFollowRatio
                : null;
        ResolveBattleCameraDirector()?.TryPlayNormalHitImpact(
            target,
            fullBlock ? -directionSign : directionSign,
            attackVsGuardPresentationPlayer.MeleeGuardReactionActiveDuration,
            followRatioOverride
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
        BattlePresentationRoute route,
        BattlePresentationRequest request,
        ActionPresentationContext context
    )
    {
        if (route == null ||
            route.HandlerKind != BattlePresentationHandlerKind.AttackVsDodge ||
            route.ResultKind != BattlePresentationResultKind.DodgeFailed ||
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
                () =>
                {
                    if (IsOwnedDodgePresentationContext(context, executionItem) &&
                        context.DodgeImpactStarted &&
                        !context.DodgeImpactFinished &&
                        context.DodgeImpactRequestId == requestId &&
                        IsCurrentPresentationRequest(requestId) &&
                        context.CameraCinematicOwned)
                    {
                        BattleCameraDirector director =
                            ResolveBattleCameraDirector();
                        Transform hitTargetWorldRoot =
                            context.DodgeDefenderHandle != null &&
                            context.DodgeDefenderHandle.WorldRoot != null
                                ? context.DodgeDefenderHandle.WorldRoot.transform
                                : null;
                        float activeHitDuration =
                            attackVsDodgePresentationPlayer != null
                                ? attackVsDodgePresentationPlayer.NormalHitActiveDuration
                                : 0f;
                        director?.TryPlayNormalHitImpact(
                            hitTargetWorldRoot,
                            directionSign,
                            activeHitDuration
                        );
                        TryPlaySharedAttackImpactShake();
                    }
                },
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

    private void TryStartPendingDodgeRollResult(
        ActionPresentationContext context,
        long requestId
    )
    {
        if (context == null ||
            !CanStartDodgeRollResultPresentation(
                context.DodgeRollPresentationPending,
                context.RollPanelExitFinished,
                activePresentationRequestId,
                requestId
            ) ||
            !IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context))
        {
            return;
        }

        BattlePresentationCompletion completion =
            context.RollResultCompletion;
        if (completion == null || context.Route == null)
        {
            return;
        }

        context.DodgeRollPresentationPending = false;
        if (!TryStartDodgeRollResult(
                requestId,
                completion,
                context,
                context.Route.ResultKind
            ))
        {
            MarkRollResultPresentationFinished(
                context,
                requestId,
                completion
            );
        }
    }

    private bool TryStartDodgeRollResult(
        long requestId,
        BattlePresentationCompletion completion,
        ActionPresentationContext context,
        BattlePresentationResultKind resultKind
    )
    {
        if (!TryResolveDodgePresentationActors(context) ||
            attackVsDodgePresentationPlayer == null ||
            !attackVsDodgePresentationPlayer.isActiveAndEnabled)
        {
            return false;
        }

        context.DodgePresentationResult = resultKind ==
                BattlePresentationResultKind.DodgeSuccess
            ? BattleDodgePresentationResult.DodgeSuccess
            : BattleDodgePresentationResult.DodgeFailed;

        float directionSign = GetAttackDirectionSign(
            context.DodgeAttackerHandle,
            context.DodgeDefenderHandle
        );
        BattleExecutionItem executionItem = context.ExecutionItem;
        context.DodgeRollStarted = true;
        context.DodgeRollResultReady = false;
        context.DodgeTailFinished = false;
        context.DodgeImpactStarted = false;
        context.DodgeImpactFinished = false;
        context.DodgeRollRequestId = requestId;
        activePresentationRequestId = requestId;

        BattleCardState attackCardState = context.InteractionContext != null &&
            context.InteractionContext.AttackAction != null
                ? context.InteractionContext.AttackAction.cardState
                : GetDodgeAttackCardState(context.ClashSession);
        BattlePresentationAttackDeliveryKind attackDelivery =
            ResolveAttackDelivery(attackCardState);
        bool started = attackVsDodgePresentationPlayer
            .TryPlayDodgeRollResult(
                context.DodgeAttackerPresentation,
                context.DodgeDefenderPresentation,
                directionSign,
                context.DodgePresentationResult,
                attackDelivery,
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
            if (context.DodgePresentationResult ==
                    BattleDodgePresentationResult.DodgeSuccess &&
                attackCardState != null &&
                attackCardState.IsMeleeAttack() &&
                context.CameraCinematicOwned)
            {
                ResolveBattleCameraDirector()?.TryPlayDodgeCameraSway(
                    directionSign,
                    context.DodgeDefenderPresentation.DodgeMotionDuration
                );
            }

            return true;
        }

        context.DodgeRollStarted = false;
        context.DodgeRollRequestId = 0L;
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
        MarkRollResultPresentationFinished(
            context,
            requestId,
            completion
        );
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
            LogDefenseGuardCoordinateDiagnostic(
                "ActionCompleteBeforeCameraRelease",
                context,
                context.ExecutionItem
            );
            CompleteActionWithCameraTail(request, completion, context);
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
        LogDefenseGuardCoordinateDiagnostic(
            "ActionCompleteBeforeCameraRelease",
            context,
            executionItem
        );
        CompleteActionWithCameraTail(request, completion, context);
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

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogDefenseGuardCoordinateDiagnostic(
        string stage,
        ActionPresentationContext context,
        BattleExecutionItem executionItem
    )
    {
        if (!IsOwnedGuardPresentationContext(context, executionItem))
        {
            return;
        }

        ResolveBattleCameraDirector()?.LogBattleActionCoordinateDiagnostic(
            stage,
            context.DefenseAttackerHandle,
            context.DefenseDefenderHandle
        );
    }

    private bool ShouldPlayDefaultAttackImpact(
        BattlePresentationRoute route,
        BattlePresentationRequest request,
        ActionPresentationContext context
    )
    {
        bool supportedRoute = route != null &&
            route.UsesNearRangeAttackGrammar &&
            (route.HandlerKind ==
                    BattlePresentationHandlerKind.AttackVsAttack ||
                route.HandlerKind ==
                    BattlePresentationHandlerKind.UnilateralAttack);

        // 中立的近距AttackVsAttack与Unilateral共用同一攻击表现入口。
        return supportedRoute &&
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
                    () => HandleDefaultAttackTrueVisualImpact(
                        context,
                        executionItem,
                        request.Impact.sourceCardState,
                        directionSign
                    ),
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
            !IsNearRangeAttack(sourceCardState))
        {
            return;
        }

        BattleCameraDirector director = ResolveBattleCameraDirector();
        bool useNormalHitCamera = context.Route != null &&
            (context.Route.HandlerKind ==
                    BattlePresentationHandlerKind.AttackVsAttack ||
                context.Route.HandlerKind ==
                    BattlePresentationHandlerKind.UnilateralAttack) &&
            sourceCardState.IsMeleeAttack();
        if (!useNormalHitCamera)
        {
            director?.TryPlayGenericHitImpact(directionSign);
            TryPlaySharedAttackImpactShake();
            return;
        }

        Transform hitTargetWorldRoot = context.CurrentTargetHandle != null &&
            context.CurrentTargetHandle.WorldRoot != null
                ? context.CurrentTargetHandle.WorldRoot.transform
                : null;
        director?.TryPlayNormalHitImpact(
            hitTargetWorldRoot,
            directionSign,
            attackVsAttackPresentationPlayer != null
                ? attackVsAttackPresentationPlayer.NormalHitActiveDuration
                : 0f
        );

        TryPlaySharedAttackImpactShake();
    }

    private void TryPlaySharedAttackImpactShake()
    {
        BattleCameraDirector director = ResolveBattleCameraDirector();
        BattleHitPresentationProfile profile =
            attackVsAttackPresentationPlayer != null
                ? attackVsAttackPresentationPlayer.NormalHitProfile
                : null;

        director?.TryPlayImpactShake(profile);
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
            bool playerUnavailable = attackVsAttackPresentationPlayer == null ||
                (!attackVsAttackPresentationPlayer.IsRunning &&
                    !attackVsAttackPresentationPlayer.IsFinished);
            if (playerUnavailable)
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
        CompleteActionWithCameraTail(request, completion, context);
    }

    private void CompleteActionWithCameraTail(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        ActionPresentationContext context
    )
    {
        CapturePreviousActionParticipants(context);
        ReleaseCameraForContext(context);
        CompleteRequest(request, completion);
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

        BattleHitPresentationProfile sharedNormalHitProfile =
            attackVsAttackPresentationPlayer != null
                ? attackVsAttackPresentationPlayer.NormalHitProfile
                : null;
        longRangeShootVsAttackPresentationPlayer.ConfigureNormalHitProfile(
            sharedNormalHitProfile
        );
        if (sharedNormalHitProfile == null)
        {
            Debug.LogError(
                "BattleSceneExecutionPresenter无法为LongRange Player注入" +
                "共享NormalHitProfile。",
                this
            );
        }
    }

    private void ResolveSpecialLongRangeDuelPresentationPlayer()
    {
        if (specialLongRangeDuelPresentationPlayer == null)
        {
            specialLongRangeDuelPresentationPlayer = GetComponent<
                BattleSpecialLongRangeDuelPresentationPlayer>();
        }

        if (specialLongRangeDuelPresentationPlayer == null)
        {
            specialLongRangeDuelPresentationPlayer = gameObject.AddComponent<
                BattleSpecialLongRangeDuelPresentationPlayer>();
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
        // Presenter顶层路由只消费冻结的中立Interaction Contract。
        context.InteractionContext = request.InteractionContext;
        context.ClashSession = request.ClashSession;
        context.ResolutionPlan = request.ResolutionPlan;
        context.Outcome = request.Outcome;
        context.LastRequestId = request.RequestId;
        context.Cancelled = false;
    }

    private void RefreshClashActors(ActionPresentationContext context)
    {
        BattlePresentationInteractionContext interaction =
            context.InteractionContext;
        BattleClashSession session = context.ClashSession;
        context.SideAActor = interaction != null && interaction.SideA != null
            ? interaction.SideA.actor
            : session != null && session.SideA != null
                ? session.SideA.actor
                : null;
        context.SideBActor = interaction != null && interaction.SideB != null
            ? interaction.SideB.actor
            : session != null && session.SideB != null
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
        if (context == null)
        {
            return;
        }

        ClearDefenseVsAttackApproachState(context);
        ClearDodgeVsAttackApproachState(context);
        context.LongRangeCameraFocusActive = false;
        if (!context.CameraCinematicOwned)
        {
            return;
        }

        context.CameraCinematicOwned = false;
        ResolveBattleCameraDirector()?
            .ReleaseBattleActionCinematicControl();
    }

    private void CancelSpecialLongRangeDuelForContext(
        ActionPresentationContext context
    )
    {
        if (context == null ||
            !context.SpecialLongRangeDuelPreRollActive)
        {
            return;
        }

        context.SpecialLongRangeDuelPreRollActive = false;
        specialLongRangeDuelPresentationPlayer?.CancelAndReset();
        ResolveBattleCameraDirector()?
            .CancelExternallyDrivenSingleActorApproach(true);
        context.CameraCinematicOwned = false;
    }

    private void CancelOrReleaseCameraForContext(
        ActionPresentationContext context
    )
    {
        if (context == null)
        {
            return;
        }

        ClearDefenseVsAttackApproachState(context);
        ClearDodgeVsAttackApproachState(context);
        context.LongRangeCameraFocusActive = false;
        if (!context.CameraCinematicOwned)
        {
            return;
        }

        context.CameraCinematicOwned = false;
        BattleCameraDirector director = ResolveBattleCameraDirector();
        if (director == null)
        {
            return;
        }

        if (!director.CancelExternallyDrivenSingleActorApproach(true) &&
            !director.CancelTwoUnitFocus(true) &&
            !director.CancelAnchoredTwoUnitApproach(true))
        {
            director.ReleaseBattleActionCinematicControl();
        }
    }

    private void ReleasePendingBattleActionCamera()
    {
        if (!battleActionCameraCarryPending)
        {
            return;
        }

        battleActionCameraCarryPending = false;
        ResolveBattleCameraDirector()?
            .ReleaseBattleActionCinematicControl();
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

    private void BeginRollPanelEntrance(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId
    )
    {
        if (context == null || context.RollPanelEntranceAttempted ||
            !IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem))
        {
            return;
        }

        context.RollPanelEntranceAttempted = true;
        BattlePresentationRequest request = context.ActionBeginRequest;
        if (request == null || context.Route == null ||
            BattlePresentationRouter.IsActionUnavailable(request))
        {
            context.RollPanelEntranceFinished = true;
            TryCompleteActionBegin(context, requestId);
            return;
        }

        context.RollPanelEntranceRequired = true;
        context.RollPanelEntranceFinished = false;
        bool started = BattleActionRollPanelHost.ShowForActionBegin(
            request,
            () => MarkRollPanelEntranceFinished(
                context,
                executionItem,
                requestId
            )
        );
        if (started)
        {
            return;
        }

        context.RollPanelEntranceRequired = false;
        context.RollPanelEntranceFinished = true;
        TryCompleteActionBegin(context, requestId);
    }

    private void MarkRollPanelEntranceFinished(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId
    )
    {
        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem))
        {
            return;
        }

        context.RollPanelEntranceFinished = true;
        TryCompleteActionBegin(context, requestId);
    }

    private void MarkActionBeginPresentationFinished(
        ActionPresentationContext context,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (context == null || !IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context))
        {
            return;
        }

        context.ActionBeginCompletion = completion;
        context.ActionBeginPresentationFinished = true;
        BeginRollPanelEntrance(context, context.ExecutionItem, requestId);
        TryCompleteActionBegin(context, requestId);
    }

    private void TryCompleteActionBegin(
        ActionPresentationContext context,
        long requestId
    )
    {
        if (context == null || context.ActionBeginCompletion == null ||
            !BattleActionRollPanelHost.CanCompleteActionBegin(
                context.ActionBeginPresentationFinished,
                context.RollPanelEntranceRequired,
                context.RollPanelEntranceFinished
            ) || !IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context))
        {
            return;
        }

        BattlePresentationCompletion completion =
            context.ActionBeginCompletion;
        context.ActionBeginCompletion = null;
        if (!TryReleasePresentationRequestOwnership(
                ref activePresentationRequestId,
                requestId
            ))
        {
            context.ActionBeginCompletion = completion;
            return;
        }
        completion.TryComplete(requestId);
    }

    internal static bool TryReleasePresentationRequestOwnership(
        ref long activeRequestId,
        long requestId
    )
    {
        if (requestId == 0L || activeRequestId != requestId)
        {
            return false;
        }

        activeRequestId = 0L;
        return true;
    }

    internal static bool CanCompleteRollResult(
        bool presentationFinished,
        bool panelExitRequired,
        bool panelExitFinished
    )
    {
        return presentationFinished &&
            (!panelExitRequired || panelExitFinished);
    }

    internal static bool CanStartDodgeRollResultPresentation(
        bool presentationPending,
        bool panelExitFinished,
        long activeRequestId,
        long requestId
    )
    {
        return presentationPending && panelExitFinished &&
            requestId != 0L && activeRequestId == requestId;
    }

    private void PrepareRollPanelResultLifecycle(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion,
        BattlePresentationRoute route,
        ActionPresentationContext context
    )
    {
        context.RollResultCompletion = completion;
        context.RollResultPresentationFinished = false;
        context.RollPanelExitRequired = false;
        context.RollPanelExitFinished = true;
        if (route == null)
        {
            return;
        }

        if (!BattleActionRollPanelHost.ShouldUseTerminalExit(
                route.ResultKind
            ))
        {
            BattleActionRollPanelHost.ShowForRoll(request);
            return;
        }

        context.RollPanelExitRequired = true;
        context.RollPanelExitFinished = false;
        bool started = BattleActionRollPanelHost.ShowTerminalRollResult(
            request,
            () => MarkRollPanelExitFinished(
                context,
                request.ExecutionItem,
                request.RequestId
            )
        );
        if (started)
        {
            return;
        }

        context.RollPanelExitRequired = false;
        context.RollPanelExitFinished = true;
    }

    private void MarkRollPanelExitFinished(
        ActionPresentationContext context,
        BattleExecutionItem executionItem,
        long requestId
    )
    {
        if (!IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context) ||
            !object.ReferenceEquals(context.ExecutionItem, executionItem))
        {
            return;
        }

        context.RollPanelExitFinished = true;
        TryStartPendingDodgeRollResult(context, requestId);
        TryCompleteRollResult(context, requestId);
    }

    private void MarkRollResultPresentationFinished(
        ActionPresentationContext context,
        long requestId,
        BattlePresentationCompletion completion
    )
    {
        if (context == null || !IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context))
        {
            return;
        }

        context.RollResultCompletion = completion;
        context.RollResultPresentationFinished = true;
        TryCompleteRollResult(context, requestId);
    }

    private void TryCompleteRollResult(
        ActionPresentationContext context,
        long requestId
    )
    {
        if (context == null || context.RollResultCompletion == null ||
            !CanCompleteRollResult(
                context.RollResultPresentationFinished,
                context.RollPanelExitRequired,
                context.RollPanelExitFinished
            ) ||
            !IsCurrentPresentationRequest(requestId) ||
            !object.ReferenceEquals(activeContext, context))
        {
            return;
        }

        BattlePresentationCompletion completion =
            context.RollResultCompletion;
        context.RollResultCompletion = null;
        if (!TryReleasePresentationRequestOwnership(
                ref activePresentationRequestId,
                requestId
            ))
        {
            context.RollResultCompletion = completion;
            return;
        }
        completion.TryComplete(requestId);
    }

    private void CompleteRequest(
        BattlePresentationRequest request,
        BattlePresentationCompletion completion
    )
    {
        if (request.Cue == BattlePresentationCue.ActionBegin &&
            activeContext != null &&
            object.ReferenceEquals(
                activeContext.ExecutionItem,
                request.ExecutionItem
            ))
        {
            MarkActionBeginPresentationFinished(
                activeContext,
                request.RequestId,
                completion
            );
            return;
        }

        if (request.Cue == BattlePresentationCue.RollResult &&
            activeContext != null &&
            object.ReferenceEquals(
                activeContext.ExecutionItem,
                request.ExecutionItem
            ))
        {
            MarkRollResultPresentationFinished(
                activeContext,
                request.RequestId,
                completion
            );
            return;
        }

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
