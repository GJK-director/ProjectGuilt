using System.Collections.Generic;

namespace ProjectGuilt.Story
{
    // 没有绑定正式 UI 时仍可运行逻辑和测试数据，不产生空引用。
    internal sealed class NullStoryView : IStoryView
    {
        // 所有方法均为空实现，用于无界面测试和宿主尚未完成 UI 接入的阶段。
        public void SetStoryVisible(bool visible) { }
        public void SetStoryUiVisible(bool visible) { }
        public void SetOverlayOpen(bool isOpen) { }
        public void SetPlaybackMode(StoryPlaybackMode mode) { }
        public void SetContinueIndicator(bool visible) { }

        public void ShowDialogue(
            string speakerName,
            string fullText,
            int visibleCharacterCount,
            bool isComplete
        )
        {
        }

        public void ShowChoices(IReadOnlyList<StoryChoiceViewData> choices) { }
        public void HideChoices() { }
        public void SetBackground(string backgroundId, float fadeSeconds) { }

        public void ApplyPortraits(
            IReadOnlyList<StoryPortraitStateData> portraits,
            string activeSpeakerId
        )
        {
        }

        public void NotifyStoryEnded(string storyId) { }
    }
}
