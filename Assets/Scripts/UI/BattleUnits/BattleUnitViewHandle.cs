using System.Collections.Generic;
using UnityEngine;

public enum BattleUnitCamp
{
    Ally,
    Enemy
}

// 保存一名正式运行时单位对应的完整表现引用，避免其他系统重复遍历层级。
public sealed class BattleUnitViewHandle
{
    public CharacterData RuntimeUnit { get; }
    public string RuntimeUnitID => RuntimeUnit != null
        ? RuntimeUnit.runtimeUnitID
        : string.Empty;
    public BattleUnitCamp Camp { get; }
    public GameObject WorldRoot { get; }
    public SpriteRenderer WorldRenderer { get; }
    public BattleCharacterPresentationController PresentationController { get; }
    public Transform HeadUIAnchor { get; }
    public Transform FootUIAnchor { get; }
    public Transform CenterAnchor { get; }
    public Transform TargetAnchor { get; }
    public GameObject StatusUIRoot { get; }
    public BattleCharacterStatusUIView StatusView { get; }
    public BattleCharacterStatusWorldFollower WorldFollower { get; }
    public IReadOnlyList<BattleActionSlotUIView> ActionSlotViews =>
        actionSlotViews;
    public IReadOnlyList<BattleActionSlotRelationHoverRelay> HoverRelays =>
        hoverRelays;

    private readonly List<BattleActionSlotUIView> actionSlotViews;
    private readonly List<BattleActionSlotRelationHoverRelay> hoverRelays;

    public BattleUnitViewHandle(
        CharacterData runtimeUnit,
        BattleUnitCamp camp,
        GameObject worldRoot,
        SpriteRenderer worldRenderer,
        BattleCharacterPresentationController presentationController,
        Transform headUIAnchor,
        Transform footUIAnchor,
        Transform centerAnchor,
        Transform targetAnchor,
        GameObject statusUIRoot,
        BattleCharacterStatusUIView statusView,
        BattleCharacterStatusWorldFollower worldFollower,
        List<BattleActionSlotUIView> slotViews,
        List<BattleActionSlotRelationHoverRelay> relays
    )
    {
        RuntimeUnit = runtimeUnit;
        Camp = camp;
        WorldRoot = worldRoot;
        WorldRenderer = worldRenderer;
        PresentationController = presentationController;
        HeadUIAnchor = headUIAnchor;
        FootUIAnchor = footUIAnchor;
        CenterAnchor = centerAnchor;
        TargetAnchor = targetAnchor;
        StatusUIRoot = statusUIRoot;
        StatusView = statusView;
        WorldFollower = worldFollower;
        actionSlotViews = slotViews ?? new List<BattleActionSlotUIView>();
        hoverRelays = relays ??
            new List<BattleActionSlotRelationHoverRelay>();
    }
}
