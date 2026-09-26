using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ProjectGuilt.Story
{
    internal sealed class StoryTextColorSpan
    {
        public int Start;
        public int Length;
        public string ColorHex;
    }

    internal sealed class StoryTimedPause
    {
        public int Position;
        public float Seconds;
    }

    internal sealed class StoryTextMarkupResult
    {
        public string FullText = string.Empty;
        public readonly List<int> InlinePausePositions = new List<int>();
        public readonly List<StoryTextColorSpan> ColorSpans =
            new List<StoryTextColorSpan>();
        public readonly List<StoryTimedPause> TimedPauses =
            new List<StoryTimedPause>();
    }

    // Story 文本的作者标记由 Serializer 和逐字运行时共享解析，确保校验与表现语义一致。
    internal static class StoryTextMarkupParser
    {
        private const string PauseMarker = "||";
        private const string FullWidthPauseMarker = "｜｜";
        private const string WaitPrefix = "[[wait=";
        private const string ColorPrefix = "[color=";
        private const string ColorCloseTag = "[/color]";

        public static bool TryParse(
            string sourceText,
            out StoryTextMarkupResult result,
            out string errorMessage
        )
        {
            result = new StoryTextMarkupResult();
            errorMessage = string.Empty;
            string source = sourceText ?? string.Empty;
            StringBuilder visibleText = new StringBuilder(source.Length);
            int openColorStart = -1;
            string openColorHex = string.Empty;

            for (int index = 0; index < source.Length; index++)
            {
                if (IsPauseMarkerAt(source, index, PauseMarker) ||
                    IsPauseMarkerAt(source, index, FullWidthPauseMarker))
                {
                    result.InlinePausePositions.Add(visibleText.Length);
                    index++;
                    continue;
                }

                if (StartsWith(source, index, WaitPrefix))
                {
                    int closeIndex = source.IndexOf("]]", index + WaitPrefix.Length,
                        StringComparison.Ordinal);

                    if (closeIndex < 0)
                    {
                        return Fail(
                            result,
                            out errorMessage,
                            "定时暂停标记未闭合"
                        );
                    }

                    string value = source.Substring(
                        index + WaitPrefix.Length,
                        closeIndex - (index + WaitPrefix.Length)
                    );
                    float seconds;

                    if (string.IsNullOrWhiteSpace(value) ||
                        !float.TryParse(
                            value,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out seconds
                        ) ||
                        float.IsNaN(seconds) ||
                        float.IsInfinity(seconds) ||
                        seconds < 0f)
                    {
                        return Fail(
                            result,
                            out errorMessage,
                            "定时暂停秒数必须是大于等于 0 的有限数字"
                        );
                    }

                    result.TimedPauses.Add(new StoryTimedPause
                    {
                        Position = visibleText.Length,
                        Seconds = seconds
                    });
                    index = closeIndex + 1;
                    continue;
                }

                if (StartsWith(source, index, ColorCloseTag))
                {
                    if (openColorStart < 0)
                    {
                        return Fail(
                            result,
                            out errorMessage,
                            "出现未匹配的 [/color]"
                        );
                    }

                    result.ColorSpans.Add(new StoryTextColorSpan
                    {
                        Start = openColorStart,
                        Length = visibleText.Length - openColorStart,
                        ColorHex = openColorHex
                    });
                    openColorStart = -1;
                    openColorHex = string.Empty;
                    index += ColorCloseTag.Length - 1;
                    continue;
                }

                if (StartsWith(source, index, ColorPrefix))
                {
                    int closeIndex = source.IndexOf(']', index + ColorPrefix.Length);

                    if (closeIndex < 0)
                    {
                        return Fail(
                            result,
                            out errorMessage,
                            "颜色标签未闭合"
                        );
                    }

                    string colorHex = source.Substring(
                        index + ColorPrefix.Length,
                        closeIndex - (index + ColorPrefix.Length)
                    );

                    if (!IsValidColorHex(colorHex))
                    {
                        return Fail(
                            result,
                            out errorMessage,
                            "颜色标签必须使用 #RRGGBB 或 #RRGGBBAA"
                        );
                    }

                    if (openColorStart >= 0)
                    {
                        return Fail(
                            result,
                            out errorMessage,
                            "不支持嵌套颜色标签"
                        );
                    }

                    openColorStart = visibleText.Length;
                    openColorHex = colorHex;
                    index = closeIndex;
                    continue;
                }

                visibleText.Append(source[index]);
            }

            if (openColorStart >= 0)
            {
                return Fail(
                    result,
                    out errorMessage,
                    "颜色标签缺少 [/color]"
                );
            }

            foreach (StoryTimedPause timedPause in result.TimedPauses)
            {
                if (result.InlinePausePositions.Contains(timedPause.Position))
                {
                    return Fail(
                        result,
                        out errorMessage,
                        "定时暂停不能与 || / ｜｜ 位于同一个可见字符位置"
                    );
                }
            }

            result.FullText = visibleText.ToString();
            return true;
        }

        // 仅作为直接调用 DTO 时的防御性回退；正式 JSON 会先经过 TryParse 校验。
        public static StoryTextMarkupResult CreateLegacyFallback(string sourceText)
        {
            StoryTextMarkupResult result = new StoryTextMarkupResult();
            string source = sourceText ?? string.Empty;
            StringBuilder visibleText = new StringBuilder(source.Length);

            for (int index = 0; index < source.Length; index++)
            {
                if (IsPauseMarkerAt(source, index, PauseMarker) ||
                    IsPauseMarkerAt(source, index, FullWidthPauseMarker))
                {
                    result.InlinePausePositions.Add(visibleText.Length);
                    index++;
                    continue;
                }

                visibleText.Append(source[index]);
            }

            result.FullText = visibleText.ToString();
            return result;
        }

        private static bool Fail(
            StoryTextMarkupResult result,
            out string errorMessage,
            string message
        )
        {
            errorMessage = message;
            result = null;
            return false;
        }

        private static bool IsValidColorHex(string value)
        {
            if (string.IsNullOrEmpty(value) || value[0] != '#')
            {
                return false;
            }

            if (value.Length != 7 && value.Length != 9)
            {
                return false;
            }

            for (int index = 1; index < value.Length; index++)
            {
                char character = value[index];

                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f') ||
                      (character >= 'A' && character <= 'F')))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool StartsWith(string source, int index, string value)
        {
            return index + value.Length <= source.Length &&
                string.CompareOrdinal(source, index, value, 0, value.Length) == 0;
        }

        private static bool IsPauseMarkerAt(
            string source,
            int index,
            string marker
        )
        {
            return StartsWith(source, index, marker);
        }
    }
}
