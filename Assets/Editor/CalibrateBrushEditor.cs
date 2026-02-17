using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CalibrateBrush))]
public class CalibrateBrushEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. Setup the serialized object for modification
        serializedObject.Update();
        CalibrateBrush myScript = (CalibrateBrush)target;

        // 2. Iterate through all fields to draw the "Default Inspector" manually
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            using (new EditorGUI.DisabledScope("m_Script" == prop.propertyPath))
            {
                EditorGUILayout.PropertyField(prop, true);
            }

            // Check if we just drew the "dataFilePath" field
            if (prop.name == "dataFilePath")
            {
                EditorGUILayout.Space(2); // Small spacing between text box and button

                if (GUILayout.Button("Pick File Path", GUILayout.Height(20)))
                {
                    string path = EditorUtility.OpenFilePanel("Data Collection Path", "", "csv,xlsx");
                    if (!string.IsNullOrEmpty(path))
                    {
                        // Assigning to the property directly supports Undo/Redo automatically
                        prop.stringValue = path;

                        // Force Unity to save the change
                        EditorUtility.SetDirty(target);
                    }
                }

                EditorGUILayout.Space(10); // Spacing after the button before the next field
            }
        }

        // Apply any changes made to properties (like the file path)
        serializedObject.ApplyModifiedProperties();


        // 3. DRAW CUSTOM FOOTER CONTROLS

        EditorGUILayout.Space(10);

        // --- EXISTING CALIBRATION BUTTON ---
        GUI.backgroundColor = new Color(0.7f, 0.9f, 1.0f);
        if (GUILayout.Button("Calibrate Brush", GUILayout.Height(40)))
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

        EditorGUILayout.Space(5);
        if (GUILayout.Button("Preform Recording", GUILayout.Height(20))){
            myScript.PerformRecording();
        }

        EditorGUILayout.Space(5);
        if (GUILayout.Button("Add data break", GUILayout.Height(20)))
        {
            myScript.NextMeasurement();
        }

        // --- EXISTING FINE-TUNING CONTROLS ---
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Fine Tune X-Axis Offset", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Shift Left (-0.1)"))
        {
            Undo.RecordObject(myScript, "Shift Offset Left");
            myScript.hand_offset.x -= 0.1f;
        }

        if (GUILayout.Button("Shift Right (+0.1)"))
        {
            Undo.RecordObject(myScript, "Shift Offset Right");
            myScript.hand_offset.x += 0.1f;
        }

        EditorGUILayout.EndHorizontal();

        // Ensure the editor updates during play mode so the button state switches automatically
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}