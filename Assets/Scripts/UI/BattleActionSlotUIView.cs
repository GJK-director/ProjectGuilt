using System;
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

public class BattleActionSlotUIView : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    [SerializeField] private Image slotImage;

    [SerializeField] private Sprite slotEmptySprite;
    [SerializeField] private Sprite slotAllyActionSetSprite;
    [SerializeField] private Sprite slotAllyTargetedNoActionSprite;
    [SerializeField] private Sprite slotEnemyEmptySprite;
    [SerializeField] private Sprite slotEnemyActionSetSprite;
    [SerializeField] private Sprite slotSelectedSprite;

    [SerializeField] private BattleActionSlotUIState defaultState = BattleActionSlotUIState.AllyEmpty;

    private BattleActionSlotUIState currentBaseState;
    private bool isSelected;
    private CharacterData boundCharacter;
    private int slotIndex = -1;
    private bool isEnemySlot;
    private BattleEnemyIntent boundEnemyIntent;
    private Action<BattleActionSlotUIView> leftClickHandler;
    private Action<BattleActionSlotUIView> rightClickHandler;
    private Action<BattleActionSlotUIView, BattleCardUIView> cardDropHandler;

    public CharacterData BoundCharacter => boundCharacter;
    public int SlotIndex => slotIndex;
    public int UISlotIndex => slotIndex;
    public int FormalSlotIndex => slotIndex >= 0 ? slotIndex + 1 : -1;
    public bool IsEnemySlot => isEnemySlot;
    public bool IsSelected => isSelected;
    public BattleActionSlotUIState CurrentBaseState => currentBaseState;
    public BattleEnemyIntent BoundEnemyIntent => boundEnemyIntent;

    void Reset()
    {
        TryBindImage();
    }

    void Awake()
    {
        TryBindImage();
        currentBaseState = defaultState;
        isSelected = false;
        RefreshDisplayedSprite();
    }

    public void SetState(BattleActionSlotUIState state)
    {
        currentBaseState = state;
        RefreshDisplayedSprite();
    }

    public void SetDefaultState()
    {
        SetState(defaultState);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshDisplayedSprite();
    }

    public void BindInteraction(
        CharacterData character,
        int index,
        bool enemySlot,
        Action<BattleActionSlotUIView> onClicked
    )
    {
        BindInteraction(character, index, enemySlot, onClicked, null, null);
    }

    public void BindInteraction(
        CharacterData character,
        int index,
        bool enemySlot,
        Action<BattleActionSlotUIView> onLeftClicked,
        Action<BattleActionSlotUIView> onRightClicked,
        Action<BattleActionSlotUIView, BattleCardUIView> onCardDropped
    )
    {
        boundCharacter = character;
        slotIndex = index;
        isEnemySlot = enemySlot;
        leftClickHandler = onLeftClicked;
        rightClickHandler = onRightClicked;
        cardDropHandler = onCardDropped;
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
        slotSelectedSprite = sprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (boundCharacter == null || eventData == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            leftClickHandler?.Invoke(this);
            return;
        }

        if (!isEnemySlot &&
            eventData.button == PointerEventData.InputButton.Right)
        {
            rightClickHandler?.Invoke(this);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!isEnemySlot || boundCharacter == null || eventData == null || eventData.pointerDrag == null)
        {
            return;
        }

        BattleCardUIView cardView = eventData.pointerDrag.GetComponent<BattleCardUIView>();
        if (cardView == null || cardView.BoundCardState == null)
        {
            return;
        }

        cardDropHandler?.Invoke(this, cardView);
    }

    private void RefreshDisplayedSprite()
    {
        TryBindImage();

        if (slotImage == null)
        {
            Debug.LogWarning("BattleActionSlotUIView 缺少 slotImage，无法刷新行动槽图标。");
            return;
        }

        if (isSelected)
        {
            if (slotSelectedSprite != null)
            {
                slotImage.sprite = slotSelectedSprite;
            }
            else
            {
                Debug.LogWarning("BattleActionSlotUIView 缺少选中状态 Sprite。");
            }

            return;
        }

        Sprite targetSprite = GetSpriteByState(currentBaseState);

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
}
