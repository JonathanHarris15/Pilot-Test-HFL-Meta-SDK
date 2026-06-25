using UnityEngine;

// Vertically aligns the VIRTUAL table to the REAL table.
//
// Workflow: the participant rests their real right hand flat on the real table.
// The virtual hand (tracked by the headset) is therefore sitting at the real
// table's height. Pressing "Calibrate Table Height" reads the tracked hand's
// world Y and moves the virtual table to match - X and Z are left untouched.
//
// CalibrateTableHeight() is public, so the same action can also be bound to a
// controller button, a UI button, or a UnityEvent if you later want in-headset
// self-calibration.
public class TableHeightCalibrator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The virtual table to move. ONLY its vertical (Y) position changes.")]
    public Transform table;

    [Tooltip("The virtual hand point that tracks the real hand resting on the table " +
             "(e.g. the right index fingertip or palm marker). Its world Y is the measured table height.")]
    public Transform handReference;

    [Header("Calibration")]
    [Tooltip("Added to the measured hand height to get the table's Y. Use it to correct for " +
             "where the table's PIVOT sits relative to its top surface, plus hand thickness. " +
             "Positive raises the table. Tune until the virtual surface meets the hand.")]
    public float contactOffset = 0f;

    // Captured the first time we calibrate so the operator can revert a bad reading.
    [SerializeField, HideInInspector] private float _originalHeight;
    [SerializeField, HideInInspector] private bool _hasOriginal;

    // --- Read-only info surfaced in the custom inspector ---
    public float MeasuredHandHeight => handReference != null ? handReference.position.y : 0f;
    public float TargetTableHeight => MeasuredHandHeight + contactOffset;
    public float CurrentTableHeight => table != null ? table.position.y : 0f;
    public bool IsReady => table != null && handReference != null;
    public bool HasOriginal => _hasOriginal;
    public float OriginalHeight => _originalHeight;

    // Moves the table's Y to match the tracked hand. Returns true on success.
    public bool CalibrateTableHeight()
    {
        if (table == null || handReference == null)
        {
            Debug.LogWarning("TableHeightCalibrator: assign both 'Table' and 'Hand Reference' before calibrating.", this);
            return false;
        }

        // Remember where the table started, once, so "Reset" can undo a mistake.
        if (!_hasOriginal)
        {
            _originalHeight = table.position.y;
            _hasOriginal = true;
        }

        Vector3 p = table.position;
        p.y = handReference.position.y + contactOffset;
        table.position = p;

        Debug.Log($"Table height calibrated to Y = {p.y:F4} " +
                  $"(hand {handReference.position.y:F4} + offset {contactOffset:F4}).", this);
        return true;
    }

    // Restores the table height captured before the first calibration.
    public void ResetToOriginalHeight()
    {
        if (table == null || !_hasOriginal) return;

        Vector3 p = table.position;
        p.y = _originalHeight;
        table.position = p;
        Debug.Log($"Table height reset to original Y = {p.y:F4}.", this);
    }
}
