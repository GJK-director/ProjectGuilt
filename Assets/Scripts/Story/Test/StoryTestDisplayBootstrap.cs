using UnityEngine;

namespace ProjectGuilt.Story
{
    // StoryTest 专用显示启动保证，不参与正式游戏启动流程。
    [DisallowMultipleComponent]
    public sealed class StoryTestDisplayBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Resolution resolution = Screen.currentResolution;
            Screen.SetResolution(
                resolution.width,
                resolution.height,
                FullScreenMode.FullScreenWindow
            );
        }
    }
}
