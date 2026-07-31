using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum BattleActionSlotUIState
{
    AllyEmpty,
    AllyActionSet,
    AllyTargetedNoAction,
    EnemyEmpty,
    EnemyActionSet
}

public class BattleActionSlotUIView : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Image slotImage;

    [SerializeField] private Sprite slotEmptySprite;
    [SerializeField] private Sprite slotAllyActionSetSprite;
    [SerializeField] private Sprite slotAllyTargetedNoActionSprite;
    [SerializeField] private Sprite slotEnemyEmptySprite;
    [SerializeField] private Sprite slotEnemyActionSetSprite;
    [SerializeField]
    private BattleActionSlotSelectionEffectUIView selectionEffectView;
    [SerializeField] private RectTransform relationLineAnchor;

    [SerializeField] private BattleActionSlotUIState defaultState = BattleActionSlotUIState.AllyEmpty;

    private BattleActionSlotUIState currentBaseState;
    private BattleActionSlotUIState committedState;
    private bool hasCommittedState;
    private bool isSelected;
    private bool isHovered;
    private bool warnedMissingSlotImage;
    private bool warnedMissingSelectionEffect;
    private CharacterData boundCharacter;
    private int slotIndex = -1;
    private bool isEnemySlot;
    private BattleEnemyIntent boundEnemyIntent;
    private Action<BattleActionSlotUIView> leftClickHandler;
    private Action<BattleActionSlotUIView> rightClickHandler;
    private Coroutine stateFeedbackCoroutine;

    public CharacterData BoundCharacter => boundCharacter;
    public int SlotIndex => slotIndex;
    public int UISlotIndex => slotIndex;
    public int FormalSlotIndex => slotIndex >= 0 ? slotIndex + 1 : -1;
    public bool IsEnemySlot => isEnemySlot;
    public bool IsSelected => isSelected;
    public bool IsHovered => isHovered;
    public BattleActionSlotUIState CurrentBaseState => currentBaseState;
    public BattleEnemyIntent BoundEnemyIntent => boundEnemyIntent;
    public RectTransform RelationLineAnchor => relationLineAnchor != null
        ? relationLineAnchor
        : transform as RectTransform;
    public bool HasExplicitRelationLineAnchor => relationLineAnchor != null;

    void Reset()
    {
        TryBindImage();
        TryBindSelectionEffect();
    }

    void Awake()
    {
        TryBindImage();
        TryBindSelectionEffect();
        currentBaseState = defaultState;
        committedState = defaultState;
        hasCommittedState = false;
        isSelected = false;
        isHovered = false;
        RefreshDisplayedSprite();
        selectionEffectView?.StopAndReset();
    }

    public void SetState(BattleActionSlotUIState state)
    {
        currentBaseState = state;
        RefreshDisplayedSprite();
        ScheduleStateFeedbackCommit();
    }

    public void SetDefaultState()
    {
        SetState(defaultState);
    }

    public void SetSelected(bool selected)
    {
        bool wasSelected = isSelected;
        isSelected = selected;
        RefreshDisplayedSprite();

        if (isEnemySlot)
        {
            return;
        }

        RefreshSelectionEffectReference();
        if (selectionEffectView == null)
        {
            return;
        }

        if (selected)
        {
            selectionEffectView.SetPersistentVisible(true);
            if (!wasSelected)
            {
                selectionEffectView.PlayPulse();
            }

            return;
        }

        selectionEffectView.SetPersistentVisible(isHovered);
        if (isHovered)
        {
            selectionEffectView.ShowImmediate();
        }
    }

    public void BindInteraction(
        CharacterData character,
        int index,
        bool enemySlot,
        Action<BattleActionSlotUIView> onClicked
    )
    {
        BindInteraction(character, index, enemySlot, onClicked, null);
    }

    public void BindInteraction(
        CharacterData character,
        int index,
        bool enemySlot,
        Action<BattleActionSlotUIView> onLeftClicked,
        Action<BattleActionSlotUIView> onRightClicked
    )
    {
        bool bindingChanged =
            !object.ReferenceEquals(boundCharacter, character) ||
            slotIndex != index ||
            isEnemySlot != enemySlot;

        if (bindingChanged)
        {
            isHovered = false;
            isSelected = false;
            StopStateFeedbackCommit();
            hasCommittedState = false;
            committedState = currentBaseState;
            RefreshSelectionEffectReference();
            selectionEffectView?.StopAndReset();
        }

        boundCharacter = character;
        slotIndex = index;
        isEnemySlot = enemySlot;
        leftClickHandler = onLeftClicked;
        rightClickHandler = onRightClicked;
        RefreshDisplayedSprite();
    }

    public void SetBoundEnemyIntent(BattleEnemyIntent enemyIntent)
    {
        boundEnemyIntent = enemyIntent;
    }

    internal void ConfigureTestVisuals(Image image, Sprite sprite)
    {
        slotImage = image;
        slotEmptySprite = sprite;
        slotAllyActionSetSprite = sprite;
        slotAllyTargetedNoActionSprite = sprite;
        slotEnemyEmptySprite = sprite;
        slotEnemyActionSetSprite = sprite;
    }

    internal void CommitStateFeedbackForTesting()
    {
        StopStateFeedbackCommit();
        CommitStableStateFeedback();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (boundCharacter == null || eventData == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            bool wasSelected = isSelected;
            leftClickHandler?.Invoke(this);

            if (!isEnemySlot && wasSelected && isSelected)
            {
                RefreshSelectionEffectReference();
                selectionEffectView?.SetPersistentVisible(true);
                selectionEffectView?.PlayPulse();
            }

            return;
        }

        if (!isEnemySlot &&
            eventData.button == PointerEventData.InputButton.Right)
        {
            rightClickHandler?.Invoke(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (boundCharacter == null || isEnemySlot)
        {
            return;
        }

        isHovered = true;
        RefreshDisplayedSprite();
        RefreshSelectionEffectReference();
        selectionEffectView?.SetPersistentVisible(true);
        selectionEffectView?.ShowImmediate();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (boundCharacter == null || isEnemySlot)
        {
            return;
        }

        isHovered = false;
        RefreshDisplayedSprite();
        RefreshSelectionEffectReference();
        selectionEffectView?.SetPersistentVisible(isSelected);
    }

    private void RefreshDisplayedSprite()
    {
        TryBindImage();

        if (slotImage == null)
        {
            if (!warnedMissingSlotImage)
            {
                Debug.LogWarning(
                    "BattleActionSlotUIView 缺少 slotImage，无法刷新行动槽图标。",
                    this
                );
                warnedMissingSlotImage = true;
            }

            return;
        }

        BattleActionSlotUIState displayedState = currentBaseState;
        if (!isEnemySlot &&
            currentBaseState ==
                BattleActionSlotUIState.AllyTargetedNoAction &&
            (isHovered || isSelected))
        {
            displayedState = BattleActionSlotUIState.AllyEmpty;
        }

        Sprite targetSprite = GetSpriteByState(displayedState);

        if (targetSprite == null)
        {
            Debug.LogWarning("BattleActionSlotUIView 缺少状态 " + currentBaseState + " 对应的 Sprite。");
            return;
        }

        slotImage.sprite = targetSprite;
    }

    private void TryBindImage()
    {
        if (slotImage == null)
        {
            slotImage = GetComponent<Image>();
        }
    }

    private void TryBindSelectionEffect()
    {
        if (selectionEffectView == null)
        {
            selectionEffectView =
                GetComponentInChildren<
                    BattleActionSlotSelectionEffectUIView
                >(true);
        }

        if (selectionEffectView == null &&
            !warnedMissingSelectionEffect)
        {
            Debug.LogWarning(
                "BattleActionSlotUIView 缺少 Selection Effect View，基础状态和点击业务仍可正常使用。",
                this
            );
            warnedMissingSelectionEffect = true;
        }
    }

    private void RefreshSelectionEffectReference()
    {
        TryBindSelectionEffect();
    }

    private void ScheduleStateFeedbackCommit()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            CommitStableStateFeedback();
            return;
        }

        if (stateFeedbackCoroutine == null)
        {
            stateFeedbackCoroutine = StartCoroutine(
                CommitStateFeedbackAtFrameEnd()
            );
        }
    }

    private IEnumerator CommitStateFeedbackAtFrameEnd()
    {
        yield return null;
        stateFeedbackCoroutine = null;
        CommitStableStateFeedback();
    }

    private void CommitStableStateFeedback()
    {
        bool shouldPlayActionSetPulse =
            hasCommittedState &&
            committedState != BattleActionSlotUIState.AllyActionSet &&
            currentBaseState ==
                BattleActionSlotUIState.AllyActionSet &&
            !isEnemySlot;

        committedState = currentBaseState;
        hasCommittedState = true;

        if (!shouldPlayActionSetPulse)
        {
            return;
        }

        RefreshSelectionEffectReference();
        selectionEffectView?.SetPersistentVisible(
            isSelected || isHovered
        );
        selectionEffectView?.PlayPulse();
    }

    private void StopStateFeedbackCommit()
    {
        if (stateFeedbackCoroutine == null)
        {
            return;
        }

        StopCoroutine(stateFeedbackCoroutine);
        stateFeedbackCoroutine = null;
    }

    private Sprite GetSpriteByState(BattleActionSlotUIState state)
    {
        switch (state)
        {
            case BattleActionSlotUIState.AllyEmpty:
                return slotEmptySprite;
            case BattleActionSlotUIState.AllyActionSet:
                return slotAllyActionSetSprite;
            case BattleActionSlotUIState.AllyTargetedNoAction:
                return slotAllyTargetedNoActionSprite;
            case BattleActionSlotUIState.EnemyEmpty:
                return slotEnemyEmptySprite;
            case BattleActionSlotUIState.EnemyActionSet:
                return slotEnemyActionSetSprite;
            default:
                return null;
        }
    }

    void OnDisable()
    {
        StopStateFeedbackCommit();
        isHovered = false;
        selectionEffectView?.StopAndReset();
        RefreshDisplayedSprite();
    }
}
