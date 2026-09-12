using ProjectGuilt.Story;
using UnityEngine;

/// <summary>
/// StoryTest 场景使用的测试宿主。
/// 用来模拟战斗、关卡等正式宿主流程在合适时机触发剧情。
/// </summary>
[DisallowMultipleComponent]
public sealed class StoryTestHost : MonoBehaviour
{
    [Header("测试宿主触发")]
    [SerializeField] private StorySceneFacade storyFacade = null;
    [SerializeField] private string storyId = "story_test_demo";
    [SerializeField] private bool triggerOnStart = true;

    private void Start()
    {
        if (triggerOnStart)
        {
            TriggerStory();
        }
    }

    /// <summary>
    /// 宿主只负责给出 storyId；面板显示、按钮交互和剧情启动均由 Story 内部完成。
    /// </summary>
    [ContextMenu("测试宿主/触发剧情")]
    public void TriggerStory()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("StoryTestHost：请在 Play 模式下触发测试剧情。");
            return;
        }

        if (storyFacade == null)
        {
            Debug.LogError("StoryTestHost：未绑定 StorySceneFacade。");
            return;
        }

        if (!storyFacade.OpenStoryPanel(storyId))
        {
            Debug.LogError("StoryTestHost：打开剧情面板失败，storyId=" + storyId);
        }
    }

    /// <summary>
    /// 宿主需要中断剧情或切换场景时，直接关闭 Story 面板。
    /// </summary>
    [ContextMenu("测试宿主/关闭剧情面板")]
    public void CloseStoryPanel()
    {
        if (storyFacade != null)
        {
            storyFacade.CloseStoryPanel();
        }
    }
}
