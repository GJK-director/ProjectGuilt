using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleUnitViewSpawner : MonoBehaviour
{
    private const int MinimumUnitCountPerCamp = 1;
    private const int MaximumUnitCountPerCamp = 2;

    private sealed class WorldVisualReferences
    {
        public SpriteRenderer renderer;
        public Transform headAnchor;
        public Transform footAnchor;
        public Transform centerAnchor;
        public Transform targetAnchor;
    }

    [Header("世界角色Prefab")]
    [SerializeField] private GameObject allyWorldPrefab;
    [SerializeField] private GameObject enemyWorldPrefab;

    [Header("状态UI Prefab")]
    [SerializeField] private GameObject allyStatusUIPrefab;
    [SerializeField] private GameObject enemyStatusUIPrefab;

    [Header("出生点")]
    [SerializeField] private Transform allySpawn01;
    [SerializeField] private Transform allySpawn02;
    [SerializeField] private Transform enemySpawn01;
    [SerializeField] private Transform enemySpawn02;

    [Header("生成父节点")]
    [SerializeField] private Transform generatedAlliesRoot;
    [SerializeField] private Transform generatedEnemiesRoot;
    [SerializeField] private RectTransform worldFollowUIRoot;
    [SerializeField] private Canvas statusCanvas;
    [SerializeField] private Camera worldCamera;

    [Header("现有系统")]
    [SerializeField]
    private BattleActionRelationLineController relationLineController;

    private readonly List<BattleUnitViewHandle> generatedHandles =
        new List<BattleUnitViewHandle>();
    private readonly List<GameObject> generatedWorldObjects =
        new List<GameObject>();
    private readonly List<GameObject> generatedStatusUIObjects =
        new List<GameObject>();
    private bool isSpawned;
    private bool isClearing;

    public bool IsSpawned => isSpawned;
    public Camera WorldCamera => worldCamera;
    public IReadOnlyList<BattleUnitViewHandle> GeneratedHandles =>
        generatedHandles;
    public event Action SpawnCompleted;
    public event Action GeneratedViewsCleared;

    public bool Spawn(BattleRuntimeState runtimeState)
    {
        if (isSpawned)
        {
            Debug.LogWarning(
                name + " 重复生成被阻止：本场战斗的角色表现已经生成。",
                this
            );
            return false;
        }

        string errorMessage;
        if (!ValidateSpawnRequest(runtimeState, out errorMessage))
        {
            Debug.LogError(name + " 生成失败：" + errorMessage, this);
            return false;
        }

        Transform[] allySpawns = { allySpawn01, allySpawn02 };
        Transform[] enemySpawns = { enemySpawn01, enemySpawn02 };

        relationLineController.BindRuntimeState(runtimeState);

        for (int index = 0; index < runtimeState.allyUnits.Count; index++)
        {
            if (!TrySpawnUnit(
                runtimeState.allyUnits[index],
                BattleUnitCamp.Ally,
                allyWorldPrefab,
                allyStatusUIPrefab,
                allySpawns[index],
                generatedAlliesRoot,
                "Ally_0" + (index + 1),
                "CharacterStatus_Ally0" + (index + 1),
                out errorMessage))
            {
                Debug.LogError(name + " 生成失败：" + errorMessage, this);
                ClearGeneratedViews();
                return false;
            }
        }

        for (int index = 0; index < runtimeState.enemyUnits.Count; index++)
        {
            if (!TrySpawnUnit(
                runtimeState.enemyUnits[index],
                BattleUnitCamp.Enemy,
                enemyWorldPrefab,
                enemyStatusUIPrefab,
                enemySpawns[index],
                generatedEnemiesRoot,
                "Enemy_0" + (index + 1),
                "CharacterStatus_Enemy0" + (index + 1),
                out errorMessage))
            {
                Debug.LogError(name + " 生成失败：" + errorMessage, this);
                ClearGeneratedViews();
                return false;
            }
        }

        isSpawned = true;
        relationLineController.RefreshRelations();
        SpawnCompleted?.Invoke();
        return true;
    }

    public void RefreshGeneratedViews()
    {
        for (int index = 0; index < generatedHandles.Count; index++)
        {
            BattleUnitViewHandle handle = generatedHandles[index];
            if (handle == null || handle.StatusView == null)
            {
                continue;
            }

            handle.StatusView.SetCharacter(handle.RuntimeUnit);
            handle.WorldFollower?.RefreshNow();
        }
    }

    public void SetPlanningCharacterUIVisible(bool visible)
    {
        for (int index = 0; index < generatedHandles.Count; index++)
        {
            generatedHandles[index]?.StatusView?.SetHeadStatusVisible(visible);
        }
    }

    public void RestoreGeneratedUnitWorldPoses()
    {
        for (int index = 0; index < generatedHandles.Count; index++)
        {
            BattleUnitViewHandle handle = generatedHandles[index];
            if (handle == null)
            {
                continue;
            }

            if (handle.WorldRoot != null)
            {
                // 使用每个Runtime Unit生成完成后的真实姿态，不依赖阵营或数量。
                handle.WorldRoot.transform.SetPositionAndRotation(
                    handle.InitialWorldPosition,
                    handle.InitialWorldRotation
                );
            }

            handle.PresentationController?.ResetToStableIdlePresentation();
        }
    }

    public BattleUnitViewHandle GetHandle(CharacterData runtimeUnit)
    {
        for (int index = 0; index < generatedHandles.Count; index++)
        {
            if (generatedHandles[index] != null &&
                object.ReferenceEquals(
                    generatedHandles[index].RuntimeUnit,
                    runtimeUnit))
            {
                return generatedHandles[index];
            }
        }
        return null;
    }

    public void ClearGeneratedViews()
    {
        if (isClearing)
        {
            return;
        }

        bool hadGeneratedViews = isSpawned ||
            generatedHandles.Count > 0 ||
            generatedWorldObjects.Count > 0 ||
            generatedStatusUIObjects.Count > 0;
        isClearing = true;
        for (int index = 0; index < generatedHandles.Count; index++)
        {
            BattleUnitViewHandle handle = generatedHandles[index];
            if (handle == null)
            {
                continue;
            }

            if (relationLineController != null)
            {
                relationLineController.UnregisterSlotViews(
                    handle.ActionSlotViews
                );
            }

            for (int relayIndex = 0;
                relayIndex < handle.HoverRelays.Count;
                relayIndex++)
            {
                if (handle.HoverRelays[relayIndex] != null)
                {
                    handle.HoverRelays[relayIndex].Bind(null, null);
                }
            }

            if (handle.StatusView != null)
            {
                handle.StatusView.SetAllySlotInteractionHandlers(null, null);
                handle.StatusView.SetEnemySlotClickHandler(null);
                handle.StatusView.SetSelfTargetClickHandler(null);
                handle.StatusView.Clear();
            }

            handle.WorldFollower?.ClearWorldAnchors();
        }

        DestroyGeneratedObjects(generatedStatusUIObjects);
        DestroyGeneratedObjects(generatedWorldObjects);
        generatedHandles.Clear();
        generatedStatusUIObjects.Clear();
        generatedWorldObjects.Clear();
        isSpawned = false;
        isClearing = false;
        if (hadGeneratedViews)
        {
            GeneratedViewsCleared?.Invoke();
        }
    }

    private void OnDestroy()
    {
        ClearGeneratedViews();
    }

    private bool ValidateSpawnRequest(
        BattleRuntimeState runtimeState,
        out string errorMessage
    )
    {
        errorMessage = string.Empty;
        if (runtimeState == null)
        {
            errorMessage = "BattleRuntimeState为空";
            return false;
        }
        if (!HasSupportedUnitCount(runtimeState.allyUnits))
        {
            errorMessage = "友方运行时单位数量必须为1到2";
            return false;
        }
        if (!HasSupportedUnitCount(runtimeState.enemyUnits))
        {
            errorMessage = "敌方运行时单位数量必须为1到2";
            return false;
        }
        if (!ValidateRuntimeUnits(runtimeState, out errorMessage) ||
            !ValidateSceneReferences(runtimeState, out errorMessage) ||
            !ValidateWorldPrefab(allyWorldPrefab, "友方", out errorMessage) ||
            !ValidateWorldPrefab(enemyWorldPrefab, "敌方", out errorMessage) ||
            !ValidateStatusPrefab(allyStatusUIPrefab, "友方", out errorMessage) ||
            !ValidateStatusPrefab(enemyStatusUIPrefab, "敌方", out errorMessage))
        {
            return false;
        }
        return true;
    }

    private bool ValidateRuntimeUnits(
        BattleRuntimeState runtimeState,
        out string errorMessage
    )
    {
        errorMessage = string.Empty;
        HashSet<CharacterData> references = new HashSet<CharacterData>();
        HashSet<string> runtimeIDs = new HashSet<string>(StringComparer.Ordinal);
        List<CharacterData> units = new List<CharacterData>();
        units.AddRange(runtimeState.allyUnits);
        units.AddRange(runtimeState.enemyUnits);

        for (int index = 0; index < units.Count; index++)
        {
            CharacterData unit = units[index];
            if (unit == null)
            {
                errorMessage = "第" + (index + 1) + "个运行时单位为空";
                return false;
            }
            if (!references.Add(unit))
            {
                errorMessage = "阵营列表包含相同RuntimeUnit引用：" +
                    unit.characterName;
                return false;
            }
            if (string.IsNullOrEmpty(unit.runtimeUnitID) ||
                !runtimeIDs.Add(unit.runtimeUnitID))
            {
                errorMessage = "单位运行时ID为空或重复：" +
                    unit.runtimeUnitID;
                return false;
            }
        }

        for (int allyIndex = 0;
            allyIndex < runtimeState.allyUnits.Count;
            allyIndex++)
        {
            for (int slotIndex = 1; slotIndex <= 2; slotIndex++)
            {
                if (!HasRuntimeActionSlot(
                        runtimeState.actionSlots,
                        runtimeState.allyUnits[allyIndex],
                        slotIndex))
                {
                    errorMessage = "友方运行时行动槽不足2：" +
                        runtimeState.allyUnits[allyIndex].runtimeUnitID;
                    return false;
                }
            }
        }
        return true;
    }

    private bool ValidateSceneReferences(
        BattleRuntimeState runtimeState,
        out string errorMessage
    )
    {
        errorMessage = string.Empty;
        if (allyWorldPrefab == null || enemyWorldPrefab == null)
            errorMessage = "世界Prefab为空";
        else if (allyStatusUIPrefab == null || enemyStatusUIPrefab == null)
            errorMessage = "状态UI Prefab为空";
        else if (allySpawn01 == null || enemySpawn01 == null)
            errorMessage = "第一组SpawnPoint为空";
        else if (runtimeState.allyUnits.Count > 1 && allySpawn02 == null)
            errorMessage = "第二名友方存在但AllySpawn_02为空";
        else if (runtimeState.enemyUnits.Count > 1 && enemySpawn02 == null)
            errorMessage = "第二名敌方存在但EnemySpawn_02为空";
        else if (generatedAlliesRoot == null || generatedEnemiesRoot == null)
            errorMessage = "Generated Root为空";
        else if (worldFollowUIRoot == null)
            errorMessage = "WorldFollowUI Root为空";
        else if (statusCanvas == null)
            errorMessage = "状态UI Canvas为空";
        else if (worldCamera == null)
            errorMessage = "世界摄像机为空";
        else if (relationLineController == null)
            errorMessage = "关系线Controller为空";

        return string.IsNullOrEmpty(errorMessage);
    }

    private static bool HasSupportedUnitCount(List<CharacterData> units)
    {
        return units != null &&
            units.Count >= MinimumUnitCountPerCamp &&
            units.Count <= MaximumUnitCountPerCamp;
    }

    private bool ValidateWorldPrefab(
        GameObject prefab,
        string label,
        out string errorMessage
    )
    {
        WorldVisualReferences references;
        if (!TryResolveWorldVisualReferences(
                prefab,
                "世界Prefab",
                out references,
                out errorMessage))
        {
            errorMessage = label + "：" + errorMessage;
            return false;
        }

        Vector3 scale = prefab.transform.localScale;
        if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
        {
            errorMessage = label + "世界Prefab根节点必须使用正Scale";
            return false;
        }
        return true;
    }

    private bool ValidateStatusPrefab(
        GameObject prefab,
        string label,
        out string errorMessage
    )
    {
        errorMessage = string.Empty;
        BattleCharacterStatusUIView statusView =
            prefab.GetComponent<BattleCharacterStatusUIView>();
        if (statusView == null)
        {
            errorMessage = label + "状态UI缺少BattleCharacterStatusUIView";
            return false;
        }
        if (prefab.GetComponent<BattleCharacterStatusWorldFollower>() == null)
        {
            errorMessage = label +
                "状态UI缺少BattleCharacterStatusWorldFollower";
            return false;
        }
        if (statusView.GetSlotView(0) == null ||
            statusView.GetSlotView(1) == null)
        {
            errorMessage = label + "状态UI行动槽不足2";
            return false;
        }
        bool shouldBeEnemy = label == "敌方";
        if (statusView.IsEnemyView != shouldBeEnemy)
        {
            errorMessage = label + "状态UI的敌我标记配置错误";
            return false;
        }
        return true;
    }

    private bool TrySpawnUnit(
        CharacterData runtimeUnit,
        BattleUnitCamp camp,
        GameObject worldPrefab,
        GameObject statusPrefab,
        Transform spawnPoint,
        Transform worldParent,
        string worldName,
        string statusName,
        out string errorMessage
    )
    {
        errorMessage = string.Empty;
        GameObject worldRoot = Instantiate(
            worldPrefab,
            spawnPoint.position,
            spawnPoint.rotation,
            worldParent
        );
        worldRoot.name = worldName;
        generatedWorldObjects.Add(worldRoot);

        WorldVisualReferences worldVisual;
        if (!TryResolveWorldVisualReferences(
                worldRoot,
                "世界实例",
                out worldVisual,
                out errorMessage))
        {
            return false;
        }

        BattleCharacterPresentationController presentationController =
            worldRoot.GetComponent<BattleCharacterPresentationController>();
        // 素材原始朝向与阵营目标朝向共同决定最终镜像，不改Sprite或Scale。
        ApplyCampVisualSettings(
            worldVisual.renderer,
            presentationController,
            camp
        );
        if (camp == BattleUnitCamp.Ally && worldName == "Ally_01")
        {
            // 临时诊断仅观察第一名友方的锚点与可见脚底投影差异。
            BattleWorldFollowProjectionDiagnostic diagnostic =
                worldRoot.AddComponent<
                    BattleWorldFollowProjectionDiagnostic>();
            diagnostic.Bind(
                worldCamera,
                worldVisual.footAnchor,
                worldVisual.renderer
            );
        }
        GameObject statusRoot = Instantiate(
            statusPrefab,
            worldFollowUIRoot,
            false
        );
        statusRoot.name = statusName;
        generatedStatusUIObjects.Add(statusRoot);

        BattleCharacterStatusUIView statusView =
            statusRoot.GetComponent<BattleCharacterStatusUIView>();
        BattleCharacterStatusWorldFollower follower =
            statusRoot.GetComponent<BattleCharacterStatusWorldFollower>();
        if (statusView == null || follower == null)
        {
            errorMessage = statusName + "缺少状态UI绑定组件";
            return false;
        }

        statusView.SetCharacter(runtimeUnit);
        follower.Bind(
            worldCamera,
            statusCanvas,
            worldVisual.headAnchor,
            worldVisual.footAnchor,
            worldVisual.centerAnchor
        );
        follower.SetVisualFootSource(worldVisual.renderer);

        List<BattleActionSlotUIView> slots =
            new List<BattleActionSlotUIView>();
        List<BattleActionSlotRelationHoverRelay> relays =
            new List<BattleActionSlotRelationHoverRelay>();
        for (int slotIndex = 0; slotIndex < 2; slotIndex++)
        {
            BattleActionSlotUIView slotView = statusView.GetSlotView(slotIndex);
            if (slotView == null)
            {
                errorMessage = statusName + "缺少槽位" + (slotIndex + 1);
                return false;
            }

            BattleActionSlotRelationHoverRelay relay =
                slotView.GetComponent<BattleActionSlotRelationHoverRelay>();
            if (relay == null)
            {
                errorMessage = statusName + "槽位" + (slotIndex + 1) +
                    "缺少Hover Relay";
                return false;
            }

            slots.Add(slotView);
            relays.Add(relay);
        }

        for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
        {
            relationLineController.RegisterSlotView(slots[slotIndex]);
            if (!relationLineController.IsSlotViewRegistered(
                    slots[slotIndex]))
            {
                relationLineController.UnregisterSlotViews(slots);
                errorMessage = statusName + "槽位" + (slotIndex + 1) +
                    "注册关系线失败";
                return false;
            }
            relays[slotIndex].Bind(
                relationLineController,
                slots[slotIndex]
            );
        }

        generatedHandles.Add(new BattleUnitViewHandle(
            runtimeUnit,
            camp,
            worldRoot,
            worldVisual.renderer,
            presentationController,
            worldVisual.headAnchor,
            worldVisual.footAnchor,
            worldVisual.centerAnchor,
            worldVisual.targetAnchor,
            statusRoot,
            statusView,
            follower,
            slots,
            relays
        ));
        return true;
    }

    private static bool TryResolveWorldVisualReferences(
        GameObject root,
        string objectKind,
        out WorldVisualReferences references,
        out string errorMessage
    )
    {
        references = new WorldVisualReferences();
        errorMessage = string.Empty;
        if (root == null)
        {
            errorMessage = objectKind + "为空。";
            return false;
        }

        BattleCharacterPresentationController presentationController =
            root.GetComponent<BattleCharacterPresentationController>();
        if (presentationController != null)
        {
            // 特效层增加后，层级中的第一个SpriteRenderer不一定是角色主体。
            references.renderer =
                presentationController.CharacterSpriteRenderer;
        }

        // 仅在初始化和预检时遍历一次，并包含Inactive子节点。
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int index = 0; index < transforms.Length; index++)
        {
            Transform current = transforms[index];
            if (references.renderer == null)
            {
                references.renderer = current.GetComponent<SpriteRenderer>();
            }

            if (current.name == "HeadUIAnchor")
            {
                references.headAnchor = current;
            }
            else if (current.name == "FootUIAnchor")
            {
                references.footAnchor = current;
            }
            else if (current.name == "CenterAnchor")
            {
                references.centerAnchor = current;
            }
            else if (current.name == "TargetAnchor")
            {
                references.targetAnchor = current;
            }
        }

        string displayName = root.name;
        if (references.renderer == null)
        {
            errorMessage = objectKind + "“" + displayName +
                "”及其子节点中均未找到 SpriteRenderer。";
            return false;
        }
        if (references.headAnchor == null)
        {
            errorMessage = objectKind + "“" + displayName +
                "”缺少 HeadUIAnchor。";
            return false;
        }
        if (references.footAnchor == null)
        {
            errorMessage = objectKind + "“" + displayName +
                "”缺少 FootUIAnchor。";
            return false;
        }
        if (references.centerAnchor == null)
        {
            errorMessage = objectKind + "“" + displayName +
                "”缺少 CenterAnchor。";
            return false;
        }
        if (references.targetAnchor == null)
        {
            errorMessage = objectKind + "“" + displayName +
                "”缺少 TargetAnchor。";
            return false;
        }
        return true;
    }

    private static void ApplyCampVisualSettings(
        SpriteRenderer renderer,
        BattleCharacterPresentationController presentationController,
        BattleUnitCamp camp
    )
    {
        if (renderer == null)
        {
            return;
        }

        if (presentationController == null)
        {
            if (camp == BattleUnitCamp.Enemy)
            {
                renderer.flipX = true;
            }
            return;
        }

        bool desiredFacesRight = camp == BattleUnitCamp.Ally;
        renderer.flipX = presentationController.SourceFacesRight !=
            desiredFacesRight;
    }

    private static bool HasRuntimeActionSlot(
        List<BattleActionSlot> slots,
        CharacterData owner,
        int slotIndex
    )
    {
        if (slots == null || owner == null)
        {
            return false;
        }
        for (int index = 0; index < slots.Count; index++)
        {
            BattleActionSlot slot = slots[index];
            if (slot != null &&
                object.ReferenceEquals(slot.owner, owner) &&
                slot.slotIndex == slotIndex)
            {
                return true;
            }
        }
        return false;
    }

    private static void DestroyGeneratedObjects(List<GameObject> objects)
    {
        for (int index = objects.Count - 1; index >= 0; index--)
        {
            if (objects[index] == null)
            {
                continue;
            }
            if (Application.isPlaying)
            {
                Destroy(objects[index]);
            }
            else
            {
                DestroyImmediate(objects[index]);
            }
        }
    }
}
