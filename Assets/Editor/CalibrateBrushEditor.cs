using UnityEngine;
using UnityEditor; // We need this for custom editors

[CustomEditor(typeof(CalibrateBrush))]
public class CalibrateBrushEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CalibrateBrush myScript = (CalibrateBrush)target;

        EditorGUILayout.Space(10);

        GUI.backgroundColor = new Color(0.7f, 0.9f, 1.0f);
        if (GUILayout.Button("Run Calibration", GUILayout.Height(30)))
        {
            myScript.PerformCalibration();
        }
        GUI.backgroundColor = Color.white;

        //fine-tuning buttons
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Fine Tune X-Axis Offset", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Shift Left (-0.1)"))
        {
            // Register this change for Undo/Redo
            Undo.RecordObject(myScript, "Shift Offset Left");
            myScript.hand_offset.x -= 0.1f;
        }

        if (GUILayout.Button("Shift Right (+0.1)"))
        {
            // Register this change for Undo/Redo
            Undo.RecordObject(myScript, "Shift Offset Right");
            myScript.hand_offset.x += 0.1f;
        }

        // Stop the horizontal layout
        EditorGUILayout.EndHorizontal();
    }
}