using UnityEngine;

public static class BattleCharacterStatusWorldFollowMode74Tests
{
    private sealed class TransformSnapshot
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 sizeDelta;
        public Vector3 localScale;
        public Quaternion localRotation;
    }

    private sealed class Fixture
    {
        public GameObject cameraObject;
        public Camera worldCamera;
        public GameObject canvasObject;
        public Canvas canvas;
        public RectTransform statusRoot;
        public CanvasGroup visibility;
        public RectTransform headGroup;
        public RectTransform footGroup;
        public RectTransform selfGroup;
        public GameObject worldRoot;
        public Transform headAnchor;
        public Transform footAnchor;
        public Transform centerAnchor;
        public BattleCharacterStatusWorldFollower follower;
    }

    private static readonly string[] TestNames =
    {
        "Overlay Canvas下世界锚点可转换为UI父级本地坐标",
        "HeadUIAnchor正确更新HeadSlotGroup",
        "FootUIAnchor正确更新FootStatusGroup",
        "CenterAnchor正确更新SelfActionDropZone",
        "三个组独立计算位置",
        "Head Offset正确叠加",
        "Foot Offset正确叠加",
        "Center Offset正确叠加",
        "修改世界角色位置后RefreshNow更新UI",
        "修改Main Camera位置后RefreshNow更新UI",
        "只修改anchoredPosition",
        "不修改sizeDelta",
        "不修改localScale",
        "不修改localRotation",
        "不修改anchorMin和anchorMax",
        "inactive的SelfActionDropZone不会被自动激活",
        "centerAnchor为空时Head和Foot仍正常更新",
        "headAnchor为空时Foot仍正常更新",
        "footAnchor为空时Head仍正常更新",
        "worldCamera为空时安全失败",
        "targetCanvas为空且父级存在Canvas时可自动取得",
        "Bind运行时接口会立即刷新",
        "SetWorldAnchors后使用新锚点",
        "ClearWorldAnchors后不再更新",
        "IsBound状态正确",
        "敌人与友方使用同一计算结果",
        "SpriteRenderer Flip X不会改变锚点投影结果",
        "Screen Space Camera模式使用Canvas worldCamera",
        "摄像机后方锚点不会产生反向异常坐标",
        "OnDisable后不修改任何战斗数据"
    };

    public static bool Run()
    {
        Debug.Log(
            "===== BattleCharacterStatusWorldFollowBasic 模式74开始 ====="
        );
        bool[] results = new bool[30];
        Fixture fixture = CreateFixture();
        try
        {
            RunProjectionAndInvariantTests(results, fixture);
            RunBindingAndEdgeTests(results, fixture);
        }
        finally
        {
            DestroyFixture(fixture);
        }

        bool allPassed = true;
        for (int index = 0; index < results.Length; index++)
        {
            Debug.Log(
                "模式74 测试" + (index + 1) + " " +
                TestNames[index] + "：" + results[index]
            );
            allPassed &= results[index];
        }
        Debug.Log(
            "===== BattleCharacterStatusWorldFollowBasic 模式74基础测试结束 ====="
        );
        return allPassed;
    }

    private static void RunProjectionAndInvariantTests(
        bool[] r,
        Fixture f
    )
    {
        f.follower.ConfigureOffsetsForTesting(
            Vector2.zero,
            Vector2.zero,
            Vector2.zero
        );
        f.follower.RefreshNow();

        Vector2 expectedHead;
        Vector2 expectedFoot;
        Vector2 expectedCenter;
        bool convertedHead = f.follower.TryGetAnchorLocalPosition(
            f.headAnchor,
            f.headGroup,
            out expectedHead
        );
        bool convertedFoot = f.follower.TryGetAnchorLocalPosition(
            f.footAnchor,
            f.footGroup,
            out expectedFoot
        );
        bool convertedCenter = f.follower.TryGetAnchorLocalPosition(
            f.centerAnchor,
            f.selfGroup,
            out expectedCenter
        );
        r[0] = convertedHead && f.follower.LastCanvasEventCamera == null;
        r[1] = Approximately(f.headGroup.anchoredPosition, expectedHead);
        r[2] = convertedFoot &&
            Approximately(f.footGroup.anchoredPosition, expectedFoot);
        r[3] = convertedCenter &&
            Approximately(f.selfGroup.anchoredPosition, expectedCenter);
        r[4] = !Approximately(expectedHead, expectedFoot) &&
            !Approximately(expectedHead, expectedCenter) &&
            !Approximately(expectedFoot, expectedCenter);

        TransformSnapshot headSnapshot = Capture(f.headGroup);
        TransformSnapshot footSnapshot = Capture(f.footGroup);
        TransformSnapshot selfSnapshot = Capture(f.selfGroup);
        Vector2 headOffset = new Vector2(17f, 23f);
        Vector2 footOffset = new Vector2(-11f, -19f);
        Vector2 centerOffset = new Vector2(31f, -7f);
        f.follower.ConfigureOffsetsForTesting(
            headOffset,
            footOffset,
            centerOffset
        );
        f.follower.RefreshNow();
        r[5] = Approximately(
            f.headGroup.anchoredPosition,
            expectedHead + headOffset
        );
        r[6] = Approximately(
            f.footGroup.anchoredPosition,
            expectedFoot + footOffset
        );
        r[7] = Approximately(
            f.selfGroup.anchoredPosition,
            expectedCenter + centerOffset
        );

        Vector2 beforeWorldMove = f.headGroup.anchoredPosition;
        f.worldRoot.transform.position += new Vector3(0.75f, 0.35f, 0f);
        f.follower.RefreshNow();
        r[8] = !Approximately(
            beforeWorldMove,
            f.headGroup.anchoredPosition
        );

        Vector2 beforeCameraMove = f.headGroup.anchoredPosition;
        f.worldCamera.transform.position += new Vector3(0.5f, 0f, 0f);
        f.follower.RefreshNow();
        r[9] = !Approximately(
            beforeCameraMove,
            f.headGroup.anchoredPosition
        );

        bool headInvariant = MatchesNonPosition(f.headGroup, headSnapshot);
        bool footInvariant = MatchesNonPosition(f.footGroup, footSnapshot);
        bool selfInvariant = MatchesNonPosition(f.selfGroup, selfSnapshot);
        r[10] = headInvariant && footInvariant && selfInvariant;
        r[11] =
            Approximately(f.headGroup.sizeDelta, headSnapshot.sizeDelta) &&
            Approximately(f.footGroup.sizeDelta, footSnapshot.sizeDelta) &&
            Approximately(f.selfGroup.sizeDelta, selfSnapshot.sizeDelta);
        r[12] =
            Approximately(f.headGroup.localScale, headSnapshot.localScale) &&
            Approximately(f.footGroup.localScale, footSnapshot.localScale) &&
            Approximately(f.selfGroup.localScale, selfSnapshot.localScale);
        r[13] =
            Quaternion.Angle(
                f.headGroup.localRotation,
                headSnapshot.localRotation
            ) < 0.001f &&
            Quaternion.Angle(
                f.footGroup.localRotation,
                footSnapshot.localRotation
            ) < 0.001f &&
            Quaternion.Angle(
                f.selfGroup.localRotation,
                selfSnapshot.localRotation
            ) < 0.001f;
        r[14] =
            Approximately(f.headGroup.anchorMin, headSnapshot.anchorMin) &&
            Approximately(f.headGroup.anchorMax, headSnapshot.anchorMax) &&
            Approximately(f.footGroup.anchorMin, footSnapshot.anchorMin) &&
            Approximately(f.footGroup.anchorMax, footSnapshot.anchorMax) &&
            Approximately(f.selfGroup.anchorMin, selfSnapshot.anchorMin) &&
            Approximately(f.selfGroup.anchorMax, selfSnapshot.anchorMax);
        r[15] = !f.selfGroup.gameObject.activeSelf;
    }

    private static void RunBindingAndEdgeTests(bool[] r, Fixture f)
    {
        f.worldCamera.transform.position = new Vector3(0f, 0f, -10f);
        f.worldRoot.transform.position = Vector3.zero;
        f.follower.ConfigureOffsetsForTesting(
            Vector2.zero,
            Vector2.zero,
            Vector2.zero
        );
        f.follower.Bind(
            f.worldCamera,
            f.canvas,
            f.headAnchor,
            f.footAnchor,
            null
        );
        Vector2 oldHead = f.headGroup.anchoredPosition;
        Vector2 oldFoot = f.footGroup.anchoredPosition;
        f.headAnchor.position += new Vector3(0.3f, 0f, 0f);
        f.footAnchor.position += new Vector3(-0.25f, 0f, 0f);
        f.follower.RefreshNow();
        r[16] = !Approximately(oldHead, f.headGroup.anchoredPosition) &&
            !Approximately(oldFoot, f.footGroup.anchoredPosition);

        f.follower.SetWorldAnchors(null, f.footAnchor, f.centerAnchor);
        oldFoot = f.footGroup.anchoredPosition;
        f.footAnchor.position += new Vector3(0.2f, 0f, 0f);
        f.follower.RefreshNow();
        r[17] = !Approximately(oldFoot, f.footGroup.anchoredPosition);

        f.follower.SetWorldAnchors(f.headAnchor, null, f.centerAnchor);
        oldHead = f.headGroup.anchoredPosition;
        f.headAnchor.position += new Vector3(0.2f, 0f, 0f);
        f.follower.RefreshNow();
        r[18] = !Approximately(oldHead, f.headGroup.anchoredPosition);

        Vector2 safeHead = f.headGroup.anchoredPosition;
        f.follower.SetWorldCameraForTesting(null);
        f.headAnchor.position += new Vector3(0.2f, 0f, 0f);
        f.follower.RefreshNow();
        r[19] = Approximately(safeHead, f.headGroup.anchoredPosition);

        RectTransform autoRoot = CreateRect(
            "Mode74AutoCanvasRoot",
            f.canvasObject.transform
        );
        BattleCharacterStatusWorldFollower autoFollower =
            autoRoot.gameObject.AddComponent<
                BattleCharacterStatusWorldFollower
            >();
        RectTransform autoHead = CreateRect("AutoHead", autoRoot);
        autoFollower.ConfigureUIRootsForTesting(autoHead, null, null);
        autoFollower.SetWorldCameraForTesting(f.worldCamera);
        autoFollower.SetWorldAnchors(f.headAnchor, null, null);
        autoFollower.RefreshNow();
        r[20] = autoFollower.ResolvedTargetCanvas == f.canvas;

        f.headGroup.anchoredPosition = new Vector2(999f, 999f);
        f.follower.Bind(
            f.worldCamera,
            f.canvas,
            f.headAnchor,
            f.footAnchor,
            f.centerAnchor
        );
        r[21] = !Approximately(
            f.headGroup.anchoredPosition,
            new Vector2(999f, 999f)
        );

        GameObject newAnchorRoot = new GameObject("Mode74NewAnchors");
        Transform newHead = CreateAnchor(
            "NewHead",
            newAnchorRoot.transform,
            new Vector3(1.8f, 2.7f, 0f)
        );
        Transform newFoot = CreateAnchor(
            "NewFoot",
            newAnchorRoot.transform,
            new Vector3(1.1f, -1.9f, 0f)
        );
        Transform newCenter = CreateAnchor(
            "NewCenter",
            newAnchorRoot.transform,
            new Vector3(1.4f, 0.5f, 0f)
        );
        Vector2 beforeNewAnchors = f.headGroup.anchoredPosition;
        f.follower.SetWorldAnchors(newHead, newFoot, newCenter);
        r[22] = !Approximately(
            beforeNewAnchors,
            f.headGroup.anchoredPosition
        );

        bool wasBound = f.follower.IsBound;
        f.follower.ClearWorldAnchors();
        Vector2 beforeClearRefresh = f.headGroup.anchoredPosition;
        newHead.position += Vector3.right;
        f.follower.RefreshNow();
        r[23] = Approximately(
            beforeClearRefresh,
            f.headGroup.anchoredPosition
        );
        r[24] = wasBound && !f.follower.IsBound;

        RectTransform enemyRoot = CreateRect(
            "Mode74EnemyStatusRoot",
            f.canvasObject.transform
        );
        RectTransform enemyHead = CreateRect("EnemyHead", enemyRoot);
        RectTransform enemyFoot = CreateRect("EnemyFoot", enemyRoot);
        RectTransform enemySelf = CreateRect("EnemySelf", enemyRoot);
        BattleCharacterStatusWorldFollower enemyFollower =
            enemyRoot.gameObject.AddComponent<
                BattleCharacterStatusWorldFollower
            >();
        enemyFollower.ConfigureUIRootsForTesting(
            enemyHead,
            enemyFoot,
            enemySelf
        );
        enemyFollower.Bind(
            f.worldCamera,
            f.canvas,
            newHead,
            newFoot,
            newCenter
        );
        f.follower.SetWorldAnchors(newHead, newFoot, newCenter);
        r[25] = Approximately(
            f.headGroup.anchoredPosition,
            enemyHead.anchoredPosition
        ) && Approximately(
            f.footGroup.anchoredPosition,
            enemyFoot.anchoredPosition
        );

        SpriteRenderer renderer =
            newAnchorRoot.AddComponent<SpriteRenderer>();
        Vector2 beforeFlip = f.headGroup.anchoredPosition;
        renderer.flipX = true;
        f.follower.RefreshNow();
        r[26] = Approximately(beforeFlip, f.headGroup.anchoredPosition);

        GameObject uiCameraObject = new GameObject(
            "Mode74UICamera",
            typeof(Camera)
        );
        Camera uiCamera = uiCameraObject.GetComponent<Camera>();
        uiCamera.transform.position = new Vector3(0f, 0f, -10f);
        GameObject cameraCanvasObject = new GameObject(
            "Mode74CameraCanvas",
            typeof(RectTransform),
            typeof(Canvas)
        );
        Canvas cameraCanvas = cameraCanvasObject.GetComponent<Canvas>();
        cameraCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        cameraCanvas.worldCamera = uiCamera;
        RectTransform cameraStatusRoot = CreateRect(
            "CameraStatusRoot",
            cameraCanvasObject.transform
        );
        RectTransform cameraHead = CreateRect(
            "CameraHead",
            cameraStatusRoot
        );
        BattleCharacterStatusWorldFollower cameraFollower =
            cameraStatusRoot.gameObject.AddComponent<
                BattleCharacterStatusWorldFollower
            >();
        cameraFollower.ConfigureUIRootsForTesting(cameraHead, null, null);
        cameraFollower.Bind(
            f.worldCamera,
            cameraCanvas,
            newHead,
            null,
            null
        );
        r[27] = cameraFollower.LastCanvasEventCamera == uiCamera;

        f.follower.Bind(
            f.worldCamera,
            f.canvas,
            newHead,
            newFoot,
            newCenter
        );
        Vector2 beforeBehind = f.headGroup.anchoredPosition;
        float originalHeadZ = newHead.position.z;
        float originalFootZ = newFoot.position.z;
        float originalCenterZ = newCenter.position.z;
        newHead.position = new Vector3(
            newHead.position.x,
            newHead.position.y,
            -20f
        );
        newFoot.position = new Vector3(
            newFoot.position.x,
            newFoot.position.y,
            -20f
        );
        newCenter.position = new Vector3(
            newCenter.position.x,
            newCenter.position.y,
            -20f
        );
        f.follower.RefreshNow();
        bool hiddenBehind = Mathf.Approximately(f.visibility.alpha, 0f) &&
            Approximately(beforeBehind, f.headGroup.anchoredPosition);
        newHead.position = new Vector3(
            newHead.position.x,
            newHead.position.y,
            originalHeadZ
        );
        newFoot.position = new Vector3(
            newFoot.position.x,
            newFoot.position.y,
            originalFootZ
        );
        newCenter.position = new Vector3(
            newCenter.position.x,
            newCenter.position.y,
            originalCenterZ
        );
        f.follower.RefreshNow();
        r[28] = hiddenBehind && Mathf.Approximately(f.visibility.alpha, 1f);

        CharacterData character = new CharacterData(
            "mode74_character",
            30,
            5,
            5
        );
        int hpBeforeDisable = character.currentHP;
        f.follower.enabled = false;
        r[29] = character.currentHP == hpBeforeDisable;

        Object.Destroy(autoRoot.gameObject);
        Object.Destroy(enemyRoot.gameObject);
        Object.Destroy(newAnchorRoot);
        Object.Destroy(cameraCanvasObject);
        Object.Destroy(uiCameraObject);
    }

    private static Fixture CreateFixture()
    {
        Fixture f = new Fixture();
        f.cameraObject = new GameObject(
            "Mode74WorldCamera",
            typeof(Camera)
        );
        f.worldCamera = f.cameraObject.GetComponent<Camera>();
        f.worldCamera.orthographic = true;
        f.worldCamera.orthographicSize = 5f;
        f.worldCamera.transform.position = new Vector3(0f, 0f, -10f);

        f.canvasObject = new GameObject(
            "Mode74OverlayCanvas",
            typeof(RectTransform),
            typeof(Canvas)
        );
        f.canvas = f.canvasObject.GetComponent<Canvas>();
        f.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        f.statusRoot = CreateRect(
            "Mode74CharacterStatus",
            f.canvasObject.transform
        );
        f.visibility = f.statusRoot.gameObject.AddComponent<CanvasGroup>();
        f.headGroup = CreateRect("HeadSlotGroup", f.statusRoot);
        f.footGroup = CreateRect("FootStatusGroup", f.statusRoot);
        f.selfGroup = CreateRect("SelfActionDropZone", f.statusRoot);
        ConfigureDistinctRect(f.headGroup, new Vector2(160f, 80f));
        ConfigureDistinctRect(f.footGroup, new Vector2(180f, 70f));
        ConfigureDistinctRect(f.selfGroup, new Vector2(120f, 120f));
        f.selfGroup.gameObject.SetActive(false);

        f.worldRoot = new GameObject("Mode74WorldCharacter");
        f.headAnchor = CreateAnchor(
            "HeadUIAnchor",
            f.worldRoot.transform,
            new Vector3(-1.25f, 2.1f, 0f)
        );
        f.footAnchor = CreateAnchor(
            "FootUIAnchor",
            f.worldRoot.transform,
            new Vector3(-0.9f, -1.6f, 0f)
        );
        f.centerAnchor = CreateAnchor(
            "CenterAnchor",
            f.worldRoot.transform,
            new Vector3(0.4f, 0.25f, 0f)
        );

        f.follower = f.statusRoot.gameObject.AddComponent<
            BattleCharacterStatusWorldFollower
        >();
        f.follower.ConfigureUIRootsForTesting(
            f.headGroup,
            f.footGroup,
            f.selfGroup
        );
        f.follower.ConfigureVisibilityForTesting(f.visibility, true);
        Canvas.ForceUpdateCanvases();
        f.follower.Bind(
            f.worldCamera,
            f.canvas,
            f.headAnchor,
            f.footAnchor,
            f.centerAnchor
        );
        return f;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject value = new GameObject(name, typeof(RectTransform));
        value.transform.SetParent(parent, false);
        RectTransform rect = value.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return rect;
    }

    private static void ConfigureDistinctRect(
        RectTransform rect,
        Vector2 size
    )
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.localScale = new Vector3(0.9f, 1.1f, 1f);
        rect.localRotation = Quaternion.Euler(0f, 0f, 3f);
    }

    private static Transform CreateAnchor(
        string name,
        Transform parent,
        Vector3 localPosition
    )
    {
        GameObject anchor = new GameObject(name);
        anchor.transform.SetParent(parent, false);
        anchor.transform.localPosition = localPosition;
        return anchor.transform;
    }

    private static TransformSnapshot Capture(RectTransform rect)
    {
        return new TransformSnapshot
        {
            anchorMin = rect.anchorMin,
            anchorMax = rect.anchorMax,
            sizeDelta = rect.sizeDelta,
            localScale = rect.localScale,
            localRotation = rect.localRotation
        };
    }

    private static bool MatchesNonPosition(
        RectTransform rect,
        TransformSnapshot snapshot
    )
    {
        return Approximately(rect.anchorMin, snapshot.anchorMin) &&
            Approximately(rect.anchorMax, snapshot.anchorMax) &&
            Approximately(rect.sizeDelta, snapshot.sizeDelta) &&
            Approximately(rect.localScale, snapshot.localScale) &&
            Quaternion.Angle(
                rect.localRotation,
                snapshot.localRotation
            ) < 0.001f;
    }

    private static void DestroyFixture(Fixture f)
    {
        if (f == null)
        {
            return;
        }
        if (f.canvasObject != null)
        {
            Object.Destroy(f.canvasObject);
        }
        if (f.worldRoot != null)
        {
            Object.Destroy(f.worldRoot);
        }
        if (f.cameraObject != null)
        {
            Object.Destroy(f.cameraObject);
        }
    }

    private static bool Approximately(Vector2 left, Vector2 right)
    {
        return Vector2.SqrMagnitude(left - right) < 0.01f;
    }

    private static bool Approximately(Vector3 left, Vector3 right)
    {
        return Vector3.SqrMagnitude(left - right) < 0.01f;
    }
}
