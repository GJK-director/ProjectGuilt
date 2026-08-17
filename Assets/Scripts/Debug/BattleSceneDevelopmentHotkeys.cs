#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// BattleScene开发期快捷键；正式Player Build不启用场景重载输入。
public sealed class BattleSceneDevelopmentHotkeys : MonoBehaviour
{
#if UNITY_EDITOR
    private bool isReloadingScene;

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (isReloadingScene || keyboard == null ||
            !keyboard.rKey.wasPressedThisFrame)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || string.IsNullOrEmpty(activeScene.path))
        {
            Debug.LogWarning(
                "BattleScene开发重载失败：当前场景没有可用的资源路径。",
                this
            );
            return;
        }

        // 重载已保存场景，统一重建Runtime、表现和UI状态。
        isReloadingScene = true;
        EditorSceneManager.LoadSceneInPlayMode(
            activeScene.path,
            new LoadSceneParameters(LoadSceneMode.Single)
        );
    }
#endif
}
