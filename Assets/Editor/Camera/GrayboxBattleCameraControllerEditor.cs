using UnityEditor;

[CustomEditor(typeof(GrayboxBattleCameraController))]
[CanEditMultipleObjects]
public sealed class GrayboxBattleCameraControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty enableIdleSway =
            serializedObject.FindProperty("enableIdleHorizontalSway");
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            bool isScriptReference = property.propertyPath == "m_Script";
            bool isIdleSwayChild = property.propertyPath ==
                "pauseIdleSwayOnInteractiveUI";
            bool disableIdleSwayChild = isIdleSwayChild &&
                enableIdleSway != null &&
                !enableIdleSway.hasMultipleDifferentValues &&
                !enableIdleSway.boolValue;

            EditorGUI.BeginDisabledGroup(
                isScriptReference || disableIdleSwayChild
            );
            EditorGUILayout.PropertyField(property, true);
            EditorGUI.EndDisabledGroup();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
