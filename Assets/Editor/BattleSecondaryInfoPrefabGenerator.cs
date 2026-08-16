#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 二级信息面板预设体生成器。
// 首次导入时仅补齐缺失资源；之后可通过菜单显式重建，避免覆盖策划在预设体上的修改。
public static class BattleSecondaryInfoPrefabGenerator
{
    const string GenericPrefabPath =
        "Assets/Prefabs/BattleSecondaryInfoPanel.prefab";
    const string SlotCardPrefabPath =
        "Assets/Prefabs/BattleActionSlotCardInfoPanel.prefab";
    const string CardUIPrefabPath =
        "Assets/Art/battle/kapai/BattleCardUI.prefab";
    const string BattleScenePath = "Assets/Scenes/BattleScene.unity";
    const string FontPath = "Assets/Fonts/TMP_Font_CN_Runtime.asset";
    const string SessionKey =
        "ProjectGuilt.BattleSecondaryInfoPrefabGenerator.CardOnlyV2Checked";
    static readonly Vector2 SlotCardPanelSize = new Vector2(366f, 540f);
    const float SlotCardPanelGap = 80f;

    [InitializeOnLoadMethod]
    static void ScheduleMissingAssetCheck()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += EnsureMissingAssetsAndScene;
    }

    [MenuItem("Project Guilt/UI/重新生成二级信息面板预设体")]
    public static void RebuildPrefabsAndRefreshBattleScene()
    {
        GenerateAll(true);
    }

    [MenuItem("Project Guilt/UI/重新生成行动槽位卡牌详情预设体")]
    public static void RebuildActionSlotCardInfoPrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EnsureAssetFolder("Assets/Prefabs");
        GameObject slotCardPrefab = CreateActionSlotCardInfoPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        GameObject genericPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(GenericPrefabPath);
        EnsureBattleSceneInstances(
            genericPrefab,
            slotCardPrefab,
            true
        );
        Debug.Log("行动槽位卡牌详情预设体已重新生成：\n" + SlotCardPrefabPath);
    }

    public static void ValidateGeneratedSetup()
    {
        GameObject genericPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(GenericPrefabPath);
        GameObject slotCardPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(SlotCardPrefabPath);
        Require(genericPrefab != null, "通用二级信息面板预设体不存在。");
        Require(slotCardPrefab != null, "行动槽位卡牌详情预设体不存在。");

        BattleSecondaryInfoPanelHost genericHost =
            genericPrefab.GetComponent<BattleSecondaryInfoPanelHost>();
        Require(genericHost != null, "通用预设体缺少宿主组件。");
        ValidateObjectReferences(
            new SerializedObject(genericHost),
            "overlayCanvas",
            "panelRect",
            "titleText",
            "bodyText",
            "footerText",
            "panelLayout"
        );

        BattleActionSlotCardInfoPanelHost slotHost =
            slotCardPrefab.GetComponent<BattleActionSlotCardInfoPanelHost>();
        Require(slotHost != null, "行动槽位预设体缺少宿主组件。");
        ValidateObjectReferences(
            new SerializedObject(slotHost),
            "overlayCanvas",
            "allyPanelRect",
            "enemyPanelRect",
            "allyPanelView",
            "enemyPanelView"
        );

        BattleActionSlotCardInfoPanelView[] sideViews =
            slotCardPrefab.GetComponentsInChildren<
                BattleActionSlotCardInfoPanelView
            >(true);
        Require(sideViews.Length == 2, "行动槽位预设体必须恰好包含左右两个详情 View。");
        for (int index = 0; index < sideViews.Length; index++)
        {
            ValidateObjectReferences(
                new SerializedObject(sideViews[index]),
                "artworkImage",
                "cardPreviewPrefab",
                "closeButton"
            );
            RectTransform sideRect =
                sideViews[index].transform as RectTransform;
            Require(
                sideRect != null &&
                Vector2.Distance(
                    sideRect.sizeDelta,
                    SlotCardPanelSize
                ) <= 0.01f,
                "行动槽位技能面板预设体尺寸必须为 366×540。"
            );
        }

        Scene battleScene = EditorSceneManager.OpenScene(
            BattleScenePath,
            OpenSceneMode.Additive
        );
        try
        {
            Require(
                CountSceneComponents<BattleSecondaryInfoPanelHost>(
                    battleScene
                ) == 1,
                "BattleScene 必须恰好包含一个通用二级信息面板预设体实例。"
            );
            Require(
                CountSceneComponents<BattleActionSlotCardInfoPanelHost>(
                    battleScene
                ) == 1,
                "BattleScene 必须恰好包含一个行动槽位卡牌详情预设体实例。"
            );
        }
        finally
        {
            EditorSceneManager.CloseScene(battleScene, true);
        }

        Debug.Log("二级信息面板预设体与 BattleScene 实例校验通过。");
    }

    static void EnsureMissingAssetsAndScene()
    {
        GenerateAll(false);
    }

    static void GenerateAll(bool forceRebuild)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        try
        {
            EnsureAssetFolder("Assets/Prefabs");

            GameObject genericPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(GenericPrefabPath);
            if (genericPrefab == null || forceRebuild)
            {
                genericPrefab = CreateGenericInfoPrefab();
            }

            GameObject slotCardPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(SlotCardPrefabPath);
            bool slotCardPrefabRebuilt = slotCardPrefab == null ||
                forceRebuild ||
                RequiresActionSlotCardPrefabUpgrade(slotCardPrefab);
            if (slotCardPrefabRebuilt)
            {
                slotCardPrefab = CreateActionSlotCardInfoPrefab();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EnsureBattleSceneInstances(
                genericPrefab,
                slotCardPrefab,
                slotCardPrefabRebuilt
            );
            Debug.Log(
                "二级信息面板预设体已就绪：\n" +
                GenericPrefabPath + "\n" +
                SlotCardPrefabPath
            );
        }
        catch (Exception exception)
        {
            Debug.LogError("生成二级信息面板预设体失败：" + exception);
        }
    }

    static bool RequiresActionSlotCardPrefabUpgrade(GameObject prefab)
    {
        if (prefab == null)
        {
            return true;
        }

        BattleActionSlotCardInfoPanelHost host =
            prefab.GetComponent<BattleActionSlotCardInfoPanelHost>();
        BattleActionSlotCardInfoPanelView[] views =
            prefab.GetComponentsInChildren<
                BattleActionSlotCardInfoPanelView
            >(true);
        if (host == null || views.Length != 2)
        {
            return true;
        }

        for (int index = 0; index < views.Length; index++)
        {
            SerializedObject serializedView =
                new SerializedObject(views[index]);
            SerializedProperty artwork =
                serializedView.FindProperty("artworkImage");
            SerializedProperty cardPrefab =
                serializedView.FindProperty("cardPreviewPrefab");
            SerializedProperty closeButton =
                serializedView.FindProperty("closeButton");
            RectTransform panelRect =
                views[index].transform as RectTransform;
            if (artwork == null ||
                artwork.objectReferenceValue == null ||
                cardPrefab == null ||
                cardPrefab.objectReferenceValue == null ||
                closeButton == null ||
                closeButton.objectReferenceValue == null ||
                panelRect == null ||
                Vector2.Distance(
                    panelRect.sizeDelta,
                    SlotCardPanelSize
                ) > 0.01f ||
                views[index].transform.Find("InfoColumn") != null)
            {
                return true;
            }
        }

        return false;
    }

    static GameObject CreateGenericInfoPrefab()
    {
        TMP_FontAsset font =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        GameObject root = CreateOverlayCanvasRoot(
            "BattleSecondaryInfoPanel",
            out Canvas canvas
        );
        BattleSecondaryInfoPanelHost host =
            root.AddComponent<BattleSecondaryInfoPanelHost>();

        GameObject panel = new GameObject(
            "SecondaryInfoPanel",
            typeof(RectTransform),
            typeof(Image),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter),
            typeof(Outline)
        );
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(root.transform, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(440f, 160f);

        Image background = panel.GetComponent<Image>();
        background.color = new Color32(24, 27, 35, 248);
        background.raycastTarget = true;
        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color32(205, 177, 104, 210);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        VerticalLayoutGroup layout =
            panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text title = CreateText(
            panelRect,
            "Title",
            "状态 / 关键词名称（预设体中可编辑）",
            22f,
            new Color32(238, 210, 137, 255),
            FontStyles.Bold,
            font
        );
        TMP_Text body = CreateText(
            panelRect,
            "Body",
            "这里是二级信息说明文字。运行时会由悬停目标的内容覆盖。",
            17f,
            new Color32(235, 237, 242, 255),
            FontStyles.Normal,
            font
        );
        TMP_Text footer = CreateText(
            panelRect,
            "Footer",
            "补充信息（可选）",
            14f,
            new Color32(167, 174, 190, 255),
            FontStyles.Normal,
            font
        );

        SerializedObject serializedHost = new SerializedObject(host);
        SetObjectReference(serializedHost, "overlayCanvas", canvas);
        SetObjectReference(serializedHost, "panelRect", panelRect);
        SetObjectReference(serializedHost, "titleText", title);
        SetObjectReference(serializedHost, "bodyText", body);
        SetObjectReference(serializedHost, "footerText", footer);
        SetObjectReference(serializedHost, "panelLayout", layout);
        serializedHost.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
            root,
            GenericPrefabPath
        );
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject CreateActionSlotCardInfoPrefab()
    {
        TMP_FontAsset font =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        GameObject cardUIPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(CardUIPrefabPath);
        if (cardUIPrefab == null)
        {
            throw new InvalidOperationException(
                "找不到行动卡牌 UI 预设体：" + CardUIPrefabPath
            );
        }

        GameObject root = CreateOverlayCanvasRoot(
            "BattleActionSlotCardInfoPanel",
            out Canvas canvas
        );
        canvas.sortingOrder = 32766;
        BattleActionSlotCardInfoPanelHost host =
            root.AddComponent<BattleActionSlotCardInfoPanelHost>();

        BattleActionSlotCardInfoPanelView allyView = CreateCardInfoSide(
            root.transform,
            "AllyCardInfoPanel",
            false,
            font,
            cardUIPrefab,
            out RectTransform allyRect
        );
        BattleActionSlotCardInfoPanelView enemyView = CreateCardInfoSide(
            root.transform,
            "EnemyCardInfoPanel",
            true,
            font,
            cardUIPrefab,
            out RectTransform enemyRect
        );

        SerializedObject serializedHost = new SerializedObject(host);
        SetObjectReference(serializedHost, "overlayCanvas", canvas);
        SetObjectReference(serializedHost, "allyPanelRect", allyRect);
        SetObjectReference(serializedHost, "enemyPanelRect", enemyRect);
        SetObjectReference(serializedHost, "allyPanelView", allyView);
        SetObjectReference(serializedHost, "enemyPanelView", enemyView);
        serializedHost.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
            root,
            SlotCardPrefabPath
        );
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    static BattleActionSlotCardInfoPanelView CreateCardInfoSide(
        Transform parent,
        string objectName,
        bool enemySide,
        TMP_FontAsset font,
        GameObject cardUIPrefab,
        out RectTransform panelRect
    )
    {
        GameObject panel = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(BattleActionSlotCardInfoPanelView)
        );
        panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(parent, false);
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = panelRect.anchorMin;
        panelRect.pivot = enemySide
            ? new Vector2(0f, 1f)
            : new Vector2(1f, 1f);
        panelRect.anchoredPosition = enemySide
            ? new Vector2(SlotCardPanelGap * 0.5f, -24f)
            : new Vector2(-SlotCardPanelGap * 0.5f, -24f);
        panelRect.sizeDelta = SlotCardPanelSize;

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        Image artwork = CreateImage(
            panelRect,
            "CardArtwork",
            Color.clear,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );
        BattleCardUIView cardPreviewPrefab =
            cardUIPrefab.GetComponent<BattleCardUIView>();
        if (cardPreviewPrefab == null)
        {
            throw new InvalidOperationException(
                "行动卡牌 UI 预设体缺少 BattleCardUIView。"
            );
        }

        Button closeButton = CreateCloseButton(panelRect, font, enemySide);

        BattleActionSlotCardInfoPanelView view =
            panel.GetComponent<BattleActionSlotCardInfoPanelView>();
        SerializedObject serializedView = new SerializedObject(view);
        SetObjectReference(serializedView, "artworkImage", artwork);
        SetObjectReference(
            serializedView,
            "cardPreviewPrefab",
            cardPreviewPrefab
        );
        SetObjectReference(serializedView, "closeButton", closeButton);
        serializedView.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    static Button CreateCloseButton(
        RectTransform parent,
        TMP_FontAsset font,
        bool enemySide
    )
    {
        GameObject buttonObject = new GameObject(
            "CloseButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-28f, -28f);
        rect.sizeDelta = new Vector2(52f, 52f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = enemySide
            ? new Color32(126, 52, 158, 242)
            : new Color32(38, 107, 70, 242);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(232, 232, 232, 255);
        colors.pressedColor = new Color32(190, 190, 190, 255);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color32(128, 128, 128, 160);
        button.colors = colors;

        TMP_Text label = CreateText(
            rect,
            "Label",
            "×",
            36f,
            Color.white,
            FontStyles.Bold,
            font
        );
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return button;
    }

    static GameObject CreateOverlayCanvasRoot(
        string objectName,
        out Canvas canvas
    )
    {
        GameObject root = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32767;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
        return root;
    }

    static TMP_Text CreateLayoutText(
        RectTransform parent,
        string objectName,
        string previewText,
        float fontSize,
        Color color,
        FontStyles fontStyle,
        TMP_FontAsset font,
        float preferredHeight
    )
    {
        TMP_Text text = CreateText(
            parent,
            objectName,
            previewText,
            fontSize,
            color,
            fontStyle,
            font
        );
        LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        return text;
    }

    static TMP_Text CreateText(
        RectTransform parent,
        string objectName,
        string previewText,
        float fontSize,
        Color color,
        FontStyles fontStyle,
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
        text.text = previewText;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.richText = true;
        return text;
    }

    static Image CreateImage(
        RectTransform parent,
        string objectName,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax
    )
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image)
        );
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    static void SetObjectReference(
        SerializedObject serializedObject,
        string propertyName,
        UnityEngine.Object value
    )
    {
        SerializedProperty property =
            serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException(
                "找不到序列化字段：" + propertyName
            );
        }

        property.objectReferenceValue = value;
    }

    static void EnsureBattleSceneInstances(
        GameObject genericPrefab,
        GameObject slotCardPrefab,
        bool refreshSlotCardInstance
    )
    {
        if (genericPrefab == null || slotCardPrefab == null)
        {
            throw new InvalidOperationException("二级信息面板预设体资源为空。");
        }

        Scene battleScene = SceneManager.GetSceneByPath(BattleScenePath);
        bool openedAdditively = !battleScene.IsValid() || !battleScene.isLoaded;
        if (openedAdditively)
        {
            battleScene = EditorSceneManager.OpenScene(
                BattleScenePath,
                OpenSceneMode.Additive
            );
        }

        bool changed = false;
        if (!SceneContainsComponent<BattleSecondaryInfoPanelHost>(battleScene))
        {
            GameObject instanceObject = PrefabUtility.InstantiatePrefab(
                genericPrefab,
                battleScene
            ) as GameObject;
            instanceObject.name = "BattleSecondaryInfoPanel";
            SetChildActive(instanceObject, "SecondaryInfoPanel", false);
            changed = true;
        }

        if (refreshSlotCardInstance)
        {
            while (TryRemoveSceneComponentInstance<
                BattleActionSlotCardInfoPanelHost
            >(battleScene))
            {
                changed = true;
            }
        }

        if (!SceneContainsComponent<BattleActionSlotCardInfoPanelHost>(battleScene))
        {
            GameObject instanceObject = PrefabUtility.InstantiatePrefab(
                slotCardPrefab,
                battleScene
            ) as GameObject;
            instanceObject.name = "BattleActionSlotCardInfoPanel";
            SetChildActive(instanceObject, "AllyCardInfoPanel", false);
            SetChildActive(instanceObject, "EnemyCardInfoPanel", false);
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(battleScene);
            if (!EditorSceneManager.SaveScene(battleScene))
            {
                throw new InvalidOperationException("BattleScene 保存失败。");
            }
        }

        if (openedAdditively)
        {
            EditorSceneManager.CloseScene(battleScene, true);
        }
    }

    static bool TryRemoveSceneComponentInstance<T>(Scene scene)
        where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int index = 0; index < roots.Length; index++)
        {
            T component = roots[index].GetComponentInChildren<T>(true);
            if (component == null)
            {
                continue;
            }

            GameObject instanceRoot =
                PrefabUtility.GetNearestPrefabInstanceRoot(
                    component.gameObject
                );
            UnityEngine.Object.DestroyImmediate(
                instanceRoot != null
                    ? instanceRoot
                    : component.gameObject
            );
            return true;
        }

        return false;
    }

    static bool SceneContainsComponent<T>(Scene scene)
        where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int index = 0; index < roots.Length; index++)
        {
            if (roots[index].GetComponentInChildren<T>(true) != null)
            {
                return true;
            }
        }

        return false;
    }

    static int CountSceneComponents<T>(Scene scene)
        where T : Component
    {
        int count = 0;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int index = 0; index < roots.Length; index++)
        {
            count += roots[index].GetComponentsInChildren<T>(true).Length;
        }

        return count;
    }

    static void ValidateObjectReferences(
        SerializedObject serializedObject,
        params string[] propertyNames
    )
    {
        for (int index = 0; index < propertyNames.Length; index++)
        {
            string propertyName = propertyNames[index];
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);
            Require(property != null, "找不到序列化字段：" + propertyName);
            Require(
                property.objectReferenceValue != null,
                serializedObject.targetObject.name +
                " 的序列化引用为空：" +
                propertyName
            );
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    static void SetChildActive(
        GameObject root,
        string childName,
        bool active
    )
    {
        if (root == null)
        {
            return;
        }

        Transform child = root.transform.Find(childName);
        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
    }

    static void EnsureAssetFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
#endif
