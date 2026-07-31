using System.Collections;
using UnityEngine;

public sealed class BattleBuffGroupDebugPreview : MonoBehaviour
{
    [Header("预览目标")]
    [SerializeField] private BattleBuffGroupUIView targetBuffGroup;

    [Header("运行时预览")]
    [Tooltip("开启后会用临时角色覆盖该BuffGroup当前显示内容，仅用于Play Mode测试。正式运行前应关闭。")]
    [SerializeField] private bool enableRuntimePreview;

    [Min(0)]
    [SerializeField] private int previewBuffCount = 9;

    [Min(1)]
    [SerializeField] private int defaultStack = 1;

    [SerializeField] private bool useIncreasingStacks = true;
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool refreshWhenInspectorChanges = true;

    private CharacterData previewCharacter;
    private Coroutine initialApplyCoroutine;
    private Coroutine refreshCoroutine;
    private bool refreshRequested;
    private int applyInvocationCount;

    public CharacterData PreviewCharacter => previewCharacter;

    public int PreviewBuffCount =>
        previewCharacter != null && previewCharacter.buffs != null
            ? previewCharacter.buffs.Count
            : 0;

    internal int ApplyInvocationCount => applyInvocationCount;
    internal bool HasPendingInitialApply =>
        initialApplyCoroutine != null;
    internal bool HasPendingRefresh =>
        refreshRequested || refreshCoroutine != null;

    void Awake()
    {
        ResolveTargetBuffGroup();
    }

    void Start()
    {
        if (Application.isPlaying &&
            enableRuntimePreview &&
            applyOnStart)
        {
            ScheduleInitialApply();
        }
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!enableRuntimePreview)
        {
            StopScheduledPreviewCoroutines();
            return;
        }

        if (refreshWhenInspectorChanges)
        {
            RequestRefresh();
        }
    }

    [ContextMenu("应用Buff数量预览")]
    public void ApplyPreview()
    {
        StopScheduledPreviewCoroutines();
        ApplyPreviewImmediately();
    }

    private void ApplyPreviewImmediately()
    {
        if (!Application.isPlaying || !enableRuntimePreview)
        {
            return;
        }

        ResolveTargetBuffGroup();
        if (targetBuffGroup == null)
        {
            Debug.LogWarning(
                "BattleBuffGroupDebugPreview 未绑定目标 BattleBuffGroupUIView。",
                this
            );
            return;
        }

        applyInvocationCount++;
        previewCharacter = new CharacterData(
            "BuffUI_Preview",
            100,
            1,
            1
        );

        int validBuffCount = Mathf.Max(0, previewBuffCount);
        int validDefaultStack = Mathf.Max(1, defaultStack);
        for (int index = 0; index < validBuffCount; index++)
        {
            string number = (index + 1).ToString("00");
            int stack = useIncreasingStacks
                ? index + 1
                : validDefaultStack;
            previewCharacter.AddBuff(
                "DebugPreviewBuff_" + number,
                "预览Buff " + number,
                "UpBuff",
                stack,
                2,
                "TurnEnd",
                "DurationDown"
            );
        }

        targetBuffGroup.SetCharacter(previewCharacter);
    }

    [ContextMenu("清除Buff数量预览")]
    public void ClearPreview()
    {
        previewCharacter = null;

        if (!Application.isPlaying)
        {
            return;
        }

        StopScheduledPreviewCoroutines();
        ResolveTargetBuffGroup();

        if (targetBuffGroup != null)
        {
            targetBuffGroup.Clear();
        }
    }

    void OnDisable()
    {
        StopScheduledPreviewCoroutines();
    }

    void OnDestroy()
    {
        StopScheduledPreviewCoroutines();
        previewCharacter = null;
    }

    private void ScheduleInitialApply()
    {
        if (!Application.isPlaying ||
            !isActiveAndEnabled ||
            initialApplyCoroutine != null)
        {
            return;
        }

        initialApplyCoroutine =
            StartCoroutine(ApplyInitialPreviewAtFrameEnd());
    }

    private IEnumerator ApplyInitialPreviewAtFrameEnd()
    {
        yield return new WaitForEndOfFrame();
        CompleteInitialApply();
    }

    private void CompleteInitialApply()
    {
        initialApplyCoroutine = null;

        bool shouldApply =
            Application.isPlaying &&
            enableRuntimePreview &&
            applyOnStart;
        refreshRequested = false;

        if (!shouldApply)
        {
            return;
        }

        ResolveTargetBuffGroup();
        if (targetBuffGroup != null)
        {
            ApplyPreviewImmediately();
        }
    }

    private void RequestRefresh()
    {
        if (!Application.isPlaying ||
            !enableRuntimePreview ||
            !refreshWhenInspectorChanges)
        {
            return;
        }

        refreshRequested = true;
        if (initialApplyCoroutine != null ||
            refreshCoroutine != null ||
            !isActiveAndEnabled)
        {
            return;
        }

        refreshCoroutine =
            StartCoroutine(ApplyRequestedRefreshAtFrameEnd());
    }

    private IEnumerator ApplyRequestedRefreshAtFrameEnd()
    {
        yield return new WaitForEndOfFrame();
        CompleteRequestedRefresh();
    }

    private void CompleteRequestedRefresh()
    {
        refreshCoroutine = null;
        if (!refreshRequested)
        {
            return;
        }

        refreshRequested = false;
        if (Application.isPlaying &&
            enableRuntimePreview &&
            refreshWhenInspectorChanges)
        {
            ApplyPreviewImmediately();
        }
    }

    private void StopScheduledPreviewCoroutines()
    {
        if (initialApplyCoroutine != null)
        {
            StopCoroutine(initialApplyCoroutine);
            initialApplyCoroutine = null;
        }

        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }

        refreshRequested = false;
    }

    internal void ScheduleInitialApplyForTesting()
    {
        ScheduleInitialApply();
    }

    internal void CompleteInitialApplyForTesting()
    {
        if (initialApplyCoroutine != null)
        {
            StopCoroutine(initialApplyCoroutine);
        }

        CompleteInitialApply();
    }

    internal void RequestRefreshForTesting()
    {
        RequestRefresh();
    }

    internal void CompleteRefreshForTesting()
    {
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
        }

        CompleteRequestedRefresh();
    }

    internal void SetRuntimePreviewEnabledForTesting(bool enabled)
    {
        enableRuntimePreview = enabled;
        if (!enableRuntimePreview)
        {
            StopScheduledPreviewCoroutines();
        }
    }

    private void ResolveTargetBuffGroup()
    {
        if (targetBuffGroup == null)
        {
            targetBuffGroup = GetComponent<BattleBuffGroupUIView>();
        }
    }
}
