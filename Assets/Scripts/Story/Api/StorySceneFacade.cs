using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectGuilt.Story
{
    // 外部游戏流程的统一入口。推荐通过 IStorySceneService 持有本组件。
    public sealed class StorySceneFacade :
        MonoBehaviour,
        IStorySceneService,
        IStorySceneConfigurator
    {
        [Header("Presentation")]
        [SerializeField] private StoryViewBehaviour storyView;

        [Header("Content")]
        [SerializeField] private string resourcesFolder = "Story";

        [Header("Playback")]
        [SerializeField] private float charactersPerSecond = 40f;
        [SerializeField] private float autoBaseDelay = 0.8f;
        [SerializeField] private float skipDelay = 0.05f;

        [Header("History")]
        [SerializeField] private int maxHistoryEntries = 500;

        private StoryJsonSerializer serializer;
        private IStoryContentProvider contentProvider;
        private IStoryClock clock;
        private StoryFlowController controller;
        private readonly Dictionary<string, IStoryNodeHandler> customNodeHandlers =
            new Dictionary<string, IStoryNodeHandler>(StringComparer.OrdinalIgnoreCase);

        public event Action<string> StoryStarted;
        public event Action<string> StoryEnded;
        public event Action<string> StoryError;

        internal StoryRuntimeState RuntimeState
        {
            get { return controller != null ? controller.State : null; }
        }

        // 只在尚未组装 Controller 时建立默认依赖，避免覆盖宿主在 Awake 前完成的注入。
        private void Awake()
        {
            // 如果宿主项目已在更早的脚本执行顺序中注入依赖，不要用默认实现覆盖它。
            EnsureInitialized();
        }

        // 使用不受 Time.timeScale 影响的时间驱动剧情，暂停游戏时仍可正常显示对话。
        private void Update()
        {
            if (controller != null)
            {
                controller.Tick(Time.unscaledDeltaTime);
            }
        }

        // 解除事件订阅，防止组件销毁后 Controller 仍保留对 Facade 的回调。
        private void OnDestroy()
        {
            UnsubscribeControllerEvents();
        }

        // 在 Inspector 修改参数时约束最小值，避免零速逐字或负数等待时间。
        private void OnValidate()
        {
            charactersPerSecond = Mathf.Max(1f, charactersPerSecond);
            autoBaseDelay = Mathf.Max(0f, autoBaseDelay);
            skipDelay = Mathf.Max(0.01f, skipDelay);
            maxHistoryEntries = Mathf.Max(1, maxHistoryEntries);
        }

        // 宿主推荐入口：启动剧情后由内置 View 自动显示完整 Story 面板。
        public bool OpenStoryPanel(string storyId)
        {
            return ShowStory(storyId);
        }

        // 宿主推荐入口：中止当前剧情、清理会话状态并隐藏完整 Story 面板。
        public void CloseStoryPanel()
        {
            CloseStory();
        }

        // 按 storyId 从当前内容源读取 JSON，再复用统一的 JSON 启动入口。
        public bool ShowStory(string storyId)
        {
            EnsureInitialized();
            string storyJson;
            string errorMessage;

            if (!contentProvider.TryGetStoryJson(storyId, out storyJson, out errorMessage))
            {
                ReportError(errorMessage);
                return false;
            }

            return ShowStoryFromJson(storyJson);
        }

        // 反序列化并校验剧情定义，成功后由核心 Controller 建立运行状态。
        public bool ShowStoryFromJson(string storyJson)
        {
            EnsureInitialized();
            StoryDefinitionData definition;
            string errorMessage;

            if (!serializer.TryDeserializeDefinition(
                storyJson,
                out definition,
                out errorMessage
            ))
            {
                ReportError(errorMessage);
                return false;
            }

            return controller.StartStory(definition);
        }

        // 转发普通点击推进；核心层会区分补全文本、进入下一节点和不可推进状态。
        public bool RequestAdvance()
        {
            EnsureInitialized();
            return controller.RequestAdvance();
        }

        // 只允许提交当前 Choice 节点中仍满足条件的选项。
        public bool SubmitChoice(string optionId)
        {
            EnsureInitialized();
            return controller.SubmitChoice(optionId);
        }

        // 自动播放与快速播放互斥，具体模式切换由 Controller 统一处理。
        public void SetAuto(bool enabled)
        {
            EnsureInitialized();
            controller.SetAuto(enabled);
        }

        public void SetSkip(bool enabled)
        {
            EnsureInitialized();
            controller.SetSkip(enabled);
        }

        // 跳到定义中的 skipNodeId；没有收尾节点时由 Controller 正常结束剧情。
        public bool SkipToEnd()
        {
            EnsureInitialized();
            return controller.SkipToEnd();
        }

        // 打开覆盖面板后核心 Tick 暂停，适用于菜单和历史记录界面。
        public void OpenOverlay()
        {
            EnsureInitialized();
            controller.OpenOverlay();
        }

        public void CloseOverlay()
        {
            EnsureInitialized();
            controller.CloseOverlay();
        }

        // 隐藏剧情 UI 时保留全部状态，再次显示不会额外推进节点。
        public void ToggleStoryUi()
        {
            EnsureInitialized();
            controller.ToggleStoryUi();
        }

        // 主动中止并清理当前剧情，不触发正常 End 节点的 StoryEnded 流程。
        public void CloseStory()
        {
            EnsureInitialized();
            controller.CloseStory();
        }

        // 返回历史记录副本，避免宿主 UI 直接修改内部集合。
        public IReadOnlyList<StoryHistoryEntryData> GetHistorySnapshot()
        {
            EnsureInitialized();
            return controller.GetHistorySnapshot();
        }

        // 只供宿主项目组装层调用。普通游戏流程应只持有 IStorySceneService。
        // 自定义节点通过接口注册，例如未来的 StartBattle、Timeline 或语音节点。
        public void RegisterNodeHandler(IStoryNodeHandler handler)
        {
            if (handler == null || string.IsNullOrWhiteSpace(handler.NodeType))
            {
                ReportError("剧情节点处理器或 NodeType 为空");
                return;
            }

            EnsureInitialized();

            try
            {
                controller.RegisterNodeHandler(handler);
                customNodeHandlers[handler.NodeType.Trim()] = handler;
            }
            catch (Exception exception)
            {
                ReportError(
                    "注册剧情节点处理器失败（" +
                    exception.GetType().Name + "）：" + exception.Message
                );
            }
        }

        // 单元测试或宿主项目组装层可注入自定义内容源、View 和时钟。
        public void ConfigureDependencies(
            IStoryContentProvider customContentProvider,
            IStoryView customView,
            IStoryClock customClock
        )
        {
            if (IsStoryActive())
            {
                ReportError("剧情运行期间不能重新配置依赖，请先结束或关闭当前剧情");
                return;
            }

            serializer = serializer ?? new StoryJsonSerializer();
            contentProvider = customContentProvider ??
                new ResourcesStoryContentProvider(resourcesFolder);
            clock = customClock ?? new SystemStoryClock();
            BuildController(customView ?? ResolveView());
        }

        // 首次运行时装配单文件 Resources 内容源、系统时钟和当前 View。
        private void InitializeDefaultDependencies()
        {
            serializer = new StoryJsonSerializer();
            contentProvider = new ResourcesStoryContentProvider(resourcesFolder);
            clock = new SystemStoryClock();
            BuildController(ResolveView());
        }

        // 未绑定正式 View 时使用空实现，让剧情逻辑仍可运行和接受自动化验证。
        private IStoryView ResolveView()
        {
            if (storyView == null)
            {
                storyView = GetComponentInChildren<StoryViewBehaviour>(true);
            }

            if (storyView != null)
            {
                return storyView;
            }

            Debug.LogWarning(
                "StorySceneFacade 未绑定 StoryViewBehaviour，当前使用空 View，仅运行剧情逻辑"
            );
            return new NullStoryView();
        }

        // 集中创建核心对象并重新挂接事件；重新配置依赖时会替换整个运行组合。
        private void BuildController(IStoryView view)
        {
            UnsubscribeControllerEvents();
            StoryRuntimeState runtimeState = new StoryRuntimeState();
            StoryHistoryService history = new StoryHistoryService(
                clock,
                maxHistoryEntries
            );
            StoryTextPresenter textPresenter = new StoryTextPresenter(
                charactersPerSecond
            );
            StoryNodeExecutor nodeExecutor = new StoryNodeExecutor();
            controller = new StoryFlowController(
                runtimeState,
                history,
                textPresenter,
                nodeExecutor,
                view,
                autoBaseDelay,
                skipDelay
            );
            controller.StoryStarted += HandleStoryStarted;
            controller.StoryEnded += HandleStoryEnded;
            controller.StoryError += HandleStoryError;

            foreach (IStoryNodeHandler handler in customNodeHandlers.Values)
            {
                controller.RegisterNodeHandler(handler);
            }
        }

        // 所有公开入口都可安全调用；尚未初始化时会按需建立默认组合。
        private void EnsureInitialized()
        {
            if (controller == null)
            {
                InitializeDefaultDependencies();
            }
        }

        // 在 Controller 被替换或组件销毁前解除旧事件，避免一次错误被重复转发。
        private void UnsubscribeControllerEvents()
        {
            if (controller == null)
            {
                return;
            }

            controller.StoryStarted -= HandleStoryStarted;
            controller.StoryEnded -= HandleStoryEnded;
            controller.StoryError -= HandleStoryError;
        }

        // 只有 Idle、Ended 和 Error 状态允许重新注入依赖，防止运行中丢失进度。
        private bool IsStoryActive()
        {
            if (controller == null || controller.State == null)
            {
                return false;
            }

            StoryMainState state = controller.State.MainState;
            return state != StoryMainState.Idle &&
                state != StoryMainState.Ended &&
                state != StoryMainState.Error;
        }

        // 以下三个回调把核心生命周期转换为稳定的外部 API 事件。
        private void HandleStoryStarted(string storyId)
        {
            if (StoryStarted != null)
            {
                StoryStarted(storyId);
            }
        }

        private void HandleStoryEnded(string storyId)
        {
            if (StoryEnded != null)
            {
                StoryEnded(storyId);
            }
        }

        private void HandleStoryError(string message)
        {
            ReportError(message);
        }

        // 统一补全空错误、写入 Unity Console，并通知宿主错误监听者。
        private void ReportError(string message)
        {
            string safeMessage = string.IsNullOrWhiteSpace(message)
                ? "未知剧情系统错误"
                : message;
            Debug.LogError("StorySystem：" + safeMessage);

            if (StoryError != null)
            {
                StoryError(safeMessage);
            }
        }
    }
}
