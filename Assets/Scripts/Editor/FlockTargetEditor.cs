using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlockTarget))]
public class FlockTargetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var flockTarget = (FlockTarget)target;
        var label = flockTarget.IsActive ? "Active Target" : "Make Active";

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(flockTarget.IsActive))
        {
            if (GUILayout.Button(label))
            {
                flockTarget.MakeActive();
                EditorUtility.SetDirty(flockTarget);
            }
        }
    }
}
