namespace ProjectGuilt.Story
{
    // 剧情主状态机。任一时刻只处于一个状态，输入和 Tick 会据此决定可执行操作。
    public enum StoryMainState
    {
        Idle,              // 没有正在运行的剧情
        ExecutingNode,     // 正在同步执行节点处理器
        Typing,            // 对话文本逐字显示中
        WaitingAdvance,    // 当前文本已完整，等待点击或自动推进
        ShowingChoice,     // 正在等待玩家选择
        WaitingTime,       // Wait 节点计时中
        Ended,             // 剧情通过 End 或 SkipToEnd 正常结束
        Error              // 数据或执行异常，停止继续推进
    }

    // 文本完成后的推进方式；Choice 节点会强制恢复 Manual，避免自动误选。
    public enum StoryPlaybackMode
    {
        Manual,
        Auto,
        Skip
    }

    // 节点处理器返回给 Controller 的统一执行结果类型。
    public enum StoryExecutionKind
    {
        Continue,          // 立即继续执行下一个节点
        WaitForDialogue,   // 等待逐字显示和玩家/自动推进
        WaitForChoice,     // 等待选项提交
        WaitForTime,       // 等待指定秒数
        End,               // 正常结束剧情
        Error              // 进入错误状态
    }

    // 剧情变量支持的基础类型，保持简单以便稳定序列化和跨项目移植。
    public enum StoryValueType
    {
        Bool,
        Int,
        String
    }

    // Set 覆盖变量；Add 只支持 Int，并包含溢出保护。
    public enum StoryVariableOperationType
    {
        Set,
        Add
    }

    // 条件比较方式。数值大小比较要求两侧类型一致。
    public enum StoryComparisonType
    {
        Equals,
        NotEquals,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        Exists,
        NotExists
    }

    // 条件组求值方式：All 要求全部成立，Any 要求至少一个成立。
    public enum StoryConditionMode
    {
        All,
        Any
    }

    // 不可用选项可以隐藏，也可以显示为禁用状态。
    public enum StoryChoiceUnavailableMode
    {
        Hide,
        Disable
    }
}
