using System;
using System.IO;
using System.Text;
using UnityEngine;

// Central data store + recorder for the experiment.
//
// One participant = 2 trials x 6 points = 12 measurements. The 6 points per
// trial are captured in a fixed order. Each record samples the live, calibrated
// brush X / button X from CalibrateBrush and stamps the app time.
//
// A single record cursor walks the 12 slots. Recording writes into the cursor
// slot and auto-advances. Re-recording = move the cursor back (operator clicks a
// row in the control window) and record again, overwriting from there forward.
//
// The participant's CSV is fully rewritten after every change (auto-save), so
// overwrites and re-records are always reflected on disk with no stale lines.
public class ExperimentDataManager : MonoBehaviour
{
    // The 6 points per trial, in fixed capture order.
    public static readonly string[] PointNames =
    {
        "pre_control", "pre_vis", "pre_touch",
        "post_control", "post_vis", "post_touch"
    };

    public const int TrialsPerParticipant = 2;
    public const int PointsPerTrial = 6;
    public const int TotalSlots = TrialsPerParticipant * PointsPerTrial; // 12

    [Serializable]
    public class Measurement
    {
        public bool recorded;
        public float brushX;
        public float buttonX;
        public float appTime;
    }

    [Header("Sampling source")]
    [Tooltip("Supplies the calibrated brush X and button X at the moment of recording. " +
             "Auto-found on this GameObject if left empty.")]
    public CalibrateBrush sampleSource;

    [Header("Output")]
    [Tooltip("Folder where each participant's CSV is written.")]
    public string outputFolder = @"C:/Users/jonathan.h.1505/Documents/Pilot_Data_Collection";

    // --- Session state ---
    public string participantName = "";
    public string participantNumber = "";
    public bool SessionActive { get; private set; }
    public string CurrentFilePath { get; private set; }

    // 12 measurement slots (trial-major: slots 0-5 = trial 1, 6-11 = trial 2).
    private readonly Measurement[] _slots = new Measurement[TotalSlots];

    // 0..TotalSlots. Equal to TotalSlots means "all points recorded".
    public int Cursor { get; private set; }

    // Lazily create the slot so the editor window can read it before Awake() runs.
    public Measurement GetSlot(int i) => _slots[i] ??= new Measurement();
    public int SlotCount => TotalSlots;

    // How many of the 12 slots have been captured (for progress display).
    public int RecordedCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < TotalSlots; i++)
                if (_slots[i] != null && _slots[i].recorded) n++;
            return n;
        }
    }

    public static int TrialOf(int slot) => slot / PointsPerTrial + 1;     // 1 or 2
    public static string PointOf(int slot) => PointNames[slot % PointsPerTrial];

    private void Awake()
    {
        for (int i = 0; i < _slots.Length; i++) _slots[i] = new Measurement();
        if (sampleSource == null) sampleSource = GetComponent<CalibrateBrush>();
    }

    // ---------------- Session lifecycle ----------------

    public bool StartSession(string number, string name, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(number)) { error = "Participant number is required."; return false; }
        if (string.IsNullOrWhiteSpace(name)) { error = "Participant name is required."; return false; }

        participantNumber = number.Trim();
        participantName = name.Trim();

        try { Directory.CreateDirectory(outputFolder); }
        catch (Exception e) { error = $"Cannot create output folder:\n{e.Message}"; return false; }

        CurrentFilePath = BuildFilePath();

        // Reset all data and the cursor.
        foreach (var m in _slots)
        {
            m.recorded = false;
            m.brushX = m.buttonX = m.appTime = 0f;
        }
        Cursor = 0;
        SessionActive = true;

        Save(); // Write the header + empty rows immediately.
        Debug.Log($"Session started for P{participantNumber} ({participantName}) -> {CurrentFilePath}");
        return true;
    }

    public void EndSession()
    {
        if (!SessionActive) return;
        Save();
        SessionActive = false;
        Debug.Log("Session ended.");
    }

    private string BuildFilePath()
    {
        string baseName = $"P{Sanitize(participantNumber)}_{Sanitize(participantName)}_{DateTime.Now:yyyy-MM-dd}";
        string path = Path.Combine(outputFolder, baseName + ".csv");

        // Never silently overwrite an existing participant file.
        int v = 2;
        while (File.Exists(path))
        {
            path = Path.Combine(outputFolder, $"{baseName}_v{v}.csv");
            v++;
        }
        return path;
    }

    private static string Sanitize(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in s.Trim())
            sb.Append(char.IsLetterOrDigit(c) ? c : (c == ' ' ? '_' : '-'));
        return sb.Length == 0 ? "unnamed" : sb.ToString();
    }

    // ---------------- Recording ----------------

    // Records into the current cursor slot from the live source, then advances.
    public bool RecordCurrent(out string error)
    {
        error = null;
        if (!SessionActive) { error = "Start a participant before recording."; return false; }
        if (Cursor < 0 || Cursor >= TotalSlots)
        {
            error = "All 12 points are already recorded. Move the cursor back to re-record.";
            return false;
        }
        if (sampleSource == null) { error = "No sample source (CalibrateBrush) assigned."; return false; }

        var m = _slots[Cursor];
        m.brushX = sampleSource.CurrentBrushX;
        m.buttonX = sampleSource.CurrentButtonX;
        m.appTime = Time.time;
        m.recorded = true;

        Debug.Log($"Recorded slot {Cursor} (Trial {TrialOf(Cursor)} / {PointOf(Cursor)}): " +
                  $"brushX={m.brushX:F4}, buttonX={m.buttonX:F4}, t={m.appTime:F2}");

        Cursor++;   // auto-advance
        Save();     // auto-save
        return true;
    }

    // Moves the record cursor (operator clicked a row to re-record from there).
    public void SetCursor(int slot)
    {
        Cursor = Mathf.Clamp(slot, 0, TotalSlots);
    }

    // ---------------- Persistence ----------------

    public void Save()
    {
        if (string.IsNullOrEmpty(CurrentFilePath)) return;
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("participant_number,participant_name,trial,point,brush_x,button_x,app_time");
            for (int i = 0; i < TotalSlots; i++)
            {
                var m = _slots[i];
                string brush = m.recorded ? m.brushX.ToString("F6") : "";
                string btn = m.recorded ? m.buttonX.ToString("F6") : "";
                string t = m.recorded ? m.appTime.ToString("F3") : "";
                sb.Append(CsvField(participantNumber)).Append(',')
                  .Append(CsvField(participantName)).Append(',')
                  .Append(TrialOf(i)).Append(',')
                  .Append(PointOf(i)).Append(',')
                  .Append(brush).Append(',')
                  .Append(btn).Append(',')
                  .Append(t).Append('\n');
            }
            File.WriteAllText(CurrentFilePath, sb.ToString());
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save experiment data: {e.Message}");
        }
    }

    private static string CsvField(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
