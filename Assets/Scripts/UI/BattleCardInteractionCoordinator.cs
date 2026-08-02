using System;

public sealed class BattleCardInteractionOutcome
{
    public bool hadSelectedCard;
    public bool isSuccess;
    public BattleActionAssignmentResult assignmentResult;
}

// 点击式卡牌交互的唯一协调入口，不复制任何资格、速度或槽位规则。
public sealed class BattleCardInteractionCoordinator
{
    private readonly BattleCardSelectionController cardSelectionController;
    private CharacterData selectedCharacter;
    private BattleActionSlotUIView selectedActionSlotView;

    public event Action<BattleActionSlotUIView> SourceSlotSelectionChanged;

    public BattleCardInteractionCoordinator(
        BattleCardSelectionController selectionController
    )
    {
        cardSelectionController = selectionController;
    }

    public CharacterData SelectedCharacter => selectedCharacter;
    public BattleActionSlotUIView SelectedActionSlotView =>
        selectedActionSlotView;
    public int SelectedFormalSlotIndex =>
        selectedActionSlotView != null &&
        !selectedActionSlotView.IsEnemySlot &&
        object.ReferenceEquals(
            selectedActionSlotView.BoundCharacter,
            selectedCharacter
        )
            ? selectedActionSlotView.FormalSlotIndex
            : -1;

    public bool SelectSourceSlot(BattleActionSlotUIView slotView)
    {
        if (slotView == null ||
            slotView.IsEnemySlot ||
            slotView.BoundCharacter == null)
        {
            return false;
        }

        if (selectedActionSlotView != null &&
            !object.ReferenceEquals(selectedActionSlotView, slotView))
        {
            selectedActionSlotView.SetSelected(false);
        }

        selectedCharacter = slotView.BoundCharacter;
        selectedActionSlotView = slotView;
        selectedActionSlotView.SetSelected(true);
        SourceSlotSelectionChanged?.Invoke(selectedActionSlotView);
        return true;
    }

    public BattleCardInteractionOutcome ClickSelectedSourceSlotAsSelf(
        BattleRuntimeState runtimeState,
        BattleActionSlotUIView clickedSlotView
    )
    {
        BattleCardUIView selectedCardView =
            cardSelectionController != null
                ? cardSelectionController.SelectedCardView
                : null;
        BattleCardInteractionOutcome outcome =
            CreateOutcome(selectedCardView);

        if (selectedCardView == null ||
            clickedSlotView == null ||
            !object.ReferenceEquals(clickedSlotView, selectedActionSlotView) ||
            clickedSlotView.IsEnemySlot ||
            !IsDefenseOrDodge(selectedCardView.BoundCardState))
        {
            return outcome;
        }

        outcome.isSuccess = BattleCardAssignmentRouter.TryAssignToSelf(
            runtimeState,
            selectedCharacter,
            SelectedFormalSlotIndex,
            selectedCardView.BoundOwner,
            selectedCardView.BoundCardState,
            selectedCharacter,
            out outcome.assignmentResult
        );

        CompleteSuccessfulAssignment(outcome);
        return outcome;
    }

    public BattleCardInteractionOutcome ClickEnemySlot(
        BattleRuntimeState runtimeState,
        BattleActionSlotUIView targetSlotView
    )
    {
        BattleCardUIView selectedCardView =
            cardSelectionController != null
                ? cardSelectionController.SelectedCardView
                : null;
        BattleCardInteractionOutcome outcome =
            CreateOutcome(selectedCardView);

        if (selectedCardView == null ||
            targetSlotView == null ||
            !targetSlotView.IsEnemySlot)
        {
            return outcome;
        }

        outcome.isSuccess = BattleCardAssignmentRouter.TryAssignToEnemySlot(
            runtimeState,
            selectedCharacter,
            SelectedFormalSlotIndex,
            selectedCardView.BoundOwner,
            selectedCardView.BoundCardState,
            targetSlotView.BoundCharacter,
            targetSlotView.BoundEnemyIntent,
            targetSlotView.FormalSlotIndex,
            out outcome.assignmentResult
        );

        CompleteSuccessfulAssignment(outcome);
        return outcome;
    }

    public BattleCardInteractionOutcome ClickSelfTarget(
        BattleRuntimeState runtimeState,
        BattleSelfActionDropZone targetView
    )
    {
        BattleCardUIView selectedCardView =
            cardSelectionController != null
                ? cardSelectionController.SelectedCardView
                : null;
        BattleCardInteractionOutcome outcome =
            CreateOutcome(selectedCardView);

        if (selectedCardView == null || targetView == null)
        {
            return outcome;
        }

        outcome.isSuccess = BattleCardAssignmentRouter.TryAssignToSelf(
            runtimeState,
            selectedCharacter,
            SelectedFormalSlotIndex,
            selectedCardView.BoundOwner,
            selectedCardView.BoundCardState,
            targetView.BoundCharacter,
            out outcome.assignmentResult
        );

        CompleteSuccessfulAssignment(outcome);
        return outcome;
    }

    public bool ToggleCardMode(bool showingSinCards)
    {
        cardSelectionController?.ClearSelection();
        return !showingSinCards;
    }

    public void PrepareForBattleStart()
    {
        ClearAllSelections();
    }

    public void ClearAllSelections()
    {
        cardSelectionController?.ClearSelection();
        ClearSourceSlot();
    }

    public void ClearSourceSlot()
    {
        if (selectedActionSlotView != null)
        {
            selectedActionSlotView.SetSelected(false);
        }

        selectedCharacter = null;
        selectedActionSlotView = null;
        SourceSlotSelectionChanged?.Invoke(null);
    }

    private static BattleCardInteractionOutcome CreateOutcome(
        BattleCardUIView selectedCardView
    )
    {
        return new BattleCardInteractionOutcome
        {
            hadSelectedCard = selectedCardView != null
        };
    }

    private void CompleteSuccessfulAssignment(
        BattleCardInteractionOutcome outcome
    )
    {
        if (!outcome.isSuccess ||
            outcome.assignmentResult == null ||
            !outcome.assignmentResult.isSuccess)
        {
            return;
        }

        cardSelectionController?.ClearSelection();
    }

    private static bool IsDefenseOrDodge(BattleCardState cardState)
    {
        return cardState != null &&
            cardState.cardData != null &&
            (cardState.cardData.cardType == CardType.Defense ||
             cardState.cardData.cardType == CardType.Dodge);
    }
}
