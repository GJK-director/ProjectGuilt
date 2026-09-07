using System;
using System.Collections.Generic;
using ProjectGuilt.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 序章场景的一次性/可重复执行组装工具。
/// </summary>
public static class IntroStorySceneSetup
{
    private const string IntroScenePath = "Assets/Scenes/NewGameText.unity";
    private const string StoryPanelPrefabPath =
        "Assets/Scripts/Story/Prefabs/StoryPanel.prefab";
    private const string ChineseFontPath = "Assets/Fonts/SIMHEI.TTF";

    private struct BackgroundEntry
    {
        public string id;
        public string assetPath;

        public BackgroundEntry(string id, string assetPath)
        {
            this.id = id;
            this.assetPath = assetPath;
        }
    }

    private static readonly BackgroundEntry[] BackgroundEntries =
    {
        new BackgroundEntry(
            "corridor_door_closed",
            "Assets/Art/Story/Prologue501/CG/CorridorDoorClosed.png"
        ),
        new BackgroundEntry(
            "corridor_door_open",
            "Assets/Art/Story/Prologue501/CG/CorridorDoorOpen.png"
        ),
        new BackgroundEntry(
            "phone_off",
            "Assets/Art/Story/Prologue501/Layers/PhoneOff.png"
        ),
        new BackgroundEntry(
            "phone_on",
            "Assets/Art/Story/Prologue501/Layers/PhoneOn.png"
        ),
        new BackgroundEntry(
            "tv_viewer_wide",
            "Assets/Art/Story/Prologue501/CG/TVViewerWide.png"
        ),
        new BackgroundEntry(
            "tv_viewer_variant",
            "Assets/Art/Story/Prologue501/CG/TVViewerVariant.png"
        ),
        new BackgroundEntry(
            "monster_turn_closeup",
            "Assets/Art/Story/Prologue501/CG/MonsterTurnCloseup.png"
        )
    };

    [MenuItem("ProjectGuilt/Story/Rebuild Prologue 501 Scene")]
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureStoryTextureImports();
        ConfigureStoryPanelPrefab();
        BuildIntroScene();
        EnsureIntroSceneInBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("序章场景组装完成：" + IntroScenePath);
    }

    [MenuItem("ProjectGuilt/Story/Validate Prologue 501")]
    public static void Validate()
    {
        const string storyJsonPath =
            "Assets/Resources/Story/prologue_501.json";
        TextAsset storyJson = AssetDatabase.LoadAssetAtPath<TextAsset>(storyJsonPath);

        if (storyJson == null)
        {
            throw new InvalidOperationException("找不到序章 JSON：" + storyJsonPath);
        }

        StoryDefinitionData definition;
        string errorMessage;
        StoryJsonSerializer serializer = new StoryJsonSerializer();

        if (!serializer.TryDeserializeDefinition(
            storyJson.text,
            out definition,
            out errorMessage
        ))
        {
            throw new InvalidOperationException("序章 JSON 校验失败：" + errorMessage);
        }

        StoryTextPresenter presenter = new StoryTextPresenter(100f);
        presenter.Begin(
            "pause_test",
            new StoryDialogueData { text = "一下||又一下。" }
        );
        presenter.Tick(0.02f);
        int countBeforePause = presenter.VisibleCharacterCount;
        presenter.Tick(0.1f);

        if (presenter.FullText != "一下又一下。" ||
            countBeforePause != 2 ||
            presenter.VisibleCharacterCount != countBeforePause)
        {
            throw new InvalidOperationException("剧情 || 停顿标记行为校验失败。 ");
        }

        Scene scene = EditorSceneManager.OpenScene(IntroScenePath, OpenSceneMode.Single);
        IntroStoryHost host = UnityEngine.Object.FindFirstObjectByType<IntroStoryHost>();
        SceneLoadingOverlay overlay =
            UnityEngine.Object.FindFirstObjectByType<SceneLoadingOverlay>();
        StorySceneFacade facade =
            UnityEngine.Object.FindFirstObjectByType<StorySceneFacade>();

        if (!scene.IsValid() || host == null || overlay == null || facade == null)
        {
            throw new InvalidOperationException(
                "序章场景缺少 IntroStoryHost、SceneLoadingOverlay 或 StorySceneFacade。"
            );
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            StoryPanelPrefabPath
        );
        StoryPanelView view = prefab != null
            ? prefab.GetComponent<StoryPanelView>()
            : null;
        SerializedObject serializedView = view != null
            ? new SerializedObject(view)
            : null;
        SerializedProperty bindings = serializedView != null
            ? serializedView.FindProperty("backgroundBindings")
            : null;

        if (bindings == null || bindings.arraySize != BackgroundEntries.Length)
        {
            throw new InvalidOperationException("剧情面板的序章 CG 绑定不完整。 ");
        }

        Debug.Log(
            "序章校验通过：" +
            definition.nodes.Count + " 个节点，" +
            bindings.arraySize + " 个 CG/背景绑定，|| 停顿规则正常。"
        );
    }

    private static void ConfigureStoryTextureImports()
    {
        foreach (BackgroundEntry entry in BackgroundEntries)
        {
            TextureImporter importer = AssetImporter.GetAtPath(entry.assetPath)
                as TextureImporter;

            if (importer == null)
            {
                throw new InvalidOperationException(
                    "找不到序章图片或导入器：" + entry.assetPath
                );
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureStoryPanelPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(
            StoryPanelPrefabPath
        );

        try
        {
            StoryPanelView view = prefabRoot.GetComponent<StoryPanelView>();

            if (view == null)
            {
                throw new InvalidOperationException(
                    "StoryPanel.prefab 缺少 StoryPanelView。"
                );
            }

            SerializedObject serializedView = new SerializedObject(view);
            SerializedProperty bindings = serializedView.FindProperty(
                "backgroundBindings"
            );

            if (bindings == null)
            {
                throw new InvalidOperationException(
                    "StoryPanelView 缺少 backgroundBindings 字段。"
                );
            }

            bindings.arraySize = BackgroundEntries.Length;

            for (int index = 0; index < BackgroundEntries.Length; index++)
            {
                BackgroundEntry entry = BackgroundEntries[index];
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    entry.assetPath
                );

                if (sprite == null)
                {
                    throw new InvalidOperationException(
                        "序章图片未按 Sprite 导入：" + entry.assetPath
                    );
                }

                SerializedProperty element = bindings.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("backgroundId").stringValue = entry.id;
                element.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            }

            SetReferencedObjectInactive(
                serializedView.FindProperty("backgroundLabel")
            );
            SetReferencedObjectInactive(serializedView.FindProperty("statusText"));
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, StoryPanelPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void SetReferencedObjectInactive(SerializedProperty property)
    {
        Component component = property != null
            ? property.objectReferenceValue as Component
            : null;

        if (component != null)
        {
            component.gameObject.SetActive(false);
        }
    }

    private static void BuildIntroScene()
    {
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        CreateCamera();
        CreateEventSystem();

        GameObject storyPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            StoryPanelPrefabPath
        );

        if (storyPanelPrefab == null)
        {
            throw new InvalidOperationException(
                "找不到剧情面板预制体：" + StoryPanelPrefabPath
            );
        }

        GameObject storyPanel = PrefabUtility.InstantiatePrefab(
            storyPanelPrefab,
            scene
        ) as GameObject;
        storyPanel.name = "StoryPanel_Prologue501";
        Canvas storyCanvas = storyPanel.GetComponent<Canvas>();

        if (storyCanvas != null)
        {
            storyCanvas.sortingOrder = 0;
        }

        StorySceneFacade facade = storyPanel.GetComponent<StorySceneFacade>();

        if (facade == null)
        {
            throw new InvalidOperationException(
                "剧情面板预制体缺少 StorySceneFacade。"
            );
        }

        SceneLoadingOverlay loadingOverlay = CreateLoadingOverlay();
        GameObject hostObject = new GameObject(
            "IntroStoryHost",
            typeof(AudioSource),
            typeof(IntroStoryHost)
        );
        AudioSource audioSource = hostObject.GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        IntroStoryHost host = hostObject.GetComponent<IntroStoryHost>();
        SerializedObject serializedHost = new SerializedObject(host);
        serializedHost.FindProperty("storyFacade").objectReferenceValue = facade;
        serializedHost.FindProperty("storyId").stringValue = "prologue_501";
        serializedHost.FindProperty("battleSceneName").stringValue = "BattleScene";
        serializedHost.FindProperty("loadingOverlay").objectReferenceValue =
            loadingOverlay;
        serializedHost.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);

        if (!EditorSceneManager.SaveScene(scene, IntroScenePath))
        {
            throw new InvalidOperationException(
                "保存序章场景失败：" + IntroScenePath
            );
        }
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject(
            "Main Camera",
            typeof(Camera),
            typeof(AudioListener)
        );
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule)
        );
        InputSystemUIInputModule inputModule =
            eventSystemObject.GetComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    private static SceneLoadingOverlay CreateLoadingOverlay()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(ChineseFontPath);

        if (font == null)
        {
            throw new InvalidOperationException("找不到中文字体：" + ChineseFontPath);
        }

        GameObject canvasObject = new GameObject(
            "LoadingCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(SceneLoadingOverlay)
        );
        canvasObject.layer = 5;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject overlayRoot = CreateUiObject("LoadingOverlay", canvasObject.transform);
        StretchToParent(overlayRoot.GetComponent<RectTransform>());
        Image background = overlayRoot.AddComponent<Image>();
        background.color = new Color32(7, 8, 11, 255);
        background.raycastTarget = true;

        Text title = CreateText(
            "LoadingTitle",
            overlayRoot.transform,
            font,
            "正在进入战斗",
            38,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            new Color32(225, 222, 216, 255)
        );
        SetCenteredRect(title.rectTransform, 760f, 80f, 0f, 90f);

        Text subtitle = CreateText(
            "LoadingSubtitle",
            overlayRoot.transform,
            font,
            "清洁任务 · 12栋501",
            21,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            new Color32(136, 139, 145, 255)
        );
        SetCenteredRect(subtitle.rectTransform, 760f, 44f, 0f, 38f);

        GameObject trackObject = CreateUiObject(
            "ProgressTrack",
            overlayRoot.transform
        );
        RectTransform trackRect = trackObject.GetComponent<RectTransform>();
        SetCenteredRect(trackRect, 760f, 14f, 0f, -28f);
        Image track = trackObject.AddComponent<Image>();
        track.color = new Color32(38, 40, 45, 255);

        GameObject fillObject = CreateUiObject("ProgressFill", trackObject.transform);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        StretchToParent(fillRect);
        Image fill = fillObject.AddComponent<Image>();
        fill.color = new Color32(150, 44, 54, 255);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;

        Text progressText = CreateText(
            "ProgressText",
            overlayRoot.transform,
            font,
            "加载中  0%",
            19,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            new Color32(172, 174, 180, 255)
        );
        SetCenteredRect(progressText.rectTransform, 400f, 42f, 0f, -76f);

        SceneLoadingOverlay loadingOverlay =
            canvasObject.GetComponent<SceneLoadingOverlay>();
        SerializedObject serializedOverlay = new SerializedObject(loadingOverlay);
        serializedOverlay.FindProperty("overlayRoot").objectReferenceValue = overlayRoot;
        serializedOverlay.FindProperty("progressFill").objectReferenceValue = fill;
        serializedOverlay.FindProperty("progressText").objectReferenceValue =
            progressText;
        serializedOverlay.FindProperty("minimumVisibleSeconds").floatValue = 0.8f;
        serializedOverlay.ApplyModifiedPropertiesWithoutUndo();
        overlayRoot.SetActive(false);
        return loadingOverlay;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = 5;
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Text CreateText(
        string name,
        Transform parent,
        Font font,
        string content,
        int fontSize,
        FontStyle fontStyle,
        TextAnchor alignment,
        Color color
    )
    {
        GameObject textObject = CreateUiObject(name, parent);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }

    private static void SetCenteredRect(
        RectTransform rectTransform,
        float width,
        float height,
        float x,
        float y
    )
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(width, height);
        rectTransform.anchoredPosition = new Vector2(x, y);
    }

    private static void EnsureIntroSceneInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes
        );
        bool found = false;

        for (int index = 0; index < scenes.Count; index++)
        {
            if (string.Equals(
                scenes[index].path,
                IntroScenePath,
                StringComparison.OrdinalIgnoreCase
            ))
            {
                scenes[index] = new EditorBuildSettingsScene(IntroScenePath, true);
                found = true;
                break;
            }
        }

        if (!found)
        {
            scenes.Add(new EditorBuildSettingsScene(IntroScenePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
