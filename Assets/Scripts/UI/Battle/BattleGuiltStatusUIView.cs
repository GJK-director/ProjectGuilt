// 脚本中文说明：
// 战斗负罪感一级UI。
// 红色格子完全使用代码进行手动定位，不使用Horizontal Layout Group。
// 格子宽度由当前阶段容量决定，显示数量由当前阶段进度决定。

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleGuiltStatusUIView : MonoBehaviour
{
    [Header("UI 引用")]

    [Tooltip("红色外框内部真正允许显示格子的透明区域。")]
    [SerializeField]
    private RectTransform cellContent;

    [Tooltip("纯红色色块模板。模板应放在 CellContent 下面。")]
    [SerializeField]
    private RectTransform cellTemplate;

    [Tooltip("显示阶段 I、II、III 图片的 Image。")]
    [SerializeField]
    private Image stageImage;

    [Tooltip("显示 0/3、1/5 等数值的文本。")]
    [SerializeField]
    private TMP_Text progressText;

    [Header("阶段配置")]

    [Tooltip("按顺序绑定阶段 I、II、III 的图片。")]
    [SerializeField]
    private Sprite[] stageSprites = new Sprite[3];

    [Tooltip("阶段 I、II、III 的容量。")]
    [SerializeField]
    private int[] stageCapacities = { 3, 5, 7 };

    [Header("格子布局")]

    [Tooltip("所有红色格子的垂直位置微调。0代表CellContent垂直中心。")]
    [SerializeField]
    private float cellVerticalOffset = 0f;

    [Tooltip("第一个格子距离 CellContent 左边缘的距离。")]
    [Min(0f)]
    [SerializeField]
    private float leftInset = 3f;

    [Tooltip("相邻红色格子之间的固定间隔。")]
    [Min(0f)]
    [SerializeField]
    private float cellSpacing = 6f;

    [Tooltip("异常情况下允许的最小格子宽度。")]
    [Min(1f)]
    [SerializeField]
    private float minimumCellWidth = 1f;

    [Header("Inspector 运行测试")]

    [Tooltip("勾选后，可以在 Play Mode 中直接修改测试负罪感。")]
    [SerializeField]
    private bool useInspectorPreview = true;

    [Tooltip("在 Play Mode 中直接修改这个数值测试 UI。")]
    [Min(0)]
    [SerializeField]
    private int inspectorTotalGuilt;

    private readonly List<RectTransform> cellPool =
        new List<RectTransform>();

    // 模板的原始视觉设置。
    private Vector2 templateSizeDelta;
    private Vector3 templateLocalScale;
    private Quaternion templateLocalRotation;

    private int currentTotalGuilt;
    private int currentStageIndex;
    private int currentStageProgress;

    private int lastInspectorTotalGuilt = int.MinValue;
    private bool isInitialized;

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        CacheTemplateState();

        // 模板只用于复制，不参与正式显示。
        cellTemplate.gameObject.SetActive(false);

        // 当前阶段最大容量为7，提前准备最多7个对象。
        EnsurePoolSize(GetMaximumCapacity());

        isInitialized = true;
    }

    private void Start()
    {
        if (!isInitialized)
        {
            return;
        }

        if (useInspectorPreview)
        {
            ApplyInspectorPreview();
        }
        else
        {
            SetTotalGuilt(currentTotalGuilt);
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!useInspectorPreview || !isInitialized)
        {
            return;
        }

        int correctedValue = Mathf.Max(0, inspectorTotalGuilt);

        // Inspector数值没有变化时，不重复刷新。
        if (correctedValue == lastInspectorTotalGuilt)
        {
            return;
        }

        inspectorTotalGuilt = correctedValue;
        ApplyInspectorPreview();
#endif
    }

    private void OnRectTransformDimensionsChange()
    {
        // 分辨率或CellContent宽度发生变化时重新计算。
        if (!isInitialized || !isActiveAndEnabled)
        {
            return;
        }

        RefreshDisplay();
    }

    /// <summary>
    /// 正式系统以后调用这个方法更新总负罪感。
    /// </summary>
    public void SetTotalGuilt(int totalGuilt)
    {
        currentTotalGuilt = Mathf.Max(0, totalGuilt);

        ResolveStage(
            currentTotalGuilt,
            out currentStageIndex,
            out currentStageProgress
        );

        RefreshDisplay();
    }

    private void ApplyInspectorPreview()
    {
        inspectorTotalGuilt = Mathf.Max(0, inspectorTotalGuilt);
        lastInspectorTotalGuilt = inspectorTotalGuilt;

        SetTotalGuilt(inspectorTotalGuilt);
    }

    /// <summary>
    /// 根据总负罪感推导阶段和阶段内进度。
    /// </summary>
    private void ResolveStage(
        int totalGuilt,
        out int stageIndex,
        out int stageProgress
    )
    {
        int remainingGuilt = Mathf.Max(0, totalGuilt);
        int finalStageIndex = stageCapacities.Length - 1;

        for (int index = 0;
             index < stageCapacities.Length;
             index++)
        {
            int capacity = Mathf.Max(1, stageCapacities[index]);
            bool isFinalStage = index == finalStageIndex;

            // 非最终阶段满了之后立即进入下一阶段。
            if (!isFinalStage && remainingGuilt >= capacity)
            {
                remainingGuilt -= capacity;
                continue;
            }

            stageIndex = index;
            stageProgress = Mathf.Clamp(
                remainingGuilt,
                0,
                capacity
            );

            return;
        }

        // 防御性兜底。
        stageIndex = finalStageIndex;
        stageProgress = Mathf.Max(
            1,
            stageCapacities[finalStageIndex]
        );
    }

    private void RefreshDisplay()
    {
        if (!isInitialized)
        {
            return;
        }

        int capacity = Mathf.Max(
            1,
            stageCapacities[currentStageIndex]
        );

        UpdateStageImage();
        UpdateProgressText(capacity);
        UpdateCells(capacity, currentStageProgress);
    }

    private void UpdateStageImage()
    {
        if (currentStageIndex < 0 ||
            currentStageIndex >= stageSprites.Length)
        {
            return;
        }

        Sprite targetSprite = stageSprites[currentStageIndex];

        if (targetSprite == null)
        {
            Debug.LogWarning(
                nameof(BattleGuiltStatusUIView) +
                " 当前阶段没有绑定对应图片。",
                this
            );

            return;
        }

        stageImage.sprite = targetSprite;
        stageImage.preserveAspect = true;
    }

    private void UpdateProgressText(int capacity)
    {
        progressText.text =
            currentStageProgress +
            "/" +
            capacity;
    }

    /// <summary>
    /// 容量决定格子宽度，进度决定显示数量。
    /// </summary>
    private void UpdateCells(
        int stageCapacity,
        int visibleCellCount
    )
    {
        EnsurePoolSize(stageCapacity);

        // 注意：这里必须使用阶段容量计算宽度，
        // 不能使用当前显示数量。
        float cellWidth = CalculateCellWidth(stageCapacity);

        for (int index = 0;
             index < cellPool.Count;
             index++)
        {
            RectTransform cell = cellPool[index];

            bool belongsToStage = index < stageCapacity;
            bool shouldShow =
                belongsToStage &&
                index < visibleCellCount;

            if (belongsToStage)
            {
                ConfigureCellTransform(
                    cell,
                    index,
                    cellWidth
                );
            }

            cell.gameObject.SetActive(shouldShow);
        }
    }

    /// <summary>
    /// 格子宽度：
    /// （内部宽度 - 左侧间隔 - 全部格间距）÷ 阶段容量。
    ///
    /// 不使用当前进度参与宽度计算。
    /// </summary>
    private float CalculateCellWidth(int stageCapacity)
    {
        float contentWidth = cellContent.rect.width;

        float totalSpacing =
            cellSpacing *
            Mathf.Max(0, stageCapacity - 1);

        float widthAvailableForCells =
            contentWidth -
            leftInset -
            totalSpacing;

        float calculatedWidth =
            widthAvailableForCells / stageCapacity;

        if (calculatedWidth < minimumCellWidth)
        {
            Debug.LogWarning(
                nameof(BattleGuiltStatusUIView) +
                " CellContent宽度不足，请检查容器宽度和间距。",
                this
            );
        }

        return Mathf.Max(
            minimumCellWidth,
            calculatedWidth
        );
    }

    /// <summary>
    /// 手动设置格子位置和宽度。
    /// 不使用任何Layout Group。
    /// </summary>
    private void ConfigureCellTransform(
        RectTransform cell,
        int index,
        float cellWidth
    )
    {
        // 固定在CellContent左侧、垂直中心。
        cell.anchorMin = new Vector2(0f, 0.5f);
        cell.anchorMax = new Vector2(0f, 0.5f);

        // Pivot放在左侧，方便直接计算左边缘位置。
        cell.pivot = new Vector2(0f, 0.5f);

        float positionX =
            leftInset +
            index * (cellWidth + cellSpacing);

        cell.anchoredPosition = new Vector2(
        positionX,
        cellVerticalOffset
    );

        // 只改变宽度。
        // 高度仍使用模板原始高度。
        cell.sizeDelta = new Vector2(
            cellWidth,
            templateSizeDelta.y
        );

        // 保留你设置的Scale Y。
        cell.localScale = new Vector3(
            1f,
            templateLocalScale.y,
            templateLocalScale.z
        );

        cell.localRotation = templateLocalRotation;
    }

    private void CacheTemplateState()
    {
        templateSizeDelta = cellTemplate.sizeDelta;
        templateLocalScale = cellTemplate.localScale;
        templateLocalRotation = cellTemplate.localRotation;

    }

    private void EnsurePoolSize(int requiredCount)
    {
        requiredCount = Mathf.Max(0, requiredCount);

        while (cellPool.Count < requiredCount)
        {
            RectTransform newCell = Instantiate(
                cellTemplate,
                cellContent,
                false
            );

            newCell.name =
                "GuiltCell_" +
                (cellPool.Count + 1).ToString("00");

            // 彻底禁止横向拉伸。
            newCell.anchorMin = new Vector2(0f, 0.5f);
            newCell.anchorMax = new Vector2(0f, 0.5f);
            newCell.pivot = new Vector2(0f, 0.5f);

            Image image = newCell.GetComponent<Image>();

            if (image != null)
            {
                image.preserveAspect = false;
                image.raycastTarget = false;
            }

            newCell.gameObject.SetActive(false);
            cellPool.Add(newCell);
        }
    }

    private int GetMaximumCapacity()
    {
        int maximumCapacity = 1;

        for (int index = 0;
             index < stageCapacities.Length;
             index++)
        {
            maximumCapacity = Mathf.Max(
                maximumCapacity,
                stageCapacities[index]
            );
        }

        return maximumCapacity;
    }

    private bool ValidateReferences()
    {
        if (cellContent == null)
        {
            Debug.LogError(
                nameof(BattleGuiltStatusUIView) +
                " 缺少 Cell Content。",
                this
            );

            return false;
        }

        if (cellTemplate == null)
        {
            Debug.LogError(
                nameof(BattleGuiltStatusUIView) +
                " 缺少 Cell Template。",
                this
            );

            return false;
        }

        if (stageImage == null)
        {
            Debug.LogError(
                nameof(BattleGuiltStatusUIView) +
                " 缺少 Stage Image。",
                this
            );

            return false;
        }

        if (progressText == null)
        {
            Debug.LogError(
                nameof(BattleGuiltStatusUIView) +
                " 缺少 Progress Text。",
                this
            );

            return false;
        }

        if (stageCapacities == null ||
            stageCapacities.Length == 0)
        {
            Debug.LogError(
                nameof(BattleGuiltStatusUIView) +
                " 阶段容量不能为空。",
                this
            );

            return false;
        }

        if (stageSprites == null ||
            stageSprites.Length < stageCapacities.Length)
        {
            Debug.LogError(
                nameof(BattleGuiltStatusUIView) +
                " 阶段图片数量不足。",
                this
            );

            return false;
        }

        return true;
    }
}
