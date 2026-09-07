using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectGuilt.Story
{
    // 负责当前会话的逐字显示进度，不直接依赖 Unity UI 或 Time。
    public sealed class StoryTextPresenter
    {
        private const string PauseMarker = "||";
        private const float InlinePauseSeconds = 0.65f;

        // 使用浮点累计保留不足一个字符的帧间进度，避免低帧率下丢失速度。
        private float visibleCharacterProgress;
        private readonly List<int> inlinePausePositions = new List<int>();
        private int nextInlinePauseIndex;
        private float inlinePauseRemaining;

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
            get
            {
                return VisibleCharacterCount < FullText.Length ||
                    inlinePauseRemaining > 0f ||
                    HasPauseAtCurrentPosition();
            }
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
            FullText = ParseDisplayText(
                dialogue != null ? dialogue.text ?? string.Empty : string.Empty
            );
            VisibleCharacterCount = 0;
            visibleCharacterProgress = 0f;
            nextInlinePauseIndex = 0;
            inlinePauseRemaining = 0f;
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

            if (inlinePauseRemaining > 0f)
            {
                inlinePauseRemaining = Math.Max(
                    0f,
                    inlinePauseRemaining - Math.Max(0f, deltaTime)
                );
                return false;
            }

            if (HasPauseAtCurrentPosition())
            {
                nextInlinePauseIndex++;
                inlinePauseRemaining = InlinePauseSeconds;
                return false;
            }

            int previousCount = VisibleCharacterCount;
            visibleCharacterProgress += Math.Max(0f, deltaTime) * Math.Max(1f, CharactersPerSecond);
            int targetCount = Math.Min(
                FullText.Length,
                (int)Math.Floor(visibleCharacterProgress)
            );

            if (nextInlinePauseIndex < inlinePausePositions.Count &&
                inlinePausePositions[nextInlinePauseIndex] <= targetCount)
            {
                VisibleCharacterCount = inlinePausePositions[nextInlinePauseIndex];
                visibleCharacterProgress = VisibleCharacterCount;
                nextInlinePauseIndex++;
                inlinePauseRemaining = InlinePauseSeconds;
            }
            else
            {
                VisibleCharacterCount = targetCount;
            }

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
            nextInlinePauseIndex = inlinePausePositions.Count;
            inlinePauseRemaining = 0f;
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
            inlinePausePositions.Clear();
            nextInlinePauseIndex = 0;
            inlinePauseRemaining = 0f;
            AutoDelayOverride = -1f;
            Skippable = true;
        }

        // “||” 是仅影响逐字节奏的隐藏标记，不进入 UI 文本和历史记录。
        private string ParseDisplayText(string sourceText)
        {
            inlinePausePositions.Clear();

            if (string.IsNullOrEmpty(sourceText))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(sourceText.Length);

            for (int index = 0; index < sourceText.Length; index++)
            {
                bool isPauseMarker = index + 1 < sourceText.Length &&
                    sourceText[index] == PauseMarker[0] &&
                    sourceText[index + 1] == PauseMarker[1];

                if (isPauseMarker)
                {
                    inlinePausePositions.Add(builder.Length);
                    index++;
                    continue;
                }

                builder.Append(sourceText[index]);
            }

            return builder.ToString();
        }

        private bool HasPauseAtCurrentPosition()
        {
            return nextInlinePauseIndex < inlinePausePositions.Count &&
                inlinePausePositions[nextInlinePauseIndex] <= VisibleCharacterCount;
        }
    }
}
