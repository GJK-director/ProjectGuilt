using System;
using System.Globalization;
using ProjectGuilt.Story;
using UnityEngine;

/// <summary>
/// 新游戏序章的宿主：启动剧情、播放宿主音效，并在剧情结束后进入战斗。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class IntroStoryHost : MonoBehaviour
{
    private const string PlaySfxNodeType = "PlaySfx";
    private const string DoorKnockSfxId = "door_knock";

    [Header("剧情入口")]
    [SerializeField] private StorySceneFacade storyFacade = null;
    [SerializeField] private string storyId = "prologue_501";

    [Header("剧情结束后")]
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private SceneLoadingOverlay loadingOverlay = null;

    private AudioSource audioSource;
    private AudioClip doorKnockClip;
    private bool transitionStarted;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        doorKnockClip = CreateDoorKnockClip();

        if (storyFacade == null)
        {
            storyFacade = FindFirstObjectByType<StorySceneFacade>();
        }

        if (storyFacade == null)
        {
            Debug.LogError("序章启动失败：场景中没有 StorySceneFacade。");
            return;
        }

        // StoryPanel 的 Canvas 根必须保持激活；只由 StoryPanelView 控制内部
        // StoryPresentationRoot 的显隐。防止场景实例误留 inactive override 后黑屏。
        if (!storyFacade.gameObject.activeSelf)
        {
            Debug.LogWarning("检测到 StoryPanel 根对象未激活，已在序章启动时恢复。");
            storyFacade.gameObject.SetActive(true);
        }

        if (!storyFacade.gameObject.activeInHierarchy)
        {
            Debug.LogError("序章启动失败：StoryPanel 的父级对象未激活。");
            return;
        }

        storyFacade.RegisterNodeHandler(new PlaySfxNodeHandler(this));
        storyFacade.StoryEnded += HandleStoryEnded;
        storyFacade.StoryError += HandleStoryError;
    }

    private void Start()
    {
        if (storyFacade != null && !storyFacade.OpenStoryPanel(storyId))
        {
            Debug.LogError("序章启动失败：storyId=" + storyId);
        }
    }

    private void OnDestroy()
    {
        if (storyFacade != null)
        {
            storyFacade.StoryEnded -= HandleStoryEnded;
            storyFacade.StoryError -= HandleStoryError;
        }

        if (doorKnockClip != null)
        {
            Destroy(doorKnockClip);
        }
    }

    private void HandleStoryEnded(string endedStoryId)
    {
        if (transitionStarted ||
            !string.Equals(endedStoryId, storyId, StringComparison.Ordinal))
        {
            return;
        }

        transitionStarted = true;
        storyFacade.CloseStoryPanel();

        if (loadingOverlay == null || !loadingOverlay.BeginLoad(battleSceneName))
        {
            transitionStarted = false;
            Debug.LogError("序章结束后无法打开战斗加载页。");
        }
    }

    private void HandleStoryError(string message)
    {
        Debug.LogError("序章剧情错误：" + message);
    }

    private void PlayDoorKnock()
    {
        if (audioSource != null && doorKnockClip != null)
        {
            audioSource.PlayOneShot(doorKnockClip, 0.85f);
        }
    }

    // 当前没有单独的敲门音频素材，因此生成三个短促的木门敲击声作为测试占位。
    private static AudioClip CreateDoorKnockClip()
    {
        const int sampleRate = 44100;
        const float clipDuration = 0.82f;
        float[] knockTimes = { 0.02f, 0.27f, 0.51f };
        float[] samples = new float[Mathf.CeilToInt(sampleRate * clipDuration)];
        System.Random random = new System.Random(501);

        foreach (float knockTime in knockTimes)
        {
            int startSample = Mathf.RoundToInt(knockTime * sampleRate);
            int burstSamples = Mathf.RoundToInt(0.12f * sampleRate);

            for (int index = 0; index < burstSamples; index++)
            {
                int sampleIndex = startSample + index;

                if (sampleIndex >= samples.Length)
                {
                    break;
                }

                float time = index / (float)sampleRate;
                float attack = 1f - Mathf.Exp(-time * 300f);
                float decay = Mathf.Exp(-time * 35f);
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                float body =
                    Mathf.Sin(2f * Mathf.PI * 92f * time) * 0.58f +
                    Mathf.Sin(2f * Mathf.PI * 181f * time) * 0.24f +
                    noise * 0.18f;
                samples[sampleIndex] = Mathf.Clamp(
                    samples[sampleIndex] + body * attack * decay,
                    -1f,
                    1f
                );
            }
        }

        AudioClip clip = AudioClip.Create(
            "Generated_DoorKnock_501",
            samples.Length,
            1,
            sampleRate,
            false
        );
        clip.SetData(samples, 0);
        return clip;
    }

    private sealed class PlaySfxNodeHandler : IStoryNodeHandler
    {
        private readonly IntroStoryHost host;

        public string NodeType
        {
            get { return PlaySfxNodeType; }
        }

        public PlaySfxNodeHandler(IntroStoryHost host)
        {
            this.host = host;
        }

        public StoryNodeExecutionResult Execute(
            StoryNodeExecutionContext context,
            StoryNodeData node
        )
        {
            string sfxId;

            if (node.parameters == null ||
                !node.parameters.TryGetValue("sfxId", out sfxId))
            {
                return StoryNodeExecutionResult.Error(
                    node.nodeId + " 缺少 parameters.sfxId"
                );
            }

            if (!string.Equals(sfxId, DoorKnockSfxId, StringComparison.Ordinal))
            {
                return StoryNodeExecutionResult.Error(
                    node.nodeId + " 使用了未知音效：" + sfxId
                );
            }

            host.PlayDoorKnock();
            float waitSeconds = 0.82f;
            string waitValue;

            if (node.parameters.TryGetValue("waitSeconds", out waitValue))
            {
                float parsedWait;

                if (float.TryParse(
                    waitValue,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out parsedWait
                ))
                {
                    waitSeconds = Mathf.Max(0f, parsedWait);
                }
            }

            return StoryNodeExecutionResult.WaitForTime(
                node.nextNodeId,
                waitSeconds,
                true
            );
        }
    }
}
