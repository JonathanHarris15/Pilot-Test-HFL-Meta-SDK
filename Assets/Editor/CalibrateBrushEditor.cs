using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CalibrateBrush))]
public class CalibrateBrushEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Standard fields.
        DrawDefaultInspector();

        CalibrateBrush myScript = (CalibrateBrush)target;

        EditorGUILayout.Space(10);

        // --- Brush calibration ---
        GUI.backgroundColor = new Color(0.7f, 0.9f, 1.0f);
        if (GUILayout.Button("Calibrate Brush", GUILayout.Height(40)))
        {
            myScript.PerformCalibration();
        }
        GUI.backgroundColor = Color.white;

        // --- X-axis fine tuning ---
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Fine Tune X-Axis Offset", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Shift Left (-0.18)"))
        {
            Undo.RecordObject(myScript, "Shift Offset Left");
            myScript.hand_offset.x -= 0.18f;
        }

        if (GUILayout.Button("Shift Right (+0.18)"))
        {
            Undo.RecordObject(myScript, "Shift Offset Right");
            myScript.hand_offset.x += 0.18f;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Data recording lives in the Experiment Control Panel " +
            "(menu: Experiment > Control Panel).", MessageType.Info);
    }
}
