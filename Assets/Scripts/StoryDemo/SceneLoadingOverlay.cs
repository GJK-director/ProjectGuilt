using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 在当前场景上方显示全屏加载页，并在异步加载完成后激活目标场景。
/// </summary>
[DisallowMultipleComponent]
public sealed class SceneLoadingOverlay : MonoBehaviour
{
    [SerializeField] private GameObject overlayRoot = null;
    [SerializeField] private Image progressFill = null;
    [SerializeField] private Text progressText = null;
    [SerializeField] private float minimumVisibleSeconds = 0.8f;

    private bool isLoading;

    public bool IsLoading
    {
        get { return isLoading; }
    }

    private void Awake()
    {
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }

        SetProgress(0f);
    }

    public bool BeginLoad(string sceneName)
    {
        if (isLoading)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(sceneName) ||
            !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "加载战斗场景失败：请检查场景名称和 Build Profile，sceneName=" +
                sceneName
            );
            return false;
        }

        isLoading = true;

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
        }

        SetProgress(0f);
        StartCoroutine(LoadSceneAsync(sceneName));
        return true;
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(
            sceneName,
            LoadSceneMode.Single
        );

        if (operation == null)
        {
            Debug.LogError("加载战斗场景失败：LoadSceneAsync 返回空。" + sceneName);
            isLoading = false;
            yield break;
        }

        operation.allowSceneActivation = false;
        float elapsed = 0f;
        float displayedProgress = 0f;
        float safeMinimumDuration = Mathf.Max(0f, minimumVisibleSeconds);

        while (operation.progress < 0.9f || elapsed < safeMinimumDuration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            elapsed += deltaTime;

            float loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float timeProgress = safeMinimumDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / safeMinimumDuration);
            float targetProgress = Mathf.Min(loadProgress, timeProgress);
            displayedProgress = Mathf.MoveTowards(
                displayedProgress,
                targetProgress,
                deltaTime * 1.5f
            );
            SetProgress(displayedProgress);
            yield return null;
        }

        SetProgress(1f);
        yield return new WaitForSecondsRealtime(0.15f);
        operation.allowSceneActivation = true;
    }

    private void SetProgress(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);

        if (progressFill != null)
        {
            progressFill.fillAmount = clampedProgress;
            RectTransform fillRect = progressFill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(clampedProgress, 1f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;
        }

        if (progressText != null)
        {
            progressText.text = "加载中  " +
                Mathf.RoundToInt(clampedProgress * 100f) + "%";
        }
    }
}
