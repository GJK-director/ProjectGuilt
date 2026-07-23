using System;
using TMPro;
using UnityEngine;

public class BattleCharacterStatusUIView : MonoBehaviour
{
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private BattleActionSlotUIView slot01View;
    [SerializeField] private BattleActionSlotUIView slot02View;
    [SerializeField] private BattleHpUIView hpView;
    [SerializeField] private BattleBuffGroupUIView buffGroupView;
    [SerializeField] private bool isEnemy;

    private CharacterData boundCharacter;
    private Action<BattleActionSlotUIView> slotClickHandler;

    public void SetSlotClickHandler(Action<BattleActionSlotUIView> handler)
    {
        slotClickHandler = handler;
        RefreshSlotInteractionBindings();
    }

    public void SetCharacter(CharacterData characterData)
    {
        boundCharacter = characterData;

        if (speedText != null)
        {
            speedText.text = characterData != null
                ? characterData.GetCurrentSpeed().ToString()
                : "-";
        }

        RefreshDefaultSlots();

        if (hpView != null)
        {
            hpView.SetCharacter(characterData);
        }

        if (buffGroupView != null)
        {
            buffGroupView.SetCharacter(characterData);
        }

        RefreshSlotInteractionBindings();
    }

    public void Clear()
    {
        boundCharacter = null;

        if (speedText != null)
        {
            speedText.text = "-";
        }

        RefreshDefaultSlots();

        if (hpView != null)
        {
            hpView.Clear();
        }

        if (buffGroupView != null)
        {
            buffGroupView.Clear();
        }

        if (slot01View != null)
        {
            slot01View.SetSelected(false);
        }

        if (slot02View != null)
        {
            slot02View.SetSelected(false);
        }

        RefreshSlotInteractionBindings();
    }

    public void RefreshDefaultSlots()
    {
        BattleActionSlotUIState defaultState = isEnemy
            ? BattleActionSlotUIState.EnemyEmpty
            : BattleActionSlotUIState.AllyEmpty;

        if (slot01View != null)
        {
            slot01View.SetState(defaultState);
        }

        if (slot02View != null)
        {
            slot02View.SetState(defaultState);
        }
    }

    public BattleActionSlotUIView GetSlotView(int slotIndex)
    {
        if (slotIndex == 0)
        {
            return slot01View;
        }

        if (slotIndex == 1)
        {
            return slot02View;
        }

        Debug.LogWarning("BattleCharacterStatusUIView 槽位索引超出范围：" + slotIndex);
        return null;
    }

    public void SetSlotState(int slotIndex, BattleActionSlotUIState state)
    {
        BattleActionSlotUIView slotView = GetSlotView(slotIndex);

        if (slotView != null)
        {
            slotView.SetState(state);
        }
    }

    private void RefreshSlotInteractionBindings()
    {
        if (slot01View != null)
        {
            slot01View.BindInteraction(boundCharacter, 0, isEnemy, slotClickHandler);
        }

        if (slot02View != null)
        {
            slot02View.BindInteraction(boundCharacter, 1, isEnemy, slotClickHandler);
        }
    }
}
