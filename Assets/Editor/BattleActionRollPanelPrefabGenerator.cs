#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 生成执行阶段动态加载的行动 Roll 面板预设体。
public static class BattleActionRollPanelPrefabGenerator
{
    const string PrefabPath =
        "Assets/Resources/UI/BattleActionRollPanel.prefab";
    const string CardUIPrefabPath =
        "Assets/Art/battle/kapai/BattleCardUI.prefab";
    const string FontPath = "Assets/Fonts/TMP_Font_CN_Runtime.asset";
    const string AllyRollFramePath =
        "Assets/Art/battle/Slot/xingdong-full.png";
    const string EnemyRollFramePath =
        "Assets/Art/battle/Slot/xingdong-emey-full.png";
    const string SessionKey =
        "ProjectGuilt.BattleActionRollPanelPrefabGenerator.CheckedV1";

    [InitializeOnLoadMethod]
    static void ScheduleMissingPrefabCheck()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += EnsureMissingPrefab;
    }

    [MenuItem("Project Guilt/UI/重新生成行动 Roll 点面板预设体")]
    public static void RebuildPrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        CreateAndSavePrefab();
        ValidatePrefab();
        Debug.Log("行动 Roll 点面板预设体已生成：\n" + PrefabPath);
    }

    // 供命令行校验使用；成功时 Unity 进程可直接由 -quit 退出。
    public static void RebuildAndValidateForBatchMode()
    {
        RebuildPrefab();
    }

    public static void ValidatePrefab()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Require(prefab != null, "行动 Roll 点面板预设体不存在。");

        BattleActionRollPanelHost host =
            prefab.GetComponent<BattleActionRollPanelHost>();
        Require(host != null, "行动 Roll 点面板预设体缺少宿主组件。");
        ValidateReferences(
            new SerializedObject(host),
            "overlayCanvas",
            "safeAreaRoot",
            "panelCanvasGroup",
            "allySideView",
            "enemySideView"
        );

        BattleActionRollPanelSideView[] sideViews =
            prefab.GetComponentsInChildren<BattleActionRollPanelSideView>(true);
        Require(sideViews.Length == 2, "行动 Roll 点面板必须包含左右两个 View。");
        for (int index = 0; index < sideViews.Length; index++)
        {
            ValidateReferences(
                new SerializedObject(sideViews[index]),
                "cardViewport",
                "cardPreviewPrefab",
                "rollFrameImage",
                "rangeBadgeImage",
                "rangeText",
                "rolledPointText",
                "rollFrameSprite",
                "rangeFont",
                "rolledPointFont"
            );
        }
    }

    static void EnsureMissingPrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        try
        {
            CreateAndSavePrefab();
        }
        catch (Exception exception)
        {
            Debug.LogError("自动生成行动 Roll 点面板失败：" + exception);
        }
    }

    static void CreateAndSavePrefab()
    {
        EnsureAssetFolder("Assets/Resources");
        EnsureAssetFolder("Assets/Resources/UI");

        TMP_FontAsset font =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite allyRollFrame =
            AssetDatabase.LoadAssetAtPath<Sprite>(AllyRollFramePath);
        Sprite enemyRollFrame =
            AssetDatabase.LoadAssetAtPath<Sprite>(EnemyRollFramePath);
        GameObject cardUIPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(CardUIPrefabPath);
        Require(font != null, "找不到行动 Roll 点面板字体：" + FontPath);
        Require(
            allyRollFrame != null,
            "找不到友方行动 Roll 外框：" + AllyRollFramePath
        );
        Require(
            enemyRollFrame != null,
            "找不到敌方行动 Roll 外框：" + EnemyRollFramePath
        );
        Require(cardUIPrefab != null, "找不到行动卡牌 UI：" + CardUIPrefabPath);

        BattleCardUIView cardPreviewPrefab =
            cardUIPrefab.GetComponent<BattleCardUIView>();
        Require(
            cardPreviewPrefab != null,
            "行动卡牌 UI 预设体缺少 BattleCardUIView。"
        );

        GameObject root = new GameObject(
            "BattleActionRollPanel",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(BattleActionRollPanelHost)
        );
        try
        {
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "Battle_Environment";
            canvas.sortingOrder = 32765;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            GameObject safeAreaObject = new GameObject(
                "SafeAreaRoot",
                typeof(RectTransform),
                typeof(CanvasGroup)
            );
            RectTransform safeAreaRoot =
                safeAreaObject.GetComponent<RectTransform>();
            safeAreaRoot.SetParent(root.transform, false);
            Stretch(safeAreaRoot);
            CanvasGroup panelCanvasGroup =
                safeAreaObject.GetComponent<CanvasGroup>();
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;

            BattleActionRollPanelSideView allySideView = CreateSideView(
                safeAreaRoot,
                "AllyActionRoll",
                false,
                font,
                allyRollFrame,
                cardPreviewPrefab
            );
            BattleActionRollPanelSideView enemySideView = CreateSideView(
                safeAreaRoot,
                "EnemyActionRoll",
                true,
                font,
                enemyRollFrame,
                cardPreviewPrefab
            );

            BattleActionRollPanelHost host =
                root.GetComponent<BattleActionRollPanelHost>();
            SerializedObject serializedHost = new SerializedObject(host);
            SetObjectReference(serializedHost, "overlayCanvas", canvas);
            SetObjectReference(serializedHost, "safeAreaRoot", safeAreaRoot);
            SetObjectReference(
                serializedHost,
                "panelCanvasGroup",
                panelCanvasGroup
            );
            SetObjectReference(serializedHost, "allySideView", allySideView);
            SetObjectReference(serializedHost, "enemySideView", enemySideView);
            SetFloat(serializedHost, "followHorizontalGap", 24f);
            SetFloat(serializedHost, "followVerticalGap", 18f);
            SetFloat(serializedHost, "topHudReservedHeight", 112f);
            SetBool(serializedHost, "clampToSafeArea", true);
            serializedHost.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static BattleActionRollPanelSideView CreateSideView(
        RectTransform parent,
        string objectName,
        bool enemySide,
        TMP_FontAsset font,
        Sprite rollFrame,
        BattleCardUIView cardPreviewPrefab
    )
    {
        GameObject sideObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(BattleActionRollPanelSideView)
        );
        RectTransform sideRect = sideObject.GetComponent<RectTransform>();
        sideRect.SetParent(parent, false);
        sideRect.anchorMin = new Vector2(0.5f, 0.5f);
        sideRect.anchorMax = sideRect.anchorMin;
        sideRect.pivot = new Vector2(enemySide ? 0f : 1f, 0f);
        sideRect.anchoredPosition = new Vector2(
            enemySide ? 24f : -24f,
            18f
        );
        sideRect.sizeDelta = new Vector2(440f, 340f);

        RectTransform cardViewport = CreateRect(
            sideRect,
            "ActionCardViewport",
            new Vector2(enemySide ? 94f : -94f, -170f),
            new Vector2(210f, 310f)
        );

        Image rollFrameImage = CreateImage(
            sideRect,
            "RollFrame",
            rollFrame,
            Color.white,
            new Vector2(enemySide ? -122f : 122f, -174f),
            new Vector2(164f, 164f)
        );
        rollFrameImage.preserveAspect = true;

        Image rangeBadgeImage = CreateImage(
            sideRect,
            "RangeBadge",
            null,
            new Color32(18, 14, 19, 238),
            new Vector2(enemySide ? -122f : 122f, -77f),
            new Vector2(142f, 46f)
        );
        Outline rangeOutline =
            rangeBadgeImage.gameObject.AddComponent<Outline>();
        rangeOutline.effectColor = enemySide
            ? new Color32(235, 72, 117, 230)
            : new Color32(218, 164, 61, 230);
        rangeOutline.effectDistance = new Vector2(2f, -2f);

        TMP_Text rangeText = CreateText(
            rangeBadgeImage.rectTransform,
            "RangeText",
            "1~12",
            30f,
            new Color32(250, 241, 222, 255),
            font
        );
        TMP_Text rolledPointText = CreateText(
            rollFrameImage.rectTransform,
            "RolledPointText",
            "8",
            68f,
            Color.white,
            font
        );
        Outline pointOutline =
            rolledPointText.gameObject.AddComponent<Outline>();
        pointOutline.effectColor = new Color32(22, 12, 19, 230);
        pointOutline.effectDistance = new Vector2(2f, -2f);

        BattleActionRollPanelSideView view =
            sideObject.GetComponent<BattleActionRollPanelSideView>();
        SerializedObject serializedView = new SerializedObject(view);
        SetObjectReference(serializedView, "cardViewport", cardViewport);
        SetObjectReference(
            serializedView,
            "cardPreviewPrefab",
            cardPreviewPrefab
        );
        SetObjectReference(
            serializedView,
            "rollFrameImage",
            rollFrameImage
        );
        SetObjectReference(
            serializedView,
            "rangeBadgeImage",
            rangeBadgeImage
        );
        SetObjectReference(serializedView, "rangeText", rangeText);
        SetObjectReference(
            serializedView,
            "rolledPointText",
            rolledPointText
        );
        SetObjectReference(serializedView, "rollFrameSprite", rollFrame);
        SetObjectReference(serializedView, "rangeBadgeSprite", null);
        SetObjectReference(serializedView, "rangeFont", font);
        SetObjectReference(serializedView, "rolledPointFont", font);
        serializedView.ApplyModifiedPropertiesWithoutUndo();

        sideObject.SetActive(false);
        return view;
    }

    static RectTransform CreateRect(
        RectTransform parent,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    static Image CreateImage(
        RectTransform parent,
        string objectName,
        Sprite sprite,
        Color color,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        RectTransform rect = CreateRect(
            parent,
            objectName,
            anchoredPosition,
            size
        );
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static TMP_Text CreateText(
        RectTransform parent,
        string objectName,
        string previewText,
        float fontSize,
        Color color,
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
        Stretch(rect);

        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>();
        text.text = previewText;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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

    static void SetFloat(
        SerializedObject serializedObject,
        string propertyName,
        float value
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

        property.floatValue = value;
    }

    static void SetBool(
        SerializedObject serializedObject,
        string propertyName,
        bool value
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

        property.boolValue = value;
    }

    static void ValidateReferences(
        SerializedObject serializedObject,
        params string[] propertyNames
    )
    {
        for (int index = 0; index < propertyNames.Length; index++)
        {
            SerializedProperty property =
                serializedObject.FindProperty(propertyNames[index]);
            Require(
                property != null && property.objectReferenceValue != null,
                serializedObject.targetObject.name +
                " 的预设体引用为空：" + propertyNames[index]
            );
        }
    }

    static void EnsureAssetFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
#endif
