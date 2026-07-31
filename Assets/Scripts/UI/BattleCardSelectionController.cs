using System;

public sealed class BattleCardSelectionController
{
    private BattleCardUIView selectedCardView;

    public BattleCardUIView SelectedCardView => selectedCardView;
    public bool HasSelection => selectedCardView != null;
    public event Action<BattleCardUIView> SelectionChanged;

    public bool ToggleCardSelection(BattleCardUIView cardView)
    {
        if (cardView == null || !cardView.CanSelect)
        {
            return false;
        }

        if (object.ReferenceEquals(selectedCardView, cardView))
        {
            ClearSelection();
            return true;
        }

        return SelectCard(cardView);
    }

    public bool SelectCard(BattleCardUIView cardView)
    {
        if (cardView == null || !cardView.CanSelect)
        {
            return false;
        }

        if (selectedCardView != null)
        {
            selectedCardView.SetSelected(false);
        }

        selectedCardView = cardView;
        selectedCardView.SetSelected(true);
        SelectionChanged?.Invoke(selectedCardView);
        return true;
    }

    public void ClearSelection()
    {
        BattleCardUIView previousSelection = selectedCardView;
        selectedCardView = null;

        if (previousSelection != null)
        {
            previousSelection.SetSelected(false);
            SelectionChanged?.Invoke(null);
        }
    }

    public void ClearSelectionIfSelected(BattleCardUIView cardView)
    {
        if (object.ReferenceEquals(selectedCardView, cardView))
        {
            ClearSelection();
        }
    }

    public bool IsSelected(BattleCardUIView cardView)
    {
        return cardView != null &&
            object.ReferenceEquals(selectedCardView, cardView);
    }

}
