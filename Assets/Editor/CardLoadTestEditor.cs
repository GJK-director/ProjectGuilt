using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(CardLoadTest))]
public sealed class CardLoadTestEditor : Editor
{
    private SerializedProperty testModeProperty;
    private BattleTestModeDropdown testModeDropdown;

    private void OnEnable()
    {
        testModeProperty = serializedObject.FindProperty("testMode");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(
                "Script",
                MonoScript.FromMonoBehaviour((CardLoadTest)target),
                typeof(CardLoadTest),
                false
            );
        }

        DrawSearchableTestMode();
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "testMode"
        );
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSearchableTestMode()
    {
        if (testModeProperty == null)
        {
            EditorGUILayout.HelpBox(
                "CardLoadTestEditor找不到testMode序列化字段。",
                MessageType.Error
            );
            return;
        }

        BattleTestMode currentMode =
            (BattleTestMode)testModeProperty.intValue;
        Rect buttonRect = EditorGUILayout.GetControlRect();
        buttonRect = EditorGUI.PrefixLabel(
            buttonRect,
            new GUIContent("Test Mode")
        );

        if (!EditorGUI.DropdownButton(
                buttonRect,
                new GUIContent(GetDisplayLabel(currentMode)),
                FocusType.Keyboard
            ))
        {
            return;
        }

        testModeDropdown = new BattleTestModeDropdown(
            new AdvancedDropdownState(),
            SelectTestMode
        );
        testModeDropdown.Show(buttonRect);
    }

    private void SelectTestMode(BattleTestMode mode)
    {
        serializedObject.Update();
        testModeProperty.intValue = (int)mode;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private static string GetDisplayLabel(BattleTestMode mode)
    {
        return ((int)mode) + "  " +
            ObjectNames.NicifyVariableName(mode.ToString());
    }

    private sealed class BattleTestModeDropdown : AdvancedDropdown
    {
        private readonly Action<BattleTestMode> onSelected;

        public BattleTestModeDropdown(
            AdvancedDropdownState state,
            Action<BattleTestMode> onSelected
        ) : base(state)
        {
            this.onSelected = onSelected;
            minimumSize = new Vector2(520f, 360f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem(
                "搜索测试编号、枚举名称或英文关键词"
            );
            Array values = Enum.GetValues(typeof(BattleTestMode));
            foreach (BattleTestMode mode in values)
            {
                string enumName = mode.ToString();
                AdvancedDropdownItem item = new BattleTestModeItem(
                    (int)mode + "  " +
                    ObjectNames.NicifyVariableName(enumName) +
                    "  [" + enumName + "]",
                    mode
                );
                root.AddChild(item);
            }
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            BattleTestModeItem modeItem = item as BattleTestModeItem;
            if (modeItem != null)
            {
                onSelected?.Invoke(modeItem.Mode);
            }
        }
    }

    private sealed class BattleTestModeItem : AdvancedDropdownItem
    {
        public BattleTestMode Mode { get; private set; }

        public BattleTestModeItem(string name, BattleTestMode mode)
            : base(name)
        {
            Mode = mode;
        }
    }
}
