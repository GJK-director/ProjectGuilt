using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 角色自身目标的独立 UI 命中区。它只负责输入转发，不包含卡牌规则。
[DisallowMultipleComponent]
public sealed class BattleCharacterTargetHitbox : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Graphic targetGraphic;

    private CharacterData boundCharacter;
    private Action<BattleCharacterTargetHitbox> clickHandler;
    private Action<BattleCharacterTargetHitbox> enterHandler;
    private Action<BattleCharacterTargetHitbox> exitHandler;
    private bool targetingActive;
    private bool warnedMissingGraphic;

    public CharacterData BoundCharacter => boundCharacter;
    public bool TargetingActive => targetingActive;
    public RectTransform TargetRectTransform =>
        transform as RectTransform;

    public void Bind(
        CharacterData character,
        Action<BattleCharacterTargetHitbox> onClicked,
        Action<BattleCharacterTargetHitbox> onEntered,
        Action<BattleCharacterTargetHitbox> onExited
    )
    {
        boundCharacter = character;
        clickHandler = onClicked;
        enterHandler = onEntered;
        exitHandler = onExited;
        ApplyRaycastState();
    }

    public void SetTargetingActive(bool active)
    {
        Graphic graphic = ResolveGraphic();
        if (active && graphic == null)
        {
            if (!warnedMissingGraphic)
            {
                warnedMissingGraphic = true;
                Debug.LogWarning(
                    "BattleCharacterTargetHitbox 缺少 Graphic，已安全关闭目标输入。",
                    this
                );
            }
            targetingActive = false;
            ApplyRaycastState();
            return;
        }

        targetingActive = active && boundCharacter != null;
        ApplyRaycastState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CanInteract(eventData) ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        clickHandler?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanInteract(eventData))
        {
            return;
        }

        enterHandler?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!CanInteract(eventData))
        {
            return;
        }

        exitHandler?.Invoke(this);
    }

    private bool CanInteract(PointerEventData eventData)
    {
        return targetingActive &&
            boundCharacter != null &&
            eventData != null;
    }

    private Graphic ResolveGraphic()
    {
        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }

        return targetGraphic;
    }

    private void ApplyRaycastState()
    {
        Graphic graphic = ResolveGraphic();
        if (graphic != null)
        {
            graphic.raycastTarget = targetingActive;
        }
    }

    private void OnDisable()
    {
        targetingActive = false;
        ApplyRaycastState();
    }
}
