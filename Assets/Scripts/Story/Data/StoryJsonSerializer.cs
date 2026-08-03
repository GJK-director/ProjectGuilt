using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ProjectGuilt.Story
{
    // 统一负责剧情定义 JSON 的双向转换，并在进入运行时前执行结构校验。
    public sealed class StoryJsonSerializer
    {
        private static readonly JsonSerializerSettings SerializerSettings =
            CreateSerializerSettings();

        // 把剧情 JSON 转为 DTO，并验证剧情入口、节点唯一性和所有内置路由。
        public bool TryDeserializeDefinition(
            string json,
            out StoryDefinitionData definition,
            out string errorMessage
        )
        {
            definition = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(json))
            {
                errorMessage = "剧情 JSON 为空";
                return false;
            }

            if (!TryDeserializeJson(
                    json,
                    "剧情 JSON",
                    out definition,
                    out errorMessage
                ))
            {
                return false;
            }

            return ValidateDefinition(definition, out errorMessage);
        }

        // 主要用于编辑工具、导出和调试；运行时通常只需要反序列化剧情定义。
        public string SerializeDefinition(StoryDefinitionData definition)
        {
            return JsonConvert.SerializeObject(
                definition,
                SerializerSettings
            );
        }

        // 统一转换 Newtonsoft 解析异常，避免异常穿透普通游戏流程。
        private static bool TryDeserializeJson<TData>(
            string json,
            string displayName,
            out TData data,
            out string errorMessage
        )
            where TData : class
        {
            data = default(TData);
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(json))
            {
                errorMessage = displayName + " 为空";
                return false;
            }

            try
            {
                data = JsonConvert.DeserializeObject<TData>(
                    json,
                    SerializerSettings
                );
            }
            catch (JsonException exception)
            {
                errorMessage = displayName + " 反序列化失败：" + exception.Message;
                return false;
            }

            if (data == null)
            {
                errorMessage = displayName + " 反序列化结果为空";
                return false;
            }

            return true;
        }

        private static JsonSerializerSettings CreateSerializerSettings()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
            settings.Converters.Add(
                new StringEnumConverter
                {
                    AllowIntegerValues = false
                }
            );
            return settings;
        }

        // 对剧情定义执行一次完整静态检查，尽量把错误前移到加载阶段。
        public bool ValidateDefinition(StoryDefinitionData definition, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (definition == null)
            {
                errorMessage = "剧情定义为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(definition.storyId))
            {
                errorMessage = "剧情定义缺少 storyId";
                return false;
            }

            if (string.IsNullOrWhiteSpace(definition.startNodeId))
            {
                errorMessage = definition.storyId + " 缺少 startNodeId";
                return false;
            }

            if (definition.nodes == null || definition.nodes.Count == 0)
            {
                errorMessage = definition.storyId + " 没有任何剧情节点";
                return false;
            }

            Dictionary<string, StoryNodeData> nodeMap = new Dictionary<string, StoryNodeData>();

            foreach (StoryNodeData node in definition.nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
                {
                    errorMessage = definition.storyId + " 包含空节点或空 nodeId";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(node.nodeType))
                {
                    errorMessage = node.nodeId + " 缺少 nodeType";
                    return false;
                }

                if (nodeMap.ContainsKey(node.nodeId))
                {
                    errorMessage = "发现重复 nodeId：" + node.nodeId;
                    return false;
                }

                nodeMap.Add(node.nodeId, node);
            }

            if (!nodeMap.ContainsKey(definition.startNodeId))
            {
                errorMessage = "startNodeId 指向不存在的节点：" + definition.startNodeId;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(definition.skipNodeId) &&
                !nodeMap.ContainsKey(definition.skipNodeId))
            {
                errorMessage = "skipNodeId 指向不存在的节点：" + definition.skipNodeId;
                return false;
            }

            foreach (StoryNodeData node in definition.nodes)
            {
                if (!ValidateNodeRoutes(node, nodeMap, out errorMessage))
                {
                    return false;
                }
            }

            return true;
        }

        // 根据节点类型检查 next、Choice、Condition 和 Jump 的目标是否存在。
        private bool ValidateNodeRoutes(
            StoryNodeData node,
            Dictionary<string, StoryNodeData> nodeMap,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (!ValidateOptionalRoute(node.nodeId, "nextNodeId", node.nextNodeId, nodeMap, out errorMessage))
            {
                return false;
            }

            if (RequiresNextNode(node.nodeType) &&
                !ValidateRequiredRoute(
                    node.nodeId,
                    "nextNodeId",
                    node.nextNodeId,
                    nodeMap,
                    out errorMessage
                ))
            {
                return false;
            }

            if (IsNodeType(node.nodeType, StoryNodeTypes.Choice))
            {
                if (node.choices == null || node.choices.Count == 0)
                {
                    errorMessage = node.nodeId + " 是 Choice，但没有选项";
                    return false;
                }

                HashSet<string> optionIds = new HashSet<string>();

                foreach (StoryChoiceOptionData option in node.choices)
                {
                    if (option == null || string.IsNullOrWhiteSpace(option.optionId))
                    {
                        errorMessage = node.nodeId + " 包含空选项或空 optionId";
                        return false;
                    }

                    if (!optionIds.Add(option.optionId))
                    {
                        errorMessage = node.nodeId + " 包含重复 optionId：" + option.optionId;
                        return false;
                    }

                    if (!ValidateRequiredRoute(
                        node.nodeId,
                        "choice.targetNodeId",
                        option.targetNodeId,
                        nodeMap,
                        out errorMessage
                    ))
                    {
                        return false;
                    }
                }
            }

            if (IsNodeType(node.nodeType, StoryNodeTypes.Condition))
            {
                if (!ValidateRequiredRoute(node.nodeId, "trueNodeId", node.trueNodeId, nodeMap, out errorMessage) ||
                    !ValidateRequiredRoute(node.nodeId, "falseNodeId", node.falseNodeId, nodeMap, out errorMessage))
                {
                    return false;
                }
            }

            if (IsNodeType(node.nodeType, StoryNodeTypes.Jump) &&
                !ValidateRequiredRoute(node.nodeId, "jumpNodeId", node.jumpNodeId, nodeMap, out errorMessage))
            {
                return false;
            }

            return true;
        }

        // 这些内置节点执行完成后必须拥有 nextNodeId。
        private static bool RequiresNextNode(string nodeType)
        {
            return IsNodeType(nodeType, StoryNodeTypes.Dialogue) ||
                IsNodeType(nodeType, StoryNodeTypes.SetVariable) ||
                IsNodeType(nodeType, StoryNodeTypes.ChangeBackground) ||
                IsNodeType(nodeType, StoryNodeTypes.ShowPortrait) ||
                IsNodeType(nodeType, StoryNodeTypes.HidePortrait) ||
                IsNodeType(nodeType, StoryNodeTypes.ChangeExpression) ||
                IsNodeType(nodeType, StoryNodeTypes.Wait);
        }

        // nodeType 与执行器注册表保持大小写不敏感，降低 JSON 人工编辑成本。
        private static bool IsNodeType(string actualType, string expectedType)
        {
            return string.Equals(
                actualType,
                expectedType,
                StringComparison.OrdinalIgnoreCase
            );
        }

        // 可选路由为空时合法；只要填写就必须指向已存在节点。
        private bool ValidateOptionalRoute(
            string nodeId,
            string fieldName,
            string targetNodeId,
            Dictionary<string, StoryNodeData> nodeMap,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(targetNodeId))
            {
                return true;
            }

            return ValidateRequiredRoute(nodeId, fieldName, targetNodeId, nodeMap, out errorMessage);
        }

        // 必填路由同时检查空值和目标节点存在性，并返回包含字段名的错误信息。
        private bool ValidateRequiredRoute(
            string nodeId,
            string fieldName,
            string targetNodeId,
            Dictionary<string, StoryNodeData> nodeMap,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(targetNodeId))
            {
                errorMessage = nodeId + " 缺少 " + fieldName;
                return false;
            }

            if (!nodeMap.ContainsKey(targetNodeId))
            {
                errorMessage = nodeId + " 的 " + fieldName + " 指向不存在的节点：" + targetNodeId;
                return false;
            }

            return true;
        }
    }
}
