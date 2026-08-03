using System.Text;
using UnityEngine;

namespace ProjectGuilt.Story
{
    // 默认约定：Assets/Resources/Story/{storyId}.json
    public sealed class ResourcesStoryContentProvider : IStoryContentProvider
    {
        private readonly string resourcesFolder;

        // 清理首尾斜杠；空配置回退到默认 Story 目录。
        public ResourcesStoryContentProvider(string resourcesFolder)
        {
            this.resourcesFolder = string.IsNullOrWhiteSpace(resourcesFolder)
                ? "Story"
                : resourcesFolder.Trim().Trim('/');
        }

        // Unity Resources.Load 不需要扩展名，因此路径固定为 {folder}/{storyId}。
        public bool TryGetStoryJson(
            string storyId,
            out string storyJson,
            out string errorMessage
        )
        {
            storyJson = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(storyId))
            {
                errorMessage = "storyId 为空";
                return false;
            }

            string resourcePath = resourcesFolder + "/" + storyId;
            TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);

            if (textAsset == null)
            {
                errorMessage = "找不到剧情 JSON：Assets/Resources/" + resourcePath + ".json";
                return false;
            }

            storyJson = Encoding.UTF8.GetString(textAsset.bytes);
            return true;
        }
    }
}
