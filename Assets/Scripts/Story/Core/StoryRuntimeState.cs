using System;
using System.Collections.Generic;

namespace ProjectGuilt.Story
{
    // 保存单次剧情会话的可变状态；不持有 Unity 对象，便于独立测试。
    public sealed class StoryRuntimeState
    {
        // 以角色 ID 索引立绘，确保同一角色只维护一份当前显示状态。
        private readonly Dictionary<string, StoryPortraitStateData> portraits =
            new Dictionary<string, StoryPortraitStateData>(StringComparer.Ordinal);

        public string StoryId { get; private set; }
        public string CurrentNodeId { get; private set; }
        public string PendingNextNodeId { get; private set; }
        public StoryMainState MainState { get; private set; }
        public StoryPlaybackMode PlaybackMode { get; private set; }
        public bool IsOverlayOpen { get; private set; }
        public bool IsStoryUiVisible { get; private set; }
        public string CurrentBackgroundId { get; private set; }
        public string ActiveSpeakerId { get; private set; }
        public float WaitRemaining { get; private set; }
        public bool WaitSkippable { get; private set; }
        public StoryVariableStore Variables { get; private set; }

        // 变量仓库与运行时状态同生命周期，重开剧情时重置其内容。
        public StoryRuntimeState()
        {
            Variables = new StoryVariableStore();
            ResetToIdle();
        }

        // 开始全新剧情：初始化入口节点，并清空上一会话的变量和立绘。
        public void Begin(string storyId, string startNodeId)
        {
            StoryId = storyId;
            CurrentNodeId = startNodeId;
            PendingNextNodeId = string.Empty;
            MainState = StoryMainState.ExecutingNode;
            PlaybackMode = StoryPlaybackMode.Manual;
            IsOverlayOpen = false;
            IsStoryUiVisible = true;
            CurrentBackgroundId = string.Empty;
            ActiveSpeakerId = string.Empty;
            WaitRemaining = 0f;
            WaitSkippable = false;
            Variables.Clear();
            portraits.Clear();
        }

        // 完全退出剧情后回到空闲态，同时释放所有会话状态。
        public void ResetToIdle()
        {
            StoryId = string.Empty;
            CurrentNodeId = string.Empty;
            PendingNextNodeId = string.Empty;
            MainState = StoryMainState.Idle;
            PlaybackMode = StoryPlaybackMode.Manual;
            IsOverlayOpen = false;
            IsStoryUiVisible = true;
            CurrentBackgroundId = string.Empty;
            ActiveSpeakerId = string.Empty;
            WaitRemaining = 0f;
            WaitSkippable = false;
            Variables.Clear();
            portraits.Clear();
        }

        // 以下设置方法集中维护节点位置、主状态与播放模式，避免外部任意改写属性。
        public void SetCurrentNode(string nodeId)
        {
            CurrentNodeId = nodeId;
        }

        public void SetPendingNextNode(string nodeId)
        {
            PendingNextNodeId = nodeId;
        }

        public void SetMainState(StoryMainState state)
        {
            MainState = state;
        }

        public void SetPlaybackMode(StoryPlaybackMode mode)
        {
            PlaybackMode = mode;
        }

        // 覆盖界面与剧情主体 UI 分开记录，菜单打开时仍可保留底层剧情画面。
        public void SetOverlayOpen(bool isOpen)
        {
            IsOverlayOpen = isOpen;
        }

        public void SetStoryUiVisible(bool visible)
        {
            IsStoryUiVisible = visible;
        }

        // 背景和当前说话者属于当前剧情会话的演出状态。
        public void SetBackground(string backgroundId)
        {
            CurrentBackgroundId = backgroundId ?? string.Empty;
        }

        public void SetActiveSpeaker(string characterId)
        {
            ActiveSpeakerId = characterId ?? string.Empty;
        }

        // 定时等待始终限制为非负值，并单独记录能否被跳过。
        public void SetWait(float seconds, bool skippable)
        {
            WaitRemaining = Math.Max(0f, seconds);
            WaitSkippable = skippable;
        }

        // 按帧消费剩余等待时间，不允许负 deltaTime 反向增加计时。
        public void ConsumeWait(float deltaTime)
        {
            WaitRemaining = Math.Max(0f, WaitRemaining - Math.Max(0f, deltaTime));
        }

        // 跳过或等待完成时立即清零剩余时间。
        public void CompleteWait()
        {
            WaitRemaining = 0f;
        }

        // 显示立绘时复用已有角色状态，仅用非空命令字段覆盖位置和表情。
        public void ShowPortrait(StoryPortraitCommandData command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.characterId))
            {
                return;
            }

            StoryPortraitStateData portrait;

            if (!portraits.TryGetValue(command.characterId, out portrait))
            {
                portrait = new StoryPortraitStateData
                {
                    characterId = command.characterId
                };
                portraits.Add(command.characterId, portrait);
            }

            portrait.visible = true;

            if (!string.IsNullOrWhiteSpace(command.positionId))
            {
                portrait.positionId = command.positionId;
            }

            if (!string.IsNullOrWhiteSpace(command.expressionId))
            {
                portrait.expressionId = command.expressionId;
            }

            portrait.brightness = command.brightness <= 0f ? 1f : command.brightness;
            portrait.scale = command.scale <= 0f ? 1f : command.scale;
        }

        // 隐藏立绘但保留其状态，以便后续再次显示时继续使用已有配置。
        public void HidePortrait(string characterId)
        {
            StoryPortraitStateData portrait;

            if (string.IsNullOrWhiteSpace(characterId) ||
                !portraits.TryGetValue(characterId, out portrait))
            {
                return;
            }

            portrait.visible = false;

            if (ActiveSpeakerId == characterId)
            {
                ActiveSpeakerId = string.Empty;
            }
        }

        // 只修改已经存在的角色立绘，避免单独的表情节点隐式创建角色。
        public void ChangeExpression(string characterId, string expressionId)
        {
            StoryPortraitStateData portrait;

            if (string.IsNullOrWhiteSpace(characterId) ||
                !portraits.TryGetValue(characterId, out portrait))
            {
                return;
            }

            portrait.expressionId = expressionId;
        }

        // 返回排序后的深拷贝，保证视图无法修改内部立绘状态且显示顺序稳定。
        public List<StoryPortraitStateData> CreatePortraitSnapshot()
        {
            List<StoryPortraitStateData> snapshot = new List<StoryPortraitStateData>();

            foreach (StoryPortraitStateData portrait in portraits.Values)
            {
                snapshot.Add(portrait.Clone());
            }

            snapshot.Sort(ComparePortraits);
            return snapshot;
        }

        // 先按位置、再按角色 ID 排序，使 UI 得到确定性顺序。
        private static int ComparePortraits(
            StoryPortraitStateData left,
            StoryPortraitStateData right
        )
        {
            int positionComparison = string.Compare(
                left.positionId,
                right.positionId,
                StringComparison.Ordinal
            );

            if (positionComparison != 0)
            {
                return positionComparison;
            }

            return string.Compare(left.characterId, right.characterId, StringComparison.Ordinal);
        }
    }
}
