using System;
using System.Collections.Generic;

namespace ProjectGuilt.Story
{
    // 剧情核心状态机：编排节点执行、逐字显示、玩家输入和自动播放。
    // 该类不继承 MonoBehaviour，也不直接引用任何宿主战斗或 UI 具体类型。
    internal sealed class StoryFlowController
    {
        // 防止 Jump/Condition 等立即节点形成无等待死循环并卡死主线程。
        private const int MaxImmediateNodeSteps = 256;

        private readonly StoryRuntimeState state;
        private readonly StoryHistoryService history;
        private readonly StoryTextPresenter textPresenter;
        private readonly StoryNodeExecutor nodeExecutor;
        private readonly StoryNodeExecutionContext executionContext;
        private readonly IStoryView view;
        private readonly float autoBaseDelay;
        private readonly float skipDelay;

        private readonly Dictionary<string, StoryNodeData> nodeMap =
            new Dictionary<string, StoryNodeData>(StringComparer.Ordinal);

        private StoryDefinitionData currentDefinition;
        private float playbackTimer;
        private float inputGuardRemaining;

        public event Action<string> StoryStarted;
        public event Action<string> StoryEnded;
        public event Action<string> StoryError;

        // 所有可替换依赖都在构造时注入，Controller 只面向抽象接口工作。
        public StoryFlowController(
            StoryRuntimeState state,
            StoryHistoryService history,
            StoryTextPresenter textPresenter,
            StoryNodeExecutor nodeExecutor,
            IStoryView view,
            float autoBaseDelay,
            float skipDelay
        )
        {
            this.state = state;
            this.history = history;
            this.textPresenter = textPresenter;
            this.nodeExecutor = nodeExecutor;
            this.view = view;
            this.autoBaseDelay = Math.Max(0f, autoBaseDelay);
            this.skipDelay = Math.Max(0.01f, skipDelay);
            executionContext = new StoryNodeExecutionContext(
                state,
                history,
                textPresenter,
                view
            );
        }

        // 只向 Facade 暴露当前运行状态，用于诊断，不作为宿主业务 API。
        public StoryRuntimeState State
        {
            get { return state; }
        }

        // 把扩展节点处理器注册到大小写不敏感的执行器映射中。
        public void RegisterNodeHandler(IStoryNodeHandler handler)
        {
            nodeExecutor.RegisterHandler(handler);
        }

        // 清空旧进度并从 startNodeId 开始执行一份新的剧情定义。
        public bool StartStory(StoryDefinitionData definition)
        {
            string errorMessage;

            if (!TrySetDefinition(definition, out errorMessage))
            {
                Fail(errorMessage);
                return false;
            }

            history.Clear();
            textPresenter.Clear();
            state.Begin(definition.storyId, definition.startNodeId);
            playbackTimer = 0f;
            inputGuardRemaining = 0f;
            ApplyViewState();

            if (StoryStarted != null)
            {
                StoryStarted(definition.storyId);
            }

            return ExecuteFrom(definition.startNodeId);
        }

        // 每帧推进逐字、自动播放或 Wait 计时；Overlay 和隐藏 UI 时暂停。
        public void Tick(float unscaledDeltaTime)
        {
            float deltaTime = Math.Max(0f, unscaledDeltaTime);
            inputGuardRemaining = Math.Max(0f, inputGuardRemaining - deltaTime);

            if (state.MainState == StoryMainState.Idle ||
                state.MainState == StoryMainState.Ended ||
                state.MainState == StoryMainState.Error ||
                state.IsOverlayOpen ||
                !state.IsStoryUiVisible)
            {
                return;
            }

            if (state.MainState == StoryMainState.Typing)
            {
                bool changed;

                if (state.PlaybackMode == StoryPlaybackMode.Skip && textPresenter.Skippable)
                {
                    changed = textPresenter.CompleteImmediately();
                }
                else
                {
                    changed = textPresenter.Tick(deltaTime);
                }

                if (changed)
                {
                    RenderDialogue();
                }

                if (!textPresenter.IsTyping)
                {
                    EnterWaitingAdvance();
                }

                return;
            }

            if (state.MainState == StoryMainState.WaitingAdvance)
            {
                if (state.PlaybackMode == StoryPlaybackMode.Auto ||
                    state.PlaybackMode == StoryPlaybackMode.Skip)
                {
                    playbackTimer -= deltaTime;

                    if (playbackTimer <= 0f)
                    {
                        AdvanceToPendingNode();
                    }
                }

                return;
            }

            if (state.MainState == StoryMainState.WaitingTime)
            {
                if (state.PlaybackMode == StoryPlaybackMode.Skip && state.WaitSkippable)
                {
                    state.CompleteWait();
                }
                else
                {
                    state.ConsumeWait(deltaTime);
                }

                if (state.WaitRemaining <= 0f)
                {
                    AdvanceToPendingNode();
                }
            }
        }

        // 普通点击入口：优先显示完整当前句，再推进一个节点，绝不自动选择选项。
        public bool RequestAdvance()
        {
            if (inputGuardRemaining > 0f || state.IsOverlayOpen)
            {
                return false;
            }

            if (!state.IsStoryUiVisible)
            {
                state.SetStoryUiVisible(true);
                view.SetStoryUiVisible(true);
                inputGuardRemaining = 0.05f;
                return true;
            }

            if (state.MainState == StoryMainState.Typing)
            {
                textPresenter.CompleteImmediately();
                RenderDialogue();
                EnterWaitingAdvance();
                return true;
            }

            if (state.MainState == StoryMainState.WaitingAdvance)
            {
                return AdvanceToPendingNode();
            }

            if (state.MainState == StoryMainState.WaitingTime && state.WaitSkippable)
            {
                state.CompleteWait();
                return AdvanceToPendingNode();
            }

            return false;
        }

        // 提交当前 Choice 中仍满足条件的选项，并原子写入该选项的变量操作。
        public bool SubmitChoice(string optionId)
        {
            if (state.MainState != StoryMainState.ShowingChoice ||
                state.IsOverlayOpen ||
                string.IsNullOrWhiteSpace(optionId))
            {
                return false;
            }

            StoryNodeData node;

            if (!nodeMap.TryGetValue(state.CurrentNodeId, out node) || node.choices == null)
            {
                Fail("当前 Choice 节点数据不存在");
                return false;
            }

            StoryChoiceOptionData selectedOption = null;

            foreach (StoryChoiceOptionData option in node.choices)
            {
                if (option != null && option.optionId == optionId)
                {
                    selectedOption = option;
                    break;
                }
            }

            if (selectedOption == null ||
                !state.Variables.Evaluate(
                    selectedOption.conditions,
                    selectedOption.conditionMode
                ))
            {
                return false;
            }

            string errorMessage;

            if (!state.Variables.TryApplyOperations(
                selectedOption.variableOperations,
                out errorMessage
            ))
            {
                Fail(node.nodeId + " 选项变量写入失败：" + errorMessage);
                return false;
            }

            history.AddChoice(node.nodeId, selectedOption.text);
            state.SetPlaybackMode(StoryPlaybackMode.Manual);
            view.SetPlaybackMode(StoryPlaybackMode.Manual);
            view.HideChoices();
            return ExecuteFrom(selectedOption.targetNodeId);
        }

        // 开启 Auto 后，文本完成会按计算出的延迟自动进入下一节点。
        public void SetAuto(bool enabled)
        {
            if (!CanChangePlaybackMode())
            {
                return;
            }

            StoryPlaybackMode mode = enabled
                ? StoryPlaybackMode.Auto
                : StoryPlaybackMode.Manual;
            state.SetPlaybackMode(mode);
            view.SetPlaybackMode(mode);

            if (state.MainState == StoryMainState.WaitingAdvance)
            {
                playbackTimer = GetAdvanceDelay();
            }
        }

        // 开启 Skip 后，可跳过文本立即补全，并使用较短延迟快速逐行推进。
        public void SetSkip(bool enabled)
        {
            if (!CanChangePlaybackMode())
            {
                return;
            }

            StoryPlaybackMode mode = enabled
                ? StoryPlaybackMode.Skip
                : StoryPlaybackMode.Manual;
            state.SetPlaybackMode(mode);
            view.SetPlaybackMode(mode);

            if (state.MainState == StoryMainState.WaitingAdvance)
            {
                playbackTimer = GetAdvanceDelay();
            }
        }

        // 跳转到定义的 skipNodeId 执行必要收尾；没有收尾节点时直接结束。
        public bool SkipToEnd()
        {
            if (state.MainState == StoryMainState.Idle ||
                state.MainState == StoryMainState.Ended ||
                state.MainState == StoryMainState.Error ||
                state.IsOverlayOpen)
            {
                return false;
            }

            state.SetPlaybackMode(StoryPlaybackMode.Manual);
            view.SetPlaybackMode(StoryPlaybackMode.Manual);
            view.HideChoices();

            if (currentDefinition != null &&
                !string.IsNullOrWhiteSpace(currentDefinition.skipNodeId))
            {
                return ExecuteFrom(currentDefinition.skipNodeId);
            }

            FinishStory();
            return true;
        }

        // Overlay 打开时保留全部运行状态并暂停 Tick。
        public void OpenOverlay()
        {
            if (state.MainState == StoryMainState.Idle || state.MainState == StoryMainState.Ended)
            {
                return;
            }

            state.SetOverlayOpen(true);
            view.SetOverlayOpen(true);
        }

        // 关闭 Overlay 后增加短输入保护，避免关闭按钮同一帧继续推进剧情。
        public void CloseOverlay()
        {
            state.SetOverlayOpen(false);
            view.SetOverlayOpen(false);
            inputGuardRemaining = 0.05f;
        }

        // 只隐藏或恢复剧情 UI；恢复显示的同一次点击不会推进节点。
        public void ToggleStoryUi()
        {
            bool visible = !state.IsStoryUiVisible;
            state.SetStoryUiVisible(visible);
            view.SetStoryUiVisible(visible);

            if (visible)
            {
                inputGuardRemaining = 0.05f;
            }
        }

        // 主动放弃当前剧情并回到 Idle，同时清空历史、文本和节点映射。
        public void CloseStory()
        {
            state.ResetToIdle();
            history.Clear();
            textPresenter.Clear();
            currentDefinition = null;
            nodeMap.Clear();
            view.HideChoices();
            view.SetContinueIndicator(false);
            view.SetOverlayOpen(false);
            view.SetStoryVisible(false);
        }

        // 返回历史记录副本，避免外部历史面板修改内部集合。
        public IReadOnlyList<StoryHistoryEntryData> GetHistorySnapshot()
        {
            return history.CreateSnapshot();
        }

        // 建立当前剧情的 nodeId 索引，后续所有跳转都通过该映射快速查找。
        private bool TrySetDefinition(
            StoryDefinitionData definition,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (definition == null || definition.nodes == null)
            {
                errorMessage = "剧情定义为空";
                return false;
            }

            nodeMap.Clear();

            foreach (StoryNodeData node in definition.nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
                {
                    errorMessage = "剧情定义包含空节点";
                    return false;
                }

                if (nodeMap.ContainsKey(node.nodeId))
                {
                    errorMessage = "剧情定义包含重复 nodeId：" + node.nodeId;
                    return false;
                }

                nodeMap.Add(node.nodeId, node);
            }

            currentDefinition = definition;
            return true;
        }

        // 从指定节点开始同步执行，直到遇到对话、选项、等待、结束或错误。
        private bool ExecuteFrom(string firstNodeId)
        {
            string nextNodeId = firstNodeId;

            for (int step = 0; step < MaxImmediateNodeSteps; step++)
            {
                if (string.IsNullOrWhiteSpace(nextNodeId))
                {
                    Fail("剧情节点缺少后续跳转，请显式连接 End 节点");
                    return false;
                }

                StoryNodeData node;

                if (!nodeMap.TryGetValue(nextNodeId, out node))
                {
                    Fail("找不到剧情节点：" + nextNodeId);
                    return false;
                }

                state.SetCurrentNode(node.nodeId);
                state.SetMainState(StoryMainState.ExecutingNode);
                StoryNodeExecutionResult result = nodeExecutor.Execute(executionContext, node);

                if (result == null)
                {
                    Fail(node.nodeId + " 的节点处理器返回空结果");
                    return false;
                }

                switch (result.kind)
                {
                    case StoryExecutionKind.Continue:
                        nextNodeId = result.nextNodeId;
                        continue;

                    case StoryExecutionKind.WaitForDialogue:
                        state.SetPendingNextNode(result.nextNodeId);

                        if (textPresenter.IsTyping)
                        {
                            state.SetMainState(StoryMainState.Typing);
                        }
                        else
                        {
                            EnterWaitingAdvance();
                        }

                        return true;

                    case StoryExecutionKind.WaitForChoice:
                        state.SetPendingNextNode(string.Empty);
                        state.SetMainState(StoryMainState.ShowingChoice);
                        state.SetPlaybackMode(StoryPlaybackMode.Manual);
                        view.SetPlaybackMode(StoryPlaybackMode.Manual);
                        return true;

                    case StoryExecutionKind.WaitForTime:
                        state.SetPendingNextNode(result.nextNodeId);
                        state.SetWait(result.waitSeconds, result.skippable);

                        if (state.WaitRemaining <= 0f)
                        {
                            nextNodeId = result.nextNodeId;
                            continue;
                        }

                        state.SetMainState(StoryMainState.WaitingTime);
                        view.SetContinueIndicator(false);
                        return true;

                    case StoryExecutionKind.End:
                        FinishStory();
                        return true;

                    case StoryExecutionKind.Error:
                        Fail(result.errorMessage);
                        return false;

                    default:
                        Fail(node.nodeId + " 返回未知执行结果");
                        return false;
                }
            }

            Fail("连续立即执行节点超过上限，可能存在无等待的循环跳转");
            return false;
        }

        // 继续执行当前等待状态预先保存的目标节点。
        private bool AdvanceToPendingNode()
        {
            return ExecuteFrom(state.PendingNextNodeId);
        }

        // 当前句显示完成后进入等待推进，并根据播放模式计算下一次推进时间。
        private void EnterWaitingAdvance()
        {
            state.SetMainState(StoryMainState.WaitingAdvance);
            playbackTimer = GetAdvanceDelay();
            RenderDialogue();
            view.SetContinueIndicator(true);
        }

        // Skip 使用固定短延迟；Auto 可使用节点覆盖值，否则按文本长度追加等待。
        private float GetAdvanceDelay()
        {
            if (state.PlaybackMode == StoryPlaybackMode.Skip)
            {
                return skipDelay;
            }

            if (textPresenter.AutoDelayOverride >= 0f)
            {
                return textPresenter.AutoDelayOverride;
            }

            float lengthDelay = Math.Min(2f, textPresenter.FullText.Length * 0.02f);
            return autoBaseDelay + lengthDelay;
        }

        // 把逐字器的纯数据状态映射为一次 View 刷新命令。
        private void RenderDialogue()
        {
            view.ShowDialogue(
                textPresenter.SpeakerName,
                textPresenter.FullText,
                textPresenter.VisibleCharacterCount,
                !textPresenter.IsTyping
            );
        }

        // 启动剧情后一次性同步根节点、UI、背景和立绘表现。
        private void ApplyViewState()
        {
            view.SetStoryVisible(true);
            view.SetStoryUiVisible(state.IsStoryUiVisible);
            view.SetOverlayOpen(state.IsOverlayOpen);
            view.SetPlaybackMode(state.PlaybackMode);
            view.SetContinueIndicator(false);
            view.HideChoices();

            if (!string.IsNullOrWhiteSpace(state.CurrentBackgroundId))
            {
                view.SetBackground(state.CurrentBackgroundId, 0f);
            }

            view.ApplyPortraits(
                state.CreatePortraitSnapshot(),
                state.ActiveSpeakerId
            );
        }

        // Idle、Ended、Error 和 Choice 状态禁止切换自动/快速播放。
        private bool CanChangePlaybackMode()
        {
            return state.MainState != StoryMainState.Idle &&
                state.MainState != StoryMainState.Ended &&
                state.MainState != StoryMainState.Error &&
                state.MainState != StoryMainState.ShowingChoice;
        }

        // 正常结束剧情：恢复手动模式、清理交互提示并发送结束事件。
        private void FinishStory()
        {
            string storyId = state.StoryId;
            state.SetMainState(StoryMainState.Ended);
            state.SetPlaybackMode(StoryPlaybackMode.Manual);
            view.SetPlaybackMode(StoryPlaybackMode.Manual);
            view.SetContinueIndicator(false);
            view.HideChoices();
            view.NotifyStoryEnded(storyId);

            if (StoryEnded != null)
            {
                StoryEnded(storyId);
            }
        }

        // 把任何节点或数据错误收敛为 Error 状态，并停止继续推进。
        private void Fail(string errorMessage)
        {
            string message = string.IsNullOrWhiteSpace(errorMessage)
                ? "未知剧情系统错误"
                : errorMessage;
            state.SetMainState(StoryMainState.Error);
            view.SetContinueIndicator(false);
            view.HideChoices();

            if (StoryError != null)
            {
                StoryError(message);
            }
        }
    }
}
