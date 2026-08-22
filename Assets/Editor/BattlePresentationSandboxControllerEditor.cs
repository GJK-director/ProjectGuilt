using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattlePresentationSandboxController))]
public sealed class BattlePresentationSandboxControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            BattlePresentationSandboxController controller =
                (BattlePresentationSandboxController)target;
            if (GUILayout.Button("Run Selected Test"))
            {
                controller.RunSelectedTest();
            }

            if (GUILayout.Button("Reset"))
            {
                controller.ResetSelectedTest();
            }
        }
    }
}
