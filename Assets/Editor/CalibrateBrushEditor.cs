using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CalibrateBrush))]
public class CalibrateBrushEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CalibrateBrush myScript = (CalibrateBrush)target;

        EditorGUILayout.Space(10);

        // --- EXISTING CALIBRATION BUTTON ---
        GUI.backgroundColor = new Color(0.7f, 0.9f, 1.0f);
        if (GUILayout.Button("Run Calibration", GUILayout.Height(30)))
        {
            myScript.PerformCalibration();
        }
        GUI.backgroundColor = Color.white;

        // --- NEW SCANNING LOGIC ---
        EditorGUILayout.Space(5);

        // This button changes based on whether the sweep is currently active
        if (myScript.IsSweeping)
        {
            GUI.backgroundColor = Color.red; // Visual cue that we are in recording mode
            if (GUILayout.Button("Collect Data", GUILayout.Height(40)))
            {
                myScript.PerformRecording();
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            // Default state
            if (GUILayout.Button("Start Scan", GUILayout.Height(40)))
            {
                myScript.StartScan();
            }
        }

        // --- EXISTING FINE-TUNING CONTROLS ---
        EditorGUILayout.Space(15);
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

        // Ensure the editor updates during play mode so the button state switches automatically
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}