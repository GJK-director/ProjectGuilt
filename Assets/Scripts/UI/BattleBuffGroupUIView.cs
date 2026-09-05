using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattleBuffIconBinding
{
    public string buffID;
    public string displayName;
    public Sprite iconSprite;

    [Tooltip("兼容旧场景的预制Buff槽位引用。运行时作为共享槽位池使用，不再与本条Buff永久绑定。")]
    public BattleBuffIconUIView iconView;
}

internal sealed class BattleBuffDisplayEntry
{
    public string buffID;
    public string displayName;
    public int totalStack;
    public Sprite iconSprite;
    public string description;
    public int duration;
    public string expireRule;
}

public class BattleBuffGroupUIView : MonoBehaviour
{
    private static readonly HashSet<string> PubliclyVisibleBuffIDs =
        new HashSet<string>(StringComparer.Ordinal)
        {
            BattleResourceID.Bullet,
            BattleResourceID.Anger,
            BattleResourceID.Modification,
            BattleResourceID.Conservation
        };

    [SerializeField] private BattleBuffIconBinding[] buffBindings;

    [Header("槽位来源")]
    [SerializeField] private RectTransform slotsRoot;
    [SerializeField] private BattleBuffIconUIView slotTemplate;

    [Header("默认图片")]
    [SerializeField] private Sprite defaultBuffIcon;
    [SerializeField] private Sprite overflowIcon;

    [Header("网格排版")]
    [Min(1)]
    [SerializeField] private int columnsPerRow = 4;

    [Range(1, 2)]
    [SerializeField] private int maxRows = 2;

    [SerializeField] private Vector2 startOffset = Vector2.zero;
    [SerializeField] private float horizontalSpacing = 6f;
    [SerializeField] private float verticalSpacing = 6f;
    [SerializeField] private string overflowPrefix = "...+";

    [Header("统一槽位尺寸")]
    [SerializeField] private bool useTemplateSlotSize = true;
    [SerializeField] private Vector2 slotSize =
        new Vector2(24f, 24f);

    private readonly List<BattleBuffIconUIView> slotPool =
        new List<BattleBuffIconUIView>();
    private bool slotPoolInitialized;
    private Vector2 resolvedSlotSize = Vector2.one;
    private CharacterData boundCharacter;
    private Action<CharacterData, int> overflowClickedHandler;
    private bool warnedDuplicateBuffIDs;
    private bool warnedMissingTemplate;
    private bool warnedNoUsableTemplate;
    private bool warnedMissingSlotsRoot;
    private bool warnedInvalidTemplate;
    private bool warnedBindingHierarchy;
    private bool warnedNestedSlots;
    private bool warnedInvalidDirectSlot;
    private int configurationWarningCount;
    private bool includeNonPublicBuffsForTesting;

    internal int SlotPoolCount => slotPool.Count;
    internal int RuntimeSlotCount => slotPool.Count;
    internal IReadOnlyList<BattleBuffIconUIView> RuntimeSlots =>
        slotPool;
    internal CharacterData BoundCharacter => boundCharacter;
    internal int ConfigurationWarningCount =>
        configurationWarningCount;
    internal Vector2 ResolvedSlotSize => resolvedSlotSize;

    internal void SetIncludeNonPublicBuffsForTesting(bool include)
    {
        includeNonPublicBuffsForTesting = include;
    }

    void Awake()
    {
        resolvedSlotSize = ResolveUniformSlotSize();
        EnsureSlotPoolInitialized();
        ClearSlots();
    }

    public void SetCharacter(CharacterData characterData)
    {
        if (characterData == null)
        {
            Clear();
            return;
        }

        boundCharacter = characterData;
        EnsureSlotPoolInitialized();

        int validColumns = Mathf.Max(1, columnsPerRow);
        int validRows = Mathf.Clamp(maxRows, 1, 2);
        int capacity = validColumns * validRows;
        List<BattleBuffDisplayEntry> entries =
            BuildDisplayEntries(characterData);

        EnsureSlotCapacity(capacity);

        int normalBuffCount = entries.Count <= capacity
            ? entries.Count
            : Mathf.Max(0, capacity - 1);
        bool hasOverflow = entries.Count > capacity;
        int usedSlotCount = normalBuffCount +
            (hasOverflow ? 1 : 0);
        int availableSlotCount = Mathf.Min(
            capacity,
            slotPool.Count
        );

        for (int index = 0;
            index < normalBuffCount &&
            index < availableSlotCount;
            index++)
        {
            BattleBuffIconUIView slot = slotPool[index];
            BattleBuffDisplayEntry entry = entries[index];
            slot.SetOverflowClickHandler(null);
            slot.SetBuff(
                entry.iconSprite,
                entry.totalStack,
                0,
                BuildSecondaryInfoContent(entry),
                "battle-buff-" + entry.buffID
            );
        }

        if (hasOverflow &&
            normalBuffCount < availableSlotCount)
        {
            int hiddenCount = entries.Count - normalBuffCount;
            BattleBuffIconUIView overflowSlot =
                slotPool[normalBuffCount];
            overflowSlot.SetOverflowClickHandler(
                clickedHiddenCount =>
                {
                    overflowClickedHandler?.Invoke(
                        boundCharacter,
                        clickedHiddenCount
                    );
                }
            );
            overflowSlot.SetOverflow(
                overflowIcon,
                hiddenCount,
                overflowPrefix
            );
        }

        for (int index = Mathf.Min(
                usedSlotCount,
                availableSlotCount
            );
            index < slotPool.Count;
            index++)
        {
            slotPool[index].SetOverflowClickHandler(null);
            slotPool[index].SetEmpty();
        }

        ApplyGridLayout(
            Mathf.Min(usedSlotCount, availableSlotCount),
            validColumns
        );
    }

    public void SetOverflowClickHandler(
        Action<CharacterData, int> handler
    )
    {
        overflowClickedHandler = handler;
    }

    public void Clear()
    {
        boundCharacter = null;
        EnsureSlotPoolInitialized();
        ClearSlots();
    }

    internal BattleBuffIconUIView GetSlotForTesting(int index)
    {
        if (index < 0 || index >= slotPool.Count)
        {
            return null;
        }

        return slotPool[index];
    }

    internal Vector2 GetExpectedSlotPosition(int index)
    {
        return CalculateSlotPosition(
            Mathf.Max(0, index),
            Mathf.Max(1, columnsPerRow),
            resolvedSlotSize
        );
    }

    private void EnsureSlotPoolInitialized()
    {
        if (slotPoolInitialized)
        {
            return;
        }

        slotPoolInitialized = true;
        RectTransform effectiveSlotsRoot =
            GetEffectiveSlotsRoot();
        ValidateConfigurationInternal(
            effectiveSlotsRoot,
            false
        );
        WarnDuplicateBuffIDsOnce();

        if (effectiveSlotsRoot == null)
        {
            return;
        }

        BattleBuffIconUIView[] candidates =
            effectiveSlotsRoot.GetComponentsInChildren<
                BattleBuffIconUIView
            >(true);
        for (int index = 0; index < candidates.Length; index++)
        {
            BattleBuffIconUIView candidate = candidates[index];
            if (candidate == null ||
                candidate == slotTemplate ||
                candidate.transform.parent !=
                    effectiveSlotsRoot ||
                !candidate.gameObject.scene.IsValid() ||
                slotPool.Contains(candidate))
            {
                continue;
            }

            if (!candidate.HasRequiredVisualReferences)
            {
                WarnOnce(
                    ref warnedInvalidDirectSlot,
                    "Buff槽位 " + candidate.name +
                    " 缺少 Icon Image 或 Stack Text 引用，" +
                    "不会加入运行时槽位池。",
                    false
                );
                continue;
            }

            slotPool.Add(candidate);
            candidate.SetOverflowClickHandler(null);
            candidate.SetEmpty();
        }
    }

    private void EnsureSlotCapacity(int capacity)
    {
        EnsureSlotPoolInitialized();
        if (capacity <= slotPool.Count)
        {
            return;
        }

        RectTransform effectiveSlotsRoot =
            GetEffectiveSlotsRoot();
        if (effectiveSlotsRoot == null)
        {
            return;
        }

        BattleBuffIconUIView template =
            GetUsableSlotTemplate(effectiveSlotsRoot);
        if (template == null)
        {
            WarnOnce(
                ref warnedNoUsableTemplate,
                "BattleBuffGroupUIView 找不到可用的 Slot Template，" +
                "无法扩展 Buff 槽位池。",
                false
            );
            return;
        }

        while (slotPool.Count < capacity)
        {
            BattleBuffIconUIView newSlot = Instantiate(
                template,
                effectiveSlotsRoot,
                false
            );
            newSlot.name =
                "BuffSlot_" + (slotPool.Count + 1).ToString("00");
            newSlot.SetOverflowClickHandler(null);
            newSlot.SetEmpty();
            slotPool.Add(newSlot);
        }
    }

    private RectTransform GetEffectiveSlotsRoot()
    {
        return slotsRoot != null
            ? slotsRoot
            : transform as RectTransform;
    }

    private BattleBuffIconUIView GetUsableSlotTemplate(
        RectTransform effectiveSlotsRoot
    )
    {
        if (IsUsableSlotTemplate(
            slotTemplate,
            effectiveSlotsRoot
        ))
        {
            return slotTemplate;
        }

        if (slotTemplate != null)
        {
            WarnOnce(
                ref warnedInvalidTemplate,
                "BattleBuffGroupUIView 的 Slot Template 配置无效。" +
                "模板必须是 Slots Root 的直接子对象，" +
                "并绑定 Icon Image 与 Stack Text。",
                false
            );
        }

        for (int index = 0; index < slotPool.Count; index++)
        {
            BattleBuffIconUIView candidate = slotPool[index];
            if (candidate != null &&
                candidate.transform.parent == effectiveSlotsRoot &&
                candidate.transform is RectTransform &&
                candidate.HasRequiredVisualReferences)
            {
                return candidate;
            }
        }

        return null;
    }

    private bool IsUsableSlotTemplate(
        BattleBuffIconUIView candidate,
        RectTransform effectiveSlotsRoot
    )
    {
        return candidate != null &&
            effectiveSlotsRoot != null &&
            candidate.transform.parent == effectiveSlotsRoot &&
            candidate.transform is RectTransform &&
            candidate.HasRequiredVisualReferences &&
            !slotPool.Contains(candidate);
    }

    [ContextMenu("验证Buff UI配置")]
    public void ValidateConfiguration()
    {
        ValidateConfigurationInternal(
            GetEffectiveSlotsRoot(),
            true
        );
    }

    private void ValidateConfigurationInternal(
        RectTransform effectiveSlotsRoot,
        bool forceLog
    )
    {
        if (effectiveSlotsRoot == null)
        {
            WarnOnce(
                ref warnedMissingSlotsRoot,
                "BattleBuffGroupUIView 找不到 Slots Root。" +
                "请将组件挂在 RectTransform 上，" +
                "或在 Inspector 中绑定 Slots Root。",
                forceLog
            );
            return;
        }

        if (slotTemplate == null)
        {
            WarnOnce(
                ref warnedMissingTemplate,
                "BattleBuffGroupUIView 尚未绑定 Slot Template。" +
                "扩容时将尝试复用 Slots Root 下第一个有效槽位。",
                forceLog
            );
        }
        else
        {
            bool templateDirectChild =
                slotTemplate.transform.parent ==
                effectiveSlotsRoot;
            bool templateVisualsValid =
                slotTemplate.HasRequiredVisualReferences;
            bool templateHasRectTransform =
                slotTemplate.transform is RectTransform;
            if (!templateDirectChild ||
                !templateVisualsValid ||
                !templateHasRectTransform)
            {
                WarnOnce(
                    ref warnedInvalidTemplate,
                    "BattleBuffGroupUIView 的 Slot Template 配置无效。" +
                    "模板必须是 Slots Root 的直接子对象，" +
                    "并绑定 Icon Image 与 Stack Text。",
                    forceLog
                );
            }
        }

        ValidateBindingSlotHierarchy(
            effectiveSlotsRoot,
            forceLog
        );
        ValidateNestedSlots(
            effectiveSlotsRoot,
            forceLog
        );
    }

    private void ValidateBindingSlotHierarchy(
        RectTransform effectiveSlotsRoot,
        bool forceLog
    )
    {
        if (buffBindings == null)
        {
            return;
        }

        for (int index = 0;
            index < buffBindings.Length;
            index++)
        {
            BattleBuffIconBinding binding = buffBindings[index];
            if (binding == null ||
                binding.iconView == null ||
                binding.iconView.transform.parent ==
                    effectiveSlotsRoot)
            {
                continue;
            }

            WarnOnce(
                ref warnedBindingHierarchy,
                "Buff槽位必须是 Slots Root 的直接子对象。" +
                "当前 Binding " + binding.buffID +
                " 引用了其他层级中的 " +
                binding.iconView.name + "，" +
                "该引用只保留图标映射，不会加入运行时槽位池。",
                forceLog
            );
            return;
        }
    }

    private void ValidateNestedSlots(
        RectTransform effectiveSlotsRoot,
        bool forceLog
    )
    {
        BattleBuffIconUIView[] allViews =
            effectiveSlotsRoot.GetComponentsInChildren<
                BattleBuffIconUIView
            >(true);
        for (int index = 0; index < allViews.Length; index++)
        {
            BattleBuffIconUIView view = allViews[index];
            if (view == null ||
                view == slotTemplate ||
                view.transform.parent == effectiveSlotsRoot)
            {
                continue;
            }

            WarnOnce(
                ref warnedNestedSlots,
                view.name + " 当前嵌套在 " +
                view.transform.parent.name + " 下。" +
                "运行时槽位必须与 BuffTemplate 同级，" +
                "并直接位于 Slots Root 下。",
                forceLog
            );
            return;
        }
    }

    private void WarnOnce(
        ref bool warningFlag,
        string message,
        bool forceLog
    )
    {
        if (forceLog || !warningFlag)
        {
            Debug.LogWarning(message, this);
        }

        if (!warningFlag)
        {
            warningFlag = true;
            configurationWarningCount++;
        }
    }

    private List<BattleBuffDisplayEntry> BuildDisplayEntries(
        CharacterData characterData
    )
    {
        List<BattleBuffDisplayEntry> entries =
            new List<BattleBuffDisplayEntry>();
        Dictionary<string, BattleBuffDisplayEntry> entryByID =
            new Dictionary<string, BattleBuffDisplayEntry>(
                StringComparer.Ordinal
            );

        if (characterData == null || characterData.buffs == null)
        {
            return entries;
        }

        for (int index = 0;
            index < characterData.buffs.Count;
            index++)
        {
            BuffData buff = characterData.buffs[index];
            if (buff == null ||
                string.IsNullOrEmpty(buff.buffID) ||
                buff.stack <= 0 ||
                (!includeNonPublicBuffsForTesting &&
                    !PubliclyVisibleBuffIDs.Contains(buff.buffID)))
            {
                continue;
            }

            BattleBuffDisplayEntry entry;
            if (entryByID.TryGetValue(buff.buffID, out entry))
            {
                entry.totalStack += buff.stack;
                continue;
            }

            BattleBuffIconBinding binding =
                FindBinding(buff.buffID);
            BuffDefinitionData definition;
            BuffDefinitionLoader.TryGetDefinition(
                buff.buffID,
                out definition
            );
            entry = new BattleBuffDisplayEntry
            {
                buffID = buff.buffID,
                displayName =
                    binding != null &&
                    !string.IsNullOrEmpty(binding.displayName)
                        ? binding.displayName
                        : buff.buffName,
                totalStack = buff.stack,
                iconSprite =
                    binding != null &&
                    binding.iconSprite != null
                        ? binding.iconSprite
                        : defaultBuffIcon,
                description =
                    definition != null
                        ? definition.description
                        : string.Empty,
                duration = buff.duration,
                expireRule = buff.expireRule
            };
            entryByID.Add(entry.buffID, entry);
            entries.Add(entry);
        }

        return entries;
    }

    private BattleSecondaryInfoContent BuildSecondaryInfoContent(
        BattleBuffDisplayEntry entry
    )
    {
        if (entry == null)
        {
            return null;
        }

        string title = !string.IsNullOrEmpty(entry.displayName)
            ? entry.displayName
            : entry.buffID;
        string body = !string.IsNullOrEmpty(entry.description)
            ? entry.description
            : "该状态暂时没有补充说明。";
        string durationText =
            entry.duration < 0 ||
            string.Equals(
                entry.expireRule,
                BuffExpireRule.Permanent,
                StringComparison.Ordinal
            )
                ? "永久"
                : entry.duration + " 回合";
        string footer =
            "当前层数：" + entry.totalStack +
            "\n持续时间：" + durationText;

        return new BattleSecondaryInfoContent(
            title,
            body,
            footer
        );
    }

    private BattleBuffIconBinding FindBinding(string buffID)
    {
        if (buffBindings == null || string.IsNullOrEmpty(buffID))
        {
            return null;
        }

        for (int index = 0;
            index < buffBindings.Length;
            index++)
        {
            BattleBuffIconBinding binding = buffBindings[index];
            if (binding != null &&
                string.Equals(
                    binding.buffID,
                    buffID,
                    StringComparison.Ordinal
                ))
            {
                return binding;
            }
        }

        return null;
    }

    private void WarnDuplicateBuffIDsOnce()
    {
        if (warnedDuplicateBuffIDs || buffBindings == null)
        {
            return;
        }

        HashSet<string> knownIDs =
            new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0;
            index < buffBindings.Length;
            index++)
        {
            BattleBuffIconBinding binding = buffBindings[index];
            if (binding == null ||
                string.IsNullOrEmpty(binding.buffID))
            {
                continue;
            }

            if (!knownIDs.Add(binding.buffID))
            {
                Debug.LogWarning(
                    "BattleBuffGroupUIView 发现重复 buffID：" +
                    binding.buffID +
                    "，将使用第一条图标映射。",
                    this
                );
                warnedDuplicateBuffIDs = true;
                return;
            }
        }
    }

    private void ApplyGridLayout(
        int slotCount,
        int validColumns
    )
    {
        resolvedSlotSize = ResolveUniformSlotSize();
        int safeColumns = Mathf.Max(1, validColumns);
        int count = Mathf.Min(slotCount, slotPool.Count);
        for (int index = 0; index < slotPool.Count; index++)
        {
            BattleBuffIconUIView slot = slotPool[index];
            RectTransform slotRect =
                slot != null
                    ? slot.transform as RectTransform
                    : null;
            if (slotRect == null)
            {
                continue;
            }

            NormalizeSlotRectTransform(slotRect);
        }

        for (int index = 0; index < count; index++)
        {
            BattleBuffIconUIView slot = slotPool[index];
            RectTransform slotRect =
                slot != null
                    ? slot.transform as RectTransform
                    : null;
            if (slotRect == null)
            {
                continue;
            }

            slotRect.anchoredPosition = CalculateSlotPosition(
                index,
                safeColumns,
                resolvedSlotSize
            );
        }
    }

    private Vector2 ResolveUniformSlotSize()
    {
        Vector2 candidate = slotSize;
        RectTransform templateRect = slotTemplate != null
            ? slotTemplate.transform as RectTransform
            : null;
        if (useTemplateSlotSize && templateRect != null)
        {
            candidate = templateRect.sizeDelta;
        }

        return new Vector2(
            Mathf.Max(1f, candidate.x),
            Mathf.Max(1f, candidate.y)
        );
    }

    private void NormalizeSlotRectTransform(
        RectTransform slotRect
    )
    {
        slotRect.anchorMin = new Vector2(0f, 1f);
        slotRect.anchorMax = new Vector2(0f, 1f);
        slotRect.pivot = new Vector2(0f, 1f);
        slotRect.localRotation = Quaternion.identity;
        slotRect.localScale = Vector3.one;
        slotRect.sizeDelta = resolvedSlotSize;
    }

    private Vector2 CalculateSlotPosition(
        int index,
        int safeColumns,
        Vector2 uniformSlotSize
    )
    {
        int column = index % safeColumns;
        int row = index / safeColumns;
        float x = startOffset.x +
            column *
            (uniformSlotSize.x + horizontalSpacing);
        float y = startOffset.y -
            row *
            (uniformSlotSize.y + verticalSpacing);
        return new Vector2(x, y);
    }

    [ContextMenu("重新应用Buff网格布局")]
    private void ReapplyCurrentLayout()
    {
        if (!Application.isPlaying && !slotPoolInitialized)
        {
            return;
        }

        if (Application.isPlaying)
        {
            EnsureSlotPoolInitialized();
        }

        int visibleSlotCount = 0;
        for (int index = 0; index < slotPool.Count; index++)
        {
            BattleBuffIconUIView slot = slotPool[index];
            if (slot == null || !slot.gameObject.activeSelf)
            {
                continue;
            }

            visibleSlotCount++;
        }

        ApplyGridLayout(
            visibleSlotCount,
            Mathf.Max(1, columnsPerRow)
        );
    }

    private void ClearSlots()
    {
        for (int index = 0; index < slotPool.Count; index++)
        {
            BattleBuffIconUIView slot = slotPool[index];
            if (slot == null)
            {
                continue;
            }

            slot.SetOverflowClickHandler(null);
            slot.SetEmpty();
        }
    }

    void OnDisable()
    {
        ClearSlots();
    }

    void OnDestroy()
    {
        slotPool.Clear();
        slotPoolInitialized = false;
    }
}
