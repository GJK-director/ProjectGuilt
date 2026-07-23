using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 角色自身放置区只转发拖放事件，不负责判断卡牌是否合法。
public sealed class BattleSelfActionDropZone : MonoBehaviour, IDropHandler
{
    private CharacterData boundCharacter;
    private Action<BattleSelfActionDropZone, BattleCardUIView> cardDropHandler;

    public CharacterData BoundCharacter => boundCharacter;

    public void Bind(
        CharacterData character,
        Action<BattleSelfActionDropZone, BattleCardUIView> onCardDropped
    )
    {
        boundCharacter = character;
        cardDropHandler = onCardDropped;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (boundCharacter == null ||
            eventData == null ||
            eventData.pointerDrag == null)
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
}
