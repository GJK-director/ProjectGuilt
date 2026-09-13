using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 保留类名以兼容现有场景引用；正式职责已经迁移为自身目标点击区。
public sealed class BattleSelfActionDropZone : MonoBehaviour,
    IPointerClickHandler
{
    [SerializeField] private Graphic targetGraphic;

    private CharacterData boundCharacter;
    private Action<BattleSelfActionDropZone> clickHandler;
    private bool interactionEnabled = true;
    private bool defaultRaycastTarget;
    private bool hasCachedRaycastTarget;

    public CharacterData BoundCharacter => boundCharacter;

    private void Awake()
    {
        CacheRaycastTarget();
        ApplyInteractionState();
    }

    public void Bind(
        CharacterData character,
        Action<BattleSelfActionDropZone> onClicked
    )
    {
        boundCharacter = character;
        clickHandler = onClicked;
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
        CacheRaycastTarget();
        ApplyInteractionState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactionEnabled ||
            boundCharacter == null ||
            eventData == null ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        clickHandler?.Invoke(this);
    }

    private void CacheRaycastTarget()
    {
        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }

        if (targetGraphic != null && !hasCachedRaycastTarget)
        {
            defaultRaycastTarget = targetGraphic.raycastTarget;
            hasCachedRaycastTarget = true;
        }
    }

    private void ApplyInteractionState()
    {
        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = interactionEnabled &&
                defaultRaycastTarget;
        }
    }
}
