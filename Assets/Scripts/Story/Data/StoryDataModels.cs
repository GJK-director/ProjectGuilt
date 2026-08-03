using System;
using System.Collections.Generic;

namespace ProjectGuilt.Story
{
    // 内置节点类型常量。JSON 和处理器统一使用这些值，避免散落字符串拼写错误。
    public static class StoryNodeTypes
    {
        public const string Dialogue = "Dialogue";
        public const string Choice = "Choice";
        public const string SetVariable = "SetVariable";
        public const string Condition = "Condition";
        public const string Jump = "Jump";
        public const string ChangeBackground = "ChangeBackground";
        public const string ShowPortrait = "ShowPortrait";
        public const string HidePortrait = "HidePortrait";
        public const string ChangeExpression = "ChangeExpression";
        public const string Wait = "Wait";
        public const string End = "End";
    }

    // 一份完整剧情 JSON 的根对象，负责声明入口、跳过收尾和全部节点。
    [Serializable]
    public sealed class StoryDefinitionData
    {
        public int formatVersion = 1;
        public string storyId;
        public string startNodeId;
        public string skipNodeId;
        public List<StoryNodeData> nodes = new List<StoryNodeData>();
    }

    // 使用统一节点 DTO，避免 JSON 多态反序列化与 Unity Inspector 兼容问题。
    // 每种 nodeType 只读取自己需要的载荷字段。
    [Serializable]
    public sealed class StoryNodeData
    {
        public string nodeId;
        public string nodeType;
        public string nextNodeId;
        public StoryDialogueData dialogue;
        public List<StoryChoiceOptionData> choices = new List<StoryChoiceOptionData>();
        public List<StoryVariableOperationData> variableOperations = new List<StoryVariableOperationData>();
        public List<StoryConditionData> conditions = new List<StoryConditionData>();
        public StoryConditionMode conditionMode = StoryConditionMode.All;
        public string trueNodeId;
        public string falseNodeId;
        public string jumpNodeId;
        public StoryBackgroundCommandData background;
        public StoryPortraitCommandData portrait;
        public StoryWaitData wait;
        // 自定义节点使用的宿主参数，例如 encounterId、timelineId、voiceId。
        public Dictionary<string, string> parameters =
            new Dictionary<string, string>(StringComparer.Ordinal);
    }

    // Dialogue 节点载荷；文本资源保持纯字符串，具体字体和语音由宿主 View 处理。
    [Serializable]
    public sealed class StoryDialogueData
    {
        public string speakerId;
        public string speakerName;
        public string text;
        public float autoDelayOverride = -1f;
        public bool skippable = true;
    }

    // 单个剧情选项，包含可用条件、不可用表现、变量写入和目标节点。
    [Serializable]
    public sealed class StoryChoiceOptionData
    {
        public string optionId;
        public string text;
        public string targetNodeId;
        public StoryConditionMode conditionMode = StoryConditionMode.All;
        public StoryChoiceUnavailableMode unavailableMode = StoryChoiceUnavailableMode.Hide;
        public List<StoryConditionData> conditions = new List<StoryConditionData>();
        public List<StoryVariableOperationData> variableOperations = new List<StoryVariableOperationData>();
    }

    // 一次变量写入命令；同一节点中的多条命令由变量仓库原子执行。
    [Serializable]
    public sealed class StoryVariableOperationData
    {
        public string variableName;
        public StoryVariableOperationType operation = StoryVariableOperationType.Set;
        public StoryValueData value = new StoryValueData();
    }

    // 单条变量条件，Exists/NotExists 不需要 compareValue 的实际值。
    [Serializable]
    public sealed class StoryConditionData
    {
        public string variableName;
        public StoryComparisonType comparison = StoryComparisonType.Equals;
        public StoryValueData compareValue = new StoryValueData();
    }

    // 可序列化的联合值对象，valueType 决定读取 Bool、Int 或 String 字段。
    [Serializable]
    public sealed class StoryValueData
    {
        public StoryValueType valueType = StoryValueType.Bool;
        public bool boolValue;
        public int intValue;
        public string stringValue;

        // 快照和变量写入都使用深拷贝，避免外部 DTO 与运行时共享可变对象。
        public StoryValueData Clone()
        {
            return new StoryValueData
            {
                valueType = valueType,
                boolValue = boolValue,
                intValue = intValue,
                stringValue = stringValue
            };
        }
    }

    // 背景切换命令只保存资源 ID 和淡入时长，不直接引用 Unity 贴图。
    [Serializable]
    public sealed class StoryBackgroundCommandData
    {
        public string backgroundId;
        public float fadeSeconds;
    }

    // 立绘命令描述角色、位置、表情和基础显示参数，由宿主 View 映射实际资源。
    [Serializable]
    public sealed class StoryPortraitCommandData
    {
        public string characterId;
        public string positionId;
        public string expressionId;
        public float brightness = 1f;
        public float scale = 1f;
    }

    // Wait 节点载荷；skippable 决定点击和 Skip 模式能否提前结束等待。
    [Serializable]
    public sealed class StoryWaitData
    {
        public float seconds;
        public bool skippable = true;
    }

    // 核心层输出给 UI 的只读选项数据，不暴露条件和变量写入细节。
    [Serializable]
    public sealed class StoryChoiceViewData
    {
        public string optionId;
        public string text;
        public bool interactable;
    }

    // 当前在场立绘的运行快照，用于向 View 输出位置、表情、亮度和缩放。
    [Serializable]
    public sealed class StoryPortraitStateData
    {
        public string characterId;
        public string positionId;
        public string expressionId;
        public bool visible;
        public float brightness = 1f;
        public float scale = 1f;

        // View 只接收副本，避免意外修改 RuntimeState 内部立绘状态。
        public StoryPortraitStateData Clone()
        {
            return new StoryPortraitStateData
            {
                characterId = characterId,
                positionId = positionId,
                expressionId = expressionId,
                visible = visible,
                brightness = brightness,
                scale = scale
            };
        }
    }

    // 当前会话的历史记录条目，同时保存稳定顺序号和 UTC 时间戳。
    [Serializable]
    public sealed class StoryHistoryEntryData
    {
        public int sequence;
        public string timestampUtc;
        public string entryType;
        public string nodeId;
        public string speakerId;
        public string speakerName;
        public string text;

        // 历史面板通过副本读取，不能反向修改内部记录。
        public StoryHistoryEntryData Clone()
        {
            return new StoryHistoryEntryData
            {
                sequence = sequence,
                timestampUtc = timestampUtc,
                entryType = entryType,
                nodeId = nodeId,
                speakerId = speakerId,
                speakerName = speakerName,
                text = text
            };
        }
    }

}
