using System;
using System.Collections.Generic;

namespace ProjectGuilt.Story
{
    // 汇总节点处理器所需的运行时依赖，避免处理器直接依赖流程控制器。
    public sealed class StoryNodeExecutionContext
    {
        public StoryRuntimeState State { get; private set; }
        public StoryVariableStore Variables { get; private set; }
        public StoryHistoryService History { get; private set; }
        public StoryTextPresenter TextPresenter { get; private set; }
        public IStoryView View { get; private set; }

        // 变量仓库由运行时状态持有，保证节点处理器操作的是同一份会话数据。
        public StoryNodeExecutionContext(
            StoryRuntimeState state,
            StoryHistoryService history,
            StoryTextPresenter textPresenter,
            IStoryView view
        )
        {
            State = state;
            Variables = state.Variables;
            History = history;
            TextPresenter = textPresenter;
            View = view;
        }
    }

    // 节点处理器通过统一结果描述“继续、等待、结束或失败”，由控制器决定后续状态迁移。
    public sealed class StoryNodeExecutionResult
    {
        public StoryExecutionKind kind;
        public string nextNodeId;
        public float waitSeconds;
        public bool skippable;
        public string errorMessage;

        // 当前节点无需等待，立即转到下一个节点。
        public static StoryNodeExecutionResult Continue(string nextNodeId)
        {
            return new StoryNodeExecutionResult
            {
                kind = StoryExecutionKind.Continue,
                nextNodeId = nextNodeId
            };
        }

        // 对白已开始展示，等待打字完成或玩家继续。
        public static StoryNodeExecutionResult WaitForDialogue(string nextNodeId)
        {
            return new StoryNodeExecutionResult
            {
                kind = StoryExecutionKind.WaitForDialogue,
                nextNodeId = nextNodeId
            };
        }

        // 选项已展示，等待玩家提交一个可用选项。
        public static StoryNodeExecutionResult WaitForChoice()
        {
            return new StoryNodeExecutionResult
            {
                kind = StoryExecutionKind.WaitForChoice
            };
        }

        // 定时节点进入等待状态；是否允许跳过由剧情数据决定。
        public static StoryNodeExecutionResult WaitForTime(
            string nextNodeId,
            float seconds,
            bool skippable
        )
        {
            return new StoryNodeExecutionResult
            {
                kind = StoryExecutionKind.WaitForTime,
                nextNodeId = nextNodeId,
                waitSeconds = Math.Max(0f, seconds),
                skippable = skippable
            };
        }

        // 显式结束当前剧情会话。
        public static StoryNodeExecutionResult End()
        {
            return new StoryNodeExecutionResult
            {
                kind = StoryExecutionKind.End
            };
        }

        // 将数据错误或自定义处理器异常转换成可控的剧情错误结果。
        public static StoryNodeExecutionResult Error(string message)
        {
            return new StoryNodeExecutionResult
            {
                kind = StoryExecutionKind.Error,
                errorMessage = message
            };
        }
    }

    // 自定义节点扩展点：外部模块只需声明节点类型并实现执行逻辑。
    public interface IStoryNodeHandler
    {
        string NodeType { get; }
        StoryNodeExecutionResult Execute(
            StoryNodeExecutionContext context,
            StoryNodeData node
        );
    }

    // 新节点通过 RegisterHandler 扩展，不需要修改 StoryFlowController。
    internal sealed class StoryNodeExecutor
    {
        private readonly Dictionary<string, IStoryNodeHandler> handlers =
            new Dictionary<string, IStoryNodeHandler>(StringComparer.OrdinalIgnoreCase);

        // 创建执行器时注册模块内置的全部节点类型。
        public StoryNodeExecutor()
        {
            RegisterDefaultHandlers();
        }

        // 同名节点类型以后注册者为准，允许外部替换或扩展具体实现。
        public void RegisterHandler(IStoryNodeHandler handler)
        {
            if (handler == null || string.IsNullOrWhiteSpace(handler.NodeType))
            {
                throw new ArgumentException("剧情节点处理器或 NodeType 为空");
            }

            handlers[handler.NodeType.Trim()] = handler;
        }

        // 查找并安全执行处理器；异常不会越过模块边界，而会转为 StoryError。
        public StoryNodeExecutionResult Execute(
            StoryNodeExecutionContext context,
            StoryNodeData node
        )
        {
            if (context == null || node == null)
            {
                return StoryNodeExecutionResult.Error("节点执行上下文或节点为空");
            }

            IStoryNodeHandler handler;

            if (!handlers.TryGetValue(node.nodeType ?? string.Empty, out handler))
            {
                return StoryNodeExecutionResult.Error(
                    node.nodeId + " 使用了未注册的 nodeType：" + node.nodeType
                );
            }

            try
            {
                StoryNodeExecutionResult result = handler.Execute(context, node);

                return result ?? StoryNodeExecutionResult.Error(
                    node.nodeId + " 的节点处理器返回空结果"
                );
            }
            catch (Exception exception)
            {
                return StoryNodeExecutionResult.Error(
                    node.nodeId + " 的节点处理器执行异常（" +
                    exception.GetType().Name + "）：" + exception.Message
                );
            }
        }

        // 内置处理器保持在执行层，流程控制器无需了解每类节点的数据细节。
        private void RegisterDefaultHandlers()
        {
            RegisterHandler(new DialogueNodeHandler());
            RegisterHandler(new ChoiceNodeHandler());
            RegisterHandler(new SetVariableNodeHandler());
            RegisterHandler(new ConditionNodeHandler());
            RegisterHandler(new JumpNodeHandler());
            RegisterHandler(new ChangeBackgroundNodeHandler());
            RegisterHandler(new ShowPortraitNodeHandler());
            RegisterHandler(new HidePortraitNodeHandler());
            RegisterHandler(new ChangeExpressionNodeHandler());
            RegisterHandler(new WaitNodeHandler());
            RegisterHandler(new EndNodeHandler());
        }
    }

    // 初始化逐字文本、激活说话者，并把完整对白写入历史记录。
    internal sealed class DialogueNodeHandler : IStoryNodeHandler
    {
        public string NodeType { get { return StoryNodeTypes.Dialogue; } }

        public StoryNodeExecutionResult Execute(
            StoryNodeExecutionContext context,
            StoryNodeData node
        )
        {
            if (node.dialogue == null)
            {
                return StoryNodeExecutionResult.Error(node.nodeId + " 缺少 dialogue 数据");
            }

            context.State.SetActiveSpeaker(node.dialogue.speakerId);
            context.View.HideChoices();
            context.View.ApplyPortraits(
                context.State.CreatePortraitSnapshot(),
                context.State.ActiveSpeakerId
            );
            context.TextPresenter.Begin(node.nodeId, node.dialogue);
            context.History.AddDialogue(
                node.nodeId,
                node.dialogue.speakerId,
                node.dialogue.speakerName,
                node.dialogue.text
            );
            context.View.SetContinueIndicator(false);
            context.View.ShowDialogue(
                context.TextPresenter.SpeakerName,
                context.TextPresenter.FullText,
                context.TextPresenter.VisibleCharacterCount,
                !context.TextPresenter.IsTyping
            );
            return StoryNodeExecutionResult.WaitForDialogue(node.nextNodeId);
        }
    }

    // 根据变量条件筛选或禁用选项，然后进入等待玩家选择的状态。
    internal sealed class ChoiceNodeHandler : IStoryNodeHandler
    {
        public string NodeType { get { return StoryNodeTypes.Choice; } }

        public StoryNodeExecutionResult Execute(
            StoryNodeExecutionContext context,
            StoryNodeData node
        )
        {
            List<StoryChoiceViewData> choiceViews = new List<StoryChoiceViewData>();

            if (node.choices != null)
            {
                foreach (StoryChoiceOptionData option in node.choices)
                {
                    if (option == null)
                    {
                        continue;
                    }

                    bool available = context.Variables.Evaluate(
                        option.conditions,
                        option.conditionMode
                    );

                    if (!available && option.unavailableMode == StoryChoiceUnavailableMode.Hide)
                    {
                        continue;
                    }

                    choiceViews.Add(new StoryChoiceViewData
                    {
                        optionId = option.optionId,
                        text = option.text,
                        interactable = available
                    });
                }
            }

            if (choiceViews.Count == 0)
            {
                return StoryNodeExecutionResult.Error(node.nodeId + " 没有任何可显示选项");
            }

            context.View.SetContinueIndicator(false);
            context.View.ShowChoices(choiceViews);
            return StoryNodeExecutionResult.WaitForChoice();
        }
    }

    // 原子执行一个节点内的全部变量操作，任一操作失败则不提交任何修改。
    internal sealed class SetVariableNodeHandler : IStoryNodeHandler
    {
        public string NodeType { get { return StoryNodeTypes.SetVariable; } }

        public StoryNodeExecutionResult Execute(
            StoryNodeExecutionContext context,
            StoryNodeData node
        )
        {
            string errorMessage;

            if (!context.Variables.TryApplyOperations(node.variableOperations, out errorMessage))
            {
                return StoryNodeExecutionResult.Error(node.nodeId + "：" + errorMessage);
            }

            return StoryNodeExecutionResult.Continue(node.nextNodeId);
        }
    }

    // 按 All/Any 规则计算条件，并选择 true 或 false 分支。
    internal sealed class ConditionNodeHandler : IStoryNodeHandler
    {
        public string NodeType { get { return StoryNodeTypes.Condition; } }

        public StoryNodeExecutionResult Execute(
            StoryNodeExecutionContext context,
            StoryNodeData node
        )
        {
            bool result = context.Variables.Evaluate(node.conditions, node.conditionMode);
            return StoryNodeExecutionResult.Continue(
                result ? node.trueNodeId : node.falseNodeId
            );
        }
    }

    // 无条件跳转到 JSON 中指定的目标节点。
    internal sealed class JumpNodeHandler : IStoryNodeHandler
    {
        public string NodeType { get { return StoryNodeTypes.Jump; } }

        public StoryNodeExecutionResult Execute(
            StoryNodeExecutionContext context,
            StoryNodeData node
        )
        {
            return StoryNodeExecutionResult.Continue(node.jumpNodeId);
        }
    }

    // 同步背景状态与视图，可由 UI 根据 fadeSeconds 实现淡入淡出。
    internal sealed class ChangeBackgroundNodeHandler : IStoryNodeHandler
    {
        public string NodeType { get { return StoryNodeTypes.ChangeBackground; } }

        public StoryNodeExecutionResult Execute(
            StoryNodeExecutionContext context,
            StoryNodeData node
        )
        {
            if (node.background == null || string.IsNullOrWhiteSpace(node.background.backgroundId))
            {
                return StoryNodeExecutionResult.Error(node.nodeId + " 缺少 background 数据");
            }

            context.State.SetBackground(node.background.backgroundId);
            context.View.SetBackground(
                node.background.backgroundId,
                Math.Max(0f, node.background.fadeSeconds)
            );
            return StoryNodeExecutionResult.Continue(node.nextNodeId);
        }
    }

    // 新增或更新角色立绘状态，并向视图提交完整立绘快照。
    internal sealed class ShowPortraitNodeHandler : IStoryNodeHandler
    {
        public string NodeType { get { return StoryNodeTypes.ShowPortrait; } }

        public StoryNodeExecutionResult Execute(
            StoryNodeExecutionContext context,
            StoryNodeData node
        )
        {
            if (node.portrait == null || string.IsNullOrWhiteSpace(node.portrait.characterId))
            {
                return StoryNodeExecutionResult.Error(node.nodeId + " 缺少 portrait.characterId");
            }

            context.State.ShowPortrait(node.portrait);
            context.View.ApplyPortraits(
                context.State.CreatePortraitSnapshot(),
                context.State.ActiveSpeakerId
            );
            return StoryNodeExecutionResult.Continue(node.nextNodeId);
        }
    }

    // 隐藏目标角色；若其正在说话，运行时状态会同时清除激活说话者。
    internal sealed class HidePortraitNodeHandler : IStoryNodeHandler
    {
        public string NodeType { get { return StoryNodeTypes.HidePortrait; } }

        public StoryNodeExecutionResult Execute(
            StoryNodeExecutionContext context,
            StoryNodeData node
        )
        {
            if (node.portrait == null || string.IsNullOrWhiteSpace(node.portrait.characterId))
            {
                return StoryNodeExecutionResult.Error(node.nodeId + " 缺少 portrait.characterId");
            }

            context.State.HidePortrait(node.portrait.characterId);
            context.View.ApplyPortraits(
                context.State.CreatePortraitSnapshot(),
                context.State.ActiveSpeakerId
            );
            return StoryNodeExecutionResult.Continue(node.nextNodeId);
        }
    }

    // 修改已有立绘表情，并刷新所有立绘的显示状态。
    internal sealed class ChangeExpressionNodeHandler : IStoryNodeHandler
    {
        public string NodeType { get { return StoryNodeTypes.ChangeExpression; } }

        public StoryNodeExecutionResult Execute(
            StoryNodeExecutionContext context,
            StoryNodeData node
        )
        {
            if (node.portrait == null || string.IsNullOrWhiteSpace(node.portrait.characterId))
            {
                return StoryNodeExecutionResult.Error(node.nodeId + " 缺少 portrait.characterId");
            }

            context.State.ChangeExpression(
                node.portrait.characterId,
                node.portrait.expressionId
            );
            context.View.ApplyPortraits(
                context.State.CreatePortraitSnapshot(),
                context.State.ActiveSpeakerId
            );
            return StoryNodeExecutionResult.Continue(node.nextNodeId);
        }
    }

    // 创建定时等待点；等待结束后由控制器继续执行 nextNodeId。
    internal sealed class WaitNodeHandler : IStoryNodeHandler
    {
        public string NodeType { get { return StoryNodeTypes.Wait; } }

        public StoryNodeExecutionResult Execute(
            StoryNodeExecutionContext context,
            StoryNodeData node
        )
        {
            if (node.wait == null)
            {
                return StoryNodeExecutionResult.Error(node.nodeId + " 缺少 wait 数据");
            }

            return StoryNodeExecutionResult.WaitForTime(
                node.nextNodeId,
                node.wait.seconds,
                node.wait.skippable
            );
        }
    }

    // 将 End 数据节点转换成统一的剧情结束结果。
    internal sealed class EndNodeHandler : IStoryNodeHandler
    {
        public string NodeType { get { return StoryNodeTypes.End; } }

        public StoryNodeExecutionResult Execute(
            StoryNodeExecutionContext context,
            StoryNodeData node
        )
        {
            return StoryNodeExecutionResult.End();
        }
    }
}
