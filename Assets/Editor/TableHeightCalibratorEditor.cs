using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TableHeightCalibrator))]
public class TableHeightCalibratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TableHeightCalibrator cal = (TableHeightCalibrator)target;

        // --- Workflow instructions ---
        EditorGUILayout.HelpBox(
            "TABLE HEIGHT CALIBRATION\n\n" +
            "1. Enter Play mode and put on the headset.\n" +
            "2. Rest the real RIGHT hand flat on the real table.\n" +
            "3. Click 'Calibrate Table Height' below.\n\n" +
            "The virtual table snaps vertically (Y only) to the tracked hand. " +
            "X and Z are never changed.",
            MessageType.Info);

        EditorGUILayout.Space(6);

        // --- Reference + offset fields (Table, Hand Reference, Contact Offset) ---
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8);

        // --- Live readout (updates every frame in Play mode) ---
        EditorGUILayout.LabelField("Live Readout", EditorStyles.boldLabel);
        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.LabelField("Tracked hand height (Y)",
                cal.handReference != null ? cal.MeasuredHandHeight.ToString("F4") + " m"
                                          : "— assign Hand Reference —");

            EditorGUILayout.LabelField("Contact offset", cal.contactOffset.ToString("F4") + " m");
            EditorGUILayout.LabelField("→ Target table height", cal.TargetTableHeight.ToString("F4") + " m");

            if (cal.table != null)
            {
                EditorGUILayout.LabelField("Current table height (Y)", cal.CurrentTableHeight.ToString("F4") + " m");
                float delta = cal.TargetTableHeight - cal.CurrentTableHeight;
                EditorGUILayout.LabelField("Move needed", delta.ToString("F4") + " m");
            }
            else
            {
                EditorGUILayout.LabelField("Current table height (Y)", "— assign Table —");
            }
        }

        EditorGUILayout.Space(10);

        if (!cal.IsReady)
        {
            EditorGUILayout.HelpBox("Assign both 'Table' and 'Hand Reference' to enable calibration.",
                MessageType.Warning);
        }

        // --- Primary calibrate button ---
        using (new EditorGUI.DisabledScope(!cal.IsReady))
        {
            GUI.backgroundColor = new Color(0.6f, 1.0f, 0.6f);
            if (GUILayout.Button("Calibrate Table Height", GUILayout.Height(44)))
            {
                Undo.RecordObject(cal.table, "Calibrate Table Height");
                if (cal.CalibrateTableHeight())
                {
                    EditorUtility.SetDirty(cal.table);
                    EditorUtility.SetDirty(cal);
                }
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.Space(4);

        // --- Revert button (only meaningful once we have a captured original) ---
        using (new EditorGUI.DisabledScope(cal.table == null || !cal.HasOriginal))
        {
            string label = cal.HasOriginal
                ? $"Reset to Original Height ({cal.OriginalHeight:F4} m)"
                : "Reset to Original Height";
            if (GUILayout.Button(label, GUILayout.Height(22)))
            {
                Undo.RecordObject(cal.table, "Reset Table Height");
                cal.ResetToOriginalHeight();
                EditorUtility.SetDirty(cal.table);
            }
        }

        // Keep the live readout ticking while playing or while the hand moves.
        if (Application.isPlaying)
            Repaint();
    }
}
