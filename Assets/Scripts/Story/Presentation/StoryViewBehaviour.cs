using System.Collections.Generic;
using UnityEngine;

namespace ProjectGuilt.Story
{
    // 内置 StoryPanelView 已提供完整 UGUI；只有需要完全替换表现层时才继承这个类。
    public abstract class StoryViewBehaviour : MonoBehaviour, IStoryView
    {
        // 控制剧情根界面、文本主体和菜单覆盖层的独立显隐。
        public abstract void SetStoryVisible(bool visible);
        public abstract void SetStoryUiVisible(bool visible);
        public abstract void SetOverlayOpen(bool isOpen);

        // 同步自动、快进等播放模式及文本继续提示。
        public abstract void SetPlaybackMode(StoryPlaybackMode mode);
        public abstract void SetContinueIndicator(bool visible);

        // fullText 始终是完整文本，视图根据 visibleCharacterCount 决定当前显示范围。
        public abstract void ShowDialogue(
            string speakerName,
            string fullText,
            int visibleCharacterCount,
            bool isComplete
        );

        // 选项的 interactable 已由核心层计算，视图只负责呈现和转发点击。
        public abstract void ShowChoices(IReadOnlyList<StoryChoiceViewData> choices);
        public abstract void HideChoices();

        // 资源 ID 的解析和具体过渡动画由宿主项目的视图实现。
        public abstract void SetBackground(string backgroundId, float fadeSeconds);

        public abstract void ApplyPortraits(
            IReadOnlyList<StoryPortraitStateData> portraits,
            string activeSpeakerId
        );

        // 剧情逻辑结束后通知视图播放收尾效果或释放界面。
        public abstract void NotifyStoryEnded(string storyId);
    }
}
