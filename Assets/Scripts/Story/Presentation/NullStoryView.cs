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
        public void SetAdvanceInputEnabled(bool enabled) { }

        public void ShowDialogue(
            string speakerId,
            string speakerName,
            StoryDialoguePresentationMode presentationMode,
            StoryCenterScreenStyleData centerScreenStyle,
            string fullText,
            string visibleRichText,
            int visibleCharacterCount,
            bool isComplete
        )
        {
        }

        public void ClearDialoguePresentation() { }

        public void ShowChoices(IReadOnlyList<StoryChoiceViewData> choices) { }
        public void HideChoices() { }
        public void SetBackground(
            string backgroundId,
            float fadeSeconds,
            StoryBackgroundTransitionMode transitionMode,
            float fadeOutSeconds
        ) { }
        public void ShowForeground(
            string foregroundId,
            float fadeSeconds,
            float offsetX,
            float offsetY,
            float scale,
            bool flipX
        ) { }
        public bool PlaySfx(string sfxId, float volume, bool waitUntilComplete) { return true; }
        public bool PlaySfx(
            string sfxId,
            float volume,
            bool waitUntilComplete,
            StorySfxChannel channel
        ) { return true; }
        public bool IsStorySfxPlaying(string sfxId) { return false; }
        public bool IsStorySfxPlaying(string sfxId, StorySfxChannel channel) { return false; }
        public bool PlayAmbient(string audioId, float volume, bool loop, float fadeInSeconds) { return true; }
        public void StopAmbient() { }
        public bool StartTypingAudio(string audioId, float volume) { return true; }
        public void StopTypingAudio() { }
        public void FadeDialogue(float targetAlpha, float fadeSeconds) { }
        public void SetVisualFraming(float scale, float offsetX, float offsetY) { }
        public void FadeVisualToBlack(float fadeSeconds) { }
        public void ChangeBgm(string bgmId, float fadeOutSeconds, bool loop) { }

        public void ApplyPortraits(
            IReadOnlyList<StoryPortraitStateData> portraits,
            string activeSpeakerId
        )
        {
        }

        public void NotifyStoryEnded(string storyId) { }
    }
}
