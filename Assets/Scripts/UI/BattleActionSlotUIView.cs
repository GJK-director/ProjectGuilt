using UnityEngine;
using UnityEngine.UI;

public enum BattleActionSlotUIState
{
    AllyEmpty,
    AllyActionSet,
    AllyTargetedNoAction,
    EnemyEmpty,
    EnemyActionSet
}

public class BattleActionSlotUIView : MonoBehaviour
{
    [SerializeField] private Image slotImage;

    [SerializeField] private Sprite slotEmptySprite;
    [SerializeField] private Sprite slotAllyActionSetSprite;
    [SerializeField] private Sprite slotAllyTargetedNoActionSprite;
    [SerializeField] private Sprite slotEnemyEmptySprite;
    [SerializeField] private Sprite slotEnemyActionSetSprite;

    [SerializeField] private BattleActionSlotUIState defaultState = BattleActionSlotUIState.AllyEmpty;

    void Reset()
    {
        TryBindImage();
    }

    void Awake()
    {
        TryBindImage();
        SetDefaultState();
    }

    public void SetState(BattleActionSlotUIState state)
    {
        TryBindImage();

        if (slotImage == null)
        {
            Debug.LogWarning("BattleActionSlotUIView 缺少 slotImage，无法刷新行动槽图标。");
            return;
        }

        Sprite targetSprite = GetSpriteByState(state);

        if (targetSprite == null)
        {
            Debug.LogWarning("BattleActionSlotUIView 缺少状态 " + state + " 对应的 Sprite。");
            return;
        }

        slotImage.sprite = targetSprite;
    }

    public void SetDefaultState()
    {
        SetState(defaultState);
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
