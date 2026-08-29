using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 战斗终局提示面板。只读取 BattleRuntimeState，不参与胜负判定或战斗结算。
public sealed class BattleEndPanelController : MonoBehaviour
{
    private const int OverlaySortingOrder = 32767;
    private static readonly Vector2 ReferenceResolution =
        new Vector2(1920f, 1080f);

    private static BattleEndPanelController instance;

    [SerializeField] private string startSceneName = "Menu";
    [SerializeField] private float fadeInDuration = 0.2f;

    private BattleRuntimeState runtimeState;
    private Canvas overlayCanvas;
    private CanvasGroup panelCanvasGroup;
    private RectTransform safeAreaRoot;
    private TMP_Text resultTitleText;
    private TMP_Text resultBodyText;
    private TMP_Text returnButtonText;
    private Button returnButton;
    private Coroutine fadeCoroutine;
    private bool isVisible;
    private bool isLoadingStartScene;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    public bool IsVisible => isVisible;
    public bool IsLoadingStartScene => isLoadingStartScene;

    public static void Bind(BattleRuntimeState state)
    {
        if (state == null)
        {
            Debug.LogError("战斗结束面板绑定失败：BattleRuntimeState为空。");
            return;
        }

        BattleEndPanelController host = ResolveOrCreateInstance();
        if (host != null)
        {
            host.BindRuntimeState(state);
        }
    }

    private static BattleEndPanelController ResolveOrCreateInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject root = new GameObject(
            "BattleEndPanel",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        root.layer = LayerMask.NameToLayer("UI");
        return root.AddComponent<BattleEndPanelController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ConfigureCanvas();
        BuildPanel();
        SetPanelState(0f, false);
        ApplySafeArea(true);
    }

    private void Update()
    {
        if (!isVisible && runtimeState != null && runtimeState.IsBattleEnded)
        {
            Show(runtimeState.battleResult);
        }

        ApplySafeArea(false);
    }

    private void LateUpdate()
    {
        if (!isVisible)
        {
            return;
        }

        if (overlayCanvas != null)
        {
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = OverlaySortingOrder;
        }
        transform.SetAsLastSibling();
    }

    private void OnDestroy()
    {
        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(ReturnToStartScene);
        }

        if (instance == this)
        {
            instance = null;
        }
    }

    private void BindRuntimeState(BattleRuntimeState state)
    {
        runtimeState = state;
        isLoadingStartScene = false;
        if (returnButton != null)
        {
            returnButton.interactable = true;
        }

        if (state.IsBattleEnded)
        {
            Show(state.battleResult);
        }
        else
        {
            HideImmediate();
        }
    }

    private void Show(BattleResult battleResult)
    {
        if (isVisible)
        {
            return;
        }

        SetResultContent(battleResult);
        BattleActionRollPanelHost.HideImmediate();
        BattleActionSlotCardInfoPanelHost.CloseAllPanels();

        isVisible = true;
        SetPanelState(0f, true);
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    private void HideImmediate()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        isVisible = false;
        SetPanelState(0f, false);
    }

    private IEnumerator FadeIn()
    {
        if (fadeInDuration <= 0f)
        {
            SetPanelState(1f, true);
            fadeCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetPanelState(Mathf.Clamp01(elapsed / fadeInDuration), true);
            yield return null;
        }

        SetPanelState(1f, true);
        fadeCoroutine = null;
    }

    public void ReturnToStartScene()
    {
        if (isLoadingStartScene)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(startSceneName))
        {
            ShowLoadFailure("返回失败：开始场景名称为空。");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(startSceneName))
        {
            ShowLoadFailure(
                "返回失败：场景 “" + startSceneName +
                "” 未加入 Build Settings。"
            );
            return;
        }

        isLoadingStartScene = true;
        if (returnButton != null)
        {
            returnButton.interactable = false;
        }
        if (returnButtonText != null)
        {
            returnButtonText.text = "正在返回……";
        }

        StartCoroutine(LoadStartSceneAsync());
    }

    private IEnumerator LoadStartSceneAsync()
    {
        Time.timeScale = 1f;
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
            startSceneName,
            LoadSceneMode.Single
        );
        if (loadOperation == null)
        {
            isLoadingStartScene = false;
            if (returnButton != null)
            {
                returnButton.interactable = true;
            }
            if (returnButtonText != null)
            {
                returnButtonText.text = "返回开始界面";
            }
            ShowLoadFailure("返回失败：无法启动场景加载。");
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }
    }

    private void ShowLoadFailure(string message)
    {
        Debug.LogError(message, this);
        if (resultBodyText != null)
        {
            resultBodyText.text = message;
        }
    }

    private void SetResultContent(BattleResult battleResult)
    {
        if (resultTitleText == null || resultBodyText == null)
        {
            return;
        }

        switch (battleResult)
        {
            case BattleResult.Victory:
                resultTitleText.text = "战斗胜利";
                resultTitleText.color = new Color32(238, 197, 95, 255);
                resultBodyText.text = "所有敌人已被击败。";
                break;
            case BattleResult.Defeat:
                resultTitleText.text = "战斗失败";
                resultTitleText.color = new Color32(219, 79, 91, 255);
                resultBodyText.text = "玩家已经死亡，本场战斗结束。";
                break;
            default:
                resultTitleText.text = "战斗结束";
                resultTitleText.color = new Color32(224, 224, 224, 255);
                resultBodyText.text = "本场战斗已经结束。";
                break;
        }
    }

    private void ConfigureCanvas()
    {
        overlayCanvas = GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = OverlaySortingOrder;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
    }

    private void BuildPanel()
    {
        TMP_FontAsset font = ResolveBattleFont();

        GameObject safeAreaObject = new GameObject(
            "SafeAreaRoot",
            typeof(RectTransform),
            typeof(CanvasGroup)
        );
        safeAreaRoot = safeAreaObject.GetComponent<RectTransform>();
        safeAreaRoot.SetParent(transform, false);
        Stretch(safeAreaRoot);
        panelCanvasGroup = safeAreaObject.GetComponent<CanvasGroup>();

        Image dimBackground = CreateImage(
            safeAreaRoot,
            "InputBlocker",
            new Color32(4, 5, 8, 205)
        );
        Stretch(dimBackground.rectTransform);
        dimBackground.raycastTarget = true;

        Image panelBackground = CreateImage(
            safeAreaRoot,
            "ResultPanel",
            new Color32(24, 23, 29, 250)
        );
        RectTransform panelRect = panelBackground.rectTransform;
        SetCenteredRect(panelRect, Vector2.zero, new Vector2(640f, 360f));
        panelBackground.raycastTarget = true;

        Outline panelOutline = panelBackground.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color32(160, 128, 61, 230);
        panelOutline.effectDistance = new Vector2(3f, -3f);

        Image accent = CreateImage(
            panelRect,
            "Accent",
            new Color32(190, 148, 64, 255)
        );
        RectTransform accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 6f);
        accent.raycastTarget = false;

        resultTitleText = CreateText(
            panelRect,
            "ResultTitle",
            "战斗结束",
            48f,
            font
        );
        SetCenteredRect(
            resultTitleText.rectTransform,
            new Vector2(0f, 78f),
            new Vector2(560f, 82f)
        );
        resultTitleText.fontStyle = FontStyles.Bold;

        resultBodyText = CreateText(
            panelRect,
            "ResultBody",
            "本场战斗已经结束。",
            24f,
            font
        );
        SetCenteredRect(
            resultBodyText.rectTransform,
            new Vector2(0f, 10f),
            new Vector2(560f, 64f)
        );
        resultBodyText.color = new Color32(220, 216, 208, 255);

        returnButton = CreateButton(
            panelRect,
            "ReturnToStartButton",
            new Vector2(0f, -100f),
            new Vector2(280f, 68f)
        );
        returnButtonText = CreateText(
            returnButton.transform as RectTransform,
            "Label",
            "返回开始界面",
            26f,
            font
        );
        Stretch(returnButtonText.rectTransform);
        returnButtonText.fontStyle = FontStyles.Bold;
        returnButton.onClick.AddListener(ReturnToStartScene);
    }

    private static TMP_FontAsset ResolveBattleFont()
    {
        TMP_Text[] texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        for (int index = 0; index < texts.Length; index++)
        {
            TMP_FontAsset font = texts[index] != null ? texts[index].font : null;
            if (font != null && font.name.Contains("CN"))
            {
                return font;
            }
        }

        for (int index = 0; index < texts.Length; index++)
        {
            if (texts[index] != null && texts[index].font != null)
            {
                return texts[index].font;
            }
        }

        return TMP_Settings.defaultFontAsset;
    }

    private static Image CreateImage(
        RectTransform parent,
        string objectName,
        Color color
    )
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image)
        );
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(
        RectTransform parent,
        string objectName,
        string content,
        float fontSize,
        TMP_FontAsset font
    )
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(
        RectTransform parent,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetCenteredRect(rect, anchoredPosition, size);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color32(123, 83, 34, 255);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(255, 224, 164, 255);
        colors.pressedColor = new Color32(207, 166, 93, 255);
        colors.disabledColor = new Color32(100, 100, 100, 180);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.targetGraphic = image;
        return button;
    }

    private void SetPanelState(float alpha, bool interactive)
    {
        if (panelCanvasGroup == null)
        {
            return;
        }

        panelCanvasGroup.alpha = alpha;
        panelCanvasGroup.interactable = interactive;
        panelCanvasGroup.blocksRaycasts = interactive;
    }

    private void ApplySafeArea(bool force)
    {
        if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize)
        {
            return;
        }

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;
        safeAreaRoot.anchorMin = new Vector2(
            safeArea.xMin / Screen.width,
            safeArea.yMin / Screen.height
        );
        safeAreaRoot.anchorMax = new Vector2(
            safeArea.xMax / Screen.width,
            safeArea.yMax / Screen.height
        );
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;
    }

    private static void SetCenteredRect(
        RectTransform rect,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
