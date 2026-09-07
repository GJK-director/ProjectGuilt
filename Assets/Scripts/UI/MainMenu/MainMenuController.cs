using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private CanvasGroup mainMenuCanvasGroup;
    [SerializeField] private Button newGameButton;
    [SerializeField] private string newGameSceneName = "NewGameText";

    private bool isLoadingNewGame;

    void Awake()
    {
        // 第一版只初始化两个面板的显隐和主菜单交互状态，不自动查找场景对象。
        if (!HasPanelReferences("Awake"))
        {
            return;
        }

        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        SetMainMenuInteraction(true);
    }

    public void StartNewGame()
    {
        if (isLoadingNewGame)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(newGameSceneName))
        {
            Debug.LogError("开始新游戏失败：newGameSceneName 为空，请在 MainMenuController Inspector 中填写目标场景名称。");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(newGameSceneName))
        {
            Debug.LogError(
                "开始新游戏失败：场景 " +
                newGameSceneName +
                " 无法加载，请检查 Build Profile 的 Scene List 是否包含该场景。"
            );
            return;
        }

        isLoadingNewGame = true;

        if (newGameButton != null)
        {
            newGameButton.interactable = false;
        }
        else
        {
            Debug.LogError("开始新游戏提示：newGameButton 未绑定，无法在加载时禁用按钮。");
        }

        StartCoroutine(LoadNewGameSceneAsync());
    }

    public void OpenSettings()
    {
        if (!HasPanelReferences("OpenSettings"))
        {
            return;
        }

        mainMenuPanel.SetActive(true);
        SetMainMenuInteraction(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (!HasPanelReferences("CloseSettings"))
        {
            return;
        }

        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        SetMainMenuInteraction(true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        // Editor 中停止 Play Mode，方便主菜单按钮测试。
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator LoadNewGameSceneAsync()
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(newGameSceneName, LoadSceneMode.Single);

        if (loadOperation == null)
        {
            Debug.LogError("开始新游戏失败：SceneManager.LoadSceneAsync 返回空，场景名称：" + newGameSceneName);
            isLoadingNewGame = false;

            if (newGameButton != null)
            {
                newGameButton.interactable = true;
            }

            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }
    }

    void SetMainMenuInteraction(bool enabled)
    {
        if (mainMenuCanvasGroup == null)
        {
            Debug.LogError("设置主菜单交互失败：mainMenuCanvasGroup 未绑定。");
            return;
        }

        // 设置面板打开时主菜单保持可见，只关闭按钮交互和射线阻挡。
        mainMenuCanvasGroup.alpha = 1f;
        mainMenuCanvasGroup.interactable = enabled;
        mainMenuCanvasGroup.blocksRaycasts = enabled;
    }

    bool HasPanelReferences(string callerName)
    {
        bool hasReferences = true;

        if (mainMenuPanel == null)
        {
            Debug.LogError(callerName + " 失败：mainMenuPanel 未绑定。");
            hasReferences = false;
        }

        if (settingsPanel == null)
        {
            Debug.LogError(callerName + " 失败：settingsPanel 未绑定。");
            hasReferences = false;
        }

        if (mainMenuCanvasGroup == null)
        {
            Debug.LogError(callerName + " 失败：mainMenuCanvasGroup 未绑定。");
            hasReferences = false;
        }

        return hasReferences;
    }
}
