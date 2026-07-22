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

    public void SetCharacter(CharacterData characterData)
    {
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
    }

    public void Clear()
    {
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
}
