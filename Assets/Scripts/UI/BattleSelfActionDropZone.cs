using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 保留类名以兼容现有场景引用；正式职责已经迁移为自身目标点击区。
public sealed class BattleSelfActionDropZone : MonoBehaviour,
    IPointerClickHandler
{
    private CharacterData boundCharacter;
    private Action<BattleSelfActionDropZone> clickHandler;

    public CharacterData BoundCharacter => boundCharacter;

    public void Bind(
        CharacterData character,
        Action<BattleSelfActionDropZone> onClicked
    )
    {
        boundCharacter = character;
        clickHandler = onClicked;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (boundCharacter == null ||
            eventData == null ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        clickHandler?.Invoke(this);
    }
}
