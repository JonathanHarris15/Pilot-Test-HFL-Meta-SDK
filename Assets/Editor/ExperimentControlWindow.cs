using UnityEngine;
using UnityEditor;

// Dockable operator window for running the experiment.
// Open via the menu:  Experiment > Control Panel
//
// One place for the operator to: enter participant details, watch the 12
// measurements arrive live, re-record by clicking a row, and run every
// calibration (brush, table height, recenter) without hunting through the
// Inspector. Recording also fires from the controller 'Record' button.
public class ExperimentControlWindow : EditorWindow
{
    // --- Palette ---
    private static readonly Color Banner    = new Color(0.16f, 0.22f, 0.32f);
    private static readonly Color Green     = new Color(0.55f, 0.85f, 0.55f);
    private static readonly Color Red        = new Color(1.00f, 0.55f, 0.55f);
    private static readonly Color Blue      = new Color(0.60f, 0.80f, 1.00f);
    private static readonly Color Teal      = new Color(0.55f, 0.88f, 0.85f);
    private static readonly Color Orange    = new Color(1.00f, 0.78f, 0.45f);
    private static readonly Color Grey      = new Color(0.82f, 0.82f, 0.82f);
    private static readonly Color RowCursor = new Color(1.00f, 0.92f, 0.45f);
    private static readonly Color RowDone   = new Color(0.68f, 0.88f, 0.68f);

    private ExperimentDataManager _mgr;
    private CalibrateBrush _brush;
    private TableHeightCalibrator _table;
    private RecenterOnSpace _recenter;

    private string _numberField = "";
    private string _nameField = "";
    private Vector2 _scroll;

    private GUIStyle _card, _sectionTitle, _bannerText, _subLabel;

    [MenuItem("Experiment/Control Panel")]
    public static void Open()
    {
        var w = GetWindow<ExperimentControlWindow>("Experiment Control");
        w.minSize = new Vector2(400, 560);
        w.Show();
    }

    private void OnEnable()
    {
        Refresh();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeChanged;

    private void OnPlayModeChanged(PlayModeStateChange s)
    {
        _mgr = null; _brush = null; _table = null; _recenter = null;
        Refresh();
        Repaint();
    }

    private void Refresh()
    {
        if (_mgr == null) _mgr = Object.FindFirstObjectByType<ExperimentDataManager>();
        if (_mgr != null)
        {
            if (_brush == null)    _brush = _mgr.GetComponent<CalibrateBrush>();
            if (_table == null)    _table = _mgr.GetComponent<TableHeightCalibrator>();
            if (_recenter == null) _recenter = _mgr.GetComponent<RecenterOnSpace>();
        }
    }

    private void EnsureStyles()
    {
        if (_card != null) return; // GUIStyles don't survive domain reloads; rebuild when null.
        _card = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(10, 10, 10, 10), margin = new RectOffset(2, 2, 4, 4) };
        _sectionTitle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        _bannerText = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, normal = { textColor = Color.white }, alignment = TextAnchor.MiddleLeft };
        _subLabel = new GUIStyle(EditorStyles.miniBoldLabel);
    }

    // ====================================================================

    private void OnGUI()
    {
        Refresh();
        EnsureStyles();

        DrawBanner();
        DrawStatusRow();

        if (_mgr == null)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "No ExperimentDataManager found in the open scene.\n" +
                "Click below to add one to the OffsetManager object.",
                MessageType.Warning);
            if (GUILayout.Button("Add ExperimentDataManager to OffsetManager", GUILayout.Height(32)))
                AddManagerToOffsetManager();
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawParticipantCard();
        DrawCalibrationCard();
        DrawMeasurementsCard();
        DrawRecordCard();

        EditorGUILayout.EndScrollView();

        if (Application.isPlaying)
            Repaint();
    }

    // --------------------------------------------------------------------

    private void DrawBanner()
    {
        Rect r = GUILayoutUtility.GetRect(0, 38, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(r, Banner);
        GUI.Label(new Rect(r.x + 12, r.y, r.width - 24, r.height), "Experiment Control", _bannerText);
    }

    private void DrawStatusRow()
    {
        EditorGUILayout.BeginHorizontal();
        Pill(Application.isPlaying ? "● PLAY MODE" : "■ EDIT MODE",
             Application.isPlaying ? Green : Grey);

        if (_mgr.SessionActive)
            Pill($"REC  {_mgr.RecordedCount}/{_mgr.SlotCount}", _mgr.RecordedCount >= _mgr.SlotCount ? Green : Orange);
        else
            Pill("NO SESSION", Grey);

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);
    }

    private void Pill(string text, Color bg)
    {
        var style = EditorStyles.miniBoldLabel;
        var size = style.CalcSize(new GUIContent(text));
        Rect r = GUILayoutUtility.GetRect(size.x + 16, 18, GUILayout.Width(size.x + 16));
        EditorGUI.DrawRect(r, bg);
        var prev = GUI.contentColor;
        GUI.contentColor = new Color(0.12f, 0.12f, 0.12f);
        GUI.Label(new Rect(r.x + 8, r.y, r.width, r.height), text, style);
        GUI.contentColor = prev;
    }

    // --------------------------------------------------------------------

    private void DrawParticipantCard()
    {
        EditorGUILayout.BeginVertical(_card);
        GUILayout.Label("Participant", _sectionTitle);

        using (new EditorGUI.DisabledScope(_mgr.SessionActive))
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            string folder = EditorGUILayout.TextField("Output Folder", _mgr.outputFolder);
            if (EditorGUI.EndChangeCheck())
            {
                _mgr.outputFolder = folder;
                if (!Application.isPlaying) EditorUtility.SetDirty(_mgr);
            }
            if (GUILayout.Button("Browse…", GUILayout.Width(72)))
            {
                string picked = EditorUtility.OpenFolderPanel("Data Output Folder", _mgr.outputFolder, "");
                if (!string.IsNullOrEmpty(picked))
                {
                    _mgr.outputFolder = picked;
                    if (!Application.isPlaying) EditorUtility.SetDirty(_mgr);
                }
            }
            EditorGUILayout.EndHorizontal();

            _numberField = EditorGUILayout.TextField("Participant #", _numberField);
            _nameField = EditorGUILayout.TextField("Name", _nameField);
        }

        EditorGUILayout.Space(4);

        if (!_mgr.SessionActive)
        {
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (ColorButton("Start Participant", Green, 30))
                {
                    if (!_mgr.StartSession(_numberField, _nameField, out string err))
                        EditorUtility.DisplayDialog("Cannot start session", err, "OK");
                }
            }
            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play mode to start a participant and record.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("Active", $"P{_mgr.participantNumber}   {_mgr.participantName}");
            EditorGUILayout.SelectableLabel(_mgr.CurrentFilePath, EditorStyles.miniLabel, GUILayout.Height(14));
            if (GUILayout.Button("End / New Participant"))
            {
                if (EditorUtility.DisplayDialog("End session?",
                        "Finish this participant? Their file is already saved. " +
                        "You'll then be able to start a new participant.",
                        "End Session", "Cancel"))
                {
                    _mgr.EndSession();
                    _numberField = "";
                    _nameField = "";
                }
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawMeasurementsCard()
    {
        EditorGUILayout.BeginVertical(_card);
        GUILayout.Label("Measurements", _sectionTitle);

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Point", GUILayout.Width(104));
        GUILayout.Label("brush X", GUILayout.Width(70));
        GUILayout.Label("button X", GUILayout.Width(70));
        GUILayout.Label("time", GUILayout.Width(48));
        GUILayout.FlexibleSpace();
        GUILayout.Label("cursor", GUILayout.Width(52));
        EditorGUILayout.EndHorizontal();

        Color baseColor = GUI.backgroundColor;

        for (int i = 0; i < _mgr.SlotCount; i++)
        {
            if (i % ExperimentDataManager.PointsPerTrial == 0)
            {
                EditorGUILayout.Space(2);
                GUILayout.Label($"Trial {ExperimentDataManager.TrialOf(i)}", _subLabel);
            }

            var m = _mgr.GetSlot(i);
            bool isCursor = _mgr.SessionActive && i == _mgr.Cursor;

            if (isCursor) GUI.backgroundColor = RowCursor;
            else if (m.recorded) GUI.backgroundColor = RowDone;
            else GUI.backgroundColor = baseColor;

            EditorGUILayout.BeginHorizontal("box");
            GUI.backgroundColor = baseColor;

            GUILayout.Label(ExperimentDataManager.PointOf(i), GUILayout.Width(104));
            GUILayout.Label(m.recorded ? m.brushX.ToString("F4") : "—", GUILayout.Width(70));
            GUILayout.Label(m.recorded ? m.buttonX.ToString("F4") : "—", GUILayout.Width(70));
            GUILayout.Label(m.recorded ? m.appTime.ToString("F1") : "—", GUILayout.Width(48));
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(!_mgr.SessionActive))
            {
                if (GUILayout.Button(isCursor ? "▶ here" : "set", GUILayout.Width(52)))
                    _mgr.SetCursor(i);
            }

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = baseColor;
        }

        GUI.backgroundColor = baseColor;
        EditorGUILayout.EndVertical();
    }

    private void DrawRecordCard()
    {
        EditorGUILayout.BeginVertical(_card);
        bool atEnd = _mgr.SessionActive && _mgr.Cursor >= _mgr.SlotCount;

        string label = !_mgr.SessionActive
            ? "Record"
            : atEnd
                ? "All 12 points recorded"
                : $"RECORD  →  Trial {ExperimentDataManager.TrialOf(_mgr.Cursor)} / {ExperimentDataManager.PointOf(_mgr.Cursor)}";

        using (new EditorGUI.DisabledScope(!_mgr.SessionActive || !Application.isPlaying || atEnd))
        {
            if (ColorButton(label, Red, 48))
            {
                if (!_mgr.RecordCurrent(out string err) && !string.IsNullOrEmpty(err))
                    Debug.LogWarning(err);
            }
        }

        if (atEnd)
            EditorGUILayout.HelpBox("All points recorded. Click a row's 'set' to move the cursor back and re-record.", MessageType.Info);
        EditorGUILayout.LabelField("The controller 'Record' button does the same thing.", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();
    }

    private void DrawCalibrationCard()
    {
        EditorGUILayout.BeginVertical(_card);
        GUILayout.Label("Calibration", _sectionTitle);

        // ---- Brush ----
        GUILayout.Label("Brush", _subLabel);
        if (_brush != null)
        {
            if (ColorButton("Calibrate Brush", Blue, 32))
                _brush.PerformCalibration();

            EditorGUILayout.BeginHorizontal();
            if (ColorButton("Shift Left (-0.18)", Grey, 22))
            {
                Undo.RecordObject(_brush, "Shift Hand Offset Left");
                _brush.hand_offset.x -= 0.18f;
                if (!Application.isPlaying) EditorUtility.SetDirty(_brush);
            }
            if (ColorButton("Shift Right (+0.18)", Grey, 22))
            {
                Undo.RecordObject(_brush, "Shift Hand Offset Right");
                _brush.hand_offset.x += 0.18f;
                if (!Application.isPlaying) EditorUtility.SetDirty(_brush);
            }
            EditorGUILayout.EndHorizontal();

            if (ColorButton("Cycle Hand / Brush Visibility", Grey, 22))
                _brush.PerformComboAction();
        }
        else
        {
            EditorGUILayout.HelpBox("No CalibrateBrush component found on OffsetManager.", MessageType.None);
        }

        EditorGUILayout.Space(6);

        // ---- Table height ----
        GUILayout.Label("Table Height", _subLabel);
        if (_table != null)
        {
            using (new EditorGUI.DisabledScope(!_table.IsReady))
            {
                if (ColorButton("Calibrate Table Height", Teal, 32))
                {
                    if (_table.table != null) Undo.RecordObject(_table.table, "Calibrate Table Height");
                    if (_table.CalibrateTableHeight() && !Application.isPlaying)
                        EditorUtility.SetDirty(_table.table);
                }
            }
            using (new EditorGUI.DisabledScope(!_table.HasOriginal || _table.table == null))
            {
                if (ColorButton("Reset Table Height", Grey, 22))
                {
                    Undo.RecordObject(_table.table, "Reset Table Height");
                    _table.ResetToOriginalHeight();
                    if (!Application.isPlaying) EditorUtility.SetDirty(_table.table);
                }
            }
            if (!_table.IsReady)
                EditorGUILayout.HelpBox("Assign Table + Hand Reference on the TableHeightCalibrator.", MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox("No TableHeightCalibrator component found on OffsetManager.", MessageType.None);
        }

        EditorGUILayout.Space(6);

        // ---- View ----
        GUILayout.Label("View", _subLabel);
        if (_recenter != null)
        {
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (ColorButton("Recenter View", Orange, 28))
                    _recenter.Recenter();
            }
            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Recenter runs in Play mode (uses the live headset pose). Spacebar does the same.", MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox("No RecenterOnSpace component found on OffsetManager.", MessageType.None);
        }

        EditorGUILayout.EndVertical();
    }

    // --------------------------------------------------------------------

    private bool ColorButton(string label, Color color, float height)
    {
        var prev = GUI.backgroundColor;
        GUI.backgroundColor = color;
        bool clicked = GUILayout.Button(label, GUILayout.Height(height));
        GUI.backgroundColor = prev;
        return clicked;
    }

    private void AddManagerToOffsetManager()
    {
        GameObject go = GameObject.Find("OffsetManager");
        if (go == null)
        {
            EditorUtility.DisplayDialog("OffsetManager not found",
                "No GameObject named 'OffsetManager' exists in the open scene. " +
                "Add an ExperimentDataManager component manually to the object that runs the experiment.",
                "OK");
            return;
        }

        var mgr = go.GetComponent<ExperimentDataManager>();
        if (mgr == null)
        {
            mgr = Undo.AddComponent<ExperimentDataManager>(go);
            mgr.sampleSource = go.GetComponent<CalibrateBrush>();
            EditorUtility.SetDirty(go);
            Debug.Log("Added ExperimentDataManager to OffsetManager.");
        }
        _mgr = mgr;
        Refresh();
    }
}
