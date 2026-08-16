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
    [SerializeField] private BattleSelfActionDropZone selfActionDropZone;
    [SerializeField] private bool isEnemy;

    private CharacterData boundCharacter;
    private Action<BattleActionSlotUIView> slotLeftClickHandler;
    private Action<BattleActionSlotUIView> slotRightClickHandler;
    private Action<BattleSelfActionDropZone> selfTargetClickHandler;
    private GameObject headStatusGroup;
    private bool warnedMissingHeadStatusGroup;

    public void SetHeadStatusVisible(bool visible)
    {
        GameObject resolvedHeadStatusGroup = ResolveHeadStatusGroup();
        if (resolvedHeadStatusGroup != null &&
            resolvedHeadStatusGroup.activeSelf != visible)
        {
            resolvedHeadStatusGroup.SetActive(visible);
        }
    }

    public void SetSlotClickHandler(Action<BattleActionSlotUIView> handler)
    {
        slotLeftClickHandler = handler;
        RefreshSlotInteractionBindings();
    }

    public void SetAllySlotInteractionHandlers(
        Action<BattleActionSlotUIView> onLeftClicked,
        Action<BattleActionSlotUIView> onRightClicked
    )
    {
        slotLeftClickHandler = onLeftClicked;
        slotRightClickHandler = onRightClicked;
        RefreshSlotInteractionBindings();
    }

    public void SetEnemySlotClickHandler(
        Action<BattleActionSlotUIView> onClicked
    )
    {
        slotLeftClickHandler = onClicked;
        RefreshSlotInteractionBindings();
    }

    public void SetSelfTargetClickHandler(
        Action<BattleSelfActionDropZone> onClicked
    )
    {
        selfTargetClickHandler = onClicked;
        RefreshSelfTargetBinding();
    }

    public void ClearBoundEnemyIntents()
    {
        if (slot01View != null)
        {
            slot01View.SetBoundEnemyIntent(null);
        }

        if (slot02View != null)
        {
            slot02View.SetBoundEnemyIntent(null);
        }
    }

    public void SetBoundEnemyIntent(int slotIndex, BattleEnemyIntent enemyIntent)
    {
        BattleActionSlotUIView slotView = GetSlotView(slotIndex);
        if (slotView != null)
        {
            slotView.SetBoundEnemyIntent(enemyIntent);
        }
    }

    public void ClearBoundActionSlots()
    {
        slot01View?.SetBoundActionSlot(null);
        slot02View?.SetBoundActionSlot(null);
    }

    public void SetBoundActionSlot(int slotIndex, BattleActionSlot actionSlot)
    {
        BattleActionSlotUIView slotView = GetSlotView(slotIndex);
        slotView?.SetBoundActionSlot(actionSlot);
    }

    public CharacterData BoundCharacter => boundCharacter;

    public bool IsEnemyView => isEnemy;

    private void RefreshSelfTargetBinding()
    {
        if (selfActionDropZone != null)
        {
            selfActionDropZone.Bind(
                isEnemy ? null : boundCharacter,
                isEnemy ? null : selfTargetClickHandler
            );
        }
    }

    private void RefreshAllInteractionBindings()
    {
        RefreshSlotInteractionBindings();
        RefreshSelfTargetBinding();
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

        RefreshAllInteractionBindings();
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

        ClearBoundEnemyIntents();
        ClearBoundActionSlots();
        RefreshAllInteractionBindings();
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
            slot01View.BindInteraction(
                boundCharacter,
                0,
                isEnemy,
                slotLeftClickHandler,
                isEnemy ? null : slotRightClickHandler
            );
        }

        if (slot02View != null)
        {
            slot02View.BindInteraction(
                boundCharacter,
                1,
                isEnemy,
                slotLeftClickHandler,
                isEnemy ? null : slotRightClickHandler
            );
        }
    }

    private GameObject ResolveHeadStatusGroup()
    {
        if (headStatusGroup != null)
        {
            return headStatusGroup;
        }

        Transform candidate = slot01View != null
            ? slot01View.transform.parent
            : null;
        bool sharesHeadGroup = candidate != null &&
            (slot02View == null || slot02View.transform.parent == candidate) &&
            (speedText == null || speedText.transform.parent == candidate);
        if (sharesHeadGroup)
        {
            // 三个既有引用共同确认HeadSlotGroup，不增加Prefab序列化接线。
            headStatusGroup = candidate.gameObject;
            return headStatusGroup;
        }

        if (!warnedMissingHeadStatusGroup)
        {
            warnedMissingHeadStatusGroup = true;
            Debug.LogWarning(
                "角色状态UI无法解析统一的HeadSlotGroup，已跳过阶段显隐。",
                this
            );
        }
        return null;
    }
}
