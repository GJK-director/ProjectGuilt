using System;

namespace ProjectGuilt.Story
{
    // 负责当前会话的逐字显示进度，不直接依赖 Unity UI 或 Time。
    public sealed class StoryTextPresenter
    {
        // 使用浮点累计保留不足一个字符的帧间进度，避免低帧率下丢失速度。
        private float visibleCharacterProgress;

        public float CharactersPerSecond { get; set; }
        public string NodeId { get; private set; }
        public string SpeakerId { get; private set; }
        public string SpeakerName { get; private set; }
        public string FullText { get; private set; }
        public int VisibleCharacterCount { get; private set; }
        public float AutoDelayOverride { get; private set; }
        public bool Skippable { get; private set; }

        public bool IsTyping
        {
            get { return VisibleCharacterCount < FullText.Length; }
        }

        // 最低速度限制为每秒一个字符，防止配置为零后对白永久停住。
        public StoryTextPresenter(float charactersPerSecond)
        {
            CharactersPerSecond = Math.Max(1f, charactersPerSecond);
            Clear();
        }

        // 载入一条新对白，并从零开始逐字显示。
        public void Begin(string nodeId, StoryDialogueData dialogue)
        {
            NodeId = nodeId ?? string.Empty;
            SpeakerId = dialogue != null ? dialogue.speakerId ?? string.Empty : string.Empty;
            SpeakerName = dialogue != null ? dialogue.speakerName ?? string.Empty : string.Empty;
            FullText = dialogue != null ? dialogue.text ?? string.Empty : string.Empty;
            VisibleCharacterCount = 0;
            visibleCharacterProgress = 0f;
            AutoDelayOverride = dialogue != null ? dialogue.autoDelayOverride : -1f;
            Skippable = dialogue == null || dialogue.skippable;
        }

        // 推进逐字显示；仅在可见字符数发生变化时返回 true，减少无效 UI 刷新。
        public bool Tick(float deltaTime)
        {
            if (!IsTyping)
            {
                return false;
            }

            int previousCount = VisibleCharacterCount;
            visibleCharacterProgress += Math.Max(0f, deltaTime) * Math.Max(1f, CharactersPerSecond);
            VisibleCharacterCount = Math.Min(
                FullText.Length,
                (int)Math.Floor(visibleCharacterProgress)
            );
            return VisibleCharacterCount != previousCount;
        }

        // 玩家点击或跳过时立即显示整条文本，并报告本次是否确实发生变化。
        public bool CompleteImmediately()
        {
            if (!IsTyping)
            {
                return false;
            }

            VisibleCharacterCount = FullText.Length;
            visibleCharacterProgress = FullText.Length;
            return true;
        }

        // 清除对白内容并恢复安全默认值。
        public void Clear()
        {
            NodeId = string.Empty;
            SpeakerId = string.Empty;
            SpeakerName = string.Empty;
            FullText = string.Empty;
            VisibleCharacterCount = 0;
            visibleCharacterProgress = 0f;
            AutoDelayOverride = -1f;
            Skippable = true;
        }
    }
}
