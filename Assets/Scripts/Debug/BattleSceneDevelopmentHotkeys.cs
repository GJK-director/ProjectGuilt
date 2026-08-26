#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// BattleScene开发期快捷键；正式Player Build不启用这些调试输入。
public sealed class BattleSceneDevelopmentHotkeys : MonoBehaviour
{
#if UNITY_EDITOR
    private bool isReloadingScene;

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (isReloadingScene || keyboard == null)
        {
            return;
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
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
            return;
        }

        if (keyboard.f8Key.wasPressedThisFrame)
        {
            BattleCameraDirector director =
                FindFirstObjectByType<BattleCameraDirector>();
            if (director == null)
            {
                Debug.LogWarning(
                    "Battle Intro重播失败：当前场景中没有BattleCameraDirector。",
                    this
                );
                return;
            }

            director.PlayBattleIntro();
            return;
        }

        if (keyboard.f9Key.wasPressedThisFrame)
        {
            BattleCameraDirector director =
                FindFirstObjectByType<BattleCameraDirector>();
            if (director == null)
            {
                Debug.LogWarning(
                    "Turn End Recovery重播失败：当前场景中没有BattleCameraDirector。",
                    this
                );
                return;
            }

            director.PlayTurnEndRecovery();
            return;
        }

        if (keyboard.f10Key.wasPressedThisFrame)
        {
            PlayTwoUnitFocusPreview();
        }
    }

    private void PlayTwoUnitFocusPreview()
    {
        BattleCameraDirector director =
            FindFirstObjectByType<BattleCameraDirector>();
        if (director == null)
        {
            Debug.LogWarning(
                "Two Unit Focus预览失败：当前场景中没有BattleCameraDirector。",
                this
            );
            return;
        }

        BattleUnitViewSpawner spawner =
            FindFirstObjectByType<BattleUnitViewSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning(
                "Two Unit Focus预览失败：当前场景中没有BattleUnitViewSpawner。",
                this
            );
            return;
        }

        BattleUnitViewHandle allyHandle = null;
        BattleUnitViewHandle enemyHandle = null;
        for (int i = 0; i < spawner.GeneratedHandles.Count; i++)
        {
            BattleUnitViewHandle handle = spawner.GeneratedHandles[i];
            if (handle == null || handle.WorldRoot == null)
            {
                continue;
            }

            if (allyHandle == null && handle.Camp == BattleUnitCamp.Ally)
            {
                allyHandle = handle;
            }
            else if (enemyHandle == null &&
                handle.Camp == BattleUnitCamp.Enemy)
            {
                enemyHandle = handle;
            }

            if (allyHandle != null && enemyHandle != null)
            {
                break;
            }
        }

        if (allyHandle == null || enemyHandle == null)
        {
            Debug.LogWarning(
                "Two Unit Focus预览失败：找不到有效的友方/敌方单位。",
                this
            );
            return;
        }

        if (!director.TryPlayTwoUnitFocus(
                allyHandle,
                enemyHandle,
                true))
        {
            Debug.LogWarning(
                "Two Unit Focus预览未启动：当前已有Camera Presentation运行。",
                this
            );
        }
    }
#endif
}
