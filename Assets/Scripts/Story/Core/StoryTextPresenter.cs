using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectGuilt.Story
{
    // 负责当前会话的逐字显示进度，不直接依赖 Unity UI 或 Time。
    public sealed class StoryTextPresenter
    {
        // 使用浮点累计保留不足一个字符的帧间进度，避免低帧率下丢失速度。
        private float visibleCharacterProgress;
        private readonly List<int> inlinePausePositions = new List<int>();
        private readonly List<StoryTextColorSpan> colorSpans =
            new List<StoryTextColorSpan>();
        private readonly List<StoryTimedPause> timedPauses =
            new List<StoryTimedPause>();
        private int nextInlinePauseIndex;
        private int nextTimedPauseIndex;
        private float timedPauseRemaining;

        public float CharactersPerSecond { get; set; }
        public string NodeId { get; private set; }
        public string SpeakerId { get; private set; }
        public string SpeakerName { get; private set; }
        public StoryDialoguePresentationMode PresentationMode { get; private set; }
        public StoryCenterScreenStyleData CenterScreenStyle { get; private set; }
        public float CharactersPerSecondOverride { get; private set; }
        public string TypingAudioId { get; private set; }
        public float TypingAudioVolume { get; private set; }
        public string FullText { get; private set; }
        public int VisibleCharacterCount { get; private set; }
        public float AutoDelayOverride { get; private set; }
        public bool Skippable { get; private set; }

        public string VisibleRichText
        {
            get { return BuildVisibleRichText(); }
        }

        public bool IsTyping
        {
            get
            {
                return VisibleCharacterCount < FullText.Length ||
                    HasPauseAtCurrentPosition() ||
                    HasTimedPauseAtCurrentPosition() ||
                    IsWaitingForTimedPause;
            }
        }

        // || 停在当前显示位置时，必须由玩家点击继续，不能由自动播放或快进越过。
        public bool IsWaitingForInlinePause
        {
            get { return HasPauseAtCurrentPosition(); }
        }

        public int CurrentInlinePauseIndex
        {
            get { return IsWaitingForInlinePause ? nextInlinePauseIndex : -1; }
        }

        public bool IsWaitingForTimedPause
        {
            get { return timedPauseRemaining > 0f; }
        }

        public float TimedPauseRemaining
        {
            get { return timedPauseRemaining; }
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
            PresentationMode = dialogue != null
                ? dialogue.presentationMode
                : StoryDialoguePresentationMode.Auto;
            CenterScreenStyle = dialogue != null ? dialogue.centerScreenStyle : null;
            CharactersPerSecondOverride = dialogue != null
                ? dialogue.charactersPerSecondOverride
                : -1f;
            TypingAudioId = dialogue != null
                ? dialogue.typingAudioId ?? string.Empty
                : string.Empty;
            TypingAudioVolume = dialogue != null
                ? dialogue.typingAudioVolume
                : 1f;
            string sourceText = dialogue != null ? dialogue.text ?? string.Empty : string.Empty;
            StoryTextMarkupResult markup;
            string errorMessage;

            if (!StoryTextMarkupParser.TryParse(sourceText, out markup, out errorMessage))
            {
                markup = StoryTextMarkupParser.CreateLegacyFallback(sourceText);
            }

            FullText = markup.FullText;
            inlinePausePositions.Clear();
            inlinePausePositions.AddRange(markup.InlinePausePositions);
            colorSpans.Clear();
            colorSpans.AddRange(markup.ColorSpans);
            timedPauses.Clear();
            timedPauses.AddRange(markup.TimedPauses);
            bool instantReveal = dialogue != null && dialogue.instantReveal;
            VisibleCharacterCount = instantReveal ? FullText.Length : 0;
            visibleCharacterProgress = VisibleCharacterCount;
            nextInlinePauseIndex = 0;
            nextTimedPauseIndex = 0;
            timedPauseRemaining = 0f;
            AutoDelayOverride = dialogue != null ? dialogue.autoDelayOverride : -1f;
            Skippable = dialogue == null || dialogue.skippable;
        }

        // 推进逐字显示；仅在可见字符数发生变化时返回 true，减少无效 UI 刷新。
        public bool Tick(float deltaTime)
        {
            if (IsWaitingForTimedPause)
            {
                timedPauseRemaining = Math.Max(
                    0f,
                    timedPauseRemaining - Math.Max(0f, deltaTime)
                );

                if (timedPauseRemaining <= 0f)
                {
                    nextTimedPauseIndex++;
                }

                return false;
            }

            if (!IsTyping)
            {
                return false;
            }

            if (HasPauseAtCurrentPosition())
            {
                return false;
            }

            if (StartTimedPauseIfNeeded())
            {
                return false;
            }

            int previousCount = VisibleCharacterCount;
            visibleCharacterProgress += Math.Max(0f, deltaTime) *
                GetEffectiveCharactersPerSecond();
            int targetCount = Math.Min(
                FullText.Length,
                (int)Math.Floor(visibleCharacterProgress)
            );

            int nextControlPosition = targetCount;

            if (nextInlinePauseIndex < inlinePausePositions.Count)
            {
                nextControlPosition = Math.Min(
                    nextControlPosition,
                    inlinePausePositions[nextInlinePauseIndex]
                );
            }

            if (nextTimedPauseIndex < timedPauses.Count)
            {
                nextControlPosition = Math.Min(
                    nextControlPosition,
                    timedPauses[nextTimedPauseIndex].Position
                );
            }

            if (nextControlPosition < targetCount)
            {
                VisibleCharacterCount = nextControlPosition;
                visibleCharacterProgress = VisibleCharacterCount;
            }
            else
            {
                VisibleCharacterCount = targetCount;
            }

            if (VisibleCharacterCount ==
                (nextTimedPauseIndex < timedPauses.Count
                    ? timedPauses[nextTimedPauseIndex].Position
                    : -1))
            {
                StartTimedPauseIfNeeded();
            }

            return VisibleCharacterCount != previousCount;
        }

        // 玩家点击或快进时立即显示到下一个 || 之前，停顿本身仍需玩家再次点击。
        public bool CompleteImmediately()
        {
            if (!IsTyping)
            {
                return false;
            }

            if (HasPauseAtCurrentPosition())
            {
                return false;
            }

            if (StartTimedPauseIfNeeded())
            {
                return false;
            }

            int targetCount = FullText.Length;

            if (nextInlinePauseIndex < inlinePausePositions.Count)
            {
                targetCount = inlinePausePositions[nextInlinePauseIndex];
            }

            if (nextTimedPauseIndex < timedPauses.Count)
            {
                targetCount = Math.Min(targetCount, timedPauses[nextTimedPauseIndex].Position);
            }

            bool changed = VisibleCharacterCount != targetCount;
            VisibleCharacterCount = targetCount;
            visibleCharacterProgress = targetCount;

            if (nextTimedPauseIndex < timedPauses.Count &&
                timedPauses[nextTimedPauseIndex].Position == VisibleCharacterCount)
            {
                StartTimedPauseIfNeeded();
            }

            return changed;
        }

        // 只有普通玩家点击可以解除 || 的断句；Auto 与 Skip 不调用此方法。
        public bool ResumeInlinePause()
        {
            if (IsWaitingForTimedPause)
            {
                return false;
            }

            if (!HasPauseAtCurrentPosition())
            {
                return false;
            }

            nextInlinePauseIndex++;
            visibleCharacterProgress = VisibleCharacterCount;
            return true;
        }

        // 清除对白内容并恢复安全默认值。
        public void Clear()
        {
            NodeId = string.Empty;
            SpeakerId = string.Empty;
            SpeakerName = string.Empty;
            PresentationMode = StoryDialoguePresentationMode.Auto;
            CenterScreenStyle = null;
            CharactersPerSecondOverride = -1f;
            TypingAudioId = string.Empty;
            TypingAudioVolume = 1f;
            FullText = string.Empty;
            VisibleCharacterCount = 0;
            visibleCharacterProgress = 0f;
            inlinePausePositions.Clear();
            colorSpans.Clear();
            timedPauses.Clear();
            nextInlinePauseIndex = 0;
            nextTimedPauseIndex = 0;
            timedPauseRemaining = 0f;
            AutoDelayOverride = -1f;
            Skippable = true;
        }

        private bool StartTimedPauseIfNeeded()
        {
            while (nextTimedPauseIndex < timedPauses.Count &&
                timedPauses[nextTimedPauseIndex].Position <= VisibleCharacterCount)
            {
                StoryTimedPause timedPause = timedPauses[nextTimedPauseIndex];

                if (timedPause.Position < VisibleCharacterCount || timedPause.Seconds <= 0f)
                {
                    nextTimedPauseIndex++;
                    continue;
                }

                timedPauseRemaining = timedPause.Seconds;
                return true;
            }

            return false;
        }

        private bool HasTimedPauseAtCurrentPosition()
        {
            return nextTimedPauseIndex < timedPauses.Count &&
                timedPauses[nextTimedPauseIndex].Position <= VisibleCharacterCount;
        }

        private string BuildVisibleRichText()
        {
            int visibleCount = Math.Max(
                0,
                Math.Min(VisibleCharacterCount, FullText.Length)
            );

            if (visibleCount == 0)
            {
                return string.Empty;
            }

            if (colorSpans.Count == 0)
            {
                return FullText.Substring(0, visibleCount);
            }

            StringBuilder builder = new StringBuilder(visibleCount + 32);
            int cursor = 0;

            foreach (StoryTextColorSpan span in colorSpans)
            {
                if (span == null || span.Start >= visibleCount)
                {
                    break;
                }

                int start = Math.Max(cursor, span.Start);
                int end = Math.Min(visibleCount, span.Start + span.Length);

                if (start > cursor)
                {
                    builder.Append(FullText.Substring(cursor, start - cursor));
                }

                if (end <= start)
                {
                    continue;
                }

                builder.Append("<color=").Append(span.ColorHex).Append(">");
                builder.Append(FullText.Substring(start, end - start));
                builder.Append("</color>");
                cursor = end;
            }

            if (cursor < visibleCount)
            {
                builder.Append(FullText.Substring(cursor, visibleCount - cursor));
            }

            return builder.ToString();
        }

        private float GetEffectiveCharactersPerSecond()
        {
            if (CharactersPerSecondOverride > 0f &&
                !float.IsNaN(CharactersPerSecondOverride) &&
                !float.IsInfinity(CharactersPerSecondOverride))
            {
                return CharactersPerSecondOverride;
            }

            return Math.Max(1f, CharactersPerSecond);
        }

        private bool HasPauseAtCurrentPosition()
        {
            return nextInlinePauseIndex < inlinePausePositions.Count &&
                inlinePausePositions[nextInlinePauseIndex] <= VisibleCharacterCount;
        }

    }
}
