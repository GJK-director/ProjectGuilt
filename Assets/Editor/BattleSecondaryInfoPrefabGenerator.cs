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
    const string BattleScenePath = "Assets/Scenes/BattleScene.unity";
    const string FontPath = "Assets/Fonts/TMP_Font_CN_Runtime.asset";
    const string SessionKey =
        "ProjectGuilt.BattleSecondaryInfoPrefabGenerator.Checked";

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
                "accentImage",
                "artworkImage",
                "sideLabelText",
                "ownerText",
                "cardNameText",
                "pointText",
                "typeAndCooldownText",
                "descriptionText",
                "stateText",
                "keywordText"
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
            if (slotCardPrefab == null || forceRebuild)
            {
                slotCardPrefab = CreateActionSlotCardInfoPrefab();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EnsureBattleSceneInstances(genericPrefab, slotCardPrefab);
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
        GameObject root = CreateOverlayCanvasRoot(
            "BattleActionSlotCardInfoPanel",
            out Canvas canvas
        );
        BattleActionSlotCardInfoPanelHost host =
            root.AddComponent<BattleActionSlotCardInfoPanelHost>();

        BattleActionSlotCardInfoPanelView allyView = CreateCardInfoSide(
            root.transform,
            "AllyCardInfoPanel",
            false,
            font,
            out RectTransform allyRect
        );
        BattleActionSlotCardInfoPanelView enemyView = CreateCardInfoSide(
            root.transform,
            "EnemyCardInfoPanel",
            true,
            font,
            out RectTransform enemyRect
        );

        SerializedObject serializedHost = new SerializedObject(host);
        SetObjectReference(serializedHost, "overlayCanvas", canvas);
        SetObjectReference(serializedHost, "allyPanelRect", allyRect);
        SetObjectReference(serializedHost, "enemyPanelRect", enemyRect);
        SetObjectReference(serializedHost, "allyPanelView", allyView);
        SetObjectReference(serializedHost, "enemyPanelView", enemyView);
        serializedHost.FindProperty("showDelay").floatValue = 0.25f;
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
        out RectTransform panelRect
    )
    {
        GameObject panel = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image),
            typeof(Outline),
            typeof(CanvasGroup),
            typeof(BattleActionSlotCardInfoPanelView)
        );
        panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(parent, false);
        panelRect.anchorMin = enemySide
            ? new Vector2(1f, 1f)
            : new Vector2(0f, 1f);
        panelRect.anchorMax = panelRect.anchorMin;
        panelRect.pivot = panelRect.anchorMin;
        panelRect.anchoredPosition = enemySide
            ? new Vector2(-24f, -24f)
            : new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(640f, 360f);

        Color accentColor = enemySide
            ? new Color32(174, 87, 215, 255)
            : new Color32(83, 183, 127, 255);
        Image background = panel.GetComponent<Image>();
        background.color = new Color32(17, 19, 27, 248);
        background.raycastTarget = false;
        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = accentColor;
        outline.effectDistance = new Vector2(2f, -2f);
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Image accent = CreateImage(
            panelRect,
            "Accent",
            accentColor,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 0f),
            new Vector2(0f, 7f)
        );
        Image artwork = CreateImage(
            panelRect,
            "CardArtwork",
            enemySide
                ? new Color32(76, 47, 93, 255)
                : new Color32(41, 78, 61, 255),
            new Vector2(0f, 0f),
            new Vector2(0.41f, 1f),
            new Vector2(18f, 20f),
            new Vector2(-8f, -24f)
        );
        TMP_Text artworkHint = CreateText(
            artwork.rectTransform,
            "ArtworkHint",
            "卡牌图像\n（可在预设体替换）",
            19f,
            new Color32(222, 225, 231, 210),
            FontStyles.Normal,
            font
        );
        Stretch(artworkHint.rectTransform, 12f);
        artworkHint.alignment = TextAlignmentOptions.Center;

        GameObject infoObject = new GameObject(
            "InfoColumn",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup)
        );
        RectTransform infoRect = infoObject.GetComponent<RectTransform>();
        infoRect.SetParent(panelRect, false);
        infoRect.anchorMin = new Vector2(0.43f, 0f);
        infoRect.anchorMax = Vector2.one;
        infoRect.offsetMin = new Vector2(0f, 18f);
        infoRect.offsetMax = new Vector2(-18f, -18f);
        VerticalLayoutGroup infoLayout =
            infoObject.GetComponent<VerticalLayoutGroup>();
        infoLayout.spacing = 5f;
        infoLayout.childAlignment = TextAnchor.UpperLeft;
        infoLayout.childControlWidth = true;
        infoLayout.childControlHeight = true;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = false;

        TMP_Text sideLabel = CreateLayoutText(
            infoRect,
            "SideLabel",
            enemySide ? "敌方意图" : "我方行动",
            18f,
            accentColor,
            FontStyles.Bold,
            font,
            24f
        );
        TMP_Text owner = CreateLayoutText(
            infoRect,
            "Owner",
            enemySide ? "敌方角色" : "我方角色",
            15f,
            new Color32(173, 179, 194, 255),
            FontStyles.Normal,
            font,
            22f
        );
        TMP_Text cardName = CreateLayoutText(
            infoRect,
            "CardName",
            "行动卡牌名称",
            27f,
            new Color32(244, 239, 224, 255),
            FontStyles.Bold,
            font,
            40f
        );
        TMP_Text point = CreateLayoutText(
            infoRect,
            "Point",
            "点数  2-5",
            22f,
            new Color32(245, 151, 76, 255),
            FontStyles.Bold,
            font,
            32f
        );
        TMP_Text typeAndCooldown = CreateLayoutText(
            infoRect,
            "TypeAndCooldown",
            "类型  攻    基础 CD  1",
            16f,
            new Color32(202, 206, 216, 255),
            FontStyles.Normal,
            font,
            25f
        );
        TMP_Text description = CreateLayoutText(
            infoRect,
            "Description",
            "卡牌效果说明。运行时会显示槽位上卡牌的实际内容。",
            17f,
            new Color32(235, 237, 242, 255),
            FontStyles.Normal,
            font,
            105f
        );
        LayoutElement descriptionLayout =
            description.GetComponent<LayoutElement>();
        descriptionLayout.flexibleHeight = 1f;
        description.overflowMode = TextOverflowModes.Ellipsis;
        TMP_Text state = CreateLayoutText(
            infoRect,
            "State",
            "当前状态：可行动",
            15f,
            new Color32(177, 221, 190, 255),
            FontStyles.Normal,
            font,
            23f
        );
        TMP_Text keyword = CreateLayoutText(
            infoRect,
            "Keyword",
            "关键词：无",
            14f,
            new Color32(178, 184, 200, 255),
            FontStyles.Normal,
            font,
            22f
        );

        BattleActionSlotCardInfoPanelView view =
            panel.GetComponent<BattleActionSlotCardInfoPanelView>();
        SerializedObject serializedView = new SerializedObject(view);
        SetObjectReference(serializedView, "accentImage", accent);
        SetObjectReference(serializedView, "artworkImage", artwork);
        SetObjectReference(serializedView, "sideLabelText", sideLabel);
        SetObjectReference(serializedView, "ownerText", owner);
        SetObjectReference(serializedView, "cardNameText", cardName);
        SetObjectReference(serializedView, "pointText", point);
        SetObjectReference(
            serializedView,
            "typeAndCooldownText",
            typeAndCooldown
        );
        SetObjectReference(serializedView, "descriptionText", description);
        SetObjectReference(serializedView, "stateText", state);
        SetObjectReference(serializedView, "keywordText", keyword);
        serializedView.ApplyModifiedPropertiesWithoutUndo();
        return view;
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
        GameObject slotCardPrefab
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
