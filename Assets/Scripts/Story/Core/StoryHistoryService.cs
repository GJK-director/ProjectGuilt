using System;
using System.Collections.Generic;

namespace ProjectGuilt.Story
{
    // 默认时钟以 ISO 8601 格式记录 UTC 时间，为当前会话历史提供稳定时间戳。
    public sealed class SystemStoryClock : IStoryClock
    {
        public string GetUtcTimestamp()
        {
            return DateTime.UtcNow.ToString("O");
        }
    }

    // 维护当前剧情会话的对话与选项历史，并负责只读副本和容量裁剪。
    public sealed class StoryHistoryService
    {
        private readonly List<StoryHistoryEntryData> entries =
            new List<StoryHistoryEntryData>();
        private readonly IStoryClock clock;
        private readonly int maxEntries;
        private int nextSequence = 1;

        public event Action<StoryHistoryEntryData> EntryAdded;

        // 时钟可由外部注入，便于测试时提供可预测的时间戳。
        public StoryHistoryService(IStoryClock clock, int maxEntries)
        {
            this.clock = clock ?? new SystemStoryClock();
            this.maxEntries = Math.Max(1, maxEntries);
        }

        // 开始全新剧情时清空历史，并重新从序号 1 计数。
        public void Clear()
        {
            entries.Clear();
            nextSequence = 1;
        }

        // 记录一条已经进入展示流程的角色对白。
        public void AddDialogue(
            string nodeId,
            string speakerId,
            string speakerName,
            string text
        )
        {
            AddEntry("Dialogue", nodeId, speakerId, speakerName, text);
        }

        // 记录玩家实际提交的选项，而不是仅记录曾经显示过的选项。
        public void AddChoice(string nodeId, string optionText)
        {
            AddEntry("Choice", nodeId, string.Empty, "选择", optionText);
        }

        // 返回深拷贝，防止 UI 或外部代码意外修改内部历史。
        public List<StoryHistoryEntryData> CreateSnapshot()
        {
            List<StoryHistoryEntryData> snapshot = new List<StoryHistoryEntryData>();

            foreach (StoryHistoryEntryData entry in entries)
            {
                snapshot.Add(entry.Clone());
            }

            return snapshot;
        }

        // 为记录统一补齐序号和时间戳，再通过事件向 UI 发布安全副本。
        private void AddEntry(
            string entryType,
            string nodeId,
            string speakerId,
            string speakerName,
            string text
        )
        {
            StoryHistoryEntryData entry = new StoryHistoryEntryData
            {
                sequence = nextSequence++,
                timestampUtc = clock.GetUtcTimestamp(),
                entryType = entryType,
                nodeId = nodeId,
                speakerId = speakerId,
                speakerName = speakerName,
                text = text
            };

            entries.Add(entry);
            TrimToLimit();

            if (EntryAdded != null)
            {
                EntryAdded(entry.Clone());
            }
        }

        // 超过容量时优先移除最早的记录，保留最近的剧情内容。
        private void TrimToLimit()
        {
            int excessCount = entries.Count - maxEntries;

            if (excessCount > 0)
            {
                entries.RemoveRange(0, excessCount);
            }
        }
    }
}
