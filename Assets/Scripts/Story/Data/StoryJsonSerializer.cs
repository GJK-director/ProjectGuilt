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

                if (!ValidateBackgroundCommand(
                        node.nodeId,
                        node.background,
                        out errorMessage
                    ))
                {
                    return false;
                }

                if (!ValidateInlinePauseActions(node, out errorMessage))
                {
                    return false;
                }

                if (!ValidateDialoguePresentation(node, out errorMessage))
                {
                    return false;
                }

                if (!ValidateDialogueSpeed(node, out errorMessage))
                {
                    return false;
                }

                if (!ValidateDialogueAudio(node, out errorMessage))
                {
                    return false;
                }

                if (!ValidateDialogueMarkup(node, out errorMessage))
                {
                    return false;
                }

                if (!ValidateSpecialCommand(node, out errorMessage))
                {
                    return false;
                }

                if (!ValidateInstantReveal(node, out errorMessage))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateDialoguePresentation(
            StoryNodeData node,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (node == null || node.dialogue == null)
            {
                return true;
            }

            if (node.dialogue.centerScreenStyle != null)
            {
                if (node.dialogue.centerScreenStyle.fontSize < 0)
                {
                    errorMessage = node.nodeId +
                        " 的 centerScreenStyle.fontSize 不能小于 0";
                    return false;
                }

                if (node.dialogue.presentationMode !=
                    StoryDialoguePresentationMode.CenterScreen)
                {
                    errorMessage = node.nodeId +
                        " 的 centerScreenStyle 只能用于 CenterScreen";
                    return false;
                }
            }

            if (node.dialogue.presentationMode == StoryDialoguePresentationMode.Auto)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(node.dialogue.speakerId) ||
                !string.IsNullOrWhiteSpace(node.dialogue.speakerName))
            {
                errorMessage = node.nodeId + " 的 presentationMode " +
                    node.dialogue.presentationMode + " 要求 speakerId 和 speakerName 为空";
                return false;
            }

            return true;
        }

        private static bool ValidateDialogueSpeed(
            StoryNodeData node,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (node == null || node.dialogue == null)
            {
                return true;
            }

            float overrideValue = node.dialogue.charactersPerSecondOverride;

            if (float.IsNaN(overrideValue) || float.IsInfinity(overrideValue))
            {
                errorMessage = node.nodeId +
                    " 的 charactersPerSecondOverride 必须是有限数字";
                return false;
            }

            return true;
        }

        private static bool ValidateDialogueAudio(
            StoryNodeData node,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (node == null || node.dialogue == null)
            {
                return true;
            }

            float volume = node.dialogue.typingAudioVolume;

            if (float.IsNaN(volume) ||
                float.IsInfinity(volume) ||
                volume < 0f)
            {
                errorMessage = node.nodeId +
                    " 的 typingAudioVolume 必须是大于等于 0 的有限数字";
                return false;
            }

            return true;
        }

        private static bool ValidateInstantReveal(
            StoryNodeData node,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (node == null || node.dialogue == null ||
                !node.dialogue.instantReveal)
            {
                return true;
            }

            StoryTextMarkupResult markup;
            string markupError;

            if (!StoryTextMarkupParser.TryParse(
                    node.dialogue.text,
                    out markup,
                    out markupError
                ))
            {
                return true;
            }

            if (markup.InlinePausePositions.Count > 0)
            {
                errorMessage = node.nodeId +
                    " 的 instantReveal 不允许包含 || 或 ｜｜";
                return false;
            }

            if (markup.TimedPauses.Count > 0)
            {
                errorMessage = node.nodeId +
                    " 的 instantReveal 不允许包含 [[wait=N]]";
                return false;
            }

            return true;
        }

        private static bool ValidateDialogueMarkup(
            StoryNodeData node,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (node == null || node.dialogue == null)
            {
                return true;
            }

            StoryTextMarkupResult markup;
            string markupError;

            if (StoryTextMarkupParser.TryParse(
                    node.dialogue.text,
                    out markup,
                    out markupError
                ))
            {
                return true;
            }

            errorMessage = node.nodeId + " 的 dialogue.text 标记校验失败：" + markupError;
            return false;
        }

        private static bool ValidateInlinePauseActions(
            StoryNodeData node,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (node == null || node.dialogue == null ||
                node.dialogue.inlinePauseActions == null ||
                node.dialogue.inlinePauseActions.Count == 0)
            {
                return true;
            }

            int pauseCount = CountInlinePauseMarkers(node.dialogue.text);
            HashSet<int> pauseIndexes = new HashSet<int>();

            foreach (StoryInlinePauseActionData action in node.dialogue.inlinePauseActions)
            {
                if (action == null)
                {
                    errorMessage = node.nodeId + " 包含空 inlinePauseAction";
                    return false;
                }

                if (action.pauseIndex < 0)
                {
                    errorMessage = node.nodeId + " 的 inlinePauseAction.pauseIndex 不能小于 0";
                    return false;
                }

                if (!pauseIndexes.Add(action.pauseIndex))
                {
                    errorMessage = node.nodeId + " 包含重复 inlinePauseAction.pauseIndex：" +
                        action.pauseIndex;
                    return false;
                }

                if (action.background == null ||
                    string.IsNullOrWhiteSpace(action.background.backgroundId))
                {
                    errorMessage = node.nodeId + " 的 inlinePauseAction 缺少 background.backgroundId";
                    return false;
                }

                if (!ValidateBackgroundCommand(
                        node.nodeId + " 的 inlinePauseAction[" + action.pauseIndex + "]",
                        action.background,
                        out errorMessage
                    ))
                {
                    return false;
                }

                if (action.pauseIndex >= pauseCount)
                {
                    errorMessage = node.nodeId + " 的 inlinePauseAction.pauseIndex 不对应正文中的 ||：" +
                        action.pauseIndex;
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateBackgroundCommand(
            string owner,
            StoryBackgroundCommandData background,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (background == null ||
                background.transitionMode != StoryBackgroundTransitionMode.CrossFade)
            {
                return true;
            }

            if (background.fadeSeconds <= 0f)
            {
                errorMessage = owner + " 的 CrossFade fadeSeconds 必须 > 0";
                return false;
            }

            if (background.fadeOutSeconds <= 0f)
            {
                errorMessage = owner + " 的 CrossFade fadeOutSeconds 必须 > 0";
                return false;
            }

            return true;
        }

        private static int CountInlinePauseMarkers(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            int count = 0;

            for (int index = 0; index < text.Length - 1; index++)
            {
                if ((text[index] == '|' && text[index + 1] == '|') ||
                    (text[index] == '｜' && text[index + 1] == '｜'))
                {
                    count++;
                    index++;
                }
            }

            return count;
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

            if (IsNodeType(node.nodeType, StoryNodeTypes.ChangeBgm) &&
                !ValidateChangeBgmNode(node, out errorMessage))
            {
                return false;
            }

            if (IsNodeType(node.nodeType, StoryNodeTypes.ShowForeground) &&
                !ValidateShowForegroundNode(node, out errorMessage))
            {
                return false;
            }

            if (IsNodeType(node.nodeType, StoryNodeTypes.PlaySfx) &&
                !ValidateSfxNode(node, out errorMessage))
            {
                return false;
            }

            if (IsNodeType(node.nodeType, StoryNodeTypes.WaitForSfx) &&
                !ValidateSfxNode(node, out errorMessage))
            {
                return false;
            }

            if (IsNodeType(node.nodeType, StoryNodeTypes.FadeDialogue) &&
                !ValidateDialogueFadeNode(node, out errorMessage))
            {
                return false;
            }

            if (IsNodeType(node.nodeType, StoryNodeTypes.SetVisualFraming) &&
                !ValidateVisualFramingNode(node, out errorMessage))
            {
                return false;
            }

            if (IsNodeType(node.nodeType, StoryNodeTypes.FadeVisualToBlack) &&
                !ValidateVisualFadeNode(node, out errorMessage))
            {
                return false;
            }

            return true;
        }

        private static bool ValidateSpecialCommand(
            StoryNodeData node,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (node == null)
            {
                return true;
            }

            if (IsNodeType(node.nodeType, StoryNodeTypes.PlaySfx) ||
                IsNodeType(node.nodeType, StoryNodeTypes.WaitForSfx))
            {
                return ValidateSfxNode(node, out errorMessage);
            }

            if (IsNodeType(node.nodeType, StoryNodeTypes.PlayAmbient))
            {
                return ValidateAmbientNode(node, out errorMessage);
            }

            if (IsNodeType(node.nodeType, StoryNodeTypes.FadeDialogue))
            {
                return ValidateDialogueFadeNode(node, out errorMessage);
            }

            if (IsNodeType(node.nodeType, StoryNodeTypes.SetVisualFraming))
            {
                return ValidateVisualFramingNode(node, out errorMessage);
            }

            if (IsNodeType(node.nodeType, StoryNodeTypes.FadeVisualToBlack))
            {
                return ValidateVisualFadeNode(node, out errorMessage);
            }

            return true;
        }

        private static bool ValidateSfxNode(
            StoryNodeData node,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (node.sfx == null || string.IsNullOrWhiteSpace(node.sfx.sfxId))
            {
                errorMessage = node.nodeId + " 缺少 sfx.sfxId";
                return false;
            }

            if (float.IsNaN(node.sfx.volume) ||
                float.IsInfinity(node.sfx.volume) ||
                node.sfx.volume < 0f)
            {
                errorMessage = node.nodeId +
                    " 的 sfx.volume 必须是大于等于 0 的有限数字";
                return false;
            }

            if (!Enum.IsDefined(typeof(StorySfxChannel), node.sfx.channel))
            {
                errorMessage = node.nodeId + " 的 sfx.channel 无效";
                return false;
            }

            return true;
        }

        private static bool ValidateAmbientNode(
            StoryNodeData node,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (node.ambient == null || string.IsNullOrWhiteSpace(node.ambient.audioId))
            {
                errorMessage = node.nodeId + " 缺少 ambient.audioId";
                return false;
            }

            if (float.IsNaN(node.ambient.volume) ||
                float.IsInfinity(node.ambient.volume) ||
                node.ambient.volume < 0f)
            {
                errorMessage = node.nodeId +
                    " 的 ambient.volume 必须是大于等于 0 的有限数字";
                return false;
            }

            if (float.IsNaN(node.ambient.fadeInSeconds) ||
                float.IsInfinity(node.ambient.fadeInSeconds) ||
                node.ambient.fadeInSeconds < 0f)
            {
                errorMessage = node.nodeId +
                    " 的 ambient.fadeInSeconds 必须是大于等于 0 的有限数字";
                return false;
            }

            return true;
        }

        private static bool ValidateDialogueFadeNode(
            StoryNodeData node,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (node.fadeDialogue == null)
            {
                errorMessage = node.nodeId + " 缺少 fadeDialogue 数据";
                return false;
            }

            if (float.IsNaN(node.fadeDialogue.targetAlpha) ||
                float.IsInfinity(node.fadeDialogue.targetAlpha) ||
                node.fadeDialogue.targetAlpha < 0f ||
                node.fadeDialogue.targetAlpha > 1f)
            {
                errorMessage = node.nodeId +
                    " 的 fadeDialogue.targetAlpha 必须在 0 到 1 之间且为有限数字";
                return false;
            }

            if (float.IsNaN(node.fadeDialogue.fadeSeconds) ||
                float.IsInfinity(node.fadeDialogue.fadeSeconds) ||
                node.fadeDialogue.fadeSeconds < 0f)
            {
                errorMessage = node.nodeId +
                    " 的 fadeDialogue.fadeSeconds 必须是大于等于 0 的有限数字";
                return false;
            }

            return true;
        }

        private static bool ValidateVisualFramingNode(
            StoryNodeData node,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (node.visualFraming == null)
            {
                errorMessage = node.nodeId + " 缺少 visualFraming 数据";
                return false;
            }

            if (float.IsNaN(node.visualFraming.scale) ||
                float.IsInfinity(node.visualFraming.scale) ||
                node.visualFraming.scale <= 0f)
            {
                errorMessage = node.nodeId +
                    " 的 visualFraming.scale 必须是大于 0 的有限数字";
                return false;
            }

            if (float.IsNaN(node.visualFraming.offsetX) ||
                float.IsInfinity(node.visualFraming.offsetX))
            {
                errorMessage = node.nodeId +
                    " 的 visualFraming.offsetX 必须是有限数字";
                return false;
            }

            if (float.IsNaN(node.visualFraming.offsetY) ||
                float.IsInfinity(node.visualFraming.offsetY))
            {
                errorMessage = node.nodeId +
                    " 的 visualFraming.offsetY 必须是有限数字";
                return false;
            }

            return true;
        }

        private static bool ValidateVisualFadeNode(
            StoryNodeData node,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (node.visualFade == null)
            {
                errorMessage = node.nodeId + " 缺少 visualFade 数据";
                return false;
            }

            if (float.IsNaN(node.visualFade.fadeSeconds) ||
                float.IsInfinity(node.visualFade.fadeSeconds) ||
                node.visualFade.fadeSeconds < 0f)
            {
                errorMessage = node.nodeId +
                    " 的 visualFade.fadeSeconds 必须是大于等于 0 的有限数字";
                return false;
            }

            return true;
        }

        private static bool ValidateShowForegroundNode(
            StoryNodeData node,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (node.foreground == null)
            {
                errorMessage = node.nodeId + " 缺少 foreground 数据";
                return false;
            }

            if (string.IsNullOrWhiteSpace(node.foreground.foregroundId))
            {
                errorMessage = node.nodeId + " 缺少 foreground.foregroundId";
                return false;
            }

            if (float.IsNaN(node.foreground.fadeSeconds) ||
                float.IsInfinity(node.foreground.fadeSeconds) ||
                node.foreground.fadeSeconds < 0f)
            {
                errorMessage = node.nodeId +
                    " 的 foreground.fadeSeconds 必须是大于等于 0 的有限数字";
                return false;
            }

            if (float.IsNaN(node.foreground.offsetX) ||
                float.IsInfinity(node.foreground.offsetX))
            {
                errorMessage = node.nodeId +
                    " 的 foreground.offsetX 必须是有限数字";
                return false;
            }

            if (float.IsNaN(node.foreground.offsetY) ||
                float.IsInfinity(node.foreground.offsetY))
            {
                errorMessage = node.nodeId +
                    " 的 foreground.offsetY 必须是有限数字";
                return false;
            }

            if (float.IsNaN(node.foreground.scale) ||
                float.IsInfinity(node.foreground.scale) ||
                node.foreground.scale <= 0f)
            {
                errorMessage = node.nodeId +
                    " 的 foreground.scale 必须是大于 0 的有限数字";
                return false;
            }

            return true;
        }

        private static bool ValidateChangeBgmNode(
            StoryNodeData node,
            out string errorMessage
        )
        {
            errorMessage = string.Empty;

            if (node.bgm == null)
            {
                errorMessage = node.nodeId + " 缺少 bgm 数据";
                return false;
            }

            if (string.IsNullOrWhiteSpace(node.bgm.bgmId))
            {
                errorMessage = node.nodeId + " 缺少 bgm.bgmId";
                return false;
            }

            if (node.bgm.fadeOutSeconds < 0f)
            {
                errorMessage = node.nodeId + " 的 bgm.fadeOutSeconds 不能小于 0";
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
                IsNodeType(nodeType, StoryNodeTypes.ShowForeground) ||
                IsNodeType(nodeType, StoryNodeTypes.ChangeBgm) ||
                IsNodeType(nodeType, StoryNodeTypes.PlaySfx) ||
                IsNodeType(nodeType, StoryNodeTypes.PlayAmbient) ||
                IsNodeType(nodeType, StoryNodeTypes.WaitForSfx) ||
                IsNodeType(nodeType, StoryNodeTypes.FadeDialogue) ||
                IsNodeType(nodeType, StoryNodeTypes.SetVisualFraming) ||
                IsNodeType(nodeType, StoryNodeTypes.FadeVisualToBlack) ||
                IsNodeType(nodeType, StoryNodeTypes.ShowPortrait) ||
                IsNodeType(nodeType, StoryNodeTypes.HidePortrait) ||
                IsNodeType(nodeType, StoryNodeTypes.ChangeExpression) ||
                IsNodeType(nodeType, StoryNodeTypes.Wait) ||
                IsNodeType(nodeType, StoryNodeTypes.WaitForAdvance);
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
