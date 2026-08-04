using System;
using System.Collections.Generic;

namespace ProjectGuilt.Story
{
    // 游戏流程只依赖这个接口，不需要了解剧情内部的节点或 UI 实现。
    public interface IStorySceneService
    {
        // 剧情生命周期事件。宿主通过这些事件切换输入、场景或后续游戏流程。
        event Action<string> StoryStarted;
        event Action<string> StoryEnded;
        event Action<string> StoryError;

        // 宿主推荐只调用这两个面板级入口，不需要实现或继承 Story View。
        bool OpenStoryPanel(string storyId);
        void CloseStoryPanel();

        // 从内容源或直接 JSON 启动剧情。返回 false 表示数据校验或启动失败。
        // 这些是高级入口，供自定义表现层、自动化测试和特殊数据来源使用。
        bool ShowStory(string storyId);
        bool ShowStoryFromJson(string storyJson);

        // 处理玩家交互。普通点击与选项提交分开，防止 Choice 被误推进。
        bool RequestAdvance();
        bool SubmitChoice(string optionId);

        // 切换自动播放与快速播放；两者最终都由核心状态机控制推进时机。
        void SetAuto(bool enabled);
        void SetSkip(bool enabled);

        // 跳到剧情定义的收尾节点；未配置收尾节点时直接结束。
        bool SkipToEnd();

        // Overlay 用于菜单和历史面板；打开时剧情 Tick 会暂停。
        void OpenOverlay();
        void CloseOverlay();

        // 只切换剧情 UI 显示，不销毁当前剧情运行状态。
        void ToggleStoryUi();

        // 主动清理当前剧情并回到 Idle，不等同于正常执行 End 节点。
        void CloseStory();

        // 返回历史记录副本，调用方修改副本不会影响运行中的剧情。
        IReadOnlyList<StoryHistoryEntryData> GetHistorySnapshot();
    }

    // 只供宿主项目的组装层使用。普通游戏流程不应该依赖这个接口。
    public interface IStorySceneConfigurator
    {
        // 注入宿主提供的内容、表现和时钟适配器；传 null 时使用默认实现。
        void ConfigureDependencies(
            IStoryContentProvider customContentProvider,
            IStoryView customView,
            IStoryClock customClock
        );

        // 注册自定义节点，例如 StartBattle、Timeline 或 Voice。
        void RegisterNodeHandler(IStoryNodeHandler handler);
    }

    // 把 storyId 映射为 JSON 文本，核心层不关心数据来自 Resources、网络还是数据库。
    public interface IStoryContentProvider
    {
        bool TryGetStoryJson(string storyId, out string storyJson, out string errorMessage);
    }

    // 为当前会话的历史记录提供可替换 UTC 时间源，测试可注入固定时钟。
    public interface IStoryClock
    {
        string GetUtcTimestamp();
    }

    // 逻辑层只面向这个接口。正式 UI、测试替身和空实现都可以独立替换。
    public interface IStoryView
    {
        // 分别控制整个剧情根节点、剧情 UI 和覆盖面板的可见状态。
        void SetStoryVisible(bool visible);
        void SetStoryUiVisible(bool visible);
        void SetOverlayOpen(bool isOpen);

        // 同步自动/快进按钮与继续提示的表现状态。
        void SetPlaybackMode(StoryPlaybackMode mode);
        void SetContinueIndicator(bool visible);

        // 每次逐字字符数变化时刷新对话文本。
        void ShowDialogue(
            string speakerName,
            string fullText,
            int visibleCharacterCount,
            bool isComplete
        );

        // 显示或隐藏当前 Choice 节点的选项。
        void ShowChoices(IReadOnlyList<StoryChoiceViewData> choices);
        void HideChoices();

        // 资源 ID 由宿主 View 映射为实际背景和立绘资源。
        void SetBackground(string backgroundId, float fadeSeconds);
        void ApplyPortraits(
            IReadOnlyList<StoryPortraitStateData> portraits,
            string activeSpeakerId
        );

        // 允许 View 播放收尾表现；真正的游戏流程切换由 StoryEnded 事件负责。
        void NotifyStoryEnded(string storyId);
    }
}
